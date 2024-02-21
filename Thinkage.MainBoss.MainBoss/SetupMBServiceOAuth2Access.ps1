<#
.SYNOPSIS
Create the objects necessary in Azure AD and Exchange to allow the MainBoss Service to fetch mail using PO or IMAP with OAuth2 authentication.
There must be an existing MainBoss Service Configuration in the MainBoss database.

.DESCRIPTION
This command script creates an Application Registration, gives it appropriate scopes, provides it with sign-on credentials,
creates an AD Service Principal, creates an Exchange Service Principal, and gives that Principal access to the mailbox.

.INPUTS
None.

.OUTPUTS
None.

.NOTES
Version 4.2.4.18 $Modtime: 2023-04-03 08:13:58-04:00 $
#>
[CmdletBinding(PositionalBinding=$false)]
param (
    [Parameter(Mandatory=$false)][string]
        # Specifies the name to use for the Application Registration and the Display Name for other objects
        $ApplicationName = "MainBoss Service",
    [Parameter(Mandatory=$true)][string]
        # Specifies the connection string for the SQL server. This should contain the SQL server name and database name.
        # If windows authentication is not used then authentication credentials must also be supplied.
        $SQLConnectString
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
# We need a name for the app, for the service principal, and for the Exchange service principal.
# right now they are all the same, done here. We could have an option for the "root name" and options to override each specific name
$ADPrincipalName = $ApplicationName
$ExchangePrincipalName = $ApplicationName

# Verify that the user has permission to edit the Service Configuration record.
.\mbutility.exe editserviceconfiguration /connection:$SQLConnectString
if (!$?)
{
    exit
}    
Install-Module SqlServer -Scope CurrentUser
$configRow = Invoke-Sqlcmd -connectionstring $SQLConnectString -query "select MailUserName from ServiceConfiguration"
$EMailAddress = $configRow.MailUserName

# Extract this from $emailAddress
# This is the same thing the OAuth2Manager does in the service, with the same concerns about error checking and pretty emails like "John Smith <jsmith@xxx.ca>"
$tenantName = $EMailAddress.Split('@')[1]

# Create the certificate: $cert = New-SelfSignedCertificate -Subject "CN=Kevins Test MBRequests" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256
# Export the certificate so you can upload it to make a credential for the spp registration: Export-Certificate -Cert $cert -FilePath "C:\Users\kpmartin\documents\MBService.cer"
#Google needs a PEM file, do this: certutil -encode .\MBService.cer MBService2.pem
#
$userCredentials = Get-Credential -Message "Enter Credentials for Azure AD and Exchange Management access"
if($userCredentials -eq $null) 
{
    throw "No or invalid credentials entered"
}

Install-Module -Name AzureAD -scope CurrentUser
Install-Module -Name ExchangeOnlineManagement -scope CurrentUser # -allowprerelease

$connectResult = Connect-AzureAD -TenantId $tenantName -Credential $userCredentials

# Create or find existing app registration
if ($appObject = Get-AzureADApplication -Filter "DisplayName eq '$($ApplicationName)'" -ErrorAction SilentlyCOntinue)
{
    throw "Application object '$ApplicationName' already exists"
}

# Before creating the App Registration, we need to build a list of the Scopes.
# We know them by name, and we have to search the appropriate objects to find their Id's.
# The scopes we want are in two different applications so we need to do two searches
# NOTE: Some day we might also want SMTP.Send from this group
# NOTE: The resource accesses are now being added to the Enterprise Application, further down, to remove the need for Admin Consent grant.
$authorizingPrincipal = get-azureadserviceprincipal -filter "displayName eq 'Office 365 Exchange Online'"
#$accesses = New-Object -TypeName "microsoft.open.azuread.model.requiredresourceAccess" `
#    -ArgumentList ($authorizingPrincipal.AppId, (($authorizingPrincipal.AppRoles `
#        | where-object {$_.Value -eq 'IMAP.AccessAsApp' -or $_.Value -eq 'POP.AccessAsApp'} `
#        | foreach-object {New-Object -TypeName "microsoft.open.azuread.model.resourceAccess" -ArgumentList ($_.Id, "Role")})))

# Create the App Registration (No longer: with the required scopes).
$appObject = New-AzureADApplication -DisplayName $ApplicationName # -RequiredResourceAccess ($accesses)

# Add the Credential which allows the MB service to authenticate with the server.
# We do this after creating the App Registration by calling New-AzureADApplicationXxxCredential which creates the credential,
# applies it to the App Registration, and returns a XxxCredential object for us to examine (e.g. to see the generated password).
# Note that the Credentials can also be created along with the App Registration instead of adding them after. In this case we would use New-Object
# to create a Microsoft.Open.AzureAD.Model.XxxCredential and pass that to a -XxxCredentials option on the New- command. I am not sure, in this case,
# whether we can see the Client Secret that was generated.

# Create the Password Credential, implicitly applying it to the given object (the appObject)
# The actual Secret is generated by the New- call, and we can see it in the created Credential object,
# but it will not be visible in the PasswordCredentials collection of the appObject.
# If we re-fetch the appObject,  The credential will be in the PasswordCredentials collection but the Value (Client Secret) will be blank.
# TODO: Don't know how to set the "Description" for this. StackOverflow implies it can only be done by calling undocumented web API.
# TODO: Make the lifetime be an option. Can this be unbounded (non-expiring)?
$startDate = Get-Date
$credential = New-AzureADApplicationPasswordCredential -ObjectID $appObject.ObjectId -StartDate $startDate -EndDate $startDate.AddYears(1)
$clientSecret = $credential.Value
Write-Output ("Client Secret is " + $clientSecret)

#TODO: Instead, for a certificate, (TODO: Want an option for this)
#$cer = (load from store, maybe, or from file)
#$certBase64 = [System.Convert]::ToBase64String($cer.GetRawCertData())
#$thumbprintBase64 = [System.COnvert]::ToBase64String($cer.GetCertHash())
#New-AzureADApplicationKeyCredential -ObjectID $appObject.ObjectId -StartDate $cer.GetEffectiveDateString() -EndDate $cer.GetExpirationDateString() -CustomKeyIdentifier $thumbprintBase64 -Type AsymmetricX509Cert -Usage Verify -Value $certBase64

# Create the Azure AD Service Principal (Enterprise Application)
$ADprincipalObject = New-AzureADServicePrincipal -AccountEnabled $true -AppId $appObject.AppId -DisplayName $ADPrincipalName -ServicePrincipalType Application -Tags WindowsAzureActiveDirectoryIntegratedApp
# If it already exists: $ADprincipalObject=$(get-azureadserviceprincipal -filter "AppId eq '$($appObject.AppId)'")
# NOTE: The following credentials do not show up anywhere in the AD Portal.
#$credential2=new-azureadserviceprincipalpasswordcredential -objectid $ADprincipalObject.ObjectId
#Write-Output ("Client Secret is " + $credential2.Value)
$assignment1=new-azureadserviceapproleassignment -Id ($authorizingPrincipal.AppRoles | where-object {$_.Value -eq 'IMAP.AccessAsApp'}).Id -principalid $ADprincipalObject.ObjectId -ResourceID $authorizingPrincipal.ObjectId -objectid $authorizingPrincipal.ObjectId
$assignment2=new-azureadserviceapproleassignment -Id ($authorizingPrincipal.AppRoles | where-object {$_.Value -eq 'POP.AccessAsApp'}).Id -principalid $ADprincipalObject.ObjectId -ResourceID $authorizingPrincipal.ObjectId -objectid $authorizingPrincipal.ObjectId

# Give the Enterprise Application permission to access the mailbox
Import-module ExchangeOnlineManagement 
Connect-ExchangeOnline -Organization $tenantName -Credential $userCredentials -ShowBanner:$false
$ExchangePrincipalObject = New-ServicePrincipal -AppId $appObject.AppId -ServiceId $ADprincipalObject.ObjectId -Organization $tenantName -DisplayName $ExchangePrincipalName
$mailboxPermission = Add-MailboxPermission -Identity $emailAddress -User $ExchangePrincipalObject.ServiceID -AccessRights FullAccess

# Place the client secret into the MB config record. We have to figure out how to encrypt this MB-style.
# Alternatively we could give the MBService exe file an option to update this information.
# Better yet, instead of navigating that maze of options, add a verb to MBUtility to set the OAuth2 access fields.
Invoke-Sqlcmd -connectionstring $SQLConnectString -query "update ServiceConfiguration set MailClientID='$($appObject.AppId)'"

.\MBUtility.exe EditServiceConfiguration /ClientID:$($appObject.AppId) /ClientSecret:$clientSecret /Connection:$SQLConnectString
}
catch {
throw $_
}