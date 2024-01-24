using System.Collections.Generic;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;

namespace Thinkage.MainBoss.Controls {
	public class WithHistoryColumnBrowseLogic : BrowseLogic {
		public WithHistoryColumnBrowseLogic(IBrowseUI control, XAFClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
		}
		ApplicationTblDefaults.AlreadyBeingEditedDisabler AlreadyBeingEditedDisabler;
		Source MainIdSource;
		protected override void CreateCustomBrowserCommands() {
			base.CreateCustomBrowserCommands();
			MainIdSource = base.GetTblPathDisplaySource(TblSchema.InternalId, -1);
			AlreadyBeingEditedDisabler = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().CreateAlreadyBeingEditedDisabler(TblSchema);
			AlreadyBeingEditedDisabler.Id = MainIdSource.GetValue();
			CommonLogic.CommandNode stateHistoryCommands = Commands.CreateNestedNode(KB.K("State History"), KB.K("State History"));
			MB3BTbl.WithHistoryLogicArg wha = (MB3BTbl.WithHistoryLogicArg)BTbl.BrowserLogicClassArg;
			var manager = new HistoryActionManager(DB, wha.HistoryTable, BrowseUI);
			foreach (List<HistoryActionManager.Transition> tlist in manager.Transitions) {
				var multiCommand = new HistoryActionManager.Transition.MultiCommandForBrowser(this);
				multiCommand.Notification += delegate () {
					AlreadyBeingEditedDisabler.Id = MainIdSource.GetValue();
				};
				Key caption = tlist[0].TransitionDefinition.Operation;
				MultiCommandIfAnyEnabled alternativeCommands = null;
				foreach (HistoryActionManager.Transition t in tlist) {
					ICommand command = t.GetCommand((p) => new BrowserNotifyingSource(ref multiCommand.Notification, GetTblPathDisplaySource(p, -1)));
					if (alternativeCommands == null)
						alternativeCommands = new MultiCommandIfAnyEnabled(command);
					else
						alternativeCommands.Add(command);
				}
				var disabledCommand = new MultiCommandIfAllEnabled(alternativeCommands);
				disabledCommand.Add(AlreadyBeingEditedDisabler);
				multiCommand.ContextualSingleRecordEditCommand = disabledCommand;
				stateHistoryCommands.AddCommand(caption, null, multiCommand, null);
			}
		}
	}
}
