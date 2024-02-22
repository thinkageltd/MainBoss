using Thinkage.Libraries.GIS;
using Thinkage.Libraries.XAF.Database.Service;

namespace UnitEntities {
	/// <summary>
	/// Wrapper to enclose the WorkOrder and all associated records (e.g. StateHistory, Resources)
	/// </summary>
	public partial class Unit {
		#region GeoGeography conversions
		static FromDSType FromGeographyConverter;
		static ToDSType ToGeographyConverter;
		public static GeoGeography LinqSqlToGeoGeography(System.Data.Linq.Binary gislocation) {
			if (gislocation == null)
				return null;
			return (GeoGeography)FromGeographyConverter(gislocation.ToArray());
		}
		public static System.Data.Linq.Binary GeoGeographyToLinqSql(GeoGeography gislocation) {
			object result = ToGeographyConverter(gislocation);
			if (result == System.DBNull.Value)
				return null;
			else
				return new System.Data.Linq.Binary((byte[])result);
		}
		static Unit() {
			Thinkage.Libraries.XAF.Database.Service.MSSql.Server.GetConverters(Thinkage.MainBoss.Database.dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.GISLocation.ReferencedColumn.EffectiveType, out FromGeographyConverter, out ToGeographyConverter);
		}
		#endregion
		public GeoGeography UnitGISLocation {
			get {
				return LinqSqlToGeoGeography(this.BaseRelativeLocation.BaseLocation.GISLocation);
			}
			set {
				this.BaseRelativeLocation.BaseLocation.GISLocation = GeoGeographyToLinqSql(value);
			}
		}
		/// <summary>
		/// The URL that will allow a user to goto a MAP program to locate a unit.
		/// </summary>
		public string UnitGISLocationURL {
			get;
			set;
		}
	}
}