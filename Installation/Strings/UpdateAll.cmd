call setup.cmd

call runvault.cmd checkout  %VAULTROOT%/Thinkage.MainBoss.MainBoss/HelpTopics.xml
copy "..\..\Thinkage.MainBoss.MainBoss\HelpTopics.xml.NEW" "..\..\Thinkage.MainBoss.MainBoss\HelpTopics.xml"

call UpdateOne.cmd Thinkage.Libraries
call UpdateOne.cmd Thinkage.Libraries.DBAccess
call UpdateOne.cmd Thinkage.Libraries.DBILibrary
call UpdateOne.cmd Thinkage.Libraries.DBILibrary.MsSql
call UpdateOne.cmd Thinkage.Libraries.MVC
call UpdateOne.cmd Thinkage.Libraries.Presentation
call UpdateOne.cmd Thinkage.Libraries.Presentation.MSWindows
call UpdateOne.cmd Thinkage.Libraries.RDLReports
call UpdateOne.cmd Thinkage.Libraries.Sql
call UpdateOne.cmd Thinkage.Libraries.XAF.Database.Layout
call UpdateOne.cmd Thinkage.Libraries.XAF.Database.Service
call UpdateOne.cmd Thinkage.Libraries.XAF.Database.Service.MSSql
call UpdateOne.cmd Thinkage.Libraries.XAF.UI
call UpdateOne.cmd Thinkage.Libraries.XAF.UI.MSWindows

call UpdateOne.cmd Thinkage.MainBoss.Application
call UpdateOne.cmd Thinkage.MainBoss.Controls
call UpdateOne.cmd Thinkage.MainBoss.Database
call UpdateOne.cmd Thinkage.MainBoss.MainBoss
call UpdateOne.cmd Thinkage.MainBoss.MainBossServiceConfiguration
call UpdateOne.cmd Thinkage.MainBoss.MBUtility
call UpdateOne.cmd Thinkage.MainBoss.Service


