using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
namespace Thinkage.MainBoss.MBUtility {
	// Things we want to be able to do here:
	// Add a user (given userName (code), windows User and optional Scope), group memberships
	// Delete (hide) a user (given userName (code))
	// Unhide a user??? (given  userName (code) and a Hidden date)
	// Modify a user (given userName (code), new windows User, new Scope, add/remove group memberships)
	// List all users (optionally include hidden ones)
	//
	// All these verbs share the same permission requirements: same as required by the GUI app, or holding CONTROL permission on the database.
	// THe latter feature is the only added abilities that this has over the GUI user editing, but it is enough to allow "breaking in".
	// Note that changes made by breaking in will be logged in the DB history.
	//
	// TODO: This is getting too complex. What we really want for this is some way of presenting the TBL-coded forms as a console-mode prompting program.
	// All we really need is a "BreakIn" command.
	// All it takes is an optional userName.
	// If the userName is supplied:
	//		try to find a COntact record for that name. If none is found, create one using the given name.
	//		try to find a (possibly hidden) User record associated with the Contact. If none is found, create one using null scope and the current Windows user name.
	// If a non-hidden User record can be found for the current user:
	//		check the contact Code against the userName if supplied; they must match
	// else create a new User record using the current user's NT user name, no scope, and the userName (which at this point becomes required) as the code for the Contact.
	// AAARGH. If there is a non-Hidden user with the correct user and scope we must use it (we can't create a new one without getting a duplicate key). But we should insist that the userName match what's in the contact record.
	// If no non-Hidden record exists for the windows user, look for a Hidden one whose contact matches the userName and unhide it.
	// If still no user record, create one and make a new COntact with the userName. In this case the userName would be required.\
	// Maybe we should have an option controlling whether to use an existing user record, unhide an old one, or create a new one (and hide any conflicting one).
	// The actions would be
	// "grant my user the permissions required to modify users and roles"
	// "Unhide the most recently hidden User of mine [whose Contact matches the given userName] and grant..."
	// "Hide my existing User if any and create a new one with the given userName as the Contact code and grant..."
	// The third choice should not be forced on the user because it causes hidden User records to accumulate.
	// Then grant the User membership in the group(s) required to permit them to modify user and group information using the GUI.
	internal class AddUserVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Optable.Add(AuthenticationCredential = new StringValueOption("AuthenticationCredential", KB.K("Specify the SQL authentication credential of the MainBoss user").Translate(), true));
			}
			public readonly StringValueOption AuthenticationCredential;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "AddUser";
				}
			}
			public override void RunVerb() {
				new AddUserVerb(this).Run();
			}
		}
		private AddUserVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			System.Version minDBVersionForRolesTable = new System.Version(1, 0, 4, 38); // The roles table appeared in its current form at this version
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			// Get a connection to the database that we are referencing
			var dbapp = new ApplicationWithSingleDatabaseConnection(Thinkage.Libraries.Application.Instance);
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
			XAFClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;

			// Test if we have the required permissions
			// We need Create on the User table
			// If group memberships are specified, we need Create on the UserRole table. Since creating a user with no group memberships is kind of useless we always require
			// the latter permission
			Thinkage.Libraries.XAF.UI.PermissionDisabler failedPermission = null;
			if (failedPermission == null) {
				failedPermission = (Thinkage.Libraries.XAF.UI.PermissionDisabler)Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager.GetPermission(Root.Rights.Table[dsMB.Schema.T.User.Name].Create);
				if (failedPermission.Enabled)
					failedPermission = null;
			}
			if (failedPermission == null) {
				failedPermission = (Thinkage.Libraries.XAF.UI.PermissionDisabler)Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager.GetPermission(Root.Rights.Table[dsMB.Schema.T.UserRole.Name].Create);
				if (failedPermission.Enabled)
					failedPermission = null;
			}
			if (failedPermission != null) {
				// See if we can break in
				int? result = null;
				try {
					result = (int?)Thinkage.Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(db.Session.ExecuteCommandReturningScalar(MSSqlServer.INT_NULLABLE_TypeInfo, new MSSqlLiteralCommandSpecification("select has_perms_by_name(db_name(), 'DATABASE', 'CONTROL')")), typeof(int?));
				}
				catch {
				}
				// If not, bye-bye.
				if (!result.HasValue || result.Value == 0)
					throw new GeneralException(failedPermission.Tip);
				// We managed to "break in". Copy our command line into a Database History record.
				using (dsMB updateDs = new dsMB(db)) {
					// TODO: Use the VersionHandler to do this.
					dsMB.DatabaseHistoryRow row = (dsMB.DatabaseHistoryRow)db.AddNewRowAndBases(updateDs, dsMB.Schema.T.DatabaseHistory);
					row.F.Subject = KB.K("User table change attempt using SQL CONTROL permission").Translate();
					System.Text.StringBuilder argumentLine = new System.Text.StringBuilder();
					argumentLine.Append(Strings.IFormat("/{0}:{1}", Options.AuthenticationCredential.OptionName, Options.AuthenticationCredential.Value));
					row.F.Description = argumentLine.ToString();
					db.Update(updateDs);
				}
			}
			using (dsMB updateDs = new dsMB(db)) {
				dsMB.UserRow uRow = (dsMB.UserRow)db.AddNewRowAndBases(updateDs, dsMB.Schema.T.User);
				dsMB.PrincipalRow pRow = uRow.PrincipalIDParentRow;
				dsMB.ContactRow cRow = (dsMB.ContactRow)db.AddNewRowAndBases(updateDs, dsMB.Schema.T.Contact);
				string userName;
				string realm;
				DatabaseCreation.ParseUserIdentification(Options.AuthenticationCredential.Value, out userName, out realm);
				cRow.F.Code = userName;
				uRow.F.AuthenticationCredential = Options.AuthenticationCredential.Value;
				if (failedPermission != null) {
					// Since we logged the attempt, we should also log that it completed.
					// TODO: Use the VersionHandler to do this.
					dsMB.DatabaseHistoryRow row = (dsMB.DatabaseHistoryRow)db.AddNewRowAndBases(updateDs, dsMB.Schema.T.DatabaseHistory);
					row.F.Subject = KB.K("User table change using SQL CONTROL permission completed").Translate();
				}
				db.Update(updateDs);
			}
		}
	}
}