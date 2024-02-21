using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.TypeInfo;

namespace Thinkage.MainBoss.Database.Service {
	//
	// working info about a service, could come from the running cmd line, the cmd line for the service record or the the MainBoss Configuation record
	//
	// Note the user supplies the ServiceCode
	//
	// The user can not directly change the ServiceName is set from the ServiceCode when the service is installed, and null otherwise
	// the Computer is set to the current machine name when the service is install and null otherwise
	// the Userid is set to the Userid of the installed service and null otherwise
	// the MBVersion is set to the current version of the MainBoss Server when the service is install and null otherwise
	//
	// Because of critical section problems
	// the following order is always preserved.
	// The ServiceName, Computer, Userid are stored in the MainBoss Service Record just before the service is created,updated, or deleted
	// the Version is set after the service has be created, updated, or deleted.
	//  
	#region ServiceParms
	[Thinkage.Libraries.Translation.Invariant]
	public struct ServiceParms {
		private string pServiceComputer;
		public string ServiceComputer {
			get { return pServiceComputer; }
			set { pServiceComputer = string.IsNullOrWhiteSpace(value) ? DomainAndIP.MyDnsName : value; }
		}
		public string ServiceCode;
		public string MBVersion;
		private string pServiceUserid;
		public string ServiceUserid {
			get { return pServiceUserid; }
			set { pServiceUserid = string.IsNullOrWhiteSpace(value) ? null : value; }
		}
		private string pServicePassword;
		public string ServicePassword {
			get { return pServicePassword; }
			set { pServicePassword = string.IsNullOrWhiteSpace(value) ? null : value; }
		}
		public SQLConnectionInfo ConnectionInfo;
		public ServiceParms UpdateWith(ServiceParms n) {
			if (string.Compare(ServiceComputer, n.ServiceComputer, true) != 0)
				ServiceComputer = n.ServiceComputer;
			if (string.Compare(ServiceCode, n.ServiceCode, true) != 0)
				ServiceCode = n.ServiceCode;
			if (string.Compare(MBVersion, n.MBVersion, true) != 0)
				MBVersion = n.MBVersion;
			if (string.Compare(ServiceUserid, n.ServiceUserid, true) != 0)
				ServiceUserid = n.ServiceUserid;
			if (string.Compare(ServicePassword, n.ServiceUserid, true) != 0)
				ServicePassword = n.ServicePassword;
			if (ConnectionInfo == null || ConnectionInfo.ConnectionString != n.ConnectionInfo.ConnectionString)
				ConnectionInfo = n.ConnectionInfo;
			return this;
		}
		public void Clear() {
			ServiceUserid = null;
			ServicePassword = null;
			ServiceComputer = null;
			MBVersion = null;
		}
		public string WorkingServiceUserid {
			get {
				if (ServiceUserid == null)
					return Utilities.NETWORKSERVICE_REGISTRYNAME;
				return Utilities.UseridToRegistryName(ServiceUserid);
			}
		}
		public string SqlUseridFormatted {
			get {
				var displayname = Utilities.UseridToDisplayName(ConnectionInfo.SqlUserid ?? WorkingServiceUserid);
				var externalname = Utilities.IsComputerLocalName(displayname) ? getExternalMachineUserid(displayname) : null;
				if (externalname == null) return displayname;
				return !Utilities.IsComputerLocalName(displayname) ? Strings.IFormat("'{0}'", displayname) : Strings.Format(KB.K("'{0}' (external name '{1}')"), displayname, externalname);
			}
		}
		public string ServiceUseridAsSqlUserid {
			get {
				var u = Utilities.UseridToDisplayName(WorkingServiceUserid);
				if (DomainAndIP.IsThisComputer(ServiceComputer))
					return u;
				if (ServiceComputer != null && Utilities.IsComputerLocalName(WorkingServiceUserid))
					return getExternalMachineUserid(u);
				return u;
			}
		}
		private static string getExternalMachineUserid(string u) {
			var name = DomainAndIP.GetDomainName();
			if( name != null )
				return Strings.IFormat("{0}\\{1}$", name, Environment.MachineName);
			return null;
		}
		public bool WindowsAccountExists {
			get {
				var n = WorkingServiceUserid;
				if (n == Utilities.LOCALADMIN_REGISTRYNAME || n == Utilities.LOCALSERVICE_REGISTRYNAME || n == Utilities.NETWORKSERVICE_REGISTRYNAME)
					return true;
				try {
					NTAccount acct = new NTAccount(n);
					SecurityIdentifier id = (SecurityIdentifier)acct.Translate(typeof(SecurityIdentifier));
					return id.IsAccountSid();
				}
				catch (IdentityNotMappedException) {
					/* Invalid user account */
					return false;
				}
			}
		}
	}
	#endregion
	#region ServiceUtilities
	static public class ServiceUtilities {
		static public IServiceLogging VerifyDatabaseAndAcquireLog(MB3Client.ConnectionDefinition DBConnection, string serviceCode) {
			// check if log is available and the configuration looks good 
			// these test is done synchronously which enables better error reporting. 
			// we check the existence of the log file
			// check the licensing 
			// check the existence of the config record and minor error checking on its contents 
			// if we are running a service we wait for a correct configuration to exist.
			// if we are running interactive there is no waiting.
			// the database session to the log file is then closed and the session will be reopened but on a different thread
			// we will get cross thread error if we try to use the same session
			// the checking of existences and licensing is only done at service startup.
			// there should be code to do it on a command so the service would not needed to be restarted.
			var serviceLogSession = ObtainServiceLogSession(DBConnection, serviceCode);
			if (serviceLogSession == null)
				return null;
			var logging = new LogToUserAndDatabase(DBConnection);
			logging.ProcessLog();
			if (!VerifyLicense(serviceLogSession, logging)) {
				//TODO: who is responsible now for logging disposal ?
				// not an important problem since this null return will cause the program to exit
				return null;
			}
			serviceLogSession.CloseDatabase();
			return logging;
		}
		public static MB3Client ObtainServiceLogSession(MB3Client.ConnectionDefinition DBConnection, string serviceCode) {
			Thinkage.Libraries.DBAccess.DBVersionHandler VersionHandler = null;
			MB3Client logSession = null;
			string oldmessage = null;
			DateTime lastMessageOutput = DateTime.Now;
			while (true) {
				try {
					logSession = new MB3Client(DBConnection);
					logSession.Session.LockShared();
					Version minDBVersionForServiceLog = new Version(1, 0, 10, 28); // Version where ServiceLog appeared
					VersionHandler = MBUpgrader.UpgradeInformation.CheckDBVersion(logSession, VersionInfo.ProductVersion, minDBVersionForServiceLog, dsMB.Schema.V.MinAReqAppVersion, KB.I("MainBoss Service"));
					break;
				}
				catch (ApplicationUpgradeRequiredException) {
					throw;
				}
				catch (System.Exception e) {
					if (!UserInterface.IsRunningAsAService)
						throw;
					// we can't get at the database, it may not be there or it may be temporarily down
					// so we just keep trying, and hopefully someone will fix it.
					if (oldmessage != e.Message || DateTime.Now - lastMessageOutput > new TimeSpan(1, 0, 0)) {
						Thinkage.Libraries.Application.Instance.DisplayError(Thinkage.Libraries.Strings.IFormat("{0}: {1}", serviceCode, Thinkage.Libraries.Exception.FullMessage(e)));
						lastMessageOutput = DateTime.Now;
						oldmessage = e.Message;
					}
					logSession = null;
					System.Threading.Thread.Sleep(60 * 1000);
				}
			}
			// Busy the database and check the licensing
			logSession.ObtainSession((int)DatabaseEnums.ApplicationModeID.MainBossService);
			return logSession;
		}
		public static bool HasRequestLicense = false;
		public static bool HasWebRequestsLicense = false;
		public static bool HasWebAccessLicense = false;
		static bool licenseErrorReported = false;
		/// <summary>
		/// The Action to do if no demoAction is specified to VerifyLicense
		/// </summary>
		static Action defaultDemoAction = new Action(() => { });
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		static public bool VerifyLicense(MB3Client DBSession, IServiceLogging logging, Action demoAction = null) {
			bool logErrors = logging != null;
			DBVersionHandler VersionHandler = null;
			// validate the databaselog
			try {
				Version MinDBVersion = new Version(1, 1, 5, 15); // Usually kept in lock step with mainboss application; changing this should be reflected in the checkin comment to vault
																// MinMBAppVersion; 4.2; MinDBVersion; changing this should be reflected in the checkin comment to vault
				VersionHandler = MBUpgrader.UpgradeInformation.CheckDBVersion(DBSession, VersionInfo.ProductVersion, MinDBVersion, dsMB.Schema.V.MinMBRemoteAppVersion, KB.I("MainBoss Service"));
			}
			catch (Thinkage.Libraries.GeneralException e) { // Catch any version errors at the outset and terminate the service. User will see in event log that the wrong service version is running.
				if (logErrors)
					logging.LogError(Thinkage.Libraries.Exception.FullMessage(e));
				return false;
			}

			var licensedObject = new MBServiceLicensedObjectSet(demoAction ?? defaultDemoAction, DBSession);
			try {
				LicenseManager.CheckLicensesAndEnableFeatures(
					new[] {
					new []  { new LicenseRequirement(Licensing.MainBossServiceLicense),
							  new LicenseRequirement(Licensing.RequestsLicense, overLimitFatal:true)
							}
					},
					new ILicenseEnabledFeature[] {
						new LicenseEnabledFeature(Licensing.RequestsLicense,        delegate() { HasRequestLicense = true; }),
						new LicenseEnabledFeature(Licensing.WebRequestsLicense,     delegate() { HasWebRequestsLicense = true; }),
						new LicenseEnabledFeature(Licensing.WebAccessLicense,       delegate() { HasWebAccessLicense = true; }),
					},
					licensedObject, VersionHandler.GetLicenses(DBSession), null);
				if (logErrors && licenseErrorReported) {
					logging.LogInfo(Strings.Format(KB.K("Licensing errors have been fixed")));
					licenseErrorReported = false;
				}
			}
			catch (NoLicenseException) {
				if (logErrors) {
					if (!licenseErrorReported)
						logging.LogError(Strings.Format(KB.K("There is no valid license for the MainBoss Service")));
					licenseErrorReported = true;
				}
				return false;
			}
			catch (LicenseCountExceededException e) { // A nonexistent RequestLicense does not cause the exception
				if (logErrors) {
					if (!licenseErrorReported)
						logging.LogError(Strings.Format(KB.K("{0}. No email requests will be processed until the problem has been fixed"), e.Message));
					licenseErrorReported = true;
				}
				HasRequestLicense = false;
			}
			return true;
		}
		private class MBServiceLicensedObjectSet : Licensing.MBLicensedObjectSet {
			public MBServiceLicensedObjectSet(Action mbs, MB3Client DBSession)
				: base(DBSession) {
				MBS = mbs;
			}
			private readonly Action MBS;
			public override void EnforceDemonstrationLimits(System.Collections.Generic.IEnumerable<int> fullyLicensedApplicationIDs
				, System.Collections.Generic.IEnumerable<int> demonstrationApplicationIDs, System.Text.StringBuilder warningsCollector) {
				base.EnforceDemonstrationLimits(fullyLicensedApplicationIDs, demonstrationApplicationIDs, warningsCollector);
				foreach (int appId in demonstrationApplicationIDs)
					if (appId == (int)Thinkage.MainBoss.Database.Licensing.ApplicationID.MainBossService) {
						System.Diagnostics.Debug.Assert(MBS != null);
						MBS();
					}
			}
		}
		public static void VerifyServiceParameters(ServiceConfiguration serviceConfiguration, ServiceParms dbParms, IServiceLogging logTo) {
			if (serviceConfiguration != null && serviceConfiguration.LastError == null) {
				if (serviceConfiguration != null && serviceConfiguration.StartType == System.ServiceProcess.ServiceStartMode.Disabled) {
					serviceConfiguration.Updatable();
					logTo.LogWarning(Strings.Format(KB.K("Windows Service for MainBoss '{0}' has been disabled"), serviceConfiguration.ServiceName));
				}
				if (serviceConfiguration != null && serviceConfiguration.StartType == System.ServiceProcess.ServiceStartMode.Manual)
					logTo.LogWarning(Strings.Format(KB.K("Start mode of MainBoss Service '{0}' is 'Manual'. It should be set to 'Automatic'"), serviceConfiguration.ServiceName));
				if (dbParms.ServiceUserid != null && Utilities.UseridToDisplayName(serviceConfiguration.StartName) != Utilities.UseridToDisplayName(dbParms.ServiceUserid))
					logTo.LogWarning(Strings.Format(KB.K("The Service Configuration expects the service to be running as user '{0}'. The MainBoss Service is configured to be running as '{1}'")
						, dbParms.ServiceUserid, Utilities.UseridToDisplayName(serviceConfiguration.StartName)));
			}
		}

