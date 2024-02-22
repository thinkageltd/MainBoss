using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;
using System;
using Thinkage.Libraries.DBAccess;

namespace Thinkage.MainBoss.Database {
	// The purpose of this class is to "rescue" DatabaseHistory messages created during a failed transaction and to try to
	// commit them anyway.
	public class DatabaseHistoryManager {
		public DatabaseHistoryManager(DBClient session) {
			Session = session;
		}
		private DBClient Session;
//		private DateTime? LastPreviousDate;
#if WORK_IN_PROGRESS
		// Any DatabaseHistory rows created in the workingDS during the PerformTransaction body will be "rescued" and
		// flushed to the DB outside the transaction even if the transaction rolls back. We will also query any messages created
		// in the DB itself by direct SQL code.
		// The final SuccessMessage will not be written out.
		// In addition a message will be placed in the DatabaseHistory giving the failure exception that caused the error.
		public void PerformTransaction(SqlClient.Transaction body, [Translated] string operationDescription, ref DBVersionHandler versionHandler) {
			DBVersionHandler originalVersionHandler = versionHandler;
			try {
				Session.PerformTransaction(true, delegate() {
					try {
						//		Get the date of the last existing message, if any, into DateTime.
						originalVersionHandler.LogHistory(Session, Strings.Format(KB.K("Starting: {0}"), operationDescription));
						return body();
					}
					catch {
						//	query any messages created since the TX started.
						throw;
					}
				});
				versionHandler.LogHistory(Session, Strings.Format(KB.K("Completed: {0}"), operationDescription));
			}
			catch (System.Exception ex) {
				// Place the rescued messages in the DS as "new" records. This must be done using the originalVersionHandler since any
				// changes to the DB structure have already been rolled back by Session.PerformTransaction.
				originalVersionHandler.LogHistory(Session, Strings.Format(KB.K("Failed: {0}"), operationDescription), Thinkage.Libraries.Exception.FullMessage(ex));
				throw;
			}
		}
#endif
		public SqlExpression GetFilter() {	// or return a BrowseControl.Filter object?
			// Return the Entry-Date based filter from the start of the last transaction.
			// If PerformTransaction has not been called yet return FALSE.
			return null;
		}
	}
}