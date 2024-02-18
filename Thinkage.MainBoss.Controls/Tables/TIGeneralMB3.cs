using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// This class doesn't really *do* anything other than provide a scope full of identifiers for all the MB Tbl creation,
	/// holding stuff like common NodeId values, methods to make Check objects, and unqualified exposure of TblRegistry's DefineTbl methods.
	/// </summary>
	// This really should be a static class, but the designers of the language have decided in their infinite wisdumb
	// that static classes are also implicitly sealed and therefore cannot be used as base classes for other classes,
	// even ones that are themselves static! So instead we declare this to have a protected ctor. Even a private ctor
	// cannot be used because the derived classes' (also private) ctors implicitly call our ctor.
	public class xyzzy : TblRegistry {
		// This derivation level is here so the feature groups can be referenced by the application Mode definitions without causing the Tbls
		// themselves to be elaborated.
		// It also provides access to the Security Manager object that will be in effect once the application becomes current.
		protected xyzzy() {
		}
		#region FeatureGroups which control the app's appearance and behaviour
		#region - FeatureGroups set based on which mode/app is being run
		public static readonly SettableFeatureGroup MainBossModeGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup RequestsModeGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup AssignmentsModeGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup AdministrationModeGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup SessionsModeGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup ImportExportModeGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup WebModeGroup = new SettableFeatureGroup();
		#endregion
		#region - FeatureGroups set based on which licenses are used
		// Note that these are only enabled if the app/mode actually checks for and finds a valid license of the corresponding name.
		public static readonly SettableFeatureGroup MainBossWebLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup MainBossServiceAsWindowsServiceLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup WebRequestsLicenseGroup = new SettableFeatureGroup();

		public static readonly SettableFeatureGroup CoreLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup WorkOrderLaborLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup RequestsLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup InventoryLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup ScheduledMaintenanceLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup PurchasingLicenseGroup = new SettableFeatureGroup();
		public static readonly SettableFeatureGroup AccountingLicenseGroup = new SettableFeatureGroup();

		public static readonly FeatureGroup WorkOrdersEnabledGroup = (ScheduledMaintenanceLicenseGroup | InventoryLicenseGroup | WorkOrderLaborLicenseGroup);
		#endregion
		#region - old derived feature groups
		public static readonly FeatureGroup AdminGroup = MainBossModeGroup | AdministrationModeGroup | RequestsModeGroup | ImportExportModeGroup | WebModeGroup;            // basic adminstration stuff
		public static readonly FeatureGroup SecurityGroup = MainBossModeGroup | AdministrationModeGroup | ImportExportModeGroup;                                                                    // users, and permissions (included in all most modes)
		public static readonly FeatureGroup LicenseGroup = MainBossModeGroup | AdministrationModeGroup;                                                                                             // License keys
		public static readonly FeatureGroup RequestsAssignmentsGroup = (MainBossModeGroup | AssignmentsModeGroup | ImportExportModeGroup | WebModeGroup) & RequestsLicenseGroup;                    // Request Action List
		public static readonly FeatureGroup WorkOrdersAssignmentsGroup = (MainBossModeGroup | AssignmentsModeGroup | ImportExportModeGroup | WebModeGroup) & WorkOrdersEnabledGroup;                // WorkOrder Action List
		public static readonly FeatureGroup PurchasingAssignmentsGroup = (MainBossModeGroup | AssignmentsModeGroup | ImportExportModeGroup | WebModeGroup) & PurchasingLicenseGroup;                // Purchasing Action List
		public static readonly FeatureGroup AssignmentsGroup = RequestsAssignmentsGroup | WorkOrdersAssignmentsGroup | PurchasingAssignmentsGroup;

		public static readonly FeatureGroup SessionsGroup = SessionsModeGroup;                                                                                                                      // Active sessions (only in View Sessions mode)
		public static readonly FeatureGroup MainBossServiceAdminGroup = (MainBossModeGroup | AdministrationModeGroup | ImportExportModeGroup | WebModeGroup) & RequestsLicenseGroup;                // MainBoss Service Administration admin but no access to ServiceAsWindowsService themselves
		public static readonly FeatureGroup RequestsGroup = (RequestsAssignmentsGroup | MainBossModeGroup | RequestsModeGroup | ImportExportModeGroup | WebModeGroup) & RequestsLicenseGroup;       // Requests and referenced tables
		public static readonly FeatureGroup RequestorAdminGroup = AdministrationModeGroup & (RequestsLicenseGroup | WorkOrdersEnabledGroup);                                                        // Ability to create/delete requestors
		public static readonly FeatureGroup MainBossServiceGroup = (MainBossModeGroup | RequestsModeGroup | AdministrationModeGroup | ImportExportModeGroup | WebModeGroup) & RequestsLicenseGroup;      // MainBoss Service features. Always used with RequestsGroup.
		public static readonly FeatureGroup MainBossServiceAsWindowsServiceGroup = (MainBossServiceGroup) & MainBossServiceAsWindowsServiceLicenseGroup;      // MainBoss Service features. Always used with RequestsGroup.
																																							  // This group was strange, as it appeared both in the main feature groups for the modes (MainBossModeGroup | RequestsModeGroup | ImportExportModeGroup) and also in their Inventory License sub-mode,
																																							  // but only in the main feature groups for MainBossSoloModeGroup. Their presence in the submode would have been pointless as the main feature groups already enabled them.
																																							  // Only the old Web interface requires the license key to get the Items Group.
		public static readonly FeatureGroup ItemsGroup = (MainBossModeGroup | RequestsModeGroup | ImportExportModeGroup | WebModeGroup) & InventoryLicenseGroup;                                      // Items and their pricing
		public static readonly FeatureGroup StoreroomGroup = (MainBossModeGroup | RequestsModeGroup | ImportExportModeGroup | WebModeGroup) & InventoryLicenseGroup;                                // Ability to create and pick Storerooms (but not assign items). 
																																																	// Note that in the following, MainBossSoloModeGroup does NOT require a special license for WorkOrdersGroup.
		public static readonly FeatureGroup WorkOrdersGroup = (WorkOrdersAssignmentsGroup | MainBossModeGroup | ImportExportModeGroup | WebModeGroup) & WorkOrdersEnabledGroup;                         // Work Orders, Items, and referenced tables. Always used with RequestsGroup and UnitValueAndServiceGroup.
		public static readonly FeatureGroup ItemResourcesGroup = (MainBossModeGroup | ImportExportModeGroup) & InventoryLicenseGroup;                                                                  // WorkOrder Resources; Only used with WorkOrdersGroup
		public static readonly FeatureGroup LaborResourcesGroup = (MainBossModeGroup | ImportExportModeGroup) & WorkOrderLaborLicenseGroup;                                                                  // WorkOrder Labor Resources; Only used with WorkOrdersGroup
		public static readonly FeatureGroup AnyResourcesGroup = ItemResourcesGroup | LaborResourcesGroup;

		public static readonly FeatureGroup SchedulingGroup = ((MainBossModeGroup | ImportExportModeGroup | WebModeGroup) & ScheduledMaintenanceLicenseGroup);             // Ability to create tasks, schedules, and generate WO's from them. Always used with WorkOrdersGroup.
		public static readonly FeatureGroup PurchasingGroup = (PurchasingAssignmentsGroup | MainBossModeGroup | ImportExportModeGroup | WebModeGroup) & PurchasingLicenseGroup;                                             // Ability to create PO's and their line items along with receiving. Always used with WorkOrdersGroup.
		public static readonly FeatureGroup AccountingGroup = (MainBossModeGroup | ImportExportModeGroup | WebModeGroup) & AccountingLicenseGroup;                                                  // Ability to set cost centres and expense models. 
		#endregion
		#endregion
		#region FeatureGroup objects controlling which parts of MainBoss are visible (based on mode/licensing)
		public static readonly FeatureGroup RequestorsGroup = RequestorAdminGroup | RequestsGroup;   // Requestors
		public static readonly FeatureGroup AccessCodeGroup = RequestsGroup | WorkOrdersGroup;               // Access Code
																											 // TODO: Although Units can exist in Storerooms, unless one has Inventory there is no reason for Storerooms to exist.
		public static readonly FeatureGroup UnitsDependentGroup = CoreLicenseGroup | RequestsGroup | WorkOrdersGroup | StoreroomGroup;
		public static readonly FeatureGroup UnitValueAndServiceGroup = UnitsDependentGroup;                      // Value/Service tab on Units
		public static readonly FeatureGroup MetersDependentGroup = WorkOrdersGroup | SchedulingGroup;            // Meters tab on Units
		public static readonly FeatureGroup StoreroomOrItemResourcesGroup = StoreroomGroup | ItemResourcesGroup;
		public static readonly FeatureGroup ItemsDependentGroup = ItemsGroup | StoreroomOrItemResourcesGroup;
		public static readonly FeatureGroup VendorsDependentGroup = ItemsGroup | PurchasingGroup | ItemResourcesGroup | UnitValueAndServiceGroup;
		public static readonly FeatureGroup ContactsDependentGroup = SecurityGroup | RequestorsGroup | WorkOrdersGroup | VendorsDependentGroup; // Tables that reference the Contact table
		public static readonly FeatureGroup LocationGroup = UnitsDependentGroup;                             // Locations other than Storerooms, Temporary Storage, Unit.
		public static readonly FeatureGroup PurchasingAndLaborResourcesGroup = PurchasingGroup & LaborResourcesGroup;

		// Combinations of feature groups which offer more that either of the individual features individually.
		// Note that this behaviour can also be implied by having some parts of the program only reachable through other parts thus causing an implicit AND operation with the feature groups of the referencing part,
		// e.g. the WO resources browsettes permitted by the ResourcesGroup are also implicitly controlled by the WorkOrdersGroup
		// although use of such implicit AND operations should perhaps be avoided
		public static readonly FeatureGroup PartsGroup = ItemsDependentGroup & UnitsDependentGroup;
		public static readonly FeatureGroup SchedulingAndItemResourcesGroup = SchedulingGroup & ItemResourcesGroup;
		public static readonly FeatureGroup SchedulingAndLaborResourcesGroup = SchedulingGroup & LaborResourcesGroup;
		public static readonly FeatureGroup SchedulingAndAnyResourcesGroup = SchedulingGroup & AnyResourcesGroup;
		public static readonly FeatureGroup InventoryGroup = ItemsGroup & StoreroomGroup;
		public static readonly FeatureGroup WorkOrdersAndStoreroomGroup = WorkOrdersGroup & InventoryGroup;
		public static readonly FeatureGroup SchedulingAndPurchasingGroup = SchedulingGroup & PurchasingGroup;
		public static readonly FeatureGroup WorkOrdersAndRequestsGroup = RequestsGroup & WorkOrdersGroup;    // Both WorkOrders and Requests are visible.
		public static readonly FeatureGroup PurchasingAndInventoryGroup = PurchasingGroup & InventoryGroup;
		public static readonly FeatureGroup SchedulingAndPurchasingAndInventoryGroup = PurchasingAndInventoryGroup & SchedulingGroup;

		// We may also need combinations of Inventory, Scheduling, Purchasing, and Accounting if the appropriate pickers and browsettes
		// do not naturally hide themselves. This is most likely if some combinatorial Tbl is directly browsable from main level or visible from the other side of the linkage
		// Inventory+Scheduling -> If you have just Inventory can you see the Task Usage on the storage locations?
		#endregion
	}
	public /* static */ class TIGeneralMB3 : xyzzy {
		protected TIGeneralMB3() {
		}
		/// <summary>
		/// Use where you wish to have No (false) and Yes (true) displayed for a Boolean column
		/// </summary>
		public static EnumValueTextRepresentations YesOrNoForBoolColumnReportText = EnumValueTextRepresentations.NewForBool(KB.K("No"), null, KB.K("Yes"), null);
		static public TblLayoutNode.ICtorArg AccountingFeatureArg = new FeatureGroupArg(AccountingGroup);
		#region NodeIds that apply to multiple tables
		#region Common Validator Check methods
		internal static readonly object EmailAddressId = KB.I("EmailAddressId");
		public static Check1<string> EmailAddressValidator = new Check1<string>(
			delegate (string emailAddress) {
				// Make sure the email address entered is a valid SMTP EmailAddress
				try {
					if (emailAddress != null && emailAddress.Length > 0)
						Thinkage.Libraries.Mail.MailAddress(emailAddress);
				}
				catch (System.Exception errorException) {
					return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(new GeneralException(KB.T(errorException.Message)));
				}
				return null;
			}
		).Operand1(EmailAddressId);

		internal static readonly object WebUrlAddressId = KB.I("WebUrlAddressId");
		public static Check1<string> WebUrlAddressValidator = new Check1<string>(
			delegate (string webUrlAddress) {
				// Make sure the webUrl address entered is a valid Url
				try {
					if (webUrlAddress != null && webUrlAddress.Length > 0) {
						var tf = dsMB.Path.T.Contact.F.WebURL.ReferencedColumn.EffectiveType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
						var impliedUrl = Thinkage.Libraries.Xml.UriUtilities.SetImpliedProtocol(tf.Format(webUrlAddress), "http://");
						if (!System.Uri.IsWellFormedUriString(impliedUrl, UriKind.RelativeOrAbsolute))
							throw new GeneralException(KB.K("Not a well formed Url value"));
						// may be valid Uri, but is it a valid Url
						var urlScheme = new Uri(impliedUrl).Scheme; // warn about protocols that might not be valid (we assume the majority want http or possibly ftp
						switch (urlScheme) {
							case "http":
							case "https":
							case "ftp":
							case "ftps":
							case "file":
								break;
							default:
								throw new GeneralException(KB.K("Url scheme '{0}' may not be a valid locator scheme. Expecting http, https, ftp, ftps or file."), urlScheme);
						}
						return null;
					}
				}
				catch (System.Exception errorException) {
					return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(new GeneralException(KB.T(errorException.Message)));
				}
				return null;
			}
		).Operand1(WebUrlAddressId);
		#endregion
		internal static readonly SimpleKey OverdueLabelKey = KB.K("Days Overdue");
		internal static readonly object StateHistoryEffectiveDateId = KB.I("StateHistoryEffectiveDateId");
		internal static readonly object StateHistoryPreviousEffectiveDateId = KB.I("StateHistoryPreviousEffectiveDateId");
		internal static readonly object StateHistoryNextEffectiveDateId = KB.I("StateHistoryNextEffectiveDateId");
		internal static readonly object CurrentStateHistoryIdWhenCalledId = KB.I("CurrentStateHistoryIdWhenCalledId");
		internal static readonly object CurrentStateHistoryCodeWhenLoadedId = KB.I("CurrentStateHistoryCodeWhenLoadedId");
		internal static readonly object ItemLocationMinimumId = KB.I("ItemLocationMinimumId");
		internal static readonly object ItemLocationMaximumId = KB.I("ItemLocationMaximumId");
		internal static readonly object ItemLocationAvailableId = KB.I("ItemLocationAvailableId");
		internal static readonly object POLineDemandedId = KB.I("POLineDemandedId");
		internal static readonly object POLineOrderedId = KB.I("POLineOrderedId");

		protected static readonly SimpleKey UsingDemandedCost = KB.K("The demanded cost is being used");
		protected static readonly SimpleKey UseDemandedCost = KB.K("Use demanded cost");
		private static readonly SimpleKey DemandedCost = KB.K("Demanded Cost");
		#endregion
		#region Layout node label support
		protected static readonly Key CommonCodeColumnKey = dsMB.LabelKeyBuilder.K("Code");
		protected static readonly Key CommonDescColumnKey = dsMB.LabelKeyBuilder.K("Desc");
		/// <summary>
		/// Produce a path-key for a path which contains a reverse linkage (.L.) where you want the name of the
		/// table referenced by this linkage to appear as a context element of the path-key.
		/// </summary>
		/// <param name="startPath">a path-to-row of the table whose name should be added to the path-key</param>
		/// <param name="endPath">the remainder of the path starting at the table the .L. links to.</param>
		/// <returns></returns>
		/// <example>
		/// This can be used to supply a "User" level of context to the information from the User record associated with a Contact record.
		/// The full path dsMB.Path.T.Employee.F.ContactID.Id.L.User.ContactID.F.Name normally keys as "Name of the Contact"
		/// By calling VisibleReverseLinkagePathKey(dsMB.Path.T.Employee.F.ContactID.Id.L.User.ContactID, dsMB.Path.T.User.F.Name)
		/// the result is "Name of the User of the Contact" instead.
		/// </example>
		protected static Key VisibleReverseLinkagePathKey(DBI_PathToRow startPath, DBI_Path endPath) {
			return XKey.New(endPath.Key(), XKey.New(endPath.Table.LabelKey, startPath.PathToReferencingColumn.Key()));
		}
		#endregion
		#region ApplicationIdProvider
		public readonly static EnumValueTextRepresentations ApplicationIdProvider;
		#endregion
		#region Common methods and properties for support of Tbls that allows corrections (e.g., actuals, etc.)
		#region Unit Cost types
		public static readonly TypeInfo ItemUnitCostTypeOnClient = new CurrencyTypeInfo(0.0001m, decimal.MinValue, decimal.MaxValue, true);
		public static readonly TypeInfo HourlyUnitCostTypeOnClient = new CurrencyTypeInfo(0.01m, decimal.MinValue, decimal.MaxValue, true);
		public static readonly TypeInfo PerJobUnitCostTypeOnClient = new CurrencyTypeInfo(0.01m, decimal.MinValue, decimal.MaxValue, true);
		public static readonly TypeInfo POMiscellaneousUnitCostTypeOnClient = new CurrencyTypeInfo(0.01m, decimal.MinValue, decimal.MaxValue, true);
		public static readonly TypeInfo ItemUnitCostTypeOnServer = new Libraries.TypeInfo.CurrencyTypeInfo(0.0001m, Int64.MinValue / 10000m, Int64.MaxValue / 10000m, true);    // TODO: This includes SQL knowledge as to the allowed range and precision.
		public static readonly TypeInfo HourlyCostTypeOnServer = new Libraries.TypeInfo.CurrencyTypeInfo(0.01m, Int64.MinValue / 10000m, Int64.MaxValue / 10000m, true);    // TODO: This includes SQL knowledge as to the allowed range and precision.
		#endregion
		#region Node Ids and common multi table layout columns
		protected static readonly object AvailableQuantityId = KB.I("AvailableQuantityId");

		protected static readonly object ThisEntryQuantityId = KB.I("ThisEntryQuantityId");
		protected static readonly object ThisEntryUnitCostId = KB.I("ThisEntryUnitCostId");
		protected static readonly object ThisEntryCostId = KB.I("ThisEntryCostId");

		protected static readonly object BeforeCorrectionQuantityId = KB.I("BeforeCorrectionQuantityId");

		protected static readonly object OnHandQuantityId = KB.I("OnHandQuantityId");
		protected static readonly object OnHandValueId = KB.I("OnHandValueId");
		protected static readonly object OnHandUnitCostId = KB.I("OnHandUnitCostId");

		protected static readonly object PricingQuantityId = KB.I("PricingQuantityId");
		protected static readonly object PricingCostId = KB.I("PricingCostId");
		protected static readonly object PricingUnitCostId = KB.I("PricingUnitCostId");

		protected static readonly Key Remaining = KB.K("Remaining");
		protected static readonly object RemainingQuantityId = KB.I("RemainingQuantityId");
		protected static readonly Key AlreadyUsed = KB.K("Already used");
		protected static readonly object AlreadyUsedQuantityId = KB.I("AlreadyUsedQuantityId");
		protected static readonly object AlreadyUsedValueId = KB.I("AlreadyUsedValueId");
		protected static readonly object AlreadyUsedUnitCostId = KB.I("AlreadyUsedUnitCostId");

		protected static readonly Key SuggestedCost = KB.K("Suggested cost");
		protected static readonly Key CalculatedCost = KB.K("Calculated Cost");
		protected static readonly object CalculatedUnitCostId = KB.I("CalculatedUnitCostId");
		protected static readonly object CalculatedQuantityId = KB.I("CalculatedQuantityId");

		protected static readonly object MinimumId = KB.I("MinimumId");
		protected static readonly object MaximumId = KB.I("MaximumId");

		protected static readonly object RequiredQuantityId = KB.I("RequiredQuantityId");
		protected static readonly object RequiredUnitCostId = KB.I("RequiredUnitCostId");

		protected static readonly Key TotalCostLabel = KB.K("Total Cost");

		protected static readonly object AccessCodeId = KB.I("AccessCodeId");

		protected static readonly Key UseCalculatedCost = KB.K("Use calculated cost");

		protected static readonly Key UseSuggestedCost = KB.K("Use suggested cost");
		protected static readonly Key UseUnitAccessCode = KB.K("Use Access Code from Unit");

		protected static readonly Key UsingSuggestedCost = KB.K("The suggested cost is being used");
		protected static readonly Key UsingCalculatedCost = KB.K("The calculated cost is being used");

		protected static readonly Key BecauseUsingUnitAccessCode = KB.K("Readonly because Unit's Access Code is being used");
		protected static readonly Key BecauseUsingCalculatedCost = KB.K("Readonly because calculated cost is being used");
		protected static readonly Key BecauseUsingSuggestedCost = KB.K("Readonly because suggested cost is being used");
		protected static readonly Key BecauseUsingCalculatedQuantity = KB.K("Readonly because calculated quantity is being used");
		protected static readonly Key BecauseUsingCalculatedCostQuantity = KB.K("Readonly because calculated cost and quantity are being used");
		protected static readonly Key BecauseUsingCalculatedPOLineDescription = KB.K("Readonly because description from item is being used");

		// For passing a cost estimate value/quantity for suggested cost handling from demands, etc.
		protected static readonly object CostEstimateBasisValueId = KB.I("CostEstimateBasisValueId");
		protected static readonly object CostEstimateBasisQuantityId = KB.I("CostEstimateBasisQuantityId");
		protected static readonly object CostEstimateBasisUnitCostId = KB.I("CostEstimateBasisUnitCostId");

		// The value in the "Calculated Cost" row
		protected static readonly object CostCalculationValueSuggestedSourceId = KB.I("CostCalculationValueId");
		// The value in the Demanded Cost row
		protected static readonly object CostEstimationValueId = KB.I("CostEstimationValueId");

		// Common Multicolumn layouts
		protected static Key[] MulticolumnQuantityUnitCostTotalLabels = new Key[] { KB.K("Quantity"), KB.K("Unit Cost"), KB.K("Cost") };
		protected static Key[] MulticolumnHoursHourlyRateTotalLabels = new Key[] { KB.K("Hours"), KB.K("Hourly Rate"), KB.K("Cost") };
		protected static Key[] MulticolumnDemandActualLabels = new Key[] { KB.K("Demand"), KB.K("Actual") };
		// Common columns used in mainboss
		private const string QuantityColumnName = "Quantity";
		public static DBI_Column QuantityColumn(DBI_Table schema) {
			return schema.Columns[KB.I(QuantityColumnName)];
		}
		private const string ActualQuantityColumnName = "ActualQuantity";
		public static DBI_Column ActualQuantityColumn(DBI_Table schema) {
			return schema.Columns[KB.I(ActualQuantityColumnName)];
		}
		#endregion
		#region Contact Group
		// TODO: The following various contact groups should perhaps be changed to follow the model of CurrentStateHistoryGroup,
		// except to allow several paths to contact rows. This would eliminate the need for all the explicit column labels and the combinatorial explosion
		// of methods based on what columns should be shown.
		/// <summary>
		/// Create the Layout Node for a multicolumn layout of one of more rows of Contact information, specifically the Name, Business Phone, and Email for the contact.
		/// </summary>
		/// <param name="nodes">Definitions for the individual rows, typically the return values from ContactRowGroup</param>
		/// <returns></returns>
		protected static TblContainerNode ContactGroupTblLayoutNode(params TblRowNode[] nodes) {
			return TblMultiColumnNode.New(
				new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
				new Key[] { KB.K("Name"), KB.K("Business Phone"), KB.K("Email") },
				nodes
			);
		}
		protected static TblContainerNode ContactGroupPreferredLanguageTblLayoutNode(params TblRowNode[] nodes) {
			return TblMultiColumnNode.New(
				new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
				new Key[] { KB.K("Name"), KB.K("Email"), KB.K("Preferred Language") },
				nodes
			);
		}
		/// <summary>
		/// Define a single row for a multicolumn contact row group, where the editor should be able to pick a Contact directly
		/// </summary>
		/// <param name="pathToContact">Path from the Tbl root to the Contact record</param>
		/// <param name="nameEcolAttr">ECol attribute to use on the Name entry in the row</param>
		/// <returns></returns>
		protected static TblRowNode ContactGroupRow(DBI_Path pathToContact, ECol nameEcolAttr) {
			return ContactGroupRow(pathToContact, dsMB.Path.T.Contact, nameEcolAttr);
		}
		/// <summary>
		/// Define a single row for a multicolumn contact row group, where the editor should be able to pick a record that refers to and is identified by a Contact record.
		/// </summary>
		/// <param name="pathToRecord">Path from the Tbl root to the picked record</param>
		/// <param name="pathRecordToContact">Path from the picked record to its identifying Contact</param>
		/// <param name="nameEcolAttr">ECol attribute to use on the Name entry in the row</param>
		/// <returns></returns>
		protected static TblRowNode ContactGroupRow(DBI_Path pathToRecord, DBI_PathToRow pathRecordToContact, ECol nameEcolAttr) {
			return TblRowNode.New(pathToRecord.Key(), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(pathToRecord, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(new DBI_Path(pathRecordToContact, dsMB.Path.T.Contact.F.Code))), nameEcolAttr),
					TblColumnNode.New(new DBI_Path(pathToRecord.PathToReferencedRow, new DBI_Path(pathRecordToContact, dsMB.Path.T.Contact.F.BusinessPhone)), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(new DBI_Path(pathToRecord.PathToReferencedRow, new DBI_Path(pathRecordToContact, dsMB.Path.T.Contact.F.Email)), DCol.Normal, ECol.AllReadonly));
		}
		/// <summary>
		/// Make a multicolumn Contact row group that represents a Requestor, with phone and email information
		/// </summary>
		/// <param name="pathToRequestor"></param>
		/// <param name="editable"></param>
		/// <returns></returns>
		public static readonly object RequestorPickerNodeId = KB.I("RequestorPickerNodeId");
		public static readonly object RequestorEmailFilterNodeId = KB.I("RequestorEmailFilterNodeId");
		protected static TblContainerNode SingleRequestorGroup(DBI_Path pathToRequestor, bool editable) {
			return ContactGroupTblLayoutNode(
					ContactGroupRow(pathToRequestor, dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, editable
						? new ECol(ECol.NormalAccess, Fmt.SetId(RequestorPickerNodeId), Fmt.SetBrowserFilter(BTbl.TaggedEqFilter(dsMB.Path.T.Requestor.F.ContactID.F.Email, RequestorEmailFilterNodeId, true)))
						: ECol.AllReadonly)
				);
		}
		/// <summary>
		/// Make a multicolumn Contact row group that represents a Requestor, with email information and language preference
		/// </summary>
		/// <param name="pathToRequestor"></param>
		/// <returns></returns>
		protected static TblContainerNode SingleRequestorLanguagePreferenceGroup(DBI_Path pathToRequestor) {
			return ContactGroupPreferredLanguageTblLayoutNode(TblRowNode.New(KB.TOi(TId.Requestor), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(pathToRequestor, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(new DBI_Path(dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, dsMB.Path.T.Contact.F.Code))), ECol.AllReadonly),
					TblColumnNode.New(new DBI_Path(pathToRequestor.PathToReferencedRow, new DBI_Path(dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, dsMB.Path.T.Contact.F.Email)), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(new DBI_Path(pathToRequestor.PathToReferencedRow, new DBI_Path(dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, dsMB.Path.T.Contact.F.PreferredLanguage)), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)
				));
		}
		#endregion
		#region Requestor Assignee Group
		/// <summary>
		/// Return a List of nodes to add to a TblLayoutNode Array display common Contact information for an Assignee
		/// </summary>
		protected static TblContainerNode SingleContactGroup(DBI_Path pathToContact) {
			return ContactGroupTblLayoutNode(
				ContactGroupRow(pathToContact, ECol.Normal)
			);
		}
		#endregion
		#region CurrentStateGroup
		/// <summary>
		/// Return a TblContainerNode construct to display the given State History information in a compact form.
		/// This isn't really tied to State History, any path to the "Current" something-or-other will work.
		/// </summary>
		/// <returns></returns>
		protected static TblContainerNode CurrentStateHistoryGroup(DBI_Path pathToCurrentStateHistory, params DBI_Path[] pathsFromStateHistoryToFields) {
			var nodes = new List<TblColumnNode>();
			var headers = new List<Key>();
			foreach (DBI_Path p in pathsFromStateHistoryToFields) {
				if (p.ReferencedColumn.EffectiveType is TranslationKeyTypeInfo)
					nodes.Add(TblColumnNode.New(new DBI_Path(pathToCurrentStateHistory.PathToReferencedRow, p), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly, Fmt.SetDynamicSizing()));
				else
					nodes.Add(TblColumnNode.New(new DBI_Path(pathToCurrentStateHistory.PathToReferencedRow, p), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly));
				headers.Add(p.Key());
			}

			return TblMultiColumnNode.New(
				new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
				headers.ToArray(),
				TblRowNode.New(KB.K("Current"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, nodes.ToArray())
			);
		}
		#endregion
		#region LicenseAnalysis
		private static string LicenseAnalysis(AnalyzedLicense bestLicense) {
			if (bestLicense.IsValidLicense) {
				// The license is valid. There may be several things to say about it and we arbitrarily prioritize these.
				// First, we comment on the expiry, if it is relevant. Then we comment on the count, it there is any.
				if (bestLicense.DaysToExpiry.HasValue && bestLicense.DaysToExpiry.Value <= 42)
					return Thinkage.Libraries.Exception.FullMessage(bestLicense.ExpiryMessage);
				else if (bestLicense.License.ExpiryModel == License.ExpiryModels.DemoExpiry)
					return KB.K("Licensed for Demonstration mode").Translate();
				else if (!bestLicense.UsesCount)
					return KB.K("Licensed").Translate();
				else if (bestLicense.CountedItemsCount.HasValue)
					return Strings.Format(Application.InstanceMessageCultureInfo, KB.K("Licensed for {0} {0.IsOne ? {1} : {2} } of which {3} currently exist"), bestLicense.License.LicenseCount, bestLicense.CountedItemName, bestLicense.CountedItemsName, bestLicense.CountedItemsCount.Value);
				else
					return Strings.Format(Application.InstanceMessageCultureInfo, KB.K("Licensed for {0} {0.IsOne ? {1} : {2} }"), bestLicense.License.LicenseCount, bestLicense.CountedItemName, bestLicense.CountedItemsName);
			}
			else {
				if (bestLicense.InvalidLicenseId ?? false)
					return Thinkage.Libraries.Exception.FullMessage(bestLicense.LicenseIdMessage);
				else if (bestLicense.Expired ?? false)
					return Thinkage.Libraries.Exception.FullMessage(bestLicense.ExpiryMessage);
				else if (bestLicense.CountLimitExceeded ?? false)
					return Thinkage.Libraries.Exception.FullMessage(bestLicense.CountLimitMessage);
				// else should never occur. If !IsValidLicense, there must be a true InvalidLicenseId value, a true Expired value, or a true CountLimitExceeded value.
				return null;
			}
		}
		#endregion


		#region ColumnSourceWrappers
		/// <summary>
		/// PerViewColumn format specifiers for items that may differ in formatting requirements in the same list; return an Interval formatted as string
		/// </summary>
		protected static BTbl.ListColumnArg.IAttr IntervalFormat = BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<System.TimeSpan?>(originalSource, 15));
		/// <summary>
		/// PerViewColumn format specifiers for items that may differ in formatting requirements in the same list; return an Integer/long formatted as string
		/// </summary>
		protected static BTbl.ListColumnArg.IAttr IntegralFormat = BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<long?>(originalSource, 15));
		/// <summary>
		/// PerViewColumn format specifiers for items that may differ in formatting requirements in the same list; return an Integer/long formatted fixed-width as string so the strings sort in the same order as the original numbers (if non-negative)
		/// </summary>
		protected static BTbl.ListColumnArg.IAttr SequenceFormat = BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<long?>(originalSource, 15, padLeft: true));
		/// <summary>
		/// PerViewColumn format specifiers for items that may differ in formatting requirements in the same list; return an DateTime formatted as string
		/// </summary>
		protected static BTbl.ListColumnArg.IAttr DateTimeFormat = BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<DateTime?>(originalSource, 50));
		/// <summary>
		/// PerViewColumn format specifiers for items that may differ in formatting requirements in the same list; return an Currency formatted as string
		/// </summary>
		protected static BTbl.ListColumnArg.IAttr CurrencyFormat = BTbl.ListColumnArg.WrapSource((Source originalSource) => new FormattingSource<decimal?>(originalSource, 15));

		// ListColumn attribute for use in picker use(non PerViewColumn)
		// TODO: This is a mess. It has a stupid name. It is not just used on pickers. It *is* used on some XID fields (Code) and so *should* include ClosedCombo.
		protected static BTbl.ListColumnArg.Contexts NonPerViewColumn = BTbl.ListColumnArg.Contexts.List | BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter;// Common picker use is to only show in Open combo form and leave closed form to display the XID of the picked item | BTbl.ListColumnArg.Contexts.ClosedCombo;
		#endregion
		#region DerivationTblCreatorBase
		protected abstract class DerivationTblCreatorBase {
			#region Construction
			public DerivationTblCreatorBase(Tbl.TblIdentification identification, DBI_Table mostDerivedTable) {
				Identification = identification;
				MostDerivedTable = mostDerivedTable;
			}
			#endregion
			#region TblCreation
			// Common Tbl builder
			public virtual Tbl GetTbl(params Tbl.IAttr[] tblAttrs) {
				TblAttributes.AddRange(tblAttrs);
				Tabs.Insert(0, DetailsTabNode.New(DetailColumns.ToArray()));
				return new Tbl(MostDerivedTable,
					Identification,
					TblAttributes.ToArray(),
					new TblLayoutNodeArray(Tabs),
					Actions
				);
			}
			#endregion
			#region Picker and filtering support for next picker to build
			public object AddPickerFilterControl(Key label, TypeInfo controlValueType, params ECol.ICtorArg[] args) {
				object controlId = 42;
#if DEBUG
				controlId = Strings.IFormat("{0} ID {1}", (label != null ? label.IdentifyingName : string.Empty), seed++);
#endif
				List<ECol.ICtorArg> augmentedArgs = new List<ECol.ICtorArg>(args);
				augmentedArgs.Add(Fmt.SetId(controlId));
				PickerGroupContents.Add(TblUnboundControlNode.New(label, controlValueType, new ECol(augmentedArgs.ToArray())));
				return controlId;
			}
			public void StartPickerFilterControlGroup() {
				if (PickerGroupingStack == null)
					PickerGroupingStack = new Stack<List<TblLayoutNode>>();
				PickerGroupingStack.Push(PickerGroupContents);
				PickerGroupContents = new List<TblLayoutNode>();
			}
			public void EndPickerFilterControlGroup(Key groupLabel) {
				// Too many EndPickerFilterControlGroup calls will cause a null dereference when we touch PickerGroupingStack.
				TblLayoutNode groupNode = TblGroupNode.New(groupLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal }, PickerGroupContents.ToArray());
				PickerGroupContents = PickerGroupingStack.Pop();
				PickerGroupContents.Add(groupNode);
				if (PickerGroupingStack.Count == 0)
					PickerGroupingStack = null;
			}
			public void AddPickerFilter(BTbl.IBTblFilterArg filterDef, object controllingControlId, Init.ValueMapperDelegate controlValueMapper) {
				AddPickerFilter(filterDef, new Thinkage.Libraries.Presentation.ControlValue(controllingControlId), controlValueMapper);
			}
			public void AddPickerFilter(BTbl.IBTblFilterArg filterDef, EditorInitValue controllingValue, Init.ValueMapperDelegate controlValueMapper) {
				object browserFilterId = 42;
#if DEBUG
				browserFilterId = Strings.IFormat("{0} Filter ID {1}", controllingValue.DebugName, seed++);
#endif
				AddCustomPickerFilter(BTbl.TaggedDisableableFilter(filterDef, false, browserFilterId), browserFilterId, controllingValue, controlValueMapper);
			}
			public void AddCustomPickerFilter(BTbl.IBTblFilterArg filterDef, object browserFilterId, EditorInitValue controllingValue, Init.ValueMapperDelegate controlValueMapper) {
				PickerEColAttrs.Add(Fmt.SetBrowserFilter(filterDef));
				PickerFilters.Add(new PickerFilterInfo(browserFilterId, controllingValue, controlValueMapper));
			}
			public void AddPickerPanelDisplay(DBI_Path boundPath, params DCol.ICtorArg[] args) {
				PickerGroupContents.Add(TblColumnNode.New(boundPath, new DCol(args)));
			}
			public void CreateBoundPickerControl(object pickerId, DBI_Path boundPath, params ECol.ICtorArg[] args) {
				System.Diagnostics.Debug.Assert(PickerGroupingStack == null, "Mismatched calls to start/end picker filter control group");
				PickerEColAttrs.Add(Fmt.SetId(pickerId));
				PickerEColAttrs.AddRange(args);
				TblLeafNode pickerNode = TblColumnNode.New(boundPath, new NonDefaultCol(), new ECol(PickerEColAttrs.ToArray()));
				PickerEColAttrs.Clear();
				// If there are no filters just place the labelled picker in the main LCP.
				// If there are filters place the unlabelled picker in the group box.
				// Technically we should actually count Group Contents that have an ECol, but right now that is just filter controls.
				(PickerFilters.Count > 0 ? PickerGroupContents : DetailColumns).Add(pickerNode);
				CompletePickerControlActions(pickerId, pickerNode);
				CompleteGroupPickerControl(boundPath);
			}
			/// <summary>
			/// Add a Picker to the group, but don't complete the group yet! This ONLY works for PickerGroupContents
			/// </summary>
			/// <param name="pickerId"></param>
			/// <param name="tInfo"></param>
			/// <param name="args"></param>
			protected void AddUnboundPickerControlToGroup(Key label, object pickerId, TypeInfo tInfo, params ECol.ICtorArg[] args) {
				System.Diagnostics.Debug.Assert(PickerGroupingStack == null, "Mismatched calls to start/end picker filter control group");
				// If there are filters place the unlabelled picker in the group box.
				// Technically we should actually count Group Contents that have an ECol, but right now that is just filter controls.
				PickerEColAttrs.Add(Fmt.SetId(pickerId));
				PickerEColAttrs.AddRange(args);
				TblLeafNode pickerNode = TblUnboundControlNode.New(label, tInfo, new ECol(PickerEColAttrs.ToArray()));
				PickerEColAttrs.Clear();
				PickerGroupContents.Add(pickerNode);
				CompletePickerControlActions(pickerId, pickerNode);
			}
			protected void CreateAndCompleteUnboundPickerControl(object pickerId, Key resourceLabel, TypeInfo tInfo, params ECol.ICtorArg[] args) {
				System.Diagnostics.Debug.Assert(PickerGroupingStack == null, "Mismatched calls to start/end picker filter control group");
				PickerEColAttrs.Add(Fmt.SetId(pickerId));
				PickerEColAttrs.AddRange(args);
				TblLeafNode pickerNode = TblUnboundControlNode.New(resourceLabel, tInfo, new ECol(PickerEColAttrs.ToArray()));
				PickerEColAttrs.Clear();
				// If there are no filters just place the labelled picker in the main LCP.
				// If there are filters place the unlabelled picker in the group box.
				// Technically we should actually count Group Contents that have an ECol, but right now that is just filter controls.
				(PickerFilters.Count > 0 ? PickerGroupContents : DetailColumns).Add(pickerNode);
				CompletePickerControlActions(pickerId, pickerNode);
				CompleteGroupPickerControl(resourceLabel);
			}
			protected void CompleteGroupPickerControl(Key resourceLabel) {
				// If there were no filters the following group box will be empty in the editor and will therefore vanish.
				DetailColumns.Add(TblGroupNode.New(resourceLabel, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, PickerGroupContents.ToArray()));
				PickerGroupContents.Clear();
			}
			protected void CompleteGroupPickerControl(DBI_Path resourceBoundPath) {
				// If there were no filters the following group box will be empty in the editor and will therefore vanish.
				DetailColumns.Add(TblGroupNode.New(resourceBoundPath, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, PickerGroupContents.ToArray()));
				PickerGroupContents.Clear();
			}
			private void CompletePickerControlActions(object pickerId, TblLeafNode pickerNode) {
				ECol ec = TblLayoutNode.GetECol(pickerNode);
				TblLeafNode.Access[] access = ec.Access;
				List<EdtMode> writeableModes = new List<EdtMode>();
				for (EdtMode i = EdtMode.Max; --i >= 0;)
					if (access[(int)i] >= TblLeafNode.Access.Writeable)
						writeableModes.Add(i);
				TblActionNode.IArg activity = TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, writeableModes.ToArray());
				for (int ii = PickerFilters.Count; --ii >= 0;) {
					Init i = Init.New(new InSubBrowserTarget(pickerId, new BrowserFilterTarget(PickerFilters[ii].FilterId)), PickerFilters[ii].ControllingInitValue,
						null, TblActionNode.Activity.Disabled, activity);
					i.ValueMapper = PickerFilters[ii].ValueMapper;
					Actions.Add(i);
				}
				PickerFilters.Clear();
			}
			#endregion
			#region Creation of suggested-value choice control
			public struct SuggestedValueSource {
				public readonly Key UseDirectiveLabel;
				public readonly object SourceValueId;
				public readonly Key TargetReadonlyDisablerTip;
				public SuggestedValueSource(Key useDirectiveLabel, object sourceValueId, Key targetReadonlyDisablerTip) {
					UseDirectiveLabel = useDirectiveLabel;
					SourceValueId = sourceValueId;
					TargetReadonlyDisablerTip = targetReadonlyDisablerTip;
				}
			}
			protected delegate void SetTargetControlsReadonly(EditorInitValue readonlyValue, Key readonlyExplanationTip);
			protected TblLeafNode CreateSuggestedValueSelectorControl(EditInitTarget targetValue, SetTargetControlsReadonly setTargetControlsReadonly, List<SuggestedValueSource> suggestedValueSources, object defaultChoiceSource, Key manualEntryChoiceKey, object choiceControlId, params TblLayoutNode.ICtorArg[] attrs) {
				TblLeafNode suggestedValueChoice;
				List<TblLayoutNode.ICtorArg> overallAttrs = new List<TblLayoutNode.ICtorArg>(attrs);

				if (suggestedValueSources.Count == 0)
					return null;

				int defaultChoice = 0; // will be set to the SuggestedValueSource that matches our defaultValue
				Key[] choices = new Key[suggestedValueSources.Count + 1];
				object[] results = new object[suggestedValueSources.Count + 1];
				choices[0] = manualEntryChoiceKey;
				results[0] = 0;
				for (int i = 1; i <= suggestedValueSources.Count; ++i) {
					choices[i] = suggestedValueSources[i - 1].UseDirectiveLabel;
					results[i] = i;
					if (suggestedValueSources[i - 1].SourceValueId == defaultChoiceSource)
						defaultChoice = i;
				}

				List<ECol.ICtorArg> ecolArgs = new List<ECol.ICtorArg>();
				ecolArgs.Add(Fmt.SetId(choiceControlId));
				ecolArgs.Add(ECol.OmitInUpdateAccess);

				object nonNewValue;
				object defaultValueIfSetting;
				TypeInfo controlType;
				Key controlLabel;
				if (suggestedValueSources.Count == 1) {
					defaultValueIfSetting = defaultChoice == 1;
					controlType = BoolTypeInfo.NonNullUniverse;
					controlLabel = suggestedValueSources[0].UseDirectiveLabel;
					nonNewValue = false;
				}
				else {
					defaultValueIfSetting = defaultChoice;
					ecolArgs.Add(Fmt.SetEnumText(new EnumValueTextRepresentations(choices, null, results, (uint)suggestedValueSources.Count + 1)));
					controlType = new IntegralTypeInfo(true, 0, suggestedValueSources.Count);
					controlLabel = null;
					nonNewValue = 0;
				}
				// Set the new-mode source
				if (defaultChoiceSource is DBI_Path)
					// In New mode the source is the Path value
					Actions.Add(Init.OnLoadNew(new ControlTarget(choiceControlId), new EditorPathValue((DBI_Path)defaultChoiceSource)));
				else
					// In New mode, the source is the default determined above
					ecolArgs.Add(Fmt.SetIsSetting(defaultValueIfSetting));
				// In all other modes, the source is nonNewValue (Manually Enter) to ensure the target control does not get modified.
				Actions.Add(Init.New(new ControlTarget(choiceControlId), new ConstantValue(nonNewValue), null, TblActionNode.Activity.OnLoad, TblActionNode.SelectiveActivity(TblActionNode.Activity.Disabled, EdtMode.New)));

				// Create the layout node.
				overallAttrs.Add(new ECol(ecolArgs.ToArray()));
				suggestedValueChoice = TblUnboundControlNode.New(controlLabel, controlType, overallAttrs.ToArray());

				Thinkage.Libraries.Presentation.ControlValue choiceControlInitValue = new Thinkage.Libraries.Presentation.ControlValue(choiceControlId);
				for (int i = suggestedValueSources.Count; --i >= 0;) {
					EditorInitValue targetReadonlyValue;
					if (suggestedValueSources.Count == 1)
						targetReadonlyValue = choiceControlInitValue;
					else {
						int thisi = i + 1;
						targetReadonlyValue = new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse,
							delegate (object[] inputs) {
								if (inputs[0] == null)
									return false;
								return thisi == (int)IntegralTypeInfo.AsNativeType(inputs[0], typeof(int));
							}, choiceControlInitValue);
					}
					// Make the actions that actually manage the target.
					// Build the action copying the suggested value to the targetValue only in New mode.
					Actions.Add(Init.New(targetValue, new ControlUncheckedValue(suggestedValueSources[i].SourceValueId), targetReadonlyValue,
						TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New)));
					setTargetControlsReadonly(targetReadonlyValue, suggestedValueSources[i].TargetReadonlyDisablerTip);
				}
				return suggestedValueChoice;
			}
			#endregion
			#region Members
			#region - Information computed based on the ctor arguments
			public readonly DBI_Table MostDerivedTable;
			protected readonly Tbl.TblIdentification Identification;
			#endregion
			#region - The pieces that will make up the Tbl
			public readonly List<TblLayoutNode> DetailColumns = new List<TblLayoutNode>();
			public readonly List<TblLayoutNode> Tabs = new List<TblLayoutNode>();
			public readonly List<TblActionNode> Actions = new List<TblActionNode>();
			public readonly List<Tbl.IAttr> TblAttributes = new List<Tbl.IAttr>();
			#region Picker Pieces
			List<TblLayoutNode> PickerGroupContents = new List<TblLayoutNode>();
			Stack<List<TblLayoutNode>> PickerGroupingStack;
			List<ECol.ICtorArg> PickerEColAttrs = new List<ECol.ICtorArg>();
			struct PickerFilterInfo {
				public PickerFilterInfo(object filterId, EditorInitValue controllingInitValue, Init.ValueMapperDelegate valueMapper) {
					FilterId = filterId;
					ControllingInitValue = controllingInitValue;
					ValueMapper = valueMapper;
				}
				public readonly object FilterId;
				public readonly EditorInitValue ControllingInitValue;
				public readonly Init.ValueMapperDelegate ValueMapper;
			}
			List<PickerFilterInfo> PickerFilters = new List<PickerFilterInfo>();
			#endregion
			#endregion
			#region - The NodeId's we use

