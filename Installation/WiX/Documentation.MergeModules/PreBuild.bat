@ECHO ON
rem goto :exit
SET ConfigurationName=%1
SET SolutionDir=%2
SET PROJECTNAME=%3
rem Following should be same as $(var.BuildDir) defined in Common.wxi
SET BuildDir=BuildDir
rem Help files are named same as our projectName
SET SRCDIR=%SolutionDir%Installation\HtmlHelp\%PROJECTNAME%

pushd ..\..
rd %BuildDir% /s /q
md %BuildDir%
cd
rem Copy all
xcopy "%SRCDIR%\*.*" %BuildDir%
"..\..\WixToolPath\heat.exe" dir %BuildDir% -var var.%BuildDir% -dr INSTALLDIR -gg -t ..\..\WixToIncludeNoFragment.xsl -srd -sfrag -nologo -out .\HelpFiles.wxs
popd
:exit
