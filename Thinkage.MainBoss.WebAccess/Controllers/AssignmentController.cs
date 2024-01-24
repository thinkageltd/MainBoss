using System.Web.Mvc;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError]
	public class AssignmentController : BaseController<AssignmentRepository> {
		#region Index
		[MainBossAuthorization]
		public ActionResult Index() {
			var repository = NewRepository<AssignmentRepository>();
			repository.CheckPermission(repository.ViewRight, repository.BrowseRight);

			if (!((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).IsMainBossUser)
				throw new NoPermissionException(KB.K("You must be a MainBoss User to view your Assignment status").Translate());

			return View("Index", repository.GetAssignment(((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).UserID.Value));
		}
		#endregion
		#region NoPermission
		public ActionResult NoPermission(NoPermissionException exception) {
			return View("NoPermission", exception);
		}
		#endregion
	}
}
