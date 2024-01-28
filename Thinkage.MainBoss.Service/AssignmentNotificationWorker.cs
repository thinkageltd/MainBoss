using Thinkage.MainBoss.Service;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using System.Linq;

namespace Thinkage.MainBoss.Service
{
	public class AssignmentNotificationWorker: MainBossServiceWorker
	{
		public AssignmentNotificationWorker(ServiceWithServiceWorkers baseService)	: base(baseService) {}
		
		public override string Name {
			get	{
				return KB.I("AssignmentNotification");
			}
		}
		public override bool ValidCommand(int command) {
			return (new[] { ApplicationServiceRequests.TERMINATE_ALL, 
							  ApplicationServiceRequests.PROCESS_ALL,
							  ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS
							}).Any(e => (int)e == command);
		}
		protected override void DoWork(int command) {
			base.DoWork(command);
			if (DBSession != null) {
				switch ((ApplicationServiceRequests)command) {
					case ApplicationServiceRequests.PROCESS_ALL:
					case ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS: {
						AssignmentNotificationProcessor.DoAllAssignmentNotifications(ServiceLogging, DBSession, Logging.Activities, Logging.NotifyAssignee);
						break;
					}
				}
			}
		}
		public override int OnStartCommand	{
			get	{
				return (int)ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS;
			}
		}
		public override int OnIntervalTimerCommand	{
			get	{
				return (int)ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS;
			}
		}
		public override System.TimeSpan GetWorkerInterval(MainBossServiceConfiguration config)	{
			return config.NotificationInterval;
		}
	}
}