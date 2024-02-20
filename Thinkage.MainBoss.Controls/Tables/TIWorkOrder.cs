#if DEBUG
#define SearchIsOneOf
#endif
using System;
using System.Linq;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Work Orders.
	/// </summary>
	public class TIWorkOrder : TIGeneralMB3 {
		#region Record-type providers
		#region ChargebackActivityProvider
		private static object[] ChargebackActivityValues = new object[] {
			(int)ViewRecordTypes.ChargebackActivity.NotSpecified,
			(int)ViewRecordTypes.ChargebackActivity.Chargeback,
			(int)ViewRecordTypes.ChargebackActivity.ChargebackCorrection
		};
		private static Key[] ChargebackActivityLabels = new Key[] {
			KB.K("Not Specified"),
			KB.TOi(TId.Chargeback),
			KB.K("Correction of Chargeback"),
		};
		public static EnumValueTextRepresentations ChargebackActivityProvider = new EnumValueTextRepresentations(ChargebackActivityLabels, null, ChargebackActivityValues);
		#endregion
		#region WorkOrderTemplateResourceProvider
		#region WorkOrderTemplateItemsProvider
		public static EnumValueTextRepresentations WorkOrderTemplateItemsProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.Item),
				KB.TOi(TId.TaskDemandItem),
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderItems.Item,
				(int)ViewRecordTypes.WorkOrderItems.DemandItem,
			}
		);
		#endregion
		#region WorkOrderTemplateInsideProvider
		public static EnumValueTextRepresentations WorkOrderTemplateInsideProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Unassigned Trade"),
				KB.TOi(TId.Trade),
				KB.TOi(TId.TaskDemandHourlyInside),
				KB.TOi(TId.TaskDemandPerJobInside)
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderTemplateInside.UnassignedTrade,
				(int)ViewRecordTypes.WorkOrderTemplateInside.Trade,
				(int)ViewRecordTypes.WorkOrderTemplateInside.DemandLaborInsideTemplate,
				(int)ViewRecordTypes.WorkOrderTemplateInside.DemandOtherWorkInsideTemplate
			}
		);
		#endregion
		#region WorkOrderTemplateOutsideProvider
		public static EnumValueTextRepresentations WorkOrderTemplateOutsideProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Unassigned Trade"),
				KB.TOi(TId.Trade),
				KB.TOi(TId.TaskDemandHourlyOutside),
				KB.TOi(TId.TaskDemandPerJobOutside)
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderTemplateOutside.UnassignedTrade,
				(int)ViewRecordTypes.WorkOrderTemplateOutside.Trade,
				(int)ViewRecordTypes.WorkOrderTemplateOutside.DemandLaborOutsideTemplate,
				(int)ViewRecordTypes.WorkOrderTemplateOutside.DemandOtherWorkOutsideTemplate
			}
		);
		#endregion
		#region WorkOrderTemplateMiscellaneousProvider
		public static EnumValueTextRepresentations WorkOrderTemplateMiscellaneousProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.MiscellaneousCost),
				KB.TOi(TId.TaskDemandMiscellaneousCost),
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderTemplateMiscellaneous.MiscellaneousWorkOrderCost,
				(int)ViewRecordTypes.WorkOrderTemplateMiscellaneous.DemandMiscellaneousWorkOrderCostTemplate
			}
		);
		#endregion
		#endregion
		#region WorkOrderItemsProvider
		public static EnumValueTextRepresentations WorkOrderItemsProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.Item),
				KB.TOi(TId.DemandItem),
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderItems.Item,
				(int)ViewRecordTypes.WorkOrderItems.DemandItem,
			}
		);
		#endregion
		#region WorkOrderInsideProvider
		public static EnumValueTextRepresentations WorkOrderInsideProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Unassigned Trade"),
				KB.TOi(TId.Trade),
				KB.TOi(TId.DemandHourlyInside),
				KB.TOi(TId.DemandPerJobInside)
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderInside.UnassignedTrade,
				(int)ViewRecordTypes.WorkOrderInside.Trade,
				(int)ViewRecordTypes.WorkOrderInside.DemandLaborInside,
				(int)ViewRecordTypes.WorkOrderInside.DemandOtherWorkInside
			}
		);
		#endregion
		#region WorkOrderOutsideProvider
		public static EnumValueTextRepresentations WorkOrderOutsideProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Unassigned Trade"),
				KB.TOi(TId.Trade),
				KB.TOi(TId.DemandHourlyOutside),
				KB.TOi(TId.DemandPerJobOutside),
				KB.K("Purchase Order Line Hourly"),
				KB.K("Purchase Order Line Per Job")
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderOutside.UnassignedTrade,
				(int)ViewRecordTypes.WorkOrderOutside.Trade,
				(int)ViewRecordTypes.WorkOrderOutside.DemandLaborOutside,
				(int)ViewRecordTypes.WorkOrderOutside.DemandOtherWorkOutside,
				(int)ViewRecordTypes.WorkOrderOutside.POLineLabor,
				(int)ViewRecordTypes.WorkOrderOutside.POLineOtherWork
			}
		);
		#endregion
		#region WorkOrderMiscellaneousProvider
		public static EnumValueTextRepresentations WorkOrderMiscellaneousProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.MiscellaneousCost),
				KB.TOi(TId.DemandMiscellaneousCost)
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderMiscellaneous.MiscellaneousWorkOrderCost,
				(int)ViewRecordTypes.WorkOrderMiscellaneous.DemandMiscellaneousWorkOrderCost
			}
		);
		#endregion
		#region WorkOrderGroupNameProvider
		public static EnumValueTextRepresentations WorkOrderGroupNameProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Unspecified Employee"),
				KB.K("Outside Work by Unspecified Vendor"),
				KB.K("Unspecified Trade")
			},
			null,
			new object[] {
				KnownIds.WorkOrderGroupNameProviderUnspecifiedInsideAgentId,
				KnownIds.WorkOrderGroupNameProviderUnspecifiedOutsideAgentId,
				KnownIds.WorkOrderGroupNameProviderUnspecifiedTradeId
			}
		);

		#endregion
		#region WorkOrderResourceActivityDateAsCodeWrapper
		private static System.Globalization.CultureInfo sortableDateCultureInfo = null;
		/// <summary>
		/// This method is used to wrap the effective date of activity on a WO resource to produce a "code" field that sorts chronologically.
		/// </summary>
		internal static BTbl.ListColumnArg.IAttr WorkOrderResourceActivityDateAsCodeWrapper() {
			return BTbl.ListColumnArg.WrapSource(delegate (Source originalSource) {
				if (sortableDateCultureInfo == null) {
					sortableDateCultureInfo = (System.Globalization.CultureInfo)Thinkage.Libraries.Application.InstanceFormatCultureInfo.Clone();
					System.Globalization.DateTimeFormatInfo dtfi = (System.Globalization.DateTimeFormatInfo)System.Globalization.DateTimeFormatInfo.InvariantInfo.Clone();
					dtfi.FullDateTimePattern = KB.I("yyyy/MM/dd HH:mm:ss.fff");
					sortableDateCultureInfo.DateTimeFormat = dtfi;
				}
				DateTimeTypeInfo dtti = (DateTimeTypeInfo)originalSource.TypeInfo;
				TypeFormatter fmtr = dtti.GetTypeFormatter(sortableDateCultureInfo);
				return new ConvertingSource<DateTime?, string>(originalSource,
					new StringTypeInfo(0, 23, 0, false, false, false),
					delegate (DateTime? val) {
						return val.HasValue ? fmtr.Format(val.Value) : null;
					}
				);
			});
		}
		#endregion
		#region WorkOrderPurchaseOrderLinkageProvider
		public static EnumValueTextRepresentations WorkOrderPurchaseOrderLinkageProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Explicit"),
				KB.K("Using Hourly Demand"),
				KB.K("Using Per Job Demand"),
				KB.K("Using Purchase item to Temporary Storage"),
				KB.K("Using Receive item to Temporary Storage")
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderPurchaseOrderLinkage.Explicit,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderLinkage.UsingLaborDemand,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderLinkage.UsingOtherWorkDemand,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderLinkage.UsingPOLineItemToTemporary,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderLinkage.UsingReceiveItemToTemporary
			}
		);
		#endregion
		#region WorkOrderPurchaseOrderViewProvider
		public static EnumValueTextRepresentations WorkOrderPurchaseOrderViewProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.WorkOrder),
				KB.TOi(TId.PurchaseOrder),
				KB.K("Explicit"),
				KB.K("Using Hourly Demand"),
				KB.K("Using Per Job Demand"),
				KB.K("Using Purchase item to Temporary Storage"),
				KB.K("Using Receive item to Temporary Storage")
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.WorkOrder,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.PurchaseOrder,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.Explicit,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.UsingLaborDemand,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.UsingOtherWorkDemand,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.UsingPOLineItemToTemporary,
				(int)ViewRecordTypes.WorkOrderPurchaseOrderView.UsingReceiveItemToTemporary
			}
		);
		#endregion
		#region WorkOrderTemplatePurchaseOrderTemplateLinkageProvider
		public static EnumValueTextRepresentations WorkOrderTemplatePurchaseOrderTemplateLinkageProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Explicit"),
				KB.K("Using Task Hourly Demand"),
				KB.K("Using Task Per Job Demand"),
				KB.K("Using Purchase Item Template to Template Storage Assignment"),
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.Explicit,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingLaborDemandTemplate,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingOtherWorkDemandTemplate,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingPOLineItemTemplateToTemplateItemLocation
			}
		);
		#endregion
		#region WorkOrderTemplatePurchaseOrderTemplateViewProvider
		public static EnumValueTextRepresentations WorkOrderTemplatePurchaseOrderTemplateViewProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.WorkOrder),
				KB.TOi(TId.PurchaseOrder),
				KB.K("Using Task Hourly Demand"),
				KB.K("Using Task Per Job Demand"),
				KB.K("Using Purchase Item Template to Template Storage Assignment"),
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateView.WorkOrderTemplate,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateView.PurchaseOrderTemplate,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateView.Explicit,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingLaborDemandTemplate,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingOtherWorkDemandTemplate,
				(int)ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingPOLineItemTemplateToTemplateItemLocation
			}
		);
		#endregion
		#region WorkOrderAssigneeProspectProvider
		public static EnumValueTextRepresentations WorkOrderAssigneeProspectProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.TOi(TId.Employee),
				KB.K("Vendor Service"),
				KB.TOi(TId.Requestor),
				KB.K("Unit Contact"),
				KB.K("Assignees"),
				KB.K("Vendor Sales"),
				KB.K("Vendor Payables"),
				KB.TOi(TId.BillableRequestor),
			},
			null,
			new object[] {
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.Employee,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.VendorService,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.Requestor,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.UnitContact,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.Assignees,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.VendorSales,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.VendorPayables,
				(int)ViewRecordTypes.WorkOrderAssigneeProspect.BillableRequestor,
			}
		);
		#endregion

		#endregion
		#region NodeIds
		private static readonly object WorkOrderStartDateEstimateId = KB.I("WorkOrderStartDateEstimateId");
		private static readonly object WorkOrderEndDateEstimateId = KB.I("WorkOrderEndDateEstimateId");
		private static readonly object WorkOrderDurationEstimateId = KB.I("WorkOrderDurationEstimateId");
		private static readonly object WorkOrderResourceTemplateCodeId = KB.I("WorkOrderResourceTemplateCodeId");
		private static readonly object WorkOrderMainFieldDisablerId = KB.I("Main Disabler");
		private static readonly object WorkOrderWorkIntervalDisablerId = KB.I("Interval Disabler");
		private static readonly object DemandFromStoreroomQuantityId = KB.I("DemandFromStoreroomQuantityId");
		private static readonly object DemandFromStoreroomUnitCostId = KB.I("DemandFromStoreroomUnitCostId");
		private static readonly object DemandFromStoreroomValueId = KB.I("DemandFromStoreroomValueId");
		private static readonly object DemandViaPurchaseQuantityId = KB.I("DemandViaPurchaseQuantityId");
		private static readonly object DemandViaPurchaseValueId = KB.I("DemandViaPurchaseValueId");
		private static readonly object DemandViaPurchaseUnitCostId = KB.I("DemandViaPurchaseUnitCostId");
		private static readonly object NetQuantityAvailableId = KB.I("NetQuantityAvailableId");
		private static readonly object OriginalQuantityNotYetUsedId = KB.I("OriginalQuantityNotYetUsedId");
		private static readonly object ExpenseClassItemId = KB.I("ExpenseClassItemId");
		private static readonly object ExpenseClassLaborId = KB.I("ExpenseClassLaborId");
		private static readonly object ExpenseClassMiscellaneousId = KB.I("ExpenseClassMiscellaneousId");
		private static readonly object NetWorkDurationId = KB.I("NetWorkDurationId");
		private static readonly object NetGenerateLeadTimeId = KB.I("NetGenerateLeadTimeId");
		private static readonly object LocalWorkDurationId = KB.I("LocalWorkDurationId");
		private static readonly object LocalGenerateLeadTimeId = KB.I("LocalGenerateLeadTimeId");
		private static readonly object BasisWorkDurationId = KB.I("BasisWorkDurationId");
		private static readonly object BasisGenerateLeadTimeId = KB.I("BasisGenerateLeadTimeId");

		//private static readonly object DowntimeId = KB.I("DowntimeId");
		internal static readonly object SourceWorkOrderPickerId = KB.I("SourceWorkOrderPickerId");  // Source WO for Task from WO editor


		internal static readonly Key costColumnId = KB.K("Cost");
		internal static readonly Key quantityColumnId = KB.K("Quantity");
		internal static readonly Key correctedCostColumnId = KB.K("Corrected Cost");
		internal static readonly Key correctedQuantityColumnId = KB.K("Corrected Quantity");
		internal static readonly Key EffectiveDateId = KB.K("Effective Date");

		#endregion

		#region Caption keys shared by several layout objects
		private static readonly Key ExpenseCategory = KB.K("Expense Category");
		private static readonly Key ActualizeGroup = KB.K("Actualize");
		private static readonly Key CorrectGroup = KB.K("Correct");
		private static readonly Key NewDemandGroup = KB.K("New Demand");
		private static readonly Key DemandedColumnKey = dsMB.Path.T.DemandItem.F.Quantity.Key();
		private static readonly Key OrderedQuantityColumnKey = dsMB.Path.T.DemandLaborOutside.F.OrderQuantity.Key();
		private static readonly Key DemandedCostColumnKey = dsMB.Path.T.Demand.F.CostEstimate.Key();
		private static readonly Key ActualColumnKey = dsMB.Path.T.Demand.F.DemandItemID.F.ActualQuantity.Key();
		private static readonly Key ActualCostColumnKey = dsMB.Path.T.Demand.F.ActualCost.Key();
		private static readonly object WorkOrderExpenseModelId = KB.I("WorkOrderExpenseModelId");
		static SimpleKey UseUnitWorkOrderExpenseModel = KB.K("Use Expense Model from Unit");
		static SimpleKey BecauseUsingUnitWorkOrderExpenseModel = KB.K("Readonly because Unit's Expense Model is being used");

		#endregion
		#region StartEndDurationCalculator
		private static Check StartEndDurationCalculator(object startCol, object durationCol, object endCol) {
			return new Check3<DateTime, TimeSpan, DateTime>(delegate (DateTime start, TimeSpan span, DateTime end) {
				if (checked(start + span - Extensions.TimeSpan.OneDay != end))
					return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Work Duration is inconsistent with work start and end dates")));
				return null;
			})
				.Operand1(startCol, delegate (TimeSpan span, DateTime end) {
					return checked(end + Extensions.TimeSpan.OneDay - span);
				})
				.Operand2(durationCol, delegate (DateTime start, DateTime end) {
					if (end < start)
						throw new GeneralException(KB.K("Work Start date cannot be after Work End date"));
					return checked(end - start + Extensions.TimeSpan.OneDay);
				})
				.Operand3(endCol, delegate (DateTime start, TimeSpan span) {
					return checked(start + span - Extensions.TimeSpan.OneDay);
				});
		}
		#endregion

		#region Named Tbls
		public static DelayedCreateTbl AssigneeBrowsetteFromWorkOrderTblCreator;
		public static DelayedCreateTbl WorkOrderBrowsetteFromAssigneeTblCreator;
		public static DelayedCreateTbl WorkOrderBrowsetteFromUnitTblCreator;
		public static DelayedCreateTbl WorkOrderAllBrowseTbl;
		public static DelayedCreateTbl WorkOrderInProgressAssignedToBrowseTbl;
		public static DelayedCreateTbl UnassignedWorkOrderBrowseTbl;
		public static DelayedCreateTbl WorkOrderAssignmentByAssigneeTblCreator;
		public static DelayedCreateTbl WorkOrderEditTblCreator;

		public static DelayedCreateTbl WorkOrderTemplateSpecializationEditTbl;
		public static DelayedCreateTbl WorkOrderTemplateEditTbl;
		public static DelayedCreateTbl WorkOrderTemplateFromWorkOrderEditTbl;

		public static DelayedCreateTbl WorkOrderEditorFromRequestTbl;   // Has the hooks to create the WorkOrderStateHistory and RequestedWorkOrder records
		public static DelayedCreateTbl WorkOrderDraftBrowseTbl;
		public static DelayedCreateTbl WorkOrderOpenBrowseTbl;
		public static DelayedCreateTbl AllWorkOrderTemporaryStoragePickerTblCreator;
		public static DelayedCreateTbl AllWorkOrderChargebackBrowsePickerTblCreator;
		public static DelayedCreateTbl WorkOrderClosedBrowseTbl;
		public static DelayedCreateTbl WorkOrderVoidBrowseTbl;
		public static DelayedCreateTbl WorkOrderOverdueBrowseTbl;
		private static readonly DelayedCreateTbl CloseWorkOrderEditTblCreator;

		public static readonly DelayedCreateTbl DemandLaborOutsideForPOLinePickerTblCreator = null;
		public static readonly DelayedCreateTbl DemandOtherWorkOutsideForPOLinePickerTblCreator = null;

		public static readonly DelayedCreateTbl DemandLaborOutsideTemplateForPOLineTemplatePickerTblCreator = null;
		public static readonly DelayedCreateTbl DemandOtherWorkOutsideTemplateForPOLineTemplatePickerTblCreator = null;

		public static readonly DelayedCreateTbl ActualItemBrowseTblCreator = null;
		public static readonly DelayedCreateTbl ActualItemCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualLaborInsideBrowseTblCreator = null;
		public static readonly DelayedCreateTbl ActualLaborInsideCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualLaborOutsideNonPOCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualLaborOutsidePOCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualOtherWorkInsideBrowseTblCreator = null;
		public static readonly DelayedCreateTbl ActualOtherWorkInsideCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualOtherWorkOutsideNonPOCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualOtherWorkOutsidePOCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ActualMiscellaneousWorkOrderCostBrowseTblCreator = null;
		public static readonly DelayedCreateTbl ActualMiscellaneousWorkOrderCostCorrectionTblCreator = null;
		public static readonly DelayedCreateTbl ChargebackLineCorrectionTblCreator;
		private static DelayedCreateTbl ChargebackLineEditTblCreator;
		private static DelayedCreateTbl DemandItemDefaultEditorTblCreator;
		private static DelayedCreateTbl DemandLaborInsideDefaultEditorTblCreator;
		private static DelayedCreateTbl DemandLaborOutsideDefaultEditorTblCreator;
		private static DelayedCreateTbl DemandOtherWorkInsideDefaultEditorTblCreator;
		private static DelayedCreateTbl DemandOtherWorkOutsideDefaultEditorTblCreator;
		private static DelayedCreateTbl DemandMiscellaneousWorkOrderCostDefaultEditorTblCreator;

		private static readonly DelayedCreateTbl AssociatedPurchaseOrdersTbl;
		private static readonly DelayedCreateTbl AssociatedPurchaseOrderTemplatesTbl;

		public static readonly DelayedCreateTbl AllChargebackTbl;

		public static DelayedCreateTbl FilteredWorkOrderExpenseCategoryTbl;
		public static DelayedCreateTbl WorkOrderExpenseModelEntryAsCategoryPickerTbl;
		public static DelayedCreateTbl EmployeeTbl;

		public static readonly DelayedCreateTbl TaskPickerTblCreator = null;
		#endregion
		#region State History with UI definition
		public static DelayedConstruction<StateHistoryUITable> WorkOrderHistoryTable = new DelayedConstruction<StateHistoryUITable>(delegate () {
			return new StateHistoryUITable(MB3Client.WorkOrderHistoryTable, dsMB.Schema.T.WorkOrderState.F.CanModifyOperationalFields, FindDelayedEditTbl(dsMB.Schema.T.WorkOrderStateHistory), CloseWorkOrderEditTblCreator);
		});
		#endregion
		#region Tbl-creator functions
		#region WorkOrder browser Tbl attributes
		private static BTbl.ICtorArg WorkOrderNumberListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.Number);
		private static BTbl.ICtorArg WorkOrderSubjectListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.Subject);
		private static BTbl.ICtorArg WorkOrderUnitListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.Code, BTbl.ListColumnArg.Contexts.ClosedCombo | BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter);
		private static BTbl.ICtorArg WorkOrderStatusListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateHistoryStatusID.F.Code);
		private static BTbl.ICtorArg WorkOrderStateListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.Code, Fmt.SetDynamicSizing());
		private static BTbl.ICtorArg WorkOrderClosingCodeListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.CloseCodeID.F.Code, BTbl.ListColumnArg.Contexts.List | BTbl.ListColumnArg.Contexts.SearchAndFilter);
		private static BTbl.ICtorArg WorkOrderStateAuthorListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.UserID.F.ContactID.F.Code, BTbl.ListColumnArg.Contexts.SearchAndFilter);
		private static BTbl.ICtorArg WorkOrderCurrentStateHistoryEffectiveDateListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.EffectiveDate, BTbl.ListColumnArg.Contexts.SortInitialDescending);
		private static BTbl.ICtorArg WorkOrderOverdueListColumn = BTbl.ListColumn(KB.K("Overdue"), new TblQueryExpression(WOStatisticCalculation.Overdue(dsMB.Path.T.WorkOrder, dsMB.Path.T.WorkOrder.F.Id.L.WorkOrderExtras.WorkOrderID)), null, BTbl.ListColumnArg.Contexts.SortInitialDescending, Fmt.SetUsage(DBI_Value.UsageType.IntervalDays), Fmt.SetColor(System.Drawing.Color.Red));

		// The priority column uses an alternative sort key which biases the non-null values:
		// null -> int.MaxValue, int.MinValue -> null, and all others -> value-1
		// This causes null to sort as lower-than-lowest (because a big ranking number means low priority), and also that the default ascending sort puts highest priority at the top of the list.
		private static BTbl.ICtorArg WorkOrderPriorityListColumnSortValue = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID.F.Code.Key(), dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID.F.Rank, BTbl.ListColumnArg.Contexts.TaggedValueProvider | BTbl.ListColumnArg.Contexts.SortNormal,
			BTbl.ListColumnArg.WrapSource((originalSource) => new ConvertingSource<int?, int?>(originalSource, dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID.F.Rank.ReferencedColumn.EffectiveType,
				(value) => (value.HasValue ? value.Value == int.MinValue ? null : value - 1 : int.MaxValue))),
				new CustomizationOptions(CustomizationOptions.HidingOptions.HideableButNoUI)); // NoHideUi on the SortValue column since the Code value column is also listed; only provide one opportunity to hide the listcolumn
		private static BTbl.ICtorArg WorkOrderPriorityListColumn = BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID.F.Code, BTbl.ListColumnArg.Contexts.List | BTbl.ListColumnArg.Contexts.SearchAndFilter | BTbl.ListColumnArg.Contexts.SortAlternativeValue);
		/// <summary>
		/// Return a WO browse/pick tbl
		/// </summary>
		/// <param name="tableId">The Table Id to be used, which determines the form caption for full browsers (possibly called by "Pick with Full Browser" from picker)</param>
		/// <param name="editTblCreator">The Tbl to use for all the composite views, which controls panel contents, and also what form the Edit/View/New commands use</param>
		/// <param name="reportTblCreatorDelegate">A delegate to paee to new DelayedCreatTbl for a NewRemotePTbl attribute, which defines the Print button if any. Note only full browsers have a Print button</param>
		/// <param name="featureGroup">The FeatureGroup to use for the Tbl; normally none is used and the Tbl is shown/hidden based on the visibility of its composite views</param>
		/// <param name="tableNameForPermissions">The name of a table to use for rights rather than WorkOrder (as in Table.WorkOrder.View right)</param>
		/// <param name="classifyByState">Classify the work orders by state and show row icons to identify these states (use whenever the filter can return WOs in several states)</param>
		/// <param name="allowNewWO">Allow creation of a new Work Order from scratch or from a task</param>
		/// <param name="includeDefaultViews">Include the views that link to related tables to give them Defaults for Xxx tabs in the main form</param>
		/// <param name="listColumns">Columns to show in the list. Default is Number, Priority (including special sort value), Statue, Subject, Author</param>
		/// <param name="extraBTblAttributes">Extra BTbl attributes, which might be a filter specification or an additional browser verb</param>
		/// 
		/// 
		/// <returns></returns>
		private static Tbl StandardWorkOrderBrowser(TId tableId, DelayedCreateTbl editTblCreator = null, DelayedCreateTbl.Delegate reportTblCreatorDelegate = null, FeatureGroup featureGroup = null, string tableNameForPermissions = null,
			bool classifyByState = false, bool allowNewWO = false, bool includeDefaultViews = false, BTbl.ICtorArg[] listColumns = null, params BTbl.ICtorArg[] extraBTblAttributes) {
			if (editTblCreator == null)
				editTblCreator = WorkOrderEditTblCreator;
			if (listColumns == null)
				listColumns = new[] { WorkOrderNumberListColumn, WorkOrderPriorityListColumnSortValue, WorkOrderPriorityListColumn, WorkOrderStatusListColumn, WorkOrderSubjectListColumn, WorkOrderStateAuthorListColumn };

			var btblArgs = new List<BTbl.ICtorArg>(listColumns);
			btblArgs.Add(MB3BTbl.HasStateHistory(WorkOrderHistoryTable));
#if SearchIsOneOf  // Enable this to give a testbed for checked combos etc.
			btblArgs.Add(BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.UnitLocationID, BTbl.ListColumnArg.Contexts.SearchAndFilter));
