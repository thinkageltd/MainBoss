using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
#if OLD

	[System.Diagnostics.DebuggerDisplay("Server={DBserver Database={Name} Version={MainBossVersion} Organization={OrganizationName} Access={Access}")]
	public struct ServerDatabase {
		public string DBServer;
		public string Name;
		public string MainBossVersion;
		public string OrganizationName;
		public bool? Access;
	}
	public class DatabasesOnServer : IEnumerable<ServerDatabase> {
		public string DBServer;
		public List<ServerDatabase> Databases;
		SqlClient.Connection Connection;
		private System.Data.DataTable names;
		private bool IgnoreErrors;
		private readonly string userid;
		private readonly string domain;
		private readonly bool CheckUserAccess;
		public delegate bool ExitNow();
		public DatabasesOnServer(string dbServer, bool ignoreErrors, bool checkUserAccess, string u, string d, IProgressDisplay ipd) {
			userid = SqlClient.SqlServer.SqlLiteral(u);
			domain = SqlClient.SqlServer.SqlLiteral(d);
			CheckUserAccess = checkUserAccess;
			DBServer = dbServer;
			IgnoreErrors = ignoreErrors;
			Connection = new SqlClient.MasterConnection(DBServer, Libraries.DBILibrary.AuthenticationCredentials.Default);
			Connection.DBConnectTimeout = new TimeSpan(0, 0, 1);
			Refresh(ipd);
		}
		public void Refresh(IProgressDisplay ipd) {
			Databases = new List<ServerDatabase>();
			ipd.Update(KB.K("Obtaining list of databases"));
			if (!IgnoreErrors)
				names = SqlClient.ListDatabaseNames(Connection);
			else {
				try {
					names = null;
					names = SqlClient.ListDatabaseNames(Connection);
				}
				catch (System.Data.SqlClient.SqlException) { }
				catch (GeneralException) { }
			}
			if (names == null) {
				var database = new ServerDatabase();
				database.DBServer = DBServer;
				database.MainBossVersion = KB.K("Not Available").Translate();
				Databases.Add(database);
				return;
			}
			var dbnames = from System.Data.DataRow r in names.Rows let sn = r["DataBaseName"] as string where !string.IsNullOrEmpty(sn) select sn;
			var dblist = dbnames.AsParallel().WithDegreeOfParallelism(4).Select(name => {
				var database = new ServerDatabase() { DBServer = DBServer, Name = name };
				MB3Client dbsession = null;
				try {
					ipd.Update(KB.T(Strings.Format(KB.K("Opening database '{0}'"), name)));
					dbsession = null;
					var mbconnection = new MB3Client.ConnectionDefinition(Connection.DBServer, database.Name, Connection.Credentials);
					((SqlClient.Connection)mbconnection.ConnectionInformation).DBConnectTimeout = new TimeSpan(0, 0, 10);
					((SqlClient.Connection)mbconnection.ConnectionInformation).DBTimeout = new TimeSpan(0, 0, 10);
					//System.Threading.Thread.Sleep(2000);	// To give a chance to try cancelling the operation
					dbsession = new MB3Client(mbconnection);
					ipd.Update(KB.T(Strings.Format(KB.K("Testing database '{0}' for MainBoss fingerprint, version, and permissions"), name)));
					// TODO: The following should be done using DBVersionHandler methods (possibly new ones)
					//System.Threading.Thread.Sleep(2000);	// To give a chance to try cancelling the operation
					database.MainBossVersion = SQLValue(dbsession, dsMB.Schema.V.DBVersion.EffectiveType, "select dbo._vgetDBVersion()");
					if (database.MainBossVersion == null) // cann't be a MainBoss database
						database.Access = null;
					else {
						Version dbVersion = new Version(database.MainBossVersion);
						if (dbVersion < new Version(1, 0, 6, 43)) // at this version the Organization name was the CompanyLocationID code; See Upgrade History
							database.OrganizationName = SQLValue(dbsession, dsMB.Schema.T.Location.F.Code.EffectiveType, "select [Code] from [Location] where [Id] = (select dbo._vgetCompanyLocationID())");
						else
							database.OrganizationName = SQLValue(dbsession, dsMB.Schema.V.OrganizationName.EffectiveType, "select dbo._vgetOrganizationName()");
						if (CheckUserAccess) {
							database.Access = false;
							var username = SQLValue(dbsession, dsMB.Schema.T.User.F.UserName.EffectiveType.UnionCompatible(NullTypeInfo.Universe), Strings.IFormat("select UserName from [User] where Hidden is null and Lower(UserName) = {0} and ( ScopeName is null or Lower(ScopeName) = {1})", userid, domain));
							database.Access = username != null && username != "";
						}
						else
							database.Access = null; // don't know
					}
				}
				// The following is sort of like InterpretException except dumbed down to make short messages.
				// it also "knows" what exceptions are already reinterpreted (no permissions)
				catch (System.Data.SqlClient.SqlException e) {
					if (e.Number == (int)Libraries.DBILibrary.MSSql.SqlClient.MSSqlServerErrors.InvalidColumnReference)
						database.MainBossVersion = KB.K("Not a MainBoss database").Translate();
					else if (e.Number == (int)Libraries.DBILibrary.MSSql.SqlClient.MSSqlServerErrors.ConnectionTimeout)
						database.MainBossVersion = KB.K("Request timed out").Translate();
					else
						database.MainBossVersion = KB.K("Unable to access").Translate();
				}
				catch (Thinkage.Libraries.DBILibrary.MSSql.SqlClient.SqlConnectException) {
					database.MainBossVersion = KB.K("Unable to access").Translate();
				}
				catch (GeneralException e) {
					var se = e.InnerException as System.Data.SqlClient.SqlException;
					if (se != null && se.Number == (int)Libraries.DBILibrary.MSSql.SqlClient.MSSqlServerErrors.UserHasNoDatabasePermissions)
						database.MainBossVersion = KB.K("No permissions").Translate();
					else
						database.MainBossVersion = KB.K("Unable to access").Translate();
				}
				finally {
					if (dbsession != null)
						dbsession.CloseDatabase();
				}
				return database;
			});
			Databases = dblist.OrderBy(e => e.Name).ToList();
		}
		string SQLValue(MB3Client dbsession, Thinkage.Libraries.TypeInfo.TypeInfo stringTypeInfo, [Invariant]string SQLCommand) {
			return (string)Thinkage.Libraries.TypeInfo.StringTypeInfo.AsNativeType(dbsession.Session.ExecuteCommandReturningScalar(stringTypeInfo, new MSSqlLiteralCommandSpecification(SQLCommand)), typeof(string));
		}
		public IEnumerator<ServerDatabase> GetEnumerator() {
			if (Databases != null)
				foreach (var d in Databases) yield return d;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			foreach (var d in Databases) yield return d;
		}
	}
#endif
}
