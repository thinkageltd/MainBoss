using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;
namespace Thinkage.MainBoss.MBUtility
{
	internal class ExportAllVerb
	{
		public class Definition : UtilityVerbWithDatabaseDefinition
		{
			public Definition()
				: base()
			{
				Optable.Add(OutputPackageFilename = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Output"), KB.I("Filename to receive the packaged dataset export data"), true));
				Optable.Add(EmbedSchema = new BooleanOption(KB.I("EmbeddedSchema"), KB.I("Indicates if the output XML file should contain an embedded schema"), '+', false));
				Optable.Add(ExcludeDeleted = new BooleanOption(KB.I("ExcludeDeleted"), KB.I("Exclude deleted records"), '+', false));
				EmbedSchema.Value = false;
				Optable.MarkAsDefaults();
			}
			public readonly StringValueOption OutputPackageFilename;
			public readonly BooleanOption EmbedSchema;
			public readonly BooleanOption ExcludeDeleted;
			public override string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "ExportAll";
				}
			}
			public override void RunVerb()
			{
				new ExportAllVerb(this).Run();
			}
		}
		private ExportAllVerb(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			DataImportExportHelper.Setup();
			Thinkage.MainBoss.Controls.DataImportExportPackage p = new Thinkage.MainBoss.Controls.DataImportExportPackage();
			p.AddAllExports();
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			DataImportExportHelper.SetupDatabaseAccess(oName, connect);
			// Get a connection to the database that we are exporting from
			Thinkage.Libraries.DBAccess.DBClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			p.ExportXml(Options.OutputPackageFilename.Value, db, dsMB.Schema, Options.ExcludeDeleted.HasValue ? Options.ExcludeDeleted.Value : false, schemaIdentification => new DataImportExport(schemaIdentification));
		}
	}
}
