// Uncomment following to enable conversion using SQL scripts (enables command in Add Organization list)
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.XAF.UI;
using Thinkage.Libraries.XAF.UI.MSWindows;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MainBoss {
	using Libraries.TypeInfo;
	using System;
	using System.Collections.Generic;
	using Thinkage.Libraries.Collections;
	using Thinkage.Libraries.DBAccess;
	using Thinkage.Libraries.DBILibrary;
	using Thinkage.Libraries.DBILibrary.MSSql;
	using Thinkage.Libraries.Licensing;
	using Thinkage.Libraries.Presentation;
	using Thinkage.Libraries.Translation;
	using Thinkage.MainBoss.Application;
	using Thinkage.MainBoss.Controls;
	public class PickOrganizationApplication : Thinkage.Libraries.Application {
		public PickOrganizationApplication(MB3Client.OptionSupport.LocalAllUserRecordUpdateBehaviors localAllUserRecordChanges = 0) {
			new ApplicationExecutionWithMainFormCreatorDelegate(this,
				delegate () {
					var form = Libraries.Presentation.MSWindows.BrowseForm.NewBrowseForm(GetInterface<UIFactory>(), null, SavedOrganizationsBrowserTblCreator);
					form.Menu = form.MainBrowseControl.UIFactory.CreateMainMenu(
						form.MainBrowseControl.UIFactory.CreateSubMenu(KB.K("Session"), null,
#if DEBUG
						form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Start a SQLite instance"), new CallDelegateCommand(KB.K("Start a SQLite MainBoss instance"), new EventHandler(delegate (object sender, EventArgs args) {
							var junk = new NamedOrganization(KB.I("MainBoss SQLite"), new MB3Client.MBConnectionDefinitionNoServer(DatabaseEnums.ApplicationModeID.Normal, false));
							var app = MainBossApplication.CreateMainBossApplication(junk);
							Thinkage.Libraries.Application.ReplaceActiveApplication(app);
						}))),
						form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.T("DEBUG: Create a SQLite database"), new CallDelegateCommand(KB.K("Create a SQLite MainBoss database"), new EventHandler(delegate (object sender, EventArgs args) {
							var junk = new MB3Client.MBConnectionDefinitionNoServer(DatabaseEnums.ApplicationModeID.Normal, false);
							MB3Client.CreateDatabase(junk, KB.I("MainBoss SQLite"), delegate (MB3Client db) {
								var licenses = new List<License>(new License[] {
								new Thinkage.Libraries.Licensing.License("5muy5-x3cx9-qqp66-17ba1-rfg0f"), //  Named Users 1 licenses, LicenseID 1
								new Thinkage.Libraries.Licensing.License("9gm8m-0ap7r-k8fw9-q5xm9-ggp0b") // Requests License 10 Requestors
								});
								DatabaseCreation.AddLicenses(db, licenses.ToArray());
							}, null);
						}))),
#endif
						form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.K("Exit"), new CallDelegateCommand(KB.K("Close this window"),
								delegate () {
									form.CloseForm(UIDialogResult.Cancel);
								}))
							),
						form.MainBrowseControl.UIFactory.CreateSubMenu(KB.K("Help"), null,
							form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.K("Contents"), new CallDelegateCommand(KB.K("Go to Help Table of Contents"),
								delegate () {
									PickOrganizationApplication.Instance.GetInterface<PickOrganizationApplication.IHelp>().ShowTopic(Thinkage.Libraries.Presentation.KB.HelpTopicKey("TableOfContents"));
								}))
							, form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.K("Help Index"), new CallDelegateCommand(KB.K("Go to Help Index"),
								delegate () {
									PickOrganizationApplication.Instance.GetInterface<PickOrganizationApplication.IHelp>().ShowTopic(Thinkage.Libraries.Presentation.KB.HelpTopicKey("Index"));
								}))
							, form.MainBrowseControl.UIFactory.CreateMenuSeparator()
							, form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.K("Get Technical Support"), new CallDelegateCommand(KB.K("Go to technical support information"),
								delegate () {
									Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IHelp>().ShowTopic(Thinkage.Libraries.Presentation.KB.HelpTopicKey("GetSupport"));
								}))
							, form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.K("Start Support Connection"), new CallDelegateCommand(KB.K("Start the Teamviewer support connection with Thinkage"), new EventHandler((object s, EventArgs a) => {
								MBAboutForm.StartTeamviewer();
							})))
							, form.MainBrowseControl.UIFactory.CreateMenuSeparator()
							, form.MainBrowseControl.UIFactory.CreateCommandMenuItem(KB.K("About..."), new CallDelegateCommand(KB.K("Show information about this application"),
								delegate () {
									new MBAboutForm(form.MainBrowseControl.UIFactory, null, KB.I("MainBoss")).ShowModal(form);
								}))
						)
					);
					form.MainBrowseControl.NotifyRefresh += delegate () {
						var savedOrganizationSession = (SavedOrganizationSession)form.MainBrowseControl.LogicObject.DB.Session;
						savedOrganizationSession.PrepareToRefresh();
					};
					if (localAllUserRecordChanges != 0) {
						GetInterface<IIdleCallback>().ScheduleIdleCallback((object)42, delegate () {
							// Build a message that consists of all the changed information so a single display warning is issued.
							System.Text.StringBuilder message = new System.Text.StringBuilder();
							if ((localAllUserRecordChanges & MB3Client.OptionSupport.LocalAllUserRecordUpdateBehaviors.LocalRecordCreated) != 0)
								message.AppendLine(KB.K("Organization record was created from the AllUsers configuration entry.").Translate());
							else if ((localAllUserRecordChanges & MB3Client.OptionSupport.LocalAllUserRecordUpdateBehaviors.LocalRecordUpdated) != 0)
								message.AppendLine(KB.K("Organization record was updated from the AllUsers configuration entry.").Translate());

							if ((localAllUserRecordChanges & MB3Client.OptionSupport.LocalAllUserRecordUpdateBehaviors.PreferredOrganizationDemanded) != 0)
								message.AppendLine(KB.K("AllUsers configuration has set the preferred organization to its configuration.").Translate());
							if ((localAllUserRecordChanges & MB3Client.OptionSupport.LocalAllUserRecordUpdateBehaviors.AuthenticationCredentialsRequired) != 0)
								message.AppendLine(KB.K("Organization record requires you to enter login credentials.").Translate());
							if (message.Length > 0)
								Libraries.Application.Instance.DisplayWarning(message.ToString());
							// User input is required so start the editor on the AllUsers Local record.
							if ((localAllUserRecordChanges & MB3Client.OptionSupport.LocalAllUserRecordUpdateBehaviors.AuthenticationCredentialsRequired) != 0) {
								Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(
									form.MainBrowseControl.UIFactory,
									new XAFClient(new DBClient.Connection(new SavedOrganizationSession.Connection(), dsSavedOrganizations.Schema)),
									OrganizationEditorTblCreator(TId.Organization, typeof(NewOrganizationEditorLogic)),
									EdtMode.Edit,
									new object[][] { new object[] { KnownIds.OrganizationMasterRecordId } },
									new bool[(int)EdtMode.Max],
									null,
									form.MainBrowseControl.LogicObject.CommonUI.Form,
									true,
									null
									);
							}
						});
					}
					return form;
				});
			// Copy the help parameters from the app that is creating us.
			HelpUsingFolderOfHtml.CopyFromOtherApplication(this, Instance);

			bool netDeployed;
			try {
				WebInfoRoot = GetWebDeploymentLocation(out netDeployed);
				if (!netDeployed) {
					InitialStartupSettings.IsWebBrowserContextMenuEnabled = true;
				}
			}
			catch (System.Exception e) {
				Thinkage.Libraries.Exception.AddContext(e, new MessageExceptionContext(KB.K("Unable to determine ActivationURI from network deployment")));
				throw;
			}
		}
		protected override void CreateUIFactory() {
#if DEBUG
			new Libraries.Presentation.MSWindows.MSWindowsDebugProvider(this, KB.I("MainBoss Debug Form"));
#endif
			new StandardApplicationIdentification(this, "MainBoss", "MainBoss");
			new GUIApplicationIdentification(this, "Thinkage.MainBoss.MainBoss.Resources.MainBoss400.ico");
			new Thinkage.Libraries.XAF.UI.MSWindows.UserInterface(this);
			new Thinkage.Libraries.Presentation.MSWindows.UserInterface(this);
			new MSWindowsUIFactory(this);
			// following attempted to use a permission Manager to govern access to Tbl's in the dsMB namespace (i.e. Restore Organization); however, our structure isn't really set up
			// to permit PermissionManagers to be associated with different Tbl schemas (in this case the dsSavedOrganizations schema and the dsMB schema)
			//			var permissionsManager = new MainBossPermissionsManager(Root.Rights);
			//			var app = new ApplicationTblDefaultsUsingWindows(this, new ETbl(), permissionsManager, Root.Rights.Table, null); //, Root.Rights.Table, Root.Rights.Action.Customize);
			//			TableOperationRightsGroup rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild(dsSavedOrganizations.Schema.T.Organizations.MainTableName);
			//			permissionsManager.GetPermission(rightsGroup.GetTableOperationRight(Thinkage.Libraries.DBILibrary.TableOperationRightsGroup.TableOperation.Browse)).SetPermission(true);
			//			permissionsManager.SetPermission("Table.Organizations.*", true);
			new Libraries.Presentation.MSWindows.ApplicationTblDefaultsUsingWindows(this, new ETbl(), null, null, null, null);
			new Libraries.Presentation.MSWindows.FormsPresentationApplication(this);
		}
		// The following is referenced by reflection as a controlprovider in an .xafdb file, NOT directly. Do not remove unless you have searched for its usage EVERYWHERE
		// and determined it is no longer required.
		public static readonly EnumValueTextRepresentations StartModes;
		/// <summary>
		/// Expected DBVersions for various Start operations
		/// </summary>
		public static readonly Version[] ExpectedMinDBVersion;

		static PickOrganizationApplication() {

			int len = MainBossApplication.Modes.Length;
			var names = new Key[len];
			var hints = new Key[len];
			var values = new object[len];
			ExpectedMinDBVersion = new Version[(int)DatabaseEnums.ApplicationModeID.NextAvailable];
			for (int i = len; --i >= 0;) {
				ModeDefinition m = MainBossApplication.Modes[i];
				names[i] = DatabaseEnums.ApplicationModeName((DatabaseEnums.ApplicationModeID)m.AppMode);
				hints[i] = m.Hint;
				values[i] = m.AppMode;
				ExpectedMinDBVersion[(int)m.AppMode] = m.MinDBVersion;
			}
			StartModes = new EnumValueTextRepresentations(names, hints, values);

			LicenseInformationToSend.Add(KB.K("ProductName"), Thinkage.Libraries.VersionInfo.ProductName);
			LicenseInformationToSend.Add(KB.K("ProductVersion"), Thinkage.Libraries.VersionInfo.ProductVersion.ToString());
			LicenseInformationToSend.Add(KB.K("WindowsVersion"), Environment.OSVersion.VersionString);
			LicenseInformationToSend.Add(KB.K("CultureInfo"), Thinkage.Libraries.Application.InstanceCultureInfo.Name);
		}
		#region -   Disablers
		private class CanDropDatabaseDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public CanDropDatabaseDisabler(BrowseLogic browser, Libraries.DataFlow.Source dbIsSqlAdminSource)
				: base(browser, KB.K("You require CONTROL permission, or ALTER ANY DATABASE, or be the db_owner"), () => (bool)(dbIsSqlAdminSource.GetValue())) {
			}
		}
		private class NeedRecognizableDBDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public NeedRecognizableDBDisabler(BrowseLogic browser, Libraries.DataFlow.Source dbVersionSource)
				: base(browser, KB.K("This is not a MainBoss DataBase"),
				() => {
					Version dbVersion;
					return Version.TryParse((string)dbVersionSource.GetValue(), out dbVersion);
				}) {
			}
		}
		private class StartApplicationDBVersionDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public StartApplicationDBVersionDisabler(BrowseLogic browser, Libraries.DataFlow.Source dbVersionSource, Libraries.DataFlow.Source applicationIdSource)
				: base(browser, KB.K("The default application requires a newer database version"),
				() => {
					Version dbVersion;
					if (!Version.TryParse((string)dbVersionSource.GetValue(), out dbVersion))
						return true; // we have no idea what version the database is; we may have not had permissions to probe it; give benefit of doubt an allow user to try to start mainboss on it anyway.
					try {
						var appIdObject = applicationIdSource.GetValue();
						var appId = (int)applicationIdSource.TypeInfo.GenericAsNativeType(appIdObject, typeof(int));
						return appId < ExpectedMinDBVersion.Length ? dbVersion >= ExpectedMinDBVersion[appId] : false;
					}
					catch (InvalidCastException) {  // thrown by GenericAsNativeType or the cast of its return value
						return true;
					}
				}) {
			}
			public StartApplicationDBVersionDisabler(BrowseLogic browser, Libraries.DataFlow.Source dbVersionSource, int applicationId)
				: base(browser, KB.K("This application requires a newer database version"),
				() => {
					Version dbVersion;
					if (!Version.TryParse((string)dbVersionSource.GetValue(), out dbVersion))
						return false;
					return applicationId < ExpectedMinDBVersion.Length ? dbVersion >= ExpectedMinDBVersion[applicationId] : false;
				}) {
			}
		}
		#endregion
		#region SavedOrganizationsBrowser
		private static Key newGroupKey = KB.T("New group key");
		private static Key sameNewExistingKey = KB.K("Add Existing Organization");
		private const int MAX_PATH = 260; // There is NO .NET defined constant that is common in the C Windows libraries, so we define the common accepted value here with the common name found elsewhere (so if someone searches for it, the will find it here)
		private static Libraries.TypeInfo.StringTypeInfo FilenameTypeInfo = new Libraries.TypeInfo.StringTypeInfo(1, MAX_PATH, 0, false, true, true);
		private static ICommand MakeOpenOrganizationCommand(BrowseLogic browserLogic, Key hint, IDisablerProperties appIdDisabler, DatabaseEnums.ApplicationModeID? forcedMode = null) {
			Libraries.DataFlow.Source defaultAppModeSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.PreferredApplicationMode, -1);
			Libraries.DataFlow.Source organizationNameSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName, -1);
			Libraries.DataFlow.Source compactBrowsersSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CompactBrowsers, -1);
			Libraries.DataFlow.Source dbServerSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer, -1);
			Libraries.DataFlow.Source dbNameSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName, -1);
			AuthenticationCredentialsSource credentialSource = new AuthenticationCredentialsSource(
				browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CredentialsAuthenticationMethod, -1),
				browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CredentialsUsername, -1),
				browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword, -1)
			);
			return new MultiCommandIfAllEnabled(new CallDelegateCommand(hint,
				delegate () {
					// Make a NamedOrganization from the selected record, and extract its app mode.
					browserLogic.BrowserSelectionPositioner.CurrentPosition = browserLogic.BrowserSelectionPositioner.StartPosition.Next;
					int mode = (int?)forcedMode ?? (int)defaultAppModeSource.TypeInfo.GenericAsNativeType(defaultAppModeSource.GetValue(), typeof(int));
					var junk = new NamedOrganization((string)organizationNameSource.GetValue(), new MB3Client.MBConnectionDefinition((DatabaseEnums.ApplicationModeID)mode, (bool?)compactBrowsersSource.GetValue() ?? false, (string)dbServerSource.GetValue(), (string)dbNameSource.GetValue(), credentialSource.GetValue()));
					var app = MainBossApplication.CreateMainBossApplication(junk);
					if (app == null)
						throw new GeneralException(KB.K("Application mode {0} is unknown"), DatabaseEnums.ApplicationModeID.Normal);
					PickOrganizationApplication.ReplaceActiveApplication(app);

				}), browserLogic.NeedSingleSelectionDisabler, appIdDisabler);
		}
		private class AuthenticationCredentialsSource {
			private readonly Libraries.DataFlow.Source UserName;
			private readonly Libraries.DataFlow.Source Password;
			private readonly Libraries.DataFlow.Source Type;
			public AuthenticationCredentialsSource(Libraries.DataFlow.Source type, Libraries.DataFlow.Source username, Libraries.DataFlow.Source password) {
				UserName = username;
				Password = password;
				Type = type;
			}
			public AuthenticationCredentials GetValue() {
				return new AuthenticationCredentials((AuthenticationMethod)(byte)Type.GetValue(), (string)UserName.GetValue(), MB3Client.OptionSupport.DecryptCredentialsPassword((string)Password.GetValue()));
			}
		}
		public static readonly DelayedCreateTbl SavedOrganizationsBrowserTblCreator = new DelayedCreateTbl(
			delegate () {
				var btblAttrs = new List<BTbl.ICtorArg>();
				//				btblAttrs.Add(BTbl.SetShowBrowserPanel(false));
				btblAttrs.Add(BTbl.SetDeleteCommandIdentification(null, KB.K("Remove from list"), KB.K("Remove the organization from the list of saved organizations. The underlying database is NOT deleted.")));
				btblAttrs.Add(BTbl.SetCloseExitCommandIdentification(null, KB.K("Exit"), KB.K("Exit this application")));
				// Add the list columns
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName));
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer));
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName));
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.DBVersion));
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.Access));
				btblAttrs.Add(BTbl.ListColumn(KB.K("Hide Panels"), dsSavedOrganizations.Path.T.Organizations.F.CompactBrowsers));
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.PreferredApplicationMode));
				btblAttrs.Add(BTbl.ListColumn(dsSavedOrganizations.Path.T.Organizations.F.Status));
				btblAttrs.Add(BTbl.AdditionalVerb(null,
					delegate (BrowseLogic browserLogic) {
						Libraries.DataFlow.Source organizationNameSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName, -1);
						Libraries.DataFlow.Source dbSqlServerCanDropDatabase = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CanDropDatabase, -1);
						Libraries.DataFlow.Source dbServerSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer, -1);
						Libraries.DataFlow.Source dbNameSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName, -1);
						Libraries.DataFlow.Source dbVersionSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.DBVersion, -1);
						Libraries.DataFlow.Source defaultAppModeSource = browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.PreferredApplicationMode, -1);
						AuthenticationCredentialsSource credentialSource = new AuthenticationCredentialsSource(
							browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CredentialsAuthenticationMethod, -1),
							browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CredentialsUsername, -1),
							browserLogic.GetTblPathDisplaySource(dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword, -1)
						);
						#region Common Disablers
						IDisablerProperties NeedRecognizableDBDisabler = new NeedRecognizableDBDisabler(browserLogic, dbVersionSource);
						IDisablerProperties DBIsSqlAdminDisabler = new CanDropDatabaseDisabler(browserLogic, dbSqlServerCanDropDatabase);
						#endregion
						#region Open Organization commands
						// Add all the "open" verbs
						CommonLogic.CommandNode openOrganizationNode = browserLogic.Commands.CreateNestedNode(KB.K("Open Organization"), null);
						// Move this group to the head of the list.
						CommonLogic.CommandNode.Entry entry = browserLogic.Commands.Nodes[browserLogic.Commands.Nodes.Count - 1];
						browserLogic.Commands.Nodes.RemoveAt(browserLogic.Commands.Nodes.Count - 1);
						browserLogic.Commands.Nodes.Insert(0, entry);
						// Make this first command be the double-click command for the browser (instead of Edit)
						ICommand openDefaultModeCommand = MakeOpenOrganizationCommand(browserLogic, KB.K("Open the selected organization in the preferred mode specified in the saved organization"),
							new StartApplicationDBVersionDisabler(browserLogic, dbVersionSource, defaultAppModeSource));
						browserLogic.DefaultCommand = openDefaultModeCommand;
						openOrganizationNode.AddCommand(KB.K("Start"), null, openDefaultModeCommand, null);
						foreach (ModeDefinition m in MainBossApplication.Modes)
							openOrganizationNode.AddCommand(DatabaseEnums.ApplicationModeName((DatabaseEnums.ApplicationModeID)m.AppMode), null,
								MakeOpenOrganizationCommand(browserLogic, m.Hint,
								new StartApplicationDBVersionDisabler(browserLogic, dbVersionSource, m.AppMode), (DatabaseEnums.ApplicationModeID)m.AppMode), null);
						#endregion
						#region Maintenance Commands
						CommonLogic.CommandNode maintenanceCommands = browserLogic.Commands.CreateNestedNode(KB.K("Database Operations"), null);

						maintenanceCommands.AddCommand(KB.K("Upgrade"), null,
							new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Upgrade the database structure to the one used by this version of MainBoss"),
								delegate () {
									PickOrganizationApplication.ReplaceActiveApplication(new UpgraderApplication((string)organizationNameSource.GetValue(), new MB3Client.ConnectionDefinition((string)dbServerSource.GetValue(), (string)dbNameSource.GetValue(), credentialSource.GetValue()),
										delegate () {
											return new PickOrganizationApplication();
										}));
								}), browserLogic.NeedSingleSelectionDisabler, NeedRecognizableDBDisabler), null);
						maintenanceCommands.AddCommand(KB.K("Restore Organization"), null,
							new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Restore the selected organization from one of its backups"),
								delegate () {
									// The BrowseForm opens and closes its own session on the DB using a custom tbl session.
									// TODO: This should call up an Edit form on the organization, with a picker for existing backup files. That way if the db is trashed all that doesn't work is the '...' button on the picker.
									Libraries.Presentation.MSWindows.BrowseForm.NewBrowseForm(browserLogic.CommonUI.UIFactory, null, TICommon.RestoreExistingTblCreator((string)dbServerSource.GetValue(), (string)dbNameSource.GetValue(), credentialSource.GetValue(), () => {
										// we will be notified at this point on completion of the restore; do whatever you need at this point to update controls.
									})).ShowModal();
								}), browserLogic.NeedSingleSelectionDisabler, NeedRecognizableDBDisabler), null);
						maintenanceCommands.AddCommand(KB.K("Run Script"), null,
							new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Run a SQL script against the selected organization"),
								delegate () {
									Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(
										browserLogic.CommonUI.UIFactory,
										browserLogic.DB,
										RunScriptEditLogic.RunScriptTblCreator(),
										EdtMode.View,
										new object[][] { new object[] { (Guid)browserLogic.BrowserSelectionPositioner.StartPosition.Next.Id } },
										ApplicationTblDefaults.NoModeRestrictions,
										new[] { new List<TblActionNode>() },
										((ICommonUI)browserLogic.CommonUI).Form,
										true,
										delegate (object sender, EditLogic.SavedEventArgs ea) {
											// Do what here ?
										});
								}),
								browserLogic.NeedSingleSelectionDisabler, NeedRecognizableDBDisabler), null);

						ICommand deleteCommand = new BrowseLogic.DeleteRecordsCommand(browserLogic, KB.K("Delete the saved organization and its associated SQL database"));
						maintenanceCommands.AddCommand(KB.K("Delete database and organization"), null,
							new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Delete the saved organization and its associated SQL database"),
								delegate () {
									var dbserver = (string)dbServerSource.GetValue();
									var dbname = (string)dbNameSource.GetValue();
									if (Ask.Question(Strings.Format(KB.K("Delete Database '{0}' from Server '{1}'. ALL Data will be destroyed"), dbname, dbserver)) == Ask.Result.No)
										return;
									try {
										MB3Client.DeleteDatabase(new MB3Client.ConnectionDefinition(dbserver, dbname, credentialSource.GetValue()));
										deleteCommand.Execute();
									}
									catch (GeneralException) {
										throw;
									}
									catch (System.Exception e) {
										throw new GeneralException(e, KB.K("Cannot drop database '{0}'"), dbname);
									}
								}), browserLogic.NeedSingleSelectionDisabler, DBIsSqlAdminDisabler), null);
						#endregion
						#region Default Set/Clear Commands
						CommonLogic.CommandNode defaultNode = browserLogic.Commands.CreateNestedNode(KB.K("Set default Organization"), null);
						defaultNode.AddCommand(KB.K("Set Default"), null,
							new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Set the selected organization to be the one MainBoss opens by default"),
								delegate () {
									using (dsSavedOrganizations ds = new dsSavedOrganizations(browserLogic.DB)) {
#if DEBUG
										browserLogic.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.PreferredOrganizationDebug);
										ds.V.PreferredOrganizationDebug.Value = (Guid)browserLogic.BrowserSelectionPositioner.StartPosition.Next.Id;
#else
										browserLogic.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.PreferredOrganization);
										ds.V.PreferredOrganization.Value = (Guid)browserLogic.BrowserSelectionPositioner.StartPosition.Next.Id;
#endif
										browserLogic.DB.Update(ds);
									}
								}), browserLogic.NeedSingleSelectionDisabler), null);
						ICommand cmd = new CallDelegateCommand(KB.K("Clear the default organization so MainBoss always asks you to select one on program startup"),
							delegate () {
								using (dsSavedOrganizations ds = new dsSavedOrganizations(browserLogic.DB)) {
#if DEBUG
									browserLogic.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.PreferredOrganizationDebug);
									ds.V.PreferredOrganizationDebug.SetNull();
#else
									browserLogic.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.PreferredOrganization);
									ds.V.PreferredOrganization.SetNull();
#endif
									browserLogic.DB.Update(ds);
								}
							});

						defaultNode.AddCommand(KB.K("Clear Default"), null, cmd, cmd);
						#endregion
						return null;
					}));
				btblAttrs.Add(BTbl.SetDummyBrowserPanelControl(new TblLayoutNodeArray(
					TblUnboundControlNode.New(Libraries.TypeInfo.StringTypeInfo.Universe, DCol.Normal,
						Fmt.SetUsage(DBI_Value.UsageType.Html),
						Fmt.SetHtmlDisplaySettings(((PickOrganizationApplication)Thinkage.Libraries.Application.Instance).GetHtmlSettings())
					)
				)));

				return new CompositeTbl(dsSavedOrganizations.Schema.T.Organizations, TId.Organization,
					new Tbl.IAttr[] {
						new BTbl(btblAttrs.ToArray()),
						new CustomSessionTbl(
							delegate(XAFClient existingDatabaseAccess, DBI_Database newSchema) {
								return new SavedOrganizationSession.Connection();
							}
						)
					},
					null,
				#region New Commands (Create)
					// For all of these we want a New command in the browser but no transition back to New in the editor. In fact we don't even really want Edit/View mode in most of them so you have Save&Close which does the work. but this is not a requirement.
					new CompositeView(OrganizationEditorTblCreator(TId.Organization, typeof(NewOrganizationEditorLogic)), dsSavedOrganizations.Path.T.Organizations.F.Id,
						CompositeView.JoinedNewCommand(sameNewExistingKey),
						CompositeView.NewCommandGroup(newGroupKey),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.IsPreferredOrganization).Not())),    // This is the only one where we want Edit mode...?
					new CompositeView(OrganizationEditorTblCreator(TId.Organization, typeof(NewOrganizationEditorLogic)), dsSavedOrganizations.Path.T.Organizations.F.Id,
						CompositeView.JoinedNewCommand(sameNewExistingKey),
						CompositeView.NewCommandGroup(newGroupKey),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.IsPreferredOrganization)),
						CompositeView.IdentificationOverride(TId.DefaultOrganization)),
					CompositeView.ExtraNewVerb(
						OrganizationEditorTblCreator(TId.Organization, typeof(NewCreateDatabaseWithLicensesOrganizationEditorLogic)
						), CompositeView.JoinedNewCommand(KB.K("Create New Organization")),
						CompositeView.NewCommandGroup(newGroupKey)),
					CompositeView.ExtraNewVerb(
						OrganizationEditorTblCreator(TId.Organization, typeof(NewOrganizationFromBackupEditorLogic),
							TblUnboundControlNode.New(KB.K("Backup file"),
								FilenameTypeInfo,
								ECol.ReadonlyInUpdate,
								Fmt.SetId(InputFileId),
								Fmt.SetCreator(
									delegate (CommonLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) {
										// TODO: Backup file must be accessible to the SQL server
										// TODO: "SQL backup from MainBoss Advanced (*.bak)|*.bak|All Files (*.*)|*.*"
										return logicObject.CommonUI.UIFactory.CreateNamePicker(FilenameTypeInfo, enabledDisabler, writeableDisabler, UINamePickerOptions.ClassExistingFile | UINamePickerOptions.FormatFilePath);
									}
								)
							),
							TblUnboundControlNode.New(KB.K("Backup Set number"),
								new Libraries.TypeInfo.IntegralTypeInfo(true, 1, int.MaxValue),
								ECol.ReadonlyInUpdate,
								Fmt.SetId(BackupSetId),
								Fmt.SetIsSetting((object)null)
							)
						), CompositeView.JoinedNewCommand(KB.K("Create New Organization from a Backup")),
						CompositeView.NewCommandGroup(newGroupKey)),
					CompositeView.ExtraNewVerb(
						OrganizationEditorTblCreator(TId.Organization, typeof(NewOrganizationFrom29DataEditorLogic),
							TblUnboundControlNode.New(KB.K("MainBoss Basic 2.9 export file"),
								FilenameTypeInfo,
								ECol.ReadonlyInUpdate,
								Fmt.SetId(InputFileId),
								Fmt.SetCreator(
									delegate (CommonLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) {
										// TODO: "Xml export data from MB2.9 (*.XML)|*.XML|Xml export data from MB2.9 (*.*)|*.*"
										return logicObject.CommonUI.UIFactory.CreateNamePicker(FilenameTypeInfo, enabledDisabler, writeableDisabler, UINamePickerOptions.ClassExistingFile | UINamePickerOptions.FormatFilePath);
									}))
						), CompositeView.JoinedNewCommand(KB.K("Create New Organization using MainBoss Basic 2.9 export file")),
						CompositeView.NewCommandGroup(newGroupKey))
				#endregion
				);
			}
		);
		#endregion
		#region RunScriptTblCreator
		public class RunScriptEditLogic : EditLogic {
			public RunScriptEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, ModifySubsequentModeRestrictions(subsequentModeRestrictions), initLists) {
			}
			private static bool[] ModifySubsequentModeRestrictions(bool[] subsequentModeRestrictions) {
				bool[] result = (bool[])subsequentModeRestrictions.Clone();
				result[(int)EdtMode.New] = false;
				result[(int)EdtMode.Clone] = false;
				result[(int)EdtMode.View] = true;
				return result;
			}
			protected override object[] SaveRecord(Libraries.DBILibrary.Server.UpdateOptions updateOptions) {
				// Do not call base.SaveRecord which assumes there is a DB client
				return new object[] { null /* RunScript ? */ };
			}
			EditorState StateParametersChanged = null;
			EditorState StateParametersUnchanged = null;
			//			EditorState StateUnCommittedChanged = null;
			//			EditorState StateUnCommittedUnchanged = null;

			protected override void SetupStates() {
				base.SetupStates();
				StateParametersChanged = new EditorState(EdtMode.View, true, true, false);
				AllStates.AddUnique(StateParametersChanged);
				StateParametersUnchanged = new EditorState(EdtMode.View, false, false, false);
				AllStates.AddUnique(StateParametersUnchanged);

				// Define the initial entry state for New mode.
				// This means that the normal new-mode state (StateNewUnchanged) is not reachable, nor is StateNewChanged.
				// Instead we define a RunScript command (from StateParameters[Un]changed to StateParametersChanged)
				// Once we get to InitialStatesForModes[(int)EdtMode.View] we are in the standard state graph
				// except that because we remove the NewCommandGroup at the end of SetupCommands, transitions back to InitialStatesForModes[(int)EdtMode.New] cannot occur.
				// The only way to have multiple editing history entries is to edit multiple existing (committed) records, and in this case the editor stays firmly within
				// standard edit states.
				InitialStatesForModes[(int)EdtMode.View] = StateParametersUnchanged;

				// Define the state transitions caused by user changes
				UserChangeStateTransitions.Add(StateParametersUnchanged, StateParametersChanged);

				// Define states where the user can close the form
				CloseCommandEnabledStates.AddUnique(StateParametersUnchanged);
			}
			protected override void SetupCommands() {
				base.SetupCommands();
				MutuallyExclusiveCommandSetDeclaration cgd = new MutuallyExclusiveCommandSetDeclaration();
				cgd.Add(new CommandDeclaration(KB.K("Run Script"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Run a script against the organization database"),
					delegate () {
						RunScript();
					},
					StateParametersUnchanged,
					StateParametersChanged,
					StateParametersUnchanged
					)));
				CommandGroupDeclarationsInOrder.Insert(0, cgd);
			}
			public static Tbl RunScriptTblCreator() {
				var layoutNodes = new List<TblLayoutNode>();
				// We are using View Mode so all ECol.Normal will be readonly anyway
				layoutNodes.Add(TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName, ECol.Normal, Fmt.SetId(OrganizationNameId)));
				layoutNodes.Add(TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer, ECol.Normal, Fmt.SetId(ServerNameId)));
				layoutNodes.Add(TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName, ECol.Normal, Fmt.SetId(DatabaseNameId)));
				layoutNodes.Add(TblGroupNode.New(KB.K("Credentials"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
					TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CredentialsAuthenticationMethod, ECol.Normal, Fmt.SetId(CredentialsAuthenticationMethodId)),
					TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CredentialsUsername, ECol.Normal, Fmt.SetId(CredentialsUsernameId)),
					//								TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword, new ECol(getPasswordCreatorAttribute()), Fmt.SetId(CredentialsPasswordId))
					TblCustomTypedColumnNode.New(
							PasswordInputTypeInfo,
							(pwd) => {
								string spwd = (string)pwd;
								if (!String.IsNullOrEmpty(spwd))
									pwd = MB3Client.OptionSupport.DecryptCredentialsPassword(spwd);
								return pwd;
							},
							(pwd) => {
								string spwd = (string)pwd;
								if (!String.IsNullOrEmpty(spwd))
									pwd = MB3Client.OptionSupport.EncryptCredentialsPassword(spwd);
								return pwd;
							},
							dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword,
							0,
							ECol.AllReadonly,
							Fmt.SetId(CredentialsPasswordId)
						)
					)
				);
				layoutNodes.Add(TblUnboundControlNode.New(KB.K("Source Database Name"),
					new Libraries.TypeInfo.StringTypeInfo(1, 128, 0, true, true, true),
					ECol.AllWriteable,
					Fmt.SetId(InputDatabaseNameId),
					Fmt.SetCreatorT<EditLogic>(
						delegate (EditLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) {
							Libraries.DataFlow.Source serverNameSourceInEditor = logicObject.GetControlNotifyingSource(ServerNameId);
							return logicObject.CommonUI.UIFactory.CreateTextEditWithPickButton((Libraries.TypeInfo.StringTypeInfo)controlTypeInfo, enabledDisabler, writeableDisabler, KB.K("Select from available databases"),
										delegate (IInputControl control) {
											Libraries.DataFlow.Source serverNameSourceInPicker = null;
											var form = Libraries.Presentation.MSWindows.SelectValueForm.NewSelectValueForm(logicObject.CommonUI.UIFactory, null, SelectDatabaseTbl, new BrowserPathValue(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.Database),
												delegate (object value) {
													control.Value = value;
													try {
														serverNameSourceInEditor.GetValue();
													}
													catch (System.Exception) {
													}
												}, allowNull: false);
											object serverNameInEditor = null;
											try {
												serverNameInEditor = serverNameSourceInEditor.GetValue();
											}
											catch (Libraries.TypeInfo.NonnullValueRequiredException) {
											}
											form.MainBrowseControl.BrowserLogic.GetSinkForInit(new BrowserFilterTarget(DatabasePickerServerFilterId)).SetValue(serverNameInEditor);
											serverNameSourceInPicker = form.MainBrowseControl.BrowserLogic.GetTblPathDisplaySource(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.ServerName, -1);
											form.ShowModal(control.ContainingForm);
										}
									);
						}
					)
				));
				layoutNodes.Add(TblUnboundControlNode.New(KB.K("Script file"),
					FilenameTypeInfo,
					ECol.AllWriteable,
					Fmt.SetId(ScriptFileId),
					Fmt.SetCreator(
						delegate (CommonLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) {
							return logicObject.CommonUI.UIFactory.CreateNamePicker(FilenameTypeInfo, enabledDisabler, writeableDisabler, UINamePickerOptions.ClassExistingFile | UINamePickerOptions.FormatFilePath);
						})));
				return new Tbl(
					dsSavedOrganizations.Schema.T.Organizations,
					TId.Organization,
					new Tbl.IAttr[] {
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View), new ETbl.CustomLogicClassArg(typeof(RunScriptEditLogic)))
					},
					new TblLayoutNodeArray(layoutNodes)
				);
			}
			protected void RunScript() {
				IProgressDisplay ipd = null;
				HistoryLogText = new System.Text.StringBuilder();
				string inputDatabase = (string)GetControlNotifyingSource(InputDatabaseNameId).GetValue();
				string scriptUrl = (string)GetControlNotifyingSource(ScriptFileId).GetValue(); // The root script url
				connectInfo = GetConnectionDefinition();
				MB3Client mb3db = null;
				GeneralException gex = null;
				try {
					ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Running SQL script"), -1);
					TheConverter = new Thinkage.MainBoss.CustomConversion.RunScriptBasedOnDBConverter((SqlClient.Connection)connectInfo.ConnectionInformation, inputDatabase, connectInfo.DBName);
					TheConverter.CheckSrcDatabase();
					ipd.Update(KB.T(Strings.Format(KB.K("Running script on database {0} on database server {1}"), connectInfo.DBName, connectInfo.DBServer)));
					mb3db = new MB3Client(connectInfo);
					ScriptProcessing(mb3db, inputDatabase, scriptUrl, ipd);
					if (HistoryLogText.Length == 0)
						MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(mb3db).LogHistory(mb3db, Strings.Format(KB.K("SQL Script completed {0}"), scriptUrl), null);
					else {
						MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(mb3db).LogHistory(mb3db, Strings.Format(KB.K("SQL Script completed with errors {0}"), scriptUrl), HistoryLogText.ToString());
						gex = new GeneralException(KB.K("Run Script has errors"));
					}
				}
				catch (System.Exception ex) {
					gex = new GeneralException(ex, KB.K("Run Script has errors"));
				}
				finally {
					if (gex != null) {
						Thinkage.Libraries.Exception.AddContext(gex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Script source {0}"), scriptUrl));
						if (inputDatabase != null)
							Thinkage.Libraries.Exception.AddContext(gex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Input SQL database {0}"), inputDatabase));
						if (HistoryLogText.Length > 0)
							Thinkage.Libraries.Exception.AddContext(gex, new Thinkage.Libraries.MessageExceptionContext(KB.T(HistoryLogText.ToString())));
						ipd.ErrorAndWait(null, gex);
					}
					if (mb3db != null)
						mb3db.CloseDatabase();
					mb3db = null;
					if (ipd != null)
						ipd.Complete();
					HistoryLogText = null;
					TheConverter = null;
				}
			}
			Thinkage.MainBoss.CustomConversion.RunScriptBasedOnDBConverter TheConverter;
			System.Text.StringBuilder HistoryLogText;
			MB3Client.ConnectionDefinition connectInfo;

			// TODO: When done open in admin mode so licenses can be added.
			protected void ScriptProcessing(MB3Client db, string inputDatabase, string scriptUrl, IProgressDisplay ipd) {
				TheConverter.TableConverting += delegate (object source, TableConvertEventArgs e) {
					ipd.Update(KB.T(Strings.Format(KB.K("Script Step {0}{1}"), e.TableName, Environment.NewLine)));
				};
				TheConverter.TableConverted += delegate (object source, TableConvertEventArgs e) {
					if (!String.IsNullOrEmpty(e.Messages)) {
						HistoryLogText.Append(separator);
						HistoryLogText.Append(Strings.Format(KB.K("Script Step {0} Errors{1}"), e.TableName, Environment.NewLine));
						HistoryLogText.Append(separator);
						HistoryLogText.Append(e.Messages);
					}
				};
				TheConverter.Convert(Strings.IFormat("file://{0}", scriptUrl));
			}
			private readonly static string separator = Strings.IFormat("*****************************************************************{0}", Environment.NewLine);
			protected Key ProgressCaption() {
				return KB.K("Running SQL script");
			}
			protected MB3Client.ConnectionDefinition GetConnectionDefinition() {
				return new MB3Client.ConnectionDefinition((string)GetControlNotifyingSource(ServerNameId).GetValue(), (string)GetControlNotifyingSource(DatabaseNameId).GetValue(),
					new AuthenticationCredentials(
						(AuthenticationMethod)(long)GetControlNotifyingSource(CredentialsAuthenticationMethodId).GetValue(),
						(string)GetControlNotifyingSource(CredentialsUsernameId).GetValue(),
						(string)GetControlNotifyingSource(CredentialsPasswordId).GetValue()
						));
			}
		}

		#endregion
		#region Object NodeIds
		private static readonly object DatabasePickerServerFilterId = KB.I("DatabasePickerServerFilterId");
		private static readonly object ExistingOrganizationNamesId = KB.I("ExistingOrganizationNamesId");
		private static readonly object OrganizationNameId = KB.I("OrganizationNameId");
		private static readonly object ServerNameId = KB.I("ServerNameId");
		private static readonly object DatabaseNameId = KB.I("DatabaseNameId");
		private static readonly object InputDatabaseNameId = KB.I("InputDatabaseNameId");
		private static readonly object InputFileId = KB.I("InputFileId");
		private static readonly object ScriptFileId = KB.I("ScriptFileId");
		private static readonly object BackupSetId = KB.I("BackupSetId");
		private static readonly object CompactBrowsersId = KB.I("CompactBrowsersId");
		private static readonly object PreferredModeId = KB.I("PreferredModeId");
		private static readonly object CredentialsUsernameId = KB.I("CredentialsUsernameId");
		private static readonly object CredentialsAuthenticationMethodId = KB.I("CredentialsAuthenticationMethodId");
		private static readonly object CredentialsPasswordId = KB.I("CredentialsPasswordId");
		private static readonly object HtmlDisplayId = KB.I("HtmlDisplayId");
		private static readonly object LicensePickerId = KB.I("LicensePickerId");
		private static readonly object EmailForLicenseId = KB.I("EmailForLicenseId");
		#endregion
		#region Organization Editor
		private static AuthenticationMethod GetCreditionalsAuthenticationMethod(EditLogic el) {
			var v = el.GetControlNotifyingSource(CredentialsAuthenticationMethodId).GetValue();
			return (AuthenticationMethod)(int)dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod.EffectiveType.GenericAsNativeType(v, typeof(int));
		}
		private static EnumValueTextRepresentations LicenseRequestTypes = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("No License"),
				KB.K("Demonstration"),
				KB.K("Free Limited Single User")
			},
			null,
			new object[] {
				"", // no request is made if we pick No License, but we need a non-null value here for the control error checking to not complain about Value Required.
				KB.I("DemoLicense"),
				KB.I("FreeLicense")
			}
		);
		private static Dictionary<Key, string> LicenseInformationToSend = new Dictionary<Key, string>();
		private static Key EmailAddressLabel = KB.K("Email Address");
		private static Key LicensesToRequestLabel = KB.K("Licenses To Request");
		private static readonly EditorCalculatedInitValue.CalculationDelegate GetLicenseDisplay = delegate (object[] inputs) {

			//TODO: build some mechanism to put a Dictionary of <Keys, Strings> one per line in the display. Get this dictionary of 'information' from some common place and format a multiline display of information
			// Information to be supplied, customer id if known, database and server name, current licenses, email address.
			// the web site will generate the customer id if unknown.
			// also need to convert microsoft store licenses to our licenses, 
			System.Text.StringBuilder displayText = new System.Text.StringBuilder();
			displayText.AppendLine(KB.K("The following information will be sent to Thinkage when requesting the licenses").Translate());
			displayText.AppendLine();
			displayText.AppendLine(Strings.IFormat("{0}: {1}", EmailAddressLabel.Translate(), (string)inputs[0] ?? ""));
			displayText.AppendLine(Strings.IFormat("{0}: {1}", dsSavedOrganizations.Path.T.Organizations.F.OrganizationName.Key().Translate(), (string)inputs[1] ?? ""));
			displayText.AppendLine(Strings.IFormat("{0}: {1}", dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer.Key().Translate(), (string)inputs[2] ?? ""));
			displayText.AppendLine(Strings.IFormat("{0}: {1}", dsSavedOrganizations.Path.T.Organizations.F.DataBaseName.Key().Translate(), (string)inputs[3] ?? ""));

			foreach (var e in LicenseInformationToSend) {
				displayText.AppendLine(Strings.IFormat("{0}:{1}", e.Key.Translate(), e.Value ?? ""));
			}
			return displayText.ToString();
		};
		private static DelayedCreateTbl OrganizationEditorTblCreator(Tbl.TblIdentification identification, System.Type EL, params TblLayoutNode[] additionalInputs) {
			var etblAttrs = new List<ETbl.ICtorArg>();
			etblAttrs.Add(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.View, EdtMode.ViewDefault, EdtMode.EditDefault, EdtMode.ViewDefault));
			etblAttrs.Add(ETbl.LogicClass(EL));
			etblAttrs.Add(ETbl.SetAllowMultiRecordEditing(false));
			return new DelayedCreateTbl(
				delegate () {
					return new Tbl(dsSavedOrganizations.Schema.T.Organizations, identification,
						new Tbl.IAttr[] {
							new ETbl(etblAttrs.ToArray()),
						},
						new TblLayoutNodeArray(
							TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer, Fmt.SetId(ServerNameId),
								new ECol(ECol.NormalAccess,
									Fmt.SetCreator(
										delegate (CommonLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) {
											return logicObject.CommonUI.UIFactory.CreateTextEditWithPickButton((Libraries.TypeInfo.StringTypeInfo)leafNode.ReferencedType, enabledDisabler, writeableDisabler, KB.K("Select from available database servers"),
												delegate (IInputControl control) {
													Libraries.Presentation.MSWindows.SelectValueForm.NewSelectValueForm(logicObject.CommonUI.UIFactory, null, SelectServerTbl, new BrowserPathValue(dsSqlServers.Path.T.SqlServers.F.Name),
														delegate (object value) {
															control.Value = value;
														}, allowNull: false).ShowModal(control.ContainingForm);
												}
											);
										}
									)
								)
							),
							TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName, Fmt.SetId(DatabaseNameId),
								new ECol(ECol.NormalAccess,
									Fmt.SetCreatorT<EditLogic>(
										delegate (EditLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) {
											// We want to copy the server name back too if the user selected the database without a valid value in the server control.
											// There are two cheats happening here:
											// One is that the only reason we can get the Path Source out of the picker browser after NewBrowseForm has elaborated the browser to completion is that the browser itself already created the path source for a list column.
											//		Some day we will explicitly forbid this and NewBrowseForm will have to be given a list of sources of interest.
											// The other is that we are assuming tht when BrowseForm calls its acceptor delegate, the browser's data is still positioned to the selected record so the source mentioned above gets the correct record.
											//		Again this could be cured by having NewBrowseForm manage multiple sources and passing all the values as a value array (for example)
											Libraries.DataFlow.Source serverNameSourceInEditor = logicObject.GetControlNotifyingSource(ServerNameId);
											Libraries.DataFlow.Sink serverNameSinkInEditor = logicObject.GetControlSink(ServerNameId);
											return logicObject.CommonUI.UIFactory.CreateTextEditWithPickButton((Libraries.TypeInfo.StringTypeInfo)controlTypeInfo, enabledDisabler, writeableDisabler, KB.K("Select from available databases"),
												delegate (IInputControl control) {
													Libraries.DataFlow.Source serverNameSourceInPicker = null;
													var form = Libraries.Presentation.MSWindows.SelectValueForm.NewSelectValueForm(logicObject.CommonUI.UIFactory, null, SelectDatabaseTbl, new BrowserPathValue(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.Database),
														delegate (object value) {
															control.Value = value;
															try {
																serverNameSourceInEditor.GetValue();
															}
															catch (System.Exception) {
																serverNameSinkInEditor.SetValue(serverNameSourceInPicker.GetValue());
															}
														}, allowNull: false);
													object serverNameInEditor = null;
													try {
														serverNameInEditor = serverNameSourceInEditor.GetValue();
													}
													catch (Libraries.TypeInfo.NonnullValueRequiredException) {
													}
													form.MainBrowseControl.BrowserLogic.GetSinkForInit(new BrowserFilterTarget(DatabasePickerServerFilterId)).SetValue(serverNameInEditor);
													serverNameSourceInPicker = form.MainBrowseControl.BrowserLogic.GetTblPathDisplaySource(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.ServerName, -1);
													form.ShowModal(control.ContainingForm);
												}
											);
										}
									)
								)
							),
							TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName, ECol.Normal, Fmt.SetId(OrganizationNameId)),
							TblGroupNode.New(KB.K("Credentials"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
								TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CredentialsAuthenticationMethod, ECol.Normal, Fmt.SetId(CredentialsAuthenticationMethodId)),
								TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CredentialsUsername, ECol.Normal, Fmt.SetId(CredentialsUsernameId)),
								//								TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword, new ECol(getPasswordCreatorAttribute()), Fmt.SetId(CredentialsPasswordId))
								TblCustomTypedColumnNode.New(
									PasswordInputTypeInfo,
									(pwd) => {
										string spwd = (string)pwd;
										if (!String.IsNullOrEmpty(spwd))
											pwd = MB3Client.OptionSupport.DecryptCredentialsPassword(spwd);
										return pwd;
									},
									(pwd) => {
										string spwd = (string)pwd;
										if (!String.IsNullOrEmpty(spwd))
											pwd = MB3Client.OptionSupport.EncryptCredentialsPassword(spwd);
										return pwd;
									},
									dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword,
									0,
									ECol.Normal,
									Fmt.SetId(CredentialsPasswordId)
									)
							),
							TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.CompactBrowsers, ECol.Normal, Fmt.SetId(CompactBrowsersId)),
							TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.PreferredApplicationMode, ECol.Normal, Fmt.SetId(PreferredModeId)),   // TODO: Can we force a dropdown here?
							TblUnboundControlNode.New(new Libraries.TypeInfo.SetTypeInfo(true, dsSavedOrganizations.Path.T.Organizations.F.OrganizationName.ReferencedColumn.EffectiveType), ECol.Hidden, Fmt.SetId(ExistingOrganizationNamesId)),
							EL == typeof(NewCreateDatabaseWithLicensesOrganizationEditorLogic) ?
								TblGroupNode.New(KB.K("Licensing"), new TblLayoutNode.ICtorArg[] { ECol.OmitInUpdate },
									// Use an HTML display to provide information to user about WHAT we will be sending to Thinkage.
									TblUnboundControlNode.New(LicensesToRequestLabel, new StringTypeInfo(0, 32, 0, false, true, true), ECol.OmitInUpdate,
										Fmt.SetEnumText(LicenseRequestTypes),
										Fmt.SetIsSetting(KB.I("FreeLicense")),
										Fmt.SetId(LicensePickerId)
									),
									TblUnboundControlNode.New(EmailAddressLabel, new StringTypeInfo(0, 128, 0, false, true, true), ECol.OmitInUpdate, Fmt.SetId(EmailForLicenseId)),
									TblUnboundControlNode.New(StringTypeInfo.Universe, ECol.AllReadonly, Fmt.SetId(HtmlDisplayId))
							) : null,
							TblColumnNode.New(dsSavedOrganizations.Path.T.Organizations.F.Status, DCol.Normal, Fmt.SetLineCount(3))
							)
							+
							new TblLayoutNodeArray(additionalInputs),
							// The custom Logic builds the list of existing organizations on every state change.
							// TODO: Perhaps is should also do it on a failed Save (e.g. sql db create failed) which might not involve a state change.
							new Check2<string, Set<string>>(
								delegate (string name, Set<string> existingNames) {
									if (existingNames != null && existingNames.Contains(name))
										return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("There is already a saved organization with this name")));
									return null;
								}, TblActionNode.Activity.Continuous)
								.Operand1(OrganizationNameId)
								.Operand2(ExistingOrganizationNamesId)
							, Init.OnLoadNew(new ControlTarget(CredentialsAuthenticationMethodId), new ConstantValue((int)Thinkage.Libraries.DBILibrary.AuthenticationMethod.WindowsAuthentication))
							, Init.OnLoadNew(new ControlTarget(CredentialsUsernameId), new ConstantValue(null))
							, Init.Continuous(new ControlReadonlyTarget(CredentialsUsernameId, KB.K("A username is not required for this authentication method"))
								, new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse, delegate (object[] inputs) {
									// determine if authentication method needs the password field
									return (inputs[0] != null &&
											(AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(int)) == AuthenticationMethod.WindowsAuthentication
										);
								}, new Libraries.Presentation.ControlValue(CredentialsAuthenticationMethodId)))
							, Init.OnLoadNew(new ControlTarget(CredentialsPasswordId), new ConstantValue(null))
							, Init.Continuous(new ControlReadonlyTarget(CredentialsPasswordId, KB.K("A password is not required for this authentication method"))
								, new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse, delegate (object[] inputs) {
								// determine if authentication method needs the password field
								return (inputs[0] != null &&
										!((AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(int)) == AuthenticationMethod.SQLPassword
									|| (AuthenticationMethod)(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(int)) == AuthenticationMethod.ActiveDirectoryPassword)
									);
							}, new Libraries.Presentation.ControlValue(CredentialsAuthenticationMethodId)))
							, Init.OnLoadNew(new ControlTarget(CompactBrowsersId), new ConstantValue(false))
							, Init.OnLoadNew(new ControlTarget(PreferredModeId), new ConstantValue((int)DatabaseEnums.ApplicationModeID.Normal))
							, EL == typeof(NewCreateDatabaseWithLicensesOrganizationEditorLogic) ?
								Init.Continuous(new ControlTarget(HtmlDisplayId), new EditorCalculatedInitValue(StringTypeInfo.Universe, GetLicenseDisplay,
									new ControlValue(EmailForLicenseId),
									new ControlValue(OrganizationNameId),
									new ControlValue(ServerNameId),
									new ControlValue(DatabaseNameId)
									))
									:
									null
							, EL == typeof(NewCreateDatabaseWithLicensesOrganizationEditorLogic) ?
								new Check2<string, string>(
									delegate (string licenseType, string emailAddress) {
										if (!String.IsNullOrEmpty(licenseType)) {  // no check if no license being requested
											try {
												if (String.IsNullOrEmpty(emailAddress))
													throw new System.FormatException();
												var se = Thinkage.Libraries.Mail.MailAddress(emailAddress);
											}
											catch (System.FormatException) {
												return new EditLogic.ValidatorAndCorrector.ValidatorStatus(1, new GeneralException(KB.K("A valid email address is required to send the full license to you.")));
											}
										}
										return null;
									}, TblActionNode.Activity.Continuous).Operand1(LicensePickerId).Operand2(EmailForLicenseId)
								: null
					);
				}
			);
		}
		private static StringTypeInfo PasswordInputTypeInfo = new StringTypeInfo(0, 32, 0, true, true, true);
		#endregion
		#region New Organization Editor Logic
		private class NewOrganizationEditorLogic : EditLogic {
			private IBasicDataControl existingNamesControl;
			public NewOrganizationEditorLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
				EditStateChanged += delegate () {
					FillExistingOrganizationsNamesList();
				};
			}
			public override void EndSetup() {
				existingNamesControl = ((EditLogic.TblLayoutNodeInfo)TblLeafNodeInfoCollection.FindById(ExistingOrganizationNamesId)).UncheckedControl;
				base.EndSetup();
			}
			protected void FillExistingOrganizationsNamesList() {
				Set<string> list = null;
				SqlExpression filter = null;
				if (State != null && State.EditRecordState == EditRecordStates.Existing)
					// If not in New mode the query must exclude the current edit record.
					filter = new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.Id).NEq(SqlExpression.Constant(RootRowIDs[0]));

				using (var ds = new dsSavedOrganizations(DB)) {
					DB.ViewOnlyRows(ds, dsSavedOrganizations.Schema.T.Organizations, filter, null, null);
					list = new Set<string>(ds.T.Organizations.Rows.Cast<dsSavedOrganizations.OrganizationsRow>().Select(r => r.F.OrganizationName), StringComparer.InvariantCultureIgnoreCase); // Use a set with caseless string compare.
				}
				// Even though the new set might be equal to the old set, the cost of comparing the sets it probably greater than the cost of triggering the Check operation which does a single membership test.
				existingNamesControl.Value = list;
			}
		}
		#endregion
		#region Create Organization (base)
		private abstract class CreateOrganizationEditorLogic : NewOrganizationEditorLogic {
			public CreateOrganizationEditorLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
			}
			protected virtual void PostCloseEditorAction() {
			}
			#region Command setup
			abstract protected Key CreateCommandCaption {
				get;
			}
			protected override void SetupCommands() {
				base.SetupCommands();
				CommandGroupDeclarationsInOrder.Remove(NewCommandGroup);
				NewCommandGroup = null;
				CommandGroupDeclarationsInOrder.Remove(CloneCommandGroup);
				CloneCommandGroup = null;
				CommandGroupDeclarationsInOrder.Remove(PrintCommandGroup);
				PrintCommandGroup = null;
				CommandGroupDeclarationsInOrder.Remove(SaveCommandGroup);
				SaveCommandGroup = null;
				SaveAndCloseCommandGroup.Clear();
				SaveAndCloseCommandGroup.Add(new CommandDeclaration(CreateCommandCaption,
					EditLogic.StateTransitionCommand.NewSameTargetState(this, KB.K("Create the database and close this editor"),
					delegate () {
						SaveAction(NormalPostSaveState);
						CloseEditorAction();
						PostCloseEditorAction();
					},
					StateNewChanged)));
			}
			#endregion
			protected MB3Client.IMBConnectionParameters GetConnectionDefinition() {
				return new MB3Client.MBConnectionDefinition(DatabaseEnums.ApplicationModeID.Administration, false, (string)GetControlNotifyingSource(ServerNameId).GetValue(), (string)GetControlNotifyingSource(DatabaseNameId).GetValue(),
					new AuthenticationCredentials(
						GetCreditionalsAuthenticationMethod(this),
						(string)GetControlNotifyingSource(CredentialsUsernameId).GetValue(),
						(string)GetControlNotifyingSource(CredentialsPasswordId).GetValue()
						));
			}
			// This intercepts the save cycle and first does a DB create and post-processing, and also has error handling to drop the DB
			protected override object[] SaveRecord(Server.UpdateOptions updateOptions) {
				if (State.EditRecordState == EditRecordStates.New) {
					CreateDatabase();   // otherwise the user has done a "save & stay in edit mode"
				}
				return base.SaveRecord(updateOptions);
			}
			abstract protected Key ProgressCaption();
			abstract protected void CreateDatabase();
			protected virtual void PostCreateProcessing(MB3Client db, IProgressDisplay ipd) {
			}
			protected void ReplaceActiveApplicationWithAdministrationApplication() {
				var junk = new NamedOrganization((string)GetControlNotifyingSource(OrganizationNameId).GetValue(), new MB3Client.MBConnectionDefinition(
					DatabaseEnums.ApplicationModeID.Administration,
					(bool)GetControlNotifyingSource(CompactBrowsersId).GetValue(),
					(string)GetControlNotifyingSource(ServerNameId).GetValue(), (string)GetControlNotifyingSource(DatabaseNameId).GetValue(),
					new AuthenticationCredentials(
						GetCreditionalsAuthenticationMethod(this),
						(string)GetControlNotifyingSource(CredentialsUsernameId).GetValue(),
						(string)GetControlNotifyingSource(CredentialsPasswordId).GetValue()
						)));
				var app = MainBossApplication.CreateMainBossApplication(junk);
				if (app == null)
					throw new GeneralException(KB.K("Application mode {0} is unknown"), DatabaseEnums.ApplicationModeID.Normal);
				PickOrganizationApplication.ReplaceActiveApplication(app);
			}
		}
		#endregion
		#region New Organization (Empty)
		private class NewCreateDatabaseWithLicensesOrganizationEditorLogic : CreateOrganizationEditorLogic {
			public NewCreateDatabaseWithLicensesOrganizationEditorLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
			}
			protected override Key CreateCommandCaption {
				get {
					return KB.K("Create Empty Database");
				}
			}
			protected override Key ProgressCaption() {
				return KB.K("Creating empty database");
			}
			protected override void PostCloseEditorAction() {
				ReplaceActiveApplicationWithAdministrationApplication();
			}
			protected override void CreateDatabase() {
				IProgressDisplay ipd = null;
				List<License> licenses = new List<License>();
				string licenseHtml = null;
				for (;;) {
					try {
						licenseHtml = GetLicenses(); // try to get the licenses BEFORE creating any database in case of error retrieving
						if (licenseHtml == null)
							break; // no licenses provided by design (otherwise exception would have been thrown)
						var matches = LicenseProvisionSuccess.Matches(licenseHtml);
						if (matches.Count != 1)
							throw new GeneralException(KB.T(licenseHtml));
						licenses.AddRange(Thinkage.Libraries.Licensing.License.FindLicensesInText(licenseHtml));
						break;
					}
					catch (GeneralException e) {
						switch (Ask.AbortRetryIgnore(Thinkage.Libraries.Exception.FullMessage(e))) {
						case Ask.Result.Abort:
							throw;
						case Ask.Result.Retry:
							continue;
						case Ask.Result.Ignore:
							break;
						}
					}
				}
				try {
					ipd = CommonUI.UIFactory.CreateProgressDisplay(ProgressCaption(), -1);
					MB3Client.CreateDatabase(GetConnectionDefinition(), (string)GetControlNotifyingSource(OrganizationNameId).GetValue(), delegate (MB3Client db) {
						// Add the default licenses if any exist
						if (licenses.Count > 0)
							DatabaseCreation.AddLicenses(db, licenses.ToArray());
						PostCreateProcessing(db, ipd);
						// Display the licenseHtml text in a web page here ?
					}, ipd);
				}
				finally {
					if (ipd != null)
						ipd.Complete();
				}
			}
			// The pattern to search the license provision HTML page for to signify success
			private static readonly System.Text.RegularExpressions.Regex LicenseProvisionSuccess = new System.Text.RegularExpressions.Regex(KB.I("class=\"LicenseKey\""),
				System.Text.RegularExpressions.RegexOptions.Multiline
				| System.Text.RegularExpressions.RegexOptions.ExplicitCapture);

			private string GetLicenses() {
				GeneralException gex = null;
				Libraries.DataFlow.Source licenseType = this.GetControlNotifyingSource(LicensePickerId);
				System.Net.WebClient webFetch = null;
				string licenseHtml = null;
				if (!String.IsNullOrEmpty((string)licenseType.GetValue())) {
					Libraries.DataFlow.Source organization = this.GetControlNotifyingSource(OrganizationNameId);
					Libraries.DataFlow.Source emailAddress = this.GetControlNotifyingSource(EmailForLicenseId);
					Libraries.DataFlow.Source dbname = this.GetControlNotifyingSource(DatabaseNameId);
					Libraries.DataFlow.Source dbserverSource = this.GetControlNotifyingSource(ServerNameId);
					string dbserver = (string)dbserverSource.GetValue();
					try {
						dbserver = System.Net.Dns.GetHostEntry(dbserver).HostName;
					}
					catch (System.Exception) {
					}

					System.Text.StringBuilder queryString = new System.Text.StringBuilder();
					var parameters = new Dictionary<string, string>();
					parameters.Add(EmailAddressLabel.IdentifyingName, (string)emailAddress.GetValue());
					parameters.Add(LicensesToRequestLabel.IdentifyingName, (string)licenseType.GetValue());
					parameters.Add(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName.Key().IdentifyingName, (string)organization.GetValue());
					parameters.Add(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer.Key().IdentifyingName, dbserver);
					parameters.Add(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName.Key().IdentifyingName, (string)dbname.GetValue());
					LicenseInformationToSend.ToList().ForEach(x => parameters.Add(x.Key.IdentifyingName, x.Value));
					string format = "?{0}={1}";
					foreach (var r in parameters) {
						queryString.Append(Strings.IFormat(format, r.Key.Replace(" ", string.Empty), Uri.EscapeUriString(r.Value)));
						format = "&{0}={1}";
					}
					try {
						webFetch = new System.Net.WebClient();
						webFetch.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
						// TODO: Convert the dictionary of other information to query parameters ....
						var uri = new Uri(Strings.IFormat("https://www.mainboss.com/GetLicense.shtml{0}", queryString.ToString()));
						licenseHtml = webFetch.DownloadString(uri);
						if (String.IsNullOrEmpty(licenseHtml))
							throw new GeneralException(KB.K("The license provider returned an empty response"));
					}
					catch (System.Exception e) {
						throw gex = new GeneralException(e, KB.K("Licenses were not received from the MainBoss license provider."));
					}
				}
				return licenseHtml;
			}
		}
		#endregion
		#region NewOrganization From Backup EditorLogic
		private class NewOrganizationFromBackupEditorLogic : CreateOrganizationEditorLogic {
			public NewOrganizationFromBackupEditorLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
			}
			protected override Key CreateCommandCaption {
				get {
					return KB.K("Create Database from Backup");
				}
			}

			protected override void CreateDatabase() {
				IProgressDisplay ipd = null;
				try {
					ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Creating database from backup file"), -1);
					MB3Client.CreateDatabaseFromBackup(GetConnectionDefinition().Connection as MB3Client.ConnectionDefinition,
						(string)GetControlNotifyingSource(InputFileId).GetValue(),
						(int?)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(GetControlNotifyingSource(BackupSetId).GetValue(), typeof(int?)), ipd);
				}
				finally {
					if (ipd != null)
						ipd.Complete();
				}
			}
			protected override Key ProgressCaption() {
				return KB.K("Creating new database from backup file");
			}
		}
		#endregion
		#region NewOrganization From29Data EditorLogic
		private class NewOrganizationFrom29DataEditorLogic : CreateOrganizationEditorLogic {
			public NewOrganizationFrom29DataEditorLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
			}
			protected override Key CreateCommandCaption {
				get {
					return KB.K("Create Database using 2.9 Export Data");
				}
			}

			protected override void PostCloseEditorAction() {
				ReplaceActiveApplicationWithAdministrationApplication();
			}
			static Key importErrorMessage = KB.K("MainBoss 2.9 data import has errors");
			protected override void CreateDatabase() {
				IProgressDisplay ipd = null;
				xmlInputReader = null;
				HistoryLogText = new System.Text.StringBuilder();
				string xmlInput = (string)GetControlNotifyingSource(InputFileId).GetValue();
				var connectParameters = GetConnectionDefinition();
				connectInfo = connectParameters.Connection as MB3Client.ConnectionDefinition;
				GeneralException gex = null;
				bool promptToDelete = false;
				bool throwErrorOnExit = false;
				try {
					ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Creating database from MB2.9 export data"), -1);
					xmlInputReader = new System.IO.FileStream(xmlInput, System.IO.FileMode.Open, System.IO.FileAccess.Read);
					TheConverter = new Thinkage.MainBoss.MB29Conversion.MBConverter((SqlClient.Connection)connectInfo.ConnectionInformation, null, connectInfo.DBName);
					ipd.Update(KB.K("Loading MainBoss 2.9 import data"));
					TheConverter.Load29XMLData(xmlInputReader, delegate (object sender, System.Xml.Schema.ValidationEventArgs e) {
						HistoryLogText.Append(separator);
						HistoryLogText.Append(Strings.Format(KB.K("Line {0}, Position {1}"), e.Exception.LineNumber, e.Exception.LinePosition));
						HistoryLogText.Append(Environment.NewLine);
						HistoryLogText.AppendLine(e.Exception.Message); // only use the Xml validation message; the inner exception just repeats the same message again
					});
					if (HistoryLogText.Length == 0) { // no errors on import, proceed
						ipd.Update(KB.T(Strings.Format(KB.K("Creating database {0} on database server {1}"), connectInfo.DBName, connectInfo.DBServer)));
						promptToDelete = true; // we made it this far so a database may exist to be deleted on further errors
						MB3Client.CreateDatabase(connectParameters, (string)GetControlNotifyingSource(OrganizationNameId).GetValue(), delegate (MB3Client db) {
							PostCreateProcessing(db, ipd);
							if (HistoryLogText.Length == 0)
								MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(db).LogHistory(db, KB.K("MainBoss 2.9 data imported").Translate(), null);
							else {
								gex = new GeneralException(importErrorMessage);
								MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(db).LogHistory(db, KB.K("MainBoss 2.9 data imported with errors").Translate(), HistoryLogText.ToString());
							}
						}, ipd);
					}
					else
						gex = new GeneralException(importErrorMessage);
				}
				catch (System.Exception ex) {
					gex = new GeneralException(ex, importErrorMessage);
					Thinkage.Libraries.Exception.AddContext(gex, new Thinkage.Libraries.MessageExceptionContext(KB.K("MB2.9 data source {0}"), xmlInput));
				}
				finally {
					if (gex != null) {
						if (HistoryLogText.Length > 0)
							Thinkage.Libraries.Exception.AddContext(gex, new Thinkage.Libraries.MessageExceptionContext(KB.T(HistoryLogText.ToString())));
						// Display the error results
						ipd.ErrorAndWait(null, gex);
						throwErrorOnExit = true; // unless the user wants to keep the database for analysis, we will throw an exception to not 'save' the organization
						if (promptToDelete) {
							throwErrorOnExit = Ask.Question(Strings.Format(KB.K("Do you want to keep the database {0} on database server {1}"), connectInfo.DBName, connectInfo.DBServer)) == Ask.Result.No;
							if (throwErrorOnExit)
								connectInfo.ConnectionInformation.CreateServer().DeleteDatabase(connectInfo.ConnectionInformation);
						}
					}
					if (ipd != null)
						ipd.Complete();
					if (xmlInputReader != null)
						xmlInputReader.Close();
					HistoryLogText = null;
				}
				if (throwErrorOnExit)
					throw new GeneralException(KB.K("Unable to import organization into database {0} on server {1}"), connectInfo.DBName, connectInfo.DBServer);
			}
			System.IO.FileStream xmlInputReader;
			Thinkage.MainBoss.MB29Conversion.MBConverter TheConverter;
			System.Text.StringBuilder HistoryLogText;
			MB3Client.ConnectionDefinition connectInfo;

			// TODO: When done open in admin mode so licenses can be added.
			protected override void PostCreateProcessing(MB3Client db, IProgressDisplay ipd) {
				// Fill the database from 2.9 import data
				TheConverter.TableConverting += delegate (object source, TableConvertEventArgs e) {
					ipd.Update(KB.T(Strings.Format(KB.K("Import Conversion Step {0}{1}"), e.TableName, Environment.NewLine)));
				};
				TheConverter.TableConverted += delegate (object source, TableConvertEventArgs e) {
					if (!String.IsNullOrEmpty(e.Messages)) {
						HistoryLogText.Append(separator);
						HistoryLogText.Append(Strings.Format(KB.K("Import Conversion Step {0} Errors{1}"), e.TableName, Environment.NewLine));
						HistoryLogText.Append(separator);
						HistoryLogText.Append(e.Messages);
					}
				};
				TheConverter.Convert29();
			}
			private readonly static string separator = Strings.IFormat("*****************************************************************{0}", Environment.NewLine);
			protected override Key ProgressCaption() {
				return KB.K("Creating new database from data exported from MainBoss Basic");
			}
		}
		#endregion
		#region SelectServerTbl
		private static Tbl SelectServerTbl = new Tbl(dsSqlServers.Schema.T.SqlServers, TId.SQLServer,
			new Tbl.IAttr[] {
				new CustomSessionTbl(delegate(XAFClient existingDBAccess, DBI_Database newSchema) { return new SqlServersSession.Connection(); }),
				new BTbl(
					BTbl.ListColumn(dsSqlServers.Path.T.SqlServers.F.Name),
					BTbl.ListColumn(dsSqlServers.Path.T.SqlServers.F.Version)
				)
			},
			new TblLayoutNodeArray()
		);
		#endregion
		#region SelectDatabaseTbl
		private static Tbl SelectDatabaseTbl = new Tbl(dsDatabasesOnServer.Schema.T.DatabasesOnServer, TId.SQLDatabase,
			new Tbl.IAttr[] {
				new CustomSessionTbl(delegate(XAFClient existingDBAccess, DBI_Database newSchema) { return new DatabasesOnServerSession.Connection(); }),
				new BTbl(
					BTbl.SetShowBrowserPanel(false),
					BTbl.ListColumn(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.ServerName),
					BTbl.ListColumn(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.Database),
					BTbl.ListColumn(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.Version),
					BTbl.ListColumn(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.OrganizationName),
					BTbl.ListColumn(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.UserRecordExists),
					BTbl.ListColumn(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.AccessError),
					BTbl.TaggedEqFilter(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.ServerName, DatabasePickerServerFilterId, true)
				)
			},
			new TblLayoutNodeArray(
				TblColumnNode.New(dsDatabasesOnServer.Path.T.DatabasesOnServer.F.AccessError, DCol.Normal)
			)
		);
		#endregion
		#region Web Panel Support
		HtmlDisplaySettings GetHtmlSettings() {
			// Determine the initial URL to display to the user
			string defaultURL = Strings.IFormat("{0}/{1}", WebInfoRoot, "default.htm");
			var uri = new System.Uri(defaultURL);
			return new HtmlDisplaySettings(uri) { ValueIsURI = true, SuppressScriptErrors = true, IsWebBrowserContextMenuEnabled = InitialStartupSettings.IsWebBrowserContextMenuEnabled };
		}
		private string WebInfoRoot;
		private HtmlDisplaySettings InitialStartupSettings = new HtmlDisplaySettings();
		#region GetWebDeployment
		public static string GetWebDeploymentLocation(out bool isnetDeployed) {
			string version = Strings.IFormat("{0}.{1}.{2}", Thinkage.Libraries.VersionInfo.ProductVersion.Major, Thinkage.Libraries.VersionInfo.ProductVersion.Minor, Thinkage.Libraries.VersionInfo.ProductVersion.Build);
			string webRoot;
			try {
				if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
					// Determine the root of our application from the UpdateLocation Uri (where our .application is stored)
					webRoot = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.UpdateLocation.GetLeftPart(System.UriPartial.Path).Replace(KB.I("/mainboss.application"), "");
					isnetDeployed = true;
				}
				else {
					webRoot = Strings.IFormat("file://{0}/www", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
					version = "version";
					isnetDeployed = false;
				}
			}
			catch (System.Exception) {
				isnetDeployed = true;
				throw;
			}
			return Strings.IFormat("{0}/{1}/{2}", webRoot, version, System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
		}
		#endregion
		#endregion
	}
}
