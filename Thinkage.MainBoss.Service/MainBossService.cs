using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.Service {

	[System.ComponentModel.DesignerCategory("Code")]
	public class MainBossService : ServiceWithServiceWorkers {
		#region Service Workers
		protected override Type[] ServiceWorkersToCreate {
			get {
				return pServiceWorkersToCreate;
			}
		}
		// For now, statically define the serviceWorkers we support
		private static System.Type[] pServiceWorkersToCreate = {
			 typeof(ServiceLogWorker), typeof(EmailRequestWorker), typeof(RequestorNotificationWorker), typeof(AssignmentNotificationWorker)
		};

		public class ServiceOptable : Optable {
			public StringValueOption DataBaseName;
			public StringValueOption DataBaseServer;
			public ServiceOptable() {
				Add(DataBaseServer = MB3Client.OptionSupport.CreateServerNameOption(true));
				Add(DataBaseName = MB3Client.OptionSupport.CreateDatabaseNameOption(true));
			}
		}
		#endregion

		#region CustomCommand
		protected override void OnCustomCommand(int command) {
			bool loggingChanged = false;
			switch ((ApplicationServiceRequests)command) {
			case ApplicationServiceRequests.TERMINATE_ALL:
				WatchDog.Stop();
				OnStop();
				MainBossService.Exit(0);
				return;
			case ApplicationServiceRequests.PAUSE_SERVICE:
				OnPauseUser();
				return;
			case ApplicationServiceRequests.RESUME_SERVICE:
				OnContinueUser();
				return;
			case ApplicationServiceRequests.REREAD_CONFIG:
				if (!ServiceEnvironmentSetup)
					return; // not ready yet
				LogInfoIfAble(KB.K("'Refresh' command received").Translate());
				OnReset();
				return;
			case ApplicationServiceRequests.TRACE_OFF:
				loggingChanged |= setLogging(ref Logging.Activities, true);
				loggingChanged |= setLogging(ref Logging.ReadEmailRequest, false);
				loggingChanged |= setLogging(ref Logging.NotifyRequestor, false);
				loggingChanged |= setLogging(ref Logging.NotifyAssignee, false);
				break;
			case ApplicationServiceRequests.TRACE_ALL:
				loggingChanged |= setLogging(ref Logging.Activities, true);
				loggingChanged |= setLogging(ref Logging.ReadEmailRequest, true);
				loggingChanged |= setLogging(ref Logging.NotifyRequestor, true);
				loggingChanged |= setLogging(ref Logging.NotifyAssignee, true);
				break;
			case ApplicationServiceRequests.TRACE_ACTIVITIES:
				loggingChanged |= setLogging(ref Logging.Activities, true);
				break;
			case ApplicationServiceRequests.TRACE_EMAIL_REQUESTS:
				loggingChanged |= setLogging(ref Logging.Activities, true);
				loggingChanged |= setLogging(ref Logging.ReadEmailRequest, true);
				break;
			case ApplicationServiceRequests.TRACE_NOTIFY_REQUESTOR:
				loggingChanged |= setLogging(ref Logging.Activities, true);
				loggingChanged |= setLogging(ref Logging.NotifyRequestor, true);
				break;
			case ApplicationServiceRequests.TRACE_NOTIFY_ASSIGNEE:
				loggingChanged |= setLogging(ref Logging.Activities, true);
				loggingChanged |= setLogging(ref Logging.NotifyAssignee, true);
				break;
			case ApplicationServiceRequests.PROCESS_ALL:
				LogInfoIfAble(KB.K("'Process All' command received").Translate());
				break;
			case ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS:
				LogInfoIfAble(KB.K("'Process Assignment Notifications' command received").Translate());
				break;
			case ApplicationServiceRequests.PROCESS_REQUESTOR_NOTIFICATIONS:
				LogInfoIfAble(KB.K("'Process Requestor Notifications' command received").Translate());
				break;
			case ApplicationServiceRequests.PROCESS_REQUESTS_INCOMING_EMAIL:
				LogInfoIfAble(KB.K("'Process Email Requests' command received").Translate());
				break;
			}
			if (loggingChanged)
				LogInfoIfAble(Thinkage.Libraries.Strings.Format(KB.K("MainBoss Service Logging set to show: {0}"), Logging.GetTranslatedLoggingParameterMessage()));
			if (!ServiceEnvironmentSetup)
				return; // not ready yet
			base.OnCustomCommand(command);
		}
		private bool setLogging(ref bool logit, bool v) {
			bool r = logit != v;
			if (r)
				logit = v;
			return r;
		}
		#endregion

		#region OnStart
		public MB3Client.ConnectionDefinition DBConnection;
		//
		// Set things in motion so your service can do its work.
		//  Environment.UserInteractive  if true then this program was up a a comandline program.
		//  otherwise has been started as a service.
		//
		private void StartService(string[] args) {
			if (System.Threading.Thread.CurrentThread.Name == null)
				System.Threading.Thread.CurrentThread.Name = KB.I("MainBossService");
			// We will retrieve the startup parameters from the service registry key
			// and the command line. The command line overrides the registry
			// We will store the connection parameters in the registry as well

			ServiceParms cmdParms = new ServiceParms();
			cmdParms.ServiceCode = string.IsNullOrWhiteSpace(ServiceOptions.ServiceName) ? null : ServiceOptions.ServiceName;
			cmdParms.MBVersion = string.IsNullOrWhiteSpace(ServiceOptions.MBVersion) ? null : ServiceOptions.MBVersion;
			cmdParms.ServiceUserid = string.IsNullOrWhiteSpace(ServiceOptions.ServiceUserid) ? null : ServiceOptions.ServiceUserid;
			cmdParms.ServicePassword = string.IsNullOrWhiteSpace(ServiceOptions.ServicePassword) ? null : ServiceOptions.ServicePassword;
			cmdParms.ServiceComputer = string.IsNullOrWhiteSpace(ServiceOptions.ServiceComputer) ? DomainAndIP.MyDnsName : ServiceOptions.ServiceComputer;
			//
			// build a SQL Connection string for information supplied. Command line argument override information 
			// directly supplied.
			//
			SqlConnectionStringBuilder builder;
			try {
				if (string.IsNullOrWhiteSpace(ServiceOptions.Connection))
					builder = new SqlConnectionStringBuilder();
				else
					builder = new SqlConnectionStringBuilder(ServiceOptions.Connection);
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("'{0}' is not a valid formatted SQL Server connection string"), ServiceOptions.Connection);
			}
			if (!string.IsNullOrWhiteSpace(ServiceOptions.Connection) || !(string.IsNullOrWhiteSpace(ServiceOptions.DatabaseName))) {
				try {
					if (!string.IsNullOrWhiteSpace(ServiceOptions.DatabaseName))
						builder.InitialCatalog = ServiceOptions.DatabaseName;
					if (!string.IsNullOrWhiteSpace(ServiceOptions.SQLUserid))
						builder.UserID = ServiceOptions.SQLUserid;
					if (!string.IsNullOrWhiteSpace(ServiceOptions.SQLPassword))
						builder.Password = ServiceOptions.SQLPassword;
					if (!string.IsNullOrWhiteSpace(ServiceOptions.DatabaseServer))
						builder.DataSource = ServiceOptions.DatabaseServer;
					if (ServiceOptions.AuthenticationMethod != null)
						builder.Authentication = ServiceOptions.AuthenticationMethod.Value;
					if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ".")
						builder.DataSource = DomainAndIP.MyDnsName;
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Could not construct a valid SQL Server Connection string"));
				}
				cmdParms.ConnectionInfo = new SQLConnectionInfo(builder);
			}
			else if (!string.IsNullOrWhiteSpace(ServiceOptions.SQLUserid) || ServiceOptions.AuthenticationMethod != null || !string.IsNullOrWhiteSpace(ServiceOptions.SQLPassword))
				throw new GeneralException(KB.K("Database connection information was not supplied"));
			if (System.Threading.Thread.CurrentThread.Name == null)
				System.Threading.Thread.CurrentThread.Name = cmdParms.ServiceCode ?? KB.I("MainBossService");
			if (UserInterface.IsRunningAsAService) {
#if DEBUG
				System.Diagnostics.Debugger.Launch();
#endif
				VerifyServiceEnvironment(cmdParms);
				LogInfoIfAble(Strings.Format(KB.K("MainBoss Service '{0}' starting on computer '{1}' using {2} running as user '{3}'"), cmdParms.ServiceCode, DomainAndIP.MyDnsName, DBConnection.DisplayNameLowercase, Environment.UserName));
			}
			else {
				ProcessCommandLine(cmdParms); // will only return if the service is expected to carry out any actions.
				LogInfoIfAble(Strings.Format(KB.K("MainBoss Service started in manual mode on {0} by user '{1}'"), DBConnection.DisplayNameLowercase, Environment.UserName));
			}
			ServiceTranslator = new Thinkage.MainBoss.Database.StaticUserMessageTranslator(DBConnection);
			Thinkage.Libraries.Application.Instance.Translator = new Thinkage.Libraries.Translation.TranslatorConcentrator(ServiceTranslator, Thinkage.Libraries.Application.Instance.Translator);

			WatchDog = new System.Timers.Timer();
			WatchDog.Elapsed += new System.Timers.ElapsedEventHandler((s, e) => WatchDogTest());
			WatchDog.Interval = WatchDogInterval.TotalMilliseconds;

			WatchDog.Start();
			ServiceEnvironmentSetup = true;
			OnStartUser(args);
			ServiceEnvironment = null;
		}

		private Thinkage.MainBoss.Database.StaticUserMessageTranslator ServiceTranslator = null;
		#endregion
		#region VerifyServiceEnvironment
		private void VerifyServiceEnvironment(ServiceParms cmdParms) {
			ServiceParms dbParms = cmdParms;
			if ((cmdParms.ConnectionInfo == null || cmdParms.ServiceCode == null)) {
				if (cmdParms.ServiceCode != null)
					LogErrorAndExit(1, Strings.Format(KB.K("The Windows Service for MainBoss '{0}' registry entries have been damaged; Delete the Windows Service for MainBoss '{0}' and then reconfigure the service"), cmdParms.ServiceCode));
				else
					LogErrorAndExit(1, Strings.Format(KB.K("The Windows Service for MainBoss registry entries have been damaged; Delete the Windows Service for MainBoss and reconfigure the service")));
			}

			if (cmdParms.ConnectionInfo != null) {
				try {
					DBConnection = cmdParms.ConnectionInfo.DefineConnection();
					ServiceLogging = ServiceUtilities.VerifyDatabaseAndAcquireLog(DBConnection, cmdParms.ServiceCode ?? KB.I("MainBoss Service"));
					dbParms = ServiceUtilities.AcquireServiceRecordInfo(cmdParms.ConnectionInfo, DBConnection, cmdParms.ServiceCode, ServiceLogging ?? this);
					cmdParms.UpdateWith(dbParms);
				}
				catch (GeneralException ex) {
					LogErrorAndExit(1, Thinkage.Libraries.Exception.FullMessage(ex));
				}
			}
			if (ServiceLogging == null) {
				if (Environment.UserName.EndsWith("$") && cmdParms.ConnectionInfo.IsWindowsAuthentication)
					LogErrorAndExit(1, Strings.Format(KB.K("Windows Service for MainBoss '{0}' cannot access '{1}' probably because the user that the service has been configured to run as (probably 'NT AUTHORITY\\Network Service') does not have the required SQL permissions on the database."),
										cmdParms.ServiceCode, DBConnection.DisplayNameLowercase));
				LogErrorAndExit(1, Strings.Format(KB.K("Windows Service for MainBoss '{0}' cannot access '{1}' probably because user '{2}' does not have the required SQL permissions on the database."),
										cmdParms.ServiceCode, DBConnection.DisplayNameLowercase, Environment.UserName));
			}
			if (dbParms.MBVersion == null)
				LogErrorAndExit(1, Strings.Format(KB.K("'{0}' does not have a Windows Service for MainBoss '{1}' configured"), DBConnection.DisplayNameLowercase, cmdParms.ServiceCode));
			if (string.Compare(dbParms.ServiceCode, cmdParms.ServiceCode) != 0)
				LogErrorAndExit(1, Strings.Format(KB.K("'{0}' is configured with Windows Service for MainBoss '{1}' not '{2}'"), DBConnection.DisplayNameLowercase, dbParms.ServiceComputer, DomainAndIP.MyDnsName));
			if (!DomainAndIP.SameComputer(cmdParms.ServiceComputer, dbParms.ServiceComputer))
				LogErrorAndExit(1, Strings.Format(KB.K("'{0}' is configured with its Windows Service for MainBoss on computer '{1}' not computer '{2}'"), DBConnection.DisplayNameLowercase, dbParms.ServiceComputer, DomainAndIP.MyDnsName));
			var idenity = WindowsIdentity.GetCurrent();
			var isAdmin = new WindowsPrincipal(idenity).IsInRole(WindowsBuiltInRole.Administrator);
			var workingUserid = idenity.Name;
			if (workingUserid[workingUserid.Length - 1] == '$')
				workingUserid = isAdmin ? Utilities.LOCALADMIN_DISPLAYNAME : Utilities.NETWORKSERVICE_DISPLAYNAME;
			if (string.Compare(dbParms.ServiceUserid, workingUserid, true) != 0) { // needed in case User change Service Userid using the Windows Server Manager
				dbParms.ServiceUserid = workingUserid;
				ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, ServiceLogging ?? this);
			}
		}
		#endregion
		#region ProcessCommandLine
		private void ProcessCommandLine(ServiceParms cmdParms) {
			ServiceParms dbParms = cmdParms;
			ServiceConfiguration serviceConfiguration = null;
			bool isAdmin = (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
			bool requireAdmin = ServiceOptions.CreateService | ServiceOptions.DeleteService | ServiceOptions.UpdateService;
			if (!isAdmin && requireAdmin)
				throw new GeneralException(KB.K("Options '{0}', '{1}' and '{2}' require this program to be run as an Administrator"), KB.I("/DELeteService"), KB.I("/CreateService"), KB.I("/UpdateService"));
			if ((ServiceOptions.ListMainBossServices)) {
				var allServices = ServiceConfiguration.AllMainBossServices(ServiceOptions.ServiceComputer);
				switch (allServices.Count) {
				case 0:
					System.Console.WriteLine(Strings.Format(KB.K("No Windows Service for MainBoss found on computer '{0}'"), cmdParms.ServiceComputer));
					Exit(0);
					break;
				case 1:
					cmdParms.ServiceCode = allServices.First();
					System.Console.WriteLine(Strings.Format(KB.K("Found one Windows Service for MainBoss named '{0}' on computer '{1}'"), cmdParms.ServiceCode, cmdParms.ServiceComputer));
					break;
				default:
					ListMainBossServices(ServiceOptions.ServiceComputer);
					if (!ServiceOptions.ListMainBossServices)
						throw new GeneralException(KB.K("Multiple Windows Services for MainBoss exist; Choose one with the '{0}' option"), KB.I("/ServiceName:"));
					LogCloseAndExit();
					break;
				}
			}
			// 
			// if we don't have information to connect to the database
			// see if we can extract the information from the service command line (which must be on this computer)
			//
			if (string.IsNullOrWhiteSpace(cmdParms.ConnectionInfo?.DatabaseName)) {
				serviceConfiguration = ServiceConfiguration.AcquireServiceConfiguration(isAdmin, ServiceOptions.CreateService | ServiceOptions.DeleteService | ServiceOptions.UpdateService, cmdParms.ServiceComputer, cmdParms.ServiceCode);
				cmdParms.ConnectionInfo = serviceConfiguration?.ConnectionInfo;
				cmdParms.ServiceCode = serviceConfiguration?.ServiceName;
				cmdParms.ServiceComputer = DomainAndIP.MyDnsName;
				if (cmdParms.ConnectionInfo?.DatabaseName == null || cmdParms.ConnectionInfo?.DatabaseServer == null)
					throw new GeneralException(KB.K("Need options {0} or {1} and {2} option to continue;"), KB.I("/Connection:"), KB.I("/DatabaseServer:"), KB.I("/DatabaseName:"));
			}
			//
			// access the database
			//
			try {
				DBConnection = cmdParms.ConnectionInfo.DefineConnection();
				ServiceLogging = ServiceUtilities.VerifyDatabaseAndAcquireLog(DBConnection, cmdParms.ServiceCode ?? KB.I("MainBoss Service"));
				if (ServiceLogging == null)
					Exit(-1); // there will have been an error
			}
			catch (System.Exception ex) {
				LogErrorAndExit(1, Thinkage.Libraries.Exception.FullMessage(ex));
			}
			//
			// get the service record
			//
			try {
				dbParms = ServiceUtilities.AcquireServiceRecordInfo(cmdParms.ConnectionInfo, DBConnection, cmdParms.ServiceCode, null);
			}
			catch (System.Exception ex) {
				if (!ServiceOptions.DeleteService)  // no MainBoss Service record but a MainBoss Service may actually exist.
					LogErrorAndExit(1, Thinkage.Libraries.Exception.FullMessage(ex));
			}
			//
			// check if service record is consistent with command line.
			//
			if (string.IsNullOrWhiteSpace(cmdParms.ServiceCode))
				cmdParms.ServiceCode = dbParms.ServiceCode;
			else if (string.Compare(cmdParms.ServiceCode, dbParms.ServiceCode, true) != 0)
				throw new GeneralException(KB.K("The command has the service code as '{0}', but the service record has the code '{1}'"), cmdParms.ServiceCode, dbParms.ServiceCode);
			if (string.IsNullOrWhiteSpace(dbParms.ServiceComputer))
				dbParms.ServiceComputer = cmdParms.ServiceComputer;

			//
			// check is a service does exist, and that it is a valid MainBoss Service
			// 
			try {
				if (serviceConfiguration == null)
					serviceConfiguration = ServiceConfiguration.AcquireServiceConfiguration(isAdmin, ServiceOptions.CreateService | ServiceOptions.DeleteService | ServiceOptions.UpdateService, dbParms.ServiceComputer, dbParms.ServiceCode);
				if (serviceConfiguration != null) {  // we can at least enumerate the services.
					if (serviceConfiguration.ServiceDetailsAvailable) {
						if (!serviceConfiguration.IsMainBossDatabase)
							throw serviceConfiguration.LastError;
						if (!ServiceOptions.DeleteService || !ServiceOptions.Force) {
							var error = ServiceUtilities.TestForValidMainBossService(serviceConfiguration, dbParms);
							if (error != null)
								throw error;
						}
					}
					else if (ServiceOptions.UpdateService)
						throw serviceConfiguration.LastError;
				}
				else if (!ServiceOptions.CreateService && !ServiceOptions.DeleteService && !ServiceOptions.UpdateService)
					Console.WriteLine(Strings.Format(KB.K("Currently there is no Windows Service for MainBoss installed for {0}"), DBConnection.DisplayName));
			}
			catch (System.Exception e) {
				if (ServiceOptions.DeleteService && ServiceOptions.Force)
					System.Console.WriteLine(Thinkage.Libraries.Exception.FullMessage(e));
				else if (ServiceOptions.CreateService || ServiceOptions.UpdateService)
					throw;
				if (!ServiceOptions.CreateService && !ServiceOptions.DeleteService && !ServiceOptions.UpdateService) {
					Console.WriteLine(Strings.Format(KB.K("Unable to determine if a Windows Service for MainBoss is installed for {0}"), DBConnection.DisplayName));
					if (!isAdmin) {
						Console.Write("        ");
						Console.WriteLine(Strings.Format(KB.K("Running this program as an Administrator may enable the configuration checks")));
					}
				}
				Console.WriteLine(Strings.Format(KB.K("Unable to determine if a Windows Service for MainBoss is installed for {0}"), DBConnection.DisplayName));
			}
			if (cmdParms.ConnectionInfo == null) {
				if (serviceConfiguration == null)
					throw new GeneralException(KB.K("The Windows Service for MainBoss '{0}' does not exist on computer '{1}'."), cmdParms.ServiceCode, cmdParms.ServiceComputer);
				else if (!ServiceOptions.DeleteService && !ServiceOptions.UpdateService)
					throw new GeneralException(KB.K("A Windows Service for MainBoss '{0}' exists on computer '{1}', but needs to be updated"), cmdParms.ServiceCode, cmdParms.ServiceComputer);
				else {   // a service misconfiguration and we can't find the database delete without commend.
					if (serviceConfiguration != null)
						serviceConfiguration.Dispose();
					serviceConfiguration = null;
					DeleteService(serviceConfiguration, dbParms, cmdParms);
					Exit(0);
				}
			}
			try {
				if (DBConnection == null)
					DBConnection = cmdParms.ConnectionInfo.DefineConnection();
				if (ServiceLogging == null)
					ServiceLogging = ServiceUtilities.VerifyDatabaseAndAcquireLog(DBConnection, cmdParms.ServiceCode);
				if (ServiceLogging == null)
					Exit(-1);
			}
			catch (GeneralException) {
				if (!(ServiceOptions.DeleteService && ServiceOptions.Force))
					throw;
			}
			catch (System.Exception e) {
				if (!(ServiceOptions.DeleteService && ServiceOptions.Force))
					throw new GeneralException(e, KB.K("Cannot access MainBoss database {0}"), DBConnection.DisplayNameLowercase);
			}
			//
			// if the service exists and we are supposed to create check at least we have permissions.
			//
			if (serviceConfiguration != null && ServiceOptions.UpdateService && !serviceConfiguration.Updatable())
				throw new GeneralException(KB.K("Windows Service for MainBoss '{0}' exists but there are insufficient permissions to access the Windows Service details"), dbParms.ServiceCode);
			//
			// check that the service command line agrees with service record
			//
			if (serviceConfiguration != null && !ServiceOptions.DeleteService) {
				if (!serviceConfiguration.ServiceDetailsAvailable) {
					System.Console.WriteLine(Strings.Format(KB.K("Windows Service for MainBoss '{0}' exists but there are insufficient permissions to check the service configuration"), dbParms.ServiceCode));
					if (!isAdmin)
						System.Console.WriteLine(Strings.Format(KB.K("Running this program as an Administrator may enable the configuration checks"), cmdParms.ServiceCode));
				}
				else {
					try {
						var minVersion = ServiceUtilities.MinRequiredServiceVersion(DBConnection);
						dbParms = ServiceUtilities.CheckServiceCmdLine(serviceConfiguration, dbParms, Assembly.GetExecutingAssembly().Location, ServiceOptions.UpdateService, ServiceOptions.Force, minVersion);
						ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, ServiceLogging);
					}
					catch (Libraries.Exception e) {
						ServiceLogging.LogError(Thinkage.Libraries.Exception.FullMessage(e));
					}
				}
			}
			//
			// Delete Service
			//
			if (ServiceOptions.DeleteService) {
				//
				// clean up non existent service -- check occurs after create service, that way a partial installed service, can still be created.
				// (if we don't have permissions to view services, an exception will have occurred when we tried to get the serviceConfiguration
				//
				if (serviceConfiguration == null) {
					System.Console.WriteLine(Strings.Format(KB.K("Windows Service for MainBoss '{0}' is not installed on '{1}'."), dbParms.ServiceCode, dbParms.ServiceComputer));
					if (dbParms.ServiceCode != null || dbParms.ServiceUserid != null || dbParms.ServiceComputer != null || dbParms.ServicePassword != null) { // make sure the database is in sync
						dbParms.Clear();
						ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, ServiceLogging);  // correct the service record
					}
					Exit(0);
				}
				DeleteService(serviceConfiguration, dbParms, cmdParms);
				serviceConfiguration.Dispose();
				serviceConfiguration = null;
				if (ServiceOptions.DeleteService) {
					System.Console.WriteLine(Strings.Format(KB.K("Windows Service for MainBoss '{0}' was deleted from '{1}'."), cmdParms.ServiceCode, cmdParms.ServiceComputer));
					Exit(0);
				}
			}
			var sqlServerUserid = new VerifyServiceAccessToDatabase(cmdParms, serviceConfiguration, DBConnection, ServiceOptions.GrantAccess);
			var sqlUseridError = sqlServerUserid.Error;
			if (sqlUseridError == null) {
				var warn = sqlServerUserid.ReportAccessPermissionOnDatabase();
				if (warn != null)
					System.Console.WriteLine(warn);
				else
					sqlUseridError = sqlServerUserid.ReportAccessError();
			}
			if (sqlUseridError != null)
				if (ServiceOptions.Force)
					System.Console.WriteLine(Libraries.Exception.FullMessage(sqlUseridError));
				else
					throw sqlUseridError;

			if (sqlServerUserid.LoginGranted || sqlServerUserid.AccessGranted) {
				if (sqlServerUserid.LoginGranted && !sqlServerUserid.AccessGranted)
					LogInfoIfAble(Strings.Format(KB.K("SQL login added user {0}. Used by the Windows Service for MainBoss '{1}' on '{2}'"), sqlServerUserid.SqlUseridFormatted, cmdParms.ServiceCode, cmdParms.ServiceComputer));
				else if (!sqlServerUserid.LoginGranted && sqlServerUserid.AccessGranted)
					LogInfoIfAble(Strings.Format(KB.K("SQL access added to {0} for user {0}."), DBConnection.DisplayName, sqlServerUserid.SqlUseridFormatted));
				else if (sqlServerUserid.LoginGranted && sqlServerUserid.AccessGranted)
					LogInfoIfAble(Strings.Format(KB.K("SQL login to server {0} and access to {0} added for user {0}."), DBConnection.DBServer, DBConnection.DBName, sqlServerUserid.SqlUseridFormatted));
			}
			//
			// Create Service if requested ( or recreate if doing an Uppdate 
			//

			if (ServiceOptions.CreateService) {
				if (serviceConfiguration != null) {
					System.Console.WriteLine(Strings.Format(KB.K("A Windows Service for MainBoss '{0}' already exists on this computer"), serviceConfiguration.ServiceName));
					Exit(0);
				}
				CreateService(dbParms, cmdParms);
			}
			else if (ServiceOptions.UpdateService) {
				if (ServiceOptions.UpdateService && !DomainAndIP.IsThisComputer(serviceConfiguration.ServiceComputer))
					throw new GeneralException(KB.K("An {0} of the Windows Service for MainBoss must be done on computer '{1}'."), KB.I("/UpdateService"), serviceConfiguration.ServiceComputer);
				dbParms.MBVersion = Thinkage.Libraries.VersionInfo.ProductVersion.ToString();
				;
				serviceConfiguration.BinaryPathName = ServiceConfiguration.ServiceCmdLine(System.Reflection.Assembly.GetExecutingAssembly().Location, dbParms.ConnectionInfo, dbParms.MBVersion, dbParms.ServiceCode);
				serviceConfiguration.Update();
				System.Console.WriteLine(Strings.Format(KB.K("Windows Service for MainBoss '{0}' has been updated on computer '{1}'"), dbParms.ServiceCode, DomainAndIP.MyDnsName));
				ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, ServiceLogging);
			}
			if (!dbParms.WindowsAccountExists && (ServiceOptions.TestConfiguration || serviceConfiguration != null))      // ToDo: don't know how to check if the userid has "Login as a service" set.
				throw new GeneralException(KB.K("The Windows Service for MainBoss is being configured to run as user '{0}' which is not a valid user"), cmdParms.ServiceUserid);
			var accessnotallowed = sqlServerUserid.ReportAccessPermissionOnDatabase();
			if (accessnotallowed != null)
				System.Console.WriteLine(accessnotallowed);
			else if (!sqlServerUserid.IsMainBossUser && (ServiceOptions.TestConfiguration || serviceConfiguration != null))
				System.Console.WriteLine(Strings.Format(KB.K("The SQL user name for the service {0} does not have the expected permissions to access the database {1}"), sqlServerUserid.SqlUseridFormatted, DBConnection.DisplayName));
			if (serviceConfiguration != null)
				CheckServiceUserPermissions(cmdParms, sqlServerUserid);
			if (!ServiceOptions.ManuallyRun && !ServiceOptions.TestConfiguration) {
				if (!ServiceOptions.CreateService && !ServiceOptions.UpdateService)
					System.Console.WriteLine(Strings.Format(KB.K("MainBoss Service: Basic diagnostics completed on {0} accessing the SQL Server with SQL User {1}"), DBConnection.DisplayName, sqlServerUserid.SqlUseridFormatted));
				Exit(0);
			}
			if (serviceConfiguration != null)
				serviceConfiguration.Dispose();
			serviceConfiguration = null;
		}
		#endregion
		#region CheckServiceUserPermissions
		private void CheckServiceUserPermissions(ServiceParms cmdParms, VerifyServiceAccessToDatabase sqlServerUserid) {
			if (sqlServerUserid.Parms.ServiceUserid != null && string.Compare(sqlServerUserid.Parms.ServiceUserid, Environment.UserName, true) != 0)
				System.Console.WriteLine(Strings.Format(KB.K("This program is running as user '{0}'. The Windows Service for MainBoss is configured to be running as {1}"), Environment.UserName, sqlServerUserid.Parms.ServiceUserid));
			if (string.Compare(sqlServerUserid.SqlUserid, Utilities.LOCALADMIN_REGISTRYNAME, true) == 0)
				System.Console.WriteLine(Strings.Format(KB.K("Windows Service for MainBoss should not run as user {0}. Use 'NT AUTHORITY\\Network Service' instead"), sqlServerUserid.Parms.ServiceUserid));
			if (sqlServerUserid.HasAccessToDatabase)
				return;
			ServiceLogging.LogError(Strings.Format(KB.K("The Windows Service for MainBoss is configured access the SQL Server using user {0}.  "
														+ "For the service to run, that SQL User must have access to {1}  "
														+ "The '{2}' option can be used to give the user the expected permissions"
														), sqlServerUserid.SqlUseridFormatted, DBConnection.DisplayName, KB.I("/GrantAccess")));
			if (sqlServerUserid.HasAccessToDatabase && cmdParms.ServiceComputer != cmdParms.ConnectionInfo.DatabaseServer && ServiceLogging != null)
				System.Console.WriteLine(Strings.Format(KB.K("Special considerations are required when running the Windows Service for MainBoss under user '{0}' "
														+ "and the database server is on a different computer.  "
														+ "The user '{0}' will access resources on the computer '{1}' as '{2}' "
														+ "which by default has no privileges and belongs to group 'Domain Computers'."
														), sqlServerUserid.SqlUserid, cmdParms.ConnectionInfo.DatabaseServer, sqlServerUserid.Error == null ? sqlServerUserid.SqlUseridFormatted : "domain\\machine$"));
		}
		#endregion

		#region WatchDog
		System.Timers.Timer WatchDog;
		IEnumerable<ServiceWorker> StillActive = null;
		bool doingWatchDogtest = false;
		DateTime? WorkerShutdownTime = null;
