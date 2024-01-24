using System;
using System.Collections;
using System.Text;
using System.Configuration;
using System.Xml;
using Thinkage.Libraries.CommandLineParsing;
using System.Web.Configuration;
using System.Collections.Specialized;

namespace Thinkage.MainBoss.Web {
	public class ConfigHandler : Optable {
		public ConfigHandler() {
			// TODO: Fetch these option definitions from a common place; they are the same as the ones on the client MainBoss command.
			Add(pOrganizationName = new StringValueOption("OrganizationName", "The name of the organization", false));
			Add(pDatabaseServer = new StringValueOption("DatabaseServer", "The name of the SQL server instance", true));
			Add(pDatabaseName = new StringValueOption("DatabaseName", "The name of the databasae", true));
		}
		public static ConfigHandler FetchConfig() {
			ConfigHandler result = new ConfigHandler();
			NameValueCollection settings = WebConfigurationManager.AppSettings;
			result.Parse(settings);
			result.CheckRequired();
			return result;
		}

		public string OrganizationName {
			get { return pOrganizationName.Value; }
		}
		private readonly StringValueOption pOrganizationName;
		public string DatabaseServer {
			get { return pDatabaseServer.Value; }
		}
		private readonly StringValueOption pDatabaseServer;
		public string DatabaseName {
			get { return pDatabaseName.Value; }
		}
		private readonly StringValueOption pDatabaseName;
	}
}
