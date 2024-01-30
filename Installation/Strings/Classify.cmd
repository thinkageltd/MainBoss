call setup.cmd
pushd "%SRC%\a"
rd /q /s output
md output
"%TOOLS%\StringClassifier" /input:mainboss.exe /input:mainbossSolo.exe /input:Thinkage.MainBoss.Service.exe /input:mbutility.exe /input:Thinkage.MainBoss.MainBossServiceConfiguration.exe /input:Thinkage.Libraries.MVC.dll /output:output /fnpp:"^[Cc]:.*[Tt]hinkage[\\\\]" +sd
popd
