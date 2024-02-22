using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.MainBoss.Database;
using System.Collections.Generic;
using System.Text;
using Thinkage.Libraries.Translation;
using System;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.MBUtility {
	internal class ListOrganizationsVerb {
		public class Definition : Thinkage.Libraries.CommandLineParsing.Optable, UtilityVerbDefinition {
			public Definition()
				: base() {
				Add(OrderbyServerOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("OrderbyServer"), KB.K("Order the list by the Database Server Name").Translate(), '+', false));
				OrderbyServerOption.Value = false;
				Add(OrderbyDatabaseOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("OrderbyDatabaseName"), KB.K("Order the list by the Database Name").Translate(), '+', false));
				OrderbyDatabaseOption.Value = false;
				Add(DatabaseVersionOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("DatabaseVersion"), KB.K("Show the verion of the MainBoss database, if the database is accessible").Translate(), '+', false));
				DatabaseVersionOption.Value = false;
				Add(DefaultsOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("ShowDefaults"), KB.K("Show the Start Default if any (and Last Selected Organization if different)").Translate(), '+', false));
				DefaultsOption.Value = false;
				Add(ProbeOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("Probe"), KB.K("Test if it is possible to connect to the referenced database and show error message if it is not possible").Translate(), '+', false));
				ProbeOption.Value = false;
				Add(ShowRealNamesOption = new Thinkage.Libraries.CommandLineParsing.BooleanOption(KB.I("RealNames"), KB.K("Show the real (registry) organization names as well as their display names").Translate(), '+', false));
				ShowRealNamesOption.Value = false;
				Add(AllUsersOption = new Libraries.CommandLineParsing.BooleanOption(KB.I("AllUsers"), KB.K("Show the AllUsers organization entry instead of the list").Translate(), '+', false));
				AllUsersOption.Value = false;
				MarkAsDefaults();
			}
			public Thinkage.Libraries.CommandLineParsing.BooleanOption AllUsersOption;
			public Thinkage.Libraries.CommandLineParsing.BooleanOption ProbeOption;
			public Thinkage.Libraries.CommandLineParsing.BooleanOption ShowRealNamesOption;
			public Thinkage.Libraries.CommandLineParsing.BooleanOption OrderbyServerOption;
			public Thinkage.Libraries.CommandLineParsing.BooleanOption OrderbyDatabaseOption;
			public Thinkage.Libraries.CommandLineParsing.BooleanOption DatabaseVersionOption;
			public Thinkage.Libraries.CommandLineParsing.BooleanOption DefaultsOption;
			public Thinkage.Libraries.CommandLineParsing.Optable Optable {
				get { return this; }
			}
			public string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get { return "ListOrganizations"; }
			}
			public void RunVerb() {
				new ListOrganizationsVerb(this).Run();
			}
		}
		private ListOrganizationsVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		struct Names {
			public NamedOrganization Real;
			public string Database;
			public string Application;
			public string Server;
			public string Probe;
			public string Version;
			public string Defaults;
		}
		class Column {
			string title;
			int width;
			bool needed;
			const int maxwidth = 50;
			public Column(Key Title, bool Needed = true ) {
				title = Title.Translate();
				needed = Needed;
				width = title.Length;
			}
			public void Contains(string v ) {
				width = Math.Min(maxwidth, Math.Max(width, v.Length));
			}
			public string Title {
				get { return Display(title); }
			}
			public string Display([Invariant]string v) {
				if (v == null) v = "";
				if (!needed) return "";
				if (v.Length > maxwidth - 3)
					v = v.Substring(maxwidth) + KB.I("...");
				return v + " ".PadRight(width - v.Length+1);
			}
		}

		private void Run() {
			DBVersionHandler vh = new DBVersionHandler();
			MainBossNamedOrganizationStorage connections = new MainBossNamedOrganizationStorage(Options.AllUsersOption.Value ? (IConnectionInformation) new SavedOrganizationSessionAllUsers.Connection(writeAccess:false) : new SavedOrganizationSession.Connection());
			var names = new List<Names>();
			var organization = new Column(KB.K("Organization"));
			var application = new Column(KB.K("Application"));
			var server = new Column(KB.K("Server"));
			var database = new Column(KB.K("Database Name"));
			var probestatus = new Column(KB.K("Probe Status"), Options.ProbeOption.Value);
			var registrykey = new Column(KB.K("Registry Key"), Options.ShowRealNamesOption.Value);
			var databaseversion = new Column(KB.K("Version"), Options.DatabaseVersionOption.Value);
			var defaults = new Column(KB.K("Defaults"), Options.DefaultsOption.Value);
			//var databasevsersion = new Column("Database Version", Options.DatabaseVersion.Value);
			var startname = connections.PreferredOrganizationId;
			foreach (NamedOrganization cn in connections.GetOrganizationNames()) {
				var n = new Names();
				MB3Client.MBConnectionDefinition def = cn.ConnectionDefinition;
				n.Real = cn;
				n.Application = DatabaseEnums.ApplicationModeName(def.ApplicationMode).Translate();
				n.Database = def.DBName;
				n.Server = def.DBServer;
				n.Probe = "";
				n.Version = "";
				if( n.Real.Id == startname )
					n.Defaults = KB.K("Start").Translate();
				else
					n.Defaults = "";
				if (Options.ProbeOption.Value || Options.DatabaseVersionOption.Value) {
					MB3Client probeSession = null;
					try {
						probeSession = new MB3Client(def);
						vh.CurrentVersion = MBUpgrader.UpgradeInformation.LatestDBVersion;	// give vh a starting point for looking at version info
						vh.LoadDBVersion(probeSession);
						n.Version = vh.CurrentVersion.ToString();
						n.Probe = Strings.Format(KB.K("Access OK"));
						// TODO: Also check that license keys allow def's preferred mode???
					}
					catch (System.Exception ex) {
						n.Probe = Strings.Format(KB.K("Access failed: {0}"), Thinkage.Libraries.Exception.FullMessage(ex));
					}
					finally {
						if (probeSession != null)
							probeSession.CloseDatabase();
					}
				}
				organization.Contains(n.Real.DisplayName);
				application.Contains(n.Application);
				server.Contains(n.Server);
				database.Contains(n.Database);
				registrykey.Contains(n.Real.Id.ToString());
				databaseversion.Contains(n.Version);
				// probestatus.Contains(n.Probe); // error message can be multiple lines don't register, always prints as last so its not a problem
				names.Add(n);
			}
			if( names.Count <= 0 ) 
				System.Console.WriteLine(Strings.Format(KB.K("There are no saved organizations")));
			else {
				var format = "{0}{1}{2}{3}{4}{5}{6}{7}";
				System.Console.WriteLine(Strings.IFormat(format, organization.Title, database.Title, server.Title, application.Title, defaults.Title, registrykey.Title, databaseversion.Title, probestatus.Title).TrimEnd());
				var outputorder = names.OrderBy(p => p.Real.DisplayName.ToLower());
				if( Options.OrderbyDatabaseOption.Value )
					outputorder = outputorder.OrderBy(p => p.Database.ToLower()); 
				if( Options.OrderbyServerOption.Value )
					outputorder = outputorder.OrderBy(p => p.Server.ToLower()); 
				foreach (var n in outputorder) {
					System.Console.WriteLine(Strings.IFormat(format,
						organization.Display(n.Real.DisplayName), 
						database.Display(n.Database), 
						server.Display(n.Server), 
						application.Display(n.Application),
						defaults.Display(n.Defaults),
						registrykey.Display(n.Real.Id.ToString()),
						databaseversion.Display(n.Version),
						n.Probe).TrimEnd()
					);
				}
			}
		}
	}
}
