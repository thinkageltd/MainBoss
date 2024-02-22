using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.MVC.Models;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebAccess.Models {
	[Serializable]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "No need for other constructors")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "No need for other constructors")]

	public class ActionNotPermittedForCurrentStateException : System.Exception {
	}
	[Serializable]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "No need for other constructors")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "No need for other constructors")]

	public class StateHistoryChangedUnderneathUsException : System.Exception {
		[NonSerialized]
		public readonly Guid NewStateHistoryID;
		[NonSerialized]
		public readonly Guid ParentID;
		public StateHistoryChangedUnderneathUsException(Guid parentID, Guid currentStateHistoryID)
			: base() {
			ParentID = parentID;
			NewStateHistoryID = currentStateHistoryID;
		}
	}
	/// <summary>
	/// Special Respository support for Close operations on xxxStateHistoryTables
	/// </summary>
	public abstract class StateHistoryRepository : BaseRepository, IStateHistoryRepository {
		public enum CustomInstructions {
			DefaultToExisting,
			CheckRequestorID,
			CheckUserIsAssignee,
			FullUpdate,
			SelfAssign,
			CloseWorkOrder
		}
		protected delegate void CustomProcessing(dsMB ds);
		/// <summary>
		/// Return an error string if the currentState is not acceptable for the action. Otherwise return null.
		/// </summary>
		/// <param name="currentState"></param>
		/// <returns></returns>
		protected delegate string AllowedState(Guid currentState);

		protected StateHistoryRepository([Invariant] string governingTableRight, FormMap formDefinition)
			: base(governingTableRight, formDefinition) {
		}
		#region State Transition Map
		public Transition FindTransitionByName(string name) {
			return Transitions.Find(t => t.Name.IdentifyingName.Equals(name));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "<Pending>")]
		public class Transition {
			public readonly MB3Client.StateHistoryTransition TransitionDefinition;
			public readonly State OldState;
			public readonly State NewState;
			public readonly List<MB3Client.StateFlagRestriction> Restrictions = new List<MB3Client.StateFlagRestriction>();
			public readonly Thinkage.Libraries.Permissions.Right ControllingActionRight;
			public Key Name { get { return TransitionDefinition.Operation; } }

			public Transition(MB3Client.StateHistoryTable stateHistory, DBIDataRow row, Dictionary<Guid, State> stateMap) {
				TransitionDefinition = stateHistory.StateHistoryTransitionFromRow(row);
				OldState = stateMap[TransitionDefinition.FromStateID];
				NewState = stateMap[TransitionDefinition.ToStateID];
				ControllingActionRight = Root.Rights.FindRightByName(TransitionDefinition.RightName);
				System.Diagnostics.Debug.Assert(ControllingActionRight != null, "Missing state transition right");

				foreach (MB3Client.StateFlagRestriction r in NewState.Restrictions)
					if (!OldState.Restrictions.Contains(r))
						Restrictions.Add(r);
			}
		}


		// The State class defines each State, and provides the list of Transitions out of that state. It also acts as a Disabler that is only enabled
		// when it is the CurrentState.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "<Pending>")]
		public class State {
			public readonly Guid ID;
			public readonly List<Transition> Transitions = new List<Transition>();
			public readonly List<MB3Client.StateFlagRestriction> Restrictions = new List<MB3Client.StateFlagRestriction>();

			public State(Guid id, MB3Client.StateFlagRestriction[] restrictions, DBIDataRow row) {
				ID = id;
				foreach (MB3Client.StateFlagRestriction r in restrictions)
					if ((bool)row[r.StateFlagColumn] == r.ConditionAppliesWhenFlagIs)
						Restrictions.Add(r);
			}
		}
		protected static void InitStateInformation(MB3Client DB, MB3Client.StateHistoryTable HistoryTable, List<Transition> transitions) {
			Dictionary<Guid, State> stateMap = new Dictionary<Guid, State>();
			// Read the history records from the cached buffer manager
			using (DynamicBufferManager bm = new DynamicBufferManager(DB, HistoryTable.StateTable.Database, false)) {
				DynamicBufferManager.Query q = bm.Add(HistoryTable.StateTable, false, null);
				q.KeepUpToDate = true;
				DBIDataTable stateTable = q.DataTable;
				foreach (DBIDataRow r in stateTable.Rows) {
					Guid stateID = (Guid)r[HistoryTable.StateTable.InternalIdColumn];
					State state = new State(stateID, HistoryTable.StateRestrictions, r);
					stateMap.Add(stateID, state);
				}
			}
			using (DynamicBufferManager bm = new DynamicBufferManager(DB, HistoryTable.TransitionTable.Database, false)) {
				DynamicBufferManager.Query q = bm.Add(HistoryTable.TransitionTable, false, null);
				q.KeepUpToDate = true;
				DBIDataRow[] view = q.DataTable.Rows.Select(null, new SortExpression(new DBI_Path(HistoryTable.TransitionRankColumn), SortExpression.SortOrder.Asc));
				for (int i = 0; i < view.Length; ++i) {
					Transition transition = new Transition(HistoryTable, view[i], stateMap);
					transition.OldState.Transitions.Add(transition);
					transitions.Add(transition);
				}
			}
		}
		#endregion

		#region IStateHistoryRepository Members

		public abstract List<StateHistoryRepository.Transition> Transitions {
			get;
		}

		#endregion
	}
}