using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess.Models.interfaces;
using WOResourceEntities;

namespace Thinkage.MainBoss.WebAccess.Models {
	#region UnAssignedWorkOrderRepository
	public class UnAssignedWorkOrderRepository : WorkOrderBaseRepository {
		public UnAssignedWorkOrderRepository()
			: base("UnassignedWorkOrder") {
		}
	}
	#endregion
	#region WorkOrderRepository
	public class WorkOrderRepository : WorkOrderBaseRepository {
		public WorkOrderRepository()
			: base("AssignedWorkOrder") {
		}
	}
	#endregion

	public class WorkOrderBaseRepository : BaseRepository, IBaseRepository<WorkOrderDataContext>, IBrowse<WorkOrderEntities.WorkOrder>, IDisposable {
		#region WorkOrderBaseRepository

		/// <summary>
		/// The right required to Actualize resources
		/// </summary>
		private static Right ActualizeRight {
			get {
				return GetTableRightsGroup(KB.I("ActualItem")).GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create);
			}
		}
		/// <summary>
		/// return null if Actualization can be done; otherwise the permission message as to why not. This allows the View to display a 
		/// message as to why the operation cannot be accomplished.
		/// </summary>
		public string CantActualizeBecause {
			get {
				var pd = (MainBossPermissionDisabler)PermissionsManager.GetPermission(ActualizeRight);
				return pd.Enabled ? null : pd.DisablerTip;
			}
		}

		public delegate void AddActualizeError([Invariant] string propertyName, System.Exception e, System.Web.Mvc.ValueProviderResult value);

		#region Constructor and Base Support
		public WorkOrderBaseRepository(string governingTableRight)
			: base(governingTableRight) {
		}
		#endregion

		#region IDisposable Members
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (pWOResourcesDC != null) {
					pWOResourcesDC.Dispose();
					pWOResourcesDC = null;
				}
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
		#region IBaseRepository<WorkOrderDataContext>
		public WorkOrderDataContext DataContext {
			get;
			private set;
		}
		public override void InitializeDataContext() {
			DataContext = new WorkOrderDataContext(Connection.ConnectionString);
		}
		public WOResourceDataContext WOResourcesDC {
			get {
				if (pWOResourcesDC == null)
					pWOResourcesDC = new WOResourceDataContext(Connection.ConnectionString);
				return pWOResourcesDC;
			}
		}
		#endregion
		private WOResourceDataContext pWOResourcesDC;
		#region IBrowse<WorkOrderEntities.WorkOrder>
		public IQueryable<WorkOrderEntities.WorkOrder> BrowseUnAssigned() {
			var q = from unassigned in DataContext.WorkOrderAssignmentByAssignee
					join WorkOrder in DataContext.WorkOrder on unassigned.WorkOrderID equals WorkOrder.Id
					join WorkOrderPriorities in DataContext.WorkOrderPriority on WorkOrder.WorkOrderPriorityID equals WorkOrderPriorities.Id into ugj2
					from RP in ugj2.DefaultIfEmpty()
					join SH in DataContext.WorkOrderStateHistory on WorkOrder.CurrentWorkOrderStateHistoryID equals SH.Id
					join WS in DataContext.WorkOrderState on SH.WorkOrderStateID equals WS.Id
					join Status in DataContext.WorkOrderStateHistoryStatus on SH.WorkOrderStateHistoryStatusID equals Status.Id into ugj3
					from WSTATUS in ugj3.DefaultIfEmpty()
					where unassigned.ContactID == UnassignedGuid && (WS.FilterAsOpen == true || WS.FilterAsDraft == true)
					orderby RP == null ? 999999 : RP.Rank, WorkOrder.Number
					select WorkOrder;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
		}
		/// <summary>
		///  Return just workorders that are assigned to the assigneeID
		/// </summary>
		public IQueryable<WorkOrderEntities.WorkOrder> BrowseAssigned() {
			Guid contactID = ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).ContactID.Value;

