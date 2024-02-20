using System;
using System.Web.Mvc;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models.Repository;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class ItemController : BaseControllerWithRulesViolationCheck<ItemCountValueEntities.Item> {
		ItemCountValueEntities.Item Model;
		protected override ItemCountValueEntities.Item GetModelForModelStateErrors() {
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
		[MainBossAuthorization]
		public ActionResult Index([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var Items = repository.BrowseItems();
			ViewData["Refresh"] = "";
			ViewData["ResultMessage"] = resultMessage;
			SetViewPermissionInformation(repository);
			return View(Items);
		}
		#endregion
		#region View
		//
		// GET: /Item/View?id=
		[MainBossAuthorization]
		public ActionResult View(Guid id) {
			InitViewData();
			RemoveCancelURL();
			SetHomeURLAsReferrer();
			var repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.ViewRight);
			SetViewPermissionInformation(repository);
			var item = repository.View(id);
			return View(item);
		}
		#endregion
		#region ItemAssignments
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