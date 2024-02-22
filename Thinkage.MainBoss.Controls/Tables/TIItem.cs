using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Items.
	/// </summary>
	public class TIItem : TIGeneralMB3 {
		#region Record-type providers
		#region - ItemReceivingProvider
		private static object[] ItemReceivingValues = new object[] {
			(int)ViewRecordTypes.ItemReceiving.NotSpecified,
			(int)ViewRecordTypes.ItemReceiving.ReceivePermanentPO,
			(int)ViewRecordTypes.ItemReceiving.ReceiveTemporaryPO,
			(int)ViewRecordTypes.ItemReceiving.ReceivePermanentNonPO,
			(int)ViewRecordTypes.ItemReceiving.ReceiveTemporaryNonPO,
			(int)ViewRecordTypes.ItemReceiving.ReceivePermanentPOCorrection,
			(int)ViewRecordTypes.ItemReceiving.ReceiveTemporaryPOCorrection,
			(int)ViewRecordTypes.ItemReceiving.ReceivePermanentNonPOCorrection,
			(int)ViewRecordTypes.ItemReceiving.ReceiveTemporaryNonPOCorrection
		};
		private static Key[] ItemReceivingLabels = new Key[] {
			KB.K("Not Specified"),
			KB.TOi(TId.ReceiveItemWithPO),
			KB.K("Receive Item to Temporary Location (with PO)"),
			KB.TOi(TId.ReceiveItemNoPO),
			KB.K("Receive Item to Temporary Location (no PO)"),
			KB.TOi(TId.CorrectionofReceiveItemWithPO),
			KB.K("Correction of Receive Item to Temporary Location (with PO)"),
			KB.TOi(TId.CorrectionofReceiveItemNoPO),
			KB.K("Correction of Receive Item to Temporary Location (no PO)")
		};
		public static EnumValueTextRepresentations ItemReceivingProvider = new EnumValueTextRepresentations(ItemReceivingLabels, null, ItemReceivingValues);
		#endregion
		#region - ItemPricingProvider
		public static EnumValueTextRepresentations ItemPricingProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Not Specified"),
				KB.K("Price Quote"),
				KB.TOi(TId.ReceiveItemWithPO),
				KB.TOi(TId.ReceiveItemNoPO)
			}, null,
			new object[] {
				(int)ViewRecordTypes.ItemPricing.NotSpecified,
				(int)ViewRecordTypes.ItemPricing.PriceQuote,
				(int)ViewRecordTypes.ItemPricing.ReceivePO,
				(int)ViewRecordTypes.ItemPricing.ReceiveNonPO
			}
		);
		#endregion
		#region - ActiveTemporaryStorageWithItemAssignmentsProvider
		public static EnumValueTextRepresentations ActiveTemporaryStorageWithItemAssignmentsProvider
			= new EnumValueTextRepresentations(
				new Key[] {
					KB.TOi(TId.TemporaryStorage),
					KB.TOi(TId.TemporaryStorageAssignment)
				}, null,
				new object[] {
					(int)ViewRecordTypes.ActiveTemporaryStorageWithItemAssignments.TemporaryStorage,
					(int)ViewRecordTypes.ActiveTemporaryStorageWithItemAssignments.TemporaryItemLocation
				}
			);
		#endregion
		#region ItemActivityProvider
		private static object[] ItemActivityValues = new object[] {
			(int)ViewRecordTypes.ItemActivity.NotSpecified,
			(int)ViewRecordTypes.ItemActivity.ItemCountValue,
			(int)ViewRecordTypes.ItemActivity.VoidItemCountValue,
			(int)ViewRecordTypes.ItemActivity.ItemAdjustment,
			(int)ViewRecordTypes.ItemActivity.ItemIssue,

			(int)ViewRecordTypes.ItemActivity.ItemTransferTo,
			(int)ViewRecordTypes.ItemActivity.ItemTransferFrom,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemPO,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemNonPO,
			(int)ViewRecordTypes.ItemActivity.ActualItem,

			(int)ViewRecordTypes.ItemActivity.ItemIssueCorrection,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemPOCorrection,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemNonPOCorrection,
			(int)ViewRecordTypes.ItemActivity.ActualItemCorrection,
			(int)ViewRecordTypes.ItemActivity.ItemTransferToCorrection,

			(int)ViewRecordTypes.ItemActivity.ItemTransferFromCorrection,
			(int)ViewRecordTypes.ItemActivity.VoidedItemCountValue
		};
		private static Key[] ItemActivityLabels = new Key[] {
			KB.K("Not Specified"),
			KB.TOi(TId.PhysicalCount),
			KB.TOi(TId.VoidPhysicalCount),
			KB.TOi(TId.ItemAdjustment),
			KB.TOi(TId.ItemIssue),
			KB.TOi(TId.ItemTransferTo),
			KB.TOi(TId.ItemTransferFrom),
			KB.TOi(TId.ReceiveItemWithPO),
			KB.TOi(TId.ReceiveItemNoPO),
			KB.TOi(TId.ActualItem),
			KB.TOi(TId.CorrectionofItemIssue),
			KB.TOi(TId.CorrectionofReceiveItemWithPO),
			KB.TOi(TId.CorrectionofReceiveItemNoPO),
			KB.TOi(TId.CorrectionofActualItem),
			KB.TOi(TId.CorrectionofItemTransferTo),
			KB.TOi(TId.CorrectionofItemTransferFrom),
			KB.TOi(TId.VoidPhysicalCount)
		};

		public static EnumValueTextRepresentations ItemActivityProvider = new EnumValueTextRepresentations(ItemActivityLabels, null, ItemActivityValues);
