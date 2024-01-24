using System;
using System.Net.Mail;
using System.Text;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using System.Text.RegularExpressions;
using System.DirectoryServices;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;

namespace Thinkage.MainBoss.Database.Service {
	#region ReplytoSender
	/// <summary>
	/// A rejection message to convey back to requestor
	/// </summary>
	public struct ReplytoSender {
		public ReplytoSender(MailAddress to, string subject, DateTime recievedDate, bool rejection, String originalBody, System.Globalization.CultureInfo preferredLanguage, string messageToConvey) {
			To = to;
			Subject = subject;
			ReceivedDate = recievedDate;
			Rejection = rejection;
			OriginalBody = originalBody;
			PreferredLanguage = preferredLanguage;
			MessageToConvey = messageToConvey;
		}
		public readonly MailAddress To;
		public readonly string Subject;
		public readonly bool Rejection;
		public readonly string OriginalBody;
		public readonly string MessageToConvey;
		public System.Globalization.CultureInfo PreferredLanguage;
		public DateTime ReceivedDate;
	}
	#endregion

	public class NewRequestFromEmailRequest {
		private readonly MB3Client DB;
		private readonly IServiceLogging Log = null;
		private readonly string AvoidSendingToPreventMessageLoopingEmailAddress = null;
		private TimeSpan ManualProcessingAllowance;
		private bool CreateRequestors = true;
		private bool CreateFromEmail = true;
		private bool CreateFromLDAP = true;
		private readonly Regex AcceptRegex = null;
		private readonly Regex RejectRegex = null;

