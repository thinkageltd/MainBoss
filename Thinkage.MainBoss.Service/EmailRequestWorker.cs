using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using Thinkage.MainBoss.Service;
using System.Linq;

namespace Thinkage.MainBoss.Service
{
	public class EmailRequestWorker : MainBossServiceWorker	{
		public EmailRequestWorker(ServiceWithServiceWorkers baseService) : base(baseService) {
		}
		public override string Name	{
			get	{
				return KB.I("ServiceIncomingEmail");
			}
		}
		public override bool ValidCommand(int command) {
			return (new[] { ApplicationServiceRequests.TERMINATE_ALL,
							ApplicationServiceRequests.PROCESS_ALL,
							ApplicationServiceRequests.PROCESS_REQUESTS_INCOMING_EMAIL,
					}).Any(e => (int)e == command);
		}
		protected override void DoWork(int command) {
			if (!ServiceUtilities.HasRequestLicense)
				return; // no request license no work
			base.DoWork(command);
			if (DBSession != null) {
				switch ((ApplicationServiceRequests)command) {
					case ApplicationServiceRequests.PROCESS_REQUESTS_INCOMING_EMAIL:
					case ApplicationServiceRequests.PROCESS_ALL: {
						RequestProcessor.DoAllRequestProcessing(ServiceLogging, DBSession, Logging.Activities, Logging.ReadEmailRequest);
							break;
						}
				}
			}
		}
		public override int OnStartCommand	{
			get	{
				return (int)ApplicationServiceRequests.PROCESS_REQUESTS_INCOMING_EMAIL;
			}
		}
		public override System.TimeSpan GetWorkerInterval(MainBossServiceConfiguration config)	{
			return config.WakeUpInterval;
		}
	}
}
