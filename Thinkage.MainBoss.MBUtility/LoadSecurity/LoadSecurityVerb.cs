using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Layout;
using System.Collections.Generic;
using System;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
namespace Thinkage.MainBoss.MBUtility {
	internal class LoadSecurityVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Optable.Add(SecurityDefinitionsFileName = new StringValueOption("SecurityDefinitions", KB.K("Specify the name of the file containing the security definitions to be loaded").Translate(), true));
			}
			public readonly StringValueOption SecurityDefinitionsFileName;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "LoadSecurity";
				}
			}
			public override void RunVerb() {
				new LoadSecurityVerb(this).Run();
			}
		}
		private LoadSecurityVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string _);
			// Get a connection to the database that we are referencing
			// Because we don't enforce any permissions ourselves, nor any licensing, we don't use the usual open-database code.
			Thinkage.Libraries.DBAccess.DBClient db = new MB3Client(connect);

			// The roles table appeared in its current form at this version
			_ = MBUpgrader.UpgradeInformation.CheckDBVersion(db, VersionInfo.ProductVersion, new System.Version(1, 0, 4, 38), dsMB.Schema.V.MinMBAppVersion, KB.I("MainBoss Utility Tool--Load Security"));
			db.ObtainSession((int)DatabaseEnums.ApplicationModeID.UtilityTool);

			using (dsMB updateDs = new dsMB(db)) {
				Thinkage.MainBoss.Database.SecurityCreation.CreateSecurityDataSet(updateDs, null, Options.SecurityDefinitionsFileName.Value);
				db.Update(updateDs);
			}
		}
	}
}
