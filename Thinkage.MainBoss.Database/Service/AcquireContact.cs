using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {
	#region ContactFromEmailAddress
	public class AcquireContact {
		//
		// Because there is a need for two classes of error. The error suitable for the MainBoss Administration giving information about how to fix the problem
		// and a generic error for the user. Exceptions can not be used to report errors.
		// Instead a successful run had the variable RequestID set
		// Any exception will be in variable Exception and contains the message for the MainBoss Administrator further more the variable  UserText will contain the error message for the end user 
		// WarningText contains any warning messages for the MainBoss Administrator
		// Assuming no error then InfoText with contain generic information messages for the MainBoss Administrator
		protected MailAddress EmailAddress;
		private IEnumerable<LDAPEntry> LDAPUsers = null;
		public System.Exception Exception = null;
		public string WarningText = null;
		public string InfoText = null;
		public ErrorToRequestor ErrorToRequestor = ErrorToRequestor.None;
		public DatabaseEnums.EmailRequestState State = DatabaseEnums.EmailRequestState.NoContact;
		public Guid? ContactID = null;
		public String ContactCode = null;
		public String ContactEmail = null;
		private bool CreateFromLDAP;
		private bool CreateFromEmail;
		int? PreferredLanguage;

		public AcquireContact(XAFClient DB, IEnumerable<LDAPEntry> aLDAPUsers, MailAddress emailAddress, bool createFromLDAP, bool createFromEmail, int? preferredLanguage) {
			EmailAddress = emailAddress;
			CreateFromLDAP = createFromLDAP;
			CreateFromEmail = createFromEmail;
			LDAPUsers = aLDAPUsers;
			PreferredLanguage = preferredLanguage;
			if (LDAPUsers == null && emailAddress == null) {
				ContactError(DatabaseEnums.EmailRequestState.NoContact, ErrorToRequestor.Unknown, KB.K("Cannot find a contact since no email address or Active Directory Reference provided"));
				return;
			}
			try { // try to enhance/find our LDAPUsers list; ignore any errors getting the LDAP list at this level
				if (emailAddress != null)
					LDAPUsers = LDAPUsers == null ? LDAPEntry.GetActiveDirectoryGivenEmail(emailAddress.Address) : LDAPUsers.Concat(LDAPEntry.GetActiveDirectoryGivenEmail(emailAddress.Address));
				LDAPUsers = LDAPUsers?.Where(e => !e.Disabled);
			}
			catch { }
			try {
				using (dsMB ds = new dsMB(DB)) {
					ds.EnsureDataTableExists(dsMB.Schema.T.Contact);
					List<dsMB.ContactRow> found = new List<dsMB.ContactRow>();
					SqlExpression mailtest = null;
					SqlExpression test = null;
					Set<object> LDAPEmail = null;
					Set<object> LDAPAllEmail = null;
					Set<object> LDAPguids = null;
					if (EmailAddress.Address != null)
						mailtest = new SqlExpression(dsMB.Path.T.Contact.F.Email).Eq(EmailAddress.Address).Or(new SqlExpression(dsMB.Path.T.Contact.F.AlternateEmail).Like(SqlExpression.Constant(Strings.IFormat("%{0}%", EmailAddress.Address))));
					if (LDAPUsers != null && LDAPUsers.Any() ) {
						LDAPguids = new Set<Object>(LDAPUsers.Select(e => (object)e.Guid));
						LDAPEmail = new Set<Object>(LDAPUsers.Select(e => e.Mail));
						LDAPAllEmail = new Set<Object>(LDAPUsers.Select(e => e.Mail).Concat(LDAPUsers.SelectMany(e => e.AlternateEmail)));
						test = new SqlExpression(dsMB.Path.T.Contact.F.LDAPGuid).In(SqlExpression.Constant(LDAPguids));
						if (LDAPAllEmail.Any() )
							test = test.Or(new SqlExpression(dsMB.Path.T.Contact.F.Email).In(SqlExpression.Constant(LDAPAllEmail)));
					}
					if (mailtest != null && test != null)
						test = test.Or(mailtest);
					else
						test = mailtest;
					ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.Contact, test, null, null);
					foreach (dsMB.ContactRow row in ds.T.Contact.Rows)
						found.Add(row);

					var contactRow = ContactInformation(ds, found, LDAPguids, LDAPEmail, LDAPAllEmail, false);
					if (contactRow == null)  // look for hidden contacts that match
						contactRow = ContactInformation(ds, found, LDAPguids, LDAPEmail, LDAPAllEmail, true);
					// At this point, if we do not have a valid contactRow, we will try to create it depending on user options
					if (contactRow == null && (createFromLDAP || createFromEmail)) {
						// if we have an ActiveDirectory present and we have no matching EmailAddresses there and we weren't told to createFromEmail, complain.
						if (LDAPUsers != null && (LDAPAllEmail == null || !LDAPAllEmail.Any()) && !createFromEmail)
							ContactError(DatabaseEnums.EmailRequestState.NoContact, ErrorToRequestor.NotFound, KB.K("Active Directory does not have an entry for email address '{0}'"), EmailAddress);
						contactRow = CreateContact(ds, LDAPUsers, EmailAddress, preferredLanguage);
						if (contactRow == null)
							ContactError(DatabaseEnums.EmailRequestState.NoContact, ErrorToRequestor.Unknown, KB.K("Cannot create a Contact for email address '{0}'"), EmailAddress);
					}
					if (contactRow == null)
						ContactError(DatabaseEnums.EmailRequestState.NoContact, ErrorToRequestor.Unknown, KB.K("Cannot find a Contact for email address '{0}'"), EmailAddress);
					else {
						ContactID = contactRow.F.Id;
						ContactCode = contactRow.F.Code;
						ContactEmail = contactRow.F.Email;
					}
				}
			}
			catch (System.Exception ex) {
				if (Exception == null)
					Exception = ex;
				if (ErrorToRequestor == ErrorToRequestor.None)
					ErrorToRequestor = ErrorToRequestor.Unknown;
			}
			if (Exception == null)
				State = DatabaseEnums.EmailRequestState.Completed;
			else if ( !(Exception is GeneralException))
				Exception = new GeneralException(Exception, KB.K("Unable to resolve '{0}' to a Contact"), emailAddress);

		}
		#region ContactInformation
		private dsMB.ContactRow ContactInformation(dsMB ds, List<dsMB.ContactRow> found, Set<object> LDAPguids, Set<object> LDAPEmail, Set<object> LDAPAllEmail, bool hidden) {
			var from = EmailAddress != null ? EmailAddress.Address : null;
			var source = KB.K("Contacts {0} have the same email address {1}");
			// find all contacts with the LDAP reference on any mail that match if email address from contacts matches any from address
			//
			IEnumerable<dsMB.ContactRow> active = null;
			IEnumerable<dsMB.ContactRow> matched = null;
			// start by ignoring deleted contacts;
			// look for LDAPguid first
			if (hidden)
				active = found.Where(e => e.F.Hidden != null);
			else
				active = found.Where(e => e.F.Hidden == null);
			// look for matching guid first
			source = KB.K("Contacts {0} all have the same Active Directory Reference");
			if (DomainAndIP.GetDomainName() != null && LDAPguids != null) {
				matched = active.Where(e => LDAPguids.Contains(e));
				//
				// look for primary email addresses next
				if (!matched.Any()) {
					source = KB.K("Contacts {0} have the same primary email address '{1}'");
					matched = active.Where(e => LDAPEmail.Contains(e.F.Email));
				}
				//
				// if no primary email address look for alternate email addresses
				// the email address has to be an exact case match anywhere in the string for Alternate mails.
				// and the text before or after cannot be in an email address.
				// the funny characters came from the standard, but may not be in the domain address
				//

				if (!matched.Any() && from != null) {
					source = KB.K("Contacts {0} have the same alternate email address '{1}'");
					matched = active.Where(e => ServiceUtilities.CheckAlternateEmail(e.F.AlternateEmail, from));
				}
				// 
				// if the contact does not have a email address matching try active direcory
				//
				if (!matched.Any()) {
					source = KB.K("Contacts {0} have the same email address '{1}' in Active Directory");
					matched = active.Where(e => LDAPAllEmail.Contains(e.F.Email));
				}
				// 
				// we don't check contacts Alternate email address agains LDAP alternate email addresss
				//
			}
			else
				matched = active;
			if (!matched.Any())
				return null;
			if (matched.Count() == 1 || hidden) {
				var contactRow = hidden ? matched.OrderByDescending(e => e.F.Hidden).First() : matched.First();
				try {
					bool changed = false;
					// if the contact does not contain the email address that we found it by (as it possible could if we found the contact by using LDAP) update to contain the email address.
					if (contactRow.F.Hidden != null) {
						contactRow.F.Hidden = null;
						StringBuilder updateComment = new StringBuilder();
						updateComment.AppendLine(Strings.Format(KB.K("Restored by MainBoss on {0}"), DateTime.Now));
						updateComment.AppendLine(contactRow.F.Comment);
						contactRow.F.Comment = updateComment.ToString();
						InfoText = Strings.Format(KB.K("Restoring Contact '{0}'"), contactRow.F.Code);
						changed = true;
					}
					if (LDAPguids != null  && LDAPguids.Count() == 1) { 
						LDAPEntry LDAPUser = null;
						if ( contactRow.F.LDAPGuid == null)
							LDAPUser = LDAPUsers.First();
						if (LDAPUser != null && contactRow.F.LDAPGuid == null) {
							contactRow.F.LDAPGuid = LDAPUser.Guid;
							changed = true;
						}
						if (from == null)
							from = LDAPUser.Mail;
						if (contactRow.F.Email != from && !ServiceUtilities.CheckAlternateEmail(contactRow.F.AlternateEmail, from)) {
							if (contactRow.F.Email == null)
								contactRow.F.Email = from;
							else if (contactRow.F.AlternateEmail == null)
								contactRow.F.AlternateEmail = from;
							else
								contactRow.F.AlternateEmail = Strings.IFormat("{0} {1}", contactRow.F.AlternateEmail, from);
							changed = true;
						}
					}
					if (changed)
						ds.DB.Update(ds);
					return contactRow;
				}
				catch (System.Exception ex) {
					// Determine if we received a duplicate code trying to save our restored contact
					InterpretedDbException ie = ex as InterpretedDbException;
					if (ie != null && ie.InterpretedErrorCode != InterpretedDbExceptionCodes.ViolationUniqueConstraint)
						ContactError(DatabaseEnums.EmailRequestState.AmbiguousContactCreation, Service.ErrorToRequestor.Multiple, KB.K("Cannot restore Contact '{0}' because there is a duplicate with the same name"), contactRow.F.Code);
					else
						throw;
				}
			}
			var ordered = found.OrderBy(e => e.F.Code.ToLower());
			var contact = ordered.First();
			ContactError(DatabaseEnums.EmailRequestState.AmbiguousContact, ErrorToRequestor.Multiple, source, ValuesAsString(", ", ordered.Select(e => e.F.Code)), from);
			return null;
		}
		#endregion
		#region CreateContact
		private dsMB.ContactRow CreateContact(dsMB ds, IEnumerable<LDAPEntry> LDAPUsers, System.Net.Mail.MailAddress from, int? preferredLanguage) {
			LDAPEntry LDAPUser = null;
			var contactRow = ds.T.Contact.AddNewContactRow();
			if (LDAPUsers != null) {  // if we have LDAP info use it.
				if (LDAPUsers.Count() == 1)
					LDAPUser = LDAPUsers.First();
				if (LDAPUsers.Count() > 1) {
					var primary = LDAPUsers.Where(sr => string.Compare(from.Address, sr.Mail, true) == 0);
					if (primary.Count() > 1) {
						var users = LDAPUsers.Select(sr => sr.DisplayName);
						var usersAsString = String.Join(KB.I(", "), users.Select(u => Strings.IFormat("'{0}'", u)));
						ContactError(DatabaseEnums.EmailRequestState.AmbiguousContactCreation, ErrorToRequestor.Multiple, KB.K("Cannot create a new Contact for email address {0}, Users {1} in the Active Directory use that email address"), from, usersAsString);
					}
					LDAPUser = primary.First();
				}
				if (LDAPUser != null)
					LDAPEntry.SetContactValues(contactRow, LDAPUser);
			}
			if (LDAPUser == null) { // use email to try to create a contact with the email address and Code = DisplayName
				contactRow.F.Code = string.IsNullOrWhiteSpace(from.DisplayName) ? from.Address : from.DisplayName;
				contactRow.F.Email = from.Address;
				contactRow.F.PreferredLanguage = preferredLanguage;
			}

			contactRow.F.Comment = Strings.Format(KB.K("Created by MainBoss on {0}"), DateTime.Now);
			InfoText = Strings.Format(KB.K("Creating Contact '{0}'"), contactRow.F.Code);

			List<string> tried = new List<string>();
			List<string> possible = new List<string>();
			possible.Add(contactRow.F.Code);
			if (contactRow.F.Code != from.DisplayName && !string.IsNullOrWhiteSpace(from.DisplayName))  // may be true if code came from active directory.
				possible.Add(from.DisplayName);
			foreach (var code in possible) {
				contactRow.F.Code = code;
				try {
					ds.DB.Update(ds); // try to save it now to detect UniqueConstraint violations (possibly because someone created the contact record between the time we looked and now)
					return contactRow;
				}
				catch (System.Exception ex) {
					// Determine if we received a duplicate code trying to save our new contact
					InterpretedDbException ie = ex as InterpretedDbException;
					if (ie != null && ie.InterpretedErrorCode != InterpretedDbExceptionCodes.ViolationUniqueConstraint)
						throw;
				}
			}
			ContactError(DatabaseEnums.EmailRequestState.AmbiguousContactCreation, ErrorToRequestor.InUse, KB.K("A Contact for email address '{0} could not be created since Contacts already exist for '{1}'"), from.Address, contactRow.F.Code);
			return null;
		}
		#endregion
		#region ValueAsString
		private static string ValuesAsString([Invariant]string separator, IEnumerable<string> values) {
			return string.Join(separator, values.OrderBy(e => e.ToLower()).ThenBy(e => e).Select(e => Strings.IFormat("'{0}'", e)));
		}
		#endregion
		#region ContactError
		void ContactError(DatabaseEnums.EmailRequestState state, ErrorToRequestor errorToRequestor, Thinkage.Libraries.Translation.Key errformat, params object[] args) {
			State = state;
			WarningText = null;
			ErrorToRequestor = errorToRequestor;
			Exception = new GeneralException(errformat, args);
			throw Exception;
		}
		void ContactError(System.Exception ex, DatabaseEnums.EmailRequestState state, ErrorToRequestor errorToRequestor, Thinkage.Libraries.Translation.Key format, params object[] args) {
			State = state;
			WarningText = null;
			var address = EmailAddress?.Address ?? KB.I("None");
			ErrorToRequestor = errorToRequestor;
			Exception = new GeneralException(format, args);
			throw Exception;
		}
		#endregion
	}
	#endregion
}
