using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database {
	public class PhysicalCountImportExport {
		const string macroResource = "PermanentInventoryLocationData.xlsm";
		const string customizationResource = "MainBossCustomizationPhysicalCount.exportedUI";

		private byte[] Buffer = new byte[1024];
		private DataImportExportHelper DataHelper;
		private readonly static string directoryName = "MainBossPhysicalCounts";
		private readonly string macroPath = System.IO.Path.Combine(directoryName, "PermanentInventoryLocationData.xlsm");
		private readonly string ribbonCustomizationsPath = System.IO.Path.Combine(directoryName, "MainBossCustomizationPhysicalCount.exportedUI");
		private readonly string storageAssignmentExportDataFile = System.IO.Path.Combine(directoryName, "PermanentInventoryLocationData.xml");
		private readonly string storageAssignmentExportSchema = System.IO.Path.Combine(directoryName, "PhysicalCountPreparationSchema.xsd");
		private readonly string physicalCountImportSchema = System.IO.Path.Combine(directoryName, "PhysicalCountImportSchema.xsd");

		public PhysicalCountImportExport() {
		}
		public void PreparePhysicalCounts(dsMB mbds) {
			try {
				System.IO.Directory.CreateDirectory("MainBossPhysicalCounts");
			}
			catch (System.IO.IOException x) {
				throw new GeneralException(x, KB.K("Cannot create the MainBossPhysicalCounts directory"));
			}
			SaveManifestResourceInFile(macroPath, macroResource);
			SaveManifestResourceInFile(ribbonCustomizationsPath, customizationResource);
			DataSchemaBuilder sb = new DataSchemaBuilder(dsMB.Schema.T.PermanentItemLocation.Name);
			List<KeyValuePair<DBI_Path, string>> mappings = new List<KeyValuePair<DBI_Path, string>>();
			sb.AddColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.F.Code, mappings);
			sb.AddColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.L.LocationReport.LocationID.F.OrderByRank, mappings);
			sb.AddColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Code, mappings);
			sb.AddColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.F.Desc, mappings);
			sb.AddColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.OnHand, mappings);
			sb.AddColumn(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.TotalCost, mappings);
			DataImportExportHelper dh = new DataImportExportHelper(sb);
			System.IO.File.WriteAllText(storageAssignmentExportSchema, dh.ExcelSchemaText, System.Text.Encoding.Unicode);

			DataImportExportHelper importSchemaHelper = new DataImportExportHelper(PhysicalCountSchema());
			System.IO.File.WriteAllText(physicalCountImportSchema, importSchemaHelper.ExcelSchemaText, System.Text.Encoding.Unicode);
#if NOTIMPLEMENTED
			using (DataSet rds = FetchData(dh.TableSchema, mbds.DB, mappings)) {
				rds.WriteXml(storageAssignmentExportDataFile);
			}
#endif
		}
		private DataSet FetchData(DBI_Table tableSchema, DBClient db, List<KeyValuePair<DBI_Path, string>> mappings) {
			// Note that this dataset is only used by the MS report viewer control and thus never needs to be a custom typed dataset.
			List<SqlExpression> queryColumns = new List<SqlExpression>();
			// Iterate over all columns and select the ServerExpressionQueryColumn objects only. Alternatively we could iterate through the PathColumns.Values and ServerExpressionColumns but then we would have to duplicate the loop guts.
			foreach (KeyValuePair<DBI_Path, string> qCol in mappings) {
				var columnExpression = new SqlExpression(qCol.Key);
				queryColumns.Add(columnExpression); //.Cast(qCol.Key.ReferencedColumn.ReadType).As(qCol.Value));
			}
			DBDataSet xds = DBDataSet.New(tableSchema.Database, db);
			db.ViewAdditionalRows(xds, tableSchema, null, queryColumns.ToArray(), null);
			//			{
			//				dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.PathToReferencedRow,
			//				dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.PathToReferencedRow,
			//				dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID.PathToReferencedRow
			//			});
			return xds;
		}
		private void SaveManifestResourceInFile(string outputFile, string resourceName) {
			try {
				using (System.IO.Stream outputStream = System.IO.File.OpenWrite(outputFile)) {
					using (var resourceStream = typeof(PhysicalCountImportExport).Assembly.GetManifestResourceStream(KB.I("Thinkage.MainBoss.Database.Client.PhysicalCounts.") + KB.I(resourceName))) {
						for (var bytes = 0; (bytes = resourceStream.Read(Buffer, 0, Buffer.Length)) > 0;) {
							outputStream.Write(Buffer, 0, bytes);
						}
					}
				}
			}
			catch (System.Exception x) {
				throw new GeneralException(x, KB.K("Cannot save the resource {0} to the file {1}"), resourceName, outputFile);
			}
		}