#if DEBUG
			// This is used to generate textual id's in DEBUG mode.
			private static int seed = 4242;
#endif
			#endregion

			#endregion
		}
		#endregion
		#region DerivationTblCreatorWithQuantityAndCostBase
		/// <summary>
		/// A base class that expects Quantity and Cost type operations
		/// </summary>
		protected abstract class DerivationTblCreatorWithQuantityAndCostBase : DerivationTblCreatorBase {
			#region Construction
			public DerivationTblCreatorWithQuantityAndCostBase(Tbl.TblIdentification identification, bool correction, DBI_Table mostDerivedTable, DBI_Path costPath, TypeInfo unitCostTypeInfo)
				: base(identification, mostDerivedTable) {
				DBI_Column qColumn = TIGeneralMB3.QuantityColumn(MostDerivedTable);
				pQuantityPath = qColumn == null ? null : new DBI_Path(qColumn);
				CostPath = costPath;
				UnitCostTypeInfo = unitCostTypeInfo;
				NullableUnitCostTypeInfo = unitCostTypeInfo?.UnionCompatible(NullTypeInfo.Universe);
				Correction = correction;
				DBI_Column correctionColumn = MostDerivedTable.Columns[KB.I("CorrectionID")];
				if (correctionColumn != null) {
					OriginalRecordPath = new DBI_Path(correctionColumn);
					NetCostPath = new DBI_Path(MostDerivedTable.Columns[KB.I("CorrectedCost")]);
					if (correction)
						NetCostPath = new DBI_Path(OriginalRecordPath.PathToReferencedRow, NetCostPath);
				}

				// Not allowed to ask for a Correction on a non-correctable table
				System.Diagnostics.Debug.Assert(!Correction || Correctable);
				pThisQuantityId = KB.I("ThisQuantityId - ") + Identification.TableNameKeyLocalPart + CorrectionIdentification;
				pNewNetQuantityId = KB.I("NewNetQuantityId - ") + Identification.TableNameKeyLocalPart + CorrectionIdentification;
				ThisCostId = KB.I("ThisCostId - ") + Identification.TableNameKeyLocalPart + CorrectionIdentification;
				NewNetCostId = KB.I("NewNetCostId - ") + Identification.TableNameKeyLocalPart + CorrectionIdentification;
			}
			#endregion
			#region - Common ItemPrice-based operations
			// This must be used in conjunction with one of BuildPickedItemPriceResult...() so its Init targets exist.
			public void BuildItemPricePickerControl(DBI_Path suggestedItemPriceRecord, DBI_Path itemPathForFilter, DBI_Path vendorPathForFilter) {
				if (vendorPathForFilter != null) {
					object vendorControlId = this.AddPickerFilterControl(KB.K("Use Item Pricing or Purchase History only for this vendor"), new BoolTypeInfo(false), Fmt.SetIsSetting(false));
					AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.ItemPricing.F.VendorID)
									.Eq(new SqlExpression(vendorPathForFilter, 1))), vendorControlId, null);
				}
				object historyPricingControlId = AddPickerFilterControl(KB.K("Do not include Purchasing History"), new BoolTypeInfo(false), Fmt.SetIsSetting(false));
				AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.ItemPricing.F.TableEnum).Eq(SqlExpression.Constant((int)ViewRecordTypes.ItemPricing.PriceQuote))), historyPricingControlId, null);
				object filterId = KB.I("itemPricingFilterId");
				CreateAndCompleteUnboundPickerControl(ItemPricePickerId, KB.K("Item Pricing and Purchasing History"), Thinkage.Libraries.TypeInfo.IdTypeInfo.Universe,
					Fmt.SetBrowserFilter(BTbl.TaggedEqFilter(dsMB.Path.T.ItemPricing.F.ItemID, filterId)),
					Fmt.SetPickFrom(
						new DelayedCreateTbl(
							delegate () {
								return TIItem.ItemPricingTbl();
							}
						))
				);
				var pickerValueInit = Init.Continuous(new ControlTarget(ItemPricePickerId), new EditorPathValue(suggestedItemPriceRecord));
				// add the value setter dependent on the filter target to ensure the value is always set after the filter init runs
				Actions.Add(Init.Continuous(new InSubBrowserTarget(ItemPricePickerId, new BrowserFilterTarget(filterId)), new EditorPathValue(itemPathForFilter, 0)).AddDependentInit(pickerValueInit));
				// Init the picker from the suggestion provided to us
				Actions.Add(pickerValueInit);
			}
			public void BuildPickedItemPriceResultValueTransfers(object defaultQuantityId = null, object defaultValueId = null) {
				// Build inits to extract the selected pricing information from the picker
				// If supplied, the 'default' Ids specify sources to use if nothing is picked by the picker.
				// TODO: Make building these a separate method, and another layout that creates them as visible controls including a unit cost.
				// NOTE: The activity of the following inits must be a subset of the EdtModes for which the ItemPricePicker control is user-writeable.
				// otherwise there is the chance that EditControl might make a display instead of a browser and InSubBrowserValue will get an
				// Invalid Cast error. The ItemPricePicker control is now always a live browser so these inits can also likewise be live.
				EditorInitValue quantitySource = new InSubBrowserValue(ItemPricePickerId, new BrowserPathValue(dsMB.Path.T.ItemPricing.F.Quantity));
				EditorInitValue noHistoryPickedSource = new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse,
					(values) => values[0] == null,
					new InSubBrowserValue(ItemPricePickerId, new BrowserPathValue(dsMB.Path.T.ItemPricing.F.Id))
				);
				if (defaultQuantityId != null)
					quantitySource = new EditorCalculatedInitValue(quantitySource.TypeInfo,
						(values) => (bool)values[0] ? values[2] : values[1],
						noHistoryPickedSource,
						quantitySource,
						new Libraries.Presentation.ControlValue(defaultQuantityId));
				EditorInitValue valueSource = new InSubBrowserValue(ItemPricePickerId, new BrowserPathValue(dsMB.Path.T.ItemPricing.F.Cost));
				if (defaultValueId != null)
					valueSource = new EditorCalculatedInitValue(valueSource.TypeInfo,
						(values) => (bool)values[0] ? values[2] : values[1],
						noHistoryPickedSource,
						valueSource,
						new Libraries.Presentation.ControlValue(defaultValueId));
				Actions.Add(Init.Continuous(new ControlTarget(ItemPriceQuantityId), quantitySource));
				Actions.Add(Init.Continuous(new ControlTarget(ItemPriceValueId), valueSource));
				// If no history is picked and there are no default costing fields, clear out the Unit Cost.
				if (defaultQuantityId == null && defaultValueId == null)
					Actions.Add(Init.New(new ControlTarget(ItemPriceUnitCostId), new ConstantValue(UnitCostTypeInfo, null), noHistoryPickedSource));
			}
			public void BuildPickedItemPriceResultHiddenValues() {
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(ItemPriceQuantityId, QuantityTypeInfo));
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(ItemPriceValueId, CostTypeInfo));
			}
			#endregion
			#region StartCostingLayout/FlushCostingLayout - Set up the multi-column layout for the costing controls.
			// TODO: This is somewhat mis-named. It is a dual-purpose multi-column layout handler which either generates
			// <this record> <as corrected> layout or <quantity> <unit cost> <total cost> layout; the latter has priority when
			// a record has both quantity information and is correctable. The rows are ended by calling EndCostingRow, except for
			// <this record> rows where EndThisRecordCostingRow is called which may or may not end the row. This leaves the problem
			// of obtaining a row label. Perhaps this should be shifted to StartCostingRow form which takes the row label as a param.
			public void StartCostingLayout() {
				if (CostingRows != null || CostingColumnHeaders == null)
					return;
				CostingRowContents = new List<TblLayoutNode>();
				CostingRows = new List<TblRowNode>();
			}
			public void StartCostingRow(Key rowLabel = null) {
				CostingRowLabel = rowLabel;
			}
			public void StartCostingRow(DBI_Path rowLabelPath) {
				CostingRowLabel = rowLabelPath.Key();
			}
			public virtual void StartAsCorrectedCostingRow(Key rowLabel) {
			}
			public void EndCostingRow() {
				if (CostingRowContents == null || CostingRowContents.Count == 0)
					return;
				// TODO: Need a better source for the label.
				CostingRows.Add(TblRowNode.New(CostingRowLabel, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, CostingRowContents.ToArray()));
				CostingRowContents.Clear();
			}
			public virtual void EndThisRecordCostingRow() {
				// This is used after a "this record" value. If there is no CostingColumnHeaders etc override and "this record" and "as corrected" are side-by-side, we do nothing.
				// If the "this record" and "as corrected" must appear on separate rows (because the multi columns are used for something else like quantity/unit cost/total cost)
				// this method should be overridden to call EndCostingRow.
			}
			public void AddCostingControl(TblLeafNode control) {
				(CostingRowContents ?? DetailColumns).Add(control);
			}
			public void AddWholeRowCostingEditControl(TblLeafNode control) {
				StartCostingRow();
				AddCostingControl(control);
				for (int i = CostingColumnHeaders != null ? CostingColumnHeaders.Length : 0; --i > 0;)
					AddCostingControl(TblUnboundControlNode.EmptyECol());
				EndCostingRow();
			}
			public void FlushCostingLayout() {
				if (CostingRows == null)
					return;
				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new NonDefaultCol(), DCol.Normal, ECol.Normal }, CostingColumnHeaders, CostingRows.ToArray()));
				CostingRowContents = null;
				CostingRows = null;
			}
			protected virtual Key[] CostingColumnHeaders {
				get {
					return Correctable ? new Key[] { KB.K("This Entry"), KB.K("As Corrected") } : null;
				}
			}
			#endregion
			#region BuildCostControls
			public void BuildCostControls(object defaultSourceCost) {
				CreateSuggestedCostSelectorControl(defaultSourceCost);
				StartCostingLayout();

				// We don't show "original Net" controls because there is only one combination of New/Edit Correction/Original where they can really contain a
				// useful and sensible value (i.e. New Correction).

				// Build the costing controls.
				StartCostingRow(!Correctable ? KB.K("Cost") : KB.K("This Entry"));
				PrefaceThisTotalCostControl();
				// Create the "This record" or only Cost control
				AddCostingControl(TblColumnNode.New(CostPath, DCol.Normal, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(ThisCostId))));  // TODO: Pass the label to use

				if (Correctable) {
					EndThisRecordCostingRow();
					StartAsCorrectedCostingRow(KB.K("As Corrected"));
					PrefaceNewNetTotalCostControl();

					// Create the "New net" cost, either completing the "This cost/Net cost" row or the "Net Quantity/Net Unit Cost/Net Total Cost" row
					AddCostingControl(TblColumnNode.New(NetCostPath, DCol.Normal));
					AddCostingControl(TblUnboundControlNode.New(KB.K("Cost as corrected"), NetCostTypeInfo, new ECol(Correction ? ECol.ReadonlyInUpdateAccess : ECol.AllReadonlyAccess, ECol.RestrictPerGivenPath(CostPath, 0), Fmt.SetId(NewNetCostId))));

					SetupOldThisNewValues(KB.K("Cost before correction"), NetCostPath, ThisCostId, NewNetCostId);
				}
				EndCostingRow();
			}
			// These two methods are used by with-quantity derivations to place quantity and unit cost controls before the total cost control.
			protected virtual void PrefaceThisTotalCostControl() {
			}
			protected virtual void PrefaceNewNetTotalCostControl() {
			}
			public TblLeafNode CreateUnitCostEditControl(params ECol.ICtorArg[] attrs) {
				var allAttrs = new List<ECol.ICtorArg>(attrs);
				allAttrs.Add(ECol.RestrictPerGivenPath(CostPath, 0));
				return TblUnboundControlNode.New(UnitCostTypeInfo, new ECol(allAttrs.ToArray()));
			}
			public void AddUnitCostEditDisplay(object id, bool allowNull = false) {
				AddCostingControl(CreateUnitCostEditDisplay(id, allowNull));
			}
			public TblLeafNode CreateUnitCostEditDisplay(object id, bool allowNull = false) {
				return TblUnboundControlNode.New(allowNull ? NullableUnitCostTypeInfo : UnitCostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(id)));
			}
			protected void AddUnitCostBrowserDisplayAndEditControl<QT>(Key label, DBI_Path quantityPath, object unitCostId, DBI_Path totalCostPath, params ECol.ICtorArg[] attrs)
				where QT : struct, System.IComparable<QT> {
				AddCostingControl(TblInitSourceNode.New(
					new BrowserCalculatedInitValue(UnitCostTypeInfo,
						delegate (object[] values) {
							object totalCost = values[1];
							object quantity = values[0];
							if (totalCost == null || quantity == null)
								return null;
							QT typedQuantity = (QT)quantityPath.ReferencedColumn.EffectiveType.GenericAsNativeType(quantity, typeof(QT));
							if (Compute.Equal<QT>(typedQuantity, Compute.Zero<QT>()))
								return null;
							return Compute.Divide<QT>((decimal)totalCost, typedQuantity);
						},
						new BrowserPathValue(quantityPath),
						new BrowserPathValue(totalCostPath)),
					DCol.Normal));
				var augmentedAttrs = new List<ECol.ICtorArg>(attrs);
				augmentedAttrs.Add(Fmt.SetId(unitCostId));
				AddCostingControl(TblUnboundControlNode.New(label, UnitCostTypeInfo, new ECol(augmentedAttrs.ToArray())));
			}
			private class UnitCostSource<QT> : Source
				where QT : struct, System.IComparable<QT> {
				public UnitCostSource(TypeInfo unitCostTypeInfo, Source quantitySource, Source totalCostSource) {
					UnitCostTypeInfo = unitCostTypeInfo;
					TotalCostSource = totalCostSource;
					QuantitySource = quantitySource;
				}
				private readonly Source TotalCostSource;
				private readonly Source QuantitySource;
				private readonly TypeInfo UnitCostTypeInfo;
				public object GetValue() {
					object totalCost = TotalCostSource.GetValue();
					object quantity = QuantitySource.GetValue();
					if (totalCost == null || quantity == null)
						return null;
					QT typedQuantity = (QT)QuantitySource.TypeInfo.GenericAsNativeType(quantity, typeof(QT));
					if (Compute.Equal<QT>(typedQuantity, Compute.Zero<QT>()))
						return null;
					return Compute.Divide<QT>((decimal)totalCost, typedQuantity);
				}
				public Thinkage.Libraries.TypeInfo.TypeInfo TypeInfo {
					get {
						return UnitCostTypeInfo;
					}
				}
			}
			#endregion
			#region Internal Helper methods
			#region - SetupOldThisNewValues - Set up the Inits/Checks to maintain the "New Net" control from the "This" control and the "Old Net" path
			protected void SetupOldThisNewValues(Key oldNetLabel, DBI_Path oldNetPath, object thisControlId, object newNetControlId) {
				// The newNetControl should be an unbound control of the appropriate type.
				// A separate bound control node is required for the browser panel.

				// In edit mode the net cost displays the calculated Corrected Cost field obtained from the Original Cost storage.
				Actions.Add(Init.New(new ControlTarget(newNetControlId), new EditorPathValue(oldNetPath), null,
					TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.Edit)));
				if (!Correction)
					// For original records in New mode the corrected quantity echoes the Quantity; there are no corrections so Net == This
					Actions.Add(Init.ContinuousNew(new ControlTarget(newNetControlId), new ControlUncheckedValue(thisControlId)));
				else {
					// For Correction records in New mode we have a hidden value bound to the NetQuantityPath.
					// We use a Check3 to maintain Old Net Quantity + This Quantity = New Net Quantity
					TypeInfo valType = oldNetPath.ReferencedColumn.EffectiveType;
					if (valType is IntervalTypeInfo)
						// Do a Duration one
						AddNewModeOriginalCorrectionNetSummingTriple<System.TimeSpan>(oldNetLabel, oldNetPath, thisControlId, newNetControlId);
					else if (valType is IntegralTypeInfo)
						// Do a Count one
						AddNewModeOriginalCorrectionNetSummingTriple<long>(oldNetLabel, oldNetPath, thisControlId, newNetControlId);
					else if (valType is CurrencyTypeInfo)
						// Do a Cost one
						AddNewModeOriginalCorrectionNetSummingTriple<decimal>(oldNetLabel, oldNetPath, thisControlId, newNetControlId);
					else
						System.Diagnostics.Debug.Assert(false, "SetupOldThisNewValues: Unknown type");
				}
			}
			private void AddNewModeOriginalCorrectionNetSummingTriple<T>(Key oldNetLabel, DBI_Path oldNetPath, object thisControlId, object newNetControlId)
				where T : struct, System.IComparable<T> {
				Actions.Add(new Check3<T?, T?, T?>()
					.Operand1(oldNetLabel, oldNetPath)
					.Operand2(thisControlId,
						delegate (T? original, T? net) {
							return !original.HasValue || !net.HasValue ? null : checked(Compute.Subtract<T>(net, original));
						}, NewAndCloneOnly)
					.Operand3(newNetControlId,
						delegate (T? original, T? correction) {
							return !original.HasValue || !correction.HasValue ? null : checked(Compute.Add<T>(original, correction));
						}, NewAndCloneOnly));
			}
			#endregion
			#endregion

			#region SuggestedQuantity handling
			private List<SuggestedValueSource> SuggestedQuantitySources = new List<SuggestedValueSource>();
			public void AddQuantitySuggestion(SuggestedValueSource info) {
				SuggestedQuantitySources.Add(info);
			}
			protected void CreateSuggestedQuantitySelectorControl(object defaultSourceQuantity) {
				TblLeafNode suggestedValueChoice = CreateSuggestedValueSelectorControl(
					new PathTarget(QuantityPath, 0),
					SetQuantityControlsReadonly,
					SuggestedQuantitySources,
					defaultSourceQuantity,
					KB.K("Manually Enter Quantity"),
					KB.I("SuggestedQuantityControlId"));
				if (suggestedValueChoice != null)
					DetailColumns.Add(suggestedValueChoice);
			}
			protected virtual void SetQuantityControlsReadonly(EditorInitValue readonlyValue, Key readonlyExplanationTip) {
				// Build the action forcing the bound cost control readonly if the check mark is set. The control is EditReadonly so it is automatically
				// readonly in Edit mode and we only have to do this Init in New mode.
				Actions.Add(Init.ContinuousNew(new ControlReadonlyTarget(ThisQuantityId, readonlyExplanationTip), readonlyValue));
				if (Correction)
					// Build the action forcing the net quantity control readonly if the check mark is set. For original records this control is always
					// readonly, and the control doesn't exist for non-correctable records.
					Actions.Add(Init.ContinuousNew(new ControlReadonlyTarget(NewNetQuantityId, readonlyExplanationTip), readonlyValue));
			}
			#endregion
			#region SuggestedCost handling
			private List<SuggestedValueSource> SuggestedCostSources = new List<SuggestedValueSource>();
			public void AddCostSuggestion(SuggestedValueSource info) {
				SuggestedCostSources.Add(info);
			}
			protected void CreateSuggestedCostSelectorControl(object defaultSourceCost) {
				TblLeafNode suggestedValueChoice = CreateSuggestedValueSelectorControl(
					new PathTarget(CostPath, 0),
					SetCostControlsReadonly,
					SuggestedCostSources,
					defaultSourceCost,
					KB.K("Manually Enter Cost"),
					KB.I("SuggestedCostControlId"));
				if (suggestedValueChoice != null)
					AddWholeRowCostingEditControl(suggestedValueChoice);
			}
			protected virtual void SetCostControlsReadonly(EditorInitValue readonlyValue, Key readonlyExplanationTip) {
				// Build the action forcing the bound cost control readonly if the check mark is set. The control is EditReadonly so it is automatically
				// readonly in Edit mode and we only have to do this Init in New mode.
				Actions.Add(Init.ContinuousNew(new ControlReadonlyTarget(ThisCostId, readonlyExplanationTip), readonlyValue));
				if (Correction)
					// Build the action forcing the net quantity control readonly if the check mark is set. For original records this control is always
					// readonly, and the control doesn't exist for non-correctable records.
					Actions.Add(Init.ContinuousNew(new ControlReadonlyTarget(NewNetCostId, readonlyExplanationTip), readonlyValue));
			}
			#endregion
			#region Members
			#region - Information computed based on the ctor arguments
			private readonly TypeInfo UnitCostTypeInfo;
			private readonly TypeInfo NullableUnitCostTypeInfo;
			protected DBI_Path QuantityPath {
				get {
					System.Diagnostics.Debug.Assert(pQuantityPath != null, "DerivationTblCreatorWithQuantityAndCostBase.QuantityPath referenced when schema has no Quantity");
					return pQuantityPath;
				}
			}
			protected readonly DBI_Path pQuantityPath;
			public readonly bool Correction;
			public readonly DBI_Path OriginalRecordPath;
			public readonly DBI_Path CostPath;              // This (usually same as AccountingCostPath) is used to bind the Total Cost control and browser display.
			public readonly DBI_Path NetCostPath;
			public TypeInfo NetCostTypeInfo {
				get {
					return NetCostPath.ReferencedColumn.EffectiveType;
				}
			}
			public bool Correctable {
				get {
					return OriginalRecordPath != null;
				}
			}
			public TypeInfo QuantityTypeInfo {
				get {
					return QuantityPath.ReferencedColumn.EffectiveType;
				}
			}
			public TypeInfo NullableQuantityTypeInfo {
				get {
					return QuantityTypeInfo.UnionCompatible(NullTypeInfo.Universe);
				}
			}
			public TypeInfo CostTypeInfo {
				get {
					return CostPath?.ReferencedColumn.EffectiveType;
				}
			}
			public TypeInfo NullableCostTypeInfo {
				get {
					return CostTypeInfo?.UnionCompatible(NullTypeInfo.Universe);
				}
			}
			#endregion
			#region - The NodeId's we use
			public readonly object ThisCostId;
			protected readonly object NewNetCostId;
			protected object ThisQuantityId {
				get {
					System.Diagnostics.Debug.Assert(pQuantityPath != null, "DerivationTblCreatorWithThisQuantityAndCostBase.ThisQuantityId referenced when schema has no Quantity");
					return pThisQuantityId;
				}
			}
			private readonly object pThisQuantityId;
			protected object NewNetQuantityId {
				get {
					System.Diagnostics.Debug.Assert(pQuantityPath != null, "DerivationTblCreatorWithThisQuantityAndCostBase.NewNetQuantityId referenced when schema has no Quantity");
					return pNewNetQuantityId;
				}
			}
			private readonly object pNewNetQuantityId;
			// Costing values derived from a selected ItemPrice record and the ItemPrice picker itself
			public static readonly object ItemPriceValueId = KB.I("ItemPriceValueId");
			public static readonly object ItemPriceQuantityId = KB.I("ItemPriceQuantityId");
			protected static readonly object ItemPriceUnitCostId = KB.I("ItemPriceUnitCostId");
			protected string CorrectionIdentification {
				get {
					return Correction ? KB.I(" Correction") : "";
				}
			}
			public static readonly object ItemPricePickerId = KB.I("ItemPricePickerId");
			#endregion
			#region - Labels
			protected Key QuantityLabel {
				get {
					return QuantityTypeInfo is IntervalTypeInfo ? KB.K("Hours") : KB.K("Quantity");
				}
			}
			protected Key QuantityBeforeCorrectionLabel {
				get {
					return QuantityTypeInfo is IntervalTypeInfo ? KB.K("Hours before correction") : KB.K("Quantity before correction");
				}
			}

			#endregion
			private List<TblLayoutNode> CostingRowContents = null;
			private List<TblRowNode> CostingRows = null;
			private Key CostingRowLabel = null;
			#endregion
		}
		#endregion

		#region AssignmentDerivationTblCreator
		/// <summary>
		/// Used to implement xxxxAssignment Tbls, with picker support for 
		/// </summary>
		protected class AssignmentDerivationTblCreator : DerivationTblCreatorBase {
			public AssignmentDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable)
				: base(identification, mostDerivedTable) {
			}
			#region GetTbl - add the final actions and accounting transaction info and build & return the tbl
			// Note that we put in the ECol ourselves.
			public override Tbl GetTbl(params Tbl.IAttr[] tblAttrs) {
				TblAttributes.Add(new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete)));
				return base.GetTbl(tblAttrs);
			}
			#endregion
		}
		#endregion
		// TODO: The POLineTblDerivationCreator and AccountingTransactionDerivationTblCreator share some common functionality,
		// POLineTblDerivationCreator.HandleSuggestedValue which has not been distilled out in AccountingTransactionDerivationTblCreator yet.
		#region Common AccountingTransaction handling
		#region AccountingTransactionDerivationTblCreator
		protected class AccountingTransactionDerivationTblCreator : DerivationTblCreatorWithQuantityAndCostBase {
			#region Usage Comment
			// This class is used to build derivations of the accounting transaction record.
			//
			// The general model for building the tbl is:
			// Build one of these objects.
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// If a cost calculation exists:
			//		Build controls and actions to produce a cost suggestion
			//		Call this.RegisterSuggestedCostSource to create the controls to take the sggested cost into the common calculations
			// else
			//		Call BuildCostControls to create the actual costing control(s)
			// Set the two paths that yield the from/to CostCenter ID's. The 'To' path can be null if the calling browser has supplied an Init for the To C/C.
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// Call this.GetTbl to actually get the Tbl for the record.
			//
			// Things we assume the calling browser will do:
			// If we have no path to find a "to" c/c, the browser will have a init setting it (original/noncorrectable records only)
			// We assume the browser has inited the CorrectionIDColumn to link to the record being corrected (correction records only).
			#endregion
			#region Construction
			// This flag controls the form of range-checking that will occur on the Cost field (and Quantity in the derived class)
			// AnyDelta indicates that the cost (and quantity if any) are deltas to be totalled somewhere, and that they can have either sign;
			//		this is usually used for self-correcting records (e.g. ItemAdjustment)
			// PositiveDelta indicates that the cost (and quantity if any) are deltas to be totalled somewhere, but that they cannot contain negative values
			//		for the net sum of the original record and its corrections (if any [can] exist)
			public enum ValueInterpretations {
				AbsoluteTangibleSetting, AnyDelta, PositiveDelta
			}

			public AccountingTransactionDerivationTblCreator(Tbl.TblIdentification identification, bool correction, DBI_Table mostDerivedTable, ValueInterpretations valueInterpretation, DBI_Path costPath, TypeInfo unitCostTypeInfo)
				: base(identification, correction, mostDerivedTable, costPath == null ? dsMB.Path.T.AccountingTransaction.F.Cost.ReOrientFromRelatedTable(mostDerivedTable) : costPath, unitCostTypeInfo) {
				AccountingCostPath = dsMB.Path.T.AccountingTransaction.F.Cost.ReOrientFromRelatedTable(MostDerivedTable);
				ValueInterpretation = valueInterpretation;
			}
			/// <summary>
			/// Call this first after the CTOR to form the basis of all Accounting style Tbls
			/// </summary>
			public void BuildCommonAccountingHeaderControls() {
				// Create the start of the layout: Record type and date information from the base AccountingTransaction record
				DetailColumns.Add(TblFixedRecordTypeNode.New());
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.AccountingTransaction.F.EntryDate.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					ECol.AllReadonly,
					new NonDefaultCol()));
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.AccountingTransaction.F.EffectiveDate.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(EffectiveDateId)),
					new NonDefaultCol()));
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.AccountingTransaction.F.UserID.F.ContactID.F.Code.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					ECol.AllReadonly,
					new NonDefaultCol()));
			}
			#endregion

			#region Cost Center init source paths
			// The following should be called just before calling GetTbl. They can be a path to a C/C or to an ItemLocation. In the latter case the path is continued
			// to ActualItemLocation.CostCenterID
			public virtual void SetFromCCSourcePath(DBI_Path path) {
				FromCCSourcePath = path;
			}
			private DBI_Path FromCCSourcePath;
			public virtual void SetToCCSourcePath(DBI_Path path) {
				ToCCSourcePath = path;
			}
			private DBI_Path ToCCSourcePath;
			#endregion
			#region GetTbl - add the final actions and accounting transaction info and build & return the tbl
			// Note that we put in the ECol ourselves.
			public List<ETbl.ICtorArg> ETblArgs = new[] { (ETbl.ICtorArg)ETbl.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete) }.ToList();
			public override Tbl GetTbl(params Tbl.IAttr[] tblAttrs) {
				TblAttributes.Add(new ETbl(ETblArgs.ToArray()));

				FlushCostingLayout();
				// Add Checkers on the values based on the ValueInterpretation.
				if (ValueInterpretation != ValueInterpretations.AnyDelta)
					// For the positive-only records insist that the Only/Net values be positive.
					ForbidNonNegativeValues();

				// Finish off the layout with the Cost Center information from the transaction
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.AccountingTransaction.F.FromCostCenterID.F.Code.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					ECol.AllReadonly,
					CommonNodeAttrs.PermissionToViewAccounting,
					AccountingFeatureArg));
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.AccountingTransaction.F.ToCostCenterID.F.Code.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					ECol.AllReadonly,
					CommonNodeAttrs.PermissionToViewAccounting,
					AccountingFeatureArg));
				// Prime the UserID of the transaction
				Actions.Add(Init.OnLoadNew(new PathTarget(dsMB.Path.T.AccountingTransaction.F.UserID.ReOrientFromRelatedTable(MostDerivedTable)), new UserIDValue()));

				if (Correction) {
					// Add the Inits that copy Original record information to the Correction record.
					var prefix = new DBI_PathToRow(MostDerivedTable);
					for (;;) {
						foreach (DBI_Column c in prefix.ReferencedTable.Columns) {
							DBI_Path pathToC = new DBI_Path(prefix, c);
							if (!PathShouldNotBeCopiedToCorrection(pathToC))
								Actions.Add(Init.OnLoadNewClone(pathToC, new EditorPathValue(new DBI_Path(OriginalRecordPath.PathToReferencedRow, pathToC))));
						}
						if (prefix.ReferencedTable.VariantBaseTable == null)
							break;
						prefix = new DBI_Path(prefix, prefix.ReferencedTable.VariantBaseRecordIDColumn).PathToReferencedRow;
					}
				}
				else {
					// Initialize the basic Accounting Transaction fields.
					Actions.Add(Init.ContinuousNewClone(dsMB.Path.T.AccountingTransaction.F.FromCostCenterID.ReOrientFromRelatedTable(MostDerivedTable), new EditorPathValue(FromCCSourcePath)));
					if (ToCCSourcePath != null)
						Actions.Add(Init.ContinuousNewClone(dsMB.Path.T.AccountingTransaction.F.ToCostCenterID.ReOrientFromRelatedTable(MostDerivedTable), new EditorPathValue(ToCCSourcePath)));
					if (Correctable)
						Actions.Add(Init.OnLoadNewClone(OriginalRecordPath, new EditorPathValue(new DBI_Path(MostDerivedTable.InternalIdColumn))));
				}
				return base.GetTbl(tblAttrs);
			}
			protected virtual bool PathShouldNotBeCopiedToCorrection(DBI_Path p) {
				if (!p.ReferencedColumn.IsWriteable)
					return true;
				if (p.ReferencedColumn.LinkageType == DBI_Relation.LinkageTypes.Base)
					return true;
				if (p.ReferencedColumn == dsMB.Schema.T.AccountingTransaction.F.Cost)
					return true;
				if (p.ReferencedColumn == dsMB.Schema.T.AccountingTransaction.F.EntryDate)
					return true;
				if (p.ReferencedColumn == dsMB.Schema.T.AccountingTransaction.F.UserID)
					return true;
				if (p.ReferencedColumn == OriginalRecordPath.Column)
					return true;
				return false;
			}
			protected virtual void ForbidNonNegativeValues() {
				// Net/Only: $ >= 0
				object costId = ThisCostId;
				if (Correctable)
					costId = NewNetCostId;

				Actions.Add(new Check1<decimal?>(
					delegate (decimal? cost) {
						if (cost.HasValue && cost.Value < 0)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Cost must not be negative")));
						return null;
					})
					.Operand1(costId)
				);
			}
			#endregion
			#region Helper methods
			#region - ActualSubstitution
			/// <summary>
			/// This method is used to create the control required when an Actual (or any AccountingTransation derivation) contains a field which is normally a copy
			/// of a field in an underlying demand or order but which the user is actuall allowed to change. This creates the edit control and browser panel display,
			/// and also arranges to init the value at the appropriate time(s).
			/// </summary>
			/// <param name="caption"></param>
			/// <param name="boundPath"></param>
			/// <param name="browserDisplayPath"></param>
			/// <param name="defaultSourcePath"></param>
			public void ActualSubstitution(DBI_Path boundPath, DBI_Path browserDisplayPath, DBI_Path defaultSourcePath) {
				DetailColumns.Add(TblColumnNode.New(boundPath, new DCol(Fmt.SetDisplayPath(browserDisplayPath)), Correction ? ECol.AllReadonly : ECol.ReadonlyInUpdate));
				if (!Correction)
					// If this is not a Correction, we init the value from the given default path for New records.
					// The init is continuous so if the user changes the demand/order selection the actual selection will change too, but the user can then
					// override it.
					// If this is a correction the value is copied from the original record and we only display it.
					Actions.Add(Init.ContinuousNew(boundPath, new EditorPathValue(defaultSourcePath)));
			}
			#endregion
			#region - BuildCommonNonPOActualControls
			public void BuildCommonNonPOActualControls(DBI_Path pickedPath, params DBI_Path[] extraDisplayInfo) {
				TblLayoutNode[] contents = new TblLayoutNode[extraDisplayInfo.Length + 2];
				contents[0] = TblColumnNode.New(pickedPath, Correction ? ECol.AllReadonly : ECol.Normal);
				contents[1] = TblColumnNode.New(new DBI_Path(pickedPath.PathToReferencedRow, dsMB.Path.T.Demand.F.WorkOrderID.F.Number.ReOrientFromRelatedTable(pickedPath.ReferencedColumn.ConstrainedBy.Table)), DCol.Normal, ECol.AllReadonly);
				for (int i = extraDisplayInfo.Length; --i >= 0;)
					contents[i + 2] = TblColumnNode.New(new DBI_Path(pickedPath.PathToReferencedRow, extraDisplayInfo[i]), DCol.Normal, ECol.AllReadonly);
				DetailColumns.Add(TblGroupNode.New(pickedPath, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, contents));
			}
			#endregion
			#endregion
			#region Members
			// CTOR settings
			public readonly ValueInterpretations ValueInterpretation;
			public readonly DBI_Path AccountingCostPath;    // This is the cost path in the AccountingTransaction (always)
															// NodeIds
			protected static readonly object EffectiveDateId = KB.I("EffectiveDateId");
			#endregion
		}
		#endregion
		#region AccountingTransactionWithQuantityDerivationTblCreator<QT>
		protected class AccountingTransactionWithQuantityDerivationTblCreator<QT> : AccountingTransactionDerivationTblCreator
			where QT : struct, System.IComparable<QT> {
			#region Usage Comment
			// This class is used to build derivations of the accounting transaction record which involve a quantity (of a resource).
			//
			// The general model for building the tbl is:
			// Build one of these objects.
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// If there is a Demand or POLine to suggest a quantity:
			//		call BuildXxxxxQuantityDisplaysAndSuggestQuantity
			// else if there is some custom quantity suggestion:
			//		Build controls to produce a quantity suggestion
			//		Call this.HandleSuggestedQuantityAndBuildQuantityControls to create the controls to take the sggested quantity into the common calculations
			// else
			//		Call this.BuildQuantityControls to create the actual quantity controls
			// Optionally:
			//		Build controls and actions to produce a cost basis (either an quantity and total cost, or a unit cost)
			//		Call this.HandleCostSuggestionWithBasisCost to create the controls to produce a sggested cost and include it in the common calculations;
			//			this includes proper handling of negative quantities on corrections (i.e. returns)
			// Call BuildCostControls to create the actual costing control(s)
			// Set the two paths that yield the from/to CostCenter ID's. The 'To' path can be null if the calling browser has supplied an Init for the To C/C.
			//		If either or both of these paths is a path to a ItemLocation instead we also add checks for overdraft from the itemlocation. This feature
			//		should perhaps be in a further derivation of this class specifically for *tangible* resources...
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// Call this.GetTbl to actually get the Tbl for the record.
			#endregion
			#region Construction
			public AccountingTransactionWithQuantityDerivationTblCreator(Tbl.TblIdentification identification, bool correction, DBI_Table mostDerivedTable, ValueInterpretations valueInterpretation, bool echoQuantity, TypeInfo unitCostTypeInfo)
				: this(identification, correction, mostDerivedTable, valueInterpretation, echoQuantity, null, unitCostTypeInfo) {
			}
			// Only direct call to this is from ItemCountValue TblCreator; everyone else uses the 'null' value argument
			public AccountingTransactionWithQuantityDerivationTblCreator(Tbl.TblIdentification identification, bool correction, DBI_Table mostDerivedTable, ValueInterpretations valueInterpretation, bool echoQuantity, DBI_Path costPath, TypeInfo unitCostTypeInfo)
				: base(identification, correction, mostDerivedTable, valueInterpretation, costPath, unitCostTypeInfo) {
				EchoQuantity = echoQuantity;
				if (Correctable) {
					NetQuantityPath = new DBI_Path(MostDerivedTable.Columns[KB.I("CorrectedQuantity")]);
					if (correction)
						NetQuantityPath = new DBI_Path(OriginalRecordPath.PathToReferencedRow, NetQuantityPath);
				}
				QuantityForNetCostingId = KB.I("QuantityForNetCostingId - ") + Identification.TableNameKeyLocalPart;
				System.Diagnostics.Debug.Assert(QuantityTypeInfo.GenericAcceptedNativeType(typeof(QT)), "AccountingTransactionWithQuantityDerivationTblCreator: QT incompatible with QuantityTypeInfo");
			}
			#endregion
			#region InnerFlushCostingLayout - Set up the multi-column layout for the costing controls.
			public override void StartAsCorrectedCostingRow(Key rowLabel) {
				StartCostingRow(rowLabel);
			}
			public override void EndThisRecordCostingRow() {
				EndCostingRow();
			}
			protected override Key[] CostingColumnHeaders {
				get {
					return new Key[] { QuantityLabel, QuantityTypeInfo is IntervalTypeInfo ? KB.K("Hourly Rate") : KB.K("Unit Cost"), KB.K("Total Cost") };
				}
			}
			#endregion
			#region Build...QuantityDisplaysAndSuggestRemainingQuantity (specific quantity suggesters)
			#region - BuildDemandQuantityDisplaysAndSuggestRemainingQuantity
			public void BuildDemandQuantityDisplaysAndSuggestRemainingQuantity(DBI_Path pathToDerivedDemandQuantityDemanded, DBI_Path pathToDerivedDemandQuantityActualized) {
				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Demanded"), AlreadyUsed, KB.K("Remaining Demand") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(pathToDerivedDemandQuantityDemanded, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredQuantityId))),
						TblColumnNode.New(pathToDerivedDemandQuantityActualized, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(AlreadyUsedQuantityId))),
						TblUnboundControlNode.New(pathToDerivedDemandQuantityActualized.ReferencedColumn.EffectiveType, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CalculatedQuantityId)))
					)
				));
				Actions.Add(RemainingCalculator<QT>(RequiredQuantityId, AlreadyUsedQuantityId, CalculatedQuantityId));

				// Declare the quantity suggestion
				AddQuantitySuggestion(new SuggestedValueSource(KB.K("Use all remaining demand"), CalculatedQuantityId, KB.K("Using all remaining demand")));
				// Define the quantity(ies)
				BuildQuantityControls();
			}
			#endregion
			#region - BuildPOLineQuantityDisplaysAndSuggestRemainingQuantity
			public void BuildPOLineQuantityDisplaysAndSuggestRemainingQuantity(DBI_Path pathToQuantityOrdered, DBI_Path pathToQuantityReceived, DBI_Path pathToCostOrdered, DBI_Path pathToCostReceived,
				DBI_PathToRow pathToDemand, DBI_Path pathToDemandQuantity) {
				// Define the cost suggestion within the creator's zone of 3-column layout: Quantity, Unit Cost, Total Cost.
				StartCostingLayout();

				// The first row describes the ordered situation.
				StartCostingRow(KB.K("Ordered"));
				AddCostingControl(TblColumnNode.New(pathToQuantityOrdered, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(OrderedQuantityId))));
				AddUnitCostEditDisplay(OrderedUnitCostId);
				AddCostingControl(TblColumnNode.New(pathToCostOrdered, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(OrderedValueId))));
				EndCostingRow();
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(OrderedQuantityId, OrderedValueId, OrderedUnitCostId));

				// The second row describes the already received situation.
				StartCostingRow(KB.K("Already Received"));
				AddCostingControl(TblColumnNode.New(pathToQuantityReceived, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ReceivedQuantityId))));
				AddUnitCostEditDisplay(ReceivedUnitCostId);
				AddCostingControl(TblColumnNode.New(pathToCostReceived, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ReceivedValueId))));
				EndCostingRow();
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(ReceivedQuantityId, ReceivedValueId, ReceivedUnitCostId));

				// The third row describes the still-to-receive situation.
				StartCostingRow(KB.K("Order Remaining"));
				AddCostingControl(TblUnboundControlNode.New(QuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RemainingQuantityId))));
				AddUnitCostEditDisplay(RemainingUnitCostId);
				AddCostingControl(TblUnboundControlNode.New(CostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RemainingValueId))));
				EndCostingRow();
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(RemainingQuantityId, RemainingValueId, RemainingUnitCostId));
				Actions.Add(RemainingCalculator<QT>(OrderedQuantityId, ReceivedQuantityId, RemainingQuantityId));
				Actions.Add(RemainingCalculator<decimal>(OrderedValueId, ReceivedValueId, RemainingValueId));

				FlushCostingLayout();

				// Finally, some hidden controls provide the still-to-receive values if # > 0, otherwise the already-received values.
				// This means that as long as you are receiving less than ordered, the suggested cost will get you to a full order at the ordered price.
				// If you receive extras, though, you get them priced at the same as the received price for the originally-ordered quantity (if that was overridden)
				object AdditionalReceivingBasisQuantityId = KB.I("AdditionalReceivingBasisQuantityId");
				object AdditionalReceivingBasisCostId = KB.I("AdditionalReceivingBasisCostId");
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(AdditionalReceivingBasisQuantityId, QuantityTypeInfo));
				AddConditionalCopyOperation<QT>(RemainingQuantityId, RemainingQuantityId, pathToQuantityReceived, false, AdditionalReceivingBasisQuantityId);

				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(AdditionalReceivingBasisCostId, CostTypeInfo));
				AddConditionalCopyOperation<decimal>(RemainingQuantityId, RemainingValueId, pathToCostReceived, false, AdditionalReceivingBasisCostId);

				// Declare the quantity suggestion
				AddQuantitySuggestion(new SuggestedValueSource(KB.K("Use order remaining quantity"), RemainingQuantityId, KB.K("Using calculated remaining order quantity")));
				// Define the quantity(ies)
				BuildQuantityControls();

				StartCostingLayout();
				// The next row costs out This Quantity to a total cost.
				HandleCostSuggestionWithBasisCost(KB.K("Calculated Remaining Order Cost"), AdditionalReceivingBasisQuantityId, AdditionalReceivingBasisCostId,
					new SuggestedValueSource(KB.K("Use calculated remaining order cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated remaining order cost")));
				if (pathToDemand != null) {
					// The second row describes the demand estimate values
					StartCostingRow(DemandedCost);
					AddCostingControl(TblColumnNode.New(pathToDemandQuantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisQuantityId))));
					AddUnitCostEditDisplay(CostEstimateBasisUnitCostId);
					AddCostingControl(TblColumnNode.New(new DBI_Path(pathToDemand, dsMB.Path.T.Demand.F.CostEstimate), new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostEstimateBasisValueId))));
					EndCostingRow();
					Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(CostEstimateBasisQuantityId, CostEstimateBasisValueId, CostEstimateBasisUnitCostId));
					HandleCostSuggestionWithBasisCost(DemandedCost, CostEstimateBasisQuantityId, CostEstimateBasisValueId,
						new AccountingTransactionDerivationTblCreator.SuggestedValueSource(UseDemandedCost, CostEstimationValueId, UsingDemandedCost));
					BuildCostControls(new DBI_Path(pathToDemand, dsMB.Path.T.Demand.F.DemandActualCalculationInitValue));
				}
				else
					BuildCostControls(CostCalculationValueSuggestedSourceId);
			}
			#endregion
			#endregion
			#region BuildQuantityControls - Build the Quantity control(s) that actually bind to the record.
			// This should only be called if the specific transaction defines a quantity.
			// This defines the quantity entry controls.
			public void BuildQuantityControls() {
				CreateSuggestedQuantitySelectorControl(null);
				if (!Correctable)
					// For Noncorrectable records we just have a single control bound to the QuantityColumn
					DetailColumns.Add(TblColumnNode.New(QuantityPath, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(ThisQuantityId))));
				else {
					// For correctable records we show two values: The value from this record and the (new) net value.
					DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("This Entry"), KB.K("As Corrected") },
						TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblColumnNode.New(QuantityPath, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(ThisQuantityId))),
							TblUnboundControlNode.New(NetQuantityTypeInfo, new ECol(Correction ? ECol.ReadonlyInUpdateAccess : ECol.AllReadonlyAccess, ECol.RestrictPerGivenPath(QuantityPath, 0), Fmt.SetId(NewNetQuantityId)))
						)
					));
					SetupOldThisNewValues(QuantityBeforeCorrectionLabel, NetQuantityPath, ThisQuantityId, NewNetQuantityId);
				}
			}
			#endregion
			#region SetCostControlsReadonly
			protected override void SetCostControlsReadonly(EditorInitValue readonlyValue, Key readonlyExplanationTip) {
				base.SetCostControlsReadonly(readonlyValue, readonlyExplanationTip);
				// Build the action forcing the unit cost control readonly if the check mark is set. The control is EditReadonly so it is automatically
				// readonly in Edit mode and we only have to do this Init in New mode.
				Actions.Add(Init.ContinuousNew(new ControlReadonlyTarget(ThisUnitCostId, readonlyExplanationTip), readonlyValue));
				if (Correction)
					// Build the action forcing the net unit control readonly if the check mark is set. For original records this control is always
					// readonly, and the control doesn't exist for non-correctable records.
					Actions.Add(Init.ContinuousNew(new ControlReadonlyTarget(NewUnitNetCostId, readonlyExplanationTip), readonlyValue));
			}
			#endregion
			#region HandleCostSuggestion - Create the columns/actions necessary for dealing with calculated suggested costs
			// For records with Quantity this declares the normal costing basis (for positive quantities) given a basis quantity and basis total cost as control id's.
			// This should be used where the Quantity is an integral value.
			// This builds a cost calculator (that also accounts for reversals in corrections) and uses its result as a suggestion.
			// TODO: Do we want forms of this that take paths?
			public void HandleCostSuggestionWithBasisCost(Key costingRowLabel, object basisQuantityId, object basisCostId, SuggestedValueSource info) {
				InnerHandleCostSuggestion(costingRowLabel, basisQuantityId, basisCostId, info);
			}
			// For records with Quantity this declares the normal costing basis (for positive quantities) given a basis unit cost as control id's
			// This should be used where the Quantity is a time span.
			// TODO: Do we want a form of this that takes a path?
			// TODO: Several callers to this (PO forms) should not be using this because they actually have a total cost and quantity from the PO line, not a unit cost.
			public void HandleCostSuggestionWithUnitCost(SimpleKey costingRowLabel, object unitValue, object basisUnitCostId, SuggestedValueSource info) {
				// Make a hidden basis Quantity containing 1 'unit', and treat the given Unit Cost as a Total Cost
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(DeemedPricingQuantityId, QuantityTypeInfo));
				Actions.Add(Init.OnLoad(new ControlTarget(DeemedPricingQuantityId), new ConstantValue(unitValue)));
				InnerHandleCostSuggestion(costingRowLabel, DeemedPricingQuantityId, basisUnitCostId, info);
			}
			#region BasisQuantityId
			private object BasisQuantityId {
				get {
					if (pBasisQuantityId == null) {
						// Keep a hidden quantity value inited from the ThisQuantityId (unchecked) so we always have a valid value to compute the suggested Cost values based on what the user entered
						pBasisQuantityId = KB.I("BasisQuantityId");
						DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(pBasisQuantityId, QuantityTypeInfo));
						Actions.Add(Init.Continuous(new ControlTarget(pBasisQuantityId), new ControlUncheckedValue(ThisQuantityId)));
					}
					return pBasisQuantityId;
				}
			}
			private object pBasisQuantityId;
			#endregion

			private void InnerHandleCostSuggestion(Key costingRowLabel, object basisQuantityId, object basisCostId, SuggestedValueSource sourceInfo) {
				// node ids for internal calculation
				string costCalculationUnitCostId = KB.I("BasisCostCalculationUnitCostId - ") + costingRowLabel.Translate(null);

				StartCostingRow(costingRowLabel);
				if (EchoQuantity) {
					string costCalculationQuantityId = KB.I("BasisCostCalculationQuantityId - ") + costingRowLabel.Translate(null);
					AddCostingControl(TblUnboundControlNode.New(QuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(costCalculationQuantityId))));
					// Make QuantityForCostCalculationId echo QuantityId
					Actions.Add(Init.Continuous(new ControlTarget(costCalculationQuantityId), new ControlUncheckedValue(ThisQuantityId)));
				}
				else
					AddCostingControl(TblLeafNode.Empty(ECol.Normal));

				AddUnitCostEditDisplay(costCalculationUnitCostId);
				AddCostingControl(TblUnboundControlNode.New(CostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(sourceInfo.SourceValueId))));
				EndCostingRow();
				if (Correction) {
					// Add 2 Hidden fields:
					// EffectiveCostForCostCalculationId <- Quantity >= 0 ? basisCost : OldNetCost
					// EffectiveQuantityForCostCalculationId <- Quantity >= 0 ? basisQuantity : OldNetQuantity
					string costBasisValueId = KB.I("CostBasisValueId") + costingRowLabel.Translate(null);
					string costBasisQuantityId = KB.I("CostBasisQuantityId") + costingRowLabel.Translate(null);

					DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(costBasisValueId, CostTypeInfo));
					AddConditionalCopyOperation<decimal>(BasisQuantityId, basisCostId, NetCostPath, true, costBasisValueId);

					DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(costBasisQuantityId, QuantityTypeInfo));
					AddConditionalCopyOperation<QT>(BasisQuantityId, basisQuantityId, NetQuantityPath, true, costBasisQuantityId);
					// Change the basis Quantity to the original cost basis values for corrections
					basisQuantityId = costBasisQuantityId;
					basisCostId = costBasisValueId;
				}
				// Add the calculation: Total <- Quantity * EffectiveBasisCost / EffectiveBasisQuantity
				Actions.Add(TotalFromQuantityAndPricingCalculator<QT>(BasisQuantityId, basisQuantityId, basisCostId, sourceInfo.SourceValueId));
				// Add the calculation: Unit Cost <- Total / Quantity
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(BasisQuantityId, sourceInfo.SourceValueId, costCalculationUnitCostId));
				AddCostSuggestion(sourceInfo);
			}
			#endregion
			#region BuildCostControls
			protected override void PrefaceThisTotalCostControl() {
				// Note that although these have a display of the ThisQuantity they get this directly from the record, and any calculations using the value
				// reference the bound control built by BuildQuantityControls().

				// Create the "This record" or only costing line
				AddCostingControl(TblColumnNode.New(QuantityPath, DCol.Normal));
				if (EchoQuantity) {
					AddCostingControl(TblUnboundControlNode.New(QuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(EchoThisQuantityId))));
					// Make QuantityForThisCostingId echo QuantityId
					Actions.Add(Init.Continuous(new ControlTarget(EchoThisQuantityId), new ControlUncheckedValue(ThisQuantityId)));
				}
				else
					AddCostingControl(TblLeafNode.Empty(ECol.Normal));

				AddUnitCostBrowserDisplayAndEditControl<QT>(KB.K("Unit Cost"), QuantityPath, ThisUnitCostId, CostPath, ECol.ReadonlyInUpdateAccess);
				// The Total Cost control is made in common BuildCostControls code.
				// Enforce the (quantity/Unit cost/Total cost) relationship. Note that in modes other than New mode only the Unit Cost is correctable.
				Actions.Add(new Check3<QT?, decimal?, decimal?>()
					.Operand1(ThisQuantityId,
						delegate (decimal? unit, decimal? total) {
							return !unit.HasValue || Compute.IsZero<decimal>(unit) || !total.HasValue
								? null : checked(Compute.Divide<QT>(total, unit));
						}, EdtMode.New)
					.Operand2(ThisUnitCostId,
						delegate (QT? quantity, decimal? total) {
							return !quantity.HasValue || Compute.IsZero<QT>(quantity) || !total.HasValue
								? null : checked(Compute.Divide<QT>(total, quantity));
						})
					.Operand3(ThisCostId,
						delegate (QT? quantity, decimal? unit) {
							return !quantity.HasValue || !unit.HasValue
								? null : checked(Compute.Multiply<QT>(unit, quantity));
						}, EdtMode.New)
				);
			}
			protected override void PrefaceNewNetTotalCostControl() {
				// Create the "New net" costing line (Quantity and Unit Cost only, Total Cost is done below in common code)
				AddCostingControl(TblColumnNode.New(NetQuantityPath, DCol.Normal));
				if (EchoQuantity) {
					AddCostingControl(TblUnboundControlNode.New(NetQuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(QuantityForNetCostingId))));
					// Make QuantityForNetCostingId echo QuantityId
					Actions.Add(Init.Continuous(new ControlTarget(QuantityForNetCostingId), new ControlUncheckedValue(NewNetQuantityId)));
				}
				else
					AddCostingControl(TblLeafNode.Empty(ECol.Normal));

				AddUnitCostBrowserDisplayAndEditControl<QT>(KB.K("As Corrected Unit Cost"), NetQuantityPath, NewUnitNetCostId, NetCostPath, Correction ? ECol.ReadonlyInUpdateAccess : ECol.AllReadonlyAccess, ECol.RestrictPerGivenPath(QuantityPath, 0));
				// The Total Cost control is made in common BuildCostControls code.
				// Using the normal triple control cost calculator would cause an update loop: changing the New Net Quantity would try to update the
				// New Net Total Cost directly through the triple, and also indirectly via the "This" calculator. The values could be different and there
				// is an undefined ordering (in the New Net Quantity's Notification multicast delegate) as to which occurs first.
				// We actually want the calculation to go the long way around (i.e. the latter path).
				// This is done by using an altered unit cost triple calculator which gives correcting the Unit Cost priority over
				// correcting the Total Cost (both of which already have priority over updating the Quantity).
				// As a result the above scenario updates the Unit Cost twice instead, but always from information within the Triple itself.
				// Note that in modes other than New mode only the Unit Cost is correctable.
				Actions.Add(
					new Check3<QT?, decimal?, decimal?>()
						.Operand1(NewNetQuantityId,
							delegate (decimal? total, decimal? unit) {
								return !unit.HasValue || Compute.IsZero<decimal>(unit) || !total.HasValue
									? null : checked(Compute.Divide<QT>(total, unit));
							}, EdtMode.New)
						.Operand2(NewNetCostId,
							delegate (QT? quantity, decimal? unit) {
								return !quantity.HasValue || !unit.HasValue
									? null : checked(Compute.Multiply<QT>(unit, quantity));
							}, EdtMode.New)
						.Operand3(NewUnitNetCostId,
							delegate (QT? quantity, decimal? total) {
								return !quantity.HasValue || Compute.IsZero<QT>(quantity) || !total.HasValue
									? null : checked(Compute.Divide<QT>(total, quantity));
							})
				);
			}
			#endregion
			#region Cost Center init source paths
			public override void SetFromCCSourcePath(DBI_Path path) {
				DBI_PathToRow pathToCCSourceRow = path.PathToReferencedRow;
				if (pathToCCSourceRow.ReferencedTable == dsMB.Schema.T.ItemLocation) {
					AddOverdraftLimitAction(1, pathToCCSourceRow);
					AddEffectiveDateRestrictions(pathToCCSourceRow);
					path = new DBI_Path(pathToCCSourceRow, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.CostCenterID);
				}
				base.SetFromCCSourcePath(path);
			}
			public override void SetToCCSourcePath(DBI_Path path) {
				DBI_PathToRow pathToCCSourceRow = path.PathToReferencedRow;
				if (pathToCCSourceRow.ReferencedTable == dsMB.Schema.T.ItemLocation) {
					AddOverdraftLimitAction(-1, pathToCCSourceRow);
					AddEffectiveDateRestrictions(pathToCCSourceRow);
					path = new DBI_Path(pathToCCSourceRow, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.CostCenterID);
				}
				base.SetToCCSourcePath(path);
			}
			private void AddOverdraftLimitAction(int sign, DBI_PathToRow pathToSR) {
				if (ValueInterpretation == ValueInterpretations.AbsoluteTangibleSetting)
					return;
				// Make sure we don't overdraw: Note that we treat sign*number > 0 as *reducing* inventory.
				// This/Only: sign*$ <= SR.TotalCost
				Actions.Add(new Check2<decimal?, decimal?>(
					delegate (decimal? cost, decimal? valueOnHand) {
						if (cost.HasValue && valueOnHand.HasValue && sign * cost.Value > valueOnHand.Value)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Cost must not exceed value on hand")));
						return null;
					}, TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New))
					.Operand1(ThisCostId)
					.Operand2(KB.K("Value On Hand"), new DBI_Path(pathToSR, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.TotalCost))
				);
				// This/Only: sign*# <= SR.OnHand
				Actions.Add(new Check2<QT?, QT?>(
					delegate (QT? quantity, QT? onHand) {
						if (quantity.HasValue && onHand.HasValue && Compute.Greater<QT>(Compute.Sign<QT>(sign, quantity.Value), onHand.Value))
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Quantity must not exceed quantity on hand")));
						return null;
					}, TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New))
					.Operand1(ThisQuantityId)
					.Operand2(KB.K("Quantity On Hand"), new DBI_Path(pathToSR, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.OnHand))
				);
				// This/Only: $ == SR.TotalCost || sign*# < SR.OnHand (no stranded value)
				Actions.Add(new Check4<decimal?, QT?, decimal?, QT?>(
					delegate (decimal? cost, QT? quantity, decimal? valueOnHand, QT? onHand) {
						if (quantity.HasValue && cost.HasValue && valueOnHand.HasValue && onHand.HasValue
							&& Compute.Equal<QT>(Compute.Sign<QT>(sign, quantity.Value), onHand.Value) && sign * cost.Value != valueOnHand.Value)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("This would set quantity on hand to zero but leave non-zero value")));
						return null;
					}, TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New))
					.Operand1(ThisCostId)
					.Operand2(ThisQuantityId)
					.Operand3(KB.K("Value On Hand"), new DBI_Path(pathToSR, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.TotalCost))
					.Operand4(KB.K("Quantity On Hand"), new DBI_Path(pathToSR, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.OnHand))
				);
			}
			// This could be made public for transactions that reference ItemLocations but for some reason the IL path
			// is not passed to SetTo/FromCostCenterPath (instead the actual CC path is passed).
			private void AddEffectiveDateRestrictions(DBI_PathToRow pathToItemLocation) {
				Actions.Add(new Check2<System.DateTime?, System.DateTime?>(
					delegate (System.DateTime? lastPCDate, System.DateTime? thisEffectiveDate) {
						if (thisEffectiveDate.HasValue && lastPCDate.HasValue && thisEffectiveDate.Value <= lastPCDate.Value)
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Effective date must be after latest Physical Count's effective date of {0}"), lastPCDate));
						return null;
					}, EffectiveDateRestrictionDefaultActivity, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
					.Operand1(KB.K("Latest Physical Count"), new DBI_Path(pathToItemLocation, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.PermanentItemLocationID.F.CurrentItemCountValueID.F.AccountingTransactionID.F.EffectiveDate))
					.Operand2(EffectiveDateId));
			}
			/// <summary>
			/// Default activity for EffectiveDateRestriction. In New and Clone Edtmodes the check is always continuous despite the setting of this variable.
			/// </summary>
			public TblActionNode.Activity EffectiveDateRestrictionDefaultActivity = TblActionNode.Activity.Continuous;

			#endregion
			#region GetTbl - add the final actions and accounting transaction info and build & return the tbl
			protected override bool PathShouldNotBeCopiedToCorrection(DBI_Path p) {
				if (p == QuantityPath)
					return true;
				return base.PathShouldNotBeCopiedToCorrection(p);
			}
			protected override void ForbidNonNegativeValues() {
				base.ForbidNonNegativeValues();

				// Net/Only: # >= 0
				object quantityId = ThisQuantityId;
				if (Correctable)
					quantityId = NewNetQuantityId;
				Actions.Add(new Check1<QT?>(
					delegate (QT? quantity) {
						if (quantity.HasValue && Compute.Less<QT>(quantity.Value, Compute.Zero<QT>()))
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Quantity must not be negative")));
						return null;
					})
					.Operand1(quantityId)
				);

				if (ValueInterpretation == ValueInterpretations.AbsoluteTangibleSetting) {
					// Net/Only: $ == 0 || # > 0 (no stranded value)
					object costId = ThisCostId;
					if (Correctable)
						costId = NewNetCostId;
					Actions.Add(new Check2<decimal?, QT?>(
						delegate (decimal? cost, QT? quantity) {
							if (quantity.HasValue && cost.HasValue && Compute.IsZero<QT>(quantity.Value) && cost.Value != 0)
								return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Cost must be zero when quantity is zero")));
							return null;
						})
						.Operand1(costId)
						.Operand2(quantityId)
					);
				}
			}
			#endregion
			#region Private helpers
			private void AddConditionalCopyOperation<CT>(object testOp, object ifPositiveOp, DBI_Path ifNegativeOp, bool zeroCountsPositive, object resultId)
				where CT : struct, System.IComparable<CT> {
				Check4<QT?, CT?, CT?, CT?> checker = new Check4<QT?, CT?, CT?, CT?>();
				checker.Operand1(testOp);
				checker.Operand2(ifPositiveOp);
				checker.Operand3(ifNegativeOp.Key(), ifNegativeOp);
				if (zeroCountsPositive)
					checker.Operand4(resultId,
						delegate (QT? n, CT? normal, CT? reverse) {
							if (!n.HasValue)
								return null;
							return Compute.Less<QT>(n.Value, Compute.Zero<QT>()) ? reverse : normal;
						}
					);
				else
					checker.Operand4(resultId,
						delegate (QT? n, CT? normal, CT? reverse) {
							if (!n.HasValue)
								return null;
							return Compute.LessEqual<QT>(n.Value, Compute.Zero<QT>()) ? reverse : normal;
						}
					);
				Actions.Add(checker);
			}
			#endregion
			#region Public helper methods
			#region - BuildCommonPOReceivingControls
			public void BuildCommonPOReceivingControls(DBI_Path pickedPath, DBI_Path receiptPath, params DBI_Path[] extraDisplayInfo) {
				DBI_Table derivedPOLineTable = pickedPath.ReferencedColumn.ConstrainedBy.Table;
				DBI_PathToRow pickedRowPath = pickedPath.PathToReferencedRow;
				TblLayoutNode[] contents = new TblLayoutNode[extraDisplayInfo.Length + 4];
				contents[0] = TblColumnNode.New(pickedPath, Correction ? ECol.AllReadonly : ECol.Normal);
				contents[1] = TblColumnNode.New(new DBI_Path(pickedRowPath, dsMB.Path.T.POLine.F.PurchaseOrderID.F.Number.ReOrientFromRelatedTable(derivedPOLineTable)), DCol.Normal, ECol.AllReadonly);
				contents[2] = TblColumnNode.New(new DBI_Path(pickedRowPath, dsMB.Path.T.POLine.F.PurchaseOrderID.F.VendorID.F.Code.ReOrientFromRelatedTable(derivedPOLineTable)), DCol.Normal, ECol.AllReadonly);
				contents[3] = TblColumnNode.New(new DBI_Path(pickedRowPath, dsMB.Path.T.POLine.F.PurchaseOrderText.ReOrientFromRelatedTable(derivedPOLineTable)), DCol.Normal, ECol.AllReadonly);
				for (int i = extraDisplayInfo.Length; --i >= 0;)
					contents[i + 4] = TblColumnNode.New(new DBI_Path(pickedRowPath, extraDisplayInfo[i]), DCol.Normal, ECol.AllReadonly);
				DetailColumns.Add(TblGroupNode.New(pickedPath, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, contents));
				DetailColumns.Add(TblColumnNode.New(receiptPath, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Receipt.F.Waybill)), Correction ? ECol.AllReadonly : ECol.Normal));
			}
			#endregion
			#region - Common inventory on-hand operations
			public void BuildCostingBasedOnInventory(DBI_Path pathToItemLocation) {
				BuildCommonInventoryOnHandCostControls(pathToItemLocation);
				HandleCommonInventoryCostSuggestion();
				SetFromCCSourcePath(pathToItemLocation);
			}
			public void BuildCommonInventoryOnHandCostControls(DBI_Path pathToItemLocation) {
				// The first row describes the on-hand situation.
				StartCostingRow(KB.K("On Hand"));
				AddCostingControl(TblColumnNode.New(new DBI_Path(pathToItemLocation.PathToReferencedRow, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.OnHand), new ECol(ECol.AllReadonlyAccess, Fmt.SetId(OnHandQuantityId))));
				AddUnitCostEditDisplay(OnHandUnitCostId);
				AddCostingControl(TblColumnNode.New(new DBI_Path(pathToItemLocation.PathToReferencedRow, dsMB.Path.T.ItemLocation.F.ActualItemLocationID.F.TotalCost), new ECol(ECol.AllReadonlyAccess, Fmt.SetId(OnHandValueId))));
				EndCostingRow();
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(OnHandQuantityId, OnHandValueId, OnHandUnitCostId));
			}
			public void HandleCommonInventoryCostSuggestion() {
				HandleCostSuggestionWithBasisCost(KB.K("Calculated On Hand Cost"), OnHandQuantityId, OnHandValueId,
					new SuggestedValueSource(KB.K("Use calculated On Hand cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated On Hand cost")));
			}
			#endregion
			#region - Common ItemPrice-based operations
			public void BuildPickedItemPriceResultDisplays() {
				StartCostingRow(KB.K("Pricing Basis"));
				AddCostingControl(TblUnboundControlNode.New(QuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemPriceQuantityId))));
				AddUnitCostEditDisplay(ItemPriceUnitCostId);
				AddCostingControl(TblUnboundControlNode.New(CostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemPriceValueId))));
				EndCostingRow();
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(ItemPriceQuantityId, ItemPriceValueId, ItemPriceUnitCostId));
			}
			public void HandlePickedItemCostSuggestion() {
				HandleCostSuggestionWithBasisCost(KB.K("Calculated Item Price Cost"), ItemPriceQuantityId, ItemPriceValueId,
					new SuggestedValueSource(KB.K("Use calculated item price cost"), CostCalculationValueSuggestedSourceId, KB.K("Using calculated item price cost")));
			}
			#endregion
			#endregion
			#region Members
			#region - Information computed based on the ctor arguments
			private readonly bool EchoQuantity;
			private readonly DBI_Path NetQuantityPath;
			private TypeInfo NetQuantityTypeInfo {
				get {
					return NetQuantityPath.ReferencedColumn.EffectiveType;
				}
			}
			#endregion
			#region - The NodeId's we use
			// Two of these node Id's are instance members to make sure that each Tbl uses distinct Id's. This is necessary if more than one
			// of the Tbl's we create is referenced by the CompositeViews of a CompositeTbl, which essentially puts all the Id's in the same scope.
			// The two instance Id's are the only ones the browser code and DCols require.
			// The "this" record fields.
			// Value Id in base class
			private static readonly object ThisUnitCostId = KB.I("ThisUnitCostId");
			private static readonly object EchoThisQuantityId = KB.I("EchoThisQuantityId");
			// Value Id in base class
			private static readonly object NewUnitNetCostId = KB.I("NewUnitNetCostId");
			private readonly object QuantityForNetCostingId;
			// A deemed (implied) quantity used to express resources priced by Unit Cost as a Total Cost/Quantity pair
			private static readonly object DeemedPricingQuantityId = KB.I("DeemedPricingQuantityId");
			// The values expressing inventory on-hand condition
			public static readonly object OnHandValueId = KB.I("OnHandValueId");
			private static readonly object OnHandUnitCostId = KB.I("OnHandUnitCostId");
			private static readonly object OnHandQuantityId = KB.I("OnHandQuantityId");
			// The values representing the order status of a POLine
			private static readonly object OrderedValueId = KB.I("OrderedValueId");
			private static readonly object OrderedUnitCostId = KB.I("OrderedUnitCostId");
			private static readonly object OrderedQuantityId = KB.I("OrderedQuantityId");
			// The values representing the received status of a POLine
			private static readonly object ReceivedValueId = KB.I("ReceivedValueId");
			private static readonly object ReceivedUnitCostId = KB.I("ReceivedUnitCostId");
			private static readonly object ReceivedQuantityId = KB.I("ReceivedQuantityId");
			// The values representing the yet-to-receive status of a POLine
			private static readonly object RemainingValueId = KB.I("RemainingValueId");
			private static readonly object RemainingUnitCostId = KB.I("RemainingUnitCostId");
			private static readonly object RemainingQuantityId = KB.I("RemainingQuantityId");
			#endregion
			#region - Our readonly message keys
			private static readonly Key UsingSuggestedQuantity = KB.K("The calculated quantity is being used");
			#endregion
			#endregion
		}
		#endregion
		#endregion
		#region Base POLine/POLineTemplate DerivationTblCreator<QT>
		protected abstract class POLineDerivationTblCreatorBase<QT> : DerivationTblCreatorWithQuantityAndCostBase
			where QT : struct, System.IComparable<QT> {
			#region Construction
			public POLineDerivationTblCreatorBase(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, DBI_Path costPath, TypeInfo unitCostTypeInfo)
				: base(identification, false, mostDerivedTable, costPath, unitCostTypeInfo) {
				pQuantityId = KB.I("QuantityId - ") + Identification.TableNameKeyLocalPart + CorrectionIdentification;
				// Create the start of the layout: Record type and date information from the base POLine record
				DetailColumns.Add(TblFixedRecordTypeNode.New());
				System.Diagnostics.Debug.Assert(QuantityTypeInfo.GenericAcceptedNativeType(typeof(QT)), "POLineDerivationTblCreatorBase: QT incompatible with QuantityTypeInfo");
			}
			#endregion
			#region GetTbl - add the final actions and accounting transaction info and build & return the tbl
			// Note that we put in the ECol ourselves.
			public override Tbl GetTbl(params Tbl.IAttr[] tblAttrs) {
				TblAttributes.Add(
					new ETbl(
						ETbl.EditorAccess(false, EdtMode.UnDelete),
						ETbl.SetMultiSelectNewModeBehaviour(ETbl.MultiSelectNewModeBehaviours.Single)
					)
				);

				FlushCostingLayout();
				// Add Checkers on the values based on the ValueInterpretation.
				// For the positive-only records insist that the Only/Net values be positive.
				// Net/Only: $ >= 0 checked by the column TypeInfo.
				// Net/Only: # >= 0 checked by the column TypeInfo.
				return base.GetTbl(tblAttrs);
			}
			#endregion
			#region HandleSuggestedQuantityAndBuildQuantityControl - Declare the Id of a control that contains a suggested quantity to use.
			// This should be called if the form has a control containing a suggested quantity.
			// It adds the "Use suggested quantity" checkbox and the associated quantity-copy action and forcing the quantity entry control(s) readonly as needed.
			public void HandleSuggestedQuantityAndBuildQuantityControl([Context(Level = 2)]string checkboxCaption, object suggestedQuantitySourceId, [Context(Level = 2)]string quantityLabel) {
				HandleSuggestedValue(QuantityPath, KB.K(checkboxCaption), UsingSuggestedQuantity, UseSuggestedQuantityId, suggestedQuantitySourceId, QuantityId);
				BuildQuantityControl(quantityLabel);
			}
			#endregion
			#region BuildQuantityControl - Build the Quantity control(s) that actually bind to the record.
			// This should only be called if the specific transaction defines a quantity.
			// This defines the quantity entry controls.
			public void BuildQuantityControl([Context(Level = 2)]string quantityLabel) {
				// For Noncorrectable records we just have a single control bound to the QuantityColumn
				DetailColumns.Add(TblColumnNode.New(KB.K(quantityLabel), QuantityPath, new ECol(Fmt.SetId(QuantityId))));
			}
			public void BuildQuantityControlforDefault([Context(Level = 2)]string quantityLabel) {
				DetailColumns.Add(TblColumnNode.New(KB.K(quantityLabel), QuantityPath, DCol.Normal, ECol.Normal));
			}
			#endregion
			#region Members
			#region - Node Ids
			private static readonly object UseSuggestedQuantityId = KB.I("UseSuggestedQuantityId");
			protected object QuantityId {
				get {
					System.Diagnostics.Debug.Assert(pQuantityPath != null, "DerivationTblCreatorWithQuantityAndCostBase.QuantityId referenced when schema has no Quantity");
					return pQuantityId;
				}
			}
			private readonly object pQuantityId;
			#endregion
			#region - Our readonly message keys
			private static readonly Key UsingSuggestedQuantity = KB.K("The calculated quantity is being used");
			#endregion
			public TypeInfo QuantityTypeInfoWithZero {
				get {
					TypeInfo basis = QuantityPath.ReferencedColumn.EffectiveType;
					if (basis is IntervalTypeInfo) {
						// TODO: Having to figure out the epsilon of a singleton set for unioning should not be necessary!
						if (((IntervalTypeInfo)basis).EpsilonType is IntervalTypeInfo)
							return basis.UnionCompatible(new IntervalTypeInfo((TimeSpan)((IntervalTypeInfo)basis).NativeEpsilon(typeof(TimeSpan)), TimeSpan.Zero, TimeSpan.Zero, allow_null: false));
						else
							return basis.UnionCompatible(new IntervalTypeInfo((uint)((IntervalTypeInfo)basis).NativeEpsilon(typeof(uint)), TimeSpan.Zero, TimeSpan.Zero, allow_null: false));
					}
					else
						return basis.UnionCompatible(new IntegralTypeInfo(false, 0, 0));
				}
			}
			#endregion
			#region Helper methods
			#region - HandleSuggestedValue
			protected void HandleSuggestedValue(DBI_Path ultimateBoundTarget, Key checkboxCaption, Key readonlyTip, object checkboxId, object suggestedValueId, object targetId, object relatedSuggestedValueId = null, object relatedTargetId = null) {
				// The "related" suggested value and target are copied before the main suggested value and target.
				// As a result, we can be used to copy a unit cost (calculating a new total cost in the target), then a total cost (calculating a new unit cost in the target). The difference between this
				// and just copying the total cost is that if the unit cost is null, this extended procedure sets the destination unit cost null and (assuming the total cost being copied is also null) leaves it that way.
				// TODO: Force all the quantity suggestor blank and disabled in Edit mode if editReadonly ???

				// Build the "Use suggestion" checkbox.
				TblLeafNode checkbox = TblUnboundControlNode.New(checkboxCaption, BoolTypeInfo.NonNullUniverse, new ECol(Fmt.SetId(checkboxId), Fmt.SetIsSetting(false), ECol.RestrictPerGivenPath(ultimateBoundTarget, 0)),
					new NonDefaultCol());
				AddWholeRowCostingEditControl(checkbox);

				// Build the action copying the suggested text to the bound quantity control only when the checkbox is checked.
				TblActionNode.IArg[] extraArgs = new[] {
					Init.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone),
					Init.SetCopyIllegalNullValue(Init.WhenToCopy.Always)
				};
				if (relatedSuggestedValueId != null)
					Actions.Add(Init.New(new ControlTarget(relatedTargetId), new ControlUncheckedValue(relatedSuggestedValueId),
						new Thinkage.Libraries.Presentation.ControlValue(checkboxId),
						TblActionNode.Activity.Disabled, extraArgs));
				Actions.Add(Init.New(new ControlTarget(targetId), new ControlUncheckedValue(suggestedValueId),
					new Thinkage.Libraries.Presentation.ControlValue(checkboxId),
					TblActionNode.Activity.Disabled, extraArgs));

				// Build the action forcing the related-target control readonly if the checkbox is checked.
				if (relatedTargetId != null)
					Actions.Add(Init.Continuous(new ControlReadonlyTarget(relatedTargetId, readonlyTip), new Thinkage.Libraries.Presentation.ControlValue(checkboxId)));
				// Build the action forcing the target control readonly if the checkbox is checked.
				Actions.Add(Init.Continuous(new ControlReadonlyTarget(targetId, readonlyTip), new Thinkage.Libraries.Presentation.ControlValue(checkboxId)));

				// Build the action setting the checkbox off as we enter Edit mode since saving the record may change the inputs.
				Actions.Add(Init.OnLoadEditUndelete(new ControlTarget(checkboxId), new ConstantValue(false)));
			}
			#endregion

			#endregion
		}
		#endregion
		#region Common POLine handling
		protected class POLineDerivationTblCreator<QT> : POLineDerivationTblCreatorBase<QT>
			where QT : struct, System.IComparable<QT> {
			#region Usage Comment
			// This class is used to build derivations of the POLine record.
			//
			// The general model for building the tbl is:
			// Build one of these objects.
			// Use CreatePOControl() optionally preceded by AddPickerFilterControl and AddPickerFilter to identify the PO
			// Use CreateResourceControl() optionally preceded by AddPickerFilterContro, AddPickerFilter, AddPickerPanelDisplay to identify the item being ordered.
			//
			// If there is suggested POLine Text:
			//		Build controls to produce a poline text suggestion
			//		Call this.HandleSuggestedPOLineTextAndBuildPOLineTextControl
			//	else
			//		Call this.BuildPOLineTextControl
			// If there is some custom quantity suggestion:
			//		Build controls to produce a quantity suggestion
			//		Call this.HandleSuggestedQuantityAndBuildQuantityControl to create the controls to take the suggested quantity into the common calculations
			// else
			//		Call this.BuildQuantityControl to create the actual quantity control
			// If there is a cost basis (unit cost or quantity/cost pair)
			//		Build controls and actions to produce a cost basis (either an quantity and total cost, or a unit cost)
			//		Call this.HandleBasisCostAndBuildCostControl to create the controls to produce a sggested cost and include it in the common calculations;
			//			this includes proper handling of negative quantities on corrections (i.e. returns)
			// else if there is a suggested cost
			//		build controls and actions to produce a suggested cost
			//		Call this.RegisterSuggestedCostSource
			// else
			//		Call BuildCostControl to create the actual costing control(s)
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// Call this.GetTbl to actually get the Tbl for the record.
			// TODO: The code in here that turns off the "Use suggested Xxxx" checkboxes runs too late when a record is saved. The field bindings
			// occur before the checkboxes are cleared and new 'suggested' values (now taking into effect the saved record) get copied before the checkboxes
			// turn off. Because this occurs during the record-fetch cycle the record is still in the "saved" state in the editor as well.
			// Perhaps the suggestors must take into account the suggestion net of the current record concept.
			// The filtering controls should only be in edt modes that the picker control itself is writeable in. This is not harmful, other than the presence of
			// apparently enabled controls that actually do nothing useful could confuse the user. Note that they still have one effect: if the "..." view in full browser
			// command is used, the filtering affects what records appear in the browser.
			// However, this is more harmful than it would first appear: if the control is not writeable
			// but we are in New mode (or likely Clone too) the filter can cause an Init value to be rejected.
			// This is cured by making a custom tbl for calling from the POLineItems that has filtering as coded here, another for calling from the WO Labor
			// browsette that has assistance in picking the PO based on the Demand, and a third with no filtering at all for registering for general usage.
			#endregion
			#region Construction
			public POLineDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, bool echoQuantity, bool editDefaults, TypeInfo unitCostTypeInfo)
				: base(identification, mostDerivedTable, dsMB.Path.T.POLine.F.Cost.ReOrientFromRelatedTable(mostDerivedTable), unitCostTypeInfo) {
				CostId = KB.I("CostId - ") + Identification.TableNameKeyLocalPart;
				EchoQuantity = echoQuantity;
				EditDefaults = editDefaults;
			}
			#endregion
			#region PO Picker and filtering support
			public void CreatePOControl() {
				if (EditDefaults)
					return;
				// The PO picker has standardized panel display.
				AddPickerPanelDisplay(dsMB.Path.T.POLine.F.PurchaseOrderID.F.Number.ReOrientFromRelatedTable(MostDerivedTable));
				AddPickerPanelDisplay(dsMB.Path.T.POLine.F.PurchaseOrderID.F.Subject.ReOrientFromRelatedTable(MostDerivedTable));
				AddPickerPanelDisplay(dsMB.Path.T.POLine.F.PurchaseOrderID.F.VendorID.F.Code.ReOrientFromRelatedTable(MostDerivedTable));

				CreateBoundPickerControl(POPickerId, dsMB.Path.T.POLine.F.PurchaseOrderID.ReOrientFromRelatedTable(MostDerivedTable));
			}
			#endregion
			#region ItemNumber Control
			public void CreateItemNumberControl() {
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.POLine.F.LineNumber.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					ECol.Normal));
			}
			#endregion
			#region Resource picker and filtering support
			public void CreateResourceControl(DBI_Path boundPath, params ECol.ICtorArg[] args) {
				CreateBoundPickerControl(ResourcePickerId, boundPath, args);
			}
			#endregion
			#region InnerFlushCostingLayout - Set up the multi-column layout for the costing controls.
			public override void StartAsCorrectedCostingRow(Key rowLabel) {
				StartCostingRow(rowLabel);
			}
			public override void EndThisRecordCostingRow() {
				EndCostingRow();
			}
			protected override Key[] CostingColumnHeaders {
				get {
					return new Key[] { QuantityLabel, QuantityTypeInfo is IntervalTypeInfo ? KB.K("Hourly Rate") : KB.K("Unit Cost"), KB.K("Total Cost") };
				}
			}
			#endregion
			#region HandleSuggestedPOLineTextAndBuildPOLineTextControl - Declare the Id of a control that contains a suggested POLine text to use.
			public void HandleSuggestedPOLineTextAndBuildPOLineTextControl([Context(Level = 2)]string checkboxCaption, object suggestedTextId) {
				HandleSuggestedValue(dsMB.Path.T.POLine.F.PurchaseOrderText.ReOrientFromRelatedTable(MostDerivedTable), KB.K(checkboxCaption), UsingSuggestedPOLineText, UseSuggestedPOLineTextId, suggestedTextId, POLineTextId);
				BuildPOLineTextControl();
			}
			#endregion
			#region BuildPOLineTextControl - Build the POLineText control that actually binds to the record
			public void BuildPOLineTextControl() {
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.POLine.F.PurchaseOrderText.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					new ECol(Fmt.SetId(POLineTextId))));
			}
			#endregion
			#region BuildSuggestedTextDisplay
			public void BuildSuggestedTextDisplay([Context(Level = 2)]string suggestedCaption, object suggestedTextId) {
				DetailColumns.Add(TblUnboundControlNode.New(KB.K(suggestedCaption), PrototypeDescriptionTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(suggestedTextId))));
			}
			#endregion
			#region HandleCostSuggestionWithBasisCost - Build cost controls and cost suggestion controls based on supplied cost basis
			// For records with Quantity this declares the normal costing basis (for positive quantities) given a basis quantity and basis total cost as control id's.
			// This should be used where the Quantity is an integral value.
			// This builds a cost calculator (that also accounts for reversals in corrections) and uses its result as a suggestion.
			// TODO: Do we want forms of this that take paths?
			public void HandleBasisCostAndBuildCostControls(object basisQuantityId, object basisCostId, Key unitCostLabel) {
				InnerHandleBasisCostAndBuildCostControl(basisQuantityId, basisCostId, unitCostLabel);
			}
			// For records with Quantity this declares the normal costing basis (for positive quantities) given a basis unit cost as control id's
			// This should be used where the Quantity is a time span.
			// TODO: Do we want a form of this that takes a path?
			// TODO: Several callers to this (PO forms) should not be using this because they actually have a total cost and quantity from the PO line, not a unit cost.
			public void HandleUnitCostAndBuildCostControl(object unitValue, object basisUnitCostId, Key unitCostLabel) {
				// Make a hidden basis Quantity containing one hour, and treat the given Unit Cost as a Total Cost
				DetailColumns.Add(TblUnboundControlNode.StoredEditorValue(DeemedPricingQuantityId, QuantityTypeInfo));
				Actions.Add(Init.OnLoad(new ControlTarget(DeemedPricingQuantityId), new ConstantValue(unitValue)));
				InnerHandleBasisCostAndBuildCostControl(DeemedPricingQuantityId, basisUnitCostId, unitCostLabel);
			}
			private void InnerHandleBasisCostAndBuildCostControl(object basisQuantityId, object basisCostId, Key unitCostLabel) {
				StartCostingLayout();
				StartCostingRow(CalculatedCost);
				if (EchoQuantity) {
					AddCostingControl(TblUnboundControlNode.New(NullableQuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostCalculationQuantityId))));
					// Make QuantityForCostCalculationId echo QuantityId
					Actions.Add(Init.New(new ControlTarget(CostCalculationQuantityId), new ControlUncheckedValue(QuantityId), null, Init.SetCopyIllegalNullValue(Init.WhenToCopy.Always)));
				}
				else
					AddCostingControl(TblLeafNode.Empty(ECol.Normal));

				AddUnitCostEditDisplay(CostCalculationUnitCostId);
				AddCostingControl(TblUnboundControlNode.New(NullableCostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(CostCalculationValueId))));
				EndCostingRow();
				// Add the calculation: Total <- Quantity * EffectiveBasisCost / EffectiveBasisQuantity
				Actions.Add(TotalFromQuantityAndPricingCalculator<QT>(QuantityId, basisQuantityId, basisCostId, CostCalculationValueId));
				// Add the calculation: Unit Cost <- Total / Quantity
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<QT>(QuantityId, CostCalculationValueId, CostCalculationUnitCostId));

				HandleSuggestedValue(CostPath, UseCalculatedCost, UsingSuggestedCost, UseSuggestedCostId, CostCalculationValueId, CostId, CostCalculationUnitCostId, UnitCostId);
				BuildCostControls(unitCostLabel, UnitCostId);
			}
			#endregion
			#region BuildCostControls - Build the cost and Unit cost controls that are bound to the record
			public void BuildCostControls(Key unitCostLabel, object unitCostId) {
				StartCostingLayout();

				// Build the costing controls.
				// Note that although these have a display of the Quantity they get this directly from the record, and any calculations using the value
				// reference the bound control built by BuildQuantityControls().

				// Create the only costing line
				StartCostingRow(CostPath);
				AddCostingControl(TblColumnNode.New(QuantityPath, DCol.Normal));
				if (EchoQuantity) {
					AddCostingControl(TblUnboundControlNode.New(QuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(EchoQuantityId))));
					// Make QuantityForCostingId echo QuantityId
					Actions.Add(Init.New(new ControlTarget(EchoQuantityId), new ControlUncheckedValue(QuantityId), null, Init.SetCopyIllegalNullValue(Init.WhenToCopy.Always)));
				}
				else
					AddCostingControl(TblLeafNode.Empty(ECol.Normal));
				AddUnitCostBrowserDisplayAndEditControl<QT>(unitCostLabel, QuantityPath, unitCostId, CostPath, ECol.RestrictPerGivenPath(CostPath, 0));
				// Create the Cost control
				AddCostingControl(TblColumnNode.New(CostPath, DCol.Normal, new ECol(Fmt.SetId(CostId))));
				EndCostingRow();

				// The Total Cost control is made in common BuildCostControls code.
				// Enforce the (quantity/Unit cost/Total cost) relationship.
				Actions.Add(new Check3<QT?, decimal?, decimal?>(
					delegate (QT? quantity, decimal? unit, decimal? total) {
						if (!unit.HasValue && !total.HasValue)
							// If we have no unit cost and no total value we don't care what the quantity says, so we return null
							// so no correction attempt is made.
							return null;
						if (unit.HasValue && total.HasValue && quantity.HasValue && total.Value == checked((decimal?)CostTypeInfo.GenericAsNativeType(CostTypeInfo.ClosestValueTo(Compute.Multiply<QT>(unit, quantity)), typeof(decimal?))))
							// Everything has a value and everything is OK, so just leave it alone.
							return null;
						// Otherwise return an error to cause things to be corrected. We do not supply a corrector delegate for Quantity, so this may mean
						// than no operand is correctable, in which case all 3 controls will be flagged with this error.
						return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Quantity, Unit Cost, and Total Cost are mutually inconsistent")));
					})
					.Operand1(QuantityId)
					.Operand2(unitCostId,
						delegate (QT? quantity, decimal? total) {
							return !quantity.HasValue || Compute.IsZero<QT>(quantity) || !total.HasValue
								? null : checked(Compute.Divide<QT>(total, quantity));
						})
					.Operand3(CostId,
						delegate (QT? quantity, decimal? unit) {
							return !quantity.HasValue || !unit.HasValue
								? null : checked(Compute.Multiply<QT>(unit, quantity));
						})
				);
			}
			#endregion
			#region Members
			#region - Information computed based on the ctor arguments
			private readonly bool EchoQuantity;
			private readonly bool EditDefaults;
			public TypeInfo PrototypeDescriptionTypeInfo {
				get {
					return dsMB.Schema.T.POLine.F.PurchaseOrderText.EffectiveType;
				}
			}
			#endregion
			#region - The NodeId's we use
			// Some of these node Id's are instance members to make sure that each Tbl uses distinct Id's. This is necessary if more than one
			// of the Tbl's we create is referenced by the CompositeViews of a CompositeTbl, which essentially puts all the Id's in the same scope.
			// The two instance Id's are the only ones the browser code and DCols require.

			public readonly object CostId;
			private static readonly object POLineTextId = KB.I("POLineTextId");
			// Value Id in base class
			private static readonly object UnitCostId = KB.I("UnitCostId");
			private static readonly object EchoQuantityId = KB.I("EchoQuantityId");
			private static readonly object UseSuggestedPOLineTextId = KB.I("UseSuggestedPOLineTextId");
			private static readonly object UseSuggestedCostId = KB.I("UseSuggestedCostId");
			// A deemed (implied) quantity used to express resources priced by Unit Cost as a Total Cost/Quantity pair
			private static readonly object DeemedPricingQuantityId = KB.I("DeemedPricingQuantityId");
			// The values in the Calculated Cost row
			private static readonly object CostCalculationValueId = KB.I("CostCalculationValueId");
			private static readonly object CostCalculationQuantityId = KB.I("CostCalculationQuantityId");
			private static readonly object CostCalculationUnitCostId = KB.I("CostCalculationUnitCostId");
			private static readonly object ResourcePickerId = KB.I("ResourcePickerId");
			private static readonly object POPickerId = KB.I("POPickerId");

			#endregion
			#region - Our readonly message keys
			private static readonly Key UsingSuggestedCost = KB.K("The calculated cost is being used");
			private static readonly Key UsingSuggestedPOLineText = KB.K("The suggested Purchase Order Line Text is being used");
			#endregion
			#endregion
			#region Public helper methods
			#region - ItemPricePickerCost
			public void BuildPickedItemPriceResultDisplays() {
				StartCostingLayout();
				StartCostingRow(KB.K("Pricing Basis"));
				AddCostingControl(TblUnboundControlNode.New(NullableQuantityTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemPriceQuantityId))));
				AddUnitCostEditDisplay(ItemPriceUnitCostId, allowNull: true);
				AddCostingControl(TblUnboundControlNode.New(NullableCostTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemPriceValueId))));
				EndCostingRow();
				Actions.Add(UnitCostFromQuantityAndTotalCalculator<long>(ItemPriceQuantityId, ItemPriceValueId, ItemPriceUnitCostId));
			}
			public void BuildPickedItemPriceCostingControls() {
				if (EditDefaults)
					return;
				HandleBasisCostAndBuildCostControls(ItemPriceQuantityId, ItemPriceValueId, KB.K("Unit Cost"));
			}

			#endregion
			#region - POLineItemStorageAssignment
			public void POLineItemStorageAssignment(object toOrderId) {
				if (EditDefaults)
					return;
				AddPickerPanelDisplay(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.Code);
				AddPickerPanelDisplay(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code);
				AddPickerPanelDisplay(dsMB.Path.T.POLineItem.F.ItemLocationID.F.LocationID.F.Code);
				CreateResourceControl(dsMB.Path.T.POLineItem.F.ItemLocationID);

				// Show item quantity information for the ItemLocation
				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Available"), KB.K("Minimum"), KB.K("Maximum"), KB.K("To Order") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ActualItemLocationID.F.Available, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemLocationAvailableId))),
						TblColumnNode.New(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemLocationMaximumId))),
						TblUnboundControlNode.New(KB.K("Recommended To Order"), QuantityTypeInfoWithZero, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(toOrderId)))
					)
				));
				Actions.Add(RemainingCalculator<long>(ItemLocationMaximumId, ItemLocationAvailableId, toOrderId));
			}
			#endregion
			#region - POLineItemResourceFilter
			public void POLineItemResourceFilter() {
				if (EditDefaults)
					return;
				// Because we have two independent sets of radio buttons, and a checkbox associated with one of them, we use nested group boxes.
				StartPickerFilterControlGroup();

				// A - vendor history:
				// - only assignments with a preferred pricing which is still valid [and is from correct vendor]
				// - only assignments with items that have valid price quotes [from current vendor]
				// - only assignments with items that have previously been received [from current vendor]
				// - No filtering based on history
				// The even control values represent the filter without Vendor checking. If vendor checking is selected we add one to the value.
				EnumValueTextRepresentations itemVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Storage Assignments which have a preferred Price Quote associated with them"),
								KB.K("Only include Storage Assignments for items which have a Price Quote associated with them"),
								KB.K("Only include Storage Assignments for items which have previously been received"),
								KB.K("Do not filter Storage Assignments based on Pricing or Purchasing History")
							},
					null,
					new object[] {
								0, 2, 4, 6
							}
				);
				object ATypeId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 6),
					Fmt.SetEnumText(itemVendorAssociations),
					Fmt.SetIsSetting(0)
				);
				object AVendorId = AddPickerFilterControl(KB.K("Use Pricing or Purchasing history only for the Purchase Order's Vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				EndPickerFilterControlGroup(KB.K("Storage Assignment filtering based on Pricing and Purchasing History"));

				// Make a calculated init value that adds one to the radio-button value if specific-vendor filtering is requested.
				EditorInitValue combined = new EditorCalculatedInitValue(
					new IntegralTypeInfo(false, 0, 7),
					delegate (object[] inputs) {
						return ((bool)inputs[1] ? 1 : 0) + (int)IntegralTypeInfo.AsNativeType(inputs[0], typeof(int));
					},
					new Thinkage.Libraries.Presentation.ControlValue(ATypeId), new Thinkage.Libraries.Presentation.ControlValue(AVendorId));
				// IL's with a preferred pricing
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemPriceID).IsNotNull()),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				// IL's with a preferred pricing from this vendor
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemPriceID.F.VendorID)
						.Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.VendorID, 1))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);
				// IL's with a price quote
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ItemPrice.F.ItemID) },
							null,
							null).SetDistinct(true))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 2);
					}
				);
				// IL's with a price quote from this vendor
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ItemPrice.F.ItemID) },
							new SqlExpression(dsMB.Path.T.ItemPrice.F.VendorID).Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
							null).SetDistinct(true))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 3);
					}
				);
				// IL's for items with previous receiving (this one is kinda useless, most any Items will have receiving)
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID) },
							null,
							null).SetDistinct(true))
					.Or(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID) },
							null,
							null).SetDistinct(true)))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 4);
					}
				);
				// IL's for items with previous receiving from this vendor.
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID).Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
							null).SetDistinct(true))
					.Or(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.VendorID).Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
							null).SetDistinct(true)))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 5);
					}
				);

				StartPickerFilterControlGroup();
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
				// Note that the do/don't want to consider transfers from other locations may be a fly in the ointment of database partitioning
				// (where different users only manage subsets of storage locations)
				EnumValueTextRepresentations quantityTests = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Storage Assignments which are below minimum and cannot be replenished using transfers"),
								KB.K("Only include Storage Assignments which are below minimum"),
								KB.K("Only include Storage Assignments which are below maximum and should not be replenished using transfers"),
								KB.K("Only include Storage Assignments which are below maximum"),
								KB.K("Do not filter Storage Assignments based on quantity available")
							},
					null,
					new object[] {
								0, 1, 2, 3, 4
							}
				);
				object BId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 4),
					Fmt.SetEnumText(quantityTests),
					Fmt.SetIsSetting(0)
				);
				EndPickerFilterControlGroup(KB.K("Storage Assignment filtering based on Available quantity"));

				// The subquery only sums over PermanentItemLocation since TemporaryItemLocation Minimum is always zero.
				// The .GEq().IsFalse() is there in case no PermanentItemLocation records exist (i.e. only temp ILs exist for the item) and
				// the subquery returns null.
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.Available)
						.Lt(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum))
					.And(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID.F.Available)
						.GEq(SqlExpression.ScalarSubquery(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.PermanentItemLocation.F.Minimum).Sum() },
							new SqlExpression(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID)
								.Eq(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, 1)),
							null))).IsFalse())),
					BId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.Available)
						.Lt(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum))),
					BId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);
				// The subquery only sums over PermanentItemLocation since TemporaryItemLocation Maximum is always zero.
				// The .GEq().IsFalse() is there in case no PermanentItemLocation records exist (i.e. only temp ILs exist for the item) and
				// the subquery returns null.
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.Available)
						.Lt(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum))
					.And(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID.F.Available)
						.GEq(SqlExpression.ScalarSubquery(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.PermanentItemLocation.F.Maximum).Sum() },
							new SqlExpression(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID)
								.Eq(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, 1)),
							null))).IsFalse())),
					BId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 2);
					}
				);
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.Available)
						.Lt(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum))),
					BId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 3);
					}
				);
			}
			#endregion
			#region - POLineItemPOFilter
			public void POLineItemPOFilter() {
				// A - vendor history:
				// - only POs from the vendor named in the ItemLocation's preferred pricing which is still valid.
				// - only POs from vendors named in valid price quotes for the item
				// - only POs from vendors from which this item has previously been received.
				// [should there also be previous receiving to this particular ItemLocation?]
				// - No filtering based on history
				EnumValueTextRepresentations itemVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Purchase Orders from the Vendor named in the preferred Price Quote for this Storage Assignment"),
								KB.K("Only include Purchase Orders from Vendors who have a Price Quote for the Item in this Storage Assignment"),
								KB.K("Only include Purchase Orders from Vendors who have previously supplied the Item in this Storage Assignment"),
								KB.K("Do not filter Purchase Orders based on Vendors in Pricing or Purchasing History")
							},
					null,
					new object[] {
								0, 1, 2, 3
							}
				);
				object ATypeId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 3),
					Fmt.SetEnumText(itemVendorAssociations),
					Fmt.SetIsSetting(0)
				);

				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
						.Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemPriceID.F.VendorID, 1))),
					ATypeId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
						.In(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ItemPrice.F.VendorID) },
							new SqlExpression(dsMB.Path.T.ItemPrice.F.ItemID).Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, 2)),
							null))),
					ATypeId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
						.In(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID).Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, 2)),
							null).SetDistinct(true))
					.Or(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
						.In(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.VendorID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID).Eq(new SqlExpression(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, 2)),
							null).SetDistinct(true)))),
					ATypeId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 2);
					}
				);
			}
			#endregion

			#region - POLineLaborAssignment
			public void POLineLaborAssignment(object toOrderId) {
				if (EditDefaults)
					return;
				AddPickerPanelDisplay(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderID.F.Number);
				AddPickerPanelDisplay(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code);
				CreateResourceControl(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID, Fmt.SetPickFrom(TIWorkOrder.DemandLaborOutsideForPOLinePickerTblCreator));

				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Demanded"), KB.K("Already Ordered"), KB.K("Remaining Demand") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineDemandedId))),
						TblColumnNode.New(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.OrderQuantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineOrderedId))),
						TblUnboundControlNode.New(KB.K("Recommended To Order"), QuantityTypeInfoWithZero, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(toOrderId)))
					)
				));
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID.F.Cost, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredUnitCostId))));
				Actions.Add(RemainingCalculator<System.TimeSpan>(POLineDemandedId, POLineOrderedId, toOrderId));

			}
			#endregion
			#region - POLineLaborPOFilter
			public void POLineLaborPOFilter() {
				// A - Work vendor association:
				// - Only PO's from vendors who can supply the demanded work
				// - no filtering based on vendor/work associations
				object AId = AddPickerFilterControl(KB.K("Only include Purchase Orders for Vendors who can perform this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID.F.VendorID, 1)
							.Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID))
							.IsFalse().Not()),
					AId,
					null
				);

				// B - Vendor history:
				// - only POs for vendors that have previously been done the work
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include Purchase Orders for Vendors who have previously performed this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// PurchaseOrderID.VendorID IN
				//		(select distinct ALOPO.POLL.POL.PO.VendorID from ALOPO where ALOPO.POLL.DLO.LO = editor's Labor Outside
				//	union
				//		select distinct ALONPO.VendorID from ALONPO where ALONPO.DLO.LO = editor's Labor Outside)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID.F.LaborOutsideID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);
			}
			#endregion
			#region - POLineLaborResourceFilter
			public void POLineLaborResourceFilter() {
				// A - Work vendor association:
				// - only demands for work specifically defined to be from this vendor
				// - all demands for work this vendor could provide (including records with null vendor)
				// - all demands for work amy vendor might supply (no filter)
				EnumValueTextRepresentations demandVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Demands for work specifically from this Purchase Order's Vendor"),
								KB.K("Only include Demands for work that this Purchase Order's Vendor could perform"),
								KB.K("Do not filter Demands based on the Vendor associated with the work")
							},
					null,
					new object[] {
								0, 1, 2
							}
				);
				object AId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 2),
					Fmt.SetEnumText(demandVendorAssociations),
					Fmt.SetIsSetting(0)
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID.F.VendorID, 1))),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID.F.VendorID, 1))
							.IsFalse().Not()),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);

				// B - Vendor history:
				// - only demands for work that has previously been received
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include demands for work previously performed by this vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// DemandLaborOutide.LaborOutsideID IN
				//		(select distinct ALOPO.POLL.DLOID.LOID from ALOPO where ALOPO.ReceiptID.POID.VendorID = editor's vendor
				//	union
				//		select distinct ALONPO.DLOID.LOID from ALONPO where ALONPO.VendorID = editor's vendor)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);
				// C - quantity status:
				// - only demands where ordered < demanded
				// - No filtering based on quantity.
				// TODO: Want a way for this control to start off true without having to provide a creator delegate in the TblLeafNode. We could do it with another INit.
				object CId = AddPickerFilterControl(KB.K("Only include demands where quantity demanded exceeds quantity currently ordered"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.Quantity).Gt(new SqlExpression(dsMB.Path.T.DemandLaborOutside.F.OrderQuantity))), CId, null);
			}
			#endregion

			#region - POLineOtherWorkAssignment
			public void POLineOtherWorkAssignment(object toOrderId) {
				if (EditDefaults)
					return;
				AddPickerPanelDisplay(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderID.F.Number);
				AddPickerPanelDisplay(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code);
				CreateResourceControl(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID, Fmt.SetPickFrom(TIWorkOrder.DemandOtherWorkOutsideForPOLinePickerTblCreator));

				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Demanded"), KB.K("Already Ordered"), KB.K("Remaining Demand") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineDemandedId))),
						TblColumnNode.New(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OrderQuantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineOrderedId))),
						TblUnboundControlNode.New(KB.K("Recommended To Order"), QuantityTypeInfoWithZero, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(toOrderId)))
					)
				));
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Cost, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredUnitCostId))));
				Actions.Add(RemainingCalculator<long>(POLineDemandedId, POLineOrderedId, toOrderId));
			}
			#endregion
			#region - POLineOtherWorkPOFilter
			public void POLineOtherWorkPOFilter() {
				// A - Work vendor association:
				// - Only PO's from vendors who can supply the demanded work
				// - no filtering based on vendor/work associations
				object AId = AddPickerFilterControl(KB.K("Only include Purchase Orders for Vendors who can perform this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.VendorID, 1)
							.Eq(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID))
							.IsFalse().Not()),
					AId,
					null
				);

				// B - Vendor history:
				// - only POs for vendors that have previously been done the work
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include Purchase Orders for Vendors who have previously performed this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// PurchaseOrderID.VendorID IN
				//		(select distinct ALOPO.POLL.POL.PO.VendorID from ALOPO where ALOPO.POLL.DLO.LO = editor's Other Work Outside
				//	union
				//		select distinct ALONPO.VendorID from ALONPO where ALONPO.DLO.LO = editor's Other Work Outside)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.PurchaseOrder.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);
			}
			#endregion
			#region - POLineOtherWorkResourceFilter
			public void POLineOtherWorkResourceFilter() {
				// A - Work vendor association:
				// - only demands for work specifically defined to be from this vendor
				// - all demands for work this vendor could provide (including records with null vendor)
				// - all demands for work amy vendor might supply (no filter)
				EnumValueTextRepresentations demandVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Demands for work specifically from this Purchase Order's Vendor"),
								KB.K("Only include Demands for work that this Purchase Order's Vendor could provide"),
								KB.K("Do not filter Demands based on the Vendor associated with the work")
							},
					null,
					new object[] {
								0, 1, 2
							}
				);
				object AId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 2),
					Fmt.SetEnumText(demandVendorAssociations),
					Fmt.SetIsSetting(0)
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID.F.VendorID, 1))),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID.F.VendorID, 1))
							.IsFalse().Not()),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);

				// B - Vendor history:
				// - only demands for work that has previously been received
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include demands for work previously provided by this vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// DemandOtherWorkOutide.OtherWorkOutsideID IN
				//		(select distinct ALOPO.POLL.DLOID.LOID from ALOPO where ALOPO.ReceiptID.POID.VendorID = editor's vendor
				//	union
				//		select distinct ALONPO.DLOID.LOID from ALONPO where ALONPO.VendorID = editor's vendor)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID.F.VendorID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);
				// C - quantity status:
				// - only demands where ordered < demanded
				// - No filtering based on quantity.
				// TODO: Want a way for this control to start off true without having to provide a creator delegate in the TblLeafNode. We could do it with another INit.
				object CId = AddPickerFilterControl(KB.K("Only include demands where quantity demanded exceeds quantity currently ordered"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.Quantity).Gt(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutside.F.OrderQuantity))), CId, null);
			}
			#endregion
			#endregion
		}
		#endregion
		#region Common POLineTemplate handling
		protected class POLineTemplateDerivationTblCreator<QT> : POLineDerivationTblCreatorBase<QT>
			where QT : struct, System.IComparable<QT> {
			#region Usage Comment
			// This class is used to build derivations of the POLineTemplate record.
			//
			// The general model for building the tbl is:
			// Build one of these objects.
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// If there is suggested POLineTemplate Text:
			//		Build controls to produce a poline text suggestion
			//		Call this.HandleSuggestedPOLineTextAndBuildPOLineTextControl
			//	else
			//		Call this.BuildPOLineTextControl
			// If there is some custom quantity suggestion:
			//		Build controls to produce a quantity suggestion
			//		Call this.HandleSuggestedQuantityAndBuildQuantityControl to create the controls to take the suggested quantity into the common calculations
			// else
			//		Call this.BuildQuantityControl to create the actual quantity control
			// If there is a cost basis (unit cost or quantity/cost pair)
			//		Build controls and actions to produce a cost basis (either an quantity and total cost, or a unit cost)
			//		Call this.HandleBasisCostAndBuildCostControl to create the controls to produce a sggested cost and include it in the common calculations;
			//			this includes proper handling of negative quantities on corrections (i.e. returns)
			// else if there is a suggested cost
			//		build controls and actions to produce a suggested cost
			//		Call this.RegisterSuggestedCostSource
			// else
			//		Call BuildCostControl to create the actual costing control(s)
			// Build controls (in this.Columns) and actions (in this.Actions) to select any parameters to the transaction (i.e. fields in the
			//		derived record describing he details of the transaction) or display information in the browser panel.
			// Call this.GetTbl to actually get the Tbl for the record.
			#endregion
			#region Construction
			public POLineTemplateDerivationTblCreator(Tbl.TblIdentification identification, DBI_Table mostDerivedTable, TypeInfo unitCostTypeInfo)
				: base(identification, mostDerivedTable, null, unitCostTypeInfo) {
			}
			#endregion
			#region PO Picker and filtering support
			public void CreatePOTemplateControl() {
				// The PO picker has standardized panel display.
				AddPickerPanelDisplay(dsMB.Path.T.POLineTemplate.F.PurchaseOrderTemplateID.F.Code.ReOrientFromRelatedTable(MostDerivedTable));
				AddPickerPanelDisplay(dsMB.Path.T.POLineTemplate.F.PurchaseOrderTemplateID.F.VendorID.F.Code.ReOrientFromRelatedTable(MostDerivedTable));

				CreateBoundPickerControl(POTemplatePickerId, dsMB.Path.T.POLineTemplate.F.PurchaseOrderTemplateID.ReOrientFromRelatedTable(MostDerivedTable));
			}
			#endregion
			#region ItemNumber Control
			public void CreateItemNumberControl() {
				DetailColumns.Add(TblColumnNode.New(
					dsMB.Path.T.POLineTemplate.F.LineNumberRank.ReOrientFromRelatedTable(MostDerivedTable),
					DCol.Normal,
					ECol.Normal));
			}
			#endregion
			#region Resource picker and filtering support
			public void CreateResourceControl(DBI_Path boundPath, params ECol.ICtorArg[] args) {
				CreateBoundPickerControl(ResourcePickerId, boundPath, args);
			}
			#endregion
			#region HandleSuggestedPOLineTextAndBuildPOLineTextControl - Declare the Id of a control that contains a suggested POLine text to use.
			public void HandleSuggestedPOLineTextAndBuildPOLineTextControl([Context(Level = 2)]string checkboxCaption, object suggestedTextId) {
				// PurchaseOrderText was removed pending discussions of best way to provide 'custom' override PO Text at instantiation time of a PO from a template.
				// The infrastructure is left here for possible future use, just commented out to not provide any UI
				//HandleSuggestedValue(KB.K(checkboxCaption), UsingSuggestedPOLineText, suggestedTextId, UseSuggestedPOLineTextId, POLineTextId, false, null);
				//BuildPOLineTextControl();
			}
			#endregion
			#region BuildPOLineTextControl - Build the POLineText control that actually binds to the record
			public void BuildPOLineTextControl() {
				// PurchaseOrderText was removed pending discussions of best way to provide 'custom' override PO Text at instantiation time of a PO from a template.
				// The infrastructure is left here for possible future use, just commented out to not provide any UI
				//Columns.Add(TblColumnNode.New(
				//	dsMB.Path.T.POLineTemplate.F.PurchaseOrderText.ReOrientFromRelatedTable(MostDerivedTable),
				//	DCol.Normal,
				//	new NodeId(POLineTextId),
				//	ECol.Normal));
			}
			#endregion
			#region Members
			#region - The NodeId's we use
			// Some of these node Id's are instance members to make sure that each Tbl uses distinct Id's. This is necessary if more than one
			// of the Tbl's we create is referenced by the CompositeViews of a CompositeTbl, which essentially puts all the Id's in the same scope.
			// The two instance Id's are the only ones the browser code and DCols require.
			// Value Id in base class
			// A deemed (implied) quantity used to express resources priced by Unit Cost as a Total Cost/Quantity pair
			private static readonly object ResourcePickerId = KB.I("ResourcePickerId");
			private static readonly object POTemplatePickerId = KB.I("POTemplatePickerId");
			#endregion
			#endregion
			#region Public helper methods
			#region - POLineItemTemplateStorageAssignment
			public void POLineItemTemplateStorageAssignment() {
				AddPickerPanelDisplay(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID.F.Code);
				AddPickerPanelDisplay(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code);
				AddPickerPanelDisplay(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.LocationID.F.Code);
				CreateResourceControl(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID);

				// Show item quantity information for the ItemLocation
				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Available"), KB.K("Minimum"), KB.K("Maximum") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ActualItemLocationID.F.Available, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemLocationAvailableId))),
						TblColumnNode.New(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemLocationMinimumId))),
						TblColumnNode.New(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(ItemLocationMaximumId)))
					)
				));
			}
			#endregion
			#region - POLineItemTemplatePOTemplateFilter
			public void POLineItemTemplatePOTemplateFilter() {
				// A - vendor history:
				// - only POTs from the vendor named in the ItemLocation's preferred pricing which is still valid.
				// - only POTs from vendors named in valid price quotes for the item
				// - only POTs from vendors from which this item has previously been received.
				// [should there also be previous receiving to this particular ItemLocation?]
				// - No filtering based on history
				EnumValueTextRepresentations itemVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Purchase Order Templates from the Vendor named in the preferred Price Quote for this Storage Assignment"),
								KB.K("Only include Purchase Order Templates from Vendors who have a Price Quote for the Item in this Storage Assignment"),
								KB.K("Only include Purchase Order Templates from Vendors who have previously supplied the Item in this Storage Assignment"),
								KB.K("Do not filter Purchase Order Templates based on Vendors in Pricing or Purchasing History")
							},
					null,
					new object[] {
								0, 1, 2, 3
							}
				);
				object ATypeId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 3),
					Fmt.SetEnumText(itemVendorAssociations),
					Fmt.SetIsSetting(0)
				);

				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
						.Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID.F.VendorID, 1))),
					ATypeId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
						.In(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ItemPrice.F.VendorID) },
							new SqlExpression(dsMB.Path.T.ItemPrice.F.ItemID).Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID, 2)),
							null))),
					ATypeId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
						.In(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID).Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID, 2)),
							null).SetDistinct(true))
					.Or(new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
						.In(new SelectSpecification(null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.VendorID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID).Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemID, 2)),
							null).SetDistinct(true)))),
					ATypeId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 2);
					}
				);
			}
			#endregion
			#region - POLineItemTemplateResourceFilter
			public void POLineItemTemplateResourceFilter() {
				// Because we have two independent sets of radio buttons, and a checkbox associated with one of them, we use nested group boxes.
				StartPickerFilterControlGroup();

				// A - vendor history:
				// - only assignments with a preferred pricing which is still valid [and is from correct vendor]
				// - only assignments with items that have valid price quotes [from current vendor]
				// - only assignments with items that have previously been received [from current vendor]
				// - No filtering based on history
				// The even control values represent the filter without Vendor checking. If vendor checking is selected we add one to the value.
				EnumValueTextRepresentations itemVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Storage Assignments which have a preferred Price Quote associated with them"),
								KB.K("Only include Storage Assignments for items which have a Price Quote associated with them"),
								KB.K("Only include Storage Assignments for items which have previously been received"),
								KB.K("Do not filter Storage Assignments based on Pricing or Purchasing History")
							},
					null,
					new object[] {
								0, 2, 4, 6
							}
				);
				object ATypeId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 6),
					Fmt.SetEnumText(itemVendorAssociations),
					Fmt.SetIsSetting(0)
				);
				object AVendorId = AddPickerFilterControl(KB.K("Use Pricing or Purchasing history only for the Purchase Order Template's Vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				EndPickerFilterControlGroup(KB.K("Storage Assignment filtering based on Pricing and Purchasing History"));

				// Make a calculated init value that adds one to the radio-button value if specific-vendor filtering is requested.
				EditorInitValue combined = new EditorCalculatedInitValue(
					new IntegralTypeInfo(false, 0, 7),
					delegate (object[] inputs) {
						return ((bool)inputs[1] ? 1 : 0) + (int)IntegralTypeInfo.AsNativeType(inputs[0], typeof(int));
					},
					new Thinkage.Libraries.Presentation.ControlValue(ATypeId), new Thinkage.Libraries.Presentation.ControlValue(AVendorId));
				// IL's with a preferred pricing
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemPriceID).IsNotNull()),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				// IL's with a preferred pricing from this vendor
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemPriceID.F.VendorID)
						.Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 1))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);
				// IL's with a price quote
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ItemPrice.F.ItemID) },
							null,
							null).SetDistinct(true))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 2);
					}
				);
				// IL's with a price quote from this vendor
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ItemPrice.F.ItemID) },
							new SqlExpression(dsMB.Path.T.ItemPrice.F.VendorID).Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
							null).SetDistinct(true))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 3);
					}
				);
				// IL's for items with previous receiving (this one is kinda useless, most any Items will have receiving)
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID) },
							null,
							null).SetDistinct(true))
					.Or(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID) },
							null,
							null).SetDistinct(true)))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 4);
					}
				);
				// IL's for items with previous receiving from this vendor.
				AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ItemLocationID.F.ItemID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemPO.F.ReceiptID.F.PurchaseOrderID.F.VendorID).Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
							null).SetDistinct(true))
					.Or(new SqlExpression(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.ItemLocationID.F.ItemID) },
							new SqlExpression(dsMB.Path.T.ReceiveItemNonPO.F.VendorID).Eq(new SqlExpression(dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
							null).SetDistinct(true)))),
					combined,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 5);
					}
				);
			}
			#endregion

			#region - POLineLaborTemplateAssignment
			public void POLineLaborTemplateAssignment(object toOrderId) {
				AddPickerPanelDisplay(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.DemandTemplateID.F.WorkOrderTemplateID.F.Code);
				AddPickerPanelDisplay(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Code);
				CreateResourceControl(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID, Fmt.SetPickFrom(TIWorkOrder.DemandLaborOutsideTemplateForPOLineTemplatePickerTblCreator));

				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Demanded"), KB.K("Already Ordered"), KB.K("Remaining Demand") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineDemandedId))),
						TblColumnNode.New(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.OrderQuantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineOrderedId))),
						TblUnboundControlNode.New(KB.K("Recommended To Order"), QuantityTypeInfoWithZero, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(toOrderId)))
					)
				));
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Cost, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredUnitCostId))));
				Actions.Add(RemainingCalculator<System.TimeSpan>(POLineDemandedId, POLineOrderedId, toOrderId));
			}
			#endregion
			#region - POLineLaborTemplatePOTemplateFilter
			public void POLineLaborTemplatePOTemplateFilter() {
				// A - Work vendor association:
				// - Only PO's from vendors who can supply the demanded work
				// - no filtering based on vendor/work associations
				object AId = AddPickerFilterControl(KB.K("Only include Purchase Order Templates for Vendors who can perform this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.VendorID, 1)
							.Eq(new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID))
							.IsFalse().Not()),
					AId,
					null
				);

				// B - Vendor history:
				// - only POs for vendors that have previously been done the work
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include Purchase Order Templates for Vendors who have previously performed this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// PurchaseOrderTemplateID.VendorID IN
				//		(select distinct ALOPO.POLL.POL.PO.VendorID from ALOPO where ALOPO.POLL.DLO.LO = editor's Labor Outside
				//	union
				//		select distinct ALONPO.VendorID from ALONPO where ALONPO.DLO.LO = editor's Labor Outside)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: CorrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);
			}
			#endregion
			#region - POLineLaborTemplateResourceFilter
			public void POLineLaborTemplateResourceFilter() {
				// A - Work vendor association:
				// - only demands for work specifically defined to be from this vendor
				// - all demands for work this vendor could provide (including records with null vendor)
				// - all demands for work amy vendor might supply (no filter)
				EnumValueTextRepresentations demandVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Demand Templates for work specifically from this Purchase Order Template's Vendor"),
								KB.K("Only include Demand Templates for work that this Purchase Order Template's Vendor could perform"),
								KB.K("Do not filter Demand Templates based on the Vendor associated with the work")
							},
					null,
					new object[] {
								0, 1, 2
							}
				);
				object AId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 2),
					Fmt.SetEnumText(demandVendorAssociations),
					Fmt.SetIsSetting(0)
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 1))),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 1))
							.IsFalse().Not()),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);

				// B - Vendor history:
				// - only demands for work that has previously been received
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include demands for work previously performed by this vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// DemandLaborOutide.LaborOutsideID IN
				//		(select distinct ALOPO.POLL.DLOID.LOID from ALOPO where ALOPO.ReceiptID.POID.VendorID = editor's vendor
				//	union
				//		select distinct ALONPO.DLOID.LOID from ALONPO where ALONPO.VendorID = editor's vendor)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineLaborTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);

				// C - quantity status:
				// - only demands where ordered < demanded
				// - No filtering based on quantity.
				// TODO: Want a way for this control to start off true without having to provide a creator delegate in the TblLeafNode. We could do it with another INit.
				object CId = AddPickerFilterControl(KB.K("Only include demands where quantity demanded exceeds quantity currently ordered"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.DemandLaborOutsideTemplate.F.Quantity).Gt(new SqlExpression(dsMB.Path.T.DemandLaborOutsideTemplate.F.OrderQuantity))), CId, null);
			}
			#endregion

			#region - POLineOtherWorkTemplateAssignment
			public void POLineOtherWorkTemplateAssignment(object toOrderId) {
				AddPickerPanelDisplay(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.DemandTemplateID.F.WorkOrderTemplateID.F.Code);
				AddPickerPanelDisplay(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Code);
				CreateResourceControl(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID, Fmt.SetPickFrom(TIWorkOrder.DemandOtherWorkOutsideTemplateForPOLineTemplatePickerTblCreator));

				DetailColumns.Add(TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Demanded"), KB.K("Already Ordered"), KB.K("Remaining Demand") },
					TblRowNode.New(QuantityLabel, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.Quantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineDemandedId))),
						TblColumnNode.New(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OrderQuantity, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(POLineOrderedId))),
						TblUnboundControlNode.New(KB.K("Recommended To Order"), QuantityTypeInfoWithZero, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(toOrderId)))
					)
				));
				DetailColumns.Add(TblColumnNode.New(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Cost, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(RequiredUnitCostId))));
				Actions.Add(RemainingCalculator<long>(POLineDemandedId, POLineOrderedId, toOrderId));
			}
			#endregion
			#region - POLineOtherWorkTemplatePOTemplateFilter
			public void POLineOtherWorkTemplatePOTemplateFilter() {
				// A - Work vendor association:
				// - Only PO's from vendors who can supply the demanded work
				// - no filtering based on vendor/work associations
				object AId = AddPickerFilterControl(KB.K("Only include Purchase Order Templates for Vendors who can perform this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.VendorID, 1)
							.Eq(new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID))
							.IsFalse().Not()),
					AId,
					null
				);

				// B - Vendor history:
				// - only POs for vendors that have previously been done the work
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include Purchase Order Templates for Vendors who have previously performed this work"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// PurchaseOrderTemplateID.VendorID IN
				//		(select distinct ALOPO.POLL.POL.PO.VendorID from ALOPO where ALOPO.POLL.DLO.LO = editor's Other Work Outside
				//	union
				//		select distinct ALONPO.VendorID from ALONPO where ALONPO.DLO.LO = editor's Other Work Outside)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.PurchaseOrderTemplate.F.VendorID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);
			}
			#endregion
			#region - POLineOtherWorkTemplateResourceFilter
			public void POLineOtherWorkTemplateResourceFilter() {
				// A - Work vendor association:
				// - only demands for work specifically defined to be from this vendor
				// - all demands for work this vendor could provide (including records with null vendor)
				// - all demands for work amy vendor might supply (no filter)
				EnumValueTextRepresentations demandVendorAssociations = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only include Demand Templates for work specifically from this Purchase Order Template's Vendor"),
								KB.K("Only include Demand Templates for work that this Purchase Order Template's Vendor could provide"),
								KB.K("Do not filter Demand Templates based on the Vendor associated with the work")
							},
					null,
					new object[] {
								0, 1, 2
							}
				);
				object AId = AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 2),
					Fmt.SetEnumText(demandVendorAssociations),
					Fmt.SetIsSetting(0)
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 1))),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 0);
					}
				);
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.F.VendorID)
							.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 1))
							.IsFalse().Not()),
					AId,
					delegate (object o) {
						return IntegralTypeInfo.Equals(o, 1);
					}
				);

				// B - Vendor history:
				// - only demands for work that has previously been received
				// - No filtering based on history
				object BId = AddPickerFilterControl(KB.K("Only include demands for work previously provided by this vendor"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				// To search both PO and NonPO history the following filter should be of the form
				// DemandOtherWorkOutide.OtherWorkOutsideID IN
				//		(select distinct ALOPO.POLL.DLOID.LOID from ALOPO where ALOPO.ReceiptID.POID.VendorID = editor's vendor
				//	union
				//		select distinct ALONPO.DLOID.LOID from ALONPO where ALONPO.VendorID = editor's vendor)
				// but we have no support for union queries.
				// So instead we code (xxx in (query1)) or (xxx in (query2))
				// TODO: Each of these subqueries needs the additional conditions: COrrectionID = ID AND Quantity > 0 so that receiving that has
				// been corrected to zero does not count.
				AddPickerFilter(
					BTbl.ExpressionFilter(
						new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID.F.PurchaseOrderID.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
								null
							).SetDistinct(true))
						.Or(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID)
							.In(new SelectSpecification(
								null,
								new SqlExpression[] { new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID) },
								new SqlExpression(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID)
									.Eq(new SqlExpression(dsMB.Path.T.POLineOtherWorkTemplate.F.POLineTemplateID.F.PurchaseOrderTemplateID.F.VendorID, 2)),
								null
							).SetDistinct(true)))),
					BId,
					null
				);

				// C - quantity status:
				// - only demands where ordered < demanded
				// - No filtering based on quantity.
				// TODO: Want a way for this control to start off true without having to provide a creator delegate in the TblLeafNode. We could do it with another INit.
				object CId = AddPickerFilterControl(KB.K("Only include demands where quantity demanded exceeds quantity currently ordered"), BoolTypeInfo.NonNullUniverse, Fmt.SetIsSetting(false));
				AddPickerFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.Quantity).Gt(new SqlExpression(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OrderQuantity))), CId, null);
			}
			#endregion
			#endregion
		}
		#endregion
		#endregion
		#region Calculators
		#region Quantity/Unit/Total Triple Control Calculators
		public static Check QuantityUnitTotalTripleCalculator<T>(object quantityCol, object unitCostCol, object totalCostCol)
			where T : struct, System.IComparable<T> {
			return new Check3<T?, decimal?, decimal?>()
				.Operand1(quantityCol,
					delegate (decimal? unit, decimal? total) {
						return !unit.HasValue || Compute.IsZero<decimal>(unit) || !total.HasValue
							? null : checked(Compute.Divide<T>(total, unit));
					})
				.Operand2(unitCostCol,
					delegate (T? quantity, decimal? total) {
						return !quantity.HasValue || Compute.IsZero<T>(quantity) || !total.HasValue
							? null : checked(Compute.Divide<T>(total, quantity));
					})
				.Operand3(totalCostCol,
					delegate (T? quantity, decimal? unit) {
						return !quantity.HasValue || !unit.HasValue
							? null : checked(Compute.Multiply<T>(unit, quantity));
					});
		}
		#endregion
		#region Remaining [C <- max(0, A-B))]
		public static Check RemainingCalculator<T>(object originalCol, object usedCol, object remainingCol) where T : struct, System.IComparable<T> {
			return new Check3<T?, T?, T?>()
				.Operand1(originalCol)
				.Operand2(usedCol)
				.Operand3(remainingCol,
					delegate (T? quantity, T? used) {
						return Compute.Remaining(quantity, used);
					});
		}
		#endregion
		#region Total Cost from quantity and unit cost
		// This is used by POLine Template tbls and (indirectly) by Demand tbls
		public static Check TotalFromQuantityAndUnitCostCalculator<T>(
			object quantityCol, object unitCostCol, object totalCostCol) where T : struct {
			return new Check3<T?, decimal?, decimal?>()
				.Operand1(quantityCol)
				.Operand2(unitCostCol)
				.Operand3(totalCostCol, delegate (T? quantity, decimal? unit) {
					return !quantity.HasValue || !unit.HasValue ? null : checked(Compute.Multiply<T>(unit, quantity));
				});
		}
		#endregion
		#region Total Cost from quantity and pricing
		public static Check TotalFromQuantityAndPricingCalculator<T>(
			object quantityCol, object pricingQuantityCol, object pricingCostCol, object totalCostCol)
			where T : struct, System.IComparable<T> {
			return new Check4<T?, T?, decimal?, decimal?>()
				.Operand1(quantityCol)
				.Operand2(pricingQuantityCol)
				.Operand3(pricingCostCol)
				.Operand4(totalCostCol, delegate (T? quantity, T? pricingQuantity, decimal? pricingCost) {
					return Compute.TotalFromQuantityAndBasisCost<T>(quantity, pricingQuantity, pricingCost);
				});
		}
		#endregion
		#region Unit Cost from quantity and total
		public static Check UnitCostFromQuantityAndTotalCalculator<Q>(
			object quantityCol, object totalCostCol, object unitCostCol) where Q : struct, System.IComparable<Q> {
			return new Check3<Q?, decimal?, decimal?>()
				.Operand1(quantityCol)
				.Operand2(totalCostCol)
				.Operand3(unitCostCol, delegate (Q? quantity, decimal? total) {
					return !quantity.HasValue || Compute.IsZero<Q>(quantity) || !total.HasValue
						? null : checked(Compute.Divide(total, quantity));
				});
		}
		#endregion
		#region EffectiveDate limitations for History tables along with UserID init.
		internal static List<TblActionNode> StateHistoryInits(MB3Client.StateHistoryTable histInfo, out TblLayoutNodeArray extraNodes) {
			TypeInfo dateLimitType = histInfo.HistEffectiveDatePath.ReferencedColumn.EffectiveType.UnionCompatible(NullTypeInfo.Universe);
			extraNodes = new TblLayoutNodeArray(
				TblUnboundControlNode.StoredEditorValue(TIGeneralMB3.StateHistoryPreviousEffectiveDateId, dateLimitType),
				TblUnboundControlNode.StoredEditorValue(TIGeneralMB3.StateHistoryNextEffectiveDateId, dateLimitType),
				TblUnboundControlNode.StoredEditorValue(TIGeneralMB3.CurrentStateHistoryIdWhenCalledId, histInfo.MainToCurrentStateHistoryPath.ReferencedColumn.EffectiveType)
			);

			var result = new List<TblActionNode>();
			// Verify that the current state history record has not been changed since the caller's enablers were refreshed.
			// The caller passes us an init of what it thought the current state history ID was, and we verify that this is the same as
			// what we loaded. If not it means someone has added a new state history record between the caller's refresh and the user editing the state history record.
			// If we are passed a null, we accept it as well. This covers the case of editing an existing record and also regular New operations (which, as done here
			// are the "add comment" which does not change the state; the state shown in the editor may differ from what was seen in the caller but we always allow
			// transitions from a state to itself.
			// TODO: Perhaps we should just check if the State ID has changed?
			// TODO: Need similar checking for any transition-specific conditions (e.g. temporary storage empty) but this does not require an init from the caller,
			// but should only take place in New/Clone, not Edit modes.
			result.Add(new Check3<object, object, object>(
					(object idWhenCalled, object idWhenLoaded, object codeWhenLoaded)
						=> idWhenCalled == null || histInfo.MainToCurrentStateHistoryPath.ReferencedColumn.EffectiveType.GenericEquals(idWhenCalled, idWhenLoaded)
							? null
							: EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("The current state has changed")))
				)
				.Operand1(TIGeneralMB3.CurrentStateHistoryIdWhenCalledId)                           // This is supplied by the caller and was the basis for the enabling of the Edit command
				.Operand2(KB.K("Current State History Id"), new DBI_Path(histInfo.HistToMainPath.PathToReferencedRow, histInfo.MainToCurrentStateHistoryPath)) // This is the current state ID when the record was loaded, which will be concurrency-checked on save
				.Operand3(TIGeneralMB3.CurrentStateHistoryCodeWhenLoadedId));                       // We just use this as a place to put the error flag
			result.Add(Init.OnLoad(
				new ControlReadonlyTarget(TIGeneralMB3.StateHistoryEffectiveDateId, KB.K("Readonly because this state history record was generated by MainBoss")),
				new EditorPathValue(histInfo.HistEffectiveDateReadonlyPath)));
			result.Add(Init.OnLoadNew(histInfo.HistUserIDPath, new UserIDValue()));
			// When editing an existing record the lower date limit (if any) comes from the previous record on the same PO (if any).
			result.Add(Init.OnLoadEditUndelete(new ControlTarget(TIGeneralMB3.StateHistoryPreviousEffectiveDateId), new EditorCalculatedInitValue(dateLimitType,
				delegate (object[] inputs) {
					// inputs[0] is the id of the history record being edited; inputs[1] is the id of the PO record.
					var innerQuery = new SelectSpecification(histInfo.HistTable, new SqlExpression[] { new SqlExpression(histInfo.HistEffectiveDatePath) },
						new SqlExpression(histInfo.HistTable.InternalId).Eq(SqlExpression.Constant(inputs[0])), null);
					var outerQuery = new SelectSpecification(histInfo.HistTable, new SqlExpression[] { new SqlExpression(histInfo.HistEffectiveDatePath).Max() },
						new SqlExpression(histInfo.HistToMainPath).Eq(SqlExpression.Constant(inputs[1]))
							.And(new SqlExpression(histInfo.HistEffectiveDatePath).Lt(SqlExpression.ScalarSubquery(innerQuery))), null);
					return Libraries.Application.Instance.GetInterface<Libraries.DBAccess.IApplicationWithSingleDatabaseConnection>().Session.Session.ExecuteCommandReturningScalar(histInfo.HistEffectiveDatePath.ReferencedColumn.EffectiveType.UnionCompatible(NullTypeInfo.Universe), outerQuery);
				}, new EditorPathValue(histInfo.HistTable.InternalId), new EditorPathValue(histInfo.HistToMainPath))));
			// In New/Clone mode the lower bound (if any) comes from the current history record (if any).
			result.Add(Init.OnLoadNewClone(new ControlTarget(TIGeneralMB3.StateHistoryPreviousEffectiveDateId), new EditorPathValue(new DBI_Path(histInfo.HistToMainPath.PathToReferencedRow, new DBI_Path(histInfo.MainToCurrentStateHistoryPath.PathToReferencedRow, histInfo.HistEffectiveDatePath)))));
			// When editing an existing record the upper date limit (if any) comes from the next record on the same PO (if any).
			result.Add(Init.OnLoadEditUndelete(new ControlTarget(TIGeneralMB3.StateHistoryNextEffectiveDateId), new EditorCalculatedInitValue(dateLimitType,
				delegate (object[] inputs) {
					// inputs[0] is the id of the history record being edited; inputs[1] is the id of the PO record.
					var innerQuery = new SelectSpecification(histInfo.HistTable, new SqlExpression[] { new SqlExpression(histInfo.HistEffectiveDatePath) },
						new SqlExpression(histInfo.HistTable.InternalId).Eq(SqlExpression.Constant(inputs[0])), null);
					var outerQuery = new SelectSpecification(histInfo.HistTable, new SqlExpression[] { new SqlExpression(histInfo.HistEffectiveDatePath).Min() },
						new SqlExpression(histInfo.HistToMainPath).Eq(SqlExpression.Constant(inputs[1]))
							.And(new SqlExpression(histInfo.HistEffectiveDatePath).Gt(SqlExpression.ScalarSubquery(innerQuery))), null);
					return Libraries.Application.Instance.GetInterface<Libraries.DBAccess.IApplicationWithSingleDatabaseConnection>().Session.Session.ExecuteCommandReturningScalar(histInfo.HistEffectiveDatePath.ReferencedColumn.EffectiveType.UnionCompatible(NullTypeInfo.Universe), outerQuery);
				}, new EditorPathValue(histInfo.HistTable.InternalId), new EditorPathValue(histInfo.HistToMainPath))));
			// In new/clone mode there is no upper bound.
			result.Add(Init.OnLoadNewClone(new ControlTarget(TIGeneralMB3.StateHistoryNextEffectiveDateId), new ConstantValue(null)));
			result.Add(new Check3<DateTime, DateTime?, DateTime?>(
						delegate (DateTime edate, DateTime? prevdate, DateTime? nextdate) {
							// TODO: For these error messages, we really want the date formatted the same as in the date control...
							// The ToString calls are here because whatever ToString operation Strings.Format applies is really ugly.
							if ((prevdate.HasValue && edate <= prevdate.Value)
								|| (nextdate.HasValue && edate >= nextdate.Value)) {
								if (prevdate.HasValue && nextdate.HasValue)
									return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Date must be after {0} and before {1}."), prevdate.Value.ToString(), nextdate.Value.ToString()));
								else if (nextdate.HasValue)
									return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Date must be before {0}."), nextdate.Value.ToString()));
								else
									return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Date must be after {0}."), prevdate.Value.ToString()));
							}
							return null;
						})
						.Operand1(TIGeneralMB3.StateHistoryEffectiveDateId)
						.Operand2(TIGeneralMB3.StateHistoryPreviousEffectiveDateId)
						.Operand3(TIGeneralMB3.StateHistoryNextEffectiveDateId));
			return result;
		}
		#endregion
		#endregion
		static protected EdtMode[] NewAndCloneOnly = new EdtMode[] { EdtMode.New, EdtMode.Clone };

		#region ICTorArg Common definitions
		protected static readonly CompositeView.ICtorArg NoContextFreeNew = CompositeView.SetAllowContextFreeNew(false);
		// TODO: Check that ReadonlyView is not required with (i.e. implied by) ForceNotPrimary and if so remove such specifications.
		// Also add a debug check to forbid mixing ForceNotPrimary and ContextualInit (if there isn't one already and the combination is indeed useless)
		/// <summary>
		/// This attribute can be applied to CompositeViews that want full editor access but do not want the editor to re-enter New mode if it did not start there.
		/// Since this view allows creation of records, this attribute also allows Deletion and Undeletion (assuming the referenced edit tbl also allows these)
		/// </summary>
		protected static readonly CompositeView.ICtorArg NoNewMode = CompositeView.EditorAccess(false, EdtMode.New, EdtMode.EditDefault, EdtMode.ViewDefault);
		/// <summary>
		/// This attribute can be applied to CompositeViews that should only provide readonly access to the underlying records. It forbids all
		/// edit modes capable of modifying the record. There should be a debug check in CompositeView
		/// that this cannot be used with ContextualInit (which would imply that New mode *is* allowed!) except that all the tbl-checking code is below the MB
		/// level.
		/// </summary>
		protected static readonly CompositeView.ICtorArg ReadonlyView = CompositeView.EditorAccess(false, EdtMode.New, EdtMode.Edit, EdtMode.Clone, EdtMode.Delete, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault);
		/// <summary>
		/// This attribute can be applied to CompositeViews that should only provide readonly access to the underlying records while still allowing them to be deleted.
		/// It forbids all edit modes capable of modifying the record. There should be a debug check in CompositeView
		/// that this cannot be used with ContextualInit (which would imply that New mode *is* allowed!) except that all the tbl-checking code is below the MB
		/// level.
		/// </summary>
		protected static readonly CompositeView.ICtorArg ReadonlyWithDeleteView = CompositeView.EditorAccess(false, EdtMode.New, EdtMode.Edit, EdtMode.Clone, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault);
		/// <summary>
		/// This attribute can be applied to CompositeViews that should only allow Edit/Viewing of existing records. It forbids all
		/// edit modes capable of creating or deleting records. There should be a debug check in CompositeView
		/// that this cannot be used with ContextualInit (which would imply that New mode *is* allowed!) except that all the tbl-checking code is below the MB
		/// </summary>
		protected static readonly CompositeView.ICtorArg OnlyViewEdit = CompositeView.EditorAccess(false, EdtMode.New, EdtMode.Clone, EdtMode.Delete, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault);
		/// <summary>
		/// Attribute used where all fields in the editor are readonly (because they are captive or specify readonly access, etc.) and the view mode editor doesn't show any useful information beyond what is already
		/// displayed in the browser panel, but it is still useful to create or delete records.
		/// </summary>
		protected static readonly CompositeView.ICtorArg NoViewEdit = CompositeView.EditorAccess(false, EdtMode.Edit, EdtMode.View);
		/// <summary>
		/// This attribute can be applied to CompositeViews that should only show the record in the Panel, while forbidding all access to the editor
		/// (except if a contextual New verb is coded but that is discouraged). There should be a debug check in CompositeView
		/// that this cannot be used with ContextualInit (which would imply that New mode *is* allowed!) except that all the tbl-checking code is below the MB
		/// level.
		/// </summary>
		protected static readonly CompositeView.ICtorArg PanelOnly = CompositeView.EditorAccess(false, EdtMode.New, EdtMode.View, EdtMode.ViewDeleted, EdtMode.Edit, EdtMode.Clone, EdtMode.Delete, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault);
		/// <summary>
		/// This attribute can be applied to CompositeViews that should only show the record in the Panel and allow it to be deleted, while forbidding all access to the editor
		/// (except if a contextual New verb is coded but that is discouraged). There should be a debug check in CompositeView
		/// that this cannot be used with ContextualInit (which would imply that New mode *is* allowed!) except that all the tbl-checking code is below the MB
		/// level.
		/// </summary>
		protected static readonly CompositeView.ICtorArg PanelOnlyWithDelete = CompositeView.EditorAccess(false, EdtMode.New, EdtMode.View, EdtMode.ViewDeleted, EdtMode.Edit, EdtMode.Clone, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault);

		#endregion
		#region General Tbl definitions
		public static readonly DelayedCreateTbl ContactFunctionsBrowsetteTblCreator = null;
		public static readonly DelayedCreateTbl AccountingTransactionDerivationsTblCreator;
		public static readonly DelayedCreateTbl CompanyInformationTblCreator;
		public static readonly DelayedCreateTbl NewsPanelTblCreator;
		public static readonly DelayedCreateTbl AdministrationTblCreator;
		public static readonly DelayedCreateTbl SettingsTblCreator;
		public static readonly DelayedCreateTbl AssignmentStatusTblCreator;
		public static readonly DelayedCreateTbl UserMessageKeyWithEditAbilityTblCreator;
		public static readonly DelayedCreateTbl UserMessageKeyPickerTblCreator;
		public static readonly DelayedCreateTbl DatabaseManagementTblCreator;
		#region ContactFunctionProvider
		private static object[] ContactFunctionValues = new object[] {
			(int)ViewRecordTypes.ContactFunctions.Contact,
			(int)ViewRecordTypes.ContactFunctions.Requestor,
			(int)ViewRecordTypes.ContactFunctions.BillableRequestor,
			(int)ViewRecordTypes.ContactFunctions.Employee,
			(int)ViewRecordTypes.ContactFunctions.SalesVendor,
			(int)ViewRecordTypes.ContactFunctions.ServiceVendor,
			(int)ViewRecordTypes.ContactFunctions.AccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.SalesServiceVendor,
			(int)ViewRecordTypes.ContactFunctions.SalesAccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.ServiceAccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.SalesServiceAccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.RequestAssignee,
			(int)ViewRecordTypes.ContactFunctions.WorkOrderAssignee,
			(int)ViewRecordTypes.ContactFunctions.PurchaseOrderAssignee,
			(int)ViewRecordTypes.ContactFunctions.User
		};
		private static Key[] ContactFunctionLabels = new Key[] {
			KB.K("Contact"),
			KB.TOi(TId.Requestor),
			KB.TOi(TId.BillableRequestor),
			KB.TOi(TId.Employee),
			KB.K("Sales Vendor"),
			KB.K("Service Vendor"),
			KB.K("Accounts Payable Vendor"),
			KB.K("Sales Service Vendor"),
			KB.K("Sales Accounts Payable Vendor"),
			KB.K("Service Accounts Payable Vendor"),
			KB.K("Sales Service Accounts Payable Vendor"),
			KB.TOi(TId.RequestAssignee),
			KB.TOi(TId.WorkOrderAssignee),
			KB.TOi(TId.PurchaseOrderAssignee),
			KB.TOi(TId.User)
		};
		public static EnumValueTextRepresentations ContactFunctionProvider = new EnumValueTextRepresentations(ContactFunctionLabels, null, ContactFunctionValues);
		#endregion
		#region Name Providers
		public static EnumValueTextRepresentations UnassignedNameProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("*Unassigned")
			},
			null,
			new object[] {
				KnownIds.UnassignedID
			}
		);
		public static EnumValueTextRepresentations GlobalSettingsNameProvider = new EnumValueTextRepresentations(
			new Key[] {
					KB.K("*Global")
				},
			null,
			new object[] {
					KnownIds.UnassignedID.ToByteArray().Concat(new byte[16] ).ToArray()
				}
		);
		#endregion
		#region UserMessageKey
		private static DelayedCreateTbl UserMessageKeyTblCreator(bool contextIsFiltered, bool allowKeyModifications) {
			return new DelayedCreateTbl(delegate () {
				ETbl editTbl;
				BTbl browseTbl;

				if (allowKeyModifications || contextIsFiltered) {
					// It is expected that things that allow UserMessageKey creation/modification will have a filter on the Context to restrict the new keys specifically to the particular context. For this reason,
					// we do not show the Context information. Note this will cause a CheckTableInfo warning of : No ListArg in closed context for XID identified by name(s) UserMessageKey.Context: Message
					if (!allowKeyModifications)
						editTbl = new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View));
					else
						editTbl = new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View));
					browseTbl = new BTbl(BTbl.ListColumn(dsMB.Path.T.UserMessageKey.F.Key));
				}
				else {
					editTbl = new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View));
					browseTbl = new BTbl(BTbl.ListColumn(dsMB.Path.T.UserMessageKey.F.Context), BTbl.ListColumn(dsMB.Path.T.UserMessageKey.F.Key));
				}

				return new Tbl(dsMB.Schema.T.UserMessageKey, TId.Message,
					new Tbl.IAttr[] {
						MainBossServiceAdminGroup,
						browseTbl,
						editTbl
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							contextIsFiltered ?
								TblColumnNode.New(dsMB.Path.T.UserMessageKey.F.Context)
								:
								TblColumnNode.New(dsMB.Path.T.UserMessageKey.F.Context, DCol.Normal, allowKeyModifications ? ECol.AllReadonly : ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.UserMessageKey.F.Key, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.UserMessageKey.F.Comment, DCol.Normal),
							TblInitSourceNode.New(KB.K("Current Translation"), new DualCalculatedInitValue(TranslationKeyTypeInfo.Universe, delegate (object[] inputs) {
								if (inputs[0] == null || inputs[1] == null)
									return null;
								return new Thinkage.Libraries.Translation.SimpleKey(ContextReference.New((string)inputs[0]), (string)inputs[1]);

							}, new DualPathValue(dsMB.Path.T.UserMessageKey.F.Context), new DualPathValue(dsMB.Path.T.UserMessageKey.F.Key)), DCol.Normal, ECol.AllReadonly, Fmt.SetLineCount(5)),
							BrowsetteTabNode.New(TId.MessageTranslation, TId.Message,
								TblColumnNode.NewBrowsette(dsMB.Path.T.UserMessageTranslation.F.UserMessageKeyID, DCol.Normal, ECol.Normal))
						)
					)
				);
			});
		}
		#endregion
		#region RecordTypeViewColumnValue for ListColumn Views
		private static object RecordTypeColumnTagID = KB.I("RecordType");
		private static CompositeView.ICtorArg RecordTypeViewColumnValue() {
			return BTbl.PerViewColumnValue(RecordTypeColumnTagID, new BrowserCalculatedInitValue(TranslationKeyTypeInfo.NonNullUniverse, (args => ((Tbl.TblIdentification)args[0]).Compose("{0} Record Type")),
					new BrowseRecordTblIdentificationValue()), null);
		}
		#endregion
		private static TblLeafNode SingleWorkOrdersByEndDateHistogramNode(FeatureGroup featureGroup, DBI_Table tableSchema, List<BTbl.ICtorArg> otherBTblArgs) {
			otherBTblArgs.Add(
				BTbl.SetCustomListCreator(
					delegate (BrowseLogic browseLogic, TypeInfo rowIdType) {
						var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ShowColumnNames | UIListStyles.ChartStackedColumn);
						result.SetAxisXCrossing(0, DateTime.Today.ToOADate());  // TODO: Hook to update this in Refresh
						return result;
					}
				)
			);
			return TblColumnNode.NewBrowsette(
				new DelayedCreateTbl(
					new Tbl(tableSchema, TId.WorkOrderEndDateHistogram,
						new Tbl.IAttr[] { featureGroup, new BTbl(otherBTblArgs.ToArray()) },
						new TblLayoutNodeArray()
					)
				),
				DCol.Normal
			);
		}
		private static object TotalCounts(object[] values) {
			ulong total = 0;
			foreach (object o in values)
				total += (ulong)IntegralTypeInfo.AsNativeType(o, typeof(ulong));
			return total;
		}
		private static TblGroupNode WorkOrdersByEndDateHistogramGroup(FeatureGroup featureGroup, DBI_Table tableSchema,
				DBI_Path endDatePath, DBI_Path unrequestedPMCountPath, DBI_Path unrequestedCMCountPath, DBI_Path requestedPMCountPath, DBI_Path requestedCMCountPath, params BTbl.ICtorArg[] otherBTblArgs) {
			TypeInfo countType = unrequestedCMCountPath.ReferencedColumn.EffectiveType;
			var btblArgsWithSchedulingAndRequests = new List<BTbl.ICtorArg>(otherBTblArgs);
			btblArgsWithSchedulingAndRequests.Add(BTbl.ListColumn(endDatePath));
			btblArgsWithSchedulingAndRequests.Add(BTbl.ListColumn(unrequestedPMCountPath));
			btblArgsWithSchedulingAndRequests.Add(BTbl.ListColumn(unrequestedCMCountPath));
			btblArgsWithSchedulingAndRequests.Add(BTbl.ListColumn(requestedPMCountPath));
			btblArgsWithSchedulingAndRequests.Add(BTbl.ListColumn(requestedCMCountPath));

			var btblArgsWithScheduling = new List<BTbl.ICtorArg>(otherBTblArgs);
			btblArgsWithScheduling.Add(BTbl.ListColumn(endDatePath));
			btblArgsWithScheduling.Add(BTbl.ListColumn(unrequestedPMCountPath.Key(), new BrowserCalculatedInitValue(countType, TotalCounts, new BrowserPathValue(unrequestedPMCountPath), new BrowserPathValue(requestedPMCountPath)), null));
			btblArgsWithScheduling.Add(BTbl.ListColumn(unrequestedCMCountPath.Key(), new BrowserCalculatedInitValue(countType, TotalCounts, new BrowserPathValue(unrequestedCMCountPath), new BrowserPathValue(requestedCMCountPath)), null));

			var btblArgsWithRequests = new List<BTbl.ICtorArg>(otherBTblArgs);
			btblArgsWithRequests.Add(BTbl.ListColumn(endDatePath));
			btblArgsWithRequests.Add(BTbl.ListColumn(KB.K("Work Order Count"), new BrowserCalculatedInitValue(countType, TotalCounts, new BrowserPathValue(unrequestedPMCountPath), new BrowserPathValue(unrequestedCMCountPath)), null));
			btblArgsWithRequests.Add(BTbl.ListColumn(KB.K("Requested Work Order Count"), new BrowserCalculatedInitValue(countType, TotalCounts, new BrowserPathValue(requestedPMCountPath), new BrowserPathValue(requestedCMCountPath)), null));

			var btblArgsWithNeither = new List<BTbl.ICtorArg>(otherBTblArgs);
			btblArgsWithNeither.Add(BTbl.ListColumn(endDatePath));
			btblArgsWithNeither.Add(BTbl.ListColumn(KB.K("Work Order Count"), new BrowserCalculatedInitValue(countType, TotalCounts,
					new BrowserPathValue(unrequestedPMCountPath), new BrowserPathValue(unrequestedCMCountPath),
					new BrowserPathValue(requestedPMCountPath), new BrowserPathValue(requestedCMCountPath)
				),
				null));

			return TblGroupNode.New(KB.K("Number of Open Work Orders by Work End Date"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
				SingleWorkOrdersByEndDateHistogramNode(featureGroup & SchedulingGroup & RequestsGroup, tableSchema, btblArgsWithSchedulingAndRequests),
				SingleWorkOrdersByEndDateHistogramNode(featureGroup & SchedulingGroup & !RequestsGroup, tableSchema, btblArgsWithScheduling),
				SingleWorkOrdersByEndDateHistogramNode(featureGroup & !SchedulingGroup & RequestsGroup, tableSchema, btblArgsWithRequests),
				SingleWorkOrdersByEndDateHistogramNode(featureGroup & !SchedulingGroup & !RequestsGroup, tableSchema, btblArgsWithNeither)
			);
		}
		/// <summary>
		/// This method produces a filter to apply to a containing-location picker so the picker does not offer the edit record or any of its descendents.
		/// This means that the user is not given the chance to make a looped structure.
		/// Note that the code that generates Default tbl's from regular ones is unable to recognize and map the editorLocationIdPath and so this should only be
		/// used on layout nodes that specify new NonDefaultCol() or for which a Default Tbl is never generated. If the code generating Default tbl's worked properly
		/// the filter would be valid but would never filter anything out as it would be using the Id of the _DLocation table row as the search value.
		/// </summary>
		/// <param name="editorLocationIdPath"></param>
		/// <param name="pickerResultLocationIDPath"></param>
		/// <returns></returns>
		public static Fmt.ICtorArg FilterOutContainedLocations(DBI_Path editorLocationIdPath, DBI_Path pickerResultLocationIDPath) {
			return Fmt.SetBrowserFilter(BTbl.ExpressionFilter(
							new SqlExpression(pickerResultLocationIDPath)
								.In(new SelectSpecification(dsMB.Schema.T.LocationContainment,
															new[] { new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainedLocationID) },
															new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainingLocationID).Eq(new SqlExpression(editorLocationIdPath, 2)),
															null)).Not()));
		}
		/// <summary>
		/// Initialize a new Tbl table to contain descriptions of all the tables and
		/// columns and their associated attributes.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		static TIGeneralMB3() {
			#region ApplicationIdProvider
			List<Key> applicationName = new List<Key>();
			List<object> applicationIds = new List<object>();
			foreach (int i in (int[])System.Enum.GetValues(typeof(DatabaseEnums.ApplicationModeID))) {
				if (i != (int)DatabaseEnums.ApplicationModeID.NextAvailable) { // skip the Unknown one
					applicationName.Add(DatabaseEnums.ApplicationModeName((DatabaseEnums.ApplicationModeID)i));
					applicationIds.Add(i);
				}
			}
			ApplicationIdProvider = new EnumValueTextRepresentations(applicationName.ToArray(), null, applicationIds.ToArray());
			#endregion

			#region AccountingTransaction
			AccountingTransactionDerivationsTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.AccountingTransaction, TId.Accounting,
					new Tbl.IAttr[] {
						AccountingGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.AccountingTransaction.F.EffectiveDate),
							BTbl.PerViewListColumn(KB.K("Type"), RecordTypeColumnTagID),
							BTbl.ListColumn(dsMB.Path.T.AccountingTransaction.F.FromCostCenterID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.AccountingTransaction.F.ToCostCenterID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.AccountingTransaction.F.AccountingSystemTransactionID, Fmt.SetDynamicSizing()),
							BTbl.ListColumn(dsMB.Path.T.AccountingTransaction.F.Cost)
						),
						TIReports.NewRemotePTbl(TIReports.AccountingTransactionReport)
					},
					null,
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualItemID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualLaborInsideID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsideNonPOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsidePOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualMiscellaneousWorkOrderCostID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkInsideID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsideNonPOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsidePOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ChargebackLineID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ItemAdjustmentID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ItemCountValueID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ItemCountValueVoidID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ItemIssueID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ItemTransferID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ReceiveItemNonPOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ReceiveItemPOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue()),
					new CompositeView(dsMB.Path.T.AccountingTransaction.F.ReceiveMiscellaneousPOID, CompositeView.RecognizeByValidEditLinkage(), ReadonlyView, RecordTypeViewColumnValue())
				);
			});

			#endregion
			#region ContactFunctions
			ContactFunctionsBrowsetteTblCreator = new DelayedCreateTbl(delegate () {
				Key assigneeGroup = KB.K("New Assignee");
				return new CompositeTbl(dsMB.Schema.T.ContactFunctions, TId.ContactFunction,
					new Tbl.IAttr[] {
						new BTbl(BTbl.ListColumn(dsMB.Path.T.ContactFunctions.F.TableEnum))
					},
					dsMB.Path.T.ContactFunctions.F.TableEnum,
					null,               // Table 0 (Contact)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.RequestorID,               // Table 1 (Requestor)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Requestor.F.ContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.BillableRequestorID,       // Table 2 (BillableRequestor)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.BillableRequestor.F.ContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.EmployeeID,                // Table 3 (Employee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Employee.F.ContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID,                  // Table 4 (Sales Vendor)
						CompositeView.ContextFreeInit(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Vendor.F.SalesContactID),
						CompositeView.ContextFreeInit(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Vendor.F.ServiceContactID),
						CompositeView.ContextFreeInit(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Vendor.F.PayablesContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 5 (Service Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 6 (Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 7 (Sales & Service Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 8 (Sales & Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 9 (Service & Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 10 (Sales, Service & Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.RequestAssigneeID,     // Table 11 (RequestAssignee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.RequestAssignee.F.ContactID),
						CompositeView.NewCommandGroup(assigneeGroup)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.WorkOrderAssigneeID,   // Table 12(WorkOrderAssignee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.WorkOrderAssignee.F.ContactID),
						CompositeView.NewCommandGroup(assigneeGroup)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.PurchaseOrderAssigneeID,// Table 13 (PurchaseOrderRequestAssignee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.PurchaseOrderAssignee.F.ContactID),
						CompositeView.NewCommandGroup(assigneeGroup)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.UserID,                // Table 14 (User)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.User.F.ContactID),
						NoNewMode)
				);
			});
			#endregion
			#region UserMessageKey & UserMessageTranslation
			DefineTbl(dsMB.Schema.T.UserMessageKey, UserMessageKeyTblCreator(false, false));
			UserMessageKeyWithEditAbilityTblCreator = UserMessageKeyTblCreator(true, true);
			UserMessageKeyPickerTblCreator = UserMessageKeyTblCreator(true, false);

			DefineTbl(dsMB.Schema.T.UserMessageTranslation, delegate () {
				return new Tbl(dsMB.Schema.T.UserMessageTranslation, TId.MessageTranslation,
					new Tbl.IAttr[] {
						AdminGroup,
						new MinimumDBVersionTbl(new Version(1, 0, 10, 6)),
						new BTbl(BTbl.ListColumn(dsMB.Path.T.UserMessageTranslation.F.LanguageLCID),
							BTbl.ListColumn(dsMB.Path.T.UserMessageTranslation.F.Translation)
						),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.UserMessageTranslation.F.LanguageLCID, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UserMessageTranslation.F.Translation, DCol.Normal, ECol.Normal)
					),
					// Default is the Invariant LCID (127)
					Init.OnLoadNew(dsMB.Path.T.UserMessageTranslation.F.LanguageLCID, new ConstantValue(127))
				);
			});
			#endregion

			#region DefineTblEntries Block
			// Add code to add column info to tInfo.
			// TODO: BuildRegisteredImportTable is a bit of a crock. ANything we do there we could also do in the class static ctor. But we would still have to
			// reference the class to ensure the static ctor is executed. Also, because not all Tbls use delayed creation yet, we could get order dependencies.
			// To avoid this, all Tbl's that contain composite views (and thus reference other tbl's during their ctor) must be delayed-create (the *referenced*
			// Tbls need not be)
			// We could get rid of some of these classes by making TIGeneralMB3 a big partial class and replacing TIXxxxx.BuildRegisteredImportTable with DefineXxxxxTbls and put
			// each such method in its own (partial) class definition file.
			// There are on the other hand some tbl's that are defined in the static ctor and in fact *must* be defined there?? Perhaps the only reason is that these
			// are Tbl's saved in static members which are readonly and thus *must* be inited in the static ctor.
			TIReceive.DefineTblEntries();
			TIPurchaseOrder.DefineTblEntries();
			TIRequest.DefineTblEntries();
			TIMainBossService.DefineTblEntries();
			TISchedule.DefineTblEntries();
			TISecurity.DefineTblEntries();
			TIUnit.DefineTblEntries();
			TIItem.DefineTblEntries();
			TIWorkOrder.DefineTblEntries();
			#endregion

			#region AccessCode
			DefineTbl(dsMB.Schema.T.AccessCode, delegate () {
				return new Tbl(dsMB.Schema.T.AccessCode, TId.AccessCode,
				new Tbl.IAttr[] {
					AccessCodeGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.AccessCode.F.Code), BTbl.ListColumn(dsMB.Path.T.AccessCode.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.AccessCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AccessCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AccessCode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Request, TId.AccessCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Request.F.AccessCodeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.AccessCode,
						TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID.F.UnitID.F.AccessCodeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.AccessCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.AccessCodeID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Task, TId.AccessCode,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderTemplate.F.AccessCodeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.AccessCode, dsMB.Schema.T.AccessCode);
			#endregion

			#region AttentionStatus - Top level node over the entire database
			AssignmentStatusTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.AttentionStatus, TId.MyAssignmentOverview,
				new Tbl.IAttr[] {
					AssignmentsGroup,
					new BTbl(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AttentionStatus.F.UserID).Eq(new SqlExpression(new UserIDSource()))))
				},
				new TblLayoutNodeArray(
					TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(RequestsAssignmentsGroup), DCol.Normal }, new Key[] { StateContext.NewCode, StateContext.InProgressCode },
							TblRowNode.New(KB.TOc(TId.Request), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.AttentionStatus.F.NumNewRequests, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.AttentionStatus.F.NumInProgressRequests, DCol.Normal)
							)
						),
						TblGroupNode.New(KB.K("Number of In Progress Requests by In Progress Date"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.NewBrowsette(new DelayedCreateTbl(
								new Tbl(dsMB.Schema.T.AssignedActiveRequestAgeHistogram, TId.InProgressRequestInProgressDateHistogram,
									new Tbl.IAttr[] {
										RequestsAssignmentsGroup,
										new BTbl(
											BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AssignedActiveRequestAgeHistogram.F.RequestAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))),
											BTbl.SetCustomListCreator(
												delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
													var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ShowColumnNames|UIListStyles.ChartStackedColumn);
													result.SetAxisXCrossing(0, DateTime.Today.ToOADate());	// TODO: Hook to update this in Refresh
													return result;
												}
											),
											BTbl.ListColumn(dsMB.Path.T.AssignedActiveRequestAgeHistogram.F.RequestOpenDate),
											BTbl.ListColumn(dsMB.Path.T.AssignedActiveRequestAgeHistogram.F.UnconvertedCount),
											BTbl.ListColumn(dsMB.Path.T.AssignedActiveRequestAgeHistogram.F.ConvertedSomeIncompleteCount),
											BTbl.ListColumn(dsMB.Path.T.AssignedActiveRequestAgeHistogram.F.ConvertedAllCompleteCount)
										)
									},
									new TblLayoutNodeArray()
								)), DCol.Normal)
						),
					TblGroupNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblGroupNode.New(KB.TOc(TId.InProgressRequestByPriority), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.NewBrowsette(new DelayedCreateTbl(
									new Tbl(dsMB.Schema.T.AssignedRequestCountsByPriority, TId.InProgressRequestByPriority,
										new Tbl.IAttr[] {
											RequestsAssignmentsGroup,
											new BTbl(
												BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AssignedRequestCountsByPriority.F.RequestAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))),
												BTbl.SetCustomListCreator(
													delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
														var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
														return result;
													}
												),
												BTbl.ListColumn(dsMB.Path.T.AssignedRequestCountsByPriority.F.RequestPriorityID.F.Code),
												BTbl.ListColumn(dsMB.Path.T.AssignedRequestCountsByPriority.F.RequestCount)
											)
										},
										new TblLayoutNodeArray()
									)), DCol.Normal)
							)
						),
						TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblGroupNode.New(KB.TOc(TId.InProgressRequestByStatus), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.NewBrowsette(new DelayedCreateTbl(
									new Tbl(dsMB.Schema.T.AssignedRequestCountsByStatus, TId.InProgressRequestByStatus,
										new Tbl.IAttr[] {
											RequestsAssignmentsGroup,
											new BTbl(
												BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AssignedRequestCountsByStatus.F.RequestAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))),
												BTbl.SetCustomListCreator(
													delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
														var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
														return result;
													}
												),
												BTbl.ListColumn(dsMB.Path.T.AssignedRequestCountsByStatus.F.RequestStateHistoryStatusID.F.Code),
												BTbl.ListColumn(dsMB.Path.T.AssignedRequestCountsByStatus.F.RequestCount)
											)
										},
										new TblLayoutNodeArray()
									)), DCol.Normal)
							)
						)
					),
						TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(WorkOrdersAssignmentsGroup), DCol.Normal }, new Key[] { StateContext.DraftCode, StateContext.OpenCode },
							TblRowNode.New(KB.TOc(TId.WorkOrder), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.AttentionStatus.F.NumNewWorkOrders, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.AttentionStatus.F.NumInProgressWorkOrders, DCol.Normal)
							)
						),
						WorkOrdersByEndDateHistogramGroup(WorkOrdersAssignmentsGroup, dsMB.Schema.T.AssignedWorkOrderEndDateHistogram,
							dsMB.Path.T.AssignedWorkOrderEndDateHistogram.F.WorkEndDate,
							dsMB.Path.T.AssignedWorkOrderEndDateHistogram.F.PMWorkOrderCount,
							dsMB.Path.T.AssignedWorkOrderEndDateHistogram.F.CMWorkOrderCount,
							dsMB.Path.T.AssignedWorkOrderEndDateHistogram.F.RequestedPMWorkOrderCount,
							dsMB.Path.T.AssignedWorkOrderEndDateHistogram.F.RequestedCMWorkOrderCount,
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AssignedWorkOrderEndDateHistogram.F.WorkOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource())))
						),
						TblGroupNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblGroupNode.New(KB.TOc(TId.OpenWorkOrderbyPriority), new TblLayoutNode.ICtorArg[] { DCol.Normal },
									TblColumnNode.NewBrowsette(new DelayedCreateTbl(
										new Tbl(dsMB.Schema.T.AssignedWorkOrderCountsByPriority, TId.OpenWorkOrderbyPriority,
											new Tbl.IAttr[] {
												WorkOrdersAssignmentsGroup,
												new BTbl(
													BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AssignedWorkOrderCountsByPriority.F.WorkOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))),
													BTbl.SetCustomListCreator(
														delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
															var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
															return result;
														}
													),
													BTbl.ListColumn(dsMB.Path.T.AssignedWorkOrderCountsByPriority.F.WorkOrderPriorityID.F.Code),
													BTbl.ListColumn(dsMB.Path.T.AssignedWorkOrderCountsByPriority.F.WorkOrderCount)
												)
											},
											new TblLayoutNodeArray()
										)), DCol.Normal)
								)
							),
							TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblGroupNode.New(KB.TOc(TId.OpenWorkOrderbyStatus), new TblLayoutNode.ICtorArg[] { DCol.Normal },
									TblColumnNode.NewBrowsette(new DelayedCreateTbl(
										new Tbl(dsMB.Schema.T.AssignedWorkOrderCountsByStatus, TId.OpenWorkOrderbyStatus,
											new Tbl.IAttr[] {
												WorkOrdersAssignmentsGroup,
												new BTbl(
													BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.AssignedWorkOrderCountsByStatus.F.WorkOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))),
													BTbl.SetCustomListCreator(
														delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
															var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
															return result;
														}
													),
													BTbl.ListColumn(dsMB.Path.T.AssignedWorkOrderCountsByStatus.F.WorkOrderStateHistoryStatusID.F.Code),
													BTbl.ListColumn(dsMB.Path.T.AssignedWorkOrderCountsByStatus.F.WorkOrderCount)
												)
											},
											new TblLayoutNodeArray()
										)), DCol.Normal)
								)
							)
						),
						TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(PurchasingAssignmentsGroup), DCol.Normal }, new Key[] { StateContext.DraftCode, StateContext.IssuedCode },
							TblRowNode.New(KB.TOc(TId.PurchaseOrder), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.AttentionStatus.F.NumNewPurchaseOrders, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.AttentionStatus.F.NumInProgressPurchaseOrders, DCol.Normal)
							)
						)
					)
				));
			});
			#endregion

			#region VoidCode
			DefineTbl(dsMB.Schema.T.VoidCode, delegate () {
				return new Tbl(dsMB.Schema.T.VoidCode, TId.VoidCode,
				new Tbl.IAttr[] {
					WorkOrdersAndStoreroomGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.VoidCode.F.Code), BTbl.ListColumn(dsMB.Path.T.VoidCode.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.VoidCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.VoidCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.VoidCode.F.Comment, DCol.Normal, ECol.Normal))//,
																									// This browsette is one of the strongest requirements for making property browsettes. As it stands it claims to allow you to create a new voided
																									// physical count, but when you click on New you get New Physical Count (I went no further to see what would happen beyond that)
																									//BrowsetteTabNode.New(TId.VoidPhysicalCount, TId.VoidCode, 
																									//	TblColumnNode.NewBrowsette(dsMB.Path.T.ItemCountValue.F.VoidingItemCountValueVoidID.F.VoidCodeID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion

			#region Contact
			Tbl contactCreatorFromDirectoryServiceTbl = new Tbl(dsUserPrincipal.Schema.T.UserPrincipal, TId.UserPrincipalInformation,
				new Tbl.IAttr[] {
					new UseNamedTableSchemaPermissionTbl(dsMB.Schema.T.Contact),
					xyzzy.ContactsDependentGroup,
					new BTbl(
						BTbl.LogicClass(typeof(ContactFromDirectoryServiceBrowseLogic)),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.Name),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.DisplayName),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.BusPhone),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.EmailAddress)
					),
					new ETbl(ETbl.EditorDefaultAccess(false)),
					new CustomSessionTbl(delegate(XAFClient existingDatabaseAccess, DBI_Database schema, out bool callerHasCustody) {
						callerHasCustody = true;
						return new XAFClient(existingDatabaseAccess.ConnectionInfo, new UserFromDirectoryServiceSession(existingDatabaseAccess));
					})
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.Name, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.DisplayName, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.BusPhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.EmailAddress, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.HomePhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.PagerPhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.MobilePhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.FaxPhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.WebURL, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.PreferredLanguage, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.LDAPPath, DCol.Normal)
				)
			);
			DefineTbl(dsMB.Schema.T.Contact, delegate () {
				return new Tbl(dsMB.Schema.T.Contact, TId.Contact,
				new Tbl.IAttr[] {
						ContactsDependentGroup,
						// Note that there was up until Oct 24/08 a ContactBrowseControl which contained #if'ed-off inoperational code to import from Outlook Contacts
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Contact.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Contact.F.BusinessPhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
							BTbl.ListColumn(dsMB.Path.T.Contact.F.MobilePhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
							BTbl.AdditionalVerb(KB.K("Create from Active Directory"),
								delegate(BrowseLogic browserLogic) {
									return new CallDelegateCommand(
										delegate() {
											BrowseForm.NewBrowseForm(browserLogic.CommonUI.UIFactory, browserLogic.DB, contactCreatorFromDirectoryServiceTbl).ShowForm();
										}
									);
								}
							)
						),
						new ETbl(
							ETbl.CustomCommand(delegate(EditLogic editorLogic) {
								if (editorLogic.WillBeEditingDefaults)
									return null;
								Source CommentSource = editorLogic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.Comment, 0);
								var userDirectory = new EditLogic.CommandDeclaration(KB.K("Initialize all from Active Directory"), new FillContactFromUserDirectoryCommand(editorLogic, false));
								var useActiveDirectoryGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								useActiveDirectoryGroup.Add(userDirectory);
								return useActiveDirectoryGroup;
							}),
							ETbl.CustomCommand(delegate(EditLogic editorLogic) {
								if (editorLogic.WillBeEditingDefaults)
									return null;
								var userDirectory = new EditLogic.CommandDeclaration(KB.K("Update all from Active Directory"), new FillContactFromUserDirectoryCommand(editorLogic, true));
								var useActiveDirectoryGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								useActiveDirectoryGroup.Add(userDirectory);
								return useActiveDirectoryGroup;
							})
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.ContactReport; }))
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.Contact.F.Code, DCol.Normal, new ECol(Fmt.SetId(ContactNameId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.BusinessPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.Email, DCol.Normal, new ECol(Fmt.SetId(EmailAddressId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.AlternateEmail, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.HomePhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.PagerPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.MobilePhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.FaxPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.WebURL, DCol.Normal, new ECol(Fmt.SetId(WebUrlAddressId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LocationID.F.Desc, DCol.Normal),
						// TODO: In the following we only really need the version check for the ECol since the edit buffer fetches the entire record and the schema must match even for fields we don't use,
						// but MinimumDBVersionTbl is not an ECol.ICtorArg
						TblColumnNode.New(dsMB.Path.T.Contact.F.Id.L.User.ContactID.F.AuthenticationCredential, ECol.AllReadonly, new MinimumDBVersionTbl(new Version(1, 1, 4, 2)), DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.PreferredLanguage, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LDAPGuid, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.Contact.F.Comment, DCol.Normal, ECol.Normal)
					),
					// NOTE: Currently the Requestors tab appears blank because the only information that the Requestor
					// Tbl entry displays comes from the Contact record, and these fields get removed since this is
					// a browsette on the Contact table.
					BrowsetteTabNode.New(TId.ContactFunction, TId.Contact,
						TblColumnNode.NewBrowsette(TIGeneralMB3.ContactFunctionsBrowsetteTblCreator, dsMB.Path.T.ContactFunctions.F.ParentContactID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ContactRelation, TId.Contact,
						TblColumnNode.NewBrowsette(TIRelationship.ContactRelatedRecordsBrowseTbl, dsMB.Path.T.ContactRelatedRecords.F.ThisContactID, DCol.Normal, ECol.Normal))
				), EmailAddressValidator, WebUrlAddressValidator
				);
			});
			RegisterExistingForImportExport(TId.Contact, dsMB.Schema.T.Contact);
			#endregion
			#region CostCenter
			DefineTbl(dsMB.Schema.T.CostCenter, delegate () {
				return new Tbl(dsMB.Schema.T.CostCenter, TId.CostCenter,
				new Tbl.IAttr[] {
					AccountingGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.CostCenter.F.Code), BTbl.ListColumn(dsMB.Path.T.CostCenter.F.Desc)),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.CostCenter.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.CostCenter.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.CostCenter.F.GeneralLedgerAccount, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.CostCenter.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.BillableRequestor, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.BillableRequestor.F.AccountsReceivableCostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ChargebackCategory, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ChargebackLineCategory.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ItemAdjustmentCode, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ItemAdjustmentCode.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ItemIssueCode, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.ItemIssueCode.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.StorageAssignment, TId.CostCenter,
						TblColumnNode.NewBrowsette(TILocations.ActiveActualItemLocationBrowseTblCreator, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.MiscellaneousItem, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Miscellaneous.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Labor, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.LaborInside.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ExpenseModel, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderExpenseModel.F.NonStockItemHoldingCostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ExpenseMapping, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrderExpenseModelEntry.F.CostCenterID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Vendor, TId.CostCenter,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Vendor.F.AccountsPayableCostCenterID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.CostCenter, dsMB.Schema.T.CostCenter);
			#endregion
			#region BackupFileName - record where backups have been done
			DelayedCreateTbl editorTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.BackupFileName, TId.Backup,
					new Tbl.IAttr[] {
						AdminGroup,
						new ETbl(ETbl.EditorAccess(false), ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View, EdtMode.Delete), new MinimumDBVersionTbl(new Version(1, 0, 10, 24)))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.BackupFileName.F.LastBackupDate, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.BackupFileName.F.DatabaseVersion, new MinimumDBVersionTbl(new Version(1, 0, 10, 24)), DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.BackupFileName.F.FileName, DCol.Normal, ECol.ReadonlyInUpdate),
						TblGroupNode.New(KB.K("Default Backup Location"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblUnboundControlNode.New(KB.K("Computer"), new StringTypeInfo(0, 256, 1, true, true, true),
								new DCol(Fmt.SetInitialValue(
									delegate (CommonLogic bl) {
										var session = (Libraries.DBILibrary.MSSql.SqlClient)bl.DB.Session;
										try {
											return session.DBServerComputer();
										}
										catch (GeneralException e) {
											return Strings.Format(KB.K("Unavailable: {0}"), e.Message);
										}
										catch (System.Exception) {
											return Strings.Format(Application.InstanceMessageCultureInfo, KB.K("Unavailable: {0}"), KB.K("No permissions"));
										}
									}
							))),
							TblUnboundControlNode.New(KB.K("Directory"), new StringTypeInfo(0, 256, 1, true, true, true),
								new DCol(Fmt.SetInitialValue(
									delegate (CommonLogic bl) {
										var session = (Libraries.DBILibrary.MSSql.SqlClient)bl.DB.Session;
										try {
											return session.DBDefaultBackupDirectory();
										}
										catch (GeneralException e) {
											return Strings.Format(KB.K("Unavailable: {0}"), e.Message);
										}
										catch (System.Exception) {
											return Strings.Format(Application.InstanceMessageCultureInfo, KB.K("Unavailable: {0}"), KB.K("No permissions"));
										}
									}
							))),
							TblUnboundControlNode.New(KB.K("Extension"), new StringTypeInfo(0, 256, 1, true, true, true),
								new DCol(Fmt.SetInitialValue(
									delegate (CommonLogic bl) {
										return KB.I("BAK");
									}
							)))
						),
						TblColumnNode.New(dsMB.Path.T.BackupFileName.F.Comment, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.BackupFileName.F.Message, new MinimumDBVersionTbl(new Version(1, 0, 10, 24)), DCol.Normal, ECol.AllReadonly)
					)
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.BackupFileName, new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.BackupFileName, TId.Backup,
					new Tbl.IAttr[] {
						AdminGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.BackupFileName.F.FileName),
							BTbl.ListColumn(dsMB.Path.T.BackupFileName.F.LastBackupDate, BTbl.ListColumnArg.Contexts.SortInitialAscending),
							BTbl.ListColumn(dsMB.Path.T.BackupFileName.F.DatabaseVersion, new MinimumDBVersionTbl(new Version(1, 0, 10, 24))),
							BTbl.ListColumn(dsMB.Path.T.BackupFileName.F.Message, new MinimumDBVersionTbl(new Version(1, 0, 10, 24)))
						),
						TIReports.NewRemotePTbl(TIReports.BackupFileReport)
					},
					null,
					new CompositeView(editorTblCreator, dsMB.Path.T.BackupFileName.F.Id,
						CompositeView.AdditionalVerb(KB.K("Backup"),
								delegate (BrowseLogic browserLogic, int viewIndex) {
									Source pathSource = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.BackupFileName.F.FileName, -1);
									return new CallDelegateCommand(KB.K("Backup the organization data to a file"),
										delegate () {
											try {
												using (Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().VersionHandler.GetUnfetteredDatabaseAccess(browserLogic.DB)) {
													((MB3Client)browserLogic.DB).BackupDatabase((string)pathSource.GetValue());
												}
											}
											catch (System.Exception e) {
												Thinkage.Libraries.Application.Instance.DisplayError(e);
											}
											// Although BackupDatabase ultimately uses DBClient to back up the database, it does so using a subset schema. Since the
											// schema is not object-identical to dsMB, we don't get cache propagation of the change, so we need an explicit refresh here.
											// Note that the last backup on the main status page will not see the change either (even if we refresh here) nor will any other Backup browsers in other main windows.
											// It would be nice if we could just synthesize the cache broadcast here, or if (since you can only backup a current-version db???)
											// MB3Client.BackupDatabase did the updating itself using the proper schema.
											browserLogic.SetAllOutOfDate();
										});
								}))
				);
			}));
			#endregion
			#region DatabaseStatus - Top level node over the entire database
			DefineTbl(dsMB.Schema.T.DatabaseStatus, delegate () {
				return new Tbl(dsMB.Schema.T.DatabaseStatus, TId.MainBossOverview,
				new Tbl.IAttr[] {
					new BTbl()
				},
				new TblLayoutNodeArray(
					TblGroupNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.LastBackupDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.LastBackupFileName, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumEmailNeedingManualProcessing, DCol.Normal, new FeatureGroupArg(MainBossServiceGroup), Fmt.SetColor(System.Drawing.Color.Red))

					),
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(RequestsGroup), DCol.Normal }, new Key[] { StateContext.NewCode, StateContext.InProgressCode },
						TblRowNode.New(KB.TOc(TId.Request), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumNewRequests, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumInProgressRequests, DCol.Normal)
						),
						TblRowNode.New(KB.K("Unassigned"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumUnAssignedNewRequests, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumUnAssignedInProgressRequests, DCol.Normal)
						)
					),
					TblGroupNode.New(KB.K("Number of In Progress Requests by In Progress Date"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblColumnNode.NewBrowsette(new DelayedCreateTbl(
							new Tbl(dsMB.Schema.T.ActiveRequestAgeHistogram, TId.InProgressRequestInProgressDateHistogram,
								new Tbl.IAttr[] {
									RequestsGroup,
									new BTbl(
										BTbl.SetCustomListCreator(
											delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
												var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ShowColumnNames|UIListStyles.ChartStackedColumn);
												result.SetAxisXCrossing(0, DateTime.Today.ToOADate());	// TODO: Hook to update this in Refresh
												return result;
											}
										),
										BTbl.ListColumn(dsMB.Path.T.ActiveRequestAgeHistogram.F.RequestOpenDate),
										BTbl.ListColumn(dsMB.Path.T.ActiveRequestAgeHistogram.F.UnconvertedCount),
										BTbl.ListColumn(dsMB.Path.T.ActiveRequestAgeHistogram.F.ConvertedSomeIncompleteCount),
										BTbl.ListColumn(dsMB.Path.T.ActiveRequestAgeHistogram.F.ConvertedAllCompleteCount)
									)
								},
								new TblLayoutNodeArray()
							)), DCol.Normal)
					),
					TblGroupNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblGroupNode.New(KB.TOc(TId.InProgressRequestByPriority), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.NewBrowsette(new DelayedCreateTbl(
									new Tbl(dsMB.Schema.T.RequestCountsByPriority, TId.InProgressRequestByPriority,
										new Tbl.IAttr[] {
											RequestsGroup,
											new BTbl(
												BTbl.SetCustomListCreator(
													delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
														var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
														return result;
													}
												),
												BTbl.ListColumn(dsMB.Path.T.RequestCountsByPriority.F.RequestPriorityID.F.Code),
												BTbl.ListColumn(dsMB.Path.T.RequestCountsByPriority.F.RequestCount)
											)
										},
										new TblLayoutNodeArray()
									)), DCol.Normal)
							)
						),
						TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblGroupNode.New(KB.TOc(TId.InProgressRequestByStatus), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.NewBrowsette(new DelayedCreateTbl(
									new Tbl(dsMB.Schema.T.RequestCountsByStatus, TId.InProgressRequestByStatus,
										new Tbl.IAttr[] {
											RequestsGroup,
											new BTbl(
												BTbl.SetCustomListCreator(
													delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
														var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
														return result;
													}
												),
												BTbl.ListColumn(dsMB.Path.T.RequestCountsByStatus.F.RequestStateHistoryStatusID.F.Code),
												BTbl.ListColumn(dsMB.Path.T.RequestCountsByStatus.F.RequestCount)
											)
										},
										new TblLayoutNodeArray()
									)), DCol.Normal)
							)
						)
					),
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(WorkOrdersGroup), DCol.Normal }, new Key[] { StateContext.DraftCode, StateContext.OpenCode, StateContext.OverdueCode },
						TblRowNode.New(KB.TOc(TId.WorkOrder), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumNewWorkOrders, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumOpenWorkOrders, DCol.Normal),
							TblQueryValueNode.New(KB.K("Number Overdue Work Orders"), new TblQueryExpression(
								SqlExpression.ScalarSubquery(new SelectSpecification( // open w/o whose Overdue value is not null
									null,
									new SqlExpression[] {
										SqlExpression.CountRows(null)
									},
									CommonExpressions.OverdueWorkOrderExpression,
								null))), DCol.Normal)
						),
						TblRowNode.New(KB.K("Unassigned"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumUnAssignedNewWorkOrders, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumUnAssignedOpenWorkOrders, DCol.Normal),
							TblLeafNode.Empty(DCol.Normal)
						)
					),
					WorkOrdersByEndDateHistogramGroup(WorkOrdersGroup, dsMB.Schema.T.WorkOrderEndDateHistogram,
						dsMB.Path.T.WorkOrderEndDateHistogram.F.WorkEndDate,
						dsMB.Path.T.WorkOrderEndDateHistogram.F.PMWorkOrderCount,
						dsMB.Path.T.WorkOrderEndDateHistogram.F.CMWorkOrderCount,
						dsMB.Path.T.WorkOrderEndDateHistogram.F.RequestedPMWorkOrderCount,
						dsMB.Path.T.WorkOrderEndDateHistogram.F.RequestedCMWorkOrderCount
					),
					TblGroupNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblGroupNode.New(KB.TOc(TId.OpenWorkOrderbyPriority), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.NewBrowsette(new DelayedCreateTbl(
									new Tbl(dsMB.Schema.T.WorkOrderCountsByPriority, TId.OpenWorkOrderbyPriority,
										new Tbl.IAttr[] {
											WorkOrdersGroup,
											new BTbl(
												BTbl.SetCustomListCreator(
													delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
														var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
														return result;
													}
												),
												BTbl.ListColumn(dsMB.Path.T.WorkOrderCountsByPriority.F.WorkOrderPriorityID.F.Code),
												BTbl.ListColumn(dsMB.Path.T.WorkOrderCountsByPriority.F.WorkOrderCount)
											)
										},
										new TblLayoutNodeArray()
									)), DCol.Normal)
							)
						),
						TblSectionNode.New(new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblGroupNode.New(KB.TOc(TId.OpenWorkOrderbyStatus), new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.NewBrowsette(new DelayedCreateTbl(
									new Tbl(dsMB.Schema.T.WorkOrderCountsByStatus, TId.OpenWorkOrderbyStatus,
										new Tbl.IAttr[] {
											WorkOrdersGroup,
											new BTbl(
												BTbl.SetCustomListCreator(
													delegate(BrowseLogic browseLogic, TypeInfo rowIdType) {
														var result = browseLogic.CommonUI.UIFactory.CreateChart(rowIdType, null, UIListStyles.ChartStackedBar);
														return result;
													}
												),
												BTbl.ListColumn(dsMB.Path.T.WorkOrderCountsByStatus.F.WorkOrderStateHistoryStatusID.F.Code),
												BTbl.ListColumn(dsMB.Path.T.WorkOrderCountsByStatus.F.WorkOrderCount)
											)
										},
										new TblLayoutNodeArray()
									)), DCol.Normal)
							)
						)
					),
					TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(PurchasingGroup), DCol.Normal }, new Key[] { StateContext.DraftCode, StateContext.IssuedCode },
						TblRowNode.New(KB.TOc(TId.PurchaseOrder), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumDraftPurchaseOrders, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumIssuedPurchaseOrders, DCol.Normal)
						),
						TblRowNode.New(KB.K("Unassigned"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumUnAssignedDraftPurchaseOrders, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.DatabaseStatus.F.NumUnAssignedIssuedPurchaseOrders, DCol.Normal)
						)
					)
				));
			});
			#endregion

			#region Administration
			AdministrationTblCreator = new DelayedCreateTbl(delegate () {
				// TODO: This really should be something the License browser would tell you.
				// Maybe it could be 2-level tree structured with the root level being the union of all the ApplicationID's in found licenses and all the ApplicationID's in known licenses.
				// Below that would be all the existing licenses for the app id.
				// If you select the app-id row, the panel would show the effective licensing.
				List<TblLayoutNode> licensingNodes = new List<TblLayoutNode>();
				string moduleLicensedDisplayString = KB.K("Licensed").Translate();
				string notLicensedDisplayString = KB.K("Not Licensed").Translate();
				string demonstrationLicensedDisplayString = KB.K("Demonstration License Only").Translate();
				foreach (LicenseDefinition lDef in Thinkage.MainBoss.Database.Licensing.AllMainbossLicenses) {
					LicenseDefinition closureLDef = lDef;
					licensingNodes.Add(
						TblUnboundControlNode.New(KB.T(lDef.LicensingApplicationName),
							new StringTypeInfo(0, null, 0, false, false, false),
							new DCol(
								Fmt.SetInitialValue(
									delegate () {
										try {
											IApplicationWithSingleDatabaseConnection app = Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>();
											AnalyzedLicense bestLicense = LicenseManager.BestLicense(
															app.VersionHandler.GetLicenses(app.Session)
																.FindAll(l => l.ApplicationID == closureLDef.LicensingApplicationID)
																.ConvertAll(l => new AnalyzedLicense(l, null, new Database.Licensing.MBLicensedObjectSet(app.Session))));

											if (bestLicense == null)
												return KB.K("Not Licensed").Translate();
											else
												return LicenseAnalysis(bestLicense);
										}
										catch (System.Exception ex) {
											// Normally VerifyLicenses does not itself throw an exception but we catch this in case some lower level throws one.
											return Thinkage.Libraries.Exception.FullMessage(ex);
										}
									}
								)
							)
						)
					);
				}
				licensingNodes.Add(
					TblUnboundControlNode.New(KB.K("Deemed Release Date for license validation"),
						DateTimeTypeInfo.NullableOneDayEpsilon.IntersectCompatible(ObjectTypeInfo.NonNullUniverse),
						new DCol(Fmt.SetInitialValue(Thinkage.MainBoss.Database.Licensing.DeemedReleaseDate))
					)
				);
				licensingNodes.Add(
						TblUnboundControlNode.New(KB.K("For more information, see"), new StringTypeInfo(0, 100, 0, false, true, true),
						// TODO: To eliminate this Fmt.SetCreator we need a way to create a link display whose display text and link are not the same.
						// Some MS link-labels had the convention that the actual value would be (display text)#(link text) but this would require an
						// upgrade step or value-mappers for data-bound link labels. Perhaps there is a better way, or maybe we should just display the actual link
						// so people can tell where it will take them.
						new DCol(Fmt.SetUsage(DBI_Value.UsageType.ProtocolEncodedLink),
							Fmt.SetCreated(
								delegate (IBasicDataControl valueCtrl) {
									var link = (UILinkDisplay)valueCtrl;
									link.MakeUrl = (tf, value) => KB.I("http://mainboss.com/info/licenses.htm");
									link.VerticalAlignment = System.Drawing.StringAlignment.Far;
								}
							),
							Fmt.SetInitialValue(KB.I("MainBoss Licensing"))
						)));

				return new Tbl(dsMB.Schema.T.Session, TId.Administration,
				new Tbl.IAttr[] {
					AdminGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.Session.F.ClientName),
						BTbl.ListColumn(dsMB.Path.T.Session.F.ApplicationID),
						BTbl.ListColumn(dsMB.Path.T.Session.F.Creation)
					)
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.Session.F.ApplicationID, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Session.F.ClientName, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Session.F.Creation, DCol.Normal),
					TblGroupNode.New(KB.K("Modules"), new TblLayoutNode.ICtorArg[] { DCol.Normal }, licensingNodes.ToArray())
				));
			});
			#endregion
			#region Settings (User & Global)
			DelayedCreateTbl settingsUser = new DelayedCreateTbl(
				delegate () {
					return new Tbl(dsMB.Schema.T.SettingsAdministration, Libraries.Presentation.MSWindows.TId.SettingsGlobalUser,
						new Tbl.IAttr[] {
								AdminGroup
							},
						new TblLayoutNodeArray(
							TblColumnNode.New(null, dsMB.Path.T.SettingsAdministration.F.Id, DCol.Normal)
						)
					);
				}
			);
			DelayedCreateTbl settingsNamePanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.SettingsName, Libraries.Presentation.MSWindows.TId.Settings,
					new Tbl.IAttr[] {
						AdminGroup
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.SettingsName.F.Code, DCol.Normal)
					)
				);
			});
			DelayedCreateTbl settingsPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Settings, Libraries.Presentation.MSWindows.TId.Settings,
					new Tbl.IAttr[] {
						AdminGroup
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.Settings.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.Settings.F.Version, DCol.Normal)
					)
				);
			});
			SettingsTblCreator = new DelayedCreateTbl(delegate () {
				object userSettingID = KB.I("userSettingID");
				object settingID = KB.K("settingID");
				return new CompositeTbl(dsMB.Schema.T.SettingsAdministration, Libraries.Presentation.MSWindows.TId.Settings,
						new Tbl.IAttr[] {
							AdminGroup,
							new MinimumDBVersionTbl(new System.Version(1,0,10,58)),
							new BTbl(
								BTbl.PerViewListColumn(KB.K("User Setting"), userSettingID),
								BTbl.ListColumn(dsMB.Path.T.SettingsAdministration.F.Size),
								BTbl.ListColumn(dsMB.Path.T.SettingsAdministration.F.IsDefault)
							),
							new TreeStructuredTbl(dsMB.Path.T.SettingsAdministration.F.ParentID, 3)
						},
						null,
						new CompositeView(settingsUser, dsMB.Path.T.SettingsAdministration.F.Id, CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.SettingsAdministration.F.UserID).IsNull().And
														(new SqlExpression(dsMB.Path.T.SettingsAdministration.F.SettingsID).IsNull()).And
														(new SqlExpression(dsMB.Path.T.SettingsAdministration.F.SettingsNameID).IsNull())),
										ReadonlyView,
										BTbl.PerViewColumnValue(userSettingID, dsMB.Path.T.SettingsAdministration.F.Id)
						),
						new CompositeView(dsMB.Path.T.SettingsAdministration.F.UserID,
										CompositeView.RecognizeByValidEditLinkage(), ReadonlyView,
											BTbl.PerViewColumnValue(userSettingID, dsMB.Path.T.User.F.ContactID.F.Code)
						),
						new CompositeView(settingsNamePanelTbl, dsMB.Path.T.SettingsAdministration.F.SettingsNameID,
										CompositeView.RecognizeByValidEditLinkage(), ReadonlyView,
											BTbl.PerViewColumnValue(userSettingID, dsMB.Path.T.SettingsName.F.Code)
						),
						new CompositeView(settingsPanelTbl, dsMB.Path.T.SettingsAdministration.F.SettingsID,
										CompositeView.RecognizeByValidEditLinkage(), ReadonlyView,
											BTbl.PerViewColumnValue(userSettingID, dsMB.Path.T.Settings.F.Code)
						)
					);
			}
			);
			#endregion
			#region Database Management & record of what has administrative changes have been made to the database
			DelayedCreateTbl UpdateStatisticsTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.DatabaseHistory, TId.DatabaseManagement,
				new Tbl.IAttr[] {
					AdminGroup,
					new ETbl( ETbl.EditorDefaultAccess(false))
				},
				new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.DatabaseHistory.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.DatabaseHistory.F.Subject, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.DatabaseHistory.F.Description, DCol.Normal)
					)
				);
			});
			DatabaseManagementTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.DatabaseHistory, TId.DatabaseManagement,
				new Tbl.IAttr[] {
						AdminGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.DatabaseHistory.F.EntryDate, BTbl.ListColumnArg.Contexts.SortInitialDescending),
							BTbl.ListColumn(dsMB.Path.T.DatabaseHistory.F.Subject)
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.DatabaseHistoryReport; }))
					},
				null,
				CompositeView.ChangeEditTbl(UpdateStatisticsTbl,
					CompositeView.AdditionalVerb(KB.K("Update Database Statistics"),
							delegate (BrowseLogic browserLogic, int viewIndex) {
								var session = ((XAFClient)browserLogic.DB).Session;
								return new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Update the database statistics to improve performance"),
									delegate () {
										try {
											using (Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().VersionHandler.GetUnfetteredDatabaseAccess(browserLogic.DB)) {

												var sqlCommand = new Thinkage.Libraries.DBILibrary.MSSql.MSSqlLiteralCommandSpecification(KB.I("EXEC sp_updatestats"));
												System.Text.StringBuilder output = new System.Text.StringBuilder();
												session.ExecuteCommand(sqlCommand, output);
												var vh = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().VersionHandler;
												vh.LogHistory((DBClient)browserLogic.DB, KB.K("Database Management - Update Statistics").Translate(), output.ToString());
											}
										}
										catch (System.Exception e) {
											Thinkage.Libraries.Application.Instance.DisplayError(e);
										}
										browserLogic.SetAllOutOfDate();
									}),
									new SettableDisablerProperties(null, KB.K("Database server must be SQL Server 2008 or later"), session.EffectiveDBServerVersion >= new Version(10, 0, 0, 0)));
							}))
			);
			});
			DefineBrowseTbl(dsMB.Schema.T.DatabaseHistory, DatabaseManagementTblCreator);
			#endregion

			#region News Panel
			NewsPanelTblCreator = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(null, TId.MainBossNewsPanel,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("CompanyInformation"),
						AdminGroup,
						new BTbl(
							BTbl.SetDummyBrowserPanelControl(new TblLayoutNodeArray(
								TblInitSourceNode.New(null,
									new DualCalculatedInitValue(StringTypeInfo.Universe,
										delegate (object[] inputs) {
											var defaultURL = Strings.IFormat("http://mainboss.com/MainBossNews/{0}.{1}.{2}/index.htm?version={0}.{1}.{2}.{3}&language={4}", VersionInfo.ProductVersion.Major, VersionInfo.ProductVersion.Minor, VersionInfo.ProductVersion.Build, VersionInfo.ProductVersion.Revision, Thinkage.Libraries.Application.InstanceFormatCultureInfo.TwoLetterISOLanguageName);
											return new System.Uri((string)inputs[0] ?? defaultURL);
										},
										new VariableValue(dsMB.Schema.V.NewsURL)),
									new DCol(Fmt.SetUsage(DBI_Value.UsageType.Html), Fmt.SetHtmlDisplaySettings(new HtmlDisplaySettings() { ValueIsURI = true, SuppressScriptErrors = true })))
							))
						)
					},
					null
				);
			});

			#endregion
			#region Company Information - User site logos, contact info, etc.
			CompanyInformationTblCreator = new DelayedCreateTbl(delegate () {
				// TODO: Remove this kludge of using a dummy record; allow a "browser" to have a BrowseControlMode.NoRecords
				// with the following provisos: 1 - No ListColumn's allowed. 2 - The root record fetch is modified to generate an empty DataTable
				// 3 - The panel is altered to show data even with no selection
				// 4 - Need some way to get to an Edit control too, similar to EditDefaults, which controls if there is an Edit button
				// Typically such a browser would have Variable values displayed.
				object fontId = KB.I("xxxx ID");
				object faceNameID = KB.I("yyy id");
				object sizeId = KB.I("zzz id");
				object ffontId = KB.I("fxxxx ID");
				object ffaceNameID = KB.I("fyyy id");
				object fsizeId = KB.I("fzzz id");

				return new Tbl(dsMB.Schema.T.SingleRecordNoDataTable, TId.CompanyInformation,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("CompanyInformation"),
						AdminGroup,
						new BTbl(),
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.Edit, EdtMode.View))
					},
					new TblLayoutNodeArray(
						TblVariableNode.New(KB.K("Company Logo"), dsMB.Schema.V.CompanyLogo, DCol.Image, new ECol(Fmt.SetUsage(DBI_Value.UsageType.Image))),
						// TODO: DCol.Display does not work on variables.
						TblVariableNode.New(KB.K("Purchasing Contact"), dsMB.Schema.V.PurchaserContactID, new FeatureGroupArg(PurchasingGroup), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Contact.F.Code)), ECol.Normal),
						TblVariableNode.New(KB.K("Organization Name"), dsMB.Schema.V.OrganizationName, DCol.Normal, ECol.Normal),
						TblVariableNode.New(KB.K("MainBoss News URL"), dsMB.Schema.V.NewsURL, DCol.Normal, ECol.Normal),
						TblVariableNode.New(KB.K("Company Location"), dsMB.Schema.V.CompanyLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblGroupNode.New(KB.K("Reports"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblUnboundControlNode.New(KB.K("Main Font"), ObjectTypeInfo.NonNullUniverse, Fmt.SetId(fontId), ECol.Font(false)),
							TblVariableNode.New(KB.K("Font name"), dsMB.Schema.V.ReportFont, Fmt.SetId(faceNameID), ECol.Hidden),
							TblVariableNode.New(KB.K("Point size"), dsMB.Schema.V.ReportFontSize, Fmt.SetId(sizeId), ECol.Hidden),
							TblInitSourceNode.New(KB.K("Main Font"), new BrowserCalculatedInitValue(ObjectTypeInfo.NonNullUniverse, (values => values[0] == null || values[1] == null ? null : new System.Drawing.Font((string)values[0], (int)IntegralTypeInfo.AsNativeType(values[1], typeof(int)))), new VariableValue(dsMB.Schema.V.ReportFont), new VariableValue(dsMB.Schema.V.ReportFontSize)), DCol.Font()),
							TblUnboundControlNode.New(KB.K("Fixed-Pitch Font"), ObjectTypeInfo.NonNullUniverse, Fmt.SetId(ffontId), ECol.Font(true)),
							TblVariableNode.New(KB.K("Font name"), dsMB.Schema.V.ReportFontFixedWidth, Fmt.SetId(ffaceNameID), ECol.Hidden),
							TblVariableNode.New(KB.K("Point size"), dsMB.Schema.V.ReportFontSizeFixedWidth, Fmt.SetId(fsizeId), ECol.Hidden),
							TblInitSourceNode.New(KB.K("Fixed-width Font"), new BrowserCalculatedInitValue(ObjectTypeInfo.NonNullUniverse, (values => values[0] == null || values[1] == null ? null : new System.Drawing.Font((string)values[0], (int)IntegralTypeInfo.AsNativeType(values[1], typeof(int)))), new VariableValue(dsMB.Schema.V.ReportFontFixedWidth), new VariableValue(dsMB.Schema.V.ReportFontSizeFixedWidth)), DCol.Font()),
							TblVariableNode.New(KB.K("Bar Code Symbology"), dsMB.Schema.V.BarCodeSymbology,
								Fmt.SetEnumText(new EnumValueTextRepresentations(BarCodeBase.SymbologyTypeInfo, BarCodeBase.BarCodeNameContext, 8)),
								DCol.Normal, ECol.Normal)
						),
						TblGroupNode.New(KB.K("Filters"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblVariableNode.New(KB.K("Active filter shows only records less than this many days old"), dsMB.Schema.V.ActiveFilterInterval, DCol.Normal, ECol.Normal),
							TblVariableNode.New(KB.K("Active filter shows only records updated since this date"), dsMB.Schema.V.ActiveFilterSinceDate, DCol.Normal, ECol.Normal)
						)
					),
					Init.OnLoad(new ControlTarget(fontId), new EditorCalculatedInitValue(ObjectTypeInfo.Universe, (object[] values) => new System.Drawing.Font((string)values[0], (int)IntegralTypeInfo.AsNativeType(values[1], typeof(int))), new VariableValue(dsMB.Schema.V.ReportFont), new VariableValue(dsMB.Schema.V.ReportFontSize))),
					new Check2<System.Drawing.Font, int>(
							delegate (System.Drawing.Font f, int ps) {
								if ((int)Math.Round(f.SizeInPoints) != ps)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(1, new GeneralException(KB.K("Size does not match font's size")));
								return null;
							}
						)
						.Operand1(fontId)
						.Operand2(sizeId, delegate (System.Drawing.Font f) {
							return (int)Math.Round(f.SizeInPoints);
						}),
					new Check2<System.Drawing.Font, string>(
							delegate (System.Drawing.Font f, string n) {
								if (f.Name != n)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(1, new GeneralException(KB.K("Name does not match font's name")));
								return null;
							}
						)
						.Operand1(fontId)
						.Operand2(faceNameID, delegate (System.Drawing.Font f) {
							return f.Name;
						}),
					Init.OnLoad(new ControlTarget(ffontId), new EditorCalculatedInitValue(ObjectTypeInfo.Universe, (object[] values) => new System.Drawing.Font((string)values[0], (int)IntegralTypeInfo.AsNativeType(values[1], typeof(int))), new VariableValue(dsMB.Schema.V.ReportFontFixedWidth), new VariableValue(dsMB.Schema.V.ReportFontSizeFixedWidth))),
					new Check2<System.Drawing.Font, int>(
							delegate (System.Drawing.Font f, int ps) {
								if ((int)Math.Round(f.SizeInPoints) != ps)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(1, new GeneralException(KB.K("Size does not match font's size")));
								return null;
							}
						)
						.Operand1(ffontId)
						.Operand2(fsizeId, delegate (System.Drawing.Font f) {
							return (int)Math.Round(f.SizeInPoints);
						}),
					new Check2<System.Drawing.Font, string>(
							delegate (System.Drawing.Font f, string n) {
								if (f.Name != n)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(1, new GeneralException(KB.K("Name does not match font's name")));
								return null;
							}
						)
						.Operand1(ffontId)
						.Operand2(ffaceNameID, delegate (System.Drawing.Font f) {
							return f.Name;
						}),
					new Check1<System.Drawing.Font>(
							delegate (System.Drawing.Font f) {
								// Ensure we can make a Bold form that is distinct (for captions)
								if (f.Bold)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Font cannot have Bold style")));
								using (var fb = new System.Drawing.Font(f, f.Style | System.Drawing.FontStyle.Bold)) {   // This might throw an exception
									if (!fb.Bold)
										return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Font does not have a Bold style")));
								}
								// Ensure we can make a Underlined form that is distinct (for column headers)
								if (f.Underline)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Font cannot have Underlined style")));
								using (var fb = new System.Drawing.Font(f, f.Style | System.Drawing.FontStyle.Underline)) {  // This might throw an exception
									if (!fb.Underline)
										return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Font does not have an Underlined style")));
								}
								// Ensure we can make a Strikeout form that is distinct (for data from Hidden records)
								if (f.Strikeout)
									return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Font cannot have Strikeout style")));
								using (var fb = new System.Drawing.Font(f, f.Style | System.Drawing.FontStyle.Strikeout)) {  // This might throw an exception
									if (!fb.Strikeout)
										return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Font does not have a Strikeout style")));
								}
								return null;
							}
						)
						.Operand1(fontId),
					new Check1<System.Drawing.Font>(
							delegate (System.Drawing.Font f) {
								// TODO: Ensure the font is fixed-pitch. How? The web indicates that the LF_FIXED bit in the LOGFONT is no longer set. After all, the only characteristic of
								// a fixed-pitch font is that all the letters are the same width. Some examples suggest measuring a string of 'i's and 'w's and comparing the sizes. Yuck.
								// The system seems to know it is fixed-pitch because that's all the user is offered, so for now we will trust that the font picker knows what it's doing.
								return null;
							}
						)
						.Operand1(ffontId)
					);
			});
			#endregion
			#region License
			object keyId = KB.I("keyId");
			object licenseId = KB.I("licenseId");
			object applicationIDId = KB.I("applicationIDId");
			object licenseModelId = KB.I("licenseModelId");
			object licenseCountId = KB.I("licenseCountId");
			object licenseCountStatusId = KB.I("licenseCountStatusId");
			object expiryModelId = KB.I("expiryModelId");
			object expiryDateId = KB.I("expiryDateId");
			object expiryStatusId = KB.I("expiryStatusId");
			object licenseIDId = KB.I("licenseIDId");
			object licenseIDStatusId = KB.I("licenseIDStatusId");
			DefineTbl(dsMB.Schema.T.License, delegate () {
				// A browser to extract potential new licenses from text and display them in a list, correlating them to existing licenses, and optionally installing them.
				Tbl licenseUpdaterTbl = new Tbl(dsLicense_1_1_4_2.Schema.T.License, TId.License,
					new Tbl.IAttr[] {
						xyzzy.LicenseGroup,
						new BTbl(
							BTbl.LogicClass(typeof(LicenseUpdaterBrowseLogic)),
							BTbl.ListColumn(dsLicense_1_1_4_2.Path.T.License.F.ApplicationID, Fmt.SetEnumText(Licensing.LicenseModuleIdProvider), Fmt.SetDynamicSizing()),
							BTbl.ListColumn(dsLicense_1_1_4_2.Path.T.License.F.LicenseID),
							BTbl.ListColumn(dsLicense_1_1_4_2.Path.T.License.F.License, Fmt.SetMonospace(true))),
//						new ImageTbl(Resources.Images.LicenseKey, KB.TOi(TId.License)),
						new ETbl(ETbl.EditorDefaultAccess(false)),
						new CustomSessionTbl(delegate(XAFClient existingDatabaseAccess, DBI_Database schema, out bool callerHasCustody) {
							callerHasCustody = true;
							var contents = (string)Thinkage.Libraries.Application.Instance.GetInterface<UIFactory>().CreateClipboard<string>().Value;
							return new XAFClient(existingDatabaseAccess.ConnectionInfo,
								new LicenseUpdateSession(existingDatabaseAccess, contents != null ? contents : ""));
						})
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsLicense_1_1_4_2.Path.T.License.F.License, DCol.Normal, Fmt.SetMonospace(true)),
						TblColumnNode.New(KB.K("License Module"), dsLicense_1_1_4_2.Path.T.License.F.ApplicationID, DCol.Normal, Fmt.SetEnumText(Licensing.LicenseModuleIdProvider), Fmt.SetDynamicSizing()),
						TblColumnNode.New(KB.K("License Module ID"), dsLicense_1_1_4_2.Path.T.License.F.ApplicationID, DCol.Normal),
						TblColumnNode.New(dsLicense_1_1_4_2.Path.T.License.F.LicenseCount, DCol.Normal),
						TblColumnNode.New(dsLicense_1_1_4_2.Path.T.License.F.ExpiryModel, DCol.Normal, Fmt.SetEnumText(License.ExpiryModelNameProvider)),
						TblColumnNode.New(dsLicense_1_1_4_2.Path.T.License.F.Expiry, DCol.Normal),
						TblColumnNode.New(dsLicense_1_1_4_2.Path.T.License.F.LicenseModel, DCol.Normal, Fmt.SetEnumText(License.LicenseModelNameProvider)),
						TblColumnNode.New(dsLicense_1_1_4_2.Path.T.License.F.LicenseID, DCol.Normal),
						TblGroupNode.New(KB.K("Licenses for the same Application already entered in this database"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
							TblColumnNode.NewBrowsette(FindDelayedBrowseTbl(dsMB.Schema.T.License), dsLicense_1_1_4_2.Path.T.License.F.ApplicationID, dsMB.Path.T.License.F.ApplicationID,
								DCol.Normal,
								new CustomSessionTbl(
									delegate (XAFClient existingDatabaseAccess, DBI_Database schema, out bool callerHasCustody) {
										callerHasCustody = false;
										return ((LicenseUpdateSession)existingDatabaseAccess.Session).ExistingDB;
									}
								)
							)
						)
					)
				);
				var exceptionFullMessageTypeInfo = new StringTypeInfo(1, 1024, 2, true, false, false);  // TODO: This is probably not correct
				return new Tbl(dsMB.Schema.T.License, TId.License,
					new Tbl.IAttr[] {
						LicenseGroup,
						new BTbl(
							BTbl.ListColumn(KB.K("License Module"), dsMB.Path.T.License.F.ApplicationID, Fmt.SetEnumText(Licensing.LicenseModuleIdProvider), Fmt.SetDynamicSizing()),
							BTbl.ListColumn(dsMB.Path.T.License.F.Expiry),
							BTbl.ListColumn(dsMB.Path.T.License.F.License, Fmt.SetMonospace(true)),
							BTbl.AdditionalVerb(KB.K("Update Licenses"),
								delegate(BrowseLogic browserLogic) {
									return new CallDelegateCommand(
										delegate() {
											BrowseForm.NewBrowseForm(browserLogic.CommonUI.UIFactory, browserLogic.DB, licenseUpdaterTbl).ShowForm();
										}
									);
								}
							)
						),
//						new ImageTbl(Resources.Images.LicenseKey, KB.TOi(TId.License)),
						new ETbl(ETbl.EditorAccess(false, EdtMode.Clone, EdtMode.Edit, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault)),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.LicenseReport; }))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(KB.K("License Module"), dsMB.Path.T.License.F.ApplicationID, DCol.Normal, ECol.AllReadonly, Fmt.SetEnumText(Licensing.LicenseModuleIdProvider), Fmt.SetDynamicSizing()),
						TblColumnNode.New(dsMB.Path.T.License.F.License, DCol.Normal, new ECol(Fmt.SetId(keyId)), Fmt.SetMonospace(true)),
						TblUnboundControlNode.StoredEditorValue(licenseId, ObjectTypeInfo.Universe),
						TblColumnNode.New(KB.T("License Module ID"), dsMB.Path.T.License.F.ApplicationID, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(applicationIDId))),
						TblColumnNode.New(dsMB.Path.T.License.F.LicenseModel, DCol.Normal, ECol.AllReadonly, Fmt.SetId(licenseModelId), Fmt.SetEnumText(License.LicenseModelNameProvider)),
						TblColumnNode.New(dsMB.Path.T.License.F.LicenseCount, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(licenseCountId))),
						TblInitSourceNode.New(KB.K("License Count Status"), new DualCalculatedInitValue(exceptionFullMessageTypeInfo, delegate (object[] inputs) {
							string key = (string)inputs[0];
							if (key == null)
								return null;
							var l = new Libraries.Licensing.License(key);
							var al = new Libraries.Licensing.AnalyzedLicense(l, (int)l.LicenseID, new Database.Licensing.MBLicensedObjectSet(Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session));
							return al == null ? null : LicenseAnalysis(al);
						}, new DualPathValue(dsMB.Path.T.License.F.License)), DCol.Normal, ECol.AllReadonly, Fmt.SetId(licenseCountStatusId)),
						TblColumnNode.New(dsMB.Path.T.License.F.ExpiryModel, DCol.Normal, ECol.AllReadonly, Fmt.SetId(expiryModelId), Fmt.SetEnumText(License.ExpiryModelNameProvider)),
						TblColumnNode.New(dsMB.Path.T.License.F.Expiry, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(expiryDateId))),
						TblUnboundControlNode.New(KB.K("Expiry Status"), exceptionFullMessageTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(expiryStatusId))),
						TblColumnNode.New(dsMB.Path.T.License.F.LicenseID, DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(licenseIDId))),
						TblUnboundControlNode.New(KB.K("License ID Status"), exceptionFullMessageTypeInfo, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(licenseIDStatusId)))
					),
					// All these check operations declare the license operand as type object because ObjectTypeInfo objects to calling AsNativeType to any other type; whether this
					// is correct is debatable.
					// The first check converts the key into a license if possible, but just quietly returns null if this fails. Thus the hidden value always reflects the current
					// key. The second check again tries to create a License, this time capturing the exception if it fails and returning it as a Check failure. Because of the
					// order these are coded in, the red-flagging caused by the second Check does not prevent the first Check from always operating, and so all the break-out value controls are not red-flagged.
					new Check2<string, object>()
						.Operand1(keyId)
						.Operand2(licenseId,
							delegate (string key) {
								try {
									// TODO: We need some way here to get the Session from the calling editor, but there is no way of doing this.
									// For now, as there is no path to this editor that substitutes a Session object, we pry it out of the Application instance.
									var l = new Libraries.Licensing.License(key);
									return new Libraries.Licensing.AnalyzedLicense(l, (int)l.LicenseID, new Database.Licensing.MBLicensedObjectSet(Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session));
								}
								catch {
									return null;
								}
							}
						),
					new Check1<string>(
						delegate (string key) {
							try {
								var l = new Libraries.Licensing.License(key);
								var a = new Libraries.Licensing.AnalyzedLicense(l, (int)l.LicenseID, new Database.Licensing.MBLicensedObjectSet(Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session));
								if (a.Definition.LicenseIsDeprecated)
									throw a.LicenseIdMessage;
							}
							catch (System.Exception ex) {
								return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(ex);
							}
							return null;
						},
						TblActionNode.Activity.Disabled,
						TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone)
						)
						.Operand1(keyId),
					// The remaining Check operations pick out individual fields from the License in the hidden control.
					// Currently they target the controls by ID and the controls are defined with bidirectional binding so the values are also transferred back to the
					// bound fields in the record.
					// An alternative would be to replace all the Check operations with Init's having the same activity, with a CalculatedValue as input, and targeting
					// the field directly (by path). In this case the bidirectional binding would not be required, although CalculatedValue does not have the same genericized
					// type-checking that Check has. But it would remove the need for the bidirectional binding.
					new Check2<object, int?>(TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
						.Operand1(licenseId)
						.Operand2(applicationIDId,
							delegate (object license) {
								return (int?)((Libraries.Licensing.AnalyzedLicense)license)?.License.ApplicationID;
							}
						),
					new Check2<object, int?>(TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
						.Operand1(licenseId)
						.Operand2(licenseModelId,
							delegate (object license) {
								return (int?)((Libraries.Licensing.AnalyzedLicense)license)?.License.LicenseModel;
							}
						),
					new Check2<object, int?>(TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
						.Operand1(licenseId)
						.Operand2(licenseCountId,
							delegate (object license) {
								return (int?)((Libraries.Licensing.AnalyzedLicense)license)?.License.LicenseCount;
							}
						),
					new Check2<object, int?>(TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
						.Operand1(licenseId)
						.Operand2(expiryModelId,
							delegate (object license) {
								return (int?)((Libraries.Licensing.AnalyzedLicense)license)?.License.ExpiryModel;
							}
						),
					new Check2<object, DateTime?>(TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
						.Operand1(licenseId)
						.Operand2(expiryDateId,
							delegate (object license) {
								return license == null ? null : ((Libraries.Licensing.AnalyzedLicense)license).License.ExpiryDateSpecified ? (DateTime?)((Libraries.Licensing.AnalyzedLicense)license).License.Expiry : null;
							}
						),
					new Check2<object, string>()
						.Operand1(licenseId)
						.Operand2(expiryStatusId,
							delegate (object license) {
								var al = (Libraries.Licensing.AnalyzedLicense)license;
								return (al == null || al.ExpiryMessage == null) ? null : Libraries.Exception.FullMessage(al.ExpiryMessage);
							}
						),
					new Check2<object, int?>(TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone))
						.Operand1(licenseId)
						.Operand2(licenseIDId,
							delegate (object license) {
								return license == null ? null : (int?)((Libraries.Licensing.AnalyzedLicense)license).License.LicenseID;
							}
						),
					new Check2<object, string>()
						.Operand1(licenseId)
						.Operand2(licenseIDStatusId,
							delegate (object license) {
								var al = (Libraries.Licensing.AnalyzedLicense)license;
								return (al == null || al.LicenseIdMessage == null) ? null : Libraries.Exception.FullMessage(al.LicenseIdMessage);
							}
						)
					);
			});
			#endregion
			#region Location
			DefineEditTbl(dsMB.Schema.T.Location, delegate () {
				return new Tbl(dsMB.Schema.T.Location, TId.Location,
				new Tbl.IAttr[] {
					LocationGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.Location.F.Code),
						BTbl.ListColumn(dsMB.Path.T.Location.F.Desc, NonPerViewColumn)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault)),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					TblFixedRecordTypeNode.New(),
					TblColumnNode.New(dsMB.Path.T.Location.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Location.F.Desc, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Location.F.GISLocation, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Location.F.Comment, DCol.Normal)
				)
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.Location, new DelayedCreateTbl(delegate () {
				return TILocations.LACComposite(TId.Location, dsMB.Schema.T.Location,
					null,
					TILocations.ViewUse.Full,       // Table 0 (PostalAddress)
					TILocations.ViewUse.Full,       // Table 1 (TemporaryStorage)
					TILocations.ViewUse.Full,       // Table 2 (Unit)
					TILocations.ViewUse.Full,       // Table 3 (PermanentStorage)
					TILocations.ViewUse.Full,       // Table 4 (PlainRelativeLocation)
					TILocations.ViewUse.Full        // Table 5 (TemplateTemporaryStorage)
				);
			}));
			#endregion
			#region PlainRelativeLocation
			DefineTbl(dsMB.Schema.T.PlainRelativeLocation, delegate () {
				return new Tbl(dsMB.Schema.T.PlainRelativeLocation, TId.SubLocation,
				new Tbl.IAttr[] {
					LocationGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID.F.Desc, NonPerViewColumn)
					),
					new ETbl(
						ETbl.UseNewConcurrency(true),
						ETbl.CustomCommand(delegate(EditLogic editorLogic) {
							var ShowOnMap = new EditLogic.CommandDeclaration(KB.K("Show on map"), new ShowOnMapCommand(editorLogic));
							var ShowOnMapGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
							ShowOnMapGroup.Add(ShowOnMap);
							return ShowOnMapGroup;
						})
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID.F.GISLocation, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.ContainingLocationID,
							new NonDefaultCol(),
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
							new ECol(
								Fmt.SetPickFrom(TILocations.LocationBrowseTblCreator),
								FilterOutContainedLocations(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID, dsMB.Path.T.LocationDerivations.F.LocationID)
							)),
						TblColumnNode.New(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.ContainingLocationID,
							new DefaultOnlyCol(),
							new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
							new ECol(
								Fmt.SetPickFrom(TILocations.LocationBrowseTblCreator)
							)),
						TblColumnNode.New(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Contact, TId.SubLocation,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID, dsMB.Path.T.Contact.F.LocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.SubLocation,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID, dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Storeroom, TId.SubLocation,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TemporaryStorageAndItem, TId.SubLocation,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID, dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ContainingLocationID, DCol.Normal, ECol.Normal))
				)
				);
			});
			RegisterExistingForImportExport(TId.SubLocation, dsMB.Schema.T.PlainRelativeLocation);
			#endregion
			#region PostalAddress
			DefineTbl(dsMB.Schema.T.PostalAddress, delegate () {
				return new Tbl(dsMB.Schema.T.PostalAddress, TId.PostalAddress,
				new Tbl.IAttr[] {
					LocationGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PostalAddress.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PostalAddress.F.LocationID.F.Desc, NonPerViewColumn)
					),
					new ETbl(
						ETbl.UseNewConcurrency(true),
						ETbl.CustomCommand(delegate(EditLogic editorLogic) {
							var ShowOnMap = new EditLogic.CommandDeclaration(KB.K("Show on map"), new ShowOnMapCommand(editorLogic));
							var ShowOnMapGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
							ShowOnMapGroup.Add(ShowOnMap);
							return ShowOnMapGroup;
						})
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.LocationID.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.Address1, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.Address2, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.City, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.Territory, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.Country, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.PostalCode, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.LocationID.F.GISLocation, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.PostalAddress.F.LocationID.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Contact, TId.PostalAddress,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PostalAddress.F.LocationID, dsMB.Path.T.Contact.F.LocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.PostalAddress,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PostalAddress.F.LocationID, dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Storeroom, TId.PostalAddress,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PostalAddress.F.LocationID, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.TemporaryStorageAndItem, TId.PostalAddress,
						TblColumnNode.NewBrowsette(dsMB.Path.T.PostalAddress.F.LocationID, dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ContainingLocationID, DCol.Normal, ECol.Normal))
				)
				);
			});
			RegisterExistingForImportExport(TId.PostalAddress, dsMB.Schema.T.PostalAddress);
			#endregion
			#region RelativeLocation/Sub Location
			DefineTbl(dsMB.Schema.T.RelativeLocation, delegate () {
				return new Tbl(dsMB.Schema.T.RelativeLocation, TId.SubLocation,
				new Tbl.IAttr[] {
					LocationGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.RelativeLocation.F.Code),
						BTbl.ListColumn(dsMB.Path.T.RelativeLocation.F.LocationID.F.Desc, NonPerViewColumn)
					),
					new ETbl()
				},
				new TblLayoutNodeArray(
					TblFixedRecordTypeNode.New(),
					TblColumnNode.New(dsMB.Path.T.RelativeLocation.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.RelativeLocation.F.LocationID.F.Desc, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.RelativeLocation.F.LocationID.F.Comment, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.RelativeLocation.F.LocationID.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.RelativeLocation.F.LocationID.F.GISLocation, DCol.Normal, ECol.Normal)
				)
				);
			});
			#endregion
			#region Session
			DefineTbl(dsMB.Schema.T.Session, delegate () {
				return new Tbl(dsMB.Schema.T.Session, TId.Session,
				new Tbl.IAttr[] {
					SessionsGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.Session.F.ClientName),
							 BTbl.ListColumn(dsMB.Path.T.Session.F.Creation))
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.Session.F.ApplicationID, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Session.F.ClientName, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Session.F.Creation, DCol.Normal)
				)
			);
			});
			#endregion
			#region ExternalTag
			DefineBrowseTbl(dsMB.Schema.T.ExternalTag, delegate () {
				return new CompositeTbl(dsMB.Schema.T.ExternalTag, TId.ExternalTag,
					new Tbl.IAttr[] {
							UnitsDependentGroup | WorkOrdersGroup | PurchasingGroup | RequestsGroup | PurchasingAssignmentsGroup | RequestsAssignmentsGroup | WorkOrdersAssignmentsGroup,
							new BTbl(
								BTbl.ListColumn(dsMB.Path.T.ExternalTag.F.ExternalTag)
							),
					},
					null,
					new CompositeView(dsMB.Path.T.ExternalTag.F.RequestID,
						CompositeView.RecognizeByValidEditLinkage(),
						OnlyViewEdit, Fmt.SetExport(false)
					),
					new CompositeView(dsMB.Path.T.ExternalTag.F.WorkOrderID,
						CompositeView.RecognizeByValidEditLinkage(),
						OnlyViewEdit, Fmt.SetExport(false)
					),
					new CompositeView(dsMB.Path.T.ExternalTag.F.PurchaseOrderID,
						CompositeView.RecognizeByValidEditLinkage(),
						OnlyViewEdit, Fmt.SetExport(false)
					),
					new CompositeView(dsMB.Path.T.ExternalTag.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID,
						CompositeView.RecognizeByValidEditLinkage(),
						OnlyViewEdit, Fmt.SetExport(false)
					),
					new CompositeView(dsMB.Path.T.ExternalTag.F.LocationID.F.RelativeLocationID.F.UnitID,
						CompositeView.RecognizeByValidEditLinkage(),
						OnlyViewEdit, Fmt.SetExport(false)
					),
					new CompositeView(dsMB.Path.T.ExternalTag.F.LocationID.F.RelativeLocationID.F.PermanentStorageID,
						CompositeView.RecognizeByValidEditLinkage(),
						OnlyViewEdit, Fmt.SetExport(false)
					));
			});
			#endregion
			#region Test Tables
