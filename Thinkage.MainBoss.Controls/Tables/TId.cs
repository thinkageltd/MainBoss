using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Controls.Resources;

namespace Thinkage.MainBoss.Controls {
	public class TId : Tbl.TblIdentification {
		// This initializer exists to cause the Thinkage.MainBoss.Controls translation messages to be registered
		// for external applications that are using TId (TableObjects) in Tbls defined elsewhere to cause the translations
		// for those TIds to exist when the application starts.
		static TId() {
			KB.K(".");
		}
		/// <summary>
		/// Common function to create Tbl.TblIdentification objects, in proper translation context
		/// </summary>
		/// <param name="ident"></param>
		/// <returns></returns>
		protected TId([Context("TableObjects", Translatable = true)] string ident, System.Drawing.Icon primaryIcon = null, System.Drawing.Icon secondaryIcon = null)
			: base(ident, primaryIcon, secondaryIcon) {
		}
		protected TId(Key ident, System.Drawing.Icon primaryIcon = null, System.Drawing.Icon secondaryIcon = null)
			: base(ident, primaryIcon, secondaryIcon) {
		}
		protected TId([Context("TableObjects", Translatable = true)] string ident, System.Drawing.Image image)
			: base(ident, image) {
		}
		protected TId(Key ident, System.Drawing.Image image)
			: base(ident, image) {
		}
		#region Variants
		/// <summary>
		/// Variants are ComposedKeys in themselves, later used to make other ComposedKeys for labels, and tips
		/// </summary>
		private class TIdVariant : TId {
			protected TIdVariant(TId baseTId, [Thinkage.Libraries.Translation.Context("TablePhraseRoots", Translatable = true)] string composePhrase)
				: base(baseTId.Compose(composePhrase), baseTId.ImageWithTip?.Image) {
			}
		}
		private class ReportSummaryVariant : TIdVariant {
			public ReportSummaryVariant(TId baseTid)
				: base(baseTid, "{0} Summary") {
			}
		}
		private class ReportHistoryVariant : TIdVariant {
			public ReportHistoryVariant(TId baseTid)
				: base(baseTid, "{0} History") {
			}
		}
		private class ReportByAssigneeVariant : TIdVariant {
			public ReportByAssigneeVariant(TId baseTid)
				: base(baseTid, "{0} by Assignee") {
			}
		}
		private class ReportSingleVariant : TIdVariant {
			public ReportSingleVariant(TId baseTid)
				: base(baseTid, "Single {0}") {
			}
		}
		// Accessors
		public TId ReportSummary {
			get {
				if (pReportSummary == null)
					pReportSummary = new ReportSummaryVariant(this);
				return pReportSummary;
			}
		}
		private TId pReportSummary;
		public TId ReportHistory {
			get {
				if (pReportHistory == null)
					pReportHistory = new ReportHistoryVariant(this);
				return pReportHistory;
			}
		}
		private TId pReportHistory;
		public TId ReportByAssignee {
			get {
				if (pReportByAssignee == null)
					pReportByAssignee = new ReportByAssigneeVariant(this);
				return pReportByAssignee;
			}
		}
		private TId pReportByAssignee;
		public TId ReportSingle {
			get {
				if (pReportSingle == null)
					pReportSingle = new ReportSingleVariant(this);
				return pReportSingle;
			}
		}
		private TId pReportSingle;
		#endregion
#if DEBUG
		public static readonly TId MeterReadingPrediction = new TId("DEBUG: Meter Reading Prediction");
		public static readonly TId AcknowledgmentStatus = new TId("DEBUG: Acknowledgment Status");
#endif

