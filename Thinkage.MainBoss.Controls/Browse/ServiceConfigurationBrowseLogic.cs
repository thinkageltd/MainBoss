using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using System.Collections.Generic;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Controls {

	public class ServiceConfigurationBrowseLogic : BrowseLogic {
		#region OnlyOneRecordDisablerClass
		private class OnlyOneRecordDisablerClass : SettableDisablerProperties {
			public OnlyOneRecordDisablerClass(BrowseLogic browser)
				: base(null, KB.K("Only one record is allowed"), true) {
				browser.BrowserDataPositioner.Changed += delegate(DataChangedEvent whatHappened, Position affectedPosition) {
					switch (whatHappened) {
					case DataChangedEvent.Added:
					case DataChangedEvent.Changed:
					case DataChangedEvent.Moved:
					case DataChangedEvent.MovedAndChanged:
						// These all indicate the presence of at least one record, so we want to be disabled.
						Enabled = false;
						return;
					case DataChangedEvent.Reset:
					case DataChangedEvent.Deleted:
						// We need to count how many records there are (excluding the record being deleted)
						for (Position p = browser.BrowserDataPositioner.StartPosition.Next; !p.IsEnd; p = p.Next)
							if (p != affectedPosition) {
								// found at least one record that still remains
								Enabled = false;
								return;
							}
						Enabled = true;
						return;
					}
				};
			}
		}
		#endregion
		public ServiceConfigurationBrowseLogic(IBrowseUI control, XAFClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
		}
		public override void CreateLocalNewCommands(bool includeCommandsMarkedExportable, EditLogic.SavedEventHandler savedHandler, params IDisablerProperties[] extraDisablers) {
			List<IDisablerProperties> myDisablers = new List<IDisablerProperties>(extraDisablers);
			myDisablers.Add(new OnlyOneRecordDisablerClass(this));
			base.CreateLocalNewCommands(includeCommandsMarkedExportable, savedHandler, myDisablers.ToArray());
		}
	}
}