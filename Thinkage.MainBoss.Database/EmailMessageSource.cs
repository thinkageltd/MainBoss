using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text;
using Dart.Mail;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	#region EMailMessageSource
	public class EmailMessageSource : IDisposable {
		#region Dart Adapters
		#region - IMailMessageSource common interface
		public interface IMailMessageSource : IDisposable {
			IEnumerable<EmailMessage> Messages {
				get;
			}
			void Close();
		}
		#endregion
		#region - GetEncrypt - Get the Dart encryption type from ConnectionInformation
		private static Encrypt GetEncrypt(ConnectionInformation connectInfo) {
			if ((connectInfo.ServiceType & ConnectionInformation.ServiceTypes.ImplicitlyEncrypted) != 0)
				return Encrypt.Implicit;
			else if ((connectInfo.ServiceType & ConnectionInformation.ServiceTypes.ExplicitlyEncrypted) != 0)
				return Encrypt.Explicit;
			return Encrypt.None;
		}
		#endregion
		#region - DartPopMessages
		public class DartPopMessages : IMailMessageSource {
			Pop DartPop;
			public DartPopMessages(RemoteCertificateValidationCallback CheckCertificate, ConnectionInformation connectInfo, EventHandler<DataEventArgs> logger) {
				DartPop = new Pop {
					Session = new MailSession {
						Username = connectInfo.User,
						Password = connectInfo.AccessTokenOrPW,
						Security = new MailSecurity {
							TargetHost = connectInfo.Server,
							Encrypt = GetEncrypt(connectInfo),
							ValidationCallback = CheckCertificate,
							//Trying to pass Protocols.None per the MSDN documentation (https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols?view=net-5.0#System_Security_Authentication_SslProtocols_Default)
							// is rejected by
							// System.ArgumentException
							// System.Net.Security.SslState.ValidateCreateContext(Boolean isServer, String targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, Boolean remoteCertRequired, Boolean checkCertRevocationStatus, Boolean checkCertName)
							// in .NET 4.0 at least
							// Default only permits TLS 1.0 and SSL3; we need to enable the others explicitly
							Protocols = System.Security.Authentication.SslProtocols.Ssl3
							| System.Security.Authentication.SslProtocols.Tls11
							| System.Security.Authentication.SslProtocols.Tls12
							| System.Security.Authentication.SslProtocols.Tls
						},
						Authentication = connectInfo.UseOAuth2 ? Authentication.OAuth2 : Authentication.Auto
					}
				};

				try {
					DartPop.Session.RemoteEndPoint = connectInfo.IPEndPoint;
				}
				catch (System.Exception) {
					// This is a bad error message:
					// 1 - It loses the original exception information
					// 2 - Is states that we can't open a connection but we haven't tried that yet (see DartPop.Connect() below)
					// 3 - It is grammatically incorrect
					//
					// We should put the entire new IPEndPoint/Connect/Authenticate in the try block, and just add an exception context to supply some/most of the connectInfo.
					// Better yet, the IncomingEmailServerConnectionInformation object should be able to supply the exception context
					throw new GeneralException(KB.K("Cannot connect to {0}"), connectInfo.FullName);
				}
				DartPop.Connection.Log += logger;
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
		#endregion
		#region - DartImapMessages
		public class DartImapMessages : IMailMessageSource {
			Imap DartImap;
			public DartImapMessages(RemoteCertificateValidationCallback CheckCertificate, ConnectionInformation connectInfo, string mailbox, EventHandler<DataEventArgs> logger) {
				DartImap = new Imap {
					Session = new ImapSession {
						Username = connectInfo.User,
						Password = connectInfo.AccessTokenOrPW,
						Security = new MailSecurity {
							TargetHost = connectInfo.Server,
							Encrypt = GetEncrypt(connectInfo),
							ValidationCallback = CheckCertificate,
							//Trying to pass Protocols.None per the MSDN documentation (https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols?view=net-5.0#System_Security_Authentication_SslProtocols_Default)
							// is rejected by
							// System.ArgumentException
							// System.Net.Security.SslState.ValidateCreateContext(Boolean isServer, String targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, Boolean remoteCertRequired, Boolean checkCertRevocationStatus, Boolean checkCertName)
							// in .NET 4.0 at least
							// Default only permits TLS 1.0 and SSL3; we need to enable the others explicitly
							Protocols = System.Security.Authentication.SslProtocols.Ssl3
										| System.Security.Authentication.SslProtocols.Tls11
										| System.Security.Authentication.SslProtocols.Tls12
										| System.Security.Authentication.SslProtocols.Tls
						},
						Authentication = connectInfo.UseOAuth2 ? Authentication.OAuth2 : Authentication.Auto
					}
				};
				try {
					DartImap.Session.RemoteEndPoint = connectInfo.IPEndPoint;
				}
				catch (System.Exception) {
					// This exception handling is atrocious, see similar in DartPopMessages ctor.
					throw new GeneralException(KB.K("Cannot connect to {0}"), connectInfo.FullName);
				}
				DartImap.Connection.Log += logger;
				DartImap.Connect();
				DartImap.Authenticate();
				GeneralException noMailboxException = null;
				if (!string.IsNullOrWhiteSpace(mailbox)) {
					DartImap.SelectedMailbox = DartImap.Mailboxes[mailbox];
					if (DartImap.SelectedMailbox == null)
						noMailboxException = new GeneralException(KB.K("Mailbox '{0}' does not exist"), mailbox);
				}
				else {
					DartImap.SelectedMailbox = DartImap.Mailboxes["MBOX"] ?? DartImap.Mailboxes["INBOX"] ?? DartImap.Mailboxes["Inbox"];
					if (DartImap.SelectedMailbox == null)
						noMailboxException = new GeneralException(KB.K("No default Mailbox found, tried 'MBOX', 'INBOX' and 'Inbox'"));
				}
				if (noMailboxException != null)
					throw noMailboxException.WithContext(new MessageExceptionContext(KB.K("Available Mailboxes: {0}"), string.Join(", ", DartImap.Mailboxes.Select(e => e.Name))));
			}
			IEnumerable<EmailMessage> pMessages = null;
			public IEnumerable<EmailMessage> Messages {
				get {
					if (pMessages == null && DartImap.SelectedMailbox != null)
						pMessages = DartImap.SelectedMailbox.Select(e => new EmailMessage(e));
					return pMessages;
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
		#endregion
		#region ConnectionInformation
		public struct ConnectionInformation {
			public enum ServiceTypeIndices {
				// These are in order from least to most preferable and this determines the search order when probing.
				POP3,
				IMAP4,
				N_Basic,
				ExplicitEncryptionOffset = N_Basic,
				POP3_TLS = POP3 + ExplicitEncryptionOffset,
				IMAP4_TLS = IMAP4 + ExplicitEncryptionOffset,
				ImplicitEncryptionOffset = 2 * N_Basic,
				POP3S = POP3 + ImplicitEncryptionOffset,
				IMAP4S = IMAP4 + ImplicitEncryptionOffset,

				Count
			}
			[Flags]
			public enum ServiceTypes {
				POP3 = 1 << ServiceTypeIndices.POP3,
				IMAP4 = 1 << ServiceTypeIndices.IMAP4,
				POP3_TLS = 1 << ServiceTypeIndices.POP3_TLS,
				IMAP4_TLS = 1 << ServiceTypeIndices.IMAP4_TLS,
				POP3S = 1 << ServiceTypeIndices.POP3S,
				IMAP4S = 1 << ServiceTypeIndices.IMAP4S,

				Plaintext = POP3 | IMAP4,
				ImplicitlyEncrypted = POP3S | IMAP4S,
				ExplicitlyEncrypted = POP3_TLS | IMAP4_TLS,
				Encrypted = ExplicitlyEncrypted | ImplicitlyEncrypted,

				CanSpecifyMailbox = IMAP4 | IMAP4S | IMAP4_TLS,

				AnyIMAPProtocol = CanSpecifyMailbox,
				AnyPOPProtocol = All & ~AnyIMAPProtocol,

				None = 0,
				Last = IMAP4_TLS,
				All = (1 << Last) - 1
			}
			static readonly int[] ServiceTypeToPort = new[] {
				110,
				143,
				110,
				143,
				995,
				993
			};
			// Most of the information we have is definite.
			// However, pPort can be unset (using zero right now), which means use the standard port for ServiceType.
			// ServiceType can be Any.
			public readonly string Server;
			public readonly ServiceTypes ServiceType;
			private readonly ServiceTypeIndices? ServiceTypeIndex;
			public readonly int Port;
			public readonly string User;
			public readonly string AccessTokenOrPW;
			public readonly bool UseOAuth2;

			// TODO: Encapsulate the port==0 vs using int? for port when port is unspecified
			// TODO: Encapsulate the emptry-string vs null-string duality. We should use null for don't know/unspecified as must as possible
			// TODO: Have a property that provides exception context
			// TODO: Roll the Protocol and useEncryption information into us as well, clean up the partial redundancy between Protocol and Port
			public ConnectionInformation(string server, ServiceTypes serviceType, int port, string user, string accessTokenOrPW, bool useOAuth2) {
				Server = server;
				// Check right away that the server DNS name can be resolved
				try {
					// This will throw an error if there are no A records in DNS.
					if (System.Net.Dns.GetHostAddresses(Server).Length == 0)
						throw new GeneralException(KB.K("Only IPv6 addresses could be found but IPv6 is not enabled on this computer"));
				}
				catch (System.Exception ex) {
					throw new GeneralException(ex, KB.K("Cannot resolve an IP address for Server '{0}'"), Server);
				}
				ServiceType = serviceType;
				if ((serviceType & (serviceType - 1)) == 0) {
					// a single service type is specified, get its index
					// With only 6 values to choose from this loop is simplest.
					int index = (int)ServiceTypeIndices.Count;
					while (--index >= 0 && ((int)serviceType & (1 << index)) == 0) { }
					// If there was a rogue bit or no bits, we set a null index.
					ServiceTypeIndex = index >= 0 ? (ServiceTypeIndices?)index : null;
					Port = port == 0 ? ServiceTypeToPort[(int)ServiceTypeIndex] : port;
				}
				else {
					ServiceTypeIndex = null;
					Port = port;
				}
				User = user;
				AccessTokenOrPW = accessTokenOrPW;
				UseOAuth2 = useOAuth2;
			}
			public ConnectionInformation(ConnectionInformation basis, ServiceTypes serviceType) {
				Server = basis.Server;
				ServiceType = serviceType;
				// in the basis there may have already been a port forced by having a single service selected already
				// If we now allow selection of a different service the original specification of a default port in basis
				// will not be overridden by the new service selection.
				// We get around this by asserting that the new service selection be a subset of the old one.
				System.Diagnostics.Debug.Assert((serviceType & ~basis.ServiceType) == 0);
				if ((serviceType & (serviceType - 1)) == 0) {
					// a single service type is specified, get its index
					// With only 6 values to choose from this loop is simplest.
					int index = (int)ServiceTypeIndices.Count;
					while (--index >= 0 && ((int)serviceType & (1 << index)) == 0) { }
					// If there was a rogue bit or no bits, we set a null index.
					ServiceTypeIndex = index >= 0 ? (ServiceTypeIndices?)index : null;
				}
				else
					ServiceTypeIndex = null;
				Port = basis.Port == 0 ? ServiceTypeToPort[(int)ServiceTypeIndex] : basis.Port;
				User = basis.User;
				AccessTokenOrPW = basis.AccessTokenOrPW;
				UseOAuth2 = basis.UseOAuth2;
			}

			public IPEndPoint IPEndPoint => new IPEndPoint(System.Net.Dns.GetHostAddresses(Server)[0], Port);

			public bool IsSpecific => ServiceTypeIndex.HasValue;
			// [Translated]
			public string ProtocolName =>
				(ServiceType & ServiceTypes.ExplicitlyEncrypted) != 0
					? Strings.IFormat("{0} with TLS", (ServiceTypes)((int)ServiceType >> (int)ServiceTypeIndices.ExplicitEncryptionOffset))
					: ServiceType.ToString();
			// [Translated]
			public string FullName =>
				Strings.Format(KB.K("User '{0}' on server '{1}', port '{2}', using {3}"), User, Server, Port, ProtocolName);
			public IExceptionContext ExceptionContext => new MessageExceptionContext(KB.T(FullName));
			public bool Match(ConnectionInformation pattern) {
				// We return true if our connection information is possible using 'other'
				// Everything must be the same except:
				// Our ServiceType must be a subset of other.ServiceType
				// The tricky one is the port: If both ports are equal it is a match, but we can also have the default port for our protocol and other can be zero.
				if (Server != pattern.Server
					|| User != pattern.User
					|| AccessTokenOrPW != pattern.AccessTokenOrPW
					|| UseOAuth2 != pattern.UseOAuth2)
					return false;
				if ((ServiceType & ~pattern.ServiceType) != 0)
					return false;
				if (Port == pattern.Port)
					return true;
				if (ServiceTypeIndex.HasValue && Port == ServiceTypeToPort[(int)ServiceTypeIndex.Value]
					&& !pattern.ServiceTypeIndex.HasValue && pattern.Port == 0)
					// The port we are using is the default for the protocol, and other is not specifying a port
					return true;
				return false;
			}
		}
		#endregion
		public string Protocol => LastConnectInfo.Value.ProtocolName;
		public int Port => LastConnectInfo.Value.Port;
		IMailMessageSource Source;
		bool TraceDetails;
		IServiceLogging Logger;
		// This is the last ConnectionInformation that successfully connected.
		// If Source != null this is also the info for that Source.
		static ConnectionInformation? LastConnectInfo = null;
		#region Construction
		#region - Constructor
		public EmailMessageSource(IServiceLogging logger, bool traceDetails, ConnectionInformation connectInfo, string mailbox, bool requireCertificate) {
			// What is the following even doing here??????????????
			Attachment.Directory = Path.GetTempPath();
			var path = Path.Combine(Attachment.Directory, Path.GetRandomFileName());
			try {
				// TODO this code will give an error and stop the processing, but I am not sure what the customer should do if it gets the error, 
				// the only fix is to manual construct the directories with the right permissions.
				Directory.CreateDirectory(Attachment.Directory); // they should be there and available, but if not the error message will be better
				using (File.Create(path)) { }
				File.Delete(path);
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("The Windows System supplied directory '{0}' acquired from System.IO.Path.GetTempPath is not usable"), Attachment.Directory);
			}
			// end of what is this doing here?????????????

			TraceDetails = traceDetails;
			Logger = logger;
			// if the user want one type try that only
			if (connectInfo.IsSpecific) {
				tryOneMailSource(connectInfo, mailbox, requireCertificate, false, out bool _);
				return;
			}
			// The user has not specified a connection type. If the current connection info matches the most recent
			// succesful connection try that again.
			if (LastConnectInfo.HasValue && LastConnectInfo.Value.Match(connectInfo))
				try {
					tryOneMailSource(LastConnectInfo.Value, mailbox, requireCertificate, false, out bool _);
					return;
				}
				catch (System.Exception) {
					// We ignore the exception here, it should happen again when we try FindMessageSource
					// Note that this may not be true if it is a transient exception.
				}

			// Search from scratch through all server types
			List<System.Exception> errors = new List<System.Exception>();

			// try all the connection types in typesToTry.
			// We only throw an exception if they all fail, otherwise we just log the successful one.
			// If a site wants to diagnose why they are not getting a particular higher-priority protocol they can name it specifically and see what error occurs.
			for (int index = (int)ConnectionInformation.ServiceTypeIndices.Count; --index >= 0;) {
				if (((int)connectInfo.ServiceType & (1 << index)) == 0)
					continue;
				var specificConnectionInfo = new ConnectionInformation(connectInfo, (ConnectionInformation.ServiceTypes)(1 << index));
				bool tryagain = true;
				try {
					tryOneMailSource(specificConnectionInfo, mailbox, requireCertificate, true, out tryagain);
					return;
				}
				catch (System.Exception ex) {
					errors.Add(ex);
					if (!tryagain)
						break;
					continue;
				}
			}
			if (errors.Count == 1)
				throw errors[0];
			var err = new GeneralException(KB.K("Cannot connect to incoming mail server. The following was tried:"), connectInfo.Server).WithContext(TraceContext);
			foreach (var e in errors)
				err = err.WithContext(new Thinkage.Libraries.MessageExceptionContext(KB.T(Thinkage.Libraries.Exception.FullMessage(e))));
			throw err;
		}
		private void tryOneMailSource(ConnectionInformation connectInfo, string mailbox, bool requireCertificate, bool traceSuccess, out bool tryAnotherProtocol) {
			// This always throws an exception on failure. For all expected exceptions we add the connect info context.
			// Otherwise it sets Source and LastConnectInfo. The latter is static and is used on subsequent Email MessageSource ctor calls when a non-specific
			// connectInfo is provided. It can also be used to get identification information for Source.
			Source = null;
			tryAnotherProtocol = false;
			try {
				RemoteCertificateValidationCallback checker
					= (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => CheckCertificate(sender, certificate, chain, sslPolicyErrors, requireCertificate);
				if ((connectInfo.ServiceType & ConnectionInformation.ServiceTypes.AnyIMAPProtocol) == 0)
					Source = new DartPopMessages(checker, connectInfo, Connection_Log);
				else
					Source = new DartImapMessages(checker, connectInfo, mailbox, Connection_Log);
				if (traceSuccess || TraceDetails) {
					// We build but never throw an Exception to get the formatting of exception contexts
					var sb = new StringBuilder();
					Strings.Append(sb, KB.K("Successfully authenticated with {0}"), connectInfo.FullName);
					if (!string.IsNullOrWhiteSpace(mailbox))
						Strings.Append(sb, KB.K(", mailbox '{0}'"), mailbox);
					if (TraceDetails)
						Logger.LogTrace(true, Thinkage.Libraries.Exception.FullMessage(new GeneralException(KB.T(sb.ToString())).WithContext(TraceContext)));
					else
						Logger.LogInfo(sb.ToString());
				}
				LastConnectInfo = connectInfo;
			}
			catch (Libraries.Exception e) {
				e.WithContext(connectInfo.ExceptionContext);
				throw;
			}
			catch (Dart.Mail.ProtocolException e) {
				Libraries.Exception te;
				// This could be an unaccepted protocol or an authorization failure.
				// In the former case we want to indicate that a different protocol might work.
				// In the latter case we prefer not to do this but it is not particularly harmful if we do.
				// This check is heuristic because other than the actual basic response ("-ERR" in POP, "NO" in IMAP)
				// the rest of the message is free-form so these checks only really work for particular servers set up in English.
				// The IMAP responses start with a tag which was applied by our client to the command; in this case the authorization
				// command is tagged A2 in POP3/POP3S because it is the second command issued, and A4 in POP3/TLS because of the
				// extra commands in TLS setup
				if (e.Message.Contains(KB.I("-ERR [AUTH] Authentication failed"))                    // pop
					|| e.Message.Contains(KB.I("[AUTHENTICATIONFAILED] Authentication failed"))       // imap
					|| e.Message.Contains(KB.I("A2 NO AUTHENTICATE failed"))                          // imap
					|| e.Message.Contains(KB.I("A4 NO AUTHENTICATE failed"))                          // imap
					) {
					te = new GeneralException(e, KB.K("Cannot authenticate user")).WithContext(connectInfo.ExceptionContext);
					if (TraceDetails)
						Libraries.Exception.AddContext(te, TraceContext);
				}
				else {
					tryAnotherProtocol = true;
					te = new GeneralException(e, KB.K("Cannot access Mail Server")).WithContext(connectInfo.ExceptionContext).WithContext(TraceContext);
				}
				throw te;
			}
			catch (System.Net.Sockets.SocketException e) {
				Libraries.Exception ge = new GeneralException(e, KB.K("Cannot access Mail Server")).WithContext(connectInfo.ExceptionContext)
					.WithContext(TraceContext);
				if (TraceDetails)
					Logger.LogTrace(true, Thinkage.Libraries.Exception.FullMessage(ge));
				throw e;
			}
		}
		#endregion
		#region - Certificate checking and logging
		static private string LastCertificate;
		public bool CheckCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, bool requireCertificate) {
			var newCertificateAsString = certificate.ToString(false);
			var newCertificate = LastCertificate != newCertificateAsString;
			LastCertificate = newCertificateAsString;
			if (sslPolicyErrors == SslPolicyErrors.None) {
				if (newCertificate)
					Logger.LogTrace(TraceDetails, certificateMessage(KB.K("Valid Certificate '{0}' found"), certificate.Subject, sslPolicyErrors, newCertificateAsString));
				return true;
			}
			if (requireCertificate) {
				if (newCertificate)
					Logger.LogError(certificateMessage(KB.K("Certificate '{0}' had error {1}"), certificate.Subject, sslPolicyErrors, newCertificateAsString));
				return false;
			}
			if (newCertificate)
				if (TraceDetails)
					Logger.LogInfo(certificateMessage(KB.K("Certificate '{0}' had error {1}"), certificate.Subject, sslPolicyErrors, newCertificateAsString));
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
		#endregion
		#endregion
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
		#region Trace (a log of the protocol interactions)
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
		private StringBuilder log = null;
		private void Connection_Log(object sender, DataEventArgs e) {
			if (log == null)
				log = new StringBuilder();
			log.Append(e.Data.Direction == DataDirection.In ? KB.I("<--- ") : KB.I("---> "));
			for (var i = 0; i < e.Data.Count; ++i) {
				log.Append((char)e.Data.Buffer[i]);
			}
		}
		public MessageExceptionContext TraceContext => TraceLength == 0 ? null : new MessageExceptionContext(KB.T(Trace()));
		#endregion
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
