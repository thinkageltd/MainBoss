using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;
using Thinkage.MainBoss.Controls;
using System;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.DataFlow;
using System.Collections.Generic;

namespace Thinkage.MainBoss.MBUtility {
	internal class PreparePhysicalCountsVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
			}
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "PreparePhysicalCounts";
				}
			}
			public override void RunVerb() {
				new PreparePhysicalCountsVerb(this).Run();
			}
		}
		private PreparePhysicalCountsVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			// Get a connection to the database that we are importing to
			DataImportExportHelper.Setup();
			MB3Client.ConnectionDefinition connect = MB3Client.OptionSupport.ResolveSavedOrganization(Options.OrganizationName, Options.DataBaseServer, Options.DataBaseName, out string oName);
			DataImportExportHelper.SetupDatabaseAccess(oName, connect);
			Thinkage.Libraries.DBAccess.DBClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			using (dsMB mbds = new dsMB(db)) {
				mbds.DisableUpdatePropagation();
				// Produce the Storeroom Assignment data for excel
				PhysicalCountImportExport physicalCountImport = new PhysicalCountImportExport();
				physicalCountImport.PreparePhysicalCounts(mbds);
			}
		}
	}
}
