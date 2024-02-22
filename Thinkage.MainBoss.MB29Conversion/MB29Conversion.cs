using System;
using System.Data;
using System.Xml;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.Libraries.Xml;

namespace Thinkage.MainBoss.MB29Conversion {
	public class MBConverter : DBConverter {
		private readonly ManifestXmlResolver xmlResolver;
		public static string Version = KB.I("2.966"); // Version of MB 29 exported schema we support
		public static string MB29SchemaNamespace = KB.I("http://thinkage.ca/MB29/dsMB29.xsd");
		private readonly IServer ServerObject;
		protected SqlClient.Connection srcSqlConnection;

		public MBConverter(SqlClient.Connection baseConnection, string src_dbname, string dest_dbname)
			: base(dest_dbname) {
			ServerObject = baseConnection.CreateServer();
			srcSqlConnection = baseConnection.WithNewDBName(src_dbname);
			xmlResolver = new ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly());
		}
		private System.Xml.Schema.ValidationEventHandler externalXmlValidationEventHandler;
		/// <summary>
		/// Load the 29 XML instance document into the MB29 dataset. We validate the input data during Xml processing and do not proceed with the import
		/// if any validation events are observed. This avoids getting errors trying to write 'bad' data to the Sql database and receiving even more
		/// error messages that are less useful
		/// </summary>
		/// <param name="dbserver"></param>
		/// <param name="dbname"></param>
		public void Load29XMLData(System.IO.Stream xmlInputReader, System.Xml.Schema.ValidationEventHandler eventHandler) {
			DBClient db;
			int tries = 10;
			if (srcSqlConnection.DBName == null)
				srcSqlConnection = srcSqlConnection.WithNewDBName(Strings.IFormat("{0}ImportData", ODBname));
			else
				tries = 0;
			for (; ; )
				try {
					db = new DBClient(new DBClient.Connection(srcSqlConnection, MB29.Schema), (SqlClient)ServerObject.CreateDatabase(srcSqlConnection, MB29.Schema));
					break;
				}
				catch (InterpretedDbException e) {
					if (e.InterpretedErrorCode != InterpretedDbExceptionCodes.DatabaseExists || --tries < 0)
						throw;
					// Try another name.
					srcSqlConnection = srcSqlConnection.WithNewDBName(Strings.IFormat("{0}{1}ImportData", ODBname, tries));
				}

			System.Xml.XmlReader schemaReader = null;
			System.Xml.XmlReader reader = null;
			externalXmlValidationEventHandler = eventHandler;
			try {
				using (MB29 dsMB29 = new MB29(db)) {
					dsMB29.Namespace = MB29SchemaNamespace; // must match the schema namespace otherwise ReadXML will NOT read in data since the XmlNodeMapping table will be built using the Namespace found in the dataset

					// We must cause all the tables to be created since our typed data sets do not normally do this.
					foreach (DBI_Table t in MB29.Schema.Tables)
						dsMB29.EnsureDataTableExists(t);

					//TODO: need to address AuditTable assumptions in MB3 creation code when using MB29 dataset definition
					// There seems to be no XmlReadMode that does what we want: Use the existing schema of the ds only,
					// and complain about data that does not fit. The IgnoreSchema at least prevents the ReadXml from damaging the
					// ds structure, but it causes data assigned to unknown tables and columns in the XML to be ignored and discarded.
					// Load our schema that we need for the data first

					System.Xml.XmlReaderSettings schemaReaderSettings = new System.Xml.XmlReaderSettings();
					XmlResolver schema_resolver = new ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly());
					schemaReaderSettings.XmlResolver = schema_resolver;
					schemaReaderSettings.ValidationType = System.Xml.ValidationType.None;
					schemaReaderSettings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings | System.Xml.Schema.XmlSchemaValidationFlags.ProcessSchemaLocation;
					schemaReaderSettings.ValidationEventHandler += new System.Xml.Schema.ValidationEventHandler(SchemaReaderSettings_ValidationEventHandler);

					schemaReader = System.Xml.XmlReader.Create("manifest://localhost/Thinkage/MainBoss/MB29Conversion/MB296.Schema.xsd", schemaReaderSettings);

					System.Xml.Schema.XmlSchemaSet cachedSchemaCollection = new System.Xml.Schema.XmlSchemaSet {
						XmlResolver = schema_resolver
					};
					cachedSchemaCollection.ValidationEventHandler += new System.Xml.Schema.ValidationEventHandler(SchemaReaderSettings_ValidationEventHandler);
					cachedSchemaCollection.Add(null, schemaReader);

					// Let the dataset read the schema we expect to see
					dsMB29.ReadXmlSchema(schemaReader);
					// Turn off checking during load data to possibly speed things up
					foreach (DataTable t in dsMB29.Tables)
						t.BeginLoadData();

					XmlReaderSettings readerSettings = new XmlReaderSettings {
						ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None,
						ValidationType = System.Xml.ValidationType.None
					};
					reader = System.Xml.XmlReader.Create(xmlInputReader, readerSettings);
					// Skip the inline schema without validation
					reader.ReadToFollowing("schema", "http://www.w3.org/2001/XMLSchema");
					string version = reader.GetAttribute("version");
					if (version != Version)
						throw new GeneralException(KB.T("Conversion can only use a MainBoss Version {0} exported XML file for input."), Thinkage.MainBoss.MB29Conversion.MBConverter.Version);
					// Read to the instance data
					reader.ReadToFollowing("MB29", MB29SchemaNamespace);
					// Create a new reader that validates against our resource schema (for date checking etc.)
					XmlReaderSettings validatingSettings = new XmlReaderSettings {
						// need ProcessInlineSchema to actual read our instance data
						ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings | System.Xml.Schema.XmlSchemaValidationFlags.ProcessInlineSchema,
						ValidationType = System.Xml.ValidationType.Schema
					};
					validatingSettings.Schemas.Add(cachedSchemaCollection);
					validatingSettings.ValidationEventHandler += new System.Xml.Schema.ValidationEventHandler(SchemaReaderSettings_ValidationEventHandler);
					reader = System.Xml.XmlReader.Create(reader, validatingSettings);
					try {
						// Now load the data from the current node (the instance data) we are positioned at
						dsMB29.ReadXml(reader.ReadSubtree(), System.Data.XmlReadMode.IgnoreSchema);
					}
					catch (System.Exception ex) {
						throw new GeneralException(ex, KB.T("Import data contains validation errors"));
					}
					foreach (DataTable t in dsMB29.Tables)
						t.EndLoadData();
#if DEBUG
					// Assign GUIDs to all the ID fields in the dataset since they are PrimaryKey fields
					// Our GUIDS will be the RECNOs of the original database for debugging
					int tableNumber = 0;
					foreach (DataTable dt in dsMB29.Tables) {
						if ((dt.Columns["ID"] as DataColumn) == null)
							continue;

						dt.Columns["ID"].ReadOnly = false;
						foreach (DataRow dr in dt.Rows) {
							int recno = (int)dr["RECNO"];
							Guid AlteredGuid = new System.Guid("{" + tableNumber.ToString("000") + recno.ToString("00000") + "-0000-0000-0000-000000000000}");
							dr["ID"] = AlteredGuid;
						}
						dt.Columns["ID"].ReadOnly = true;
						tableNumber++;
					}
#else
					// Assign GUIDs to all the ID fields in the dataset since they are PrimaryKey fields
					// Our GUIDS use "0029" to identify the records as MB 29 original records.
					foreach (DataTable dt in dsMB29.Tables) {
						if ((dt.Columns["ID"] as DataColumn) == null)
							continue;

						dt.Columns["ID"].ReadOnly = false;
						foreach (DataRow dr in dt.Rows) {
							string alt = Guid.NewGuid().ToString("");
							string altered = alt.Substring(0, 9) + KB.I("0029") + alt.Substring(13, 23);
							Guid AlteredGuid = new System.Guid(altered);
							dr["ID"] = AlteredGuid;
						}
						dt.Columns["ID"].ReadOnly = true;
					}
#endif
					// Make sure all the data imported keeps to the XAFDB schema requirements. We do this by assigning
					// each value of every field back to itself, processed through the XAF data converters.
					// To save time, we only do the VariableStringTypeInfo (the comment fields from 2.9 tables)
					foreach (DBI_Table tableSchema in MB29.Schema.Tables) {
						System.Collections.Generic.List<DBI_Column> toConvert = new System.Collections.Generic.List<DBI_Column>();
						foreach (DBI_Column columnSchema in tableSchema.Columns) {
							if (columnSchema.IsWriteable && columnSchema.EffectiveType is StringTypeInfo)
								toConvert.Add(columnSchema);
						}
						if (toConvert.Count > 0) {
							DBIDataTable t = dsMB29.GetDataTable(tableSchema);
							for (int i = 0; i < t.Rows.Count; ++i) {
								DBIDataRow r = t.Rows[i];
								foreach (DBI_Column columnSchema in toConvert) {
									r[columnSchema] = r[columnSchema];
								}
							}
						}
					}
					db.Update(dsMB29);
				}
			}
			catch {
				db.CloseDatabase();
				DeleteSrcDB();
				throw;
			}
			finally {
				if (reader != null)
					reader.Close();
				if (schemaReader != null)
					schemaReader.Close();
				db.CloseDatabase();
			}
		}
#pragma warning disable CA1822 // Mark members as static
		void DeleteSrcDB() {
#pragma warning restore CA1822 // Mark members as static
#if !DEBUG
			ServerObject.DeleteDatabase(srcSqlConnection);
#endif
		}

		private void SchemaReaderSettings_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e) {
			externalXmlValidationEventHandler(sender, e);
		}
		public void Convert29() {
			ISession db = null;
			try {
				// We use the Source database as the operating context so we can create temporary procedures that don't pollute our New imported database
				db = ServerObject.OpenSession(srcSqlConnection, MB29.Schema);
				Convert(srcSqlConnection.DBName, db, "manifest://localhost/Thinkage/MainBoss/MB29Conversion/dbconversion.xml", xmlResolver);
			}
			finally {
				if (db != null)
					db.CloseDatabase();
				DeleteSrcDB();
			}
		}
	}
}
