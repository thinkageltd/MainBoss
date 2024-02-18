# Build ALL aspects of the MainBoss product; Note that the IDE must be used FIRST to do the dependency compilation of the modules. This is a work in progress and not working
$VISUALSTUDIO = join-path -path "${env:ProgramFiles(x86)}" -childpath(join-path -path "Microsoft Visual Studio" -childpath (join-path -path "2017" -childpath "Enterprise"))
$MSBUILD = join-path -path "$VISUALSTUDIO" -childpath(join-path -path "MSBuild" -childpath (join-path "15.0" -childpath(join-path "bin" -childpath  "msbuild.exe")))
# Set the 'macros' set by Visual Studio during IDE builds that we depend on elsewhere
$SolutionDir = join-path -path (get-location) -child ""
# Project specific macros
$ProjectName = "Install.MainBoss"
pushd Installation\$ProjectName
$ProjectDir = join-path -path (get-location) -child ""
#configuration specific
$Configuration = "Release"
#&$MSBUILD "$ProjectName.wixproj" /verbosity:normal /p:Configuration=$Configuration /p:SolutionDir=$SolutionDir /p:ProjectDir=$ProjectDir /p:ProjectName=$ProjectName
popd
pushd Installation\Install.ClickOnce
powershell -noprofile .\BuildClickOnce.ps1
popd
pushd Installation\Install.MainBoss.WebAccess
powershell -noprofile .\BuildMainBossWebAccess.ps1
popd
# Following will fail if not running as Administrator
pushd Installation\DesktopApp
powershell -noprofile .\BuildDesktopApp.ps1
popd
#
#Now copy the files to Release
