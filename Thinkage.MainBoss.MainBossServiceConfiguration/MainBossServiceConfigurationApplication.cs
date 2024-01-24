using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI.MSWindows;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using System.Data.SqlClient;

namespace Thinkage.MainBoss.MainBossServiceConfiguration {
	#region CommanLine Setup and Processing
	public interface IServiceVerbDefinition	{
		Thinkage.Libraries.CommandLineParsing.Optable Optable	{
			get;
		}
		string Verb	{
			[return: Thinkage.Libraries.Translation.Invariant]
			get;
		}
		void RunVerb();
	}
	public abstract class ServiceVerbDefinition : Thinkage.Libraries.CommandLineParsing.Optable, IServiceVerbDefinition	{
		public ServiceVerbDefinition([Invariant]string verb)	: base() {
			Verb = verb;
		}
		public Optable Optable	{
			get	{
				return this;
			}
		}
		public string Verb	{
			[return: Thinkage.Libraries.Translation.Invariant]
			get;  set;
		}
		public abstract void RunVerb();
	}
	public abstract class ServiceVerbWithServiceNameDefinition : ServiceVerbDefinition, IServiceVerbDefinition {
		public ServiceVerbWithServiceNameDefinition([Invariant]string verb) : base(verb)	{
			Add(ServiceCodeOption		= new StringValueOption(KB.I("ServiceCode"), KB.K("The service name to perform operation on").Translate(), true));
			Add(SqlConnectionOption = new StringValueOption(KB.I("Connection"), KB.K("The SQL server connection string").Translate(), true));
		}
		public readonly StringValueOption ServiceCodeOption;
		public readonly StringValueOption SqlConnectionOption;
	}

	public class MainBossServiceConfigurationApplication : Thinkage.Libraries.Application {
		static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		static readonly EmptyTableRights Root = new EmptyTableRights();
		#region Main
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[System.STAThread]
		static void Main(string[] args) {
			if (!IsElevated) {
				Thinkage.Libraries.Application.Instance.DisplayError(new GeneralException(KB.K("MainBoss Service Configuration must be run by a Windows Administrator")));
				Environment.Exit(1);
			}
			try {
				//	 System.Diagnostics.Debugger.Launch();
				//	 System.Diagnostics.Debugger.Break();
				new MainBossServiceConfigurationApplication(args);
				Thinkage.Libraries.Application.Run();
			}
			// General Exceptions that we generate in the application are reported to the user
			catch (Thinkage.Libraries.GeneralException startupException) {
				Thinkage.Libraries.Application.Instance.DisplayError(startupException);
			}
			// We catch System.Data exceptions and report to the user directly
			catch (System.Data.Common.DbException startupException) {
				Thinkage.Libraries.Application.Instance.DisplayError(startupException);
			}
		}

		#endregion

