using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.GIS;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	public class ShowOnMapCommand : SettableDisablerProperties, ICommand {
		// TODO: This fails on a Default table, although the operation still sort of makes sense, since it can be based on the default GISLocation and/or the default containing location etc.
		// However, if logic.TInfo is a default table, all our other paths must start with the default table instead of the normal one.
		private delegate DBI_Path pathMapper(DBI_Path p);
		private delegate Source sourceGetter(DBI_Path p);
		private ShowOnMapCommand(DBI_Table schema, sourceGetter getter, XAFClient db)
			: base(KB.K("Show this location on a map")) {
			DB = db;
			pathMapper mapper;
			DBI_Table mainTable;
			if (schema.IsDefaultTable) {
				mapper = delegate(DBI_Path p) {
					return p.DefaultPath;
				};
				mainTable = schema.Main;
			}
			else {
				mapper = delegate(DBI_Path p) {
					return p;
				};
				mainTable = schema;
			}
			ContainingLocationIDValueSource = dsMB.Schema.T.RelativeLocation.IsVariantIndirectBaseTableOf(mainTable)
				? getter(mapper(dsMB.Path.T.RelativeLocation.F.ContainingLocationID).ReOrientFromRelatedTable(schema))
				: null;

			LocationCodeValueSource = getter(mapper(dsMB.Path.T.Location.F.Code).ReOrientFromRelatedTable(schema));
			GISLocationValueSource = getter(mapper(dsMB.Path.T.Location.F.GISLocation).ReOrientFromRelatedTable(schema));
			if (mainTable == dsMB.Schema.T.PostalAddress) {
				Address1ValueSource = getter(mapper(dsMB.Path.T.PostalAddress.F.Address1));
				Address2ValueSource = getter(mapper(dsMB.Path.T.PostalAddress.F.Address2));
				CityValueSource = getter(mapper(dsMB.Path.T.PostalAddress.F.City));
				TerritoryValueSource = getter(mapper(dsMB.Path.T.PostalAddress.F.Territory));
				CountryValueSource = getter(mapper(dsMB.Path.T.PostalAddress.F.Country));
				PostalCodeValueSource = getter(mapper(dsMB.Path.T.PostalAddress.F.PostalCode));
			}
		}
		public ShowOnMapCommand(EditLogic logic)
			: this(logic.TInfo.Schema, (path) => logic.GetPathNotifyingSource(path, 0), logic.DB) {
		}
		public ShowOnMapCommand(BrowseLogic logic, int viewIndex)
			: this(logic.CompositeViews[viewIndex].PathToEditRecordRow, logic, viewIndex) {
		}
		private ShowOnMapCommand(DBI_PathToRow pathToEditRow, BrowseLogic logic, int viewIndex)
			: this(pathToEditRow.ReferencedTable,
				(path) => logic.GetTblPathDisplaySource(DBI_Path.NewFromAssuredPath(pathToEditRow, path), viewIndex),
				logic.DB) {
		}
		public void Execute() {
			// TODO (maybe): monitor changes on the LookupValueSource and therby enable or disable the command. THis is expensive to do because there is no cheap way to determine if the containing location
			// has geography information suitable for the visualizer. In the long run the visualizer should have a way of finding out this information.
			// THis also requires the ability to alter our Disabled Tip; this may need to expand SettableDisablerProperties into us or something.
			GeoGeography GISLocation = (GeoGeography)GISLocationValueSource.GetValue();
			if (GISLocation == null && ContainingLocationIDValueSource != null) {
				// The current record contains no GIS Location. Fetch from the nearest outermost containing location that specifies a value.
				// TODO: The SetTop operation on SelectSpecification does not permit specification of a sort order to define topness. Once this exists
				// the second half of this #if can be used.
#if !SetTopTakesSortOrder
				CommandBatchSpecification batch = new CommandBatchSpecification();
				NormalParameterSpecification param = batch.CreateNormalParameter(dsMB.Schema.T.Location.Id.EffectiveType);
				batch.Commands.Add(new Libraries.DBILibrary.MSSql.MSSqlLiteralCommandSpecification(Strings.IFormat(@"
							select top 1 L.GISLocation
								from Location as L
								join LocationContainment as LC on LC.ContainingLocationID = L.ID
								where L.GISLocation is not null
									and LC.ContainedLocationID = @{0}
								order by LC.Depth asc
							", param.Name)));
				param.Value = ContainingLocationIDValueSource.GetValue();
				GISLocation = (GeoGeography)dsMB.Schema.T.Location.F.GISLocation.EffectiveType.GenericAsNativeType(DB.Session.ExecuteCommandBatchReturningScalar(dsMB.Schema.T.Location.F.GISLocation.EffectiveType, batch), typeof(GeoGeography));
#else
				// Note that in this case we don't need the param or the batch, just a single SQL command.
				GISLocation = (GeoGeography)GISGeographyConverter(DB.Session.ExecuteCommandReturningScalar(new SelectSpecification(
					dsMB.Schema.T.LocationContainment,
					new SqlExpression[] { new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainingLocationID.F.GISLocation) },
					new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainingLocationID.F.GISLocation).IsNotNull()
						.And(new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainedLocationID).Eq(SqlExpression.Constant(ContainingLocationIDLookupValueSource.GetValue()))),
					null).SetTop(1, order by dsMB.Path.T.LocationContainment.F.Depth ascending)));
#endif
			}
			// This uses the compound code of the Unit record.
			// It could be argued that we should use the compound code of the Location that supplied the GISLocation.
			// It could also be argued that we should use the compound code of the closest-containing PlainRelativeLocation or PostalLocation.
			string code = (string)LocationCodeValueSource.GetValue();
			// Fetch information from the containing PostalAddress.
			string address1;
			string address2;
			string city;
			string territory;
			string country;
			string postalCode;
			if (Address1ValueSource != null) {
				// This handles the case where the current record *is* a PostalAddress record.
				address1 = (string)Address1ValueSource.GetValue();
				address2 = (string)Address2ValueSource.GetValue();
				city = (string)CityValueSource.GetValue();
				territory = (string)TerritoryValueSource.GetValue();
				country = (string)CountryValueSource.GetValue();
				postalCode = (string)PostalCodeValueSource.GetValue();
			}
			else
				using (XAFDataSet ds = XAFDataSet.New(dsMB.Schema, DB)) {
					object containingLocationID = ContainingLocationIDValueSource.GetValue();
					dsMB.PostalAddressRow postalInfo = null;
					if (containingLocationID != null)
						postalInfo = (dsMB.PostalAddressRow)DB.ViewAdditionalRow(ds, dsMB.Schema.T.PostalAddress,
							new SqlExpression(dsMB.Path.T.PostalAddress.F.LocationID)
								.In(new SelectSpecification(
									dsMB.Schema.T.LocationContainment,
									new SqlExpression[] { new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainingLocationID) },
									new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainingLocationID.F.PostalAddressID).IsNotNull()
										.And(new SqlExpression(dsMB.Path.T.LocationContainment.F.ContainedLocationID).Eq(SqlExpression.Constant(ContainingLocationIDValueSource.GetValue()))),
									null)));
					if (postalInfo != null) {
						address1 = postalInfo.F.Address1;
						address2 = postalInfo.F.Address2;
						city = postalInfo.F.City;
						territory = postalInfo.F.Territory;
						country = postalInfo.F.Country;
						postalCode = postalInfo.F.PostalCode;
					}
					else {
						// This can happen if the containing location cannot be found or is null (e.g. in New mode)
						address1 = null;
						address2 = null;
						city = null;
						territory = null;
						country = null;
						postalCode = null;
					}
				}
			// for purposes of letting user decide what is a 'valid' postal addrses for GoogleGIS, we only enforce the case where ALL the fields are empty; anything else, we send to Google to let it try.
			bool emptyPostalAddress = string.IsNullOrWhiteSpace(address1) && string.IsNullOrWhiteSpace(address2) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(territory) && string.IsNullOrWhiteSpace(country) && string.IsNullOrWhiteSpace(postalCode);
			// TODO: It should be up to the visualizer to throw a well-worded exception if the information is inadequate. (the ICommand execution logic will catch and display the exception properly)
			if (GISLocation == null && emptyPostalAddress)
				throw new GeneralException(KB.K("Show on Map does not have enough information; needs map coordinates or full postal address"));
			GoogleGISShowMap.Show(new GISMap(code, GISLocation, null, address1, address2, city, territory, country, postalCode));
		}
		public bool RunElevated {
			get { return false; }
		}
		private readonly Source ContainingLocationIDValueSource;
		private readonly Source LocationCodeValueSource;
		private readonly Source GISLocationValueSource;
		private readonly Source Address1ValueSource;
		private readonly Source Address2ValueSource;
		private readonly Source CityValueSource;
		private readonly Source TerritoryValueSource;
		private readonly Source CountryValueSource;
		private readonly Source PostalCodeValueSource;
		private readonly XAFClient DB;
	}
}
