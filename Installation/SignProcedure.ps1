#Provide the Signing procedures for signing executables
# This requires that the local environment variable WINDOWSKIT80 is set to the directory where (since VS2012) 
# Microsoft installs a signtool program (typically C:\Program Files (x86)\Windows Kits\<os release><bin>/<kit version>/x64/))
# you must create the environment variable 'PLATFORMSDK' yourself, there currently is no automatic tool.
# WINDOWSKIT80 is an old version from an automatic tool.
$signtoolRoot = $null
$WIN10VERSION = $null
if ($env:WINDOWSKIT10 -ne $null ) {
	$signtoolRoot = $env:WINDOWSKIT10
    $WIN10VERSION = "10.0.22000.0"	#Windows 11 SDK is 10.0.22000.0
}
elseif ($env:WINDOWSKIT80 -ne $null ) {
	$signtoolRoot = $env:WINDOWSKIT80
}
elseif ($env:PLATFORMSDK -ne $null) {
	$signtoolRoot = $env:PLATFORMSDK
}
else {
	throw "WINDOWSKIT80 or PLATFORMSDK environment variable must be set to allow assembly signing"
}
$signtool = join-path -path "$signtoolRoot" -childpath (join-path -path 'bin' -childpath 'signtool.exe')
#signtoolvsix is built from github open source project separately and included as executable files in our Installation directory; see https://github.com/vcsjones/OpenOpcSignTool.git
$signtoolvsix = join-path -path ".." -childpath (join-path -path 'OpenVsixSignTool' -childpath 'OpenVsixSignTool.exe')
#following is the thumbprint of the current Thinkage Code Sign certificate (only way to provide identity to vsix signer)
$signingCertificateThumbPrint = "6BCBEF3F5A78F2A2648C61ABC82700F0DF2BEAB3"

if (-not (test-path "$signtool")) {
	# The new "Windows Kits" organization divides bin into x86, x64, and ARM.
	if ($WIN10VERSION -ne $null) {
		$signtool = join-path -path "$signtoolRoot" -childpath (join-path -path 'bin' -childpath (join-path -path "$WIN10VERSION" -childpath (join-path -path 'x86' -childpath 'signtool.exe')))
	}
	else {
		$signtool = join-path -path "$signtoolRoot" -childpath (join-path -path 'bin' -childpath (join-path -path 'x86' -childpath 'signtool.exe'))
	}
	if (-not (test-path "$signtool")) {
		throw "Unable to locate signtool at expected location "+$signtool
	}
}
# Sign one or more files specified on the command-line. This will retry the sign operation up to 10 times.
# TODO: The purpose of retrying is to cover the case where a transient problem causes the timestamp fetch to fail, and this is really the only
# case we want to retry. Ideally, other causes for failure (the most likely being that the user does not have the private key for the certificate installed)
# would not retry.
# Conversely, failure to get a timestamp is treated by signtool as a "warning" so it may exit with zero status anyway.
# The actual error that occurs is:
#EXEC(0,0): error information: "Error: SignerSign() failed." (-2146881269/0x8009310b)
#EXEC(0,0): error : An unexpected internal error has occurred.
# and the file is not signed. We capture these errors to out-null; otherwise if we retry the sign and it succeeds, the presence of the error messages still cause the build to fail.
# We retry ok now and sign the file but the outermost script still exits with code -1 (after completing all the PreBuild code). I think the output appears to contain
# error messages (the above) which Studio is treating as errors.
function ThinkageCodeSign([parameter(Mandatory=$true, position=0)][string[]] $filesToSign) {
	foreach ($fileToSign in $filesToSign) {
		$retriesLeft = 10
		do {
			$retriesLeft -= 1
			if( $fileToSign.EndsWith("vsix")) {
				$signErrors = (&$signtoolvsix sign $fileToSign --sha1 $signingCertificateThumbPrint --file-digest "sha256" --timestamp "http://sha256timestamp.ws.symantec.com/sha256/timestamp" 2>&1)
			}
			else{
				$signErrors = (&$signtool sign /fd SHA256 /td SHA256 /tr "http://sha256timestamp.ws.symantec.com/sha256/timestamp" /n "Papertrail Code Signing" /i "Papertrail Code Signing"  $fileToSign 2>&1)
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
