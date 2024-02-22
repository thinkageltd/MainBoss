using System;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Database;
namespace Thinkage.MainBoss.Controls {
	public class ControlPanelLayout {
		/// <summary>
		/// Placeholder to record highest id number allocated in this file. If you add any more just change this to reflect the highest one.
		/// </summary>
#if DEBUG
		const int HighestIdNumberUsedInThisFile = 288;
		static readonly Thinkage.Libraries.Collections.Set<int> DuplicateCatcher = new Libraries.Collections.Set<int>();
#endif
		/// <summary>
		/// Make a Guid string from an int
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		private static string Id(int n) {
#if DEBUG
			System.Diagnostics.Debug.Assert(n <= HighestIdNumberUsedInThisFile, "You forgot to reset HighestIdNumberUsedInThisFile");
			System.Diagnostics.Debug.Assert(!DuplicateCatcher.Contains(n), "A duplicate Id number has been used");
			DuplicateCatcher.Add(n);
#endif
			return Libraries.Strings.IFormat("4D61696E-426F-7373-F00D-{0:X12}", n);
		}
		public ControlPanelLayout() {
		}
		#region Browser Support
		/// <summary>
		/// Create a menu item for browsing of a table and keep track of the BrowseExplorers used in case they are referenced from more than one node
		/// </summary>
		/// <param name="name">The menu item name</param>
		/// <param name="table">The table to browse</param>
		/// <param name="sub">The submenu items, if any</param>
		/// <returns>The new MenuDef</returns>
		private static MenuDef BrowseExplorer(int id, [Context("ControlPanel", Level = 1)] string name, DBI_Table table, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), ControlPanelContext.K(name), TIGeneralMB3.FindDelayedBrowseTbl(table), sub);
		}
		private static MenuDef BrowseExplorer(int id, [Context("ControlPanel", Level = 1)] string name, DelayedCreateTbl table, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), ControlPanelContext.K(name), table, sub);
		}
		private static MenuDef BrowseExplorer(int id, Tbl.TblIdentification name, DBI_Table table, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), KB.TOControlPanel(name), TIGeneralMB3.FindDelayedBrowseTbl(table), sub);
		}
		private static MenuDef BrowseExplorer(int id, Tbl.TblIdentification name, DelayedCreateTbl delayedTbl, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), KB.TOControlPanel(name), delayedTbl, sub);
		}
		#endregion
		#region Report Support
		/// <summary>
		/// Menu item for printing a table
		/// </summary>
		/// <param name="name"></param>
		/// <param name="table"></param>
		/// <param name="rid"></param>
		/// <param name="sub"></param>
		/// <returns></returns>
		private static MenuDef ReportExplorer(int id, [Context("ControlPanel", Level = 1)] string name, Tbl tableInfo, Key userTip, params MenuDef[] sub) {
			return new ReportMenuDef(Id(id), ControlPanelContext.K(name), tableInfo, userTip, null, sub);
		}

		private static MenuDef ReportExplorer(int id, Tbl.TblIdentification name, Tbl tableInfo, Key userTip, params MenuDef[] sub) {
			return new ReportMenuDef(Id(id), KB.TOControlPanel(name), tableInfo, userTip, null, sub);
		}
		#endregion
		#region General Support
		public static MenuDef ContainerMenuItem(int id, [Context("ControlPanel", Level = 1)] string name, params MenuDef[] sub) {
			return new MenuDef(Id(id), ControlPanelContext.K(name), sub);
		}
		public static MenuDef ContainerMenuItem(int id, [Context("ControlPanel", Level = 1)] string name, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new MenuDef(Id(id), ControlPanelContext.K(name), featureGroup, sub);
		}
		public static MenuDef ContainerMenuItem(int id, Tbl.TblIdentification name, params MenuDef[] sub) {
			return new MenuDef(Id(id), KB.TOControlPanel(name), sub);
		}
		public static MenuDef ContainerMenuItem(int id, Tbl.TblIdentification name, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new MenuDef(Id(id), KB.TOControlPanel(name), featureGroup, sub);
		}
		#endregion
		#region Data-related menu groups
		private static MenuDef AssignmentNode => BrowseExplorer(1, TId.MyAssignmentOverview, TIGeneralMB3.AssignmentStatusTblCreator
				, BrowseExplorer(2, TId.InProgressRequest, TIRequest.RequestInProgressAssignedToBrowseTbl
					, BrowseExplorer(3, TId.UnassignedRequest, TIRequest.UnassignedRequestBrowseTbl))
				, BrowseExplorer(4, TId.OpenWorkOrder, TIWorkOrder.WorkOrderInProgressAssignedToBrowseTbl
					, BrowseExplorer(5, TId.UnassignedWorkOrder, TIWorkOrder.UnassignedWorkOrderBrowseTbl))
				, BrowseExplorer(6, TId.IssuedPurchaseOrder, TIPurchaseOrder.PurchaseOrderInProgressAssignedToBrowseTbl
					, BrowseExplorer(7, TId.UnassignedPurchaseOrder, TIPurchaseOrder.UnassignedPurchaseOrderBrowseTbl))
				);

		private static MenuDef RequestNode => BrowseExplorer(8, TId.Request, dsMB.Schema.T.Request
				, BrowseExplorer(9, TId.NewRequest, TIRequest.RequestNewBrowseTbl)
				, BrowseExplorer(10, TId.InProgressRequest, TIRequest.RequestInProgressBrowseTbl
					, BrowseExplorer(11, TId.InProgressRequestWithNoLinkedWorkOrder, TIRequest.RequestInProgressWithoutWOBrowseTbl)
					, BrowseExplorer(12, TId.InProgressRequestWithLinkedWorkOrder, TIRequest.RequestInProgressWithWOBrowseTbl)
				)
				, BrowseExplorer(13, TId.ClosedRequest, TIRequest.RequestClosedBrowseTbl)
				, ReportExplorer(14, "Print Requests", TIReports.RequestFormReport, KB.K("Print Request forms for distribution"))
				, BrowseExplorer(15, TId.Requestor, TIRequest.RequestorForRequestsTblCreator)
				, BrowseExplorer(16, TId.RequestAssignment, TIRequest.RequestAssignmentByAssigneeTblCreator)
				, ContainerMenuItem(17, "Reports"
					, ReportExplorer(18, TId.Request.ReportByAssignee, TIReports.RequestByAssigneeFormReport, KB.K("Print Request forms for distribution to assignees"))
					, ReportExplorer(19, TId.Request.ReportSummary, TIReports.RequestSummary, KB.K("Summary with one line per request"))
					, ReportExplorer(20, TId.Request.ReportSummary.ReportByAssignee, TIReports.RequestByAssigneeSummary, KB.K("Summary by assignee with one line per request"))
					, ReportExplorer(21, TId.Request.ReportHistory, TIReports.RequestHistory, KB.K("Detailed info on selected requests"))
					, ReportExplorer(22, TId.Request.ReportHistory.ReportByAssignee, TIReports.RequestHistoryByAssignee, KB.K("Detailed info on selected requests by assignees"))
					, ReportExplorer(23, TId.RequestStateHistory, TIReports.RequestStateHistory, KB.K("Report on Requests with State History"))
					, ReportExplorer(24, TId.RequestStateHistory.ReportSummary, TIReports.RequestStateHistorySummary, KB.K("Summary of information from State History section of requests, one line per State History entry"))
					)
				, ContainerMenuItem(25, "Charts"
					, ReportExplorer(26, TId.RequestChartCountByCreatedDate, TIReports.RequestChartCountByCreatedDate, KB.K("Bar chart showing when Requests were created"))
					, ReportExplorer(27, TId.RequestChartCountByInProgressDate, TIReports.RequestChartCountByInProgressDate, KB.K("Bar chart showing when Requests were first made In Progress"))
					, ReportExplorer(28, TId.RequestChartCountByEndedDate, TIReports.RequestChartCountByEndedDate, KB.K("Bar chart showing when Requests ended"))
					, ReportExplorer(29, TId.RequestChartCount, TIReports.RequestChartCount, KB.K("Bar chart showing number of Requests per group"))
					, ReportExplorer(30, TId.RequestChartAverageDuration, TIReports.RequestChartAverageDuration, KB.K("Bar chart showing average Request duration"))
					, ReportExplorer(31, TId.RequestChartLifetime, TIReports.RequestChartLifetime, KB.K("Bar chart showing Request duration based on when the request started"))
					, ReportExplorer(32, TId.RequestChartStatus, TIReports.RequestChartStatus, KB.K("Bar chart showing average time in each Request Status"))
				)
				);
		private static MenuDef WoNode => BrowseExplorer(33, TId.WorkOrder, TIWorkOrder.WorkOrderAllBrowseTbl
				, BrowseExplorer(34, TId.OverdueWorkOrder, TIWorkOrder.WorkOrderOverdueBrowseTbl)
				, BrowseExplorer(35, TId.DraftWorkOrder, TIWorkOrder.WorkOrderDraftBrowseTbl)
				, BrowseExplorer(36, TId.OpenWorkOrder, TIWorkOrder.WorkOrderOpenBrowseTbl)
				, BrowseExplorer(37, TId.ClosedWorkOrder, TIWorkOrder.WorkOrderClosedBrowseTbl)
				, BrowseExplorer(38, TId.VoidWorkOrder, TIWorkOrder.WorkOrderVoidBrowseTbl)
				, ReportExplorer(39, "Print Work Orders", TIReports.WorkOrderFormReport, KB.K("Print Work Order forms"))
				, BrowseExplorer(40, TId.Requestor, TIRequest.RequestorForWorkOrdersTblCreator)
				, BrowseExplorer(41, TId.WorkOrderAssignment, TIWorkOrder.WorkOrderAssignmentByAssigneeTblCreator)
				, BrowseExplorer(42, TId.BillableRequestor, dsMB.Schema.T.BillableRequestor)
				, BrowseExplorer(43, TId.Chargeback, TIWorkOrder.AllChargebackTbl)
				, ContainerMenuItem(44, "Reports"
					, ReportExplorer(45, TId.WorkOrder.ReportByAssignee, TIReports.WorkOrderByAssigneeFormReport, KB.K("Print Work Order forms with multiple copies as needed for distribution to assignees"))
					, ReportExplorer(46, TId.WorkOrder.ReportHistory, TIReports.WOHistory, KB.K("Report on Work Orders with Resources"))
					, ReportExplorer(47, TId.WorkOrder.ReportHistory.ReportByAssignee, TIReports.WOHistoryByAssignee, KB.K("Report on Work Orders by assignee with Resources"))
					, ReportExplorer(48, TId.WorkOrder.ReportSummary, TIReports.WOSummary, KB.K("Report on Work Orders without Resources or State History"))
					, ReportExplorer(49, TId.WorkOrder.ReportSummary.ReportByAssignee, TIReports.WOSummaryByAssignee, KB.K("Report on Work Orders by assignee without Resources or State History"))
					, ReportExplorer(50, TId.WorkOrderStateHistory, TIReports.WOStateHistory, KB.K("Report on Work Orders with State History"))
					, ReportExplorer(51, TId.WorkOrderStateHistory.ReportSummary, TIReports.WOStateHistorySummary, KB.K("Summary of information from State History section of work orders, one line per State History entry"))
					, ReportExplorer(52, TId.Chargeback.ReportHistory, TIReports.ChargebackHistoryReport, KB.K("Report on Chargebacks with line items"))
					, ReportExplorer(53, TId.Chargeback.ReportSummary, TIReports.ChargebackSummaryReport, KB.K("Report on Chargebacks without line items"))
					, ReportExplorer(54, TId.ChargebackActivity, TIReports.ChargebackLineReport, KB.K("Report on individual Chargeback line items"))
					, ReportExplorer(55, TId.TemporaryStorage, TIReports.TemporaryStorageReport, KB.K("Report on Temporary Storage usage"))
					, ReportExplorer(56, TId.WorkOrderResourceDemand, TIReports.WOResourceDemand, KB.K("Work Order Resources with demanded and actual quantity and cost")
						, ReportExplorer(57, TId.WorkOrderItem, TIReports.WODemandItem, KB.K("Work Order Items with demanded and actual quantity and cost"))
						, ReportExplorer(58, TId.WorkOrderHourlyInside, TIReports.WODemandHourlyInside, KB.K("Work Order Hourly Inside with demanded and actual time and cost"))
						, ReportExplorer(59, TId.WorkOrderPerJobInside, TIReports.WODemandPerJobInside, KB.K("Work Order Per Job Inside with demanded and actual quantity and cost"))
						, ReportExplorer(60, TId.WorkOrderHourlyOutside, TIReports.WODemandHourlyOutside, KB.K("Work Order Hourly Outside with demanded and actual time and cost"))
						, ReportExplorer(61, TId.WorkOrderPerJobOutside, TIReports.WODemandPerJobOutside, KB.K("Work Order Per Job Outside with demanded and actual quantity and cost"))
						, ReportExplorer(62, TId.WorkOrderMiscellaneousExpense, TIReports.WODemandMiscellaneousWorkOrderCost, KB.K("Work Order Miscellaneous with demanded and actual cost"))
						, ReportExplorer(63, TId.ReceiptActivity, TIReports.ResourceReceiving, KB.K("Received items and services"))
					)
					, ReportExplorer(64, TId.WorkOrderResourceActual, TIReports.WOResourceActual, KB.K("Work Order Resources actually used")
						, ReportExplorer(65, TId.WorkOrderItemActual, TIReports.WOActualItem, KB.K("Work Order Items actually used"))
						, ReportExplorer(66, TId.WorkOrderHourlyInsideActual, TIReports.WOActualHourlyInside, KB.K("Work Order Hourly Inside actually used"))
						, ReportExplorer(67, TId.WorkOrderPerJobInsideActual, TIReports.WOActualPerJobInside, KB.K("Work Order Per Job Inside actually used"))
						, ReportExplorer(68, TId.WorkOrderHourlyOutsidePOActual, TIReports.WOActualHourlyOutsidePO, KB.K("Work Order Hourly Outside actually used (with PO)"))
						, ReportExplorer(69, TId.WorkOrderPerJobOutsidePOActual, TIReports.WOActualPerJobOutsidePO, KB.K("Work Order Per Job Outside actually used (with PO)"))
						, ReportExplorer(70, TId.WorkOrderHourlyOutsideNonPOActual, TIReports.WOActualHourlyOutsideNonPO, KB.K("Work Order Hourly Outside actually used (no PO)"))
						, ReportExplorer(71, TId.WorkOrderPerJobOutsideNonPOActual, TIReports.WOActualPerJobOutsideNonPO, KB.K("Work Order Per Job Outside actually used (no PO)"))
						, ReportExplorer(72, TId.WorkOrderMiscellaneousExpenseActual, TIReports.WOActualMiscellaneousWorkOrderCost, KB.K("Work Order Miscellaneous actually incurred"))
					)
				)
				, ContainerMenuItem(73, "Charts"
					, ReportExplorer(74, TId.WOChartCountByCreatedDate, TIReports.WOChartCountByCreatedDate, KB.K("Bar chart showing when work orders were created"))
					, ReportExplorer(75, TId.WOChartCountByOpenedDate, TIReports.WOChartCountByOpenedDate, KB.K("Bar chart showing when Work Orders were first Opened"))
					, ReportExplorer(76, TId.WOChartCountByEndedDate, TIReports.WOChartCountByEndedDate, KB.K("Bar chart showing when Work Orders ended"))
					, ReportExplorer(77, TId.WOChartCount, TIReports.WOChartCount, KB.K("Bar chart showing number of Work Orders per group"))
					, ReportExplorer(78, TId.WOChartAverageDuration, TIReports.WOChartAverageDuration, KB.K("Bar chart showing average Work Order duration"))
					, ReportExplorer(79, TId.WOChartTotalDuration, TIReports.WOChartTotalDuration, KB.K("Bar chart showing total Work Order duration"))
					, ReportExplorer(80, TId.WOChartAverageDowntime, TIReports.WOChartAverageDowntime, KB.K("Bar chart showing average downtime per group"))
					, ReportExplorer(81, TId.WOChartTotalDowntime, TIReports.WOChartTotalDowntime, KB.K("Bar chart showing total downtime per group"))
					, ReportExplorer(287, TId.WOChartAverageSelectedDuration, TIReports.WOChartAverageSelectedDuration, KB.K("Bar chart showing average selected Work Order duration"))
					, ReportExplorer(82, TId.WOChartLifetime, TIReports.WOChartLifetime, KB.K("Range chart showing work order duration based on when the work order started"))
					, ReportExplorer(83, TId.WOChartStatus, TIReports.WOChartStatus, KB.K("Bar chart showing average time in each Work Order Status"))
					, ContainerMenuItem(84, TId.WorkOrderResourceActual
						, ReportExplorer(85, TId.WOChartCostsByResourceType, TIReports.WOChartCostsByResourceType, KB.K("Bar chart showing costs by resource type"))
						, ReportExplorer(86, TId.WOChartCostsByTrade, TIReports.WOChartCostsByTrade, KB.K("Bar chart showing labor costs broken down by trade"))
						, ReportExplorer(87, TId.WOChartCostsByEmployee, TIReports.WOChartCostsByEmployee, KB.K("Bar chart showing inside labor costs broken down by employee"))
						, ReportExplorer(88, TId.WOChartCostsByVendor, TIReports.WOChartCostsByVendor, KB.K("Bar chart showing outside labor costs broken down by vendor/contractor"))
						, ReportExplorer(89, TId.WOChartCosts, TIReports.WOChartCosts, KB.K("Bar chart showing costs broken down by groups in Grouping section"))
						, ReportExplorer(90, TId.WOChartLaborTimeByTrade, TIReports.WOChartHoursByTrade, KB.K("Bar chart showing labor time broken down by trade"))
						, ReportExplorer(91, TId.WOChartLaborTimeByEmployee, TIReports.WOChartHoursByEmployee, KB.K("Bar chart showing inside labor time broken down by employee"))
						, ReportExplorer(92, TId.WOChartLaborTimeByVendor, TIReports.WOChartHoursByVendor, KB.K("Bar chart showing outside labor time broken down by vendor/contractor"))
						, ReportExplorer(93, TId.WOChartLaborTime, TIReports.WOChartHours, KB.K("Bar chart showing labor time broken down by groups in Grouping section"))
					)
				)
			);
		private static MenuDef UnitMaintenancePlanNode => BrowseExplorer(94, TId.UnitMaintenancePlan, TISchedule.ScheduledWorkOrderBrowserTbl
					, BrowseExplorer(95, "Generate Planned Maintenance", dsMB.Schema.T.PMGenerationBatch)
					, BrowseExplorer(96, TId.Task, dsMB.Schema.T.WorkOrderTemplate)
					, ContainerMenuItem(97, "Reports"
						, ReportExplorer(98, TId.UnitMaintenancePlan.ReportSummary, TIReports.ScheduledWorkOrderSummary, KB.K("One line per selected unit maintenance plan"))
						, ReportExplorer(99, TId.Task.ReportSummary, TIReports.WorkOrderTemplateSummary, KB.K("One line per selected task"))
						, ReportExplorer(100, TId.LaborForecast, TIReports.LaborForecast, KB.K("Prediction of future labor requirements"))
						, ReportExplorer(101, TId.MaterialForecast, TIReports.MaterialForecast, KB.K("Prediction of future item requirements"))
						, ReportExplorer(102, TId.MaintenanceForecast, TIReports.MaintenanceForecast, KB.K("Prediction of future work orders"))
						, ReportExplorer(103, TId.MaintenanceTiming, TIReports.MaintenanceTimings, KB.K("List maintenance timing records"))
						, ReportExplorer(104, TId.TaskResource, TIReports.WOTemplateResource, KB.K("Task items, labor, and miscellaneous expenses")
							, ReportExplorer(105, TId.Item, TIReports.WOTemplateItem, KB.K("Task demands and actuals"))
							, ReportExplorer(106, TId.HourlyInside, TIReports.WOTemplateHourlyInside, KB.K("Task hourly inside demands and actuals"))
							, ReportExplorer(107, TId.PerJobInside, TIReports.WOTemplatePerJobInside, KB.K("Task per job inside demands and actuals"))
							, ReportExplorer(108, TId.HourlyOutside, TIReports.WOTemplateHourlyOutside, KB.K("Task hourly outside demands and actuals"))
							, ReportExplorer(109, TId.PerJobOutside, TIReports.WOTemplatePerJobOutside, KB.K("Task per job outside demands and actuals"))
							, ReportExplorer(110, TId.MiscellaneousCost, TIReports.WOTemplateMisc, KB.K("Task miscellaneous demands and actuals"))
						)
					)
				);
		private static MenuDef UnitNode => BrowseExplorer(111, TId.Unit, TILocations.UnitBrowseTblCreator
				, BrowseExplorer(112, TId.ServiceContract, dsMB.Schema.T.ServiceContract)
				, BrowseExplorer(113, TId.Part, dsMB.Schema.T.SparePart)
				, ContainerMenuItem(114, "Reports"
					// TODO: , new MenuDef("Maintenance Status") whatever that means
					, ReportExplorer(115, TId.Unit.ReportSummary, TIReports.UnitSummary, KB.K("One line per selected unit"))
					, ReportExplorer(116, TId.ServiceContract.ReportSummary, TIReports.ServiceContractSummaryReport, KB.K("One line per selected service contract"))
					, ReportExplorer(117, TId.MaintenanceHistory, TIReports.UnitMaintenanceHistory, KB.K("History of work orders on selected units"))
					, ReportExplorer(118, TId.UnitReplacementForecast, TIReports.UnitReplacementSchedule, KB.K("Prediction of unit lifetimes and replacement costs"))
					, ReportExplorer(119, TId.Meter, TIReports.UnitMeters, KB.K("List meters and their current reading"))
					, ReportExplorer(120, TId.MeterReading.ReportHistory, TIReports.MeterReadingHistory, KB.K("History of meter readings"))
				));
		private static MenuDef ItemNode => BrowseExplorer(121, TId.Item, dsMB.Schema.T.Item
				, BrowseExplorer(122, TId.StoreroomAssignment, TILocations.PermanentItemLocationBrowseTblCreator)
				, BrowseExplorer(123, TId.ItemRestocking, dsMB.Schema.T.ItemRestocking)
				, BrowseExplorer(281, TId.ItemPricing, TIItem.ItemPriceTblCreator)
				, ContainerMenuItem(124, "Reports"
					, ReportExplorer(125, TId.ItemActivity, TIReports.InventoryActivity, KB.K("History of all item quantity and value changes"))
					, ReportExplorer(126, TId.ItemIssue, TIReports.InventoryIssue, KB.K("History of items issued without a work order"))
					, ReportExplorer(127, TId.ItemAdjustment, TIReports.InventoryAdjustment, KB.K("History of item adjustments"))
					, ReportExplorer(128, TId.ItemPricing, TIReports.ItemPricing, KB.K("Price quotes for Items"))
					, ReportExplorer(129, "Location and Status", TIReports.StorageLocationStatus, KB.K("Location and status of inventory items"))
					, ReportExplorer(130, TId.ItemsInTemporaryStorage, TIReports.TemporaryInventoryLocation, KB.K("Items assigned to Temporary Storage associated with a work order"))
					, ReportExplorer(131, TId.ItemUsageAsParts, TIReports.InventoryUsageAsParts, KB.K("Items marked as Parts for Units"))
					, ReportExplorer(132, TId.ReceiptActivity, TIReports.ItemReceiving, KB.K("Received items and services"))
				));
		private static MenuDef PoNode => BrowseExplorer(133, TId.PurchaseOrder, dsMB.Schema.T.PurchaseOrder
				, BrowseExplorer(134, TId.DraftPurchaseOrder, TIPurchaseOrder.PurchaseOrderDraftBrowseTbl)
				, BrowseExplorer(135, TId.IssuedPurchaseOrder, TIPurchaseOrder.PurchaseOrderIssuedBrowseTbl)
				, BrowseExplorer(136, TId.ClosedPurchaseOrder, TIPurchaseOrder.PurchaseOrderClosedBrowseTbl)
				, BrowseExplorer(137, TId.VoidPurchaseOrder, TIPurchaseOrder.PurchaseOrderVoidBrowseTbl)
				, ReportExplorer(139, "Print Purchase Orders", TIReports.PurchaseOrderFormReport, KB.K("Print Purchase Order forms"))
				, BrowseExplorer(140, TId.PurchaseOrderAssignment, TIPurchaseOrder.PurchaseOrderAssignmentByAssigneeTblCreator)
				, BrowseExplorer(141, TId.Vendor, TIPurchaseOrder.VendorForPurchaseOrdersTblCreator)
				, BrowseExplorer(142, TId.Receipt, dsMB.Schema.T.Receipt)
				, ContainerMenuItem(143, "Reports"
					, ReportExplorer(144, TId.PurchaseOrder.ReportByAssignee, TIReports.PurchaseOrderByAssigneeFormReport, KB.K("Print Purchase Order forms with multiple copies as needed for distribution to assignees"))
					, ReportExplorer(145, TId.PurchaseOrder.ReportHistory, TIReports.POHistory, KB.K("Report on Purchase Orders with order lines"))
					, ReportExplorer(146, TId.PurchaseOrder.ReportHistory.ReportByAssignee, TIReports.POHistoryByAssignee, KB.K("Report on Purchase Orders by assignee with order lines"))
					, ReportExplorer(147, TId.PurchaseOrder.ReportSummary, TIReports.POSummary, KB.K("Report on Purchase Orders without order lines or State History"))
					, ReportExplorer(148, TId.PurchaseOrder.ReportSummary.ReportByAssignee, TIReports.POSummaryByAssignee, KB.K("Report on Purchase Orders by assignee without order lines or State History"))
					, ReportExplorer(149, TId.PurchaseOrderStateHistory, TIReports.POStateHistory, KB.K("Report on Purchase Orders with State History"))
					, ReportExplorer(150, TId.PurchaseOrderStateHistory.ReportSummary, TIReports.POStateHistorySummary, KB.K("Summary of information from State History section of Purchase Orders, one line per State History entry"))
					, ReportExplorer(151, TId.ItemOnOrder, TIReports.InventoryOnOrder, KB.K("Items currently on order"))
					, ReportExplorer(152, TId.ReceiptActivity, TIReports.PurchaseReceiving, KB.K("Received items and services"))
					)
				, ContainerMenuItem(153, "Charts"
					, ReportExplorer(154, TId.POChartCountByCreatedDate, TIReports.POChartCountByCreatedDate, KB.K("Bar chart showing when Purchase Orders were created"))
					, ReportExplorer(155, TId.POChartCountByIssuedDate, TIReports.POChartCountByIssuedDate, KB.K("Bar chart showing when Purchase Orders were first Issued"))
					, ReportExplorer(156, TId.POChartCountByEndedDate, TIReports.POChartCountByEndedDate, KB.K("Bar chart showing when Purchase Orders ended"))
					, ReportExplorer(157, TId.POChartCount, TIReports.POChartCount, KB.K("Bar chart showing number of Purchase Orders per group"))
					, ReportExplorer(158, TId.POChartAverageDuration, TIReports.POChartAverageDuration, KB.K("Bar chart showing average Purchase Order duration"))
					, ReportExplorer(159, TId.POChartLifetime, TIReports.POChartLifetime, KB.K("Bar chart showing Purchase Order duration based on when the request started"))
					, ReportExplorer(160, TId.POChartStatus, TIReports.POChartStatus, KB.K("Bar chart showing average time in each Purchase Order Status"))
					)
				);
		private static MenuDef DefinitionNode {
			get {
				MenuDef sortedMenu = ContainerMenuItem(161, "Coding Definitions"
					, BrowseExplorer(162, TId.Location, TILocations.LocationBrowseTblCreator
						, BrowseExplorer(163, "Organize", TILocations.LocationOrganizerBrowseTblCreator))
					, BrowseExplorer(164, TId.Contact, TIContact.ContactWithMergeTbl)
					, BrowseExplorer(165, TId.Relationship, dsMB.Schema.T.Relationship)
					, BrowseExplorer(166, TId.CostCenter, dsMB.Schema.T.CostCenter)
					, BrowseExplorer(167, TId.UnitOfMeasure, dsMB.Schema.T.UnitOfMeasure)
					, BrowseExplorer(168, TId.AccessCode, dsMB.Schema.T.AccessCode)
					, BrowseExplorer(169, TId.Vendor, TIPurchaseOrder.VendorTblCreator
						, BrowseExplorer(170, TId.VendorCategory, dsMB.Schema.T.VendorCategory)
					)
				#region Attachments
				, BrowseExplorer(282, TId.Attachment, TIAttachments.AttachmentBrowseTblCreator
							, BrowseExplorer(283, TId.AttachmentPath, TIAttachments.AttachmentPathBrowseTblCreator)
							, BrowseExplorer(284, TId.Specification, TIAttachments.SpecificationWithFormBrowseTblCreator
								, BrowseExplorer(203, TId.SpecificationForm, dsMB.Schema.T.SpecificationForm))
							, BrowseExplorer(285, TId.UnitAttachment, TIAttachments.UnitAttachmentBrowseTblCreator)
							, BrowseExplorer(286, TId.WorkOrderAttachment, TIAttachments.WorkOrderAttachmentBrowseTblCreator)
							, BrowseExplorer(288, TId.WorkOrderTemplateAttachment, TIAttachments.WorkOrderTemplateAttachmentBrowseTblCreator)
					//							, BrowseExplorer(289, TId.AttachmentImage, TIAttachments.AttachmentImageBrowseTblCreator)

					)
				#endregion
				#region Requests
				, BrowseExplorer(171, TId.Request, dsMB.Schema.T.Request
						, BrowseExplorer(172, TId.Requestor, TIRequest.RequestorForRequestsTblCreator)
						, BrowseExplorer(173, TId.RequestAssignee, dsMB.Schema.T.RequestAssignee)
						, BrowseExplorer(174, TId.RequestStatus, dsMB.Schema.T.RequestStateHistoryStatus)
						, BrowseExplorer(175, TId.RequestPriority, dsMB.Schema.T.RequestPriority)
					// NOT FOR 3.0					,embeddedBrowseMenuItem("Request States", dsMB.Schema.T.RequestState)
					)
				#endregion
				#region WorkOrders
				, BrowseExplorer(176, TId.WorkOrder, dsMB.Schema.T.WorkOrder
						, BrowseExplorer(177, TId.Requestor, TIRequest.RequestorForWorkOrdersTblCreator)
						, BrowseExplorer(178, TId.WorkOrderAssignee, dsMB.Schema.T.WorkOrderAssignee)
						, BrowseExplorer(179, TId.WorkOrderStatus, dsMB.Schema.T.WorkOrderStateHistoryStatus)
						, BrowseExplorer(180, TId.BillableRequestor, dsMB.Schema.T.BillableRequestor)
						, BrowseExplorer(181, TId.ChargebackCategory, dsMB.Schema.T.ChargebackLineCategory)
						, BrowseExplorer(182, TId.ClosingCode, dsMB.Schema.T.CloseCode)
						, BrowseExplorer(183, TId.MiscellaneousCost, dsMB.Schema.T.MiscellaneousWorkOrderCost)
						, ContainerMenuItem(184, "Labor"
							, BrowseExplorer(185, TId.Employee, dsMB.Schema.T.Employee)
							, BrowseExplorer(186, TId.Trade, dsMB.Schema.T.Trade)
							, BrowseExplorer(187, TId.HourlyInside, dsMB.Schema.T.LaborInside)
							, BrowseExplorer(188, TId.HourlyOutside, dsMB.Schema.T.LaborOutside)
							, BrowseExplorer(189, TId.PerJobInside, dsMB.Schema.T.OtherWorkInside)
							, BrowseExplorer(190, TId.PerJobOutside, dsMB.Schema.T.OtherWorkOutside)
						)
						, BrowseExplorer(191, TId.Project, dsMB.Schema.T.Project)
						, BrowseExplorer(192, TId.WorkCategory, dsMB.Schema.T.WorkCategory)
						, BrowseExplorer(193, TId.ExpenseCategory, dsMB.Schema.T.WorkOrderExpenseCategory)
						, BrowseExplorer(194, TId.ExpenseModel, dsMB.Schema.T.WorkOrderExpenseModel)
						, BrowseExplorer(195, TId.WorkOrderPriority, dsMB.Schema.T.WorkOrderPriority)
					)
				#endregion
				#region Tasks
				, BrowseExplorer(222, TId.UnitMaintenancePlan, TISchedule.ScheduledWorkOrderBrowserTbl
						, BrowseExplorer(223, TId.Task, dsMB.Schema.T.WorkOrderTemplate)
						, BrowseExplorer(224, TId.MaintenanceTiming, dsMB.Schema.T.Schedule)
						, BrowseExplorer(225, TId.PurchaseOrderTemplate, dsMB.Schema.T.PurchaseOrderTemplate)
					)
				#endregion
				#region Units
				, BrowseExplorer(196, TId.Unit, TILocations.UnitBrowseTblCreator
						, BrowseExplorer(197, TId.AssetCode, dsMB.Schema.T.AssetCode)
						, BrowseExplorer(198, TId.ServiceContract, dsMB.Schema.T.ServiceContract)
						, BrowseExplorer(199, TId.Meter, TIUnit.MeterWithManualReadingBrowseTbl)
						, BrowseExplorer(200, TId.MeterClass, dsMB.Schema.T.MeterClass)
						, BrowseExplorer(201, TId.Part, dsMB.Schema.T.SparePart)
						, BrowseExplorer(202, TId.Ownership, dsMB.Schema.T.Ownership)
						, BrowseExplorer(204, TId.System, dsMB.Schema.T.SystemCode)
						, BrowseExplorer(205, TId.UnitCategory, dsMB.Schema.T.UnitCategory)
						, BrowseExplorer(206, TId.UnitUsage, dsMB.Schema.T.UnitUsage)
					)
				#endregion
				#region Items
				, BrowseExplorer(207, TId.Item, dsMB.Schema.T.Item
						, BrowseExplorer(208, TId.ItemAdjustmentCode, dsMB.Schema.T.ItemAdjustmentCode)
						, BrowseExplorer(209, TId.ItemCategory, dsMB.Schema.T.ItemCategory)
						, BrowseExplorer(210, TId.ItemIssueCode, dsMB.Schema.T.ItemIssueCode)
						, BrowseExplorer(211, TId.Storeroom, TILocations.PermanentStorageBrowseTblCreator)
						, BrowseExplorer(212, TId.StoreroomAssignment, TILocations.PermanentItemLocationBrowseTblCreator)
						, BrowseExplorer(213, TId.VoidCode, dsMB.Schema.T.VoidCode)
						)
				#endregion
				#region Purchase Orders
				, BrowseExplorer(214, TId.PurchaseOrder, dsMB.Schema.T.PurchaseOrder
						, BrowseExplorer(215, TId.PurchaseOrderAssignee, dsMB.Schema.T.PurchaseOrderAssignee)
						, BrowseExplorer(216, TId.Project, dsMB.Schema.T.Project)
						, BrowseExplorer(217, TId.PurchaseOrderCategory, dsMB.Schema.T.PurchaseOrderCategory)
						, BrowseExplorer(218, TId.PaymentTerm, dsMB.Schema.T.PaymentTerm)
						, BrowseExplorer(219, TId.ShippingMode, dsMB.Schema.T.ShippingMode)
						, BrowseExplorer(220, TId.PurchaseOrderStatus, dsMB.Schema.T.PurchaseOrderStateHistoryStatus)
						, BrowseExplorer(221, TId.MiscellaneousItem, dsMB.Schema.T.Miscellaneous)
					)
				#endregion
#if ZTESTTABLE
				,browseExplorer("ZTestTable", dsMB.Schema.T.ZTestTable)
#endif
			);
				if (sortedMenu != null)
					return sortedMenu.Sort();
				return sortedMenu;
			}
		}

		private static MenuDef AdminNode => BrowseExplorer(226, TId.Administration, TIGeneralMB3.AdministrationTblCreator, AdminSubNodes);
		private static MenuDef[] AdminSubNodes => new MenuDef[] {
				BrowseExplorer(227, TId.CompanyInformation, TIGeneralMB3.CompanyInformationTblCreator)
				, BrowseExplorer(228, TId.User, dsMB.Schema.T.User,
						BrowseExplorer(229, TId.SQLDatabaseLogin, TISecurity.ManageDatabaseLoginTblCreator),
						BrowseExplorer(230, TId.SQLDatabaseUser, TISecurity.ManageDatabaseUserTblCreator)
				)
				, BrowseExplorer(231, TId.SecurityRole, TISecurity.RoleBrowserTblCreator)
#if DEBUG
				, BrowseExplorer(232, "DEBUG: Settings", TIGeneralMB3.SettingsTblCreator)
#endif
				, BrowseExplorer(233, TId.License, dsMB.Schema.T.License)
				, BrowseExplorer(234, TId.DatabaseManagement, TIGeneralMB3.DatabaseManagementTblCreator)
				, BrowseExplorer(235, TId.Backup, dsMB.Schema.T.BackupFileName)
				, BrowseExplorer(236, TId.ExternalTag, dsMB.Schema.T.ExternalTag)
				, BrowseExplorer(237, TId.Accounting, TIGeneralMB3.AccountingTransactionDerivationsTblCreator
					, ReportExplorer(238, TId.AccountingLedger, TIReports.AccountingLedgerReport, KB.K("Accounting transactions in Ledger format")))
				, BrowseExplorer(239, TId.MainBossService, TIMainBossService.MainBossServiceManageTblCreator
					, BrowseExplorer(240, "Configuration", dsMB.Schema.T.ServiceConfiguration)
					, BrowseExplorer(241, TId.EmailRequest, dsMB.Schema.T.EmailRequest)
#if DEBUG
					, BrowseExplorer(242, "DEBUG: Pending Requestor Acknowledgements", dsMB.Schema.T.RequestAcknowledgement)
					, BrowseExplorer(243, "DEBUG: Message Definitions", TIGeneralMB3.UserMessageKeyWithEditAbilityTblCreator)
#endif
				)
			};

		#endregion
		#region Overall content
		public static MenuDef[] MainContents {
			get {
				return new MenuDef[] {
					new BrowseMenuDef(Id(250), KB.TOControlPanel(TId.MainBossOverview), TIGeneralMB3.FindDelayedBrowseTbl(dsMB.Schema.T.DatabaseStatus)),
					AssignmentNode,
					RequestNode,
					WoNode,
					UnitMaintenancePlanNode,
					UnitNode,
					ItemNode,
					PoNode,
					DefinitionNode,
					AdminNode               };
			}
		}
		private static MenuDef[] AssignmentContents {
			get {
				return AssignmentNode.subMenu;
			}
		}
		public MenuDef OverallContent() {
#if DEBUG
			DuplicateCatcher.Clear();
#endif
			// TODO: Right now both the local item and the children depend on the mode.
			// Ideally everyone would use MainContents; it and its children would contain mode dependencies.
			if (TIGeneralMB3.MainBossModeGroup.Enabled || TIGeneralMB3.RequestsModeGroup.Enabled)
				return BrowseExplorer(244, TId.MainBossNewsPanel, TIGeneralMB3.NewsPanelTblCreator, MainContents);
			else if (TIGeneralMB3.AssignmentsModeGroup.Enabled)
				return new BrowseMenuDef(Id(260), KB.TOControlPanel(TId.MyAssignmentOverview), TIGeneralMB3.AssignmentStatusTblCreator, AssignmentContents);
			else if (TIGeneralMB3.AdministrationModeGroup.Enabled)
				return new BrowseMenuDef(Id(270), KB.TOControlPanel(TId.Administration), TIGeneralMB3.AdministrationTblCreator, AdminSubNodes);
			else if (TIGeneralMB3.SessionsModeGroup.Enabled)
				return new BrowseMenuDef(Id(280), KB.TOControlPanel(TId.Session), TIGeneralMB3.FindDelayedBrowseTbl(dsMB.Schema.T.Session));
			else
				throw new System.InvalidOperationException(KB.I("ControlPanelLayout: unknown application mode"));
		}
		#endregion
	}
}
