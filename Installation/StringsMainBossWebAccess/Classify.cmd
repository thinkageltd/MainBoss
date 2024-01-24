call setup.cmd
pushd "%SRC%\a"
rd /q /s output
md output
"%TOOLS%\StringClassifier" /input:SingleAssembly.dll  /output:output /fnpp:"^[Cc]:.*[Tt]hinkage[\\\\]" +sd
popd
