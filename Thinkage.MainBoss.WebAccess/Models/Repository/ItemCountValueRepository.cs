using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinkage.MainBoss.WebAccess.Models.Repository {
	public class ItemCountValueRepository : BaseRepository {
		public ItemCountValueRepository() : base("ItemCountValue") { }

		#region AdjustmentCodePickList
		/// <summary>
		/// Return the available RequestPriorities
		/// </summary>
		public SelectListWithEmpty ItemAdjustmentPickList(Guid? defaultValue) {
			return PickListWithEmptyOption<ItemCountValueEntities.ItemAdjustmentCode>(
						from rp in DataContext.ItemAdjustmentCode
						where rp.Hidden == null
						orderby rp.Code
						select rp,
					defaultValue);
		}
		#endregion

		#region IBaseRepository<ItemCountValueContext>
		public override void InitializeDataContext() {
			DataContext = new ItemCountValueDataContext(Connection.ConnectionString);
		}
		public ItemCountValueDataContext DataContext {
			get;
			private set;
		}
		#endregion
		#region IBrowse<ItemCountValueEntities.Item> Members
		/// <summary>
		///  Return just item locations that have external tags
		/// </summary>
		public IQueryable<ItemCountValueEntities.Item> BrowseItems() {

			var q = from item in DataContext.Item
					where item.Hidden == null
					orderby item.Code
					select item;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
		}
		#endregion
		#region View
		/// <summary>
		/// Item View
		/// </summary>
		/// <param name="Id">The Id from the Item</param>
		/// <returns></returns>
		public ItemCountValueEntities.Item View(Guid Id) {
			try {
				var u = (from item in DataContext.Item
						 select item).Single(ux => ux.Id == Id);
				return u;
			}
			catch (InvalidOperationException) {
				// construct a minimal model to permit the display of a message to the user that the record couldn't be found.
				return new ItemCountValueEntities.Item() {
					Id = Id,
					Code = Thinkage.Libraries.Strings.Format(KB.K("The item with Id '{0}' was not found."), Id)
				};
			};
		}
		#endregion
		#region ViewByExternalTag
		public ItemCountValueEntities.PermanentItemLocation ViewByExternalTag(string tag) {
			try {
				return (from itemlocation in DataContext.PermanentItemLocation
						where itemlocation.ExternalTag == tag
						select itemlocation).Single(); // InvalidOperationException if record not found
			}
			catch (InvalidOperationException) {
				return new ItemCountValueEntities.PermanentItemLocation() {
					ExternalTag = Thinkage.Libraries.Strings.Format(KB.K("External tag '{0}' not found."), tag)
				};
			}
		}
		#endregion
		#region ViewByItemAssignmentId
		public ItemCountValueEntities.PermanentItemLocation ViewByItemAssignmentId(Guid id) {
			try {
				return (from itemlocation in DataContext.PermanentItemLocation
						where itemlocation.Id == id
						select itemlocation).Single(); // InvalidOperationException if record not found
			}
			catch (InvalidOperationException) {
				return new ItemCountValueEntities.PermanentItemLocation() {
					ExternalTag = Thinkage.Libraries.Strings.Format(KB.K("ItemAssignment '{0}' not found."), id)
				};
			}
		}
		#endregion
		#region IBrowse<ItemCountValueEntities.PermanentStorage> Members
		/// <summary>
		///  Return just item locations that have external tags
		/// </summary>
		public IQueryable<ItemCountValueEntities.PermanentStorage> BrowseStorerooms() {

			var q = from storeroom in DataContext.PermanentStorage
					where storeroom.BaseRelativeLocation.Hidden == null
					select storeroom;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
		}
		public IQueryable<ItemCountValueEntities.PermanentItemLocation> BrowseItemLocationsByStoreroom(Guid storeroomLocationID) {
			return (from itemlocation in DataContext.PermanentItemLocation
					where itemlocation.BaseActualItemLocation.BaseItemLocation.LocationID == storeroomLocationID && itemlocation.BaseActualItemLocation.BaseItemLocation.Item.Hidden == null && itemlocation.BaseActualItemLocation.BaseItemLocation.Hidden == null
					select itemlocation);
		}

		public IEnumerable<ItemCountValueEntities.ItemPhysicalCount> BrowseItemPhysicalCountByStoreroom(Guid storeroomLocationID) {
			DateTime now = DateTime.Now;
			return (from itemlocation in DataContext.PermanentItemLocation
					join ItemPricing in DataContext.ItemPrice on itemlocation.BaseActualItemLocation.BaseItemLocation.ItemPriceID equals ItemPricing.Id into ugj2
					join ICV in DataContext.ItemCountValue on itemlocation.CurrentItemCountValueID equals ICV.Id
					join AT in DataContext.AccountingTransaction on ICV.AccountingTransactionID equals AT.Id
					from IP in ugj2.DefaultIfEmpty()
					where itemlocation.BaseActualItemLocation.BaseItemLocation.LocationID == storeroomLocationID && itemlocation.BaseActualItemLocation.BaseItemLocation.Item.Hidden == null && itemlocation.BaseActualItemLocation.BaseItemLocation.Hidden == null
					select new ItemCountValueEntities.ItemPhysicalCount() {
						ItemCode = itemlocation.BaseActualItemLocation.BaseItemLocation.Item.Code,
						ItemDesc = itemlocation.BaseActualItemLocation.BaseItemLocation.Item.Desc,
						ItemPricingID = IP.Id,
						StorageAssignmentID = itemlocation.Id,
						AdjustmentCodeID = null,
						DaysSinceLastPhysicalCount = AT == null ? 0 : (now - AT.EffectiveDate).Days,
						TotalCost = itemlocation.BaseActualItemLocation.TotalCost,
						UnitCost = itemlocation.BaseActualItemLocation.UnitCost,
						OnHand = itemlocation.BaseActualItemLocation.OnHand
					});
		}
		public IEnumerable<ItemCountValueEntities.PermanentStorage> BrowseStoreroomsSortedByLocation() {
			List<ItemCountValueEntities.PermanentStorage> storerooms = BrowseStorerooms().ToList();
			storerooms.Sort((x, y) => { return String.Compare(SortLocation(x.BaseRelativeLocation.BaseLocation.Code), SortLocation(y.BaseRelativeLocation.BaseLocation.Code)); });
			foreach (var x in storerooms)
				yield return x;
		}

		/// <summary>
		/// Sort on the compount location code; based on the report code in visual basic of the same name
		/// </summary>
		/// <param name="As"></param>
		/// <param name=""></param>
		/// <returns></returns>
		private string SortLocation(string l) {
			if (String.IsNullOrEmpty(l))
				return null;
			var a = l.Split('@');
			System.Text.StringBuilder ret = new System.Text.StringBuilder();
			for (int i = a.Length; --i > 0;) {
				ret.Append(a[i].Trim() + "@");
			}
			ret.Append(a[0]);
			return ret.ToString();
		}
		#endregion
	}
}