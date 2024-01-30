#And we need the PLATFORMSDK bin directory in our path using the NETFX 4.0 tools
#$env:path = $env:PLATFORMSDK + '\bin\NETFX 4.6 Tools' + ';' + $env:path

#write-host $env:path

$magetoolRoot = $null
if ($env:PLATFORMSDK -ne $null) {
	$magetoolRoot = $env:PLATFORMSDK
}
else {
	throw "PLATFORMSDK environment variable must be set to use MAGE tools"
}
$magetool = join-path -path "$magetoolRoot" -childpath (join-path -path 'bin' (join-path -path 'NETFX 4.6 Tools' -childpath 'mage.exe'))
. (resolve-path '..\SignProcedure.ps1')

$BuildDir = "BuildDir"
$teamviewer = "..\TeamViewerQS.exe"
write-host $env:path

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
$ConfigurationName = $args[0]
$source = "..\..\Thinkage.MainBoss.MainBossSolo\bin\$ConfigurationName"
$wwwSource = "..\..\Thinkage.MainBoss.MainBossSolo\www"
$entryAssembly = resolve-path (join-path -path "$source" -childpath "mainbossSolo.exe")
#Determine the version from the entryAssembly
$assembly = [System.Reflection.Assembly]::LoadFile($entryAssembly.path)
$assemblyName = $assembly.GetName().name
$version = $assembly.GetName().version
$supportVersion = [System.String]::Format("{0}.{1}", $version.Major, $version.Minor)
$productVersion = [System.String]::Format("{0}.{1}", $version.Major, $version.Minor)
$attribute = $assembly.GetCustomAttributes([System.Reflection.AssemblyProductAttribute], $false)
$productName = ([System.Reflection.AssemblyProductAttribute]$attribute[0]).Product + " " + $productVersion
$assembly = "" #unload the assembly
if ([System.String]::IsNullOrEmpty($version))
{
	write-host 'Version is missing! Quitting'
	return
}
$supportUrlBase = "http://www.mainboss.com/info"
$supportUrl = "$supportUrlBase/mainbosssolo.shtml?version=$supportVersion"
$microsoftSupportUrl = "$supportUrlBase/microsoft.shtml"

#Filenames we build & other properties
$applicationManifestFileName = "$assemblyName.exe.manifest"
$deploymentManifestFileName = "$assemblyName.application"
#GoDaddy Code Signing Certificate
$signingCertificateThumbPrint = "c254bb1717015f1fa38f174663f58ebda6c5b7c3"
#################################################################################################
#Clean out previous build, and make new Package structure
rd -r -force "$BuildDir" -ErrorAction SilentlyContinue
md "$BuildDir" | out-null
md "$BuildDir\ApplicationFiles" | out-null

$DebugFiles = "PDB"+$version
rd -r -force $DebugFiles -ErrorAction SilentlyContinue
md "$DebugFiles" | out-null

$AppFiles = join-path -path "ApplicationFiles" -childpath ("$assemblyName" + "_" + $version)
$PackageAppFiles = join-path -path "$BuildDir" -childpath $AppFiles
md "$PackageAppFiles" | out-null

#$PackageHelp = join-path -path $PackageAppFiles -childpath "help"
#md "$PackageHelp" | out-null

#Copy constant resource files that need to be included in application manifest
copy Resources\AppIcon.ico $PackageAppFiles
#Setup the executable files for mainboss .exe and resource DLLS associated
write-host "Building $productName ($assemblyName): $ConfigurationName $version"
write-host "Copy .exe files"
xcopy /q /s "$source\*.exe" $PackageAppFiles
xcopy /q /s "$source\*.exe.config" $PackageAppFiles
write-host "Copy .dll files"
xcopy /q /s "$source\*.dll" $PackageAppFiles
write-host "Copy .pdb files"
xcopy /q /s "$source\*.pdb" "$DebugFiles"
write-host "Copy help files"
#remove MainBoss.Service.exe as it isn't part of the ClickOnce install (and it eliminates our problem with MAGE picking it as the entry point for the deployment)
get-childitem -recurse "$PackageAppFiles\*" -include "Thinkage.MainBoss.Service.*" | remove-item
get-childitem -recurse "$PackageAppFiles\*" -include "Thinkage.MainBoss.ManageMainBossService.*" | remove-item
pushd $PackageAppFiles
	get-childitem -recurse "." -include '*.exe' | foreach-object {
		ThinkageCodeSign $_.FullName
	}
	get-childitem -recurse "." -include '*.dll' | foreach-object {
		ThinkageCodeSign $_.FullName
	}
