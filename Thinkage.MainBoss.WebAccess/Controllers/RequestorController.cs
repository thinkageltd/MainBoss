using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.MVC.Controllers;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError]
	public class RequestorController : StateHistoryController<RequestStateHistoryModel> {
		RequestStateHistoryModel Model;
		protected override RequestStateHistoryModel GetModelForModelStateErrors() {
			if (Model.RequestStateHistoryStatusPickList == null)
				Model.RequestStateHistoryStatusPickList = NewRepository<RequestStateHistoryRepository>().RequestStateHistoryStatusPickList(Model.RequestStateHistoryStatusID);
			return Model;
		}
		#region AddComment
		static List<Guid> RequestorAddCommentStates = new List<Guid>(new Guid[] { Thinkage.MainBoss.Database.KnownIds.RequestStateNewId, Thinkage.MainBoss.Database.KnownIds.RequestStateInProgressId });
		static List<StateHistoryRepository.CustomInstructions> RequestorChangeStateInstructions = new List<StateHistoryRepository.CustomInstructions>(new StateHistoryRepository.CustomInstructions[] {
			StateHistoryRepository.CustomInstructions.DefaultToExisting,
			StateHistoryRepository.CustomInstructions.CheckRequestorID
		});
		private RequestStateHistoryRepository InitModel(Guid requestId, Guid currentStateHistoryId) {
			var repository = NewRepository<RequestStateHistoryRepository>();
			Model = new RequestStateHistoryModel() {
				RequestID = requestId,
				CurrentStateHistoryID = currentStateHistoryId,
				EffectiveDate = DateTime.Now
			};
			return repository;
		}
		private ActionResult GetViewModel(RequestStateHistoryRepository repository) {
			// Setup Model pickers and other information we expect to display
			Model.RequestStateHistoryStatusPickList = repository.RequestStateHistoryStatusPickList(Model.RequestStateHistoryStatusID);
			ViewData["ParentXID"] = Model.RequestNumber;
			SetCancelURL();
			return View(ControllerContext.RouteData.GetRequiredString("action"), Model);
		}
		#region AddComment (GET)
		//
		// GET: /Requestor/AddComment
		[MainBossAuthorization(MainBossAuthorized.Anyone)]
		[AcceptVerbs(HttpVerbs.Get)]
		[ImportModelStateFromTempData]
		//WARNING: Do not rename the arguments to this controller method without due consideration to changes in the RequestorNotificationProcessor in the MainBoss Service that constructs
		// links to the web site with knowledge of these arguments
		public ActionResult AddComment(Guid parentID, Guid currentStateHistoryId) {
			Model = new RequestStateHistoryModel() {
				RequestID = parentID,
				CurrentStateHistoryID = currentStateHistoryId,
				EffectiveDate = DateTime.Now
			};
			var repository = NewRepository<RequestStateHistoryRepository>();
			try {
				var emailFromCookie = Cookies.GetRequestorEmail(Request);
				if (emailFromCookie != null)
					Model.EmailAddress = emailFromCookie;
				repository.Prepare(Model, RequestorAddCommentStates, RequestorChangeStateInstructions);
			}
			catch (StateHistoryChangedUnderneathUsException e) {
				return DefaultStateHistoryChangedException(e.ParentID, e.NewStateHistoryID);
			}
			catch (ActionNotPermittedForCurrentStateException) {
				ResultMessage = KB.K("You cannot add a remark to the request in its current state").Translate();
				return RedirectToCannotMakeRemarks(parentID);
			}
			catch (NotCorrectRequestorForCommentException) {
				ModelState.AddModelError("EmailAddress", KB.K("Your email address does not match that of the originator of this request.").Translate());
				return View(Model);
			}
			catch (System.Exception gex) {
				Session.Add("Exception", gex);
				return RedirectToAction("UnHandledException", "Error");
			}
			return GetViewModel(repository);

#if OLD
			DBConcurrencyExceptionView = delegate()
			{
				return ToConcurrencyErrorView(parentID);
			};
			try {
				var emailFromCookie = Cookies.GetRequestorEmail(Request);
				if (emailFromCookie != null)
					Model.EmailAddress = emailFromCookie;
			}
			catch (System.Exception gex) {
				Session.Add("Exception", gex);
				return RedirectToAction("UnHandledException", "Error");
			}
			CompleteTheModel(repository);
			// Need to stash the id in the form so the Post code can link the New Request State history to the Request (actually it automagically appears in the FormCollection ...)
			return View(Model);
#endif
		}
		#endregion
		#region AddComment (POST)
		//
		// POST: /Requestor/AddComment
		[MainBossAuthorization(MainBossAuthorized.Requestor)]
		[AcceptVerbs(HttpVerbs.Post)]
		[ExportModelStateToTempData]
		public ActionResult AddComment(FormCollectionWithType collection) {
			var parentID = new System.Guid(collection["RequestID"]);
			Guid newStateHistoryID = new System.Guid(collection["CurrentStateHistoryID"]);
			try {
				var repository = NewRepository<RequestStateHistoryRepository>();
				Model = new RequestStateHistoryModel();
				newStateHistoryID = repository.Update(null, UpdateFromForm(Model, collection), null, RequestorAddCommentStates, RequestorChangeStateInstructions);
				Cookies.CreateRequestorEmail(Response, Model.EmailAddress);
			}
			catch (StateHistoryChangedUnderneathUsException e) {
				return DefaultStateHistoryChangedException(e.ParentID, e.NewStateHistoryID);
			}
			catch (ActionNotPermittedForCurrentStateException) {
				ResultMessage = KB.K("You cannot add a comment to the request in its current state").Translate();
				return RedirectToCannotMakeRemarks(parentID);
			}
			catch (NotCorrectRequestorForCommentException) {
				ModelState.AddModelError("EmailAddress", KB.K("Your email address does not match that of the originator of this request.").Translate());
				return RedirectBack(parentID, newStateHistoryID);
			}
			RemoveCancelURL();
#if OLD
			var requestId = new System.Guid(collection["RequestID"]);
			var repository = NewRepository<RequestStateHistoryRepository>();
			Model = new RequestStateHistoryModel();
			try {
				Model = UpdateFromForm(Model, collection);
				if (!Model.IsValid)
					throw new Exception();
			}
			catch {
				CollectRuleViolations(Model);
				CompleteTheModel(repository);
				return View(Model);
			}
			Guid requestStateHistoryId;
			try {
				requestStateHistoryId = repository.AddRequestorsComment(Model);
				Cookies.CreateRequestorEmail(Response, Model.EmailAddress);
			}
			catch (NotCorrectRequestorForCommentException) {
				ModelState.AddModelError("EmailAddress", KB.K("Your email address does not match that of the originator of this request.").Translate());
				return View(Model);
			}
#endif
			// return to the now 'new' current statehistory comment so Requestor can see what they added, and maybe add another. The 
			// comment is placed into the RequestorComment field.
			return RedirectToAction("AddComment", "Requestor", new {
				parentID = parentID,
				currentStateHistoryID = newStateHistoryID
			});
		}
		#endregion
		#endregion
		#region CanNoLongerMakeRemarks
		[MainBossAuthorization(MainBossAuthorized.Anyone)]
		public ActionResult CanNoLongerMakeRemarks(Guid id, [Translated] string resultMessage) {
			ViewData["ResultMessage"] = resultMessage;
			return View("CanNoLongerMakeRemarks");
		}
		#endregion
		#region Support Functions
		private ActionResult RedirectToCannotMakeRemarks(Guid requestId) {
			return RedirectToAction("../Requestor/CanNoLongerMakeRemarks", new {
				id = requestId,
				resultMessage = ResultMessage
			});
		}
		#endregion
	}
}
