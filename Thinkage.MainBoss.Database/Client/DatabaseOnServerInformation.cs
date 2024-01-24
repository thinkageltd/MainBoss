using System;
using System.Net;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary.MSSql;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// Provide information on databases that look like MainBoss Databases as identified in the provided SqlClient.Connection
	/// </summary>
	public class DatabaseOnServerInformation {
		public DatabaseOnServerInformation(SqlClient.Connection c) {
			Id = unchecked((ulong)System.Threading.Interlocked.Increment(ref Counter));
			ServerName = c.DBServer;
			DatabaseName = c.DBName;
			DBClient db = null;
			//
			// do a fast check for the existence of the machine name
			// it allows a better error message
			// if the ServerName contains starts with a . it means the local machine
			// if it contans a '(' '=' or ':' which will be in connections to localdb or named pipes
			// etc, we will skip the test
			//
			var serverMachine = ServerName.Split(KB.I("\\,").ToCharArray())[0]; // '\\' is for instance name, ',' is for port, named pipes, '(' local db
			if (serverMachine.Length > 0 && serverMachine.IndexOfAny(KB.I("(=:").ToCharArray()) == -1 && ServerName[0] != '.') {
				try {
					var addresses = Dns.GetHostAddresses(serverMachine);
				}
				catch (Exception ex) {
					AccessError = Libraries.Exception.FullMessage(new Thinkage.Libraries.GeneralException(ex, KB.K("No access to computer '{0}'"), ServerName.Split('\\')[0]));
					Access = KB.K("None").Translate();
					return;
				}
			}
			Access = "";
			try {
				c.DBConnectTimeout = new TimeSpan(0, 0, 10);
				c.DBTimeout = new TimeSpan(0, 0, 10);
				db = new DBClient(new DBClient.Connection(c, Database.dsMB.Schema));
			}
			catch (Exception ex) {
				Access = KB.K("SQL User").Translate();
				AccessError = Libraries.Exception.FullMessage(ex);
				if (db != null)
					db.CloseDatabase();
				return;
			}
			try {
				var handler = new DBVersionHandler();
				handler.LoadDBVersion(db);
				// Version
				Version = handler.CurrentVersion.ToString();
				// Add warning to AccessError indicating Version is < current maximum version in Version History Upgrades
				if (MBUpgrader.UpgradeInformation.LatestDBVersion > handler.CurrentVersion)
					AccessError = Libraries.Strings.Format(KB.K("Upgrade available to {0}"), MBUpgrader.UpgradeInformation.LatestDBVersion);
				// Presence of record for current User
				try {
					handler.IdentifyUser(db);
					UserRecordExists = true;
					Access = KB.K("MainBoss User").Translate();
					if (db.Session.CanManageUserCredentials())
						Access += Libraries.Strings.IFormat(" ({0})", KB.K("User Admin").Translate());
					if (db.Session.CanManageUserLogins())
						Access += Libraries.Strings.IFormat(" ({0})", KB.K("Login Admin").Translate());
				}
				catch (System.Exception ex) {
					Access = KB.K("SQL User").Translate();
					if (db.Session.CanManageUserCredentials())
						Access += Libraries.Strings.IFormat(" ({0})", KB.K("User Admin").Translate());
					if (db.Session.CanManageUserLogins())
						Access += Libraries.Strings.IFormat(" ({0})", KB.K("Login Admin").Translate());
					AccessError = Libraries.Exception.FullMessage(new Thinkage.Libraries.GeneralException(ex, KB.K("No MainBoss User record for '{0}'"), db.Session.ConnectionInformation.UserIdentification));
				}
				CanDropDatabase = db.Session.CanDropDatabase();

				// Organization name
				if (handler.CurrentVersion < new Version(1, 0, 6, 43)) {
					// TODO: Fetch the variable which contains the id of the Location record
					// TODO: Fetch the ??? of the referenced Location record
				}
				else {
					// We want a dsMB to access the OrganizationName variable, and this requires at least an XAFClient
					// so we create a new XAFClient using db's ISession object, then we DO NOT call CloseDatabase on it,
					// leaving the ISession object intact for db's use.
					var XAFdb = new XAFClient(new DBClient.Connection(c, Database.dsMB.Schema), db.Session);
					using (dsMB ds = new dsMB(XAFdb)) {
						XAFdb.ViewAdditionalVariables(ds, dsMB.Schema.V.OrganizationName);
						OrganizationName = ds.V.OrganizationName.Value;
					}
				}
			}
			catch (System.Exception e2) {
				// TODO: If the exception is due to the lack of a user record, set UserRecordExists = false.
				// TODO: Also distinguish presence of a User record from Admin access
				AccessError = Libraries.Exception.FullMessage(e2);
				Access = KB.K("Not User").Translate();
			}
			finally {
				if (db != null)
					db.CloseDatabase();
			}
		}
		public DatabaseOnServerInformation(string serverName, System.Exception error) {
			// This is used if we are unable to enumerate the databases on the server
			Id = unchecked((ulong)System.Threading.Interlocked.Increment(ref Counter));
			ServerName = serverName;
			AccessError = Libraries.Exception.FullMessage(error);
		}
		public readonly ulong Id;
		public readonly string ServerName;
		public readonly string DatabaseName;
		public readonly string OrganizationName;
		public readonly bool? UserRecordExists;
		public readonly string Version;
		public readonly string Access;
		public readonly string AccessError;
		public readonly bool CanDropDatabase;
		private static long Counter = 0;
	};
}
