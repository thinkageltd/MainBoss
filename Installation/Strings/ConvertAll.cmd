call setup.cmd

call RefreshAssemblies.cmd
call Classify.cmd
call RightSetTranslations.cmd
@echo on

call helptopics.cmd "..\..\Thinkage.MainBoss.MainBoss\HelpTopics.xml"
del %SRC%a\output\FormatQualifiers.*.xml /f /q

call resxconversion.cmd "Thinkage.Libraries," "..\..\Thinkage.Libraries"
call resxconversion.cmd Thinkage.Libraries.MVC
call resxconversion.cmd Thinkage.Libraries.DBAccess
call resxconversion.cmd "Thinkage.Libraries.DBILibrary," "..\..\Thinkage.Libraries.DBILibrary"
call resxconversion.cmd Thinkage.Libraries.DBILibrary.MSSql
call resxconversion.cmd "Thinkage.Libraries.Presentation," "..\..\Thinkage.Libraries.Presentation"
call resxconversion.cmd Thinkage.Libraries.Presentation.MSWindows
call resxconversion.cmd Thinkage.Libraries.RDLReports
call resxconversion.cmd Thinkage.Libraries.Sql
call resxconversion.cmd Thinkage.Libraries.XAF.Database.Layout
call resxconversion.cmd "Thinkage.Libraries.XAF.Database.Service," "..\..\Thinkage.Libraries.XAF.Database.Service"
call resxconversion.cmd Thinkage.Libraries.XAF.Database.Service.MSSql
call resxconversion.cmd "Thinkage.Libraries.XAF.UI," "..\..\Thinkage.Libraries.XAF.UI"
call resxconversion.cmd Thinkage.Libraries.XAF.UI.MSWindows

call resxconversion.cmd Thinkage.MainBoss.Application
call resxconversion.cmd Thinkage.MainBoss.Controls
call resxconversionWithRightSet.cmd Thinkage.MainBoss.Database
call resxconversion.cmd "mainboss," "..\..\Thinkage.MainBoss.MainBoss"
call resxconversion.cmd Thinkage.MainBoss.MainBossServiceConfiguration
call resxconversion.cmd MBUtility "..\..\Thinkage.MainBoss.MBUtility"
call resxconversion.cmd "Thinkage.MainBoss.Service," "..\..\Thinkage.MainBoss.Service"

call AnalyzeFormatQualifiers.cmd
:end
