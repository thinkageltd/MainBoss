rem Copy all the files over to the release directory and build ZIP files from the files copied there
setlocal
set DESTDIR=\\dc\mainboss\releases
set VERSION=%1
set BUILDDIR=%DESTDIR%\%VERSION%
set PACKAGES=Install.ClickOnce Install.MainBossRemote Install.MainBossSolo
set WIXPACKAGES=Install.MainBoss Install.MainBossService

if EXIST "%BUILDDIR%" (
	echo %BUILDDIR% already exists; stopping Copy
	goto:eof
)
mkdir %BUILDDIR%
mkdir %BUILDDIR%\PDB

for %%i in (%WIXPACKAGES%) do (
	mkdir %BUILDDIR%\%%i
	mkdir %BUILDDIR%\PDB\%%i
	pushd ..\Wix
	xcopy /s %%i\bin\Installation\*	%BUILDDIR%\%%i
	xcopy    %%i\PDBDir\*			%BUILDDIR%\PDB\%%i
	popd
	@echo on
	powershell -NoProfile .\MakeZipInstalls.ps1 %BUILDDIR% %VERSION% "%%i"
)

for %%i in (%PACKAGES%) do (
	mkdir %BUILDDIR%\%%i
	mkdir %BUILDDIR%\PDB\%%i
	pushd ..
	xcopy /s %%i\Installation\*		%BUILDDIR%\%%i
	xcopy    %%i\PDB%VERSION%\*		%BUILDDIR%\PDB\%%i
	popd
	@echo on
	powershell -NoProfile .\MakeZipInstalls.ps1 %BUILDDIR% %VERSION% "%%i"
)
