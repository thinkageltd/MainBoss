using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// A common place for parameters that are shared across programs in the same application family.
	/// </summary>
	static public class ApplicationParameters {
		public static string HelpFileLocalLocation {
			get {
				return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), KB.I("help"));
			}		
		}
		public static string HelpFileOnlineLocation {
			get {
				return Strings.IFormat("https://mainboss.com/manual/{0}.{1}.{2}/HtmlHelp/{3}", VersionInfo.ProductVersion.Major, VersionInfo.ProductVersion.Minor, VersionInfo.ProductVersion.Build, Thinkage.Libraries.Application.InstanceMessageCultureInfo.TwoLetterISOLanguageName);
			}
		}
		/// <summary>
		/// Location in the registry where the application may store things
		/// </summary>
		public static readonly string RegistryLocation = KB.I("MainBoss");
		public static readonly string ApplicationDisplayName = KB.I("MainBoss");
	}
}
