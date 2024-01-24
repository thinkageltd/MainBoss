using System.Collections.Generic;
using System.Web.Mvc;
using Thinkage.Libraries;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess;

namespace ItemCountValueEntities {

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

		public string ItemActivityType{
			get {
				return ItemActivityProvider.GetLabel(this.TableEnum, Thinkage.MainBoss.Database.dsMB.Schema.T.ItemActivity.F.TableEnum.EffectiveType).Translate();
			}
		}
	}
}