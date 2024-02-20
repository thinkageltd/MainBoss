using System.Web.Mvc;
using Thinkage.MainBoss.WebAccess.Models;
using Thinkage.MainBoss.WebAccess.Models.Repository;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError]
	public class InventoryController : BaseController<ItemCountValueRepository> {
		#region Index
		[MainBossAuthorization]
		public ActionResult Index() {
			if (!((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).IsMainBossUser)
				throw new NoPermissionException(KB.K("You must be a MainBoss User to view Inventory").Translate());

			return View("Index");
		}
		#endregion
	}
}
