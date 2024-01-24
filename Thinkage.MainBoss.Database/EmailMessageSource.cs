using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dart.Mail;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	public interface IMailMessageSource : IDisposable {
		IEnumerable<EmailMessage> Messages {
			get;
		}
		int Port {
			get;
		}
		string Protocol {
			get;
		}
		void Close();
	}
	#region Dart Adapters
	public class DartPopMessages : IMailMessageSource {
		Pop DartPop;
		public DartPopMessages(Encrypt tryingEncryption, RemoteCertificateValidationCallback CheckCertificate, string server, int port, string user, string pw) {
			DartPop = new Pop();
			DartPop.Session = new MailSession();
			DartPop.Session.Username = user;
			DartPop.Session.Password = pw;
			DartPop.Session.Security = new MailSecurity();
			DartPop.Session.Security.TargetHost = server;
			DartPop.Session.Security.Encrypt = tryingEncryption;
			DartPop.Session.Security.ValidationCallback += CheckCertificate;
			try {
				DartPop.Session.RemoteEndPoint = new IPEndPoint(System.Net.Dns.GetHostAddresses(server)[0], port);
			}
			catch (System.Exception) {
				throw new GeneralException(KB.K("Cannot open a connect to server '{0}' on port {1} using {2}"), server, port, Protocol);
			}
			DartPop.Connection.Log += EmailMessageSource.Connection_Log;
			DartPop.Connect();
			DartPop.Authenticate(true, true);
		}
		IEnumerable<EmailMessage> pMessages = null;
		public IEnumerable<EmailMessage> Messages {
			get {
				if (pMessages == null)
					pMessages = DartPop.Messages.Select(e => new EmailMessage(e));
				return pMessages;
			}
		}
		public int Port {
			get {
				return DartPop?.Session.RemoteEndPoint.Port ?? 0;
			}
		}
		public string Protocol {
			[Invariant]
			get {
				if (DartPop == null || DartPop.Session == null || DartPop.Session.Security == null)
					return "POP3";
				switch (DartPop.Session.Security.Encrypt) {
				case Encrypt.Implicit:
					return "POP3S";
				case Encrypt.Explicit:
					return Strings.Format(Thinkage.Libraries.Application.InstanceCultureInfo, KB.K("{0} with {1}"), KB.I("POP3"), KB.I("TLS"));
				case Encrypt.None:
					return Strings.Format(Thinkage.Libraries.Application.InstanceCultureInfo, KB.K("{0} plain text"), KB.I("POP3"));
				default:
					return Strings.Format(Thinkage.Libraries.Application.InstanceCultureInfo, KB.K("{0} with unknown security"), KB.I("POP3"));
				}
			}
		}
		public void Close() {
			DartPop.Close();
		}
		#region IDisposable Members
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				pMessages = null;
				if (DartPop != null)
					DartPop.Dispose();
			}
		}
		#endregion
	}
	public class DartImapMessages : IMailMessageSource {
		Imap DartImap;
		public DartImapMessages(Encrypt tryingEncryption, RemoteCertificateValidationCallback CheckCertificate, string server, int port, string user, string pw, string mailbox) {
			DartImap = new Imap();
			DartImap.Session = new ImapSession();
			DartImap.Session.Username = user;
			DartImap.Session.Password = pw;
			DartImap.Session.Security = new MailSecurity();
			DartImap.Session.Security.TargetHost = server;
			DartImap.Session.Security.Encrypt = tryingEncryption;
			DartImap.Session.Security.ValidationCallback += CheckCertificate;
			try {
				DartImap.Session.RemoteEndPoint = new IPEndPoint(System.Net.Dns.GetHostAddresses(server)[0], port);
			}
			catch (System.Exception) {
				throw new GeneralException(KB.K("Cannot open a connect to server '{0}' on port {1} using {2}"), server, port, Protocol);
			}
			DartImap.Connection.Log += EmailMessageSource.Connection_Log;
			DartImap.Connect();
			DartImap.Authenticate();
			if (mailbox != null) {
				DartImap.SelectedMailbox = DartImap.Mailboxes[mailbox];
				if (DartImap.SelectedMailbox == null)
					throw new GeneralException(KB.K("Mailbox '{0}' does not exist on server '{1}' on port {2} using {3}"), mailbox, server, port, Protocol);
			}
			else {
				DartImap.SelectedMailbox = DartImap.Mailboxes["MBOX"] ?? DartImap.Mailboxes["INBOX"];
				if (DartImap.SelectedMailbox == null)
					throw new GeneralException(KB.K("No incoming Mailbox found on server '{0}' on port {1} using {2}, tried 'MBOX' and 'INBOX'"), server, port, Protocol);
			}
		}
		IEnumerable<EmailMessage> pMessages = null;
		public IEnumerable<EmailMessage> Messages {
			get {
				if (pMessages == null && DartImap.SelectedMailbox != null)
					pMessages = DartImap.SelectedMailbox.Select(e => new EmailMessage(e));
				return pMessages;
			}
		}

		public int Port {
			get {
				return DartImap?.Session.RemoteEndPoint.Port ?? 0;
			}
		}
		public string Protocol {
			[Invariant]
			get {
				if (DartImap == null || DartImap.Session == null || DartImap.Session.Security == null)
					return "IMAP4";
				switch (DartImap.Session.Security.Encrypt) {
				case Encrypt.Implicit:
					return "IMAP4S";
				case Encrypt.Explicit:
					return Strings.Format(Thinkage.Libraries.Application.InstanceCultureInfo, KB.K("{0} with {1}"), KB.I("IMAP4"), KB.I("TLS"));
				case Encrypt.None:
					return Strings.Format(Thinkage.Libraries.Application.InstanceCultureInfo, KB.K("{0} plain text"), KB.I("IMAP4"));
				default:
					return Strings.Format(Thinkage.Libraries.Application.InstanceCultureInfo, KB.K("{0} with unknown security"), KB.I("IMAP4"));
				}
			}
		}

		public void Close() {
			DartImap.Close();
		}
		#region IDisposable Members
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (DartImap != null)
					DartImap.Dispose();
			}
		}
		#endregion
	}
	#endregion
	#region EMailMessageSource
	public class EmailMessageSource : IDisposable {
		IMailMessageSource Source;
		System.Exception Error;
		bool TraceDetails;
		IServiceLogging Logger;
		DatabaseEnums.MailServerType ServiceType;
		static DatabaseEnums.MailServerType LastServiceType;
		static DatabaseEnums.MailServerEncryption LastRequestedEncryption;
		static int LastPort = 0;
		bool LastEncrypt;
		DatabaseEnums.MailServerEncryption RequestedEncryption;
		static readonly Dictionary<DatabaseEnums.MailServerType, int> ServiceTypeToPort = new Dictionary<DatabaseEnums.MailServerType, int> {
			{ DatabaseEnums.MailServerType.POP3,   110 },
			{ DatabaseEnums.MailServerType.IMAP4,  143 },
			{ DatabaseEnums.MailServerType.POP3S,  995 },
			{ DatabaseEnums.MailServerType.IMAP4S, 993 },
		};
		static readonly Dictionary<DatabaseEnums.MailServerType, Encrypt> ServiceTypeToEncypt = new Dictionary<DatabaseEnums.MailServerType, Encrypt> {
			{ DatabaseEnums.MailServerType.POP3,   Encrypt.Explicit },
			{ DatabaseEnums.MailServerType.IMAP4,  Encrypt.Explicit },
			{ DatabaseEnums.MailServerType.POP3S,  Encrypt.Implicit },
			{ DatabaseEnums.MailServerType.IMAP4S, Encrypt.Implicit },
		};
		public EmailMessageSource(IServiceLogging logger, bool traceDetails, DatabaseEnums.MailServerType serviceType, DatabaseEnums.MailServerEncryption requestedEncryption, string server, int port, string user, string pw, string mailbox) {
			RequestedEncryption = requestedEncryption;
			this.ServiceType = serviceType;
			TraceDetails = traceDetails;
			Logger = logger;
			// if the user want one type try that
			if (serviceType != DatabaseEnums.MailServerType.Any) {
				if (port == 0)
					port = ServiceTypeToPort[serviceType];
				bool encrypt = requestedEncryption == DatabaseEnums.MailServerEncryption.None ? false : true;
				bool tryagain = tryOneMailSource(encrypt, serviceType, server, port, user, pw, mailbox);
				if (tryagain && requestedEncryption == DatabaseEnums.MailServerEncryption.AnyAvailable && (serviceType == DatabaseEnums.MailServerType.POP3 || serviceType == DatabaseEnums.MailServerType.IMAP4)) {
					encrypt = false;
					tryOneMailSource(encrypt, serviceType, server, port, user, pw, mailbox);
				}
				if (Source == null)
					throw Error ?? (string.IsNullOrWhiteSpace(mailbox) ? new GeneralException(KB.K("Cannot access mail messages using {0} on server '{1}' on port {2}"), serviceType, server, port).WithContext(TraceContext(this))
							: new GeneralException(KB.K("Cannot access mail messages from mailbox '{0}' using {1} on server '{2}' on port {3}"), mailbox, serviceType, server, port).WithContext(TraceContext(this)));
				LastPort = port;
				LastServiceType = serviceType;
				LastEncrypt = encrypt;
				return;
			}
			if (LastPort != 0  // we were successful the last time and our environment didn't change
				&& (port == 0 || LastPort == port)
				&& (this.ServiceType == DatabaseEnums.MailServerType.Any || this.ServiceType == LastServiceType)
				&& (LastRequestedEncryption == requestedEncryption)) {
				tryOneMailSource(LastEncrypt, LastServiceType, server, LastPort, user, pw, mailbox);
				if (Source != null)
					return;
			}
			LastPort = 0;
			FindMessageSource(server, port, user, pw, mailbox);
			if (Source == null)
				throw Error ?? new GeneralException(KB.K("Cannot access mail messages on server '{0}'"), server).WithContext(TraceContext(this));
			if (string.IsNullOrWhiteSpace(mailbox))
				logger.LogInfo(Strings.Format(KB.K("Using {0} server '{1}' on port {2}"), Protocol, server, LastPort));
			else
				logger.LogInfo(Strings.Format(KB.K("Using {0} with mailbox '{0}' server '{2}' on port {3}"), Protocol, mailbox, server, LastPort));
		}
		private void FindMessageSource(string server, int port, string user, string pw, string mailbox) {
			List<System.Exception> errors = new List<System.Exception>();
			Attachment.Directory = System.IO.Path.GetTempPath();
			try {
				System.Net.Dns.GetHostAddresses(server); // will throw an error if machine does not exists, don't want to retry if there is no hope
			}
			catch (System.Exception) {
				throw new GeneralException(KB.K("Cannot find an IP address for Computer '{0}'"), server);
			}
			bool tryagain = true;
			bool usingEncryption = true;
			int tryPort = port;
			DatabaseEnums.MailServerType serviceUsed = DatabaseEnums.MailServerType.Any;
			DatabaseEnums.MailServerType[] mailServerType = new DatabaseEnums.MailServerType[] { DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerType.IMAP4S };
			if (!string.IsNullOrWhiteSpace(mailbox)) // if the user supplied a mailbox it must be IMAP
				mailServerType = new DatabaseEnums.MailServerType[] { DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerType.IMAP4S };
			// try with encryption
			foreach (var s in mailServerType) {
				serviceUsed = s;
				tryPort = port != 0 ? port : ServiceTypeToPort[s];
				Error = null;
				tryagain = tryOneMailSource(usingEncryption, s, server, tryPort, user, pw, mailbox);
				if (Error != null)
					errors.Add(Error);
				if (Source != null || !tryagain)
					break;
			}
			// try without Encryption
			mailServerType = mailbox == null ? new DatabaseEnums.MailServerType[] { DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerType.IMAP4 } : new DatabaseEnums.MailServerType[] { DatabaseEnums.MailServerType.IMAP4 };
			if (Source != null && tryagain && RequestedEncryption == DatabaseEnums.MailServerEncryption.AnyAvailable) {
				usingEncryption = false;
				foreach (var s in mailServerType) {
					serviceUsed = s;
					tryPort = port != 0 ? port : ServiceTypeToPort[s];
					Error = null;
					tryagain = tryOneMailSource(usingEncryption, s, server, tryPort, user, pw, mailbox);
					if (Error != null)
						errors.Add(Error);
					if (Source != null || !tryagain)
						break;
				}
			}
			if (Source != null) {
				LastPort = tryPort;
				LastServiceType = serviceUsed;
				LastRequestedEncryption = RequestedEncryption;
				LastEncrypt = usingEncryption;
				return;
			}
			if (errors.Count == 1)
				throw Error;
			var err = new GeneralException(KB.K("Cannot access mail messages on server '{0}' the following was tried:"), server).WithContext(TraceContext(this));
			foreach (var e in errors)
				err = err.WithContext(new Thinkage.Libraries.MessageExceptionContext(KB.T(Thinkage.Libraries.Exception.FullMessage(e))));
			Error = err;
			throw Error;
		}
		private bool tryOneMailSource(bool usingEncryption, DatabaseEnums.MailServerType serviceType, string server, int port, string user, string pw, string mailbox) {
			Source = null;
			Encrypt encryption = usingEncryption ? ServiceTypeToEncypt[serviceType] : Encrypt.None;
			try {
				switch (serviceType) {
				case DatabaseEnums.MailServerType.POP3:
				case DatabaseEnums.MailServerType.POP3S:
					Source = new DartPopMessages(encryption, CheckCertificate, server, port, user, pw);
					break;
				case DatabaseEnums.MailServerType.IMAP4:
				case DatabaseEnums.MailServerType.IMAP4S:
					Source = new DartImapMessages(encryption, CheckCertificate, server, port, user, pw, mailbox);
					break;
				}
				if (Source == null)
					LogTrace(Logger, TraceDetails, KB.K("Tried"), server, port, Protocol, mailbox, this);
				else
					LogTrace(Logger, TraceDetails, KB.K("Successfully authenticated with"), server, port, Protocol, mailbox, this);
			}
			catch (GeneralException e) {
				Error = e;
				return false;
			}
			catch (Dart.Mail.ProtocolException e) {
				if (e.Message.Contains(KB.I(" -ERR [AUTH] Authentication failed"))                    // pop
					|| e.Message.Contains(KB.I("[AUTHENTICATIONFAILED] Authentication failed"))       // imap
					|| e.Message.Contains(KB.I("A4 NO AUTHENTICATE failed"))                          // imap
					) { // heuristic but it will not matter if it fails, the failure will just clutter the logs.
					if (TraceDetails)
						Error = new GeneralException(KB.K("Cannot authenticate user '{0}' on server '{1}'"), user, server).WithContext(TraceContext(this));
					else
						Error = new GeneralException(KB.K("Cannot authenticate user '{0}' on server '{1}'"), user, server);
					return false;
				}
				Error = new GeneralException(e, KB.K("Cannot access Mail Server on server '{0}' on port {1} using {2}"), server, port, SourceType(Source, serviceType, usingEncryption)).WithContext(TraceContext(this));
				return true;
			}
			catch (System.Net.Sockets.SocketException e) {
				var p = SourceType(Source, serviceType, usingEncryption);
				GeneralException ge;
				if (port != 0)
					ge = new GeneralException(e, KB.K("Cannot access Mail Server on server '{0}' on port {1} using {2}"), server, port, p);
				else
					ge = new GeneralException(e, KB.K("Cannot access Mail Server on server '{0}' using {1}"), server, p);
				Error = ge.WithContext(TraceContext(this));
				if (TraceDetails)
					Logger.LogTrace(true, Thinkage.Libraries.Exception.FullMessage(Error));
				return true;
			}
			return Source == null;
		}
		public void Close() {
			Source.Close();
		}
		public IEnumerable<EmailMessage> Messages {
			get {
				if (Source != null)
					return Source.Messages;
				throw new GeneralException(KB.K("No email message source"));
			}
		}
		public string Trace(bool forceFullTrace = false) {
			if (log == null || log.Length == 0)
				return string.Empty;
			StringBuilder l = new StringBuilder();
			l.AppendLine();
			l.AppendLine(Strings.Format(KB.K("Mail Server Communication Log:")));
			if (log.Length < 11000 || TraceDetails || forceFullTrace) {
				l.Append(log.ToString());
			}
			else {
				l.AppendLine(log.ToString().Substring(0, 10000));
				l.Append(Environment.NewLine);
				l.Append(KB.I("...    "));
				l.Append(Strings.Format(KB.K("Content Deleted")));
				l.AppendLine(KB.I("    ..."));
				l.AppendLine();
				l.Append(log.ToString().Substring(log.Length - 1000));
			}
			log = null;
			return l.ToString();
		}
		public int TraceLength {
			get {
				return log?.Length ?? 0;
			}
		}
		public void TraceClear() {
			log = null;
		}
		static private string CertificateCache;
		public bool CheckCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			var newCertificateAsString = certificate.ToString(false);
			var newCertificate = CertificateCache != newCertificateAsString;
			CertificateCache = newCertificateAsString;
			if (sslPolicyErrors == SslPolicyErrors.None) {
				if (newCertificate)
					Logger.LogTrace(TraceDetails, certificateMessage(KB.K("Valid Certificate '{0}' found"), certificate.Subject, sslPolicyErrors, CertificateCache));
				return true;
			}
			if (RequestedEncryption == DatabaseEnums.MailServerEncryption.RequireValidCertificate) {
				if (newCertificate)
					Logger.LogError(certificateMessage(KB.K("Certificate '{0}' had error {1}"), certificate.Subject, sslPolicyErrors, CertificateCache));
				return false;
			}
			if (newCertificate)
				if (TraceDetails)
					Logger.LogInfo(certificateMessage(KB.K("Certificate '{0}' had error {1}"), certificate.Subject, sslPolicyErrors, CertificateCache));
				else
					Logger.LogInfo(Strings.Format(KB.K("Certificate '{0}' had error {1}"), certificate.Subject, sslPolicyErrors));
			return true;
		}

		private string certificateMessage(Key format, string subject, SslPolicyErrors policyErrors, string certificateText) {
			var s = new StringBuilder();
			s.AppendLine(Strings.Format(format, subject, policyErrors.ToString()));
			s.AppendLine(Strings.Format(KB.K("Details:")));
			s.Append(certificateText);
			return s.ToString();
		}
		public string Protocol {
			get {
				if (Source != null)
					return Source.Protocol;
				return ServiceType.ToString();
			}
		}
		public string SourceType(IMailMessageSource source, DatabaseEnums.MailServerType servicetype, bool encrypt) {
			if (source != null)
				return Source.Protocol;
			if ((servicetype == DatabaseEnums.MailServerType.POP3 || servicetype == DatabaseEnums.MailServerType.IMAP4) && encrypt)
				return Strings.IFormat("{0} with TLS", servicetype);
			return servicetype.ToString();
		}
		public int Port => LastPort;
		static StringBuilder log = null;
		public static void Connection_Log(object sender, DataEventArgs e) {
			if (log == null)
				log = new StringBuilder();
			log.Append(e.Data.Direction == DataDirection.In ? KB.I("<--- ") : KB.I("---> "));
			for (var i = 0; i < e.Data.Count; ++i) {
				log.Append((char)e.Data.Buffer[i]);
			}
		}
		private static void LogTrace(IServiceLogging logger, bool traceDetails, Key prefix, string server, int port, string protocol, string mailbox, EmailMessageSource source) {
			if (!traceDetails)
				return;
			if (string.IsNullOrWhiteSpace(mailbox))
				logger.LogTrace(traceDetails, Thinkage.Libraries.Exception.FullMessage(new GeneralException(KB.K("{0} server '{1}' on port {2} using {3}"), prefix, server, port, protocol).WithContext(TraceContext(source))));
			else
				logger.LogTrace(traceDetails, Thinkage.Libraries.Exception.FullMessage(new GeneralException(KB.K("{0} mailbox '{1}' server '{2}' on port {3} using {4}"), prefix, mailbox, server, port, protocol).WithContext(TraceContext(source))));
		}
		public static MessageExceptionContext TraceContext(EmailMessageSource messageSource) {
			if (messageSource == null || messageSource.TraceLength == 0)
				return null;
			return new MessageExceptionContext(KB.T(messageSource.Trace()));
		}
		#region IDisposable Members
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (Source != null)
					Source.Dispose();
			}
		}
		#endregion
	}
	#endregion


}
