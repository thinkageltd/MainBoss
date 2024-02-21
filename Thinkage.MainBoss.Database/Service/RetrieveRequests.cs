#define DEBUGGINGLEAVESMESSAGESBEHIND
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {

	public class RetrieveRequests : EmailProcessor {
		#region Constructors, Destructor
		public RetrieveRequests(IServiceLogging logger, MB3Client dbSession, bool force)
			: base(logger, dbSession) {
			Force = force;
			dsmb.EnsureDataTableExists(dsMB.Schema.T.EmailRequest, dsMB.Schema.T.EmailPart);
		}
		#endregion
		#region Properties and Members
		private readonly bool Force;
		#endregion
		#region Run - GetEmailRequests
		/// <summary>
		/// Retrieves mail, creates work requests for valid mail-requests and put them in the user-defined state that is specified by XXX.
		/// An erequest record is created for each email regardless of validity.
		/// </summary>
		public void Run(bool traceDetails) {
			if (string.IsNullOrEmpty(ServiceConfiguration.MailServer))
				return;
			var encryption = (DatabaseEnums.MailServerEncryption)ServiceConfiguration.Encryption;
			var mailbox = ServiceConfiguration.MailboxName ?? string.Empty;
			bool useOAuth2 = (DatabaseEnums.MailServerAuthentication)ServiceConfiguration.MailAuthenticationType == DatabaseEnums.MailServerAuthentication.OAuth2;
			EmailMessageSource.ConnectionInformation connectInfo;
			if (useOAuth2) {
				if (ServiceConfiguration.MailClientCertificateName != null && ServiceConfiguration.MailEncryptedClientSecret != null)
					throw new GeneralException(KB.K("Configuration record contains both a client certificate name and a client secret"));
				// Woth Dart the Access Token is passed as a password.

				// This currently assumes only Azure (office 365). Some provision possibly required for GMAIL as well ?
				// Would this have to be another field in the record to id the OAuth provider type or can we just extend the enum type to have a separate
				// value for each supported? Is there any way of telling from the email address?

				// Do we need to get a new access token on every Run call? Can we cache the last one,
				// check if it has expired, and renew it or replace it?
				if (ServiceConfiguration.MailClientCertificateName != null)
					connectInfo = new EmailMessageSource.ConnectionInformation(ServiceConfiguration.MailServer,
						CalculateServiceTypes((DatabaseEnums.MailServerType)ServiceConfiguration.MailServerType, encryption, mailbox), ServiceConfiguration.MailPort ?? 0,
						ServiceConfiguration.MailUserName, ServiceConfiguration.MailClientID, OAuth2ManagerBase.GetCertificateFromStore(ServiceConfiguration.MailClientCertificateName));
				else
					connectInfo = new EmailMessageSource.ConnectionInformation(ServiceConfiguration.MailServer,
						CalculateServiceTypes((DatabaseEnums.MailServerType)ServiceConfiguration.MailServerType, encryption, mailbox), ServiceConfiguration.MailPort ?? 0,
						ServiceConfiguration.MailUserName, ServiceConfiguration.MailClientID, ServicePassword.Decode(ServiceConfiguration.MailEncryptedClientSecret));
			}
			else {
				string pw;
				if (ServiceConfiguration.MailEncryptedPassword != null)
					pw = ServicePassword.Decode(ServiceConfiguration.MailEncryptedPassword);
				else
					pw = string.Empty;
				connectInfo = new EmailMessageSource.ConnectionInformation(ServiceConfiguration.MailServer,
					CalculateServiceTypes((DatabaseEnums.MailServerType)ServiceConfiguration.MailServerType, encryption, mailbox), ServiceConfiguration.MailPort ?? 0,
					ServiceConfiguration.MailUserName, pw);
			}

			var maxMailSize = ServiceConfiguration.MaxMailSize ?? int.MaxValue;


#if DEBUG_Connection
			DebugDart(Logger, user, pw );
#endif
			try {
				using (var messageSource = new EmailMessageSource(Logger, traceDetails, connectInfo, mailbox, encryption == DatabaseEnums.MailServerEncryption.RequireEncryption)) {
					// each email that is received will be stored in a new request record.
					int n = messageSource.Messages.Count();
					Logger.LogTrace(traceDetails, Strings.Format(KB.K("Retrieving {0} Email {0.IsOne ? message : messages }"), n));
					DateTime staleDate = DateTime.Now.AddDays(-30);
					// Make a sanity check so this code does not utterly destroy the mailbox contents if it is pointed at the wrong mailbox, i.e. too many stale messages.
					// Not only will this avoid destroying the mailbox, it will also avoid flooding MB with bogus requests.
					// As this can happen legitimately if the service is not run for a while (e.g. school summer shutdown) the service can be manually run
					// with /Force+ to override this check.
					if (!Force && messageSource.Messages.Where(m => m.Sent < staleDate).Count() * 3 > n * 2) {
						// More than 2/3 of the messages are stale, don't process this as it may be the wrong mailbox.
						// Because of the fraction chose, this can also happen if the service is not run for three months and requests arrive at a steady state.
						// It can happen sooner if the service is not run and there is a dearth of recent (non-stale) requests.
						Logger.LogError(KB.K("No e-mails were processed because more than two thirds of them were stale; This may be the wrong mailbox.").Translate());
						return;
					}

					int deleteCount = 0;
					var copyOfMessages = messageSource.Messages.ToList();
					foreach (var message in copyOfMessages) {
						if (message.Error != null) {
							messageError(messageSource, message.Error, true);
							continue;
						}
						dsmb.T.EmailRequest.Clear();
						dsmb.T.EmailPart.Clear();
						dsmb.AcceptChanges();
						bool delete = false;
						DatabaseEnums.EmailRequestState processingState = DatabaseEnums.EmailRequestState.UnProcessed;
						try {
							if (string.IsNullOrEmpty(message.FromAddress)) {
								Logger.LogWarning(Thinkage.Libraries.Exception.FullMessage((new GeneralException(KB.K("Missing From address in message")).WithContext(MailMessageAsContext(message, traceDetails)))));
								delete = true;
								continue;
							}
							Logger.LogTrace(traceDetails, Strings.Format(KB.K("Processing message from '{0}' Sent '{1}' Message-ID {2}"), message.FromAddress, message.SentAsString, message.MessageId));
							var messageID = message.MessageId;
							MessageUniqueness duplicateId;
							if (!string.IsNullOrWhiteSpace(messageID) && MessageIDs.TryGetValue(messageID, out duplicateId) && message.SentAsString == duplicateId.SentAsText) {
								// we have had cases when pop3 failed and was delivering the same message over and over again
								// this will only allow the same message to be converted per time the program runs.
								if (duplicateId.Count++ < 2)
									Logger.LogWarning(Thinkage.Libraries.Exception.FullMessage((new GeneralException(KB.K("Duplicate Message-Id")).WithContext(MailMessageAsContext(message, traceDetails)))));
								delete = true;
								continue;
							}
							var mkey = new MessageUniqueness(messageID, message.SentAsString, message.Sent);
							MessageIDs[messageID] = mkey;
							if (message.Sent < staleDate) {
								Logger.LogWarning(Thinkage.Libraries.Exception.FullMessage(new GeneralException(KB.K("Stale message, over 30 days old")).WithContext(MailMessageAsContext(message, traceDetails))));
								processingState = DatabaseEnums.EmailRequestState.HoldRequiresManualReview;
							}
							else if (message.AutoSubmitted)
								processingState = DatabaseEnums.EmailRequestState.HoldRequiresManualReview;

							ServiceUtilities.EmailRequestFromEmail(dsmb, message, maxMailSize, processingState);
							DB.Update(dsmb);
							delete = true; // safe to mark delete if we didn't get error from Update
							Logger.LogTrace(traceDetails, Strings.Format(KB.K("Retrieved message from '{0}' with {1} parts"), message.FromAddress, message.Parts == null ? 0 : message.Parts.Count()));
						}
						catch (System.IO.IOException e) {
							GeneralException ge;
							if ((uint)e.HResult != 0x800700e1)
								ge = new GeneralException(e, KB.K("Error processing email message, message will be retried"));
							else {
								ge = new GeneralException(e, KB.K("The email message contains a virus or other unwanted software. The message will be deleted."));
								delete = true;
							}
							messageError(messageSource, ge, false);
						}
						catch (System.Exception e) {
							var ge = new GeneralException(e, KB.K("Error processing email message, message will be deleted"));
							delete = true;
							messageError(messageSource, ge, true);
						}
						finally {
							if (delete) {
#if DEBUG && DEBUGGINGLEAVESMESSAGESBEHIND
								Logger.LogTrace(Logging.Activities, Strings.Format(KB.K("DEBUG: should have deleted message from '{0}' Sent {1} Message-ID {2}"), message.FromAddress, message.Sent, message.MessageId));
#else
								message.Delete = true;
#endif
								deleteCount++;
							}
							Logger.LogTrace(traceDetails, Strings.Format(KB.T("{0}{1}"), Environment.NewLine, messageSource.Trace(true)));
							messageSource.TraceClear();
						}
					}
					try {
						messageSource.Close();
						if (traceDetails)
							Logger.LogTrace(traceDetails, Strings.Format(KB.K("Deleting processed Email Messages and closing Email Source:{0}{1}"), Environment.NewLine, messageSource.Trace()));
					}
					catch (System.Exception e) {
						GeneralException ge;
						switch (deleteCount) {
						default:
							ge = new GeneralException(e, KB.K("Error deleting {0} email messages"), deleteCount);
							break;
						case 1:
							ge = new GeneralException(e, KB.K("Error deleting email message"));
							break;
						case 0:
							ge = new GeneralException(e, KB.K("Error closing email message source"));
							break;
						}
						messageError(messageSource, ge, true);
					}
				}
				return;
			}
			catch (GeneralException e) {
				Logger.LogError(Thinkage.Libraries.Exception.FullMessage(e));
			}
			catch (System.Exception e) {
				if (connectInfo.Port != 0)
					Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("Error accessing server '{0}' on port {1}. Manual intervention may be necessary."), ServiceConfiguration.MailServer, connectInfo.Port)));
				else
					Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("Error accessing server '{0}' manual intervention may be necessary"), ServiceConfiguration.MailServer)));
			}
		}
		private EmailMessageSource.ConnectionInformation.ServiceTypes CalculateServiceTypes(DatabaseEnums.MailServerType serverType, DatabaseEnums.MailServerEncryption encryption, string mailboxName) {
			EmailMessageSource.ConnectionInformation.ServiceTypes serviceTypes = EmailMessageSource.ConnectionInformation.ServiceTypes.All;
			switch (serverType) {
			case DatabaseEnums.MailServerType.POP3:
				serviceTypes = EmailMessageSource.ConnectionInformation.ServiceTypes.AnyPOPProtocol & ~EmailMessageSource.ConnectionInformation.ServiceTypes.ImplicitlyEncrypted;
				break;
			case DatabaseEnums.MailServerType.IMAP4:
				serviceTypes = EmailMessageSource.ConnectionInformation.ServiceTypes.AnyIMAPProtocol & ~EmailMessageSource.ConnectionInformation.ServiceTypes.ImplicitlyEncrypted;
				break;
			case DatabaseEnums.MailServerType.POP3S:
				serviceTypes = EmailMessageSource.ConnectionInformation.ServiceTypes.POP3S;
				break;
			case DatabaseEnums.MailServerType.IMAP4S:
				serviceTypes = EmailMessageSource.ConnectionInformation.ServiceTypes.IMAP4S;
				break;
			}

			switch (encryption) {
			case DatabaseEnums.MailServerEncryption.None:
				if ((serviceTypes & EmailMessageSource.ConnectionInformation.ServiceTypes.Plaintext) == 0)
					Logger.LogWarning(Strings.Format(KB.K("Mail Service Type does not allow any plaintext protocol, so Encryption 'None' will be ignored")));
				else
					serviceTypes &= EmailMessageSource.ConnectionInformation.ServiceTypes.Plaintext;
				break;
			case DatabaseEnums.MailServerEncryption.RequireEncryption:
			case DatabaseEnums.MailServerEncryption.RequireValidCertificate:
				// All the protocls allow an encrypted form so we don't have to check for this eliminating all choices.
				serviceTypes &= EmailMessageSource.ConnectionInformation.ServiceTypes.Encrypted;
				break;
			}

			if (!string.IsNullOrEmpty(mailboxName)) {
				if ((serviceTypes & EmailMessageSource.ConnectionInformation.ServiceTypes.CanSpecifyMailbox) == 0)
					Logger.LogWarning(Strings.Format(KB.K("Mail Service Type does not allow specification of a Mailbox, so the Mailbox setting will be ignored")));
				else
					serviceTypes &= EmailMessageSource.ConnectionInformation.ServiceTypes.CanSpecifyMailbox;
			}

			return serviceTypes;
		}
		private MessageExceptionContext MailMessageAsContext(EmailMessage mail, bool full) {
			return new MessageExceptionContext(KB.K("Message:{0}{1}"), Environment.NewLine, mail.MessageAsText(full));
		}
		#region MessageUniqueness
		//
		// message ID are supposted to be unique but some times are not
		// this test is the same one that exchange uses
		// we check agains the date as text, not decoded as a data, since if its the same
		// message the printed format will be the same.
		//
		struct MessageUniqueness {
			public readonly string MessageId;
			public readonly string SentAsText;
			public readonly DateTime Sent;
			public int Count;
			public MessageUniqueness(string messageId, string sentAsText, DateTime sent) {
				MessageId = messageId;
				SentAsText = sentAsText;
				Sent = sent;
				Count = 1;
			}
		}
		//
		// all messageid and date sent kept for 24 hours, so there will be a duplicate message received each 24 hours. 
		// the messageid's could be checed with the messages in the database. 
		// This was not done. If we have duplicate message it occurs either because we have a bug,
		// the pop3/imap server is giving us the same email message over again.
		// If we are getting the same message multiple times while the program is running we do not
		// want to fill the logs with useless error messages. So we need to keep track in the program
		// no using the database causes the email message to be accepted each time the program is started
		// which will be hopefully enough but not too much to cause soone to examine why.
		//
		static private Dictionary<string, MessageUniqueness> MessageIDs = new Dictionary<string, MessageUniqueness>();
		public static void FlushIDs() {
			MessageIDs = MessageIDs.Where(e => e.Value.Sent != null && e.Value.Sent > DateTime.Today - TimeSpan.FromDays(1)).ToDictionary(e => e.Key, e => e.Value);
		}
		private void messageError(EmailMessageSource messageSource, GeneralException e, bool fullTrace) {
			var m = new StringBuilder(); // can not use WithContext, it puts the trace before the cause of the error
			m.AppendLine(Thinkage.Libraries.Exception.FullMessage(e));
			m.AppendLine();
			if (messageSource != null && messageSource.TraceLength != 0)
				m.Append(messageSource.Trace(fullTrace));
			Logger.LogWarning(m.ToString());
		}

		#endregion
