param([string]$SolutionDir, [string]$ProjectName, [string]$Configuration)
# Following should be same as $(var.BuildDir) defined in Common.wxi
$ScriptRoot = Split-path $MyInvocation.MyCommand.Path
pushd $ScriptRoot

$BuildDir="BuildDir"
# Help files are named same as our projectName
$SRCDIR=(join-path -path $SolutionDir -childpath (join-path -path $ProjectName -childpath (join-path -path "bin" -childpath $Configuration)))
if ($Configuration -eq "Release"){
	$packages = "$ProjectName", "Thinkage.MainBoss.Service", "Thinkage.MainBoss.MainBossServiceConfiguration", "Thinkage.MainBoss.MBUtility"
}
elseif( $Configuration -eq "Desktop"){
	$packages = "$ProjectName"
}
else{
	write-host "Unsupported Configuration $Configuration"
	return
}
#auxilary programs that are in release version (not desktop version)
$HELPFILES=(join-path -path $SolutionDir -childpath (join-path "Installation" -childpath "HtmlHelp"))
$TEAMVIEWER_FILE="TeamViewerQS.exe"
$TEAMVIEWER=(join-path -path $SolutionDir -childpath (join-path "Installation" -childpath $TEAMVIEWER_FILE))
$WixToolPath=(resolve-path -path "..\WiX\WixToolPath")

. (resolve-path '..\SignProcedure.ps1')

#TODO: We need to load the assembly in its own AppDomain ? and fish the information out of there.
$entryAssembly=(Join-Path -Path $SRCDIR -ChildPath "mainboss.exe")
$assembly = [System.Reflection.Assembly]::LoadFile($entryAssembly)
$version = $assembly.GetName().version
$assembly = $null #unload the assembly
Write-Host 'Entry Assembly version is ' $version
if ([System.String]::IsNullOrEmpty($version)) {
	write-host 'Version is missing! Quitting'
	return
}
if ((Test-Path $BuildDir -PathType Container)) {
	remove-item $BuildDir -r -force | out-null
}
md $BuildDir | out-null

# Must Copy all the produced files in the target projects build directory, then prune out stuff we KNOW shouldn't be there. YECH
foreach( $package in $packages ){
	$x=(join-path -path $SolutionDir -childpath (join-path -path $package -childpath (join-path -path "bin" -childpath $Configuration)))
	"Copying from $x" | out-host
	xcopy (join-path -path "$x" -childpath "*.*") /s /e /q /y /d $BuildDir
}
#remove chaff files
pushd $BuildDir
remove-item *.vshost.*
remove-item -r -force app.publish
#remove assemblies that have the same identify given to us by Microsoft for ReportViewer.
remove-item -r -force zh-CHT
remove-item -r -force zh-CHS
popd
#Get the PDB files
"Segregating PDB files" | out-host
$PDBDIR="PDB_"+$Configuration+$version
if ((Test-Path $PDBDIR -PathType Container)) {
	remove-item -r -force $PDBDIR
}
new-item -ItemType Directory -Force -Path $PDBDIR | out-null
move (join-path -path $BuildDir -childpath "*.PDB") $PDBDIR

#code sign all the executables and dlls
pushd $BuildDir
get-childitem -recurse "." -include '*.exe' | foreach-object {
	ThinkageCodeSign $_.FullName
}
get-childitem -recurse "." -include '*.dll' | foreach-object {
	ThinkageCodeSign $_.FullName
}
get-childitem -recurse "." -include '*.ps1' | foreach-object {
	ThinkageCodeSign $_.FullName
}
# copy the TeamViewer support executable NOW that we have signed all our stuff
"Copying Teamviewer from $TEAMVIEWER to BuildDir" | out-host
copy $TEAMVIEWER $TEAMVIEWER_FILE

# copy the help files now
md help | out-null
md help\es | out-null
"Copying English help files" | out-host
xcopy "$HELPFILES\en\*.*" /q help
"Copying Spanish help files" | out-host
xcopy "$HELPFILES\es\*.*" /q help\es
popd

"Running heat" | out-host
& "$WixToolPath\heat.exe" dir $BuildDir -var var.$BuildDir -dr INSTALLDIR -ag -cg Executables -t ..\Wix\WixToInclude.xsl -srd -sfrag -sreg -nologo -out .\Executables.wxs
popd
"Done PreBuild" | out-host
