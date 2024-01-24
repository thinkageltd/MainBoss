using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;

using Thinkage.Libraries.TypeInfo;

namespace Thinkage.Applications.MB3TestDataGenerator {
	using Thinkage.Libraries.CommandLineParsing;
	using Thinkage.MainBoss.Database;
	using Thinkage.Libraries.DBILibrary;
	class MyApplicationObject : Thinkage.Libraries.Application {
		public override RunApplicationDelegate GetRunApplicationDelegate {
			get {
				throw new System.Exception("The method or operation is not implemented.");
			}
		}
		protected override void CreateUIFactory() {
			new StandardApplicationIdentification(this, "MainBossDiagnostics", "MainBoss Diagnostics");
			new Thinkage.Libraries.Console.UserInterface(this);
		}
	}
	class Program {
		// at least 100 primes (indexed by row number; if you increase the number of rows, you will need a bigger table)
		// 0 isn't prime, but we want the 0'th cost to be 0
		static int[] Primes100 = {
0, 2,3,5,7,11,13,17,19,23,
29,31,37,41,43,47,53,59,61,67,
71,73,79,83,89,97,101,103,107,109,
113,127,131,137,139,149,151,157,163,167,
173,179,181,191,193,197,199,211,223,227,
229,233,239,241,251,257,263,269,271,277,
281,283,293,307,311,313,317,331,337,347,
349,353,359,367,373,379,383,389,397,401,
409,419,421,431,433,439,443,449,457,461,
463,467,479,487,491,499,503,509,521,523,
541,547,557,563,569,571,577,587,593,599
		};
		static DateTime BaseDateTime = DateTime.Now;
		const int RowsPerTable = 100;
		const int CategoryGroupingRatio = 10;
		const int RelativeDepth = 3;
		static MyApplicationObject AppInstance = new MyApplicationObject();
		private class TestDataSet {
			public TestDataSet(Thinkage.Libraries.DBAccess.DBClient.Connection c) {
				DB = new MB3Client(c);
			}

			public MB3Client DB;
			public DataRow[] PostalLocations = new DataRow[RowsPerTable];
			public DataRow[] PlainRelativeLocations = new DataRow[RowsPerTable];
			public DataRow[] UnitRelativeLocations = new DataRow[RowsPerTable];
			public DataRow[] PermanentStorageLocations = new DataRow[RowsPerTable];
			public DataRow PMGenerationBatchRow;
			// following table must be in dependency order for update/clear to work
			public DBI_Table[] tablesToGenerateFor = new DBI_Table[] {
				dsMB.Schema.T.Location, // variant base table, but need to write before derived tables
				dsMB.Schema.T.PostalAddress,
				dsMB.Schema.T.RelativeLocation,
				dsMB.Schema.T.PlainRelativeLocation,
				dsMB.Schema.T.PermanentStorage,
				dsMB.Schema.T.Contact,
				dsMB.Schema.T.Requestor,
				dsMB.Schema.T.BillableRequestor,
				dsMB.Schema.T.Employee,
				dsMB.Schema.T.RequestAssignee,
				dsMB.Schema.T.WorkOrderAssignee,
				dsMB.Schema.T.PurchaseOrderAssignee,
				dsMB.Schema.T.Trade,
				dsMB.Schema.T.LaborInside,
				dsMB.Schema.T.SystemCode,
				dsMB.Schema.T.UnitOfMeasure,
				dsMB.Schema.T.Project,
				dsMB.Schema.T.PaymentTerm,
				dsMB.Schema.T.ShippingMode,
				dsMB.Schema.T.Ownership,
				dsMB.Schema.T.Miscellaneous,
				dsMB.Schema.T.AccessCode,
				dsMB.Schema.T.AssetCode,
				dsMB.Schema.T.CloseCode,
				dsMB.Schema.T.RequestStateHistoryStatus,
				dsMB.Schema.T.WorkOrderStateHistoryStatus,
				dsMB.Schema.T.PurchaseOrderStateHistoryStatus,
				dsMB.Schema.T.VoidCode,
				dsMB.Schema.T.ItemAdjustmentCode,
				dsMB.Schema.T.ItemIssueCode,
				dsMB.Schema.T.ItemCategory,
				dsMB.Schema.T.Item,
				dsMB.Schema.T.WorkCategory,
				dsMB.Schema.T.WorkOrderPriority,
				dsMB.Schema.T.RequestPriority,
				dsMB.Schema.T.MeterClass,
				dsMB.Schema.T.VendorCategory,
				dsMB.Schema.T.Vendor,
				dsMB.Schema.T.LaborOutside,
				dsMB.Schema.T.ServiceContract,
				dsMB.Schema.T.WorkOrderTemplate,
				dsMB.Schema.T.UnitCategory,
				dsMB.Schema.T.UnitUsage,
				dsMB.Schema.T.Unit,
				dsMB.Schema.T.UnitServiceContract,
				dsMB.Schema.T.SparePart,
				dsMB.Schema.T.Attachment,
				dsMB.Schema.T.Schedule,
				dsMB.Schema.T.Periodicity,
				dsMB.Schema.T.ScheduledWorkOrder,
				dsMB.Schema.T.ItemPrice,
				dsMB.Schema.T.MiscellaneousWorkOrderCost,
				dsMB.Schema.T.PermanentItemLocation,
				dsMB.Schema.T.OtherWorkInside,
				dsMB.Schema.T.OtherWorkOutside,
				dsMB.Schema.T.Meter
			};
			public bool IsCategoryTable(DBI_Table t) {
				return (t == dsMB.Schema.T.VendorCategory
					|| t == dsMB.Schema.T.UnitCategory
					|| t == dsMB.Schema.T.WorkCategory
					|| t == dsMB.Schema.T.ItemCategory
					|| t == dsMB.Schema.T.Trade);
			}
			public int NumberOfRowsPerTable(DBI_Table t) {
				if (IsCategoryTable(t))
					return CategoryGroupingRatio;
				else
					return RowsPerTable;
			}

		}

