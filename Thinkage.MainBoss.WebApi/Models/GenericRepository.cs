using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary;
using System.Linq;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebApi.Models
{
	#region IRepository
	public interface IRepositoryWrapperBase
	{
	}
	/// <summary>
	/// All Api objects are based on this interface
	/// </summary>
	public interface IRepositoryWrapper<MROW,DR> : IRepositoryWrapperBase
	{
		/// <summary>
		/// Create a new MROW object, and update its contents from row
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		MROW Create(DR row);
		/// <summary>
		/// Update ourself from row values
		/// </summary>
		/// <param name="row"></param>
		void UpdateTo(DR row);
		/// <summary>
		/// Update row using our values.
		/// </summary>
		/// <param name="row"></param>
		void UpdateFrom(DR row);
	}
	#endregion
	/// <summary>
	/// General respository interface for interaction with records in Thinkage DBILibrary defined tables
	/// </summary>
	/// <typeparam name="MROW">Model row type</typeparam>
	public interface IGenericRepository<MROW>
	{
		/// <summary>
		/// Return a collection of {MROW} objects
		/// </summary>
		/// <returns>Null if successfull</returns>
		GeneralException Browse(out IEnumerable<MROW> result);
		/// <summary>
		/// Return an {MROW} object corresponding to the id
		/// </summary>
		/// <returns>Null if successfull</returns>
		GeneralException GetById(Guid id, out MROW result);
		/// <summary>
		/// Create a database record from an {MROW} object
		/// </summary>
		/// <returns>Null if successfull</returns>
		GeneralException Create(MROW o);
		/// <summary>
		/// Delete a database record corresponding to the id
		/// </summary>
		/// <returns>Null if successfull</returns>
		GeneralException DeleteById(Guid id);
		/// <summary>
		/// Update a database record identified by id, using values in an {MROW} object
		/// </summary>
		/// <returns>Null if successfull</returns>
		GeneralException UpdateById(Guid id, MROW original, MROW update);
		// more to follow
	}
#pragma warning disable 1591
	public abstract class GenericRepository<MROW,DR> : IGenericRepository<MROW> 
		where MROW : class, IRepositoryWrapper<MROW,DR>
		where DR : System.Data.DataRow
	{
		protected dsMB DataSet
		{
			get{

				if (pDataset == null)
					pDataset = new dsMB(MB3DB);
				return pDataset;
			}

		}
		private dsMB pDataset;
		/// <summary>
		/// The MB3Client session for this request
		/// </summary>
		protected MB3Client MB3DB
		{
			get
			{
				return (Thinkage.MainBoss.Database.MB3Client)((Thinkage.MainBoss.WebApi.MainBossWebApiApplication)Thinkage.Libraries.Application.Instance).GetInterface<ISessionDatabaseFeature>().Session;
			}
		}
		/// <summary>
		/// The underlying DBIDataTable for this table
		/// </summary>
		public abstract DBIDataTable Table
		{
			get;
		}
		/// <summary>
		/// The RowWrapper for creating/updating an Model row based on a DataRow
		/// </summary>
		public abstract IRepositoryWrapper<MROW,DR> RowWrapper
		{
			get;
		}

		#region IGenericRepository<R> Members
		/// <summary>
		/// Obtain a collection of {MROW} records
		/// </summary>
		/// <param name="result">The {MROW} records</param>
		/// <returns>Null if successful</returns>
		public GeneralException Browse(out System.Collections.Generic.IEnumerable<MROW> result)
		{
			result = null;
			try {
				DataSet.Clear();
				MB3DB.ViewAdditionalRows(DataSet, Table.Schema);
				List<MROW> rows = new List<MROW>();
				foreach (DR row in Table.Rows)
					rows.Add(RowWrapper.Create(row));
				result = rows;
				return null;
			}
			catch (GeneralException x) {
				return x;
			}
		}
		/// <summary>
		/// Obtain an {MROW} record 
		/// </summary>
		/// <param name="id">The InternalID of the record</param>
		/// <param name="result">The {MROW} record</param>
		/// <returns>Null if successful</returns>
		public GeneralException GetById(Guid id, out MROW result)
		{
			result = null;
			try {
				DataSet.Clear();
				DR row = (DR)MB3DB.ViewAdditionalRow(DataSet, Table.Schema, new SqlExpression(Table.Schema.InternalId).Eq(SqlExpression.Constant(id)));
				if (row == null)
					throw new GeneralException(KB.K("Record not found"));
				result = RowWrapper.Create(row);
				return null;
			}
			catch (GeneralException x) {
				return x;
			}
		}
		/// <summary>
		/// Create a new record from {MROW} object
		/// </summary>
		/// <param name="o">The {MROW} object</param>
		/// <returns>Null if successful</returns>
		public GeneralException Create(MROW o)
		{
			try {
				DataSet.Clear();
				DR row = (DR)MB3DB.AddNewRowAndBases(DataSet, Table.Schema);
				o.UpdateTo(row);
				MB3DB.Update(DataSet);
				return null;
			}
			catch (GeneralException x) {
				return x;
			}
		}

		/// <summary>
		/// Delete a record specified by Id
		/// </summary>
		/// <param name="id">The InternalID of the record</param>
		/// <returns>Null if successful</returns>
		public GeneralException DeleteById(Guid id)
		{
			try {
				DataSet.Clear();
				DR row = (DR)MB3DB.ViewAdditionalRow(DataSet, Table.Schema, new SqlExpression(Table.Schema.InternalId).Eq(SqlExpression.Constant(id)));
				if (row == null)
					throw new GeneralException(KB.K("Record not found"));
				row.Delete();
				MB3DB.Update(DataSet);
				return null;
			}
			catch (GeneralException x) {
				return x;
			}
		}
		/// <summary>
		/// Update a record from an {MROW} object
		/// </summary>
		/// <param name="id">The InternalID of the record</param>
		/// <param name="original">The {MROW} object with original values</param>
		/// <param name="update">The {MROW} object with changed values</param>
		/// <returns>Null if successful</returns>
		public GeneralException UpdateById(Guid id, MROW original, MROW update)
		{
			try {
				DataSet.Clear();
				DR row = (DR)MB3DB.EditSingleRow(DataSet, Table.Schema, new SqlExpression(Table.Schema.InternalId).Eq(SqlExpression.Constant(id)));
				if (row == null)
					throw new GeneralException(KB.K("Record not found"));
				row.BeginEdit();
				update.UpdateTo(row);
				row.EndEdit();
				MB3DB.Update(DataSet);
				return null;
			}
			catch (GeneralException x) {
				return x;
			}
		}
		#endregion
	}
}
