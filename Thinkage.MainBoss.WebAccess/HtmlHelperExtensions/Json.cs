using System;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Thinkage.Web.Mvc.Json {

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class,
				AllowMultiple = false, Inherited = true)]
	public sealed class ValidateJsonAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter {
		public void OnAuthorization(AuthorizationContext filterContext) {
			if (filterContext == null) {
				throw new ArgumentNullException(nameof(filterContext));
			}

			var httpContext = filterContext.HttpContext;
			var cookie = httpContext.Request.Cookies[AntiForgeryConfig.CookieName];
			AntiForgery.Validate(cookie?.Value,
								 httpContext.Request.Headers["__RequestVerificationToken"]);
		}
	}
}