using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using Thinkage.MainBoss.Service;

namespace Thinkage.MainBoss.Service {
	/// <summary>
	/// Base class for MainBoss services where common code can be put that is shared amongst different workers.
	/// </summary>
	abstract public class MainBossServiceWorker : Thinkage.Libraries.Service.ServiceWorker {
		/// <summary>
		/// Properly case ServiceBase
		/// </summary>
		public MainBossService MainBossServiceBase => (MainBossService)ServiceBase;

		protected MainBossServiceWorker(ServiceWithServiceWorkers serviceBase) : base(serviceBase) {
		}
		abstract public override string Name {
			get;
		}
		abstract public override int OnStartCommand {
			get;
		}
		abstract public TimeSpan GetWorkerInterval(MainBossServiceConfiguration config);
		public override int OnIntervalTimerCommand {
			get {
				return (int)ApplicationServiceRequests.PROCESS_ALL;
			}
		}
		static bool htmLinkedChecked = false;
		protected override void DoWork(int command) {
			if (!htmLinkedChecked) {
				htmLinkedChecked = true;
				MainBossServiceConfiguration config = MainBossServiceConfiguration.GetConfiguration(DBSession.ConnectionInfo);
				if ((ServiceUtilities.HasWebRequestsLicense || ServiceUtilities.HasWebAccessLicense) && config.MainBossRemoteURL == null)
					ServiceLogging.LogWarning(Strings.Format(KB.K("{0} has not been set. Notification messages will not contain a link to the Web Access or Web Request web page."), dsMB.Schema.T.ServiceConfiguration.F.MainBossRemoteURL.LabelKey.Translate()));
			}
			if ((int)command == (int)Thinkage.Libraries.Service.Application.ServiceRequests.USER_STOP_SERVICE) {
				Stop();
				return;
			}
			// If we lost a valid DBSession, just reestablish it
			if (DBSession == null)
				SetupDBSession();
			//derived class will do the work
		}
		protected override bool HandleWorkException(System.Exception e) {
			ServiceLogging.LogError(Thinkage.Libraries.Exception.FullMessage(e));
			if (e is System.Data.Common.DbException || e is System.Data.SqlClient.SqlException || e is System.InvalidOperationException) {
				CleanupDBSession();
				return true;
			}
			return false;
		}
		#region InitializeService
		protected MB3Client DBSession = null;
		public override void InitializeService() {
			SetupDBSession();
			MainBossServiceConfiguration config = MainBossServiceConfiguration.GetConfiguration(DBSession.ConnectionInfo);
			SetWakeUpInterval(GetWorkerInterval(config));
		}
		public override void TeardownService() {
			CleanupDBSession();
		}
		protected void SetupDBSession() {
			DBSession = new MB3Client(MainBossServiceBase.DBConnection);
			ServiceUtilities.VerifyLicense(DBSession, ServiceLogging);
		}
		protected void CleanupDBSession() {
			if (DBSession != null) {
				DBSession.CloseDatabase();
				DBSession = null;
			}
		}
		#endregion
	}
}