#if ZTESTTABLE
#if TESTINGCOMPOSITETABLES
			#region TestCompositeTable
			Tbl tblLocView0 = new Tbl( dsMB.Schema.T.Location,
				"Location (view #0)",
				new Tbl.IAttr[] { new BTbl(), new ETbl() },
				TblColumnNode.NewLastColumnBound("Code (view #0)", dsMB.Path.T.Location.F.Code,									DColBase.Normal,	EColBase.Normal			),
				TblColumnNode.NewLastColumnBound("Description (view #0)", dsMB.Path.T.Location.F.Desc,							DColBase.Normal,	EColBase.Normal			)
				);
			Tbl tblLocView1 = new Tbl( dsMB.Schema.T.Location,
				"Location (view #1)",
				new Tbl.IAttr[] { new BTbl(), new ETbl() },
				TblColumnNode.NewLastColumnBound("Code (view #1)", dsMB.Path.T.Location.F.Code,									DColBase.Normal,		EColBase.Normal			),
				TblColumnNode.NewLastColumnBound("Comment (view #1)", dsMB.Path.T.Location.F.Comment,						DColBase.Normal,			EColBase.Normal			)
				);
			DefineTbl(dsMB.Schema.T.ZTestTable, delegate() {
				return new CompositeTbl(dsMB.Schema.T.ZTestTable, TId.TestCompositeTable,
				new Tbl.IAttr[] {
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Unsigned32),
						BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.GUIDREF.F.Code),
						BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Code)
					),
					new ETbl()
				},
				new TblLayoutNode[]
				{
					TblColumnNode.NewLastColumnBound("Record Type (common)", dsMB.Path.T.ZTestTable.F.Unsigned32, DColBase.Normal),
					TblColumnNode.NewLastColumnBound( "Underlying record Code (common)", dsMB.Path.T.ZTestTable.F.GUIDREF.F.Code, DColBase.Normal),
					TblColumnNode.NewLastColumnBound( "Underlying record ID (common)", dsMB.Path.T.ZTestTable.F.GUIDREF.F.Code, DColBase.Normal ),
					TblColumnNode.NewLastColumnBound( "Code (common)", dsMB.Path.T.ZTestTable.F.Code, DColBase.Normal)
				},
				dsMB.Schema.T.ZTestTable.F.Unsigned32, dsMB.Schema.T.ZTestTable.F.GUIDREF,
				new CompositeView( tblLocView0,
					TblColumnNode.NewLastColumnBound("Date (view #0)", dsMB.Path.T.ZTestTable.F.Date, DColBase.Normal),
					TblColumnNode.NewLastColumnBound("Quantity (view #0)", dsMB.Path.T.ZTestTable.F.Quantity, DColBase.Normal)
				),
				new CompositeView( tblLocView1,
					TblColumnNode.NewLastColumnBound("Desc (view #1)", dsMB.Path.T.ZTestTable.F.Desc, DColBase.Normal),
					TblColumnNode.NewLastColumnBound("Subject (view #1)", dsMB.Path.T.ZTestTable.F.Subject, DColBase.Normal)
				)
			));
			#endregion