#if DEBUG
		TimeSpan WatchDogInterval = new TimeSpan(0, 1, 0);
#else
		TimeSpan WatchDogInterval = new TimeSpan(0, 10, 0);
#endif
		private void WatchDogTest() {
			if (ServiceLogging == null)
				return; // connection to database has yet to be established.
			if (doingWatchDogtest)
				return;
			if (WorkersInactive)
				return;
			if (ServiceLogging.LogFailureTime != null && ServiceLogging.LogFailureTime.Value + WatchDogInterval > DateTime.Now)
				return;
			//
			// the order of these test is important
			// the ServiceLogging.LogFailure will not be reset until the next time
			// a message pulled off log's queue and attempted to be added to the 
			// database.
			// the MainBossServiceConfiguration.CheckForChanges() will force a read of the database
			// which if it fails there is no point of rebuilding the workers.
			// the log message cause by MainBossServiceConfiguration.CheckForChanges() will end
			// up setting/resetting the ServiceLogging.LogFailure
			// 
			doingWatchDogtest = true;
			try {
				if (ServiceLogging.LogFailure != null)
					ServiceLogging.LogActivity(Strings.Format(KB.K("Attempting to restart Windows Service for MainBoss after a logging failure")));
				if (MainBossServiceConfiguration.CheckForChanges()) {
					ServiceLogging.LogInfo(Strings.Format(KB.K("MainBoss Service Configuration has changed, refreshing")));
					MainBossServiceConfiguration.GetConfiguration(DBConnection);
					ServiceTranslator.RefreshTranslations(DBConnection);
				}
				else
					ServiceLogging.LogTrace(Logging.Tracing, Strings.Format(KB.K("Checked for changes in MainBoss Service Configuration")));
			}
			catch (System.Exception e) {
				if (e is GeneralException)
					ServiceLogging.LogError(Thinkage.Libraries.Exception.FullMessage(e));
				else
					ServiceLogging.LogError((Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("Could not check Service Configuration for changes")))));
				StillActive = StopWorkers(); // stop all the workers.
				if (!StillActive.Any()) {
					StillActive = null;
					WorkerShutdownTime = null;
				}
				else
					WorkerShutdownTime = WorkerShutdownTime ?? DateTime.Now;
				doingWatchDogtest = false;
				return;
			} // we can't get the configuration, most likely the sql server is not available.
			if (StillActive != null) {
				StillActive = StillActive.Where(e => e.IsActive).ToList();
				if (WorkerShutdownTime + new TimeSpan(0, 10, 0) < DateTime.Now) {
					foreach (var w in StillActive)
						ServiceLogging.LogWarning(Strings.Format(KB.K("Service Worker '{0}' appears to be not responding"), w.Name));
					StillActive = null;
					WorkerShutdownTime = null;
				}
			}
			if (StillActive == null)
				RestartWorkers();
			doingWatchDogtest = false;
		}

		#endregion
		#region Delete Service
		private void DeleteService(ServiceConfiguration service, ServiceParms dbParms, ServiceParms cmdParms) {
			bool deletefailed = false;
			try {
				dbParms.MBVersion = null;
				if (DBConnection != null && dbParms.ConnectionInfo.DatabaseName != null) {
					dbParms.MBVersion = null;
					ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, ServiceLogging);
				}
				if (service != null) {
					ServiceInstaller.Uninstall(cmdParms.ServiceCode);
				}
				if (service != null) {
					System.Threading.Thread.Sleep(2000);
					if (!ServiceController.ServiceExists(service.StaticConfig)) // it did exist and it now doesn't
						(ServiceLogging ?? this).LogInfo(Strings.Format(KB.K("Windows Service for MainBoss '{0}' deleted."), ServiceName));
					else
						(ServiceLogging ?? this).LogInfo(Strings.Format(KB.K("Windows Service for MainBoss '{0}' delete request sent to Windows. There may be a arbitrary delay before Windows acts on the request."), ServiceName));
				}
			}
			catch (GeneralException ex) {
				(ServiceLogging ?? this).LogError(Thinkage.Libraries.Exception.FullMessage(ex));
				deletefailed = true;
			}
			if (!deletefailed || ServiceOptions.Force) {
				try {
					dbParms.Clear();
					if (DBConnection != null && dbParms.ConnectionInfo.DatabaseName != null)
						ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, ServiceLogging);
				}
				catch (GeneralException ex) {
					LogErrorAndExit(1, Thinkage.Libraries.Exception.FullMessage(ex));
				}
			}
			LogCloseAndExit();
		}
		#endregion
		#region Create Service
		private void CreateService(ServiceParms dbParms, ServiceParms cmdParms) {
			ServiceConfiguration serviceConfiguration = null;
			try {
				dbParms.UpdateWith(cmdParms);
				if (ServiceOptions.Force)
					dbParms.ServiceCode = cmdParms.ServiceCode;
				else if (cmdParms.ServiceCode != dbParms.ServiceCode)
					throw new GeneralException(KB.K("The Service Name '{0}' on the command line does not match the Service Name '{1}' in the database"), cmdParms.ServiceCode, dbParms.ServiceCode);
				if (!dbParms.WindowsAccountExists)      // ToDo: don't know how to check if the userid has "Login as a service" set.
					throw new GeneralException(KB.K("The Windows Service for MainBoss is being configured to run as user '{0}' which is not a valid user"), cmdParms.ServiceUserid);
				dbParms = ServiceUtilities.CreateMainBossService(DBConnection, dbParms, System.Reflection.Assembly.GetExecutingAssembly().Location, ServiceLogging);
				serviceConfiguration = ServiceConfiguration.AcquireServiceConfiguration(true, true, dbParms.ServiceComputer, dbParms.ServiceCode);
				if (serviceConfiguration == null)
					throw new GeneralException(KB.K("Cannot install Windows Service for MainBoss"));
				if (serviceConfiguration.LastError != null)
					throw serviceConfiguration.LastError;
				System.Diagnostics.EventLog.WriteEntry("MainBoss", Strings.Format(KB.K("Windows Service for MainBoss installed")), System.Diagnostics.EventLogEntryType.Information);
			}
			catch (GeneralException) {
				throw;
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Cannot install Windows Service for MainBoss"));
			}
			LogCloseAndExit();
		}
		#endregion
		#region Service Commands
		Task ServiceEnvironment = null;
		ServiceOptions ServiceOptions;
		MainBossServiceApplication MainBossServiceApplication;
		protected override void OnStart(string[] args) {
			ServiceEnvironment = Task.Run(() => StartService(args));
			MainBossServiceApplication = (MainBossServiceApplication)ServiceApplication;
			ServiceOptions = MainBossServiceApplication.ServiceOptions;
			base.OnStart(args);
		}

		protected override void OnStop() {
			MainBossServiceConfiguration.Reset();
			OnStopUser();
			if (ServiceLogging != null) {
				if (UserInterface.IsRunningAsAService)
					ServiceLogging.LogClose(Strings.Format(KB.K("MainBoss Service Shutdown")));
				else
					ServiceLogging.LogClose(Strings.Format(KB.K("MainBoss Service End of Debugging Session")));
			}
			base.OnStop();
		}
		protected override void OnShutdown() {
			WatchDog.Stop();
			OnStopUser();
			if (ServiceLogging != null) {
				if (UserInterface.IsRunningAsAService)
					ServiceLogging.LogClose(Strings.Format(KB.K("MainBoss Service Shutdown")));
				else
					ServiceLogging.LogClose(Strings.Format(KB.K("MainBoss Service End of Debugging Session")));
			}
			base.OnShutdown();
		}

		protected override void OnPauseUser() {
			base.OnPauseUser();
			ServiceLogging.LogClose(Strings.Format(KB.K("MainBoss Service Paused")));
			if (ServiceLogging == null)
				Exit(-1);
			MainBossServiceConfiguration.Reset();
		}
		protected override void OnContinueUser() {
			ServiceLogging = ServiceUtilities.VerifyDatabaseAndAcquireLog(DBConnection, ServiceName);
			LogInfoIfAble(Strings.Format(KB.K("MainBoss Service Resumed ")));
			if (ServiceLogging == null)
				Exit(-1);
			base.OnContinueUser();
		}

		protected void OnReset() {
			base.OnPauseUser();
			(ServiceLogging ?? this).LogClose(Strings.Format(KB.K("MainBoss Service refresh starting")));
			ServiceLogging = ServiceUtilities.VerifyDatabaseAndAcquireLog(DBConnection, ServiceName);
			LogInfoIfAble(Strings.Format(KB.K("MainBoss Service refresh complete with current configuration")));
			ServiceTranslator.RefreshTranslations(DBConnection);
			base.OnContinueUser();
		}
		#endregion
		#region Exit and friends
		public void LogCloseAndExit(string message = null) {
			(ServiceLogging ?? this).LogClose(message);
			Exit(0);
		}
		public void LogErrorAndExit(int status, string message) {
			(ServiceLogging ?? this).LogError(message);
			(ServiceLogging ?? this).LogClose(null);
			Exit(status);
		}
		/// <summary>
		/// Info messages are only kept to the ServiceLogging logger, never to our System Log
		/// </summary>
		/// <param name="message"></param>
		public void LogInfoIfAble(string message) {
			if (ServiceLogging != null)
				ServiceLogging.LogInfo(message);
		}
		//
		// the cmd window was created just for this program
		//
		public static bool PopUpConsole = false;
		static public void Exit(int status) {
			if (!UserInterface.IsRunningAsAService && PopUpConsole) {
				try {
					while (System.Console.KeyAvailable)
						Console.ReadKey(true);
					Console.WriteLine(Strings.Format(KB.K("Press any key to end program")));
					Console.ReadKey(true);
				}
				catch (System.Exception) {; }
			}
			Environment.Exit(status);
		}
		public void DebugCommand(ApplicationServiceRequests command) {
			var tmp = ServiceEnvironment;
			if (tmp != null)
				tmp.Wait();
			OnCustomCommand((int)command);
		}

		#endregion
		#region Constructor

		public MainBossService(Thinkage.Libraries.Service.Application appObject)
			: base(appObject) {
			AutoLog = false; // We do not want automatic logging from ServiceProcess.ServiceBase (nor can the ServiceBase write to a service with a custom log)
		}
		#endregion
		#region ListMainBossServies
		void ListMainBossServices(string machineName) {
			var output = new StringBuilder();
			var lineFormat = "{0,-20} {1,-30} {2,-10} {3,-30} {4}";
			var AllServices = ServiceConfiguration.AllMainBossServices(machineName);
			if (!AllServices.Any())
				LogClose(Strings.Format(KB.K("There are no MainBoss Services installed on this computer")));
			foreach (var s in AllServices) {
				try {
					var config = new StaticServiceConfiguration(machineName, s);
					using (var serviceInfo = new ServiceConfiguration(config)) {
						if (serviceInfo.LastError != null)
							throw serviceInfo.LastError;
						string canAccess = KB.K("Unknown").Translate();
						try {
							var authentication = new Libraries.DBILibrary.AuthenticationCredentials();
							DBConnection = new MB3Client.ConnectionDefinition(serviceInfo.DatabaseServer, serviceInfo.DatabaseName, Libraries.DBILibrary.AuthenticationCredentials.Default);
							using (var logging = ServiceUtilities.VerifyDatabaseAndAcquireLog(DBConnection, s)) {
								if (logging != null)
									canAccess = KB.K("Can Access").Translate();
							}
						}
						catch (System.Exception) {; }
						output.AppendLine(Strings.IFormat(lineFormat, s, serviceInfo.ServiceUserid, canAccess, serviceInfo.DatabaseName, serviceInfo.DatabaseServer));
					}
				}
				catch (System.Exception e) {
					output.AppendLine(Strings.Format(KB.K("'{0:20}' Error:{1}"), s, Thinkage.Libraries.Exception.FullMessage(e)));
				}
			}
			System.Console.WriteLine(Strings.Format(KB.K("List of Windows Services for MainBoss on computer '{0}' follows:"), machineName));
			Console.WriteLine(Strings.IFormat(lineFormat, KB.K("Service Name"), KB.K("Service User Name"), KB.K("Access"), KB.K("Database Name"), KB.K("Database Server")));
			Console.WriteLine(output);
		}
		#endregion
		#region Main Entry
		/// <summary>
		/// The main entry point for the service
		/// </summary>
		static void Main(string[] args) {
			//
			// If you are running as system or as a service the console does not exist
			//
			try {
				PopUpConsole = System.Console.CursorLeft == 0 && System.Console.CursorTop == 0;
			}
			catch (System.Exception) {; }
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((o, a) => {
				System.Exception e = (System.Exception)a.ExceptionObject;
				GeneralException eg = e as GeneralException;
				if (eg != null)
					System.Console.WriteLine(Thinkage.Libraries.Exception.FullMessage(eg));
				else
					System.Console.WriteLine(Strings.Format(KB.K("Exception: {0}"), Thinkage.Libraries.Exception.FullMessage(e)));
				MainBossService.Exit(1);
			});
			new MainBossServiceApplication(args);
			Thinkage.Libraries.Application.Run();
		}
		#endregion
	}
}
