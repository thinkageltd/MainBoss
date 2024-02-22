<#
.SYNOPSIS
Manage the objects necessary in Azure AD and Exchange that allow the MainBoss Service to fetch mail using POP or IMAP with OAuth2 authentication.
There must be an existing MainBoss Service Configuration in the MainBoss database.

.DESCRIPTION
This command script manages the objects required to provide the MainBoss Service with access to a mailbox to
fetch request e-mails from it. Objects managed include an Application Registration, with appropriate scopes
and sign-on credentials, an AD Service Principal (Enterprise Application),
an Exchange Service Principal, with its mailbox permissions, and the relevant connection
information in the Service Configuration of the MainBoss database.

.INPUTS
None.

.OUTPUTS
None.

.NOTES
This command needs to use MBUtiility; it will use ".\MBUtility.exe" if that exists, otherwise it will use
the MBUtility.exe located somewhere under the MainBoss Install Directory specified by the $MainBossInstallDirectory parameter.

Specific versions of the Microsoft.Graph and ExchangeOnlineManagement Powershell modules will be downloaded and
installed local to the user if they are not already present.

The user may be asked to allow downloading of these packages from an untrusted repository.

The user may be prompted twice for credentials giving access to the Active Directory for the domain and access
to the Exchange server.

The user must be a valid MainBoss user who has permission to modify the Service Configuration.

Version 4.2.4.20
#>
[CmdletBinding(PositionalBinding=$false)]
param (
	[Parameter(Mandatory=$true,parameterSetName="Setup")]
		# Set up the objects and permissions required to give the MainBoss Service access to a requests mailbox.
		[switch] $Setup,
	[Parameter(Mandatory=$true,parameterSetName="RemoveCurrent")]
	[Parameter(Mandatory=$true,parameterSetName="RemoveNonCurrent")]
		# Remove the permissions and objects to give the MainBoss Service access to a requests mailbox.
		# This will also try to remove items from a setup that is somehow non-functional.
		# When used with -SQLCOnnectString the service is found using the information in the Service Configuration record,
		# and on success this information is cleared out. This application's name must match the ApplicationName if specified.
		# When used with -EmailAddress the service is found from the given ApplicationName and no MainBoss database is accessed.
		[switch] $Remove,
	[Parameter(Mandatory=$true,parameterSetName="Refresh")]
		# Refresh an existing MainBoss Service connection with a new password.
		[switch] $Refresh,
	[Parameter(Mandatory=$true,parameterSetName="Setup")]
	[Parameter(Mandatory=$true,parameterSetName="RemoveCurrent")]
	[Parameter(Mandatory=$true,parameterSetName="Refresh")]
        # Specifies the connection string for the SQL server. 
		# When used with windows authentication the format can be something like
		#   "server=mysqlerver.domain.com;initial catalog=MyDataBaseName;Integrated Security=True"
		# where "server" is the sql server, "initial catalog" is the name of the database
		# When used with SQL authentication the format can be something like
		#   "server=mysqlerver.domain.com;initial catalog=MyDataBaseName;user id=MySQLUserID;password=MySQLPassword"
		# If the server is not the default instance on the system, the instance name is specified as part of the server, for example
		#   "server=mysqlerver.domain.com\InstanceName;initial catalog=TyDataBaseName;Integrated Security=True"
        [string]$SQLConnectString = $null,
	[Parameter(Mandatory=$true,parameterSetName="RemoveNonCurrent")]
		# specifies the mailbox to remove permissions from and implicitly the tenant (domain) in which changes should be made.
		[string]$EmailAddress = $null,
	[Parameter(Mandatory=$false,parameterSetName="RemoveCurrent")]
	[Parameter(Mandatory=$false,parameterSetName="RemoveNonCurrent")]
		# specifies that the application and its permissions should be removed from all mailboxes rather than just the one used by the MainBoss Service,
		# and that applications with certain SignInAudience values are allowed to be deleted.
		[switch]$Force,
	[Parameter(Mandatory=$false,parameterSetName="Setup")]
	[Parameter(Mandatory=$false,parameterSetName="RemoveCurrent")]
	[Parameter(Mandatory=$true,parameterSetName="RemoveNonCurrent")]
        # Specifies the name to use for the Application Registration and the Display Name for other objects.
        [string]$ApplicationName = $null,
	[Parameter(Mandatory=$false,parameterSetName="Setup")]
	[Parameter(Mandatory=$false,parameterSetName="RemoveCurrent")]
	[Parameter(Mandatory=$false,parameterSetName="Refresh")]
		# the path to the MainBoss Install directory.
		[string]$MainBossInstallDirectory = $([Environment]::GetEnvironmentVariable("ProgramFiles(x86)")+"\Thinkage\MainBoss")
 
)
# This seems a bit complicated because the model is intended for applications that will be published and used in several domains (tenants)
# Thus there is the App Registration, which is sort of a prototype, and the Enterprise Application, which is an instance.
# The App Registration can be marked as multi-tenant (so it can be instantiated in several domains) but the Credentials here will be the same for all.
# In our case we have a single-tenant App Registration, and we only instantiate it in our own domain. It is the Enterprise Application which ultimately
# is granted permissions to access the mailbox.

