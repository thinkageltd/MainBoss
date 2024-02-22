using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.Collections;
using System.Linq;

namespace Thinkage.MainBoss.Database {
	#region Scheduling
	/// <summary>
	/// Base common class to PMGeneration and setting schedule Basis for unit maintenance plans
	/// </summary>
	public abstract class Scheduling : ICheckpointedObject, IDisposable {
		// The dataset to work with for creation of records
		readonly protected dsMB WorkingDs;
		// The dataset to work for reference of records for information
		readonly protected dsMB LookupDs;
		// Quick reference to the Database interface
		readonly protected MB3Client DB;
		protected Scheduling(dsMB workingDataSet) {

			this.WorkingDs = workingDataSet;
			this.DB = (MB3Client)WorkingDs.DB;
			this.LookupDs = new dsMB(this.DB);
#if DEBUG
			this.LookupDs.DataSetName = KB.I("Scheduling.LookupDs");
#endif
		}


		#region IDisposable Members
		bool disposed = false;
		protected virtual void Dispose(bool disposing) {
			if(disposing) {
				if(disposed)
					return;
				disposed = true;
				LookupDs.Dispose();
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(true);
		}
		#endregion

		#region Checkpoint and Rollback
		// This checkpoint mechanism records any new PMGenerationDetail rows that have been created.
		// It does NOT checkpoint changes or deletions to either pre-existing rows or new rows.
		protected List<dsMB.PMGenerationDetailRow> AddedDetailRows = new List<dsMB.PMGenerationDetailRow>();
		protected class CheckpointDataImplementation : ICheckpointData {
			public CheckpointDataImplementation(Scheduling pmg) {
				AddedDetailRows = new List<dsMB.PMGenerationDetailRow>(pmg.AddedDetailRows);
			}
			private readonly List<dsMB.PMGenerationDetailRow> AddedDetailRows;
			public void Rollback(Scheduling pmg) {
				for(int i = pmg.AddedDetailRows.Count; --i >= 0;) {
					dsMB.PMGenerationDetailRow row = pmg.AddedDetailRows[i];
					if(!AddedDetailRows.Contains(row)) {
						// This particular row did not exist when the checkpoint was taken.
						row.Delete();
						pmg.AddedDetailRows.RemoveAt(i);
					}
				}
			}
		}
		/// <summary>
		/// Checkpoint the state of PMGeneration for rolling back in case the dataset Update fails or is not attempted, or the
		/// client code want to discard what was generated but still use the dataset.
		/// </summary>
		public virtual ICheckpointData Checkpoint() {
			return new CheckpointDataImplementation(this);
		}
		/// <summary>
		/// Roll back to time of the given checkpoint data. Note that we undo row creation by calling datarow.Delete; as a result if the
		/// dataset has not been updated to SQL the rows detach and vanish. If the dataset is updated to SQL they will become deleted rows, and
		/// another Update operation will cause them to be deleted from SQL.
		/// </summary>
		public virtual void Rollback(ICheckpointData data) {
			((CheckpointDataImplementation)data).Rollback(this);
		}
		#endregion
	}
	#endregion
	#region ScheduleBasis
	/// <summary>
	/// Set the scheduling Basis for a set of Unit Maintenance Plans
	/// </summary>
	public class ScheduleBasis : Scheduling {
		public ScheduleBasis(dsMB workingDataSet)
			: base(workingDataSet) {
		}
		#region SetBasis
		public void SetBasis(dsMB.PMGenerationBatchRow batchRow, Set<object> ScheduledWorkOrderIDsToGenerate, DateTime newScheduleBasis, IProgressDisplay progress) {
			progress.Update(KB.K("Selecting Unit Maintenance Plans with user criteria"));
			//
			// Do initial setup of members
			//
			System.Diagnostics.Debug.Assert(batchRow.Table.DataSet == WorkingDs, "PMGenerationBatch row in incorrect dataset");
			// We should never be called on a saved committed batch row.
			if(batchRow.RowState != DataRowState.Added && batchRow[dsMB.Schema.T.PMGenerationBatch.F.SessionID, DataRowVersion.Original] == null)
				throw new GeneralException(KB.K("Cannot update schedule basis for a pre-existing committed batch"));

			WorkingDs.EnsureDataTableExists(dsMB.Schema.T.PMGenerationDetail);
			foreach(Guid sRecId in ScheduledWorkOrderIDsToGenerate) {
				// Make one seed row for the SWO. Provisionally each starts off as an error row until we are sure we can set the schedule basis.
				dsMB.PMGenerationDetailRow dRow = WorkingDs.T.PMGenerationDetail.AddNewPMGenerationDetailRow();
				AddedDetailRows.Add(dRow);

				dRow.F.DetailType = (sbyte)DatabaseEnums.PMType.ManualReschedule;
				dRow.F.PMGenerationBatchID = batchRow.F.Id;
				dRow.F.ScheduledWorkOrderID = sRecId;
				dRow.F.Sequence = 0;
				dRow.F.WorkStartDate = newScheduleBasis;
				dRow.F.NextAvailableDate = newScheduleBasis;
				dRow.F.ScheduleDate = newScheduleBasis;
				dRow.F.FirmScheduleData = true;
			}
			progress.Update(KB.K("Completing schedule basis update"));
		}
		#endregion
	}


