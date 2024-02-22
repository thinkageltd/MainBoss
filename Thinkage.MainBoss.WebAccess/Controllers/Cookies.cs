using System;
using System.Web;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	/// <summary>
	/// A class containing common Cookie handling support functions.
	/// </summary>
	public static class Cookies {
		public static void CreateRequestorEmail(HttpResponseBase response, string emailAddress) {
			HttpCookie mbRemoteCookie = new HttpCookie("MainBossWebAccess") {
				HttpOnly = true,
				Expires = DateTime.Now.AddYears(3)
			};

			mbRemoteCookie.Values["requestorEmail"] = emailAddress;
			response.Cookies.Add(mbRemoteCookie);
		}
		public static string GetRequestorEmail(HttpRequestBase request) {
			var cookie = request.Cookies["MainBossWebAccess"];
			return cookie?["requestorEmail"];
		}
	}
}
