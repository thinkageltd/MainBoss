@echo on
rem assumes is invoked with arguments #1=%VERSION% #2=%BuildDir%, #3=%BUILTOUTPUTPATH% and #4=%CONFIGURATION%
setlocal

rem ---------- Compile the base version for %ConfigurationName% ------------------------------------
rem pushd ..\..
rem "%DevEnvDir%\devenv.com" "%SolutionFileName%" /project Thinkage.MainBoss.MainBossSolo /build "%ConfigurationName%"
rem popd
rem --------------------------------Build the MainBoss Solo ClickOnce version ----------------------------------------------------------
@echo off
pushd ..
powershell -NoProfile -File .\BuildMainBossSolo.ps1 "%ConfigurationName%"
popd