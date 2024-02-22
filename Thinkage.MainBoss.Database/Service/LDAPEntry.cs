using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.Translation;


namespace Thinkage.MainBoss.Database.Service {
	#region LDAPEntry
	//
	// It would be nice to use AccountManagement.UserPrincipal rather than directly using DirectoryServices.ActiveDirectory
	// UserPrincipal does not have any method of searching that will match any email address associated with the user
	// The only suggestion I can find is access them all and check each one.
	// That would not be viable for each incoming email message on a large site.
	// There also does not seem to be any method of going from ActiveDirectory.SearchResult back to a UserPrincipal
	// 
	public class LDAPEntry {
		readonly public Guid Guid;
		readonly public string UserPrincipalName;
		readonly public string DisplayName;
		readonly public string DistinguishName;
		readonly public string Mail;
		readonly public string TelephoneNumber;
		readonly public string Mobile;
		readonly public string FacsimileTelephoneNumber;
		readonly public string HomePhone;
		readonly public string Pager;
		readonly public string WWWHomePage;
		readonly public int UserAccountControl;
		readonly public bool Disabled;
		readonly public List<string> AlternateEmail;
		readonly public int? PreferredLanguage;
		public LDAPEntry(SearchResult sr) {
			Guid = sr.GetDirectoryEntry().Guid;
			UserPrincipalName = LDAPPropertyString(sr, "userPrincipalName");
			DisplayName = LDAPPropertyString(sr, "displayName");
			DistinguishName = LDAPPropertyString(sr, "distinguishedName");
			Mail = LDAPPropertyString(sr, "mail");
			TelephoneNumber = LDAPPropertyString(sr, "telephoneNumber");
			Mobile = LDAPPropertyString(sr, "mobile");
			FacsimileTelephoneNumber = LDAPPropertyString(sr, "facsimileTelephoneNumber");
			HomePhone = LDAPPropertyString(sr, "homePhone");
			Pager = LDAPPropertyString(sr, "pager");
			WWWHomePage = LDAPPropertyString(sr, "wWWHomePage");
			UserAccountControl = sr.Properties[KB.I("userAccountControl")].Count > 0 ? (int)sr.Properties[KB.I("userAccountControl")][0] : 0;
			Disabled = (UserAccountControl & (int)UserAccountControlFlags.ACCOUNTDISABLE) != 0;
			int ne = sr.Properties[KB.I("proxyAddresses")].Count;
			AlternateEmail = new List<string>();
			var proxy = sr.Properties[KB.I("proxyAddresses")];
			foreach (var e in proxy) {
				var a = e as string;
				if (a == null || !a.StartsWith(KB.I("smtp:"), StringComparison.OrdinalIgnoreCase))
					continue;
				a = a.Substring(5).Trim();
				if (string.IsNullOrWhiteSpace(a))
					continue;
				AlternateEmail.Add(a);
			}
			var language = LDAPPropertyString(sr, "preferredLanguage");
			PreferredLanguage = null;
			if (!System.String.IsNullOrEmpty(language)) {
				try {
					System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(language);
					PreferredLanguage = ci.LCID;
				}
				catch (System.Exception) {
				}
			}
		}
		static string LDAPPropertyString(SearchResult sr, [Invariant] string propertyName) {
			string ret = null;
			if (sr.Properties[propertyName].Count > 0)
				ret = sr.Properties[propertyName][0].
						 ToString();

			return ret;
		}
		public static List<LDAPEntry> GetActiveDirectoryFiltered([Invariant]string command, [Invariant]string filter) {
			// see the comments with 'GetActiveDirectoryGivenGuid'
			List<LDAPEntry> ret = new List<LDAPEntry>();
			CheckActiveDirectory();
			try {
				using (DirectoryEntry gc = new DirectoryEntry(command)) {
					foreach (DirectoryEntry z in gc.Children) {
						using (DirectoryEntry root = z) {
							using (DirectorySearcher searcher = new DirectorySearcher(root, filter, activeDirectoryAttributes)) {
								searcher.ReferralChasing = ReferralChasingOption.All;
								var r = searcher.FindAll();
								if (r == null)
									return ret;
								foreach (SearchResult sr in r)
									ret.Add(new LDAPEntry(sr));
								r.Dispose();
								return ret;
							}
						}
					}
				}
			}
			catch (System.Exception ex) {
				if ((uint)ex.HResult == 0x80072030) // There is no such object on the server 
					return ret;
				throw;
			}
			return ret;
		}
		public static List<LDAPEntry> GetActiveDirectoryGivenEmail([Invariant]string aEmailAddress) {
			return GetActiveDirectoryFiltered("LDAP:", Strings.IFormat("(proxyaddresses=SMTP:{0})", aEmailAddress));
		}
		public static List<LDAPEntry> GetActiveDirectoryGivenEmail([Invariant]List<string> aEmailAddresses) {
			if (aEmailAddresses.Count == 0)
				return new List<LDAPEntry>();
			if (aEmailAddresses.Count == 1)
				return GetActiveDirectoryGivenEmail(aEmailAddresses[0]);
			var test = new StringBuilder();
			foreach (var a in aEmailAddresses)
				test.AppendFormat(KB.I("(proxyaddresses=SMTP:{0})"), a);
			return GetActiveDirectoryFiltered("LDAP:", Strings.IFormat("(|{0}))", test));
		}
		public static List<LDAPEntry> GetActiveDirectoryGivenPrimaryEmail([Invariant]string aEmailAddress) {
			return GetActiveDirectoryFiltered("LDAP:", Strings.IFormat("(mail={0})", aEmailAddress));
		}
		public static List<LDAPEntry> GetActiveDirectoryGivenPrimaryEmail([Invariant]List<string> aEmailAddresses) {
			if (aEmailAddresses.Count == 0)
				return new List<LDAPEntry>();
			if (aEmailAddresses.Count == 1)
				return GetActiveDirectoryGivenEmail(aEmailAddresses[0]);
			var test = new StringBuilder();
			foreach (var a in aEmailAddresses)
				test.AppendFormat(KB.I("(mail={0})"), a);
			return GetActiveDirectoryFiltered("LDAP:", Strings.IFormat("(|{0}))", test));
		}
		public static List<LDAPEntry> GetActiveDirectoryGivenPrincipalName([Invariant]string aPrincipalName) {
			return GetActiveDirectoryFiltered("LDAP:", Strings.IFormat("(userPrincipalName={0})", aPrincipalName));
		}
		public static List<LDAPEntry> GetActiveDirectoryGivenGuid(Guid aGuid) { // will return disabled accounts
																				// there should on by one entry for each guid
																				// but the search by guid seems to return one for each path
																				// so we only take the first one.
																				// depending on which version of Windows
																				// findone seems to fault if it doesn't find the guid
																				// and may or may not give an error if active directory is not available.
																				// Windows 10 seems to return values from the local computer if
																				// active directory is not available, but does not seem to give any errors.
																				// For the purposes of MainBoss the local users do not contain any information
																				// to be added to contacts.
																				// so for a none domain computer we return an empty list.
			//
			var ret = new List<LDAPEntry>();
			CheckActiveDirectory();

			try {
				using (var user = new DirectoryEntry(Strings.IFormat("LDAP://<GUID={0}>", aGuid))) {
					user.RefreshCache();
					using (DirectorySearcher searcher = new DirectorySearcher(user, null, activeDirectoryAttributes)) {
						searcher.ReferralChasing = ReferralChasingOption.All;
						SearchResult r = searcher.FindOne();
						if (r == null)
							return new List<LDAPEntry>();
						return new List<LDAPEntry>() { new LDAPEntry(r) };
					}
				}
			}
			catch (System.Exception ex) {
				if ((uint)ex.HResult == 0x80072030) // There is no such object on the server 
					return ret;
				throw;
			}
		}
		readonly static string[] activeDirectoryAttributes = new string[] {"userPrincipalName", "proxyAddresses", "objectGuid", "displayName",
																			"distinguishedName", "mail", "telephoneNumber", "mobile", "facsimileTelephoneNumber",
																			"homePhone", "pager", "wWWHomePage", "userAccountControl", "language", "preferredLanguage"};
		public static void CheckActiveDirectory(string k = null) {
			if (DomainAndIP.GetDomainName() == null) {
				if (k != null)
					throw new GeneralException(KB.K("'{0}' can only work if the computer is a member of a domain"), k);
				throw new GeneralException(KB.K("This computer is not a member of a domain"));
			}
			try {
				System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain();  // faults if not in a domain
			}
			catch (ActiveDirectoryObjectNotFoundException e) {
				if( k == null)
					throw new GeneralException(e,KB.K("The domain controller for the domain '{0}' is currently inaccessible"), DomainAndIP.GetDomainName());
				throw new GeneralException(e, KB.K("'{0}' requires access to the domain controller for the domain '{1}' which is currently inaccessible"), k, DomainAndIP.GetDomainName() );
			}
		}
		public static bool SetContactValues(dsMB.ContactRow contactRow, LDAPEntry LDAPUser, bool preservePrimaryEmail = false) {
			bool changed = false;
			if (LDAPUser.Guid != null && LDAPUser.Guid != contactRow.F.LDAPGuid) {
				contactRow.F.LDAPGuid = LDAPUser.Guid;
				changed |= true;
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
			else if (string.Compare(LDAPUser.Mail, contactRow.F.Email, true) != 0) {
				if (contactRow.F.AlternateEmail == null) {
					contactRow.F.AlternateEmail = LDAPUser.Mail;
					changed = true;
				}
				else if (ServiceUtilities.CheckAlternateEmail(LDAPUser.Mail, contactRow.F.AlternateEmail)) {
					contactRow.F.AlternateEmail = Strings.IFormat("{0} {1}", contactRow.F.AlternateEmail, LDAPUser.Mail);
					changed = true;
				}
			}
			return changed;
		}
		private static bool IsChangedValue(string to, string from) {
			return from != null && from != to;
		}
	}
	#endregion
	#region UserAccountControlFlags
	/// <summary>
	/// Flags that control the behavior of the user account.
	/// </summary>
	[Flags()]
	public enum UserAccountControlFlags : int {
		/// <summary>
		/// The logon script is executed. 
		///</summary>
		SCRIPT = 0x00000001,

