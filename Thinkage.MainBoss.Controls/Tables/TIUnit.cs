using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Units.
	/// </summary>
	public class TIUnit : TIGeneralMB3 {
		#region View Record Types
		#region - WorkOrderMeterTreeView
		public enum WorkOrderMeterTreeView {
			Meter,
			WorkOrderReading
		}
		#endregion
		#endregion
		#region NodeIds
		public static readonly object PredictedDateFromReadingId = KB.I("PredictedDateFromReadingId");
		public static readonly object ReadingToPredictFromId = KB.I("ReadingToPredictFromId");
		public static readonly object PredictedReadingFromDateId = KB.I("PredictedReadingFromDateId");
		public static readonly object DateToPredictFromId = KB.I("DateToPredictFromId");
		public static readonly object DurationToPredictOverId = KB.I("DurationToPredictOverId");
		public static readonly object WorkOrderIDValueId = KB.I("WorkOrderIDValueId");
		public static readonly object MeterReadingOffsetId = KB.I("MeterReadingOffsetId");
		public static readonly object MeterEffectiveReadingId = KB.I("MeterEffectiveReadingId");
		public static readonly object MeterEffectiveDateId = KB.I("MeterEffectiveDateId");
		public static readonly object MeterCurrentEffectiveReadingId = KB.I("MeterCurrentEffectiveReadingId");
		public static readonly object MeterCurrentEffectiveDateId = KB.I("MeterCurrentEffectiveDateId");
		public static readonly object MeterReadingId = KB.I("MeterReadingId");
		public static readonly object WorkOrderUnitLocationValueId = KB.I("WorkOrderUnitLocationValueId");
		#endregion
		#region Named Tbls
		public static DelayedCreateTbl WorkOrderMeterReadingBrowseTbl;
		public static DelayedCreateTbl MeterWithManualReadingBrowseTbl;
		public static DelayedCreateTbl WorkOrderServiceContractsBrowseTbl;
		#endregion
		#region Tbl-creator functions

		#endregion

		#region Constructors and Property Initializers
		private TIUnit() {
		}
		static TIUnit() {
		}
		#endregion

		internal static void DefineTblEntries() {
			#region AssetCode
			DefineTbl(dsMB.Schema.T.AssetCode, delegate () {
				return new Tbl(dsMB.Schema.T.AssetCode, TId.AssetCode,
				new Tbl.IAttr[] {
					UnitValueAndServiceGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.AssetCode.F.Code), BTbl.ListColumn(dsMB.Path.T.AssetCode.F.Desc),
						BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.AssetCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AssetCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AssetCode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.AssetCode,
						TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.AssetCodeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.AssetCode, dsMB.Schema.T.AssetCode);
			#endregion
			#region MeterReading
			// This Tbl only allows New and Edit modes. In New mode, the offset comes from the Meter selection, and is used to update the actual/effective
			// reading when the other is changed. In Edit mode, everything is readonly, and the offset is set from the difference between the actual and
			// effective readings (since the meter's own offset may have changed since the reading record was first saved).
			TblLayoutNodeArray MeterReadingNodesGeneral = new TblLayoutNodeArray(
				TblGroupNode.New(dsMB.Path.T.MeterReading.F.MeterID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.MeterClassID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.UnitLocationID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.MeterClassID.F.UnitOfMeasureID.F.Code, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset, DCol.Normal, new NonDefaultCol()),
					TblUnboundControlNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset.Key(), dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset.ReferencedColumn.EffectiveType, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(MeterReadingOffsetId)), new NonDefaultCol()),
					// TODO: The Latest-reading information should be done by a function similar to current state history and/or contact groupings.
					// TODO: These are sort of silly to show in the browser, as they beat little connection to the reading being viewed.
					TblMultiColumnNode.New(
						new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						new[] { dsMB.Path.T.MeterReading.F.EntryDate.Key(), dsMB.Path.T.MeterReading.F.EffectiveDate.Key(), dsMB.Path.T.MeterReading.F.Reading.Key(), dsMB.Path.T.MeterReading.F.EffectiveReading.Key(), dsMB.Path.T.MeterReading.F.Comment.Key() },
						TblRowNode.New(KB.K("Current"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.CurrentMeterReadingID.F.EntryDate, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.CurrentMeterReadingID.F.EffectiveDate, new NonDefaultCol(), DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(MeterCurrentEffectiveDateId))),
							TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.CurrentMeterReadingID.F.Reading, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.CurrentMeterReadingID.F.EffectiveReading, new NonDefaultCol(), DCol.Normal, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(MeterCurrentEffectiveReadingId))),
							TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.CurrentMeterReadingID.F.Comment, Fmt.SetLineCount(2), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)
						)
					)
				),
				// TODO: Perhaps these should just be a second row in the multi-column layout for "Current". The "Meter" group box would have to be eliminated, or the current reading
				// information would have to follow it.
				TblColumnNode.New(dsMB.Path.T.MeterReading.F.EntryDate, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
				TblColumnNode.New(dsMB.Path.T.MeterReading.F.EffectiveDate, new NonDefaultCol(), DCol.Normal, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(MeterEffectiveDateId))),
				TblColumnNode.New(dsMB.Path.T.MeterReading.F.UserID.F.ContactID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
				TblColumnNode.New(dsMB.Path.T.MeterReading.F.Reading, DCol.Normal, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(MeterReadingId))),
				TblColumnNode.New(dsMB.Path.T.MeterReading.F.EffectiveReading, DCol.Normal),
				TblUnboundControlNode.New(dsMB.Path.T.MeterReading.F.EffectiveReading.Key(), dsMB.Path.T.MeterReading.F.EffectiveReading.ReferencedColumn.EffectiveType, new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetId(MeterEffectiveReadingId))),
				TblColumnNode.New(dsMB.Path.T.MeterReading.F.Comment, Fmt.SetLineCount(2), DCol.Normal, ECol.Normal)
			);
			// The following Check validates the new effective reading and current effective reading are in the same order as their respective effective dates.
			// TODO: The 2.9 code actually fetched the readings that bracket the EffectiveDate in the form and checked the Effective Reading against those of the two fetched records.
			// What we do here is a diluted version of the same check. A trigger in the db checks this properly so this is not a high-priority item.
			var EffectiveReadingChecker = new Check4<DateTime, DateTime?, ulong, ulong?>(
					delegate (DateTime newEffectiveDate, DateTime? currentEffectiveDate, ulong newEffectiveReading, ulong? currentEffectiveReading) {
						if (currentEffectiveDate.HasValue && newEffectiveDate == currentEffectiveDate.Value)
							// This just flags the Effective Date control (operand 1 -> index 0)
							return new EditLogic.ValidatorAndCorrector.ValidatorStatus(0, new GeneralException(KB.K("Effective Date must be different from Current Effective Date")));
						if (currentEffectiveDate.HasValue && currentEffectiveReading.HasValue) {
							if (newEffectiveDate > currentEffectiveDate.Value && newEffectiveReading < currentEffectiveReading.Value)
								return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Effective Reading must be greater than Current Effective Reading of {0}"), currentEffectiveReading.Value));
							if (newEffectiveDate < currentEffectiveDate.Value && newEffectiveReading > currentEffectiveReading.Value)
								return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Effective Reading that predates Current Reading must be less than Current Effective Reading of {0}"), currentEffectiveReading.Value));
						}
						return null;
					},
					TblActionNode.Activity.Disabled,
					TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Clone)
				)
				.Operand1(MeterEffectiveDateId)
				.Operand2(MeterCurrentEffectiveDateId)
				.Operand3(MeterEffectiveReadingId)
				.Operand4(MeterCurrentEffectiveReadingId);
			// The following Check maintains the relationship EffectiveReading = Reading+Offset.
			// In New mode, an init fills the offset from the Meter for calculation on-screen, and a trigger actually fills in the EffectiveReading when the record
			//		is saved. The Effective and Actual readings correct each other as either is changed.
			// In UnDelete mode, an init fills in the EffectiveReading, this calculator generates the Offset and all the controls are readonly.
			Check3<ulong, ulong, ulong> MeterReadingChecker = new Check3<ulong, ulong, ulong>(
					delegate (ulong actual, ulong effective, ulong offset) {
						if (checked(actual + offset != effective))
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Actual and Effective readings must differ by Meter offset of {0}"), offset));
						return null;
					})
					.Operand1(MeterReadingId,
						delegate (ulong effective, ulong offset) {
							if (effective < offset)
								throw new GeneralException(KB.K("Effective reading must not be less than offset of {0}"), offset);
							return checked(effective - offset);
						}, EdtMode.New
					)
					.Operand2(MeterEffectiveReadingId,
						delegate (ulong actual, ulong offset) {
							return checked(actual + offset);
						}, EdtMode.New
					)
					.Operand3(MeterReadingOffsetId,
						delegate (ulong actual, ulong effective) {
							return checked(effective - actual);
						}, EdtMode.View, EdtMode.ViewDeleted, EdtMode.UnDelete
					);

			DelayedCreateTbl WorkOrderMeterReadingEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.MeterReading, TId.WorkOrderMeterReading,
					new Tbl.IAttr[] {
						MetersDependentGroup,
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.View, EdtMode.Delete, EdtMode.UnDelete, EdtMode.ViewDeleted))
					},
					TblFixedRecordTypeNode.New()
					+ (TblColumnNode.New(dsMB.Path.T.MeterReading.F.WorkOrderID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrder.F.Number)), new ECol(ECol.AllReadonlyAccess, ECol.ForceValue()), new NonDefaultCol())
					  + MeterReadingNodesGeneral),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.MeterReading.F.UserID), new UserIDValue()),
					Init.New(new ControlTarget(MeterReadingOffsetId),
									new EditorPathValue(dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset),
									null,
									TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New)),
					Init.New(new ControlTarget(MeterEffectiveReadingId),
									new EditorPathValue(dsMB.Path.T.MeterReading.F.EffectiveReading),
									null,
									TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.View, EdtMode.ViewDeleted, EdtMode.UnDelete)),
					MeterReadingChecker,
					EffectiveReadingChecker
				);
			});
			DelayedCreateTbl ManualMeterReadingEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.MeterReading, TId.ManualMeterReading,
					new Tbl.IAttr[] {
						MetersDependentGroup,
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.View, EdtMode.Delete, EdtMode.UnDelete, EdtMode.ViewDeleted))
					},
					TblFixedRecordTypeNode.New() + MeterReadingNodesGeneral,
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.MeterReading.F.UserID), new UserIDValue()),
					Init.New(new ControlTarget(MeterReadingOffsetId),
								new EditorPathValue(dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset),
								null,
								TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New)),
					Init.New(new ControlTarget(MeterEffectiveReadingId),
									new EditorPathValue(dsMB.Path.T.MeterReading.F.EffectiveReading),
									null,
									TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.View, EdtMode.ViewDeleted, EdtMode.UnDelete)),
					MeterReadingChecker,
					EffectiveReadingChecker
				);
			});
			// Importing of meter reading needs modified Tbl to provide a picker to identify the meter
			RegisterForImportExport(TId.MeterReading, new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.MeterReading, TId.ManualMeterReading,
					new Tbl.IAttr[] {
						MetersDependentGroup,
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.View, EdtMode.Delete, EdtMode.UnDelete, EdtMode.ViewDeleted))
					},
					TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID, new NonDefaultCol(), ECol.Normal) + MeterReadingNodesGeneral
				);
			}));

			#region Meter Reading Predictor (testing)
