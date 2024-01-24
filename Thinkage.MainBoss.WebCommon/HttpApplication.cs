using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using Thinkage.Libraries;
//using System.Data.Services;
//using System.Web.Optimization;
//using System.Web.Http;

namespace Thinkage.MainBoss.WebCommon
{
	abstract public class HttpApplication<APPOBJ> : System.Web.HttpApplication where APPOBJ : Thinkage.Libraries.Application
	{
		/// <summary>
		/// Derived class must be able to create a new ApplicationObject when we need it
		/// </summary>
		/// <returns></returns>
		protected abstract APPOBJ CreateNewApplicationObject(System.Globalization.CultureInfo ci);

		const string InstanceKey = "Thinkage.Libraries.Application.Instance";
#if DEBUG
		static System.Globalization.CultureInfo testingCI = new System.Globalization.CultureInfo("es");
		//replace the appObj.CultureInfo below with testingCI to force spanish to appear to test translation
#endif
		protected HttpApplication()
			:base()
		{
			// Arrange to set the current thread's Thinkage.Libraries.Application.Instance at the start of each request and clear it out when done.
			PostAcquireRequestState += delegate(object sender, EventArgs e)
			{
				if (Context.Session != null)  // Context.Session is null on content load of /Content/Site.css file
				{
					var appObj = (APPOBJ)Context.Session[InstanceKey];
					if (appObj != null) { // could be null if exception occurred during Session startup and no app object was successfully created
						Thinkage.Libraries.Application.SetApplicationObject(appObj);
						// Set thread culture info to the application's CultureInfo.
						System.Threading.Thread.CurrentThread.CurrentCulture = appObj.CultureInfo;
						System.Threading.Thread.CurrentThread.CurrentUICulture = appObj.CultureInfo;
					}
				}
			};
			EndRequest += delegate(object sender, EventArgs e)
			{
				Thinkage.Libraries.Application.SetApplicationObject(null);
			};
		}
		protected void Application_PostAuthorizeRequest()
		{
			HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
		}

		#region Application_Start
		// Multiple Application objects exist, one for each simultaneous processing thread, but
		// these methods are 'special' insofar as they are only called on the first Application object created, which might in fact not
		// be the one that actually processes the request! As a result setting event handlers here on 'this' is pointless.
		protected virtual void Application_Start()
		{
			Thinkage.Libraries.Application.SetCustomApplicationInstance(new HttpContextInstanceStore());
		}
		#region ContextInstanceStore
		private class HttpContextInstanceStore : Thinkage.Libraries.Application.IApplicationInstanceStore
		{
			public Application Instance
			{
				get
				{
					return HttpContext.Current == null ? instance : (Application)HttpContext.Current.Items[InstanceKey];
				}
				set
				{
					if (HttpContext.Current == null)
						instance = value;
					else {
						instance = null; // ensure no crap is left from some previous instance
						HttpContext.Current.Items[InstanceKey] = value;
					}
				}
			}
			// Allow this instance store to retain an arbitrary Application object in the event HttpContext.Current is no valid (e.g. on Session_End)
			[ThreadStatic]
			static Application instance;
		}
		#endregion
		#endregion
		#region Application_End
		protected virtual void Application_End(object sender, EventArgs e)
		{
		}
		#endregion

		#region Session_Start
		// These methods do no appear to have corresponding event handlers, so instead they are protected methods called by the surrounding
		// coded-on-the-fly calling framework.
		protected virtual void Session_Start(object sender, EventArgs e)
		{
			// This is called when a new Session is created. This code creates our Thinkage.Libraries.Application object and adds it to the
			// Session state but leaves Application.Instance null.

			// TODO: The following check might have to go. When a session times out we null out Instance but if, before that happens, a new session
			// is created, we will trip here. If the requests are single-file processed and we had an EndRequest that we could rely on it could null
			// out INstance. If requests are handled concurrently by multiple threads there is no way to use Instance.
			//if (Thinkage.Libraries.Application.Instance != null)
			//	throw new GeneralException(KB.K("HTTP state indicates a new session but Application.Instance is non-null"));
			System.Globalization.CultureInfo userCultureInfo = null;
			for (int i = 0; i < Request.UserLanguages.Length && userCultureInfo == null; ++i) {
				try {
					string simpleUserLanguage = Request.UserLanguages[i].Split(';')[0]; // separate any "; q = xx" parameters that might be present
					userCultureInfo = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag(simpleUserLanguage);
					userCultureInfo = System.Globalization.CultureInfo.CreateSpecificCulture(userCultureInfo.Name); // must use Specific cultures in .NET programs (see MSDN)
					System.Threading.Thread.CurrentThread.CurrentCulture = userCultureInfo;
					System.Threading.Thread.CurrentThread.CurrentUICulture = userCultureInfo;
				}
				catch (ArgumentException) { // Unsupported culture, try another
					userCultureInfo = null;
				}
			}
			if (userCultureInfo == null)
				userCultureInfo = System.Globalization.CultureInfo.CurrentCulture;

			try {
				// Note the Current.Request.UserHostName will be "::1" for a local computer if testing locally (meaning 127.0.0.1 for some reason).
				APPOBJ app = CreateNewApplicationObject(userCultureInfo);
				Session.Add(InstanceKey, app); // Unset up object is fine in event of an exception

				app.SetupApplication();
				if (Session.Mode != SessionStateMode.InProc)
					throw new System.Exception("HTTP state mode must be InProc");

				Thinkage.Libraries.Application.SetApplicationObject(null);
			}
			catch (System.Exception gex) {
				this.Session.Add("Exception", gex);
				// The following will expect something to detech the URL /UnHandledException and do something.
				Response.Redirect(GetSiteRoot() + "/UnHandledException", true);
			}
		}
		#region GetSiteRoot
		/// <summary>
		/// Function to formulate the Site root used for this application based on context provided in initial response. This ensures we goto the
		/// proper virtual directory that may have been used to install the application when we get an unhandled exception
		/// </summary>
		/// <returns></returns>
		private string GetSiteRoot()
		{
			var port = Request.ServerVariables["SERVER_PORT"];
			if (String.IsNullOrEmpty(port) || port == "80" || port == "443")
				port = "";
			else
				port = ":" + port;
			var protocol = Request.ServerVariables["SERVER_PORT_SECURE"];
			if (String.IsNullOrEmpty(protocol) || protocol == "0")
				protocol = "http://";
			else
				protocol = "https://";
			var appPath = Request.ApplicationPath;
			if (appPath == "/")
				appPath = "";
			return protocol + Request.ServerVariables["SERVER_NAME"] + port + appPath;
		}
		#endregion
		#endregion
		#region Session_End
		protected virtual void Session_End(object sender, EventArgs e)
		{
			// This is called when a session dies, either by timeout or because we called Abandon on it.
			var endingApplication = (Thinkage.Libraries.Application)Session[InstanceKey];
			// Using the HttpContext InstanceStore on session end requires us to tell it the application instance ourselves since on Session_End HttpContext.Current will be null (the Session end is apparently not an HttpRequest)
			Thinkage.Libraries.Application.SetApplicationObject(endingApplication);
			if (endingApplication != null)
				endingApplication.TeardownApplication(null);
			Session.Remove(InstanceKey);
		}
		#endregion
	}
}
