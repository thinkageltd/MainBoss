using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Win32;
using Thinkage.Libraries;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database {
	public class SqlServersSession : EnumerableDrivenSession<DataRow, DataRow> {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0052:Remove unread private members", Justification = " this static will make sure KB.K registers the translation tables when this class is referenced.")]
		private static readonly Thinkage.Libraries.Translation.Key forceLoadTranslations = KB.K("");
		#region Connection
		public class Connection : IConnectionInformation {
			public Connection() {
			}

			#region IConnectionInformation Members
			public IServer CreateServer() {
				return new SqlServersServer();
			}
			public string DisplayName {
				get {
					return KB.K("¯SQL Servers Enumerator").Translate();
				}
			}
			public string DisplayNameLowercase {
				get {
					return KB.K("SQL servers enumerator").Translate();
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
		public class SqlServersServer : EnumerableDrivenSession<DataRow, DataRow>.EnumerableDrivenServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {
				return new SqlServersSession((Connection)connectInfo, this);
			}
		}
		#endregion
		#region Constructor
		public SqlServersSession(IConnectionInformation connection, IServer server)
			: base(connection, server) {
		}
		#endregion
		#region Destruction/Disposal
		public override void CloseDatabase() {
			base.CloseDatabase();
		}
		#endregion
		#region Properties
		public override DBI_Database Schema {
			get {
				return dsSqlServers.Schema;
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
		#endregion
		#region Overrides to support base class abstraction
		protected override DataRow PrepareItemForRead(DataRow item) {
			return item;
		}
		protected override DataRow PrepareItemForWrite(DataRow item) {
			return item;
		}
		#region - ServerIdentification
		public override string ServerIdentification {
			get {
				return KB.I("Thinkage SQL Server Enumerator");
			}
		}
		#endregion
		private DataColumn ServerNameColumn;
		private DataColumn InstanceNameColumn;
		private DataColumn IsClusteredColumn;
		private DataColumn VersionColumn;

		#region - GetEvaluators (returns delegates that produce column values)
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			normalUpdater = delegate (DataRow e, object v) {
				throw new NotImplementedException();
			};
			if (sourceColumnSchema == dsSqlServers.Schema.T.SqlServers.F.Id) {
				normalEvaluator = delegate (DataRow e) {
					return System.Text.Encoding.Unicode.GetBytes(e[InstanceNameColumn] is DBNull ? (string)e[ServerNameColumn] : Strings.IFormat("{0}\\{1}", e[ServerNameColumn], e[InstanceNameColumn]));
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return new System.Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
				};
			}
			else if (sourceColumnSchema == dsSqlServers.Schema.T.SqlServers.F.Server) {
				normalEvaluator = delegate (DataRow e) {
					return e[ServerNameColumn];
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSqlServers.Schema.T.SqlServers.F.Instance) {
				normalEvaluator = delegate (DataRow e) {
					return e[InstanceNameColumn] is DBNull ? null : e[InstanceNameColumn];
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return Libraries.Exception.FullMessage(e);
				};
			}
			else if (sourceColumnSchema == dsSqlServers.Schema.T.SqlServers.F.Name) {
				normalEvaluator = delegate (DataRow e) {
					return e[InstanceNameColumn] is DBNull ? e[ServerNameColumn] : Strings.IFormat("{0}\\{1}", e[ServerNameColumn], e[InstanceNameColumn]);
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSqlServers.Schema.T.SqlServers.F.IsClustered) {
				normalEvaluator = delegate (DataRow e) {
					// TODO: Not sure what this column contains! Apparently 'No' and probably 'Yes'
					return e[IsClusteredColumn];
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return false;
				};
			}
			else if (sourceColumnSchema == dsSqlServers.Schema.T.SqlServers.F.Version) {
				normalEvaluator = delegate (DataRow e) {
					return e[VersionColumn] is DBNull ? null : e[VersionColumn];
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("0.0.0.0");
				};
			}
			else
				throw new GeneralException(KB.K("Unknown source column in ColumnEvaluator"));
		}
		#endregion
		#region - GetItemEnumerable (returns an enumerable of the ItemT containing the driving data)
		protected override IEnumerable<DataRow> GetItemEnumerable(DBI_Table dbit) {
			DataTable dt = Thinkage.Libraries.XAF.Database.Service.MSSql.SqlClient.SqlServer.ListDatabaseServers();
			ServerNameColumn = dt.Columns["ServerName"];
			InstanceNameColumn = dt.Columns["InstanceName"];
			IsClusteredColumn = dt.Columns["IsClustered"];
			VersionColumn = dt.Columns["Version"];
			var instances = LocalSqlServers();
			List<DataRow> localRows = new List<DataRow>();
			List<DataRow> localNotFound = new List<DataRow>();
			//
			// when the SQL browser is not running SqlDataSourceEnumerator seems to return an unnamed instance for the 
			// local machine, even if it doesn't exist, and even if there is an actual named instance
			// 
			foreach (DataRow row in dt.Rows) {
				if (!DomainAndIP.IsThisComputer(row["ServerName"].ToString()))
					continue;
				if (!instances.Any(e => string.Compare(row["InstanceName"].ToString(), e, true) == 0))
					localNotFound.Add(row);
				else
					localRows.Add(row);
			}
			bool added = false;
			foreach (var i in instances) {
				if (localRows.Any(e => string.Compare(e["InstanceName"].ToString(), i, true) == 0))
					continue;
				var newrow = dt.NewRow();
				newrow["ServerName"] = Environment.MachineName;
				newrow["InstanceName"] = string.IsNullOrWhiteSpace(i) ? null : i;
				newrow["IsClustered"] = null;
				newrow["Version"] = null;
				dt.Rows.Add(newrow);
				added = true;
			}
			//
			// if we added local instance and we have exactly one unknown instance and it is the empty then assume 
			// it is a incorrect one created by SqlDataSourceEnumerator
			//
			if (added && localNotFound.Count == 1 && string.IsNullOrWhiteSpace(localNotFound.First()["InstanceName"].ToString()))
				localNotFound.First().Delete();
			return dt.Rows.Cast<DataRow>();
		}
		#endregion
		#region LocalSqlServers
		private List<string> LocalSqlServers() {
			List<string> instances = new List<string>();
			try {
				RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
				RegistryKey key = baseKey.OpenSubKey(KB.I(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"));
				foreach (string s in key.GetValueNames())
					if (!string.IsNullOrWhiteSpace(s))
						instances.Add(string.Compare(s, KB.I("mssqlserver"), true) == 0 ? "" : s);
				key.Close();
				baseKey.Close();
			}
			catch (System.Exception) { } // if the registry fails we get no local list
			return instances;
		}

		#endregion
		#endregion
	}
}

