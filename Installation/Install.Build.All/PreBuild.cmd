@echo off
rem Establish the default location of MSI manipulation tools
PATH "%PLATFORMSDK%\BIN";%PATH%
rem assumes is invoked with arguments #1=%VERSION% #2=%BuildDir%, #3=%BUILTOUTPUTPATH% and #4=%CONFIGURATION%
setlocal
set VERSION=%1
set BuildDir=%2
set BuiltOutputPath=%3
set Configuration=%4
set DevEnvDir=%VS100COMNTOOLS%..\IDE

set SolutionFile=mainboss.sln

pushd %BuildDir%
rem ---------- Compile the base version for %Configuration% ------------------------------------
pushd ..\..
"%DevEnvDir%\devenv.com" "%SolutionFile%" /project Thinkage.MainBoss.MainBoss /build "%Configuration%"
"%DevEnvDir%\devenv.com" "%SolutionFile%" /project Thinkage.MainBoss.MainBossSolo /build "%Configuration%"
"%DevEnvDir%\devenv.com" "%SolutionFile%" /project Thinkage.MainBoss.Service /build "%Configuration%"
popd

rem -------------------------------------------------- ********************** ----------------------------------
rem goto :SkipWixThingsThatDontWork
rem -------------------------------------------------- ********************** ----------------------------------

rem --------------------------------Build the MainBoss Install -------------------------------------------------
rem NOTE: to use devenv.com you must give it the mainboss.sln file AND the project file for the install
@echo on
pushd ..\Wix\Install.MainBoss
"%DevEnvDir%\devenv.com" "..\..\..\%SolutionFile%" /project Install.MainBoss /build "%Configuration%"
popd
rem --------------------------------Build the MainBossService Install ------------------------------------------
rem NOTE: to use devenv.com you must give it the mainboss.sln file AND the project file for the install
@echo on
pushd ..\Wix\Install.MainBossService
"%DevEnvDir%\devenv.com" "..\..\..\%SolutionFile%" /project Install.MainBossService /build "%Configuration%"
popd

:SkipWixThingsThatDontWork

rem --------------------------------Build the ClickOnce version ------------------------------------------------
@echo off
pushd ..\Install.ClickOnce
powershell -NoProfile -File .\BuildClickOnce.ps1 "%VERSION%"
popd
rem --------------------------------Build the MainBoss Solo ClickOnce version ----------------------------------------------------------
@echo off
pushd ..\Install.MainBossSolo
powershell -NoProfile -File .\BuildMainBossSolo.ps1 "%VERSION%"
popd
rem --------------------------------Build the MainBoss Remove version -------------------------------------------
pushd ..\Install.MainBossRemote
powershell -NoProfile -File .\BuildMainBossRemote.ps1 "%VERSION%"
popd
@echo off
popd
