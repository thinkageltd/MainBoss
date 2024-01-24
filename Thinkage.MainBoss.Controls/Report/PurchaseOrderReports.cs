using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.RDL2010;
using Thinkage.Libraries.RDLReports;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls.Reports {
	#region PurchaseOrder Charts
	#region -   PurchaseOrder Timeline charts
	public class POChartLifetime : GenericChartReport {
		public POChartLifetime(Report r, ReportViewLogic logic)
			: base(r, logic, dsMB.Path.T.PurchaseOrder.F.Number) {
		}
		protected override void MakeChartTemplates() {
			MakeTimelineChart(DateTimeField(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.CreatedDate), TId.PurchaseOrder.TranslationKey, KB.TOi(TId.PurchaseOrderState),
				new TimelineStage(labelExpr: StateContext.NewCode, durationExpr: TimeSpanField(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.InNewDuration)),
				new TimelineStage(labelExpr: StateContext.IssuedCode, durationExpr: TimeSpanField(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.InIssuedDuration))
			);
		}
	}
	#endregion
	#region -   PurchaseOrder Count charts
	/// <summary>
	/// Chart a count of work orders using a user-selected or ctor-passed (groupingPath) grouping.
	/// </summary>
	public class POChartCountBase : GenericChartReport {
		public POChartCountBase(Report r, ReportViewLogic logic, DBI_Path groupingPath)
			: base(r, logic, dsMB.Path.T.PurchaseOrder.F.Id) {
			GroupingPath = groupingPath;
		}
		private readonly DBI_Path GroupingPath;
		protected override void MakeChartTemplates() {
			// TODO: Perhaps instead of Completed Normally: True/False it should have no legend title and self-descriptive values (Complete/Incomplete ???)
			MakeBarChart(null, Expression.Function.Count, TabularReport.MakeSearchValueFromPath(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.CompletedNormally), GroupingPath == null ? null : TabularReport.MakeSearchValueFromPath(GroupingPath));
		}
	}
	#endregion
	#region -   PurchaseOrder Time-span charts
	/// <summary>
	/// Chart the aggregate of a duration related to a Work Order using user-selected grouping.
	/// </summary>
	public class POChartDurationBase : GenericChartReport {
		public POChartDurationBase(Report r, ReportViewLogic logic, TblLeafNode yValueNode, Expression.Function aggregateFunction)
			: base(r, logic, dsMB.Path.T.PurchaseOrder.F.Id) {
			YValueNode = yValueNode;
			AggregateFunction = aggregateFunction;
		}
		private readonly TblLeafNode YValueNode;
		private readonly Expression.Function AggregateFunction;
		protected override void MakeChartTemplates() {
			MakeBarChart(YValueNode, AggregateFunction, TabularReport.MakeSearchValueFromPath(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.CompletedNormally));
		}
	}
	#endregion
	#region -   PurchaseOrderStateHistory
	public class PurchaseOrderChartStatusReport : GenericChartReport {
		public PurchaseOrderChartStatusReport(Report r, ReportViewLogic logic)
			: base(r, logic, dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID) {
		}
		protected override void MakeChartTemplates() {
			var endTimePath = dsMB.Path.T.PurchaseOrderStateHistory.F.Id.L.PurchaseOrderStateHistoryReport.PreviousPurchaseOrderStateHistoryID.F.PurchaseOrderStateHistoryID.F.EffectiveDate;
			MakeStatusChart(TabularReport.MakeSearchValueFromPath(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateHistoryStatusID), TIReports.IntervalDifferenceQueryExpression(new SqlExpression(dsMB.Path.T.PurchaseOrderStateHistory.F.EffectiveDate), SqlExpression.Coalesce(new SqlExpression(endTimePath), SqlExpression.Now(endTimePath.ReferencedColumn.EffectiveType))));
		}
	}
	#endregion
	#endregion
}