#else
			#region ZTestTable
			DefineTbl(dsMB.Schema.T.ZTestTable, delegate() {
				return new Tbl(dsMB.Schema.T.ZTestTable, TId.TestTable,
				new Tbl.IAttr[] { BasicGroup, new BTbl(
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredAddress),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredAutoNumber),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredBool),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredCode),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredCultureInfoLCID),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredCurrency),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredDate),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredDateTime),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredDayOfYear),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredDaySpan),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredDesc),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredDowntime),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredEmailAddress),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredGUIDREF),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredGUIDREF.F.Desc),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredGenerationValue),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredLaborDuration),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredLineText),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredMeterReadingValue),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredMeterSpan),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredMonthSpan),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredPhoneNumber),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredPostalCode),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredQuantity),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredSubject),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredURL),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredUserName),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredVariableLengthString),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredComment),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredMultiLineUnlimited),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredVersionInfo),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredWorkOrderDuration),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredUnsigned16),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.RequiredUnsigned32),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Address),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.AutoNumber),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Bool),
					CodeColumn(dsMB.Path.T.ZTestTable.F.Code),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.CultureInfo),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Currency),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Date),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.DateTime),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.DayOfYear),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.DaySpan),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Desc),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Downtime),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.EmailAddress),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.GUIDREF),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.GUIDREF.F.Desc),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.GenerationValue),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.LaborDuration),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.LineText),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.MeterReadingValue),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.MeterSpan),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.MonthSpan),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.PhoneNumber),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.PostalCode),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.ProcessName),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Quantity),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Subject),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.URL),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.UserName),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.VariableLengthString),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Comment),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.MultiLineUnlimited),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.VersionInfo),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.WorkOrderDuration),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Unsigned16),
					BTbl.ListColumn(dsMB.Path.T.ZTestTable.F.Unsigned32)
				), new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.Delete)), TIReports.NewLocalPTbl() },
				new TblLayoutNodeArray(
				TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Id, DColBase.Normal, EColBase.AllReadonly),
				TblTabNode.New(KB.K("Required Fields"), KB.K("Required"), new TblLayoutNode.IAttr[] { DColBase.Normal, EColBase.Normal },
			TblSectionNode.New(new TblLayoutNode.IAttr[] { DColBase.Normal, EColBase.Normal },
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredAddress, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredAutoNumber, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredBool, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredCode, DColBase.Normal, EColBase.Normal)),
				TblSectionNode.New(new TblLayoutNode.IAttr[] { DColBase.Normal, EColBase.Normal },
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredCultureInfo, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredCurrency, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredDate, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredDateTime, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredDayOfYear, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredDaySpan, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredDesc, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredDowntime, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredEmailAddress, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredGUIDREF, new DColBase(DColBase.Display(dsMB.Path.T.AccessCode.F.Code)), new EColBase(Fmt.SetPickFrom(dsMB.Schema.T.AccessCode))),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredGUIDREF.F.Desc,  DColBase.Normal, EColBase.AllReadonly)
				),
				TblSectionNode.New(new TblLayoutNode.IAttr[] { DColBase.Normal, EColBase.Normal },
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredGenerationValue, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredLaborDuration, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredLineText, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredMeterReadingValue, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredMeterSpan, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredMonthSpan, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredPhoneNumber, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredPostalCode, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredQuantity, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredSubject, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredURL, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredUserName, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredVariableLengthString, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredComment, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredMultiLineUnlimited, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredVersionInfo, DColBase.Normal, EColBase.Normal)
				),
				TblSectionNode.New(new TblLayoutNode.IAttr[] { DColBase.Normal, EColBase.Normal },
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredWorkOrderDuration, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredUnsigned16, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.RequiredUnsigned32, DColBase.Normal, EColBase.Normal))
				),
				TblTabNode.New(KB.K("Non Required Fields"), KB.K("Non Required"), new TblLayoutNode.IAttr[] { EColBase.Normal },
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Address, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.AutoNumber, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Bool, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Code, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.CultureInfo, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Currency, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Date, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.DateTime, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.DayOfYear, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.DaySpan, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Desc, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Downtime, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.EmailAddress, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.GUIDREF, new DColBase(DColBase.Display(dsMB.Path.T.AccessCode.F.Code)), new EColBase(Fmt.SetPickFrom(dsMB.Schema.T.AccessCode))),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.GUIDREF.F.Desc,  DColBase.Normal, EColBase.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.GenerationValue, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.LaborDuration, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.LineText, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.MeterReadingValue, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.MeterSpan, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.MonthSpan, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.PhoneNumber, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.PostalCode, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.ProcessName, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Quantity, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Subject, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.URL, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.UserName, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.VariableLengthString, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Comment, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.MultiLineUnlimited, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.VersionInfo, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.WorkOrderDuration, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Unsigned16, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Unsigned32, DColBase.Normal, EColBase.Normal)),
				TblTabNode.New(KB.K("Custom Fields"), KB.K("Custom"), new TblLayoutNode.IAttr[] { DColBase.Normal, EColBase.Normal },
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Bool, DColBase.Normal, EColBase.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Code, DColBase.Normal, EColBase.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.Desc, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.EmailAddress, DColBase.Normal, EColBase.Normal),
					TblColumnNode.New(dsMB.Path.T.ZTestTable.F.URL, DColBase.Normal, EColBase.Normal))
				));
			});

			#endregion
