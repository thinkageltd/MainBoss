using System;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Database;
namespace Thinkage.MainBoss.Controls {
	public class ControlPanelLayout {
		/// <summary>
		/// Placeholder to record highest id number allocated in this file. If you add any more just change this to reflect the highest one.
		/// </summary>
#if DEBUG
		const int HighestIdNumberUsedInThisFile = 281;
		static Thinkage.Libraries.Collections.Set<int> DuplicateCatcher = new Libraries.Collections.Set<int>();
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
		private MenuDef browseExplorer(int id, [Context("ControlPanel", Level = 1)] string name, DBI_Table table, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), ControlPanelContext.K(name), TIGeneralMB3.FindDelayedBrowseTbl(table), sub);
		}
		private MenuDef browseExplorer(int id, [Context("ControlPanel", Level = 1)] string name, DelayedCreateTbl table, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), ControlPanelContext.K(name), table, sub);
		}
		private MenuDef browseExplorer(int id, Tbl.TblIdentification name, DBI_Table table, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), KB.TOControlPanel(name), TIGeneralMB3.FindDelayedBrowseTbl(table), sub);
		}
		private MenuDef browseExplorer(int id, Tbl.TblIdentification name, DelayedCreateTbl delayedTbl, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), KB.TOControlPanel(name), delayedTbl, sub);
		}
		private MenuDef browseExplorer(int id, Tbl.TblIdentification name, DBI_Table table, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), KB.TOControlPanel(name), TIGeneralMB3.FindDelayedBrowseTbl(table), featureGroup, sub);
		}
		private MenuDef browseExplorer(int id, Tbl.TblIdentification name, DelayedCreateTbl delayedTbl, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new BrowseMenuDef(Id(id), KB.TOControlPanel(name), delayedTbl, featureGroup, sub);
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
		private MenuDef reportExplorer(int id, [Context("ControlPanel", Level = 1)] string name, Tbl tableInfo, Key userTip, params MenuDef[] sub) {
			return new ReportMenuDef(Id(id), ControlPanelContext.K(name), tableInfo, userTip, null, sub);
		}
		private MenuDef reportExplorer(int id, [Context("ControlPanel", Level = 1)] string name, Tbl tableInfo, Key userTip, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new ReportMenuDef(Id(id), ControlPanelContext.K(name), tableInfo, userTip, featureGroup, sub);
		}
		private MenuDef reportExplorer(int id, Tbl.TblIdentification name, Tbl tableInfo, Key userTip, params MenuDef[] sub) {
			return new ReportMenuDef(Id(id), KB.TOControlPanel(name), tableInfo, userTip, null, sub);
		}
		private MenuDef reportExplorer(int id, Tbl.TblIdentification name, Tbl tableInfo, params MenuDef[] sub) {
			return new ReportMenuDef(Id(id), KB.TOControlPanel(name), tableInfo, null, null, sub);
		}
		#endregion
		#region General Support
		public static MenuDef containerMenuItem(int id, [Context("ControlPanel", Level = 1)] string name, params MenuDef[] sub) {
			return new MenuDef(Id(id), ControlPanelContext.K(name), sub);
		}
		public static MenuDef containerMenuItem(int id, [Context("ControlPanel", Level = 1)] string name, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new MenuDef(Id(id), ControlPanelContext.K(name), featureGroup, sub);
		}
		public static MenuDef containerMenuItem(int id, Tbl.TblIdentification name, params MenuDef[] sub) {
			return new MenuDef(Id(id), KB.TOControlPanel(name), sub);
		}
		public static MenuDef containerMenuItem(int id, Tbl.TblIdentification name, FeatureGroup featureGroup, params MenuDef[] sub) {
			return new MenuDef(Id(id), KB.TOControlPanel(name), featureGroup, sub);
		}
		#endregion
		#region Data-related menu groups
		private MenuDef assignmentNode() {
			return browseExplorer(1, TId.MyAssignmentOverview, TIGeneralMB3.AssignmentStatusTblCreator
				, browseExplorer(2, TId.InProgressRequest, TIRequest.RequestInProgressAssignedToBrowseTbl
					, browseExplorer(3, TId.UnassignedRequest, TIRequest.UnassignedRequestBrowseTbl))
				, browseExplorer(4, TId.OpenWorkOrder, TIWorkOrder.WorkOrderInProgressAssignedToBrowseTbl
					, browseExplorer(5, TId.UnassignedWorkOrder, TIWorkOrder.UnassignedWorkOrderBrowseTbl))
				, browseExplorer(6, TId.IssuedPurchaseOrder, TIPurchaseOrder.PurchaseOrderInProgressAssignedToBrowseTbl
					, browseExplorer(7, TId.UnassignedPurchaseOrder, TIPurchaseOrder.UnassignedPurchaseOrderBrowseTbl))
				);
		}

		private MenuDef requestNode() {
			return browseExplorer(8, TId.Request, dsMB.Schema.T.Request
				, browseExplorer(9, TId.NewRequest, TIRequest.RequestNewBrowseTbl)
				, browseExplorer(10, TId.InProgressRequest, TIRequest.RequestInProgressBrowseTbl
					, browseExplorer(11, TId.InProgressRequestWithNoLinkedWorkOrder, TIRequest.RequestInProgressWithoutWOBrowseTbl)
					, browseExplorer(12, TId.InProgressRequestWithLinkedWorkOrder, TIRequest.RequestInProgressWithWOBrowseTbl)
				)
				, browseExplorer(13, TId.ClosedRequest, TIRequest.RequestClosedBrowseTbl)
				, reportExplorer(14, "Print Requests", TIReports.RequestFormReport, KB.K("Print Request forms for distribution"))
				, browseExplorer(15, TId.Requestor, TIRequest.RequestorForRequestsTblCreator)
				, browseExplorer(16, TId.RequestAssignment, TIRequest.RequestAssignmentByAssigneeTblCreator)
				, containerMenuItem(17, "Reports"
					, reportExplorer(18, TId.Request.ReportByAssignee, TIReports.RequestByAssigneeFormReport, KB.K("Print Request forms for distribution to assignees"))
					, reportExplorer(19, TId.Request.ReportSummary, TIReports.RequestSummary, KB.K("Summary with one line per request"))
					, reportExplorer(20, TId.Request.ReportSummary.ReportByAssignee, TIReports.RequestByAssigneeSummary, KB.K("Summary by assignee with one line per request"))
					, reportExplorer(21, TId.Request.ReportHistory, TIReports.RequestHistory, KB.K("Detailed info on selected requests"))
					, reportExplorer(22, TId.Request.ReportHistory.ReportByAssignee, TIReports.RequestHistoryByAssignee, KB.K("Detailed info on selected requests by assignees"))
					, reportExplorer(23, TId.RequestStateHistory, TIReports.RequestStateHistory, KB.K("Report on Requests with State History"))
					, reportExplorer(24, TId.RequestStateHistory.ReportSummary, TIReports.RequestStateHistorySummary, KB.K("Summary of information from State History section of requests, one line per State History entry"))
					)
				, containerMenuItem(25, "Charts"
					, reportExplorer(26, TId.RequestChartCountByCreatedDate, TIReports.RequestChartCountByCreatedDate, KB.K("Bar chart showing when Requests were created"))
					, reportExplorer(27, TId.RequestChartCountByInProgressDate, TIReports.RequestChartCountByInProgressDate, KB.K("Bar chart showing when Requests were first made In Progress"))
					, reportExplorer(28, TId.RequestChartCountByEndedDate, TIReports.RequestChartCountByEndedDate, KB.K("Bar chart showing when Requests ended"))
					, reportExplorer(29, TId.RequestChartCount, TIReports.RequestChartCount, KB.K("Bar chart showing number of Requests per group"))
					, reportExplorer(30, TId.RequestChartAverageDuration, TIReports.RequestChartAverageDuration, KB.K("Bar chart showing average Request duration"))
					, reportExplorer(31, TId.RequestChartLifetime, TIReports.RequestChartLifetime, KB.K("Bar chart showing Request duration based on when the request started"))
					, reportExplorer(32, TId.RequestChartStatus, TIReports.RequestChartStatus, KB.K("Bar chart showing average time in each Request Status"))
				)
				);
		}
		private MenuDef woNode() {
			return browseExplorer(33, TId.WorkOrder, TIWorkOrder.WorkOrderAllBrowseTbl
				, browseExplorer(34, TId.OverdueWorkOrder, TIWorkOrder.WorkOrderOverdueBrowseTbl)
				, browseExplorer(35, TId.DraftWorkOrder, TIWorkOrder.WorkOrderDraftBrowseTbl)
				, browseExplorer(36, TId.OpenWorkOrder, TIWorkOrder.WorkOrderOpenBrowseTbl)
				, browseExplorer(37, TId.ClosedWorkOrder, TIWorkOrder.WorkOrderClosedBrowseTbl)
				, browseExplorer(38, TId.VoidWorkOrder, TIWorkOrder.WorkOrderVoidBrowseTbl)
				, reportExplorer(39, "Print Work Orders", TIReports.WorkOrderFormReport, KB.K("Print Work Order forms"))
				, browseExplorer(40, TId.Requestor, TIRequest.RequestorForWorkOrdersTblCreator)
				, browseExplorer(41, TId.WorkOrderAssignment, TIWorkOrder.WorkOrderAssignmentByAssigneeTblCreator)
				, browseExplorer(42, TId.BillableRequestor, dsMB.Schema.T.BillableRequestor)
				, browseExplorer(43, TId.Chargeback, TIWorkOrder.AllChargebackTbl)
				, containerMenuItem(44, "Reports"
					, reportExplorer(45, TId.WorkOrder.ReportByAssignee, TIReports.WorkOrderByAssigneeFormReport, KB.K("Print Work Order forms with multiple copies as needed for distribution to assignees"))
					, reportExplorer(46, TId.WorkOrder.ReportHistory, TIReports.WOHistory, KB.K("Report on Work Orders with Resources"))
					, reportExplorer(47, TId.WorkOrder.ReportHistory.ReportByAssignee, TIReports.WOHistoryByAssignee, KB.K("Report on Work Orders by assignee with Resources"))
					, reportExplorer(48, TId.WorkOrder.ReportSummary, TIReports.WOSummary, KB.K("Report on Work Orders without Resources or State History"))
					, reportExplorer(49, TId.WorkOrder.ReportSummary.ReportByAssignee, TIReports.WOSummaryByAssignee, KB.K("Report on Work Orders by assignee without Resources or State History"))
					, reportExplorer(50, TId.WorkOrderStateHistory, TIReports.WOStateHistory, KB.K("Report on Work Orders with State History"))
					, reportExplorer(51, TId.WorkOrderStateHistory.ReportSummary, TIReports.WOStateHistorySummary, KB.K("Summary of information from State History section of work orders, one line per State History entry"))
					, reportExplorer(52, TId.Chargeback.ReportHistory, TIReports.ChargebackHistoryReport, KB.K("Report on Chargebacks with line items"))
					, reportExplorer(53, TId.Chargeback.ReportSummary, TIReports.ChargebackSummaryReport, KB.K("Report on Chargebacks without line items"))
					, reportExplorer(54, TId.ChargebackActivity, TIReports.ChargebackLineReport, KB.K("Report on individual Chargeback line items"))
					, reportExplorer(55, TId.TemporaryStorage, TIReports.TemporaryStorageReport, KB.K("Report on Temporary Storage usage"))
					, reportExplorer(56, TId.WorkOrderResourceDemand, TIReports.WOResourceDemand, KB.K("Work Order Resources with demanded and actual quantity and cost")
						, reportExplorer(57, TId.WorkOrderItem, TIReports.WODemandItem, KB.K("Work Order Items with demanded and actual quantity and cost"))
						, reportExplorer(58, TId.WorkOrderHourlyInside, TIReports.WODemandHourlyInside, KB.K("Work Order Hourly Inside with demanded and actual time and cost"))
						, reportExplorer(59, TId.WorkOrderPerJobInside, TIReports.WODemandPerJobInside, KB.K("Work Order Per Job Inside with demanded and actual quantity and cost"))
						, reportExplorer(60, TId.WorkOrderHourlyOutside, TIReports.WODemandHourlyOutside, KB.K("Work Order Hourly Outside with demanded and actual time and cost"))
						, reportExplorer(61, TId.WorkOrderPerJobOutside, TIReports.WODemandPerJobOutside, KB.K("Work Order Per Job Outside with demanded and actual quantity and cost"))
						, reportExplorer(62, TId.WorkOrderMiscellaneousExpense, TIReports.WODemandMiscellaneousWorkOrderCost, KB.K("Work Order Miscellaneous with demanded and actual cost"))
						, reportExplorer(63, TId.ReceiptActivity, TIReports.ResourceReceiving, KB.K("Received items and services"))
					)
					, containerMenuItem(64, TId.WorkOrderResourceActual
						, reportExplorer(65, TId.WorkOrderItemActual, TIReports.WOActualItem, KB.K("Work Order Items actually used"))
						, reportExplorer(66, TId.WorkOrderHourlyInsideActual, TIReports.WOActualHourlyInside, KB.K("Work Order Hourly Inside actually used"))
						, reportExplorer(67, TId.WorkOrderPerJobInsideActual, TIReports.WOActualPerJobInside, KB.K("Work Order Per Job Inside actually used"))
						, reportExplorer(68, TId.WorkOrderHourlyOutsidePOActual, TIReports.WOActualHourlyOutsidePO, KB.K("Work Order Hourly Outside actually used (with PO)"))
						, reportExplorer(69, TId.WorkOrderPerJobOutsidePOActual, TIReports.WOActualPerJobOutsidePO, KB.K("Work Order Per Job Outside actually used (with PO)"))
						, reportExplorer(70, TId.WorkOrderHourlyOutsideNonPOActual, TIReports.WOActualHourlyOutsideNonPO, KB.K("Work Order Hourly Outside actually used (no PO)"))
						, reportExplorer(71, TId.WorkOrderPerJobOutsideNonPOActual, TIReports.WOActualPerJobOutsideNonPO, KB.K("Work Order Per Job Outside actually used (no PO)"))
						, reportExplorer(72, TId.WorkOrderMiscellaneousExpenseActual, TIReports.WOActualMiscellaneousWorkOrderCost, KB.K("Work Order Miscellaneous actually incurred"))
					)
				)
				, containerMenuItem(73, "Charts"
					, reportExplorer(74, TId.WOChartCountByCreatedDate, TIReports.WOChartCountByCreatedDate, KB.K("Bar chart showing when work orders were created"))
					, reportExplorer(75, TId.WOChartCountByOpenedDate, TIReports.WOChartCountByOpenedDate, KB.K("Bar chart showing when Work Orders were first Opened"))
					, reportExplorer(76, TId.WOChartCountByEndedDate, TIReports.WOChartCountByEndedDate, KB.K("Bar chart showing when Work Orders ended"))
					, reportExplorer(77, TId.WOChartCount, TIReports.WOChartCount, KB.K("Bar chart showing number of Work Orders per group"))
					, reportExplorer(78, TId.WOChartAverageDuration, TIReports.WOChartAverageDuration, KB.K("Bar chart showing average Work Order duration"))
					, reportExplorer(79, TId.WOChartTotalDuration, TIReports.WOChartTotalDuration, KB.K("Bar chart showing total Work Order duration"))
					, reportExplorer(80, TId.WOChartAverageDowntime, TIReports.WOChartAverageDowntime, KB.K("Bar chart showing average downtime per group"))
					, reportExplorer(81, TId.WOChartTotalDowntime, TIReports.WOChartTotalDowntime, KB.K("Bar chart showing total downtime per group"))
					, reportExplorer(82, TId.WOChartLifetime, TIReports.WOChartLifetime, KB.K("Range chart showing work order duration based on when the work order started"))
					, reportExplorer(83, TId.WOChartStatus, TIReports.WOChartStatus, KB.K("Bar chart showing average time in each Work Order Status"))
					, containerMenuItem(84, TId.WorkOrderResourceActual
						, reportExplorer(85, TId.WOChartCostsByResourceType, TIReports.WOChartCostsByResourceType, KB.K("Bar chart showing costs by resource type"))
						, reportExplorer(86, TId.WOChartCostsByTrade, TIReports.WOChartCostsByTrade, KB.K("Bar chart showing labor costs broken down by trade"))
						, reportExplorer(87, TId.WOChartCostsByEmployee, TIReports.WOChartCostsByEmployee, KB.K("Bar chart showing inside labor costs broken down by employee"))
						, reportExplorer(88, TId.WOChartCostsByVendor, TIReports.WOChartCostsByVendor, KB.K("Bar chart showing outside labor costs broken down by vendor/contractor"))
						, reportExplorer(89, TId.WOChartCosts, TIReports.WOChartCosts, KB.K("Bar chart showing costs broken down by groups in Grouping section"))
						, reportExplorer(90, TId.WOChartLaborTimeByTrade, TIReports.WOChartHoursByTrade, KB.K("Bar chart showing labor time broken down by trade"))
						, reportExplorer(91, TId.WOChartLaborTimeByEmployee, TIReports.WOChartHoursByEmployee, KB.K("Bar chart showing inside labor time broken down by employee"))
						, reportExplorer(92, TId.WOChartLaborTimeByVendor, TIReports.WOChartHoursByVendor, KB.K("Bar chart showing outside labor time broken down by vendor/contractor"))
						, reportExplorer(93, TId.WOChartLaborTime, TIReports.WOChartHours, KB.K("Bar chart showing labor time broken down by groups in Grouping section"))
					)
				)
			);
		}
		private MenuDef unitMaintenancePlanNode() {
			return browseExplorer(94, TId.UnitMaintenancePlan, TISchedule.ScheduledWorkOrderBrowserTbl
					, browseExplorer(95, "Generate Planned Maintenance", dsMB.Schema.T.PMGenerationBatch)
					, browseExplorer(96, TId.Task, dsMB.Schema.T.WorkOrderTemplate)
					, containerMenuItem(97, "Reports"
						, reportExplorer(98, TId.UnitMaintenancePlan.ReportSummary, TIReports.ScheduledWorkOrderSummary, KB.K("One line per selected unit maintenance plan"))
						, reportExplorer(99, TId.Task.ReportSummary, TIReports.WorkOrderTemplateSummary, KB.K("One line per selected task"))
						, reportExplorer(100, TId.LaborForecast, TIReports.LaborForecast, KB.K("Prediction of future labor requirements"))
						, reportExplorer(101, TId.MaterialForecast, TIReports.MaterialForecast, KB.K("Prediction of future item requirements"))
						, reportExplorer(102, TId.MaintenanceForecast, TIReports.MaintenanceForecast, KB.K("Prediction of future work orders"))
						, reportExplorer(103, TId.MaintenanceTiming, TIReports.MaintenanceTimings, KB.K("List maintenance timing records"))
						, reportExplorer(104, TId.TaskResource, TIReports.WOTemplateResource, KB.K("Task items, labor, and miscellaneous expenses")
							, reportExplorer(105, TId.Item, TIReports.WOTemplateItem, KB.K("Task demands and actuals"))
							, reportExplorer(106, TId.HourlyInside, TIReports.WOTemplateHourlyInside, KB.K("Task hourly inside demands and actuals"))
							, reportExplorer(107, TId.PerJobInside, TIReports.WOTemplatePerJobInside, KB.K("Task per job inside demands and actuals"))
							, reportExplorer(108, TId.HourlyOutside, TIReports.WOTemplateHourlyOutside, KB.K("Task hourly outside demands and actuals"))
							, reportExplorer(109, TId.PerJobOutside, TIReports.WOTemplatePerJobOutside, KB.K("Task per job outside demands and actuals"))
							, reportExplorer(110, TId.MiscellaneousCost, TIReports.WOTemplateMisc, KB.K("Task miscellaneous demands and actuals"))
						)
					)
				);
		}
		private MenuDef unitNode() {
			return browseExplorer(111, TId.Unit, TILocations.UnitBrowseTblCreator
				, browseExplorer(112, TId.ServiceContract, dsMB.Schema.T.ServiceContract)
				, browseExplorer(113, TId.Part, dsMB.Schema.T.SparePart)
				, containerMenuItem(114, "Reports"
				// TODO: , new MenuDef("Maintenance Status") whatever that means
					, reportExplorer(115, TId.Unit.ReportSummary, TIReports.UnitSummary, KB.K("One line per selected unit"))
					, reportExplorer(116, TId.ServiceContract.ReportSummary, TIReports.ServiceContractSummaryReport, KB.K("One line per selected service contract"))
					, reportExplorer(117, TId.MaintenanceHistory, TIReports.UnitMaintenanceHistory, KB.K("History of work orders on selected units"))
					, reportExplorer(118, TId.UnitReplacementForecast, TIReports.UnitReplacementSchedule, KB.K("Prediction of unit lifetimes and replacement costs"))
					, reportExplorer(119, TId.Meter, TIReports.UnitMeters, KB.K("List meters and their current reading"))
					, reportExplorer(120, TId.MeterReading.ReportHistory, TIReports.MeterReadingHistory, KB.K("History of meter readings"))
				));
		}
		private MenuDef itemNode() {
			return browseExplorer(121, TId.Item, dsMB.Schema.T.Item
				, browseExplorer(122, TId.StoreroomAssignment, TILocations.PermanentItemLocationBrowseTblCreator)
				, browseExplorer(123, TId.ItemRestocking, dsMB.Schema.T.ItemRestocking)
				, browseExplorer(281, TId.ItemPricing, TIItem.ItemPriceTblCreator)
				, containerMenuItem(124, "Reports"
					, reportExplorer(125, TId.ItemActivity, TIReports.InventoryActivity, KB.K("History of all item quantity and value changes"))
					, reportExplorer(126, TId.ItemIssue, TIReports.InventoryIssue, KB.K("History of items issued without a work order"))
					, reportExplorer(127, TId.ItemAdjustment, TIReports.InventoryAdjustment, KB.K("History of item adjustments"))
					, reportExplorer(128, TId.ItemPricing, TIReports.ItemPricing, KB.K("Price quotes for Items"))
					, reportExplorer(129, "Location and Status", TIReports.StorageLocationStatus, KB.K("Location and status of inventory items"))
					, reportExplorer(130, TId.ItemsInTemporaryStorage, TIReports.TemporaryInventoryLocation, KB.K("Items assigned to Temporary Storage associated with a work order"))
					, reportExplorer(131, TId.ItemUsageAsParts, TIReports.InventoryUsageAsParts, KB.K("Items marked as Parts for Units"))
					, reportExplorer(132, TId.ReceiptActivity, TIReports.ItemReceiving, KB.K("Received items and services"))
				));
		}
		private MenuDef poNode() {
			return browseExplorer(133, TId.PurchaseOrder, dsMB.Schema.T.PurchaseOrder
				, browseExplorer(134, TId.DraftPurchaseOrder, TIPurchaseOrder.PurchaseOrderDraftBrowseTbl)
				, browseExplorer(135, TId.IssuedPurchaseOrder, TIPurchaseOrder.PurchaseOrderIssuedBrowseTbl)
				, browseExplorer(136, TId.ClosedPurchaseOrder, TIPurchaseOrder.PurchaseOrderClosedBrowseTbl)
				, browseExplorer(137, TId.VoidPurchaseOrder, TIPurchaseOrder.PurchaseOrderVoidBrowseTbl)
				, reportExplorer(139, "Print Purchase Orders", TIReports.PurchaseOrderFormReport, KB.K("Print Purchase Order forms"))
				, browseExplorer(140, TId.PurchaseOrderAssignment, TIPurchaseOrder.PurchaseOrderAssignmentByAssigneeTblCreator)
				, browseExplorer(141, TId.Vendor, TIPurchaseOrder.VendorForPurchaseOrdersTblCreator)
				, browseExplorer(142, TId.Receipt, dsMB.Schema.T.Receipt)
				, containerMenuItem(143, "Reports"
					, reportExplorer(144, TId.PurchaseOrder.ReportByAssignee, TIReports.PurchaseOrderByAssigneeFormReport, KB.K("Print Purchase Order forms with multiple copies as needed for distribution to assignees"))
					, reportExplorer(145, TId.PurchaseOrder.ReportHistory, TIReports.POHistory, KB.K("Report on Purchase Orders with order lines"))
					, reportExplorer(146, TId.PurchaseOrder.ReportHistory.ReportByAssignee, TIReports.POHistoryByAssignee, KB.K("Report on Purchase Orders by assignee with order lines"))
					, reportExplorer(147, TId.PurchaseOrder.ReportSummary, TIReports.POSummary, KB.K("Report on Purchase Orders without order lines or State History"))
					, reportExplorer(148, TId.PurchaseOrder.ReportSummary.ReportByAssignee, TIReports.POSummaryByAssignee, KB.K("Report on Purchase Orders by assignee without order lines or State History"))
					, reportExplorer(149, TId.PurchaseOrderStateHistory, TIReports.POStateHistory, KB.K("Report on Purchase Orders with State History"))
					, reportExplorer(150, TId.PurchaseOrderStateHistory.ReportSummary, TIReports.POStateHistorySummary, KB.K("Summary of information from State History section of Purchase Orders, one line per State History entry"))
					, reportExplorer(151, TId.ItemOnOrder, TIReports.InventoryOnOrder, KB.K("Items currently on order"))
					, reportExplorer(152, TId.ReceiptActivity, TIReports.PurchaseReceiving, KB.K("Received items and services"))
					)
				, containerMenuItem(153, "Charts"
					, reportExplorer(154, TId.POChartCountByCreatedDate, TIReports.POChartCountByCreatedDate, KB.K("Bar chart showing when Purchase Orders were created"))
					, reportExplorer(155, TId.POChartCountByIssuedDate, TIReports.POChartCountByIssuedDate, KB.K("Bar chart showing when Purchase Orders were first Issued"))
					, reportExplorer(156, TId.POChartCountByEndedDate, TIReports.POChartCountByEndedDate, KB.K("Bar chart showing when Purchase Orders ended"))
					, reportExplorer(157, TId.POChartCount, TIReports.POChartCount, KB.K("Bar chart showing number of Purchase Orders per group"))
					, reportExplorer(158, TId.POChartAverageDuration, TIReports.POChartAverageDuration, KB.K("Bar chart showing average Purchase Order duration"))
					, reportExplorer(159, TId.POChartLifetime, TIReports.POChartLifetime, KB.K("Bar chart showing Purchase Order duration based on when the request started"))
					, reportExplorer(160, TId.POChartStatus, TIReports.POChartStatus, KB.K("Bar chart showing average time in each Purchase Order Status"))
					)
				);
		}
		private MenuDef definitionNode() {
			MenuDef sortedMenu = containerMenuItem(161, "Coding Definitions"
				, browseExplorer(162, TId.Location, TILocations.LocationBrowseTblCreator
					, browseExplorer(163, "Organize", TILocations.LocationOrganizerBrowseTblCreator))
				, browseExplorer(164, TId.Contact, dsMB.Schema.T.Contact)
				, browseExplorer(165, TId.Relationship, dsMB.Schema.T.Relationship)
				, browseExplorer(166, TId.CostCenter, dsMB.Schema.T.CostCenter)
				, browseExplorer(167, TId.UnitOfMeasure, dsMB.Schema.T.UnitOfMeasure)
				, browseExplorer(168, TId.AccessCode, dsMB.Schema.T.AccessCode)
				, browseExplorer(169, TId.Vendor, TIPurchaseOrder.VendorTblCreator
					, browseExplorer(170, TId.VendorCategory, dsMB.Schema.T.VendorCategory)
				)
			#region Requests
				, browseExplorer(171, TId.Request, dsMB.Schema.T.Request
					, browseExplorer(172, TId.Requestor, TIRequest.RequestorForRequestsTblCreator)
					, browseExplorer(173, TId.RequestAssignee, dsMB.Schema.T.RequestAssignee)
					, browseExplorer(174, TId.RequestStatus, dsMB.Schema.T.RequestStateHistoryStatus)
					, browseExplorer(175, TId.RequestPriority, dsMB.Schema.T.RequestPriority)
				// NOT FOR 3.0					,embeddedBrowseMenuItem("Request States", dsMB.Schema.T.RequestState)
				)
			#endregion
			#region WorkOrders
				, browseExplorer(176, TId.WorkOrder, dsMB.Schema.T.WorkOrder
					, browseExplorer(177, TId.Requestor, TIRequest.RequestorForWorkOrdersTblCreator)
					, browseExplorer(178, TId.WorkOrderAssignee, dsMB.Schema.T.WorkOrderAssignee)
					, browseExplorer(179, TId.WorkOrderStatus, dsMB.Schema.T.WorkOrderStateHistoryStatus)
					, browseExplorer(180, TId.BillableRequestor, dsMB.Schema.T.BillableRequestor)
					, browseExplorer(181, TId.ChargebackCategory, dsMB.Schema.T.ChargebackLineCategory)
					, browseExplorer(182, TId.ClosingCode, dsMB.Schema.T.CloseCode)
					, browseExplorer(183, TId.MiscellaneousCost, dsMB.Schema.T.MiscellaneousWorkOrderCost)
					, containerMenuItem(184, "Labor"
						, browseExplorer(185, TId.Employee, dsMB.Schema.T.Employee)
						, browseExplorer(186, TId.Trade, dsMB.Schema.T.Trade)
						, browseExplorer(187, TId.HourlyInside, dsMB.Schema.T.LaborInside)
						, browseExplorer(188, TId.HourlyOutside, dsMB.Schema.T.LaborOutside)
						, browseExplorer(189, TId.PerJobInside, dsMB.Schema.T.OtherWorkInside)
						, browseExplorer(190, TId.PerJobOutside, dsMB.Schema.T.OtherWorkOutside)
					)
					, browseExplorer(191, TId.Project, dsMB.Schema.T.Project)
					, browseExplorer(192, TId.WorkCategory, dsMB.Schema.T.WorkCategory)
					, browseExplorer(193, TId.ExpenseCategory, dsMB.Schema.T.WorkOrderExpenseCategory)
					, browseExplorer(194, TId.ExpenseModel, dsMB.Schema.T.WorkOrderExpenseModel)
					, browseExplorer(195, TId.WorkOrderPriority, dsMB.Schema.T.WorkOrderPriority)
				)
			#endregion
			#region Tasks
				, browseExplorer(222, TId.UnitMaintenancePlan, TISchedule.ScheduledWorkOrderBrowserTbl
					, browseExplorer(223, TId.Task, dsMB.Schema.T.WorkOrderTemplate)
					, browseExplorer(224, TId.MaintenanceTiming, dsMB.Schema.T.Schedule)
					, browseExplorer(225, TId.PurchaseOrderTemplate, dsMB.Schema.T.PurchaseOrderTemplate)
				)
			#endregion
			#region Units
				, browseExplorer(196, TId.Unit, TILocations.UnitBrowseTblCreator
					, browseExplorer(197, TId.AssetCode, dsMB.Schema.T.AssetCode)
					, browseExplorer(198, TId.ServiceContract, dsMB.Schema.T.ServiceContract)
					, browseExplorer(199, TId.Meter, TIUnit.MeterWithManualReadingBrowseTbl)
					, browseExplorer(200, TId.MeterClass, dsMB.Schema.T.MeterClass)
					, browseExplorer(201, TId.Part, dsMB.Schema.T.SparePart)
					, browseExplorer(202, TId.Ownership, dsMB.Schema.T.Ownership)
					, browseExplorer(203, TId.SpecificationForm, dsMB.Schema.T.SpecificationForm)
					, browseExplorer(204, TId.System, dsMB.Schema.T.SystemCode)
					, browseExplorer(205, TId.UnitCategory, dsMB.Schema.T.UnitCategory)
					, browseExplorer(206, TId.UnitUsage, dsMB.Schema.T.UnitUsage)
				)
			#endregion
			#region Items
				, browseExplorer(207, TId.Item, dsMB.Schema.T.Item
					, browseExplorer(208, TId.ItemAdjustmentCode, dsMB.Schema.T.ItemAdjustmentCode)
					, browseExplorer(209, TId.ItemCategory, dsMB.Schema.T.ItemCategory)
					, browseExplorer(210, TId.ItemIssueCode, dsMB.Schema.T.ItemIssueCode)
					, browseExplorer(211, TId.Storeroom, TILocations.PermanentStorageBrowseTblCreator)
					, browseExplorer(212, TId.StoreroomAssignment, TILocations.PermanentItemLocationBrowseTblCreator)
					, browseExplorer(213, TId.VoidCode, dsMB.Schema.T.VoidCode)
					)
			#endregion
			#region Purchase Orders
				, browseExplorer(214, TId.PurchaseOrder, dsMB.Schema.T.PurchaseOrder
					, browseExplorer(215, TId.PurchaseOrderAssignee, dsMB.Schema.T.PurchaseOrderAssignee)
					, browseExplorer(216, TId.Project, dsMB.Schema.T.Project)
					, browseExplorer(217, TId.PurchaseOrderCategory, dsMB.Schema.T.PurchaseOrderCategory)
					, browseExplorer(218, TId.PaymentTerm, dsMB.Schema.T.PaymentTerm)
					, browseExplorer(219, TId.ShippingMode, dsMB.Schema.T.ShippingMode)
					, browseExplorer(220, TId.PurchaseOrderStatus, dsMB.Schema.T.PurchaseOrderStateHistoryStatus)
					, browseExplorer(221, TId.MiscellaneousItem, dsMB.Schema.T.Miscellaneous)
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
		private MenuDef adminNode() {
			return browseExplorer(226, TId.Administration, TIGeneralMB3.AdministrationTblCreator, adminSubNodes());
		}
		private MenuDef[] adminSubNodes() {
			return new MenuDef[] {
				browseExplorer(227, TId.CompanyInformation, TIGeneralMB3.CompanyInformationTblCreator)
				, browseExplorer(228, TId.User, dsMB.Schema.T.User,
						browseExplorer(229, TId.SQLDatabaseLogin, TISecurity.ManageDatabaseLoginTblCreator),
						browseExplorer(230, TId.SQLDatabaseUser, TISecurity.ManageDatabaseCredentialTblCreator)
				)
				, browseExplorer(231, TId.SecurityRole, TISecurity.RoleBrowserTblCreator)
#if DEBUG
				, browseExplorer(232, "DEBUG: Settings", TIGeneralMB3.SettingsTblCreator)
#endif
				, browseExplorer(233, TId.License, dsMB.Schema.T.License)
				, browseExplorer(234, TId.DatabaseManagement, TIGeneralMB3.DatabaseManagementTblCreator)
				, browseExplorer(235, TId.Backup, dsMB.Schema.T.BackupFileName)
				, browseExplorer(236, TId.ExternalTag, dsMB.Schema.T.ExternalTag)
				, browseExplorer(237, TId.Accounting, TIGeneralMB3.AccountingTransactionDerivationsTblCreator
					, reportExplorer(238, TId.AccountingLedger, TIReports.AccountingLedgerReport, KB.K("Accounting transactions in Ledger format")))
				, browseExplorer(239, TId.MainBossService, TIMainBossService.MainBossServiceManageTblCreator
					, browseExplorer(240, "Configuration", dsMB.Schema.T.ServiceConfiguration)
					, browseExplorer(241, TId.EmailRequest, dsMB.Schema.T.EmailRequest)
#if DEBUG
					, browseExplorer(242, "DEBUG: Pending Requestor Acknowledgements", dsMB.Schema.T.RequestAcknowledgement)
					, browseExplorer(243, "DEBUG: Message Definitions", TIGeneralMB3.UserMessageKeyWithEditAbilityTblCreator)
#endif
				)
			};
		}

		#endregion
		#region Overall content
		public MenuDef[] MainContents {
			get {
				return new MenuDef[] {
					new BrowseMenuDef(Id(250), KB.TOControlPanel(TId.MainBossOverview), TIGeneralMB3.FindDelayedBrowseTbl(dsMB.Schema.T.DatabaseStatus)),
					assignmentNode(),
					requestNode(),
					woNode(),
					unitMaintenancePlanNode(),
					unitNode(),
					itemNode(),
					poNode(),
					definitionNode(),
					adminNode()
				};
			}
		}
		public MenuDef[] SoloContents {
			get {
				return new MenuDef[] {
					assignmentNode(),
					requestNode(),
					woNode(),
					unitMaintenancePlanNode(),
					unitNode(),
					itemNode(),
					poNode(),
					definitionNode(),
					adminNode()
				};
			}
		}
		private MenuDef NewsPanelMenuDef {
			get {
				return browseExplorer(244, TId.MainBossNewsPanel, TIGeneralMB3.NewsPanelTblCreator);
			}
		}
		private MenuDef[] AssignmentContents {
			get {
				return assignmentNode().subMenu;
			}
		}
		public MenuDef OverallContent() {
#if DEBUG
			DuplicateCatcher.Clear();
#endif
			// TODO: Right now both the local item and the children depend on the mode.
			// Ideally everyone would use MainContents; it and its children would contain mode dependencies.
			if (TIGeneralMB3.MainBossModeGroup.Enabled || TIGeneralMB3.RequestsModeGroup.Enabled)
				return browseExplorer(244, TId.MainBossNewsPanel, TIGeneralMB3.NewsPanelTblCreator, MainContents);
			else if (TIGeneralMB3.AssignmentsModeGroup.Enabled)
				return new BrowseMenuDef(Id(260), KB.TOControlPanel(TId.MyAssignmentOverview), TIGeneralMB3.AssignmentStatusTblCreator, AssignmentContents);
			else if (TIGeneralMB3.AdministrationModeGroup.Enabled)
				return new BrowseMenuDef(Id(270), KB.TOControlPanel(TId.Administration), TIGeneralMB3.AdministrationTblCreator, adminSubNodes());
			else if (TIGeneralMB3.SessionsModeGroup.Enabled)
				return new BrowseMenuDef(Id(280), KB.TOControlPanel(TId.Session), TIGeneralMB3.FindDelayedBrowseTbl(dsMB.Schema.T.Session));
			else 
				throw new System.InvalidOperationException(KB.I("ControlPanelLayout: unknown application mode"));
		}
		#endregion
	}
}
