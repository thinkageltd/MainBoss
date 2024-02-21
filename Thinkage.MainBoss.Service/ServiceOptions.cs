using System;
using System.Linq;
using System.Security.Principal;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database.Service;
using System.Data.SqlClient;

namespace Thinkage.MainBoss.Service {
	public class ServiceOptions {
		/// <summary>
		/// Extract the service name from the command line argument (typically passed as the Service is started by the system from our altered ImagePath we configured)
		/// </summary>
		/// <param name="args"></param>
		public ServiceOptions(string[] args) {

			ServiceCommandOptable startupOptions = new ServiceCommandOptable();
			string authenicationName = null;
			string sqlPass = null;
			string sqlToken = null;
			try {
				startupOptions.Parse(args);
				startupOptions.CheckRequired();
				ServiceName = startupOptions.ServiceName.HasValue ? startupOptions.ServiceName.Value : null;
				MBVersion = startupOptions.MBVersion.HasValue ? startupOptions.MBVersion.Value : null;
				ServiceComputer = startupOptions.ServiceComputer.HasValue ? startupOptions.ServiceComputer.Value : null;
				DatabaseName = startupOptions.DatabaseName.HasValue ? startupOptions.DatabaseName.Value : null;
				DatabaseServer = startupOptions.DatabaseServer.HasValue ? startupOptions.DatabaseServer.Value : null;
				Connection = startupOptions.Connection.HasValue ? startupOptions.Connection.Value : null;
				ManuallyRun = startupOptions.ManuallyRun.HasValue ? startupOptions.ManuallyRun.Value : false;
				TestConfiguration = startupOptions.TestConfiguration.HasValue ? startupOptions.TestConfiguration.Value : false;
				Force = startupOptions.Force.HasValue ? startupOptions.Force.Value : false;
				CreateService = startupOptions.CreateService.HasValue ? startupOptions.CreateService.Value : false;
				DeleteService = startupOptions.DeleteService.HasValue ? startupOptions.DeleteService.Value : false;
				GrantAccess = startupOptions.GrantAccess.HasValue ? startupOptions.GrantAccess.Value : false;
				ServiceUserid = startupOptions.ServiceUserid.HasValue ? startupOptions.ServiceUserid.Value : null;
				ServicePassword = startupOptions.ServicePassword.HasValue ? startupOptions.ServicePassword.Value : null;
				sqlPass = startupOptions.SQLPassword.HasValue ? startupOptions.SQLPassword.Value : null;
				sqlToken = startupOptions.SecurityToken.HasValue ? startupOptions.SecurityToken.Value : null;
				SQLUserid = startupOptions.SQLUserid.HasValue ? startupOptions.SQLUserid.Value : null;
				SQLPassword = startupOptions.SQLPassword.HasValue ? startupOptions.SQLPassword.Value : null;
				ListMainBossServices = startupOptions.ListMainBossServices.HasValue ? startupOptions.ListMainBossServices.Value : false;
				UpdateService = startupOptions.UpdateService.HasValue ? startupOptions.UpdateService.Value : false;
				authenicationName = startupOptions.Authentication.HasValue ? startupOptions.Authentication.Value : null;
			}
			catch (Thinkage.Libraries.CommandLineParsing.Exception ex)	{
				var m = Libraries.Translation.MessageBuilder.Build(ex.Message, startupOptions.Help);
				throw new System.Exception(Libraries.Translation.MessageBuilder.Build(ex.Message, startupOptions.Help), ex.InnerException);
			}
			catch (System.Exception se)	{
				Thinkage.Libraries.Exception.AddContext(se, ServiceStartupContext());
				throw;
			}
			var isAService = UserInterface.IsRunningAsAService && !ManuallyRun;
			//
			// option interactions.
			// 
			var numargs = Environment.GetCommandLineArgs().Length;
			if (numargs <= 1)
				DetailedHelp = true;
			if (ListMainBossServices && numargs > 2 )
				throw new GeneralException(KB.K("{0} cannot be used with any other arguments"), KB.I("/ListMainBossServices"));
			if (isAService && Connection == null)
				throw new GeneralException(KB.K("{0} option is required when running as a service"), KB.I("/Connection"));
			if (isAService && (DetailedHelp || ListMainBossServices || CreateService || DeleteService || ServiceUserid != null || ServicePassword != null || UpdateService)  )
				throw new GeneralException(KB.K("{0} options can only be used in user interactive mode"), KB.I("/ListMainBossServices /CreateService /DELeteService /UpdateService /ServiceUserName /ServicePassword"));
#if !DEBUG
			if (Environment.UserInteractive && MBVersion != null   )
				throw new GeneralException(KB.K("{0} option can not be used in user interactive mode"), KB.I("/Version"));
#endif
			if (Force && !(CreateService || DeleteService ))
				throw new GeneralException(KB.K("{0} can only be used with {1}"), KB.I("/Force"), KB.I("/CreateService | /DELeteService "));
			var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
			if (!isAdmin && (DeleteService || CreateService || UpdateService))
				throw new GeneralException(KB.K("{0} options all require the program to be run as an Administrator"), KB.I("/DELeteService /CreateService /UpdateService"));
			if (ServiceName != null && !ServiceName.All(e => Char.IsLetterOrDigit(e)))
				throw new GeneralException(KB.K("Service Name must be alphanumeric with no spaces"));
			if (ServiceUserid != null && !(CreateService || UpdateService))
				throw new GeneralException(KB.K("{0} can only be used with {1}"), KB.I("/ServiceUsername"), KB.I("/CreateService | /UpdateService"));
			if (ServicePassword != null && ServiceUserid == null)
				throw new GeneralException(KB.K("{0} can only be used with {1}"), KB.I("/ServicePassword"), KB.I("/ServiceUsername"));
			if (DeleteService && (CreateService || UpdateService))
				throw new GeneralException(KB.K("{0} must be used with no other options"), KB.I("/DELeteService"));
			if (isAService && ServiceComputer != null )
				throw new GeneralException(KB.K("{0} option can only be used in user interactive mode"), KB.I("/ServiceComputer"));
			if (ServiceComputer == null)
				ServiceComputer = DomainAndIP.MyDnsName;
			if (isAService && sqlPass != null)
				throw new GeneralException(KB.K("{0} option can not be used when running as a service"), KB.I("/SQLPassword"));
			if ( !string.IsNullOrWhiteSpace(sqlPass) )
				SQLPassword = sqlPass;
			if ( !string.IsNullOrWhiteSpace(authenicationName) )
				AuthenticationMethod = (new SQLAuthenticationFromName(authenicationName)).Method;
			if (AuthenticationMethod == null && SQLPassword != null && SQLUserid != null)
				AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
			if ((AuthenticationMethod == null || AuthenticationMethod == SqlAuthenticationMethod.NotSpecified) && (SQLPassword != null || SQLUserid != null) )
				throw new GeneralException(KB.K("{0} and {1} should not be supplied without specifying a {2}"), KB.I("/SQLUsernane"), KB.I("/SQLPassword"), KB.I("/Authentication"));
			if ((AuthenticationMethod == SqlAuthenticationMethod.ActiveDirectoryPassword || AuthenticationMethod == SqlAuthenticationMethod.ActiveDirectoryPassword) && SQLPassword == null)
				throw new GeneralException(KB.K("A {0} is needed with {1} or {2}"), KB.I("/SQLPassword"), KB.I("/Authentication:SQLAuthentication"), KB.I("/Authentication:ActiveDirectoryPassword"));
			else if ((AuthenticationMethod ?? SqlAuthenticationMethod.NotSpecified) != SqlAuthenticationMethod.NotSpecified && SQLUserid == null )
				throw new GeneralException(KB.K("A {0} is needed with {1} or {2} or {3}"), KB.I("/SQLUsername"), KB.I("/Authentication:SQLAuthentication"), KB.I("/Authentication:ActiveDirectoryPassword"),KB.I("/Authentication:ActiveDirectoryIntegrated"));
			else if ((AuthenticationMethod != null && AuthenticationMethod != SqlAuthenticationMethod.NotSpecified) && (SQLPassword == null || SQLUserid == null))
				throw new GeneralException(KB.K("{0} and {1} are required with all authentication methods other than {0}=\"{1}\""), KB.I("/SQLUsername"), KB.I("/SQLPassword"), KB.I("/Authentication=WindowsAuthentication"));
			if (!DomainAndIP.IsThisComputer(ServiceComputer) && (ManuallyRun || TestConfiguration))
				throw new GeneralException(KB.K("{0} can not be used on a remote computer"), KB.I("/TestConfiguration"));
			if(sqlToken != null && SQLPassword  != null)
				throw new GeneralException(KB.K("{0} can not be used with {1}"), KB.I("/SQLPassword"),KB.I("/SecurityToken"));
			if (!string.IsNullOrWhiteSpace(sqlToken)) {
				try {
					SQLPassword = Database.Service.ServicePassword.Decode(Convert.FromBase64String(sqlToken));
				}
				catch {
					throw new GeneralException(KB.K("'{0}' is not a valid value for '{1}'"), sqlToken, KB.I("/SecurityToken"));
				}
			}
			if ((ManuallyRun || TestConfiguration) && (CreateService || DeleteService || UpdateService ))
				throw new GeneralException(KB.K("{0} can not be used with {1}"), KB.I("/TestConfiguration"), KB.I("/CreateService | /DELeteService | /UpdateService"));
			if ( DetailedHelp ) {
				System.Console.WriteLine(KB.K("MainBoss Service expects to be run as a Windows Service.").Translate());
				ConsoleIndent();
				System.Console.WriteLine(KB.K("However, this program can also be run from a CMD console for diagnostic purposes.").Translate());

				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("Usage: \"{0}\""), System.Environment.GetCommandLineArgs()[0]));
				ConsoleIndent();
				System.Console.WriteLine(KB.I("[/TestConfiguration] [/ServiceName:MainBossService]"));
				ConsoleIndent();
				System.Console.WriteLine(KB.I("[/DatabaseName:database] [/DataBaseServer:computer] [/Connection:SqlConnectionString]"));
				ConsoleIndent();
				System.Console.WriteLine(KB.I("[/SetServiceParameters] [/CreateService] [/DELeteService]"));
				ConsoleIndent();
				System.Console.WriteLine(KB.I("[/ServiceUsername:username] [/ServicePassword:password] [/Force]"));
				ConsoleIndent();
				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("The command must be able to find the location of the MainBoss Database")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("If the command is run as an Administrator, and there is exactly one MainBoss Service,")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("the information from that service will be used to find the MainBoss Database.")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("If there is more that one Windows Service for MainBoss on the computer then '{0}' can be use to pick the desired service,"), KB.I("/ServiceName:name")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("In other cases '{0}' or '{1}' '{2}' can be used to select the correct database"), KB.I("/Connection:SqlConnectionString"), KB.I("/Database:server"), KB.I("DatabaseName:name")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("The default is to use Windows Authentication, but other forms of Authentication can be used,")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("by supplying '{0}' with '{1}' and '{2}'"), KB.I("/Authenication:AuthenicationType"), KB.I("/SqlUsername:user"), KB.I("/SqlPassword:password")));
				System.Console.WriteLine();
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option installs the Windows Service for MainBoss."), KB.I("/CreateService")));
				ConsoleIndent();
				System.Console.WriteLine(KB.K("This option requires the command to be run as an Administrator.").Translate());
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' and '{1}' options can be used to set the user and password for the Windows Service for MainBoss"), KB.I("/ServiceUsername:username"), KB.I("/ServicePassword:password")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K(", but normally should not be used as the service defaults to 'Network Service'")));
				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option updates and refreshes the Windows Service for MainBoss as necessary."), KB.I("/Update")));
				ConsoleIndent();
				System.Console.WriteLine(KB.K("This option requires the command to be run as an Administrator.").Translate());
				ConsoleIndent();
				System.Console.WriteLine(KB.K("This option will be needed after any updated version of MainBoss is installed.").Translate());

				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option carries out the standard MainBoss Service processing of incoming and outgoing email."),
											KB.I("/ManuallyRun")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option gives more detailed diagnostic information."),
											KB.I("/TestConfiguration")));
				ConsoleIndent();
				System.Console.WriteLine(Strings.Format(KB.K("If the Windows Service for MainBoss is running, then running this program using the '{0}' or '{1}' options may result in duplicate notifications or duplicate requests made from incoming email."),
											KB.I("/ManuallyRun"), KB.I("/TestConfiguration")));

				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option deletes the Windows Service for MainBoss from this computer and from the MainBoss database."), KB.I("/DELeteService")));
				ConsoleIndent();
				System.Console.WriteLine(KB.K("This option requires the command to be run as an Administrator.").Translate());

				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option will give the user that the service runs under permissions on the database."),
											KB.I("/GrantAccess")));
				ConsoleIndent();
				System.Console.WriteLine(KB.K("This option requires SQL Server Administrator privileges").Translate());
				System.Console.WriteLine();

				System.Console.WriteLine();
				System.Console.WriteLine(Strings.Format(KB.K("The '{0}' option may be used with {1} {2} or {3} options."), KB.I("/Force"), KB.I("/CreateService"), KB.I("/UpdateService"), KB.I("/DELeteService")));
				ConsoleIndent();
				System.Console.WriteLine(KB.K("It should not normally be used. The option will allow overriding of various error messages.").Translate());
				System.Console.WriteLine();
			}
		}
		private void ConsoleIndent() {
			System.Console.Write(KB.I("    "));
		}
		public MessageExceptionContext ServiceStartupContext()	{
			return new MessageExceptionContext(KB.K("Service {0} running as user {1}"), ServiceName, Thinkage.Libraries.Application.Instance.UserName);
		}
		/// <summary>
		/// Where all service definition parameters are defined.
		/// </summary>
		public string ServiceName { get; set; }
		public string MBVersion	  { get; set; }
		public string ServiceComputer { get; set; }
        public string DatabaseName { get; set; }
		public string DatabaseServer { get; set; }
		public string ServiceUserid { get; set; }
		public string ServicePassword { get; set; }
		public string SQLUserid { get; set; }
		public string SQLPassword { get; set; }
		public SqlAuthenticationMethod? AuthenticationMethod { get; set; }
		public string Connection { get; set; }
		public bool ManuallyRun { get; set; }
		public bool TestConfiguration { get; set; }
		public bool CreateService { get; set; }
		public bool DeleteService { get; set; }
		public bool GrantAccess { get; set; }
		public bool Force{ get; set; }
		public bool ListMainBossServices { get; set; }
		public bool UpdateService { get; set; }
		public bool DetailedHelp { get; set; }
		//
		// the SQL password encoded a a base 64 string
		// 
	}

	#region ServiceCommandOptable Optable Support
	/// <summary>
	/// Optable for passing ServiceCommandOptable as argument to a Service being started so it knows its own ServiceCommandOptable
	/// </summary>
	public class ServiceCommandOptable : Optable {
		public const string ServiceNameCommandArgument = "ServiceName";

		public StringValueOption ServiceName;
		public StringValueOption MBVersion;
		public StringValueOption ServiceComputer;
		public StringValueOption DatabaseName;
		public StringValueOption DatabaseServer;
		public StringValueOption ServiceUserid;
		public StringValueOption ServicePassword;
		public StringValueOption SQLUserid;
		public StringValueOption SQLPassword;
		public StringValueOption SecurityToken;
		public StringValueOption Authentication;
		public StringValueOption Connection;
		public BooleanOption ManuallyRun;
		public BooleanOption TestConfiguration;
		public BooleanOption CreateService;
		public BooleanOption DeleteService;
		public BooleanOption GrantAccess;
		public BooleanOption Force;
		public BooleanOption ListMainBossServices;
		public BooleanOption UpdateService;
		public BooleanOption DetailedHelp;
		
		public ServiceCommandOptable() {
			Add(ServiceName			= new StringValueOption(KB.I(ServiceNameCommandArgument), KB.K("The service name identifier.").Translate(), false));
			Add(MBVersion			= new StringValueOption(KB.I("Version"), KB.K("The version of MainBoss used to install the Windows Service for MainBoss.").Translate(), false));
			Add(ServiceComputer		= new StringValueOption(KB.I("ServiceComputer"), KB.K("The computer where the Windows Service for MainBoss is installed.").Translate(), false));
			Add(DatabaseName		= new StringValueOption(KB.I("DataBaseName"), KB.K("Name of the Database.").Translate(), false));
			Add(DatabaseServer		= new StringValueOption(KB.I("DataBaseServer"), KB.K("Name of the SQL Server that the database is on.").Translate(), false));
			Add(ServiceUserid		= new StringValueOption(KB.I("ServiceUsername"), KB.K("User name for the service to run as; normally not supplied, defaults to 'Network Service'.").Translate(), false));
			Add(ServicePassword		= new StringValueOption(KB.I("ServicePassword"), KB.K("Password for the service User name; normally not supplied, not needed for 'Network Service'.").Translate(), false));
			Add(SQLUserid			= new StringValueOption(KB.I("SQLUsername"), KB.K("User name used to access the SQL Server; not used with 'Windows Authentication'.").Translate(), false));
			Add(SQLPassword		    = new StringValueOption(KB.I("SQLPassword"), KB.K("Password for the SQL User Name; normally not used with 'Windows Authentication'.").Translate(), false));
			Add(SecurityToken		= new StringValueOption(KB.I("SecurityToken"), KB.K("Security Identification to the SQL Server. For internal use only.").Translate(), false));
			Add(Authentication		= new StringValueOption(KB.I("Authentication"), KB.K("SQL Server authentication; not needed needed, default to 'Windows Authentication'.").Translate(), false));
			Add(Connection			= new StringValueOption(KB.I("Connection"), KB.K("SQL Server Connection string.").Translate(), false));
			Add(ManuallyRun			= new BooleanOption(KB.I("ManuallyRun"), KB.K("Manually run the MainBoss Service. May cause duplicate messages if Windows Service for MainBoss is running").Translate(), '+', false));
			Add(TestConfiguration	= new BooleanOption(KB.I("TestConfiguration"), KB.K("Test the MainBoss Service configuration. May cause duplicate messages if Windows Service for MainBoss is running").Translate(), '+', false));
			Add(DetailedHelp		= new BooleanOption(KB.I("DetailedHelp"), KB.K("Detailed Help on the options").Translate(), '+', false));
			Add(CreateService		= new BooleanOption(KB.I("CreateService"), KB.K("Administrative option to create a Windows Service for MainBoss on this computer").Translate(), '+', false));
			Add(DeleteService		= new BooleanOption(KB.I("DELeteService"), KB.K("Administrative option to delete a Windows Service for MainBoss on this computer").Translate(), '+', false));
			Add(GrantAccess			= new BooleanOption(KB.I("GrantAccess"), KB.K("SQL Server Administrative option to grant access to the MainBoss database by the Service User Name").Translate(), '+', false));
			Add(Force				= new BooleanOption(KB.I("Force"), KB.K("Force an action to take place; should not normally be used. Will be required when a computer is no longer available.").Translate(), '+', false));
			Add(UpdateService		= new BooleanOption(KB.I("UpdateService"), KB.K("Update and refresh the Windows Service for MainBoss; needed after every new release of MainBoss is installed").Translate(), '+', false));
			Add(ListMainBossServices= new BooleanOption(KB.I("ListMainBossServices"), KB.K("List all the Windows Services for MainBoss installed on this computer").Translate(), '+', false));
		}
	}
	#endregion
}
