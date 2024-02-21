using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary;

namespace Thinkage.MainBoss.Database {
	public class CommonExpressions {
		private CommonExpressions() {
		}
		public static SqlExpression OverdueWorkOrderExpression = new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen).IsTrue()
							.And(WOStatisticCalculation.Overdue(dsMB.Path.T.WorkOrder, dsMB.Path.T.WorkOrder.F.Id.L.WorkOrderExtras.WorkOrderID).IsNotNull());

		public static SqlExpression IntervalSumSqlExpression(SqlExpression term1, SqlExpression term2) {
			// The cast is required because the addition doubles the range of the type (max - min) and so the uncast sum
			// has too large a max and too small a min to be representable.
			return term2.Plus(term1).Cast((Libraries.TypeInfo.IntervalTypeInfo)dsMB.Schema.Types["DurationFine"].Type);
		}
		/// <summary>
		/// Return the expression for minuend - subtrahend cast to be SQL-compatible
		/// </summary>
		public static SqlExpression IntervalDifferenceSqlExpression(SqlExpression subtrahend, SqlExpression minuend) {
			// TODO: Why is this cast here? The epsilon of the type returned by Minus is the finer of that of the two
			// operands. Part of the problem is that WorkOrderReport contains several fields called XxxxxDate but whose type
			// is to the 100th of a second, and the expression result is expected to be in days.
			// If the types of those values could be changed, most (if not all) callers to this method could just call Minus.
			// Answer: Actually the cast is required because the subtraction doubles the range of the type (max - min) and so the uncast difference
			// has too large a max and too small a min to be representable.
			return minuend.Minus(subtrahend).Cast((Libraries.TypeInfo.IntervalTypeInfo)dsMB.Schema.Types["DurationFine"].Type);
		}
		public static SqlExpression RequestStateHistoryDuration(dsMB.PathClass.PathToRequestStateHistoryRow.FAccessor RSH) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(RSH.EffectiveDate), SqlExpression.Coalesce(new SqlExpression(RSH.Id.L.RequestStateHistoryReport.PreviousRequestStateHistoryID.F.RequestStateHistoryID.F.EffectiveDate),
										SqlExpression.Now(dsMB.Schema.T.RequestStateHistory.F.EffectiveDate.EffectiveType)));
		}
		public static SqlExpression WorkOrderStateHistoryDuration(dsMB.PathClass.PathToWorkOrderStateHistoryRow.FAccessor WOSH) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WOSH.EffectiveDate), SqlExpression.Coalesce(new SqlExpression(WOSH.Id.L.WorkOrderStateHistoryReport.PreviousWorkOrderStateHistoryID.F.WorkOrderStateHistoryID.F.EffectiveDate),
										SqlExpression.Now(dsMB.Schema.T.WorkOrderStateHistory.F.EffectiveDate.EffectiveType)));
		}
		public static SqlExpression PurchaseOrderStateHistoryDuration(dsMB.PathClass.PathToPurchaseOrderStateHistoryRow.FAccessor POSH) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(POSH.EffectiveDate), SqlExpression.Coalesce(new SqlExpression(POSH.Id.L.PurchaseOrderStateHistoryReport.PreviousPurchaseOrderStateHistoryID.F.PurchaseOrderStateHistoryID.F.EffectiveDate),
										SqlExpression.Now(dsMB.Schema.T.PurchaseOrderStateHistory.F.EffectiveDate.EffectiveType)));
		}
	}
	#region Work Order Statistical Date/Time calculations
	public class WOStatisticCalculation {
		private WOStatisticCalculation() {
		}
		private static SqlExpression OneDayTimeSpan = SqlExpression.Constant(Extensions.TimeSpan.OneDay);
		#region EndingDelay and related expressions

		public static SqlExpression EndEarly(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var endingDelayExpression = EndingDelay(WO, WOR);
			return SqlExpression.Select(endingDelayExpression.LEq(SqlExpression.Constant(Extensions.TimeSpan.NegativeOneDay)).And(new SqlExpression(WOR.F.EndedDateIfEnded).IsNotNull()), WOStatisticCalculation.NegatedEndingDelay(WO, WOR), SqlExpression.Constant(null));
		}
		public static SqlExpression EndEarlyEstimate(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var endingDelayExpression = EndingDelay(WO, WOR);
			return SqlExpression.Select(endingDelayExpression.LEq(SqlExpression.Constant(Extensions.TimeSpan.NegativeOneDay)).And(new SqlExpression(WOR.F.EndedDateIfEnded).IsNull()), WOStatisticCalculation.NegatedEndingDelay(WO, WOR), SqlExpression.Constant(null));
		}
		public static SqlExpression EndLate(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var endingDelayExpression = EndingDelay(WO, WOR);
			return SqlExpression.Select(endingDelayExpression.GEq(SqlExpression.Constant(Extensions.TimeSpan.OneDay)).And(new SqlExpression(WOR.F.EndedDateIfEnded).IsNotNull()), endingDelayExpression, SqlExpression.Constant(null));
		}
		public static SqlExpression EndLateEstimate(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var endingDelayExpression = EndingDelay(WO, WOR);
			return SqlExpression.Select(endingDelayExpression.GEq(SqlExpression.Constant(Extensions.TimeSpan.OneDay)).And(new SqlExpression(WOR.F.EndedDateIfEnded).IsNull()), endingDelayExpression, SqlExpression.Constant(null));
		}
		/// <summary>
		/// Difference between EarliestEndDate and the original EndDateEstimate in the workorder
		/// </summary>
		public static SqlExpression EndingDelay(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WO.F.EndDateEstimate), new SqlExpression(WOR.F.EarliestEndDate));
		}
		public static SqlExpression NegatedEndingDelay(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WOR.F.EarliestEndDate), new SqlExpression(WO.F.EndDateEstimate));
		}
		// A common definition of overdue on a work order
		public static SqlExpression Overdue(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var endingDelayExpression = WOStatisticCalculation.EndingDelay(WO, WOR).Cast((Libraries.TypeInfo.IntervalTypeInfo)dsMB.Schema.Types["DurationDays"].Type);
			return SqlExpression.Select(endingDelayExpression.Lt(SqlExpression.Constant(Extensions.TimeSpan.OneDay)).Or(new SqlExpression(WOR.F.EndedDateIfEnded).IsNotNull()), SqlExpression.Constant(null), endingDelayExpression);
		}
		#endregion
		#region OpeningDelay and related expressions
		public static SqlExpression StartEarly(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var openingDelayExpression = OpeningDelay(WO, WOR);
			return SqlExpression.Select(openingDelayExpression.LEq(SqlExpression.Constant(Extensions.TimeSpan.NegativeOneDay)).And(new SqlExpression(WOR.F.FirstOpenedDate).IsNotNull()), WOStatisticCalculation.NegatedOpeningDelay(WO, WOR), SqlExpression.Constant(null));
		}
		public static SqlExpression StartEarlyEstimate(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var openingDelayExpression = OpeningDelay(WO, WOR);
			return SqlExpression.Select(openingDelayExpression.LEq(SqlExpression.Constant(Extensions.TimeSpan.NegativeOneDay)).And(new SqlExpression(WOR.F.FirstOpenedDate).IsNull()), WOStatisticCalculation.NegatedOpeningDelay(WO, WOR), SqlExpression.Constant(null));
		}
		public static SqlExpression StartLate(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var openingDelayExpression = OpeningDelay(WO, WOR);
			return SqlExpression.Select(openingDelayExpression.GEq(SqlExpression.Constant(Extensions.TimeSpan.OneDay)).And(new SqlExpression(WOR.F.FirstOpenedDate).IsNotNull()), openingDelayExpression, SqlExpression.Constant(null));
		}
		public static SqlExpression StartLateEstimate(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			var openingDelayExpression = OpeningDelay(WO, WOR);
			return SqlExpression.Select(openingDelayExpression.GEq(SqlExpression.Constant(Extensions.TimeSpan.OneDay)).And(new SqlExpression(WOR.F.FirstOpenedDate).IsNull()), openingDelayExpression, SqlExpression.Constant(null));
		}
		/// <summary>
		/// Difference between EarliestOpenDate and original StartDateEstimate in the workorder
		/// </summary>
		public static SqlExpression OpeningDelay(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WO.F.StartDateEstimate), new SqlExpression(WOR.F.EarliestOpenDate));
		}
		public static SqlExpression NegatedOpeningDelay(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WOR.F.EarliestOpenDate), new SqlExpression(WO.F.StartDateEstimate));
		}
		public static SqlExpression CreatedDelay(dsMB.PathClass.PathToWorkOrderRow WO, dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WO.F.StartDateEstimate), new SqlExpression(WOR.F.CreatedDate));
		}
		#endregion
		#region Duration
		/// <summary>
		/// WorkOrder.EndDateEstimate - WorkOrder.StartDateEstimate + OneDay
		/// </summary>
		public static SqlExpression EstimatedDuration(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.IntervalSumSqlExpression(new SqlExpression(WO.F.EndDateEstimate).Minus(new SqlExpression(WO.F.StartDateEstimate)), OneDayTimeSpan);
		}
		public static SqlExpression MinimumDuration(dsMB.PathClass.PathToWorkOrderExtrasRow WOXR) {
			return CommonExpressions.IntervalSumSqlExpression(new SqlExpression(WOXR.F.EarliestEndDate).Minus(new SqlExpression(WOXR.F.EarliestOpenDate)), OneDayTimeSpan);
		}
		public static SqlExpression ActualDuration(dsMB.PathClass.PathToWorkOrderExtrasRow WOXR) {
			return CommonExpressions.IntervalSumSqlExpression(new SqlExpression(WOXR.F.EndedDateIfEnded).Minus(new SqlExpression(WOXR.F.FirstOpenedDate)), OneDayTimeSpan);
		}
		#endregion
		#region Lifetime
		/// <summary>
		/// WorkOrderExtras.EndedDate - WorkOrderExtras.CreatedDate
		/// </summary>
		public static SqlExpression Lifetime(dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WOR.F.CreatedDate), new SqlExpression(WOR.F.EndedDateIfEnded));
		}
		/// <summary>
		/// WorkOrderExtras.EarliestEndDate - WorkOrderExtras.CreatedDate
		/// </summary>
		public static SqlExpression MinimumLifetime(dsMB.PathClass.PathToWorkOrderExtrasRow WOR) {
			return CommonExpressions.IntervalDifferenceSqlExpression(new SqlExpression(WOR.F.CreatedDate), new SqlExpression(WOR.F.EarliestEndDate));
		}
		#endregion
	}
	#endregion
}
