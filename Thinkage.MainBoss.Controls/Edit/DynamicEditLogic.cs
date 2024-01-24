using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;

namespace Thinkage.MainBoss.Controls {
	public interface IDynamicCustomTbl {
		Tbl CustomTbl(XAFClient db);
		List<TblActionNode>[] InitLists { get; }
	}
	public class DynamicEditLogic<T> : EditLogic where T : IDynamicCustomTbl, new() {
		public DynamicEditLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: this(control, db, initialEditMode, initRowIDs, subsequentModeRestrictions, new T(), settingsContainer) {
		}
		private DynamicEditLogic(IEditUI control, XAFClient db, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, IDynamicCustomTbl customBuilder, Settings.Container settingsContainer)
			: base(control, db, customBuilder.CustomTbl(db), settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, customBuilder.InitLists) {
		}
	}
}