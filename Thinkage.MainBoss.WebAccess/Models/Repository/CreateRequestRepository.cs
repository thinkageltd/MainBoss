using System;
using System.Linq;
using Thinkage.Libraries.MVC.Models;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess.Models.interfaces;

namespace Thinkage.MainBoss.WebAccess.Models {
	public class CreateRequestRepository : BaseRepository, IBaseRepository<RequestDataContext> {
		#region Constructor
		public CreateRequestRepository()
			: base("Request", CreateForm) {
		}
		#endregion
		// We need to persist a DataSet across calls to recover possible used sequence numbers. We do this using support from the ApplicationObject
		private readonly Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication ApplicationObject = ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance);
		#region IBaseRepository<RequestDataContext>
		public override void InitializeDataContext() {
			DataContext = new RequestDataContext(Connection.ConnectionString);
		}
		public RequestDataContext DataContext {
			get;
			private set;
		}
		#endregion

		#region RequestPriorityPickList
		/// <summary>
		/// Return the available RequestPriorities
		/// </summary>
		public SelectListWithEmpty RequestPriorityPickList(Guid? defaultValue) {
			return PickListWithEmptyOption<RequestEntities.RequestPriority>(
						from rp in DataContext.RequestPriority
						where rp.Hidden == null
						orderby rp.Code
						select rp,
					defaultValue);
		}
		#endregion

