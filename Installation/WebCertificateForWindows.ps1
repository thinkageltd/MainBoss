function MakeWebCertificate {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory,ParameterSetName = 'HostNames')]
		[string]$HostNames
	)
	$exportFile = New-TemporaryFile
	$date_now = Get-Date
	$date_extended = $date_now.AddYears(20)
	$cert = New-SelfSignedCertificate -CertStoreLocation cert:\localmachine\my -dnsname $HostNames -NotAfter $date_extended -KeyUsageProperty All -KeyLength 4096
	Export-Certificate -Cert $cert -FilePath $exportFile
	Import-Certificate -FilePath $exportFile -CertStoreLocation cert:\LocalMachine\Root
	Import-Certificate -FilePath $exportFile -CertStoreLocation cert:\CurrentUser\My
	Import-Certificate -FilePath $exportFile -CertStoreLocation cert:\CurrentUser\Root
	Remove-Item $exportFile
}
 
