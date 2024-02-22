using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.RDL2016;
using Thinkage.Libraries.RDLReports;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls.Reports {
	#region Request Charts
	#region - Request Timeline charts
	public class RequestChartLifetime : GenericChartReport {
		public RequestChartLifetime(Report r, ReportViewLogic logic)
			: base(r, logic, dsMB.Path.T.Request.F.Number) {
		}
		protected override void MakeChartTemplates() {
			MakeTimelineChart(DateTimeField(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CreatedDate), TId.Request.TranslationKey, KB.TOi(TId.RequestState),
				new TimelineStage(labelExpr: StateContext.NewCode, durationExpr: TimeSpanField(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.InNewDuration)),
				new TimelineStage(labelExpr: StateContext.InProgressCode, durationExpr: TimeSpanField(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.InProgressDuration))
			);
		}
	}
	#endregion
	#region - Request Count charts
	/// <summary>
	/// Chart a count of work orders using a user-selected or ctor-passed (groupingPath) grouping.
	/// </summary>
	public class RequestChartCountBase : GenericChartReport {
		public RequestChartCountBase(Report r, ReportViewLogic logic, DBI_Path groupingPath)
			: base(r, logic, dsMB.Path.T.Request.F.Id) {
			GroupingPath = groupingPath;
		}
		private readonly DBI_Path GroupingPath;
		protected override void MakeChartTemplates() {
			// TODO: Make the class selector be more or less the logarithm of the number of linked work orders, so that, say, the groups are:
			// None
			// 1
			// 2-3
			// 4-7
			// 8-15
			// 16-31
			// etc.
			// "more or less" because log(0) is undefined.
			// The grouping expression would be something like (count == 0) ? -1 : floor(log2(count))
			// and the group label expression would be something like (val == -1) ? "None" : (val == 0) ? 1 : exp2(val) + " - " + (exp2(val+1)-1) if starting from the grouping value,
			//                          or (count == 0) ? "None" : (count == 1) ? 1 : exp2(floor(log2(count))) + " - " + (exp2(floor(log2(count))+1)-1 if starting from the original count.
			MakeBarChart(null, Expression.Function.Count, TabularReport.MakeSearchValueFromPath(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders), GroupingPath == null ? null : TabularReport.MakeSearchValueFromPath(GroupingPath));
		}
	}
	#endregion
	#region - Request Time-span charts
	/// <summary>
	/// Chart the aggregate of a duration related to a Work Order using user-selected grouping.
	/// </summary>
	public class RequestChartDurationBase : GenericChartReport {
		public RequestChartDurationBase(Report r, ReportViewLogic logic, TblLeafNode yValueNode, Expression.Function aggregateFunction)
			: base(r, logic, dsMB.Path.T.Request.F.Id) {
			YValueNode = yValueNode;
			AggregateFunction = aggregateFunction;
		}
		private readonly TblLeafNode YValueNode;
		private readonly Expression.Function AggregateFunction;
		protected override void MakeChartTemplates() {
			// See the note in RequestChartCountBase about classifying by number of linked work orders.
			MakeBarChart(YValueNode, AggregateFunction, TabularReport.MakeSearchValueFromPath(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders));
		}
	}
	#endregion
	#endregion
	#region RequestStateHistory Charts
	public class RequestChartStatusReport : GenericChartReport {
		public RequestChartStatusReport(Report r, ReportViewLogic logic)
			: base(r, logic, dsMB.Path.T.RequestStateHistory.F.RequestID) {
		}
		protected override void MakeChartTemplates() {
			var endTimePath = dsMB.Path.T.RequestStateHistory.F.Id.L.RequestStateHistory.PreviousRequestStateHistoryID.F.EffectiveDate;
			MakeStatusChart(TabularReport.MakeSearchValueFromPath(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID), TIReports.IntervalDifferenceQueryExpression(new SqlExpression(dsMB.Path.T.RequestStateHistory.F.EffectiveDate), SqlExpression.Coalesce(new SqlExpression(endTimePath), SqlExpression.Now(endTimePath.ReferencedColumn.EffectiveType))));
		}
	}
	#endregion
}
