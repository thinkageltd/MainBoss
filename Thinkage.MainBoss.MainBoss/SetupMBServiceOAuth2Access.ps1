<#
.SYNOPSIS
Create the objects necessary in Azure AD and Exchange to allow the MainBoss Service to fetch mail using PO or IMAP with OAuth2 authentication.
There must be an existing MainBoss Service Configuration in the MainBoss database.
The commandlet requires privileges to modify the MainBoss database and the office 365 and Azure.

.DESCRIPTION
This command script creates an Application Registration, gives it appropriate scopes, provides it with sign-on credentials,
creates an AD Service Principal, creates an Exchange Service Principal, and gives that Principal access to the mailbox.

.INPUTS
None.

.OUTPUTS
None.

.NOTES
The commandlet needs to use MBUtiility which is found in the MainBoss Install directory.
The commandlet tries to use ".\MBUtility" or if that fails then it searches in the MainBoss Install Directory for the most recent MBUtility 

An updatated version of SqlServer module with downloaded an install for the current user.
Modules AzureAD ande ExchangeOnlineManagement will be installed for the current user;
Version 4.2.4.18 $Modtime: 2023-04-25 11:55:56-04:00 $
#>
[CmdletBinding(PositionalBinding=$false)]
param (
    [Parameter(Mandatory=$true)][string]
        # Specifies the connection string for the SQL server. 
		# SQL connections string when used with windows authentication is in the format of
		#           "server=mysqlerver.domain.com;initial catalog=MyDataBaseName;Integrated Security=True"
		# or        "server=mysqlerver.domain.com\InstanceName;initial catalog=TyDataBaseName;Integrated Security=True"
		# where "server" is the sql server, "initial catalog" is the name of the database
		# SQL connections string when used with SQL authentication
        # If windows authentication is not used then authentication credentials must also be supplied.
		# "mysqlerver.domain.com;initial catalog=MyDataBaseName;user id=MySQLUserID;password=MySQLPassword"
        $SQLConnectString,
	[Parameter(Mandatory=$false)][string]
        # Specifies the name to use for the Application Registration and the Display Name for other objects
        $ApplicationName = "MainBoss Service",
	[Parameter(Mandatory=$false)][string]
		# the path to the MainBoss Install directory.
		$MainBossInstallDirectory = $([Environment]::GetEnvironmentVariable("ProgramFiles(x86)")+"\Thinkage\MainBoss")
 
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

try {
	
if( (Test-Path -path ".\MBUtility.exe" ) )
{
	$MBUtility = ".\MBUtility.exe"
}
elseif ( -not (Test-Path -path $MainBossInstallDirectory ) ) 
{ 
	Write-error "Cannot find the MainBoss Install Directory. The MainBoss install directory can be set using -MainBossInstallDirectory" 
	exit
}
else {
	$MBUtility = (Get-ChildItem  $MainBossInstallDirectory -include MBUtility.exe -recurse |sort-object -Descending LastWriteTime |Select-Object -first 1).fullname
	if( $MBUtility -eq $null ) {
		Write-error ("Cannot find MBUlility.exe under "+$MainBossInstallDirectory)
		exit
	}
	"Using: "+$MBUtility
}
	
# We need a name for the app, for the service principal, and for the Exchange service principal.
# right now they are all the same, done here. We could have an option for the "root name" and options to override each specific name
$ADPrincipalName = $ApplicationName
$ExchangePrincipalName = $ApplicationName

# Verify that the user has permission to edit the Service Configuration record.
&$MBUtility editserviceconfiguration /connection:$SQLConnectString
if (!$?) 
{
	Write-error "MButility can not access the database" 
	exit
}

# Get the e-mail address from the database ServiceConfiguration record
$conn = new-object System.Data.SqlClient.SqlConnection $SQLConnectString
$conn.Open()
$cmd = new-object System.Data.SqlClient.SqlCommand "select MailUserName from ServiceConfiguration"
$cmd.Connection = $conn
$EmailAddress = $cmd.ExecuteScalar()
$conn.Close()

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
This script may have to install (download) other script modules, and you may be prompted that these are being loaded from an untrusted location.`n`
You will have to answer 'Yes' to permit the installation. This installation is local to the current user, not global to this computer.`n`
This download may also take several minutes to complete, depending on your network connection.`n`
You may also be prompted once or twice for credentials. One prompt is to authorize access to domain information to create the security objects required`n`
to permit the MainBoss service to fetch mail. The other prompt is to authorize changes to your Exchange service to complete the access grant required.`
"
Install-Module -Name Microsoft.Graph.Authentication -scope CurrentUser -allowClobber -RequiredVersion 1.26.0
Import-module Microsoft.Graph.Authentication -RequiredVersion 1.26.0
Install-Module -Name Microsoft.Graph.Applications -scope CurrentUser -allowClobber -RequiredVersion 1.26.0
Import-module Microsoft.Graph.Applications -RequiredVersion 1.26.0
Install-Module -Name ExchangeOnlineManagement -scope CurrentUser -allowClobber -RequiredVersion 3.1.0
Import-module ExchangeOnlineManagement -RequiredVersion 3.1.0

$connectResult = Connect-MgGraph -TenantId $tenantName -scopes Application.ReadWrite.All

# Create or find existing app registration
if ($appObject = Get-MgApplication -Filter "DisplayName eq '$($ApplicationName)'" )
{
    Write-error "Application object '$ApplicationName' already exists"
	exit
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
$appObject = New-mgApplication -DisplayName $ApplicationName # -RequiredResourceAccess ($accesses)

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
Write-Output ("Client Secret is " + $clientSecret)

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
$ExchangePrincipalObject = New-ServicePrincipal -AppId $appObject.AppId -ServiceId $ADprincipalObject.Id -DisplayName $ExchangePrincipalName
$mailboxPermission = Add-MailboxPermission -Identity $emailAddress -User $ExchangePrincipalObject.ServiceID -AccessRights FullAccess

# Place the client secret into the MB config record. We have to figure out how to encrypt this MB-style.
# Alternatively we could give the MBService exe file an option to update this information.
# Better yet, instead of navigating that maze of options, add a verb to MBUtility to set the OAuth2 access fields.
&$MBUtility EditServiceConfiguration /ClientID:$($appObject.AppId) /ClientSecret:$clientSecret /Connection:$SQLConnectString

# connect-exchangeonline creates all the cmdlets to do things and discommect removes them and so by default asks a confirmation question.
Disconnect-ExchangeOnline -Confirm:$false
Disconnect-MgGraph
# TODO: Should we unimport-module things? (actually, the command is remove-module). Unless we also unistall it this can be pointless.
# The module auto-loading will re-import the module as soon as someone calls a command from that module (assuming unimporting it did not uncover another command
# of the same name).
# TODO: Should we uninstall-module things? Certainly not is our install-module call found the module already present!
}
catch {
# to undo: Remove-MailboxPermission -Identity $emailAddress -User $ExchangePrincipalObject.ServiceID -AccessRights FullAccess
# to undo: something like Remove-ServicePrincipal -identity $ExchangePrincipalObject.ServiceID
# to undo: Remove-MGServicePrincipal -serviceprincipalid $adPrincipalObject.id
# to undo: Remove-MgApplication -applicationid $appObject.id
throw $_
}