#endif
#endif
			#endregion
			// Define CompositeTbl entries after all the other tables so the CompositeView
			// constructors can reference other Tbl objects through tInfo[xxx].
			TILocations.DefineTblEntries();
			TIRelationship.DefineTblEntries();
		}
		internal static readonly object ContactNameId = KB.I("ContactNameId");
		#endregion
		#region BuildRegisteredImportTable
		// These are here to ensure that the TIGeneralMB3 class static initializers are elaborated before any lookups in the TblRegistry.
		// One should ALWAYS call TIGeneralMB3.FindXxxxx rather than TblRegistry.FindXxxxx as the latter will not cause elaboration of the statics.
		// Note that without these methods, a call to TIGeneralMB3.FindXxxxx will quitely resolve to the inherited TblRegistry.FindXxxxx at compile time
		// and TIGeneralMB3 will not have its statics elaborated.
		public new static string FindImportNameFromSchemaIdentification(string schemaIdentification) {
			return TblRegistry.FindImportNameFromSchemaIdentification(schemaIdentification);
		}
		public new static Tuple<string, DelayedCreateTbl> FindImport(string identifyingName) {
			return TblRegistry.FindImport(identifyingName);
		}
		public new static Tuple<string, DelayedCreateTbl> FindImport(Tbl.TblIdentification tid) {
			return TblRegistry.FindImport(tid);
		}
		public new static Tbl FindEditTbl(DBI_Table t) {
			return TblRegistry.FindEditTbl(t);
		}
		public new static DelayedCreateTbl FindDelayedEditTbl(DBI_Table t) {
			return TblRegistry.FindDelayedEditTbl(t);
		}
		public new static Tbl FindBrowseTbl(DBI_Table t) {
			return TblRegistry.FindBrowseTbl(t);
		}
		public new static DelayedCreateTbl FindDelayedBrowseTbl(DBI_Table t) {
			var dt = TblRegistry.FindDelayedBrowseTbl(t);
			System.Diagnostics.Debug.Assert(dt != null, "FindBrowseTbl failures", "Browse Tbl for {0} not found", t.Name);
			return dt;
		}
		public new static Dictionary<Key, string> RegisteredImportKeys {
			get {
				return TblRegistry.RegisteredImportKeys;
			}
		}
		#endregion
		#region CheckTableInfo
