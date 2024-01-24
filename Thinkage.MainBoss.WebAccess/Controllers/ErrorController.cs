using System.Web.Mvc;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError(View = "Error")]
	public class ErrorController : Controller {
		public ErrorController() {
		}

		[AcceptVerbs(HttpVerbs.Get)]
		public ActionResult UnHandledException() {
			var e = (System.Exception)this.Session["Exception"];
			if (e == null) // got here because user did a reload on xxx/UnHandledException, and we no longer have a session with an exception object ?
				return RedirectToAction("Index", "Home"); // try the root again; perhaps we'll be lucky.
			return View(new HandleErrorInfo(e, "ErrorController", "UnHandledException"));
		}
	}
}
