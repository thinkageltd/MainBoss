using System;
using System.Web.Mvc;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.WebAccess.Models;
using Thinkage.MainBoss.WebAccess.Models.Repository;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class StoreroomController : BaseControllerWithRulesViolationCheck<ItemCountValueEntities.PermanentStorage> {
		ItemCountValueEntities.PermanentStorage Model;
		protected override ItemCountValueEntities.PermanentStorage GetModelForModelStateErrors() {
			return Model;
		}
		private void InitViewData() {
			ViewData["ResultMessage"] = "";
			ViewData["Refresh"] = "";
		}
		#region Index
		// GET: /
		[MainBossAuthorization]
		public ActionResult Index([Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var Storerooms = repository.BrowseStorerooms();
			ViewData["Refresh"] = "Storerooms";
			ViewData["ResultMessage"] = resultMessage;
			return View(Storerooms);
		}
		#endregion
		#region ItemAssignments
		public ActionResult ItemAssignments(Guid locationId, [Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			SetHomeURL("../Index");
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.BrowseRight);
			var ItemAssignments = repository.BrowseItemLocationsByStoreroom(locationId);
			ViewData["Refresh"] = "Item Assignments";
			ViewData["ResultMessage"] = resultMessage;
			return View(ItemAssignments);
		}
		#endregion
		#region ItemAssignments
		public ActionResult ViewItemAssignment(Guid itemAssignmentId, [Translated]string resultMessage) {
			InitViewData();
			SetCancelURL();
			SetHomeURL("../Index");
			ItemCountValueRepository repository = NewRepository<ItemCountValueRepository>();
			repository.CheckPermission(repository.ViewRight);
			var ItemAssignment = repository.ViewByItemAssignmentId(itemAssignmentId);
			ViewData["Refresh"] = "Item Assignment";
			ViewData["ResultMessage"] = resultMessage;
			return View(ItemAssignment);
		}
		#endregion
	}
}