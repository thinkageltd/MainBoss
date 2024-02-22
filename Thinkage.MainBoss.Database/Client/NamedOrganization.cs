using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.MSWindows;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	#region NamedOrganization
	public class NamedOrganization {
		// This is an MBConnectionDefinition along with its Id and display name.

		// This ctor is used to make a new (unsaved) entry
		public NamedOrganization(string displayName, MB3Client.IMBConnectionParameters connectionDefinition)
			: this(null, displayName, connectionDefinition) {
		}
		/// <summary>
		/// This creates a NamedOrganization to be saved with a saveName
		/// </summary>
		/// <param name="id"></param>
		/// <param name="displayName"></param>
		/// <param name="connection"></param>
		internal NamedOrganization(Guid? id, string displayName, MB3Client.IMBConnectionParameters connectionInfo) {
			Id = id;
			DisplayName = displayName;
			MBConnectionParameters = connectionInfo;
			ConnectionDefinition = connectionInfo.Connection as MB3Client.MBConnectionDefinition; // Non server connections this will be null
		}
		public readonly MB3Client.IMBConnectionParameters MBConnectionParameters;
		public readonly MB3Client.MBConnectionDefinition ConnectionDefinition; // provided as short cut for all the NamedOrganization users that expect an MBConnection Definition to be here
		public readonly Guid? Id;
		public readonly string DisplayName;
		public override string ToString() {
			return DisplayName;
		}
	}
	#endregion
	#region OrganizationConnection Definitions
	public class MainBossNamedOrganizationStorage {
		/// <summary>
		/// This class provides the old interface to handling saved organizations in the user registry. It is now implemented using the dsSavedOrganization registry session
		/// methods.
		/// </summary>
		public MainBossNamedOrganizationStorage(IConnectionInformation connection)
			: base() {
			Session = new DBClient(new DBClient.Connection(connection, dsSavedOrganizations.Schema));
		}
		protected DBClient Session;
		/// <summary>
		/// Return the saved connection for the given connection id, or null if none exists by that id.
		/// Note that connectionName is the Guid identification of the organization.
		/// </summary>
		/// <param name="connectionName"></param>
		/// <returns></returns>
		public NamedOrganization Load([Invariant] Guid? id) {
			if(id == null)
				return null;

			using(var ds = new dsSavedOrganizations(Session)) {
				//				dsSavedOrganizations.OrganizationsRow r = (dsSavedOrganizations.OrganizationsRow)ds.DB.ViewAdditionalRow(ds, dsSavedOrganizations.Schema.T.Organizations, new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.Id).Eq(SqlExpression.Constant(id)));
								ds.DB.ViewAdditionalRows(ds, dsSavedOrganizations.Schema.T.Organizations, new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.Id).Eq(SqlExpression.Constant(id)),
					new SqlExpression[] {
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.OrganizationName),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.CompactBrowsers),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.DataBaseName),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.DataBaseServer),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.CredentialsUsername),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.CredentialsPassword),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.CredentialsAuthenticationMethod),
						new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.PreferredApplicationMode),
					}, null);
				if (ds.T.Organizations.Rows.Count != 1)
					return null;
				var r = (dsSavedOrganizations.OrganizationsRow)ds.T.Organizations.Rows[0];
					
				MB3Client.MBConnectionDefinition conn = new MB3Client.MBConnectionDefinition(r);
				return new NamedOrganization(id, r.F.OrganizationName, conn);
			}
		}
		/// <summary>
		/// Saves the given NamedOrganization, overwriting any previous connection by the same name.
		/// </summary>
		public NamedOrganization Save(NamedOrganization o) {
			using(var ds = new dsSavedOrganizations(Session)) {
				dsSavedOrganizations.OrganizationsRow r = null;
				if(o.Id != null)
					r = (dsSavedOrganizations.OrganizationsRow)ds.DB.EditSingleRow(ds, dsSavedOrganizations.Schema.T.Organizations, new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.Id).Eq(SqlExpression.Constant(o.Id)));
				if(r == null) {
					ds.EnsureDataTableExists(dsSavedOrganizations.Schema.T.Organizations);
					r = ds.T.Organizations.AddNewOrganizationsRow();
					r.F.DataBaseName = o.ConnectionDefinition.DBName;
					r.F.DataBaseServer = o.ConnectionDefinition.DBServer;
					r.F.CompactBrowsers = o.ConnectionDefinition.CompactBrowsers;
					r.F.PreferredApplicationMode = (sbyte)o.ConnectionDefinition.ApplicationMode;
					r.F.CredentialsAuthenticationMethod = (sbyte)o.ConnectionDefinition.DBCredentials.Type;
					r.F.CredentialsUsername = o.ConnectionDefinition.DBCredentials.Username;
					r.F.CredentialsPassword = MB3Client.OptionSupport.EncryptCredentialsPassword(o.ConnectionDefinition.DBCredentials.Password);
				}
				r.F.OrganizationName = o.DisplayName;
				ds.DB.Update(ds);
				return new NamedOrganization(r.F.Id, o.DisplayName, o.ConnectionDefinition); // if we created a new one, return a NamedOrganization with the proper Id field.
			}
		}
		public NamedOrganization Replace(NamedOrganization old, NamedOrganization replacement) {
			bool keepPreferredIfSame = IsPreferredOrganizationId(replacement);
			Delete(old);
			replacement = Save(replacement);
			if(keepPreferredIfSame)
				PreferredOrganizationId = replacement.Id;
			return replacement;
		}
		public List<NamedOrganization> GetOrganizationNames(string filterByOrganizationName = null) {
			List<NamedOrganization> list = new List<NamedOrganization>();
			using(var ds = new dsSavedOrganizations(Session)) {
				try {
					ds.DB.ViewAdditionalRows(ds, dsSavedOrganizations.Schema.T.Organizations); //TODO: Use the filterByOrganizationName to do the view and eliminate our comparison below
					foreach(dsSavedOrganizations.OrganizationsRow r in ds.T.Organizations) {
						NamedOrganization o = Load(r.F.Id);
						if(filterByOrganizationName == null || string.Equals(filterByOrganizationName, o.DisplayName, StringComparison.InvariantCultureIgnoreCase))
							list.Add(o);
					}
				}
				catch {
				}
			}
			return list;
		}
		public void Delete(NamedOrganization o) {
			if(IsPreferredOrganizationId(o))
				PreferredOrganizationId = null;
			using(var ds = new dsSavedOrganizations(Session)) {
				dsSavedOrganizations.OrganizationsRow r = (dsSavedOrganizations.OrganizationsRow)ds.DB.EditSingleRow(ds, dsSavedOrganizations.Schema.T.Organizations, new SqlExpression(dsSavedOrganizations.Path.T.Organizations.F.Id).Eq(SqlExpression.Constant(o.Id)));
				r.Delete();
				ds.DB.Update(ds);
			}
		}
		public bool IsPreferredOrganizationId(NamedOrganization o) {
			// Because the registry is caseless and invariant, we have to do a similar compare of the names.
			Guid? preferredName = PreferredOrganizationId;
			if(preferredName == null)
				return o.Id == null;
			return o.Id.Equals(preferredName);
		}
		public Guid? PreferredOrganizationId {
			get {
				using(var ds = new dsSavedOrganizations(Session)) {
					try {
#if DEBUG
						ds.DB.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.PreferredOrganizationDebug);
						return (Guid?)ds.V.PreferredOrganizationDebug.Value;
#else
						ds.DB.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.PreferredOrganization);
						return (Guid?)ds.V.PreferredOrganization.Value;
#endif
					}
					catch { // any error means we do not know
						return null;
					}
				}
			}
			set {
				try {
					using(var ds = new dsSavedOrganizations(Session)) {
#if DEBUG
						ds.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.PreferredOrganizationDebug);
						ds.V.PreferredOrganizationDebug.Value = value;
						ds.DB.Update(ds);
#else
						ds.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.PreferredOrganization);
						ds.V.PreferredOrganization.Value = value;
						ds.DB.Update(ds);
#endif
					}
				}
				catch { }
			}
		}
	}

	public class MainBossSoloNamedOrganizationStorage : MainBossNamedOrganizationStorage {
		public MainBossSoloNamedOrganizationStorage() : base(new SavedOrganizationSession.Connection()) {
		}
		public new NamedOrganization Load([Invariant] Guid? id) {
			using (var ds = new dsSavedOrganizations(Session)) {
				ds.DB.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.SoloOrganization);
				return base.Load((Guid?)ds.V.SoloOrganization.Value);
			}
		}
		public new NamedOrganization Save(NamedOrganization o) {
			NamedOrganization solo = base.Save(o);
			using (var ds = new dsSavedOrganizations(Session)) {
				ds.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.SoloOrganization);
				ds.V.SoloOrganization.Value = solo.Id.Value;
				ds.DB.Update(ds);
			}
			return solo;
		}
		public new void Delete(NamedOrganization o) {
			base.Delete(o);
			using (var ds = new dsSavedOrganizations(Session)) {
				ds.DB.EditVariable(ds, dsSavedOrganizations.Schema.V.SoloOrganization);
				ds.V.SoloOrganization.Value = null;
				ds.DB.Update(ds);
			}
		}
		#endregion
	}
}
