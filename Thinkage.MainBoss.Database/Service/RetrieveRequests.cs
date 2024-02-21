using System;
using System.Linq;
using System.Text;
using System.Net.Security;
using System.Security.Authentication;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Dart.Mail;
using Thinkage.Libraries.Translation;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Thinkage.MainBoss.Database.Service {

	public class RetrieveRequests : EmailProcessor {
		#region Constructors, Destructor
		public RetrieveRequests(IServiceLogging logger, MB3Client dbSession)
			: base(logger, dbSession) {
			dsmb.EnsureDataTableExists(dsMB.Schema.T.EmailRequest, dsMB.Schema.T.EmailPart);
		}
		#endregion

		#region Run - GetEmailRequests
		/// <summary>
		/// Retrieves mail, creates work requests for valid mail-requests and put them in the user-defined state that is specified by XXX.
		/// An erequest record is created for each email regardless of validity.
		/// </summary>
		public void Run(bool traceDetails) {
			var port = ServiceConfiguration.MailPort;
			var server = ServiceConfiguration.MailServer;
			string user = ServiceConfiguration.MailUserName ?? string.Empty;
			string pw = ServiceConfiguration.MailEncryptedPassword == null ? string.Empty : Thinkage.MainBoss.Database.Service.ServicePassword.Decode(ServiceConfiguration.MailEncryptedPassword);
			var protocol = (DatabaseEnums.MailServerType)ServiceConfiguration.MailServerType;
			var encryption = (DatabaseEnums.MailServerEncryption)ServiceConfiguration.Encryption;
			var serverType = (DatabaseEnums.MailServerType)ServiceConfiguration.MailServerType;
			var mailbox = ServiceConfiguration.MailboxName;
			var maxMailSize = ServiceConfiguration.MaxMailSize ?? int.MaxValue;
			bool useOAuth2 = false; // eventually will have to get this setting and values from Service Configuration


			if (server == null)
				return;

			if (useOAuth2) {
				// need access token which is the password in Dart
				pw = new OAuth2ManagerAzure().GetAccessToken();
			}
#if DEBUG_Connection
			DebugDart(Logger, user, pw );
#endif
			try {
				using (var messageSource = new EmailMessageSource(Logger, traceDetails, serverType, encryption, server, port ?? 0, user, pw, mailbox, useOAuth2)) {
					// each email that is received will be stored in a new request record.
					int n = messageSource.Messages.Count();
					Logger.LogTrace(traceDetails, Strings.Format(KB.K("Retrieving {0} Email {0.IsOne ? message : messages }"), n));
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
							if (message.Sent + TimeSpan.FromDays(30) < DateTime.Today) {
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
#if DEBUGGINGLEAVESMESSAGESBEHIND
								Logger.LogTrace(Logging.ReadEmailRequest, Strings.Format(KB.K("DEBUG: should have deleted message from '{0}' Sent {1} Message-ID {2}"), message.FromAddress, message.Sent, message.MessageId));
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
				if ((port ?? 0) != 0)
					Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("Error accessing server '{0}' on port {1}. Manual intervention may be necessary."), ServiceConfiguration.MailServer, port)));
				else
					Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("Error accessing server '{0}' manual intervention may be necessary"), ServiceConfiguration.MailServer)));
			}
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
		void DebugDart(IServiceLogging Logger, string user, string pw) {
			if (debugOnce)
				return;
			debugOnce = true;
			// pop
			string server = "mail.thinkage.ca";
			int port = 0;
			string mailbox = null;
			// pop3
			DebugClient(Logger, KB.K("Bad Host"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad Port"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3723, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad User"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(Logger, KB.K("Bad Password"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			// pop3s         
			DebugClient(Logger, KB.K("Bad Host"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad Port"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3723, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad User"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(Logger, KB.K("Bad Password"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			// imap4
			DebugClient(Logger, KB.K("Bad Host"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad Port"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3733, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad User"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(Logger, KB.K("Bad Password"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			DebugClient(Logger, KB.K("Bad MailBox"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, "Unknown");
			// imap4S
			DebugClient(Logger, KB.K("Bad Host"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad Port"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3733, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad User"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(Logger, KB.K("Bad Password"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			DebugClient(Logger, KB.K("Bad MailBox"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, "Unknown");
			// imap4S
			DebugClient(Logger, KB.K("Bad Host"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, "xx@thinkage.ca", port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad Port"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 3733, user, pw, mailbox);
			DebugClient(Logger, KB.K("Bad User"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, "xyz", pw, mailbox);
			DebugClient(Logger, KB.K("Bad Password"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, "abc", mailbox);
			DebugClient(Logger, KB.K("Bad MailBox"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, "Unknown");
			// pop3 and iamp4 
			DebugClient(Logger, KB.K("POP3-none"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("POP3-Implicit"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 110, user, pw, mailbox);
			DebugClient(Logger, KB.K("POP3S-Explict"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 995, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4-none"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4-Explicit"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 143, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4-Implicit"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 993, user, pw, mailbox);
			DebugClient(Logger, KB.K("Any"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.AnyAvailable, server, 993, user, pw, mailbox);
			// pop3 and imap4 force encryption
			DebugClient(Logger, KB.K("POP3-none"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.None, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("POP3-encrypt"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("POP3S-encrypt"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4-none"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.None, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4-encrypt"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4S-encrypt"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Any-encrypt"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.RequireEncryption, server, port, user, pw, mailbox);
			// pop3 and imap4 force certificate
			DebugClient(Logger, KB.K("POP3-cert"), DatabaseEnums.MailServerType.POP3, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("POP3S-cert"), DatabaseEnums.MailServerType.POP3S, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4-cert"), DatabaseEnums.MailServerType.IMAP4, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("IMAP4S-cert"), DatabaseEnums.MailServerType.IMAP4S, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
			DebugClient(Logger, KB.K("Any-cert"), DatabaseEnums.MailServerType.Any, DatabaseEnums.MailServerEncryption.RequireValidCertificate, server, port, user, pw, mailbox);
		}
		private void DebugClient(IServiceLogging logger, Key prefix, DatabaseEnums.MailServerType serverType, DatabaseEnums.MailServerEncryption Encryption, string server, int port, string user, string pw, string mailbox, bool useOAuth2 = false) {
			try {
				using (var messagesource = new EmailMessageSource(Logger, true, serverType, Encryption, server, port, user, pw, mailbox, useOAuth2)) {
					logger.LogTrace(true, Thinkage.Libraries.Exception.FullMessage(new GeneralException(KB.K("Succeeded with {0} -- server '{1}' on port {2} using {3}\n"),
						prefix, server, messagesource.Port, messagesource.Protocol).WithContext(EmailMessageSource.TraceContext(messagesource))));
				}
			}
			catch (System.Exception e) {
				logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(e, KB.K("{0} server '{1}' on port {2} using {3} with user '{4}'\n"), prefix, server, port, serverType, user)));
			}
		}
#endif
		#endregion
	}
}
