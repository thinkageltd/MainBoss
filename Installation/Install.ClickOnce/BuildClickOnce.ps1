# adjust location of magetoolRoot as needed given microsoft shuffles it around constantly.
$magetoolRoot = "C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v10.0A"
$magetool = join-path -path "$magetoolRoot" -childpath (join-path -path 'bin' (join-path -path 'NETFX 4.6.1 Tools' -childpath 'mage.exe'))
. (resolve-path '..\SignProcedure.ps1')
################# FUNCTIONS ############################
function SaveXML {
	Param ($file, [System.Xml.XmlDocument] $xml)
	
	$s = new-object System.Xml.XmlWriterSettings
	$s.Indent = $true
	$s.IndentChars = "  "
	$s.NewLineHandling = [System.Xml.NewLineHandling]::Entitize
	$s.CloseOutput = $true
	$s.CheckCharacters = $false

	$w = [System.Xml.XmlWriter]::Create((resolve-path $file), $s)
	$xml.WriteTo($w)
	$w.Close()
}

################################################################################################
$source = "..\..\Thinkage.MainBoss.MainBoss\bin\desktop"
$teamviewer = "..\TeamViewerQS.exe"

$entryAssembly = resolve-path (join-path -path "$source" -childpath "mainboss.exe")
#Determine the version from the entryAssembly
#Note this will busy the entryAssembly until power shell terminates since .NET doesn't allow you unload an assembly until the AppDomain is unloaded.
#TODO: We need to load the assembly in its own AppDomain ? and fish the information out of there.
$assembly = [System.Reflection.Assembly]::LoadFile($entryAssembly.path)
$assemblyName = $assembly.GetName().name
$version = $assembly.GetName().version
$supportVersion = [System.String]::Format("{0}.{1}", $version.Major, $version.Minor)
$productVersion = [System.String]::Format("{0}.{1}.{2}", $version.Major, $version.Minor, $version.Build)
$assembly = $null #unload the assembly
#$version = $args[0]
if ([System.String]::IsNullOrEmpty($version))
{
	write-host 'Version is missing! Quitting'
	return
}
$productName = "MainBoss"
$supportUrlBase = "http://www.mainboss.com/info"
$supportUrl = "$supportUrlBase/support.htm?version=$supportVersion"
$microsoftSupportUrl = "$supportUrlBase/microsoft.htm"

#Filenames we build & other properties
$applicationManifestFileName = "$assemblyName.exe.manifest"
$deploymentManifestFileName = "$assemblyName.application"
#Old thumbprint for  Thinkage Certificate
#$signingCertificateThumbPrint = "10b2896f51f68f2bd676e71eb6949e15b7df38f6"
#GoDaddy Code Signing Certificate
#$signingCertificateThumbPrint = "c254bb1717015f1fa38f174663f58ebda6c5b7c3"
$signingCertificateThumbPrint = "0072e09a760bfac04e5cd4f5a26abc34a1c0072b"
#################################################################################################
#Clean out previous build, and make new Package structure
rd -r -force "Installation" -ErrorAction SilentlyContinue
md "Installation" | out-null
md "Installation\Application Files" | out-null

$DebugFiles = "PDB_ClickOnce"+$version
rd -r -force $DebugFiles -ErrorAction SilentlyContinue
md "$DebugFiles" | out-null

$AppFiles = join-path -path "Application Files" -childpath ("$assemblyName" + "_" + $version.ToString().Replace(".","_"))
$PackageAppFiles = join-path -path "Installation" -childpath $AppFiles
md "$PackageAppFiles" | out-null

#$PackageHelp = join-path -path $PackageAppFiles -childpath "help"
#md "$PackageHelp" | out-null

#Copy constant resource files that need to be included in application manifest
copy Resources\AppIcon.ico $PackageAppFiles
$documentation = "..\HtmlHelp\en-us"
#Setup the executable files for mainboss .exe and resource DLLS associated

write-host "Copy .exe files"
xcopy /q /s "$source\*.exe" $PackageAppFiles
xcopy /q /s "$source\*.exe.config" $PackageAppFiles
xcopy /q /s /i "$source\www\*.*" "$PackageAppFiles\www"
write-host "Copy .dll files"
xcopy /q /s "$source\*.dll" $PackageAppFiles
rd -r -force "$PackageAppFiles\app.publish" #remove any remnant of Visual Studio publishing build files
#remove assemblies that have the same identify given to us by Microsoft for ReportViewer.
rd -r -force "$PackageAppFiles\zh-CHT"
rd -r -force "$PackageAppFiles\zh-CHS"
pushd $PackageAppFiles
	get-childitem -recurse "." -include '*.exe' | foreach-object {
		ThinkageCodeSign $_.FullName
	}
	get-childitem -recurse "." -include '*.dll' | foreach-object {
		ThinkageCodeSign $_.FullName
	}
popd
xcopy /q $teamviewer $PackageAppFiles
write-host "Copy .pdb files"
xcopy /q /s "$source\*.pdb" "$DebugFiles"
#write-host "Copy help files"
#xcopy /q /s "$documentation\*.*" $PackageHelp
#remove MainBoss.Service.exe as it isn't part of the ClickOnce install (and it eliminates our problem with MAGE picking it as the entry point for the deployment)
#get-childitem -recurse "$PackageAppFiles\*" -include "Thinkage.MainBoss.Service.*" | remove-item
#get-childitem -recurse "$PackageAppFiles\*" -include "Thinkage.MainBoss.ManageMainBossService.*" | remove-item
######################### APPLICATION Manifest ###################################
$applicationManifest = join-path -path $PackageAppFiles -childpath $applicationManifestFileName
write-host "Maging $version"
&$magetool -new Application -ToFile "$applicationManifest" -FromDirectory $PackageAppFiles -Algorithm sha256RSA -Name $productName -Version $version -Publisher "Thinkage" -SupportURL "$supportUrl" -UseManifestForTrust true -IconFile AppIcon.ico

