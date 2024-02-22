using System;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Report that first generates the PMs up to the batch end-date given by the user.
	/// StaticFilters that are set through an instance of this class will be discarded and overridden.
	/// </summary>
	public abstract class ForecastReportViewerControl : ReportViewerControl
	{
		private IInputControl fromDateControl, toDateControl;
		public ForecastReportViewerControl(UIFactory uiFactory, DBClient db, Tbl tbl, Settings.Container settingsContainer, SqlExpression filterExpression)
			: base(uiFactory, db, tbl, settingsContainer, filterExpression)
		{
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		protected override void CreateFilterTabs()
		{
			base.CreateFilterTabs();
			LabelledControlPanel lcp = new LabelledControlPanel(UIFactory, true, null);
			// PM generation parameters
			// From date
			Key fromLabel = KB.K("The first day of work");
			// our dateControl values will be used in a SqlExpression so make sure we only provide values acceptable to Sql
			DateTimeTypeInfo limitDateTypeInfo = (DateTimeTypeInfo)Thinkage.Libraries.XAF.Database.Service.MSSql.Server.SqlDateTimeTypeInfo.IntersectCompatible(ObjectTypeInfo.NonNullUniverse);
			fromDateControl = UIFactory.CreateDateTimePicker(limitDateTypeInfo, null, null);
			var wrappedFromDateControl = UIFactory.WrapFeedbackProvider(fromDateControl, fromDateControl);
			fromDateControl.Value = DateTime.Today;
			// To date
			Key toLabel = KB.K("The last day of work");
			toDateControl = UIFactory.CreateDateTimePicker(limitDateTypeInfo, null, null);
			var wrappedToDateControl = UIFactory.WrapFeedbackProvider(toDateControl, toDateControl);
			using (dsMB tempds = new dsMB(DB))
			{
				DB.ViewAdditionalVariables(tempds, dsMB.Schema.V.PmGenerateInterval);
				toDateControl.Value = DateTime.Today.AddDays(((TimeSpan)tempds.V.PmGenerateInterval.Value).Days);
			}
			// The panel to contain the controls.
			lcp.AddControl(KB.K("From Date"), wrappedFromDateControl);
			lcp.AddControl(KB.K("To Date"), wrappedToDateControl);
			// Add the date controls to the first filter tab-page... sounds arbitrary doesn't it?  We should really put these date control in a separate filter tab-page,
			// but there's currently no easy way to positioning the new filter tab-page to gel with the other tab-pages (should be inserted between the grouping page
			// and the regular filter page.
			FirstFilterPage.Add(UIFactory.CreateGroupBox(KB.K("PM Generation Filters"), lcp));
		}
		System.Guid LastBatchId = Guid.Empty;
		protected override void DeconfigureReport() {
			base.DeconfigureReport();
			// if we have a batch from before, delete it now before making a new one.
			if (LastBatchId != Guid.Empty) {
				using (dsMB tempds = new dsMB(DB)) {
					try {
						var row = DB.EditSingleRow(tempds, dsMB.Schema.T.PMGenerationBatch, new SqlExpression(dsMB.Path.T.PMGenerationBatch.F.Id).Eq(SqlExpression.Constant(LastBatchId)));
						row.Delete();
						DB.Update(tempds);
					}
					catch (System.Exception) { // ignore ALL possible exceptions at this point, not just General Exception (DB may be null for example, or the DBConnection has gone away)
						// what do we want to do? just ignore the exception as we were trying to delete the batch and associated records of previous generation.
					}
					LastBatchId = Guid.Empty;
				}
			}
		}
		protected override void ConfigureReport() {
			// generate the PMs before we can report on them.
			using (dsMB tempds = new dsMB(DB)) {
				// TODO: The end of the generate interval and the end date requested by the user should match.
				// Get batch end date from user
				tempds.EnsureDataTableExists(dsMB.Schema.T.PMGenerationBatch);
				{
					dsMB.PMGenerationBatchRow bRow = tempds.T.PMGenerationBatch.AddNewPMGenerationBatchRow();
					LastBatchId = bRow.F.Id;
					bRow.F.UserID = Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().UserRecordID;
					bRow.F.SessionID = null;
					bRow.F.EndDate = (DateTime)toDateControl.Value;
					bRow.F.FilterUsed = null;
					bRow.F.Comment = null;
					bRow.F.SinglePurchaseOrders = false;
					bRow.F.PurchaseOrderCreationStateID = null;
					bRow.F.WorkOrderCreationStateID = null;

					using (var PMGenerator = new PMGeneration(tempds)) {
						ICheckpointData checkpoint = PMGenerator.Checkpoint();
						IProgressDisplay ipd = UIFactory.CreateProgressDisplay(KB.K("Maintenance Schedule Generation"), PMGeneration.GenerationProgressSteps);
						try {
							PMGenerator.Generate(bRow, null, null, ipd);
							// TODO: Now that we are using a 'push' model is it still necessary to save the batch and detail records to the database?
							// TODO: Assuming the Update is required, is there a place where the batch could be deleted again (assuming PMGenerator could generate the appropriate
							// Deleted DataRows) so they don't linger until the user quits mainboss?
							DB.Update(tempds);
						}
						catch {
							PMGenerator.Rollback(checkpoint);
							throw;
						}
						finally {
							ipd.Complete();
							PMGenerator.Destroy();
						}
					}
				}
				ReportViewerLogic.AddNonStaticFilter(composeStaticFilter(LastBatchId, (DateTime)fromDateControl.Value, (DateTime)toDateControl.Value));
			}
			base.ConfigureReport();
		}
		/// <summary>
		/// Compose a static filter to filter in only valid PMs generated by us and W/O within the given dates.
		/// </summary>
		/// <param name="batchId">Id of the PM Batch generated by us</param>
		/// <param name="fromDate"></param>
		/// <param name="toDate"></param>
		/// <returns>composed filter</returns>
		protected abstract SqlExpression composeStaticFilter(Guid batchId, DateTime fromDate, DateTime toDate);
	}

	public class ResourceForecastReportViewerControl : ForecastReportViewerControl {
		public ResourceForecastReportViewerControl(UIFactory uiFactory, DBClient db, Tbl tbl, Settings.Container settingsContainer, SqlExpression filterExpression)
			: base(uiFactory, db, tbl, settingsContainer, filterExpression) {
		}
		protected override SqlExpression composeStaticFilter(Guid batchId, DateTime fromDate, DateTime toDate) {
			SqlExpression batchFilter = new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.PMGenerationDetailID.F.PMGenerationBatchID).Eq(batchId)
				.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.PMGenerationDetailID).IsNull());
			SqlExpression woFilter = new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.EndDateEstimate).GEq(fromDate)
				.And(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.StartDateEstimate).LEq(toDate));
			return batchFilter.And(woFilter);
		}
	}

	public class MaintenanceForecastReportViewerControl : ForecastReportViewerControl
	{
		public MaintenanceForecastReportViewerControl(UIFactory uiFactory, DBClient db, Tbl tbl, Settings.Container settingsContainer, SqlExpression filterExpression)
			: base(uiFactory, db, tbl, settingsContainer, filterExpression)
		{
		}
		protected override SqlExpression composeStaticFilter(Guid batchId, DateTime fromDate, DateTime toDate)
		{
			SqlExpression batchFilter = new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.PMGenerationBatchID).Eq(batchId)
				.Or(new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.PMGenerationBatchID).IsNull());
			SqlExpression woFilter = new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.WorkStartDate).GEq(fromDate)
				.And(new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.WorkStartDate).LEq(toDate));
			return batchFilter.And(woFilter);
		}
	}
}