		public static readonly TId AccessCode = new TId("Access Code");
		public static readonly TId Accounting = new TId("Accounting");
		public static readonly TId AccountingLedger = new TId("Accounting Ledger");
		public static readonly TId AccountingTransaction = new TId("Accounting Transaction");
		public static readonly TId ActualHourlyInside = new TId("Actual Hourly Inside", Images.LaborInside, Images.Actual);
		public static readonly TId ActualHourlyOutsideNoPO = new TId("Actual Hourly Outside (no PO)", Images.LaborOutside, Images.Actual);
		public static readonly TId ActualHourlyOutsideWithPO = new TId("Actual Hourly Outside (with PO)", Images.LaborOutside, Images.Actual);
		public static readonly TId ActualItem = new TId("Actual Item", Images.Item, Images.Actual);
		public static readonly TId ActualMiscellaneousCost = new TId("Actual Miscellaneous Cost", Images.MiscellaneousCost, Images.Actual);
		public static readonly TId ActualPerJobInside = new TId("Actual Per Job Inside", Images.OtherWorkInside, Images.Actual);
		// note that ActualPerJobOutside is same as ActualPerJobOutsideNoPO; use the latter
		public static readonly TId ActualPerJobOutsideNoPO = new TId("Actual Per Job Outside (no PO)", Images.OtherWorkOutside, Images.Actual);
		public static readonly TId ActualPerJobOutsideWithPO = new TId("Actual Per Job Outside (with PO)", Images.OtherWorkOutside, Images.Actual);
		public static readonly TId Administration = new TId("Administration");
		public static readonly TId AllowedShipToLocation = new TId("Allowed Ship To Location", Images.PostalAddress);
		public static readonly TId AssetCode = new TId("Asset Code");
		public static readonly TId AssignedGroup = new TId("Assigned Group");
		public static readonly TId Attachment = new TId("Attachment");
		public static readonly TId Backup = new TId("Backup");
		public static readonly TId BillableRequestor = new TId("Billable Requestor", Images.RequestBillable);
		public static readonly TId CalendarPeriod = new TId("Calendar Period");
		public static readonly TId Chargeback = new TId("Chargeback", Images.Chargeback);
		public static readonly TId ChargebackActivity = new TId("Chargeback Activity");
		public static readonly TId ChargebackCategory = new TId("Chargeback Category");
		public static readonly TId ClosedPurchaseOrder = new TId("Closed Purchase Order", Images.PurchaseOrderClosed);
		public static readonly TId ClosedRequest = new TId("Closed Request", Images.RequestClosed);
		public static readonly TId ClosedWorkOrder = new TId("Closed Work Order", Images.WorkOrderClosed);
		public static readonly TId ClosingCode = new TId("Closing Code");
		public static readonly TId CompanyInformation = new TId("Company Information");
		public static readonly TId Contact = new TId("Contact");
		public static readonly TId ContactFunction = new TId("Contact Function");
		public static readonly TId ContactRelation = new TId("Contact Relation");
		public static readonly TId CorrectionofActualHourlyInside = new TId("Correction of Actual Hourly Inside", Images.LaborInside, Images.Correction);
		public static readonly TId CorrectionofActualHourlyOutsideNoPO = new TId("Correction of Actual Hourly Outside (no PO)", Images.LaborOutside, Images.Correction);
		public static readonly TId CorrectionofActualHourlyOutsideWithPO = new TId("Correction of Actual Hourly Outside (with PO)", Images.LaborOutside, Images.Correction);
		public static readonly TId CorrectionofActualItem = new TId("Correction of Actual Item", Images.Item, Images.Correction);
		public static readonly TId CorrectionofActualMiscellaneousCost = new TId("Correction of Actual Miscellaneous Cost", Images.MiscellaneousCost, Images.Correction);
		public static readonly TId CorrectionofActualPerJobInside = new TId("Correction of Actual Per Job Inside", Images.OtherWorkInside, Images.Correction);
		public static readonly TId CorrectionofActualPerJobOutsideNoPO = new TId("Correction of Actual Per Job Outside (no PO)", Images.OtherWorkOutside, Images.Correction);
		public static readonly TId CorrectionofActualPerJobOutsideWithPO = new TId("Correction of Actual Per Job Outside (with PO)", Images.OtherWorkOutside, Images.Correction);
		public static readonly TId CorrectionofChargebackActivity = new TId("Correction of Chargeback Activity", Images.Chargeback, Images.Correction);
		public static readonly TId CorrectionofItemIssue = new TId("Correction of Item Issue", Images.Item, Images.Correction);
		public static readonly TId CorrectionofItemTransfer = new TId("Correction of Item Transfer", Images.Item, Images.Correction);
		public static readonly TId CorrectionofItemTransferFrom = new TId("Correction of Item Transfer From", Images.Item, Images.Correction);
		public static readonly TId CorrectionofItemTransferTo = new TId("Correction of Item Transfer To", Images.Item, Images.Correction);
		public static readonly TId CorrectionofReceiveItemNoPO = new TId("Correction of Receive Item (no PO)", Images.Item, Images.Correction);
		public static readonly TId CorrectionofReceiveItemWithPO = new TId("Correction of Receive Item (with PO)", Images.Item, Images.Correction);
		public static readonly TId CorrectionofReceiveMiscellaneous = new TId("Correction of Receive Miscellaneous", Images.Miscellaneous, Images.Correction);
		public static readonly TId CostCenter = new TId("Cost Center");
		public static readonly TId DatabaseManagement = new TId("Database Management");
		public static readonly TId DemandHourlyInside = new TId("Demand Hourly Inside", Images.LaborInside, Images.Demand);
		public static readonly TId DemandHourlyOutside = new TId("Demand Hourly Outside", Images.LaborOutside, Images.Demand);
		public static readonly TId DemandItem = new TId("Demand Item", Images.Item, Images.Demand);
		public static readonly TId DemandMiscellaneousCost = new TId("Demand Miscellaneous Cost", Images.MiscellaneousCost, Images.Demand);
		public static readonly TId DemandPerJobInside = new TId("Demand Per Job Inside", Images.OtherWorkInside, Images.Demand);
		public static readonly TId DemandPerJobOutside = new TId("Demand Per Job Outside", Images.OtherWorkOutside, Images.Demand);
		public static readonly TId DraftPurchaseOrder = new TId("Draft Purchase Order", Images.PurchaseOrderDraft);
		public static readonly TId DraftWorkOrder = new TId("Draft Work Order", Images.WorkOrderDraft);
		public static readonly TId EmailPart = new TId("Email Part");
		public static readonly TId EmailRequest = new TId("Email Request");
		public static readonly TId Employee = new TId("Employee", Images.Employee);
		public static readonly TId ExpenseCategory = new TId("Expense Category");
		public static readonly TId ExpenseMapping = new TId("Expense Mapping");
		public static readonly TId ExpenseModel = new TId("Expense Model");
		public static readonly TId ExternalTag = new TId("External Tag");
		public static readonly TId GenerationDetail = new TId("Generation Detail", Images.PMGenerationDetail);
		public static readonly TId GenerationDetailSchedulingTerminated = new TId("The next Work Order isn't needed until after the current scheduling period", Images.DispositionAfterSchedulingPeriod);
		public static readonly TId GenerationDetailDeferred = new TId("Work was deferred to a later date", Images.Deferred);
		public static readonly TId GenerationDetailPredictedWorkOrder = new TId("The Start Date for the work was uncertain so no Work Order is made", Images.WorkOrder, Images.Predicted);
		public static readonly TId GenerationDetailError = new TId("An error occurred while scheduling the work", Images.Error);
		public static readonly TId GenerationDetailErrorMakingWorkOrder = new TId("An error occurred creating the Work Order", Images.WorkOrder, Images.Error);
		public static readonly TId GenerationDetailCommittedMakeWorkOrder = new TId("A Work Order was generated", Images.WorkOrder, Images.Successful);
		public static readonly TId GenerationDetailMakeUnscheduledWorkOrder = new TId("An unplanned Work Order was created directly from the Unit Maintenance Plan", Images.WorkOrder, Images.Unscheduled);
		public static readonly TId GenerationDetailInhibited = new TId("The scheduled Work Start Date fell on a date inhibited by the Schedule", Images.Inhibited);
		public static readonly TId GenerationDetailManualReschedule = new TId("A new Scheduling Basis was manually entered", Images.ManualReschedule);
		public static readonly TId GenerationDetailCommittedMakeSharedWorkOrder = new TId("A single Work Order was generated for several Schedule Dates that have been deferred to the same date", Images.WorkOrder, Images.Shared);
		public static readonly TId GenerationDetailMakeWorkOrder = new TId("A Work Order will be generated", Images.WorkOrder, Images.Successful);
		public static readonly TId GenerationDetailMakeSharedWorkOrder = new TId("A single Work Order will be generated for several Schedule Dates that have been deferred to the same date", Images.WorkOrder, Images.Shared);
		public static readonly TId GenerationDetailWithContainers = new TId("Generation Detail with containers", Images.PMGenerationDetail);
		public static readonly TId HourlyInside = new TId("Hourly Inside", Images.LaborInside);
		public static readonly TId HourlyOutside = new TId("Hourly Outside", Images.LaborOutside);
		public static readonly TId InProgressRequest = new TId("In Progress Request", Images.RequestInProgress);
		public static readonly TId InProgressRequestInProgressDateHistogram = new TId("In Progress Request In Progress Date Histogram", Images.RequestInProgress);
		public static readonly TId InProgressRequestWithLinkedWorkOrder = new TId("In Progress Request with linked Work Order", Images.RequestInProgress);
		public static readonly TId InProgressRequestWithNoLinkedWorkOrder = new TId("In Progress Request with no linked Work Order", Images.RequestInProgress);
		public static readonly TId IssuedPurchaseOrder = new TId("Issued Purchase Order", Images.PurchaseOrder);
		public static readonly TId Item = new TId("Item", Images.Item);
		public static readonly TId ItemActivity = new TId("Item Activity");
		public static readonly TId ItemAdjustment = new TId("Item Adjustment", Images.Item, Images.Adjustment);
		public static readonly TId ItemAdjustmentCode = new TId("Item Adjustment Code");
		public static readonly TId ItemCategory = new TId("Item Category");
		public static readonly TId ItemIssue = new TId("Item Issue", Images.Item, Images.Issue);
		public static readonly TId ItemIssueCode = new TId("Item Issue Code");
		public static readonly TId ItemLocation = new TId("Storage Assignment");
		public static readonly TId ItemOnOrder = new TId("Item On Order");
		public static readonly TId ItemPricing = new TId("Item Pricing", Images.PriceQuote);
		public static readonly TId ItemReceiving = new TId("Item Receiving");
		public static readonly TId ItemRestocking = new TId("Item Restocking");
		public static readonly TId ItemsInTemporaryStorage = new TId("Items in Temporary Storage");
		public static readonly TId ItemTransfer = new TId("Item Transfer", Images.Item, Images.Transfer);
		public static readonly TId ItemTransferFrom = new TId("Item Transfer From", Images.Item, Images.Transfer);
		public static readonly TId ItemTransferTo = new TId("Item Transfer To", Images.Item, Images.Transfer);
		public static readonly TId ItemUsageAsParts = new TId("Item Usage as Parts");
		public static readonly TId LaborForecast = new TId("Labor Forecast");
		public static readonly TId License = new TId("License");
		public static readonly TId LinkedWorkOrder = new TId("Linked Work Order");
		public static readonly TId Location = new TId("Location", Images.PostalAddress);
		public static readonly TId MainBossService = new TId("MainBoss Service");
		public static readonly TId MainBossServiceConfiguration = new TId("MainBoss Service Configuration");
		public static readonly TId MainBossOverview = new TId("MainBoss Overview");
		public static readonly TId MaintenanceForecast = new TId("Maintenance Forecast");
		public static readonly TId MaintenanceHistory = new TId("Maintenance History");
		public static readonly TId MaintenanceTiming = new TId("Maintenance Timing");
		public static readonly TId ManualMeterReading = new TId("Manual Meter Reading");
		public static readonly TId MaterialForecast = new TId("Material Forecast");
		public static readonly TId MainBossNewsPanel = new TId("MainBoss News");
		public static readonly TId Message = new TId("Message");
		public static readonly TId MessageTranslation = new TId("Message Translation");
		public static readonly TId Meter = new TId("Meter");
		public static readonly TId MeterClass = new TId("Meter Class");
		public static readonly TId MeterPeriod = new TId("Meter Period");
		public static readonly TId MeterReading = new TId("Meter Reading");
		public static readonly TId MiscellaneousCost = new TId("Miscellaneous Cost", Images.MiscellaneousCost);
		public static readonly TId MiscellaneousItem = new TId("Miscellaneous Item", Images.Miscellaneous);
		public static readonly TId MyAssignmentOverview = new TId("My Assignment Overview");
		public static readonly TId NewRequest = new TId("New Request", Images.RequestNew);
		public static readonly TId NonTemporaryLocation = new TId("Non-Temporary Location", Images.PostalAddress);
		public static readonly TId OpenWorkOrder = new TId("Open Work Order", Images.WorkOrderOpen);
		public static readonly TId OpenWorkOrderbyPriority = new TId("Open Work Order by Priority");
		public static readonly TId OpenWorkOrderbyStatus = new TId("Open Work Order by Status");
		public static readonly TId OverdueWorkOrder = new TId("Overdue Work Order", Images.WorkOrder);
		public static readonly TId Ownership = new TId("Ownership");
		public static readonly TId Part = new TId("Part");
		public static readonly TId PaymentTerm = new TId("Payment Term");
		public static readonly TId PerJobInside = new TId("Per Job Inside", Images.OtherWorkInside);
		public static readonly TId PerJobOutside = new TId("Per Job Outside", Images.OtherWorkOutside);
		public static readonly TId Period = new TId("Period");
		public static readonly TId Permission = new TId("Permission");
		public static readonly TId PhysicalCount = new TId("Physical Count", Images.Item, Images.CountValue);	// TODO (icons): Why isn't Item part of CountValue image??
		public static readonly TId PlannedMaintenanceBatch = new TId("Planned Maintenance Batch", Images.Folder);
		public static readonly TId PostalAddress = new TId("Postal Address", Images.PostalAddress);
		public static readonly TId Project = new TId("Project");
		public static readonly TId PurchaseHourlyOutside = new TId("Purchase Hourly Outside", Images.LaborOutside);
		public static readonly TId PurchaseItem = new TId("Purchase Item", Images.Item);
		public static readonly TId PurchaseMiscellaneousItem = new TId("Purchase Miscellaneous Item", Images.Miscellaneous);
		public static readonly TId PurchaseOrder = new TId("Purchase Order", Images.PurchaseOrder);
		public static readonly TId PurchaseOrderCategory = new TId("Purchase Order Category");
		public static readonly TId PurchaseOrderAssignee = new TId("Purchase Order Assignee", Images.PurchaseOrder, Images.Contact);
		public static readonly TId PurchaseOrderAssignment = new TId("Purchase Order Assignment");
		public static readonly TId PurchaseOrderAssignmentByAssignee = new TId("Purchase Order Assignment By Assignee");
		public static readonly TId PurchaseOrderComment = new TId("Purchase Order Comment");
		public static readonly TId PurchaseOrderLine = new TId("Purchase Order Line");
		public static readonly TId PurchaseOrderState = new TId("Purchase Order State");
		public static readonly TId PurchaseOrderStateHistory = new TId("Purchase Order State History");
		public static readonly TId PurchaseOrderStatus = new TId("Purchase Order Status");
		public static readonly TId PurchaseOrderStatusStatistics = new TId("Purchase Order Status Statistics");
		public static readonly TId PurchaseOrderTemplate = new TId("Purchase Order Template");
		public static readonly TId PurchaseOrderTemplateLine = new TId("Purchase Order Template Line");
		public static readonly TId PurchasePerJobOutside = new TId("Purchase Per Job Outside", Images.OtherWorkOutside);
		public static readonly TId PurchaseTemplateHourlyOutside = new TId("Purchase Template Hourly Outside", Images.LaborOutside);
		public static readonly TId PurchaseTemplateItem = new TId("Purchase Template Item", Images.Item);
		public static readonly TId PurchaseTemplateMiscellaneousItem = new TId("Purchase Template Miscellaneous Item", Images.Miscellaneous);
		public static readonly TId PurchaseTemplatePerJobOutside = new TId("Purchase Template Per Job Outside", Images.OtherWorkOutside);
		public static readonly TId Receipt = new TId("Receipt", Images.Receipt);
		public static readonly TId ReceiptActivity = new TId("Receipt Activity");
		public static readonly TId ReceiveItemNoPO = new TId("Receive Item (no PO)", Images.Item, Images.PostalAddress);
		public static readonly TId ReceiveItemWithPO = new TId("Receive Item (with PO)", Images.Item, Images.PostalAddress);
		public static readonly TId ReceiveMiscellaneous = new TId("Receive Miscellaneous", Images.Miscellaneous, Images.PostalAddress);
		public static readonly TId Relationship = new TId("Relationship");
		public static readonly TId Request = new TId("Request", Images.Request);
		public static readonly TId RequestAssignee = new TId("Request Assignee", Images.Request, Images.Contact);
		public static readonly TId RequestAssignment = new TId("Request Assignment");
		public static readonly TId RequestAssignmentByAssignee = new TId("Request Assignment By Assignee");
		public static readonly TId RequestPriority = new TId("Request Priority");
		public static readonly TId RequestState = new TId("Request State");
		public static readonly TId RequestStateHistory = new TId("Request State History");
		public static readonly TId RequestStatus = new TId("Request Status");
		public static readonly TId RequestedWorkOrder = new TId("Requested Work Order");
		public static readonly TId Requestor = new TId("Requestor", Images.Request);
		public static readonly TId RequestorComment = new TId("Requestor Comment");
		public static readonly TId ResourceGroup = new TId("Resource Group");
		public static readonly TId SchedulingBasis = new TId("Scheduling Basis", Images.Folder);
		public static readonly TId SecurityRole = new TId("MainBoss-defined Security Role", Images.SecurityRole);
		public static readonly TId CustomSecurityRole = new TId("Custom Security Role", Images.CustomSecurityRole);
		public static readonly TId ServiceContract = new TId("Service Contract");
		public static readonly TId ServiceLog = new TId("Service Log", Images.Error);
		public static readonly TId ServiceLogError = new TId("Error", Images.eventlogError);
		public static readonly TId ServiceLogWarning = new TId("Warning", Images.eventlogWarn);
		public static readonly TId ServiceLogInformation = new TId("Information", Images.eventlogInfo);
		public static readonly TId ServiceLogActivity = new TId("Activity", Images.eventlogInfo);
		public static readonly TId ServiceLogTrace = new TId("Trace", Images.eventlogInfo);
		public static readonly TId Session = new TId("Session");
		public static readonly TId ShippingMode = new TId("Shipping Mode");
		public static readonly TId Specification = new TId("Specification");
		public static readonly TId SpecificationData = new TId("Specification Data");
		public static readonly TId SpecificationForm = new TId("Specification Form");
		public static readonly TId SpecificationFormField = new TId("Specification Form Field");
		public static readonly TId StorageAssignment = new TId("Storage Assignment");
		public static readonly TId Storeroom = new TId("Storeroom", Images.PermanentStorage);
		public static readonly TId StoreroomAssignment = new TId("Storeroom Assignment", Images.Item);
		public static readonly TId StoreroomOrTemporaryStorageAssignment = new TId("Storeroom or Temporary Storage Assignment");
		public static readonly TId SubLocation = new TId("Sub Location", Images.PlainRelativeLocation);
		public static readonly TId System = new TId("System");
		public static readonly TId Task = new TId("Task", Images.Task);
		public static readonly TId TaskDemandHourlyInside = new TId("Task Demand Hourly Inside", Images.LaborInside, Images.Demand);
		public static readonly TId TaskDemandHourlyOutside = new TId("Task Demand Hourly Outside", Images.LaborOutside, Images.Demand);
		public static readonly TId TaskDemandItem = new TId("Task Demand Item", Images.Item, Images.Demand);
		public static readonly TId TaskDemandMiscellaneousCost = new TId("Task Demand Miscellaneous Cost", Images.MiscellaneousCost, Images.Demand);
		public static readonly TId TaskDemandPerJobInside = new TId("Task Demand Per Job Inside", Images.OtherWorkInside, Images.Demand);
		public static readonly TId TaskDemandPerJobOutside = new TId("Task Demand Per Job Outside", Images.OtherWorkOutside, Images.Demand);
		public static readonly TId TaskFromWorkOrder = new TId("Task from Work Order", Images.Task);
		public static readonly TId TaskItem = new TId("Task Item");
		public static readonly TId TaskInside = new TId("Task Inside");
		public static readonly TId TaskMiscellaneousExpense = new TId("Task Miscellaneous Expense");
		public static readonly TId TaskOutside = new TId("Task Outside");
		public static readonly TId TaskResource = new TId("Task Resource");
		public static readonly TId TaskSpecialization = new TId("Task Specialization", Images.Task);
		public static readonly TId TaskStoreroomorTemporaryStorageAssignment = new TId("Task Storeroom or Temporary Storage Assignment");
		public static readonly TId TaskTemporaryStorage = new TId("Task Temporary Storage", Images.TemporaryStorage, Images.Task);
		public static readonly TId TaskTemporaryStorageAssignment = new TId("Task Temporary Storage Assignment", Images.Item);
		public static readonly TId TaskToPurchaseOrderTemplateLinkage = new TId("Task to Purchase Order Template Linkage", Images.Task, Images.PurchaseOrder);
		public static readonly TId TemplateTemporaryStorage = new TId("Template Temporary Storage", Images.TemporaryStorage);
		public static readonly TId TemporaryStorage = new TId("Temporary Storage", Images.TemporaryStorage);
		public static readonly TId TemporaryStorageAndItem = new TId("Temporary Storage and Item");
		public static readonly TId TemporaryStorageAssignment = new TId("Temporary Storage Assignment", Images.Item);
		public static readonly TId TimingPeriod = new TId("Timing Period");
		public static readonly TId Trade = new TId("Trade", Images.Trade);
		public static readonly TId Unit = new TId("Unit", Images.Unit);
		public static readonly TId UnassignedRequest = new TId("Unassigned Request", Images.Request);
		public static readonly TId UnassignedPurchaseOrder = new TId("Unassigned Purchase Order", Images.PurchaseOrder);
		public static readonly TId UnassignedWorkOrder = new TId("Unassigned Work Order", Images.WorkOrder);
		public static readonly TId UnitCategory = new TId("Unit Category");
		public static readonly TId UnitContactRelationship = new TId("Unit Contact Relationship");
		public static readonly TId UnitMaintenancePlan = new TId("Unit Maintenance Plan", Images.Task, Images.PMGenerationDetail);
		public static readonly TId UnitOfMeasure = new TId("Unit of Measure");
		public static readonly TId UnitRelatedContact = new TId("Unit Related Contact");
		public static readonly TId UnitRelatedUnit = new TId("Unit Related Unit");
		public static readonly TId UnitRelation = new TId("Unit Relation");
		public static readonly TId UnitReplacementForecast = new TId("Unit Replacement Forecast");
		public static readonly TId UnitServiceContract = new TId("Unit Service Contract");
		public static readonly TId UnitUnitRelationship = new TId("Unit Unit Relationship");
		public static readonly TId UnitUsage = new TId("Unit Usage");
		public static readonly TId User = new TId("MainBoss User", Images.User);
		public static readonly TId UserSecurityRole = new TId("User Security Role");
		public static readonly TId Vendor = new TId("Vendor", Images.Vendor);
		public static readonly TId VendorCategory = new TId("Vendor Category");
		public static readonly TId VoidCode = new TId("Void Code");
		public static readonly TId VoidPhysicalCount = new TId("Void Physical Count", Images.Item, Images.VoidCountValue);	// TODO (icons): Why include the Item image? VoidCountValue should include it already????
		public static readonly TId VoidPurchaseOrder = new TId("Void Purchase Order", Images.PurchaseOrderVoid);
		public static readonly TId VoidWorkOrder = new TId("Void Work Order", Images.WorkOrderVoid);
		public static readonly TId WorkCategory = new TId("Work Category");
		public static readonly TId WorkOrder = new TId("Work Order", Images.WorkOrder);
		public static readonly TId WorkOrderAssignee = new TId("Work Order Assignee", Images.WorkOrder, Images.Contact);
		public static readonly TId WorkOrderAssignment = new TId("Work Order Assignment");
		public static readonly TId WorkOrderAssignmentByAssignee = new TId("Work Order Assignment By Assignee");
		public static readonly TId WorkOrderComment = new TId("Work Order Comment");
		public static readonly TId WorkOrderEndDateHistogram = new TId("Work Order End Date Histogram");
		public static readonly TId WorkOrderFromTask = new TId("Work Order From Task");
		public static readonly TId WorkOrderHourlyInside = new TId("Work Order Hourly Inside");
		public static readonly TId WorkOrderHourlyInsideActual = new TId("Work Order Hourly Inside Actual");
		public static readonly TId WorkOrderHourlyOutside = new TId("Work Order Hourly Outside");
		public static readonly TId WorkOrderHourlyOutsidePO = new TId("Work Order Hourly Outside (with PO)");
		public static readonly TId WorkOrderHourlyOutsidePOActual = new TId("Work Order Hourly Outside Actual (with PO)");
		public static readonly TId WorkOrderHourlyOutsideNonPO = new TId("Work Order Hourly Outside (no PO)");
		public static readonly TId WorkOrderHourlyOutsideNonPOActual = new TId("Work Order Hourly Outside Actual (no PO)");
		public static readonly TId WorkOrderItem = new TId("Work Order Item");
		public static readonly TId WorkOrderItemActual = new TId("Work Order Item Actual");
		public static readonly TId WorkOrderInside = new TId("Work Order Inside");
		public static readonly TId WorkOrderMeterReading = new TId("Work Order Meter Reading");
		public static readonly TId WorkOrderMiscellaneousExpense = new TId("Work Order Miscellaneous Expense");
		public static readonly TId WorkOrderMiscellaneousExpenseActual = new TId("Work Order Miscellaneous Expense Actual");
		public static readonly TId WorkOrderOutside = new TId("Work Order Outside");
		public static readonly TId WorkOrderPerJobInside = new TId("Work Order Per Job Inside");
		public static readonly TId WorkOrderPerJobInsideActual = new TId("Work Order Per Job Inside Actual");
		public static readonly TId WorkOrderPerJobOutside = new TId("Work Order Per Job Outside");
		public static readonly TId WorkOrderPerJobOutsidePO = new TId("Work Order Per Job Outside (with PO)");
		public static readonly TId WorkOrderPerJobOutsidePOActual = new TId("Work Order Per Job Outside Actual (with PO)");
		public static readonly TId WorkOrderPerJobOutsideNonPO = new TId("Work Order Per Job Outside (no PO)");
		public static readonly TId WorkOrderPerJobOutsideNonPOActual = new TId("Work Order Per Job Outside Actual (no PO)");
		public static readonly TId WorkOrderPriority = new TId("Work Order Priority");
		public static readonly TId WorkOrderPurchaseOrder = new TId("Work Order Purchase Order", Images.WorkOrder, Images.PurchaseOrder);
		public static readonly TId WorkOrderRequestTransition = new TId("Work Order Request Transition");
		public static readonly TId WorkOrderResource = new TId("Work Order Resource");
		public static readonly TId WorkOrderResourceDemand = new TId("Work Order Resource Demand");
		public static readonly TId WorkOrderResourceActual = new TId("Work Order Resource Actual");
		public static readonly TId WorkOrderState = new TId("Work Order State");
		public static readonly TId WorkOrderStateHistory = new TId("Work Order State History");
		public static readonly TId WorkOrderStatistics = new TId("Work Order Statistics");
		public static readonly TId WorkOrderStatus = new TId("Work Order Status");
		public static readonly TId WorkOrderTemporaryStorage = new TId("Work Order Temporary Storage");
		// The set of 'other' table objects for MainBoss.
		public static readonly TId UnitValue = new TId("Unit Value"); // TODO: This should go away ?
		public static readonly TId Labor = new TId("Labor");
		public static readonly TId OtherWork = new TId("Other Work");

