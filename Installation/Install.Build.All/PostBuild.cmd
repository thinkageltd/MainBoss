@echo off
rem Establish the default location of MSI manipulation tools
PATH "%PLATFORMSDK%\BIN";%PATH%
rem assumes is invoked with arguments #1=%VERSION% #2=%BuildDir%, #3=%BUILTOUTPUTPATH% and #4=%CONFIGURATION%
setlocal
set VERSION=%1
set BuildDir=%2
set BuiltOutputPath=%3
set Configuration=%4
rem Goto Project directory and apply changes
pushd %BuildDir%
rem -----------------------------------------------------------------------------------------------------------------------------
@echo on
call .\CopyToReleases.cmd %VERSION%
@echo off
popd
