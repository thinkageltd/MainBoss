using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Thinkage.Libraries;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.MVC.Controllers;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError]
	public class RequestStateHistoryController : StateHistoryController<RequestStateHistoryModel> {
		private RequestStateHistoryModel Model;

		protected override RequestStateHistoryModel GetModelForModelStateErrors() {
			if (Model.RequestStateHistoryStatusPickList == null)
				Model.RequestStateHistoryStatusPickList = NewRepository<RequestStateHistoryRepository>().RequestStateHistoryStatusPickList(Model.RequestStateHistoryStatusID);
			return Model;
		}
		#region Common processing
		private RequestStateHistoryRepository InitModel(Guid parentId, Guid currentStateHistoryId) {
			var repository = NewRepository<RequestStateHistoryRepository>();
			Model = new RequestStateHistoryModel() {
				RequestID = parentId,
				CurrentStateHistoryID = currentStateHistoryId,
				EffectiveDate = DateTime.Now
			};
			return repository;
		}
		private delegate ActionResult BuildViewModel(RequestStateHistoryRepository respository);
		private ActionResult GetViewModel(RequestStateHistoryRepository repository) {
			return GetViewModel(repository, ControllerContext.RouteData.GetRequiredString("action"));
		}
		private ActionResult GetViewModel(RequestStateHistoryRepository repository, string view) {
			// Setup Model pickers and other information we expect to display
			Model.RequestStateHistoryStatusPickList = repository.RequestStateHistoryStatusPickList(Model.RequestStateHistoryStatusID);
			ViewData["ParentXID"] = Model.RequestNumber;
			SetCancelURL();
			return View(view, Model);
		}
		// Redirect locations to goto on Success of an operation
		private ActionResult RedirectTo(string where, Guid requestId) {
			return RedirectToAction(Strings.IFormat("../Request/{0}", where), new {
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
			var parentID = new System.Guid(collection["RequestID"]);
			try {
				var repository = NewRepository<RequestStateHistoryRepository>();
				Model = new RequestStateHistoryModel();
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
				return RedirectBack(new System.Guid(collection["RequestID"]), new System.Guid(collection["CurrentStateHistoryID"]));
			}
			RemoveCancelURL();
			return RedirectTo((string)TempData["RedirectOnSuccess"], parentID);
		}
		#endregion

		#region Close
		private static readonly List<Guid> AllowedToCloseStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.RequestStateInProgressId });
		#region Close (GET)
		// GET: /RequestStateHistory/Close
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult Close(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "View";
			return GETPrepare(parentId, currentStateHistoryId, AllowedToCloseStates, NonRequestorChangeStateInstructions, (r) => {
				return GetViewModel(r);
			},
			Thinkage.MainBoss.Database.DatabaseCreation.CloseRequestAction,
			KB.K("You cannot change the request to Closed from its current state"));
		}
		#endregion
		#region Close (POST)
		//
		// POST: /RequestStateHistory/Close
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		[ValidateAntiForgeryToken]
		public ActionResult Close(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "Index";
			return POSTUpdate(collection, Thinkage.MainBoss.Database.KnownIds.RequestStateClosedId, AllowedToCloseStates, NonRequestorChangeStateInstructions, KB.K("You cannot Close the request since it is already closed"));
		}
		#endregion
		#endregion

		#region InProgress
		#region InProgress (GET)
		private static readonly List<Guid> CanChangeToInProgressStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.RequestStateNewId });

		// GET: /RequestStateHistory/InProgress
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult InProgress(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "Index";
			return GETPrepare(parentId, currentStateHistoryId, CanChangeToInProgressStates, NonRequestorChangeStateInstructions, (r) => {
				return GetViewModel(r);
			},
			Thinkage.MainBoss.Database.DatabaseCreation.InProgressRequestAction,
			KB.K("You cannot change the request to In Progress from its current state"));
		}
		#endregion
		#region InProgress (POST)
		//
		// POST: /RequestStateHistory/Close
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		[ValidateAntiForgeryToken]
		public ActionResult InProgress(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "Index";
			return POSTUpdate(collection, Thinkage.MainBoss.Database.KnownIds.RequestStateInProgressId, CanChangeToInProgressStates, NonRequestorChangeStateInstructions,
				KB.K("You cannot change the request to In Progress from its current state"));
		}
		#endregion
		#endregion

		#region AddComment
		// Common GetAddComment processing
		private static readonly List<Guid> CanCommentStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.RequestStateNewId, Thinkage.MainBoss.Database.KnownIds.RequestStateInProgressId });
		private static readonly List<StateHistoryRepository.CustomInstructions> NonRequestorChangeStateInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.FullUpdate
		});
		private static readonly List<StateHistoryRepository.CustomInstructions> NonRequestorSameStateInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.DefaultToExisting,
			StateHistoryRepository.CustomInstructions.FullUpdate
		});
		#region AddComment (GET)
		//
		// GET: /RequestStateHistory/AddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult AddComment(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "View";
			return GETPrepare(parentId, currentStateHistoryId, CanCommentStates, NonRequestorSameStateInstructions, (r) => {
				return GetViewModel(r, "AddComment");
			},
			null,
			KB.K("You cannot add a comment to the request in its current state"));
		}
		// GET: /RequestStateHistory/UnAssignedAddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult UnAssignedAddComment(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "ViewUnAssigned";
			return GETPrepare(parentId, currentStateHistoryId, CanCommentStates, NonRequestorSameStateInstructions, (r) => {
				return GetViewModel(r, "AddComment");
			},
			null,
			KB.K("You cannot add a comment to the request in its current state"));
		}
		#endregion
		#region AddComment (POST)
		//
		// POST: /RequestStateHistory/AddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		[ValidateAntiForgeryToken]
		public ActionResult AddComment(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "View";
			return POSTUpdate(collection, null, CanCommentStates, NonRequestorChangeStateInstructions, KB.K("You cannot add a comment to the request in its current state"));
		}
		// POST: /RequestStateHistory/UnAssignedAddComment
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		[ValidateAntiForgeryToken]
		public ActionResult UnAssignedAddComment(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "ViewUnAssigned";
			return POSTUpdate(collection, null, CanCommentStates, NonRequestorChangeStateInstructions, KB.K("You cannot add a comment to the request in its current state"));
		}
		#endregion
		#endregion

		#region SelfAssign
		private static readonly List<StateHistoryRepository.CustomInstructions> SelfAssignInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.DefaultToExisting,
			StateHistoryRepository.CustomInstructions.FullUpdate,
			StateHistoryRepository.CustomInstructions.SelfAssign
		});
		#region SelfAssign (GET)
		//
		// GET: /RequestStateHistory/SelfAssign
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		public ActionResult SelfAssign(Guid parentId, Guid currentStateHistoryId) {
			TempData["RedirectOnError"] = "UnAssigned";
			return GETPrepare(parentId, currentStateHistoryId, CanCommentStates, SelfAssignInstructions, (r) => {
				return GetViewModel(r);
			},
			null,
			KB.K("You cannot self assign the request in its current state"));
		}
		#endregion
		#region SelfAssign (POST)
		//
		// POST: /RequestStateHistory/SelfAssign
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		[ValidateAntiForgeryToken]
		public ActionResult SelfAssign(FormCollectionWithType collection) {
			TempData["RedirectOnSuccess"] = "UnAssigned";
			return POSTUpdate(collection, Thinkage.MainBoss.Database.KnownIds.RequestStateInProgressId, CanCommentStates, SelfAssignInstructions,
				KB.K("You cannot self assign the request in its current state"));
		}
		#endregion
		#endregion
	}
}