		public static readonly TId InProgressRequestByStatus = new TId("In Progress Request by Status");
		public static readonly TId InProgressRequestByPriority = new TId("In Progress Request by Priority");
		public static readonly TId UnplannedMaintenanceWorkOrder = new TId("Unplanned Maintenance Work Order", Images.WorkOrder);
		public static readonly TId PendingAcknowledgment = new TId("Pending Acknowledgment");
		public static readonly TId Organization = new TId("Organization");
		public static readonly TId DefaultOrganization = new TId("Default Organization", Libraries.Presentation.MSWindows.Resources.Images.FlaggedGreenBitmap);
		public static readonly TId SQLServer = new TId("SQL Server");
		public static readonly TId SQLDatabase = new TId("SQL Database");
		public static readonly TId SQLDatabaseUser = new TId("SQL Database User");
		public static readonly TId SQLDatabaseLogin = new TId("SQL Database Login");
		public static readonly TId ActiveFilter = new TId("Active Filter");
		// Report only
		public static readonly TId WOChartCountByCreatedDate = new TId("Work Order Count by Creation Date");
		public static readonly TId WOChartCountByOpenedDate = new TId("Work Order Count by Opened Date");
		public static readonly TId WOChartCountByEndedDate = new TId("Work Order Count by Ended Date");
		public static readonly TId WOChartAverageDuration = new TId("Average Work Order Duration by Grouping");
		public static readonly TId WOChartTotalDuration = new TId("Total Work Order Duration by Grouping");
		public static readonly TId WOChartLifetime = new TId("Work Order Lifetime");
		public static readonly TId WOChartCount = new TId("Work Order Count by Grouping");
		public static readonly TId WOChartAverageDowntime = new TId("Average Work Order Downtime by Grouping");
		public static readonly TId WOChartTotalDowntime = new TId("Total Work Order Downtime by Grouping");
		public static readonly TId WOChartCostsByResourceType = new TId("Costs by Resource Type");
		public static readonly TId WOChartCosts = new TId("Costs by Grouping");
		public static readonly TId WOChartLaborTime = new TId("Labor Time by Grouping");
		public static readonly TId WOChartCostsByTrade = new TId("Costs by Trade");
		public static readonly TId WOChartLaborTimeByTrade = new TId("Labor Time by Trade");
		public static readonly TId WOChartCostsByEmployee = new TId("Costs by Employee");
		public static readonly TId WOChartLaborTimeByEmployee = new TId("Labor Time by Employee");
		public static readonly TId WOChartCostsByVendor = new TId("Costs by Vendor");
		public static readonly TId WOChartLaborTimeByVendor = new TId("Labor Time by Vendor");
		public static readonly TId WOChartStatus = new TId("Time in Work Order Status");
		public static readonly TId RequestChartCountByCreatedDate = new TId("Request Count by Creation Date");
		public static readonly TId RequestChartCountByInProgressDate = new TId("Request Count by In Progress Date");
		public static readonly TId RequestChartCountByEndedDate = new TId("Request Count by Ended Date");
		public static readonly TId RequestChartAverageDuration = new TId("Average Request Duration Per Grouping");
		public static readonly TId RequestChartLifetime = new TId("Request Lifetime");
		public static readonly TId RequestChartCount = new TId("Request Count by Grouping");
		public static readonly TId RequestChartStatus = new TId("Time in Request Status");
		public static readonly TId POChartCountByCreatedDate = new TId("Purchase Order Count by Creation Date");
		public static readonly TId POChartCountByIssuedDate = new TId("Purchase Order Count by Issued Date");
		public static readonly TId POChartCountByEndedDate = new TId("Purchase Order Count by Ended Date");
		public static readonly TId POChartAverageDuration = new TId("Average Purchase Order Duration Per Grouping");
		public static readonly TId POChartLifetime = new TId("Purchase Order Lifetime");
		public static readonly TId POChartCount = new TId("Purchase Order Count by Grouping");
		public static readonly TId POChartStatus = new TId("Time in Purchase Order Status");
		public static readonly TId Waybill = new TId("Waybill");
	}
}