#endif
			if (extraBTblAttributes != null)
				btblArgs.AddRange(extraBTblAttributes);

			var tblAttrs = new List<Tbl.IAttr>();
			tblAttrs.Add(new BTbl(btblArgs.ToArray()));
			if (reportTblCreatorDelegate != null)
				tblAttrs.Add(TIReports.NewRemotePTbl(new DelayedCreateTbl(reportTblCreatorDelegate)));
			if (featureGroup != null)
				tblAttrs.Add(featureGroup);
			if (tableNameForPermissions != null)
				tblAttrs.Add(new UseNamedTableSchemaPermissionTbl(tableNameForPermissions));

			var views = new List<CompositeView>();
			if (classifyByState) {
				views.Add(CompositeView.ChangeEditTbl(editTblCreator, allowNewWO ? null : OnlyViewEdit,
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.WorkOrderStateDraftId).Eq(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID))),
						CompositeView.IdentificationOverride(TId.DraftWorkOrder)));
				views.Add(CompositeView.ChangeEditTbl(editTblCreator, OnlyViewEdit,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.WorkOrderStateOpenId).Eq(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID))),
						CompositeView.IdentificationOverride(TId.OpenWorkOrder)));
				views.Add(CompositeView.ChangeEditTbl(editTblCreator, OnlyViewEdit,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.WorkOrderStateClosedId).Eq(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID))),
						CompositeView.IdentificationOverride(TId.ClosedWorkOrder)));
				views.Add(CompositeView.ChangeEditTbl(editTblCreator, OnlyViewEdit,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.WorkOrderStateVoidId).Eq(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID))),
						CompositeView.IdentificationOverride(TId.VoidWorkOrder)));
			}
			else
				views.Add(CompositeView.ChangeEditTbl(editTblCreator, allowNewWO ? null : OnlyViewEdit));


			if (allowNewWO)
				views.Add(CompositeView.ExtraNewVerb(WorkOrderFromTemplateEditLogic.WorkOrderFromTemplateTbl,
					CompositeView.IdentificationOverride(TId.WorkOrderFromTask)));

			// allow NewTaskFromWO
			views.Add(CompositeView.ExtraNewVerb(WorkOrderTemplateFromWorkOrderEditTbl, NoNewMode,
				CompositeView.ContextualInit(classifyByState ? new int[] { 0, 1, 2, 3 } : new int[] { 0 },
				new CompositeView.Init(new ControlTarget(SourceWorkOrderPickerId), dsMB.Path.T.WorkOrder.F.Id))));

			if (includeDefaultViews) {
				Key GroupDefaultDemands = TId.WorkOrderResourceDemand.Compose(Tbl.TblIdentification.TablePhrase_DefaultsFor);
				views.Add(CompositeView.AdditionalEditDefault(DemandItemDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(DemandLaborInsideDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(DemandLaborOutsideDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(DemandOtherWorkInsideDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(DemandOtherWorkOutsideDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(DemandMiscellaneousWorkOrderCostDefaultEditorTblCreator, CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.WorkOrderStateHistory)));
			}

			return new CompositeTbl(dsMB.Schema.T.WorkOrder, tableId,
				tblAttrs.ToArray(),
				null,   // no record type
				views.ToArray()
			);
		}
		#endregion
		#region WorkOrderEdit
		/// <summary>
		/// The 'rules' say if you have WorkOrderFulfillment OR WorkOrderClose "as well as" WorkOrderAssigneSelf, you can use the "Self Assign" operation.
		/// We do this by knowing the WorkOrderAssignSelf role (only) allows 'Create' on the 'UnassignedWorkOrder' view as a table op.
		/// We explicitly create disablers for each of the required table rights. (We 'know' that WorkOrderFulfillment and WorkOrderClose have 'Create' on the WorkOrderStateHistory table)
		/// yech!
		/// </summary>
		/// <returns></returns>
		private static List<IDisablerProperties> SelfAssignDisablers() {
			ITblDrivenApplication app = Application.Instance.GetInterface<ITblDrivenApplication>();
			TableOperationRightsGroup rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild("UnassignedWorkOrder");
			var list = new List<IDisablerProperties>();
			list.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
			rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild(dsMB.Schema.T.WorkOrderStateHistory.Name);
			list.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
			return list;
		}
		private static Key SelfAssignCommand = KB.K("Self Assign");
		private static Key SelfAssignTip = KB.K("Add yourself as an assignee to this Work Order");
		private static void SelfAssignmentEditor(CommonLogic el, object requestID) {
			object requestAssigneeID;
			using (dsMB ds = new dsMB(el.DB)) {
				var row = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.WorkOrderAssignee,
					// Although a User row might be Hidden, such a row could not be the UserRecordID.
					// However, a deleted Contact could still name a User record so we check to ensure that the user's Contact record is not Hidden.
					new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(Application.Instance.GetInterface<Thinkage.Libraries.DBAccess.IApplicationWithSingleDatabaseConnection>().UserRecordID))
						.And(new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.ContactID.F.Hidden).IsNull()));
				if (row == null)
					throw new GeneralException(KB.K("You are not registered as a Work Order Assignee"));
				requestAssigneeID = ((dsMB.WorkOrderAssigneeRow)row).F.Id;
			}
			var initList = new List<TblActionNode>();
			initList.Add(Init.OnLoadNew(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, new ConstantValue(requestID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID, new ConstantValue(KnownIds.WorkOrderStateOpenId)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID, new EditorPathValue(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateHistoryStatusID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.WorkOrderStateHistory.F.Comment, new ConstantValue(Strings.Format(KB.K("Self assigned")))));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID, 1, new EditorPathValue(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID, 1, new ConstantValue(requestAssigneeID)));
			Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().GetInterface<ITblDrivenApplication>().PerformMultiEdit(el.CommonUI.UIFactory, el.DB, TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.WorkOrderStateHistory),
				EdtMode.New,
				new[] { new object[] { } },
				ApplicationTblDefaults.NoModeRestrictions,
				new[] { initList },
				((ICommonUI)el.CommonUI).Form, el.CallEditorsModally,
				null);
		}

		#region - Layout Nodes
		private static TblLayoutNodeArray WorkOrderNodes() {
			return new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Number, new NonDefaultCol(), DCol.Normal, ECol.ReadonlyInUpdate, Fmt.SetColor(System.Drawing.Color.Green)),
					TblVariableNode.New(KB.K("Number Format"), dsMB.Schema.V.WOSequenceFormat, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Number Sequence"), dsMB.Schema.V.WOSequence, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Subject, DCol.Normal, ECol.Normal),
					CurrentStateHistoryGroup(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID,
						dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate,
						dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.Code,
						dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID.F.Code),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.PMGenerationBatchID, new NonDefaultCol(), ECol.AllReadonly),
					TblMultiColumnNode.New(
						new TblLayoutNode.ICtorArg[] { DCol.Normal },
						new Key[] { KB.K("Type"), dsMB.Path.T.PMGenerationBatch.F.EndDate.Key(), dsMB.Path.T.PMGenerationBatch.F.EntryDate.Key() },
						TblRowNode.New(KB.K("Maintenance"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							IsPreventiveValueNodeBuilder(dsMB.Path.T.WorkOrder, new NonDefaultCol(), DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.PMGenerationBatchID.F.EndDate, new NonDefaultCol(), DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.PMGenerationBatchID.F.EntryDate, new NonDefaultCol(), DCol.Normal)
						)
					),
					// No value is provided when editing so commented out for now					IsPreventiveValueNodeBuilder(dsMB.Path.T.WorkOrder, new NonDefaultCol(), ECol.AllReadonly),
					TIContact.SingleRequestorGroup(dsMB.Path.T.WorkOrder.F.RequestorID, true),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.UnitLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
					TblMultiColumnNode.New(
						new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal, new NonDefaultCol() },
						new Key[] { KB.K("Start Estimate"), KB.K("End Estimate"), KB.K("Duration"), KB.K("Due Date") },
						TblRowNode.New(KB.K("Work Period"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.StartDateEstimate, DCol.Normal, new ECol(Fmt.SetId(WorkOrderStartDateEstimateId))),
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.EndDateEstimate, DCol.Normal, new ECol(Fmt.SetId(WorkOrderEndDateEstimateId))),
							TblUnboundControlNode.New(KB.K("Work Duration"), new IntervalTypeInfo(new TimeSpan(1, 0, 0, 0, 0), new TimeSpan(1, 0, 0, 0, 0), TimeSpan.MaxValue, false),
								new ECol(Fmt.SetId(WorkOrderDurationEstimateId), ECol.RestrictPerGivenPath(dsMB.Path.T.WorkOrder.F.EndDateEstimate, 0))),
							// The following isn't valid because we have no SQL type for time spans that comes back in the data set as a TimeSpan.
							//TblQueryValueNode.New(KB.K("Work Duration"), new TblQueryExpression(new SqlExpression(dsMB.Path.T.WorkOrder.F.EndDateEstimate).Minus(new SqlExpression(dsMB.Path.T.WorkOrder.F.StartDateEstimate))), DCol.Normal),
							TblInitSourceNode.New(KB.K("Work Duration"),
								new BrowserCalculatedInitValue(new IntervalTypeInfo(new TimeSpan(1, 0, 0, 0, 0), new TimeSpan(1, 0, 0, 0, 0), TimeSpan.MaxValue, false),
									values => (DateTime?)values[0] - (DateTime?)values[1] + new TimeSpan(1, 0, 0, 0),
									new BrowserPathValue(dsMB.Path.T.WorkOrder.F.EndDateEstimate),
									new BrowserPathValue(dsMB.Path.T.WorkOrder.F.StartDateEstimate)),
								DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.WorkDueDate, DCol.Normal, ECol.Normal)
						)
					),
					TblVariableNode.New(KB.K("Work Duration"), dsMB.Schema.V.WODefaultDuration, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblQueryValueNode.New(KB.K("Overdue"), new TblQueryExpression(WOStatisticCalculation.Overdue(dsMB.Path.T.WorkOrder, dsMB.Path.T.WorkOrder.F.Id.L.WorkOrderExtras.WorkOrderID)), DCol.Normal, Fmt.SetUsage(DBI_Value.UsageType.IntervalDays), Fmt.SetColor(System.Drawing.Color.Red)),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.WorkCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkCategory.F.Code)), ECol.Normal),
					TblUnboundControlNode.New(UseUnitAccessCode, BoolTypeInfo.NonNullUniverse, new ECol(Fmt.SetId(UseUnitAccessCode), Fmt.SetIsSetting(false), ECol.RestrictPerGivenPath(dsMB.Path.T.WorkOrder.F.AccessCodeID, 0)), new NonDefaultCol()),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.AccessCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.AccessCode.F.Code)), new ECol(Fmt.SetId(AccessCodeId))),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderPriority.F.Code)), ECol.Normal),
					TblUnboundControlNode.New(UseUnitWorkOrderExpenseModel, BoolTypeInfo.NonNullUniverse, new ECol(Fmt.SetId(UseUnitWorkOrderExpenseModel), Fmt.SetIsSetting(false), ECol.RestrictPerGivenPath(dsMB.Path.T.WorkOrder.F.WorkOrderExpenseModelID, 0)), new NonDefaultCol(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.WorkOrderExpenseModelID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModel.F.Code)), new ECol(Fmt.SetId(WorkOrderExpenseModelId)), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.ProjectID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Project.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Description, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.ClosingComment, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Downtime, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.CloseCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CloseCode.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.SelectPrintFlag, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.WorkOrderAssignment, TId.WorkOrder,
					TblColumnNode.NewBrowsette(AssigneeBrowsetteFromWorkOrderTblCreator, dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.ServiceContract, TId.WorkOrder,
					TblColumnNode.NewBrowsette(TIUnit.WorkOrderServiceContractsBrowseTbl, dsMB.Path.T.WorkOrder.F.UnitLocationID, dsMB.Path.T.UnitServiceContract.F.UnitLocationID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.WorkOrderResource, TId.WorkOrder, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblMultiColumnNode.New(
						new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal, new AddCostCol(CommonNodeAttrs.ViewTotalWorkOrderCosts) },
						MulticolumnDemandActualLabels,
						TblRowNode.New(KB.K("Total"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal, new AddCostCol(CommonNodeAttrs.ViewTotalWorkOrderCosts), new FeatureGroupArg(AnyResourcesGroup) },
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.TotalDemand, new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInBrowsetteArea)), ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.WorkOrder.F.TotalActual, new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInBrowsetteArea)), ECol.AllReadonly)
						)
					),
					BrowsetteTabNode.New(TId.WorkOrderItem, TId.WorkOrder,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderItems.F.DemandID.F.WorkOrderID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrderInside, TId.WorkOrder,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderInside.F.DemandID.F.WorkOrderID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrderOutside, TId.WorkOrder,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderOutside.F.DemandID.F.WorkOrderID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrderMiscellaneousExpense, TId.WorkOrder,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderMiscellaneous.F.WorkOrderID, DCol.Normal, ECol.Normal))
				),
				BrowsetteTabNode.New(TId.TemporaryStorage, TId.WorkOrder,
					// We don't use TILocations.TemporaryItemLocationBrowseTblCreator because it does not permit direct creation of temp storage nor would it show any as primary
					TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemporaryStorage.F.WorkOrderID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.Meter, TId.WorkOrder,
					TblColumnNode.NewBrowsette(TIUnit.WorkOrderMeterReadingBrowseTbl, DCol.Normal, ECol.Normal,
						new BrowsetteFilterBind(TIUnit.WorkOrderIDValueId, dsMB.Path.T.WorkOrder.F.Id),
						new BrowsetteFilterBind(TIUnit.WorkOrderUnitLocationValueId, dsMB.Path.T.WorkOrder.F.UnitLocationID)
					)
				),
				BrowsetteTabNode.New(TId.PurchaseOrder, TId.WorkOrder,
					TblColumnNode.NewBrowsette(AssociatedPurchaseOrdersTbl, dsMB.Path.T.WorkOrderPurchaseOrderView.F.LinkedWorkOrderID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.Chargeback, TId.WorkOrder,
					TblColumnNode.NewBrowsette(AllChargebackTbl, dsMB.Path.T.Chargeback.F.WorkOrderID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.Request, TId.WorkOrder,
					TblColumnNode.NewBrowsette(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.WorkOrderStateHistory, TId.WorkOrder,
					TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, DCol.Normal, ECol.Normal)
				),
				TblTabNode.New(KB.K("Printed Form"), KB.K("Display settings for printed work orders"), new TblLayoutNode.ICtorArg[] { new DefaultOnlyCol(), DCol.Normal, ECol.Normal },
					TblVariableNode.New(KB.K("Additional Labor/Material Lines"), dsMB.Schema.V.WOFormAdditionalBlankLines, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Additional Information"), dsMB.Schema.V.WOFormAdditionalInformation, new DefaultOnlyCol(), DCol.Normal, ECol.Normal)
				)
			);
		}
		#endregion
		private static DelayedCreateTbl WorkOrderEditTbl(FeatureGroup featureGroup, bool includeAssignToSelfCommand = false) {
			return new DelayedCreateTbl(delegate () {
				List<ETbl.ICtorArg> etblArgs = new List<ETbl.ICtorArg>();
				etblArgs.Add(MB3ETbl.HasStateHistoryAndSequenceCounter(dsMB.Path.T.WorkOrder.F.Number, dsMB.Schema.T.WorkOrderSequenceCounter, dsMB.Schema.V.WOSequence, dsMB.Schema.V.WOSequenceFormat, WorkOrderHistoryTable));
				etblArgs.Add(ETbl.EditorDefaultAccess(false));
				etblArgs.Add(ETbl.EditorAccess(true, EdtMode.Edit, EdtMode.View, EdtMode.Clone, EdtMode.EditDefault, EdtMode.ViewDefault, EdtMode.New));
				etblArgs.Add(ETbl.Print(TIReports.SingleWorkOrderFormReport, dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID));
				if (includeAssignToSelfCommand) {
					etblArgs.Add(ETbl.CustomCommand(
							delegate (EditLogic el) {
								var group = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								group.Add(new EditLogic.CommandDeclaration(
									SelfAssignCommand,
									new MultiCommandIfAllEnabled(
										EditLogic.StateTransitionCommand.NewSameTargetState(el,
											SelfAssignTip,
											delegate () {
												SelfAssignmentEditor(el, el.RootRowIDs[0]);
											},
											el.AllStatesWithExistingRecord.ToArray()),
										SelfAssignDisablers().ToArray())
									));
								return group;
							}
					));
				}

				return new Tbl(dsMB.Schema.T.WorkOrder, TId.WorkOrder,
					new Tbl.IAttr[] {
						featureGroup,
						new ETbl(etblArgs.ToArray()),
						TIReports.NewRemotePTbl(TIReports.WorkOrderFormReport)
					},
					WorkOrderNodes(),
					Init.LinkRecordSets(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, 1, dsMB.Path.T.WorkOrder.F.Id, 0),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.WorkOrderStateHistory.F.UserID, 1), new UserIDValue()),
					Init.OnLoadNew(new ControlTarget(WorkOrderDurationEstimateId), new VariableValue(dsMB.Schema.V.WODefaultDuration)),
					StartEndDurationCalculator(WorkOrderStartDateEstimateId, WorkOrderDurationEstimateId, WorkOrderEndDateEstimateId),
					// Copy the WO Expense and AccessCode from unit if the checkbox is checked
					Init.New(new ControlTarget(WorkOrderExpenseModelId), new EditorPathValue(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.WorkOrderExpenseModelID), new Thinkage.Libraries.Presentation.ControlValue(UseUnitWorkOrderExpenseModel), TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone)),
					Init.New(new ControlTarget(AccessCodeId), new EditorPathValue(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.AccessCodeID), new Thinkage.Libraries.Presentation.ControlValue(UseUnitAccessCode), TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone)),
					// Arrange for WO / Access Code choices to be readonly if using the Unit's values
					Init.Continuous(new ControlReadonlyTarget(WorkOrderExpenseModelId, BecauseUsingUnitWorkOrderExpenseModel), new Thinkage.Libraries.Presentation.ControlValue(UseUnitWorkOrderExpenseModel)),
					Init.Continuous(new ControlReadonlyTarget(AccessCodeId, BecauseUsingUnitAccessCode), new Thinkage.Libraries.Presentation.ControlValue(UseUnitAccessCode))
				);
			});
		}
		#endregion
		#region Common Actual code
		private static void AddActualToCCSetup(AccountingTransactionDerivationTblCreator creator, DBI_Path baseDemandPath) {
			if (!creator.Correction)
				// Look up the To c/c from the work order expense model.
				creator.Actions.Add(Init.OnLoadNewClone(dsMB.Path.T.AccountingTransaction.F.ToCostCenterID.ReOrientFromRelatedTable(creator.MostDerivedTable),
					new EditorCalculatedInitValue(IdTypeInfo.Universe,
						delegate (object[] inputs) {
							// TODO: Technically the following Session might not be the same as the editor's Session. Maybe we need a 'Session' init source (yuk!)
							object result = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session.Session.ExecuteCommandReturningScalar(
								dsMB.Schema.T.WorkOrderExpenseModelEntry.F.CostCenterID.EffectiveType.UnionCompatible(NullTypeInfo.Universe),
								new SelectSpecification(
									dsMB.Schema.T.DemandWorkOrderExpenseModelEntry,
									new SqlExpression[] { new SqlExpression(dsMB.Path.T.DemandWorkOrderExpenseModelEntry.F.WorkOrderExpenseModelEntryID.F.CostCenterID) },
									new SqlExpression(dsMB.Path.T.DemandWorkOrderExpenseModelEntry.F.Id).Eq(SqlExpression.Constant(inputs[0])),
									null
								));
							return result;
						},
						new EditorPathValue(baseDemandPath)
					)
				));
		}
		#endregion
		#region ActualItemTbl
		private static Tbl ActualItemTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(
					correction ? TId.CorrectionofActualItem : TId.ActualItem,
					correction, dsMB.Schema.T.ActualItem, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.ETblArgs.Add(ETbl.SetAllowMultiRecordEditing(false));
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonNonPOActualControls(dsMB.Path.T.ActualItem.F.DemandItemID,
				dsMB.Path.T.DemandItem.F.ItemLocationID.F.ItemID.F.Code,
				dsMB.Path.T.DemandItem.F.ItemLocationID.F.LocationID.F.Code);
			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Demanded - Already Actualized
			creator.BuildDemandQuantityDisplaysAndSuggestRemainingQuantity(dsMB.Path.T.ActualItem.F.DemandItemID.F.Quantity, dsMB.Path.T.ActualItem.F.DemandItemID.F.ActualQuantity);
			creator.StartCostingLayout();
			// The first row describes the on-hand situation.
			creator.BuildCostingBasedOnInventory(dsMB.Path.T.ActualItem.F.DemandItemID.F.ItemLocationID);
			// The second row describes the demand estimate values
			creator.StartCostingRow(DemandedColumnKey);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualItem.F.DemandItemID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisQuantityId))));
			creator.AddUnitCostEditDisplay(CostEstimateBasisUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualItem.F.DemandItemID.F.DemandID.F.CostEstimate, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(CostEstimateBasisQuantityId, CostEstimateBasisValueId, CostEstimateBasisUnitCostId));
			creator.HandleCostSuggestionWithBasisCost(DemandedCostColumnKey, CostEstimateBasisQuantityId, CostEstimateBasisValueId,
				new AccountingTransactionDerivationTblCreator.SuggestedValueSource(UseDemandedCost, CostEstimationValueId, UsingDemandedCost));

			creator.BuildCostControls(dsMB.Path.T.ActualItem.F.DemandItemID.F.DemandID.F.DemandActualCalculationInitValue);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualItem.F.DemandItemID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualItem.F.AccountingTransactionID.F.EffectiveDate),
						BTbl.ListColumn(dsMB.Path.T.ActualItem.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualItem.F.AccountingTransactionID.F.Cost),
						BTbl.ListColumn(dsMB.Path.T.ActualItem.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualItem.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualLaborInsideTbl
		private static Tbl ActualLaborInsideTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<TimeSpan> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<TimeSpan>(correction ? TId.CorrectionofActualHourlyInside : TId.ActualHourlyInside, correction, dsMB.Schema.T.ActualLaborInside, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonNonPOActualControls(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID,
				dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.TradeID.F.Code,
				dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.EmployeeID.F.ContactID.F.Code);

			// We use a hidden control to source the hourly rate because HandleCostSuggestionWithBasisCost only accepts NodeId's.
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.LaborInsideID.F.Cost,
											new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingUnitCostId))));

			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Demanded - Already Actualized
			creator.BuildDemandQuantityDisplaysAndSuggestRemainingQuantity(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.Quantity, dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.ActualQuantity);

			// The next row costs out This Quantity to a total cost.
			creator.StartCostingLayout();
			creator.HandleCostSuggestionWithUnitCost(KB.K("Calculated Labor Cost"), new TimeSpan(1, 0, 0), PricingUnitCostId,
				new DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(KB.K("Use calculated labor cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated labor cost")));

			// The second row describes the demand estimate values
			creator.StartCostingRow(DemandedColumnKey);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisQuantityId))));
			creator.AddUnitCostEditDisplay(CostEstimateBasisUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.DemandID.F.CostEstimate, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<TimeSpan>(CostEstimateBasisQuantityId, CostEstimateBasisValueId, CostEstimateBasisUnitCostId));
			creator.HandleCostSuggestionWithBasisCost(DemandedCostColumnKey, CostEstimateBasisQuantityId, CostEstimateBasisValueId,
				new AccountingTransactionDerivationTblCreator.SuggestedValueSource(UseDemandedCost, CostEstimationValueId, UsingDemandedCost));

			creator.BuildCostControls(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.DemandID.F.DemandActualCalculationInitValue);
			creator.SetFromCCSourcePath(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.LaborInsideID.F.CostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualLaborOutsideNonPO
		private static Tbl ActualLaborOutsideNonPOTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<TimeSpan> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<TimeSpan>(correction ? TId.CorrectionofActualHourlyOutsideNoPO : TId.ActualHourlyOutsideNoPO, correction, dsMB.Schema.T.ActualLaborOutsideNonPO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonNonPOActualControls(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID,
				dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.TradeID.F.Code,
				dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.VendorID.F.Code);
			// We use a hidden control to source the hourly rate because HandleCostSuggestionWithBasisCost only accepts NodeId's.
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID.F.Cost,
											new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingUnitCostId))));

			creator.ActualSubstitution(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID, dsMB.Path.T.Vendor.F.Code, dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID.F.VendorID);

			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Demanded - Already Actualized
			creator.BuildDemandQuantityDisplaysAndSuggestRemainingQuantity(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.Quantity, dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.ActualQuantity);

			creator.StartCostingLayout();
			// The next row costs out This Quantity to a total cost.
			creator.HandleCostSuggestionWithUnitCost(KB.K("Calculated Labor Cost"), new TimeSpan(1, 0, 0), PricingUnitCostId,
								new DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(KB.K("Use calculated labor cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated labor cost")));
			// The second row describes the demand estimate values
			creator.StartCostingRow(DemandedColumnKey);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisQuantityId))));
			creator.AddUnitCostEditDisplay(CostEstimateBasisUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.DemandID.F.CostEstimate, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<TimeSpan>(CostEstimateBasisQuantityId, CostEstimateBasisValueId, CostEstimateBasisUnitCostId));
			creator.HandleCostSuggestionWithBasisCost(DemandedCostColumnKey, CostEstimateBasisQuantityId, CostEstimateBasisValueId,
				new AccountingTransactionDerivationTblCreator.SuggestedValueSource(UseDemandedCost, CostEstimationValueId, UsingDemandedCost));
			creator.BuildCostControls(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.DemandID.F.DemandActualCalculationInitValue);
			creator.SetFromCCSourcePath(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID.F.AccountsPayableCostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.DemandID);
#if LATER_SOMEHOW
			// TODO: We want something like the following, to want the user that they are doing non-po actualization when PO lines exist.
			// There are three problems here:
			// 1 - The Purchased Quantity is not part of this form. For now this code refers to the Already Used quantity to demonstrate...
			// 2 - The Purchased Quantity isn't really what we want, we want the count of PO lines.
			// 3 - Although the ValueWarningException is recognized by the Error Provider, it is nevertheless an exception which gives the control's wrapper a non-null
			//		ValueStatus and thus blocks propagation of the value (use in other Check directives and/or transfer to the bound field.
			// If we can get this to work it should also apply to ActualOtherWorkOutsideNonPO.
			// This also requires making creator.ThisQuantityId public.
			creator.Actions.Add(
				new Check2<TimeSpan, TimeSpan>(
					delegate(TimeSpan purchasedQuantity, TimeSpan ActualizingQuantity)
					{
						// Make sure the email address entered is a valid SMTP EmailAddress
						if (purchasedQuantity > TimeSpan.Zero)
							return new ValueWarningException(KB.K("Labor for this Demand has been ordered on a Purchase Order; are you sure non-PO actualization is appropriate?"));
						return null;
					}
				).Operand1(AlreadyUsedQuantity).Operand2(creator.ThisQuantityId));
