using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBILibrary;
using System.Collections.Generic;
using System;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
namespace Thinkage.MainBoss.MBUtility
{
	internal class ImportCustomizationVerb
	{
		public class Definition : UtilityVerbWithDatabaseDefinition
		{
			public Definition()
				: base()
			{
				Add(CustomizationFileName = new StringValueOption("Customizations", KB.K("Specify the name of the file containing the customizations to be imported").Translate(), true));
			}
			public readonly StringValueOption CustomizationFileName;
			public override string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "ImportCustomization";
				}
			}
			public override void RunVerb()
			{
				new ImportCustomizationVerb(this).Run();
			}
		}
		private ImportCustomizationVerb(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			MB3Client.ConnectionDefinition connect = MB3Client.OptionSupport.ResolveSavedOrganization(Options.OrganizationName, Options.DataBaseServer, Options.DataBaseName, out string oName);

			Customization.SetupDatabaseAccess(oName, connect);
			Thinkage.Libraries.DBAccess.DBClient db = Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
			using (dsMB ds = new dsMB(db)) {
				db.EditVariable(ds, dsMB.Schema.V.HiddenFeatures);
				ds.V.HiddenFeatures.Value = Customization.ImportCustomizationFile(Options.CustomizationFileName.Value);
				db.Update(ds);
			}
		}
	}
}