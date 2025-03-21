using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	// TODO: Need a way of putting the parameter definition into the CompositeView in which case this custom Logic class would not be needed and we would just use
	// the standard New verb.
	public class AttachmentSpecificationBrowseLogic : BrowseLogic {
		internal static object SpecificationNodeId = KB.I("Specification");
		public AttachmentSpecificationBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure) {
		}

		public override void CreateLocalNewCommands(bool includeCommandsMarkedExportable, EditLogic.SavedEventHandler savedHandler, params IDisablerProperties[] extraDisablers) {
			base.CreateLocalNewCommands(includeCommandsMarkedExportable, savedHandler, extraDisablers);
			// Since we are editing with the same Tbl we are browsing with we don't need to check for hiding attributes.
			// Note that if the browser UI is incapable of creating a parameter control it should also omit the New command (e.g. in menu forms)
			// For now we skip the obvious case of a picker control, which has no place for parameter controls.
			// Make this a multi-command? No, it is context-free so you would only ever get one anyway. (TODO: so why is it NeedsUIContext? Perhaps so it does not show up in the browser's context menu)
			switch (BrowseOption & BrowseOptions.BrowserTypeMask) {
			case BrowseOptions.Browser:
			case BrowseOptions.Browsette:
				BrowseLogic.CallEditorCommand basicCommand = new CallNewModeSpecificationEditorCommand(this, savedHandler);
				Commands.AddCommand(
					KB.K("New xSpecification"),
					null,
					CreateBasicBrowserCommand(basicCommand,
						base.TInfo.GetPermissionBasedDisabler(DB.Session, TableOperationRightsGroup.TableOperation.Create)
					),
					null,
					null,
					TblUnboundControlNode.New(dsMB.Schema.T.Specification.F.SpecificationFormID.EffectiveType, new DCol(Fmt.SetId(SpecificationNodeId))));
				break;
			}
		}

		private class CallNewModeSpecificationEditorCommand : BrowseLogic.CallEditorCommand {
			// This class supplies a special tip.
			// It also creates a special Init based on the value of the FormPicker once the command Execute occurs.
			public CallNewModeSpecificationEditorCommand(AttachmentSpecificationBrowseLogic browser, EditLogic.SavedEventHandler handler)
				: base(browser, EdtMode.New, 0, KB.K("Create a new Specification based on the selected form"), handler) {
				AddInitDirective(new ControlValue(SpecificationNodeId), new PathTarget(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.SpecificationID.F.SpecificationFormID), browser);
			}
		}
	}
}