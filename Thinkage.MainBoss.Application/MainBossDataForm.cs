using System;
using System.Drawing;
using System.Text;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;
using System.Collections.Generic;

namespace Thinkage.MainBoss.Application {
	using Thinkage.MainBoss.Controls;
	public abstract class MainBossDataForm : DataForm, ITagDataReceiptHandler {
		#region Classes for menu customization
		public class MainMenuCustomizationForm : DataForm {
			protected MainMenuCustomizationForm(MainMenuCustomizationControl vc)
				: base(vc.UIFactory, vc) {
				MainCustomizerControl = vc;
				Caption = KB.K("Customize Main Menu");
				// The following handles the user pressing Esc or using the 'x' to close a window. This Cancel button however is not put into the form; that is left up to the Control to build whatever
				// cancel/close button operation is required.
				CancelButton = MainCustomizerControl.UIFactory.CreateButton(KB.K("Cancel"), new CloseFormCommand(KB.K("Cancel customization"), this, UIDialogResult.Cancel));
			}
			public readonly MainMenuCustomizationControl MainCustomizerControl;
			public static void RunFormCustomizationForm(UIFactory uIFactory) {
				new MainMenuCustomizationForm(new MainMenuCustomizationControl(uIFactory)).ShowModal();
			}
			public override string GetHelpContext() {
				return Libraries.Presentation.KB.HelpTopicKey("MainMenuCustomizationForm");
			}
			protected override void OnClosing(FormClosingEventArgs ea) {
				base.OnClosing(ea);
				// Give the underlying common control to cancel the close operation.
				if (!ea.Cancel && ea.CloseReason == FormClosingEventArgs.CloseReasons.UserClose)
					MainCustomizerControl.OnControlClosing(ea);
			}
		}
		public class MainMenuCustomizationControl : PanelProxy {
			private class Node : TextTreeViewManager.Node, IDisablerProperties {
				public Node(Key showText, [Invariant]string customizationText) {
					Text = showText.Translate();
					CustomizationText = customizationText;
					Enabled = true;
				}
				public Node(Key showText) {
					Text = showText.Translate();
					CustomizationText = Strings.IFormat("Internal Node {0}", InternalNodeIndex++);
					Enabled = false;
				}
				private static int InternalNodeIndex;
				private readonly string CustomizationText;
				protected override object ListId {
					get {
						return CustomizationText;
					}
				}
				public Key Tip {
					get { return KB.K("Container nodes are made visible as required by their contents"); }
				}
				public bool Enabled { get; set; }
				public event IEnabledChangedEvent EnabledChanged { add { } remove { } }
			}
			public MainMenuCustomizationControl(UIFactory uiFactory)
				: base(uiFactory.CreatePanel(null)) {
				UIFactory = uiFactory;
				SetupCenterColumnPanel();

				// The value we give to each leaf node it its customization path.
				List = TblDrivenMainBossApplication.Instance.QueryInterface<UIFactory>().CreateList(
					new Libraries.TypeInfo.SetTypeInfo(false, Libraries.TypeInfo.StringTypeInfo.NonNullUniverse),
					null,
					UIListStyles.ListPresentation | UIListStyles.ExplicitMultipleRowValue | UIListStyles.ExcludeDisabledRowsFromValue,
					changeEnabler: null);
				Add(List);
				var manager = new TextTreeViewManager(UIFactory, (ITreeStructuredListComboControl)List);
				manager.UseDistinctListIdDictionary(Libraries.TypeInfo.StringTypeInfo.NonNullUniverse);

				FillNode(new StringBuilder(), manager.RootNode, new[] { new ControlPanelLayout().OverallContent() });

				SetupButtons();
				AddButtonPanel();

				List.Notify += delegate () {
					ChangesExist = true;
				};

				LoadLeafNodeVisibility();
			}
			private void FillNode(StringBuilder customizationName, TreeViewManager.Node container, MenuDef[] source) {
				int originalLen = customizationName.Length;
				customizationName.Append('/');
				for (int i = 0; i < source.Length; i++) {
					MenuDef item = source[i];
					if (item == null || item.name == null)
						continue;
					customizationName.Append(item.name.IdentifyingName);
					Node nodeForItem = null;
					if (item.ItemVisible
						&& (item is BrowseMenuDef
							|| item is ReportMenuDef
							|| (item is CommandMenuDef && ((CommandMenuDef)item).menuCommand != null))) {
						// It has activity in its own right, so we want a checkbox for it.
						nodeForItem = new Node(item.name, customizationName.ToString());
						AllMenuCustomizationNames.Add(customizationName.ToString());
					}
					else
						// All it is is a potential container
						nodeForItem = new Node(item.name);

					if (item.subMenu != null) {
						// Add subcontents
						nodeForItem.Expanded = false;
						FillNode(customizationName, nodeForItem, item.subMenu);
					}

					if (nodeForItem.Count > 0 || nodeForItem.Enabled)
						// The node has children and/or is content unto itself, so add it to our parent.
						container.Add(nodeForItem);
					customizationName.Length = originalLen + 1;
				}
				customizationName.Length = originalLen;
			}
			#region CreateButtons Command buttons
			protected void SetupButtons() {
				AddButton(UIFactory.CreateButton(KB.K("Apply"), new MultiCommandIfAllEnabled(new CallDelegateCommand(delegate () {
					SaveLeafNodeVisibility();
				}), NothingToApplyDisabler)));
				AddButton(UIFactory.CreateButton(KB.K("Apply && Close"), new MultiCommandIfAllEnabled(new CallDelegateCommand(delegate () {
					SaveLeafNodeVisibility();
					CloseForm();
				}), NothingToApplyDisabler)));
				var cancelClose = UIFactory.CreateDropDownButton();
				cancelClose.AddCommand(KB.K("Close"), new MultiCommandIfAllEnabled(new CallDelegateCommand(delegate () {
					CloseForm();
				}), YouHaveChangesToApplyDisabler));
				cancelClose.AddCommand(KB.K("Cancel"), new MultiCommandIfAllEnabled(new CallDelegateCommand(delegate () {
					LoadLeafNodeVisibility();
				}), NothingToApplyDisabler));
				AddButton(cancelClose);
			}
			protected void CloseForm() {
				var ParentForm = ContainingForm;
				if (ParentForm != null)
					ParentForm.CloseForm(UIDialogResult.OK);
			}
			#endregion
			#region - Button Panel construction
			protected void AddButton(IContainable ctrl) {
				if (ButtonPanel == null)
					ButtonPanel = new ButtonRowPanel(UIFactory, ButtonControlSpacing);

				ButtonPanel.Add(ctrl);
			}
			protected void AddButtonPanel() {
				AddButtonPanel(this);
			}
			protected void AddButtonPanel(UIPanel toWhom) {
				if (ButtonPanel != null)
					toWhom.Add(ButtonPanel);
			}
			#endregion
			#region Command Group Handling
			/// <summary>
			/// Flag to indicate changes have been made and should be saved (for Cancel/Close)
			/// </summary>
			public bool ChangesExist {
				get {
					return pChangesExist;
				}
				set {
					pChangesExist = value;
					NothingToApplyDisabler.Enabled = pChangesExist;
					YouHaveChangesToApplyDisabler.Enabled = !pChangesExist;
				}
			}
			private bool pChangesExist;
			public SettableDisablerProperties NothingToApplyDisabler = new SettableDisablerProperties(null, KB.K("You have no customizations to apply"), false);
			public SettableDisablerProperties YouHaveChangesToApplyDisabler = new SettableDisablerProperties(null, KB.K("You have unapplied customizations"), false);
			#endregion
			public readonly UIFactory UIFactory;
			protected const int ButtonControlSpacing = 8;   // This is the spacing used between buttons in a row.
			private ButtonRowPanel ButtonPanel;             // The command-button panel if any
			private Set<string> AllMenuCustomizationNames = new Set<string>();
			private readonly IListComboDataControlWithExplicitMultipleRowValue List;
			private void SaveLeafNodeVisibility() {
				IApplicationWithCustomizations customizer = TblDrivenMainBossApplication.Instance.QueryInterface<IApplicationWithCustomizations>();
				var selected = (Set<object>)List.Value;
				foreach (string s in AllMenuCustomizationNames)
					customizer.SetMainMenuVisibility(s, selected.Contains(s));
				customizer.FormCustomizationChanged();
				ChangesExist = false;
			}
			private void LoadLeafNodeVisibility() {
				IApplicationWithCustomizations customizer = TblDrivenMainBossApplication.Instance.QueryInterface<IApplicationWithCustomizations>();
				Set<object> enabledEntries = new Set<object>(((Libraries.TypeInfo.SetTypeInfo)List.TypeInfo).ElementType);
				foreach (string s in AllMenuCustomizationNames)
					if (customizer.GetMainMenuVisibility(s))
						enabledEntries.Add(s);
				List.Value = enabledEntries;
				ChangesExist = false;
			}
			public void OnControlClosing(FormClosingEventArgs e) {
				if (ChangesExist) {
					// If we are here it is because one of the following occurred:
					// - the user clicked on the 'X' box of the form
					// - because the user selected the form's system-menu Close item
					// - the user hit ALT+F4
					// - the user hit ESCAPE whic which "clicks" the Form.Cancel button (which is actually a non-visible button created by CommonFormCustomizationForm)
					// - causing the Close() method to be called on the Form.
					//
					// We can't get here from the actual Close/Cancel button because the latter command does not close the form, and the former is only enabled if !userChanges
					switch (Ask.QuestionWithCancel(KB.K("Do you want to apply your changes before closing?").Translate())) {
					case Ask.Result.Yes:
						try {
							SaveLeafNodeVisibility();
						}
						catch (System.Exception ex) {
							TblDrivenMainBossApplication.Instance.DisplayError(ex);
							e.Cancel = true;
						}
						break;
					case Ask.Result.No:
						break;
					case Ask.Result.Cancel:
						e.Cancel = true;
						break;
					}
				}
			}
		}
		#endregion
		abstract protected IContainableMenuItem BuildSessionMenu();
		abstract protected MainBossDataForm CopySelf();
		// the first organization name is the one selected by the user on the command line or from the "saved organizations"; it is the user's personal name for the organization.
		// the second organization name is the one from the DB variable; it is what the organization calls itself.
		abstract protected void SetStatusAndCaption(string organizationName, string dbOrganizationName, string userName);
		protected UIFactory UIFactory { get; private set; }
		/// <summary>
		/// To detect OnClosing whether to warn
		/// </summary>
		private SettableDisablerProperties NoUnPublishedCustomizationsExistDisabler = new SettableDisablerProperties(null, KB.K("No unpublished form customizations exist"), false);
		#region Constructor
		public MainBossDataForm(UIFactory uiFactory, MB3Client db)
			: base(uiFactory, new TreeViewExplorer(uiFactory, Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.XAF.UI.MSWindows.IGUIApplicationIdentification>().DefaultFormIcon, TblDrivenMainBossApplication.Instance.AppCustomizationsMixIn)) {
			UIFactory = uiFactory;
			myPreferredSize = new System.Drawing.Size((int)(UIFactory.FormParameters.WorkingArea.Width * .75), (int)(UIFactory.FormParameters.WorkingArea.Height * .75));
			// The Status bar contains an infinite (naturally) value so the main form will grow to any width the user chooses.
			// We mark the TreeView explorer to fill that space as well.
			FormContents.Arranger.HorizontalInfiniteGrowth = true;
			DB = db;
			MainControlPanel = (TreeViewExplorer)FormContents;
			MainControlPanel.SetSettingsControlContainerToTitleBar();
			string organizationName = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.OrganizationName;
			GetIdentificationForDisplay(out string dbOrganizationName, out string userName);
			SetStatusAndCaption(organizationName, dbOrganizationName, userName);

			TblDrivenMainBossApplication.Instance.AppConnectionMixIn.PermissionsReloadNotify += HandlePermissionsReloadEvent;
			TblDrivenMainBossApplication.Instance.AppCustomizationsMixIn.FormCustomizationChangeNotify += HandleFormCustomizationChangeEvent;
#if DEBUG
			bool showDebug = Thinkage.Libraries.Application.Instance.DebugProvider.ShowDebug;
			Thinkage.Libraries.Application.Instance.DebugProvider.ShowDebug = false;    // during initialization
#endif
			#region menu construction
			ViewMenu = BuildViewMenu();
			ViewMenuOriginalCount = ViewMenu.Count;
			Menu = UIFactory.CreateMainMenu(
				BuildSessionMenu(),
				ViewMenu,
				BuildActionMenu(),
				BuildHelpMenu()
				);

			MainControlPanel.ZoomMenuItem = ZoomMenuItem;
			SetMainControlPanelMenu();
			// Set up the CompactBrowsersItem checked menu item.
			// We must be prepared to copy the value in either direction, in case there is more than one main form and one changes the setting,
			// all the other forms' checked menu item must also reflect the changed value.
			CompactBrowsersItem.Value = !TblDrivenMainBossApplication.Instance.GetInterface<IFormsPresentationApplication>().BrowsersHavePanel;
			Thinkage.Libraries.Application.Instance.GetInterface<IFormsPresentationApplication>().BrowsersHavePanelChanged += BrowsersHavePanelChangedNotification;
			CompactBrowsersItem.Notify += delegate () {
				bool changedValue = !(bool)CompactBrowsersItem.Value;
				if (TblDrivenMainBossApplication.Instance.GetInterface<IFormsPresentationApplication>().BrowsersHavePanel != changedValue)
					((FormsPresentationApplication)TblDrivenMainBossApplication.Instance.GetInterface<IFormsPresentationApplication>()).BrowsersHavePanel = changedValue;
			};
			#endregion

#if DEBUG
			Thinkage.Libraries.Application.Instance.DebugProvider.ShowDebug = showDebug;
#endif
		}
		private class DefaultThemeFromSchema {
			public readonly DBI_Table TableSchema;
			public readonly List<Tbl.TblIdentification> TableIds = new List<Tbl.TblIdentification>();
			public readonly System.Drawing.Color TitleBackground;
			public readonly System.Drawing.Color TitleForeground;
			public readonly System.Drawing.Font TitleFont;
			public DefaultThemeFromSchema(DBI_Table schema, System.Drawing.Color? bg = null, System.Drawing.Color? fg = null, System.Drawing.Font f = null) {
				TableSchema = schema;
				TitleBackground = bg ?? Thinkage.Libraries.Presentation.MSWindows.TreeViewExplorer.DefaultTitleBackgroundColour;
				TitleForeground = fg ?? Thinkage.Libraries.Presentation.MSWindows.TreeViewExplorer.DefaultTitleForegroundColour;
				TitleFont = f ?? Thinkage.Libraries.Presentation.MSWindows.TreeViewExplorer.DefaultTitleFont;
			}
			public DefaultThemeFromSchema AddTId(params Tbl.TblIdentification[] tableIds) {
				TableIds.AddRange(tableIds);
				return this;
			}
		}
		// Thinkage Colors used on main news panel and database status
		// 56,88,21       #385815 (dark green)
		// 255,235,160 #FFEBA0 (yellow)
		private static DefaultThemeFromSchema[] ThemeFromSchemas = {
			new DefaultThemeFromSchema( dsMB.Schema.T.DatabaseStatus, Color.FromArgb(56,88,21), Color.FromArgb(255,235,160)).AddTId(TId.MainBossOverview).AddTId(TId.MainBossNewsPanel),
			new DefaultThemeFromSchema( dsMB.Schema.T.WorkOrder, Color.ForestGreen).AddTId(TId.WorkOrder, TId.WorkOrderStateHistory, TId.WorkOrderStatus, TId.WorkOrderState),
			new DefaultThemeFromSchema( dsMB.Schema.T.PurchaseOrder, System.Drawing.Color.PaleVioletRed ).AddTId(TId.PurchaseOrder, TId.PurchaseOrderStateHistory, TId.PurchaseOrderStatus, TId.PurchaseOrderState),
			new DefaultThemeFromSchema( dsMB.Schema.T.Request, Color.PaleGoldenrod, Color.Black ).AddTId(TId.Request, TId.RequestStateHistory, TId.RequestStatus, TId.RequestState),
			new DefaultThemeFromSchema( dsMB.Schema.T.Item, Color.Blue ).AddTId(TId.Item),
			new DefaultThemeFromSchema( dsMB.Schema.T.ScheduledWorkOrder, Color.Violet).AddTId(TId.UnitMaintenancePlan),
			new DefaultThemeFromSchema( dsMB.Schema.T.AttentionStatus, Color.Green, Color.Yellow).AddTId(TId.MyAssignmentOverview),
			new DefaultThemeFromSchema( dsMB.Schema.T.LocationDerivations, Color.SeaGreen).AddTId(TId.Unit),
			new DefaultThemeFromSchema( dsMB.Schema.T.WorkOrderTemplate, Color.Sienna).AddTId(TId.Task)
		};
		private class MainBossDefaultProvider : SettableValueManager.DefaultProvider {
			public MainBossDefaultProvider(Tbl t) : base(t) {
			}
			public override object GetDefault(SettableValueManager.SettableValueDefinition definition) {
				DefaultThemeFromSchema dt = ThemeFromSchemas.FirstOrDefault(t => t.TableSchema == TblInfo.Schema) ?? ThemeFromSchemas.FirstOrDefault(t => t.TableIds.FirstOrDefault(x => x == TblInfo.Identification) != null);
				if (definition.Name == TreeViewExplorer.TitleBackgroundColourTheme) {
					return dt?.TitleBackground ?? definition.DefaultValue;
				}
				else if (definition.Name == TreeViewExplorer.TitleForegroundColourTheme) {
					return dt?.TitleForeground ?? definition.DefaultValue;
				}
				else if (definition.Name == TreeViewExplorer.TitleFontTheme) {
					var baseFont = dt?.TitleFont ?? (System.Drawing.Font)definition.DefaultValue;
					return new System.Drawing.Font(Configuration.RegularFontFamilyForDisplay, baseFont.Size + 4, baseFont.Style | FontStyle.Bold, baseFont.Unit, baseFont.GdiCharSet);
				}
				return base.GetDefault(definition);
			}
		}
		private void SetMainControlPanelMenu() {
			MainControlPanel.SetTreeViewMenu(delegate (MenuDef item) {
				if (item is BrowseMenuDef brdef)
					return new BrowseExplorer(UIFactory, brdef.Id, DB, new MainBossDefaultProvider(brdef.TblCreator.Tbl));
				if (item is ReportMenuDef repdef)
					return new ReportExplorer(UIFactory, repdef.Id, DB, new MainBossDefaultProvider(repdef.Tbl), repdef.UserTip);
				return null;
			},
				new ControlPanelLayout().OverallContent()
			);
			if (ViewMenu.Count > ViewMenuOriginalCount)
				ViewMenu.RemoveAt(ViewMenuOriginalCount);
			ISubMenu selectViewMenu = MainControlPanel.SelectViewMenu;
			if (selectViewMenu != null)
				ViewMenu.Add(selectViewMenu);
		}
		private void BrowsersHavePanelChangedNotification() {
			bool changedValue = !TblDrivenMainBossApplication.Instance.GetInterface<IFormsPresentationApplication>().BrowsersHavePanel;
			if ((bool)CompactBrowsersItem.Value != changedValue)
				CompactBrowsersItem.Value = changedValue;
		}