#region LoadPhysicalCounts
		private DataSchemaBuilder PhysicalCountSchema() {
			DataSchemaBuilder sb = new DataSchemaBuilder(KB.I("PhysicalCount"));
			sb.AddColumn(dsPhysicalCount.Path.T.PhysicalCount.F.StoreroomAssignmentID);
			sb.AddColumn(dsPhysicalCount.Path.T.PhysicalCount.F.Quantity);
			sb.AddColumn(dsPhysicalCount.Path.T.PhysicalCount.F.Cost);
			return sb;
		}
		public void LoadPhysicalCounts(string physicalCounts, System.Xml.Schema.ValidationEventHandler eventHandler) {
			DataHelper = new DataImportExportHelper(PhysicalCountSchema());
			// Preprocess the input records as a string and add the name space xmlns = "http://thinkage.ca/MainBoss/dsPhysicalCount.xsd" since Excel can't do it.
			System.IO.Stream stream = null;
			try {
				stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(physicalCounts.Replace("<dsPhysicalCount>", "<dsPhysicalCount xmlns=\"http://thinkage.ca/MainBoss/dsPhysicalCount.xsd\">")));
				DataHelper.LoadDataSetFromXml(stream, eventHandler);
			}
			catch (System.Exception ex) {
				throw new GeneralException(ex, KB.K("Unable to read physical counts"));
			}
			finally {
				if (stream != null) {
					stream.Close();
				}
			}
		}
#endregion