		/// <summary>
		/// The user account is disabled. 
		///</summary>
		ACCOUNTDISABLE = 0x00000002,

		/// <summary>
		/// The home directory is required. 
		///</summary>
		HOMEDIR_REQUIRED = 0x00000008,

		/// <summary>
		/// The account is currently locked out. 
		///</summary>
		LOCKOUT = 0x00000010,

		/// <summary>
		/// No password is required. 
		///</summary>
		PASSWD_NOTREQD = 0x00000020,

		/// <summary>
		/// The user cannot changed the password. 
		///</summary>
		/// <remarks>
		/// Note:  You cannot assign the permission settings of PASSWD_CANT_changed by directly modifying the UserAccountControl attribute. 
		/// For more information and a code example that shows how to prevent a user from changing the password, see User Cannot changed Password.
		// </remarks>
		PASSWD_CANT_changed = 0x00000040,

		/// <summary>
		/// The user can send an encrypted password. 
		///</summary>
		ENCRYPTED_TEXT_PASSWORD_ALLOWED = 0x00000080,

		/// <summary>
		/// This is an account for users whose primary account is in another domain. This account provides user access to this domain, but not 
		/// to any domain that trusts this domain. Also known as a local user account. 
		///</summary>
		TEMP_DUPLICATE_ACCOUNT = 0x00000100,

