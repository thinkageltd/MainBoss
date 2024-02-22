using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.XAF.Database.Layout;

namespace Thinkage.MainBoss.Database.Service {
	public static class RequestFromEmailActions {
		static readonly Set<object> Rejectable = new Set<object>(new object[] {
			(short) DatabaseEnums.EmailRequestState.NoRequestor ,
			(short) DatabaseEnums.EmailRequestState.NoContact,
			(short) DatabaseEnums.EmailRequestState.AmbiguousRequestor,
			(short) DatabaseEnums.EmailRequestState.AmbiguousContactCreation,
			(short) DatabaseEnums.EmailRequestState.UnProcessed,
			});
		static readonly DatabaseEnums.EmailRequestState[] HasBeenRejected = new DatabaseEnums.EmailRequestState[] {
			DatabaseEnums.EmailRequestState.RejectNoRequestor,
			DatabaseEnums.EmailRequestState.RejectNoContact,
			DatabaseEnums.EmailRequestState.RejectManual,
			DatabaseEnums.EmailRequestState.RejectAmbiguousRequestor,
			DatabaseEnums.EmailRequestState.RejectAmbiguousContact,
			DatabaseEnums.EmailRequestState.RejectAmbiguousContactCreation
		};
		public static void Reject(MB3Client DB, Set<object> EmailRequestIds) { // there is a critical section here, but not important
			if (EmailRequestIds.Count() == 0)
				return;
			using (dsMB ds = new dsMB(DB)) {
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.EmailRequest, (new SqlExpression(dsMB.Path.T.EmailRequest.F.Id).In(SqlExpression.Constant(EmailRequestIds)))
					.And((new SqlExpression(dsMB.Path.T.EmailRequest.F.ProcessingState).In(SqlExpression.Constant(Rejectable)))));
				foreach (dsMB.EmailRequestRow r in ds.T.EmailRequest.Rows)
					r.F.ProcessingState = (short)DatabaseEnums.EmailRequestState.ToBeRejected;
				DB.Update(ds);
			}
		}
		static readonly Set<object> Createable = new Set<object>(new object[] {
			(short) DatabaseEnums.EmailRequestState.NoRequestor ,
			(short) DatabaseEnums.EmailRequestState.NoContact,
			});

		public static void Create(MB3Client DB, Set<object> EmailRequestIds, bool forceCreate) {
			// there is a critical section here, but not important
			// if the user sets one of the email request to create a requestor they should all have to be set.
			// if not the service will create the Requestor when it finds it, any email request earlier will not be processed
			// and any email request after will be.
			// setting the state on all of them will cause them to be processed in order
			// it is still possible to require the two pass effect. If a requestor was asked be be created on a retryable EmailRequest
			// and afterwards a non retryable request is marked retryable. That request will not able to be reprocessed correctly until after
			// the contact is created.
			if (EmailRequestIds.Count() == 0)
				return;
			using (dsMB ds = new dsMB(DB)) {
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.EmailRequest, (new SqlExpression(dsMB.Path.T.EmailRequest.F.Id).In(SqlExpression.Constant(EmailRequestIds)))
					.And((new SqlExpression(dsMB.Path.T.EmailRequest.F.ProcessingState).NEq(SqlExpression.Constant((short)DatabaseEnums.EmailRequestState.Completed)))));
				Set<string> emailAddresses = new Set<string>();
				Set<object> createdContacts = new Set<object>(dsMB.Schema.T.EmailRequest.F.RequestorEmailAddress.EffectiveType); // type object so SqlExpression.Constant will process it
				Dictionary<string, Guid> requestorIDs = new Dictionary<string, Guid>();
				foreach (dsMB.EmailRequestRow r in ds.T.EmailRequest.Rows) {
					var address = r.F.RequestorEmailAddress.ToLower();
					if (!emailAddresses.Contains(address)) {
						var requestorInfo = new AcquireRequestor(DB, new System.Net.Mail.MailAddress(r.F.RequestorEmailAddress, r.F.RequestorEmailDisplayName), forceCreate, forceCreate, forceCreate, null, null, r.F.PreferredLanguage);
						if (requestorInfo.Exception != null) {
							var oldstate = (DatabaseEnums.EmailRequestState)r.F.ProcessingState;
							if (!HasBeenRejected.Any(e => e == oldstate))  //don't want to lose the fact that a reject message has been sent.
								r.F.ProcessingState = (short)requestorInfo.State;
							Application.Instance.DisplayError(requestorInfo.Exception);
						}
						else {
							if (requestorInfo.WarningText != null)
								Application.Instance.DisplayWarning(requestorInfo.WarningText);
							createdContacts.Add(address);
							requestorIDs[address] = requestorInfo.RequestorID.Value;
						}
						emailAddresses.Add(address);
					}
				}
				if (emailAddresses.Count() > 0 && forceCreate && createdContacts.Count > 0)
					ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.EmailRequest, (new SqlExpression(dsMB.Path.T.EmailRequest.F.RequestorEmailAddress).In(SqlExpression.Constant(createdContacts)))
					.And((new SqlExpression(dsMB.Path.T.EmailRequest.F.ProcessingState).In(SqlExpression.Constant(Createable)))));
				foreach (dsMB.EmailRequestRow r in ds.T.EmailRequest.Rows) {
					try {
						if (!requestorIDs.TryGetValue(r.F.RequestorEmailAddress.ToLower(), out Guid requestorId))
							continue;
						System.Globalization.CultureInfo preferredLanguage = Thinkage.Libraries.Translation.MessageBuilder.PreferredLanguage((int?)r.F.PreferredLanguage);
						string commentToRequestor = UK.K("RequestorInitialCommentPreamble").Translate(preferredLanguage);
						ServiceUtilities.CreateRequest(ds, r, requestorId, commentToRequestor);
					}
					catch (System.Exception ex) {
						if (!(ex is GeneralException))
							ex = new GeneralException(ex, KB.K("Request from {0} had internal error. The request was not processed"), r.F.RequestorEmailAddress);
						Application.Instance.DisplayError(ex);
					}
				}
				DB.Update(ds);
			}
		}
	}
}
