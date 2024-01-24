using System.Web.Mvc;
using System.Web.Routing;

namespace Thinkage.MainBoss.WebAccess {
	public class RouteConfig {
		public static void RegisterRoutes(RouteCollection routes) {
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.IgnoreRoute("favicon.ico"); // we don't have one, so avoid exceptions when a browser goes looking
			routes.IgnoreRoute("ChartImg.axd/{*pathInfo}");
			routes.IgnoreRoute("{controller}/ChartImg.axd/{*pathInfo}");
			routes.IgnoreRoute("{controller}/{action}/ChartImg.axd/{*pathInfo}");


			WebCommon.RouteConfig.RegisterUnhandledException(routes);
			routes.MapRoute(
				"Default",                                              // Route name
				"{controller}/{action}/{id}",                           // URL with parameters
				new
				{
					controller = "Home",
					action = "Index",
					id = ""
				},  // Parameter defaults
				new
				{
					id = @"([\dA-Fa-f]{8}(-([\dA-Fa-f]){4}){4}[\dA-Fa-f]{8})|",
				} // Constraints on parameters
			);
		}
	}
}