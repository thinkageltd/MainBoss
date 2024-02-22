using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.XAF.Database.Layout;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Does the actual work for @Requests - retrieves mail, updates requests status and sends request acknowledgements.
	/// </summary>
	public class RequestorNotificationProcessor : EmailNotificationProcessor {
		#region Constructors, Destructor

		private RequestorNotificationProcessor(IServiceLogging logger, MB3Client dbSession)
			: base(logger, dbSession, true) {
		}
		public static void DoAllRequestorNotifications(IServiceLogging logger, MB3Client dbSession, bool traceActivities, bool traceDetails) {
			using (var x = new RequestorNotificationProcessor(logger, dbSession)) {
				if (x.Unavailable)
					return;
				logger.LogTrace(traceDetails, KB.K("Requestor notifications started").Translate());
				x.DoRequestorNotifications(traceActivities, traceDetails);
				logger.LogTrace(traceDetails, KB.K("Requestor notifications completed").Translate());
			}
		}
		#endregion

		#region DoRequestorNotifications
		/// <summary>
		/// Determine what requests require email acknowledgments to be sent to the Requestor
		/// - checks for changes in requeststatehistory for requests that have requestors associated with them
		/// - sends acknowledgements to requestors about changes to their requests.
		/// </summary>
		public void DoRequestorNotifications(bool traceActivities, bool traceDetails) {
			var requestorError = new Dictionary<string, string>();
			var requestorErrorOnRequest = new Dictionary<string, List<string>>();
			try {
				DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.RequestAcknowledgement, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.PathToReferencedRow,
					dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.F.RequestStateID.PathToReferencedRow,
					dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.F.RequestID.PathToReferencedRow,
					dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.F.RequestID.F.RequestorID.PathToReferencedRow,
					dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.F.RequestID.F.RequestorID.F.ContactID.PathToReferencedRow
				});
				// check each work request record for changes.
				if (dsmb.T.Request.Rows.Count == 0) {
					Logger.LogActivity(Strings.Format(KB.K("No Requestor Notifications Found")));
					return;
				}
				Logger.LogTrace(traceDetails, Strings.Format(KB.K("Processing {0} Requestor Notifications"), dsmb.T.Request.Rows.Count));
				foreach (dsMB.RequestRow requestRow in dsmb.T.Request.Rows) {
					// Get the set of requeststatehistory entry that is to be included in this request's acknowledgement.
					// There must be at least one requeststatehistory record, otherwise the request should have already been filtered out.

					DataRow[] rshrows = dsmb.T.RequestStateHistory.Rows.Select(
						new SqlExpression(dsMB.Path.T.RequestStateHistory.F.RequestID).Eq(requestRow.F.Id),
						new SortExpression(dsMB.Path.T.RequestStateHistory.F.EntryDate, SortExpression.SortOrder.Desc));

					// The last aknowledgement date is the date of the last history record (which is the first in our list)
					DateTime ackdate = ((dsMB.RequestStateHistoryRow)rshrows[0]).F.EntryDate;
					string lastAcknowledgementError = null;

					// create some variables for convenience
					dsMB.RequestorRow requestorRow = requestRow.RequestorIDParentRow;
					dsMB.ContactRow contactRow = requestorRow.ContactIDParentRow;

					// If the contact has an email address and the contact has permission to receive acknowledgements,
					// then send acknowledgement to him/her conveying the message in the requeststatehistory record.

					MailAddress recipient = EmailHelper.EmailAddressFromContact(contactRow, out string noAcknowledgement);
					System.Globalization.CultureInfo preferredLanguage = Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage(contactRow.F.PreferredLanguage);

					if (noAcknowledgement == null) { // Other validations
						if (requestorRow.F.Hidden.HasValue)
							noAcknowledgement = Strings.Format(KB.K("Contact '{0}' is no longer a Requestor"), contactRow.F.Code);
						else if (!requestorRow.F.ReceiveAcknowledgement)
							noAcknowledgement = Strings.Format(KB.K("Contact '{0}' does not want to receive acknowledgments"), contactRow.F.Code);
					}
					if (noAcknowledgement != null) {
						// update the request's LastAcknowledgementDate date with the last entry date from the requeststatehistory for any entry
						// that we cannot contact
						lastAcknowledgementError = Strings.Format(KB.K("{0} skipped to date {1}"), requestRow.F.Number, ackdate);

						if (!requestorErrorOnRequest.ContainsKey(contactRow.F.Code)) {
							requestorError[contactRow.F.Code] = noAcknowledgement;
							requestorErrorOnRequest[contactRow.F.Code] = new List<string>();
						}
						requestorErrorOnRequest[contactRow.F.Code].Add(lastAcknowledgementError);
						requestRow.F.LastRequestorAcknowledgementDate = ackdate;
					}
					else {
						// compose the message header
						using (MailMessage mm = smtp.NewMailMessage(recipient)) {
							mm.Subject = UK.K("RequestorNotificationSubjectPrefix").Translate(preferredLanguage);
							mm.Subject += requestRow.F.Subject.Replace("\r\n", ""); // Exception is thrown if there are newlines in the subject.

							SMTPClient.BuildMailMessageBody(mm, BuildRequestorNotificationMessage(new TextNotificationEmail(preferredLanguage), requestRow, rshrows),
								BuildRequestorNotificationMessage(smtp.GetHtmlEmailBuilder(preferredLanguage), requestRow, rshrows));
							try {
								smtp.Send(mm);
								Logger.LogInfo(Strings.Format(KB.K("Request acknowledgement sent for request {0} to '{1}' with email address '{2}'"), requestRow.F.Number, contactRow.F.Code, mm.To));
								requestRow.F.LastRequestorAcknowledgementDate = ackdate; // acknowledgement sent successfully.
							}
							catch (System.Exception se) {
								var er = EmailHelper.RetrySmtpException(se as SmtpException);
								if (er == EmailHelper.ErrorRecovery.StopProcessing) {
									lastAcknowledgementError = Thinkage.Libraries.Exception.FullMessage(new GeneralException(se, KB.K("Could not contact SMTP server. Request acknowledgments will be deferred")).WithContext(smtp.SmtpServerContext));
									Logger.LogError(lastAcknowledgementError);
									break;
								}
								else if (er == EmailHelper.ErrorRecovery.RetryMessage) {
									lastAcknowledgementError = Thinkage.Libraries.Exception.FullMessage(new GeneralException(se, KB.K("Request acknowledgement will be retried on request {0} to '{1}' with email address '{2}'"), requestRow.F.Number, contactRow.F.Code, mm.To).WithContext(smtp.SmtpServerContext));
									Logger.LogWarning(lastAcknowledgementError);
								}
								else {
									lastAcknowledgementError = Thinkage.Libraries.Exception.FullMessage(new GeneralException(se, KB.K("Request acknowledgement for request {0} to '{1}' with email address '{2}' skipped to date {3}"), requestRow.F.Number, contactRow.F.Code, mm.To, ackdate).WithContext(smtp.SmtpServerContext));
									Logger.LogError(lastAcknowledgementError);
									requestRow.F.LastRequestorAcknowledgementDate = ackdate; // acknowledgement not sent, but this was some other unknown error fatal error so we flag it as done until another state history is added with a newer date
								}
							}
						}
					}
					// Update as we go along in case an error occurs further down the chain. At least the ones we have done will be flagged done and any disabling/skipping of
					// notifications will be recorded.
					requestRow.F.LastRequestorAcknowledgementError = lastAcknowledgementError;
					DB.Update(dsmb);
				}
			}
			catch (MainBossServiceWorkerException) {
				throw;
			}
			catch (System.Exception e) {
				throw new MainBossServiceWorkerException(e, KB.K("Error processing request acknowledgments")).WithContext(smtp.SmtpServerContext);
			}
			finally {
				foreach (var r in requestorError.OrderBy(e => e.Key)) {
					var mess = new StringBuilder();
					mess.AppendLine(r.Value).AppendLine().AppendLine(Strings.Format(KB.K("Request acknowledgement for:"))).AppendLine();
					foreach (var m in requestorErrorOnRequest[r.Key].Take(20))
						mess.AppendLine(m);
					if (requestorErrorOnRequest[r.Key].Count > 20)
						mess.AppendLine().AppendLine(Strings.Format(KB.K("Plus {0} more"), requestorErrorOnRequest[r.Key].Count - 20));
					Logger.LogWarning(mess.ToString());
				}
			}
		}
		#region BuildRequestorNotificationMessage
		private INotificationEmail BuildRequestorNotificationMessage(INotificationEmail builder, dsMB.RequestRow requestRow, DataRow[] rshrows) {
			if (builder == null)
				return null;
			builder.AppendParagraph(UK.K("RequestorNotificationIntroduction").Translate(builder.PreferredLanguage));
			builder.Append(UK.K("ReferenceWorkRequest").Translate(builder.PreferredLanguage));
			builder.AppendLine(Strings.IFormat(" {0}", requestRow.F.Number));
			dsMB.RequestStateHistoryRow currentStateHistory = (dsMB.RequestStateHistoryRow)rshrows[0];
			dsMB.RequestStateRow currentState = currentStateHistory.RequestStateIDParentRow;

			if (currentStateHistory.F.PredictedCloseDate.HasValue) {
				builder.Append(UK.K("EstimatedCompletionDate").Translate(builder.PreferredLanguage));
				builder.AppendLine(Strings.IFormat(" {0}", dsMB.Schema.T.RequestStateHistory.F.PredictedCloseDate.EffectiveType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo).Format(currentStateHistory.F.PredictedCloseDate)));
			}
			if (ServiceConfiguration.MainBossRemoteURL != null && !currentState.F.FilterAsClosed) {
				builder.AppendWebAccessLink(UK.K("RequestAddCommentPreamble").Translate(builder.PreferredLanguage), requestRow.F.Number,
					Strings.IFormat("{0}/Requestor/AddComment?parentID={1}&CurrentStateHistoryID={2}", ServiceConfiguration.MainBossRemoteURL, currentStateHistory.F.RequestID, currentStateHistory.F.Id));
			}
			builder.NewLine();
			// the history comments are included in the body in order of descending effectivedates
			foreach (dsMB.RequestStateHistoryRow rshrow in rshrows) {
				builder.StartViewHistoryItem();
				builder.StartHistoryItemTitle();
				// Note we do not put the name of the commenter in this email; that is considered 'inside' information not typically communicated to external users.
				// This differs from the Assignment notification whereby the information is deemed to come from an insider to an insider receiving the notification.
				builder.Append(dsMB.Schema.T.RequestStateHistory.F.EffectiveDate.EffectiveType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo).Format(rshrow.F.EffectiveDate));
				builder.AppendBlank();
				builder.Append(rshrow.RequestStateIDParentRow.F.Desc.Translate(builder.PreferredLanguage));
				builder.EndHistoryItemTitle();

				builder.StartHistoryItemBody();
				if (rshrow.F.CommentToRequestor != null)
					builder.AppendMultiLine("RequestorComment", rshrow.F.CommentToRequestor);

				builder.EndHistoryItemBody();
				builder.EndViewHistoryItem();
			}

			// todo: optionally include the original request info in the body?
			if (requestRow.F.Description != null) {
				builder.NewLine();
				builder.AppendLine(UK.K("OriginalRequestPreamble").Translate(builder.PreferredLanguage));
				builder.StartViewPanel();
				builder.LabelValueLine(KB.K("Subject"), requestRow.F.Subject);
				builder.LabelValueMultiLine(KB.K("Description"), null, requestRow.F.Description);
				builder.EndViewPanel();
			}
			return builder;
		}
		#endregion
		#endregion
	}
}
