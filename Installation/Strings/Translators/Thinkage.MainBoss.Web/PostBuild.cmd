@echo on
rem Do common PostBuild operations. This is a COMMON PostBuild file that is shared amongst all assemblies
rem and called from the PostBuild event. It will call any CustomPostBuild.cmd file located in the project directory
rem FIRST, then end by authenticode signing the resulting Target.
rem THIS ASSUMES THE local environment variable PLATFORMSDK is set to the directory where a Microsoft .NET Platform SDK
rem has been installed (typically C:\Program Files\Microsoft SDKs\Windows\v6.1)
rem If not set, it will synthesize one to the (older) one installed with Visual Studio 2008
IF "%CONFIGURATIONNAME%" NEQ "Installation" goto NoSdkCheck
IF /I "%PLATFORMSDK%" NEQ "" goto NoSdkCheck
echo PlatformSdk Environment variable must be set for Installation configuration.
SET ERRORLEVEL 1
goto Leave
:NoSdkCheck
IF /I "%PLATFORMSDK%" == "" SET PLATFORMSDK=%VS90COMNTOOLS%..\..\SDK\v3.5
PATH "%PLATFORMSDK%\Bin";%Path%

if EXIST "%ProjectDir%CustomPostBuild.cmd" call "%ProjectDir%CustomPostBuild.Cmd"
IF ERRORLEVEL 1 goto CustomError

rem Authenticode Sign the target for release. Assumes local MY cryptostore contains the necessary Verisign key.
if "%CONFIGURATIONNAME%" NEQ "Installation" goto Leave
if EXIST "%TARGETDIR%en\%TargetName%.resources.dll" SET RESOURCES="%ProjectDir%obj\%ConfigurationName%\en\%TargetName%.resources.dll" "%ProjectDir%obj\%ConfigurationName%\fr\%TargetName%.resources.dll" "%ProjectDir%obj\%ConfigurationName%\es\%TargetName%.resources.dll" "%TARGETDIR%en\%TargetName%.resources.dll" "%TARGETDIR%fr\%TargetName%.resources.dll"
set BINOUTPUT="%TARGETDIR%%TARGETFILENAME%"
set OBJOUTPUT="%PROJECTDIR%OBJ\%CONFIGURATIONNAME%\%TARGETFILENAME%"
rem Because MSBUILD process is so confused, and copies files from OBJ to BIN AND does not invoke the PostBuild process (if nothing changed in the OBJ .exe, it still copies it)
signtool sign  /t http://timestamp.verisign.com/scripts/timestamp.dll /n "Thinkage Ltd." /i "VeriSign Class 3 Code Signing 2004 CA" %OBJOUTPUT% %BINOUTPUT% %RESOURCES%
IF ERRORLEVEL 0 goto :EOF
echo Failed to Sign output.
goto Leave

:CustomError
echo CustomPostBuild Processing Errors

:Leave
rem Debug versions are forgiving on PostBuild.cmd errors ..... Release versions however will exit with last ErrorLevel
if NOT "%CONFIGURATIONNAME%" == "Release" exit /b 0
