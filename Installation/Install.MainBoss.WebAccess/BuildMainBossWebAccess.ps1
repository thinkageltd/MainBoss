#And we need the PLATFORMSDK bin directory in our path using the NETFX 4.0 tools
$MSDOTNET =  join-path -path $env:WINDIR -childpath(join-path -path "Microsoft.Net" -childpath (join-path -path "Framework" -childpath "v4.0.30319"))
$VISUALSTUDIO = join-path -path "${env:ProgramFiles(x86)}" -childpath(join-path -path "Microsoft Visual Studio" -childpath (join-path -path "2017" -childpath "Enterprise"))
$MSBUILD = join-path -path "$VISUALSTUDIO" -childpath(join-path -path "MSBuild" -childpath (join-path "15.0" -childpath(join-path "bin" -childpath  "msbuild.exe")))
$ASPCOMPILER = join-path -path "$MSDOTNET" -childpath "aspnet_compiler.exe"
####################################################################
$project = "Thinkage.MainBoss.WebAccess"
. ..\..\SolutionSettings.ps1
$version = $SolutionVersion
if ([System.String]::IsNullOrEmpty($version))
{
	write-host 'Version is missing! Quitting'
	return
}
$msbuilddir = join-path -path (get-location) -childpath "msbuild"
rd -r -force $msbuilddir
md $msbuilddir | out-null

$outputdir = join-path -path (get-location) -childpath "installation"
rd -r -force $outputdir
md $outputdir | out-null

$pdbdir = join-path -path (get-location) -childpath ("PDB_WebAccess"+$version)
rd -r -force $pdbdir 
md $pdbdir | out-null

$tempdir = join-path -path $env:TEMP -childpath "MainBossWebAccessPackage"
rd -r -force $tempdir
md $tempdir | out-null

#following is the actual root path of msbuild's package
$packagedir = join-path -path $tempdir -childpath "PackageTmp"

$archivedir = join-path -path "C:\" -childpath "MainBossWebAccessArchive"
rd -r -force $archivedir
md $archivedir | out-null

$packagefile = join-path -path $msbuilddir -childpath ($project+".zip")

push-location (join-path -path ".." -childpath (join-path -path ".." -childpath $project))
$SolutionDir = (resolve-path "..") 
$SolutionDir = $SolutionDir.ToString() + "\\"

&$MSBUILD ($project+".csproj") /t:Package /p:SolutionDir=$SolutionDir /p:Configuration=Release /p:PackageFileName=$packagefile /p:PackageTempRootDir=$TempDir /p:PackageArchiveRootDir=$archivedir /p:PackageAsSingleFile=true

xcopy bin\*.pdb ($pdbdir) /a /s /i

# NUGETPACKAGES assumes the Microsoft.AspNet.Merge package has been installed; it may still be a prerelease if you need to find it in nuget.
$NUGETPACKAGES = "$SolutionDir\packages\Microsoft.AspNet.Merge.5.0.0-beta2\tools\net40"
$ASPMERGE = join-path -path "$NUGETPACKAGES" -childpath "aspnet_merge.exe"
#Make a precompiled variant
&$ASPCOMPILER -v "/Thinkage.MainBoss.WebAccess" -p "$packagedir" "$outputdir" -c
&$ASPMERGE "$outputdir" -o MainBossWebAccess.dll -a

pop-location
Import-module Pscx
del installation\Thinkage.MainBoss.WebAccess.wpp.targets
del -r installation -include *.pdb
write-zip installation/* Install.MainBoss.WebAccess.$version.zip