popd
xcopy /q $teamviewer $PackageAppFiles
rd -r -force "$PackageAppFiles\app.publish" #remove any remnant of Visual Studio publishing build files
######################### APPLICATION Manifest ###################################
$applicationManifest = join-path -path $PackageAppFiles -childpath $applicationManifestFileName
write-host "Maging $productName $version"
&$magetool  -new Application -ToFile "$applicationManifest" -FromDirectory $PackageAppFiles -Name $productName -Version $version -Publisher "Thinkage" -SupportURL "$supportUrl" -UseManifestForTrust true -IconFile AppIcon.ico

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
$assemblyIdentity.name="$assemblyName"+$productVersion+".exe"
SaveXML "$applicationManifest" $manifest

&$magetool  -sign "$applicationManifest" -CertHash $signingCertificateThumbPrint -Algorithm sha256RSA -timestampUri "http://timestamp.verisign.com/scripts/timestamp.dll"
#################### DEPLOYMENT manifest #########################################
$deploymentManifest = join-path -path "$PackageAppFiles" -childpath $deploymentManifestFileName
$AppCodeBase = join-path -path $AppFiles -childpath $applicationManifestFileName
&$magetool  -new Deployment -ToFile "$deploymentManifest" -Publisher "Thinkage" -Algorithm sha256RSA -Name "$productName" -Version $version -Install true -AppManifest "$applicationManifest"  -AppCodeBase "$AppCodeBase" -UseManifestForTrust true
#mage doesn't work with -MinVersion on above command line so add it in a second step
&$magetool  -update "$deploymentManifest" -MinVersion "$version" -Publisher "Thinkage" -Algorithm sha256RSA
#modify the deployment manifest wrt Expiry dates since MAGE doesn't provide a means to do so on the command line.
[System.Xml.XmlDocument] $manifest = new-object System.Xml.XmlDocument
$manifest.load( (resolve-path $deploymentManifest) )
[System.Xml.XmlNode] $updateNode = $manifest.assembly.deployment.subscription.update
$updateNode.RemoveAll()
$updateNode.AppendChild($manifest.CreateElement("beforeApplicationStartup", "urn:schemas-microsoft-com:asm.v2"))
$trustUrlParameters = $manifest.CreateAttribute("trustURLParameters")
$trustUrlParameters.set_Value("true")
$manifest.assembly.deployment.SetAttributeNode($trustUrlParameters)
[System.xml.XmlNode] $compatibleFrameworks = $manifest.assembly.compatibleFrameworks
[System.Xml.XmlNode]$framework = $compatibleFrameworks.ChildNodes[0]
$framework.targetVersion = "4.6"
$framework.profile="Full"
$framework.supportedRuntime="4.0.30319"
$compatibleFrameworks.RemoveAll() #remove the ones mage assumes
$compatibleFrameworks.AppendChild($framework) | out-null
SaveXML "$deploymentManifest" $manifest
#Sign the mangled deployment manifest
&$magetool  -sign "$deploymentManifest" -CertHash $signingCertificateThumbPrint -Algorithm sha256RSA -timestampUri "http://timestamp.verisign.com/scripts/timestamp.dll"
#################### WEB Page and prerequisite Setup ##############################
#Add the web page and setup file
copy $deploymentManifest $BuildDir

$x = new-object -type System.Version -Argumentlist $version
$v = $x.Major.ToString() + "." + $x.Minor.ToString() + "." + $x.Build.ToString()
xcopy /q /s /I "$wwwSource\Scripts\*" $BuildDir\Scripts
xcopy /q /s /I "$wwwSource\version\*.*" $BuildDir\$v
xcopy /q /I "$wwwSource\.htaccess" $BuildDir
xcopy /q /I "$wwwSource\default.htm" $BuildDir
#The bootstrapper variants for web.thinkage.ca and www.mainboss.com
xcopy /q "Resources\setup_for_solo*.exe" $BuildDir
write-host 'BuildClickOnce MainBoss Solo Complete'
pop-location