		public NewRequestFromEmailRequest(MB3Client db, TimeSpan manualProcessingAllowance, string emailAddressToReject, bool createRequestors, bool createFromLDAP, bool createFromEmail, string acceptPattern, string rejectPattern, IServiceLogging log) {
			DB = db;
			Log = log;
			ManualProcessingAllowance = manualProcessingAllowance;
			AvoidSendingToPreventMessageLoopingEmailAddress = emailAddressToReject; // for checking for emails from same address as that used to processing incoming emails; could cause an endless loop of emails to ourselves
			CreateFromEmail  = createFromEmail;
			CreateFromLDAP   = createFromLDAP;
			CreateRequestors = createRequestors | createFromLDAP | createFromEmail;
			if (!string.IsNullOrWhiteSpace(acceptPattern)) {
				try {
					AcceptRegex = new Regex(acceptPattern, RegexOptions.IgnoreCase);
				}
				catch (ArgumentException) {
					log.LogError(Strings.Format(KB.K("Syntax error in the regular expression /{0}/ used to validate acceptable auto created requests, as a result all Email addresses will be acceptable"), acceptPattern));
				}
			}
			if (!string.IsNullOrWhiteSpace(rejectPattern)) {
				try {
					RejectRegex = new Regex(rejectPattern, RegexOptions.IgnoreCase);
				}
				catch (ArgumentException) {
					log.LogError(Strings.Format(KB.K("Syntax error in the regular expression /{0}/ used to reject auto created Requests, as a result no Email addresses will be rejected"), rejectPattern));
				}
			}
		}
		public NewRequestFromEmailRequest(MB3Client db) {
			DB = db;
		}
		/// <summary>
		/// Test if safe to create a rejection email
		/// </summary>
		/// <param name="toWhom"></param>
		/// <returns></returns>
		private bool OkayToSendRejectionEmail(string toWhom) {
			return !(AvoidSendingToPreventMessageLoopingEmailAddress != null && String.Compare(toWhom, AvoidSendingToPreventMessageLoopingEmailAddress, true) == 0);
		}
		#region ConvertOne
		/// <summary>
		/// Convert one EmailRequest into a MainBoss Request; return a RejectionMessage if unable for possibly sending to the originator
		/// </summary>
		/// <param name="emailRequestID"></param>
		/// <param name="commentToRequestor">The (translated) comment in the preferred language of the requestor to put in the StateHistoryRecord for conveyance to the Requestor</param>
		/// <returns></returns>
		public ReplytoSender? ConvertOne(Guid emailRequestID, string commentToRequestor) {
			try {
				using (dsMB ds = new dsMB(DB)) {
					ds.EnsureDataTableExists(dsMB.Schema.T.EmailRequest, dsMB.Schema.T.ActiveRequestor, dsMB.Schema.T.Requestor, dsMB.Schema.T.RequestState,
						dsMB.Schema.T.RequestStateHistory, dsMB.Schema.T.Request, dsMB.Schema.T.Contact,
						dsMB.Schema.T.RequestAcknowledgement);
					// Fetch the Email to process into our update dataset
					DB.ViewAdditionalRows(ds, dsMB.Schema.T.EmailRequest, new SqlExpression(dsMB.Path.T.EmailRequest.F.Id).Eq(SqlExpression.Constant(emailRequestID)), null, null);
					if (ds.T.EmailRequest.Rows.Count != 1)
						throw new GeneralException(KB.K("Cannot locate Email Request {0} for processing."), emailRequestID);
					System.Net.Mail.MailAddress from;
					dsMB.EmailRequestRow emailRequest = (dsMB.EmailRequestRow)ds.T.EmailRequest.Rows[0];
					try {
						from = new System.Net.Mail.MailAddress(emailRequest.F.RequestorEmailAddress, emailRequest.F.RequestorEmailDisplayName);
					}
					catch( System.Exception ) {
						ProcessingError(emailRequest, DatabaseEnums.EmailRequestState.NoRequestor, Strings.Format(KB.K("There was no valid sender's email address in the mail message")));
						return null;
					}
					try {
						// parse email message info.
						// set a status to reject email addresses that would cause a cycle of sending/receiving messages to ourselves. We set a flag here so we
						// can also not send a rejection email to the same address (again, causing an endless cycle of rejection messages to ourselves)
						// perhaps in future the condition can be expanded beyond the simple string comparison.
						if (!OkayToSendRejectionEmail(from.Address))
							throw new GeneralException(KB.K("The 'from address' is the same as the email address used for incoming MainBoss Service request emails. This email will be ignored to prevent a cycle of endless emails."));
						// First try to locate an existing requestor that matches the email address in the email message
						var requestorInfo = new AcquireRequestor(DB, from, CreateFromEmail, CreateFromLDAP, CreateFromEmail, AcceptRegex, RejectRegex, emailRequest.F.PreferredLanguage);
						if (requestorInfo.Exception != null) {
							ProcessingError(emailRequest, requestorInfo.State, Thinkage.Libraries.Exception.FullMessage(requestorInfo.Exception), requestorInfo.UserText);
							return InformSender;
						}
						else if (requestorInfo.WarningText != null)
							Log.LogWarning(requestorInfo.WarningText);
						else if (requestorInfo.InfoText != null)
							Log.LogInfo(requestorInfo.InfoText);
	
						var requestorID = requestorInfo.RequestorID;
						if (requestorID == null) {
							emailRequest.F.RequestID = null;
							string userMess = Strings.Format(KB.K("Email request from '{0}' does not match the email address of any Requestor"), from.Address);
							if (InformSender == null)
								ProcessingError(emailRequest, DatabaseEnums.EmailRequestState.NoRequestor, requestorInfo.UserText ?? userMess, userMess);
							return InformSender;
						}
						var requestNumber = ServiceUtilities.CreateRequest(ds, emailRequest, requestorID.Value, commentToRequestor);
						Log.LogInfo(Strings.Format(KB.K("Request {0} created for requestor '{1}' using email from '{2}'"), requestNumber, requestorInfo.ContactCode, from.Address));
						return null;
					}
					catch (System.Exception ex) {
						var errorText = Strings.Format(KB.K("Request from {0} rejected: {1}"), from.Address, Thinkage.Libraries.Exception.FullMessage(ex));
						ProcessingError(emailRequest, DatabaseEnums.EmailRequestState.Error, errorText, Strings.Format(KB.K("Request from {0} had internal error. The request was not processed"), from.Address));
						return InformSender;
					}
					finally {
						DB.Update(ds);
					}
				}
			}
			catch (System.Exception ex) {
				Log.LogError(Strings.Format(KB.K("Cannot update EmailRequest status (ID {0}): {1}"), emailRequestID, Thinkage.Libraries.Exception.FullMessage(ex)));
			}
			return null; // No Rejection message
		}
		#endregion
		#region ProcessingError
		private ReplytoSender? InformSender = null;
		private void ProcessingError(dsMB.EmailRequestRow emailRequest, DatabaseEnums.EmailRequestState errorState, string errorText, string textToUser = null) {
			//
			// retryable message expry and wil be rejected after a time limit
			//
			if( emailRequest.F.ProcessedDate.Value + ManualProcessingAllowance < DateTime.Now)
				expiredState.TryGetValue(errorState,out errorState);

			//
			// the type of message returned to the user depends on the previous state and the new state
			//
			var currentState = (DatabaseEnums.EmailRequestState) emailRequest.F.ProcessingState;
			if ( currentState == errorState && emailRequest.F.Comment == errorText)
				return; // same state and same comment return, no message;

			emailRequest.F.ProcessingState = (short)errorState;
			emailRequest.F.Comment = errorText;
			emailRequest.F.ProcessedDate = DateTime.Now;
			Log.LogWarning(errorText);
			var accept = AcceptRegex == null || AcceptRegex.Match(emailRequest.F.RequestorEmailAddress).Length >= 1;
			var reject = RejectRegex != null && RejectRegex.Match(emailRequest.F.RequestorEmailAddress).Length >= 1;

			if (!accept && reject) 
				return; // no messages to spammers.

			var retry = retryAble(errorState);
			if( retry && currentState != DatabaseEnums.EmailRequestState.UnProcessed )
				return; // a message has been sent we don't want to send out out on each retry attempt.

			if( retry )
				textToUser = null;
			else if( textToUser == null )
					textToUser = Strings.Format(KB.K("No valid Requestor found for email address '{0}'"), emailRequest.F.RequestorEmailAddress);
			if( emailRequest.F.ReceiveDate > DateTime.Today-TimeSpan.FromDays(30) ) // if over 30 days old we never send a message back
				InformSender = new ReplytoSender(new MailAddress(emailRequest.F.RequestorEmailAddress, emailRequest.F.RequestorEmailDisplayName), emailRequest.F.Subject, emailRequest.F.ReceiveDate, 
					retry,emailRequest.F.MailMessage, Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage((int?)emailRequest.F.PreferredLanguage), textToUser);
		}
		static private Dictionary<DatabaseEnums.EmailRequestState, DatabaseEnums.EmailRequestState> expiredState = new Dictionary<DatabaseEnums.EmailRequestState, DatabaseEnums.EmailRequestState> {
			{ DatabaseEnums.EmailRequestState.NoContact, DatabaseEnums.EmailRequestState.RejectNoContact },
			{ DatabaseEnums.EmailRequestState.NoRequestor, DatabaseEnums.EmailRequestState.RejectNoRequestor },
			{ DatabaseEnums.EmailRequestState.AmbiguousRequestor, DatabaseEnums.EmailRequestState.RejectAmbiguousRequestor },
			{ DatabaseEnums.EmailRequestState.AmbiguousContact, DatabaseEnums.EmailRequestState.RejectAmbiguousContact },
			{ DatabaseEnums.EmailRequestState.AmbiguousContactCreation, DatabaseEnums.EmailRequestState.RejectAmbiguousContactCreation },
		};
		private bool retryAble(DatabaseEnums.EmailRequestState state) {
			return state == DatabaseEnums.EmailRequestState.NoRequestor
						|| state == DatabaseEnums.EmailRequestState.NoContact
						|| state == DatabaseEnums.EmailRequestState.AmbiguousRequestor
						|| state == DatabaseEnums.EmailRequestState.AmbiguousContact
						|| state == DatabaseEnums.EmailRequestState.AmbiguousContactCreation;
		}
		#endregion
	}
}
