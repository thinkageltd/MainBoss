using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;
namespace Thinkage.MainBoss.MBUtility
{
	internal class BackupVerb
	{
		public class Definition : UtilityVerbWithDatabaseDefinition
		{
			public Definition()
				: base()
			{
				Optable.Add(BackupFileName = new StringValueOption("BackupFile", KB.K("Specify the name of the file to backup the database").Translate(), true));
			}
			public readonly StringValueOption BackupFileName;
			public override string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "Backup";
				}
			}
			public override void RunVerb()
			{
				new BackupVerb(this).Run();
			}
		}
		private BackupVerb(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			// Get a connection to the database that we are referencing to check our permissions to access it and licensing restrictions.
			// First make sure we have a security manager for security checking
			new ApplicationTblDefaultsNoEditing(Thinkage.Libraries.Application.Instance, new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			var dbapp = new ApplicationWithSingleDatabaseConnection(Thinkage.Libraries.Application.Instance);
			// TODO: This needs its own Mode definition, specifying dsMB.Schema.V.MinMBAppVersion, new System.Version(1, 0, 4, 82); // The BackupFileName table appeared at 1.0.4.82 with appropriate restore support, KB.I("MainBoss Utility Tool--Backup")
			try {
				System.Version MinDBVersion = new System.Version(1, 0, 4, 82); // // The BackupFileName table appeared at 1.0.4.82 with appropriate restore support
				var session = new MB3Client(connect);
				dbapp.SetAppAndOrganizationAndSession(oName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, MinDBVersion, dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool--Backup")));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);
				// TODO: Why require ANY licenses for a backup?
				dbapp.CheckLicensesAndSetFeatureGroups(new[] {
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true) }
				}, new LicenseEnabledFeatureGroups[] { new LicenseEnabledFeatureGroups() }, new Licensing.MBLicensedObjectSet(session), dbapp.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
				dbapp.InitializeUserId();
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;			// message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), connect.DBName, connect.DBServer);
			}
			// Now that we have verified we had access to the data base, issue the command to back it up.
			using (dbapp.VersionHandler.GetUnfetteredDatabaseAccess(dbapp.Session)) {
				((MB3Client)dbapp.Session).BackupDatabase(Options.BackupFileName.Value);
			}
			dbapp.CloseDatabaseSession();
		}
	}
}
