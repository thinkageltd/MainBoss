using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.MVC.Models;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class WorkOrderController : BaseControllerWithRulesViolationCheck<WorkOrderEntities.WorkOrder> {
		#region Serialization Support
		protected override void OnActionExecuting(ActionExecutingContext filterContext) {
			base.OnActionExecuting(filterContext);

			ProposedWOResources = (SerializationUtils.DeSerialize(Request.Form["OriginalValuesModel"])
				?? TempData["OriginalValuesModel"]
				?? null) as List<WOResource>;
		}
		protected override void OnResultExecuted(ResultExecutedContext filterContext) {
			base.OnResultExecuted(filterContext);
			if (filterContext.Result is RedirectToRouteResult)
				TempData["OriginalValuesModel"] = ProposedWOResources;

		}
		public List<WOResource> ProposedWOResources;
		// the current model we are working with
		private WorkOrderEntities.WorkOrder Model;
		protected override WorkOrderEntities.WorkOrder GetModelForModelStateErrors() {
			return Model;
		}
		protected override ViewResult GetViewForModelStateError(WorkOrderEntities.WorkOrder model) {
			return WorkOrderView();
		}

		// The Id of the WorkOrder we are dealing with for View&Actualize
		private Guid WorkOrderId;
		#endregion
		private void InitViewData() {
			ViewData["ResultMessage"] = "";
			ViewData["UnAssigned"] = false;
			ViewData["Refresh"] = "";
			ViewData["Home"] = "WebAccess";
			ViewData["CanSelfAssign"] = false;
		}
		#region UnAssigned
		// GET: /WorkOrder/UnAssigned
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult UnAssigned([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			UnAssignedWorkOrderRepository WorkOrderRepository = NewRepository<UnAssignedWorkOrderRepository>();
			WorkOrderRepository.CheckPermission(WorkOrderRepository.BrowseRight);
			var WorkOrders = WorkOrderRepository.BrowseUnAssigned();
			ViewData["Refresh"] = "UnAssigned";
			ViewData["ResultMessage"] = resultMessage;
			return View(WorkOrders);
		}
		#endregion
		#region Index
		// GET: /WorkOrder/
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult Index([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			WorkOrderRepository repository = NewRepository<WorkOrderRepository>();
			var requests = repository.BrowseAssigned();
			ViewData["Refresh"] = "";
			ViewData["ResultMessage"] = resultMessage;
			return View(requests);
		}
		#endregion
		#region View
		//
		// GET: /WorkOrder/View/5
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult View(Guid id, [Translated] string resultMessage) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURL("../Index");
			WorkOrderRepository repository = NewRepository<WorkOrderRepository>();
			repository.CheckPermission(repository.ViewRight);
			SetBasicModel(id, repository);
			ProposedWOResources = new List<WOResource>(repository.Resources(WorkOrderId));
			Model.Resources = ProposedWOResources;
			Model.CannotActualizeBecause = repository.CantActualizeBecause;
			ViewData["UnAssigned"] = false;
			ViewData["ResultMessage"] = resultMessage;
			return WorkOrderView();
		}
		// GET: /WorkOrder/View/UnAssigned
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult ViewUnAssigned(Guid id, [Translated] string resultMessage) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURL("../UnAssigned");
			UnAssignedWorkOrderRepository repository = NewRepository<UnAssignedWorkOrderRepository>();
			repository.CheckPermission(repository.ViewRight);
			SetBasicModel(id, repository);
			ProposedWOResources = new List<WOResource>(repository.Resources(WorkOrderId));
			Model.Resources = ProposedWOResources;
			ViewData["UnAssigned"] = true;
			ViewData["CanSelfAssign"] = UnAssignedWorkOrderRepository.CanSelfAssign();
			ViewData["ResultMessage"] = resultMessage;
			return WorkOrderView();
		}
		private ViewResult WorkOrderView() {
			ViewData["WorkOrderID"] = WorkOrderId;
			return View("View", Model);
		}
		private void SetBasicModel(Guid woId, WorkOrderBaseRepository repository) {
			WorkOrderId = woId;
			Model = repository.View(WorkOrderId);
		}
		/// <summary>
		/// Form posted for Actualization
		/// </summary>
		/// <param name="toActualize"></param>
		/// <returns></returns>

		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ValidateInput(false)]
		[ValidateAntiForgeryToken]
		public ActionResult Actualize(FormCollectionWithType inputValues) {
			ViewData["UnAssigned"] = false;
			var values = inputValues.ToValueProvider();
			Dictionary<Guid, ValueProviderResult> toActualize = new Dictionary<Guid, ValueProviderResult>();
			foreach (var a in from p in values
							  where p.Key.StartsWith("CheckBox_")
							  && (bool)p.Value.ConvertTo(typeof(bool))
							  select p) {

				string inputId = a.Key.Replace("CheckBox_", "Input_");
				Guid demandId = new Guid(a.Key.Replace("CheckBox_", ""));
				ValueProviderResult v = null;
				v = values.GetValue(inputId);
				toActualize.Add(demandId, v);
			}
			RuleViolationCollector errors = new RuleViolationCollector();
			WorkOrderRepository repository = NewRepository<WorkOrderRepository>();
			SetBasicModel(new System.Guid(inputValues["WorkOrderID"]), repository);
			Model.Resources = ProposedWOResources;
			repository.ActualizeResources(WorkOrderId, Model, toActualize, (string inputId, System.Exception e, ValueProviderResult value) => {
				ModelState.Add(new KeyValuePair<string, ModelState>(inputId, new ModelState() {
					Value = value
				}));
				errors.AddError(inputId, e);
			});
			if (!errors.IsValid)
				throw new ValueProviderException(errors);

			return RedirectToAction("../WorkOrder/View", new {
				id = WorkOrderId,
				resultMessage = ResultMessage
			});
		}
		#endregion
	}
}
