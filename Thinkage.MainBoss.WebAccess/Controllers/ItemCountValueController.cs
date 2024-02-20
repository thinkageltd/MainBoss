using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.MVC.Models;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models;
using Thinkage.MainBoss.WebAccess.Models.Repository;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class ItemCountValueController : BaseControllerWithRulesViolationCheck<ItemCountValueEntities.ItemPhysicalCount> {
		#region Serialization Support
		protected override void OnActionExecuting(ActionExecutingContext filterContext) {
			base.OnActionExecuting(filterContext);

			ProposedPhysicalCounts = (SerializationUtils.DeSerialize(Request.Form["OriginalValuesModel"])
				?? TempData["OriginalValuesModel"]
				?? null) as List<ItemCountValueEntities.ItemPhysicalCount>;
		}
		protected override void OnResultExecuted(ResultExecutedContext filterContext) {
			base.OnResultExecuted(filterContext);
			if (filterContext.Result is RedirectToRouteResult)
				TempData["OriginalValuesModel"] = ProposedPhysicalCounts;

		}
		public List<ItemCountValueEntities.ItemPhysicalCount> ProposedPhysicalCounts;
		// the current model we are working with
		private ItemCountValueEntities.ItemPhysicalCount Model;
		protected override ItemCountValueEntities.ItemPhysicalCount GetModelForModelStateErrors() {
			return Model;
		}
		protected override ViewResult GetViewForModelStateError(ItemCountValueEntities.ItemPhysicalCount model) {
			return PhysicalCountsView();
		}
		// The Id of the WorkOrder we are dealing with for View&Actualize
		Guid StoreroomID;
		#endregion
		private ViewResult PhysicalCountsView() {
			ViewData["locationID"] = StoreroomID;
			return View("PhysicalCounts", Model);
		}
		private void InitViewData() {
			ViewData["ResultMessage"] = "";
			ViewData["Home"] = "WebAccess";
			ViewData["ViewCost"] = false;
			ViewData["ViewCostDisablerReason"] = "";
			ViewData["Refresh"] = "";
		}
		private void SetViewCostInformation(ItemCountValueRepository repository) {
			ViewData["ViewCost"] = repository.HasViewCostRight("InventoryActivity", out string reason);
			ViewData["ViewCostDisablerReason"] = reason;
		}
		#region ItemAssignments
		[MainBossAuthorization]
		public ActionResult PhysicalCounts(Guid locationId, [Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			SetHomeURL("../PhysicalCounts");
			StoreroomID = locationId;
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var PhysicalCountItemAssignments = repository.BrowseItemPhysicalCountByStoreroom(locationId);
			ViewData["Refresh"] = "PhysicalCounts";
			ViewData["ResultMessage"] = resultMessage;
			var storeroom = (from s in repository.BrowseStorerooms() where s.BaseRelativeLocation.LocationID == locationId select s).FirstOrDefault();
			ViewData["StoreroomIdentification"] = storeroom.BaseRelativeLocation.Code;
			ViewData["StoreroomPath"] = storeroom.BaseRelativeLocation.BaseLocation.Code;
			ViewData["StoreroomID"] = StoreroomID;
			SetViewCostInformation(repository);
			// Setup Model pickers and other information we expect to display
			ViewData["ItemAdjustmentPickList"] = repository.ItemAdjustmentPickList(null);
			return View(PhysicalCountItemAssignments);
		}
		#endregion
		[MainBossAuthorization]
		[AcceptVerbs(HttpVerbs.Post)]
		[ValidateInput(false)]
		public ActionResult Apply(FormCollectionWithType inputValues) {
			ViewData["UnAssigned"] = false;
			StoreroomID = new Guid(inputValues["StoreroomID"]);
			var ItemAdjustmentCodeID = new Guid(inputValues["ItemAdjustmentCodeID"]);

			var values = inputValues.ToValueProvider();
			Dictionary<Guid, ValueProviderResult> toPhysicalCount = new Dictionary<Guid, ValueProviderResult>();
			foreach (var a in from p in values
							  where p.Key.StartsWith("CheckBox_")
							  && (bool)p.Value.ConvertTo(typeof(bool))
							  select p) {

				string inputId = a.Key.Replace("CheckBox_", "Input_");
				Guid itemAssignmentID = new Guid(a.Key.Replace("CheckBox_", ""));
				ValueProviderResult v = null;
				v = values.GetValue(inputId);
				toPhysicalCount.Add(itemAssignmentID, v);
			}
			RuleViolationCollector errors = new RuleViolationCollector();
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.View(StoreroomID);
#if tobedone
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
#endif
			return RedirectToAction("../PhysicalCounts", new {
				locationID = StoreroomID,
				resultMessage = ResultMessage
			});
		}
	}
}
