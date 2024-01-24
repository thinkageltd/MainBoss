using System;
using System.Linq;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.DBAccess;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Purchase Orders.
	/// </summary>
	public class TIPurchaseOrder : TIGeneralMB3
	{
		#region Record-type providers
		#region - ReceiptActivityProvider and PurchaseOrderLineProvider
		public static EnumValueTextRepresentations ReceiptActivityProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Item Purchase Line"),
				KB.TOi(TId.ReceiveItemWithPO),
				KB.TOi(TId.CorrectionofReceiveItemWithPO),
				KB.K("Hourly Purchase Line"),
				KB.K("Actual Hourly (with PO)"),
				KB.K("Correction of Actual Hourly (with PO)"),
				KB.K("Per Job Purchase Line"),
				KB.K("Actual Per Job (with PO)"),
				KB.K("Correction of Actual Per Job (with PO)"),
				KB.K("Miscellaneous Purchase Line"),
				KB.TOi(TId.ReceiveMiscellaneous),
				KB.TOi(TId.CorrectionofReceiveMiscellaneous)
			},
			null,
			new object[] {
				(int)ViewRecordTypes.ReceiptActivity.POLineItem,
				(int)ViewRecordTypes.ReceiptActivity.ReceiveItemPO,
				(int)ViewRecordTypes.ReceiptActivity.ReceiveItemPOCorrection,
				(int)ViewRecordTypes.ReceiptActivity.POLineLabor,
				(int)ViewRecordTypes.ReceiptActivity.ActualLaborOutsidePO,
				(int)ViewRecordTypes.ReceiptActivity.ActualLaborOutsidePOCorrection,
				(int)ViewRecordTypes.ReceiptActivity.POLineOtherWork,
				(int)ViewRecordTypes.ReceiptActivity.ActualOtherWorkOutsidePO,
				(int)ViewRecordTypes.ReceiptActivity.ActualOtherWorkOutsidePOCorrection,
				(int)ViewRecordTypes.ReceiptActivity.POLineMiscellaneous,
				(int)ViewRecordTypes.ReceiptActivity.ReceiveMiscellaneousPO,
				(int)ViewRecordTypes.ReceiptActivity.ReceiveMiscellaneousPOCorrection
			}
		);
		/// <summary>
		/// This view uses the SAME enum values as Receipt activity Provider
		/// </summary>
		public static EnumValueTextRepresentations PurchaseOrderLineProvider = ReceiptActivityProvider;
		#endregion
		#endregion
		#region NodeIds
		private static readonly object PrototypeDescriptionId = KB.I("PrototypeDescriptionId");
		private static readonly object ToOrderId = KB.I("ToOrderId");
		internal static readonly object POLinePricingBasisCost = KB.I("POLinePricingBasisCost");
		internal static readonly object POLinePricingBasisQuantity = KB.I("POLinePricingBasisQuantity");
		internal static readonly object POLineTotalCost = KB.I("POLineTotalCost");
		internal static readonly object POLineTotalQuantity = KB.I("POLineTotalQuantity");
		internal static readonly object PaymentTerms = KB.I("PaymentTerms");

		internal static readonly Key UseVendorPaymentTerms = KB.K("Use payment terms from vendor");
		internal static readonly Key BecauseUsingVendorPaymentTerms = KB.K("Readonly because vendor's payment terms are being used");

		internal static readonly Key costColumnId = dsMB.Path.T.POLineItem.F.POLineID.F.Cost.Key();
		internal static readonly Key uomColumnId = dsMB.Path.T.Item.F.UnitOfMeasureID.Key();
		internal static readonly Key quantityColumnId = dsMB.Path.T.POLineItem.F.Quantity.Key();

		#endregion
		#region Common list column caption keys
		private static Key CommonOrderLineColumnKey = dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID.F.PurchaseOrderText.Key(); // Common to all Purchase objects
		#endregion
		#region Named Tbls
		public static DelayedCreateTbl PurchaseOrderInProgressAssignedToBrowseTbl;
		public static DelayedCreateTbl UnassignedPurchaseOrderBrowseTbl;
		public static DelayedCreateTbl PurchaseOrderAssignedToEditorTblCreator;
		public static DelayedCreateTbl PurchaseOrderAssignmentByAssigneeTblCreator;

		public static DelayedCreateTbl VendorTblCreator;
		public static DelayedCreateTbl VendorForPurchaseOrdersTblCreator;

		public static DelayedCreateTbl PurchaseOrderDraftBrowseTbl;
		public static DelayedCreateTbl PurchaseOrderIssuedBrowseTbl;
		public static DelayedCreateTbl PurchaseOrderClosedBrowseTbl;
		public static DelayedCreateTbl PurchaseOrderVoidBrowseTbl;
		public static DelayedCreateTbl PurchaseOrderEditTblCreator;
		public static DelayedCreateTbl PurchaseOrderWithPOLineItemEditTbl;

		public static DelayedCreateTbl PurchaseOrderTemplateEditTbl;

		private static readonly DelayedCreateTbl POLineItemTblCreator;
		private static readonly DelayedCreateTbl POLineItemTemplateTblCreator;
		private static readonly DelayedCreateTbl AssociatedWorkOrdersTbl;
		private static readonly DelayedCreateTbl AssociatedWorkOrderTemplatesTbl;

		public static readonly DelayedCreateTbl POLineItemWithItemLocationInitTblCreator;	// TODO: There is no way to write PO lines from any ItemLocation editor; there should be and it should use this tbl for editing new records.
		private static readonly DelayedCreateTbl POLineItemWithPOInitTblCreator;
		public static readonly DelayedCreateTbl POLineLaborWithDemandInitTblCreator;
		private static readonly DelayedCreateTbl POLineLaborWithPOInitTblCreator;
		public static readonly DelayedCreateTbl POLineOtherWorkWithDemandInitTblCreator;
		private static readonly DelayedCreateTbl POLineOtherWorkWithPOInitTblCreator;
		// public static readonly DelayedCreateTbl POLineMiscellaneousWithMiscellaneousInitTblCreator; // would be needed e.g. from POLine browsette of Miscellaneous Item editor.
		private static readonly DelayedCreateTbl POLineMiscellaneousWithPOInitTblCreator;

		public static readonly DelayedCreateTbl POLineItemTemplateWithItemLocationInitTblCreator;// TODO: There is no way to write PO Template lines from any TemplateItemLocation editor; if there is, it should use this tbl for editing new records.
		private static readonly DelayedCreateTbl POLineItemTemplateWithPOTemplateInitTblCreator;
		public static readonly DelayedCreateTbl POLineLaborTemplateWithDemandTemplateInitTblCreator;
		private static readonly DelayedCreateTbl POLineLaborTemplateWithPOTemplateInitTblCreator;
		public static readonly DelayedCreateTbl POLineOtherWorkTemplateWithDemandTemplateInitTblCreator;
		private static readonly DelayedCreateTbl POLineOtherWorkTemplateWithPOTemplateInitTblCreator;
		// public static readonly DelayedCreateTbl POLineMiscellaneousTemplateWithMiscellaneousInitTblCreator; // would be needed e.g. from POLine browsette of Miscellaneous Item editor.
		private static readonly DelayedCreateTbl POLineMiscellaneousTemplateWithPOTemplateInitTblCreator;

		private static DelayedCreateTbl POLineItemDefaultEditorTblCreator;
		private static DelayedCreateTbl POLineLaborDefaultEditorTblCreator;
		private static DelayedCreateTbl POLineOtherWorkDefaultEditorTblCreator;
		private static DelayedCreateTbl POLineMiscellaneousTblCreator;
		#endregion
		#region State History with UI definition
		public static DelayedConstruction<StateHistoryUITable> PurchaseOrderHistoryTable = new DelayedConstruction<StateHistoryUITable>(delegate () {
			return new StateHistoryUITable(MB3Client.PurchaseOrderHistoryTable, null, FindDelayedEditTbl(dsMB.Schema.T.PurchaseOrderStateHistory), null);
		});
		#endregion
		#region Tbl-creator functions
		#region PurchaseOrderEdit
		/// <summary>
		/// The 'rules' say if you have PurchaseOrderFulfillment OR PurchaseOrderClose "as well as" PurchaseOrderAssigneSelf, you can use the "Self Assign" operation.
		/// We do this by knowing the PurchaseOrderAssignSelf role (only) allows 'Create' on the 'UnassignedPurchaseOrder' view as a table op.
		/// We explicitly create disablers for each of the required table rights. (We 'know' that PurchaseOrderFulfillment and PurchaseOrderClose have 'Create' on the PurchaseOrderStateHistory table)
		/// yech!
		/// </summary>
		/// <returns></returns>
		private static List<IDisablerProperties> SelfAssignDisablers() {
			ITblDrivenApplication app = Application.Instance.GetInterface<ITblDrivenApplication>();
			TableOperationRightsGroup rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild("UnassignedPurchaseOrder");
			var list = new List<IDisablerProperties>();
			list.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
			rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild(dsMB.Schema.T.PurchaseOrderStateHistory.Name);
			list.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
			return list;
		}
		private static Key SelfAssignCommand = KB.K("Self Assign");
		private static Key SelfAssignTip = KB.K("Add yourself as an assignee to this Purchase Order");
		private static void SelfAssignmentEditor(CommonLogic el, object requestID) {
			object requestAssigneeID;
			using (dsMB ds = new dsMB(el.DB)) {
				var row = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.PurchaseOrderAssignee,
					// Although a User row might be Hidden, such a row could not be the UserRecordID.
					// However, a deleted Contact could still name a User record so we check to ensure that the user's Contact record is not Hidden.
					new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(Application.Instance.GetInterface<Thinkage.Libraries.DBAccess.IApplicationWithSingleDatabaseConnection>().UserRecordID))
						.And(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.F.Hidden).IsNull()));
				if (row == null)
					throw new GeneralException(KB.K("You are not registered as a Purchase Order Assignee"));
				requestAssigneeID = ((dsMB.PurchaseOrderAssigneeRow)row).F.Id;
			}
			var initList = new List<TblActionNode>();
			initList.Add(Init.OnLoadNew(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, new ConstantValue(requestID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID, new ConstantValue(KnownIds.PurchaseOrderStateIssuedId)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID, new EditorPathValue(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateHistoryStatusID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.PurchaseOrderStateHistory.F.Comment, new ConstantValue(Strings.Format(KB.K("Self assigned")))));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID, 1, new EditorPathValue(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID, 1, new ConstantValue(requestAssigneeID)));
			Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().GetInterface<ITblDrivenApplication>().PerformMultiEdit(el.CommonUI.UIFactory, el.DB, TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.PurchaseOrderStateHistory),
				EdtMode.New,
				new[] { new object[] { } },
				ApplicationTblDefaults.NoModeRestrictions,
				new[] { initList },
				((ICommonUI)el.CommonUI).Form, el.CallEditorsModally,
				null);
		}

		private static DelayedCreateTbl PurchaseOrderEditTbl(TblLayoutNodeArray nodes, FeatureGroup featureGroup, bool AssignToSelf) {
			return new DelayedCreateTbl(delegate()
			{
				List<ETbl.ICtorArg> etblArgs = new List<ETbl.ICtorArg>();
				etblArgs.Add(MB3ETbl.HasStateHistoryAndSequenceCounter(dsMB.Path.T.PurchaseOrder.F.Number, dsMB.Schema.T.PurchaseOrderSequenceCounter, dsMB.Schema.V.POSequence, dsMB.Schema.V.POSequenceFormat, PurchaseOrderHistoryTable));
				etblArgs.Add(ETbl.EditorDefaultAccess(false));
				etblArgs.Add(ETbl.EditorAccess(true, EdtMode.Edit, EdtMode.View, EdtMode.Clone, EdtMode.EditDefault, EdtMode.ViewDefault, EdtMode.New));
				etblArgs.Add(ETbl.Print(TIReports.SinglePurchaseOrderFormReport, dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID));
				if (AssignToSelf) {
					etblArgs.Add(ETbl.CustomCommand(
							delegate(EditLogic el)
							{
								var group = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								group.Add(new EditLogic.CommandDeclaration(
									SelfAssignCommand,
									new MultiCommandIfAllEnabled(
										EditLogic.StateTransitionCommand.NewSameTargetState(el,
											SelfAssignTip,
											delegate()
											{
												SelfAssignmentEditor(el, el.RootRowIDs[0]);
											},
											el.AllStatesWithExistingRecord.ToArray()),
										SelfAssignDisablers().ToArray())
									));
								return group;
							}
					));
				}
				return new Tbl(dsMB.Schema.T.PurchaseOrder, TId.PurchaseOrder,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						featureGroup,
						new ETbl(etblArgs.ToArray()),
						TIReports.NewRemotePTbl(TIReports.PurchaseOrderFormReport)
					},
					(TblLayoutNodeArray)nodes.Clone(),
					Init.LinkRecordSets(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, 1, dsMB.Path.T.PurchaseOrder.F.Id, 0),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PurchaseOrderStateHistory.F.UserID, 1), new UserIDValue()),
					// Copy the PaymentTerm from Vendor if the checkbox is checked
					Init.New(new ControlTarget(PaymentTerms), new EditorPathValue(dsMB.Path.T.PurchaseOrder.F.VendorID.F.PaymentTermID), new ControlValue(UseVendorPaymentTerms), TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone)),
					// Arrange for PaymentTerm choices to be readonly if using the Vendor's values
					Init.Continuous(new ControlReadonlyTarget(PaymentTerms, BecauseUsingVendorPaymentTerms), new ControlValue(UseVendorPaymentTerms))
				);
			});
		}
		#endregion
		#region POLineItemsTbl
		private static CompositeTbl POLineItemsBrowseTbl()
		{
			Key purchaseNewItemGroup = KB.K("New PO Line Item");

			// For the Purchase Order editor's Lines browsette
			return new CompositeTbl(dsMB.Schema.T.PurchaseOrderLine, TId.PurchaseOrderLine,
				new Tbl.IAttr[]
				{
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.LineNumber),
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.PurchaseOrderText),
						BTbl.PerViewListColumn(quantityColumnId, quantityColumnId),
						BTbl.PerViewListColumn(uomColumnId, uomColumnId),
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.Cost)
					)
				},
				dsMB.Path.T.PurchaseOrderLine.F.TableEnum,
				new CompositeView(POLineItemWithPOInitTblCreator, dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineItemID,			// ViewRecordTypes.PurchaseOrderLine.POLineItem
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineItem.F.Quantity, IntegralFormat),
					BTbl.PerViewColumnValue(uomColumnId, dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code),
					CompositeView.NewCommandGroup(purchaseNewItemGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID)),
				null,
				null,
				new CompositeView(POLineLaborWithPOInitTblCreator, dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineLaborID,			// ViewRecordTypes.PurchaseOrderLine.POLineLabor
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineLabor.F.Quantity, IntervalFormat),
					CompositeView.NewCommandGroup(purchaseNewItemGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID)),
				null,
				null,
				new CompositeView(POLineOtherWorkWithPOInitTblCreator, dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineOtherWorkID,		// ViewRecordTypes.PurchaseOrderLine.POLineOtherWork
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineOtherWork.F.Quantity, IntegralFormat),
					CompositeView.NewCommandGroup(purchaseNewItemGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID)),
				null,
				null,
				new CompositeView(POLineMiscellaneousWithPOInitTblCreator, dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineMiscellaneousID,	// ViewRecordTypes.PurchaseOrderLine.POLineMiscellaneous
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineMiscellaneous.F.Quantity, IntegralFormat),
					CompositeView.NewCommandGroup(purchaseNewItemGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderID)),
				null,
				null
			);
		}
		#endregion
		// Note that the labels for all the Hourly/Per Job filtering are generic so they apply equally to either. They should be centralized somehow. Furthermore
		// all the filtering is essentially identical, other than mapping between Labor and OtherWork.
		#region POLineItem

		/// <summary>
		/// Build a POLineItemTbl either starting from an Item (Location) or from a Purchase Order. Only one filter argument can be true.
		/// </summary>
		/// <param name="filterPO">Build using PO Picking filters</param>
		/// <param name="filterResource">Build using Storage Assignment filters</param>
		/// <returns></returns>
		static DelayedCreateTbl POLineItemTbl(bool filterPO, bool filterResource, bool editDefaults) {
			return new DelayedCreateTbl(
				delegate() {
					POLineDerivationTblCreator<long> creator = new POLineDerivationTblCreator<long>(TId.PurchaseItem, dsMB.Schema.T.POLineItem, true, editDefaults, TIGeneralMB3.ItemUnitCostTypeOnClient);

					// Note that the do/don't want to consider transfers from other locations may be a fly in the ointment of database partitioning
					// (where different users only manage subsets of storage locations)
					// Also, history of previous receiving would interfere with partitioning, since a user in one partition would find history of
					// receiving to all partitions.
					creator.CreateItemNumberControl();
					if (!editDefaults) {
						if (filterPO) {
							// if filtering the PO, we expect to pick the resource first.
							System.Diagnostics.Debug.Assert(!filterResource);
							creator.POLineItemStorageAssignment(ToOrderId);
							creator.BuildItemPricePickerControl(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemPriceID, dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, null);
							creator.POLineItemPOFilter();
							creator.CreatePOControl();
						}
						else {
							// otherwise, we pick the PO first, then the resource
							creator.CreatePOControl();
							if (filterResource)
								creator.POLineItemResourceFilter();
							creator.POLineItemStorageAssignment(ToOrderId);
							creator.BuildItemPricePickerControl(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemPriceID, dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.VendorID);
						}
						creator.BuildSuggestedTextDisplay("Suggested Text", PrototypeDescriptionId);
					}
					creator.HandleSuggestedPOLineTextAndBuildPOLineTextControl("Use suggested text", PrototypeDescriptionId);
					if (editDefaults)
						creator.BuildQuantityControlforDefault("Order Quantity");
					else
						creator.HandleSuggestedQuantityAndBuildQuantityControl("Use To Order quantity", ToOrderId, "Order Quantity");
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code, ECol.AllReadonly));
					creator.BuildPickedItemPriceResultDisplays();
					creator.BuildPickedItemPriceResultValueTransfers();
					creator.Actions.Add(Init.Continuous(new ControlTarget(PrototypeDescriptionId),
						new EditorCalculatedInitValue(creator.PrototypeDescriptionTypeInfo,
							(values => values[1] ?? values[0]),
							new EditorPathValue(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.Code),
							new InSubBrowserValue(DerivationTblCreatorWithQuantityAndCostBase.ItemPricePickerId, new BrowserPathValue(dsMB.Path.T.ItemPricing.F.PurchaseOrderText))))
					);
					creator.BuildPickedItemPriceCostingControls();

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingAndInventoryGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.POLineItem.F.POLineID.F.LineNumber),
							BTbl.ListColumn(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineItem.F.Quantity, NonPerViewColumn)
						)
					);
				}
			);
		}
		#endregion
		#region POLineLabor
		static DelayedCreateTbl POLineLaborTbl(bool filterPO, bool filterResource, bool editDefaults) {
			return new DelayedCreateTbl(
				delegate() {
					POLineDerivationTblCreator<TimeSpan> creator = new POLineDerivationTblCreator<TimeSpan>(TId.PurchaseHourlyOutside, dsMB.Schema.T.POLineLabor, true, editDefaults, TIGeneralMB3.HourlyUnitCostTypeOnClient);
					creator.CreateItemNumberControl();

					if (filterPO) {
						// if filtering the PO, we expect to pick the resource first.
						System.Diagnostics.Debug.Assert(!filterResource);
						creator.POLineLaborAssignment(ToOrderId);
						creator.POLineLaborPOFilter();
						creator.CreatePOControl();
					}
					else
					{
						// otherwise, we pick the PO first, then the resource
						creator.CreatePOControl();
						if (filterResource)
							creator.POLineLaborResourceFilter();
						creator.POLineLaborAssignment(ToOrderId);
					}
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID.F.PurchaseOrderText, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.HandleSuggestedPOLineTextAndBuildPOLineTextControl("Use suggested text", PrototypeDescriptionId);
					if (editDefaults)
						creator.BuildQuantityControlforDefault("Order Time");
					else
						creator.HandleSuggestedQuantityAndBuildQuantityControl("Use remaining demand as quantity to order", ToOrderId, "Order Time");
					creator.HandleUnitCostAndBuildCostControl(new TimeSpan(1, 0, 0), RequiredUnitCostId, KB.K("Hourly Rate"));

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingAndLaborResourcesGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.POLineLabor.F.POLineID.F.LineNumber),
							BTbl.ListColumn(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineLabor.F.Quantity, NonPerViewColumn),
							BTbl.ListColumn(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.ActualQuantity, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter)
						)
					);
				}
			);
		}
		#endregion
		#region POLineOtherWork
		static DelayedCreateTbl POLineOtherWorkTbl(bool filterPO, bool filterResource, bool editDefaults) {
			return new DelayedCreateTbl(
				delegate() {
					POLineDerivationTblCreator<long> creator = new POLineDerivationTblCreator<long>(TId.PurchasePerJobOutside, dsMB.Schema.T.POLineOtherWork, true, editDefaults, TIGeneralMB3.PerJobUnitCostTypeOnClient);
					creator.CreateItemNumberControl();
					if (filterPO)
					{
						// if filtering the PO, we expect to pick the resource first.
						System.Diagnostics.Debug.Assert(!filterResource);
						creator.POLineOtherWorkAssignment(ToOrderId);
						creator.POLineOtherWorkPOFilter();
						creator.CreatePOControl();
					}
					else
					{
						// otherwise, we pick the PO first, then the resource
						creator.CreatePOControl();
						if (filterResource)
							creator.POLineOtherWorkResourceFilter();
						creator.POLineOtherWorkAssignment(ToOrderId);
					}
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.PurchaseOrderText, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.HandleSuggestedPOLineTextAndBuildPOLineTextControl("Use suggested text", PrototypeDescriptionId);
					if (editDefaults)
						creator.BuildQuantityControlforDefault("Order Quantity");
					else
						creator.HandleSuggestedQuantityAndBuildQuantityControl("Use remaining demand as quantity to order", ToOrderId, "Order Quantity");
					creator.HandleUnitCostAndBuildCostControl(1L, RequiredUnitCostId, KB.K("Unit Cost"));

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingAndLaborResourcesGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWork.F.POLineID.F.LineNumber),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWork.F.Quantity, NonPerViewColumn),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.ActualQuantity, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter)
						)
					);
				}
			);
		}
		#endregion
		#region POLineMiscellaneous
		static DelayedCreateTbl POLineMiscellaneousTbl(bool filterPO, bool filterResource, bool editDefaults) {
			return new DelayedCreateTbl(
				delegate() {
					POLineDerivationTblCreator<long> creator = new POLineDerivationTblCreator<long>(TId.PurchaseMiscellaneousItem, dsMB.Schema.T.POLineMiscellaneous, true, editDefaults, TIGeneralMB3.POMiscellaneousUnitCostTypeOnClient);
					creator.CreateItemNumberControl();
					if (filterPO) {
						// A - Work Vendor association - DefaultVisibility defined, Miscellaneous item definitions are not vendor-specific.

						// B - Vendor history:
						// - only POs for vendors that have previously been done the work
						// - No filtering based on history
						object BId = creator.AddPickerFilterControl(KB.K("Only include Purchase Orders for Vendors who have previously provided this item"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
						// This only occurs for PO purchasing.
						creator.AddPickerFilter(
							BTbl.ExpressionFilter(
								new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
									.In(new SelectSpecification(
										null,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
										new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.MiscellaneousID)
											.Eq(new SqlExpression(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID, 2)),
										null
									).SetDistinct(true))
								),
							BId,
							null
						);
					}
					creator.CreatePOControl();

					if (filterResource) {
						// A - Work vendor association - DefaultVisibility defined, Miscellaneous item definitions are not vendor-specific.

						// B - Vendor history:
						// - only demands for work that has previously been received
						// - No filtering based on history
						object BId = creator.AddPickerFilterControl(KB.K("Only include Miscellaneous items previously provided by this vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
						// This only occurs for PO purchasing.
						creator.AddPickerFilter(
							BTbl.ExpressionFilter(
								new SqlExpression(dsMB.Path.T.Miscellaneous.F.Id)
									.In(new SelectSpecification(
										null,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.MiscellaneousID) },
										new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID)
											.Eq(new SqlExpression(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
										null
									).SetDistinct(true))
								),
							BId,
							null
						);

						// C - quantity status - There is no associated Demand for doing Quantity filtering.
					}
					creator.AddPickerPanelDisplay(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID.F.Code);
					creator.CreateResourceControl(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID);
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID.F.Cost, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredUnitCostId))));
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID.F.PurchaseOrderText, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.HandleSuggestedPOLineTextAndBuildPOLineTextControl("Use suggested text", PrototypeDescriptionId);
					if (!editDefaults) {
						creator.BuildQuantityControl("Order Quantity");
						creator.HandleUnitCostAndBuildCostControl(1L, RequiredUnitCostId, KB.K("Unit Cost"));
					}
					else {
						creator.BuildQuantityControlforDefault("Order Quantity");
					}

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.LineNumber),
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneous.F.Quantity, NonPerViewColumn)
						)
					);
				}
			);
		}
		#endregion

		#region POLineItemsTemplateTbl
		private static CompositeTbl POLineItemsTemplateTbl() {
			Key newPOLineGroup = KB.K("New Line Item");
			object descriptionColumnId = KB.I("DescriptionId");
			// For the Purchase Order Template editor's Lines browsette
			return new CompositeTbl(dsMB.Schema.T.PurchaseOrderTemplateLine, TId.PurchaseOrderTemplateLine,
				new Tbl.IAttr[]
					{
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.LineNumberRank),
							BTbl.PerViewListColumn(CommonOrderLineColumnKey, descriptionColumnId),
							BTbl.PerViewListColumn(quantityColumnId, quantityColumnId)
						)
					},
				dsMB.Path.T.PurchaseOrderTemplateLine.F.TableEnum,
				new CompositeView(POLineItemTemplateWithPOTemplateInitTblCreator, dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.POLineItemTemplateID,				// Table #0 - Purchase tangible item
					BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID.F.PurchaseOrderText),
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineItemTemplate.F.Quantity, IntegralFormat),
					CompositeView.NewCommandGroup(newPOLineGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.PurchaseOrderTemplateID, dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID)),
				new CompositeView(POLineLaborTemplateWithPOTemplateInitTblCreator, dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.POLineLaborTemplateID,			// Table #1 - Purchase Labor
					BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.PurchaseOrderText),
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineLaborTemplate.F.Quantity, IntervalFormat),
					CompositeView.NewCommandGroup(newPOLineGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.PurchaseOrderTemplateID, dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID)),
				new CompositeView(POLineOtherWorkTemplateWithPOTemplateInitTblCreator, dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.POLineOtherWorkTemplateID,		// Table #2 - Purchase Other Work
					BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.PurchaseOrderText),
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineOtherWorkTemplate.F.Quantity, IntegralFormat),
					CompositeView.NewCommandGroup(newPOLineGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.PurchaseOrderTemplateID, dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID)),
				new CompositeView(POLineMiscellaneousTemplateWithPOTemplateInitTblCreator, dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.POLineMiscellaneousTemplateID,	// Table #3 - Purchase Miscellaneous charges
					BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID.F.PurchaseOrderText),
					BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineMiscellaneousTemplate.F.Quantity, IntegralFormat),
					CompositeView.NewCommandGroup(newPOLineGroup),
					CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.PurchaseOrderTemplateID, dsMB.Path.T.POLineMiscellaneousTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID))
			);
		}
		#endregion
		#region POLineItemTemplate
		static DelayedCreateTbl POLineItemTemplateTbl(bool filterPO, bool filterResource)
		{
			return new DelayedCreateTbl(
				delegate() {
					POLineTemplateDerivationTblCreator<long> creator = new POLineTemplateDerivationTblCreator<long>(TId.PurchaseTemplateItem, dsMB.Schema.T.POLineItemTemplate, TIGeneralMB3.ItemUnitCostTypeOnClient);
#if NEED_NUMBERS_RELATING_TO_TEMPLATE_DEMANDS_AND_POLINES
					// Pick the resource (ActualItemLocation)
					// TODO: Selectable filtering on the following picker:
					// A - vendor history:
					// - only assignments with a preferred pricing which is still valid [and is from correct vendor]
					// - only assignments with items that have valid price quotes [from current vendor]
					// - only assignments with items that have previously been received [from current vendor]
					// - No filtering based on history
					//
					// B - quantity status:
					// - IL's where available < min and total(onHand-Demand) for item < total min
					//		i.e. show items that must be ordered
					// - IL's where available < min
					//		i.e. show items that must be ordered if I don't want to transfer them from other locations
					// - IL's where available < max and total(onHand-Demand) for item < total max
					//		i.e. show items that should be ordered to make up max
					// - IL's where available < max
					//		i.e. show items that should be ordered to make up max if I don't want to transfer them from other locations
					// - No filtering based on quantity.
					//
						// TODO: the OnHand etc. numbers displayed here are those that relate to real demands and polines,
						// but to be of any use, we need to display similar numbers based on demand templates and poline templates.
						// OnHand will not be useful, but OnOrder, OnReserve, and Available will be of use if made relative
						// to the template demands/polines.
									TblGroupNode.New(KB.K("Item Details"), new TblLayoutNode.IAttr[] { EColBase.Normal },
										TblColumnNode.NewLastColumnBound("On Hand", dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.OnHand, DColBase.Normal, EColBase.AllReadonly ),
										TblColumnNode.NewLastColumnBound("On Order", dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.OnOrder, DColBase.Normal, EColBase.AllReadonly ),
										TblColumnNode.NewLastColumnBound("On Reserve", dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.OnReserve, DColBase.Normal, EColBase.AllReadonly ),
										TblColumnNode.NewLastColumnBound("Available", dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.Available, DColBase.Normal, EColBase.AllReadonly ),
										TblColumnNode.NewLastColumnBound("Unit Cost", dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.UnitCost, DColBase.Normal, EColBase.AllReadonly ),
										TblColumnNode.NewLastColumnBound("Total Cost", dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.TotalCost, DColBase.Normal, EColBase.AllReadonly )
									),
#endif
					creator.CreateItemNumberControl();
					if (filterPO)
					{
						// if filtering the PO, we expect to pick the resource first.
						System.Diagnostics.Debug.Assert(!filterResource);
						creator.POLineItemTemplateStorageAssignment();
// Not until we allow custom POText in a template						creator.BuildItemPricePickerControl(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID, dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID, null);
						creator.POLineItemTemplatePOTemplateFilter();
						creator.CreatePOTemplateControl();
					}
					else
					{
						// otherwise, we pick the PO Template first, then the resource
						creator.CreatePOTemplateControl();
						if (filterResource)
							creator.POLineItemTemplateResourceFilter();
						creator.POLineItemTemplateStorageAssignment();
// Not until we allow custom POText in a template						creator.BuildItemPricePickerControl(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID, dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID, dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID);
					}

					// Following Set from picked ItemPrice above
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID.F.PurchaseOrderText, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.HandleSuggestedQuantityAndBuildQuantityControl("Use minimum quantity", ItemLocationMinimumId, "Order Quantity");
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code, ECol.AllReadonly));
// Not until we allow custom POText in a template					creator.BuildPickedItemPriceResultHiddenValues();
// Not until we allow custom POText in a template					creator.BuildPickedItemPriceResultValueTransfers(PrototypeDescriptionId);

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingAndInventoryGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.LineNumberRank),
							BTbl.ListColumn(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineItemTemplate.F.Quantity, NonPerViewColumn)
						)
					);
				});
		}

		#endregion
		#region POLineLaborTemplate
		static DelayedCreateTbl POLineLaborTemplateTbl(bool filterPO, bool filterResource)
		{
			return new DelayedCreateTbl(
				delegate()
				{
					POLineTemplateDerivationTblCreator<TimeSpan> creator = new POLineTemplateDerivationTblCreator<TimeSpan>(TId.PurchaseTemplateHourlyOutside, dsMB.Schema.T.POLineLaborTemplate, TIGeneralMB3.HourlyUnitCostTypeOnClient);
					creator.CreateItemNumberControl();
					if (filterPO)
					{
						// if filtering the PO, we expect to pick the resource first.
						System.Diagnostics.Debug.Assert(!filterResource);
						creator.POLineLaborTemplateAssignment(ToOrderId);
						creator.POLineLaborTemplatePOTemplateFilter();
						creator.CreatePOTemplateControl();
					}
					else
					{
						// otherwise, we pick the PO Template first, then the resource
						creator.CreatePOTemplateControl();
						if (filterResource)
							creator.POLineLaborTemplateResourceFilter();
						creator.POLineLaborTemplateAssignment(ToOrderId);
					}
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.PurchaseOrderText, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.HandleSuggestedQuantityAndBuildQuantityControl("Use remaining demand as quantity to order", ToOrderId, "Order Time");

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingAndLaborResourcesGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.LineNumberRank),
							BTbl.ListColumn(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineLaborTemplate.F.Quantity, NonPerViewColumn)
						)
					);
				});
		}
		#endregion
		#region POLineOtherWorkTemplate
		static DelayedCreateTbl POLineOtherWorkTemplateTbl(bool filterPO, bool filterResource)
		{
			return new DelayedCreateTbl(
				delegate() {
					POLineTemplateDerivationTblCreator<long> creator = new POLineTemplateDerivationTblCreator<long>(TId.PurchaseTemplatePerJobOutside, dsMB.Schema.T.POLineOtherWorkTemplate, TIGeneralMB3.PerJobUnitCostTypeOnClient);
					creator.CreateItemNumberControl();
					if (filterPO)
					{
						// if filtering the PO, we expect to pick the resource first.
						System.Diagnostics.Debug.Assert(!filterResource);
						creator.POLineOtherWorkTemplateAssignment(ToOrderId);
						creator.POLineOtherWorkTemplatePOTemplateFilter();
						creator.CreatePOTemplateControl();
					}
					else
					{
						// otherwise, we pick the PO Template first, then the resource
						creator.CreatePOTemplateControl();
						if (filterResource)
							creator.POLineOtherWorkTemplateResourceFilter();
						creator.POLineOtherWorkTemplateAssignment(ToOrderId);
					}
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.PurchaseOrderText, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.HandleSuggestedQuantityAndBuildQuantityControl("Use remaining demand as quantity to order", ToOrderId, "Order Quantity");
					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingAndLaborResourcesGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.LineNumberRank),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineOtherWorkTemplate.F.Quantity, NonPerViewColumn)
						)
					);
				});
		}
		#endregion
		#region POLineMiscellaneousTemplate
		static DelayedCreateTbl POLineMiscellaneousTemplateTbl(bool filterPO, bool filterResource)
		{
			return new DelayedCreateTbl(
				delegate()
				{
					POLineTemplateDerivationTblCreator<long> creator = new POLineTemplateDerivationTblCreator<long>(TId.PurchaseTemplateMiscellaneousItem, dsMB.Schema.T.POLineMiscellaneousTemplate, null);
					creator.CreateItemNumberControl();
					if (filterPO)
					{
						// A - Work Vendor association - DefaultVisibility defined, Miscellaneous item definitions are not vendor-specific.
						// B - Vendor history:
						// - only POs for vendors that have previously been done the work
						// - No filtering based on history
						object BId = creator.AddPickerFilterControl(KB.K("Only include Purchase Order Templates for Vendors who have previously provided this item"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
						// This only occurs for PO purchasing.
						creator.AddPickerFilter(
							BTbl.ExpressionFilter(
								new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
									.In(new SelectSpecification(
										null,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
										new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.MiscellaneousID)
											.Eq(new SqlExpression(dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID, 2)),
										null
									).SetDistinct(true))
								),
							BId,
							null
						);
					}
					creator.CreatePOTemplateControl();

					if (filterResource)
					{
						// A - Work vendor association - DefaultVisibility defined, Miscellaneous item definitions are not vendor-specific.
						// B - Vendor history:
						// - only demands for work that has previously been received
						// - No filtering based on history
						object BId = creator.AddPickerFilterControl(KB.K("Only include Miscellaneous items previously provided by this vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
						// This only occurs for PO purchasing.
						creator.AddPickerFilter(
							BTbl.ExpressionFilter(
								new SqlExpression(dsMB.Path.T.Miscellaneous.F.Id)
									.In(new SelectSpecification(
										null,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.MiscellaneousID) },
										new SqlExpression(dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID)
											.Eq(new SqlExpression(dsMB.Path.T.POLineMiscellaneousTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
										null
									).SetDistinct(true))
								),
							BId,
							null
						);

						// C - quantity status - There is no associated Demand for doing Quantity filtering.
					}
					creator.AddPickerPanelDisplay(dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID.F.Code);
					creator.CreateResourceControl(dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID);

					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID.F.Cost, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredUnitCostId))));
					creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID.F.PurchaseOrderText, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PrototypeDescriptionId))));
					creator.BuildQuantityControl("Order Quantity");

					// TODO: Make creator do many of the list columns.
					return creator.GetTbl(
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneousTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneousTemplate.F.POLineTemplateID.F.LineNumberRank),
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.POLineMiscellaneousTemplate.F.Quantity, NonPerViewColumn)
						)
					);
				});
		}
		#endregion

		#region PurchaseOrderAssignment
		private static DelayedCreateTbl PurchaseOrderAssignmentBrowseTbl(bool fixedPurchaseOrder) {
			return new DelayedCreateTbl(
				delegate() {
					List<BTbl.ICtorArg> BTblAttrs = new List<BTbl.ICtorArg>();
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.Number));
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.Code));
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.Subject));
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.F.Code));

					return new CompositeTbl(dsMB.Schema.T.PurchaseOrderAssignment, TId.PurchaseOrderAssignment,
						new Tbl.IAttr[] {
							PurchasingGroup,
							new BTbl(BTblAttrs.ToArray())
						},
						null,
						new CompositeView(PurchaseOrderAssignmentEditTbl(fixedPurchaseOrder), dsMB.Path.T.PurchaseOrderAssignment.F.Id,
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AdditionalViewVerb(KB.K("View Purchase Order"),  KB.K("View the assigned Purchase Order"), null, dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID, null, null),
							CompositeView.AdditionalViewVerb(KB.K("View Assignee"),  KB.K("View the Purchase Order Assignee"), null, dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID, null, null)
						)
					);
				}
			);
		}
		private static DelayedCreateTbl PurchaseOrderAssignmentEditTbl(bool fixedPurchaseOrder) {
			var creator = new AssignmentDerivationTblCreator(TId.PurchaseOrderAssignment, dsMB.Schema.T.PurchaseOrderAssignment);
			if (!fixedPurchaseOrder) {
				// TODO: This should be 3 checkboxes instead.
				EnumValueTextRepresentations requestState = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only show Draft Purchase Orders"),
								KB.K("Only show Issued Purchase Orders"),
								KB.K("Only show Closed Purchase Orders"),
								KB.K("Show all Purchase Orders")
							},
					null,
					new object[] { 0, 1, 2, 3, 4 }
				);
				object POFilterChoiceId = creator.AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 4),
					Fmt.SetEnumText(requestState),
					Fmt.SetIsSetting(0)
				);

				creator.AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsDraft)),
					POFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 0)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsIssued)),
					POFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 1)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsClosed)),
					POFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 2)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(SqlExpression.Constant(true)),
					POFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 3)
				);
			}
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.Number);
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.Code);
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.Subject);
			creator.CreateBoundPickerControl(KB.I("PurchaseOrderPickerId"), dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID);

			if (fixedPurchaseOrder) {
				EnumValueTextRepresentations assigneeCriteria = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Show prospects for Purchase Order Assignee for this Purchase Order"),
								KB.K("Show all Assignees not currently assigned to this Purchase Order")
							},
					null,
					new object[] { 0, 1 }
				);

				object assigneeFilterChoiceId = creator.AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 1),
					Fmt.SetEnumText(assigneeCriteria),
					Fmt.SetIsSetting(0)
				);

				// Probable assignees based on whether demands for this workorder include any workorder assignees, the requestor for this workorder, the logged in user, or
				// the Unit's contact id matches a workorder assignee contact.
				creator.AddPickerFilter(BTbl.ExpressionFilter(
																new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID)
																	.In(new SelectSpecification(
																		null,
																		new SqlExpression[] {
																			new SqlExpression(dsMB.Path.T.PurchaseOrderAssigneeProspect.F.ContactID),
																		},
																		new SqlExpression(dsMB.Path.T.PurchaseOrderAssigneeProspect.F.PurchaseOrderID)
																			.Eq(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID, 2)	// outer scope 2 refers to the edit buffer contents
																		),
																		null).SetDistinct(true))
																.Or(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.L.User.ContactID.F.Id)
																	.Eq(new SqlExpression(new UserIDSource())))),
					assigneeFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 0));
				// Build an expression that finds all assignees not current associated with the PurchaseOrder associated with this PurchaseOrder Assignment
				// select ID from PurchaseOrderAssignee 
				// where ID NOT IN (select PurchaseOrderAssigneeID from PurchaseOrderAssignment where PurchaseOrderID = <ID of current PurchaseOrder associated with PurchaseOrderAssignment>)
				creator.AddPickerFilter(BTbl.ExpressionFilter(
																new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.Id)
																	.In(new SelectSpecification(
																		null,
																		new SqlExpression[] { new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID) },
																		new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID, 2)),	// outer scope 2 refers to the edit buffer contents
																		null).SetDistinct(true)).Not()),
					assigneeFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 1));
			}
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.F.Code);
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.F.BusinessPhone);
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.F.MobilePhone);
			creator.AddPickerPanelDisplay(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.F.Email);
			// Label of PickerControl must be same as the underlying Tbl identification of for PurchaseOrderAssignee as the Tbl identification will
			// be used in any Unique violations to identify the control on the screen (since the actual picker control will have a 'null' label
			creator.CreateBoundPickerControl(KB.I("PurchaseOrderAssigneePickerId"), dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID);

			return new DelayedCreateTbl(
				delegate() {
					return creator.GetTbl(PurchasingGroup);
				}
			);
		}
		#endregion
		#endregion

		private TIPurchaseOrder() {}
		static TIPurchaseOrder() {
			#region TblCreators
			POLineItemTblCreator = new DelayedCreateTbl(delegate () { return POLineItemsBrowseTbl(); });
			POLineItemTemplateTblCreator = new DelayedCreateTbl(delegate () { return POLineItemsTemplateTbl(); });
			AssociatedWorkOrdersTbl = TIWorkOrder.WorkOrderPurchaseOrderLinkageTbl(false);
			AssociatedWorkOrderTemplatesTbl = TIWorkOrder.WorkOrderTemplatePurchaseOrderTemplateLinkageTbl(false);

			POLineItemWithPOInitTblCreator = POLineItemTbl(false, true, false);
			POLineItemWithItemLocationInitTblCreator = POLineItemTbl(true, false, false);
			POLineLaborWithPOInitTblCreator = POLineLaborTbl(false, true, false);
			POLineLaborWithDemandInitTblCreator = POLineLaborTbl(true, false, false);
			POLineOtherWorkWithPOInitTblCreator = POLineOtherWorkTbl(false, true, false);
			POLineOtherWorkWithDemandInitTblCreator = POLineOtherWorkTbl(true, false, false);
			POLineMiscellaneousWithPOInitTblCreator = POLineMiscellaneousTbl(false, true, false);
			//POLineMiscellaneousWithMiscellaneousInitTblCreator = POLineMiscellaneousTbl(true, false, false);

			POLineItemTemplateWithPOTemplateInitTblCreator = POLineItemTemplateTbl(false, true);
			POLineItemTemplateWithItemLocationInitTblCreator = POLineItemTemplateTbl(true, false);
			POLineLaborTemplateWithPOTemplateInitTblCreator = POLineLaborTemplateTbl(false, true);
			POLineLaborTemplateWithDemandTemplateInitTblCreator = POLineLaborTemplateTbl(true, false);
			POLineOtherWorkTemplateWithPOTemplateInitTblCreator = POLineOtherWorkTemplateTbl(false, true);
			POLineOtherWorkTemplateWithDemandTemplateInitTblCreator = POLineOtherWorkTemplateTbl(true, false);
			POLineMiscellaneousTemplateWithPOTemplateInitTblCreator = POLineMiscellaneousTemplateTbl(false, true);
			//POLineMiscellaneousTemplateWithMiscellaneousInitTblCreator = POLineMiscellaneousTemplateTbl(true, false);
			#endregion
			#region PurchaseOrderAssignee
			DefineTbl(dsMB.Schema.T.PurchaseOrderAssignee, delegate () {
				return new Tbl(dsMB.Schema.T.PurchaseOrderAssignee, TId.PurchaseOrderAssignee,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.F.BusinessPhone, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.F.MobilePhone, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignee.F.Id.L.PurchaseOrderAssigneeStatistics.Id.F.NumNew, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignee.F.Id.L.PurchaseOrderAssigneeStatistics.Id.F.NumInProgress, NonPerViewColumn)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete)),
					TIReports.NewRemotePTbl(TIReports.PurchaseOrderAssigneeReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						SingleContactGroup(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderAssignee.F.ReceiveNotification, new FeatureGroupArg(MainBossServiceAsWindowsServiceGroup), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderAssignee.F.Comment, DCol.Normal, ECol.Normal)
					),
					TblTabNode.New(KB.TOc(TId.PurchaseOrderAssignment), KB.K("Purchase Order Assignments for this Assignee"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.NewBrowsette(PurchaseOrderAssignmentBrowseTbl(false), dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.PurchaseOrderAssignee, dsMB.Schema.T.PurchaseOrderAssignee);
			#endregion
			#region PurchaseOrderAssignmentByAssignee
			PurchaseOrderAssignmentByAssigneeTblCreator = new DelayedCreateTbl(delegate () {
				object assignedToColumnID = KB.I("codeName");
				DelayedCreateTbl assignedToGroup = new DelayedCreateTbl(
					delegate () {
						return new Tbl(dsMB.Schema.T.PurchaseOrderAssignmentByAssignee, TId.AssignedGroup,
							new Tbl.IAttr[] {
								PurchasingAssignmentsGroup,
							},
							new TblLayoutNodeArray(
								TblColumnNode.New(null, dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.Id, DCol.Normal)
							)
						);
					}
				);
				Key newAssignmentKey = TId.PurchaseOrderAssignment.ComposeCommand("New {0}");
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrderAssignmentByAssignee, TId.PurchaseOrderAssignmentByAssignee,
					new Tbl.IAttr[] {
						PurchasingGroup,
						new BTbl(
							BTbl.PerViewListColumn(KB.K("Assigned to"), assignedToColumnID),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID.F.Subject)
						),
						new TreeStructuredTbl(null, 2),
						TIReports.NewRemotePTbl(TIReports.PurchaseOrderByAssigneeFormReport),
					},
					null,
					// The fake contact row for unassigned work orders; This displays because the XAFDB file specifies a text provider for its own ID field.
					new CompositeView(assignedToGroup, dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.Id, ReadonlyView,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.ContactID).IsNull()),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.Id)
					),
					// Normal contact row for an assignee
					// TODO: The view should return the AssigneeID rather than the ContactID in all row types so we don't need this .L. linkage
					// TODO: We should then have extra verbs for direct Edit/View contact so the user doesn't have to drill into the assignee record.
					new CompositeView(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.ContactID.L.PurchaseOrderAssignee.ContactID,
						NoNewMode,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID).IsNull()),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.F.Code)
					),
					// TODO: These views show the PO in the panel. If an explicit assignment exists (PurchaseOrderAssignmentID != null) there should be a way to delete it.
					// (un)assigned draft PO
					new CompositeView(PurchaseOrderEditTblCreator, dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID).IsNotNull()
									.And(SqlExpression.Constant(KnownIds.PurchaseOrderStateDraftId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID)))),
						CompositeView.SetParentPath(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.ContactID),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.PurchaseOrder.F.Number),
						CompositeView.IdentificationOverride(TId.DraftPurchaseOrder)
					),
					// (un)assigned open PO
					new CompositeView(PurchaseOrderEditTblCreator, dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID).IsNotNull()
									.And(SqlExpression.Constant(KnownIds.PurchaseOrderStateIssuedId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID)))),
						CompositeView.SetParentPath(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.ContactID),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.PurchaseOrder.F.Number),
						CompositeView.UseSamePanelAs(2),
						CompositeView.IdentificationOverride(TId.IssuedPurchaseOrder)
					),
					// Allow creation of new assignments if a PO is selected.
					CompositeView.ExtraNewVerb(PurchaseOrderAssignmentEditTbl(true),
						CompositeView.JoinedNewCommand(newAssignmentKey),
						NoContextFreeNew,
						CompositeView.ContextualInit(
							new int[] { 2, 3 }, // corresponds to the composite views above on PurchaseOrders
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID), dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID)
						)
					),
					// Allow creation of new assignments if an assignee is selected.
					CompositeView.ExtraNewVerb(PurchaseOrderAssignmentEditTbl(false),
						CompositeView.JoinedNewCommand(newAssignmentKey),
						NoContextFreeNew,
						CompositeView.ContextualInit(
							new int[] { 1 }, // corresponds to the composite view above on assignees
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID), dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.ContactID.L.PurchaseOrderAssignee.ContactID.F.Id)
						)
					)
				);
			}
			);
			#endregion
			POLineItemDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return POLineItemTbl(false, false, true);
			});
			POLineLaborDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return POLineLaborTbl(false, false, true);
			});
			POLineOtherWorkDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return POLineOtherWorkTbl(false, false, true);
			});
			POLineMiscellaneousTblCreator = new DelayedCreateTbl(delegate () {
				return POLineMiscellaneousTbl(false, false, true);
			});
		}

		internal static void DefineTblEntries()
		{
			#region	PaymentTerm
			DefineTbl(dsMB.Schema.T.PaymentTerm, delegate() {
				return new Tbl(dsMB.Schema.T.PaymentTerm, TId.PaymentTerm,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.PaymentTerm.F.Code), BTbl.ListColumn(dsMB.Path.T.PaymentTerm.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.PaymentTerm.F.Code, DCol.Normal, ECol.Normal ),
						TblColumnNode.New(dsMB.Path.T.PaymentTerm.F.Desc, DCol.Normal, ECol.Normal ),
						TblColumnNode.New(dsMB.Path.T.PaymentTerm.F.Comment, DCol.Normal, ECol.Normal )),
					BrowsetteTabNode.New(TId.PurchaseOrder, TId.PaymentTerm,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.PaymentTermID, DCol.Normal, ECol.Normal) )
				));
			});
			RegisterExistingForImportExport(TId.PaymentTerm, dsMB.Schema.T.PaymentTerm);
			#endregion

			#region PurchaseOrderLine for Item Receiving which include the receiving lines as well.
			{
				Key receiveItemGroup = KB.K("Receive");
				Key joinedCorrectionsCommand = KB.K("Correct");
				// This condition can be applied to any ActualLabor or ActualOtherWork record
				CompositeView.Condition[] DemandMustHaveValidCategory = new CompositeView.Condition[] {
					new CompositeView.Condition(
						new SqlExpression(dsMB.Path.T.PurchaseOrderLine.F.WorkOrderExpenseModelEntryID).IsNotNull(),
						KB.K("The Demand associated with this purchase line item has an Expense Category that is not valid for the Work Order's Expense Model")
					)
				};
				// For the Purchase Order editor's Lines browsette
				Key costColumnId = dsMB.Path.T.POLineItem.F.POLineID.F.Cost.Key();
				Key uomColumnId = dsMB.Path.T.Item.F.UnitOfMeasureID.Key();
				Key quantityColumnId = dsMB.Path.T.POLineItem.F.Quantity.Key();
				DefineBrowseTbl(dsMB.Schema.T.PurchaseOrderLine, delegate()
				{
					return new CompositeTbl(dsMB.Schema.T.PurchaseOrderLine, TId.PurchaseOrderLine,
					new Tbl.IAttr[]
					{
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.PurchaseOrderText),
							BTbl.PerViewListColumn(quantityColumnId, quantityColumnId),
							BTbl.PerViewListColumn(uomColumnId, uomColumnId),
							BTbl.PerViewListColumn(costColumnId, costColumnId)
						),
						new TreeStructuredTbl(dsMB.Path.T.PurchaseOrderLine.F.ParentID, 2)
					},
					dsMB.Path.T.PurchaseOrderLine.F.TableEnum,
						// TODO: This browser tries to but fails to allow receiving (actualization) of labor & other work because it does not supply an init for the To C/C;
						// the data is nota available in the underlying db view either. The ReceiptActivity view and Tbl do this properly.
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineItemID,									// ViewRecordTypes.PurchaseOrderLine.POLineItem
						ReadonlyView,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineItem.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineItem.F.POLineID.F.Cost),
						BTbl.PerViewColumnValue(uomColumnId, dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code),
						CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID)),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveItemPOID,				// ViewRecordTypes.PurchaseOrderLine.ReceiveItemPO
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveItemPO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(receiveItemGroup),
						CompositeView.ContextualInit((int)ViewRecordTypes.PurchaseOrderLine.POLineItem,
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveItemPO.F.POLineItemID), dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineItemID))
					),
					new CompositeView(TIReceive.ReceiveItemPOCorrectionTblCreator, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveItemPOID,	// ViewRecordTypes.PurchaseOrderLine.ReceiveItemPOCorrection
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveItemPO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
						CompositeView.ContextualInit(new int[] {
								(int)ViewRecordTypes.PurchaseOrderLine.ReceiveItemPO,
								(int)ViewRecordTypes.PurchaseOrderLine.ReceiveItemPOCorrection
							},
							new CompositeView.Init(dsMB.Path.T.ReceiveItemPO.F.CorrectionID, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectionID),
							// The automatic dead end disabling does not realize that the correction editor automatically copies several fields including the POLineItemID from the corrected record to
							// the new record and sets the POLine picker control readonly thus causing the dead end situtation if the PO is in the wrong state.
							// The following init also sets the picker readonly and allows the browser to know the value in the picker and thus eliminate the dead end.
							// In the long run, the init isn't really necessary but we need to have some other CompositeView attribute to trigger the dead end.
							// It could be coded as a condition expression here but that requires duplication of the tip already defined in the DBI_WriteRestriction
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveItemPO.F.POLineItemID), dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveItemPOID.F.POLineItemID) 		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						)
					),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineLaborID,									// ViewRecordTypes.PurchaseOrderLine.POLineLabor
						ReadonlyView,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineLabor.F.Quantity, IntervalFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineLabor.F.POLineID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID)),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualLaborOutsidePOID,			// ViewRecordTypes.PurchaseOrderLine.ActualLaborOutsidePO
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.Quantity, IntervalFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(receiveItemGroup),
						CompositeView.ContextualInit((int)ViewRecordTypes.PurchaseOrderLine.POLineLabor,
							DemandMustHaveValidCategory,
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineLaborID),
							new CompositeView.Init(dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.ToCostCenterID, dsMB.Path.T.PurchaseOrderLine.F.WorkOrderExpenseModelEntryID.F.CostCenterID)
							)
					),
					new CompositeView(TIWorkOrder.ActualLaborOutsidePOCorrectionTblCreator, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualLaborOutsidePOID,	// ViewRecordTypes.PurchaseOrderLine.ActualLaborOutsidePOCorrection
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.Quantity, IntervalFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
						CompositeView.ContextualInit(new int[] {
								(int)ViewRecordTypes.PurchaseOrderLine.ActualLaborOutsidePO,
								(int)ViewRecordTypes.PurchaseOrderLine.ActualLaborOutsidePOCorrection
							},
							new CompositeView.Init(dsMB.Path.T.ActualLaborOutsidePO.F.CorrectionID, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.POLineLaborID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						)
					),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineOtherWorkID,								// ViewRecordTypes.PurchaseOrderLine.POLineOtherWork
						ReadonlyView,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineOtherWork.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineOtherWork.F.POLineID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID)),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID,		// ViewRecordTypes.PurchaseOrderLine.ActualOtherWorkOutsidePO
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(receiveItemGroup),
						CompositeView.ContextualInit((int)ViewRecordTypes.PurchaseOrderLine.POLineOtherWork,
							DemandMustHaveValidCategory,
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineOtherWorkID),
							new CompositeView.Init(dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.ToCostCenterID, dsMB.Path.T.PurchaseOrderLine.F.WorkOrderExpenseModelEntryID.F.CostCenterID)
							)
					),
					new CompositeView(TIWorkOrder.ActualOtherWorkOutsidePOCorrectionTblCreator, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID,	// ViewRecordTypes.PurchaseOrderLine.ActualOtherWorkOutsidePOCorrection
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
						CompositeView.ContextualInit(new int[] {
								(int)ViewRecordTypes.PurchaseOrderLine.ActualOtherWorkOutsidePO,
								(int)ViewRecordTypes.PurchaseOrderLine.ActualOtherWorkOutsidePOCorrection
							},
							new CompositeView.Init(dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectionID, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						)
					),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineMiscellaneousID,							// ViewRecordTypes.PurchaseOrderLine.POLineMiscellaneous
						ReadonlyView,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineMiscellaneous.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderID)),
					new CompositeView(dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID,		// ViewRecordTypes.PurchaseOrderLine.ReceiveMiscellaneousPO
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(receiveItemGroup),
						CompositeView.ContextualInit((int)ViewRecordTypes.PurchaseOrderLine.POLineMiscellaneous,
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID), dsMB.Path.T.PurchaseOrderLine.F.POLineID.F.POLineMiscellaneousID))
					),
					new CompositeView(TIReceive.ReceiveMiscellaneousPOCorrectionTblCreator, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID,		// ViewRecordTypes.PurchaseOrderLine.ReceiveMiscellaneousPOCorrection
						NoNewMode,
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.Quantity, IntegralFormat),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.AccountingTransactionID.F.Cost),
						CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
						CompositeView.ContextualInit(new int[] {
								(int)ViewRecordTypes.PurchaseOrderLine.ReceiveMiscellaneousPO,
								(int)ViewRecordTypes.PurchaseOrderLine.ReceiveMiscellaneousPOCorrection
							},
							new CompositeView.Init(dsMB.Path.T.ReceiveMiscellaneousPO.F.CorrectionID, dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID), dsMB.Path.T.PurchaseOrderLine.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID.F.POLineMiscellaneousID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						)
					)
				);
				});
			}
			#endregion
			#region PurchaseOrderState
			DefineTbl(dsMB.Schema.T.PurchaseOrderState, delegate()
			{
				return new Tbl(dsMB.Schema.T.PurchaseOrderState, TId.PurchaseOrderState,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderState.F.Code), BTbl.ListColumn(dsMB.Path.T.PurchaseOrderState.F.Desc)),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.Comment, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.OrderCountsActive, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.CanModifyOrder, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.CanModifyReceiving, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.CanHaveReceiving, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.FilterAsDraft, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.FilterAsIssued, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.FilterAsClosed, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderState.F.FilterAsVoid, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.PurchaseOrder, TId.PurchaseOrderState,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion
			#region PurchaseOrderStateHistoryStatus
			DefineTbl(dsMB.Schema.T.PurchaseOrderStateHistoryStatus, delegate()
			{
				return new Tbl(dsMB.Schema.T.PurchaseOrderStateHistoryStatus, TId.PurchaseOrderStatus,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistoryStatus.F.Code), BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistoryStatus.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistoryStatus.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistoryStatus.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistoryStatus.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PurchaseOrder, TId.PurchaseOrderStatus,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateHistoryStatusID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion
			#region PurchaseOrderStateHistory
			DefineEditTbl(dsMB.Schema.T.PurchaseOrderStateHistory, new DelayedCreateTbl(delegate () {
				TblLayoutNodeArray extraNodes;
				List<TblActionNode> inits = StateHistoryInits(MB3Client.PurchaseOrderHistoryTable, out extraNodes);
				TblLayoutNodeArray PurchaseOrderStateHistoryNodes = new TblLayoutNodeArray(
					TblGroupNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.Number, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.Code, new ECol(ECol.AllReadonlyAccess, ECol.ForceErrorsFatal(), Fmt.SetId(TIGeneralMB3.CurrentStateHistoryCodeWhenLoadedId))),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateHistoryStatusID.F.Code, ECol.AllReadonly)
					),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.EffectiveDate, DCol.Normal, new ECol(Fmt.SetId(TIGeneralMB3.StateHistoryEffectiveDateId)), new NonDefaultCol()),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.EntryDate, new NonDefaultCol(), DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.UserID.F.ContactID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrderStateHistoryStatus.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrderStateHistory.F.Comment, DCol.Normal, ECol.Normal)
				);
				return new Tbl(dsMB.Schema.T.PurchaseOrderStateHistory, TId.PurchaseOrderStateHistory,
					new Tbl.IAttr[] {
						PurchasingGroup,
						new ETbl(
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View, EdtMode.EditDefault, EdtMode.ViewDefault),
							ETbl.NewOnlyOnce(true),
							ETbl.UseNewConcurrency(true),
							ETbl.AllowConcurrencyErrorOverride(false),
							ETbl.RowEditType(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.Id, 0, RecordManager.RowInfo.RowEditTypes.EditOnly),
							MB3ETbl.IsStateHistoryTbl(PurchaseOrderHistoryTable)
						)
					},
					PurchaseOrderStateHistoryNodes + extraNodes,
					inits.ToArray()
				);
			}));
			DefineBrowseTbl(dsMB.Schema.T.PurchaseOrderStateHistory, new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrderStateHistory, TId.PurchaseOrderStateHistory,
					new Tbl.IAttr[] {
						PurchasingGroup,
						new BTbl(
							MB3BTbl.IsStateHistoryTbl(PurchaseOrderHistoryTable),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistory.F.EffectiveDate, BTbl.ListColumnArg.Contexts.SortInitialDescending),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderStateHistory.F.Comment)
						)
					},
					null,
					CompositeView.ChangeEditTbl(FindDelayedEditTbl(dsMB.Schema.T.PurchaseOrderStateHistory), NoNewMode)
				);
			}));
			#endregion
			#region PurchaseOrder
			BTbl.ICtorArg PurchaseOrderNumberListColumn = BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.Number);
			BTbl.ICtorArg PurchaseOrderVendorListColumn = BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.VendorID.F.Code);
			BTbl.ICtorArg PurchaseOrderSummaryListColumn = BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.Subject, NonPerViewColumn);
			BTbl.ICtorArg PurchaseOrderStatusListColumn = BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateHistoryStatusID.F.Code);
			BTbl.ICtorArg PurchaseOrderStateAuthorListColumn = BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.UserID.F.ContactID.F.Code, BTbl.ListColumnArg.Contexts.SearchAndFilter);

			TblLayoutNodeArray PurchaseOrderNodes = new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.Number, new NonDefaultCol(), DCol.Normal, ECol.ReadonlyInUpdate),
					TblVariableNode.New(KB.K("Number Format"), dsMB.Schema.V.POSequenceFormat, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Number Sequence"), dsMB.Schema.V.POSequence, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Send Invoice To"), dsMB.Schema.V.POInvoiceContactID, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.Subject, DCol.Normal, ECol.Normal),
					CurrentStateHistoryGroup(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID,
						dsMB.Path.T.PurchaseOrderStateHistory.F.EffectiveDate,
						dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.F.Code,
						dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID.F.Code),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.RequiredByDate, new NonDefaultCol(), DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.PurchaseOrderCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrderCategory.F.Code)), ECol.Normal),
					TblUnboundControlNode.New(UseVendorPaymentTerms, BoolTypeInfo.NonNullUniverse, new ECol(Fmt.SetId(UseVendorPaymentTerms), Fmt.SetIsSetting(false)), new NonDefaultCol()),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.PaymentTermID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PaymentTerm.F.Code)), new ECol(Fmt.SetId(PaymentTerms))),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.ShippingModeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ShippingMode.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.ShipToLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.ProjectID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Project.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.CommentToVendor, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.Comment, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.SelectPrintFlag, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.PurchaseOrderLine, TId.PurchaseOrder, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblContainerNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal, new AddCostCol(CommonNodeAttrs.ViewTotalPurchaseOrderCosts) },
						// This is to avoid squeezing the browsette into the 'control' column defined by this control in the LCP.
						TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.TotalPurchase, new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInBrowsetteArea)), ECol.AllReadonly)
					),
					TblColumnNode.NewBrowsette(POLineItemTblCreator, dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.PurchaseOrderAssignment, TId.PurchaseOrder, 
					TblColumnNode.NewBrowsette(PurchaseOrderAssignmentBrowseTbl(true), dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.ItemReceiving, TId.PurchaseOrder, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblContainerNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal, new AddCostCol(CommonNodeAttrs.ViewTotalPurchaseOrderCosts) },
						// This is to avoid squeezing the browsette into the 'control' column defined by this control in the LCP.
						TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.TotalReceive, new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInBrowsetteArea)), ECol.AllReadonly)
					),
					TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrderLine.F.PurchaseOrderID, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.Receipt, TId.PurchaseOrder, 
					TblColumnNode.NewBrowsette(dsMB.Path.T.Receipt.F.PurchaseOrderID, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.WorkOrder, TId.PurchaseOrder,
					TblColumnNode.NewBrowsette(AssociatedWorkOrdersTbl, dsMB.Path.T.WorkOrderPurchaseOrderView.F.LinkedPurchaseOrderID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.PurchaseOrderStateHistory, TId.PurchaseOrder, 
					TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, DCol.Normal, ECol.Normal)
				),
				TblTabNode.New(KB.K("Printed Form"), KB.K("Display settings for printed purchase orders"), new TblLayoutNode.ICtorArg[] { new DefaultOnlyCol(), DCol.Normal, ECol.Normal },
					TblVariableNode.New(KB.K("Title"), dsMB.Schema.V.POFormTitle, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Additional Lines"), dsMB.Schema.V.POFormAdditionalBlankLines, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Additional Information"), dsMB.Schema.V.POFormAdditionalInformation, new DefaultOnlyCol(), DCol.Normal, ECol.Normal)
				)
			);
			#region - PurchaseOrderEditTblCreator
			PurchaseOrderEditTblCreator = PurchaseOrderEditTbl(PurchaseOrderNodes, PurchasingGroup, false);
			DefineEditTbl(dsMB.Schema.T.PurchaseOrder, PurchaseOrderEditTblCreator);
