using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Sets up and maintains the extra action buttons required for state-history table support.
	/// The buttons are set up according to the MainBoss table being browsed/edited.
	/// </summary>
	public class HistoryActionManager {
		#region nested classes
		// NOTE: Although this implements ICommand it should not be used directly since that does not apply any data-dependent Restrictions; instead
		// GetCommand should be called to obtain an appropriately guarded command.
		private static bool[] SubsequentModeRestrictions;
		static HistoryActionManager() {
			SubsequentModeRestrictions = new bool[(int)EdtMode.Max];
			for (int i = (int)EdtMode.Max; --i >= 0;)
				SubsequentModeRestrictions[i] = false;
		}
		public class Transition : SqlExpression.ILeafTransformation {
			public class CommandForEditor : ICommand {
				public CommandForEditor(Transition transition, Tbl tbl, PathToSourceMapperDelegate mapper) {
					Transition = transition;
					EditTbl = tbl;
					CurrentStateHistoryIdSource = mapper(Transition.Manager.HistoryTable.HistoryTable.MainToCurrentStateHistoryPath);
				}
				private readonly Tbl EditTbl;
				private readonly Transition Transition;
				private readonly NotifyingSource CurrentStateHistoryIdSource;
				public bool RunElevated { get { return false; } }
				public Key Tip {
					get { return Transition.TransitionDefinition.OperationHint; }
				}

				public bool Enabled { get { return true; } }

				public event IEnabledChangedEvent EnabledChanged { add { } remove { } }
				public void Execute() {
					if (Transition.TransitionDefinition.CanTransitionWithoutUI)
						using (SaveRecordNoUIControl ctrl = new SaveRecordNoUIControl(Transition.Manager.DB, EditTbl, EdtMode.New, new object[][] { null }, new bool[(int)EdtMode.Max], Transition.Manager.ParentUI, new List<TblActionNode>[] { constructInitList() }))
							ctrl.Save();
					else
						Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(Transition.Manager.ParentUI.UIFactory, Transition.Manager.DB, EditTbl, EdtMode.New, new object[][] { null }, new bool[(int)EdtMode.Max], // All false, so the only operation is Save & Close.
							new List<TblActionNode>[] { constructInitList() }, Transition.Manager.ParentUI.Form, Transition.Manager.ParentUI.LogicObject.CallEditorsModally, null);
				}
				private List<TblActionNode> constructInitList() {
					List<TblActionNode> initList = new List<TblActionNode>();
					MB3Client.StateHistoryTable h = Transition.Manager.HistoryTable.HistoryTable;
					// By using PathOrFilterTarget here we force the corresponding bound control readonly in new mode. In other modes, the
					// linkage columns should be readonly because they have Captive linkage, and the EffectiveDateReadonlyPath is not bound to any control.
					initList.Add(Init.OnLoadNew(new PathOrFilterTarget(h.HistToMainPath), new ConstantValue((Guid)((EditLogic)Transition.Manager.ParentUI.LogicObject).RootRowIDs[0])));
					if (h.HistEffectiveDateReadonlyPath != null)
						initList.Add(Init.OnLoadNew(new PathOrFilterTarget(h.HistEffectiveDateReadonlyPath), new ConstantValue(!Transition.EffectiveDateEditable)));
					initList.Add(Init.OnLoadNew(new PathOrFilterTarget(h.HistToStatePath), new ConstantValue(Transition.NewState.ID)));
					// Pass what we believe to be the current state history so the editor can check that nothing has changed since our last refresh.
					initList.Add(Init.OnLoadNew(new ControlTarget(TIGeneralMB3.CurrentStateHistoryIdWhenCalledId), new ConstantValue(CurrentStateHistoryIdSource.GetValue())));

					// Optionally propagate the status from the current record
					// Here we use PathTarget so the user can select some other value; all we are doing is setting a default.
					if (Transition.TransitionDefinition.CopyStatusFromPrevious)
						initList.Add(Init.OnLoadNew(new PathTarget(h.HistToStatusPath), new EditorPathValue(new DBI_Path(new DBI_PathToRow(h.HistToMainPath.PathToReferencedRow, h.MainToCurrentStateHistoryPath.PathToReferencedRow), h.HistToStatusPath))));
					// TODO: Optionally pass other initializers from the Transition record to the StateHistory record (e.g. Comment, Status, ...?)
					return initList;
				}
			}
			public class CommandForBrowser : BrowseLogic.CallEditorCommandBase {
				public CommandForBrowser(Transition transition, Tbl tbl, PathToSourceMapperDelegate mapper)
					: base((BrowseLogic)transition.Manager.ParentUI.LogicObject, transition.TransitionDefinition.OperationHint, EdtMode.New, tbl, null, null) {
					Transition = transition;
					CurrentStateHistoryIdSource = mapper(Transition.Manager.HistoryTable.HistoryTable.MainToCurrentStateHistoryPath);
				}
				public readonly Transition Transition;
				private readonly NotifyingSource CurrentStateHistoryIdSource;
				public override Key Tip {
					get {
						return EnabledTip;
					}
				}
				public override bool Enabled {
					get {
						return true;
					}
				}
				protected override List<TblActionNode> CalculateInitList() {
					List<TblActionNode> initList = new List<TblActionNode>();
					MB3Client.StateHistoryTable h = Transition.Manager.HistoryTable.HistoryTable;
					// By using PathOrFilterTarget here we force the corresponding bound control readonly in new mode. In other modes, the
					// linkage columns should be readonly because they have Captive linkage, and the EffectiveDateReadonlyPath is not bound to any control.
					initList.Add(Init.OnLoadNew(new PathOrFilterTarget(h.HistToMainPath), new ConstantValue((Guid)Browser.CompositeRecordEditIDSource.GetValue())));
					if (h.HistEffectiveDateReadonlyPath != null)
						initList.Add(Init.OnLoadNew(new PathOrFilterTarget(h.HistEffectiveDateReadonlyPath), new ConstantValue(!Transition.EffectiveDateEditable)));
					initList.Add(Init.OnLoadNew(new PathOrFilterTarget(h.HistToStatePath), new ConstantValue(Transition.NewState.ID)));
					// Pass what we believe to be the current state history so the editor can check that nothing has changed since our last refresh.
					initList.Add(Init.OnLoadNew(new ControlTarget(TIGeneralMB3.CurrentStateHistoryIdWhenCalledId), new ConstantValue(CurrentStateHistoryIdSource.GetValue())));

					// Optionally propagate the status from the current record
					// Here we use PathTarget so the user can select some other value; all we are doing is setting a default.
					if (Transition.TransitionDefinition.CopyStatusFromPrevious)
						initList.Add(Init.OnLoadNew(new PathTarget(h.HistToStatusPath), new EditorPathValue(new DBI_Path(new DBI_PathToRow(h.HistToMainPath.PathToReferencedRow, h.MainToCurrentStateHistoryPath.PathToReferencedRow), h.HistToStatusPath))));
					// TODO: Optionally pass other initializers from the Transition record to the StateHistory record (e.g. Comment, Status, ...?)
					return initList;
				}
				protected override bool[] CalculateSubsequentModeRestrictions() {
					return SubsequentModeRestrictions;
				}
			}
			public class MultiCommandForBrowser : BrowseLogic.CallEditorCommandMulti {
				public MultiCommandForBrowser(BrowseLogic browser)
					: base(browser, null) {
				}
				private bool NoUI;
				private bool ContextualCommandsHaveMixedNoUI;
				protected override Key StartUpdateEnablingLoop() {
					ContextualCommandsHaveMixedNoUI = false;
					return base.StartUpdateEnablingLoop();
				}
				protected override Key AnalyzeSingleRecord(int enabledCount) {
					Key result = base.AnalyzeSingleRecord(enabledCount);
					if (result != null)
						return result;
					var cecb = (CommandForBrowser)FindRootEditCommand(ContextualSingleRecordEditCommand);
					if (enabledCount == 0)
						NoUI = cecb.Transition.TransitionDefinition.CanTransitionWithoutUI;
					else if (NoUI != cecb.Transition.TransitionDefinition.CanTransitionWithoutUI)
						ContextualCommandsHaveMixedNoUI = true;
					return result;
				}
				protected override Key EndUpdateEnablingLoop(ref int enabledCount, ref bool newEnabled, int totalCount) {
					Key result = base.EndUpdateEnablingLoop(ref enabledCount, ref newEnabled, totalCount);
					if (result != null)
						return result;
					if (newEnabled)
						if (ContextualCommandsHaveMixedNoUI)
							return KB.K("This command needs to show a form for some but not all of the selected records");
					return result;
				}
				protected override void CallEditor(object[][] editIdList, List<TblActionNode>[] initLists) {
					if (NoUI)
						using (SaveRecordNoUIControl ctrl = new SaveRecordNoUIControl(Browser.DB, EditTbl, EdtMode.New, new object[initLists.Length][], new bool[(int)EdtMode.Max], Browser.BrowseUI, initLists))
							ctrl.Save();
					else
						base.CallEditor(editIdList, initLists);
				}
			}
			public readonly MB3Client.StateHistoryTransition TransitionDefinition;
			public readonly List<MB3Client.StateFlagRestriction> Restrictions = new List<MB3Client.StateFlagRestriction>();
			public readonly State OldState;
			public readonly State NewState;
			private readonly bool EffectiveDateEditable;
			private readonly HistoryActionManager Manager;
			public readonly DelayedCreateTbl EditTblDefaultCreator;
			public readonly DelayedCreateTbl EditTblCreatorWhenCustomFlagGoesFalse;
			public readonly PermissionDisabler ControllingActionPermission;
			private PathToSourceMapperDelegate PathMapper;

			public Transition(HistoryActionManager manager, DataRow row, Dictionary<Guid, State> stateMap, bool effectiveDateEditable, DelayedCreateTbl editTblToUseByDefault, DelayedCreateTbl editTblToUseWhenCustomFlagGoesFalse) {
				Manager = manager;
				TransitionDefinition = manager.HistoryTable.HistoryTable.StateHistoryTransitionFromRow(row);
				OldState = stateMap[TransitionDefinition.FromStateID];
				NewState = stateMap[TransitionDefinition.ToStateID];
				EffectiveDateEditable = effectiveDateEditable;
				var right = Root.Rights.FindRightByName(TransitionDefinition.RightName);
				System.Diagnostics.Debug.Assert(right != null, "Missing state transition right");
				ControllingActionPermission = (PermissionDisabler)((MainBossPermissionsManager)Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager).GetPermission(right);
				EditTblDefaultCreator = editTblToUseByDefault;
				EditTblCreatorWhenCustomFlagGoesFalse = editTblToUseWhenCustomFlagGoesFalse ?? editTblToUseByDefault;
				foreach (MB3Client.StateFlagRestriction r in NewState.Restrictions)
					if (!OldState.Restrictions.Contains(r))
						Restrictions.Add(r);
			}

			// GetCommand returns the command associated with the Transition including any disablers implied by the Restrictions.
			// TODO: Pass to GetCommand any information required to turn Restrictions[*].Condition into an evaluable quantity,
			// perhaps the HistoryActionManager.PathNotifyingSourceProvider.
			public ICommand GetCommand(PathToSourceMapperDelegate mapper) {
				// TODO: Yuk! Set a member variable so the ILeafTransformation member methods can find it. Yuk!
				PathMapper = mapper;
				var browseLogic = Manager.ParentUI.LogicObject as BrowseLogic;
				ICommand basicCommand;
				// TODO (W20170064): We make the arbitrary choice here that commands for use in editor never use the custom Tbl. In the long run
				// we might need to have the StateHistoryUITable decide this.
				if (browseLogic != null)
					basicCommand = new CommandForBrowser(this, ((OldState.CustomFlag && !NewState.CustomFlag) ? EditTblCreatorWhenCustomFlagGoesFalse : EditTblDefaultCreator).Tbl, mapper);
				else
					basicCommand = new CommandForEditor(this, EditTblDefaultCreator.Tbl, mapper);

				MultiCommandIfAllEnabled result = new MultiCommandIfAllEnabled(basicCommand);

				NotifyingSource currentStateIdSource = mapper(Manager.HistoryTable.HistoryTable.MainToCurrentStatePath);
				var validStateIdSource = new NotifyingSourceWrapper(new ConvertingSource<Guid?, bool>(currentStateIdSource, Libraries.TypeInfo.BoolTypeInfo.NonNullUniverse, id => id == OldState.ID));
				currentStateIdSource.Notify += validStateIdSource.CheckForUnderlyingValueChange;
				result.Add(new DataSourceFalseDisabler(KB.K("This operation is not allowed in the current state"), validStateIdSource));

				if (ControllingActionPermission != null)
					result.Add(ControllingActionPermission);

				foreach (MB3Client.StateFlagRestriction r in Restrictions) {
					// We provide the transformation from path/column leaf nodes into NotifyingSources using the DataPanel's Getxxxx methods,
					// merging multiple occurrences of the same path in an expr to the same source, and collect all the sources into the
					// pathSources dictionary.
					pathSources = new Dictionary<DBI_Path, Thinkage.Libraries.DataFlow.NotifyingSource>();
					var notifyingExprSource = new NotifyingSourceWrapper(r.Condition.TransformLeafNodes(this).GetSource());
					DataSourceFalseDisabler source = new DataSourceFalseDisabler(r.ViolationMessage, notifyingExprSource);
					result.Add(source);
					// Arrange that the DataSourceFalseDisabler re-evaluates any time any of our path sources notifies.
					foreach (NotifyingSource pathSource in pathSources.Values)
						pathSource.Notify += notifyingExprSource.CheckForUnderlyingValueChange;
					pathSources = null;
				}
				return result;
			}
			#region ILeafTransformation Members
			public SqlExpression CustomLeafNode(object customLeafValue, Libraries.TypeInfo.TypeInfo resultType, int currentScopeDepth) {
				throw new System.NotImplementedException();
			}

			private Dictionary<DBI_Path, NotifyingSource> pathSources = null;
			public SqlExpression Path(DBI_Path path, int currentScopeDepth, int netScopeDepth) {
				if (netScopeDepth != 0)
					return null;
				// We keep a collection of all the sources we have obtained from paths so we use the same notifying source for multiple
				// occurrences of the same path. This can cut down slightly on the number of re-evaluations we do. We should actually keep
				// this keyed on a per-Condition basis so when we have multiple Conditions we only re-evaluate the xxxxx.
				NotifyingSource source;
				if (!pathSources.TryGetValue(path, out source)) {
					source = PathMapper(path);
					pathSources[path] = source;
				}
				return new SqlExpression(source);
			}

			public SqlExpression DataFlowSource(Thinkage.Libraries.DataFlow.Source dataFlowSource, int currentScopeDepth) {
				return null;
			}
			#endregion
		}
		// The State class defines each State, 
		public class State : Thinkage.Libraries.XAF.UI.SettableDisablerProperties {
			public readonly Guid ID;
			public readonly List<MB3Client.StateFlagRestriction> Restrictions = new List<MB3Client.StateFlagRestriction>();
			public readonly bool CustomFlag;

			public State(Guid id, MB3Client.StateHistoryTable historyTable, System.Data.DataRow row, DBI_Column customFlagField)
				: base(null, KB.K("This operation is not allowed in the current state"), false) {
				ID = id;
				CustomFlag = (bool?)customFlagField?[row] ?? false;
				foreach (MB3Client.StateFlagRestriction r in historyTable.StateRestrictions)
					if ((bool)r.StateFlagColumn[row] == r.ConditionAppliesWhenFlagIs)
						Restrictions.Add(r);
			}
		}
		#endregion
		#region Properties
		public readonly StateHistoryUITable HistoryTable;
		private readonly ICommonUI ParentUI;
		public readonly List<List<Transition>> Transitions = new List<List<Transition>>();
		#endregion
		#region Construction
		// TODO: Our actions should be properly controlled by permissions. Whether this belongs here or in the StateAction is not clear.
		// In the case in point, the ability to add WO/WR/PO state history records should be controlled by the Create permission on that table.

		// The following delegate returns a NotifyingSource for the given path rooted at the "main" table as defined by the StateHistoryTable. The caller might
		// add further context (e.g. a recordSet index) to build the NotifyingSource.
		public delegate NotifyingSource PathToSourceMapperDelegate(DBI_Path path);
		public HistoryActionManager(XAFClient session, StateHistoryUITable historyTable, ICommonUI parentUI) {
			HistoryTable = historyTable;
			System.Diagnostics.Debug.Assert(HistoryTable != null, "HistoryActionManager: Unknown table");
			ParentUI = parentUI;
			DB = session;

			// Read the history records from the cached buffer manager
			Dictionary<Guid, State> statesById = new Dictionary<Guid, State>();
			using (DynamicBufferManager bm = new DynamicBufferManager(DB, HistoryTable.HistoryTable.StateTable.Database, false))
			{
				DynamicBufferManager.Query q = bm.Add(HistoryTable.HistoryTable.StateTable, false, null);
				q.KeepUpToDate = true;
				DataTable stateTable = q.DataTable;
				foreach (DataRow r in stateTable.Rows)
				{
					Guid stateID = (Guid)HistoryTable.HistoryTable.StateTable.InternalIdColumn[r];
					State state = new State(stateID, HistoryTable.HistoryTable, r, HistoryTable.CustomFlagField);
					statesById.Add(stateID, state);
				}
			}
			using (DynamicBufferManager bm = new DynamicBufferManager(DB, HistoryTable.HistoryTable.TransitionTable.Database, false))
			{
				DynamicBufferManager.Query q = bm.Add(HistoryTable.HistoryTable.TransitionTable, false, null);
				q.KeepUpToDate = true;
				using (DataView view = new DataView(q.DataTable, null, new SortExpression(new DBI_Path(HistoryTable.HistoryTable.TransitionRankColumn), SortExpression.SortOrder.Asc).ToDataExpressionString(), DataViewRowState.CurrentRows)) {
					var indicesByName = new Dictionary<Key, int>();
					for (int i = 0; i < view.Count; ++i) {
						Transition transition = new Transition(this, view[i].Row, statesById, true, HistoryTable.EditTblToUseByDefault, HistoryTable.EditTblToUseWhenCustomFlagGoesFalse);
						List<Transition> transitions;
						int transitionsIndex;
						if (!indicesByName.TryGetValue(transition.TransitionDefinition.Operation, out transitionsIndex)) {
							indicesByName.Add(transition.TransitionDefinition.Operation, Transitions.Count);
							transitions = new List<Transition>();
							Transitions.Add(transitions);
						}
						else
							transitions = Transitions[transitionsIndex];
						transitions.Add(transition);
					}
				}
			}
		}
		#endregion
		private readonly XAFClient DB;
	}
}
