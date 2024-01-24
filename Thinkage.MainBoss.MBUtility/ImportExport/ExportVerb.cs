using System.Data;
using System.Xml;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;
using Thinkage.MainBoss.Controls;
namespace Thinkage.MainBoss.MBUtility
{
	internal class ExportVerb
	{
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Add(SchemaIdentification = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("SchemaIdentification"), KB.I("Identity of schema"), true));
				Add(OutputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Output"), KB.I("File to receive the export data"), true));
				Add(EmbedSchema = new BooleanOption(KB.I("EmbeddedSchema"), KB.I("Indicates if the output XML file should contain an embedded schema"), '+', false));
				Add(ExcelOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("EXCEL"), KB.I("Output has both export and schema files ready for import to EXCEL"), '+', false));
				EmbedSchema.Value = false;
				ExcelOption.Value = false;
				MarkAsDefaults();
			}
			public readonly StringValueOption SchemaIdentification;
			public readonly StringValueOption OutputFile;
			public readonly BooleanOption EmbedSchema;
			public readonly BooleanOption ExcelOption;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "Export";
				}
			}
			public override void RunVerb() {
				new ExportVerb(this).Run();
			}
		}
		private ExportVerb(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			DataImportExportHelper.Setup();
			DataImportExport id = new DataImportExport(DataImportExportHelper.ValidateSchemaIdentification(Options.SchemaIdentification.Value));
			// Get a connection to the database that we are exporting from
			string oName;
			MB3Client.ConnectionDefinition connect = MB3Client.OptionSupport.ResolveSavedOrganization(Options.OrganizationName, Options.DataBaseServer, Options.DataBaseName, out oName);
			DataImportExportHelper.SetupDatabaseAccess(oName, connect);
			Thinkage.Libraries.DBAccess.XAFClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			DataSet exportDataSet = id.LoadDataSetFromDatabase(db, dsMB.Schema);
			if( Options.EmbedSchema.Value && Options.ExcelOption.Value)
				throw new Thinkage.Libraries.GeneralException(KB.K("You cannot use EmbeddedSchema together with EXCEL"));
			id.WriteDataSetToFile(Options.OutputFile.Value, exportDataSet, Options.EmbedSchema.Value, Options.ExcelOption.Value);
		}
	}
}