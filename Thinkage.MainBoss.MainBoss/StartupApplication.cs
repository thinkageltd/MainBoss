using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.XAF.UI.MSWindows;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MainBoss {
	public class StartupApplication : Thinkage.Libraries.Application {
		#region Main Program Entry
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[System.STAThread]
		static void Main(string[] args) {
			try {
				if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
					// Clickonce deployments can configure their URLs to include the mainboss parameters as ?param1&param2&...
					// E.g. to change the language (CultureInfo), put ?/ci:es after the deployment url
					// .NET PORTABILITY - SetupInformation and ActivationArguments not supported in .NET Core 3.0
					string[] urlargs = System.AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
					if (urlargs != null && urlargs.Length > 0) {
						string[] splitArgs = urlargs[0].Split('?');
						if (splitArgs.Length > 1)
							args = splitArgs[1].Split('&');
					}
				}
				new StartupApplication(args);
				Thinkage.Libraries.Application.Run();
			}
			// General Exceptions that we generate in the application are reported to the user
			catch (Thinkage.Libraries.GeneralException startupException) {
				Thinkage.Libraries.Application.Instance.DisplayError(startupException);
			}
			// We catch System.Data exceptions and report to the user directly
			catch (System.Data.Common.DbException startupException) {
				Thinkage.Libraries.Application.Instance.DisplayError(startupException);
			}
#if DEBUG
			// For debug mode catch all exceptions to allow the debugger to continue operating rather than simply repositioning back at the original exception with
			// an Unhandled exception state.
			catch (System.Exception startupException) {
				Thinkage.Libraries.Application.Instance.DisplayError(startupException);
			}
#endif
			// For Windows Vista Logo Program, Requirement 3.2, crashes and other unhandled exceptions
			// must report via Windows Error Reporting, hence we cannot intercept System.Exception and report with our
			// own message. We catch only our internally generated GeneralException messages and report them to the user directly
			// rather than leaving them to Windows Error Reporting.
		}

		#endregion

		#region Construction
		public StartupApplication(string[] args) {
			Args = args;
		}
		protected override void CreateUIFactory() {
#if DEBUG
			new MSWindowsDebugProvider(this, KB.I("MainBoss Debug Form"));
			DebugProvider.AddStatus("DB");
#endif
			new StandardApplicationIdentification(this, ApplicationParameters.RegistryLocation, "MainBoss");
			new GUIApplicationIdentification(this, "Thinkage.MainBoss.MainBoss.Resources.MainBoss400.ico");
			new Thinkage.Libraries.XAF.UI.MSWindows.UserInterface(this);
			new Thinkage.Libraries.Presentation.MSWindows.UserInterface(this);
			new MSWindowsUIFactory(this);
		}
		#endregion
		private static string[] Args;
		public override RunApplicationDelegate GetRunApplicationDelegate {
			get { return new RunApplicationDelegate(RunMainBossApplication); }
		}
		#region RunMainBossApplication
		private Thinkage.Libraries.Application RunMainBossApplication() {
			// Check for schema changes without upgrade steps
			MBUpgrader.UpgradeInformation.CheckCurrentSchemaDesignHash(dsMB.Schema);
			MBUpgrader.CheckUpgradeSteps();

			Application.MainBossOptable Options = new Application.MainBossOptable();
			try {
#if PARSE_QUERY_STRING
//Allow URL parameters to be passed if Network Deployed, and parse those parameters as options to mainboss.
// Note these parameters are ONLY passed to the application when launched from a web page; 
// they are stored (but NOT passed) in the application reference shortcut created on the user's start programs menu making them sort of useless.
// This was disabled since it requires use of System.Web (which needs full .NET 4 profile), and it doesn't work anyway.
				if (ApplicationDeployment.IsNetworkDeployed)
				{
					System.Collections.Specialized.NameValueCollection nameValueTable = new System.Collections.Specialized.NameValueCollection();
					if (ApplicationDeployment.CurrentDeployment.ActivationUri != null)
					{ // no query string is provided if not launched from the web page; a 'installed' application from a web page does not retain the query parameters in the Application Reference shortcut
						string queryString = ApplicationDeployment.CurrentDeployment.ActivationUri.Query;
						nameValueTable = System.Web.HttpUtility.ParseQueryString(queryString);
					}
					Options.Parse(nameValueTable);
				}
				else
#endif
				Options.Parse(Args);
				Options.CheckRequired();
			}
			catch (Thinkage.Libraries.CommandLineParsing.Exception ex) {
				throw new GeneralException(ex.InnerException, KB.T(Thinkage.Libraries.Translation.MessageBuilder.Build(ex.Message, Options.Help)));
			}
			if ((Options.MessageCultureInfo.HasValue || Options.FormatCultureInfo.HasValue) && Options.CultureInfo.HasValue)
				throw new GeneralException(KB.K("Use either /CultureInfo or /MessageCultureInfo and /FormatCultureInfo"));
			if (Options.MessageCultureInfo.HasValue) {
				try {
					System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Options.MessageCultureInfo.Value, true);
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Invalid /MessageCultureInfo '{0}'"), Options.MessageCultureInfo.Value);
				}
			}
			if (Options.FormatCultureInfo.HasValue) {
				try {
					System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(Options.FormatCultureInfo.Value, true);
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Invalid /FormatCultureInfo '{0}'"), Options.FormatCultureInfo.Value);
				}
			}
			if (Options.CultureInfo.HasValue) {
				try {
					System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(Options.CultureInfo.Value, true);
					System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Options.CultureInfo.Value, true);
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Invalid CultureInfo '{0}'"), Options.CultureInfo.Value);
				}
			}
			string helpProviderPath = null;
			if (Options.HelpManualPath.HasValue)
				helpProviderPath = Options.HelpManualPath.Value;
			if (helpProviderPath == null)
				helpProviderPath = ApplicationParameters.HelpFileLocalLocation;
			new HelpUsingFolderOfHtml(this, helpProviderPath, Thinkage.Libraries.Application.InstanceMessageCultureInfo, ApplicationParameters.HelpFileOnlineLocation);


			// Note that if the user calls up MB with no arguments and their default organization is scragged they will get an error and the app will exit.
			// When this happens they should run MB with /Mode:PickApp or use MBUtility to turn off their default organization.
			try
			{
				var behavior = MB3Client.OptionSupport.ResolveAllUserConfiguration();
				if( behavior != 0)
					return new PickOrganizationApplication(behavior);

				NamedOrganization o = Options.ResolveSavedOrganization();
				Libraries.Application app;
				if(o != null && (app = MainBossApplication.CreateMainBossApplication(o)) != null)
					return app;
			}
			catch (GeneralException ex)
			{
				Thinkage.Libraries.Application.Instance.DisplayError(ex);
			}
			return new PickOrganizationApplication();
		}
		#endregion
	}
}