#endif
			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualLaborOutsideNonPO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualLaborOutsideNonPO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualLaborOutsideNonPO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualLaborOutsidePO
		private static Tbl ActualLaborOutsidePOTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<TimeSpan> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<TimeSpan>(correction ? TId.CorrectionofActualHourlyOutsideWithPO : TId.ActualHourlyOutsideWithPO, correction, dsMB.Schema.T.ActualLaborOutsidePO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonPOReceivingControls(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID, dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID,
				dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code,
				dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderID.F.Number);

			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Ordered - Already Received
			creator.BuildPOLineQuantityDisplaysAndSuggestRemainingQuantity(
				dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.Quantity, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.ReceiveQuantity,
				dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.POLineID.F.Cost, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.POLineID.F.ReceiveCost,
				dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID.PathToReferencedRow, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.Quantity);

			creator.SetFromCCSourcePath(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.POLineID.F.PurchaseOrderID.F.VendorID.F.AccountsPayableCostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					PurchasingAndLaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualLaborOutsidePO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualLaborOutsidePO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualLaborOutsidePO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualOtherWorkInside
		private static Tbl ActualOtherWorkInsideTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(correction ? TId.CorrectionofActualPerJobInside : TId.ActualPerJobInside, correction, dsMB.Schema.T.ActualOtherWorkInside, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonNonPOActualControls(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID,
				dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID.F.Code);
			// We need no additional controls to describe what was demanded; there is no Agent for OtherWorkInside.

			// We use a hidden control to source the hourly rate because HandleCostSuggestionWithBasisCost only accepts NodeId's.
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Cost,
											new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingUnitCostId))));

			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Demanded - Already Actualized
			creator.BuildDemandQuantityDisplaysAndSuggestRemainingQuantity(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.Quantity, dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.ActualQuantity);

			creator.StartCostingLayout();
			// The next row costs out This Quantity to a total cost.
			creator.HandleCostSuggestionWithUnitCost(KB.K("Calculated Job Cost"), 1L, PricingUnitCostId,
				new DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(KB.K("Use calculated job cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated job cost")));
			// The second row describes the demand estimate values
			creator.StartCostingRow(DemandedColumnKey);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisQuantityId))));
			creator.AddUnitCostEditDisplay(CostEstimateBasisUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.DemandID.F.CostEstimate, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(CostEstimateBasisQuantityId, CostEstimateBasisValueId, CostEstimateBasisUnitCostId));
			creator.HandleCostSuggestionWithBasisCost(DemandedCostColumnKey, CostEstimateBasisQuantityId, CostEstimateBasisValueId,
				new AccountingTransactionDerivationTblCreator.SuggestedValueSource(UseDemandedCost, CostEstimationValueId, UsingDemandedCost));
			creator.BuildCostControls(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.DemandID.F.DemandActualCalculationInitValue);

			creator.SetFromCCSourcePath(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.CostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualOtherWorkOutsideNonPO
		private static Tbl ActualOtherWorkOutsideNonPOTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(correction ? TId.CorrectionofActualPerJobOutsideNoPO : TId.ActualPerJobOutsideNoPO, correction, dsMB.Schema.T.ActualOtherWorkOutsideNonPO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonNonPOActualControls(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID,
				dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.PurchaseOrderText,
				dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.VendorID.F.Code);
			// We use a hidden control to source the hourly rate because HandleCostSuggestionWithBasisCost only accepts NodeId's.
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Cost,
											new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingUnitCostId))));

			creator.ActualSubstitution(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID, dsMB.Path.T.Vendor.F.Code, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.VendorID);

			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Demanded - Already Actualized
			creator.BuildDemandQuantityDisplaysAndSuggestRemainingQuantity(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.Quantity, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.ActualQuantity);

			creator.StartCostingLayout();
			// The next row costs out This Quantity to a total cost.
			creator.HandleCostSuggestionWithUnitCost(KB.K("Calculated Job Cost"), 1L, PricingUnitCostId,
					new DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(KB.K("Use calculated job cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated job cost")));

			// The second row describes the demand estimate values
			creator.StartCostingRow(DemandedColumnKey);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisQuantityId))));
			creator.AddUnitCostEditDisplay(CostEstimateBasisUnitCostId);
			creator.AddCostingControl(TblColumnNode.New(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.DemandID.F.CostEstimate, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisValueId))));
			creator.EndCostingRow();
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(CostEstimateBasisQuantityId, CostEstimateBasisValueId, CostEstimateBasisUnitCostId));
			creator.HandleCostSuggestionWithBasisCost(DemandedCostColumnKey, CostEstimateBasisQuantityId, CostEstimateBasisValueId,
				new AccountingTransactionDerivationTblCreator.SuggestedValueSource(UseDemandedCost, CostEstimationValueId, UsingDemandedCost));
			creator.BuildCostControls(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.DemandID.F.DemandActualCalculationInitValue);

			creator.SetFromCCSourcePath(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID.F.AccountsPayableCostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualOtherWorkOutsidePO
		private static Tbl ActualOtherWorkOutsidePOTbl(bool correction) {
			AccountingTransactionWithQuantityDerivationTblCreator<long> creator
				= new AccountingTransactionWithQuantityDerivationTblCreator<long>(correction ? TId.CorrectionofActualPerJobOutsideWithPO : TId.ActualPerJobOutsideWithPO, correction, dsMB.Schema.T.ActualOtherWorkOutsidePO, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, false, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonPOReceivingControls(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID, dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID,
				dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code,
				dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderID.F.Number);
			// Define the destination - Nothing to do, the C/C comes from a browser init.

			// Define the quantity suggestion: Ordered - Already Received
			creator.BuildPOLineQuantityDisplaysAndSuggestRemainingQuantity(
				dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.Quantity, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.ReceiveQuantity,
				dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.POLineID.F.Cost, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.POLineID.F.ReceiveCost,
				dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID.PathToReferencedRow, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.Quantity);

			creator.SetFromCCSourcePath(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.POLineID.F.PurchaseOrderID.F.VendorID.F.AccountsPayableCostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					PurchasingAndLaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectedQuantity, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectedCost)
					)
				}
			);
		}
		#endregion
		#region ActualMiscellaneousWorkOrderCostTbl
		private static Tbl ActualMiscellaneousWorkOrderCostTbl(bool correction) {
			AccountingTransactionDerivationTblCreator creator
				= new AccountingTransactionDerivationTblCreator(correction ? TId.CorrectionofActualMiscellaneousCost : TId.ActualMiscellaneousCost, correction, dsMB.Schema.T.ActualMiscellaneousWorkOrderCost, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, null, null);
			creator.BuildCommonAccountingHeaderControls();
			// Define the source of the resource
			creator.BuildCommonNonPOActualControls(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID,
				dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID.F.Code);

			string SuggestedCostId = KB.I("SuggestedCostId");
			creator.DetailColumns.Add(TblColumnNode.New(SuggestedCost, dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Cost,
											new ECol(ECol.AllReadonlyAccess, Fmt.SetId(SuggestedCostId))));
			creator.AddCostSuggestion(new DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(UseSuggestedCost, SuggestedCostId, UsingSuggestedCost));

			string MiscellaneousDemandCostId = KB.I("MiscellaneousDemandCostId");
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.DemandID.F.CostEstimate, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(MiscellaneousDemandCostId))));
			creator.AddCostSuggestion(new DerivationTblCreatorWithQuantityAndCostBase.SuggestedValueSource(UseDemandedCost, MiscellaneousDemandCostId, UsingDemandedCost));

			creator.BuildCostControls(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.DemandID.F.DemandActualCalculationInitValue);

			// Define the destination - Nothing to do, the C/C comes from a browser init.
			creator.SetFromCCSourcePath(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.CostCenterID);
			AddActualToCCSetup(creator, dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.DemandID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectedCost)
					)
				}
			);
		}
		#endregion

		#region Command Demand With Picker Support
		#region DemandDerivationTblCreator
		/// <summary>
		/// Basis for creating Demand Tbls. Note that this creates a Tbl either for Browsing or for Editing but not for both.
		/// </summary>
		// The Tbl for editing contains the second recordset that provides the WorkOrderExpenseModelEntry and passes its ID value off to the Actuals browsette.
		// The Tbl for browsing does not do this.
		protected class DemandDerivationTblCreator : DerivationTblCreatorWithQuantityAndCostBase {
			#region Construction
			public DemandDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, bool tblForBrowsing, bool tblForEditDefaults, TypeInfo unitCostTypeInfo)
				: base(identification, false, mostDerivedTable, dsMB.Path.T.Demand.F.CostEstimate.ReOrientFromRelatedTable(mostDerivedTable), unitCostTypeInfo) {
				TblForBrowsing = tblForBrowsing;
				TblForEditDefaults = tblForEditDefaults;
			}
			#endregion
			#region BuildCommonDemandHeaderControls
			public void BuildCommonDemandHeaderControls() {
				DetailColumns.Add(TblFixedRecordTypeNode.New());
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.Demand.F.EntryDate.ReOrientFromRelatedTable(MostDerivedTable), new NonDefaultCol(), DCol.Normal));
				DetailColumns.Add(TblGroupNode.New(dsMB.Path.T.Demand.F.WorkOrderID.ReOrientFromRelatedTable(MostDerivedTable), new TblLayoutNode.ICtorArg[] { DCol.Normal },
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderID.F.Number.ReOrientFromRelatedTable(MostDerivedTable), DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderID.F.UnitLocationID.Key(), dsMB.Path.T.Demand.F.WorkOrderID.F.UnitLocationID.F.Code.ReOrientFromRelatedTable(MostDerivedTable), DCol.Normal)
				));
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderID.ReOrientFromRelatedTable(MostDerivedTable), new NonDefaultCol(), ECol.Normal));
			}
			#endregion

			#region PanelCostDisplay
			public virtual void BuildPanelCostDisplay() {
				DetailColumns.Add(
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), DCol.Normal },
						MulticolumnDemandActualLabels,
						TblRowNode.New(TotalCostLabel, new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.Demand.F.CostEstimate.ReOrientFromRelatedTable(MostDerivedTable), DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.Demand.F.ActualCost.ReOrientFromRelatedTable(MostDerivedTable), DCol.Normal)
						)
					)
				);
			}
			#endregion
			#region BuildListColumns - Build the common BTbl.ICtorArgs
			private readonly List<BTbl.ICtorArg> BTblAttributes = new List<BTbl.ICtorArg>();
			public void AddBTblAttributes(params BTbl.ICtorArg[] btblAttrs) {
				BTblAttributes.AddRange(btblAttrs);
			}
			/// <summary>
			/// Add the common ListColumns 
			/// </summary>
			/// <param name="btblAttrs"></param>
			public void BuildListColumns(params BTbl.ICtorArg[] btblAttrs) {
				// Common to all demands for picking purposes
				AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.Demand.F.WorkOrderID.F.Number.ReOrientFromRelatedTable(MostDerivedTable)));
				AddBTblAttributes(btblAttrs);
				AddCommonListColumns();
			}
			/// <summary>
			/// Add the common cost (and possibly Quantity) ListColumns
			/// </summary>
			public virtual void AddCommonListColumns() {
				AddBTblAttributes(new BTbl.ICtorArg[] {
					BTbl.ListColumn(dsMB.Path.T.Demand.F.CostEstimate.ReOrientFromRelatedTable(MostDerivedTable)),
					BTbl.ListColumn(dsMB.Path.T.Demand.F.ActualCost.ReOrientFromRelatedTable(MostDerivedTable), NonPerViewColumn)
				});
			}
			#endregion
			#region BuildExpenseCategory
			static Key[] choiceLabels = new Key[] {
							KB.K("Manual entry"),
							KB.K("Current value calculation"),
							DemandedColumnKey
			};
			static EnumValueTextRepresentations costChoices = new EnumValueTextRepresentations(
				choiceLabels,
				null,
				new object[] {
								(int) DatabaseEnums.DemandActualCalculationInitValues.ManualEntry,
								(int) DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue,
								(int) DatabaseEnums.DemandActualCalculationInitValues.UseDemandEstimateValue,
							}
			);
			public static TblColumnNode ActualDefaultColumnNode(DBI_Path basisPath, DBI_Table mostDerivedTable) {
				// Common to DemandTemplate and Demand
				return
					TblColumnNode.New(basisPath.ReOrientFromRelatedTable(mostDerivedTable),
						new ECol(Fmt.SetEnumText(costChoices)),
						new DCol(Fmt.SetEnumText(costChoices))
					);
			}
			public void BuildExpenseCategory(DBI_Path defaultExpenseModelEntryInitPath, DBI_Path expenseCategoryFilterColumn) {
				DetailColumns.Add(ActualDefaultColumnNode(dsMB.Path.T.Demand.F.DemandActualCalculationInitValue, MostDerivedTable));
				DBI_Path workOrderExpenseCategoryPath = dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.ReOrientFromRelatedTable(MostDerivedTable);
				DetailColumns.Add(TblColumnNode.New(workOrderExpenseCategoryPath,
					new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseCategory.F.Code)),
					new ECol(
						Fmt.SetId(ExpenseCategory),
						Fmt.SetBrowserFilter(BTbl.EqFilter(expenseCategoryFilterColumn, true))
					),
					AccountingFeatureArg));

				Actions.Add(Init.OnLoad(new InSubBrowserTarget(ExpenseCategory, new BrowserFilterTarget(FilteredWorkOrderExpenseCategoryBrowseLogic.ModelFilterId)),
									new EditorPathValue(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.ReOrientFromRelatedTable(MostDerivedTable))));

				Actions.Add(Init.ContinuousNew(workOrderExpenseCategoryPath, new EditorPathValue(new DBI_Path(defaultExpenseModelEntryInitPath.PathToReferencedRow, dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID).ReOrientFromRelatedTable(MostDerivedTable))));

			}
			#endregion
			#region BuildActualsBrowsette
			public void BuildActualsBrowsette(DBI_Path filterPath) {
				Tabs.Add(TblTabNode.New(KB.K("Actuals"), KB.K("Show Actuals for the Demand"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.NewBrowsette(filterPath, DCol.Normal, ECol.Normal)
				));
			}
			public void BuildActualsBrowsette(DelayedCreateTbl tbl, DBI_Path filterPath) {
				Tabs.Add(TblTabNode.New(KB.K("Actuals"), KB.K("Show Actuals for the Demand"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.NewBrowsette(tbl, filterPath, DCol.Normal, ECol.Normal)
				));
			}
			#endregion
			#region GetTbl - add the final actions and accounting transaction info and build & return the tbl
			// Note that we put in the ECol ourselves.
			public override Tbl GetTbl(params Tbl.IAttr[] tblAttrs) {
				if (TblForBrowsing) {
					TblAttributes.Add(new BTbl(BTblAttributes.ToArray()));
					TblAttributes.AddRange(tblAttrs);
					Tabs.Insert(0, DetailsTabNode.New(DetailColumns.ToArray()));
					return new CompositeTbl(MostDerivedTable,
						Identification,
						TblAttributes.ToArray(),
						null,
						CompositeView.ChangeEditTbl(FindDelayedEditTbl(MostDerivedTable))
					);
				}
				else {
					// Although this is for editing we put in the BTbl anyway since it might contribute list columns to a browser that references it.
					TblAttributes.Add(new BTbl(BTblAttributes.ToArray()));
#if DEBUG
					DetailColumns.Add(TblColumnNode.New(KB.T("DEBUG: exp cat"), dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code, 1, ECol.AllReadonly));
					DetailColumns.Add(TblColumnNode.New(KB.T("DEBUG: exp mdl"), dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID.F.Code, 1, ECol.AllReadonly));
#endif
					if( TblForEditDefaults )
						TblAttributes.Add(new ETbl(ETbl.LogicClass(typeof(DemandEditLogic)), ETbl.EditorAccess(false, EdtMode.UnDelete)));
					else
						TblAttributes.Add(new ETbl(ETbl.LogicClass(typeof(DemandEditLogic)), ETbl.EditorAccess(false, EdtMode.UnDelete), ETbl.RowEditType(dsMB.Path.T.WorkOrderExpenseModelEntry.F.Id, 1, RecordManager.RowInfo.RowEditTypes.Lookup)));
					return base.GetTbl(tblAttrs);
				}
			}
			#endregion
			#region Properties set in Constructor
			protected readonly bool TblForBrowsing;
			protected readonly bool TblForEditDefaults;
			#endregion
		}
		#endregion
		#region DemandDerivationTblCreator<QT>
		protected class DemandDerivationTblCreator<QT> : DemandDerivationTblCreator
			where QT : struct, System.IComparable<QT> {
			#region Construction
			public DemandDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, bool tblForBrowsing, bool tblForEditDefaults, TypeInfo unitCostTypeInfo)
				: base(identification, mostDerivedTable, tblForBrowsing, tblForEditDefaults, unitCostTypeInfo) {
				ActualQuantityPath = new DBI_Path(TIGeneralMB3.ActualQuantityColumn(MostDerivedTable));
				System.Diagnostics.Debug.Assert(QuantityTypeInfo.GenericAcceptedNativeType(typeof(QT)), "DemandDerivationTblCreator: QT incompatible with QuantityTypeInfo");
				if (ActualQuantityPath.ReferencedColumn.EffectiveType is IntervalTypeInfo)
					QuantityFormat = IntervalFormat;
				else
					QuantityFormat = IntegralFormat;
			}
			public override void BuildPanelCostDisplay() {
				if (TblForEditDefaults)
					DetailColumns.Add(TblColumnNode.New(QuantityPath, DCol.Normal));
				else {
					DetailColumns.Add(
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
						MulticolumnDemandActualLabels,
						TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(QuantityPath, DCol.Normal),
							TblColumnNode.New(new DBI_Path(TIGeneralMB3.ActualQuantityColumn(MostDerivedTable)), DCol.Normal)
						),
						TblRowNode.New(TotalCostLabel, new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.Demand.F.CostEstimate.ReOrientFromRelatedTable(MostDerivedTable), DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.Demand.F.ActualCost.ReOrientFromRelatedTable(MostDerivedTable), DCol.Normal)
						)
					)
				);
				}
			}
			public void BuildDemandQuantityControls(params TblRowNode[] entryNodes) {
				if (TblForEditDefaults)
					DetailColumns.Add(TblColumnNode.New(QuantityPath, ECol.Normal));
				else {
					CreateSuggestedQuantitySelectorControl(null);
					var threeColumnNodes = new List<TblRowNode>();
					threeColumnNodes.Add(TblRowNode.New(DemandedColumnKey, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(QuantityPath, new ECol(Fmt.SetId(ThisQuantityId))),
						TblColumnNode.EmptyECol(),
						TblColumnNode.EmptyECol()
					));
					threeColumnNodes.Add(TblRowNode.New(AlreadyUsed, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(ActualQuantityPath, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(AlreadyUsedQuantityId))),
						CreateUnitCostEditDisplay(AlreadyUsedUnitCostId),
						TblColumnNode.New(dsMB.Path.T.Demand.F.ActualCost.ReOrientFromRelatedTable(MostDerivedTable), new ECol(ECol.AllReadonlyAccess, Fmt.SetId(AlreadyUsedValueId)))
					));
					threeColumnNodes.Add(TblRowNode.New(Remaining, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblUnboundControlNode.New(QuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RemainingQuantityId))),
						TblColumnNode.EmptyECol(),
						TblColumnNode.EmptyECol()
					));
					threeColumnNodes.AddRange(entryNodes);
					threeColumnNodes.Add(TblRowNode.New(CalculatedCost, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblLayoutNode.EmptyECol(),
						CreateUnitCostEditDisplay(CalculatedUnitCostId),
						TblUnboundControlNode.New(CostPath.ReferencedColumn.EffectiveType, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CalculatedCost)))
					));
					List<TblLayoutNode.ICtorArg> withViewCostAttrs = new List<TblLayoutNode.ICtorArg>();
					withViewCostAttrs.Add(new ECol(Fmt.SetId(UseCalculatedCost), Fmt.SetIsSetting(true), ECol.RestrictPerGivenPath(CostPath, 0)));
					withViewCostAttrs.Add(new NonDefaultCol());
					withViewCostAttrs.AddRange(AddSchemaCostTblLayoutNodeAttributesTbl.PermissionAttributesFromSchema(CostPath.Table));
					threeColumnNodes.Add(TblRowNode.New(null, new TblLayoutNode.ICtorArg[] { ECol.Normal },

						TblUnboundControlNode.New(UseCalculatedCost, BoolTypeInfo.NonNullUniverse, withViewCostAttrs.ToArray()),
						TblLayoutNode.EmptyECol(),
						TblLayoutNode.EmptyECol()
					));
					threeColumnNodes.Add(TblRowNode.New(DemandedCostColumnKey, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblLayoutNode.EmptyECol(),
						CreateUnitCostEditControl(Fmt.SetId(ThisEntryUnitCostId)),
						TblColumnNode.New(CostPath, new ECol(Fmt.SetId(ThisEntryCostId)))
					));
					DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.AllReadonly }, QuantityTypeInfo is IntervalTypeInfo ? MulticolumnHoursHourlyRateTotalLabels : MulticolumnQuantityUnitCostTotalLabels,
						threeColumnNodes.ToArray()));
					Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(ThisQuantityId, CalculatedCost, CalculatedUnitCostId));
					Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(AlreadyUsedQuantityId, AlreadyUsedValueId, AlreadyUsedUnitCostId));
					// The next action is to turn off the UseCalculatedCost calculator when we Edit a Demand so if someone Actualizes something with a different cost in the Actuals tab of the demand
					// editor, the change in value is not reflected automatically in the Demand editor's screen (and thereby enabling the Save operation leading to user confusion if they try to
					// Save the 'changed' demand record.
					// This also means that if someone "edits" a Demand when the WO state does not allow Demand editing, the (forced readonly) checkbox is off.
					Actions.Add(Init.New(new ControlTarget(UseCalculatedCost), new ConstantValue(false), null, TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.OnLoad, EdtMode.Edit)));
				}
			}

			public void UseUnitPricing(Thinkage.Libraries.Translation.Key label, DBI_Path unitPricingValue) {
				DetailColumns.Add(TblColumnNode.New(label, unitPricingValue, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingUnitCostId))));
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(PricingQuantityId, QuantityTypeInfo));
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(PricingCostId, unitPricingValue.ReferencedColumn.EffectiveType));
				Actions.Add(TotalFromQuantityAndUnitCostCalculator<QT>(PricingQuantityId, PricingUnitCostId, PricingCostId));
				Actions.Add(Init.OnLoad(new ControlTarget(PricingQuantityId), new ConstantValue(Compute.One<QT>())));
			}
			#endregion
			#region AddPricingActions
			public void AddPricingActions(params TblActionNode[] pricing) {
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(BeforeCorrectionQuantityId, QuantityTypeInfo));
				Actions.AddRange(new TblActionNode[] {
					// The first Init captures the Quantity in the original record (i.e. as in the DB). This is necessary for
					// calculating the available quantity net of this particular demand.
					Init.OnLoad(new ControlTarget(BeforeCorrectionQuantityId), new Thinkage.Libraries.Presentation.ControlValue(ThisQuantityId)),
					RemainingCalculator<QT>(ThisQuantityId, AlreadyUsedQuantityId, RemainingQuantityId)
				});
				Actions.AddRange(pricing);
				Actions.AddRange(new TblActionNode[] {
					Init.New(new ControlTarget(ThisEntryCostId), new Thinkage.Libraries.Presentation.ControlValue(CalculatedCost), new Thinkage.Libraries.Presentation.ControlValue(UseCalculatedCost)),
					Init.Continuous(new ControlReadonlyTarget(ThisEntryCostId, BecauseUsingCalculatedCost), new Thinkage.Libraries.Presentation.ControlValue(UseCalculatedCost)),
					Init.Continuous(new ControlReadonlyTarget(ThisEntryUnitCostId, BecauseUsingCalculatedCost), new Thinkage.Libraries.Presentation.ControlValue(UseCalculatedCost)),
					QuantityUnitTotalTripleCalculator<QT>(ThisQuantityId, ThisEntryUnitCostId, ThisEntryCostId)
			});
			}
			public void AddUnitPricingAction() {
				// TODO: In general *all* demands should calculate (total cost already used) + (unactualized demand * future unit cost). Right now only Demand Item does this properly,
				// and even at that the controls end up in an odd order. The order should be:
				// quantity demanded
				// quantity and cost actualized already
				// remaining quantity (and pricing if single-sourced)
				// pricing breakdownn of remaining quantity of multi-sourced
				// net demanded cost and unit cost based on total quantity demanded.
				AddPricingActions(TotalFromQuantityAndPricingCalculator<QT>(ThisQuantityId, PricingQuantityId, PricingCostId, CalculatedCost));
			}
			#endregion
			#region BuildListColumns - Build the common BTbl.ICtorArgs
			public override void AddCommonListColumns() {
				AddBTblAttributes(new BTbl.ICtorArg[] {
					BTbl.ListColumn(QuantityPath, NonPerViewColumn),
					BTbl.ListColumn(dsMB.Path.T.Demand.F.CostEstimate.ReOrientFromRelatedTable(MostDerivedTable)),
					BTbl.ListColumn(ActualQuantityPath, NonPerViewColumn),
					BTbl.ListColumn(dsMB.Path.T.Demand.F.ActualCost.ReOrientFromRelatedTable(MostDerivedTable), NonPerViewColumn)
				});
			}
			#endregion

			#region Members
			#region - Information computed based on the ctor arguments
			private readonly DBI_Path ActualQuantityPath;
			private readonly BTbl.ListColumnArg.IAttr QuantityFormat;
			#endregion
			#endregion
			#region Demand Filters
			public void StorageAssignmentPicker() {
				if (TblForEditDefaults)
					return;

				EnumValueTextRepresentations itemTests = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Items on the Parts list"),
								KB.K("Show all Items")
							},
					null,
					new object[] {
								0, 1
							}
				);
				object BId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 1),
					Fmt.SetEnumText(itemTests),
					Fmt.SetIsSetting(0)
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.Item.F.Id)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.SparePart.F.ItemID) },
							new SqlExpression(dsMB.Path.T.SparePart.F.UnitLocationID)
								.Eq(new SqlExpression(dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderID.F.UnitLocationID, 2)),
							null).SetDistinct(true))),
					BId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddUnboundPickerControlToGroup(KB.K("Show Storage Assignments for"), StorageAssignmentItemPickerId, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID.ReferencedColumn.EffectiveType.UnionCompatible(NullTypeInfo.Universe));
				// Add init to clear any Item the user may have picked if they use 'Cancel' to avoid the 
				// problem when the Cancel operation tries to set the Item Location picker to the initial value, 
				// but this value does not pass the filter (i.e. doesn't match the Item in the item picker).
				// Then on Idle, the picker refreshes, notices the value does not pass the item picker's filter and the control set's itself null
				// which notifies the editor who acts as if the user cleared the value.
				Actions.Add(Init.OnLoadNew(new ControlTarget(StorageAssignmentItemPickerId), new ConstantValue(null)));

				object storageAssignmentPickerItemFilterId = KB.I("storageAssignmentPickerItemFilterId");
				AddCustomPickerFilter(
					BTbl.TaggedEqFilter(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, storageAssignmentPickerItemFilterId, true),
					storageAssignmentPickerItemFilterId,
					new Thinkage.Libraries.Presentation.ControlValue(StorageAssignmentItemPickerId),
					null);

				AddPickerPanelDisplay(dsMB.Path.T.DemandItem.F.ItemLocationID.F.Code);
				// Label of PickerControl must be same as the underlying Tbl identification of for RequestAssignee as the Tbl identification will
				// be used in any Unique violations to identify the control on the screen (since the actual picker control will have a 'null' label)
				CreateBoundPickerControl(StorageAssignmentPickerId, dsMB.Path.T.DemandItem.F.ItemLocationID);
			}
			private static readonly object StorageAssignmentItemPickerId = KB.I("StorageAssignmentItemPickerId");
			private static readonly object StorageAssignmentPickerId = KB.I("StorageAssignmentPickerId");
			#endregion
		}
		#endregion
		#endregion
		#region DemandItem
		private static Tbl DemandItemTbl(bool tblForBrowsing, bool tblForEditDefaults) {
			var creator = new DemandDerivationTblCreator<long>(TId.DemandItem, dsMB.Schema.T.DemandItem, tblForBrowsing, tblForEditDefaults, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonDemandHeaderControls();
			// This is where we add what we are picking !
			creator.StorageAssignmentPicker();
			creator.BuildPanelCostDisplay();
			#region Availability Info - specific to Demand Item
			// DemandItem has Availability Information
			creator.DetailColumns.AddRange(new TblLayoutNode[] {
				TblGroupNode.New(KB.K("Available"), new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), ECol.Normal },
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, MulticolumnQuantityUnitCostTotalLabels,
						TblRowNode.New(KB.K("On Hand"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ActualItemLocationID.F.OnHand, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(OnHandQuantityId))),
							creator.CreateUnitCostEditDisplay(OnHandUnitCostId),
							TblColumnNode.New(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ActualItemLocationID.F.TotalCost, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(OnHandValueId)))
						),

						TblRowNode.New(KB.K("Available"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ActualItemLocationID.F.Available, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(AvailableQuantityId))),
							TblLayoutNode.EmptyECol(),
							TblLayoutNode.EmptyECol()
						),

						TblRowNode.New(KB.K("Via purchase"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblUnboundControlNode.New(KB.K("Quantity via purchase"), IntegralTypeInfo.Universe, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingQuantityId))),
							creator.CreateUnitCostEditDisplay(PricingUnitCostId),
							TblUnboundControlNode.New(KB.K("Purchase total cost"), creator.CostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PricingCostId)))
						)
					)
				)
			});
			creator.AddQuantitySuggestion(new DerivationTblCreatorBase.SuggestedValueSource(KB.K("Use On Hand Quantity"), OnHandQuantityId, KB.K("Using On Hand Quantity from storage assignment")));
			creator.AddQuantitySuggestion(new DerivationTblCreatorBase.SuggestedValueSource(KB.K("Use Available Quantity"), AvailableQuantityId, KB.K("Using Available Quantity from storage assignment")));
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(OnHandQuantityId, OnHandValueId, OnHandUnitCostId));
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(PricingQuantityId, PricingCostId, PricingUnitCostId));

			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(DemandFromStoreroomQuantityId, DemandFromStoreroomValueId, DemandFromStoreroomUnitCostId));
			creator.Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(DemandViaPurchaseQuantityId, DemandViaPurchaseValueId, DemandViaPurchaseUnitCostId));
			creator.DetailColumns.AddRange(new TblLayoutNode[] {
						TblUnboundControlNode.StoredEditorValue(OriginalQuantityNotYetUsedId, IntegralTypeInfo.NonNullUniverse),
						TblUnboundControlNode.StoredEditorValue(NetQuantityAvailableId, IntegralTypeInfo.NonNullUniverse)
			});
			#endregion
			creator.BuildDemandQuantityControls(
				TblRowNode.New(KB.K("Demand from on hand"), new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), ECol.Normal },
					TblUnboundControlNode.New(IntegralTypeInfo.NonNullUniverse, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(DemandFromStoreroomQuantityId))),
					creator.CreateUnitCostEditDisplay(DemandFromStoreroomUnitCostId),
					TblUnboundControlNode.New(creator.CostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(DemandFromStoreroomValueId)))
				),
				TblRowNode.New(KB.K("Demand via purchase"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
					// Use unbound nodes for ItemPrice to allow null values to be computed since ItemPrice columns are nonnullable
					TblUnboundControlNode.New(IntegralTypeInfo.NonNullUniverse, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(DemandViaPurchaseQuantityId))),
					creator.CreateUnitCostEditDisplay(DemandViaPurchaseUnitCostId),
					TblUnboundControlNode.New(creator.CostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(DemandViaPurchaseValueId)))
				)
			);
			creator.BuildExpenseCategory(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.F.DefaultItemExpenseModelEntryID, dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsItem);
			#region Pricing Actions - specific to Demand Item
			creator.AddPricingActions(
				new TblActionNode[] {
				Init.New(new ControlTarget(PricingQuantityId), new EditorPathValue(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ItemPriceID.F.Quantity), null, TblActionNode.Activity.Continuous),
				Init.New(new ControlTarget(PricingCostId), new EditorPathValue(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ItemPriceID.F.Cost), null, TblActionNode.Activity.Continuous),
				new Check3<long?, long?, long>()
						.Operand1(BeforeCorrectionQuantityId)
						.Operand2(AlreadyUsedQuantityId)
						.Operand3(OriginalQuantityNotYetUsedId, delegate(long? demand, long? used)
					{
						long effectiveUsed = used ??  0;
						long effectiveDemand = demand ?? 0;
						return checked(effectiveUsed > effectiveDemand ? 0 : effectiveDemand - effectiveUsed);
					}),
				new Check4<bool, long, long, long>()
							.Operand1(KB.K("Demand counts active"), dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.DemandCountsActive)
							.Operand2(OriginalQuantityNotYetUsedId)
							.Operand3(AvailableQuantityId)
							.Operand4(NetQuantityAvailableId, delegate(bool If, long qStillDemanded, long qTotalAvailable)
					{
						return If ? checked(qTotalAvailable + qStillDemanded) : qTotalAvailable;
					}),
				new Check3<long, long, long>()
							.Operand1(RemainingQuantityId)
							.Operand2(NetQuantityAvailableId)
							.Operand3(DemandFromStoreroomQuantityId, delegate(long stillDemand, long netAvail)
					{
						if (netAvail < 0 || stillDemand < 0)
							return 0;
						return checked(stillDemand > netAvail ? netAvail : stillDemand);
					}),
				new Check3<long, long, long>()
							.Operand1(RemainingQuantityId)
							.Operand2(DemandFromStoreroomQuantityId)
							.Operand3(DemandViaPurchaseQuantityId, delegate(long stillDemand, long fromStore)
					{
						if (stillDemand < 0)
							return 0;
						return checked(stillDemand - fromStore);
					}),
				TotalFromQuantityAndPricingCalculator<long>(DemandFromStoreroomQuantityId, OnHandQuantityId, OnHandValueId, DemandFromStoreroomValueId),
				// TODO: The following does not work properly when there is no quote present. Although the Check handles
				// the null values that result, the displays themselves are non-nullable (because the fields are required
				// within the ItemPrice record) and the Value fetch fails before we even get to our delegate.
				// Possibly EditControl should be changed to use a nullable version of the column's type if the path to
				// the column traverses a nullable link field (in particular a nullable link field that links to a
				// not-ForEditing RowInfo, but that cannot be ascertained before *all* paths are processed).
				//
				// This is also a problem in New mode for ECol columns whose default value is null. FIddling with the type
				// of control on an ERefCol will not help here.
				//
				// Another workaround would be to have the VnC handle the NonNullValueRequired status specially: If this is
				// the status, and the corresponding VnC operand has a Nullable type (can we tell???), then use a null value
				// instead of treating it as an error.
				TotalFromQuantityAndPricingCalculator<long>(DemandViaPurchaseQuantityId, PricingQuantityId, PricingCostId, DemandViaPurchaseValueId),
				new Check4<decimal?, decimal?, decimal?, decimal?>()
						.Operand1(AlreadyUsedValueId)
						.Operand2(DemandFromStoreroomValueId)
						.Operand3(DemandViaPurchaseValueId)
						.Operand4(CalculatedCost, delegate(decimal? alreadyUsed, decimal? fromStock, decimal? fromPurchase)
					{
						if (!fromStock.HasValue || !fromPurchase.HasValue)
							return null;
						return checked((alreadyUsed ?? 0) + fromStock + fromPurchase);
					})
			});
			#endregion
			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ItemID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandItem.F.ItemLocationID.F.LocationID.F.Code)
			});
			creator.BuildActualsBrowsette(ActualItemBrowseTblCreator, dsMB.Path.T.ActualItem.F.DemandItemID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				});
		}
		#endregion
		#region DemandLaborInside
		private static Tbl DemandLaborInsideTbl(bool tblForBrowsing, bool tblForEditDefaults) {
			var creator = new DemandDerivationTblCreator<TimeSpan>(TId.DemandHourlyInside, dsMB.Schema.T.DemandLaborInside, tblForBrowsing, tblForEditDefaults, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonDemandHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandLaborInside.F.LaborInsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.LaborInside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			// Availability information
			creator.UseUnitPricing(KB.K("Hourly Rate"), dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.Cost);

			creator.BuildDemandQuantityControls();
			creator.AddUnitPricingAction();
			creator.BuildExpenseCategory(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.F.DefaultHourlyInsideExpenseModelEntryID, dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsLabor);

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.EmployeeID.F.ContactID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.TradeID.F.Code),
			});
			creator.BuildActualsBrowsette(ActualLaborInsideBrowseTblCreator, dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
						LaborResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema
					}
			);
		}
		#endregion
		#region DemandLaborOutside
		private static Tbl DemandLaborOutsideTbl(bool activeOnly, bool forPOLine, bool alreadyUsed, bool tblForBrowsing, bool tblForEditDefaults) {
			var creator = new DemandDerivationTblCreator<TimeSpan>(TId.DemandHourlyOutside, dsMB.Schema.T.DemandLaborOutside, tblForBrowsing, tblForEditDefaults, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonDemandHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.LaborOutside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			creator.UseUnitPricing(KB.K("Hourly Rate"), dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.Cost);

			creator.BuildDemandQuantityControls();
			creator.AddUnitPricingAction();
			creator.BuildExpenseCategory(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.F.DefaultHourlyOutsideExpenseModelEntryID, dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsLabor);

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.VendorID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.TradeID.F.Code),
			});
			if (activeOnly)
				creator.AddBTblAttributes(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.DemandID.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.DemandCountsActive).IsTrue()));

			if (forPOLine) {
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup));
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandLaborOutside.F.OrderQuantity, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter));
			}
			if (alreadyUsed)
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandLaborOutside.F.ActualQuantity));
			creator.BuildActualsBrowsette(FindDelayedBrowseTbl(dsMB.Schema.T.DemandLaborOutsideActivity), dsMB.Path.T.DemandLaborOutsideActivity.F.DemandLaborOutsideID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				}
			);
		}
		#endregion
		#region DemandOtherWorkInsideTbl
		private static Tbl DemandOtherWorkInsideTbl(bool tblForBrowsing,bool tblForEditDefaults) {
			var creator = new DemandDerivationTblCreator<long>(TId.DemandPerJobInside, dsMB.Schema.T.DemandOtherWorkInside, tblForBrowsing, tblForEditDefaults, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonDemandHeaderControls();

			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.OtherWorkInside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			creator.UseUnitPricing(KB.K("Per Job Rate"), dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID.F.Cost);
			creator.BuildDemandQuantityControls();
			creator.AddUnitPricingAction();
			creator.BuildExpenseCategory(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.F.DefaultPerJobInsideExpenseModelEntryID, dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsLabor);

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID.F.EmployeeID.F.ContactID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID.F.TradeID.F.Code),
			});
			creator.BuildActualsBrowsette(ActualOtherWorkInsideBrowseTblCreator, dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				}
			);
		}
		#endregion
		#region DemandOtherWorkOutside
		/// <summary>
		///
		/// </summary>
		/// <param name="forPOLine">Expects vendor filter</param>
		/// <param name="alreadyUsed"></param>
		/// <returns></returns>
		public static Tbl DemandOtherWorkOutsideTbl(bool activeOnly, bool forPOLine, bool alreadyUsed, bool tblForBrowsing, bool tblForEditDefaults) {
			var creator = new DemandDerivationTblCreator<long>(TId.DemandPerJobOutside, dsMB.Schema.T.DemandOtherWorkOutside, tblForBrowsing, tblForEditDefaults, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonDemandHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.OtherWorkOutside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			creator.UseUnitPricing(KB.K("Per Job Rate"), dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.Cost);
			creator.BuildDemandQuantityControls();
			creator.AddUnitPricingAction();
			creator.BuildExpenseCategory(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.F.DefaultPerJobOutsideExpenseModelEntryID, dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsLabor);

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.VendorID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.TradeID.F.Code),
			});
			if (activeOnly)
				creator.AddBTblAttributes(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.DemandCountsActive).IsTrue()));

			if (forPOLine) {
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup));
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutside.F.OrderQuantity, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup));
			}
			if (alreadyUsed)
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutside.F.ActualQuantity, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter));
			creator.BuildActualsBrowsette(FindDelayedBrowseTbl(dsMB.Schema.T.DemandOtherWorkOutsideActivity), dsMB.Path.T.DemandOtherWorkOutsideActivity.F.DemandOtherWorkOutsideID);

			return creator.GetTbl(
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				}
			);
		}
		#endregion
		#region DemandMiscellaneousWorkOrderCost
		private static Tbl DemandMiscellaneousWorkOrderCostTbl(bool tblForBrowsing, bool tblForEditDefaults) {
			var creator = new DemandDerivationTblCreator(TId.DemandMiscellaneousCost, dsMB.Schema.T.DemandMiscellaneousWorkOrderCost, tblForBrowsing, tblForEditDefaults, null);
			creator.BuildCommonDemandHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			// Misc cost doesn't have quantities, and the base class isn't yet set up to distinguish between the qty calculations and just a simple cost so we do it directly here
			creator.DetailColumns.AddRange(new TblLayoutNode[] {
				TblColumnNode.New(SuggestedCost, dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID.F.Cost, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(SuggestedCost))),
				TblUnboundControlNode.New(UseSuggestedCost, BoolTypeInfo.NonNullUniverse, new ECol(Fmt.SetId(UseSuggestedCost), Fmt.SetIsSetting(false)), new NonDefaultCol()),
				TblColumnNode.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.CostEstimate, new DCol(), new ECol(Fmt.SetId(ThisEntryCostId))),
			});
			creator.BuildExpenseCategory(dsMB.Path.T.Demand.F.WorkOrderID.F.WorkOrderExpenseModelID.F.DefaultMiscellaneousExpenseModelEntryID, dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsMiscellaneous);
			// Misc cost doesn't have quantities, and the base class isn't yet set up to distinguish between the qty calculations and just a simple cost so we do it directly here
			creator.Actions.AddRange(new TblActionNode[] {
				Init.New(new ControlTarget(ThisEntryCostId), new Thinkage.Libraries.Presentation.ControlValue(SuggestedCost), new Thinkage.Libraries.Presentation.ControlValue(UseSuggestedCost)),
				Init.Continuous(new ControlReadonlyTarget(ThisEntryCostId, BecauseUsingSuggestedCost), new Thinkage.Libraries.Presentation.ControlValue(UseSuggestedCost)),
			});
			creator.BuildListColumns(BTbl.ListColumn(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID.F.Code));
			creator.BuildActualsBrowsette(ActualMiscellaneousWorkOrderCostBrowseTblCreator, dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
						ItemResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema
					}
			);
		}
		#endregion

		#region ChargebackLine
		/// <summary>
		/// Much of this code mimics CommonCorrectableColumnsAndActions, but because there is no quantity we have to do it ourselves
		/// </summary>
		/// <param name="correction"></param>
		/// <returns></returns>
		private static Tbl ChargebackLineEditTbl(bool correction) {
			AccountingTransactionDerivationTblCreator creator
				= new AccountingTransactionDerivationTblCreator(correction ? TId.CorrectionofChargebackActivity : TId.ChargebackActivity, correction, dsMB.Schema.T.ChargebackLine, AccountingTransactionDerivationTblCreator.ValueInterpretations.PositiveDelta, null, null);
			creator.BuildCommonAccountingHeaderControls();
			// Show the Chargeback in the editor
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.ChargebackID, ECol.AllReadonly, new NonDefaultCol()));
			// Show the Chargeback information in the browser: Chargeback Code, WO, Billable Requestor
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.ChargebackID.F.Code, DCol.Normal));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.ChargebackID.F.WorkOrderID.F.Number, DCol.Normal));
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.ChargebackID.F.BillableRequestorID.F.ContactID.F.Code, DCol.Normal));
			// Define the source of the resource: the CB category
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.ChargebackLineCategoryID,
				new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ChargebackLineCategory.F.Code)), correction ? ECol.AllReadonly : ECol.Normal));
			// Add the costing controls
			creator.BuildCostControls(null);
			// Set the C/C init source paths
			creator.SetFromCCSourcePath(dsMB.Path.T.ChargebackLine.F.ChargebackLineCategoryID.F.CostCenterID);
			creator.SetToCCSourcePath(dsMB.Path.T.ChargebackLine.F.ChargebackID.F.BillableRequestorID.F.AccountsReceivableCostCenterID);
			// Add the comment field
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.Comment, DCol.Normal, ECol.Normal));
			return creator.GetTbl(
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new ViewCostPermissionImpliesTablePermissionsTbl(
						Root.Rights.ViewCost.Chargeback,
						new TableOperationRightsGroup.TableOperation[]
						{
							TableOperationRightsGroup.TableOperation.Edit,
							TableOperationRightsGroup.TableOperation.Create,
						}
					),
					new BTbl() // For default editing
				}
			);
		}
		#endregion
		#region OtherWorkOutside
		static Tbl OtherWorkOutside() {
			return new Tbl(dsMB.Schema.T.OtherWorkOutside, TId.PerJobOutside,
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.OtherWorkOutside.F.Code),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkOutside.F.Desc, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkOutside.F.VendorID.F.Code), // part of XID
						BTbl.ListColumn(dsMB.Path.T.OtherWorkOutside.F.TradeID.F.Code), // uncomment when field is added to OtherWorkOutside to match LaborOutside
						BTbl.ListColumn(dsMB.Path.T.OtherWorkOutside.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.ClosedCombo | BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkOutside.F.Cost, NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.OtherWorkOutsideReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.TradeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Trade.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.PurchaseOrderText, new FeatureGroupArg(PurchasingGroup), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.Cost, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.VendorID.F.AccountsPayableCostCenterID.F.Code, AccountingFeatureArg, DCol.Normal, ECol.AllReadonly, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting),
						TblColumnNode.New(dsMB.Path.T.OtherWorkOutside.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.TaskDemandPerJobOutside, TId.PerJobOutside,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID, DCol.Normal, ECol.Normal))
				));
		}
		#endregion
		#region Common Demand Template
		#region DemandTemplateDerivationTblCreator
		/// <summary>
		/// Basis for creating Demand Tbls. Note that this creates a Tbl either for Browsing or for Editing but not for both.
		/// </summary>
		// The Tbl for editing contains the second recordset that provides the WorkOrderExpenseModelEntry and passes its ID value off to the Actuals browsette.
		// The Tbl for browsing does not do this.
		protected class DemandTemplateDerivationTblCreator : DerivationTblCreatorWithQuantityAndCostBase {
			#region Node IDs common to all DemandTemplate derivations
			protected static readonly object EstimateCostId = KB.I("EstimateCostId");
			#endregion
			#region Construction
			public DemandTemplateDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, bool tblForBrowsing, TypeInfo unitCostTypeInfo)
				: base(identification, false, mostDerivedTable, null, unitCostTypeInfo) {
				TblForBrowsing = tblForBrowsing;
			}
			#endregion
			#region BuildCommonDemandTemplateHeaderControls
			public void BuildCommonDemandTemplateHeaderControls() {
				DetailColumns.Add(TblFixedRecordTypeNode.New());
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderTemplateID.ReOrientFromRelatedTable(MostDerivedTable),
						new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderTemplate.F.Code)), new NonDefaultCol(), ECol.Normal));
			}
			#endregion

			#region PanelCostDisplay
			public virtual void BuildPanelCostDisplay() {
				DetailColumns.Add(
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.EstimateCost.ReOrientFromRelatedTable(MostDerivedTable),
						new ECol(
							Fmt.SetEnumText(EnumValueTextRepresentations.NewForBool(
								KB.K("Leave total demanded cost blank when work order is created"), null,
								KB.K("Estimate total demanded cost when work order is created"), null
							)),
							Fmt.SetId(EstimateCostId)
						),
						new DCol(
							Fmt.SetEnumText(EnumValueTextRepresentations.NewForBool(
								KB.K("Leave total demanded cost blank when work order is created"), null,
								KB.K("Estimate total demanded cost when work order is created"), null
							))
						)
					)
				);
			}
			#endregion
			#region BuildListColumns - Build the common BTbl.ICtorArgs
			private readonly List<BTbl.ICtorArg> BTblAttributes = new List<BTbl.ICtorArg>();
			public void AddBTblAttributes(params BTbl.ICtorArg[] btblAttrs) {
				BTblAttributes.AddRange(btblAttrs);
			}
			/// <summary>
			/// 
			/// </summary>
			/// 
			/// <param name="btblAttrs"></param>
			public void BuildListColumns(params BTbl.ICtorArg[] btblAttrs) {
				// Common to all demands for picking purposes
				AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandTemplate.F.WorkOrderTemplateID.F.Code.ReOrientFromRelatedTable(MostDerivedTable)));
				AddBTblAttributes(btblAttrs);
				AddCommonListColumns();
			}
			/// <summary>
			/// Add the common cost (and possibly Quantity) ListColumns
			/// </summary>
			public virtual void AddCommonListColumns() {
			}
			#endregion
			#region BuildExpenseCategory
			static Key[] choiceLabels = new Key[] {
							KB.K("Manual entry"),
							KB.K("Current value calculation"),
							DemandedColumnKey
			};
			static EnumValueTextRepresentations costChoices = new EnumValueTextRepresentations(
				choiceLabels,
				null,
				new object[] {
								(int) DatabaseEnums.DemandActualCalculationInitValues.ManualEntry,
								(int) DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue,
								(int) DatabaseEnums.DemandActualCalculationInitValues.UseDemandEstimateValue,
							}
			);
			public static TblColumnNode ActualDefaultColumnNode(DBI_Path basisPath, DBI_Table mostDerivedTable) {
				// Common to DemandTemplate and Demand
				return
					TblColumnNode.New(basisPath.ReOrientFromRelatedTable(mostDerivedTable),
					new ECol(Fmt.SetEnumText(costChoices)),
					new DCol(Fmt.SetEnumText(costChoices))
				);
			}
			public void BuildExpenseCategory() {
				DetailColumns.Add(ActualDefaultColumnNode(dsMB.Path.T.DemandTemplate.F.DemandActualCalculationInitValue, MostDerivedTable));
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.ReOrientFromRelatedTable(MostDerivedTable), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseCategory.F.Code)), ECol.Normal, AccountingFeatureArg));
			}
			#endregion
			#region GetTbl - add the final actions and accounting transaction info and build & return the tbl
			// Note that we put in the ECol ourselves.
			public override Tbl GetTbl(params Tbl.IAttr[] tblAttrs) {
				if (TblForBrowsing) {
					TblAttributes.Add(new BTbl(BTblAttributes.ToArray()));
					TblAttributes.AddRange(tblAttrs);
					Tabs.Insert(0, DetailsTabNode.New(DetailColumns.ToArray()));
					return new CompositeTbl(MostDerivedTable,
						Identification,
						TblAttributes.ToArray(),
						null,
						CompositeView.ChangeEditTbl(FindDelayedEditTbl(MostDerivedTable))
					);
				}
				else {
					// Although this is for editing we put in the BTbl anyway since it might contribute list columns to a browser that references it.
					TblAttributes.Add(new BTbl(BTblAttributes.ToArray()));
					TblAttributes.Add(new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete)));
					return base.GetTbl(tblAttrs);
				}
			}
			#endregion
			#region Properties set in Constructor
			protected readonly bool TblForBrowsing;
			#endregion
		}
		#endregion
		#region DemandTemplateDerivationTblCreator<QT>
		protected class DemandTemplateDerivationTblCreator<QT> : DemandTemplateDerivationTblCreator
			where QT : struct, System.IComparable<QT> {
			#region Labels
			protected Key EstimatedQuantityLabel {
				get {
					return QuantityTypeInfo is IntervalTypeInfo ? KB.K("Demanded Hours") : KB.K("Demanded Quantity");
				}
			}
			#endregion
			#region Construction
			public DemandTemplateDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, bool tblForBrowsing, TypeInfo unitCostTypeInfo)
				: base(identification, mostDerivedTable, tblForBrowsing, unitCostTypeInfo) {
				System.Diagnostics.Debug.Assert(QuantityTypeInfo.GenericAcceptedNativeType(typeof(QT)), "DemandDerivationTblCreator: QT incompatible with QuantityTypeInfo");
				if (QuantityPath.ReferencedColumn.EffectiveType is IntervalTypeInfo)
					QuantityFormat = IntervalFormat;
				else
					QuantityFormat = IntegralFormat;
			}
			public override void BuildPanelCostDisplay() {
				DetailColumns.Add(TblColumnNode.New(QuantityPath, DCol.Normal));
				base.BuildPanelCostDisplay();
			}
			public void BuildDemandTemplateQuantityControls() {
				DetailColumns.Add(TblColumnNode.New(QuantityPath, new ECol(Fmt.SetId(ThisQuantityId))));
				Actions.Add(new Check2<bool, QT?>(
					delegate (bool docalc, QT? qty) {
						if (docalc && !qty.HasValue)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Calculating the Demanded Cost when a Work Order is generated requires that a Quantity be supplied")));
						return null;
					})
					.Operand1(EstimateCostId)
					.Operand2(ThisQuantityId));
			}
			#endregion
			#region BuildListColumns - Build the common BTbl.ICtorArgs
			public override void AddCommonListColumns() {
				AddBTblAttributes(new BTbl.ICtorArg[] {
					BTbl.ListColumn(QuantityPath, NonPerViewColumn)
				});
			}
			#endregion
			#region BuildPO Template LinesBrowsette
			public void BuildPOTemplateLinesBrowsette(Key tabKey, DelayedCreateTbl tbl, DBI_Path filterPath) {
				Tabs.Add(TblTabNode.New(tabKey, KB.K("Show Purchase Template Lines for the Demand"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.NewBrowsette(tbl, filterPath, DCol.Normal, ECol.Normal)
				));
			}
			#endregion
			#region Members
			#region - Information computed based on the ctor arguments
			private readonly BTbl.ListColumnArg.IAttr QuantityFormat;
			#endregion
			#endregion
			#region Demand Filters
			public void StorageAssignmentPicker() {
				AddUnboundPickerControlToGroup(KB.K("Show Storage Assignments for"), StorageAssignmentItemPickerId, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID.ReferencedColumn.EffectiveType.UnionCompatible(NullTypeInfo.Universe));
				// Add init to clear any Item the user may have picked if they use 'Cancel' to avoid the 
				// problem when the Cancel operation tries to set the Item Location picker to the initial value, 
				// but this value does not pass the filter (i.e. doesn't match the Item in the item picker).
				// Then on Idle, the picker refreshes, notices the value does not pass the item picker's filter and the control set's itself null
				// which notifies the editor who acts as if the user cleared the value.
				Actions.Add(Init.OnLoadNew(new ControlTarget(StorageAssignmentItemPickerId), new ConstantValue(null)));

				object storageAssignmentPickerItemFilterId = KB.I("storageAssignmentPickerItemFilterId");
				AddCustomPickerFilter(
					BTbl.TaggedEqFilter(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, storageAssignmentPickerItemFilterId, true),
					storageAssignmentPickerItemFilterId,
					new Thinkage.Libraries.Presentation.ControlValue(StorageAssignmentItemPickerId),
					null);

				AddPickerPanelDisplay(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.Code);
				// Label of PickerControl must be same as the underlying Tbl identification of for RequestAssignee as the Tbl identification will
				// be used in any Unique violations to identify the control on the screen (since the actual picker control will have a 'null' label)
				CreateBoundPickerControl(StorageAssignmentPickerId, dsMB.Path.T.DemandItemTemplate.F.ItemLocationID);
			}
			private static readonly object StorageAssignmentItemPickerId = KB.I("StorageAssignmentItemPickerId");
			private static readonly object StorageAssignmentPickerId = KB.I("StorageAssignmentPickerId");
			#endregion
		}
		#endregion

		#endregion
		#region DemandItemTemplate
		private static Tbl DemandItemTemplateTbl(bool tblForBrowsing) {
			var creator = new DemandTemplateDerivationTblCreator<long>(TId.TaskDemandItem, dsMB.Schema.T.DemandItemTemplate, tblForBrowsing, TIGeneralMB3.ItemUnitCostTypeOnClient);
			creator.BuildCommonDemandTemplateHeaderControls();
			// This is where we add what we are picking !
			creator.StorageAssignmentPicker();
			creator.BuildPanelCostDisplay();

			creator.BuildDemandTemplateQuantityControls();
			creator.BuildExpenseCategory();

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.ItemID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.LocationID.F.Code)
			});
			return creator.GetTbl(
				new Tbl.IAttr[] {
					SchedulingAndItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				});
		}
		#endregion
		#region DemandLaborInsideTemplate
		private static Tbl DemandLaborInsideTemplateTbl(bool tblForBrowsing) {
			var creator = new DemandTemplateDerivationTblCreator<TimeSpan>(TId.TaskDemandHourlyInside, dsMB.Schema.T.DemandLaborInsideTemplate, tblForBrowsing, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonDemandTemplateHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.LaborInside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			creator.BuildDemandTemplateQuantityControls();
			creator.BuildExpenseCategory();

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID.F.EmployeeID.F.ContactID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID.F.TradeID.F.Code),
			});
			return creator.GetTbl(
				new Tbl.IAttr[] {
						SchedulingAndLaborResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema
					}
			);
		}
		#endregion
		#region DemandLaborOutsideTemplate
		private static Tbl DemandLaborOutsideTemplateTbl(bool forPOLine, bool tblForBrowsing) {
			var creator = new DemandTemplateDerivationTblCreator<TimeSpan>(TId.TaskDemandHourlyOutside, dsMB.Schema.T.DemandLaborOutsideTemplate, tblForBrowsing, TIGeneralMB3.HourlyUnitCostTypeOnClient);
			creator.BuildCommonDemandTemplateHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.LaborOutside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();

			creator.BuildDemandTemplateQuantityControls();

			creator.BuildExpenseCategory();

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.VendorID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.TradeID.F.Code),
			});

			if (forPOLine) {
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup));
			}
			creator.BuildPOTemplateLinesBrowsette(dsMB.Schema.T.POLineLaborTemplate.LabelKey, FindDelayedBrowseTbl(dsMB.Schema.T.POLineLaborTemplate), dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					SchedulingAndLaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				}
			);
		}
		#endregion
		#region DemandOtherWorkInsideTemplate
		private static Tbl DemandOtherWorkInsideTemplateTbl(bool tblForBrowsing) {
			var creator = new DemandTemplateDerivationTblCreator<long>(TId.TaskDemandPerJobInside, dsMB.Schema.T.DemandOtherWorkInsideTemplate, tblForBrowsing, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonDemandTemplateHeaderControls();

			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.OtherWorkInside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			creator.BuildDemandTemplateQuantityControls();
			creator.BuildExpenseCategory();

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID.F.EmployeeID.F.ContactID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID.F.TradeID.F.Code),
			});
			return creator.GetTbl(
				new Tbl.IAttr[] {
					SchedulingAndLaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				}
			);
		}
		#endregion
		#region DemandOtherWorkOutsideTemplate
		/// <summary>
		///
		/// </summary>
		/// <param name="forPOLine">Expects vendor filter</param>
		/// <param name="alreadyUsed"></param>
		/// <returns></returns>
		public static Tbl DemandOtherWorkOutsideTemplateTbl(bool forPOLine, bool tblForBrowsing) {
			var creator = new DemandTemplateDerivationTblCreator<long>(TId.TaskDemandPerJobOutside, dsMB.Schema.T.DemandOtherWorkOutsideTemplate, tblForBrowsing, TIGeneralMB3.PerJobUnitCostTypeOnClient);
			creator.BuildCommonDemandTemplateHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.OtherWorkOutside.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();
			creator.BuildDemandTemplateQuantityControls();
			creator.BuildExpenseCategory();

			creator.BuildListColumns(new BTbl.ICtorArg[] {
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.VendorID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.TradeID.F.Code),
			});

			if (forPOLine) {
				creator.AddBTblAttributes(BTbl.ListColumn(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup));
			}
			creator.BuildPOTemplateLinesBrowsette(dsMB.Schema.T.POLineOtherWorkTemplate.LabelKey, FindDelayedBrowseTbl(dsMB.Schema.T.POLineOtherWorkTemplate), dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID);
			return creator.GetTbl(
				new Tbl.IAttr[] {
					SchedulingAndLaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema
				}
			);
		}
		#endregion
		#region DemandMiscellaneousWorkOrderCostTemplate
		private static Tbl DemandMiscellaneousWorkOrderCostTemplateTbl(bool tblForBrowsing) {
			var creator = new DemandTemplateDerivationTblCreator(TId.TaskDemandMiscellaneousCost, dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate, tblForBrowsing, null);
			creator.BuildCommonDemandTemplateHeaderControls();
			creator.DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Code)), ECol.Normal));
			creator.BuildPanelCostDisplay();

			creator.BuildExpenseCategory();
			creator.BuildListColumns(
				BTbl.ListColumn(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID.F.Code)
			);
			return creator.GetTbl(
				new Tbl.IAttr[] {
						SchedulingAndItemResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema
					}
			);
		}
		#endregion

		#region Chargeback
		private static DelayedCreateTbl ChargebackEditTbl() {
			return new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Chargeback, TId.Chargeback,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.Delete), ETbl.Print(TIReports.SingleChargebackFormReport, dsMB.Path.T.ChargebackFormReport.F.ChargebackID)),
					TIReports.NewRemotePTbl(TIReports.ChargebackFormReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.Chargeback.F.WorkOrderID, new NonDefaultCol(),
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrder.F.Number)),
							new ECol(Fmt.SetPickFrom(TIWorkOrder.AllWorkOrderChargebackBrowsePickerTblCreator))),
						TblColumnNode.New(dsMB.Path.T.Chargeback.F.Code, DCol.Normal, ECol.Normal),
						TIContact.ContactGroupTblLayoutNode(
							TIContact.ContactGroupRow(dsMB.Path.T.Chargeback.F.BillableRequestorID, dsMB.Path.T.BillableRequestor.F.ContactID.PathToReferencedRow, ECol.Normal)
						),
						TblColumnNode.New(dsMB.Path.T.Chargeback.F.TotalCost, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.Chargeback.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ChargebackActivity, TId.Chargeback,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ChargebackActivity.F.ChargebackID, DCol.Normal, ECol.Normal))
				));
			});
		}
		#endregion

		#region WorkOrderPurchaseOrderLinkageTbl
		public static DelayedCreateTbl WorkOrderPurchaseOrderLinkageTbl(bool showPurchaseOrders) {
			List<BTbl.ICtorArg> BTblAttrs = new List<BTbl.ICtorArg>();
			return new DelayedCreateTbl(delegate () {
				DBI_Path parentPath;
				DBI_Table containmentTable;
				if (showPurchaseOrders) {
					containmentTable = dsMB.Schema.T.WorkOrderLinkedPurchaseOrdersTreeview;
					parentPath = dsMB.Path.T.WorkOrderPurchaseOrderView.F.LinkedPurchaseOrderID;
				}
				else {
					containmentTable = dsMB.Schema.T.PurchaseOrderLinkedWorkOrdersTreeview;
					parentPath = dsMB.Path.T.WorkOrderPurchaseOrderView.F.LinkedWorkOrderID;
				}
				object codeColumnId = KB.I("WOTemplatePOTemplateCodeColumnId");
				object descColumnId = KB.I("WOTemplatePOTemplateDescColumnId");
				return new CompositeTbl(dsMB.Schema.T.WorkOrderPurchaseOrderView, showPurchaseOrders ? TId.WorkOrder : TId.PurchaseOrder,
					new Tbl.IAttr[] {
							PurchasingGroup,
							new BTbl(
								BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Number"), codeColumnId),
								BTbl.PerViewListColumn(CommonDescColumnKey, descColumnId)
								),
								new FilteredTreeStructuredTbl(parentPath, containmentTable, 2, 2)
						},
					dsMB.Path.T.WorkOrderPurchaseOrderView.F.TableEnum,
					new CompositeView(TIWorkOrder.WorkOrderEditTblCreator, dsMB.Path.T.WorkOrderPurchaseOrderView.F.WorkOrderID,
						CompositeView.ForceNotPrimary(), ReadonlyView,
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.WorkOrder.F.Number),
						BTbl.PerViewColumnValue(descColumnId, dsMB.Path.T.WorkOrder.F.Subject)
					),
					new CompositeView(TIPurchaseOrder.PurchaseOrderEditTblCreator, dsMB.Path.T.WorkOrderPurchaseOrderView.F.PurchaseOrderID,
						CompositeView.ForceNotPrimary(), ReadonlyView,
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.PurchaseOrder.F.Number),
						BTbl.PerViewColumnValue(descColumnId, dsMB.Path.T.PurchaseOrder.F.Subject)
					),
					new CompositeView(dsMB.Path.T.WorkOrderPurchaseOrderView.F.WorkOrderPurchaseOrderID,
						CompositeView.PathAlias(dsMB.Path.T.WorkOrderPurchaseOrderView.F.LinkedWorkOrderID, dsMB.Path.T.WorkOrderPurchaseOrder.F.WorkOrderID),
						CompositeView.PathAlias(dsMB.Path.T.WorkOrderPurchaseOrderView.F.LinkedPurchaseOrderID, dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID),
						CompositeView.SetAdditionalPermissionGroupName(showPurchaseOrders ? (DBI_Table)dsMB.Schema.T.PurchaseOrder : (DBI_Table)dsMB.Schema.T.WorkOrder)
					),
					new CompositeView(dsMB.Path.T.WorkOrderPurchaseOrderView.F.POLineID.F.POLineLaborID, ReadonlyView),
					new CompositeView(dsMB.Path.T.WorkOrderPurchaseOrderView.F.POLineID.F.POLineOtherWorkID, ReadonlyView),
					new CompositeView(dsMB.Path.T.WorkOrderPurchaseOrderView.F.POLineID.F.POLineItemID, ReadonlyView),
					new CompositeView(dsMB.Path.T.WorkOrderPurchaseOrderView.F.AccountingTransactionID.F.ReceiveItemPOID, ReadonlyView)
				);
			});
		}
		#endregion
		#region WorkOrderTemplatePurchaseOrderTemplateLinkageTbl
		public static DelayedCreateTbl WorkOrderTemplatePurchaseOrderTemplateLinkageTbl(bool showPurchaseOrderTemplates) {
			List<BTbl.ICtorArg> BTblAttrs = new List<BTbl.ICtorArg>();
			return new DelayedCreateTbl(delegate () {
				DBI_Path parentPath;
				DBI_Table containmentTable;
				if (showPurchaseOrderTemplates) {
					containmentTable = dsMB.Schema.T.WorkOrderTemplateLinkedPurchaseOrderTemplatesTreeview;
					parentPath = dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.LinkedPurchaseOrderTemplateID;
				}
				else {
					containmentTable = dsMB.Schema.T.PurchaseOrderTemplateLinkedWorkOrderTemplatesTreeview;
					parentPath = dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.LinkedWorkOrderTemplateID;
				}

				object codeColumnId = KB.I("WOTemplatePOTemplateCodeColumnId");
				object descColumnId = KB.I("WOTemplatePOTemplateDescColumnId");
				return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplatePurchaseOrderTemplateView, showPurchaseOrderTemplates ? TId.Task : TId.PurchaseOrderTemplate,
					new Tbl.IAttr[] {
							PurchasingGroup,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(CommonDescColumnKey, descColumnId)
								),
								new FilteredTreeStructuredTbl(parentPath, containmentTable, 2, 2)
						},
					dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.TableEnum,
					new CompositeView(TIWorkOrder.WorkOrderTemplateEditTbl, dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.WorkOrderTemplateID,
						CompositeView.ForceNotPrimary(), ReadonlyView,
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.WorkOrderTemplate.F.Code),
						BTbl.PerViewColumnValue(descColumnId, dsMB.Path.T.WorkOrderTemplate.F.Desc)),
					new CompositeView(TIPurchaseOrder.PurchaseOrderTemplateEditTbl, dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.PurchaseOrderTemplateID,
						CompositeView.ForceNotPrimary(), ReadonlyView,
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.PurchaseOrderTemplate.F.Code),
						BTbl.PerViewColumnValue(descColumnId, dsMB.Path.T.PurchaseOrderTemplate.F.Desc)),
					new CompositeView(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.WorkOrderTemplatePurchaseOrderTemplateID,
						CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.LinkedWorkOrderTemplateID, dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.WorkOrderTemplateID),
						CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.LinkedPurchaseOrderTemplateID, dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.PurchaseOrderTemplateID)
						),
					new CompositeView(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.POLineTemplateID.F.POLineLaborTemplateID, ReadonlyView),
					new CompositeView(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.POLineTemplateID.F.POLineOtherWorkTemplateID, ReadonlyView),
					new CompositeView(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.POLineTemplateID.F.POLineItemTemplateID, ReadonlyView)
					);

			});
		}
		#endregion
		#region Task Browsers
		private static Tbl TaskBrowserTbl(bool includeMakeWO, bool includeDefaultViews) {
			var views = new List<CompositeView>();
			// Table #0 (WorkOrderTemplate)
			views.Add(new CompositeView(WorkOrderTemplateEditTbl, dsMB.Path.T.WorkOrderTemplate.F.Id));
			// Table #1 (WorkOrderTemplate SupplementalTask -- this record type never occurs in the dataset)
			views.Add(CompositeView.ExtraNewVerb(TIWorkOrder.WorkOrderTemplateSpecializationEditTbl,
						NoNewMode,
						CompositeView.ContextualInit(0, dsMB.Path.T.WorkOrderTemplate.F.Id, dsMB.Path.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID),
						CompositeView.EditorAccess(false, EdtMode.EditDefault, EdtMode.ViewDefault)));
			if (includeMakeWO)
				// Table #2 (Make WorkOrder -- this record type never occurs in the dataset)
				views.Add(CompositeView.ExtraNewVerb(WorkOrderFromTemplateEditLogic.WorkOrderFromTemplateTbl,
						CompositeView.IdentificationOverride(TId.WorkOrderFromTask),
						NoNewMode,
						CompositeView.ContextualInit(0, new CompositeView.Init(new ControlTarget(WorkOrderFromTemplateEditLogic.TemplateControlId), dsMB.Path.T.WorkOrderTemplate.F.Id))));
			if (includeDefaultViews) {
				Key GroupDefaultDemands = TId.TaskResource.Compose(Tbl.TblIdentification.TablePhrase_DefaultsFor);
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.DemandItemTemplate), CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.DemandLaborInsideTemplate), CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.DemandLaborOutsideTemplate), CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.DemandOtherWorkInsideTemplate), CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.DemandOtherWorkOutsideTemplate), CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
				views.Add(CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate), CompositeView.AdditionalEditDefaultsGroupKey(GroupDefaultDemands)));
			}
			return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplate, TId.Task,
				new Tbl.IAttr[] {
						SchedulingGroup,
						new BTbl(
								BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplate.F.Code),
								BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplate.F.Desc)
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl(delegate() { return TIReports.WorkOrderTemplateReport;})),
						new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID, dsMB.Schema.T.WorkOrderTemplateWithContainers, dsMB.Schema.T.WorkOrderTemplateWithContainers.F.BaseWorkOrderTemplateID, dsMB.Schema.T.WorkOrderTemplateWithContainers.F.WorkOrderTemplateID, 4, uint.MaxValue, treatAllRecordsAsPrimary: false)
				},
				null,   // All records in query are type 0.
				views.ToArray()
			);
		}
		#endregion
		#region WorkOrderAssignment
		#region WorkOrderAssignmentTbl
		// This has added complexities compared to PO and Request assignments because of the assignments implied by the presence of labor demands. The WorkOrderAssignmentAll view
		// includes both the explicit and implied assignments, and that is what the browser browses. Only the explicit entries can be edited, and the editor has
		// a customized edit tbl to include filtering on the picker for the "other" record
		private static DelayedCreateTbl WorkOrderAssignmentAllBrowsetteTbl(bool fixedWorkOrder) {
			return new DelayedCreateTbl(delegate () {
				List<BTbl.ICtorArg> BTblAttrs = new List<BTbl.ICtorArg>();
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID.F.Number));
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.Code));
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID.F.Subject));
				BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID.F.ContactID.F.Code));

				// For implied assignees, rather than showing the DemandLaborInside or DemandOtherWorkInside, we show the "other" record (WO or Assignee) in the panel
				// and allow no editing.
				//
				// TODO (W20110339) (mixed linkage): because the view-linked record (WorkOrderAssignment) is just a linkage it would be nice if the panel just showed the linked record
				// (the Assignee), but the View linkage for editing must still be to the Assignment record. We have no way of using different linkage for the
				// panel and for the edit linkage. As a result for explicit assignments, the panel must show the assignment record (which just conveniently shows just enough
				// Assignee->Contact information to be useful) so that we can edit/view the assignment record (though all fields a captive and thus readonly).
				// Even if we forbade edit/view of the Assignment record, the linkage path is also used to make the browser select any new records, so this would fail if we directed the path to the Assignee table.
				// To cure this we would have to differentiate between the various purposes of the linkage path, then allow extensions to specify different paths for the various purposes.
				// Purposes include (but are not limited to):
				// Implying the referenced EditTbl (in CompositeView ctor)
				// Mapping browser paths to paths in the EditTbl
				// CompositeView.RecognizeByValidEditLinkage
				// Obtaining the ID to call up an editor
				// Searching for newly-created record in the browse data based on ID returned from editor
				//
				// In this case we also want distinct "edit tbls" i.e. we want the Tbl used for editing to be different than the one used for the Panel layout (and correspondingly different linkage paths)
				//
				// For the implied assignments, there is no direct Edit/View capability so the linkage goes directly to the Assignee or Work Order record, but then the panel information
				// looks different between implicit and explicit linkages. In particular, the WO details shown in the Assignment record for explicit assignments
				// pale compared to what is seen in the WO record for implicit assignments.
				DBI_Path impliedAssigneeLinkagePath = fixedWorkOrder
					? (DBI_Path)dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID// .F.ContactID see TODO (mixed linkage) above.
					: (DBI_Path)dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID;
				Key viewWOKey = KB.K("View Work Order");
				Key viewWOTip = KB.K("View the assigned Work Order");
				Key viewAssigneeKey = KB.K("View Assignee");
				Key viewAssigneeTip = KB.K("View the Work Order Assignee");
				return new CompositeTbl(dsMB.Schema.T.WorkOrderAssignmentAll, TId.WorkOrderAssignment,
					new Tbl.IAttr[] {
						WorkOrdersGroup,
						new BTbl(BTblAttrs.ToArray() )
					},
					null,
					// Explicit assignment records
					new CompositeView(WorkOrderAssignmentEditTbl(fixedWorkOrder), dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssignmentID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.PathAlias(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID, dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID),
						CompositeView.PathAlias(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID, dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID),
						CompositeView.AdditionalViewVerb(viewWOKey, viewWOTip, null, dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID, null, null),
						CompositeView.AdditionalViewVerb(viewAssigneeKey, viewAssigneeTip, null, dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID, null, null)
					),
					// Assignments implied by Labor Demands.
					new CompositeView(TblRegistry.FindDelayedEditTbl(impliedAssigneeLinkagePath.ReferencedColumn.ConstrainedBy.Table), impliedAssigneeLinkagePath,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssignmentID).IsNull()),
						CompositeView.EditorDefaultAccess(false),
						CompositeView.AdditionalViewVerb(viewWOKey, viewWOTip, null, dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID, null, null),
						CompositeView.AdditionalViewVerb(viewAssigneeKey, viewAssigneeTip, null, dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID, null, null)
					)
				);
			});
		}
		private static DelayedCreateTbl WorkOrderAssignmentEditTbl(bool fixedWorkOrder) {
			var creator = new AssignmentDerivationTblCreator(TId.WorkOrderAssignment, dsMB.Schema.T.WorkOrderAssignment);
			if (!fixedWorkOrder) {
				// TODO: This should be 3 checkboxes instead.
				EnumValueTextRepresentations requestState = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only show Draft Work Orders"),
								KB.K("Only show Open Work Orders"),
								KB.K("Only show Closed Work Orders"),
								KB.K("Only show Open or Draft Unassigned Work Orders"),
								KB.K("Show all Work Orders")
							},
					null,
					new object[] { 0, 1, 2, 3, 4 }
				);
				object woFilterChoiceId = creator.AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 4),
					Fmt.SetEnumText(requestState),
					Fmt.SetIsSetting(0)
				);

				creator.AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsDraft)),
					woFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 0)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen)),
					woFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 1)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsClosed)),
					woFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 2)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen)
						.Or(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsDraft))
						.And(
							new SqlExpression(dsMB.Path.T.WorkOrder.F.Id)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAndUnassignedWorkOrder.F.WorkOrderID)},
								new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAndUnassignedWorkOrder.F.WorkOrderAssigneeID).IsNull(),
								null)))
					),
					woFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 3)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(SqlExpression.Constant(true)),
					woFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 4)
				);
			}
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID.F.Number);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.Code);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID.F.Subject);
			creator.CreateBoundPickerControl(KB.I("WorkOrderPickerId"), dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID);

			if (fixedWorkOrder) {
				EnumValueTextRepresentations assigneeCriteria = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Show prospects for Work Order Assignee for this Work Order"),
								KB.K("Show all Work Order Assignees not currently assigned to this Work Order")
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
																new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.ContactID)
																	.In(new SelectSpecification(
																		null,
																		new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderAssigneeProspect.F.ContactID), },
																		new SqlExpression(dsMB.Path.T.WorkOrderAssigneeProspect.F.WorkOrderID).Eq(new SqlExpression(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID, 2)), // outer scope 2 refers to the edit buffer contents
																		null).SetDistinct(true))
																.Or(new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.ContactID.L.User.ContactID.F.Id)
																	.Eq(new SqlExpression(new UserIDSource())))),
					assigneeFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 0)
				);

				// Build an expression that finds all assignees not currently associated with the WorkOrder associated with this WorkOrder Assignment
				// select ID from WorkOrderAssignee 
				// where ID NOT IN (select WorkOrderAssigneeID from WorkOrderAssignment where WorkOrderID = <ID of current WorkOrder associated with WorkOrderAssignment>)
				creator.AddPickerFilter(BTbl.ExpressionFilter(
																new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.Id)
																	.In(new SelectSpecification(
																		null,
																		new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID) },
																		new SqlExpression(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID).Eq(new SqlExpression(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID, 2)),   // outer scope 2 refers to the edit buffer contents
																		null).SetDistinct(true)).Not()),
					assigneeFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 1)
				);
			}
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID.F.ContactID.F.Code);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID.F.ContactID.F.BusinessPhone);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID.F.ContactID.F.MobilePhone);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID.F.ContactID.F.Email);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID.F.ReceiveNotification);
			creator.AddPickerPanelDisplay(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID.F.Comment);
			// Label of PickerControl must be same as the underlying Tbl identification of for WorkOrderAssignee as the Tbl identification will
			// be used in any Unique violations to identify the control on the screen (since the actual picker control will have a 'null' label
			creator.CreateBoundPickerControl(KB.I("WorkOrderAssigneePickerId"), dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID);

			return new DelayedCreateTbl(
				delegate () {
					return creator.GetTbl(WorkOrdersGroup);
				}
			);
		}
		#endregion
		#endregion
		#endregion

		private TIWorkOrder() { }
		// Common TblQueryExpression
		public static TblQueryValueNode IsPreventiveValueNodeBuilder(dsMB.PathClass.PathToWorkOrderRow WO, params TblLayoutNode.ICtorArg[] attrs) {
			List<TblLayoutNode.ICtorArg> newAttrs = new List<TblLayoutNode.ICtorArg>();
			newAttrs.Add(Fmt.SetEnumText(ViewRecordTypes.IsPreventiveEnumText));
			newAttrs.AddRange(attrs);
			return TblQueryValueNode.New(KB.K("Maintenance Type"),
				new TblQueryExpression(new SqlExpression(WO.F.PMGenerationBatchID).IsNotNull()), newAttrs.ToArray());
		}
		static TIWorkOrder() {
			#region Demand Pickers
			DemandLaborOutsideForPOLinePickerTblCreator = new DelayedCreateTbl(delegate () {
				return DemandLaborOutsideTbl(true, true, false, true, false);
			});
			DemandOtherWorkOutsideForPOLinePickerTblCreator = new DelayedCreateTbl(delegate () {
				return DemandOtherWorkOutsideTbl(true, true, false, true, false);
			});
			DemandLaborOutsideTemplateForPOLineTemplatePickerTblCreator = new DelayedCreateTbl(delegate () {
				return DemandLaborOutsideTemplateTbl(true, true);
			});
			DemandOtherWorkOutsideTemplateForPOLineTemplatePickerTblCreator = new DelayedCreateTbl(delegate () {
				return DemandOtherWorkOutsideTemplateTbl(true, true);
			});
			#endregion
			#region Actuals browsers for the Demand tbls
			// TODO: All of these should disable their New command on non-correction views if the combination of expense model and expense category are invalid.
			// The Demand editor that typically (but not necessarily) contains these browsettes has the WorkOrderExpenseModelEntry records (if any) loaded in recordset
			// 1, and we should use the non-nullness of the c/c field therein to determine if our New command should be enabled. The problem lies in figuring out
			// how to get the information from the containing editor to us.
			// We could have a BrowserStoredValue coded in the BTbl, which the editor hits with an init (using InSubBrowserTarget), but then we also need a way
			// of making a context-free New conditional. The condition does not count as "context" because it does not depend on the record already selected.
			// This also has the problem that if we are not embedded in the appropriate editor no actualization would be possible. I suppose another stored value
			// of type bool could be used to turn the dead-end disabling on and off, or there could just be a single boolean stored value "DisableActualization"
			// and have the enclosing editor do the is-null test.
			// Note that the called Actual editor fetches the c/c itself with its own lookup code independent of anything we are doing.
			ActualItemBrowseTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.ActualItem, TId.ActualItem,
					new Tbl.IAttr[] {
						ItemResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ActualItem.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
							BTbl.ListColumn(dsMB.Path.T.ActualItem.F.Quantity),
							BTbl.ListColumn(dsMB.Path.T.ActualItem.F.AccountingTransactionID.F.Cost),
							BTbl.ListColumn(dsMB.Path.T.ActualItem.F.CorrectedQuantity),
							BTbl.ListColumn(dsMB.Path.T.ActualItem.F.CorrectedCost)
						),
						new TreeStructuredTbl(null, 2)
					},
					null,
					CompositeView.ChangeEditTbl(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.ActualItem),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualItem.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.ActualItem.F.Id)))),
					CompositeView.ChangeEditTbl(ActualItemCorrectionTblCreator,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualItem.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.ActualItem.F.Id))),
						CompositeView.SetParentPath(dsMB.Path.T.ActualItem.F.CorrectionID),
						CompositeView.ContextualInit(
							new int[] { 0, 1 },
							new CompositeView.Init(dsMB.Path.T.ActualItem.F.CorrectionID, dsMB.Path.T.ActualItem.F.CorrectionID)
						)
					)
				);
			});
			ActualLaborInsideBrowseTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.ActualLaborInside, TId.ActualHourlyInside,
					new Tbl.IAttr[] {
						LaborResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
							BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.Quantity),
							BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.AccountingTransactionID.F.Cost),
							BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.CorrectedQuantity),
							BTbl.ListColumn(dsMB.Path.T.ActualLaborInside.F.CorrectedCost)
						),
						new TreeStructuredTbl(null, 2)
					},
					null,
					CompositeView.ChangeEditTbl(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.ActualLaborInside),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualLaborInside.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.ActualLaborInside.F.Id)))),
					CompositeView.ChangeEditTbl(ActualLaborInsideCorrectionTblCreator,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualLaborInside.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.ActualLaborInside.F.Id))),
						CompositeView.SetParentPath(dsMB.Path.T.ActualLaborInside.F.CorrectionID),
						CompositeView.ContextualInit(
							new int[] { 0, 1 },
							new CompositeView.Init(dsMB.Path.T.ActualLaborInside.F.CorrectionID, dsMB.Path.T.ActualLaborInside.F.CorrectionID)
						)
					)
				);
			});
			ActualOtherWorkInsideBrowseTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.ActualOtherWorkInside, TId.ActualPerJobInside,
					new Tbl.IAttr[] {
						LaborResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
							BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.Quantity),
							BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.AccountingTransactionID.F.Cost),
							BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.CorrectedQuantity),
							BTbl.ListColumn(dsMB.Path.T.ActualOtherWorkInside.F.CorrectedCost)
						),
						new TreeStructuredTbl(null, 2)
					},
					null,
					CompositeView.ChangeEditTbl(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.ActualOtherWorkInside),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualOtherWorkInside.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.ActualOtherWorkInside.F.Id)))),
					CompositeView.ChangeEditTbl(ActualOtherWorkInsideCorrectionTblCreator,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualOtherWorkInside.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.ActualOtherWorkInside.F.Id))),
						CompositeView.SetParentPath(dsMB.Path.T.ActualOtherWorkInside.F.CorrectionID),
						CompositeView.ContextualInit(
							new int[] { 0, 1 },
							new CompositeView.Init(dsMB.Path.T.ActualOtherWorkInside.F.CorrectionID, dsMB.Path.T.ActualOtherWorkInside.F.CorrectionID)
						)
					)
				);
			});
			ActualMiscellaneousWorkOrderCostBrowseTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.ActualMiscellaneousWorkOrderCost, TId.ActualMiscellaneousCost,
					new Tbl.IAttr[] {
						ItemResourcesGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
							BTbl.ListColumn(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.AccountingTransactionID.F.Cost),
							BTbl.ListColumn(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectedCost)
						),
						new TreeStructuredTbl(null, 2)
					},
					null,
					CompositeView.ChangeEditTbl(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.ActualMiscellaneousWorkOrderCost),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.Id)))),
					CompositeView.ChangeEditTbl(ActualMiscellaneousWorkOrderCostCorrectionTblCreator,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.Id))),
						CompositeView.SetParentPath(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectionID),
						CompositeView.ContextualInit(
							new int[] { 0, 1 },
							new CompositeView.Init(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectionID, dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.CorrectionID)
						)
					)
				);
			});
			#endregion
			#region Actual/Chargeback Correction Tbl Creators
			ActualItemCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualItemTbl(true);
			});
			ActualLaborInsideCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualLaborInsideTbl(true);
			});
			ActualLaborOutsideNonPOCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualLaborOutsideNonPOTbl(true);
			});
			ActualLaborOutsidePOCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualLaborOutsidePOTbl(true);
			});
			ActualOtherWorkInsideCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualOtherWorkInsideTbl(true);
			});
			ActualOtherWorkOutsideNonPOCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualOtherWorkOutsideNonPOTbl(true);
			});
			ActualOtherWorkOutsidePOCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualOtherWorkOutsidePOTbl(true);
			});
			ActualMiscellaneousWorkOrderCostCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ActualMiscellaneousWorkOrderCostTbl(true);
			});
			ChargebackLineCorrectionTblCreator = new DelayedCreateTbl(delegate () {
				return ChargebackLineEditTbl(true);
			});
			ChargebackLineEditTblCreator = new DelayedCreateTbl(delegate () {
				return ChargebackLineEditTbl(false);
			});
			DemandItemDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return DemandItemTbl(false, true);
			});
			DemandLaborInsideDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return DemandLaborInsideTbl(false, true);
			});
			DemandLaborOutsideDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return DemandLaborOutsideTbl(false, false, false, false, true);
			});
			DemandOtherWorkInsideDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return DemandOtherWorkInsideTbl(false, true);
			});
			DemandOtherWorkOutsideDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return DemandOtherWorkOutsideTbl(false, false, false, false, true);
			});
			DemandMiscellaneousWorkOrderCostDefaultEditorTblCreator = new DelayedCreateTbl(delegate () {
				return DemandMiscellaneousWorkOrderCostTbl(false, true);
			});
			#endregion
			#region PurchaseOrdersLinkedToWorkOrdersTbl
			AssociatedPurchaseOrdersTbl = WorkOrderPurchaseOrderLinkageTbl(true);
			AssociatedPurchaseOrderTemplatesTbl = WorkOrderTemplatePurchaseOrderTemplateLinkageTbl(true);
			#endregion
			#region Task Picker Tbl Creators
			TaskPickerTblCreator = new DelayedCreateTbl(delegate () {
				return TaskBrowserTbl(false, false);
			});
			#endregion
			#region Chargeback
			var ChargebackEditorTblCreator = ChargebackEditTbl();
			AllChargebackTbl = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.Chargeback, TId.Chargeback,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.Chargeback.F.WorkOrderID.F.Number),
						BTbl.ListColumn(dsMB.Path.T.Chargeback.F.Code),
						BTbl.ListColumn(dsMB.Path.T.Chargeback.F.BillableRequestorID.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.Chargeback.F.TotalCost)
					),
					TIReports.NewRemotePTbl(TIReports.ChargebackFormReport)
				},
				null,
				CompositeView.ChangeEditTbl(ChargebackEditorTblCreator),
				CompositeView.AdditionalEditDefault(ChargebackLineEditTblCreator)
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.Chargeback, AllChargebackTbl);
			#endregion
			#region WorkOrderStateHistory
			#region - Common layout for regular state history and Close Work Order with comments
			TblLayoutNodeArray WorkOrderStateHistoryNodes = new TblLayoutNodeArray(
				TblGroupNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Number, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.Code, new ECol(ECol.AllReadonlyAccess, ECol.ForceErrorsFatal(), Fmt.SetId(TIGeneralMB3.CurrentStateHistoryCodeWhenLoadedId))),
					TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateHistoryStatusID.F.Code, ECol.AllReadonly)
				),
				TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate, DCol.Normal, new ECol(Fmt.SetId(TIGeneralMB3.StateHistoryEffectiveDateId)), new NonDefaultCol()),
				TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.EntryDate, new NonDefaultCol(), DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.UserID.F.ContactID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
				TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly, Fmt.SetDynamicSizing()),
				TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Code)), ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.Comment, DCol.Normal, ECol.Normal)
			);
			#endregion
			#region - State History editor used for Close Work Order with comments
			CloseWorkOrderEditTblCreator = new DelayedCreateTbl(delegate () {
				TblLayoutNodeArray extraNodes;
				List<TblActionNode> inits = StateHistoryInits(MB3Client.WorkOrderHistoryTable, out extraNodes);
				inits.Add(StartEndDurationCalculator(WorkOrderStartDateEstimateId, WorkOrderDurationEstimateId, WorkOrderEndDateEstimateId));

				return new Tbl(dsMB.Schema.T.WorkOrderStateHistory, TId.WorkOrderStateHistory,
					new Tbl.IAttr[] {
						WorkOrdersGroup,
						new ETbl(
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View),
							ETbl.NewOnlyOnce(true),
							ETbl.UseNewConcurrency(true),
							ETbl.AllowConcurrencyErrorOverride(false),
							ETbl.RowEditType(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Id, 0, RecordManager.RowInfo.RowEditTypes.EditOnly), // needed so new WO is not created in New mode.
							MB3ETbl.IsStateHistoryTbl(WorkOrderHistoryTable)
						)
					},
					WorkOrderStateHistoryNodes
						+ TblGroupNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.StartDateEstimate, new ECol(Fmt.SetId(WorkOrderStartDateEstimateId))),
								TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.EndDateEstimate, new ECol(Fmt.SetId(WorkOrderEndDateEstimateId))),
								TblUnboundControlNode.New(KB.K("Work Duration"), new IntervalTypeInfo(new TimeSpan(1, 0, 0, 0, 0), new TimeSpan(1, 0, 0, 0, 0), TimeSpan.MaxValue, false),
									new ECol(Fmt.SetId(WorkOrderDurationEstimateId), ECol.RestrictPerGivenPath(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.EndDateEstimate, 0))),
								TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Downtime, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.CloseCodeID, ECol.Normal)
							)
						+ extraNodes,
					inits.ToArray()
				);
			});
			#endregion
			#region - Normal Tbl for state history
			DefineEditTbl(dsMB.Schema.T.WorkOrderStateHistory, new DelayedCreateTbl(delegate () {
				TblLayoutNodeArray extraNodes;
				List<TblActionNode> inits = StateHistoryInits(MB3Client.WorkOrderHistoryTable, out extraNodes);
				return new Tbl(dsMB.Schema.T.WorkOrderStateHistory, TId.WorkOrderStateHistory,
					new Tbl.IAttr[] {
						WorkOrdersGroup,
						new ETbl(
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View, EdtMode.EditDefault, EdtMode.ViewDefault),
							ETbl.NewOnlyOnce(true),
							ETbl.UseNewConcurrency(true),
							ETbl.AllowConcurrencyErrorOverride(false),
							ETbl.RowEditType(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Id, 0, RecordManager.RowInfo.RowEditTypes.EditOnly),
							MB3ETbl.IsStateHistoryTbl(WorkOrderHistoryTable)
						)
					},
					WorkOrderStateHistoryNodes + extraNodes,
					inits.ToArray()
				);
			}));
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderStateHistory, new DelayedCreateTbl(delegate () {
				TblLayoutNodeArray extraNodes;
				List<TblActionNode> inits = StateHistoryInits(MB3Client.WorkOrderHistoryTable, out extraNodes);
				return new CompositeTbl(dsMB.Schema.T.WorkOrderStateHistory, TId.WorkOrderStateHistory,
					new Tbl.IAttr[] {
						WorkOrdersGroup,
						new BTbl(
							MB3BTbl.IsStateHistoryTbl(WorkOrderHistoryTable),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate, BTbl.ListColumnArg.Contexts.SortInitialDescending),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.Code, Fmt.SetDynamicSizing()),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistory.F.Comment)
						)
					},
					null,
					CompositeView.ChangeEditTbl(FindDelayedEditTbl(dsMB.Schema.T.WorkOrderStateHistory), NoNewMode)
				);
			}));
			#endregion
			#endregion

			#region WorkOrderAssignment
			AssigneeBrowsetteFromWorkOrderTblCreator = WorkOrderAssignmentAllBrowsetteTbl(true);
			WorkOrderBrowsetteFromAssigneeTblCreator = WorkOrderAssignmentAllBrowsetteTbl(false);
			// DefineTbl(dsMB.Schema.T.WorkOrderAssignment, WorkOrderAssignmentBrowseTbl());
			#endregion
			#region WorkOrderAssignee
			DefineTbl(dsMB.Schema.T.WorkOrderAssignee, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderAssignee, TId.WorkOrderAssignee,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignee.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignee.F.ContactID.F.BusinessPhone, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignee.F.ContactID.F.MobilePhone, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignee.F.Id.L.WorkOrderAssigneeStatistics.WorkOrderAssigneeID.F.NumNew, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignee.F.Id.L.WorkOrderAssigneeStatistics.WorkOrderAssigneeID.F.NumInProgress, NonPerViewColumn)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete)),
					TIReports.NewRemotePTbl(TIReports.WorkOrderAssigneeReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TIContact.SingleContactGroup(dsMB.Path.T.WorkOrderAssignee.F.ContactID),
						TblColumnNode.New(dsMB.Path.T.WorkOrderAssignee.F.ReceiveNotification, new FeatureGroupArg(MainBossServiceAsWindowsServiceGroup), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderAssignee.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.WorkOrderAssignment, TId.WorkOrderAssignee,
						TblColumnNode.NewBrowsette(WorkOrderBrowsetteFromAssigneeTblCreator, dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.WorkOrderAssignee, dsMB.Schema.T.WorkOrderAssignee);
			#endregion
			#region WorkOrderAssignmentByAssignee
			WorkOrderAssignmentByAssigneeTblCreator = new DelayedCreateTbl(delegate () {
				object assignedToColumnID = KB.I("codeName");
				DelayedCreateTbl assignedToGroup = new DelayedCreateTbl(
					delegate () {
						return new Tbl(dsMB.Schema.T.WorkOrderAssignmentByAssignee, TId.AssignedGroup,
							new Tbl.IAttr[] {
								WorkOrdersAssignmentsGroup,
							},
							new TblLayoutNodeArray(
								TblColumnNode.New(null, dsMB.Path.T.WorkOrderAssignmentByAssignee.F.Id, DCol.Normal)
							)
						);
					}
				);
				Key newAssignmentKey = TId.WorkOrderAssignment.ComposeCommand("New {0}");
				return new CompositeTbl(dsMB.Schema.T.WorkOrderAssignmentByAssignee, TId.WorkOrderAssignmentByAssignee,
					new Tbl.IAttr[] {
						WorkOrdersGroup,
						new BTbl(
							BTbl.PerViewListColumn(KB.K("Assigned to"), assignedToColumnID),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID.F.Subject)
						),
						new TreeStructuredTbl(null, 2),
						TIReports.NewRemotePTbl(TIReports.WorkOrderByAssigneeFormReport),
					},
					null,
					// The fake contact row for unassigned work orders; This displays because the XAFDB file specifies a text provider for its own ID field.
					new CompositeView(assignedToGroup, dsMB.Path.T.WorkOrderAssignmentByAssignee.F.Id, ReadonlyView,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.ContactID).IsNull()),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.WorkOrderAssignmentByAssignee.F.Id)
					),
					// Normal contact row for an assignee
					// TODO: The view should return the AssigneeID rather than the ContactID in all row types so we don't need this .L. linkage
					// TODO: We should then have extra verbs for direct Edit/View contact so the user doesn't have to drill into the assignee record.
					new CompositeView(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.ContactID.L.WorkOrderAssignee.ContactID,
						NoNewMode,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID).IsNull()),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.WorkOrderAssignee.F.ContactID.F.Code)
					),
					// TODO: These views show the WO in the panel. If an explicit assignment exists (WorkOrderAssignmentID != null) there should be a way to delete it.
					// (un)assigned draft WO
					new CompositeView(WorkOrderEditTblCreator, dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID).IsNotNull()
									.And(SqlExpression.Constant(KnownIds.WorkOrderStateDraftId).Eq(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID)))),
						CompositeView.SetParentPath(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.ContactID),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.WorkOrder.F.Number),
						CompositeView.IdentificationOverride(TId.DraftWorkOrder)
					),
					// (un)assigned open WO
					new CompositeView(WorkOrderEditTblCreator, dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID).IsNotNull()
									.And(SqlExpression.Constant(KnownIds.WorkOrderStateOpenId).Eq(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID)))),
						CompositeView.SetParentPath(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.ContactID),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.WorkOrder.F.Number),
						CompositeView.UseSamePanelAs(2),
						CompositeView.IdentificationOverride(TId.OpenWorkOrder)
					),
					// Allow creation of new assignments if a WO is selected.
					CompositeView.ExtraNewVerb(WorkOrderAssignmentEditTbl(true),
						CompositeView.JoinedNewCommand(newAssignmentKey),
						NoContextFreeNew,
						CompositeView.ContextualInit(
							new int[] { 2, 3 }, // corresponds to the composite views above on WorkOrders
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderID), dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID)
						)
					),
					// Allow creation of new assignments if an assignee is selected.
					CompositeView.ExtraNewVerb(WorkOrderAssignmentEditTbl(false),
						CompositeView.JoinedNewCommand(newAssignmentKey),
						NoContextFreeNew,
						CompositeView.ContextualInit(
							new int[] { 1 }, // corresponds to the composite view above on assignees
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.WorkOrderAssignment.F.WorkOrderAssigneeID), dsMB.Path.T.WorkOrderAssignmentByAssignee.F.ContactID.L.WorkOrderAssignee.ContactID.F.Id)
						)
					)
				);
			}
			);
			#endregion
		}
		internal static void DefineTblEntries() {
			#region ActualItem
			DefineTbl(dsMB.Schema.T.ActualItem, delegate () {
				return ActualItemTbl(false);
			});
			#endregion
			#region ActualLaborInside
			DefineTbl(dsMB.Schema.T.ActualLaborInside, delegate () {
				return ActualLaborInsideTbl(false);
			});
			#endregion
			#region ActualLaborOutsideNonPO
			DefineTbl(dsMB.Schema.T.ActualLaborOutsideNonPO, delegate () {
				return ActualLaborOutsideNonPOTbl(false);
			});
			#endregion
			#region ActualLaborOutsidePO
			DefineTbl(dsMB.Schema.T.ActualLaborOutsidePO, delegate () {
				return ActualLaborOutsidePOTbl(false);
			});
			#endregion
			#region ActualOtherWorkInside
			DefineTbl(dsMB.Schema.T.ActualOtherWorkInside, delegate () {
				return ActualOtherWorkInsideTbl(false);
			});
			#endregion
			#region ActualOtherWorkOutsideNonPO
			DefineTbl(dsMB.Schema.T.ActualOtherWorkOutsideNonPO, delegate () {
				return ActualOtherWorkOutsideNonPOTbl(false);
			});
			#endregion
			#region ActualOtherWorkOutsidePO
			DefineTbl(dsMB.Schema.T.ActualOtherWorkOutsidePO, delegate () {
				return ActualOtherWorkOutsidePOTbl(false);
			});
			#endregion
			#region ActualMiscellaneousWorkOrderCost
			DefineTbl(dsMB.Schema.T.ActualMiscellaneousWorkOrderCost, delegate () {
				return ActualMiscellaneousWorkOrderCostTbl(false);
			});
			#endregion

			#region BillableRequestor
			DefineTbl(dsMB.Schema.T.BillableRequestor, delegate () {
				return new Tbl(dsMB.Schema.T.BillableRequestor, TId.BillableRequestor,
				new Tbl.IAttr[] {
				WorkOrdersGroup,
				new BTbl(BTbl.ListColumn(dsMB.Path.T.BillableRequestor.F.ContactID.F.Code),
					BTbl.ListColumn(dsMB.Path.T.BillableRequestor.F.ContactID.F.BusinessPhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
					BTbl.ListColumn(dsMB.Path.T.BillableRequestor.F.ContactID.F.MobilePhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter)
				),
				new ETbl(),
				TIReports.NewRemotePTbl(TIReports.BillableRequestorReport)
			},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TIContact.ContactGroupTblLayoutNode(TIContact.ContactGroupRow(dsMB.Path.T.BillableRequestor.F.ContactID, ECol.Normal)),
						TblColumnNode.New(dsMB.Path.T.BillableRequestor.F.AccountsReceivableCostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting),
						TblColumnNode.New(dsMB.Path.T.BillableRequestor.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Chargeback, TId.BillableRequestor,
						TblColumnNode.NewBrowsette(TIWorkOrder.AllChargebackTbl, dsMB.Path.T.Chargeback.F.BillableRequestorID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.BillableRequestor, dsMB.Schema.T.BillableRequestor);
			#endregion

			#region ChargebackLine
			DefineEditTbl(dsMB.Schema.T.ChargebackLine, ChargebackLineEditTblCreator);
			#endregion
			#region ChargebackLineCategory
			DefineTbl(dsMB.Schema.T.ChargebackLineCategory, delegate () {
				return new Tbl(dsMB.Schema.T.ChargebackLineCategory, TId.ChargebackCategory,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.ChargebackLineCategory.F.Code), BTbl.ListColumn(dsMB.Path.T.ChargebackLineCategory.F.Desc)),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.ChargebackLineCategoryReport)
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.ChargebackLineCategory.F.Code, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ChargebackLineCategory.F.Desc, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ChargebackLineCategory.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.ChargebackLineCategory.F.Comment, DCol.Normal, ECol.Normal)
				));
			});
			RegisterExistingForImportExport(TId.ChargebackCategory, dsMB.Schema.T.ChargebackLineCategory);
			#endregion
			#region ChargebackActivity
			DefineBrowseTbl(dsMB.Schema.T.ChargebackActivity, new DelayedCreateTbl(
				delegate () {
					object correctedCostColumnId = KB.I("CorrectedCostId");
					dsMB.PathClass.PathToChargebackActivityRow root = dsMB.Path.T.ChargebackActivity;
					return new CompositeTbl(dsMB.Schema.T.ChargebackActivity, TId.ChargebackActivity,
						new Tbl.IAttr[] {
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.ListColumn(root.F.AccountingTransactionID.F.ChargebackLineID.F.ChargebackLineCategoryID.F.Code),
								BTbl.ListColumn(root.F.AccountingTransactionID.F.Cost),
								BTbl.PerViewListColumn(KB.K("Corrected Cost"), correctedCostColumnId)
							),
							new TreeStructuredTbl(root.F.ParentID, 2)
						},
						root.F.TableEnum,
						// TODO: Since both record variants have a valid root.F.AccountingTransactionID.F.ChargebackLineID.F.ChargebackID the unified column
						// root.F.ChargebackID is not required. It should be removed from the view, the PathAlias removed, and any calling browsette node
						// should refer to .F.AccountingTransactionID.F.ChargebackLineID.F.ChargebackID.
						// Table #0 - Chargeback
						new CompositeView(root.F.AccountingTransactionID.F.ChargebackLineID,
							BTbl.PerViewColumnValue(correctedCostColumnId, dsMB.Path.T.ChargebackLine.F.CorrectedCost),
							CompositeView.PathAlias(root.F.ChargebackID, dsMB.Path.T.ChargebackLine.F.ChargebackID),
							CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)),
						// Table #1 - Chargeback Correction
						new CompositeView(TIWorkOrder.ChargebackLineCorrectionTblCreator,
							root.F.AccountingTransactionID.F.ChargebackLineID, NoNewMode,
							CompositeView.PathAlias(root.F.ChargebackID, dsMB.Path.T.ChargebackLine.F.ChargebackID),
							CompositeView.JoinedNewCommand(CorrectGroup),
							CompositeView.ContextualInit(
								new int[] {
								(int)ViewRecordTypes.ChargebackActivity.Chargeback,
								(int)ViewRecordTypes.ChargebackActivity.ChargebackCorrection
							},
								// TODO: All but the first of the following Inits is the responsibility of the edit tbl, and likely are already automatically
								// created by the supporting accounting tbl-building infrastructure.
								new CompositeView.Init(dsMB.Path.T.ChargebackLine.F.CorrectionID, root.F.AccountingTransactionID.F.ChargebackLineID.F.CorrectionID),
								new CompositeView.Init(dsMB.Path.T.ChargebackLine.F.AccountingTransactionID.F.EffectiveDate, root.F.AccountingTransactionID.F.ChargebackLineID.F.AccountingTransactionID.F.EffectiveDate),
								new CompositeView.Init(dsMB.Path.T.ChargebackLine.F.AccountingTransactionID.F.FromCostCenterID, root.F.AccountingTransactionID.F.ChargebackLineID.F.AccountingTransactionID.F.FromCostCenterID),
								new CompositeView.Init(dsMB.Path.T.ChargebackLine.F.AccountingTransactionID.F.ToCostCenterID, root.F.AccountingTransactionID.F.ChargebackLineID.F.AccountingTransactionID.F.ToCostCenterID),
								new CompositeView.Init(dsMB.Path.T.ChargebackLine.F.ChargebackLineCategoryID, root.F.AccountingTransactionID.F.ChargebackLineID.F.ChargebackLineCategoryID)),
							CompositeView.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete))
							
					);
				}
			));
			#endregion

			#region CloseCode
			DefineTbl(dsMB.Schema.T.CloseCode, delegate () {
				return new Tbl(dsMB.Schema.T.CloseCode, TId.ClosingCode,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.CloseCode.F.Code), BTbl.ListColumn(dsMB.Path.T.CloseCode.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.CloseCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.CloseCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.CloseCode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.ClosingCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.CloseCodeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Task, TId.ClosingCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplate.F.CloseCodeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.ClosingCode, dsMB.Schema.T.CloseCode);
			#endregion

			#region Demand
#if KEEPBASETABE_TBL
			DefineTbl(dsMB.Schema.T.Demand, delegate() {
				return new Tbl(dsMB.Schema.T.Demand, TId.Demand,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.Demand.F.EntryDate)),
					new ETbl(ETbl.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					// WorkOrderID automatically assigned
					TblColumnNode.New(dsMB.Path.T.Demand.F.EntryDate, DColBase.Normal, new NonDefaultCol() ),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderID, new DColBase(DColBase.Display(dsMB.Path.T.WorkOrder.F.Number)), new EColBase(new EArg(TblLeafNode.Access.Readable, EdtMode.Edit, EdtMode.UnDelete, EdtMode.EditDefault))),
					TblColumnNode.New(dsMB.Path.T.Demand.F.CostEstimate, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(ExpenseCategory, dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID, new DColBase(DColBase.Display(dsMB.Path.T.WorkOrderExpenseCategory.F.Code)), EColBase.Normal)
				));
			});
