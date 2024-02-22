using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.MVC.Models;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebAccess.Models {
	[Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable CA2229 // Implement standard exception constructors

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "No need for other constructors")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "No need for other constructors")]
	public class NotCorrectRequestorForCommentException : System.Exception {
	}
	[Serializable]
	public class NotRegisteredAsRequestAssigneeException : System.Exception {
	}
#pragma warning restore CA1032 // Implement standard exception constructors
#pragma warning restore CA2229 // Implement standard exception constructors
	public class RequestStateHistoryRepository : StateHistoryRepository, IStateHistoryRepository<RequestStateHistoryModel> {
		protected delegate string GetConcurrencyContextMessage(Guid currentState);

		#region Constructor and Base Support
		public RequestStateHistoryRepository()
			: base(dsMB.Schema.T.RequestStateHistory.Name, StateHistoryForm) {
		}
		public override void InitializeDataContext() {
			DataContext = new RequestDataContext(Connection.ConnectionString);
		}
		public RequestDataContext DataContext {
			get;
			private set;
		}
		#endregion
		#region RequestStateHistoryStatusPickList
		/// <summary>
		/// Return the available RequestPriorities
		/// </summary>
		public SelectListWithEmpty RequestStateHistoryStatusPickList(Guid? defaultValue) {
			return PickListWithEmptyOption<RequestEntities.RequestStateHistoryStatus>(
						from rp in DataContext.RequestStateHistoryStatus
						where rp.Hidden == null
						orderby rp.Code
						select rp,
					defaultValue);
		}
		#endregion

		public static Thinkage.Libraries.MVC.Models.FormMap<RequestStateHistoryModel> StateHistoryForm = new Thinkage.Libraries.MVC.Models.FormMap<RequestStateHistoryModel>("Close Request",
			FormMapping.New(KB.T("RequestID"), dsMB.Path.T.RequestStateHistory.F.RequestID),
			FormMapping.New(KB.T("RequestStateID"), dsMB.Path.T.RequestStateHistory.F.RequestStateID),
			FormMapping.New(KB.T("CurrentStateHistoryID"), dsMB.Path.T.RequestStateHistory.F.RequestID.F.CurrentRequestStateHistoryID),
			FormMapping.New(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID),
			FormMapping.New(dsMB.Path.T.RequestStateHistory.F.EffectiveDate),
			FormMapping.New(dsMB.Path.T.RequestStateHistory.F.PredictedCloseDate),
			FormMapping.New(dsMB.Path.T.RequestStateHistory.F.CommentToRequestor),
			FormMapping.New(dsMB.Path.T.RequestStateHistory.F.Comment),
			FormMapping.New(KB.K("Email Address"), KB.I("EmailAddress"), dsMB.Path.T.Contact.F.Email.ReferencedColumn.EffectiveType.IntersectCompatible(Thinkage.Libraries.TypeInfo.StringTypeInfo.NonNullUniverse)));

		#region IStateHistoryRepository<RequestStateHistoryModel> Members
		private void CommonInit(dsMB ds, RequestStateHistoryModel model, IEnumerable<Guid> allowedStates) {
			GetCurrentStateHistory(ds, model.RequestID);
			if (pParentRow.F.CurrentRequestStateHistoryID != model.CurrentStateHistoryID)
				throw new StateHistoryChangedUnderneathUsException(pParentRow.F.Id, pParentRow.F.CurrentRequestStateHistoryID);
			if (!allowedStates.Contains(pCurrentStateHistoryStateRow.F.Id)) {
				throw new ActionNotPermittedForCurrentStateException();
			}
		}

		private delegate void OtherInstruction(CustomInstructions instruction);
		private void ProcessCustomInstructions(dsMB ds, RequestStateHistoryModel model, IEnumerable<CustomInstructions> instructions, OtherInstruction other) {
			if (instructions != null) {
				foreach (var instruction in instructions) {
					if (instruction == CustomInstructions.CheckRequestorID && (model.RequestorID.HasValue && model.RequestorID != pParentRow.F.RequestorID))
						throw new NotCorrectRequestorForCommentException();
					else if (instruction == CustomInstructions.CheckUserIsAssignee) {
						Guid assigneeID = RequestRepository.GetCurrentUserAsRequestAssignee(ds);
						if (assigneeID == Guid.Empty)
							throw new NotRegisteredAsRequestAssigneeException();
					}
					else
						other(instruction);
				}
			}
		}
		public void Prepare(RequestStateHistoryModel model, IEnumerable<Guid> allowedStates, IEnumerable<CustomInstructions> customInstructions, params Libraries.Permissions.Right[] rights) {
			CheckPermission(rights);
			using (dsMB ds = new dsMB(MB3DB)) {
				CommonInit(ds, model, allowedStates);
				GetDefaultStateHistory(ds);
				// Copy the default values to the model now; may change with later processing
				model.Comment = pDefaultStateHistoryRow.F.Comment;
				model.CommentToRequestor = pDefaultStateHistoryRow.F.CommentToRequestor;
				model.RequestStateHistoryStatusID = pDefaultStateHistoryRow.F.RequestStateHistoryStatusID;
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
				model.RequestNumber = pParentRow.F.Number;
				model.RequestSubject = pParentRow.F.Subject;
				model.RequestDescription = pParentRow.F.Description;
				model.CurrentStateCode = pCurrentStateHistoryStateRow.F.Code;

				// Keep the actual current records values as defaults (overwrite what user may have entered)
				if (defaultToExisting) {
					model.PredictedCloseDate = pCurrentStateHistoryRow?.F.PredictedCloseDate;
					model.RequestStateHistoryStatusID = pCurrentStateHistoryRow.F.RequestStateHistoryStatusID;
					model.CurrentStatusCode = pCurrentStateHistoryStatusRowCode;
				}
				model.LastComment = pCurrentStateHistoryRow.F.Comment;
				model.LastRequestorComment = pCurrentStateHistoryRow.F.CommentToRequestor;
				model.RequestStateID = pCurrentStateHistoryRow.F.RequestStateID;
			}
		}

		public Guid Update(RequestStateHistoryModel originalModel, RequestStateHistoryModel updatedModel, Guid? changeToState, IEnumerable<Guid> allowedStates, IEnumerable<CustomInstructions> instructions) {
			Guid newRequestStateHistoryId = Guid.Empty;
			using (dsMB ds = new dsMB(MB3DB)) {
				MB3DB.PerformTransaction(true,
					delegate () {
						CommonInit(ds, updatedModel, allowedStates);
						bool fullUpdate = false;
						ProcessCustomInstructions(ds, updatedModel, instructions, (CustomInstructions instruction) => {
							if (instruction == CustomInstructions.FullUpdate)
								fullUpdate = true;
							if (instruction == CustomInstructions.SelfAssign) {
								Guid assigneeID = RequestRepository.GetCurrentUserAsRequestAssignee(ds);
								if (assigneeID == Guid.Empty)
									throw new NotRegisteredAsRequestAssigneeException();
								ds.EnsureDataTableExists(dsMB.Schema.T.RequestAssignment);
								var assignToRequestRow = ds.T.RequestAssignment.AddNewRow();
								assignToRequestRow.F.RequestAssigneeID = assigneeID;
								assignToRequestRow.F.RequestID = updatedModel.RequestID;
							}
						});
						dsMB.RequestStateHistoryRow row = (dsMB.RequestStateHistoryRow)MB3DB.AddNewRowAndBases(ds, dsMB.Schema.T.RequestStateHistory);
						newRequestStateHistoryId = row.F.Id;
						row.F.RequestID = updatedModel.RequestID;
						// Requestors adding comments (not FullUpdate) don't have UserId values
						if (fullUpdate) {
							row.F.UserID = ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).UserID.Value;
							if (updatedModel.PredictedCloseDate.HasValue)
								row.F.PredictedCloseDate = (DateTime)dsMB.Schema.T.RequestStateHistory.F.PredictedCloseDate.EffectiveType.ClosestValueTo(updatedModel.PredictedCloseDate);
						}
						row.F.RequestStateHistoryStatusID = updatedModel.RequestStateHistoryStatusID;
						row.F.RequestStateID = changeToState ?? updatedModel.RequestStateID;
						row.F.EffectiveDate = (DateTime)dsMB.Schema.T.RequestStateHistory.F.EffectiveDate.EffectiveType.ClosestValueTo(updatedModel.EffectiveDate);
						row.F.CommentToRequestor = updatedModel.CommentToRequestor;
						row.F.Comment = updatedModel.Comment;

						ds.DB.Update(ds);
					});
			}
			return newRequestStateHistoryId;
		}

		public override System.Collections.Generic.List<StateHistoryRepository.Transition> Transitions {
			get {
				lock (RequestStateHistoryRepository.pTransitions) {
					if (pTransitions.Count == 0) {
						StateHistoryRepository.InitStateInformation(MB3DB, MB3Client.RequestHistoryTable, pTransitions);
					}
					return pTransitions;
				}
			}
		}

		// we keep a static definition so we only execute the filling once. We do not anticipate changes to happen in the StateTransition definitions underneath us.
		private static readonly List<Transition> pTransitions = new List<Transition>();

		public void GetCurrentStateHistory(dsMB ds, Guid parentID) {
			ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.Request, new SqlExpression(dsMB.Path.T.Request.F.Id).Eq(SqlExpression.Constant(parentID)), null, new DBI_PathToRow[] {
					dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.PathToReferencedRow,
					dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.PathToReferencedRow,  // We want the StateHistory code in our model for possible display
					dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID.PathToReferencedRow // and the RequestHistoryStatus code for display
				});
			pParentRow = (dsMB.RequestRow)ds.T.Request.Rows[0];
			pCurrentStateHistoryRow = (dsMB.RequestStateHistoryRow)ds.T.RequestStateHistory.Rows[0];
			pCurrentStateHistoryStateRow = (dsMB.RequestStateRow)ds.T.RequestState.Rows[0];

			if (ds.T.RequestStateHistoryStatus.Rows.Count == 1)
				pCurrentStateHistoryStatusRowCode = ((dsMB.RequestStateHistoryStatusRow)ds.T.RequestStateHistoryStatus.Rows[0]).F.Code;
			else
				pCurrentStateHistoryStatusRowCode = null;
		}
		private dsMB.RequestRow pParentRow;
		private dsMB.RequestStateHistoryRow pCurrentStateHistoryRow;
		private dsMB.RequestStateRow pCurrentStateHistoryStateRow;
		private string pCurrentStateHistoryStatusRowCode;
		public void GetDefaultStateHistory(dsMB ds) {
			pDefaultStateHistoryRow = (dsMB.RequestStateHistoryRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.RequestStateHistory);
			if (pDefaultStateHistoryRow.F.RequestStateHistoryStatusID.HasValue) {
				// need to get the Code of the default RequestStateHistoryStatus record
				ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.RequestStateHistoryStatus, new SqlExpression(dsMB.Path.T.RequestStateHistoryStatus.F.Id).Eq(SqlExpression.Constant(pDefaultStateHistoryRow.F.RequestStateHistoryStatusID.Value)), null, null);
				pDefaultStateHistoryStatusRowCode = ((dsMB.RequestStateHistoryStatusRow)ds.T.RequestStateHistoryStatus.Rows[0]).F.Code;
			}
		}
		private dsMB.RequestStateHistoryRow pDefaultStateHistoryRow;
		private string pDefaultStateHistoryStatusRowCode;
		#endregion
	}
}
