using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	internal static class UserPrincipalFromDirectoryService {
		public static Dictionary<string, DBI_Path> AttributeMapping = new Dictionary<string, DBI_Path> {
				{ KB.I("displayName"), dsUserPrincipal.Path.T.UserPrincipal.F.DisplayName },
				{ KB.I("mail"), dsUserPrincipal.Path.T.UserPrincipal.F.EmailAddress },
				{ KB.I("telephoneNumber"), dsUserPrincipal.Path.T.UserPrincipal.F.BusPhone },
				{ KB.I("mobile"), dsUserPrincipal.Path.T.UserPrincipal.F.MobilePhone },
				{ KB.I("facsimileTelephoneNumber"), dsUserPrincipal.Path.T.UserPrincipal.F.FaxPhone },
				{ KB.I("homePhone"), dsUserPrincipal.Path.T.UserPrincipal.F.HomePhone },
				{ KB.I("pager"), dsUserPrincipal.Path.T.UserPrincipal.F.PagerPhone },
				{ KB.I("wWWHomePage"), dsUserPrincipal.Path.T.UserPrincipal.F.WebURL },
				{ KB.I("preferredLanguage"), dsUserPrincipal.Path.T.UserPrincipal.F.PreferredLanguage },
				// The following are not part of the contact record so are not relevant here but fetched anyway (but won't form part of contact record)
				{ KB.I("streetAddress"), dsUserPrincipal.Path.T.UserPrincipal.F.StreetAddress },
				{ KB.I("st"), dsUserPrincipal.Path.T.UserPrincipal.F.Territory }, //State
				{ KB.I("L"), dsUserPrincipal.Path.T.UserPrincipal.F.City }, // City
				{ KB.I("co"), dsUserPrincipal.Path.T.UserPrincipal.F.Country }, // Country
				{ KB.I("postalCode"), dsUserPrincipal.Path.T.UserPrincipal.F.PostalCode }
			};
	}
	#region ContactFromDirectoryServiceBrowseLogic
	/// <summary>
	/// For creating contact records from User Directory information
	/// </summary>
	class ContactFromDirectoryServiceBrowseLogic : BrowseLogic {
		#region Construction
		#region - Constructor
		public ContactFromDirectoryServiceBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
		}
		#endregion
		#region - Custom browser command creation
		protected override void CreateCustomBrowserCommands() {
			base.CreateCustomBrowserCommands();
			GetDirectoryServiceUserListCommand = new CallDelegateCommand(
				KB.K("Obtain list of users and contacts from Active Directory"),
				null,
				delegate () {
					List<UserDirectoryObject> users = CommonUI.UIFactory.SelectUsersFromSystemDirectory(UserPrincipalFromDirectoryService.AttributeMapping.Keys.ToList<string>(), multiSelect: true);
					if (users != null) {
						Session.UserDirectorySource = users;
						SetAllOutOfDate();
					}
				}, true);
			Commands.AddCommand(KB.K("Refresh from Active Directory"), null, GetDirectoryServiceUserListCommand, GetDirectoryServiceUserListCommand);
			ICommand cmd = new CreateContactFromDirectoryServiceCommand(this);
			Commands.AddCommand(KB.K("Create Contacts"), null, cmd, cmd);
		}
		#endregion
		public override void SetAllKeepUpdated(bool keepUpdated) {
			base.SetAllKeepUpdated(keepUpdated);
		}
		#endregion
		#region Properties
		public UserFromDirectoryServiceSession Session { get { return (UserFromDirectoryServiceSession)base.DB.Session; } }
		private CallDelegateCommand GetDirectoryServiceUserListCommand;
		#endregion
		#region Data Synchronization
		public override void SetAllOutOfDate() {
			base.SetAllOutOfDate();
		}
		#endregion
		#region CreateContactFromDirectoryServiceCommand
		/// <summary>
		/// Create multiple contacts from information in a UserPrincipalBrowser
		/// </summary>
		private class CreateContactFromDirectoryServiceCommand : ICommand {
			readonly Source nameSource;
			readonly Source displayNameSource;
			readonly Source emailSource;
			readonly Source LDAPGuidSource;
			readonly Source busPhoneSource;
			readonly Source homePhoneSource;
			readonly Source pagerSource;
			readonly Source faxSource;
			readonly Source wwwSource;
			readonly Source mobileSource;
			readonly Source preferredLanguageSource;

			//DataSources for all fields in the user directory list we are updating from
			public CreateContactFromDirectoryServiceCommand(ContactFromDirectoryServiceBrowseLogic browser) {
				Browser = browser;
				displayNameSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.DisplayName, 0);
				nameSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.Name, 0);
				emailSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.EmailAddress, 0);
				LDAPGuidSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.Guid, 0);
				busPhoneSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.BusPhone, 0);
				homePhoneSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.HomePhone, 0);
				pagerSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.PagerPhone, 0);
				faxSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.FaxPhone, 0);
				wwwSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.WebURL, 0);
				mobileSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.MobilePhone, 0);
				preferredLanguageSource = Browser.GetTblPathDisplaySource(dsUserPrincipal.Path.T.UserPrincipal.F.PreferredLanguage, 0);

				OverallDisabler = new MultiDisablerIfAllEnabled();

				AnyRecordsEnabler = new SettableDisablerProperties(null, KB.K("There are no Active Directory records to create from"), false);
				Browser.AugmentedCursors.Changed += new DataChanged(UpdateEnabling);
				UpdateEnabling(DataChangedEvent.Reset, null);
				OverallDisabler.Add(AnyRecordsEnabler);

				ITblDrivenApplication app = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>();
				if (app.TableRights != null) { //rights are based on the Contact table
					var rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild(dsMB.Schema.T.Contact.MainTableName);
					if (rightsGroup != null)
						OverallDisabler.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
				}
			}
			private readonly ContactFromDirectoryServiceBrowseLogic Browser;
			//			private RowContentChangedHandler Notification;
			// The following arrays are all indexed by view index.
			// The enablers that control whether deletion is allowed.
			private readonly MultiDisablerIfAllEnabled OverallDisabler;
			private readonly SettableDisablerProperties AnyRecordsEnabler;
			// The source in the browser for the ID of the row as the source of the new license record
			private void UpdateEnabling(DataChangedEvent changeType, Position affectedRecord) {
				bool enabled = false;
				Browser.IterateOverAllRecords(rowId => {
					// Determine if we are enabled or not; we are enabled if at least one record exists
					enabled = true;
					return false; // only need to find 1 so stop iterating now.
				});
				AnyRecordsEnabler.Enabled = enabled;
			}
			public void Execute() {
				if (!OverallDisabler.Enabled)
					throw new GeneralException(OverallDisabler.Tip);
				// Prohibit deletion if an editor is currently editing the record. Note that this check is done
				// here but it should really be done using the Enabled property of the command. This would however remove the ability to activate the offending editor.
				// This could be done via new ApplicationTblDefaultsUsingWindows.AlreadyBeingEditedDisabler(TblSchema) but then you must set the Id property of the disabler,
				// and this is a single Id. The disabler would have to be expanded to take a set of ID's, and we would once again be faced with the choice of whether
				// the overall Enabled would be false if *any* or *all* of the named id's were being edited. Either that, or we would have to make one such notifier for each record
				// in the selection, and figure out how to keep it somewhere.
				Set<object> contextIds = new Set<object>(Browser.Cursors.IdType);
				Browser.IterateOverAllRecords(
					rowId => {
						contextIds.Add(rowId);
						return true;
					}
				);
				Browser.BrowseContextRecordIds = contextIds;
				//				Browser.IterateOverContextRecords(delegate()
				//					{
				//						allLicensesPresent.Add(new UserDirectoryObject((string)SourceForNewLicenseKey.GetValue()));
				//						return true; // keep going
				//					}
				//				);

				// Note our updates go to the Original existing DBClient DB for the contact table.
				using (var dsUpdate = new dsMB(Browser.Session.ExistingDB)) {
					dsUpdate.EnforceConstraints = false;
					dsUpdate.DataSetName = KB.I("ContactFromDirectoryServiceBrowseLogic.dsUpdate");
					// Change the UserDirectoryObject table dataset to reflect our changes and save to database
					dsMB.ContactDataTable contactTable = (dsMB.ContactDataTable)dsUpdate.GetDataTable(dsMB.Schema.T.Contact);
					// Add the selected ones

					Browser.IterateOverContextRecords(() => {
						var newRow = contactTable.AddNewRow();
						newRow.F.Code = (string)displayNameSource.GetValue();
						if (String.IsNullOrEmpty(newRow.F.Code))
							newRow.F.Code = (string)nameSource.GetValue(); // use the name if display name is empty
						newRow.F.Email = (string)emailSource.GetValue();
						newRow.F.LDAPGuid = (Guid)LDAPGuidSource.GetValue();
						newRow.F.BusinessPhone = (string)busPhoneSource.GetValue();
						newRow.F.HomePhone = (string)homePhoneSource.GetValue();
						newRow.F.MobilePhone = (string)mobileSource.GetValue();
						newRow.F.PagerPhone = (string)pagerSource.GetValue();
						newRow.F.FaxPhone = (string)faxSource.GetValue();
						newRow.F.WebURL = (string)wwwSource.GetValue();
						var lang = (string)preferredLanguageSource.GetValue();
						if (!String.IsNullOrEmpty(lang))
							try {
								System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(lang);
								newRow.F.PreferredLanguage = ci.LCID;
							}
							catch (System.Exception) {
							}
						return true;
					});
					ServerExtensions.UpdateOptions updateOptions = ServerExtensions.UpdateOptions.Normal;
					for (; ; ) {
						try {
							dsUpdate.DB.Update(dsUpdate, updateOptions);
							Browser.Session.UserDirectorySource = null;
							Browser.SetAllOutOfDate();
						}
						catch (DBConcurrencyException e) {
							if (updateOptions == ServerExtensions.UpdateOptions.Normal && Browser.BrowseUI.HandleConcurrencyError(Browser.DB, e)) {
								// User wants to retry the delete even though changed
								updateOptions = ServerExtensions.UpdateOptions.NoConcurrencyCheck;
								continue;
							}
						}
						catch (InterpretedDbException ie) {
							if (ie.InterpretedErrorCode == InterpretedDbExceptionCodes.ViolationUniqueConstraint)
								throw new GeneralException(ie, KB.K("Contact '{0}' can not be created since it already exists"), (string)nameSource.GetValue());
							throw;
						}
						return;
					}
				}
			}
			public bool RunElevated {
				get {
					return false;
				}
			}

			public Thinkage.Libraries.Translation.Key Tip {
				get {
					return Enabled ? KB.K("Create the contacts") : OverallDisabler.Tip;
				}
			}
			public bool Enabled {
				get {
					return OverallDisabler.Enabled;
				}
			}
			public event IEnabledChangedEvent EnabledChanged { add { OverallDisabler.EnabledChanged += value; } remove { OverallDisabler.EnabledChanged -= value; } }
		}
		#endregion
	}
	#endregion
	#region UserFromDirectoryServiceSession
	public class UserFromDirectoryServiceSession : EnumerableDrivenSession<UserDirectoryObject, UserDirectoryObject> {
		#region Connection
		public class Connection : IConnectionInformation {
			public Connection() {
			}

			#region IConnectionInformation Members
			public IServer CreateServer() {
				return new UserFromDirectoryServiceServer();
			}
			public string DisplayName {
				get {
					return KB.K("").Translate();
				}
			}
			public string DisplayNameLowercase {
				get {
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

			public string DatabaseConnectionString => throw new NotImplementedException();
			#endregion
		}
		#endregion
		#region Server
		public class UserFromDirectoryServiceServer : EnumerableDrivenSession<UserDirectoryObject, UserDirectoryObject>.EnumerableDrivenServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {
				// Schema is fixed for dsContactFromDirectoryService
				return new UserFromDirectoryServiceSession((Connection)connectInfo, this);
			}
		}
		#endregion
		#region Constructor
		public UserFromDirectoryServiceSession(IConnectionInformation connection, IServer server)
			: base(connection, server) {
		}
		public UserFromDirectoryServiceSession(DBClient existing)
			: this(new Connection(), new UserFromDirectoryServiceServer()) {
			ExistingDB = existing;
		}
		#endregion
		#region Properties
		public override DBI_Database Schema {
			get {
				return dsUserPrincipal.Schema;
			}
			set {
				throw new NotImplementedException();
			}
		}
		protected new Connection ConnectionObject {
			get {
				return (Connection)base.ConnectionObject;
			}
		}
		/// <summary>
		/// The UserDirectory object source
		/// </summary>
		public List<UserDirectoryObject> UserDirectorySource {
			get;
			set;
		}
		public readonly DBClient ExistingDB;
		#endregion
		#region Overrides to support base class abstraction
		protected override UserDirectoryObject PrepareItemForRead(UserDirectoryObject item) { return item; }
		protected override UserDirectoryObject PrepareItemForWrite(UserDirectoryObject item) { return item; }
		#region - ServerIdentification
		public override string ServerIdentification {
			get {
				return KB.I("Thinkage Contact Active Directory Create Server");
			}
		}
		#endregion
		#region - GetEvaluators (returns delegates that produce column values)
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			normalUpdater = delegate (UserDirectoryObject e, object v) {
				throw new NotImplementedException();
			};
			if (sourceColumnSchema == dsUserPrincipal.Schema.T.UserPrincipal.F.Guid) {
				normalEvaluator = delegate (UserDirectoryObject u) {
					return u.Id;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return Guid.Empty;
				};
			}
			else if (sourceColumnSchema == dsUserPrincipal.Schema.T.UserPrincipal.F.Name) {
				normalEvaluator = delegate (UserDirectoryObject u) {
					return u.Name;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsUserPrincipal.Schema.T.UserPrincipal.F.LDAPPath) {
				normalEvaluator = delegate (UserDirectoryObject u) {
					return u.Path;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return Thinkage.Libraries.Exception.FullMessage(e);
				};
			}
			else {
				// search for the schema in the AttributeMapping table
				normalEvaluator = delegate (UserDirectoryObject u) {
					foreach (var p in UserPrincipalFromDirectoryService.AttributeMapping) {
						if (sourceColumnSchema == p.Value.ReferencedColumn)
							return (string)u.FetchSingleAttributeValue(p.Key, p.Value.ReferencedType, typeof(string));
					}
					throw new Thinkage.Libraries.GeneralException(KB.K("Unknown source column in ColumnEvaluator"));
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return null;
				};
			}
		}
		#endregion
		#region - GetItemEnumerable (returns an enumerable of the ItemT containing the driving data)
		protected override IEnumerable<UserDirectoryObject> GetItemEnumerable(DBI_Table dbit) {
			return UserDirectorySource ?? new List<UserDirectoryObject>();
		}
		#endregion
		#endregion
	}
	#endregion
}