#endif
			#endregion
			#region DemandItem
			DefineEditTbl(dsMB.Schema.T.DemandItem, delegate () {
				return DemandItemTbl(false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandItem, delegate () {
				return DemandItemTbl(true, false);
			});
			#endregion
			#region DemandLaborInside
			DefineEditTbl(dsMB.Schema.T.DemandLaborInside, delegate () {
				return DemandLaborInsideTbl(false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandLaborInside, delegate () {
				return DemandLaborInsideTbl(true, false);
			});
			#endregion
			#region DemandLaborOutside
			DefineEditTbl(dsMB.Schema.T.DemandLaborOutside, delegate () {
				return DemandLaborOutsideTbl(false, false, false, false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandLaborOutside, delegate () {
				return DemandLaborOutsideTbl(false, false, false, true, false);
			});
			#endregion
			#region DemandLaborOutsideActivity
			DefineBrowseTbl(dsMB.Schema.T.DemandLaborOutsideActivity, delegate () {
				return new CompositeTbl(dsMB.Schema.T.DemandLaborOutsideActivity, TId.ActualHourlyOutsideNoPO,
					new Tbl.IAttr[] {
						LaborResourcesGroup,
						new BTbl(
							BTbl.PerViewListColumn(EffectiveDateId, EffectiveDateId),
							BTbl.PerViewListColumn(quantityColumnId, quantityColumnId),
							BTbl.PerViewListColumn(costColumnId, costColumnId),
							BTbl.PerViewListColumn(correctedQuantityColumnId, correctedQuantityColumnId),
							BTbl.PerViewListColumn(correctedCostColumnId, correctedCostColumnId)
						),
						new TreeStructuredTbl(null, 3)
					},
					null,
					// ActualLaborOutsideNonPO
					new CompositeView(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualLaborOutsideNonPO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(correctedQuantityColumnId, dsMB.Path.T.ActualLaborOutsideNonPO.F.CorrectedQuantity),
						BTbl.PerViewColumnValue(correctedCostColumnId, dsMB.Path.T.ActualLaborOutsideNonPO.F.CorrectedCost),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsideNonPO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsideNonPO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandLaborOutsideActivity.F.DemandLaborOutsideID, dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID)
					),
					// ActualLaborOutsidePO
					new CompositeView(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.POLineLaborID.F.POLineID),
						CompositeView.ContextualInit(
							new int[] { 4 },
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.DemandLaborOutsideActivity.F.POLineID.F.POLineLaborID)
						),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(correctedQuantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.CorrectedQuantity),
						BTbl.PerViewColumnValue(correctedCostColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.CorrectedCost),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandLaborOutsideActivity.F.DemandLaborOutsideID, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID)
					),
					// ActualLaborOutsideNonPO Correction
					new CompositeView(TIWorkOrder.ActualLaborOutsideNonPOCorrectionTblCreator, dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.CorrectionID.F.AccountingTransactionID),
						CompositeView.ContextualInit(
							new int[] { 0, 2 },
							new CompositeView.Init(dsMB.Path.T.ActualLaborOutsideNonPO.F.CorrectionID, dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.CorrectionID)
						),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualLaborOutsideNonPO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsideNonPO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsideNonPO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandLaborOutsideActivity.F.DemandLaborOutsideID, dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID)
					),
					// ActualLaborOutsidePO Correction
					new CompositeView(TIWorkOrder.ActualLaborOutsidePOCorrectionTblCreator, dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID.F.AccountingTransactionID),
						CompositeView.ContextualInit(
							new int[] { 1, 3 },
							new CompositeView.Init(dsMB.Path.T.ActualLaborOutsidePO.F.CorrectionID, dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.DemandLaborOutsideActivity.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.POLineLaborID)     // This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandLaborOutsideActivity.F.DemandLaborOutsideID, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID)
					),
					// POLineLabor
					new CompositeView(dsMB.Path.T.DemandLaborOutsideActivity.F.POLineID.F.POLineLaborID,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandLaborOutsideActivity.F.POLineID).IsNotNull()),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderText),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineLabor.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineLabor.F.POLineID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandLaborOutsideActivity.F.DemandLaborOutsideID, dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID)
					)
				);
			});
			#endregion
			#region DemandOtherWorkInside
			DefineEditTbl(dsMB.Schema.T.DemandOtherWorkInside, delegate () {
				return DemandOtherWorkInsideTbl(false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandOtherWorkInside, delegate () {
				return DemandOtherWorkInsideTbl(true, false);
			});
			#endregion
			#region DemandOtherWorkOutside
			DefineEditTbl(dsMB.Schema.T.DemandOtherWorkOutside, delegate () {
				return DemandOtherWorkOutsideTbl(false, false, false, false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandOtherWorkOutside, delegate () {
				return DemandOtherWorkOutsideTbl(false, false, false, true, false);
			});
			#endregion
			#region DemandOtherWorkOutsideActivity
			DefineBrowseTbl(dsMB.Schema.T.DemandOtherWorkOutsideActivity, delegate () {
				return new CompositeTbl(dsMB.Schema.T.DemandOtherWorkOutsideActivity, TId.ActualPerJobOutsideNoPO,
					new Tbl.IAttr[] {
						LaborResourcesGroup,
						new BTbl(
							BTbl.PerViewListColumn(EffectiveDateId, EffectiveDateId),
							BTbl.PerViewListColumn(quantityColumnId, quantityColumnId),
							BTbl.PerViewListColumn(costColumnId, costColumnId),
							BTbl.PerViewListColumn(correctedQuantityColumnId, correctedQuantityColumnId),
							BTbl.PerViewListColumn(correctedCostColumnId, correctedCostColumnId)
						),
						new TreeStructuredTbl(null, 3)
					},
					null,
					// ActualOtherWorkOutsideNonPO
					new CompositeView(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(correctedQuantityColumnId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.CorrectedQuantity),
						BTbl.PerViewColumnValue(correctedCostColumnId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.CorrectedCost),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.DemandOtherWorkOutsideID, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID)
					),
					// ActualOtherWorkOutsidePO
					new CompositeView(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID).Eq(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID.F.POLineID),
						CompositeView.ContextualInit(
							new int[] { 4 },
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.DemandOtherWorkOutsideActivity.F.POLineID.F.POLineOtherWorkID)
						),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(correctedQuantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectedQuantity),
						BTbl.PerViewColumnValue(correctedCostColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectedCost),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.DemandOtherWorkOutsideID, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID)
					),
					// ActualOtherWorkOutsideNonPO Correction
					new CompositeView(TIWorkOrder.ActualOtherWorkOutsideNonPOCorrectionTblCreator, dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.CorrectionID.F.AccountingTransactionID),
						CompositeView.ContextualInit(
							new int[] { 0, 2 },
							new CompositeView.Init(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.CorrectionID, dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.CorrectionID)
						),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.DemandOtherWorkOutsideID, dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID)
					),
					// ActualOtherWorkOutsidePO Correction
					new CompositeView(TIWorkOrder.ActualOtherWorkOutsidePOCorrectionTblCreator, dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID,
						NoNewMode,
						CompositeView.JoinedNewCommand(CorrectGroup),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.Id))),
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID.F.AccountingTransactionID),
						CompositeView.ContextualInit(
							new int[] { 1, 3 },
							new CompositeView.Init(dsMB.Path.T.ActualOtherWorkOutsidePO.F.CorrectionID, dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID),
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.DemandOtherWorkOutsideActivity.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID)     // This is redundant on the inits in the Correction editor but is needed for the browser to recognize dead-ends based on PO state.
						),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.EffectiveDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.DemandOtherWorkOutsideID, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID)
					),
					// POLineOtherWork
					new CompositeView(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.POLineID.F.POLineOtherWorkID,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.POLineID).IsNotNull()),
						BTbl.PerViewColumnValue(EffectiveDateId, dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderText),
						BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.POLineOtherWork.F.Quantity),
						BTbl.PerViewColumnValue(costColumnId, dsMB.Path.T.POLineOtherWork.F.POLineID.F.Cost),
						CompositeView.PathAlias(dsMB.Path.T.DemandOtherWorkOutsideActivity.F.DemandOtherWorkOutsideID, dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID)
					)
				);
			});
			#endregion
			#region DemandMiscellaneousWorkOrderCost
			DefineEditTbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCost, delegate () {
				return DemandMiscellaneousWorkOrderCostTbl(false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCost, delegate () {
				return DemandMiscellaneousWorkOrderCostTbl(true, false);
			});
			#endregion

			#region DemandTemplate
#if KEEPBASETABLE_TBL
			DefineTbl(dsMB.Schema.T.DemandTemplate, delegate() {
				return new Tbl(dsMB.Schema.T.DemandTemplate, TId.TaskDemand,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.Delete))
				},
				new TblLayoutNodeArray(
					// WorkOrderID automatically assigned
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderTemplateID, new DColBase(DColBase.Display(dsMB.Path.T.WorkOrderTemplate.F.Code)), new EColBase(new EArg(TblLeafNode.Access.Readable, EdtMode.Edit, EdtMode.UnDelete, EdtMode.EditDefault))),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.EstimateCost, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(ExpenseCategory, dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID, new DColBase(DColBase.Display(dsMB.Path.T.WorkOrderExpenseCategory.F.Code)), EColBase.Normal)
				));
			});
