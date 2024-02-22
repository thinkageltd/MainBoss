using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Application {
	/// <summary>
	/// The command line options available to this program
	/// </summary>
	public class MainBossOptable : Optable {
		public StringValueOption HelpManualPath;
		public KeywordValueOption Mode;
		public BooleanOption CompactBrowsers;
		public StringValueOption OrganizationName;
		public StringValueOption DataBaseName;
		public StringValueOption DataBaseServer;
		public StringValueOption CultureInfo;
		public StringValueOption FormatCultureInfo;
		public StringValueOption MessageCultureInfo;
		public MainBossOptable() {
			Add(OrganizationName = MB3Client.OptionSupport.CreateOrganizationNameOption(false));
			Add(DataBaseServer = MB3Client.OptionSupport.CreateServerNameOption(false));
			Add(DataBaseName = MB3Client.OptionSupport.CreateDatabaseNameOption(false));

			Add(Mode = MB3Client.OptionSupport.CreateApplicationOption(true, false));
			Mode.Value = MB3Client.OptionSupport.EncodeApplicationOptionValue(DatabaseEnums.ApplicationModeID.Normal);

			Add(CompactBrowsers = MB3Client.OptionSupport.CreateCompactBrowsersOption(false));

			Add(HelpManualPath = new StringValueOption(KB.I("HelpManualPath"), KB.K("The URL location for the documentation").Translate(), false));
			Add(CultureInfo = new StringValueOption(KB.I("CultureInfo"), KB.K("The culture info to use").Translate(), false));
			Add(FormatCultureInfo = new StringValueOption(KB.I("FormatCultureInfo"), KB.K("The culture info to use for formatting").Translate(), false));
			Add(MessageCultureInfo = new StringValueOption(KB.I("MessageCultureInfo"), KB.K("The culture info to use for messages").Translate(), false));
			MarkAsDefaults();
		}
		/// <summary>
		/// Determine the NamedOrganization according to the options provided
		/// </summary>
		/// <returns></returns>
		public NamedOrganization ResolvedSavedOrganization() {
			return MB3Client.OptionSupport.ResolveSavedOrganization(OrganizationName, DataBaseServer, DataBaseName, Mode, CompactBrowsers);
		}
	}
}
