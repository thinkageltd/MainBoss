call setup.cmd
if '%1' neq '' goto check2
echo Need vault password to proceed as argument 1
goto end
:check2
call runvault.cmd checkout  %VAULTROOT%/Thinkage.MainBoss.MainBoss/HelpTopics.xml %1
copy "..\..\Thinkage.MainBoss.MainBoss\HelpTopics.xml.NEW" "..\..\Thinkage.MainBoss.MainBoss\HelpTopics.xml"

call UpdateOne.cmd Thinkage.Libraries %1
call UpdateOne.cmd Thinkage.Libraries.DBAccess  %1
call UpdateOne.cmd Thinkage.Libraries.MVC  %1
call UpdateOne.cmd Thinkage.Libraries.Presentation  %1
call UpdateOne.cmd Thinkage.Libraries.Presentation.MSWindows  %1
call UpdateOne.cmd Thinkage.Libraries.RDLReports %1
call UpdateOne.cmd Thinkage.Libraries.Sql %1
call UpdateOne.cmd Thinkage.Libraries.XAF.Database.Layout %1
call UpdateOne.cmd Thinkage.Libraries.XAF.Database.Service %1
call UpdateOne.cmd Thinkage.Libraries.XAF.Database.Service.MSSql %1
call UpdateOne.cmd Thinkage.Libraries.XAF.UI %1
call UpdateOne.cmd Thinkage.Libraries.XAF.UI.MSWindows %1

call UpdateOne.cmd Thinkage.MainBoss.Application %1
call UpdateOne.cmd Thinkage.MainBoss.Controls %1
call UpdateOne.cmd Thinkage.MainBoss.Database %1
call UpdateOne.cmd Thinkage.MainBoss.MainBoss %1
call UpdateOne.cmd Thinkage.MainBoss.MainBossServiceConfiguration %1
call UpdateOne.cmd Thinkage.MainBoss.MBUtility %1
call UpdateOne.cmd Thinkage.MainBoss.Service %1
:end


