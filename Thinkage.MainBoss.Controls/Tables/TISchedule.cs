using System;
using System.Linq;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;
using static Thinkage.MainBoss.Database.DatabaseEnums;
using System.CodeDom;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Scheduling.
	/// </summary>
	public class TISchedule : TIGeneralMB3 {
		public static EnumValueTextRepresentations UnitMaintenancePlanInhibitEnumText = EnumValueTextRepresentations.NewForBool(KB.K("Unit Maintenance Plan Enabled"), null, KB.K("Unit Maintenance Plan Inhibited"), null);
		public static EnumValueTextRepresentations DeferInhibitEnumText = EnumValueTextRepresentations.NewForBool(KB.K("Defer"), null, KB.K("Inhibit"), null);
		#region View Record Types
		#region - PMGenerationDetailAndScheduledWorkOrderAndLocation types
		public enum PMGenerationDetailAndScheduledWorkOrderAndLocation {
			PostalAddress,
			Unit,
			PermanentStorage,
			PlainRelativeLocation,
			ScheduledWorkOrder
			// There are other views but no references to them.
		}
		#endregion
		#region - CommittedPMGenerationDetailAndPMGenerationBatch types
		public enum CommittedPMGenerationDetailAndPMGenerationBatch {
			PMGenerationBatch,
			DispositionAfterSchedulingPeriod,
			Deferred,
			PredictedWorkOrder,
			Error,
			ErrorMakingWorkOrder,
			MakeWorkOrder,
			MakeUnscheduledWorkOrder,
			Inhibited,
			ManualReschedule,
			MakeSharedWorkOrder
		}
		#endregion
		#endregion
		#region NodeIds
		private static readonly object SeasonStartId = KB.I("SeasonStartId");
		private static readonly object SeasonEndId = KB.I("SeasonEndId");
		private static readonly object MonId = KB.I("MonId");
		private static readonly object TueId = KB.I("TueId");
		private static readonly object WedId = KB.I("WedId");
		private static readonly object ThuId = KB.I("ThuId");
		private static readonly object FriId = KB.I("FriId");
		private static readonly object SatId = KB.I("SatId");
		private static readonly object SunId = KB.I("SunId");
		private static readonly object MaintenancePlansId = KB.I("MaintenancePlansId");
		#endregion
		private static readonly Key SetScheduleBasisCommandText = KB.K("Change Schedule Basis");
		private static readonly Key ValidateScheduledWorkOrderCommandText = KB.K("Validate");
		#region SeasonStartEndCheck
		private static Check SeasonStartEndCheck(object startNodeId, object endNodeId) {
			return new Check2<TimeSpan?, TimeSpan?>(delegate (TimeSpan? start, TimeSpan? end) {
				if (start.HasValue && !end.HasValue)
					return new EditLogic.ValidatorAndCorrector.ValidatorStatus(1, new GeneralException(KB.K("A season end must be provided if a start is provided.")));
				if (!start.HasValue && end.HasValue)
					return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("A season start must be provided if an end is provided.")));

				if (!start.HasValue) // implies !end.HasValue
					return null; // no season is valid

				// Both the start and end are inclusive dates so if equal it represents a valid 1 day season
				TimeSpan seasonLength = end.Value - start.Value + Extensions.TimeSpan.OneDay;
				if (seasonLength == TimeSpan.Zero || seasonLength == Extensions.TimeSpan.OneYear)
					// seasonLength is 0 when you have a trivial season spanning the year boundary (e.g. March 1 to Feb 29)
					// and seasonLength is one year for the trivial season Jan 1 to Dec 31
					// All other non spanning seasons give values between these, and spanning non trivial seasons
					// give negative values.
					return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("These limits specify the entire year.")));
				return null;
			}).Operand1(startNodeId)
			.Operand2(endNodeId);
		}
		#endregion

		#region WeekdayCheck
		internal class WeekdayChecker : CheckN<bool> {
			public WeekdayChecker(params object[] days)
				: base(days) {
			}

			protected override EditLogic.ValidatorAndCorrector MakeBareChecker(EditLogic ec) {
				return new EditLogic.ValidatorAndCorrector(ec, 7, delegate (object[] operands) {
					for (int i = operands.Length; --i >= 0;)
						if ((bool)operands[i])
							return null;
					return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(new GeneralException(KB.K("At least one weekday should be enabled.")));
				}, this);
			}
		}
		private static Check WeekdayCheck(params object[] days) {
			return new WeekdayChecker(days);
		}
		#endregion

		public static DelayedCreateTbl ScheduledWorkOrderBrowserTbl;
		private static DelayedCreateTbl CreateUnscheduledWorkOrderTbl;
		private static DelayedCreateTbl PMGenerationDetailSchedulingTerminatedPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailDeferredPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailPredictedWorkOrderPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailErrorPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailErrorMakingWorkOrderPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailUncommittedMakeWorkOrderPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailCommittedMakeWorkOrderPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailMakeUnscheduledWorkOrderPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailInhibitedPanelTbl;
		private static DelayedCreateTbl PMGenerationDetailManualReschedulePanelTbl;
		public static DelayedCreateTbl PMGenerationBatchBrowserTbl;
		private static DelayedCreateTbl PMGenerationBatchEditTbl;
		private static DelayedCreateTbl PMGenerationBatchChangeMultipleSchedulingBasisEditTbl;
		#region BasisAlgorithm
		private static readonly Key[] BasisAlgorithmLabels = new Key[]
			{
				KB.K("Work Start"),
				KB.K("Work End"),
				KB.K("Scheduled Date")
			};
		private static readonly object[] BasisAlgorithms = new object[]
			{
				(int)DatabaseEnums.RescheduleBasisAlgorithm.FromWorkOrderStartDate,
				(int)DatabaseEnums.RescheduleBasisAlgorithm.FromWorkOrderEndDate,
				(int)DatabaseEnums.RescheduleBasisAlgorithm.FromScheduleBasis
			};
		public static EnumValueTextRepresentations BasisAlgorithmProvider = new EnumValueTextRepresentations(BasisAlgorithmLabels, null, BasisAlgorithms, 2);
		#endregion
		#region Periodicity Interval
		// this table is here so the String harvestor will define labels for the Enum values of DatabaseEnums.CalendarUnit.
		// TODO: Make harvester recognize EnumValueTextRepresentations defined with the EnumTypeInfo signature and extract the values of the Enum within the context specified to EnumValueTextRepresentations to eliminate this array.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "String harvester needs this")]
		private static readonly Key[] CalendarUnitLabels = new Key[]
		{
			KB.K("Months"),
			KB.K("Days")
		};
		public static EnumValueTextRepresentations PeriodicityIntervalProvider = new EnumValueTextRepresentations(new EnumTypeInfo(true, typeof(DatabaseEnums.CalendarUnit)), ContextReference.New("Thinkage.MainBoss.Controls"));
		#endregion
		#region PM DetailTypes
		private static readonly Key[] PMTypeLabels = new Key[] {
			KB.K("Deferred"),
			KB.K("Error"),
			KB.K("Work Order Creation Error"),
			KB.K("Make Work Order"),
			KB.K("Make Shared Work Order"),
			KB.K("Unplanned Work Order"),
			KB.K("After Scheduling Period"),
			KB.K("Manual Scheduling Basis"),
			KB.K("Inhibited"),
			KB.K("Predicted Work Order")
		};
		private static readonly object[] PMTypeValues = new object[] {
			(int)DatabaseEnums.PMType.Deferred,
			(int)DatabaseEnums.PMType.Error,
			(int)DatabaseEnums.PMType.ErrorMakingWorkOrder,
			(int)DatabaseEnums.PMType.MakeWorkOrder,
			(int)DatabaseEnums.PMType.MakeSharedWorkOrder,
			(int)DatabaseEnums.PMType.MakeUnscheduledWorkOrder,
			(int)DatabaseEnums.PMType.SchedulingTerminated,
			(int)DatabaseEnums.PMType.ManualReschedule,
			(int)DatabaseEnums.PMType.Inhibited,
			(int)DatabaseEnums.PMType.PredictedWorkOrder
		};
		public static EnumValueTextRepresentations PMTypesProvider = new EnumValueTextRepresentations(PMTypeLabels, null, PMTypeValues);
		#endregion
		#region TaskUnitPriorityProvider
		public static EnumValueTextRepresentations TaskUnitPriorityProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Prefer value from Task"),
				KB.K("Prefer value from Unit"),
				KB.K("Only use value from Task"),
				KB.K("Only use value from Unit"),
			},
			null,
			new object[] {
				(int)DatabaseEnums.TaskUnitPriority.PreferTaskValue,
				(int)DatabaseEnums.TaskUnitPriority.PreferUnitValue,
				(int)DatabaseEnums.TaskUnitPriority.OnlyTaskValue,
				(int)DatabaseEnums.TaskUnitPriority.OnlyUnitValue,
			}
		);
		#endregion
		#region Tbl Creation for PMGenerationBatch records
		private static readonly SqlExpression AllNonHiddenScheduledWorkOrders = new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.Hidden).IsNull();
		private static TblUnboundControlNode MultiplePlanPickerControl(SqlExpression filter = null) {
			var ecolArgs = new List<ECol.ICtorArg> {
				ECol.OmitInUpdateAccess,
				Fmt.SetUseChecks(true),
				ECol.SetUserChangeNotify(),
				Fmt.SetId(MaintenancePlansId),
				Fmt.SetCreatedT<SchedulingBaseEditLogic>(
				delegate (SchedulingBaseEditLogic editor, IBasicDataControl valueCtrl) {
					editor.ScheduledWorkOrderSelectionControl = valueCtrl;
				}
			)
			};
			if (filter != null)
				ecolArgs.Add(
					Fmt.SetInitialValue(
						delegate (CommonLogic editor) {
							var initialValue = new Set<object>(dsMB.Schema.T.ScheduledWorkOrder.F.Id.EffectiveType);
							editor.DB.Session.Server.DataColumnConverters(dsMB.Schema.T.ScheduledWorkOrder.F.Id.EffectiveType, out FromDSType fromDSType, out ToDSType toDSType);
							using (System.Data.DataSet ds = editor.DB.Session.ExecuteCommandReturningTable(
									new SelectSpecification(dsMB.Schema.T.ScheduledWorkOrder,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.Id) },
										filter,
										null)))
								foreach (System.Data.DataRow row in ds.Tables[0].Rows)
									initialValue.Add(fromDSType(row[0]));
							return initialValue;
						}
					)
			);
			return TblUnboundControlNode.New(KB.TOi(TId.UnitMaintenancePlan), new SetTypeInfo(false, dsMB.Schema.T.PMGenerationDetail.F.ScheduledWorkOrderID.EffectiveType, 1),
					new NonDefaultCol(),
					new ECol(ecolArgs.ToArray())
				);
		}
		static void CallChangeScheduleBasisForm(CommonLogic logic, Set<object> planSelection) {
			ITblDrivenApplication appInstance = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>();
			var initList = new List<TblActionNode> {
				// TODO: If caller is editing a Deleted record (mode is ViewDeleted or EditDeleted..., called picker should be in ALL mode. This is a general problem, see W20140007
				Init.OnLoadNew(new ControlTarget(MaintenancePlansId), new ConstantValue(planSelection))
			};
			appInstance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(logic.CommonUI.UIFactory, logic.DB, PMGenerationBatchChangeMultipleSchedulingBasisEditTbl,
				EdtMode.New,
				new[] { Array.Empty<object>() },
				ApplicationTblDefaults.NoModeRestrictions,
				new[] { initList },
				((ICommonUI)logic.CommonUI).Form, logic.CallEditorsModally,
				null);
		}
		static void CallValidationForm(CommonLogic logic, Set<object> planSelection) {
			ITblDrivenApplication appInstance = Libraries.Application.Instance.GetInterface<ITblDrivenApplication>();
			appInstance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(logic.CommonUI.UIFactory, logic.DB, ValidateMaintenencePlanEditLogic.ScheduledWorkOrderValidationTbl,
				EdtMode.View,
				planSelection.Select(id => new[] { id }).ToArray(),
				ApplicationTblDefaults.NoModeRestrictions,
				new[] { new List<TblActionNode>() },
				((ICommonUI)logic.CommonUI).Form, logic.CallEditorsModally,
				null);
		}
		static void PMGenerationBatchGenerationParameters(List<TblLayoutNode> generationParametersGroup) {
			// These records have two distinct saved states, committed and uncommitted. The "scheduling parameter" controls are readonly in all saved modes so we can code
			// this in the ECol's. "generation parameter" controls are only readonly once committed, so we have to rely on a custom changeEnabler for them.
			// FInally, there are "regular fields" which are writeable anytime.

			generationParametersGroup.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.PurchaseOrderCreationStateID,
				new FeatureGroupArg(TIGeneralMB3.PurchasingGroup),
				new DCol(Fmt.SetDisplayPath(dsMB.Path.T.PurchaseOrderState.F.Code)),
				new ECol(ECol.AddChangeEnablerT<PMGenerationBatchBaseEditLogic>(editor => editor.CommittedDisabler))
			));
			generationParametersGroup.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.WorkOrderCreationStateID,
				new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderState.F.Code)),
				new ECol(ECol.AddChangeEnablerT<PMGenerationBatchBaseEditLogic>(editor => editor.CommittedDisabler))
			));
			generationParametersGroup.Add(TblSectionNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal },
				TblGroupNode.New(KB.K("For Access Code"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
					TblColumnNode.New(null, dsMB.Path.T.PMGenerationBatch.F.AccessCodeUnitTaskPriority,
						new ECol(ECol.AddChangeEnablerT<PMGenerationBatchBaseEditLogic>(editor => editor.CommittedDisabler))
					)
				)
			));
			generationParametersGroup.Add(TblSectionNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal, AccountingFeatureArg },
				TblGroupNode.New(KB.K("For Expense Model"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
					TblColumnNode.New(null, dsMB.Path.T.PMGenerationBatch.F.WorkOrderExpenseModelUnitTaskPriority,
						new ECol(ECol.AddChangeEnablerT<PMGenerationBatchBaseEditLogic>(editor => editor.CommittedDisabler))
					)
				)
			));
			// The following two nodes are for the browser panel instead of the editor. DefaultVisibility of these can be placed between the two editor controls above
			// because it breaks up the sequence of TblSectionNodes.
			generationParametersGroup.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.AccessCodeUnitTaskPriority, DCol.Normal));
			generationParametersGroup.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.WorkOrderExpenseModelUnitTaskPriority, DCol.Normal, AccountingFeatureArg));
		}

		#endregion
		#region ViewPMGenerationDetailWorkOrderVerb
		// in order that all commands share the same button requires the button label be object identical amongst the commands
		private readonly static Key viewLabel = KB.K("View Work Order");
		// in order that multiple selection of records that use different AdditionalView verbs from this function requires the enable Tip to be object identical amongst the commands
		private readonly static Key viewTip = KB.K("View the generated Work Order");
		private static CompositeView.ICtorArg ViewPMGenerationDetailWorkOrderVerb(DBI_PathToRow pathToDetailRecord) {
			DBI_Path linkageToWorkOrder = new DBI_Path(pathToDetailRecord, dsMB.Path.T.PMGenerationDetail.F.WorkOrderID);
			return CompositeView.AdditionalViewVerb(viewLabel, viewTip,
				TIWorkOrder.WorkOrderEditTblCreator,
				linkageToWorkOrder,
				new SqlExpression(linkageToWorkOrder).IsNotNull(),
				KB.K("The Planned Maintenance Batch has not been committed so there is no Work Order to view"));
		}
		#endregion
		private static TblLayoutNode WorkEndDateNodeFromNextAvailableDate(Key k) {
			return TblInitSourceNode.New(k, new BrowserCalculatedInitValue(dsMB.Path.T.PMGenerationDetail.F.NextAvailableDate.ReferencedColumn.EffectiveType,
				(values => values[0] == null ? null : (object)((DateTime)values[0]).AddDays(-1)), new BrowserPathValue(dsMB.Path.T.PMGenerationDetail.F.NextAvailableDate)), DCol.Normal);
		}
		internal static void DefineTblEntries() {
			#region Schedule
			DefineTbl(dsMB.Schema.T.Schedule, delegate () {
				return new Tbl(dsMB.Schema.T.Schedule, TId.MaintenanceTiming,
							new Tbl.IAttr[] {
								SchedulingGroup,
								new BTbl(
									BTbl.ListColumn(dsMB.Path.T.Schedule.F.Code),
									BTbl.ListColumn(dsMB.Path.T.Schedule.F.Desc),
									BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.MaintenanceTimings))
								),
								new ETbl()
						},
							new TblLayoutNodeArray(
								DetailsTabNode.New(
									TblColumnNode.New(dsMB.Path.T.Schedule.F.Code, DCol.Normal, ECol.Normal),
									TblColumnNode.New(dsMB.Path.T.Schedule.F.Desc, DCol.Normal, ECol.Normal),
									TblColumnNode.New(dsMB.Path.T.Schedule.F.Comment, DCol.Normal, ECol.Normal)),
								BrowsetteTabNode.New(TId.Period, TId.MaintenanceTiming,
									TblColumnNode.NewBrowsette(dsMB.Path.T.Periodicity.F.ScheduleID, DCol.Normal, ECol.Normal)),
								TblTabNode.New(KB.K("Exceptions"), KB.K("Exceptions to periodic scheduling"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
									TblColumnNode.New(dsMB.Path.T.Schedule.F.InhibitIfOverdue,
										Fmt.SetLabelPositioning(Fmt.LabelPositioning.BlankOnSide),
										Fmt.SetEnumText(EnumValueTextRepresentations.NewForBool(
											KB.K("Defer overdue work orders to the date maintenance is generated"), null,
											KB.K("Inhibit generation of overdue work orders"), null
										)),
										DCol.Normal, ECol.Normal
									),
									TblGroupNode.New(KB.K("Seasonal exceptions"), new TblLayoutNode.ICtorArg[] { ECol.Normal, DCol.Normal },
										// TODO: Instead of a special Usage for date-in-year, we could also have a Fmt attribute which yields a custom TypeEditTextHandler and/or TypeTextFormatter.
										// Then when we allow Fmt specifications directly in the TblLayoutNode, and have empty-column removal in multicolumn layouts, we could put the DCol's
										// in the same layout nodes as the ECols and just specify the formatter once.
										TblMultiColumnNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, new Key[] { KB.K("Start"), KB.K("End"), KB.K("Example for the fourth of May") },
											TblRowNode.New(KB.K("Season"), new TblLayoutNode.ICtorArg[] { ECol.Normal },
												TblColumnNode.New(dsMB.Path.T.Schedule.F.SeasonStart,
													new ECol(
														Fmt.SetId(SeasonStartId),
														Fmt.SetEditTextHandler((type) => new DateInYearFormatter((IntervalTypeInfo)type, Thinkage.Libraries.Application.InstanceFormatCultureInfo))
													)
												),
												TblColumnNode.New(dsMB.Path.T.Schedule.F.SeasonEnd,
													new ECol(
														Fmt.SetId(SeasonEndId),
														Fmt.SetEditTextHandler((type) => new DateInYearFormatter((IntervalTypeInfo)type, Thinkage.Libraries.Application.InstanceFormatCultureInfo))
													)
												),
												TblUnboundControlNode.New(dsMB.Path.T.Schedule.F.SeasonStart.ReferencedColumn.EffectiveType,
													new ECol(
														ECol.AllReadonlyAccess,
														Fmt.SetEditTextHandler((type) => new DateInYearFormatter((IntervalTypeInfo)type, Thinkage.Libraries.Application.InstanceFormatCultureInfo)),
														Fmt.SetInitialValue(new TimeSpan(31 + 29 + 31 + 30 + 4 - 1, 0, 0, 0))   // Jan+feb(leap)+mar+april+4th-offset
													)
												)
											)
										),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.SeasonStart,
											new DCol(Fmt.SetEditTextHandler((type) => new DateInYearFormatter((IntervalTypeInfo)type, Thinkage.Libraries.Application.InstanceFormatCultureInfo)))
										),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.SeasonEnd,
											new DCol(Fmt.SetEditTextHandler((type) => new DateInYearFormatter((IntervalTypeInfo)type, Thinkage.Libraries.Application.InstanceFormatCultureInfo)))
										),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.InhibitSeason,
											Fmt.SetLabelPositioning(Fmt.LabelPositioning.BlankOnSide),
											Fmt.SetEnumText(EnumValueTextRepresentations.NewForBool(
												KB.K("Defer work orders triggered outside the season until the start of the season"), null,
												KB.K("Inhibit generation of work orders outside the season"), null
											)),
											DCol.Normal, ECol.Normal
										)
									),
									TblGroupNode.New(KB.K("Weekday exceptions"), new TblLayoutNode.ICtorArg[] { ECol.Normal, DCol.Normal },
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableMonday, DCol.Normal, new ECol(Fmt.SetId(MonId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableTuesday, DCol.Normal, new ECol(Fmt.SetId(TueId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableWednesday, DCol.Normal, new ECol(Fmt.SetId(WedId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableThursday, DCol.Normal, new ECol(Fmt.SetId(ThuId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableFriday, DCol.Normal, new ECol(Fmt.SetId(FriId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableSaturday, DCol.Normal, new ECol(Fmt.SetId(SatId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.EnableSunday, DCol.Normal, new ECol(Fmt.SetId(SunId))),
										TblColumnNode.New(dsMB.Path.T.Schedule.F.InhibitWeek,
											Fmt.SetLabelPositioning(Fmt.LabelPositioning.BlankOnSide),
											Fmt.SetEnumText(EnumValueTextRepresentations.NewForBool(
												KB.K("Defer work orders triggered on disabled weekdays until the next enabled weekday"), null,
												KB.K("Inhibit generation of work orders on disabled weekdays"), null
											)),
											DCol.Normal, ECol.Normal
										)
									)
								),
							BrowsetteTabNode.New(TId.UnitMaintenancePlan, TId.MaintenanceTiming,
								TblColumnNode.NewBrowsette(ScheduledWorkOrderBrowserTbl, dsMB.Path.T.ScheduledWorkOrder.F.ScheduleID, DCol.Normal, ECol.Normal))),
							SeasonStartEndCheck(SeasonStartId, SeasonEndId),
							WeekdayCheck(MonId, TueId, WedId, ThuId, FriId, SatId, SunId)
						);
			});
			RegisterExistingForImportExport(TId.MaintenanceTiming, dsMB.Schema.T.Schedule);

			DelayedCreateTbl calendarPeriodTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Periodicity, TId.CalendarPeriod,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.ScheduleID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Schedule.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.Interval, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.CalendarUnit, DCol.Normal, new ECol(ECol.ForceValue()))
				),
				Init.OnLoadNew(dsMB.Path.T.Periodicity.F.CalendarUnit, new ConstantValue(DatabaseEnums.CalendarUnit.Months))    // A somewhat arbitrary choice but I expect Months are more likely than Days.
				);
			});
			DelayedCreateTbl meterPeriodTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Periodicity, TId.MeterPeriod,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.ScheduleID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Schedule.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.MeterClassID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.MeterClass.F.Code)), new ECol(ECol.ForceValue())),
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.Interval, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Periodicity.F.MeterClassID.F.UnitOfMeasureID.F.Code, DCol.Normal, ECol.AllReadonly)
				)
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.Periodicity, delegate () {
				return new CompositeTbl(dsMB.Schema.T.Periodicity, TId.Period,
					new Tbl.IAttr[] {
						SchedulingGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Periodicity.F.Interval),
							BTbl.ListColumn(dsMB.Path.T.Periodicity.F.CalendarUnit),
							BTbl.ListColumn(dsMB.Path.T.Periodicity.F.MeterClassID.F.Code)
						)
					},
					CompositeView.ChangeEditTbl(meterPeriodTbl,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.Periodicity.F.MeterClassID).IsNotNull())
					),
					CompositeView.ChangeEditTbl(calendarPeriodTbl,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.Periodicity.F.MeterClassID).IsNull())
					)
				);
			});
			// This Tbl is purely for export/import of Timing Periodicity records. The Calendar Unit field is exported/imported to define the type of interval
			RegisterForImportExport(TId.TimingPeriod, delegate () {
				return new Tbl(dsMB.Schema.T.Periodicity, TId.TimingPeriod,
					new Tbl.IAttr[] {
						SchedulingGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.Periodicity.F.ScheduleID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Schedule.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Periodicity.F.CalendarUnit, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Periodicity.F.Interval, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Periodicity.F.MeterClassID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.MeterClass.F.Code)), new ECol(ECol.ForceValue())),
						TblColumnNode.New(dsMB.Path.T.Periodicity.F.MeterClassID.F.UnitOfMeasureID.F.Code, DCol.Normal, ECol.AllReadonly)
					)
				);
			});
			#endregion
			#region ScheduledWorkOrder
			var detailSingleRecordBrowsetteTblCreator = new DelayedCreateTbl(
				delegate () {
					return new CompositeTbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailWithContainers,
						new Tbl.IAttr[] {
							new BTbl(BTbl.SetNoRefreshCommand(), BTbl.SetTreatAllRecordsAsActive(true)),
						},
						CompositeView.ChangeEditTbl(PMGenerationDetailCommittedMakeWorkOrderPanelTbl,                                                           // MakeWorkOrder or MakeSharedWorkOrder
							ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.PMGenerationDetail),
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(
								new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.MakeWorkOrder))
									.Or(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.MakeSharedWorkOrder)))
								)),
						CompositeView.ChangeEditTbl(PMGenerationDetailMakeUnscheduledWorkOrderPanelTbl,                                                         // MakeUnscheduledWorkOrder
							ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.PMGenerationDetail),
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.MakeUnscheduledWorkOrder)))),
						CompositeView.ChangeEditTbl(PMGenerationDetailInhibitedPanelTbl,                                                                        // Inhibited
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.Inhibited)))),
						CompositeView.ChangeEditTbl(PMGenerationDetailManualReschedulePanelTbl,                                                                 // ManualReschedule
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.ManualReschedule)))),
						CompositeView.ChangeEditTbl(PMGenerationDetailPredictedWorkOrderPanelTbl,                                                                 // PredictedWorkOrder
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.PredictedWorkOrder)))),
						CompositeView.ChangeEditTbl(PMGenerationDetailErrorPanelTbl,                                                                 // Error
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.Error)))),
						CompositeView.ChangeEditTbl(PMGenerationDetailErrorMakingWorkOrderPanelTbl,                                                                 // ErrorMakingWorkOrder
							CompositeView.RecognizeByValidEditLinkage(), CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetail.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.ErrorMakingWorkOrder))))
					);
				}
			);
			object nodeid = KB.I("nodeid");
			TblLayoutNodeArray swoNodes = new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.WorkOrderTemplateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderTemplate.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.UnitLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.InitialWOStatusID, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.InitialWOComment, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.Inhibit, new DCol(Fmt.SetLabelPositioning(Fmt.LabelPositioning.BlankOnSide), Fmt.SetEnumText(UnitMaintenancePlanInhibitEnumText))),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.Inhibit, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.SlackDays, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.ScheduleID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Schedule.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.RescheduleBasisAlgorithm, DCol.Normal, ECol.Normal),
						TblColumnNode.New(KB.K("Schedule Basis Date"),dsMB.Path.T.ScheduledWorkOrder.F.CurrentPMGenerationDetailID.F.ScheduleDate, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(KB.K("Last Generation"),dsMB.Path.T.ScheduledWorkOrder.F.LastPMGenerationDetailID.F.PMGenerationBatchID.F.EntryDate, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrder.F.LastPMGenerationDetailID.F.DetailType,DCol.Normal, ECol.AllReadonly),
						TblGroupNode.New(dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.AllReadonly },
							//TblColumnNode.NewBrowsette(detailSingleRecordBrowsetteTblCreator, dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID, dsMB.Path.T.PMGenerationDetail.F.Id, DCol.Normal, ECol.Normal,
							// The following filter is redundant (and always true) but it serves to elide the SWO information from the panel.
							// TODO: This does not work; there is a known problem that additional filters such as this one are applied to the browser after panel and list-column creation
							// and so elision does not work.
							//	Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID, null, true))
							//),
							// Because of the above TODO, we instead make the primary binding of the browsette be to all the Details for the SWO, and an additional filter picks the current one.
							// This means the sql query will have redundant WHERE conditions.
							// We set DCol.Layouts.VisibleInNonBrowsetteArea so the browsette is always created, even if the SWO browser is itself a browsette.
							TblColumnNode.NewBrowsette(detailSingleRecordBrowsetteTblCreator, dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID, new DCol(DCol.LayoutOptions(DCol.Layouts.VisibleInNonBrowsetteArea)), ECol.Normal,
								new BrowsetteFilterBind(nodeid, dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID),
								Fmt.SetBrowserFilter(BTbl.TaggedEqFilter(dsMB.Path.T.PMGenerationDetail.F.Id, nodeid))
							)
						)
					),
					BrowsetteTabNode.New(TId.GenerationDetail, TId.UnitMaintenancePlan,
						TblColumnNode.NewBrowsette(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.ScheduledWorkOrderID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.UnitMaintenancePlan,
						TblColumnNode.NewBrowsette(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.ScheduledWorkOrderID, DCol.Normal, ECol.Normal,
							Fmt.SetBrowserFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.MakeWorkOrder))
								.Or(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq((int)DatabaseEnums.PMType.MakeSharedWorkOrder))
								.Or(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq((int)DatabaseEnums.PMType.MakeUnscheduledWorkOrder)))))),
					TblTabNode.New(KB.K("Errors"), KB.K("Display only the generation errors"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.NewBrowsette(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.ScheduledWorkOrderID, DCol.Normal, ECol.Normal,
							Fmt.SetBrowserFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.Error))
								.Or(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq((int)DatabaseEnums.PMType.ErrorMakingWorkOrder))))))
				);
			;

			DelayedCreateTbl swoEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.ScheduledWorkOrder, TId.UnitMaintenancePlan,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new ETbl(
						ETbl.CustomCommand(
							delegate(EditLogic el) {
								var group = new EditLogic.MutuallyExclusiveCommandSetDeclaration{
								new EditLogic.CommandDeclaration(
									SetScheduleBasisCommandText,
									EditLogic.StateTransitionCommand.NewSameTargetState(el,
										null,	// TODO: Put a tip here ?? What about the others, which are (or will be) just AdditionalNewVerbs on browsers??
										delegate() {
											Set<object> callerSelection = new Set<object>(el.TInfo.Schema.InternalIdColumn.EffectiveType) { el.RootRowIDs[0] };
											CallChangeScheduleBasisForm(el, callerSelection);
										},
										el.AllStatesWithExistingRecord.ToArray()))
								};
								return group;
							}
						),
						ETbl.CustomCommand(
							// TODO: This might now work because we're calling what is built as an editor on the SWO from an editor for the same SWO.
							delegate(EditLogic el) {
								var group = new EditLogic.MutuallyExclusiveCommandSetDeclaration{
								new EditLogic.CommandDeclaration(
									ValidateScheduledWorkOrderCommandText,
									EditLogic.StateTransitionCommand.NewSameTargetState(el,
										null,	// TODO: Put a tip here ?? What about the others, which are (or will be) just AdditionalNewVerbs on browsers??
										delegate() {
											Set<object> callerSelection = new Set<object>(el.TInfo.Schema.InternalIdColumn.EffectiveType) { el.RootRowIDs[0] };
											CallValidationForm(el, callerSelection);
										},
										el.AllStatesWithExistingRecord.ToArray()))
								};
								return group;
							}
						)
					),
				},
				(TblLayoutNodeArray)swoNodes.Clone()
				);
			});
			DefineEditTbl(dsMB.Schema.T.ScheduledWorkOrder, swoEditTbl);
			RegisterForImportExport(TId.UnitMaintenancePlan, swoEditTbl);

			// TODO: This does not need to be a named Tbl. Each place it is used, its value would be used anyway because it is the registered browse tbl for ScheduledWorkOrder.
			ScheduledWorkOrderBrowserTbl = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.ScheduledWorkOrder, TId.UnitMaintenancePlan,
					new Tbl.IAttr[] {
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ScheduledWorkOrder.F.WorkOrderTemplateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ScheduledWorkOrder.F.UnitLocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ScheduledWorkOrder.F.ScheduleID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ScheduledWorkOrder.F.CurrentPMGenerationDetailID.F.NextAvailableDate),
							// TODO: The following should be coded just as an AdditionalNewVerb, except that we have no InitSource that can calculate the correct value.
							// Perhaps the CalculatedSource could be extended to give its delegate access to the Logic object.
							BTbl.AdditionalVerb(SetScheduleBasisCommandText,
								delegate(BrowseLogic browserLogic) {
									return new CallDelegateCommand(	// TODO: Put a tip here ?? What about the others, which are (or will be) just AdditionalNewVerbs on browsers??
										delegate() {
											CallChangeScheduleBasisForm(browserLogic, browserLogic.BrowseContextRecordIds);
										}
									);
								}
							),
							BTbl.AdditionalVerb(ValidateScheduledWorkOrderCommandText,
								delegate(BrowseLogic browserLogic) {
									return new CallDelegateCommand(	// TODO: Put a tip here ?? What about the others, which are (or will be) just AdditionalNewVerbs on browsers??
										delegate() {
											CallValidationForm(browserLogic, browserLogic.BrowseContextRecordIds);
										}
									);
								}
							),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ScheduledWorkOrderReport))
						)
					},
					CompositeView.ChangeEditTbl(swoEditTbl, NoNewMode,              // list processed in reverse order, default must be first
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(true)),
						CompositeView.IdentificationOverride(TId.GenerationDetailSchedulingTerminated)
					),
					CompositeView.ChangeEditTbl(swoEditTbl, null,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID).IsNull()),
						CompositeView.UseSamePanelAs(0),
						CompositeView.IdentificationOverride(TId.UnitMaintenancePlan)
					),
					CompositeView.ChangeEditTbl(swoEditTbl, NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.CurrentPMGenerationDetailID).IsNull()),
						CompositeView.UseSamePanelAs(0),
						CompositeView.IdentificationOverride(TId.UnitMaintenancePlanNeedScheduleBases)
					),
					CompositeView.ChangeEditTbl(swoEditTbl, NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeWorkOrder))),
						CompositeView.UseSamePanelAs(0),
						CompositeView.IdentificationOverride(TId.UnitMaintenancePlanWorkOrderOk)
					),
					CompositeView.ChangeEditTbl(swoEditTbl, NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Inhibited))),
						CompositeView.UseSamePanelAs(0),
						CompositeView.IdentificationOverride(TId.UnitMaintenancePlanWorkInhibited)
					),
					CompositeView.ChangeEditTbl(swoEditTbl, NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Error))),
						CompositeView.UseSamePanelAs(0),
						CompositeView.IdentificationOverride(TId.UnitMaintenancePlanWorkError)
					),
					CompositeView.ChangeEditTbl(swoEditTbl, NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ScheduledWorkOrder.F.StatusPMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.ErrorMakingWorkOrder))),
						CompositeView.UseSamePanelAs(0),
						CompositeView.IdentificationOverride(TId.UnitMaintenancePlanWorkErrorMakingWorkorder)
					),
					CompositeView.ExtraNewVerb(CreateUnscheduledWorkOrderTbl,
						NoNewMode,
						CompositeView.ContextualInit(new int[] { 0, 1, 2, 3, 4, 5, 6 }, new CompositeView.Init(new PathTarget(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID, 1), dsMB.Path.T.ScheduledWorkOrder.F.Id))
					)
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.ScheduledWorkOrder, ScheduledWorkOrderBrowserTbl);
			#endregion
			#region CreateUnscheduledWorkOrderTbl
			CreateUnscheduledWorkOrderTbl = new DelayedCreateTbl(delegate () {
				var layout = new List<TblLayoutNode> {
					// We could make the Detail be the root of this Tbl and have it link to the Batch but all the paths would have to be mapped.
					// Instead we use a second recordset for the Detail record.
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID, 1, ECol.Normal)
				};
				PMGenerationBatchGenerationParameters(layout);
				layout.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.Comment, DCol.Normal, ECol.Normal));

				return new Tbl(dsMB.Schema.T.PMGenerationBatch, TId.UnplannedMaintenanceWorkOrder,
					new Tbl.IAttr[] {
						new ETbl(
							ETbl.SetCloseHandler( delegate(EditLogic l) {
								if (l.EditMode == EdtMode.New) {
									// Because in New mode Save means Commit we don't want to offer the standard question.
									// The only options we offer are to continue closing or to cancel the close.
									switch (Ask.Question(KB.K("You have not created the work order. Are you sure you want to quit?").Translate())) {
									case Ask.Result.Yes:
										return ETbl.CloseHandlerClassArg.CloseHandlerDecision.Close;
									case Ask.Result.No:
										return ETbl.CloseHandlerClassArg.CloseHandlerDecision.DoNotClose;
									}
								}
								return ETbl.CloseHandlerClassArg.CloseHandlerDecision.YouDecide;
							}),
							ETbl.LogicClass(typeof(PMGenerationManualScheduledWorkOrderEditLogic)),
							ETbl.CreateCustomDataSet(true),
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.New)
						)	// Once saved, these are viewed/edited using the regular PM batch editor.
					},
					new TblLayoutNodeArray(layout),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.UserID), new UserIDValue()),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.SinglePurchaseOrders), new ConstantValue(true)),

					Init.LinkRecordSets(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID, 1, dsMB.Path.T.PMGenerationBatch.F.Id, 0),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationDetail.F.DetailType, 1), new ConstantValue(DatabaseEnums.PMType.MakeUnscheduledWorkOrder)),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationDetail.F.Sequence, 1), new ConstantValue(0)),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationDetail.F.FirmScheduleData, 1), new ConstantValue(true))
				);
			});
			#endregion
			#region PMGenerationBatch
			PMGenerationBatchEditTbl = new DelayedCreateTbl(
				delegate () {
					// These records have two distinct saved states, committed and uncommitted. The "scheduling parameter" controls are readonly in all saved modes so we can code
					// this in the ECol's. "generation parameter" controls are only readonly once committed, so we have to rely on a custom changeEnabler for them.
					// Finally, there are "regular fields" which are writeable anytime.
					var detailTabLayout = new List<TblLayoutNode> {
						TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.UserID.F.ContactID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)
					};

					List<TblLayoutNode> schedulingParametersGroup = new List<TblLayoutNode> {
						// These nodes represent the "scheduling parameters"
						TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.EntryDate, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
						TblVariableNode.New(KB.K("Generation Interval"), dsMB.Schema.V.PmGenerateInterval, new DefaultOnlyCol(), DCol.Normal, ECol.ReadonlyInUpdate)
					};
					object BatchEndDateId = KB.I("BatchEndDateId");
					schedulingParametersGroup.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.EndDate, new NonDefaultCol(), DCol.Normal, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(BatchEndDateId))));
					// TODO: A picker tree-structured on Units would be nice. THis requires an explicit PickFrom in our ECol and might open a can of worms (only primary records should have checkboxes)
					// TODO: Eliminate the dsMB.Path.T.PMGenerationBatch.F.FilterUsed field.
					// TODO: What if the user does not have Browse permission on the SWO table?
					// The type info is Set of Link to SWO so that the Fmt created can tell what record type to put in the list control.
					schedulingParametersGroup.Add(MultiplePlanPickerControl(AllNonHiddenScheduledWorkOrders));
					detailTabLayout.Add(TblGroupNode.New(KB.K("Scheduling Parameters"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, schedulingParametersGroup.ToArray()));

					List<TblLayoutNode> generationParametersGroup = new List<TblLayoutNode> {
						TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.SinglePurchaseOrders,
						new FeatureGroupArg(PurchasingGroup), DCol.Normal,
						new ECol(ECol.AddChangeEnablerT<PMGenerationBatchBaseEditLogic>(editor => editor.CommittedDisabler))
					)
					};
					PMGenerationBatchGenerationParameters(generationParametersGroup);
					detailTabLayout.Add(TblGroupNode.New(KB.K("Work Order Generation Parameters"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, generationParametersGroup.ToArray()));

					detailTabLayout.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.Comment, DCol.Normal, ECol.Normal));

					return new Tbl(dsMB.Schema.T.PMGenerationBatch, TId.PlannedMaintenanceBatch,
						new Tbl.IAttr[] {
							SchedulingGroup,
							new ETbl(
								ETbl.SetCloseHandler(delegate(EditLogic l) {
									PMGenerationBatchEditLogic el = (PMGenerationBatchEditLogic)l;
									if (el.EditMode == EdtMode.Edit && el.SessionIdSource.GetValue() != null) {
										// We have saved but not committed.
										// The only options we offer are to continue closing or to cancel the close.
										switch (Ask.Question(KB.K("You have not committed the generated work orders. Are you sure you want to quit?").Translate())) {
										case Ask.Result.Yes:
											el.DeleteUncommittedSet();
											return ETbl.CloseHandlerClassArg.CloseHandlerDecision.Close;
										case Ask.Result.No:
											return ETbl.CloseHandlerClassArg.CloseHandlerDecision.DoNotClose;
										}
									}
									if (el.UserChanges && el.EditMode == EdtMode.New) {
										// Because in New mode Save means Generate we don't want to offer the standard question.
										// The only options we offer are to continue closing or to cancel the close.
										switch (Ask.Question(KB.K("You have not generated the work orders. Are you sure you want to quit?").Translate())) {
										case Ask.Result.Yes:
											return ETbl.CloseHandlerClassArg.CloseHandlerDecision.Close;
										case Ask.Result.No:
											return ETbl.CloseHandlerClassArg.CloseHandlerDecision.DoNotClose;
										}
									}
									return ETbl.CloseHandlerClassArg.CloseHandlerDecision.YouDecide;
								}),
								ETbl.LogicClass(typeof(PMGenerationBatchEditLogic)),
								ETbl.CreateCustomDataSet(true),
								ETbl.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete)
							)
						},
						new TblLayoutNodeArray(
							DetailsTabNode.New(detailTabLayout.ToArray()),
							BrowsetteTabNode.New(TId.GenerationDetail, TId.PlannedMaintenanceBatch,
								TblColumnNode.NewBrowsette(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID, DCol.Normal, ECol.Normal)),
							BrowsetteTabNode.New(TId.WorkOrder, TId.PlannedMaintenanceBatch,
								TblColumnNode.NewBrowsette(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID, DCol.Normal, ECol.Normal,
									Fmt.SetBrowserFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.MakeWorkOrder))
										.Or(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq((int)DatabaseEnums.PMType.MakeSharedWorkOrder))
										.Or(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq((int)DatabaseEnums.PMType.MakeUnscheduledWorkOrder))
										)))),
							TblTabNode.New(KB.K("Errors"), KB.K("Display only the generation errors"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.NewBrowsette(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID, DCol.Normal, ECol.Normal,
									Fmt.SetBrowserFilter(BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)DatabaseEnums.PMType.Error))
										.Or(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq((int)DatabaseEnums.PMType.ErrorMakingWorkOrder))))))
						),
						Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.UserID), new UserIDValue()),
						new Check1<DateTime>(
							delegate (DateTime endDate) {
								if (endDate < DateTime.Today)
									return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(new GeneralException(KB.K("No Work Orders will be generated if Batch End Date is in the past")));
								return null;
							},
							TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New))
							.Operand1(BatchEndDateId)
					);
				}
			);

			PMGenerationBatchChangeMultipleSchedulingBasisEditTbl = new DelayedCreateTbl(delegate () {
				var layout = new List<TblLayoutNode>();
				var actions = new List<TblActionNode> {
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.UserID), new UserIDValue()),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.EndDate), new ConstantValue(null)),
					// Ensure the other required fields of the PMGenerationBatch record are initialized not withstanding the (possible) null settings in the default PMGenerationBatch
					// record. Otherwise, the user will be unable to save their new Scheduling basis: WO20130094
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.AccessCodeUnitTaskPriority), new ConstantValue(DatabaseEnums.TaskUnitPriority.PreferUnitValue)),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.WorkOrderExpenseModelUnitTaskPriority), new ConstantValue(DatabaseEnums.TaskUnitPriority.PreferUnitValue)),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.PMGenerationBatch.F.SinglePurchaseOrders), new ConstantValue(true))
				};

				layout.Add(MultiplePlanPickerControl());
				layout.Add(TblUnboundControlNode.New(KB.K("New Scheduling Basis"), dsMB.Schema.T.PMGenerationDetail.F.ScheduleDate.EffectiveType,
						new NonDefaultCol(),
						new ECol(
							ECol.OmitInUpdateAccess,
							ECol.SetUserChangeNotify(),
							Fmt.SetCreatedT<ScheduleBasisEditLogic>(delegate (ScheduleBasisEditLogic editor, IBasicDataControl valueCtrl) {
								editor.NewScheduleBasisCtrl = valueCtrl;
							}
							)
						)
				));
				layout.Add(TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.Comment, DCol.Normal, ECol.Normal));

				return new Tbl(dsMB.Schema.T.PMGenerationBatch, TId.SchedulingBasis,
					new Tbl.IAttr[] {
						SchedulingGroup,
						new ETbl(
							ETbl.LogicClass(typeof(ScheduleBasisEditLogic)),
							ETbl.CreateCustomDataSet(true),
							ETbl.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete, EdtMode.Edit, EdtMode.EditDefault)
							)
					},
					new TblLayoutNodeArray(layout), actions.ToArray()
				);
			});
			PMGenerationBatchBrowserTbl = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.PMGenerationBatch, TId.PlannedMaintenanceBatch,
					new Tbl.IAttr[] {
						new BTbl(
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.PMGenerationBatch.F.SessionID).IsNull()),
							BTbl.ListColumn(dsMB.Path.T.PMGenerationBatch.F.EntryDate),
							BTbl.ListColumn(dsMB.Path.T.PMGenerationBatch.F.EndDate)
						)
					},
					CompositeView.ChangeEditTbl(PMGenerationBatchEditTbl),
					CompositeView.ExtraNewVerb(PMGenerationBatchChangeMultipleSchedulingBasisEditTbl, CompositeView.JoinedNewCommand(SetScheduleBasisCommandText))
				);
			});
			DefineBrowseTbl(dsMB.Schema.T.PMGenerationBatch, PMGenerationBatchBrowserTbl);
			DefineEditTbl(dsMB.Schema.T.PMGenerationBatch, PMGenerationBatchEditTbl);
			#endregion
			#region PMGenerationDetail and customized versions thereof
			DefineTbl(dsMB.Schema.T.PMGenerationDetail, delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetail,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.DetailType, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.DetailContext, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.NextAvailableDate, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.FirmScheduleData, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.BasisDetails, DCol.Normal)
				));
			});
			#region - Customized versions depending on Detail Type
			#region -   PMGenerationDetailSchedulingTerminatedPanelTbl
			PMGenerationDetailSchedulingTerminatedPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailSchedulingTerminated,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EndDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(KB.K("Predicted Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal)
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailDeferredPanelTbl
			PMGenerationDetailDeferredPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailDeferred,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(KB.K("Reason for deferral"), dsMB.Path.T.PMGenerationDetail.F.DetailContext, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Proposed Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal)
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailPredictedWorkOrderPanelTbl
			PMGenerationDetailPredictedWorkOrderPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailPredictedWorkOrder,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(KB.K("Reason for uncertainty"), dsMB.Path.T.PMGenerationDetail.F.BasisDetails, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(KB.K("Predicted Schedule Date"), dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(KB.K("Predicted Schedule Reading"), dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Predicted Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Predicted Work End Date"))
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailErrorPanelTbl
			PMGenerationDetailErrorPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailError,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(KB.K("Error message"), dsMB.Path.T.PMGenerationDetail.F.DetailContext, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Proposed Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Proposed Work End Date"))
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailErrorMakingWorkOrderPanelTbl
			// Only occurs for committed batches
			PMGenerationDetailErrorMakingWorkOrderPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailErrorMakingWorkOrder,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(KB.K("Error message"), dsMB.Path.T.PMGenerationDetail.F.DetailContext, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Proposed Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Proposed Work End Date"))
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailUncommittedMakeWorkOrderPanelTbl
			// This panel is used in MakeWorkOrder and MakeSHaredWorkOrder detail records within uncommitted batches, where no WO actually exists (yet)
			PMGenerationDetailUncommittedMakeWorkOrderPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailUncommittedMakeWorkOrder,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Proposed Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Proposed Work End Date"))
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailCommittedMakeWorkOrderPanelTbl
			PMGenerationDetailCommittedMakeWorkOrderPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailCommittedMakeWorkOrder,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Provisional Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Provisional Work End Date"))
					),
					TblTabNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.Key(), KB.K("Generated Work Order"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Number, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Subject, DCol.Normal),
						CurrentStateHistoryGroup(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID,
							dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate,
							dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.Code,
							dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID.F.Code),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.StartDateEstimate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.EndDateEstimate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.WorkCategoryID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.AccessCodeID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.WorkOrderPriorityID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.WorkOrderExpenseModelID.F.Code, DCol.Normal, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.ProjectID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Description, DCol.Normal),
						// Remove the comments normally displayed to provide more vertical space
						//						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.ClosingComment, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Downtime, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.CloseCodeID.F.Code, DCol.Normal)
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailMakeUnscheduledWorkOrderPanelTbl
			// Only occurs for committed batches.
			PMGenerationDetailMakeUnscheduledWorkOrderPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailMakeUnscheduledWorkOrder,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(KB.K("Created on"), dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(KB.K("Provisional Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Provisional Work End Date"))
					),
					TblTabNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.Key(), KB.K("Generated Work Order"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Number, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Subject, DCol.Normal),
						CurrentStateHistoryGroup(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID,
							dsMB.Path.T.WorkOrderStateHistory.F.EffectiveDate,
							dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.Code,
							dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateHistoryStatusID.F.Code),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.StartDateEstimate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.EndDateEstimate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.WorkCategoryID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.AccessCodeID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.WorkOrderPriorityID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.WorkOrderExpenseModelID.F.Code, DCol.Normal, AccountingFeatureArg),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.ProjectID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Description, DCol.Normal),
						// Remove the comments normally displayed to provide more vertical space
						//						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.ClosingComment, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.Downtime, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.WorkOrderID.F.CloseCodeID.F.Code, DCol.Normal)
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailInhibitedPanelTbl
			PMGenerationDetailInhibitedPanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailInhibited,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(KB.K("Reason for inhibiting"), dsMB.Path.T.PMGenerationDetail.F.DetailContext, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduleReading, DCol.Normal),
						TblColumnNode.New(KB.K("Deemed Work Start Date"), dsMB.Path.T.PMGenerationDetail.F.WorkStartDate, DCol.Normal),
						WorkEndDateNodeFromNextAvailableDate(KB.K("Deemed Work End Date"))
					)
				));
			});
			#endregion
			#region -   PMGenerationDetailManualReschedulePanelTbl
			PMGenerationDetailManualReschedulePanelTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.PMGenerationDetail, TId.GenerationDetailManualReschedule,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.PMGenerationDetail.F.Sequence, BTbl.Contexts.SortInitialAscending)
					)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblFixedRecordTypeNode.New(null),
						TblColumnNode.New(dsMB.Path.T.PMGenerationBatch.F.EntryDate.Key(), dsMB.Path.T.PMGenerationDetail.F.PMGenerationBatchID.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.PMGenerationDetail.F.ScheduledWorkOrderID.F.ScheduleID.F.Code, DCol.Normal),
						TblColumnNode.New(KB.K("Basis date for scheduling"), dsMB.Path.T.PMGenerationDetail.F.ScheduleDate, DCol.Normal)
					)
				));
			});
			#endregion
			#endregion
			#endregion
			#region PMGenerationDetailAndScheduledWorkOrderAndLocation
			// Note that MakeWorkOrder and MakeSharedWorkOrder have to variantes, one for uncommitted batches, where it just shows information on the
			// proposed WO, and one for committed batches, where it shows the actual WO and allows you to view it.
			DefineBrowseTbl(dsMB.Schema.T.PMGenerationDetailAndScheduledWorkOrderAndLocation, delegate () {
				object localCodeColumnId = KB.I("Local Code Id");
				return new CompositeTbl(dsMB.Schema.T.PMGenerationDetailAndScheduledWorkOrderAndLocation, TId.GenerationDetailWithContainers,
					new Tbl.IAttr[] {
						new BTbl(
							BTbl.PerViewListColumn(CommonCodeColumnKey, localCodeColumnId),
							BTbl.ListColumn(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.WorkStartDate),
							BTbl.SetTreeStructure(null, 6, uint.MaxValue, dsMB.Schema.T.PMGenerationDetailAndContainers, treatAllRecordsAsPrimary: true)
						)
					},
					// PostalAddress
					new CompositeView(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.PostalAddressID,
						CompositeView.RecognizeByValidEditLinkage(),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PostalAddress.F.Code),
						ReadonlyView,
						CompositeView.ForceNotPrimary()),
					// Unit
					new CompositeView(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.RelativeLocationID.F.UnitID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.RelativeLocationID.F.ContainingLocationID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.Unit),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PermanentStorage),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PostalAddress),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PlainRelativeLocation),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.Unit.F.RelativeLocationID.F.Code),
						ReadonlyView,
						CompositeView.ForceNotPrimary()),
					// PermanentStorage
					new CompositeView(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.RelativeLocationID.F.PermanentStorageID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.RelativeLocationID.F.ContainingLocationID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.Unit),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PermanentStorage),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PostalAddress),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PlainRelativeLocation),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code),
						ReadonlyView,
						CompositeView.ForceNotPrimary()),
					// PlainRelativeLocation
					new CompositeView(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.RelativeLocationID.F.PlainRelativeLocationID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.LocationID.F.RelativeLocationID.F.ContainingLocationID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PostalAddress),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.PlainRelativeLocation),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code),
						ReadonlyView,
						CompositeView.ForceNotPrimary()),
					// ScheduledWorkOrder
					new CompositeView(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.ScheduledWorkOrderID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.ScheduledWorkOrderID.F.UnitLocationID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.Unit),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.ScheduledWorkOrder.F.WorkOrderTemplateID.F.Code),
						OnlyViewEdit),
					// Remaining record types are readonly because the edit tbls do not have any ETbl.
					// SchedulingTerminated
					new CompositeView(PMGenerationDetailSchedulingTerminatedPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.SchedulingTerminated))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// Deferred
					new CompositeView(PMGenerationDetailDeferredPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Deferred))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// Predicted Work Order
					new CompositeView(PMGenerationDetailPredictedWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.PredictedWorkOrder))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// Error
					new CompositeView(PMGenerationDetailErrorPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Error))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// ErrorMakingWorkOrder
					new CompositeView(PMGenerationDetailErrorMakingWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.ErrorMakingWorkOrder))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// MakeWorkOrder committed
					new CompositeView(PMGenerationDetailCommittedMakeWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeWorkOrder))),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID.F.SessionID).IsNull()),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.PathToReferencedRow),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// MakeWorkOrder uncommitted
					new CompositeView(PMGenerationDetailUncommittedMakeWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeWorkOrder))),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID.F.SessionID).IsNotNull()),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// MakeUnscheduledWorkOrder
					new CompositeView(PMGenerationDetailMakeUnscheduledWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeUnscheduledWorkOrder))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.PathToReferencedRow),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// Inhibited
					new CompositeView(PMGenerationDetailInhibitedPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Inhibited))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// ManualReschedule
					new CompositeView(PMGenerationDetailManualReschedulePanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.ManualReschedule))),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat)),
					// MakeSharedWorkOrder committed
					new CompositeView(PMGenerationDetailCommittedMakeWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeSharedWorkOrder))),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID.F.SessionID).IsNull()),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.PathToReferencedRow),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat),
						CompositeView.IdentificationOverride(TId.GenerationDetailCommittedMakeSharedWorkOrder)),
					// MakeSharedWorkOrder uncommitted
					new CompositeView(PMGenerationDetailUncommittedMakeWorkOrderPanelTbl, dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeSharedWorkOrder))),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.PMGenerationBatchID.F.SessionID).IsNotNull()),
						CompositeView.SetParentPath(dsMB.Path.T.PMGenerationDetailAndScheduledWorkOrderAndLocation.F.PMGenerationDetailID.F.ScheduledWorkOrderID),
						CompositeView.ParentType((int)PMGenerationDetailAndScheduledWorkOrderAndLocation.ScheduledWorkOrder),
						BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PMGenerationDetail.F.Sequence, SequenceFormat),
						CompositeView.IdentificationOverride(TId.GenerationDetailUncommittedMakeSharedWorkOrder))
				);
			});
			#endregion
			#region CommittedPMGenerationDetailAndPMGenerationBatch
			DefineBrowseTbl(dsMB.Schema.T.CommittedPMGenerationDetailAndPMGenerationBatch, new DelayedCreateTbl(
				delegate () {
					object generationOrWorkStartDateId = KB.I("GenerationOrWorkStartDateId");
					return new CompositeTbl(dsMB.Schema.T.CommittedPMGenerationDetailAndPMGenerationBatch, TId.GenerationDetailWithContainers,
						new Tbl.IAttr[] {
							new BTbl(
								BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Generation/Work Start Date"), generationOrWorkStartDateId),
								BTbl.ListColumn(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.Sequence),	// note that because this field is numeric it sorts properly without the "Seq xxxxx" formatting.
								BTbl.SetTreeStructure(null, 2, 2, dsMB.Schema.T.CommittedPMGenerationDetailAndBatches, treatAllRecordsAsPrimary: true)
							)
						},
						new CompositeView(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationBatchID,    // PMGenerationBatch
							CompositeView.RecognizeByValidEditLinkage(),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationBatch.F.EntryDate),
							CompositeView.EditorAccess(false, EdtMode.New)),
						new CompositeView(PMGenerationDetailSchedulingTerminatedPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,   // SchedulingTerminated
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.SchedulingTerminated))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailDeferredPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,   // Deferred
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Deferred))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailPredictedWorkOrderPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID, // PredictedWorkOrder
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.PredictedWorkOrder))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailErrorPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,  // Error
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Error))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailErrorMakingWorkOrderPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,   // ErrorMakingWorkOrder
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.ErrorMakingWorkOrder))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailCommittedMakeWorkOrderPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID, // MakeWorkOrder
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeWorkOrder))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.PathToReferencedRow),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailMakeUnscheduledWorkOrderPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,   // MakeUnscheduledWorkOrder
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeUnscheduledWorkOrder))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.PathToReferencedRow),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailInhibitedPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,  // Inhibited
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.Inhibited))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailManualReschedulePanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID,   // ManualReschedule
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.ManualReschedule))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate)),
						new CompositeView(PMGenerationDetailCommittedMakeWorkOrderPanelTbl, dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID, // MakeSharedWorkOrder
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.DetailType).Eq(SqlExpression.Constant((int)PMType.MakeSharedWorkOrder))),
							CompositeView.SetParentPath(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.F.PMGenerationBatchID),
							CompositeView.ParentType((int)CommittedPMGenerationDetailAndPMGenerationBatch.PMGenerationBatch),
							ViewPMGenerationDetailWorkOrderVerb(dsMB.Path.T.CommittedPMGenerationDetailAndPMGenerationBatch.F.PMGenerationDetailID.PathToReferencedRow),
							BTbl.PerViewColumnValue(generationOrWorkStartDateId, dsMB.Path.T.PMGenerationDetail.F.WorkStartDate),
							CompositeView.IdentificationOverride(TId.GenerationDetailCommittedMakeSharedWorkOrder))
					);
				}
			));
			#endregion
		}
		private TISchedule() {
		}
	}
}
