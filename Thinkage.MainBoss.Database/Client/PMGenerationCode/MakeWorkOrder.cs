using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.DataFlow;

namespace Thinkage.MainBoss.Database {
	public class PurchaseOrderBuilder : ICheckpointedObject, IDisposable {
		// TODO: Migrate the old accumulation code to the new one. This requires changing the new one so the source is a DataFlow.Source which is separately
		// specified from the accumulating value kept in a DataFlow.Value, allowing the sources e.g. to refer to WOTemplate while the accumulators refer to the WO being built.
		#region - new Template value accumulation
		#region -   The base type-independent interface
		public interface IMerger {
			// This class is only able to merge from a DataFlow value to itself, so it is only really useful for something
			// like the Value associated with a table, where a context (in this case the table's current position) causes the Value
			// to actually refer to different underlying storage locations.
			// The DataFlowValue accessor is used to set the Value.
			Value DataFlowValue { set; }
			// Fetch a "new" value to be included (if non-null) in the accumulated value
			void FetchNewValue();
			// Fetch the accumulated value, combine it with the latest New value, and put the combined result back as a new accumulated value.
			void MergeValue();
		}
		#endregion
		#region -   The basic types merge handler
		public abstract class Merger<VT> : IMerger {
			Value pValue;
			public Value DataFlowValue {
				set { pValue = value; }
			}
			object NewValue;
			public void FetchNewValue() {
				NewValue = pValue.TypeInfo.GenericAsNativeType(pValue.GetValue(), typeof(VT));
			}
			protected abstract VT Combine(VT existingValue, VT newValue);
			public void MergeValue() {
				if (NewValue != null)
					pValue.SetValue(Combine((VT)pValue.TypeInfo.GenericAsNativeType(pValue.GetValue(), typeof(VT)), (VT)NewValue));
			}
		}
		#endregion
		#region -   Specific merge operations
		#region -     LastValueMerger - takes the last value given to it (which would be the one from the most-specific Task)
		public class LastValueMerger<VT> : Merger<VT> {
			protected override VT Combine(VT existingValue, VT newValue) {
				return newValue;
			}
		}
		#endregion
		#region -     TextConcatenateOnEndMerger - Adds new text to the end of existing accumulation
		public class TextConcatenateOnEndMerger : Merger<string> {
			protected override string Combine(string existingValue, string newValue) {
				if (String.IsNullOrEmpty(newValue))
					return existingValue;
				if (String.IsNullOrEmpty(existingValue))
					return newValue;
				return existingValue + System.Environment.NewLine + newValue;
			}
		}
		#endregion
		#region -     TotalCountMerger - Totals up a required integral value
		public class TotalCountMerger : Merger<long> {
			protected override long Combine(long existingValue, long newValue) {
				return existingValue + newValue;
			}
		}
		#endregion
		#region -     TotalTimeSpanMerger - Totals up a required TimeSpan value
		public class TotalTimeSpanMerger : Merger<TimeSpan> {
			protected override TimeSpan Combine(TimeSpan existingValue, TimeSpan newValue) {
				return existingValue + newValue;
			}
		}
		#endregion
		#endregion
		#endregion
		#region - old Template value accumulation
		#region -   the basic Accumulator interface.
		protected interface IAccumulator {
			void Reset(DBIDataRow destRow);
			void Accumulate(DBIDataRow templateRow);
			void Accumulate(object curValue);
			void StoreResult();
		}
		#endregion
		#region -   the generic base-class for Accumulators, with the working-value type being the generic argument.
		/// <summary>
		/// Base class that represents an object that accumulates a value from the outer-most
		/// task to the inner-most one.
		/// </summary>
		protected abstract class Accumulator<WorkingType> : IAccumulator {
			private DBI_Column templateColumn;

			protected DBIDataRow destRow;
			protected DBI_Column destColumn;
			protected WorkingType workingValue;

			protected virtual WorkingType InitialWorkingValue { get { return default(WorkingType); } }
			protected virtual object Result(WorkingType workingValue) { return workingValue; }

			/// <summary>
			/// Create an accumulator that accumulates values from the given template column
			/// and stores the final result in the given column in the row supplied to Reset.
			/// </summary>
			/// <param name="destColumn">The column to store the result in.</param>
			/// <param name="templateColumn">The template column to accumulate</param>
			protected Accumulator(DBI_Column destColumn, DBI_Column templateColumn) {
				this.destColumn = destColumn;
				this.templateColumn = templateColumn;
			}

			public void Reset(DBIDataRow destRow) {
				workingValue = InitialWorkingValue;
				this.destRow = destRow;
			}

			/// <summary>
			/// Allows derived classes to alter how value accumulation is done.
			/// </summary>
			/// <param name="value">The value to accumulate</param>
			protected abstract WorkingType GetNextAccumulation(object curValue, WorkingType workingValue);

			/// <summary>
			/// GetNextAccumulation the next value from the given template row.
			/// </summary>
			/// <param name="templateRow">The template row containing the value to accumulate</param>
			public void Accumulate(DBIDataRow templateRow) {
				Accumulate(templateRow[templateColumn]);
			}

			/// <summary>
			/// Accumulate a value from some place other than the template column of a template row.
			/// </summary>
			/// <param name="curValue"></param>
			public void Accumulate(object curValue) {
				if (curValue != null)
					workingValue = GetNextAccumulation(curValue, workingValue);
			}
			/// <summary>
			/// Store the final accumulated result in the destination row
			/// </summary>
			public void StoreResult() {
				destRow[destColumn] = Result(workingValue);
			}
		}
		#endregion
		#region -   Specific accumulators
		#region -     LastValueAssumulator: Only remembers the last value seen (from the base-most task)
		protected class LastValueAccumulator : Accumulator<object> {
			public LastValueAccumulator(DBI_Column destColumn, DBI_Column templateColumn)
				: base(destColumn, templateColumn) {
			}

			protected override object GetNextAccumulation(object curValue, object workingValue) {
				return curValue;
			}
		}
		#endregion
		#region -     IntervalAccumulator: Adds all durations together. (Durations not nullable)
		protected class IntervalAccumulator : Accumulator<TimeSpan> {
			public IntervalAccumulator(DBI_Column destColumn, DBI_Column templateColumn)
				: base(destColumn, templateColumn) {
			}
			protected override TimeSpan GetNextAccumulation(object value, TimeSpan workingValue) {
				return workingValue + (TimeSpan)value;
			}
		}
		#endregion
		#region -     WorkOrderDurationAccumulator: Total up durations, but store the result as an end date based on the start date in the record.
		protected class WorkOrderDurationAccumulator : IntervalAccumulator {
			protected dsMB.WorkOrderRow DestRow { get { return (dsMB.WorkOrderRow)base.destRow; } }

			public WorkOrderDurationAccumulator(DBI_Column destColumn, DBI_Column templateColumn)
				: base(destColumn, templateColumn) {
			}
			protected override object Result(TimeSpan workingValue) {
				// StartDateEstimate field may be null if not yet supplied or during a 'Cancel' cycle of a calling editor.
				// In this case, just return a null end date as well. The WO cannot be saved unless it is corrected and the values re-accumulated.
				return DestRow[dsMB.Schema.T.WorkOrder.F.StartDateEstimate] == null ? null : (object)DestRow.F.StartDateEstimate.AddDays(workingValue.Days - 1);
			}
		}
		#endregion
		#region -     TextConcatenateOnEndAccumulator: Concatenate text with line breaks between, later text (from derived task) on end.
		protected class TextConcatenateOnEndAccumulator : Accumulator<System.Text.StringBuilder> {
			public TextConcatenateOnEndAccumulator(DBI_Column destColumn, DBI_Column templateColumn)
				: base(destColumn, templateColumn) {
			}
			protected override System.Text.StringBuilder GetNextAccumulation(object value, System.Text.StringBuilder workingValue) {
				if (value == null)
					return workingValue;
				if (workingValue == null)
					workingValue = new System.Text.StringBuilder();
				else
					workingValue.AppendLine();
				workingValue.Append((string)value);
				return workingValue;
			}

			protected override object Result(System.Text.StringBuilder workingValue) {
				return workingValue == null || workingValue.Length == 0 ? null : workingValue.ToString();
			}
		}
		#endregion
		#endregion
		#endregion
		#region Classes for checkpointing and rolling back changes we might make to a DataTable.
		protected DataTableCheckpointer AddDataTableCheckpointer(DataTable t) {
			if (!CheckpointedTables.TryGetValue(t, out DataTableCheckpointer result))
				CheckpointedTables.Add(t, result = new DataTableCheckpointer(t));
			return result;
		}
		private Dictionary<DataTable, DataTableCheckpointer> CheckpointedTables = new Dictionary<DataTable, DataTableCheckpointer>(Thinkage.Libraries.ObjectByReferenceEqualityComparer<DataTable>.Instance);
		private ICheckpointData[] CheckpointDataTables() {
			ICheckpointData[] result = new ICheckpointData[CheckpointedTables.Count];
			int i = 0;
			// We assume this foreach and the one in RollbackDataTables runs in the same order.
			foreach (DataTableCheckpointer cp in CheckpointedTables.Values)
				result[i++] = cp.Checkpoint();
			return result;
		}
		private void RollbackDataTables(ICheckpointData[] checkpoints) {
			int i = 0;
			// We assume this foreach and the one in CheckpointDataTables runs in the same order.
			foreach (DataTableCheckpointer cp in CheckpointedTables.Values)
				cp.Rollback(checkpoints[i++]);
		}

		#endregion
		#region Class to build and merge new records
		protected class RecordBuilder<RT> : IEqualityComparer<object[]> where RT : DBIDataRow {
			#region Construction
			/// <summary>
			/// Define an object used to build new records in the given table, with the ability to have new records merged with previously-created records.
			/// </summary>
			/// <param name="schema"></param>
			public RecordBuilder(PurchaseOrderBuilder owner, DBI_Table schema) {
				Owner = owner;

				List<ChangedDataTablePositioner> dtps = new List<ChangedDataTablePositioner>();
				List<Source> bls = new List<Source>();
				List<DBI_Table> schemas = new List<DBI_Table>();
				for (DBI_Table s = schema; s != null; s = s.VariantBaseTable) {
					schemas.Add(s);
					DataTable t = DS.GetDataTable(s);
					ChangedDataTablePositioner dtp = new ChangedDataTablePositioner(t);
					owner.AddDataTableCheckpointer(t).StartRollbackEvent += delegate (AddChangeDeleteCheckpointer<DataRow, object[]> sender) {
						// We can't remove an entry from the dictionary we are iterating over, so we have to remember all the keys of the entries we want deleted.
						List<object[]> keysToRemove = new List<object[]>();
						foreach (KeyValuePair<object[], RT> kvp in MergeDictionary)
							if (sender.ObjectBeingRemovedByRollback(kvp.Value))
								keysToRemove.Add(kvp.Key);
						for (int i = keysToRemove.Count; --i >= 0;)
							MergeDictionary.Remove(keysToRemove[i]);
					};
					dtps.Add(dtp);
					if (s.VariantBaseRecordIDColumn != null)
						bls.Add(dtp.GetDataColumnSource(GetDataColumn(s.VariantBaseRecordIDColumn)));
				}
				Schemas = schemas.ToArray();
				Positioners = dtps.ToArray();
				BaseLinkages = bls.ToArray();
			}
			/// <summary>
			/// Declare a column in the record or its base records as being a Key for deciding on Merging
			/// </summary>
			/// <param name="p"></param>
			public void KeyField(DBI_Path p) {
				ChangedDataTablePositioner pos = GetPositioner(p, out DBI_Column c);
				KeySources.Add(pos.GetDataColumnSource(GetDataColumn(c)));
			}
			/// <summary>
			/// Declare a column in the record or its base records as being value-accumulated on Merging
			/// </summary>
			/// <param name="p"></param>
			/// <param name="merger"></param>
			public void MergedField(DBI_Path p, IMerger merger) {
				ChangedDataTablePositioner pos = GetPositioner(p, out DBI_Column c);
				merger.DataFlowValue = pos.GetDataColumnValue(GetDataColumn(c));
				FieldMergers.Add(merger);
			}
			/// <summary>
			/// Indicate construction is complete.
			/// Any field that is not a Merged field declared by calling MergedField will retain the values from the original record (i.e. the first instance of a record having
			/// the particular key value)
			/// </summary>
			public void CompleteConstruction() {
				// Verify all fields are accounted for
				MergeDictionary = new Dictionary<object[], RT>(this);
			}
			// TODO (future need) a form of KeyField that just checkes whether a field is null or not, so records with null values in the field do not merge
			// with records with non-null values. This way records with quantities and without would be kept separate, for instance.
			#endregion
			#region Re-use
			/// <summary>
			/// This method is used to clear out the contents of the RecordBuilder's history of created records, to allow it to be re-used "like new".
			/// </summary>
			public void ClearHistory() {
				MergeDictionary.Clear();
			}
			#endregion
			#region Cursor/path/source management
			private ChangedDataTablePositioner GetPositioner(DBI_Path p, out DBI_Column c) {
				c = p.ReferencedColumn;
				DBI_PathToRow ptr = p.PathToContainingRow;
				if (ptr.IsSimple)
					// If VariantBasePaths[0] was the simple path to the same row we would not have to do this...
					return Positioners[0];
				// TODO: Why not just use ptr.Length?
				return Positioners[Array.IndexOf<DBI_PathToRow>(Schemas[0].VariantBasePaths, ptr) + 1];
			}
			private void SetCursorPositions(DBIDataRow row) {
				int i = 0;
				object idValue = row[Schemas[0].InternalIdColumn];

				for (;;) {
					Positioners[i].CurrentPosition = Positioners[i].PositionOfId(idValue);
					if (i >= BaseLinkages.Length)
						break;
					idValue = BaseLinkages[i].GetValue();
					++i;
				}
			}
			private DataColumn GetDataColumn(DBI_Column c) {
				return DS.GetDataTable(c.Table)[c];
			}
			#endregion
			#region Members
			private readonly PurchaseOrderBuilder Owner;
			private DBDataSet DS { get { return Owner.workingDs; } }
			private readonly DBI_Table[] Schemas;
			private readonly ChangedDataTablePositioner[] Positioners;
			private readonly Source[] BaseLinkages;
			private readonly List<Source> KeySources = new List<Source>();
			private readonly List<IMerger> FieldMergers = new List<IMerger>();
			private Dictionary<object[], RT> MergeDictionary;
			#endregion
			#region Record-building methods
			/// <summary>
			/// Create a new row and its bases, returning the derived row.
			/// </summary>
			/// <returns></returns>
			public RT NewRowAndChildren() {
				return (RT)DS.DB.AddNewRowAndBases(DS, Schemas[0]);
			}
			/// <summary>
			/// Note that a row is complete. A check is made to see if its key fields, along with the given mergeKey, match an already-generated record,
			/// in which case the values in the new record are merged to the previous one, the new record is discarded, and the old record is returned.
			/// Otherwise, the checkpointing is informed of the new record, which is also the record returned.
			/// </summary>
			/// <param name="completedRow"></param>
			/// <param name="mergeKey"></param>
			/// <returns></returns>
			public void RowComplete(ref RT completedRow, Guid mergeKey) {
				object[] key = new object[KeySources.Count + 1];
				key[0] = mergeKey;
				SetCursorPositions(completedRow);
				for (int i = KeySources.Count; --i >= 0;)
					key[i + 1] = KeySources[i].GetValue();
				if (MergeDictionary.TryGetValue(key, out RT oldRow)) {
					// fetch the merge values from the completedRow;
					for (int i = FieldMergers.Count; --i >= 0;)
						FieldMergers[i].FetchNewValue();
					// delete completedRow and its base rows;
					for (int i = Positioners.Length; --i >= 0;)
						((ChangedDataTablePositioner.Node)Positioners[i].CurrentPosition).Row.Delete();
					// Merge the values into the oldRow.
					SetCursorPositions(oldRow);
					for (int i = FieldMergers.Count; --i >= 0;)
						FieldMergers[i].MergeValue();
					completedRow = oldRow;
				}
				else
					MergeDictionary.Add(key, completedRow);
			}
			#endregion
			#region IEqualityComparer<object[]> Members
			public bool Equals(object[] x, object[] y) {
				System.Diagnostics.Debug.Assert(x.Length == KeySources.Count + 1);
				System.Diagnostics.Debug.Assert(y.Length == KeySources.Count + 1);
				for (int i = x.Length; --i > 0;)
					if (!KeySources[i - 1].TypeInfo.GenericEquals(x[i], y[i]))
						return false;
				return (Guid)x[0] == (Guid)y[0];
			}