		protected void SetupMainBossForm(MainBossDataForm result) {
			result.myPreferredSize = Size;
			result.SetHiddenModeFromSiblingForm(this);
			SetMenuState(result.Menu, Menu);
			result.MainControlPanel.SetInitialState(MainControlPanel.GetState());
		}
		private void GetIdentificationForDisplay(out string organizationName, out string userName) {
			userName = String.Empty; // construct the display username from the User table by locating the record matching currentUserID
			System.Text.StringBuilder userToDisplay = new System.Text.StringBuilder();
			string contactCode = (string)DB.Session.ExecuteCommandReturningScalar(dsMB.Schema.T.Contact.F.Code.EffectiveType,
				new SelectSpecification(dsMB.Schema.T.User, new[] { new SqlExpression(dsMB.Path.T.User.F.ContactID.F.Code) }, new SqlExpression(dsMB.Path.T.User.F.Id).Eq(SqlExpression.Constant(TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordID)), null));
			if (contactCode != null)
				userToDisplay.Append(contactCode);
			string amendDisplay = null;
			//			if (DB.Session.UserIsSqlAdmin())
			//				amendDisplay = KB.K("SQL Administrator").Translate();
			if (DB.Session.CanManageUserCredentials()) {
				if (DB.Session.CanManageUserLogins())
					amendDisplay = KB.K("Login&User Administrator").Translate();
				else
					amendDisplay = KB.K("User Administrator").Translate();
			}
			if (amendDisplay != null) {
				if (userToDisplay.Length > 0)
					userToDisplay.Append(" ");
				userToDisplay.Append("(");
				userToDisplay.Append(amendDisplay);
				userToDisplay.Append(")");
			}
			userName = userToDisplay.ToString();
			using (dsMB ds = new dsMB(DB)) {
				DB.ViewAdditionalVariables(ds, dsMB.Schema.V.OrganizationName);
				organizationName = (string)ds.V.OrganizationName.Value;
			}
		}

