@echo on
setlocal
"%VAULTCLIENT%" %1 -repository Thinkage -host admin.thinkage.ca -ssl -user kadorken -password "i376C29!vault" %2
endlocal
