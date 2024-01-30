call setup.cmd
pushd "%SRC%"
rd a /s /q
md a
pushd ..\..
"%msbuild%" MainBoss.sln /verbosity:normal /ignoreprojectextensions:.wixproj /t:Rebuild /p:Configuration=Release /p:DefineConstants="DEFINELABELKEYS"
popd
xcopy /y "..\..\Thinkage.Libraries.MVC\bin\release\*.*" a
xcopy /y "..\..\Thinkage.MainBoss.MainBossSolo\bin\release\*.*" a
xcopy /y "..\..\Thinkage.MainBoss.MainBoss\bin\release\*.*" a
xcopy /y "..\..\Thinkage.MainBoss.MBUtility\bin\release\mbutility.*" a
rem part of mainboss.exe release tree xcopy /y "..\..\Thinkage.MainBoss.Service\bin\release\Thinkage.MainBoss.Service*.*" a
xcopy /y "..\..\Thinkage.MainBoss.Database\bin\release\Dart.*" a
xcopy /y "..\..\Thinkage.MainBoss.MainBossServiceConfiguration\bin\release\Thinkage.MainBoss.MainBossServiceConfiguration.*" a
popd