#if DEBUG
			DelayedCreateTbl MeterReadingPredictorTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.MeterReading, TId.MeterReadingPrediction,
					new Tbl.IAttr[] {
						MetersDependentGroup,
						// We allow UnDelete just to avoid an overly-eager assertion failure because the table we happen to be using is hideable.
						// We really don't want a table at all, but EC really would like one, and we use it to make the Unit and UoM displays work,
						// although we don't really have to have them around.
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.New, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.MeterClassID.F.Code, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.UnitLocationID.F.Code, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.MeterReading.F.MeterID.F.MeterClassID.F.UnitOfMeasureID.F.Code, ECol.AllReadonly),
						TblUnboundControlNode.New(KB.K("Reading to Predict From"), dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset.ReferencedColumn.EffectiveType, new ECol(Fmt.SetId(ReadingToPredictFromId))),
						TblUnboundControlNode.New(KB.K("Predicted Date"), DateTimeTypeInfo.Universe, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PredictedDateFromReadingId))),
						TblUnboundControlNode.New(KB.K("Date to Predict From"), DateTimeTypeInfo.Universe, new ECol(Fmt.SetId(DateToPredictFromId))),
						TblUnboundControlNode.New(KB.K("Duration to Predict within"), IntervalTypeInfo.Universe, new ECol(Fmt.SetId(DurationToPredictOverId))),
						TblUnboundControlNode.New(KB.K("Predicted Reading"), dsMB.Path.T.MeterReading.F.MeterID.F.MeterReadingOffset.ReferencedColumn.EffectiveType, new ECol(ECol.AllReadonlyAccess, Fmt.SetId(PredictedReadingFromDateId)))
					),
					new Check3<Guid, long, DateTime>()
						.Operand1(KB.TOi(TId.Meter), new EditorPathValue(dsMB.Path.T.MeterReading.F.MeterID))
						.Operand2(ReadingToPredictFromId)
						.Operand3(PredictedDateFromReadingId,
							delegate (Guid meterID, long reading) {
								PMGeneration.FuzzyDate when = new MeterReadingAnalysis.PredictDateFromReadingRange((MB3Client)Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session)
																.Predict(meterID, new PMGeneration.FuzzyReading(reading));
								return when.ExpectedValue;
							}
						),
					new Check4<Guid, DateTime, TimeSpan, long>()
						.Operand1(KB.TOi(TId.Meter), new EditorPathValue(dsMB.Path.T.MeterReading.F.MeterID))
						.Operand2(DateToPredictFromId)
						.Operand3(DurationToPredictOverId)
						.Operand4(PredictedReadingFromDateId,
							delegate (Guid meterID, DateTime when, TimeSpan interval) {
								PMGeneration.FuzzyReading reading = new MeterReadingAnalysis.PredictReadingFromDateRange((MB3Client)Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session)
									.Predict(meterID, new PMGeneration.FuzzyDate(when));
								return reading.ExpectedValue;
							}
						)

				);
			});
