using System;
using System.Web.SessionState;
using Thinkage.Libraries;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Layout;

namespace Thinkage.MainBoss.Database {
	public static class CommonExpressions {
		public static readonly TypeInfo DateTypeInfo = dsMB.Schema.T.WorkOrder.F.StartDateEstimate.EffectiveType.UnionCompatible(NullTypeInfo.Universe);
		public static readonly TypeInfo RequiredDateTypeInfo = dsMB.Schema.T.WorkOrder.F.StartDateEstimate.EffectiveType.IntersectCompatible(ObjectTypeInfo.NonNullUniverse);
		// The following time span expressions have an epsilon appropriate for their usage.
		// The standard epsilon for a Constant is null (as the value is exact) and this often results in null epsilons in the result type of expressions
		// where they are used. This is a flaw in SqlExpression's or TypeInfo.UnionCompatible's logic: If one operand is a singleton value and the value itself
		// is a member of the other type, then the result type should be the other type.
		public static readonly SqlExpression ZeroDayTimeSpan = SqlExpression.Constant(TimeSpan.Zero, new IntervalTypeInfo(new TimeSpan(1, 0, 0, 0), TimeSpan.Zero, TimeSpan.Zero, false));
		public static readonly SqlExpression OneDayTimeSpan = SqlExpression.Constant(Extensions.TimeSpan.OneDay, new IntervalTypeInfo(new TimeSpan(1, 0, 0, 0), Extensions.TimeSpan.OneDay, Extensions.TimeSpan.OneDay, false));
		public static readonly SqlExpression Today = SqlExpression.Now(RequiredDateTypeInfo);

