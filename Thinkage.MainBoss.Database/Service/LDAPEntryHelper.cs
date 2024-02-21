using System.Collections.Generic;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database.Service {
	//
	// It would be nice to use AccountManagement.UserPrincipal rather than directly using DirectoryServices.ActiveDirectory
	// UserPrincipal does not have any method of searching that will match any email address associated with the user
	// The only suggestion I can find is access them all and check each one.
	// That would not be viable for each incoming email message on a large site.
	// There also does not seem to be any method of going from ActiveDirectory.SearchResult back to a UserPrincipal
	// 
	public static class LDAPEntryHelper {
		public static bool SetContactValues(dsMB.ContactRow contactRow, LDAPEntry LDAPUser, bool preservePrimaryEmail = false) {
			bool changed = false;
			if (LDAPUser.Guid != null && LDAPUser.Guid != contactRow.F.LDAPGuid) {
				contactRow.F.LDAPGuid = LDAPUser.Guid;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.Code, LDAPUser.DisplayName)) {
				contactRow.F.Code = LDAPUser.DisplayName;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.BusinessPhone, LDAPUser.TelephoneNumber)) {
				contactRow.F.BusinessPhone = LDAPUser.TelephoneNumber;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.HomePhone, LDAPUser.HomePhone)) {
				contactRow.F.HomePhone = LDAPUser.HomePhone;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.MobilePhone, LDAPUser.Mobile)) {
				contactRow.F.MobilePhone = LDAPUser.Mobile;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.PagerPhone, LDAPUser.Pager)) {
				contactRow.F.PagerPhone = LDAPUser.Pager;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.FaxPhone, LDAPUser.FacsimileTelephoneNumber)) {
				contactRow.F.FaxPhone = LDAPUser.FacsimileTelephoneNumber;
				changed = true;
			}
			if (IsChangedValue(contactRow.F.WebURL, LDAPUser.WWWHomePage)) {
				contactRow.F.WebURL = LDAPUser.WWWHomePage;
				changed = true;
			}
			if (LDAPUser.PreferredLanguage != null && LDAPUser.PreferredLanguage != contactRow.F.PreferredLanguage) {
				contactRow.F.PreferredLanguage = LDAPUser.PreferredLanguage;
				changed |= true;
			}
			if (LDAPUser.Mail != null && (LDAPUser.Mail != contactRow.F.Email) && (!preservePrimaryEmail || contactRow.F.Email == null)) {
				changed = true;
				contactRow.F.Email = LDAPUser.Mail;
			}
			string newAlternate = BuildAlternateEmail(contactRow.F.AlternateEmail, contactRow.F.Email, LDAPUser.Mail, LDAPUser.AlternateEmail);
			if (IsChangedValue(contactRow.F.AlternateEmail, newAlternate)) {
				contactRow.F.AlternateEmail = newAlternate;
				changed = true;
			}
			return changed;
		}
		public static bool IsChangedValue(string to, string from) {
			return from != null && from != to;
		}

		public static string BuildAlternateEmail(string originalFieldValue, string primaryEmailValue, string LDAPEmail, List<string> otherAlternates) {
			List<string> alternateEmailAddresses = new List<string>();
			if (!string.IsNullOrEmpty(originalFieldValue))
				alternateEmailAddresses.AddRange(ServiceUtilities.AlternateEmailAddresses(originalFieldValue));
			if (!string.IsNullOrEmpty(primaryEmailValue) && !string.IsNullOrEmpty(LDAPEmail) && string.Compare(primaryEmailValue, LDAPEmail, true) != 0 && !alternateEmailAddresses.Contains(primaryEmailValue))
				alternateEmailAddresses.Add(primaryEmailValue);
			// Now add any remaining alternate email addresses (proxyaddresses) that do not match the original Email
			foreach (string email in otherAlternates)
				if (!alternateEmailAddresses.Contains(email))
					alternateEmailAddresses.Add(email);

			return string.Join(" ", alternateEmailAddresses);
		}
	}
}
