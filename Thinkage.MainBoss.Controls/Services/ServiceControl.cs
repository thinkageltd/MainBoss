using System;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.Controls {
	// TODO: The flow-of-control of Refreshing here is really messed up.
	// After commands that might change status, the BrowseLogic calls Refresh which calls the custom control Refresh which calls BrowseControl.Refresh
	// which causes a data-refresh which end us up in our HandleDataRowContentChanged which then handles changes to the status.
	// The problem is that, as it stands, BrowseLogic assumes all the data is somewhere in the queried rows, and it either keeps the data up to date or not,
	// and the code that runs the BrowseLogic (in this case the BrowseControl) knows how to handle refresh requests.
	// There is no notification of status change from the ServiceController, so we can only notice changes based on the user hitting the Refresh button.
	// It is not clear if we can do Refresh instead as a local (BrowseLogic) command, in which case the derived BL could override with an end-call method
	// that refreshes the non-data stuff before the base-class forces the data out of date.
	//
	// TODO: This actually wants to be a no-list browser, with a browsette on the event log. Once we have some way of intercepting browser data fetches we could
	// code the event log as a fake data table (view) with columns identifying the computer and event log name; unless the view had filters on both these it would
	// return no rows. There would be other columns for all the information available in the event log. The a regular tbl could determine what shows up in the columns
	// and what shows in the panel. The main tbl would use BrowsetteFilterBind to set the computer and event log names.
	#region ServiceCommonBrowseLogic
	public abstract class ServiceCommonBrowseLogic : BrowseLogic {
		public const string ServiceConfigurationCommand = "Thinkage.MainBoss.MainBossServiceConfiguration.exe"; // command that runs elevated to control windows service configuration

		// This is the basic BrowseLogic for Service management. The service is defined by the Tbl which must specify MB3BTbl.WithServiceLogicArg.
		// We provide service status monitoring.
		#region Constructor
		public ServiceCommonBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
			MainBossServiceConfiguration config = MainBossServiceConfiguration.GetConfiguration(db);
			ServiceCode = config.ServiceName;
			ServiceNameSource = new StoredValue(dsMB.Schema.T.ServiceConfiguration.F.Code.EffectiveType, config.ServiceName);
			ServerMachineNameSource = new StoredValue(dsMB.Schema.T.ServiceConfiguration.F.ServiceMachineName.EffectiveType, config.ServiceMachineName);
			Controller = new ServiceController();
			Controller.ExecuteIfChanged += delegate () { RefreshCommand.Execute(); };
			ObjectsToDispose.Add(Controller);
		}
		#endregion
		protected override void HandleDataRowContentChanged() {
			// Reset the Controlled service in case a channel is using e.g. the status to bind it to a control.
			// This we be less iffy on order-of-evaluation if the browser just used Notifying Sources to trigger its panel filling. Doesn't really help the list column
			// filling though, which expects all the sources to synchronously get the correct values.
			Controller.SetControllerInfo(new Thinkage.Libraries.Service.StaticServiceConfiguration(ServerMachineName, ServiceName));
			if (refreshlog == null && ServerMachineName != null) {
				IdleCallback idleCallback = Thinkage.Libraries.Application.Instance.GetInterface<IIdleCallback>().CallInCurrentThread(
					delegate () {
						Controller.Refresh(0);
					}
				);
				refreshlog = new System.Threading.Timer((state) => { idleCallback(); });
				refreshlog.Change(0, 60 * 1000); // start the timer now
				ObjectsToDispose.Add(refreshlog);
			}
			else if (refreshlog != null && ServerMachineName == null) {
				refreshlog.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); // disable the timer
				ObjectsToDispose.Remove(refreshlog);
				refreshlog.Dispose();
				refreshlog = null;
			}
			base.HandleDataRowContentChanged();
		}
		public override void SetAllOutOfDate() {
			RefreshStatus();
			base.SetAllOutOfDate();
		}
		#region Properties
		public MainBossServiceDefinition ServiceDefinition {
			get {
				if(pServiceDefinition == null)
					pServiceDefinition = (MainBossServiceDefinition)
						((MB3BTbl.WithServiceLogicArg)BTbl.BrowserLogicClassArg).ServiceDefinitionClass.GetConstructor(MB3BTbl.WithServiceLogicArg.ctorArgTypes).Invoke(new object[0]);
				return pServiceDefinition;
			}
		}
		private MainBossServiceDefinition pServiceDefinition = null;
		private Source ServiceNameSource;
		private Source ServerMachineNameSource;
		public string ServiceName {
			get {
				return (string)ServiceNameSource.GetValue();
			}
		}
		public string ServerMachineName {
			get {
				return (string)ServerMachineNameSource.GetValue();
			}
		}

		public string ServiceCode {
			get; private set;
		}
		public readonly ServiceController Controller;
		#region Service Status
		public IBasicDataControl ServiceStatus;
		public SettableDisablerProperties NoServiceConfigurationRecord = new SettableDisablerProperties(null, KB.K("A Service configuration record must be created."), true);
		public SettableDisablerProperties ServiceInstalledDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss is not installed."), true);
		public SettableDisablerProperties ServiceNotInstalledDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss is already installed."), true);
		public SettableDisablerProperties ServiceConfigurationNeededDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss needs to be configured."), false);
		public SettableDisablerProperties ServiceConfigurationNotNeededDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss does not need to be configured."), false);
		public SettableDisablerProperties ServiceConfigurationNeededWrongComputerDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss configuration must be done on the computer where the service is installed."), true);
		public SettableDisablerProperties ServiceStatusKnownDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss status cannot be determined."), false);
		public SettableDisablerProperties ServiceIsNotRunningDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss is running."), false);
		public SettableDisablerProperties ServiceIsRunningDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss is not running."), false);
		public SettableDisablerProperties ServiceIsStoppedDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss is not stopped."), false);
		public SettableDisablerProperties ServiceNotPendingDisabler = new SettableDisablerProperties(null, KB.K("Waiting for the Windows Service for MainBoss to respond. A refresh may be needed."), false);
		public SettableDisablerProperties HasTraceLogEntriesDisabler = new SettableDisablerProperties(null, KB.K("No 'Trace' log entries in the MainBoss Service log."), false);
		public SettableDisablerProperties HasActivityLogEntriesDisabler = new SettableDisablerProperties(null, KB.K("No 'Activity' log entries in the MainBoss Service log."), false);
		public SettableDisablerProperties HasLogEntriesDisabler = new SettableDisablerProperties(null, KB.K("No log entries in the MainBoss Service log."), false);
		public SettableDisablerProperties IsNotDemonstrationDisabler = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss cannot be configured with only a demonstration license."), true);
		public SettableDisablerProperties HasMainBossServiceAsWindowsServiceLicense = new SettableDisablerProperties(null, KB.K("The Windows Service for MainBoss requires a MainBoss Service License Key."), TIGeneralMB3.MainBossServiceAsWindowsServiceLicenseGroup.Enabled);
		#endregion
		#endregion
		public void ClearStatus() {
			Controller.ClearStatus();
			ServiceNotPendingDisabler.Enabled = false;
			ServiceStatus.Value = Strings.Format(KB.K("Status Pending"));
		}
		private System.Threading.Timer refreshlog = null;
		public void RefreshStatus() {
			MainBossServiceConfiguration config = MainBossServiceConfiguration.GetConfiguration(DB);
			ServiceCode = config.ServiceName;
			if(ServiceCode == null) {
				NoServiceConfigurationRecord.Enabled = false;
				ServiceStatus.Value = Strings.Format(KB.K("No Windows Service can exist unless there is a service configuration record"));
				return;
			}
			NoServiceConfigurationRecord.Enabled = true;
			var sv = ServiceUtilities.Version(config.InstalledServiceVersion);
			var MinVersion =  ServiceUtilities.MinRequiredServiceVersion(DB.ConnectionInfo);
			bool ServiceNotInstalled = string.IsNullOrWhiteSpace(config.ServiceMachineName);
			bool serviceInvalid = string.IsNullOrWhiteSpace(config.InstalledServiceVersion) || string.IsNullOrWhiteSpace(config.SqlUserid);
			bool versionOk = !serviceInvalid && MinVersion <= sv;
			ServiceNotInstalledDisabler.Enabled = sv == null;
			ServiceInstalledDisabler.Enabled = config.InstalledServiceVersion != null;
			ServiceConfigurationNeededWrongComputerDisabler.Enabled = config.ServiceMachineName == null || DomainAndIP.IsThisComputer(config.ServiceMachineName);
			ServiceConfigurationNeededDisabler.Enabled = versionOk;
			ServiceConfigurationNotNeededDisabler.Enabled = !versionOk;

			ServiceNameSource = new StoredValue(dsMB.Schema.T.ServiceConfiguration.F.Code.EffectiveType, config.ServiceName);
			ServerMachineNameSource = new StoredValue(dsMB.Schema.T.ServiceConfiguration.F.ServiceMachineName.EffectiveType, config.ServiceMachineName);

			HasTraceLogEntriesDisabler.Enabled = HasLogEntries(Database.DatabaseEnums.ServiceLogEntryType.Trace);
			HasActivityLogEntriesDisabler.Enabled = HasLogEntries(Database.DatabaseEnums.ServiceLogEntryType.Activity);
			HasLogEntriesDisabler.Enabled = HasLogEntries(null);
			ServiceUtilities.VerifyLicense((MB3Client)DB, null, new Action(delegate () {
				IsNotDemonstrationDisabler.Enabled = false;
			}));
			if (Controller.ServiceComputer != ServerMachineName || Controller.ServiceName != ServiceName) 
				Controller.SetControllerInfo(new StaticServiceConfiguration(ServerMachineName, ServiceName));
			else 
				Controller.Refresh();
			ServiceController.Statuses status = Controller.Status;
			if (Controller.LastStatusError == null && ServiceNotInstalled)
				status = ServiceController.Statuses.NotInstalled;
			string statusText;
			if (ServiceController.StatusNames.TryGetValue(status, out SimpleKey statusKey))
				statusText = statusKey.Translate();
			else {
				statusText = Strings.Format(KB.K("Unknown service controller status 0X{0:x}"), status);
				ServiceStatusKnownDisabler.Enabled = false;
			}
			if(Controller.LastStatusError != null )
				ServiceStatus.Value = Strings.IFormat("{0}{1}{2}", statusText, System.Environment.NewLine, Thinkage.Libraries.Exception.FullMessage(Controller.LastStatusError));
			else
				ServiceStatus.Value = statusText;
			switch (status) {
			case ServiceController.Statuses.Running:
				ServiceInstalledDisabler.Enabled = true;
				ServiceNotInstalledDisabler.Enabled = false;
				ServiceStatusKnownDisabler.Enabled = true;
				ServiceNotPendingDisabler.Enabled = true;
				ServiceIsRunningDisabler.Enabled = true;
				ServiceIsNotRunningDisabler.Enabled = false;
				ServiceIsStoppedDisabler.Enabled = false;
				break;
			case ServiceController.Statuses.Stopped:
				ServiceInstalledDisabler.Enabled = true;
				ServiceNotInstalledDisabler.Enabled = false;
				ServiceStatusKnownDisabler.Enabled = true;
				ServiceIsRunningDisabler.Enabled = false;
				ServiceIsNotRunningDisabler.Enabled = true;
				ServiceIsStoppedDisabler.Enabled = true;
				ServiceNotPendingDisabler.Enabled = true;
				break;
			case ServiceController.Statuses.NotInstalled:
				ServiceInstalledDisabler.Enabled = versionOk; // service not there but configuration record says it is
				ServiceNotInstalledDisabler.Enabled = !versionOk;
				ServiceStatusKnownDisabler.Enabled = true;
				ServiceIsNotRunningDisabler.Enabled = true;
				ServiceIsRunningDisabler.Enabled = false;
				ServiceIsStoppedDisabler.Enabled = false;
				ServiceNotPendingDisabler.Enabled = true;
				ServiceNotInstalledDisabler.Enabled = true;
				ServiceConfigurationNeededDisabler.Enabled = true;
				ServiceConfigurationNotNeededDisabler.Enabled = false;
				break;
			case ServiceController.Statuses.StatusUnavailable:
				// This can happen if the service has a valid definition and appears to be installed but
				// an error occurs getting the service status. Some common examples: The service is not actually defined
				// on the server machine, or the server machine is unreachable (network or machine is down)
			case ServiceController.Statuses.InvalidDefinition:
			case ServiceController.Statuses.NeedsConfiguration:
				ServiceInstalledDisabler.Enabled = true;
				ServiceNotInstalledDisabler.Enabled = false;
				ServiceNotPendingDisabler.Enabled = true;
				ServiceIsNotRunningDisabler.Enabled = true;
				ServiceIsRunningDisabler.Enabled = false;
				ServiceIsStoppedDisabler.Enabled = false;
				break;
			case ServiceController.Statuses.StatusPending:
			case ServiceController.Statuses.StartPending:
			case ServiceController.Statuses.StopPending:
			case ServiceController.Statuses.PausePending:
			case ServiceController.Statuses.ContinuePending:
				// This can happen if the service has a valid definition and appears to be installed but
				// an error occurs getting the service status. Some common examples: The service is not actually defined
				// on the server machine, or the server machine is unreachable (network or machine is down)
				ServiceIsNotRunningDisabler.Enabled = false;
				ServiceIsRunningDisabler.Enabled = false;
				ServiceIsStoppedDisabler.Enabled = false;
				ServiceNotPendingDisabler.Enabled = false;
				break;
			default:
				ServiceIsNotRunningDisabler.Enabled = true;
				ServiceIsRunningDisabler.Enabled = false;
				ServiceIsStoppedDisabler.Enabled = false;
				ServiceNotPendingDisabler.Enabled = false;
				break;
			}
		}
		private bool HasLogEntries(Database.DatabaseEnums.ServiceLogEntryType? type) {
			try {
				return DB.Session.RowsExist(dsMB.Schema.T.ServiceLog, type.HasValue ? new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq(SqlExpression.Constant((int)type.Value)) : null);
			}
			catch {
				return true; // some thing is weird
			}
		}
	}
	#endregion
	#region ManageServiceBrowseLogic
	public class ManageServiceBrowseLogic : ServiceCommonBrowseLogic {
		// This adds commands to install/uninstall the service, to control its logging level (via standard command bytes to the service),
		// and to send custom commands as defined in the Tbl which must specify MB3BTbl.WithManageServiceLogicArg.
		public struct ServiceActionCommand {
			public ServiceActionCommand([Context(Level = 2)]string actionName, [Context(Level = 2)]string actionTip, ApplicationServiceRequests actionValue) {
				ActionName = KB.K(actionName);
				ActionTip = KB.K(actionTip);
				ActionValue = actionValue;
			}
			public Key ActionName;
			public Key ActionTip;
			public ApplicationServiceRequests ActionValue;
		}
		public ManageServiceBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
		}
		#region ServiceControllerCommand which sends a command to the service
		private class ServiceControllerCommand : ICommand {
			ManageServiceBrowseLogic ServiceControl;
			ServiceActionCommand Action;
			int NumberOfRefreshes;
			public ServiceControllerCommand(ManageServiceBrowseLogic serviceControl, ServiceActionCommand action, int numberOfRefreshes = 10) {
				ServiceControl = serviceControl;
				Action = action;
				NumberOfRefreshes = numberOfRefreshes;
			}
			//
			// there is no way to in general do an automatic refresh just because the user is looking at the screen 
			// so after each command that should be generating so output in the log we do a refresh every 10 seconds for 
			// about a minute and half, which should at least show the start of the effect of the command
			// 
			public async void Execute() {
				ServiceControl.ClearStatus();
				ServiceControl.ExecuteServiceAction(Action.ActionValue);
				for(int i = 0; i < NumberOfRefreshes; ++i) {
					await System.Threading.Tasks.Task.Run(() => System.Threading.Thread.Sleep(10 * 1000));
					ServiceControl.RefreshStatus();
				}
			}
			public bool RunElevated {
				get {
					return false;
				}
			}
			public Key Tip {
				get {
					return Action.ActionTip;
				}
			}
			public bool Enabled {
				get {
					return true;
				}
			}
			public event IEnabledChangedEvent EnabledChanged {
				add {
				}
				remove {
				}
			}
		}

		#endregion
		#region Custom command creation
		// Define the logging-control commands. These are handled by the server and the server must be running for them to execute.
		static ManageServiceBrowseLogic.ServiceActionCommand[] pServiceTraceControlActions = {
			new ManageServiceBrowseLogic.ServiceActionCommand("Stop All Tracing", "Turn off all tracing options in the MainBoss Service", ApplicationServiceRequests.TRACE_OFF),
			new ManageServiceBrowseLogic.ServiceActionCommand("Trace All", "Turn on all tracing options in the MainBoss Service", ApplicationServiceRequests.TRACE_ALL),
			new ManageServiceBrowseLogic.ServiceActionCommand("Trace Email Requests", "Trace incoming emails and creation of requests", ApplicationServiceRequests.TRACE_EMAIL_REQUESTS),
			new ManageServiceBrowseLogic.ServiceActionCommand("Trace Notify Requestor", "Trace all email notifications sent to requestors", ApplicationServiceRequests.TRACE_NOTIFY_REQUESTOR),
			new ManageServiceBrowseLogic.ServiceActionCommand("Trace Notify Assignee", "Trace all email notifications sent to request assignees", ApplicationServiceRequests.TRACE_NOTIFY_ASSIGNEE),
		};
		bool? pElevatedCommandsAvailable;
		public bool ElevatedCommandsAvailable {
			get {
				if (!pElevatedCommandsAvailable.HasValue) {
					string serviceCommand = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), KB.I(ServiceConfigurationCommand));
					try {
						pElevatedCommandsAvailable = System.IO.File.Exists(serviceCommand);
					}
					catch {
						pElevatedCommandsAvailable = false;
					}
				}
				return pElevatedCommandsAvailable.Value;
			}
		}

		protected override void CreateCustomBrowserCommands() {
			base.CreateCustomBrowserCommands();

			MultiCommandIfAllEnabled command;

			if (ElevatedCommandsAvailable) {
				CommonLogic.CommandNode serviceStart = Commands.CreateNestedNode(KB.K("Service Control"), null);

				command = new MultiCommandIfAllEnabled(new RunElevatedCallDelegateCommand(KB.K("Start the Windows Service for MainBoss"), new CallDelegateCommand.Delegate(
					delegate () {
						try {
							ClearStatus();
							new TIMainBossService.ServiceCommand("StartService", ServiceCode, DB).RunCommand();
						}
						catch (System.Exception ex) {
							Libraries.Application.Instance.DisplayError(ex);
						}
						RefreshStatus();
						RefreshCommand.Execute(); // refresh any log information that may have appeared
				})),
					HasMainBossServiceAsWindowsServiceLicense,
					ServiceIsStoppedDisabler,
					ServiceNotPendingDisabler,
					ServiceStatusKnownDisabler,
					ServiceConfigurationNeededDisabler,
					ServiceInstalledDisabler,
					NoServiceConfigurationRecord
					);
				serviceStart.AddCommand(KB.K("Start Service"), KB.K("Start"), command, command);

				// Now add the administrator, less likely to be used commands
				command = new MultiCommandIfAllEnabled(new RunElevatedCallDelegateCommand(KB.K("Restart the Windows Service for MainBoss"), new CallDelegateCommand.Delegate(
					delegate () {
						try {
							ClearStatus();
							new TIMainBossService.ServiceCommand("RestartService", ServiceCode, DB).RunCommand();
						}
						catch (System.Exception ex) {
							Libraries.Application.Instance.DisplayError(ex);
						}
						RefreshStatus();
						RefreshCommand.Execute(); // refresh any log information that may have appeared
				})),
					HasMainBossServiceAsWindowsServiceLicense,
					ServiceIsRunningDisabler,
					ServiceNotPendingDisabler,
					ServiceStatusKnownDisabler,
					ServiceConfigurationNeededDisabler,
					ServiceInstalledDisabler,
					NoServiceConfigurationRecord
					);
				serviceStart.AddCommand(KB.K("Restart Service"), KB.K("Restart"), command, command);

				command = new MultiCommandIfAllEnabled(new RunElevatedCallDelegateCommand(KB.K("Stop the Windows Service for MainBoss"), new CallDelegateCommand.Delegate(
					delegate () {
						try {
							ClearStatus();
							new TIMainBossService.ServiceCommand("StopService", ServiceCode, DB).RunCommand();
						}
						catch (System.Exception ex) {
							Libraries.Application.Instance.DisplayError(ex);
						}
						RefreshStatus();
						RefreshCommand.Execute(); // refresh any log information that may have appeared
				})),
					ServiceIsRunningDisabler,
					ServiceNotPendingDisabler,
					ServiceStatusKnownDisabler,
					ServiceConfigurationNeededDisabler,
					ServiceInstalledDisabler,
					NoServiceConfigurationRecord
					);
				serviceStart.AddCommand(KB.K("Stop Service"), KB.K("Stop"), command, command);
			}
			CommonLogic.CommandNode serviceManagementNodes = Commands.CreateNestedNode(KB.K("Service Management"), null);
			command = new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Retrieve Email, create Requests from the Email, and when licensed notify Assignees, and Log the activity for the Windows Service for MainBoss"), new CallDelegateCommand.Delegate(
				delegate () {
					try {
						ClearStatus();
						new TIMainBossService.ProcessEmailServiceCommand(CommonUI.UIFactory, DB, false, HasMainBossServiceAsWindowsServiceLicense.Enabled).RunCommand();
					}
					catch (System.Exception ex) {
						Libraries.Application.Instance.DisplayError(ex);
					}
					RefreshStatus();
					RefreshCommand.Execute(); // refresh any log information that may have appeared
				})),
				ServiceStatusKnownDisabler,
				ServiceNotPendingDisabler,
				ServiceIsNotRunningDisabler,
				ServiceConfigurationNeededDisabler,
				NoServiceConfigurationRecord
				);
			serviceManagementNodes.AddCommand(KB.K("Process Email"), KB.K("Process Email"), command, command);

			command = new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Retrieve Email, create Requests from the Email, and when licensed notify Assignees, and Log the activity with detailed Diagnostics for the Windows Service for MainBoss"), new CallDelegateCommand.Delegate(
				delegate () {
					try {
						ClearStatus();
						new TIMainBossService.ProcessEmailServiceCommand(CommonUI.UIFactory, DB, true, HasMainBossServiceAsWindowsServiceLicense.Enabled).RunCommand();
					}
					catch (System.Exception ex) {
						Libraries.Application.Instance.DisplayError(ex);
					}
					RefreshStatus();
					RefreshCommand.Execute(); // refresh any log information that may have appeared
				})),
				ServiceStatusKnownDisabler,
				ServiceIsNotRunningDisabler,
				ServiceConfigurationNeededDisabler,
				NoServiceConfigurationRecord
				);
			serviceManagementNodes.AddCommand(KB.K("Process Email with Diagnostics"), KB.K("Process Email with Diagnostics"), command, command);

			if (ElevatedCommandsAvailable) {
				command = new MultiCommandIfAllEnabled(new RunElevatedCallDelegateCommand(KB.K("Associates the Windows Service for MainBoss with this database and configures the service."), new CallDelegateCommand.Delegate(
				delegate () {
					try {
						ClearStatus();
						new TIMainBossService.ServiceCommand("ConfigureService", ServiceCode, DB).RunCommand();
					}
					catch (System.Exception ex) {
						Libraries.Application.Instance.DisplayError(ex);
					}
					SetAllOutOfDate();
				})),
				ServiceNotPendingDisabler,
				HasMainBossServiceAsWindowsServiceLicense,
				IsNotDemonstrationDisabler,
				ServiceConfigurationNeededWrongComputerDisabler,
				NoServiceConfigurationRecord
				);
				serviceManagementNodes.AddCommand(KB.K("Configure Windows Service for MainBoss"), KB.K("Configure"), command, command);

				command = new MultiCommandIfAllEnabled(new RunElevatedCallDelegateCommand(KB.K("Verify configuration of the Windows Service for MainBoss."), new CallDelegateCommand.Delegate(
					delegate () {
						try {
							ClearStatus();
							new TIMainBossService.ServiceCommand("VerifyService", ServiceCode, DB).RunCommand();
						}
						catch (System.Exception ex) {
							Libraries.Application.Instance.DisplayError(ex);
						}
						SetAllOutOfDate();
					})),
					ServiceNotPendingDisabler,
					ServiceInstalledDisabler,
					HasMainBossServiceAsWindowsServiceLicense,
					IsNotDemonstrationDisabler,
					NoServiceConfigurationRecord
					);
				serviceManagementNodes.AddCommand(KB.K("Verify Windows Service for MainBoss"), KB.K("Verify"), command, command);

				command = new MultiCommandIfAllEnabled(new RunElevatedCallDelegateCommand(KB.K("Delete the Windows Service for MainBoss"), new CallDelegateCommand.Delegate(
					delegate () {
						try {
							ClearStatus();
							new TIMainBossService.ServiceCommand("DeleteService", ServiceCode, DB).RunCommand();
						}
						catch (System.Exception ex) {
							Libraries.Application.Instance.DisplayError(ex);
						}
						SetAllOutOfDate();
					})),
					ServiceInstalledDisabler,
					ServiceNotPendingDisabler,
					ServiceIsNotRunningDisabler,
					NoServiceConfigurationRecord
					);
				serviceManagementNodes.AddCommand(KB.K("Delete Windows Service for MainBoss"), KB.K("Delete"), command, command);

				CommonLogic.CommandNode actionNodes = Commands.CreateNestedNode(KB.K("Service"), KB.K("Service"));
				foreach (ManageServiceBrowseLogic.ServiceActionCommand c in ((MB3BTbl.WithManageServiceLogicArg)BTbl.BrowserLogicClassArg).ServiceCommands) {
					command = new MultiCommandIfAllEnabled(new ServiceControllerCommand(this, c),
						ServiceIsRunningDisabler,
						ServiceInstalledDisabler,
						ServiceStatusKnownDisabler,
						ServiceConfigurationNeededDisabler,
						ServiceInstalledDisabler,
						NoServiceConfigurationRecord
						);
					actionNodes.AddCommand(c.ActionName, c.ActionName, command, command);
				}
			}
			CommonLogic.CommandNode loggingNodes = Commands.CreateNestedNode(KB.K("Logging"), KB.K("Logging"));

			command = new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Permanently delete all current 'Trace' entries in the MainBoss Service log"), new CallDelegateCommand.Delegate(delegate () {
				try {
					var sqlcmd = new DeleteSpecification(dsMB.Schema.T.ServiceLog, new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq((int)Database.DatabaseEnums.ServiceLogEntryType.Trace));
					DB.Session.ExecuteCommand(sqlcmd);
					RefreshCommand.Execute();
				}
				catch(System.Exception ex) {
					Libraries.Application.Instance.DisplayError(ex);
				}
				RefreshStatus();
			})),
				HasTraceLogEntriesDisabler);
			loggingNodes.AddCommand(KB.K("Delete Trace Logging Entries"), KB.K("Delete Trace Logging Entries"), command, command);

			command = new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Permanently delete all current 'Activity' and 'Trace' entries in the MainBoss Service log"), new CallDelegateCommand.Delegate(delegate () {
				try {
					var sqlcmd = new DeleteSpecification(dsMB.Schema.T.ServiceLog, (new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq((int)Database.DatabaseEnums.ServiceLogEntryType.Activity))
																					.Or(new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq((int)Database.DatabaseEnums.ServiceLogEntryType.Trace)));
					DB.Session.ExecuteCommand(sqlcmd);
					RefreshCommand.Execute();
				}
				catch (System.Exception ex) {
					Libraries.Application.Instance.DisplayError(ex);
				}
				RefreshStatus();
			})),
				HasActivityLogEntriesDisabler);
			loggingNodes.AddCommand(KB.K("Delete Activity Logging Entries"), KB.K("Delete Activity Logging Entries"), command, command);


			// Provide a means to delete all the log messages
			command = new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Permanently delete all current entries in the MainBoss Service log"), new CallDelegateCommand.Delegate(delegate () {
				try {
					var sqlcmd = new DeleteSpecification(dsMB.Schema.T.ServiceLog, null);
					DB.Session.ExecuteCommand(sqlcmd);
					RefreshCommand.Execute();
				}
				catch (System.Exception ex) {
					Libraries.Application.Instance.DisplayError(ex);
				}
				RefreshStatus();
			})),
				HasLogEntriesDisabler);
			loggingNodes.AddCommand(KB.K("Delete All Logging Entries"), KB.K("Delete All Logging Entries"), command, command);
			if (ElevatedCommandsAvailable) {
				// commands to trace service actions
				foreach (ManageServiceBrowseLogic.ServiceActionCommand c in pServiceTraceControlActions) {
					command = new MultiCommandIfAllEnabled(new ServiceControllerCommand(this, c),
						ServiceNotPendingDisabler,
						ServiceIsRunningDisabler,
						ServiceInstalledDisabler,
						ServiceStatusKnownDisabler,
						ServiceConfigurationNeededDisabler,
						ServiceInstalledDisabler
						);
					loggingNodes.AddCommand(c.ActionName, c.ActionName, command, command);
				}
			}
		}
		#endregion
		#region Service Control Actions
		internal void ExecuteServiceAction(ApplicationServiceRequests action) {
			try {
				Controller.ExecuteCommand((int)action);
			}
			catch(System.Exception ex) {
				Libraries.Application.Instance.DisplayError(ex);
			}
		}
		#endregion
	}
	#endregion
}
