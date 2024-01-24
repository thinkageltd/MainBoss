using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.TypeInfo;

namespace Thinkage.MainBoss.Database {
	#region SavedOrganizationSession
	public class SavedOrganizationSession : RegistrySession {
		private static Version Version_1000_0_0_0 = new Version(1000, 0, 0, 0); // migrate from OLD organizations to dsSavedOrganization structure
		private static Version Version_1000_1_0_0 = new Version(1000, 1, 0, 0); // move the PreferredOrganization settings to Variables in the dsSavedOrganization structure
		private static Version Version_1000_2_0_0 = new Version(1000, 2, 0, 0); // delete the LastSelectedOrganization* variables no longer defined.
		private static Version Version_1000_3_0_0 = new Version(1000, 3, 0, 0); // Add authentication credentials per entry and move the root directory to 4.0
		protected static Version LatestVersion = Version_1000_3_0_0;
		#region Connection
		/// <summary>
		/// Connection3 represents versions of MainBoss up to 4.0 where everything resided under "3.0"
		/// </summary>
		public class Connection3 : RegistrySession.Connection {
			public Connection3()
				: base(KB.I("localhost"),
				KB.I(KB.I("HKCU\\Software\\") + Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IIdentification>().SharedStorageIdentifier + KB.I("\\3.0")),
				Version_1000_1_0_0) {
				// Need to modify minor product version tag at end to the base '0' release where all original Organizations lists are kept until such time
				// as we determine whether we should migrate the Organization list for each version release or not.
			}
			public override Libraries.DBILibrary.IServer CreateServer() {
				return new SavedOrganizationServer3();
			}
		}
		/// <summary>
		/// Current Connection is kept separate from the older connections; conversion is done ONCE of old connections to new connections with appropriate defaulting of the new columns
		/// </summary>
		public new class Connection : RegistrySession.Connection {
			public Connection()
				: base(KB.I("localhost"),
				KB.I(KB.I("HKCU\\Software\\") + Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IIdentification>().SharedStorageIdentifier + KB.I("\\4.0")),
				LatestVersion) {
				// Need to modify minor product version tag at end to the base '0' release where all original Organizations lists are kept until such time
				// as we determine whether we should migrate the Organization list for each version release or not.
			}
			public override Libraries.DBILibrary.IServer CreateServer() {
				return new SavedOrganizationServer();
			}
		}
		#endregion
		#region Server
		#region Version 3 Server
		public class SavedOrganizationServer3 : RegistrySessionServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {
				SavedOrganizationSession session = new SavedOrganizationSession(connectInfo, this, schema);
				Version currentVersion;

