using System;
using System.Net.Mail;
using Thinkage.Libraries.Tcpip;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// A wrapper for sending emails by SMTP that gets its configuration from the dsMB variable structure for MainBoss Service
	/// </summary>
	public class SMTPClient : IDisposable {
		private Thinkage.Libraries.Tcpip.SmtpClient smtp = null;
		private readonly MailAddress SendingFromAddress;
		/// <summary>
		/// True if we return a non-null HtmlEmailNotification builder
		/// </summary>
		private readonly bool HtmlBody;

		public string Host {
			get {
				return smtp.Host;
			}
		}
		public int Port {
			get {
				return smtp.Port;
			}
		}
		public static string CrLf = "\r\n";// this is the proper string for a new line in an rfc-2822 Email message; Can't rely on Environment.NewLine

		public MessageExceptionContext SmtpServerContext {
			get {
				return new MessageExceptionContext(KB.K("SMTP Server '{0}' on port {1}"), Host, Port);
			}
		}

		public SMTPClient(MainBossServiceConfiguration config, bool htmlEmail, MailAddress sendFromAddress) {
			HtmlBody = htmlEmail;
			if (String.IsNullOrEmpty(config.SMTPServer))
				throw new MainBossServiceWorkerException(KB.K("The SMTP server parameter is not set."));
			SendingFromAddress = sendFromAddress;

			smtp = new Libraries.Tcpip.SmtpClient(config.SMTPServer) {
				Port = config.SMTPPort,
				EnableSsl = config.SMTPUseSSL
			};
			switch ((DatabaseEnums.SMTPCredentialType)config.SMTPCredentialType) {
				case DatabaseEnums.SMTPCredentialType.DEFAULT:
					smtp.UseDefaultCredentials = true;
					break;
				case DatabaseEnums.SMTPCredentialType.CUSTOM:
					smtp.Credentials = new System.Net.NetworkCredential(
						config.SMTPUserName ?? string.Empty,
						config.SMTPEncryptedPassword == null ? string.Empty : ServicePassword.Decode(config.SMTPEncryptedPassword),
						config.SMTPUserDomain ?? string.Empty
						);
					break;
				default:
					smtp.UseDefaultCredentials = false;
					break;
			}
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		// Callers of this function are expected to use using(xxxx = NewMailMessage)
		public MailMessage NewMailMessage(MailAddress toAddress) {
			MailMessage mm = new MailMessage(SendingFromAddress, toAddress);
			// Add a message-id to guarantee that a proper message-id tag is associated with the message.
			// MS Exchange 2007 sticks a non-conforming (depends on how you interpret the RFCs) message-id tag onto a relayed message if that message doesn't have a message-id tag.
			System.Net.NetworkInformation.IPGlobalProperties ipProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
			string fqdn = Strings.IFormat("{0}.{1}", ipProperties.HostName, ipProperties.DomainName);
			mm.Headers.Add(KB.I(Dart.Mail.HeaderKey.MessageID), string.Format(KB.I("<{0}@{1}>"), DateTime.Now.Ticks.ToString(), fqdn));
			mm.Headers.Add(KB.I(Dart.Mail.HeaderKey.AutoSubmitted), KB.I("auto-generated")); // indicate this was an auto-generated reply
			return mm;
		}
		public void Send(MailMessage msg) {
			smtp.Send(msg);
		}
		public void BuildMailMessageBody(MailMessage msg, INotificationEmail textEmail, INotificationEmail htmlEmail) {
			msg.Body = textEmail.BodyAsString;
			if (htmlEmail != null) {
				AlternateView htmlNotification;
				// now the Html variant
				htmlNotification = AlternateView.CreateAlternateViewFromString(htmlEmail.BodyAsString, null, KB.I("text/html"));
				msg.AlternateViews.Add(htmlNotification);
				msg.IsBodyHtml = true;
			}
		}
		/// <summary>
		/// Return an Html message builder if Html message is turned on. Otherwise return null.
		/// </summary>
		/// <returns></returns>
		public HtmlNotificationEmail GetHtmlEmailBuilder(System.Globalization.CultureInfo preferredLanguage) {
			return HtmlBody ? new HtmlNotificationEmail(preferredLanguage) : null;
		}
		#region IDisposable Members
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
			}
			if (smtp != null) {
				smtp.Dispose();
				smtp = null;
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}