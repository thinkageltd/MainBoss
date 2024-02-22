using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	#region PermissionsUpgradeStep - Special Permission upgrade step for MainBoss
	/// <summary>
	/// Special Upgrade Step to change over permission handling from 'old' to 'new' method at MainBoss Version 3.2
	/// This upgrade step transitions the old Administrator versus basic rights user to the new roles based. All users that previously had
	/// Administrator rights get assigned all the roles associated with the defined AdminUser in the rightset
	/// All other users are assigned the roles associated with the UpgradeUser in the rightset
	/// </summary>
	public class PermissionUpgradeStep : DataUpgradeStep {
		public PermissionUpgradeStep() {
		}
		// TODO (Collation clash): This code creates temp tables which are in tempdb and thus default to a collation that may be different from our own
		// DB collation. Will this cause problems?
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			DBI_Database completedSchema = new DBI_Database();
			handler.CompleteDBIForSchemaOperations(completedSchema, schema);
			// First we have to ensure the Roles defined in the rightset exist in the database. There is a function that does this for Database Creation so
			// we use it here.
			var adminroles = new List<int>();
			var nonadminroles = new List<int>();
			for (int i = Security.RightSet.AdminUser.Length; --i >= 0;) {
				int roleid = (int)Security.RightSet.AdminUser[i];
				adminroles.Add(roleid);
			}
			for (int i = Security.RightSet.UpgradeUser.Length; --i >= 0;) {
				int roleid = (int)Security.RightSet.UpgradeUser[i];
				nonadminroles.Add(roleid);
			}
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(KB.I(@"
				CREATE TABLE dbo.##adminroles (
					nroleid uniqueidentifier,
					nprincipalid uniqueidentifier,
					code nvarchar(100)
				)
			")));
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(KB.I(@"
				CREATE TABLE dbo.##nonadminroles (
					nroleid uniqueidentifier,
					nprincipalid uniqueidentifier,
					code nvarchar(100)
				)
			")));
			foreach (int i in adminroles) {
				Guid roleId;
				roleId = KnownIds.RoleAndPrincipalIDFromRoleRight(i, out Guid principalId);
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("INSERT INTO ##adminroles (nroleid, nprincipalid, code) VALUES ('{0}', '{1}', '{2}')", roleId.ToString("D"), principalId.ToString("D"), i.ToString())));
			}
			foreach (int i in nonadminroles) {
				Guid roleId;
				roleId = KnownIds.RoleAndPrincipalIDFromRoleRight(i, out Guid principalId);
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("INSERT INTO ##nonadminroles (nroleid, nprincipalid, code) VALUES ('{0}', '{1}', '{2}')", roleId.ToString("D"), principalId.ToString("D"), i.ToString())));
			}
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(KB.I("INSERT INTO Principal (ID) SELECT nPrincipalId from ##adminroles union select nPrincipalID from ##nonadminroles")));
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(KB.I("INSERT INTO [Role](ID,PrincipalId,Code,Class) SELECT nRoleid, nPrincipalId, Code, 0 from ##adminroles union select nRoleid, nPrincipalId, Code, 0 from ##nonadminroles")));

			// Delete all UserRoles that are cross assigned to a user (i.e. user has both All and nonAdmin) to leave just All or nonAdmin assigned roles
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("delete [UserRole] where [RoleId] = '{0}' and [UserId] IN (SELECT UserId from [UserRole] where RoleId = '{1}')",
				KnownIds.RoleId_NonAdministrator.ToString("D"),
				KnownIds.RoleId_All.ToString("D"))));

			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("INSERT INTO UserRole (ID, RoleId, UserId) SELECT NEWID(), nRoleId, UserId from UserRole cross join ##adminroles where UserRole.RoleId = '{0}'",
				KnownIds.RoleId_All.ToString("D"))));
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("INSERT INTO UserRole (ID, RoleId, UserId) SELECT NEWID(), nRoleId, UserId from UserRole cross join ##nonadminroles where UserRole.RoleId = '{0}'",
				KnownIds.RoleId_NonAdministrator.ToString("D"))));

			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("DELETE UserRole where RoleId = '{0}' or RoleId = '{1}'",
				KnownIds.RoleId_NonAdministrator.ToString("D"),
				KnownIds.RoleId_All.ToString("D"))));
			// Get any permissions linked to our old roles
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("DELETE Permission where PrincipalId = '{0}' or PrincipalId = '{1}'",
				KnownIds.PrincipalId_NonAdministrator.ToString("D"),
				KnownIds.PrincipalId_All.ToString("D"))));
			// now delete the roles (base and derived records will go at same time due to cascade delete)
			session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("DELETE [Principal] where Id = '{0}' or Id = '{1}'",
				KnownIds.PrincipalId_NonAdministrator.ToString("D"),
				KnownIds.PrincipalId_All.ToString("D"))));
		}
	}
	#endregion
	#region XAFDatabaseFunctionUpgradeStep - add/modify/remove "built-in" XAF database functions
	public class XAFDatabaseFunctionUpgradeStep : DatabaseFunctionUpgradeStep {
		public XAFDatabaseFunctionUpgradeStep(Method method, [Invariant] string name)
			: base(method, name) {
		}
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			if (How == Method.Alter)
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(SqlClient.BuiltinDatabaseFunctions.Update(Name)));
			else if (How == Method.Create)
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(SqlClient.BuiltinDatabaseFunctions.Create(Name)));
			else if (How == Method.Drop)
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(SqlClient.BuiltinDatabaseFunctions.Delete(Name)));
		}
	}
	#endregion
	#region ChangeClosestValueOnAllDatesUpgradeStep
	public class ChangeClosestValueOnAllDatesUpgradeStep : DataUpgradeStep {
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			foreach (DBI_Table t in schema.Tables)
				if (t.SqlQueryText == null)
					foreach (DBI_Column c in t.Columns) {
						string closestValueConversion = SqlClient.SqlServer.DateTimeClosestValue(c.EffectiveType, SqlClient.SqlServer.SqlIdentifier(c));
						if (closestValueConversion != null)
							session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat("UPDATE {0} SET {1} = {2}", SqlClient.SqlServer.SqlIdentifier(t), SqlClient.SqlServer.SqlIdentifier(c), closestValueConversion)));
					}
		}
	}
	#endregion
	#region MakeDefaultTableDerivedLinkagesConsistentUpgradeStep
	public class MakeDefaultTableDerivedLinkagesConsistentUpgradeStep : DataUpgradeStep {
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			// Newly-created databases contain genuine stored derived-table linkages in their default tables.
			// older databases (from before derived-table linkages) have no such pointers. Because queries don't use them no one cares in general.
			// However, when trying to delete a derived table's default rows, they get in the way because they must be nulled out first and the foreign key constraint dropped.
			// Were we make sure everyone is on the same page by removing these to the default tables where they exist.
			// This is used in two places: Just before the place where it matters (dropping TemporaryStorageTemplate) and as a new upgrade step at the point the
			// problem was discovered (around 1.0.0.336).
			DBI_Database completed = new DBI_Database();
			handler.CompleteDBIForSchemaOperations(completed, schema);
			foreach (DBI_Table t in completed.Tables) {
				if (!t.HasDefaults)
					continue;
				// Find a base-table linkage if any.
				DBI_Table baseTable = t.VariantBaseTable;
				if (baseTable == null)
					continue;
				// We can't use MSSqlMethods.BuildSqlForColumnConstrainingForeignConstraintDelete because it call Execute which forces a GO command.
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat(@"
						if exists (select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = 'dbo' and TABLE_NAME = {0} and COLUMN_NAME = {1})
						begin
							declare @cmd nvarchar(max);
							select @cmd = 'ALTER TABLE {2} DROP CONSTRAINT ' 
									+ STUFF( ( select distinct ','+ccu.constraint_name
										from information_schema.constraint_column_usage as ccu	-- ccu for the column being dropped
										join information_schema.referential_constraints as rc	-- referencing foreign constraint
										on ccu.constraint_catalog = rc.constraint_catalog and ccu.constraint_schema = rc.constraint_schema	and ccu.constraint_name = rc.constraint_name
										where ccu.table_schema = 'dbo' and ccu.table_name = {0}	and ccu.column_name = {1}
										for xml path(''), TYPE).value('.','varchar(max)'), 1, 1, '')
							if @cmd is not null
							begin
							   exec sp_executesql @cmd;
							end
							ALTER TABLE {2} DROP COLUMN {3}
						end
					",
					Libraries.Sql.SqlUtilities.SqlLiteral(baseTable.Default.Name), Libraries.Sql.SqlUtilities.SqlLiteral(baseTable.VariantDerivedRecordIDColumns[t].Name),
					Libraries.Sql.SqlUtilities.SqlIdentifier(baseTable.Default.Name), Libraries.Sql.SqlUtilities.SqlIdentifier(baseTable.VariantDerivedRecordIDColumns[t].Name))));
			}
		}
	}
	#endregion
	#region RemoveDuplicateExternalTagsUpgradeStep
	/// <summary>
	/// The ServerUpgrade step attempts to put UNIQUE constraints on all tables that have a conditional constraint (with a where clause) configured.
	/// The tables RelativeLocation and PermanentItemLocation have ExternalTag columns that may cause a problem if a user has duplicate tags in those tables.
	/// We detected the rows that have duplicates, record them in DatabaseHistory then NULL them out to avoid duplicates when the Server Upgrade step is run.
	/// </summary>
	public class RemoveDuplicateExternalTagsUpgradeStep : DataUpgradeStep {
		public RemoveDuplicateExternalTagsUpgradeStep() {
		}

		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			DBI_Database completedSchema = new DBI_Database();
			handler.CompleteDBIForSchemaOperations(completedSchema, schema);
			DBClient db = new DBClient(new DBClient.Connection(session.ConnectionInformation, completedSchema), session);

			// determine if any RelativeLocation records have a duplicate ExternalTag
			System.Data.DataSet rlDuplicates = session.ExecuteCommandReturningTable(new MSSqlLiteralCommandSpecification(KB.I(@"
				select DISTINCT L.Code, ExternalTag from RelativeLocation as T
					join Location as L on L.Id = T.LocationID
					where ExternalTag in (select ExternalTag from RelativeLocation  group by ExternalTag having count(ExternalTag) > 1)
			")));
			// TODO: This should get the Item code too so the stroage assignment can be fully described to the user.
			System.Data.DataSet pilDuplicates = session.ExecuteCommandReturningTable(new MSSqlLiteralCommandSpecification(KB.I(@"
				 select DISTINCT L.Code, ExternalTag from PermanentItemLocation as T
					join ActualItemLocation as ail on ail.Id = T.ActualItemLocationID
					join ItemLocation as IL on IL.Id = AIL.ItemLocationID
					join Location as L on L.Id = IL.LocationID
				 where ExternalTag in (select ExternalTag from PermanentItemLocation  group by ExternalTag having count(ExternalTag) > 1)
			")));
			System.Text.StringBuilder removeRLduplicates = new System.Text.StringBuilder();
			System.Text.StringBuilder removePILduplicates = new System.Text.StringBuilder();
			// TODO: process the returned rows, and make a DatabaseHistory record that itemizes the rows whose columns we are about to nullify
			if (rlDuplicates.Tables[0].Rows.Count > 0)
				foreach (System.Data.DataRow r in rlDuplicates.Tables[0].Rows) {
					var locationCode = (string)dsMB.Schema.T.Location.F.Code.EffectiveType.GenericAsNativeType(r[0], typeof(string));
					var externalTag = (string)dsMB.Schema.T.RelativeLocation.F.ExternalTag.EffectiveType.GenericAsNativeType(r[1], typeof(string));
					removeRLduplicates.AppendLine(Strings.Format(KB.K("ExternalTag {0} cleared from Unit location {1}"), externalTag, locationCode));
				}
			if (pilDuplicates.Tables[0].Rows.Count > 0)
				foreach (System.Data.DataRow r in pilDuplicates.Tables[0].Rows) {
					var locationCode = (string)dsMB.Schema.T.Location.F.Code.EffectiveType.GenericAsNativeType(r[0], typeof(string));
					var externalTag = (string)dsMB.Schema.T.PermanentItemLocation.F.ExternalTag.EffectiveType.GenericAsNativeType(r[1], typeof(string));
					removePILduplicates.AppendLine(Strings.Format(KB.K("ExternalTag {0} cleared from Storeroom Assignment location {1}"), externalTag, locationCode));
				}
			// Remove the duplicates by NULLing out the column on those records
			if (removeRLduplicates.Length > 0) {
				handler.LogHistory(db, KB.K("ExternalTags cleared on Units and Locations").Translate(), removeRLduplicates.ToString());
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(KB.I(@"
					update RelativeLocation Set ExternalTag = NULL where ExternalTag in (select ExternalTag from RelativeLocation group by ExternalTag having count(ExternalTag) > 1)
				")));
			}
			if (removePILduplicates.Length > 0) {
				handler.LogHistory(db, KB.K("ExternalTags cleared on Storeroom Assignments").Translate(), removePILduplicates.ToString());
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(KB.I(@"
					update PermanentItemLocation Set ExternalTag = NULL where ExternalTag in (select ExternalTag from PermanentItemLocation group by ExternalTag having count(ExternalTag) > 1)
				")));
			}
		}
	}
	#endregion
	#region SqlUpgradeStep
	public class SqlUpgradeStep : DataUpgradeStep {
		public SqlUpgradeStep([Invariant] string sqlStatement) {
			SqlStatement = sqlStatement;
		}
		public override void Perform(Version startingVersion, ISession DB, DBI_Database schema, DBVersionHandler handler) {
			CommandBatchSpecification batch = new CommandBatchSpecification();
			batch.Commands.Add(new MSSqlLiteralCommandSpecification(SqlStatement));
			batch.CreateNormalParameter(KB.I("UserID"), Thinkage.Libraries.TypeInfo.IdTypeInfo.Universe).Value = handler.IdentifiedUser;
			DB.ExecuteCommandBatch(batch);
		}
		private string SqlStatement;
	}
	#endregion
	#region SqlMinApplicationVersionUpgradeStep
	public class SqlMinApplicationVersionUpgradeStep : MinApplicationVersionUpgradeStep {
		SqlUpgradeStep sqlStep;
		public SqlMinApplicationVersionUpgradeStep(DBI_Variable variable, Version version) : base(variable, version) {
			sqlStep = new SqlUpgradeStep(Strings.IFormat("exec dbo._vset{0} '{1}'", variable.Name, version));
		}

		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			sqlStep.Perform(startingVersion, session, schema, handler);
		}

		public override void Reverse(Version startingVersion, DBI_Database schema) {
			sqlStep.Reverse(startingVersion, schema);
		}
	}
	#endregion
	#region AddAddCommentStateTransitionRecordsUpgradeStep
	public class AddAddCommentStateTransitionRecordsUpgradeStep : DataUpgradeStep {
		public AddAddCommentStateTransitionRecordsUpgradeStep() {
		}
		private static Guid[] WOStates = { KnownIds.WorkOrderStateDraftId, KnownIds.WorkOrderStateOpenId, KnownIds.WorkOrderStateClosedId, KnownIds.WorkOrderStateVoidId };
		private static Guid[] POStates = { KnownIds.PurchaseOrderStateDraftId, KnownIds.PurchaseOrderStateIssuedId, KnownIds.PurchaseOrderStateClosedId, KnownIds.PurchaseOrderStateVoidId };
		private static Guid[] RequestStates = { KnownIds.RequestStateNewId, KnownIds.RequestStateInProgressId, KnownIds.RequestStateClosedId };
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler) {
			foreach (Guid stateId in WOStates)
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat(
					@"INSERT into WorkOrderStateTransition (Id, Operation, OperationHint, CanTransitionWithoutUI, CopyStatusFromPrevious, FromStateID, ToStateID, Rank, RightName)
						select newid(), 'Thinkage.MainBoss.Database§Add Work Order Comment', 'Thinkage.MainBoss.Database§Add a comment to this Work Order or change its Status without changing State',
								0, 1, {0}, {0}, 0, 'Table.WorkOrderStateHistory.Create'", Libraries.Sql.SqlUtilities.SqlLiteral(stateId.ToString()))));
			foreach (Guid stateId in POStates)
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat(
					@"INSERT into PurchaseOrderStateTransition (Id, Operation, OperationHint, CanTransitionWithoutUI, CopyStatusFromPrevious, FromStateID, ToStateID, Rank, RightName)
						select newid(), 'Thinkage.MainBoss.Database§Add Purchase Order Comment', 'Thinkage.MainBoss.Database§Add a comment to this Purchase Order or change its Status without changing State',
								0, 1, {0}, {0}, 0, 'Table.PurchaseOrderStateHistory.Create'", Libraries.Sql.SqlUtilities.SqlLiteral(stateId.ToString()))));
			foreach (Guid stateId in RequestStates)
				session.ExecuteCommand(new MSSqlLiteralCommandSpecification(Strings.IFormat(
					@"INSERT into RequestStateTransition (Id, Operation, OperationHint, CanTransitionWithoutUI, CopyStatusFromPrevious, FromStateID, ToStateID, Rank, RightName)
						select newid(), 'Thinkage.MainBoss.Database§New Requestor Comment', 'Thinkage.MainBoss.Database§Add a comment to this Request or change its Status without changing State',
								0, 1, {0}, {0}, 0, 'Table.RequestStateHistory.Create'", Libraries.Sql.SqlUtilities.SqlLiteral(stateId.ToString()))));
		}
	}
	#endregion
}