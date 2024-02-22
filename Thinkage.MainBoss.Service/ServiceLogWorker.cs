using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.MainBoss.Service;
using System;
using System.Linq;

namespace Thinkage.MainBoss.Service {
	public class ServiceLogWorker : MainBossServiceWorker {
		// ToDo: add a ServiceLogHistoryInterval to service config
		// for now default to once a day at midnight
		// and keep logs for 30 days.
		TimeSpan ExpiryInterval = new TimeSpan(30, 0, 0, 0);
		public ServiceLogWorker(ServiceWithServiceWorkers baseService)
			: base(baseService) {
		}
		public override string Name {
			get {
				return KB.I("ServiceMainBossLog");
			}
		}
		public override bool ValidCommand(int command) {
			return (new[] { ApplicationServiceRequests.TERMINATE_ALL,
							ApplicationServiceRequests.PROCESS_ALL,
							ApplicationServiceRequests.TRACE_CLEAR,
							ApplicationServiceRequests.LOG_EMPTY,
							ApplicationServiceRequests.LOG_AGE,
					}).Any(e => (int)e == command);
		}
		protected override void DoWork(int command) {
			base.DoWork(command);
			if (DBSession != null) {
				CommandBatchSpecification batch;
				switch ((ApplicationServiceRequests)command) {
					case ApplicationServiceRequests.PROCESS_ALL:
					case ApplicationServiceRequests.LOG_AGE: 
						var expiryDate = DateTime.Today - ExpiryInterval; // note: the calculation will give a time of 0:00:00 
						if (lastrunDate == expiryDate) break;
						batch = new CommandBatchSpecification();
						batch.CreateNormalParameter(KB.I("ExpiryDate"), Thinkage.Libraries.XAF.Database.Service.MSSql.Server.SqlDateTimeTypeInfo).Value = expiryDate;
						batch.Commands.Add(new MSSqlLiteralCommandSpecification("DELETE ServiceLog Where EntryDate < @ExpiryDate"));
						DBSession.Session.ExecuteCommandBatch(batch);
						ServiceLogging.LogInfo(Strings.Format(KB.K("Log truncation request processing completed, removed all entries older than {0}"), expiryDate));
						lastrunDate = expiryDate;
						break;
#if DEBUG
					case ApplicationServiceRequests.LOG_EMPTY:
						batch = new CommandBatchSpecification();
						batch.Commands.Add(new MSSqlLiteralCommandSpecification("DELETE ServiceLog Where EntryDate is not null")); // we don't want to delete the table just the entries
						DBSession.Session.ExecuteCommandBatch(batch);
						ServiceLogging.LogInfo(Strings.Format(KB.K("Log empty request processing completed, removed all log entries")));
						break;
					case ApplicationServiceRequests.TRACE_CLEAR:
						batch = new CommandBatchSpecification();
						batch.Commands.Add(new MSSqlLiteralCommandSpecification(string.Format("DELETE ServiceLog Where EntryType = {0}", (int)Thinkage.MainBoss.Database.DatabaseEnums.ServiceLogEntryType.Trace))); 
						DBSession.Session.ExecuteCommandBatch(batch);
						ServiceLogging.LogInfo(Strings.Format(KB.K("Log clear request processing completed, removed all trace log entries")));
						break;
#endif
				}
			}
		}
		DateTime lastrunDate;
		public override int OnStartCommand {
			get {
				return (int)ApplicationServiceRequests.LOG_AGE;
			}
		}
		public override System.TimeSpan GetWorkerInterval(MainBossServiceConfiguration config) {
			return new System.TimeSpan(1, 0, 0, 0);
		}
	}
}