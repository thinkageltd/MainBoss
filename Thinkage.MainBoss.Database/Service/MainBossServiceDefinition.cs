using System.Linq;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
using System.Net;
using System.Net.Sockets;
using System;

namespace Thinkage.MainBoss.Database.Service {
	public class MainBossServiceDefinition : Thinkage.Libraries.Service.SettableNameServiceDescriptor {
		public const int DemoExpiresMinutes = 60; // in demo mode, the service will stop after this time.
		public const string ServiceDisplayName = "MainBoss Service";
		public const string ServiceAssemblyName = "Thinkage.MainBoss.Service.exe";
		// This is used to communicate with an installed service
		public MainBossServiceDefinition([Invariant]string serviceName, [Invariant] string serverComputerName, [Invariant]string executable)
			: base(ServiceDisplayName, serviceName, serverComputerName, executable) {
		}
		public MainBossServiceDefinition(string serviceName, string executable)
			: base(Thinkage.Libraries.Translation.KB.I(ServiceDisplayName), serviceName, executable) {
		}
		// This is used to simply define the parameters associated with the Service; the ServiceName will be set during startup
		public MainBossServiceDefinition()
			: this(ServiceDisplayName, null, ServiceAssemblyName) {
		}
		public override string DisplayName {
			[Invariant]
			get {
				return Thinkage.Libraries.Translation.KB.I(ServiceDisplayName);
			}
		}
		public override string Description {
			get {
				return Thinkage.MainBoss.Database.KB.K("Base service for all licensed MainBoss Service workers.").Translate();
			}
		}
	}
	/// <summary>
	/// Service requests that are performed by this service
	/// </summary>
	public enum ApplicationServiceRequests {
		// All workers honor the following
		RESET_LOGGING = Thinkage.Libraries.Service.Application.ServiceRequests.RESET_LOGGING_PARAMETERS,
		TERMINATE_ALL = Thinkage.Libraries.Service.Application.ServiceRequests.USER_STOP_SERVICE,
		PAUSE_SERVICE = Thinkage.Libraries.Service.Application.ServiceRequests.USER_PAUSE_SERVICE,
		RESUME_SERVICE = Thinkage.Libraries.Service.Application.ServiceRequests.USER_RESUME_SERVICE,
		PROCESS_ALL = Thinkage.Libraries.Service.Application.ServiceRequests.NEXT_AVAILABLE,
		// Service request specific to Request notification
		PROCESS_REQUESTS_INCOMING_EMAIL,
		PROCESS_REQUESTOR_NOTIFICATIONS,
		/// <summary>
		/// Send acknowledgments of change in status of Requests/WorkOrders/PurchaseOrders to all Assignees of those items
		/// </summary>
		PROCESS_ASSIGNMENT_NOTIFICATIONS,
		/// <summary>
		/// Empty the log file in the database
		/// </summary>
		LOG_EMPTY,
		/// <summary>
		/// delete all over aged log file records
		/// </summary>
		LOG_AGE,
		/// <summary>
		/// remove trace entry from database log file
		/// </summary>
		TRACE_CLEAR,
		/// <summary>
		/// turn on traceing showing each time the service look for items to process
		/// </summary>
		TRACE_ACTIVITIES,
		/// <summary>
		/// turn on tracing on request handing
		/// </summary>
		TRACE_EMAIL_REQUESTS,
		/// <summary>
		/// turn on tracing on assignee notifications
		/// </summary>
		TRACE_NOTIFY_ASSIGNEE,
		/// <summary>
		/// turn on tracing on request handing
		/// </summary>
		TRACE_NOTIFY_REQUESTOR,
		/// <summary>
		/// turn on all tracing information in the logging
		/// </summary>
		TRACE_ALL,
		/// <summary>
		/// turn off the tracing information in the logging
		/// </summary>
		TRACE_OFF,
		/// <summary>
		/// Stop all service, read configuration, restart services.
		/// </summary>
		REREAD_CONFIG,
	}


