using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thinkage.Libraries.MVC;

namespace Thinkage.MainBoss.WebAccess.Models
{
	/// <summary>
	/// A means to view a record identified by Id
	/// </summary>
	/// <typeparam name="VT"></typeparam>
	internal interface IView<VT>
	{
		VT View(Guid Id);
	}
}