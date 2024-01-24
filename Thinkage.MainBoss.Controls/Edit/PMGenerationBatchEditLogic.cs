using System;
using System.Collections.Generic;
using System.Threading;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	#region SchedulingBaseEditLogic
	/// <summary>
	/// Common base for generating workorders and/or setting schedule basis. All involve creating/editing PMGenerationBatch records
	/// </summary>
	public class SchedulingBaseEditLogic : EditLogic {
		public SchedulingBaseEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
			if (!WillBeEditingDefaults)
				BatchRowBeingEditedAccessor = RecordManager.GetDirectRowAccessor(dsMB.Path.T.PMGenerationBatch.F.Id, 0, true);
		}
		public dsMB.PMGenerationBatchRow BatchRowBeingEdited {
			get {
				return (dsMB.PMGenerationBatchRow)BatchRowBeingEditedAccessor.Row;
			}
		}
		private readonly RecordManager.DirectRowAccessor BatchRowBeingEditedAccessor;
		// Public datacontrol set by Tbl node in OnCreated attribute
		public IBasicDataControl ScheduledWorkOrderSelectionControl;
	}
	#endregion
	#region Automatically scheduled PM work order batch (PMGenerationBatchEditControl/PMGenerationBatchEditLogic)
	public class PMGenerationBatchBaseEditLogic : SchedulingBaseEditLogic {
		public PMGenerationBatchBaseEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		protected override void SetupDataset() {
			base.SetupDataset();
			if (WillBeEditingDefaults)
				pCommittedDisabler.Enabled = true;
			else
				pPMGenerator = new PMGeneration((dsMB)DataSet);
		}
		protected override void Dispose(bool disposing) {
			if (disposing && PMGenerator != null) {
				// TODO: PMGenerator is also Disposable. Perhaps its Destroy and Dispose semantics should be combined, in which case we can just put it in ObjectsToDispose when we create it.
				PMGenerator.Destroy();
				pPMGenerator.Dispose();
				pPMGenerator = null;
			}
			base.Dispose(disposing);
		}
		#region Properties
		// The committed and uncommitted states both appear to be EdtMode.Edit so ECol access attributes cannot be used to control
		// the readonly state of the WO-creation parameter controls.
		// Thus we have a control-creator delegate in the Tbl which adds the CommittedDisabler to these controls.
		// The derived Logic class controls this disabler.
		internal IDisablerProperties CommittedDisabler {
			get {
				return pCommittedDisabler;
			}
		}
		protected readonly SettableDisablerProperties pCommittedDisabler = new SettableDisablerProperties(null, KB.K("PM Batch has been committed"), false);
		protected PMGeneration PMGenerator {
			get {
				return pPMGenerator;
			}
		}
		private PMGeneration pPMGenerator;

		#endregion
	}
	public class PMGenerationBatchEditLogic : PMGenerationBatchBaseEditLogic {
		#region Construction
		#region - Constructor
		public PMGenerationBatchEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, ModifySubsequentModeRestrictions(subsequentModeRestrictions), initLists) {
		}
		// The sequence Generate-Commit-New-Generate-Close-Ok(discard batch) causes errors because the deletions seem to try to 
		// delete records from the first batch (which fail because of some of the linkages that exist)
		//
		// To avoid this we just suppress all the xxx-and-New functionality from the standard editor by preventing re-entry into New mode.
		// This also has the side effect of eliminating the Next and Previous operations in general.
		private static bool[] ModifySubsequentModeRestrictions(bool[] subsequentModeRestrictions) {
			bool[] result = (bool[])subsequentModeRestrictions.Clone();
			result[(int)EdtMode.New] = false;
			result[(int)EdtMode.Clone] = false;
			return result;
		}
		#endregion
		#region - DataSet setup
		protected override void SetupDataset() {
			base.SetupDataset();
			if (!WillBeEditingDefaults) {
				PMGeneratorInitialCheckpoint = PMGenerator.Checkpoint();
				DB.ViewAdditionalVariables(DataSet, dsMB.Schema.V.PmGenerateInterval);
				int default_end = 1;
				if (((dsMB)DataSet).V.PmGenerateInterval.Value != null)
					default_end = ((dsMB)DataSet).V.PmGenerateInterval.Value.Days;
				InitList.Add(Init.OnLoadNew(dsMB.Path.T.PMGenerationBatch.F.EndDate, new ConstantValue(DateTime.Today.AddDays(default_end))));

				SessionIdSource = RecordManager.GetPathNotifyingSource(dsMB.Path.T.PMGenerationBatch.F.SessionID, 0);
				SessionIdSource.Notify += delegate() {
					UpdateDisablers();
				};
			}

			UpdateDisablers();
			EditStateChanged += delegate() {
				UpdateDisablers();
			};
		}
		#endregion
		#region - State Setup
		protected override void SetupStates() {
			base.SetupStates();
			StateParametersChanged = new EditorState(EdtMode.New, true, true, false);
			AllStates.AddUnique(StateParametersChanged);
			StateParametersUnchanged = new EditorState(EdtMode.New, true, false, false);
			AllStates.AddUnique(StateParametersUnchanged);
			StateUnCommittedChanged = new EditorState(EdtMode.Edit, true, true, false);
			AllStates.AddUnique(StateUnCommittedChanged);
			StateUnCommittedUnchanged = new EditorState(EdtMode.Edit, true, false, false);
			AllStates.AddUnique(StateUnCommittedUnchanged);

			// Define the initial entry state for New mode.
			// This means that the normal new-mode state (StateNewUnchanged) is not reachable, nor is StateNewChanged.
			// Instead we define a Generate command (from StateParameters[Un]changed to StateUnCommitted[Un]changed)
			// and a Commit command (from StateUnCommitted[Un]changed to InitialStatesForModes[(int)EdtMode.Edit])
			// and a Change Parameters command (from StateUnCommitted[Un]changed to StateParameters[Un]changed)
			// Once we get to InitialStatesForModes[(int)EdtMode.Edit] we are in the standard state graph
			// except that because we remove the NewCommandGroup at the end of SetupCommands, transitions back to InitialStatesForModes[(int)EdtMode.New] cannot occur.
			// The only way to have multiple editing history entries is to edit multiple existing (committed) records, and in this case the editor stays firmly within
			// standard edit states.
			InitialStatesForModes[(int)EdtMode.New] = StateParametersUnchanged;

			// Define the state transitions caused by user changes
			UserChangeStateTransitions.Add(StateParametersUnchanged, StateParametersChanged);
			UserChangeStateTransitions.Add(StateUnCommittedUnchanged, StateUnCommittedChanged);

			// Define states where the user can close the form
			CloseCommandEnabledStates.AddUnique(StateParametersUnchanged);
			CloseCommandEnabledStates.AddUnique(StateUnCommittedUnchanged);
		}
		#endregion
		#region - Command Setup
		protected override void SetupCommands() {
			base.SetupCommands();
			// TODO: We could save a button by putting Generate on the Save group and Change Scheduling Parameters on the Cancel group but we would
			// have to remove StateUnCommittedChanged from the Change... command; if the user did a Generate, then altered the WO creation parameters
			// they would have to Cancel first, then Change Scheduling Parameters.
			// The argument that Generate and Commit should not be on the same button to prevent accidental double-click problems is somewhat silly and
			// should really be cured by looking at *why* people might intentionally click more than once
			MutuallyExclusiveCommandSetDeclaration cgd = new MutuallyExclusiveCommandSetDeclaration();
			cgd.Add(new CommandDeclaration(KB.K("Generate"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Calculate the Work Order scheduled dates based on the Unit Maintenance Plans"),
					delegate() {
						// If the user wants to change the parameters after a Generate, essentially the record saved here is deleted and a fresh New onde is created.
						// We must remember the current slot contents for the EditingHistory so that too can be reset.
						PreGenerateEditingHistoryEntry = EditingHistory[CurrentEditingHistoryIndex];
						ValidateBeforeSave();
						// TODO: Force validation of ScheduledWorkOrderSelectionControl
						ICheckpointData PMGeneratorCheckpoint = PMGenerator.Checkpoint();
						System.Text.StringBuilder resultMessage = new System.Text.StringBuilder();
						IProgressDisplay ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Maintenance Schedule Generation"), PMGeneration.GenerationProgressSteps);
						try {
#if true
							PMGenerator.Generate(BatchRowBeingEdited, (Set<object>)ScheduledWorkOrderSelectionControl.Value, resultMessage, ipd);
#else
							PMGenerator.Generate(BatchRowBeingEdited, null, ipd);
#endif
							SaveAction(StateUnCommittedUnchanged);
							LoadExistingRecordAction(StateUnCommittedUnchanged);
						}
						catch {
							PMGenerator.Rollback(PMGeneratorCheckpoint);
							throw;
						}
						finally {
							ipd.Complete(resultMessage.ToString());
						}
					},
					StateUnCommittedUnchanged,
					StateParametersChanged,
					StateParametersUnchanged)));
			cgd.Add(new CommandDeclaration(KB.K("Change Scheduling Parameters"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Discard current generated scheduling and alter scheduling parameters"),
					delegate() {
						// We save the contents of the batch record, delete it and all details using DeleteUncommittedSet), tell the edit control
						// to make a new record, then restore all the batch record contents.
						object[] savedValues = BatchRowBeingEdited.ItemArray;
						DeleteUncommittedSet();
						EditingHistory[CurrentEditingHistoryIndex] = PreGenerateEditingHistoryEntry;
						SuspendBeforeRecordOp();
						LoadNewAction();
						// Update all the columns except for the ID. Note that null in ItemArray means "don't change the value", DBNull means a null value.
						savedValues[((dsMB)DataSet).T.PMGenerationBatch.Columns[dsMB.Schema.T.PMGenerationBatch.InternalIdColumn.Name].Ordinal] = null;
						BatchRowBeingEdited.ItemArray = savedValues;
					},
					StateParametersUnchanged,
					StateTransition.DisallowedModeHandling.AllowInitialState,	// Even if re-entry to New mode is disallowed we still want to be able to do this.
					StateUnCommittedUnchanged,
					StateUnCommittedChanged)));
			CommandGroupDeclarationsInOrder.Insert(0, cgd);

			SaveCommandGroup.Add(new CommandDeclaration(KB.K("Commit"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Create the scheduled Work Orders"),
					delegate() {
						ValidateBeforeSave();
						ICheckpointData PMGeneratorCheckpoint = PMGenerator.Checkpoint();
						System.Text.StringBuilder resultMessage = new System.Text.StringBuilder(); 
						IProgressDisplay ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Maintenance Work Order Creation"), PMGeneration.CommitProgressSteps);
						try {
							PMGenerator.Commit(BatchRowBeingEdited, resultMessage, ipd);
							SaveAction(NormalPostSaveState);
							LoadCurrentRecord();
						}
						catch {
							PMGenerator.Rollback(PMGeneratorCheckpoint);
							throw;
						}
						finally {
							ipd.Complete(resultMessage.ToString());
						}
					},
					InitialStatesForModes[(int)EdtMode.Edit],
					StateUnCommittedUnchanged,
					StateUnCommittedChanged)));
			SaveAndCloseCommandGroup.Add(new CommandDeclaration(KB.K("Commit && Close"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Create the scheduled Work Orders and close this editor"),
					delegate() {
						ValidateBeforeSave();
						ICheckpointData PMGeneratorCheckpoint = PMGenerator.Checkpoint();
						System.Text.StringBuilder resultMessage = new System.Text.StringBuilder();
						IProgressDisplay ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Maintenance Work Order Creation"), PMGeneration.CommitProgressSteps);
						try {
							PMGenerator.Commit(BatchRowBeingEdited, resultMessage, ipd);
							SaveAction(NormalPostSaveState);
							CloseEditorAction();
						}
						catch {
							PMGenerator.Rollback(PMGeneratorCheckpoint);
							throw;
						}
						finally {
							ipd.Complete();
						}
					},
					InitialStatesForModes[(int)EdtMode.Edit],
					StateUnCommittedUnchanged,
					StateUnCommittedChanged)));
			// Add a Commit-and-New to the New command group, which would be enabled when we have a generated but uncommitted batch.
			// Note however that this command does not currently appear because re-entry to New mode is disallowed; see ModifySubsequentModeRestrictions for details why.
			NewCommandGroup.Add(new CommandDeclaration(KB.K("Commit && New"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Create the scheduled Work Orders and create a new batch"),
					delegate() {
						ValidateBeforeSave();
						ICheckpointData PMGeneratorCheckpoint = PMGenerator.Checkpoint();
						System.Text.StringBuilder resultMessage = new System.Text.StringBuilder();
						IProgressDisplay ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Maintenance Work Order Creation"), PMGeneration.CommitProgressSteps);
						try {
							PMGenerator.Commit(BatchRowBeingEdited, resultMessage, ipd);
							SaveAction(NormalPostSaveState);
							LoadNewAction();
						}
						catch {
							PMGenerator.Rollback(PMGeneratorCheckpoint);
							throw;
						}
						finally {
							ipd.Complete(resultMessage.ToString());
						}
					},
				InitialStatesForModes[(int)EdtMode.New],
				StateUnCommittedUnchanged,
				StateUnCommittedChanged)));
			// TODO: Arrange that Cancel works in StateUncommittedChanged
			// TODO: Arrange that the FormClose command works in both our states.
		}
		#endregion
		#endregion
		#region Deletion of saved but uncommitted batch using the Editor's dataset
		public void DeleteUncommittedSet() {
			// remove the uncommitted set: Use the PMGenerator to roll back the dataset and Update it to SQL.
			// We are using the EditLogic's DataSet to do this entirely unbeknownst to it; when we are done (barring errors) all changes will have been accepted.
			// As far as the RecordManager is concerned, the buffer had no changes before we started and there will be no changes once we're done.
			PMGenerator.DeleteUncommittedBatch(BatchRowBeingEdited);
			try {
				DB.Update(DataSet, Libraries.DBILibrary.Server.UpdateOptions.NoConcurrencyCheck);
			}
			catch {
				// Even though we failed to tell SQL about it, we still want the rows gone from the dataset.
				DataSet.AcceptChanges();
			}
		}
		#endregion
		#region Members/Properties
		public NotifyingSource SessionIdSource;
		protected void UpdateDisablers() {
			// TODO: PMGeneration.Generate sets the SessionId column; perhaps we should be doing that with an Init instead, in which case we would not need to check the EditMode here.
			pCommittedDisabler.Enabled = (State != null && EditMode == EdtMode.New) || SessionIdSource == null || SessionIdSource.GetValue() != null;
		}
		public ICheckpointData PMGeneratorInitialCheckpoint;
		private EditingHistoryEntry PreGenerateEditingHistoryEntry;
		#region - Custom edit states
		EditorState StateParametersUnchanged = null;
		EditorState StateParametersChanged = null;
		EditorState StateUnCommittedUnchanged = null;
		EditorState StateUnCommittedChanged = null;
		#endregion
		#endregion
	}
	#endregion
	#region Manually scheduled PM work order (PMGenerationManualScheduledWorkOrderEditControl/PMGenerationManualScheduledWorkOrderEditLogic)
	public class PMGenerationManualScheduledWorkOrderEditLogic : PMGenerationBatchBaseEditLogic {
		#region Constructor
		public PMGenerationManualScheduledWorkOrderEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		#endregion
		#region Command setup
		protected override void SetupCommands() {
			base.SetupCommands();
			// TODO: THe original idea was to replace the InitialState[New] with a special state of our own. The problem is that the existing
			// New command has already been created and transitions to the standard New state. To get around this we need to separate SetupStates and
			// SetupCommands, and have the latter use InitialStates to find the target states for verbs like New etc.
			// The New states are uncommitted states so we want to replace the Save verb with a new caption and new code.
			// First we remove any Print and Clone groups.
			CommandGroupDeclarationsInOrder.Remove(CloneCommandGroup);
			CloneCommandGroup = null;
			CommandGroupDeclarationsInOrder.Remove(PrintCommandGroup);
			PrintCommandGroup = null;
		}
		#endregion
		#region DataSet setup
		protected override void SetupDataset() {
			base.SetupDataset();
			InitList.Add(Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, 1), new EditorPathValue(dsMB.Path.T.PMGenerationBatch.F.EntryDate)));
			InitList.Add(Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, 1), new EditorPathValue(dsMB.Path.T.PMGenerationBatch.F.EntryDate)));

			// We also fetch bogus paths to make sure the SWO, Work Order template, and Schedule records are in the buffer. This is because of
			// lazy code in PMGenerate.SetCurrentDetail that wants all the referenced tables populated.
			RecordManager.GetPathSource(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, 1);
			RecordManager.GetPathSource(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, 1);

			// We set the committed disabler always true. Our Tbl forbids EdtMode.Edit.
			pCommittedDisabler.Enabled = true;
		}
		#endregion
		#region Save handling
		// Because this editor is only called in New mode, and the calling Tbl forbids re-entry into New mode (the CV has NoReentryToNewMode which forbids new-mode re-entry),
		// multi-selections are not allowed, so SaveMultipleRecords will only ever save one record.
		// As a result per-record operations which would normally belong in SaveRecord can be done in SaveMultipleRecords instead.
		// In this case, the Commit operation needs its own transaction(s) to reserve sequence numbers, and so cannot occur within SaveRecord (which is already contained in the
		// transaction created by EditLogic.SaveMultipleRecords). To avoid this, we move the Commit call to SaveMultipleRecords.
		protected override void SaveMultipleRecords(Libraries.DBILibrary.Server.UpdateOptions updateOptions, EditorState postSaveState, IProgress<IProgressDisplay> rP = null, CancellationToken cT = default(CancellationToken)) {
			// This is called once per Save command, from outside the transaction.
			ICheckpointData PMGeneratorCheckpoint = PMGenerator.Checkpoint();
			System.Text.StringBuilder resultMessage = new System.Text.StringBuilder();
			IProgressDisplay ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Maintenance Work Order Creation"), PMGeneration.CommitProgressSteps);
			try {
				PMGenerator.Commit(BatchRowBeingEdited, resultMessage, ipd);
				base.SaveMultipleRecords(updateOptions, postSaveState, rP, cT);
			}
			catch {
				// If we arrive here, any DB changes have already been rolled back, but we have to roll back the PMGenerator's internal state to what it was before this all started.
				PMGenerator.Rollback(PMGeneratorCheckpoint);
				throw;
			}
			finally {
				ipd.Complete(resultMessage.ToString());
			}
		}
		#endregion
	}
	#endregion
	#region Setting of Schedule Basis on multiple unit maintenance plans (ScheduleBasisEditLogic)
	public class ScheduleBasisEditLogic : SchedulingBaseEditLogic {
		// This does not use the usual multiple-new-record code because we only want a single Batch record generated.
		// The normal multiple-save-new code would generate a new batch for each SWO we reschedule.
		#region Construction
		#region - Constructor
		public ScheduleBasisEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		#endregion
		#region - State Setup
		#region - Custom edit states
		EditorState StateScheduleBasisUnchanged = null;
		EditorState StateScheduleBasisChanged = null;
		protected override void SetupStates() {
			base.SetupStates();
			StateScheduleBasisChanged = new EditorState(EdtMode.New, true, true, false);
			AllStates.AddUnique(StateScheduleBasisChanged);
			StateScheduleBasisUnchanged = new EditorState(EdtMode.New, true, false, false);
			AllStates.AddUnique(StateScheduleBasisUnchanged);

			// Define the initial entry state for New mode.
			// This means that the normal new-mode state (StateNewUnchanged) is not reachable, nor is StateNewChanged.
			// Instead we define a Save & Close command (from StateScheduleBasis[Un]changed to StateScheduleBasis[Un]changed)
			// We only permit Save & Close and once started, we do not return to the edit form anyway
			InitialStatesForModes[(int)EdtMode.New] = StateScheduleBasisUnchanged;

			// Define the state transitions caused by user changes
			UserChangeStateTransitions.Add(StateScheduleBasisUnchanged, StateScheduleBasisChanged);

			// Define states where the user can close the form
			CloseCommandEnabledStates.AddUnique(StateScheduleBasisUnchanged);
		}
		#endregion
		#region - Command Setup
		protected override void SetupCommands() {
			base.SetupCommands();

			SaveAndCloseCommandGroup.Add(new CommandDeclaration(KB.K("Save Scheduling Basis"), StateTransitionCommand.NewSingleTargetState(this, KB.K("Save the scheduling basis for each of the selected Unit Maintenance Plans"),
					delegate() {
						ValidateBeforeSave();
						ICheckpointData ScheduleBasisCheckpoint = ScheduleBasisGenerator.Checkpoint();
						IProgressDisplay ipd = CommonUI.UIFactory.CreateProgressDisplay(KB.K("Scheduling Basis Update"), PMGeneration.CommitProgressSteps);
						try {
							ScheduleBasisGenerator.SetBasis(BatchRowBeingEdited, (Set<object>)ScheduledWorkOrderSelectionControl.Value,
									(DateTime)dsMB.Schema.T.PMGenerationDetail.F.WorkStartDate.EffectiveType.GenericAsNativeType(NewScheduleBasisCtrl.Value, typeof(DateTime)), ipd);
							SaveAction(StateScheduleBasisUnchanged);
							CloseEditorAction();
						}
						catch {
							ScheduleBasisGenerator.Rollback(ScheduleBasisCheckpoint);
							throw;
						}
						finally {
							ipd.Complete();
						}
					},
					StateScheduleBasisUnchanged,
					StateScheduleBasisChanged
					)));
		}
		#endregion
		#endregion
		#endregion
		public IBasicDataControl NewScheduleBasisCtrl;
		protected override void SetupDataset() {
			base.SetupDataset();
			ScheduleBasisGenerator = new ScheduleBasis((dsMB)DataSet);
			ObjectsToDispose.Add(ScheduleBasisGenerator);
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "ScheduleBasisGenerator")]
		// ObjectsToDispose has ScheduleBasisGenerator
		protected override void Dispose(bool disposing) {
			if (disposing && ScheduleBasisGenerator != null)
				ScheduleBasisGenerator = null;
			base.Dispose(disposing);
		}
		#region Properties
		private ScheduleBasis ScheduleBasisGenerator;
		#endregion
	}
	#endregion
}
