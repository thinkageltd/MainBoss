using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.RDL2010;
using Thinkage.Libraries.RDLReports;
using Thinkage.MainBoss.Database;
namespace Thinkage.MainBoss.Controls.Reports {
	public class MainBossFormReport : FormReport {
		public MainBossFormReport(Report r, ReportViewLogic logic)
			: base(r, logic) {
		}

		/// <summary>
		/// Forms Reports do not want the Report coded Parameters splattered at the top of the report so we override the code normally used to do so.
		/// </summary>
		protected override void ReportTblCodedParameters() {
		}
		protected override System.Drawing.Image LogoImage {
			get {
				using (dsMB tempds = new dsMB(Logic.DB)) {
					Logic.DB.ViewAdditionalVariables(tempds, dsMB.Schema.V.CompanyLogo);
					if (!tempds.V.CompanyLogo.IsNull)
						using (var stream = new System.IO.MemoryStream(tempds.V.CompanyLogo.Value)) {
							return System.Drawing.Image.FromStream(stream);
						}
				}
				return null;
			}
		}
		protected override List<Paragraph> CompanyLocationInformation {
			get {
				using (dsMB tempds = new dsMB(Logic.DB)) {
					Logic.DB.ViewAdditionalVariables(tempds, dsMB.Schema.V.CompanyLocationID);
					if (!tempds.V.CompanyLocationID.IsNull) {
						tempds.DB.ViewAdditionalRows(tempds, dsMB.Schema.T.LocationReport, new SqlExpression(dsMB.Path.T.LocationReport.F.LocationID).Eq(SqlExpression.Constant(tempds.V.CompanyLocationID.Value)));
						if (tempds.T.LocationReport.Rows.Count > 0) {
							dsMB.LocationReportRow locrow = (dsMB.LocationReportRow)tempds.T.LocationReport.Rows[0];
							return new[] { new Paragraph(locrow.F.LocationCode), new Paragraph(locrow.F.LocationDetail) }.ToList();
						}
					}
				}
				// There is no company information. Instead give a single paragraph containing a single run whose value is an empty string.
				// This is because TextBox must contain at least one paragraph and paragraph must contain at least one text run.
				return new[] { new Paragraph(KB.T("")) }.ToList();
			}
		}
	}
}
