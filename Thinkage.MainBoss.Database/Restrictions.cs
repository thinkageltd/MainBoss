// This file defines restrictions on when certain fields or tables can be modified.
// Eventually this information should perhaps be represented as DBI objects and declared using extensions in the actual schema.
// Ideally the Edit and Browse controls should consult this information in order to force some controls readonly, filter the values available in pickers,
// and limit access to New operations where a forced value would violate the editor's picker's filter.
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
namespace Thinkage.MainBoss.Database {
	public static class MBRestrictions {
		public static void DefineRestrictions() {
			Key message;
			DBI_Path flagPath;

			// TODO: THings that need explicit specifications based on restrictions:
			// Any browsettes or top-level browsers that (by UI design choice) only show things associated with certain state flags must be explicitly filtered.
			// Any unbound editor fields that are input to a calculation or enable a Init that targets a controlled field need a ECol attribute that says
			// they are restricted the same as the field they indirectly change (WO work interval duration, 'use access code from unit' on WO editor)

			// TODO: Prevent deletion of records which could not be created based on current state bits.
			#region Restrictions based on WorkOrder State Flags
			flagPath = dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.TemporaryStorageActive;
			message = KB.K("The current Work Order State does not permit changes to temporary storage");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.TemporaryStorage.F.WorkOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.TemporaryItemLocation.F.WorkOrderID, flagPath, message);
			flagPath = new DBI_Path(dsMB.Path.T.Location.F.TemporaryStorageID.F.WorkOrderID.PathToReferencedRow, flagPath);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, flagPath, message);
			// The following forces the PO's ship-to location picker to be filtered.
			DBI_WriteRestriction.RestrictSpecific(dsMB.Path.T.PurchaseOrder.F.ShipToLocationID, flagPath, message, dsMB.Path.T.PurchaseOrder.F.ShipToLocationID);
			// Also control all purchasing and inventory traffic to temp itemlocations
			flagPath = new DBI_Path(dsMB.Path.T.ItemLocation.F.LocationID.PathToReferencedRow, flagPath);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandItem.F.ItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualItem.F.DemandItemID.F.ItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ItemTransfer.F.FromItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ItemTransfer.F.ToItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ItemIssue.F.ItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ItemAdjustment.F.ItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID, flagPath, message);
			// ItemCountValue is never applicable to temporary locations.
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineItem.F.ItemLocationID, flagPath, message);

			// On demands the restriction applies to all derivations so we can restrict the base and derived tables individually.
			flagPath = dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyDemands;
			message = KB.K("The current Work Order State does not permit changes to resource demands");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandLaborInside.F.DemandID.F.WorkOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandLaborOutside.F.DemandID.F.WorkOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.WorkOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.WorkOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.WorkOrderID, flagPath, message);

			// Actuals control: all actuals. All of these link to the WO through the Demand records.
			flagPath = dsMB.Path.T.Demand.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyActuals;
			message = KB.K("The current Work Order State does not permit resource actualization");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualItem.F.DemandItemID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.DemandID, flagPath, message);

			flagPath = dsMB.Path.T.Demand.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyPOLines;
			message = KB.K("The current Work Order State does not permit purchasing of outside labor");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.DemandID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.DemandID, flagPath, message);

			// CanModifyChargebacks does exactly what is says.
			flagPath = dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyChargebacks;
			message = KB.K("The current Work Order State does not permit changes to Chargebacks");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.Chargeback.F.WorkOrderID, flagPath, message);

			flagPath = dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyChargebackLines;
			message = KB.K("The current Work Order State does not permit changes to Chargeback detail lines");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ChargebackLine.F.ChargebackID.F.WorkOrderID, flagPath, message);

			// Direct WO field restrictions include everything except operational fields and the select-for-print flag and Comments field.
			flagPath = dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyDefinitionFields;
			message = KB.K("The current Work Order State does not permit changes to the Work Order");
			DBI_WriteRestriction.RestrictAllBut(flagPath, message,
				dsMB.Path.T.WorkOrder.F.SelectPrintFlag,
				dsMB.Path.T.WorkOrder.F.ClosingComment,
				// the rest are operational fields and should be duplicated in the CanModifyOperationalFields restriction
				dsMB.Path.T.WorkOrder.F.Downtime,
				dsMB.Path.T.WorkOrder.F.CloseCodeID,
				dsMB.Path.T.WorkOrder.F.StartDateEstimate,
				dsMB.Path.T.WorkOrder.F.EndDateEstimate
				);

			// WO operational restrictions only covers specific fields.
			flagPath = dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyOperationalFields;
			message = KB.K("The current Work Order State does not permit changes to operational information");
			DBI_WriteRestriction.RestrictSpecific(flagPath, message,
				// operational fields to restrict
				dsMB.Path.T.WorkOrder.F.Downtime,
				dsMB.Path.T.WorkOrder.F.CloseCodeID,
				dsMB.Path.T.WorkOrder.F.StartDateEstimate,
				dsMB.Path.T.WorkOrder.F.EndDateEstimate
			);
			// Restrict Requested WorkOrders linkages according to WorkOrderState
			DBI_WriteRestriction.RestrictSpecific(
				dsMB.Path.T.MeterReading.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.CanModifyOperationalFields,
				KB.K("The current Work Order State does not permit changes to its associated Meter Readings"),
				dsMB.Path.T.MeterReading.F.WorkOrderID);
			#endregion

			#region Restrictions based on PurchaseOrderState Flags
			flagPath = dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.CanModifyOrder;
			message = KB.K("The current Purchase Order State does not permit changes to the Purchase Order");
			DBI_WriteRestriction.RestrictAllBut(flagPath, message, dsMB.Path.T.PurchaseOrder.F.Comment, dsMB.Path.T.PurchaseOrder.F.SelectPrintFlag);
			message = KB.K("The current Purchase Order State does not permit changes to the Purchase Order lines");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderID, flagPath, message);

			flagPath = dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.CanModifyReceiving;
			message = KB.K("The current Purchase Order State does not permit changes to receiving");
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.Receipt.F.PurchaseOrderID, flagPath, message);
			// Note that because of an equate, there are two paths from the receive line to the PO, one via the POLine and one via the Receipt.
			// Because of the need to init the To C/C, all new receive lines currently have their POLine linkage inited by a calling browser, so
			// the path through the PO line always restrict properly, even coming from a line-item browsette in the Receipt editor (which would otherwise
			// only init the ReceiptID).
			// If we ever give the receipt line editors the ability to properly find their own To C/C, and thus remove the mandated Init of the POLine
			// we will have to also code the restrictions via the other (Receipt) path as per the commented-out line.
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.POLineID.F.PurchaseOrderID, flagPath, message);
//			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.PurchaseOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.POLineID.F.PurchaseOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.POLineID.F.PurchaseOrderID, flagPath, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.POLineID.F.PurchaseOrderID, flagPath, message);
			#endregion
			#region Restrictions to Purchase Order based on presence of receiving
			flagPath = dsMB.Path.T.PurchaseOrder.F.HasReceiving;
			message = KB.K("The Vendor cannot be changed once there has been any Receiving");
			DBI_WriteRestriction.RestrictSpecific(pathExpr => pathExpr.IsFalse(), flagPath, message, dsMB.Path.T.PurchaseOrder.F.VendorID);
			#endregion

			#region Restrictions on modifying Specification Forms that are in use
			message = KB.K("Specification Form Fields cannot be changed if a Specification already uses their associated Specification Form");
			// Although the EditAllowed field is in the Form, none of the Form fields themselves are restricted by it.
			// DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.SpecificationForm.F.EditAllowed, message);
			DBI_WriteRestriction.RestrictAllBut(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID.F.EditAllowed, message);
			#endregion

			#region Restrictions on causing accounting discrepancies
			#region - Restriction on changing the CostCenterID of an ActualItemLocation that has non-zero value
			message = KB.K("The Cost Center of a Storage Assignment cannot be changed because the Total Cost is not zero");
			DBI_WriteRestriction.RestrictSpecific(
				delegate(SqlExpression totalCostExpression) {
					return totalCostExpression.Eq(SqlExpression.Constant(0m)).IsFalse().Not();
				},
				dsMB.Path.T.ActualItemLocation.F.TotalCost,
				message,
				dsMB.Path.T.ActualItemLocation.F.CostCenterID
			);
			#endregion
			#region Restrictions on Deleting the Service record if a service is installed.
			message = KB.K("The Windows Service for MainBoss cannot exist");
			DBI_WriteRestriction.RestrictSpecific(
				delegate(SqlExpression serviceName) {
					return serviceName.IsNull();
				}
				, dsMB.Path.T.ServiceConfiguration.F.SqlUserid
				, message
				, dsMB.Path.T.ServiceConfiguration.F.Code
				, dsMB.Path.T.ServiceConfiguration.F.Id			 // this should prevent record deletion but doesn't because of strange code in browse logic
				, dsMB.Path.T.ServiceConfiguration.F.SqlUserid // need to restrict this field to prevent deletion because of strange code in browse logic
			);
			#endregion
			#region Restrictions on Deleting the Items when OnHand or other inventory fields are non-zero
			message = KB.K("{0} must be zero");
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0)).IsFalse().Not();
				}
				, dsMB.Path.T.Item.F.OnHand
				, message
			);
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0m)).IsFalse().Not();
				}
				, dsMB.Path.T.Item.F.TotalCost
				, message
			);
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0)).IsFalse().Not();
				}
				, dsMB.Path.T.Item.F.OnReserve
				, message
			);
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0)).IsFalse().Not();
				}
				, dsMB.Path.T.Item.F.OnOrder
				, message
			);
			// Item Storerooms
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0)).IsFalse().Not();
				}
				, dsMB.Path.T.ActualItemLocation.F.OnHand
				, message
			);
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0m)).IsFalse().Not();
				}
				, dsMB.Path.T.ActualItemLocation.F.TotalCost
				, message
			);
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0)).IsFalse().Not();
				}
				, dsMB.Path.T.ActualItemLocation.F.OnReserve
				, message
			);
			DBI_WriteRestriction.RestrictDelete(
				delegate (SqlExpression qty) {
					return qty.Eq(SqlExpression.Constant(0)).IsFalse().Not();
				}
				, dsMB.Path.T.ActualItemLocation.F.OnOrder
				, message
			);
			#endregion
			#endregion
		}
	}
}