#endif
			#endregion
			DelayedCreateTbl ManualMeterReadingBrowseTbl = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.MeterReading, TId.ManualMeterReading,
					new Tbl.IAttr[] {
						new BTbl(BTbl.ListColumn(dsMB.Path.T.MeterReading.F.MeterID.F.MeterClassID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.MeterReading.F.MeterID.F.UnitLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.MeterReading.F.EffectiveDate, BTbl.Contexts.SortInitialAscending),
								BTbl.ListColumn(dsMB.Path.T.MeterReading.F.Reading),
								BTbl.ListColumn(dsMB.Path.T.MeterReading.F.EffectiveReading)
						)
					},
					CompositeView.ChangeEditTbl(ManualMeterReadingEditTbl,
						//CompositeView.IdentificationOverride(TId.MeterReading),
						CompositeView.AddRecognitionCondition(new Libraries.XAF.Database.Layout.SqlExpression(dsMB.Path.T.MeterReading.F.WorkOrderID).IsNull()),
						CompositeView.ExportNewVerb(true)),
					CompositeView.ChangeEditTbl(WorkOrderMeterReadingEditTbl,
						CompositeView.AddRecognitionCondition(new Libraries.XAF.Database.Layout.SqlExpression(dsMB.Path.T.MeterReading.F.WorkOrderID).IsNotNull()),
						NoNewMode
						//CompositeView.IdentificationOverride(TId.MeterReading),
						)
				);
			});

			#endregion
			#region Meter
			TblLayoutNodeArray MeterNodesBasic = new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsMB.Path.T.Meter.F.MeterClassID, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.MeterClass.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Meter.F.UnitLocationID, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Meter.F.MeterClassID.F.UnitOfMeasureID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.Meter.F.MeterReadingOffset, DCol.Normal, ECol.Normal),
					// TODO: The Latest-reading information should be done by a function similar to current state history and/or contact groupings.
					TblMultiColumnNode.New(
						new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						new[] { dsMB.Path.T.MeterReading.F.EntryDate.Key(), dsMB.Path.T.MeterReading.F.EffectiveDate.Key(), dsMB.Path.T.MeterReading.F.Reading.Key(), dsMB.Path.T.MeterReading.F.EffectiveReading.Key(), dsMB.Path.T.MeterReading.F.Comment.Key() },
						TblRowNode.New(KB.K("Current"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.EntryDate, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.EffectiveDate, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.Reading, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.EffectiveReading, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.Comment, Fmt.SetLineCount(1), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)
						)
					),
					TblColumnNode.New(dsMB.Path.T.Meter.F.Comment, DCol.Normal, ECol.Normal))
			);

			TblLayoutNodeArray MeterNodesWithManualReading = MeterNodesBasic + new TblLayoutNodeArray(
				BrowsetteTabNode.New(TId.MeterReading, TId.Meter,
					TblColumnNode.NewBrowsette(ManualMeterReadingBrowseTbl, dsMB.Path.T.MeterReading.F.MeterID, DCol.Normal, ECol.Normal)
				)
			);

			DelayedCreateTbl MeterWithManualReadingEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Meter, TId.Meter,
					new Tbl.IAttr[] {
						MetersDependentGroup,
						new ETbl()
					 },
					(TblLayoutNodeArray)MeterNodesWithManualReading.Clone(),
					Init.LinkRecordSets(dsMB.Path.T.MeterReading.F.MeterID, 1, dsMB.Path.T.Meter.F.Id, 0),
					// TODO: The following should be replaced with special handling in the editor. The Reading should have a control for input
					// (Initial Reading), and the effective reading should use the meter editor's Offset control to calculate itself.
					// MeterReading Effective date is servergetsDateTime to reflect the server's time, not the client time
					//							Init.OnLoadNew(dsMB.Path.T.MeterReading.F.EffectiveDate, 1, new CurrentDateTimeValue()),
					Init.OnLoadNew(dsMB.Path.T.MeterReading.F.UserID, 1, new UserIDValue()),
					Init.OnLoadNew(dsMB.Path.T.MeterReading.F.Reading, 1, new ConstantValue(0))
				);
			});
			// The Meter browser is made Composite only to allow specification of an alternative Tbl for editing.
			// We need the alternative edit tbl because recordset 1 is used to create the first reading in New mode and the browser disallows
			// anything but recordset 0. As well, in DEBUG mode we want a second New verb.
			MeterWithManualReadingBrowseTbl = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.Meter, TId.Meter,
					new Tbl.IAttr[] {
						new BTbl(BTbl.ListColumn(dsMB.Path.T.Meter.F.MeterClassID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.Meter.F.UnitLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.EffectiveDate, BTbl.Contexts.List|BTbl.Contexts.SearchAndFilter),
								BTbl.ListColumn(dsMB.Path.T.Meter.F.CurrentMeterReadingID.F.EffectiveReading, BTbl.Contexts.List|BTbl.Contexts.SearchAndFilter),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.UnitMeters))
						)
					},
					CompositeView.ChangeEditTbl(MeterWithManualReadingEditTbl)
