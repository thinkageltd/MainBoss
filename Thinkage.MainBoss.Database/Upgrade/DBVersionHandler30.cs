using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Service;

[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_0_1_0_To_1_0_2_44")]
[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_0_2_45_To_1_0_4_78")]
[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_0_4_79_To_1_0_6_30")]
[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_0_6_31_To_1_0_8_1")]
[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_0_8_2_To_1_0_8_28")]
[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_0_8_29_To_1_1_4_1")]
[assembly: DBVersionHandler.Register("Thinkage.MainBoss.Database.DBVersionRangeHandler_1_1_4_2")]

// TODO: The dsUpgrade_1_0_0_1_To_1_0_4_13 should be a 10 version as well, or perhaps we should be skipping the typed data set altogether
namespace Thinkage.MainBoss.Database {

	#region UnfetteredDatabaseAccessNoAction
	/// <summary>
	/// Dummy IDisposable that does nothing for earlier databases that didn't support giving up unfettered access to the database
	/// </summary>
	internal class UnfetteredDatabaseAccessNoAction : IDisposable {
		public UnfetteredDatabaseAccessNoAction() {
		}
		public void Dispose() {
		}
	}
	#endregion

	public class DBVersionRangeHandler_1_1_4_2 : DBVersionRangeHandler {
		public DBVersionRangeHandler_1_1_4_2()
			: base() {
		}
		public override Guid? IdentifyUser(DBClient db) {
			string system_user = DatabaseCreation.GetDatabaseSystemUser(db);

			using(dsUser_1_1_4_2 dsUser = new dsUser_1_1_4_2(db.Session.Server)) {
				dsUser.DataSetName = KB.I("DBVersionHandler30.IdentifyUser.dsUser");

				db.ViewAdditionalRows(dsUser, dsUser_1_1_4_2.Schema.T.User, new SqlExpression(dsUser_1_1_4_2.Path.T.User.F.Hidden).IsNull()
					.And(new SqlExpression(dsUser_1_1_4_2.Path.T.User.F.AuthenticationCredential).Lower().Eq(SqlExpression.Constant(system_user))));

				// We should have at most one row
				switch(dsUser.T.User.Rows.Count) {
					case 0:
						break;
					case 1:
						return ((dsUser_1_1_4_2.UserRow)dsUser.T.User.Rows[0]).F.Id;
					default:
						break;
				}
			}
			return null;
		}
		#region UpdateUsersAfterRestore - DEPRECATED
		public override void UpdateUsersAfterRestore(DBClient client, System.Text.StringBuilder restoreErrors) {
			// as of conversion to AuthenticationCredentials, post processing of User table is no longer performed after a restore. The SysAdmin restoring the database is responsible
			// for establishing the proper credentials for the users.
		}
		#endregion
		protected virtual object GetVariableValue(DBClient db, DBDataSet ds, DBI_Variable v) {
			// This is only called for DBVersion, and any MinAppVersion variables that may exist.
			// Because we only have a DBClient we cannot use View(..., DBI_Variable) to get at the variables.
			db.ViewAdditionalVariables(ds, v);
			return ds[v].Value;
		}
		protected virtual void SetVariableValue(DBClient db, DBDataSet ds, DBI_Variable v, object value) {
			// This is only called for DBVersion, and any MinAppVersion variables that may exist.
			// Because we only have a DBClient we cannot use View(..., DBI_Variable) to get at the variables.
			db.ViewAdditionalVariables(ds, v);
			ds[v].Value = value;
			db.Update(ds);
		}
		public override Version LoadDBVersion(DBClient db) {
			// TODO: A custom DS which only contains the DBVersion variable
			using(dsDBVersion_1_0_8_2 ds = new dsDBVersion_1_0_8_2(db.Session.Server)) {
				return new Version((string)GetVariableValue(db, ds, dsDBVersion_1_0_8_2.Schema.V.DBVersion));
			}
		}
		public override void StoreDBVersion(DBClient db, Version versionToStore) {
			// TODO: A custom DS which only contains the DBVersion variable (same as LoadDBVersion uses)
			using(dsDBVersion_1_0_8_2 ds = new dsDBVersion_1_0_8_2(db.Session.Server)) {
				SetVariableValue(db, ds, dsDBVersion_1_0_8_2.Schema.V.DBVersion, versionToStore.ToString());
			}
		}
		public override Version GetDBServerVersion(DBClient db) {
			using(dsDBVersion_1_0_8_2 ds = new dsDBVersion_1_0_8_2(db.Session.Server)) {
				return new Version((string)GetVariableValue(db, ds, dsDBVersion_1_0_8_2.Schema.V.DBServerVersion));
			}
		}
		public override void StoreDBServerVersion(DBClient db, Version serverVersion) {
			using(dsDBVersion_1_0_8_2 ds = new dsDBVersion_1_0_8_2(db.Session.Server)) {
				SetVariableValue(db, ds, dsDBVersion_1_0_8_2.Schema.V.DBServerVersion, serverVersion.ToString());
			}
		}
		public override List<License> GetLicenses(DBClient db) {
			List<License> result = new List<License>();
			using(dsLicense_1_1_4_2 ds = new dsLicense_1_1_4_2(db.Session.Server)) {
				db.ViewAdditionalRows(ds, dsLicense_1_1_4_2.Schema.T.License);
				foreach(dsLicense_1_1_4_2.LicenseRow dr in ds.T.License) {
					License fromKey = new License(dr.F.License);
					License fromDecoded = new License(dr.F.ApplicationID,
						dr.F.Expiry,
						(License.ExpiryModels)dr.F.ExpiryModel,
						checked((uint)dr.F.LicenseCount),
						(License.LicenseModels)dr.F.LicenseModel,
						checked((uint)dr.F.LicenseID));
					try {
						fromKey.Match(fromDecoded);
					}
					catch(System.Exception ex) {
						throw new GeneralException(ex, KB.K("License key {0} record for application {1} contains inconsistent data"), dr.F.License, dr.F.ApplicationID);
					}
					result.Add(fromKey);
				}
			}
			return result;
		}
		public override Version GetMinAppVersion(DBClient db, DBI_Variable minAppVersionVariable) {
			// This is never called on mouldy-oldy databases, but it will be called on non-current databases which fall into the particular app's window of "current" ones.
			// TODO: As such then we need a custom dataset class which includes all the variables which might be queried. In some sense changing the list of such variables is
			// itself a DB version change requiring a new hndler.
			if(minAppVersionVariable == null)
				return new Version(0, 0, 0, 0);
			// Any DBDataSet will work for the following as we just need to get to the Variables table that exists in ALL XAFDataSets
			using(dsDBVersion_1_0_0_1_To_1_0_8_1 ds = new dsDBVersion_1_0_0_1_To_1_0_8_1(db.Session.Server)) {
				return new Version((string)GetVariableValue(db, ds, minAppVersionVariable));
			}
		}
		public override void VerifyUpgradePermission(DBClient db, Guid currentUserID) {
			bool upgradeAllowed = false;
			LoadPermissions(db, currentUserID,
				delegate (string pattern, bool grant) {
					if(pattern.Equals(KB.I("action.*"), StringComparison.CurrentCultureIgnoreCase)
						|| pattern.Equals(KB.I("*.*"))
						|| pattern.Equals(KB.I("action.upgradedatabase"), StringComparison.CurrentCultureIgnoreCase))
						upgradeAllowed = grant;
				}
			);
			if(!upgradeAllowed)
				throw new GeneralException(KB.K("To upgrade you must have 'Action.UpgradeDatabase' permission on this database"));
		}
		public override void LoadPermissions(DBClient db, Guid currentUserID, HandlePermissionPattern handler) {
			using(dsPermission_1_1_4_2 dsPerms = new dsPermission_1_1_4_2(db.Session.Server)) {
				dsPerms.DataSetName = KB.I("Security.LoadPermissions.dsPerms");
				db.ViewAdditionalRows(dsPerms, dsPermission_1_1_4_2.Schema.T.UserPermission, new SqlExpression(dsPermission_1_1_4_2.Path.T.UserPermission.F.UserID).Eq(currentUserID));
				// Because we only grant (and never revoke) rights it does not matter what order we process the permission records in.
				foreach(dsPermission_1_1_4_2.UserPermissionRow row in dsPerms.T.UserPermission)
					handler(row.F.PermissionPathPattern.ToLower(), true);
			}
		}
		public override void AddAdministratorPermissions(HandlePermissionPattern handler) {
			// apply the ITAdmin role permissions if acting as a Windows Administrator. This is in addition to any other permissions the user may have accumulated if they are a real mainboss user
			var securitySet = new Security.RightSet(dsMB.Schema, SecurityCreation.RightSetLocation);

			foreach(Security.RightSet.RoleAndPermission p in securitySet.RolesAndPermissionsFor(Security.RightSet.ITAdminUser))
				foreach(string r in new Security.RightSet.RolePermissionStrings(p.Permission))
					handler(r, true);
		}
		public override void CompleteDBIForSchemaOperations(DBI_Database schema, DBI_Database inputSchema) {
			schema.CompleteDBIForSchemaOperations(inputSchema, true, true);
		}
		public override void LogHistory(DBClient db, [Thinkage.Libraries.Translation.Translated] string subject, [Thinkage.Libraries.Translation.Translated] string text) {
			using(dsDatabaseHistory_1_0_0_337 ds = new dsDatabaseHistory_1_0_0_337(db.Session.Server)) {
				dsDatabaseHistory_1_0_0_337.DatabaseHistoryRow row = (dsDatabaseHistory_1_0_0_337.DatabaseHistoryRow)db.AddNewRowAndBases(ds, dsDatabaseHistory_1_0_0_337.Schema.T.DatabaseHistory);
				row.F.Subject = subject;
				if(text != null)
					row.F.Description = text;
				row.EndEdit();
				db.Update(ds);
			}
		}
		public override IDisposable GetUnfetteredDatabaseAccess(DBClient db) {
			return new UnfetteredDatabaseAccessNoAction();
		}
	}
	public class DBVersionRangeHandler_1_0_8_29_To_1_1_4_1 : DBVersionRangeHandler_1_1_4_2 {
		public DBVersionRangeHandler_1_0_8_29_To_1_1_4_1()
			: base() {
		}
		public override Guid? IdentifyUser(DBClient db) {
			DatabaseCreation.ParseUserIdentification(db.Session.ConnectionInformation.UserIdentification, out string userName, out string userRealm);
			using (dsUser_1_0_4_14_To_1_1_4_1 dsUser = new dsUser_1_0_4_14_To_1_1_4_1(db.Session.Server)) {
				dsUser.DataSetName = KB.I("DBVersionHandler30.IdentifyUser.dsUser");

				db.ViewAdditionalRows(dsUser, dsUser_1_0_4_14_To_1_1_4_1.Schema.T.User, new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.Hidden).IsNull()
					.And(new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.UserName).Lower().Eq(SqlExpression.Constant(userName)))
					.And(new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.ScopeName).Lower().Eq(SqlExpression.Constant(userRealm))
						.Or(new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.ScopeName).IsNull())), new SqlExpression[] {
							new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.Id),
							new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.ScopeName)
						}, null);
				// We should have at most two rows: One with a null ScopeName and one with a matching ScopeName, both with matching UserName.
				switch(dsUser.T.User.Rows.Count) {
					case 0:
						break;
					case 1:
						return ((dsUser_1_0_4_14_To_1_1_4_1.UserRow)dsUser.T.User.Rows[0]).F.Id;
					default:
						// Find the one with the non-null scopename
						DataRow[] rows = dsUser.T.User.Select(new ColumnExpressionUnparser().UnParse(new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.ScopeName).IsNotNull()));
						if(rows.Length == 1)
							return ((dsUser_1_0_4_14_To_1_1_4_1.UserRow)rows[0]).F.Id;
						break;
				}
			}
			return null;
		}
		#region UpdateUsersAfterRestore
		public override void UpdateUsersAfterRestore(DBClient client, System.Text.StringBuilder restoreErrors) {
			ISession db = client.Session;
			using(dsUser_1_0_4_14_To_1_1_4_1 ds = new dsUser_1_0_4_14_To_1_1_4_1(db.Server)) {
				DateTime now = DateTime.Now;
				var errorList = new Dictionary<string, List<UserPermissionsRefreshError>>();
				// We would prefer to do this in a transaction but the error that occurs when the security change cannot be made kills the transaction and causes any further code
				// to just get all bungled up.
				// The trigger should really be an INSTEAD OF rather than a AFTER trigger (and with no ROLLBACK on error) so if it cannot do the request the table just doesn't change.
				// By not having a TX here we could get interrupted and leave the DB present, no entry in the Organizations table, and some users left hidden.
				//client.PerformTransaction(true,
				//	delegate() {
				client.ViewOnlyRows(ds, dsUser_1_0_4_14_To_1_1_4_1.Schema.T.User, new SqlExpression(dsUser_1_0_4_14_To_1_1_4_1.Path.T.User.F.Hidden).IsNull(), null, null);
				foreach(dsUser_1_0_4_14_To_1_1_4_1.UserRow r in ds.T.User)
					r.F.Hidden = now;
				// TODO: As of db version 1.0.10.21 there is no longer a trigger causing an error when the last user is deleted. At some point we should make a new
				// version of UpdateAfterRestores that skips the disable/enable trigger used here but it is pretty harmless to leave it in.
				try {
					db.ExecuteCommand(new MSSqlLiteralCommandSpecification("disable trigger all on [user]"));
					client.Update(ds);
				}
				finally {
					db.ExecuteCommand(new MSSqlLiteralCommandSpecification("enable trigger all on [user]"));
				}
				//		// Since the TX is still in effect we must accept the just-saved changes ourselves.
				//		ds.AcceptChanges();
				// If we were to try to restore all the users in one update call, and were unable to restore some of the users
				// (e.g. the scope/user has no matching real user name), the whole thing will fail.
				// We want it to charge on, and leave the unfixable ones hidden.
				// So we do this part one at a time, and report to the user the ones we could not fix.
				foreach(dsUser_1_0_4_14_To_1_1_4_1.UserRow r in ds.T.User) {
					r.F.Hidden = null;
					try {
						client.Update(ds);
						//				// Since the TX is still in effect we must accept the just-saved changes ourselves.
						//				ds.AcceptChanges();
					}
					catch(System.Exception ex) {
						r.RejectChanges();
						var errorEntry = new UserPermissionsRefreshError(r.F.ScopeName, r.F.UserName, ex);
						if (!errorList.TryGetValue(errorEntry.KeyText, out List<UserPermissionsRefreshError> onSameKey))
							errorList.Add(errorEntry.KeyText, onSameKey = new List<UserPermissionsRefreshError>());
						onSameKey.Add(errorEntry);
					}
				}
				//	}
				//);
				if(errorList.Count > 0) {
					if(errorList.Count == 1) {
						// All the exceptions get the same message...
						// Use the exception for display with the user name(s) as detail information.
						// Dictionary has no way to get its only entry except by iterating (or knowing the key!)
						foreach(List<UserPermissionsRefreshError> entry in errorList.Values) {
							System.Exception exceptionToShow = entry[0].ExceptionObject;
							Libraries.Exception.AddContext(exceptionToShow, new MessageExceptionContext(KB.T(BuildUserList(entry))));
							restoreErrors.AppendLine(Thinkage.Libraries.Exception.FullMessage(exceptionToShow));
						}
					}
					else {
						// Several exception messages...
						// Concatenate each one and the users is applies to into one lone error message.
						foreach(List<UserPermissionsRefreshError> entry in errorList.Values) {
							restoreErrors.AppendLine(entry[0].KeyText);
							restoreErrors.AppendLine(BuildUserList(entry));
						}
					}
				}
			}
		}
		protected static string BuildUserList(List<UserPermissionsRefreshError> entry) {
			var result = new System.Text.StringBuilder(Strings.Format(KB.K("The error occurred while trying to update:"), entry[0].UserName, entry[0].ScopeName));
			for(int i = 0; i < entry.Count; i++) {
				result.AppendLine(i == 0 ? "" : ",");
				if(string.IsNullOrEmpty(entry[0].ScopeName))
					result.Append(Strings.Format(KB.K("user '{0}' with no scope"), entry[i].UserName));
				else
					result.Append(Strings.Format(KB.K("user '{0}' in scope '{1}'"), entry[i].UserName, entry[i].ScopeName));
			}
			return result.ToString();
		}
		protected class UserPermissionsRefreshError {
			public UserPermissionsRefreshError(string scopeName, string userName, System.Exception ex) {
				ExceptionObject = ex;
				KeyText = Libraries.Exception.FullMessage(ExceptionObject);
				ScopeName = scopeName;
				UserName = userName;
			}
			public readonly string KeyText;
			public readonly System.Exception ExceptionObject;
			public readonly string ScopeName;
			public readonly string UserName;
		}
		#endregion
		public override List<License> GetLicenses(DBClient db) {
			List<License> result = new List<License>();
			using(dsLicense_1_0_0_1_To_1_0_4_13 ds = new dsLicense_1_0_0_1_To_1_0_4_13(db.Session.Server)) {
				db.ViewAdditionalRows(ds, dsLicense_1_0_0_1_To_1_0_4_13.Schema.T.License);
				foreach(dsLicense_1_0_0_1_To_1_0_4_13.LicenseRow dr in ds.T.License) {
					License fromKey = new License(dr.F.License);
					License fromDecoded = new License(dr.F.ApplicationID,
						dr.F.Expiry,
						(License.ExpiryModels)dr.F.ExpiryModel,
						checked((uint)dr.F.LicenseCount),
						(License.LicenseModels)dr.F.LicenseModel,
						checked((uint)dr.F.LicenseID));
					try {
						fromKey.Match(fromDecoded);
					}
					catch(System.Exception ex) {
						throw new GeneralException(ex, KB.K("License key {0} record for application {1} contains inconsistent data"), dr.F.License, dr.F.ApplicationID);
					}
					result.Add(fromKey);
				}
			}
			return result;
		}
	}
	public class DBVersionRangeHandler_1_0_8_2_To_1_0_8_28 : DBVersionRangeHandler_1_0_8_29_To_1_1_4_1 {
		public DBVersionRangeHandler_1_0_8_2_To_1_0_8_28()
			: base() {
		}
		// Prior to 1.0.8.29 the service definition was located in DB variables rather than being in a table.
		public override IDisposable GetUnfetteredDatabaseAccess(DBClient db) {
			// We must fish the ServiceParameters required from the variables for our service configuration
			Thinkage.Libraries.Service.StaticServiceConfiguration config;
			using (dsMainBossService_1_0_0_1_To_1_0_8_28 ds = new dsMainBossService_1_0_0_1_To_1_0_8_28(db.Session.Server)) {
				db.ViewAdditionalVariables(ds, dsMainBossService_1_0_0_1_To_1_0_8_28.Schema.V.ATRServiceMachineName, dsMainBossService_1_0_0_1_To_1_0_8_28.Schema.V.ATRServiceName);
				config = new Libraries.Service.StaticServiceConfiguration((string)ds[dsMainBossService_1_0_0_1_To_1_0_8_28.Schema.V.ATRServiceMachineName].Value, (string)ds[dsMainBossService_1_0_0_1_To_1_0_8_28.Schema.V.ATRServiceName].Value);
			}
			return new StopService(config);
		}
	}
	public class DBVersionRangeHandler_1_0_6_31_To_1_0_8_1 : DBVersionRangeHandler_1_0_8_2_To_1_0_8_28 {
		public DBVersionRangeHandler_1_0_6_31_To_1_0_8_1()
			: base() {
		}
		// Prior to 1.0.8.2 the database contained no place to record server version information.
		public override void StoreDBServerVersion(DBClient db, Version serverVersion) {
			throw new NotImplementedException();
		}
		public override Version GetDBServerVersion(DBClient db) {
			return new Version(0, 0, 0, 0);
		}
	}
	public class DBVersionRangeHandler_1_0_4_79_To_1_0_6_30 : DBVersionRangeHandler_1_0_6_31_To_1_0_8_1 {
		public DBVersionRangeHandler_1_0_4_79_To_1_0_6_30()
			: base() {
		}
		// Prior to 1.0.6.31 ... what?
		// TODO: Why not use the more recent code anyway? All the DB variables are still there; at worst the more recent code would send a Pause command to the service which would be ignored, n'est-ce pas?
		// Possibly 1.0.6.31 happened to be the current version when we introduced this method but we just failed to back-implement it?
		public override IDisposable GetUnfetteredDatabaseAccess(DBClient db) {
			return new UnfetteredDatabaseAccessNoAction();
		}
	}
	public class DBVersionRangeHandler_1_0_2_45_To_1_0_4_78 : DBVersionRangeHandler_1_0_4_79_To_1_0_6_30 {
		public DBVersionRangeHandler_1_0_2_45_To_1_0_4_78()
			: base() {
		}
		// Prior to 1.0.4.14 permissions were tied directly to the User rather than to the Principal, and groups were "done" (though never exposed) using an IsGroup flag.
		// Also, the ability to grant/revoke existed.
		public override void LoadPermissions(DBClient db, Guid currentUserID, HandlePermissionPattern handler) {
			// This now handles separate Scope Name and User Name.
			// TODO: Verify that LoggedInUserActsAs will behave if the scope name is null (it must return true as long as the UserName part matches).
			using (dsPermission_1_0_0_1_To_1_0_4_13 dsPerms = new dsPermission_1_0_0_1_To_1_0_4_13(db.Session.Server)) {
				dsPerms.DataSetName = KB.I("Security.LoadPermissions.dsPerms");
				db.ViewAdditionalRows(dsPerms, dsPermission_1_0_0_1_To_1_0_4_13.Schema.T.User, new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.IsGroup).IsTrue()
					.And(new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.Hidden).IsNull()), new SqlExpression[] {
							new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.Id),
							new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.UserName),
							new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.ScopeName),
							new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.Hidden),
							new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.User.F.IsGroup)
						}, null);
				// We ultimately want these records sorted by lowercased permission path pattern, so that the '*'
				// at any level appears before the specific entries, and also so Xxx and xxx appear together.
				// But right now we have no way of specifying a function call (in this case Lower(xxx) in a column list
				// so we must assume the table contains only properly-cased entries.
				db.ViewAdditionalRows(dsPerms, dsPermission_1_0_0_1_To_1_0_4_13.Schema.T.Permission);

				handler(KB.I("Table.*.*").ToLower(), true);
				handler(KB.I("Action.*").ToLower(), true);
				handler(KB.I("Action.Administration").ToLower(), DefaultAdministrationPermission);
				foreach (dsPermission_1_0_0_1_To_1_0_4_13.UserRow userRow in dsPerms.T.User)
					if (Thinkage.Libraries.Application.LoggedInUserActsAs(userRow.F.ScopeName + '\\' + userRow.F.UserName))
						LoadUserPermissions(dsPerms, userRow.F.Id, handler);

				LoadUserPermissions(dsPerms, currentUserID, handler);
			}
		}
		// The following is for the benefit of DBVersionRangeHandler_1_0_0_390_To_1_0_2_44
		// As of 1.0.2.45, Action.Administration *should* be either granted or revoked by a permission record for all users, but in case someone just creates a user record (e.g. using a SQL command)
		// it would not immediately have permissions, so we set a default permission here to false. Before 1.0.2.45 there was no such Permission record so DBVersionRangeHandler_1_0_0_390_To_1_0_2_44
 		// overrides this to return true.
		protected virtual bool DefaultAdministrationPermission { get { return false; } }
		protected static void LoadUserPermissions(dsPermission_1_0_0_1_To_1_0_4_13 dsPerms, Guid userID, HandlePermissionPattern handler) {
			System.Data.DataRow[] view = dsPerms.T.Permission.Select(new ColumnExpressionUnparser().UnParse(new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.Permission.F.UserID).Eq(userID)),
									new ColumnExpressionUnparser().UnParse(new SqlExpression(dsPermission_1_0_0_1_To_1_0_4_13.Path.T.Permission.F.PermissionPathPattern)));
			foreach (dsPermission_1_0_0_1_To_1_0_4_13.PermissionRow row in view)
				handler(row.F.PermissionPathPattern.ToLower(), row.F.Grant);
		}
		// Prior to 1.0.4.14 the User record contained an IsGroup flag which we had to filter for false.
		public override Guid? IdentifyUser(DBClient db) {
			string userIdentification = Thinkage.Libraries.Application.Instance.UserName;

			using (dsUser_1_0_0_1_To_1_0_4_13 dsUser = new dsUser_1_0_0_1_To_1_0_4_13(db.Session.Server)) {
				dsUser.DataSetName = KB.I("DBVersionHandler30.IdentifyUser.dsUser");
				string[] splitUser = userIdentification.ToLower().Split('\\');
				if (splitUser.Length != 2)
					throw new GeneralException(KB.K("The name of the current user \"{0}\" is not in the expected form \"SCOPE\\USER\""), userIdentification);

				db.ViewAdditionalRows(dsUser, dsUser_1_0_0_1_To_1_0_4_13.Schema.T.User, new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.IsGroup).IsFalse()
					.And(new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.Hidden).IsNull())
					.And(new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.UserName).Lower().Eq(SqlExpression.Constant(splitUser[1])))
					.And(new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.ScopeName).Lower().Eq(SqlExpression.Constant(splitUser[0]))
						.Or(new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.ScopeName).IsNull())), new SqlExpression[] {
							new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.Id),
							new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.ScopeName)
						}, null);
				// We should have at most two rows: One with a null ScopeName and one with a matching ScopeName, both with matching UserName.
				switch (dsUser.T.User.Rows.Count) {
				case 0:
					break;
				case 1:
					return ((dsUser_1_0_0_1_To_1_0_4_13.UserRow)dsUser.T.User.Rows[0]).F.Id;
				default:
					// Find the one with the non-null scopename
					DataRow[] rows = dsUser.T.User.Select(new ColumnExpressionUnparser().UnParse(new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.ScopeName).IsNotNull()));
					if (rows.Length == 1)
						return ((dsUser_1_0_0_1_To_1_0_4_13.UserRow)rows[0]).F.Id;
					break;
				}
			}
			return null;
		}
		// Prior to 1.0.4.14 the permissions required to upgrade were different. One needed both action.upgradedatabase and action.administration.
		public override void VerifyUpgradePermission(DBClient db, Guid currentUserID) {
			bool upgradeAllowed = false;
			bool adminAllowed = false;

			LoadPermissions(db, currentUserID, delegate(string pattern, bool grant) {
				if (pattern == KB.I("action.*")
					|| pattern == KB.I("*.*")) {
					upgradeAllowed = grant;
					adminAllowed = grant;
				}
				else if (pattern == KB.I("action.upgradedatabase")
					|| pattern == KB.I("*.upgradedatabase"))
					upgradeAllowed = grant;
				else if (pattern == KB.I("action.administration")
					|| pattern == KB.I("*.administration"))
					adminAllowed = grant;
			});

			if (!upgradeAllowed)
				throw new GeneralException(KB.K("To upgrade you must have 'Action.UpgradeDatabase' permission on this database"));
			if (!adminAllowed)
				throw new GeneralException(KB.K("To upgrade you must have 'Action.Administration' permission on this database"));
		}
		public override void UpdateUsersAfterRestore(DBClient client, System.Text.StringBuilder restoreErrors) {
			ISession db = client.Session;
			using (dsUser_1_0_0_1_To_1_0_4_13 ds = new dsUser_1_0_0_1_To_1_0_4_13(db.Server)) {
				DateTime now = DateTime.Now;
				var errorList = new Dictionary<string, List<UserPermissionsRefreshError>>();
				// We would prefer to do this in a transaction but the error that occurs when the security change cannot be made kills the transaction and causes any further code
				// to just get all bungled up.
				// The trigger should really be an INSTEAD OF rather than a AFTER trigger (and with no ROLLBACK on error) so if it cannot do the request the table just doesn't change.
				// By not having a TX here we could get interrupted and leave the DB present, no entry in the Organizations table, and some users left hidden.
				//client.PerformTransaction(true,
				//	delegate() {
				client.ViewOnlyRows(ds, dsUser_1_0_0_1_To_1_0_4_13.Schema.T.User, new SqlExpression(dsUser_1_0_0_1_To_1_0_4_13.Path.T.User.F.Hidden).IsNull(), null, null);
				foreach (dsUser_1_0_0_1_To_1_0_4_13.UserRow r in ds.T.User)
					r.F.Hidden = now;
				try {
					db.ExecuteCommand(new MSSqlLiteralCommandSpecification("disable trigger all on [user]"));
					client.Update(ds);
				}
				finally {
					db.ExecuteCommand(new MSSqlLiteralCommandSpecification("enable trigger all on [user]"));
				}
				//		// Since the TX is still in effect we must accept the just-saved changes ourselves.
				//		ds.AcceptChanges();
				// If we were to try to restore all the users in one update call, and were unable to restore some of the users
				// (e.g. the scope/user has no matching real user name), the whole thing will fail.
				// We want it to charge on, and leave the unfixable ones hidden.
				// So we do this part one at a time, and report to the user the ones we could not fix.
				foreach (dsUser_1_0_0_1_To_1_0_4_13.UserRow r in ds.T.User) {
					r.F.Hidden = null;
					try {
						client.Update(ds);
						//				// Since the TX is still in effect we must accept the just-saved changes ourselves.
						//				ds.AcceptChanges();
					}
					catch (System.Exception ex) {
						r.RejectChanges();
						var errorEntry = new UserPermissionsRefreshError(r.F.ScopeName, r.F.UserName, ex);
						if (!errorList.TryGetValue(errorEntry.KeyText, out List<UserPermissionsRefreshError> onSameKey))
							errorList.Add(errorEntry.KeyText, onSameKey = new List<UserPermissionsRefreshError>());
						onSameKey.Add(errorEntry);
					}
				}
				//	}
				//);
				if (errorList.Count > 0) {
					if (errorList.Count == 1) {
						// All the exceptions get the same message...
						// Use the exception for display with the user name(s) as detail information.
						// Dictionary has no way to get its only entry except by iterating (or knowing the key!)
						foreach (List<UserPermissionsRefreshError> entry in errorList.Values) {
							System.Exception exceptionToShow = entry[0].ExceptionObject;
							Libraries.Exception.AddContext(exceptionToShow, new MessageExceptionContext(KB.T(BuildUserList(entry))));
							restoreErrors.AppendLine(Thinkage.Libraries.Exception.FullMessage(exceptionToShow));
						}
					}
					else {
						// Several exception messages...
						// Concatenate each one and the users is applies to into one lone error message.
						foreach (List<UserPermissionsRefreshError> entry in errorList.Values) {
							restoreErrors.AppendLine(entry[0].KeyText);
							restoreErrors.AppendLine(BuildUserList(entry));
						}
					}
				}
			}
		}
	}
	public class DBVersionRangeHandler_1_0_1_0_To_1_0_2_44 : DBVersionRangeHandler_1_0_2_45_To_1_0_4_78 {
		public DBVersionRangeHandler_1_0_1_0_To_1_0_2_44()
			: base() {
		}
		// Prior to 1.0.2.45 there was no permission record either granting nor revoking Action.Administration, so we have to grant it by default.
		protected override bool DefaultAdministrationPermission { get { return true; } }
	}
}