		public MainBossServiceConfigurationApplication(string[] args) {
			// Add other verbs as extra arguments to this ctor.
			Options = new Optable(
				new StartServiceVerb.Definition(),
				new StopServiceVerb.Definition(),
				new RestartServiceVerb.Definition(),
				new ConfigureServiceVerb.Definition(),
				new VerifyServiceVerb.Definition(),
				new DeleteServiceVerb.Definition());
			try {
				Options.Parse(args);
				Options.CheckRequired();
			}
			catch (Thinkage.Libraries.CommandLineParsing.Exception ex) {
				throw new GeneralException(ex.InnerException, KB.T(Thinkage.Libraries.Translation.MessageBuilder.Build(Thinkage.Libraries.Exception.FullMessage(ex), Options.Help)));
			}
			new HelpUsingFolderOfHtml(this, ApplicationParameters.HelpFileLocalLocation, Thinkage.Libraries.Application.InstanceCultureInfo, ApplicationParameters.HelpFileOnlineLocation);
		}
		protected override void CreateUIFactory() {
			new ApplicationTblDefaultsUsingWindows(this, new ETbl(), new AllowAllTablesAllOperationsPermissionsManager(Root.Table), Root.Table, null, null);
			new StandardApplicationIdentification(this, ApplicationParameters.RegistryLocation, "MainBoss Service Control");
			new GUIApplicationIdentification(this, "Thinkage.MainBoss.MainBossServiceConfiguration.Resources.ServiceIcon.ico");
			new Thinkage.Libraries.XAF.UI.MSWindows.UserInterface(this);
			new Thinkage.Libraries.Presentation.MSWindows.UserInterface(this);
			new MSWindowsUIFactory(this);
		}
		public override Libraries.Application.RunApplicationDelegate GetRunApplicationDelegate {
			get {
				return new RunApplicationDelegate(RunApplication);
			}
		}
		private class Optable : Thinkage.Libraries.CommandLineParsing.Optable {
			public Optable(params ServiceVerbDefinition[] subapps) {
				Subapps = subapps;
				object[] arg = new object[subapps.Length * 2];
				for (int i = 0; i < subapps.Length; i++) {
					arg[2 * i] = subapps[i].Verb;
					arg[2 * i + 1] = subapps[i];
				}
				DefineVerbs(true, arg);
			}
			private readonly ServiceVerbDefinition[] Subapps;
			public void RunVerb() {
				Subapps[VerbValue].RunVerb();
			}
		}
		private readonly Optable Options;
		private Thinkage.Libraries.Application RunApplication() {
			Options.RunVerb();
			return null;
		}

	}
	#endregion
	#region MainBossServiceVerb
	internal abstract class MainBossServiceVerb {
		// from command line
		protected Key Action;
		protected string ServiceName;
		protected ServiceParms dbParms;
		protected ServiceVerbWithServiceNameDefinition Options = null;
		protected string ServiceBinary {
			get {
				string currentExecutingPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentExecutingPath), KB.I("Thinkage.MainBoss.Service.exe"));
			}
		}
		protected LogToDatabase Log = null;
		protected MB3Client DBSession = null;
		protected ServiceController Controller = null;
		protected ServiceConfiguration ServiceParameters = null;
		protected MB3Client.ConnectionDefinition DBConnection;
		protected Version MinServerVersion = new Version(0,0,0,0);
		protected String MBVersion = Thinkage.Libraries.VersionInfo.ProductVersion.ToString();

		public enum ServiceOpType { Create, Delete, Use };
		public ServiceOpType OperationType;

		public MainBossServiceVerb(ServiceOpType operationType, Key action, ServiceVerbWithServiceNameDefinition options) {
			OperationType = operationType;
			Options = options;
			Action = action;
			ServiceName = Options.ServiceCodeOption.Value;
			SQLConnectionInfo sqlConnectionInfo = null;
			try {
				sqlConnectionInfo = new SQLConnectionInfo(Options.SqlConnectionOption.Value);
			}
			catch (System.Exception) {
				throw new GeneralException(KB.K("{0}'{1}' is invalid"), KB.I("/Connection:"), Options.SqlConnectionOption.Value);
			}
			VerifyDatabaseServer(sqlConnectionInfo.Server);
			ObtainLog(sqlConnectionInfo);
			MinServerVersion = ServiceUtilities.MinRequiredServiceVersion(DBConnection);
			dbParms = ServiceUtilities.AcquireServiceRecordInfo(sqlConnectionInfo, DBConnection, ServiceName, Log);
			dbParms.ConnectionInfo = sqlConnectionInfo;
		}

		protected void CheckEnvironment() {
			try {
				ServiceParameters = ServiceConfiguration.AcquireServiceConfiguration(true, false, dbParms.ServiceComputer, dbParms.ServiceCode);
				if (ServiceParameters == null) { // no service with that name on that computer
					dbParms.Clear();
					ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, Log);
					throw new GeneralException(KB.K("No Windows Service for MainBoss named '{0}' exists on computer '{1}', the service will have to be reconfigured"), dbParms.ServiceComputer, dbParms.ServiceCode);
				}

				var problem = CheckServiceStartup(ServiceParameters, dbParms);
				if (problem != null)
					throw new GeneralException(KB.T(problem));
				if (ServiceParameters.ServiceDetailsAvailable)
					dbParms = CheckServiceCmdLine(ServiceParameters, dbParms, OperationType == ServiceOpType.Delete ? null : ServiceBinary, ServiceUtilities.MinRequiredServiceVersion(DBConnection));
			}
			catch (System.Exception ex) {
				if (!(ex is GeneralException))
					ex = new GeneralException(ex, KB.K("Errors in MainBoss Service configuration. The Windows Service for MainBoss must be reconfigured"));
				Log.LogError(Thinkage.Libraries.Exception.FullMessage(ex));
				throw ex;
			}
		}
		protected void VerifyDatabaseServer(string computer) {
			if (! DomainAndIP.HasIPAddresses(computer) ) {
				Thinkage.Libraries.Application.Instance.DisplayError(Thinkage.Libraries.Strings.Format(KB.K("Cannot find the IP address of the computer where the SQL Server '{0}' runs"), computer));
				Environment.Exit(1);
			}
		}
		protected void ObtainController() {
			try {
				if (dbParms.ServiceCode == null)
					throw new GeneralException(KB.K("There is no Windows Service for MainBoss configured for database '{0}' on server '{1}'"), DBConnection.DBName, DBConnection.DBServer);
				if (dbParms.ServiceComputer == null)
					throw new GeneralException(KB.K("The Windows Service for MainBoss '{0}' needs to be deleted and then reconfigured"), dbParms.ServiceCode);
				var StaticServiceConfig = new Thinkage.Libraries.Service.StaticServiceConfiguration(dbParms.ServiceComputer ?? Environment.MachineName, dbParms.ServiceCode);
				if (!ServiceController.ServiceExists(StaticServiceConfig))
					throw new GeneralException(KB.K("The Windows Service for MainBoss '{0}' does not exist on computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer ?? DomainAndIP.MyDnsName);
				Controller = new ServiceController();
				Controller.SetControllerInfo(StaticServiceConfig);
				Controller.ThrowLastErrorIfAny();
			}
			catch (GeneralException) {
				throw;
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Cannot access service {0} on computer {1}; the computer may not be available or you may not have sufficient privileges."), dbParms.ServiceCode, dbParms.ServiceComputer);
			}
		}
		protected void ObtainLog(SQLConnectionInfo sqlConnectionInfo) {
			try {
				DBConnection = sqlConnectionInfo.DefineConnection();
				DBSession = new MB3Client(DBConnection);
				Log = new LogToDatabase(DBConnection);
				Log.ProcessLog();
			}
			catch (GeneralException) {
				throw;
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Cannot obtain ServiceLog; "
										+ "most likely, user '{0}' does not "
										+ "have access to the database '{1}' on '{2}'.  "
										+ "User '{0}' does not have to be in the "
										+ "MainBoss users list, but user '{0}' must "
										+ "have permissions to modify the database '{1}'. "
										+ "User '{0}' should have "
										+ "SQL server administration privileges "
										+ "on the SQL server '{2}' "
										+ "in order to obtain all the diagnostics."
										), Environment.UserName, dbParms.ConnectionInfo.DatabaseName, dbParms.ConnectionInfo.DatabaseServer);
			}
		}
		public VerifyServiceAccessToDatabase VerifyServiceUserPermissions(MB3Client.ConnectionDefinition dbConnection, ServiceConfiguration config, AuthenticationCredentials credentials) {
			GeneralException error = null;
			bool sameComputer = DomainAndIP.IsThisComputer(dbParms.ServiceComputer);
			var sqlServerUserid = new VerifyServiceAccessToDatabase(dbParms, config, DBSession.ConnectionInfo, false);
			if (sqlServerUserid.HasAccessToDatabase && sqlServerUserid.IsMainBossUser) return sqlServerUserid;
			error = sqlServerUserid.ReportAccessError();
			if (credentials.Type != AuthenticationMethod.WindowsAuthentication && config != null)
				error = ServiceUtilities.TryAccessToDatabase(config, dbParms);
			//
			// still need check if right version
			// check if correct executable.
			//
			if (error != null) throw error;
			return sqlServerUserid;
		}
		protected TimeSpan ServiceOperationTimeOut = TimeSpan.FromMinutes(2);
		protected void DoServiceOperation(Key operation, ServiceController.Statuses wantedStatus) {
			System.Exception ex = null;
			try {
				Controller.ThrowLastErrorIfAny();
				if (wantedStatus == ServiceController.Statuses.Running) {
					if (Controller.Status != ServiceController.Statuses.StartPending && Controller.Status != ServiceController.Statuses.Running)
						Controller.Start();
				}
				else if (Controller.Status != ServiceController.Statuses.StopPending && Controller.Status != ServiceController.Statuses.Stopped)
					Controller.Stop();

				Controller.ThrowLastErrorIfAny();
			}
			catch (System.ServiceProcess.TimeoutException) {
				if (Log != null)
					Log.LogError(Strings.Format(KB.K("{0} operation timed out after {1} seconds on service {2} on computer {3}."), operation, ServiceOperationTimeOut.TotalSeconds, dbParms.ServiceCode, dbParms.ServiceComputer));
				throw new GeneralException(KB.K("{0} operation timed out after {1} seconds on service {2} on computer {3}."), operation, ServiceOperationTimeOut.TotalSeconds, dbParms.ServiceCode, dbParms.ServiceComputer);
			}
			catch (System.InvalidOperationException e) {
				var win32 = e.InnerException as System.ComponentModel.Win32Exception;
				GeneralException ef = null;
				if (win32 != null && win32.NativeErrorCode == 2) // file not found
					ef = new GeneralException(KB.K("The executable for the Windows Service for MainBoss '{0}' was not found. The Windows Service for MainBoss needs to be deleted and then reconfigured"), Controller.ServiceName);
				if (win32 != null && win32.NativeErrorCode == 5)  // access denied
					ef = new GeneralException(KB.K("The userid '{0}' did not have read and execute permissions on the executable files for the Windows Service for MainBoss '{1}'"), ServiceParameters.StartName, Controller.ServiceName);
				if (ef != null) {
					if (!DomainAndIP.IsThisComputer(Controller.ServiceComputer))
						ef.WithContext(new MessageExceptionContext(KB.K("On computer '{0}'"), Controller.ServiceComputer));
					var path = ServiceUtilities.GetServiceImagePath(Controller.ServiceComputer, Controller.ServiceName);
					if (path != null)
						ef.WithContext(new MessageExceptionContext(KB.K("Service command line '{0}'"), path));
					throw ef;
				}
				ex = e;
			}
			catch (System.Exception e) {
				ex = e;
			}
			if ( Controller.WaitForStatus(wantedStatus) )
				return; // all errors will be forgiven if the service is in desired state
			ex = ex ?? (System.Exception)Controller.LastStatusError;
			Log.LogError(Strings.IFormat("{0}  {1}", Strings.Format(Thinkage.Libraries.Service.Application.InstanceCultureInfo, KB.K("Cannot {0} Windows Service for MainBoss '{1}' on computer '{2}'."), operation, dbParms.ServiceCode, dbParms.ServiceComputer), ex == null ? "" : Thinkage.Libraries.Exception.FullMessage(ex)));
			throw new GeneralException(ex, KB.K("Cannot {0} Windows Service for MainBoss '{1}' on computer '{2}'."), operation, dbParms.ServiceCode, dbParms.ServiceComputer);
		}
		protected static ServiceParms CheckServiceCmdLine(ServiceConfiguration sc, ServiceParms parms, string serviceBinary, Version minVersion) {
			Libraries.Exception baseError = null;
			baseError = ServiceUtilities.TestForValidMainBossService(sc, parms);
			// If a user creates another database from a backup on the both database will have the same Service Configuration
			// The one from the second database will be wrong. The user has to be able to delete the service configuration 
			// on the new database without deleting the service that belongs to the other one.
			// the inverse problem exists, if there is no other database using this service, then there will be a lost service
			// that no one is using and can only be delected by using the 'sc' command.
			// trying to configure a new MainBoss Service will note the existence of that lost service, and allow the user to re configure the lost service
			// then delete it.
			if (baseError == null)
				baseError = ServiceUtilities.TestForMainBossServiceVersion(sc, parms, serviceBinary, minVersion);
			if (baseError != null)
				throw baseError;
			if (baseError != null && parms.WorkingServiceUserid != sc.StartName)
				Thinkage.Libraries.Application.Instance.DisplayWarning(Strings.Format(KB.K("The Windows Service for MainBoss '{0}' is configured to run as user {1} but the MainBoss Service configuration record shows the service to be running as '{2}'."), sc.ServiceName, sc.StartName, parms.WorkingServiceUserid));
			return parms;
		}
		protected string CheckServiceStartup(ServiceConfiguration sc, ServiceParms parms ) {
			if (sc == null) return null;
			string problemText = null;
			if (sc.StartType == System.ServiceProcess.ServiceStartMode.Disabled) {
				var p = addError(problemText, Strings.Format(KB.K("The Windows Service for MainBoss '{0}' on computer '{1}' can not be run because it has been disabled"), sc.ServiceName, sc.ServiceComputer));
				Log.LogWarning(problemText);
				problemText = addError(problemText, p);
			}
			if (sc.StartType == System.ServiceProcess.ServiceStartMode.Manual) {
				var p = Strings.Format(KB.K("Start mode of MainBoss Service '{0}' is 'Manual'. It should be set to 'Automatic (Delayed Start)'"), sc.ServiceName);
				Log.LogWarning(p);
				problemText = addError(problemText, p);  // not sure if it should be an error, or just a warning
			}
			if (parms.ServiceUserid != null && Utilities.UseridToDisplayName(sc.StartName) != parms.ServiceUserid) {
				var p = Strings.Format(KB.K("The Service Configuration expects the service to be running as user '{0}'. The MainBoss Service is configured to be running as {1}")
					, parms.ServiceUserid, Utilities.UseridToDisplayName(sc.StartName));
				Log.LogWarning(p);
				problemText = addError(problemText, p);
			}
			return problemText;
		}
		protected string addError(string old, string e) {
			if (old == null) return e;
			return old + Environment.NewLine + Environment.NewLine + e;
		}
		protected void accessQuestion(VerifyServiceAccessToDatabase accessToDatabase) {
			if (!accessToDatabase.LoginExists && accessToDatabase.CanManageUserLogins || !accessToDatabase.IsMainBossUser && accessToDatabase.ViewLocalUsers) {
				var questiontext1 = Strings.Format(KB.K("The Windows Service for MainBoss is currently configured to run as user {0}.  "
									+ "This user does not have the expected permissions "
									+ "to work with the {1}.  "
									+ "This user must have these permissions for "
									+ "the Windows Service for MainBoss to operate correctly."), accessToDatabase.SqlUseridFormatted, DBConnection.DisplayName);
				var questiontext2 = Strings.Format(KB.K("Do you want this user to have a login into the SQL Server "
						+ "and required permission to work with the MainBoss database?"));
				if (Thinkage.Libraries.Application.Instance.AskQuestion(questiontext1 + Environment.NewLine + Environment.NewLine + questiontext2,
					Strings.Format(KB.K("Grant {0} Sql Permissions"), accessToDatabase.SqlUseridFormatted), Ask.Questions.YesNo) == Ask.Result.No) {
					throw new GeneralException(KB.K("User '{0}' does not have correct permissions on database {1}"), accessToDatabase.SqlUseridFormatted, DBConnection.DisplayName);
				}
				var notGranted = accessToDatabase.GrantAccessToDatabase();
				if (notGranted != null)
					throw notGranted;
				Log.LogInfo(Strings.Format(KB.K("SQL login added to database for user {0}. Used by the Windows Service for MainBoss '{1}' on '{2}'"), accessToDatabase.SqlUseridFormatted, ServiceName, DomainAndIP.MyDnsName));
			}
		}
		#endregion
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		protected static extern bool PathIsNetworkPath(string Path);
	}

	#region ConfigureService
	internal class ConfigureServiceVerb : MainBossServiceVerb {
		public class Definition : ServiceVerbWithServiceNameDefinition {
			public Definition() : base("ConfigureService") {
			}
			public override void RunVerb() {
				new ConfigureServiceVerb(this).Run();
			}
		}
		private ConfigureServiceVerb(Definition options) : base(ServiceOpType.Create, KB.K("Configure Service"), options) {}
		private void Run() {
			if (dbParms.ServiceComputer == null)
				dbParms.ServiceComputer = DomainAndIP.MyDnsName;
			bool oKToContinue = false;
			bool sameComputer = DomainAndIP.IsThisComputer(dbParms.ServiceComputer);
			if( ! sameComputer )
				throw new GeneralException(KB.K("The Windows Service for MainBoss must be configured from the computer that it is to be run on."), dbParms.ServiceCode);
			try { 
				ServiceParameters = ServiceConfiguration.AcquireServiceConfiguration(true, true, dbParms.ServiceComputer, dbParms.ServiceCode);
			}
			catch(System.Exception e ) {
					throw e as GeneralException ?? new GeneralException(e, KB.K("Cannot access Windows Service properties"), dbParms.ServiceComputer);
			}
			if ( ServiceParameters != null && !ServiceParameters.ServiceDetailsAvailable )
				throw new GeneralException(KB.K("Insufficient permissions to examine the details of the Windows Service for MainBoss '{0}'"), dbParms.ServiceCode);
			if (ServiceParameters != null) {
				//
				// if a service record exists. One of the following occurs.
				// 1) Some else added the record while this program was running, and every thing is fine.
				// 2) The service is an old one and should be updated.
				// 3) The service record was removed from the database with out removing the record
				//    (delete service will do this, but only after asking, the functionality is need when a computer will no longer be available.
				//    in this case we quietly re add the service)
				if( ServiceParameters.ConnectionInfo == null ) {
					if (!ServiceParameters.IsMainBossDatabase)
						throw new GeneralException(KB.K("There is already a Windows Service named '{0}' on this computer"), dbParms.ServiceCode);
					else
						throw new GeneralException(KB.K("There is an old Windows Service for MainBoss '{0}' that should be deleted"), dbParms.ServiceCode);
				}
				else if (string.Compare(ServiceParameters.ConnectionInfo.DatabaseServer, dbParms.ConnectionInfo.DatabaseServer, true) != 0
							|| string.Compare(ServiceParameters.ConnectionInfo.DatabaseName, dbParms.ConnectionInfo.DatabaseName, true) != 0)
					throw new GeneralException(KB.K("A Windows Service for MainBoss '{0}' exists for this computer but the Service is for Database '{1}' on SQL Server '{2}'")
						, dbParms.ServiceCode, ServiceParameters.DatabaseName, ServiceParameters.DatabaseServer);
				if( dbParms.MBVersion == null ) { // no service record exists, but a service exists and is for our database, create a service record for it.
					dbParms.ServiceComputer = DomainAndIP.MyDnsName;
					dbParms.ServiceUserid = ServiceParameters.ServiceUserid;
					dbParms.ConnectionInfo = ServiceParameters.ConnectionInfo;
					dbParms.MBVersion = ServiceParameters.Version;
					ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, Log);
				}
				bool needsUpdate = ServiceBinary != ServiceParameters.ServiceExecutable ;
				if (string.Compare(Utilities.UseridToRegistryName(ServiceParameters.ServiceUserid), Utilities.UseridToRegistryName(dbParms.ServiceUseridAsSqlUserid)) != 0) {
					var questiontextu = Strings.Format(KB.K("There is an existing Windows Service for MainBoss which is using Service Userid '{0}', the MainBoss Service configuration uses Userid '{1}'. Do you want to change the Service Userid?"), Utilities.UseridToDisplayName(ServiceParameters.ServiceUserid), dbParms.ServiceUserid);
					if (Thinkage.Libraries.Application.Instance.AskQuestion(questiontextu, Strings.Format(KB.K("Change MainBoss Service Userid")), Ask.Questions.YesNo) == Ask.Result.Yes) {
						ServiceParameters.ServiceUserid = Utilities.UseridToRegistryName(dbParms.ServiceUserid);
						needsUpdate = true;
					}
				}
				if (string.Compare(ServiceParameters.ConnectionInfo.ConnectionString, dbParms.ConnectionInfo.ConnectionString) != 0) {
					var questiontextc = Strings.Format(KB.K("There is an existing Windows Service for MainBoss using {0}. Do you wish to change the credentials to {1}?"),
						ServiceParameters.ConnectionInfo.Credentials.ToString(), dbParms.ConnectionInfo.Credentials.ToString());
					needsUpdate = true;
					if (Thinkage.Libraries.Application.Instance.AskQuestion(questiontextc, Strings.Format(KB.K("Change MainBoss Service Credentials")), Ask.Questions.YesNo) == Ask.Result.Yes) 
						ServiceParameters.ConnectionInfo = dbParms.ConnectionInfo;
					else
						dbParms.ConnectionInfo = ServiceParameters.ConnectionInfo;
				}
				needsUpdate |= ServiceParameters.StartType != System.ServiceProcess.ServiceStartMode.Automatic;
				if (ServiceParameters.Version == MBVersion && !needsUpdate) {
					Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("The Windows Service for MainBoss '{0}' is up to date"), dbParms.ServiceCode));
					Log.LogClose(null);
					return;
				}
				oKToContinue = true;
			}
			var accessToDatabase = new VerifyServiceAccessToDatabase(dbParms, ServiceParameters, DBSession.ConnectionInfo, false);
			var problem = accessToDatabase.ReportAccessPermissionOnDatabase();
			if (problem != null)
				Thinkage.Libraries.Application.Instance.DisplayWarning(problem);
			if ( !sameComputer &&  ServiceParameters == null) {
				var questiontext1 = KB.K("The service was installed on computer '{0}', but is no longer installed there.{1}Do you want to install a Windows Service for MainBoss '{2}' on this computer?");
				oKToContinue = Thinkage.Libraries.Application.Instance.AskQuestion(Strings.Format(questiontext1,dbParms.ServiceComputer, Environment.NewLine, dbParms.ServiceCode), Strings.Format(KB.K("Create Windows Service for MainBoss '{0}'"), dbParms.ServiceCode), Ask.Questions.YesNo) == Ask.Result.Yes;
				if (!oKToContinue) {
					ServiceUtilities.DeleteServiceFromDatabase(DBConnection, dbParms, Log);
					throw new GeneralException(KB.K("Creation of Windows Service for MainBoss '{0}' on computer '{1}' canceled"), dbParms.ServiceCode);
				}
				dbParms.ServiceComputer = DomainAndIP.MyDnsName;
				sameComputer = true;
			}
			if (String.Compare(accessToDatabase.SqlUserid, Utilities.LOCALADMIN_DISPLAYNAME, ignoreCase: true) == 0)
				Log.LogWarning(Strings.Format(KB.K("Windows Service for MainBoss should not run as user {0}. Use 'NT AUTHORITY\\Network Service' instead"), accessToDatabase.SqlUseridFormatted));
			accessQuestion(accessToDatabase);
			if (PathIsNetworkPath(ServiceBinary))
				throw new GeneralException(KB.K("The MainBoss executable must be on the computer where the Windows Service for MainBoss is to be created."))
					.WithContext(new MessageExceptionContext(KB.K("The MainBoss executable location is '{0}'"), Path.GetDirectoryName(ServiceBinary)));
			var questiontext = Strings.Format(KB.K("Do you want to create a Windows Service for MainBoss '{0}' on this computer?"), dbParms.ServiceCode);
			if (!oKToContinue)
				oKToContinue = Thinkage.Libraries.Application.Instance.AskQuestion(questiontext, Strings.Format(KB.K("Create Windows Service for MainBoss '{0}'"), dbParms.ServiceCode), Ask.Questions.YesNo) == Ask.Result.Yes;
			if (!oKToContinue)
				throw new GeneralException(KB.K("Creation of Windows Service for MainBoss '{0}' on computer '{1}' canceled"), dbParms.ServiceCode, DomainAndIP.MyDnsName);
			dbParms.ServiceComputer = DomainAndIP.MyDnsName;
			if (ServiceParameters != null) {
				dbParms.MBVersion = MBVersion;
				ServiceParameters.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
				ServiceParameters.BinaryPathName = ServiceConfiguration.ServiceCmdLine(ServiceBinary, dbParms.ConnectionInfo, MBVersion, dbParms.ServiceCode);
				ServiceParameters.Update();
				Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("Windows Service for MainBoss '{0}' has been updated on computer '{1}'"), dbParms.ServiceCode, DomainAndIP.MyDnsName));
				ServiceUtilities.UpdateServiceRecord(DBConnection, dbParms, Log);
			}
			else {
				dbParms = ServiceUtilities.CreateMainBossService(DBConnection, dbParms, ServiceBinary, Log);
				Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("Windows Service for MainBoss '{0}' has been created on computer '{1}'"), dbParms.ServiceCode, DomainAndIP.MyDnsName));
			}
			Log.LogClose(null);
		}
	}
	#endregion
	#region VerifyService
	internal class VerifyServiceVerb : MainBossServiceVerb {
		public class Definition : ServiceVerbWithServiceNameDefinition {
			public Definition() : base("VerifyService") {
			}
			public override void RunVerb() {
				new VerifyServiceVerb(this).Run();
			}
		}
		private VerifyServiceVerb(Definition options) : base(ServiceOpType.Create, KB.K("Verify Service"), options) {
			if (dbParms.ServiceComputer != null && !DomainAndIP.HasIPAddresses(dbParms.ServiceComputer))
				throw new GeneralException(KB.K("Cannot find an IP address for the computer '{0}' referenced in the MainBoss Service configuration information"), dbParms.ServiceComputer);
		}
		private void Run() {
			if (dbParms.ServiceComputer == null)
				dbParms.ServiceComputer = DomainAndIP.MyDnsName;
			if (dbParms.MBVersion == null)
				throw new GeneralException(KB.K("Windows Service for MainBoss '{0}' is not installed on computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer);
			bool sameComputer = DomainAndIP.IsThisComputer(dbParms.ServiceComputer);
			try {
				ServiceParameters = ServiceConfiguration.AcquireServiceConfiguration(true, true, dbParms.ServiceComputer, dbParms.ServiceCode);
			}
			catch (System.Exception e) {
				if (sameComputer)
					throw e as GeneralException ?? new GeneralException(e, KB.K("Cannot access Windows Service properties"));
				throw e as GeneralException ?? new GeneralException(e, KB.K("Cannot access Windows Service properties")).WithContext(Strings.Format(KB.K("Run this command on computer '{0}'"), dbParms.ServiceComputer));
			}
			if (ServiceParameters != null && !ServiceParameters.ServiceDetailsAvailable) {
				var ex = new GeneralException(KB.K("Insufficient permissions to examine the details of the Windows Service for MainBoss '{0}'"), dbParms.ServiceCode);
				if (!sameComputer)
					Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Login to computer '{0}' to check the configuration of the service"), dbParms.ServiceComputer));
				throw ex;
			}
			var accessToDatabase = new VerifyServiceAccessToDatabase(dbParms, ServiceParameters, DBSession.ConnectionInfo, false);
			if (ServiceParameters == null) {
				ServiceUtilities.DeleteServiceFromDatabase(DBConnection, dbParms, Log);
				throw new GeneralException(KB.K("Windows Service for MainBoss '{0}' is not installed on computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer)
					.WithContext(new Thinkage.Libraries.MessageExceptionContext(KB.K("MainBoss Service configuration information has been corrected.")));
			}
			GeneralException accessErrors = null;
			if (accessToDatabase.Error != null)
				throw accessToDatabase.Error;
			var notMine = ServiceUtilities.TestForValidMainBossService(ServiceParameters, dbParms);
			if (notMine != null)
				throw notMine;
			var versionError = ServiceUtilities.TestForMainBossServiceVersion(ServiceParameters, dbParms, ServiceBinary, MinServerVersion);
			if (versionError != null)
				throw versionError;
			if (ServiceParameters.Credentials.Type != AuthenticationMethod.WindowsAuthentication)
				accessErrors = ServiceUtilities.TryAccessToDatabase(ServiceParameters, dbParms);
			if (accessErrors != null)
				accessQuestion(accessToDatabase);
			string startupErrors = CheckServiceStartup(ServiceParameters, dbParms);
			var p = accessToDatabase.ReportAccessPermissionOnDatabase();
			if (startupErrors != null)
				Thinkage.Libraries.Application.Instance.DisplayError(startupErrors);
			else if (sameComputer)
				Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("Configuration testing for the MainBoss Service '{0}' on {1} using SQL access with {2} was successful"), dbParms.ServiceCode, accessToDatabase.ServiceConfiguration.DisplayName, accessToDatabase.SqlUseridFormatted));
			else
				Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("Configuration testing for the MainBoss Service '{0}' on {1}"
					+ " using SQL access with {2} was successful but it is not possible to verify that the service executable is accessible since it is on a different computer")
					, dbParms.ServiceCode, accessToDatabase.ServiceConfiguration.DisplayName, accessToDatabase.SqlUseridFormatted));
			Log.LogClose(null);
		}
	}
	#endregion

	#region DeleteService
	internal class DeleteServiceVerb : MainBossServiceVerb {
		public class Definition : ServiceVerbWithServiceNameDefinition {
			public Definition() : base("DeleteService") { }
			public override void RunVerb() {
				new DeleteServiceVerb(this).Run();
			}
		}
		private DeleteServiceVerb(Definition options) : base(ServiceOpType.Delete, KB.K("Delete Service"), options) {
		}
		private void Run() {
			var sameComputer = DomainAndIP.IsThisComputer(dbParms.ServiceComputer);
			bool canDeleteService = false;
			bool serviceComputerExists = false;
			Libraries.Exception NotMainBossService = null;
			try {
				ServiceParameters = ServiceConfiguration.AcquireServiceConfiguration(true, true, dbParms.ServiceComputer, dbParms.ServiceCode);
				if (ServiceParameters != null) {
					if (!ServiceParameters.IsMainBossDatabase)
						throw ServiceParameters.LastError;
					NotMainBossService = ServiceUtilities.TestForValidMainBossService(ServiceParameters, dbParms);
				}
				serviceComputerExists = true;
				canDeleteService = true;
			}
			catch {
				canDeleteService = false;
			}
			if (!sameComputer && ServiceParameters == null) {
				foreach (var a in DomainAndIP.IpAddresses(dbParms.ServiceComputer)) {
					try { // try a ping to see if computer exists
						var ping = new System.Net.NetworkInformation.Ping();
						var pingResult = ping.Send(a);
						serviceComputerExists = pingResult.Status == System.Net.NetworkInformation.IPStatus.Success;
						break;
					}
					catch { };
				}
				var questionLine1 = Strings.Format(KB.K("The Windows Service for MainBoss '{0}' was installed on computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer);
				string questionLine2;
				if (!serviceComputerExists)
					questionLine2 = Strings.Format(KB.K("Cannot determine if computer '{0}' still exists."), dbParms.ServiceComputer);
				else
					questionLine2 = Strings.Format(KB.K("Computer '{0}' exists, but am not able to determine if a Windows Service for MainBoss is currently installed on that computer"), dbParms.ServiceComputer);
				var questionLine3 = Strings.Format(KB.K("Do you want the MainBoss Service configuration information on that Windows Service for MainBoss to be removed?"));
				var questionLine4 = Strings.Format(KB.K("If the Windows Service for MainBoss is still installed on computer '{0}', the service will have to be deleted manually on that computer, using the Window's 'SC DELETE' command")
															, dbParms.ServiceComputer);
				var questionText = Strings.IFormat("{0}{1}{2}{3}{4}{5}{6}", questionLine1, Environment.NewLine + Environment.NewLine, questionLine2, Environment.NewLine, questionLine3, Environment.NewLine + Environment.NewLine, questionLine4);
				canDeleteService = Thinkage.Libraries.Application.Instance.AskQuestion(questionText, Strings.Format(KB.K("Erase MainBoss Service configuration information on the Windows Service for MainBoss on computer '{0}'")
															, dbParms.ServiceCode), Ask.Questions.YesNo) == Ask.Result.Yes;
				if (!canDeleteService)
					Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("Windows Service for MainBoss '{0}' was not deleted"), dbParms.ServiceCode));
				else {
					ServiceUtilities.DeleteServiceFromDatabase(DBConnection, dbParms, Log);
					Log.LogInfo(Strings.Format(KB.K("The MainBoss Service configuration information on the Windows Service for MainBoss '{0}' on computer '{1}' has been removed"), dbParms.ServiceCode, dbParms.ServiceComputer));
				}
				Log.LogClose(null);
				return;
			}
			bool okToDelete = deleteServiceQuestion(canDeleteService, sameComputer, NotMainBossService, dbParms);
			if (!okToDelete)
				Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("The Windows Service for MainBoss '{0}' has not been removed from computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer));
			else { 
				if (dbParms.ServiceComputer == null) // in case a service exists but no service record
					dbParms.ServiceComputer = DomainAndIP.MyDnsName;
				if (ServiceParameters != null) { // service does no actual exist but MainBoss thinks it does.
					ObtainController();
					DoServiceOperation(KB.K("Stop"), ServiceController.Statuses.Stopped);
					Controller?.Dispose();
					Controller = null;
				}
				dbParms.MBVersion = null;
				if (dbParms.ServiceCode != null && ServiceParameters != null ) {
					try {
						//System.Diagnostics.Debug.Assert(false, "Service delete testing");
						ServiceInstaller.Uninstall(dbParms.ServiceCode, dbParms.ServiceComputer);
						Log.LogInfo(Strings.Format(KB.K("Windows Service for MainBoss '{0}' on computer '{1}' has been removed"), dbParms.ServiceCode, dbParms.ServiceComputer));
					}
					catch (System.Exception ex) {
						Log.LogError(Strings.Format(KB.K("Cannot remove Windows Service for MainBoss '{0}' on computer '{1}' : {2}"), dbParms.ServiceCode, dbParms.ServiceComputer, Thinkage.Libraries.Exception.FullMessage(ex)));
					}
				}
				ServiceUtilities.DeleteServiceFromDatabase(DBConnection, dbParms, Log);
				Thinkage.Libraries.Application.Instance.DisplayInfo(Strings.Format(KB.K("The Windows Service for MainBoss '{0}' has been removed from computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer));
			}
			Log.LogClose(null);
		}
		bool deleteServiceQuestion(bool canDeleteService, bool sameComputer, Thinkage.Libraries.Exception serviceError, ServiceParms dbParms) {
			if (canDeleteService) {
				string questionText = sameComputer ? Strings.Format(KB.K("Remove the Windows Service for MainBoss '{0}'"), dbParms.ServiceCode)
					: Strings.Format(KB.K("Remove the Windows Service for MainBoss '{0}' installed on computer '{1}'"), dbParms.ServiceCode, dbParms.ServiceComputer);
				if (serviceError != null)
					questionText = string.Concat(Thinkage.Libraries.Exception.FullMessage(serviceError), Environment.NewLine + Environment.NewLine, questionText);
				return canDeleteService = Thinkage.Libraries.Application.Instance.AskQuestion(questionText, Strings.Format(KB.K("Delete the MainBoss Service")), Ask.Questions.YesNo) != Ask.Result.No;
			}
			else if (!canDeleteService && !sameComputer )
				throw new GeneralException(KB.K("Login to computer '{0}' to delete the Windows Service for MainBoss '{1}'"), dbParms.ServiceComputer, dbParms.ServiceCode);
			throw new GeneralException(KB.K("You do not have permission to access windows services on computer '{0}'"), dbParms.ServiceComputer);
		}
	}
	#endregion
	#region StartService
	internal class StartServiceVerb: MainBossServiceVerb	{
		public class Definition : ServiceVerbWithServiceNameDefinition {
			public Definition()	: base("StartService") {}
			public override void RunVerb()	{
				new StartServiceVerb(this).Run();
			}
		}
		private StartServiceVerb(Definition options) : base(ServiceOpType.Use, KB.K("Start Service"), options) {
			if (dbParms.ServiceComputer != null && !DomainAndIP.HasIPAddresses(dbParms.ServiceComputer) )
				throw new GeneralException(KB.K("Cannot find an IP address for the computer '{0}' referenced in the MainBoss Service configuration information"), dbParms.ServiceComputer);
		}
		private void Run() {
			ObtainController();
			CheckEnvironment();
			if (DBSession != null && ServiceParameters.ServiceFound) {// if we can't get to the database or get the to the registry we can't verify the user.
				ServiceUtilities.VerifyServiceParameters(ServiceParameters, dbParms, Log);
				VerifyServiceUserPermissions(DBConnection, ServiceParameters, ServiceParameters.Credentials);
			}
			DoServiceOperation(KB.K("Start"), ServiceController.Statuses.Running);
			Controller.Dispose();
			Log.LogClose(null);
		}
	}
	#endregion
	#region StopService
	internal class StopServiceVerb : MainBossServiceVerb	{
		public class Definition : ServiceVerbWithServiceNameDefinition {
			public Definition() 	: base("StopService") {}
			public override void RunVerb()	{
				new StopServiceVerb(this).Run();
			}
		}
		private StopServiceVerb(Definition options) : base(ServiceOpType.Use, KB.K("Stop Service"), options) {
			if (dbParms.ServiceComputer != null && !DomainAndIP.HasIPAddresses(dbParms.ServiceComputer) )
				throw new GeneralException(KB.K("Cannot find an IP address for the computer '{0}' referenced in the MainBoss Service configuration information"), dbParms.ServiceComputer);
		}
		private void Run()	{
			ObtainController();
			CheckEnvironment();
			DoServiceOperation(KB.K("Stop"), ServiceController.Statuses.Stopped);
			Controller.Dispose();
			Log.LogClose(null);
		}
	}
	#endregion
	#region RestartService
	internal class RestartServiceVerb: MainBossServiceVerb {
		public class Definition : ServiceVerbWithServiceNameDefinition {
			public Definition() : base("RestartService") { }
			public override void RunVerb() {
				new RestartServiceVerb(this).Run();
			}
		}
		private RestartServiceVerb(Definition options) : base(ServiceOpType.Use, KB.K("Restart Service"), options) {
			if (dbParms.ServiceComputer != null && !DomainAndIP.HasIPAddresses(dbParms.ServiceComputer) )
				throw new GeneralException(KB.K("Cannot find an IP address for the computer '{0}' referenced in the MainBoss Service configuration information"), dbParms.ServiceComputer);
		}
		private void Run() {
			ObtainController();
			CheckEnvironment();
			if (DBSession != null && ServiceParameters.ServiceFound ) {// if we can't get to the database or get the to the registry we can't verify the user.
				ServiceUtilities.VerifyServiceParameters(ServiceParameters, dbParms, Log);
				VerifyServiceUserPermissions(DBConnection, ServiceParameters, ServiceParameters.Credentials);
			}
			if( ServiceParameters.StartType == System.ServiceProcess.ServiceStartMode.Disabled) 
				throw new GeneralException(KB.K("The Windows Service for MainBoss '{0}' on computer '{1}' can not be run because it has been disabled"), Controller.ServiceName, Controller.ServiceComputer);
			else {
				DoServiceOperation(KB.K("Stop"), ServiceController.Statuses.Stopped);
				DoServiceOperation(KB.K("Start"), ServiceController.Statuses.Running);
			}
			Controller.Dispose();
			Log.LogClose(null);
		}
	}
	#endregion

	#region AllowAllTablesAllOperationsPermissionManager
	public class AllowAllTablesAllOperationsPermissionsManager : PermissionsManager, IRightGrantor	{
		public AllowAllTablesAllOperationsPermissionsManager(TableRightsGroup namedGroup)
			: base(true,(PermissionsManager creator, PermissionsGroup owner, string name)=> new Thinkage.Libraries.XAF.UI.PermissionDisabler(owner, name))	{
			FindPermissionGroup(namedGroup.QualifiedName).SetPermission(Strings.IFormat("{0}.*.*", namedGroup.QualifiedName), true);
		}
		#region IRightGrantor Members
		public Permission GetPermission(Right r) {
			return FindPermission(r.QualifiedName);
		}
		public GeneralException ValidatePermission(string permissionPattern) {
			throw new NotImplementedException();
		}
			#endregion
	}
	#region EmptyTableRights - The Rights structure that defines all the controlled operations in this app for Tables
	public class EmptyTableRights : NamedRightsGroup {
#region Constructor
		public EmptyTableRights() 	: base() {}
#endregion
		public readonly TableRightsGroup Table;
	}
	#endregion
	#endregion
}
