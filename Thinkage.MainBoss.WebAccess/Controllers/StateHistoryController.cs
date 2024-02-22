using System;
using System.Web.Mvc;
using Thinkage.Libraries;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	public abstract class StateHistoryController<TModel> : BaseControllerWithRulesViolationCheck<TModel> where TModel : class, new() {
		protected StateHistoryController() {
			StateHistoryChangedException = DefaultStateHistoryChangedException;
		}
		/// <summary>
		/// Common function to redirect back to the initiating controller action for a StateHistory Record
		/// </summary>
		/// <param name="parentID"></param>
		/// <param name="stateHistoryID"></param>
		/// <returns></returns>
		protected ActionResult RedirectBack(Guid parentID, Guid stateHistoryID) {
			var redirectController = ControllerContext.RouteData.GetRequiredString("controller");
			var redirectTarget = ControllerContext.RouteData.GetRequiredString("action");
			ModelState.SetModelValue("CurrentStateHistoryID", new ValueProviderResult(stateHistoryID, stateHistoryID.ToString(), Application.InstanceFormatCultureInfo));
			DateTime resetEffectiveDate = DateTime.Now;
			ModelState.SetModelValue("EffectiveDate", new ValueProviderResult(resetEffectiveDate, null, Application.InstanceFormatCultureInfo));
			return RedirectToAction(Thinkage.Libraries.Strings.IFormat("../{0}/{1}", redirectController, redirectTarget), new {
				@ParentID = parentID,
				@CurrentStateHistoryID = stateHistoryID,
				 ResultMessage
			});
		}
		protected ActionResult DefaultStateHistoryChangedException(Guid parentID, Guid id) {
			// Add a ModelError for IsValid false
			ModelState.AddModelError(String.Empty, KB.K("State history has changed before we could save your information. Please review any changed information and try again").Translate());
			// switch the modelstate value for CurrentRequestStateHistoryID to the one updated in Model at time of error
			return RedirectBack(parentID, id);
		}
		delegate ActionResult StateChangedException(Guid parentID, Guid stateHistoryID);
		StateChangedException StateHistoryChangedException;
		protected override void OnException(ExceptionContext filterContext) {
			base.OnException(filterContext);
			if (filterContext.Exception is StateHistoryChangedUnderneathUsException) {
				filterContext.ExceptionHandled = true;
				StateHistoryChangedUnderneathUsException ex = filterContext.Exception as StateHistoryChangedUnderneathUsException;
				StateHistoryChangedException(ex.ParentID, ex.NewStateHistoryID).ExecuteResult(this.ControllerContext);
			}
		}
	}
}