@echo off
call setup.cmd

if '%1' neq '' goto check2
echo Need Project identification for Vault containing TranslationResources\messages ...
goto end
:check2
if '%2' neq '' goto check3
echo Need vault password
goto end
:check3
set passwd=%2
set convertoutput=..\..\%1\TranslationResources
set repositoryPath=%VAULTROOT%/%1/TranslationResources
call runvault.cmd checkout %repositoryPath% %passwd%
@echo on
for %%i in (en,fr,es) do copy %convertoutput%\UPDATES\merged.%%i.resx %convertoutput%\messages.%%i.resx 
copy %convertoutput%\UPDATES\merged.en.resx %convertoutput%\messages.resx 
@echo off
rem call runvault.cmd checkin %repositoryPath% %passwd%
:end
