using System;
using System.Collections.Generic;
using System.Linq;
// A framework command for MB utility commands. Each command includes a class that implements UtilityVerbDefinition and an instance of
// this class is passed to the Optable ctor in the MainApplication ctor. This means the verb Definitions are constucted after Application.Instance
// is set so all the translation stuff works properly in the Definition derived ctors. Note that I am not sure of the timing of static initializations in the
// Definition classes and their enclosing classes if any.
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.MBUtility {
	public interface UtilityVerbDefinition {
		Thinkage.Libraries.CommandLineParsing.Optable Optable { get; }
		string Verb
		{
			[return: Thinkage.Libraries.Translation.Invariant]
			get;
		}
		void RunVerb();
	}
	public abstract class UtilityVerbWithDatabaseDefinition : UtilityVerbDefinition {
		public UtilityVerbWithDatabaseDefinition()
			: base() {
			Optable = new MB3Client.OptionSupport.DatabaseConnectionOptable();
		}
		public MB3Client.ConnectionDefinition ConnectionDefinition(out string oName) =>
			((MB3Client.OptionSupport.DatabaseConnectionOptable)Optable).ResolveConnectionDefinition(out oName, out NamedOrganization _);
		public Optable Optable {get; private set;}
		public abstract string Verb { [return: Thinkage.Libraries.Translation.Invariant] get; }
		public abstract void RunVerb();
	}
	internal class MainApplication : Thinkage.Libraries.Application {
		// TODO: A whole bunch of stuff (e.g. translator setup) that occurs in the derivation path from Thinkage.Libraries.Application to the full MB app must be copied to this
		// class, mostly because multiple base classes and virtual baes classes do not exist.
		// Our derivation is T.L.MSWindows.Application->T.L.Console.Application->us
		// MB derivation is T.L.MSWindows.Application->XAF.UI.Application->MB3.WinControls.Application->...WindowedMainBossApplication->...TblDrivenMainBossApplication
		// ->Applications.MB3.MainBoss.MainBossApplication
		// The one we are most likely to have to steal from is MB3.WinControls.Application
		static int Main(string[] args) {
			try	{  //if running under powershell ISE System.Console.CursorLeft faults, but it will not disappear any since it is inside of Powershell ISE
				StopConsoleFromDisappearing = Environment.UserInteractive && (System.Console.CursorLeft == 0 && System.Console.CursorTop == 0);
			}
			catch (SystemException ) {
				StopConsoleFromDisappearing = false;
            }
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((o, a) => {
				System.Exception e = (System.Exception)a.ExceptionObject;
				GeneralException eg = e as GeneralException;
				if (e != null)
					System.Console.WriteLine(Thinkage.Libraries.Exception.FullMessage(e));
				else
					System.Console.WriteLine(Strings.Format(KB.K("Exception: {0}"), Thinkage.Libraries.Exception.FullMessage(e)));
				Exit(1);
			});

			try {
				new MainApplication(args);
				if (!Run())
					Exit(1);
			}
			catch (Thinkage.Libraries.Exception ex) {
				System.Console.WriteLine(Thinkage.Libraries.Exception.FullMessage(ex));
				Exit(1); // failure
			}
#if DEBUG
			catch (System.Exception ex) {
				System.Console.WriteLine(Thinkage.Libraries.Exception.FullMessage(ex));
			}
#endif
			Exit(0); // success
			return 0;
		}
		public static bool StopConsoleFromDisappearing = true;
		static public void Exit(int status) {
			try	{
				if (System.Environment.UserInteractive && StopConsoleFromDisappearing) {
					while (System.Console.KeyAvailable)
						System.Console.ReadKey(true);
					System.Console.WriteLine(Strings.Format(KB.K("Press any key to end program")));
					System.Console.ReadKey(true);
				}
			}
			catch( SystemException ) {  }
			System.Environment.Exit(status);
		}
		private class Optable : Thinkage.Libraries.CommandLineParsing.Optable {
			public Optable(params UtilityVerbDefinition[] subapps) {
				Subapps = subapps;
				DefineVerbs(true, subapps.Select(svd => new KeyValuePair<string, Libraries.CommandLineParsing.Optable>(svd.Verb, svd.Optable)));
			}
			private readonly UtilityVerbDefinition[] Subapps;
			public void RunVerb() {
				Subapps[VerbValue].RunVerb();
			}
		}
		private MainApplication(string[] args) {

			// Add other verbs as extra arguments to this ctor.
			Ops = new Optable(
#if DEBUG
				new LoadSecurityVerb.Definition(),
#endif
				new BackupVerb.Definition(),
				new AddOrganizationVerb.Definition(),
				new DeleteOrganizationVerb.Definition(),
				new ListOrganizationsVerb.Definition(),
				new ListImportSchemasVerb.Definition(),
				new GenerateImportSchemaVerb.Definition(),
				new ImportVerb.Definition(),
				new ExportVerb.Definition(),
				new HelpVerb.Definition(),
				new AddRequestorFromLDAP.Definition(),
				new AddContactFromLDAP.Definition(),
				new UpdateContactsFromLDAP.Definition(),
				new ExportAllVerb.Definition(),
				new ImportAllVerb.Definition(),
				new ImportCustomizationVerb.Definition(),
				new ExportCustomizationVerb.Definition(),
				new CreateMainBossBasicDatabaseVerb.Definition(),
				new EditServiceConfigurationVerb.Definition()
//				new ImportPhysicalCountsVerb.Definition(),
//				new PreparePhysicalCountsVerb.Definition()
				//, new ScriptVerb.Definition()	Script verb removed for 3.1 release
				//, new GenerateWebApi.Definition()
				);
			try {
				Ops.Parse(args);
				Ops.CheckRequired();
			}
			catch (Thinkage.Libraries.CommandLineParsing.Exception ex)
			{
				Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("For help, type 'mbutility help'")));
				throw;
			}
		}
		protected override void CreateUIFactory() {
			new StandardApplicationIdentification(this, ApplicationParameters.RegistryLocation, "MainBoss Utilities");
			new Thinkage.Libraries.Console.UserInterface(this);
		}
		private readonly Optable Ops;
		public override RunApplicationDelegate GetRunApplicationDelegate {
			get {
				return delegate() {
					Ops.RunVerb();
					return null;
				};
			}
		}
		#region HelpVerb
		private class HelpVerb
		{
			public class Definition : Thinkage.Libraries.CommandLineParsing.Optable, UtilityVerbDefinition
			{
				public Definition()
					: base()
				{
				}
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
						return "Help";
					}
				}
				public void RunVerb()
				{
					new HelpVerb(this).Run();
				}
			}
			private HelpVerb(Definition options)
			{
				Options = options;
			}
			private readonly Definition Options;
			private void Run()
			{
				Optable mainOptable = ((MainApplication)Application.Instance).Ops;
				// Clear the verb so help is issued for ALL verbs, not just the help verb.
				// should do this, but accessor is deficient and doesn't permit mainOptable.VerbValue = null;
				mainOptable.MarkVerbAsDefault();
				System.Console.WriteLine(mainOptable.Help);
			}
		}
		#endregion
	}
}
