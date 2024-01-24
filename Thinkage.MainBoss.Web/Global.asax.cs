using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Web {
	public class Global : System.Web.HttpApplication {
		protected Global() {
			// Arrange to set the current thread's Thinkage.Libraries.Application.Instance at the start of each request and clear it out when done.
			PostAcquireRequestState += delegate(object sender, EventArgs e) {
				var appObj = (Thinkage.Libraries.Application)Context.Session["Thinkage.Libraries.Application.Instance"];
				Thinkage.Libraries.Application.SetApplicationObject(appObj);
				// Set thread culture info to the application's CultureInfo.
				System.Threading.Thread.CurrentThread.CurrentCulture = appObj.CultureInfo;
				System.Threading.Thread.CurrentThread.CurrentUICulture = appObj.CultureInfo;
			};
			EndRequest += delegate(object sender, EventArgs e) {
				Thinkage.Libraries.Application.SetApplicationObject(null);
			};
		}

		#region Global setup/teardown
		// Multiple Application objects exist, one for each simultaneous processing thread, but
		// these methods are 'special' insofar as they are only called on the first Application object created, which might in fact not
		// be the one that actually processes the request! As a result setting event handlers here on 'this' is pointless.
		protected void Application_Start(object sender, EventArgs e) {
			//System.Threading.Thread.Sleep(15000);
			Thinkage.Libraries.Application.SetPerThreadApplicationInstance();
			RootApplication = MBWApplication.CreateRootApplicationObject();
			RootApplication.SetupApplication();
			// This method runs in the thread that will serve the first request, so we must clear out the current app object Instance so the request's
			// app object is correctly created.
			Thinkage.Libraries.Application.SetApplicationObject(null);
		}
		protected void Application_End(object sender, EventArgs e) {
			RootApplication.TeardownApplication(null);
			RootApplication = null;
		}
		private MBWApplication RootApplication;
		#endregion

		#region Session setup/treadown
		// These methods do no appear to have corresponding event handlers, so instead they are protected methods called by the surrounding
		// coded-on-the-fly calling framework.
		protected void Session_Start(object sender, EventArgs e) {
			// This is called when a new Session is created. This code creates our Thinkage.Libraries.Application object and adds it to the
			// Session state but leaves Application.Instance null.

			// TODO: The following check might have to go. When a session ends we null out Instance for the current thread if it is the same application object,
			// but it is not clear that will always be the case.
			//if (Thinkage.Libraries.Application.Instance != null)
			//	throw new GeneralException(KB.K("HTTP state indicates a new session but Application.Instance is non-null"));

			// TODO: If client-impersonation is off Request.UserHostName is just an IP address (at least, when the client browser asks for localhost it is
			// the loopback IP address).
			System.Globalization.CultureInfo userCultureInfo = null;
			try {
				// TODO: We should loop until we don't get an exception.
				userCultureInfo = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag(Request.UserLanguages[0]);
			}
			catch {
			}
			if (userCultureInfo == null)
				userCultureInfo = System.Globalization.CultureInfo.CurrentCulture;
			else {
				System.Threading.Thread.CurrentThread.CurrentCulture = userCultureInfo;
				System.Threading.Thread.CurrentThread.CurrentUICulture = userCultureInfo;
			}
			MBWApplication app = Thinkage.MainBoss.Web.MBWApplication.CreateClientApplicationObject(Request.LogonUserIdentity.Name, Request.UserHostName, userCultureInfo, new System.Security.Principal.WindowsPrincipal(Request.LogonUserIdentity));
			app.SetupApplication();
			if (Session.Mode != SessionStateMode.InProc)
				throw new GeneralException(KB.K("HTTP state mode must be InProc"));

			Session.Add("Thinkage.Libraries.Application.Instance", app);
			Thinkage.Libraries.Application.SetApplicationObject(null);
		}
		protected void Session_End(object sender, EventArgs e) {
			// This is called when a session dies, either by timeout or because we called Abandon on it.
			var endingApplication = (Thinkage.Libraries.Application)Session["Thinkage.Libraries.Application.Instance"];
			if (endingApplication == Thinkage.Libraries.Application.Instance)
				Thinkage.Libraries.Application.SetApplicationObject(null);
			endingApplication.TeardownApplication(null);
		}
		#endregion
	}
}