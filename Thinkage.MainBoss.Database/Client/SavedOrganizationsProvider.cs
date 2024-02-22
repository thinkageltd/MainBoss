using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Service;
using static Thinkage.Libraries.DBAccess.DBDataSet;

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
			public override IServer CreateServer() {
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
			public override IServer CreateServer() {
				return new SavedOrganizationServer();
			}
		}
		#endregion
		#region Server
		#region Version 3 Server
		public class SavedOrganizationServer3 : RegistrySessionServer {
			public override ISession OpenSession(IConnectionInformation connectInfo, DBI_Database schema) {
				SavedOrganizationSession session = new SavedOrganizationSession(connectInfo, this, schema);

				session.GetDBVersion(schema, out Version currentVersion);
				if (currentVersion.Major > 0) { // A structure exists of some form
					if (currentVersion < Version_1000_0_0_0) { // we have no existing structure from 3.0; we just start fresh with no copy of original structure
						session.UpgradeTo_1000_0_0_0();
						currentVersion = session.SetDBVersion(schema, Version_1000_0_0_0);
					}
					if (currentVersion < Version_1000_1_0_0) {
						var root = session.CreateItem(rootKeyDesignator);
						session.UpgradeTo_1000_1_0_0(root);
						currentVersion = session.SetDBVersion(schema, Version_1000_1_0_0);
					}
					if (currentVersion < Version_1000_2_0_0) {
						var root = session.CreateItem(rootKeyDesignator);
						session.UpgradeTo_1000_2_0_0(root);
						currentVersion = session.SetDBVersion(schema, Version_1000_2_0_0);
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

				session.GetDBVersion(schema, out Version currentVersion);
				if (currentVersion.Major == 0 && CreateRegistryStructure(session, schema)) { // this is the FIRST time ever; one time only, COPY any existing 3.0 structure to this new structure
					var session3 = new SavedOrganizationSession(new Connection3(), this, schema);
					session3.GetDBVersion(schema, out Version org3Version);
					if (org3Version.Major > 0) { // Something exists
												 // Copy the 3.0 tree to the 4.0 tree, and refresh the Version.
						RegistrySession.CloneSession(session3, session);
						session.GetDBVersion(schema, out currentVersion);
					}
					session3.CloseDatabase();
				}
				Upgrade(session, currentVersion, schema);
				return session;
			}
			protected void Upgrade(SavedOrganizationSession session, Version currentVersion, DBI_Database schema) {
				if (currentVersion < Version_1000_3_0_0) {
					var root = session.CreateItem(rootKeyDesignator);
					session.UpgradeTo_1000_3_0_0(root);
					currentVersion = session.SetDBVersion(schema, Version_1000_3_0_0);
				}
				// Something wrong; missing upgrade step
				System.Diagnostics.Debug.Assert(currentVersion == LatestVersion, "Missing upgrade version step");
				//TODO: Change the above pattern to a table driven mechanism when it becomes too unwieldly to maintain the way it is.
			}
		}
		#endregion
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
			GetConverters(dsSavedOrganizations.Schema.T.Organizations.F.CompactBrowsers.EffectiveType, out FromRegType fromConverter, out ToRegType toConverter, out RegistryValueKind valueKind);

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
			DBClient forUpdate = new DBClient(new DBClient.Connection(ConnectionObject, dsSavedOrganizations.Schema), this);
			using (var ds = new dsSavedOrganizations(forUpdate)) {
				ds.DisableUpdatePropagation();
				foreach (var valueName in varsToMigrate.GetValueNames()) {
					try {
						var value = varsToMigrate.GetValue(valueName);
						DBI_Variable v = ds.DBISchema.Variables[valueName];
						if (value != null) {
							ds.DB.EditVariable(ds, v);
							TypeInfo baseResultType = v.EffectiveType;
							if (v.EffectiveType is LinkedTypeInfo linkedResultType)
								baseResultType = linkedResultType.BaseType;
							if (baseResultType is IdTypeInfo)
								ds[v].Value = v.EffectiveType.GenericAsNativeType(Guid.Parse((string)value), v.EffectiveType.GenericMinimalNativeType());
							else
								ds[v].Value = v.EffectiveType.GenericAsNativeType(value, v.EffectiveType.GenericMinimalNativeType());
						}
						varsToMigrate.DeleteValue(valueName);
					}
					catch (System.Exception) {
					}
				}
				ds.DB.Update(ds);
				varsToMigrate.Close();
			}
		}
		private void UpgradeTo_1000_2_0_0(RegistryKey root) {
			DeleteVariable(dsSavedOrganizations_0_0_0_0_to_1000_1_0_0.Schema.V.LastSelectedOrganization);
			DeleteVariable(dsSavedOrganizations_0_0_0_0_to_1000_1_0_0.Schema.V.LastSelectedOrganizationDebug);
		}
		private void UpgradeTo_1000_3_0_0(RegistryKey root) {

			GetConverters(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod.EffectiveType, out FromRegType authenticationMethodfromConverter, out ToRegType authenticationMethodtoConverter, out RegistryValueKind authenticationMethodValueKind);
			// Move any Solo connection to a new organization record, and set the SoloOrganization variable to the id of that record. Then delete the Solo SubKey
			var solo = root.OpenSubKey(KB.I("Solo"), true);
			if (solo != null) {
				var connection = solo.OpenSubKey(KB.I("ConnectionDefinition"), true);
				if (connection != null) {
					Guid soloRowId;
					DBClient forUpdate = new DBClient(new DBClient.Connection(ConnectionObject, dsSavedOrganizations.Schema), this);
					using (var ds = new dsSavedOrganizations(forUpdate)) {
						ds.DisableUpdatePropagation();
						ds.EnsureDataTableExists(dsSavedOrganizations.Schema.T.Organizations);
						var soloRow = ds.T.Organizations.AddNewOrganizationsRow();
						soloRow.F.OrganizationName = KB.I("MainBoss Solo");
						soloRowId = soloRow.F.Id;
						DBI_Variable v = dsSavedOrganizations.Schema.V.SoloOrganization;
						ds.DB.EditVariable(ds, v);
						ds[v].Value = v.EffectiveType.GenericAsNativeType(soloRowId, v.EffectiveType.GenericMinimalNativeType());
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
					tKey.SetValue(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod.Name, authenticationMethodtoConverter(AuthenticationMethod.WindowsAuthentication), authenticationMethodValueKind);
				tKey.Close();
			}
			CloseItemEnumerable(organizations);
		}
		#endregion
		#region Accessors
		private DBClient VariablesSession {
			get {
				if (pVariablesSession == null)
					pVariablesSession = new DBClient(new DBClient.Connection(ConnectionObject, dsSavedOrganizations.Schema));
				return pVariablesSession;
			}
		}
		private DBClient pVariablesSession = null;
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
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.DataBaseServer, out pDataBaseServerSource, out SetNormalColumnValue noUpdate, out GetExceptionColumnValue noException);
				}
				return pDataBaseServerSource;
			}
		}
		GetNormalColumnValue pDataBaseServerSource;
		GetNormalColumnValue DataBaseNameSource {
			get {
				if (pDataBaseNameSource == null) {
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.DataBaseName, out pDataBaseNameSource, out SetNormalColumnValue noUpdate, out GetExceptionColumnValue noException);
				}
				return pDataBaseNameSource;
			}
		}
		GetNormalColumnValue pDataBaseNameSource;

		GetNormalColumnValue CredentialsAuthenticationMethodSource {
			get {
				if (pCredentialsAuthenticationMethodSource == null) {
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsAuthenticationMethod, out pCredentialsAuthenticationMethodSource, out SetNormalColumnValue noUpdate, out GetExceptionColumnValue noException);
				}
				return pCredentialsAuthenticationMethodSource;
			}
		}
		GetNormalColumnValue pCredentialsAuthenticationMethodSource;
		GetNormalColumnValue CredentialsUsernameSource {
			get {
				if (pCredentialsUsernameSource == null) {
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsUsername, out pCredentialsUsernameSource, out SetNormalColumnValue noUpdate, out GetExceptionColumnValue noException);
				}
				return pCredentialsUsernameSource;
			}
		}
		GetNormalColumnValue pCredentialsUsernameSource;
		GetNormalColumnValue CredentialsPasswordSource {
			get {
				if (pCredentialsPasswordSource == null) {
					GetEvaluators(dsSavedOrganizations.Schema.T.Organizations.F.CredentialsPassword, out pCredentialsPasswordSource, out SetNormalColumnValue noUpdate, out GetExceptionColumnValue noException);
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
		public void GetDBVersion(DBI_Database schema, out Version currentVersion) {
			DBIDataSet varData = ViewVariables(dsSavedOrganizations.Schema.V.DBVersion);
			try {
				currentVersion = new Version((string)varData[dsSavedOrganizations.Schema.V.DBVersion].Value);
			}
			catch (System.Exception) {
				currentVersion = new Version(0, 0, 0, 0);
			}
		}
		protected Version SetDBVersion(DBI_Database schema, Version newVersion) {
			DBIDataSet varData = ViewVariables(dsSavedOrganizations.Schema.V.DBVersion);
			try {
				varData[dsSavedOrganizations.Schema.V.DBVersion].Value = newVersion.ToString();
				UpdateGivenVariables(ServerExtensions.UpdateOptions.Normal, varData[dsSavedOrganizations.Schema.V.DBVersion]);
			}
			catch (System.Exception) {
				return new Version(0, 0, 0, 0);
			}
			return newVersion;
		}
		protected override void GetEvaluators(DBI_Column sourceColumnSchema, out GetNormalColumnValue normalEvaluator, out SetNormalColumnValue normalUpdater, out GetExceptionColumnValue exceptionEvaluator) {
			exceptionEvaluator = delegate (System.Exception e) {
				return Thinkage.Libraries.Exception.FullMessage(e);
			};
			GetConverters(dsSavedOrganizations.Schema.T.Organizations.F.Id.EffectiveType, out FromRegType fromConverter, out ToRegType toConverter, out RegistryValueKind valueKind);
			if (sourceColumnSchema == dsSavedOrganizations.Schema.T.Organizations.F.IsPreferredOrganization) {
				normalUpdater = null;
				normalEvaluator = delegate (RegistryKey sc) {
					using (dsSavedOrganizations ds = new dsSavedOrganizations(VariablesSession)) {
						ds.DisableUpdatePropagation();
#if DEBUG
						VariablesSession.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.PreferredOrganizationDebug);
						DBIVariable variable = ds.V.PreferredOrganizationDebug;
#else
						VariablesSession.ViewAdditionalVariables(ds, dsSavedOrganizations.Schema.V.PreferredOrganization);
						DBIVariable variable = ds.V.PreferredOrganization;
#endif
						string fullname = sc.Name;
						return variable.Schema.EffectiveType.GenericEquals((Guid)fromConverter(fullname.Substring(1 + fullname.LastIndexOf('\\'))), variable.Value);
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
												OnRowChanged(dsSavedOrganizations.Schema.T.Organizations, id, SessionExtensions.RowChangeTypes.AddedOrChanged);
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
			else if (sourceColumnSchema == dsVariablesService.Schema.T.__Variables.F.Value) {
				// The Registry session uses the VariablesService like everyone else, using a blob type for the value encoding type.
				// However, the old ad-hoc registry service code stored string variables in __Variables.Value as REG_SZ rather than REG_BINARY.
				// We have to map the string in the registry to a byte array that will ultimately decode to the correct value for the variable.
				normalEvaluator = delegate (RegistryKey sc) {
					var valueAsStored = sc.GetValue(sourceColumnSchema.Name);
					if (valueAsStored is string s)
						// Encode the string as a byte array, which is what should have been stored in the first place.
						valueAsStored = Schema.VariableEncoderProviderObject.GetValueEncoder(Libraries.XAF.Database.Service.MSSql.Server.NVARCHAR_MAX_NULLABLE_TypeInfo, Libraries.XAF.Database.Service.MSSql.Server.EncodedType).Encode(s);
					return (Byte[])valueAsStored;
				};
				normalUpdater = (sc, v) => {
					RegistryUpdates.Add(() => {
						// Going in the other direction, we have things already blob-encoded so we have to use a heuristic to determine if the value was a string
						// or an Id. The only string we stored was a version number, which is all 7-bit characters.
						if (v == null)
							try {
								sc.DeleteValue(sourceColumnSchema.Name);
							}
							catch (System.Exception) { }
						else {
							var blobValue = (Byte[])v;
							try {
								var stringValue = (string)Schema.VariableEncoderProviderObject.GetValueEncoder(Libraries.XAF.Database.Service.MSSql.Server.NVARCHAR_MAX_NULLABLE_TypeInfo, Libraries.XAF.Database.Service.MSSql.Server.EncodedType).UnEncode(blobValue);
								var regex = new System.Text.RegularExpressions.Regex("^[0-9.]+$");
								if (regex.IsMatch(stringValue)) {
									sc.SetValue(sourceColumnSchema.Name, stringValue, RegistryValueKind.String);
									return;
								}
							}
							catch {
							}
							sc.SetValue(sourceColumnSchema.Name, blobValue, RegistryValueKind.Binary);
						}
					});
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
			public override IServer CreateServer() {
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
			public override IServer CreateServer() {
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
				SavedOrganizationSessionAllUsers session = (SavedOrganizationSessionAllUsers)OpenSession(connectInfo, schema);
				session.GetDBVersion(schema, out Version currentVersion);
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