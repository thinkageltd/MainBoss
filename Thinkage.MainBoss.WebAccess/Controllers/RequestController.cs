using System;
using System.Web.Mvc;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class RequestController : BaseControllerWithRulesViolationCheck<RequestEntities.Request> {
		RequestEntities.Request Model;
		protected override RequestEntities.Request GetModelForModelStateErrors() {
			return Model;
		}
		private void InitViewData() {
			ViewData["ResultMessage"] = "";
			ViewData["UnAssigned"] = false;
			ViewData["Refresh"] = "";
			ViewData["Home"] = "WebAccess";
			ViewData["CanSelfAssign"] = false;
		}
		#region UnAssigned
		// GET: /Request/UnAssigned
		[MainBossAuthorization]
		public ActionResult UnAssigned([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			UnAssignedRequestRepository repository = NewRepository<UnAssignedRequestRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var requests = repository.BrowseUnAssigned();
			ViewData["Refresh"] = "UnAssigned";
			ViewData["ResultMessage"] = resultMessage;
			return View(requests);
		}
		#endregion
		#region Index
		// GET: /Request/
		[MainBossAuthorization]
		public ActionResult Index([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			RequestRepository repository = NewRepository<RequestRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var requests = repository.BrowseAssigned();
			ViewData["Refresh"] = "";
			ViewData["ResultMessage"] = resultMessage;
			return View(requests);
		}
		#endregion
		#region View
		//
		// GET: /Request/View/5
		[MainBossAuthorization]
		public ActionResult View(Guid id, [Translated] string resultMessage) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURL("../Index");
			RequestRepository repository = NewRepository<RequestRepository>();
			repository.CheckPermission(repository.ViewRight);
			Model = repository.View(id);
			ViewData["ResultMessage"] = resultMessage;
			return View(Model);
		}
		#endregion
		#region ViewUnAssigned
		//
		// GET: /Request/ViewUnAssigned/5
		[MainBossAuthorization]
		public ActionResult ViewUnAssigned(Guid id, [Translated] string resultMessage) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURL("../UnAssigned");
			UnAssignedRequestRepository repository = NewRepository<UnAssignedRequestRepository>();
			repository.CheckPermission(repository.ViewRight);
			Model = repository.View(id);
			ViewData["UnAssigned"] = true;
			ViewData["CanSelfAssign"] = repository.CanSelfAssign();
			ViewData["ResultMessage"] = resultMessage;
			return View("View", Model); // Share the same View as above
		}
		#endregion
		#region Create
		#region Create (GET)
		// GET: /Request/Create
		[MainBossAuthorization(MainBossAuthorized.Requestor)]
		public ActionResult Create(Guid requestorID) {
			var createRepository = NewRepository<CreateRequestRepository>();
			Model = new RequestEntities.Request() {
				RequestorID = requestorID
			};
			createRepository.PrepareForNewRequest(Model);
			Model.RequestPriorityPickList = createRepository.RequestPriorityPickList(Model.RequestPriorityID);
			SetCancelURL();
			ViewData["Home"] = "Index";
			return View(Model);
		}
		#endregion
		#region Create (POST)
		//
		// POST: /Request/Create
		[MainBossAuthorization(MainBossAuthorized.Requestor)]
		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult Create(FormCollectionWithType collection) {
			var repository = NewRepository<CreateRequestRepository>();
			// repository.CheckPermission( ?? );
			Model = new RequestEntities.Request();
			Model.RequestPriorityPickList = repository.RequestPriorityPickList(Model.RequestPriorityID);
			repository.CreateNewRequest(UpdateFromForm(Model, collection));
			RemoveCancelURL();
			return RedirectToAction("Index", "Home");
		}
		#endregion
		#endregion
		#region RequestorList
		[MainBossAuthorization(MainBossAuthorized.Requestor)]
		public ActionResult RequestorList(Guid requestorID, string resultMessage) {
			InitViewData();
			SetCancelURL();
			RequestRepository repository = NewRepository<RequestRepository>();
			var requests = repository.BrowseRequestorPendingRequests(requestorID);
			ViewData["Refresh"] = "";
			ViewData["Home"] = "Index";
			ViewData["ResultMessage"] = resultMessage;
			return View(requests);
		}
		#endregion
	}
}
