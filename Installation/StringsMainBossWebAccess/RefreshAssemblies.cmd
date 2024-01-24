call setup.cmd
pushd "%SRC%"
rd a /s /q
rd compiled /s /q
rd package /s /q
md a
md compiled
md package
xcopy "..\..\Thinkage.MainBoss.WebAccess\obj\release\Package\PackageTmp\*.*" /s package
if %ERRORLEVEL% LSS 1 goto okay
echo ***************************************************************************************************************
echo obj/release/Package/PackageTmp missing; rerun Release Build Deployment Package (Publish to StringBuilding project)
echo ***************************************************************************************************************
goto :end
:okay
@echo on
%MSDOTNET%\aspnet_compiler -v "/Thinkage.MainBoss.WebAccess" -p "%SRC%package" "%SRC%compiled" -c -nologo
"%NETFX%\aspnet_merge" "%SRC%compiled" -o SingleAssembly.dll -nologo 
xcopy "%SRC%compiled\bin\*.dll" a
@echo off
:end
popd
