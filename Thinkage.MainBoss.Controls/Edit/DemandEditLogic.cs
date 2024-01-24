using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	public class DemandEditLogic : EditLogic {
		public DemandEditLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		public override object[] GetEditRowIDs() {
			// Override EditLogic's holdover code that eliminates all but the first ID
			return GetFullRowIDs();
		}
		public override object[] GetFullRowIDs() {
			object[] result = base.GetFullRowIDs();
			// Entry zero is the Id of the Demand record. We get the ID of entry 1 (the WOXME record) from the special DemandWorkOrderExpenseModelEntry view.
			// This provides DEBUG display of the WOXME information, and is also part of unfinished dead-end removal when the user goes to the Actuals browser
			// with intent to actualize the demand. For more notes on the latter, see the "Actuals browsers for the Demand tbls" region of TIWorkOrder.cs
			result[1] = DB.Session.ExecuteCommandReturningScalar(dsMB.Schema.T.DemandWorkOrderExpenseModelEntry.F.WorkOrderExpenseModelEntryID.EffectiveType.UnionCompatible(NullTypeInfo.Universe),
				new SelectSpecification(
					dsMB.Schema.T.DemandWorkOrderExpenseModelEntry,
					new SqlExpression[] { new SqlExpression(dsMB.Path.T.DemandWorkOrderExpenseModelEntry.F.WorkOrderExpenseModelEntryID) },
					new SqlExpression(dsMB.Path.T.DemandWorkOrderExpenseModelEntry.F.DerivedDemandID).Eq(SqlExpression.Constant(result[0])),
					null
				));
			return result;
		}
	}
}
