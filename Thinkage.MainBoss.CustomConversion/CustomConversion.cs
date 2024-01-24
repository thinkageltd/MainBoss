using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Xml;

namespace Thinkage.MainBoss.CustomConversion {
	/// <summary>
	/// This is the DBConverter that lets you run arbitrary SQL scripts with optional input database (IDB)
	/// </summary>
	public class RunScriptBasedOnDBConverter : DBConverter {
		private ManifestXmlResolver xmlResolver;
		private IServer ServerObject;
		protected SqlClient.Connection srcSqlConnection;
		private SqlClient.Connection BaseConnection;

		public RunScriptBasedOnDBConverter(SqlClient.Connection baseConnection, string src_dbname, string dest_dbname)
			: base(dest_dbname) {
			BaseConnection = baseConnection;
			ServerObject = baseConnection.CreateServer();
			if(!string.IsNullOrEmpty(src_dbname))
				srcSqlConnection = baseConnection.WithNewDBName(src_dbname);
			xmlResolver = new ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly());
		}
		public void CheckSrcDatabase() {
			if(srcSqlConnection != null) {
				var db = new SqlClient(srcSqlConnection, srcSqlConnection.CreateServer(), null);
				db.CloseDatabase();
			}
		}
		public void Convert([Libraries.Translation.Invariant] string scriptSrc) {
			SqlClient db = null;
			try {
				// We use the Source database as the operating context so we can create temporary procedures that don't pollute our New imported database if the src database exists
				db = (SqlClient)ServerObject.OpenSession(srcSqlConnection ?? BaseConnection, null);
				Convert(srcSqlConnection?.DBName, db, scriptSrc, xmlResolver);
			}
			finally {
				if (db != null)
					db.CloseDatabase();
			}
		}
	}
}
