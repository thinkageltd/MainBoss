using System;
using System.Collections.Generic;
using System.Threading;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.Libraries.XAF.UI;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// MainBoss database client.  Defines extra support on top of an XAFClient specific
	/// to the structure of the MainBoss database.
	/// </summary>
	public class MB3Client : XAFClient {
		public delegate void PopulateDatabaseDelegate(MB3Client mbdb);

		#region Option Support
		public static class OptionSupport {
			#region Encryption/Decryption of credentials password
			public static string DecryptCredentialsPassword(string spwd) {
				if (spwd == null)
					return null;
				Byte[] pwBytes = Convert.FromBase64String(spwd);
				Byte[] decrypted = System.Security.Cryptography.ProtectedData.Unprotect(pwBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
				char[] chars = new char[decrypted.Length / sizeof(char)];
				System.Buffer.BlockCopy(decrypted, 0, chars, 0, decrypted.Length);
				return new string(chars);
			}
			public static string EncryptCredentialsPassword(string spwd) {
				if (spwd == null)
					return null;
				byte[] bytes = new byte[spwd.Length * sizeof(char)];
				System.Buffer.BlockCopy(spwd.ToCharArray(), 0, bytes, 0, bytes.Length);
				Byte[] encrypted = System.Security.Cryptography.ProtectedData.Protect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
				return Convert.ToBase64String(encrypted);
			}
			#endregion

			public class NoOrganizationException : GeneralException {
				public NoOrganizationException(GeneralException inner)
					: base(inner, KB.K("No organization name or database server was specified and you have no default organization")) {
				}
			}
			// These strings serve as the (abbreviable) option name for the command line for MB,
			// the @Requests Admin program, and some MBUtility verbs.
			// Being option names these will also appear as labels on the OptableForm if one is built from an optable containing one of these options.
			// TODO: The option names and the web/direct strings are currently treated as pre-translated and are thus invariant.
			// The application mode names *are* translated.
			// TODO: We use DatabaseEnums.ApplicationModeName to get the app mode names which are then translated. There is no garantee that the result will be
			// suitable for abbreviation matching or that the multiple possibilities will not be ambiguous for some strings.
			private const string OrganizationNameCommandArgument = "OrganizationName";
			private const string DataBaseNameCommandArgument = "DataBaseName";
			private const string DataBaseServerCommandArgument = "DataBaseServer";
			private const string ApplicationCommandArgument = "Mode";
			private const string CredentialsAuthenticationMethodArgument = "AuthenticationType";
			private const string CredentialsAuthenticationUsernameArgument = "AuthenticationUsername";
			private const string CredentialsAuthenticationPasswordArgument = "AuthenticationPassword";

			// This is a list of the modes that the MainBoss application can start in.
			// Except for PickOrganization, there should be Mode definitions in the TblDrivenMainBossApplication for these, and only these, modes.
			public static readonly DatabaseEnums.ApplicationModeID[] ApplicationIdChoices = new DatabaseEnums.ApplicationModeID[] {
				DatabaseEnums.ApplicationModeID.Requests,			// Just requests, admin, related definition tables
				DatabaseEnums.ApplicationModeID.Normal,				// full mainboss and admin
				DatabaseEnums.ApplicationModeID.Sessions,			// license keys, sessions in progress
				DatabaseEnums.ApplicationModeID.Administration,		// licensing, users
				DatabaseEnums.ApplicationModeID.PickOrganization	// This must be LAST in this table so the includePickOrganization argument works.
			};

			#region static methods to create specific options for an optable
			public static StringValueOption CreateOrganizationNameOption(bool required) {
				return new StringValueOption(OrganizationNameCommandArgument,
					KB.K("The name of the maintenance organization.").Translate(), required);
			}
			public static StringValueOption CreateServerNameOption(bool required) {
				return new StringValueOption(DataBaseServerCommandArgument,
					KB.K("The database server where the MainBoss database is hosted").Translate(), required);
			}
			public static StringValueOption CreateDatabaseNameOption(bool required) {
				return new StringValueOption(DataBaseNameCommandArgument,
					KB.K("The name of the database containing the MainBoss data on the database server.").Translate(), required);
			}
			public static KeywordValueOption CreateApplicationOption(bool includePickOrganization, bool required) {
				int l = ApplicationIdChoices.Length;
				if (!includePickOrganization)
					--l;
				string[] choices = new string[l];
				for (int i = l; --i >= 0;)
					// Note that as with all other options, we use the untranslated text
					choices[i] = DatabaseEnums.ApplicationModeName(ApplicationIdChoices[i]).Translate(null);

				return new KeywordValueOption(ApplicationCommandArgument,
					KB.K("Application to run").Translate(), required,
					choices);
			}
			public static BooleanOption CreateCompactBrowsersOption(bool required) {
				return new BooleanOption("CompactBrowsers", KB.K("Make the browser screens more compact by not showing details of the selected record").Translate(), required);
			}
			public static KeywordValueOption CreateAuthenticationMethodOption(bool required) {
				int l = Thinkage.Libraries.DBILibrary.AuthenticationCredentials.AuthenticationMethodProvider.Values.Length;
				string[] choices = new string[l];
				for (int i = l; --i >= 0;)
					// Note that as with all other options, we use the untranslated text
					choices[(int)Thinkage.Libraries.DBILibrary.AuthenticationCredentials.AuthenticationMethodProvider.Values[i]] = Thinkage.Libraries.DBILibrary.AuthenticationCredentials.AuthenticationMethodProvider.Labels[i].Translate(null);

				return new KeywordValueOption(CredentialsAuthenticationMethodArgument,
					KB.K("Server authentication method to use").Translate(), required,
					choices);
			}
			public static StringValueOption CreateCredentialsAuthenticationUsernameOption(bool required) {
				return new StringValueOption(CredentialsAuthenticationUsernameArgument,
					KB.K("The authentication username to use.").Translate(), required);
			}
			public static StringValueOption CreateCredentialsAuthenticationPasswordOption(bool required) {
				return new StringValueOption(CredentialsAuthenticationPasswordArgument,
					KB.K("The authentication password to use.").Translate(), required);
			}

			#endregion
			#region static methods to convert between the Value of an option object and the effective value
			// Some options may have a Value that is not in the same value-set as the caller expects. In particular the KeywordValueOption's value
			// is just an index into the list of keywords, and a mapping is required between these indices and the actual enumerated values that they
			// correspond to.
			public static DatabaseEnums.ApplicationModeID DecodeApplicationOptionValue(int optionValue) {
				if (optionValue < 0 || optionValue >= ApplicationIdChoices.Length)
					throw new System.ArgumentOutOfRangeException(KB.I("optionValue"));
				return ApplicationIdChoices[optionValue];
			}
			public static int EncodeApplicationOptionValue(DatabaseEnums.ApplicationModeID appId) {
				for (int i = ApplicationIdChoices.Length; --i >= 0;)
					if (appId == ApplicationIdChoices[i])
						return i;
				throw new System.ArgumentOutOfRangeException(KB.I("appId"));
			}
			#endregion
			#region static method to resolve any AllUser database configuration that may exist on the local machine
			[Flags]
			public enum LocalAllUserRecordUpdateBehaviors {
				/// <summary>
				/// An AllUsers configuration record resulted in the CREATION of a LocalAllUsers record in the organization list
				/// </summary>
				LocalRecordCreated = 1,
				/// <summary>
				/// An existing LocalAllUsers record changed as a result of changes in the underlying AllUser's record
				/// </summary>
				LocalRecordUpdated = 2,
				/// <summary>
				/// The PreferredOrganization IS the LocalAllUsers record.
				/// </summary>
				PreferredOrganizationDemanded = 4,
				/// <summary>
				/// An authentication change requires the user to provide credentials
				/// </summary>
				AuthenticationCredentialsRequired = 8
			}

			/// <summary>
			/// Determine if any AllUser organization configuration exists. If not, we are done and startup proceeds as normal (return true)
			/// If a configuration DOES exist, update (or create) a local user copy of the configuration. Return true if the user isn't required to provide any information 
			/// like username/password that only they know.
			/// </summary>
			/// <returns>true if we determine the user is required to do something interactively (PickOrganization), otherwise false to permit normal application startup</returns>
			public static LocalAllUserRecordUpdateBehaviors ResolveAllUserConfiguration() {
				MainBossNamedOrganizationStorage organizations = new MainBossNamedOrganizationStorage(new SavedOrganizationSessionAllUsers.Connection(writeAccess: false));
				NamedOrganization auOrg;
				LocalAllUserRecordUpdateBehaviors retValue = 0;
				Guid? preferredOrganizationID = null;
				try {
					auOrg = organizations.Load(KnownIds.OrganizationMasterRecordId);
					preferredOrganizationID = organizations.PreferredOrganizationId;
				}
				catch {
					return retValue; // probably error due to nonexistence of the HKLM organization entries
				}
				if (auOrg != null) {
					// There IS one. See if user has a copy and compare for any 'relevant' changes.
					var localOrganizations = new MainBossNamedOrganizationStorage(new SavedOrganizationSessionLocalAllUsers.Connection());
					var lcuOrg = localOrganizations.Load(KnownIds.OrganizationMasterRecordId);
					bool credentialTypeChange = false;
					if (lcuOrg == null) { // doesn't exist
						lcuOrg = auOrg;
						localOrganizations.Save(lcuOrg); // user now has a copy in his local organizations
						credentialTypeChange = true; // user may have to provide credentials in PickOrganization
						retValue |= LocalAllUserRecordUpdateBehaviors.LocalRecordCreated;
					}
					else {
						// both exist, do the 'relevant' comparisons and construct a replacement if needed.
						// relevant applies only to the DBS, the DBN, the ON. Username/password Credentials will be LEFT ALONE 
						// Changes in ApplicationMode will be ignored as well. Is this correct is debatable.
						if (lcuOrg.DisplayName != auOrg.DisplayName
							|| lcuOrg.ConnectionDefinition.DBServer != auOrg.ConnectionDefinition.DBServer
							|| lcuOrg.ConnectionDefinition.DBName != auOrg.ConnectionDefinition.DBName
							|| lcuOrg.ConnectionDefinition.DisplayName != auOrg.ConnectionDefinition.DisplayName
							) {
							var newOrg = new NamedOrganization(auOrg.Id, auOrg.DisplayName, new MBConnectionDefinition(
								lcuOrg.MBConnectionParameters.ApplicationMode,        // keep user existing
								lcuOrg.MBConnectionParameters.CompactBrowsers,    // keep user existing 
								auOrg.ConnectionDefinition.DBServer,
								auOrg.ConnectionDefinition.DBName,
								lcuOrg.ConnectionDefinition.DBCredentials));
							localOrganizations.Replace(lcuOrg, newOrg);
							lcuOrg = newOrg;
							retValue |= LocalAllUserRecordUpdateBehaviors.LocalRecordUpdated;
						}
					}
					if (preferredOrganizationID.HasValue && localOrganizations.PreferredOrganizationId != preferredOrganizationID) {
						localOrganizations.PreferredOrganizationId = preferredOrganizationID;
						retValue |= LocalAllUserRecordUpdateBehaviors.PreferredOrganizationDemanded;
					}
					// Finally, determine if we need the user to see the PickOrganizations browser to set username/password credentials. This is governed by 
					// the type of DBCredentials currently in the lcuOrg if it requires a username/password. WindowsAuthentication doesn't.
					if (lcuOrg.ConnectionDefinition.DBCredentials.Type != AuthenticationMethod.WindowsAuthentication && credentialTypeChange)
						retValue |= LocalAllUserRecordUpdateBehaviors.AuthenticationCredentialsRequired; // user will have to set his username/password credentials
				}
				return retValue;
			}
			#endregion
			#region static methods to handle the interaction between OrganizationName and the other four options for use with actual DB access.
			/// <summary>
			/// Handle the options used to refer to an existing organization. The organization may be one that is saved under the current user's
			/// registry, in which case the saved organization provides defaults for all the other options. All the options should be created as
			/// non-required, although they can be given default values which will apply if they are not explicitly speficied and also there is no
			/// reference to a saved organization. The values in any saved organization are assumed to be valid. For each of the other options, the
			/// priority is: (1) the explicit value specified in the options (2) the value from the organization if any (3) the default value from
			/// the option.
			/// </summary>
			/// <param name="organizationNameOption"></param>
			/// <param name="serverNameOption"></param>
			/// <param name="databaseNameOption"></param>
			///
			/// <param name="applicationOption"></param>
			/// This can be null if the user did not name one and we also did not load any default organization.</param>
			/// <returns>A NamedOrganization or if the mode was PickOrganization, null is returned</returns>
			public static NamedOrganization ResolveSavedOrganization(
				StringValueOption organizationNameOption,
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				KeywordValueOption applicationOption,
				BooleanOption compactBrowsersOption) {

				NamedOrganization organization = null;

				string orgName = null;
				DatabaseEnums.ApplicationModeID appId;
				bool compact;

				if (applicationOption.ExplicitlySet && DecodeApplicationOptionValue(applicationOption.Value) == DatabaseEnums.ApplicationModeID.PickOrganization) {
					// If the user said "pick organization" do not allow any other options.
					// Perhaps in the future we could still allow an organization name, which would just set the list selection for the Pick form...
					// TODO: Use the static constant strings to build Mode option in the the following messages???
					if (organizationNameOption.ExplicitlySet)
						throw new GeneralException(KB.K("Cannot use '/{0}' with '/Mode:PickOrganization'"), OrganizationNameCommandArgument);
					if (serverNameOption.ExplicitlySet)
						throw new GeneralException(KB.K("Cannot use '/{0}' with '/Mode:PickOrganization'"), DataBaseServerCommandArgument);
					if (databaseNameOption.ExplicitlySet)
						throw new GeneralException(KB.K("Cannot use '/{0}' with '/Mode:PickOrganization'"), DataBaseNameCommandArgument);
					return null;
				}
				ConnectionDefinition connDef; // use common SavedOrganization resolution determination for the common options
				try {
					connDef = MB3Client.OptionSupport.ResolveSavedOrganization(organizationNameOption, serverNameOption, databaseNameOption, out orgName, ref organization);
				}
				catch (GeneralException e) {
					throw new NoOrganizationException(e);
				}
				// Get the app mode
				if (applicationOption.ExplicitlySet)
					appId = MB3Client.OptionSupport.DecodeApplicationOptionValue(applicationOption.Value);
				else if (organization != null)
					appId = organization.ConnectionDefinition.ApplicationMode;
				else if (applicationOption.HasValue)
					appId = MB3Client.OptionSupport.DecodeApplicationOptionValue(applicationOption.Value);
				else
					throw new NoOrganizationException(null);

				// Get the CompactBrowsers option
				if (compactBrowsersOption.ExplicitlySet)
					compact = compactBrowsersOption.Value;
				else if (organization != null)
					compact = organization.ConnectionDefinition.CompactBrowsers;
				else if (compactBrowsersOption.HasValue)
					compact = compactBrowsersOption.Value;
				else
					throw new NoOrganizationException(null);

				// It is possible here, despite the earlier check, for the appId to be PickOrganization (either because that is the default for the
				// option or because the saved Organization somehow specified that. Just to protect from this, we check here.
				if (appId == DatabaseEnums.ApplicationModeID.PickOrganization)
					return null;
				// Finally, return a usable NamedOrganization
				MBConnectionDefinition def = new MBConnectionDefinition(appId, compact, connDef.DBServer, connDef.DBName, connDef.DBCredentials);
				if (organization != null)
					return new NamedOrganization(organization.Id, orgName, def); // use the existing one
				else
					return new NamedOrganization(orgName, def); // make a new one
			}
			/// <summary>
			/// Handle the options used to refer to an existing organization but with no support for any Application Mode settings.
			/// Presumeably the calling App can only run in one mode or does not need a mode.
			/// The organization may be one that is saved under the current user's
			/// registry, in which case the saved organization provides defaults for all the other options. All the options should be created as
			/// non-required, although they can be given default values which will apply if they are not explicitly speficied and also there is no
			/// reference to a saved organization. The values in any saved organization are assumed to be valid. For each of the other options, the
			/// priority is: (1) the explicit value specified in the options (2) the value from the organization if any (3) the default value from
			/// the option.
			/// </summary>
			/// <param name="organizationNameOption"></param>
			/// <param name="serverNameOption"></param>
			/// <param name="databaseNameOption"></param>
			///
			/// <param name="organizationName">the name of the organization, as it was saved.
			/// This can be null if the user did not name one and we also did not load any default organization.</param>
			/// <returns></returns>

			public static ConnectionDefinition ResolveSavedOrganization(
				StringValueOption organizationNameOption,
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				out string organizationName) {
				NamedOrganization existingOrganization = null;
				ConnectionDefinition connDef = ResolveSavedOrganization(organizationNameOption, serverNameOption, databaseNameOption, out organizationName, ref existingOrganization);
				return connDef;
			}

			// internal version needs to keep the existing NamedOrganization if it was found
			internal static ConnectionDefinition ResolveSavedOrganization(
				StringValueOption organizationNameOption,
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				out string organizationName,
				ref NamedOrganization existingOrganization) {

				NamedOrganization organization = null;
				MainBossNamedOrganizationStorage organizations = new MainBossNamedOrganizationStorage(new SavedOrganizationSession.Connection());

				string orgName = null;
				string server;
				string dbname;

				// Command line arguments override any preferred Organization settings.
				if (organizationNameOption.HasValue) {
					var organizationIds = organizations.GetOrganizationNames(orgName = organizationNameOption.Value);
#if DEBUG          // I don't see how this could ever happen, (It can now that old display names differing only in case can exist
					if (organizationIds.Count >= 2)
						throw new GeneralException(KB.K("There are multiple organizations with the name '{0}'"), orgName);
#endif
					if (organizationIds.Count >= 1)
						organization = organizationIds[0];
					// If we didn't find one the user wanted or an error quietly occurred loading it, complain
					if (organization == null)
						throw new GeneralException(KB.K("Cannot find organization '{0}'"), orgName);
				}
				else {
					// If no organization name was specified, use the preferred one if any. If there is none we will charge on with a null OrganizationName anyway.
					organization = organizations.Load(organizations.PreferredOrganizationId);
					if (organization != null)
						orgName = organization.DisplayName;
				}

				// Get the DB server
				if (serverNameOption.ExplicitlySet) {
					server = serverNameOption.Value;
					// If the server is explicitly changed, only use the org name if it was explicitly named.
					orgName = organizationNameOption.Value;
				}
				else if (organization != null)
					server = organization.ConnectionDefinition.DBServer;
				else if (serverNameOption.HasValue)
					server = serverNameOption.Value;
				else
					throw new GeneralException(KB.K("No database server was specified"));

				// Get the DB name
				if (databaseNameOption.ExplicitlySet) {
					dbname = databaseNameOption.Value;
					// If the database name is explicitly changed, only use the org name if it was explicitly named.
					orgName = organizationNameOption.Value;
				}
				else if (organization != null)
					dbname = organization.ConnectionDefinition.DBName;
				else if (databaseNameOption.HasValue)
					dbname = databaseNameOption.Value;
				else
					throw new GeneralException(KB.K("No database name was specified"));

				// Finally, summarize the results; need original NamedOrganization in case so we save to same record.
				existingOrganization = organization;
				organizationName = orgName;
				return new ConnectionDefinition(server, dbname, organization?.ConnectionDefinition.DBCredentials);
			}
			/// <summary>
			/// Handle the options used to refer to a possibly nonexistent organization with no support for handling the Organization name.
			/// </summary>
			/// <param name="serverNameOption"></param>
			/// <param name="databaseNameOption"></param>
			///
			/// <param name="applicationOption"></param>
			/// <returns></returns>
			public static MBConnectionDefinition ResolveUnnamedAdHocOrganization(
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				KeywordValueOption applicationOption,
				BooleanOption compactBrowsersOption,
				AuthenticationCredentials credentials) {

				// The Option objects should have been created so as to force them to have values by this point, i.e. they should be required
				// or they should have defaults set. If the string options are unset there will be nulls in the MBConnectionDefinition. For the
				// other options we would get a null deref trying to unbox the value in option.Value.
				return new MBConnectionDefinition(DecodeApplicationOptionValue(applicationOption.Value), compactBrowsersOption.HasValue ? compactBrowsersOption.Value : false, serverNameOption.Value, databaseNameOption.Value, credentials);
			}
			/// <summary>
			/// Handle the options used to refer to a possibly nonexistent organization with no support for handling the Organization name or
			/// for any Application Mode settings. Presumeably the calling App can only run in one mode or does not need a mode.
			/// </summary>
			/// <param name="serverNameOption"></param>
			/// <param name="databaseNameOption"></param>
			///
			/// <returns></returns>
			public static ConnectionDefinition ResolveUnnamedAdHocOrganization(
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				AuthenticationCredentials credentials) {

				// The Option objects should have been created so as to force them to have values by this point, i.e. they should be required
				// or they should have defaults set. If the string options are unset there will be nulls in the MBConnectionDefinition. For the
				// other options we would get a null deref trying to unbox the value in option.Value.
				return new ConnectionDefinition(serverNameOption.Value, databaseNameOption.Value, credentials);
			}
			/// <summary>
			/// Handle the options used to refer to a possibly nonexistent organization.
			/// </summary>
			/// <param name="organizationNameOption"></param>
			/// <param name="serverNameOption"></param>
			/// <param name="databaseNameOption"></param>
			///
			/// <param name="applicationOption"></param>
			/// <param name="organizationName"></param>
			/// <returns></returns>
			public static MBConnectionDefinition ResolveNamedAdHocOrganization(
				StringValueOption organizationNameOption,
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				KeywordValueOption applicationOption,
				BooleanOption compactBrowsersOption,
				AuthenticationCredentials credentials,
				out string organizationName) {

				organizationName = organizationNameOption.Value;
				return ResolveUnnamedAdHocOrganization(serverNameOption, databaseNameOption, applicationOption, compactBrowsersOption, credentials);
			}
			/// <summary>
			/// Handle the options used to refer to a possibly nonexistent organization with no support for handling
			/// any Application Mode settings. Presumeably the calling App can only run in one mode or does not need a mode.
			/// </summary>
			/// <param name="organizationNameOption"></param>
			/// <param name="serverNameOption"></param>
			/// <param name="databaseNameOption"></param>
			/// <param name="organizationName"></param>
			/// <returns></returns>
			public static ConnectionDefinition ResolveNamedAdHocOrganization(
				StringValueOption organizationNameOption,
				StringValueOption serverNameOption,
				StringValueOption databaseNameOption,
				AuthenticationCredentials credentials,
				out string organizationName) {

				organizationName = organizationNameOption.Value;
				return ResolveUnnamedAdHocOrganization(serverNameOption, databaseNameOption, credentials);
			}
			#endregion
			#region static methods to format an option and its value appropriately to appear on a command line.
			// TODO: This is something the individual option values should be able to do for us. They know best how to quote stuff properly.
			// TODO: Perhaps these should take a StringBuilder as an argument and append to it (and maybe return it) (and maybe create one if null)
			// instead of returning a string.
			public static string FormatOrganizationNameOption(string value) {
				return Strings.IFormat("/{0}=\"{1}\"", OrganizationNameCommandArgument, value);
			}
			public static string FormatServerNameOption(string value) {
				return Strings.IFormat("/{0}=\"{1}\"", DataBaseServerCommandArgument, value);
			}
			public static string FormatDatabaseNameOption(string value) {
				return Strings.IFormat("/{0}=\"{1}\"", DataBaseNameCommandArgument, value);
			}
			public static string FormatApplicationOption(DatabaseEnums.ApplicationModeID value) {
				return Strings.IFormat("/{0}=\"{1}\"", ApplicationCommandArgument, DatabaseEnums.ApplicationModeName(value).Translate());
			}
			#endregion
		}
		#endregion
		#region Connection Definitions
		public class ConnectionDefinition : DBClient.Connection {
			private const string DataBaseNameRegistryValueName = "DataBaseName";
			private const string DataBaseServerRegistryValueName = "DataBaseServer";

			public ConnectionDefinition(string serverName, string dbName, AuthenticationCredentials credentials)
				: base(new SqlClient.Connection(serverName, dbName, delegate () {
					if (SqlClient.IsSqlLocalDBServer(serverName)) { // a local database
																	// we want the database files to exist under the User Personal folder in a directory called MainBoss
						return System.IO.Path.ChangeExtension(
							System.IO.Path.Combine(
							Environment.GetFolderPath(Environment.SpecialFolder.Personal),
							KB.I("MainBoss"),
							dbName), KB.I(".mdf"));
					}
					return null;
				}, credentials), dsMB.Schema) {
			}

			// TODO Use some generic code that relies on attributes on the members to be loaded/saved and mark these members in our class definition.
			public new SqlClient.Connection ConnectionInformation {
				get {
					return (SqlClient.Connection)base.ConnectionInformation;
				}
			}
			public string DBName {
				get {
					return ConnectionInformation.DBName;
				}
			}
			public string DBServer {
				get {
					return ConnectionInformation.DBServer;
				}
			}
			public AuthenticationCredentials DBCredentials {
				get {
					return ConnectionInformation.Credentials;
				}
			}
		}
		public interface IMBConnectionParameters {
			DatabaseEnums.ApplicationModeID ApplicationMode { get; }
			bool CompactBrowsers { get; }
			DBClient.Connection Connection { get; }
		}
		public class MBConnectionDefinition : ConnectionDefinition, IMBConnectionParameters {
			private const string PreferredApplicationModeValueName = "PreferredApplicationMode";
			private const string CompactBrowsersValueName = "CompactBrowsers";
			public MBConnectionDefinition(DatabaseEnums.ApplicationModeID applicationMode, bool compactBrowsers, string serverName, string dbName, AuthenticationCredentials credentials)
				: base(serverName, dbName, credentials) {
				ApplicationMode = applicationMode;
				CompactBrowsers = compactBrowsers;
			}
			public DatabaseEnums.ApplicationModeID ApplicationMode { get; private set; }
			public bool CompactBrowsers { get; private set; }
			public DBClient.Connection Connection { get { return this;} }

			public MBConnectionDefinition(dsSavedOrganizations.OrganizationsRow organization)
				: base(organization.F.DataBaseServer, organization.F.DataBaseName, FromOrganizationRow(organization)) {
				ApplicationMode = (DatabaseEnums.ApplicationModeID)organization.F.PreferredApplicationMode;
				CompactBrowsers = organization.F.CompactBrowsers ?? false;
			}
			private static AuthenticationCredentials FromOrganizationRow(dsSavedOrganizations.OrganizationsRow organization) {
				return new AuthenticationCredentials(
					(AuthenticationMethod)organization.F.CredentialsAuthenticationMethod,
					organization.F.CredentialsUsername,
					OptionSupport.DecryptCredentialsPassword(organization.F.CredentialsPassword)
					);
			}
		}
		#endregion
		#region Constructors
		/// <summary>
		/// Create a new database access object
		/// </summary>
		/// <param name="connection">the information required to make the connection</param>
		public MB3Client(Connection connection)
			: base(connection) {
		}
		public MB3Client(Connection connection, ISession existingSession)
			: base(connection, existingSession) {
		}
		#endregion
		#region Properties
		public new ConnectionDefinition ConnectionInfo {
			get {
				return (ConnectionDefinition)base.ConnectionInfo;
			}
		}
		#endregion
		#region Overrides

		public class ContainmentViolation : ColumnRelatedDBException {
			public ContainmentViolation(MB3Client client, System.Exception inner, dsMB.RelativeLocationRow rlrow)
				: base(InterpretedDbExceptionCodes.ViolationContainment, dsMB.Schema.T.RelativeLocation.Name, new string[] { dsMB.Schema.T.RelativeLocation.F.ContainingLocationID.Name }, inner, KB.K("'{0}' cannot contain itself either directly or indirectly"), rlrow.F.Code) {
			}
		}
		protected override System.Exception InterpretException(System.Exception e) {
			System.Exception result = base.InterpretException(e);
			var vuc = result as ColumnRelatedDBException;
			if (vuc != null
				&& vuc.InterpretedErrorCode == InterpretedDbExceptionCodes.ViolationUniqueConstraint
				&& dsMB.Schema.Tables[vuc.TableInError] == dsMB.Schema.T.LocationContainment)
				// Special case, this means a containment loop in Locations was created.
				result = new ContainmentViolation(this, e, vuc.RowInError as dsMB.RelativeLocationRow);
			return result;
		}
		#endregion
		#region History table classes
		#region - StateHistoryTransition
		public class StateHistoryTransition {
			public readonly Key Operation;
			public readonly Key OperationHint;
			public readonly Guid FromStateID;
			public readonly Guid ToStateID;
			public readonly int Rank;
			public readonly string RightName;
			public readonly bool CanTransitionWithoutUI;
			public readonly bool CopyStatusFromPrevious;

			public StateHistoryTransition(Key operation, Key operationHint, Guid fromStateID, Guid toStateID, int rank, string rightName, bool canTransitionWithoutUI, bool copyStatusFromPrevious) {
				Operation = operation;
				OperationHint = operationHint;
				FromStateID = fromStateID;
				ToStateID = toStateID;
				Rank = rank;
				RightName = rightName;
				CanTransitionWithoutUI = canTransitionWithoutUI;
				CopyStatusFromPrevious = copyStatusFromPrevious;
			}
		}
		#endregion
		#region - StateFlagRestriction
		/// <summary>
		/// This class defines restrictions on certain states. Each state contains boolean flags, and this class defines a condition that must be true
		/// before a transition to a state with the name flag set (or cleared) is allowed.
		/// </summary>
		public class StateFlagRestriction {
			public StateFlagRestriction(DBI_Column stateFlagColumn, bool conditionAppliesWhenFlagIs, SqlExpression condition, Key violationMessage) {
				StateFlagColumn = stateFlagColumn;
				ConditionAppliesWhenFlagIs = conditionAppliesWhenFlagIs;
				Condition = condition;
				ViolationMessage = violationMessage;
			}
			public readonly DBI_Column StateFlagColumn;
			public readonly bool ConditionAppliesWhenFlagIs;
			public readonly SqlExpression Condition;
			public readonly Key ViolationMessage;
		}
		#endregion
		#region - StateHistoryTable
		/// <summary>
		/// The StateHistoryTable defines the interrelation between the main table and its corresponding State History, State, and State Transition tables.
		/// </summary>
		public class StateHistoryTable {
			#region Constructor
			/// <summary>
			/// Define a group of related State history tables
			/// </summary>
			/// <param name="mainToCurrentHistPath">The path from the main table to the current history row</param>
			/// <param name="histEffectiveDatePath">The path from a History row to the effective date column</param>
			/// <param name="histEffectiveDateReadonlyPath">The path from a History row to a boolean column indicating if the effective date is editable,
			/// or null if the date should always be editable.</param>
			/// <param name="histToStatePath">The path from the state history table to the State row</param>
			/// <param name="histToMainPath">The path from the state history table to the parent main row</param>
			/// <param name="transitionOperationColumn">the column in the transition table defining the operation name</param>
			/// <param name="transitionOperationHintColumn">the column in the transition table defining the operation hint/description</param>
			/// <param name="transitionFromStateColumn">the column in the transition table defining the operation starting state ID</param>
			/// <param name="transitionToStateColumn">the column in the transition table defining the operation target state ID</param>
			/// <param name="transitionRankColumn">the column in the transition table defining the operation rank</param>
			public StateHistoryTable(DBI_Path mainToCurrentHistPath,
				DBI_Path histEffectiveDatePath,
				DBI_Path histEffectiveDateReadonlyPath,
				DBI_Path histUserIDPath,
				DBI_Path histToStatePath,
				DBI_Path histToStatusPath,
				DBI_Path histToMainPath,
				DBI_Column transitionOperationColumn, DBI_Column transitionOperationHintColumn,
				DBI_Column transitionWithoutUIColumn, DBI_Column transitionCopyStatusFromPreviousColumn,
				DBI_Column transitionFromStateColumn, DBI_Column transitionToStateColumn,
				DBI_Column transitionRankColumn, DBI_Column transitionRightColumn, params StateFlagRestriction[] stateRestrictions) {

				HistEffectiveDatePath = histEffectiveDatePath;
				HistEffectiveDateReadonlyPath = histEffectiveDateReadonlyPath;
				HistUserIDPath = histUserIDPath;
				HistToStatePath = histToStatePath;
				HistToStatusPath = histToStatusPath;
				MainToCurrentStateHistoryPath = mainToCurrentHistPath;
				MainToCurrentStatePath = new DBI_Path(mainToCurrentHistPath.PathToReferencedRow, histToStatePath);
				HistToMainPath = histToMainPath;

				TransitionOperationColumn = transitionOperationColumn;
				TransitionWithoutUIColumn = transitionWithoutUIColumn;
				TransitionCopyStatusFromPreviousColumn = transitionCopyStatusFromPreviousColumn;
				TransitionOperationHintColumn = transitionOperationHintColumn;
				TransitionFromStateColumn = transitionFromStateColumn;
				TransitionToStateColumn = transitionToStateColumn;
				TransitionRankColumn = transitionRankColumn;
				TransitionRightColumn = transitionRightColumn;

				StateRestrictions = stateRestrictions;

#if DEBUG
				List<DBI_Column> restrictionColumns = new List<DBI_Column>();
				foreach (StateFlagRestriction sfr in StateRestrictions) {
					System.Diagnostics.Debug.Assert(!restrictionColumns.Contains(sfr.StateFlagColumn) && sfr.StateFlagColumn.Table == StateTable,
						"StateFlagRestrictions improperly specified in StateHistoryTable ctor");
					restrictionColumns.Add(sfr.StateFlagColumn);
				}
				System.Diagnostics.Debug.Assert(
					HistEffectiveDatePath.Table == HistTable
					&& HistEffectiveDateReadonlyPath.Table == HistTable
					&& HistUserIDPath.Table == HistTable
					&& HistToStatePath.Table == HistTable
					&& HistToMainPath.Table == HistTable
					&& HistToMainPath.ReferencedColumn.ConstrainedBy.Table == MainTable
					&& TransitionFromStateColumn.ConstrainedBy.Table == StateTable
					&& TransitionToStateColumn.ConstrainedBy.Table == StateTable
					&& HistToStatePath.ReferencedColumn.ConstrainedBy.Table == StateTable
					&& TransitionOperationColumn.Table == TransitionTable
					&& TransitionOperationHintColumn.Table == TransitionTable
					&& TransitionWithoutUIColumn.Table == TransitionTable
					&& TransitionCopyStatusFromPreviousColumn.Table == TransitionTable
					&& TransitionFromStateColumn.Table == TransitionTable
					&& TransitionToStateColumn.Table == TransitionTable
					&& TransitionRankColumn.Table == TransitionTable
					&& (TransitionRightColumn == null || TransitionRightColumn.Table == TransitionTable),
					"Table mismatch in StateHistoryTable ctor");
#endif
			}
			#endregion
			#region Properties
			/// <summary>
			/// The main table
			/// </summary>
			public DBI_Table MainTable { get { return MainToCurrentStatePath.Table; } }
			/// <summary>
			/// The State History table
			/// </summary>
			public DBI_Table HistTable { get { return HistToMainPath.Table; } }
			/// <summary>
			/// The State Transition table
			/// </summary>
			public DBI_Table TransitionTable { get { return TransitionOperationColumn.Table; } }
			/// <summary>
			/// The State table
			/// </summary>
			public DBI_Table StateTable { get { return HistToStatePath.ReferencedColumn.ConstrainedBy.Table; } }
			/// <summary>
			/// The Path, if any, of a boolean value in the State History table that causes the effective date of that record to be readonly.
			/// If this path is null the effective date is always editable.
			/// </summary>
			public readonly DBI_Path HistEffectiveDateReadonlyPath;
			/// <summary>
			/// The column containing the EffectiveDate of the history record for finding date restrictions on new records (if any)
			/// </summary>
			public readonly DBI_Path HistEffectiveDatePath;
			/// <summary>
			/// The column containing the User ID of the history record (if any)
			/// </summary>
			public readonly DBI_Path HistUserIDPath;
			/// <summary>
			/// The linkage path from the State History record to the parent main table.
			/// </summary>
			public readonly DBI_Path HistToMainPath;
			/// <summary>
			/// The linkage path from a State History record to its State
			/// </summary>
			public readonly DBI_Path HistToStatePath;
			/// <summary>
			/// The linkage path from a State History record to its State
			/// </summary>
			public readonly DBI_Path HistToStatusPath;

			/// <summary>
			/// The linkage path from the main record to its current State History record
			/// </summary>
			public readonly DBI_Path MainToCurrentStateHistoryPath;
			/// <summary>
			/// The linkage path from the main record to the State of its current State History record
			/// </summary>
			public readonly DBI_Path MainToCurrentStatePath;
			/// <summary>
			/// The column in the State Transition table giving the name of the transition operation
			/// </summary>
			public readonly DBI_Column TransitionOperationColumn;
			/// <summary>
			/// The column in the State Transition table giving the hint (long description) of the transition operation
			/// </summary>
			public readonly DBI_Column TransitionOperationHintColumn;
			/// <summary>
			/// The column in the State Transition table giving the flag indicating whether a UI is needed for this operation.
			/// </summary>
			public readonly DBI_Column TransitionWithoutUIColumn;
			/// <summary>
			/// The column in the State Transition table giving the flag indicating whether the new record Status should be initialized from the current History status
			/// </summary>
			public readonly DBI_Column TransitionCopyStatusFromPreviousColumn;
			/// <summary>
			/// The column in the State Transition table giving the starting state of the transition operation
			/// </summary>
			public readonly DBI_Column TransitionFromStateColumn;
			/// <summary>
			/// The column in the State Transition table giving the final state of the transition operation
			/// </summary>
			public readonly DBI_Column TransitionToStateColumn;
			/// <summary>
			/// The column in the State Transition table giving the ranking of the transition operation for ordering the commands on the screen.
			/// </summary>
			public readonly DBI_Column TransitionRankColumn;
			/// <summary>
			/// The column in the State Transition table giving the name of the right required to perform the transition operation
			/// </summary>
			public readonly DBI_Column TransitionRightColumn;

			/// <summary>
			/// The list of conditions limited by state flags;
			/// </summary>
			public readonly StateFlagRestriction[] StateRestrictions;
			#endregion
			#region StateHistoryTransitionFromRow
			public StateHistoryTransition StateHistoryTransitionFromRow(System.Data.DataRow row) {
				return new StateHistoryTransition(
					(Key)TransitionOperationColumn[row],
					(Key)TransitionOperationHintColumn[row],
					(Guid)TransitionFromStateColumn[row],
					(Guid)TransitionToStateColumn[row],
					(int)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(TransitionRankColumn[row], typeof(int)),
					TransitionRightColumn == null ? null : (string)TransitionRightColumn[row],
					(bool)TransitionWithoutUIColumn[row],
					(bool)TransitionCopyStatusFromPreviousColumn[row]);
			}
			#endregion
			#region StateHistoryTransitionToRow
			public void StateHistoryTransitionToRow(StateHistoryTransition transition, System.Data.DataRow row) {
				TransitionOperationColumn[row] = transition.Operation;
				TransitionOperationHintColumn[row] = transition.OperationHint;
				TransitionFromStateColumn[row] = transition.FromStateID;
				TransitionToStateColumn[row] = transition.ToStateID;
				TransitionRankColumn[row] = transition.Rank;
				if (TransitionRightColumn != null)
					TransitionRightColumn[row] = transition.RightName;
				TransitionWithoutUIColumn[row] = transition.CanTransitionWithoutUI;
				TransitionCopyStatusFromPreviousColumn[row] = transition.CopyStatusFromPrevious;
			}
			#endregion
		}
		#endregion
		#region - History table initialisation
		public static DelayedConstruction<StateHistoryTable> RequestHistoryTable = new DelayedConstruction<StateHistoryTable>(delegate () {
			return new StateHistoryTable(
				dsMB.Path.T.Request.F.CurrentRequestStateHistoryID,
				dsMB.Path.T.RequestStateHistory.F.EffectiveDate,
				dsMB.Path.T.RequestStateHistory.F.EffectiveDateReadonly,
				dsMB.Path.T.RequestStateHistory.F.UserID,
				dsMB.Path.T.RequestStateHistory.F.RequestStateID,
				dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID,
				dsMB.Path.T.RequestStateHistory.F.RequestID,
				dsMB.Schema.T.RequestStateTransition.F.Operation,
				dsMB.Schema.T.RequestStateTransition.F.OperationHint,
				dsMB.Schema.T.RequestStateTransition.F.CanTransitionWithoutUI,
				dsMB.Schema.T.RequestStateTransition.F.CopyStatusFromPrevious,
				dsMB.Schema.T.RequestStateTransition.F.FromStateID,
				dsMB.Schema.T.RequestStateTransition.F.ToStateID,
				dsMB.Schema.T.RequestStateTransition.F.Rank,
				dsMB.Schema.T.RequestStateTransition.F.RightName
				);
		});

		public static DelayedConstruction<StateHistoryTable> WorkOrderHistoryTable = new DelayedConstruction<StateHistoryTable>(delegate () {
			return new StateHistoryTable(
				dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID,
				dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate,
				dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDateReadonly,
				dsMB.Path.T.WorkOrderStateHistory.F.UserID,
				dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID,
				dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID,
				dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID,
				dsMB.Schema.T.WorkOrderStateTransition.F.Operation,
				dsMB.Schema.T.WorkOrderStateTransition.F.OperationHint,
				dsMB.Schema.T.WorkOrderStateTransition.F.CanTransitionWithoutUI,
				dsMB.Schema.T.WorkOrderStateTransition.F.CopyStatusFromPrevious,
				dsMB.Schema.T.WorkOrderStateTransition.F.FromStateID,
				dsMB.Schema.T.WorkOrderStateTransition.F.ToStateID,
				dsMB.Schema.T.WorkOrderStateTransition.F.Rank,
				dsMB.Schema.T.WorkOrderStateTransition.F.RightName,
				new StateFlagRestriction(dsMB.Schema.T.WorkOrderState.F.TemporaryStorageActive, false,
					new SqlExpression(dsMB.Path.T.WorkOrder.F.TemporaryStorageEmpty), KB.K("All temporary storage must be empty")),
				new StateFlagRestriction(dsMB.Schema.T.WorkOrderState.F.FilterAsVoid, true,
					new SqlExpression(dsMB.Path.T.WorkOrder.F.TotalActual).Eq(SqlExpression.Constant((decimal)0)), KB.K("Actual costs must be zero"))
				);
		});

		public static DelayedConstruction<StateHistoryTable> PurchaseOrderHistoryTable = new DelayedConstruction<StateHistoryTable>(delegate () {
			return new StateHistoryTable(
				dsMB.Path.T.PurchaseOrder.F.CurrentPurchaseOrderStateHistoryID,
				dsMB.Path.T.PurchaseOrderStateHistory.F.EffectiveDate,
				dsMB.Path.T.PurchaseOrderStateHistory.F.EffectiveDateReadonly,
				dsMB.Path.T.PurchaseOrderStateHistory.F.UserID,
				dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID,
				dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID,
				dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.Operation,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.OperationHint,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.CanTransitionWithoutUI,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.CopyStatusFromPrevious,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.FromStateID,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.ToStateID,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.Rank,
				dsMB.Schema.T.PurchaseOrderStateTransition.F.RightName,
				new StateFlagRestriction(dsMB.Schema.T.PurchaseOrderState.F.CanHaveReceiving, false,
					new SqlExpression(dsMB.Path.T.PurchaseOrder.F.HasReceiving).Not(), KB.K("Some purchased items have already been received"))
				);
		});
		#endregion
		#endregion
		#region Database Methods
		#region Create Database
		/// <summary>
		/// Create a MainBoss database using dsMB.Schema
		/// </summary>
		/// <param name="connectionDefinition"></param>
		/// <param name="populator"></param>
		public static void CreateDatabase(IMBConnectionParameters parameters, string organizationName, PopulateDatabaseDelegate populator, IProgressDisplay ipd) {
			var connection = parameters.Connection;
			IServer server = connection.ConnectionInformation.CreateServer();
			ISession db = null;
			// Do the common Minimal database initialization with this client
			MB3Client mb3db = null;
			try {
				db = server.CreateDatabase(connection.ConnectionInformation, dsMB.Schema);
				try {
					// TODO: We really want to calculate the EffectiveServerVersion even before CreateDatabase starts to do anything.
					int serverUpgradeIndex = DBVersionInformation.GetServerVersionUpgradersIndex(db.ServerVersion);
					Version effectiveServerVersion = serverUpgradeIndex == 0 ? new Version() : DBVersionInformation.ServerVersionUpgraders[serverUpgradeIndex - 1].ServerVersion;
					db.EffectiveDBServerVersion = effectiveServerVersion;
					foreach (DatabaseCreation.ServerSideDefinition def in DatabaseCreation.ServerSideDefinitions)
						((SqlClient)db).SetServerSideColumnDefinition(def.Column, def.DefinitionText);
					string sql = DatabaseCreation.GetPostTablesCreationScript();
					if (sql != null)
						db.ExecuteCommandBatches(SqlClient.BuildBatchSpecificationListFromSqlScript(sql));
					mb3db = new MB3Client(connection, db);
					DatabaseCreation.SetMinimalData(mb3db, organizationName);
					populator(mb3db);
				}
				catch (System.Exception e) {
					mb3db?.CloseDatabase();
					db.CloseDatabase();
					Libraries.DBILibrary.Server.DeleteDatabaseAfterFailedCompletion(server, connection.ConnectionInformation, e);
					throw;
				}
			}
			finally {
				mb3db?.CloseDatabase();
				db?.CloseDatabase();
			}
		}
		#endregion
		#region Delete Database
		/// <summary>
		/// Delete a database
		/// </summary>
		/// <param name="connectionDefinition"></param>
		public static void DeleteDatabase(ConnectionDefinition connectionDefinition) {
			connectionDefinition.ConnectionInformation.CreateServer().DeleteDatabase(connectionDefinition.ConnectionInformation);
		}

		#endregion
		#region Backup Database
		public void BackupDatabase([Thinkage.Libraries.Translation.Invariant] string filename) {
			DBVersionHandler vh = MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(this);
			Version dbVersion = vh.CurrentVersion;
			try {
				if (dbVersion >= new Version(1, 0, 10, 24))
					using (dsBackupFileName_1_0_10_24 dsLog = new dsBackupFileName_1_0_10_24(this)) {
						// Log start of backup
						System.Data.DataRow row = EditSingleRow(dsLog, dsBackupFileName_1_0_10_24.Schema.T.BackupFileName, new SqlExpression(dsBackupFileName_1_0_10_24.Path.T.BackupFileName.F.FileName).Eq(filename));
						if (row != null) {
							dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.LastBackupDate[row] = (DateTime)dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.LastBackupDate.EffectiveType.ClosestValueTo(DateTime.Now);
							// TODO: The idea behond the following is that if MB crashes during the backup, the BackupFileName record would have a message stating that the backup is incomplete.
							// This would get cleared on successful backup completion.
							// The problem is that the backed-up version of the table contains the message, so if you restore from that backup, your backupfilename table will then show this backup as "incomplete"
							// The is related to the problem that, after a restore, the BackupFileName table is incorrect for backups done since the backup of the restored file. i.e. you restore from an older backup
							// and more recent backups to other files are "forgotten".
							// dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.Message[row] = KB.K("Backup incomplete").Translate();
							dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.Message[row] = null;   // in any case we have to clear it out of any previous message.
							row.EndEdit();
							Update(dsLog);
						}
					}
				else if (dbVersion >= new Version(1, 0, 4, 79))
					using (dsBackupFileName_1_0_4_79_To_1_0_10_23 dsLog = new dsBackupFileName_1_0_4_79_To_1_0_10_23(this)) {
						// Log start of backup
						System.Data.DataRow row = EditSingleRow(dsLog, dsBackupFileName_1_0_4_79_To_1_0_10_23.Schema.T.BackupFileName, new SqlExpression(dsBackupFileName_1_0_4_79_To_1_0_10_23.Path.T.BackupFileName.F.FileName).Eq(filename));
						if (row != null) {
							dsBackupFileName_1_0_4_79_To_1_0_10_23.Schema.T.BackupFileName.F.LastBackupDate[row] = (DateTime)dsBackupFileName_1_0_4_79_To_1_0_10_23.Schema.T.BackupFileName.F.LastBackupDate.EffectiveType.ClosestValueTo(DateTime.Now);
							row.EndEdit();
							Update(dsLog);
						}
					}
				System.Text.StringBuilder backupOutput = new System.Text.StringBuilder();
				Server.BackupDatabase(ConnectionInfo.ConnectionInformation, filename, Strings.IFormat("MainBoss Backup - {0}", ConnectionInfo.DBName), backupOutput);
				vh.LogHistory(this, Strings.Format(KB.K("Backup to '{0}'"), filename), backupOutput.ToString());
				if (dbVersion >= new Version(1, 0, 10, 24))
					using (dsBackupFileName_1_0_10_24 dsLog = new dsBackupFileName_1_0_10_24(this)) {
						// Log successful end of backup
						System.Data.DataRow row = EditSingleRow(dsLog, dsBackupFileName_1_0_10_24.Schema.T.BackupFileName, new SqlExpression(dsBackupFileName_1_0_10_24.Path.T.BackupFileName.F.FileName).Eq(filename));
						if (row != null) {
							dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.LastBackupDate[row] = (DateTime)dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.LastBackupDate.EffectiveType.ClosestValueTo(DateTime.Now);
							dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.DatabaseVersion[row] = dbVersion.ToString();
							// TODO: Clear out Message if it was set above
							row.EndEdit();
							Update(dsLog);
						}
					}
			}
			catch (System.Exception e) {
				string message = Thinkage.Libraries.Exception.FullMessage(e);
				vh.LogHistory(this, Strings.Format(KB.K("Backup failed to '{0}'"), filename), message);
				if (dbVersion >= new Version(1, 0, 10, 24))
					using (dsBackupFileName_1_0_10_24 dsLog = new dsBackupFileName_1_0_10_24(this)) {
						// Log failure of backup.
						System.Data.DataRow row = EditSingleRow(dsLog, dsBackupFileName_1_0_10_24.Schema.T.BackupFileName, new SqlExpression(dsBackupFileName_1_0_10_24.Path.T.BackupFileName.F.FileName).Eq(filename));
						if (row != null) {
							dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.LastBackupDate[row] = (DateTime)dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.LastBackupDate.EffectiveType.ClosestValueTo(DateTime.Now);
							dsBackupFileName_1_0_10_24.Schema.T.BackupFileName.F.Message[row] = message;
							row.EndEdit();
							Update(dsLog);
						}
					}
				throw;
			}
		}
		#endregion
		#region Restore Database
		/// <summary>
		/// Restore a database over an existing database
		/// </summary>
		/// <param name="connectionDefinition"></param>
		/// <param name="restoreFileName"></param>
		/// <param name="ipd"></param>
		public static GeneralException RestoreDatabase(ConnectionDefinition connectionDefinition, [Thinkage.Libraries.Translation.Invariant]string restoreFileName, int? backupNumber, IProgressDisplay ipd) {
			IDisposable unfetteredDatabaseAccess = null;
			{
				// Make sure Database Exists
				XAFClient oldDBSession = null;
				try {
					oldDBSession = new XAFClient(connectionDefinition);
					DBVersionHandler versionHandler = null;
					try {
						versionHandler = MBUpgrader.UpgradeInformation.CheckDBVersion(oldDBSession, VersionInfo.ProductVersion, new System.Version(1, 0, 1, 0), null, KB.K("Restore MainBoss Database").Translate());
					}
					catch (GeneralException) {
						throw new GeneralException(KB.K("'{0}' is not a MainBoss database"), connectionDefinition.DBName);
					}
					unfetteredDatabaseAccess = versionHandler.GetUnfetteredDatabaseAccess(oldDBSession);
				}
				catch (GeneralException) {
					throw;
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Cannot access MainBoss database '{0}' on database server {1}"), connectionDefinition.DBName, connectionDefinition.DBServer);
				}
				finally {
					if (oldDBSession != null)
						oldDBSession.CloseDatabase();
				}
			}
			ipd.Update(KB.T(Strings.Format(KB.K("Restoring database {0} on database server {1}"), connectionDefinition.DBName, connectionDefinition.DBServer)));
			IServer server = connectionDefinition.ConnectionInformation.CreateServer();
			SqlClient db = null;
			GeneralException completionInformation;
			try {
				db = (SqlClient)server.RestoreOverExistingDatabase(connectionDefinition.ConnectionInformation, restoreFileName, backupNumber, dsMB.Schema);
				ipd.Update(KB.K("Checking the restored database"));
				completionInformation = PostprocessRestoredDatabase(db, connectionDefinition, restoreFileName);
			}
			catch (System.Exception mainException) {
				throw new GeneralException(mainException, KB.K("Unable to restore database"));
			}
			finally {
				if (db != null)
					db.CloseDatabase();
				if (unfetteredDatabaseAccess != null)
					unfetteredDatabaseAccess.Dispose();
			}
			return completionInformation;
		}
		#region Create Database from Backup file
		/// <summary>
		/// Create a new Database
		/// </summary>
		/// <param name="connectionDefinition"></param>
		/// <param name="restoreFileName"></param>
		/// <param name="ipd"></param>
		public static void CreateDatabaseFromBackup(ConnectionDefinition connectionDefinition, [Thinkage.Libraries.Translation.Invariant]string restoreFileName, int? backupNumber, IProgressDisplay ipd) {
			ipd.Update(KB.T(Strings.Format(KB.K("Creating database {0} on database server {1}"), connectionDefinition.DBName, connectionDefinition.DBServer)));
			IServer server = connectionDefinition.ConnectionInformation.CreateServer();
			SqlClient db = (SqlClient)server.CreateDatabaseFromBackup(connectionDefinition.ConnectionInformation, restoreFileName, backupNumber, dsMB.Schema);
			try {
				try {
					ipd.Update(KB.K("Checking the restored database"));
					PostprocessRestoredDatabase(db, connectionDefinition, restoreFileName);
				}
				finally {
					db.CloseDatabase();
				}
			}
			catch (System.Exception mainException) {
				Libraries.DBILibrary.Server.DeleteDatabaseAfterFailedCompletion(server, connectionDefinition.ConnectionInformation, mainException);
				throw new GeneralException(mainException, KB.K("Unable to restore database"));
			}
		}
		#endregion

		#region - Verification and post-processing of restored database
		/// <summary>
		/// Always returns a GeneralException with information on the success/failure of the Restore
		/// </summary>
		/// <param name="db"></param>
		/// <param name="connection"></param>
		/// <param name="restoreFileName"></param>
		private static GeneralException PostprocessRestoredDatabase(ISession db, DBClient.Connection connection, [Thinkage.Libraries.Translation.Invariant]string restoreFileName) {

			// Do whatever checks necessary at this level
			// TODO: We want a version-safe schema in the connection object. Even dsUpgrade is somewhat version-dependent.
			// TODO: Actually, we want all the DBVersionHandler methods to take an ISession not DBClient, and all the code here should use ISession only as well.
			var client = new XAFClient(connection, db);
			DBVersionHandler versionHandler = MBUpgrader.UpgradeInformation.CheckDBVersion(client, VersionInfo.ProductVersion, new System.Version(1, 0, 1, 0), null, KB.K("Restore MainBoss Database").Translate());
			// TODO: A bit more poking to ensure it is indeed an MB database possibly of some older version

			Key message;
			string restoreSource = Strings.Format(KB.K("Restore source '{0}'"), restoreFileName);
			message = KB.K("Database restored from backup");
			versionHandler.LogHistory(client, message.Translate(), restoreSource);
			return (GeneralException)new GeneralException(message).
				WithContext(new Thinkage.Libraries.MessageExceptionContext(KB.T(restoreSource)));
		}
		#endregion

		#endregion

		#endregion
	}
}
