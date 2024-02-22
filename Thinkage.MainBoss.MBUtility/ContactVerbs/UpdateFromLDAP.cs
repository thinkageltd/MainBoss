using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.MBUtility {
	// This command arranges that the current Windows user have MB permissions at least as large as those granted to the creator of a new database.
	// We either use:
	// a new User record (and hide any existing one with the same Windows user)
	// a non-hidden User record for the current Windows user (preferring scoped over unscoped)
	// a hidden User record for the current Windows user which we unhide (preferring most-recently-hidden)
	//
	// Then we use the SecurityDefinitions to add group memberships to that user as if they were the DB creator (although any other memberships are preserved)
	//
	// One problem here is, should we be able to break into "old" databases? Doing so is more difficult because we need to know the creator-permissions
	// for old database versions. Not doing so is also a problem, though: If someone installs new MB then finds they have no one permitted to upgrade the DB,
	// they will have to re-install the old MB to break in.
	// Old databases would pose problems both in terms of our own DB operations tripping on schema mismatches (which can be minized on queries by providing a column list
	// so we aren't tripped up by "new" columns appearing, but can't do much about on Update; perhaps we need to use a subset schema like dsUpgrade), and also in terms
	// of the database-creator group list being incorrect. The latter is more likely since I expect these groups will be in flux for a while.
	//
	// We create new user records with a specific Scope, the domain name (for domain users) or machine name (for machine-local users) of the logged-on user.
	// This should be documented so that if the "break-in" user is to be used long-term, they might want to edit the User record and remove the scope if it is a machine name.
	internal class UpdateContactsFromLDAP {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Optable.Add(EmailAddresses = new StringValueOption("EmailAddresses", KB.K("The email addresses used to find the Active Directory entry for the contacts to be updated").Translate(), false));
				Optable.Add(EmailAddressPattern = new StringValueOption("EmailAddressesPattern", KB.K("All contacts with Active Directory References whose primary email address matches this pattern will have their contacts updated from Active Directory").Translate(), false));
				Optable.Add(ExcludeEmailAddressPattern = new StringValueOption("ExcludedEmailAddressesPattern", KB.K("Any contact whose primary email address matches this pattern will be excluded from being updated from Active Directory").Translate(), false));
				Optable.Add(ExcludeEmailAddress = new StringValueOption("ExcludeEmailAddresses", KB.K("All contacts with Active Directory References excluding these email addresses will have their contacts updated from Active Directory").Translate(), false));
				Optable.Add(FromEmailAddresses = new BooleanOption("FromEmailAddresses", KB.K("Use the primary email address in the contact as well as the Active Directory Reference to find the Active Directory entry").Translate()));
				Optable.Add(DeleteContacts = new BooleanOption("DeleteContacts", KB.K("Contacts that do not have a valid Active Directory Reference are deleted").Translate()));
				Optable.Add(RemoveLDAPReference = new BooleanOption("RemoveActiveDirectoryReference", KB.K("Clear the Active Directory Reference for any Contact that does not have a valid Active Directory Reference").Translate()));
				Optable.Add(PreservePrimaryEmail = new BooleanOption("PreservePrimaryEmailAddress", KB.K("Preserve the Contact's primary email address if it exists; otherwise the Active Directory Mail address will be added to the Contacts Alternate email address").Translate()));
				Optable.Add(Verbose = new BooleanOption("Verbose", KB.K("Provide additional information about changes that have occurred").Translate()));
				Optable.Add(UpdateAll = new BooleanOption("UpdateAll", KB.K("Update all the contacts that have Active Directory References").Translate()));
			}
			public StringValueOption EmailAddresses;
			public StringValueOption EmailAddressPattern;
			public StringValueOption ExcludeEmailAddressPattern;
			public StringValueOption ExcludeEmailAddress;
			public BooleanOption FromEmailAddresses;
			public BooleanOption DeleteContacts;
			public BooleanOption RemoveLDAPReference;
			public BooleanOption UpdateAll;
			public BooleanOption PreservePrimaryEmail;
			public BooleanOption Verbose;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "UpdateContactsfromactivedirectory";
				}
			}
			public override void RunVerb() {
				new UpdateContactsFromLDAP(this).Run();
			}
		}
		private UpdateContactsFromLDAP(Definition options) {
			Options = options;
		}
		private readonly Definition Options;

		private void Run() {
			LDAPEntry.CheckActiveDirectory(KB.I("UpdateContactsfromactivedirectory"));
			var EmailAddressPattern = Options.EmailAddressPattern.HasValue ? Options.EmailAddressPattern.Value : null;
			var ExcludeEmailAddressPattern = Options.ExcludeEmailAddressPattern.HasValue ? Options.ExcludeEmailAddressPattern.Value : null;
			var deleteContacts = Options.DeleteContacts.HasValue ? Options.DeleteContacts.Value : false;
			var removeLDAPReference = Options.RemoveLDAPReference.HasValue ? Options.RemoveLDAPReference.Value : false;
			var UpdateAll = Options.UpdateAll.HasValue ? Options.UpdateAll.Value : false;
			var FromEmailAddresses = Options.FromEmailAddresses.HasValue ? Options.FromEmailAddresses.Value : false;
			var PreservePrimaryEmail = Options.PreservePrimaryEmail.HasValue ? Options.PreservePrimaryEmail.Value : false;
			var verbose = Options.Verbose.HasValue ? Options.Verbose.Value : false;
			if (UpdateAll && Options.EmailAddresses.HasValue)
				throw new GeneralException(KB.K("Cannot have both '{0}' and '{1}'"), KB.I("UpdateAll"), KB.I("EmailAddresses"));
			if (FromEmailAddresses && Options.EmailAddresses.HasValue)
				throw new GeneralException(KB.K("Cannot have both '{0}' and '{1}'"), KB.I("FromEmailAddresses"), KB.I("EmailAddresses"));
			if (deleteContacts && removeLDAPReference)
				throw new GeneralException(KB.K("Cannot have both '{0}' and '{1}'"), KB.I("DeleteContacts"), KB.I("RemoveActiveDirrectoryReference"));
			if (EmailAddressPattern != null && Options.EmailAddresses.HasValue)
				throw new GeneralException(KB.K("Cannot have both '{0}' and '{1}'"), KB.I("EmailAddressPattern"), KB.I("EmailAddresses"));
			if (ExcludeEmailAddressPattern != null && Options.EmailAddresses.HasValue)
				throw new GeneralException(KB.K("Cannot have both '{0}' and '{1}'"), KB.I("ExcludeEmailAddressPattern"), KB.I("EmailAddresses"));
			Regex EmailAddressRE = null;
			Regex ExcludeEmailAddressRE = null;
			if (!string.IsNullOrWhiteSpace(EmailAddressPattern))
				try {
					EmailAddressRE = new Regex(EmailAddressPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				}
				catch (ArgumentException) {
					throw new GeneralException(KB.K("The {0} could not be parsed"), KB.I("EmailAddressPattern"));
				}
			if (!string.IsNullOrWhiteSpace(ExcludeEmailAddressPattern))
				try {
					ExcludeEmailAddressRE = new Regex(ExcludeEmailAddressPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				}
				catch (ArgumentException) {
					throw new GeneralException(KB.K("The {0} could not be parsed"), KB.I("ExcludeEmailAddressPattern"));
				}
			System.Version minDBVersionForRolesTable = new System.Version(1, 0, 4, 38); // The roles table appeared in its current form at this version
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			// Get a connection to the database that we are referencing
			new ApplicationTblDefaultsNoEditing(Thinkage.Libraries.Application.Instance, new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			var dbapp = new ApplicationWithSingleDatabaseConnection(Thinkage.Libraries.Application.Instance);
			try {
				var session = new MB3Client(connect);
				dbapp.SetAppAndOrganizationAndSession(oName, session);
				dbapp.SetVersionHandler(MBUpgrader.UpgradeInformation.CheckDBVersion(session, VersionInfo.ProductVersion, minDBVersionForRolesTable, dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool--Update Contacts")));
				dbapp.Session.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);
				// We need to pass a LicenseEnabledFeatureGroup that requires no licenses since CheckLicensesAndSetFeatureGroups requires at least one LicenseEnabledFeatureGroup to be enabled.
				// TODO: Why check for any licensing?
				dbapp.CheckLicensesAndSetFeatureGroups(new[] {
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.NamedUsersLicense, overLimitFatal: true) },
					new[] { new Libraries.Licensing.LicenseRequirement(Licensing.RequestsLicense, overLimitFatal: true) }
				}, new LicenseEnabledFeatureGroups[] { new LicenseEnabledFeatureGroups() }, new Licensing.MBLicensedObjectSet(session), dbapp.VersionHandler.GetLicenses(session), null, Licensing.ExpiryWarningDays);
				dbapp.InitializeUserId();
			}
			catch (System.Exception ex) {
				dbapp.CloseDatabaseSession();
				if (ex is GeneralException)
					throw;          // message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to the database {0} on server {1}"), connect.DBName, connect.DBServer);
			}
			DBClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			IEnumerable<MailAddress> EmailAddresses = ResolveEmailAddresses(Options.EmailAddresses);
			IEnumerable<MailAddress> ExcludeEmailAddresses = ResolveEmailAddresses(Options.ExcludeEmailAddress);
			List<Guid> ContactIds = new List<Guid>();
			if (EmailAddresses != null) {
				foreach (var EmailAddress in EmailAddresses) {
					using (dsMB ds = new dsMB(db)) {
						var ContactInfo = new AcquireContact(db, null, EmailAddress, false, false, null);
						if (ContactInfo.Exception != null)
							System.Console.WriteLine(Strings.Format(KB.K("Error: {0}"), Thinkage.Libraries.Exception.FullMessage(ContactInfo.Exception)));
						if (ContactInfo.WarningText != null)
							System.Console.WriteLine(Strings.Format(KB.K("Warning: {0}"), ContactInfo.WarningText));
						if (ContactInfo.ContactID != null)
							ContactIds.Add(ContactInfo.ContactID.Value);
					}
				}
			}
			else if (UpdateAll || ExcludeEmailAddresses != null || EmailAddressRE != null || ExcludeEmailAddressRE != null) {
				ContactIds = AllContactsInLDAP(db, FromEmailAddresses, ExcludeEmailAddresses, EmailAddressRE, ExcludeEmailAddressRE);
			}
			else
				throw new GeneralException(KB.K("Need a '{0}' or a '{1}' option to specify which Contacts to update"), KB.I("UpdateAll"), KB.I("EmailAddresses"));
			if (EmailAddresses != null && ContactIds.Count == 0)
				return; // the email parsing will generate valid error messages.
			var errorCount = 0;
			string originalCode = null;
			dsMB.ContactRow row = null;
			IEnumerable<LDAPEntry> ldapusers = null;
			foreach (var contactid in ContactIds) {
				using (dsMB ds = new dsMB(db)) {
					ldapusers = null;
					bool changed = false;
					ds.EnsureDataTableExists(dsMB.Schema.T.Contact, dsMB.Schema.T.Requestor);
					row = null;
					row = (dsMB.ContactRow)ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.Contact, new SqlExpression(dsMB.Path.T.Contact.F.Id).Eq(SqlExpression.Constant(contactid)));
					if (row == null)
						continue;
					originalCode = row.F.Code;
					try {
						bool tryemail = false;
						if (row.F.LDAPGuid != null) {
							ldapusers = LDAPEntry.GetActiveDirectoryGivenGuid(row.F.LDAPGuid.Value);
							if (ldapusers.Count() == 1)
								changed = LDAPEntryHelper.SetContactValues(row, ldapusers.First(), PreservePrimaryEmail);
							else {
								if (deleteContacts) {
									HideContacts(ds, row);
									changed = true;
								}
								else if (removeLDAPReference) {
									row.F.LDAPGuid = null;
									changed = true;
								}
								else
									tryemail = true;
							}
						}
						if (FromEmailAddresses && (row.F.Email != null || row.F.AlternateEmail != null) && (row.F.LDAPGuid == null || tryemail)) {
							var emailAddresses = ServiceUtilities.AlternateEmailAddresses(row.F.AlternateEmail);
							if (row.F.Email != null)
								emailAddresses.Add(row.F.Email);
							ldapusers = new List<LDAPEntry>();
							foreach (var ea in emailAddresses) {
								var ad = LDAPEntry.GetActiveDirectoryUsingEmail(ea);
								ldapusers = ldapusers.Union(ad, new LDAPEntryComparerByGuid());
							}
							if (ldapusers.Count() >= 1 && ldapusers.Select(e => e.Guid).Distinct().Count() == 1)
								changed = LDAPEntryHelper.SetContactValues(row, ldapusers.First(), PreservePrimaryEmail);
							else if (ldapusers.Count() > 1)
								System.Console.WriteLine(Strings.Format(KB.K("Contact '{0}' email address is defined in multiple Active Directory Users {1}"), row.F.Code, string.Join(", ", ldapusers.Select(e => Strings.IFormat("'{0}'", e.DisplayName)))));
						}
						if (tryemail && !changed)
							System.Console.WriteLine(Strings.Format(KB.K("Cannot get the Active Directory entry for Contact '{0}'"), row.F.Code));
					}
					catch (System.Exception ex) {
						throw new GeneralException(ex, KB.K("Cannot get the Active Directory entry for Contact '{0}'"), row.F.Code);
					}
					try {
						if (changed) {
							db.Update(ds);
							if (row.F.Hidden != null)
								System.Console.WriteLine(Strings.Format(KB.K("Contact '{0}' was deleted"), row.F.Code));
							else if (originalCode != row.F.Code & verbose)
								System.Console.WriteLine(Strings.Format(KB.K("Contact '{0}' was updated, and was changed to {1}"), originalCode, row.F.Code));
							else if (verbose)
								System.Console.WriteLine(Strings.Format(KB.K("Contact '{0}' was updated"), originalCode, row.F.Code));
							changed = false;
						}
					}
					catch (System.Exception ex) {
						errorCount++;
						// Determine if we received a duplicate code trying to save our restored contact
						if (!(ex is InterpretedDbException ie) || ie.InterpretedErrorCode != InterpretedDbExceptionCodes.ViolationUniqueConstraint) {
							System.Console.WriteLine(Strings.Format(KB.K("Cannot update Contact '{0}'; {1}"), originalCode ?? row.F.Code, Thinkage.Libraries.Exception.FullMessage(ex)));
						}
					}
					try {
						if (originalCode != row.F.Code && changed) {
							var shouldbe = row.F.Code;
							row.F.Code = originalCode; //put it back to the original code;
							db.Update(ds);
							if (verbose)
								System.Console.WriteLine(Strings.Format(KB.K("Contact '{0}' was updated, but was unable to change the Contact Code to {1}"), row.F.Code, shouldbe));
						}
					}
					catch (System.Exception ex) {
						errorCount++;
						if (row == null)
							System.Console.WriteLine(Strings.Format(KB.K("Cannot update Contact ID '{0}'; {1}"), row.F.Id, Thinkage.Libraries.Exception.FullMessage(ex)));
						else
							System.Console.WriteLine(Strings.Format(KB.K("Cannot update Contact '{0}'; {1}"), originalCode ?? row.F.Code, Thinkage.Libraries.Exception.FullMessage(ex)));
					}
					if (errorCount > 10)
						throw new GeneralException(KB.K("Too many errors to continue"));
				}
			}
		}

		private static void HideContacts(dsMB ds, dsMB.ContactRow contactRow) {
			var now = DateTime.Now;
			contactRow.F.Hidden = now;
			var requestorRow = (dsMB.RequestorRow)ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.Requestor, new SqlExpression(dsMB.Path.T.Requestor.F.ContactID).Eq(SqlExpression.Constant(contactRow.F.Id)));
			requestorRow.F.Hidden = now;
		}

		private static List<Guid> AllContactsInLDAP(DBClient db, bool FromEmailAddresses, IEnumerable<MailAddress> ExcludeEmailAddresses, Regex EmailAddressRE, Regex ExcludeEmailAddressRE) {
			List<Guid> contactIds = new List<Guid>();
			using (dsMB ds = new dsMB(db)) {
				ds.EnsureDataTableExists(dsMB.Schema.T.Contact);
				SqlExpression test = new SqlExpression(dsMB.Path.T.Contact.F.LDAPGuid).IsNotNull();
				if (FromEmailAddresses || ExcludeEmailAddresses != null || ExcludeEmailAddressRE != null || EmailAddressRE != null)
					test = test.Or(new SqlExpression(dsMB.Path.T.Contact.F.Email).IsNotNull()).Or(new SqlExpression(dsMB.Path.T.Contact.F.AlternateEmail).IsNotNull());
				if (ExcludeEmailAddresses != null)
					test = test.And(new SqlExpression(dsMB.Path.T.Contact.F.Email).In(SqlExpression.Constant(new HashSet<object>(ExcludeEmailAddresses.Select(e => (object)e.Address)))).Not());
				test = test.And(new SqlExpression(dsMB.Path.T.Contact.F.Hidden).IsNull());
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.Contact, test, null, null);
				foreach (var row in ds.T.Contact.Rows) {
					if (row.F.LDAPGuid != null) {
						contactIds.Add(row.F.Id);
						continue;
					}
					if (!FromEmailAddresses)
						continue;
					if (row.F.Email == null && row.F.AlternateEmail == null)
						continue;
					var emailAddresses = ServiceUtilities.AlternateEmailAddresses(row.F.AlternateEmail);
					if (row.F.Email != null)
						emailAddresses.Add(row.F.Email);
					foreach (var a in emailAddresses) {
						if (ExcludeEmailAddresses != null && ExcludeEmailAddresses.Any(e => string.Compare(e.Address, row.F.Email, true) == 0))
							continue;
						if (EmailAddressRE != null && !EmailAddressRE.Match(row.F.Email).Success)
							continue;
						if (ExcludeEmailAddressRE != null && ExcludeEmailAddressRE.Match(row.F.Email).Success)
							continue;
						contactIds.Add(row.F.Id);
						break;
					}
				}
			}
			return contactIds;
		}

		private static List<MailAddress> ResolveEmailAddresses(StringValueOption option) {
			if (!option.HasValue)
				return null;
			var l = new List<System.Net.Mail.MailAddress>();
			foreach (var e in option.Value.Split(new char[] { ' ', ',', ';' }))
				try {
					l.Add(Thinkage.Libraries.Mail.MailAddress(e));
				}
				catch (System.FormatException) {
					System.Console.WriteLine(Strings.Format(KB.K("Error: '{0}' is not a valid email address"), e));
				}
			return l.Count == 0 ? null : l;
		}
		public void ObtainSession() {
			throw new System.NotImplementedException();
		}

		public void ReleaseSession() {
			throw new System.NotImplementedException();
		}
	}
}