#modify the application manifest to include dependencies (from Resources\Dependencies.xml file) that mage doesn't add
$manifest = new-object System.Xml.XmlDocument
$manifest.load( (resolve-path $applicationManifest) )
#update the osVersionInfo
$dependentSupportUrl = $manifest.CreateAttribute("supportUrl")
$dependentSupportUrl.set_Value("$microsoftSupportUrl") | out-null
$manifest.assembly.dependency[0].dependentOS.SetAttributeNode($dependentSupportUrl) | out-null
[System.Xml.XmlNode] $osInfo = $manifest.assembly.dependency[0].dependentOS.osVersionInfo.os
$b4 =[System.String]::Format("{0}.{1}.{2}.{3}", $osInfo.majorVersion,$osInfo.minorVersion,$osInfo.buildNumber,$osInfo.servicePackMajor)
$osInfo.majorVersion = "5"
$osInfo.minorVersion = "1"
$osInfo.buildNumber = "2600"
$osInfo.servicePackMajor = "0"
$x =[System.String]::Format("{0}.{1}.{2}.{3}", $osInfo.majorVersion,$osInfo.minorVersion,$osInfo.buildNumber,$osInfo.servicePackMajor)
write-host "OS Info changed from" $b4 " to " $x
#mage uses the Product argument for both assemblyIdentify name= attribute whereas Visual Studio Publish uses the entry point assembly name instead; change that here
[System.Xml.XmlNode] $assemblyIdentity = $manifest.assembly.assemblyIdentity
$assemblyIdentity.name="$assemblyName.exe"
SaveXML "$applicationManifest" $manifest
&$magetool -sign "$applicationManifest" -CertHash $signingCertificateThumbPrint -Algorithm sha256RSA -timestampUri "http://timestamp.verisign.com/scripts/timestamp.dll"
#################### DEPLOYMENT manifest #########################################
$deploymentManifest = join-path -path "$PackageAppFiles" -childpath $deploymentManifestFileName
$AppCodeBase = join-path -path $AppFiles -childpath $applicationManifestFileName
&$magetool -new Deployment -ToFile "$deploymentManifest" -Publisher "Thinkage" -Algorithm sha256RSA -Name "$productName" -Version $version -Install true -AppManifest "$applicationManifest"  -AppCodeBase "$AppCodeBase" -UseManifestForTrust true -SupportURL "$supportUrl"
#mage doesn't work with -MinVersion on above command line so add it in a second step
&$magetool -update "$deploymentManifest" -MinVersion "$version" -Publisher "Thinkage" -Algorithm sha256RSA
#modify the deployment manifest wrt Expiry dates since MAGE doesn't provide a means to do so on the command line.
[System.Xml.XmlDocument] $manifest = new-object System.Xml.XmlDocument
$manifest.load( (resolve-path $deploymentManifest) )
[System.Xml.XmlNode] $updateNode = $manifest.assembly.deployment.subscription.update
$updateNode.RemoveAll()
$updateNode.AppendChild($manifest.CreateElement("beforeApplicationStartup", "urn:schemas-microsoft-com:asm.v2")) | out-null
$trustUrlParameters = $manifest.CreateAttribute("trustURLParameters")
$trustUrlParameters.set_Value("true") | out-null
$manifest.assembly.deployment.SetAttributeNode($trustUrlParameters) | out-null
$manifest.assembly.assemblyIdentity.name = "mainboss.application"
[System.xml.XmlNode] $compatibleFrameworks = $manifest.assembly.compatibleFrameworks
[System.Xml.XmlNode]$framework = $compatibleFrameworks.ChildNodes[0]
$framework.targetVersion = "4.6"
$framework.profile="Full"
$framework.supportedRuntime="4.0.30319"
$compatibleFrameworks.RemoveAll() #remove the ones mage assumes
$compatibleFrameworks.AppendChild($framework) | out-null

# Save the updated manifest
SaveXML "$deploymentManifest" $manifest
#Sign the mangled deployment manifest
&$magetool -sign "$deploymentManifest" -CertHash $signingCertificateThumbPrint -Algorithm sha256RSA -timestampUri "http://timestamp.verisign.com/scripts/timestamp.dll"
#################### WEB Page and prerequisite Setup ##############################
#Add the web page and setup file
copy $deploymentManifest Installation
$applicationUrl = $deploymentManifestFileName + "?/hmp:http://www.mainboss.com/english/manual/"+$productVersion+"/HtmlHelp"
get-content Resources\DeployWebPage.htm | % {$_ -replace("PRODUCTNAME","$productName")} | % {$_ -replace("VERSION_TO_SUBSTITUTE","$version")} | % {$_ -replace("SUPPORT_URL","$supportUrl")} | % {$_ -replace("APPLICATION_URL_WITH_ARGUMENTS","$applicationURL")} | set-content Installation\default.htm
copy Resources\logo.gif Installation
Import-module Pscx
write-zip Installation\* Install.MainBoss.ClickOnce.$version.zip
write-host 'BuildClickOnce Complete'
pop-location
