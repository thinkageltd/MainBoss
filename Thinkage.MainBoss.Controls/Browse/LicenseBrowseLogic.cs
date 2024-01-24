using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	#region LicenseUpdaterBrowseLogic
	class LicenseUpdaterBrowseLogic : BrowseLogic {
		#region Construction
		#region - Constructor
		public LicenseUpdaterBrowseLogic(IBrowseUI control, XAFClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure)
		{
		}
		#endregion
		#region - Custom browser command creation
		protected override void CreateCustomBrowserCommands() {
			base.CreateCustomBrowserCommands();
			FillFromClipboardCommand = new CallDelegateCommand(
				KB.K("Extract license keys from clipboard text"),
				null,
				delegate() {
					var contents = (string)CommonUI.UIFactory.CreateClipboard<string>().Value;
					if (contents != null) {
						Session.TextSource = contents;
						SetAllOutOfDate();
					}
				}, true);
			Commands.AddCommand(KB.K("Refresh from Clipboard"), null, FillFromClipboardCommand, FillFromClipboardCommand);
			ICommand cmd = new UpdateLicensesCommand(this);
			Commands.AddCommand(KB.K("Update Licenses"), null, cmd, cmd);
		}
		#endregion
		#endregion
		#region Properties
		public LicenseUpdateSession Session { get { return (LicenseUpdateSession)base.DB.Session; } }
		private CallDelegateCommand FillFromClipboardCommand;
		#endregion
#if SOMEDAY
		#region RefreshFromFileCommand
		private class RefreshFromFileCommand : ICommand
		{
			private readonly LicenseUpdaterBrowseLogic BrowseLogic;
			public RefreshFromFileCommand(LicenseUpdaterBrowseLogic browselogic)
			{
				BrowseLogic = browselogic;
			}

		#region ICommand Members

			public void Execute()
			{
				Application.Instance.DisplayError("file reading not yet implemented");
				BrowseLogic.SetAllOutOfDate();
			}

			public bool RunElevated
			{
				get
				{
					return false;
				}
			}

			#endregion

		#region IDisablerProperties Members
			public Libraries.Translation.Key Tip
			{
				get
				{
					if (Enabled)
						return KB.K("Look for licenses in a text file");
					else
						return KB.K("TODO: This needs to be implemented");
				}
			}
			public bool Enabled
			{
				get
				{
					return false;
				}
			}
			public event IEnabledChangedEvent EnabledChanged { add {} remove {}	}
			#endregion
		}
		#endregion
#endif
		#region UpdateLicensesCommand
		private class UpdateLicensesCommand : ICommand
		{
			//DataSources for all fields in the license list we are updating from
			public UpdateLicensesCommand(LicenseUpdaterBrowseLogic browser)
			{
				Browser = browser;

				SourceForNewLicenseKey = Browser.GetTblPathDisplaySource(dsLicense_1_1_4_2.Path.T.License.F.License, 0);

				Disabler = new MultiDisablerIfAllEnabled();
				ITblDrivenApplication app = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>();
				if (app.TableRights != null) {
					TableOperationRightsGroup rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild(dsMB.Schema.T.License.MainTableName);
					if (rightsGroup != null) {
						Disabler.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
						Disabler.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Edit)));
						Disabler.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Delete)));
					}
				}
				Browser.AugmentedCursors.Changed += new DataChanged(UpdateEnabling);
				UpdateEnabling(DataChangedEvent.Reset, null);
			}
			private readonly LicenseUpdaterBrowseLogic Browser;
