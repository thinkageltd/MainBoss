using System;
using System.Collections.Generic;
using System.Text;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// Operations on a Quantity (either long or TimeSpan) and/or a Cost (decimal)
	/// In a common class so both on screen calculators and internal computation use the same methods for calculation.
	/// </summary>
	// The Unbox/Box [(Xxx)(object)value] are used to make the compiler stop trying to convert the value to Xxx. Because it is not sure here
	// what type Xxx actually is, it can't generate a convertion. But because we use if (blat is Tttt) and value is of type Ttt and we make sure
	// Tttt always turns out to be the same type as Xxx there is actually no conversion and the box/unbox is enough to prevent the compiler from complaining.
	public class Compute {
		private Compute() {
		}

		public static T Zero<T>() where T : struct {
			return default(T);
		}
		public static T One<T>() where T : struct {
			if (typeof(T) == typeof(long))
				return (T)(object)1L;
			else if (typeof(T) == typeof(TimeSpan))
				return (T)(object)new TimeSpan(1, 0, 0);
			else
				throw new ArgumentException(KB.I("One<T> requires T is long or TimeSpan"), KB.I("T"));
		}
		public static bool IsZero<T>(T? quantity) where T : struct, IComparable<T> {
			return Equal<T>(quantity, Zero<T>());
		}
		public static bool Equal<T>(T? q1, T? q2) where T : struct, IComparable<T> {
			return Compare<T>(q1, q2) == 0;
		}
		public static bool Less<T>(T? q1, T? q2) where T : struct, IComparable<T> {
			return Compare<T>(q1, q2) < 0;
		}
		public static bool LessEqual<T>(T? q1, T? q2) where T : struct, IComparable<T> {
			return Compare<T>(q1, q2) <= 0;
		}
		public static bool Greater<T>(T? q1, T? q2) where T : struct, IComparable<T> {
			return Compare<T>(q1, q2) > 0;
		}
		public static bool GreaterEqual<T>(T? q1, T? q2) where T : struct, IComparable<T> {
			return Compare<T>(q1, q2) >= 0;
		}
		public static int Compare<T>(T? q1, T? q2) where T : struct, IComparable<T> {
			if (!q1.HasValue && !q2.HasValue)
				return 0;
			else if (!q1.HasValue)
				return -1;
			else if (!q2.HasValue)
				return 1;
			else
				return q1.Value.CompareTo(q2.Value);
		}
		public static T? Add<T>(T? q1, T? q2) where T : struct {
			if (!q1.HasValue || !q2.HasValue)
				return null;
			return Add<T>(q1.Value, q2.Value);
		}
		public static T Add<T>(T q1, T q2) where T : struct {
			if (q1 is long)
				return checked((T)(object)((long)(object)q1 + (long)(object)q2));
			else if (q1 is decimal)
				return checked((T)(object)((decimal)(object)q1 + (decimal)(object)q2));
			else if (q1 is TimeSpan)
				return checked((T)(object)((TimeSpan)(object)q1 + (TimeSpan)(object)q2));
			else
				throw new ArgumentException(KB.I("Add<T> requires T is long, decimal, or TimeSpan"), KB.I("T"));
		}
		public static T? Subtract<T>(T? q1, T? q2) where T : struct {
			if (!q1.HasValue || !q2.HasValue)
				return null;
			return Subtract<T>(q1.Value, q2.Value);
		}
		public static T Subtract<T>(T q1, T q2) where T : struct {
			if (q1 is long)
				return checked((T)(object)((long)(object)q1 - (long)(object)q2));
			else if (q1 is decimal)
				return checked((T)(object)((decimal)(object)q1 - (decimal)(object)q2));
			else if (q1 is TimeSpan)
				return checked((T)(object)((TimeSpan)(object)q1 - (TimeSpan)(object)q2));
			else
				throw new ArgumentException(KB.I("Subtract<T> requires T is long, decimal, or TimeSpan"), KB.I("T"));
		}
		public static T? Sign<T>(int sign, T? q2) where T : struct {
			if (!q2.HasValue)
				return null;
			return Sign<T>(sign, q2.Value);
		}
		public static T Sign<T>(int sign, T q2) where T : struct {
			if (q2 is long)
				return checked((T)(object)(sign * (long)(object)q2));
			else if (q2 is decimal)
				return checked((T)(object)(sign * (decimal)(object)q2));
			else if (q2 is TimeSpan)
				return checked((T)(object)new TimeSpan(sign * ((TimeSpan)(object)q2).Ticks));
			else
				throw new ArgumentException(KB.I("Sign<T> requires T is long, decimal, or TimeSpan"), KB.I("T"));
		}
		public static decimal? Multiply<T>(decimal? unitcost, T? quantity) where T : struct {
			if (!unitcost.HasValue || !quantity.HasValue)
				return null;
			return Multiply<T>(unitcost.Value, quantity.Value);
		}
		public static decimal Multiply<T>(decimal unitcost, T quantity) where T : struct {
			if (quantity is long)
				return checked(unitcost * (long)(object)quantity);
			else if (quantity is TimeSpan)
				return checked(unitcost * (decimal)((TimeSpan)(object)quantity).TotalHours);
			else
				throw new ArgumentException(KB.I("Multiply<T> requires T is long or TimeSpan"), KB.I("T"));
		}
		public static decimal? Divide<T>(decimal? cost, T? quantity) where T : struct {
			if (!cost.HasValue || !quantity.HasValue)
				return null;
			return Divide<T>(cost.Value, quantity.Value);
		}
		public static decimal Divide<T>(decimal cost, T quantity) where T : struct {
			if (quantity is long)
				return checked(cost / (long)(object)quantity);
			else if (quantity is TimeSpan)
				return checked(cost / (decimal)((TimeSpan)(object)quantity).TotalHours);
			else
				throw new ArgumentException(KB.I("Divide<T> requires T is long or TimeSpan"), KB.I("T"));
		}
		public static T? Divide<T>(decimal? cost, decimal? unitcost) where T : struct {
			if (!cost.HasValue || !unitcost.HasValue)
				return null;
			return Divide<T>(cost.Value, unitcost.Value);
		}
		public static T Divide<T>(decimal cost, decimal unitcost) where T : struct {
			if (typeof(T) == typeof(long))
				return (T)(object)checked((long)(0.5m + cost / unitcost));
			else if (typeof(T) == typeof(TimeSpan))
				return (T)(object)new TimeSpan(checked((long)(cost / unitcost * TimeSpan.TicksPerHour)));
			else
				throw new ArgumentException(KB.I("Divide<T> requires T is long or TimeSpan"), KB.I("T"));
		}
		public static T Remaining<T>(T? quantity, T? used) where T : struct, IComparable<T>
		{
			T difference = Compute.Subtract<T>(quantity ?? Compute.Zero<T>(), used ?? Compute.Zero<T>());
			return Compute.Less<T>(difference, Compute.Zero<T>()) ? Compute.Zero<T>() : difference;
		}
		public static decimal? TotalFromQuantityAndBasisCost<T>(T? quantity, T? basisQuantity, decimal? basisCost) where T : struct, IComparable<T>
		{
			if (Compute.IsZero<T>(quantity))
				return 0;
			if (!quantity.HasValue || !basisQuantity.HasValue || Compute.IsZero<T>(basisQuantity) || !basisCost.HasValue)
				return null;
			return checked(Compute.Divide(Compute.Multiply<T>(basisCost, quantity), basisQuantity));
		}
	}
}
