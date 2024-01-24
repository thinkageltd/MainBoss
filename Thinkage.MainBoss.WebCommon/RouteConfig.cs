using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Thinkage.MainBoss.WebCommon
{
	public class RouteConfig
	{
		public static void RegisterUnhandledException(RouteCollection routes)
		{
			routes.MapRoute(
				"Error",
				"UnHandledException",
				new
				{
					controller = "Error",
					action = "UnHandledException"
				}
			);
		}
	}
}