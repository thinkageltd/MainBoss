using System.Linq;
using Thinkage.MainBoss.Service;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
namespace Thinkage.MainBoss.Service
{
	public class RequestorNotificationWorker : MainBossServiceWorker
	{
		public RequestorNotificationWorker(ServiceWithServiceWorkers baseService) : base(baseService) {
		}
		public override string Name	{
			get	{
				return KB.I("RequestorNotification");
			}
		}
		public override bool ValidCommand(int command) {
			return (new  [] { ApplicationServiceRequests.TERMINATE_ALL, 
							  ApplicationServiceRequests.PROCESS_ALL,
							  ApplicationServiceRequests.PROCESS_REQUESTOR_NOTIFICATIONS
							}).Any(e=>(int)e==command);
		}
		protected override void DoWork(int command) {
			base.DoWork(command);
			if (DBSession != null) {
				switch ((ApplicationServiceRequests)command) {
					case ApplicationServiceRequests.PROCESS_REQUESTOR_NOTIFICATIONS:
					case ApplicationServiceRequests.PROCESS_ALL: {
						RequestorNotificationProcessor.DoAllRequestorNotifications(ServiceLogging, DBSession, Logging.Activities, Logging.NotifyRequestor);
							break;
						}
				}
			}
		}
		public override int OnStartCommand	{
			get	{
				return (int)ApplicationServiceRequests.PROCESS_REQUESTOR_NOTIFICATIONS;
			}
		}
		public override System.TimeSpan GetWorkerInterval(MainBossServiceConfiguration config)	{
			return config.NotificationInterval;
		}
	}
}