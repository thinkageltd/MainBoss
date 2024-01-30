param([string]$SolutionDir, [string]$ProjectName, [string]$Configuration)
# Following should be same as $(var.BuildDir) defined in Common.wxi
$ScriptRoot = Split-path $MyInvocation.MyCommand.Path
pushd $ScriptRoot

$BuildDir="BuildDir"
# Help files are named same as our projectName
$SRCDIR=(join-path -path $SolutionDir -childpath (join-path -path $ProjectName -childpath (join-path -path "bin" -childpath $Configuration)))
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
if ([System.String]::IsNullOrEmpty($version))
{
	write-host 'Version is missing! Quitting'
	return
}

rd $BuildDir -r -force | out-null
md $BuildDir

$PDBDIR="PDB_"+$version
rd $PDBDIR -r -force | out-null
md $PDBDIR

# Must Copy all the produced files in the target projects build directory, then prune out stuff we KNOW shouldn't be there. YECH
"Copying from `$SRCDIR" | out-host
xcopy (join-path -path "$SRCDIR" -childpath "*.*") /s /e /q $BuildDir
"Segregating PDB files" | out-host
move (join-path -path $BuildDir -childpath "*.PDB") $PDBDIR
pushd $BuildDir
del *.vshost.*
rd -r -force app.publish
if( $Configuration -eq "Release") {
	get-childitem -recurse "." -include '*.exe' | foreach-object {
		ThinkageCodeSign $_.FullName
	}
	get-childitem -recurse "." -include '*.dll' | foreach-object {
		ThinkageCodeSign $_.FullName
	}
}
# copy the TeamViewer support executable NOW that we have signed all our stuff
"Copying Teamviewer from $TEAMVIEWER to BuildDir" | out-host
copy $TEAMVIEWER $TEAMVIEWER_FILE

# copy the help files now
md help
md help\es
"Copying English help files" | out-host
xcopy "$HELPFILES\en\*.*" /q help
"Copying Spanish help files" | out-host
xcopy "$HELPFILES\es\*.*" /q help\es
popd

"Running heat" | out-host
& "$WixToolPath\heat.exe" dir $BuildDir -var var.$BuildDir -dr INSTALLDIR -ag -cg Executables -t ..\Wix\WixToInclude.xsl -srd -sfrag -sreg -nologo -out .\Executables.wxs
popd
"Done PreBuild" | out-host
