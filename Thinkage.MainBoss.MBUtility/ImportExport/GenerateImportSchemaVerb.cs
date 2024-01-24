using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Controls;
namespace Thinkage.MainBoss.MBUtility
{
	internal class GenerateImportSchemaVerb
	{
		public class Definition : Thinkage.Libraries.CommandLineParsing.Optable, UtilityVerbDefinition
		{
			public Definition()
				: base()
			{
				Add(Output = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Output"), KB.I("File to receive the output schema"), false));
				Add(SchemaIdentification = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("SchemaIdentification"), KB.I("Identity of schema"), true));
				Add(ExcelOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("EXCEL"), KB.I("Schema is in format acceptable to EXCEL to export data"), '+', false));
				ExcelOption.Value = false;
				MarkAsDefaults();
			}
			public readonly Thinkage.Libraries.CommandLineParsing.StringValueOption Output;
			public readonly Thinkage.Libraries.CommandLineParsing.StringValueOption SchemaIdentification;
			public readonly Thinkage.Libraries.CommandLineParsing.BooleanOption ExcelOption;
			public Thinkage.Libraries.CommandLineParsing.Optable Optable
			{
				get
				{
					return this;
				}
			}
			public string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "GenerateImportSchema";
				}
			}
			public void RunVerb()
			{
				new GenerateImportSchemaVerb(this).Run();
			}
		}
		private GenerateImportSchemaVerb(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			DataImportExportHelper.Setup();
			DataImportExport id = new DataImportExport(DataImportExportHelper.ValidateSchemaIdentification(Options.SchemaIdentification.Value));
			string theSchema = Options.ExcelOption.Value ? id.DataHelper.ExcelSchemaText : id.DataHelper.StandardSchemaText;
			if (Options.Output.HasValue)
				System.IO.File.WriteAllText(Options.Output.Value, theSchema, System.Text.Encoding.Unicode);
			else
				System.Console.Write(theSchema);
		}
	}
}