	#endregion
	#region PMGeneration
	/// <summary>
	/// Class to assist in computing future work based on the defined schedules.
	/// </summary>
	public class PMGeneration : Scheduling {
		// For scheduling algorithm generation detail type records, see DatabaseEnums.PMType
		#region Members
		private static readonly TimeSpan WorkDurationEpsilon = (TimeSpan)((DateTimeTypeInfo)dsMB.Schema.T.WorkOrder.F.StartDateEstimate.EffectiveType).NativeEpsilon(typeof(TimeSpan));
		internal static readonly TimeSpan SchedulingEpsilon = (TimeSpan)((DateTimeTypeInfo)dsMB.Schema.T.PMGenerationDetail.F.ScheduleDate.EffectiveType).NativeEpsilon(typeof(TimeSpan));
#if DEBUG
		private bool destroyed = false;
#endif
		#region - Members set at ctor time
		// The current date (only)
		readonly DateTime currentDateOnly;
		#region -   helper objects
		readonly WorkOrderBatchBuilder woBuilder;
		readonly MeterReadingAnalysis.Predictor<DateTime, long, FuzzyReading> MeterReadingPredictor;
		readonly MeterReadingAnalysis.Predictor<long, DateTime, FuzzyDate> MeterDatePredictor;
		#endregion
		#endregion
		#region - Members set when a Generate starts and unchanged afterwards.
		// NOTE: These should not be used by Commit, since it is legal to call Commit without ever having calle Generate. Maybe Generate should be a more-derived class.
		// End of generation time period
		DateTime BatchEndDate;
		Guid PMGenerationBatchID;
		Guid? PurchaseOrderCreationStateID;
		bool SinglePurchaseOrders;
		#endregion
		#endregion
		#region Constructor
		/// <summary>
		/// DataSet expected to contain primed PMGenerationBatch row with information present wrt generation parameters
		/// </summary>
		/// <param name="workingDataSet"></param>
		public PMGeneration(dsMB workingDataSet) : base(workingDataSet) {
			this.currentDateOnly = DateTime.Today;

			woBuilder = new WorkOrderBatchBuilder(this.WorkingDs);
			MeterReadingPredictor = new MeterReadingAnalysis.PredictReadingFromDateRange(DB);
			MeterDatePredictor = new MeterReadingAnalysis.PredictDateFromReadingRange(DB);
		}
		#endregion
		#region Destruction
		/// <summary>
		/// Destroy this Manager; any Reserved numbers are returned to the database's pool.
		/// Note that the Manager must be destroyed, not just dropped for the garbage collector to finalize
		/// because by then it is too late to do an Update operation.
		/// </summary>
		public void Destroy() {
			woBuilder.Destroy();
#if DEBUG
			destroyed = true;
#endif
		}
#if DEBUG
		~PMGeneration() {
			System.Diagnostics.Debug.Assert(destroyed, "PMGeneration: Was not Destroyed");
		}
#endif
		#endregion
		#region Checkpoint and Rollback
		// TODO: What really should be happening here is that we actually use a private dataset. This would also ease the
		// inter-thread DBClient problems. We would copy what few rows we need from the caller's dataset, make changes here,
		// and merge the changes back to the caller's dataset. Most of checkpoint/rollback would then consist of checkpointing
		// the merge. Note that this would not work smoothly with the sequence count managers in WorkOrderBuilder though. In this respect, then
		// generating the batch (scheduling) and making the work orders (committing) are independent operations and should be done
		// as such.
		// During Generate we create Added PMGEnerationDetail rows in the dataset but change no existing rows; this is managed by the base-class CheckpointDataImplementation class.
		// During Commit, we have the WorkOrderBatchBuilder do work for us, and we modify MakeWorkOrder/MakeSharedWorkOrder Detail rows,
		// setting their WorkOrderID to a nonnull value or altering the type to ErrorMakingWorkOrder and set in error text. To roll these back we need to
		// remember the original DetailType (MakeWorkOrder/MakeSharedWorkOrder).
		// Similarly, the Commit operation merely clears out the PMGenerationBatch.SessionID field, so we can undo the change by setting it back to the user's SessionID without needing to keep original record contents.
		// Note that we do not handle the case (which does not occur) where a Detail or Batch record is changed multiple times with checkpoints taken between. Rolling back to the intermediate checkpoint
		// would not roll the record back because the code would think it had already changed when the checkpoint was taken.
		private Dictionary<dsMB.PMGenerationDetailRow, DatabaseEnums.PMType> CommittedDetailRows = new Dictionary<dsMB.PMGenerationDetailRow, DatabaseEnums.PMType>();
		public List<dsMB.PMGenerationBatchRow> CommittedBatchRows = new List<dsMB.PMGenerationBatchRow>();
		private new class CheckpointDataImplementation : Scheduling.CheckpointDataImplementation {
			public CheckpointDataImplementation(PMGeneration pmg)
				: base(pmg) {
				WOBuilderCheckpoint = pmg.woBuilder.Checkpoint();
				CommittedDetailRows = new Dictionary<dsMB.PMGenerationDetailRow, DatabaseEnums.PMType>(pmg.CommittedDetailRows);
				CommittedBatchRows = new List<dsMB.PMGenerationBatchRow>(pmg.CommittedBatchRows);
			}
			private readonly ICheckpointData WOBuilderCheckpoint;
			private readonly Dictionary<dsMB.PMGenerationDetailRow, DatabaseEnums.PMType> CommittedDetailRows;
			private readonly List<dsMB.PMGenerationBatchRow> CommittedBatchRows;
			public void Rollback(PMGeneration pmg) {
				pmg.woBuilder.Rollback(WOBuilderCheckpoint);
				foreach(KeyValuePair<dsMB.PMGenerationDetailRow, DatabaseEnums.PMType> kvp in new Dictionary<dsMB.PMGenerationDetailRow, DatabaseEnums.PMType>(pmg.CommittedDetailRows)) {
					dsMB.PMGenerationDetailRow row = kvp.Key;
					if(!CommittedDetailRows.ContainsKey(row)) {
						// This particular row was not committed when the checkpoint was taken but is now.
						row.F.WorkOrderID = null;
						row.F.DetailContext = null;
						row.F.DetailType = (sbyte)kvp.Value;
						pmg.CommittedDetailRows.Remove(kvp.Key);
					}
				}
				base.Rollback(pmg); // Delete any newly-created Detail rows.
				for(int i = pmg.CommittedBatchRows.Count; --i >= 0;) {
					dsMB.PMGenerationBatchRow row = pmg.CommittedBatchRows[i];
					if(!CommittedBatchRows.Contains(row)) {
						// This particular row was not committed when the checkpoint was taken but is now.
						row.F.SessionID = pmg.DB.SessionRecordID;
						pmg.CommittedBatchRows.RemoveAt(i);
					}
				}
			}
		}
		/// <summary>
		/// Checkpoint the state of PMGeneration for rolling back in case the dataset Update fails or is not attempted, or the
		/// client code want to discard what was generated but still use the dataset.
		/// </summary>
		public override ICheckpointData Checkpoint() {
			return new CheckpointDataImplementation(this);
		}
		/// <summary>
		/// Roll back to time of the given checkpoint data. Note that we undo row creation by calling datarow.Delete; as a result if the
		/// dataset has not been updated to SQL the rows detach and vanish. If the dataset is updated to SQL they will become deleted rows, and
		/// another Update operation will cause them to be deleted from SQL.
		/// </summary>
		public override void Rollback(ICheckpointData data) {
			((CheckpointDataImplementation)data).Rollback(this);
		}
		#endregion
		#region Generate and helpers
		public static readonly int GenerationProgressSteps = -1; // will be updated when we see how many to do.
		#region - Generate
		#region Fuzzy
		public abstract class FuzzyValue<VT> where VT : struct {
			protected FuzzyValue(VT value, VT maxValue) {
				ExpectedValue = value;
				InclusiveMinValue = value;
				ExclusiveMaxValue = maxValue;
			}
			protected FuzzyValue(VT value, VT? minValue, VT? maxValue, Key reasonForUncertainty, params object[] arguments) {
				ExpectedValue = value;
				InclusiveMinValue = value;
				ExclusiveMaxValue = maxValue;
				UncertaintyExplanationKey = reasonForUncertainty;
				UncertaintyExplanationArguments = arguments;
			}
			public void SetIndefinite(Key reasonForUncertainty, params object[] arguments) {
				InclusiveMinValue = null;
				ExclusiveMaxValue = null;
				UncertaintyExplanationKey = reasonForUncertainty;
				UncertaintyExplanationArguments = arguments;
			}
			public VT? InclusiveMinValue;
			public readonly VT ExpectedValue;
			public VT? ExclusiveMaxValue;
			public Key UncertaintyExplanationKey;
			public object[] UncertaintyExplanationArguments;
			public override bool Equals(object obj) {
				if(obj == null)
					return false;
				if(base.Equals(obj))
					return true;
				var other = obj as FuzzyValue<VT>;
				if(other == null)
					return false;
				return InclusiveMinValue.Equals(other.InclusiveMinValue) && ExpectedValue.Equals(other.ExpectedValue) && ExclusiveMaxValue.Equals(other.ExclusiveMaxValue);
			}
			public override int GetHashCode() {
				return InclusiveMinValue.GetHashCode() ^ ExpectedValue.GetHashCode() ^ ExclusiveMaxValue.GetHashCode();
			}
			public static bool operator ==(FuzzyValue<VT> one, FuzzyValue<VT> other) {
				if(object.ReferenceEquals(one, null))
					return object.ReferenceEquals(other, null);
				return one.Equals(other);
			}
			public static bool operator !=(FuzzyValue<VT> one, FuzzyValue<VT> other) {
				return !(one == other);
			}
		}
		public class FuzzyDate : FuzzyValue<DateTime> {
			public FuzzyDate(DateTime t)
				: base(t, t + SchedulingEpsilon) {
			}
			public FuzzyDate(DateTime t, DateTime? min, DateTime? max, Key reasonForUncertainty, params object[] arguments)
				: base(t, min, max, reasonForUncertainty, arguments) {
			}
			public FuzzyDate AddDays(int days) {
				return new FuzzyDate(ExpectedValue.AddDays(days));
			}
			public FuzzyDate AddMonths(int months) {
				return new FuzzyDate(ExpectedValue.AddMonths(months));
			}
			public FuzzyDate AddTicks(long ticks) {
				return new FuzzyDate(ExpectedValue.AddTicks(ticks));
			}
			public void SetFutureIndefinite(DateTime minDate, Key reasonForUncertainty, params object[] arguments) {
				InclusiveMinValue = ExpectedValue < minDate ? ExpectedValue : minDate;
				ExclusiveMaxValue = null;
				UncertaintyExplanationKey = reasonForUncertainty;
				UncertaintyExplanationArguments = arguments;
			}
			public bool PossiblyBefore(FuzzyDate other) {
				if(!InclusiveMinValue.HasValue)
					return true;
				if(!other.ExclusiveMaxValue.HasValue)
					return true;
				return InclusiveMinValue.Value < other.ExclusiveMaxValue.Value;
			}
			public bool DefinitelyBefore(FuzzyDate other) {
				if(!ExclusiveMaxValue.HasValue)
					return false;
				if(!other.InclusiveMinValue.HasValue)
					return false;
				return ExclusiveMaxValue.Value < other.InclusiveMinValue.Value;
			}
			public static FuzzyDate Min(FuzzyDate one, FuzzyDate other) {
				DateTime? minInclusiveMinValue = one.InclusiveMinValue;
				DateTime minExpectedValue = one.ExpectedValue;
				DateTime? minExclusiveMaxValue = one.ExclusiveMaxValue;
				Key minUncertaintyExplanationKey = one.UncertaintyExplanationKey;
				object[] minUncertaintyExplanationArguments = one.UncertaintyExplanationArguments;

				if(!other.InclusiveMinValue.HasValue || other.InclusiveMinValue < minInclusiveMinValue)
					minInclusiveMinValue = other.InclusiveMinValue;
				if(!minExclusiveMaxValue.HasValue || other.ExclusiveMaxValue < minExclusiveMaxValue)
					minExclusiveMaxValue = other.ExclusiveMaxValue;

				if(minUncertaintyExplanationKey == null || (other.UncertaintyExplanationKey != null && other.ExpectedValue < minExpectedValue)) {
					// Either 'one' is definite or both are fuzzy but 'other' will provide the expected value
					minUncertaintyExplanationKey = other.UncertaintyExplanationKey;
					minUncertaintyExplanationArguments = other.UncertaintyExplanationArguments;
				}

				if(other.ExpectedValue < minExpectedValue)
					minExpectedValue = other.ExpectedValue;

				return new FuzzyDate(minExpectedValue, minInclusiveMinValue, minExclusiveMaxValue, minUncertaintyExplanationKey, minUncertaintyExplanationArguments);
			}
			public static FuzzyDate operator +(FuzzyDate left, TimeSpan right) {
				try {
					return checked(new FuzzyDate(left.ExpectedValue + right, left.InclusiveMinValue + right, left.ExclusiveMaxValue + right, left.UncertaintyExplanationKey, left.UncertaintyExplanationArguments));
				}
				catch(ArgumentOutOfRangeException e) {
					// For some unknown reason overflow/underflow on DateTime operator+ is reported as ArgumentOutOfRangeException (orly?? Which argument do you blame???) instead of OverflowException so we convert it here.
					throw new OverflowException(e.Message);
				}
			}
			public static implicit operator FuzzyDate(DateTime t) {
				return new FuzzyDate(t);
			}
			public static readonly FuzzyDate MaxValue = new FuzzyDate(DateTime.MaxValue - SchedulingEpsilon);   // We do this so we can still represent ExclusiveMaxValue.
		}
		public class FuzzyReading : FuzzyValue<long> {
			public FuzzyReading(long r)
				: base(r, r + 1) {
			}
			public FuzzyReading(long r, long? min, long? max, Key reasonForUncertainty, params object[] arguments)
				: base(r, min, max, reasonForUncertainty, arguments) {
			}
			public static implicit operator FuzzyReading(long t) {
				return new FuzzyReading(t);
			}
			public static FuzzyReading operator +(FuzzyReading left, int right) {
				return checked(new FuzzyReading(left.ExpectedValue + right, left.InclusiveMinValue + right, left.ExclusiveMaxValue + right, left.UncertaintyExplanationKey, left.UncertaintyExplanationArguments));
			}
		}
		#endregion
		public void Generate(dsMB.PMGenerationBatchRow batchRow, Set<object> ScheduledWorkOrderIDsToGenerate, System.Text.StringBuilder resultMessage, IProgressDisplay progress) {
			//
			// Do initial setup of members
			//
			System.Diagnostics.Debug.Assert(batchRow.Table.DataSet == WorkingDs, "PMGenerationBatch row in incorrect dataset");
			// We should never be called on a saved committed batch row.
			if(batchRow.RowState != DataRowState.Added && batchRow[dsMB.Schema.T.PMGenerationBatch.F.SessionID, DataRowVersion.Original] == null)
				throw new GeneralException(KB.K("Cannot re-generate PMs for a pre-existing committed batch"));
			PMGenerationBatchID = batchRow.F.Id;
			PurchaseOrderCreationStateID = batchRow.F.PurchaseOrderCreationStateID;
			SinglePurchaseOrders = batchRow.F.SinglePurchaseOrders;
			BatchEndDate = batchRow.F.EndDate.Value;
			batchRow.F.SessionID = DB.SessionRecordID;

			// Find all the "interesting" pre-existing detail records.
			// Fetch the interesting SWO's,
			// their current detail records (providing the schedule basis),
			//		each one's WO (for updated work start/end),
			//			its current state history and state (so we can tell if work start/end is still subject to change and if it affects scheduling at all),
			// the Work Order template (to get the work duration for new WO's),
			// the SWO's Schedule (to get the 'exceptions' for building availabilities),
			// the Unit's RelativeLocation record (so we can complain if the unit is Hidden)
			//
			// Also fetch the periodicities associated with the interesting SWO's (to allow us to advance the schedules). The PMInitializingGenerationDetail
			// view provides the fanout necessary to do this.
			// and the MeterClass records referenced by them.
			//
			// This includes SWO's that have no Current detail record (i.e. have no schedule basis)
			progress.Update(KB.K("Selecting Unit Maintenance Plans with user criteria"));
			DB.ViewOnlyRows(LookupDs, dsMB.Schema.T.PMInitializingGenerationDetail, new SqlExpression(dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.Hidden).IsNull(), null, new DBI_PathToRow[] {
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.CurrentPMGenerationDetailID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.CurrentPMGenerationDetailID.F.WorkOrderID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.CurrentPMGenerationDetailID.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.CurrentPMGenerationDetailID.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.RelativeLocationID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.PeriodicityID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.PeriodicityID.F.MeterClassID.PathToReferencedRow,
					dsMB.Path.T.PMInitializingGenerationDetail.F.MeterID.PathToReferencedRow
				});
			progress.Update(null, -1, LookupDs.T.ScheduledWorkOrder.Rows.Count + 2); // add 2 for this Update message and the final one in addition to all the interim ones
																					 // Loop over the relevant SWO's, convert the seed information into an initial detail row, and loop advancing that row until the SWO is disposed of.
			WorkingDs.EnsureDataTableExists(dsMB.Schema.T.PMGenerationDetail);
			int validationErrorCount = 0;
			foreach(dsMB.ScheduledWorkOrderRow currentScheduledWORow in LookupDs.T.ScheduledWorkOrder) {
				int detailSequence = 0;
				progress.Update(KB.T(Strings.Format(KB.K("Task {0} on Unit {1}"), currentScheduledWORow.WorkOrderTemplateIDParentRow.F.Code, currentScheduledWORow.UnitLocationIDParentRow.RelativeLocationIDParentRow.F.Code)));
				// Check if there is a list of ids to generate. If there is and this row is not in it, skip the row.
				if(ScheduledWorkOrderIDsToGenerate != null && !ScheduledWorkOrderIDsToGenerate.Contains(currentScheduledWORow.F.Id))
					continue;

				// Make first detail row for the SWO.
				dsMB.PMGenerationDetailRow currentDetailRow = WorkingDs.T.PMGenerationDetail.AddNewPMGenerationDetailRow();
				AddedDetailRows.Add(currentDetailRow);
				currentDetailRow.F.PMGenerationBatchID = PMGenerationBatchID;
				currentDetailRow.F.ScheduledWorkOrderID = currentScheduledWORow.F.Id;
				currentDetailRow.F.Sequence = detailSequence++;
				currentDetailRow.F.FirmScheduleData = true;

				FuzzyDate currentScheduleDate = currentDateOnly;
				FuzzyDate currentWorkStartDate = currentDateOnly;
				FuzzyDate currentWorkEndDate = currentDateOnly;
				FuzzyReading currentScheduleReading = null;
				Guid? currentTriggeringMeterID = null;
				try {
					if (currentScheduledWORow.F.Inhibit) {
						// This entire SWO is inhibited, say so.
						// If the site doesn't want to see it at all, the can delete (hide) the maintenance plan.
						// TODO: We don't have a PMType for this case, so we treat it as an Error.
						// This means it will show up in the Errors tab of the PMGBatch tbl, but because we don't throw an exception here
						// it does not show up as an error in a popup window on generation.
						// Adding a PMType is nontrivial because we number them so the types that form a valid scheduling basis are the upper values
						// so we would either have to add this value at the end and make this check more complex, or add the value in the middle and
						// use an upgrade step to renumber all the values in existing records.
						currentDetailRow.F.DetailType = (sbyte)DatabaseEnums.PMType.Error;
						// We add only the message without context to the disposition record. FullMessage will not put AggregateException messages in so we need to do it explicitly
						currentDetailRow.F.DetailContext = KB.K("This Unit Maintenance Plan record is Inhibited").Translate();
						// Update the current detail row to reflect the 'current' values in case the ValidationException occurs between creating a new detail record and calculating a valid next schedule point.
						SetWorkStartDate(currentDetailRow, currentWorkStartDate, currentWorkEndDate);
						currentDetailRow.F.ScheduleDate = currentScheduleDate.ExpectedValue;
						// On to the next Maintenance Plan
						continue;
					}
					var aggregateExceptions = new List<GeneralException>();
					if (currentScheduledWORow.WorkOrderTemplateIDParentRow.F.Hidden.HasValue)
						// Treat it as an error if the Task is hidden.
						aggregateExceptions.Add(new GeneralException(KB.K("The Task for this Unit Maintenance Plan has been deleted")));
					if(currentScheduledWORow.UnitLocationIDParentRow.RelativeLocationIDParentRow.F.Hidden.HasValue)
						// Treat it as an error if the Unit is hidden. Note though that we don't complain if the unit is *contained in* a deleted location.
						aggregateExceptions.Add(new GeneralException(KB.K("The Unit for this Unit Maintenance Plan has been deleted")));
					if(currentScheduledWORow.ScheduleIDParentRow.F.Hidden.HasValue)
						// Treat it as an error if the Schedule is hidden.
						aggregateExceptions.Add(new GeneralException(KB.K("The Schedule for this Unit Maintenance Plan has been deleted")));
					if(!currentScheduledWORow.F.CurrentPMGenerationDetailID.HasValue)
						// If there is no schedule basis, just make an error row saying so.
						aggregateExceptions.Add(new GeneralException(KB.K("There is no timing basis for this Unit Maintenance Plan")));

					Thinkage.Libraries.Exception.ThrowIfAggregateExceptions(aggregateExceptions, KB.K("Unit Maintenance Plan has multiple errors."));

					dsMB.PMGenerationDetailRow seedRow = currentScheduledWORow.CurrentPMGenerationDetailIDParentRow;
					// Load the seed information into the 'current' values.
					currentScheduleDate = seedRow.F.ScheduleDate;
					currentWorkStartDate = seedRow.F.WorkStartDate;
					currentWorkEndDate = seedRow.F.NextAvailableDate;
					currentScheduleReading = seedRow.F.ScheduleReading;
					currentTriggeringMeterID = seedRow.F.TriggeringMeterID;

					dsMB.WorkOrderTemplateRow currentTemplateRow = currentScheduledWORow.WorkOrderTemplateIDParentRow;
					// We should be searching ResolvedWorkOrderTemplate by its WorkOrderTemplateID field but that is difficult with DataView stuff, but ID field of
					// that table is equal to the WorkOrderTemplateID field so we just use a simple Find call.
					dsMB.ResolvedWorkOrderTemplateRow currentResolvedTemplateRow = (dsMB.ResolvedWorkOrderTemplateRow)LookupDs.T.ResolvedWorkOrderTemplate.RowFind(currentScheduledWORow.F.WorkOrderTemplateID);
					dsMB.ScheduleRow currentScheduleRow = currentScheduledWORow.ScheduleIDParentRow;

					DateTime effectiveBatchEndDate = BatchEndDate.Add(currentResolvedTemplateRow.F.GenerateLeadTime);
					Guid? currentWorkOrderID = seedRow.F.WorkOrderID;

					// Create the mapping from Periodicity to Meter row for this SWO.
					var meterMapping = new Dictionary<Guid, Guid>();
					foreach(dsMB.PeriodicityRow currentPeriodicityRow in currentScheduleRow.GetPeriodicityScheduleIDChildRows()) {
						if(currentPeriodicityRow.F.MeterClassID.HasValue) {
							if(currentPeriodicityRow.F.CalendarUnit.HasValue)
								// TODO: Add to the schema a CHECK constraint that (CalendarUnit IS NULL OR MeterClassID IS NULL) AND (CalendarUnit IS NOT NULL OR MeterClassID IS NOT NULL) so
								// SQL will ensure that exactly one of these is non-null.
								throw new GeneralException(KB.K("Maintenance Period for Maintenance Timing '{0}' contains both a Meter Class and a Calendar Unit"), currentScheduleRow.F.Code);
							if(currentPeriodicityRow.MeterClassIDParentRow.F.Hidden.HasValue)
								throw new GeneralException(KB.K("Maintenance Period for Maintenance Timing '{0}' references deleted Meter Class '{1}'"), currentScheduleRow.F.Code, currentPeriodicityRow.MeterClassIDParentRow.F.Code);
							DataRow[] meterRows = LookupDs.T.Meter.Select(new ColumnExpressionUnparser().UnParse(
										new SqlExpression(dsMB.Path.T.Meter.F.MeterClassID).Eq(SqlExpression.Constant(currentPeriodicityRow.F.MeterClassID))
											.And(new SqlExpression(dsMB.Path.T.Meter.F.UnitLocationID).Eq(SqlExpression.Constant(currentScheduledWORow.F.UnitLocationID)))));
							if(meterRows.Length == 0)
								throw new GeneralException(KB.K("Maintenance Period for Maintenance Timing '{0}' refers to meter class '{1}' but Unit '{2}' has no meter of that class"), currentScheduleRow.F.Code, currentPeriodicityRow.MeterClassIDParentRow.F.Code, currentScheduledWORow.UnitLocationIDParentRow.F.Code);
							var meterRow = (dsMB.MeterRow)meterRows[0];
							if(meterRow.F.Hidden.HasValue)
								throw new GeneralException(KB.K("Maintenance Period for Maintenance Timing '{0}' refers to meter class '{1}' but the meter of that class for Unit '{2}' has been deleted"), currentScheduleRow.F.Code, currentPeriodicityRow.MeterClassIDParentRow.F.Code, currentScheduledWORow.UnitLocationIDParentRow.F.Code);
							meterMapping[currentPeriodicityRow.F.Id] = meterRow.F.Id;
						}
					}

					var currentDetailType = (DatabaseEnums.PMType)seedRow.F.DetailType;
					bool basedOnTentativeBasis = false;     // This prevents availability deferrals from firming up the date.

					dsMB.PMGenerationDetailRow lastMakeWorkOrderRow = null;
					// Do the forward scheduling for this SWO. On loop entry, the currentXxxx variables represent the state of our seed row (from a previous batch),
					// and on each iteration these are advanced to the result (Make(Shared)WorkOrder or Inhibited) row of the schedule point just handled.
					// This outer loop advances the schedule basis, and an inner loop does deferrals.
					for(;;) {
						#region Calculate basis date for scheduling
						// Calculate when the next schedule point is, based on the scheduling interval plus the basis from the current row information.
						FuzzyDate basisScheduleDate = currentScheduleDate;
						if(basisScheduleDate.UncertaintyExplanationKey != null) {
							basisScheduleDate.UncertaintyExplanationKey = KB.K("Based on previous tentative schedule date");
							basisScheduleDate.UncertaintyExplanationArguments = null;
						}
						FuzzyDate basisWorkStartDate = currentWorkStartDate;
						if(basisWorkStartDate.UncertaintyExplanationKey != null) {
							basisWorkStartDate.UncertaintyExplanationKey = KB.K("Based on previous tentative schedule date");
							basisWorkStartDate.UncertaintyExplanationArguments = null;
						}
						FuzzyDate basisWorkEndDate = currentWorkEndDate;
						if(basisWorkEndDate.UncertaintyExplanationKey != null) {
							basisWorkEndDate.UncertaintyExplanationKey = KB.K("Based on previous tentative schedule date");
							basisWorkEndDate.UncertaintyExplanationArguments = null;
						}

						// First, find a basisDate based on the rescheduling algorithm being either the trigger, work start, or work end.
						// For the latter two cases the date may be marked as tentative unless a WO really exists and it no longer affects scheduling.
						// TODO: The code from the start of this for(;;) up to the end of the following switch statement should be put before the head of the loop and streamlined based only on the seed row,
						// and also should be placed at the bottom of the loop and streamlined for only the possible detail types encountered there (Inhibit or Make/Predict(Shared)WorkOrder).
						// Then currentDetailType and currentWorkOrderID would no longer be needed and the code would be clearer because each iteration of the loop starts with the three basis dates set.
						// Maybe also the basis/current/new/next variable names could be made clearer as well.
						switch(currentDetailType) {
							case DatabaseEnums.PMType.PredictedWorkOrder:
							case DatabaseEnums.PMType.MakeWorkOrder:
							case DatabaseEnums.PMType.MakeSharedWorkOrder:
							case DatabaseEnums.PMType.MakeUnscheduledWorkOrder:
								// If the next schedule is based on work start/end date check if there is a real WO. If so obtain updated work start/end from it.
								// Also mark the date tentative if the WO dates can still change.
								//
								// It could be argued that detail record referencing a WO that !AffectsFuturePMGeneration (i.e. a voided WO) should not
								// be eligible to become a current detail record and thus a schedule basis. But that is not sufficient to "undo" a Commit (for instance
								// it will not let you undo an Inhibit record). Thus the records we look at *can* reference !AffectsFuturePMGeneration wo's so we must check for them.
								if((DatabaseEnums.RescheduleBasisAlgorithm)currentScheduledWORow.F.RescheduleBasisAlgorithm != DatabaseEnums.RescheduleBasisAlgorithm.FromScheduleBasis) {
									if(currentWorkOrderID.HasValue) {
										dsMB.WorkOrderRow woRow = (dsMB.WorkOrderRow)LookupDs.T.WorkOrder.RowFind(currentWorkOrderID.Value);
										basisWorkStartDate = woRow.F.StartDateEstimate;
										basisWorkEndDate = woRow.F.EndDateEstimate + WorkDurationEpsilon;
										if(!woRow.CurrentWorkOrderStateHistoryIDParentRow.WorkOrderStateIDParentRow.F.CanModifyOperationalFields
												|| !woRow.CurrentWorkOrderStateHistoryIDParentRow.WorkOrderStateIDParentRow.F.AffectsFuturePMGeneration)
											break;
										var tentativeMessageKey = KB.K("Work Order {0} start/end date subject to change");
										var tentativeMessageArgs = new object[] { woRow.F.Number };
										// TODO: Setting min to the current date here is not really correct; we should perhaps be using some date from the work order.
										basisWorkStartDate.SetFutureIndefinite(currentDateOnly, tentativeMessageKey, tentativeMessageArgs);
										basisWorkEndDate.SetFutureIndefinite(currentDateOnly, tentativeMessageKey, tentativeMessageArgs);
									}
									else {
										// TODO: base this decision on the initial WO state of the WO that would be generated.
										var tentativeMessageKey = KB.K("Previous Work Order has not been generated yet so start/end date subject to change");
										basisWorkStartDate.SetFutureIndefinite(currentDateOnly, tentativeMessageKey);
										basisWorkEndDate.SetFutureIndefinite(currentDateOnly, tentativeMessageKey);
									}
									basedOnTentativeBasis = true;
								}
								break;
							case DatabaseEnums.PMType.ManualReschedule:
								if(currentTriggeringMeterID.HasValue) {
									// TODO: No one creates these right now; only calendar-based ManualReschedules are used.
									// This is to handle meter-based manual reschedules. We ignore the date stored in these records
									// Calculate a corrected basis date by re-predicting back from the meter reading. This way any calendar-based periodicity
									// as well as any periodicities on other meters will have a more correct date to work from, in case the meter information has changed
									// since the ManualReschedule record was created.
									basisScheduleDate = MeterDatePredictor.Predict(currentTriggeringMeterID.Value, currentScheduleReading);
									basisWorkStartDate = basisScheduleDate;
									basisWorkEndDate = basisScheduleDate;
								}
								break;
							case DatabaseEnums.PMType.Inhibited:
								break;
						}

						FuzzyDate basisDate;
						switch((DatabaseEnums.RescheduleBasisAlgorithm)currentScheduledWORow.F.RescheduleBasisAlgorithm) {
							case DatabaseEnums.RescheduleBasisAlgorithm.FromScheduleBasis:
								basisDate = basisScheduleDate;
								break;
							case DatabaseEnums.RescheduleBasisAlgorithm.FromWorkOrderStartDate:
								basisDate = basisWorkStartDate;
								break;
							case DatabaseEnums.RescheduleBasisAlgorithm.FromWorkOrderEndDate:
								basisDate = basisWorkEndDate;
								break;
							default:
								throw new GeneralException(KB.K("Unknown timing basis algorithm {0}"), currentScheduledWORow.F.RescheduleBasisAlgorithm);
						}

						#endregion
						#region Calculate new trigger point as min(basis+each interval)
						// We now have a basis date. Find the next date from that. We have to loop over each of the periodicities and find the one that generates
						// the earliest trigger date. However, if any of them cause estimated dates, we have to estimate everything.
						IEnumerable<dsMB.PeriodicityRow> currentPeriodicityRows = currentScheduleRow.GetPeriodicityScheduleIDChildRows();
						if(Enumerable.Count(currentPeriodicityRows) == 0)
							throw new GeneralException(KB.K("No intervals are defined for the Maintenance Timing '{0}'"), currentScheduleRow.F.Code);
						FuzzyDate nextScheduleDate = null;
						FuzzyReading nextScheduleReading = null;
						Guid? nextTriggeringMeterID = null;
						foreach(dsMB.PeriodicityRow periodicity in currentPeriodicityRows) {
							FuzzyDate thisScheduleDate;
							FuzzyReading thisScheduleReading = 0;
							Guid? thisTriggeringMeterID = null;

							if(periodicity.F.MeterClassID.HasValue) {
								// This is a meter-based periodicity. Get the ID of the referenced meter
								Guid referencedMeterID = meterMapping[periodicity.F.Id];
								// advance from reading estimate on basisDate by the periodicity's interval and estimate a new schedule date, possibly marking it tentative
								// If scheduling is FromScheduleBasis and the meter for this periodicity is the same as the one from the current record information,
								// ignore the basis date and start immediately with the current record's reading.
								if((DatabaseEnums.RescheduleBasisAlgorithm)currentScheduledWORow.F.RescheduleBasisAlgorithm == DatabaseEnums.RescheduleBasisAlgorithm.FromScheduleBasis
									&& currentTriggeringMeterID == referencedMeterID)
									// The meters match and it is the Trigger point on which the next should be based,
									// so use the reading as it stands in the record rather than estimating it from the Trigger date.
									// TODO: If this becomes the next schedule date we should clear any tentativeMessageKey.
									thisScheduleReading = currentScheduleReading;
								else
									// The basis record is for a different meter or is based on WO start/end or manually-entered date.
									// Predict what reading this meter would have had on the basis date.
									thisScheduleReading = MeterReadingPredictor.Predict(referencedMeterID, basisDate);
								// Advance by the meter interval from the periodicity
								try {
									thisScheduleReading += periodicity.F.Interval;
								}
								catch(System.OverflowException) {
									throw new GeneralException(KB.K("Overflow occurred adding the interval {0} to meter reading {1}"), periodicity.F.Interval, thisScheduleReading.ExpectedValue);
								}
								// Map that back to a date
								thisScheduleDate = MeterDatePredictor.Predict(referencedMeterID, thisScheduleReading);
								// Remember that we used this particular meter as a trigger.
								thisTriggeringMeterID = referencedMeterID;
							}
							else
								switch((DatabaseEnums.CalendarUnit)periodicity.F.CalendarUnit) {
									// This is a calendar-based periodicity, just add the interval to the basisDate.
									case DatabaseEnums.CalendarUnit.Days:
										// advance from basisDate by next's schedule's interval, possibly marking it tentative
										try {
											thisScheduleDate = basisDate.AddDays(periodicity.F.Interval);
										}
										catch(System.OverflowException) {
											throw new GeneralException(KB.K("Overflow occurred adding {0} days to basis date {1}"), periodicity.F.Interval, basisDate.ExpectedValue);
										}
										break;
									case DatabaseEnums.CalendarUnit.Months:
										// advance from basisDate by next's schedule's interval, possibly marking it tentative
										try {
											thisScheduleDate = basisDate.AddMonths(periodicity.F.Interval);
										}
										catch(System.OverflowException) {
											throw new GeneralException(KB.K("Overflow occurred adding {0} months to basis date {1}"), periodicity.F.Interval, basisDate.ExpectedValue);
										}
										break;
									default:
										throw new GeneralException(KB.K("Unknown unit for period in Maintenance Timing '{0}'"), currentScheduleRow.F.Code);
								}
							if(nextScheduleDate == null || thisScheduleDate.DefinitelyBefore(nextScheduleDate)) {
								// 'this' schedule date is definitely before the (so far) 'next' schedule date (if any), so keep only the information from the former.
								nextScheduleDate = thisScheduleDate;
								nextScheduleReading = thisScheduleReading;
								nextTriggeringMeterID = thisTriggeringMeterID;
							}
							else if(thisScheduleDate.PossiblyBefore(nextScheduleDate)) {
								// 'this' schedule date is *possibly* before the (so far) 'next' schedule date, so build a new fuzzy date taking the min of each of the corresponding values.
								nextScheduleDate = FuzzyDate.Min(nextScheduleDate, thisScheduleDate);
								// Since we are uncertain as to the basis for the trigger, we must forget about any basis meter reading.
								nextScheduleReading = null;
								nextTriggeringMeterID = null;
							}
						}
						#endregion
						#region Crowd the Excpected, min, and max values of nextScheduleDate to fit into our TypeInfo.
						var scheduleDateType = (Libraries.TypeInfo.DateTimeTypeInfo)dsMB.Schema.T.PMGenerationDetail.F.ScheduleDate.EffectiveType;
						var nextAvailableDateType = (Libraries.TypeInfo.DateTimeTypeInfo)dsMB.Schema.T.PMGenerationDetail.F.NextAvailableDate.EffectiveType;
						// First we crowd the ExpectedValue, which requires making a new FuzzyDate.
						if(nextScheduleDate.ExpectedValue < (DateTime)scheduleDateType.NativeMinLimit(typeof(DateTime)))
							nextScheduleDate = new FuzzyDate((DateTime)scheduleDateType.NativeMinLimit(typeof(DateTime)), nextScheduleDate.InclusiveMinValue, nextScheduleDate.ExclusiveMaxValue, nextScheduleDate.UncertaintyExplanationKey, nextScheduleDate.UncertaintyExplanationArguments);
						if(nextScheduleDate.ExpectedValue + currentResolvedTemplateRow.F.Duration > (DateTime)nextAvailableDateType.NativeMaxLimit(typeof(DateTime)))
							nextScheduleDate = new FuzzyDate((DateTime)nextAvailableDateType.NativeMaxLimit(typeof(DateTime)) - currentResolvedTemplateRow.F.Duration, nextScheduleDate.InclusiveMinValue, nextScheduleDate.ExclusiveMaxValue, nextScheduleDate.UncertaintyExplanationKey, nextScheduleDate.UncertaintyExplanationArguments);
						// Crowd the fuzzy limits as well. These can be changed in situ.
						if(nextScheduleDate.InclusiveMinValue.HasValue) {
							if(nextScheduleDate.InclusiveMinValue.Value < (DateTime)scheduleDateType.NativeMinLimit(typeof(DateTime)))
								nextScheduleDate.InclusiveMinValue = (DateTime)scheduleDateType.NativeMinLimit(typeof(DateTime));
							if(nextScheduleDate.InclusiveMinValue.Value + currentResolvedTemplateRow.F.Duration > (DateTime)nextAvailableDateType.NativeMaxLimit(typeof(DateTime)))
								nextScheduleDate.InclusiveMinValue = (DateTime)nextAvailableDateType.NativeMaxLimit(typeof(DateTime)) - currentResolvedTemplateRow.F.Duration;
						}
						if(nextScheduleDate.ExclusiveMaxValue.HasValue) {
							if(nextScheduleDate.ExclusiveMaxValue.Value < (DateTime)scheduleDateType.NativeMinLimit(typeof(DateTime)))
								nextScheduleDate.ExclusiveMaxValue = (DateTime)scheduleDateType.NativeMinLimit(typeof(DateTime));
							if(nextScheduleDate.ExclusiveMaxValue.Value + currentResolvedTemplateRow.F.Duration > (DateTime)nextAvailableDateType.NativeMaxLimit(typeof(DateTime)))
								nextScheduleDate.ExclusiveMaxValue = (DateTime)nextAvailableDateType.NativeMaxLimit(typeof(DateTime)) - currentResolvedTemplateRow.F.Duration;
						}
						#endregion
						#region Validate that the 'next' values can fit in the SQL fields of the Detail record before making them 'current'
						System.Exception membershipException;
						if((membershipException = dsMB.Schema.T.PMGenerationDetail.F.ScheduleDate.EffectiveType.CheckMembership(nextScheduleDate.ExpectedValue)) != null)
							throw new GeneralException(membershipException, KB.K("The calculated Next Schedule Date {0} is invalid"), nextScheduleDate.ExpectedValue);
						if((membershipException = dsMB.Schema.T.PMGenerationDetail.F.NextAvailableDate.EffectiveType.CheckMembership(nextScheduleDate.ExpectedValue + currentResolvedTemplateRow.F.Duration)) != null)
							throw new GeneralException(membershipException, KB.K("The calculated Next Work End Date {0} is invalid"), nextScheduleDate.ExpectedValue + currentResolvedTemplateRow.F.Duration);
						if(nextTriggeringMeterID.HasValue && (membershipException = dsMB.Schema.T.PMGenerationDetail.F.ScheduleReading.EffectiveType.CheckMembership(nextScheduleReading.ExpectedValue)) != null)
							throw new GeneralException(membershipException, KB.K("The calculated Next Schedule Reading {0} is invalid"), nextScheduleReading.ExpectedValue);
						// Remember the old scheduled date so we can check for positive progress after updating the 'current' from 'next'
						FuzzyDate previousScheduleDate = currentScheduleDate;
						#endregion
						#region Make the new trigger point be the current schedule date and (tentative) work start date
						currentScheduleDate = nextScheduleDate;
						currentWorkStartDate = nextScheduleDate;
						currentWorkEndDate = nextScheduleDate + currentResolvedTemplateRow.F.Duration;
						currentScheduleReading = nextScheduleReading;
						currentTriggeringMeterID = nextTriggeringMeterID;
						#endregion
						#region Store trigger point information in Detail record
						SetWorkStartDate(currentDetailRow, currentWorkStartDate, currentWorkEndDate);
						currentDetailRow.F.ScheduleDate = currentScheduleDate.ExpectedValue;
						if(currentTriggeringMeterID.HasValue) {
							currentDetailRow.F.TriggeringMeterID = currentTriggeringMeterID;
							currentDetailRow.F.ScheduleReading = currentScheduleReading.ExpectedValue;
						}
						#endregion
						if(currentScheduleDate.ExpectedValue <= previousScheduleDate.ExpectedValue)
							if(previousScheduleDate.UncertaintyExplanationKey == null && currentScheduleDate.UncertaintyExplanationKey != null)
								throw new GeneralException(currentScheduleDate.UncertaintyExplanationKey, currentScheduleDate.UncertaintyExplanationArguments);
							else
								throw new GeneralException(KB.K("Maintenance Timing interval is not sufficient to advance scheduled date by at least a day"));
						// TODO: if (!Firm && NoPredictionOption) also terminate scheduling.
						#region Advance work start date based on deferrals
						// Do Availability checking
						// Conceptually there are any number of availabilities, which can either defer or inhibit the work.
						// We process all deferrals first; availabilities which inhibit the work "pass" any offered date as being available.
						// If one availability defers the work, we write a new detail record with the work start suitably adjusted.
						//
						// Once all deferrals are passed, we again iterate over the availabilities to see if any will inhibit the work outright.
						//
						// In practice, there are three availabilities:
						// Current date, which always defers (that is, work cannot be scheduled in the past)
						// Seasonal period, which can inhibit or defer
						// Weekday selections, which can inhibit or defer.
						// Nevertheless, these three hard-wired availabilities are hidden under the cover of GetAvailability, which returns null once
						// the index exceeds the count.
						IScheduleAvailability availability;
						for(int availabilityIndex = 0; (availability = GetAvailability(availabilityIndex, currentScheduleRow)) != null && currentWorkStartDate.ExpectedValue <= effectiveBatchEndDate; ++availabilityIndex) {
							if(!availability.Inhibits && !availability.IsAssuredlyAvailable(currentWorkStartDate)) {
								// Defer due to this availability.
								// Ask the availability when next available. Note that this can reduce the uncertain range if the start of the current range is not available but the end is,
								// or in the opposite case it can widen the uncertain range.
								// TODO: A deferral could invalidate a previous deferral (i.e. a weekday deferral could advance the date to a value that is off-season or vice versa)
								FuzzyDate deferredWorkStartDate;
								try {
									deferredWorkStartDate = availability.NextAvailableOnOrAfter(currentWorkStartDate);
								}
								catch(AvailabilityException ex) {
									throw new GeneralException(ex, KB.K("Deferral error on availability '{0}'"), availability.IDText);
								}
								if(deferredWorkStartDate == currentWorkStartDate)
									// No deferral actually took place because all the uncertainty boundaries are on 'available' dates
									continue;
								if(currentWorkStartDate.UncertaintyExplanationKey != null
									&& deferredWorkStartDate.UncertaintyExplanationKey == null
									&& basedOnTentativeBasis) {
									// The deferral removed any uncertainty from the date, but our basis is tentative so we want to preclude generating any work orders.
									// We put the uncertainty reason in the deferredWorkStartDate from the reason in currentWorkStartDate (which still contains the pre-deferral value)
									deferredWorkStartDate.UncertaintyExplanationKey = currentWorkStartDate.UncertaintyExplanationKey;
									deferredWorkStartDate.UncertaintyExplanationArguments = currentWorkStartDate.UncertaintyExplanationArguments;
								}

								currentDetailRow.F.DetailType = (sbyte)DatabaseEnums.PMType.Deferred;
								currentDetailRow.F.DetailContext = availability.MessageText.Translate();

								// Make a new detail row with the same schedule point but a deferred work start date.
								currentDetailRow = WorkingDs.T.PMGenerationDetail.AddNewPMGenerationDetailRow();
								AddedDetailRows.Add(currentDetailRow);
								currentDetailRow.F.PMGenerationBatchID = PMGenerationBatchID;
								currentDetailRow.F.ScheduledWorkOrderID = currentScheduledWORow.F.Id;
								currentDetailRow.F.Sequence = detailSequence++;

								// Validate the new work interval before making it 'current'
								if((membershipException = dsMB.Schema.T.PMGenerationDetail.F.WorkStartDate.EffectiveType.CheckMembership(deferredWorkStartDate.ExpectedValue)) != null)
									throw new GeneralException(membershipException, KB.K("The calculated Next Work Start Date {0} is invalid"), deferredWorkStartDate.ExpectedValue);
								if((membershipException = dsMB.Schema.T.PMGenerationDetail.F.NextAvailableDate.EffectiveType.CheckMembership(deferredWorkStartDate.ExpectedValue + currentResolvedTemplateRow.F.Duration)) != null)
									throw new GeneralException(membershipException, KB.K("The calculated Next Work End Date {0} is invalid"), deferredWorkStartDate.ExpectedValue + currentResolvedTemplateRow.F.Duration);
								// Make it 'current'
								currentWorkStartDate = deferredWorkStartDate;
								currentWorkEndDate = deferredWorkStartDate + currentResolvedTemplateRow.F.Duration;
								// Put the updated 'current' values into the new Detail row.
								SetWorkStartDate(currentDetailRow, currentWorkStartDate, currentWorkEndDate);
								currentDetailRow.F.ScheduleDate = currentScheduleDate.ExpectedValue;
								if(currentTriggeringMeterID.HasValue) {
									currentDetailRow.F.ScheduleReading = currentScheduleReading.ExpectedValue;
									currentDetailRow.F.TriggeringMeterID = currentTriggeringMeterID;
								}
							}
						}
						#endregion
						if(currentWorkStartDate.ExpectedValue > effectiveBatchEndDate) {
							currentDetailRow.F.DetailType = (sbyte)DatabaseEnums.PMType.SchedulingTerminated;
							break;
						}
						#region Check for inhibiting on finalized work start date
						// The work might be "available" to deferring availabilities on the work start date and this date does not exceed the ActualBatchEndDate.
						// Do inhibits
						for(short availabilityIndex = 0; (availability = GetAvailability(availabilityIndex, currentScheduleRow)) != null; ++availabilityIndex)
							if(availability.Inhibits && !availability.IsAssuredlyAvailable(currentWorkStartDate))
								break;
						#endregion
						#region Record disposition as Inhibited or some form of MakeWorkOrder
						// If the work start date is uncertain we have to poison the SWO so future work orders cannot be made even if something like a seasonal deferral firms up the date.
						if(currentWorkStartDate.UncertaintyExplanationKey != null)
							basedOnTentativeBasis = true;
						if(availability != null) {
							// 'availability' inhibits the work order. If the date is not firm this cannot become eligible to be a "current" detail record.
							currentDetailType = DatabaseEnums.PMType.Inhibited;
							currentDetailRow.F.DetailContext = availability.MessageText.Translate();
						}
						else if(currentWorkStartDate.UncertaintyExplanationKey == null) {
							// Made it this far and available - make a workorder and advance to next schedule point.
							if (lastMakeWorkOrderRow != null && lastMakeWorkOrderRow.F.WorkStartDate == currentWorkStartDate.ExpectedValue) {
								lastMakeWorkOrderRow.F.DetailType = (sbyte)DatabaseEnums.PMType.MakeSharedWorkOrder;
								currentDetailType = DatabaseEnums.PMType.MakeSharedWorkOrder;
							}
							else
								currentDetailType = DatabaseEnums.PMType.MakeWorkOrder;
							lastMakeWorkOrderRow = currentDetailRow;
						}
						else {
							currentDetailType = DatabaseEnums.PMType.PredictedWorkOrder;
							currentDetailRow.F.DetailContext = KB.K("Indefinite work start date").Translate();
						}
						currentDetailRow.F.DetailType = (sbyte)currentDetailType;
						currentWorkOrderID = null;
						#endregion
						#region Make the next Detail row
						currentDetailRow = WorkingDs.T.PMGenerationDetail.AddNewPMGenerationDetailRow();
						AddedDetailRows.Add(currentDetailRow);
						currentDetailRow.F.PMGenerationBatchID = PMGenerationBatchID;
						currentDetailRow.F.ScheduledWorkOrderID = currentScheduledWORow.F.Id;
						currentDetailRow.F.Sequence = detailSequence++;
						currentDetailRow.F.FirmScheduleData = currentWorkStartDate.UncertaintyExplanationKey == null;
						#endregion
					}
				}
				catch (System.Exception ex) {
					++validationErrorCount;
					currentDetailRow.F.DetailType = (sbyte)DatabaseEnums.PMType.Error;
					// We add only the message without context to the disposition record. FullMessage will not put AggregateException messages in so we need to do it explicitly
					currentDetailRow.F.DetailContext = Thinkage.Libraries.Exception.FullMessage(ex);
					// Update the current detail row to reflect the 'current' values in case the ValidationException occurs between creating a new detail record and calculating a valid next schedule point.
					SetWorkStartDate(currentDetailRow, currentWorkStartDate, currentWorkEndDate);
					currentDetailRow.F.ScheduleDate = currentScheduleDate.ExpectedValue;
					if (currentTriggeringMeterID.HasValue) {
						currentDetailRow.F.ScheduleReading = currentScheduleReading.ExpectedValue;
						currentDetailRow.F.TriggeringMeterID = currentTriggeringMeterID;
					}
					if (resultMessage != null) { // Add the message PLUS context to our resultMessage. We try to make a numbered list of the errors.
						Thinkage.Libraries.Exception.AddContext(ex, new MessageExceptionContext(KB.K("Task '{0}', Schedule '{1}'"), currentScheduledWORow.WorkOrderTemplateIDParentRow.F.Code, currentScheduledWORow.ScheduleIDParentRow.F.Code));
						Thinkage.Libraries.Exception.AddContext(ex, new MessageExceptionContext(KB.K("Unit '{0}'"), currentScheduledWORow.UnitLocationIDParentRow.F.Code));
						System.Text.StringBuilder sb = new System.Text.StringBuilder();
						resultMessage.Append(Strings.IFormat("{0}) ", validationErrorCount));
						Thinkage.Libraries.Exception.AddFullMessage(sb, ex, 4); // want these indented; we will remove the FIRST 4 blanks on return to account for our numbering
																				// message returned has a new line so using AppendLine will make it double spaced
						resultMessage.AppendLine(sb.ToString().TrimStart(' ')); // Trim will remove the initial indentation we specified, but the inner exceptions will be indented accordingly
					}
				}
			}
			progress.Update(KB.K("Completing generation"));
			if(resultMessage != null && validationErrorCount > 0) {
				resultMessage.AppendLine(KB.K("All errors may also be reviewed in the 'Errors' tab.").Translate());
				resultMessage.Insert(0, Environment.NewLine);
				resultMessage.Insert(0, Environment.NewLine);
				if(validationErrorCount == 1)
					resultMessage.Insert(0, KB.K("There is one error.").Translate());
				else
					resultMessage.Insert(0, Strings.Format(KB.K("There are {0} errors."), validationErrorCount));
			}
		}
		private static void SetWorkStartDate(dsMB.PMGenerationDetailRow detailRow, FuzzyDate workStartDate, FuzzyDate workEndDate) {
			detailRow.F.WorkStartDate = workStartDate.ExpectedValue;
			detailRow.F.NextAvailableDate = workEndDate.ExpectedValue;
			detailRow.F.FirmScheduleData = workStartDate.UncertaintyExplanationKey == null;
			if(workStartDate.UncertaintyExplanationKey != null) {
				var explanation = new System.Text.StringBuilder();
				if(workStartDate.UncertaintyExplanationArguments != null)
					Strings.Append(explanation, workStartDate.UncertaintyExplanationKey, workStartDate.UncertaintyExplanationArguments);
				else
					explanation.Append(workStartDate.UncertaintyExplanationKey.Translate());
				if(workStartDate.InclusiveMinValue != workStartDate.ExpectedValue
					|| workStartDate.ExclusiveMaxValue != workStartDate.ExpectedValue + SchedulingEpsilon) {
					explanation.AppendLine();
					if(workStartDate.InclusiveMinValue == null)
						if(workStartDate.ExclusiveMaxValue == null)
							Strings.Append(explanation, KB.K("Work Start Date could potentially have any value"));
						else
							Strings.Append(explanation, KB.K("Work Start Date could potentially have any value before {0}"), workStartDate.ExclusiveMaxValue.Value);
					else
						if(workStartDate.ExclusiveMaxValue == null)
						Strings.Append(explanation, KB.K("Work Start Date could potentially have any value on or after {0}"), workStartDate.InclusiveMinValue.Value);
					else
						Strings.Append(explanation, KB.K("Work Start Date could potentially have any value from {0} up to but not including {1}"), workStartDate.InclusiveMinValue.Value, workStartDate.ExclusiveMaxValue.Value);
				}
				detailRow.F.BasisDetails = explanation.ToString();
			}
		}
		#endregion
		#region - Scheduling/Availability
		#region -   base interface and class
		/// <summary>
		/// An IAvailability which indicates how to react to unavailability: Inhibit or not (defer)
		/// </summary>
		public interface IScheduleAvailability : IAvailability {
			bool Inhibits
			{
				get;
			}
		}
		#endregion
		#region -   WeekdayAvailability
		private class WeekdayAvailability : IScheduleAvailability {
			public WeekdayAvailability(dsMB.ScheduleRow scheduleRow) {
				Inhibits = scheduleRow.F.InhibitWeek;
				AvailableWeekDayMask = 0;
				if(scheduleRow.F.EnableSunday)
					AvailableWeekDayMask |= 1 << 0;
				if(scheduleRow.F.EnableMonday)
					AvailableWeekDayMask |= 1 << 1;
				if(scheduleRow.F.EnableTuesday)
					AvailableWeekDayMask |= 1 << 2;
				if(scheduleRow.F.EnableWednesday)
					AvailableWeekDayMask |= 1 << 3;
				if(scheduleRow.F.EnableThursday)
					AvailableWeekDayMask |= 1 << 4;
				if(scheduleRow.F.EnableFriday)
					AvailableWeekDayMask |= 1 << 5;
				if(scheduleRow.F.EnableSaturday)
					AvailableWeekDayMask |= 1 << 6;
			}
			#region IAvailability Members

