using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Service logging types. Set dynamically by service at start up and when refreshed manually by Service Request.
	/// </summary>
	public static class Logging {
		static public bool ReadEmailRequest = false;
		static public bool NotifyRequestor = false;
		static public bool NotifyAssignee = false;
		static public bool Activities = true;
		static public bool Tracing {
			get { return ReadEmailRequest | NotifyRequestor | NotifyAssignee ; }
		}
		/// <summary>
		/// Return current setting as comma separated string of all our Logging conditions
		/// </summary>
		/// <returns></returns>
		public static string GetTranslatedLoggingParameterMessage() {
			System.Text.StringBuilder stateString = new System.Text.StringBuilder();
			if (Logging.Activities) {
				stateString.Append(KB.K("Activities").Translate());
				stateString.Append(KB.I(", "));
			}
			if (Logging.NotifyRequestor) {
				stateString.Append(KB.K("Notify Requestor").Translate());
				stateString.Append(KB.I(", "));
			}
			if (Logging.NotifyAssignee) {
				stateString.Append(KB.K("Notify Assignee").Translate());
				stateString.Append(KB.I(", "));
			}
			if (Logging.ReadEmailRequest) {
				stateString.Append(KB.K("Read Email Request").Translate());
				stateString.Append(KB.I(", "));
			}
			if (stateString.Length > 0)
				stateString.Remove(stateString.Length - 2, 2); // remove the trailing ", " that will exist
			else
				stateString.Append(KB.K("None").Translate());
			return stateString.ToString();
		}
	}
	public struct LogMessage {
        public readonly Database.DatabaseEnums.ServiceLogEntryType Type;
		public readonly string Source;
		public readonly string Message;
        public LogMessage(Database.DatabaseEnums.ServiceLogEntryType type, string source, string msg)
        {
			Type = type;
			Source = source;
			Message = msg;
		}
	}
}
