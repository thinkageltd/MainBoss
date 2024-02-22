using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Receiving.
	/// </summary>
	public class TIReceive : TIGeneralMB3 {
		#region NodeIds
		internal static readonly Key costColumnId = dsMB.Path.T.POLineItem.F.POLineID.F.Cost.Key();
		internal static readonly Key uomColumnId = dsMB.Path.T.Item.F.UnitOfMeasureID.Key();
		internal static readonly Key quantityColumnId = dsMB.Path.T.POLineItem.F.Quantity.Key();
		#endregion
		#region Named Tbls
		private static readonly Key ReceivingItemLocation = KB.K("Receiving Storage Assignment");
		public static DelayedCreateTbl ReceiveItemPOCorrectionTblCreator = null;
		public static DelayedCreateTbl ReceiveItemPOFromActivityTblCreator = null;
		public static DelayedCreateTbl ReceiveItemNonPOCorrectionTblCreator = null;
		public static DelayedCreateTbl ReceiveMiscellaneousPOCorrectionTblCreator = null;
		#endregion
		#region Tbl-creator functions
		#region ReceiveItemPO
		private static Tbl ReceiveItemPOTbl(bool correction, bool fromItemActivity)
		{
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator = new AccountingTransactionWithQuantityDerivationTblCreator<long>(
				correction ? TId.CorrectionofReceiveItemWithPO : TId.ReceiveItemWithPO,
				correction, dsMB.Schema.T.ReceiveItemPO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			creator.BuildCommonPOReceivingControls(dsMB.Path.T.ReceiveItemPO.F.POLineItemID, dsMB.Path.T.ReceiveItemPO.F.ReceiptID,
				dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.Code,
				dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code,
				dsMB.Path.T.POLineItem.F.ItemLocationID.F.LocationID.F.Code);

			if (fromItemActivity && !correction)
				// If we are coming from the ItemActivity browsette the ItemLocationID is inited from the calling browser and we don't want the value
				// copied from the selected PO line.
				creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)), ECol.ReadonlyInUpdate));
			else
				creator.ActualSubstitution(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID, dsMB.Path.T.ItemLocation.F.Code, dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.ItemLocationID);

			// TODO: Prefix with an OnHand column: dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ActualItemLocationID.F.OnHand, ECol.AllReadonly),
			// or with an inventory availability line (Available, Maximum, Required)
			creator.BuildPOLineQuantityDisplaysAndSuggestRemainingQuantity(
				dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.Quantity, dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.ReceiveQuantity,
				dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.POLineID.F.Cost, dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.POLineID.F.ReceiveCost, null, null);

			creator.SetFromCCSourcePath(dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.POLineID.F.PurchaseOrderID.F.VendorID.F.AccountsPayableCostCenterID);
			creator.SetToCCSourcePath(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					CommonTblAttrs.ViewCostsDefinedBySchema,
					PurchasingAndInventoryGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.Waybill),
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemPO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemPO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemPO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ReceiveItemNonPO
		private static Tbl ReceiveItemNonPOTbl( bool correction )
		{
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator = new AccountingTransactionWithQuantityDerivationTblCreator<long>(
				correction ? TId.CorrectionofReceiveItemNoPO : TId.ReceiveItemNoPO,
				correction, dsMB.Schema.T.ReceiveItemNonPO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)), correction ? ECol.AllReadonly : ECol.Normal));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ReceiveItemNonPO.F.VendorID,
				new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), correction ? ECol.AllReadonly : ECol.Normal));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ReceiveItemNonPO.F.PaymentTermID,
				new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PaymentTerm.F.Code)), correction ? ECol.AllReadonly : ECol.Normal));

			// TODO (maybe) use ItemLocation-Maximum - ItemLocation.Available as the suggested quantity.
			creator.BuildQuantityControls();
			creator.BuildItemPricePickerControl(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemPriceID, dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID, dsMB.Path.T.ReceiveItemNonPO.F.VendorID);
			creator.StartCostingLayout();
			creator.BuildCommonInventoryOnHandCostControls(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID);
			creator.BuildPickedItemPriceResultDisplays();
			creator.BuildPickedItemPriceResultValueTransfers();
			creator.HandlePickedItemCostSuggestion();
			creator.BuildCostControls(CostCalculationValueSuggestedSourceId);
			creator.SetFromCCSourcePath(dsMB.Path.T.ReceiveItemNonPO.F.VendorID.F.AccountsPayableCostCenterID);
			creator.SetToCCSourcePath(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					CommonTblAttrs.ViewCostsDefinedBySchema,
					InventoryGroup | ItemResourcesGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemNonPO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemNonPO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ReceiveItemNonPO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ReceiveMiscellaneousPO
		private static Tbl ReceiveMiscellaneousPOTbl( bool correction )
		{
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator = new AccountingTransactionWithQuantityDerivationTblCreator<long>(
				correction ? TId.CorrectionofReceiveMiscellaneous : TId.ReceiveMiscellaneous,
				correction, dsMB.Schema.T.ReceiveMiscellaneousPO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.POMiscellaneousUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			creator.BuildCommonPOReceivingControls(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID, dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID,
				dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID.F.Code);
			creator.BuildPOLineQuantityDisplaysAndSuggestRemainingQuantity(
				dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.Quantity, dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.ReceiveQuantity,
				dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.POLineID.F.Cost, dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.POLineID.F.ReceiveCost, null, null);

			creator.SetFromCCSourcePath(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.POLineID.F.PurchaseOrderID.F.VendorID.F.AccountsPayableCostCenterID);
			creator.SetToCCSourcePath(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID.F.MiscellaneousID.F.CostCenterID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					CommonTblAttrs.ViewCostsDefinedBySchema,
					PurchasingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID.F.Waybill),
						BTbl.ListColumn(dsMB.Path.T.ReceiveMiscellaneousPO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ReceiveMiscellaneousPO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ReceiveMiscellaneousPO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#endregion

		#region Constructors and Property Initializers
		private TIReceive() {
		}
		static TIReceive() {
			ReceiveItemPOFromActivityTblCreator = new DelayedCreateTbl(delegate() {
				return ReceiveItemPOTbl(false, true);
			});
			ReceiveItemPOCorrectionTblCreator = new DelayedCreateTbl(delegate() {
				return ReceiveItemPOTbl(true, false);
			});
			ReceiveItemNonPOCorrectionTblCreator = new DelayedCreateTbl(delegate() {
				return ReceiveItemNonPOTbl(true);
			});
			ReceiveMiscellaneousPOCorrectionTblCreator = new DelayedCreateTbl(delegate() {
				return ReceiveMiscellaneousPOTbl(true);
			});
		}
		#endregion

		internal static void DefineTblEntries()
		{
			#region ReceiveItemPO
			DefineTbl(dsMB.Schema.T.ReceiveItemPO, delegate() {
				return ReceiveItemPOTbl(false, false);
			});
			#endregion
			#region ReceiveItemNonPO
			DefineTbl(dsMB.Schema.T.ReceiveItemNonPO, delegate() {
				return ReceiveItemNonPOTbl(false);
			});
			#endregion
			#region ReceiveMiscellaneousPO
			DefineTbl(dsMB.Schema.T.ReceiveMiscellaneousPO, delegate() {
				return ReceiveMiscellaneousPOTbl(false);
			});
			#endregion

			#region ReceiptActivity
			{
				Key receiveGroup = KB.K("Receive");
				Key joinedCorrectionsCommand = KB.K("Correct");
				// This condition can be applied to any ActualLabor or ActualOtherWork record
				CompositeView.Condition[] DemandMustHaveValidCategory = new CompositeView.Condition[] {
					new CompositeView.Condition(
						new SqlExpression(dsMB.Path.T.ReceiptActivity.F.WorkOrderExpenseModelEntryID).IsNotNull(),
						KB.K("The Demand associated with this purchase line item has an Expense Category that is not valid for the Work Order's Expense Model")
					)
				};
				// For the Receipt editor's Activity browsette
				DefineBrowseTbl(dsMB.Schema.T.ReceiptActivity, delegate()
				{
					return new CompositeTbl(dsMB.Schema.T.ReceiptActivity, TId.ReceiptActivity,
						new Tbl.IAttr[] {
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.ListColumn(dsMB.Path.T.ReceiptActivity.F.POLineID.F.PurchaseOrderText),
								BTbl.PerViewListColumn(quantityColumnId, quantityColumnId),
								BTbl.PerViewListColumn(uomColumnId, uomColumnId),
								BTbl.PerViewListColumn(costColumnId, costColumnId),
								BTbl.SetTreeStructure(dsMB.Path.T.ReceiptActivity.F.ParentID, 2)
							)
						},
						dsMB.Path.T.ReceiptActivity.F.TableEnum,
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineItemID,								// ViewRecordTypes.ReceiptActivity.POLineItem
							ReadonlyView,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineItem.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineItem.F.POLineID.F.Cost),
							BTbl.PerViewColumnValue(uomColumnId, dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveItemPOID,				// ViewRecordTypes.ReceiptActivity.ReceiveItemPO
							NoNewMode,
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ReceiveItemPO.F.ReceiptID),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveItemPO.F.AccountingTransactionID.F.Cost),
							CompositeView.JoinedNewCommand(receiveGroup),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.POLineItem },
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveItemPO.F.POLineItemID), dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineItemID)
							)
						),
						new CompositeView(TIReceive.ReceiveItemPOCorrectionTblCreator, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveItemPOID,	// ViewRecordTypes.ReceiptActivity.ReceiveItemPOCorrection
							NoNewMode,
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ReceiveItemPO.F.ReceiptID),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveItemPO.F.AccountingTransactionID.F.Cost),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.ReceiveItemPO, (int)ViewRecordTypes.ReceiptActivity.ReceiveItemPOCorrection},
								new CompositeView.Init(dsMB.Path.T.ReceiveItemPO.F.CorrectionID, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectionID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveItemPO.F.POLineItemID), dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveItemPOID.F.POLineItemID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
							)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineLaborID,								// ViewRecordTypes.ReceiptActivity.POLineLabor
							ReadonlyView,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineLabor.F.Quantity, IntervalFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineLabor.F.POLineID.F.Cost)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID,		// ViewRecordTypes.ReceiptActivity.ActualLaborOutsidePO
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.Quantity, IntervalFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.Cost),
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID),
							CompositeView.JoinedNewCommand(receiveGroup),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.POLineLabor },
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineLaborID),
								new CompositeView.Init(dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.ToCostCenterID, dsMB.Path.T.ReceiptActivity.F.WorkOrderExpenseModelEntryID.F.CostCenterID)
							)
						),
						new CompositeView(TIWorkOrder.ActualLaborOutsidePOCorrectionTblCreator, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID,	// ViewRecordTypes.ReceiptActivity.ActualLaborOutsidePOCorrection
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.Quantity, IntervalFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.Cost),
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.ActualLaborOutsidePO, (int)ViewRecordTypes.ReceiptActivity.ActualLaborOutsidePOCorrection},
								new CompositeView.Init(dsMB.Path.T.ActualLaborOutsidePO.F.CorrectionID, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.POLineLaborID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO and WO states.
							)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineOtherWorkID,							// ViewRecordTypes.ReceiptActivity.POLineOtherWork
							ReadonlyView,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineOtherWork.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineOtherWork.F.POLineID.F.Cost)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID,	// ViewRecordTypes.ReceiptActivity.ActualOtherWorkOutsidePO
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.Cost),
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID),
							CompositeView.JoinedNewCommand(receiveGroup),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.POLineOtherWork },
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineOtherWorkID),
								new CompositeView.Init(dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.ToCostCenterID, dsMB.Path.T.ReceiptActivity.F.WorkOrderExpenseModelEntryID.F.CostCenterID)
							)
						),
						new CompositeView(TIWorkOrder.ActualOtherWorkOutsidePOCorrectionTblCreator, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID,	// ViewRecordTypes.ReceiptActivity.ActualOtherWorkOutsidePOCorrection
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.Cost),
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.ActualOtherWorkOutsidePO, (int)ViewRecordTypes.ReceiptActivity.ActualOtherWorkOutsidePOCorrection},
								new CompositeView.Init(dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectionID, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO and WO states.
							)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineMiscellaneousID,						// ViewRecordTypes.ReceiptActivity.POLineMiscellaneous
							ReadonlyView,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineMiscellaneous.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.Cost)
						),
						new CompositeView(dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID,		// ViewRecordTypes.ReceiptActivity.ReceiveMiscellaneousPO
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.AccountingTransactionID.F.Cost),
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID),
							CompositeView.JoinedNewCommand(receiveGroup),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.POLineMiscellaneous },
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID), dsMB.Path.T.ReceiptActivity.F.POLineID.F.POLineMiscellaneousID)
							)
						),
						new CompositeView(TIReceive.ReceiveMiscellaneousPOCorrectionTblCreator, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID,	// ViewRecordTypes.ReceiptActivity.ReceiveMiscellaneousPOCorrection
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ReceiveMiscellaneousPO.F.AccountingTransactionID.F.Cost),
							CompositeView.PathAlias(dsMB.Path.T.ReceiptActivity.F.ReceiptID, dsMB.Path.T.ReceiveMiscellaneousPO.F.ReceiptID),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
							CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ReceiptActivity.ReceiveMiscellaneousPO, (int)ViewRecordTypes.ReceiptActivity.ReceiveMiscellaneousPOCorrection},
								new CompositeView.Init(dsMB.Path.T.ReceiveMiscellaneousPO.F.CorrectionID, dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID.F.CorrectionID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveMiscellaneousPO.F.POLineMiscellaneousID), dsMB.Path.T.ReceiptActivity.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID.F.POLineMiscellaneousID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
							)
						)
					);
				});
			}
			#endregion

			#region Receipt
			DefineTbl(dsMB.Schema.T.Receipt, delegate() {
				return new Tbl(dsMB.Schema.T.Receipt, TId.Receipt,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						PurchasingGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Receipt.F.EntryDate),
							BTbl.ListColumn(dsMB.Path.T.Receipt.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.Receipt.F.PurchaseOrderID.F.VendorID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Receipt.F.Waybill),
							BTbl.ListColumn(dsMB.Path.T.Receipt.F.Desc, BTbl.Contexts.List|BTbl.Contexts.SearchAndFilter),
							BTbl.ListColumn(dsMB.Path.T.Receipt.F.TotalReceive)
						,
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ReceiptReport))),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.EntryDate, new NonDefaultCol(), DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.PurchaseOrderID, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrder.F.Number)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.PurchaseOrderID.F.VendorID.F.Code, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.Waybill, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.Reference, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Receipt.F.Comment, DCol.Normal, ECol.Normal)
						),
						BrowsetteTabNode.New(TId.ReceiptActivity, TId.Receipt, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblContainerNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								// This is to avoid squeezing the browsette into the 'control' column defined by this control in the LCP.
								TblColumnNode.New(dsMB.Path.T.Receipt.F.TotalReceive, new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInBrowsetteArea)), ECol.AllReadonly)
							),
							TblColumnNode.NewBrowsette(dsMB.Path.T.ReceiptActivity.F.ReceiptID, DCol.Normal, ECol.Normal)
						)
					)
				);
			});
			#endregion
		}
	}
}
