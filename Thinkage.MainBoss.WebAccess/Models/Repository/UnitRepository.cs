using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries.GIS;
using Thinkage.MainBoss.WebAccess.Models.interfaces;

namespace Thinkage.MainBoss.WebAccess.Models {
	public class UnitRepository : BaseRepository, IBaseRepository<UnitDataContext>, IView<UnitEntities.Unit> {
		#region Constructor and Base Support
		public UnitRepository()
			: base("Unit") {
		}
		#endregion
		#region IBaseRepository<UnitDataContext>
		public UnitDataContext DataContext {
			get;
			private set;
		}
		public override void InitializeDataContext() {
			DataContext = new UnitDataContext(Connection.ConnectionString);
		}
		#endregion

		#region Autocomplete
		/// <summary>
		/// Provide list of BaseLocation codes that contain the term the user has input.
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public List<KeyValuePair<Guid, string>> Autocomplete(string term, string pattern) {
			System.Text.RegularExpressions.Regex searchTerm = null;
			if (!String.IsNullOrEmpty(pattern)) {
				try {
					searchTerm = new System.Text.RegularExpressions.Regex(pattern);
				}
				catch (ArgumentException x) {
					Thinkage.Libraries.Exception.AddContext(x, new Thinkage.Libraries.MessageExceptionContext(KB.T("RequestCreateUnitCodePattern error")));
					throw;
				}
			}

			try {
				var termMatches = (from unit in DataContext.Unit
								   where unit.BaseRelativeLocation.Hidden == null && unit.BaseRelativeLocation.BaseLocation.Code.ToLower().Contains(term.ToLower())
								   select new KeyValuePair<Guid, string>(unit.BaseRelativeLocation.LocationID, unit.BaseRelativeLocation.BaseLocation.Code)).ToList();

				if (searchTerm == null)
					return termMatches;
				else
					return (from t in termMatches
							where searchTerm.Matches(t.Value).Count > 0
							select t).ToList();
			}
			catch (Exception) {
				return new List<KeyValuePair<Guid, string>>();
			}
		}
		#endregion
		#region IView
		/// <summary>
		/// Unit View
		/// </summary>
		/// <param name="Id">The UnitLocationID from the Request/WorkOrder</param>
		/// <returns></returns>
		public UnitEntities.Unit View(Guid Id) {
			try {
				var u = (from unit in DataContext.Unit
						 select unit).Single(ux => ux.BaseRelativeLocation.BaseLocation.Id == Id);
				SetGISLocationURL(u);
				return u;
			}
			catch (InvalidOperationException) {
				// construct a minimal model to permit the display of a message to the user that the record couldn't be found.
				return new UnitEntities.Unit() {
					Id = Id,
					BaseRelativeLocation = new UnitEntities.RelativeLocation() {
						Code = Thinkage.Libraries.Strings.Format(KB.K("The unit with Id '{0}' was not found."), Id),
						BaseLocation = new UnitEntities.Location() {
						}
					}
				};
			}
		}
		#endregion
		#region GIS Location
		public void SetGISLocationURL(UnitEntities.Unit u) {
			GeoGeography GISLocation = u.UnitGISLocation;
			if (GISLocation == null && u.BaseRelativeLocation.ContainingLocation != null) {
				// The current record contains no GIS Location. Fetch from the nearest outermost containing location that specifies a value.
				// TODO: The SetTop operation on SelectSpecification does not permit specification of a sort order to define topness. Once this exists
				// the second half of this #if can be used.
				var closestGISLocation = (from location in DataContext.Location
										  join container in DataContext.LocationContainment
										  on location.Id equals container.ContainingLocationID into containment
										  from c in containment
										  where location.GISLocation != null
										  && c.ContainedLocationID == u.BaseRelativeLocation.ContainingLocationID
										  orderby c.Depth ascending
										  select location.GISLocation).FirstOrDefault();
				GISLocation = UnitEntities.Unit.LinqSqlToGeoGeography(closestGISLocation);
			}
			string code = u.BaseRelativeLocation.BaseLocation.Code;
			var p2 = from location in DataContext.Location
					 from container in DataContext.LocationContainment
					 where container.ContainedLocationID == u.BaseRelativeLocation.LocationID
					 && container.ContainingLocationID == location.Id
					 select location.Id;

			var postalAddress = (from pa in DataContext.PostalAddress
								 where p2.Contains(pa.LocationID)
								 select pa).FirstOrDefault();
			if (postalAddress.Address1 == null
				&& postalAddress.Address2 == null
				&& postalAddress.City == null
				&& postalAddress.Territory == null
				&& postalAddress.Country == null
				&& postalAddress.PostalCode == null)
				postalAddress = null;


			if (GISLocation == null && postalAddress == null)
				u.UnitGISLocationURL = null;
			else
				u.UnitGISLocationURL = GoogleGISShowMap.Format(new GISMap(code, GISLocation, null,
					postalAddress?.Address1,
					postalAddress?.Address2,
					postalAddress?.City,
					postalAddress?.Territory,
					postalAddress?.Country,
					postalAddress?.PostalCode));
		}
		#endregion

	}
}