using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.RDL2010;
using Thinkage.Libraries.Sql;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// Support for database creation scripts. These are scripts to be run at time
	/// of database creation.
	/// </summary>
	/// 
	public static class DatabaseCreation {
		// The following are globally defined for WebAccess to use to find transition rights by Key IdentifyingName
		public const string CloseWorkOrderAction = "Close WO";
		public const string OpenWorkOrderAction = "Open WO";
		public const string CloseRequestAction = "Close WR";
		public const string InProgressRequestAction = "In Progress";

		static public void ParseUserIdentification(string credential, out string userName, out string userRealm) {
			string[] splitUser = credential.ToLower().Split('\\');
			if (splitUser.Length == 2) {
				userName = splitUser[1];
				userRealm = splitUser[0];
			}
			else {
				splitUser = credential.ToLower().Split('@');
				if (splitUser.Length == 2) {
					userName = splitUser[0];
					userRealm = splitUser[1];
				}
				else {
					userName = credential.ToLower();
					userRealm = "";
				}
			}
		}
		public static string GetDatabaseSystemUser(DBClient db) {
			return (string)Thinkage.Libraries.TypeInfo.StringTypeInfo.AsNativeType(db.Session.ExecuteCommandReturningScalar(new Libraries.TypeInfo.StringTypeInfo(0, 128, 0, false, false, false), new Libraries.DBILibrary.MSSql.MSSqlLiteralCommandSpecification("SELECT SYSTEM_USER")), typeof(string));
		}
		public static void ManageDatabaseUserCredential(DBClient db, string authenticationCredential, bool delete = false) {
			Thinkage.Libraries.DBILibrary.MSSql.MSSqlLiteralCommandSpecification sqlCommand;
			if (delete)
				sqlCommand = new Thinkage.Libraries.DBILibrary.MSSql.MSSqlLiteralCommandSpecification(Strings.IFormat("exec [mbsp_DropDataBaseUser] {0}", SqlUtilities.SqlLiteral(authenticationCredential)));
			else
				sqlCommand = new Thinkage.Libraries.DBILibrary.MSSql.MSSqlLiteralCommandSpecification(Strings.IFormat("exec [mbsp_AddDataBaseUser] {0}", SqlUtilities.SqlLiteral(authenticationCredential)));
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			db.Session.ExecuteCommand(sqlCommand, output);
			if (output.Length > 0)
				throw new GeneralException(KB.T(output.ToString())).WithContext(new Thinkage.Libraries.MessageExceptionContext(KB.K("Authentication Credential '{0}'"), authenticationCredential));
		}
		/// <summary>
		/// Open each manifest resource listed and read all the statements contained within. Separate each
		/// manifest resource with a "GO" statement.
		/// </summary>
		/// <param name="resource_list">resource containing resource list, one per line.</param>
		/// <returns>All the statements to be executed.</returns>
		static private string BuildScriptFromResource(Stream resourcelist) {
			StreamReader sr = new StreamReader(resourcelist);
			string line;
			StringBuilder statements = new StringBuilder();
			statements.Append(Environment.NewLine);
			statements.Append(KB.I("GO"));
			statements.Append(Environment.NewLine);

			while ((line = sr.ReadLine()) != null) {
				if (line.TrimStart().StartsWith(KB.I("//")) || line.TrimStart().Length == 0)
					continue;
				string resourceName = KB.I("Thinkage.MainBoss.Database.Creation.") + line;
				StreamReader contents;
				try {
					contents = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));
				}
				catch (System.Exception e) {
					Thinkage.Libraries.Exception.AddContext(e, new Thinkage.Libraries.MessageExceptionContext(KB.K("Manifest resource {0}"), resourceName));
					throw;
				}
				string statement;
				while ((statement = contents.ReadLine()) != null) {
					if (!statement.TrimStart().StartsWith(KB.I("//"))) {
						statements.Append(statement);
						statements.Append(Environment.NewLine);
					}
				}
				statements.Append(KB.I("GO"));
				statements.Append(Environment.NewLine);
			}
			return statements.ToString();
		}
		static public string GetPostTablesCreationScript() {
			return BuildScriptFromResource(Assembly.GetExecutingAssembly().GetManifestResourceStream(KB.I("Thinkage.MainBoss.Database.Creation.PostTablesCreation.sql")));
		}
		public struct ServerSideDefinition {
			public ServerSideDefinition(DBI_Column column, [Invariant]string definitionText) {
				Column = column;
				DefinitionText = definitionText;
			}
			public readonly DBI_Column Column;
			public readonly string DefinitionText;
		};
		public static readonly ServerSideDefinition[] ServerSideDefinitions = new[] {
			new ServerSideDefinition(dsMB.Schema.T.ItemPrice.F.UnitCost, "as dbo.mbfn_CalculateUnitCost(Cost, Quantity, 1)"),
			new ServerSideDefinition(dsMB.Schema.T.ItemLocation.F.Code, "as dbo.mbfn_ItemLocation_Code(ID)"),
			new ServerSideDefinition(dsMB.Schema.T.ActualItemLocation.F.Available, "as (OnHand + OnOrder - OnReserve)"),
			new ServerSideDefinition(dsMB.Schema.T.ActualItemLocation.F.Shortage, "as case when EffectiveMinimum > (OnHand + OnOrder - OnReserve) then EffectiveMinimum - (OnHand + OnOrder - OnReserve) else 0 end"),
			new ServerSideDefinition(dsMB.Schema.T.ActualItemLocation.F.UnitCost, "as dbo.mbfn_CalculateUnitCost(TotalCost, OnHand, 1)"),
			new ServerSideDefinition(dsMB.Schema.T.Item.F.UnitCost, "as dbo.mbfn_CalculateUnitCost(TotalCost, OnHand, 1)"),
			new ServerSideDefinition(dsMB.Schema.T.SpecificationForm.F.EditAllowed, "as dbo.mbfn_SpecificationForm_EditAllowed(ID)"),
			new ServerSideDefinition(dsMB.Schema.T.SpecificationForm.F.DefaultReportLayout, "as dbo.mbfn_SpecificationForm_DefaultReportLayout(ID)"),
			new ServerSideDefinition(dsMB.Schema.T.ServiceLog.F.EntryVersion, "rowversion")
		};
		private static MB3Client.StateHistoryTransition[] RequestStateTransitions = new[] {
			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK(InProgressRequestAction),
				operationHint: DatabaseLayoutK("Change a request to the In Progress state."),
				fromStateID: KnownIds.RequestStateNewId,
				toStateID: KnownIds.RequestStateInProgressId,
				rank: 1,
				rightName: KB.I("Transition.Request.InProgress"),
				canTransitionWithoutUI: true,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK(CloseRequestAction),
				operationHint: DatabaseLayoutK("Change an In Progress request to the Closed state."),
				fromStateID: KnownIds.RequestStateInProgressId,
				toStateID: KnownIds.RequestStateClosedId,
				rank: 2,
				rightName: KB.I("Transition.Request.Close"),
				canTransitionWithoutUI: true,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("In Progress (With Comment)"),
				operationHint: DatabaseLayoutK("Change a request to the In Progress state and allow a comment."),
				fromStateID: KnownIds.RequestStateNewId,
				toStateID: KnownIds.RequestStateInProgressId,
				rank: 3,
				rightName: KB.I("Transition.Request.InProgress"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Close WR (With Comment)"),
				operationHint: DatabaseLayoutK("Change an In Progress request to the Closed state and allow a comment."),
				fromStateID: KnownIds.RequestStateInProgressId,
				toStateID: KnownIds.RequestStateClosedId,
				rank: 4,
				rightName: KB.I("Transition.Request.Close"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Reopen"),
				operationHint: DatabaseLayoutK("Change a Closed request to the In Progress state."),
				fromStateID: KnownIds.RequestStateClosedId,
				toStateID: KnownIds.RequestStateInProgressId,
				rank: 5,
				rightName: KB.I("Transition.Request.Reopen"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Void"),
				operationHint: DatabaseLayoutK("Change a request directly to Closed."),
				fromStateID: KnownIds.RequestStateNewId,
				toStateID: KnownIds.RequestStateClosedId,
				rank: 6,
				rightName: KB.I("Transition.Request.Void"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("New Requestor Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Request or change its Status without changing State"),
				fromStateID: KnownIds.RequestStateNewId,
				toStateID: KnownIds.RequestStateNewId,
				rank: 0,
				rightName: KB.I("Table.RequestStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("New Requestor Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Request or change its Status without changing State"),
				fromStateID: KnownIds.RequestStateInProgressId,
				toStateID: KnownIds.RequestStateInProgressId,
				rank: 0,
				rightName: KB.I("Table.RequestStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("New Requestor Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Request or change its Status without changing State"),
				fromStateID: KnownIds.RequestStateClosedId,
				toStateID: KnownIds.RequestStateClosedId,
				rank: 0,
				rightName: KB.I("Table.RequestStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),
		};
		private static MB3Client.StateHistoryTransition[] WorkOrderStateTransitions = new[] {
			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK(OpenWorkOrderAction),
				operationHint: DatabaseLayoutK("Change a Draft Work Order to Open"),
				fromStateID: KnownIds.WorkOrderStateDraftId,
				toStateID: KnownIds.WorkOrderStateOpenId,
				rank: 1,
				rightName: KB.I("Transition.WorkOrder.Open"),
				canTransitionWithoutUI: true,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK(CloseWorkOrderAction),
				operationHint: DatabaseLayoutK("Close a Work Order"),
				fromStateID: KnownIds.WorkOrderStateOpenId,
				toStateID: KnownIds.WorkOrderStateClosedId,
				rank: 2,
				rightName: KB.I("Transition.WorkOrder.Close"),
				canTransitionWithoutUI: true,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Open WO (With Comment)"),
				operationHint: DatabaseLayoutK("Change a Draft Work Order to Open and allow a comment"),
				fromStateID: KnownIds.WorkOrderStateDraftId,
				toStateID: KnownIds.WorkOrderStateOpenId,
				rank: 3,
				rightName: KB.I("Transition.WorkOrder.Open"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Close WO (With Comment)"),
				operationHint: DatabaseLayoutK("Close a Work Order and allow a comment"),
				fromStateID: KnownIds.WorkOrderStateOpenId,
				toStateID: KnownIds.WorkOrderStateClosedId,
				rank: 4,
				rightName: KB.I("Transition.WorkOrder.Close"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Suspend WO"),
				operationHint: DatabaseLayoutK("Change an Open Work Order back to Draft status"),
				fromStateID: KnownIds.WorkOrderStateOpenId,
				toStateID: KnownIds.WorkOrderStateDraftId,
				rank: 5,
				rightName: KB.I("Transition.WorkOrder.Suspend"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Reopen"),
				operationHint: DatabaseLayoutK("Change a Closed Work Order to Open"),
				fromStateID: KnownIds.WorkOrderStateClosedId,
				toStateID: KnownIds.WorkOrderStateOpenId,
				rank: 6,
				rightName: KB.I("Transition.WorkOrder.Reopen"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Void"),
				operationHint: DatabaseLayoutK("Void a Work Order"),
				fromStateID: KnownIds.WorkOrderStateDraftId,
				toStateID: KnownIds.WorkOrderStateVoidId,
				rank: 7,
				rightName: KB.I("Transition.WorkOrder.Void"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Draft WO"),
				operationHint: DatabaseLayoutK("Change a Voided Work Order back to Draft status"),
				fromStateID: KnownIds.WorkOrderStateVoidId,
				toStateID: KnownIds.WorkOrderStateDraftId,
				rank: 8,
				rightName: KB.I("Transition.WorkOrder.Draft"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Work Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Work Order or change its Status without changing State"),
				fromStateID: KnownIds.WorkOrderStateDraftId,
				toStateID: KnownIds.WorkOrderStateDraftId,
				rank: 0,
				rightName: KB.I("Table.WorkOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Work Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Work Order or change its Status without changing State"),
				fromStateID: KnownIds.WorkOrderStateOpenId,
				toStateID: KnownIds.WorkOrderStateOpenId,
				rank: 0,
				rightName: KB.I("Table.WorkOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Work Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Work Order or change its Status without changing State"),
				fromStateID: KnownIds.WorkOrderStateClosedId,
				toStateID: KnownIds.WorkOrderStateClosedId,
				rank: 0,
				rightName: KB.I("Table.WorkOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Work Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Work Order or change its Status without changing State"),
				fromStateID: KnownIds.WorkOrderStateVoidId,
				toStateID: KnownIds.WorkOrderStateVoidId,
				rank: 0,
				rightName: KB.I("Table.WorkOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),
		};
		private static MB3Client.StateHistoryTransition[] PurchaseOrderStateTransitions = new[] {
			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Issue"),
				operationHint: DatabaseLayoutK("Issue a Purchase Order"),
				fromStateID: KnownIds.PurchaseOrderStateDraftId,
				toStateID: KnownIds.PurchaseOrderStateIssuedId,
				rank: 1,
				rightName: KB.I("Transition.PurchaseOrder.Issue"),
				canTransitionWithoutUI: true,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Close PO"),
				operationHint: DatabaseLayoutK("Close a Purchase Order"),
				fromStateID: KnownIds.PurchaseOrderStateIssuedId,
				toStateID: KnownIds.PurchaseOrderStateClosedId,
				rank: 2,
				rightName: KB.I("Transition.PurchaseOrder.Close"),
				canTransitionWithoutUI: true,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Issue (With Comment)"),
				operationHint: DatabaseLayoutK("Issue a Purchase Order and allow a comment"),
				fromStateID: KnownIds.PurchaseOrderStateDraftId,
				toStateID: KnownIds.PurchaseOrderStateIssuedId,
				rank: 3,
				rightName: KB.I("Transition.PurchaseOrder.Issue"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Close PO (With Comment)"),
				operationHint: DatabaseLayoutK("Close a Purchase Order and allow a comment"),
				fromStateID: KnownIds.PurchaseOrderStateIssuedId,
				toStateID: KnownIds.PurchaseOrderStateClosedId,
				rank: 4,
				rightName: KB.I("Transition.PurchaseOrder.Close"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Withdraw PO"),
				operationHint: DatabaseLayoutK("Withdraw Issued Purchase Order back to Draft status"),
				fromStateID: KnownIds.PurchaseOrderStateIssuedId,
				toStateID: KnownIds.PurchaseOrderStateDraftId,
				rank: 5,
				rightName: KB.I("Transition.PurchaseOrder.Withdraw"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("ReActivate PO"),
				operationHint: DatabaseLayoutK("Change a Closed Purchase Order to Issued"),
				fromStateID: KnownIds.PurchaseOrderStateClosedId,
				toStateID: KnownIds.PurchaseOrderStateIssuedId,
				rank: 6,
				rightName: KB.I("Transition.PurchaseOrder.ReActivate"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Void"),
				operationHint: DatabaseLayoutK("Void a Purchase Order"),
				fromStateID: KnownIds.PurchaseOrderStateDraftId,
				toStateID: KnownIds.PurchaseOrderStateVoidId,
				rank: 7,
				rightName: KB.I("Transition.PurchaseOrder.Void"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Draft PO"),
				operationHint: DatabaseLayoutK("Change a Voided Purchase Order back to Draft status"),
				fromStateID: KnownIds.PurchaseOrderStateVoidId,
				toStateID: KnownIds.PurchaseOrderStateDraftId,
				rank: 8,
				rightName: KB.I("Transition.PurchaseOrder.Draft"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: false),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Purchase Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Purchase Order or change its Status without changing State"),
				fromStateID: KnownIds.PurchaseOrderStateDraftId,
				toStateID: KnownIds.PurchaseOrderStateDraftId,
				rank: 0,
				rightName: KB.I("Table.PurchaseOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Purchase Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Purchase Order or change its Status without changing State"),
				fromStateID: KnownIds.PurchaseOrderStateIssuedId,
				toStateID: KnownIds.PurchaseOrderStateIssuedId,
				rank: 0,
				rightName: KB.I("Table.PurchaseOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Purchase Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Purchase Order or change its Status without changing State"),
				fromStateID: KnownIds.PurchaseOrderStateClosedId,
				toStateID: KnownIds.PurchaseOrderStateClosedId,
				rank: 0,
				rightName: KB.I("Table.PurchaseOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

			new MB3Client.StateHistoryTransition(
				operation: DatabaseLayoutK("Add Purchase Order Comment"),
				operationHint: DatabaseLayoutK("Add a comment to this Purchase Order or change its Status without changing State"),
				fromStateID: KnownIds.PurchaseOrderStateVoidId,
				toStateID: KnownIds.PurchaseOrderStateVoidId,
				rank: 0,
				rightName: KB.I("Table.PurchaseOrderStateHistory.Create"),
				canTransitionWithoutUI: false,
				copyStatusFromPrevious: true),

		};

		/// <summary>
		/// Obtain a Key that resides in the database.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		static private Thinkage.Libraries.Translation.SimpleKey DatabaseLayoutK([Context(Level = 1)] string s) {
			return KB.K(s);
		}
		#region Minimal Datasets
		internal struct ServiceUserMessageKey {
			public string Key;
			public SimpleKey Comment;
			public ServiceUserMessageKey([Invariant] string k, SimpleKey comment) {
				Key = k;
				Comment = comment;
			}
		}
		// Because of visiblity problems we have to be passed the db version. The information originates in the
		// structure of the upgrade step table in the upgrader.
		internal static void SetMinimalData(MB3Client mb3db, [Invariant] string OrganizationName) {
			#region Provide defaults for trigger-calculated fields
			// Many of the trigger-calculated fields accumulate information on child records of some sort, and are updated by triggers on the child records.
			// THis means however, that no trigger comes into play when the parent record containing the field is created. As a result we put proper defaults
			// into the default records for these parent records.
			// Some trigger-calculated fields are not set here, reasons may be:
			// - null is the correct original value (CurrentXxxxID which refers to the 'current' record in a child table)
			// - the correct value is actually created by an insert trigger on the table or along derived tables on EACH of its derivation paths and the trigger is
			//		declared nullable to allow it to contain the transient null value before the trigger runs.
			using (dsMB ds = new dsMB(mb3db)) {
				ds.DisableUpdatePropagation();

				ds.DB.Edit(ds, dsMB.Schema.T.ActualItemLocation.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ActualItemLocation.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItemLocation.F.EffectiveMinimum, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItemLocation.F.EffectiveMaximum, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItemLocation.F.OnHand, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItemLocation.F.OnOrder, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItemLocation.F.OnReserve, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItemLocation.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.ItemAdjustment.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ItemAdjustment.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemAdjustment.F.TotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemAdjustment.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.ItemIssue.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ItemIssue.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemIssue.F.TotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemIssue.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.ItemTransfer.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ItemTransfer.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemTransfer.F.ToTotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemTransfer.F.ToTotalCost, 0m);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemTransfer.F.FromTotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ItemTransfer.F.FromTotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.ReceiveItemPO.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ReceiveItemPO.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ReceiveItemPO.F.TotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ReceiveItemPO.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.ReceiveItemNonPO.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ReceiveItemNonPO.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ReceiveItemNonPO.F.TotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ReceiveItemNonPO.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.ActualItem.Default, null);
				foreach (DataRow r in dsMB.Schema.T.ActualItem.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItem.F.TotalQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.ActualItem.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.Chargeback.Default, null);
				foreach (DataRow r in dsMB.Schema.T.Chargeback.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Chargeback.F.TotalCost, 0m);
				ds.DB.Edit(ds, dsMB.Schema.T.Demand.Default, null);
				foreach (DataRow r in dsMB.Schema.T.Demand.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Demand.F.ActualCost, 0m);
				ds.DB.Edit(ds, dsMB.Schema.T.DemandItem.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandItem.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandItem.F.ActualQuantity, 0);
				ds.DB.Edit(ds, dsMB.Schema.T.DemandLaborInside.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandLaborInside.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandLaborInside.F.ActualQuantity, TimeSpan.Zero);
				ds.DB.Edit(ds, dsMB.Schema.T.DemandLaborOutside.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandLaborOutside.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandLaborOutside.F.ActualQuantity, TimeSpan.Zero);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandLaborOutside.F.OrderQuantity, TimeSpan.Zero);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.DemandLaborOutsideTemplate.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandLaborOutsideTemplate.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandLaborOutsideTemplate.F.OrderQuantity, TimeSpan.Zero);
				ds.DB.Edit(ds, dsMB.Schema.T.DemandOtherWorkInside.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandOtherWorkInside.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandOtherWorkInside.F.ActualQuantity, 0);
				ds.DB.Edit(ds, dsMB.Schema.T.DemandOtherWorkOutside.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandOtherWorkOutside.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandOtherWorkOutside.F.ActualQuantity, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandOtherWorkOutside.F.OrderQuantity, 0);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.DemandOtherWorkOutsideTemplate.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandOtherWorkOutsideTemplate.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.DemandOtherWorkOutsideTemplate.F.OrderQuantity, 0);
				ds.DB.Edit(ds, dsMB.Schema.T.Item.Default, null);
				foreach (DataRow r in dsMB.Schema.T.Item.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Item.F.Available, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Item.F.OnHand, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Item.F.OnOrder, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Item.F.OnReserve, 0);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Item.F.TotalCost, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.POLine.Default, null);
				foreach (DataRow r in dsMB.Schema.T.POLine.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.POLine.F.ReceiveCost, 0m);
				ds.DB.Edit(ds, dsMB.Schema.T.POLineItem.Default, null);
				foreach (DataRow r in dsMB.Schema.T.POLineItem.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.POLineItem.F.ReceiveQuantity, 0);
				ds.DB.Edit(ds, dsMB.Schema.T.POLineLabor.Default, null);
				foreach (DataRow r in dsMB.Schema.T.POLineLabor.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.POLineLabor.F.ReceiveQuantity, TimeSpan.Zero);
				ds.DB.Edit(ds, dsMB.Schema.T.POLineMiscellaneous.Default, null);
				foreach (DataRow r in dsMB.Schema.T.POLineMiscellaneous.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.POLineMiscellaneous.F.ReceiveQuantity, 0);
				ds.DB.Edit(ds, dsMB.Schema.T.POLineOtherWork.Default, null);
				foreach (DataRow r in dsMB.Schema.T.POLineOtherWork.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.POLineOtherWork.F.ReceiveQuantity, 0);
				ds.DB.Edit(ds, dsMB.Schema.T.PurchaseOrder.Default, null);
				foreach (DataRow r in dsMB.Schema.T.PurchaseOrder.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.PurchaseOrder.F.HasReceiving, false);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.PurchaseOrder.F.TotalPurchase, 0m);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.PurchaseOrder.F.TotalReceive, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.Receipt.Default, null);
				foreach (DataRow r in dsMB.Schema.T.Receipt.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.Receipt.F.TotalReceive, 0m);
				ds.DB.Edit(ds, dsMB.Schema.T.WorkOrder.Default, null);
				foreach (DataRow r in dsMB.Schema.T.WorkOrder.Default.GetDataTable(ds).Rows) {
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.WorkOrder.F.TemporaryStorageEmpty, true);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.WorkOrder.F.TotalActual, 0m);
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.WorkOrder.F.TotalDemand, 0m);
				}
				ds.DB.Edit(ds, dsMB.Schema.T.WorkOrderTemplate.Default, null);
				foreach (DataRow r in dsMB.Schema.T.WorkOrderTemplate.Default.GetDataTable(ds).Rows)
					DBClient.SetReadOnlyColumn(r, dsMB.Schema.T.WorkOrderTemplate.F.DemandCount, 0);

				DataRow defaultMeterReadingRow;
				defaultMeterReadingRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.MeterReading.Default, null);
				DBClient.SetReadOnlyColumn(defaultMeterReadingRow, dsMB.Schema.T.MeterReading.F.EffectiveReading, 0);

				ds.DB.Update(ds, Thinkage.Libraries.DBILibrary.Server.UpdateOptions.InitialDefaults);
			}
			#endregion
			using (dsMB ds = new dsMB(mb3db)) {
				ds.DisableUpdatePropagation();
				foreach (DBI_Variable dv in dsMB.Schema.Variables)
					mb3db.ViewAdditionalVariables(ds, dv);
				#region General Variables

				ds.V.DBVersion.Value = MBUpgrader.UpgradeInformation.LatestDBVersion.ToString();
				ds.V.DBServerVersion.Value = mb3db.Session.EffectiveDBServerVersion.ToString();
				ds.V.ActiveFilterInterval.Value = new TimeSpan(500, 0, 0, 0); // default ActiveFilter interval is a year and a half
				ds.V.ActiveFilterSinceDate.SetNull();                               // and no Since date
				ds.V.MinMBAppVersion.Value = MBUpgrader.UpgradeInformation.LastestAppVersion(dsMB.Schema.V.MinMBAppVersion).ToString();
				ds.V.MinAReqAppVersion.Value = MBUpgrader.UpgradeInformation.LastestAppVersion(dsMB.Schema.V.MinAReqAppVersion).ToString();
				ds.V.MinMBRemoteAppVersion.Value = MBUpgrader.UpgradeInformation.LastestAppVersion(dsMB.Schema.V.MinMBRemoteAppVersion).ToString();
				// Demo logo variable
				Creation.Resources.Images.Culture = Thinkage.Libraries.Application.InstanceCultureInfo;
				System.Drawing.Bitmap logo = Creation.Resources.Images.DemoLogo;
				using (System.IO.MemoryStream ms = new MemoryStream()) {
					logo.Save(ms, logo.RawFormat);
					ds.V.CompanyLogo.Value = ms.ToArray();
				}
				// W/R, W/O, P/O variables
				ds.V.WOFormAdditionalBlankLines.Value = 6;
				ds.V.WOFormAdditionalInformation.Value = KB.K("Date/Time     __________________________ Repair Code __________________________\r\nMeter Reading __________________________ Down Time   __________________________\r\nComments\r\n\r\n\r\n\r\nSignature     __________________________\r\n\r\n=============================== For Office Use Only ===========================\r\nCharge Back   __________________________ Charge To   __________________________\r\nLabor         __________________________ Material    __________________________").Translate();
				ds.V.WOSequence.Value = 1;
				ds.V.WODefaultDuration.Value = new TimeSpan(1, 0, 0, 0);// 1 day default duration
				ds.V.WRSequence.Value = 1;
				ds.V.POSequence.Value = 1;
				ds.V.WOSequenceFormat.Value = KB.I("WO {0:D8}");
				ds.V.WRSequenceFormat.Value = KB.I("WR {0:D8}");
				ds.V.POSequenceFormat.Value = KB.I("PO {0:D8}");
				// POForm variables
				ds.V.POFormTitle.Value = KB.K("Purchase Order").Translate();
				ds.V.POFormAdditionalInformation.Value = KB.K("Authorized by ___________________________________").Translate();
				ds.V.POFormAdditionalBlankLines.Value = 6;

				ds.V.ManageRequestStates.Value = false;
				ds.V.CopyWSHCommentToRSH.Value = false;
				// MainBoss Service Configuration Variables
				// TODO: The following using translated text is WRONG; the acknowledgement should go back in the language of the requestor as defined by their
				// contact culture info definition....
				// Other configuration variables (non Service)
				#endregion
				#region MainBoss Service Default record
				DataRow defaultServiceConfiguration = ds.DB.EditSingleRow(ds, dsMB.Schema.T.ServiceConfiguration.Default, null);
				dsMB.Schema.T.ServiceConfiguration.F.WakeUpInterval[defaultServiceConfiguration] = new TimeSpan(0, 30, 0);
				dsMB.Schema.T.ServiceConfiguration.F.SMTPServer[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.SMTPPort[defaultServiceConfiguration] = 25;
				dsMB.Schema.T.ServiceConfiguration.F.SMTPUseSSL[defaultServiceConfiguration] = false;
				dsMB.Schema.T.ServiceConfiguration.F.SMTPCredentialType[defaultServiceConfiguration] = (sbyte)DatabaseEnums.SMTPCredentialType.ANONYMOUS;
				dsMB.Schema.T.ServiceConfiguration.F.SMTPUserDomain[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.SMTPUserName[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.SMTPEncryptedPassword[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.MailServer[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.MailServerType[defaultServiceConfiguration] = (sbyte)DatabaseEnums.MailServerType.POP3;
				dsMB.Schema.T.ServiceConfiguration.F.MailPort[defaultServiceConfiguration] = 110;   // POP3 Port
				dsMB.Schema.T.ServiceConfiguration.F.MailUserName[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.MailEncryptedPassword[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.MailboxName[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.AutomaticallyCreateRequestors[defaultServiceConfiguration] = true;
				dsMB.Schema.T.ServiceConfiguration.F.ReturnEmailAddress[defaultServiceConfiguration] = null;
				dsMB.Schema.T.ServiceConfiguration.F.ReturnEmailDisplayName[defaultServiceConfiguration] = KB.I("MainBoss Service");
				dsMB.Schema.T.ServiceConfiguration.F.HtmlEmailNotification[defaultServiceConfiguration] = true;
				dsMB.Schema.T.ServiceConfiguration.F.NotificationInterval[defaultServiceConfiguration] = new TimeSpan(0, 10, 0);
				dsMB.Schema.T.ServiceConfiguration.F.ProcessNotificationEmail[defaultServiceConfiguration] = true;
				dsMB.Schema.T.ServiceConfiguration.F.ProcessRequestorIncomingEmail[defaultServiceConfiguration] = true;

				#region MainBoss Service UserMessageKeys
				/// Define the MainBoss Service UserMessageTranslation keys (invariant ONLY provided)

				ds.EnsureDataTableExists(dsMB.Schema.T.UserMessageKey, dsMB.Schema.T.UserMessageTranslation);
				ServiceUserMessageKey[] predefinedServiceMessageKeys =
				{
					new ServiceUserMessageKey("RequestorNotificationSubjectPrefix",KB.K("RequestorNotificationSubjectPrefix_Comment")),
					new ServiceUserMessageKey("RequestorNotificationIntroduction",KB.K("RequestorNotificationIntroduction_Comment")),
					new ServiceUserMessageKey("ANRequestIntroduction",KB.K("ANRequestIntroduction_Comment")),
					new ServiceUserMessageKey("ANRequestSubjectPrefix",KB.K("ANRequestSubjectPrefix_Comment")),
					new ServiceUserMessageKey("ANWorkOrderIntroduction",KB.K("ANWorkOrderIntroduction_Comment")),
					new ServiceUserMessageKey("ANWorkOrderSubjectPrefix",KB.K("ANWorkOrderSubjectPrefix_Comment")),
					new ServiceUserMessageKey("ANPurchaseOrderIntroduction",KB.K("ANPurchaseOrderIntroduction_Comment")),
					new ServiceUserMessageKey("ANPurchaseOrderSubjectPrefix",KB.K("ANPurchaseOrderSubjectPrefix_Comment")),
					new ServiceUserMessageKey("EstimatedCompletionDate", KB.K("EstimatedCompletionDate_Comment")),
					new ServiceUserMessageKey("ReferenceWorkRequest", KB.K("ReferenceWorkRequest_Comment")),
					new ServiceUserMessageKey("RequestAddCommentPreamble", KB.K("RequestAddCommentPreamble_Comment")),
					new ServiceUserMessageKey("OriginalRequestPreamble", KB.K("OriginalRequestPreamble_Comment")),
					new ServiceUserMessageKey("RequestURLPreamble", KB.K("RequestURLPreamble_Comment")),
					new ServiceUserMessageKey("WorkOrderURLPreamble", KB.K("WorkOrderURLPreamble_Comment")),
#if MAINBOSSREMOTEHASPURCHASEORDERS
					new ServiceUserMessageKey("PurchaseOrderURLPreamble", KB.K("PurchaseOrderURLPreamble_Comment")),
#endif
					new ServiceUserMessageKey("RequestDeniedPreamble", KB.K("RequestDeniedPreamble_Comment")),
					new ServiceUserMessageKey("RequestorInitialCommentPreamble", KB.K("RequestorInitialCommentPreamble_Comment")),
					new ServiceUserMessageKey("RequestorStatusPending", KB.K("RequestorStatusPendingPending_Comment"))
				};
				foreach (ServiceUserMessageKey smk in predefinedServiceMessageKeys) {
					dsMB.UserMessageKeyRow serviceMessageKey = ds.T.UserMessageKey.AddNewUserMessageKeyRow();
					serviceMessageKey.F.Context = Thinkage.MainBoss.Database.Service.MainBossServiceConfiguration.MainBossServiceTag; // must match context used in Thinkage.MainBoss.ServiceWorker.Translation.UK
					serviceMessageKey.F.Key = smk.Key;
					serviceMessageKey.F.Comment = smk.Comment;
				}
				// Now add the two special translations for 'RequestClosePreference' trigger code
				Guid CommentToRequestorWhenNewRequestedWorkOrderID, CommentToRequestorWhenWorkOrderClosesID;
				dsMB.UserMessageKeyRow RequestClosePreference = ds.T.UserMessageKey.AddNewUserMessageKeyRow();
				CommentToRequestorWhenNewRequestedWorkOrderID = RequestClosePreference.F.Id;
				RequestClosePreference.F.Context = KB.I(RequestClosePreferenceK.RequestClosePreference);
				RequestClosePreference.F.Key = RequestClosePreferenceK.K("CommentToRequestorWhenNewRequestedWorkOrder").IdentifyingName; // need the KB.K for translation harvestor to find the Key for translation.
				RequestClosePreference.F.Comment = KB.K("CommentToRequestorWhenNewRequestedWorkOrder_Comment");
				RequestClosePreference = ds.T.UserMessageKey.AddNewUserMessageKeyRow();
				CommentToRequestorWhenWorkOrderClosesID = RequestClosePreference.F.Id;
				RequestClosePreference.F.Context = KB.I(RequestClosePreferenceK.RequestClosePreference);
				RequestClosePreference.F.Key = RequestClosePreferenceK.K("CommentToRequestorWhenWorkOrderCloses").IdentifyingName;
				RequestClosePreference.F.Comment = KB.K("CommentToRequestorWhenWorkOrderCloses_Comment");

				#endregion
				#endregion

				#region DatabaseHistory entry
				DataRow databaseHistoryRow = ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.DatabaseHistory);
				dsMB.Schema.T.DatabaseHistory.F.Subject[databaseHistoryRow] = Strings.Format(KB.K("Database version {0} created"), MBUpgrader.UpgradeInformation.LatestDBVersion.ToString());
				System.Text.StringBuilder description = new StringBuilder();
				description.AppendLine(Strings.Format(KB.K("SQL Version {0}"), ds.DB.DatabaseServerProductIdentification));
				description.AppendLine(Strings.Format(KB.K("Culture {0} ({1})"), Thinkage.Libraries.Application.InstanceCultureInfo.NativeName, Thinkage.Libraries.Application.InstanceCultureInfo.Name));
				Application.IUserInformation uInfo = Application.Instance.QueryInterface<Application.IUserInformation>();
				if (uInfo != null) {
					description.AppendLine(Strings.Format(KB.K("By user {0}"), ds.DB.Session.ConnectionInformation.UserIdentification));
					description.AppendLine(Strings.Format(KB.K("On machine {0}"), uInfo.WorkstationName));
				}
				dsMB.Schema.T.DatabaseHistory.F.Description[databaseHistoryRow] = description.ToString();
				#endregion

				#region General Defaults
				// In general the Defaults are left null. Some defaults are set to allow the "hidden accounting" to work.
				// We also have the following specific changes.
				// Set the Meter default offset to zero. This is a required field in the Meter record and will almost
				// always be zero anyway. Perhaps this should be handled as a general case: any numeric field that is
				// required should have zero in the its default value ...????
				DataRow defaultMeterRow;
				defaultMeterRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.Meter.Default, null);
				dsMB.Schema.T.Meter.F.MeterReadingOffset[defaultMeterRow] = 0;

				ds.DB.Edit(ds, dsMB.Schema.T.Requestor.Default, null);
				foreach (DataRow r in dsMB.Schema.T.Requestor.Default.GetDataTable(ds).Rows) {
					dsMB.Schema.T.Requestor.F.ReceiveAcknowledgement[r] = true;
				}

				//Assignment Notification defaults are all on.
				DataRow rarow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.RequestAssignee.Default, null);
				dsMB.Schema.T.RequestAssignee.F.ReceiveNotification[rarow] = true;
				DataRow worow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.WorkOrderAssignee.Default, null);
				dsMB.Schema.T.WorkOrderAssignee.F.ReceiveNotification[worow] = true;
				DataRow porow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.PurchaseOrderAssignee.Default, null);
				dsMB.Schema.T.PurchaseOrderAssignee.F.ReceiveNotification[porow] = true;

				DataRow defaultServiceContractRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.ServiceContract.Default, null);
				dsMB.Schema.T.ServiceContract.F.Parts[defaultServiceContractRow] = true;
				dsMB.Schema.T.ServiceContract.F.Labor[defaultServiceContractRow] = true;

				#region SelectPrintFlag
				// The interim SelectPrintFlag on Purchase Orders, and Requests defaults to True for New records. WorkOrder will be set with other workorder default values below
				DataRow defaultSelectPrintRow;
				defaultSelectPrintRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.Request.Default, null);
				dsMB.Schema.T.Request.F.SelectPrintFlag[defaultSelectPrintRow] = true;
				defaultSelectPrintRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.PurchaseOrder.Default, null);
				dsMB.Schema.T.PurchaseOrder.F.SelectPrintFlag[defaultSelectPrintRow] = true;
				#endregion

				#endregion

				#region Users and permissions

				// User
				string userName;
				string userRealm;
				string system_user = DatabaseCreation.GetDatabaseSystemUser(ds.DB);
				DatabaseCreation.ParseUserIdentification(system_user, out userName, out userRealm);

				ds.EnsureDataTableExists(dsMB.Schema.T.User, dsMB.Schema.T.Contact, dsMB.Schema.T.UserRole);

				// We need a Contact record to associate with the Administration User
				dsMB.ContactRow contactRow = ds.T.Contact.AddNewContactRow();
				contactRow.F.Code = userName;
				contactRow.F.Comment = KB.K("This contact is for the user who created this organization").Translate();

				dsMB.UserRow databaseCreatorUserRow = ds.T.User.AddNewUserRow();
				dsMB.PrincipalRow databaseCreatorPrincipalRow = databaseCreatorUserRow.PrincipalIDParentRow;

				databaseCreatorUserRow.F.Desc = KB.K("This account is for the user who created this organization").Translate();
				databaseCreatorUserRow.F.Hidden = null;
				databaseCreatorUserRow.F.ContactID = contactRow.F.Id;

				databaseCreatorUserRow.F.AuthenticationCredential = system_user;
				// Establish the user creating the database has the Administrator and All Rights role as defined in the CreateSecurityDataSet
				SecurityCreation.CreateSecurityDataSet(ds, databaseCreatorUserRow.F.Id, SecurityCreation.RightSetLocation);
				#endregion

				// For MainBoss 3.0 we have predefined RequestStates, WorkOrderStates, PurchaseOrderStates.

				#region RequestState

				dsMB.RequestStateRow requeststaterow;
				ds.EnsureDataTableExists(dsMB.Schema.T.RequestState);

				requeststaterow = ds.T.RequestState.AddNewRequestStateRow();
				DBClient.SetReadOnlyColumn(requeststaterow, dsMB.Schema.T.RequestState.F.Id, KnownIds.RequestStateNewId);
				requeststaterow.F.Code = StateContext.NewCode;
				requeststaterow.F.Desc = StateContext.DescK("New Request");
				requeststaterow.F.Hidden = null;
				requeststaterow.F.FilterAsNew = true;
				requeststaterow.F.FilterAsClosed = false;
				requeststaterow.F.FilterAsInProgress = false;

				requeststaterow = ds.T.RequestState.AddNewRequestStateRow();
				DBClient.SetReadOnlyColumn(requeststaterow, dsMB.Schema.T.RequestState.F.Id, KnownIds.RequestStateInProgressId);
				requeststaterow.F.Code = StateContext.InProgressCode;
				requeststaterow.F.Desc = StateContext.DescK("Request work in progress");
				requeststaterow.F.Hidden = null;
				requeststaterow.F.FilterAsNew = false;
				requeststaterow.F.FilterAsClosed = false;
				requeststaterow.F.FilterAsInProgress = true;

				requeststaterow = ds.T.RequestState.AddNewRequestStateRow();
				DBClient.SetReadOnlyColumn(requeststaterow, dsMB.Schema.T.RequestState.F.Id, KnownIds.RequestStateClosedId);
				requeststaterow.F.Code = StateContext.ClosedCode;
				requeststaterow.F.Desc = StateContext.DescK("Request has been completed");
				requeststaterow.F.Hidden = null;
				requeststaterow.F.FilterAsNew = false;
				requeststaterow.F.FilterAsClosed = true;
				requeststaterow.F.FilterAsInProgress = false;

				// Set the StateTransition table for Requests
				// TODO: This should be table-driven to make the code more compact to read.
				ds.EnsureDataTableExists(dsMB.Schema.T.RequestStateTransition);
				foreach (MB3Client.StateHistoryTransition t in RequestStateTransitions)
					MB3Client.RequestHistoryTable.Value.StateHistoryTransitionToRow(t, ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.RequestStateTransition));

				// Set the Default RequestStateHistory initial state for Requests to 'New'
				DataRow defaultRequestStateHistoryRow;
				defaultRequestStateHistoryRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.RequestStateHistory.Default, null);
				dsMB.Schema.T.RequestStateHistory.F.RequestStateID[defaultRequestStateHistoryRow] = KnownIds.RequestStateNewId;
				dsMB.Schema.T.RequestStateHistory.F.EffectiveDateReadonly[defaultRequestStateHistoryRow] = false;


				#endregion
				#region WorkOrderState

				// WorkOrderState
				dsMB.WorkOrderStateRow workorderstaterow;
				ds.EnsureDataTableExists(dsMB.Schema.T.WorkOrderState);
				Guid WOVoidStateID, WOOpenStateID, WOClosedStateID, WODraftStateID;

				workorderstaterow = ds.T.WorkOrderState.AddNewWorkOrderStateRow();
				WODraftStateID = KnownIds.WorkOrderStateDraftId;
				DBClient.SetReadOnlyColumn(workorderstaterow, dsMB.Schema.T.WorkOrderState.F.Id, WODraftStateID);
				workorderstaterow.F.Code = StateContext.DraftCode;
				workorderstaterow.F.Desc = StateContext.DescK("Work Order being drafted");
				workorderstaterow.F.Hidden = null;
				workorderstaterow.F.AffectsFuturePMGeneration = false;
				workorderstaterow.F.CanModifyChargebacks = true;
				workorderstaterow.F.CanModifyChargebackLines = false;
				workorderstaterow.F.CanModifyDemands = true;
				workorderstaterow.F.CanModifyDefinitionFields = true;
				workorderstaterow.F.CanModifyActuals = false;
				workorderstaterow.F.CanModifyPOLines = true;
				workorderstaterow.F.CanModifyOperationalFields = true;
				workorderstaterow.F.DemandCountsActive = true;
				workorderstaterow.F.TemporaryStorageActive = true;
				workorderstaterow.F.FilterAsOpen = false;
				workorderstaterow.F.FilterAsClosed = false;
				workorderstaterow.F.FilterAsVoid = false;
				workorderstaterow.F.FilterAsDraft = true;



				workorderstaterow = ds.T.WorkOrderState.AddNewWorkOrderStateRow();
				WOOpenStateID = KnownIds.WorkOrderStateOpenId;
				DBClient.SetReadOnlyColumn(workorderstaterow, dsMB.Schema.T.WorkOrderState.F.Id, WOOpenStateID);

				workorderstaterow.F.Code = StateContext.OpenCode;
				workorderstaterow.F.Desc = StateContext.DescK("Work Order waiting");
				workorderstaterow.F.Hidden = null;
				workorderstaterow.F.AffectsFuturePMGeneration = true;
				workorderstaterow.F.CanModifyChargebacks = true;
				workorderstaterow.F.CanModifyChargebackLines = true;
				workorderstaterow.F.CanModifyDemands = true;
				workorderstaterow.F.CanModifyDefinitionFields = false;
				workorderstaterow.F.CanModifyActuals = true;
				workorderstaterow.F.CanModifyPOLines = true;
				workorderstaterow.F.CanModifyOperationalFields = true;
				workorderstaterow.F.DemandCountsActive = true;
				workorderstaterow.F.TemporaryStorageActive = true;
				workorderstaterow.F.FilterAsOpen = true;
				workorderstaterow.F.FilterAsClosed = false;
				workorderstaterow.F.FilterAsVoid = false;
				workorderstaterow.F.FilterAsDraft = false;


				workorderstaterow = ds.T.WorkOrderState.AddNewWorkOrderStateRow();
				WOClosedStateID = KnownIds.WorkOrderStateClosedId;
				DBClient.SetReadOnlyColumn(workorderstaterow, dsMB.Schema.T.WorkOrderState.F.Id, WOClosedStateID);
				workorderstaterow.F.Code = StateContext.ClosedCode;
				workorderstaterow.F.Desc = StateContext.DescK("Work Order completed");
				workorderstaterow.F.Hidden = null;
				workorderstaterow.F.AffectsFuturePMGeneration = true;
				workorderstaterow.F.CanModifyChargebacks = false;
				workorderstaterow.F.CanModifyChargebackLines = false;
				workorderstaterow.F.CanModifyDemands = false;
				workorderstaterow.F.CanModifyDefinitionFields = false;
				workorderstaterow.F.CanModifyActuals = false;
				workorderstaterow.F.CanModifyPOLines = false;
				workorderstaterow.F.CanModifyOperationalFields = false;
				workorderstaterow.F.DemandCountsActive = false;
				workorderstaterow.F.TemporaryStorageActive = false;
				workorderstaterow.F.FilterAsOpen = false;
				workorderstaterow.F.FilterAsClosed = true;
				workorderstaterow.F.FilterAsVoid = false;
				workorderstaterow.F.FilterAsDraft = false;

				workorderstaterow = ds.T.WorkOrderState.AddNewWorkOrderStateRow();
				WOVoidStateID = KnownIds.WorkOrderStateVoidId;
				DBClient.SetReadOnlyColumn(workorderstaterow, dsMB.Schema.T.WorkOrderState.F.Id, WOVoidStateID);
				workorderstaterow.F.Hidden = null;
				workorderstaterow.F.Code = StateContext.VoidedCode;
				workorderstaterow.F.Desc = StateContext.DescK("Work Order voided");
				workorderstaterow.F.AffectsFuturePMGeneration = false;
				workorderstaterow.F.CanModifyChargebacks = false;
				workorderstaterow.F.CanModifyChargebackLines = false;
				workorderstaterow.F.CanModifyDemands = false;
				workorderstaterow.F.CanModifyDefinitionFields = false;
				workorderstaterow.F.CanModifyActuals = false;
				workorderstaterow.F.CanModifyPOLines = false;
				workorderstaterow.F.CanModifyOperationalFields = false;
				workorderstaterow.F.DemandCountsActive = false;
				workorderstaterow.F.TemporaryStorageActive = false;
				workorderstaterow.F.FilterAsOpen = false;
				workorderstaterow.F.FilterAsClosed = false;
				workorderstaterow.F.FilterAsVoid = true;
				workorderstaterow.F.FilterAsDraft = false;

				// Set the StateTransition table for WorkOrders
				ds.EnsureDataTableExists(dsMB.Schema.T.WorkOrderStateTransition);
				foreach (MB3Client.StateHistoryTransition t in WorkOrderStateTransitions)
					MB3Client.WorkOrderHistoryTable.Value.StateHistoryTransitionToRow(t, ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.WorkOrderStateTransition));

				// Set the Default WorkOrderStateHistory initial state for WorkOrders to 'Draft'
				DataRow defaultWorkOrderStateHistoryRow;
				defaultWorkOrderStateHistoryRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.WorkOrderStateHistory.Default, null);
				dsMB.Schema.T.WorkOrderStateHistory.F.WorkOrderStateID[defaultWorkOrderStateHistoryRow] = WODraftStateID;
				dsMB.Schema.T.WorkOrderStateHistory.F.EffectiveDateReadonly[defaultWorkOrderStateHistoryRow] = false;

				#endregion
				#region ManageRequestTransition
				// depends on values created in RequestState and WorkOrderState
				// and was set up according to this definition
				//--Request       WO			 Change
				//--State         State          in Request State (create RSH records)

				//--New           Draft          Open
				//--New           Open           Open
				//--New           Closed         Open, then Close (2 records)
				//--New           Void           Close

				//--InProgress    Draft          None
				//--InProgress    Open           None
				//--InProgress    Closed         Close
				//--InProgress    Void           Close

				//--Closed        Any            None

				dsMB.ManageRequestTransitionRow requestTransitionRow;
				ds.EnsureDataTableExists(dsMB.Schema.T.ManageRequestTransition);

				requestTransitionRow = ds.T.ManageRequestTransition.AddNewManageRequestTransitionRow();
				requestTransitionRow.F.RequestStateID = KnownIds.RequestStateNewId;
				requestTransitionRow.F.WorkOrderStateID = WODraftStateID;
				requestTransitionRow.F.ChangeToRequestStateID = KnownIds.RequestStateInProgressId;
				requestTransitionRow.F.CommentToRequestorUserMessageKeyID = CommentToRequestorWhenNewRequestedWorkOrderID;

				requestTransitionRow = ds.T.ManageRequestTransition.AddNewManageRequestTransitionRow();
				requestTransitionRow.F.RequestStateID = KnownIds.RequestStateNewId;
				requestTransitionRow.F.WorkOrderStateID = WOOpenStateID;
				requestTransitionRow.F.ChangeToRequestStateID = KnownIds.RequestStateInProgressId;
				requestTransitionRow.F.CommentToRequestorUserMessageKeyID = CommentToRequestorWhenNewRequestedWorkOrderID;

				requestTransitionRow = ds.T.ManageRequestTransition.AddNewManageRequestTransitionRow();
				requestTransitionRow.F.RequestStateID = KnownIds.RequestStateNewId;
				requestTransitionRow.F.WorkOrderStateID = WOClosedStateID;
				requestTransitionRow.F.ChangeToRequestStateID = KnownIds.RequestStateInProgressId; // trigger will process this first, then find the one that changes to Closed second
				requestTransitionRow.F.CommentToRequestorUserMessageKeyID = CommentToRequestorWhenNewRequestedWorkOrderID;

				requestTransitionRow = ds.T.ManageRequestTransition.AddNewManageRequestTransitionRow();
				requestTransitionRow.F.RequestStateID = KnownIds.RequestStateNewId;
				requestTransitionRow.F.WorkOrderStateID = WOVoidStateID;
				requestTransitionRow.F.ChangeToRequestStateID = KnownIds.RequestStateClosedId;
				requestTransitionRow.F.CommentToRequestorUserMessageKeyID = CommentToRequestorWhenWorkOrderClosesID;

				requestTransitionRow = ds.T.ManageRequestTransition.AddNewManageRequestTransitionRow();
				requestTransitionRow.F.RequestStateID = KnownIds.RequestStateInProgressId;
				requestTransitionRow.F.WorkOrderStateID = WOClosedStateID;
				requestTransitionRow.F.ChangeToRequestStateID = KnownIds.RequestStateClosedId;
				requestTransitionRow.F.CommentToRequestorUserMessageKeyID = CommentToRequestorWhenWorkOrderClosesID;

				requestTransitionRow = ds.T.ManageRequestTransition.AddNewManageRequestTransitionRow();
				requestTransitionRow.F.RequestStateID = KnownIds.RequestStateInProgressId;
				requestTransitionRow.F.WorkOrderStateID = WOVoidStateID;
				requestTransitionRow.F.ChangeToRequestStateID = KnownIds.RequestStateClosedId;
				requestTransitionRow.F.CommentToRequestorUserMessageKeyID = CommentToRequestorWhenWorkOrderClosesID;
				#endregion
				#region PurchaseOrderState

				// PurchaseOrderState
				dsMB.PurchaseOrderStateRow purchaseorderstaterow;
				ds.EnsureDataTableExists(dsMB.Schema.T.PurchaseOrderState);
				Guid PODraftStateID, POIssuedStateID, POClosedStateID, POVoidStateID;

				purchaseorderstaterow = ds.T.PurchaseOrderState.AddNewPurchaseOrderStateRow();
				PODraftStateID = KnownIds.PurchaseOrderStateDraftId;
				DBClient.SetReadOnlyColumn(purchaseorderstaterow, dsMB.Schema.T.PurchaseOrderState.F.Id, PODraftStateID);
				purchaseorderstaterow.F.Hidden = null;
				purchaseorderstaterow.F.Code = StateContext.DraftCode;
				purchaseorderstaterow.F.Desc = StateContext.DescK("Purchase Order being drafted");
				purchaseorderstaterow.F.OrderCountsActive = true;
				purchaseorderstaterow.F.FilterAsDraft = true;
				purchaseorderstaterow.F.FilterAsIssued = false;
				purchaseorderstaterow.F.FilterAsClosed = false;
				purchaseorderstaterow.F.FilterAsVoid = false;
				purchaseorderstaterow.F.CanHaveReceiving = true;
				purchaseorderstaterow.F.CanModifyOrder = true;
				purchaseorderstaterow.F.CanModifyReceiving = false;

				purchaseorderstaterow = ds.T.PurchaseOrderState.AddNewPurchaseOrderStateRow();
				POIssuedStateID = KnownIds.PurchaseOrderStateIssuedId;
				DBClient.SetReadOnlyColumn(purchaseorderstaterow, dsMB.Schema.T.PurchaseOrderState.F.Id, POIssuedStateID);
				purchaseorderstaterow.F.Hidden = null;
				purchaseorderstaterow.F.Code = StateContext.IssuedCode;
				purchaseorderstaterow.F.Desc = StateContext.DescK("Purchase Order issued to vendor");
				purchaseorderstaterow.F.OrderCountsActive = true;
				purchaseorderstaterow.F.FilterAsDraft = false;
				purchaseorderstaterow.F.FilterAsIssued = true;
				purchaseorderstaterow.F.FilterAsClosed = false;
				purchaseorderstaterow.F.FilterAsVoid = false;
				purchaseorderstaterow.F.CanHaveReceiving = true;
				purchaseorderstaterow.F.CanModifyOrder = false;
				purchaseorderstaterow.F.CanModifyReceiving = true;

				purchaseorderstaterow = ds.T.PurchaseOrderState.AddNewPurchaseOrderStateRow();
				POClosedStateID = KnownIds.PurchaseOrderStateClosedId;
				DBClient.SetReadOnlyColumn(purchaseorderstaterow, dsMB.Schema.T.PurchaseOrderState.F.Id, POClosedStateID);
				purchaseorderstaterow.F.Hidden = null;
				purchaseorderstaterow.F.Code = StateContext.ClosedCode;
				purchaseorderstaterow.F.Desc = StateContext.DescK("Purchase Order completed");
				purchaseorderstaterow.F.OrderCountsActive = false;
				purchaseorderstaterow.F.FilterAsDraft = false;
				purchaseorderstaterow.F.FilterAsIssued = false;
				purchaseorderstaterow.F.FilterAsClosed = true;
				purchaseorderstaterow.F.FilterAsVoid = false;
				purchaseorderstaterow.F.CanHaveReceiving = true;
				purchaseorderstaterow.F.CanModifyOrder = false;
				purchaseorderstaterow.F.CanModifyReceiving = false;

				purchaseorderstaterow = ds.T.PurchaseOrderState.AddNewPurchaseOrderStateRow();
				POVoidStateID = KnownIds.PurchaseOrderStateVoidId;
				DBClient.SetReadOnlyColumn(purchaseorderstaterow, dsMB.Schema.T.PurchaseOrderState.F.Id, POVoidStateID);
				purchaseorderstaterow.F.Hidden = null;
				purchaseorderstaterow.F.Code = StateContext.VoidedCode;
				purchaseorderstaterow.F.Desc = StateContext.DescK("Purchase Order voided");
				purchaseorderstaterow.F.OrderCountsActive = false;
				purchaseorderstaterow.F.FilterAsDraft = false;
				purchaseorderstaterow.F.FilterAsIssued = false;
				purchaseorderstaterow.F.FilterAsClosed = false;
				purchaseorderstaterow.F.FilterAsVoid = true;
				purchaseorderstaterow.F.CanHaveReceiving = false;
				purchaseorderstaterow.F.CanModifyOrder = false;
				purchaseorderstaterow.F.CanModifyReceiving = false;

				// Set the StateTransition table for PurchaseOrders
				ds.EnsureDataTableExists(dsMB.Schema.T.PurchaseOrderStateTransition);
				foreach (MB3Client.StateHistoryTransition t in PurchaseOrderStateTransitions)
					MB3Client.PurchaseOrderHistoryTable.Value.StateHistoryTransitionToRow(t, ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.PurchaseOrderStateTransition));

				// Set the Default PurchaseOrderStateHistory initial state for PurchaseOrders to 'Draft'
				DataRow defaultPurchaseOrderStateHistoryRow;
				defaultPurchaseOrderStateHistoryRow = ds.DB.EditSingleRow(ds, dsMB.Schema.T.PurchaseOrderStateHistory.Default, null);
				dsMB.Schema.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID[defaultPurchaseOrderStateHistoryRow] = PODraftStateID;
				dsMB.Schema.T.PurchaseOrderStateHistory.F.EffectiveDateReadonly[defaultPurchaseOrderStateHistoryRow] = false;

				#endregion

				#region Accounting-related records

				// Default Accounting-related entries.
				// Default cost centers and expense models, etc. are created.
				// Additionally, default records are created for all tables that
				// refer to expense models or cost centers etc.

				dsMB.CostCenterRow costCenterRow;   // used to define various cost centers where needed.
				ds.EnsureDataTableExists(dsMB.Schema.T.CostCenter);

				#region Work Order related accounting info

				// WorkOrderExpenseCategory
				dsMB.WorkOrderExpenseCategoryRow workOrderExpenseCategoryRow;
				ds.EnsureDataTableExists(dsMB.Schema.T.WorkOrderExpenseCategory);

				DataRow WorkOrderExpenseCategoryDefaultRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.WorkOrderExpenseCategory.Default, null);
				dsMB.Schema.T.WorkOrderExpenseCategory.F.FilterAsItem[WorkOrderExpenseCategoryDefaultRow] = false;
				dsMB.Schema.T.WorkOrderExpenseCategory.F.FilterAsLabor[WorkOrderExpenseCategoryDefaultRow] = false;
				dsMB.Schema.T.WorkOrderExpenseCategory.F.FilterAsMiscellaneous[WorkOrderExpenseCategoryDefaultRow] = false;

				workOrderExpenseCategoryRow = ds.T.WorkOrderExpenseCategory.AddNewWorkOrderExpenseCategoryRow();
				workOrderExpenseCategoryRow.F.Hidden = null;
				workOrderExpenseCategoryRow.F.Code = DatabaseLayoutK("Default Expense Category").Translate(null);
				workOrderExpenseCategoryRow.F.Desc = DatabaseLayoutK("Default Expense Category").Translate(null);
				workOrderExpenseCategoryRow.F.FilterAsItem = true;
				workOrderExpenseCategoryRow.F.FilterAsLabor = true;
				workOrderExpenseCategoryRow.F.FilterAsMiscellaneous = true;

				// Set the default expenseCategory for all demand types, and all template demand types
				// Also set the default costing calculation to Current Value calculation.
				ds.DB.Edit(ds, dsMB.Schema.T.Demand.Default, null);
				foreach (DataRow r in dsMB.Schema.T.Demand.Default.GetDataTable(ds).Rows) {
					dsMB.Schema.T.Demand.F.WorkOrderExpenseCategoryID[r] = workOrderExpenseCategoryRow.F.Id;
					dsMB.Schema.T.Demand.F.DemandActualCalculationInitValue[r] = DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue;
				}
				ds.DB.Edit(ds, dsMB.Schema.T.DemandTemplate.Default, null);
				foreach (DataRow r in dsMB.Schema.T.DemandTemplate.Default.GetDataTable(ds).Rows) {
					dsMB.Schema.T.DemandTemplate.F.WorkOrderExpenseCategoryID[r] = null; // DemandTemplates typically have no overriding ExpenseCategory
					dsMB.Schema.T.DemandTemplate.F.EstimateCost[r] = true;
					dsMB.Schema.T.DemandTemplate.F.DemandActualCalculationInitValue[r] = DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue;
				}

				// WorkOrderExpenseModel defaults
				costCenterRow = newCostCenterRow(ds, "Default Non Stock Item Holding Costs");
				DataRow WorkOrderExpenseModelDefaultRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.WorkOrderExpenseModel.Default, null);
				dsMB.Schema.T.WorkOrderExpenseModel.F.NonStockItemHoldingCostCenterID[WorkOrderExpenseModelDefaultRow] = costCenterRow.F.Id;

				// WorkOrderExpenseModel definitions
				dsMB.WorkOrderExpenseModelRow workOrderExpenseModelRow;
				ds.EnsureDataTableExists(dsMB.Schema.T.WorkOrderExpenseModel);

				workOrderExpenseModelRow = ds.T.WorkOrderExpenseModel.AddNewWorkOrderExpenseModelRow();
				workOrderExpenseModelRow.F.Hidden = null;
				workOrderExpenseModelRow.F.Code = DatabaseLayoutK("Default Expense Model").Translate(null);
				workOrderExpenseModelRow.F.Desc = DatabaseLayoutK("Default Expense Model").Translate(null);
				workOrderExpenseModelRow.F.NonStockItemHoldingCostCenterID = costCenterRow.F.Id;
				// Later we will set the defaults for categories but they involve a circular reference so we must save the Expense Model Entry row first.

				DataRow defaultWorkOrderRow;
				defaultWorkOrderRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.WorkOrder.Default, null);
				dsMB.Schema.T.WorkOrder.F.WorkOrderExpenseModelID[defaultWorkOrderRow] = workOrderExpenseModelRow.F.Id;
				dsMB.Schema.T.WorkOrder.F.SelectPrintFlag[defaultWorkOrderRow] = true;

				DataRow defaultWorkOrderTemplateRow;
				defaultWorkOrderTemplateRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.WorkOrderTemplate.Default, null);
				dsMB.Schema.T.WorkOrderTemplate.F.WorkOrderExpenseModelID[defaultWorkOrderTemplateRow] = workOrderExpenseModelRow.F.Id;
				dsMB.Schema.T.WorkOrderTemplate.F.Duration[defaultWorkOrderTemplateRow] = new TimeSpan(1, 0, 0, 0, 0); // default duration for new Tasks is 1 day ala MainBoss 2.9
				dsMB.Schema.T.WorkOrderTemplate.F.GenerateLeadTime[defaultWorkOrderTemplateRow] = new TimeSpan(0, 0, 0, 0, 0); // default is no lead time on a task
				dsMB.Schema.T.WorkOrderTemplate.F.SelectPrintFlag[defaultWorkOrderTemplateRow] = true; // default SelectPrintFlag on for top level Tasks

				DataRow defaultUnitRow;
				defaultUnitRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.Unit.Default, null);
				dsMB.Schema.T.Unit.F.WorkOrderExpenseModelID[defaultUnitRow] = workOrderExpenseModelRow.F.Id;

				// WorkOrderExpenseModelEntry defaults
				costCenterRow = newCostCenterRow(ds, "Default Work Order Costs");
				DataRow workOrderExpenseModelEntryDefaultRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.WorkOrderExpenseModelEntry.Default, null);
				dsMB.Schema.T.WorkOrderExpenseModelEntry.F.CostCenterID[workOrderExpenseModelEntryDefaultRow] = costCenterRow.F.Id;

				// WorkOrderExpenseModelEntry definitions
				dsMB.WorkOrderExpenseModelEntryRow workOrderExpenseModelEntryRow;
				ds.EnsureDataTableExists(dsMB.Schema.T.WorkOrderExpenseModelEntry);

				workOrderExpenseModelEntryRow = ds.T.WorkOrderExpenseModelEntry.AddNewWorkOrderExpenseModelEntryRow();
				workOrderExpenseModelEntryRow.F.WorkOrderExpenseModelID = workOrderExpenseModelRow.F.Id;
				workOrderExpenseModelEntryRow.F.WorkOrderExpenseCategoryID = workOrderExpenseCategoryRow.F.Id;
				workOrderExpenseModelEntryRow.F.CostCenterID = costCenterRow.F.Id;

				// LaborInside
				costCenterRow = newCostCenterRow(ds, "Default Hourly Inside Costs");

				DataRow defaultInsideLaborRow;
				defaultInsideLaborRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.LaborInside.Default, null);
				dsMB.Schema.T.LaborInside.F.CostCenterID[defaultInsideLaborRow] = costCenterRow.F.Id;

				// OtherWorkInside
				costCenterRow = newCostCenterRow(ds, "Default Per Job Inside Costs");

				DataRow defaultInsideOtherWorkRow;
				defaultInsideOtherWorkRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.OtherWorkInside.Default, null);
				dsMB.Schema.T.OtherWorkInside.F.CostCenterID[defaultInsideOtherWorkRow] = costCenterRow.F.Id;

				// MiscellaneousWorkOrderCost
				costCenterRow = newCostCenterRow(ds, "Default Miscellaneous Work Order Costs");

				DataRow defaultMiscellaneousRow;
				defaultMiscellaneousRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.MiscellaneousWorkOrderCost.Default, null);
				dsMB.Schema.T.MiscellaneousWorkOrderCost.F.CostCenterID[defaultMiscellaneousRow] = costCenterRow.F.Id;

				#endregion

				#region Purchase Order related accounting info

				DataRow defaultPurchaseOrderTemplateRow;
				defaultPurchaseOrderTemplateRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.PurchaseOrderTemplate.Default, null);
				dsMB.Schema.T.PurchaseOrderTemplate.F.RequiredByInterval[defaultPurchaseOrderTemplateRow] = new TimeSpan(0, 0, 0, 0, 0); // i.e. NOW !
				dsMB.Schema.T.PurchaseOrderTemplate.F.SelectPrintFlag[defaultPurchaseOrderTemplateRow] = true; // SelectPrintFlag on for new Purchase order templates

				// Miscellaneous purchase items
				costCenterRow = newCostCenterRow(ds, "Default Miscellaneous Costs");

				DataRow defaultMiscRow;
				defaultMiscRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.Miscellaneous.Default, null);
				dsMB.Schema.T.Miscellaneous.F.CostCenterID[defaultMiscRow] = costCenterRow.F.Id;

				#endregion

				#region Item related accounting info

				// Items
				costCenterRow = newCostCenterRow(ds, "Default Item Costs");
				DataRow defaultActualItemLocationRow;
				DataRow defaultPermanentItemLocationRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.PermanentItemLocation.Default, null);
				defaultActualItemLocationRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.ActualItemLocation.Default,
					new SqlExpression(dsMB.Schema.T.ActualItemLocation.Default.InternalId).Eq((Guid)dsMB.Schema.T.PermanentItemLocation.F.ActualItemLocationID[defaultPermanentItemLocationRow]));
				dsMB.Schema.T.ActualItemLocation.F.CostCenterID[defaultActualItemLocationRow] = costCenterRow.F.Id;

				DataRow defaultTemporaryItemLocationRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.TemporaryItemLocation.Default, null);
				defaultActualItemLocationRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.ActualItemLocation.Default,
					new SqlExpression(dsMB.Schema.T.ActualItemLocation.Default.InternalId).Eq((Guid)dsMB.Schema.T.TemporaryItemLocation.F.ActualItemLocationID[defaultTemporaryItemLocationRow]));
				dsMB.Schema.T.ActualItemLocation.F.CostCenterID[defaultActualItemLocationRow] = costCenterRow.F.Id;

				// Adjustments
				costCenterRow = newCostCenterRow(ds, "Default Adjustment Costs");

				DataRow defaultItemAdjustmentCodeRow;
				defaultItemAdjustmentCodeRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.ItemAdjustmentCode.Default, null);
				dsMB.Schema.T.ItemAdjustmentCode.F.CostCenterID[defaultItemAdjustmentCodeRow] = costCenterRow.F.Id;

				// Issues
				costCenterRow = newCostCenterRow(ds, "Default Issue Costs");

				DataRow defaultItemIssueCodeRow;
				defaultItemIssueCodeRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.ItemIssueCode.Default, null);
				dsMB.Schema.T.ItemIssueCode.F.CostCenterID[defaultItemIssueCodeRow] = costCenterRow.F.Id;

				#endregion

				#region A/R & A/P

				costCenterRow = newCostCenterRow(ds, "Default Accounts Payable Costs");

				DataRow defaultVendorRow;
				defaultVendorRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.Vendor.Default, null);
				dsMB.Schema.T.Vendor.F.AccountsPayableCostCenterID[defaultVendorRow] = costCenterRow.F.Id;

				costCenterRow = newCostCenterRow(ds, "Default Accounts Receivable Costs");

				DataRow defaultBillableRequestorRow;
				defaultBillableRequestorRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.BillableRequestor.Default, null);
				dsMB.Schema.T.BillableRequestor.F.AccountsReceivableCostCenterID[defaultBillableRequestorRow]
					= costCenterRow.F.Id;

				costCenterRow = newCostCenterRow(ds, "Default Chargeback Costs");

				DataRow defaultChargebackLineCategoryRow;
				defaultChargebackLineCategoryRow = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.ChargebackLineCategory.Default, null);
				dsMB.Schema.T.ChargebackLineCategory.F.CostCenterID[defaultChargebackLineCategoryRow] = costCenterRow.F.Id;

				#endregion

				#endregion

				#region Preventive Maintenance & Scheduling
				ds.V.PmGenerateInterval.Value = new TimeSpan(7, 0, 0, 0, 0);// one week default setting
																			// Default is days of the week are enabled.
				ds.DB.Edit(ds, dsMB.Schema.T.Schedule.Default, null);
				// Enable by default all the week days for schedules.
				// Set other required bool fields to
				foreach (DataRow r in dsMB.Schema.T.Schedule.Default.GetDataTable(ds).Rows) {
					dsMB.Schema.T.Schedule.F.EnableMonday[r] = true;
					dsMB.Schema.T.Schedule.F.EnableTuesday[r] = true;
					dsMB.Schema.T.Schedule.F.EnableWednesday[r] = true;
					dsMB.Schema.T.Schedule.F.EnableThursday[r] = true;
					dsMB.Schema.T.Schedule.F.EnableFriday[r] = true;
					dsMB.Schema.T.Schedule.F.EnableSaturday[r] = false;
					dsMB.Schema.T.Schedule.F.EnableSunday[r] = false;

					dsMB.Schema.T.Schedule.F.InhibitIfOverdue[r] = false;
					dsMB.Schema.T.Schedule.F.InhibitSeason[r] = false;
					dsMB.Schema.T.Schedule.F.InhibitWeek[r] = false;
				}

				ds.DB.Edit(ds, dsMB.Schema.T.ScheduledWorkOrder.Default, null);
				// Enable by default any new Unit Maintenance Plans
				foreach (DataRow r in dsMB.Schema.T.ScheduledWorkOrder.Default.GetDataTable(ds).Rows) {
					dsMB.Schema.T.ScheduledWorkOrder.F.Inhibit[r] = false;
					dsMB.Schema.T.ScheduledWorkOrder.F.RescheduleBasisAlgorithm[r] = (int)DatabaseEnums.RescheduleBasisAlgorithm.FromScheduleBasis;
				}

				ds.DB.Edit(ds, dsMB.Schema.T.PMGenerationBatch.Default, null);
				// Set RequiredBool to reasonable defaults
				foreach (DataRow r in dsMB.Schema.T.PMGenerationBatch.Default.GetDataTable(ds).Rows) {
					dsMB.Schema.T.PMGenerationBatch.F.SinglePurchaseOrders[r] = false;
					dsMB.Schema.T.PMGenerationBatch.F.AccessCodeUnitTaskPriority[r] = (int)DatabaseEnums.TaskUnitPriority.PreferUnitValue;
					dsMB.Schema.T.PMGenerationBatch.F.WorkOrderExpenseModelUnitTaskPriority[r] = (int)DatabaseEnums.TaskUnitPriority.PreferUnitValue;
				}

				#endregion
				#region Organization Name
				ds.V.OrganizationName.Value = OrganizationName;
				#endregion

				#region Reports
				ds.V.ReportFont.Value = Defaults.Font;
				ds.V.ReportFontFixedWidth.Value = Defaults.FontFixedWidth;
				ds.V.ReportFontSize.Value = Defaults.FontSize;
				ds.V.ReportFontSizeFixedWidth.Value = Defaults.FontSize;
				ds.V.BarCodeSymbology.Value = 0; // Thinkage.Libraries.Presentation.BarCodeSymbology.None
				#endregion

				ds.DB.Update(ds, Thinkage.Libraries.DBILibrary.Server.UpdateOptions.NoConcurrencyCheck);

				// Add circular references in Work Order Expense Models
				workOrderExpenseModelRow.F.DefaultItemExpenseModelEntryID = workOrderExpenseModelEntryRow.F.Id;
				workOrderExpenseModelRow.F.DefaultHourlyInsideExpenseModelEntryID = workOrderExpenseModelEntryRow.F.Id;
				workOrderExpenseModelRow.F.DefaultHourlyOutsideExpenseModelEntryID = workOrderExpenseModelEntryRow.F.Id;
				workOrderExpenseModelRow.F.DefaultPerJobInsideExpenseModelEntryID = workOrderExpenseModelEntryRow.F.Id;
				workOrderExpenseModelRow.F.DefaultPerJobOutsideExpenseModelEntryID = workOrderExpenseModelEntryRow.F.Id;
				workOrderExpenseModelRow.F.DefaultMiscellaneousExpenseModelEntryID = workOrderExpenseModelEntryRow.F.Id;

				ds.DB.Update(ds, Thinkage.Libraries.DBILibrary.Server.UpdateOptions.NoConcurrencyCheck);
			}
		}
		/// <summary>
		/// Add licenses to a MainBoss database
		/// </summary>
		/// <param name="mb3db"></param>
		/// <param name="licenses"></param>
		public static void AddLicenses(MB3Client mb3db, params License[] licenses) {
			using (var ds = new dsMB(mb3db)) {
				ds.EnsureDataTableExists(dsMB.Schema.T.License);
				foreach (License l in licenses)
					SetLicenseRow(ds.T.License.AddNewLicenseRow(), l);
				ds.DB.Update(ds, Thinkage.Libraries.DBILibrary.Server.UpdateOptions.NoConcurrencyCheck);
			}
		}
		public static void SetLicenseRow(dsMB.LicenseRow licenserow, License license) {
			licenserow.F.License = license.LicenseStr;
			licenserow.F.ApplicationID = license.ApplicationID;
			if (license.ExpiryDateSpecified)
				licenserow.F.Expiry = license.Expiry;
			licenserow.F.ExpiryModel = (short)license.ExpiryModel;
			licenserow.F.LicenseCount = license.LicenseCount;
			licenserow.F.LicenseModel = (short)license.LicenseModel;
			licenserow.F.LicenseID = license.LicenseID;
		}
		/// <summary>
		/// Create default cost center codes for the initial database.
		/// </summary>
		/// <param name="ds"></param>
		/// <param name="code"></param>
		/// <returns></returns>
		private static dsMB.CostCenterRow newCostCenterRow(dsMB ds, [Context(Level = 1)] string code) {
			dsMB.CostCenterRow costCenterRow;
			costCenterRow = ds.T.CostCenter.AddNewCostCenterRow();
			costCenterRow.F.Hidden = null;
			costCenterRow.F.Code = DatabaseLayoutK(code).Translate(null);
			costCenterRow.F.Desc = DatabaseLayoutK(code).Translate(null);   // same desc as code for created items.
			return costCenterRow;
		}
		#endregion
	}
}