#endregion
		#endregion
		#region NodeIds
		private static readonly object ActivityBrowsetteId = KB.I("ActivityBrowsetteId");
		#endregion

		#region ItemLocationInit
		private static TblActionNode ItemLocationInit {
			get {
				return new Check2<ulong, ulong>(
					delegate(ulong min, ulong max) {
						if (min > max)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Minimum must be less than or equal to Maximum")));
						return null;
					})
				.Operand1(MinimumId)
				.Operand2(MaximumId);
			}
		}
		#endregion

		#region Named Tbls
		public static Tbl VoidPhysicalCountTbl;
		public static DelayedCreateTbl ItemAsSparePartPickerTblCreator = null;
		public static readonly DelayedCreateTbl ItemIssueCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ItemTransferCorrectionTblCreator = null;
		// In case of known vendor and item, only display costs and purchase order text
		public static readonly DelayedCreateTbl PermanentItemActivityBrowseTblCreator = null;
		public static readonly DelayedCreateTbl TemporaryItemActivityBrowseTblCreator = null;
		public static readonly DelayedCreateTbl ActiveTemporaryStorageEditTblCreator = null;
		public static readonly DelayedCreateTbl ActiveTemporaryItemLocationTblCreator = null;
		public static readonly DelayedCreateTbl AllTemporaryStorageEditTblCreator = null;
		public static readonly DelayedCreateTbl AllTemporaryItemLocationTblCreator = null;
		public static readonly DelayedCreateTbl ItemPriceTblCreator = null;
		#endregion
		#region Tbl-creator functions
		#region Item Activity

		private static DelayedCreateTbl ItemActivityTbl(bool forPermanentStorage) {
			// For the Item editor's Activity browsette
			return new DelayedCreateTbl(delegate() {
				Key correctionGroup = KB.K("Correct"); // JoinedNewCommand
				Key activityGroup = KB.K("Receive");
				Key clericalGroup = KB.K("Physical Count");
				dsMB.PathClass.PathToItemActivityRow root = dsMB.Path.T.ItemActivity;
				dsMB.PathClass.PathToAccountingTransactionLink tx = root.F.AccountingTransactionID;
				CompositeView CompositeViewZero = null;
				CompositeView CompositeViewOne = null;
				CompositeView CompositeViewFifteen = null;
				if (forPermanentStorage) {
					CompositeViewZero =
							new CompositeView(tx.F.ItemCountValueID,							// Table #0 - Physical count
								CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemCountValue.F.ItemLocationID),
								CompositeView.NewCommandGroup(clericalGroup),
								CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete));

					// These other two views could always be in the Tbl since they have no context-free New and the context required by the contextual New operation
					// can never occur. However this could also cause panel growth from impossible panel variants, and impossible New operations to show up on buttons.
					CompositeViewOne =
							new CompositeView(tx.F.ItemCountValueVoidID, NoNewMode,	// Table #1 - Void physical count
								CompositeView.JoinedNewCommand(correctionGroup),
								CompositeView.ContextualInit((int)ViewRecordTypes.ItemActivity.ItemCountValue,
									new CompositeView.Init(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID, tx.F.ItemCountValueID),
						// Reverse the accounting transaction.
									new CompositeView.Init(dsMB.Path.T.ItemCountValueVoid.F.AccountingTransactionID.F.FromCostCenterID, tx.F.ToCostCenterID),
									new CompositeView.Init(dsMB.Path.T.ItemCountValueVoid.F.AccountingTransactionID.F.ToCostCenterID, tx.F.FromCostCenterID),
									new CompositeView.Init(dsMB.Path.T.ItemCountValueVoid.F.AccountingTransactionID.F.Cost, tx.F.Cost),
									new CompositeView.Init(dsMB.Path.T.ItemCountValueVoid.F.AccountingTransactionID.F.EffectiveDate, tx.F.EffectiveDate)),
						// TODO: The PathAlias is commented out as a kludge to stop the browsette from specifying filter-based setting of the ItemLocationID.
						// Someday we may want the PathAlias info for other purposes, or it might be inferred automatically from the query definition,
						// in which case we would want a CompositeView.NoFilterBasedInit() or some such beast to explicitly prevent this filter.
						// , CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.ItemLocationID)
								CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete));
					CompositeViewFifteen =
							new CompositeView(tx.F.ItemCountValueID,							// Table #15 - Voided Physical count
								CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemCountValue.F.ItemLocationID),
								ReadonlyView);
				}

				return new CompositeTbl(dsMB.Schema.T.ItemActivity, TId.ItemActivity,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(BTbl.ListColumn(root.F.TableEnum),
							BTbl.ListColumn(tx.F.EffectiveDate, BTbl.Contexts.SortInitialAscending),
							BTbl.ListColumn(root.F.ItemLocationID.F.ItemID.F.Code),
							BTbl.ListColumn(root.F.Quantity),
							BTbl.ListColumn(root.F.UnitCost),
							BTbl.ListColumn(root.F.Cost),
							BTbl.SetTreeStructure(root.F.ParentID, 2)
						)
					},
					root.F.TableEnum,
					// TODO: As a policy we do not allow ActualizeItem and PO Receiving here because it is too difficult to control the filtering
					// of the pickers (DemandItem/POLIneItem) in the called editor. Also with POLineItem there is the issue of whether we are
					// initing the POLIneItem choice or the receive-to location choice.
					// Ideally, though, they should be allowed.
					CompositeViewZero,																								// Table #0 - Physical count
					CompositeViewOne,																								// Table #1 - Voided physical count
					new CompositeView(tx.F.ItemAdjustmentID,																		// Table #2 - Adjustment
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemAdjustment.F.ItemLocationID),
						CompositeView.NewCommandGroup(clericalGroup)),
					new CompositeView(tx.F.ItemIssueID,																				// Table #3 - Issue
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemIssue.F.ItemLocationID),
						CompositeView.NewCommandGroup(activityGroup)),
					new CompositeView(tx.F.ItemTransferID,																			// Table #4 - Transfer 'To'
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemTransfer.F.FromItemLocationID),
						CompositeView.NewCommandGroup(activityGroup),
						CompositeView.IdentificationOverride(TId.ItemTransferTo)),
					new CompositeView(tx.F.ItemTransferID,																			// Table #5 - Transfer 'From'
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemTransfer.F.ToItemLocationID),
						CompositeView.NewCommandGroup(activityGroup),
						CompositeView.IdentificationOverride(TId.ItemTransferFrom)),
					new CompositeView(TIReceive.ReceiveItemPOFromActivityTblCreator, tx.F.ReceiveItemPOID,							// Table #6 - PO Receive
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ReceiveItemPO.F.ItemLocationID),
						CompositeView.NewCommandGroup(activityGroup)),
					new CompositeView(tx.F.ReceiveItemNonPOID,																		// Table #7 - Non-PO receive
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID),
						CompositeView.NewCommandGroup(activityGroup)),
					new CompositeView(tx.F.ActualItemID, ReadonlyView,																// Table #8 - Actualize Item
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ActualItem.F.DemandItemID.F.ItemLocationID)),
					new CompositeView(TIItem.ItemIssueCorrectionTblCreator, tx.F.ItemIssueID, NoNewMode,						// Table #9 - Issue Correction
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemIssue.F.ItemLocationID),
						CompositeView.JoinedNewCommand(correctionGroup),
						CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ItemActivity.ItemIssue, (int)ViewRecordTypes.ItemActivity.ItemIssueCorrection},
							new CompositeView.Init(dsMB.Path.T.ItemIssue.F.CorrectionID, tx.F.ItemIssueID.F.CorrectionID)),
						CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
					new CompositeView(TIReceive.ReceiveItemPOCorrectionTblCreator, tx.F.ReceiveItemPOID, NoNewMode,			// Table #10 - PO Receive Correction
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ReceiveItemPO.F.ItemLocationID),
						CompositeView.JoinedNewCommand(correctionGroup),
						CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ItemActivity.ReceiveItemPO, (int)ViewRecordTypes.ItemActivity.ReceiveItemPOCorrection},
							new CompositeView.Init(dsMB.Path.T.ReceiveItemPO.F.CorrectionID, tx.F.ReceiveItemPOID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveItemPO.F.POLineItemID), tx.F.ReceiveItemPOID.F.POLineItemID)	// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						),
						CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
					new CompositeView(TIReceive.ReceiveItemNonPOCorrectionTblCreator, tx.F.ReceiveItemNonPOID, NoNewMode,	// Table #11 - Non-PO receive Correction
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID),
						CompositeView.JoinedNewCommand(correctionGroup),
						CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ItemActivity.ReceiveItemNonPO, (int)ViewRecordTypes.ItemActivity.ReceiveItemNonPOCorrection},
							new CompositeView.Init(dsMB.Path.T.ReceiveItemNonPO.F.CorrectionID, tx.F.ReceiveItemNonPOID.F.CorrectionID)),
						CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
					new CompositeView(TIWorkOrder.ActualItemCorrectionTblCreator, tx.F.ActualItemID, NoNewMode,				// Table #12 - Actualize Item Correction
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ActualItem.F.DemandItemID.F.ItemLocationID),
						CompositeView.JoinedNewCommand(correctionGroup),
						CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ItemActivity.ActualItem, (int)ViewRecordTypes.ItemActivity.ActualItemCorrection},
							new CompositeView.Init(dsMB.Path.T.ActualItem.F.CorrectionID, tx.F.ActualItemID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualItem.F.DemandItemID), tx.F.ActualItemID.F.DemandItemID)			// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on WO state.
						),
						CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
					new CompositeView(TIItem.ItemTransferCorrectionTblCreator, tx.F.ItemTransferID, NoNewMode,				// Table #13 - Transfer 'To' Correction
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemTransfer.F.FromItemLocationID),
						CompositeView.JoinedNewCommand(correctionGroup),
						CompositeView.IdentificationOverride(TId.CorrectionofItemTransferTo),
						CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ItemActivity.ItemTransferTo, (int)ViewRecordTypes.ItemActivity.ItemTransferToCorrection},
							new CompositeView.Init(dsMB.Path.T.ItemTransfer.F.CorrectionID, tx.F.ItemTransferID.F.CorrectionID)),
						CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
					new CompositeView(TIItem.ItemTransferCorrectionTblCreator, tx.F.ItemTransferID, NoNewMode,				// Table #14 - Transfer 'From' Correction
						CompositeView.PathAlias(root.F.ItemLocationID, dsMB.Path.T.ItemTransfer.F.ToItemLocationID),
						CompositeView.JoinedNewCommand(correctionGroup),
						CompositeView.IdentificationOverride(TId.CorrectionofItemTransferFrom),
						CompositeView.ContextualInit(new int[] {(int)ViewRecordTypes.ItemActivity.ItemTransferFrom, (int)ViewRecordTypes.ItemActivity.ItemTransferFromCorrection},
							new CompositeView.Init(dsMB.Path.T.ItemTransfer.F.CorrectionID, tx.F.ItemTransferID.F.CorrectionID)),
						CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
					CompositeViewFifteen
				);
			});
		}
		#endregion
		#region ItemIssueTbl
		private static Tbl ItemIssueTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(correction ? TId.CorrectionofItemIssue : TId.ItemIssue, correction, dsMB.Schema.T.ItemIssue, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemIssue.F.ItemLocationID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)),
											correction ? ECol.AllReadonly : ECol.Normal));
			// Define the destination - This is the issue code and issue-to
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemIssue.F.ItemIssueCodeID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemIssueCode.F.Code)),
											correction ? ECol.AllReadonly : ECol.ReadonlyInUpdate));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemIssue.F.EmployeeID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Employee.F.ContactID.F.Code)),
											correction ? ECol.AllReadonly : ECol.Normal));

			// No quantity suggestion
			// Define the quantity(ies)
			creator.BuildQuantityControls();
			creator.StartCostingLayout();
			creator.BuildCostingBasedOnInventory(dsMB.Path.T.ItemIssue.F.ItemLocationID);
			creator.BuildCostControls(CostCalculationValueSuggestedSourceId);
			creator.SetToCCSourcePath(dsMB.Path.T.ItemIssue.F.ItemIssueCodeID.F.CostCenterID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					InventoryGroup | ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ItemIssue.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ItemIssue.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ItemIssue.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ItemTransferTbl
		private static Tbl ItemTransferTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(
					correction ? TId.CorrectionofItemTransfer : TId.ItemTransfer,
					correction, dsMB.Schema.T.ItemTransfer, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemTransfer.F.FromItemLocationID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)),
											correction ? ECol.AllReadonly : ECol.Normal));
			// Define the destination - This is the issue code and issue-to
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemTransfer.F.ToItemLocationID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)),
											correction ? ECol.AllReadonly : ECol.Normal));

			// No quantity suggestion
			// Define the quantity(ies)
			creator.BuildQuantityControls();

			creator.StartCostingLayout();
			// Inlined from common inventory code since we want to give choice of From and To costing values.
			object FromOnHandQuantityId = KB.I("FromOnHandQuantityId");
			object FromOnHandValueId = KB.I("FromOnHandValue");
			object CalculatedFromOnHandValueId = KB.I("CalculatedFromOnHandValue");
			object FromOnHandUnitCostId = KB.I("FromOnHandUnitCost");
			creator.StartCostingRow(KB.K("Source Assignment status"));
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ItemTransfer.F.FromItemLocationID.F.ActualItemLocationID.F.OnHand, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(FromOnHandQuantityId))));
			creator.AddUnitCostEditDisplay(FromOnHandUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ItemTransfer.F.FromItemLocationID.F.ActualItemLocationID.F.TotalCost, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(FromOnHandValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(FromOnHandQuantityId, FromOnHandValueId, FromOnHandUnitCostId));
			creator.HandleCostSuggestionWithBasisCost(KB.K("Calculated source Assignment On Hand Cost"), FromOnHandQuantityId, FromOnHandValueId,
				new Thinkage.MainBoss.Controls.TIGeneralMB3.DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(KB.K("Use calculated source Assignment On Hand cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated source Assignment On Hand cost")));

			// Show the destination on-hand situation.
			object ToOnHandQuantityId = KB.I("ToOnHandQuantityId");
			object ToOnHandValueId = KB.I("ToOnHandValue");
			object CalculatedToOnHandValueId = KB.I("CalculatedToOnHandValue");
			object ToOnHandUnitCostId = KB.I("ToOnHandUnitCost");
			creator.StartCostingRow(KB.K("Destination Assignment status"));
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ItemTransfer.F.ToItemLocationID.F.ActualItemLocationID.F.OnHand, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ToOnHandQuantityId))));
			creator.AddUnitCostEditDisplay(ToOnHandUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ItemTransfer.F.ToItemLocationID.F.ActualItemLocationID.F.TotalCost, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ToOnHandValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(ToOnHandQuantityId, ToOnHandValueId, ToOnHandUnitCostId));

			creator.HandleCostSuggestionWithBasisCost(KB.K("Calculated destination Assignment On Hand Cost"), ToOnHandQuantityId, ToOnHandValueId,
				new Thinkage.MainBoss.Controls.TIGeneralMB3.DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(KB.K("Use calculated destination Assignment On Hand cost"), CalculatedToOnHandValueId, KB.K("Using calculated destination Assignment On Hand cost")));

			creator.BuildCostControls(CostCalculationValueSuggestedSourceId);
			creator.SetFromCCSourcePath(dsMB.Path.T.ItemTransfer.F.FromItemLocationID);
			creator.SetToCCSourcePath(dsMB.Path.T.ItemTransfer.F.ToItemLocationID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					InventoryGroup | ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ItemTransfer.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ItemTransfer.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ItemTransfer.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ItemPriceTbl
		internal static Tbl ItemPriceTbl(params BTbl.ICtorArg[] passedBtblArgs) {
			List<BTbl.ICtorArg> btblArgs = new List<BTbl.ICtorArg>(passedBtblArgs);

			btblArgs.AddRange(new BTbl.ICtorArg[] {
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.ItemID.F.Code, BTbl.Contexts.List|BTbl.Contexts.ClosedPicker|BTbl.Contexts.SearchAndFilter),
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.VendorID.F.Code), // Always show Vendor ID with Pricing
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.PurchaseOrderText, PurchasingGroup),
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.Quantity, NonPerViewColumn),
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.ItemID.F.UnitOfMeasureID.F.Code),
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.UnitCost),
					BTbl.ListColumn(dsMB.Path.T.ItemPrice.F.Cost, NonPerViewColumn),
					BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ItemPricing))
			});

			return new Tbl(dsMB.Schema.T.ItemPrice, TId.ItemPricing,
				new Tbl.IAttr[] {
						ItemsDependentGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(btblArgs.ToArray()),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
				},
				new TblLayoutNodeArray(
					TblFixedRecordTypeNode.New(),
					TblColumnNode.New(dsMB.Path.T.ItemPrice.F.EffectiveDate, DCol.Normal, new NonDefaultCol(), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ItemPrice.F.ItemID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Item.F.Code)), ECol.Normal, new NonDefaultCol()),
					TblColumnNode.New(dsMB.Path.T.ItemPrice.F.ItemID.F.UnitOfMeasureID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitOfMeasure.F.Code))),
					TblColumnNode.New(dsMB.Path.T.ItemPrice.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), DCol.Normal, ECol.Normal }, MulticolumnQuantityUnitCostTotalLabels,
						TblRowNode.New(KB.K("Quoted Pricing"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.ItemPrice.F.Quantity, DCol.Normal, new ECol(Fmt.SetId(ThisEntryQuantityId))),
							TblColumnNode.New(dsMB.Path.T.ItemPrice.F.UnitCost, new NonDefaultCol(), DCol.Normal),
							TblUnboundControlNode.New(KB.K("Quoted Unit Cost"), dsMB.Path.T.ItemPrice.F.UnitCost.ReferencedColumn.EffectiveType, new ECol(Fmt.SetId(ThisEntryUnitCostId))),
							TblColumnNode.New(dsMB.Path.T.ItemPrice.F.Cost, DCol.Normal, new ECol(Fmt.SetId(ThisEntryCostId)))
						)
					),
					TblColumnNode.New(dsMB.Path.T.ItemPrice.F.Quantity, DCol.Normal, ECol.Normal, new DefaultOnlyCol()),
					TblColumnNode.New(dsMB.Path.T.ItemPrice.F.PurchaseOrderText, DCol.Normal, ECol.Normal, new FeatureGroupArg(PurchasingGroup))
				),
				QuantityUnitTotalTripleCalculator<long>(ThisEntryQuantityId, ThisEntryUnitCostId, ThisEntryCostId)
			);
		}
		#endregion
		#region ItemAdjustmentTbl
		private static Tbl ItemAdjustmentTbl() {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(TId.ItemAdjustment, false, dsMB.Schema.T.ItemAdjustment, AccountingTransactionDerivationTblCreator.ValueInterpretations.AnyDelta, false, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemAdjustment.F.ItemLocationID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)),
											ECol.ReadonlyInUpdate));
			// Define the destination - This is the adjustment code
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemAdjustment.F.ItemAdjustmentCodeID,
											new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemAdjustmentCode.F.Code)),
											ECol.ReadonlyInUpdate));

			// No quantity suggestion
			// Define the quantity(ies)
			creator.BuildQuantityControls();
			// We can't call BuildCostingBasedOnInventory(dsMB.Path.T.ItemAdjustment.F.ItemLocationID); because that method sets the "FROM" CC source path based on the IL
			// but we treat the IL as the "destination" of the transaction.
			creator.StartCostingLayout();
			creator.BuildCommonInventoryOnHandCostControls(dsMB.Path.T.ItemAdjustment.F.ItemLocationID);
			creator.HandleCommonInventoryCostSuggestion();
			creator.BuildCostControls(CostCalculationValueSuggestedSourceId);
			creator.SetFromCCSourcePath(dsMB.Path.T.ItemAdjustment.F.ItemAdjustmentCodeID.F.CostCenterID);
			creator.SetToCCSourcePath(dsMB.Path.T.ItemAdjustment.F.ItemLocationID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					InventoryGroup | ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ItemAdjustment.F.AccountingTransactionID.F.EffectiveDate),
						BTbl.ListColumn(dsMB.Path.T.ItemAdjustment.F.Quantity),
						BTbl.ListColumn(dsMB.Path.T.ItemAdjustment.F.AccountingTransactionID.F.Cost)
					)
				}
			);
		}
		#endregion
		#region ItemCountValueTbl
		private static Tbl ItemCountValueTbl() {
			// Physical count differs from the rest of the world in that the user enters, and the derived record stores, *absolute* quantity and value,
			// and that the change in value for the accounting record is calculated for the user.
			// For that reason, we give the creator object's ctor an alternative binding path for its TotalCost control, and we manage the one bound to
			// the AccountingTransaction ourselves.
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(TId.PhysicalCount, false, dsMB.Schema.T.ItemCountValue, AccountingTransactionDerivationTblCreator.ValueInterpretations.AbsoluteTangibleSetting, false, dsMB.Path.T.ItemCountValue.F.Cost, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			creator.EffectiveDateRestrictionDefaultActivity = TblActionNode.Activity.Disabled;
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemCountValue.F.ItemLocationID,
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemLocation.F.Code)), ECol.ReadonlyInUpdate));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemCountValue.F.ItemAdjustmentCodeID,
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemAdjustmentCode.F.Code)), ECol.ReadonlyInUpdate));

			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemCountValue.F.VoidingItemCountValueVoidID.F.AccountingTransactionID.F.EntryDate,
								DCol.Normal, ECol.AllReadonly));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemCountValue.F.VoidingItemCountValueVoidID,
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemCountValueVoid.F.VoidCodeID.F.Code)), ECol.AllReadonly));

			creator.BuildQuantityControls();
			// Setup Resource availability values and display the on-hand situation.
			creator.StartCostingLayout();
			creator.BuildCommonInventoryOnHandCostControls(dsMB.Path.T.ItemCountValue.F.ItemLocationID);

			// Allow the user to pick a price quote and suggest a value based purely on the quoted pricing (for all the on-hand)
			// TODO: We should use the selected price quote when increasing quantity on hand, and on-hand values when decreasing on-hand.
			// Or perhaps there should be two evaluations: One based purely on the price quote, and another using the price quote only for added quantity.
			creator.BuildItemPricePickerControl(dsMB.Path.T.ItemCountValue.F.ItemLocationID.F.ItemPriceID, dsMB.Path.T.ItemCountValue.F.ItemLocationID.F.ItemID, null);
			creator.BuildPickedItemPriceResultDisplays();
			creator.BuildPickedItemPriceResultValueTransfers(OnHandQuantityId, OnHandValueId);
			creator.HandlePickedItemCostSuggestion();
			creator.BuildCostControls(CostCalculationValueSuggestedSourceId);

			// AccountingTransaction.Cost = on-hand value - entered value (*reduction* in value, so the IL is the FROM for accounting purposes).
			creator.FlushCostingLayout();
			creator.DetailColumns.Add(TblColumnNode.New(creator.AccountingCostPath, DCol.Normal, CommonNodeAttrs.PermissionToViewAccounting, AccountingFeatureArg));

			// Unfortunately, for some reason lost in the mists of time, CheckN.OperandM() does not have a form that takes a label, a path (and recordSet), and a corrector delegate.
			// This may have been only to reduce combinatorial complexity?
			// So to allow the following Check to reference the AccountingCostPath we have to make a bound node with a hidden control. This uses the hidden-control creator delegate in
			// TblUnboundControlNode.
			object TransferredValueId = KB.I("TransferredValueId");
			creator.DetailColumns.Add(TblColumnNode.New(creator.AccountingCostPath, new ECol(ECol.HiddenAccess, Fmt.SetId(TransferredValueId), ECol.ForceBidirectionalTransfer())));
			creator.Actions.Add(new Check3<decimal?, decimal?, decimal?>()
				.Operand1(AccountingTransactionWithQuantityDerivationTblCreator<long>.OnHandValueId)
				.Operand2(creator.ThisCostId)
				.Operand3(TransferredValueId,
					delegate(decimal? apparent, decimal? entered) {
						return !apparent.HasValue || !entered.HasValue ? null : (decimal?)checked(apparent.Value - entered.Value);
					}, NewAndCloneOnly));

			creator.SetFromCCSourcePath(dsMB.Path.T.ItemCountValue.F.ItemLocationID);
			creator.SetToCCSourcePath(dsMB.Path.T.ItemCountValue.F.ItemAdjustmentCodeID.F.CostCenterID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					InventoryGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ItemCountValue.F.AccountingTransactionID.F.EffectiveDate),
							BTbl.ListColumn(dsMB.Path.T.ItemCountValue.F.ItemLocationID.F.ItemID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ItemCountValue.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ItemCountValue.F.ItemAdjustmentCodeID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ItemCountValue.F.VoidingItemCountValueVoidID.F.AccountingTransactionID.F.EntryDate)
					)
				}
			);
		}
		#endregion
		#region ItemCountValueVoidTbl
		private static Tbl ItemCountValueVoidTbl() {
			// Physical count differs from the rest of the world in that the user enters, and the derived record stores, *absolute* quantity and value,
			// and that the change in value for the accounting record is calculated for the user.
			// For that reason, this method does a lot of the work itself rather than calling the creator's methods.
			AccountingTransactionDerivationTblCreator creator = new AccountingTransactionDerivationTblCreator(TId.VoidPhysicalCount, false, dsMB.Schema.T.ItemCountValueVoid, AccountingTransactionDerivationTblCreator.ValueInterpretations.AbsoluteTangibleSetting, null, TIGeneralMB3.ItemUnitCostTypeOnClient);
			object voidedCountId = KB.I("VoidedCountId");
			creator.BuildCommonAccountingHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(voidedCountId))));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.VoidCode.F.Code)), ECol.ReadonlyInUpdate)); //TODO: Should this be restricted Readonly? it doesn't reflect any accounting information since the cost center is always the original ItemCount's costcenter

			TblGroupNode group = TblGroupNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
				TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.AccountingTransactionID.F.EntryDate, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.AccountingTransactionID.F.EffectiveDate, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.AccountingTransactionID.F.UserID.F.ContactID.F.Code, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.ItemLocationID.F.Code, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.ItemAdjustmentCodeID.F.Code, DCol.Normal)
			);
			creator.DetailColumns.Add(group);
			creator.DetailColumns.Add(TblColumnNode.New(creator.AccountingCostPath, DCol.Normal, CommonNodeAttrs.PermissionToViewAccounting, AccountingFeatureArg));

			creator.Actions.Add(Init.OnLoadNew(dsMB.Path.T.ItemCountValueVoid.F.AccountingTransactionID.F.Cost,
				new EditorPathValue(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.AccountingTransactionID.F.Cost)));

			// The Creator class still thinks it should be checking for Cost > 0 so we give it a dummy cost that is always null to check.
			creator.DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(creator.ThisCostId, creator.CostTypeInfo));

			creator.SetFromCCSourcePath(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.AccountingTransactionID.F.ToCostCenterID);
			creator.SetToCCSourcePath(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.AccountingTransactionID.F.FromCostCenterID);

			// We add our own effective-date restrictions, since these are based on the EffectiveDate of the PhysicalCount we are trying to void.
			// Essentially we can only void the current physical count.
			// TODO: This should also be enforced in the calling browser as a condition on the composite view's ContextualInit.
			creator.Actions.Add(new Check2<Guid?, Guid?>(
				delegate(Guid? currentPCId, Guid? thisPCId) {
					if (currentPCId.HasValue && thisPCId.HasValue && currentPCId != thisPCId)
						return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Only the most latest non-voided Physical Count can be voided")));
					return null;
				})
				.Operand1(KB.K("Latest Physical Count"), dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.CurrentItemCountValueID)
				.Operand2(voidedCountId));

			return creator.GetTbl(
				new Tbl.IAttr[] {
					CommonTblAttrs.ViewCostsDefinedBySchema,
					InventoryGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.ItemLocationID.F.ItemID.F.Code), // Defined for completeness for XID picker requirements (never used)
						BTbl.ListColumn(dsMB.Path.T.ItemCountValueVoid.F.VoidedItemCountValueID.F.ItemLocationID.F.LocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.ItemCountValueVoid.F.VoidCodeID.F.Code)
					)
				}
			);
		}
		#endregion
		#region TemporaryStorageTbl
		private static DelayedCreateTbl TemporaryStorageEditTbl(bool onlyActiveOnes) {
			return new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.TemporaryStorage, TId.TemporaryStorage,
					new Tbl.IAttr[] {
						ItemResourcesGroup,
						new ETbl(
							ETbl.UseNewConcurrency(true),
							ETbl.EditorAccess(false, EdtMode.UnDelete)
						)
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.WorkOrderID, new NonDefaultCol(),
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrder.F.Number)),
								new ECol(Fmt.SetPickFrom(TIWorkOrder.AllWorkOrderTemporaryStoragePickerTblCreator))),
							TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.ContainingLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.LocationID.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.LocationID.F.Comment, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.TemporaryStorageAssignment, TId.TemporaryStorage,
							TblColumnNode.NewBrowsette(onlyActiveOnes ? TIItem.ActiveTemporaryItemLocationTblCreator : TIItem.AllTemporaryItemLocationTblCreator, dsMB.Path.T.TemporaryStorage.F.LocationID, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, DCol.Normal, ECol.Normal))
					)
				);
			});
		}
		#endregion
		#region TemporaryItemLocationTbl
		private static DelayedCreateTbl TemporaryItemLocationTbl(bool activeOnly) {
			return new DelayedCreateTbl(delegate() {
				List<BTbl.ICtorArg> BTblAttrs = new List<BTbl.ICtorArg>();
				if (activeOnly)
					BTblAttrs.Add(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.TemporaryStorageID.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.TemporaryStorageActive).IsTrue()));
				// Show the code only in the panel. Composite codes are typically too wide for ListColumns, and it is the Item most people are interested in
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Code));
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Desc, NonPerViewColumn));
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.OnHand));
				return new Tbl(dsMB.Schema.T.TemporaryItemLocation, TId.TemporaryStorageAssignment,
						new Tbl.IAttr[] {
							ItemResourcesGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(BTblAttrs.ToArray()),
							new ETbl()
						},
						new TblLayoutNodeArray(
							DetailsTabNode.New(
								TblFixedRecordTypeNode.New(),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.WorkOrderID, new NonDefaultCol(), ECol.ReadonlyInUpdate),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Item.F.Code)), ECol.ReadonlyInUpdate),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Desc, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID,
									new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
									new ECol(ECol.NormalAccess, Fmt.SetPickFrom(TILocations.AllTemporaryStoragePickerTblCreator))),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID, new NonDefaultCol(), ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.VendorID.F.Code, DCol.Normal),
								TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), DCol.Normal, ECol.Normal }, MulticolumnQuantityUnitCostTotalLabels,
									TblRowNode.New(KB.K("Preferred Pricing"), new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), DCol.Normal, ECol.Normal },
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.Quantity, DCol.Normal),
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.UnitCost, new NonDefaultCol(), DCol.Normal),
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.Cost, DCol.Normal)
									),

									TblRowNode.New(KB.K("On Hand"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.OnHand, DCol.Normal, ECol.AllReadonly	/* calculated */),
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.UnitCost, DCol.Normal, ECol.AllReadonly	/* calculated */),
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.TotalCost, DCol.Normal, ECol.AllReadonly	/* calculated */)
									),

									TblRowNode.New(KB.K("On Order"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.OnOrder, new FeatureGroupArg(PurchasingGroup), DCol.Normal, ECol.AllReadonly	/* calculated */),
										TblLayoutNode.Empty(),
										TblLayoutNode.Empty()
									),

									TblRowNode.New(KB.K("On Reserve"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.OnReserve, DCol.Normal, ECol.AllReadonly	/* calculated */),
										TblLayoutNode.Empty(),
										TblLayoutNode.Empty()
									),

									TblRowNode.New(KB.K("Available"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
										TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.Available, DCol.Normal, ECol.AllReadonly	/* calculated */),
										TblLayoutNode.Empty(),
										TblLayoutNode.Empty()
									)
								),
								TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting)),
							BrowsetteTabNode.New(TId.ItemActivity, TId.TemporaryStorageAssignment, 
								TblColumnNode.NewBrowsette(TemporaryItemActivityBrowseTblCreator, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID, dsMB.Path.T.ItemActivity.F.ItemLocationID,
									DCol.Normal, new ECol(Fmt.SetId(ActivityBrowsetteId)))
							)
						),
						// Init the CostCenter from the selected WO's Expense Model record. The c/c should be always readonly,
						Init.New(new PathTarget(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.CostCenterID),
								new EditorPathValue(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.TemporaryStorageID.F.WorkOrderID.F.WorkOrderExpenseModelID.F.NonStockItemHoldingCostCenterID),
								null, TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
					);
			});
		}
		#endregion
		#region ItemPricingTbl
		public static CompositeTbl ItemPricingTbl(params BTbl.ICtorArg[] passedBtblArgs) {
			List<BTbl.ICtorArg> btblArgs = new List<BTbl.ICtorArg>(passedBtblArgs);

			btblArgs.AddRange(new BTbl.ICtorArg[] {
							BTbl.ListColumn(dsMB.Path.T.ItemPricing.F.ItemID.F.Code, BTbl.Contexts.List|BTbl.Contexts.ClosedPicker|BTbl.Contexts.SearchAndFilter),
							BTbl.ListColumn(dsMB.Path.T.ItemPricing.F.VendorID.F.Code), // Always show Vendor ID with Pricing
							BTbl.ListColumn(dsMB.Path.T.ItemPricing.F.Quantity),
							BTbl.ListColumn(dsMB.Path.T.ItemPricing.F.UnitCost),
							BTbl.ListColumn(dsMB.Path.T.ItemPricing.F.Cost)
			});
			return new CompositeTbl(dsMB.Schema.T.ItemPricing, TId.ItemPricing,
				new Tbl.IAttr[] {
					ItemsDependentGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(btblArgs.ToArray())
				},
				dsMB.Path.T.ItemPricing.F.TableEnum,
				new CompositeView(dsMB.Path.T.ItemPricing.F.ItemPriceID,
						CompositeView.PathAlias(dsMB.Path.T.ItemPricing.F.VendorID, dsMB.Path.T.ItemPrice.F.VendorID),
						CompositeView.PathAlias(dsMB.Path.T.ItemPricing.F.ItemID, dsMB.Path.T.ItemPrice.F.ItemID)),
				new CompositeView(dsMB.Path.T.ItemPricing.F.ReceiveItemPOID, ReadonlyView,
						CompositeView.PathAlias(dsMB.Path.T.ItemPricing.F.VendorID, dsMB.Path.T.ReceiveItemPO.F.POLineItemID.F.POLineID.F.PurchaseOrderID.F.VendorID),
						CompositeView.PathAlias(dsMB.Path.T.ItemPricing.F.ItemID, dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID)),
				new CompositeView(dsMB.Path.T.ItemPricing.F.ReceiveItemNonPOID, ReadonlyView,
						CompositeView.PathAlias(dsMB.Path.T.ItemPricing.F.VendorID, dsMB.Path.T.ReceiveItemNonPO.F.VendorID),
						CompositeView.PathAlias(dsMB.Path.T.ItemPricing.F.ItemID, dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID))
			);
		}
		#endregion
		#endregion

		#region Constructors and Property Initializers
		private TIItem() {
		}
		static TIItem() {
			ItemPriceTblCreator = new DelayedCreateTbl(() => ItemPriceTbl());
			#region ItemIssue Correction
			ItemIssueCorrectionTblCreator = new DelayedCreateTbl(delegate() {
				return ItemIssueTbl(true);
			});
			#endregion
			#region ItemTransfer Correction
			ItemTransferCorrectionTblCreator = new DelayedCreateTbl(delegate() {
				return ItemTransferTbl(true);
			});
			#endregion
			#region ItemActivity
			PermanentItemActivityBrowseTblCreator = ItemActivityTbl(true);
			TemporaryItemActivityBrowseTblCreator = ItemActivityTbl(false);
			#endregion
			#region ActiveTemporaryStorageEditTblCreator
			ActiveTemporaryStorageEditTblCreator = TemporaryStorageEditTbl(true);
			#endregion
			#region ActiveTemporaryItemLocationTblCreator
			ActiveTemporaryItemLocationTblCreator = TemporaryItemLocationTbl(true);
			#endregion
			#region AllTemporaryStorageEditTblCreator
			AllTemporaryStorageEditTblCreator = TemporaryStorageEditTbl(false);
			#endregion
			#region AllTemporaryItemLocationTblCreator
			AllTemporaryItemLocationTblCreator = TemporaryItemLocationTbl(false);
			TblRegistry.DefineEditTbl(dsMB.Schema.T.TemporaryItemLocation, AllTemporaryItemLocationTblCreator);
			#endregion
		}
		#endregion

		internal static void DefineTblEntries() {
			#region ItemReceiving
			{
				Key joinedCorrectionsCommand = KB.K("Correct");
				object quantityColumnId = dsMB.Path.T.ReceiveItemPO.F.Quantity.Key(); // to ensure all labels for Quantity use the same object
				// For the Receiving tab from the Item editor
				DefineBrowseTbl(dsMB.Schema.T.ItemReceiving, delegate() {
					return new CompositeTbl(dsMB.Schema.T.ItemReceiving, TId.ItemReceiving,
						new Tbl.IAttr[] {
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(BTbl.ListColumn(dsMB.Path.T.ItemReceiving.F.TableEnum),
								BTbl.ListColumn(dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.EffectiveDate, BTbl.Contexts.SortInitialAscending),
								BTbl.PerViewListColumn(dsMB.Path.T.ReceiveItemPO.F.Quantity.Key(), quantityColumnId),
								BTbl.ListColumn(dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.Cost),
								BTbl.SetTreeStructure(dsMB.Path.T.ItemReceiving.F.ParentID, 2)
							)
						},
						dsMB.Path.T.ItemReceiving.F.TableEnum,
						new CompositeView(dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemPOID,						// Table #0 - Permanent (with PO)
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity),
							CompositeView.PathAlias(dsMB.Path.T.ItemReceiving.F.ItemID,
													dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID)
						),
						new CompositeView(dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemPOID,						// Table #1 - Temporary (with PO)
							ReadonlyView,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity),
							CompositeView.PathAlias(dsMB.Path.T.ItemReceiving.F.ItemID,
													dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID)
						),
						new CompositeView(dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemNonPOID,						// Table #2 - Permanent (Non-PO)
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemNonPO.F.Quantity),
							CompositeView.PathAlias(dsMB.Path.T.ItemReceiving.F.ItemID,
													dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID)
						),
						new CompositeView(dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemNonPOID,						// Table #3 - Temporary (Non-PO)
							ReadonlyView,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemNonPO.F.Quantity),
							CompositeView.PathAlias(dsMB.Path.T.ItemReceiving.F.ItemID,
													dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID)
						),
						new CompositeView(TIReceive.ReceiveItemPOCorrectionTblCreator,													// Table #4 - Permanent (with PO) Correction
							dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemPOID,
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
							CompositeView.ContextualInit(new int[] {
									(int)ViewRecordTypes.ItemReceiving.ReceivePermanentPO,
									(int)ViewRecordTypes.ItemReceiving.ReceivePermanentPOCorrection
								},
								new CompositeView.Init(dsMB.Path.T.ReceiveItemPO.F.CorrectionID, dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectionID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ReceiveItemPO.F.POLineItemID), dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemPOID.F.POLineItemID)		// This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
							)
						),
						new CompositeView(TIReceive.ReceiveItemPOCorrectionTblCreator, dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemPOID, ReadonlyView,			// Table #5 - Temporary (with PO) Correction
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemPO.F.Quantity),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand)
						),
						new CompositeView(TIReceive.ReceiveItemNonPOCorrectionTblCreator,												// Table #6 - Permanent (Non-PO) Correction
							dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemNonPOID,
							NoNewMode,
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemNonPO.F.Quantity),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand),
							CompositeView.ContextualInit(new int[] {
									(int)ViewRecordTypes.ItemReceiving.ReceivePermanentNonPO,
									(int)ViewRecordTypes.ItemReceiving.ReceivePermanentNonPOCorrection
								},
								new CompositeView.Init(dsMB.Path.T.ReceiveItemNonPO.F.CorrectionID, dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectionID)
							)
						),
						new CompositeView(TIReceive.ReceiveItemNonPOCorrectionTblCreator, dsMB.Path.T.ItemReceiving.F.AccountingTransactionID.F.ReceiveItemNonPOID, ReadonlyView,	// Table #7 - Temporary (Non-PO) Correction
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ReceiveItemNonPO.F.Quantity),
							CompositeView.JoinedNewCommand(joinedCorrectionsCommand)
						)
					);
				});
			}
			#endregion

			#region Item
			TblLayoutNodeArray itemNodes = new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblFixedRecordTypeNode.New(),
					TblColumnNode.New(dsMB.Path.T.Item.F.Code, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Item.F.Desc, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Item.F.UnitOfMeasureID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitOfMeasure.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Item.F.ItemCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ItemCategory.F.Code)), ECol.Normal),
					TblGroupNode.New(KB.K("Totals for this item over all locations"), new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(StoreroomGroup), new NonDefaultCol(), DCol.Normal, ECol.Normal },
						TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, MulticolumnQuantityUnitCostTotalLabels,
							TblRowNode.New(KB.K("On Hand"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.Item.F.OnHand, DCol.Normal, ECol.AllReadonly	/* calculated */ ),
								TblColumnNode.New(dsMB.Path.T.Item.F.UnitCost, DCol.Normal, ECol.AllReadonly	/* calculated */ ),
								TblColumnNode.New(dsMB.Path.T.Item.F.TotalCost, DCol.Normal, ECol.AllReadonly	/* calculated */ )
							),
							TblRowNode.New(KB.K("On Order"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.Item.F.OnOrder, new FeatureGroupArg(PurchasingGroup), DCol.Normal, ECol.AllReadonly	/* calculated */ ),
								TblLayoutNode.Empty(),
								TblLayoutNode.Empty()
							),
							TblRowNode.New(KB.K("On Reserve"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.Item.F.OnReserve, DCol.Normal, ECol.AllReadonly	/* calculated */ ),
								TblLayoutNode.Empty(),
								TblLayoutNode.Empty()
							),
							TblRowNode.New(KB.K("Available"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.Item.F.Available, DCol.Normal, ECol.AllReadonly	/* calculated */ ),
								TblLayoutNode.Empty(),
								TblLayoutNode.Empty()
							)
						)
					),
					TblColumnNode.New(dsMB.Path.T.Item.F.Comment, DCol.Normal, ECol.Normal)));
			TblLayoutNodeArray itemNodeBrowsettes = new TblLayoutNodeArray(
				BrowsetteTabNode.New(TId.StoreroomAssignment, TId.Item,
					TblColumnNode.NewBrowsette(TILocations.PermanentItemLocationPickerTblCreator, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.TemporaryStorageAssignment, TId.Item,
					TblColumnNode.NewBrowsette(TILocations.ActiveTemporaryItemLocationBrowseTblCreator, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.TaskTemporaryStorageAssignment, TId.Item,
					TblColumnNode.NewBrowsette(TILocations.TemporaryTaskItemLocationBrowseTblCreator, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.ItemPricing, TId.Item,
					TblColumnNode.NewBrowsette(dsMB.Path.T.ItemPrice.F.ItemID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.PurchaseOrderLine, TId.Item,
					TblColumnNode.NewBrowsette(TIPurchaseOrder.POLineItemWithItemLocationInitTblCreator, dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.ItemReceiving, TId.Item,
					TblColumnNode.NewBrowsette(dsMB.Path.T.ItemReceiving.F.ItemID, DCol.Normal, ECol.Normal)),
				TblTabNode.New(KB.K("Usage"), KB.K("Display units using this item as a part"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.NewBrowsette(dsMB.Path.T.SparePart.F.ItemID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.TaskDemandItem, TId.Item,
					TblColumnNode.NewBrowsette(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.ItemID, DCol.Normal, ECol.Normal))
			);
			BTbl.ICtorArg[] columnList = new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.Item.F.Code),
				BTbl.ListColumn(dsMB.Path.T.Item.F.Desc, NonPerViewColumn),
				BTbl.ListColumn(dsMB.Path.T.Item.F.ItemCategoryID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.Item.F.OnHand, NonPerViewColumn, new BTbl.ListColumnArg.FeatureGroupArg(StoreroomGroup)),
				BTbl.ListColumn(dsMB.Path.T.Item.F.UnitCost, NonPerViewColumn, new BTbl.ListColumnArg.FeatureGroupArg(StoreroomGroup)),
				BTbl.ListColumn(dsMB.Path.T.Item.F.TotalCost, NonPerViewColumn, new BTbl.ListColumnArg.FeatureGroupArg(StoreroomGroup)),
				BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ItemReport))
			};
			var ItemEditorTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Item, TId.Item,
					new Tbl.IAttr[] {
						ItemsDependentGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new ETbl()
				},
				(TblLayoutNodeArray)itemNodes.Clone() + itemNodeBrowsettes
				);
			});
			DefineEditTbl(dsMB.Schema.T.Item, ItemEditorTblCreator);
			DefineBrowseTbl(dsMB.Schema.T.Item, delegate() {
				return new CompositeTbl(dsMB.Schema.T.Item, TId.Item,
					new Tbl.IAttr[] {
						ItemsDependentGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl((BTbl.ICtorArg[]) columnList.Clone())
					},
					null,
					CompositeView.ChangeEditTbl(ItemEditorTblCreator),
					CompositeView.AdditionalEditDefault(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.PermanentItemLocation)),
					CompositeView.AdditionalEditDefault(AllTemporaryItemLocationTblCreator),
					CompositeView.AdditionalEditDefault(ItemPriceTblCreator)
				);
			});
			RegisterExistingForImportExport(TId.Item, dsMB.Schema.T.Item);
			ItemAsSparePartPickerTblCreator = new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.Item, TId.Part,
					new Tbl.IAttr[] {
						ItemsDependentGroup,
						new AddCostTblLayoutNodeAttributesTbl(CommonNodeAttrs.ViewUnitSparePartCosts),
						new BTbl((BTbl.ICtorArg[]) columnList.Clone()),
						new ETbl()
					},
					(TblLayoutNodeArray)itemNodes
				);
			}
			);

			#endregion
			#region ItemAdjustment
			DefineTbl(dsMB.Schema.T.ItemAdjustment, delegate() {
				return ItemAdjustmentTbl();
			});
			#endregion
			#region ItemAdjustmentCode
			DefineTbl(dsMB.Schema.T.ItemAdjustmentCode, delegate() {
				return new Tbl(dsMB.Schema.T.ItemAdjustmentCode, TId.ItemAdjustmentCode,
				new Tbl.IAttr[] {
					InventoryGroup | ItemResourcesGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.ItemAdjustmentCode.F.Code), BTbl.ListColumn(dsMB.Path.T.ItemAdjustmentCode.F.Desc),
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ItemAdjustmentCodeReport))),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.ItemAdjustmentCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ItemAdjustmentCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ItemAdjustmentCode.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.ItemAdjustmentCode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ItemAdjustment, TId.ItemAdjustmentCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ItemAdjustment.F.ItemAdjustmentCodeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PhysicalCount, TId.ItemAdjustmentCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ItemCountValue.F.ItemAdjustmentCodeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.ItemAdjustmentCode, dsMB.Schema.T.ItemAdjustmentCode);
			#endregion
			#region ItemCategory
			DefineTbl(dsMB.Schema.T.ItemCategory, delegate() {
				return new Tbl(dsMB.Schema.T.ItemCategory, TId.ItemCategory,
				new Tbl.IAttr[] {
					ItemsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.ItemCategory.F.Code), BTbl.ListColumn(dsMB.Path.T.ItemCategory.F.Desc),
						BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.ItemCategory.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ItemCategory.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ItemCategory.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Item, TId.ItemCategory,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Item.F.ItemCategoryID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.ItemCategory, dsMB.Schema.T.ItemCategory);
			#endregion
			#region ItemCountValue
			DefineTbl(dsMB.Schema.T.ItemCountValue, delegate() {
				return ItemCountValueTbl();
			});
			// Physical Count import/export disabled until we handle processing of Init records during Import/Export to set field values from Init directies during import (e.g. UserID field)
			// RegisterExistingBrowserImport(TId.PhysicalCount, dsMB.Schema.T.ItemCountValue);
			#endregion
			#region ItemCountValueVoid
			DefineTbl(dsMB.Schema.T.ItemCountValueVoid, delegate() {
				return ItemCountValueVoidTbl();
			});
			#endregion
			#region ItemIssue
			DefineTbl(dsMB.Schema.T.ItemIssue, delegate() {
				return ItemIssueTbl(false);
			});
			#endregion
			#region ItemIssueCode
			DefineTbl(dsMB.Schema.T.ItemIssueCode, delegate() {
				return new Tbl(dsMB.Schema.T.ItemIssueCode, TId.ItemIssueCode,
				new Tbl.IAttr[] {
					InventoryGroup | ItemResourcesGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.ItemIssueCode.F.Code), BTbl.ListColumn(dsMB.Path.T.ItemIssueCode.F.Desc),
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ItemIssueCodeReport))),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.ItemIssueCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ItemIssueCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ItemIssueCode.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.ItemIssueCode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ItemIssue, TId.ItemIssueCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ItemIssue.F.ItemIssueCodeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.ItemIssueCode, dsMB.Schema.T.ItemIssueCode);
			#endregion
			#region ItemPrice
			DefineTbl(dsMB.Schema.T.ItemPrice, ItemPriceTblCreator);
			RegisterExistingForImportExport(TId.ItemPricing, dsMB.Schema.T.ItemPrice);
			#endregion
			#region ItemRestocking
			DefineBrowseTbl(dsMB.Schema.T.ItemRestocking,
				delegate() {
					Key restockJoinedCaption = KB.K("Restock from selected source");
					object codeColumnId = KB.I("ItemRestockingCodeId");
					object quantityColumnId = KB.I("ItemRestockingQuantityId");
					object uomColumnId = KB.I("ItemRestockingUOMId");
					object minimumColumnId = KB.I("ItemRestockingMinimumId");
					object unitCostColumnId = KB.I("ItemRestockingUnitCostId");
					return new CompositeTbl(dsMB.Schema.T.ItemRestocking, TId.ItemRestocking,
						new Tbl.IAttr[] {
							InventoryGroup,	// TODO: THis could also be argued to belong in the ResourceGroup which allows creation of temp stroage assignments. This browser would help fulfill ordering of such items.
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(dsMB.Path.T.ActualItemLocation.F.EffectiveMinimum.Key(), minimumColumnId),
								BTbl.PerViewListColumn(dsMB.Path.T.ActualItemLocation.F.Available.Key(), quantityColumnId),
								BTbl.PerViewListColumn(dsMB.Path.T.Item.F.UnitOfMeasureID.Key(), uomColumnId),
								BTbl.PerViewListColumn(dsMB.Path.T.ActualItemLocation.F.UnitCost.Key(), unitCostColumnId),
								BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.InventoryRestocking)),
								BTbl.SetTreeStructure(dsMB.Path.T.ItemRestocking.F.ParentID, 3)
							)
						},
						null,
						// Items that need restocking
						new CompositeView(dsMB.Path.T.ItemRestocking.F.ItemID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Item.F.Code),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.Item.F.Available),
						// TODO: BTbl.PerViewColumn(minimumColumnId, dsMB.Path.T.Item.F.Minimum), we would like to dispay the total of all active IL minimum values.
							BTbl.PerViewColumnValue(uomColumnId, dsMB.Path.T.Item.F.UnitOfMeasureID.F.Code),
							BTbl.PerViewColumnValue(unitCostColumnId, dsMB.Path.T.Item.F.UnitCost)
						),
						// ItemLocations that need restocking
						new CompositeView(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.Available),
							BTbl.PerViewColumnValue(minimumColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.EffectiveMinimum),
							BTbl.PerViewColumnValue(unitCostColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.UnitCost),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.Available).Lt(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum)))
						),
						// The driving view filters on Available < Minimum, which can only happen for *active* temporary locations, so we don't need the TemporaryStorageActive filtering that ActiveTemporaryItemLocationTblCreator provides.
						// If the temp storage is not active it is because the WO is closed, and so Demands are also not active and so Available cannot be less than zero. Since Minimum == 0 for temp storage this means the Available < Minimum will filter out all such ILs
						new CompositeView(AllTemporaryItemLocationTblCreator, dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.Available),
							// For Temp IL's the Effective Minimum is always zero so we could in theory leave the field blank but it seems visually cleaner to be explicit.
							BTbl.PerViewColumnValue(minimumColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.EffectiveMinimum),
							BTbl.PerViewColumnValue(unitCostColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.UnitCost),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.Available).Lt(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum)))
						),
						// ItemLocations that can provide stock
						// Note that although these views appear to have the same presentation as the previous two, the distinct view indices are used to enable/disable the record as context for some of the
						// additional New verbs below. Also, these records also contain different parent linkages: our parent linkages point to type 1 or 2 records, while the parent linkages
						// for types 1 and 2 point to Item (type 0) records.
						new CompositeView(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.Available),
							BTbl.PerViewColumnValue(minimumColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.EffectiveMinimum),
							BTbl.PerViewColumnValue(unitCostColumnId, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.UnitCost),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.Available).Gt(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum)))
						),
						// The driving view filters on OnHand > 0, which can only happen for *active* temporary locations, so we don't need the TemporaryStorageActive filtering that ActiveTemporaryItemLocationTblCreator provides.
						// If the temp storage is not active OnHand must be zero.
						new CompositeView(AllTemporaryItemLocationTblCreator, dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.Available),
							// For Temp IL's the Effective Minimum is always zero so we could in theory leave the field blank but it seems visually cleaner to be explicit.
							BTbl.PerViewColumnValue(minimumColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.EffectiveMinimum),
							BTbl.PerViewColumnValue(unitCostColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.UnitCost),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.Available).Gt(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum)))
						),
						// ItemPrice records that can supply
						new CompositeView(dsMB.Path.T.ItemRestocking.F.ItemPriceID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.ItemPrice.F.VendorID.F.Code),
							BTbl.PerViewColumnValue(unitCostColumnId, dsMB.Path.T.ItemPrice.F.UnitCost)
						),
						// Non-po purchasing history
						new CompositeView(dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemNonPOID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.ReceiveItemNonPO.F.VendorID.F.Code)
						// TODO: BTbl.PerViewColumn(unitCostColumnId, dsMB.Path.T.ReceiveItemNonPO.F.UnitCost), no Unit Cost on ReceiveItemNonPO, and no way to specify a client-side expr here
						),
						// PO purchasing history
						new CompositeView(dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemPOID,
							CompositeView.RecognizeByValidEditLinkage(),
							ReadonlyView,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID.F.Code)
						// TODO: BTbl.PerViewColumn(unitCostColumnId, dsMB.Path.T.ReceiveItemPO.F.UnitCost), there is no such field and client-side expressions are not allowed here.
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ItemTransfer),
							NoNewMode,
							CompositeView.NewCommandGroup(restockJoinedCaption),
							CompositeView.ContextualInit(
								new int[] { 3, 4 },
								new CompositeView.Init(dsMB.Path.T.ItemTransfer.F.FromItemLocationID, dsMB.Path.T.ItemRestocking.F.ItemLocationID),
								new CompositeView.Init(dsMB.Path.T.ItemTransfer.F.ToItemLocationID, dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID),
								new CompositeView.Init(dsMB.Path.T.ItemTransfer.F.Quantity,
									new BrowserCalculatedInitValue(dsMB.Path.T.ItemTransfer.F.Quantity.ReferencedColumn.EffectiveType,
										delegate(object[] inputs) {
											long needed = (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(long))
												- (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[1], typeof(long));
											long availableToTransfer = (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[3], typeof(long))
												- (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[2], typeof(long));
											long onHand = (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[4], typeof(long));
											if (needed < availableToTransfer)
												availableToTransfer = needed;
											if (onHand < availableToTransfer)
												availableToTransfer = onHand;
											return availableToTransfer;
										},
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.Available),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.Available),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.OnHand)
									)
								)
							)
						),
						// For purchasing, we use a special PO editor which also creates a POLineItem record in recordset 2. This tbl also has two hidden controls
						// tagged by ID which expect to contain cost basis information.
						// Note that the underlying View excludes any Correction records, and any possible source records that do not form a valid costing basis.
						CompositeView.ExtraNewVerb(TIPurchaseOrder.PurchaseOrderWithPOLineItemEditTbl,
							NoNewMode,
							CompositeView.NewCommandGroup(restockJoinedCaption),
							CompositeView.ContextualInit(new int[] { 1, 2 },
								new CompositeView.Condition[] {
									new CompositeView.Condition(new SqlExpression(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ItemPriceID).IsNotNull(),
										KB.K("This storage assignment has no preferred pricing"))
								},
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.ItemLocationID, 2), dsMB.Path.T.ItemRestocking.F.ItemLocationID),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.LineNumber, 2), new ConstantValue(1)),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.Quantity, 2),
									new BrowserCalculatedInitValue(dsMB.Path.T.ItemTransfer.F.Quantity.ReferencedColumn.EffectiveType,
										(inputs => (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(long)) - (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[1], typeof(long))),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ActualItemLocationID.F.Available)
									)
								),
								new CompositeView.Init(dsMB.Path.T.PurchaseOrder.F.VendorID, dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ItemPriceID.F.VendorID),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisCost), dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ItemPriceID.F.Cost),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisQuantity), dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ItemPriceID.F.Quantity),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderText, 2), dsMB.Path.T.ItemRestocking.F.ItemLocationID.F.ItemPriceID.F.PurchaseOrderText)
							),
						// TODO: The first three inits are the same in all the remaining cases here (they all refer to the Parent record which is the ItemLocation with the shortfall.
						// Furthermore they are the same as the first three inits in the previous case except for the rooting of the paths to the shortfall IL.
						// We should make these common somehow... Putting them in a single local variable is clumsy though; so is 3 local variables though...
							CompositeView.ContextualInit(5,
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.ItemLocationID, 2), dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.LineNumber, 2), new ConstantValue(1)),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.Quantity, 2),
									new BrowserCalculatedInitValue(dsMB.Path.T.ItemTransfer.F.Quantity.ReferencedColumn.EffectiveType,
										(inputs => (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(long)) - (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[1], typeof(long))),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.Available)
									)
								),
								new CompositeView.Init(dsMB.Path.T.PurchaseOrder.F.VendorID, dsMB.Path.T.ItemRestocking.F.ItemPriceID.F.VendorID),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisCost), dsMB.Path.T.ItemRestocking.F.ItemPriceID.F.Cost),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisQuantity), dsMB.Path.T.ItemRestocking.F.ItemPriceID.F.Quantity),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderText, 2), dsMB.Path.T.ItemRestocking.F.ItemPriceID.F.PurchaseOrderText)
							),
							CompositeView.ContextualInit(6,
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.ItemLocationID, 2), dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.LineNumber, 2), new ConstantValue(1)),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.Quantity, 2),
									new BrowserCalculatedInitValue(dsMB.Path.T.ItemTransfer.F.Quantity.ReferencedColumn.EffectiveType,
										(inputs => (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(long)) - (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[1], typeof(long))),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.Available)
									)
								),
								new CompositeView.Init(dsMB.Path.T.PurchaseOrder.F.VendorID, dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.VendorID),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisCost), dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedCost),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisQuantity), dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedQuantity),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderText, 2), dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ItemID.F.Code)
							),
							CompositeView.ContextualInit(7,
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.ItemLocationID, 2), dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.LineNumber, 2), new ConstantValue(1)),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.Quantity, 2),
									new BrowserCalculatedInitValue(dsMB.Path.T.ItemTransfer.F.Quantity.ReferencedColumn.EffectiveType,
										(inputs => (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[0], typeof(long)) - (long)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(inputs[1], typeof(long))),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum),
										new BrowserPathValue(dsMB.Path.T.ItemRestocking.F.ParentID.F.ItemLocationID.F.ActualItemLocationID.F.Available)
									)
								),
								new CompositeView.Init(dsMB.Path.T.PurchaseOrder.F.VendorID, dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemPOID.F.ReceiptID.F.PurchaseOrderID.F.VendorID),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisCost), dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedCost),
								new CompositeView.Init(new ControlTarget(TIPurchaseOrder.POLinePricingBasisQuantity), dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectedQuantity),
								new CompositeView.Init(new PathTarget(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderText, 2), dsMB.Path.T.ItemRestocking.F.AccountingTransactionID.F.ReceiveItemPOID.F.POLineItemID.F.POLineID.F.PurchaseOrderText)
							)
						)
					);
				}
			);
			#endregion
			#region ItemTransfer
			DefineTbl(dsMB.Schema.T.ItemTransfer, delegate() {
				return ItemTransferTbl(false);
			});
			#endregion
			#region PermanentStorage
			DefineTbl(dsMB.Schema.T.PermanentStorage, delegate() {
				return new Tbl(dsMB.Schema.T.PermanentStorage, TId.Storeroom,
				new Tbl.IAttr[] {
					StoreroomGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.Desc, NonPerViewColumn),
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.PermanentStorageReport))
					),
					new ETbl(
						ETbl.UseNewConcurrency(true),
						ETbl.CustomCommand(delegate(EditLogic editorLogic) {
							var ShowOnMap = new EditLogic.CommandDeclaration(KB.K("Show on map"), new ShowOnMapCommand(editorLogic));
							var ShowOnMapGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration{
							ShowOnMap
};
							return ShowOnMapGroup;
						})
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ExternalTag, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.GISLocation, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID,
							new NonDefaultCol(),
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
							new ECol(
								Fmt.SetPickFrom(TILocations.PermanentLocationPickerTblCreator),
								FilterOutContainedLocations(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID, dsMB.Path.T.LocationDerivations.F.LocationID)	
							)),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID,
							new DefaultOnlyCol(),
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
							new ECol(
								Fmt.SetPickFrom(TILocations.PermanentLocationPickerTblCreator)
							)),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.Rank, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.StoreroomAssignment, TId.Storeroom,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TemporaryStorageAndItem, TId.Storeroom,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID, dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ContainingLocationID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.Storeroom, dsMB.Schema.T.PermanentStorage);

			#endregion
			#region TemporaryStorage
			// These are not registered in tInfo to avoid accidentally getting the All or Active form when the other is the one wanted.
			// Instead the named Tbl Creators ActiveTemporaryStorageTblCreator and AllTemporaryStorageTblCreator should be used.
			//DefineTbl(dsMB.Schema.T.TemporaryStorage, TemporaryStorageTbl());
			#endregion
			#region PermanentItemLocation
			DefineTbl(dsMB.Schema.T.PermanentItemLocation, new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.PermanentItemLocation, TId.StoreroomAssignment,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						InventoryGroup,
						new BTbl(
							// Show the code only in the panel. Composite codes are typically too wide for ListColumns, and it is the Item most people are interested in
							BTbl.ListColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Desc, NonPerViewColumn),
							BTbl.ListColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.RelativeLocationID.F.PermanentStorageID.F.RelativeLocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.OnHand),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.PermanentInventoryLocation))
						),
						new ETbl()
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.Code, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Item.F.Code)), ECol.ReadonlyInUpdate),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Desc, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitOfMeasure.F.Code)), ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ExternalTag, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), new ECol(ECol.NormalAccess, Fmt.SetPickFrom(TILocations.PermanentStorageBrowseTblCreator))),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID, new NonDefaultCol(), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.Minimum, DCol.Normal, new ECol(Fmt.SetId(MinimumId))),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.Maximum, DCol.Normal, new ECol(Fmt.SetId(MaximumId))),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.VendorID.F.Code, DCol.Normal),

							TblMultiColumnNode.New(
								new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								MulticolumnQuantityUnitCostTotalLabels,

								TblRowNode.New(KB.K("Preferred Pricing"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.Quantity, DCol.Normal),
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.UnitCost, new NonDefaultCol(), DCol.Normal),
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.Cost, DCol.Normal)
								),

								TblRowNode.New(KB.K("On Hand"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.OnHand, DCol.Normal, ECol.AllReadonly	/* calculated */),
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.UnitCost, DCol.Normal, ECol.AllReadonly	/* calculated */),
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.TotalCost, DCol.Normal, ECol.AllReadonly	/* calculated */)
								),

								TblRowNode.New(KB.K("On Order"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.OnOrder, new FeatureGroupArg(PurchasingGroup), DCol.Normal, ECol.AllReadonly	/* calculated */),
									TblLayoutNode.Empty(),
									TblLayoutNode.Empty()
								),

								TblRowNode.New(KB.K("On Reserve"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.OnReserve, DCol.Normal, ECol.AllReadonly	/* calculated */),
									TblLayoutNode.Empty(),
									TblLayoutNode.Empty()
								),

								TblRowNode.New(KB.K("Available"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
									TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.Available, DCol.Normal, ECol.AllReadonly	/* calculated */),
									TblLayoutNode.Empty(),
									TblLayoutNode.Empty()
								)
							),
							TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg)),
						BrowsetteTabNode.New(TId.ItemActivity, TId.StoreroomAssignment,
							TblColumnNode.NewBrowsette(PermanentItemActivityBrowseTblCreator, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID, dsMB.Path.T.ItemActivity.F.ItemLocationID,
								DCol.Normal, new ECol(Fmt.SetId(ActivityBrowsetteId)))
						)
					),
					ItemLocationInit
				);
			}));
			RegisterExistingForImportExport(TId.StoreroomAssignment, dsMB.Schema.T.PermanentItemLocation);

			#endregion
			#region TemporaryItemLocation
			// These are not registered in tInfo to avoid accidentally getting the All or Active form when the other is the one wanted.
			// Instead the named Tbl Creators ActiveTemporaryItemLocationTblCreator and AllTemporaryItemLocationTblCreator should be used.
			//tInfo.Add(dsMB.Schema.T.TemporaryItemLocation, TemporaryItemLocationTbl());
			#endregion
			#region ActiveTemporaryStorageWithItemAssignments
			DefineBrowseTbl(dsMB.Schema.T.ActiveTemporaryStorageWithItemAssignments, new DelayedCreateTbl(delegate() {
				object descriptionColumnId = KB.I("DescriptionId");
				object codeColumnId = KB.I("ActiveTemporaryStorageWithItemAssignmentsCodeId");
				return new CompositeTbl(dsMB.Schema.T.ActiveTemporaryStorageWithItemAssignments, TId.TemporaryStorageAndItem,
					new Tbl.IAttr[] {
						ItemResourcesGroup,
						new BTbl(BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
							BTbl.PerViewListColumn(CommonDescColumnKey, descriptionColumnId),
							BTbl.SetTreeStructure(dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ParentID, 2)
						)
					},
					dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.TableEnum,
					new CompositeView(TIItem.ActiveTemporaryStorageEditTblCreator, dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.LocationID.F.TemporaryStorageID,
						BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.TemporaryStorage.F.LocationID.F.Desc),
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.TemporaryStorage.F.WorkOrderID.F.Number),
						CompositeView.PathAlias(dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ContainingLocationID,
							dsMB.Path.T.TemporaryStorage.F.ContainingLocationID)),
					new CompositeView(TIItem.ActiveTemporaryItemLocationTblCreator, dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID, NoNewMode,
						BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Desc),
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Code),
						CompositeView.ContextualInit((int)ViewRecordTypes.ActiveTemporaryStorageWithItemAssignments.TemporaryStorage,
							new CompositeView.Init(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID,
								dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.LocationID)),
						CompositeView.ContextualInit((int)ViewRecordTypes.ActiveTemporaryStorageWithItemAssignments.TemporaryItemLocation,
							new CompositeView.Init(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID,
								dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ItemLocationID.F.LocationID)),
						CompositeView.PathAlias(dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ContainingLocationID,
							dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.TemporaryStorageID.F.ContainingLocationID))
				);
			}));
			#endregion
			#region TemplateItemLocation
			DefineTbl(dsMB.Schema.T.TemplateItemLocation, new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.TemplateItemLocation, TId.TaskTemporaryStorageAssignment,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						SchedulingAndItemResourcesGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID.F.Desc, NonPerViewColumn),
							BTbl.ListColumn(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.TemplateInventoryLocation))
						),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Item.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID.F.Desc, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID,
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
							new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetPickFrom(TILocations.TemplateTemporaryStoragePickerTblCreator))),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID.F.TemplateTemporaryStorageID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemPriceID, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemPriceID.F.VendorID.F.Code, DCol.Normal),
						TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, MulticolumnQuantityUnitCostTotalLabels,
							TblRowNode.New(KB.K("Preferred Pricing"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemPriceID.F.Quantity, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemPriceID.F.UnitCost, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemPriceID.F.Cost, DCol.Normal)
							)
						)
					)
				);
			}));
			#endregion
			#region TemplateTemporaryStorage
			DefineTbl(dsMB.Schema.T.TemplateTemporaryStorage, new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.TemplateTemporaryStorage, TId.TaskTemporaryStorage,
					new Tbl.IAttr[] {
						SchedulingAndItemResourcesGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.TemplateTemporaryStorage.F.WorkOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.TemplateTemporaryStorage.F.LocationID.F.Desc)
						),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.TemplateTemporaryStorage.F.WorkOrderTemplateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderTemplate.F.Code)), ECol.ReadonlyInUpdate),
						TblColumnNode.New(dsMB.Path.T.TemplateTemporaryStorage.F.ContainingLocationID,
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
							new ECol(/* The xafdb file limits this to Permanent Locations */))
					)
				);
			}));
			#endregion
		}
	}
}