#region Physical Count Builder
		private Guid GetItemLocationID(Guid storeroomAssignmentID) {
			if (AILValuesIndexedByPermanentItemLocation.TryGetValue(storeroomAssignmentID, out Tuple<Guid, Guid> ailValue))
				return ailValue.Item1;
			throw new GeneralException(KB.K("Storeroom Assignment reference {0} is not valid for this database"), storeroomAssignmentID);
		}
		private Guid GetAILCostCenterID(Guid storeroomAssignmentID) {
			if (AILValuesIndexedByPermanentItemLocation.TryGetValue(storeroomAssignmentID, out Tuple<Guid, Guid> ailValue))
				return ailValue.Item2;
			throw new GeneralException(KB.K("Storeroom Assignment reference {0} is not valid for this database"), storeroomAssignmentID);
		}
		Guid AdjustmentCodeID;
		Guid AdjustmentCostCenterID;
		static DateTime PCEffectiveDate = (DateTime)dsMB.Schema.T.AccountingTransaction.F.EffectiveDate.WriteType.ClosestValueTo(DateTime.Now);
		/// <summary>
		/// Keep all the ActualItemLocation data in a dictionary indexed by PermanentItemLocation (the ID used in the physical count reference records feeding us input)
		/// </summary>
		Dictionary<Guid?, Tuple<Guid, Guid>> AILValuesIndexedByPermanentItemLocation = new Dictionary<Guid?, Tuple<Guid, Guid>>();

		private void SetupLookValues(dsMB ds, string adjustmentCode) {
			ds.EnsureDataTableExists(dsMB.Schema.T.ItemAdjustmentCode);
			ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.ItemAdjustmentCode, SqlExpression.Constant(adjustmentCode).Eq(new SqlExpression(dsMB.Path.T.ItemAdjustmentCode.F.Code)), null, new DBI_PathToRow[] {
					dsMB.Path.T.ItemAdjustmentCode.F.CostCenterID.PathToReferencedRow }
			);
			dsMB.ItemAdjustmentCodeDataTable ajTable = ds.T.ItemAdjustmentCode;
			if (ajTable.Rows.Count != 1)
				throw new GeneralException(KB.K("Was unable to locate the Adjustment Code record for '{0}'"), adjustmentCode);
			// Check the associated CostCenter is still valid (i.e. not deleted)
			var aRecord = ((dsMB.ItemAdjustmentCodeRow)ajTable.Rows[0]);
			var ccRow = (dsMB.CostCenterRow)ds.T.CostCenter.RowFind(aRecord.F.CostCenterID);
			if (ccRow == null || ccRow.F.Hidden.HasValue)
				throw new GeneralException(KB.K("Cost Center associated with Adjustment Code is missing or has been deleted and cannot be used"));

			AdjustmentCodeID = aRecord.F.Id;
			AdjustmentCostCenterID = aRecord.F.CostCenterID;
			ajTable.Clear();

			ds.EnsureDataTableExists(dsMB.Schema.T.ActualItemLocation, dsMB.Schema.T.PermanentItemLocation);
			ds.DB.ViewOnlyRows(ds, dsMB.Schema.T.PermanentItemLocation, null, null, new DBI_PathToRow[] {
					dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.PathToReferencedRow
				});
			var AILTable = ds.T.ActualItemLocation;
			foreach (dsMB.ActualItemLocationRow row in AILTable) {
				AILValuesIndexedByPermanentItemLocation.Add(row.F.PermanentItemLocationID, new Tuple<Guid, Guid>(row.F.ItemLocationID, row.F.CostCenterID));
			}
			AILTable.Clear();
		}
		public DataSet CreatePhysicalCounts(dsMB ds, string adjustmentCode, Source userIDSource) {
			SetupLookValues(ds, adjustmentCode);
			ds.EnsureDataTableExists(dsMB.Schema.T.ItemCountValue);
			ds.EnsureDataTableExists(dsMB.Schema.T.AccountingTransaction);

			const string ErrorColumnName = "ErrorOnSave";
			DataSet errorDataSet = DataHelper.DataSet.Clone();
			DataTable errorDataTable = errorDataSet.Tables[0];
			errorDataTable.Columns.Add(new DataColumn(ErrorColumnName, typeof(string)));

			DataTablePositioner importPositioner = new DataTablePositioner(DataHelper.DataTable);
			Source storeroomID = importPositioner.GetDataColumnSource(DataHelper.DataTable.Columns[dsPhysicalCount.Path.T.PhysicalCount.F.StoreroomAssignmentID.Column.Name]);
			Source PCQuantity = importPositioner.GetDataColumnSource(DataHelper.DataTable.Columns[dsPhysicalCount.Path.T.PhysicalCount.F.Quantity.Column.Name]);
			Source PCCost = importPositioner.GetDataColumnSource(DataHelper.DataTable.Columns[dsPhysicalCount.Path.T.PhysicalCount.F.Cost.Column.Name]);

			dsMB.ItemCountValueDataTable itemCountTable = ds.T.ItemCountValue;
			dsMB.AccountingTransactionDataTable accountingTransactionTable = ds.T.AccountingTransaction;

			for (Position p = importPositioner.StartPosition.Next; !p.IsEnd; p = p.Next) {
				importPositioner.CurrentPosition = p;
				object checkQuantity = PCQuantity.GetValue();
				if (checkQuantity == null)
					continue;
				try {
					Guid pilID = (Guid)storeroomID.GetValue();
					var pcRecord = (dsMB.ItemCountValueRow)itemCountTable.AddNewItemCountValueRow(); // will add AccountingTransaction Base row as well
					pcRecord.F.Cost = (decimal)(dsMB.Path.T.ItemCountValue.F.Cost.ReferencedColumn.EffectiveType.ClosestValueTo(PCCost.GetValue()));
					pcRecord.F.Quantity = (int)(dsMB.Path.T.ItemCountValue.F.Quantity.ReferencedColumn.EffectiveType.GenericAsNativeType(checkQuantity, typeof(int)));
					pcRecord.F.ItemLocationID = GetItemLocationID(pilID);
					pcRecord.F.ItemAdjustmentCodeID = AdjustmentCodeID;

					dsMB.AccountingTransactionRow accountingTransactionRow = pcRecord.AccountingTransactionIDParentRow;
					accountingTransactionRow.F.UserID = (Guid)userIDSource.GetValue();
					accountingTransactionRow.F.EffectiveDate = PCEffectiveDate;
					accountingTransactionRow.F.FromCostCenterID = GetAILCostCenterID(pilID);
					accountingTransactionRow.F.ToCostCenterID = AdjustmentCostCenterID;
					accountingTransactionRow.F.Cost = 0;
					// Update the database one record at a time to detect errors on a per record basis
					ds.DB.Update(ds);
				}
				catch (System.Exception ex) {
					DataRow errorRow = errorDataTable.NewRow();
					DataRow currentRow = DataHelper.DataTable.RowFind(p.Id);
					for (int j = DataHelper.DataTable.Columns.Count; --j >= 0;)
						errorRow[j] = currentRow[j];
					errorRow[ErrorColumnName] = Thinkage.Libraries.Exception.FullMessage(ex);
					errorDataTable.Rows.Add(errorRow);
				}
				itemCountTable.Clear();
				accountingTransactionTable.Clear();
			}
			return errorDataSet;
		}
#endregion
	}
}
