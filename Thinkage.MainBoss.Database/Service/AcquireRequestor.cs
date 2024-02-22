using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {
	#region AcquireRequestor
	public class AcquireRequestor {
		//
		// Because there is a need for two classes of error. The error suitable for the MainBoss Administration giving information about how to fix the problem
		// and a generic error for the user. Exceptions can not be used to report errors.
		// Instead a successful run had the variable RequestID set
		// Any exception will be in variable Exception and contains the message for the MainBoss Administrator further more the variable  UserText will contain the error message for the end user 
		// WarningText contains any warning messages for the MainBoss Administrator
		// Assuming no error then InfoText with contain generic information messages for the MainBoss Administrator
		protected MailAddress EmailAddress;
		protected bool CreateRequestors = false;
		protected bool CreateFromLDAP = false;
		protected bool CreateFromEmail = false;
		protected Regex AcceptRegex = null;
		protected Regex RejectRegex = null;
		private IEnumerable<LDAPEntry> LDAPUsers = null;
		public System.Exception Exception = null;
		public string WarningText = null;
		public string InfoText = null;
		public ErrorToRequestor ErrorToRequestor = ErrorToRequestor.None;
		public string UserText {
			get {
				Key[] requestorUserText = new Key[] { KB.K("Your request cannot be accepted because no email address was found"),
					KB.K("Your request using email address {0} cannot be accepted. Please contact Maintenance by some other means."),
					KB.K("A Request could not be created because the email address '{0}' does not match the email address of any Requestor"),
					KB.K("A Request could not be created because the email address '{0}' is not unique to one Requestor"),
					KB.K("A Request could not be created because a unique Contact could not be found for email address '{0}'")
				};

				if (ErrorToRequestor == ErrorToRequestor.None)
					return null;
				int format = string.IsNullOrWhiteSpace(EmailAddress.Address) ? (int)ErrorToRequestor.None : (int)ErrorToRequestor;
				return Strings.Format(requestorUserText[format], EmailAddress);
			}
		}
		public DatabaseEnums.EmailRequestState State = DatabaseEnums.EmailRequestState.NoRequestor;
		public Guid? RequestorID = null;
		public string ContactCode = null;
		public string ContactEmail = null;

		public AcquireRequestor() { }

		public AcquireRequestor(DBClient DB, MailAddress emailAddress, bool createRequestors, bool createFromLDAP, bool createFromEmail, Regex acceptRegex, Regex rejectRegex, int? preferredLanguage) {
			EmailAddress = emailAddress;
			CreateRequestors = createRequestors;
			CreateFromLDAP = createFromLDAP;
			CreateFromEmail = createFromEmail;
			AcceptRegex = acceptRegex;
			RejectRegex = rejectRegex;
			try {
				FindRequestor(DB, preferredLanguage);
			}
			catch (System.Exception ex) {
				if (Exception == null)
					Exception = ex;
				if (ErrorToRequestor == ErrorToRequestor.None)
					ErrorToRequestor = ErrorToRequestor.Unknown;
			}
			if (Exception == null)
				State = DatabaseEnums.EmailRequestState.Completed;
			else if (!(Exception is GeneralException))
				Exception = new GeneralException(Exception, KB.K("Unable to resolve '{0}' to a Requestor"), emailAddress);

		}
		protected void FindRequestor(DBClient DB, int? preferredLanguage) {
			using (dsMB ds = new dsMB(DB)) {
				ds.EnsureDataTableExists(dsMB.Schema.T.Requestor, dsMB.Schema.T.ActiveRequestor, dsMB.Schema.T.Contact);
				var RequestorRow = FetchRequestorInformation(ds);
				if (RequestorRow == null && Exception == null && CreateRequestors) {
					var accept = AcceptRegex == null || AcceptRegex.Match(EmailAddress.Address).Length >= 1;
					var reject = RejectRegex != null && RejectRegex.Match(EmailAddress.Address).Length >= 1;
					if (!accept)
						RequestorError(DatabaseEnums.EmailRequestState.RejectNoRequestor, ErrorToRequestor.NotFound, KB.K("Email Address '{0}' does not match the email address of any Requestor, and a Requestor was not created because the email address failed to match the accept auto create email address pattern"), EmailAddress.Address);
					else if (reject)
						RequestorError(DatabaseEnums.EmailRequestState.RejectNoRequestor, ErrorToRequestor.NotFound, KB.K("Email Address '{0}' does not match the email address of any Requestor, and a Requestor was not created because the email address matched the reject auto create email address pattern"), EmailAddress.Address);
					if (ResurrectOrCreateRequestor(ds, LDAPUsers, preferredLanguage))
						RequestorRow = FetchRequestorInformation(ds); // See if we have one now
				}
				if (RequestorRow == null)
					RequestorError(DatabaseEnums.EmailRequestState.RejectNoRequestor, ErrorToRequestor.NotFound, KB.K("Email Address '{0}' does not match the email address of any Requestor"), EmailAddress.Address);
				RequestorID = RequestorRow.F.Id;
			}
		}
		#region FetchRequestorInformation
		private dsMB.RequestorRow FetchRequestorInformation(dsMB ds) {
			var from = EmailAddress.Address;
			var source = KB.K("Requestors {0} have the same email address {1}");
			// first check if email address from contacts matches any from address
			//
			List<dsMB.ActiveRequestorRow> found = new List<dsMB.ActiveRequestorRow>();
			ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.ActiveRequestor, new SqlExpression(dsMB.Path.T.ActiveRequestor.F.ContactID.F.Email).Eq(from.ToLower())
								.Or(new SqlExpression(dsMB.Path.T.ActiveRequestor.F.ContactID.F.AlternateEmail).Like(SqlExpression.Constant(Strings.IFormat("%{0}%", from)))), null
								, new DBI_PathToRow[] {
										dsMB.Path.T.ActiveRequestor.F.ContactID.PathToReferencedRow,
										dsMB.Path.T.ActiveRequestor.F.RequestorID.PathToReferencedRow
									});
			//
			// look for primary email addresses first
			foreach (dsMB.ActiveRequestorRow row in ds.T.ActiveRequestor.Rows)
				if (string.Compare(row.ContactIDParentRow.F.Email, from, true) == 0)
					found.Add(row);
			//
			// if no primary email address look for alternate email addresses
			// the email address has to be an exact case match anywhere in the string for Alternate mails.
			// and the text before or after cannot be in an email address.
			// the funny characters came from the standard, but may not be in the domain address
			//
			if (found.Count == 0) {
				source = KB.K("Requestors {0} have the same alternate email address '{1}'");
				foreach (dsMB.ActiveRequestorRow row in ds.T.ActiveRequestor.Rows)
					if (ServiceUtilities.CheckAlternateEmail(row.ContactIDParentRow.F.AlternateEmail, from))
						found.Add(row);
			}
			// 
			// if the contact does not have a email address matching try active directory
			//
			if (found.Count == 0) {
				source = KB.K("Requestors {0} have the same email address '{1}' in Active Directory");
				try {
					LDAPUsers = LDAPEntry.GetActiveDirectoryUsingEmail(from);
					if (LDAPUsers.Count() > 1) {
						var names = LDAPUsers.Select(e => e.UserPrincipalName);
						var namesAsString = string.Join(KB.I(", "), names.OrderBy(e => e.ToLower()).Select(e => Strings.IFormat("'{0}'", e)));
						WarningText = Strings.Format(KB.K("Users {0} in the Active Directory have the same email address '{1}'"), namesAsString, from);
					}
					foreach (var adUser in LDAPUsers) {
						var LDAPGuid = adUser.Guid;
						var LDAPEmail = adUser.Mail;
						var test = new SqlExpression(dsMB.Path.T.ActiveRequestor.F.ContactID.F.LDAPGuid).Eq(LDAPGuid);
						if (LDAPEmail != from) // 
							test = test.Or(new SqlExpression(dsMB.Path.T.ActiveRequestor.F.ContactID.F.Email).Eq(LDAPEmail));
						ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.ActiveRequestor, new SqlExpression(dsMB.Path.T.ActiveRequestor.F.ContactID.F.LDAPGuid).Eq(LDAPGuid), null, new DBI_PathToRow[] {
												dsMB.Path.T.ActiveRequestor.F.ContactID.PathToReferencedRow,
												dsMB.Path.T.ActiveRequestor.F.RequestorID.PathToReferencedRow
											});
					}
				}
				catch (System.Exception e) {
					WarningText = Strings.Format(KB.K("Cannot access Active Directory to check email address {0}. Details: {1}"), EmailAddress, Thinkage.Libraries.Exception.FullMessage(e));
				}
				foreach (dsMB.ActiveRequestorRow row in ds.T.ActiveRequestor.Rows)
					found.Add(row);
			}
			if (found.Count == 0)
				return null;
			if (found.Count == 1) {
				// if the contact does not contain the email address that we found it by (as it possible could if we found the contact by using LDAP) update to contain the email address.
				var r = found.First();
				if (r.ContactIDParentRow.F.Email != from && !ServiceUtilities.CheckAlternateEmail(r.ContactIDParentRow.F.AlternateEmail, from)) {
					if (r.ContactIDParentRow.F.Email == null)
						r.ContactIDParentRow.F.Email = from;
					else if (r.ContactIDParentRow.F.AlternateEmail == null)
						r.ContactIDParentRow.F.AlternateEmail = from;
					else
						r.ContactIDParentRow.F.AlternateEmail = Strings.IFormat("{0} {1}", r.ContactIDParentRow.F.AlternateEmail, from);
					ds.DB.Update(ds);
				}
				ContactCode = r.ContactIDParentRow.F.Code;
				ContactEmail = r.ContactIDParentRow.F.Email;
				return r.RequestorIDParentRow;
			}
			var ordered = found.OrderBy(e => e.ContactIDParentRow.F.Code.ToLower());
			var requestor = ordered.First().RequestorIDParentRow;
			RequestorError(DatabaseEnums.EmailRequestState.AmbiguousRequestor, ErrorToRequestor.Multiple, source, ValuesAsString(", ", ordered.Select(e => e.ContactIDParentRow.F.Code)), from);
			return null;
		}
		#endregion
		#region ResurrectOrCreateRequestor
		private bool ResurrectOrCreateRequestor(dsMB ds, IEnumerable<LDAPEntry> LDAPUsers, int? preferredLanguage) {
			//
			// if there are no Active contact records that currently match our email address
			//		make one (may get error if contact name exists
			// else if there are > 1 Active contact records that match our email address
			//		consider this an error condition and quit
			// else Retrieve all the Requestor records for the existing contact record.
			// if (no requestor records)
			//		make one
			// else
			//		use first active requestor record (or restore a hidden one and use that)
			//
			if (!CreateFromEmail && !CreateFromLDAP && !CreateRequestors)
				return false;
			var contactRow = new AcquireContact(ds.DB, LDAPUsers, EmailAddress, CreateFromLDAP, CreateFromEmail, preferredLanguage);
			InfoText = contactRow.InfoText;
			WarningText = contactRow.WarningText;
			if (contactRow.Exception != null) {
				State = contactRow.State;
				Exception = contactRow.Exception;
				throw Exception;
			}
			CreateRequestor(ds, contactRow.ContactID.Value, contactRow.ContactCode, EmailAddress);
			return true;
		}
		#endregion
		#region CreateRequestor
		public void CreateRequestor(dsMB ds, Guid contactid, string contactcode, System.Net.Mail.MailAddress from) {
			try {
				// Find all existing Requestors linked to this contact 
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.Requestor, new SqlExpression(dsMB.Path.T.Requestor.F.ContactID).Eq(SqlExpression.Constant(contactid)), null
					, new DBI_PathToRow[] {
						dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow
						});
				if (ds.T.Requestor.Rows.Count == 0) { // none, create a new one.
					dsMB.RequestorRow requestor = ds.T.Requestor.AddNewRow();
					requestor.F.ContactID = contactid;
					requestor.F.ReceiveAcknowledgement = true; // if we are creating the requestor, they are going to get an acknowledge despite any setting in the defaults for a Requestor record
					requestor.F.Comment = Strings.Format(KB.K("Created by MainBoss on {0}"), DateTime.Now);
					InfoText = Strings.Format(KB.K("Creating a Requestor from '{0}' with Contact Code '{1}'"), from, contactcode);
				}
				else {
					// must be at least one hidden requestor record or active; we will resurrect the most recently hidden one if there is not active one (usually because someone deleted the contact record, but not the requestor)
					// We first sort by ascending Hidden field. If there is an active Requestor it will sort first in the list.
					dsMB.RequestorRow[] orderedByHiddenField = ds.T.Requestor.Rows.Select(null, new SortExpression(dsMB.Path.T.Requestor.F.Hidden, SortExpression.SortOrder.Asc));
					dsMB.RequestorRow requestor = orderedByHiddenField[0];
					if (requestor.F.Hidden != null) {
						// There is no active Requestor, so find the most recently-hidden on and resurrect it with a message to that effect
						if (orderedByHiddenField.Length > 1)
							requestor = orderedByHiddenField[orderedByHiddenField.Length - 1];

						StringBuilder updateComment = new StringBuilder();
						updateComment.AppendLine(Strings.Format(KB.K("Restored by MainBoss on {0}"), DateTime.Now));
						updateComment.Append(requestor.F.Comment);
						// if we are recreating the requestor, they are going to get an acknowledge despite any setting in the defaults for a Requestor record.
						// TODO: Is there a reason to do it this way, other than being a bit of a pain to find the default value?
						requestor.F.ReceiveAcknowledgement = true;
						requestor.F.Comment = updateComment.ToString();
						requestor.F.Hidden = null;
						InfoText = Strings.Format(KB.K("Restoring a Requestor from '{0}' with Contact Code '{1}'"), from, contactcode);
					}
				}
				ds.DB.Update(ds);
			}
			catch (System.Exception e) {
				throw new GeneralException(e, KB.K("Cannot create a Requestor from '{0}' with Contact Code '{1}'"), from, contactcode);
			}
		}
		#endregion
		#region ValueAsString
		private static string ValuesAsString([Invariant]string separator, IEnumerable<string> values) {
			return string.Join(separator, values.OrderBy(e => e.ToLower()).ThenBy(e => e).Select(e => Strings.IFormat("'{0}'", e)));
		}
		#endregion
		#region RequestorError
		void RequestorError(DatabaseEnums.EmailRequestState state, ErrorToRequestor errorToRequestor, Thinkage.Libraries.Translation.Key errformat, params object[] args) {
			State = state;
			WarningText = null;
			ErrorToRequestor = errorToRequestor;
			Exception = new GeneralException(errformat, args);
			throw Exception;
		}
		#endregion
	}
	#endregion
	#region
	public class AcquireRequestorAddressWithLogging : AcquireRequestor {
		public AcquireRequestorAddressWithLogging([Thinkage.Libraries.Translation.Invariant] string source, DBClient DB, MailAddress emailAddress, int? preferredLanguage) {
			EmailAddress = emailAddress;
			try {
				using (dsMB ds = new dsMB(DB)) {
					ds.EnsureDataTableExists(dsMB.Schema.T.ServiceConfiguration);
					ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.ServiceConfiguration);
					foreach (dsMB.ServiceConfigurationRow sRow in ds.T.ServiceConfiguration.Rows) {  // there should only be one, but this is safe.
						CreateRequestors |= sRow.F.AutomaticallyCreateRequestors;
						CreateFromLDAP |= sRow.F.AutomaticallyCreateRequestorsFromLDAP;
						CreateFromEmail |= sRow.F.AutomaticallyCreateRequestorsFromEmail;
						// if asked to create from LDAP and/or Email, we will always create the Requestor function for the matching/created Contact
						CreateRequestors |= CreateFromLDAP | CreateFromEmail;
						string pat = sRow.F.AcceptAutoCreateEmailPattern;
						try {
							if (!String.IsNullOrWhiteSpace(pat))
								AcceptRegex = new Regex(pat, RegexOptions.IgnoreCase);
						}
						catch (ArgumentException ex) {
							throw new GeneralException(ex, KB.K("Unable to compile {0} /{1}/"), dsMB.Schema.T.ServiceConfiguration.F.AcceptAutoCreateEmailPattern.LabelKey.Translate(), pat);
						}
						pat = sRow.F.RejectAutoCreateEmailPattern;
						try {
							if (!String.IsNullOrWhiteSpace(pat))
								RejectRegex = new Regex(pat, RegexOptions.IgnoreCase);
						}
						catch (ArgumentException ex) {
							throw new GeneralException(ex, KB.K("Unable to compile {0} /{1}/"), dsMB.Schema.T.ServiceConfiguration.F.RejectAutoCreateEmailPattern.LabelKey.Translate(), pat);
						}
					}
					FindRequestor(DB, preferredLanguage);
				}
			}
			catch (System.Exception ex) {
				if (Exception == null)
					Exception = ex;
				if (ErrorToRequestor == ErrorToRequestor.None)
					ErrorToRequestor = ErrorToRequestor.Unknown;
			}
			if (Exception != null || WarningText != null || InfoText != null) {
				using (dsMB ds = new dsMB(DB)) {
					ds.EnsureDataTableExists(dsMB.Schema.T.ServiceLog);
					var sr = ds.T.ServiceLog.AddNewRow();
					sr.F.Source = source;
					if (Exception != null) {
						sr.F.Message = Thinkage.Libraries.Exception.FullMessage(Exception);
						sr.F.EntryType = (byte)Database.DatabaseEnums.ServiceLogEntryType.Error;
					}
					else if (WarningText != null) {
						sr.F.Message = WarningText;
						sr.F.EntryType = (byte)Database.DatabaseEnums.ServiceLogEntryType.Warn;
					}
					else if (InfoText != null) {
						sr.F.Message = InfoText;
						sr.F.EntryType = (byte)Database.DatabaseEnums.ServiceLogEntryType.Info;
					}
					DB.Update(ds);
				}
			}
		}
	}
	#endregion
}
