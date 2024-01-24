using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
namespace Thinkage.MainBoss.MBUtility {
	// This command arranges that the current Windows user have MB permissions at least as large as those granted to the creator of a new database.
	// We either use:
	// a new User record (and hide any existing one with the same Windows user)
	// a non-hidden User record for the current Windows user (preferring scoped over unscoped)
	// a hidden User record for the current Windows user which we unhide (preferring most-recently-hidden)
	//
	// Then we use the SecurityDefinitions to add group memberships to that user as if they were the DB creator (although any other memberships are preserved)
	//
	// One problem here is, should we be able to break into "old" databases? Doing so is more difficult because we need to know the creator-permissions
	// for old database versions. Not doing so is also a problem, though: If someone installs new MB then finds they have no one permitted to upgrade the DB,
	// they will have to re-install the old MB to break in.
	// Old databases would pose problems both in terms of our own DB operations tripping on schema mismatches (which can be minized on queries by providing a column list
	// so we aren't tripped up by "new" columns appearing, but can't do much about on Update; perhaps we need to use a subset schema like dsUpgrade), and also in terms
	// of the database-creator group list being incorrect. The latter is more likely since I expect these groups will be in flux for a while.
	//
	// We create new user records with a specific Scope, the domain name (for domain users) or machine name (for machine-local users) of the logged-on user.
	// This should be documented so that if the "break-in" user is to be used long-term, they might want to edit the User record and remove the scope if it is a machine name.
	internal class AddRequestorFromLDAP {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Add(EmailAddresses = new StringValueOption("Emailaddresses", KB.K("The email addresses used to find the Active Directory entry for the users").Translate(), false));
			}
			public StringValueOption EmailAddresses;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "AddRequestorfromactivedirectory";
				}
			}
			public override void RunVerb() {
				new AddRequestorFromLDAP(this).Run();
			}
		}
		private AddRequestorFromLDAP(Definition options) {
			Options = options;
		}
		private readonly Definition Options;

		private void Run() {
			if (!LDAPEntry.IsInDomain())
				throw new GeneralException(KB.K("'{0}' can only work if the computer is in a domain; if you are in a domain, then the domain controller is currently inaccessible"), KB.I("AddRequestorfromactivedirectory"));
			string oName;
			System.Version minDBVersionForRolesTable = new System.Version(1, 0, 4, 38); // The roles table appeared in its current form at this version
			MB3Client.ConnectionDefinition connect = MB3Client.OptionSupport.ResolveSavedOrganization(Options.OrganizationName, Options.DataBaseServer, Options.DataBaseName, out oName);
			// Get a connection to the database that we are referencing
			new ApplicationTblDefaultsNoEditing(Thinkage.Libraries.Application.Instance, new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			var dbapp = new ApplicationWithSingleDatabaseConnection(Thinkage.Libraries.Application.Instance);
			try {
				var session = new MB3Client(connect);
				dbapp.SetAppAndOrganizationAndSession(oName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, minDBVersionForRolesTable, dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool--Create Requestor")));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);
				// We need to pass a LicenseEnabledFeatureGroup that requires no licenses since CheckLicensesAndSetFeatureGroups requires at least one LicenseEnabledFeatureGroup to be enabled.
				// TODO: Why check for any licensing?
				dbapp.CheckLicensesAndSetFeatureGroups(new[] {
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true) },
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.RequestsLicense, overLimitFatal: true) }
				}, new LicenseEnabledFeatureGroups[] { new LicenseEnabledFeatureGroups() }, new Licensing.MBLicensedObjectSet(session), dbapp.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
				dbapp.InitializeUserId();
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;			// message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), connect.DBName, connect.DBServer);
			}
			XAFClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			var EmailAddresses = Options.EmailAddresses.Value.Split(new char[] { ' ', ',', ';' });
			System.Net.Mail.MailAddress se = null;
			foreach (var EmailAddress in EmailAddresses) {
				var e = EmailAddress.Trim();
				if (string.IsNullOrWhiteSpace(e))
					continue;
				try {
					se = Thinkage.Libraries.Mail.MailAddress(e);
				}
				catch (System.FormatException) {
					System.Console.WriteLine(Strings.Format(KB.K("Error: '{0}' is not a valid email address"), e));
				}
				using (dsMB updateDs = new dsMB(db)) {
					var RequestorInfo = new AcquireRequestor(db, se, true, true,  true, null, null, null);
					if (RequestorInfo.Exception != null)
						System.Console.WriteLine(Strings.Format(KB.K("Error: {0}"), Thinkage.Libraries.Exception.FullMessage(RequestorInfo.Exception)));
					if (RequestorInfo.WarningText != null)
						System.Console.WriteLine(Strings.Format(KB.K("Warning: {0}"), RequestorInfo.WarningText));
					if (RequestorInfo.InfoText != null)
						System.Console.WriteLine(RequestorInfo.InfoText);
					else
						System.Console.WriteLine(Strings.Format(KB.K("Requestor '{0}' with email address '{1}' already existed"), RequestorInfo.ContactCode, e));
				}
			}
		}	
		public void ObtainSession() {
			throw new System.NotImplementedException();
		}

		public void ReleaseSession() {
			throw new System.NotImplementedException();
		}
	}
}