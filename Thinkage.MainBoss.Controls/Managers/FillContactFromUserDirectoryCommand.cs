using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;
using System.Collections.Specialized;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Common Active Directory attribute mapping to Contact fields for the Micosoft control
	/// </summary>
	internal static class ContactFromDirectoryService {
		public static Dictionary<string, DBI_Path> AttributeMapping = new Dictionary<string, DBI_Path> {
				{ KB.I("mail"), dsMB.Path.T.Contact.F.Email },
				{ KB.I("telephoneNumber"), dsMB.Path.T.Contact.F.BusinessPhone },
				{ KB.I("mobile"), dsMB.Path.T.Contact.F.MobilePhone },
				{ KB.I("facsimileTelephoneNumber"), dsMB.Path.T.Contact.F.FaxPhone },
				{ KB.I("homePhone"), dsMB.Path.T.Contact.F.HomePhone },
				{ KB.I("pager"), dsMB.Path.T.Contact.F.PagerPhone },
				{ KB.I("wWWHomePage"), dsMB.Path.T.Contact.F.WebURL },
				{ KB.I("preferredLanguage"), dsMB.Path.T.Contact.F.PreferredLanguage },
				{ KB.I("displayName"), dsMB.Path.T.Contact.F.Code },
				{ KB.I("userPrincipalName"), dsMB.Path.T.Contact.F.Code }
			};
		// The following are not part of the contact record so are not relevant here
		//			AttributeMapping.Add("streetAddress", );
		//			AttributeMapping.Add("st", ); //State
		//			AttributeMapping.Add("L", ); // City
		//			AttributeMapping.Add("co", ); // Country
		//			AttributeMapping.Add("postalCode", );
	}
	public class FillContactFromUserDirectoryCommand : SettableDisablerProperties, ICommand {
		readonly bool Update;
		private delegate DBI_Path pathMapper(DBI_Path p);
		private delegate Sink sinkPutter(DBI_Path p);
		private FillContactFromUserDirectoryCommand(DBI_Table schema, sinkPutter putter, UIFactory uiFactory, XAFClient db)
			: base(KB.K("Get user or contact information from Active Directory"), KB.K("There must be a valid email address"), false) {
			UIFactory = uiFactory;
			pathMapper mapper;
			DBI_Table mainTable;
			if (schema.IsDefaultTable) {
				mapper = delegate (DBI_Path p) {
					return p.DefaultPath;
				};
				mainTable = schema.Main;
			}
			else {
				mapper = delegate (DBI_Path p) {
					return p;
				};
				mainTable = schema;
			}
			NameSink = putter(mapper(dsMB.Path.T.Contact.F.Code));
			LDAPGuidSink = putter(mapper(dsMB.Path.T.Contact.F.LDAPGuid));
			CommentSink = putter(mapper(dsMB.Path.T.Contact.F.Comment));
			EmailSink = putter(mapper(dsMB.Path.T.Contact.F.Email));
			AlternateEmailSink = putter(mapper(dsMB.Path.T.Contact.F.AlternateEmail));
			OtherSinks = new Dictionary<string, Sink>();
			foreach (var key in ContactFromDirectoryService.AttributeMapping.Keys)
				OtherSinks.Add(key, putter(mapper(ContactFromDirectoryService.AttributeMapping[key])));
		}
		public FillContactFromUserDirectoryCommand(EditLogic logic, bool update)
			: this(logic.TInfo.Schema, (path) => logic.GetPathNotifyingValue(path, 0), logic.CommonUI.UIFactory, logic.DB) {
			Update = update;
			NameSource = logic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.Code, 0);
			if (update) {
				EmailSource = logic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.Email, 0);
				LDAPGuidSource = logic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.LDAPGuid, 0);
				AlternateEmailSource = logic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.AlternateEmail, 0);
				EmailSource.Notify += delegate () { Enabled = EmailSource.GetValue() != null || AlternateEmailSource.GetValue() != null || LDAPGuidSource.GetValue() != null; };
				AlternateEmailSource.Notify += delegate () { Enabled = EmailSource.GetValue() != null || AlternateEmailSource.GetValue() != null || LDAPGuidSource.GetValue() != null; };
			}
			else
				Enabled = true;
			CommentSource = logic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.Comment, 0);
		}
		public void Execute() {
			if (Update)
				UpdateFromLDAP();
			else
				FillFromUserQuery();
		}
		private void FillFromUserQuery() {
			var on = (string)(NameSource?.GetValue());
			List<UserDirectoryObject> users = UIFactory.SelectUsersFromSystemDirectory(ContactFromDirectoryService.AttributeMapping.Keys.ToList<string>(), multiSelect: false);
			if (users == null || users.Count == 0)
				return;
			if (users.Count > 1)
				throw new GeneralException(KB.K("Only one Windows user can be picked"));
			var user = users[0];
			NameSink.SetValue(user.Name); // Set the Code field here; DisplayName will overwrite if non-null in OtherSinks
			LDAPGuidSink.SetValue(user.ObjectGuid);
			MoveCorresponding(user, "userPrincipalName");
			var dn = (string)user.FetchSingleAttributeValue("displayName", CommentSink.TypeInfo, typeof(string));
			var pn = (string)user.FetchSingleAttributeValue("userPrincipalName", CommentSink.TypeInfo, typeof(string));
			if (on == null || string.Compare(on, dn, true) == 0)
				CommentSink.SetValue(Strings.IFormat("{0}\nValues set from Active Directory from Windows User {1} on {2}\n", CommentSource.GetValue(), pn, DateTime.Now));
			else
				CommentSink.SetValue(Strings.IFormat("{0}\nValues set from Active Directory from Windows User {1} on {2}, The Contact's name changed from '{3}'\n", CommentSource.GetValue(), pn, DateTime.Now, on));

		}
		private void UpdateFromLDAP() {
			//
			// if there is a LDAPGUid use that 
			// if not use the a LDAP user with match LDAP 'mail' value
			// if not use the a LDAP user whose principle name match
			// if not use the a LDAP whose defines the email address matching the Contact.F.Email
			// The Contact.F.AlternateEmail is not used.
			//
			LDAPEntry.CheckActiveDirectory();
			IEnumerable<LDAPEntry> ades = new List<LDAPEntry>();
			Guid? LDAPGuid = (Guid?)LDAPGuidSource?.GetValue();
			var OriginalName = (string)NameSource?.GetValue();
			var Email = (string)(EmailSource?.GetValue());
			var AlternateEmailString = (string)(AlternateEmailSource?.GetValue());
			bool noEmailAddresses = string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(AlternateEmailString);
			if (LDAPGuid == null && noEmailAddresses)
				throw new GeneralException(KB.K("In order to update the contact there must be either an Active Directory Reference or an email address"));
			if (LDAPGuid != null) {
				ades = LDAPEntry.GetActiveDirectoryGivenGuid(LDAPGuid.Value);
				if (!ades.Any() && noEmailAddresses)
					throw new GeneralException(KB.K("Cannot update because the Active Directory Reference cannot be found and there is no email address to match"));
			}
			if (!ades.Any() && !string.IsNullOrEmpty(Email))
				ades = LDAPEntry.GetActiveDirectoryUsingEmail(Email);
			if (!ades.Any() && OriginalName != null)
				ades = LDAPEntry.GetActiveDirectoryGivenPrincipalName(OriginalName);
			if (!ades.Any())
				foreach (var a in ServiceUtilities.AlternateEmailAddresses(AlternateEmailString))
					ades = ades.Union(LDAPEntry.GetActiveDirectoryUsingEmail(a), new LDAPEntryComparerByGuid());
			if (!ades.Any())
				throw new GeneralException(KB.K("Cannot update because Windows has no login names associated with any of the email addresses"));
			if (ades.Count() > 1) {
				var names = ades.Select(sr => sr.DisplayName);
				var namesAsString = String.Join(", ", ades.Select(n => Strings.IFormat("'{0}'", n)));
				throw new GeneralException(KB.K("Cannot update because Windows has multiple login names {0} associated with Contact's email addresses"), namesAsString);
			}
			var LDAPE = ades.First();

			var user = FillByGuid(LDAPE);
			if (string.IsNullOrWhiteSpace(user.Name))
				throw new GeneralException(KB.K("Active Directory does not have any relevant information for email address {0}"), Email);
			NameSink.SetValue(user.Name);
			LDAPGuidSink.SetValue(user.ObjectGuid);
			MoveCorresponding(user);
			//
			// email is special, we will not over write an email address on an update matches an ldap secondary address, but will add it to the contacts secondary addresses if necessary
			// we also keep whatever alternate email addresses may have been entered, and add the ones provided (if any) from LDAP.
			AlternateEmailSink.SetValue(LDAPEntryHelper.BuildAlternateEmail(Email, AlternateEmailString, LDAPE.Mail, LDAPE.AlternateEmail));
			if (OriginalName == null || string.Compare(LDAPE.DisplayName, OriginalName, true) == 0)
				CommentSink.SetValue(Strings.IFormat("{0}\nUpdated from Active Directory from Windows User {1} on {2}\n", CommentSource.GetValue(), LDAPE.UserPrincipalName, DateTime.Now));
			else
				CommentSink.SetValue(Strings.IFormat("{0}\nUpdated from Active Directory from Windows User {1} on {2}, The Contact's name changed from '{3}'\n", CommentSource.GetValue(), LDAPE.UserPrincipalName, DateTime.Now, OriginalName));
		}
		private void MoveCorresponding(UserDirectoryObject user, params string[] skip) {
			foreach (var key in ContactFromDirectoryService.AttributeMapping.Keys) {
				if (skip.Any(e => e == key))
					continue;
				Sink s = OtherSinks[key];
				try {
					if (key.Equals(KB.I("preferredLanguage"))) { // Special case as preferredLanguage is an LCID
						int? language = (int?)user.FetchSingleAttributeValue(key, Thinkage.Libraries.TypeInfo.IntegralTypeInfo.NonNullUniverse, typeof(int));
						if (language.HasValue) {
							try {
								s.SetValue(language.Value);
							}
							catch (System.Exception) {
							}
						}
					}
					else {
						var v = (string)user.FetchSingleAttributeValue(key, s.TypeInfo, typeof(string));
						if (!System.String.IsNullOrEmpty(v))
							s.SetValue(v);
					}
				}
				catch (System.Exception) {
				}
			}
		}

		private static UserDirectoryObject FillByGuid(LDAPEntry LDAPUser) { // mapping from active direcory 
			var Code = LDAPUser.DisplayName;
			var Path = LDAPUser.DistinguishName;
			var LDAPGuid = LDAPUser.Guid;
			var oa = new Dictionary<string, object[]> {
				{ KB.I("mail"),                         new object[] { LDAPUser.Mail} },
				{ KB.I("telephoneNumber"),              new object[] { LDAPUser.TelephoneNumber} },
				{ KB.I("homePhone"),                    new object[] { LDAPUser.HomePhone} },
				{ KB.I("mobile"),                       new object[] { LDAPUser.Mobile} },
				{ KB.I("pager"),                        new object[] { LDAPUser.Pager} },
				{ KB.I("facsimileTelephoneNumber"),     new object[] { LDAPUser.FacsimileTelephoneNumber} },
				{ KB.I("wWWHomePage"),                  new object[] { LDAPUser.WWWHomePage} },
				{ KB.I("displayName"),                  new object[] { LDAPUser.DisplayName} },
				{ KB.I("preferredLanguage"),            new object[] { LDAPUser.PreferredLanguage} },
				{ KB.I("userPrincipalName"),            new object[] { LDAPUser.UserPrincipalName} },
				{ KB.I("proxyAddresses"),               new object[] { LDAPUser.AlternateEmail} }
			};
			return new UserDirectoryObject(Code, LDAPGuid, null, Path, oa);
		}
		public bool RunElevated {
			get {
				return false;
			}
		}
		private readonly Source NameSource;
		private readonly Source LDAPGuidSource;
		private readonly Source CommentSource;
		private readonly NotifyingSource AlternateEmailSource;
		private readonly NotifyingSource EmailSource;
		private readonly UIFactory UIFactory;
		private readonly Dictionary<string, Sink> OtherSinks;
		private readonly Sink NameSink;
		private readonly Sink LDAPGuidSink;
		private readonly Sink CommentSink;
		private readonly Sink EmailSink;
		private readonly Sink AlternateEmailSink;

		static SettableDisablerProperties IsNotDomainDisableer = new SettableDisablerProperties(null, KB.K("Only available on computer that is a member of a domain"),DomainAndIP.GetDomainName() != null);
	}
}