# There are two ways to grant permissions (scopes) to the Enterprise Application:
# One is to add them to the App Registration, in which case they are automatically inherited by the Enterprise Application, except that because
# of the specific scopes we apply "tenant admin consent" is required, so this still needs intervention on the Enterprise Application to enable the scopes.
# The other, used here because of the admin consent problem, and possible because this is a single-tenant application,
# is to add them to the Enterprise Application after we create it.
#
# Manually granting admin consent is a bit confusing when seen from the AD portal because you can do this in two places,
# from the API Permissions of the App Registration, or from the Permissions on the Enterprise Application.
# Doing it in the latter location has a delay (10-15 seconds?) before the granted permissions appear on Refresh.

# Credentials can be added to the App Registration or the Enterprise Application. It would seem to be that the former credentials would be
# useable by all instances of the App Registration (if it were multi-tenant). I think each MB Service site should have its own credentials,
# meaning they should be applied to the Enterprise Application (should we ever publish our App Registration as multi-tenant).
# The problem is that the AD Portal does not seem to show, or allow changes to, per-instance credentials.
# So although per-instance credentials would be the way to go, because we are single-tenant, putting them on the App Registration is equivalent
# and allows access to them via the AD portal.

# We put a try/catch around the whole script so that we stop on an error rather than barging on with the command not done

# We need a name for the app, for the service principal, and for the Exchange service principal.
# right now they are all the same, done here. We could have an option for the "root name" and options to override each specific name
if ($Setup -and [string]::IsNullOrEmpty($ApplicationName)) {
	$ApplicationName = "MainBoss Service"
}
$ADPrincipalName = $ApplicationName
$ExchangePrincipalName = $ApplicationName