		#endregion

		#region Tag Data (barcode) handling
		public void HandleTagData([Invariant] string scannedText) {
			try {
				TblDrivenMainBossApplication.Instance.BarcodeHandlerBrowser.Object.EditRecord(scannedText);
			}
			catch (System.Exception ex) {
				TblDrivenMainBossApplication.Instance.DisplayError(ex);
			}
		}
		#endregion
#if DEBUG
		protected Thinkage.MainBoss.Database.Security.RightSet UpdateSecurity() {
			return ((MainBossPermissionsManager)TblDrivenMainBossApplication.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager).CurrentRightSet;
		}
#endif
		private void HandlePermissionsReloadEvent() {
			// The nodes in the TreeView itself refresh themselves properly, but any IExplorerItems that have been built must be rebuilt;
			// the following call accomplishes this.
			MainControlPanel.RefreshTreeViewMenu();
		}
		private void HandleFormCustomizationChangeEvent() {
			TreeViewExplorer.TreeViewExplorerState currentState = MainControlPanel.GetState();
			SetMainControlPanelMenu();
			MainControlPanel.RefreshTreeViewMenu();
			MainControlPanel.SetInitialState(currentState);
			NoUnPublishedCustomizationsExistDisabler.Enabled = true;
		}
		private bool ClosingToPickOrganization = false;
		protected override void OnClosing(FormClosingEventArgs e) {
			base.OnClosing(e);
			// Give the underlying common control to cancel the close operation.
			if (e.CloseReason == FormClosingEventArgs.CloseReasons.UserClose && Thinkage.Libraries.Application.Instance.GetInterface<IApplicationExecution>().OnlyOneUIExists) {
				// Closing this form will exit the application.
				if (!e.Cancel && !ClosingToPickOrganization)
					e.Cancel = !CanCloseApplication();
			}
		}
		private bool CanCloseApplication() {
			if (NoUnPublishedCustomizationsExistDisabler.Enabled == true)
				switch (Ask.QuestionWithCancel(KB.K("Do you want to publish your form customizations before closing?").Translate())) {
				case Ask.Result.Yes:
					try {
						TblDrivenMainBossApplication.Instance.AppCustomizationsMixIn.Publish();
					}
					catch (System.Exception ex) {
						Thinkage.Libraries.Application.Instance.DisplayError(ex);
						return false;
					}
					break;
				case Ask.Result.No:
					// We set this so the user is not asked again when the main form actually closes.
					// TODO: Note that this is inconsistent with the fact that the customizations actually *do* contain unpublished changes.
					// Perhaps we should reload from published values here in case the user cancels out of quitting some other way.
					NoUnPublishedCustomizationsExistDisabler.Enabled = false;
					break;
				case Ask.Result.Cancel:
					return false;
				}
#if DEBUG
			if (Thinkage.Libraries.Diagnostics.DebugLeakDetection.Check(null, false) > 0)
				switch (Ask.Question(KB.T("DEBUG: Memory leaks have been recorded, which can be viewed using the \"Check Memory\" button of the Debug form. Do you want to quit anyway?").Translate())) {
				case Ask.Result.Yes:
					break;
				case Ask.Result.No:
					return false;
				}
#endif
			return true;
		}
		#region Dispose
		protected override void Dispose(bool disposing) {
			if (disposing) {
			}
			TblDrivenMainBossApplication.Instance.AppConnectionMixIn.PermissionsReloadNotify -= HandlePermissionsReloadEvent;
			TblDrivenMainBossApplication.Instance.AppCustomizationsMixIn.FormCustomizationChangeNotify -= HandleFormCustomizationChangeEvent;
			Thinkage.Libraries.Application.Instance.GetInterface<IFormsPresentationApplication>().BrowsersHavePanelChanged -= BrowsersHavePanelChangedNotification;
			base.Dispose(disposing);
		}
		#endregion
		#region Properties
		/// <summary>
		/// Initial application window starts at the application mainFormPreferredSize
		/// </summary>
		public override Size? InitialPreferredSize {
			get {
				return myPreferredSize;
			}
		}
		// Default is 3/4 size of user's working area on initial startup
		private Size myPreferredSize;

