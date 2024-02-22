using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MBUtility {
	// Common code required to import basic data tables into MainBoss database
	public static class DataImportExportHelper {
		/// <summary>
		///  Common code to setup for MainBoss DataImportExport
		/// </summary>
		/// 
		/// <returns></returns>
		public static void Setup() {
			new ApplicationTblDefaultsNoEditing(Thinkage.Libraries.Application.Instance, new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			new Thinkage.MainBoss.MBUtility.ImportExport.ApplicationImportExport(Thinkage.Libraries.Application.Instance);
		}
		public static void SetupDatabaseAccess(string oName, DBClient.Connection c) {
			MB3Client.ConnectionDefinition connect = (MB3Client.ConnectionDefinition)c;
			System.Version MinDBVersion = new System.Version(1, 1, 5, 5); // MB 4.2
			LicenseEnabledFeatureGroups[] FeatureGroups = new LicenseEnabledFeatureGroups[] {
					new LicenseEnabledFeatureGroups(TIGeneralMB3.ImportExportModeGroup, TIGeneralMB3.CoreLicenseGroup),
					// all feature groups are enabled if the database has at least a NamedUser license for import/export purposes
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.RequestsLicenseGroup),
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.MainBossServiceAsWindowsServiceLicenseGroup),
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.InventoryLicenseGroup),
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.AccountingLicenseGroup),
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.PurchasingLicenseGroup),
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.WorkOrderLaborLicenseGroup),
					new LicenseEnabledFeatureGroups(new [] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense) }, TIGeneralMB3.ScheduledMaintenanceLicenseGroup)
			};
			// Get a connection to the database that we are referencing
			var dbapp = new ApplicationWithSingleDatabaseConnection(Thinkage.Libraries.Application.Instance);
			try {
				var session = new MB3Client(connect);
				dbapp.SetAppAndOrganizationAndSession(oName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, MinDBVersion, dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool - Data Import/Export")));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);
				dbapp.CheckLicensesAndSetFeatureGroups(new[] {
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true) }
				}, FeatureGroups, new Licensing.MBLicensedObjectSet(session), dbapp.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
				dbapp.InitializeUserId();
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;          // message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), connect.DBName, connect.DBServer);
			}
		}
		public static void CheckAndSaveErrors([Invariant]string errorOutputFile, DBIDataSet errors, GeneralException ex) {
			if (ex == null)
				return;
			if (errorOutputFile != null)
				errors.WriteXml(errorOutputFile);
			if (errorOutputFile != null)
				Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Full error details can be found in {0}"), errorOutputFile));
			else
				Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Specify the /ErrorOutput: option to record all error details")));
			throw ex;
		}
		/// <summary>
		/// Validate the user supplied schema identification against our list. Complain if not valid, otherwise return the identifyingName associated with that schema identification.
		/// </summary>
		/// <param name="schemaIdentification"></param>
		/// <returns></returns>
		public static string ValidateSchemaIdentification(string schemaIdentification) {
			IApplicationDataImportExport importer = Libraries.Application.Instance.GetInterface<IApplicationDataImportExport>();
			var identifyingName = importer.FindImportExportNameFromSchemaIdentification(schemaIdentification);
			if (identifyingName != null)
				return identifyingName;
			throw new GeneralException(KB.K("The schema identification {0} does not exist."), schemaIdentification);
		}
	}
}
