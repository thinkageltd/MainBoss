using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MBUtility {
	internal class ImportPhysicalCountsVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Optable.Add(InputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Input"), KB.I("File containing the import data"), true));
				Optable.Add(ErrorOutputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("ErrorOutput"), KB.I("File containing the errors encountered during import."), false));
				Optable.Add(AdjustmentCode = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("AdjustmentCode"), KB.I("The Adjustment Code to use for the physical count"), true));
			}
			public readonly StringValueOption InputFile;
			public readonly StringValueOption ErrorOutputFile;
			public readonly StringValueOption AdjustmentCode;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "ImportPhysicalCounts";
				}
			}
			public override void RunVerb() {
				new ImportPhysicalCountsVerb(this).Run();
			}
		}
		private ImportPhysicalCountsVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;

		private void Run() {
			DataImportExportHelper.Setup();
			try {
				// Get a connection to the database that we are importing to
				MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
				DataImportExportHelper.SetupDatabaseAccess(oName, connect);
				Thinkage.Libraries.DBAccess.XAFClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
				DataSet errors;
				using (dsMB mbds = new dsMB(db)) {
					mbds.DisableUpdatePropagation();
					// Fetch the physical counts
					PhysicalCountImportExport physicalCountImport = new PhysicalCountImportExport();
					physicalCountImport.LoadPhysicalCounts(System.IO.File.ReadAllText(Options.InputFile.Value), new System.Xml.Schema.ValidationEventHandler(xml_ValidationEventHandler));
					// and create them
					errors = physicalCountImport.CreatePhysicalCounts(mbds, Options.AdjustmentCode.Value, new UserIDSource());
				}
				DataImportExportHelper.CheckAndSaveErrors(Options.ErrorOutputFile.HasValue ? Options.ErrorOutputFile.Value : null, errors, DataImportExport.CheckErrorDataSet(errors));
			}
			catch (System.Exception ex) {
				throw new GeneralException(ex, KB.K("Import of physical counts failed"));
			}
		}
		// Report import data XML validation errors with line number and position to Console
		void xml_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e) {
			System.Console.WriteLine(Strings.Format(KB.K("Line {0}, Position {1}:{2}"), e.Exception.LineNumber, e.Exception.LinePosition, e.Message));
		}
	}
}