			public int GetHashCode(object[] obj) {
				int result = obj[0].GetHashCode();
				for (int i = obj.Length; --i > 0;)
					// TODO: TypeInfo has a deficiency here; there is no way to get a hashcode for a canonicalized value, so in theory although
					// IntegralTypeInfo would consider (Byte)1 and (long)1 to be equal, there is no garanteed way to ensure they actually generate the
					// same hash code.
					result += obj[i].GetHashCode();
				return result;
			}
			#endregion
		}
		#endregion
		#region Constant members, set from ctor arguments.
		// The dataset we are adding workorder information to
		protected readonly dsMB workingDs;
		// For creation of new purchase orders, a sequence count manager to allocate PO numbers
		protected readonly SequenceCountManager POSequenceCounter;
		// For fetching all the template records.
		protected readonly dsMB lookupDs;
		// Accumulation of resource consumption and costing.
		protected readonly CostingInformationSet Costing;
		#region Row builders
		private readonly RecordBuilder<dsMB.POLineItemRow> POLineItemBuilder;
		private readonly RecordBuilder<dsMB.POLineLaborRow> POLineLaborBuilder;
		private readonly RecordBuilder<dsMB.POLineOtherWorkRow> POLineOtherWorkBuilder;
		private readonly RecordBuilder<dsMB.POLineMiscellaneousRow> POLineMiscellaneousBuilder;
		#endregion
		#endregion
		#region Members changed by SpecifyRecordArguments
		// If it has a value, the ID of the PurchaseOrderState to use for the initial state of the purchase orders instead of
		// the state defined by the PO template or the default state in the PurchaseOrderStateHistory defaults.
		private Guid? poCreationStateIDOverride;
		// The current PurcaseOrder being manipulated
		private dsMB.PurchaseOrderRow purchaseOrderRow;
		// The template row that purchaseOrderRow is based on
		private dsMB.PurchaseOrderTemplateRow purchaseOrderTemplateRow;
		#endregion
		#region Constructor
		/// <summary>
		/// For filling in template details to an existing workorder row in a dataset. Sequence numbering is not provided.
		/// </summary>
		/// <param name="workingDs"></param>
		public PurchaseOrderBuilder(dsMB workingDs) {
			POSequenceCounter = new SequenceCountManager(workingDs.DB, dsMB.Schema.T.PurchaseOrderSequenceCounter, dsMB.Schema.V.POSequence, dsMB.Schema.V.POSequenceFormat);
			this.workingDs = workingDs;
			foreach (DBI_Table t in TargetTableList)
				workingDs.EnsureDataTableExists(t);
			lookupDs = new dsMB(workingDs.DB);
#if DEBUG
			lookupDs.DataSetName = KB.I("PurchaseOrderBuilder.lookupDs");
#endif

			Costing = new CostingInformationSet();

			// Have checkpointing monitor all the tables where we build rows without the help of the RecordBuilder
			AddDataTableCheckpointer(workingDs.T.PurchaseOrderStateHistory);

			// Make the row builders we need.
			POLineItemBuilder = new RecordBuilder<dsMB.POLineItemRow>(this, dsMB.Schema.T.POLineItem);
			POLineItemBuilder.KeyField(dsMB.Path.T.POLineItem.F.ItemLocationID);
			POLineItemBuilder.KeyField(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID);
			// Currently we use the POLineItemTemplate ID as the merge ID so the LineNumber and PurchaseOrderText will be the same on all merged records
			// since they come from the same template records, so we don't need merged-field declarations for them.
			//POLineItemBuilder.MergedField(dsMB.Path.T.POLineItem.F.POLineID.F.LineNumber, ???);
			//POLineItemBuilder.MergedField(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderText, ???);
			POLineItemBuilder.MergedField(dsMB.Path.T.POLineItem.F.Quantity, new TotalCountMerger());
			// dsMB.Path.T.POLineItem.F.POLineID.F.Cost is recalculated each time from scratch after merging.
			POLineItemBuilder.CompleteConstruction();

			POLineLaborBuilder = new RecordBuilder<dsMB.POLineLaborRow>(this, dsMB.Schema.T.POLineLabor);
			POLineLaborBuilder.KeyField(dsMB.Path.T.POLineLabor.F.DemandLaborOutsideID);
			POLineLaborBuilder.KeyField(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderID);
			// Currently we use the POLineLaborTemplate ID as the merge ID so the LineNumber and PurchaseOrderText will be the same on all merged records
			// since they come from the same template records, so we don't need merged-field declarations for them.
			//POLineLaborBuilder.MergedField(dsMB.Path.T.POLineLabor.F.POLineID.F.LineNumber, ???);
			//POLineLaborBuilder.MergedField(dsMB.Path.T.POLineLabor.F.POLineID.F.PurchaseOrderText, ???);
			POLineLaborBuilder.MergedField(dsMB.Path.T.POLineLabor.F.Quantity, new TotalTimeSpanMerger());
			// dsMB.Path.T.POLineLabor.F.POLineID.F.Cost is recalculated each time from scratch after merging.
			POLineLaborBuilder.CompleteConstruction();

			POLineOtherWorkBuilder = new RecordBuilder<dsMB.POLineOtherWorkRow>(this, dsMB.Schema.T.POLineOtherWork);
			POLineOtherWorkBuilder.KeyField(dsMB.Path.T.POLineOtherWork.F.DemandOtherWorkOutsideID);
			POLineOtherWorkBuilder.KeyField(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderID);
			// Currently we use the POLineOtherWorkTemplate ID as the merge ID so the LineNumber and PurchaseOrderText will be the same on all merged records
			// since they come from the same template records, so we don't need merged-field declarations for them.
			//POLineOtherWorkBuilder.MergedField(dsMB.Path.T.POLineOtherWork.F.POLineID.F.LineNumber, ???);
			//POLineOtherWorkBuilder.MergedField(dsMB.Path.T.POLineOtherWork.F.POLineID.F.PurchaseOrderText, ???);
			POLineOtherWorkBuilder.MergedField(dsMB.Path.T.POLineOtherWork.F.Quantity, new TotalTimeSpanMerger());
			// dsMB.Path.T.POLineOtherWork.F.POLineID.F.Cost is recalculated each time from scratch after merging.
			POLineOtherWorkBuilder.CompleteConstruction();

			POLineMiscellaneousBuilder = new RecordBuilder<dsMB.POLineMiscellaneousRow>(this, dsMB.Schema.T.POLineMiscellaneous);
			POLineMiscellaneousBuilder.KeyField(dsMB.Path.T.POLineMiscellaneous.F.MiscellaneousID);
			POLineMiscellaneousBuilder.KeyField(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderID);
			// Currently we use the POLineMiscellaneousTemplate ID as the merge ID so the LineNumber and PurchaseOrderText will be the same on all merged records
			// since they come from the same template records, so we don't need merged-field declarations for them.
			//POLineMiscellaneousBuilder.MergedField(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.LineNumber, ???);
			//POLineMiscellaneousBuilder.MergedField(dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.PurchaseOrderText, ???);
			POLineMiscellaneousBuilder.MergedField(dsMB.Path.T.POLineMiscellaneous.F.Quantity, new TotalCountMerger());
			// dsMB.Path.T.POLineMiscellaneous.F.POLineID.F.Cost is recalculated each time from scratch after merging.
			POLineMiscellaneousBuilder.CompleteConstruction();
		}

