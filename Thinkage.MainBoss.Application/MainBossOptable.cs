using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using static Thinkage.MainBoss.Database.MB3Client.OptionSupport;
using static Thinkage.MainBoss.Database.MB3Client;

namespace Thinkage.MainBoss.Application {
	/// <summary>
	/// The command line options available to this program
	/// </summary>
	public class MainBossOptable : MB3Client.OptionSupport.DatabaseConnectionOptable {
		public StringValueOption HelpManualPath;
		public KeywordValueOption Mode;
		public BooleanOption CompactBrowsers;
		public StringValueOption CultureInfo;
		public StringValueOption FormatCultureInfo;
		public StringValueOption MessageCultureInfo;
		public MainBossOptable() {
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
			return ResolveSavedOrganization(Mode, CompactBrowsers);
		}
		/// <summary>
		/// Return a NamedOrganization built to specify the compact-browsers and application id from the given command-line options.
		/// </summary>
		/// <param name="applicationOption"></param>
		/// <param name="compactBrowsersOption"></param>
		/// <returns></returns>
		/// <exception cref="GeneralException"></exception>
		/// <exception cref="NoOrganizationException"></exception>
		public NamedOrganization ResolveSavedOrganization(KeywordValueOption applicationOption, BooleanOption compactBrowsersOption) {
			string orgName = null;
			DatabaseEnums.ApplicationModeID appId;
			bool compact;

			if (applicationOption.ExplicitlySet && DecodeApplicationOptionValue(applicationOption.Value) == DatabaseEnums.ApplicationModeID.PickOrganization) {
				// If the user said "pick organization" do not allow any other options.
				// Perhaps in the future we could still allow an organization name, which would just set the list selection for the Pick form...
				// TODO: Use the static constant strings to build Mode option in the the following messages???
				DisallowConnectionOptions(KB.K("Cannot use '{0}' with '/Mode:PickOrganization'"));
				return null;
			}
			NamedOrganization organization;
			ConnectionDefinition connDef;
			try {
				connDef = ResolveConnectionDefinition(out orgName, out organization);
			}
			catch (GeneralException e) {
				throw new NoOrganizationException(e);
			}
			// Get the app mode
			if (applicationOption.ExplicitlySet)
				appId = MB3Client.OptionSupport.DecodeApplicationOptionValue(applicationOption.Value);
			else if (organization != null)
				appId = organization.ConnectionDefinition.ApplicationMode;
			else if (applicationOption.HasValue)
				appId = MB3Client.OptionSupport.DecodeApplicationOptionValue(applicationOption.Value);
			else
				throw new NoOrganizationException(null);

			// Get the CompactBrowsers option
			if (compactBrowsersOption.ExplicitlySet)
				compact = compactBrowsersOption.Value;
			else if (organization != null)
				compact = organization.ConnectionDefinition.CompactBrowsers;
			else if (compactBrowsersOption.HasValue)
				compact = compactBrowsersOption.Value;
			else
				throw new NoOrganizationException(null);

			// It is possible here, despite the earlier check, for the appId to be PickOrganization (either because that is the default for the
			// option or because the saved Organization somehow specified that. Just to protect from this, we check here.
			if (appId == DatabaseEnums.ApplicationModeID.PickOrganization)
				return null;
			// Finally, return a usable NamedOrganization
			MBConnectionDefinition def = new MBConnectionDefinition(appId, compact, connDef.DBServer, connDef.DBName, connDef.DBCredentials);
			// Note that even if we started with a save organization that has an Id, and used it unchanged, our return value is still an unsaved one with null Id.
			return new NamedOrganization(orgName, def);
		}
	}
}
