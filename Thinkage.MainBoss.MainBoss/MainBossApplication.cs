using System;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.XAF.UI;
using Thinkage.Libraries.XAF.UI.MSWindows;
using Thinkage.MainBoss.Application;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;
#if DEBUG
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.DBILibrary;
#endif

namespace Thinkage.MainBoss.MainBoss {

	public class MainBossApplication : TblDrivenMainBossApplication {
		#region Mode definitions for Tbl-driven Apps
		/// <summary>
		/// The set of licenses to feature groups for MainBoss defined once and cloned as required in the ModeDefinitions table.
		/// </summary>
		internal static LicenseEnabledFeatureGroups[] LicensedFeatureGroups = new LicenseEnabledFeatureGroups[] {
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.WorkOrderLaborLicense) }, TIGeneralMB3.WorkOrderLaborLicenseGroup),
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.RequestsLicense, overLimitFatal:false) }, TIGeneralMB3.RequestsLicenseGroup),
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.MainBossServiceLicense) }, TIGeneralMB3.MainBossServiceAsWindowsServiceLicenseGroup),
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.InventoryLicense, overLimitFatal: false) }, TIGeneralMB3.InventoryLicenseGroup),
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.AccountingLicense) }, TIGeneralMB3.AccountingLicenseGroup),
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.PurchasingLicense) }, TIGeneralMB3.PurchasingLicenseGroup),
			new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.ScheduledMaintenanceLicense) }, TIGeneralMB3.ScheduledMaintenanceLicenseGroup)
		};
		/// <summary>
		/// The modes for the MainBoss and related utility applications
		/// </summary>
		internal static ModeDefinition[] Modes {
			get {
				// This table has a lazy getter to prevent a whole bunch of types from being elaborated before the
				// StartupApplication ctor returns (thus setting Application.Instance)
				// Note that entry zero in the table is special insofar as it is the default Mode used when valid DB connection info exists
				// but no mode is specified on the command line and there is no default organization.
				if (pModes == null)
					pModes = new ModeDefinition[] {
						new ModeDefinition("MainBoss Maintenance Manager", (int)DatabaseEnums.ApplicationModeID.Normal, KB.K("Start MainBoss with all permitted licenses."),
							dsMB.Schema.V.MinMBAppVersion, new Version(1, 1, 5, 2),// changing this should be reflected in the checkin comment to vault
							new [] {
								new [] {new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true)}
							},
							((LicenseEnabledFeatureGroups[])LicensedFeatureGroups.Clone()).Concat(new [] {new LicenseEnabledFeatureGroups(TIGeneralMB3.MainBossModeGroup, TIGeneralMB3.CoreLicenseGroup)}).ToArray()
						),
						new ModeDefinition("MainBoss Assignments", (int)DatabaseEnums.ApplicationModeID.Assignments, KB.K("Start MainBoss showing only user's assignments."),
							// This is a simplified mode for users to see only Work Orders, Requests, and/or Purchase Orders which are assigned to them.
							dsMB.Schema.V.MinMBAppVersion, new Version(1, 1, 5, 2), // changing this should be reflected in the checkin comment to vault
							new [] {
								new [] {new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true)}
							},
							((LicenseEnabledFeatureGroups[])LicensedFeatureGroups.Clone()).Concat(new [] {new LicenseEnabledFeatureGroups(TIGeneralMB3.AssignmentsModeGroup, TIGeneralMB3.CoreLicenseGroup)}).ToArray()
						),
						new ModeDefinition("MainBoss Requests", (int)DatabaseEnums.ApplicationModeID.Requests, KB.K("Start only MainBoss Requests"),
							// This is a simplified mode only for users to enter Requests and monitor requests they have entered (and are thus part of their Assigned requests)
							dsMB.Schema.V.MinMBAppVersion, new Version(1, 1, 5, 2), // changing this should be reflected in the checkin comment to vault
							new[] {
								new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true), new Libraries.Licensing.LicenseRequirement(Licensing.RequestsLicense, overLimitFatal: true) }
							},
							((LicenseEnabledFeatureGroups[])LicensedFeatureGroups.Clone()).Concat(new [] {new LicenseEnabledFeatureGroups(TIGeneralMB3.RequestsModeGroup, TIGeneralMB3.CoreLicenseGroup, TIGeneralMB3.RequestsLicenseGroup)}).ToArray()
						),
						// Amininstration mode provides a simplified access to enter (with no licenses) Licenses, Users, and Security, and (with appropriate licenses) Requestors and MB Service administration.
						new ModeDefinition("MainBoss Database Administration", (int)DatabaseEnums.ApplicationModeID.Administration, KB.K("Start the MainBoss Administration program."),
							dsMB.Schema.V.MinMBAppVersion, new Version(1, 1, 4, 11), // ServiceConfiguration changed in 1.1.4.11 // changing this should be reflected in the checkin comment to vault
							new Libraries.Licensing.LicenseRequirement[] { },
							((LicenseEnabledFeatureGroups[])LicensedFeatureGroups.Clone()).Concat(new [] {new LicenseEnabledFeatureGroups(TIGeneralMB3.AdministrationModeGroup)}).ToArray()
						),
						new ModeDefinition("MainBoss Database Sessions", (int)DatabaseEnums.ApplicationModeID.Sessions, KB.K("View the MainBoss sessions currently in use."),
							dsMB.Schema.V.MinMBAppVersion, new Version(1, 0, 4, 15), // User tables and views changed at 1.0.4.15 // changing this should be reflected in the checkin comment to vault
							new Libraries.Licensing.LicenseRequirement[] { },
							new LicenseEnabledFeatureGroups(TIGeneralMB3.SessionsModeGroup)
						)
					};
				return pModes;
			}
		}
		private static ModeDefinition[] pModes;
		#endregion

		#region Constructors
		private MainBossApplication(ModeDefinition mode, NamedOrganization o)
			: base(mode, o, supportsPersonalSettings: true) {
		}
		protected override void CreateUIFactory() {
#if DEBUG
			new MSWindowsDebugProvider(this, KB.I("MainBoss Debug Form"));
			MSWindowsDebugProvider.AddPushbutton(MSWindowsDebugProvider.GeneralCategory, "Check Table Info",
				delegate (object sender, System.EventArgs e) {
					TIGeneralMB3.TableInfoDebug.CheckTableInfo(new ControlPanelLayout().OverallContent());
				}
			);
#endif
			new StandardApplicationIdentification(this, "MainBoss", "MainBoss");
			new GUIApplicationIdentification(this, "Thinkage.MainBoss.MainBoss.Resources.MainBoss400.ico");
			new Thinkage.Libraries.XAF.UI.MSWindows.UserInterface(this);
			new Thinkage.Libraries.Presentation.MSWindows.UserInterface(this);
			new MSWindowsUIFactory(this);
			// Note that the derived App has the option of doing the DBSession setup in its Ctor or in its SetupApplication.
			// The former is used for built-in app objects called from the startup application or Pick Organization, where one wants
			// the ctor to fail if the DB connection cannot be made.
			// The latter is used for a standalone MB app, where failed DB access must be properly reported and thus where this must
			// occur in SetupApplication (as does the command-line options parsing).
			new ApplicationExecutionWithMainFormCreatorDelegate(this,
				delegate () {
					return CreateMainForm((MB3Client)GetInterface<IApplicationWithSingleDatabaseConnection>().Session);
				});
#if DEBUG
			MSWindowsDebugProvider.AddPushbutton(MSWindowsDebugProvider.GeneralCategory, "Test Relative Date control",
				delegate (object sender, System.EventArgs e) {
					new RelativeDateControlTestForm(GetInterface<UIFactory>()).ShowForm();
					;
				}
			);
#endif
		}
		internal static Thinkage.Libraries.Application CreateMainBossApplication(NamedOrganization o) {
			ModeDefinition pAppModeDefinition = null;
			foreach (ModeDefinition m in MainBossApplication.Modes)
				if (m.AppMode == (int)o.MBConnectionParameters.ApplicationMode) {
					pAppModeDefinition = m;
					break;
				}
			if (pAppModeDefinition == null)
				return null;
			return new MainBossApplication(pAppModeDefinition, o);
		}
		protected override FormProxy CreateMainForm(MB3Client db) {
			return MainBossMainForm.New(GetInterface<UIFactory>(), db);
		}
		public override Libraries.Application CreateSetupApplication() {
			return new PickOrganizationApplication();
		}
		#endregion
	}
	public class MainBossMainForm : MainBossDataForm {

		protected override IContainableMenuItem BuildSessionMenu() {
			return UIFactory.CreateSubMenu(KB.K("Session"), null
				// TODO: Make the following disabled when more than one window is open.
				, UIFactory.CreateCommandMenuItem(KB.K("Change Maintenance Organization"), new CallDelegateCommand(KB.K("Close this organization and switch to a different organization"), new EventHandler(this.SetupDatabase)))
				, UIFactory.CreateCommandMenuItem(KB.T(Strings.Format(KB.K("Reset to user {0} security"), DB.Session.ConnectionInformation.UserIdentification)), new CallDelegateCommand(KB.K("Security roles will be set back to the logged in user name"),
					delegate () {
						var appobj = MainBossApplication.Instance.AppConnectionMixIn;
						// The following does not attempt to re-evaluate the User record ID, which may have changed if the original logon was with a scope-less
						// record and a record now exists with a non-null scope that matches the current windows user.
						// If we were to do this we would also want to alter the "real" appobj.UserRecordID.
						appobj.SetUserRecordIDForPermissionsAndLoadPermissions(appobj.UserRecordID, applyAdminAccess: true);
					}
				))
#if DEBUG
, UIFactory.CreateMenuSeparator()
				, UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Restart on same database"), new CallDelegateCommand(KB.K("Close this organization and restart to same database"), new EventHandler(delegate (object sender, EventArgs args) {
					bool compactBrowsers = !TblDrivenMainBossApplication.Instance.GetInterface<IFormsPresentationApplication>().BrowsersHavePanel;
					var junk = new NamedOrganization(Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().OrganizationName, new MB3Client.MBConnectionDefinition(DatabaseEnums.ApplicationModeID.Normal, compactBrowsers, DB.ConnectionInfo.DBServer, DB.ConnectionInfo.DBName, DB.ConnectionInfo.DBCredentials));
					var app = MainBossApplication.CreateMainBossApplication(junk);
					Thinkage.Libraries.Application.ReplaceActiveApplication(app);
				})))
				, UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Select View Cost Permissions"), new CallDelegateCommand(KB.T("DefaultVisibility"), new EventHandler(delegate (object sender, EventArgs args) {
					new PickViewCostPermissions(UIFactory, UpdateSecurity()).ShowAppModalAndWait();
					MainControlPanel.RefreshTreeViewMenu();
				})))
				, UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Select View Cost Roles"), new CallDelegateCommand(KB.T("DefaultVisibility"), new EventHandler(delegate (object sender, EventArgs args) {
					new PickViewCostRoles(UIFactory, UpdateSecurity()).ShowAppModalAndWait();
					MainControlPanel.RefreshTreeViewMenu();
				})))
				, UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Select Roles"), new CallDelegateCommand(delegate () {
					new PickRoles(UIFactory, UpdateSecurity()).ShowAppModalAndWait();
					MainControlPanel.RefreshTreeViewMenu();
				}))
				, UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Select from existing roles"), new CallDelegateCommand(delegate () {
					new PickLiveRoles(UIFactory).ShowAppModalAndWait();
					MainControlPanel.RefreshTreeViewMenu();
				}))
