using System;
using System.Net.Mail;
using Thinkage.Libraries;
namespace Thinkage.MainBoss.Database.Service {
	static class EmailHelper {
		public static MailAddress EmailAddressFromContact(dsMB.ContactRow contactRow, out string rejectionReason) {
			rejectionReason = null;
			if (String.IsNullOrEmpty(contactRow.F.Email))
				rejectionReason = Strings.Format(KB.K("Contact '{0}' does not have an email address"), contactRow.F.Code);
			else if (contactRow.F.Hidden.HasValue)
				rejectionReason = Strings.Format(KB.K("Contact '{0}' has been deleted"), contactRow.F.Code);
			else
				try {
					return new MailAddress(contactRow.F.Email, contactRow.F.Code);
				}
				catch (System.Exception e) {
					rejectionReason = Strings.Format(KB.K("Contact '{0}' has an invalid email address; '{1}'"), contactRow.F.Code, Thinkage.Libraries.Exception.FullMessage(e));
				}
			return null;
		}
		/// <summary>
		/// Determine which SmtpException codes we should continue to retry sending an email for
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static ErrorRecovery RetrySmtpException(SmtpException e) {
			if (e == null)
				return ErrorRecovery.NoRetry;
			switch (e.StatusCode) {
			case SmtpStatusCode.GeneralFailure:
			case SmtpStatusCode.MustIssueStartTlsFirst:
			case SmtpStatusCode.ClientNotPermitted:
			case SmtpStatusCode.ServiceNotAvailable:
				return ErrorRecovery.StopProcessing;
			case SmtpStatusCode.MailboxBusy:
			case SmtpStatusCode.InsufficientStorage:
			case SmtpStatusCode.TransactionFailed:
			case SmtpStatusCode.MailboxUnavailable:
				return ErrorRecovery.RetryMessage;
			default:
				return ErrorRecovery.NoRetry;
			}
		}
		public enum ErrorRecovery {
			StopProcessing,
			RetryMessage,
			NoRetry
		}
	}
}