	/// <summary>
	/// A class providing properties about the current MainBoss Service configuration. All references to MainBossService configuration should be done through
	/// an instance of this class.
	/// </summary>
	public class MainBossServiceConfiguration : Thinkage.Libraries.Service.IServiceConfiguration {
		/// <summary>
		/// A predefined prefix on all MainBoss services so we can find them in the service configuration.
		/// </summary>
		public static readonly string MainBossServiceTag = KB.I("MainBossService");
		private DBClient DB;
		static Dictionary<string,MainBossServiceConfiguration > configs = new Dictionary<string,MainBossServiceConfiguration>();
		public MainBossServiceConfiguration(DBClient db, [Invariant] string serviceCode = null, [Invariant] string machineName = null) {
			DB = db;
			if (serviceCode != null)
				ServiceName = serviceCode;
			ServiceMachineName = machineName;
			try {
				var sRow = getServiceConfigurationRow(DB);
				SetValues(sRow);
			}
			catch (GeneralException e) {
				Application.Instance.DisplayError(e);
			}
		}
		static public MainBossServiceConfiguration GetConfiguration(DBClient db, [Invariant] string serviceCode = null) {
			if (db == null)
				throw new GeneralException(KB.K("Cannot get service configuration because no database connection is available"));
			lock (configs) {
				try {
					MainBossServiceConfiguration c;
					if (configs.ContainsKey(db.ConnectionInfo.DisplayNameLowercase)) {
						c = configs[db.ConnectionInfo.DisplayNameLowercase];
						if (c.TestIfEqual((MB3Client)db)) return c;
					}
					c = new MainBossServiceConfiguration(db, serviceCode);
					configs[db.ConnectionInfo.DisplayNameLowercase] = c;
					return c;
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Cannot get service configuration from database"));
				}
			}
		}

