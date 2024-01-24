using System;
using System.Web.Mvc;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public class UnitController : BaseController {
		#region View
		//
		// GET: /Unit/View?id=
		[MainBossAuthorization]
		public ActionResult View(Guid id) {
			var repository = NewRepository<UnitRepository>();
			repository.CheckPermission(repository.ViewRight);
			var unit = repository.View(id);
			return View(unit);
		}
		#endregion
		#region Autocomplete
		[AcceptVerbs(HttpVerbs.Post)]
		public JsonResult Autocomplete(string term, string pattern) {
			var repository = NewRepository<UnitRepository>();
			return Json(repository.Autocomplete(term, pattern), JsonRequestBehavior.AllowGet);
		}
		#endregion
		protected override void OnException(ExceptionContext filterContext) {
			base.OnException(filterContext);
			if (filterContext.ExceptionHandled)
				return;
			// Return the response of an error in Ajax call; this could ONLY be the pattern caused an exception as we simple return no exception for any other case.
			filterContext.ExceptionHandled = true;
			filterContext.HttpContext.Response.Clear();
			filterContext.HttpContext.Response.ContentEncoding = System.Text.Encoding.UTF8;
			filterContext.HttpContext.Response.HeaderEncoding = System.Text.Encoding.UTF8;
			filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
			filterContext.HttpContext.Response.StatusCode = 400;
			filterContext.Result = new ContentResult {
				Content = Thinkage.Libraries.Exception.FullMessage(filterContext.Exception),
				ContentEncoding = System.Text.Encoding.UTF8,
			};
		}
	}
}