//			private RowContentChangedHandler Notification;
			// The following arrays are all indexed by view index.
			// The enablers that control whether deletion is allowed.
			private readonly MultiDisablerIfAllEnabled Disabler;
			// The source in the browser for the ID of the row as the source of the new license record
			private readonly Source SourceForNewLicenseKey;
			private void UpdateEnabling(DataChangedEvent changeType, Position affectedRecord)
			{
				bool enabled = false;
				Browser.IterateOverAllRecords(rowId =>
				{
					// Determine if we are enabled or not; we are enabled if at least one record exists
					enabled = true;
					return false; // only need to find 1
				});
				Enabled = enabled;
			}
			public void Execute()
			{
				List<License> allLicensesPresent = new List<License>();
				// Prohibit deletion if an editor is currently editing the record. Note that this check is done
				// here but it should really be done using the Enabled property of the command. This would however remove the ability to activate the offending editor.
				// This could be done via new ApplicationTblDefaultsUsingWindows.AlreadyBeingEditedDisabler(TblSchema) but then you must set the Id property of the disabler,
				// and this is a single Id. The disabler would have to be expanded to take a set of ID's, and we would once again be faced with the choice of whether
				// the overall Enabled would be false if *any* or *all* of the named id's were being edited. Either that, or we would have to make one such notifier for each record
				// in the selection, and figure out how to keep it somewhere.
				Set<object> contextIds = new Set<object>(Browser.Cursors.IdType);
				Browser.IterateOverAllRecords(
					rowId =>
					{
						contextIds.Add(rowId);
						return true;
					}
				);
				Browser.BrowseContextRecordIds = contextIds;
				Browser.IterateOverContextRecords(delegate()
					{
						allLicensesPresent.Add(new License((string)SourceForNewLicenseKey.GetValue()));
						return true; // keep going
					}
				);
				// Make sure the user really picked something (although this is a should not happen event)
				if (allLicensesPresent.Count == 0)
					throw new Thinkage.Libraries.GeneralException(KB.K("No licenses were found")); // use same message BrowseLogic has; no need for special message to clutter up translation strings

				License newNamedUserLicense; // set to the NamedUser License in the set of selected licenses, if present. 
				var namedUserLicenses = (from l in allLicensesPresent
									where l.ApplicationID == (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.NamedUsers
									select l).ToList<License>(); //
				if (namedUserLicenses.Count > 1)
					throw new Thinkage.Libraries.GeneralException(KB.K("More than one Named User license key exists in the set of selected licenses"));
				newNamedUserLicense = namedUserLicenses.FirstOrDefault<License>();

				if (newNamedUserLicense != null && !allLicensesPresent.All<License>(l => l.LicenseID == newNamedUserLicense.LicenseID))
					throw new Thinkage.Libraries.GeneralException(KB.K("At least one of the selected licenses has a License Id different from the new Named User License {0}"), newNamedUserLicense.LicenseStr);

				// Note our updates go to the Original existing XAFClient DB for the license table.
				using (XAFDataSet dsUpdate = XAFDataSet.New(dsMB.Schema.T.License.Database, Browser.Session.ExistingDB)) {
					dsUpdate.EnforceConstraints = false;
					dsUpdate.DataSetName = KB.I("LicenseUpdaterBrowseLogic.dsUpdate");
					// Build a list of the current licenses to prepare to make decision on resolution. Since the licenses are unique we can use them for 'ids' in locating records later
					// rather than build a complicated list of recordids to rows.
					List<License> existingLicenses = new List<License>();
					List<License> DeleteThese = new List<License>();
					List<License> AddThese = new List<License>();
					// get the existing licenses in the database
					dsUpdate.DB.ViewAdditionalRows(dsUpdate, dsMB.Schema.T.License); // fetch all the current licenses
					foreach (dsMB.LicenseRow lr in dsMB.Schema.T.License.GetDataTable(dsUpdate).Rows) {
						try {
							existingLicenses.Add(new License(lr.F.License));
						}
						catch (Thinkage.Libraries.GeneralException) {
							// This should only happen if there is a BAD license string in the database; what should we do ?
						}
					}
					// From an update perspective, if there is a Named User key already in the database then that is the key that governs what serial number keys we will update unless
					// we were given a new (better) Named User license
					namedUserLicenses = (from mb in existingLicenses
										 where mb.ApplicationID == (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.NamedUsers
										 select mb).ToList<License>();
					if (namedUserLicenses.Count > 1) {
						System.Text.StringBuilder msg = new System.Text.StringBuilder();
						msg.AppendLine(KB.K("There was more than 1 Named User license found:").Translate());
						msg.Append(ListOfKeysAsString(namedUserLicenses));
						msg.AppendLine(KB.K("Make sure only one Named User license exists").Translate());
						throw new Thinkage.Libraries.GeneralException(KB.T(msg.ToString()));
					}
					License namedUserLicense = namedUserLicenses.FirstOrDefault<License>();
					/// Remove any licenses whose Id exceeds our application MaxValue (this would be the temporary Named User license in place BEFORE we changed licensing to use the old MainBoss licence for Named Users
					var invalidLicenses = (from mb in existingLicenses
										 where mb.ApplicationID >= (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.MaxValue
										 select mb).ToList<License>();
					DeleteThese.AddRange(invalidLicenses);


					// First see if we are replacing the Named user License key with a better one from our selectedLicense set
					List<License> enforcingLicenses = new List<License>();
					if (newNamedUserLicense != null && newNamedUserLicense.IsBetterThan(namedUserLicense, LicenseModelDifferences))
						enforcingLicenses.Add(newNamedUserLicense);
					else if (namedUserLicense != null)
						enforcingLicenses.Add(namedUserLicense);
					if (enforcingLicenses.Count == 0)
						throw new Thinkage.Libraries.GeneralException(KB.K("There must be a Named User license in the selected set, or an existing Named User license in the database to update using the selected set of licenses"));

					List<License> wrongIds = (from l in allLicensesPresent
											  where l.LicenseID != enforcingLicenses[0].LicenseID
											  select l).ToList();
					if (wrongIds.Count > 0)
					{
						// no new license, so we will only add, update keys whose licenseID matches the current Named User license; if any don't match, we give up.
						System.Text.StringBuilder msg = new System.Text.StringBuilder();
						msg.AppendLine(Thinkage.Libraries.Strings.Format(KB.K("The following licenses have a License Id different from the Named User license {0}:"), enforcingLicenses[0].LicenseStr));
						msg.Append(ListOfKeysAsString(wrongIds));

						throw new Thinkage.Libraries.GeneralException(KB.T(msg.ToString()));
					}

					// remove all existingLicenses whose LicenseID doesn't match our new defined enforcingLicense License Key's id
					DeleteThese.AddRange(existingLicenses.FindAll(l => l.LicenseID != enforcingLicenses[0].LicenseID));
					existingLicenses.RemoveAll(l => l.LicenseID != enforcingLicenses[0].LicenseID); // remove from checking consideration

					// At this point, we have a list of new licenses to add that have a LicenseID matching the existingLicenses LicenseIDs
					// All non matching LicenseID keys have been removed from our existing set. 
					// We now just compare the new licenses to the existing licenses for the same ID and keep the 'better' ones. Note that Demo keys would have been removed already
					// if their LicenseID's didn't match our new NamedUser license key.
					//
					// TODO: Right now the message we generate is a bit confusing, because a license may appear in WIllNotReplace because the new license is worse than the existing one
					// or because the new and existing licenses are tied (neither is better). The wording of the message is the same either way although in one case it should say
					// "These licenses will not be replaced because they are better than the new ones ..."
					// "These licenses will not be replaced because they are as good as the new ones ..."
					List<License> WillNotReplace = new List<License>();
					foreach (var newLicense in allLicensesPresent) {
						DeleteThese.AddRange(from l in existingLicenses
												 where newLicense.ApplicationID == l.ApplicationID && newLicense.LicenseID == l.LicenseID
												 && newLicense.IsBetterThan(l, LicenseModelDifferences) select l);
						WillNotReplace.AddRange(from l in existingLicenses
												 where newLicense.ApplicationID == l.ApplicationID && newLicense.LicenseID == l.LicenseID
												 && !newLicense.IsBetterThan(l, LicenseModelDifferences) && !l.LicenseStr.Equals(newLicense.LicenseStr, StringComparison.InvariantCultureIgnoreCase)
												select l);
						if (!existingLicenses.Any<License>(l => l.LicenseID == newLicense.LicenseID &&
								(l.IsBetterThan(newLicense, LicenseModelDifferences) || l.LicenseStr.Equals(newLicense.LicenseStr, StringComparison.InvariantCultureIgnoreCase)))) {
							AddThese.Add(newLicense);
						}
					}

					// Last check for compatibility amongst the new licenses to be added against what will our new set of licenses (including the existing ones not deleted).
					// Although this information is somewhat present in the
					// ModeDefinition table in the MainBossApplication class, it is not currently accessible nor exactly definitive as to what licenses are permitted with what.
					// We will use the rules defined in the MainBoss.Database.Licensing class as to what licenses are permitted with what.
					List<License> resultingSet = new List<License>(existingLicenses); // start with what remains of the existing licenses
					foreach (License toRemove in DeleteThese)
						resultingSet.Remove(toRemove);
					resultingSet.AddRange(AddThese);
					foreach (var l in AddThese) {
						var needsException = Thinkage.Libraries.Licensing.LicenseManager.CheckRestriction(l, resultingSet, Thinkage.MainBoss.Database.Licensing.Restrictions);
						if (needsException != null)
							throw needsException;
					}

					System.Text.StringBuilder askMsg = new System.Text.StringBuilder();
					if (AddThese.Count > 0) {
						askMsg.AppendLine(KB.K("Licenses to add").Translate());
						askMsg.Append(ListOfKeysAsString(AddThese));
					}
					else {
						askMsg.AppendLine(KB.K("There are no licenses to add that are better than existing licenses").Translate());
					}
					if (DeleteThese.Count > 0) {
						askMsg.AppendLine(KB.K("Licenses to delete").Translate());
						askMsg.Append(ListOfKeysAsString(DeleteThese));
					}
					else {
						askMsg.AppendLine(KB.K("There are no licenses to delete").Translate());
					}
					askMsg.AppendLine();
					if (WillNotReplace.Count > 0) {
						askMsg.AppendLine(KB.K("Existing licenses considered better than ones in license update set").Translate());
						askMsg.Append(ListOfKeysAsString(WillNotReplace));
						askMsg.AppendLine(KB.K("You must first remove the existing license manually and then try again with the new license set if the intent is to replace a better license.").Translate());
					}
					if (AddThese.Count == 0 && DeleteThese.Count == 0)
						throw new Thinkage.Libraries.GeneralException(KB.T(askMsg.ToString()));
					askMsg.AppendLine(KB.K("OK to update the licenses?").Translate());
					if (Thinkage.Libraries.Ask.Question(askMsg.ToString()) != Thinkage.Libraries.Ask.Result.Yes)
						return;
					// Change the License table dataset to reflect our changes and save to database
					dsMB.LicenseDataTable licTable = (dsMB.LicenseDataTable) dsMB.Schema.T.License.GetDataTable(dsUpdate);
					foreach (dsMB.LicenseRow rowToDelete in licTable.Rows) {
						if (DeleteThese.Any<License>(l => l.LicenseStr.Equals(rowToDelete.F.License,StringComparison.InvariantCultureIgnoreCase)))
							rowToDelete.Delete();
					}
					// Add the new ones
					foreach (License l in AddThese) {
						Thinkage.MainBoss.Database.DatabaseCreation.SetLicenseRow(licTable.AddNewLicenseRow(), l);
					}
					Server.UpdateOptions updateOptions = Server.UpdateOptions.Normal;
					for (; ; ) {
						try {
							dsUpdate.DB.Update(dsUpdate, updateOptions);
						}
						catch (DBConcurrencyException e) {
							if (updateOptions == Server.UpdateOptions.Normal && Browser.BrowseUI.HandleConcurrencyError(Browser.DB, e)) {
								// User wants to retry the delete even though changed
								updateOptions = Server.UpdateOptions.NoConcurrencyCheck;
								continue;
							}
						}
						catch (InterpretedDbException) {
							//if (ie.InterpretedErrorCode == InterpretedDbExceptionCodes.ViolationForeignConstraint)
							//	throw new Thinkage.Libraries.GeneralException(ie, KB.K("Cannot delete the record because it is referenced by another record in the database."));
							// We also formerly handled ie.InterpretedErrorCode == InterpretedDbExceptionCodes.ViolationUniqueConstraint here, which could really only
							// happen if one person hides a record 'X', someone creates a new record 'X', then someone whose clock is just the right amount of time after
							// the first person's tries to hide the second 'X' record.
							// Because this is such a far-fetched condition, and the resolution (try again) is something the user can do anyway, we no longer handle this case.
							throw;
						}
						return;
					}
				}
			}
			private bool LicenseModelDifferences(License replacement, License original) {
				// A TableLimit license limits the count of rows in a table. Any other model counts as unlimited
				// So if the original is TableLimitLicensing any other model is better.
				// Otherwise the replacement is tied or worse than the original.
				return original.LicenseModel == License.LicenseModels.TableLimitLicensing;
			}
			private string ListOfKeysAsString(List<License> list)
			{
				System.Text.StringBuilder t = new System.Text.StringBuilder();
				list.ForEach(k => t.AppendLine("\t" + k.LicenseStr));
				return t.ToString();
			}
			public bool RunElevated
			{
				get
				{
					return false;
				}
			}

			public Thinkage.Libraries.Translation.Key Tip
			{
				get
				{
					return Enabled ? KB.K("Update the licenses") : KB.K("There are no license records to update");
				}
			}
			public bool Enabled
			{
				get
				{
					return pEnabled;
				}
				set
				{
					if (pEnabled != value) {
						pEnabled = value;
						EnabledChanged?.Invoke();
					}
				}
			}
			private bool pEnabled;
			public event IEnabledChangedEvent EnabledChanged;
		}
		#endregion
	}
	#endregion
	#region LicenseUpdateSession
	public class LicenseUpdateSession : EnumerableDrivenSession<License, License>
	{
		#region Connection
		public class Connection : IConnectionInformation
		{
			public Connection()
			{
			}

			#region IConnectionInformation Members
			public IServer CreateServer()
			{
				return new LicenseUpdateServer();
			}
			public string DisplayName
			{
				get
				{
					return KB.K("").Translate();
				}
			}
			public string DisplayNameLowercase
			{
				get
				{
					return KB.K("").Translate();
				}
			}

			public AuthenticationCredentials Credentials {
				get {
					throw new NotImplementedException();
				}
			}

			public string UserIdentification {
				get {
					throw new NotImplementedException();
				}
			}
			#endregion
		}
		#endregion
		#region Server
		public class LicenseUpdateServer : EnumerableDrivenSession<License, License>.EnumerableDrivenServer
		{
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema)
			{
				// Schema is fixed for dsLicenseUpdate
				return new LicenseUpdateSession((Connection)connectInfo, this);
			}
		}
		#endregion
		#region Constructor
		public LicenseUpdateSession(IConnectionInformation connection, IServer server)
			: base(connection, server)
		{
		}
		public LicenseUpdateSession(XAFClient existing, string initialText) : this(new Connection(), existing.Session.Server)
		{
			ExistingDB = existing;
			TextSource = initialText;
		}
		#endregion
		#region Properties
		public override DBI_Database Schema
		{
			get
			{
				return dsLicense_1_1_4_2.Schema;
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		protected new Connection ConnectionObject
		{
			get
			{
				return (Connection)base.ConnectionObject;
			}
		}
		/// <summary>
		/// The TextSource of a string to search for License Keys in 
		/// </summary>
		public string TextSource
		{
			get;
			set;
		}
		public readonly XAFClient ExistingDB;
		#endregion
		#region Overrides to support base class abstraction
		protected override License PrepareItemForRead(License item) { return item; }
		protected override License PrepareItemForWrite(License item) { return item; }
		#region - ServerIdentification
		public override string ServerIdentification
		{
			get
			{
				return KB.I("Thinkage License Update Server");
			}
		}
		#endregion
		#region - GetEvaluators (returns delegates that produce column values)
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator)
		{
			normalUpdater = delegate(License e, object v)
			{
				throw new NotImplementedException();
			};
			if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.Id) {
				normalEvaluator = delegate(License license)
				{
					return System.Guid.NewGuid();
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return System.Guid.NewGuid();
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.ApplicationID) {
				normalEvaluator = delegate(License license)
				{
					return license.ApplicationID;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.ExpiryModel) {
				normalEvaluator = delegate(License license)
				{
					return license.ExpiryModel;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.Expiry) {
				normalEvaluator = delegate(License license)
				{
					return license.Expiry;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.LicenseModel) {
				normalEvaluator = delegate(License license)
				{
					return license.LicenseModel;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.LicenseCount) {
				normalEvaluator = delegate(License license)
				{
					return license.LicenseCount;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.LicenseID) {
				normalEvaluator = delegate(License license)
				{
					return license.LicenseID;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsLicense_1_1_4_2.Schema.T.License.F.License) {
				normalEvaluator = delegate(License license)
				{
					return license.LicenseStr;
				};
				exceptionEvaluator = delegate(System.Exception e)
				{
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else
				throw new Thinkage.Libraries.GeneralException(KB.K("Unknown source column in ColumnEvaluator"));
		}
		#endregion
		#region - GetItemEnumerable (returns an enumerable of the ItemT containing the driving data)
		protected override IEnumerable<License> GetItemEnumerable(DBI_Table dbit)
		{
			if (TextSource != null)
				return License.FindLicensesInText(TextSource);
			return new List<License>(); // empty list
		}
		#endregion
		#endregion
	}
	#endregion
}
