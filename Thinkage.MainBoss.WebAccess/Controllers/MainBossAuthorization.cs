using System.Web;
using System.Web.Mvc;

namespace Thinkage.MainBoss.WebAccess {
	public enum MainBossAuthorized {
		Requestor = 0,
		MainBossUser = 1,
		Anyone = 3
	}
	public class MainBossAuthorization : AuthorizeAttribute {
		readonly MainBossAuthorized HowAuthorized;
		public MainBossAuthorization()
			: this(MainBossAuthorized.MainBossUser) {
		}
		public MainBossAuthorization(MainBossAuthorized how) {
			HowAuthorized = how;
		}
		protected override bool AuthorizeCore(HttpContextBase httpContext) {
			switch (HowAuthorized) {
			case MainBossAuthorized.Anyone:
			case MainBossAuthorized.Requestor:
				return true;
			default:
				return base.AuthorizeCore(httpContext);
			}
		}
	}
}
