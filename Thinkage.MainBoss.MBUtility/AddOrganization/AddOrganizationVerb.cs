
// TODO: This needs to have the Organization class and its Collection class pried out of the Thinkage.MainBoss.MainBoss project
// and placed maybe in MBAccess. Both classes should be refitted to be less registry-centric; instead they should be passive data structures
// that happen to have methods or helper classes to stream then to/from the registry. Perhaps it belongs in DBAccess and could be passed
// to DBClient construction instead of the raft of individual values currently passed.
// TODO: Also the BasicDataBaseAccessOptable needs some cleaning up regarding the DataBaseConnection option required status and default.
// In the long run I think it will need static methods to build each option individually because we will want /ON in the main optable
// and the other options just in this local verb's optable.
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using System.Collections.Generic;
using Thinkage.Libraries.DBILibrary;

namespace Thinkage.MainBoss.MBUtility {
	internal class AddOrganizationVerb {
		public class Definition : Optable, UtilityVerbDefinition {
			public Definition() : base() {
				Add(OrganizationNameOption = MB3Client.OptionSupport.CreateOrganizationNameOption(true));
				Add(ServerNameOption = MB3Client.OptionSupport.CreateServerNameOption(true));
				Add(DatabaseNameOption = MB3Client.OptionSupport.CreateDatabaseNameOption(true));
				Add(ModeOption = MB3Client.OptionSupport.CreateApplicationOption(false, false));
				ModeOption.Value = MB3Client.OptionSupport.EncodeApplicationOptionValue(DatabaseEnums.ApplicationModeID.Normal);
				Add(CompactBrowsersOption = MB3Client.OptionSupport.CreateCompactBrowsersOption(false));
				Add(AuthenticationMethodOption = MB3Client.OptionSupport.CreateAuthenticationMethodOption(false));
				Add(CredentialsAuthenticationUsernameOption = MB3Client.OptionSupport.CreateCredentialsAuthenticationUsernameOption(false));
				Add(CredentialsAuthenticationPasswordOption = MB3Client.OptionSupport.CreateCredentialsAuthenticationPasswordOption(false));
				Add(ReplaceOption = new BooleanOption(KB.I("Replace"), KB.K("Overwrite any existing organization of the same name").Translate(), '+', false));
				ReplaceOption.Value = false;
				Add(ProbeOption = new BooleanOption(KB.I("Probe"), KB.K("Verify that the organization database is accessible").Translate(), '+', false));
				ProbeOption.Value = false;
				Add(AllUsersOption = new Libraries.CommandLineParsing.BooleanOption(KB.I("AllUsers"), KB.K("Show the AllUsers organization entry instead of the list").Translate(), '+', false));
				AllUsersOption.Value = false;
				Add(SetOrganizationDefaultOption = new BooleanOption(KB.I("SetDefault"), KB.K("Set the default start organization to this entry").Translate(), '+', false));
				SetOrganizationDefaultOption.Value = false;
				// default app, default is Normal TODO: We can't see these, they are away in the app assembly.
				// IfExists action: error, replace, modify; default is error
				MarkAsDefaults();
			}
			public readonly KeywordValueOption ModeOption;
			public readonly BooleanOption CompactBrowsersOption;
			public readonly StringValueOption OrganizationNameOption;
			public readonly StringValueOption ServerNameOption;
			public readonly StringValueOption DatabaseNameOption;
			public readonly BooleanOption ReplaceOption;
			public readonly BooleanOption ProbeOption;
			public readonly BooleanOption AllUsersOption;
			public readonly KeywordValueOption AuthenticationMethodOption;
			public readonly StringValueOption CredentialsAuthenticationUsernameOption;
			public readonly StringValueOption CredentialsAuthenticationPasswordOption;
			public readonly BooleanOption SetOrganizationDefaultOption;
			public Optable Optable {
				get { return this; }
			}
			public string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get { return "AddOrganization"; }
			}
			public void RunVerb() {
				new AddOrganizationVerb(this).Run();
			}
		}
		private AddOrganizationVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			Libraries.DBILibrary.IConnectionInformation connectionInfo;
			if(Options.AllUsersOption.Value) { // ensure a 'database' structure exists by creating it HERE now. Unlike the user's organization list, this 'database' must be explicitly created
				connectionInfo = new SavedOrganizationSessionAllUsers.Connection(writeAccess:true);
				var server = connectionInfo.CreateServer();
				var session = server.CreateDatabase(connectionInfo, dsSavedOrganizations.Schema);
				session.CloseDatabase();
			}
			else
				connectionInfo = new SavedOrganizationSession.Connection();
			MainBossNamedOrganizationStorage connections = new MainBossNamedOrganizationStorage(connectionInfo);
			AuthenticationCredentials credentials = AuthenticationCredentials.Default;
			if(Options.AuthenticationMethodOption.HasValue && (AuthenticationMethod)Options.AuthenticationMethodOption.Value != AuthenticationCredentials.Default.Type) {
				credentials = new AuthenticationCredentials((AuthenticationMethod)Options.AuthenticationMethodOption.Value, Options.CredentialsAuthenticationUsernameOption.Value, Options.CredentialsAuthenticationPasswordOption.Value);
			}
			string organizationName;
			MB3Client.MBConnectionDefinition conn = MB3Client.OptionSupport.ResolveNamedAdHocOrganization(
				Options.OrganizationNameOption, Options.ServerNameOption, Options.DatabaseNameOption, Options.ModeOption, Options.CompactBrowsersOption, credentials, out organizationName);

			List<NamedOrganization> existingOrganizations;
			if((existingOrganizations = connections.GetOrganizationNames(Options.AllUsersOption.Value ? null : organizationName)).Count > 0) {
				if(!Options.ReplaceOption.Value)
					throw new Thinkage.Libraries.GeneralException(KB.K("Organization {0} already exists; use the /+Replace option if you want to replace it"), existingOrganizations[0].DisplayName);
			}
			if(Options.ProbeOption.Value) {
				MB3Client probeSession = null;
				try {
					probeSession = new MB3Client(conn);
				}
				catch(System.Exception ex) {
					throw new Thinkage.Libraries.GeneralException(ex, KB.K("Unable to access database {0} on server {1}"), conn.DBName, conn.DBServer);
				}
				finally {
					if(probeSession != null)
						probeSession.CloseDatabase();
				}
			}
			NamedOrganization o = new NamedOrganization(organizationName, conn);
			if(existingOrganizations.Count > 0)
				o = connections.Replace(existingOrganizations[0], o);
			else
				o = connections.Save(o);
			if(Options.SetOrganizationDefaultOption.HasValue && Options.SetOrganizationDefaultOption.Value)
				connections.PreferredOrganizationId = o.Id;
		}
	}
}
