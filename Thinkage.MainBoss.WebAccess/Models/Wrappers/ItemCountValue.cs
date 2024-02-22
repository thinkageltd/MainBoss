using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Thinkage.Libraries;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess;
using Thinkage.MainBoss.WebAccess.Models;

namespace ItemCountValueEntities {
	public partial class ItemAdjustmentCode : ICodeIdPicker {
	}
	public partial class ItemPrice : ICodeIdPicker {
		public string Code { get => Strings.Format(KB.K("{0} from vendor {1}"), Item.Code, Vendor.Code); set => throw new System.NotImplementedException(); }
	}
	[System.Serializable]
	public class ItemPhysicalCount {
		/// <summary>
		/// The ItemAssignment record that this ItemPhysicalCount targets
		/// </summary>
		public System.Guid StorageAssignmentID { get; set; }
		public System.Guid? AdjustmentCodeID { get; set; }
		public System.Guid? ItemPricingID { get; set; }
		public string ExternalTag { get; set; }
		public string ItemCode { get; set; }
		public string ItemDesc { get; set; }
		public long OnHand { get; set; }
		public decimal TotalCost { get; set; }
		public decimal? UnitCost { get; set; }
		public long DaysSinceLastPhysicalCount { get; set; }
		public long RevisedOnHand { get; set; }
		public decimal RevisedCost { get; set; }
		public decimal? RevisedUnitCost { get; set; }
	}
}