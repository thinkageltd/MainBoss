using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.MBUtility {
	internal class EditServiceConfigurationVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				// Although this command may someday expand to allow more general editing of the service config record, for now the only choice is to set
				// the Client ID and Secret, and as a side effect clearing out the Client Certificate Name and setting OAuth2 authentication.
				// The options are however optional so you can call the command with no options to test permission to do this.
				Optable.Add(ClientIDOption = new StringValueOption("ClientID", KB.K("Specify the Client ID (App Id of the App Registration in Azure AD)").Translate(), false));
				Optable.Add(ClientSecretOption = new StringValueOption("ClientSecret", KB.K("Specify the Client Secret (password) required to authenticate with the Azure Application").Translate(), false));
				Optable.Add(UseOAUth2Option = new BooleanOption("UseOAuth2", KB.K("Specify whether the MainBoss service should use OAUth2 authentication").Translate(), false));
			}
			public readonly StringValueOption ClientIDOption;
			public readonly StringValueOption ClientSecretOption;
			public readonly BooleanOption UseOAUth2Option;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "EditServiceConfiguration";
				}
			}
			public override void RunVerb() {
				new EditServiceConfigurationVerb(this).Run();
			}
		}
		private EditServiceConfigurationVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			System.Version minDBVersionForRolesTable = new System.Version(1, 1, 5, 15); // The relevant fields appeared at this version
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			new ApplicationTblDefaultsNoEditing(Application.Instance, new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			// Get a connection to the database that we are referencing
			var dbapp = new ApplicationWithSingleDatabaseConnection(Application.Instance);
			try {
				var session = new MB3Client(connect);
				dbapp.SetAppAndOrganizationAndSession(oName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, minDBVersionForRolesTable, dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool--Load Security")));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);
				// We need to pass a LicenseEnabledFeatureGroup that requires no licenses since CheckLicensesAndSetFeatureGroups requires at least one LicenseEnabledFeatureGroup to be enabled.
				// TODO: Why check for any licensing?
				dbapp.CheckLicensesAndSetFeatureGroups(new[] {
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true) }
				}, new LicenseEnabledFeatureGroups[] { new LicenseEnabledFeatureGroups() }, new Licensing.MBLicensedObjectSet(session), dbapp.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
				dbapp.InitializeUserId();
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;          // message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), connect.DBName, connect.DBServer);
			}
			XAFClient db = Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;

			// Test if we have the required permissions
			// We need Edit on the ServiceConfiguration record
			Thinkage.Libraries.XAF.UI.PermissionDisabler failedPermission = null;
			if (failedPermission == null) {
				failedPermission = (Thinkage.Libraries.XAF.UI.PermissionDisabler)Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager.GetPermission(((TableOperationRightsGroup)Root.Rights.Table.FindDirectChild(dsMB.Schema.T.ServiceConfiguration.Name)).Edit);
				if (failedPermission.Enabled)
					failedPermission = null;
			}
			if (failedPermission != null)
				throw new GeneralException(failedPermission.Tip);

			if (Options.ClientIDOption.ExplicitlySet || Options.ClientSecretOption.ExplicitlySet) {
				if (!Options.ClientIDOption.ExplicitlySet || !Options.ClientSecretOption.ExplicitlySet)
					throw new GeneralException(KB.K("/ClientID and /ClientSecret options must be used together"));
				if (string.IsNullOrEmpty(Options.ClientIDOption.Value))
					throw new GeneralException(KB.K("/ClientID must have a non-empty value"));
				if (string.IsNullOrEmpty(Options.ClientSecretOption.Value))
					throw new GeneralException(KB.K("/ClientSecret must have a non-empty value"));
				if (Options.UseOAUth2Option.ExplicitlySet && !Options.UseOAUth2Option.Value)
					throw new GeneralException(KB.K("/ClientID and /ClientSecret options must be used only with OAuth2 enabled"));
				using (dsMB updateDs = new dsMB(db)) {
					// There should only be one record, so we use a row select expression of 'true'.
					// EditSingleRow comment claim it is an error if more than one row is found, but it is actually just an assertion failure.
					// TODO: Fix that at some level.
					var scRow = (dsMB.ServiceConfigurationRow)db.EditSingleRow(updateDs, dsMB.Schema.T.ServiceConfiguration, SqlExpression.Constant(true));
					scRow.F.MailClientID = Options.ClientIDOption.Value;
					scRow.F.MailEncryptedClientSecret = ServicePassword.Encode(Options.ClientSecretOption.Value);
					scRow.F.MailAuthenticationType = (int)DatabaseEnums.MailServerAuthentication.OAuth2;
					scRow.F.MailClientCertificateName = null;
					db.Update(updateDs);
				}
			}
			else if (Options.UseOAUth2Option.ExplicitlySet) {
				if (Options.UseOAUth2Option.Value)
					throw new GeneralException(KB.K("OAuth2 authentication can only be enabled if /ClientID and /ClientSecret are specified"));
				using (dsMB updateDs = new dsMB(db)) {
					// There should only be one record, so we use a row select expression of 'true'.
					// EditSingleRow comment claim it is an error if more than one row is found, but it is actually just an assertion failure.
					// TODO: Fix that at some level.
					var scRow = (dsMB.ServiceConfigurationRow)db.EditSingleRow(updateDs, dsMB.Schema.T.ServiceConfiguration, SqlExpression.Constant(true));
					scRow.F.MailClientID = null;
					scRow.F.MailEncryptedClientSecret = null;
					scRow.F.MailAuthenticationType = (int)DatabaseEnums.MailServerAuthentication.Plain;
					scRow.F.MailClientCertificateName = null;
					db.Update(updateDs);
				}
			}
		}
	}
}