using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Edit Logic that supports history table information.
	/// This is used on tables that refer to a history table where the editor should be able to add new History entries.
	/// Also implied is handling for the edit record having state history.
	/// </summary>
	public class WithHistoryColumnEditLogic : EditLogic {
		#region Constructors
		public WithHistoryColumnEditLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		#endregion
		#region Setup
		protected override void SetupCommands() {
			base.SetupCommands();
			if (!TInfo.Schema.IsDefaultTable)
				SetupStateHistoryTransitionCommands();
		}
		protected virtual void SetupStateHistoryTransitionCommands() {
			MB3ETbl.WithStateHistoryAndSequenceCounterLogicArg sh = (MB3ETbl.WithStateHistoryAndSequenceCounterLogicArg)ETbl.EditorLogicClassArg;
			MultiplyValidCommandSetDeclaration cld = new MultiplyValidCommandSetDeclaration();
			CommandGroupDeclarationsInOrder.Insert(CommandGroupDeclarationsInOrder.Count - 1, cld);
			List<EditorState> allowedStates = new List<EditorState>();
			foreach (EditorState s in AllStates)
				if (s.EditRecordState == EditRecordStates.Existing && !s.CanSave)
					allowedStates.Add(s);
			var manager = new HistoryActionManager(DB, sh.StateHistory, EditUI);
			foreach (List<HistoryActionManager.Transition> tlist in manager.Transitions) {
				Key caption = tlist[0].TransitionDefinition.Operation;
				MultiCommandIfAnyEnabled alternativeCommands = null;
				foreach (HistoryActionManager.Transition t in tlist) {
					ICommand command = t.GetCommand((p) => RecordManager.GetPathNotifyingSource(p, 0));
					if (alternativeCommands == null)
						alternativeCommands = new MultiCommandIfAnyEnabled(command);
					else
						alternativeCommands.Add(command);
				}
				var disabledCmd = new MultiCommandIfAllEnabled(alternativeCommands);
				disabledCmd.Add(BrowsetteDisabler);
				disabledCmd.Add(NewUnsavedRecordDisabler);
				disabledCmd.Add(AnyAlreadyBeingEditedDisabler);
				cld.Add(new CommandDeclaration(caption, disabledCmd));
			}
		}
		#endregion
	}
}
