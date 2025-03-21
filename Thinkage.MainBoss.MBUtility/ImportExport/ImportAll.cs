﻿using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.MBUtility {
	internal class ImportAllVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Optable.Add(InputPackageFilename = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Input"), KB.I("File to read the packaged datasets from"), true));
				Optable.Add(ErrorOutputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("ErrorOutput"), KB.I("File containing the errors encountered during import."), false));

				Optable.MarkAsDefaults();
			}
			public readonly StringValueOption InputPackageFilename;
			public readonly StringValueOption ErrorOutputFile;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "ImportAll";
				}
			}
			public override void RunVerb() {
				new ImportAllVerb(this).Run();
			}
		}
		private ImportAllVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			DataImportExportHelper.Setup();
			Thinkage.MainBoss.Controls.DataImportExportPackage p = new Thinkage.MainBoss.Controls.DataImportExportPackage();
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string oName);
			DataImportExportHelper.SetupDatabaseAccess(oName, connect);
			// Get a connection to the database that we are exporting from
			Thinkage.Libraries.DBAccess.DBClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;

			DBIDataSet errors;
			using (dsMB mbds = new dsMB(db)) {
				mbds.DisableUpdatePropagation();
				errors = DataImportExportPackage.ImportXml(Options.InputPackageFilename.Value, mbds, schemaIdentification => new DataImportExport(schemaIdentification));
			}
			DataImportExportHelper.CheckAndSaveErrors(Options.ErrorOutputFile.HasValue ? Options.ErrorOutputFile.Value : null, errors, DataImportExport.CheckErrorDataSet(errors));
		}
	}
}
