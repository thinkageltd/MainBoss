using Thinkage.Libraries.Service;
using System.Net.Mail;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Provides base for sending email notifications
	/// </summary>
	public class EmailNotificationProcessor : EmailProcessor {
		protected SMTPClient smtp = null;
		public bool Unavailable = true; // cannot send messages, can be fixed by changes in the configuration record
		#region Constructors, Destructors
		public EmailNotificationProcessor(IServiceLogging serviceBase, MB3Client dbClient)	: base(serviceBase, dbClient) {
			if (!ServiceConfiguration.ProcessNotificationEmail) {
				if( oldProcessNotificationEmail != ServiceConfiguration.ProcessNotificationEmail )
					Logger.LogInfo(KB.K("The Outgoing Email Notification service has been disabled").Translate());
				oldProcessNotificationEmail = ServiceConfiguration.ProcessNotificationEmail;
				return;
			}
			if (ServiceConfiguration.SMTPServer == null || ServiceConfiguration.ReturnEmailAddress == null) {
				if (oldConfigured != (ServiceConfiguration.SMTPServer == null || ServiceConfiguration.ReturnEmailAddress == null)) {
					if (ServiceConfiguration.SMTPServer == null)
						Logger.LogError(Libraries.Strings.Format(KB.K("The Outgoing Email Notification service requires a '{0}' to be configured"), dsMB.Schema.T.ServiceConfiguration.F.SMTPServer.LabelKey.Translate()));
					if (ServiceConfiguration.ReturnEmailAddress == null)
						Logger.LogError(Libraries.Strings.Format(KB.K("The Outgoing Email Notification service requires a '{0}' to be configured"), dsMB.Schema.T.ServiceConfiguration.F.ReturnEmailAddress.LabelKey.Translate()));
				}
				oldConfigured = (ServiceConfiguration.SMTPServer == null || ServiceConfiguration.ReturnEmailAddress == null);
				return;
			}
			smtp = new SMTPClient(ServiceConfiguration, ServiceConfiguration.HtmlEmailNotification, new MailAddress(ServiceConfiguration.ReturnEmailAddress, ServiceConfiguration.ReturnEmailDisplayName));
			Unavailable = false;
		}
		static bool? oldProcessNotificationEmail = null;
		static bool? oldConfigured = null;
		#endregion
		#region IDisposable Members
		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if (disposing) {
				if (smtp != null) {
					smtp.Dispose();
					smtp = null;
				}
			}
		}
		#endregion
	}
}