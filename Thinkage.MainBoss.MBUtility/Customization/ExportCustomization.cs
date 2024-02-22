using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Layout;
using System.Collections.Generic;
using System;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
namespace Thinkage.MainBoss.MBUtility
{
	internal class ExportCustomizationVerb
	{
		public class Definition : UtilityVerbWithDatabaseDefinition
		{
			public Definition()
				: base()
			{
				Add(CustomizationFileName = new StringValueOption("Customizations", KB.K("Specify the name of the file where the customizations will be exported").Translate(), true));
			}
			public readonly StringValueOption CustomizationFileName;
			public override string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "ExportCustomization";
				}
			}
			public override void RunVerb()
			{
				new ExportCustomizationVerb(this).Run();
			}
		}
		private ExportCustomizationVerb(Definition options)
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
				db.ViewAdditionalVariables(ds, dsMB.Schema.V.HiddenFeatures);
				if (ds.V.HiddenFeatures.Value == null)
					Application.Instance.DisplayInfo(KB.K("There are no customizations to export from this database.").Translate());
				else
					Customization.ExportCustomizationFile(Options.CustomizationFileName.Value, (VisabilityCustomizations)dsMB.Schema.V.HiddenFeatures.EffectiveType.GenericAsNativeType(ds.V.HiddenFeatures.Value, typeof(VisabilityCustomizations)));
			}
		}
	}
}