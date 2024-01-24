using System.Collections.Generic;
using Thinkage.MainBoss.WebAccess.Models;

namespace WorkOrderEntities {
	/// <summary>
	/// Wrapper to enclose the WorkOrder and all associated records (e.g. StateHistory, Resources)
	/// </summary>
	public partial class WorkOrder {
		public List<WOResource> Resources {
			get;
			set;
		}
		public string CannotActualizeBecause {
			get;
			set;
		}
	}
}