			public bool IsAssuredlyAvailable(FuzzyDate d) {
				if(d.InclusiveMinValue == null || d.ExclusiveMaxValue == null)
					return false;
				int daysMask;
				if(d.ExclusiveMaxValue.Value >= d.InclusiveMinValue.Value.AddDays(7))
					// The uncertainty spans 7 or more days so we use all weekdays.
					daysMask = 0x7F;
				else {
					int minMask = 0x7F & (~0 << (int)d.InclusiveMinValue.Value.DayOfWeek);
					int maxMask = 0x7F & ~(~0 << (int)d.ExclusiveMaxValue.Value.DayOfWeek);
					// OR'ing occurs if the start is later in the week than the end.
					// AND'ing occurs if the start is earlier in the week than the end.
					daysMask = (minMask & maxMask) == 0 ? (minMask | maxMask) : (minMask & maxMask);
				}
				return (daysMask & AvailableWeekDayMask) == daysMask;
			}

			public FuzzyDate NextAvailableOnOrAfter(FuzzyDate d) {
				int wday_mask = AvailableWeekDayMask;
				if(d.InclusiveMinValue == null || wday_mask == 0x7F)
					return d;
				if((wday_mask & 0x7f) == 0)// never available ?
					throw new AvailabilityException(KB.K("week day is never available"));
				DateTime mind = d.InclusiveMinValue.Value;
				int wday = (int)mind.DayOfWeek;
				while((wday_mask & (1 << wday)) == 0) {
					mind = mind.AddDays(1);
					if(++wday > 6)
						wday = 0;
				}
				if(d.ExclusiveMaxValue != null && mind.AddDays(1) >= d.ExclusiveMaxValue.Value)
					return new FuzzyDate(mind);
				return new FuzzyDate(mind > d.ExpectedValue ? mind : d.ExpectedValue, mind, d.ExclusiveMaxValue, d.UncertaintyExplanationKey, d.UncertaintyExplanationArguments);
			}

