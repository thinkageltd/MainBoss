using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.Service
{
	public class MainBossServiceApplication : Thinkage.Libraries.Service.Application {
		#region Constructor
		public MainBossServiceApplication()	: this (new string[] {}) {	}
		public MainBossServiceApplication(string[] args) : base()	{
			ServiceOptions = new ServiceOptions(args);
			ServiceName = ServiceOptions.ServiceName ?? KB.I("MainBossService");
			if (ServiceOptions.ManuallyRun)  // to allow forcing the running of this program as a console program under system on a terminal server.
				UserInterface.IsRunningAsAService = false;
			new StandardApplicationIdentification(this, MainBossServiceConfiguration.MainBossServiceTag, MainBossServiceDefinition.ServiceDisplayName);
			new Thinkage.MainBoss.Database.RaiseErrorTranslationKeyBuilder(this);
		}
		protected override void CreateUIFactory() {
			new Thinkage.Libraries.Service.UserInterface(this, KB.I("MainBoss"));
		}
		#endregion
		public override MessageExceptionContext ServiceStartupContext() {
			return new MessageExceptionContext(KB.K("Service {0} running as user {1}"), ServiceOptions.ServiceName, Thinkage.Libraries.Service.Application.Instance.UserName);
		}
		public override ServiceDescriptor ServiceDefinition	{
			get	{
				if (pMainBossServiceDefinition == null)
					pMainBossServiceDefinition = new MainBossServiceDefinition();
				return pMainBossServiceDefinition;
			}
		}
		ServiceDescriptor pMainBossServiceDefinition;
		public ServiceOptions ServiceOptions;

		public override Thinkage.Libraries.Service.Application.RunApplicationDelegate GetRunApplicationDelegate	{
			get	{
				return delegate() {
					using (var x = new MainBossService(this, ServiceOptions.Force)) {
						x.Run();
						int NBusy = 0;
						if (!UserInterface.IsRunningAsAService) {
							x.DebugCommand(ServiceOptions.TestConfiguration ? ApplicationServiceRequests.TRACE_ALL : ApplicationServiceRequests.TRACE_ACTIVITIES);
#if DEBUG_servicecommands					// when not in debug mode one PROCESS_ALL will be done, and the program will terminate
							var commands = "Commands: 'help' 'stop' 'pause' 'resume' 'reread'"
								+ System.Environment.NewLine + "          'all' 'email' 'workorders' 'requests'"
								+ System.Environment.NewLine + "          'log rest' 'log empty' 'log age'"
								+ System.Environment.NewLine + "          'trace clear' 'trace all' 'trace off' 'trace flow'"
								+ System.Environment.NewLine + "          'trace request email' 'trace notify requestor' 'trace notify assignee'";
							System.Console.WriteLine("MainBoss Service Debug");
							while (true) {
								System.Threading.Thread.Sleep(10 * 1000);  // the sleeps in here are heuristic, but in general allow most output to come out before the prompt
								NBusy = activeWorkers(x, 20);
								System.Console.WriteLine(commands);
								System.Console.Write("{0} Busy -- Command? ", NBusy);
								var l = System.Console.ReadLine();
								int n = 0;
								if (int.TryParse(l, out n)) {
									x.DebugCommand((ApplicationServiceRequests)n);
								}
								switch (l.ToLower().Trim()) {
									case "help":								System.Console.WriteLine(commands); continue;
									case "stop":								break; 
									case "pause":								x.DebugCommand(ApplicationServiceRequests.PAUSE_SERVICE); continue;
									case "resume":								x.DebugCommand(ApplicationServiceRequests.RESUME_SERVICE); continue;
									case "reread":								x.DebugCommand(ApplicationServiceRequests.REREAD_CONFIG); continue;
									case "all":									x.DebugCommand(ApplicationServiceRequests.PROCESS_ALL); continue;
									case "email":								x.DebugCommand(ApplicationServiceRequests.PROCESS_REQUESTS_INCOMING_EMAIL); continue;
									case "workorders":							x.DebugCommand(ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS); continue;
									case "requests":							x.DebugCommand(ApplicationServiceRequests.PROCESS_REQUESTOR_NOTIFICATIONS); continue;
									case "log reset":							x.DebugCommand(ApplicationServiceRequests.RESET_LOGGING); continue;
									case "log empty":							x.DebugCommand(ApplicationServiceRequests.LOG_EMPTY); continue;
									case "log age":								x.DebugCommand(ApplicationServiceRequests.LOG_AGE); continue;
									case "trace clear":							x.DebugCommand(ApplicationServiceRequests.TRACE_CLEAR); continue;  // removes all trace log entries
									case "trace all":							x.DebugCommand(ApplicationServiceRequests.TRACE_ALL); continue;
									case "trace off":							x.DebugCommand(ApplicationServiceRequests.TRACE_OFF); continue;
									case "trace flow":							x.DebugCommand(ApplicationServiceRequests.TRACE_ACTIVITIES); continue;
									case "trace request email":					x.DebugCommand(ApplicationServiceRequests.TRACE_EMAIL_REQUESTS); continue;
									case "trace notify requestor":				x.DebugCommand(ApplicationServiceRequests.TRACE_NOTIFY_REQUESTOR); continue;
									case "trace notify assignee":				x.DebugCommand(ApplicationServiceRequests.TRACE_NOTIFY_ASSIGNEE); continue;
									case "": continue;
									default:
										System.Console.WriteLine("Unknown command: " + l);
										System.Console.WriteLine(commands);
										continue;
								}
								break;
							}
#else
							//x.DebugCommand(ApplicationServiceRequests.PROCESS_ALL);
#endif
							x.StopWorkers();
							NBusy = activeWorkers(x, 5 * 60);
							x.ServiceLogging.LogClose(null);
							MainBossService.Exit(0);
						}
					}
					return null;
				};
			}
		}
		private int activeWorkers(MainBossService s, int maxPauseInSeconds) {
			int NBusy = 0;
			int retries = maxPauseInSeconds / 5;
			for (int i = 0; i < retries ; ++i) {
				NBusy = s.NumberBusy();
				if (NBusy != 0)
					System.Threading.Thread.Sleep(5 * 1000);
				else break;
			}
			return NBusy;
		}
	}
}