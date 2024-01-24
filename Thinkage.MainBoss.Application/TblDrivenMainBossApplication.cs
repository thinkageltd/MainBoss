using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Application {
	#region DataImportExportSupport
	public class ApplicationImportExport : GroupedInterface<IApplicationInterfaceGroup>, IApplicationDataImportExport, IDataImportExport {
		public ApplicationImportExport(GroupedInterface<IApplicationInterfaceGroup> attachTo)
			: base(attachTo) {
			RegisterService<IApplicationDataImportExport>(this);
			RegisterService<IDataImportExport>(this);
		}

		#region IApplicationDataImportExport Members
		public string DataSetNamespace {
			get {
				return KB.I("thinkage.ca/MainBoss");
			}
		}
		public IServer ServerProperties {
			get {
				// Use the database connection for serverProperties (if exists)
				var databaseAppConnection = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithSingleDatabaseConnection>();
				return databaseAppConnection != null ? databaseAppConnection.Session.Session.Server : new Thinkage.Libraries.DBILibrary.MSSql.SqlClient.SqlServer();
			}
		}
		#endregion


		#region IApplicationDataImportExport Members
		private System.Text.StringBuilder validationErrors;
		public void Import(UIFactory uiFactory, XAFClient DB, Tuple<string, DelayedCreateTbl> info) {
			UIFileSelectorPattern[] importFilter = new UIFileSelectorPattern[] {
				new UIFileSelectorPattern(KB.K("Import files (*.xml)"), KB.I("*.xml")),
				new UIFileSelectorPattern(KB.K("All files (*.*)"), KB.I("*.*")) };

			var filename = uiFactory.SelectFileToOpen(null, importFilter);
			if (filename != null) {
				using (DataImportExport importer = new DataImportExport(info)) {
					validationErrors = new System.Text.StringBuilder();
					try {
						importer.LoadDataSetFromXmlFile(filename.Value.Pathname, new System.Xml.Schema.ValidationEventHandler(xml_ValidationEventHandler));
					}
					catch (GeneralException e) {
						if (validationErrors.Length > 0) {
							validationErrors.Insert(0, Environment.NewLine);
							Thinkage.Libraries.Exception.AddContext(e, new Thinkage.Libraries.MessageExceptionContext(KB.K("XML Validation Errors {0}"), validationErrors));
						}
						throw;
					}
					System.Data.DataSet errorDataSet;
					IProgressDisplay ipd = uiFactory.CreateProgressDisplay(KB.K("Importing Data"), importer.DataHelper.DataTable.Rows.Count);
					using (dsMB mbds = new dsMB(DB)) {
						mbds.DisableUpdatePropagation();
						try {
							errorDataSet = importer.SaveDataSetToDatabase(mbds, ipd);
						}
						finally {
							ipd.Complete();
						}
						GeneralException ex = DataImportExport.CheckErrorDataSet(errorDataSet);
						if (ex != null) {
							System.Text.StringBuilder question = new System.Text.StringBuilder();
							question.AppendLine(Thinkage.Libraries.Exception.FullMessage(ex));
							question.AppendLine();
							question.AppendLine(KB.K("Do you want to save all errors into a file for later analysis?").Translate());

							if (Thinkage.Libraries.Application.Instance.AskQuestion(question.ToString(), KB.K("Import Errors").Translate(), Ask.Questions.YesNo) == Ask.Result.Yes) {
								UIFileSelectorPattern[] saveErrorFilter = new UIFileSelectorPattern[] {
								new UIFileSelectorPattern(KB.K("XML file (*.xml)"), KB.I("*.xml")),
								new UIFileSelectorPattern(KB.K("All files (*.*)"), KB.I("*.*")) };
								var errorOutputFile = uiFactory.SelectFileToSaveTo(null, saveErrorFilter);
								if (errorOutputFile != null)
									errorDataSet.WriteXml(errorOutputFile.Value.Pathname);
							}
						}
					}
				}
			}
		}
		void xml_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e) {
			validationErrors.AppendLine(Strings.Format(KB.K("Line {0}, Position {1}:{2}"), e.Exception.LineNumber, e.Exception.LinePosition, e.Message));
		}

		public void Export(UIFactory uiFactory, XAFClient DB, Tuple<string, DelayedCreateTbl> info) {
			UIFileSelectorPattern[] exportFilter = new UIFileSelectorPattern[] {
				new UIFileSelectorPattern(KB.K("Export for Excel (includes schema in other file)  (*.xml)"), KB.I("*.xml")),
				new UIFileSelectorPattern(KB.K("Export with embedded schema  (*.xml)"), KB.I("*.xml")),
				new UIFileSelectorPattern(KB.K("Export with no embedded schema (*.xml)"), KB.I("*.xml")),
				new UIFileSelectorPattern(KB.K("All files (*.*)"), KB.I("*.*")) };
			var saveInfo = uiFactory.SelectFileToSaveTo(null, exportFilter);
			if (saveInfo != null) {
				using (DataImportExport exporter = new DataImportExport(info)) {
					System.Data.DataSet exportDataSet = exporter.LoadDataSetFromDatabase(DB, dsMB.Schema);
					exporter.WriteDataSetToFile(saveInfo.Value.Pathname, exportDataSet, saveInfo.Value.FileSelectorPatternIndex == 2, saveInfo.Value.FileSelectorPatternIndex == 1);
				}
			}
		}
		public void GenerateXmlSchema(UIFactory uiFactory, Tuple<string, DelayedCreateTbl> info) {
			UIFileSelectorPattern[] schemaFilter = new UIFileSelectorPattern[] {
				new UIFileSelectorPattern(KB.K("For Excel (*.xml)"), KB.I("*.xml")),
				new UIFileSelectorPattern(KB.K("Normal (*.xml)"), KB.I("*.xml")),
				new UIFileSelectorPattern(KB.K("All files (*.*)"), KB.I("*.*")) };
			var saveInfo = uiFactory.SelectFileToSaveTo(null, schemaFilter);
			if (saveInfo != null) {
				using (DataImportExport exporter = new DataImportExport(info)) {
					bool forExcel = saveInfo.Value.FileSelectorPatternIndex == 1;
					System.IO.File.WriteAllText(saveInfo.Value.Pathname, forExcel ? exporter.DataHelper.ExcelSchemaText : exporter.DataHelper.StandardSchemaText, System.Text.Encoding.Unicode);
				}
			}
		}

		public Tuple<string, DelayedCreateTbl> FindImportExportInformation(Tbl.TblIdentification tid) {
			return TIGeneralMB3.FindImport(tid);
		}
		public Tuple<string, DelayedCreateTbl> FindImportExportInformation(string identifyingName) {
			return TIGeneralMB3.FindImport(identifyingName);
		}
		public string FindImportExportNameFromSchemaIdentification(string schemaIdentification) {
			return TIGeneralMB3.FindImportNameFromSchemaIdentification(schemaIdentification);
		}
		#endregion
	}
	#endregion
	#region Customization Support
	// TODO: This uses a mix of Execute methods and DBClient methods. Migrate everything to the latter.
	public class MainBossCustomizations : ApplicationWithCustomizations {
		static UIFileSelectorPattern[] ImportExportFileFilter = new UIFileSelectorPattern[] {
			new UIFileSelectorPattern(KB.K("Customization files (*.xml)"), KB.I("*.xml")),
			new UIFileSelectorPattern(KB.K("All files (*.*)"), KB.I("*.*")) };
		public MainBossCustomizations(TblDrivenMainBossApplication attachTo, bool supportsPersonalSettings)
			: base(attachTo) {
			// At this point the current application object is not the one we will be running with, but 'attachTo' is.
			pSupportsSettings = attachTo.AppConnectionMixIn.VersionHandler.CurrentVersion >= new Version(1, 0, 10, 52);
			pSupportsPersonalSettings = supportsPersonalSettings;
		}
		private dsMB DS {
			get {
				if (pDS == null)
					pDS = new dsMB(GetInterface<IApplicationWithSingleDatabaseConnection>().Session);
				return pDS;
			}
		}
		private dsMB pDS;
		/// <summary>
		/// Publish the current set of HiddenFeatures so all users of the database will get them on startup
		/// </summary>
		public void Publish() {
			MB3Client session = (MB3Client)GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			session.EditVariable(DS, dsMB.Schema.V.HiddenFeatures);
			DS.V.HiddenFeatures.Value = HiddenFeatures;
			session.Update(DS);
		}
		public void RestoreFromPublished() {
			MB3Client session = (MB3Client)GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			session.ViewAdditionalVariables(DS, dsMB.Schema.V.HiddenFeatures);
			HiddenFeatures = (VisabilityCustomizations)dsMB.Schema.V.HiddenFeatures.EffectiveType.GenericAsNativeType(DS.V.HiddenFeatures.Value, typeof(VisabilityCustomizations));
			if (HiddenFeatures == null)
				// The variable is nullable but we treat null here as an empty set instead.
				HiddenFeatures = new VisabilityCustomizations();
		}
		/// <summary>
		/// Clear all VisabilityCustomizations
		/// </summary>
		public void ClearAll() {
			HiddenFeatures = new VisabilityCustomizations();
		}
		public void Export(UIFactory uiFactory) {
			var filename = uiFactory.SelectFileToSaveTo(null, ImportExportFileFilter);
			if (filename != null)
				Customization.ExportCustomizationFile(filename.Value.Pathname, HiddenFeatures);
		}
		public bool Import(UIFactory uiFactory, bool append) {
			var filename = uiFactory.SelectFileToOpen(null, ImportExportFileFilter);
			if (filename != null) {
				var newHiddenFeatures = Customization.ImportCustomizationFile(filename.Value.Pathname);
				if (append)
					HiddenFeatures.UnionWith(newHiddenFeatures);
				else
					HiddenFeatures = newHiddenFeatures;
				return true;
			}
			return false;
		}
		// want: public override bool SupportsSettings { get; private set; } but that's not allowed.
		public override bool SupportsSettings { get { return pSupportsSettings; } }
		private readonly bool pSupportsSettings;
		public override bool SupportsPersonalSettings { get { return pSupportsPersonalSettings; } }
		private readonly bool pSupportsPersonalSettings;
		private IPopulatingCursorManager SettingsCursorManager;
		public override void StartSettingsEnumeration(string settingsName) {
			if (EnumerationPositioner == null) {
				Guid? userId = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.IsAnonymousLogin ? null : (Guid?)TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordID;

				// Query all the Settings records by matching userID (or null userID) but do not fetch all the Values. If there is no current user id all we fetch is the global ones.
				SettingsCursorManager = new FlattenedNonSchemaPopulatingCursorManager(GetInterface<IApplicationWithSingleDatabaseConnection>().Session, dsMB.Schema.T.Settings);
				SettingsCursorManager.SetRootFilters(
					SqlExpression.Or(
						new SqlExpression(dsMB.Path.T.Settings.F.UserID).IsNull(),
						(userId.HasValue && SupportsPersonalSettings) ? new SqlExpression(dsMB.Path.T.Settings.F.UserID).Eq(userId.Value) : null));
				pSettingsEnumerationID = SettingsCursorManager.GetPathSource(dsMB.Path.T.Settings.F.Id);
				pSettingsEnumerationCode = SettingsCursorManager.GetPathSource(dsMB.Path.T.Settings.F.Code);
				pSettingsEnumerationIsGlobal = SettingsCursorManager.GetExpressionSource(new SqlExpression(dsMB.Path.T.Settings.F.UserID).IsNull());
				pSettingsEnumerationIsDefault = SettingsCursorManager.GetExpressionSource(
					userId.HasValue
					? new SqlExpression(dsMB.Path.T.Settings.F.Id)
						.Eq(SqlExpression.ScalarSubquery(new SelectSpecification(
																new[] { new SqlExpression(dsMB.Path.T.DefaultSettings.F.SettingsID) },
																new SqlExpression(dsMB.Path.T.DefaultSettings.F.SettingsNameID).Eq(new SqlExpression(dsMB.Path.T.Settings.F.SettingsNameID, 1))
																	.And(new SqlExpression(dsMB.Path.T.DefaultSettings.F.UserID).Eq(userId.Value)), null)))
						.IsTrue()   // if false, there is a default but this isn't it, and if null, there is no default
					: SqlExpression.Constant(false));
				pSettingsName = SettingsCursorManager.GetPathSource(dsMB.Path.T.Settings.F.SettingsNameID.F.Code);
				// Make a sorting positioner to control the order of enumeration.
				var sortingPositioner = new SortingPositioner(SettingsCursorManager, new KeyProviderFromSources(new SortKey<Source>(pSettingsEnumerationCode, descending: false), new SortKey<Source>(pSettingsEnumerationIsGlobal, descending: true)));

				sortingPositioner.Changed += delegate (DataChangedEvent whatHappened, Position toWhat) {
					switch (whatHappened) {
					case DataChangedEvent.Added:
						sortingPositioner.CurrentPosition = toWhat;
						string newSettingsName = (string)pSettingsName.GetValue();
						SettingsNameMap[toWhat.Id] = newSettingsName;
						SettingsChanged?.Invoke(newSettingsName);
						break;
					case DataChangedEvent.Changed:
					case DataChangedEvent.MovedAndChanged:
					case DataChangedEvent.Moved:
						SettingsChanged?.Invoke(SettingsNameMap[toWhat.Id]);
						break;
					case DataChangedEvent.Reset:
						// We tell that every/anything has changed by notifying with null
						SettingsNameMap.Clear();
						SettingsChanged?.Invoke(null);
						break;
					case DataChangedEvent.Deleted:
						// We have a problem here; we can't position to the deleted record to find its settingsName!
						// so we need to keep a mapping from Settings row id to settingsName.
						// But it may be that the node we have has been deleted externally by someone else (another MainBoss session) and we won't have it in our
						// SettingsNameMap since we only put entries in that dictionary for nodes WE add to ourselves.  So we have to check that we have the node
						// in our dictionary before just assuming it is there.
						string settingName;
						if (SettingsNameMap.TryGetValue(toWhat.Id, out settingName)) {
							SettingsChanged?.Invoke(settingName);
							SettingsNameMap.Remove(toWhat.Id);
						}
						break;
					}
				};
				// Now that we have our handler for the positioner Changed operation, fill the data.
				SettingsCursorManager.SetAllKeepUpdated(true);

				// Make a positioner that filters on SettingsName so we can easily enumerate the contents for one Settings control.
				EnumerationPositioner = new FilteringPositioner(sortingPositioner, null);
			}
			EnumerationPositioner.FilterSource = new TransformingSource(BoolTypeInfo.NonNullUniverse, pSettingsName, (recordSettingsName) => StringTypeInfo.Equals(settingsName, recordSettingsName));
			EnumerationPositioner.CurrentPosition = EnumerationPositioner.StartPosition;
		}
		private FilteringPositioner EnumerationPositioner;
		private Dictionary<object, string> SettingsNameMap = new Dictionary<object, string>(dsMB.Schema.T.Settings.InternalIdColumn.EffectiveType); 
		public override bool NextSettingsEnumeration() {
			EnumerationPositioner.CurrentPosition = EnumerationPositioner.CurrentPosition.Next;
			return !EnumerationPositioner.CurrentPosition.IsEnd;
		}
		public override event SettingsChangedHander SettingsChanged;
		public override Source SettingsEnumerationID {
			get { return pSettingsEnumerationID; }
		}
		private Source pSettingsEnumerationID;
		public override Source SettingsEnumerationCode {
			get { return pSettingsEnumerationCode; }
		}
		private Source pSettingsEnumerationCode;
		public override Source SettingsEnumerationIsGlobal {
			get { return pSettingsEnumerationIsGlobal; }
		}
		private Source pSettingsEnumerationIsGlobal;
		public override Source SettingsEnumerationIsDefault {
			get { return pSettingsEnumerationIsDefault; }
		}
		private Source pSettingsEnumerationIsDefault;
		private Source pSettingsName;
		public override Stream GetSettings(object id, out Version version, out bool isGlobal) {
			// Load the Settings record with the given Id, and get its Value field. If no record use new Byte[0].
			XAFClient db = GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			var row = (dsMB.SettingsRow)db.ViewAdditionalRow(DS, dsMB.Schema.T.Settings, new SqlExpression(dsMB.Path.T.Settings.F.Id).Eq(SqlExpression.Constant(id)));
			if (row == null)
				throw new GeneralException(KB.K("Requested Settings could not be found"));
			// Return a read MemoryStream around the value.
			version = new Version(row.F.Version);
			isGlobal = !row.F.UserID.HasValue;
			return new MemoryStream(row.F.Value, writable: false);
		}
		public override object CreateSettings(string settingsName, string Code, bool isGlobal) {
			// Map from the settingsName to an Id (creating a SettingsName record if required).
			if (!isGlobal && !SupportsPersonalSettings)
				throw new GeneralException(KB.K("This application does not support personal Settings"));
			XAFClient db = GetInterface<IApplicationWithSingleDatabaseConnection>().Session;

			try {
				var nameRrow = (dsMB.SettingsNameRow)db.ViewAdditionalRow(DS, dsMB.Schema.T.SettingsName, new SqlExpression(dsMB.Path.T.SettingsName.F.Code).Eq(SqlExpression.Constant(settingsName)));
				if (nameRrow == null) {
					nameRrow = (dsMB.SettingsNameRow)db.AddNewRowAndBases(DS, dsMB.Schema.T.SettingsName);
					nameRrow.F.Code = settingsName;
				}
				// Create a new Settings record with SettingsNameId <- nameRrow.F.Id, Code as passed, UserId <- IsGlobal ? null : current userid, Value <- new Byte[0]
				var row = (dsMB.SettingsRow)db.AddNewRowAndBases(DS, dsMB.Schema.T.Settings);
				row.F.SettingsNameID = nameRrow.F.Id;
				if (!isGlobal)
					// The caller should not ask to create personal settings if there is no current user.
					row.F.UserID = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordID;
				row.F.Code = Code;
				row.F.Version = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.VersionHandler.CurrentVersion.ToString();
				row.F.Value = new Byte[0];
				db.Update(DS);
				return row.F.Id;
			}
			finally {
				DS.Clear();
			}
		}
		public override Stream SaveSettings(object id) {
			SaveStream = new MemoryStream();
			SaveId = id;
			return SaveStream;
		}
		private MemoryStream SaveStream;
		private object SaveId;
		public override void EndSaveSettings() {
			GetInterface<IApplicationWithSingleDatabaseConnection>().Session.Session.ExecuteCommand(
				new UpdateSpecification(dsMB.Schema.T.Settings,
					new[] { dsMB.Schema.T.Settings.F.Value, dsMB.Schema.T.Settings.F.Version },
					new[] { SqlExpression.Constant(SaveStream.ToArray()), SqlExpression.Constant(TblDrivenMainBossApplication.Instance.AppConnectionMixIn.VersionHandler.CurrentVersion.ToString()) },
					new SqlExpression(dsMB.Path.T.Settings.F.Id).Eq(SqlExpression.Constant(SaveId))));
		}
		public override void SaveDefault(object id) {
			// Find the DefaultSettings record for the user id and SettingsNameID of the Settings record with the given id. If none, create one, otherwise update the existing one.
			// This call is not allowed for anonymous logins
			XAFClient db = GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			object userId = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordID;
			try {
				object settingsNameId = db.Session.ExecuteCommandReturningScalar(dsMB.Schema.T.DefaultSettings.F.SettingsNameID.EffectiveType,
													new SelectSpecification(dsMB.Schema.T.Settings,
														new[] { new SqlExpression(dsMB.Path.T.Settings.F.SettingsNameID) },
														new SqlExpression(dsMB.Path.T.Settings.F.Id).Eq(SqlExpression.Constant(id)),
														null));

				var defaultSettingsRow = (dsMB.DefaultSettingsRow)db.ViewAdditionalRow(DS, dsMB.Schema.T.DefaultSettings,
						new SqlExpression(dsMB.Path.T.DefaultSettings.F.SettingsNameID).Eq(SqlExpression.Constant(settingsNameId))
							.And(new SqlExpression(dsMB.Path.T.DefaultSettings.F.UserID).Eq(SqlExpression.Constant(userId))));
				if (defaultSettingsRow == null) {
					defaultSettingsRow = (dsMB.DefaultSettingsRow)db.AddNewRowAndBases(DS, dsMB.Schema.T.DefaultSettings);
					defaultSettingsRow.F.SettingsNameID = (Guid)settingsNameId;
					defaultSettingsRow.F.UserID = (Guid)userId;
				}
				defaultSettingsRow.F.SettingsID = (Guid)id;
				db.Update(DS);
			}
			finally {
				DS.Clear();
			}
		}
		public override void ClearDefault(string settingsName) {
			// Delete any DefaultSettings record for the given SettingsName id and user id.
			XAFClient db = GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			object userId = TblDrivenMainBossApplication.Instance.AppConnectionMixIn.UserRecordID;
			try {
				var defaultSettingsRow = (dsMB.DefaultSettingsRow)db.ViewAdditionalRow(DS, dsMB.Schema.T.DefaultSettings,
						new SqlExpression(dsMB.Path.T.DefaultSettings.F.SettingsNameID.F.Code).Eq(SqlExpression.Constant(settingsName))
							.And(new SqlExpression(dsMB.Path.T.DefaultSettings.F.UserID).Eq(SqlExpression.Constant(userId))));
				if (defaultSettingsRow != null) {
					defaultSettingsRow.Delete();
					db.Update(DS);
				}
			}
			finally {
				DS.Clear();
			}
		}
		public override void DeleteSettings(object id) {
			XAFClient db = GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			try {
				// Delete any DefaultSettings records that refer to this Settings. For a personal Settings there should be only one but for global Settings there could be up to one per user.
				db.Edit(DS, dsMB.Schema.T.DefaultSettings, new SqlExpression(dsMB.Path.T.DefaultSettings.F.SettingsID).Eq(SqlExpression.Constant(id)));
				foreach (dsMB.DefaultSettingsRow defaultSettingsRow in DS.T.DefaultSettings.Rows)
					defaultSettingsRow.Delete();
				// delete the Settings record for the given Id.
				var settingsRow = (dsMB.SettingsRow)db.EditSingleRow(DS, dsMB.Schema.T.Settings, new SqlExpression(dsMB.Path.T.Settings.F.Id).Eq(SqlExpression.Constant(id)));
				if (settingsRow != null)
					settingsRow.Delete();
				db.Update(DS);
			}
			finally {
				DS.Clear();
			}
		}
		public override void RefreshSettings() {
			SettingsCursorManager.SetAllOutOfDate();
		}
	}
	#endregion
	// This application class is for apps that run logged on to a current MainBoss database.
	public abstract class TblDrivenMainBossApplication : Thinkage.Libraries.Application {
		public static new TblDrivenMainBossApplication Instance { get { return (TblDrivenMainBossApplication)Thinkage.Libraries.Application.Instance; } }

		#region Creation and teardown
		protected abstract FormProxy CreateMainForm(MB3Client db);

		protected TblDrivenMainBossApplication(ModeDefinition mode, NamedOrganization o, bool supportsPersonalSettings) { //bool browsersHavePanel, string organizationName, DBClient.Connection connection) {
																														  // TODO: This is a crappy place to do this, but it is also crappy that it needs to be done. Eventually the information will be in
																														  // extended properties of the schema and will thus automatically be there.
			Thinkage.MainBoss.Database.MBRestrictions.DefineRestrictions();

			FormsPresentationMixIn = new FormsPresentationApplication(this, !o.MBConnectionParameters.CompactBrowsers,
				// This UIHandler adds a recursive containment filter to all Link(Location) type values. It uses the PermanentLocationPickerTblCreator, which is correct for most cases,
				// but for Sublocation it contains values (Units and Storerooms) that cannot possibly contain a Sublocation. I don't have a good way of avoiding this because we don't
				// know what types of Locations the search value allows in the first place. Even looking at the field underlying the search value is insufficient, because it might be
				// RelativeLocation.ContainingLocationID which has exactly the ambiguity described above.
				// Note that LocationContainment.ContainedLocationID is not nullable, but sv.TypeInfo might be so we have to remove nullability from the latter before testing the type.
				new SearchExpressionControl.UIHandler(
					(sv => sv.TypeInfo.IntersectCompatible(ObjectTypeInfo.NonNullUniverse).TypeEquals(dsMB.Schema.T.LocationContainment.F.ContainedLocationID.EffectiveType)),
					KB.K("Is contained in one of"), KB.K("is contained in one of {0}"),
					null,
					// We can't code this as an EXISTS(SELECT * FROM LocationContainment WHERE ContainedLocationID = (Source Placeholder) AND ...
					// because we would have to increment the scope level of any Path expr node in the Source value.
					SearchExpressionControl.UIHandler.SourcePlaceholder.In(
						new SelectSpecification(dsMB.Schema.T.LocationContainment, new[] { new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainedLocationID) },
												new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainingLocationID).In(SearchExpressionControl.UIHandler.ValuePlaceholders[0]), null)),
					SearchExpressionControl.Context.Server,
					new SearchExpressionControl.UIHandler.ParameterInfoNode(
						(searchValue) => new SetTypeInfo(false, dsMB.Schema.T.LocationContainment.F.ContainingLocationID.EffectiveType, 1),
						(searchValue) => new Fmt(Fmt.SetPickFrom(TILocations.PermanentLocationPickerTblCreator)),
						null, null, null, null)
				)
			);
			new Thinkage.MainBoss.Database.RaiseErrorTranslationKeyBuilder(this);
			var permManager = new MainBossPermissionsManager(Root.Rights);
			new ApplicationTblDefaultsUsingWindows(this, new ETbl(), permManager, Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			Thinkage.Libraries.XAF.UI.UIFactory uiFactory = GetInterface<UIFactory>();
			uiFactory.FixedPitchControlFontFamily = Configuration.MonospaceFontFamilyForDisplay;
			uiFactory.ProportionalControlFontFamily = Configuration.RegularFontFamilyForDisplay;

			// Copy the help parameters from the app that is creating us.
			HelpUsingFolderOfHtml.CopyFromOtherApplication(this, Thinkage.Libraries.Application.Instance);
			// TODO: What should happen here is we open a database under a minimal schema in order to extract from it version information.
			// If the version information is satisfactory we create a full MB3Client; if the version information names an old version
			// we offer to upgrade it
			AppConnectionMixIn = new ApplicationWithSingleDatabaseConnection(this);
			try {
				try {
					var session = new MB3Client(o.MBConnectionParameters.Connection);
					session.Session.LockShared(); // no longer done in DBClient, but at the application level
					AppConnectionMixIn.SetAppAndOrganizationAndSession(o.DisplayName, session);
					ApplicationFullName = mode.FullName;
					AppConnectionMixIn.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, mode.MinDBVersion, mode.MinAppVersionVariable, mode.FullName));
					AppConnectionMixIn.Session.ObtainSession(mode.SessionModeID);
					AppConnectionMixIn.CheckLicensesAndSetFeatureGroups(mode.MainKeys, mode.FeatureGroupLicensing, new Licensing.MBLicensedObjectSet(session), AppConnectionMixIn.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
					// Check DBEffectiveServerVersion against actual server version and warn that special processing may be required.
					if (DBVersionInformation.GetServerVersionUpgradersIndex(session.Session.EffectiveDBServerVersion) < DBVersionInformation.GetServerVersionUpgradersIndex(session.Session.ServerVersion))
						Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("This database uses features available in SQL server version {0} but your server is version {1} which has additional features which would enhance performance. Use the 'Upgrade' operation to update the database to use these newer features."), session.Session.EffectiveDBServerVersion, session.Session.ServerVersion));
					AppConnectionMixIn.InitializeUserId();

					permManager.InitializeRolesGrantingPermission(session);
					using (dsMB ds = new dsMB(session)) {
						session.ViewAdditionalVariables(ds, dsMB.Schema.V.ActiveFilterInterval, dsMB.Schema.V.ActiveFilterSinceDate);
						DateTime? sinceDate = null;
						TimeSpan? interval = null;
						if (!ds.V.ActiveFilterSinceDate.IsNull)
							sinceDate = ds.V.ActiveFilterSinceDate.Value;
						if (!ds.V.ActiveFilterInterval.IsNull)
							interval = ds.V.ActiveFilterInterval.Value;

						new MainBossActiveFilter(this, sinceDate, interval);
					}
					new ApplicationImportExport(this);
					// Daisy-chain our UserMessageTranslator in front of the existing one thus giving UserMessageTranslations precedence.
					// Note though that a generic-culture translation in the UserMessage Translator will take precedence over a specific-culture translation in the original Translator.
					this.Translator = new TranslatorConcentrator(new Thinkage.MainBoss.Database.UserMessageTranslator(session), this.Translator);
				}
				catch (System.Exception ex) {
					AppConnectionMixIn.CloseDatabaseSession();
					if (ex is GeneralException)
						throw;          // message should be good
					throw new GeneralException(ex, KB.K("There was a problem validating access to {0}"), o.MBConnectionParameters.Connection.DisplayNameLowercase);
				}
			}
			catch (Thinkage.Libraries.Licensing.NoLicenseException e) {
				throw new GeneralException(e, KB.K("There are no licenses for the application '{0}'. Use the 'Administration' application to add the required licenses."), mode.FullName);
			}
			catch (Thinkage.Libraries.DBAccess.DatabaseUpgradeRequiredException e) {
				throw new GeneralException(e, KB.K("The database requires upgrading for the application '{0}'. Use the 'Upgrade' operation to upgrade the database."), mode.FullName);
			}
			if (AppConnectionMixIn.VersionHandler.CurrentVersion >= new Version(1, 0, 10, 31)) {
				notifierBuffer = new DynamicBufferManager(AppConnectionMixIn.Session, dsMB.Schema, false);
				q = notifierBuffer.Add(dsMB.Schema.T.UserRole, true, null);
				dtp = new Thinkage.Libraries.DataFlow.DataTablePositioner(q.DataTable);
				AppConnectionMixIn.UserForPermissionsChangeNotify += delegate () {
					// Note the lifetime of the Application object and the AppConnectionMixIn object match so we do not need to unsubscribe the event
					UserForPermissionsCheckingChanged();
				};
				UserForPermissionsCheckingChanged();
			}

			// Initialize exteral tag handling
			DBI_Path tagPath = dsMB.Path.T.ExternalTag.F.ExternalTag;
			IntegralTypeInfo sizeType = ((StringTypeInfo)tagPath.ReferencedColumn.EffectiveType).SizeType;
			// Build the "browser" for the barcode handling in the main form to use.
			BarcodeHandlerBrowser = new CachedObjectWithTimeout<FindAndEditBrowseControl>(delegate () {
				return new FindAndEditBrowseControl(TIGeneralMB3.FindBrowseTbl(tagPath.Table), GetInterface<IApplicationWithSingleDatabaseConnection>().Session, false, tagPath, null);
			}, new TimeSpan(0, 1, 0));  // One minute timespan.
			BarcodeScannerHandler = uiFactory.CreateBarcodeScanner((int)sizeType.NativeMinLimit(typeof(int)), (int)sizeType.NativeMaxLimit(typeof(int)), UIKeys.F12);

			// Load the customizations
			AppCustomizationsMixIn = new MainBossCustomizations(this, supportsPersonalSettings);
			if (AppConnectionMixIn.VersionHandler.CurrentVersion >= new Version(1, 0, 10, 13))
				// TODO: For now we just load customizations if we are beyond a certain version.
				// Eventually I expect that customizer.Load would have to do different things based on version in which case we would change to always call Load and let Load figure it out.
				AppCustomizationsMixIn.RestoreFromPublished();
		}
		public readonly string ApplicationFullName;
		public abstract Thinkage.Libraries.Application CreateSetupApplication();

		public override void TeardownApplication(Thinkage.Libraries.Application nextApplication) {
			if (BarcodeScannerHandler != null) {
				BarcodeScannerHandler.Dispose();
				BarcodeScannerHandler = null;
			}
			if (BarcodeHandlerBrowser != null) {
				BarcodeHandlerBrowser.Dispose();
				BarcodeHandlerBrowser = null;
			}
			AppConnectionMixIn.CloseDatabaseSession();
			base.TeardownApplication(nextApplication);
		}
		IDisposable BarcodeScannerHandler;
		public CachedObjectWithTimeout<FindAndEditBrowseControl> BarcodeHandlerBrowser;
		public readonly ApplicationWithSingleDatabaseConnection AppConnectionMixIn;
		public readonly MainBossCustomizations AppCustomizationsMixIn;
		public readonly FormsPresentationApplication FormsPresentationMixIn;

		#region Permission Notification Handling
		DynamicBufferManager notifierBuffer;
		DynamicBufferManager.Query q;
		Thinkage.Libraries.DataFlow.DataTablePositioner dtp;
		private void UserForPermissionsCheckingChanged() {
			dtp.Changed -= new Thinkage.Libraries.DataFlow.DataChanged(dtp_Changed);
			q.SetFilter(new Thinkage.Libraries.DBILibrary.SqlExpression(dsMB.Path.T.UserRole.F.UserID).Eq(Thinkage.Libraries.DBILibrary.SqlExpression.Constant(AppConnectionMixIn.UserRecordIDForPermissions)));
			notifierBuffer.SetAllKeepUpdated(true);
			dtp.Changed += new Thinkage.Libraries.DataFlow.DataChanged(dtp_Changed);
		}
		void dtp_Changed(Thinkage.Libraries.DataFlow.DataChangedEvent whatHappened, Thinkage.Libraries.DataFlow.Position affectedRecordPosition) {
			// Since we might get more than one notification we defer this until idle.
			GetInterface<IIdleCallback>().ScheduleIdleCallback(this,
				delegate () {
					AppConnectionMixIn.ReloadPermissions(applyAdminAccess: false);
				});
		}
		#endregion
		#endregion
	}
}