#if DEBUG
		static bool debugOnce = false;
		void DebugDart(string user, string pw) {
			// TODO: This method is kinda useless now because it only tries Plain authentication, not OAuth2.
			if (debugOnce)
				return;
			debugOnce = true;
			// pop
			string server = "mail.thinkage.ca";
			int port = 0;
			string mailbox = null;
			// pop3
			DebugClient(KB.K("Bad Host"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(KB.K("Bad Port"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3723, user, pw, mailbox);
			DebugClient(KB.K("Bad User"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(KB.K("Bad Password"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			// pop3s         
			DebugClient(KB.K("Bad Host"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(KB.K("Bad Port"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3723, user, pw, mailbox);
			DebugClient(KB.K("Bad User"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(KB.K("Bad Password"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			// imap4
			DebugClient(KB.K("Bad Host"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(KB.K("Bad Port"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3733, user, pw, mailbox);
			DebugClient(KB.K("Bad User"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(KB.K("Bad Password"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			DebugClient(KB.K("Bad MailBox"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, "Unknown");
			// imap4S
			DebugClient(KB.K("Bad Host"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(KB.K("Bad Port"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3733, user, pw, mailbox);
			DebugClient(KB.K("Bad User"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(KB.K("Bad Password"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			DebugClient(KB.K("Bad MailBox"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, "Unknown");
			// imap4S
			DebugClient(KB.K("Bad Host"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(KB.K("Bad Port"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3733, user, pw, mailbox);
			DebugClient(KB.K("Bad User"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(KB.K("Bad Password"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			DebugClient(KB.K("Bad MailBox"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, "Unknown");
			// pop3 and iamp4 
			DebugClient(KB.K("POP3-none"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, mailbox);
			DebugClient(KB.K("POP3-Implicit"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 110, user, pw, mailbox);
			DebugClient(KB.K("POP3S-Explict"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 995, user, pw, mailbox);
			DebugClient(KB.K("IMAP4-none"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, mailbox);
			DebugClient(KB.K("IMAP4-Explicit"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 143, user, pw, mailbox);
			DebugClient(KB.K("IMAP4-Implicit"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 993, user, pw, mailbox);
			DebugClient(KB.K("Any"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 993, user, pw, mailbox);
			// pop3 and imap4 force encryption
			DebugClient(KB.K("POP3-none"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.None, server, port, user, pw, mailbox);
			DebugClient(KB.K("POP3-encrypt"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(KB.K("POP3S-encrypt"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(KB.K("IMAP4-none"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.None, server, port, user, pw, mailbox);
			DebugClient(KB.K("IMAP4-encrypt"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(KB.K("IMAP4S-encrypt"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(KB.K("Any-encrypt"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			// pop3 and imap4 force certificate
			DebugClient(KB.K("POP3-cert"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(KB.K("POP3S-cert"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(KB.K("IMAP4-cert"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(KB.K("IMAP4S-cert"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(KB.K("Any-cert"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
		}
		private void DebugClient(Key prefix, DatabaseEnums.MailServerType serverType, DatabaseEnums.MailServerEncryption Encryption, string server, int port, string user, string pw, string mailbox) {
			try {
				using (var messagesource = new EmailMessageSource(Logger, true, new EmailMessageSource.ConnectionInformation(server,
                    CalculateServiceTypes(serverType, Encryption, mailbox), port, user, pw), mailbox,
					Encryption == DatabaseEnums.MailServerEncryption.RequireEncryption)) {
					Logger.LogTrace(true, Thinkage.Libraries.Exception.FullMessage(new GeneralException(KB.K("Succeeded with {0} -- server '{1}' on port {2} using {3}\n"),
						prefix, server, messagesource.Port, messagesource.Protocol).WithContext(messagesource.TraceContext)));
				}
			}
			catch (System.Exception e) {
				Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("{0} server '{1}' on port {2} using {3} with user '{4}'\n"), prefix, server, port, serverType, user)));
			}
		}
#endif
		#endregion
	}
}