#endif
, UIFactory.CreateMenuSeparator()
				, ExitOrCloseMenuItem()
				);
		}
		#region Construction
		static public MainBossMainForm New(UIFactory uiFactory, MB3Client db) {
			MainBossMainForm result = new MainBossMainForm(uiFactory, db);
			result.MainControlPanel.SetInitialState(null);
			return result;
		}
		protected override MainBossDataForm CopySelf() {
			MainBossMainForm result = new MainBossMainForm(UIFactory, DB);
			// Set InitialPreferredSize to existing size of form we are duplicating.
			SetupMainBossForm(result);
			return result;
		}
		/// <summary>
		/// Determine what to display on caption, status bar, and the control panel root name
		/// </summary>
		/// <param name="organizationName"></param>
		/// <param name="dbOrganizationName"></param>
		/// <param name="userName"></param>
		/// <returns>The string to use for the root of the ControlPanel</returns>
		protected override void SetStatusAndCaption(string organizationName, string dbOrganizationName, string userName) {
			// TODO: The following comment, which was transplanted from MainBossDataForm.
			// Check if the OrganizationName stored in the database differs from the one the User gave us; if so tack on the 'real' organization name to the identification
			var statusText = new System.Text.StringBuilder();
			statusText.Append(Strings.Format(KB.K("User {0}"), userName));
			statusText.Append(" ");
			if (!string.IsNullOrEmpty(organizationName)) {
				statusText.Append(Strings.Format(KB.K("Organization {0}"), organizationName));
				statusText.Append(" ");
			}
			statusText.Append("(");
			if (!string.IsNullOrEmpty(dbOrganizationName) && dbOrganizationName != organizationName)
				statusText.Append(Strings.IFormat("{0}: ", dbOrganizationName));
			statusText.Append(DB.ConnectionInfo.DisplayName);
			statusText.Append(")");
			StatusBarStatusDisplay.Value = statusText.ToString();
			// Window title shows organization as part of title in form used commonly by Microsoft products 'object - application' (e.g. Outlook)
			if (!string.IsNullOrEmpty(organizationName))
				Caption = KB.T(Strings.IFormat("{0} - {1}", organizationName, MainBossApplication.Instance.ApplicationFullName));
			else if (!string.IsNullOrEmpty(dbOrganizationName))
				Caption = KB.T(Strings.IFormat("{0} - {1}", dbOrganizationName, MainBossApplication.Instance.ApplicationFullName));
			else
				Caption = KB.T(MainBossApplication.Instance.ApplicationFullName); // only the application if no organization
		}
		protected MainBossMainForm(UIFactory uiFactory, MB3Client db)
			: base(uiFactory, db) {
		}
		#endregion
	}
