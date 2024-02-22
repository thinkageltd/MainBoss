using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Provide access to a service's registry key, and provide support for a 'Parameters' subkey for a service.
	/// Note this class does NOT implement IServiceConfiguration as this class ONLY works on the machine the service is installed on and is not intended
	/// for cross network use (whereas IServiceConfiguration is)
	/// </summary>
	public class ServiceConfiguration : ServiceInfo {
		#region Properties 
		private string pVersion;
		public string Version {
			[Invariant]
			get { return pVersion; }
			set {
				if (pVersion == value)
					return;
				pVersion = value;
				setServiceCmdLine();
			}
		}
		private IServiceConfiguration pStaticConfig;
		public  IServiceConfiguration StaticConfig {
			get { return pStaticConfig; }
			set {
				pStaticConfig = value;
				setServiceCmdLine();
			}
		}
		public string ServiceCode => StaticConfig.ServiceName;
		public string ServiceMachineName => StaticConfig.ServiceMachineName;
		public string DatabaseServer => pConnectionInfo?.DatabaseServer;
		public string DatabaseName => pConnectionInfo?.DatabaseName;
		private string pServiceExecutable;
		public string ServiceExecutable {
			[Invariant]
			get { return pServiceExecutable; }
			set {
				if (pServiceExecutable == value)
					return;
				pServiceExecutable = value;
				setServiceCmdLine();
			}
		}
		private SQLConnectionInfo pConnectionInfo = null;
		public SQLConnectionInfo ConnectionInfo {
			get { return pConnectionInfo; }
			set {
				if (pConnectionInfo?.ConnectionString == value?.ConnectionString )
					return;
				pConnectionInfo = value;
				setServiceCmdLine();
			}
		}
		public AuthenticationCredentials Credentials => pConnectionInfo?.Credentials;
		public bool IsMainBossDatabase { get; private set; } =  false;
		public string ServiceUserid {
			[Invariant]
			get {
				if (!string.IsNullOrEmpty(StartName))
					return Utilities.UseridToDisplayName(StartName);
				StartName = Utilities.LOCALADMIN_REGISTRYNAME;
				return StartName;
			}
			set {
				if (string.Compare(StartName, value, true) != 0) {
					StartName = value;
				}
			}
		}
		#endregion

		private System.ServiceProcess.ServiceController Controller = null;
		bool disposeController = true;
		//
		// accessing a controller on another computer can take many seconds. 
		// the controller allows this class to skip that delay
		// but only supply the controller if you can garantee that the lifetime 
		// of the SericeConfiguration will be less than that of the controller.
		// 
		public ServiceConfiguration(IServiceConfiguration config, bool admin = false, System.ServiceProcess.ServiceController controller = null)
			: base(config.ServiceName, config.ServiceMachineName, admin) {
			pStaticConfig = config;
			Controller = controller;
			if (BinaryPathName != null) {
				try {
					if (DisplayName == null ||  !DisplayName.StartsWith(KB.I("MainBoss")))
						throw new GeneralException(KB.K("Service '{0}' exists but is not a MainBoss Service"), config.ServiceName);
					IsMainBossDatabase = true;
					var serviceArguments = new ServiceStartupOptions(BinaryPathName);
					pServiceExecutable = serviceArguments.Executable;
					pVersion = serviceArguments.Version ?? new Version(3,0,0,0).ToString();
					pConnectionInfo = serviceArguments.Connection;
					if (LastError == null && (Version == null || ConnectionInfo == null)) {
						LastError = new GeneralException(KB.K("The Windows Service for MainBoss '{0}' is an old version."), config.ServiceName);
						LastError = LastError.WithContext(new MessageExceptionContext(KB.K("MainBoss Service should be removed using uninstall a program in the Control Panel's 'Programs and Features'.")));
						LastError = LastError.WithContext(new MessageExceptionContext(KB.K("The changes made to this Windows Service for MainBoss will be lost when the old 'MainBoss Service' is uninstalled. Please do the uninstall first.")));
					}
				}
				catch ( Thinkage.Libraries.Exception e) {
					LastError = e;
				}
			}
		}
		private void setServiceCmdLine() {
			BinaryPathName =  ServiceCmdLine(ServiceExecutable, ConnectionInfo, Version, ServiceCode);
		}
		public static string ServiceCmdLine([Invariant]string executable, SQLConnectionInfo sqlConnectionInfo, [Invariant]string version,[Invariant]string serviceCode) {
			if (sqlConnectionInfo.Credentials.Type == AuthenticationMethod.WindowsAuthentication)
				return Strings.IFormat("\"{0}\" /ServiceName:{1} /Connection:{2}  /Version:\"{3}\"", executable, serviceCode, EscapeArg(sqlConnectionInfo.ConnectionString), version);
			var noPassword = sqlConnectionInfo.ConnectionStringNoPassword;
			var encrypted = Convert.ToBase64String(Database.Service.ServicePassword.Encode(sqlConnectionInfo.SqlPassword));
			return Strings.IFormat("\"{0}\" /ServiceName:{1} /Connection:{2} /Version:\"{3}\" /SecurityToken:{4} ", executable, serviceCode, EscapeArg(noPassword), version.ToString(), EscapeArg(encrypted));
		}		
		/// <summary>
		/// Quotes all arguments that contain whitespace, or begin with a quote and returns a single
		/// argument string for use with Process.Start().
		/// </summary>
		/// <param name="args">A list of strings for arguments, may not contain null, '\0', '\r', or '\n'</param>
		/// <returns>The combined list of escaped/quoted strings</returns>
		static Regex invalidChar = new Regex("[\x00\x0a\x0d]"); //  these can not be escaped
		static Regex needsQuotes = new Regex(@"\s|""");         //  contains whitespace or two quote characters
		static Regex escapeQuote = new Regex(@"(\\*)(""|$)");   //  one or more '\' followed with a quote or end of string
		public static string EscapeArg([Invariant] string arg) {
			if (invalidChar.IsMatch(arg)) { throw new ArgumentOutOfRangeException("arg"); }
			if (string.IsNullOrEmpty(arg)) return "\"\"";
			else if (!needsQuotes.IsMatch(arg))  return arg; 
			return '"' + escapeQuote.Replace(arg, m => m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : "")				) + '"';
		}

		public static List<string> AllMainBossServices([Thinkage.Libraries.Translation.Invariant]string machineName) {
			try {
				return System.ServiceProcess.ServiceController.GetServices(machineName).Where(serviceController => serviceController.DisplayName.StartsWith(KB.I("MainBoss"))).Select(serviceController => serviceController.ServiceName).ToList();
			}
			catch (System.Exception ex) {
				throw new GeneralException(ex, KB.K("Unable to find any Windows Services for MainBoss on computer '{0}'. The computer may not exist or there may not be permissions to access the service on the computer"), machineName);
			}
		}
		static public ServiceConfiguration AcquireServiceConfiguration(bool isAdmin, bool requireAdmin, string serviceComputer, string serviceName) {
			if ( serviceName == null ) {
				try {
					var allServices = ServiceConfiguration.AllMainBossServices(serviceComputer);
					switch (allServices.Count ) {
						case 0:
							throw new GeneralException(KB.K("Database connection information was not supplied"));
						default:
							throw new GeneralException(KB.K("Multiple Windows Services for MainBoss exist; Choose one with the '{0}' option"), KB.I("/ServiceName:"));
						case 1:
							serviceName = allServices.First();
							break;
					}
				}
				catch (System.Exception ex) {
					Libraries.Exception ge = new GeneralException(ex, KB.K("Checking the Windows Service for MainBoss on '{0}' was not possible"), serviceComputer);
					if (!isAdmin)
						ge.WithContext(new MessageExceptionContext(KB.K("Running this program as an Administrator may enable the configuration checks")));
					throw ge;
				}
			}
			bool serviceExists = false;
			var staticConfig = new StaticServiceConfiguration(serviceComputer, serviceName);
			try {
				serviceExists = ServiceController.ServiceExists(staticConfig);
			}
			catch (GeneralException ex) {
				Libraries.Exception ge =  new GeneralException(ex,KB.K("Checking the Windows Service for MainBoss '{0}' on '{1}' was not possible"), serviceName, serviceComputer);
				if (!isAdmin)
					ge = ge.WithContext(new MessageExceptionContext(KB.K("Running this program as an Administrator may enable the configuration checks")));
				throw ge;
			};
			return serviceExists ? new ServiceConfiguration(staticConfig, requireAdmin) : null;
		}
		#region IDisposable Members

		override protected void Dispose(bool disposing) {
			if (disposing && disposeController && Controller != null)
				Controller.Dispose();
			disposeController = false;
			Controller = null;
			base.Dispose(disposing);
		}
		#endregion
	}
	#region ServiceStartupOptions Optable Support
	/// <summary>
	/// Optable for passing ServiceCommandOptable as argument to a Service being started so it knows its own ServiceCommandOptable
	/// </summary>
	public class ServiceStartupOptions : Optable {
		public const string ServiceNameCommandArgument = "ServiceName";

		private StringValueOption ServiceNameO;
		private StringValueOption ConnectionO;
		private StringValueOption VersionO;
		private StringValueOption DatabaseServerO;
		private StringValueOption DatabaseNameO;

		public string Executable     { get; private set; }
		public string Version        { get; private set; }
		public SQLConnectionInfo Connection  { get; private set; }

		public ServiceStartupOptions(string commandLine) {
			StringValueOption SecurityTokenO;
			string Password = null;
			var args = SplitArguments(commandLine);
			Executable = args.Length > 0 ? args[0] : "";
			Add(ServiceNameO   = new StringValueOption(KB.I("ServiceName"), KB.K("The service name identifier.").Translate(), false));
			Add(SecurityTokenO = new StringValueOption(KB.I("SecurityToken"), KB.K("Security Identification to the SQL Server. For internal use only.").Translate(), false));
			Add(ConnectionO    = new StringValueOption(KB.I("Connection"), KB.K("SQL Server Connection string.").Translate(), false));
			Add(VersionO       = new StringValueOption(KB.I("Version"), KB.K("The version of MainBoss used to install the Windows Service for MainBoss.").Translate(), true));
			Add(DatabaseNameO  = new StringValueOption(KB.I("DataBaseName"), KB.K("The DataBaseName option is obsolete.").Translate(), false));					//for historical compatablity
			Add(DatabaseServerO= new StringValueOption(KB.I("DataBaseServer"), KB.K("The DataBaseServer option is obsolete.").Translate(), false));				//for historical compatablity
			if (args.Length    == 0)
				return;
			try {
				Parse(args.Skip(1).ToArray());
				CheckRequired();
			}
			catch ( System.Exception ex) {
				throw new GeneralException(ex, KB.K("Windows Service for MainBoss does not have the expected configuration. It may be an old version of the Service. The service needs to be reconfigured"));
			}
			try {
				if (!string.IsNullOrWhiteSpace(SecurityTokenO.Value))
					Password = Database.Service.ServicePassword.Decode(Convert.FromBase64String(SecurityTokenO.Value));
			}
			catch {
				Password = null;  // checking the command line will catch the bad password, we allow it through so that /update will work
			}
			try {
				if (VersionO.HasValue)
					Version = VersionO.Value;
				if (ConnectionO.HasValue)
					Connection = new SQLConnectionInfo(ConnectionO.Value);
				else if (DatabaseNameO.HasValue && DatabaseServerO.HasValue)
					Connection = new SQLConnectionInfo(DatabaseServerO.Value, DatabaseNameO.Value);
				if (Password != null)
					Connection.SqlConnectionObject.Password = Password;
			}
			catch( System.Exception ex ) {
				throw new GeneralException(ex, KB.K("Invalid SQL Server connection string '{0}'"), ConnectionO.Value);
			}
			if( Connection == null)
				throw new GeneralException(KB.K("Windows Service for MainBoss does not have the expected configuration."));
		}
		//
		// a simple parsing of the command line stored in the service
		// there will be no redirection or complex args other then quoting
		// escaping is funny, \" and \' are converted to " and ' but no other use of \ is changed
		// in theory ^ could be used for an escape but it is not processed by the service invoker.
		// \ cannot be used as an escape because the processing of the service to find the command line
		// the rest of the command line does not use funny characters except for the password, but those are restricted to the 
		// base64 string and will not cause problems.
		//
		public static string[] SplitArguments(string commandLine) {
			var parmChars = commandLine.ToCharArray();
			var inSingleQuote = false;
			var inDoubleQuote = false;
			for (var index = 0; index < parmChars.Length; index++) {
				if (parmChars[index] == '"' && !inSingleQuote) {
					inDoubleQuote = !inDoubleQuote;
					parmChars[index] = '\0';
				}
				if (parmChars[index] == '\'' && !inDoubleQuote) {
					inSingleQuote = !inSingleQuote;
					parmChars[index] = '\0';
				}
				if (parmChars[index] == '\\' && index + 1 < parmChars.Length && (parmChars[index + 1] == '"' || parmChars[index+1] == '\'' )  )
					parmChars[index++] = '\0';

				if (!inSingleQuote && !inDoubleQuote && (parmChars[index] == ' ' || parmChars[index] == '	'))
					parmChars[index] = '\n';
			}
			return (new string(parmChars)).Replace("\0", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
	#endregion
}
