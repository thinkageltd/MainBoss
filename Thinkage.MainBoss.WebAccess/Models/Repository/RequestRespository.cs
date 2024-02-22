using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess.Models.interfaces;

namespace Thinkage.MainBoss.WebAccess.Models {
	#region UnAssignedRequestRepository
	public class UnAssignedRequestRepository : RequestBaseRepository {
		public UnAssignedRequestRepository()
			: base("UnassignedRequest") {
		}
	}
	#endregion
	#region RequestRepository
	public class RequestRepository : RequestBaseRepository {
		public RequestRepository()
			: base("AssignedRequest") {
		}
	}
	#endregion
	#region RequestBaseRepository
	public class RequestBaseRepository : BaseRepository, IBaseRepository<RequestDataContext>, IBrowse<RequestEntities.Request> {
		#region Constructor and Base Support
		public RequestBaseRepository(string governingTableRight)
			: base(governingTableRight) {
#if NOTNEEDED
			System.Data.Linq.DataLoadOptions dlo = new System.Data.Linq.DataLoadOptions();
			dlo.LoadWith<RequestEntities.Request>(r => r.RequestStateHistoryRequest);
			dlo.LoadWith<RequestEntities.Request>(r => r.UnitLocation);
			dlo.LoadWith<RequestEntities.Request>(r => r.RequestPriority);
			dlo.LoadWith<RequestEntities.Request>(r => r.AccessCode);
			dlo.LoadWith<RequestEntities.Request>(r => r.RequestAssignmentRequest);

			dlo.LoadWith<RequestEntities.RequestStateHistory>(rsh => rsh.User);
			dlo.LoadWith<RequestEntities.User>(u => u.ContactUser);

			dlo.LoadWith<RequestEntities.RequestStateHistory>(rsh => rsh.RequestState);
			dlo.LoadWith<RequestEntities.RequestStateHistory>(rsh => rsh.RequestStateHistoryStatus);

			dlo.LoadWith<RequestEntities.RequestAssignment>(ra => ra.RequestAssignee);
			dlo.LoadWith<RequestEntities.RequestAssignee>(ra => ra.Contact);
			DataContext.LoadOptions = dlo;
#endif
		}
		#endregion
		#region IBaseRepository<RequestDataContext>
		public override void InitializeDataContext() {
			DataContext = new RequestDataContext(Connection.ConnectionString);
		}
		public RequestDataContext DataContext {
			get;
			private set;
		}
		#endregion
		#region IBrowse<Request.Request> Members

		public IQueryable<RequestEntities.Request> BrowseUnAssigned() {
			var q = from unassigned in DataContext.RequestAssignmentByAssignee
					join request in DataContext.Request on unassigned.RequestID equals request.Id
					join RequestPriorities in DataContext.RequestPriority on request.RequestPriorityID equals RequestPriorities.Id into ugj2
					from RP in ugj2.DefaultIfEmpty()
					join SH in DataContext.RequestStateHistory on request.CurrentRequestStateHistoryID equals SH.Id
					join RS in DataContext.RequestState on SH.RequestStateID equals RS.Id
					join Status in DataContext.RequestStateHistoryStatus on SH.RequestStateHistoryStatusID equals Status.Id into ugj3
					from RSTATUS in ugj3.DefaultIfEmpty()
					where unassigned.ContactID == UnassignedGuid && (RS.FilterAsInProgress == true || RS.FilterAsNew == true)
					orderby RP == null ? 999999 : RP.Rank, request.Number
					select request;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
		}

		/// <summary>
		///  Return just requests that are assigned to the assigneeID
		/// </summary>
		public IQueryable<RequestEntities.Request> BrowseAssigned() {
			Guid contactID = ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).ContactID.Value;

			var q = from request in DataContext.Request
					join RequestPriorities in DataContext.RequestPriority on request.RequestPriorityID equals RequestPriorities.Id into ugj2
					from RP in ugj2.DefaultIfEmpty()
					join SH in DataContext.RequestStateHistory on request.CurrentRequestStateHistoryID equals SH.Id
					join RS in DataContext.RequestState on SH.RequestStateID equals RS.Id
					join Status in DataContext.RequestStateHistoryStatus on SH.RequestStateHistoryStatusID equals Status.Id into ugj3
					from RSTATUS in ugj3.DefaultIfEmpty()
					join assignment in DataContext.RequestAssignment on request.Id equals assignment.RequestID
					join assignee in DataContext.RequestAssignee on assignment.RequestAssignee.Id equals assignee.Id
					where assignee.ContactID == contactID && RS.FilterAsInProgress == true
					orderby RP == null ? 9999999 : RP.Rank, request.Number
					select request;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
		}
		public RequestEntities.Request View(Guid Id) {
			// We may end up viewing a Closed Request (someone closed it underneath the currently user when they were trying to Add a comment or close it themselves)
			// so we need to look in all requests for the Id given regardless of its state
			try {
				return (from request in DataContext.Request
						where request.Id == Id
						select request).Single(); // InvalidOperationException if record not found
			}
			catch (InvalidOperationException) {
				return new RequestEntities.Request() {
					Id = Id,
					Number = Thinkage.Libraries.Strings.Format(KB.K("The request with Id '{0}' was not found."), Id)
				};
			}
		}
		#endregion
		public bool CanSelfAssign() {
			using (dsMB ds = new dsMB(MB3DB)) {
				return RequestRepository.GetCurrentUserAsRequestAssignee(ds) != Guid.Empty;
			}
		}
		public static Guid GetCurrentUserAsRequestAssignee(dsMB ds) {
			// Determine (and return if found) the ID of the current user as a RequestAssignee so that it may be used as a RequestAssignment to the current request
			ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.RequestAssignee, new SqlExpression(dsMB.Path.T.RequestAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).UserID.Value)),
				null, new DBI_PathToRow[] {
				dsMB.Path.T.RequestAssignee.F.ContactID.PathToReferencedRow
				});
			if (ds.T.RequestAssignee.Rows.Count == 0)
				return Guid.Empty;
			else
				return (((dsMB.RequestAssigneeRow)ds.T.RequestAssignee.Rows[0]).F.Id);
		}
	}
	#endregion
}