#if DEBUG
,
					CompositeView.ExtraNewVerb(MeterReadingPredictorTbl,
						CompositeView.ContextualInit(0, dsMB.Path.T.Meter.F.Id, dsMB.Path.T.MeterReading.F.MeterID)
					)
#endif
);
			});
			// Need tInfo defined for Default Editor
			DefineBrowseTbl(dsMB.Schema.T.Meter, MeterWithManualReadingBrowseTbl);
			RegisterForImportExport(TId.Meter, MeterWithManualReadingEditTbl);

			// Definition to have the MeterClass identifier in WorkOrderMeterTreeView displayed in the view, but to disallow any editing/creation of a new Meter
			DelayedCreateTbl WorkOrderMeterEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Meter, TId.Meter,
					new Tbl.IAttr[] {
							MetersDependentGroup,
							new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true,EdtMode.UnDelete)),// UnDelete enabled to avoid assertion failure that checks for UnDelete mode against schema providing Hidden Column for deletion.
						 },
					(TblLayoutNodeArray)MeterNodesBasic.Clone()
				);
			}
			);

			// This meter reading browser uses per-view filtering to select both readings associated with this WO and also meters associated with this WO's Unit.
			// Note that although we can include readings on meters from units other than the one associated with the WO, we do not show the heirarchical location for
			// each meter's unit, since the "wrong unit" condition is rare... it required altering the WO's Unit after a WO-linked reading has been taken.
			// However, we must be filtered-tree-structured so at least the meters for these wrong-unit readings are in the list.
			WorkOrderMeterReadingBrowseTbl = new DelayedCreateTbl(delegate () {
				object codeColumnId = KB.I("MeterReadingCodeId");
				return new CompositeTbl(dsMB.Schema.T.MeterAndReadingVariants, TId.WorkOrderMeterReading,
					new Tbl.IAttr[] {
						new BTbl(
							BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Meter Class/Effective Date"), codeColumnId),
							BTbl.ListColumn(dsMB.Path.T.MeterAndReadingVariants.F.MeterReadingID.F.EffectiveReading),
							BTbl.SetTreeStructure(null, 2, 2, dsMB.Schema.T.WorkOrderMeterTreeView)
						)
					},
					// Meter itself
					new CompositeView(WorkOrderMeterEditTbl, dsMB.Path.T.MeterAndReadingVariants.F.MeterID,
						// All rows have a meter id, so instead we recognize it by the *lack* of a meter reading ID.
						CompositeView.AddRecognitionCondition(new Libraries.XAF.Database.Layout.SqlExpression(dsMB.Path.T.MeterAndReadingVariants.F.MeterReadingID).IsNull()),
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.Meter.F.MeterClassID.F.Code),
						CompositeView.EditorDefaultAccess(false),
						BTbl.TaggedEqFilter(dsMB.Path.T.MeterAndReadingVariants.F.MeterID.F.UnitLocationID, WorkOrderUnitLocationValueId)
					),
					new CompositeView(WorkOrderMeterReadingEditTbl, dsMB.Path.T.MeterAndReadingVariants.F.MeterReadingID,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.SetParentPath(dsMB.Path.T.MeterAndReadingVariants.F.MeterID),
						NoNewMode,
						BTbl.PerViewColumnValue(codeColumnId, dsMB.Path.T.MeterReading.F.EffectiveDate, DateTimeFormat),
						CompositeView.ContextualInit(
							new int[] {
								(int)WorkOrderMeterTreeView.WorkOrderReading,
								(int)WorkOrderMeterTreeView.Meter
							},
							dsMB.Path.T.MeterAndReadingVariants.F.MeterID, dsMB.Path.T.MeterReading.F.MeterID),
						BTbl.TaggedEqFilter(dsMB.Path.T.MeterAndReadingVariants.F.MeterReadingID.F.WorkOrderID, WorkOrderIDValueId)
					)
				);
			});


			#endregion
			#region MeterClass
			DefineTbl(dsMB.Schema.T.MeterClass, delegate () {
				return new Tbl(dsMB.Schema.T.MeterClass, TId.MeterClass,
				new Tbl.IAttr[] {
					MetersDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.MeterClass.F.Code), BTbl.ListColumn(dsMB.Path.T.MeterClass.F.Desc),
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.MeterClassReport))),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.MeterClass.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.MeterClass.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.MeterClass.F.UnitOfMeasureID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitOfMeasure.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.MeterClass.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Meter, TId.MeterClass,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Meter.F.MeterClassID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.MaintenanceTiming, TId.MeterClass,
						// TODO: We want to filter on record type here in a manner that prevents all the other New operations (besides new meter schedule)
						TblColumnNode.NewBrowsette(dsMB.Path.T.Periodicity.F.MeterClassID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.MeterClass, dsMB.Schema.T.MeterClass);
			#endregion
			#region Ownership
			DefineTbl(dsMB.Schema.T.Ownership, delegate () {
				return new Tbl(dsMB.Schema.T.Ownership, TId.Ownership,
				new Tbl.IAttr[] {
					UnitValueAndServiceGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.Ownership.F.Code), BTbl.ListColumn(dsMB.Path.T.Ownership.F.Desc),
						BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.Ownership.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Ownership.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Ownership.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.Ownership,
						TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.OwnershipID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.Ownership, dsMB.Schema.T.Ownership);

			#endregion
			#region ServiceContract
			DefineTbl(dsMB.Schema.T.ServiceContract, delegate () {
				return new Tbl(dsMB.Schema.T.ServiceContract, TId.ServiceContract,
				new Tbl.IAttr[] {
					CommonTblAttrs.ViewCostsDefinedBySchema,
					UnitValueAndServiceGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.ServiceContract.F.Code),BTbl.ListColumn(dsMB.Path.T.ServiceContract.F.Desc),
							BTbl.ListColumn(dsMB.Path.T.ServiceContract.F.VendorID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ServiceContract.F.ContractNumber)
					,
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.ServiceContractReport))),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.VendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.VendorID.F.ServiceContactID.F.BusinessPhone, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.ContractNumber, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.StartDate, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.EndDate, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.Parts, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.Labor, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.Cost, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceContract.F.Comment, DCol.Normal, ECol.Normal)
					),
					BrowsetteTabNode.New(TId.Unit, TId.ServiceContract,
						TblColumnNode.NewBrowsette(dsMB.Path.T.UnitServiceContract.F.ServiceContractID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion
			#region Part
			DefineTbl(dsMB.Schema.T.SparePart, delegate () {
				return new Tbl(dsMB.Schema.T.SparePart, TId.Part,
				new Tbl.IAttr[] {
					PartsGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.SparePart.F.UnitLocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.SparePart.F.ItemID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.SparePart.F.Quantity, NonPerViewColumn),
							BTbl.ListColumn(dsMB.Path.T.SparePart.F.ItemID.F.UnitOfMeasureID.F.Code, NonPerViewColumn)
					,
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.UnitParts))),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					TblGroupNode.New(dsMB.Path.T.SparePart.F.UnitLocationID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.SparePart.F.UnitLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.UnitLocationID.F.Desc, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Make, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Model, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Serial, DCol.Normal, ECol.AllReadonly)
					}),
					TblGroupNode.New(dsMB.Path.T.SparePart.F.ItemID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.SparePart.F.ItemID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Item.F.Code)), new ECol(Fmt.SetPickFrom(TIItem.ItemAsSparePartPickerTblCreator))),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.ItemID.F.OnHand, DCol.Normal, ECol.AllReadonly, new FeatureGroupArg(StoreroomGroup)),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.ItemID.F.Desc, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.Quantity, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.ItemID.F.UnitOfMeasureID.F.Code, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.SparePart.F.Comment, DCol.Normal, ECol.Normal)
					})
				));
			});
			RegisterExistingForImportExport(TId.Part, dsMB.Schema.T.SparePart);
			#endregion
			#region SystemCode
			DefineTbl(dsMB.Schema.T.SystemCode, delegate () {
				return new Tbl(dsMB.Schema.T.SystemCode, TId.System,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.SystemCode.F.Code), BTbl.ListColumn(dsMB.Path.T.SystemCode.F.Desc),
						BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.SystemCode.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.SystemCode.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.SystemCode.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.System,
						TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.SystemCodeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.System, dsMB.Schema.T.SystemCode);

			#endregion
			#region Unit
			var UnitEditorTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.Unit, TId.Unit,
					new Tbl.IAttr[] {
							UnitsDependentGroup,
							new ETbl(
								ETbl.UseNewConcurrency(true),
								ETbl.CustomCommand(delegate(EditLogic editorLogic) {
									var ShowOnMap = new EditLogic.CommandDeclaration(KB.K("Show on map"), new ShowOnMapCommand(editorLogic));
									var ShowOnMapGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration{
									ShowOnMap
};
									return ShowOnMapGroup;
								})
							)
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblFixedRecordTypeNode.New(),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.ExternalTag, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.GISLocation, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID,
								new NonDefaultCol(),
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
								new ECol(
									Fmt.SetPickFrom(TILocations.PermanentLocationPickerTblCreator),
									FilterOutContainedLocations(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.Location.F.Id)
								)),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID,
								new DefaultOnlyCol(),
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)),
								new ECol(
									Fmt.SetPickFrom(TILocations.PermanentLocationPickerTblCreator)
								)),
							TblColumnNode.New(dsMB.Path.T.Unit.F.Make, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.Model, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.Serial, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.UnitUsageID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitUsage.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.UnitCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitCategory.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.SystemCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.SystemCode.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.Drawing, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.Comment, DCol.Normal, ECol.Normal)
						),
						TblTabNode.New(KB.K("Service"), KB.K("Display the service properties for this unit"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.Unit.F.AccessCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.AccessCode.F.Code)), ECol.Normal, new FeatureGroupArg(AccessCodeGroup)),
							TblColumnNode.New(dsMB.Path.T.Unit.F.WorkOrderExpenseModelID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderExpenseModel.F.Code)), ECol.Normal, AccountingFeatureArg),
							TblGroupNode.New(KB.TOc(TId.ServiceContract), new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(UnitValueAndServiceGroup), new NonDefaultCol(), DCol.Normal, ECol.Normal },
								TblColumnNode.NewBrowsette(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.UnitServiceContract.F.UnitLocationID, new FeatureGroupArg(UnitValueAndServiceGroup), DCol.Normal, ECol.Normal))
							),
						TblTabNode.New(KB.K("Related"), KB.K("Display the related records for this unit"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.NewBrowsette(TIRelationship.UnitRelatedRecordsBrowseTbl, dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.UnitRelatedRecords.F.ThisUnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Part, TId.Unit,
							TblColumnNode.NewBrowsette(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.SparePart.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Specification, TId.Unit,
							TblColumnNode.NewBrowsette(TIAttachments.UnitSpecificationBrowseTblCreator, dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.UnitAttachment.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						// We disable the Value tab for Requests only mode. The biggest gumball here is Vendor; if we want the Vendor for Requests mode,
						// we might as well also include Service Contracts and their unit association table.
						TblTabNode.New(KB.K("Value"), KB.K("Display the value properties for this unit"), new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(UnitValueAndServiceGroup), DCol.Normal, ECol.Normal, new AddCostCol(CommonNodeAttrs.ViewUnitValueCosts) },
							TblColumnNode.New(dsMB.Path.T.Unit.F.PurchaseDate, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.PurchaseVendorID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.OriginalCost, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.OwnershipID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Ownership.F.Code)), ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Unit.F.AssetCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.AssetCode.F.Code)), ECol.Normal),
							TblGroupNode.New(KB.K("Future Value"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(dsMB.Path.T.Unit.F.ReplacementCostLastDate, DCol.Normal, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Unit.F.ReplacementCost, DCol.Normal, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Unit.F.TypicalLife, DCol.Normal, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Unit.F.ScrapDate, DCol.Normal, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Unit.F.ScrapValue, DCol.Normal, ECol.Normal)
							)
						),
						BrowsetteTabNode.New(TId.Meter, TId.Unit,
							TblColumnNode.NewBrowsette(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.Meter.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Attachment, TId.Unit,
							TblColumnNode.NewBrowsette(TIAttachments.UnitAttachmentBrowseTblCreator, dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.UnitAttachment.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Request, TId.Unit,
							TblColumnNode.NewBrowsette(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.Request.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.UnitMaintenancePlan, TId.Unit,
							TblColumnNode.NewBrowsette(TISchedule.ScheduledWorkOrderBrowserTbl, dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.ScheduledWorkOrder.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.WorkOrder, TId.Unit,
							TblColumnNode.NewBrowsette(TIWorkOrder.WorkOrderBrowsetteFromUnitTblCreator, dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.WorkOrder.F.UnitLocationID, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.TemporaryStorageAndItem, TId.Unit,
							TblColumnNode.NewBrowsette(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, dsMB.Path.T.ActiveTemporaryStorageWithItemAssignments.F.ContainingLocationID, DCol.Normal, ECol.Normal))
					));
			});
			DefineEditTbl(dsMB.Schema.T.Unit, UnitEditorTblCreator);
			DefineBrowseTbl(dsMB.Schema.T.Unit, delegate () {
				return new CompositeTbl(dsMB.Schema.T.Unit, TId.Unit,
					new Tbl.IAttr[] {
							UnitsDependentGroup,
							new BTbl(
								BTbl.ListColumn(dsMB.Path.T.Unit.F.RelativeLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.Desc, NonPerViewColumn),
								BTbl.ListColumn(dsMB.Path.T.Unit.F.Serial)
							)
					},
					CompositeView.ChangeEditTbl(UnitEditorTblCreator)//,
																	 //					CompositeView.AdditionalEditDefault(UnitAttachmentTblCreator)
				);
			}
			);
			RegisterExistingForImportExport(TId.Unit, dsMB.Schema.T.Unit);
			#endregion
			#region UnitCategory
			DefineTbl(dsMB.Schema.T.UnitCategory, delegate () {
				return new Tbl(dsMB.Schema.T.UnitCategory, TId.UnitCategory,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitCategory.F.Code), BTbl.ListColumn(dsMB.Path.T.UnitCategory.F.Desc),
						BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.UnitCategory.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitCategory.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitCategory.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Unit, TId.UnitCategory,
						TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.UnitCategoryID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.UnitCategory, dsMB.Schema.T.UnitCategory);
			#endregion
			#region UnitOfMeasure
			DefineTbl(dsMB.Schema.T.UnitOfMeasure, delegate () {
				return new Tbl(dsMB.Schema.T.UnitOfMeasure, TId.UnitOfMeasure,
				new Tbl.IAttr[] {
					ItemsDependentGroup|MetersDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitOfMeasure.F.Code), BTbl.ListColumn(dsMB.Path.T.UnitOfMeasure.F.Desc),
						BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.UnitOfMeasure.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitOfMeasure.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitOfMeasure.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Item, TId.UnitOfMeasure,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Item.F.UnitOfMeasureID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.MeterClass, TId.UnitOfMeasure,
						TblColumnNode.NewBrowsette(dsMB.Path.T.MeterClass.F.UnitOfMeasureID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.UnitOfMeasure, dsMB.Schema.T.UnitOfMeasure);
			#endregion
			#region UnitServiceContract
			TblLayoutNodeArray unitServiceContractNodes = new TblLayoutNodeArray(
					TblGroupNode.New(dsMB.Path.T.UnitServiceContract.F.UnitLocationID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.UnitLocationID, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.Desc, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Make, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Model, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Serial, DCol.Normal, ECol.AllReadonly)
					}),
					TblGroupNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.ServiceContract.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Desc, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID.F.Code, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID.F.ServiceContactID.F.BusinessPhone, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.ContractNumber, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.StartDate, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.EndDate, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Parts, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Labor, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Cost, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Comment, DCol.Normal, ECol.AllReadonly)
					})
			);
			DelayedCreateTbl UnitServiceContractEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.UnitServiceContract, TId.UnitServiceContract,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						UnitValueAndServiceGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
				},
				(TblLayoutNodeArray)unitServiceContractNodes.Clone()
				);
			});
			WorkOrderServiceContractsBrowseTbl = new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.UnitServiceContract, TId.UnitServiceContract,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						UnitValueAndServiceGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID.F.ServiceContactID.F.BusinessPhone),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.ContractNumber)
						)
					},
					CompositeView.ChangeEditTbl(UnitServiceContractEditTbl,
						CompositeView.EditorAccess(false, EdtMode.UnDelete, EdtMode.New, EdtMode.Edit, EdtMode.Delete))
				);
			});
			DelayedCreateTbl UnitServiceContractBrowseTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.UnitServiceContract, TId.UnitServiceContract,
					new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						UnitValueAndServiceGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID.F.ServiceContactID.F.BusinessPhone),
								BTbl.ListColumn(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.ContractNumber)
						),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					(TblLayoutNodeArray)unitServiceContractNodes.Clone()
				);
			});
			DefineTbl(dsMB.Schema.T.UnitServiceContract, UnitServiceContractBrowseTbl);
			#endregion
			#region UnitUsage
			DefineTbl(dsMB.Schema.T.UnitUsage, delegate () {
				return new Tbl(dsMB.Schema.T.UnitUsage, TId.UnitUsage,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitUsage.F.Code), BTbl.ListColumn(dsMB.Path.T.UnitUsage.F.Desc),
							BTbl.SetCustomClassReportTbl<CodeDescReportTbl>()),
						new ETbl()
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.UnitUsage.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.UnitUsage.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.UnitUsage.F.Comment, DCol.Normal, ECol.Normal)),
						BrowsetteTabNode.New(TId.Unit, TId.UnitUsage,
							TblColumnNode.NewBrowsette(TILocations.UnitBrowseTblCreator, dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.UnitUsageID, DCol.Normal, ECol.Normal))
					)
				);
			});
			RegisterExistingForImportExport(TId.UnitUsage, dsMB.Schema.T.UnitUsage);
			#endregion
		}
	}
}
