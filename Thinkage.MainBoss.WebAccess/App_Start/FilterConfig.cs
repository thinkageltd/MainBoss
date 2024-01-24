using System.Web.Mvc;

namespace Thinkage.MainBoss.WebAccess {
	public class FilterConfig {
		public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
			filters.Add(new HandleErrorAttribute());
		}
	}
}