#if DEBUG
	#region Test form
	public class RelativeDateControlTestForm : TblForm<UIPanel> {
		public RelativeDateControlTestForm(UIFactory uiFactory)
			: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
			FormContents.Add(uiFactory.CreateLabel(KB.T("Test Input")));
			InputControl = new RelativeDateControl(uiFactory, null, null, ValuePlaceholder);
			FormContents.Add(InputControl);
			FormContents.Add(uiFactory.CreateLabel(KB.T("Reinterpreted value")));
			RedisplayControl = new RelativeDateControl(uiFactory, null, new SettableDisablerProperties(null, null, false), ValuePlaceholder);
			FormContents.Add(RedisplayControl);
			FormContents.Add(uiFactory.CreateLabel(KB.T("Expression")));
			ExpressionControl = uiFactory.CreateTextDisplay(StringTypeInfo.Universe.GetTypeFormatter(Libraries.Application.Instance.FormatCultureInfo), null, textPreferences: new TextSizePreference() { MaxPreferredLineCount = 4, DefaultLineCount = 4 });
			FormContents.Add(ExpressionControl);
			FormContents.Add(uiFactory.CreateLabel(KB.T("Sample SQL expression")));
			SQLExpressionControl = uiFactory.CreateTextDisplay(StringTypeInfo.Universe.GetTypeFormatter(Libraries.Application.Instance.FormatCultureInfo), null, textPreferences: new TextSizePreference() { MaxPreferredLineCount = 4, DefaultLineCount = 4 });
			FormContents.Add(SQLExpressionControl);
			FormContents.Add(uiFactory.CreateLabel(KB.T("If today were")));
			TodayControl = uiFactory.CreateDateTimePicker(DateTimeTypeInfo.NullableOneDayEpsilon, null, null);
			FormContents.Add(TodayControl);
			FormContents.Add(uiFactory.CreateLabel(KB.T("Result would be")));
			ResultControl = uiFactory.CreateTextDisplay(DateTimeTypeInfo.NullableOneDayEpsilon.GetTypeFormatter(Libraries.Application.Instance.FormatCultureInfo), null);
			FormContents.Add(ResultControl);

			InputControl.Notify += TextInputChange;
			TodayControl.Notify += TodayChange;

			// Handle the initial values of the input controls.
			TextInputChange();
		}
		private readonly SqlExpression ValuePlaceholder = new SqlExpression(dsMB.Path.T.WorkOrder.F.WorkDueDate);
		private readonly IDataControl InputControl;
		private readonly IDataControl RedisplayControl;
		private readonly UITextDisplay ExpressionControl;
		private readonly UITextDisplay SQLExpressionControl;
		private readonly IDataControl TodayControl;
		private readonly IDataControl ResultControl;
		private readonly Libraries.DBILibrary.MSSql.MSSqlUnparser Unparser = new Libraries.DBILibrary.MSSql.MSSqlUnparser(null);
		private void TextInputChange() {
			if (InputControl.ValueStatus != null) {
				RedisplayControl.Value = null;
				SQLExpressionControl.Value = Libraries.Exception.FullMessage(InputControl.ValueStatus);
				ExpressionControl.Value = null;
			}
			else {
				var expr = (SqlExpression)InputControl.Value;
				RedisplayControl.Value = expr;
				SQLExpressionControl.Value = Unparser.UnParseCondition(expr);
				ExpressionControl.Value = expr.DebugText;
			}

			TodayChange();
		}
		private void TodayChange() {
			if (InputControl.ValueStatus != null || TodayControl.ValueStatus != null)
				ResultControl.Value = null;
			else {
				var expr = (SqlExpression)InputControl.Value;
				var today = (DateTime?)TodayControl.Value;
				if (expr == null || !today.HasValue)
					ResultControl.Value = null;
				else {
					expr = expr.TraverseWithReplacement(((node, depth) => node.Op == SqlExpression.OpName.Now ? SqlExpression.Constant(today) : null), 0);
					ResultControl.Value = expr.Evaluate();
				}
			}
		}
	}
	#endregion
#endif
}
