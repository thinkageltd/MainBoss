using System.Collections.Generic;
using System.Web.Mvc;

namespace RequestEntities {
	/// <summary>
	/// Wrapper to enclose the Request and all the properties of the Request that are linked to other tables (e.g. AccessCode.Code)
	/// </summary>
	[Bind(Exclude = "RequestPriorityPickList")]
	[System.Data.Services.Common.DataServiceEntity()]
	[System.Data.Services.Common.DataServiceKey("Id")] // Required because heuristic is case sensitive to "ID", and we use "Id" for our primary key
	[System.Data.Services.IgnoreProperties("RequestPriorityPickList", "RequestAssignmentRequest", "RequestStateHistoryRequest")]
	public partial class Request {
		public IEnumerable<SelectListItem> RequestPriorityPickList {
			get;
			set;
		}
		/// <summary>
		/// As an assist to the hapless submitter we provide a field that specifically asks 'Where' in the hopes they may
		/// provide the information to be prepended to the Description field when we save the request. This is intended for sites that want the 'old way'
		/// and not the Unit picker. The textual part of the UnitPicker control will be put into the model (because the picker control is named 'WhereIsProblem')
		/// 
		/// </summary>
		public string WhereIsProblem {
			get;
			set;
		}
	}
	[System.Data.Services.Common.DataServiceEntity()]
	[System.Data.Services.Common.DataServiceKey("Id")]
	public partial class RequestStateHistory {
	}
}