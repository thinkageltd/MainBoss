using System;
using System.Collections.Generic;
using System.Threading;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	public class TemplateFromWorkOrderEditLogic : EditLogic {
		#region Class for checkpointing all the records we create
		private class ChildRowCheckpointer : ICheckpointedObject {
			private class CheckpointData : ICheckpointData {
				public CheckpointData(ChildRowCheckpointer owner) {
					Checkpoints = new ICheckpointData[owner.Checkpointers.Length];
					for (int i = owner.Checkpointers.Length; --i >= 0; )
						Checkpoints[i] = owner.Checkpointers[i].Checkpoint();
				}
				public readonly ICheckpointData[] Checkpoints;
			}
			public ChildRowCheckpointer(dsMB dataSet) {
				dataSet.EnsureDataTableExists(dsMB.Schema.T.TemplateTemporaryStorage);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.Location);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.TemplateItemLocation);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.ItemLocation);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandTemplate);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandItemTemplate);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandLaborInsideTemplate);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandLaborOutsideTemplate);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandOtherWorkInsideTemplate);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandOtherWorkOutsideTemplate);
				dataSet.EnsureDataTableExists(dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate);
				Checkpointers = new[] {
					new DataTableCheckpointer(dataSet.T.TemplateTemporaryStorage),
					new DataTableCheckpointer(dataSet.T.Location),
					new DataTableCheckpointer(dataSet.T.TemplateItemLocation),
					new DataTableCheckpointer(dataSet.T.ItemLocation),
					new DataTableCheckpointer(dataSet.T.DemandTemplate),
					new DataTableCheckpointer(dataSet.T.DemandItemTemplate),
					new DataTableCheckpointer(dataSet.T.DemandLaborInsideTemplate),
					new DataTableCheckpointer(dataSet.T.DemandLaborOutsideTemplate),
					new DataTableCheckpointer(dataSet.T.DemandOtherWorkInsideTemplate),
					new DataTableCheckpointer(dataSet.T.DemandOtherWorkOutsideTemplate),
					new DataTableCheckpointer(dataSet.T.DemandMiscellaneousWorkOrderCostTemplate),
				};
			}
			private readonly DataTableCheckpointer[] Checkpointers;
			public ICheckpointData Checkpoint() {
				return new CheckpointData(this);
			}
			public void Rollback(ICheckpointData data) {
				for (int i = Checkpointers.Length; --i >= 0; )
					Checkpointers[i].Rollback(((CheckpointData)data).Checkpoints[i]);
			}
		}
		#endregion
		public TemplateFromWorkOrderEditLogic(IEditUI control, DBClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(control, db, tbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, initLists) {
		}
		public override void EndSetup() {
			WorkOrderPickerSource = GetControlNotifyingSource(TIWorkOrder.SourceWorkOrderPickerId);
			Checkpointer = new ChildRowCheckpointer((dsMB)DataSet);
			base.EndSetup();
		}
		private Source WorkOrderPickerSource;
		private ICheckpointedObject Checkpointer;
		protected override object[] SaveRecord(ServerExtensions.UpdateOptions updateOptions) {
			ICheckpointData checkpoint = Checkpointer.Checkpoint();
			try {
				if (State.EditRecordState == EditRecordStates.New) {
					// Make child records based on the WO's child records.
					Guid workOrderID = (Guid)WorkOrderPickerSource.GetValue();
					Guid workOrderTemplateID = (Guid)RecordManager.GetCurrentRecordIDs()[0];

					// Query all the temporary storage and convert it to template temporary storage; preserve a mapping from TS ID to TTS ID
					var TempStorageMap = new Dictionary<Guid, Guid>();
					DB.ViewAdditionalRows(DataSet, dsMB.Schema.T.TemporaryStorage, new SqlExpression(dsMB.Path.T.TemporaryStorage.F.WorkOrderID).Eq(workOrderID), null, new[] { dsMB.Path.T.TemporaryStorage.F.LocationID.PathToReferencedRow });
					foreach (dsMB.TemporaryStorageRow temporaryStorageRow in ((dsMB)DataSet).T.TemporaryStorage) {
						if (temporaryStorageRow.F.WorkOrderID != workOrderID)
							continue;
						var templateTemporaryStorageRow = (dsMB.TemplateTemporaryStorageRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.TemplateTemporaryStorage);
						TempStorageMap.Add(temporaryStorageRow.F.LocationID, templateTemporaryStorageRow.F.LocationID);
						templateTemporaryStorageRow.F.ContainingLocationID = temporaryStorageRow.F.ContainingLocationID;// TODO: If we ever support TemplateTemporaryStorage with null ContainingLocationID to indicate the unit of the generated WO, put in a null if the original was the WO's UnitLocationID.
						templateTemporaryStorageRow.F.WorkOrderTemplateID = workOrderTemplateID;
						var templateTemporaryStorageLocationRow = templateTemporaryStorageRow.LocationIDParentRow;
						var temporaryStorageLocationRow = temporaryStorageRow.LocationIDParentRow;
						templateTemporaryStorageLocationRow.F.Comment = temporaryStorageLocationRow.F.Comment;
						templateTemporaryStorageLocationRow.F.Desc = temporaryStorageLocationRow.F.Desc;
						templateTemporaryStorageLocationRow.F.GISLocation = temporaryStorageLocationRow.F.GISLocation;
						templateTemporaryStorageLocationRow.F.GISZoom = temporaryStorageLocationRow.F.GISZoom;
					}
					// Query all the temp storage assignments and convert them to template temp storage assignments; preserve a maping from TSA ID to TTSA ID
					var TempStorageAssignmentMap = new Dictionary<Guid, Guid>();
					DB.ViewAdditionalRows(DataSet, dsMB.Schema.T.TemporaryItemLocation, new SqlExpression(dsMB.Path.T.TemporaryItemLocation.F.WorkOrderID).Eq(workOrderID), null,
						new DBI_PathToRow[] {
						dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.PathToReferencedRow,
						dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.PathToReferencedRow
					}
					);
					foreach (dsMB.TemporaryItemLocationRow temporaryItemLocationRow in ((dsMB)DataSet).T.TemporaryItemLocation) {
						if (temporaryItemLocationRow.F.WorkOrderID != workOrderID)
							continue;
						var templateItemLocationRow = (dsMB.TemplateItemLocationRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.TemplateItemLocation);
						TempStorageAssignmentMap.Add(temporaryItemLocationRow.ActualItemLocationIDParentRow.F.ItemLocationID, templateItemLocationRow.F.ItemLocationID);
						var templateItemLocationItemLocationRow = templateItemLocationRow.ItemLocationIDParentRow;
						var temporaryItemLocationItemLocationRow = temporaryItemLocationRow.ActualItemLocationIDParentRow.ItemLocationIDParentRow;
						templateItemLocationItemLocationRow.F.ItemID = temporaryItemLocationItemLocationRow.F.ItemID;
						templateItemLocationItemLocationRow.F.ItemPriceID = temporaryItemLocationItemLocationRow.F.ItemPriceID;
						templateItemLocationItemLocationRow.F.LocationID = TempStorageMap[temporaryItemLocationItemLocationRow.F.LocationID];
					}
					// Query all the Demands and derivations and convert them to template Demands
					DB.ViewAdditionalRows(DataSet, dsMB.Schema.T.Demand, new SqlExpression(dsMB.Path.T.Demand.F.WorkOrderID).Eq(workOrderID), null,
						new DBI_PathToRow[] {
						dsMB.Path.T.Demand.F.DemandItemID.PathToReferencedRow,
						dsMB.Path.T.Demand.F.DemandItemID.F.ItemLocationID.PathToReferencedRow,							// so we can determine the type of ItemLocation
						dsMB.Path.T.Demand.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.PathToReferencedRow,	// so we can determine the type of ItemLocation
						dsMB.Path.T.Demand.F.DemandLaborInsideID.PathToReferencedRow,
						dsMB.Path.T.Demand.F.DemandLaborOutsideID.PathToReferencedRow,
						dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.PathToReferencedRow,
						dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.PathToReferencedRow,
						dsMB.Path.T.Demand.F.DemandMiscellaneousWorkOrderCostID.PathToReferencedRow
					}
					);
					foreach (dsMB.DemandRow demandRow in ((dsMB)DataSet).T.Demand) {
						if (demandRow.F.WorkOrderID != workOrderID)
							continue;
						dsMB.DemandTemplateRow demandTemplateRow = null;
						if (demandRow.F.DemandItemID != null) {
							var derivedDemandRow = demandRow.DemandItemIDParentRow;
							var derivedTemplateDemandRow = (dsMB.DemandItemTemplateRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.DemandItemTemplate);
							derivedTemplateDemandRow.F.ItemLocationID = derivedDemandRow.ItemLocationIDParentRow.ActualItemLocationIDParentRow.F.TemporaryItemLocationID == null ? derivedDemandRow.F.ItemLocationID : TempStorageAssignmentMap[derivedDemandRow.F.ItemLocationID];
							derivedTemplateDemandRow.F.Quantity = derivedDemandRow.F.Quantity;
							demandTemplateRow = derivedTemplateDemandRow.DemandTemplateIDParentRow;
						}
						else if (demandRow.F.DemandLaborInsideID != null) {
							var derivedDemandRow = demandRow.DemandLaborInsideIDParentRow;
							var derivedTemplateDemandRow = (dsMB.DemandLaborInsideTemplateRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.DemandLaborInsideTemplate);
							derivedTemplateDemandRow.F.LaborInsideID = derivedDemandRow.F.LaborInsideID;
							derivedTemplateDemandRow.F.Quantity = derivedDemandRow.F.Quantity;
							demandTemplateRow = derivedTemplateDemandRow.DemandTemplateIDParentRow;
						}
						else if (demandRow.F.DemandLaborOutsideID != null) {
							var derivedDemandRow = demandRow.DemandLaborOutsideIDParentRow;
							var derivedTemplateDemandRow = (dsMB.DemandLaborOutsideTemplateRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.DemandLaborOutsideTemplate);
							derivedTemplateDemandRow.F.LaborOutsideID = derivedDemandRow.F.LaborOutsideID;
							derivedTemplateDemandRow.F.Quantity = derivedDemandRow.F.Quantity;
							demandTemplateRow = derivedTemplateDemandRow.DemandTemplateIDParentRow;
						}
						else if (demandRow.F.DemandOtherWorkInsideID != null) {
							var derivedDemandRow = demandRow.DemandOtherWorkInsideIDParentRow;
							var derivedTemplateDemandRow = (dsMB.DemandOtherWorkInsideTemplateRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.DemandOtherWorkInsideTemplate);
							derivedTemplateDemandRow.F.OtherWorkInsideID = derivedDemandRow.F.OtherWorkInsideID;
							derivedTemplateDemandRow.F.Quantity = derivedDemandRow.F.Quantity;
							demandTemplateRow = derivedTemplateDemandRow.DemandTemplateIDParentRow;
						}
						else if (demandRow.F.DemandOtherWorkOutsideID != null) {
							var derivedDemandRow = demandRow.DemandOtherWorkOutsideIDParentRow;
							var derivedTemplateDemandRow = (dsMB.DemandOtherWorkOutsideTemplateRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.DemandOtherWorkOutsideTemplate);
							derivedTemplateDemandRow.F.OtherWorkOutsideID = derivedDemandRow.F.OtherWorkOutsideID;
							derivedTemplateDemandRow.F.Quantity = derivedDemandRow.F.Quantity;
							demandTemplateRow = derivedTemplateDemandRow.DemandTemplateIDParentRow;
						}
						else if (demandRow.F.DemandMiscellaneousWorkOrderCostID != null) {
							var derivedDemandRow = demandRow.DemandMiscellaneousWorkOrderCostIDParentRow;
							var derivedTemplateDemandRow = (dsMB.DemandMiscellaneousWorkOrderCostTemplateRow)DB.AddNewRowAndBases(DataSet, dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate);
							derivedTemplateDemandRow.F.MiscellaneousWorkOrderCostID = derivedDemandRow.F.MiscellaneousWorkOrderCostID;
							demandTemplateRow = derivedTemplateDemandRow.DemandTemplateIDParentRow;
						}
						demandTemplateRow.F.EstimateCost = demandRow.F.CostEstimate.HasValue;
						demandTemplateRow.F.DemandActualCalculationInitValue = demandRow.F.DemandActualCalculationInitValue;
						demandTemplateRow.F.WorkOrderExpenseCategoryID = demandRow.F.WorkOrderExpenseCategoryID;
						demandTemplateRow.F.WorkOrderTemplateID = workOrderTemplateID;
					}
				}

				return base.SaveRecord(updateOptions);
			}
			catch {
				Checkpointer.Rollback(checkpoint);
				throw;
			}
		}
	}
}
