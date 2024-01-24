using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thinkage.Libraries.MVC;

namespace Thinkage.MainBoss.WebAccess.Models
{
	/// <summary>
	/// Provide means to Browse records of a table and to view details
	/// </summary>
	/// <typeparam name="T">The particular record type</typeparam>
	/// <typeparam name="VT">The view type returned</typeparam>
	interface IBrowse<VT> : IView<VT>
	{
		//		IQueryable<T> Browse();
		IQueryable<VT> BrowseAssigned();
		IQueryable<VT> BrowseUnAssigned();
	}
}