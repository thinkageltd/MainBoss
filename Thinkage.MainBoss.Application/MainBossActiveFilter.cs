using System;
using System.Collections.Generic;

using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Application
{
	public class MainBossActiveFilter : ApplicationWithLastUpdateFilter
	{
		public DateTime? ActiveFilterSinceDate;
		public TimeSpan? ActiveFilterInterval;
		#region ActiveFilterEditLogic
		public class ActiveFilterEditLogic : EditLogic {
			public IBasicDataControl ActiveFilterSinceDateControl;
			public IBasicDataControl ActiveFilterIntervalControl;

			public ActiveFilterEditLogic(IEditUI editUI, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
				: base(editUI, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
			}
			protected override object[] SaveRecord(Libraries.DBILibrary.Server.UpdateOptions updateOptions) {
				// Do not call base.SaveRecord which assumes there is a DB client
				// base.SaveRecord();
				MainBossActiveFilter filter = (MainBossActiveFilter)Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithLastUpdateFilter>();
				filter.ActiveFilterSinceDate = (DateTime?)ActiveFilterSinceDateControl.Value;
				filter.ActiveFilterInterval = (TimeSpan?)ActiveFilterIntervalControl.Value;
				filter.LastUpdateFilterChanged();
				return new object[0];
			}
			public static Tbl EditTbl;
			static ActiveFilterEditLogic() {
				EditTbl = new Tbl(null, TId.ActiveFilter,
					new Tbl.IAttr[] {
							xyzzy.LocationGroup,
							new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.Edit, EdtMode.View), ETbl.LogicClass(typeof(ActiveFilterEditLogic)))
						},
					new TblLayoutNodeArray(
						// We use the delegate form of SetInitialValue here so we get the current filter settings as of when the form is shown.
						// Note though that the user can have multiple main forms, call up this form from both, change the values in one and save it, and
						// the other form will still show the original values.
						TblUnboundControlNode.New(KB.K("Show only records less than this many days old"), dsMB.Schema.V.ActiveFilterInterval.EffectiveType,
							new ECol(
								Fmt.SetInitialValue(() => ((MainBossActiveFilter)Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithLastUpdateFilter>()).ActiveFilterInterval),
								ECol.SetUserChangeNotify(),
								Fmt.SetCreatedT<ActiveFilterEditLogic>(
									delegate(ActiveFilterEditLogic editor, IBasicDataControl valueCtrl) {
										editor.ActiveFilterIntervalControl = valueCtrl;
									}
								)
							)
						),
						TblUnboundControlNode.New(KB.K("Show only records since"), dsMB.Schema.V.ActiveFilterSinceDate.EffectiveType,
							new ECol(
								Fmt.SetInitialValue(() => ((MainBossActiveFilter)Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithLastUpdateFilter>()).ActiveFilterSinceDate),
								ECol.SetUserChangeNotify(),
								Fmt.SetCreatedT<ActiveFilterEditLogic>(
									delegate(ActiveFilterEditLogic editor, IBasicDataControl valueCtrl) {
										editor.ActiveFilterSinceDateControl = valueCtrl;
									}
								)
							)
						)
					)
				);
			}
		}
		#endregion

		public MainBossActiveFilter(GroupedInterface<IApplicationInterfaceGroup> attachTo, DateTime? initialSinceDate, TimeSpan? initialInterval)
			: base(attachTo)
		{
			ActiveFilterSinceDate = initialSinceDate;
			ActiveFilterInterval = initialInterval;
		}

		public override SqlExpression GetLastUpdateFilter(SqlExpression lastUpdatePathExpression)
		{
			SqlExpression retExpr = SqlExpression.Constant(true);
			if (ActiveFilterSinceDate.HasValue || ActiveFilterInterval.HasValue) {
				if (ActiveFilterSinceDate.HasValue)
					retExpr = SqlExpression.And(retExpr, lastUpdatePathExpression.Gt(SqlExpression.Constant(ActiveFilterSinceDate.Value)));
				if (ActiveFilterInterval.HasValue)
					retExpr = SqlExpression.And(retExpr, lastUpdatePathExpression.Gt(SqlExpression.Constant(DateTime.Now - (TimeSpan)ActiveFilterInterval.Value)));
				// Now add the exceptions for 'Open' requests/workorders/purchase orders
				// we do this by interpreting the lastUpdatePath to see if it ends in the RequestStateHistory/WorkOrderStateHistory/PurchaseOrderStateHistory table
				// and add the appropriate additional condition based on the FilterOnxxxx flag for the current state
				DBI_Path lastUpdatePath = lastUpdatePathExpression.Path;
				if (lastUpdatePath.ReferencedColumn == dsMB.Schema.T.RequestStateHistory.F.EffectiveDate) {
					var statePath = new DBI_PathToRow(lastUpdatePath.PathToContainingRow, dsMB.Path.T.RequestStateHistory.F.RequestStateID.PathToReferencedRow);
					retExpr = SqlExpression.Or(retExpr, new SqlExpression(new DBI_Path(statePath, dsMB.Path.T.RequestState.F.FilterAsClosed)).IsFalse());
				}
				if (lastUpdatePath.ReferencedColumn == dsMB.Schema.T.WorkOrderStateHistory.F.EffectiveDate) {
					var statePath = new DBI_PathToRow(lastUpdatePath.PathToContainingRow, dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.PathToReferencedRow);
					retExpr = SqlExpression.Or(retExpr, new SqlExpression(new DBI_Path(statePath, dsMB.Path.T.WorkOrderState.F.FilterAsDraft)).IsTrue());
					retExpr = SqlExpression.Or(retExpr, new SqlExpression(new DBI_Path(statePath, dsMB.Path.T.WorkOrderState.F.FilterAsOpen)).IsTrue());
				}
				if (lastUpdatePath.ReferencedColumn == dsMB.Schema.T.PurchaseOrderStateHistory.F.EffectiveDate) {
					var statePath = new DBI_PathToRow(lastUpdatePath.PathToContainingRow, dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.PathToReferencedRow);
					retExpr = SqlExpression.Or(retExpr, new SqlExpression(new DBI_Path(statePath, dsMB.Path.T.PurchaseOrderState.F.FilterAsDraft)).IsTrue());
					retExpr = SqlExpression.Or(retExpr, new SqlExpression(new DBI_Path(statePath, dsMB.Path.T.PurchaseOrderState.F.FilterAsIssued)).IsTrue());
				}
				if (lastUpdatePath.ReferencedColumn == dsMB.Schema.T.PMGenerationBatch.F.EntryDate
					&& lastUpdatePath.IsCompound
					&& lastUpdatePath.AllButLastLink.ReferencedColumn == dsMB.Schema.T.PMGenerationDetail.F.PMGenerationBatchID) {
					retExpr = SqlExpression.Or(retExpr,
						new SqlExpression(new DBI_Path(lastUpdatePath.PathToContainingRow, dsMB.Path.T.PMGenerationBatch.F.EntryDate))
							.GEq(new SqlExpression(new DBI_Path(lastUpdatePath.AllButLastLink.PathToContainingRow, dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.CurrentPMGenerationDetailID.F.PMGenerationBatchID.F.EntryDate))));
				}
			}
			return retExpr;
		}
		public void CreateEditor(UIFactory uiFactory, UIForm parentForm, XAFClient session)
		{
			// TODO: EditLogic should now behave with no Client object, so get rid of all this junk passing it around.
			ITblDrivenApplication appInstance = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>();
			appInstance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(uiFactory, session, ActiveFilterEditLogic.EditTbl, EdtMode.Edit, new object[][] { new object[] { } }, ApplicationTblDefaults.NoModeRestrictions, null, parentForm, true, null);
		}
	}
}