		/// <summary>
		/// This is a default account type that represents a typical user. 
		///</summary>
		NORMAL_ACCOUNT = 0x00000200,

		/// <summary>
		/// This is a permit to trust account for a system domain that trusts other domains. 
		///</summary>
		INTERDOMAIN_TRUST_ACCOUNT = 0x00000800,

		/// <summary>
		/// This is a computer account for a computer that is a member of this domain. 
		///</summary>
		WORKSTATION_TRUST_ACCOUNT = 0x00001000,

		/// <summary>
		/// This is a computer account for a system backup domain controller that is a member of this domain. 
		///</summary>
		SERVER_TRUST_ACCOUNT = 0x00002000,

		/// <summary>
		/// Not used. 
		///</summary>
		Unused1 = 0x00004000,

		/// <summary>
		/// Not used. 
		///</summary>
		Unused2 = 0x00008000,

		/// <summary>
		/// The password for this account will never expire. 
		///</summary>
		DONT_EXPIRE_PASSWD = 0x00010000,

		/// <summary>
		/// This is an MNS logon account. 
		///</summary>
		MNS_LOGON_ACCOUNT = 0x00020000,

		/// <summary>
		/// The user must log on using a smart card. 
		///</summary>
		SMARTCARD_REQUIRED = 0x00040000,

		/// <summary>
		/// The service account (user or computer account), under which a service runs, is trusted for Kerberos delegation. Any such service 
		/// can impersonate a client requesting the service. 
		///</summary>
		TRUSTED_FOR_DELEGATION = 0x00080000,

		/// <summary>
		/// The security context of the user will not be delegated to a service even if the service account is set as trusted for Kerberos delegation. 
		///</summary>
		NOT_DELEGATED = 0x00100000,

		/// <summary>
		/// Restrict this principal to use only Data Encryption Standard (DES) encryption types for keys. 
		///</summary>
		USE_DES_KEY_ONLY = 0x00200000,

		/// <summary>
		/// This account does not require Kerberos pre-authentication for logon. 
		///</summary>
		DONT_REQUIRE_PREAUTH = 0x00400000,

		/// <summary>
		/// The user password has expired. This flag is created by the system using data from the Pwd-Last-Set attribute and the domain policy. 
		///</summary>
		PASSWORD_EXPIRED = 0x00800000,

		/// <summary>
		/// The account is enabled for delegation. This is a security-sensitive setting; accounts with this option enabled should be strictly 
		/// controlled. This setting enables a service running as the account to assume a client identity and authenticate as that user to 
		/// other remote servers on the network.
		///</summary>
		TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION = 0x01000000,

		/// <summary>
		/// 
		/// </summary>
		PARTIAL_SECRETS_ACCOUNT = 0x04000000,

		/// <summary>
		/// 
		/// </summary>
		USE_AES_KEYS = 0x08000000
	}
	#endregion
}