		#region - List of tables modified by this Manager (not including Sequence number spoil tables)
		protected virtual Set<DBI_Table> TargetTableList {
			get {
				return new Set<DBI_Table>(pTargetTableList);
			}
		}
		private static readonly DBI_Table[] pTargetTableList = {
			dsMB.Schema.T.PurchaseOrder,
			dsMB.Schema.T.PurchaseOrderStateHistory,
			dsMB.Schema.T.POLine,
			dsMB.Schema.T.POLineItem,
			dsMB.Schema.T.POLineLabor,
			dsMB.Schema.T.POLineMiscellaneous,
			dsMB.Schema.T.POLineOtherWork,
			dsMB.Schema.T.WorkOrderPurchaseOrder
		};
		#endregion
		#endregion
		#region Destruction
		/// <summary>
		/// Disconnect any event handlers and Destroy any SequenceCountManagers, returning unused numbers to the database's pool
		/// </summary>
		public void Destroy() {
			POSequenceCounter.Destroy();
		}
		#endregion
		#region Checkpointing and rollback of row creation and sequence numbers
		protected class CheckpointDataImplementation : ICheckpointData {
			public CheckpointDataImplementation(PurchaseOrderBuilder builder) {
				POSequenceCheckpoint = builder.POSequenceCounter.Checkpoint();
				TableCheckpoints = builder.CheckpointDataTables();
				CostingCheckpoint = builder.Costing.Checkpoint();
			}
			public SequenceCountManager.CheckpointData POSequenceCheckpoint;
			public ICheckpointData[] TableCheckpoints;
			public ICheckpointData CostingCheckpoint;
		}
		/// <summary>
		/// Checkpoint the state of the Sequence counters for rolling back in case the dataset Update fails or is not attempted, or the
		/// client code want to discard what was generated but still use the dataset.
		/// </summary>
		public virtual ICheckpointData Checkpoint() {
			return new CheckpointDataImplementation(this);
		}
		/// <summary>
		/// Roll back the sequence counters and added records to time of the given checkpoint data.
		/// </summary>
		public virtual void Rollback(ICheckpointData data) {
			CheckpointDataImplementation tdata = (CheckpointDataImplementation)data;
			POSequenceCounter.Rollback(tdata.POSequenceCheckpoint);
			RollbackDataTables(tdata.TableCheckpoints);
			Costing.Rollback(tdata.CostingCheckpoint);
		}
		#endregion
		#region public row creation methods
		/// <summary>
		/// Fetch the lookup tables required to build the main record. Call soon before calling SpecifyRecordArguments or FillIn(main record)FromTemplate
		/// </summary>
		public virtual void PopulateLookupTables() {
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.PurchaseOrderTemplate);
		}
		/// <summary>
		/// Fetch the lookup tables required to build the child records. Call soon before calling FillIn(child records)FromTemplate
		/// </summary>
		public virtual void PopulateChildLookupTables() {
			PopulateSpecificPOLineTemplateLookupTables();
		}
		protected void PopulateAllPOLineTemplateLookupTables() {
			// For a derived class that may want to generate Labor POLines, this method is used to get *all* the POLineTemplate derivations.

			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineTemplate);
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineItemTemplate, null, null, new DBI_PathToRow[] {
						dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.PathToReferencedRow,
						dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID.PathToReferencedRow,
						dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ActualItemLocationID.PathToReferencedRow
					});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineMiscellaneousTemplate, null, null, new DBI_PathToRow[] {
						dsMB.Path.T.POLineMiscellaneousTemplate.F.MiscellaneousID.PathToReferencedRow
					});
		}
		private void PopulateSpecificPOLineTemplateLookupTables() {
			// The PO generation by itself does not need Labor POLines.
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineItemTemplate, null, null, new DBI_PathToRow[] {
						dsMB.Path.T.POLineItemTemplate.F.POLineTemplateID.PathToReferencedRow,
						dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.PathToReferencedRow,
						dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ItemPriceID.PathToReferencedRow,
						dsMB.Path.T.POLineItemTemplate.F.ItemLocationID.F.ActualItemLocationID.PathToReferencedRow
					});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineMiscellaneousTemplate, null, null, new DBI_PathToRow[] {
						dsMB.Path.T.POLineMiscellaneousTemplate.F.POLineTemplateID.PathToReferencedRow
					});
		}

		/// <summary>
		/// Specify the arguments for making the next work order.
		/// </summary>
		public void SpecifyRecordArguments(Guid templateID, dsMB.PurchaseOrderRow poRow, Guid? poCreationState) {
			SpecifyRecordArguments(templateID, poRow);
			SpecifyRecordArguments(poCreationState);
		}
		public void SpecifyRecordArguments(Guid? poCreationState) {
			poCreationStateIDOverride = poCreationState;
		}
		public void SpecifyRecordArguments(Guid templateID, dsMB.PurchaseOrderRow poRow) {
			purchaseOrderTemplateRow = lookupDs.T.PurchaseOrderTemplate.RowFind<dsMB.PurchaseOrderTemplateRow>(templateID);
			purchaseOrderRow = poRow;
		}

		public void FillInPurchaseOrderDetailsFromTemplate(DateTime originDate) {
			DateTime requiredBy = originDate + (purchaseOrderTemplateRow.F.RequiredByInterval ?? TimeSpan.Zero);
			POSequenceCounter.ReserveSequence(1);

			// Fill in the PurchaseOrder basic information from the template
			purchaseOrderRow.F.Number = POSequenceCounter.GetFormattedFirstReservedSequence();
			POSequenceCounter.ConsumeFirstReservedSequence();
			purchaseOrderRow.F.RequiredByDate = requiredBy;
			CopyColumns(purchaseOrderRow, purchaseOrderTemplateRow,
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.Comment, dsMB.Schema.T.PurchaseOrder.F.Comment),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.CommentToVendor, dsMB.Schema.T.PurchaseOrder.F.CommentToVendor),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.PaymentTermID, dsMB.Schema.T.PurchaseOrder.F.PaymentTermID),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.ShippingModeID, dsMB.Schema.T.PurchaseOrder.F.ShippingModeID),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.ShipToLocationID, dsMB.Schema.T.PurchaseOrder.F.ShipToLocationID),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.Subject, dsMB.Schema.T.PurchaseOrder.F.Subject),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.VendorID, dsMB.Schema.T.PurchaseOrder.F.VendorID),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.ProjectID, dsMB.Schema.T.PurchaseOrder.F.ProjectID),
				new ColumnCopier(dsMB.Schema.T.PurchaseOrderTemplate.F.PurchaseOrderCategoryID, dsMB.Schema.T.PurchaseOrder.F.PurchaseOrderCategoryID));
		}
		public void UpdateExistingPurchaseOrderDate(DateTime originDate) {
			DateTime requiredBy = originDate + (purchaseOrderTemplateRow.F.RequiredByInterval ?? TimeSpan.Zero);
			if (requiredBy < purchaseOrderRow.F.RequiredByDate)
				purchaseOrderRow.F.RequiredByDate = requiredBy;
		}

		public void FillInPurchaseOrderChildRecords(List<System.Exception> aggregateExceptions, Guid creatorId) {
			AddPOHistoryRecord(creatorId);
			FillInPurchaseOrderChildPOLineRecords(aggregateExceptions);
		}
		public void FillInPurchaseOrderChildPOLineRecords(List<System.Exception> aggregateExceptions) {
			foreach (dsMB.POLineTemplateRow poLineTemplateRow in lookupDs.T.POLineTemplate) {
				if (poLineTemplateRow.F.PurchaseOrderTemplateID != purchaseOrderTemplateRow.F.Id)
					// record is not for the current PO template
					continue;
				if (poLineTemplateRow.F.POLineItemTemplateID.HasValue) {
					dsMB.POLineItemTemplateRow poLineItemTemplateRow = poLineTemplateRow.POLineItemTemplateIDParentRow;
					// See if the row refers to a TemplateTemporaryItemLocation and if so return false. This code only handles permanent ItemLocations.
					if (poLineItemTemplateRow.ItemLocationIDParentRow.F.TemplateItemLocationID.HasValue)
						continue;
					CopyPOLineItemRow(aggregateExceptions, poLineItemTemplateRow, poLineTemplateRow, null);
				}
				else if (poLineTemplateRow.F.POLineMiscellaneousTemplateID.HasValue)
					CopyPOLineMiscellaneousRow(aggregateExceptions, poLineTemplateRow.POLineMiscellaneousTemplateIDParentRow, poLineTemplateRow);
			}
		}
		public void RenumberPOLines() {
			// TODO: For the current PO only?/all PO's generated fix up the numbering for the PO lines.
		}
		protected void VerifyColumns(List<System.Exception> aggregateExceptions, DBIDataRow row, DBI_Table schema) {
			var missingColumns = new Set<DBI_Column>();
			for (DBI_Table t = schema; row != null;) {
				foreach (DBI_Column c in t.Columns)
					// if column doesn't allow null, but isn't writeable, then we have to assume a value will be put there on the server
					if (!c.EffectiveType.AllowNull && c.IsWriteable && row[c] == null)
						missingColumns.Add(c);

				if (t.VariantBaseTable == null)
					break;
				object link = row[t.VariantBaseRecordIDColumn];
				t = t.VariantBaseTable;
				row = ((DBIDataSet)row.Table.DataSet).GetDataTable(t).RowFind(link);
			}
			if (missingColumns.Count == 0)
				return;

			System.Text.StringBuilder message = new System.Text.StringBuilder();
			Strings.Append(message, KB.K("The Task and related records have not provided values for the following required fields in the {0} record"), schema.LabelKey.Translate());
			string comma = KB.I(": ");
			foreach (var c in missingColumns) {
				Strings.Append(message, KB.I("{0}'{1}'"), comma, c.LabelKey.Translate());
				comma = KB.I(", ");
			}
			aggregateExceptions.Add(new NonnullValueRequiredException(KB.T(message.ToString())));
		}
		#endregion
		#region Actual Creation Support
		#region - Costing Support
		protected abstract class BaseCostingInformation {
			public abstract object[] GetState();
			public abstract void SetState(object[] state);
		}
		protected class CostingInformationSet : AddChangeDeleteCheckpointer<BaseCostingInformation, object[]> {
			#region Classes and methods for checkpointing and rolling back costing objects.
			protected class CostingCheckpointDataImplementation : AddChangeDeleteCheckpointDataImplementation<BaseCostingInformation, object[]> {
				public CostingCheckpointDataImplementation(CostingInformationSet checkpointedObject)
					: base(checkpointedObject) {
				}
			}
			protected override object[] GetOldValues(BaseCostingInformation obj) {
				return obj.GetState();
			}
			protected override void StartCombinedRollback() {
				base.StartCombinedRollback();
				// Roll back the Add operations here, since we can only remove objects from our collection by key and not by value.
				List<Guid> keysToRemove = new List<Guid>();
				foreach (KeyValuePair<Guid, BaseCostingInformation> kvp in CostingTable)
					if (ObjectBeingRemovedByRollback(kvp.Value))
						keysToRemove.Add(kvp.Key);
				for (int i = keysToRemove.Count; --i >= 0;)
					CostingTable.Remove(keysToRemove[i]);
			}
			protected override void RollbackAdd(BaseCostingInformation obj) {
				// Add operations were rolled back at the Start event.
			}
			protected override void RollbackChange(BaseCostingInformation obj, object[] oldValues) {
				obj.SetState(oldValues);
			}
			protected override void RollbackDelete(BaseCostingInformation obj, object[] oldValues) {
				// We never Delete, so we never have to roll back a Delete.
				throw new NotImplementedException();
			}
			#endregion
			#region The generic CostingInformation class that actually does the cost calculations
			public class CostingInformation<QT> : BaseCostingInformation where QT : struct, IComparable<QT> {
				public CostingInformation(CostingInformationSet owner, decimal initialCost, QT initialQuantity, decimal remainingCost, QT remainingQuantity) {
					InitialCost = initialCost;
					InitialQuantity = initialQuantity;
					RemainingCost = remainingCost;
					RemainingQuantity = remainingQuantity;

					Owner = owner;
					// If we were passed our key Id we could register ourselves in the collection. We could even have a static method passed a creator-delegate
					// and we could do the TryGetValue etc.

					// Tell the checkpointer (our owner) about our creation
					Owner.ObjectAdded(this);
				}
				public override object[] GetState() {
					return new object[] { InitialCost, InitialQuantity };
				}
				public override void SetState(object[] state) {
					InitialCost = (decimal)state[0];
					InitialQuantity = (QT)state[1];
				}
				public decimal ConsumeAndGetCost(TypeInfo ti, QT? quantity) {
					decimal result = 0;
					if (!quantity.HasValue)
						throw new GeneralException(KB.K("Cannot calculate Demand cost because Quantity is null"));
					QT iq = quantity.Value;
					if (Compute.Greater<QT>(iq, InitialQuantity))
						iq = InitialQuantity;
					QT rq = Compute.Subtract(quantity.Value, iq);
					if (Compute.Greater<QT>(iq, Compute.Zero<QT>())) {
						// TODO: Must round the result based on the TypeInfo (which must be passed to us or our ctor or our owner's ctor)
						result = checked(Compute.Divide(Compute.Multiply(InitialCost, iq), InitialQuantity));
						result = (decimal)ti.GenericAsNativeType(ti.ClosestValueTo(result), typeof(decimal));
						// Tell the checkpointer our state is changing before it changes
						Owner.ObjectChanging(this);
						InitialQuantity = Compute.Subtract(InitialQuantity, iq);
						InitialCost -= result;
					}

					if (Compute.Equal<QT>(rq, Compute.Zero<QT>()))
						return result;

					if (Compute.Equal<QT>(RemainingQuantity, Compute.Zero<QT>()))
						throw new GeneralException(KB.K("Unable to calculate a cost estimate"));
					return (decimal)ti.GenericAsNativeType(ti.ClosestValueTo(checked(result + Compute.Divide(Compute.Multiply(RemainingCost, rq), RemainingQuantity))), typeof(decimal));
				}
				public decimal GetPurchaseCost(TypeInfo ti, QT quantity) {
					if (Compute.Equal<QT>(quantity, Compute.Zero<QT>()))
						return 0;

					if (Compute.Equal<QT>(RemainingQuantity, Compute.Zero<QT>()))
						throw new GeneralException(KB.K("Unable to calculate a purchase cost estimate"));
					decimal result = checked(Compute.Divide(Compute.Multiply(RemainingCost, quantity), RemainingQuantity));
					return (decimal)ti.GenericAsNativeType(ti.ClosestValueTo(result), typeof(decimal));
				}
				private QT InitialQuantity;
				private decimal InitialCost;
				private readonly QT RemainingQuantity;
				private readonly decimal RemainingCost;
				private readonly CostingInformationSet Owner;
			}
			#endregion
			#region Methods to obtain CostingInformation objects for various resource types

			public CostingInformation<long> GetCosting(GetItemPriceRow getItemPriceRow, dsMB.ItemLocationRow ilr) {
				if (!CostingTable.TryGetValue(ilr.F.Id, out BaseCostingInformation result)) {
					dsMB.ActualItemLocationRow ailr = ilr.ActualItemLocationIDParentRow;
					decimal currentCost = ailr.F.TotalCost;
					int currentOnHand = ailr.F.OnHand;
					decimal futureCost = 0;
					int futureQuantity = 0;
					if (ailr.ItemLocationIDParentRow.F.ItemPriceID.HasValue) {
						dsMB.ItemPriceRow ipr = getItemPriceRow(ailr.ItemLocationIDParentRow);
						futureCost = ipr.F.Cost;
						futureQuantity = ipr.F.Quantity;
						if (futureQuantity == 0) { // indeterminate cost will cause error; indicate that no cost information exists 
							throw new GeneralException(KB.K("Item pricing information has no estimated cost"));
						}
					}
					// TODO: Adjust so InitialQuantity is Available rather than OnHand by proportional adjustment, including accounting for
					// the estimated costs of items already on order.
					// Specifically, take OnHand-Reserved at a cost of (OnHand-Reserved)/OnHand*Cost, then add on the costs and quantities
					// not-yet-received of all POLineItem's.
					// For now we use a more simplistic approach that assumes more or less that all items on-order are at the preferred pricing,
					// and we don't account separately for demands and on-orders that cancel each other out.
					// First draw out any reserved at the current cost.
					int reserved = ailr.F.OnReserve;
					if (reserved > currentOnHand)
						reserved = currentOnHand;
					if (reserved > 0) { // this implies currentOnHand != 0 so the division is safe.
						currentCost -= checked(reserved * currentCost / currentOnHand);
						currentOnHand -= reserved;
					}
					// At this point we want to find and total all the POLineItems for this ItemLocation where the quantity ordered > quantity received
					// and add the still-to-come quantity along with the apportioned order price to currentCost and currentOnHand.
					// However, for now we just assume that the total OnOrder quantity is at the ItemLocation's preferred pricing; this is really easy, we just
					// do no further adjustments because the CostingInformation will automatically do this when the currentOnHand is exhausted.
					result = new CostingInformation<long>(this, currentCost, currentOnHand, futureCost, futureQuantity);
					CostingTable.Add(ilr.F.Id, result);
				}
				return (CostingInformation<long>)result;
			}

			public CostingInformation<TimeSpan> GetCosting(dsMB.LaborInsideRow lir) {
				if (!CostingTable.TryGetValue(lir.F.Id, out BaseCostingInformation result)) {
					if (!lir.F.Cost.HasValue)
						throw new GeneralException(KB.K("No cost available"));
					result = new CostingInformation<TimeSpan>(this, 0, TimeSpan.Zero, lir.F.Cost.Value, new TimeSpan(1, 0, 0));
					CostingTable.Add(lir.F.Id, result);
				}
				return (CostingInformation<TimeSpan>)result;
			}
			public CostingInformation<long> GetCosting(dsMB.OtherWorkInsideRow row) {
				if (!CostingTable.TryGetValue(row.F.Id, out BaseCostingInformation result)) {
					if (!row.F.Cost.HasValue)
						throw new GeneralException(KB.K("No cost available"));
					result = new CostingInformation<long>(this, 0, 0, row.F.Cost.Value, 1);
					CostingTable.Add(row.F.Id, result);
				}
				return (CostingInformation<long>)result;
			}
			public CostingInformation<TimeSpan> GetCosting(dsMB.LaborOutsideRow row) {
				if (!CostingTable.TryGetValue(row.F.Id, out BaseCostingInformation result)) {
					if (!row.F.Cost.HasValue)
						throw new GeneralException(KB.K("No cost available"));
					result = new CostingInformation<TimeSpan>(this, 0, TimeSpan.Zero, row.F.Cost.Value, new TimeSpan(1, 0, 0));
					CostingTable.Add(row.F.Id, result);
				}
				return (CostingInformation<TimeSpan>)result;
			}
			public CostingInformation<long> GetCosting(dsMB.OtherWorkOutsideRow row) {
				if (!CostingTable.TryGetValue(row.F.Id, out BaseCostingInformation result)) {
					if (!row.F.Cost.HasValue)
						throw new GeneralException(KB.K("No cost available"));
					result = new CostingInformation<long>(this, 0, 0, row.F.Cost.Value, 1);
					CostingTable.Add(row.F.Id, result);
				}
				return (CostingInformation<long>)result;
			}
			public CostingInformation<long> GetCosting(dsMB.MiscellaneousWorkOrderCostRow row) {
				if (!CostingTable.TryGetValue(row.F.Id, out BaseCostingInformation result)) {
					if (!row.F.Cost.HasValue)
						throw new GeneralException(KB.K("No cost available"));
					result = new CostingInformation<long>(this, 0, 0, row.F.Cost.Value, 1);
					CostingTable.Add(row.F.Id, result);
				}
				return (CostingInformation<long>)result;
			}
			public CostingInformation<long> GetCosting(dsMB.MiscellaneousRow row) {
				if (!CostingTable.TryGetValue(row.F.Id, out BaseCostingInformation result)) {
					if (!row.F.Cost.HasValue)
						throw new GeneralException(KB.K("No cost available"));
					result = new CostingInformation<long>(this, 0, 0, row.F.Cost.Value, 1);
					CostingTable.Add(row.F.Id, result);
				}
				return (CostingInformation<long>)result;
			}
			#endregion
			private Dictionary<Guid, BaseCostingInformation> CostingTable = new Dictionary<Guid, BaseCostingInformation>();
		}
		// Delegate to estimate the cost for a demand row.
		protected delegate RT GetResourceRow<DT, RT>(DT variantDemandRow) where RT : DataRow where DT : DataRow;
		protected delegate string GetPOLineText<DT, RT>(DT variantDemandRow, RT resourceRow) where RT : DataRow where DT : DataRow;
		protected delegate decimal GetCostEstimate<DT, RT>(DT variantDemandRow, RT resourceRow) where RT : DataRow where DT : DataRow;
		protected delegate IExceptionContext GetRecordIdentificationContext<DT, RT>(DT variantDemandRow, RT resourceRow) where RT : DataRow where DT : DataRow;
		#endregion
		#region - Column copying
		// This class defines a simple copy operation from a template or prototype row to a new instance row.
		// It optionally takes a dictionary mapping from one Guid to another; this is used to duplicate linkages between
		// template records into corresponding linkages between instance records.
		// If a mapping table is provided it is assumed that the field being copied is a Guid.
		// TODO: If the field is a linkage field that is not base/derived complain if it links to a Hidden record.
		protected class ColumnCopier {
			private readonly DBI_Column SourceColumn;
			private readonly DBI_Column DestColumn;
			private readonly Dictionary<Guid, Guid> MappingTable;

			public ColumnCopier(DBI_Column sourceColumn, DBI_Column destColumn)
				: this(sourceColumn, destColumn, null) {
			}
			public ColumnCopier(DBI_Column sourceColumn, DBI_Column destColumn, Dictionary<Guid, Guid> mappingTable) {
				System.Diagnostics.Debug.Assert(sourceColumn != null && destColumn != null, KB.I("Must provide non-null columns to ColumnIndexer constructor"));
				SourceColumn = sourceColumn;
				DestColumn = destColumn;
				MappingTable = mappingTable;
			}

			public void CopyValue(DBIDataRow sourceRow, DBIDataRow destRow) {
				object sourceValue = sourceRow[SourceColumn];
				if (sourceValue != null && MappingTable != null) {
					if (MappingTable.TryGetValue((Guid)sourceValue, out Guid replacementValue))
						sourceValue = replacementValue;
				}
				destRow[DestColumn] = sourceValue;
			}
		}

		/// <summary>
		/// Copy multiple columns from one row to another - the column names must match in both rows.
		/// </summary>
		/// <param name="destRow">The row to copy to</param>
		/// <param name="srcRow">The row to copy from</param>
		/// <param name="columnIndexers">The column indexers for the columns to copy.</param>
		protected void CopyColumns(DBIDataRow destRow, DBIDataRow srcRow, params ColumnCopier[] columnIndexers) {
			foreach (ColumnCopier columnIndexer in columnIndexers)
				columnIndexer.CopyValue(srcRow, destRow);
		}

		#endregion
		#region - Child record creation.
		#region -   Purchase Order state history
		private void AddPOHistoryRecord(Guid creatorId) {
			// Setup the PurchaseOrder history record
			dsMB.PurchaseOrderStateHistoryRow poHistory = workingDs.T.PurchaseOrderStateHistory.AddNewPurchaseOrderStateHistoryRow();
			poHistory.F.EffectiveDateReadonly = false;
			poHistory.F.PurchaseOrderID = purchaseOrderRow.F.Id;
			poHistory.F.UserID = creatorId;
			if (poCreationStateIDOverride.HasValue)
				poHistory.F.PurchaseOrderStateID = poCreationStateIDOverride.Value;
			else if (purchaseOrderTemplateRow.F.PurchaseOrderStateID.HasValue)
				poHistory.F.PurchaseOrderStateID = purchaseOrderTemplateRow.F.PurchaseOrderStateID.Value;
			// else default setting left alone when row created.
		}

		#endregion
		#region -   POLine derivations
		protected delegate bool PrescanDerivedTemplateRow(DataRow derivedPOLineTemplateRow);
		protected delegate dsMB.ItemPriceRow GetItemPriceRow(dsMB.ItemLocationRow itemLocationRow);

		protected dsMB.ItemPriceRow GetItemPriceRowFromItemLocationRow(dsMB.ItemLocationRow itemLocationRow) {
			// may have to use the lookupDs directly if we switched to the workingDs for itemLocationRow due to TemporaryItemLocation generated.
			if (!itemLocationRow.F.ItemPriceID.HasValue)
				throw new GeneralException(KB.K("Item Storage Assignment '{0}' has no preferred Item Pricing"), itemLocationRow.F.Code);
			else if (itemLocationRow.Table.DataSet == lookupDs)
				return itemLocationRow.ItemPriceIDParentRow;
			else
				return (dsMB.ItemPriceRow)lookupDs.T.ItemPrice.RowFind(itemLocationRow.F.ItemPriceID);
		}
		private void CopyPOLineRow<POLT, RT>(List<System.Exception> aggregateExceptions, DBIDataRow variantPOLineTemplateRow, dsMB.POLineTemplateRow poLineTemplateRow, RecordBuilder<POLT> recordBuilder,
			DBI_Table derivedPOLineTemplateTable, DBI_Table derivedPOLineTable,
			GetResourceRow<POLT, RT> resourceRowFinder, GetPOLineText<POLT, RT> poLineTextProvider, GetCostEstimate<POLT, RT> costCalculator, GetRecordIdentificationContext<POLT, RT> contextProvider, params ColumnCopier[] variantColumnsToCopy) where RT : DBIDataRow where POLT : DBIDataRow {

			POLT variantPOLineRow = (POLT)workingDs.DB.AddNewRowAndBases(workingDs, derivedPOLineTable);
			dsMB.POLineRow poLineRow = (dsMB.POLineRow)workingDs.T.POLine.RowFind(variantPOLineRow[derivedPOLineTable.VariantBaseRecordIDColumn]);

			// Copy the base poLine info from POLineTemplate to POLine
			poLineRow.F.PurchaseOrderID = purchaseOrderRow.F.Id;
			poLineRow.F.LineNumber = poLineTemplateRow.F.LineNumberRank;
			// Copy the variant poLine info from POLineXXXTemplate to POLineXXX
			CopyColumns(variantPOLineRow, variantPOLineTemplateRow, variantColumnsToCopy);
			// Get the resource row
			RT resourceRow = resourceRowFinder(variantPOLineRow);

			try {
				poLineRow.F.PurchaseOrderText = poLineTextProvider(variantPOLineRow, resourceRow);
				recordBuilder.RowComplete(ref variantPOLineRow, (Guid)variantPOLineTemplateRow[derivedPOLineTemplateTable.InternalIdColumn]);
				try {
					poLineRow.F.Cost = costCalculator(variantPOLineRow, resourceRow);
				}
				catch (System.Exception ex) {
					Libraries.Exception.AddContext(ex, new MessageExceptionContext(KB.K("calculating POLine cost")));
					throw;
				}

				VerifyColumns(aggregateExceptions, variantPOLineRow, derivedPOLineTable);
			}
			catch (System.Exception ex) {
				Libraries.Exception.AddContext(ex, contextProvider(variantPOLineRow, resourceRow));
				aggregateExceptions.Add(ex);
			}
		}
		protected void CopyPOLineItemRow(List<System.Exception> aggregateExceptions, dsMB.POLineItemTemplateRow POLineItemTemplateRow, dsMB.POLineTemplateRow POLineTemplateRow, Dictionary<Guid, Guid> itemLocationMap) {
			CopyPOLineRow<dsMB.POLineItemRow, dsMB.ItemLocationRow>(aggregateExceptions, POLineItemTemplateRow, POLineTemplateRow, POLineItemBuilder, dsMB.Schema.T.POLineItemTemplate, dsMB.Schema.T.POLineItem,
				(variantPOLine) => (dsMB.ItemLocationRow)lookupDs.T.ItemLocation.RowFind(variantPOLine.F.ItemLocationID) ?? variantPOLine.ItemLocationIDParentRow,
				(variantPOLine, resourceRow) => GetItemPriceRowFromItemLocationRow(resourceRow).F.PurchaseOrderText,
				(variantPOLine, resourceRow) => Costing.GetCosting(GetItemPriceRowFromItemLocationRow, resourceRow).GetPurchaseCost(dsMB.Schema.T.POLine.F.Cost.EffectiveType, variantPOLine.F.Quantity),
				(variantPOLine, resourceRow) => new MessageExceptionContext(KB.K("using Purchase Template Item referencing Storage Assignment {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.POLineItemTemplate.F.ItemLocationID, dsMB.Schema.T.POLineItem.F.ItemLocationID, itemLocationMap),
				new ColumnCopier(dsMB.Schema.T.POLineItemTemplate.F.Quantity, dsMB.Schema.T.POLineItem.F.Quantity)
			);
		}
		protected void CopyPOLineLaborRow(List<System.Exception> aggregateExceptions, dsMB.POLineLaborTemplateRow POLineLaborTemplateRow, dsMB.POLineTemplateRow POLineTemplateRow, Dictionary<Guid, Guid> demandMap) {
			CopyPOLineRow<dsMB.POLineLaborRow, dsMB.LaborOutsideRow>(aggregateExceptions, POLineLaborTemplateRow, POLineTemplateRow, POLineLaborBuilder, dsMB.Schema.T.POLineLaborTemplate, dsMB.Schema.T.POLineLabor,
				(variantPOLine) => (dsMB.LaborOutsideRow)lookupDs.T.LaborOutside.RowFind(variantPOLine.DemandLaborOutsideIDParentRow.F.LaborOutsideID),
				(variantPOLine, resourceRow) => resourceRow.F.PurchaseOrderText,
				(variantPOLine, resourceRow) => Costing.GetCosting(resourceRow).GetPurchaseCost(dsMB.Schema.T.POLine.F.Cost.EffectiveType, variantPOLine.F.Quantity),
				(variantPOLine, resourceRow) => new MessageExceptionContext(KB.K("using Purchase Template Hourly Outside referencing Hourly Outside {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.POLineLaborTemplate.F.DemandLaborOutsideTemplateID, dsMB.Schema.T.POLineLabor.F.DemandLaborOutsideID, demandMap),
				new ColumnCopier(dsMB.Schema.T.POLineLaborTemplate.F.Quantity, dsMB.Schema.T.POLineLabor.F.Quantity)
			);
		}
		protected void CopyPOLineOtherWorkRow(List<System.Exception> aggregateExceptions, dsMB.POLineOtherWorkTemplateRow POLineOtherWorkTemplateRow, dsMB.POLineTemplateRow POLineTemplateRow, Dictionary<Guid, Guid> demandMap) {
			CopyPOLineRow<dsMB.POLineOtherWorkRow, dsMB.OtherWorkOutsideRow>(aggregateExceptions, POLineOtherWorkTemplateRow, POLineTemplateRow, POLineOtherWorkBuilder, dsMB.Schema.T.POLineOtherWorkTemplate, dsMB.Schema.T.POLineOtherWork,
				(variantPOLine) => (dsMB.OtherWorkOutsideRow)lookupDs.T.OtherWorkOutside.RowFind(variantPOLine.DemandOtherWorkOutsideIDParentRow.F.OtherWorkOutsideID),
				(variantPOLine, resourceRow) => resourceRow.F.PurchaseOrderText,
				(variantPOLine, resourceRow) => Costing.GetCosting(resourceRow).GetPurchaseCost(dsMB.Schema.T.POLine.F.Cost.EffectiveType, variantPOLine.F.Quantity),
				(variantPOLine, resourceRow) => new MessageExceptionContext(KB.K("using Purchase Template Per Job Outside referencing Per Job Outside {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.POLineOtherWorkTemplate.F.DemandOtherWorkOutsideTemplateID, dsMB.Schema.T.POLineOtherWork.F.DemandOtherWorkOutsideID, demandMap),
				new ColumnCopier(dsMB.Schema.T.POLineOtherWorkTemplate.F.Quantity, dsMB.Schema.T.POLineOtherWork.F.Quantity)
			);
		}
		protected void CopyPOLineMiscellaneousRow(List<System.Exception> aggregateExceptions, dsMB.POLineMiscellaneousTemplateRow POLineMiscellaneousTemplateRow, dsMB.POLineTemplateRow POLineTemplateRow) {
			CopyPOLineRow<dsMB.POLineMiscellaneousRow, dsMB.MiscellaneousRow>(aggregateExceptions, POLineMiscellaneousTemplateRow, POLineTemplateRow, POLineMiscellaneousBuilder, dsMB.Schema.T.POLineMiscellaneousTemplate, dsMB.Schema.T.POLineMiscellaneous,
				(variantPOLine) => (dsMB.MiscellaneousRow)lookupDs.T.Miscellaneous.RowFind(variantPOLine.F.MiscellaneousID),
				(variantPOLine, resourceRow) => resourceRow.F.PurchaseOrderText,
				(variantPOLine, resourceRow) => Costing.GetCosting(resourceRow).GetPurchaseCost(dsMB.Schema.T.POLine.F.Cost.EffectiveType, variantPOLine.F.Quantity),
				(variantPOLine, resourceRow) => new MessageExceptionContext(KB.K("using Purchase Template Miscellaneous Item referencing Miscellaneous Item {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.POLineMiscellaneousTemplate.F.MiscellaneousID, dsMB.Schema.T.POLineMiscellaneous.F.MiscellaneousID),
				new ColumnCopier(dsMB.Schema.T.POLineMiscellaneousTemplate.F.Quantity, dsMB.Schema.T.POLineMiscellaneous.F.Quantity)
			);
		}
		#endregion
		#endregion
		#endregion

		#region IDisposable Members
		bool disposed = false;
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (disposed)
					return;
				disposed = true;
				lookupDs.Dispose();
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
	public class WorkOrderBuilder : PurchaseOrderBuilder {
		#region Constant members, set from ctor arguments.
		// The list of accumulators that accumulate values for each task column.
		private readonly List<IAccumulator> accumulators = new List<IAccumulator>();
		// special-purpose accumulators (also in the above list) that we use for Unit-based settings.
		private readonly IAccumulator accessCodeAccumulator;
		private readonly IAccumulator workOrderExpenseModelIDAccumulator;
		#endregion
		#region Members changed by SpecifyRecordArguments
		// The template rows with the most-specialized template first.
		private List<dsMB.WorkOrderTemplateRow> templateRowsSpecializedToBase = new List<dsMB.WorkOrderTemplateRow>();
		// The current (only) work order row being created
		private dsMB.WorkOrderRow currentWorkOrderRow;
		// Priority for the WorkOrderExpenseModel.
		private DatabaseEnums.TaskUnitPriority workOrderExpenseModelPriority;
		// Priority for the AccessCode.
		private DatabaseEnums.TaskUnitPriority accessCodePriority;
		#endregion
		#region Constructor
		/// <summary>
		/// For filling in template details to an existing workorder row in a dataset. Sequence numbering is not provided.
		/// </summary>
		/// <param name="workingDs"></param>
		public WorkOrderBuilder(dsMB workingDs)
			: base(workingDs) {
			// Have checkpointing monitor all the tables where we build rows without the help of the RecordBuilder
			AddDataTableCheckpointer(workingDs.T.WorkOrderStateHistory);
			AddDataTableCheckpointer(workingDs.T.Demand);
			AddDataTableCheckpointer(workingDs.T.DemandItem);
			AddDataTableCheckpointer(workingDs.T.DemandLaborInside);
			AddDataTableCheckpointer(workingDs.T.DemandLaborOutside);
			AddDataTableCheckpointer(workingDs.T.DemandOtherWorkInside);
			AddDataTableCheckpointer(workingDs.T.DemandOtherWorkOutside);
			AddDataTableCheckpointer(workingDs.T.DemandMiscellaneousWorkOrderCost);
			AddDataTableCheckpointer(workingDs.T.PurchaseOrder);
			AddDataTableCheckpointer(workingDs.T.WorkOrderPurchaseOrder);

			// Make the row builders we need.
			TemporaryStorageBuilder = new RecordBuilder<dsMB.TemporaryStorageRow>(this, dsMB.Schema.T.TemporaryStorage);
			TemporaryStorageBuilder.KeyField(dsMB.Path.T.TemporaryStorage.F.ContainingLocationID);
			TemporaryStorageBuilder.KeyField(dsMB.Path.T.TemporaryStorage.F.WorkOrderID);
			TemporaryStorageBuilder.MergedField(dsMB.Path.T.TemporaryStorage.F.LocationID.F.Desc, new LastValueMerger<string>());
			TemporaryStorageBuilder.MergedField(dsMB.Path.T.TemporaryStorage.F.LocationID.F.Comment, new TextConcatenateOnEndMerger());
			TemporaryStorageBuilder.CompleteConstruction();

			TemporaryItemLocationBuilder = new RecordBuilder<dsMB.TemporaryItemLocationRow>(this, dsMB.Schema.T.TemporaryItemLocation);
			TemporaryItemLocationBuilder.KeyField(dsMB.Path.T.TemporaryItemLocation.F.WorkOrderID);
			TemporaryItemLocationBuilder.KeyField(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID);
			TemporaryItemLocationBuilder.KeyField(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID);
			TemporaryItemLocationBuilder.MergedField(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.CostCenterID, new LastValueMerger<Guid>());
			TemporaryItemLocationBuilder.MergedField(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID, new LastValueMerger<Guid>());
			TemporaryItemLocationBuilder.CompleteConstruction();

			// Add the code to purge our mappings on rollback.
			// The rollback operation does not occur within the generation of a single work order, and all our maps except GeneratedPurchaseOrdersByTemplateID
			// get cleared out on each work order. Similarly many of the RecordBuilders don't have to
			// handle rollbacks because we call ClearHistory on them for each new work order. Perhaps we should be creating a new RecordBuilder for each WO in such cases?
			// Thus only table mappings and RecordBuilders relating to PO's would need any rollback handling.
			AddDataTableCheckpointer(workingDs.T.PurchaseOrder).StartRollbackEvent += delegate (AddChangeDeleteCheckpointer<DataRow, object[]> sender) {
				// We can't remove an entry from the dictionary we are iterating over, so we have to remember all the keys of the entries we want deleted.
				List<Guid> keysToRemove = new List<Guid>();
				foreach (KeyValuePair<Guid, dsMB.PurchaseOrderRow> kvp in GeneratedPurchaseOrdersByTemplateID)
					if (sender.ObjectBeingRemovedByRollback(kvp.Value))
						keysToRemove.Add(kvp.Key);
				for (int i = keysToRemove.Count; --i >= 0;)
					GeneratedPurchaseOrdersByTemplateID.Remove(keysToRemove[i]);
			};

			// Define accumulators that store their result in the workorder row.
			// First define last value accumulators that do not provide a default value.
			// ID is not accumulated; it is the identification of the Task record
			// Code is not accumulated; it is the xid of the Task record
			// Hidden is not accumulated; generated tasks always have null in this field
			// ContainingWorkOrderTemplateID is not accumulated; this defines the chain of specializations
			// Desc is not accumulated; this is the user's description for this Task
			// Comment is not accumulated; this is a comment relating to this Task.
			accumulators.Add(new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.Subject, dsMB.Schema.T.WorkOrderTemplate.F.Subject));
			accumulators.Add(new TextConcatenateOnEndAccumulator(dsMB.Schema.T.WorkOrder.F.Description, dsMB.Schema.T.WorkOrderTemplate.F.Description));
			accumulators.Add(new TextConcatenateOnEndAccumulator(dsMB.Schema.T.WorkOrder.F.ClosingComment, dsMB.Schema.T.WorkOrderTemplate.F.ClosingComment));
			accumulators.Add(workOrderExpenseModelIDAccumulator = new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.WorkOrderExpenseModelID, dsMB.Schema.T.WorkOrderTemplate.F.WorkOrderExpenseModelID));
			accumulators.Add(new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.WorkCategoryID, dsMB.Schema.T.WorkOrderTemplate.F.WorkCategoryID));
			accumulators.Add(accessCodeAccumulator = new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.AccessCodeID, dsMB.Schema.T.WorkOrderTemplate.F.AccessCodeID));
			accumulators.Add(new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.ProjectID, dsMB.Schema.T.WorkOrderTemplate.F.ProjectID));
			accumulators.Add(new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.WorkOrderPriorityID, dsMB.Schema.T.WorkOrderTemplate.F.WorkOrderPriorityID));
			accumulators.Add(new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.CloseCodeID, dsMB.Schema.T.WorkOrderTemplate.F.CloseCodeID));
			accumulators.Add(new WorkOrderDurationAccumulator(dsMB.Schema.T.WorkOrder.F.EndDateEstimate, dsMB.Schema.T.WorkOrderTemplate.F.Duration));
			accumulators.Add(new IntervalAccumulator(dsMB.Schema.T.WorkOrder.F.Downtime, dsMB.Schema.T.WorkOrderTemplate.F.Downtime));
			accumulators.Add(new LastValueAccumulator(dsMB.Schema.T.WorkOrder.F.SelectPrintFlag, dsMB.Schema.T.WorkOrderTemplate.F.SelectPrintFlag));
		}
		#region - List of tables modified by this Manager (not including Sequence number spoil tables)
		protected override Set<DBI_Table> TargetTableList {
			get {
				Set<DBI_Table> result = base.TargetTableList;
				result.UnionWith(pTargetTableList);
				return result;
			}
		}
		private static readonly DBI_Table[] pTargetTableList = {
			dsMB.Schema.T.WorkOrder,
			dsMB.Schema.T.WorkOrderStateHistory,
			dsMB.Schema.T.Location,
			dsMB.Schema.T.TemporaryStorage,
			dsMB.Schema.T.ItemLocation,
			dsMB.Schema.T.ActualItemLocation,
			dsMB.Schema.T.TemporaryItemLocation,
			dsMB.Schema.T.Demand,
			dsMB.Schema.T.DemandItem,
			dsMB.Schema.T.DemandLaborInside,
			dsMB.Schema.T.DemandLaborOutside,
			dsMB.Schema.T.DemandOtherWorkInside,
			dsMB.Schema.T.DemandOtherWorkOutside,
			dsMB.Schema.T.DemandMiscellaneousWorkOrderCost,
			dsMB.Schema.T.PurchaseOrder,
			dsMB.Schema.T.PurchaseOrderStateHistory,
			dsMB.Schema.T.POLine,
			dsMB.Schema.T.POLineItem,
			dsMB.Schema.T.POLineLabor,
			dsMB.Schema.T.POLineMiscellaneous,
			dsMB.Schema.T.POLineOtherWork,
			dsMB.Schema.T.WorkOrderPurchaseOrder
		};
		#endregion
		#endregion
		#region public row creation methods
		/// <summary>
		/// Fetch the lookup tables required to build the main record. Call soon before calling SpecifyRecordArguments or FillIn(main record)FromTemplate
		/// </summary>
		public override void PopulateLookupTables() {
			base.PopulateLookupTables();

			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.WorkOrderTemplate);
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.Unit, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.Unit.F.RelativeLocationID.PathToReferencedRow,
					dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.WorkOrderTemplatePurchaseOrderTemplateLinkage);
		}
		/// <summary>
		/// Fetch the lookup tables required to build the child records. Call soon before calling FillIn(child records)FromTemplate
		/// </summary>
		public override void PopulateChildLookupTables() {
			base.PopulateAllPOLineTemplateLookupTables();
			// Populate the lookupDs with all the template records we might need.
			// Fetch the derived PO lines that the base class does not use (the base POLine is fetched by the base class)
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineLaborTemplate);
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.POLineOtherWorkTemplate);

			// Fetch the Storage and ItemLocation templates
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.TemplateItemLocation, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.TemplateTemporaryStorage, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.TemplateTemporaryStorage.F.LocationID.PathToReferencedRow
				});

			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.PathToReferencedRow
				});

			// Fetch the derived Demands and their demanded resources
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandItemTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.PathToReferencedRow,
					dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.ActualItemLocationID.PathToReferencedRow,
					dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.ItemPriceID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandLaborInsideTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandLaborOutsideTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandOtherWorkInsideTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandOtherWorkOutsideTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID.PathToReferencedRow
				});
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID.PathToReferencedRow
				});

			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.WorkOrderExpenseModel);
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.WorkOrderExpenseModelEntry);
			lookupDs.DB.ViewAdditionalRows(lookupDs, dsMB.Schema.T.WorkOrderExpenseCategory);
		}
		/// <summary>
		/// Specify the arguments for making the next work order.
		/// </summary>
		/// <param name="templateID">The template ID. If null, no template rows will be used.</param>
		/// <param name="workorder">the work order row to be built</param>
		/// <param name="effectiveDate">the ffective date to use in the state history record(s)</param>
		public void SpecifyRecordArguments(object templateID, dsMB.WorkOrderRow workorder, Guid? poCreationState, DatabaseEnums.TaskUnitPriority workOrderExpenseModelPriority, DatabaseEnums.TaskUnitPriority accessCodePriority) {
			templateRowsSpecializedToBase.Clear();

			// This loop assumes that the DB has a trigger to prevent circularly-defined templates.
			while (templateID != null) {
				dsMB.WorkOrderTemplateRow row = (dsMB.WorkOrderTemplateRow)lookupDs.T.WorkOrderTemplate.RowFind(templateID);
				templateRowsSpecializedToBase.Add(row);
				templateID = row[dsMB.Schema.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID];  // Use column indexer since value may be null
			}

			currentWorkOrderRow = workorder;
			base.SpecifyRecordArguments(poCreationState);
			this.workOrderExpenseModelPriority = workOrderExpenseModelPriority;
			this.accessCodePriority = accessCodePriority;
		}
		/// <summary>
		/// Build the workorder row in the currentWorkOrderRow given to SpecifyRecordArguments.
		/// At least for now if we throw an exception because an illegal WO record was built, these checks don't occur
		/// until the record is finished, so all the fields are "filled in" per the template.
		/// This code does not undo any changes to the work order row when it throws, and for that matter none of the changes we make are Checkpointed either.
		/// The assumption is that someone else owns the row, and that its contents are a throwaway.
		/// </summary>
		public void FillInWorkOrderFromTemplate(TimeSpan? slackDays) {
			// Fetch the Unit referenced by the current work order only if we need it for prioritized values.
			dsMB.UnitRow unitRow = null;
			// Check UnitLocationID using column indexer since it is a required field but may be null at this point; because the field is not nullable there is no
			// IsXxxNull() method on the typed table.
			// If the unit's LocationId was null, all it means is unitRow is null and we will treat all the priorities as being OnlyTaskValue. Later we will complain
			// about the null UnitLocationID anyway.
			if ((accessCodePriority != DatabaseEnums.TaskUnitPriority.OnlyTaskValue || workOrderExpenseModelPriority != DatabaseEnums.TaskUnitPriority.OnlyTaskValue)
				&& currentWorkOrderRow[dsMB.Schema.T.WorkOrder.F.UnitLocationID] != null)
				unitRow = ((dsMB.LocationRow)lookupDs.T.Location.RowFind(currentWorkOrderRow.F.UnitLocationID)).RelativeLocationIDParentRow.UnitIDParentRow;

			// Reset all the accumulators.
			foreach (IAccumulator accumulator in accumulators)
				accumulator.Reset(currentWorkOrderRow);

			// Get values from the unit if we want the task to be able to override them (since these use LastValueAccumulators)
			if (unitRow != null) {
				if (accessCodePriority == DatabaseEnums.TaskUnitPriority.PreferTaskValue)
					accessCodeAccumulator.Accumulate(unitRow[dsMB.Schema.T.Unit.F.AccessCodeID]);
				if (workOrderExpenseModelPriority == DatabaseEnums.TaskUnitPriority.PreferTaskValue)
					workOrderExpenseModelIDAccumulator.Accumulate(unitRow[dsMB.Schema.T.Unit.F.WorkOrderExpenseModelID]);
			}

			// Get the values from the task chain, starting at the base task and progressing to the most-specialized.
			// If a prioritized value only wants the value from the unit, skip it during task accumulation.
			for (int i = templateRowsSpecializedToBase.Count; --i >= 0;)
				foreach (IAccumulator accumulator in accumulators) {
					if (accumulator == accessCodeAccumulator && accessCodePriority == DatabaseEnums.TaskUnitPriority.OnlyUnitValue)
						continue;
					if (accumulator == workOrderExpenseModelIDAccumulator && workOrderExpenseModelPriority == DatabaseEnums.TaskUnitPriority.OnlyUnitValue)
						continue;
					accumulator.Accumulate(templateRowsSpecializedToBase[i]);
				}

			// Get values from the unit if we want them to be able to override the task values (since these use LastValueAccumulators)
			if (unitRow != null) {
				if (accessCodePriority == DatabaseEnums.TaskUnitPriority.PreferUnitValue || accessCodePriority == DatabaseEnums.TaskUnitPriority.OnlyUnitValue)
					accessCodeAccumulator.Accumulate(unitRow[dsMB.Schema.T.Unit.F.AccessCodeID]);
				if (workOrderExpenseModelPriority == DatabaseEnums.TaskUnitPriority.PreferUnitValue || workOrderExpenseModelPriority == DatabaseEnums.TaskUnitPriority.OnlyUnitValue)
					workOrderExpenseModelIDAccumulator.Accumulate(unitRow[dsMB.Schema.T.Unit.F.WorkOrderExpenseModelID]);
			}

			// Store all the results
			foreach (IAccumulator accumulator in accumulators)
				accumulator.StoreResult();

			// We don't use the .F. accessor in case StartDateEstimate is (improperly) null, which can happen for places like
			// Make Work Order from Task if the user has not (yet) filled in a work start date.
			if (slackDays.HasValue && currentWorkOrderRow[dsMB.Schema.T.WorkOrder.F.StartDateEstimate] != null)
				currentWorkOrderRow.F.WorkDueDate = currentWorkOrderRow.F.EndDateEstimate + slackDays;

			// Verify that required values are not null. TODO (W20110384): IAccumulator.StoreResult should do this, along with any other TypeInfo violations like text too long etc.
			var errors = new List<System.Exception>();
			VerifyColumns(errors, currentWorkOrderRow, dsMB.Schema.T.WorkOrder);
			Libraries.Exception.ThrowIfAggregateExceptions(errors);
		}

		public void ReservePONumbersForWorkOrderTemplate() {
			var POTemplateRowIds = new Set<Guid>();
			for (int i = templateRowsSpecializedToBase.Count; --i >= 0;) {
				foreach (dsMB.WorkOrderTemplatePurchaseOrderTemplateLinkageRow linkRow in lookupDs.T.WorkOrderTemplatePurchaseOrderTemplateLinkage)
					// Check the PO template actually relates to the current WO template rather than just dregs left from the last WO template.
					// Also check for generation of distinct po's
					if (linkRow.F.LinkedWorkOrderTemplateID == templateRowsSpecializedToBase[i].F.Id)
						POTemplateRowIds.Add(linkRow.F.LinkedPurchaseOrderTemplateID);
			}
			POSequenceCounter.ReserveSequence((uint)POTemplateRowIds.Count);
		}

		/// <summary>
		/// Fill in the child records from the work order template whose ID was given to SpecifyRecordArguments (and its containing templates)
		/// into the given work order.  Child records are cumulative - all child records from the template
		/// and all its containing templates are added to the work order.
		/// If we throw an exception we undo all the work we did (all the rows we create are removed again)
		/// </summary>
		public void FillInWorkOrderChildRecordsFromTemplate(Guid? workorderInitialState, Guid creatorId) {
			ICheckpointData startCheckpoint = Checkpoint();
			try {
				// Use the DBI_Column to access the value because it might be null and the row.F. accessor will get an null reference exception
				if (currentWorkOrderRow[dsMB.Schema.T.WorkOrder.F.WorkOrderExpenseModelID] != null) {
					WorkOrderExpenseModelRow = (dsMB.WorkOrderExpenseModelRow)lookupDs.T.WorkOrderExpenseModel.RowFind(currentWorkOrderRow.F.WorkOrderExpenseModelID);
					ValidExpenseCategories = new Set<Guid>();
					foreach (dsMB.WorkOrderExpenseModelEntryRow r in lookupDs.T.WorkOrderExpenseModelEntry)
						if (r.F.WorkOrderExpenseModelID == currentWorkOrderRow.F.WorkOrderExpenseModelID)
							ValidExpenseCategories.Add(r.F.WorkOrderExpenseCategoryID);
				}
				else {
					WorkOrderExpenseModelRow = null;
					ValidExpenseCategories = null;
				}

				ItemLocationTemplateToInstanceMapping = new Dictionary<Guid, Guid>();
				LocationTemplateToInstanceMapping = new Dictionary<Guid, Guid>();
				DemandLaborOutsideTemplateToInstanceMapping = new Dictionary<Guid, Guid>();
				DemandOtherWorkOutsideTemplateToInstanceMapping = new Dictionary<Guid, Guid>();
				PurchaseOrderTemplateToPurchaseOrderRowMapping = new Dictionary<Guid, dsMB.PurchaseOrderRow>();
				// It would be cleaner if we created our RecordBuilders here and discarded them in the finally clause below.
				// However, because it subscribes to DataRow events and also contains a reference to the DataTable, DataTablePositioner and its derivations
				// are not collectable even if abandoned. Each new RecordBuilder we create will build a new one and the old ones won't go away.
				// To avoid this we arrange here to re-use the same RecordBuilders by creating them once only in our ctor, and clearing out their history
				// in our finally clause below.

				CreateWorkOrderStateHistoryRecord(workorderInitialState, creatorId);
				List<System.Exception> aggregateExceptions = new List<System.Exception>();
				for (int i = templateRowsSpecializedToBase.Count; --i >= 0;) {
					try {
						Guid id = templateRowsSpecializedToBase[i].F.Id;

						var taskAggregateExceptions = new List<System.Exception>();
						CreateTemporaryStorageForTemplate(taskAggregateExceptions, id);
						CreateTemporaryItemLocationsForTemplate(taskAggregateExceptions, id);
						CreateDemandsForTemplate(taskAggregateExceptions, id);
						CreatePurchaseOrdersForTemplate(taskAggregateExceptions, id, creatorId);
						Libraries.Exception.ThrowIfAggregateExceptions(taskAggregateExceptions);
					}
					catch (System.Exception ex) {
						Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("while processing Task {0}"), templateRowsSpecializedToBase[i].F.Code));
						aggregateExceptions.Add(ex);
					}
				}
				Libraries.Exception.ThrowIfAggregateExceptions(aggregateExceptions);
			}
			catch {
				Rollback(startCheckpoint);
				throw;
			}
			finally {
				ItemLocationTemplateToInstanceMapping = null;
				LocationTemplateToInstanceMapping = null;
				DemandLaborOutsideTemplateToInstanceMapping = null;
				DemandOtherWorkOutsideTemplateToInstanceMapping = null;
				PurchaseOrderTemplateToPurchaseOrderRowMapping = null;
				TemporaryItemLocationBuilder.ClearHistory();
				TemporaryStorageBuilder.ClearHistory();
			}
		}
		#endregion
		#region Actual Creation Support
		#region - Row builders
		// Logically we re-build these anew for each WO we generate, but pieces of them are not properly collectable so instead we reuse the same
		// one serially by having it clear out its history. So we can make these readonly.
		private readonly RecordBuilder<dsMB.TemporaryStorageRow> TemporaryStorageBuilder;
		private readonly RecordBuilder<dsMB.TemporaryItemLocationRow> TemporaryItemLocationBuilder;
		#endregion
		#region - Mapping Guids from template-related records to instantiated records.
		// These tables provide mappings from the id's of certain template record types to the id's of the corresponding instance record.
		// They are used to map references from other template records. Thus only template record types that are referenced by other template records types
		// (not including derived records) need to be included here.
		//
		// None of these mappings need to be purged of references to records being removed by a Rollback operation, because they are initialized, filled, and cleared
		// before any rollback operation can occur. The exception is GeneratedPurchaseOrdersByTemplateID.
		//
		// Template ItemLocations are referenced by POLineItemTemplate and DemandItemTemplate
		protected Dictionary<Guid, Guid> ItemLocationTemplateToInstanceMapping = null;
		// Template Locations are referenced by TemplateItemLocation records
		protected Dictionary<Guid, Guid> LocationTemplateToInstanceMapping = null;
		// Template Demand Labor are referenced by POLineLaborTemplate
		protected Dictionary<Guid, Guid> DemandLaborOutsideTemplateToInstanceMapping = null;
		// Template Demand Other Work are referenced by POLineOtherWorkTemplate
		protected Dictionary<Guid, Guid> DemandOtherWorkOutsideTemplateToInstanceMapping = null;
		// Although WorkOrderTemplate and PurchaseOrderTemplate are both referenced by WorkOrderTemplatePurchaseOrderTemplate records, we are only ever
		// working on one WO at a time and the code instantiating these records finds the PO itself in a local variable.
		// However, in the case of PO templates, although we never need to map the row ID, we do need to find the instance row itself, so we have a map
		// to the row rather than to the row ID.
		private Dictionary<Guid, dsMB.PurchaseOrderRow> PurchaseOrderTemplateToPurchaseOrderRowMapping = null;

		// Finally, we keep a mapping from PO template to instance which can include PO's instantiated for work orders other than the current work order.
		// This allows purchase orders to be shared across work orders. This table is cleared to create a boundary across which po's are not shared.
		// TODO: This needs a better way of doing selective sharing of PO's.
		protected Dictionary<Guid, dsMB.PurchaseOrderRow> GeneratedPurchaseOrdersByTemplateID = new Dictionary<Guid, dsMB.PurchaseOrderRow>();
		#endregion
		#region - WorkOrderExpenseModelRow - the Expense Model row for currentWorkOrderRow
		// Set at the start of FillInWorkOrderChildRecordsFromTemplate and used only within it and its workers.
		private dsMB.WorkOrderExpenseModelRow WorkOrderExpenseModelRow;
		#endregion
		#region - ValidExpenseCategories - the set of Expense Categories which are valid for currentWorkOrderRow
		// Set at the start of FillInWorkOrderChildRecordsFromTemplate and used only within it and its workers.
		private Set<Guid> ValidExpenseCategories;
		#endregion
		#region - Child record creation.
		#region -   Work Order State History
		// Add the initial WorkOrderStateHistory record for the work order.
		private void CreateWorkOrderStateHistoryRecord(Guid? initialState, Guid creatorId) {
			// Setup the WorkOrder history to the state dictated in the template
			dsMB.WorkOrderStateHistoryRow woHistory = workingDs.T.WorkOrderStateHistory.AddNewWorkOrderStateHistoryRow();
			woHistory.F.EffectiveDateReadonly = true;
			woHistory.F.WorkOrderID = currentWorkOrderRow.F.Id;
			if (initialState.HasValue)
				woHistory.F.WorkOrderStateID = initialState.Value;
			woHistory.F.UserID = creatorId;
		}
		#endregion
		#region -   TemplateTemporaryStorage -> TemporaryStorage
		private void CreateTemporaryStorageForTemplate(List<System.Exception> aggregateExceptions, Guid templateID) {
			foreach (dsMB.TemplateTemporaryStorageRow templateTemporaryStorageRow in lookupDs.T.TemplateTemporaryStorage) {
				if (templateTemporaryStorageRow.F.WorkOrderTemplateID != templateID)
					continue;

				dsMB.TemporaryStorageRow instanceTemporaryStorageRow = TemporaryStorageBuilder.NewRowAndChildren();
				dsMB.LocationRow instanceLocationRow = instanceTemporaryStorageRow.LocationIDParentRow;
				dsMB.LocationRow locationRow = templateTemporaryStorageRow.LocationIDParentRow;
				instanceTemporaryStorageRow.F.ContainingLocationID = templateTemporaryStorageRow.F.ContainingLocationID;
				instanceTemporaryStorageRow.F.WorkOrderID = currentWorkOrderRow.F.Id;
				instanceLocationRow.F.Desc = locationRow.F.Desc;
				instanceLocationRow.F.Comment = locationRow.F.Comment;
				TemporaryStorageBuilder.RowComplete(ref instanceTemporaryStorageRow, Guid.Empty);
				LocationTemplateToInstanceMapping.Add(templateTemporaryStorageRow.F.LocationID, instanceTemporaryStorageRow.F.LocationID);
				VerifyColumns(aggregateExceptions, instanceTemporaryStorageRow, dsMB.Schema.T.TemporaryStorage);
			}
		}
		#endregion
		#region -   TemplateItemLocation -> TemporaryItemLocation
		private void CreateTemporaryItemLocationsForTemplate(List<System.Exception> aggregateExceptions, Guid templateID) {
			foreach (dsMB.TemplateItemLocationRow templateItemLocationRow in lookupDs.T.TemplateItemLocation) {
				// The apparent alternative check: LocationTemplateToInstanceMapping.ContainsKey(templateItemLocationRow.ItemLocationIDParentRow.F.LocationID)
				// will not work because as we are processing a base task, we will "find" the derived task mapped ItemLocations again.
				// TODO: Is it possible to eliminate our templateID argument and just make TemporaryItemLocations for all the TemplateItemLocations whose
				// base ItemLocation.LocationID has been mapped (meaning it was a template storage for this WO). I think so, but it requires restructuring the
				// loop that calls us. It would also change the error-detection priority if there is an error making a TemporaryItemLocation for a baser Task,
				// and also making a Demand or PO (and WO-linked PO lines) for a more-specialized Task. Currently the latter is the diagnosed error.
				if (templateItemLocationRow.ItemLocationIDParentRow.LocationIDParentRow.TemplateTemporaryStorageIDParentRow.F.WorkOrderTemplateID != templateID)
					continue;
				dsMB.TemporaryItemLocationRow instanceTemporaryItemLocationRow = TemporaryItemLocationBuilder.NewRowAndChildren();
				instanceTemporaryItemLocationRow.F.WorkOrderID = currentWorkOrderRow.F.Id;
				dsMB.ActualItemLocationRow instanceActualItemLocationRow = instanceTemporaryItemLocationRow.ActualItemLocationIDParentRow;
				dsMB.ItemLocationRow instanceItemLocationRow = instanceActualItemLocationRow.ItemLocationIDParentRow;
				dsMB.ItemLocationRow itemLocationRow = templateItemLocationRow.ItemLocationIDParentRow;
				if (WorkOrderExpenseModelRow != null)
					instanceActualItemLocationRow.F.CostCenterID = WorkOrderExpenseModelRow.F.NonStockItemHoldingCostCenterID;
				CopyColumns(instanceItemLocationRow, itemLocationRow,
					new ColumnCopier(dsMB.Schema.T.ItemLocation.F.LocationID, dsMB.Schema.T.ItemLocation.F.LocationID, LocationTemplateToInstanceMapping),
					new ColumnCopier(dsMB.Schema.T.ItemLocation.F.ItemID, dsMB.Schema.T.ItemLocation.F.ItemID),
					new ColumnCopier(dsMB.Schema.T.ItemLocation.F.ItemPriceID, dsMB.Schema.T.ItemLocation.F.ItemPriceID)
				);
				TemporaryItemLocationBuilder.RowComplete(ref instanceTemporaryItemLocationRow, Guid.Empty);
				ItemLocationTemplateToInstanceMapping.Add(templateItemLocationRow.F.ItemLocationID, instanceTemporaryItemLocationRow.ActualItemLocationIDParentRow.F.ItemLocationID);
				VerifyColumns(aggregateExceptions, instanceTemporaryItemLocationRow, dsMB.Schema.T.TemporaryItemLocation);
			}
		}
		#endregion
		#region -   TemplateDemands -> Demands
		// Fill in the demands from a single template into the given work order.
		private void CreateDemandsForTemplate(List<System.Exception> aggregateExceptions, Guid templateId) {
			// Copy demand rows from the template to the work order - one call per demand type.
			// TODO: Merge demands (maybe). Multiple demands for the same resource should get totalled. However:
			// a - Miscellaneous has no quantity to total, just cost.
			// b - Can we merge Demands with null and non-null quantities? null and non-null costs?
			// c - What happens when we add a "Detail"/Comment field giving details of this usage of the resource? This would have to be part of the criteria for merging.
			// d - Two DemandTemplates on the same task for the same resource should not merge, but then a DemandTemplate on a base Task names the same resource, which should it
			//		merge to?????
			CopyDemandRows<dsMB.DemandItemRow, dsMB.ItemLocationRow>(aggregateExceptions, templateId, null, dsMB.Schema.T.DemandItemTemplate, dsMB.Schema.T.DemandItem, dsMB.Schema.T.WorkOrderExpenseModel.F.DefaultItemExpenseModelEntryID,
				// The ItemLocation row may actually be in the workingDs if it was a TemporaryItemLocation we generated.
				(variantRow) => (dsMB.ItemLocationRow)lookupDs.T.ItemLocation.RowFind(variantRow.F.ItemLocationID) ?? variantRow.ItemLocationIDParentRow,
				(variantRow, resourceRow) => Costing.GetCosting(GetItemPriceRowFromItemLocationRow, resourceRow).ConsumeAndGetCost(dsMB.Schema.T.Demand.F.CostEstimate.EffectiveType, variantRow.F.Quantity),
				(variantRow, resourceRow) => new MessageExceptionContext(KB.K("using Task Demand Item referencing Storage Assignment {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.DemandItemTemplate.F.ItemLocationID, dsMB.Schema.T.DemandItem.F.ItemLocationID, ItemLocationTemplateToInstanceMapping),
				new ColumnCopier(dsMB.Schema.T.DemandItemTemplate.F.Quantity, dsMB.Schema.T.DemandItem.F.Quantity));

			CopyDemandRows<dsMB.DemandLaborInsideRow, dsMB.LaborInsideRow>(aggregateExceptions, templateId, null, dsMB.Schema.T.DemandLaborInsideTemplate, dsMB.Schema.T.DemandLaborInside, dsMB.Schema.T.WorkOrderExpenseModel.F.DefaultHourlyInsideExpenseModelEntryID,
				(variantRow) => (dsMB.LaborInsideRow)lookupDs.T.LaborInside.RowFind(variantRow.F.LaborInsideID),
				(variantRow, resourceRow) => Costing.GetCosting(resourceRow).ConsumeAndGetCost(dsMB.Schema.T.Demand.F.CostEstimate.EffectiveType, variantRow.F.Quantity),
				(variantRow, resourceRow) => new MessageExceptionContext(KB.K("using Task Demand Hourly Inside referencing Hourly Inside {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.DemandLaborInsideTemplate.F.LaborInsideID, dsMB.Schema.T.DemandLaborInside.F.LaborInsideID),
				new ColumnCopier(dsMB.Schema.T.DemandLaborInsideTemplate.F.Quantity, dsMB.Schema.T.DemandLaborInside.F.Quantity));

			CopyDemandRows<dsMB.DemandLaborOutsideRow, dsMB.LaborOutsideRow>(aggregateExceptions, templateId, DemandLaborOutsideTemplateToInstanceMapping, dsMB.Schema.T.DemandLaborOutsideTemplate, dsMB.Schema.T.DemandLaborOutside, dsMB.Schema.T.WorkOrderExpenseModel.F.DefaultHourlyOutsideExpenseModelEntryID,
				(variantRow) => (dsMB.LaborOutsideRow)lookupDs.T.LaborOutside.RowFind(variantRow.F.LaborOutsideID),
				(variantRow, resourceRow) => Costing.GetCosting(resourceRow).ConsumeAndGetCost(dsMB.Schema.T.Demand.F.CostEstimate.EffectiveType, variantRow.F.Quantity),
				(variantRow, resourceRow) => new MessageExceptionContext(KB.K("using Task Demand Hourly Outside referencing Hourly Outside {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.DemandLaborOutsideTemplate.F.LaborOutsideID, dsMB.Schema.T.DemandLaborOutside.F.LaborOutsideID),
				new ColumnCopier(dsMB.Schema.T.DemandLaborOutsideTemplate.F.Quantity, dsMB.Schema.T.DemandLaborOutside.F.Quantity));

			CopyDemandRows<dsMB.DemandOtherWorkInsideRow, dsMB.OtherWorkInsideRow>(aggregateExceptions, templateId, null, dsMB.Schema.T.DemandOtherWorkInsideTemplate, dsMB.Schema.T.DemandOtherWorkInside, dsMB.Schema.T.WorkOrderExpenseModel.F.DefaultPerJobInsideExpenseModelEntryID,
				(variantRow) => (dsMB.OtherWorkInsideRow)lookupDs.T.OtherWorkInside.RowFind(variantRow.F.OtherWorkInsideID),
				(variantRow, resourceRow) => Costing.GetCosting(resourceRow).ConsumeAndGetCost(dsMB.Schema.T.Demand.F.CostEstimate.EffectiveType, variantRow.F.Quantity),
				(variantRow, resourceRow) => new MessageExceptionContext(KB.K("using Task Demand Per Job Inside referencing Per Job Inside {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID, dsMB.Schema.T.DemandOtherWorkInside.F.OtherWorkInsideID),
				new ColumnCopier(dsMB.Schema.T.DemandOtherWorkInsideTemplate.F.Quantity, dsMB.Schema.T.DemandOtherWorkInside.F.Quantity));

			CopyDemandRows<dsMB.DemandOtherWorkOutsideRow, dsMB.OtherWorkOutsideRow>(aggregateExceptions, templateId, DemandOtherWorkOutsideTemplateToInstanceMapping, dsMB.Schema.T.DemandOtherWorkOutsideTemplate, dsMB.Schema.T.DemandOtherWorkOutside, dsMB.Schema.T.WorkOrderExpenseModel.F.DefaultPerJobOutsideExpenseModelEntryID,
				(variantRow) => (dsMB.OtherWorkOutsideRow)lookupDs.T.OtherWorkOutside.RowFind(variantRow.F.OtherWorkOutsideID),
				(variantRow, resourceRow) => Costing.GetCosting(resourceRow).ConsumeAndGetCost(dsMB.Schema.T.Demand.F.CostEstimate.EffectiveType, ((dsMB.DemandOtherWorkOutsideRow)variantRow).F.Quantity),
				(variantRow, resourceRow) => new MessageExceptionContext(KB.K("using Task Demand Per Job Outside referencing Per Job Outside {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID, dsMB.Schema.T.DemandOtherWorkOutside.F.OtherWorkOutsideID),
				new ColumnCopier(dsMB.Schema.T.DemandOtherWorkOutsideTemplate.F.Quantity, dsMB.Schema.T.DemandOtherWorkOutside.F.Quantity));

			CopyDemandRows<dsMB.DemandMiscellaneousWorkOrderCostRow, dsMB.MiscellaneousWorkOrderCostRow>(aggregateExceptions, templateId, null, dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate, dsMB.Schema.T.DemandMiscellaneousWorkOrderCost, dsMB.Schema.T.WorkOrderExpenseModel.F.DefaultMiscellaneousExpenseModelEntryID,
				(variantRow) => (dsMB.MiscellaneousWorkOrderCostRow)lookupDs.T.MiscellaneousWorkOrderCost.RowFind(variantRow.F.MiscellaneousWorkOrderCostID),
				(variantRow, resourceRow) => Costing.GetCosting(resourceRow).ConsumeAndGetCost(dsMB.Schema.T.Demand.F.CostEstimate.EffectiveType, 1),
				(variantRow, resourceRow) => new MessageExceptionContext(KB.K("using Task Demand Miscellaneous Cost referencing Miscellaneous Cost {0}"), resourceRow.F.Code),
				new ColumnCopier(dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID, dsMB.Schema.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID));

			Libraries.Exception.ThrowIfAggregateExceptions(aggregateExceptions);
		}

		// Copy variant&base demand template rows of a particular derived type to new variant&base demand rows.
		private void CopyDemandRows<DT, RT>(List<System.Exception> aggregateExceptions, Guid templateID, Dictionary<Guid, Guid> derivedRecordMap,
			DBI_Table derivedDemandTemplateTable, DBI_Table derivedDemandTable, DBI_Column expenseModelDefaultModelEntryColumn,
			GetResourceRow<DT, RT> resourceRowFinder, GetCostEstimate<DT, RT> costCalculator, GetRecordIdentificationContext<DT, RT> contextProvider, params ColumnCopier[] variantColumnsToCopy) where RT : DBIDataRow where DT : DBIDataRow {
			foreach (DBIDataRow variantDemandTemplateRow in lookupDs.GetDataTable(derivedDemandTemplateTable)) {
				dsMB.DemandTemplateRow demandTemplateRow = (dsMB.DemandTemplateRow)lookupDs.T.DemandTemplate.RowFind(variantDemandTemplateRow[derivedDemandTemplateTable.VariantBaseRecordIDColumn]);
				if (demandTemplateRow.F.WorkOrderTemplateID != templateID)
					// Spurious records (from a previous WO template in the same generation set)
					continue;
				DT variantDemandRow = (DT)workingDs.DB.AddNewRowAndBases(workingDs, derivedDemandTable);
				dsMB.DemandRow demandRow = (dsMB.DemandRow)workingDs.T.Demand.RowFind(variantDemandRow[derivedDemandTable.VariantBaseRecordIDColumn]);

				if (derivedRecordMap != null)
					derivedRecordMap.Add((Guid)variantDemandTemplateRow[derivedDemandTemplateTable.InternalIdColumn], (Guid)variantDemandRow[derivedDemandTable.InternalIdColumn]);

				// Copy the base demand info from DemandTemplate to Demand
				demandRow.F.WorkOrderID = currentWorkOrderRow.F.Id;
				demandRow.F.DemandActualCalculationInitValue = demandTemplateRow.F.DemandActualCalculationInitValue;
				// Copy the variant demand info from DemandXXXTemplate to DemandXXX
				CopyColumns(variantDemandRow, variantDemandTemplateRow, variantColumnsToCopy);
				// Get the resource row
				RT resourceRow = resourceRowFinder(variantDemandRow);

				try {
					if (WorkOrderExpenseModelRow != null) {
						// Get the WorkOrderExpenseCategory and validate it.
						if (!demandTemplateRow.F.WorkOrderExpenseCategoryID.HasValue) {
							if (WorkOrderExpenseModelRow[expenseModelDefaultModelEntryColumn] == null)
								throw new GeneralException(KB.K("Neither the Demand Template nor the Expense Model {0} specify an Expense Category"), WorkOrderExpenseModelRow.F.Code);
							demandRow.F.WorkOrderExpenseCategoryID = ((dsMB.WorkOrderExpenseModelEntryRow)lookupDs.T.WorkOrderExpenseModelEntry.RowFind(WorkOrderExpenseModelRow[expenseModelDefaultModelEntryColumn])).F.WorkOrderExpenseCategoryID;
						}
						else
							demandRow.F.WorkOrderExpenseCategoryID = demandTemplateRow.F.WorkOrderExpenseCategoryID.Value;
						// Verify that the Category/Model combination is allowed.
						if (!ValidExpenseCategories.Contains(demandRow.F.WorkOrderExpenseCategoryID)) {
							// since it isn't a valid expense category, the expenseCategoryIDParentRow may not be valid in this dataset; Look it up in the lookupDs
							dsMB.WorkOrderExpenseCategoryRow wecr = (dsMB.WorkOrderExpenseCategoryRow)lookupDs.T.WorkOrderExpenseCategory.RowFind(demandRow.F.WorkOrderExpenseCategoryID);
							throw new GeneralException(KB.K("Expense Model {0} does not permit Expense Category {1}"), WorkOrderExpenseModelRow.F.Code, wecr.F.Code);
						}
					}

					if (demandTemplateRow.F.EstimateCost)
						try {
							demandRow.F.CostEstimate = costCalculator(variantDemandRow, resourceRow);
						}
						catch (System.Exception ex) {
							Libraries.Exception.AddContext(ex, new MessageExceptionContext(KB.K("calculating Demand estimated cost")));
							throw;
						}

					VerifyColumns(aggregateExceptions, variantDemandRow, derivedDemandTable);
				}
				catch (System.Exception ex) {
					Libraries.Exception.AddContext(ex, contextProvider(variantDemandRow, resourceRow));
					aggregateExceptions.Add(ex);
				}
			}
		}
		#endregion
		#region -   POLineTemplate linked to task -> POLines
		// Make Purchase Orders from the Purchase Order Templates associated with the given template.
		private void CreatePurchaseOrdersForTemplate(List<System.Exception> aggregateExceptions, Guid woTemplateId, Guid creatorId) {
			foreach (dsMB.WorkOrderTemplatePurchaseOrderTemplateLinkageRow linkRow in lookupDs.T.WorkOrderTemplatePurchaseOrderTemplateLinkage) {
				// Check the PO template actually relates to the current WO template rather than just dregs left from the last WO template.
				if (linkRow.F.LinkedWorkOrderTemplateID != woTemplateId)
					continue;
				// RecordBuilder is not well-suited to building the Purchase Order. One reason is that not all the Keys for comparing to merge are fields
				// in the PO record. Another is that we need to distinguish "merging" because we've already done the PO for this WO (where we use the PO
				// unchanged) vs "merging" the PO from another WO (where we update the date and re-do the non-wo-linked items).
				// Finally, there is only one field that is ever different and thus truly needing merging.
				if (PurchaseOrderTemplateToPurchaseOrderRowMapping.TryGetValue(linkRow.F.LinkedPurchaseOrderTemplateID, out dsMB.PurchaseOrderRow purchaseorder)) {
					// We've already instantiated this PO for currentWorkOrderRow, use it as is.
					SpecifyRecordArguments(linkRow.F.LinkedPurchaseOrderTemplateID, purchaseorder);
				}
				else {
					// TODO: Caller should tell us how to key this table, rather than just keying it by template ID and expecting the caller to clear
					// it out at the scope boundary (which assumes each scope is processed to completion before starting another).
					// TODO: If RenumberPOLines only works on the current PO whoever clears this collection must call it before forgetting about the PO.
					// Otherwise the ultimate caller can just renumber all the PO's.
					if (GeneratedPurchaseOrdersByTemplateID.TryGetValue(linkRow.F.LinkedPurchaseOrderTemplateID, out purchaseorder)) {
						// We instantiated this PO template into a PO for another generated WO, update it and increment the counts on the non-linked data.
						SpecifyRecordArguments(linkRow.F.LinkedPurchaseOrderTemplateID, purchaseorder);
						UpdateExistingPurchaseOrderDate(currentWorkOrderRow.F.StartDateEstimate);
						FillInPurchaseOrderChildPOLineRecords(aggregateExceptions);
					}
					else {
						// A brand-new PO.
						purchaseorder = workingDs.T.PurchaseOrder.AddNewPurchaseOrderRow();
						SpecifyRecordArguments(linkRow.F.LinkedPurchaseOrderTemplateID, purchaseorder);
						FillInPurchaseOrderDetailsFromTemplate(currentWorkOrderRow.F.StartDateEstimate);
						FillInPurchaseOrderChildRecords(aggregateExceptions, creatorId);
						GeneratedPurchaseOrdersByTemplateID.Add(linkRow.F.LinkedPurchaseOrderTemplateID, purchaseorder);
						VerifyColumns(aggregateExceptions, purchaseorder, dsMB.Schema.T.PurchaseOrder);
					}
					PurchaseOrderTemplateToPurchaseOrderRowMapping.Add(linkRow.F.LinkedPurchaseOrderTemplateID, purchaseorder);
				}

				// Create the linking record. We use the TableEnum of the view to decide what record type we have.
				dsMB.POLineTemplateRow poLineTemplateRow = linkRow.POLineTemplateIDParentRow;
				switch ((ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage)linkRow.F.TableEnum) {
				case ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.Explicit:
					// Create the explicit linkage record from the PO to the WO if necessary
					LinkPurchaseOrder(aggregateExceptions, purchaseorder.F.Id);
					break;
				case ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingPOLineItemTemplateToTemplateItemLocation:
					// Create POLineItem records for TemporaryItemLocations associated with the WO
					CopyPOLineItemRow(aggregateExceptions, poLineTemplateRow.POLineItemTemplateIDParentRow, poLineTemplateRow, ItemLocationTemplateToInstanceMapping);
					break;
				case ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingLaborDemandTemplate:
					// Create POLineLabor for Labor demands associated with the WO
					CopyPOLineLaborRow(aggregateExceptions, poLineTemplateRow.POLineLaborTemplateIDParentRow, poLineTemplateRow, DemandLaborOutsideTemplateToInstanceMapping);
					break;
				case ViewRecordTypes.WorkOrderTemplatePurchaseOrderTemplateLinkage.UsingOtherWorkDemandTemplate:
					// Create POLineOtherWork for Other Work demands associated with the WO
					CopyPOLineOtherWorkRow(aggregateExceptions, poLineTemplateRow.POLineOtherWorkTemplateIDParentRow, poLineTemplateRow, DemandOtherWorkOutsideTemplateToInstanceMapping);
					break;
				}
			}
		}

		private void LinkPurchaseOrder(List<System.Exception> aggregateExceptions, Guid purchaseorderID) {
			// It's possible to already have a linkage record for the given PO/WO - this happens if multiple
			// nested task/supplemental task entries refer to the same PO template.  Don't generate duplicate linkage
			// records in this case.
			// TODO: Use a RecordBuilder like everyone else does for merging records.
			foreach (dsMB.WorkOrderPurchaseOrderRow wopo in workingDs.T.WorkOrderPurchaseOrder)
				if (wopo.F.PurchaseOrderID == purchaseorderID && wopo.F.WorkOrderID == currentWorkOrderRow.F.Id)
					return;

			dsMB.WorkOrderPurchaseOrderRow linkageRow = workingDs.T.WorkOrderPurchaseOrder.AddNewWorkOrderPurchaseOrderRow();
			linkageRow.F.PurchaseOrderID = purchaseorderID;
			linkageRow.F.WorkOrderID = currentWorkOrderRow.F.Id;
			VerifyColumns(aggregateExceptions, linkageRow, dsMB.Schema.T.WorkOrderPurchaseOrder);
		}
		#endregion
		#endregion
		#endregion
	}
	public class WorkOrderBatchBuilder : WorkOrderBuilder {
		#region Properties
		// For creation of new workorders, a sequence count manager to allocate workorder numbers
		private SequenceCountManager WOSequenceCounter;
		// The Batch row we are generating for
		private dsMB.PMGenerationBatchRow BatchRow;
		#endregion
		#region Constructor

		/// <summary>
		/// This object is intended for building work orders from Task records. Arguments to the ctor are used to sete general parameters to
		/// the creation process. The client can then call Create which creates and validates a work order and associated purchase orders,
		/// or the client can provide their own WO number and call SpecifyTemplate, FillInWorkOrderFromTemplate and FillInChildRecordsFromTemplate
		/// and perform the validation themselves. Note that in the latter case our sequence counter for PO numbers will still be used.
		/// In either case it is necessary to call Committed and Destroy at the same place one would normally do this for SequenceCountManager objects.
		/// </summary>
		/// <param name="workingDs">dsMB dataset</param>
		/// <param name="numberToCreate">number of workorders anticipated being created with calls to Create</param>
		///
		/// <param name="createClosedPOs">Whether to create PO's in a closed state or to use the initial state from the PO template.</param>
		public WorkOrderBatchBuilder(dsMB workingDs)
			: base(workingDs) {
			// Have checkpointing monitor all the tables where we build rows without the help of the RecordBuilder
			AddDataTableCheckpointer(workingDs.T.WorkOrder);

			WOSequenceCounter = new SequenceCountManager(workingDs.DB, dsMB.Schema.T.WorkOrderSequenceCounter, dsMB.Schema.V.WOSequence, dsMB.Schema.V.WOSequenceFormat);
			// TODO: Estimate count of POs to create???
		}
		#endregion
		#region Destruction
		/// <summary>
		/// Disconnect any event handlers and Destroy any SequenceCountManagers, returning unused numbers to the database's pool
		/// </summary>
		public new void Destroy() {
			WOSequenceCounter.Destroy();
			base.Destroy();
		}
		#endregion
		#region Checkpointing and rollback of row creation and sequence numbers
		private new class CheckpointDataImplementation : WorkOrderBuilder.CheckpointDataImplementation {
			public CheckpointDataImplementation(WorkOrderBatchBuilder builder)
				: base(builder) {
				WOSequenceCheckpoint = builder.WOSequenceCounter.Checkpoint();
			}
			public SequenceCountManager.CheckpointData WOSequenceCheckpoint;
		}
		public override ICheckpointData Checkpoint() {
			return new CheckpointDataImplementation(this);
		}
		public override void Rollback(ICheckpointData data) {
			CheckpointDataImplementation tdata = (CheckpointDataImplementation)data;
			WOSequenceCounter.Rollback(tdata.WOSequenceCheckpoint);
			base.Rollback(data);
		}
		#endregion
		#region Record Creation methods
		public void SetCountEstimate(uint count) {
			WOSequenceCounter.ReserveSequence(count);
		}
		/// <summary>
		/// Specify the arguments in common to all the work orders in this batch.
		/// </summary>
		/// <param name="batchRow">the PMGenerationBatch row, from which we extract WO-creation parameters</param>
		public void SpecifyRecordArguments(dsMB.PMGenerationBatchRow batchRow) {
			BatchRow = batchRow;
		}
		/// <summary>
		/// Create a workorder Row in the workingDs, and associated resource records given the parameters.
		/// </summary>
		/// <param name="swoRow">the ScheduledWorkOrder row, from which we extract the template and unit</param>
		/// <param name="workStartDate">Work Starting Date estimate</param>
		/// <returns>the new workorder record</returns>
		public dsMB.WorkOrderRow Create(dsMB.ScheduledWorkOrderRow swoRow, DateTime workStartDate) {
			ICheckpointData checkpoint = Checkpoint();
			dsMB.WorkOrderRow workorder = null;
			try {
				workorder = workingDs.T.WorkOrder.AddNewWorkOrderRow();

				SpecifyRecordArguments(swoRow.F.WorkOrderTemplateID, workorder,
					BatchRow.F.PurchaseOrderCreationStateID,
					(DatabaseEnums.TaskUnitPriority)BatchRow.F.WorkOrderExpenseModelUnitTaskPriority,
					(DatabaseEnums.TaskUnitPriority)BatchRow.F.AccessCodeUnitTaskPriority);
				// If the client does not want PO's merged across work orders, we do this by clearing the cached values here.
				// If we want support for mid-scale merging (e.g. all WO's on a task share or all WO's on a single date share) we would
				// need a more complaex way of managing this since at this level we only create single work orders. Since we have no control over
				// the ordering of creation of multiple work orders we would need a dictionary keyed on the shared value. We would likely just have
				// a base class that can find a PO to be shared, with derivations implementing different sharing criteria.
				// This would be something we set up once with a call to us similar to our base SpecifyRecordARguments which gives the batchRow.
				// Multiple calls to us are typically all on the same batchRow anyway.
				if (!BatchRow.F.SinglePurchaseOrders)
					GeneratedPurchaseOrdersByTemplateID.Clear();

				// Fill in the WorkOrder basic information from the supplied parameters
				// Although we assign a number to the WO later (in AssignWorkOrderNumbers), we must supply a value here because FillInWorkOrderFromTemplate validates all columns.
				// TODO: "" is not within the TypeInfo for this column but this works for now because the only validation is null/nonnull
				// TODO: If the validation in FillInWorkOrderFromTemplate changes to only validate the accumulated columns (per a comment in that area) this would no longer be needed.
				workorder.F.Number = KB.I("");
				workorder.F.UnitLocationID = swoRow.F.UnitLocationID;
				workorder.F.StartDateEstimate = workStartDate;
				List<System.Exception> aggregateExceptions = new List<System.Exception>();
				try {
					// Fill in the rest of the details for the workorder
					FillInWorkOrderFromTemplate(swoRow.F.SlackDays);
				}
				catch (System.Exception ex) {
					aggregateExceptions.Add(ex);
				}
				try {
					// Try to create the child records as well to determine errors even if we failed above
					FillInWorkOrderChildRecordsFromTemplate(BatchRow.F.WorkOrderCreationStateID, BatchRow.F.UserID);
				}
				catch (System.Exception ex) {
					aggregateExceptions.Add(ex);
				}
				Thinkage.Libraries.Exception.ThrowIfAggregateExceptions(aggregateExceptions);
			}
			catch {
				Rollback(checkpoint);
				throw;
			}
			return workorder;
		}
		// TODO: SetCountEstimate is no longer necessary. The loop in Create can count the number of work orders generated.
		// This has not been changed yet pending analysis of the timing of the WOSequenceCounter.ReserveSequence call (which cannot be within a TX)
		// TODO: Generalized sorting with user input needs to consider what data is available in which dataset.
		// Note that even the Unit records may be absent depending on user selection of generate options.
		public void AssignWorkOrderNumbers(params DBI_Path[] sortPaths) {
			ICheckpointData checkpoint = Checkpoint();
			try {
				GeneratedWorkOrderCursorManager woCursorManager = new GeneratedWorkOrderCursorManager(workingDs, lookupDs);
				var woNumber = woCursorManager.GetRootTableDataColumnValue(dsMB.Path.T.WorkOrder.F.Number);

				List<SortKey<Libraries.DataFlow.Source>> sortSources = new List<SortKey<Libraries.DataFlow.Source>>();
				foreach (var p in sortPaths)
					sortSources.Add(new SortKey<Libraries.DataFlow.Source>(woCursorManager.GetPathSource(p), false));
				SortingPositioner sorted = new SortingPositioner(woCursorManager, new KeyProviderFromSources(sortSources.ToArray()));

				for (sorted.CurrentPosition = sorted.StartPosition.Next; !sorted.CurrentPosition.IsEnd; sorted.CurrentPosition = sorted.CurrentPosition.Next) {
					woNumber.SetValue(WOSequenceCounter.GetFormattedFirstReservedSequence());
					WOSequenceCounter.ConsumeFirstReservedSequence();
				}
			}
			catch {
				Rollback(checkpoint);
				throw;
			}
		}


		#endregion
		#region GeneratedWorkOrderCursorManager
		// Used solely to sort generated workorders in workingDs according to records related to the workorder found in
		// lookupDs and to provide DataColumn Values on the Root WorkOrder table to allow changes to be made to the workorder.
		private class GeneratedWorkOrderCursorManager : CursorManagerBase {
			#region Construction
			public GeneratedWorkOrderCursorManager(DBIDataSet workingDataSet, DBIDataSet lookupDataSet)
				: base(dsMB.Schema.T.WorkOrder) {
				workingDs = workingDataSet;
				lookupDs = lookupDataSet;
				CursorIndex.GetNode(dsMB.Path.T.WorkOrder); // Create the root entry (is this necessary?)
			}
			#endregion
			protected DBIDataSet workingDs;
			protected DBIDataSet lookupDs;
			#region New Cursor creation
			protected override DataTable FindTableForNewCursor(DBI_PathToRow pathToTable, DBI_Table slaveTable) {
				if (pathToTable.IsSimple)
					return workingDs.GetDataTable(slaveTable);
				else
					return lookupDs.GetDataTable(slaveTable);
			}
			protected override DataTablePositionerBase CreateTablePositioner(DataTable t) {
				return new ChangedDataTablePositioner(t);
			}
			#endregion
			public Value GetRootTableDataColumnValue(DBI_Path p) {
				System.Diagnostics.Debug.Assert(p.IsSimple && p.Table == dsMB.Schema.T.WorkOrder);
				return ((ChangedDataTablePositioner)RootCursor.Positioner).GetDataColumnValue(RootCursor.Table.Columns[p.ReferencedColumn.Name]);
			}
		}
		#endregion
	}
}
