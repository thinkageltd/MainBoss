using System;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.MVC.Models;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess {
	public delegate DBClient CreateDBSession(DBClient.Connection connection);
	#region MainBossWebAccessApplication
	public class MainBossWebAccessApplication : Thinkage.Libraries.Application {
		// The following came from Thinkage.MainBoss.Database.MB3Client.ConnectionDefinition
		public static readonly string ApplicationName = KB.I("MainBoss Web Access");
		public static readonly Version MinDBVersion = new Version(1, 1, 6, 2);
		public static Version MBWebAccessAppVersion {
			// TODO: VersionInfo.ProductVersion farts around with AssemblyInformationalVersionAttribute which no one specifies anywhere, and then falls back on a file version somewhere.
			// It turns out that the [AssemblyVersion] attribute is a NON-"custom" attribute and so does not appear in assembly.GetCustomAttributes and instead appears as the Version property of the assembly name.
			get {
				lock (LockObject) { // This is not necessary if System.Reflection.Assembly.GetExecutingAssembly().GetName().Version always returns the same object; concurrent calls would just set the value twice to the same object. TODO: Check this.
					if (pMBWebAccessAppVersionX == null)
						pMBWebAccessAppVersionX = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				}
				return pMBWebAccessAppVersionX;
			}
		}
		private static Version pMBWebAccessAppVersionX;
		private static readonly object LockObject = new object();
		protected override void CreateUIFactory() {
			// None required
		}
		public static new MainBossWebAccessApplication Instance => (MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance;
		private MainBossWebAccessApplication() {
			IsValid = false;
		}
		public delegate DBClient CreateDBSession(DBClient.Connection connection);

		internal static MainBossWebAccessApplication CreateNewApplicationObject(System.Globalization.CultureInfo formatCultureInfo, System.Globalization.CultureInfo messageCultureInfo) {
			MainBossWebAccessApplication result = new MainBossWebAccessApplication();
			new FixedUserInformationOverride(result, "", "", formatCultureInfo, messageCultureInfo, null);
			new StandardApplicationIdentification(result, "MainBossWebAccess", ApplicationName);
			new DatabaseFeaturesApplication(result,
				(DBClient.Connection connection) => {
					return new Thinkage.MainBoss.Database.MB3Client(connection);
				});
			new ApplicationPermissions(result, new MainBossPermissionsManager(Root.Rights));
			new Thinkage.MainBoss.Database.RaiseErrorTranslationKeyBuilder(result);
			return result;
		}
		public override void SetupApplication() {
			base.SetupApplication();
			Thinkage.Libraries.MVC.ConfigHandler config = Thinkage.Libraries.MVC.ConfigHandler.FetchConfig();
			SqlClient.Connection sql = new SqlClient.Connection(config.DatabaseServer, config.DatabaseName, null, AuthenticationCredentials.Default);
			Connection = new DatabaseConnection(sql);
			GetInterface<ISessionDatabaseFeature>().OpenDatabaseSession(new DBClient.Connection(sql, Thinkage.MainBoss.Database.dsUpgrade_1_1_4_2.Schema));
			var manager = (MainBossPermissionsManager)GetInterface<IPermissionsFeature>().PermissionsManager;
			manager.InitializeRolesGrantingPermission(GetInterface<ISessionDatabaseFeature>().Session);

			HasWebRequestsLicense = false;
			HasWebAccessLicense = false;
			HasRequestsLicense = false;
			HasWorkOrdersLicense = false;
			HasPurchaseOrdersLicense = false;
			HasInventoryLicense = false;
			var licensedObject = new Licensing.MBLicensedObjectSet(GetInterface<ISessionDatabaseFeature>().Session);
			// One minor oddity in this license checking is that, even if there is no MainBossWeb license, we will still check for MainBoss, Requests, and Purchasing licenses.
			// However, since we do not report on license warnings this is immaterial. If we did report on license warnings, we would have to make LicenseEnabledFeature hierarchical,
			// so that if one is enabled (because all its licenses are present), it would then be checked for sub-features to enable.
			LicenseManager.CheckLicensesAndEnableFeatures(
				new[] {
					new [] { new LicenseRequirement(Licensing.WebRequestsLicense) },
					new [] { new LicenseRequirement(Licensing.WebAccessLicense) }
				},
				new ILicenseEnabledFeature[] {
					new LicenseEnabledFeature(Licensing.WebRequestsLicense, delegate() { HasWebRequestsLicense = true; }),
					new LicenseEnabledFeature(Licensing.WebAccessLicense, delegate() { HasWebAccessLicense = true; }),
					new LicenseEnabledFeature(Licensing.RequestsLicense, delegate() { HasRequestsLicense = true; }),
					new LicenseEnabledFeature(Licensing.WorkOrderLaborLicense, delegate() { HasWorkOrdersLicense = true; }),
					new LicenseEnabledFeature(Licensing.PurchasingLicense, delegate() { HasPurchaseOrdersLicense = true; }),
					new LicenseEnabledFeature(Licensing.InventoryLicense, delegate() { HasInventoryLicense = false; }) // DISABLED for current release; no inventory stuff for 4.2
				},
				licensedObject, GetInterface<IDBVersionDatabaseFeature>().VersionHandler.GetLicenses(GetInterface<ISessionDatabaseFeature>().Session), null);
			ReAuthenticateMainBossUser();
			IsValid = true;
		}
		public override void TeardownApplication(Thinkage.Libraries.Application nextApplication) {
			base.TeardownApplication(nextApplication);
			if (SequenceCountDataSet != null)
				DestroySequenceCountDataSet();
			GetInterface<ISessionDatabaseFeature>().CloseDatabaseSession();
			ResetIdentificationProperties();
			IsValid = false;
		}
		public readonly Guid Id = Guid.NewGuid();
		#region User Authentication
		public void ReAuthenticateMainBossUser() {
			if (IsMainBossUser)
				return; // already done

			string userIdentification = System.Web.HttpContext.Current.User.Identity.Name?.ToLower();
			if (!String.IsNullOrEmpty(userIdentification)) {
				var rp = new AuthenticationRepository();
				var validUsers = from u in rp.Users()
								 where !u.Hidden.HasValue // Not hidden
								 && (
									u.AuthenticationCredential.ToLower().Equals(userIdentification)
									|| u.AuthenticationCredential.ToLower().StartsWith(userIdentification+';')
									|| u.AuthenticationCredential.ToLower().EndsWith(';'+userIdentification)
									|| u.AuthenticationCredential.ToLower().Contains(';'+userIdentification+';')
								 )
								 select u.Id;
				if (validUsers.Count() == 1) {
					UserID = validUsers.First();
					new FixedUserInformationOverride(Application.Instance, userIdentification, Application.Instance.WorkstationName, Application.InstanceFormatCultureInfo, Application.InstanceMessageCultureInfo, System.Web.HttpContext.Current.User);
					AuthenticationEntities.Contact contact = rp.GetContactForUser(UserID.Value);
					ContactID = contact.Id;
					ContactName = contact.Code;
					ContactEmail = contact.Email;
					var manager = (MainBossPermissionsManager)GetInterface<IPermissionsFeature>().PermissionsManager;
					manager.ResetPermissions();
					GetInterface<IDBVersionDatabaseFeature>().VersionHandler.LoadPermissions(GetInterface<ISessionDatabaseFeature>().Session, UserID.Value, false, delegate (string pattern, bool grant) {
						manager.SetPermission(pattern, grant);
					});
					return;
				}
			}
			ResetIdentificationProperties();
		}
		private void ResetIdentificationProperties() {
			UserID = null;
			new FixedUserInformationOverride(Application.Instance, "", Application.Instance.WorkstationName, Application.InstanceFormatCultureInfo, Application.InstanceMessageCultureInfo, null);
			ContactName = null;
			ContactID = null;
		}
		#endregion
		#region Properties specific to MainBossWebAccess
		/// <summary>
		/// Represent the connection to the mainboss database
		/// </summary>
		public DatabaseConnection Connection {
			get;
			private set;
		}
		public bool IsMainBossUser {
			get {
				return UserID != null;
			}
		}
		public Guid? UserID {
			get;
			private set;
		}
		/// <summary>
		/// Null if not authenticated MainBoss User for the windows user accessing the web pages
		/// </summary>
		public Guid? ContactID {
			get;
			private set;
		}
		public string ContactName {
			get;
			private set;
		}
		public string ContactEmail {
			get;
			private set;
		}
		public bool IsMainBossRequestor {
			get;
			set;
		}
		/// <summary>
		/// True if Web Access license allowing 'full' MainBoss WebAccess access is present
		/// </summary>
		public bool HasWebAccessLicense {
			get;
			private set;
		}
		/// <summary>
		/// True if Web Requests license allows submission of requests
		/// </summary>
		public bool HasWebRequestsLicense {
			get;
			private set;
		}
		/// <summary>
		/// True if licensing allows requests
		/// </summary>
		public bool HasRequestsLicense {
			get;
			private set;
		}
		/// <summary>
		/// True if licensing allows work orders
		/// </summary>
		public bool HasWorkOrdersLicense {
			get;
			private set;
		}
		/// <summary>
		/// True if licensing allows Inventory
		/// </summary>
		public bool HasInventoryLicense {
			get;
			private set;
		}
		/// <summary>
		/// True if licensing allows Purchase Orders
		/// </summary>
		public bool HasPurchaseOrdersLicense {
			get;
			private set;
		}
		/// <summary>
		/// Set true when the application object has managed to connect to a database, and determine valid information. If not true, the SetupApplication function
		/// will continue to be called on each request in an attempt to see if changes have been made to remedy the problem.
		/// </summary>
		public bool IsValid {
			get;
			private set;
		}
		/// <summary>
		/// A special flag to suppress the header of who the current user is and the Logout operation. At time of rendering the session object still exists
		/// and we can't use other conditions like application instance being null to determine the logout state. So we do it explicitly.
		/// </summary>
		public bool IsLoggedOut {
			get;
			set;
		}
		#endregion
		#region SequenceCounter & DataSet Management
		private Guid SequenceCountDataSetIdentifier = Guid.Empty;
		private SequenceCountDataSet SequenceCountDataSet;
		public void RememberSequenceCountDataSet(Guid identifier, SequenceCountDataSet ds) {
			if (SequenceCountDataSet != null) {
				SequenceCountDataSet.Destroy(); // eliminate a left over
				SequenceCountDataSet = null;
			}
			SequenceCountDataSet = ds;
			SequenceCountDataSetIdentifier = identifier;
		}
		/// <summary>
		/// Called when ready to Commit an existing SequenceCountDataSet; ObtainNewSequenceCountDataSet must have been called first!
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="spoiledCountTable"></param>
		/// <param name="seedVariable"></param>
		/// <param name="formatVariable"></param>
		/// <returns></returns>
		public SequenceCountDataSet RecallSequenceCountDataSet(Guid identifier) {
			if (identifier != SequenceCountDataSetIdentifier) {
				if (SequenceCountDataSet != null)
					SequenceCountDataSet.Destroy();
				SequenceCountDataSet = null;
				SequenceCountDataSetIdentifier = Guid.Empty;
			}
			return SequenceCountDataSet; // may be null if user came to our 'POST' page directly
		}
		/// <summary>
		/// We are done with the SequenceCountDataSet
		/// </summary>
		public void DestroySequenceCountDataSet() {
			SequenceCountDataSet.Destroy();
			SequenceCountDataSet = null;
			SequenceCountDataSetIdentifier = Guid.Empty;
		}
		#endregion
	}
	#endregion

	#region DataSet Persistance Support
	// for support in creating records requiring sequence numbers across web calls, we provide a service whereby we allocate a dsMB 
	// and SequenceCounterManager and keep it until the completion of the POST event.
	// If another GET request on the same session occurrs, we make sure we cleanup what ever is required before allocating a new one.
	// And we cleanup on session teardown if there are any pending
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
	public class SequenceCountDataSet {
		public readonly dsMB DataSet;
		public readonly SequenceCountManager SequenceCountManager;
		public SequenceCountDataSet(Thinkage.MainBoss.Database.MB3Client DB, DBI_Table spoiledCountTable, DBI_Variable seedVariable, DBI_Variable formatVariable) {
			DataSet = new dsMB(DB);
			SequenceCountManager = new SequenceCountManager(DB, spoiledCountTable, seedVariable, formatVariable);
		}
		/// <summary>
		/// Call when finished with this object
		/// </summary>
		public void Destroy() {
			if (SequenceCountManager != null)
				SequenceCountManager.Destroy();
			if (DataSet != null)
				DataSet.Dispose();
		}
	}
	#endregion

	#region IPermissionUsingApplicationFeature
	public interface IPermissionsFeature : IApplicationInterfaceGroup {
		/// <summary>
		/// Return the Permissions manager associated with the application
		/// </summary>
		IRightGrantor PermissionsManager {
			get;
		}
	}
	public class ApplicationPermissions : GroupedInterface<IApplicationInterfaceGroup>, IPermissionsFeature {
		public ApplicationPermissions(GroupedInterface<IApplicationInterfaceGroup> attachTo, IRightGrantor permissionsManager)
			: base(attachTo) {
			PermissionsManager = permissionsManager;
			RegisterService<IPermissionsFeature>(this);
		}

		#region IPermissionUsingApplication Members
		public IRightGrantor PermissionsManager {
			get;
			private set;
		}
		#endregion
	}
	#endregion
	#region ISessionDatabaseFeature
	/// <summary>
	/// Provide database session handling
	/// </summary>
	public interface ISessionDatabaseFeature : IApplicationInterfaceGroup {
		DBClient Session {
			get;
		}
		void OpenDatabaseSession(DBClient.Connection connection);
		void CloseDatabaseSession();
	}
	public interface IDBVersionDatabaseFeature : IApplicationInterfaceGroup {
		DBVersionHandler VersionHandler {
			get;
		}
	}

	public class DatabaseFeaturesApplication : GroupedInterface<IApplicationInterfaceGroup>, ISessionDatabaseFeature, IDBVersionDatabaseFeature {
		public DatabaseFeaturesApplication(GroupedInterface<IApplicationInterfaceGroup> attachTo, CreateDBSession sessionCreator)
			: base(attachTo) {
			DBSessionCreator = sessionCreator;
			RegisterService<ISessionDatabaseFeature>(this);
			RegisterService<IDBVersionDatabaseFeature>(this);
		}
		private readonly CreateDBSession DBSessionCreator;
		#region ISessionDatabaseFeature
		public void OpenDatabaseSession(DBClient.Connection connection) {
			try {
				DBSession = DBSessionCreator(connection);
				pVersionHandler = Thinkage.MainBoss.Database.MBUpgrader.UpgradeInformation.CheckDBVersion(DBSession, MainBossWebAccessApplication.MBWebAccessAppVersion, MainBossWebAccessApplication.MinDBVersion, dsMB.Schema.V.MinMBRemoteAppVersion, MainBossWebAccessApplication.ApplicationName);
			}
			catch (System.Exception ex) {
				if (DBSession != null) {
					DBSession.CloseDatabase();
					DBSession = null;
				}
				if (ex is GeneralException)
					throw;          // message should be good
				throw new GeneralException(ex, KB.K("There was a problem validating access to {0}"), connection.ConnectionInformation.DisplayNameLowercase);
			}
		}

		public void CloseDatabaseSession() {
			if (DBSession != null) {
				DBSession.CloseDatabase();
				DBSession = null;
			}
		}
		public DBClient Session {
			get {
				return DBSession;
			}
		}
		private DBClient DBSession;
		#endregion
		#region IDBVersionDatabaseFeature Members
		private DBVersionHandler pVersionHandler;
		public DBVersionHandler VersionHandler {
			get {
				return pVersionHandler;
			}
		}
		#endregion
	}
	#endregion

	#region Translation Helpers
	/// <summary>
	/// Support for encoded web pages to get text translated conveniently 
	/// </summary>
	public static class T {
		/// <summary>
		/// Return string format and arguments translated and Html.Encoded for display on a web page safely.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		public static string Text([Thinkage.Libraries.Translation.Context("Thinkage.MainBoss.WebAccess")]string key, params object[] o) {
			string translated = Strings.Format(KB.K(key), o);
			return (!String.IsNullOrEmpty(translated)) ? translated : String.Empty;
		}
	}
	#endregion
}
