using System;
using System.Threading.Tasks;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Database.Service {
	public class LogToDatabase : IServiceLogging {

		#region IServiceLogging
		public virtual void LogError(string s) {
			Log(Database.DatabaseEnums.ServiceLogEntryType.Error, s);
		}
		public virtual void LogWarning(string s) {
			Log(Database.DatabaseEnums.ServiceLogEntryType.Warn, s);
		}
		public virtual void LogInfo(string s) {
			Log(Database.DatabaseEnums.ServiceLogEntryType.Info, s);
		}
		public virtual void LogActivity(string s) {
			Log(Database.DatabaseEnums.ServiceLogEntryType.Activity, s);
		}
		public virtual void LogTrace(bool tolog, string s) {
			if (tolog)
				Log(Database.DatabaseEnums.ServiceLogEntryType.Trace, s);
		}
		public virtual void LogClose(string s) {
			Log(Database.DatabaseEnums.ServiceLogEntryType.Close, s);
			CloseLog();
		}
		#endregion
		public LogToDatabase(DBClient.Connection dbConnection) {
			DBConnection = dbConnection;
		}
		private Task loggingTask;
		public void ProcessLog() {
			loggingTask = System.Threading.Tasks.Task.Factory.StartNew(() => ProcessLogThread());
		}
		public void CloseLog() {
			if (loggingTask != null) {
				loggingTask.Wait();
				loggingTask = null;
			}
		}
		/// <summary>
		/// Allow a secondary log to be created. Used to message to terminal window when, or to present error screens to user.
		/// </summary>
		public virtual void SecondaryLogging(LogMessage mesg) { }
		public virtual void LoggingFailure(LogMessage mesg, System.Exception e) {
			if (LogFailure != null && LogFailure.Message == e.Message && LogFailureTime > DateTime.Now - new TimeSpan(0, 1, 0))
				return;  // don't display the same message more than once an hour.
			LogFailure = e;
			LogFailureTime = DateTime.Now;
			var t = mesg.Type == Database.DatabaseEnums.ServiceLogEntryType.Close ? Database.DatabaseEnums.ServiceLogEntryType.Info : mesg.Type;
			Thinkage.Libraries.Application.Instance.DisplayError(new GeneralException(e, KB.K("Cannot write ServiceLog: {0}{1,-5}: {2}"), System.Environment.NewLine, t, mesg.Message));
		}
		public void Log(Database.DatabaseEnums.ServiceLogEntryType type, string s) {
			// If database connection, log the message in the database table; otherwise use the base Log method
			if (loggingTask == null && s == null) return; // don't create log for a null message.
			string source = System.Threading.Thread.CurrentThread.Name;
			if (source == null)
				source = KB.I("MainBossService");
			LogMessage msg = new LogMessage(type, source, s);
			if (loggingTask != null && loggingTask.IsCompleted)
				loggingTask = null;
			if( loggingTask == null) 
				ProcessLog();
			if (!LogMessages.TryAdd(msg))
				SecondaryLogging(msg);
		}
		/// <summary>
		/// A blocking queue where to put messages for later consumption by a worker thread who will put them in the database (if possible)
		/// </summary>
		public readonly System.Collections.Concurrent.BlockingCollection<LogMessage> LogMessages = new System.Collections.Concurrent.BlockingCollection<LogMessage>();

		/// <summary>
		/// The worker thread to process LogMessages
		/// </summary>
		public bool LogBusy { get { return busy; } }
		bool busy = false;
		static DateTime nextLogCleanup = new DateTime(2000, 1, 1);  // TODO: if there logs on a multiple databases, only one log at a time will randomly will be cleaned up
		public void ProcessLogThread() {
			foreach (LogMessage msg in LogMessages.GetConsumingEnumerable()) {
				System.Exception lasterror = null;
				busy = true;
				bool closing = msg.Type == Database.DatabaseEnums.ServiceLogEntryType.Close;
				var mtype = closing ? Database.DatabaseEnums.ServiceLogEntryType.Info : msg.Type;
				SecondaryLogging(msg);
				//
				// Two attempts to write the service log
				// with no exceptions the code is one pass
				// If we cannot attach the service log (LogDs will be null)
				// log the failure in the event log or command line
				// If the connection was broken then LogDs will not be null
				// and the Update will fail. 
				// the code will loop one time, trying to reestablish the 
				// connection and then write the original message.
				//
				if (!string.IsNullOrWhiteSpace(msg.Message)) {
					if (nextLogCleanup < DateTime.Now) // once a day (and once per running of the program) delete all log entries over 90 days old.
						try {
							if (LogDs == null)
								GetServiceLog();
							nextLogCleanup = DateTime.Today.AddDays(1);
							var sqlcmd = new DeleteSpecification(dsMB.Schema.T.ServiceLog, new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryDate).Lt(DateTime.Now.AddDays(-90)));
							LogSession.Session.ExecuteCommand(sqlcmd);
						}
						catch (System.Exception e) {
							lasterror = e;
							CloseServiceLog();
						}

					foreach (int pass in new int[] { 0, 1 }) {
						lasterror = null;
						try {
							if (LogDs == null)
								GetServiceLog();
							dsMB.ServiceLogDataTable log = LogDs.T.ServiceLog;
							dsMB.ServiceLogRow entry = log.AddNewServiceLogRow();
							entry.F.Source = msg.Source;
							entry.F.Message = msg.Message;
							entry.F.EntryType = (byte)mtype;
							LogDs.DB.Update(LogDs);
							log.Clear(); // work is done, remove all rows for this action
						}
						catch (System.Exception e) {
							lasterror = e;
							CloseServiceLog();
							continue;
						}
						break;
					}
				}
				if (lasterror != null) {
					LoggingFailure(msg, lasterror);
				}
				else {
					LogFailureTime = null;
					LogFailure = null;
				}
				if (closing) {
					CloseServiceLog();
					return;
				}
				busy = false;
			}
		}
		private dsMB LogDs;
		MB3Client LogSession;
		DBClient.Connection DBConnection;
		private void GetServiceLog() {
			LogSession = null;
			LogDs = null;
			try {
				LogSession = new MB3Client(DBConnection);
				LogSession.ObtainSession((int)DatabaseEnums.ApplicationModeID.MainBossService);
				LogDs = new dsMB(LogSession);
				LogDs.EnsureDataTableExists(dsMB.Schema.T.ServiceLog);
			}
			catch (GeneralException e) {
				LogDs = null;
				SecondaryLogging(new LogMessage(DatabaseEnums.ServiceLogEntryType.Error, null, Thinkage.Libraries.Exception.FullMessage(e)));
				throw;
			}
			return;
		}
		private void CloseServiceLog() {
			try {
				if (LogSession != null)
					LogSession.CloseDatabase();
			}
			catch (System.Exception) { }
			LogSession = null;
			LogDs = null;
		}
		public System.Exception LogFailure { get; private set; } = null;
		public DateTime? LogFailureTime { get; private set; } = null;
		public void Dispose()
		{
			Dispose(!inDispose);
			GC.SuppressFinalize(this);
		}
		private bool inDispose = false;
		protected virtual void Dispose(bool disposing) {
			if (disposing) return;
			disposing = true;
			if (disposing && LogSession != null ) {
				LogClose(null);
			}
		}

	}
	public class LogToUserAndDatabase : LogToDatabase {
		public void LogLoggingParameters() {
			string s = Logging.GetTranslatedLoggingParameterMessage();
			if (!System.String.IsNullOrEmpty(s))
				LogInfo(Thinkage.Libraries.Strings.Format(KB.K("MainBoss Service Logging set to show: {0}"), s));
		}
		ServiceBase eventLogging = null;
		public LogToUserAndDatabase(DBClient.Connection dbConnection)
			: base(dbConnection) {
			if (UserInterface.IsRunningAsAService)
				eventLogging = new ServiceBase();
		}
		public override void SecondaryLogging(LogMessage mesg) {
			base.SecondaryLogging(mesg);
			if (string.IsNullOrWhiteSpace(mesg.Message)) return;
			if (!UserInterface.IsRunningAsAService) {
				var k = Database.DatabaseEnums.ServiceLogEntryTypeProvider.GetLabel(mesg.Type, new EnumTypeInfo(typeof(Database.DatabaseEnums.ServiceLogEntryType))).Translate();
				System.Console.WriteLine(Strings.IFormat("{0}{1,-24} {2,-8}: {3}", System.Environment.NewLine, mesg.Source,  k, mesg.Message));
			}
		}
		protected override void Dispose(bool disposing) {
			if (disposing && eventLogging != null) {
				base.Dispose(disposing);
				var v = eventLogging;
				eventLogging = null;
				v.Dispose();
			}
		}
	}
	public class LogAndDatabaseAndError : LogToDatabase, IDisposable {
		public void LogLoggingParameters() {
			string s = Logging.GetTranslatedLoggingParameterMessage();
			if (!System.String.IsNullOrEmpty(s))
				LogInfo(Thinkage.Libraries.Strings.Format(KB.K("MainBoss Service Logging set to show: {0}"), s));
		}
		ServiceBase eventLogging = null;
		public LogAndDatabaseAndError(DBClient.Connection dbConnection)
			: base(dbConnection) {
			if (UserInterface.IsRunningAsAService)
				eventLogging = new ServiceBase();
		}
		public override void SecondaryLogging(LogMessage mesg) {
			base.SecondaryLogging(mesg);
			if (string.IsNullOrWhiteSpace(mesg.Message)) return;
			if (mesg.Type == DatabaseEnums.ServiceLogEntryType.Error)
				Thinkage.Libraries.Application.Instance.DisplayError(new GeneralException(KB.T("{0}"), mesg.Message));
			else if (mesg.Type == DatabaseEnums.ServiceLogEntryType.Warn)
				Thinkage.Libraries.Application.Instance.DisplayError(new GeneralException(KB.T("{0}"), mesg.Message));
		}
	}

}