		// Note that the following two functions misbehave if any of the 'others' is nullable in the sense that
		// the null value will be treated as being lesser or greater than all non-null values.
		// To counter this, we could as a .Or(other.IsNull()) to the condition if other is nullable so that 'first'
		// (the extreme value so far) is preferred over a null.
		public static SqlExpression LesserOf(SqlExpression first, params SqlExpression[] others) {
			foreach (SqlExpression other in others)
				first = SqlExpression.Select(first.Lt(other), first, other);
			return first;
		}
		public static SqlExpression GreaterOf(SqlExpression first, params SqlExpression[] others) {
			foreach (SqlExpression other in others)
				first = SqlExpression.Select(first.Gt(other), first, other);
			return first;
		}
		#region Early/Late from Delay
		// Convert a signed Delay into Early/Late.
		// These return null if the value is not valid or if it is not Early/Late (so exactly on-time returns null in either case)
		public static SqlExpression Early(SqlExpression delay, SqlExpression validity) {
			return SqlExpression.Select(delay.Lt(CommonExpressions.ZeroDayTimeSpan).And(validity), delay.Negate());
		}
		public static SqlExpression Late(SqlExpression delay, SqlExpression validity) {
			return SqlExpression.Select(delay.Gt(CommonExpressions.ZeroDayTimeSpan).And(validity), delay);
		}
		#endregion
		#region expression result type limit and epsilon correction
		/// <summary>
		/// Correct the range of an expression back to what is acceptable to Sql. Additions and subtractions widen the range of values
		/// and calling this function wraps the expression in the appropriate cast to make it acceptable to Sql types again.
		/// </summary>
		/// <param name="e">The expression with possible too-wide range limits</param>
		/// <returns>the properly range-limited expression</returns>
		public static SqlExpression FixRange(SqlExpression e) {
			TypeInfo currentType = e.ResultType;
			TypeInfo newType;
			if (currentType is DateTimeTypeInfo)
				newType = currentType.IntersectCompatible(dsMB.Schema.Types["DateTime"].Type);
			else if (currentType is IntervalTypeInfo iti)
				newType = currentType.IntersectCompatible(dsMB.Schema.Types["DurationFine"].Type);
			else
				return e;
			if (currentType.SubtypeOf(newType))
				return e;
			return e.Cast(newType);
		}
		#endregion
		public static SqlExpression RequestStateHistoryDuration(dsMB.PathClass.PathToRequestStateHistoryRow.FAccessor RSH) {
			return CommonExpressions.FixRange(SqlExpression.Coalesce(new SqlExpression(RSH.Id.L.RequestStateHistory.PreviousRequestStateHistoryID.F.EffectiveDate),
										SqlExpression.Now(dsMB.Schema.T.RequestStateHistory.F.EffectiveDate.EffectiveType)).Minus(new SqlExpression(RSH.EffectiveDate)));
		}
		public static SqlExpression WorkOrderStateHistoryDuration(dsMB.PathClass.PathToWorkOrderStateHistoryRow.FAccessor WOSH) {
			return CommonExpressions.FixRange(SqlExpression.Coalesce(new SqlExpression(WOSH.Id.L.WorkOrderStateHistory.PreviousWorkOrderStateHistoryID.F.EffectiveDate),
										SqlExpression.Now(dsMB.Schema.T.WorkOrderStateHistory.F.EffectiveDate.EffectiveType)).Minus(new SqlExpression(WOSH.EffectiveDate)));
		}
		public static SqlExpression PurchaseOrderStateHistoryDuration(dsMB.PathClass.PathToPurchaseOrderStateHistoryRow.FAccessor POSH) {
			return CommonExpressions.FixRange(SqlExpression.Coalesce(new SqlExpression(POSH.Id.L.PurchaseOrderStateHistory.PreviousPurchaseOrderStateHistoryID.F.EffectiveDate),
										SqlExpression.Now(dsMB.Schema.T.PurchaseOrderStateHistory.F.EffectiveDate.EffectiveType)).Minus(new SqlExpression(POSH.EffectiveDate)));
		}
	}
	#region Work Order Statistical Date/Time calculations
	public static class WOStatisticCalculation {
		public static SqlExpression IsIncompleteOverdueWorkOrder(dsMB.PathClass.PathToWorkOrderRow WO) {
			return IsCompleted(WO).Not().And(ExpectedEndDelay(WO).Gt(CommonExpressions.ZeroDayTimeSpan));
		}
		// Values are split into:
		// "Expected" (for when the work is not Completed/Started/etc depending on context) Note that the "expected" value can be somewhat bogus once the
		// work is completed/Started/etc. Perhaps there should be another family "OnlyExpected" which tests the appropriate IsXxx condition to return null if a firm value exists.
		// The WO reports might benefit from this because they do some HideIf stuff that might be better done if they just had a null value from the query.
		// "Actual" (for when the work is Completed/Started/etc) Note these return null if the work is not completed/started/etc
		// "Merged" which selects either Actual or Expected as appropriate
		#region WO creation (first State History)
		// Since a WO is always "Created" this is all three of Expected, Actual, and Merged
		// Creation delay occurs when the WorkStartEstimate predates the day the wo was created
		public static SqlExpression CreatedDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(new SqlExpression(WO.F.FirstWorkOrderStateHistoryID.F.EffectiveDate).Minus(new SqlExpression(WO.F.StartDateEstimate)));
		}
		#endregion
		#region Work start (First Open)
		public static SqlExpression IsStarted(dsMB.PathClass.PathToWorkOrderRow WO) {
			return new SqlExpression(WO.F.FirstOpenWorkOrderStateHistoryID).IsNotNull();
		}
		#region - Opened (Started) Date
		public static SqlExpression ExpectedOpenDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.GreaterOf(new SqlExpression(WO.F.StartDateEstimate), CommonExpressions.Today);
		}
		public static SqlExpression OnlyExpectedOpenDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Select(IsStarted(WO).Not(), ExpectedOpenDate(WO));
		}
		public static SqlExpression ActualOpenDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return new SqlExpression(WO.F.FirstOpenWorkOrderStateHistoryID.F.EffectiveDate).Cast(CommonExpressions.DateTypeInfo);
		}
		public static SqlExpression MergedOpenDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Coalesce(ActualOpenDate(WO), ExpectedOpenDate(WO));
		}
		#endregion
		#region - Opening Delay
		public static SqlExpression ExpectedOpeningDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(CommonExpressions.FixRange(ExpectedOpenDate(WO).Minus(new SqlExpression(WO.F.StartDateEstimate))));
		}
		public static SqlExpression ActualOpeningDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(CommonExpressions.FixRange(ActualOpenDate(WO).Minus(new SqlExpression(WO.F.StartDateEstimate))));
		}
		public static SqlExpression MergedOpeningDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(CommonExpressions.FixRange(MergedOpenDate(WO).Minus(new SqlExpression(WO.F.StartDateEstimate))));
		}
		#endregion
		#region - Open Early/Late
		public static SqlExpression ActualStartEarly(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Early(ActualOpeningDelay(WO), IsStarted(WO));
		}
		public static SqlExpression ExpectedStartEarly(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Early(ExpectedOpeningDelay(WO), IsStarted(WO).Not());
		}
		public static SqlExpression ActualStartLate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Late(ActualOpeningDelay(WO), IsStarted(WO));
		}
		public static SqlExpression ExpectedStartLate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Late(ExpectedOpeningDelay(WO), IsStarted(WO).Not());
		}
		#endregion
		#endregion
		#region Work end/completion (first Close after last Draft)
		public static SqlExpression IsCompleted(dsMB.PathClass.PathToWorkOrderRow WO) {
			return new SqlExpression(WO.F.CompletionWorkOrderStateHistoryID).IsNotNull();
		}
		#region - Completion Date
		public static SqlExpression DaysWorkedToDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(new SqlExpression(WO.F.PreviousWorkedDays)
				.Plus(SqlExpression.Select(new SqlExpression(WO.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen)
					, CommonExpressions.GreaterOf(CommonExpressions.Today.Minus(CommonExpressions.GreaterOf(new SqlExpression(WO.F.CurrentWorkOrderStateHistoryID.F.EffectiveDate).Cast(CommonExpressions.RequiredDateTypeInfo), new SqlExpression(WO.F.StartDateEstimate))).Plus(CommonExpressions.OneDayTimeSpan), CommonExpressions.ZeroDayTimeSpan)
					, CommonExpressions.ZeroDayTimeSpan)));
		}
		public static SqlExpression AvailableWorkDaysToDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(CommonExpressions.GreaterOf(CommonExpressions.Today.Minus(new SqlExpression(WO.F.StartDateEstimate).Plus(CommonExpressions.OneDayTimeSpan)), CommonExpressions.ZeroDayTimeSpan));
		}
		public static SqlExpression RemainingWorkDays(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(CommonExpressions.GreaterOf(new SqlExpression(WO.F.EndDateEstimate).Minus(new SqlExpression(WO.F.StartDateEstimate)).Plus(CommonExpressions.OneDayTimeSpan).Minus(DaysWorkedToDate(WO)), CommonExpressions.ZeroDayTimeSpan));
		}
		public static SqlExpression ExpectedCompletedDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(CommonExpressions.Today.Plus(RemainingWorkDays(WO)).Minus(CommonExpressions.OneDayTimeSpan));
		}
		public static SqlExpression OnlyExpectedCompletedDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Select(IsCompleted(WO).Not(), ExpectedCompletedDate(WO));
		}
		public static SqlExpression ActualCompletedDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return new SqlExpression(WO.F.CompletionWorkOrderStateHistoryID.F.EffectiveDate).Cast(CommonExpressions.DateTypeInfo);
		}
		public static SqlExpression MergedCompletedDate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Select(IsCompleted(WO), ActualCompletedDate(WO), ExpectedCompletedDate(WO));
		}
		#endregion
		#region - Completion Delay and Overdue
		public static SqlExpression ExpectedEndDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(ExpectedCompletedDate(WO).Minus(new SqlExpression(WO.F.WorkDueDate)));
		}
		public static SqlExpression ActualEndDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(ActualCompletedDate(WO).Minus(new SqlExpression(WO.F.WorkDueDate)));
		}
		public static SqlExpression MergedEndDelay(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(MergedCompletedDate(WO).Minus(new SqlExpression(WO.F.WorkDueDate)));
		}
		// Overdue is similar to Late Completion except that zero is returned rather the null if it is not Late completing.
		public static SqlExpression ExpectedOverdue(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.GreaterOf(ExpectedEndDelay(WO), CommonExpressions.ZeroDayTimeSpan);
		}
		public static SqlExpression ActualOverdue(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.GreaterOf(ActualEndDelay(WO), CommonExpressions.ZeroDayTimeSpan);
		}
		public static SqlExpression MergedOverdue(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.GreaterOf(MergedEndDelay(WO), CommonExpressions.ZeroDayTimeSpan);
		}
		#endregion
		#region - Early/Late completion
		public static SqlExpression ActualEndEarly(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Early(ActualEndDelay(WO), IsCompleted(WO));
		}
		public static SqlExpression ExpectedEndEarly(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Early(ExpectedEndDelay(WO), IsCompleted(WO).Not());
		}
		public static SqlExpression ActualEndLate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Late(ActualEndDelay(WO), IsCompleted(WO));
		}
		public static SqlExpression ExpectedEndLate(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.Late(ExpectedEndDelay(WO), IsCompleted(WO).Not());
		}
		#endregion
		#endregion
		#region Duration (completion - Start + 1)
		#region - Duration itself
		public static SqlExpression PlannedDuration(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(new SqlExpression(WO.F.EndDateEstimate).Minus(new SqlExpression(WO.F.StartDateEstimate)).Plus(CommonExpressions.OneDayTimeSpan));
		}
		public static SqlExpression ExpectedDuration(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(ExpectedCompletedDate(WO).Minus(MergedOpenDate(WO)).Plus(CommonExpressions.OneDayTimeSpan));
		}
		public static SqlExpression OnlyExpectedDuration(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Select(IsCompleted(WO).Not(), ExpectedDuration(WO));
		}
		public static SqlExpression ActualDuration(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(ActualCompletedDate(WO).Minus(ActualOpenDate(WO)).Plus(CommonExpressions.OneDayTimeSpan));
		}
		public static SqlExpression MergedDuration(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Coalesce(ActualDuration(WO), ExpectedDuration(WO));
		}
		#endregion
		#region - Duration Variance
		// Rather than the separate Early and Late we have to Start and Completion, we just have a net variance. Positive values indicate greater than planned.
		public static SqlExpression ExpectedDurationVariance(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(PlannedDuration(WO).Minus(ExpectedDuration(WO)));
		}
		public static SqlExpression OnlyExpectedDurationVariance(dsMB.PathClass.PathToWorkOrderRow WO) {
			return SqlExpression.Select(IsCompleted(WO).Not(), ExpectedDurationVariance(WO));
		}
		public static SqlExpression ActualDurationVariance(dsMB.PathClass.PathToWorkOrderRow WO) {
			return CommonExpressions.FixRange(PlannedDuration(WO).Minus(ActualDuration(WO)));
		}
		#endregion
		#endregion
	}
	#endregion
}