		protected readonly MB3Client DB;
		#endregion
		#region Help
		public override string GetHelpContext() {
			return MainControlPanel.GetHelpContext();
		}
		#endregion

		#region Menus
		private readonly ISubMenu ViewMenu;
		private readonly uint ViewMenuOriginalCount;
		protected readonly TreeViewExplorer MainControlPanel;
		private UICheckBoxMenu ZoomMenuItem;
		private UICheckBoxMenu CompactBrowsersItem;
		private UICheckBoxMenu EnableCustomizationsItem;

		protected IContainableMenuItem ExitOrCloseMenuItem() {
			return UIFactory.CreateCommandMenuItem(KB.K("Close"), new CallDelegateCommand(KB.K("Close this window"), new EventHandler(Close_Click)));
		}
		private ISubMenu BuildViewMenu() {
			#region FormCustomization
			ISubMenu customizeMenu = UIFactory.CreateSubMenu(KB.K("Form Customization"), (PermissionDisabler)TblDrivenMainBossApplication.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager.GetPermission(Root.Rights.Action.Customize)
				, EnableCustomizationsItem = UIFactory.CreateBoolCheckMenuItem(KB.K("Enable"), null)
				, UIFactory.CreateCommandMenuItem(KB.K("Customize Main Menu"), new CallDelegateCommand(KB.K("Select which entries in the main menu are visible"),
					delegate () {
						MainMenuCustomizationForm.RunFormCustomizationForm(UIFactory);
					}))
				, UIFactory.CreateCommandMenuItem(KB.K("Publish"), new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Publish all currently applied form customizations to the database for all users"),
					delegate () {
						IApplicationWithCustomizations customizer = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithCustomizations>();
						if (customizer != null) {
							((MainBossCustomizations)customizer).Publish();
							NoUnPublishedCustomizationsExistDisabler.Enabled = false;
						}
					}), NoUnPublishedCustomizationsExistDisabler))
				, UIFactory.CreateCommandMenuItem(KB.K("Restore from published"), new CallDelegateCommand(KB.K("Reset form customizations as currently published, discarding any unsaved changes you have made"),
					delegate () {
						IApplicationWithCustomizations customizer = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithCustomizations>();
						if (customizer != null) {
							((MainBossCustomizations)customizer).RestoreFromPublished();
							customizer.FormCustomizationChanged();
							NoUnPublishedCustomizationsExistDisabler.Enabled = false;
						}
					}))
				, UIFactory.CreateCommandMenuItem(KB.K("Clear all customization"), new CallDelegateCommand(KB.K("Remove all form customizations, discarding any unsaved changes you have made"),
					delegate () {
						IApplicationWithCustomizations customizer = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithCustomizations>();
						if (customizer != null) {
							((MainBossCustomizations)customizer).ClearAll();
							customizer.FormCustomizationChanged();
							NoUnPublishedCustomizationsExistDisabler.Enabled = true;
						}
					}))
				, UIFactory.CreateCommandMenuItem(KB.K("Export"), new CallDelegateCommand(KB.K("Export the current form customizations to an external file"),
					delegate () {
						IApplicationWithCustomizations customizer = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithCustomizations>();
						if (customizer != null)
							((MainBossCustomizations)customizer).Export(UIFactory);
					}))
				, UIFactory.CreateCommandMenuItem(KB.K("Replace from file"), new CallDelegateCommand(KB.K("Replace current form customizations from an external file, discarding any unsaved changes you have made"),
					delegate () {
						IApplicationWithCustomizations customizer = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithCustomizations>();
						if (customizer != null && ((MainBossCustomizations)customizer).Import(UIFactory, false)) {
							customizer.FormCustomizationChanged();
							NoUnPublishedCustomizationsExistDisabler.Enabled = true;
						}
					}))
				, UIFactory.CreateCommandMenuItem(KB.K("Add from file"), new CallDelegateCommand(KB.K("Add form customizations from an external file"),
					delegate () {
						IApplicationWithCustomizations customizer = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithCustomizations>();
						if (customizer != null && ((MainBossCustomizations)customizer).Import(UIFactory, true)) {
							customizer.FormCustomizationChanged();
							NoUnPublishedCustomizationsExistDisabler.Enabled = true;
						}
					}))

				);
			SettableDisablerProperties customizationsEnabler = TblDrivenMainBossApplication.Instance.GetInterface<IApplicationWithCustomizations>().Enabler;
			EnableCustomizationsItem.Value = customizationsEnabler.Enabled;
			EnableCustomizationsItem.Notify += delegate () {
				// When our menu item changes, set its value into the customizer's global enabler and make all forms update.
				// Preserve the value of NoUnPublishedCustomizationsExistDisabler.Enabled so this change does not prompt the user to publish changes.
				if ((bool)EnableCustomizationsItem.Value != customizationsEnabler.Enabled) {
					customizationsEnabler.Enabled = (bool)EnableCustomizationsItem.Value;
					bool publishingEnablerSetting = NoUnPublishedCustomizationsExistDisabler.Enabled;
					TblDrivenMainBossApplication.Instance.GetInterface<IApplicationWithCustomizations>().FormCustomizationChanged();
					NoUnPublishedCustomizationsExistDisabler.Enabled = publishingEnablerSetting;
				}
			};
			// We build a MultiDisablerIfAllEnabled so we only have a weak subscription to the app-global customizationsEnabler.
			MultiDisablerIfAllEnabled weakSubscriber = new MultiDisablerIfAllEnabled(customizationsEnabler);
			weakSubscriber.EnabledChanged += delegate () {
				// When the customizer's global enabler changes, set its value into our menu item.
				// Note that the code that changed the global enabler's setting also will cause a call for FormCustimizationChanged so we don't also have to call it.
				if ((bool)EnableCustomizationsItem.Value != weakSubscriber.Enabled)
					EnableCustomizationsItem.Value = weakSubscriber.Enabled;
			};
			#endregion
			return UIFactory.CreateSubMenu(KB.K("View"), null
				// TODO: Disable this item if the current treeview selection is not zoomable (requires altering the IExplorerCommand interface to
				// supply the information)
				, ZoomMenuItem = UIFactory.CreateBoolCheckMenuItem(KB.K("Zoom in on View"), null)
				, UIFactory.CreateCommandMenuItem(KB.K("Open in New Window"), new CallDelegateCommand(KB.K("Open a new main window showing the same data"), new EventHandler(OpenInNewWindow_Click)))
				, UIFactory.CreateCommandMenuItem(KB.K("Change Active Filter"), new CallDelegateCommand(KB.K("Change the number of days back that records are displayed"), new EventHandler(ChangeLastUpdateEditor)))
				, CompactBrowsersItem = UIFactory.CreateBoolCheckMenuItem(KB.K("Show compact browsers"), null)
				, customizeMenu
				);
		}
		private IContainableMenuItem BuildActionMenu() {
			// The Actions menu items are filled in depending on the current context.
			// The menu must have some content (the separator) or the containing menu is not created so no popup action can occur!
			ISubMenu result = UIFactory.CreateSubMenu(KB.K("Actions"), null,
				UIFactory.CreateMenuSeparator()
			);
			MainControlPanel.ActionsMenu = result;
			return result;
		}
		private IContainableMenuItem BuildHelpMenu() {
			return UIFactory.CreateSubMenu(KB.K("Help"), null
				, UIFactory.CreateCommandMenuItem(KB.K("Help"), ContextHelpCommand)
				, UIFactory.CreateCommandMenuItem(KB.K("Table of Contents"), new CallDelegateCommand(KB.K("Go to Help Table of Contents"), new EventHandler(HelpContents_Click)))
				, UIFactory.CreateCommandMenuItem(KB.K("Index"), new CallDelegateCommand(KB.K("Go to Help Index"), new EventHandler(HelpIndex_Click)))
				, UIFactory.CreateMenuSeparator()
				, UIFactory.CreateCommandMenuItem(KB.K("Get Technical Support"), new CallDelegateCommand(KB.K("Go to technical support information"), new EventHandler(Support_Click)))
				, UIFactory.CreateCommandMenuItem(KB.K("Start Support Connection"), new CallDelegateCommand(KB.K("Start the Teamviewer support connection with Thinkage"), new EventHandler((object s, EventArgs a) => {
					MBAboutForm.StartTeamviewer();
				})))
				, UIFactory.CreateMenuSeparator()
				, UIFactory.CreateCommandMenuItem(KB.K("About..."), new CallDelegateCommand(KB.K("Show information about this application"), new EventHandler(About_Click)))
				);
		}
		#endregion
		// NOTE that the Control Panel definitions are now in Thinkage.Libraries.MainBoss.ControlPanelLayout
		#region Startup/shutdown
		/// <summary>
		/// Set the new menu checked state equal to that of the given old menu
		/// </summary>
		private static void SetMenuState(IMenuItemContainer newItems, IMenuItemContainer oldItems) {
			for (int i = 0; i < newItems.Count; ++i) {
				var newm = newItems[i] as IBasicDataControl;
				if (oldItems[i] is IBasicDataControl oldm && newm != null && oldm.ValueStatus == null)
					newm.Value = oldm.Value;
				var nchildren = newItems[i] as IMenuItemContainer;
				if (oldItems[i] is IMenuItemContainer ochildren && nchildren != null)
					SetMenuState(nchildren, ochildren);
			}
		}

		#endregion
		#region Database management code
		protected void SetupDatabase(object sender, System.EventArgs e) {
			if (Thinkage.Libraries.Application.Instance.GetInterface<IApplicationExecution>().OnlyOneUIExists) {
				// We ask first because by the time we're in ReplaceActiveApplication it is too late to cancel out.
				if (CanCloseApplication())
					try {
						// Having asked, don't ask again.
						ClosingToPickOrganization = true;
						Thinkage.Libraries.Application.ReplaceActiveApplication(TblDrivenMainBossApplication.Instance.CreateSetupApplication());
					}
					finally {
						ClosingToPickOrganization = false;
					}
			}
			else
				Thinkage.Libraries.Application.Instance.DisplayError(KB.K("You must close all other MainBoss windows first.").Translate());
		}
		#endregion
		#region User Interface click handlers
		private void ChangeLastUpdateEditor(object sender, System.EventArgs e) {
			((MainBossActiveFilter)Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithLastUpdateFilter>()).CreateEditor(UIFactory, (UIForm)this.RawUIObject, TblDrivenMainBossApplication.Instance.AppConnectionMixIn.Session);
		}

		/// <summary>
		/// Close the current window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Close_Click(object sender, System.EventArgs e) {
			this.CloseForm(UIDialogResult.Cancel);
		}

		private void HelpContents_Click(object sender, System.EventArgs e) {
			Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IHelp>().ShowTopic(Thinkage.Libraries.Presentation.KB.HelpTopicKey("TableOfContents"));
		}

		private void HelpIndex_Click(object sender, System.EventArgs e) {
			Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IHelp>().ShowTopic(Thinkage.Libraries.Presentation.KB.HelpTopicKey("Index"));
		}
		private void Support_Click(object sender, System.EventArgs e) {
			Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IHelp>().ShowTopic(Thinkage.Libraries.Presentation.KB.HelpTopicKey("GetSupport"));
		}
		private void About_Click(object sender, System.EventArgs e) {
			var f = new MBAboutForm(UIFactory, DB, TblDrivenMainBossApplication.Instance.ApplicationFullName);
			f.PositionNearOtherForm(this);
			f.ShowModal(this);
		}

		private void OpenInNewWindow_Click(object sender, System.EventArgs e) {
			var f = CopySelf();
			f.PositionNearOtherForm(this);
			f.ShowForm();
		}
		#endregion
	}
	#region DEBUG Support
