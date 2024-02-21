using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	#region KnownIds
	/// <summary>
	/// For state transitions, security roles (well known ones) and other records we want constant static Guids to represent the Id fields for those records
	/// </summary>
	public static class KnownIds {
		// Well known internal generated Ids from views for driving UI
		public static readonly Guid WorkOrderGroupNameProviderUnspecifiedInsideAgentId = new Guid(KB.I("40000000-0000-0000-0000-000000000004"));
		public static readonly Guid WorkOrderGroupNameProviderUnspecifiedOutsideAgentId = new Guid(KB.I("50000000-0000-0000-0000-000000000005"));
		public static readonly Guid WorkOrderGroupNameProviderUnspecifiedTradeId = new Guid(KB.I("60000000-0000-0000-0000-000000000006"));
		public static readonly Guid UnassignedID = new Guid(KB.I("A0000000-0000-0000-0000-00000000000A"));

		// Well known Id values used within the database
		const string Prefix = "4D61696E-426F-7373"; // 'MainBoss' in ASCII code hex
		private static Guid InternalGuid(int group, int n) {
			return new Guid(Strings.IFormat("{0}-{1:X4}-{2:X12}", Prefix, group, n));
		}
		// Convention naming Table_xxxx, where Table is the DBI_Table in which the Id is used for a particular record

		/// <summary>
		/// Known id's for role security; note the function mbfn_IsBuiltinRole has internal knowledge of the prefix value for Principal/Role Id.
		/// </summary>
		public static readonly Guid PrincipalId_All = InternalGuid(0, 0);
		public static readonly Guid RoleId_All = InternalGuid(1, 0);
		/// <summary>
		/// The following was used to transition from the OLD administrator/non-administrator upgrade steps. It was used to represent those users who did NOT have
		/// the administrator permission.  A later upgrade step (TODO) needs to transition any users linked to this role to whatever roles we have decided they transition to
		/// if they were not an administrator previously in 3.1
		/// </summary>
		public static readonly Guid PrincipalId_NonAdministrator = InternalGuid(0, 1);
		public static readonly Guid RoleId_NonAdministrator = InternalGuid(1, 1);
		/// <summary>
		/// For creation of known Ids from the rightset Id definitions
		/// </summary>
		/// <param name="id"></param>
		/// <param name="PrincipalId"></param>
		/// <returns></returns>
		public static Guid RoleAndPrincipalIDFromRoleRight(int id, out Guid PrincipalId) {
			PrincipalId = InternalGuid(0, id);
			return InternalGuid(1, id);
		}

		public static readonly Guid RequestStateNewId = InternalGuid(3, 0);
		public static readonly Guid RequestStateInProgressId = InternalGuid(3, 1);
		public static readonly Guid RequestStateClosedId = InternalGuid(3, 2);
		public static readonly Guid WorkOrderStateDraftId = InternalGuid(4, 0);
		public static readonly Guid WorkOrderStateOpenId = InternalGuid(4, 1);
		public static readonly Guid WorkOrderStateClosedId = InternalGuid(4, 2);
		public static readonly Guid WorkOrderStateVoidId = InternalGuid(4, 3);
		public static readonly Guid PurchaseOrderStateDraftId = InternalGuid(5, 0);
		public static readonly Guid PurchaseOrderStateIssuedId = InternalGuid(5, 1);
		public static readonly Guid PurchaseOrderStateClosedId = InternalGuid(5, 2);
		public static readonly Guid PurchaseOrderStateVoidId = InternalGuid(5, 3);
		public static readonly Guid OrganizationMasterRecordId = InternalGuid(6, 0);
	}
	#endregion
	#region Security
	/// <summary>
	/// The Root.Rights associated with this database
	/// </summary>
	public static class Root {
		public static readonly MB3RootRights Rights = new MB3RootRights();
		public static Libraries.DBILibrary.DBI_Database RightsSchema = dsMB.Schema;
	}

	#endregion

	#region ViewRecordTypes
	// Values are explicitly given here and must match the numbers in the
	// TableEnum column of the view
	// "NotSpecified" represents an unspecified record type
	public static class ViewRecordTypes {   // static to prevent instantiation

		public static EnumValueTextRepresentations IsPreventiveEnumText = EnumValueTextRepresentations.NewForBool(KB.K("Corrective"), null, KB.K("Preventive"), null);

		#region AccountingTransactionVariants
		public enum AccountingTransactionVariants {
			ActualItem = 0,
			ActualLaborInside = 1,
			ActualLaborOutsideNonPO = 2,
			ActualLaborOutsidePO = 3,
			ActualOtherWorkInside = 4,
			ActualOtherWorkOutsideNonPO = 5,
			ActualOtherWorkOutsidePO = 6,
			ActualMiscellaneousWorkOrderCost = 7,
			ChargebackLine = 8,
			ItemAdjustment = 9,
			ItemCountValue = 10,
			ItemCountValueVoid = 11,
			ItemIssue = 12,
			ItemTransfer = 13,
			ReceiveItemNonPO = 14,
			ReceiveItemPO = 15,
			ReceiveMiscellaneousPO = 16
		}
		private static readonly Thinkage.Libraries.Translation.Key[] TransactionTypeNameLabels = new Thinkage.Libraries.Translation.Key[]
			{
			KB.K("Actual Item"),
			KB.K("Actual Hourly Inside"),
			KB.K("Actual Hourly Outside (no PO)"),
			KB.K("Actual Hourly Outside (with PO)"),
			KB.K("Actual Per Job Inside"),
			KB.K("Actual Per Job Outside (no PO)"),
			KB.K("Actual Per Job Outside (with PO)"),
			KB.K("Actual Miscellaneous Cost"),
			KB.K("Chargeback Activity"),
			KB.K("Item Adjustment"),
			KB.K("Physical Count"),
			KB.K("Void Physical Count"),
			KB.K("Item Issue"),
			KB.K("Item Transfer"),
			KB.K("Receive Item (no PO)"),
			KB.K("Receive Item (with PO)"),
			KB.K("Receive Miscellaneous (with PO)")
			};
		private static readonly object[] TransactionTypeNameValues = new object[]
			{
			(int)AccountingTransactionVariants.ActualItem,
			(int)AccountingTransactionVariants.ActualLaborInside,
			(int)AccountingTransactionVariants.ActualLaborOutsideNonPO,
			(int)AccountingTransactionVariants.ActualLaborOutsidePO,
			(int)AccountingTransactionVariants.ActualOtherWorkInside,
			(int)AccountingTransactionVariants.ActualOtherWorkOutsideNonPO,
			(int)AccountingTransactionVariants.ActualOtherWorkOutsidePO,
			(int)AccountingTransactionVariants.ActualMiscellaneousWorkOrderCost,
			(int)AccountingTransactionVariants.ChargebackLine,
			(int)AccountingTransactionVariants.ItemAdjustment,
			(int)AccountingTransactionVariants.ItemCountValue,
			(int)AccountingTransactionVariants.ItemCountValueVoid,
			(int)AccountingTransactionVariants.ItemIssue,
			(int)AccountingTransactionVariants.ItemTransfer,
			(int)AccountingTransactionVariants.ReceiveItemNonPO,
			(int)AccountingTransactionVariants.ReceiveItemPO,
			(int)AccountingTransactionVariants.ReceiveMiscellaneousPO
			};
		public static Thinkage.Libraries.EnumValueTextRepresentations TransactionTypeNames = new Thinkage.Libraries.EnumValueTextRepresentations(TransactionTypeNameLabels, null, TransactionTypeNameValues);

		#endregion
		#region ContactFunctions
		public enum ContactFunctions {
			Contact = 0,
			Requestor = 1,
			BillableRequestor = 2,
			Employee = 3,
			SalesVendor = 4,
			ServiceVendor = 5,
			AccountsPayableVendor = 6,
			SalesServiceVendor = 7,
			SalesAccountsPayableVendor = 8,
			ServiceAccountsPayableVendor = 9,
			SalesServiceAccountsPayableVendor = 10,
			RequestAssignee = 11,
			WorkOrderAssignee = 12,
			PurchaseOrderAssignee = 13,
			User = 14
		}
		#endregion
		#region LocationDerivations
		public enum LocationDerivations {
			PostalAddress = 0,
			TemporaryStorage,
			Unit,
			PermanentStorage,
			PlainRelativeLocation,
			TemplateTemporaryStorage
		}
		#endregion
		#region LocationReport
		public enum LocationReport {
			PostalAddress = 0,
			TemporaryStorage,
			Unit,
			PermanentStorage,
			PlainRelativeLocation,
			TemplateTemporaryStorage
		}
		private static readonly Thinkage.Libraries.Translation.Key[] LocationReportProviderLabels = new Thinkage.Libraries.Translation.Key[]
			{
			KB.K("Postal Address"),
			KB.K("Temporary Storage"),
			KB.K("Unit"),
			KB.K("Storeroom"),
			KB.K("Sub Location"),
			KB.K("Template Temporary Storage"),
			};
		private static readonly object[] LocationReportProviderValues = new object[]
			{
			(int)LocationReport.PostalAddress,
			(int)LocationReport.TemporaryStorage,
			(int)LocationReport.Unit,
			(int)LocationReport.PermanentStorage,
			(int)LocationReport.PlainRelativeLocation,
			(int)LocationReport.TemplateTemporaryStorage
			};
		public static Thinkage.Libraries.EnumValueTextRepresentations LocationReportProvider = new Thinkage.Libraries.EnumValueTextRepresentations(LocationReportProviderLabels, null, LocationReportProviderValues);

		#endregion
		#region LocationDerivationsAndItemLocations
		/// <summary>
		/// Types of Locations
		/// </summary>
		public enum LocationDerivationsAndItemLocations {
			FirstLD,	// 0
			PostalAddress = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.PostalAddress,
			TemporaryStorage = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.TemporaryStorage,
			Unit = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.Unit,
			PermanentStorage = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.PermanentStorage,
			PlainRelativeLocation = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.PlainRelativeLocation,
			TemplateTemporaryStorage = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.TemplateTemporaryStorage,
			PermanentItemLocation,
			TemporaryItemLocation,
			TemporaryItemLocationTemplate	// 8
		}
		#endregion
		#region ItemActivity
		public enum ItemActivity {
			NotSpecified = -1,
			ItemCountValue = 0,
			VoidItemCountValue = 1,
			ItemAdjustment = 2,
			ItemIssue = 3,
			ItemTransferTo = 4,
			ItemTransferFrom = 5,
			ReceiveItemPO = 6,
			ReceiveItemNonPO = 7,
			ActualItem = 8,
			ItemIssueCorrection = 9,
			ReceiveItemPOCorrection = 10,
			ReceiveItemNonPOCorrection = 11,
			ActualItemCorrection = 12,
			ItemTransferToCorrection = 13,
			ItemTransferFromCorrection = 14,
			VoidedItemCountValue = 15
		}
		#endregion
		#region ItemActivityReport
		public enum ItemActivityReport {
			ItemCountValue = 0,
			ItemCountValueVoid = 1,
			ActualItem = 2,
			ItemAdjustment = 3,
			ItemIssue = 4,
			ItemTransferFrom = 5,
			ItemTransferTo = 6,
			ReceiveItemNonPO = 7,
			ReceiveItemPO = 8,
			VoidedItemCountValue = 9
		}
		private static readonly Thinkage.Libraries.Translation.Key[] ItemActivityReportProviderLabels = new Thinkage.Libraries.Translation.Key[]
			{
			KB.K("Physical Count"),
			KB.K("Void Physical Count"),
			KB.K("Actual"),
			KB.K("Adjustment"),
			KB.K("Issue"),
			KB.K("Transfer From"),
			KB.K("Transfer To"),
			KB.K("Receive (no PO)"),
			KB.K("Receive (with PO)"),
			KB.K("Voided Physical Count"),
			};
		private static readonly object[] ItemActivityReportProviderValues = new object[]
			{
			(int)ItemActivityReport.ItemCountValue,
			(int)ItemActivityReport.ItemCountValueVoid,
			(int)ItemActivityReport.ActualItem,
			(int)ItemActivityReport.ItemAdjustment,
			(int)ItemActivityReport.ItemIssue,
			(int)ItemActivityReport.ItemTransferFrom,
			(int)ItemActivityReport.ItemTransferTo,
			(int)ItemActivityReport.ReceiveItemNonPO,
			(int)ItemActivityReport.ReceiveItemPO,
			(int)ItemActivityReport.VoidedItemCountValue,
			};
		public static Thinkage.Libraries.EnumValueTextRepresentations ItemActivityReportProvider = new Thinkage.Libraries.EnumValueTextRepresentations(ItemActivityReportProviderLabels, null, ItemActivityReportProviderValues);

		#endregion
		#region ItemReceiving
		public enum ItemReceiving {
			NotSpecified = -1,
			ReceivePermanentPO = 0,
			ReceiveTemporaryPO = 1,
			ReceivePermanentNonPO = 2,
			ReceiveTemporaryNonPO = 3,
			ReceivePermanentPOCorrection = 4,
			ReceiveTemporaryPOCorrection = 5,
			ReceivePermanentNonPOCorrection = 6,
			ReceiveTemporaryNonPOCorrection = 7
		}
		#endregion
		#region ItemPricing
		public enum ItemPricing {
			NotSpecified = -1,
			PriceQuote = 0,
			ReceivePO = 1,
			ReceiveNonPO = 2
		}
		#endregion
		#region PurchaseOrderLines Names
		public enum PurchaseOrderLine {
			POLineItem,
			ReceiveItemPO,
			ReceiveItemPOCorrection,
			POLineLabor,
			ActualLaborOutsidePO,
			ActualLaborOutsidePOCorrection,
			POLineOtherWork,
			ActualOtherWorkOutsidePO,
			ActualOtherWorkOutsidePOCorrection,
			POLineMiscellaneous,
			ReceiveMiscellaneousPO,
			ReceiveMiscellaneousPOCorrection
		}
		#endregion
		#region ReceiptActivity  Names
		public enum ReceiptActivity {
			POLineItem,
			ReceiveItemPO,
			ReceiveItemPOCorrection,
			POLineLabor,
			ActualLaborOutsidePO,
			ActualLaborOutsidePOCorrection,
			POLineOtherWork,
			ActualOtherWorkOutsidePO,
			ActualOtherWorkOutsidePOCorrection,
			POLineMiscellaneous,
			ReceiveMiscellaneousPO,
			ReceiveMiscellaneousPOCorrection
		}
		#endregion
		#region ActiveTemporaryStorageWithItemAssignments
		public enum ActiveTemporaryStorageWithItemAssignments {
			TemporaryStorage,
			TemporaryItemLocation
		}
		#endregion

		#region WorkOrderItems
		public enum WorkOrderItems {
			Item,	// 0
			DemandItem	// 1
		}
		#endregion
		#region WorkOrderInside
		public enum WorkOrderInside {
			UnassignedTrade,
			Trade,
			DemandLaborInside,
			DemandOtherWorkInside,	// 3
		}
		#endregion
		#region WorkOrderOutside
		public enum WorkOrderOutside {
			UnassignedTrade,
			Trade,
			DemandLaborOutside,
			DemandOtherWorkOutside,
			POLineLabor,
			POLineOtherWork, 	// 5
		}
		#endregion

		#region WorkOrderMiscellaneous
		public enum WorkOrderMiscellaneous {
			MiscellaneousWorkOrderCost,
			DemandMiscellaneousWorkOrderCost
		}
		#endregion
		#region WorkOrderTemporaryStorage
		public enum WorkOrderTemporaryStorage {
			FirstLD = 0,
			PostalAddress = FirstLD + LocationDerivations.PostalAddress,
			TemporaryStorage = FirstLD + LocationDerivations.TemporaryStorage,
			Unit = FirstLD + LocationDerivations.Unit,
			PermanentStorage = FirstLD + LocationDerivations.PermanentStorage,
			PlainRelativeLocation = FirstLD + LocationDerivations.PlainRelativeLocation,
			TemplateTemporaryStorage = FirstLD + LocationDerivations.TemplateTemporaryStorage,
			TemporaryItemLocation	// 6
		}
		#endregion


		#region WorkOrderTemplateItems
		public enum WorkOrderTemplateItems {
			Item,	// 0
			DemandItemTemplate	// 1
		}
		#endregion
		#region WorkOrderTemplateInside
		public enum WorkOrderTemplateInside {
			UnassignedTrade,
			Trade,
			DemandLaborInsideTemplate,
			DemandOtherWorkInsideTemplate,	// 3
		}
		#endregion
		#region WorkOrderTemplateOutside
		public enum WorkOrderTemplateOutside {
			UnassignedTrade,
			Trade,
			DemandLaborOutsideTemplate,
			DemandOtherWorkOutsideTemplate,	// 3
		}
		#endregion

		#region WorkOrderTemplateMiscellaneous
		public enum WorkOrderTemplateMiscellaneous {
			MiscellaneousWorkOrderCost,
			DemandMiscellaneousWorkOrderCostTemplate
		}
		#endregion

		#region WorkOrderTemplateTemporaryStorage
		public enum WorkOrderTemplateStorage {
			FirstLD = 0,
			PostalAddress = FirstLD + LocationDerivations.PostalAddress,
			TemporaryStorage = FirstLD + LocationDerivations.TemporaryStorage,
			Unit = FirstLD + LocationDerivations.Unit,
			PermanentStorage = FirstLD + LocationDerivations.PermanentStorage,
			PlainRelativeLocation = FirstLD + LocationDerivations.PlainRelativeLocation,
			TemplateTemporaryStorage = FirstLD + LocationDerivations.TemplateTemporaryStorage,
			TemplateItemLocation // 6
		}
		#endregion

		#region ChargebackActivity
		public enum ChargebackActivity {
			NotSpecified = -1,
			Chargeback = 0,
			ChargebackCorrection = 1
		}
		#endregion
		#region PMGenerationDetailAndScheduledWorkOrderAndLocation types
		/// <summary>
		/// Types of records in the PMGenerationDetailAndScheduledWorkOrderAndLocation.
		/// We inherit the same values as LocationDerivations, plus one special value for the Task (a child of the unit)
		/// plus offset values for each PMType.
		/// </summary>
		public enum PMGenerationDetailAndScheduledWorkOrderAndLocation {
			FirstLD,	// 0
			PostalAddress = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.PostalAddress,
			TemporaryStorage = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.TemporaryStorage,
			Unit = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.Unit,
			PermanentStorage = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.PermanentStorage,
			PlainRelativeLocation = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.PlainRelativeLocation,
			TemplateTemporaryStorage = LocationDerivationsAndItemLocations.FirstLD + LocationDerivations.TemplateTemporaryStorage,
			ScheduledWorkOrder,
			DetailBaseIndex,	// 7
			DispositionAfterSchedulingPeriod = DetailBaseIndex + DatabaseEnums.PMType.SchedulingTerminated,
			Deferred = DetailBaseIndex + DatabaseEnums.PMType.Deferred,
			PredictedWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.PredictedWorkOrder,
			Error = DetailBaseIndex + DatabaseEnums.PMType.Error,
			ErrorMakingWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.ErrorMakingWorkOrder,
			MakeWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.MakeWorkOrder,
			MakeUnscheduledWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.MakeUnscheduledWorkOrder,
			Inhibited = DetailBaseIndex + DatabaseEnums.PMType.Inhibited,
			ManualReschedule = DetailBaseIndex + DatabaseEnums.PMType.ManualReschedule,
			MakeSharedWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.MakeSharedWorkOrder	// 16
		}
		#endregion
		#region CommittedPMGenerationDetailAndPMGenerationBatch types
		/// <summary>
		/// Types of records in the CommittedPMGenerationDetailAndPMGenerationBatch.
		/// We use one type for the Batch record plus offset values for each PMType.
		/// </summary>
		public enum CommittedPMGenerationDetailAndPMGenerationBatch {
			PMGenerationBatch = 0,
			DetailBaseIndex,
			DispositionAfterSchedulingPeriod = DetailBaseIndex + DatabaseEnums.PMType.SchedulingTerminated,
			Deferred = DetailBaseIndex + DatabaseEnums.PMType.Deferred,
			PredictedWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.PredictedWorkOrder,
			Error = DetailBaseIndex + DatabaseEnums.PMType.Error,
			ErrorMakingWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.ErrorMakingWorkOrder,
			MakeWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.MakeWorkOrder,
			MakeUnscheduledWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.MakeUnscheduledWorkOrder,
			Inhibited = DetailBaseIndex + DatabaseEnums.PMType.Inhibited,
			ManualReschedule = DetailBaseIndex + DatabaseEnums.PMType.ManualReschedule,
			MakeSharedWorkOrder = DetailBaseIndex + DatabaseEnums.PMType.MakeSharedWorkOrder
		}
		#endregion
		#region WorkOrderMeterTreeView
		public enum WorkOrderMeterTreeView {
			Meter,
			ManualReading,
			WorkOrderReading
		}
		#endregion
		#region PeriodicityInterval
		public enum PeriodicityInterval {
			DayInterval = 0,
			MonthInterval = 1,
			MeterInterval = 2
		}
		#endregion

		#region WorkOrderPurchaseOrderLinkage
		public enum WorkOrderPurchaseOrderLinkage {
			Explicit = 0,
			UsingLaborDemand = 1,
			UsingOtherWorkDemand = 2,
			UsingPOLineItemToTemporary = 3,
			UsingReceiveItemToTemporary = 4
		}
		public enum WorkOrderPurchaseOrderView {
			WorkOrder = 0,
			PurchaseOrder = 1,
			Explicit = WorkOrderPurchaseOrderLinkage.Explicit + 2,
			UsingLaborDemand = WorkOrderPurchaseOrderLinkage.UsingLaborDemand + 2,
			UsingOtherWorkDemand = WorkOrderPurchaseOrderLinkage.UsingOtherWorkDemand + 2,
			UsingPOLineItemToTemporary = WorkOrderPurchaseOrderLinkage.UsingPOLineItemToTemporary + 2,
			UsingReceiveItemToTemporary = WorkOrderPurchaseOrderLinkage.UsingReceiveItemToTemporary + 2
		}
		#endregion
		#region WorkOrderTemplatePurchaseOrderTemplateLinkage
		public enum WorkOrderTemplatePurchaseOrderTemplateLinkage {
			Explicit = 0,
			UsingLaborDemandTemplate = 1,
			UsingOtherWorkDemandTemplate = 2,
			UsingPOLineItemTemplateToTemplateItemLocation = 3,
		}
		public enum WorkOrderTemplatePurchaseOrderTemplateView {
			WorkOrderTemplate = 0,
			PurchaseOrderTemplate = 1,
			Explicit = WorkOrderTemplatePurchaseOrderTemplateLinkage.Explicit + 2,
			UsingLaborDemandTemplate = WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingLaborDemandTemplate + 2,
			UsingOtherWorkDemandTemplate = WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingOtherWorkDemandTemplate + 2,
			UsingPOLineItemTemplateToTemplateItemLocation = WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingPOLineItemTemplateToTemplateItemLocation + 2,
		}
		#endregion
		#region WorkOrderAssigneeProspect (also used by RequestAssigneeProspect and PurchaseOrderAssigneeProspect )
		public enum WorkOrderAssigneeProspect {
			Employee = 0,
			VendorService = 1,
			Requestor = 2,
			UnitContact = 3,
			Assignees = 4,
			VendorSales = 5,
			VendorPayables = 6,
			BillableRequestor = 7,
		}
		#endregion
	}
	#endregion
	#region DatabaseEnums
	public static class DatabaseEnums {
		#region ApplicationModeId & ApplicationModeName
		// The application ID's that actually name all the "Applications" whether they be separate executables or modes on a single one.
		// These are what appear in the Session record.
		public enum ApplicationModeID {
			Requests,			// Just requests, admin, related definition tables
			Normal,				// full mainboss and admin
			Sessions,			// license keys, sessions in progress
			Administration,		// licensing, users
			MainBossServiceAdmin,	// MainBoss Service Administration
			PickOrganization,
			MainBossService,
			GeneralNotifierService,
			UtilityTool,		// Console mode database access utility tool
			Assignments,		// Only the user's assignments
			WebSession,			// A web application is using the database
			WebServer,			// A web server exists to host WebSession sessions
			WebAccess,			// Web Requests/Access web application session
			SoloDeprecated,		// Mode ended 4.2
			NextAvailable		// For determine length of this set, and to have a value outside the expected range for error detection
		}
		public static Key ApplicationModeName(DatabaseEnums.ApplicationModeID id) {
			switch (id) {
				case DatabaseEnums.ApplicationModeID.Requests:
					return KB.K("MainBoss Requests");
				case DatabaseEnums.ApplicationModeID.Normal:
					return KB.T("MainBoss");
				case DatabaseEnums.ApplicationModeID.Sessions:
					return KB.K("View Sessions");
				case DatabaseEnums.ApplicationModeID.Administration:
					return KB.K("Administration");
				case DatabaseEnums.ApplicationModeID.MainBossServiceAdmin:
					return KB.K("MainBoss Service Administration");
				case DatabaseEnums.ApplicationModeID.PickOrganization:
					return KB.K("Pick Organization");
				case DatabaseEnums.ApplicationModeID.MainBossService:
					return KB.T("MainBoss Service");
				case DatabaseEnums.ApplicationModeID.GeneralNotifierService:
					return KB.K("General Notifier");
				case DatabaseEnums.ApplicationModeID.UtilityTool:
					return KB.K("MainBoss Utility Tool");
				case DatabaseEnums.ApplicationModeID.Assignments:
					return KB.K("MainBoss Assignments");
				case DatabaseEnums.ApplicationModeID.WebSession:
					return KB.K("MainBoss Web");
				case DatabaseEnums.ApplicationModeID.WebServer:
					return KB.K("MainBoss Web Server");
				case DatabaseEnums.ApplicationModeID.WebAccess:
					return KB.K("MainBoss Web Access");
				case DatabaseEnums.ApplicationModeID.SoloDeprecated:
					return KB.T("MainBoss Solo (Deprecated)");
				default:
					return KB.T(Strings.IFormat("Unknown ApplicationModeID {0}", ((int)id).ToString()));
			}
		}
		#endregion
		#region RescheduleBasisAlgorithm
		/// <summary>
		/// The algorithm whereby the scheduling basis is derived from the last PMSchedulingDetail record.
		/// </summary>
		public enum RescheduleBasisAlgorithm {
			FromWorkOrderEndDate = 0,
			FromWorkOrderStartDate,
			FromScheduleBasis,
		}
		#endregion
		#region CalendarUnit
		/// <summary>
		/// The unit for non-meter Periodicity records.
		/// </summary>
		public enum CalendarUnit {
			Days = 0,
			Months = 1
		}
		#endregion
		#region DemandActualCalculationInitValues
		public enum DemandActualCalculationInitValues {
			/// <summary>
			/// The user is required to enter a value
			/// </summary>
			ManualEntry = 0,
			/// <summary>
			/// The value from the current database information (e.g. On Hand value for item, current labor cost)
			/// </summary>
			UseCurrentSourceInformationValue = 1,
			/// <summary>
			/// The value entered into the Demand record is used for the Actual cost
			/// </summary>
			UseDemandEstimateValue = 2
		}
		#endregion
		#region Role/Permission Entry Class
		/// <summary>
		/// Roles/Rights/Permission are in one type separated by a class field
		/// </summary>
		public enum SecurityRoleClass {
			Role = 0, // assignable to a user, can be created and changed by a user
			Right = 1,// Used to create roles, unchangable by user, may allow created of Rights in the future
			Table = 2,// The schema's
		}
		#endregion
		#region DetailType for PMType field of PMGenerationDetail records
		/// <summary>
		/// The DetailType values for PMGenerationDetail records.
		/// The 2.9 import process knows about value '8' for ManualReschedule for setting the Schedule basis
		/// </summary>
		public enum PMType {
			/// <summary>
			/// Scheduling has terminated. The DetailContext explains why (usually end of scheduling period)
			/// </summary>
			SchedulingTerminated,
			/// <summary>
			/// Availability was false on the work start date and a new detail record was created from the date next available.
			/// The DetailContext explains which availability failed.
			/// </summary>
			Deferred,
			/// <summary>
			/// A workorder generate would have been triggered but the work start date is sufficiently indefinite that a WO cannot actually be created.
			/// </summary>
			PredictedWorkOrder,
			/// <summary>
			/// Scheduling lead to some error in determining what to do. The DetailContext contains the explanation.
			/// </summary>
			Error,
			/// <summary>
			/// An error occurred trying to create the work order. This only appears if the commit goes to completion
			/// despite not making this workorder. The DetailContext contains the explanation.
			/// </summary>
			ErrorMakingWorkOrder,
			/// <summary>
			/// A workorder generate has been triggered.
			/// </summary>
			MakeWorkOrder,
			/// <summary>
			/// A manually generated workorder from a specified scheduledWorkOrder record.
			/// </summary>
			MakeUnscheduledWorkOrder,
			/// <summary>
			/// An availability was false causing the work order to be inhibited. The DetailContext explains which availability failed.
			/// </summary>
			Inhibited,
			/// <summary>
			/// User has altered the schedule basis directly.
			/// </summary>
			ManualReschedule,
			/// <summary>
			/// A workorder generate trigger that has a common work start date and same ScheduledWorkOrder as another workorder generate trigger
			/// </summary>
			MakeSharedWorkOrder,
			/// <summary>
			/// All values >= to this affect the schedule basis.
			/// </summary>
			ActualReschedules = MakeWorkOrder
		}
		#endregion
		#region Enum to specify priority between Task and Unit data for certain fields in PMGenerationBatch.
		/// <summary>
		/// The enum is used in some arguments to SpecifyRecordArgument and WorkOrderBatchBuilder.Create to decide what to do with certain fields
		/// that cna be specified both in the Task and as a property of the Unit.
		/// </summary>
		public enum TaskUnitPriority {
			/// <summary>
			/// Use the value from the Unit if any, otherwise use the value from the Task.
			/// </summary>
			PreferUnitValue,
			/// <summary>
			/// Use the value from the Task if any, otherwise use the value from the Unit.
			/// </summary>
			PreferTaskValue,
			/// <summary>
			/// Use the value from the Unit only, ignore the value from the Task.
			/// </summary>
			OnlyUnitValue,
			/// <summary>
			/// Use the value from the Task only, ignore the value from the Unit.
			/// </summary>
			OnlyTaskValue
		}
		#endregion
		#region MainBoss Service Worker
		public enum EmailRequestState {
			/// <summary>
			/// Request typically just fetched from a mail server and put into the table for processing.
			/// </summary>
			UnProcessed,
			/// <summary>
			/// This request has been processed successfully
			/// </summary>
			Completed,
			/// <summary>
			/// The request has no matching Requestor and/or one could not be created due to licensing issues or configuration choices
			/// </summary>
			RejectNoRequestor,
			/// <summary>
			/// No valid requestor exits with a matching email, this state not retryable
			/// </summary>
			Error,
			/// <summary>
			/// No valid Contact exists with the a matching email, this state is not retryable
			/// </summary>
			RejectNoContact,
			/// <summary>
			/// There was more than one matching Requestor, this state is retryable
			/// </summary>
			AmbiguousRequestor,
			/// <summary>
			/// Did not create a Requestor since the was more than one suitable contact, this state is retryable
			/// </summary>
			AmbiguousContactCreation,
			/// <summary>
			/// The request has no matching Requestor and/or one could not be created due to licensing issues or configuration choices, this state is retryable.
			/// </summary>
			NoRequestor,
			/// <summary>
			/// No Contact exists with the a matching email, this state is  retryable
			/// </summary>
			NoContact,
			/// <summary>
			/// There was more than one matching Requestor, this state is  retryable
			/// </summary>
			RejectAmbiguousRequestor,
			/// <summary>
			/// Did not create a Contact for a Requestor since the was more than one suitable contact, this state is not retryable
			/// </summary>
			RejectAmbiguousContactCreation,
			/// <summary>
			/// The email request is to be rejected, the state is not retryable.
			/// </summary>
			ToBeRejected,
			/// <summary>
			/// The email request was manually rejected, the state is not retryable.
			/// </summary>
			RejectManual,
			/// <summary>
			/// Did not create a Contact since the was more than one suitable contact, this state is retryable
			/// </summary>
			AmbiguousContact,
			/// <summary>
			/// Could not create A contact  since the was more already a contact with this code
			/// /// </summary>
			RejectAmbiguousContact,
			/// <summary>
			/// The message was identified as requireing manual review to be processed. This may be something like it contained an AutoSubmitted field flag as other than 'no',
			/// so we do not make a request out of it but hold it for user to decide what to do.
			/// </summary>
			HoldRequiresManualReview
		};
		/// <summary>
		/// The type of mail server that @Requests will attempt to connect to.
		/// </summary>
		public enum MailServerType {
			Any,  
			POP3, // post office protocol version 3
			IMAP4, // internet message access protocol version 4
			POP3S,
			IMAP4S
		}
		/// <summary>
		/// The acceptable types of encryption for the incoming mail server connection
		/// </summary>
		public enum MailServerEncryption {
			AnyAvailable,			  // Use Encryption if available
			RequireEncryption,		  // Require encryption
			RequireValidCertificate,  // Require a valid certicate
			None,					  // No Encryption
		}
		/// <summary>
		/// The type of authentication used when connecting to the incoming mail server.
		/// </summary>
		public enum MailServerAuthentication {
			Plain,						// User and password
			OAuth2,						// OAuth2 (at least, as implemented by Microsoft)
		}
		/// <summary>
		/// The authentication method to use when connecting the SMTP server.
		/// </summary>
		public enum SMTPCredentialType {
			ANONYMOUS,
			DEFAULT, // the logged-in user
			CUSTOM, // manually specified domain, username, password
		}
		#endregion
		#region RelationshipRoleType
		public enum RelationshipRoleType {
			/// <summary>
			/// The relationship is to/from a UnitLocation
			/// </summary>
			Unit = 0,
			/// <summary>
			/// The relationship is to/from a Contact
			/// </summary>
			Contact
		}
		#endregion
		#region ServiceLog EntryType
		public enum ServiceLogEntryType {
			Error,
			Info,
			Warn,
			Activity,
			Trace,
			Close
		};
		public static Dictionary<ServiceLogEntryType, System.Diagnostics.EventLogEntryType> ServiceLogEntryToEventLogEntry = new Dictionary<ServiceLogEntryType, System.Diagnostics.EventLogEntryType>{ 
			{ServiceLogEntryType.Error, System.Diagnostics.EventLogEntryType.Error },
			{ServiceLogEntryType.Info, System.Diagnostics.EventLogEntryType.Information },
			{ServiceLogEntryType.Warn, System.Diagnostics.EventLogEntryType.Warning },
			{ServiceLogEntryType.Activity, System.Diagnostics.EventLogEntryType.Information },
			{ServiceLogEntryType.Trace, System.Diagnostics.EventLogEntryType.Information },
			{ServiceLogEntryType.Close, System.Diagnostics.EventLogEntryType.Information },
		};
		private static readonly Thinkage.Libraries.Translation.Key[] ServiceLogEntryTypeLabels = new Thinkage.Libraries.Translation.Key[]
			{
				KB.K("Error"),
				KB.K("Info"),
				KB.K("Warning"),
				KB.K("Activity"),
				KB.K("Trace"),
				KB.K("Close")
			};
		private static readonly object[] ServiceLogEntryTypeValues = new object[]
			{
				(int)ServiceLogEntryType.Error,
				(int)ServiceLogEntryType.Info,
				(int)ServiceLogEntryType.Warn,
				(int)ServiceLogEntryType.Activity,
				(int)ServiceLogEntryType.Trace,
				(int)ServiceLogEntryType.Close
			};
		public static Thinkage.Libraries.EnumValueTextRepresentations ServiceLogEntryTypeProvider = new Thinkage.Libraries.EnumValueTextRepresentations(ServiceLogEntryTypeLabels, null, ServiceLogEntryTypeValues);
		#endregion
	}
	#endregion
}
