using System;
using System.Web.Mvc;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models.Repository;
using System.Linq;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class StoreroomController : BaseControllerWithRulesViolationCheck<ItemCountValueEntities.PermanentStorage> {
		private ItemCountValueEntities.PermanentStorage Model;
		protected override ItemCountValueEntities.PermanentStorage GetModelForModelStateErrors() {
			Model=null; // assign to eliminate warnings
			return Model;
		}
		private void InitViewData() {
			ViewData["ResultMessage"] = "";
			ViewData["Refresh"] = "";
			ViewData["Home"] = "WebAccess";
			ViewData["ViewCost"] = false;
			ViewData["ViewCostDisablerReason"] = "";
			ViewData["ViewAccounting"] = false;
			ViewData["ViewAccountingDisablerReason"] = "";
		}
		private void SetViewPermissionInformation(ItemCountValueRepository repository) {
			ViewData["ViewCost"] = repository.HasViewCostRight("InventoryActivity", out string reasonC);
			ViewData["ViewCostDisablerReason"] = reasonC;
			ViewData["ViewAccounting"] = repository.HasActionRight("ViewAccounting", out string reasonA);
			ViewData["ViewAccountingDisablerReason"] = reasonA;
		}
		#region Index
		// GET: /
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult Index([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var Storerooms = repository.BrowseStoreroomsSortedByLocation();
			ViewData["Refresh"] = "Storerooms";
			ViewData["ResultMessage"] = resultMessage;
			return View(Storerooms);
		}
		#endregion
		#region ItemAssignments
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult ItemAssignments(Guid locationId, [Translated]string resultMessage) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURL("Index");
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var ItemAssignments = repository.BrowseItemLocationsByStoreroom(locationId);
			ViewData["Refresh"] = "Item Assignments";
			ViewData["ResultMessage"] = resultMessage;
			var storeroom = (from s in repository.BrowseStorerooms() where s.BaseRelativeLocation.LocationID == locationId select s).FirstOrDefault();
			ViewData["StoreroomIdentification"] = storeroom.BaseRelativeLocation.Code;
			ViewData["StoreroomPath"] = storeroom.BaseRelativeLocation.BaseLocation.Code;
			SetViewPermissionInformation(repository);
			return View(ItemAssignments);
		}
		#endregion
		#region ViewItemAssignment
		[HttpGet]
		[MainBossAuthorization]
		public ActionResult ViewItemAssignment(Guid itemAssignmentId, [Translated]string resultMessage) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURLAsReferrer();
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.ViewRight);
			var ItemAssignment = repository.ViewByItemAssignmentId(itemAssignmentId);
			ViewData["Refresh"] = "Item Assignment";
			ViewData["ResultMessage"] = resultMessage;
			SetViewPermissionInformation(repository);
			return View(ItemAssignment);
		}
		#endregion
	}
}