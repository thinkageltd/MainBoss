using System;
using System.Data;
using Thinkage.Libraries.DBILibrary;
using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	public static class MeterReadingAnalysis {
		#region the public Predictor abstract class
		// The following class structure has a minor amount of code duplication: The <algorithm>Predictor class must be derived from twice
		// once for date->reading and once for reading->date, and the code that selects the Key and Value columns will be the same in each
		// corresponding derivation. We could have a "PredictorFieldChooser" class and statically make only 2 instances of it, and pass it to the
		// <algorithm>Predictor ctor to avoid this, but you would still need a derived class to ultimately do the math required on keys and values
		// by the <algorithm>Predictor. The only way to avoid the latter problem would be to provide a generalized PredictorMath<KType, RType> interface
		// that slowly accumulates methods as they become required by more sophisticated predictors. The methods required in that interface right now
		// would be: KType KeySubtract(KType, KType), KType KeyAdd(KType, KType), KType KeyScale(KType, int divisor), RType ValueScale(RType, KType num, KType denom)
		// The predictor math could be embedded in the same class as was termed "predictor field chooser".
		//
		// If we start having more types of predictors we may want to do this.
		// We may also want to give MeterReadingAnalysis static methods to provide two predictor objects of the appropriate algorithm, so we can
		// make all the derived Predictor derivations private.
		public abstract class Predictor<KType, RType, FuzzyRType>
			where KType : struct
			where RType : struct
			where FuzzyRType : PMGeneration.FuzzyValue<RType> {
			public Predictor(MB3Client db) {
				DB = db;
			}
			protected readonly MB3Client DB;
			// Predict a Result value for the given range of Key values which is to be treated as being all equally statistically probable.
			// Note that if the two keys passed are equal, an exact-hit status will never be returned. As well, a false "DefectiveData"
			// return can occur if there are two entries whose key equals both passed keys.
			public abstract FuzzyRType Predict(Guid meterID, PMGeneration.FuzzyValue<KType> key);
		}
		#endregion
		#region the Linear Predictor implementation
		#region - the Linear algorithm itself
		public abstract class LinearPredictor<KType, RType, FuzzyRType> : Predictor<KType, RType, FuzzyRType>
			where KType : struct, IComparable
			where RType : struct
			where FuzzyRType : PMGeneration.FuzzyValue<RType> {
			public LinearPredictor(MB3Client db)
				: base(db) {
			}
			protected abstract DBI_Path KeyColumn { get; }
			protected abstract DBI_Path ValueColumn { get; }
			protected abstract KType AddKeyDelta(KType value);
			protected abstract KType SubtractKeyDelta(KType value);
			protected abstract RType AddValueDelta(RType value);
			// This method must calculate the result nearValue + (minInclusiveKey+maxExclusiveKey)/2 * (farValue-nearValue)/(farKey-nearKey); this is
			// to avoid us having to develop generic arithmetic operators to add, subtract, multiply and divide these types.
			protected abstract RType PredictLinear(KType nearKey, KType farKey, RType nearValue, RType farValue, KType givenKey);
			protected abstract FuzzyRType MakeFuzzyRType(RType expected);
			protected abstract FuzzyRType MakeFuzzyRType(RType expected, RType? min, RType? max, Key reasonForUncertainty, params object[] arguments);
			private FuzzyRType PredictSingle(Guid meterID, KType key) {
				KType keymax = AddKeyDelta(key);
				MSSqlUnparser up = new MSSqlUnparser();
				SqlExpression belowFilter = new SqlExpression(KeyColumn).Lt(SqlExpression.Constant(key));
				SqlExpression aboveFilter = new SqlExpression(KeyColumn).GEq(SqlExpression.Constant(keymax));
				SqlExpression meterFilter = new SqlExpression(dsMB.Path.T.MeterReading.F.MeterID).Eq(SqlExpression.Constant(meterID)).And(new SqlExpression(dsMB.Path.T.MeterReading.F.Hidden).IsNull());

				// Actually fetch the data rows >= key and < keymax. We could do this using DB.View but we cannot specify TOP and sorting that way.
				var query = new SelectSpecification(dsMB.Schema.T.MeterReading, new[] { new SqlExpression(ValueColumn) }, SqlExpression.And(SqlExpression.Or(belowFilter, aboveFilter).Not(), meterFilter), null);
				query.SetSort(new SqlExpression(KeyColumn), new SqlExpression(ValueColumn));
				DataRowCollection matchingRows = DB.Session.ExecuteCommandReturningTable(query).Tables[0].Rows;
				if (matchingRows.Count > 0) {
					if (matchingRows.Count > 1 && !ValueColumn.ReferencedColumn.EffectiveType.GenericEquals(matchingRows[0][0], matchingRows[matchingRows.Count - 1][0]))
						// There are multiple different Values between key and up to but not including AddKeyDelta(key). Return the median as the expected value and the min and max values as the range.
						return MakeFuzzyRType(
							(RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(matchingRows[matchingRows.Count / 2][0], typeof(RType)),
							(RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(matchingRows[0][0], typeof(RType)),
							AddValueDelta((RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(matchingRows[matchingRows.Count - 1][0], typeof(RType))),
							KB.K("Multiple meter readings match the target"));
					else
						// There is only a single value (possibly in multiple readings) for the key range. Return it as definite.
						return MakeFuzzyRType((RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(matchingRows[0][0], typeof(RType)));
				}

				// No hit on keys in that range; we have to interpolate between entries above and below the range.
				// In particular, query 2 below, then query min(1, 2-belowCount) above. If this row(s) discard the first below row.
				// Then keep the two first combined rows.
				query = new SelectSpecification(dsMB.Schema.T.MeterReading, new[] { new SqlExpression(KeyColumn), new SqlExpression(ValueColumn) }, SqlExpression.And(belowFilter, meterFilter), null);
				query.Sorting = new[] { new SortKey<SqlExpression>(new SqlExpression(KeyColumn), true), new SortKey<SqlExpression>(new SqlExpression(ValueColumn), true) };	// Descending sort on both keys
				query.TopExpression = SqlExpression.Constant(2);
				DataRowCollection belowRows = DB.Session.ExecuteCommandReturningTable(query).Tables[0].Rows;
				query = new SelectSpecification(dsMB.Schema.T.MeterReading, new [] { new SqlExpression(KeyColumn), new SqlExpression(ValueColumn) }, SqlExpression.And(aboveFilter, meterFilter), null);
				query.SetSort(new SqlExpression(KeyColumn), new SqlExpression(ValueColumn));
				query.TopExpression = SqlExpression.Constant(belowRows.Count == 0 ? 2 : 1);
				DataRowCollection aboveRows = DB.Session.ExecuteCommandReturningTable(query).Tables[0].Rows;

				RType val;
				DataRow nearRow;
				DataRow farRow;
				switch (belowRows.Count*3+aboveRows.Count) {
				case 0*3+0:
					// No data at all
					return MakeFuzzyRType(default(RType), null, null, KB.K("There are no readings for meter"));
				case 0*3+1:
					// Only one point above
					val = (RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(aboveRows[0][1], typeof(RType));
					return MakeFuzzyRType(default(RType), null, val, KB.K("There is only one reading for this meter"));
				case 1*3+0:
					// Only one point below
					val = (RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(belowRows[0][1], typeof(RType));
					return MakeFuzzyRType(val, val, null, KB.K("There is only one reading for this meter"));
				case 0 * 3 + 2:
					// Only two points above
					nearRow = aboveRows[0];
					farRow = aboveRows[1];
					break;
				case 2 * 3 + 0:
					// Only two points below
					nearRow = belowRows[0];
					farRow = belowRows[1];
					break;
				default:
					// Points both above and below
					nearRow = belowRows[0];	// Note that due to reverse sort row 0 is the largest key in the Below set.
					farRow = aboveRows[0];
					break;
				}

				var nearKey = (KType)KeyColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(nearRow[0], typeof(KType));
				var farKey = (KType)KeyColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(farRow[0], typeof(KType));
				var nearVal = (RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(nearRow[1], typeof(RType));
				var farVal = (RType)ValueColumn.ReferencedColumn.EffectiveType.GenericAsNativeType(farRow[1], typeof(RType));
				RType predictedVal;
				if (KeyColumn.ReferencedColumn.EffectiveType.GenericEquals(nearKey, farKey))
					// This can only happen in the Extrapolated case, either two below or two above.
					// If nearKey != key or AddKeyDelta(key) the result should really be +-infinity but we have no way to represent that.
					predictedVal = nearVal;
				else
					predictedVal = PredictLinear(nearKey, farKey, nearVal, farVal, key);

				if (belowRows.Count == 0)
					// Two above. result is -infinity to closer (smaller) value
					return MakeFuzzyRType(predictedVal, null, AddValueDelta(nearVal), KB.T(Strings.Format(KB.K("All readings on this meter are above the target {0}"), KeyColumn.ReferencedColumn.EffectiveType.GetTypeFormatter(Application.InstanceFormatCultureInfo).Format(keymax))));
				else if (aboveRows.Count == 0)
					// Two below, result is closer (larger) value to infinity.
					return MakeFuzzyRType(predictedVal, nearVal, null, KB.T(Strings.Format(KB.K("All readings on this meter are below the target {0}"), KeyColumn.ReferencedColumn.EffectiveType.GetTypeFormatter(Application.InstanceFormatCultureInfo).Format(key))));
				else
					// Using one above and one below, treat it as definitive.
					return MakeFuzzyRType(predictedVal);
			}
			public override FuzzyRType Predict(Guid meterID, PMGeneration.FuzzyValue<KType> key) {
				FuzzyRType resultFromExpected = PredictSingle(meterID, key.ExpectedValue);
				if (!resultFromExpected.InclusiveMinValue.HasValue) {
					// The prediction has unlimited uncertainty on the low end
				}
				else if (!key.InclusiveMinValue.HasValue) {
					// The key has unlimited uncertainty on the low end
					resultFromExpected.InclusiveMinValue = null;
					resultFromExpected.UncertaintyExplanationKey = key.UncertaintyExplanationKey;
					resultFromExpected.UncertaintyExplanationArguments = key.UncertaintyExplanationArguments;
				}
				else if (!KeyColumn.ReferencedColumn.EffectiveType.GenericEquals(key.InclusiveMinValue.Value, key.ExpectedValue)) {
					// The key has finite non-zero uncertainty on the low end
					FuzzyRType resultFromMin = PredictSingle(meterID, key.InclusiveMinValue.Value);
					if (!resultFromMin.InclusiveMinValue.HasValue || ValueColumn.ReferencedColumn.EffectiveType.GenericGreaterThan(resultFromExpected.InclusiveMinValue.Value, resultFromMin.InclusiveMinValue.Value)) {
						resultFromExpected.InclusiveMinValue = resultFromMin.InclusiveMinValue;
						if (resultFromMin.UncertaintyExplanationKey != null) {
							resultFromExpected.UncertaintyExplanationKey = resultFromMin.UncertaintyExplanationKey;
							resultFromExpected.UncertaintyExplanationArguments = resultFromMin.UncertaintyExplanationArguments;
						}
						else {
							resultFromExpected.UncertaintyExplanationKey = key.UncertaintyExplanationKey;
							resultFromExpected.UncertaintyExplanationArguments = key.UncertaintyExplanationArguments;
						}
					}
				}

				if (!resultFromExpected.ExclusiveMaxValue.HasValue) {
					// The prediction has unlimited uncertainty on the high end
				}
				else if (!key.ExclusiveMaxValue.HasValue) {
					// The key has unlimited uncertainty on the high end
					resultFromExpected.ExclusiveMaxValue = null;
					resultFromExpected.UncertaintyExplanationKey = key.UncertaintyExplanationKey;
					resultFromExpected.UncertaintyExplanationArguments = key.UncertaintyExplanationArguments;
				}
				else if (!KeyColumn.ReferencedColumn.EffectiveType.GenericEquals(key.ExclusiveMaxValue.Value, AddKeyDelta(key.ExpectedValue))) {
					// The key has finite non-zero uncertainty on the high end
					FuzzyRType resultFromMax = PredictSingle(meterID, SubtractKeyDelta(key.InclusiveMinValue.Value));
					if (!resultFromMax.ExclusiveMaxValue.HasValue || ValueColumn.ReferencedColumn.EffectiveType.GenericGreaterThan(resultFromMax.ExclusiveMaxValue.Value, resultFromExpected.ExclusiveMaxValue.Value)) {
						resultFromExpected.ExclusiveMaxValue = resultFromMax.ExclusiveMaxValue;
						if (resultFromMax.UncertaintyExplanationKey != null) {
							resultFromExpected.UncertaintyExplanationKey = resultFromMax.UncertaintyExplanationKey;
							resultFromExpected.UncertaintyExplanationArguments = resultFromMax.UncertaintyExplanationArguments;
						}
						else {
							resultFromExpected.UncertaintyExplanationKey = key.UncertaintyExplanationKey;
							resultFromExpected.UncertaintyExplanationArguments = key.UncertaintyExplanationArguments;
						}
					}
				}

				return resultFromExpected;
			}
		}
		#endregion
		#region - The two derivations for to reading/to date
		public class PredictReadingFromDateRange : LinearPredictor<DateTime, long, PMGeneration.FuzzyReading> {
			public PredictReadingFromDateRange(MB3Client db)
				: base(db) {
			}

			protected override DBI_Path KeyColumn {
				get { return dsMB.Path.T.MeterReading.F.EffectiveDate; }
			}

			protected override DBI_Path ValueColumn {
				get { return dsMB.Path.T.MeterReading.F.EffectiveReading; }
			}

			protected override long PredictLinear(DateTime nearKey, DateTime farKey, long nearValue, long farValue, DateTime givenKey) {
				long denominator = farKey.Ticks - nearKey.Ticks;
				long numerator = givenKey.Ticks - nearKey.Ticks;
				// We need long*long range here but we don't need exact integral precision so we turn the fraction into a double first rather than
				// horsing around with decimal.
				return nearValue+(long)Math.Round((farValue - nearValue) * ((double)numerator / denominator));
			}
			protected override PMGeneration.FuzzyReading MakeFuzzyRType(long expected) {
				return new PMGeneration.FuzzyReading(expected);
			}
			protected override PMGeneration.FuzzyReading MakeFuzzyRType(long expected, long? min, long? max, Key reasonForUncertainty, params object[] arguments) {
				return new PMGeneration.FuzzyReading(expected, min, max, reasonForUncertainty, arguments);
			}
			protected override DateTime AddKeyDelta(DateTime value) {
				return value + PMGeneration.SchedulingEpsilon;
			}
			protected override long AddValueDelta(long value) {
				return value + 1;
			}
			protected override DateTime SubtractKeyDelta(DateTime value) {
				return value - PMGeneration.SchedulingEpsilon;
			}
		}

		public class PredictDateFromReadingRange : LinearPredictor<long, DateTime, PMGeneration.FuzzyDate> {
			public PredictDateFromReadingRange(MB3Client db)
				: base(db) {
			}

			protected override DBI_Path KeyColumn {
				get { return dsMB.Path.T.MeterReading.F.EffectiveReading; }
			}

			protected override DBI_Path ValueColumn {
				get { return dsMB.Path.T.MeterReading.F.EffectiveDate; }
			}

			protected override DateTime PredictLinear(long nearKey, long farKey, DateTime nearValue, DateTime farValue, long givenKey) {
				long denominator = farKey - nearKey;
				long numerator = givenKey - nearKey;
				// We need long*long range here but we don't need exact integral precision so we turn the fraction into a double first rather than
				// horsing around with decimal.
				return nearValue.AddTicks((long)Math.Round((farValue.Ticks - nearValue.Ticks) * ((double)numerator / denominator)));
			}
			protected override PMGeneration.FuzzyDate MakeFuzzyRType(DateTime expected) {
				return new PMGeneration.FuzzyDate(expected.Date);
			}
			protected override PMGeneration.FuzzyDate MakeFuzzyRType(DateTime expected, DateTime? min, DateTime? max, Key reasonForUncertainty, params object[] arguments) {
				return new PMGeneration.FuzzyDate(expected.Date, min.HasValue ? (DateTime?)min.Value.Date : null, max.HasValue ? (DateTime?)max.Value.Date : null, reasonForUncertainty, arguments);
			}
			protected override long AddKeyDelta(long value) {
				return value + 1;
			}
			protected override DateTime AddValueDelta(DateTime value) {
				return value + PMGeneration.SchedulingEpsilon;
			}
			protected override long SubtractKeyDelta(long value) {
				return value - 1;
			}
		}
		#endregion
		#endregion
	}
}