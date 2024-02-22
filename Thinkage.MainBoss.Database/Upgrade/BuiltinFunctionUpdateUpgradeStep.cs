using System;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database {
	public class BuiltinFunctionUpdateUpgradeStep : UpgradeStep {
		// This step is used to update a builtin function like dbo._IAdd to another version that is 100% compatible with the old one.
		public BuiltinFunctionUpdateUpgradeStep([Invariant] string functionName) {
			FunctionName = functionName;
		}
		private readonly string FunctionName;
		public override void Reverse(Version startingVersion, DBI_Database schema) {
		}
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			session.ExecuteCommand(new DBSpecificCommandSpecification(Thinkage.Libraries.XAF.Database.Service.MSSql.Session.BuiltinDatabaseFunctions.Update(FunctionName)));
		}
	}
}