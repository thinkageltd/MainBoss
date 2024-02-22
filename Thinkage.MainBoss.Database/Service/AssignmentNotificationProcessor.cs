using System;
using System.Data;
using System.Net.Mail;
using Thinkage.Libraries;
using Thinkage.Libraries.Service;
using Thinkage.Libraries.XAF.Database.Layout;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Does the actual work for processing Assignee notifications.
	/// TODO: Need to have a common base class with RequestProcessor that handles sending of email, and to check the MBSHtmlEmailNotification flag when building response emails
	/// </summary>
	public class AssignmentNotificationProcessor : EmailNotificationProcessor {
		#region Constructor
		private AssignmentNotificationProcessor(IServiceLogging logger, MB3Client dbSession) : base(logger, dbSession, true) { }
		public static void DoAllAssignmentNotifications(IServiceLogging logger, MB3Client dbSession, bool traceActivities, bool traceDetails) {
			using (AssignmentNotificationProcessor x = new AssignmentNotificationProcessor(logger, dbSession)) {
				if (x.Unavailable)
					return;
				logger.LogTrace(traceDetails, KB.K("Assignment notifications started").Translate());
				x.DoAssignmentNotifications(traceActivities, traceDetails);
				logger.LogTrace(traceDetails, KB.K("Assignment notifications completed").Translate());
			}
		}
		#endregion
		#region UpdateWorkOrderAssignmentNotificationTable
		/// <summary>
		/// WorkOrderAssignments include implied assignments from Demand Labor records. For this reason, the LastNotification date is kept in a separate table
		/// that must be updated prior to processing normal notifications.
		/// </summary>
		private void UpdateWorkOrderAssignmentNotificationTable() {
			// This essentially needs to become
			// INSERT INTO WorkOrderAssignmentNotification (ID, WorkOrderAssigneeID, WorkOrderID)
			//		select NEWID(), WorkOrderAssigneeID, WorkOrderID from WorkOrderAssignmentAll as WOAA
			//			where NOT EXISTS (
			//					select * from WorkOrderAssignmentNotification as WOAN
			//						where WOAA.WorkOrderID = WOAN.WorkOrderID and AWOA.WorkOrderAssigneeID = WOAN.WorkOrderAssigneeID
			//			)
			SelectSpecification existingNotificationRecords =
				new SelectSpecification(dsMB.Schema.T.WorkOrderAssignmentNotification,
					new SqlExpression[] {
						new SqlExpression(dsMB.Path.T.WorkOrderAssignmentNotification.F.Id)
					},
					new SqlExpression(dsMB.Path.T.WorkOrderAssignmentNotification.F.WorkOrderAssigneeID).Eq(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID, 1))
						.And(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentNotification.F.WorkOrderID).Eq(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID, 1))),
					null);

			SelectSpecification sourceColumns = new SelectSpecification(dsMB.Schema.T.WorkOrderAssignmentAll,
				new SqlExpression[] {
					SqlExpression.NewId(),
					new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID),
					new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID)
				},
				SqlExpression.Exists(existingNotificationRecords).Not(),
				null);


			DB.Session.ExecuteCommand(new InsertSpecification(dsMB.Schema.T.WorkOrderAssignmentNotification,
				new DBI_Column[] {
					dsMB.Schema.T.WorkOrderAssignmentNotification.F.Id,
					dsMB.Schema.T.WorkOrderAssignmentNotification.F.WorkOrderAssigneeID,
					dsMB.Schema.T.WorkOrderAssignmentNotification.F.WorkOrderID
				},
				sourceColumns));
		}
		#endregion


		#region DoAssignmentNotifications
		/// <summary>
		/// Determine changes to Request/WorkOrder/PurchaseOrder state history since last time an assignee was notified. Send email summarizing all changes to each assignee.
		/// </summary>
		public void DoAssignmentNotifications(bool traceActivities, bool traceDetails) {
			try {
				UpdateWorkOrderAssignmentNotificationTable();

				DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.AssignmentNotification, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestID.F.RequestorID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestID.F.RequestorID.F.ContactID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestID.F.UnitLocationID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestID.F.UnitLocationID.F.RelativeLocationID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestAssigneeID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.RequestAssignmentID.F.RequestAssigneeID.F.ContactID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderID.F.RequestorID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderID.F.RequestorID.F.ContactID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderID.F.UnitLocationID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderID.F.UnitLocationID.F.RelativeLocationID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderAssigneeID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.WorkOrderAssignmentNotificationID.F.WorkOrderAssigneeID.F.ContactID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.PurchaseOrderAssignmentID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.PurchaseOrderAssignmentID.F.PurchaseOrderID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.PurchaseOrderAssignmentID.F.PurchaseOrderAssigneeID.PathToReferencedRow,
					dsMB.Path.T.AssignmentNotification.F.PurchaseOrderAssignmentID.F.PurchaseOrderAssigneeID.F.ContactID.PathToReferencedRow
				});
				// check each work request record for changes.
				if (dsmb.T.AssignmentNotification.Rows.Count == 0) {
					if (traceActivities)
						Logger.LogActivity(Strings.Format(KB.K("No Assignment Notifications found")));
					return;
				}
				Logger.LogTrace(traceDetails, Strings.Format(KB.K("Processing {0} {0.IsOne ? {0} Notification : {0} Notifications }"), dsmb.T.AssignmentNotification.Rows.Count));
				#region Processing Loop
				#region RequestAssignments
				foreach (dsMB.RequestAssignmentRow rarow in dsmb.T.RequestAssignment.Rows) {
					// Ensure no history remains earlier iterations that may end up in someone else's notification
					// we do it at the top to ensure it is done if we stop processing further down due to some error and end up doing a continue. The history
					// wouldn't be cleared at the bottom of the loop.
					if (dsmb.T.RequestStateHistory != null)
						dsmb.T.RequestStateHistory.Clear();

					// Get the set of requeststatehistory entry that is to be included in this request's notification
					// Only get those StateHistory records whose date are gt our current requestassignment's lastnotification date (different requestassignments on same request may have different date/times
					// and we don't want to send the worse case set of state history records to all requestassignee's
					// There must be at least one requeststatehistory record, otherwise the request assignment should have already been filtered out.
					DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.RequestStateHistory, new SqlExpression(dsMB.Path.T.RequestStateHistory.F.RequestID).Eq(rarow.F.RequestID)
						.And(!rarow.F.LastNotificationDate.HasValue ? SqlExpression.Constant(true) : new SqlExpression(dsMB.Path.T.RequestStateHistory.F.EntryDate).Gt(rarow.F.LastNotificationDate)), null, new DBI_PathToRow[] {
							dsMB.Path.T.RequestStateHistory.F.UserID.PathToReferencedRow,
							dsMB.Path.T.RequestStateHistory.F.UserID.F.ContactID.PathToReferencedRow,
							dsMB.Path.T.RequestStateHistory.F.RequestStateID.PathToReferencedRow,
							dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID.PathToReferencedRow
					});
					//Get the Rows fetched above in a sorted order descending order
					DataRow[] rshrows = dsmb.T.RequestStateHistory.Rows.Select(
							SqlExpression.Constant(true),
							new SortExpression(dsMB.Path.T.RequestStateHistory.F.EntryDate, SortExpression.SortOrder.Desc));

					// The last aknowledgement date is the date of the last history record (which is the first in our list)
					DateTime ackdate = ((dsMB.RequestStateHistoryRow)rshrows[0]).F.EntryDate;
					// create some variables for convenience
					dsMB.RequestRow requestRow = rarow.RequestIDParentRow;
					dsMB.RequestAssigneeRow requestAssigneeRow = rarow.RequestAssigneeIDParentRow;
					dsMB.ContactRow contactRow = requestAssigneeRow.ContactIDParentRow;

					// If the contact has an email address and the contact assignee has notifications enabled, send notification
					MailAddress recipient = EmailHelper.EmailAddressFromContact(contactRow, out string noNotification);
					System.Globalization.CultureInfo preferredLanguage = Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage(contactRow.F.PreferredLanguage);

					if (noNotification == null) { // Other validations
						if (!requestAssigneeRow.F.ReceiveNotification)
							noNotification = Strings.Format(KB.K("Contact '{0}' does receive request notifications"), contactRow.F.Code);
					}
					if (noNotification != null) {
						// update the RequestAssignment last notification if we cannot notify the assignee
						Logger.LogWarning(Strings.Format(KB.K("Notification skipped for request {0} to {1}:\r\n{2}"), requestRow.F.Number, contactRow.F.Code, noNotification));
						rarow.F.LastNotificationDate = ackdate;
					}
					else {
						using (MailMessage mm = smtp.NewMailMessage(recipient)) {
							mm.Subject = UK.K("ANRequestSubjectPrefix").Translate(preferredLanguage);
							mm.Subject += requestRow.F.Subject.Replace("\r\n", ""); // Exception is thrown if there are newlines in the subject.
							InitialNotificationInformation iInfo = null;
							if (!rarow.F.LastNotificationDate.HasValue) {
								//Requestors are required on requests; Units are optional
								dsMB.ContactRow requestorContactInfo = requestRow.RequestorIDParentRow.ContactIDParentRow;
								ContactInformation rInfo = new ContactInformation(requestorContactInfo.F.Hidden.HasValue, requestorContactInfo.F.Code, requestorContactInfo.F.Email, requestorContactInfo.F.BusinessPhone);
								UnitInformation uInfo = !requestRow.F.UnitLocationID.HasValue ? null :
															new UnitInformation(requestRow.UnitLocationIDParentRow.RelativeLocationIDParentRow.F.Hidden.HasValue, requestRow.UnitLocationIDParentRow.F.Code);
								iInfo = new InitialNotificationInformation(requestRow.F.Description, uInfo, rInfo);
							}

							SMTPClient.BuildMailMessageBody(mm, BuildRequestNotificationEmail(new TextNotificationEmail(preferredLanguage), dsmb, requestRow, iInfo, rshrows),
								BuildRequestNotificationEmail(smtp.GetHtmlEmailBuilder(preferredLanguage), dsmb, requestRow, iInfo, rshrows));
							try {
								smtp.Send(mm);
								Logger.LogInfo(Strings.Format(KB.K("Notification sent for request {0} to {1}"), requestRow.F.Number, mm.To));
								rarow.F.LastNotificationDate = ackdate; // acknowledgement sent successfully.
							}
							catch (System.Exception se) {
								var er = EmailHelper.RetrySmtpException(se as SmtpException);
								if (er == EmailHelper.ErrorRecovery.StopProcessing) {
									Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(se, KB.K("Could not contact SMTP server. Request Assignment Notifications will be deferred")).WithContext(smtp.SmtpServerContext)));
									break;
								}
								else if (er == EmailHelper.ErrorRecovery.RetryMessage) {
									Logger.LogWarning(Thinkage.Libraries.Exception.FullMessage(
										new GeneralException(se, KB.K("Notification will be retried on request {0} to '{1}' with email address '{2}'"), requestRow.F.Number, contactRow.F.Code, mm.To).WithContext(smtp.SmtpServerContext)));
								}
								else {
									Logger.LogWarning(Thinkage.Libraries.Exception.FullMessage(
										new GeneralException(se, KB.K("Notification for request {0} to '{1}' with email address '{2}' skipped to date {3}"), requestRow.F.Number, contactRow.F.Code, mm.To, ackdate).WithContext(smtp.SmtpServerContext)));
									rarow.F.LastNotificationDate = ackdate; // acknowledgement not sent, but this was some other unknown error fatal error so we flag it as done until another state history is added with a newer date
								}
							}
						}
					}
					// Update as we go along in case an error occurs further down the chain. At least the ones we have done will be flagged done.
					DB.Update(dsmb);
				}
				#endregion

				#region WorkOrderAssignments
				foreach (dsMB.WorkOrderAssignmentNotificationRow rarow in dsmb.T.WorkOrderAssignmentNotification.Rows) {
					// Ensure no history remains earlier iterations that may end up in someone else's notification
					// we do it at the top to ensure it is done if we stop processing further down due to some error and end up doing a continue. The history
					// wouldn't be cleared at the bottom of the loop.
					if (dsmb.T.WorkOrderStateHistory != null)
						dsmb.T.WorkOrderStateHistory.Clear();

					// See rules in RequestAssignments for what we are fetching
					DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.WorkOrderStateHistory, new SqlExpression(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID).Eq(rarow.F.WorkOrderID)
						.And(!rarow.F.LastNotificationDate.HasValue ? SqlExpression.Constant(true) : new SqlExpression(dsMB.Path.T.WorkOrderStateHistory.F.EntryDate).Gt(rarow.F.LastNotificationDate)), null, new DBI_PathToRow[] {
							dsMB.Path.T.WorkOrderStateHistory.F.UserID.PathToReferencedRow,
							dsMB.Path.T.WorkOrderStateHistory.F.UserID.F.ContactID.PathToReferencedRow,
							dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.PathToReferencedRow,
							dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID.PathToReferencedRow
					});
					//Get the Rows fetched above in a sorted descending entry date order
					DataRow[] shrows = dsmb.T.WorkOrderStateHistory.Rows.Select(
							SqlExpression.Constant(true),
							new SortExpression(dsMB.Path.T.WorkOrderStateHistory.F.EntryDate, SortExpression.SortOrder.Desc));
					// The last aknowledgement date is the date of the last history record (which is the first in our list)
					DateTime ackdate = ((dsMB.WorkOrderStateHistoryRow)shrows[0]).F.EntryDate;

					// create some variables for convenience
					dsMB.WorkOrderRow orderRow = rarow.WorkOrderIDParentRow;
					dsMB.WorkOrderAssigneeRow orderAssigneeRow = rarow.WorkOrderAssigneeIDParentRow;
					dsMB.ContactRow contactRow = orderAssigneeRow.ContactIDParentRow;

					// If the contact has an email address and the contact assignee has notifications enabled, send notification
					MailAddress recipient = EmailHelper.EmailAddressFromContact(contactRow, out string noNotification);
					System.Globalization.CultureInfo preferredLanguage = Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage(contactRow.F.PreferredLanguage);

					if (noNotification == null) { // Other validations
						if (!orderAssigneeRow.F.ReceiveNotification)
							noNotification = Strings.Format(KB.K("Contact '{0}' does not receive work order notifications"), contactRow.F.Code);
					}
					if (noNotification != null) {
						// update the WorkOrderAssignment last notification date if we cannot contact assignee
						Logger.LogWarning(Strings.Format(KB.K("Notification skipped for work order {0} to {1}:\r\n{2}"), orderRow.F.Number, contactRow.F.Code, noNotification));
						rarow.F.LastNotificationDate = ackdate;
					}
					else {
						using (MailMessage mm = smtp.NewMailMessage(recipient)) {
							mm.Subject = UK.K("ANWorkOrderSubjectPrefix").Translate(preferredLanguage);
							mm.Subject += orderRow.F.Subject.Replace("\r\n", ""); // Exception is thrown if there are newlines in the subject.
							InitialNotificationInformation iInfo = null;
							if (!rarow.F.LastNotificationDate.HasValue) {
								// requestors are optional on workorders; units are not
								ContactInformation rInfo = null;
								UnitInformation uInfo = null;
								if (orderRow.F.RequestorID.HasValue) {
									dsMB.ContactRow requestorContactInfo = orderRow.RequestorIDParentRow.ContactIDParentRow;
									rInfo = new ContactInformation(requestorContactInfo.F.Hidden.HasValue, requestorContactInfo.F.Code, requestorContactInfo.F.Email, requestorContactInfo.F.BusinessPhone);
								}
								uInfo = new UnitInformation(orderRow.UnitLocationIDParentRow.RelativeLocationIDParentRow.F.Hidden.HasValue, orderRow.UnitLocationIDParentRow.F.Code);
								iInfo = new InitialNotificationInformation(orderRow.F.Description, uInfo, rInfo);
							}
							SMTPClient.BuildMailMessageBody(mm, BuildWorkOrderNotificationEmail(new TextNotificationEmail(preferredLanguage), preferredLanguage, dsmb, orderRow, iInfo, shrows),
								BuildWorkOrderNotificationEmail(smtp.GetHtmlEmailBuilder(preferredLanguage), preferredLanguage, dsmb, orderRow, iInfo, shrows));
							try {
								smtp.Send(mm);
								Logger.LogInfo(Strings.Format(KB.K("Notification sent for work order {0} to {1}"), orderRow.F.Number, mm.To));
								rarow.F.LastNotificationDate = ackdate; // acknowledgement sent successfully.
							}
							catch (System.Exception se) {
								var er = EmailHelper.RetrySmtpException(se as SmtpException);
								if (er == EmailHelper.ErrorRecovery.StopProcessing) {
									Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(se, KB.K("Could not contact SMTP server. Work Order Assignment Notifications will be deferred")).WithContext(smtp.SmtpServerContext)));
									break;
								}
								else if (er == EmailHelper.ErrorRecovery.RetryMessage) {
									Logger.LogWarning((Thinkage.Libraries.Exception.FullMessage(
										new GeneralException(se, KB.K("Notification will be retried on work order {0} to '{1}' with email address '{2}'"), orderRow.F.Number, contactRow.F.Code, mm.To).WithContext(smtp.SmtpServerContext))));
								}
								else {
									Logger.LogError(Thinkage.Libraries.Exception.FullMessage(
										new GeneralException(se, KB.K("Notification for work order {0} to '{1}' with email address '{2}' skipped to date {3}"), orderRow.F.Number, contactRow.F.Code, mm.To, ackdate).WithContext(smtp.SmtpServerContext)));
									rarow.F.LastNotificationDate = ackdate; // acknowledgement not sent, but this was some other unknown error fatal error so we flag it as done until another state history is added with a newer date
								}
							}
						}
					}
					// Update as we go along in case an error occurs further down the chain. At least the ones we have done will be flagged done.
					DB.Update(dsmb);
				}
				#endregion
				#region PurchaseOrderAssignments
				foreach (dsMB.PurchaseOrderAssignmentRow rarow in dsmb.T.PurchaseOrderAssignment.Rows) {
					// Ensure no history remains earlier iterations that may end up in someone else's notification
					// we do it at the top to ensure it is done if we stop processing further down due to some error and end up doing a continue. The history
					// wouldn't be cleared at the bottom of the loop.
					if (dsmb.T.PurchaseOrderStateHistory != null)
						dsmb.T.PurchaseOrderStateHistory.Clear();

					// See rules in RequestAssignments for what we are fetching
					DB.ViewAdditionalRows(dsmb, dsMB.Schema.T.PurchaseOrderStateHistory, new SqlExpression(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID).Eq(rarow.F.PurchaseOrderID)
						.And(!rarow.F.LastNotificationDate.HasValue ? SqlExpression.Constant(true) : new SqlExpression(dsMB.Path.T.PurchaseOrderStateHistory.F.EntryDate).Gt(rarow.F.LastNotificationDate)), null, new DBI_PathToRow[] {
							dsMB.Path.T.PurchaseOrderStateHistory.F.UserID.PathToReferencedRow,
							dsMB.Path.T.PurchaseOrderStateHistory.F.UserID.F.ContactID.PathToReferencedRow,
							dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.PathToReferencedRow,
							dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID.PathToReferencedRow
					});
					//Get the Rows fetched above in a sorted order descending order
					DataRow[] rshrows = dsmb.T.PurchaseOrderStateHistory.Rows.Select(
							SqlExpression.Constant(true),
							new SortExpression(dsMB.Path.T.PurchaseOrderStateHistory.F.EntryDate, SortExpression.SortOrder.Desc));
					// The last aknowledgement date is the date of the last history record (which is the first in our list)
					DateTime ackdate = ((dsMB.PurchaseOrderStateHistoryRow)rshrows[0]).F.EntryDate;

					// create some variables for convenience
					dsMB.PurchaseOrderRow orderRow = rarow.PurchaseOrderIDParentRow;
					dsMB.PurchaseOrderAssigneeRow orderAssigneeRow = rarow.PurchaseOrderAssigneeIDParentRow;
					dsMB.ContactRow contactRow = orderAssigneeRow.ContactIDParentRow;

					// If the contact has an email address and the contact assignee has notifications enabled, send notification
					MailAddress recipient = EmailHelper.EmailAddressFromContact(contactRow, out string noNotification);
					System.Globalization.CultureInfo preferredLanguage = Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage(contactRow.F.PreferredLanguage);

					if (noNotification == null) { // Other validations
						if (!orderAssigneeRow.F.ReceiveNotification)
							noNotification = Strings.Format(KB.K("Contact '{0}' does not receive purchase order notifications"), contactRow.F.Code);
					}
					if (noNotification != null) {
						// update the PurchaseOrderAssignment last notification date if we cannot contact assignee
						Logger.LogWarning(Strings.Format(KB.K("Notification skipped for purchase order {0} to {1}:\r\n{2}"), orderRow.F.Number, contactRow.F.Code, noNotification));
						rarow.F.LastNotificationDate = ackdate;
					}
					else {
						using (MailMessage mm = smtp.NewMailMessage(recipient)) {
							mm.Subject = UK.K("ANPurchaseOrderSubjectPrefix").Translate(preferredLanguage);
							mm.Subject += orderRow.F.Subject.Replace("\r\n", ""); // Exception is thrown if there are newlines in the subject.
							SMTPClient.BuildMailMessageBody(mm, BuildPurchaseOrderNotificationEmail(new TextNotificationEmail(preferredLanguage), dsmb, orderRow, rshrows),
								BuildPurchaseOrderNotificationEmail(smtp.GetHtmlEmailBuilder(preferredLanguage), dsmb, orderRow, rshrows));
							try {
								smtp.Send(mm);
								Logger.LogInfo(Strings.Format(KB.K("Notification sent for purchase order {0} to {1}"), orderRow.F.Number, mm.To));
								rarow.F.LastNotificationDate = ackdate; // acknowledgement sent successfully.
							}
							catch (System.Exception se) {
								var er = EmailHelper.RetrySmtpException(se as SmtpException);
								if (er == EmailHelper.ErrorRecovery.StopProcessing) {
									Logger.LogError(Thinkage.Libraries.Exception.FullMessage(new GeneralException(se, KB.K("Could not contact SMTP server. Purchase Order Notifications will be deferred")).WithContext(smtp.SmtpServerContext)));
									break;
								}
								else if (er == EmailHelper.ErrorRecovery.RetryMessage) {
									Logger.LogWarning(Thinkage.Libraries.Exception.FullMessage(
										new GeneralException(se, KB.K("Notification will be retried on purchase order {0} to '{1}' with email address '{2}'"), orderRow.F.Number, contactRow.F.Code, mm.To).WithContext(smtp.SmtpServerContext)));
								}
								else {
									Logger.LogError(Thinkage.Libraries.Exception.FullMessage(
										new GeneralException(se, KB.K("Notification for purchase order {0} to '{1}' with email address '{2}' skipped to date {3}"), orderRow.F.Number, contactRow.F.Code, mm.To, ackdate).WithContext(smtp.SmtpServerContext)));
									rarow.F.LastNotificationDate = ackdate; // acknowledgement not sent, but this was some other unknown error fatal error so we flag it as done until another state history is added with a newer date
								}
							}
						}
					}
					// Update as we go along in case an error occurs further down the chain. At least the ones we have done will be flagged done.
					DB.Update(dsmb);
				}
				#endregion
				#endregion
			}
			catch (MainBossServiceWorkerException) {
				throw;
			}
			catch (System.Exception e) {
				throw new MainBossServiceWorkerException(e, KB.K("Error processing Assignment Notifications")).WithContext(smtp.SmtpServerContext);
			}
		}
		#endregion

		#region Message formatting
		private void CommonNotificationEmailHeader(INotificationEmail builder, string number, string subject, string introduction, InitialNotificationInformation initialInfo) {
			if (introduction != null)
				builder.AppendParagraph(introduction);
			builder.StartViewPanel();
			builder.LabelValueLine(KB.K("Number"), number);
			builder.LabelValueLine(KB.K("Subject"), subject);
			if (initialInfo != null)
				builder.AppendInitialInformation(initialInfo);
			builder.EndViewPanel();
		}
		private INotificationEmail BuildRequestNotificationEmail(INotificationEmail builder, dsMB lookupDs, dsMB.RequestRow requestRow, InitialNotificationInformation iInfo, DataRow[] historyRows) {
			if (builder == null)
				return null;
			CommonNotificationEmailHeader(builder, requestRow.F.Number, requestRow.F.Subject, UK.K("ANRequestIntroduction").Translate(builder.PreferredLanguage), iInfo);
			// the history comments are included in the body in order of descending effectivedates
			foreach (dsMB.RequestStateHistoryRow rshrow in historyRows) {
				builder.StartViewHistoryItem();
				builder.StartHistoryItemTitle();
				builder.Append(dsMB.Schema.T.RequestStateHistory.F.EffectiveDate.EffectiveType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo).Format(rshrow.F.EffectiveDate));
				if (rshrow.UserIDParentRow != null) {
					builder.AppendBlank();
					builder.Append(rshrow.UserIDParentRow.ContactIDParentRow.F.Code);
				}
				//%=Html.Encode(h.RequestState.Code)%><%if (h.RequestStateHistoryStatus != null) {%>&nbsp;<div class="Alert"><%=Html.Encode(h.RequestStateHistoryStatus.Code)%></div><%} %>
				if (rshrow.RequestStateIDParentRow != null) {
					builder.AppendBlank();
					builder.Append(rshrow.RequestStateIDParentRow.F.Code.Translate(builder.PreferredLanguage));
				}
				if (rshrow.RequestStateHistoryStatusIDParentRow != null) {
					builder.AppendBlank();
					builder.AppendAlert(rshrow.RequestStateHistoryStatusIDParentRow.F.Code);
				}
				builder.EndHistoryItemTitle();

				builder.StartHistoryItemBody();
				if (rshrow.F.CommentToRequestor != null)
					builder.AppendMultiLine("RequestorComment", rshrow.F.CommentToRequestor);

				if (rshrow.F.Comment != null) {
					builder.AppendMultiLine(null, rshrow.F.Comment);
				}
				builder.EndHistoryItemBody();
				builder.EndViewHistoryItem();
			}
			if (!string.IsNullOrEmpty(ServiceConfiguration.MainBossRemoteURL)) {
				builder.AppendWebAccessLink(UK.K("RequestURLPreamble").Translate(builder.PreferredLanguage), requestRow.F.Number,
					Strings.IFormat("{0}/Request/View/{1}", ServiceConfiguration.MainBossRemoteURL, requestRow.F.Id));
			}
			return builder;
		}

		private INotificationEmail BuildWorkOrderNotificationEmail(INotificationEmail builder, System.Globalization.CultureInfo preferredLanguage, dsMB lookupDs, dsMB.WorkOrderRow orderRow, InitialNotificationInformation initialInfo, DataRow[] historyRows) {
			if (builder == null)
				return null;
			CommonNotificationEmailHeader(builder, orderRow.F.Number, orderRow.F.Subject, UK.K("ANWorkOrderIntroduction").Translate(builder.PreferredLanguage), initialInfo);
			// the history comments are included in the body in order of descending effectivedates
			foreach (dsMB.WorkOrderStateHistoryRow woshrow in historyRows) {
				builder.StartViewHistoryItem();
				builder.StartHistoryItemTitle();
				builder.Append(dsMB.Schema.T.WorkOrderStateHistory.F.EffectiveDate.EffectiveType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo).Format(woshrow.F.EffectiveDate));
				if (woshrow.UserIDParentRow != null) {
					builder.AppendBlank();
					builder.Append(woshrow.UserIDParentRow.ContactIDParentRow.F.Code);
				}
				if (woshrow.WorkOrderStateIDParentRow != null) {
					builder.AppendBlank();
					builder.Append(woshrow.WorkOrderStateIDParentRow.F.Code.Translate(builder.PreferredLanguage));
				}
				if (woshrow.WorkOrderStateHistoryStatusIDParentRow != null) {
					builder.AppendBlank();
					builder.AppendAlert(woshrow.WorkOrderStateHistoryStatusIDParentRow.F.Code);
				}
				builder.EndHistoryItemTitle();

				builder.StartHistoryItemBody();
				if (woshrow.F.Comment != null)
					builder.AppendMultiLine(null, woshrow.F.Comment);

				builder.EndHistoryItemBody();
				builder.EndViewHistoryItem();
			}
			if (!string.IsNullOrEmpty(ServiceConfiguration.MainBossRemoteURL)) {
				builder.AppendWebAccessLink(UK.K("WorkOrderURLPreamble").Translate(builder.PreferredLanguage), orderRow.F.Number,
					Strings.IFormat("{0}/WorkOrder/View/{1}", ServiceConfiguration.MainBossRemoteURL, orderRow.F.Id));
			}
			return builder;
		}


		private INotificationEmail BuildPurchaseOrderNotificationEmail(INotificationEmail builder, dsMB lookupDs, dsMB.PurchaseOrderRow orderRow, DataRow[] historyRows) {
			if (builder == null)
				return null;
			CommonNotificationEmailHeader(builder, orderRow.F.Number, orderRow.F.Subject, UK.K("ANPurchaseOrderIntroduction").Translate(builder.PreferredLanguage), null);
			// the history comments are included in the body in order of descending effectivedates
			foreach (dsMB.PurchaseOrderStateHistoryRow poshrow in historyRows) {
				builder.StartViewHistoryItem();
				builder.StartHistoryItemTitle();
				builder.Append(dsMB.Schema.T.PurchaseOrderStateHistory.F.EffectiveDate.EffectiveType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceFormatCultureInfo).Format(poshrow.F.EffectiveDate));
				if (poshrow.UserIDParentRow != null) {
					builder.AppendBlank();
					builder.Append(poshrow.UserIDParentRow.ContactIDParentRow.F.Code);
				}
				if (poshrow.PurchaseOrderStateIDParentRow != null) {
					builder.AppendBlank();
					builder.Append(poshrow.PurchaseOrderStateIDParentRow.F.Code.Translate(builder.PreferredLanguage));
				}
				if (poshrow.PurchaseOrderStateHistoryStatusIDParentRow != null) {
					builder.AppendBlank();
					builder.AppendAlert(poshrow.PurchaseOrderStateHistoryStatusIDParentRow.F.Code);
				}
				builder.EndHistoryItemTitle();

				builder.StartHistoryItemBody();
				if (poshrow.F.Comment != null)
					builder.AppendMultiLine(null, poshrow.F.Comment);

				builder.EndHistoryItemBody();
				builder.EndViewHistoryItem();
			}
#if MAINBOSSREMOTEHASPURCHASEORDERS
			if (!string.IsNullOrEmpty(ServiceConfiguration.MainBossRemoteURL)) {
				builder.AppendWebAccessLink(UK.K("PurchaseOrderURLPreamble").Translate(builder.PreferredLanguage), orderRow.F.Number,
					Strings.IFormat("{0}PurchaseOrder/View/{1}", ServiceConfiguration.MainBossRemoteURL, orderRow.F.Id));
			}
#endif
			return builder;
		}
		#endregion
	}
}
