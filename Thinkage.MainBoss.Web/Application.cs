using System;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation.ASPNet;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;

namespace Thinkage.MainBoss.Web
{
	public class MBWApplication : TblApplication {
		protected override void CreateUIFactory() {
			throw new NotImplementedException();
		}
		public new static MBWApplication Instance {
			get {
				return (MBWApplication)Thinkage.Libraries.Application.Instance;
			}
		}
		static Version MinDBVersion = new Version(1, 0, 2, 63); // assigned to variable so it can be found in Search of MinDBVersion
		static string ApplicationName = KB.I("MainBoss Maintenance Manager web server");
		internal static MBWApplication CreateRootApplicationObject()
		{
			// This application object is created when the server is first initialized and lives for the duration of the service.
			MBWApplication result = new MBWApplication();
			// TODO: Perhaps the rest of this should be in SetupApplication
			// Connect to the database (eventually reading perhaps the web.config file to get the db connect information)
			new ApplicationTblDefaultsUsingWeb(result, new ETbl(), new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.Rights.Action.Customize);
			new StandardApplicationIdentification(result, ApplicationParameters.RegistryLocation, "MainBoss");
			var dbapp = new ApplicationWithSingleDatabaseConnection(result);
			// TODO: Need a different DatabaseEnums.ApplicationModeID for the web client
			// TODO: TblPage should do this wrapped in a try/catch that prints messages (it should expect the interface to exist and needs a way to get all the params;
			// perhaps there should be a IWebApplicationSetup interface). Currently if the DB cannot be accessed you get an unhandled exception page from IIS.
			// Force creation of all the Tbl's. Note that calling TIGeneralMB3.FindTbl does not do this! The method is actually inherited
			// from a base class (TblRegistry) so the call only elaborates the base class.
			ConfigHandler config = ConfigHandler.FetchConfig();

			Database.MBRestrictions.DefineRestrictions();	// This should really be implicit in elaboration of dsMB.Schema
			try {
				LicenseEnabledFeatureGroups[] featureGroups = new LicenseEnabledFeatureGroups[] {
						new LicenseEnabledFeatureGroups(TIGeneralMB3.WebModeGroup, TIGeneralMB3.CoreLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.WorkOrdersLicense) }, TIGeneralMB3.WorkOrdersLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.MainBossLegacyLicense) }, TIGeneralMB3.WorkOrdersLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.RequestsLicense) }, TIGeneralMB3.RequestsLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.MainBossServiceLicense) }, TIGeneralMB3.MainBossServiceLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.InventoryLicense) }, TIGeneralMB3.InventoryLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.AccountingLicense) }, TIGeneralMB3.AccountingLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.PurchasingLicense) }, TIGeneralMB3.PurchasingLicenseGroup),
						new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.ScheduledMaintenanceLicense) }, TIGeneralMB3.ScheduledMaintenanceLicenseGroup)
				};
				var session = new MB3Client(new MB3Client.MBConnectionDefinition(DatabaseEnums.ApplicationModeID.Normal, false, config.DatabaseServer, config.DatabaseName));
				dbapp.SetAppAndOrganizationAndSession(config.OrganizationName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, MinDBVersion, null, ApplicationName));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.WebServer);
				// TODO: Why doesn't this require the MainBossWebLicense or the WebRequestsLicense?
				dbapp.CheckLicensesAndSetFeatureGroups(new[] {
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.MainBossLegacyLicense, overLimitFatal: true) },
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true) }
				}, featureGroups, new Licensing.MBLicensedObjectSet(session), dbapp.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;			// message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), config.DatabaseName, config.DatabaseServer);
			}
			return result;
		}
		internal static MBWApplication CreateClientApplicationObject(string userName, string clientSystemName, System.Globalization.CultureInfo userCulture, System.Security.Principal.IPrincipal securityPrincipal) {
			// This application object is created for each new client. It does not check licenses since that was already done by the root app object.
			MBWApplication result = new MBWApplication();
			// TODO: Perhaps the rest of this should be in SetupApplication
			// Connect to the database (eventually reading perhaps the web.config file to get the db connect information)
			new FixedUserInformationOverride(result, userName, clientSystemName, userCulture, securityPrincipal);
			new ApplicationTblDefaultsUsingWeb(result, new ETbl(), new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.Rights.Action.Customize);
			new StandardApplicationIdentification(result, ApplicationParameters.RegistryLocation, "MainBoss");
			var dbapp = new ApplicationWithSingleDatabaseConnection(result);
			// TODO: Need a different DatabaseEnums.ApplicationModeID for the web client
			// TODO: TblPage should do this wrapped in a try/catch that prints messages (it should expect the interface to exist and needs a way to get all the params;
			// perhaps there should be a IWebApplicationSetup interface). Currently if the DB cannot be accessed you get an unhandled exception page from IIS.
			// Force creation of all the Tbl's. Note that calling TIGeneralMB3.FindTbl does not do this! The method is actually inherited
			// from a base class (TblRegistry) so the call only elaborates the base class.
			ConfigHandler config = ConfigHandler.FetchConfig();

			try {
				var session = new MB3Client(new MB3Client.MBConnectionDefinition(DatabaseEnums.ApplicationModeID.Normal, false, config.DatabaseServer, config.DatabaseName));
				dbapp.SetAppAndOrganizationAndSession(config.OrganizationName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, MinDBVersion, null, ApplicationName));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.WebSession);
				dbapp.InitializeUserId();
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;			// message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), config.DatabaseName, config.DatabaseServer);
			}
			return result;
		}
		public override void TeardownApplication(Thinkage.Libraries.Application nextApplication) {
			((ApplicationWithSingleDatabaseConnection)GetInterface<IApplicationWithSingleDatabaseConnection>()).CloseDatabaseSession();
			base.TeardownApplication(nextApplication);
		}
		public readonly Guid Id = Guid.NewGuid();
	}
}