//			PurchaseOrderEditorByAssigneeTblCreator = PurchaseOrderEditTbl(PurchaseOrderNodes, PurchaseOrdersGroup, false);
			DelayedCreateTbl PurchaseOrderUnassignedEditorTblCreator = PurchaseOrderEditTbl(PurchaseOrderNodes, PurchasingAssignmentsGroup, true);
			DelayedCreateTbl PurchaseOrderAssignedToEditorTblCreator = PurchaseOrderEditTbl(PurchaseOrderNodes, PurchasingAssignmentsGroup, false);

			// This is a PO editor which also has a third recordset to create a POLineItem in New mode, and hidden controls to accept init's for pricing basis information.
			// This is called from the ItemRestocking browser with inits for all the fields of the POLineItem as well as the pricing basis information.
			PurchaseOrderWithPOLineItemEditTbl = new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.PurchaseOrder, TId.PurchaseOrder,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingGroup,
						new ETbl(
							ETbl.NewOnlyOnce(true),
							MB3ETbl.HasStateHistoryAndSequenceCounter(dsMB.Path.T.PurchaseOrder.F.Number, dsMB.Schema.T.PurchaseOrderSequenceCounter, dsMB.Schema.V.POSequence, dsMB.Schema.V.POSequenceFormat, PurchaseOrderHistoryTable),
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.Edit, EdtMode.View, EdtMode.Clone, EdtMode.EditDefault, EdtMode.ViewDefault, EdtMode.New),
							ETbl.Print(TIReports.SinglePurchaseOrderFormReport, dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID)
						),
						TIReports.NewRemotePTbl(TIReports.PurchaseOrderFormReport)
					},
					PurchaseOrderNodes + new TblLayoutNodeArray(
						TblUnboundControlNode.StoredEditorValue(POLinePricingBasisQuantity, dsMB.Schema.T.POLineItem.F.Quantity.EffectiveType),
						TblUnboundControlNode.StoredEditorValue(POLinePricingBasisCost, dsMB.Schema.T.POLine.F.Cost.EffectiveType),
						TblColumnNode.New(dsMB.Path.T.POLineItem.F.Quantity, 2, new ECol(ECol.HiddenAccess, Fmt.SetId(POLineTotalQuantity))),
						TblColumnNode.New(dsMB.Path.T.POLineItem.F.POLineID.F.Cost, 2, new ECol(ECol.HiddenAccess, Fmt.SetId(POLineTotalCost)))
					),
					Init.LinkRecordSets(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, 1, dsMB.Path.T.PurchaseOrder.F.Id, 0),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PurchaseOrderStateHistory.F.UserID, 1), new UserIDValue()),
					Init.LinkRecordSets(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID, 2, dsMB.Path.T.PurchaseOrder.F.Id, 0),
					TIGeneralMB3.TotalFromQuantityAndPricingCalculator<long>(POLineTotalQuantity, POLinePricingBasisQuantity, POLinePricingBasisCost, POLineTotalCost)
				);
			});
			#endregion

			// The PurchaseOrder browser is made Composite only to allow specification of an alternative Tbl for editing and provide child record Default editors
			DefineBrowseTbl(dsMB.Schema.T.PurchaseOrder, delegate() {
				Key NewPurchaseOrder = TId.PurchaseOrder.ComposeCommand("New {0}");
				Key GroupDefaultPOLine = TId.PurchaseOrderLine.Compose(Tbl.TblIdentification.TablePhrase_DefaultsFor);
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.PurchaseOrder,
				new Tbl.IAttr[] {
					new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							PurchaseOrderNumberListColumn,
							PurchaseOrderVendorListColumn,
							PurchaseOrderStatusListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() {return TIReports.PurchaseOrderFormReport;}))
				},
				null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, CompositeView.JoinedNewCommand(NewPurchaseOrder),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.PurchaseOrderStateDraftId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID))),
						CompositeView.IdentificationOverride(TId.DraftPurchaseOrder)),
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, OnlyViewEdit, CompositeView.JoinedNewCommand(NewPurchaseOrder),
						CompositeView.UseSamePanelAs(0), 
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.PurchaseOrderStateIssuedId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID))),
						CompositeView.IdentificationOverride(TId.IssuedPurchaseOrder)),
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, OnlyViewEdit, CompositeView.JoinedNewCommand(NewPurchaseOrder),
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.PurchaseOrderStateClosedId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID))),
						CompositeView.IdentificationOverride(TId.ClosedPurchaseOrder)),
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, OnlyViewEdit, CompositeView.JoinedNewCommand(NewPurchaseOrder),
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.PurchaseOrderStateVoidId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID))),
						CompositeView.IdentificationOverride(TId.VoidPurchaseOrder)),
					CompositeView.AdditionalEditDefault(POLineItemDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultPOLine)),
					CompositeView.AdditionalEditDefault(POLineLaborDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultPOLine)),
					CompositeView.AdditionalEditDefault(POLineOtherWorkDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultPOLine)),
					CompositeView.AdditionalEditDefault(POLineMiscellaneousTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultPOLine)),
					CompositeView.AdditionalEditDefault(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.Receipt)),
					CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.PurchaseOrderStateHistory))
			);
			});
			#region PurchaseOrderDraftBrowseTbl
			PurchaseOrderDraftBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.DraftPurchaseOrder,
					new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							//BTbl.EqFilter(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsOpen, true),
							// We use ExpressionFilter instead since it does not turn into an Init directive in the new-mode editor.
							// DefaultVisibility of the other filtered Purchase Order browsers allow New mode.
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsDraft).IsTrue()),
							PurchaseOrderNumberListColumn,
							PurchaseOrderVendorListColumn,
							PurchaseOrderStatusListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() {return TIReports.PurchaseOrderDraftFormReport;}))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator)
				);
			});
			#endregion
			#region PurchaseOrderIssuedBrowseTbl
			PurchaseOrderIssuedBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.IssuedPurchaseOrder,
					new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsIssued, true),
							PurchaseOrderNumberListColumn,
							PurchaseOrderVendorListColumn,
							PurchaseOrderStatusListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() {return TIReports.PurchaseOrderIssuedFormReport;}))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, OnlyViewEdit)
				);
			});
			#endregion
			#region PurchaseOrderInProgressAssignedToBrowseTbl
			SqlExpression UserPurchaseOrderAssignmentsInProgress = new SqlExpression(dsMB.Path.T.PurchaseOrder.F.Id)
				.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID) },
							new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))
									.And(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsIssued).IsTrue()),
							null).SetDistinct(true));
			PurchaseOrderInProgressAssignedToBrowseTbl = new DelayedCreateTbl(delegate()
			{
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.IssuedPurchaseOrder,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("AssignedPurchaseOrder"),
						PurchasingAssignmentsGroup,
						new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							BTbl.ExpressionFilter(UserPurchaseOrderAssignmentsInProgress),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.EffectiveDate),
							PurchaseOrderNumberListColumn,
							PurchaseOrderVendorListColumn,
							PurchaseOrderStatusListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() {return TIReports.PurchaseOrderIssuedAndAssignedFormReport; }))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderAssignedToEditorTblCreator, OnlyViewEdit)
				);
			});
			#endregion
			#region UnassignedPurchaseOrdersBrowseTbl
			SqlExpression UnassignedPurchaseOrderAssignments = new SqlExpression(dsMB.Path.T.PurchaseOrder.F.Id)
				// TODO: There are more efficient ways of doing this that do not involve the PurchaseOrderAssignmentByAssignee view.
				// e.g. NOT IN(select distinct PurchaseOrderAssignement.PurchaseOrderID from PurchaseOrderAssignment where PurchaseOrderAssignement.ContactID.Hidden is null)
				// or COUNT(select * from PurchaseOrderAssignment where PurchaseOrderAssignement.ContactID.Hidden is null and PurchaseOrderAssignement.PurchaseOrderID == outer PurchaseOrder.ID) == 0
				.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID) },
							new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.ContactID).Eq(SqlExpression.Constant(KnownIds.UnassignedID))
									.And(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsIssued)
										.Or(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignmentByAssignee.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsDraft))),
							null).SetDistinct(true));
			UnassignedPurchaseOrderBrowseTbl = new DelayedCreateTbl(delegate()
			{
				var assigneeIdExpression = SqlExpression.ScalarSubquery(new SelectSpecification(dsMB.Schema.T.PurchaseOrderAssignee, new[] { new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.Id) }, new SqlExpression(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().UserRecordID)), null));
				var notAssigneeTip = KB.K("You are not registered as a Purchase Order Assignee");
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.UnassignedPurchaseOrder,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("UnassignedPurchaseOrder"),
						PurchasingAssignmentsGroup,
						new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							BTbl.ExpressionFilter(UnassignedPurchaseOrderAssignments),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.EffectiveDate),
							PurchaseOrderNumberListColumn,
							PurchaseOrderVendorListColumn,
							PurchaseOrderStatusListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn,
							BTbl.AdditionalVerb(SelfAssignCommand,
								delegate(BrowseLogic browserLogic) {
									List<IDisablerProperties> disablers = SelfAssignDisablers();
									disablers.Insert(0,browserLogic.NeedSingleSelectionDisabler);
									return new MultiCommandIfAllEnabled(new CallDelegateCommand(SelfAssignTip,
										delegate() {
											SelfAssignmentEditor(browserLogic, browserLogic.BrowserSelectionPositioner.CurrentPosition.Id);
										}),
										disablers.ToArray()
									);
								}
							)
						)
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderUnassignedEditorTblCreator, OnlyViewEdit,
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.PurchaseOrderStateDraftId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID))),
						CompositeView.IdentificationOverride(TId.DraftPurchaseOrder)),
					CompositeView.ChangeEditTbl(PurchaseOrderUnassignedEditorTblCreator, OnlyViewEdit,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.PurchaseOrderStateIssuedId).Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID))),
						CompositeView.IdentificationOverride(TId.IssuedPurchaseOrder))
					);
			});
			#endregion
			#region PurchaseOrderClosedBrowseTbl
			PurchaseOrderClosedBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.ClosedPurchaseOrder,
					new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsClosed, true),
							PurchaseOrderNumberListColumn,
							PurchaseOrderVendorListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() {return TIReports.PurchaseOrderClosedFormReport;}))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, OnlyViewEdit)
				);
			});
			#endregion
			#region PurchaseOrderVoidBrowseTbl
			PurchaseOrderVoidBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrder, TId.VoidPurchaseOrder,
					new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(PurchaseOrderHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsVoid, true),
							PurchaseOrderNumberListColumn,
							PurchaseOrderSummaryListColumn,
							PurchaseOrderStateAuthorListColumn
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() {return TIReports.PurchaseOrderVoidFormReport;}))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(PurchaseOrderEditTblCreator, OnlyViewEdit)
				);
			});
			#endregion

			#endregion
			#region Miscellaneous
			DefineTbl(dsMB.Schema.T.Miscellaneous, delegate() {
				return new Tbl(dsMB.Schema.T.Miscellaneous, TId.MiscellaneousItem,
				new Tbl.IAttr[] {
					CommonTblAttrs.ViewCostsDefinedBySchema,
					PurchasingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.Miscellaneous.F.Code),
						BTbl.ListColumn(dsMB.Path.T.Miscellaneous.F.Desc, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.Miscellaneous.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.OpenCombo|BTbl.ListColumnArg.Contexts.ClosedCombo|BTbl.ListColumnArg.Contexts.SearchAndFilter),
						BTbl.ListColumn(dsMB.Path.T.Miscellaneous.F.Cost, NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.MiscellaneousReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.Miscellaneous.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Miscellaneous.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Miscellaneous.F.PurchaseOrderText, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Miscellaneous.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.Miscellaneous.F.Cost, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Miscellaneous.F.Comment, DCol.Normal, ECol.Normal)
					)
				));
			});
			RegisterExistingForImportExport(TId.MiscellaneousItem, dsMB.Schema.T.Miscellaneous);
			#endregion
			#region POLineItem
			DefineTbl(dsMB.Schema.T.POLineItem, POLineItemTbl(false, false, false));
			#endregion
			#region POLineLabor
			DefineTbl(dsMB.Schema.T.POLineLabor, POLineLaborTbl(false, false, false));
			#endregion
			#region POLineOtherWork
			DefineTbl(dsMB.Schema.T.POLineOtherWork, POLineOtherWorkTbl(false, false, false));
			#endregion
			#region POLineMiscellaneous
			DefineTbl(dsMB.Schema.T.POLineMiscellaneous, POLineMiscellaneousTbl(false, false, false));
			#endregion

			#region PurchaseOrderTemplate
			BTbl.ICtorArg[] PurchaseOrderTemplateListColumns = {
				BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplate.F.Code, NonPerViewColumn),
				BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplate.F.Subject, NonPerViewColumn)
			};
			TblLayoutNodeArray PurchaseOrderTemplateNodes = new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.Subject, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.PurchaseOrderStateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrderState.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.RequiredByInterval, new NonDefaultCol(), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.PurchaseOrderCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrderCategory.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.PaymentTermID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PaymentTerm.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.ShippingModeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ShippingMode.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.ShipToLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.ProjectID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Project.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.CommentToVendor, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.Comment, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplate.F.SelectPrintFlag, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.PurchaseOrderTemplateLine, TId.PurchaseOrderTemplate,
						TblColumnNode.NewBrowsette(POLineItemTemplateTblCreator, dsMB.Path.T.PurchaseOrderTemplateLine.F.POLineTemplateID.F.PurchaseOrderTemplateID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Task, TId.PurchaseOrderTemplate, 
						TblColumnNode.NewBrowsette(AssociatedWorkOrderTemplatesTbl, dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.LinkedPurchaseOrderTemplateID, DCol.Normal, ECol.Normal))
				);
			PurchaseOrderTemplateEditTbl = new DelayedCreateTbl(delegate()
			{
				return new Tbl(dsMB.Schema.T.PurchaseOrderTemplate, TId.PurchaseOrderTemplate,
					new Tbl.IAttr[] {
						SchedulingAndPurchasingGroup,
						new BTbl(PurchaseOrderTemplateListColumns),
						new ETbl(),
						TIReports.NewRemotePTbl(TIReports.PurchaseOrderTemplateReport)
					},
					(TblLayoutNodeArray)PurchaseOrderTemplateNodes.Clone()
				);
			});
			// The PurchaseOrderTemplate browser is made Composite only to allow specification of an alternative Tbl for editing.
			DefineBrowseTbl(dsMB.Schema.T.PurchaseOrderTemplate, delegate()
			{
				return new CompositeTbl(dsMB.Schema.T.PurchaseOrderTemplate, TId.PurchaseOrderTemplate,
				new Tbl.IAttr[] {
					new BTbl(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplate.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplate.F.Desc),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrderTemplate.F.Subject, BTbl.ListColumnArg.Contexts.OpenCombo|BTbl.ListColumnArg.Contexts.SearchAndFilter)
					),
					TIReports.NewRemotePTbl(TIReports.PurchaseOrderTemplateReport)
				},
				null,	// no record type
				CompositeView.ChangeEditTbl(PurchaseOrderTemplateEditTbl)
			);
			});
			#endregion

			#region PurchaseOrderCategory
			DefineTbl(dsMB.Schema.T.PurchaseOrderCategory, delegate () {
				return new Tbl(dsMB.Schema.T.PurchaseOrderCategory, TId.PurchaseOrderCategory,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.PurchaseOrderCategory.F.Code), BTbl.ListColumn(dsMB.Path.T.PurchaseOrderCategory.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderCategory.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderCategory.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PurchaseOrderCategory.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PurchaseOrder, TId.PurchaseOrderCategory,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.PurchaseOrderCategoryID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PurchaseOrderTemplate, TId.PurchaseOrderCategory,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrderTemplate.F.PurchaseOrderCategoryID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.PurchaseOrderCategory, dsMB.Schema.T.PurchaseOrderCategory);
			#endregion

			#region POLineItemTemplate
			DefineTbl(dsMB.Schema.T.POLineItemTemplate, POLineItemTemplateTbl(false, false));
			#endregion
			#region POLineLaborTemplate
			DefineTbl(dsMB.Schema.T.POLineLaborTemplate, POLineLaborTemplateTbl(false,false));
			#endregion
			#region POLineOtherWorkTemplate
			DefineTbl(dsMB.Schema.T.POLineOtherWorkTemplate, POLineOtherWorkTemplateTbl(false, false));
			#endregion
			#region POLineMiscellaneousTemplate
			DefineTbl(dsMB.Schema.T.POLineMiscellaneousTemplate, POLineMiscellaneousTemplateTbl(false, false));
			#endregion
			#region ShippingMode
			DefineTbl(dsMB.Schema.T.ShippingMode, delegate() {
				return new Tbl(dsMB.Schema.T.ShippingMode, TId.ShippingMode,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.ShippingMode.F.Code), BTbl.ListColumn(dsMB.Path.T.ShippingMode.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.ShippingMode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ShippingMode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ShippingMode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PurchaseOrder, TId.ShippingMode, 
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.ShippingModeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.ShippingMode, dsMB.Schema.T.ShippingMode);
			#endregion
			#region Vendor
			BTbl.ICtorArg[] VendorListColumns = {
				BTbl.ListColumn(dsMB.Path.T.Vendor.F.Code),
				BTbl.ListColumn(dsMB.Path.T.Vendor.F.Desc)
			};
			TblLayoutNodeArray vendorNodes = new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsMB.Path.T.Vendor.F.Code, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Vendor.F.Desc, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Vendor.F.VendorCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.VendorCategory.F.Code)), ECol.Normal),
					ContactGroupTblLayoutNode(
						ContactGroupRow(dsMB.Path.T.Vendor.F.SalesContactID, ECol.Normal),
						ContactGroupRow(dsMB.Path.T.Vendor.F.ServiceContactID, ECol.Normal),
						ContactGroupRow(dsMB.Path.T.Vendor.F.PayablesContactID, ECol.Normal)
					),
					TblColumnNode.New(dsMB.Path.T.Vendor.F.AccountsPayableCostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Vendor.F.AccountNumber, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Vendor.F.PaymentTermID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PaymentTerm.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Vendor.F.Comment, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.ItemPricing, TId.Vendor, 
					TblColumnNode.NewBrowsette(dsMB.Path.T.ItemPrice.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.HourlyOutside, TId.Vendor,
					TblColumnNode.NewBrowsette(dsMB.Path.T.LaborOutside.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.TaskDemandHourlyOutside, TId.Vendor,
					TblColumnNode.NewBrowsette(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.PerJobOutside, TId.Vendor,
					TblColumnNode.NewBrowsette(dsMB.Path.T.OtherWorkOutside.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.TaskDemandPerJobOutside, TId.Vendor,
					TblColumnNode.NewBrowsette(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.ServiceContract, TId.Vendor,
					TblColumnNode.NewBrowsette(dsMB.Path.T.ServiceContract.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.PurchaseOrder, TId.Vendor,
					TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.PurchaseOrderTemplate, TId.Vendor, 
					TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.Unit, TId.Vendor,
					TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID.F.UnitID.F.PurchaseVendorID, DCol.Normal, ECol.Normal))
			);
			VendorTblCreator = new DelayedCreateTbl(delegate()
			{
				return new Tbl(dsMB.Schema.T.Vendor, TId.Vendor,
					new Tbl.IAttr[] {
						VendorsDependentGroup,
						new BTbl(VendorListColumns),
						new ETbl(),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.VendorReport; }))
					},
					(TblLayoutNodeArray)vendorNodes.Clone()
				);
			});
			// Vendors node under Purchasing group - part of Purchasing Group
			VendorForPurchaseOrdersTblCreator = new DelayedCreateTbl(delegate()
			{
				return new Tbl(dsMB.Schema.T.Vendor, TId.Vendor,
					new Tbl.IAttr[] {
						PurchasingGroup,
						new BTbl(VendorListColumns),
						new ETbl(),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.VendorReport; }))
					},
					(TblLayoutNodeArray)vendorNodes.Clone()
				);
			});

			DefineTbl(dsMB.Schema.T.Vendor, VendorTblCreator);
			RegisterExistingForImportExport(TId.Vendor, dsMB.Schema.T.Vendor);

			#endregion
			#region VendorCategory
			DefineTbl(dsMB.Schema.T.VendorCategory, delegate() {
				return new Tbl(dsMB.Schema.T.VendorCategory, TId.VendorCategory,
				new Tbl.IAttr[] {
					VendorsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.VendorCategory.F.Code), BTbl.ListColumn(dsMB.Path.T.VendorCategory.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.VendorCategory.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.VendorCategory.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.VendorCategory.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Vendor, TId.VendorCategory,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Vendor.F.VendorCategoryID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.VendorCategory, dsMB.Schema.T.VendorCategory);
			#endregion
		}
	}
}
