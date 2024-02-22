using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Layout;
using System.Collections.Generic;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database
{
	public static partial class Licensing
	{
		private class MBLicensedObject : ILicensedObject {
			public MBLicensedObject(MBLicenseDefinition definition, DBClient licenseSession) {
				pDefinition = definition;
				Session = licenseSession;
			}
			public uint? CurrentLicenseUsage {
				get {
					if (pDefinition.TableToCount != null)
						using (dsMB ds = new dsMB(Session))
							return (uint)Session.CountNonDeletedRows(pDefinition.TableToCount);
					else
						return null;
				}
			}
			public System.DateTime DeemedReleaseDate {
				get { return Licensing.DeemedReleaseDate; }
			}
			public LicenseDefinition Definition {
				get { return pDefinition; }
			}
			public void EnforceLimit(uint limit) {
				if (pDefinition.TableToCount != null)
					Session.RegisterTableRowCountLimit(pDefinition.TableToCount, limit, Definition.LicenseOriginText, pDefinition.CountedItemsName, pDefinition.AllowCountingError);
			}
			private readonly MBLicenseDefinition pDefinition;
			private readonly DBClient Session;
		}
		public class MBLicensedObjectSet : ILicensedObjectSet {
			static MBLicensedObjectSet() {
				foreach (LicenseDefinition ld in AllMainbossLicenses)
					AllMainbossLicensesByAppId.Add(ld.LicensingApplicationID, (MBLicenseDefinition)ld);
			}
			public MBLicensedObjectSet(DBClient licenseSession) {
				LicenseSession = licenseSession;
			}
			private readonly DBClient LicenseSession;
			public ILicensedObject GetLicensedObject(int licensingApplicationID) {
				if (AllMainbossLicensesByAppId.TryGetValue(licensingApplicationID, out MBLicenseDefinition ld))
					return new MBLicensedObject(ld, LicenseSession);
				return null;
			}
			private const int InventoryCountLimit = 10;
			private const int RequestCountLimit = 10;
			private const int PurchaseOrderCountLimit = 10;
			private const int PurchaseOrderLineCountLimit = 100;
			private const int WorkOrderCountLimit = 100;
			private const int LocationCountLimit = 30;
			private const int WorkOrderResourceDemandLimit = 100;
			private const int ScheduledWorkOrderCountLimit = 20;
			private const int PMGenerationBatchCountLimit = 20;
			private const int CostCenterCountLimit = 20;
			private void EnforceDemoLimit(DBI_Table t, uint limit, Key countedObjects) {
				LicenseSession.RegisterTableRowCountLimit(t, limit, KB.K("Demonstration mode"), countedObjects, true);
			}
			private static void AddLicenseWarning([Translated]string messageText, System.Text.StringBuilder warningCollector) {
				if (warningCollector == null)
					return;
				if (warningCollector.Length > 0)
					warningCollector.AppendLine();
				warningCollector.Append(messageText);
			}
			public virtual void EnforceDemonstrationLimits(IEnumerable<int> fullyLicensedApplicationIDs, IEnumerable<int> demonstrationApplicationIDs, System.Text.StringBuilder warningsCollector) {
				foreach (int licensingApplicationId in demonstrationApplicationIDs) {
					switch (licensingApplicationId) {
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.NamedUsers:
						EnforceDemoLimit(dsMB.Schema.T.Location, LocationCountLimit, KB.K("Locations"));
						AddLicenseWarning(Strings.Format(KB.K("Activity is limited to {0} Locations of all types."), LocationCountLimit), warningsCollector);
						break;
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.WorkOrderLabor:
						EnforceDemoLimit(dsMB.Schema.T.WorkOrder, WorkOrderCountLimit, KB.K("Work Orders"));
						EnforceDemoLimit(dsMB.Schema.T.Demand, WorkOrderResourceDemandLimit, KB.K("Work Order Resource Demands"));
						AddLicenseWarning(Strings.Format(KB.K("Activity is limited to {0} Work Orders and {1} Resource demands."), WorkOrderCountLimit, WorkOrderResourceDemandLimit), warningsCollector);
						break;
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.Requests:
						EnforceDemoLimit(dsMB.Schema.T.Request, RequestCountLimit, KB.K("Requests"));
						AddLicenseWarning(Strings.Format(KB.K("Activity is limited to {0} requests."), RequestCountLimit), warningsCollector);
						break;
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.Inventory:
						EnforceDemoLimit(dsMB.Schema.T.Item, InventoryCountLimit, KB.K("Items"));
						AddLicenseWarning(Strings.Format(KB.K("Inventory is limited to {0} items."), InventoryCountLimit), warningsCollector);
						break;
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.Purchasing:
						EnforceDemoLimit(dsMB.Schema.T.PurchaseOrder, PurchaseOrderCountLimit, KB.K("Purchase Orders"));
						EnforceDemoLimit(dsMB.Schema.T.POLine, PurchaseOrderLineCountLimit, KB.K("Purchase Order Lines"));
						AddLicenseWarning(Strings.Format(KB.K("Purchasing is limited to {0} purchase orders, and {1} purchase line items."), PurchaseOrderCountLimit, PurchaseOrderLineCountLimit), warningsCollector);
						break;
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.ScheduledMaintenance:
						EnforceDemoLimit(dsMB.Schema.T.ScheduledWorkOrder, ScheduledWorkOrderCountLimit, KB.K("Maintenance Plans"));
						EnforceDemoLimit(dsMB.Schema.T.PMGenerationBatch, PMGenerationBatchCountLimit, KB.K("Generated Maintenance Batches"));
						AddLicenseWarning(Strings.Format(KB.K("Planned maintenance is limited to {0} Maintenance Plans, and only {1} generations of work orders."), ScheduledWorkOrderCountLimit, PMGenerationBatchCountLimit), warningsCollector);
						break;
					case (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.Accounting:
						EnforceDemoLimit(dsMB.Schema.T.CostCenter, CostCenterCountLimit, KB.K("Cost Centers"));
						AddLicenseWarning(Strings.Format(KB.K("Accounting is limited to {0} cost centers."), CostCenterCountLimit), warningsCollector);
						break;
					}
				}
			}
			private static Dictionary<int, MBLicenseDefinition> AllMainbossLicensesByAppId = new Dictionary<int, MBLicenseDefinition>();
		}
		#region The License Definitions
		private class MBLicenseDefinition : LicenseDefinition {
			/// <summary>
			/// Declare a license where the count a licensing model are irrelevant
			/// </summary>
			/// <param name="id">the ApplicationID for the license</param>
			public MBLicenseDefinition(ApplicationID id, bool licenseDeprecated = false)
				: base((int)id, Thinkage.MainBoss.Database.Licensing.ApplicationName(id), null, null, licenseDeprecated, false) {
				TableToCount = null;
				AllowCountingError = false;
			}
			/// <summary>
			/// Declare a license where the count can be checked against a count of non-deleted records in a table (or view)
			/// </summary>
			/// <param name="id">the ApplicationID for the license</param>
			/// <param name="countedItemName">the singular form of the items being counted, for use in messages</param>
			/// <param name="countedItemsName">the plural form of the items being counted, for use in messages</param>
			/// <param name="tableToCount">the table or view whose records are to be counted and compared against the count in the license</param>
			/// <param name="licenseDeprecated">marks the ApplicationID as deprecated to discourage the user from placing such a license into their License table</param>
			/// <param name="allowCountingError">indicates that being over the limit produces only a warning, rather than being a fatal error</param>
			/// <param name="enforceForAnyLicensingModel">indicates that the table count should be enforced for any licensing model (rather than just TableCount); this is for compatibility with old NamedUsers licenses</param>
			public MBLicenseDefinition(ApplicationID id, Thinkage.Libraries.Translation.Key countedItemName, Thinkage.Libraries.Translation.Key countedItemsName, DBI_Table tableToCount, bool licenseDeprecated = false, bool allowCountingError = false, bool enforceForAnyLicensingModel = false)
				: base((int)id, Thinkage.MainBoss.Database.Licensing.ApplicationName(id), countedItemName, countedItemsName, licenseDeprecated, enforceForAnyLicensingModel) {
				TableToCount = tableToCount;
				AllowCountingError = allowCountingError;
			}
			public readonly DBI_Table TableToCount;
			public readonly bool AllowCountingError;
		}
		// The following license the various ways of accessing the MB database.
		// The licenses we have been issuing for NamedUsers have specified Client rather than TableCount as a licensing model, due to past history (where
		// counting sessions or clients were special licensing models). For compatibility we force this license to ignore the licensing model and enforce
		// the count all the time.
		// TODO: We should be issuing Named Users licenses with the Table Count model. This would allow making of EnforceForAnyLicensingModel = false permanent behaviour.
		public static readonly LicenseDefinition NamedUsersLicense = new MBLicenseDefinition(ApplicationID.NamedUsers, KB.K("User"), KB.K("Users"), dsMB.Schema.T.User, enforceForAnyLicensingModel: true);	// TODO: Give is a new App Id.
		public static readonly LicenseDefinition MainBossServiceLicense = new MBLicenseDefinition(ApplicationID.MainBossService);
		public static readonly LicenseDefinition WebAccessLicense = new MBLicenseDefinition(ApplicationID.WebAccess);
		public static readonly LicenseDefinition WebRequestsLicense = new MBLicenseDefinition(ApplicationID.WebRequests);

		// The following license which parts of the database structure are available.
		public static readonly LicenseDefinition RequestsLicense = new MBLicenseDefinition(ApplicationID.Requests, KB.K("Requestor"), KB.K("Requestors"), dsMB.Schema.T.ActiveRequestor);
		public static readonly LicenseDefinition WorkOrderLaborLicense = new MBLicenseDefinition(ApplicationID.WorkOrderLabor);
		public static readonly LicenseDefinition InventoryLicense = new MBLicenseDefinition(ApplicationID.Inventory, KB.K("Storeroom"), KB.K("Storerooms"), dsMB.Schema.T.PermanentStorage);
		public static readonly LicenseDefinition ScheduledMaintenanceLicense = new MBLicenseDefinition(ApplicationID.ScheduledMaintenance);
		public static readonly LicenseDefinition PurchasingLicense = new MBLicenseDefinition(ApplicationID.Purchasing);
		public static readonly LicenseDefinition AccountingLicense = new MBLicenseDefinition(ApplicationID.Accounting);

		#endregion
		#region All licenses used by MainBoss, in enumerable form
		// This is used by the Administration page to show all the license statuses.
		public static readonly LicenseDefinition[] AllMainbossLicenses = {
																				   NamedUsersLicense,
																				   RequestsLicense,
																				   WorkOrderLaborLicense,
																				   InventoryLicense,
																				   ScheduledMaintenanceLicense,
																				   PurchasingLicense,
																				   AccountingLicense,
																				   MainBossServiceLicense,
																				   WebAccessLicense,
																				   WebRequestsLicense
																			   };
		#endregion
		#region License Combination Rules
		// Define what licenses are required for a given license. This is used by the License Updater.
		public static LicenseManager.LicenseRestriction[] Restrictions = {
			// MainBossService allows submission of Requests by email (if Requests are licensed), and sending out progress information
			// on Requests and/or Work Orders, and so requires either Work Orders or Requests to be licensed.
			new LicenseManager.LicenseRestriction(MainBossServiceLicense, WorkOrderLaborLicense, RequestsLicense),
			// The WebRequests license allows Requests to be submitted (and examined ???) via a web interface, and so Requests must be enabled.
			// It could also be argued that the MainBossServiceLicense should be required to the request submitters can get email updates on the progress of their requests.
			new LicenseManager.LicenseRestriction(WebRequestsLicense, RequestsLicense),
			// maybe: new LicenseManager.LicenseRestriction(WebRequestsLicense, MainBossServiceLicense),
			// MainBossWeb allows a user to look at and to a limited extent, modify Requests and Work Orders assigned to them
			new LicenseManager.LicenseRestriction(WebAccessLicense, WorkOrderLaborLicense, RequestsLicense),

			// All other access modes require the desktop app, at least for management.
			// Although Admin mode (which does not need any licenses) can administer the MainBossService Configuration, it cannot actually look at requests, requestors, request assignees, etc.
			new LicenseManager.LicenseRestriction(MainBossServiceLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(WebRequestsLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(WebAccessLicense, NamedUsersLicense),

			// Any of the modules requires the basic desktop app.
			new LicenseManager.LicenseRestriction(RequestsLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(WorkOrderLaborLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(InventoryLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(ScheduledMaintenanceLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(PurchasingLicense, NamedUsersLicense),
			new LicenseManager.LicenseRestriction(AccountingLicense, NamedUsersLicense),

			// Scheduled Maintenance requires Work Orders
			new LicenseManager.LicenseRestriction(ScheduledMaintenanceLicense, WorkOrderLaborLicense),
		};
		#endregion
	}
}
