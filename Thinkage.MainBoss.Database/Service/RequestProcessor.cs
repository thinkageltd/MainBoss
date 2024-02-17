using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.DBILibrary;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Does the actual work of retrieving  mail, updates requests status and sends requestor rejection messages.
	/// </summary>
	public class RequestProcessor : EmailNotificationProcessor {
		#region Constructors, Destructor

		private RequestProcessor(IServiceLogging logger, MB3Client dbSession, bool allowOutgoingEmail) : base(logger, dbSession, allowOutgoingEmail) {
			// We clear some datasets during processing so ensure they exist.
			dsmb.EnsureDataTableExists(dsMB.Schema.T.EmailRequest, dsMB.Schema.T.EmailPart, dsMB.Schema.T.ActiveRequestor, dsMB.Schema.T.Requestor, dsMB.Schema.T.RequestState,
				dsMB.Schema.T.RequestStateHistory, dsMB.Schema.T.Request, dsMB.Schema.T.Contact,
				dsMB.Schema.T.RequestAcknowledgement);
		}
		public static void DoAllRequestProcessing(IServiceLogging logger, MB3Client dbSession, bool traceActivities, bool traceDetails, bool allowOutgoingEmail = true) {
			if (lastFlush + TimeSpan.FromDays(1) < DateTime.Today) {
				RetrieveRequests.FlushIDs();
				lastFlush = DateTime.Today;
			}
			using (RequestProcessor x = new RequestProcessor(logger, dbSession, allowOutgoingEmail)) {
				if (x.ServiceConfiguration.ProcessRequestorIncomingEmail ) {
					if (x.ServiceConfiguration.MailServer == null)
						logger.LogInfo(KB.K("Email cannot be processed because there is no incoming Mail Server configured").Translate());
					else if( x.ServiceConfiguration.MailUserName == null )
						logger.LogInfo(KB.K("Email cannot be processed because there is no incoming Mail User Name configured").Translate());
					else {
						logger.LogTrace(traceDetails, KB.K("Email requests processing started").Translate());
						x.FetchAndProcessEmailRequests(traceActivities, traceDetails);
						logger.LogTrace(traceDetails, KB.K("Email requests processing completed").Translate());
					}
				}
				else if(x.ServiceConfiguration.ProcessRequestorIncomingEmail != oldProcessRequestorIncomingEmail ) { 
					logger.LogInfo(KB.K("Processing Email requests has been disabled").Translate());
				}
				oldProcessRequestorIncomingEmail = x.ServiceConfiguration.ProcessRequestorIncomingEmail;
			}

		}
		static DateTime lastFlush = DateTime.Today;
		static bool? oldProcessRequestorIncomingEmail = true;
		bool SendRejections = true;
		#endregion

		#region FetchAndProcessEmailRequests
		public void FetchAndProcessEmailRequests(bool traceActivities, bool traceDetails) {
			try {
				using (var rr = new RetrieveRequests(Logger, DB))
					rr.Run(traceDetails);
				CreateRequestsFromEmail(traceActivities, traceDetails);
			}
			catch (MainBossServiceWorkerException e) {
				Logger.LogError(Thinkage.Libraries.Exception.FullMessage(e));  // a log message will be generated each servicing interval for a configuration error.
			}
			catch (GeneralException e) {
				Logger.LogError(Thinkage.Libraries.Exception.FullMessage(e));  // a log message will be generated each servicing interval for a configuration error.
			}
			catch (System.Exception e) {
				Logger.LogError(e.Message);
				throw new MainBossServiceWorkerException(e, KB.K("Error during processing of Email requests"));
			}
		}
		#endregion
		#region CreateRequestsFromEmail
		/// <summary>
		/// Create work requests for valid EmailRequest records in the 'UnProcessed' state
		/// </summary>

		private void CreateRequestsFromEmail(bool traceActivities, bool traceDetails) {
			dsmb.T.EmailRequest.Clear();
			dsmb.AcceptChanges();

			DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.RequestState);
			// Locate all the UnProcessed state EmailRequest records that are not hidden.
			DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.EmailRequest,new SqlExpression(dsMB.Path.T.EmailRequest.F.ProcessingState).In(SqlExpression.Constant(ToBeProcessed)));
			if (dsmb.T.EmailRequest.Rows.Count == 0) {
				if( traceActivities)
					Logger.LogActivity(KB.K("No Email requests to process").Translate());
				return;
			}
			var newEmail = 0;
			var retryEmail = 0;
			foreach( dsMB.EmailRequestRow r in dsmb.T.EmailRequest.Rows)
				if( r.F.ProcessingState == (int)DatabaseEnums.EmailRequestState.UnProcessed )
					newEmail++;
				else
					retryEmail++;
			Logger.LogTrace( traceDetails, Strings.Format(KB.K("Processing {0} new Email {0.IsOne ?  message : messages }, and retry processing {1} Email {1.IsOne ?  message : messages }"), newEmail, retryEmail));
			for (int i = 0; i < dsmb.T.EmailRequest.Rows.Count; i++) {
				System.Globalization.CultureInfo preferredLanguage = Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage((int?)dsMB.Schema.T.EmailRequest.F.PreferredLanguage[(dsMB.EmailRequestRow)dsmb.T.EmailRequest.Rows[i]]);
				Guid emailRequestId = (Guid)dsMB.Schema.T.EmailRequest.F.Id[(dsMB.EmailRequestRow)dsmb.T.EmailRequest.Rows[i]];
				string commentToRequestor = UK.K("RequestorInitialCommentPreamble").Translate(preferredLanguage);
				var processingAllowance = Unavailable ? new TimeSpan(30 * 24, 0, 0) : ServiceConfiguration.ManualProcessingTimeAllowance ?? new TimeSpan(0); // if no outgoing SMTP don't expire failed requests
				ReplytoSender? reply = new NewRequestFromEmailRequest(DB, processingAllowance
																			  , ServiceConfiguration.MailUserName
																			  , ServiceConfiguration.AutomaticallyCreateRequestors
																			  , ServiceConfiguration.AutomaticallyCreateRequestorsFromEmail
																			  , ServiceConfiguration.AutomaticallyCreateRequestorsFromLDAP
																			  , ServiceConfiguration.AcceptAutoCreateEmailPattern
																			  , ServiceConfiguration.RejectAutoCreateEmailPattern
																			  , Logger)
																			  .ConvertOne(emailRequestId, commentToRequestor);
				if (reply.HasValue && SendRejections )
					FormatReplyToSender(reply.Value);
			}
			ProcessRepliesToSender(traceDetails);
		}
		static Thinkage.Libraries.Collections.Set<object> ToBeProcessed = new Thinkage.Libraries.Collections.Set<object> {
			(long)DatabaseEnums.EmailRequestState.UnProcessed,
			(long)DatabaseEnums.EmailRequestState.NoRequestor,
			(long)DatabaseEnums.EmailRequestState.NoContact,
			(long)DatabaseEnums.EmailRequestState.AmbiguousRequestor,
			(long)DatabaseEnums.EmailRequestState.AmbiguousContact,
			(long)DatabaseEnums.EmailRequestState.AmbiguousContactCreation,
			(long)DatabaseEnums.EmailRequestState.ToBeRejected,
		};
		#endregion
		#region RejectionMessage Processing
		private Dictionary<string, List<ReplytoSender>> MessageWithReplyToSender = new Dictionary<string, List<ReplytoSender>>();
		private void FormatReplyToSender(ReplytoSender reply) {
			List<ReplytoSender> replies;
			var pending = UK.K("RequestorStatusPending").Translate(reply.PreferredLanguage);
			if (!reply.Rejection && string.IsNullOrWhiteSpace(pending)) return; // no message no warning
			if (!MessageWithReplyToSender.TryGetValue(reply.To.Address, out replies)) {
				replies = new List<ReplytoSender>();
				MessageWithReplyToSender.Add(reply.To.Address, replies);
			}
			// Build the message to the user
			StringBuilder body = new StringBuilder();
			if( reply.Rejection)
				body.AppendLine(UK.K("RequestDeniedPreamble").Translate(reply.PreferredLanguage));
			else
				body.AppendLine(pending);
			if (!string.IsNullOrWhiteSpace(reply.MessageToConvey)) {
				body.AppendLine();
				body.AppendLine(reply.MessageToConvey);
			}
			if (!String.IsNullOrEmpty(reply.OriginalBody)) {
				body.AppendLine();
				body.AppendLine(UK.K("OriginalRequestPreamble").Translate(reply.PreferredLanguage));
				body.AppendLine();
				body.Append(reply.OriginalBody);
			}
			replies.Add(new ReplytoSender(reply.To, reply.Subject, reply.ReceivedDate, reply.Rejection, reply.OriginalBody, reply.PreferredLanguage, body.ToString()));
		}
		private void ProcessRepliesToSender(bool traceDetails) {
			if (MessageWithReplyToSender.Count == 0)
				return; // most likely case so lets not waste time setting up to send emails
			if ( Unavailable || ! SendRejections) return; // there is no outgoing email, because of the configuration, to do not try sent the messages
			Logger.LogTrace(traceDetails, Strings.Format(KB.K("Processing Replies to Sender for {0} {0.IsOne ?  requestor : requestors }"), MessageWithReplyToSender.Count));
			foreach (string replyAddress in MessageWithReplyToSender.Keys) {
				List<ReplytoSender> messagesToSend = MessageWithReplyToSender[replyAddress];
				var rejections = MessageWithReplyToSender[replyAddress].Where(e=>e.Rejection).OrderBy(e=>e.ReceivedDate);
				var delays = MessageWithReplyToSender[replyAddress].Where(e => !e.Rejection).OrderBy(e=>e.ReceivedDate).Take(1); // never need to tell the user more that once that his request has been delayed.
				DateTime dateSent;
				IEnumerable<ReplytoSender> allreplies = rejections;
				if ( delays.Any() && (!hadDelayMessage.TryGetValue(replyAddress, out dateSent) || dateSent + TimeSpan.FromDays(1) < DateTime.Today)) {
					allreplies = rejections.Concat(delays);
					hadDelayMessage[replyAddress] =  DateTime.Today;
				}
				int c = rejections.Count();
				if( c != 0 )
					Logger.LogWarning(Strings.Format(KB.K("{0} rejection {0.IsOne ? message : messages } for {1}"), c, replyAddress));
				foreach (ReplytoSender rm in allreplies)
					using (MailMessage mm = smtp.NewMailMessage(rm.To)) {
						mm.Subject = UK.K("RequestorNotificationSubjectPrefix").Translate(rm.PreferredLanguage);
						mm.Subject += rm.Subject;
						mm.Body = rm.MessageToConvey;
						smtp.Send(mm);
					}
			}
			MessageWithReplyToSender.Clear();
		}
		static Dictionary<string, DateTime> hadDelayMessage = new Dictionary<string, DateTime>();
		#endregion
	}
}