			return from wo in DataContext.WorkOrder
				   join Unit in DataContext.Location on wo.UnitLocationID equals Unit.Id into ugj1
				   from U in ugj1.DefaultIfEmpty()
				   join WorkOrderPriorities in DataContext.WorkOrderPriority on wo.WorkOrderPriorityID equals WorkOrderPriorities.Id into ugj2
				   from RP in ugj2.DefaultIfEmpty()
				   join SH in DataContext.WorkOrderStateHistory on wo.CurrentWorkOrderStateHistoryID equals SH.Id
				   join RS in DataContext.WorkOrderState on SH.WorkOrderStateID equals RS.Id
				   join assignment in DataContext.WorkOrderAssignmentAll on wo.Id equals assignment.WorkOrderID
				   join assignee in DataContext.WorkOrderAssignee on assignment.WorkOrderAssigneeID equals assignee.Id
				   where assignee.ContactID == contactID && RS.FilterAsOpen == true
				   orderby RP == null ? 9999999 : RP.Rank, wo.Number
				   select wo;
		}
		public WorkOrderEntities.WorkOrder View(Guid Id) {
			// We may end up viewing a Closed WorkOrder (someone closed it underneath the currently user when they were trying to Add a comment or close it themselves)
			// so we need to look in all requests for the Id given regardless of its state
			try {
				return (from wo in DataContext.WorkOrder
						where wo.Id == Id
						select wo).Single();
			}
			catch (InvalidOperationException) {
				return new WorkOrderEntities.WorkOrder() {
					Id = Id,
					Number = Thinkage.Libraries.Strings.Format(KB.K("The work order with Id '{0}' was not found."), Id)
				};
			}
		}
		#endregion

		public static bool CanSelfAssign() {
			using (dsMB ds = new dsMB(MB3DB)) {
				return GetCurrentUserAsWorkOrderAssignee(ds) != Guid.Empty;
			}
		}

		public static Guid GetCurrentUserAsWorkOrderAssignee(dsMB ds) {
			// Determine (and return if found) the ID of the current user as a WorkOrderAssignee so that it may be used as a WorkOrderAssignment to the current work order
			ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.WorkOrderAssignee, new SqlExpression(dsMB.Path.T.WorkOrderAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).UserID.Value)),
				null, new DBI_PathToRow[] {
				dsMB.Path.T.WorkOrderAssignee.F.ContactID.PathToReferencedRow
				});
			if (ds.T.WorkOrderAssignee.Rows.Count == 0)
				return Guid.Empty;
			else
				return (((dsMB.WorkOrderAssigneeRow)ds.T.WorkOrderAssignee.Rows[0]).F.Id);
		}

		#region WOResources
		private class DerivedDemand<T> where T : IDerivedDemand {
			public DerivedDemand() {
			}
			public T Demand {
				get;
				set;
			}
			public Guid ToCostCenter {
				get;
				set;
			}
		}

		private IEnumerable<DerivedDemand<T>> DerivedDemands<T>(Guid parentId, System.Data.Linq.Table<T> derivedDemands) where T : class, IDerivedDemand {
			return from d in derivedDemands
				   join Demand in WOResourcesDC.Demand on d.DemandID equals Demand.Id
				   join DWOXP in WOResourcesDC.DemandWorkOrderExpenseModelEntry on Demand.Id equals DWOXP.Id
				   join WOXP in WOResourcesDC.WorkOrderExpenseModelEntry on DWOXP.WorkOrderExpenseModelEntryID equals WOXP.Id
				   where Demand.WorkOrderID == parentId
				   select new DerivedDemand<T> {
					   Demand = d,
					   ToCostCenter = WOXP.CostCenterID
				   };
		}
		public IEnumerable<IWOResourceInfo> ResourceInfos(Guid parentId) {
			return
				((from di in DerivedDemands<WOResourceEntities.DemandItem>(parentId, WOResourcesDC.DemandItem)
				  select new ItemWOResourceInfo(di.Demand, di.ToCostCenter) as IWOResourceInfo).Union(
				 ((from di in DerivedDemands<WOResourceEntities.DemandLaborInside>(parentId, WOResourcesDC.DemandLaborInside)
				   select new HourlyWOResourceInfo(di.Demand, di.ToCostCenter) as IWOResourceInfo))).Union(
				((from di in DerivedDemands<WOResourceEntities.DemandOtherWorkInside>(parentId, WOResourcesDC.DemandOtherWorkInside)
				  select new PerJobWOResourceInfo(di.Demand, di.ToCostCenter) as IWOResourceInfo).Union(
				((from di in DerivedDemands<WOResourceEntities.DemandMiscellaneousWorkOrderCost>(parentId, WOResourcesDC.DemandMiscellaneousWorkOrderCost)
				  select new MiscellaneousWOResourceInfo(di.Demand, di.ToCostCenter) as IWOResourceInfo))))));
		}
		public IEnumerable<WOResource> Resources(Guid parentId) {
			return Resources(ResourceInfos(parentId));
		}
		private static IEnumerable<WOResource> Resources(IEnumerable<IWOResourceInfo> ri) {
			return ri.Select(p => p.MakeWOResource());
		}
		public void ActualizeResources(Guid ParentId, WorkOrderEntities.WorkOrder originalModel, IDictionary<Guid, System.Web.Mvc.ValueProviderResult> toActualize, AddActualizeError processError) {
			using (dsMB ds = new dsMB(MB3DB)) {
				MB3DB.PerformTransaction(true,
					delegate()
					{
						// Build set of new demands while under transaction to compare against originals and build actuals
						var resourceInfos = new List<IWOResourceInfo>(ResourceInfos(ParentId));
						var woResources = new List<WOResource>(Resources(resourceInfos));

						bool errors = false;
						// First see if all the input values are parseable; if not, we don't do anything.
						foreach (var kvp in toActualize) {
							try {
								var originalResource = originalModel.Resources.Find(r => r.Id == kvp.Key);
								var woResource = woResources.Find(r => r.Id == kvp.Key);
								// Determine if values changed since the user was first given
								if (originalResource == null)
									throw new GeneralException(KB.K("Resource was never available for actualization")); // this should never happen
								if (woResource == null) {
									woResources.Add(originalResource); // put back the reference that was deleted 
									throw new GeneralException(KB.K("Resource is no longer present for actualization"));
								}

								if (originalResource.Quantity != woResource.Quantity)
									throw new GeneralException(KB.K("Quantity has been changed before your actualization submission"));
								if (originalResource.ActualQuantity != woResource.ActualQuantity)
									throw new GeneralException(KB.K("Actual Quantity has been changed before your actualization submission"));

								// Now try to create the actualization record
								var resourceInfo = resourceInfos.Find(r => r.Id == kvp.Key);
								if (kvp.Value != null)
									resourceInfo.SetQuantityToActualize(kvp.Value.AttemptedValue);
								resourceInfo.Actualize(ds); // might as well build the records now too
							}
							catch (System.Exception e) {
								string inputId = "Input_" + kvp.Key.ToString();
								processError(inputId, e, kvp.Value);
								errors = true;
							}
						}
						originalModel.Resources = woResources; // work from the new set in event we had an error.
						if (errors)
							return;
						MB3DB.Update(ds);
					});
			}
		}
		#endregion

	}
	#endregion
}