				GetDBVersion(session, schema, out currentVersion);
				if (currentVersion.Major > 0) { // A structure exists of some form
					if (currentVersion < Version_1000_0_0_0) { // we have no existing structure from 3.0; we just start fresh with no copy of original structure
						session.UpgradeTo_1000_0_0_0();
						currentVersion = SetDBVersion(session, schema, Version_1000_0_0_0);
					}
					if (currentVersion < Version_1000_1_0_0) {
						var root = session.CreateItem(rootKeyDesignator);
						session.UpgradeTo_1000_1_0_0(root);
						currentVersion = SetDBVersion(session, schema, Version_1000_1_0_0);
					}
					if (currentVersion < Version_1000_2_0_0) {
						var root = session.CreateItem(rootKeyDesignator);
						session.UpgradeTo_1000_2_0_0(root);
						currentVersion = SetDBVersion(session, schema, Version_1000_2_0_0);
					}
				}
				//TODO: Change the above pattern to a table driven mechanism when it becomes too unwieldly to maintain the way it is.
				return session;
			}
		}
		#endregion
		#region Current Server
		public class SavedOrganizationServer : RegistrySessionServer {
			protected virtual SavedOrganizationSession CreateSession(IConnectionInformation connectInfo, DBI_Database schema) {
				return new SavedOrganizationSession(connectInfo, this, schema);
			}
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {

				SavedOrganizationSession session = CreateSession(connectInfo, schema);
				Version currentVersion;

				GetDBVersion(session, schema, out currentVersion);
				if (currentVersion.Major == 0 && CreateRegistryStructure(session, schema)) { // this is the FIRST time ever; one time only, COPY any existing 3.0 structure to this new structure
					Connection3 connection3 = new Connection3();
					var savedOrganization3 = new XAFClient(new DBClient.Connection(connection3, dsSavedOrganizations.Schema));
					Version org3Version;
					GetDBVersion((RegistrySession)savedOrganization3.Session, schema, out org3Version);
					if (org3Version.Major > 0) { // Something exists
												 // Copy the 3.0 tree to the 4.0 tree, and refresh the Version.
						RegistrySession.CloneSession((RegistrySession)savedOrganization3.Session, session);
						GetDBVersion(session, schema, out currentVersion);
					}
					savedOrganization3.CloseDatabase();
				}
				Upgrade(session, currentVersion, schema);
				return session;
			}
			protected void Upgrade(SavedOrganizationSession session, Version currentVersion, DBI_Database schema) {
				if (currentVersion < Version_1000_3_0_0) {
					var root = session.CreateItem(rootKeyDesignator);
					session.UpgradeTo_1000_3_0_0(root);
					currentVersion = SetDBVersion(session, schema, Version_1000_3_0_0);
				}
				// Something wrong; missing upgrade step
				System.Diagnostics.Debug.Assert(currentVersion == LatestVersion, "Missing upgrade version step");
				//TODO: Change the above pattern to a table driven mechanism when it becomes too unwieldly to maintain the way it is.
			}
		}
		#endregion
		#region Construction
		public SavedOrganizationSession(IConnectionInformation connection, IServer server, DBI_Database schema)
			: base(connection, server, schema) {
			try {
				UITaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (InvalidOperationException) {
				// If the environment we are being used in doesn't permit this SynchronizationContext, we leave UIFactory null to signal NOT to try and schedule tasks to probe databases
			}
		}
		#endregion
		#region Upgraders
		private void UpgradeTo_1000_0_0_0() {
			// Old organization structures may or may not have the CompactBrowsers registry key under a configured organization. We need to enumerate all the 
			// existing organizations, and ensure that 'column' CompactBrowsers exists
			FromRegType fromConverter;
			ToRegType toConverter;
			RegistryValueKind valueKind;
			GetConverters(dsSavedOrganizations.Schema.T.Organizations.F.CompactBrowsers.EffectiveType, out fromConverter, out toConverter, out valueKind);

			var organizations = GetItemEnumerable(dsSavedOrganizations.Schema.T.Organizations);
			foreach (KeyValuePair<RegistryKey, string> organization in organizations) {
				RegistryKey tKey = PrepareItemForWrite(organization);
				List<string> columns = new List<string>(tKey.GetValueNames());
				if (!columns.Contains(dsSavedOrganizations.Schema.T.Organizations.F.CompactBrowsers.Name))
					tKey.SetValue(dsSavedOrganizations.Schema.T.Organizations.F.CompactBrowsers.Name, toConverter(false), valueKind);
				tKey.Close();
			}
			CloseItemEnumerable(organizations);
		}
		private void UpgradeTo_1000_1_0_0(RegistryKey root) {
			// Forgot to propogate LastSavedSession and other values stored under Organizations into Variables in the new schema.
			RegistryKey varsToMigrate = root.OpenSubKey(KB.I("Organizations"), true);
			XAFClient forUpdate = new XAFClient(new DBClient.Connection(ConnectionObject, dsSavedOrganizations.Schema), this);
			using (var ds = new dsSavedOrganizations(forUpdate)) {
				ds.DisableUpdatePropagation();
				foreach (var valueName in varsToMigrate.GetValueNames()) {
					try {
						var value = varsToMigrate.GetValue(valueName);
						var vKey = root.OpenSubKey(dsSavedOrganizations.Schema.VariablesTable.Name + "\\" + valueName, true);
						DBI_Variable v = ds.DBISchema.Variables[valueName];
						if (value != null) {
							ds.EnsureDataVariableExists(v);
							ds.DB.EditVariable(ds, v);
							TypeInfo baseResultType = v.EffectiveType;
							var linkedResultType = v.EffectiveType as LinkedTypeInfo;
							if (linkedResultType != null)
								baseResultType = linkedResultType.BaseType;
							if (baseResultType is IdTypeInfo)
								ds.DataVariables[v].Value = v.EffectiveType.GenericAsNativeType(Guid.Parse((string)value), v.EffectiveType.GenericMinimalNativeType());
							else
								ds.DataVariables[v].Value = v.EffectiveType.GenericAsNativeType(value, v.EffectiveType.GenericMinimalNativeType());
						}
						varsToMigrate.DeleteValue(valueName);
						vKey.Close();
					}
					catch (System.Exception) {
					}
				}
				ds.DB.Update(ds);
				varsToMigrate.Close();
			}
		}
		private void UpgradeTo_1000_2_0_0(RegistryKey root) {
			// Delete the LastSelectedOrganization/LastSelectedOrganizationDebug variables
			root.DeleteSubKey(dsSavedOrganizations.Schema.VariablesTable.Name + "\\" + KB.I("LastSelectedOrganization"), false);
			root.DeleteSubKey(dsSavedOrganizations.Schema.VariablesTable.Name + "\\" + KB.I("LastSelectedOrganizationDebug"), false);
		}
		private void UpgradeTo_1000_3_0_0(RegistryKey root) {
			// and Add credential entries to all existing entries, defaulting to WindowsAuthentication, and null userid/password
			FromRegType authenticationMethodfromConverter;
			ToRegType authenticationMethodtoConverter;

			RegistryValueKind authenticationMethodValueKind;
			GetConverters(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod.EffectiveType, out authenticationMethodfromConverter, out authenticationMethodtoConverter, out authenticationMethodValueKind);
			// Move any Solo connection to a new organization record, and set the SoloOrganization variable to the id of that record. Then delete the Solo SubKey
			var solo = root.OpenSubKey(KB.I("Solo"), true);
			if (solo != null) {
				var connection = solo.OpenSubKey(KB.I("ConnectionDefinition"), true);
				if (connection != null) {
					Guid soloRowId;
					XAFClient forUpdate = new XAFClient(new DBClient.Connection(ConnectionObject, dsSavedOrganizations.Schema), this);
					using (var ds = new dsSavedOrganizations(forUpdate)) {
						ds.DisableUpdatePropagation();
						ds.EnsureDataTableExists(dsSavedOrganizations.Schema.T.Organizations);
						var soloRow = ds.T.Organizations.AddNewOrganizationsRow();
						soloRow.F.OrganizationName = KB.I("MainBoss Solo");
						soloRowId = soloRow.F.Id;
						ds.EnsureDataVariableExists(dsSavedOrganizations.Schema.V.SoloOrganization);
						DBI_Variable v = dsSavedOrganizations.Schema.V.SoloOrganization;
						ds.DB.EditVariable(ds, v);
						ds.DataVariables[v].Value = v.EffectiveType.GenericAsNativeType(soloRowId, v.EffectiveType.GenericMinimalNativeType());
						// leave the 'columns' to our cheater code later to copy the ConnectionDefinition key to the Organizationkey
						ds.DB.Update(ds);
					}
					var dest = root.OpenSubKey(KB.I("Organizations") + "\\" + soloRowId.ToString(), RegistryKeyPermissionCheck.Default, System.Security.AccessControl.RegistryRights.CreateSubKey | System.Security.AccessControl.RegistryRights.FullControl);
					Thinkage.Libraries.MSWindows.Registry.RegCopyTree(connection.Handle.DangerousGetHandle(), dest.Handle.DangerousGetHandle());
					dest.Close();
					connection.Close();
					solo.DeleteSubKey(KB.I("ConnectionDefinition"));
				}
				solo.Close();
				root.DeleteSubKey(KB.I("Solo"));
			}

			var organizations = GetItemEnumerable(dsSavedOrganizations.Schema.T.Organizations);
			foreach (KeyValuePair<RegistryKey, string> organization in organizations) {
				RegistryKey tKey = PrepareItemForWrite(organization);
				List<string> columns = new List<string>(tKey.GetValueNames());
				if (!columns.Contains(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod.Name))
					tKey.SetValue(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod.Name, authenticationMethodtoConverter(Thinkage.Libraries.DBILibrary.AuthenticationMethod.WindowsAuthentication), authenticationMethodValueKind);
				tKey.Close();
			}
			CloseItemEnumerable(organizations);
		}
		#endregion
		#region Accessors
		private XAFClient VariablesSession {
			get {
				if (pVariablesSession == null)
					pVariablesSession = new XAFClient(new DBClient.Connection(ConnectionObject, dsSavedOrganizations.Schema));
				return pVariablesSession;
			}
		}
		private XAFClient pVariablesSession = null;
		public override void CloseDatabase() {
			if (pVariablesSession != null) {
				pVariablesSession.CloseDatabase();
				pVariablesSession = null;
			}
			base.CloseDatabase();
		}
		GetNormalColumnValue DataBaseServerSource {
			get {
				if (pDataBaseServerSource == null) {
					SetNormalColumnValue noUpdate;
					GetExceptionColumnValue noException;
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.DataBaseServer, out pDataBaseServerSource, out noUpdate, out noException);
				}
				return pDataBaseServerSource;
			}
		}
		GetNormalColumnValue pDataBaseServerSource;
		GetNormalColumnValue DataBaseNameSource {
			get {
				if (pDataBaseNameSource == null) {
					SetNormalColumnValue noUpdate;
					GetExceptionColumnValue noException;
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.DataBaseName, out pDataBaseNameSource, out noUpdate, out noException);
				}
				return pDataBaseNameSource;
			}
		}
		GetNormalColumnValue pDataBaseNameSource;

		GetNormalColumnValue CredentialsAuthenticationMethodSource {
			get {
				if (pCredentialsAuthenticationMethodSource == null) {
					SetNormalColumnValue noUpdate;
					GetExceptionColumnValue noException;
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod, out pCredentialsAuthenticationMethodSource, out noUpdate, out noException);
				}
				return pCredentialsAuthenticationMethodSource;
			}
		}
		GetNormalColumnValue pCredentialsAuthenticationMethodSource;
		GetNormalColumnValue CredentialsUsernameSource {
			get {
				if (pCredentialsUsernameSource == null) {
					SetNormalColumnValue noUpdate;
					GetExceptionColumnValue noException;
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsUsername, out pCredentialsUsernameSource, out noUpdate, out noException);
				}
				return pCredentialsUsernameSource;
			}
		}
		GetNormalColumnValue pCredentialsUsernameSource;
		GetNormalColumnValue CredentialsPasswordSource {
			get {
				if (pCredentialsPasswordSource == null) {
					SetNormalColumnValue noUpdate;
					GetExceptionColumnValue noException;
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsPassword, out pCredentialsPasswordSource, out noUpdate, out noException);
				}
				return pCredentialsPasswordSource;
			}
		}
		GetNormalColumnValue pCredentialsPasswordSource;
		/// <summary>
		/// To ensure recaching of records in the CacheManager, we add a unique value (increasing) to each row returned.
		/// </summary>
		long GenerationStampValue = 0;
		#endregion
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			exceptionEvaluator = delegate (System.Exception e) {
				return Thinkage.Libraries.Exception.FullMessage(e);
			};
			FromRegType fromConverter;
			ToRegType toConverter;
			RegistryValueKind valueKind;
			GetConverters(dsSavedOrganizations.Schema.T.Organizations.F.Id.EffectiveType, out fromConverter, out toConverter, out valueKind);
			if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.IsPreferredOrganization) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					using (dsSavedOrganizations ds = new dsSavedOrganizations(VariablesSession)) {
						ds.DisableUpdatePropagation();
#if DEBUG
						VariablesSession.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.PreferredOrganizationDebug);
						NullableDataVariable<Guid> variable = ds.V.PreferredOrganizationDebug;
