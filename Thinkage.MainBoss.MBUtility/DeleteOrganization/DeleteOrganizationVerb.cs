using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using System.Collections.Generic;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.MBUtility {
	internal class DeleteOrganizationVerb {
		public class Definition : Optable, UtilityVerbDefinition {
			public Definition() : base() {
				Add(OrganizationNameOption = MB3Client.OptionSupport.CreateOrganizationNameOption(true));
				Add(AllUsersOption = new Libraries.CommandLineParsing.BooleanOption(KB.I("AllUsers"), KB.K("Delete the AllUsers organization entry").Translate(), '+', false));
				AllUsersOption.Value = false;

				// default app, default is Normal TODO: We can't see these, they are away in the app assembly.
				// IfExists action: error, replace, modify; default is error
				MarkAsDefaults();
			}
			public readonly StringValueOption OrganizationNameOption;
			public readonly BooleanOption AllUsersOption;
			public Optable Optable {
				get {
					return this;
				}
			}
			public string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "DeleteOrganization";
				}
			}
			public void RunVerb() {
				new DeleteOrganizationVerb(this).Run();
			}
		}
		private DeleteOrganizationVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			IConnectionInformation connectionInfo;
			if(Options.AllUsersOption.Value)
				connectionInfo = new SavedOrganizationSessionAllUsers.Connection(writeAccess:true);
			else
				connectionInfo = new SavedOrganizationSession.Connection();
			MainBossNamedOrganizationStorage connections = new MainBossNamedOrganizationStorage(connectionInfo);
			string organizationName = Options.OrganizationNameOption.Value;
			List<NamedOrganization> existingOrganizations;
			if((existingOrganizations = connections.GetOrganizationNames(organizationName)).Count == 0)
				throw new Thinkage.Libraries.GeneralException(KB.K("Organization {0} does not exist."), organizationName);
			connections.Delete(existingOrganizations[0]);
		}
	}
}
