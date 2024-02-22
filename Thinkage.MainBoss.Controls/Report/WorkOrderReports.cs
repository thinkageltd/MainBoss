using System;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.RDL2016;
using Thinkage.Libraries.RDLReports;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries;
using System.Collections.Generic;

namespace Thinkage.MainBoss.Controls.Reports {
	#region Work Order Charts
	#region - Work Order Timeline charts
	public class WOChartLifetime : GenericChartReport {
		public WOChartLifetime(Report r, ReportViewLogic logic)
			: base(r, logic, dsMB.Path.T.WorkOrder.F.Number) {
		}
		protected override void MakeChartTemplates() {
			var opened = DateTimeField(new TblQueryExpression(WOStatisticCalculation.MergedOpenDate(dsMB.Path.T.WorkOrder)));
			var ended = DateTimeField(new TblQueryExpression(WOStatisticCalculation.MergedCompletedDate(dsMB.Path.T.WorkOrder)));
			var actualOpenDate = DateTimeField(dsMB.Path.T.WorkOrder.F.FirstOpenWorkOrderStateHistoryID.F.EffectiveDate);
			var actualEndedDate = DateTimeField(dsMB.Path.T.WorkOrder.F.CompletionWorkOrderStateHistoryID.F.EffectiveDate);
			var openingDelay = TimeSpanField(new TblQueryExpression(WOStatisticCalculation.MergedOpeningDelay(dsMB.Path.T.WorkOrder)));
			var endingDelay = TimeSpanField(new TblQueryExpression(WOStatisticCalculation.MergedEndDelay(dsMB.Path.T.WorkOrder)));

			var createdDelay = TimeSpanField(new TblQueryExpression(WOStatisticCalculation.CreatedDelay(dsMB.Path.T.WorkOrder)));
			var createdLate = (createdDelay > TimeSpan.Zero).Query(createdDelay, TimeSpan.Zero);
			var createDateRevised = DateTimeField(new TblQueryExpression(
				SqlExpression.Select(new SqlExpression(dsMB.Path.T.WorkOrder.F.FirstWorkOrderStateHistoryID.F.EffectiveDate).Gt(new SqlExpression(dsMB.Path.T.WorkOrder.F.StartDateEstimate)),
										new SqlExpression(dsMB.Path.T.WorkOrder.F.StartDateEstimate), new SqlExpression(dsMB.Path.T.WorkOrder.F.FirstWorkOrderStateHistoryID.F.EffectiveDate))));

			var startEarly = (openingDelay < TimeSpan.Zero & actualOpenDate.IsNotNull).Query(-openingDelay, TimeSpan.Zero);
			var startLate = (openingDelay > TimeSpan.Zero).Query(openingDelay, TimeSpan.Zero);


			var endEarly = (endingDelay < TimeSpan.Zero).Query(-endingDelay, TimeSpan.Zero);
			// the following are mutually exclusive with same value based on whether WO has actually ended. Used to have different label/color for bar
			var endLate = (endingDelay > TimeSpan.Zero & actualEndedDate.IsNotNull).Query(endingDelay, TimeSpan.Zero); // Only show overdue for workorders that have actually ended

			// Only show overdue for workorders that have actually not ended but have actually been opened, or have never been opened (i.e. still in draft; the overdue part will be the estimated work duration yet to be done)
			var overdue = (endingDelay > TimeSpan.Zero & actualEndedDate.IsNull & actualOpenDate.IsNotNull).Query(endingDelay - startLate + createdLate, TimeSpan.Zero);

			var draftDuration = (opened - DateTimeField(dsMB.Path.T.WorkOrder.F.FirstWorkOrderStateHistoryID.F.EffectiveDate)) - startLate;
			var openDuration = (actualOpenDate.IsNotNull).Query((ended - opened) - startEarly - startLate - endEarly - endLate - overdue, TimeSpan.Zero);

			MakeTimelineChart(createDateRevised, TId.WorkOrder.TranslationKey, KB.TOi(TId.WorkOrderState),
				new TimelineStage(labelExpr: StateContext.DraftCode, durationExpr: draftDuration, color: System.Drawing.Color.Green),
				new TimelineStage(labelExpr: StateContext.EarlyCode, durationExpr: startEarly, color: System.Drawing.Color.LightGreen),
				new TimelineStage(labelExpr: StateContext.OpenCode, durationExpr: openDuration, color: System.Drawing.Color.Blue),
				new TimelineStage(labelExpr: StateContext.LateCode, durationExpr: startLate, color: System.Drawing.Color.HotPink),
				new TimelineStage(labelExpr: StateContext.EndEarlyCode, durationExpr: endEarly, color: System.Drawing.Color.Purple),
				new TimelineStage(labelExpr: StateContext.EndLateCode, durationExpr: endLate, color: System.Drawing.Color.Yellow),
				new TimelineStage(labelExpr: StateContext.OverdueCode, durationExpr: overdue, color: System.Drawing.Color.Red)
			);
		}
	}
	#endregion
	#region - Work Order Count charts
	/// <summary>
	/// Chart a count of work orders using a user-selected or ctor-passed (groupingPath) grouping.
	/// </summary>
	public class WOChartCountBase : GenericChartReport {
		public WOChartCountBase(Report r, ReportViewLogic logic, DBI_Path groupingPath)
			: base(r, logic, dsMB.Path.T.WorkOrder.F.Id) {
			if (groupingPath != null)
				GroupingValue = TabularReport.MakeSearchValueFromPath(groupingPath);
		}
		public WOChartCountBase(Report r, ReportViewLogic logic, TblLeafNode groupingNode)
			: base(r, logic, dsMB.Path.T.WorkOrder.F.Id) {
			GroupingValue = TabularReport.MakeSearchValueFromNode(groupingNode);
		}
		private SearchValueWithData<InteremCommonReportViewLogic.GroupingValueExtraData> GroupingValue;
		protected override void MakeChartTemplates() {
			MakeBarChart(null, Expression.Function.Count,
				TabularReport.MakeSearchValueFromNode(TIWorkOrder.IsPreventiveValueNodeBuilder(dsMB.Path.T.WorkOrder)), GroupingValue);
		}
	}
	#endregion
	#region - Work Order Time-span charts
	/// <summary>
	/// Chart the aggregate of a duration related to a Work Order using user-selected grouping.
	/// </summary>
	public class WOChartDurationBase : GenericChartReport {
		public WOChartDurationBase(Report r, ReportViewLogic logic, TblLeafNode yValueNode, Expression.Function aggregateFunction)
			: base(r, logic, dsMB.Path.T.WorkOrder.F.Id) {
			YValueNode = yValueNode;
			AggregateFunction = aggregateFunction;
		}
		private readonly TblLeafNode YValueNode;
		private readonly Expression.Function AggregateFunction;
		protected override void MakeChartTemplates() {
			MakeBarChart(YValueNode, AggregateFunction,
				TabularReport.MakeSearchValueFromNode(TIWorkOrder.IsPreventiveValueNodeBuilder(dsMB.Path.T.WorkOrder)));
		}
	}
	public class WOChartSelectedDurationBase : GenericChartReport {
		public WOChartSelectedDurationBase(Report r, ReportViewLogic logic, Expression.Function aggregateFunction)
			: base(r, logic, dsMB.Path.T.WorkOrder.F.Id) {
			AggregateFunction = aggregateFunction;
		}
		private readonly Expression.Function AggregateFunction;
		protected override void MakeChartTemplates() {
			// TODO: Find the Field selection control and see what fields the user wants.
			var valuesToShow = new List<TblLeafNode>();
			ShowParameters.Nodes.TraverseLayoutNodes(delegate (TblLayoutNode n) {
				return true;
			}, delegate (TblLayoutNode f) {
				if (!(f is TblLeafNode leafNode))
					return;

				if (ShowParameters.UltimateShowItem(leafNode)) {
					valuesToShow.Add(leafNode);
				}
			});
			if (valuesToShow.Count == 0)
				throw new GeneralException(KB.K("A value must be selected for charting"));

			if (valuesToShow.Count == 1)
				// If only one value is selected we call MakeBarChart and the user will get Corrective/Preventative separation
				MakeBarChart(valuesToShow[0], AggregateFunction, TabularReport.MakeSearchValueFromNode(TIWorkOrder.IsPreventiveValueNodeBuilder(dsMB.Path.T.WorkOrder)));
			else {
				// Otherwise we make our own multi-value chart.
				Key horizonalAxisDescription = Expression.FunctionPhrase(AggregateFunction, KB.K("Duration"));
				StringExpr horizontalAxisLabel = TitleWithIntervalName(horizonalAxisDescription);
				SearchValueWithData<InteremCommonReportViewLogic.GroupingValueExtraData> categoryGrouping = null;
				StandardChart chart = MakeChart(categoryGrouping, horizonalAxisDescription);
				chart.AddLegend();
				StandardChartCategoryAxis category = MakeCategoryAxis(chart, null);
				var noDataCheckFields = new List<Expression>();

				foreach (TblLeafNode yValueNode in valuesToShow) {
					ColumnManager.PreAnalyzeColumn(yValueNode, out InteremCommonReportViewLogic.QueryColumnManager.QueryColumn valueColumn, out _, out _);

					// For time spans we need special code because we want to call ScaleDuration which in turn needs a TimeSpanExpr as an argument.
					Expression yAggregatedExpression = ScaleDuration(Expression.Func<TimeSpanExpr>(AggregateFunction, Statics.TimeSpanField(valueColumn)));
					string yValueFormat = RDLReport.QuantityFormatCode;
					//new StandardChartData(chart, MakeCategoryAxis(chart, null),
					//	chart.AddSeries(horizontalAxisLabel, new GroupingToApply(this, new SortingGrouping(SqlExpression.CustomLeafNode(valueGrouping), SortingGrouping.Orderings.Ascending)), labelFormat: yValueFormat),
					//	ChartType.Bar, ChartSubtype.Bar)
					//	.AddData(Y: yAggregatedExpression);
					new StandardChartData(chart, category, chart.AddSeries(null, horizontalAxisLabel, yValueNode.Label), ChartType.Bar, ChartSubtype.Bar)
						.AddData(Y: yAggregatedExpression);

					noDataCheckFields.Add(Statics.Field(valueColumn));
				}
				NoDataMessage(noDataCheckFields.ToArray());
			}
		}
	}
	#endregion
	#endregion
	#region Work Order Resource Charts
	public class WOResourceChartBase : GenericChartReport {
		public WOResourceChartBase(Report r, ReportViewLogic logic, Key yValueCaption, SqlExpression yDemandValue, SqlExpression yActualValue, TblLeafNode grouping)
			: base(r, logic, dsMB.Path.T.WorkOrderFormReport.F.Id) {
			// We could also just coalesce(actualValue, demandValue) but NOT the other way around because the actuals also show all the demand fields.
			YValueNode = TblServerExprNode.New(yValueCaption, SqlExpression.Select(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.AccountingTransactionID).IsNull(), yDemandValue, yActualValue));
			Grouping = grouping;
		}
		private readonly TblLeafNode Grouping;
		private readonly TblLeafNode YValueNode;
		protected override void MakeChartTemplates() {
			var isDemandNode = TblServerExprNode.New(KB.K("Type"), new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.AccountingTransactionID).IsNull(), Fmt.SetEnumText(TIReports.IsDemandEnumText));
			MakeBarChart(YValueNode, Expression.Function.Sum, TabularReport.MakeSearchValueFromNode(isDemandNode), Grouping == null ? null : TabularReport.MakeSearchValueFromNode(Grouping), neverStackBars: true);
		}
	}
	#endregion
	#region WO Chart Status Report
	public class WOChartStatusReport : GenericChartReport {
		public WOChartStatusReport(Report r, ReportViewLogic logic)
			: base(r, logic, dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID) {
		}
		protected override void MakeChartTemplates() {
			//TODO: if the effectiveDate of any state history is in the future, we will get negative times on the chart. 
			var endTimePath = dsMB.Path.T.WorkOrderStateHistory.F.Id.L.WorkOrderStateHistory.PreviousWorkOrderStateHistoryID.F.EffectiveDate;
			MakeStatusChart(TabularReport.MakeSearchValueFromPath(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID), TIReports.IntervalDifferenceQueryExpression(new SqlExpression(dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate), SqlExpression.Coalesce(new SqlExpression(endTimePath), SqlExpression.Now(endTimePath.ReferencedColumn.EffectiveType))));
		}
	}
	#endregion
}
