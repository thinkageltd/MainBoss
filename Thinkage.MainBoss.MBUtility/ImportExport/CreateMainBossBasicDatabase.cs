using System.Data;
using System.Xml;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;
using Thinkage.MainBoss.Controls;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries;
using System;
namespace Thinkage.MainBoss.MBUtility
{
	internal class CreateMainBossBasicDatabaseVerb
	{
		public class Definition : UtilityVerbWithDatabaseDefinition
		{
			public Definition()
				: base()
			{
				Optable.Add(BasicXMLInputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("Input"), KB.I("File containing the MainBoss Basic exported data"), true));
				Optable.Add(ErrorOutputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("ErrorOutput"), KB.I("File containing the errors encountered during import."), false));

				Optable.MarkAsDefaults();
			}
			public readonly StringValueOption BasicXMLInputFile;
			public readonly StringValueOption ErrorOutputFile;
			public override string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "CreateMainBossBasicDatabase";
				}
			}
			public override void RunVerb()
			{
				new CreateMainBossBasicDatabaseVerb(this).Run();
			}
		}
		private CreateMainBossBasicDatabaseVerb(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			string separator = Strings.IFormat("*****************************************************************{0}", Environment.NewLine);
			string xmlInput = Options.BasicXMLInputFile.Value;
			MB3Client.ConnectionDefinition connect = Options.ConnectionDefinition(out string _);

			System.Exception processingException = null;
			System.Text.StringBuilder HistoryLogText = new System.Text.StringBuilder();
			System.IO.FileStream xmlInputReader = null;
			try {
				xmlInputReader = new System.IO.FileStream(xmlInput, System.IO.FileMode.Open, System.IO.FileAccess.Read);

				Thinkage.MainBoss.MB29Conversion.MBConverter theConverter = new Thinkage.MainBoss.MB29Conversion.MBConverter((SqlClient.Connection)connect.ConnectionInformation, connect.DBName, null);
				theConverter.Load29XMLData(xmlInputReader, delegate(object sender, System.Xml.Schema.ValidationEventArgs e)
				{
					HistoryLogText.Append(separator);
					HistoryLogText.Append(Strings.Format(KB.K("Line {0}, Position {1}"), e.Exception.LineNumber, e.Exception.LinePosition));
					HistoryLogText.Append(Environment.NewLine);
					HistoryLogText.AppendLine(e.Exception.Message); // only use the Xml validation message; the inner exception just repeats the same message again
				});
			}
			// all errors conditions in conversion will have thrown an error of some form
			catch (System.Exception ex) {
				processingException = ex; // remember exception to return on exit
				Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Unable to load MainBoss Basic exported data from {0}"), xmlInput));
			}
			finally {
				if (xmlInputReader != null)
					xmlInputReader.Close();
			}
			if (processingException != null) {
				if (Options.ErrorOutputFile.HasValue) {
					try {
						System.IO.StreamWriter errorOutput = new System.IO.StreamWriter(Options.ErrorOutputFile.Value, false, System.Text.Encoding.Unicode);
						errorOutput.Write(Thinkage.Libraries.Exception.FullMessage(processingException));
						errorOutput.Write(Environment.NewLine);
						errorOutput.Write(HistoryLogText.ToString());
						errorOutput.Write(Environment.NewLine);
						errorOutput.Close();						
					}
					catch (System.Exception ex) {
						Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("Unable to save error output to {0}"), Options.ErrorOutputFile.Value));
						throw;
					}
				}
				throw processingException;
			}
		}
	}
}
