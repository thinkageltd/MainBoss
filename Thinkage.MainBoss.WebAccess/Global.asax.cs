using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Thinkage.MainBoss.WebAccess
{
	public class MvcApplication : Thinkage.MainBoss.WebCommon.HttpApplication<Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication>
	{
		protected override MainBossWebAccessApplication CreateNewApplicationObject(System.Globalization.CultureInfo ci)
		{
			return MainBossWebAccessApplication.CreateNewApplicationObject(ci);
		}
		protected override void Application_Start()
		{
			base.Application_Start();

			// speed things up and remove the WebForm viewengine
			var webformViewEngine = ViewEngines.Engines.OfType<WebFormViewEngine>().FirstOrDefault();
			if( webformViewEngine != null)
				ViewEngines.Engines.Remove(webformViewEngine);

			AreaRegistration.RegisterAllAreas();

			WebApiConfig.Register(GlobalConfiguration.Configuration);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}
	}
}
