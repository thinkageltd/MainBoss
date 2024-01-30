@echo on
setlocal
"%VAULTCLIENT%" %1 -repository Thinkage -host vault.thinkage.ca -user kadorken -password "" %2
endlocal
