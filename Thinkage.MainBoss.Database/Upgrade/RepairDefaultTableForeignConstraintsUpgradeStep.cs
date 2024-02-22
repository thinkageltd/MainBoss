using System;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database {
	public class RepairDefaultTableForeignConstraintsUpgradeStep : DataUpgradeStep {
		/// <summary>
		/// In the past AddColumn and AddTable upgrade steps neglected to put any foreign constraints on the columns in the related Defaults table. This step endeavors to fix all existing Default tables
		/// and make 
		/// </summary>
		public RepairDefaultTableForeignConstraintsUpgradeStep() {
		}
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			DBI_Database completedSchema = new DBI_Database();
			handler.CompleteDBIForSchemaOperations(completedSchema, schema);
			foreach (DBI_Table t in completedSchema.Tables) {
				if (t.HasDefaults) {
					foreach (DBI_Column dc in t.Default.Columns) {
						if (dc.FConstrainedBy != null) {
							session.DeleteColumnConstrainingForeignConstraints(dc);
							session.CreateForeignConstraint(dc.FConstrainedBy);
						}
					}
				}
			}
		}
	}
}