#if DEBUG
	[System.ComponentModel.DesignerCategory("Code")]
	public class PickViewCostPermissions : TblForm<UIPanel> {
		public PickViewCostPermissions(UIFactory uiFactory, Thinkage.MainBoss.Database.Security.RightSet security)
			: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
			PanelHelper.SetColumnPanelGaps(FormContents, 3, 3);

			Caption = KB.K("Pick View Cost Permissions");

			IListComboDataControlWithExplicitMultipleRowValue picker = uiFactory.CreateList(new Thinkage.Libraries.TypeInfo.SetTypeInfo(false, Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse), null, UIListStyles.ExcludeDisabledRowsFromValue, null);
			IListColumn column = picker.AddColumn(Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo));
			FormContents.Add(picker);

			ButtonRowPanel okCancelButtons = new ButtonRowPanel(uiFactory);
			UIButton button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("OK"), new CallDelegateCommand(KB.K("Pick the individual view cost permissions"), delegate () {
				Set<object> v = (Set<object>)picker.Value;

				PermissionsGroup costGroup = ((MainBossPermissionsManager)TblDrivenMainBossApplication.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager).GetGroup(Root.Rights.ViewCost);
				costGroup.ResetPermissions();
				foreach (string p in v) {
					costGroup.SetPermission(p, true);
				}
				security.SelectedViewCosts = v;
				CloseForm(UIDialogResult.OK);
			})));
			this.AcceptButton = button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("Cancel"), new CloseFormCommand(KB.K("Select to cancel"), this, UIDialogResult.Cancel)));
			this.CancelButton = button;
			FormContents.Add(okCancelButtons);
			// Populate the list
			foreach (RightsGroupItem c in Root.Rights.ViewCost) {
				if (c is Right) {
					picker.Append(c.Name);
					column.SetValue(c.Name);
				}
			}
			picker.Value = security.SelectedViewCosts;
			//TODO: when style available ?			picker.Arranger.Info.Preferred = new Size(picker.Arranger.Info.Preferred.Width, picker.Font.Height * Math.Min(40, picker.ListSize + 1));
		}
	}

	[System.ComponentModel.DesignerCategory("Code")]
	public class PickViewCostRoles : TblForm<UIPanel> {
		public PickViewCostRoles(UIFactory uiFactory, Thinkage.MainBoss.Database.Security.RightSet security)
			: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
			PanelHelper.SetColumnPanelGaps(FormContents, 3, 3);
			Caption = KB.K("Pick View Cost Roles");

			IListComboDataControlWithExplicitMultipleRowValue picker = uiFactory.CreateList(new Thinkage.Libraries.TypeInfo.SetTypeInfo(false, Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse), null, UIListStyles.ExcludeDisabledRowsFromValue, null);
			IListColumn column = picker.AddColumn(Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo));
			FormContents.Add(picker);

			ButtonRowPanel okCancelButtons = new ButtonRowPanel(uiFactory);
			UIButton button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("OK"), new CallDelegateCommand(KB.K("Pick the roles"), delegate () {
				Set<object> v = (Set<object>)picker.Value;

				PermissionsGroup costGroup = ((MainBossPermissionsManager)TblDrivenMainBossApplication.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager).GetGroup(Root.Rights.ViewCost);
				costGroup.ResetPermissions();
				foreach (string role in v) {
					Thinkage.MainBoss.Database.Security.RightSet.RolePermission rp = security.RolePermissions(Thinkage.Libraries.DBILibrary.Security.TableRightType.Role, role);
					foreach (string p in rp.ViewCostPermissions)
						costGroup.SetPermission(p, true);
				}
				security.SelectedViewCostRoles = v;
				CloseForm(UIDialogResult.OK);
			})));
			this.AcceptButton = button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("Cancel"), new CloseFormCommand(KB.K("Select to cancel"), this, UIDialogResult.Cancel)));
			this.CancelButton = button;
			FormContents.Add(okCancelButtons);
			// Populate the list
			System.Text.RegularExpressions.Regex shortNames = new System.Text.RegularExpressions.Regex("[a-z]"); // Remove all but Uppercase
			foreach (string p in security.RoleNames) {
				Thinkage.MainBoss.Database.Security.RightSet.RolePermission rp = security.RolePermissions(Thinkage.Libraries.DBILibrary.Security.TableRightType.Role, p);
				if (rp.ViewCostPermissions.Count == 0)
					continue;
				System.Text.StringBuilder item = new System.Text.StringBuilder();
				item.Append(p);
				item.Append(" [");
				for (int i = 0; i < rp.ViewCostPermissions.Count; ++i) {
					if (i > 0)
						item.Append(",");
					item.Append(shortNames.Replace(rp.ViewCostPermissions[i], ""));
				}
				item.Append("]");
				picker.Append(p);
				column.SetValue(item.ToString());
			}
			picker.Value = security.SelectedViewCostRoles;
			// TODO:			picker.Arranger.Info.Preferred = new Size(picker.Arranger.Info.Preferred.Width, picker.Font.Height * Math.Min(40, picker.ListSize + 1));
		}
	}

	[System.ComponentModel.DesignerCategory("Code")]
	public class PickRoles : TblForm<UIPanel> {
		public PickRoles(UIFactory uiFactory, Thinkage.MainBoss.Database.Security.RightSet security)
			: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
			PanelHelper.SetColumnPanelGaps(FormContents, 3, 3);
			Caption = KB.K("Pick Roles");

			IListComboDataControlWithExplicitMultipleRowValue picker = uiFactory.CreateList(new Thinkage.Libraries.TypeInfo.SetTypeInfo(false, Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse), null, UIListStyles.ExcludeDisabledRowsFromValue, null);
			IListColumn column = picker.AddColumn(Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo));
			FormContents.Add(picker);

			ButtonRowPanel okCancelButtons = new ButtonRowPanel(uiFactory);
			UIButton button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("OK"), new CallDelegateCommand(KB.K("Pick the roles"), delegate () {
				Set<object> v = (Set<object>)picker.Value;

				var manager = (MainBossPermissionsManager)TblDrivenMainBossApplication.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager;
				manager.ResetPermissions();
				foreach (string role in v) {
					Thinkage.MainBoss.Database.Security.RightSet.RolePermission rp = security.RolePermissions(Thinkage.Libraries.DBILibrary.Security.TableRightType.Role, role);
					foreach (string p in rp.TableRightsAsTablePermissions)
						manager.GetGroup(Root.Rights.Table).SetPermission(p, true);
					foreach (string p in rp.ViewCostPermissions)
						manager.GetGroup(Root.Rights.ViewCost).SetPermission(p, true);
					foreach (string p in rp.TransitionPermissions)
						manager.GetGroup(Root.Rights.Transition).SetPermission(p, true);
					foreach (string p in rp.ActionPermissions)
						manager.GetGroup(Root.Rights.Action).SetPermission(p, true);
				}
				security.SelectedRoles = v;
				CloseForm(UIDialogResult.OK);
			})));
			this.AcceptButton = button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("Cancel"), new CloseFormCommand(KB.K("Select to cancel"), this, UIDialogResult.Cancel)));
			this.CancelButton = button;
			FormContents.Add(okCancelButtons);
			// Populate the list
			foreach (string p in security.RoleNames) {
				picker.Append(p);
				column.SetValue(p);
			}
			picker.Value = security.SelectedRoles;
			// TODO:			picker.Arranger.Info.Preferred = new Size(picker.Arranger.Info.Preferred.Width, picker.Font.Height * Math.Min(40, picker.ListSize + 1));
		}
	}

	[System.ComponentModel.DesignerCategory("Code")]
	public class PickLiveRoles : TblForm<UIPanel> {
		public PickLiveRoles(UIFactory uiFactory)
			: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
			PanelHelper.SetColumnPanelGaps(FormContents, 3, 3);
			Caption = KB.K("Pick Live Roles");
			DBClient session = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.Session;

			IListComboDataControlWithExplicitMultipleRowValue picker = uiFactory.CreateList(new Thinkage.Libraries.TypeInfo.SetTypeInfo(false, dsMB.Schema.T.Role.F.Id.EffectiveType), null, UIListStyles.ExcludeDisabledRowsFromValue, null);
			IListColumn column = picker.AddColumn(Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo));
			FormContents.Add(picker);

			ButtonRowPanel okCancelButtons = new ButtonRowPanel(uiFactory);
			UIButton button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("OK"), new CallDelegateCommand(KB.K("Pick the roles"), delegate () {
				Set<object> v = (Set<object>)picker.Value;

				var manager = (MainBossPermissionsManager)TblDrivenMainBossApplication.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager;
				manager.ResetPermissions();
				using (dsMB ds = new dsMB(session)) {
					// First load the user's own direct (i.e. not role-based) permissions
					session.ViewAdditionalRows(ds, dsPermission_1_1_4_2.Schema.T.Permission, new SqlExpression(dsPermission_1_1_4_2.Path.T.Permission.F.PrincipalID.F.UserID).Eq(TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordIDForPermissions));
					// Now load the permissions for all the selected roles.
					foreach (Guid principalId in v)
						session.ViewAdditionalRows(ds, dsPermission_1_1_4_2.Schema.T.Permission, new SqlExpression(dsPermission_1_1_4_2.Path.T.Permission.F.PrincipalID).Eq(principalId));

					foreach (dsMB.PermissionRow row in ds.T.Permission)
						manager.SetPermission(row.F.PermissionPathPattern.ToLower(), true);
				}
				CloseForm(UIDialogResult.OK);
			})));
			this.AcceptButton = button;
			okCancelButtons.Add(button = uiFactory.CreateButton(KB.K("Cancel"), new CloseFormCommand(KB.K("Select to cancel"), this, UIDialogResult.Cancel)));
			this.CancelButton = button;
			FormContents.Add(okCancelButtons);
			using (dsMB ds = new dsMB(session)) {
				// Populate the list from the roles in the DB
				session.ViewOnlyRows(ds, dsMB.Schema.T.Role, null, null, null);
				session.ViewOnlyRows(ds, dsMB.Schema.T.CustomRole, null, null, null);
				// TODO: Sort by code
				foreach (dsMB.RoleRow r in ds.T.Role) {
					picker.Insert(r.F.Id);
					column.SetValue(r.F.Code);
				}
				foreach (dsMB.CustomRoleRow r in ds.T.CustomRole) {
					picker.Insert(r.F.Id);
					column.SetValue(r.F.Code);
				}
				// Pre-select the roles the user already belongs to
				Set<object> currentMembership = new Set<object>(dsMB.Schema.T.Principal.F.Id.EffectiveType);
				session.ViewOnlyRows(ds, dsMB.Schema.T.UserRole, new SqlExpression(dsMB.Path.T.UserRole.F.UserID).Eq(TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordIDForPermissions), null,
					new DBI_PathToRow[] { dsMB.Path.T.UserRole.F.PrincipalID.PathToReferencedRow });
				// TODO: Sort by code
				foreach (dsMB.UserRoleRow r in ds.T.UserRole)
					currentMembership.AddUnique(r.PrincipalIDParentRow.F.RoleID ?? r.PrincipalIDParentRow.F.CustomRoleID);
				picker.Value = currentMembership;
			}
			// TODO:			picker.Arranger.Info.Preferred = new Size(picker.Arranger.Info.Preferred.Width, picker.Font.Height * Math.Min(40, picker.ListSize + 1));
		}
	}
#endif
	#endregion
}