#endif
			#endregion
			#region DemandItemTemplate
			DefineEditTbl(dsMB.Schema.T.DemandItemTemplate, delegate () {
				return DemandItemTemplateTbl(false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandItemTemplate, delegate () {
				return DemandItemTemplateTbl(true);
			});
			#endregion
			#region DemandLaborInsideTemplate
			DefineEditTbl(dsMB.Schema.T.DemandLaborInsideTemplate, delegate () {
				return DemandLaborInsideTemplateTbl(false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandLaborInsideTemplate, delegate () {
				return DemandLaborInsideTemplateTbl(true);
			});
			#endregion
			#region DemandLaborOutsideTemplate
			DefineEditTbl(dsMB.Schema.T.DemandLaborOutsideTemplate, delegate () {
				return DemandLaborOutsideTemplateTbl(false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandLaborOutsideTemplate, delegate () {
				return DemandLaborOutsideTemplateTbl(false, true);
			});
			#endregion
			#region DemandOtherWorkInsideTemplate
			DefineEditTbl(dsMB.Schema.T.DemandOtherWorkInsideTemplate, delegate () {
				return DemandOtherWorkInsideTemplateTbl(false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandOtherWorkInsideTemplate, delegate () {
				return DemandOtherWorkInsideTemplateTbl(true);
			});
			#endregion
			#region DemandOtherWorkOutsideTemplate
			DefineEditTbl(dsMB.Schema.T.DemandOtherWorkOutsideTemplate, delegate () {
				return DemandOtherWorkOutsideTemplateTbl(false, false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandOtherWorkOutsideTemplate, delegate () {
				return DemandOtherWorkOutsideTemplateTbl(false, true);
			});
			#endregion
			#region DemandMiscellaneousWorkOrderCostTemplate
			DefineEditTbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate, delegate () {
				return DemandMiscellaneousWorkOrderCostTemplateTbl(false);
			});
			DefineBrowseTbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate, delegate () {
				return DemandMiscellaneousWorkOrderCostTemplateTbl(true);
			});
			#endregion

			#region Employee
			EmployeeTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Employee, TId.Employee,
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.Employee.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.Employee.F.Desc)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.EmployeeReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TIContact.ContactGroupTblLayoutNode(TIContact.ContactGroupRow(dsMB.Path.T.Employee.F.ContactID, ECol.Normal)),
						TblColumnNode.New(dsMB.Path.T.Employee.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Employee.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.HourlyInside, TId.Employee,
						TblColumnNode.NewBrowsette(dsMB.Path.T.LaborInside.F.EmployeeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TaskDemandHourlyInside, TId.Employee,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID.F.EmployeeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PerJobInside, TId.Employee,
						TblColumnNode.NewBrowsette(dsMB.Path.T.OtherWorkInside.F.EmployeeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TaskDemandPerJobInside, TId.Employee,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID.F.EmployeeID, DCol.Normal, ECol.Normal))
				));
			});
			DefineTbl(dsMB.Schema.T.Employee, EmployeeTbl);
			RegisterExistingForImportExport(TId.Employee, dsMB.Schema.T.Employee);
			#endregion
			#region HourlyInside
			DefineTbl(dsMB.Schema.T.LaborInside, delegate () {
				return new Tbl(dsMB.Schema.T.LaborInside, TId.HourlyInside,
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.LaborInside.F.Code),
						BTbl.ListColumn(dsMB.Path.T.LaborInside.F.Desc, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.LaborInside.F.EmployeeID.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.LaborInside.F.TradeID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.LaborInside.F.Cost, NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.LaborInsideReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.EmployeeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Employee.F.ContactID.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.TradeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Trade.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.Cost, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.LaborInside.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.TaskDemandHourlyInside, TId.HourlyInside,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.HourlyInside, dsMB.Schema.T.LaborInside);
			#endregion
			#region HourlyOutside
			DefineTbl(dsMB.Schema.T.LaborOutside, delegate () {
				return new Tbl(dsMB.Schema.T.LaborOutside, TId.HourlyOutside,
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.LaborOutside.F.Code),
						BTbl.ListColumn(dsMB.Path.T.LaborOutside.F.Desc, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.LaborOutside.F.VendorID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.LaborOutside.F.TradeID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.LaborOutside.F.PurchaseOrderText, BTbl.ListColumnArg.Contexts.ClosedCombo|BTbl.ListColumnArg.Contexts.OpenCombo|BTbl.ListColumnArg.Contexts.SearchAndFilter, PurchasingGroup),
						BTbl.ListColumn(dsMB.Path.T.LaborOutside.F.Cost, NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.LaborOutsideReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.TradeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Trade.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.Cost, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.PurchaseOrderText, new FeatureGroupArg(PurchasingGroup), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.VendorID.F.AccountsPayableCostCenterID.F.Code, AccountingFeatureArg, DCol.Normal, ECol.AllReadonly, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting),
						TblColumnNode.New(dsMB.Path.T.LaborOutside.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.TaskDemandHourlyOutside, TId.HourlyOutside,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID, DCol.Normal, ECol.Normal))

				));
			});
			RegisterExistingForImportExport(TId.HourlyOutside, dsMB.Schema.T.LaborOutside);
			#endregion
			#region Per Job Inside
			DefineTbl(dsMB.Schema.T.OtherWorkInside, delegate () {
				return new Tbl(dsMB.Schema.T.OtherWorkInside, TId.PerJobInside,
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.OtherWorkInside.F.Code),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkInside.F.Desc, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkInside.F.EmployeeID.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkInside.F.TradeID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.OtherWorkInside.F.Cost, NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.OtherWorkInsideReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.EmployeeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Employee.F.ContactID.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.TradeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Trade.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.Cost, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.OtherWorkInside.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.TaskDemandPerJobInside, TId.PerJobInside,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.PerJobInside, dsMB.Schema.T.OtherWorkInside);
			#endregion
			#region Per Job Outside
			DefineTbl(dsMB.Schema.T.OtherWorkOutside, delegate () {
				return OtherWorkOutside();
			});
			RegisterExistingForImportExport(TId.PerJobOutside, dsMB.Schema.T.OtherWorkOutside);
			#endregion
			#region MiscellaneousWorkOrderCost
			DefineTbl(dsMB.Schema.T.MiscellaneousWorkOrderCost, delegate () {
				return new Tbl(dsMB.Schema.T.MiscellaneousWorkOrderCost, TId.MiscellaneousCost,
					new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Code),
						BTbl.ListColumn(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Desc, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Cost, NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewRemotePTbl(TIReports.MiscellaneousWorkOrderCostReport)
				},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Cost, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.MiscellaneousWorkOrderCost.F.CostCenterID, AccountingFeatureArg, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting),
							TblColumnNode.New(dsMB.Path.T.MiscellaneousWorkOrderCost.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.TaskDemandMiscellaneousCost, TId.MiscellaneousCost,
						TblColumnNode.NewBrowsette(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID, DCol.Normal, ECol.Normal))
					));
			});
			RegisterExistingForImportExport(TId.MiscellaneousCost, dsMB.Schema.T.MiscellaneousWorkOrderCost);
			#endregion
			#region	Project
			DefineTbl(dsMB.Schema.T.Project, delegate () {
				return new Tbl(dsMB.Schema.T.Project, TId.Project,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.Project.F.Code), BTbl.ListColumn(dsMB.Path.T.Project.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.Project.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Project.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Project.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.Project,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.ProjectID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Task, TId.Project,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplate.F.ProjectID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PurchaseOrder, TId.Project,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrder.F.ProjectID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PurchaseOrderTemplate, TId.Project,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PurchaseOrderTemplate.F.ProjectID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.Project, dsMB.Schema.T.Project);
			#endregion
			#region	Trade
			DefineTbl(dsMB.Schema.T.Trade, delegate () {
				return new Tbl(dsMB.Schema.T.Trade, TId.Trade,
				new Tbl.IAttr[] {
					LaborResourcesGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.Trade.F.Code), BTbl.ListColumn(dsMB.Path.T.Trade.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.Trade.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Trade.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Trade.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.HourlyInside, TId.Trade,
						TblColumnNode.NewBrowsette(dsMB.Path.T.LaborInside.F.TradeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PerJobInside, TId.Trade,
						TblColumnNode.NewBrowsette(dsMB.Path.T.OtherWorkInside.F.TradeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.HourlyOutside, TId.Trade,
						TblColumnNode.NewBrowsette(dsMB.Path.T.LaborOutside.F.TradeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.PerJobOutside, TId.Trade,
						TblColumnNode.NewBrowsette(dsMB.Path.T.OtherWorkOutside.F.TradeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.Trade, dsMB.Schema.T.Trade);
			#endregion
			#region WorkCategory
			DefineTbl(dsMB.Schema.T.WorkCategory, delegate () {
				return new Tbl(dsMB.Schema.T.WorkCategory, TId.WorkCategory,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkCategory.F.Code), BTbl.ListColumn(dsMB.Path.T.WorkCategory.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.WorkCategory.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkCategory.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkCategory.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.WorkCategory,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.WorkCategoryID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Task, TId.WorkCategory,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplate.F.WorkCategoryID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.WorkCategory, dsMB.Schema.T.WorkCategory);
			#endregion

			#region WorkOrderState
			DefineTbl(dsMB.Schema.T.WorkOrderState, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderState, TId.WorkOrderState,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderState.F.Code, Fmt.SetDynamicSizing()), BTbl.ListColumn(dsMB.Path.T.WorkOrderState.F.Desc)),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.Comment, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.DemandCountsActive, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.TemporaryStorageActive, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyActuals, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyPOLines, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyChargebacks, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyChargebackLines, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyDemands, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyDefinitionFields, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.CanModifyOperationalFields, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.AffectsFuturePMGeneration, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.FilterAsDraft, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.FilterAsOpen, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.FilterAsClosed, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderState.F.FilterAsVoid, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.WorkOrder, TId.WorkOrderState,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion
			#region WorkOrderStateHistoryStatus
			DefineTbl(dsMB.Schema.T.WorkOrderStateHistoryStatus, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderStateHistoryStatus, TId.WorkOrderStatus,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Code), BTbl.ListColumn(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.WorkOrderStatus,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateHistoryStatusID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion

			#region WorkOrder
			#region - Edit Tbls
			#region -   WorkOrderEditTblCreator
			WorkOrderEditTblCreator = WorkOrderEditTbl(WorkOrdersGroup);
			DefineEditTbl(dsMB.Schema.T.WorkOrder, WorkOrderEditTblCreator);
			DelayedCreateTbl WorkOrderUnassignedEditorTblCreator = WorkOrderEditTbl(WorkOrdersAssignmentsGroup, includeAssignToSelfCommand: true);
			DelayedCreateTbl WorkOrderAssignedToEditorTblCreator = WorkOrderEditTbl(WorkOrdersAssignmentsGroup);
			#endregion
			#region -   WorkOrderEditorFromRequestTbl
			WorkOrderEditorFromRequestTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrder, TId.WorkOrder,
					new Tbl.IAttr[] {
						WorkOrdersGroup,
						new ETbl(
							MB3ETbl.HasStateHistoryAndSequenceCounter(dsMB.Path.T.WorkOrder.F.Number, dsMB.Schema.T.WorkOrderSequenceCounter, dsMB.Schema.V.WOSequence, dsMB.Schema.V.WOSequenceFormat, WorkOrderHistoryTable),
							ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.Delete),
							ETbl.Print(TIReports.SingleWorkOrderFormReport, dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID)
						),
						TIReports.NewRemotePTbl(TIReports.WorkOrderFormReport)
					},
					WorkOrderNodes(),
					Init.OnLoadNew(new ControlTarget(WorkOrderDurationEstimateId), new VariableValue(dsMB.Schema.V.WODefaultDuration)),
					StartEndDurationCalculator(WorkOrderStartDateEstimateId, WorkOrderDurationEstimateId, WorkOrderEndDateEstimateId),
					Init.LinkRecordSets(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, 1, dsMB.Path.T.WorkOrder.F.Id, 0),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.WorkOrderStateHistory.F.UserID, 1), new UserIDValue()),
					Init.LinkRecordSets(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID, 2, dsMB.Path.T.WorkOrder.F.Id, 0),
					// The following inits don't work in Clone mode because the initial Edit buffer will only contain the Work Order to clone,
					// not the RequestedWorkOrder (because EditControl.GetFullRowIDs is not overridden to get their ID's). But since their targets
					// are in the work order itself (the record being cloned) the values will all be copied from there instead.
					Init.OnLoadNew(dsMB.Path.T.WorkOrder.F.RequestorID, new EditorPathValue(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.RequestorID, 2)),
					Init.OnLoadNew(dsMB.Path.T.WorkOrder.F.UnitLocationID, new EditorPathValue(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.UnitLocationID, 2)),
					Init.OnLoadNew(dsMB.Path.T.WorkOrder.F.Subject, new EditorPathValue(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.Subject, 2)),
					Init.OnLoadNew(dsMB.Path.T.WorkOrder.F.Description, new EditorPathValue(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.Description, 2)),
					Init.OnLoadNew(dsMB.Path.T.WorkOrder.F.AccessCodeID, new EditorPathValue(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.AccessCodeID, 2)),

					// Copy the WO Expense and AccessCode from unit if the checkbox is checked
					Init.New(new ControlTarget(WorkOrderExpenseModelId), new EditorPathValue(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.WorkOrderExpenseModelID), new Thinkage.Libraries.Presentation.ControlValue(UseUnitWorkOrderExpenseModel), TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone)),
					Init.New(new ControlTarget(AccessCodeId), new EditorPathValue(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.AccessCodeID), new Thinkage.Libraries.Presentation.ControlValue(UseUnitAccessCode), TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone)),
					// Arrange for WO / Access Code choices to be readonly if using the Unit's values
					Init.Continuous(new ControlReadonlyTarget(WorkOrderExpenseModelId, BecauseUsingUnitWorkOrderExpenseModel), new Thinkage.Libraries.Presentation.ControlValue(UseUnitWorkOrderExpenseModel)),
					Init.Continuous(new ControlReadonlyTarget(AccessCodeId, BecauseUsingUnitAccessCode), new Thinkage.Libraries.Presentation.ControlValue(UseUnitAccessCode))
				);
			});
			#endregion
			//  WorkOrderFromTemplateTbl is defined within its custom EditLogic class
			#endregion
			#region - Browsettes
			#region -   DefineTbl Definition, used for most browsettes
			DefineBrowseTbl(dsMB.Schema.T.WorkOrder, delegate () {
				return StandardWorkOrderBrowser(TId.WorkOrder, classifyByState: true);
			});
			#endregion
			#region -   WorkOrderBrowsetteFromUnitTbl, allows creation of new WOs
			WorkOrderBrowsetteFromUnitTblCreator = new DelayedCreateTbl(delegate () {
				return StandardWorkOrderBrowser(TId.WorkOrder, classifyByState: true, allowNewWO: true);
			});
			#endregion
			#endregion
			#region - Top-level (control-panel) browsettes
			#region -   WorkOrderAllBrowseTbl
			WorkOrderAllBrowseTbl = new DelayedCreateTbl(delegate () {
				return StandardWorkOrderBrowser(TId.WorkOrder, reportTblCreatorDelegate: () => TIReports.WorkOrderFormReport, classifyByState: true, allowNewWO: true, includeDefaultViews: true);
			});
			#endregion
			#region -   WorkOrderDraftBrowseTbl
			WorkOrderDraftBrowseTbl = new DelayedCreateTbl(delegate () {
			// We use ExpressionFilter instead since it does not turn into an Init directive in the new-mode editor.
			return StandardWorkOrderBrowser(TId.DraftWorkOrder, reportTblCreatorDelegate: () => TIReports.WorkOrderDraftFormReport, allowNewWO: true,
				extraBTblAttributes: BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsDraft).IsTrue()));
			});
			#endregion
			#region -   WorkOrderOpenBrowseTbl
			WorkOrderOpenBrowseTbl = new DelayedCreateTbl(delegate () {
				// We use ExpressionFilter instead since it does not turn into an Init directive in the new-mode editor.
				return StandardWorkOrderBrowser(TId.OpenWorkOrder, reportTblCreatorDelegate: () => TIReports.WorkOrderOpenFormReport,
					extraBTblAttributes: BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen).IsTrue()));
			});
			#endregion
			#region -   WorkOrderClosedBrowseTbl
			WorkOrderClosedBrowseTbl = new DelayedCreateTbl(delegate () {
				// We use ExpressionFilter instead since it does not turn into an Init directive in the new-mode editor.
				return StandardWorkOrderBrowser(TId.ClosedWorkOrder, reportTblCreatorDelegate: () => TIReports.WorkOrderClosedFormReport, listColumns: new BTbl.ICtorArg[] {
						WorkOrderNumberListColumn,
						WorkOrderClosingCodeListColumn,
						WorkOrderSubjectListColumn,
						WorkOrderStateAuthorListColumn
					},
					extraBTblAttributes: BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsClosed).IsTrue()));
			});
			#endregion
			#region -   WorkOrderVoidBrowseTbl
			WorkOrderVoidBrowseTbl = new DelayedCreateTbl(delegate () {
				// We use ExpressionFilter instead since it does not turn into an Init directive in the new-mode editor.
				return StandardWorkOrderBrowser(TId.VoidWorkOrder, reportTblCreatorDelegate: () => TIReports.WorkOrderVoidFormReport, listColumns: new BTbl.ICtorArg[] {
							WorkOrderNumberListColumn,
							WorkOrderSubjectListColumn,
							WorkOrderStateAuthorListColumn
					},
					extraBTblAttributes: BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsVoid).IsTrue()));
			});
			#endregion
			#region -   WorkOrderInProgressAssignedToBrowseTbl
			SelectSpecification AssignedWorkOrdersExpression = new SelectSpecification( // open w/o assigned to the current user
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID) },
							new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))
									.And(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen).IsTrue()),
							null);

			SqlExpression UserWorkOrderAssignmentsInProgress = new SqlExpression(dsMB.Path.T.WorkOrder.F.Id).In(AssignedWorkOrdersExpression);

			WorkOrderInProgressAssignedToBrowseTbl = new DelayedCreateTbl(delegate () {
				return StandardWorkOrderBrowser(TId.OpenWorkOrder, reportTblCreatorDelegate: () => TIReports.WorkOrderOpenAndAssignedFormReport, editTblCreator: WorkOrderAssignedToEditorTblCreator,
					featureGroup: WorkOrdersAssignmentsGroup, tableNameForPermissions: "AssignedWorkOrder",
					listColumns: new BTbl.ICtorArg[] {
							WorkOrderCurrentStateHistoryEffectiveDateListColumn,
							WorkOrderNumberListColumn,
							WorkOrderPriorityListColumnSortValue,
							WorkOrderPriorityListColumn,
							WorkOrderStatusListColumn,
							WorkOrderSubjectListColumn,
							WorkOrderStateAuthorListColumn
						},
					extraBTblAttributes: BTbl.ExpressionFilter(UserWorkOrderAssignmentsInProgress));
			});
			#endregion
			#region -   UnassignedWorkOrdersBrowseTbl
			SqlExpression UnassignedWorkOrderAssignments = new SqlExpression(dsMB.Path.T.WorkOrder.F.Id)
				// TODO: There are more efficient ways of doing this that do not involve the WorkOrderAssignmentByAssignee view.
				// e.g. NOT IN(select distinct WorkOrderAssignement.WorkOrderID from WorkOrderAssignment where WorkOrderAssignement.ContactID.Hidden is null)
				// or COUNT(select * from WorkOrderAssignment where WorkOrderAssignement.ContactID.Hidden is null and WorkOrderAssignement.WorkOrderID == outer WorkOrder.ID) == 0
				// Actually, the above replacements are not correct because they don't account for assignments implied by inside labor demands.
				// Actually actually, the above would be correct if they used WorkOrderAssignmentAll which includes the implicit assignments.
				.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID) },
							new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.ContactID).Eq(SqlExpression.Constant(KnownIds.UnassignedID))
									.And(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen)
										.Or(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentByAssignee.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsDraft))),
							null).SetDistinct(true));
			UnassignedWorkOrderBrowseTbl = new DelayedCreateTbl(delegate () {
				var assigneeIdExpression = SqlExpression.ScalarSubquery(new SelectSpecification(dsMB.Schema.T.WorkOrderAssignee, new[] { new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.Id) }, new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().UserRecordID)), null));
				var notAssigneeTip = KB.K("You are not registered as a Work Order Assignee");
				return StandardWorkOrderBrowser(TId.UnassignedWorkOrder, classifyByState: true, editTblCreator: WorkOrderUnassignedEditorTblCreator,
					featureGroup: WorkOrdersAssignmentsGroup, tableNameForPermissions: "UnassignedWorkOrder", 
					listColumns: new BTbl.ICtorArg[] {
							WorkOrderCurrentStateHistoryEffectiveDateListColumn,
							WorkOrderNumberListColumn,
							WorkOrderPriorityListColumnSortValue,
							WorkOrderPriorityListColumn,
							WorkOrderStatusListColumn,
							WorkOrderSubjectListColumn,
							WorkOrderStateAuthorListColumn
						},
					extraBTblAttributes: new[] {
							BTbl.ExpressionFilter(UnassignedWorkOrderAssignments),
							BTbl.AdditionalVerb(SelfAssignCommand,
								delegate (BrowseLogic browserLogic) {
									List<IDisablerProperties> disablers = SelfAssignDisablers();
									disablers.Insert(0, browserLogic.NeedSingleSelectionDisabler);
									return new MultiCommandIfAllEnabled(new CallDelegateCommand(SelfAssignTip,
										delegate () {
											SelfAssignmentEditor(browserLogic, browserLogic.BrowserSelectionPositioner.CurrentPosition.Id);
										}),
										disablers.ToArray()
									);
								})
							});
			});
			#endregion
			#region -   WorkOrderOverdueBrowseTbl
			WorkOrderOverdueBrowseTbl = new DelayedCreateTbl(delegate () {
				return StandardWorkOrderBrowser(TId.OverdueWorkOrder, reportTblCreatorDelegate: () => TIReports.WOOverdue, classifyByState: true, listColumns: new BTbl.ICtorArg[] {
							WorkOrderNumberListColumn,
							WorkOrderPriorityListColumnSortValue,
							WorkOrderPriorityListColumn,
							WorkOrderStatusListColumn,
							WorkOrderSubjectListColumn,
							WorkOrderOverdueListColumn
						},
					extraBTblAttributes: BTbl.ExpressionFilter(CommonExpressions.OverdueWorkOrderExpression));
			});
			#endregion
			#endregion
			#region - Pickers (and popup full browsers)
			#region -   AllWorkOrderTemporaryStoragePickerTblCreator
			AllWorkOrderTemporaryStoragePickerTblCreator = new DelayedCreateTbl(delegate () {
				return StandardWorkOrderBrowser(TId.WorkOrder, classifyByState: true);
			});
			#endregion
			#region -   AllWorkOrderChargebackBrowsePickerTblCreator
			AllWorkOrderChargebackBrowsePickerTblCreator = new DelayedCreateTbl(delegate () {
				return StandardWorkOrderBrowser(TId.WorkOrder, classifyByState: true);
			});
			#endregion
			#endregion
			#endregion

			#region WorkOrderExpenseCategory
			DefineTbl(dsMB.Schema.T.WorkOrderExpenseCategory, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderExpenseCategory, TId.ExpenseCategory,
					new Tbl.IAttr[] {
						AccountingGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseCategory.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseCategory.F.Desc)),
						new ETbl(),
						TIReports.NewRemotePTbl(TIReports.ExpenseCategoryReport),
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseCategory.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseCategory.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseCategory.F.Comment, DCol.Normal, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsItem, DCol.Normal, new ECol(Fmt.SetId(ExpenseClassItemId))),
								TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsLabor, DCol.Normal, new ECol(Fmt.SetId(ExpenseClassLaborId))),
								TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsMiscellaneous, DCol.Normal, new ECol(Fmt.SetId(ExpenseClassMiscellaneousId))
						),
						BrowsetteTabNode.New(TId.ExpenseMapping, TId.ExpenseCategory,
							TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID, DCol.Normal, ECol.Normal))
						)
					),
					new Check3<bool, bool, bool>(delegate (bool labor, bool item, bool misc) {
						if (!labor && !item && !misc)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("You must choose at least one of Item, Labor or Miscellaneous Expense Class")));
						return null;
					}).Operand1(ExpenseClassItemId).Operand2(ExpenseClassLaborId).Operand3(ExpenseClassMiscellaneousId)
				);
			});
			//			RegisterExistingBrowserImport(TId.ExpenseCategory, dsMB.Schema.T.WorkOrderExpenseCategory);

			FilteredWorkOrderExpenseCategoryTbl = new DelayedCreateTbl(delegate () {
				// This is essentially the same as the registered browse tbl for the schema, except that we have a custom logic class which introduces a special
				// parameterized filter that checks for the existence of a WOXME record for the WOXC record and for a WOXM Id equal to the parameter value.
				// It is used for the WOXC picker on the Demand and Demand Template records via a declaration in the XAFDB file.
				// TODO: When a new WOXME record is created the picker does not refresh.
				return new CompositeTbl(dsMB.Schema.T.WorkOrderExpenseCategory, TId.ExpenseCategory,
					new Tbl.IAttr[] {
						AccountingGroup,
						new BTbl(
							BTbl.LogicClass(typeof(FilteredWorkOrderExpenseCategoryBrowseLogic)),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseCategory.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseCategory.F.Desc)
						),
						TIReports.NewRemotePTbl(TIReports.ExpenseCategoryReport),
					},
					null,
					CompositeView.ChangeEditTbl(TIGeneralMB3.FindDelayedEditTbl(dsMB.Schema.T.WorkOrderExpenseCategory)),
					// TODO: Right now, in the called editor, the user can create/select any category even if it is not permitted for the type of demand. Fixing this would be painful
					// because all we have in the demand editor is a tbl-coded filter requiring one of the flags to be true. We would instead have to have three disablable filters (one for each flag)
					// with only one enabled. Because the disablable filters are parameterized and tagged we can use inits to see which one is in effect. These could be passed on to matching disablable
					// filters on the Category picker in the Entry editor but that requires creating such filters there first. Whether these would be passed in to a New Category editor is uncertain at this point.
					// Doing all this, though, would require adding yet another UI command to allow the user to modify a Category (already mapped or not) to permit it for this type of expense.
					// As things currently stand, the user gets to the Entry editor, can select the unsuitable Category, edit the record, and approve it for the type of demand. Then they can save the
					// Entry or not depending on whether one already exists.
					CompositeView.ExtraNewVerb(TIGeneralMB3.FindDelayedEditTbl(dsMB.Schema.T.WorkOrderExpenseModelEntry),
						CompositeView.ContextFreeInit(new BrowserFilterValue(FilteredWorkOrderExpenseCategoryBrowseLogic.ModelFilterId), dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID)
					)
				);
			});
			#endregion
			#region WorkOrderExpenseModel
			DefineTbl(dsMB.Schema.T.WorkOrderExpenseModel, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderExpenseModel, TId.ExpenseModel,
					new Tbl.IAttr[] {
						AccountingGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModel.F.Code), BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModel.F.Desc)),
						new ETbl(),
						TIReports.NewRemotePTbl(TIReports.ExpenseModelReport),
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.Comment, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.DefaultItemExpenseModelEntryID,
													new ECol(ECol.DisabledInNewAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsItem, true))),
													new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code)),
													new NonDefaultCol()),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.DefaultHourlyInsideExpenseModelEntryID,
													new ECol(ECol.DisabledInNewAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsLabor, true))),
													new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code)),
													new NonDefaultCol()),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.DefaultHourlyOutsideExpenseModelEntryID,
													new ECol(ECol.DisabledInNewAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsLabor, true))),
													new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code)),
													new NonDefaultCol()),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.DefaultPerJobInsideExpenseModelEntryID,
													new ECol(ECol.DisabledInNewAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsLabor, true))),
													new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code)),
													new NonDefaultCol()),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.DefaultPerJobOutsideExpenseModelEntryID,
													new ECol(ECol.DisabledInNewAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsLabor, true))),
													new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code)),
													new NonDefaultCol()),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.DefaultMiscellaneousExpenseModelEntryID,
													new ECol(ECol.DisabledInNewAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsMiscellaneous, true))),
													new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code)),
													new NonDefaultCol()),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.NonStockItemHoldingCostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal)),
						BrowsetteTabNode.New(TId.ExpenseMapping, TId.ExpenseModel,
							TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Unit, TId.ExpenseModel,
							TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID.F.UnitID.F.WorkOrderExpenseModelID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.WorkOrder, TId.ExpenseModel,
							TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.WorkOrderExpenseModelID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Task, TId.ExpenseModel,
							TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplate.F.WorkOrderExpenseModelID, DCol.Normal, ECol.Normal))
					));
			});
			RegisterForImportExport(TId.ExpenseModel, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderExpenseModel, TId.ExpenseModel,
						new Tbl.IAttr[] {
							AccountingGroup,
							new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
						},
						new TblLayoutNodeArray(
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.Comment, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModel.F.NonStockItemHoldingCostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal)
						)
					);
			}
			);

			// WorkOrderExpenseModel/WorkOrderExpenseModelEntry have a dependency loop that prevents importing/exporting at this time with the current import model. So we do not add
			// them to the Registered Import eligible tables. This means importing of WorkorderTemplates will be limited to those that use the Default Expense model.
			//			RegisterExistingBrowserImport(TId.ExpenseModel, dsMB.Schema.T.WorkOrderExpenseModel);
			#endregion
			#region WorkOrderExpenseModelEntry
			DefineTbl(dsMB.Schema.T.WorkOrderExpenseModelEntry, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderExpenseModelEntry, TId.ExpenseMapping,
				new Tbl.IAttr[] {
					AccountingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModelEntry.F.CostCenterID.F.Code)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete)),
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModel.F.Code)), ECol.Normal),
					TblGroupNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseCategory.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsItem, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsLabor, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.FilterAsMiscellaneous, DCol.Normal, ECol.AllReadonly)
					),
					TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting)
				));
			});
			RegisterForImportExport(TId.ExpenseMapping, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderExpenseModelEntry, TId.ExpenseMapping,
						new Tbl.IAttr[] {
							AccountingGroup,
							new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
						},
						new TblLayoutNodeArray(
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModel.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseCategory.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.WorkOrderExpenseModelEntry.F.CostCenterID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CostCenter.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting)
						)
					);
			}
			);
			WorkOrderExpenseModelEntryAsCategoryPickerTbl = new DelayedCreateTbl(delegate () {
				// This is a picker on WOXME records but shows them as if they were WOXC records.
				// This is used for defining the default expense model entries for various types of WO resource expenses.
				return new CompositeTbl(dsMB.Schema.T.WorkOrderExpenseModelEntry, TId.ExpenseMapping,
					new Tbl.IAttr[] {
						AccountingGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID.F.Desc),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderExpenseModelEntry.F.CostCenterID.F.Code, AccountingGroup)
						)
					},
					null,
					CompositeView.ChangeEditTbl(TIGeneralMB3.FindDelayedEditTbl(dsMB.Schema.T.WorkOrderExpenseModelEntry))
				);
			});
			#endregion
			#region WorkOrderPriority
			DefineTbl(dsMB.Schema.T.WorkOrderPriority, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderPriority, TId.WorkOrderPriority,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderPriority.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderPriority.F.Desc),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderPriority.F.Rank, BTbl.ListColumnArg.Contexts.SortInitialAscending|NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.WorkOrderPriority.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderPriority.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderPriority.F.Rank, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderPriority.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.WorkOrderPriority,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Task, TId.WorkOrderPriority,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplate.F.WorkOrderPriorityID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.WorkOrderPriority, dsMB.Schema.T.WorkOrderPriority);
			#endregion
			#region WorkOrderPurchaseOrder
			DefineTbl(dsMB.Schema.T.WorkOrderPurchaseOrder, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderPurchaseOrder, TId.WorkOrderPurchaseOrder,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderPurchaseOrder.F.WorkOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.WorkOrder.F.Subject.Key(), dsMB.Path.T.WorkOrderPurchaseOrder.F.WorkOrderID.F.Subject),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID.F.VendorID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.PurchaseOrder.F.Subject.Key(), dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID.F.Subject)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					TblFixedRecordTypeNode.New(),
					TblColumnNode.New(dsMB.Path.T.WorkOrderPurchaseOrder.F.WorkOrderID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrder.F.Number)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderPurchaseOrder.F.WorkOrderID.F.Subject, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrder.F.Number)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code))),
					TblColumnNode.New(dsMB.Path.T.WorkOrderPurchaseOrder.F.PurchaseOrderID.F.Subject, DCol.Normal)
				));
			});
			#endregion
			#region WorkOrderItems
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderItems,
				delegate () {
					// This condition can be applied to any Demand or Actual record (since the view fills in the appropriate field for both)
					var DemandMustHaveValidCategory
						= new CompositeView.Condition(
							new SqlExpression(dsMB.Path.T.WorkOrderItems.F.WorkOrderExpenseModelEntryID).IsNotNull(),
							KB.K("This Demand's Expense Category is not valid for the Work Order's Expense Model")
						);
					var DemandMustBeOnTempLocation
						= new CompositeView.Condition(
							new SqlExpression(dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID).IsNotNull(),
							KB.K("Receiving is only allowed to temporary storage assignments for this work order")
						);

					var viewStorageAssignmentVerb = KB.K("View Storage Assignment");
					var viewStorageAssignmentTip = KB.K("View the referenced storage assignment");

					object codeColumnId = KB.I("WorkOrderItemsCodeId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderItems, TId.WorkOrderItem,
						new Tbl.IAttr[] {
							ItemResourcesGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.ListColumn(DemandedColumnKey, dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.Quantity),
								BTbl.ListColumn(DemandedCostColumnKey, dsMB.Path.T.WorkOrderItems.F.DemandID.F.CostEstimate),
								BTbl.ListColumn(ActualColumnKey, dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ActualQuantity),
								BTbl.ListColumn(ActualCostColumnKey, dsMB.Path.T.WorkOrderItems.F.DemandID.F.ActualCost)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ItemID,
								dsMB.Schema.T.WorkOrderItemsTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderItems.F.TableEnum,
						new CompositeView(dsMB.Path.T.WorkOrderItems.F.ItemID,
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Item.F.Code),
							ReadonlyView),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand
						// a resource not already mentioned in this WO's Resource view. We do this init when the current record is either an
						// appropriate resource, or a demand of the same type.
						// We separate demands on temp and perm storage so that the Edit Storage Assignment extra verb can work.
						new CompositeView(dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandItem.F.ItemLocationID.F.LocationID.F.Code),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderItems.DemandItem,
								new CompositeView.Init(dsMB.Path.T.DemandItem.F.ItemLocationID, dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID)
							),
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AdditionalViewVerb(
								viewStorageAssignmentVerb, viewStorageAssignmentTip,
								null,
								dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID,
								new SqlExpression(dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID).IsNotNull(),
								KB.K("Demand is from Temporary Storage")),
							CompositeView.AdditionalViewVerb(
								viewStorageAssignmentVerb, viewStorageAssignmentTip,
								TIItem.AllTemporaryItemLocationTblCreator,
								dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID,
								new SqlExpression(dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID).IsNotNull(),
								KB.K("Demand is from Permanent Storage"))
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualItem), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderItems.DemandItem,
								new CompositeView.Condition[] {
									DemandMustHaveValidCategory
								},
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualItem.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderItems.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualItem.F.DemandItemID), dsMB.Path.T.WorkOrderItems.F.DemandID.F.DemandItemID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						)
					);
				}
			);
			#endregion
			#region WorkOrderInside
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderInside,
				delegate () {
					DelayedCreateTbl resourceGroupTbl = new DelayedCreateTbl(
						delegate () {
							return new Tbl(dsMB.Schema.T.WorkOrderInside, TId.ResourceGroup,
								new Tbl.IAttr[] {
									LaborResourcesGroup
								},
								new TblLayoutNodeArray(
									TblColumnNode.New(dsMB.Path.T.WorkOrderInside.F.Id, DCol.Normal)
								)
							);
						}
					);
					CompositeView.Condition[] DemandMustHaveValidCategory = new CompositeView.Condition[] {
						new CompositeView.Condition(
							new SqlExpression(dsMB.Path.T.WorkOrderInside.F.WorkOrderExpenseModelEntryID).IsNotNull(),
							KB.K("This Demand's Expense Category is not valid for the Work Order's Expense Model")
						)
					};

					object codeColumnId = KB.I("WorkOrderLaborInsideCodeId");
					object demandQuantityColumnId = KB.I("WorkOrderLaborInsideDemandQuantityId");
					object demandCostColumnId = KB.I("WorkOrderResourceDemandCostId");
					object actualQuantityColumnId = KB.I("WorkOrderLaborInsideActualQuantityId");
					object actualCostColumnId = KB.I("WorkOrderResourceActualCostId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderInside, TId.WorkOrderInside,
						new Tbl.IAttr[] {
							LaborResourcesGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(DemandedColumnKey, demandQuantityColumnId),
								BTbl.PerViewListColumn(DemandedCostColumnKey, demandCostColumnId),
								BTbl.PerViewListColumn(ActualColumnKey, actualQuantityColumnId),
								BTbl.PerViewListColumn(ActualCostColumnKey, actualCostColumnId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderInside.F.ParentID,
								dsMB.Schema.T.WorkOrderInsideTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderInside.F.TableEnum,
						new CompositeView(resourceGroupTbl, dsMB.Path.T.WorkOrderInside.F.Id, ReadonlyView, CompositeView.ForceNotPrimary(), CompositeView.IdentificationOverride(TId.Trade),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.WorkOrderInside.F.Id)),
						new CompositeView(dsMB.Path.T.WorkOrderInside.F.TradeID,
							ReadonlyView,
							CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Trade.F.Code)
						),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand a resource not
						// already mentioned in this WO's Resource view. We do this init when the current record is either an appropriate resource, or a demand of the
						// same type.
						new CompositeView(dsMB.Path.T.WorkOrderInside.F.DemandID.F.DemandLaborInsideID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandLaborInside.F.LaborInsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandLaborInside.F.Quantity, IntervalFormat),
							BTbl.PerViewColumnValue(demandCostColumnId, dsMB.Path.T.DemandLaborInside.F.DemandID.F.CostEstimate),
							BTbl.PerViewColumnValue(actualQuantityColumnId, dsMB.Path.T.DemandLaborInside.F.ActualQuantity, IntervalFormat),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.DemandLaborInside.F.DemandID.F.ActualCost),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderInside.DemandLaborInside,
								new CompositeView.Init(dsMB.Path.T.DemandLaborInside.F.LaborInsideID, dsMB.Path.T.WorkOrderInside.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID)
							),
							CompositeView.NewCommandGroup(NewDemandGroup)
						),
						new CompositeView(dsMB.Path.T.WorkOrderInside.F.DemandID.F.DemandOtherWorkInsideID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandOtherWorkInside.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(demandCostColumnId, dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.CostEstimate),
							BTbl.PerViewColumnValue(actualQuantityColumnId, dsMB.Path.T.DemandOtherWorkInside.F.ActualQuantity, IntegralFormat),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.ActualCost),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderInside.DemandOtherWorkInside,
								new CompositeView.Init(dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID, dsMB.Path.T.WorkOrderInside.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID)
							),
							CompositeView.NewCommandGroup(NewDemandGroup)
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualLaborInside), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderInside.DemandLaborInside,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborInside.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderInside.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID), dsMB.Path.T.WorkOrderInside.F.DemandID.F.DemandLaborInsideID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualOtherWorkInside), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderInside.DemandOtherWorkInside,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkInside.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderInside.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID), dsMB.Path.T.WorkOrderInside.F.DemandID.F.DemandOtherWorkInsideID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						)
					);
				}
			);
			#endregion
			#region WorkOrderOutside
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderOutside,
				delegate () {
					DelayedCreateTbl resourceGroupTbl = new DelayedCreateTbl(
						delegate () {
							return new Tbl(dsMB.Schema.T.WorkOrderOutside, TId.ResourceGroup,
								new Tbl.IAttr[] {
									LaborResourcesGroup
								},
								new TblLayoutNodeArray(
									TblColumnNode.New(dsMB.Path.T.WorkOrderOutside.F.Id, DCol.Normal)
								)
							);
						}
					);
					CompositeView.Condition[] DemandMustHaveValidCategory = new CompositeView.Condition[] {
						new CompositeView.Condition(
							new SqlExpression(dsMB.Path.T.WorkOrderOutside.F.WorkOrderExpenseModelEntryID).IsNotNull(),
							KB.K("This Demand's Expense Category is not valid for the Work Order's Expense Model")
						)
					};
					object OrderedQuantityId = KB.I("OrderedQuantityId");
					// TODO: when an orderedCost exists in the DemandxxOutside records					object OrderedCostId = KB.I("OrderedCostId");
					Key newPOLineGroup = KB.K("New PO Line");

					object codeColumnId = KB.I("WorkOrderLaborOutsideCodeId");
					object demandQuantityColumnId = KB.I("WorkOrderLaborOutsideDemandQuantityId");
					object demandCostColumnId = KB.I("WorkOrderResourceDemandCostId");
					object actualQuantityColumnId = KB.I("WorkOrderLaborInsideActualQuantityId");
					object actualCostColumnId = KB.I("WorkOrderResourceActualCostId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderOutside, TId.WorkOrderOutside,
						new Tbl.IAttr[] {
							LaborResourcesGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(DemandedColumnKey, demandQuantityColumnId),
								BTbl.PerViewListColumn(DemandedCostColumnKey, demandCostColumnId),
								BTbl.PerViewListColumn(OrderedQuantityColumnKey, OrderedQuantityId),
// TODO: when OrderedCost exists in DemandxxOutside records 								BTbl.CompositeViewMatchingIdColumn(KB.K("Ordered Cost"), OrderedCostId),
								BTbl.PerViewListColumn(ActualColumnKey, actualQuantityColumnId),
								BTbl.PerViewListColumn(ActualCostColumnKey, actualCostColumnId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderOutside.F.ParentID,
								dsMB.Schema.T.WorkOrderOutsideTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderOutside.F.TableEnum,
						new CompositeView(resourceGroupTbl, dsMB.Path.T.WorkOrderOutside.F.Id, ReadonlyView, CompositeView.ForceNotPrimary(), CompositeView.IdentificationOverride(TId.Trade),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.WorkOrderOutside.F.Id)),
						new CompositeView(dsMB.Path.T.WorkOrderOutside.F.TradeID,
							ReadonlyView,
							CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Trade.F.Code)
						),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand a resource not
						// already mentioned in this WO's Resource view. We do this init when the current record is either an appropriate resource, or a demand of the
						// same type.
						new CompositeView(dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandLaborOutsideID,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.DemandLaborOutside,
								new CompositeView.Init(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID, dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID)
							),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandLaborOutside.F.Quantity, IntervalFormat),
							BTbl.PerViewColumnValue(demandCostColumnId, dsMB.Path.T.DemandLaborOutside.F.DemandID.F.CostEstimate),
							BTbl.PerViewColumnValue(OrderedQuantityId, dsMB.Path.T.DemandLaborOutside.F.OrderQuantity, IntervalFormat),
							BTbl.PerViewColumnValue(actualQuantityColumnId, dsMB.Path.T.DemandLaborOutside.F.ActualQuantity, IntervalFormat),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.DemandLaborOutside.F.DemandID.F.ActualCost),
							CompositeView.NewCommandGroup(NewDemandGroup)
						),
						new CompositeView(dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandOtherWorkOutsideID,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.DemandOtherWorkOutside,
								new CompositeView.Init(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID, dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID)
							),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandOtherWorkOutside.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(demandCostColumnId, dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.CostEstimate),
							BTbl.PerViewColumnValue(OrderedQuantityId, dsMB.Path.T.DemandOtherWorkOutside.F.OrderQuantity, IntegralFormat),
							BTbl.PerViewColumnValue(actualQuantityColumnId, dsMB.Path.T.DemandOtherWorkOutside.F.ActualQuantity, IntegralFormat),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.ActualCost),
							CompositeView.NewCommandGroup(NewDemandGroup)
						),
						new CompositeView(dsMB.Path.T.WorkOrderOutside.F.POLineID.F.POLineLaborID, NoNewMode,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID.F.VendorID.F.Code),
							BTbl.PerViewColumnValue(OrderedQuantityId, dsMB.Path.T.POLineLabor.F.Quantity, IntervalFormat),
							BTbl.PerViewColumnValue(actualQuantityColumnId, dsMB.Path.T.POLineLabor.F.ReceiveQuantity, IntervalFormat),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.POLineLabor.F.POLineID.F.ReceiveCost),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.DemandLaborOutside,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID), dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandLaborOutsideID)
							),
							CompositeView.JoinedNewCommand(newPOLineGroup)
						),
						new CompositeView(dsMB.Path.T.WorkOrderOutside.F.POLineID.F.POLineOtherWorkID, NoNewMode,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID.F.VendorID.F.Code),
							BTbl.PerViewColumnValue(OrderedQuantityId, dsMB.Path.T.POLineOtherWork.F.Quantity, IntegralFormat),
							BTbl.PerViewColumnValue(actualQuantityColumnId, dsMB.Path.T.POLineOtherWork.F.ReceiveQuantity, IntegralFormat),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.POLineOtherWork.F.POLineID.F.ReceiveCost),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.DemandOtherWorkOutside,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID), dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandOtherWorkOutsideID)
							),
							CompositeView.JoinedNewCommand(newPOLineGroup)
						),
						// Actualization Non-po (2 views)
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualLaborOutsideNonPO), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.DemandLaborOutside,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsideNonPO.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderOutside.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID), dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandLaborOutsideID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualOtherWorkOutsideNonPO), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.DemandOtherWorkOutside,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderOutside.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID), dsMB.Path.T.WorkOrderOutside.F.DemandID.F.DemandOtherWorkOutsideID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						),
						// Actualization of POLines (2 views)
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualLaborOutsidePO), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.POLineLabor,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderOutside.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID), dsMB.Path.T.WorkOrderOutside.F.POLineID.F.POLineLaborID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualOtherWorkOutsidePO), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderOutside.POLineOtherWork,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderOutside.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID), dsMB.Path.T.WorkOrderOutside.F.POLineID.F.POLineOtherWorkID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						)
					);
				}
			);
			#endregion
			#region WorkOrderMiscellaneous
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderMiscellaneous,
				delegate () {
					CompositeView.Condition[] DemandMustHaveValidCategory = new CompositeView.Condition[] {
						new CompositeView.Condition(
							new SqlExpression(dsMB.Path.T.WorkOrderMiscellaneous.F.WorkOrderExpenseModelEntryID).IsNotNull(),
							KB.K("This Demand's Expense Category is not valid for the Work Order's Expense Model")
						)
					};
					object miscellaneousCodeColumnId = KB.I("WorkOrderMiscellaneousCodeColumnId");
					object demandCostColumnId = KB.I("WorkOrderResourceDemandCostId");
					object actualCostColumnId = KB.I("WorkOrderResourceActualCostId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderMiscellaneous, TId.WorkOrderMiscellaneousExpense,
						new Tbl.IAttr[] {
							ItemResourcesGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, miscellaneousCodeColumnId),
								BTbl.PerViewListColumn(DemandedCostColumnKey, demandCostColumnId),
								BTbl.PerViewListColumn(ActualCostColumnKey, actualCostColumnId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderMiscellaneous.F.ParentID, dsMB.Schema.T.WorkOrderMiscellaneousTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderMiscellaneous.F.TableEnum,
						new CompositeView(dsMB.Path.T.WorkOrderMiscellaneous.F.MiscellaneousWorkOrderCostID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(miscellaneousCodeColumnId, dsMB.Path.T.MiscellaneousWorkOrderCost.F.Code)
						),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand a resource not
						// already mentioned in this WO's Resource view. We do this init when the current record is either an appropriate resource, or a demand of the
						// same type.
						new CompositeView(dsMB.Path.T.WorkOrderMiscellaneous.F.DemandID.F.DemandMiscellaneousWorkOrderCostID,
							BTbl.PerViewColumnValue(miscellaneousCodeColumnId, dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.EntryDate, TIWorkOrder.WorkOrderResourceActivityDateAsCodeWrapper()),
							BTbl.PerViewColumnValue(demandCostColumnId, dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.CostEstimate),
							BTbl.PerViewColumnValue(actualCostColumnId, dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.ActualCost),
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderMiscellaneous.F.WorkOrderID, dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.WorkOrderID),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderMiscellaneous.MiscellaneousWorkOrderCost,
								new CompositeView.Init(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID, dsMB.Path.T.WorkOrderMiscellaneous.F.MiscellaneousWorkOrderCostID)
							),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderMiscellaneous.DemandMiscellaneousWorkOrderCost,
								new CompositeView.Init(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID, dsMB.Path.T.WorkOrderMiscellaneous.F.DemandID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID)
							)
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.ActualMiscellaneousWorkOrderCost), NoNewMode,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderMiscellaneous.DemandMiscellaneousWorkOrderCost,
								DemandMustHaveValidCategory,
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.AccountingTransactionID.F.ToCostCenterID), dsMB.Path.T.WorkOrderMiscellaneous.F.WorkOrderExpenseModelEntryID.F.CostCenterID),
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID), dsMB.Path.T.WorkOrderMiscellaneous.F.DemandID.F.DemandMiscellaneousWorkOrderCostID)
							),
							CompositeView.JoinedNewCommand(ActualizeGroup)
						)
					);
				}
			);
			#endregion
			#region WorkOrderTemporaryStorage
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemporaryStorage,
				delegate () {
					object codeId = KB.I("WO temp storage Code column id");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderTemporaryStorage, TId.WorkOrderTemporaryStorage,
						new Tbl.IAttr[] {
							ItemResourcesGroup,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeId),
								BTbl.ListColumn(dsMB.Path.T.WorkOrderTemporaryStorage.F.ItemLocationID.F.ActualItemLocationID.F.OnHand),
								BTbl.ListColumn(dsMB.Path.T.WorkOrderTemporaryStorage.F.ItemLocationID.F.ActualItemLocationID.F.Available)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemporaryStorage.F.ParentID, dsMB.Schema.T.WorkOrderTemporaryStorageTreeView, 4, 4)
						},
						dsMB.Path.T.WorkOrderTemporaryStorage.F.TableEnum,
						// Postal address
						new CompositeView(dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID.F.PostalAddressID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.PostalAddress.F.Code)),
						// Temporary Storage
						new CompositeView(TIItem.AllTemporaryStorageTblCreator, dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID.F.TemporaryStorageID,
							// not sure what to show in the list column
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemporaryStorage.F.WorkOrderID, dsMB.Path.T.TemporaryStorage.F.WorkOrderID),
							CompositeView.ContextualInit(
								new int[] {
									(int)ViewRecordTypes.WorkOrderTemporaryStorage.PermanentStorage,
									(int)ViewRecordTypes.WorkOrderTemporaryStorage.PlainRelativeLocation,
									(int)ViewRecordTypes.WorkOrderTemporaryStorage.Unit,
									(int)ViewRecordTypes.WorkOrderTemporaryStorage.PostalAddress
								},
								new CompositeView.Init(dsMB.Path.T.TemporaryStorage.F.ContainingLocationID, dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID)
							),
							CompositeView.ContextualInit(
								new int[] {
									(int)ViewRecordTypes.WorkOrderTemporaryStorage.TemporaryItemLocation,
								},
								new CompositeView.Init(dsMB.Path.T.TemporaryStorage.F.ContainingLocationID, dsMB.Path.T.WorkOrderTemporaryStorage.F.ItemLocationID.F.LocationID)
							)
						),
						// Unit
						new CompositeView(dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID.F.RelativeLocationID.F.UnitID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.Unit.F.RelativeLocationID.F.Code)),
						// Permanent Storage
						new CompositeView(dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID.F.RelativeLocationID.F.PermanentStorageID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code)),
						// Plain Relative Location
						new CompositeView(dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID.F.RelativeLocationID.F.PlainRelativeLocationID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code)),
						// Template Temporary Storage (impossible in this view)
						null,
						// Temporary Storage Assignment
						new CompositeView(TIItem.AllTemporaryItemLocationTblCreator, dsMB.Path.T.WorkOrderTemporaryStorage.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID,
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemporaryStorage.F.WorkOrderID, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.TemporaryStorageID.F.WorkOrderID),
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemporaryStorage.F.WorkOrderID, dsMB.Path.T.TemporaryItemLocation.F.WorkOrderID),
							NoContextFreeNew,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemporaryStorage.TemporaryItemLocation,
								new CompositeView.Init(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.WorkOrderTemporaryStorage.F.ItemLocationID.F.LocationID)
							),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemporaryStorage.TemporaryStorage,
								new CompositeView.Init(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.WorkOrderTemporaryStorage.F.LocationID)
							)
						),
						CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.DemandItem),
							NoContextFreeNew,
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemporaryStorage.TemporaryItemLocation,
								new CompositeView.Init(dsMB.Path.T.DemandItem.F.ItemLocationID, dsMB.Path.T.WorkOrderTemporaryStorage.F.ItemLocationID),
								// Because this is not making a record that appears in the browse data, we cannot use a PathAlias to express
								// that dsMB.Path.T.WorkOrderTemporaryStorage.F.WorkOrderID and thus the browsette filter WO from the containing editor
								// should be inited to dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderID.
								// Instead we have to express this init explicitly, and we use a PathOrFilterTarget as the implied init would have done.
								// This makes it a forced value in the editor so the user cannot switch to a different WO, and as a result Dead-end disabling also works.
								new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderID), dsMB.Path.T.WorkOrderTemporaryStorage.F.WorkOrderID)
							)
						)
					);
				}
			);
			#endregion

			#region WorkOrderTemplate and WorkOrderTemplateFromWorkOrder
			var detailsNodes = new List<TblLayoutNode>(new TblLayoutNode[] {
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Code, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Desc, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Subject, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID, new NonDefaultCol(),
					new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderTemplate.F.Code)),
					new ECol(ECol.NormalAccess,
						Fmt.SetBrowserFilter(BTbl.ExpressionFilter(
							new SqlExpression(dsMB.Path.T.WorkOrderTemplate.F.Id)
								.In(new SelectSpecification(dsMB.Schema.T.WorkOrderTemplateContainment,
															new[] { new SqlExpression(dsMB.Path.T.WorkOrderTemplateContainment.F.ContainedWorkOrderTemplateID) },
															new SqlExpression(dsMB.Path.T.WorkOrderTemplateContainment.F.ContainingWorkOrderTemplateID).Eq(new SqlExpression(dsMB.Path.T.WorkOrderTemplate.F.Id, 2)),
															null)).Not())))),
				TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					new Key[] { KB.K("Work Duration"), KB.K("Generate Lead Time") },
					TblRowNode.New(KB.K("This Task"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Duration, DCol.Normal, ECol.Normal, Fmt.SetId(LocalWorkDurationId)),
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.GenerateLeadTime, DCol.Normal, ECol.Normal, Fmt.SetId(LocalGenerateLeadTimeId))
					),
					TblRowNode.New(KB.K("Basis Task"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.Duration, DCol.Normal, new ECol(ECol.AllReadonlyAccess, ECol.OptionalValue()), Fmt.SetId(BasisWorkDurationId)),
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.GenerateLeadTime, DCol.Normal, new ECol(ECol.AllReadonlyAccess, ECol.OptionalValue()), Fmt.SetId(BasisGenerateLeadTimeId))
					),
					TblRowNode.New(KB.K("Effective Values"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Id.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.Duration, DCol.Normal),
						TblUnboundControlNode.New(KB.K("Work Duration"), dsMB.Schema.T.WorkOrderTemplate.F.Duration.EffectiveType, Fmt.SetId(NetWorkDurationId), ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Id.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.GenerateLeadTime, DCol.Normal),
						TblUnboundControlNode.New(KB.K("Generate Lead Time"), dsMB.Schema.T.WorkOrderTemplate.F.GenerateLeadTime.EffectiveType, Fmt.SetId(NetGenerateLeadTimeId), ECol.AllReadonly)
					)
				),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.WorkCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkCategory.F.Code)), ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.AccessCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.AccessCode.F.Code)), ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.WorkOrderPriorityID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderPriority.F.Code)), ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.WorkOrderExpenseModelID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModel.F.Code)), ECol.Normal, CommonNodeAttrs.PermissionToViewAccounting, CommonNodeAttrs.PermissionToEditAccounting, AccountingFeatureArg),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.ProjectID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Project.F.Code)), ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Description, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Downtime, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.CloseCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.CloseCode.F.Code)), ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.ClosingComment, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.Comment, DCol.Normal, ECol.Normal),
				TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.SelectPrintFlag, DCol.Normal, ECol.Normal)
			});
			var browsetteTabs = new List<TblTabNode>(new[] {
				BrowsetteTabNode.New(TId.TaskResource, TId.Task, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplate.F.DemandCount, new FeatureGroupArg(SchedulingAndAnyResourcesGroup), new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInBrowsetteArea)), ECol.AllReadonly),
					BrowsetteTabNode.New(TId.TaskItem, TId.Task,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplateItems.F.DemandTemplateID.F.WorkOrderTemplateID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TaskInside, TId.Task,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplateInside.F.DemandTemplateID.F.WorkOrderTemplateID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TaskOutside, TId.Task,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplateOutside.F.DemandTemplateID.F.WorkOrderTemplateID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TaskMiscellaneousExpense, TId.Task,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.DemandTemplateID.F.WorkOrderTemplateID, DCol.Normal, ECol.Normal))
				),
				BrowsetteTabNode.New(TId.TaskTemporaryStorage, TId.Task,
				// We don't use TILocations.TemporaryItemLocationBrowseTblCreator because it does not permit direct creation of temp storage nor would it show any as primary
					TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplateStorage.F.WorkOrderTemplateID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.PurchaseOrderTemplate, TId.Task,
					TblColumnNode.NewBrowsette(AssociatedPurchaseOrderTemplatesTbl, dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplateView.F.LinkedWorkOrderTemplateID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.UnitMaintenancePlan, TId.Task,
					TblColumnNode.NewBrowsette(TISchedule.ScheduledWorkOrderBrowserTbl, dsMB.Path.T.ScheduledWorkOrder.F.WorkOrderTemplateID, DCol.Normal, ECol.Normal))
			});
			var commonActions = new List<TblActionNode>(new TblActionNode[] {
				new Check3<TimeSpan, TimeSpan?, TimeSpan>()
					.Operand1(LocalWorkDurationId)
					.Operand2(BasisWorkDurationId)
					.Operand3(NetWorkDurationId, (TimeSpan thisDuration, TimeSpan? basisDuration) => thisDuration + (basisDuration ?? TimeSpan.Zero)),
				new Check3<TimeSpan, TimeSpan?, TimeSpan>()
					.Operand1(LocalGenerateLeadTimeId)
					.Operand2(BasisGenerateLeadTimeId)
					.Operand3(NetGenerateLeadTimeId, (TimeSpan thisDuration, TimeSpan? basisDuration) => (!basisDuration.HasValue || thisDuration > basisDuration) ? thisDuration : basisDuration.Value )
			});

			WorkOrderTemplateEditTbl = new DelayedCreateTbl(delegate () {
				var allTabs = new List<TblTabNode>();
				allTabs.Add(DetailsTabNode.New(detailsNodes.ToArray()));
				allTabs.AddRange(browsetteTabs);
				return new Tbl(dsMB.Schema.T.WorkOrderTemplate,
					TId.Task,
					new Tbl.IAttr[] {
						SchedulingGroup,
						new ETbl(),
						TIReports.NewRemotePTbl(TIReports.WorkOrderTemplateReport)
					},
					(TblLayoutNodeArray)new TblLayoutNodeArray(allTabs.ToArray()).Clone(),
					new List<TblActionNode>(commonActions)
				);
			});
			RegisterForImportExport(TId.Task, WorkOrderTemplateEditTbl);
			// This Tbl is used in New mode only from the Browser when you select New Sub Task
			// It has an additonal set of Inits that set all fields subject to accumulation to the "no-op" values.
			WorkOrderTemplateSpecializationEditTbl = new DelayedCreateTbl(delegate () {
				var allTabs = new List<TblTabNode>();
				allTabs.Add(DetailsTabNode.New(detailsNodes.ToArray()));
				allTabs.AddRange(browsetteTabs);
				var actions = new List<TblActionNode>(commonActions);
				foreach (DBI_Column c in dsMB.Schema.T.WorkOrderTemplate.Columns) {
					// Skip explicitly the fields not used in Accumulator in MakeWorkOrder method.
					if (c == dsMB.Schema.T.WorkOrderTemplate.InternalIdColumn)
						continue;
					if (c == dsMB.Schema.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID)
						continue;
					if (c == dsMB.Schema.T.WorkOrderTemplate.F.Code)
						continue;
					if (!c.IsWriteable) // calculated fields are not subject to Accumulators either
						continue;

					//TODO: The following should somehow being using the knowledge in the Accumulator methods.
					// The MakeWorkOrder generator contains a set of Accumulators to produce fields in the work order and a method that given a workorder
					// template produces a workorder.
					// Another method should exist that given a template record fills in the source fields used by the Accumulators
					// with 'do nothing' values that do not affect the accumulated result.
					if (c == dsMB.Schema.T.WorkOrderTemplate.F.Duration || c == dsMB.Schema.T.WorkOrderTemplate.F.GenerateLeadTime)
						// This field is a time span and is not nullable so we put zero in.
						actions.Add(Init.OnLoadNew(new PathTarget(new DBI_Path(c)), new ConstantValue(TimeSpan.Zero)));
					else
						// All other fields just get set to null.
						actions.Add(Init.OnLoadNew(new PathTarget(new DBI_Path(c)), new ConstantValue(null)));
				}
				return new Tbl(dsMB.Schema.T.WorkOrderTemplate,
					TId.TaskSpecialization,
					new Tbl.IAttr[] {
						SchedulingGroup,
						new ETbl()
					},
					(TblLayoutNodeArray)new TblLayoutNodeArray(allTabs.ToArray()).Clone(),
					actions
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemplate, delegate () {
				return TaskBrowserTbl(true, includeDefaultViews: true);
			});
			#region WorkOrder Template From WorkOrder EditTbl
			WorkOrderTemplateFromWorkOrderEditTbl = new DelayedCreateTbl(delegate () {
				var customDetailsNodes = new List<TblLayoutNode>(detailsNodes);
				// Insert a tagged WO picker at the start of the Details node.
				// TODO: Want this to be readonly when called from the WO browser but pickable when called from the Task browser. Because the WO browser does not use a PathOrFilterTarget the init does not force
				// the picker readonly. Of course, it can't use that type of init because there is no bound path. Perhaps someday having the calling browser force a edit value readonly would be an independent attribute passed
				// to the editor.
				// For now we just make it readonly and do not allow calls from the Task browser.
				// TODO: To ensure there is a WO selection the WO browsers use NoContextFreeInit, which means the Task from WO editor cannot have a Save & New operation.
				// TODO: Because the wo picker does not notify until idle time of changes within the selected record, the editor comes up in "new/changed" mode so you must Cancel before you can Next/Previous.
				customDetailsNodes.Insert(0, TblUnboundControlNode.New(KB.K("from Work Order"), dsMB.Schema.T.Demand.F.WorkOrderID.EffectiveType, Fmt.SetId(SourceWorkOrderPickerId), ECol.AllReadonly));
				var allTabs = new List<TblTabNode>();
				allTabs.Add(DetailsTabNode.New(customDetailsNodes.ToArray()));
				allTabs.AddRange(browsetteTabs);
				// Add actions that init all the WO Template fields from the WO in the picker. These all use InSubBrowserValue
				var actions = new List<TblActionNode>(commonActions);
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.AccessCodeID, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.AccessCodeID))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.CloseCodeID, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.CloseCodeID))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.ClosingComment, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.ClosingComment))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.Description, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.Description))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.Downtime, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.Downtime))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.Duration, new InSubBrowserValue(SourceWorkOrderPickerId,
					new BrowserCalculatedInitValue(dsMB.Schema.T.WorkOrderTemplate.F.Duration.EffectiveType,
						(object[] inputs) => (inputs[0] == null || inputs[1] == null) ? null : (object)((DateTime)inputs[1] - (DateTime)inputs[0] + Extensions.TimeSpan.OneDay),
						new BrowserPathValue(dsMB.Path.T.WorkOrder.F.StartDateEstimate),
						new BrowserPathValue(dsMB.Path.T.WorkOrder.F.EndDateEstimate)))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.ProjectID, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.ProjectID))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.SelectPrintFlag, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.SelectPrintFlag))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.Subject, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.Subject))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.WorkCategoryID, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.WorkCategoryID))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.WorkOrderExpenseModelID, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.WorkOrderExpenseModelID))));
				actions.Add(Init.ContinuousNewClone(dsMB.Path.T.WorkOrderTemplate.F.WorkOrderPriorityID, new InSubBrowserValue(SourceWorkOrderPickerId, new BrowserPathValue(dsMB.Path.T.WorkOrder.F.WorkOrderPriorityID))));
				// We have a custom EditLogic which clones the child records (resources, temp storage and assignments, maybe linked PO's) as part of the Save cycle in New mode.
				return new Tbl(dsMB.Schema.T.WorkOrderTemplate, TId.TaskFromWorkOrder,
					new Tbl.IAttr[] {
						SchedulingGroup,
						new ETbl(
							ETbl.LogicClass(typeof(TemplateFromWorkOrderEditLogic)),
							ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View),
							ETbl.CreateCustomDataSet(true),
							ETbl.SetMultiSelectNewModeBehaviour(ETbl.MultiSelectNewModeBehaviours.Single)	// Disallow multiple selection to avoid checkpoint/rollback complications on child records during save cycle
						),
						TIReports.NewRemotePTbl(TIReports.WorkOrderTemplateReport)
					},
					new TblLayoutNodeArray(allTabs.ToArray()),
					actions
				);
			});
			#endregion
			#endregion
			#region WorkOrderTemplatePurchaseOrderTemplate
			DefineTbl(dsMB.Schema.T.WorkOrderTemplatePurchaseOrderTemplate, delegate () {
				return new Tbl(dsMB.Schema.T.WorkOrderTemplatePurchaseOrderTemplate, TId.TaskToPurchaseOrderTemplateLinkage,
				new Tbl.IAttr[] {
					SchedulingAndPurchasingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.WorkOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.WorkOrderTemplateID.F.Subject),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.PurchaseOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.PurchaseOrderTemplateID.F.Subject)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					TblFixedRecordTypeNode.New(),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.WorkOrderTemplateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderTemplate.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.WorkOrderTemplateID.F.Subject, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.PurchaseOrderTemplateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrderTemplate.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplatePurchaseOrderTemplate.F.PurchaseOrderTemplateID.F.Subject, DCol.Normal)
				));
			});
			#endregion
			#region WorkOrderTemplateItems
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemplateItems,
				delegate () {
					object quantityColumnId = KB.I("WorkOrderTemplateResourceDemandQuantityId");
					var editStorageAssignmentVerb = KB.K("Edit Storage Assignment");
					var viewStorageAssignmentVerb = KB.K("View Storage Assignment");

					object codeColumnId = KB.I("WorkOrderTemplateItemCodeId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplateItems, TId.TaskItem,
						new Tbl.IAttr[] {
							SchedulingAndItemResourcesGroup,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(DemandedColumnKey, quantityColumnId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemplateItems.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID.F.ItemID,
								dsMB.Schema.T.WorkOrderTemplateItemsTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderTemplateItems.F.TableEnum,
						new CompositeView(dsMB.Path.T.WorkOrderTemplateItems.F.ItemID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Item.F.Code),
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.ForceNotPrimary(),
							ReadonlyView),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand
						// a resource not already mentioned in this WO's Resource view. We do this init when the current record is either an
						// appropriate resource, or a demand of the same type.
						// We separate demands on temp and perm storage so that the Edit Storage Assignment extra verb can work.
						new CompositeView(dsMB.Path.T.WorkOrderTemplateItems.F.DemandTemplateID.F.DemandItemTemplateID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.LocationID.F.Code),
							BTbl.PerViewColumnValue(quantityColumnId, dsMB.Path.T.DemandItemTemplate.F.Quantity),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateItems.DemandItemTemplate,
								new CompositeView.Init(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID, dsMB.Path.T.WorkOrderTemplateItems.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID)
							),
							CompositeView.RecognizeByValidEditLinkage()
						)
					);
				}
			);
			#endregion
			#region WorkOrderTemplateInside
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemplateInside,
				delegate () {
					DelayedCreateTbl resourceGroupTbl = new DelayedCreateTbl(
						delegate () {
							return new Tbl(dsMB.Schema.T.WorkOrderTemplateInside, TId.ResourceGroup,
								new Tbl.IAttr[] {
									SchedulingAndLaborResourcesGroup
								},
								new TblLayoutNodeArray(
									TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateInside.F.Id, DCol.Normal)
								)
							);
						}
					);
					object codeColumnId = KB.I("WorkOrderTemplateInsideCodeId");
					object demandQuantityColumnId = KB.I("WorkOrderLaborOutsideDemandQuantityId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplateInside, TId.TaskInside,
						new Tbl.IAttr[] {
							SchedulingAndLaborResourcesGroup,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(DemandedColumnKey, demandQuantityColumnId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemplateInside.F.ParentID,
								dsMB.Schema.T.WorkOrderTemplateInsideTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderTemplateInside.F.TableEnum,
						new CompositeView(resourceGroupTbl, dsMB.Path.T.WorkOrderTemplateInside.F.Id, ReadonlyView, CompositeView.ForceNotPrimary(), CompositeView.IdentificationOverride(TId.Trade),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.WorkOrderTemplateInside.F.Id)),
						new CompositeView(dsMB.Path.T.WorkOrderTemplateInside.F.TradeID,
							ReadonlyView,
							CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Trade.F.Code)
						),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand a resource not
						// already mentioned in this WO's Resource view. We do this init when the current record is either an appropriate resource, or a demand of the
						// same type.
						new CompositeView(dsMB.Path.T.WorkOrderTemplateInside.F.DemandTemplateID.F.DemandLaborInsideTemplateID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandLaborInsideTemplate.F.Quantity, IntervalFormat),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateInside.DemandLaborInsideTemplate,
								new CompositeView.Init(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID, dsMB.Path.T.WorkOrderTemplateInside.F.DemandTemplateID.F.DemandLaborInsideTemplateID.F.LaborInsideID)
							)
						),
						new CompositeView(dsMB.Path.T.WorkOrderTemplateInside.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandOtherWorkInsideTemplate.F.Quantity, IntegralFormat),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateInside.DemandOtherWorkInsideTemplate,
								new CompositeView.Init(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID, dsMB.Path.T.WorkOrderTemplateInside.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID)
							)
						)
					);
				}
			);
			#endregion
			#region WorkOrderTemplateOutside
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemplateOutside,
				delegate () {
					DelayedCreateTbl resourceGroupTbl = new DelayedCreateTbl(
						delegate () {
							return new Tbl(dsMB.Schema.T.WorkOrderTemplateOutside, TId.ResourceGroup,
								new Tbl.IAttr[] {
									SchedulingAndLaborResourcesGroup
								},
								new TblLayoutNodeArray(
									TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateOutside.F.Id, DCol.Normal)
								)
							);
						}
					);
					object codeColumnId = KB.I("WorkOrderTemplateOutsideCodeId");
					object demandQuantityColumnId = KB.I("WorkOrderLaborOutsideDemandQuantityId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplateOutside, TId.TaskOutside,
						new Tbl.IAttr[] {
							SchedulingAndLaborResourcesGroup,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeColumnId),
								BTbl.PerViewListColumn(DemandedColumnKey, demandQuantityColumnId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemplateOutside.F.ParentID,
								dsMB.Schema.T.WorkOrderTemplateOutsideTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderTemplateOutside.F.TableEnum,
						new CompositeView(resourceGroupTbl, dsMB.Path.T.WorkOrderTemplateOutside.F.Id, ReadonlyView, CompositeView.ForceNotPrimary(), CompositeView.IdentificationOverride(TId.Trade),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.WorkOrderTemplateOutside.F.Id)),
						new CompositeView(dsMB.Path.T.WorkOrderTemplateOutside.F.TradeID,
							ReadonlyView,
							CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Trade.F.Code)
						),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand a resource not
						// already mentioned in this WO's Resource view. We do this init when the current record is either an appropriate resource, or a demand of the
						// same type.
						new CompositeView(dsMB.Path.T.WorkOrderTemplateOutside.F.DemandTemplateID.F.DemandLaborOutsideTemplateID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandLaborOutsideTemplate.F.Quantity, IntervalFormat),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateOutside.DemandLaborOutsideTemplate,
								new CompositeView.Init(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID, dsMB.Path.T.WorkOrderTemplateOutside.F.DemandTemplateID.F.DemandLaborOutsideTemplateID.F.LaborOutsideID)
							)
						),
						new CompositeView(dsMB.Path.T.WorkOrderTemplateOutside.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID,
							BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.Code),
							BTbl.PerViewColumnValue(demandQuantityColumnId, dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.Quantity, IntegralFormat),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateOutside.DemandOtherWorkOutsideTemplate,
								new CompositeView.Init(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID, dsMB.Path.T.WorkOrderTemplateOutside.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID)
							)
						)
					);
				}
			);
			#endregion
			#region WorkOrderTemplateMiscellaneous
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemplateMiscellaneous,
				delegate () {
					object miscellaneousCodeColumnId = KB.I("WorkOrderTemplateMiscellaneousCodeColumnId");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplateMiscellaneous, TId.TaskMiscellaneousExpense,
						new Tbl.IAttr[] {
							SchedulingAndItemResourcesGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, miscellaneousCodeColumnId),
								BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.MiscellaneousWorkOrderCostID.F.Cost)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.ParentID, dsMB.Schema.T.WorkOrderTemplateMiscellaneousTreeView, 2, 2)
						},
						dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.TableEnum,
						new CompositeView(dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.MiscellaneousWorkOrderCostID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(miscellaneousCodeColumnId, dsMB.Path.T.MiscellaneousWorkOrderCost.F.Code)
						),
						// We allow Demands to have a context-free New and only advisory initing of the resource because the user may want to demand a resource not
						// already mentioned in this WO's Resource view. We do this init when the current record is either an appropriate resource, or a demand of the
						// same type.
						new CompositeView(dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.DemandTemplateID.F.DemandMiscellaneousWorkOrderCostTemplateID,
							BTbl.PerViewColumnValue(miscellaneousCodeColumnId, dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.WorkOrderTemplateID, dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.DemandTemplateID.F.WorkOrderTemplateID),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateMiscellaneous.MiscellaneousWorkOrderCost,
								new CompositeView.Init(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID, dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.MiscellaneousWorkOrderCostID)
							),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateMiscellaneous.DemandMiscellaneousWorkOrderCostTemplate,
								new CompositeView.Init(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID, dsMB.Path.T.WorkOrderTemplateMiscellaneous.F.DemandTemplateID.F.DemandMiscellaneousWorkOrderCostTemplateID.F.MiscellaneousWorkOrderCostID)
							)
						)
					);
				}
			);
			#endregion
			#region WorkOrderTemplateStorage
			DefineBrowseTbl(dsMB.Schema.T.WorkOrderTemplateStorage,
				delegate () {
					object codeId = KB.I("Task temp storage Code column id");
					return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplateStorage, TId.TaskTemporaryStorage,
						new Tbl.IAttr[] {
							SchedulingAndItemResourcesGroup,
							new BTbl(
								BTbl.PerViewListColumn(CommonCodeColumnKey, codeId)
							),
							new FilteredTreeStructuredTbl(dsMB.Path.T.WorkOrderTemplateStorage.F.ParentID, dsMB.Schema.T.WorkOrderTemplateStorageTreeView, 4, 4)
						},
						dsMB.Path.T.WorkOrderTemplateStorage.F.TableEnum,
						// Postal address
						new CompositeView(dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID.F.PostalAddressID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.PostalAddress.F.Code)),
						// Temporary Storage (impossible in this view)
						null,
						// Unit
						new CompositeView(dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID.F.RelativeLocationID.F.UnitID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.Unit.F.RelativeLocationID.F.Code)),
						// Permanent Storage
						new CompositeView(dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID.F.RelativeLocationID.F.PermanentStorageID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code)),
						// Plain Relative Location
						new CompositeView(dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID.F.RelativeLocationID.F.PlainRelativeLocationID, ReadonlyView, CompositeView.ForceNotPrimary(),
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code)),
						// Template Temporary Storage 
						new CompositeView(dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID.F.TemplateTemporaryStorageID,
							// not sure what to show in the list column
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemplateStorage.F.WorkOrderTemplateID, dsMB.Path.T.TemplateTemporaryStorage.F.WorkOrderTemplateID),
							CompositeView.ContextualInit(
								new int[] {
									(int)ViewRecordTypes.WorkOrderTemplateStorage.PermanentStorage,
									(int)ViewRecordTypes.WorkOrderTemplateStorage.PlainRelativeLocation,
									(int)ViewRecordTypes.WorkOrderTemplateStorage.Unit,
									(int)ViewRecordTypes.WorkOrderTemplateStorage.PostalAddress
								},
								new CompositeView.Init(dsMB.Path.T.TemplateTemporaryStorage.F.ContainingLocationID, dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID)
							),
							CompositeView.ContextualInit(
								new int[] {
									(int)ViewRecordTypes.WorkOrderTemplateStorage.TemplateItemLocation,
								},
								new CompositeView.Init(dsMB.Path.T.TemplateTemporaryStorage.F.ContainingLocationID, dsMB.Path.T.WorkOrderTemplateStorage.F.ItemLocationID.F.LocationID)
							)
						),
						// Template Storage Assignment
						new CompositeView(dsMB.Path.T.WorkOrderTemplateStorage.F.ItemLocationID.F.TemplateItemLocationID,
							BTbl.PerViewColumnValue(codeId, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.WorkOrderTemplateStorage.F.WorkOrderTemplateID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID.F.TemplateTemporaryStorageID.F.WorkOrderTemplateID),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateStorage.TemplateItemLocation,
								new CompositeView.Init(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID, dsMB.Path.T.WorkOrderTemplateStorage.F.ItemLocationID.F.LocationID)
							),
							CompositeView.ContextualInit(
								(int)ViewRecordTypes.WorkOrderTemplateStorage.TemplateTemporaryStorage,
								new CompositeView.Init(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID, dsMB.Path.T.WorkOrderTemplateStorage.F.LocationID)
							)
						)
					);
				}
			);
			#endregion
		}
	}
}
