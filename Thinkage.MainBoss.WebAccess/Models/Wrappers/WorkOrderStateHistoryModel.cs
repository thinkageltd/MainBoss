using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Thinkage.MainBoss.WebAccess.Models {
	public class WorkOrderStateHistoryModel : WorkOrderCloseEntities.WorkOrderStateHistory {
		/// <summary>
		/// The CurrentStateHistoryID associated with the WorkOrder in question
		/// </summary>
		public Guid CurrentStateHistoryID {
			get;
			set;
		}
		/// <summary>
		/// We keep the WorkOrderState Code as string, and translate it when we set it. The View code that references it uses the WorkOrderStateHistoryStatus EffectiveType as the underlying typeinfo
		/// since it is a string type info. This is all to avoid having Serialization of a Translation.SimpleKey implemented.
		/// </summary>
		[System.Runtime.Serialization.IgnoreDataMember]
		public Thinkage.Libraries.Translation.Key CurrentStateCode {
			get;
			set;
		}
		public string CurrentStateCodeAsText {
			get;
			set;
		}

		public string CurrentStatusCode {
			get;
			set;
		}
		public string LastComment {
			get;
			set;
		}
		/// <summary>
		/// The identifying WorkOrder number associated with this history record
		/// </summary>
		public string WorkOrderNumber {
			get;
			set;
		}
		/// <summary>
		/// The identifying WorkOrder Subject associated with this history record
		/// </summary>
		public string WorkOrderSubject {
			get;
			set;
		}
		/// <summary>
		/// The identifying WorkOrder Description associated with this history record (to inform the WorkOrderor when they make a comment)
		/// </summary>
		public string WorkOrderDescription {
			get;
			set;
		}
		[System.Runtime.Serialization.IgnoreDataMember]
		public IEnumerable<SelectListItem> CloseCodePickList {
			get;
			set;
		}
		[System.Runtime.Serialization.IgnoreDataMember]
		public IEnumerable<SelectListItem> WorkOrderStateHistoryStatusPickList {
			get;
			set;
		}
		// Values in main WorkOrder record we allow to change while Closing
		public DateTime StartDateEstimate {
			get;
			set;
		}
		public DateTime EndDateEstimate {
			get;
			set;
		}
		public TimeSpan? Downtime {
			get;
			set;
		}
		public Guid? CloseCodeID {
			get;
			set;
		}
	}
}