		static TestDataSet testData;
		internal class TestDataOptable : Optable {
			public StringValueOption OrganizationName;
			public StringValueOption DataBaseName;
			public StringValueOption DataBaseServer;
			public TestDataOptable() {
				Add(OrganizationName = MB3Client.OptionSupport.CreateOrganizationNameOption(true));
				Add(DataBaseServer = MB3Client.OptionSupport.CreateServerNameOption(true));
				Add(DataBaseName = MB3Client.OptionSupport.CreateDatabaseNameOption(true));
			}
		}
		static void Main(string[] args) {
			TestDataOptable Options = new TestDataOptable();
			try {
				Options.Parse(args);
				Options.CheckRequired();
			}
			catch (Thinkage.Libraries.CommandLineParsing.Exception ex) {
				throw new GeneralException(ex.InnerException, Thinkage.Libraries.Translation.KB.T(Thinkage.Libraries.Translation.MessageBuilder.Build(ex.Message, Options.Help)));
			}
			string OrganizationName;
			MB3Client.ConnectionDefinition connection = MB3Client.OptionSupport.ResolveNamedAdHocOrganization(
				Options.OrganizationName, Options.DataBaseServer, Options.DataBaseName, out OrganizationName);
			try {
				testData = new TestDataSet(connection);

				dsMB ds = new dsMB(testData.DB);
				// Clear existing database of records we are about to add in reverse table order. This
				// fails to work if there are container records (e.g. Tasks & Sub Tasks) that reference the same table.
				// Assume we are starting with a clean empty database rather than deleting (or trying to) clear the specified one
				//				ClearRemainder(ds);
				//				ds.Clear();
				// For linking in templates, we need some data that already exists in the database.
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.WorkOrderState);
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.PurchaseOrderState);
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.User);
				ProduceCodingDefinitions(ds);
				LinkDefinitions(ds);
				ds.DB.Update(ds);
			}
			catch (System.Exception ex) {
				Console.WriteLine(Thinkage.Libraries.Exception.FullMessage(ex));
				return;
			}
		}
		static void ClearRemainder(dsMB clearingDs) {
			for (int i = testData.tablesToGenerateFor.Length; --i >= 0; ) {
				DBI_Table t = testData.tablesToGenerateFor[i];
				DeleteTableRows(clearingDs, t);
			}
		}
		static void DeleteTableRows(dsMB ds, DBI_Table t) {
			ds.DB.ViewAdditionalRows(ds, t);
			DataTable table = t.GetDataTable(ds);
			if (t == dsMB.Schema.T.WorkOrderTemplate) {
				// Clear self referencing values in table so delete will not get constraint violation
				foreach (DataRow row in table.Rows)
					dsMB.Schema.T.WorkOrderTemplate.F.ContainingWorkOrderTemplateID[row] = null;

				ds.DB.Update(ds);
			}
			ClearExistingRows(ds, t);
			ds.DB.Update(ds);
			ds.AcceptChanges();
		}
		static void ClearExistingRows(dsMB ds, DBI_Table t) {
			DataTable table = t.GetDataTable(ds);
			foreach (DataRow row in table.Rows) {
				row.Delete();
			}
		}

		static void ProduceCodingDefinitions(dsMB ds) {
			foreach (DBI_Table t in testData.tablesToGenerateFor) {
				if (t.IsVariantBaseTable)
					continue;
				if (t.IsVariantDerivedTable)
					BuildVariantRows(ds, t);
				else
					BuildRows(ds, t);
			}
			// Fill in data that wasn't previously built
			foreach (DBI_Table t in testData.tablesToGenerateFor) {
				FillInDataRows(ds, t);
			}
		}
		static void BuildVariantRows(dsMB ds, DBI_Table t) {
			int rowCount = testData.NumberOfRowsPerTable(t);
			for (int i = 0; i < rowCount; ++i) {
				DataRow row = ds.DB.AddNewRowAndBases(ds, t);
				if (t == dsMB.Schema.T.PostalAddress) {
					testData.PostalLocations[i] = row;
				}
				else if (t == dsMB.Schema.T.PlainRelativeLocation) {
					DataRow rrow = dsMB.Schema.T.RelativeLocation.GetDataTable(ds).Rows.Find((Guid)dsMB.Schema.T.PlainRelativeLocation.F.RelativeLocationID[row]);
					if (i % RelativeDepth == 0)
						dsMB.Schema.T.RelativeLocation.F.ContainingLocationID[rrow] = (Guid)dsMB.Schema.T.PostalAddress.F.LocationID[testData.PostalLocations[i]];
					else
						dsMB.Schema.T.RelativeLocation.F.ContainingLocationID[rrow] = (Guid)dsMB.Schema.T.RelativeLocation.F.LocationID[testData.PlainRelativeLocations[i - 1]];
					testData.PlainRelativeLocations[i] = rrow;
					FillRow(ds, dsMB.Schema.T.RelativeLocation, rrow, i, "Plain");
				}
				else if (t == dsMB.Schema.T.PermanentStorage) {
					DataRow rrow = dsMB.Schema.T.RelativeLocation.GetDataTable(ds).Rows.Find((Guid)dsMB.Schema.T.PermanentStorage.F.RelativeLocationID[row]);
					if (i % RelativeDepth == 0)
						dsMB.Schema.T.RelativeLocation.F.ContainingLocationID[rrow] = (Guid)dsMB.Schema.T.PostalAddress.F.LocationID[testData.PostalLocations[i]];
					else
						dsMB.Schema.T.RelativeLocation.F.ContainingLocationID[rrow] = (Guid)dsMB.Schema.T.RelativeLocation.F.LocationID[testData.PlainRelativeLocations[i]];
					testData.PermanentStorageLocations[i] = rrow;
					FillRow(ds, dsMB.Schema.T.RelativeLocation, rrow, i, "Storeroom");
				}
				else if (t == dsMB.Schema.T.Unit) {
					DataRow rrow = dsMB.Schema.T.RelativeLocation.GetDataTable(ds).Rows.Find((Guid)dsMB.Schema.T.Unit.F.RelativeLocationID[row]);
					if (i % RelativeDepth == 0)
						dsMB.Schema.T.RelativeLocation.F.ContainingLocationID[rrow] = (Guid)dsMB.Schema.T.RelativeLocation.F.LocationID[testData.PlainRelativeLocations[i]];
					else
						dsMB.Schema.T.RelativeLocation.F.ContainingLocationID[rrow] = (Guid)dsMB.Schema.T.RelativeLocation.F.LocationID[testData.UnitRelativeLocations[i - 1]];
					testData.UnitRelativeLocations[i] = rrow;
					FillRow(ds, dsMB.Schema.T.RelativeLocation, rrow, i, "Unit");
					// Special values for replacement cost calculations
					dsMB.Schema.T.Unit.F.ScrapDate[row] = BaseDateTimeYearsAdd(i);
					dsMB.Schema.T.Unit.F.ReplacementCost[row] = (decimal)(rowCount - i) * 1000m;
				}
				else if (t == dsMB.Schema.T.PermanentItemLocation) {
					DataRow ailrow = dsMB.Schema.T.ActualItemLocation.GetDataTable(ds).Rows.Find((Guid)dsMB.Schema.T.PermanentItemLocation.F.ActualItemLocationID[row]);
					DataRow ilrow = dsMB.Schema.T.ItemLocation.GetDataTable(ds).Rows.Find((Guid)dsMB.Schema.T.ActualItemLocation.F.ItemLocationID[ailrow]);
					dsMB.Schema.T.ItemLocation.F.LocationID[ilrow] = dsMB.Schema.T.RelativeLocation.F.LocationID[testData.PermanentStorageLocations[i]];
					dsMB.Schema.T.PermanentItemLocation.F.Minimum[row] = i;
					dsMB.Schema.T.PermanentItemLocation.F.Maximum[row] = 2 * i;
					dsMB.Schema.T.ItemLocation.F.ItemID[ilrow] = FindRowToLinkTo(ds, dsMB.Schema.T.ItemLocation, i, dsMB.Schema.T.ItemLocation.F.ItemID.EffectiveType as LinkedTypeInfo);
					dsMB.Schema.T.ItemLocation.F.ItemPriceID[ilrow] = FindRowToLinkTo(ds, dsMB.Schema.T.ItemLocation, i, dsMB.Schema.T.ItemLocation.F.ItemPriceID.EffectiveType as LinkedTypeInfo);
				}
			}
		}
		static DateTime BaseDateTimeYearsAdd(int n) {
			return new DateTime(BaseDateTime.Year + n, BaseDateTime.Month, BaseDateTime.Day);
		}
		static void BuildRows(dsMB ds, DBI_Table t) {
			int rowCount = testData.NumberOfRowsPerTable(t);
			for (int i = 0; i < rowCount; ++i) {
				ds.DB.AddNewRowAndBases(ds, t);
			}
		}
		static void FillInDataRows(dsMB ds, DBI_Table t) {
			for (int i = t.GetDataTable(ds).Rows.Count; --i >= 0; ) {
				FillRow(ds, t, i);
			}
		}
		/// <summary>
		/// Set the columns of a row to 'generated' data patterns according to EffectiveType
		/// </summary>
		/// <param name="ds"></param>
		/// <param name="t"></param>
		/// <param name="number"></param>
		static void FillRow(dsMB ds, DBI_Table t, int number) {
			DataRow row = t.GetDataTable(ds).Rows[number];
			FillRow(ds, t, row, number, "_");
			// Special cases as required
			if (t == dsMB.Schema.T.ServiceContract) {
				dsMB.Schema.T.ServiceContract.F.EndDate[row] = BaseDateTimeYearsAdd(number);
				dsMB.Schema.T.ServiceContract.F.Parts[row] = (number % 2) == 0 ? true : false;
				dsMB.Schema.T.ServiceContract.F.Labor[row] = (number % 3) == 0 ? true : false;
			}
			if (t == dsMB.Schema.T.LaborInside) {
				dsMB.Schema.T.LaborInside.F.Cost[row] = Primes100[number] * 10m;
			}
			if (t == dsMB.Schema.T.OtherWorkInside) {
				dsMB.Schema.T.OtherWorkInside.F.Cost[row] = Primes100[number] * 10m;
			}
			if (t == dsMB.Schema.T.LaborOutside) {
				dsMB.Schema.T.LaborOutside.F.Cost[row] = Primes100[number] * 100m;
			}
			if (t == dsMB.Schema.T.OtherWorkOutside) {
				dsMB.Schema.T.OtherWorkOutside.F.Cost[row] = Primes100[number] * 100m;
			}
			if (t == dsMB.Schema.T.MiscellaneousWorkOrderCost) {
				dsMB.Schema.T.MiscellaneousWorkOrderCost.F.Cost[row] = Primes100[number] * 1000m;
			}
			if (t == dsMB.Schema.T.Periodicity) {
				int type = number % 3; // meter, calendar days or calendar months
				if (type == 2) // It is a meter, remove calendar unit value
					dsMB.Schema.T.Periodicity.F.CalendarUnit[row] = null;
				else {
					dsMB.Schema.T.Periodicity.F.CalendarUnit[row] = type; // alternate between days / months
				}
			}
			if (t == dsMB.Schema.T.ScheduledWorkOrder) {
				if (testData.PMGenerationBatchRow == null) {
					testData.PMGenerationBatchRow = ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.PMGenerationBatch);
					FillRow(ds, dsMB.Schema.T.PMGenerationBatch, 0);
					dsMB.Schema.T.PMGenerationBatch.F.UserID[testData.PMGenerationBatchRow] = FindRowToLinkTo(ds, dsMB.Schema.T.PMGenerationBatch, 0, dsMB.Schema.T.PMGenerationBatch.F.UserID.EffectiveType as LinkedTypeInfo);
				}
				DataRow basisRow = ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.PMGenerationDetail);
				dsMB.Schema.T.PMGenerationDetail.F.PMGenerationBatchID[basisRow] = dsMB.Schema.T.PMGenerationBatch.F.Id[testData.PMGenerationBatchRow];
				dsMB.Schema.T.PMGenerationDetail.F.DetailType[basisRow] = (int)DatabaseEnums.PMType.ManualReschedule;
				dsMB.Schema.T.PMGenerationDetail.F.ScheduleDate[basisRow] = BaseDateTime;
				dsMB.Schema.T.PMGenerationDetail.F.NextAvailableDate[basisRow] = BaseDateTime;
				dsMB.Schema.T.PMGenerationDetail.F.WorkStartDate[basisRow] = BaseDateTime;
				dsMB.Schema.T.PMGenerationDetail.F.ScheduledWorkOrderID[basisRow] = dsMB.Schema.T.ScheduledWorkOrder.F.Id[row];
				dsMB.Schema.T.PMGenerationDetail.F.Sequence[basisRow] = number;
				dsMB.Schema.T.PMGenerationDetail.F.FirmScheduleData[basisRow] = true;
			}
			if (t == dsMB.Schema.T.Attachment) {
				dsMB.Schema.T.Attachment.F.Path[row] = String.Format("http://www{0:d02}.thinkage.ca", number);
			}
		}
		static bool IsHiddenColumn(DBI_Column c) {
			return c.Table.DeleteMethod == DBI_Table.DeleteMethods.Hide && c.Table.PathToHiddenColumn.ReferencedColumn == c;
		}
		static void FillRow(dsMB ds, DBI_Table t, DataRow row, int number, string ident) {
			foreach (DBI_Column c in t.Columns) {
				if (c == t.InternalIdColumn || IsHiddenColumn(c) || !c.IsWriteable || !row.IsNull(c.Name))
					continue;
				if (c.EffectiveType is StringTypeInfo) {
					if (((StringTypeInfo)c.EffectiveType).MaxLines > 1)
						c[row] = String.Format("{0:d02}{1}{2}_{3} line 1\r\n{0:d02}{1}{2}_{3} line 2\r\n{0:d02}{1}{2}_{3} line 3\r\n",
									number, ident, c.Name, t.Name,
									number, ident, c.Name, t.Name,
									number, ident, c.Name, t.Name);
					else
						c[row] = String.Format("{0:d02}{1}{2}_{3}", number, ident, c.Name, t.Name);
				}
				if (c.EffectiveType is PercentTypeInfo) {
					c[row] = (float)number / 100.0;
				}
				if (c.EffectiveType is DateTimeTypeInfo) {
					c[row] = BaseDateTime;
				}
				if (c.EffectiveType is CurrencyTypeInfo) {
					c[row] = (decimal)number * 1000m;
				}
				if (c.EffectiveType is IntegralTypeInfo) {
					IntegralTypeInfo it = c.EffectiveType as IntegralTypeInfo;
					int upperBound = (int)it.NativeMaxLimit(typeof(int));
					c[row] = number % upperBound;
				}
				if (c.EffectiveType is MonthIntervalTypeInfo) {
					c[row] = new Thinkage.Libraries.ValueTypes.MonthSpan(number);
				}
				if (c.EffectiveType is IntervalTypeInfo) {
					c[row] = new TimeSpan(number, 0, 0, 0);
				}
				if (c.EffectiveType is BoolTypeInfo) {
					c[row] = number % 2 == 0 ? false : true;
				}
			}
		}
		#region Link tables
		static void LinkDefinitions(dsMB ds) {
			foreach (DBI_Table t in testData.tablesToGenerateFor) {
				DataTable table = t.GetDataTable(ds);
				int rowCount = testData.NumberOfRowsPerTable(t);
				for (int i = 0; i < rowCount; ++i) {
					DataRow row = table.Rows[i];
					foreach (DBI_Column c in t.Columns) {
						if (c == t.InternalIdColumn || IsHiddenColumn(c) || !c.IsWriteable || !row.IsNull(c.Name))
							continue;
						LinkedTypeInfo ltype = c.EffectiveType as LinkedTypeInfo;
						if (ltype != null) {
							Guid link = FindRowToLinkTo(ds, t, i, ltype);
							if (link != Guid.Empty)
								c[row] = link;
						}
					}
				}
			}
		}

		static Guid FindRowToLinkTo(dsMB ds, DBI_Table fromTable, int number, LinkedTypeInfo ltype) {
			DBI_Table toTable = (DBI_Table)ltype.LinkTo.Container.OwnerTable;

			if (toTable == null)
				return Guid.Empty;
			DataTable table = toTable.GetDataTable(ds);
			if (table == null) {
				Console.WriteLine("Missing destination table {0} for linkage.", toTable.Name);
				return Guid.Empty;
			}
			if (testData.IsCategoryTable(toTable))
				number %= CategoryGroupingRatio;
			// Special case handling
			DataRow rowIndex;
			if (toTable == dsMB.Schema.T.WorkOrderState) {
				rowIndex = table.Rows[number % table.Rows.Count];
			}
			else if (toTable == dsMB.Schema.T.User)
				rowIndex = table.Rows[0];
			else if (toTable == fromTable)// self referencing, don't pick ourselves, but the previous row's ID for max depth of 3
			{
				if (number % RelativeDepth == 0)
					return Guid.Empty;// Top level is not contained
				rowIndex = table.Rows[number - 1];
			}
			else if (toTable == dsMB.Schema.T.Location) {
				if (fromTable == dsMB.Schema.T.UnitServiceContract || fromTable == dsMB.Schema.T.SparePart || fromTable == dsMB.Schema.T.ScheduledWorkOrder || fromTable==dsMB.Schema.T.Attachment || fromTable==dsMB.Schema.T.Meter)
					return (Guid)dsMB.Schema.T.RelativeLocation.F.LocationID[testData.UnitRelativeLocations[number]];
				else if (number % RelativeDepth == 0)
					return (Guid)dsMB.Schema.T.PostalAddress.F.LocationID[testData.PostalLocations[number]];
				else
					return (Guid)dsMB.Schema.T.RelativeLocation.F.LocationID[testData.PlainRelativeLocations[number]];
			}
			else if (toTable == dsMB.Schema.T.RelativeLocation) {
				if (fromTable == dsMB.Schema.T.Unit) {
					if (number % RelativeDepth == 0)
						rowIndex = testData.PlainRelativeLocations[number];
					else
						rowIndex = testData.UnitRelativeLocations[number - 1];
				}
				else if (fromTable == dsMB.Schema.T.PlainRelativeLocation) {
					//					if (number % RelativeDepth == 0)
					rowIndex = testData.PlainRelativeLocations[number];
					//					else
					//						rowIndex = UnitRelativeLocations[number - 1];
				}
				else {
					System.Diagnostics.Debug.Assert(table != null && (toTable.IsVariantBaseTable || table.Rows.Count == testData.NumberOfRowsPerTable(toTable)));
					rowIndex = table.Rows[number];
				}
			}
			else if (toTable == dsMB.Schema.T.MeterClass && fromTable == dsMB.Schema.T.Periodicity && (number % 3 != 2)) // only link up meters for this case only (see Periodicity setting elsewhere)
				return Guid.Empty;
			else
				rowIndex = table.Rows[number];

			return (Guid)toTable.Columns[ltype.LinkTo.Name][rowIndex];
		}
		#endregion
	}
}
