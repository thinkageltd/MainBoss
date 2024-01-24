using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Thinkage.MainBoss.WebApi
{
	public class WebApiApplication : Thinkage.MainBoss.WebCommon.HttpApplication<Thinkage.MainBoss.WebApi.MainBossWebApiApplication>
	{
		protected override MainBossWebApiApplication CreateNewApplicationObject()
		{
			return MainBossWebApiApplication.CreateNewApplicationObject();
		}
		protected override void Application_Start()
		{
			base.Application_Start();

			// speed things up and remove the WebForm viewengine
			var webformViewEngine = ViewEngines.Engines.OfType<WebFormViewEngine>().FirstOrDefault();
			if (webformViewEngine != null)
				ViewEngines.Engines.Remove(webformViewEngine);

			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}
	}
}