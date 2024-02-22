using System;
using System.Data;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	public class DataImportExportPackage : Thinkage.Libraries.Presentation.DataImportExportPackage
	{
		public DataImportExportPackage()
		{
		}
		#region Import
		/// <summary>
		/// Fetch the import data using a web client, and pass the opened stream to ImportXml to import the data
		/// </summary>
		/// <param name="db"></param>
		/// <param name="uri"></param>
		public void Import(MB3Client db, Uri uri) {
			System.IO.Stream importDataStream = null;
			try {
				using (var webFetch = new System.Net.WebClient()) {
					webFetch.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
					importDataStream = webFetch.OpenRead(uri);

					DataSet errors;
					using (dsMB mbds = new dsMB(db)) {
						mbds.DisableUpdatePropagation();
						errors = ImportXml(importDataStream, mbds, schemaIdentification => new DataImportExport(schemaIdentification));
					}
					var tables = errors.Tables.Cast<DataTable>();
					int max = tables.Select<DataTable, int>(t => t.Rows.Count).Sum();
					if (max > 0) {
						GeneralException ex = new GeneralException(KB.T(Strings.Format(KB.K("{0} {0.IsOne ? error was : errors were } encountered during import."), max)));
						var rows = tables.SelectMany<DataTable, DataRow>(t => t.Rows.Cast<DataRow>());
						int errorsIsDuplicate = 0;
						for (int i = 0; i < max; ++i) {
							Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Error {0}: {1}"), i + 1, rows.ElementAt<DataRow>(i)["ErrorOnSave"]));
							errorsIsDuplicate += (bool)rows.ElementAt<DataRow>(i)["ErrorIsDuplicate"] == true ? 1 : 0;
						}
						if( max != errorsIsDuplicate)
							throw ex;
						// all errors were duplicate (unique violation constraints) so we will NOT say anything
					}
				}
			}
			catch (System.Net.WebException e) {
				Thinkage.Libraries.Exception.AddContext(e, new Thinkage.Libraries.MessageExceptionContext(KB.K("Data import package identified as {0}"), uri.OriginalString));
				throw;
			}
			finally {
				if (importDataStream != null)
					importDataStream.Close();
			}
		}
		#endregion
		#region Export
		/// <summary>
		/// Add all schemas that are eligible for importing to be exported. Used to build entire import sets from a sample database.
		/// </summary>
		public void AddAllExports()
		{
			// TODO: Revisit this to see if all this complication of sorting by the translated name of the SchemaIdentification is really required, or just use the values in RegisteredImportKeys as the exist.
			System.Collections.Generic.List<Thinkage.Libraries.Translation.Key> sorted = new System.Collections.Generic.List<Thinkage.Libraries.Translation.Key>(TIGeneralMB3.RegisteredImportKeys.Keys);
			sorted.Sort(delegate(Thinkage.Libraries.Translation.Key a, Thinkage.Libraries.Translation.Key b)
			{
				return string.Compare(a.Translate(), b.Translate());
			});
			IApplicationDataImportExport importer = Application.Instance.GetInterface<IApplicationDataImportExport>();
			foreach (Thinkage.Libraries.Translation.Key key in sorted) {
				AddExport(importer.FindImportExportInformation(importer.FindImportExportNameFromSchemaIdentification(key.Translate())));
			}
		}
		#endregion
	}
}
