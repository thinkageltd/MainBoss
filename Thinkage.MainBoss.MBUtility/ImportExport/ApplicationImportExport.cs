using System;

using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls;

namespace Thinkage.MainBoss.MBUtility.ImportExport {
	//TODO: This class should implement the actual import/export/generateschema for the verbs to call.
	public class ApplicationImportExport : GroupedInterface<IApplicationInterfaceGroup>, IApplicationDataImportExport, IDataImportExport {
		public ApplicationImportExport(GroupedInterface<IApplicationInterfaceGroup> attachTo)
			: base(attachTo) {
			RegisterService<IApplicationDataImportExport>(this);
			RegisterService<IDataImportExport>(this);
		}

		#region IDataImportExport Members
		public string DataSetNamespace {
			get {
				return KB.I("thinkage.ca/MainBoss");
			}
		}
		public IServer ServerProperties {
			get {
				// Use the database connection for serverProperties (must exist)
				var databaseAppConnection = Thinkage.Libraries.Application.Instance.QueryInterface<IApplicationWithSingleDatabaseConnection>();
				return databaseAppConnection != null ? databaseAppConnection.Session.Session.Server : new Thinkage.Libraries.DBILibrary.MSSql.SqlClient.SqlServer();
			}
		}
		#endregion
		#region IApplicationDataImportExport Members
		public void Import(UIFactory uiFactory, XAFClient DB, Tuple<string, DelayedCreateTbl> info) {
		}

		public void Export(UIFactory uiFactory, XAFClient DB, Tuple<string, DelayedCreateTbl> info) {
		}

		public void GenerateXmlSchema(UIFactory uiFactory, Tuple<string, DelayedCreateTbl> info) {
		}
		public Tuple<string, DelayedCreateTbl> FindImportExportInformation(Tbl.TblIdentification tid) {
			return TIGeneralMB3.FindImport(tid);
		}
		public string FindImportExportNameFromSchemaIdentification(string schemaIdentification) {
			return TIGeneralMB3.FindImportNameFromSchemaIdentification(schemaIdentification);
		}
		public Tuple<string, DelayedCreateTbl> FindImportExportInformation(string identifyingName) {
			return TIGeneralMB3.FindImport(identifyingName);
		}
		#endregion
	}
}
