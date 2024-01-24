using Thinkage.MainBoss.Controls;

namespace Thinkage.MainBoss.MBUtility {
	internal class ListImportSchemasVerb {
		public class Definition : Thinkage.Libraries.CommandLineParsing.Optable, UtilityVerbDefinition {
			public Definition()
				: base() {
			}
			public Thinkage.Libraries.CommandLineParsing.Optable Optable {
				get {
					return this;
				}
			}
			public string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "ListImportSchemas";
				}
			}
			public void RunVerb() {
				new ListImportSchemasVerb(this).Run();
			}
		}
		private ListImportSchemasVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		private void Run() {
			DataImportExportHelper.Setup(); // establish Tbl definition requirements
			System.Collections.Generic.List<Thinkage.Libraries.Translation.Key> sorted = new System.Collections.Generic.List<Thinkage.Libraries.Translation.Key>(TIGeneralMB3.RegisteredImportKeys.Keys);
			sorted.Sort(delegate(Thinkage.Libraries.Translation.Key a, Thinkage.Libraries.Translation.Key b) {
				return string.Compare(a.Translate(), b.Translate());
			});
			foreach (Thinkage.Libraries.Translation.Key key in sorted)
				System.Console.WriteLine(key.Translate());
		}
	}
}