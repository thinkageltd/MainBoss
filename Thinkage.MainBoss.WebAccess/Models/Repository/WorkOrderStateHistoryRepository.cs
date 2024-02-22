using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.MVC.Models;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebAccess.Models {
	[Serializable]
	public class NotRegisteredAsWorkOrderAssigneeException : System.Exception {
	}

	public class WorkOrderStateHistoryRepository : StateHistoryRepository, IStateHistoryRepository<WorkOrderStateHistoryModel> {
		protected delegate string GetConcurrencyContextMessage(Guid currentState);

		#region Constructor and Base Support
		public WorkOrderStateHistoryRepository()
			: base(dsMB.Schema.T.WorkOrderStateHistory.Name, StateHistoryForm) {
		}
		public override void InitializeDataContext() {
			DataContext = new WorkOrderCloseDataContext(Connection.ConnectionString);
		}
		public WorkOrderCloseDataContext DataContext {
			get;
			private set;
		}
		#endregion
		#region CloseCodePickList
		/// <summary>
		/// Return the available CloseCodes
		/// </summary>
		public SelectListWithEmpty CloseCodePickList(Guid? defaultValue) {
			return PickListWithEmptyOption<WorkOrderCloseEntities.CloseCode>(
						from rp in DataContext.CloseCode
						where rp.Hidden == null
						orderby rp.Code
						select rp,
					defaultValue);
		}
		#endregion

		#region WorkOrderStateHistoryStatusPickList
		/// <summary>
		/// Return the available WorkOrderPriorities
		/// </summary>
		public SelectListWithEmpty WorkOrderStateHistoryStatusPickList(Guid? defaultValue) {
			return PickListWithEmptyOption<WorkOrderCloseEntities.WorkOrderStateHistoryStatus>(
						from rp in DataContext.WorkOrderStateHistoryStatus
						where rp.Hidden == null
						orderby rp.Code
						select rp,
					defaultValue);
		}
		#endregion

		public static Thinkage.Libraries.MVC.Models.FormMap<WorkOrderStateHistoryModel> StateHistoryForm = new Thinkage.Libraries.MVC.Models.FormMap<WorkOrderStateHistoryModel>("Close Work Order",
			FormMapping.New(KB.T("WorkOrderID"), dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID),
			FormMapping.New(KB.T("WorkOrderStateID"), dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID),
			FormMapping.New(KB.T("CurrentStateHistoryID"), dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.Comment),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.StartDateEstimate),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.EndDateEstimate),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Downtime),
			FormMapping.New(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.CloseCodeID));


		#region IStateHistoryRepository<WorkOrderStateHistoryModel> Members
		private void CommonInit(dsMB ds, WorkOrderStateHistoryModel model, IEnumerable<Guid> allowedStates) {
			GetCurrentStateHistory(ds, model.WorkOrderID);
			if (pParentRow.F.CurrentWorkOrderStateHistoryID != model.CurrentStateHistoryID)
				throw new StateHistoryChangedUnderneathUsException(pParentRow.F.Id, pParentRow.F.CurrentWorkOrderStateHistoryID);
			if (!allowedStates.Contains(pCurrentStateHistoryStateRow.F.Id)) {
				throw new ActionNotPermittedForCurrentStateException();
			}
		}
		delegate void OtherInstruction(CustomInstructions instruction);
		private void ProcessCustomInstructions(dsMB ds, WorkOrderStateHistoryModel model, IEnumerable<CustomInstructions> instructions, OtherInstruction other) {
			if (instructions != null) {
				foreach (var instruction in instructions) {
					if (instruction == CustomInstructions.CheckUserIsAssignee) {
						Guid assigneeID = WorkOrderRepository.GetCurrentUserAsWorkOrderAssignee(ds);
						if (assigneeID == Guid.Empty)
							throw new NotRegisteredAsWorkOrderAssigneeException();
					}
					else
						other(instruction);
				}
			}
		}
		public void Prepare(WorkOrderStateHistoryModel model, IEnumerable<Guid> allowedStates, IEnumerable<CustomInstructions> customInstructions, params Libraries.Permissions.Right[] rights) {
			CheckPermission(rights);
			using (dsMB ds = new dsMB(MB3DB)) {
				CommonInit(ds, model, allowedStates);
				GetDefaultStateHistory(ds);
				// Copy the default values to the model now; may change with later processing
				model.Comment = pDefaultStateHistoryRow.F.Comment;
				model.WorkOrderStateHistoryStatusID = pDefaultStateHistoryRow.F.WorkOrderStateHistoryStatusID;
				model.CurrentStatusCode = pDefaultStateHistoryStatusRowCode;

				bool defaultToExisting = false;
				ProcessCustomInstructions(ds, model, customInstructions, (CustomInstructions instruction) => {
					if (instruction == CustomInstructions.SelfAssign) {
						if (model.Comment != null)
							model.Comment += Environment.NewLine;
						else
							model.Comment = "";
						model.Comment += KB.K("Self assigned").Translate();
					}
					if (instruction == CustomInstructions.DefaultToExisting)
						defaultToExisting = true;
				});
				model.WorkOrderNumber = pParentRow.F.Number;
				model.WorkOrderSubject = pParentRow.F.Subject;
				model.WorkOrderDescription = pParentRow.F.Description;
				model.CurrentStateCode = pCurrentStateHistoryStateRow.F.Code;
				model.CurrentStateCodeAsText = pCurrentStateHistoryStateRow.F.Code.Translate();

				// Keep the actual current records values as defaults (overwrite what user may have entered)
				if (defaultToExisting) {
					model.WorkOrderStateHistoryStatusID = pCurrentStateHistoryRow.F.WorkOrderStateHistoryStatusID;
					model.CurrentStatusCode = pCurrentStateHistoryStatusRowCode;
				}
				model.LastComment = pCurrentStateHistoryRow.F.Comment;
				// Copy values that we allow users to change on the closing form into the model that we will concurrency check later
				model.CloseCodeID = pParentRow.F.CloseCodeID;
				model.Downtime = pParentRow.F.Downtime;
				model.EndDateEstimate = pParentRow.F.EndDateEstimate;
				model.StartDateEstimate = pParentRow.F.StartDateEstimate;
				model.WorkOrderStateID = pCurrentStateHistoryRow.F.WorkOrderStateID;
			}
		}

		public Guid Update(WorkOrderStateHistoryModel originalModel, WorkOrderStateHistoryModel updatedModel, Guid? changeToState, IEnumerable<Guid> allowedStates, IEnumerable<CustomInstructions> instructions) {
			Guid newWorkOrderStateHistoryId = Guid.Empty;
			using (dsMB ds = new dsMB(MB3DB)) {
				MB3DB.PerformTransaction(true,
					delegate()
					{
						CommonInit(ds, updatedModel, allowedStates);
						bool closeWorkOrder = false;
						ProcessCustomInstructions(ds, updatedModel, instructions, (CustomInstructions instruction) =>
						{
							if (instruction == CustomInstructions.CloseWorkOrder)
								closeWorkOrder = true;
							if (instruction == CustomInstructions.SelfAssign) {
								Guid assigneeID = WorkOrderRepository.GetCurrentUserAsWorkOrderAssignee(ds);
								if (assigneeID == Guid.Empty)
									throw new NotRegisteredAsWorkOrderAssigneeException();
								ds.EnsureDataTableExists(dsMB.Schema.T.WorkOrderAssignment);
								var assignToWorkOrderRow = ds.T.WorkOrderAssignment.AddNewWorkOrderAssignmentRow();
								assignToWorkOrderRow.F.WorkOrderAssigneeID = assigneeID;
								assignToWorkOrderRow.F.WorkOrderID = updatedModel.WorkOrderID;
							}
						});
						dsMB.WorkOrderStateHistoryRow row = (dsMB.WorkOrderStateHistoryRow)MB3DB.AddNewRowAndBases(ds, dsMB.Schema.T.WorkOrderStateHistory);
						newWorkOrderStateHistoryId = row.F.Id;
						row.F.WorkOrderID = updatedModel.WorkOrderID;
						row.F.UserID = ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).UserID.Value;
						if (closeWorkOrder) {
							pParentRow.F.CloseCodeID = updatedModel.CloseCodeID;
							pParentRow.F.StartDateEstimate = (DateTime)dsMB.Schema.T.WorkOrder.F.StartDateEstimate.EffectiveType.ClosestValueTo(updatedModel.StartDateEstimate);
							pParentRow.F.EndDateEstimate = (DateTime)dsMB.Schema.T.WorkOrder.F.EndDateEstimate.EffectiveType.ClosestValueTo(updatedModel.EndDateEstimate);
							if (pParentRow.F.StartDateEstimate > pParentRow.F.EndDateEstimate)
								throw new Thinkage.Libraries.GeneralException(KB.K("Start Date must be on or before End Date"));
							pParentRow.F.Downtime = (TimeSpan?) dsMB.Schema.T.WorkOrder.F.Downtime.EffectiveType.ClosestValueTo(updatedModel.Downtime);
						}
						row.F.WorkOrderStateHistoryStatusID = updatedModel.WorkOrderStateHistoryStatusID;
						row.F.WorkOrderStateID = changeToState ?? updatedModel.WorkOrderStateID;
						row.F.EffectiveDate = (DateTime)dsMB.Schema.T.WorkOrderStateHistory.F.EffectiveDate.EffectiveType.ClosestValueTo(updatedModel.EffectiveDate);
						row.F.Comment = updatedModel.Comment;

						ds.DB.Update(ds);
					});
			}
			return newWorkOrderStateHistoryId;
		}

		public override System.Collections.Generic.List<StateHistoryRepository.Transition> Transitions {
			get {
				lock (WorkOrderStateHistoryRepository.pTransitions) {
					if (pTransitions.Count == 0) {
						StateHistoryRepository.InitStateInformation(MB3DB, MB3Client.WorkOrderHistoryTable, pTransitions);
					}
					return pTransitions;
				}
			}
		}
		// we keep a static definition so we only execute the filling once. We do not anticipate changes to happen in the StateTransition definitions underneath us.
		static List<Transition> pTransitions = new List<Transition>();

		public void GetCurrentStateHistory(dsMB ds, Guid parentID) {
			ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.WorkOrder, new SqlExpression(dsMB.Path.T.WorkOrder.F.Id).Eq(SqlExpression.Constant(parentID)), null, new DBI_PathToRow[] {
					dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.PathToReferencedRow,
					dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.PathToReferencedRow,  // We want the StateHistory code in our model for possible display
					dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateHistoryStatusID.PathToReferencedRow // and the WorkOrderHistoryStatus code for display
				});
			pParentRow = (dsMB.WorkOrderRow)ds.T.WorkOrder.Rows[0];
			pCurrentStateHistoryRow = (dsMB.WorkOrderStateHistoryRow)ds.T.WorkOrderStateHistory.Rows[0];
			pCurrentStateHistoryStateRow = (dsMB.WorkOrderStateRow)ds.T.WorkOrderState.Rows[0];

			if (ds.T.WorkOrderStateHistoryStatus.Rows.Count == 1)
				pCurrentStateHistoryStatusRowCode = ((dsMB.WorkOrderStateHistoryStatusRow)ds.T.WorkOrderStateHistoryStatus.Rows[0]).F.Code;
			else
				pCurrentStateHistoryStatusRowCode = null;
		}
		private dsMB.WorkOrderRow pParentRow;
		private dsMB.WorkOrderStateHistoryRow pCurrentStateHistoryRow;
		private dsMB.WorkOrderStateRow pCurrentStateHistoryStateRow;
		private string pCurrentStateHistoryStatusRowCode;
		public void GetDefaultStateHistory(dsMB ds) {
			pDefaultStateHistoryRow = (dsMB.WorkOrderStateHistoryRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.WorkOrderStateHistory);
			if (pDefaultStateHistoryRow.F.WorkOrderStateHistoryStatusID.HasValue) {
				// need to get the Code of the default WorkOrderStateHistoryStatus record
				ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.WorkOrderStateHistoryStatus, new SqlExpression(dsMB.Path.T.WorkOrderStateHistoryStatus.F.Id).Eq(SqlExpression.Constant(pDefaultStateHistoryRow.F.WorkOrderStateHistoryStatusID.Value)), null, null);
				pDefaultStateHistoryStatusRowCode = ((dsMB.WorkOrderStateHistoryStatusRow)ds.T.WorkOrderStateHistoryStatus.Rows[0]).F.Code;
			}
		}
		private dsMB.WorkOrderStateHistoryRow pDefaultStateHistoryRow;
		private string pDefaultStateHistoryStatusRowCode;
		#endregion
	}
}
