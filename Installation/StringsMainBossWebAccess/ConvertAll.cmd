call setup.cmd

call RefreshAssemblies.cmd
call Classify.cmd

@echo on
pushd "%SRC%\a\output"
powershell.exe -noprofile -command "&{ (get-content (resolve-path \"ContextStrings.xml\")) | foreach-object {$_ -replace \"SingleAssembly\", \"Thinkage.MainBoss.WebAccess\"} | set-content \"ContextStrings.xml\" }"
popd

call resxconversion.cmd Thinkage.MainBoss.WebAccess

:end
