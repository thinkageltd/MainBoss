using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Thinkage.Libraries.DBILibrary;
using System.Linq;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MainBoss {
	public class DatabasesOnServerSession : EnumerableDrivenSession<DatabaseOnServerInformation, DatabaseOnServerInformation> {
		#region Connection
		public class Connection : IConnectionInformation {
			public Connection() {
			}

			#region IConnectionInformation Members
			public IServer CreateServer() {
				return new DatabasesOnServerServer();
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
			#endregion
		}
		#endregion
		#region Server
		public class DatabasesOnServerServer : EnumerableDrivenSession<DatabaseOnServerInformation, DatabaseOnServerInformation>.EnumerableDrivenServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {
				return new DatabasesOnServerSession((Connection)connectInfo, this);
			}
		}
		#endregion
		#region Constructor
		public DatabasesOnServerSession(IConnectionInformation connection, IServer server)
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
				return dsDatabasesOnServer.Schema;
			}
			set {
				throw new NotImplementedException();
			}
		}
		protected new Connection ConnectionObject { get { return (Connection)base.ConnectionObject; } }
		#endregion
		#region Overrides to support base class abstraction
		protected override DatabaseOnServerInformation PrepareItemForRead(DatabaseOnServerInformation item) {
			return item;
		}
		protected override DatabaseOnServerInformation PrepareItemForWrite(DatabaseOnServerInformation item) {
			return item;
		}
		#region - ServerIdentification
		public override string ServerIdentification {
			get { return KB.I("Thinkage SQL Server Enumerator"); }
		}
		#endregion
		#region - GetEvaluators (returns delegates that produce column values)
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			normalUpdater = delegate(DatabaseOnServerInformation e, object v)
			{
				throw new NotImplementedException();
			};
			if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.Id) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.Id;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return ((Libraries.TypeInfo.IntegralTypeInfo)dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.Id.EffectiveType).NativeMaxLimit(typeof(ulong));
				};
			}
			else if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.ServerName) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.ServerName;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.Database) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.DatabaseName;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return KB.I("Error");
				};
			}
			else if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.OrganizationName) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.OrganizationName;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return null;
				};
			}
			else if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.UserRecordExists) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.UserRecordExists;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return null;
				};
			}
			else if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.Version) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.Version;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return null;
				};
			}
			else if (sourceColumnSchema == dsDatabasesOnServer.Schema.T.DatabasesOnServer.F.AccessError) {
				normalEvaluator = delegate(DatabaseOnServerInformation e)
				{
					return e.AccessError;
				};
				exceptionEvaluator = delegate(System.Exception e) {
					return Libraries.Exception.FullMessage(e);
				};
			}
			else
				throw new Libraries.GeneralException(KB.K("Unknown source column in ColumnEvaluator"));
		}
		#endregion
		#region - GetItemEnumerable (returns an enumerable of the ItemT containing the driving data)
		// Paw through the filter expression looking for a filter on the ServerName. If there is one then return the value to compare with.
		// This is tricky because the expression in question is of the form (path = constant) or path is null; note that the column in question is not nullable so is null should always return false.
		// Looking for that particular ordering makes the code simpler, but makes things fragile (if the expression is later rearranged).
		private string FindServerFilter(SqlExpression filterExpr) {
			switch (filterExpr.Op) {
			case SqlExpression.OpName.Or:
				if (AlwaysFalse(filterExpr.Left))
					return FindServerFilter(filterExpr.Right);
				if (AlwaysFalse(filterExpr.Right))
					return FindServerFilter(filterExpr.Left);
				return null;

			case SqlExpression.OpName.And:
				var result = FindServerFilter(filterExpr.Left);
				if (result != null)
					return result;
				return FindServerFilter(filterExpr.Right);

			case SqlExpression.OpName.Equals:
				if (filterExpr.Left.Op == SqlExpression.OpName.Path
					&& filterExpr.Right.Op == SqlExpression.OpName.Constant
					&& filterExpr.Left.Path == dsDatabasesOnServer.Path.T.DatabasesOnServer.F.ServerName)
					return (string)filterExpr.Right.ConstantVal;
				if (filterExpr.Right.Op == SqlExpression.OpName.Path
					&& filterExpr.Left.Op == SqlExpression.OpName.Constant
					&& filterExpr.Right.Path == dsDatabasesOnServer.Path.T.DatabasesOnServer.F.ServerName)
					return (string)filterExpr.Left.ConstantVal;
				return null;

			default:
				return null;
			}
		}
		private bool AlwaysFalse(SqlExpression filterExpr) {
			switch (filterExpr.Op) {
			case SqlExpression.OpName.IsNull:
				if (filterExpr.Left.Op == SqlExpression.OpName.Path
					&& !filterExpr.Left.Path.ReferencedColumn.EffectiveType.AllowNull)
					return true;
				return false;
			default:
				return false;
			}
		}
		protected override IEnumerable<DatabaseOnServerInformation> GetItemEnumerable(DBI_Table dbit) {
			IEnumerable<string> serverEnumerator;
			var result = new List<DatabaseOnServerInformation>();
			string fixedServer = null;
			if (Specification.Filter != null)
				fixedServer = FindServerFilter(Specification.Filter);
			if (fixedServer != null)
				serverEnumerator = new string[] { fixedServer };
			else
				serverEnumerator = System.Data.Sql.SqlDataSourceEnumerator.Instance.GetDataSources().Rows.Cast<DataRow>().Select((row => row[1] is DBNull ? (string)row[0] : Libraries.Strings.IFormat("{0}\\{1}", row[0], row[1])));
			// transform the serverEnumerator into an enumerator of sql connection objects and create DatabaseOnServerInformation objects for them.
			serverEnumerator.AsParallel().WithDegreeOfParallelism(10).ForAll(
				delegate(string serverName) {
					try {
						SqlClient.SqlServer.ListDatabaseNames(serverName).AsParallel().WithDegreeOfParallelism(4).ForAll(
							delegate(SqlClient.Connection c) {
								var e = new DatabaseOnServerInformation(c);
								lock (result) {
									result.Add(e);
								}
							});
					}
					catch (System.Exception ex) {
						// TODO: Only if there is a WHERE clause asking about specific servers
						var e = new DatabaseOnServerInformation(serverName, ex);
						lock (result) {
							result.Add(e);
						}
					}
				});
			return result;
		}
		#endregion
		#endregion
	}
}

