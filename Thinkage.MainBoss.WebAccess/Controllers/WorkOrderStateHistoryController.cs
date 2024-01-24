using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Thinkage.Libraries;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.MVC.Controllers;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError]
	public class WorkOrderStateHistoryController : StateHistoryController<WorkOrderStateHistoryModel> {
		WorkOrderStateHistoryModel Model;
		protected override WorkOrderStateHistoryModel GetModelForModelStateErrors() {
			if (Model.CloseCodePickList == null)
				Model.CloseCodePickList = NewRepository<WorkOrderStateHistoryRepository>().CloseCodePickList(Model.CloseCodeID);
			if (Model.WorkOrderStateHistoryStatusPickList == null)
				Model.WorkOrderStateHistoryStatusPickList = NewRepository<WorkOrderStateHistoryRepository>().WorkOrderStateHistoryStatusPickList(Model.WorkOrderStateHistoryStatusID);
			return Model;
		}
		#region Common processing
		private WorkOrderStateHistoryRepository InitModel(Guid parentId, Guid currentStateHistoryId) {
			var repository = NewRepository<WorkOrderStateHistoryRepository>();
			Model = new WorkOrderStateHistoryModel() {
				WorkOrderID = parentId,
				CurrentStateHistoryID = currentStateHistoryId,
				EffectiveDate = DateTime.Now
			};
			return repository;
		}
		private delegate ActionResult BuildViewModel(WorkOrderStateHistoryRepository respository);
		private ActionResult GetViewModel(WorkOrderStateHistoryRepository repository) {
			return GetViewModel(repository, ControllerContext.RouteData.GetRequiredString("action"));
		}
		private ActionResult GetViewModel(WorkOrderStateHistoryRepository repository, string view) {
			// Setup Model pickers and other information we expect to display
			Model.CloseCodePickList = repository.CloseCodePickList(Model.CloseCodeID);
			Model.WorkOrderStateHistoryStatusPickList = repository.WorkOrderStateHistoryStatusPickList(Model.WorkOrderStateHistoryStatusID);
			ViewData["ParentXID"] = Model.WorkOrderNumber;
			SetCancelURL();
			return View(view, Model);
		}
		// Redirect locations to goto on Success of an operation
		private ActionResult RedirectTo(string where, Guid requestId) {
			return RedirectToAction(Strings.IFormat("../WorkOrder/{0}", where), new {
				id = requestId,
				resultMessage = ResultMessage
			});

		}
		private ActionResult GETPrepare(Guid parentId, Guid currentStateHistoryId, List<Guid> allowedStates,
			List<StateHistoryRepository.CustomInstructions> instructions,
			BuildViewModel viewModel,
			string transitionRightName,
			Thinkage.Libraries.Translation.Key actionNotPermittedMessage) {

			var repository = InitModel(parentId, currentStateHistoryId);
			try {
				if (transitionRightName != null)
					repository.Prepare(Model, allowedStates, instructions, repository.CreateRight, repository.FindTransitionByName(transitionRightName).ControllingActionRight);
				else
					repository.Prepare(Model, allowedStates, instructions, repository.CreateRight);
			}
			catch (StateHistoryChangedUnderneathUsException e) {
				return DefaultStateHistoryChangedException(e.ParentID, e.NewStateHistoryID);
			}
			catch (ActionNotPermittedForCurrentStateException) {
				ResultMessage = actionNotPermittedMessage.Translate();
				return RedirectTo((string)TempData["RedirectOnError"], parentId);
			}
			return viewModel(repository);
		}
		private ActionResult POSTUpdate(FormCollectionWithType collection, Guid? ToState, List<Guid> allowedStates, List<StateHistoryRepository.CustomInstructions> instructions,
			Thinkage.Libraries.Translation.Key actionNotPermittedMessage) {
			var parentID = new System.Guid(collection["WorkOrderID"]);
			try {
				var repository = NewRepository<WorkOrderStateHistoryRepository>();
				Model = new WorkOrderStateHistoryModel();
				repository.Update(null, UpdateFromForm(Model, collection), ToState, allowedStates, instructions);
			}
			catch (StateHistoryChangedUnderneathUsException e) {
				return DefaultStateHistoryChangedException(e.ParentID, e.NewStateHistoryID);
			}
			catch (ActionNotPermittedForCurrentStateException) {
				ResultMessage = actionNotPermittedMessage.Translate();
			}
			catch (System.Exception e) {
				InterpretException(e);
				return RedirectBack(new System.Guid(collection["WorkOrderID"]), new System.Guid(collection["CurrentStateHistoryID"]));
			}
			RemoveCancelURL();
			return RedirectTo((string)TempData["RedirectOnSuccess"], parentID);
		}
		#endregion

		#region Close
		static List<Guid> AllowedToCloseStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.WorkOrderStateOpenId });
		#region Close (GET)
		// GET: /WorkOrderStateHistory/Close
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult Close(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "View";
			return GETPrepare(parentId, currentStateHistoryId, AllowedToCloseStates, CloseStateInstructions, (r) => {
				return GetViewModel(r);
			},
			Thinkage.MainBoss.Database.DatabaseCreation.CloseWorkOrderAction,
			KB.K("You cannot change the work order to Closed from its current state"));
		}
		#endregion
		#region Close (POST)
		//
		// POST: /WorkOrderStateHistory/Close
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		public ActionResult Close(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "Index";
			return POSTUpdate(collection, Thinkage.MainBoss.Database.KnownIds.WorkOrderStateClosedId, AllowedToCloseStates, CloseStateInstructions, KB.K("You cannot close the work order since it is already closed"));
		}
		#endregion
		#endregion

		#region Open
		#region Open (GET)
		static List<Guid> CanChangeToOpenStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.WorkOrderStateDraftId });

		// GET: /WorkOrderStateHistory/Open
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult Open(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "Index";
			return GETPrepare(parentId, currentStateHistoryId, CanChangeToOpenStates, MainBossUserChangeStateInstructions, (r) => {
				return GetViewModel(r);
			},
			Thinkage.MainBoss.Database.DatabaseCreation.OpenWorkOrderAction,
			KB.K("You cannot change the work order to Open from its current state"));
		}
		#endregion
		#region Open (POST)
		//
		// POST: /WorkOrderStateHistory/Close
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		public ActionResult Open(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "Index";
			return POSTUpdate(collection, Thinkage.MainBoss.Database.KnownIds.WorkOrderStateOpenId, CanChangeToOpenStates, MainBossUserChangeStateInstructions,
				KB.K("You cannot change the work order to Open from its current state"));
		}
		#endregion
		#endregion

		#region AddComment
		// Common GetAddComment processing
		static List<Guid> CanCommentStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.WorkOrderStateDraftId, Thinkage.MainBoss.Database.KnownIds.WorkOrderStateOpenId });
		static List<StateHistoryRepository.CustomInstructions> MainBossUserChangeStateInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.DefaultToExisting
		});
		static List<StateHistoryRepository.CustomInstructions> CloseStateInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.CloseWorkOrder
		});
		#region AddComment (GET)
		//
		// GET: /WorkOrderStateHistory/AddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult AddComment(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "View";
			return GETPrepare(parentId, currentStateHistoryId, CanCommentStates, MainBossUserChangeStateInstructions, (r) => {
				return GetViewModel(r, "AddComment");
			},
			null,
			KB.K("You cannot add a comment to the work order in its current state"));
		}
		// GET: /WorkOrderStateHistory/UnAssignedAddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult UnAssignedAddComment(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "ViewUnAssigned";
			return GETPrepare(parentId, currentStateHistoryId, CanCommentStates, MainBossUserChangeStateInstructions, (r) => {
				return GetViewModel(r, "AddComment");
			},
			null,
			KB.K("You cannot add a comment to the work order in its current state"));
		}
		#endregion
		#region AddComment (POST)
		//
		// POST: /WorkOrderStateHistory/AddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		public ActionResult AddComment(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "View";
			return POSTUpdate(collection, null, CanCommentStates, MainBossUserChangeStateInstructions, KB.K("You cannot add a comment to the work order in its current state"));
		}
		// POST: /WorkOrderStateHistory/UnAssignedAddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		public ActionResult UnAssignedAddComment(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "ViewUnAssigned";
			return POSTUpdate(collection, null, CanCommentStates, MainBossUserChangeStateInstructions, KB.K("You cannot add a comment to the work order in its current state"));
		}
		#endregion
		#endregion

		#region SelfAssign
		static List<StateHistoryRepository.CustomInstructions> SelfAssignInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.DefaultToExisting,
			StateHistoryRepository.CustomInstructions.FullUpdate,
			StateHistoryRepository.CustomInstructions.SelfAssign
		});
		#region SelfAssign (GET)
		//
		// GET: /WorkOrderStateHistory/SelfAssign
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult SelfAssign(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "UnAssigned";
			return GETPrepare(parentId, currentStateHistoryId, CanCommentStates, SelfAssignInstructions, (r) => {
				return GetViewModel(r);
			},
			null,
			KB.K("You cannot self assign the work order in its current state"));
		}
		#endregion
		#region SelfAssign (POST)
		//
		// POST: /WorkOrderStateHistory/SelfAssign
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		public ActionResult SelfAssign(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "UnAssigned";
			return POSTUpdate(collection, Thinkage.MainBoss.Database.KnownIds.WorkOrderStateOpenId, CanCommentStates, SelfAssignInstructions,
				KB.K("You cannot self assign the work order in its current state"));
		}
		#endregion
		#endregion
	}
}
