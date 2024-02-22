using System;
using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.XAF.UI;

namespace Thinkage.Libraries.Presentation.ASPNet {
	// TODO: Add a debug provider which writes the output to a region on the web page that is styled DebugOutput, and make the style sheet mark this as invisible.
	// You can then see the debug output by disabling the style sheet on your browser.
	public abstract class TblApplication : Thinkage.Libraries.Application {
		#region Tbl identification routines
		// Many of the Tbl's are not directly reached from a root point, so we must create ID's for each new one as we see it.
		// For now we just use unsigned values as ID's, but this in the long run prevents a user from bookmarking anything
		// even as simple as their "My work orders" page. Ultimately, we should check for the special case where the given Tbl
		// is the one FindTbl can find and if so use the result of FindTbl instead, or perhaps there could be a RegisterTblId which
		// suggests a particular Id to use for a given Tbl.
		public string GetTblId(Tbl tbl) {
			uint index;
			if (!TblToIdMap.TryGetValue(tbl, out index)) {
				index = (uint)IdToTblMap.Count;
				IdToTblMap.Add(tbl);
				TblToIdMap.Add(tbl, index);
			}
			return index.ToString();
		}
		public Tbl GetTblFromTblId(string tblId) {
			uint index;
			if (!UInt32.TryParse(tblId, out index) || index >= IdToTblMap.Count)
				throw new GeneralException(KB.K("Cannot find form layout information for Tbl Id '{0}'"), tblId);
			return IdToTblMap[(int)index];
		}
		private Dictionary<Tbl, uint> TblToIdMap = new Dictionary<Tbl, uint>(Thinkage.Libraries.ObjectByReferenceEqualityComparer<Tbl>.Instance);
		private List<Tbl> IdToTblMap = new List<Tbl>();
		#endregion
		public static new TblApplication Instance { get { return (TblApplication)Thinkage.Libraries.Application.Instance; } }
#if WHEREDOESTHISBELONGINIUserInterface
		public override Ask.Result AskQuestion(string m, string heading, Ask.Questions options) {
			switch (options) {
			case Ask.Questions.AbortRetryIgnore:
				return Ask.Result.Abort;
			case Ask.Questions.YesNo:
				return Ask.Result.No;
			default:
				return Ask.Result.Cancel;
			}
		}
#endif
	}
	public class ApplicationTblDefaultsUsingWeb : ApplicationTblDefaults {
		public ApplicationTblDefaultsUsingWeb(GroupedInterface<IApplicationInterfaceGroup> attachTo, ETbl defaultETbl, IRightGrantor permissionsManager, TableRightsGroup rights, Right changeGlobalSettingsRight)
			: base(attachTo, defaultETbl, permissionsManager, rights, changeGlobalSettingsRight) {
		}
		private void PerformEdit(XAFClient db, Tbl tbl, EdtMode mode, object[] ID, bool[] subsequentModeRestrictions, List<TblActionNode> initList,
			UIForm owner, bool callModally, EditLogic.SavedEventHandler savedHandler) {
			string href;
			switch (mode) {
			case EdtMode.New:
			case EdtMode.EditDefault:
			case EdtMode.ViewDefault:
				href = EditPage.BuildEditHRef(tbl, mode, initList);
				break;
			default:
				href = EditPage.BuildEditHRef(tbl, mode, ID, initList);
				break;
			}

			((System.Web.UI.Page)owner).Response.Redirect(href);
		}
		public override void PerformMultiEdit(XAFClient db, Tbl tbl, EdtMode mode, object[][] IDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists,
			UIForm owner, bool callModally, EditLogic.SavedEventHandler savedHandler) {
			// TODO: Tell the EditLogic and BrowseLogic never to ask for multiple-record edits
			if (IDs.Length != 1)
				throw new GeneralException(KB.T("TODO: Call editor with {0} records"), IDs.Length);
			PerformEdit(db, tbl, mode, IDs[0], subsequentModeRestrictions, initLists == null ? null : initLists[0], owner, callModally, savedHandler);
		}
		public override IUIModifyingRecord FindRecordModifier(object[] IDs, DBI_Table[] inTables, EditLogic exceptionEditor = null) {
			return null;
		}
		public override ApplicationTblDefaults.AlreadyBeingEditedDisabler CreateAlreadyBeingEditedDisabler(DBI_Table schema, EditLogic exceptionEditor = null) {
			return null;
		}
		public override void ReevaluateModifyingDisablers(DBI_Table schema) {
		}
	}
}
