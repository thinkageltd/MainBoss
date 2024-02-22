#Set-PSDebug -trace 1             #for debugging
# copy the Dart.dll into Thinkage.MainBoss.Database

# Thinkage.Mainboss.MainBoss\exe.Licenses and Thinkage.Mainboss.Service\exe.licenses must be checked out
# Thinkage.Mainboss.MainBoss\licenses.licx and Thinkage.Mainboss.Service\licenses.licx must be checked out

# the following command will fail if you do not have the Dart Developer's license registered on your computer (and perhaps for your user name)
# The lc command is needed in has been part of Microsoft .NET Framework x.x.x Developer Pack and Language Packs
# the location seems to change from time to time.
#the lc command is build into visual studio, but you have to use View->Terminal from the menu to get the search rules right.
$lc    =    "lc.exe"																				 #location of License Compiler Command
$mb    =      split-path -parent $PSScriptRoot                                                       #location of $Products/MainBoss/XX/Thinkage.MainBoss.Mainboss on local disk
$lic   =      "Dart.Mail.MailMessage, Dart.Mail"                                                     #expected contents of Licences.licx

$anyError = $false;
#if( !(Test-Path $lc) ) { Write-Error "\$lc must be the location of the License Compiler command";  $anyError = $true }  #is now built in
if( !(Test-Path ($mb+"\MainBoss.sln")) ) { Write-Error "\$mb must be the location of \$Products/MainBoss/XX on local disk";  $anyError = $true }
if( $anyError ) { Exit }

function createLicense ($program ) {
	Push-Location "$mb\$program"
	#check that licenses.licx is correct
	$content = [System.IO.File]::ReadAllText("$mb\$program\licenses.licx")
	if( $content -ne $lic ) {
		Try { $lic| Out-File "$mb\$program\licenses.licx" }
		 Catch { Write-Error "Unable to write to output file $program\licenses.licx -- is file checked out" ;  $anyError = $true }
	}
	Try { [io.file]::OpenWrite("$mb\$program\exe.Licenses").close() }
	 Catch { Write-Error "Unable to write to output file $program\exe.Licenses -- is file checked out";  $anyError = $true }

	 if( $anyError ) { Exit }

	 #create License

	& "$lc" /v /target:exe /complist:licenses.licx /outdir:. /i:"..\Thinkage.MainBoss.Database\Dart.Mail.dll"
    Pop-Location
}
createLicense("Thinkage.MainBoss.MainBoss")
createLicense("Thinkage.MainBoss.Service")
