using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.Libraries.XAF.UI;
using Thinkage.Libraries.XAF.UI.MSWindows;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Application {
	#region UpgraderApplication
	public delegate Thinkage.Libraries.Application ReturnToApplicationCreator();
	public class UpgraderApplication : Thinkage.Libraries.Application {
		public UpgraderApplication(string organizationName, DBClient.Connection connection, ReturnToApplicationCreator returnToApplication) {
			new ApplicationExecutionWithMainFormCreatorDelegate(this,
				delegate () {
					return new MainForm(GetInterface<UIFactory>(), Upgrader, returnToApplication);
				});
			// Copy the help parameters from the app that is creating us.
			HelpUsingFolderOfHtml.CopyFromOtherApplication(this, Thinkage.Libraries.Application.Instance);
			Upgrader = new DBUpgrader(MBUpgrader.UpgradeInformation, organizationName, connection, Thinkage.MainBoss.Database.Licensing.DeemedReleaseDate, dsUpgrade_1_1_4_2.Schema);
		}
		protected override void CreateUIFactory() {
#if DEBUG
			new MSWindowsDebugProvider(this, KB.I("MainBoss Debug Form"));
#endif
			new StandardApplicationIdentification(this, ApplicationParameters.RegistryLocation, "MainBoss");
			new GUIApplicationIdentification(this, "Thinkage.MainBoss.Application.Resources.UpgradeApplication.ico");
			new Thinkage.Libraries.XAF.UI.MSWindows.UserInterface(this);
			new Thinkage.Libraries.Presentation.MSWindows.UserInterface(this);
			new MSWindowsUIFactory(this);
		}
		public override void TeardownApplication(Thinkage.Libraries.Application nextApplication) {
			Upgrader.CloseSession();
			base.TeardownApplication(nextApplication);
		}
		private DBUpgrader Upgrader;
		#region the main form
		// TODO: add a Cancel (if Upgrade is enabled) or Close (if disabled) button which closes the form (ideally calling up the Pick Organization app).
		// Have the X box ask are you sure if the upgrade has not been done.
		// What about permissions checking? This should happen in the ctor so a user who can't upgrade also can't lock the DB exclusively forever.
		private class MainForm : TblForm<UIPanel> {
			readonly DBUpgrader Upgrader;
			readonly ReturnToApplicationCreator ReturnToApplication;
			public MainForm(UIFactory uiFactory, DBUpgrader upgrader, ReturnToApplicationCreator returnToApplication)
				: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
				Upgrader = upgrader;
				ReturnToApplication = returnToApplication;
				SetupControls(uiFactory);
			}
			void AddCentered(UILabel control) {
				control.HorizontalAlignment = System.Drawing.StringAlignment.Center;
				FormContents.Add(control);
			}
			private void SetupControls(UIFactory uiFactory) {
				PanelHelper.SetColumnPanelGaps(FormContents, 5, 5);

				AddCentered(uiFactory.CreateLabel(KB.T(Strings.Format(KB.K("MainBoss {0}"), Upgrader.BasicDBConnection.DisplayNameLowercase))));
				AddCentered(uiFactory.CreateLabel(KB.K("While this form is open other MainBoss users and services will be unable to access this database")));
				CurrentVersionLabel = uiFactory.CreateLabel(null);
				AddCentered(CurrentVersionLabel);
				// Get the index of the latest server upgrade step available for the current server version.
				int currentServerVersionIndex = DBVersionInformation.GetServerVersionUpgradersIndex(Upgrader.BasicSqlDB.ServerVersion) - 1;
				if (currentServerVersionIndex >= 0)
					AddCentered(uiFactory.CreateLabel(KB.T(Strings.Format(KB.K("The latest database version is {0} tailored for SQL Server version {1}"),
														MBUpgrader.UpgradeInformation.LatestDBVersion,
														DBVersionInformation.ServerVersionUpgraders[currentServerVersionIndex].ServerVersion))));
				else
					// older Sql servers, pre 9.0.0.0 (Sql Server 2005), which predate any server upgrade steps.
					AddCentered(uiFactory.CreateLabel(KB.T(Strings.Format(KB.K("The latest database version is {0}"),
														MBUpgrader.UpgradeInformation.LatestDBVersion))));
				AddCentered(uiFactory.CreateLabel(KB.T(Strings.Format(KB.K("Your current SQL Server version is {0}"), Upgrader.BasicSqlDB.ServerVersion))));
				AddCentered(uiFactory.CreateLabel(KB.K("Upgrading this database may prevent older MainBoss services\r\nand users of older MainBoss applications from accessing this database")));
				if (Upgrader.WillExpireCount > 0)
					AddCentered(uiFactory.CreateLabel(KB.K("Some of the License Keys that were valid for earlier versions of MainBoss\r\nwill not be valid for this version")));

				var buttonRow = PanelHelper.NewCenterRowPanel(uiFactory);
				PanelHelper.SetRowPanelGaps(buttonRow, 5, 5);
				FormContents.Add(buttonRow);
				buttonRow.Add(uiFactory.CreateButton(KB.K("Upgrade"),
					new MultiCommandIfAllEnabled(
						new CallDelegateCommand(KB.K("Upgrade this database to the latest version and optimize for the current SQL Server version"),
#if !ASYNC
							delegate()
#else
							async delegate()
#endif
				{
					if (Upgrader.WillExpireCount > 0
						&& Ask.Question(KB.K("MainBoss will require new license keys after the upgrade.\r\nDo you want to continue with the upgrade?").Translate()) == Ask.Result.No)
						return;
#if !ASYNC
					IProgressDisplay ipdo = uiFactory.CreateProgressDisplay(KB.K("Upgrading MainBoss maintenance organization to current version"), 0, true);
					try {
						Upgrader.BasicDB.PerformTransaction(false, delegate ()
						{
							Upgrader.UpgradeToCurrentVersion(ipdo);
							Upgrader.UpgradeServerVersion(ipdo);
							ipdo.Update(KB.K("Setting built-in MainBoss security."), 1);
							using (dsMB ds = new dsMB(Upgrader.BasicDB)) {
								SecurityCreation.CreateSecurityDataSet(ds, null, SecurityCreation.RightSetLocation);
								ds.DB.Update(ds, ServerExtensions.UpdateOptions.NoConcurrencyCheck);
							}
							if (Upgrader.BasicDB.Session.EffectiveDBServerVersion >= new System.Version(10, 0, 0, 0)) { // only for server 2008 and higher; sorry 2005 server ...
								ipdo.Update(KB.K("Updating Database Index Statistics."), 1);
								var sqlCommand = new Thinkage.Libraries.XAF.Database.Service.MSSql.MSSqlLiteralCommandSpecification(KB.I("EXEC sp_updatestats"));
								System.Text.StringBuilder output = new System.Text.StringBuilder();
								try {
									Upgrader.BasicDB.Session.ExecuteCommand(sqlCommand, output); // we don't examine output
								}
								catch (System.Exception e) {
									GeneralException ne = new GeneralException(e, KB.K("The upgrade has completed but the update of the database statistics has failed.  A SQL sysadmin should update the MainBoss database statistics to ensure improved performance."));
									Thinkage.Libraries.Application.Instance.DisplayError(Thinkage.Libraries.Exception.FullMessage(ne));
								}
							}
						});
						UpdateDisplay();
					}
					finally {
						ipdo.Complete();
					}
#else
					// This code requires the Upgrader.UpgradeToCurrentVersion to accept both a DBClient and Cancellation token to be passed.
					UIExecuteAsync<int, IProgressDisplay> pf = uiFactory.CreateProgressDisplayAsync<int, IProgressDisplay>(
						KB.K("Upgrading MainBoss maintenance organization to current version"), true, true,
						(System.IProgress<IProgressDisplay> rP, System.Threading.CancellationToken cT, object[] pList) =>
						{
							StepByStepProgressReport reportProgress = new StepByStepProgressReport(0, rP);
							DBClient upgradeDB = new DBClient(Upgrader.BasicDBConnection, Upgrader.BasicDB.Session);
							upgradeDB.PerformTransaction(false, delegate()
							{
								Upgrader.UpgradeToCurrentVersion(reportProgress, upgradeDB, cT);
								Upgrader.UpgradeServerVersion(reportProgress, upgradeDB);
								reportProgress.Update(KB.K("Setting built-in MainBoss security."), 1);
								using (dsMB ds = new dsMB(upgradeDB)) {
									SecurityCreation.CreateSecurityDataSet(ds, null, SecurityCreation.RightSetLocation);
									ds.DB.Update(ds, Server.UpdateOptions.NoConcurrencyCheck);
								}
							});
							return 0;
						});
					try {
						int result = await pf.ExecuteAsync();
						UpdateDisplay();
					}
					catch (System.AggregateException) {
						Thinkage.Libraries.Application.Instance.DisplayMessage(KB.K("Upgrading MainBoss maintenance organization cancelled by user").Translate());
					}
#endif
				}),
						AlreadyUpgradedDisabler)));
				buttonRow.Add(uiFactory.CreateButton(KB.K("Close"),
						new CallDelegateCommand(KB.K("Close this form and return to the previous application"), delegate() {
					onlyonce++;
					ReplaceActiveApplication(ReturnToApplication());
				})));
#if DEBUG
				// Allow debugging to reset the security rights directly without need to add fake upgrade step to cause it
				buttonRow.Add(uiFactory.CreateButton(KB.T("DEBUG: Reset MainBoss Security"),
					new CallDelegateCommand(KB.T("DEBUG: Reset MainBoss Security"), delegate() {
					Upgrader.BasicDB.PerformTransaction(false, delegate() {
						using (dsMB ds = new dsMB(Upgrader.BasicDB)) {
							SecurityCreation.CreateSecurityDataSet(ds, null, SecurityCreation.RightSetLocation);
							ds.DB.Update(ds, ServerExtensions.UpdateOptions.NoConcurrencyCheck);
						}
					});
				})));
#endif
				UpdateDisplay();
			}
			int onlyonce = 0;
			protected override void OnClosing(FormClosingEventArgs e) {
				if (onlyonce++ == 0)
					ReplaceActiveApplication(ReturnToApplication());
				base.OnClosing(e);
			}
			private readonly SettableDisablerProperties AlreadyUpgradedDisabler = new SettableDisablerProperties(null, KB.K("The database is already at the latest version and tailored for your current SQL Server version"), true);
			private UILabel CurrentVersionLabel;
			private void UpdateDisplay() {
				AlreadyUpgradedDisabler.Enabled = Upgrader.CurrentDBVersion < MBUpgrader.UpgradeInformation.LatestDBVersion
					|| DBVersionInformation.GetServerVersionUpgradersIndex(Upgrader.CurrentDBServerVersion) < DBVersionInformation.GetServerVersionUpgradersIndex(Upgrader.BasicSqlDB.ServerVersion);
				CurrentVersionLabel.Caption = KB.T(Strings.Format(KB.K("This database's current version is {0} tailored for SQL Server version {1}"), Upgrader.CurrentDBVersion, Upgrader.CurrentDBServerVersion));
			}
			public override string GetHelpContext() {
				return Thinkage.Libraries.Presentation.KB.HelpTopicKey("Main.Upgrade");
			}
		}
		#endregion
	}
	#endregion
}
