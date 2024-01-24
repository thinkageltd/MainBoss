using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MBUtility
{
	// Common code required to for Customization support
	public class Customization : Thinkage.Libraries.Presentation.Customization
	{
		public static void SetupDatabaseAccess(string oName, DBClient.Connection c)
		{
			MB3Client.ConnectionDefinition connect = (MB3Client.ConnectionDefinition)c;
			System.Version minDBVersionForCustomization = new System.Version(1, 0, 10, 12); // MB 3.5
			// Get a connection to the database that we are referencing
			var dbapp = new ApplicationWithSingleDatabaseConnection(Thinkage.Libraries.Application.Instance);
			try {
				var session = new MB3Client(connect);
				dbapp.SetAppAndOrganizationAndSession(oName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, minDBVersionForCustomization, dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool - Customization Import/Export")));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;			// message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), connect.DBName, connect.DBServer);
			}
		}

	}
}