			public string IDText
			{
				get
				{
					return KB.K("week day").Translate();
				}
			}
			public Key MessageText
			{
				get
				{
					return KB.K("The proposed Work Start Date falls on a weekday disabled by the Schedule");
				}
			}

			public bool Inhibits
			{
				get; private set;
			}

			private readonly ushort AvailableWeekDayMask;
			#endregion
		}

		#endregion
		#region -   SeasonalAvailability
		private class SeasonalAvailability : IScheduleAvailability {
			public SeasonalAvailability(dsMB.ScheduleRow scheduleRow) {
				Inhibits = scheduleRow.F.InhibitSeason;
				if(scheduleRow.F.SeasonEnd.HasValue && scheduleRow.F.SeasonStart.HasValue) {
					int startDays = scheduleRow.F.SeasonStart.Value.Days;
					int endDays = scheduleRow.F.SeasonEnd.Value.Days;
					SeasonAlwaysAvailable = (startDays == 0 && endDays == 365) || (startDays == endDays + 1);
					SeasonStartIn2000 = new DateTime(2000, 1, 1).AddDays(startDays);
					SeasonEndIn2000 = new DateTime(2000, 1, 1).AddDays(endDays);
				}
				else
					SeasonAlwaysAvailable = true;
			}
			#region IAvailability Members
			private readonly bool SeasonAlwaysAvailable;
			private readonly DateTime SeasonStartIn2000;
			private readonly DateTime SeasonEndIn2000;
			public bool IsAssuredlyAvailable(FuzzyDate d) {
				if(SeasonAlwaysAvailable)
					return true;
				if(d.InclusiveMinValue == null || d.ExclusiveMaxValue == null)
					return false;

				DateTime mind = d.InclusiveMinValue.Value;
				DateTime maxd = d.ExclusiveMaxValue.Value.AddDays(-1);
				int minyear = mind.Year;
				int maxyear = maxd.Year;
				if(SeasonStartIn2000 <= SeasonEndIn2000) {
					// The season does not span the year boundary, and excludes (at least) either Dec 31st or Jan 1st
					if(maxyear > minyear)
						// The uncertainty does span the year boundary, including both Dec 31st and Jan 1st, at least one of which is out of season.
						return false;
					return SeasonStartIn2000.AddYears(minyear - 2000) <= mind && maxd <= SeasonEndIn2000.AddYears(minyear - 2000);
				}
				else {
					// The season spans the year boundary, including both Dec 31st and Jan 1st.
					if(maxyear == minyear)
						// The uncertainty is all in a single year
						return SeasonStartIn2000.AddYears(minyear - 2000) <= mind || maxd <= SeasonEndIn2000.AddYears(minyear - 2000);
					if(maxyear > minyear + 1)
						// The uncertainty spans multiple years.
						return false;
					// The uncertainty spans the year boundary.
					return SeasonStartIn2000.AddYears(minyear - 2000) <= mind && maxd <= SeasonEndIn2000.AddYears(maxyear - 2000);
				}
			}
			public FuzzyDate NextAvailableOnOrAfter(FuzzyDate d) {
				if(d.InclusiveMinValue == null || SeasonAlwaysAvailable)
					return d;

				DateTime mind = d.InclusiveMinValue.Value;
				int minyear = mind.Year;
				if(SeasonStartIn2000 <= SeasonEndIn2000) {
					if(mind < SeasonStartIn2000.AddYears(minyear - 2000))
						mind = SeasonStartIn2000.AddYears(minyear - 2000);
					else if(mind > SeasonEndIn2000.AddYears(minyear - 2000))
						mind = SeasonStartIn2000.AddYears(minyear + 1 - 2000);
				}
				else {
					if(mind < SeasonStartIn2000.AddYears(minyear - 2000) && mind > SeasonEndIn2000.AddYears(minyear - 2000))
						mind = SeasonStartIn2000.AddYears(minyear - 2000);
				}
				if(d.ExclusiveMaxValue != null && mind.AddDays(1) >= d.ExclusiveMaxValue.Value)
					return new FuzzyDate(mind);
				return new FuzzyDate(mind > d.ExpectedValue ? mind : d.ExpectedValue, mind, d.ExclusiveMaxValue, d.UncertaintyExplanationKey, d.UncertaintyExplanationArguments);
			}
			public string IDText
			{
				get
				{
					return KB.K("season").Translate();
				}
			}
			public Key MessageText
			{
				get
				{
					return KB.K("The proposed Work Start Date is in the off-season defined by the Schedule");
				}
			}
			public bool Inhibits
			{
				get; private set;
			}
			#endregion
		}
		#endregion
		#region -   CurrentDateAvailability
		private class CurrentDateAvailability : IScheduleAvailability {
			DateTime currentDate;
			public CurrentDateAvailability(dsMB.ScheduleRow scheduleRow, DateTime currentDate) {
				Inhibits = scheduleRow.F.InhibitIfOverdue;
				this.currentDate = currentDate;
			}

