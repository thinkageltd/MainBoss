#Provide the Signing procedures for signing executables
# This requires that the local environment variable WINDOWSKIT10 is set to the directory where (since VS2017) 
# Microsoft installs a signtool program (typically C:\Program Files (x86)\Windows Kits\<os release><bin>/<kit version>/x64/))
# you must create the environment variable 'PLATFORMSDK' yourself, there currently is no automatic tool.
# WINDOWSKIT80 is an old version from an automatic tool.
$signtoolRoot = $null
if ($env:WINDOWSKIT10 -ne $null ) {
	$signtoolRoot = $env:WINDOWSKIT10
    $WIN10VERSION = "10.0.22000.0"	#Windows 11 SDK is 10.0.22000.0
}
else {
	throw "WINDOWSKIT10  environment variable must be set to allow assembly signing"
}
# The new "Windows Kits" organization divides bin into x86, x64, and ARM.
$signtool = join-path -path "$signtoolRoot" -childpath (join-path -path 'bin' -childpath (join-path -path "$WIN10VERSION" -childpath (join-path -path 'x86' -childpath 'signtool.exe')))
if (-not (test-path "$signtool")) {
	throw "Unable to locate signtool at expected location "+$signtool
}
#signtoolvsix is built from github open source project separately and included as executable files in our Installation directory; see https://github.com/vcsjones/OpenOpcSignTool.git
$signtoolvsix = join-path -path ".." -childpath (join-path -path 'OpenVsixSignTool' -childpath 'OpenVsixSignTool.exe')

# Get the identifying names of the certificate to sign with
# Current signing requires the actual USB Sectigo key in the machine and this certificate is valid 1/21/2023 to 1/21/2026
$signingCertificateSubject = "CN=Thinkage LTD, O=Thinkage LTD, S=Ontario, C=CA, OID.2.5.4.15=Private Organization, OID.1.3.6.1.4.1.311.60.2.1.2=Ontario, OID.1.3.6.1.4.1.311.60.2.1.3=CA, SERIALNUMBER=125587527"
$signingCertificateIssuer = "CN=Sectigo Public Code Signing CA EV R36, O=Sectigo Limited, C=GB"
#$signingCertificateSubject = "CN=Papertrail Code Signing"
#$signingCertificateIssuer = "CN=Papertrail Code Signing"
# TODO: Have a hook (dot a .ps1 file if it exists which changes the above two variables???) to change which certificate to use without having to modfy repository code.
# TODO: Have the same hook allow the user to skip signing (perhaps set the above two items null) in which case we just return.
# TODO: If we throw an exception it does not cause the prebuild or postbuild script to fail. These should contain a try/catch which perhaps can pry a (script) filename
# and line number so it "looks like an error message" which will automatically fail the build event.

# Load the certificate, look first in Local Machine store, then in user store, to match vsixsigntool search order (not sure it matters given thumbprint)
$inStore = "LocalMachine"
while ($true) {
	# Note that Get-ChildItem will only find certificates with private key available, maybe because of the -CodeSigningCert option.
	$CurrentCodeSignCertificate = (Get-ChildItem -Path Cert:\$inStore\My -CodeSigningCert) | Where-Object {$_.Subject -eq "$signingCertificateSubject" -and $_.Issuer -eq "$signingCertificateIssuer"}
	if ($CurrentCodeSignCertificate -ne $null) {
		break; # We've found a certificate'
	}
	if ($inStore -eq "CurrentUser") {
		# Failed at our last chance
		throw "Unable to load certificate with Subject CN '$signingCertificateSubject'"
	}
	$inStore = "CurrentUser"
}
# TODO: Check for multiple certificates found (the upcoming call to .thumbprint will fail anyway but a nicer message would help)
# TODO: Or perhaps we should pick the one with the latest expiry date. This would do odd things if the one in CurrentUser had a longer expiry
# than the one in LocalMachine. We could just query both together but we would have to remember their locations in the enumeration list
# since even with the /sha1 option certutil still needs to be told to use the user or machine store.

# Extract from the certificate the information each signing tool needs to find it.
# signtoolvsix needs the thumbprint. It looks first in LocalMachine then in CurrentUser so we don't have to tell it.
# TODO: Wt least that's what the source code seems to do but it did not work for me if the certificate was only in CurrentUser (even though I Have
# the private key in both key stores)
# signtool can use the thumbprint but you still have to tell it which store to use.
$signingCertificateThumbPrint = $CurrentCodeSignCertificate.thumbprint
if ($inStore -eq "CurrentUser") {
	$SigntoolLocationOption = $null
}
else {
	$SigntoolLocationOption = "/sm"
}
# Script signing is a PS cmdlet and so takes the certificate directly, no extraction is necessary.

$timestamperURL = "http://sha256timestamp.ws.symantec.com/sha256/timestamp"

# Sign one or more files specified on the command-line. This will retry the sign operation up to 10 times.
# TODO: The purpose of retrying is to cover the case where a transient problem causes the timestamp fetch to fail, and this is really the only
# case we want to retry. Ideally, other causes for failure (the most likely being that the user does not have the private key for the certificate installed)
# note on certificate installation; the /sm option below uses the machine store so make sure you put the certificate in the Personal certificates for the machine, not the current
# user. Also, you must ensure the user who is accessing the store has permissions to get the private key (To fix - open the certificate management, find your certificate, right click -> Manage Private Keys and then in security on top be sure that your user is added and given permissions,)
#
# would not retry.
# Conversely, failure to get a timestamp is treated by signtool as a "warning" so it may exit with zero status anyway.
# The actual error that occurs is:
#EXEC(0,0): error information: "Error: SignerSign() failed." (-2146881269/0x8009310b)
#EXEC(0,0): error : An unexpected internal error has occurred.
# and the file is not signed. We capture these errors to out-null; otherwise if we retry the sign and it succeeds, the presence of the error messages still cause the build to fail.
# We retry ok now and sign the file but the outermost script still exits with code -1 (after completing all the PreBuild code). I think the output appears to contain
# error messages (the above) which Studio is treating as errors.
function ThinkageCodeSign([parameter(Mandatory=$true, position=0)][string[]] $filesToSign) {
	if ($CurrentCodeSignCertificate -eq $null) {
		write-host $signingCertificateSubject certificate is not present.
	} else {
		foreach ($fileToSign in $filesToSign) {
			$retriesLeft = 10
			do {
				$retriesLeft -= 1
				if( $fileToSign.EndsWith("vsix")) {
					$signErrors = (&$signtoolvsix sign $fileToSign --sha1 $signingCertificateThumbPrint --file-digest "sha256" --timestamp $timestamperURL 2>&1)
				}
				elseIf($fileToSign.EndsWith("ps1")) {
					Set-AuthenticodeSignature -FilePath $fileToSign -Certificate $CurrentCodeSignCertificate
				}
				else{
					$signErrors = (&$signtool sign /debug $SigntoolLocationOption /fd SHA256 /td SHA256 /tr $timestamperURL /sha1 $signingCertificateThumbPrint  $fileToSign 2>&1)
				}
			} while ($LastExitCode -ne 0 -and $retriesLeft -gt 0)
			if ($LastExitCode -ne 0) {
				[string]::Format("Error attempting to sign '{0}'", $fileToSign) | out-host
				$signErrors | out-host
			}
			else {
				[string]::Format("Signed '{0}'", $fileToSign) | out-host
			}
		}
	}
}
