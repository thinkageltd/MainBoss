using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// A 'wizard' to permit the quick creation of a work order given a WorkorderTemplateID. Currently kludged to accept the WorkOrderTemplateID
	/// as part of the List&lt;TblActionNode&gt; passed via the WorkorderTemplateBrowseControl.
	/// </summary>
	public class WorkOrderFromTemplateEditLogic : WithSequenceCounterEditLogic {
		#region Tbl Definition
		public static readonly object TemplateControlId = KB.I("TemplateControlId");
		public static DelayedCreateTbl WorkOrderFromTemplateTbl = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.WorkOrder, TId.WorkOrderFromTask,
				new Tbl.IAttr[] {
						TIGeneralMB3.SchedulingGroup,
						new ETbl(
							MB3ETbl.HasStateHistoryAndSequenceCounter(typeof(WorkOrderFromTemplateEditLogic), dsMB.Path.T.WorkOrder.F.Number, dsMB.Schema.T.WorkOrderSequenceCounter, dsMB.Schema.V.WOSequence, dsMB.Schema.V.WOSequenceFormat, TIWorkOrder.WorkOrderHistoryTable),
							ETbl.CreateCustomDataSet(true),
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.New))
				},
				new TblLayoutNodeArray(
					TblUnboundControlNode.New(KB.TOi(TId.Task), new LinkedTypeInfo(false, dsMB.Schema.T.WorkOrderTemplate.InternalIdColumn),
						new ECol(
							Fmt.SetId(TemplateControlId),
							Fmt.SetPickFrom(TIWorkOrder.TaskPickerTblCreator),
							Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord),
							Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.TemplateControl = valueCtrl; })
						)
					),
					TblSectionNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblGroupNode.New(KB.K("For Access Code"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblUnboundControlNode.New((Key)null, new IntegralTypeInfo(false, 0, 3),
								new ECol(
									Fmt.SetEnumText(TISchedule.TaskUnitPriorityProvider),
									Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord),
									Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.AccessCodePriorityControl = valueCtrl; }),
									Fmt.SetIsSetting((int)DatabaseEnums.TaskUnitPriority.PreferTaskValue)
								))
						)
					),
					TblSectionNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal, TIGeneralMB3.AccountingFeatureArg },
						TblGroupNode.New(KB.K("For Expense Model"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
							TblUnboundControlNode.New((Key)null, new IntegralTypeInfo(false, 0, 3),
								new ECol(
									Fmt.SetEnumText(TISchedule.TaskUnitPriorityProvider),
									Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord),
									Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.ExpenseModelPriorityControl = valueCtrl; }),
									Fmt.SetIsSetting((int)DatabaseEnums.TaskUnitPriority.PreferTaskValue)
								))
						)
					),
					TblUnboundControlNode.New(KB.K("Purchase Order initial State override"), new LinkedTypeInfo(true, dsMB.Schema.T.PurchaseOrderState.InternalIdColumn),
						new FeatureGroupArg(TIGeneralMB3.PurchasingGroup),
						new ECol(
							Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord),
							Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.POInitialStateOverrideControl = valueCtrl; }),
							Fmt.SetIsSetting(System.Guid.Empty)
						)
					),
					TblUnboundControlNode.New(KB.K("Work Order initial State override"), new LinkedTypeInfo(true, dsMB.Schema.T.WorkOrderState.InternalIdColumn),
						new ECol(
							Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord),
							Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.WOInitialStateOverrideControl = valueCtrl; }),
							Fmt.SetIsSetting(System.Guid.Empty)
						)
					),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Number, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.UnitLocationID,
						new ECol(Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord))),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.StartDateEstimate,
						new ECol(Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord))),
					TblUnboundControlNode.New(dsMB.Path.T.ScheduledWorkOrder.F.SlackDays.Key(), dsMB.Schema.T.ScheduledWorkOrder.F.SlackDays.EffectiveType,
						new ECol(
							Fmt.SetNotifyT<WorkOrderFromTemplateEditLogic>(editor => editor.FillInWorkOrderRecord),
							Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.SlackDaysControl = valueCtrl; }),
							Fmt.SetIsSetting(TimeSpan.Zero)
						)
					),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Subject,
						new ECol(
							ECol.AddChangeEnablerT<WorkOrderFromTemplateEditLogic>(editor => editor.SubjectChangeEnabler),
							Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.SubjectControl = valueCtrl; })
						)
					),
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.AccessCodeID, ECol.AllReadonly),
					// TODO: Should the following PermissionToView/PermissionToEdit be here? Two questions, really: should expense models be
					// under these permissions, and should it not be sufficient to place them on the referenced picker tbl?
					TblColumnNode.New(dsMB.Path.T.WorkOrder.F.WorkOrderExpenseModelID,
						new ECol(
							ECol.AddChangeEnablerT<WorkOrderFromTemplateEditLogic>(editor => editor.WorkOrderExpenseModelChangeEnabler),
							Fmt.SetCreatedT<WorkOrderFromTemplateEditLogic>((editor, valueCtrl) => { editor.WorkOrderExpenseModelControl = valueCtrl; })
						),
						CommonNodeAttrs.PermissionToViewAccounting,
						CommonNodeAttrs.PermissionToEditAccounting,
						TIGeneralMB3.AccountingFeatureArg)
				)
			);
		});
		#endregion
		#region Construction
		public WorkOrderFromTemplateEditLogic(IEditUI control, XAFClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		protected override void SetupDataset() {
			base.SetupDataset();
			WorkOrderRowAccessor = RecordManager.GetDirectRowAccessor(dsMB.Path.T.WorkOrder.F.Id, 0, true);
			woGenerator = new WorkOrderBuilder((dsMB)DataSet);
		}
		protected override void SetupStateHistoryTransitionCommands() {
			// Since we are always in New mode, the state transition commands can never become enabled so we avoid generating them with this do-nothing override.
		}
		public override void Teardown() {
			if (woGenerator != null) {
				woGenerator.Destroy();
				woGenerator = null;
			}
			base.Teardown();
		}
		#endregion
		#region Record-save override
		protected override object[] SaveRecord(Libraries.DBILibrary.Server.UpdateOptions updateOptions) {
			ICheckpointData woGeneratorData = woGenerator.Checkpoint();
			try {
				woGenerator.PopulateChildLookupTables();
				woGenerator.FillInWorkOrderChildRecordsFromTemplate(WOInitialStateValue, Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().UserRecordID);
				return base.SaveRecord(updateOptions);
			}
			catch {
				woGenerator.Rollback(woGeneratorData);
				throw;
			}
		}
		#endregion
		#region WO generation
		/// <summary>
		/// Fill in the work order record from the current template/unit/start date selections.
		/// </summary>
		private void FillInWorkOrderRecord() {
			// We defer this to Idle for two reasons: One is to avoid building the WO several times during record load.
			// The other is the undefined nature of which occurs first: the FillInWOrkOrderRecord notification or updating of bound fields
			// like the UnitLocationID that are used by WorkOrderBuilder.
			// TODO: For the PO initial state override, we don't actually have to redo the FillInWorkOrderFromTemplate as this only affects
			// the child records, but we still must call SpecifyRecordArguments.
			// TODO: Because of the delayed execution of the FillInWorkOrder it is impossible to Cancel the form, since cancelling
			// alters the Unit which causes a delayed modification of other bound controls like Subject.
			// TODO: It is possible that with enough WO defaults defined, it may be possible to Save the record even tough the
			// FillInWorkOrderFromTemplate call failed or ran with bogus values. This code should probably work from an initial WorkOrder
			// row that is all null rather than containing defaults. There is similar code in the "new subtask" handling.
			// TODO: we really should catch all exceptions during doFillInWorkOrder and use them to somehow disable the Save button. Perhaps
			// we could also use a TextDisplay to show the exception message.
			Thinkage.Libraries.Application.Instance.GetInterface<IIdleCallback>().ScheduleIdleCallback(this,
				delegate() {
					if (!IsDisposed)
						doFillInWorkOrderRecord();
				}
			);
		}
		private void doFillInWorkOrderRecord() {
			// Because this code alters controls and occurs at idle time, we must preserve and restore the editor state.
			EditLogic.EditorState stateSave = State;

			try {
				// Remember current Subject and WorkOrderExpenseModel control values if they are user-supplied so they can be restored
				// if the template doesn't provide values for these fields.
				if (SubjectChangeEnabler.Enabled)
					userSuppliedSubject = SubjectControl.ValueStatus == null ? SubjectControl.Value : null;
				if (WorkOrderExpenseModelChangeEnabler.Enabled)
					userSuppliedWorkOrderExpenseModel = WorkOrderExpenseModelControl != null && WorkOrderExpenseModelControl.ValueStatus == null ? WorkOrderExpenseModelControl.Value : null;
				if (WOInitialStateOverrideControl != null)
					WOInitialStateValue = (Guid?)WOInitialStateOverrideControl.Value;
				dsMB.WorkOrderRow workorder = (dsMB.WorkOrderRow)WorkOrderRowAccessor.Row;
				woGenerator.PopulateLookupTables();
				woGenerator.SpecifyRecordArguments(TemplateControl.ValueStatus == null ? TemplateControl.Value : null, workorder,
					(POInitialStateOverrideControl == null) ? null : (Guid?)POInitialStateOverrideControl.Value,
					// ExpenseModelPriorityControl will be missing if AccountingFeatureArg suppressed that section 
					ExpenseModelPriorityControl == null ? DatabaseEnums.TaskUnitPriority.PreferTaskValue : (DatabaseEnums.TaskUnitPriority)ExpenseModelPriorityControl.Value,
					(DatabaseEnums.TaskUnitPriority)AccessCodePriorityControl.Value);

				// Fill in the work order record using the new template/unit/start date selections.
				try {
					woGenerator.FillInWorkOrderFromTemplate(SlackDaysControl.ValueStatus == null ? (TimeSpan?)SlackDaysControl.Value : null);
				}
				catch (NonnullValueRequiredException) {
					// Any of these exceptions will be noticed when we try saving the record. As well, the bound control will flag it.
				}
				woGenerator.ReservePONumbersForWorkOrderTemplate();

				// Now set the read-only status of the Subject and WorkOrderExpenseModel controls according to whether
				// the template provided values for these fields.  If provided, then they are read-only.
				SubjectChangeEnabler.Enabled = dsMB.Schema.T.WorkOrder.F.Subject[workorder] == null;
				WorkOrderExpenseModelChangeEnabler.Enabled = dsMB.Schema.T.WorkOrder.F.WorkOrderExpenseModelID[workorder] == null;

				// Finally, if the template did not provide a value for the Subject or WorkOrderExpenseModel fields,
				// then copy the previous value back into the control so it appears unchanged.  In this case, the
				// control will be writable so the user can change it if desired.
				if (SubjectChangeEnabler.Enabled)
					SubjectControl.Value = userSuppliedSubject;
				if (WorkOrderExpenseModelChangeEnabler.Enabled && WorkOrderExpenseModelControl != null)
					WorkOrderExpenseModelControl.Value = userSuppliedWorkOrderExpenseModel;
			}
			finally {
				SetState(stateSave);
			}
		}
		#endregion
		#region Properties & Variables
		// ID of template we are creating the workorder from
		// The class that can produce the workorder contents from a template
		public WorkOrderBuilder woGenerator;

		private IBasicDataControl TemplateControl;
		private IBasicDataControl AccessCodePriorityControl;
		private IBasicDataControl ExpenseModelPriorityControl;
		private IBasicDataControl POInitialStateOverrideControl;
		private IBasicDataControl WOInitialStateOverrideControl;
		private IBasicDataControl SlackDaysControl;

		private IBasicDataControl SubjectControl;
		private IBasicDataControl WorkOrderExpenseModelControl;

		private SettableDisablerProperties SubjectChangeEnabler = new SettableDisablerProperties(null, KB.K("The template already provides a Subject"), true);
		private object userSuppliedSubject = null;
		private SettableDisablerProperties WorkOrderExpenseModelChangeEnabler = new SettableDisablerProperties(null, KB.K("The template already provides an Expense Model"), true);
		private object userSuppliedWorkOrderExpenseModel = null;

		private RecordManager.DirectRowAccessor WorkOrderRowAccessor;
		public Guid? WOInitialStateValue = null;
		#endregion
	}
}