		// this variant is need to avoid cross thread access of a database;
		static public MainBossServiceConfiguration GetConfiguration(MB3Client.ConnectionDefinition dbConnection, [Invariant] string serviceCode = null) {
			if (dbConnection == null)
				throw new GeneralException(KB.K("Cannot get service configuration because no database connection is available"));
			try {
				var db = new MB3Client(dbConnection);
				var c = GetConfiguration(db, serviceCode);
				db.CloseDatabase();
				return c;
			}
			catch(System.Exception e) {
				if (e is GeneralException)
					throw;
				throw new GeneralException(KB.K("Could not access database {0}"), dbConnection.ToString());
			}
		}
		static public bool CheckForChanges() {
			if (configs == null || !configs.Any() )
				return true;
			return configs.Any(e => !e.Value.TestIfEqual());
		}
		private bool TestIfEqual() {
			bool r = true;
			MB3Client db = new MB3Client(DB.ConnectionInfo);  // have to create a new connection 
			r = TestIfEqual(db);
			db.CloseDatabase();
			return r;
		}
		private bool TestIfEqual(MB3Client db) {
			var sRow = getServiceConfigurationRow(db);
			return TestIfEqual(sRow);
		}
		static public void Reset() {
			configs = new Dictionary<string, MainBossServiceConfiguration>();
		}
		private dsMB.ServiceConfigurationRow getServiceConfigurationRow(DBClient db) {
			dsMB.ServiceConfigurationRow sRow = null;
			using (var ds = new dsMB(db)) {
				ds.EnsureDataTableExists(dsMB.Schema.T.ServiceConfiguration);
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.ServiceConfiguration);
				dsMB.ServiceConfigurationDataTable dt = ds.T.ServiceConfiguration;
				if (dt.Rows.Count == 0)
					sRow = null;
				else if (dt.Rows.Count > 1 )
					throw new GeneralException(KB.K("More than one service configuration is not currently supported, please remove all except for one"));
				else {
					// if more than one and we asked for nothing by name, just take the first one !
					sRow = (dsMB.ServiceConfigurationRow)dt.Rows[0];
					// the machine name in the configuration record could be an alias for the machine name
					// if it is rewrite it with what the machine calls its self
					// if the name has not be specified in the configuration record or was set to local host
					// set to our name. There is a slight possibility a critical sections
					// (ie two services starting up at the same time and the configuration record says local host
					//  but the worse that could happened is duplicate notifications or duplicate requests being created)
					// if the program is not running as a service and the configure specifies LocalHost then the rewrite does not occur.
					if (sRow.F.ServiceMachineName != null  && DomainAndIP.IsThisComputer(sRow.F.ServiceMachineName)) {
							sRow.F.ServiceMachineName = DomainAndIP.MyDnsName;
							ds.DB.Update(ds);
					}
				}
			}
			return sRow;
		}
		private void SetValues(dsMB.ServiceConfigurationRow sRow) {
			 Exists =  sRow != null;
			 if (!Exists) {
				 ServiceName = null;
				 ServiceMachineName = null;
				 SqlUserid = null;
				 Desc = null;
				 Comment = null;
				 MainBossRemoteURL = null;
				 HtmlEmailNotification = false;
				 NotificationInterval = new System.TimeSpan(0);
				 ReturnEmailDisplayName = null;
				 ReturnEmailAddress = null;
				 AutomaticallyCreateRequestors = false;
				 AutomaticallyCreateRequestorsFromLDAP = false;
				 WakeUpInterval = new System.TimeSpan(0);
				 MailServerType = 0;
				 ProcessNotificationEmail = false;
				 ProcessRequestorIncomingEmail = false;
				 MailServer = null;
				 MailPort = 0;
				 Encryption = (sbyte)DatabaseEnums.MailServerEncryption.AnyAvailable;
				 MaxMailSize = null;
				 MailUserName = null;
				 MailboxName = null;
				 SMTPServer = null;
				 SMTPPort = 0;
				 SMTPUseSSL = false;
				 SMTPCredentialType = 0;
				 SMTPUserDomain = null;
				 SMTPUserName = null;
				 SMTPEncryptedPassword = null;
				 MailEncryptedPassword = null;
				 AcceptAutoCreateEmailPattern = null;
				 RejectAutoCreateEmailPattern = null;
			 }
			 else {
				ServiceName = sRow.F.Code;
				ServiceMachineName = sRow.F.ServiceMachineName;
				SqlUserid = sRow.F.SqlUserid;
				Desc = sRow.F.Desc;
				Comment = sRow.F.Comment;
				GUID = sRow.F.Id.ToString();
				InstalledServiceVersion = sRow.F.InstalledServiceVersion;
				MainBossRemoteURL = sRow.F.MainBossRemoteURL;
				HtmlEmailNotification = sRow.F.HtmlEmailNotification;
				NotificationInterval = sRow.F.NotificationInterval;
				ReturnEmailDisplayName = sRow.F.ReturnEmailDisplayName;
				ReturnEmailAddress = sRow.F.ReturnEmailAddress;
				ManualProcessingTimeAllowance = sRow.F.ManualProcessingTimeAllowance;
				AutomaticallyCreateRequestors = sRow.F.AutomaticallyCreateRequestors;
				AutomaticallyCreateRequestorsFromLDAP = sRow.F.AutomaticallyCreateRequestorsFromLDAP;
				AutomaticallyCreateRequestorsFromEmail = sRow.F.AutomaticallyCreateRequestorsFromEmail;
				WakeUpInterval = sRow.F.WakeUpInterval;
				MailServerType = sRow.F.MailServerType;
				ProcessNotificationEmail = sRow.F.ProcessNotificationEmail;
				ProcessRequestorIncomingEmail = sRow.F.ProcessRequestorIncomingEmail;
				MailServer = sRow.F.MailServer;
				MailPort = sRow.F.MailPort;
				Encryption = sRow.F.Encryption;
				MaxMailSize = sRow.F.MaxMailSize;
				MailUserName = sRow.F.MailUserName;
				MailboxName = sRow.F.MailboxName;
				SMTPServer = sRow.F.SMTPServer;
				SMTPPort = sRow.F.SMTPPort;
				SMTPUseSSL = sRow.F.SMTPUseSSL;
				SMTPCredentialType = sRow.F.SMTPCredentialType;
				SMTPUserDomain = sRow.F.SMTPUserDomain;
				SMTPUserName = sRow.F.SMTPUserName;
				SMTPEncryptedPassword = sRow.F.SMTPEncryptedPassword;
				MailEncryptedPassword = sRow.F.MailEncryptedPassword;
				AcceptAutoCreateEmailPattern = sRow.F.AcceptAutoCreateEmailPattern;
				RejectAutoCreateEmailPattern = sRow.F.RejectAutoCreateEmailPattern;
			 }
		}
		private bool TestIfEqual(dsMB.ServiceConfigurationRow sRow) {
			if (sRow == null) return Exists == (sRow != null);
			return new bool[] {
				ServiceName == sRow.F.Code,
				ServiceMachineName == sRow.F.ServiceMachineName,
				SqlUserid == sRow.F.SqlUserid,
				Desc == sRow.F.Desc,
				Comment == sRow.F.Comment,
				GUID == sRow.F.Id.ToString(),
				InstalledServiceVersion == sRow.F.InstalledServiceVersion,
				MainBossRemoteURL == sRow.F.MainBossRemoteURL,
				HtmlEmailNotification == sRow.F.HtmlEmailNotification,
				NotificationInterval == sRow.F.NotificationInterval,
				ReturnEmailDisplayName == sRow.F.ReturnEmailDisplayName,
				ReturnEmailAddress == sRow.F.ReturnEmailAddress,
				ManualProcessingTimeAllowance == sRow.F.ManualProcessingTimeAllowance,
				AutomaticallyCreateRequestors == sRow.F.AutomaticallyCreateRequestors,
				AutomaticallyCreateRequestorsFromLDAP == sRow.F.AutomaticallyCreateRequestorsFromLDAP,
				AutomaticallyCreateRequestorsFromEmail == sRow.F.AutomaticallyCreateRequestorsFromEmail,
				WakeUpInterval == sRow.F.WakeUpInterval,
				MailServerType == sRow.F.MailServerType,
				ProcessNotificationEmail == sRow.F.ProcessNotificationEmail,
				ProcessRequestorIncomingEmail == sRow.F.ProcessRequestorIncomingEmail,
				MailServer == sRow.F.MailServer,
				MailPort == sRow.F.MailPort,
				Encryption == sRow.F.Encryption,
				MaxMailSize == sRow.F.MaxMailSize,
				MailUserName == sRow.F.MailUserName,
				MailboxName == sRow.F.MailboxName,
				SMTPServer == sRow.F.SMTPServer,
				SMTPPort == sRow.F.SMTPPort,
				SMTPUseSSL == sRow.F.SMTPUseSSL,
				SMTPCredentialType == sRow.F.SMTPCredentialType,
				SMTPUserDomain == sRow.F.SMTPUserDomain,
				SMTPUserName == sRow.F.SMTPUserName,
				(SMTPEncryptedPassword??new byte[]{}).SequenceEqual((sRow.F.SMTPEncryptedPassword??new byte[]{})),
				(MailEncryptedPassword??new byte[]{}).SequenceEqual((sRow.F.MailEncryptedPassword??new byte[]{})),
				AcceptAutoCreateEmailPattern == sRow.F.AcceptAutoCreateEmailPattern,
				RejectAutoCreateEmailPattern == sRow.F.RejectAutoCreateEmailPattern
			}.All(e => e);
		}
		#region IServiceConfiguration
		public string ServiceName { get; private set; }
		public string ServiceMachineName { get; private set; }
		#endregion
		#region Properties
		public bool Exists { get; private set; }
		public string SqlUserid  { get; private set; }
		public string Desc { get; private set; }
		public string InstalledServiceVersion { get; private set; }
		public string GUID { get; set; }
		public string Comment { get; private set; }
		public string MainBossRemoteURL  { get; private set; }
		public bool HtmlEmailNotification  { get; private set; }
		public System.TimeSpan NotificationInterval  { get; private set; }
		public string ReturnEmailDisplayName  { get; private set; }
		public string ReturnEmailAddress  { get; private set; }
		public TimeSpan? ManualProcessingTimeAllowance { get; private set; }
		public bool AutomaticallyCreateRequestors { get; private set; }
		public bool AutomaticallyCreateRequestorsFromEmail { get; private set; }
		public bool AutomaticallyCreateRequestorsFromLDAP { get; private set; }
		public System.TimeSpan WakeUpInterval  { get; private set; }
		public sbyte MailServerType  { get; private set; }
		public bool ProcessNotificationEmail  { get; private set; }
		public bool ProcessRequestorIncomingEmail  { get; private set; }
		public string MailServer  { get; private set; }
		public int? MailPort  { get; private set; }
		public sbyte Encryption { get; private set; }
		public int? MaxMailSize { get; private set; }
		public string MailUserName  { get; private set; }
		public string MailboxName  { get; private set; }
		public byte[] MailEncryptedPassword  { get; private set; }
		public string SMTPServer  { get; private set; }
		public int SMTPPort { get; private set; }
		public bool SMTPUseSSL { get; private set; }
		public sbyte SMTPCredentialType { get; private set; }
		public string SMTPUserDomain { get; private set; }
		public string SMTPUserName { get; private set; }
		public byte[] SMTPEncryptedPassword  { get; private set; }
		public string AcceptAutoCreateEmailPattern { get; private set; }
		public string RejectAutoCreateEmailPattern { get; private set; }
	}
		#endregion
}