			#region IAvailability Members

			public bool IsAssuredlyAvailable(FuzzyDate d) {
				return d.InclusiveMinValue != null && d.InclusiveMinValue.Value >= currentDate;
			}

			public FuzzyDate NextAvailableOnOrAfter(FuzzyDate d) {
				DateTime mind = d.InclusiveMinValue.HasValue && d.InclusiveMinValue.Value > currentDate ? d.InclusiveMinValue.Value : currentDate;
				if(d.ExclusiveMaxValue != null && mind.AddDays(1) >= d.ExclusiveMaxValue.Value)
					return new FuzzyDate(mind);
				return new FuzzyDate(mind > d.ExpectedValue ? mind : d.ExpectedValue, mind, d.ExclusiveMaxValue, d.UncertaintyExplanationKey, d.UncertaintyExplanationArguments);
			}

			public string IDText
			{
				get
				{
					return KB.K("current date").Translate();
				}
			}
			public Key MessageText
			{
				get
				{
					return KB.K("The proposed Work Start Date is before the date the Maintenance Batch was generated");
				}
			}
			public bool Inhibits
			{
				get; private set;
			}
			#endregion
		}
		#endregion
		#region -   helpers at the PMGeneration level
		// Note: There is no ExistingWorkOrderAvailability as there was in MainBoss 2.9; multiple workorders on the same date will be suppressed
		// at Commit time in the event multiple workorders are deemed necessary by the scheduling algorithm (for example, playing catchup for a generation
		// that hasn't been done for several months
		private IScheduleAvailability GetAvailability(int index, dsMB.ScheduleRow currentScheduleRow) {
			switch(index) {
				case 0:
					return new CurrentDateAvailability(currentScheduleRow, currentDateOnly);
				case 1:
					return new SeasonalAvailability(currentScheduleRow);
				case 2:
					return new WeekdayAvailability(currentScheduleRow);

				default:
					return null;
			}
		}
		#endregion
		#endregion
		#endregion
		#region Commit
		// For generating a PMGenerationBatch, the following should occur:
		// Call Generate (to populate the Data Set with New records)
		// Call UpdateGenerationSet to update the database from the DataSet (including the PMGenerationBatch record)
		// A later call to Commit will create the work orders and commit the batch (including the SQL update).
		//
		public static readonly int CommitProgressSteps = -1;
		/// <summary>
		/// Create workorders for all the detail records that require it.
		/// </summary>
		/// <param name="progress"></param>
		public void Commit(dsMB.PMGenerationBatchRow batchRow, System.Text.StringBuilder resultMessage, IProgressDisplay progress) {
			System.Diagnostics.Debug.Assert(batchRow.Table.DataSet == WorkingDs, "PMGenerationBatch row in incorrect dataset");
			progress.Update(KB.K("Committal initialization"));

			SqlExpression filter = new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq((short)DatabaseEnums.PMType.MakeWorkOrder)
									.Or(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq((short)DatabaseEnums.PMType.MakeSharedWorkOrder))
									.Or(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq((short)DatabaseEnums.PMType.MakeUnscheduledWorkOrder))
								.And(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID).Eq(batchRow.F.Id));
			// Sort in reverse by the work start date so we can allocate stock items in date order. A second sort key of SWO id makes sure
			// simultaneous WO's for the same SWO are handled all in sequence with no simultaneous WO's from other SWO's mixed in.
			SortExpression sort = new SortExpression(dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, SortExpression.SortOrder.Desc,
									dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID, SortExpression.SortOrder.Desc);
			dsMB.WorkOrderRow lastWorkOrderRow = null; // for displaying workorder number of single workorders created
			using(DataView MakeWorkOrderRecords = new DataView(WorkingDs.T.PMGenerationDetail, new ColumnExpressionUnparser().UnParse(filter),
															sort.ToDataExpressionString(), DataViewRowState.CurrentRows)) {
				uint WorkOrderCountEstimate = 0;
				DateTime lastWOStart = DateTime.MaxValue;
				Guid lastWOScheduledWorkOrderID = Guid.Empty;
				// Count the number of workorders we will make, accounting for the MakeSharedWorkOrder records. This reduces the number of workorder numbers
				// that we reserve to a reasonable value that matches what we are going to actually generate.
				{
					for(int i = MakeWorkOrderRecords.Count; --i >= 0;) {
						dsMB.PMGenerationDetailRow dRow = (dsMB.PMGenerationDetailRow)MakeWorkOrderRecords[i].Row;
						if(!(dRow.F.WorkStartDate == lastWOStart && dRow.F.ScheduledWorkOrderID == lastWOScheduledWorkOrderID)) {
							++WorkOrderCountEstimate;
							lastWOStart = dRow.F.WorkStartDate;
							lastWOScheduledWorkOrderID = dRow.F.ScheduledWorkOrderID;
						}
					}
				}

				woBuilder.PopulateLookupTables();
				woBuilder.PopulateChildLookupTables();
				woBuilder.SetCountEstimate(WorkOrderCountEstimate);
				woBuilder.SpecifyRecordArguments(batchRow);
				// Process in reverse; if we change the detailType for an error, the row will vanish from our view, but we will still be able to process
				// the next record. The View is pre-sorted by descending WorkStartDate so this means we actually make the WO's in ascending WorkStartDate order,
				// giving them properly-ordered WO numbers and proper allocation of in-stock tangible resources.
				lastWOStart = DateTime.MaxValue;
				lastWOScheduledWorkOrderID = Guid.Empty;
				dsMB.WorkOrderRow lastWORow = null;
				System.Exception lastWOException = null;
				int currentWorkOrderCount = 1;
				int successWorkOrderCount = 0;
				int errorWorkOrderCount = 0;
				for(int i = MakeWorkOrderRecords.Count; --i >= 0;) {
					dsMB.PMGenerationDetailRow dRow = (dsMB.PMGenerationDetailRow)MakeWorkOrderRecords[i].Row;
					progress.Update(KB.T(Strings.Format(KB.K("Creating Work Order {0} of {1}"), currentWorkOrderCount, WorkOrderCountEstimate)));
					DatabaseEnums.PMType oldType = (DatabaseEnums.PMType)dRow.F.DetailType;
					try {
						if(dRow.F.WorkStartDate == lastWOStart && dRow.F.ScheduledWorkOrderID == lastWOScheduledWorkOrderID) {
							// We want to reuse the same WO, or the exception produced trying to create it.
							if(lastWOException != null)
								throw lastWOException;
						}
						else {
							// We want a fresh WO. Find the related SWO row which contains the basic information required to make the WO.
							dsMB.ScheduledWorkOrderRow SWORow = null;
							// If this Commit is tied to a Generate call the SWO row will be in the LookupDs.
							// Otherwise the SWO table will not even exist in the LookupDs and whoever called Generate must ensure the required row is in the WorkingDs.
							if(LookupDs.T.ScheduledWorkOrder != null)
								SWORow = (dsMB.ScheduledWorkOrderRow)LookupDs.T.ScheduledWorkOrder.RowFind(dRow.F.ScheduledWorkOrderID);
							if(SWORow == null && WorkingDs.T.ScheduledWorkOrder != null)
								SWORow = dRow.ScheduledWorkOrderIDParentRow;
							if(SWORow == null)
								// Our fetch failed to get the correct record. We got a Detail row without its parent SWO row.
								throw new GeneralException(KB.K("Missing Unit Maintenance Plan data row"));

							++currentWorkOrderCount;
							lastWOException = null;
							lastWOStart = dRow.F.WorkStartDate;
							lastWOScheduledWorkOrderID = dRow.F.ScheduledWorkOrderID;
							lastWORow = woBuilder.Create(SWORow, lastWOStart);
							++successWorkOrderCount;
							lastWorkOrderRow = lastWORow;
						}
						dRow.F.WorkOrderID = lastWORow.F.Id;
						dRow.F.NextAvailableDate = lastWORow.F.EndDateEstimate + WorkDurationEpsilon;   // would not be filled into MakeUnscheduledWorkOrder records yet
					}
					catch(System.Exception e) {
						if((DatabaseEnums.PMType)dRow.F.DetailType == DatabaseEnums.PMType.MakeUnscheduledWorkOrder)
							// If it is a MakeUnscheduledWorkOrder record, just continue throwing the exception since we have no way to record the error anyway.
							// Someday we may want to save the error and continue on these as well, or have a ctor arg indicating which way to do it.
							throw;
						dRow.F.DetailType = (sbyte)DatabaseEnums.PMType.ErrorMakingWorkOrder;
						dRow.F.DetailContext = Thinkage.Libraries.Exception.FullMessage(e);
						lastWOException = e;
						lastWORow = null;
						++errorWorkOrderCount;
					}
					CommittedDetailRows[dRow] = oldType;
				}
				// Last step is to allocate the workorder numbers in the order the user desires
				// TODO: make this sort order user selectable via some UI interface to select the primary, secondary, tertiary sorting order
				// Sort by the work start date, followed by Subject
				woBuilder.AssignWorkOrderNumbers(dsMB.Path.T.WorkOrder.F.StartDateEstimate, dsMB.Path.T.WorkOrder.F.Subject);

				progress.Update(KB.K("Completing committal"));

				CommittedBatchRows.Add(batchRow);
				batchRow.F.SessionID = null;
				if(successWorkOrderCount > 0) {
					if(successWorkOrderCount == 1)
						resultMessage.AppendLine(Strings.Format(KB.K("Work Order {0} was created."), lastWorkOrderRow.F.Number));
					else
						resultMessage.AppendLine(Strings.Format(KB.K("{0} Work Orders were created."), successWorkOrderCount));
				}
				else
					resultMessage.AppendLine(Strings.Format(KB.K("No Work Orders were created.")));
				resultMessage.AppendLine();
				if(errorWorkOrderCount > 0) {
					if(errorWorkOrderCount == 1)
						resultMessage.AppendLine(Strings.Format(KB.K("There is one error.")));
					else
						resultMessage.AppendLine(Strings.Format(KB.K("There are {0} errors."), errorWorkOrderCount));
				}
				else
					resultMessage.AppendLine(Strings.Format(KB.K("There are no errors.")));
			}
		}
		#endregion
		#region DeleteUncommittedBatch
		/// <summary>
		/// Delete all the records for the given batch row, on the assumption that the rows have already been written to SQL and are thus
		/// currently "unchanged" in the DataSet. This will leave all the rows in the "Deleted" state in the dataset, so the caller that then
		/// commit this to the server to delete all the records. This includes deleting the bathRow itself.
		/// </summary>
		/// <param name="batchRow"></param>
		public void DeleteUncommittedBatch(dsMB.PMGenerationBatchRow batchRow) {
			foreach(dsMB.PMGenerationDetailRow detail in batchRow.GetPMGenerationDetailPMGenerationBatchIDChildRows())
				detail.Delete();
			batchRow.Delete();
		}
		#endregion

		#region Dispose
		bool disposed = false;
		// Our override for Dispose to get rid of our objects
		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if(disposing) {
				if(disposed)
					return;
				disposed = true;
				woBuilder.Dispose();
			}
		}
		#endregion
	}
	#endregion
}
