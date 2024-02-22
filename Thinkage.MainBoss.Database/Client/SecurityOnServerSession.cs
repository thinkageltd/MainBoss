using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Thinkage.Libraries.XAF.Database.Layout;
using System.Linq;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.MainBoss {
	public class SecurityOnServerSession : EnumerableDrivenSessionWithUpdate<SqlClient.SqlServer.SecurityOnServerInformation, SqlClient.SqlServer.SecurityOnServerInformation> {
		#region Connection
		public class Connection : IConnectionInformation {
			public Connection(DBClient existingClient, bool forLogins) {
				if (existingClient.Session is SecurityOnServerSession)
					SqlDBClient = ((SecurityOnServerSession)existingClient.Session).ConnectionObject.SqlDBClient;
				else
					SqlDBClient = (SqlClient)existingClient.Session;
				DatabaseLoginManager = forLogins;
			}
			public readonly SqlClient SqlDBClient;
			public readonly bool DatabaseLoginManager;
			#region IConnectionInformation Members
			public IServer CreateServer() {
				return new SecurityOnServerServer(SqlDBClient);
			}
			public string DisplayName {
				get {
					return KB.K("¯SQL Security Enumerator").Translate();
				}
			}
			public string DisplayNameLowercase {
				get {
					return KB.K("SQL security enumerator").Translate();
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
		public class SecurityOnServerServer : EnumerableDrivenSession<SqlClient.SqlServer.SecurityOnServerInformation, SqlClient.SqlServer.SecurityOnServerInformation>.EnumerableDrivenServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {
				return new SecurityOnServerSession((Connection)connectInfo, this);
			}
			public override Thinkage.Libraries.Collections.Set<AuthenticationMethod> PermittedAuthenticationMethods(IConnectionInformation connectInfo, bool forLogins) {
				return SqlDBClient.Server.PermittedAuthenticationMethods(SqlDBClient.ConnectionInformation, forLogins);
			}
			public SecurityOnServerServer(SqlClient sqlClient) {
				SqlDBClient = sqlClient;
			}
			internal readonly SqlClient SqlDBClient;
		}
		#endregion
		#region Constructor
		public SecurityOnServerSession(IConnectionInformation connection, IServer server)
			: base(connection, server) {
//			if( ForDatabaseLoginUsers && !CanManageUserLogins())
//				throw new Libraries.GeneralException(KB.K("YOU CANNOT DO THIS to LOGINS"));
//			if (!CanManageUserCredentials())
//				throw new Libraries.GeneralException(KB.K("YOU CANNOT DO THIS Users"));
		}
		#endregion
		#region Destruction/Disposal
		public override void CloseDatabase() {
			base.CloseDatabase();
		}
		#endregion
		#region Properties
		public bool ForDatabaseLoginUsers {  get { return ConnectionObject.DatabaseLoginManager; } }
		public override DBI_Database Schema {
			get {
				return dsSecurityOnServer.Schema;
			}
			set {
				throw new NotImplementedException();
			}
		}
		protected new Connection ConnectionObject { get { return (Connection)base.ConnectionObject; } }
		#endregion
		#region Overrides to support base class abstraction
		public override bool CanManageUserCredentials() {
			// use the underlying SqlClient in our server for the answer
			return ((SecurityOnServerServer)this.Server).SqlDBClient.CanManageUserCredentials();
		}
		public override bool CanViewUserCredentials() {
			// use the underlying SqlClient in our server for the answer
			return ((SecurityOnServerServer)this.Server).SqlDBClient.CanViewUserCredentials();
		}

		public override bool CanManageUserLogins() {
			return ((SecurityOnServerServer)this.Server).SqlDBClient.CanManageUserLogins();
		}
		public override bool CanViewUserLogins() {
			return ((SecurityOnServerServer)this.Server).SqlDBClient.CanViewUserLogins();
		}

		protected override SqlClient.SqlServer.SecurityOnServerInformation PrepareItemForRead(SqlClient.SqlServer.SecurityOnServerInformation item) {
			return item;
		}
		protected override SqlClient.SqlServer.SecurityOnServerInformation PrepareItemForWrite(SqlClient.SqlServer.SecurityOnServerInformation item) {
			return item;
		}
		#region - ServerIdentification
		public override string ServerIdentification {
			get { return KB.I("Thinkage SQL Security Enumerator"); }
		}
		#endregion
		#region - GetEvaluators (returns delegates that produce column values)
		/// <summary>
		/// To ensure recaching of records in the CacheManager, we add a unique value (increasing) to each row returned.
		/// </summary>
		long GenerationStampValue = 0;
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			normalUpdater = delegate(SqlClient.SqlServer.SecurityOnServerInformation e, object v)
			{
				throw new NotImplementedException();
			};
			if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.Id) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.Id;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return ((Libraries.TypeInfo.IntegralTypeInfo)dsSecurityOnServer.Schema.T.SecurityOnServer.F.Id.EffectiveType).NativeMaxLimit(typeof(ulong));
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.LoginName) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.LoginName;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.Password) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return null;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.CredentialAuthenticationMethod) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.AuthenticationType;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.DBUserName) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.DBUsername;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.InMainBossRole) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.InUserRole;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.IsSysAdmin) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.IsSysAdmin;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.IsLoginManager) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.IsLoginManager;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.Enabled) {
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.Enabled;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if(sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.Generation) {
				normalUpdater = null;
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return GenerationStampValue++;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsSecurityOnServer.Schema.T.SecurityOnServer.F.IsDBO) {
				normalUpdater = null;
				normalEvaluator = delegate (SqlClient.SqlServer.SecurityOnServerInformation e) {
					return e.IsDBO;
				};
				exceptionEvaluator = delegate (System.Exception e) {
					return KB.I("Error");
				};
			}
			else
				throw new Libraries.GeneralException(KB.K("Unknown source column in ColumnEvaluator"));
		}
		#endregion
		#region - GetItemEnumerable (returns an enumerable of the ItemT containing the driving data)
		protected override IEnumerable<SqlClient.SqlServer.SecurityOnServerInformation> GetItemEnumerable(DBI_Table dbit) {
			if (ConnectionObject.DatabaseLoginManager)
				return SqlClient.SqlServer.ListDatabaseLogins(ConnectionObject.SqlDBClient);
			else
				return SqlClient.SqlServer.ListDatabaseUsers(ConnectionObject.SqlDBClient, KB.I("MainBoss"));
		}
		public override int UpdateGivenRowsInSingleTable(DBI_Table dbit, DataRow[] rows, ServerExtensions.UpdateOptions options) {
			int changedRowCount = 0;
			// Quick check if we have nothing to do, then exit immediately
			if (rows.Length == 0)
				return changedRowCount;

			System.Diagnostics.Debug.Assert(rows[0].Table is DBIDataTable);
			DBIDataTable dt = (DBIDataTable)rows[0].Table;
			System.Diagnostics.Debug.Assert(dt.Schema == dbit, "Update rows' table does not match schema table");
			// Fill the DataTable with the columns that were asked for
			for (int i = rows.Length; --i >= 0;) {
				dsSecurityOnServer.SecurityOnServerRow row = (dsSecurityOnServer.SecurityOnServerRow)rows[i];
				if (row.RowState == DataRowState.Unchanged || row.RowState == DataRowState.Detached)
					continue;
				if (row.RowState == DataRowState.Modified) {
					throw new NotImplementedException("Cannot update authentication credentials");
				}
				System.Diagnostics.Debug.Assert(rows[i].Table == dt, "UpdateTable: Not all rows belong to the same table");
				//				object id = dbit.InternalIdColumn[row, row.RowState == DataRowState.Added ? DataRowVersion.Current : DataRowVersion.Original];
				object id = null;
				if (row.RowState == DataRowState.Added) {
					if (ConnectionObject.DatabaseLoginManager) {
						ConnectionObject.SqlDBClient.CreateUserLogin(new AuthenticationCredentials((AuthenticationMethod)row.F.CredentialAuthenticationMethod,
							row.F.LoginName,
							row.F.Password));
						foreach (var newRow in GetItemEnumerable(dbit))
							if (newRow.LoginName == row.F.LoginName) {
								id = newRow.Id;
								break;
							}
					}
					else {
						ConnectionObject.SqlDBClient.CreateUserCredential(new AuthenticationCredentials((AuthenticationMethod)row.F.CredentialAuthenticationMethod,
							row.F.DBUserName,
							row.F.Password), row.F.LoginName, KB.I("MainBoss"));
						foreach (var newRow in GetItemEnumerable(dbit))
							if (newRow.DBUsername == row.F.DBUserName) {
								id = newRow.Id;
								break;
							}
					}
#if THISDOESNTWORK
					System.Diagnostics.Debug.Assert(id != null, "Didn't locate new principal_id after sql user creation");
					var dc = (DBIDataColumn)row.Table.Columns[dbit.InternalIdColumn.Name];
					bool ro = dc.ReadOnly;
					row.BeginEdit();
					dc.ReadOnly = false;
					dbit.InternalIdColumn[row] = id;
					dc.ReadOnly = ro;
					row.EndEdit();
					var ds = (DBDataSet)row.Table.DataSet;
					ds.PropogateMyChanges();
#endif
				}
				if (row.RowState == DataRowState.Deleted) {
					if (ConnectionObject.DatabaseLoginManager) {
						string loginName = (string)dsSecurityOnServer.Schema.T.SecurityOnServer.F.LoginName.EffectiveType.GenericAsNativeType(row[dsSecurityOnServer.Schema.T.SecurityOnServer.F.LoginName, DataRowVersion.Original], typeof(string));
						ConnectionObject.SqlDBClient.DeleteUserLogin(new AuthenticationCredentials(AuthenticationMethod.WindowsAuthentication, loginName));
					}
					else {
						string userName = (string)dsSecurityOnServer.Schema.T.SecurityOnServer.F.DBUserName.EffectiveType.GenericAsNativeType(row[dsSecurityOnServer.Schema.T.SecurityOnServer.F.DBUserName, DataRowVersion.Original], typeof(string));
						ConnectionObject.SqlDBClient.DeleteUserCredential(new AuthenticationCredentials(AuthenticationMethod.WindowsAuthentication, userName));
					}
				}
				++changedRowCount;
			}
			return changedRowCount;
		}
		protected override void BeginUpdate(SqlClient.SqlServer.SecurityOnServerInformation x) {
			throw new NotImplementedException();
		}

		protected override void CommitUpdate(SqlClient.SqlServer.SecurityOnServerInformation x) {
			throw new NotImplementedException();
		}
#endregion
#endregion
	}
}
