using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
namespace Thinkage.MainBoss.WebAccess.Models {
	/// <summary>
	/// For building pick lists of Code returning Id as a value. We typically encode the 'null' entry with a Guid.Empty value so we can recognize the empty value
	/// when it comes time to assign it into a dataset field as a null value.
	/// </summary>
	public interface ICodeIdPicker {
		Guid Id {
			get;
			set;
		}
		string Code {
			get;
			set;
		}
	}
	/// <summary>
	/// Custom SelectList for PickListWithEmptyOption
	/// </summary>
	public class SelectListWithEmpty : SelectList {
		public SelectListWithEmpty(IEnumerable items, Guid? selectedValue)
			: base(items, "Id", "Code", selectedValue ?? Guid.Empty) {
		}
	}
	/// <summary>
	/// Extensions to the BaseRepository Model for use by all repository classes
	/// </summary>
	public abstract partial class BaseRepository {
		/// <summary>
		/// Provide a SelectList that includes an Empty string with a null determinable value for Id (Guid.Empty)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="pickList"></param>
		/// <param name="dataTextField">The </param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public SelectListWithEmpty PickListWithEmptyOption<T>(IEnumerable<T> pickList, Guid? defaultValue) where T : ICodeIdPicker, new() {
			IEnumerable<T> emptyRp = new List<T>(new T[] {new T()
			{
				Code = "",
				Id = Guid.Empty
			}});
			return new SelectListWithEmpty(emptyRp.Union(pickList), defaultValue);
		}
	}
}