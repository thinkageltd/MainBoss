using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinkage.MainBoss.WebAccess.Models.Repository {
	public class ItemCountValueRepository : BaseRepository {
		public ItemCountValueRepository() : base("ItemCountValue") { }

		#region IBaseRepository<ItemCountValueContext>
		public override void InitializeDataContext() {
			DataContext = new ItemCountValueDataContext(Connection.ConnectionString);
		}
		public ItemCountValueDataContext DataContext {
			get;
			private set;
		}
		#endregion
		#region IBrowse<ItemCountValueEntities.PermanentItemLocation> Members
		/// <summary>
		///  Return just item locations that have external tags
		/// </summary>
		public IQueryable<ItemCountValueEntities.PermanentItemLocation> BrowseItemLocationsWithExternalTags() {

			var q = from itemlocation in DataContext.PermanentItemLocation
					where itemlocation.ExternalTag != null
					orderby itemlocation.ExternalTag
					select itemlocation;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
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
					orderby storeroom.Rank ascending
					select storeroom;
#if DEBUG
			System.Data.Common.DbCommand dc = DataContext.GetCommand(q);
			System.Diagnostics.Debug.WriteLine(Thinkage.Libraries.Strings.IFormat("\nCommand Text: \n{0}", dc.CommandText));
#endif
			return q;
		}
		public IQueryable<ItemCountValueEntities.PermanentItemLocation> BrowseItemLocationsByStoreroom(Guid storeroomLocationID) {
			return (from itemlocation in DataContext.PermanentItemLocation
					where itemlocation.BaseActualItemLocation.BaseItemLocation.LocationID == storeroomLocationID
					select itemlocation);
		}
		#endregion
	}
}