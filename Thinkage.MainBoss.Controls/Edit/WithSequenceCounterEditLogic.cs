using System;
using System.Collections.Generic;
using System.Threading;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Controls {
	public class WithSequenceCounterEditLogic : WithHistoryColumnEditLogic {
		private MB3ETbl.WithStateHistoryAndSequenceCounterLogicArg SequenceCountingInfo;

		// The following are only set for normal record editing.
		private Source SequenceTargetSource;
		private SequenceCountManager SequenceCounter;
		// The following are only set for default-record editing
		private Source CounterVariableSource;
		private Source CounterVariableOriginalSource;

		public WithSequenceCounterEditLogic(IEditUI control, DBClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		protected override void SetupDataset() {
			base.SetupDataset();

			SequenceCountingInfo = (MB3ETbl.WithStateHistoryAndSequenceCounterLogicArg)ETbl.EditorLogicClassArg;

			if (WillBeEditingDefaults) {
				CounterVariableSource = RecordManager.GetVariableSource(SequenceCountingInfo.CounterVariable);
				CounterVariableOriginalSource = RecordManager.GetVariableOriginalNotifyingSource(SequenceCountingInfo.CounterVariable);
			}
			else {
				SequenceCounter = new SequenceCountManager(DB, SequenceCountingInfo.SpoilTable, SequenceCountingInfo.CounterVariable, SequenceCountingInfo.FormatVariable);
				InitList.Add(Init.OnLoadNewClone(SequenceCountingInfo.SequenceTargetPath, new SequenceCountValue(SequenceCounter)));
				SequenceTargetSource = RecordManager.GetPathSource(SequenceCountingInfo.SequenceTargetPath, 0); // TODO: May need the recordSet as another ctor argument
			}
		}
		protected override System.Data.IsolationLevel SaveTransactionIsolationLevel {
			get {
				// We need a special isolation level if we need to clear the spoils table.
				if (State.EditRecordState == EditRecordStates.Defaults
					&& !SequenceCountingInfo.CounterVariable.EffectiveType.GenericEquals(CounterVariableOriginalSource.GetValue(), CounterVariableSource.GetValue()))
					return SequenceCountManager.ClearSpoilsTableIsolationLevel;
				return base.SaveTransactionIsolationLevel;
			}
		}
		protected override void SaveMultipleRecords(ServerExtensions.UpdateOptions updateOptions, EditorState postSaveState, IProgress<IProgressDisplay> rP = null, CancellationToken cT = default) {
			switch (State.EditRecordState) {
			default:
				base.SaveMultipleRecords(updateOptions, postSaveState, rP, cT);
				break;
			case EditRecordStates.New:
				// We must reserve sufficient sequence numbers for all the records that will be saved in this transaction, and use a try/catch to return Consumed numbers
				// back to Reserved ones if the save fails.
				SequenceCounter.ReserveSequence(CurrentMultiValueInfo == null ? 1 : CurrentMultiValueInfo.MultiValueSelector.ValueCount);
				SequenceCountManager.CheckpointData sequenceCounterCheckpoint = SequenceCounter.Checkpoint();
				try {
					base.SaveMultipleRecords(updateOptions, postSaveState, rP, cT);
				}
				catch {
					SequenceCounter.Rollback(sequenceCounterCheckpoint);
					throw;
				}
				break;
			}
		}
		protected override object[] SaveRecord(ServerExtensions.UpdateOptions updateOptions) {
			switch (State.EditRecordState) {
			case EditRecordStates.Defaults:
				// Need to determine if the user has changed the sequence counter, and if so, clear out the spoils table.
				if (!SequenceCountingInfo.CounterVariable.EffectiveType.GenericEquals(CounterVariableOriginalSource.GetValue(), CounterVariableSource.GetValue()))
					SequenceCountManager.ClearSpoilsTable(DB, SequenceCountingInfo.SpoilTable);
				break;
			case EditRecordStates.New:
				// Need to determine if the user is using the sequence number we may have generated for them; if so, we need to
				// tell the sequenceCounter that we used it.
				SequenceCounter.ConditionallyConsumeFirstReservedSequence((string)SequenceTargetSource.GetValue());
				break;
			}
			return base.SaveRecord(updateOptions);
		}
		// Save any allocated sequence counters that we didn't use.
		public override void Teardown() {
			base.Teardown();
			if (SequenceCounter != null)
				SequenceCounter.Destroy();
		}
	}
}
