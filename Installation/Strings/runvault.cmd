@echo on
setlocal
"%VAULTCLIENT%" %1 -repository Thinkage -host admin.thinkage.ca -ssl -user kadorken -password "%3" %2
endlocal