#if DEBUG
		[Invariant]
		public class TableInfoDebug {
			private TableInfoDebug() {
			}


			// TODO: This code still complains about far too many "unused" tbl's, most of which are not the registered Tbl for their own schema.
			public static void CheckTableInfo(MenuDef menuRoot) {
				var tblDepths = new Dictionary<Tbl, int>(ObjectByReferenceEqualityComparer<Tbl>.Instance);
				System.Diagnostics.Debug.Indent();
				System.Diagnostics.Debug.WriteLine("In CheckTableInfo");
				// We use a peculiar looping structure so that new nodes introduced to the head of the list during a visit are eventually visited as well.
				int loopcount = 0;
				List<DelayedCreateTbl> previousList = new List<DelayedCreateTbl>();
				for (;;) {
					List<DelayedCreateTbl> currentList = new List<DelayedCreateTbl>();
					System.GC.Collect();
					foreach (DelayedCreateTbl dct in DelayedCreateTbl.AllInstances.Keys)
						if (!previousList.Contains(dct))
							currentList.Add(dct);
					if (currentList.Count == 0)
						break;
					if (++loopcount > 4) {
						// Last time this code was touched, 3 passes was enough.
						System.Diagnostics.Debug.WriteLine("Giving up Tbl visit, each pass makes new nodes");
						break;
					}

					int tblCount = 0;
					foreach (DelayedCreateTbl dct in currentList) {
						Tbl tbl;
						try {
							tbl = dct.Tbl;
						}
						catch (System.Exception e) {
							System.Diagnostics.Debug.WriteLine("Got exception creating unknown Tbl: " + Libraries.Exception.FullMessage(e));
							continue;
						}
						tblDepths[tbl] = int.MaxValue;
						try {
							int nodeIndex = 0;
							if (tbl.Columns != null)
								foreach (TblLayoutNode ln in tbl.Columns) {// This is the recursive iterator
									try {
										CheckTblLeafNode(tbl, null, ln);
									}
									catch (System.Exception e) {
										Libraries.Exception.AddContext(e, new MessageExceptionContext(KB.T("recursive leaf node {0}"), nodeIndex));
										throw;
									}
									++nodeIndex;
								}
						}
						catch (System.Exception e) {
							System.Diagnostics.Debug.WriteLine("Got exception visiting Tbl '" + tbl.Identification.DebugIdentification + "': " + Libraries.Exception.FullMessage(e));
						}
						++tblCount;
					}
					Thinkage.Libraries.Diagnostics.Debug.WriteFormattedLine(null, "Processed {0} Tbl's in pass {1}", tblCount, loopcount);
					previousList.AddRange(currentList);
				}
				// Go through the main menu tree and set all their TBL depths to 1 using the SetDepth(tbl, 1, tblDepths) method.
				// foreach menu entry. This will define how much drill-down is required to get to each Tbl. A value of 1 indicates the Tbl is
				// browsable in a main browser and editable (assuming it is editable at all) with one click of the Edit button. A depth of 2
				// means it is a browsette within a main-level browser, and two Edit clicks are required, etc.
				SetTblDepth(menuRoot, tblDepths);

				//List Tbls with depth MaxValue (unused) or with depth > 3. Tbl's should be identified by their Identification and
				// by their schema if they are the schema-registered ones. Also Tbl's which allow EdtMode.EditDefault whose depth > 1 should
				// be identified. Note that Tbl's only used in picker controls will be listed as "unused".
				foreach (KeyValuePair<Tbl, int> kvp in tblDepths)
					if (kvp.Value == int.MaxValue)
						TblAnnotation(kvp.Key, "Not used in browsing/editing (but perhaps used for a picker)");
				foreach (KeyValuePair<Tbl, int> kvp in tblDepths)
					if (kvp.Value < int.MaxValue && kvp.Value > 3)
						TblAnnotation(kvp.Key, Strings.IFormat("Excessive Drilldown depth {0}", kvp.Value));
				foreach (KeyValuePair<Tbl, int> kvp in tblDepths)
					if (kvp.Value < int.MaxValue && kvp.Value > 1) {
						ETbl etbl = ETbl.Find(kvp.Key);
						if (etbl != null && (etbl.AllowedInitialMode(EdtMode.EditDefault) || etbl.AllowedInitialMode(EdtMode.ViewDefault)))
							TblAnnotation(kvp.Key, Strings.IFormat("Tbl allows default editing or viewing but depth {0} means it cannot be a main-level browser", kvp.Value));
					}
				CheckReportTbls();
				CheckTblRegistration();
				// TODO: Build a set of Tbl's that allow UnDelete but are not referenced in any way that would permit the UnDelete operation (not sure of exact criteria)
				System.Diagnostics.Debug.Unindent();
				System.Diagnostics.Debug.WriteLine("Done CheckTableInfo");
			}
			private static void TblAnnotation(Tbl tbl, string annotation) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder(annotation);
				sb.Append(KB.I(": "));
				AddTblDebugAnnotation(tbl, sb);
				System.Diagnostics.Debug.WriteLine(sb.ToString());
			}
			private static void TblNodeAnnotation(TblLayoutNode n, string additionalNodeIdentification, string annotation) {
				TblColumnNode cn = n as TblColumnNode;
				System.Text.StringBuilder sb = new System.Text.StringBuilder(annotation);
				sb.Append(KB.I(" ["));
				if (n.Label != null)
					sb.Append(KB.I("label=") + n.Label.IdentifyingName);
				if (cn != null)
					sb.Append(KB.I(" referencing path ") + cn.Path.ToString());
				sb.Append(KB.I("] : "));
				AddTblDebugAnnotation(n.OwnerTbl, sb);
				if (additionalNodeIdentification != null)
					sb.AppendFormat(" : {0}", additionalNodeIdentification);
				System.Diagnostics.Debug.WriteLine(sb.ToString());
			}
			private static void CheckReportTbls() {
				System.Reflection.FieldInfo[] reportTbls = typeof(TIReports).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public);
				foreach (System.Reflection.FieldInfo f in reportTbls) {
					Tbl tbl = f.GetValue(null) as Tbl;
					var fieldName = "TIReports." + f.Name;
					if (tbl == null)
						continue;
					try {
						int nodeIndex = 0;
						if (tbl.Columns != null)
							foreach (TblLayoutNode ln in tbl.Columns) {// This is the recursive iterator
								try {
									CheckTblLeafNode(tbl, fieldName, ln);
								}
								catch (System.Exception e) {
									Libraries.Exception.AddContext(e, new MessageExceptionContext(KB.T("recursive leaf node {0}"), nodeIndex));
									throw;
								}
								++nodeIndex;
							}
					}
					catch (System.Exception e) {
						System.Diagnostics.Debug.WriteLine("Got exception visiting ReportTbl '" + tbl.Identification.DebugIdentification + "': " + Libraries.Exception.FullMessage(e));
					}
					// Check Report Column references for duplicates. We want to ensure that we don't have two report columns referring to the same path.
					// Although these tend to have the same label and thus merge in the UI, they will still cause the value to display multiple times in the report, and can also confuse the
					// elision of values displayed in the group headers.
					// We also look for suspicious columns, including the Id and HiddenColumn for the referenced table of the path.
					var pathDuplicates = new Set<DBI_Path>();
					var labelDuplicates = new Set<string>();
					var possibleNonReportableColumns = new Set<DBI_Column>(); // we add the set of Columns from the top left schema, and remove them as we process the TblColumnNodes; ones remaining are 'unreferenced' in the Tbl and should be reviewed as they are values that are not being included in the report
																			  //TODO: make this a recursive search through linkage columns.
					foreach (DBI_Column column in tbl.Schema.Columns)
						if (!(column.EffectiveType is Thinkage.Libraries.TypeInfo.LinkedTypeInfo) && !(tbl.Schema.InternalId.ReferencedColumn == column) && !(tbl.Schema.PathToHiddenColumn == null || tbl.Schema.PathToHiddenColumn.ReferencedColumn == column))
							possibleNonReportableColumns.Add(column);

					foreach (var c in tbl.Columns) {
						var cn = c as TblColumnNode;
						if (cn == null)
							continue;
						if (cn.Label != null) {
							if (labelDuplicates.Contains(cn.Label.IdentifyingName))
								TblAnnotation(tbl, Strings.IFormat("{0}:Duplicate report column label {2} with path {1}", fieldName, cn.Path, cn.Label.IdentifyingName));
							else {
								labelDuplicates.Add(cn.Label.IdentifyingName);
							}
						}
						if (possibleNonReportableColumns.Contains(cn.Path.ReferencedColumn))
							possibleNonReportableColumns.Remove(cn.Path.ReferencedColumn);
						if (pathDuplicates.Contains(cn.Path))
							TblAnnotation(tbl, Strings.IFormat("{0}:Duplicate report column path {1} with label {2}", fieldName, cn.Path, cn.Label == null ? "<null>" : cn.Label.Translate()));
						else
							pathDuplicates.Add(cn.Path);
					}
					if (possibleNonReportableColumns.Count > 0) {
						TblAnnotation(tbl, Strings.IFormat("{0}:Report base schema {1} has {2} columns that may not be reportable -- {3}", fieldName, tbl.Schema.Name, possibleNonReportableColumns.Count, string.Join(", ", possibleNonReportableColumns.Select(i => i.Name).ToList())));
					}
					// Check default grouping for grouping by a text field. This is generally inappropriate because such a field could be duplicated by unrelated records (some of which may be Hidden to avoid the
					// unique key violation that might otherwise be expected, but note the BIG exception of Location.Code (Code Path) which is not unique at all. Instead the default grouping should be by the
					// record that contains the text field.
					RTblBase rTbl = RTblBase.Find(tbl);
					if (rTbl == null)
						TblAnnotation(tbl, KB.I("Supposed Report Tbl contains no RTblBase attribute"));
					else {
						var defaultSortingGrouping = rTbl.SortingGrouping;
						if (defaultSortingGrouping != null)
							foreach (RTblBase.SortingGroupingArg sga in defaultSortingGrouping) {
								if (sga.SortingGrouping.KeyExpression.Op == SqlExpression.OpName.Path) {
									DBI_Path p = sga.SortingGrouping.KeyExpression.Path;
									var referencedType = p.ReferencedTypeWithSelfLinkage;
									if (referencedType is LinkedTypeInfo) {
										// That's OK
									}
									else
										TblAnnotation(tbl, Strings.IFormat("Report default grouping references path {0} whose referenced type is {1}", p, referencedType.ToTypeSpecification()));
								}
								// else the grouping is specified by NodeId which is a pain to do because nowhere in the previous code did we develop a dictionary of node id's
							}
					}
				}
			}
			private static void SetTblDepth(MenuDef menu, Dictionary<Tbl, int> tblDepths) {
				CommandMenuDef cMenu = menu as CommandMenuDef;
				TreeViewExplorer.ShowExplorerItemCommand cmd = cMenu?.menuCommand as TreeViewExplorer.ShowExplorerItemCommand;
				if (cmd != null) {
					BrowseExplorer bex = cmd.Item as BrowseExplorer;
					if (bex != null)
						SetTblDepth(bex.TblInfo, 1, tblDepths);
				}
				BrowseMenuDef bMenu = menu as BrowseMenuDef;
				if (bMenu != null)
					SetTblDepth(bMenu.TblCreator.Tbl, 1, tblDepths);

				if (menu.subMenu != null)
					foreach (MenuDef item in menu.subMenu)
						SetTblDepth(item, tblDepths);
			}
			private static void SetTblDepth(Tbl tbl, int depth, Dictionary<Tbl, int> tblDepths) {
				if (tblDepths[tbl] <= depth)
					return;
				tblDepths[tbl] = depth;
				CompositeTbl ct = tbl as CompositeTbl;
				if (ct != null)
					foreach (CompositeView view in ct.TblViews)
						if ((view.HasEditLinkage && view.EditTblVisibility != Tbl.Visibilities.Hidden && !view.FilterIsConstantFalse)
							|| view.CanEdit(EdtMode.New))
							SetTblDepth(view.EditTbl, depth, tblDepths);
				if (tbl.Columns != null)
					foreach (TblLayoutNode ln in tbl.Columns) {// This is the recursive iterator
						Fmt.ShowReferencesArg brn = Fmt.GetShowReferences(ln);
						DCol dc = TblLayoutNode.GetDCol(ln);
						Fmt.ShowReferencesArg dbrn = dc == null ? null : Fmt.GetShowReferences(dc);
						ECol ec = TblLayoutNode.GetECol(ln);
						Fmt.ShowReferencesArg ebrn = ec == null ? null : Fmt.GetShowReferences(ec);
						// TODO: Only do this if it is a Child/Full browsette, not a property browsette.
						if (brn != null || dbrn != null || ebrn != null) {
							if (brn != null)
								SetTblDepth(brn.BasisTbl, depth + 1, tblDepths);
							if (ebrn != null)
								SetTblDepth(ebrn.BasisTbl, depth + 1, tblDepths);
							if (dbrn != null)
								SetTblDepth(dbrn.BasisTbl, depth + 1, tblDepths);
						}
						else {
							// If it is a picker in an editor, determine the picker table and set it to depth+1 as well.
							TblLeafNode leafNode = ln as TblLeafNode;
							if (leafNode != null) {
								var valueNode = leafNode as TblValueNode;
								Tbl pickerTbl = Fmt.GetPickFrom(new Fmt(leafNode.ReferencedType, valueNode?.ReferencedValue, TblLayoutNode.GetECol(leafNode), leafNode));
								if (pickerTbl != null)
									SetTblDepth(pickerTbl, depth + 1, tblDepths);
							}
						}
					}
			}
			private static List<DBI_Column> CreateXIDList(DBI_Table schema) {
				List<DBI_Column> xidList = new List<DBI_Column>();
				DoCreateXIDList(xidList, new DBI_PathToRow(schema), new Stack<DBI_Column>());
				return xidList;
			}
			private static void DoCreateXIDList(List<DBI_Column> list, DBI_PathToRow pathToTable, Stack<DBI_Column> pathsDone) {
				if (pathToTable.ReferencedTable.ExternalId == null)
					return;
				foreach (DBI_Column c in pathToTable.ReferencedTable.ExternalId) {
					DBI_Path pathToColumn = new DBI_Path(pathToTable, c);
					if (!c.IsConstrained) {
						DBI_Path hiddenPath = pathToColumn.CorrespondingHidingPath();
						if (hiddenPath == null || hiddenPath.ReferencedColumn != pathToColumn.ReferencedColumn)
							list.Add(pathToColumn.ReferencedColumn);
					}
					else if (pathsDone.Contains(c)) {
						throw new System.Exception("Recursion in XID list building");
						// TODO: The XID definition contains a recursion that we don't want to chase. Perhaps we should insert a Source here that returns the text "..."
						// There are some records, like Location derivations, that have a field separate from the XID that contains the compound code already built.
						// TODO: Figure out some way of specifying that this is true, and also a way of delimiting the fields in a single table's xid
					}
					else {
						pathsDone.Push(c);
						DoCreateXIDList(list, pathToColumn.PathToReferencedRow, pathsDone);
						pathsDone.Pop();
					}
				}
			}
			private static void CheckPickerTbl(Tbl pickerTbl) {
				// Verify list columns of a picker Tbl at least use the 'visible' XID fields in ClosedForm
				System.Collections.Generic.Dictionary<DBI_Column, bool> xidNames = new Dictionary<DBI_Column, bool>();
				foreach (DBI_Column f in CreateXIDList(pickerTbl.Schema))
					xidNames[f] = false;
				BTbl bTbl = BTbl.Find(pickerTbl);
				if (bTbl == null) // a picker without a BTbl ? I don't think so
					throw new System.Exception(Strings.IFormat("Tbl {0} used as picker has no BTbl attribute", pickerTbl.Identification.DebugIdentification));
				foreach (BTbl.ListColumnArg la in bTbl.ListColumnArgs) {
					var pathLA = la as BTbl.PathListColumnArg;
					if (pathLA != null && (pathLA.Context & BTbl.ListColumnArg.Contexts.ClosedCombo) != 0 && xidNames.ContainsKey(pathLA.Path.ReferencedColumn))
						xidNames[pathLA.Path.ReferencedColumn] = true;
				}
				System.Text.StringBuilder missingXids = new System.Text.StringBuilder();
				foreach (DBI_Column c in xidNames.Keys) {
					if (xidNames[c] == false) {
						if (missingXids.Length > 0)
							missingXids.Append(", ");
						missingXids.Append(Strings.IFormat("{0}.{1}", c.Table.Name, c.Name));
					}
				}
				if (missingXids.Length > 0)
					TblAnnotation(pickerTbl, Strings.IFormat("No ListArg in closed context for XID identified by name(s) {0}", missingXids.ToString()));
			}
			private static string IdentifyPermissionAttributes(List<PermissionToView> listOfAttrs) {
				System.Text.StringBuilder identity = new System.Text.StringBuilder();
				for (int i = 0; i < listOfAttrs.Count; ++i) {
					if (i > 0)
						identity.Append(", ");
					identity.Append(((PermissionToView)listOfAttrs[i]).RightName);
				}
				return identity.ToString();
			}
			private static void CheckTblLeafNode(Tbl ownerTbl, string additionalIdentification, TblLayoutNode node) {
				TblLeafNode leafNode = node as TblLeafNode;
				if (leafNode == null)
					return;
				if (leafNode.ReferencedType is CurrencyTypeInfo) {
					List<PermissionToView> listOfPermissionToViewAttributes = new List<PermissionToView>();
					foreach (PermissionToView pvAattr in TblLayoutNode.GetPermissionsToView(leafNode))
						if (pvAattr != null) {
							if (listOfPermissionToViewAttributes.Contains(pvAattr))
								TblNodeAnnotation(node, additionalIdentification, Strings.IFormat("Duplicated PermissionToView attribute on currency node: {0}", pvAattr.RightName));
							listOfPermissionToViewAttributes.Add(pvAattr);
						}
					if (listOfPermissionToViewAttributes.Count == 0)
						TblNodeAnnotation(node, additionalIdentification, "Missing PermissionToView on currency node");
				}
				Fmt.ShowReferencesArg brn = Fmt.GetShowReferences(leafNode);
				DCol dc = TblLayoutNode.GetDCol(leafNode);
				Fmt.ShowReferencesArg dbrn = dc == null ? null : Fmt.GetShowReferences(dc);
				ECol ec = TblLayoutNode.GetECol(leafNode);
				Fmt.ShowReferencesArg ebrn = ec == null ? null : Fmt.GetShowReferences(ec);
				if (brn != null || dbrn != null || ebrn != null) {
					if (brn != null) {
						Tbl t = brn.BasisTbl;
					}
					if (ebrn != null) {
						Tbl t = ebrn.BasisTbl;
					}
					if (dbrn != null) {
						Tbl t = dbrn.BasisTbl;
					}
				}
				else {
					var valueNode = leafNode as TblValueNode;
					Tbl pickerTbl = Fmt.GetPickFrom(new Fmt(leafNode.ReferencedType, valueNode?.ReferencedValue, TblLayoutNode.GetECol(leafNode), leafNode));
					if (pickerTbl != null)
						CheckPickerTbl(pickerTbl);
				}
				// The following check is to find places where the automatic calculation of whether a multiple selection picker and multiple save-new-record on a single
				// click might miss on whether such activity might cause a concurrency error or create non-deletable records. Essentially the RecordManager might try
				// to create not only a new record (and relateds) of the root of the recordset's path, but also some other referenced record. The code making the decision does not
				// inspect the TblColumnNodes nor the Init targets (which we also don't look at here but should) ahead of time and just assumes that only the path root records
				// (and related) will be created, and thus only the deletability and unique constraints on those tables must be considered.
				// Tbls flagged here must be manually inspected and dealt with in one of several ways:
				// 1 - manually determine that multi-new-save is OK, put a comment to that effect, and if the covertly-created record(s) have a unique constraint, use per-node overrides to prevent
				//		multi-select on values not in the unique field set.
				// 2 - manually determine that multi-new-save is not OK, and code the tbl attribute to prevent it.
				// 3 - Ensure that the covertly-referenced tables are marked with ETbl.RowEditType(p, rs, RecordManager.RowInfo.RowEditTypes.EditOnly) and thus will not create new rows even when in New mode
				TblColumnNode colNode = node as TblColumnNode;
				if (colNode != null && brn == null && ETbl.Find(ownerTbl) != null) {
					ECol ecol = TblLayoutNode.GetECol(node);
					if (ecol != null && (ecol.Access[(int)EdtMode.New] == TblLeafNode.Access.Writeable || ecol.Access[(int)EdtMode.Clone] == TblLeafNode.Access.Writeable)) {
						// See if the path crosses out of related records.
						DBI_Path p = colNode.Path;
						while (!p.IsSimple) {
							if (p.Column.LinkageType != DBI_Relation.LinkageTypes.Base && p.Column.LinkageType != DBI_Relation.LinkageTypes.Derived)
								Thinkage.Libraries.Diagnostics.Debug.WriteFormattedLine(null, "Path {0} on node {1} traverses outside of related records but is written by new-mode editor tbl {2}", colNode.Path.ToString(), node.Label, ownerTbl.Identification.DebugIdentification);
							p = p.AllButFirstLink;
						}
					}
				}
				TblUnboundControlNode unboundNode = node as TblUnboundControlNode;
				if (unboundNode != null && node.Label != null) {
					ECol ecol = TblLayoutNode.GetECol(unboundNode);
					if (ecol != null && (ecol.Access[(int)EdtMode.New] == TblLeafNode.Access.Writeable || ecol.Access[(int)EdtMode.Clone] == TblLeafNode.Access.Writeable)) {
						if (unboundNode.ReferencedType is BoolTypeInfo || unboundNode.ReferencedType is IntegralTypeInfo) {
							var fmt = new Fmt(unboundNode.ReferencedType, null, ecol, leafNode);
							// see if there a Fmt.IsSettingSaved present and warn if not.
							if (!Fmt.HasInitialValue(fmt)) {
								Thinkage.Libraries.Diagnostics.Debug.WriteFormattedLine(null, "UnboundControlNode {0} maybe needs a Fmt.SetIsSetting value in tbl {1}", node.Label == null ? KB.T("<no label>") : node.Label, ownerTbl.Identification.DebugIdentification);
							}
						}
					}
				}
			}
		}
#endif
		#endregion
	}
}