		#region Create New Request
		/// <summary>
		/// Load our Model with the default values from the database.
		/// </summary>
		/// <param name="model"></param>
		public void PrepareForNewRequest(RequestEntities.Request model) {
			SequenceCountDataSet creationDs = new SequenceCountDataSet(MB3DB, dsMB.Schema.T.RequestSequenceCounter, dsMB.Schema.V.WRSequence, dsMB.Schema.V.WRSequenceFormat);
			//Create a dummy row and fish the default values from it.
			dsMB.RequestRow requestRow = (dsMB.RequestRow)creationDs.DataSet.DB.AddNewRowAndBases(creationDs.DataSet, dsMB.Schema.T.Request);
			ApplicationObject.RememberSequenceCountDataSet(requestRow.F.Id, creationDs);
			model.Id = requestRow.F.Id; // so we can find the SequenceCountDataSet on Post
			requestRow.F.Number = creationDs.SequenceCountManager.GetFormattedFirstReservedSequence();
			model.Number = requestRow.F.Number;
			model.Subject = requestRow.F.Subject;
			model.RequestPriorityID = requestRow.F.RequestPriorityID;
			model.Description = requestRow.F.Description;
			model.WhereIsProblem = null;
			model.Requestor = (from r in DataContext.Requestor
							 where r.Id == model.RequestorID && r.Hidden == null
							 select r).SingleOrDefault();
			model.Requestor.Contact = (from c in DataContext.Contact
														  where c.Id == model.Requestor.ContactID && c.Hidden == null
														  select c).SingleOrDefault();

			// Following are only required if we permit editing of the values.
			//				if (requestRow.F.UnitLocationID.HasValue)
			//					model.UnitLocationID = requestRow.F.UnitLocationID;
			//				if (requestRow.F.AccessCodeID.HasValue)
			//					model.AccessCodeID = requestRow.F.AccessCodeID;
			//				if (requestRow.F.Comment.HasValue)
			//					model.Comment = requestRow.F.Comment;
		}
		public void CreateNewRequest(RequestEntities.Request model) {
			SequenceCountDataSet creationDs = ApplicationObject.RecallSequenceCountDataSet(model.Id);
			if (creationDs == null) // Whoa!
				throw new Exception("Did not properly recall SequenceCountDataSet"); // shouldn't happen but if it does, at least we'll know where
			// Determine if user used the sequence number we generated for them; if so tell SequenceManager we consumed it.
			var checkpoint = creationDs.SequenceCountManager.Checkpoint();
			try {
				creationDs.DataSet.DB.PerformTransaction(true,
					delegate()
					{
						dsMB.RequestRow requestRow = (dsMB.RequestRow)creationDs.DataSet.T.Request.Rows[0]; // there should only be one
						// guard against bad default settings on requestRow (e.g. null setting on fields we don't permit user to select like Select for Printing (W20150182)
						try {
							var x = requestRow.F.SelectPrintFlag;
						}
						catch (NullReferenceException) {
							requestRow.F.SelectPrintFlag = false;
						}
						creationDs.SequenceCountManager.ConditionallyConsumeFirstReservedSequence(model.Number);
						requestRow.F.Number = model.Number;
						requestRow.F.RequestorID = model.RequestorID;
						if (model.RequestPriorityID.HasValue) {
							if (model.RequestPriorityID == Guid.Empty)
								requestRow.F.RequestPriorityID = null;
							else
								requestRow.F.RequestPriorityID = model.RequestPriorityID.Value;
						}
						System.Text.StringBuilder description = new System.Text.StringBuilder();
						if(model.UnitLocationID.HasValue) {
							if(model.UnitLocationID == Guid.Empty)
								requestRow.F.UnitLocationID = null;
							else
								requestRow.F.UnitLocationID = model.UnitLocationID.Value;
						}
						else
							requestRow.F.UnitLocationID = FindUnitLocationFromExternalTag(model.WhereIsProblem, creationDs.DataSet); // Search the External Tags for all units for this text; if we match something, use THAT UnitLocationId
						if(!requestRow.F.UnitLocationID.HasValue && model.WhereIsProblem != null) {
							if(requestRow.F.UnitLocationID.HasValue == false) {
								description.Append(KB.K("Where").Translate());
								description.Append(KB.I(": "));
								description.AppendLine(model.WhereIsProblem);
							}
						}
						description.Append(model.Description);
						requestRow.F.Description = description.ToString();
						requestRow.F.Subject = model.Subject;
#if OTHERS
						if (model.AccessCodeID.HasValue)
							requestRow.F.AccessCodeID = model.AccessCodeID.Value;
						requestRow.F.Comment = model.Comment;
#endif

						dsMB.RequestStateHistoryRow stateHistoryRow = (dsMB.RequestStateHistoryRow)creationDs.DataSet.DB.AddNewRowAndBases(creationDs.DataSet, dsMB.Schema.T.RequestStateHistory);
						// new history row has a default RequestStateId set
						stateHistoryRow.F.RequestID = requestRow.F.Id;
						stateHistoryRow.F.EffectiveDate = (DateTime)dsMB.Schema.T.RequestStateHistory.F.EffectiveDate.EffectiveType.ClosestValueTo(DateTime.Now);
						creationDs.DataSet.DB.Update(creationDs.DataSet);
					});
			}
			catch (Exception) {
				creationDs.SequenceCountManager.Rollback(checkpoint);
				throw;
			}
			finally {
				if (creationDs != null)
					creationDs.Destroy();
			}
		}
		#endregion
		private static Guid? FindUnitLocationFromExternalTag(string externalTag, dsMB dataSetToSearchIn) {
			if(externalTag == null)
				return null;
			var row = (dsMB.RelativeLocationRow)dataSetToSearchIn.DB.ViewAdditionalRow(dataSetToSearchIn, dsMB.Schema.T.RelativeLocation, new Libraries.XAF.Database.Layout.SqlExpression(dsMB.Path.T.RelativeLocation.F.ExternalTag).Eq(Libraries.XAF.Database.Layout.SqlExpression.Constant(externalTag)));
			if(row == null)
				return null;
			return row.F.LocationID;
		}

		public static Thinkage.Libraries.MVC.Models.FormMap<RequestEntities.Request> CreateForm = new Thinkage.Libraries.MVC.Models.FormMap<RequestEntities.Request>("Request",
			FormMapping.New(KB.K("Number"), dsMB.Path.T.Request.F.Number),
			FormMapping.New(KB.K("Subject"), dsMB.Path.T.Request.F.Subject),
			FormMapping.New(KB.K("Description"), dsMB.Path.T.Request.F.Description),
			FormMapping.New(KB.K("Request Priority"), dsMB.Path.T.Request.F.RequestPriorityID),
			FormMapping.New(KB.K("Where"), "WhereIsProblem", new Thinkage.Libraries.TypeInfo.StringTypeInfo(0, 256, 0, true, true, true)),
			FormMapping.New(KB.K("Unit Location"), dsMB.Path.T.Request.F.UnitLocationID)
			);
	}
}