		static public ServiceParms AcquireServiceRecordInfo(SQLConnectionInfo ConnectionInfo, MB3Client.ConnectionDefinition DBConnection, string serviceCode, IServiceLogging logTo) {
			MB3Client DBSession = null;
			ServiceParms parms = new ServiceParms();

			try {
				DBSession = new MB3Client(DBConnection);
				using (dsMB ds = new dsMB(DBSession)) {
					ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.ServiceConfiguration);
					dsMB.ServiceConfigurationRow sr = null;
					dsMB.ServiceConfigurationRow anysr = null;
					int numrow = 0;
					var rows = dsMB.Schema.T.ServiceConfiguration.GetDataTable(ds).Rows;
					foreach (dsMB.ServiceConfigurationRow r in dsMB.Schema.T.ServiceConfiguration.GetDataTable(ds).Rows) {
						anysr = r;
						numrow++;
						if (string.Compare(r.F.Code, serviceCode, ignoreCase: true) == 0) {
							sr = r;
							break;
						}
					}
					if (numrow == 0)
						throw new GeneralException(KB.K("Unable to find any MainBoss Service records in {0}"), DBSession.ConnectionInfo.DisplayName);
					if (numrow == 1 && serviceCode == null) // only one, no information give we will use it.
						sr = anysr;
					if (sr == null)
						throw new GeneralException(KB.K("Unable to find the MainBoss Service record for '{0}' in {1}"), serviceCode, DBSession.ConnectionInfo.DisplayName);
					else {
						try {
							parms.ServiceCode = sr.F.Code;
							parms.ConnectionInfo = ConnectionInfo;
							parms.ServiceUserid = sr.F.ServiceAccountName;
							parms.ServiceComputer = sr.F.ServiceMachineName;
							parms.MBVersion = sr.F.InstalledServiceVersion;
						}
						catch (System.Exception e) { // user has MainBoss looks after SQL permissions and this user don't have perms on the database.
							logTo.LogError(Strings.Format(KB.K("Unable to access the MainBoss Service record for '{0}' in {1}.  "
									+ "User {2} must have SQL Administrator privileges on the SQL server.  {3}"
									), serviceCode, DBSession.ConnectionInfo.DisplayName, Environment.UserName, Thinkage.Libraries.Exception.FullMessage(e)));
						}
					}
				}
			}
			catch (GeneralException) {
				throw;
			}
			catch (System.Exception e) { // can't get the service record
				if (serviceCode == null)
					throw new GeneralException(e, KB.K("Unable to access any MainBoss Service records in {0}.  "
									+ "User '{1}' must have SQL Administrator privileges on the SQL server."), DBSession.ConnectionInfo.DisplayName, Environment.UserName);

				throw new GeneralException(e, KB.K("Unable to access the MainBoss Service record for '{0}' in {1}.  "
						+ "User '{2}' must have SQL Administrator privileges on the SQL server."), serviceCode, DBSession.ConnectionInfo.DisplayName, Environment.UserName);
			}
			finally {
				if (DBSession != null)
					DBSession.CloseDatabase();
			}
			return parms;
		}
		public static void DeleteServiceFromDatabase(MB3Client.ConnectionDefinition DBConnection, ServiceParms parms, IServiceLogging log) {
			try {
				parms.Clear();
				ServiceUtilities.UpdateServiceRecord(DBConnection, parms, log);
			}
			catch (System.Exception) { }
		}
		static public bool UpdateServiceRecord(MB3Client.ConnectionDefinition DBConnection, ServiceParms parms, IServiceLogging logTo) {
			MB3Client DBSession = null;
			bool changed = false;
			try {
				DBSession = new MB3Client(DBConnection);
				using (dsMB ds = new dsMB(DBSession)) {
					ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.ServiceConfiguration);
					dsMB.ServiceConfigurationRow sr = null;
					foreach (dsMB.ServiceConfigurationRow r in dsMB.Schema.T.ServiceConfiguration.GetDataTable(ds).Rows)
						if (string.Compare(r.F.Code, parms.ServiceCode, ignoreCase: true) == 0) {
							sr = r;
							break;
						}
					if (sr == null)
						logTo.LogError(Strings.Format(KB.K("Unable to find the MainBoss Service record for '{0}' in {1}"), parms.ServiceCode, DBSession.ConnectionInfo.DisplayNameLowercase));
					else {
						try {
							if (parms.MBVersion == null) {
								sr.F.InstalledServiceVersion = null;
								sr.F.ServiceMachineName = null;
								sr.F.ServiceAccountName = null;
								sr.F.SqlUserid = null;
								changed = true;
							}
							else {
								if (parms.ServiceCode != sr.F.Code) {
									changed = true;
									sr.F.Code = parms.ServiceCode;
								}
								if (parms.SqlUseridFormatted != sr.F.SqlUserid) {
									changed = true;
									sr.F.SqlUserid = parms.SqlUseridFormatted;
								}
								if (parms.ServiceComputer != sr.F.ServiceMachineName) {
									changed = true;
									sr.F.ServiceMachineName = parms.ServiceComputer;
								}
								if (parms.WorkingServiceUserid != Utilities.UseridToRegistryName(sr.F.ServiceAccountName)) {
									changed = true;
									sr.F.ServiceAccountName = Utilities.UseridToDisplayName(parms.WorkingServiceUserid);
								}
								if (parms.MBVersion != sr.F.InstalledServiceVersion) {
									sr.F.InstalledServiceVersion = parms.MBVersion;
									changed = true;
								}
							}
							if (changed)
								ds.DB.Update(ds);
						}
						catch (System.Exception e) { // user has MainBoss looks after SQL permissions and this user doesn't have perms on the database.
							logTo.LogError(Strings.Format(KB.K("Unable to update the Windows Service for MainBoss record for '{0}' in database {1}.  User {2} must have SQL Administrator privileges on the SQL server.  {3}"
									), parms.ServiceCode, DBSession.ConnectionInfo.DisplayNameLowercase, Environment.UserName, Thinkage.Libraries.Exception.FullMessage(e)));
						}
					}
				}
			}
			catch (System.Exception e) { // can't get the service record
				logTo.LogError(Strings.Format(KB.K("Unable to update the Windows Service for MainBoss record for '{0}' in {1}.  "
						+ "User '{2}' must have SQL Administrator privileges on the SQL server.  {3}"), parms.ServiceCode, DBSession.ConnectionInfo.DisplayNameLowercase, Environment.UserName, Thinkage.Libraries.Exception.FullMessage(e)));
			}
			if (DBSession != null)
				DBSession.CloseDatabase();
			return changed;
		}

		#region CreateMainBossService
		public static ServiceParms CreateMainBossService(MB3Client.ConnectionDefinition DBConnection, ServiceParms dbParms, [Libraries.Translation.Invariant]string serviceBinary, IServiceLogging logging) {
			var description = KB.K("Interacts with your local mail system "
					+ "to send acknowledgments to requestors and to assignees of requests, "
					+ "work orders and purchase orders. If this service is disabled, all "
					+ "features that depend on it will be queued up until the service is "
					+ "enabled again.");
			var config = new StaticServiceConfiguration(dbParms.ServiceComputer, dbParms.ServiceCode);
			try {
				if (dbParms.ConnectionInfo == null)
					throw new GeneralException(KB.K("Cannot create a MainBoss Service without a valid MainBoss Database and Database Server"));
				dbParms.ServiceComputer = DomainAndIP.MyDnsName;
				dbParms.MBVersion = Thinkage.Libraries.VersionInfo.ProductVersion.ToString();

				ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, logging);

				var commandline = ServiceConfiguration.ServiceCmdLine(serviceBinary, dbParms.ConnectionInfo, Thinkage.Libraries.VersionInfo.ProductVersion.ToString(), dbParms.ServiceCode);
				try {
					ServiceInstaller.Install(dbParms.ServiceCode, Strings.IFormat("MainBoss Service under name '{0}'", config.ServiceName), commandline, null, dbParms.ServiceUserid, dbParms.ServicePassword, description.Translate());
				}
				catch (System.Exception) {
					dbParms.Clear();
					ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, logging);
					throw;
				}
				System.Diagnostics.EventLog.WriteEntry("MainBoss", Strings.IFormat("MainBoss Service has been installed"), System.Diagnostics.EventLogEntryType.Information);
				logging.LogInfo(Strings.Format(KB.K("A MainBoss Service named '{0}' created on '{1}' "), config.ServiceName, config.ServiceMachineName));
				return dbParms;
			}
			catch (GeneralException) {
				throw;
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Cannot install MainBoss Service"));
			}
		}
		#endregion
		#region TryAccessToDatabase
		public static GeneralException TryAccessToDatabase(ServiceConfiguration serviceConfiguration, ServiceParms parms) {
			if (serviceConfiguration?.ConnectionInfo != null && string.Compare(serviceConfiguration.ConnectionInfo.ConnectionString, parms.ConnectionInfo.ConnectionString, true) != 0
				&& serviceConfiguration.Credentials.Type != Libraries.DBILibrary.AuthenticationMethod.WindowsAuthentication) {
				// if the connection strings are different see if the one recored in the service, will work.
				// if it windows authentication we cannot do the check.
				try {
					var serviceAccess = serviceConfiguration.ConnectionInfo.DefineConnection();
					var session = new MB3Client(serviceAccess);
					session.CloseDatabase();
				}
				catch (System.Exception e) {
					if (e is GeneralException)
						return e as GeneralException;
					return new GeneralException(e, KB.K("The SQL Credentials {0} for the MainBoss Service do not allow connection to the database"), serviceConfiguration.Credentials.ToString());
				}
			}
			return null;
		}
		#endregion
		#region CheckServiceCmdLine
		public static ServiceParms CheckServiceCmdLine(ServiceConfiguration sc, ServiceParms dbParms, string serviceBinary, bool update, bool force, Version minVersion) {
			Libraries.Exception baseError = null;
			if (!force)
				baseError = TestForMainBossServiceVersion(sc, dbParms, serviceBinary, minVersion);
			if (baseError != null && !update && dbParms.WorkingServiceUserid != Utilities.UseridToRegistryName(sc.StartName))
				Thinkage.Libraries.Application.Instance.DisplayWarning(Strings.Format(KB.K("The Windows Service for MainBoss '{0}' is configured to run as user '{1}' but the MainBoss Service configuration record shows the service to be running as '{2}'."), sc.ServiceName, sc.StartName, dbParms.ServiceUserid));
			return dbParms;
		}
		public static ServiceParms SynchronizeServiceInfo(ServiceConfiguration sc, ServiceParms dbParms, string serviceBinary) {
			sc.ConnectionInfo = dbParms.ConnectionInfo;
			sc.ServiceExecutable = serviceBinary;
			if (dbParms.ServiceUserid != null) {
				sc.StartName = Utilities.UseridToRegistryName(dbParms.ServiceUserid);
				sc.Password = dbParms.ServicePassword;
			}
			sc.Version = Thinkage.Libraries.VersionInfo.ProductVersion.ToString();
			try {
				sc.Commit();
			}
			catch (System.Exception ex) {
				throw new GeneralException(ex, KB.K("Unable to update the Windows Service for MainBoss '{0}'"), sc.ServiceName);
			}
			dbParms.ServiceCode = sc.ServiceName;
			dbParms.MBVersion = sc.Version;
			dbParms.ServiceUserid = Utilities.UseridToDisplayName(sc.StartName);
			dbParms.ServiceComputer = sc.ServiceComputer;
			return dbParms;
		}
		public static Libraries.Exception TestForValidMainBossService(ServiceConfiguration sc, ServiceParms dbParms) {
			Thinkage.Libraries.Exception baseError = null;
			if (string.IsNullOrWhiteSpace(sc.DatabaseName))
				return new GeneralException(Thinkage.MainBoss.Database.KB.K("The Windows Service for MainBoss has no database configured, cannot confirm that the service belong to this database"));
			if (string.IsNullOrWhiteSpace(sc.DatabaseServer))
				return new GeneralException(Thinkage.MainBoss.Database.KB.K("The Windows Service for MainBoss has no SQL Server configured, cannot confirm that the service belong to this database"));
			if (!DomainAndIP.SameComputer(sc.DatabaseServer, dbParms.ConnectionInfo.DatabaseServer))
				baseError = new GeneralException(Thinkage.MainBoss.Database.KB.K("The Windows Service for MainBoss '{0}' is configured to serve database '{1}' on '{2}' rather than '{3}' on '{4}'."), dbParms.ServiceCode, sc.DatabaseName, sc.ConnectionInfo.DatabaseServer, dbParms.ConnectionInfo.DatabaseName, dbParms.ConnectionInfo.DatabaseServer);
			else if (String.Compare(sc.DatabaseName, dbParms.ConnectionInfo.DatabaseName, true) != 0)
				baseError = new GeneralException(Thinkage.MainBoss.Database.KB.K("The Windows Service for MainBoss '{0}' is configured to serve database '{1}' rather than '{2}'."), dbParms.ServiceCode, sc.DatabaseName, dbParms.ConnectionInfo.DatabaseName);
			if (baseError != null)
				baseError = new GeneralException(baseError, KB.K("This Windows Service for MainBoss belongs to another MainBoss database.  The changes made to this Windows Service for MainBoss will impact the use of the other MainBoss database."));
			return baseError;
		}
		public static Thinkage.Libraries.Exception TestForMainBossServiceVersion(ServiceConfiguration sc, ServiceParms dbParms, string serviceBinary, Version MinVersion) {
			Thinkage.Libraries.Exception baseError = null;
			var parmVersion = Version(dbParms.MBVersion);
			var serviceVersion = Version(sc.Version);
#if !DEBUG
			if (DomainAndIP.IsThisComputer(sc.ServiceComputer) && serviceBinary != null && sc.ServiceExecutable != serviceBinary)
				baseError = new GeneralException(KB.K("The Windows Service for MainBoss '{0}' is using the executable located at '{1}' whereas the program for the service is expected to be located at '{2}'.")
												, sc.ServiceCode, System.IO.Path.GetDirectoryName(sc.ServiceExecutable), System.IO.Path.GetDirectoryName(serviceBinary));
#endif
			if (sc.ServiceExecutable != serviceBinary && dbParms.ServiceCode != null) {
				if (serviceVersion != null && MinVersion > serviceVersion)
					baseError = new GeneralException(KB.K("Installed Windows Service for MainBoss '{0}' is version '{1}' and the MainBoss database expects version '{2}'."), sc.ServiceCode, sc.Version ?? KB.K("Unknown").Translate(), Thinkage.Libraries.VersionInfo.ProductVersion.ToString());
				else if (parmVersion != null && MinVersion > parmVersion)
					baseError = new GeneralException(KB.K("Installed Windows Service for MainBoss '{0}' is version '{1}' and the MainBoss database shows the MainBoss Service as being version '{2}'."), sc.ServiceCode, sc.Version, dbParms.MBVersion ?? KB.K("Unknown").Translate());
			}
			if (baseError != null)
				baseError = new GeneralException(baseError, KB.K("The Windows Service for MainBoss must be reconfigured."));
			return baseError;
		}
		public static Version MinRequiredServiceVersion(DBClient.Connection connection) {
			var dbSession = new MB3Client(connection);
			var v = new Version(0, 0, 0, 0);
			using (var ds = new dsMB(dbSession)) {
				ds.DB.ViewAdditionalVariables(ds, dsMB.Schema.V.MinMBRemoteAppVersion);
				string vs = ds.V.MinMBRemoteAppVersion.Value;
				System.Version.TryParse(vs, out v);
			}
			return v;
		}
		public static Version Version(string vs) {
			Version v;
			System.Version.TryParse(vs, out v);
			return v;
		}

		#endregion
		#region CheckAlternateEmail
		public static bool CheckAlternateEmail(string address, string from) {
			if (address == null)
				return false;
			var i = address.IndexOf(from, StringComparison.OrdinalIgnoreCase);
			if (i < 0)
				return false;
			var funnyCharsBefore = KB.I("\"#!$%&'*+-/=?^_`{}().@");
			var funnyCharsAfter = KB.I(".(@");
			var before = i == 0 ? ' ' : address[i - 1];
			var after = i + from.Length == address.Length ? ' ' : address[i + from.Length];
			if (char.IsLetterOrDigit(before) || funnyCharsBefore.IndexOf(before) >= 0 || char.IsLetterOrDigit(after) || funnyCharsAfter.IndexOf(after) >= 0)
				return false;
			return true;
		}
		public static List<string> AlternateEmailAddresses(string addresses) {
			var result = new List<string>();
			if (addresses == null)
				return result;
			Regex emailRegex = new Regex(KB.I(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"), RegexOptions.IgnoreCase);
			MatchCollection emailMatches = emailRegex.Matches(addresses);
			foreach (Match emailMatch in emailMatches)
				result.Add(emailMatch.Value);
			return result;
		}
		#endregion
		#region Create Request
		public static string CreateRequest(dsMB ds, dsMB.EmailRequestRow emailRequest, Guid requestorID, string commentToRequestor) {
			// a request record is created only if the requestor is valid.
			// create and fill a new request record
			SequenceCountManager sequenceCounter = null;
			SequenceCountManager.CheckpointData sequenceCounterCheckpoint = null;
			string requestNumber;
			try {
				ds.EnsureDataTableExists(dsMB.Schema.T.RequestState, dsMB.Schema.T.RequestStateHistory, dsMB.Schema.T.Request, dsMB.Schema.T.RequestAcknowledgement);
				sequenceCounter = new SequenceCountManager(ds.DB, dsMB.Schema.T.RequestSequenceCounter, dsMB.Schema.V.WRSequence, dsMB.Schema.V.WRSequenceFormat);
				sequenceCounterCheckpoint = sequenceCounter.Checkpoint();
				dsMB.RequestRow requestRow = ds.T.Request.AddNewRequestRow();
				try {
					var test = requestRow.F.SelectPrintFlag;
				}
				catch (System.NullReferenceException) {
					requestRow.F.SelectPrintFlag = false;
				}
				requestRow.F.Number = string.Empty; // default to empty string, we'll catch this below.
				var subject = emailRequest.F.Subject;
				var description = emailRequest.F.MailMessage;
				var subjectMaxSize = (int)(dsMB.Path.T.Request.F.Subject.Column.EffectiveType as StringTypeInfo).SizeType.NativeMaxLimit(typeof(int));
				//
				// email don't have to have subject and email subjects can be longer than MainBoss's Request Subjects
				// if no email subject use the start of the desciption for the request
				// if the subject lines is too long add the origina subject line to the description 
				// and truncate the subject line.
				// no subject and no body then no request
				//
				if (string.IsNullOrWhiteSpace(subject)) {
					if (string.IsNullOrWhiteSpace(description))
						throw new GeneralException(KB.K("No subject or description in email so no request generated"));
					try {
						var subsize = Math.Min(subjectMaxSize, description.Length);
						subject = description.Substring(0, subsize).Replace("\r", "").Replace('\n', ' ');
					}
					catch {
						subject = "-----"; // give up; just put something non-null in so that the request can be created.
					}
				}
				if (subject.Length > subjectMaxSize) {
					description = Strings.IFormat("{0}\n\n{1}", subject, description);
					subject = subject.Substring(0, subjectMaxSize);
				}
				requestRow.F.Subject = subject;
				requestRow.F.RequestorID = requestorID;
				string desc = emailRequest.F.MailMessage;
				// try to parse the body of the email as an xml request first, if it fails then treat the body as the description of the request
				if (!XmlRequest.SetRequest(ds.DB, requestRow, emailRequest.F.MailMessage)) {
					requestRow.F.Description = description;
				}
				if (String.IsNullOrEmpty(requestRow.F.Number)) {
					// Automatically set the request number if not specified by the requestor
					requestRow.F.Number = sequenceCounter.GetFormattedFirstReservedSequence();
					sequenceCounter.ConsumeFirstReservedSequence();
				}
				requestNumber = requestRow.F.Number;
				// create a corresponding statehistory record for this request
				dsMB.RequestStateHistoryRow requestStateHistoryRow = (dsMB.RequestStateHistoryRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.RequestStateHistory);
				// Link the new history row to the main row.
				requestStateHistoryRow.F.RequestID = requestRow.F.Id;
				requestStateHistoryRow.F.Comment = KB.K("Generated by MainBoss Service").Translate();
				requestStateHistoryRow.F.CommentToRequestor = commentToRequestor; // the initial response-message to the requestor.
																				  //else sequenceCounter.SpoilUnused();
				emailRequest.F.ProcessingState = (int)DatabaseEnums.EmailRequestState.Completed;
				emailRequest.F.RequestID = requestRow.F.Id;
				emailRequest.F.Comment = null;
				ds.DB.Update(ds);
				sequenceCounter.Destroy();
				return requestNumber;
			}
			catch (System.Exception) {
				ds.T.Request.RejectChanges();
				ds.T.RequestStateHistory.RejectChanges();
				if (sequenceCounter != null) {
					sequenceCounter.Rollback(sequenceCounterCheckpoint);
					sequenceCounter.Destroy();
				}
				emailRequest.F.RequestID = null;
				ds.DB.Update(ds);
				throw;
			}
		}
		#endregion
		public static string GetServiceImagePath(string machineName, string serviceName) { // used for error messages, may not be possible to retrieve the data.
			try {
				string registryPath = KB.I(@"SYSTEM\CurrentControlSet\Services\") + serviceName;
				RegistryKey keyHKLM = Registry.LocalMachine;

				RegistryKey key;
				if (!string.IsNullOrWhiteSpace(machineName)) {
					key = RegistryKey.OpenRemoteBaseKey
					  (RegistryHive.LocalMachine, machineName).OpenSubKey(registryPath);
				}
				else {
					key = keyHKLM.OpenSubKey(registryPath);
				}

				string value = key.GetValue(KB.I("ImagePath")).ToString();
				key.Dispose();
				return ExpandEnvironmentVariables(machineName, value);
			}
			catch (System.Exception) {
				return null; //mostly likely no perms. 
			}
		}

		private static string ExpandEnvironmentVariables(string machineName, string path) {
			if (string.IsNullOrWhiteSpace(machineName)) {
				return Environment.ExpandEnvironmentVariables(path);
			}
			else {
				string systemRootKey = KB.I(@"Software\Microsoft\Windows NT\CurrentVersion\");

				using (RegistryKey key = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName)) {
					var k2 = key.OpenSubKey(systemRootKey);
					string expandedSystemRoot = k2.GetValue(KB.I("SystemRoot")).ToString();
					path = path.Replace(KB.I("%SystemRoot%"), expandedSystemRoot);
					k2.Dispose();
					return path;
				}
			}
		}
		#region EmailRequestFromEmail
		public static Guid EmailRequestFromEmail(dsMB dsmb, EmailMessage message, int maxsize, DatabaseEnums.EmailRequestState processingState) {
			dsMB.EmailRequestRow erequestrow = dsmb.T.EmailRequest.AddNewEmailRequestRow();
			erequestrow.F.ProcessingState = (short)processingState;
			erequestrow.F.MailHeader = message.HeaderText;
			erequestrow.F.Subject = message.Subject;
			erequestrow.F.MailMessage = message.Body;
			erequestrow.F.PreferredLanguage = message.PreferredLanguage;
			erequestrow.F.RequestorEmailAddress = message.FromAddress;
			erequestrow.F.RequestorEmailDisplayName = message.FromName;
			erequestrow.F.MailMessage = message.Body;
			if (message.Parts != null) {
				var partList = EmailPart.PartList(message);
				var ParttoEmailPart = new Dictionary<int, dsMB.EmailPartRow>();
				foreach (var ep in partList) {
					var p = ep.Part;
					if (p.Length > maxsize)
						continue;
					dsMB.EmailPartRow part = dsmb.T.EmailPart.AddNewEmailPartRow();
					part.F.EmailRequestID = erequestrow.F.Id;
					part.F.ContentType = p.ContentType.MediaType;
					part.F.ContentEncoding = p.ContentType.CharSet;
					part.F.ContentTypeDisposition = ep.ContentDisposition;
					part.F.Header = ep.Headers;
					part.F.Name = p.ContentType.Name;
					part.F.FileName = (p as Dart.Mail.Attachment)?.FileName;
					part.F.Content = ep.Content;
					part.F.ContentLength = ep.Content == null ? 0 : part.F.Content.Length;
					part.F.Order = (short)ep.Index;
					ParttoEmailPart[ep.Index] = part;
				}
				foreach (var ep in partList)
					if (ep.Parent != -1)
						ParttoEmailPart[ep.Index].F.ParentID = ParttoEmailPart[ep.Parent].F.Id;
			}
			return erequestrow.F.Id;
		}
		public static Guid EmailRequestFromEmail(XAFClient DB, string message, string filename) {
			EmailMessage emailmessage = null;
			;
			try {
				emailmessage = new EmailMessage(message);
			}
			catch (System.Exception) {
				if (filename != null)
					throw new GeneralException(KB.K("Cannot parse email message in file {0}"), filename);
				else
					throw new GeneralException(KB.K("Cannot parse email message"));
			}
			using (var dsmb = new dsMB(DB)) {
				dsmb.EnsureDataTableExists(dsMB.Schema.T.EmailRequest, dsMB.Schema.T.EmailPart);
				var id = EmailRequestFromEmail(dsmb, emailmessage, int.MaxValue, DatabaseEnums.EmailRequestState.UnProcessed);
				DB.Update(dsmb);
				return id;
			}
		}
#if DEBUG
		private const string pDBEmailViewerDebugCategory = "EmailViewer";
		private static string DBEmailViewerDebugCategory {
			get {
				if (!OutputCategoryDefined
					&& Libraries.Application.Instance != null
					&& Libraries.Application.Instance.DebugProvider.DefineOutputCategory(KB.I(pDBEmailViewerDebugCategory)))
					OutputCategoryDefined = true;
				return pDBEmailViewerDebugCategory;
			}
		}
		private static bool OutputCategoryDefined = false;
#else
		private const string DBEmailViewerDebugCategory = null;
#endif
		public static void DebugEmailMessage(XAFClient DB, bool encode, Guid emailRequestID) {
			try {
				var message = EmailMessage.EmailRequestToRFC822(DB, encode, emailRequestID);
				System.Diagnostics.Debug.WriteLine(String.Format("Email Message\r\n{0}", message), DBEmailViewerDebugCategory);
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Cannot parse Email Request"));
			}
		}
		#endregion
		static public System.CodeDom.Compiler.TempFileCollection TemporaryFiles;
		public static void OpenEmail(XAFClient DB, Guid emailRequestId) {
			string fileName = Strings.IFormat("{0}{1}.eml", System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
			if (TemporaryFiles == null)
				TemporaryFiles = new System.CodeDom.Compiler.TempFileCollection();
			TemporaryFiles.AddFile(fileName, false);
			string message = null;
			try {
				message = EmailMessage.EmailRequestToRFC822(DB, true, emailRequestId);
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Error in content of email message"));
			}
			try {
				System.IO.File.WriteAllText(fileName, message);
				using (System.Diagnostics.Process.Start(fileName)) {
				}
			}
			catch (System.Exception e) {
				if (fileName != null)
					System.IO.File.Delete(fileName);
				throw new GeneralException(e, KB.K("Cannot view Email Message"));
			}
		}
	}
	#endregion
	#region ErrorToRequestor
	public enum ErrorToRequestor {
		None,
		Unknown,
		NotFound,
		Multiple,
		InUse,
	}
	#endregion
	#region VerifyAccessToDatabase
	public class VerifyServiceAccessToDatabase {
		public string SqlUserid { get; private set; }
		public string SqlUseridFormatted { get; private set; }
		public string ServerComputer => Parms.ServiceComputer;
		public ServiceParms Parms { get; }
		public ServiceConfiguration ServiceConfiguration { get; }
		public bool ViewAllUsers { get; private set; }
		public bool ViewLocalUsers { get; private set; }
		public bool CanManageUserLogins { get; private set; }
		private MB3Client.ConnectionDefinition DBConnection;
		public bool LoginExists = false;
		public bool LoginDisabled = false;
		public bool HasAccessToDatabase = false;
		public bool IsMainBossUser = false;
		public bool LoginGranted = false;
		public bool AccessGranted = false;
		public bool SqlUserExternalDiffers = false;

		public GeneralException Error { get; private set; }
		public VerifyServiceAccessToDatabase(ServiceParms parms, ServiceConfiguration serviceConfiguration, MB3Client.ConnectionDefinition dbConnection, bool grantAccess) {
			Parms = parms;
			ServiceConfiguration = serviceConfiguration;
			DBConnection = dbConnection;
			SqlUserid = ServiceConfiguration?.ConnectionInfo?.SqlUserid ?? Parms.ConnectionInfo.SqlUserid ?? Parms.ServiceUseridAsSqlUserid;
			SqlUseridFormatted = SqlUserid == null ? Parms.SqlUseridFormatted : Strings.IFormat("'{0}'", SqlUserid);
			SqlUserid = SqlUserid ?? Parms.ServiceUseridAsSqlUserid;
			SqlUserExternalDiffers = Parms.ConnectionInfo.SqlUserid == Parms.ServiceUseridAsSqlUserid
										&& !DomainAndIP.SameComputer(ServerComputer, dbConnection.DBServer)
										&& Parms.ServiceUserid != Parms.ServiceUseridAsSqlUserid;
			var IsWindowsAuthentication = ServiceConfiguration?.ConnectionInfo?.IsWindowsAuthentication ?? true;
			Error = AccessUser(true, grantAccess, newLoginUser);
			if (Error == null)
				Error = AccessUser(false, grantAccess, newDatabaseUser);
			if (!IsMainBossUser && !IsWindowsAuthentication && !dbConnection.DBCredentials.Same(serviceConfiguration.Credentials)) {
				try {
					var serviceAccess = serviceConfiguration.ConnectionInfo.DefineConnection();
					var session = new MB3Client(serviceAccess);
					HasAccessToDatabase = true;
					LoginExists = true;
					IsMainBossUser = true;
					session.CloseDatabase();
				}
				catch (System.Exception e) {
					Error = new GeneralException(e, KB.K("The SQL Credentials {0} for the MainBoss Service do not allow connection to the database"), serviceConfiguration.Credentials.ToString());
				}
				Error = null;
			}
		}
		private GeneralException AccessUser(bool forlogins, bool grant, Action<bool, MB3Client, dsSecurityOnServer> perms) {
			dsSecurityOnServer dsmb = null;
			try {
				var Dbclient = new XAFClient(DBConnection);
				var SecurityConnection = new MainBoss.SecurityOnServerSession.Connection(Dbclient, forlogins);
				var SecurityClient = new MB3Client(new DBClient.Connection(SecurityConnection, dsSecurityOnServer.Schema), SecurityConnection.CreateServer().OpenSession(SecurityConnection, dsSecurityOnServer.Schema));
				ViewAllUsers = SecurityClient.Session.CanViewUserLogins();
				ViewLocalUsers = SecurityClient.Session.CanViewUserCredentials();
				CanManageUserLogins = SecurityClient.Session.CanManageUserLogins();
				dsmb = new dsSecurityOnServer(SecurityClient);
				SecurityClient.ViewAdditionalRows(dsmb, dsSecurityOnServer.Schema.T.SecurityOnServer);
				//
				// check for permssions, if you don't have ViewAllUsers (sql 'View Server State', the answer will be valid for your self
				// and if you have ViewLocalUsers the answer will be valid for all users that have any permsissions on the database
				//
				perms(grant, SecurityClient, dsmb);
			}
			catch (System.Exception e) {
				if (e is GeneralException)
					throw;
				return new GeneralException(e, KB.K("Unable to access user security on the database"));
			}
			finally {
				if (dsmb != null)
					dsmb.Dispose();
			}
			return null;
		}
		void newLoginUser(bool grant, MB3Client SecurityClient, dsSecurityOnServer dsmb) {
			dsSecurityOnServer.SecurityOnServerRow user = null;
			foreach (dsSecurityOnServer.SecurityOnServerRow r in dsmb.T.SecurityOnServer.Rows) {
				if (string.Compare(r.F.LoginName, SqlUserid, true) == 0) {
					user = r;
					break;
				}
			}
			if (!grant || user != null) {
				LoginExists = user != null;
				LoginDisabled = !(user?.F.Enabled ?? false);
				return;
			}
			try {
				if (user == null) {
					user = dsmb.T.SecurityOnServer.AddNewSecurityOnServerRow();
					user.F.LoginName = SqlUserid;
					user.F.Password = Parms.ConnectionInfo.Credentials.Password;
					user.F.CredentialAuthenticationMethod = (sbyte)Parms.ConnectionInfo.Credentials.Type;
					SecurityClient.Update(dsmb);
					LoginExists = true;
					LoginGranted = true;
				}
			}
			catch (System.Exception e) {
				if (e is GeneralException)
					throw new GeneralException(e, KB.K("A SQL login for user {0} could not be created"), SqlUseridFormatted);
			}
		}
		void newDatabaseUser(bool grant, MB3Client SecurityClient, dsSecurityOnServer dsmb) {
			dsSecurityOnServer.SecurityOnServerRow user = null;
			foreach (dsSecurityOnServer.SecurityOnServerRow r in dsmb.T.SecurityOnServer.Rows) {
				if (string.Compare(r.F.LoginName, SqlUserid, true) == 0) {
					user = r;
					break;
				}
			}
			IsMainBossUser = HasAccessToDatabase = false;
			HasAccessToDatabase = LoginExists && !LoginDisabled && user != null;
			IsMainBossUser = (user?.F.InMainBossRole ?? false) || (user?.F.IsDBO ?? false) || (user?.F.IsSysAdmin ?? false);
			if (!grant)
				return;
			try {
				if (user == null) {
					user = dsmb.T.SecurityOnServer.AddNewSecurityOnServerRow();
					user.F.LoginName = SqlUserid;
					user.F.DBUserName = SqlUserid;
					user.F.InMainBossRole = true;
					user.F.Password = Parms.ConnectionInfo.Credentials.Password;
					user.F.CredentialAuthenticationMethod = (sbyte)Parms.ConnectionInfo.Credentials.Type;
					SecurityClient.Update(dsmb);
					HasAccessToDatabase = true;
					IsMainBossUser = true;
					AccessGranted = true;
				}
				else if (!IsMainBossUser) {
					user.F.InMainBossRole = true;
					SecurityClient.Update(dsmb);
					IsMainBossUser = true;
					AccessGranted = true;
				}
			}
			catch (System.Exception e) {
				if (HasAccessToDatabase)
					throw new GeneralException(e, KB.K("You do not have the required permission to add the user {0} to the MainBoss role"), SqlUseridFormatted);
				throw new GeneralException(e, KB.K("A SQL login for user '{0}' exists but you do not have permission to add that user to the MainBoss database"), SqlUseridFormatted);
			}
		}
		public GeneralException GrantAccessToDatabase() {
			GeneralException e = null;
			if (!LoginExists)
				e = AccessUser(true, true, newLoginUser);
			if (!HasAccessToDatabase)
				e = AccessUser(false, true, newDatabaseUser);
			if (e != null)
				Error = e;
			return e;
		}
		public string ReportAccessPermissionOnDatabase() {
			if (LoginExists && HasAccessToDatabase)
				return null;
			if (!ViewLocalUsers) {
				return Strings.Format(KB.K("The SQL user '{0}' has insufficient permissions to view the user logins on the SQL Server, and therefore could not verify if the MainBoss Service which runs as user {1} has access the SQL server"),
					SQLAccessUser(DBConnection.DBCredentials), SqlUseridFormatted);
			}
			else if (!ViewAllUsers) {
				return Strings.Format(KB.K("The SQL user '{0}' has insufficient permissions to view the user logins on the SQL Server, and therefore could not verify if the MainBoss Service which runs as user {1} has access the SQL server"),
					SQLAccessUser(DBConnection.DBCredentials), SqlUseridFormatted);
			}
			return null;
		}
		private string SQLAccessUser(AuthenticationCredentials ac) {
			if (ac.Type == AuthenticationMethod.WindowsAuthentication)
				return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
			else
				return ac.Username ?? System.Security.Principal.WindowsIdentity.GetCurrent().Name;
		}
		public GeneralException ReportAccessError() {
			if (LoginExists && IsMainBossUser && HasAccessToDatabase)
				return null;
			GeneralException e = null;
			if (!ViewLocalUsers && !ViewAllUsers) {
				if (Error != null)
					e = Error;
				else if (ViewAllUsers && !LoginDisabled && LoginExists)
					e = new GeneralException(KB.K("The SQL user name {0} login for SQL server '{1}' has been disabled"), SqlUseridFormatted, DBConnection.DBServer);
				else if (ViewAllUsers && !LoginExists)
					e = new GeneralException(KB.K("The SQL user name {0} does not have a login for SQL server '{1}'"), SqlUseridFormatted, DBConnection.DBServer);
				else if (LoginExists && !HasAccessToDatabase)
					e = new GeneralException(KB.K("The SQL user name {0} does not have access to the database '{1}'"), SqlUseridFormatted, DBConnection.DisplayName);
				else if (LoginExists && !IsMainBossUser)
					e = new GeneralException(KB.K("The SQL user name {0} does not have the 'MainBoss' role on database '{1}'"), SqlUseridFormatted, DBConnection.DisplayName);
			}
			return e;
		}
	}
	#endregion
	#region SQLAuthenticationFromName
	public class SQLAuthenticationFromName {
		public SQLAuthenticationFromName([Libraries.Translation.Invariant] string authenicationName) {
			if (string.IsNullOrWhiteSpace(authenicationName))
				AMN = AuthenticationMethods[0];
			else {
				AMN = AuthenticationMethods.Where(e => Strings.Match(authenicationName, e.Pattern)).FirstOrDefault();
				if (AMN == null)
					throw new GeneralException(KB.K("{0}=\"{1}\" is unknown, Authentication must be one of '{2}'"), KB.I("/Authentication"), authenicationName, string.Join("', '", AuthenticationMethods.Select(e => e.Name)));

			}
		}
		AuthenticationMethodName AMN;
		public string Name => AMN.Name;
		public SqlAuthenticationMethod Method => AMN.Method;
		public class AuthenticationMethodName { public SqlAuthenticationMethod Method; public string Pattern; public string Name; }

		public static AuthenticationMethodName[] AuthenticationMethods = {
			new AuthenticationMethodName { Method= SqlAuthenticationMethod.NotSpecified,               Pattern= KB.I("Windowauthentication"),      Name = KB.I("WindowAuthentication") },
			new AuthenticationMethodName { Method= SqlAuthenticationMethod.SqlPassword,                Pattern= KB.I("sqlPassword"),               Name = KB.I("SQLPassword") },
			new AuthenticationMethodName { Method= SqlAuthenticationMethod.ActiveDirectoryPassword,    Pattern= KB.I("ActiveDirectoryPassword"),   Name = KB.I("ActiveDirectoryPassword") },
			new AuthenticationMethodName { Method= SqlAuthenticationMethod.ActiveDirectoryIntegrated,  Pattern= KB.I("ActiveDirectoryIntegrated"), Name = KB.I("ActiveDirectoryIntegrated") }
		};
	}
	#endregion
	#region SQLConnectionInfo

	/// <summary>
	/// Maps between Mcrosofts utils to decode SQL Server Connection strings
	/// and MainBoss host,database,authentication.
	/// 
	/// </summary>
	[Thinkage.Libraries.Translation.Invariant]
	public class SQLConnectionInfo {
		public SQLConnectionInfo(SqlConnectionStringBuilder builder) {
			SqlConnectionObject = builder;
		}
		public SQLConnectionInfo(string connection) {
			try {
				SqlConnectionObject = new SqlConnectionStringBuilder(connection);
				SqlConnectionObject.DataSource = globalName(SqlConnectionObject.DataSource);
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("SQL connection string '{0}' format is not valid "), connection);
			}
		}
		public SQLConnectionInfo(string server, string database, AuthenticationCredentials authenticate = null) {
			try {
				SqlConnectionObject = new SqlConnectionStringBuilder();
				SqlConnectionObject.DataSource = globalName(server);
				SqlConnectionObject.InitialCatalog = database;
				if (authenticate != null) {
					SqlConnectionObject.UserID = authenticate.Username;
					SqlConnectionObject.Password = authenticate.Password;
					SqlConnectionObject.Authentication = TtoM[authenticate.Type];
				}
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Could not construct a valid SQL connection string from server '{0}' database '{1}' and Authentication '{3}'"), server, database, authenticate);
			}
		}
		public SQLConnectionInfo(string server, string database, AuthenticationMethod method, string userid, string password)
			: this(server, database, new AuthenticationCredentials(method, userid, password)) { }

		public SQLConnectionInfo(string server, string database, SqlAuthenticationMethod method, string userid, string password) : this(server, database) {
			SqlConnectionObject.UserID = userid;
			SqlConnectionObject.Password = password;
			SqlConnectionObject.Authentication = method;
		}
		static Dictionary<AuthenticationMethod, SqlAuthenticationMethod> TtoM = new Dictionary<AuthenticationMethod, SqlAuthenticationMethod>() {
			{ AuthenticationMethod.WindowsAuthentication, SqlAuthenticationMethod.NotSpecified },
			{ AuthenticationMethod.SQLPassword, SqlAuthenticationMethod.SqlPassword },
			{ AuthenticationMethod.ActiveDirectoryPassword, SqlAuthenticationMethod.ActiveDirectoryPassword },
			{ AuthenticationMethod.ActiveDirectoryIntegrated, SqlAuthenticationMethod.ActiveDirectoryIntegrated }
		};

		// datasource has format 'computer\instance,port' where instance and port are optional
		public SqlConnectionStringBuilder SqlConnectionObject { get; }
		public string DatabaseServer => SqlConnectionObject.DataSource;
		public string ConnectionString => SqlConnectionObject.ConnectionString;
		public string DatabaseName => SqlConnectionObject.InitialCatalog;
		public string Server {
			get {
				var m = DatabaseServer.Split("\\,".ToCharArray())[0];   // remove intance and port
				var s = m.Split(':');                                   // remove protocol
				return s[s.Length - 1];
			}
		}
		public string Instance {
			get {
				var d = DatabaseServer.Split('\\');
				if (d.Length <= 1)
					return "";
				return d[1].Split(',')[0];
			}
		}
		public int? Port {
			get {
				var d = DatabaseServer.Split('\\');
				if (d.Length <= 1)
					return null;
				var ps = d[1].Split(',');
				if (ps.Length <= 1)
					return null;
				int p = 0;
				if (int.TryParse(ps[1], out p))
					return p;
				return null;
			}
		}
		public AuthenticationCredentials Credentials {
			get {
				var t = TtoM.FirstOrDefault(x => x.Value == SqlConnectionObject.Authentication).Key;
				return new AuthenticationCredentials(t, SqlConnectionObject.UserID, SqlConnectionObject.Password);
			}
		}
		public bool IsWindowsAuthentication => SqlConnectionObject.Authentication == SqlAuthenticationMethod.NotSpecified;
		public MB3Client.ConnectionDefinition DefineConnection() {
			return new MB3Client.ConnectionDefinition(DatabaseServer, DatabaseName, Credentials);
		}
		public string SqlUserid {
			get {
				return !string.IsNullOrWhiteSpace(SqlConnectionObject.UserID) ? SqlConnectionObject.UserID : null;
			}
		}
		public string SqlPassword {
			get {
				return !string.IsNullOrWhiteSpace(SqlConnectionObject.Password) ? SqlConnectionObject.Password : null;
			}
		}
		public override string ToString() {
			return SqlConnectionObject == null ? "Invalid Connection" : SqlConnectionObject.ToString();
		}
		private static string globalName(string ds) {
			var c = ds.IndexOf(',');
			var i = ds.IndexOf('\\');
			var e = c < 0 ? i : c;
			e = e < 0 ? ds.Length : e;
			var p = ds.IndexOf(':') + 1;
			var s = p > e ? 0 : p;
			var m = ds.Substring(s, e - s).Trim();
			if (m == "" || m == "." || string.Compare(m, "localhost", true) == 0)
				ds = ds.Remove(s, e - s).Insert(s, Environment.MachineName);
			return ds;
		}
	}
	#endregion

}
