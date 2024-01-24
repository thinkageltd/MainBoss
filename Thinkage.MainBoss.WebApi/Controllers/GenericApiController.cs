using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Thinkage.Libraries;
using Thinkage.MainBoss.WebApi.Models;

namespace Thinkage.MainBoss.WebApi.Controllers
{
	/// <summary>
	/// The base ApiController for all derived MROW object controllers. MROW will be a wrapper for a System.Data.DataRow object defined in a DataSet that has simple properties
	/// reflecting the columns of the DataRow object. This allows simple serialization of the table rows without the overhead of trying to serialize a System.Data.DataRow object.
	/// </summary>
	/// <typeparam name="MROW"></typeparam>
	[Authorize]
	public class GenericApiController<MROW> : ApiController
		where MROW : class, IRepositoryWrapperBase
	{
		/// <summary>
		/// Primary means to check result of an operation and throw an exception if there was an error.
		/// </summary>
		/// <param name="e"></param>
		public static void CheckResponse(GeneralException e)
		{
			if (e != null)
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
				{
					Content = new StringContent(Thinkage.Libraries.Exception.FullMessage(e, includeHelpLink: true)),
					ReasonPhrase = Strings.Format(KB.K("Table {0} request cannot be processed"), typeof(MROW).Name)
				});
		}

		private readonly IGenericRepository<MROW> Repository;
		/// <summary>
		/// Base class for repository operations.
		/// </summary>
		/// <param name="repository"></param>
		public GenericApiController( IGenericRepository<MROW> repository )
		{
			Repository = repository;
		}

		// GET api/<MROW>
		/// <summary>
		/// The collection of records for this table
		/// </summary>
		/// <returns>MROW</returns>
		public IEnumerable<MROW> Get()
		{
			IEnumerable<MROW> result;
			CheckResponse(Repository.Browse(out result));
			return result;
		}

		// GET api/<MROW>/5
		/// <summary>
		/// Return the row associated with id
		/// </summary>
		/// <param name="id">The InternalId of the record</param>
		/// <returns></returns>
		public MROW Get(Guid id)
		{
			MROW result;
			CheckResponse(Repository.GetById(id, out result));
			return result;
		}

		// POST api/<MROW>
		/// <summary>
		/// Create a new MROW
		/// </summary>
		/// <param name="value"></param>
		public void Post([FromBody]MROW value)
		{
			CheckResponse(Repository.Create(value));
		}

		// PUT api/<MROW>/id
		/// <summary>
		/// Update a record identified by id
		/// </summary>
		/// <param name="id">The InternalId of the record</param>
		/// <param name="value"></param>
		public void Put(Guid id, [FromBody]MROW value)
		{
			CheckResponse(Repository.UpdateById(id, (MROW)null, value));
		}

		// DELETE api/<MROW>/5
		/// <summary>
		/// Delete the record identified by id
		/// </summary>
		/// <param name="id">The InternalId of the record</param>
		public void Delete(Guid id)
		{
			CheckResponse(Repository.DeleteById(id));
		}
	}
}