#else
						VariablesSession.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.PreferredOrganization);
						NullableDataVariable<Guid> variable = ds.V.PreferredOrganization;
#endif
						string fullname = sc.Name;
						return !variable.IsNull && (Guid)fromConverter(fullname.Substring(1 + fullname.LastIndexOf('\\'))) == variable.Value;
					}
				};
			}
			else if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.DBVersion
					|| sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Status
					|| sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Access
					|| sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.CanDropDatabase
				) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					DatabaseOnServerInformation dbInformation = null;
					if (UITaskFactory != null) {
						string fullname = sc.Name;
						Guid id = (Guid)fromConverter(fullname.Substring(1 + fullname.LastIndexOf('\\')));
						lock (DBVersions) {
							if (!DBVersions.TryGetValue(id, out dbInformation)) {
								DBVersions.Add(id, null); // flag we are trying to process one by making an entry with no information available.
								AuthenticationCredentials credentials = new AuthenticationCredentials(
									(AuthenticationMethod)CredentialsAuthenticationMethodSource(sc),
									(string)CredentialsUsernameSource(sc),
									MB3Client.OptionSupport.DecryptCredentialsPassword((string)CredentialsPasswordSource(sc))
									);
								SqlClient.Connection dbConnection = new SqlClient.Connection((string)DataBaseServerSource(sc), (string)DataBaseNameSource(sc), null, credentials);
								Task.Factory.StartNew(() => {
									var dbInfo = new DatabaseOnServerInformation(dbConnection);
									// update the null entry
									lock (DBVersions) {
										if (DBVersions.ContainsKey(id)) {
											DBVersions[id] = dbInfo;
											UITaskFactory.StartNew(() => // Notify the CacheManager of the updated values
											{
												OnRowChanged(dsSavedOrganizations.Schema.T.Organizations, id, Thinkage.Libraries.DBILibrary.Session.RowChangeTypes.AddedOrChanged);
											});
										}
									}
								});
							}
						}
					}
					if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Access)
						return dbInformation == null ? "" : (object)dbInformation.Access;
					if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.CanDropDatabase)
						return dbInformation == null ? false : dbInformation.CanDropDatabase;
					if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Status)
						return dbInformation == null ? KB.K("Probing").Translate() : (object)((dbInformation.AccessError?.Replace(Environment.NewLine, KB.I("; "))));
					return dbInformation == null ? KB.K("Unknown").Translate() : dbInformation.Version ?? KB.K("Unknown").Translate();
				};
			}
			else if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Generation) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					return GenerationStampValue++;
				};
			}
			else
				base.GetEvaluators(sourceColumnSchema, out normalEvaluator, out normalUpdater, out exceptionEvaluator);
		}

		// Use a taskfactory whose StartNew delegate(s) are run in the UI thread (apparently from Windows message loop)
		private readonly TaskFactory UITaskFactory;
		/// <summary>
		/// Our cache of DBVersions extracted from the organizations in our list.
		/// </summary>
		private Dictionary<Guid, DatabaseOnServerInformation> DBVersions = new Dictionary<Guid, DatabaseOnServerInformation>();
		/// <summary>
		/// Refresh our cache of DBVersions will cause probing to restart when we are next queried for information.
		/// </summary>
		public void PrepareToRefresh() {
			lock (DBVersions) {
				DBVersions.Clear();
			}
		}
	}
	#endregion
	#endregion

	#region SavedOrganizationSessionLocalAllUsers
	/// <summary>
	/// This variant provides a fixed KnownId for the AllUser organization record in the HKCU registry area
	/// </summary>
	public class SavedOrganizationSessionLocalAllUsers : SavedOrganizationSession {
		#region Construction
		public SavedOrganizationSessionLocalAllUsers(IConnectionInformation connection, IServer server, DBI_Database schema)
			: base(connection, server, schema) {
		}
		#endregion
		/// <summary>
		/// Provide the whole reason for this derivation - provide a fixed KnownId for the AllUser organization record.
		/// </summary>
		/// <returns></returns>
		protected override Guid GetGuidId() {
			return KnownIds.OrganizationMasterRecordId;
		}
		public new class Connection : SavedOrganizationSession.Connection {
			public Connection()
				: base() {
			}
			public override Libraries.DBILibrary.IServer CreateServer() {
				return new SavedOrganizationLocalAllUsersServer();
			}
		}
		public class SavedOrganizationLocalAllUsersServer : SavedOrganizationServer {
			protected override SavedOrganizationSession CreateSession(IConnectionInformation connectInfo, DBI_Database schema) {
				return new SavedOrganizationSessionLocalAllUsers(connectInfo, this, schema);
			}
		}
	}
	#endregion

	#region SavedOrganizationSessionAllUsers
	/// <summary>
	/// This variant provides a fixed KnownId for the AllUser organization record in the HKLM registry area
	/// </summary>
	public class SavedOrganizationSessionAllUsers : SavedOrganizationSessionLocalAllUsers {
		#region Connection
		/// <summary>
		/// This connection provides ability to control read/write access to the HKLM registry. Ordinary users require READ access in order to copy the AllUsers record from HKLM
		/// TYpically only administrators have write access to HKLM.
		/// </summary>
		public new class Connection : RegistrySession.Connection {
			public Connection(bool writeAccess)
				: base(KB.I("localhost"),
				KB.I(KB.I("HKLM\\Software\\") + Thinkage.Libraries.Application.Instance.GetInterface<Thinkage.Libraries.Application.IIdentification>().SharedStorageIdentifier + KB.I("\\4.0")),
				LatestVersion) {
				// Need to modify minor product version tag at end to the base '0' release where all original Organizations lists are kept until such time
				// as we determine whether we should migrate the Organization list for each version release or not.
				WriteAccessRequired = writeAccess;
			}
			public override Libraries.DBILibrary.IServer CreateServer() {
				return new SavedOrganizationServerAllUsers();
			}
			public readonly bool WriteAccessRequired;
		}
		#endregion
		#region Server
		#region Current Server
		public class SavedOrganizationServerAllUsers : SavedOrganizationServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {

				SavedOrganizationSessionAllUsers session = new SavedOrganizationSessionAllUsers(connectInfo, this, schema);
				return session;
			}
			public override ISession CreateDatabase(IConnectionInformation connectInfo, DBI_Database schema) {
				Version currentVersion;
				SavedOrganizationSessionAllUsers session = (SavedOrganizationSessionAllUsers)OpenSession(connectInfo, schema);
				GetDBVersion(session, schema, out currentVersion);
				if (currentVersion.Major == 0)
					CreateRegistryStructure(session, schema);
				Upgrade(session, currentVersion, schema);
				return session;
			}
			#endregion
		}
		#endregion
		#region Construction
		public SavedOrganizationSessionAllUsers(IConnectionInformation connection, IServer server, DBI_Database schema)
			: base(connection, server, schema) {
		}
		protected override bool RootWriteAccessRequired {
			get {
				return ((SavedOrganizationSessionAllUsers.Connection)ConnectionObject).WriteAccessRequired;
			}
		}
		#endregion

		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			exceptionEvaluator = delegate (System.Exception e) {
				return Thinkage.Libraries.Exception.FullMessage(e);
			};
			if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.IsPreferredOrganization) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					return false;
				};
			}
			else if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.DBVersion
					|| sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Status
					|| sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Access
				) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					return "";
				};
			}
			else if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.Generation) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					return 1;
				};
			}
			else
				base.GetEvaluators(sourceColumnSchema, out normalEvaluator, out normalUpdater, out exceptionEvaluator);
		}
	}
	#endregion
}