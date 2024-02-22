using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;
using System.Text;
using System.Linq;

namespace Thinkage.MainBoss.Controls {
	public class ValidateMaintenencePlanEditLogic : EditLogic {
		#region Tbl Definition
		public static readonly object TemplateControlId = KB.I("TemplateControlId");
		public static DelayedCreateTbl ScheduledWorkOrderValidationTbl = new DelayedCreateTbl(() => {
			return new Tbl(dsMB.Schema.T.ScheduledWorkOrder, TId.ValidateUnitMaintenencePlan,
				new Tbl.IAttr[] {
						new ETbl(
							ETbl.LogicClass(typeof(ValidateMaintenencePlanEditLogic)),
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.View, EdtMode.ViewDeleted),
							ETbl.CreateCustomDataSet(true)
						)
				},
				new TblLayoutNodeArray(
					TblTabNode.New(KB.K("Scheduling"), KB.K("Validate the scheduling information for this Unit Maintenance Plan"), new[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.ScheduleID.F.Code, ECol.AllReadonly),
						TblUnboundControlNode.New(KB.K("Unit Maintenance Plan Status"), new StringTypeInfo(1, null, uint.MaxValue, allow_null: true, trim_leading: true, trim_trailing: true),
							new ECol(
								ECol.AllReadonlyAccess,
								// TODO: Show things like Inhibited plan, deleted unit, etc that cause quick exit from PM Generation
								Fmt.SetCreatedT<ValidateMaintenencePlanEditLogic>((editor, valueCtrl) => { editor.PlanStatusControl = valueCtrl; })
							)
						)
#if LATER
						,
						TblUnboundControlNode.New(KB.K("Scheduling Status"), new StringTypeInfo(1, null, uint.MaxValue, allow_null: true, trim_leading: true, trim_trailing: true),
							new ECol(
								ECol.AllReadonlyAccess,
								// Explain the scheduling basis and the estimated next schedule point including meter prediction stuff and anything causing "indefinite" schedule point.
								Fmt.SetCreatedT<ValidateMaintenencePlanEditLogic>((editor, valueCtrl) => { editor.SchedulingStatusControl = valueCtrl; })
							)
						)
#endif
					),
					TblTabNode.New(KB.K("Task"), KB.K("Validate the Task information for this Unit Maintenance Plan"), new[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.WorkOrderTemplateID.F.Code, ECol.AllReadonly),
						TblGroupNode.New(KB.K("For Expense Model"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblUnboundControlNode.New((Key)null, new IntegralTypeInfo(false, 0, 3),
								new ECol(
									ECol.AllWriteableAccess,
									Fmt.SetEnumText(TISchedule.TaskUnitPriorityProvider),
									Fmt.SetNotifyT<ValidateMaintenencePlanEditLogic>(editor => editor.FillInWorkOrderRecord),
									Fmt.SetCreatedT<ValidateMaintenencePlanEditLogic>((editor, valueCtrl) => { editor.ExpenseModelPriorityControl = valueCtrl; }),
									Fmt.SetIsSetting((int)DatabaseEnums.TaskUnitPriority.PreferTaskValue)
								)
							)
						),
						TblUnboundControlNode.New(KB.K("Work Order Creation Status"), new StringTypeInfo(1, null, uint.MaxValue, allow_null: true, trim_leading: true, trim_trailing: true),
							new ECol(
								ECol.AllReadonlyAccess,
								// Show the errors from generating the WO
								Fmt.SetCreatedT<ValidateMaintenencePlanEditLogic>((editor, valueCtrl) => { editor.WorkOrderCreationStatusControl = valueCtrl; })
							)
						)
					)
				)
			);
		});
#endregion
#region Construction
		public ValidateMaintenencePlanEditLogic(IEditUI control, DBClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		protected override void SetupDataset() {
			base.SetupDataset();
			ScheduledWorkOrderRowAccessor = RecordManager.GetDirectRowAccessor(dsMB.Path.T.ScheduledWorkOrder.F.Id, 0, false);
			RecordManager.GetPathNotifyingSource(dsMB.Path.T.ScheduledWorkOrder.F.Id, 0).Notify += delegate () {
				AnalyzePlan();
				FillInWorkOrderRecord();
			};
			// The following ensure the linked records are in the edit buffer as lookup records.
			RecordManager.GetPathSource(dsMB.Path.T.ScheduledWorkOrder.F.UnitLocationID.F.RelativeLocationID.F.LocationID.F.Code, 0);
			RecordManager.GetPathSource(dsMB.Path.T.ScheduledWorkOrder.F.ScheduleID.F.Code, 0);
			RecordManager.GetPathSource(dsMB.Path.T.ScheduledWorkOrder.F.CurrentPMGenerationDetailID.F.DetailType, 0);
			woGenerator = new WorkOrderBuilder((dsMB)DataSet);
			ObjectsToDispose.Add(woGenerator);
			pmGenerator = new PMGeneration((dsMB)DataSet);
			ObjectsToDispose.Add(pmGenerator);
		}
		public override void Teardown() {
			woGenerator?.Destroy();
			woGenerator = null;
			pmGenerator?.Destroy();
			pmGenerator = null;
			base.Teardown();
		}
#endregion
#region Maintenance Plan Analysis
		private void AnalyzePlan() {
			Thinkage.Libraries.Application.Instance.GetInterface<IIdleCallback>().ScheduleIdleCallback(RecordManager,
				delegate () {
					if (!IsDisposed)
						DoAnalyzePlan();
				}
			);
		}
		private void DoAnalyzePlan() {
			var planRow = (dsMB.ScheduledWorkOrderRow)ScheduledWorkOrderRowAccessor.Row;
			// Fetch all the Periodicity records referenced by this SWO and also all the meter classes (if any)
			DB.ViewAdditionalRows(DataSet, dsMB.Schema.T.Periodicity, new SqlExpression(dsMB.Path.T.Periodicity.F.ScheduleID).Eq(SqlExpression.Constant(planRow.F.ScheduleID)), null,
				new[] {
					dsMB.Path.T.Periodicity.F.MeterClassID.PathToReferencedRow
				});
			// Fetch all the meters associated with the Unit.
			DB.ViewAdditionalRows(DataSet, dsMB.Schema.T.Meter, new SqlExpression(dsMB.Path.T.Meter.F.UnitLocationID).Eq(SqlExpression.Constant(planRow.F.UnitLocationID)), null,
				new[] {
					dsMB.Path.T.Periodicity.F.MeterClassID.PathToReferencedRow
				});

			var analysisMessages = pmGenerator.AnalyzePlan(planRow, (dsMB)DataSet, quickExitIfInhibited: false);

			var text = new StringBuilder();
			foreach (System.Exception ex in analysisMessages)
				text.AppendLine(Thinkage.Libraries.Exception.FullMessage(ex));

			PlanStatusControl.Value = text.Length == 0 ? null : text.ToString();

			var schedulingMessages = pmGenerator.AnalyzeSchedule(planRow);

			text.Clear();
			foreach (Key message in schedulingMessages)
				text.AppendLine(message.Translate());

			//SchedulingStatusControl.Value = text.Length == 0 ? null : text.ToString();
		}
#endregion
#region WO generation
		private void FillInWorkOrderRecord() {
			Thinkage.Libraries.Application.Instance.GetInterface<IIdleCallback>().ScheduleIdleCallback(this,
				delegate () {
					if (!IsDisposed)
						DoFillInWorkOrderRecord();
				}
			);
		}
		private void DoFillInWorkOrderRecord() {
			ICheckpointData woGeneratorData = woGenerator.Checkpoint();
			var planRow = (dsMB.ScheduledWorkOrderRow)ScheduledWorkOrderRowAccessor.Row;
			dsMB.WorkOrderRow workorder = ((dsMB)DataSet).T.WorkOrder.AddNewRow();
			bool setAMessage = false;

			try {
				var collectedExceptions = new List<System.Exception>();
				// Remember current Subject and WorkOrderExpenseModel control values if they are user-supplied so they can be restored
				// if the template doesn't provide values for these fields.
				workorder.F.Number = KB.I("Non-null");  // We just want any non-null value.
				workorder.F.UnitLocationID = planRow.F.UnitLocationID;
				woGenerator.PopulateLookupTables();
				woGenerator.SpecifyRecordArguments(planRow.F.WorkOrderTemplateID, workorder,
					null,
					// ExpenseModelPriorityControl will be missing if AccountingFeatureArg suppressed that section 
					ExpenseModelPriorityControl == null ? DatabaseEnums.TaskUnitPriority.PreferTaskValue : (DatabaseEnums.TaskUnitPriority)ExpenseModelPriorityControl.Value,
					DatabaseEnums.TaskUnitPriority.PreferTaskValue);

				// Fill in the work order record using the new template/unit/start date selections.
				try {
					woGenerator.FillInWorkOrderFromTemplate(null);  // TODO: Use Slack Days from the Plan though it will not affect any generation errors.
				}
				catch (System.Exception woEx) {
					collectedExceptions.Add(woEx);
				}
				woGenerator.ReservePONumbersForWorkOrderTemplate();

				woGenerator.PopulateChildLookupTables();
				try {
					woGenerator.FillInWorkOrderChildRecordsFromTemplate(null, null, null, Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().UserRecordID);
				}
				catch (System.Exception childEx) {
					collectedExceptions.Add(childEx);
				}

				Thinkage.Libraries.Exception.ThrowIfAggregateExceptions(collectedExceptions);
			}
			catch (System.Exception ex) {
				WorkOrderCreationStatusControl.Value = GetExceptionMessages(ex);
				setAMessage = true;
			}
			finally {
				woGenerator.Rollback(woGeneratorData);
				workorder.Delete();
			}
			if (!setAMessage)
				WorkOrderCreationStatusControl.Value = null;
		}
#endregion
#region Properties & Variables
		// ID of template we are creating the workorder from
		// The class that can produce the workorder contents from a template
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Part of ObjectsToDispose")]
		private WorkOrderBuilder woGenerator;
		private PMGeneration pmGenerator;

		private IBasicDataControl ExpenseModelPriorityControl;
		private IBasicDataControl PlanStatusControl;
		//private IBasicDataControl SchedulingStatusControl;
		private IBasicDataControl WorkOrderCreationStatusControl;

		private RecordManager.DirectRowAccessor ScheduledWorkOrderRowAccessor;
#endregion
		private string GetExceptionMessages(System.Exception ex) {
			return Thinkage.Libraries.Exception.FullMessage(ex);
		}
	}
}
