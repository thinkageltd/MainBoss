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
	public partial class ItemActivity {
		#region ItemActivityProvider
		private static object[] ItemActivityValues = new object[] {
			(int)ViewRecordTypes.ItemActivity.NotSpecified,
			(int)ViewRecordTypes.ItemActivity.ItemCountValue,
			(int)ViewRecordTypes.ItemActivity.VoidItemCountValue,
			(int)ViewRecordTypes.ItemActivity.ItemAdjustment,
			(int)ViewRecordTypes.ItemActivity.ItemIssue,

			(int)ViewRecordTypes.ItemActivity.ItemTransferTo,
			(int)ViewRecordTypes.ItemActivity.ItemTransferFrom,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemPO,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemNonPO,
			(int)ViewRecordTypes.ItemActivity.ActualItem,

			(int)ViewRecordTypes.ItemActivity.ItemIssueCorrection,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemPOCorrection,
			(int)ViewRecordTypes.ItemActivity.ReceiveItemNonPOCorrection,
			(int)ViewRecordTypes.ItemActivity.ActualItemCorrection,
			(int)ViewRecordTypes.ItemActivity.ItemTransferToCorrection,

			(int)ViewRecordTypes.ItemActivity.ItemTransferFromCorrection,
			(int)ViewRecordTypes.ItemActivity.VoidedItemCountValue
		};
		private static Thinkage.Libraries.Translation.Key[] ItemActivityLabels = new Thinkage.Libraries.Translation.Key[] {
			KB.K("Not Specified"),
			KB.K("Physical Count"),
			KB.K("Void Physical Count"),
			KB.K("Item Adjustment"),
			KB.K("Item Issue"),
			KB.K("Item Transfer To"),
			KB.K("Item Transfer From"),
			KB.K("Receive Item With PO"),
			KB.K("Receive Item"),
			KB.K("Actual Item"),
			KB.K("Correction of ItemIssue"),
			KB.K("Correction of Receive Item With PO"),
			KB.K("Correction of Receive Item"),
			KB.K("Correction of Actual Item"),
			KB.K("Correction of Item Transfer To"),
			KB.K("Correction of Item Transfer From"),
			KB.K("Void Physical Count")
		};
		public static EnumValueTextRepresentations ItemActivityProvider = new EnumValueTextRepresentations(ItemActivityLabels, null, ItemActivityValues);
		#endregion
		public string ItemActivityType {
			get {
				return ItemActivityProvider.GetLabel(this.TableEnum, Thinkage.MainBoss.Database.dsMB.Schema.T.ItemActivity.F.TableEnum.EffectiveType).Translate();
			}
		}
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