$connectResult = $null
$appObject = $null
$ADprincipalObject = $null
$ExchangePrincipalObject = $null
$mailboxPermission = $null
try {
if (![String]::IsNullOrEmpty($SqlConnectString)) {	
	if( (Test-Path -path ".\MBUtility.exe" ) )
	{
		$MBUtility = ".\MBUtility.exe"
	}
	elseif ( -not (Test-Path -path $MainBossInstallDirectory ) ) 
	{ 
		throw "Cannot find the MainBoss Install Directory. The MainBoss install directory can be set using -MainBossInstallDirectory" 
	}
	else {
		$MBUtility = (Get-ChildItem  $MainBossInstallDirectory -include MBUtility.exe -recurse |sort-object -Descending LastWriteTime |Select-Object -first 1).fullname
		if( $MBUtility -eq $null ) {
			throw ("Cannot find MBUlility.exe under "+$MainBossInstallDirectory)
		}
		"Using: "+$MBUtility
	}
	
	# Verify that the user has permission to edit the Service Configuration record.
	&$MBUtility editserviceconfiguration /connection:$SQLConnectString
	if (!$?) 
	{
		throw "MButility can not access the database" 
	}

	# Get the e-mail address from the database ServiceConfiguration record
	$conn = new-object System.Data.SqlClient.SqlConnection $SQLConnectString
	$conn.Open()
	$cmd = new-object System.Data.SqlClient.SqlCommand "select MailUserName from ServiceConfiguration"
	$cmd.Connection = $conn
	$EmailAddress = $cmd.ExecuteScalar()
	$conn.Close()
}

# Extract the tenant name from $emailAddress
# This is the same thing the OAuth2Manager does in the service, with the same concerns about error checking and pretty emails like "John Smith <jsmith@xxx.ca>"
$tenantName = $EMailAddress.Split('@')[1]

# Create the certificate: $cert = New-SelfSignedCertificate -Subject "CN=Kevins Test MBRequests" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256
# Export the certificate so you can upload it to make a credential for the spp registration: Export-Certificate -Cert $cert -FilePath "C:\Users\kpmartin\documents\MBService.cer"
#Google needs a PEM file, do this: certutil -encode .\MBService.cer MBService2.pem
#

# TODO: Put min and max versions on all these, both the Install and the Import.
# NOTE: If -RequiredVersion is used it will install it side-by-side with other versions no questions asked.
# But if -MinimumVersion and/or -MaxomumVersion are used to give a range of acceptable versions it will not install anything if there is already some
# other version installed. It doesn't ask you, it just tells you that the -Force option must be used. This option is unfortunately a sledge hammer
# that also covers over other conditions we might be concerned about.
# However in this case we are asking for a specific version so we don't need -Force.
# The -Force option has the advantage of disabling the untrusted-repository warning though.
# whether or not we use -Force, the message to the user still mentions that they should respond "yes" to the untrusted-repository message.
write-host "`
This script may have to install (download) other script modules,`
and you may be prompted that these are being loaded from an untrusted location.`
You will have to answer 'Yes' to permit the installation.`
This installation is local to the current user, not global to this computer.`
Downloading may take several minutes to complete, depending on your network connection.`n`
You may also be prompted once or twice for credentials.`
One prompt is to authorize access to domain information to manage the security objects required`
to permit the MainBoss service to fetch mail.`
The other prompt is to authorize changes to your Exchange service to complete the changes required.`
"
Install-Module -Name Microsoft.Graph.Authentication -scope CurrentUser -allowClobber -RequiredVersion 1.26.0
Import-module Microsoft.Graph.Authentication -RequiredVersion 1.26.0
Install-Module -Name Microsoft.Graph.Applications -scope CurrentUser -allowClobber -RequiredVersion 1.26.0
Import-module Microsoft.Graph.Applications -RequiredVersion 1.26.0
Install-Module -Name ExchangeOnlineManagement -scope CurrentUser -allowClobber -RequiredVersion 3.1.0
Import-module ExchangeOnlineManagement -RequiredVersion 3.1.0

if ($Setup) {
	if ([String]::IsNullOrEmpty($ApplicationName)) {
		$ApplicationName = "MainBoss Service"
	}
	
	$connectResult = Connect-MgGraph -TenantId $tenantName -scopes Application.ReadWrite.All

	# Read the ServiceConfiguration record to get the ClientId, which is the AppId of the appObject, to see if a valid one is already registered.
	$conn = new-object System.Data.SqlClient.SqlConnection $SQLConnectString
	$conn.Open()
	$cmd = new-object System.Data.SqlClient.SqlCommand "select MailClientID from ServiceConfiguration"
	$cmd.Connection = $conn
	$clientID = $cmd.ExecuteScalar()
	$conn.Close()
	if ($clientID -ne $null -and $clientID -isnot [System.DBNull]) {
		# Find the existing appObject using the clientID
		$existingAppObject = Get-MgApplication -Filter "AppId eq '$clientID'" 
		if ($existingAppObject -ne $null) {
			throw "The Service Configuration record already refers to the application named '$($existingAppObject.DisplayName)'"
		}
		write-warning "The Service Configuration record contains a Client ID '$clientId' that does not refer to any application. This information will be overwritten."
	}

	# Create or find existing app registration
	if ($existingAppObject = Get-MgApplication -Filter "DisplayName eq '$ApplicationName'" )
	{
		throw "Application object '$ApplicationName' already exists"
	}
	# Before creating the App Registration, we need to build a list of the Scopes.
	# We know them by name, and we have to search the appropriate objects to find their Id's.
	# The scopes we want are in two different applications so we need to do two searches
	# NOTE: Some day we might also want SMTP.Send from this group
	# NOTE: The resource accesses are now being added to the Enterprise Application, further down, to remove the need for Admin Consent grant.
	$authorizingPrincipal = get-mgserviceprincipal -filter "displayName eq 'Office 365 Exchange Online'"
	#$accesses = New-Object -TypeName "microsoft.open.azuread.model.requiredresourceAccess" `
	#    -ArgumentList ($authorizingPrincipal.AppId, (($authorizingPrincipal.AppRoles `
	#        | where-object {$_.Value -eq 'IMAP.AccessAsApp' -or $_.Value -eq 'POP.AccessAsApp'} `
	#        | foreach-object {New-Object -TypeName "microsoft.open.azuread.model.resourceAccess" -ArgumentList ($_.Id, "Role")})))

	# Create the App Registration (No longer: with the required scopes).
	$appObject = New-mgApplication -DisplayName $ApplicationName -SignInAudience AzureADMyOrg # -RequiredResourceAccess ($accesses)

	# Add the Credential which allows the MB service to authenticate with the server.
	# We do this after creating the App Registration by calling Add-MgApplicationPassword which creates the credential,
	# applies it to the App Registration, and returns a XxxCredential object for us to examine (e.g. to see the generated password).

	# Create the Password Credential, implicitly applying it to the given object (the appObject)
	# The actual Secret is generated by the New- call, and we can see it in the created Credential object,
	# but it will not be visible in the PasswordCredentials collection of the appObject.
	# If we re-fetch the appObject,  the credential will be in the PasswordCredentials collection but the SecretText will be blank.
	# TODO: Don't know how to set the "Description" for this. StackOverflow implies it can only be done by calling undocumented web API. Update: the MS Graph applet might have an option for this
	# TODO: Make the lifetime be an option. Can this be unbounded (non-expiring)?
	$startDate = Get-Date
	$credential = Add-MgApplicationPassword -ApplicationID $appObject.Id `
		 -passwordcredential @{ displayName = 'Password created by SetupMBServiceOAuth2Access script';startDateTime = $startDate; EndDateTime = $startDate.AddYears(1) }
	$clientSecret = $credential.SecretText

	#TODO: Instead, for a certificate, (TODO: Want an option for this)
	#$cer = (load from store, maybe, or from file)
	#$certBase64 = [System.Convert]::ToBase64String($cer.GetRawCertData())
	#$thumbprintBase64 = [System.COnvert]::ToBase64String($cer.GetCertHash())
	#New-AzureADApplicationKeyCredential -ObjectID $appObject.ObjectId -StartDate $cer.GetEffectiveDateString() -EndDate $cer.GetExpirationDateString() -CustomKeyIdentifier $thumbprintBase64 -Type AsymmetricX509Cert -Usage Verify -Value $certBase64

	# Create the Azure AD Service Principal (Enterprise Application)
	$ADprincipalObject = New-MgServicePrincipal -AccountEnabled -AppId $appObject.AppId -DisplayName $ADPrincipalName -ServicePrincipalType Application -Tags WindowsAzureActiveDirectoryIntegratedApp
	# If it already exists: $ADprincipalObject=$(get-azureadserviceprincipal -filter "AppId eq '$($appObject.AppId)'")
	# NOTE: Credentials created this way do not show up anywhere in the AD Portal.
	#$credential2=new-azureadserviceprincipalpasswordcredential -objectid $ADprincipalObject.ObjectId
	#Write-Output ("Client Secret is " + $credential2.Value)
	$assignment1=new-MgServicePrincipalAppRoleAssignment -AppRoleId ($authorizingPrincipal.AppRoles | where-object {$_.Value -eq 'IMAP.AccessAsApp'}).Id -principalid $ADprincipalObject.Id -ResourceID $authorizingPrincipal.Id -serviceprincipalid $authorizingPrincipal.Id
	$assignment2=new-MgServicePrincipalAppRoleAssignment -AppRoleId ($authorizingPrincipal.AppRoles | where-object {$_.Value -eq 'POP.AccessAsApp'}).Id -principalid $ADprincipalObject.Id -ResourceID $authorizingPrincipal.Id -serviceprincipalid $authorizingPrincipal.Id

	# Give the Enterprise Application permission to access the mailbox
	Connect-ExchangeOnline -Organization $tenantName -ShowBanner:$false
	$ExchangePrincipalObject = New-ServicePrincipal -AppId $appObject.AppId -objectid $ADprincipalObject.Id -DisplayName $ExchangePrincipalName
	$mailboxPermission = Add-MailboxPermission -Identity $emailAddress -User $ExchangePrincipalObject.ObjectID -AccessRights FullAccess

	# Place the client secret into the MB config record. We have to figure out how to encrypt this MB-style.
	# Alternatively we could give the MBService exe file an option to update this information.
	# Better yet, instead of navigating that maze of options, add a verb to MBUtility to set the OAuth2 access fields.
	&$MBUtility EditServiceConfiguration /ClientID:$($appObject.AppId) /ClientSecret:$clientSecret /Connection:$SQLConnectString

	write-output "Application '$($appObject.DisplayName)' created and registered in the MainBoss Service Configuration record"
}
elseif ($Remove) {
	function Private:Remove-Service {
		param ($ApplicationObject)
		function Private:Get-ServicePrincipalOrNull {
			param ($ADPrincipalId)
			# This function is here to cover the stupidity of Get-ServicePrincipal which, if nothing is found, writes an obnoxious message of the form:
			# Ex6F9304|Microsoft.Exchange.Configuration.Tasks.ManagementObjectNotFoundException|The operation couldn't be performed because object ... couldn't be found ...'
			# using Write-Error rather than quietly returning null, or throwing an exception, or something else programmatically sensible.
			#
			# Also, the web api returns a warning, printed client-side using write-warning, about a parameter I'm not using bein deprecated.
			# By supplying appropruate WarningAction choice we suppress showing this to the user.
			#
			# Also the -Identity parameter searches several fields; we rely here on the Id never being the same as any other Id.
			# There seems to be no way to get a list of all the service principals so we can filter them ourselves either.
			#
			# This odd combination of syntax and redirection captures the service principal (if found) in $foundPrincipal and any error
			# output in $errorObject.
			$errorObject = $($foundPrincipal = Get-ServicePrincipal -Identity $ADPrincipalId -warningaction SilentlyContinue) 2>&1
			if ($errorObject -eq $null) {
				$foundPrincipal
			}
			elseif ($errorObject -is [ErrorRecord] -and $errorObject.Exception -like "*.ManagementObjectNotFoundException|*") {
				# Even at that we use crappy text-pattern-matching to identify the particular error. Hopefully this check of the name of the error type
				# should at least be internationalization-proof.
				$null
			}
			else {
				throw $errorObject
			}
		}
		# Note that $EmailAddress may be an alias and so not the Identity of the mailbox.
		Connect-ExchangeOnline -Organization $tenantName -ShowBanner:$false
		$targetMailbox = Get-Mailbox -Identity $EmailAddress
		if (!$Force) {
			# Refuse to do anything if other mailboxes use the principal
			foreach ($ADprincipalObject in $(Get-MgServicePrincipal -filter "Appid eq '$($ApplicationObject.AppId)'")) {
				$ExchangePrincipalObject = Get-ServicePrincipalOrNull -ADPrincipalId $($ADPrincipalObject.Id)
				if ($ExchangePrincipalObject -ne $null) {
					$otherPermittedMailboxes = $(get-mailbox).Identity `
						| where-object {$_ -ne $targetMailbox.Identity `
							-and $(Get-MailboxPermission -Identity $_ | where-object UserSid -eq $ExchangePrincipalObject.Sid) -ne $null}
					if ($otherPermittedMailboxes -ne $null) {
						throw "This application has permissions for other mailbox(es) $($otherPermittedMailboxes.identity | foreach-object {"'$_'"})).`
Use the -Force option to remove permissions from all mailboxes"
					}
				}
			}
		}
		foreach ($ADprincipalObject in $(Get-MgServicePrincipal -filter "Appid eq '$($ApplicationObject.AppId)'")) {
			$ExchangePrincipalObject = Get-ServicePrincipalOrNull -ADPrincipalId $($ADPrincipalObject.Id)
			if ($ExchangePrincipalObject -ne $null) {
				foreach ($mailboxIdentity in $(Get-Mailbox).Identity) {
					# get-mailboxpermission doesn't have adequate filtering so we look for ourselves by SID
					foreach ($mailboxPermission in Get-MailboxPermission -Identity $mailboxIdentity | where-object UserSid -eq $ExchangePrincipalObject.Sid) {
						remove-mailboxpermission -Identity $mailboxIdentity -AccessRights $mailboxPermission.AccessRights -User $ExchangePrincipalObject.ObjectID -confirm:$false
					}
				}
				remove-ServicePrincipal -Identity $ExchangePrincipalObject.Identity -confirm:$false
			}
		}
		# deleting the Application implicitly deletes all the Service Principals. I wish they would make up their minds which id is the "application id"
		remove-MgApplication -ApplicationId ($appObject.Id)
		write-output "Application '$($appObject.DisplayName)' has been removed"
	}
	# If we have a connect string this means we are removing the currently-registered service.
	if (![String]::IsNUllOrEmpty($SQLConnectString)) {
		# Read the ServiceConfiguration record to get the ClientId, which is the AppId of the appObject.
		$conn = new-object System.Data.SqlClient.SqlConnection $SQLConnectString
		$conn.Open()
		$cmd = new-object System.Data.SqlClient.SqlCommand "select MailClientID from ServiceConfiguration"
		$cmd.Connection = $conn
		$clientID = $cmd.ExecuteScalar()
		$conn.Close()
		if ($clientID -eq $null) {
			throw "The Service Configuration could not be read"
		}
		if ($clientID -is [System.DBNull]) {
			throw "The Service Configuration does not reference any application (Client ID is null)"
		}
		$connectResult = Connect-MgGraph -TenantId $tenantName -scopes Application.ReadWrite.All
		# Find the appObject using the clientID
		$appObject = Get-MgApplication -Filter "AppId eq '$clientID'" 
		if ($appObject -eq $null) {
			throw "The application referenced by the Service Configuration record can't be found.`
Edit the Service Configuration using MainBoss to clear out the Client ID and Client Secret fields and revert to Plain authentication"
		}
		# Verify this is the correct application name, if specified
		if (![String]::IsNUllOrEmpty($ApplicationName) -and $appObject.DisplayName -ne $ApplicationName) {
			throw "The application referenced by the Service Configuration record is named '$($appObject.DisplayName)' which does not match the specified name '$ApplicationName'"
		}
		Remove-Service $appObject
		&$MBUtility EditServiceConfiguration /UseOAuth2- /Connection:$SQLConnectString
	}
	else {
		$connectResult = Connect-MgGraph -TenantId $tenantName -scopes Application.ReadWrite.All
		# Remove a service not registered in a MainBoss database
		$appObject = Get-MgApplication -Filter "DisplayName eq '$ApplicationName'"
		if ($appObject -eq $null) {
			throw "No application named $ApplicationName could be found"
		}
		# Sanity-check the sign-in audience. If it is not what we expect this could be a non-MainBoss App Registration which is referenced in other tenants
		# and deleting it would wreak havoc. We now create them with the most restrictive AzureADMyOrg, but had been creating them with
		# AzureADandPersonalMicrosoftAccount (the default for Add-MGApplication). The former we delete without further ado. The latter we delete if
		# -Force is specified. Any others we refuse to delete.
		if ($appObject.SignInAudience -ne "AzureADMyOrg") {
			if ($appObject.SignInAudience -eq "AzureADandPersonalMicrosoftAccount") {
				if (!$Force) {
					throw "Application has unusual SignInAudience $($appObject.SignInAudience) and might not be a MainBoss Service application. Use the -Force option to delete it."
				}
			}
			else {
				throw "Application has wrong SignInAudience $($appObject.SignInAudience) and so it is not a MainBoss Service application and cannot be deleted."
			}

		}
		Remove-Service $appObject
	}
}
elseif ($Refresh) {
	# Read the ServiceConfiguration record to get the ClientId, which is the AppId of the appObject.
	$conn = new-object System.Data.SqlClient.SqlConnection $SQLConnectString
	$conn.Open()
	$cmd = new-object System.Data.SqlClient.SqlCommand "select MailClientID from ServiceConfiguration"
	$cmd.Connection = $conn
	$clientID = $cmd.ExecuteScalar()
	$conn.Close()
	if ($clientID -eq $null) {
		throw "The Service Configuration could not be read"
	}
	if ($clientID -is [System.DBNull]) {
		throw "The Service Configuration does not reference any application (Client ID is null)"
	}
	$connectResult = Connect-MgGraph -TenantId $tenantName -scopes Application.ReadWrite.All
	# Find the appObject
	$appObject = Get-MgApplication -Filter "AppId eq '$clientID'" 
	if ($appObject -eq $null) {
		throw "No Application could be found with the Client Id '$clientID' stored in the Service Configuration record"
	}
	if ($appObject.passwordCredentials.Length -ge 2) {
		# We must remove one to make room for another.
		# We can't tell which one MB is already using, so just pick the one with the earliest expiry date
		$toRemove = ($appObject.passwordCredentials | Sort-Object -property EndDateTime)[0]
		write-warning "The following old credential will be removed to make room, since only two are allowed: $($toRemove | Out-String)"
		remove-MgApplicationPassword -ApplicationId $appObject.Id -KeyId $toRemove.KeyId
	}
	# TODO: The next 4 lines should be in a function because Setup does this too.
	$startDate = Get-Date
	$endDate = $startDate.AddYears(1)
	$credential = Add-MgApplicationPassword -ApplicationID $appObject.Id `
		 -passwordcredential @{ displayName = 'Password created by SetupMBServiceOAuth2Access script';startDateTime = $startDate; EndDateTime = $endDate }
	if ($credential -eq $null) {
		throw "Unable to add new credential"
	}
	$clientSecret = $credential.SecretText
	&$MBUtility EditServiceConfiguration /ClientID:$($appObject.AppId) /ClientSecret:$clientSecret /Connection:$SQLConnectString
	write-output "New credentials expiry date is $endDate"
}

}
catch {
	$caughtError = $_
	if ($Setup) {
		# If we died doing a Setup, delete anything we've created.
		if ($mailboxPermission -ne $null) {
			Remove-MailboxPermission -Identity $emailAddress -User $ExchangePrincipalObject.ObjectID -AccessRights FullAccess
		}
		if ($ExchangePrincipalObject -ne $null) {
			Remove-ServicePrincipal -identity $ExchangePrincipalObject.ObjectID
		}
		if ($adPrincipalObject -ne $null) {
			Remove-MGServicePrincipal -serviceprincipalid $adPrincipalObject.id
		}
		if ($appObject -ne $null) {
			Remove-MgApplication -applicationid $appObject.id
		}
	}
	throw $caughtError
}
finally {
	# TODO: Should we remove-module things (undo import-module)? Unless we also unistall it this can be pointless.
	# The module auto-loading will re-import the module as soon as someone calls a command from that module (assuming unimporting it did not uncover another command
	# of the same name).
	# TODO: Should we uninstall-module things? Certainly not if our install-module call found the module already present!
	# TODO: Now do we know we're connected??'
	Disconnect-ExchangeOnline -Confirm:$false
	if ($connectResult -ne $null) {
		# This command writes to the output the connection it is closing, so we eat that into a variable.
		$junk = Disconnect-MgGraph
	}
}
