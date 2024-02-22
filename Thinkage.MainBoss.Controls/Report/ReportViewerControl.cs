using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.RDL2010;
using Thinkage.Libraries.RDLReports;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// MainBoss derived to provide SuppressCostPermissions
	/// </summary>
	public class ReportViewerControl : Thinkage.Libraries.Presentation.MSWindows.ReportViewControl {
		#region ISuppressCostPermissions Members
		public override IEnumerable<IDisablerProperties> SuppressCostPermissions {
			get {
				return SecurityPermissionsForTbl(this.pReportInfo);
			}
		}
		#endregion
		public ReportViewerControl(UIFactory uiFactory, DBClient db, Tbl tbl, Settings.Container settingsContainer, SqlExpression filterExpression)
			: base(uiFactory, db, tbl, settingsContainer, filterExpression) {
		}
		public static IEnumerable<IDisablerProperties> SecurityPermissionsForTbl(Tbl tbl) {
			IRightGrantor manager = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager;
			var perms = new List<IDisablerProperties>();
			foreach (string p in tbl.Schema.CostRights) {
				perms.Add((IDisablerProperties)manager.GetPermission((Right)Thinkage.MainBoss.Database.Root.Rights.ViewCost.FindDirectChild(p)));
			}
			return perms.ToArray();
		}
	}
}