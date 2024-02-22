//#define NEW_REPORTS
//#define TRANSITION_REPORTS
using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.RDL2016;
using Thinkage.Libraries.RDLReports;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	#region CodeDescReportTbl
	public class CodeDescReportTbl : Tbl {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Signature fixed for creation of object by reflection")]
		public CodeDescReportTbl(DBI_Table schema,
			Tbl.TblIdentification identification,
			Tbl.IAttr[] attributes,
			TblLayoutNodeArray columns,
			params TblActionNode[] otherNodes)
			: base(schema, identification, CodeDescAttributes(attributes), CodeDescReportTbl.Layout(schema)) {
		}
		private static TblLayoutNodeArray Layout(DBI_Table schema) {
			return TIReports.CodeDescColumnBuilder.New(schema.InternalId.PathToContainingRow, TIReports.ShowAll).LayoutArray();
		}
		private static T GetAttr<T>(Tbl.IAttr[] attributes) where T : class, Tbl.IAttr {
			foreach (Tbl.IAttr a in attributes) {
				if (a is T result)
					return result;
			}
			return null;
		}
		private static Tbl.IAttr[] CodeDescAttributes(Tbl.IAttr[] attributes) {
			Tbl.IAttr featureGroup = GetAttr<FeatureGroup>(attributes);
			List<Tbl.IAttr> newAttrs = new List<Tbl.IAttr> {
				TIReports.TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			};
			if (featureGroup != null)
				newAttrs.Add(featureGroup);
			return newAttrs.ToArray();
		}
	}
	#endregion
	public static class FieldSelectMethods {
		// These are implemented as extension methods so they work even if the object they are called on is null. In such cases, the
		// Contains predicate will return false which is the effect we want anyway.
		public static DefaultShowInDetailsCol ShowIfOneOf(this TIReports.FieldSelect effect, params TIReports.FieldSelect[] others) {
			return others.Contains(effect) ? TIReports.FieldSelect.ShowAlways : TIReports.FieldSelect.ShowNever;
		}
		public static TIReports.FieldSelect XIDIfOneOf(this TIReports.FieldSelect effect, params TIReports.FieldSelect[] others) {
			return others.Contains(effect) ? TIReports.ShowXID : null;
		}
	}
	/// <summary>
	/// Define Tbl's for custom report specifications. These Tbl's are not registered in TblRegistry
	/// but are free standing Tbl entries for describing report layouts, and report parameters as required.
	/// </summary>
	public class TIReports : TIGeneralMB3 {
		const string FormReportCaption = "Print {0}";
		const string FormReportTitle = "{0} Default Report Title";
		static readonly Key StateHistoryKey = KB.K("State History");
		#region DateTime Precision Reduction Helper
		static TblLeafNode DateHHMMColumnNode(DBI_Path p, params TblLayoutNode.ICtorArg[] attrs) {
			List<TblLayoutNode.ICtorArg> newAttrs = new List<TblLayoutNode.ICtorArg>(attrs) {
				RDLReport.DateHHMMFmt
			};
			return TblColumnNode.New(p, newAttrs.ToArray());
		}
		#endregion
		#region SameKeyContextAs
		public sealed class SameKeyContextAs {
			private readonly Key calculatedContext;
			public SameKeyContextAs(Key baseKey) {
				XKey.SplitContext(baseKey, out calculatedContext);
			}
			public Key K(Key k) {
				return XKey.New(k, calculatedContext);
			}
			public static Key K(Key k, Key baseKey) {
				return new SameKeyContextAs(baseKey).K(k);
			}
			public static Key K(Key k, DBI_Path p) {
				return new SameKeyContextAs(p.Key()).K(k);
			}
			public static Key K(Key k, DBI_PathToRow row) {
				return new SameKeyContextAs(row.ReferencedTable.LabelKey).K(k);
			}
		}
		#endregion
		#region ColumnBuilder
		// All FieldSelect objects are created here so they exist for testing later
		public static readonly FieldSelect ShowXID = new FieldSelect(); // do FieldSelectMethods can see it
		static readonly FieldSelect ShowXIDAndCost = new FieldSelect();
		static readonly FieldSelect ShowXIDBold = new FieldSelect(); // limited support in column builders at present...
		public static readonly FieldSelect ShowAll = new FieldSelect(); // CodeDescReport needs visibility
																		// TODO: The remainder of these should have comments specifying which XxxxColumns method(s) understand them.
		static readonly FieldSelect ShowXIDAndUOM = new FieldSelect();
		static readonly FieldSelect ShowXIDAndOnHand = new FieldSelect();
		static readonly FieldSelect ShowXIDAndCurrentStateHistoryState = new FieldSelect();
		static readonly FieldSelect ShowSalesContactBusPhoneEmail = new FieldSelect();
		static readonly FieldSelect ShowServiceContactBusPhoneEmail = new FieldSelect();
		static readonly FieldSelect ShowPayablesContactBusPhoneEmail = new FieldSelect();
		static readonly FieldSelect ShowContactBusPhoneEmail = new FieldSelect();
		static readonly FieldSelect ShowStateHistory = new FieldSelect();
		static readonly FieldSelect ShowWorkOrderAndUnitAddress = new FieldSelect();
		static readonly FieldSelect ShowUnitAndUnitAddress = new FieldSelect();
		static readonly FieldSelect ShowUnitAndContainingUnitAndUnitAddress = new FieldSelect();
		static readonly FieldSelect ShowUnitAndContainingUnit = new FieldSelect();
		static readonly FieldSelect ShowAvailable = new FieldSelect();

		// TODO: The following should be renamed in terms of what they actually DO rather than "what they're for"
		static readonly FieldSelect ShowForVendor = new FieldSelect();
		static readonly FieldSelect ShowForUnit = new FieldSelect();
		static readonly FieldSelect ShowForUnitSummary = new FieldSelect();
		static readonly FieldSelect ShowForContact = new FieldSelect();
		static readonly FieldSelect ShowForRequest = new FieldSelect();
		static readonly FieldSelect ShowForRequestSummary = new FieldSelect();
		static readonly FieldSelect ShowForChargeback = new FieldSelect();
		static readonly FieldSelect ShowForChargebackSummary = new FieldSelect();
		static readonly FieldSelect ShowForTask = new FieldSelect();
		static readonly FieldSelect ShowForTaskSummary = new FieldSelect();
		static readonly FieldSelect ShowForScheduledWorkOrderSummary = new FieldSelect();
		static readonly FieldSelect ShowForWorkOrder = new FieldSelect();
		static readonly FieldSelect ShowForWorkOrderSummary = new FieldSelect();
		static readonly FieldSelect ShowForOverdueWorkOrder = new FieldSelect();
		static readonly FieldSelect ShowForMaintenanceHistory = new FieldSelect();
		static readonly FieldSelect ShowForServiceContract = new FieldSelect();
		static readonly FieldSelect ShowForServiceContractSummary = new FieldSelect();
		static readonly FieldSelect ShowMaintenanceTiming = new FieldSelect();
		static readonly FieldSelect ShowForItem = new FieldSelect();
		static readonly FieldSelect ShowReading = new FieldSelect();
		static readonly FieldSelect ShowForPurchaseOrder = new FieldSelect();
		static readonly FieldSelect ShowForPurchaseOrderSummary = new FieldSelect();
		static readonly FieldSelect ShowForItemPrice = new FieldSelect();

		public sealed class FieldSelect {
			public FieldSelect() {
			}
			public static readonly DefaultShowInDetailsCol ShowNever = DefaultShowInDetailsCol.Hide();
			public static readonly DefaultShowInDetailsCol ShowAlways = DefaultShowInDetailsCol.Show();
			//public DefaultShowInDetailsCol ShowIfOneOf(params FieldSelect[] others) {
			//return others.Contains(this) ? ShowAlways : ShowNever;
			//}
			//public FieldSelect XIDIfOneOf(params FieldSelect[] others) {
			//return others.Contains(this) ? ShowXID : null;
			//}
		}

		public class ColumnBuilder {
			protected readonly DBI_PathToRow RowPath;
			private readonly List<TblLayoutNode> Nodes = new List<TblLayoutNode>();
			protected ColumnBuilder(DBI_PathToRow rowPath) {
				RowPath = rowPath;
			}
			public static ColumnBuilder New(DBI_PathToRow rowPath, params object[] nodes) {
				return new ColumnBuilder(rowPath).Concat(nodes);
			}
			public ColumnBuilder Concat(params object[] nodes) {
				foreach (object node in nodes) {
					if (node is TblLayoutNode)
						Nodes.Add(node as TblLayoutNode);
					else if (node is TblLayoutNode[])
						Concat(node as TblLayoutNode[]);
					else if (node is ColumnBuilder)
						Concat((node as ColumnBuilder).Nodes.ToArray());
					else if (node != null)
						System.Diagnostics.Debug.Assert(false, "Unknown node type passed to Concat in ColumnBuilder");
				}
				return this;
			}
			public ColumnBuilder ColumnSection(object id, params object[] nodes) {
				return Concat(TblSectionNode.New(new TblLayoutNode.ICtorArg[] { Fmt.SetId(id) }, ColumnBuilder.New(RowPath, nodes).Nodes.ToArray()));
			}
			/// <summary>
			/// Enscapsulate a set of columns within a Group node.
			/// </summary>
			/// <param name="label"></param>
			/// <param name="attrs"></param>
			/// <param name="additions"></param>
			/// <returns></returns>
			public ColumnBuilder Group(Key label, TblLayoutNode.ICtorArg[] attrs, params object[] additions) {
				return Concat(TblGroupNode.New(label, attrs, New(RowPath).Concat(additions).Nodes.ToArray()));
			}
			public ColumnBuilder Group(Tbl.TblIdentification tid, TblLayoutNode.ICtorArg[] attrs, params object[] additions) {
				return Group(KB.TOi(tid), attrs, additions);
			}
			// TODO: Temporary to assist in conversion from xxxFilters
			public TblGroupNode AsTblGroup() {
				return AsTblGroup(RowPath.ReferencedTable.LabelKey);
			}
			public TblGroupNode AsTblGroup(Key label) {
				return TblGroupNode.New(label, NoAttr, Nodes.ToArray());
			}
			public TblLayoutNodeArray LayoutArray() {
				return new TblLayoutNodeArray(Nodes.ToArray());
			}
			public TblLayoutNode[] NodeArray() {
				return Nodes.ToArray();
			}
		}
		public class CodeDescColumnBuilder : ColumnBuilder {
			private CodeDescColumnBuilder(DBI_PathToRow pathToRow, FieldSelect effect = null, params TblLayoutNode.ICtorArg[] extraArgs)
				: base(pathToRow) {
				// yech!
				var xidAttrs = BuildArgList(extraArgs);
				xidAttrs.Add(effect.ShowIfOneOf(ShowXID, ShowXIDAndCost, ShowAll));
				var defaultAttrs = BuildArgList(extraArgs);
				defaultAttrs.Add(effect.ShowIfOneOf(ShowAll));

				Concat(
					TblColumnNode.New(new DBI_Path(pathToRow, pathToRow.ReferencedTable.Columns[KB.I("Code")]), xidAttrs.ToArray()),
					TblColumnNode.New(new DBI_Path(pathToRow, pathToRow.ReferencedTable.Columns[KB.I("Desc")]), defaultAttrs.ToArray()),
					TblColumnNode.New(new DBI_Path(pathToRow, pathToRow.ReferencedTable.Columns[KB.I("Comment")]), defaultAttrs.ToArray())
				);
			}
			public static CodeDescColumnBuilder New(DBI_Path rowLink, FieldSelect effect = null, params TblLayoutNode.ICtorArg[] extraArgs) {
				return New(rowLink.PathToReferencedRow, effect, extraArgs);
			}
			public static CodeDescColumnBuilder New(DBI_PathToRow row, FieldSelect effect = null, params TblLayoutNode.ICtorArg[] extraArgs) {
				return new CodeDescColumnBuilder(row, effect, extraArgs);
			}
		}
		#endregion
		#region Text Providers
		public static readonly EnumValueTextRepresentations IsCorrectionNames = new EnumValueTextRepresentations(new Key[] { KB.K(""), KB.K("Correction") }, null, new object[] { false, true });
		#endregion
		#region Common calculated values
		/// <summary>
		/// Generate a TblLayoutNode for use in a report which returns the value from the uomCodePath if longQuantityExpression evaluates to non-null,
		/// otherwise it returns "Hours" if timeQuantityExpression is a null reference or evaluates to a non-null value. Otherwise null is returned.
		/// </summary>
		/// <param name="longQuantityExpression">A value which, if evaluates to non-null, indicates the UOM path should be the result</param>
		/// <param name="timeQuantityExpression">A value which, if evaluates to non-null, indicated "Hours" should be the result. If this is null, "Hours" will be the default when longQuantityExpression evaluates to null.</param>
		/// <param name="uomCodePath"></param>
		/// <returns></returns>
		public static TblLeafNode UoMNodeForMixedQuantities(SqlExpression longQuantityExpression, SqlExpression timeQuantityExpression, DBI_Path uomCodePath, params TblLayoutNode.ICtorArg[] args) {
			SqlExpression selected = timeQuantityExpression == null
				? SqlExpression.Select(longQuantityExpression.IsNotNull(), new SqlExpression(uomCodePath), SqlExpression.Constant(KB.K("Hours").Translate()))
				: SqlExpression.Select(
					longQuantityExpression.IsNotNull(), new SqlExpression(uomCodePath),
					timeQuantityExpression.IsNotNull(), SqlExpression.Constant(KB.K("Hours").Translate()));
			return TblQueryValueNode.New(dsMB.Schema.T.UnitOfMeasure.LabelKey, new TblQueryExpression(selected), args);
		}
		public static TblLeafNode QuantityNodeForMixedQuantities(Key label, SqlExpression longQuantityExpression, SqlExpression timeQuantityExpression, params TblLeafNode.ICtorArg[] attribs) {
			// TODO: This decides what to format based on which value is non-null. If both are non-null, the long value is formatted.
			// This may require refinement???????
			var allAttribs = new List<TblLeafNode.ICtorArg>(attribs) {
				Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
			};
			TypeFormatter longFormatter = longQuantityExpression.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			TypeFormatter timeFormatter = timeQuantityExpression.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo resultType = new StringTypeInfo(1, Math.Max((int)longFormatter.SizingInformation.MaxWidth, (int)timeFormatter.SizingInformation.MaxWidth), 0, longQuantityExpression.ResultType.AllowNull || timeQuantityExpression.ResultType.AllowNull, true, true);
			return TblQueryValueNode.New(label, TblQueryCalculation.New(
					(values =>
						values[0] != null
							? longFormatter.Format(values[0])
							: values[1] != null
								? timeFormatter.Format(values[1])
								: null),
					resultType,
					new TblQueryExpression(longQuantityExpression),
					new TblQueryExpression(timeQuantityExpression)),
				allAttribs.ToArray()
			);
		}
		public static TblLeafNode QuantityNodeForMixedQuantities(Key label, DBI_Path longQuantityExpression, DBI_Path timeQuantityExpression, params TblLeafNode.ICtorArg[] attribs) {
			return QuantityNodeForMixedQuantities(label, new SqlExpression(longQuantityExpression), new SqlExpression(timeQuantityExpression), attribs);
		}
		/// <summary>
		/// UnitCost/Price for mixed UoM (integral and Time)
		/// </summary>
		/// <param name="label"></param>
		/// <param name="longQuantityExpression"></param>
		/// <param name="timeQuantityExpression"></param>
		/// <param name="costCalculation"></param>
		/// <param name="attribs"></param>
		/// <returns></returns>
		public static TblLeafNode UnitCostNodeForMixedValues(Key label, SqlExpression longQuantityExpression, SqlExpression timeQuantityExpression, TblQueryExpression costCalculation, params TblLeafNode.ICtorArg[] attribs) {
			// Client-side calculation of and formatting of the Unit Cost. We want different formatting for time-rates and unit-rates. Formatted text is fine because it never makes sense to total or otherwise summarize this.
			var unitCostFormatter = TIGeneralMB3.ItemUnitCostTypeOnServer.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			var hourlyCostFormatter = TIGeneralMB3.HourlyCostTypeOnServer.UnionCompatible(TIGeneralMB3.ItemUnitCostTypeOnServer).GetTypeFormatter(Application.InstanceFormatCultureInfo);
			var allAttribs = new List<TblLeafNode.ICtorArg>(attribs) {
				Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
			};

			return TblQueryValueNode.New(label, TblQueryCalculation.New(
				delegate (object[] values) {
					if (values[0] == null)
						// There is no cost to format
						return null;
					else if (values[1] == null) {
						var divisor = (long)Libraries.TypeInfo.IntegralTypeInfo.AsNativeType(values[2], typeof(long));
						if (divisor == 0)
							return null;
						// It is a numeric quantity
						return unitCostFormatter.Format(TIGeneralMB3.ItemUnitCostTypeOnServer.ClosestValueTo(Compute.Divide<long>((decimal)values[0], divisor)));
					}
					else {
						var divisor = (TimeSpan)values[1];
						if (divisor.TotalSeconds == 0)
							return null;
						// It is a time quantity, so the unit cost has only two decimal places, but we are using a TypeFormatter whose currency epsilon is the union of the unitCost and the hourlyCost types.
						// Maybe we can have two text runs, the first being the actual text, the second being the padding, which is somehow hidden by still takes up its space (text colour transparent??)
						// but we have no way of doing this at this level.
						// RDL cannot apply "decimal alignment" to text values (or even numeric ones, for that matter).
						return hourlyCostFormatter.Format(TIGeneralMB3.HourlyCostTypeOnServer.ClosestValueTo(Compute.Divide<TimeSpan>((decimal)values[0], (TimeSpan)values[1])));
					}
				},
				new Libraries.TypeInfo.StringTypeInfo(1, 100, 0, true, true, false),
				costCalculation, new TblQueryExpression(timeQuantityExpression), new TblQueryExpression(longQuantityExpression)
		), allAttribs.ToArray());
		}

		// We have to cast these to keep the value in range for Sql "intervals"
		// Calculate the difference between and original estimated date/time/interval and a subsequent revised date/time/interval
		// a positive result indicates (in terms of a variance) the interval took 'longer' than estimated. A negative result indicates the interval took 'less time' than estimated.
		public static TblQueryExpression IntervalDifferenceQueryExpression(DBI_Path original, DBI_Path revised) {
			return IntervalDifferenceQueryExpression(new SqlExpression(original), new SqlExpression(revised));
		}
		public static TblQueryExpression IntervalDifferenceQueryExpression(SqlExpression original, SqlExpression revised) {
			return new TblQueryExpression(CommonExpressions.FixRange(revised.Minus(original)));
		}
		public static TblQueryCalculation LocationDetailExpression(DBI_PathToRow pathToLocationRow, bool includeCode = false) {
			var inputPaths = new List<DBI_Path>() {
				dsMB.Path.T.Location.F.Code,
				dsMB.Path.T.Location.F.Desc,
				dsMB.Path.T.Location.F.PostalAddressID.F.Address1,
				dsMB.Path.T.Location.F.PostalAddressID.F.Address2,
				dsMB.Path.T.Location.F.PostalAddressID.F.City,
				dsMB.Path.T.Location.F.PostalAddressID.F.Territory,
				dsMB.Path.T.Location.F.PostalAddressID.F.Country,
				dsMB.Path.T.Location.F.PostalAddressID.F.PostalCode,
				dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.Make,
				dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.Model,
				dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID.F.Serial
			};
			if (!includeCode)
				inputPaths.RemoveAt(0);

			// Note that both the maximum length and line count are overestimates because the PostalAddress and Unit fields cannot both be non-null at the same time.
			return
				TblQueryCalculation.New(
					values => values.Where(v => v != null).OfType<string>().Aggregate(new System.Text.StringBuilder(), (sb, v) => sb.AppendLine(v), sb => sb.Length == 0 ? null : sb.ToString().TrimEnd()),
					new Libraries.TypeInfo.StringTypeInfo(0, inputPaths.Select(p => 2 + (int)((StringTypeInfo)p.ReferencedType).SizeType.NativeMaxLimit(typeof(int))).Sum(), (uint)inputPaths.Count, true, true, true),
						inputPaths.Select(p => new TblQueryPath(new DBI_Path(pathToLocationRow, p))).ToArray()
				);
		}
		#region RecordTypeClassifierByLinkages
		/// <summary>
		/// Construct a SqlExpression.Select RecordTypeExpression based on DBI_Path linkages that provides EnumValueText Keys for each linkage
		/// </summary>
		public class RecordTypeClassifierByLinkages {
			public RecordTypeClassifierByLinkages(bool useDefaultForLastEntry, params Tuple<DBI_Path, Key>[] linkages) {
				var keys = new Key[linkages.Length];
				var indices = new object[linkages.Length];
				var selectExpressions = new SqlExpression[2 * linkages.Length - (useDefaultForLastEntry ? 1 : 0)];
				for (int i = linkages.Length; --i >= 0;) {
					keys[i] = linkages[i].Item2 ?? linkages[i].Item1.ReferencedColumn.LabelKey;

					indices[i] = i;
					int exprIndex = 2 * i;
					selectExpressions[exprIndex] = new SqlExpression(linkages[i].Item1).IsNotNull();
					if (++exprIndex == selectExpressions.Length)
						// The caller wants the last case to use the default so we decrement exprIndex again so the result value
						// will overwrite the last test condition.
						--exprIndex;
					selectExpressions[exprIndex] = SqlExpression.Constant(i);
				}
				EnumValueProvider = new EnumValueTextRepresentations(keys, null, indices);
				RecordTypeExpression = SqlExpression.Select(selectExpressions);
			}
			/// <summary>
			/// Construct a SqlExpression.Select RecordTypeExpression based on DBI_Path linkages where Keys are the DBI_Path referenced Column's LabelKey
			/// </summary>
			/// <param name="useDefaultForLastEntry"></param>
			/// <param name="linkagePaths"></param>
			public RecordTypeClassifierByLinkages(bool useDefaultForLastEntry, params DBI_Path[] linkagePaths)
				: this(useDefaultForLastEntry, linkagePaths.Select(p => new Tuple<DBI_Path, Key>(p, p.ReferencedColumn.LabelKey)).ToArray()) {
			}
			public readonly EnumValueTextRepresentations EnumValueProvider;
			public readonly SqlExpression RecordTypeExpression;
		}
		#endregion
		#endregion
		#region RTbl generators
		internal static RTbl TblDrivenReportRtbl(params RTblBase.ICtorArg[] controls) {
#if TRANSITION_REPORTS
			return new NewRTbl(controls);
#else
			return new RTbl(typeof(Libraries.RDLReports.TblDrivenReport), typeof(ReportViewerControl), controls);
#endif
		}
		internal static RTbl TblDrivenReportRTbl(System.Type viewerControlType, params RTblBase.ICtorArg[] controls) {
			return new RTbl(typeof(Libraries.RDLReports.TblDrivenReport), viewerControlType, controls);
		}

		#endregion
		private TIReports() {
		}
		#region TblReportColumnNode query-column-provider for barcode columns
		// This takes a path referring to text, and generates a Blob column whose value is an image of the bar code.
		internal static TblQueryCalculation ValueAsBarcode(DBI_Path textPath, bool includeCodeInDisplay = false) {
			return TblQueryCalculation.NewT(
				delegate (ReportViewLogic rl) {
					BarCode bcConverter = new BarCode((BarCodeSymbology)(rl.BarCodeSymbologyNodeInfo == null ? BarCodeSymbology.None : (rl.BarCodeSymbologyNodeInfo.CheckedControl.Value ?? BarCodeSymbology.None)), includeCodeInDisplay);
					return (inputs => bcConverter.ImageFromString((string)inputs[0]));
				},
				new BlobTypeInfo(0, int.MaxValue, true),
				new TblQueryPath(textPath)
				);
		}
		#endregion
		#region Builder helper functions
		private static TblActionNode ClearSelectForPrintCommand(DBI_Path selectForPrintFlagPath, DBI_Path reportLinkToRecord) {
			return new TblReportCustomAction(KB.K("Clear Select for Printing"), KB.K("Clear the select for printing flag on all records that match the current print filter"),
				delegate (ReportViewLogic rl) {
					SqlExpression clientFilter = rl.GetReportClientFilter();
					if (clientFilter.IsConstant(true)) {
						UpdateSpecification u = new UpdateSpecification(selectForPrintFlagPath.ReferencedTable,
							new DBI_Column[] { selectForPrintFlagPath.ReferencedColumn },
							new SqlExpression[] { SqlExpression.Constant(false) },
							new SqlExpression(selectForPrintFlagPath.ReferencedTable.InternalId)
								.In(new SelectSpecification(reportLinkToRecord.Table, new SqlExpression[] { new SqlExpression(reportLinkToRecord) }, rl.GetReportServerFilter(), null)));

						rl.DB.Session.ExecuteCommand(u);
						// TODO: Inform cache manager of updated records.
					}
					else {
						// TODO: Query the Id's of the root table using the server-side filter (essentially the contents of the IN subquery above) along with
						// all the server-side and client-side columns referenced by the client-side filter. Apply the client-side filter to the dataset.
						// Then convert the list of ID's into a changed dataset which we update back to the server.
						// For now we just hope there are no client-side filters on the reports that use this command.
						throw new NotImplementedException(KB.I("Unable to apply client-side filtering to Clear Select for Printing"));
					}
				});
		}
		#endregion
		private static readonly TblLayoutNode.ICtorArg[] NoAttr = Array.Empty<TblLayoutNode.ICtorArg>();

		// Done: Certain report fields and filters need to be individually group-controlled.
		// In particular all the information that appears on the Value and ServiceContract tabs of the Unit editor
		// should be in the UnitValueAndServiceGroup. This applies both to the Unit report itself and all the
		// unit information that appears in the Request-related reports. This includes all the following
		// Unit table fields and any information reached through them:
		//				PurchaseDate
		//				PurchaseVendorID
		//				OriginalCost
		//				ReplacementCostLastDate
		//				ReplacementCost
		//				TypicalLife
		//				ScrapDate
		//				ScrapValue
		//				UnitUsageID
		//				OwnershipID
		//				AssetCodeID
		//
		// Removing the filters just requires having ReportControl check IsVisible on each TblLayoutNode it processes.
		// Removing the report fields may require either conditional field insertions in the report template (with a conventional
		// name connecting the condition to a particular FeatureGroup), or the ability to apply FeatureGroups to TblReportGenericDetail entries
		// and have them honoured during report generation.
		#region Common User Filters in Reports
		static List<TblLayoutNode.ICtorArg> BuildArgList(params TblLayoutNode.ICtorArg[] extraArgs) {
			var otherArgs = new List<TblLayoutNode.ICtorArg>(Array.Empty<TblLayoutNode.ICtorArg>());
			otherArgs.AddRange(extraArgs);
			return otherArgs;
		}
		#region User Columns
		private static TblColumnNode ColumnNodeForUserContact(DBI_PathToRow startPath, DBI_Path endPath, params TblLayoutNode.ICtorArg[] attrs) {
			return TblColumnNode.New(VisibleReverseLinkagePathKey(startPath, endPath), new DBI_Path(startPath, endPath), attrs);
		}
		static ColumnBuilder UserColumnsForContact(dsMB.PathClass.PathToUserRow User, FieldSelect effect = null) {
			return ColumnBuilder.New(User,
				ColumnNodeForUserContact(User, dsMB.Path.T.User.F.AuthenticationCredential, effect.ShowIfOneOf(ShowAll, ShowXID))
				, ColumnNodeForUserContact(User, dsMB.Path.T.User.F.Desc, effect.ShowIfOneOf(ShowAll))
				, ColumnNodeForUserContact(User, dsMB.Path.T.User.F.Comment, effect.ShowIfOneOf(ShowAll))
			);
		}
		static ColumnBuilder UserColumns(dsMB.PathClass.PathToUserLink UserLink, FieldSelect effect = null) {
			return UserColumn(UserLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder UserColumn(dsMB.PathClass.PathToUserRow User, FieldSelect effect = null) {
			return ColumnBuilder.New(User,
				TblColumnNode.New(User.F.AuthenticationCredential, effect.ShowIfOneOf(ShowAll, ShowXID))
				, TblColumnNode.New(User.F.Desc, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(User.F.Comment, effect.ShowIfOneOf(ShowAll))
				, ContactColumns(User.F.ContactID, effect)
			);
		}
		#endregion
		#region Contact Columns

		static ColumnBuilder ContactColumns(dsMB.PathClass.PathToContactLink ContactLink, FieldSelect effect = null) {
			return ContactColumns(ContactLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder ContactColumns(dsMB.PathClass.PathToContactRow Contact, FieldSelect effect = null) {
			var codeArgList = BuildArgList();
			codeArgList.Add(new MapLabelCol(e => e.IsNull.Query(KB.K("No one"), e))); // TODO: This is not currently honored by anyone so No one never shows up
			codeArgList.Add(effect.ShowIfOneOf(ShowForContact, ShowXID, ShowXIDBold, ShowContactBusPhoneEmail));
			if (effect == ShowXIDBold)
				codeArgList.Add(Fmt.SetFontStyle(System.Drawing.FontStyle.Bold));

			return ColumnBuilder.New(Contact,
				TblColumnNode.New(Contact.F.Code, codeArgList.ToArray()),
				TblColumnNode.New(Contact.F.BusinessPhone, effect.ShowIfOneOf(ShowForContact, ShowContactBusPhoneEmail)),
				TblColumnNode.New(Contact.F.FaxPhone, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.HomePhone, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.PagerPhone, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.MobilePhone, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.Email, effect.ShowIfOneOf(ShowForContact, ShowContactBusPhoneEmail), Fmt.SetFontStyle(System.Drawing.FontStyle.Underline), Fmt.SetColor(System.Drawing.Color.Blue), new LabelFormattingArg(Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Near), Fmt.SetMonospace(false))),
				TblColumnNode.New(Contact.F.AlternateEmail, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.WebURL, FieldSelect.ShowNever),
				TblQueryValueNode.New(Contact.F.LocationID.Key(), LocationDetailExpression(Contact.F.LocationID.PathToReferencedRow), FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.Id.L.ContactReport.ContactID.F.ContactRelationship, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.PreferredLanguage, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.LDAPGuid, FieldSelect.ShowNever),
				TblColumnNode.New(Contact.F.Comment, FieldSelect.ShowNever)
			);
		}
		#endregion
		#region Request Columns
		#region RequestStateHistory Columns
		static ColumnBuilder RequestStateHistoryColumns(dsMB.PathClass.PathToRequestStateHistoryLink pathToLink, FieldSelect effect = null) {
			return RequestStateHistoryColumns(pathToLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder RequestStateHistoryColumns(dsMB.PathClass.PathToRequestStateHistoryRow pathToRow, FieldSelect effect = null) {
			return ColumnBuilder.New(pathToRow
				, DateHHMMColumnNode(pathToRow.F.EffectiveDate, effect.ShowIfOneOf(ShowStateHistory, ShowXID))
				, CodeDescColumnBuilder.New(pathToRow.F.RequestStateID, effect.XIDIfOneOf(ShowStateHistory, ShowXIDAndCurrentStateHistoryState))
				, CodeDescColumnBuilder.New(pathToRow.F.RequestStateHistoryStatusID, effect.XIDIfOneOf(ShowStateHistory))
				, TblColumnNode.New(pathToRow.F.PredictedCloseDate, effect.ShowIfOneOf(ShowAll))
				, DateHHMMColumnNode(pathToRow.F.EntryDate, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(pathToRow.F.Comment, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(pathToRow.F.CommentToRequestor, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowAll))
				, UserColumns(pathToRow.F.UserID)
				);
		}
		static ColumnBuilder RequestStateHistoryWithDurationColumns(dsMB.PathClass.PathToRequestStateHistoryRow pathToRow, FieldSelect effect = null) {
			return ColumnBuilder.New(pathToRow,
				RequestStateHistoryColumns(pathToRow, effect),
				TblColumnNode.New(SameKeyContextAs.K(KB.K("End Date"), pathToRow), pathToRow.F.Id.L.RequestStateHistory.PreviousRequestStateHistoryID.F.EffectiveDate, DefaultShowInDetailsCol.Hide(), RDLReport.DateOnlyFmt),
				TblQueryValueNode.New(SameKeyContextAs.K(KB.K("State Duration"), pathToRow), new TblQueryExpression(CommonExpressions.RequestStateHistoryDuration(pathToRow.F)), DefaultShowInDetailsCol.Show(), RDLReport.DHMFmt)
			);
		}

		#endregion
		static ColumnBuilder RequestColumns(dsMB.PathClass.PathToRequestLink RequestLink, dsMB.PathClass.PathToRequestExtrasRow requestReport, FieldSelect effect = null) {
			return RequestColumns(RequestLink.PathToReferencedRow, requestReport, effect);
		}
		static ColumnBuilder RequestColumns(dsMB.PathClass.PathToRequestRow Request, dsMB.PathClass.PathToRequestExtrasRow requestReport, FieldSelect effect = null) {
			FieldSelect contactEffect = null;
			FieldSelect unitEffect = null;
			if (effect == ShowForRequest) {
				contactEffect = ShowContactBusPhoneEmail;
				unitEffect = ShowUnitAndContainingUnitAndUnitAddress;
			}
			else if (effect == ShowForRequestSummary) {
				contactEffect = null;
				unitEffect = ShowXID;
			}
			return ColumnBuilder.New(Request,
					TblColumnNode.New(Request.F.Number, effect.ShowIfOneOf(ShowForRequestSummary, ShowXID)))
					.ColumnSection(StandardReport.LeftColumnFmtId
						, UnitColumns(Request.F.UnitLocationID, unitEffect)
						, RequestStateHistoryColumns(Request.F.CurrentRequestStateHistoryID)
						, TblColumnNode.New(requestReport.F.CreatedDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(requestReport.F.InProgressDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(requestReport.F.EndedDateIfEnded, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
					)
					.ColumnSection(StandardReport.RightColumnFmtId
						, ContactColumns(Request.F.RequestorID.F.ContactID, contactEffect)
						, CodeDescColumnBuilder.New(Request.F.AccessCodeID, effect.XIDIfOneOf(ShowForRequest))
						, CodeDescColumnBuilder.New(Request.F.RequestPriorityID, effect.XIDIfOneOf(ShowForRequest))
						, TblColumnNode.New(requestReport.F.Lifetime, RDLReport.DHMFmt, effect.ShowIfOneOf(ShowForRequest))
						, TblColumnNode.New(requestReport.F.InNewDuration, RDLReport.DHMFmt, effect.ShowIfOneOf(ShowForRequest))
						, TblColumnNode.New(requestReport.F.InProgressDuration, RDLReport.DHMFmt, effect.ShowIfOneOf(ShowForRequest))
						, TblColumnNode.New(requestReport.F.RequestAssigneesAsText, effect.ShowIfOneOf(ShowForRequest))
						, TblColumnNode.New(Request.F.Id.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders, FieldSelect.ShowNever)
						, TblColumnNode.New(Request.F.SelectPrintFlag, FieldSelect.ShowNever)
					)
					.ColumnSection(StandardReport.LVPRowsFmtId
					)
					.ColumnSection(StandardReport.MultiLineRowsFmtId
						, TblColumnNode.New(Request.F.Subject, Fmt.SetFontStyle(System.Drawing.FontStyle.Bold), effect.ShowIfOneOf(ShowForRequest, ShowForRequestSummary))
						, TblColumnNode.New(Request.F.Description, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowForRequest))
						, TblColumnNode.New(Request.F.Comment, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowForRequest))
					);
		}
		#endregion
		#region Meter Columns

		static ColumnBuilder MeterColumns(dsMB.PathClass.PathToMeterLink Meter, FieldSelect effect) {
			return MeterColumns(Meter.PathToReferencedRow, effect);
		}
		static ColumnBuilder MeterClassColumns(dsMB.PathClass.PathToMeterClassRow MeterClass, FieldSelect effect) {
			if (effect == ShowReading)
				effect = ShowXID;
			return CodeDescColumnBuilder.New(MeterClass, effect).Concat(CodeDescColumnBuilder.New(MeterClass.F.UnitOfMeasureID, effect));
		}
		static ColumnBuilder MeterColumns(dsMB.PathClass.PathToMeterRow Meter, FieldSelect effect) {
			return ColumnBuilder.New(Meter
					, MeterClassColumns(Meter.F.MeterClassID.PathToReferencedRow, ShowXID)
					, TblColumnNode.New(Meter.F.MeterReadingOffset)
					, UnitColumns(Meter.F.UnitLocationID, ShowXID)
					, TblColumnNode.New(Meter.F.Comment)
					, MeterReadingColumns(Meter.F.CurrentMeterReadingID.PathToReferencedRow, effect)
				);
		}
		static ColumnBuilder MeterReadingColumns(dsMB.PathClass.PathToMeterReadingRow MeterReading, FieldSelect effect) {
			return ColumnBuilder.New(MeterReading
					, TblColumnNode.New(MeterReading.F.WorkOrderID.F.Number)
					, DateHHMMColumnNode(MeterReading.F.EffectiveDate, effect.ShowIfOneOf(ShowAll, ShowReading))
					, DateHHMMColumnNode(MeterReading.F.EntryDate)
					, TblColumnNode.New(MeterReading.F.UserID.F.ContactID.F.Code, effect.ShowIfOneOf(ShowAll))
					, TblColumnNode.New(MeterReading.F.Reading, effect.ShowIfOneOf(ShowAll, ShowReading))
					, TblColumnNode.New(MeterReading.F.EffectiveReading, effect.ShowIfOneOf(ShowAll, ShowReading))
					, TblColumnNode.New(MeterReading.F.Comment, effect.ShowIfOneOf(ShowAll, ShowReading))
				);
		}
		#endregion
		#region Unit Columns

		static public ColumnBuilder UnitColumns(dsMB.PathClass.PathToLocationLink unitLink, FieldSelect effect = null) {
			return UnitColumns(unitLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder UnitColumns(dsMB.PathClass.PathToLocationRow unitLocation, FieldSelect effect = null) {
			var unit = unitLocation.F.RelativeLocationID.F.UnitID.F;
			var unitvalueFeatureArg = new FeatureGroupArg(UnitValueAndServiceGroup);
			SameKeyContextAs ourContext = new SameKeyContextAs(unitLocation.F.RelativeLocationID.F.ExternalTag.Key());
			return ColumnBuilder.New(unitLocation,
					  TblColumnNode.New(unitLocation.F.RelativeLocationID.F.Code, effect.ShowIfOneOf(ShowForUnit, ShowForUnitSummary, ShowAll, ShowXID, ShowUnitAndUnitAddress, ShowUnitAndContainingUnitAndUnitAddress, ShowUnitAndContainingUnit), effect == ShowForUnit ? Fmt.SetFontStyle(System.Drawing.FontStyle.Bold) : null)
					, TblColumnNode.New(unitLocation.F.RelativeLocationID.F.LocationID.F.Desc, effect.ShowIfOneOf(ShowForUnit, ShowForUnitSummary))
					, TblColumnNode.New(unitLocation.F.RelativeLocationID.F.ContainingLocationID.F.Code, effect.ShowIfOneOf(ShowForUnit, ShowAll,/* ShowXID*/ ShowUnitAndContainingUnitAndUnitAddress, ShowUnitAndContainingUnit), new MapSortCol(Statics.LocationSort))
					, TblColumnNode.New(unitLocation.F.RelativeLocationID.F.UnitID.L.UnitReport.Id.F.UnitPostalAddress, effect.ShowIfOneOf(ShowForUnit, ShowUnitAndUnitAddress, ShowUnitAndContainingUnitAndUnitAddress))
					, TblColumnNode.New(unitLocation.F.RelativeLocationID.F.LocationID.F.Comment, effect.ShowIfOneOf(ShowForUnit))
					, TblClientTypeFormatterNode.New(unitLocation.F.RelativeLocationID.F.LocationID.F.GISLocation, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.Make, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.Model, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.Serial, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.Drawing, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unitLocation.F.RelativeLocationID.F.ExternalTag, effect.ShowIfOneOf(ShowForUnit))
					, TblQueryValueNode.New(ourContext.K(KB.K("External Tag Bar Code")), ValueAsBarcode(unitLocation.F.RelativeLocationID.F.ExternalTag), Fmt.SetUsage(DBI_Value.UsageType.Image), FieldSelect.ShowNever)
					, WorkOrderExpenseModelColumns(unit.WorkOrderExpenseModelID, effect.XIDIfOneOf(ShowForUnit), showEntry: false)
					, CodeDescColumnBuilder.New(unit.UnitCategoryID, effect.XIDIfOneOf(ShowForUnit, ShowForUnitSummary))
					, CodeDescColumnBuilder.New(unit.UnitUsageID, effect.XIDIfOneOf(ShowForUnit, ShowForUnitSummary))
					, CodeDescColumnBuilder.New(unit.SystemCodeID, effect.XIDIfOneOf(ShowForUnit, ShowForUnitSummary))
					, CodeDescColumnBuilder.New(unit.AccessCodeID, effect.XIDIfOneOf(ShowForUnit))
				).Group(TId.UnitValue, new TblLayoutNode.ICtorArg[] { unitvalueFeatureArg }
					, CodeDescColumnBuilder.New(unit.OwnershipID, effect.XIDIfOneOf(ShowForUnit))
					, CodeDescColumnBuilder.New(unit.AssetCodeID, effect.XIDIfOneOf(ShowForUnit))
					, VendorColumns(unit.PurchaseVendorID, effect.XIDIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.PurchaseDate, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.OriginalCost, FooterAggregateCol.Sum(), effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.ReplacementCostLastDate, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.ReplacementCost, FooterAggregateCol.Sum(), effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.ScrapDate, effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.ScrapValue, FooterAggregateCol.Sum(), effect.ShowIfOneOf(ShowForUnit))
					, TblColumnNode.New(unit.TypicalLife, effect.ShowIfOneOf(ShowForUnit))
				);
		}
		#endregion
		#region WorkOrder Columns/Groupings/Sorting
		#region WorkOrderStateHistory Columns
		static ColumnBuilder WorkOrderStateHistoryColumns(dsMB.PathClass.PathToWorkOrderStateHistoryLink pathToLink, FieldSelect effect = null) {
			return WorkOrderStateHistoryColumns(pathToLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder WorkOrderStateHistoryColumns(dsMB.PathClass.PathToWorkOrderStateHistoryRow pathToRow, FieldSelect effect = null) {
			return ColumnBuilder.New(pathToRow
				, DateHHMMColumnNode(pathToRow.F.EffectiveDate, effect.ShowIfOneOf(ShowStateHistory, ShowXID))
				, CodeDescColumnBuilder.New(pathToRow.F.WorkOrderStateID, effect.XIDIfOneOf(ShowStateHistory, ShowXIDAndCurrentStateHistoryState))
				, CodeDescColumnBuilder.New(pathToRow.F.WorkOrderStateHistoryStatusID, effect.XIDIfOneOf(ShowStateHistory))
				, DateHHMMColumnNode(pathToRow.F.EntryDate, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(pathToRow.F.Comment, effect.ShowIfOneOf(ShowAll))
				, UserColumns(pathToRow.F.UserID)
				);
		}
		static ColumnBuilder WorkOrderStateHistoryAndExtraColumns(dsMB.PathClass.PathToWorkOrderStateHistoryRow pathToRow) {
			return ColumnBuilder.New(pathToRow,
				WorkOrderStateHistoryColumns(pathToRow, ShowStateHistory),
				TblColumnNode.New(SameKeyContextAs.K(KB.K("End Date"), pathToRow), pathToRow.F.Id.L.WorkOrderStateHistory.PreviousWorkOrderStateHistoryID.F.EffectiveDate, DefaultShowInDetailsCol.Hide(), RDLReport.DateOnlyFmt),
				TblQueryValueNode.New(SameKeyContextAs.K(KB.K("State Duration"), pathToRow), new TblQueryExpression(CommonExpressions.WorkOrderStateHistoryDuration(pathToRow.F)), DefaultShowInDetailsCol.Show(), RDLReport.DHMFmt)
			);
		}

		#endregion

		public static EnumValueTextRepresentations IsDemandEnumText = EnumValueTextRepresentations.NewForBool(KB.K("Actual"), null, KB.K("Demand"), null);
		static ColumnBuilder WorkOrderColumns(dsMB.PathClass.PathToWorkOrderLink WO, FieldSelect effect = null, bool excludeUnits = false) {
			return WorkOrderColumns(WO.PathToReferencedRow, effect, excludeUnits);
		}
		static ColumnBuilder WorkOrderColumns(dsMB.PathClass.PathToWorkOrderRow WO, FieldSelect effect = null, bool excludeUnits = false) {
			SameKeyContextAs ourContext = new SameKeyContextAs(WO.F.Number.Key());
			dsMB.PathClass.PathToWorkOrderExtrasRow WOR = WO.F.Id.L.WorkOrderExtras.WorkOrderID;

			FieldSelect contactEffect = null;
			FieldSelect unitEffect = null;
			if (effect == ShowForWorkOrder) {
				contactEffect = ShowContactBusPhoneEmail;
				unitEffect = ShowUnitAndContainingUnitAndUnitAddress;
			}
			else if (effect == ShowWorkOrderAndUnitAddress) {
				contactEffect = ShowContactBusPhoneEmail;
				unitEffect = ShowUnitAndUnitAddress;
			}
			else if (effect == ShowForWorkOrderSummary || effect == ShowForMaintenanceHistory) {
				contactEffect = null;
				unitEffect = ShowXID;
			}
			return ColumnBuilder.New(WO,
					TblColumnNode.New(WO.F.Number, effect.ShowIfOneOf(ShowXID, ShowAll, ShowForWorkOrderSummary, ShowForMaintenanceHistory, ShowWorkOrderAndUnitAddress, ShowXIDAndCurrentStateHistoryState, ShowForOverdueWorkOrder)))
					.ColumnSection(StandardReport.LeftColumnFmtId
						, TIWorkOrder.IsPreventiveValueNodeBuilder(WO, effect.ShowIfOneOf(ShowForWorkOrder, ShowForMaintenanceHistory))
						, WorkOrderStateHistoryColumns(WO.F.CurrentWorkOrderStateHistoryID, effect == ShowXIDAndCurrentStateHistoryState ? ShowXIDAndCurrentStateHistoryState : null)
						, TblColumnNode.New(ourContext.K(KB.K("Created Date")), WO.F.FirstWorkOrderStateHistoryID.F.EffectiveDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)

						// Stats relating to Start (first open) date
						, TblColumnNode.New(WO.F.StartDateEstimate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K(KB.K("Expected Start")), WOStatisticCalculation.OnlyExpectedOpenDate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual Start"))), WOStatisticCalculation.ActualOpenDate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Expected Start Late"))), WOStatisticCalculation.ExpectedStartLate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual Start Late"))), WOStatisticCalculation.ActualStartLate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Expected Start Early"))), WOStatisticCalculation.ExpectedStartEarly(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual Start Early"))), WOStatisticCalculation.ActualStartEarly(WO), FieldSelect.ShowNever)

						// Stats related to Duration (completion - Start)
						, TblServerExprNode.New(ourContext.K((KB.K("Planned Duration"))), WOStatisticCalculation.PlannedDuration(WO), FooterAggregateCol.Sum(), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Expected Duration"))), WOStatisticCalculation.OnlyExpectedDuration(WO), FooterAggregateCol.Sum(), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual Duration"))), WOStatisticCalculation.ActualDuration(WO),  FooterAggregateCol.Sum(), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Expected Duration Variance"))), WOStatisticCalculation.OnlyExpectedDurationVariance(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual Duration Variance"))), WOStatisticCalculation.ActualDurationVariance(WO), FieldSelect.ShowNever)
						// For some reason Duration is an oddball insofar as we include the Merged value as well as Actual/Expected, but only for the duration itself,
						// not its variance.
						, TblServerExprNode.New(ourContext.K((KB.K("Duration"))), WOStatisticCalculation.MergedDuration(WO), FieldSelect.ShowNever)

						, TblColumnNode.New(WO.F.Downtime, FooterAggregateCol.Sum(), FooterAggregateCol.Average(), RDLReport.DHMFmt, FieldSelect.ShowNever)
						, CodeDescColumnBuilder.New(WO.F.WorkCategoryID, effect.XIDIfOneOf(ShowForWorkOrder))
						, CodeDescColumnBuilder.New(WO.F.AccessCodeID, effect.XIDIfOneOf(ShowForWorkOrder))
						, CodeDescColumnBuilder.New(WO.F.WorkOrderPriorityID, effect.XIDIfOneOf(ShowForWorkOrder))
						, WorkOrderExpenseModelColumns(WO.F.WorkOrderExpenseModelID, showEntry: false)
						, CodeDescColumnBuilder.New(WO.F.ProjectID, effect.XIDIfOneOf(ShowForWorkOrder))
						, CodeDescColumnBuilder.New(WO.F.CloseCodeID)
					)
					.ColumnSection(StandardReport.RightColumnFmtId
						, excludeUnits ? null : UnitColumns(WO.F.UnitLocationID, unitEffect)

						// Stats related to Completion
						// The following two values are mutually exclusive; if EndedDate exists, it will be displayed and the EarliestEndDate will be suppressed
						, TblColumnNode.New(WO.F.EndDateEstimate, FieldSelect.ShowNever)
						, TblColumnNode.New(WO.F.WorkDueDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K(KB.K("Actual End")), WOStatisticCalculation.ActualCompletedDate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K(KB.K("Expected End")), WOStatisticCalculation.OnlyExpectedCompletedDate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual End Late"))), WOStatisticCalculation.ActualEndLate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Expected End Late"))), WOStatisticCalculation.ExpectedEndLate(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Actual End Early"))), WOStatisticCalculation.ActualEndEarly(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((KB.K("Expected End Early"))), WOStatisticCalculation.ExpectedEndEarly(WO), FieldSelect.ShowNever)
						, TblServerExprNode.New(ourContext.K((OverdueLabelKey)), WOStatisticCalculation.MergedOverdue(WO), Fmt.SetColor(System.Drawing.Color.Red), effect.ShowIfOneOf(ShowForOverdueWorkOrder), Fmt.SetId(OverdueLabelKey))

						, TaskColumns(WOR.F.ScheduledWorkOrderID.F.WorkOrderTemplateID, effect.XIDIfOneOf(ShowForWorkOrder))
						, ScheduleColumns(WOR.F.ScheduledWorkOrderID.F.ScheduleID, effect.XIDIfOneOf(ShowForWorkOrder))
						, DateHHMMColumnNode(WOR.F.PMGenerationBatchID.F.EntryDate, FieldSelect.ShowNever)
						, DateHHMMColumnNode(WOR.F.PMGenerationBatchID.F.EndDate, FieldSelect.ShowNever)
						, ContactColumns(WO.F.RequestorID.F.ContactID, effect.XIDIfOneOf(ShowForWorkOrder))
						, TblColumnNode.New(WOR.F.WorkOrderAssigneesAsText, effect.ShowIfOneOf(ShowForWorkOrder))
						, TblColumnNode.New(WO.F.TotalActual, FooterAggregateCol.Sum(), FooterAggregateCol.Average(), FieldSelect.ShowNever)
						, TblColumnNode.New(WO.F.TotalDemand, FooterAggregateCol.Sum(), FooterAggregateCol.Average(), FieldSelect.ShowNever)
						, TblColumnNode.New(WO.F.SelectPrintFlag, FieldSelect.ShowNever)
						// , TblColumnNode.New(WO.F.PMGenerationBatchID.F) -- We can't show the schedule date because it is not unique. Several schedule dates might be deferred to the same work start date and thus share one WO.F.
						, TblColumnNode.New(WO.F.TemporaryStorageEmpty, FieldSelect.ShowNever)
					)
					.ColumnSection(StandardReport.LVPRowsFmtId
					)
					.ColumnSection(StandardReport.MultiLineRowsFmtId
						, TblColumnNode.New(WO.F.Subject, effect.ShowIfOneOf(ShowForWorkOrderSummary, ShowForMaintenanceHistory, ShowForWorkOrder, ShowForOverdueWorkOrder))
						, TblColumnNode.New(WO.F.ClosingComment, effect.ShowIfOneOf(ShowForWorkOrder))
						, TblColumnNode.New(WO.F.Description, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowForWorkOrder))
						, TblColumnNode.New(WO.F.UnitLocationID.F.RelativeLocationID.F.UnitID.L.UnitReport.Id.F.UnitSpecification, Fmt.SetMonospace(true), FieldSelect.ShowNever)
						, TblColumnNode.New(WO.F.UnitLocationID.F.RelativeLocationID.F.UnitID.L.UnitReport.Id.F.UnitAttachment, Fmt.SetMonospace(true), FieldSelect.ShowNever)
					);
		}
		static ColumnBuilder ForecastWorkOrderColumns(dsMB.PathClass.PathToExistingAndForecastResourcesRow WO, FieldSelect effect) {
			return ColumnBuilder.New(WO
				, TblColumnNode.New(WO.F.Code, effect.ShowIfOneOf(ShowXID, ShowAll))
				, CodeDescColumnBuilder.New(WO.F.WorkOrderPriorityID)
				, CodeDescColumnBuilder.New(WO.F.AccessCodeID)
				, CodeDescColumnBuilder.New(WO.F.WorkCategoryID)
				, CodeDescColumnBuilder.New(WO.F.ProjectID)
				, CodeDescColumnBuilder.New(WO.F.CloseCodeID)
				, TblColumnNode.New(WO.F.ClosingComment)
				, WorkOrderExpenseModelColumns(WO.F.WorkOrderExpenseModelID, showEntry: false)
				, TblColumnNode.New(WO.F.StartDateEstimate, effect.ShowIfOneOf(ShowXID))
				, TblColumnNode.New(WO.F.EndDateEstimate)
				, TblColumnNode.New(WO.F.Subject)
				, TblColumnNode.New(WO.F.Description)
				, TblColumnNode.New(WO.F.Downtime)
				);
		}
		#endregion
		#region Miscellaneous Columns
		static ColumnBuilder MiscellaneousColumns(dsMB.PathClass.PathToMiscellaneousLink MiscellaneousLink, FieldSelect effect = null) {
			return MiscellaneousColumns(MiscellaneousLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder MiscellaneousColumns(dsMB.PathClass.PathToMiscellaneousRow Miscellaneous, FieldSelect effect = null) {
			return ColumnBuilder.New(Miscellaneous
				, CodeDescColumnBuilder.New(Miscellaneous, effect)
				, TblColumnNode.New(Miscellaneous.F.PurchaseOrderText, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(Miscellaneous.F.Cost, effect.ShowIfOneOf(ShowAll))
				, CostCenterColumns(Miscellaneous.F.CostCenterID)
				);
		}
		#endregion
		#region PurchaseOrder Columns
		#region PurchaseOrderStateHistory Columns
		static ColumnBuilder PurchaseOrderStateHistoryColumns(dsMB.PathClass.PathToPurchaseOrderStateHistoryLink pathToLink, FieldSelect effect = null) {
			return PurchaseOrderStateHistoryColumns(pathToLink.PathToReferencedRow, effect);
		}
		static ColumnBuilder PurchaseOrderStateHistoryColumns(dsMB.PathClass.PathToPurchaseOrderStateHistoryRow pathToRow, FieldSelect effect = null) {
			return ColumnBuilder.New(pathToRow
				, DateHHMMColumnNode(pathToRow.F.EffectiveDate, effect.ShowIfOneOf(ShowStateHistory, ShowXID))
				, CodeDescColumnBuilder.New(pathToRow.F.PurchaseOrderStateID, effect.XIDIfOneOf(ShowStateHistory, ShowXIDAndCurrentStateHistoryState))
				, CodeDescColumnBuilder.New(pathToRow.F.PurchaseOrderStateHistoryStatusID, effect.XIDIfOneOf(ShowStateHistory))
				, DateHHMMColumnNode(pathToRow.F.EntryDate, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(pathToRow.F.Comment, effect.ShowIfOneOf(ShowAll))
				, UserColumns(pathToRow.F.UserID)
				);
		}
		static ColumnBuilder PurchaseOrderStateHistoryAndExtraColumns(dsMB.PathClass.PathToPurchaseOrderStateHistoryRow pathToRow, FieldSelect effect = null) {
			return ColumnBuilder.New(pathToRow,
				PurchaseOrderStateHistoryColumns(pathToRow, ShowStateHistory),
				TblColumnNode.New(SameKeyContextAs.K(KB.K("End Date"), pathToRow), pathToRow.F.Id.L.PurchaseOrderStateHistory.PreviousPurchaseOrderStateHistoryID.F.EffectiveDate, DefaultShowInDetailsCol.Hide(), RDLReport.DateOnlyFmt),
				TblQueryValueNode.New(SameKeyContextAs.K(KB.K("State Duration"), pathToRow), new TblQueryExpression(CommonExpressions.PurchaseOrderStateHistoryDuration(pathToRow.F)), DefaultShowInDetailsCol.Show(), RDLReport.DHMFmt)
			);
		}
		#endregion
		static ColumnBuilder PurchaseOrderColumns(dsMB.PathClass.PathToPurchaseOrderLink purchaseOrder, dsMB.PathClass.PathToPurchaseOrderExtrasRow purchaseOrderReport, dsMB.PathClass.PathToPurchaseOrderFormReportRow purchaseOrderFormReport = null, FieldSelect effect = null) {
			return PurchaseOrderColumns(purchaseOrder.PathToReferencedRow, purchaseOrderReport, purchaseOrderFormReport, effect);
		}
		static ColumnBuilder PurchaseOrderColumns(dsMB.PathClass.PathToPurchaseOrderRow purchaseOrder, dsMB.PathClass.PathToPurchaseOrderExtrasRow purchaseOrderReport, dsMB.PathClass.PathToPurchaseOrderFormReportRow purchaseOrderFormReport = null, FieldSelect effect = null) {
			FieldSelect vendorEffect = null;
			if (effect == ShowForPurchaseOrder) {
				vendorEffect = ShowSalesContactBusPhoneEmail;
			}
			else if (effect == ShowForPurchaseOrderSummary) {
				vendorEffect = ShowXID;
			}

			return ColumnBuilder.New(purchaseOrder
				, TblColumnNode.New(purchaseOrder.F.Number, effect.ShowIfOneOf(ShowXID, ShowAll, ShowForPurchaseOrderSummary)))
					.ColumnSection(StandardReport.LeftColumnFmtId
						, VendorColumns(purchaseOrder.F.VendorID, vendorEffect)
						, CodeDescColumnBuilder.New(purchaseOrder.F.PurchaseOrderCategoryID)
						, CodeDescColumnBuilder.New(purchaseOrder.F.PaymentTermID, effect.XIDIfOneOf(ShowForPurchaseOrder))
					)
					.ColumnSection(StandardReport.RightColumnFmtId
						, TblColumnNode.New(purchaseOrder.F.RequiredByDate, RDLReport.DateOnlyFmt, effect.ShowIfOneOf(ShowForPurchaseOrder))
						, PurchaseOrderStateHistoryColumns(purchaseOrder.F.CurrentPurchaseOrderStateHistoryID)
						, CodeDescColumnBuilder.New(purchaseOrder.F.ShippingModeID, effect.XIDIfOneOf(ShowForPurchaseOrder))
						, TblQueryValueNode.New(purchaseOrder.F.ShipToLocationID.Key(), LocationDetailExpression(purchaseOrder.F.ShipToLocationID.PathToReferencedRow, includeCode: true), Fmt.SetFontStyle(System.Drawing.FontStyle.Bold), effect.ShowIfOneOf(ShowForPurchaseOrder))
						, CodeDescColumnBuilder.New(purchaseOrder.F.ProjectID)
						, purchaseOrderFormReport != null ? ContactColumns(purchaseOrderFormReport.F.InvoiceContactID, effect == ShowForPurchaseOrder ? ShowContactBusPhoneEmail : ShowXID) : null
						, TblColumnNode.New(purchaseOrderReport.F.PurchaseOrderAssigneesAsText, effect.ShowIfOneOf(ShowForPurchaseOrder))
						, TblColumnNode.New(purchaseOrderReport.F.CreatedDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.IssuedDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.EndedDateIfEnded, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.EarliestIssuedDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.EarliestEndDate, RDLReport.DateOnlyFmt, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.InNewDuration, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.InIssuedDuration, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.Lifetime, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.CompletedNormally, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrderReport.F.VarianceInReceiveTime, FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrder.F.SelectPrintFlag, FieldSelect.ShowNever)
					)
					.ColumnSection(StandardReport.LVPRowsFmtId
					)
					.ColumnSection(StandardReport.MultiLineRowsFmtId
						, TblColumnNode.New(purchaseOrder.F.Subject, Fmt.SetFontStyle(System.Drawing.FontStyle.Bold), effect.ShowIfOneOf(ShowForPurchaseOrder, ShowForPurchaseOrderSummary))
						, TblColumnNode.New(purchaseOrder.F.CommentToVendor, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowForPurchaseOrder))
						, TblColumnNode.New(purchaseOrder.F.Comment, Fmt.SetMonospace(true), effect.ShowIfOneOf(ShowForPurchaseOrder))
					)
					.Concat(
						  TblColumnNode.New(purchaseOrder.F.TotalPurchase, FooterAggregateCol.Sum(), FieldSelect.ShowNever)
						, TblColumnNode.New(purchaseOrder.F.TotalReceive, FooterAggregateCol.Sum(), FieldSelect.ShowNever)
					);
		}

		#endregion
		#region ReceiptColumns
		static ColumnBuilder ReceiptColumns(dsMB.PathClass.PathToReceiptLink receipt, FieldSelect effect = null) {
			return ColumnBuilder.New(receipt.PathToReferencedRow
				, TblColumnNode.New(receipt.F.Waybill, effect.ShowIfOneOf(ShowXID, ShowAll))
				, DateHHMMColumnNode(receipt.F.EntryDate, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(receipt.F.Desc, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(receipt.F.Reference, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(receipt.F.Comment, effect.ShowIfOneOf(ShowAll))
			);
		}
		#endregion
		#region ItemIssueCodeColumns
		static ColumnBuilder ItemIssueCodeColumns(dsMB.PathClass.PathToItemIssueCodeLink ItemIssueCode, FieldSelect effect = null) {
			return ItemIssueCodeColumns(ItemIssueCode.PathToReferencedRow, effect);
		}
		static ColumnBuilder ItemIssueCodeColumns(dsMB.PathClass.PathToItemIssueCodeRow ItemIssueCode, FieldSelect effect = null) {
			return ColumnBuilder.New(ItemIssueCode
				, CodeDescColumnBuilder.New(ItemIssueCode, effect)
				, CostCenterColumns(ItemIssueCode.F.CostCenterID)
			);
		}
		#endregion
		#region ItemAdjustmentCodeColumns
		static ColumnBuilder ItemAdjustmentCodeColumns(dsMB.PathClass.PathToItemAdjustmentCodeLink ItemAdjustmentCode, FieldSelect effect = null) {
			return ItemAdjustmentCodeColumns(ItemAdjustmentCode.PathToReferencedRow, effect);
		}
		static ColumnBuilder ItemAdjustmentCodeColumns(dsMB.PathClass.PathToItemAdjustmentCodeRow ItemAdjustmentCode, FieldSelect effect = null) {
			return ColumnBuilder.New(ItemAdjustmentCode
				, CodeDescColumnBuilder.New(ItemAdjustmentCode, effect)
				, CostCenterColumns(ItemAdjustmentCode.F.CostCenterID)
			);
		}
		#endregion
		#region Item Columns
		static ColumnBuilder ItemColumns(dsMB.PathClass.PathToItemLink item, FieldSelect effect = null) {
			return ItemColumns(item.PathToReferencedRow, effect);
		}
		static ColumnBuilder ItemColumns(dsMB.PathClass.PathToItemRow item, FieldSelect effect = null) {
			return ColumnBuilder.New(item
					, CodeDescColumnBuilder.New(item, effect.XIDIfOneOf(ShowForItem, ShowXID, ShowXIDAndUOM))
					, CodeDescColumnBuilder.New(item.F.ItemCategoryID, effect.XIDIfOneOf(ShowForItem))
					, TblColumnNode.New(item.F.OnHand, new FeatureGroupArg(StoreroomGroup), effect.ShowIfOneOf(ShowForItem))
					, TblColumnNode.New(item.F.OnReserve, new FeatureGroupArg(StoreroomGroup), effect.ShowIfOneOf(ShowForItem))
					, TblColumnNode.New(item.F.OnOrder, new FeatureGroupArg(PurchasingAndInventoryGroup), effect.ShowIfOneOf(ShowForItem))
					, TblColumnNode.New(item.F.Available, new FeatureGroupArg(StoreroomOrItemResourcesGroup), effect.ShowIfOneOf(ShowAvailable, ShowForItem))
					, CodeDescColumnBuilder.New(item.F.UnitOfMeasureID, effect.XIDIfOneOf(ShowForItem, ShowXIDAndUOM, ShowAvailable))
					, TblColumnNode.New(item.F.UnitCost, new FeatureGroupArg(StoreroomOrItemResourcesGroup), effect.ShowIfOneOf(ShowAvailable, ShowForItem))
					, TblColumnNode.New(item.F.TotalCost, FooterAggregateCol.Sum(), effect.ShowIfOneOf(ShowForItem))
			);
		}
		// grouping is the grouping to use for the Code of the Location of the storage assignment.
		// NOTE: ShowXID does not include the Item even though it should. Currently callers to ActualItemLocationColumns tend to call ItemColumns themselves.
		static ColumnBuilder ActualItemLocationColumns(dsMB.PathClass.PathToActualItemLocationLink actualItemLocation, FieldSelect effect = null, DelayedCreateTbl locationPickerTbl = null) {
			return ActualItemLocationColumns(actualItemLocation.PathToReferencedRow, effect, locationPickerTbl);
		}
		static ColumnBuilder ActualItemLocationColumns(dsMB.PathClass.PathToActualItemLocationRow actualItemLocation, FieldSelect effect = null, DelayedCreateTbl locationPickerTbl = null) {
			return ColumnBuilder.New(actualItemLocation
				, ItemLocationColumns(actualItemLocation.F.ItemLocationID, effect, locationPickerTbl)
				, TblColumnNode.New(actualItemLocation.F.EffectiveMinimum, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(actualItemLocation.F.EffectiveMaximum, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(actualItemLocation.F.OnHand, effect.ShowIfOneOf(ShowAll, ShowXIDAndOnHand))
				, TblColumnNode.New(actualItemLocation.F.OnOrder, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(actualItemLocation.F.OnReserve, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(actualItemLocation.F.Available, effect.ShowIfOneOf(ShowAll))
				, TblColumnNode.New(actualItemLocation.F.UnitCost, effect.ShowIfOneOf(ShowAll, ShowXIDAndOnHand))
				, TblColumnNode.New(actualItemLocation.F.TotalCost, effect.ShowIfOneOf(ShowAll, ShowXIDAndOnHand), new FooterAggregateCol(Expression.Function.Sum))
			);
		}
		// NOTE: ShowXID does not include the Item even though it should. Currently callers to ItemLocationColumns tend to call ItemColumns themselves.
		static ColumnBuilder ItemLocationColumns(dsMB.PathClass.PathToItemLocationLink itemLocation, FieldSelect effect = null, DelayedCreateTbl locationPickerTbl = null) {
			return ColumnBuilder.New(itemLocation.PathToReferencedRow
				// ItemLocationID.F.LocationID has no PickFrom declared in the xafdb file, because the actual choices depend on the derived record type.
				// We want to name a picker here, so we do this by adding a report column node that cannot be used for grouping and cannot be shown on the report.
				// Although in theory there could be TemporaryLocation or TemplateTemporaryLocation attempting to pick these would be tantamount to selecting
				// a Work Order or Task and so it is not generally useful to do so.
				// Note that for this information to stick, it must appear before the longer path because the code that creates the search values does not merge fmt attributes
				// from both definitions.
				, TblColumnNode.New(itemLocation.F.LocationID, Fmt.SetPickFrom(locationPickerTbl ?? TILocations.AllStorageBrowseTblCreator), new AllowShowInDetailsChangeCol(false), DefaultShowInDetailsCol.Hide())  // Don't show the value at all.
				, TblColumnNode.New(itemLocation.F.LocationID.F.Code, effect.ShowIfOneOf(ShowXID), new MapSortCol(Statics.LocationSort))
				, ItemPriceColumns(itemLocation.F.ItemPriceID, effect)
			);
		}
		#endregion
		#region ItemPrice Columns
		static ColumnBuilder ItemPriceColumns(dsMB.PathClass.PathToItemPriceLink itemPriceID, FieldSelect effect = null) {
			return ItemPriceColumns(itemPriceID.PathToReferencedRow, effect);
		}
		static ColumnBuilder ItemPriceColumns(dsMB.PathClass.PathToItemPriceRow ItemPrice, FieldSelect effect = null) {
			return ColumnBuilder.New(ItemPrice
				, DateHHMMColumnNode(ItemPrice.F.EffectiveDate, effect.ShowIfOneOf(ShowForItemPrice))
				, VendorColumns(ItemPrice.F.VendorID, effect.XIDIfOneOf(ShowForItemPrice))
				, effect == ShowForItemPrice ? ItemColumns(ItemPrice.F.ItemID, ShowXID) : null // Only time we include the Item is for the ItemPrice diret usage. All others will put the item in there choice at main level
				, TblColumnNode.New(ItemPrice.F.PurchaseOrderText)
				, TblColumnNode.New(ItemPrice.F.Quantity, effect.ShowIfOneOf(ShowForItemPrice))
				, TblColumnNode.New(ItemPrice.F.UnitCost, effect.ShowIfOneOf(ShowForItemPrice))
				, TblColumnNode.New(ItemPrice.F.Cost, effect.ShowIfOneOf(ShowForItemPrice))
			);
		}
		#endregion
		#region Task Columns
		static ColumnBuilder TaskColumns(dsMB.PathClass.PathToWorkOrderTemplateLink workOrderTemplateID, FieldSelect effect = null) {
			return TaskColumns(workOrderTemplateID.PathToReferencedRow, effect);
		}

		static ColumnBuilder TaskColumns(dsMB.PathClass.PathToWorkOrderTemplateRow WorkOrderTemplate, FieldSelect effect = null) {
			return ColumnBuilder.New(WorkOrderTemplate)
				.ColumnSection(StandardReport.LeftColumnFmtId
					, TblColumnNode.New(WorkOrderTemplate.F.Code, effect.ShowIfOneOf(ShowXID, ShowForTask, ShowForTaskSummary, ShowForWorkOrderSummary))
					, TblColumnNode.New(WorkOrderTemplate.F.Desc, effect.ShowIfOneOf(ShowForTask))
					, CodeDescColumnBuilder.New(WorkOrderTemplate.F.WorkCategoryID, effect.XIDIfOneOf(ShowForTask))
					, CodeDescColumnBuilder.New(WorkOrderTemplate.F.AccessCodeID, effect.XIDIfOneOf(ShowForTask))
					, CodeDescColumnBuilder.New(WorkOrderTemplate.F.ProjectID, effect.XIDIfOneOf(ShowForTask))
					, CodeDescColumnBuilder.New(WorkOrderTemplate.F.WorkOrderPriorityID, effect.XIDIfOneOf(ShowForTask))
					, CodeDescColumnBuilder.New(WorkOrderTemplate.F.CloseCodeID, effect.XIDIfOneOf(ShowForTask))
					, WorkOrderExpenseModelColumns(WorkOrderTemplate.F.WorkOrderExpenseModelID, effect.XIDIfOneOf(ShowForTask), showEntry: false)
				)
				.ColumnSection(StandardReport.RightColumnFmtId
					, TblColumnNode.New(WorkOrderTemplate.F.ContainingWorkOrderTemplateID.F.Code, effect.ShowIfOneOf(ShowForTask))
					, TblColumnNode.New(WorkOrderTemplate.F.GenerateLeadTime, effect.ShowIfOneOf(ShowForTask))
					, TblColumnNode.New(SameKeyContextAs.K(KB.K("Max Generate Lead Time"), WorkOrderTemplate.F.GenerateLeadTime), WorkOrderTemplate.F.Id.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.GenerateLeadTime, effect.ShowIfOneOf(ShowForTask))
					, TblColumnNode.New(WorkOrderTemplate.F.Duration, effect.ShowIfOneOf(ShowForTask, ShowForTaskSummary))
					, TblColumnNode.New(SameKeyContextAs.K(KB.K("Total Duration"), WorkOrderTemplate.F.Duration), WorkOrderTemplate.F.Id.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.Duration, effect.ShowIfOneOf(ShowForTask, ShowForTaskSummary))
					, TblColumnNode.New(WorkOrderTemplate.F.Downtime, effect.ShowIfOneOf(ShowForTask))
					, TblColumnNode.New(WorkOrderTemplate.F.DemandCount, effect.ShowIfOneOf(ShowForTask))
					, TblColumnNode.New(WorkOrderTemplate.F.SelectPrintFlag, effect.ShowIfOneOf(ShowForTask))
				)
				.ColumnSection(StandardReport.MultiLineRowsFmtId
					, TblColumnNode.New(WorkOrderTemplate.F.Subject, Fmt.SetFontStyle(System.Drawing.FontStyle.Bold), effect.ShowIfOneOf(ShowForTask, ShowForTaskSummary))
					, TblColumnNode.New(WorkOrderTemplate.F.Description, effect.ShowIfOneOf(ShowForTask), Fmt.SetMonospace(true))
					, TblColumnNode.New(WorkOrderTemplate.F.ClosingComment, effect.ShowIfOneOf(ShowForTask), Fmt.SetMonospace(true))
					, TblColumnNode.New(WorkOrderTemplate.F.Comment, effect.ShowIfOneOf(ShowForTask), Fmt.SetMonospace(true))
				);
		}
		#endregion
		#region Vendor Columns
		static ColumnBuilder VendorColumns(dsMB.PathClass.PathToVendorLink Vendor, FieldSelect effect = null) {
			return VendorColumns(Vendor.PathToReferencedRow, effect);
		}
		static ColumnBuilder VendorColumns(dsMB.PathClass.PathToVendorRow Vendor, FieldSelect effect = null) {
			return ColumnBuilder.New(Vendor,
				  CodeDescColumnBuilder.New(Vendor, effect.XIDIfOneOf(ShowForVendor, ShowXID, ShowSalesContactBusPhoneEmail, ShowServiceContactBusPhoneEmail, ShowPayablesContactBusPhoneEmail))
				, CodeDescColumnBuilder.New(Vendor.F.VendorCategoryID, effect.XIDIfOneOf(ShowForVendor))
				, ContactColumns(Vendor.F.SalesContactID, effect == ShowSalesContactBusPhoneEmail ? ShowContactBusPhoneEmail : null)
				, TblColumnNode.New(Vendor.F.AccountNumber, effect.ShowIfOneOf(ShowForVendor))
				, ContactColumns(Vendor.F.ServiceContactID, effect == ShowServiceContactBusPhoneEmail ? ShowContactBusPhoneEmail : null)
				, ContactColumns(Vendor.F.PayablesContactID, effect == ShowPayablesContactBusPhoneEmail ? ShowContactBusPhoneEmail : null)
				, CodeDescColumnBuilder.New(Vendor.F.PaymentTermID, effect.XIDIfOneOf(ShowForVendor))
				, CostCenterColumns(Vendor.F.AccountsPayableCostCenterID)
			);
		}
		#endregion
		#region AccountingTransaction Columns
		static ColumnBuilder AccountingTransactionColumns(dsMB.PathClass.PathToAccountingTransactionLink AccountingTransaction, FieldSelect effect = null, TblLayoutNode QuantityToShow = null) {
			return ColumnBuilder.New(AccountingTransaction.PathToReferencedRow
					, DateHHMMColumnNode(AccountingTransaction.F.EntryDate, effect.ShowIfOneOf(ShowAll))
					, DateHHMMColumnNode(AccountingTransaction.F.EffectiveDate, effect.ShowIfOneOf(ShowXID, ShowAll))
					, QuantityToShow
					, TblColumnNode.New(AccountingTransaction.F.Cost, FooterAggregateCol.Sum(), FieldSelect.ShowAlways) // The only reason to be here is Cost so ALWAYS show
					, CostCenterColumns(AccountingTransaction.F.FromCostCenterID)
					, CostCenterColumns(AccountingTransaction.F.ToCostCenterID)
					, TblColumnNode.New(AccountingTransaction.F.AccountingSystemTransactionID, effect.ShowIfOneOf(ShowAll))
			);
		}

		#endregion
		#region CostCenter Columns
		static ColumnBuilder CostCenterColumns(dsMB.PathClass.PathToCostCenterLink CostCenter, FieldSelect effect = null) {
			return CodeDescColumnBuilder.New(CostCenter, effect, AccountingFeatureArg)
					.Concat(TblColumnNode.New(CostCenter.F.GeneralLedgerAccount, AccountingFeatureArg, effect.ShowIfOneOf(ShowAll)));
		}
		#endregion
		#region WorkOrderExpenseModel & Model Entry Columns
		static ColumnBuilder WorkOrderExpenseCategoryColumns(dsMB.PathClass.PathToWorkOrderExpenseCategoryLink WorkOrderExpenseCategory, FieldSelect effect = null) {
			return ColumnBuilder.New(WorkOrderExpenseCategory.PathToReferencedRow
				, CodeDescColumnBuilder.New(WorkOrderExpenseCategory, effect, AccountingFeatureArg)
				, TblColumnNode.New(WorkOrderExpenseCategory.F.FilterAsItem)
				, TblColumnNode.New(WorkOrderExpenseCategory.F.FilterAsLabor)
				, TblColumnNode.New(WorkOrderExpenseCategory.F.FilterAsMiscellaneous)
			);
		}
		static ColumnBuilder WorkOrderExpenseModelEntryColumns(dsMB.PathClass.PathToWorkOrderExpenseModelEntryLink WorkOrderExpenseModelEntry, FieldSelect effect = null, bool showModel = true) {
			return WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModelEntry.PathToReferencedRow, effect, showModel);
		}
		static ColumnBuilder WorkOrderExpenseModelEntryColumns(dsMB.PathClass.PathToWorkOrderExpenseModelEntryRow WorkOrderExpenseModelEntry, FieldSelect effect = null, bool showModel = true) {
			return ColumnBuilder.New(WorkOrderExpenseModelEntry,
				showModel ? WorkOrderExpenseModelColumns(WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID, effect, showEntry: false) : null,
				WorkOrderExpenseCategoryColumns(WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID, effect == ShowAll ? ShowXID : effect),
				CostCenterColumns(WorkOrderExpenseModelEntry.F.CostCenterID)
			);
		}
		static ColumnBuilder WorkOrderExpenseModelColumns(dsMB.PathClass.PathToWorkOrderExpenseModelLink WorkOrderExpenseModel, FieldSelect effect = null, bool showEntry = true) {
			return WorkOrderExpenseModelColumns(WorkOrderExpenseModel.PathToReferencedRow, effect, showEntry);
		}
		static ColumnBuilder WorkOrderExpenseModelColumns(dsMB.PathClass.PathToWorkOrderExpenseModelRow WorkOrderExpenseModel, FieldSelect effect = null, bool showEntry = true) {
			return ColumnBuilder.New(WorkOrderExpenseModel,
				CodeDescColumnBuilder.New(WorkOrderExpenseModel, effect, AccountingFeatureArg),
				CostCenterColumns(WorkOrderExpenseModel.F.NonStockItemHoldingCostCenterID),
				showEntry ? WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModel.F.DefaultHourlyInsideExpenseModelEntryID, effect) : null,
				showEntry ? WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModel.F.DefaultHourlyOutsideExpenseModelEntryID, effect) : null,
				showEntry ? WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModel.F.DefaultItemExpenseModelEntryID, effect) : null,
				showEntry ? WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModel.F.DefaultMiscellaneousExpenseModelEntryID, effect) : null,
				showEntry ? WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModel.F.DefaultPerJobInsideExpenseModelEntryID, effect) : null,
				showEntry ? WorkOrderExpenseModelEntryColumns(WorkOrderExpenseModel.F.DefaultPerJobOutsideExpenseModelEntryID, effect) : null
			);
		}
		#endregion
		#region Employee Columns
		static ColumnBuilder EmployeeColumns(dsMB.PathClass.PathToEmployeeLink employee, FieldSelect effect = null) {
			return EmployeeColumns(employee.PathToReferencedRow, effect);
		}
		static ColumnBuilder EmployeeColumns(dsMB.PathClass.PathToEmployeeRow employee, FieldSelect effect = null) {
			// Cannot use CodeDescColumnBuilder.New because Employee doesn't have a "Code" field 
			return ColumnBuilder.New(employee
					, TblColumnNode.New(employee.F.ContactID.F.Code, effect.ShowIfOneOf(ShowAll, ShowXID))
					, TblColumnNode.New(employee.F.Desc, effect.ShowIfOneOf(ShowAll))
					, TblColumnNode.New(employee.F.Comment, effect.ShowIfOneOf(ShowAll))
			);
		}
		#endregion
		#region Trade Columns
		static ColumnBuilder TradeColumns(dsMB.PathClass.PathToTradeLink trade, FieldSelect effect = null) {
			return CodeDescColumnBuilder.New(trade, effect);
		}
		#endregion
		#region Labor Columns
		static ColumnBuilder LaborInsideColumns(dsMB.PathClass.PathToLaborInsideLink item, FieldSelect effect = null) {
			return LaborInsideColumns(item.PathToReferencedRow, effect);
		}
		static ColumnBuilder LaborInsideColumns(dsMB.PathClass.PathToLaborInsideRow item, FieldSelect effect = null) {
			return ColumnBuilder.New(item
				, CodeDescColumnBuilder.New(item, effect)
				, TblColumnNode.New(item.F.Cost, effect.ShowIfOneOf(ShowAll, ShowXIDAndCost))
				, CostCenterColumns(item.F.CostCenterID)
				, EmployeeColumns(item.F.EmployeeID)
				, TradeColumns(item.F.TradeID)
			);
		}
		static ColumnBuilder LaborOutsideColumns(dsMB.PathClass.PathToLaborOutsideLink item, FieldSelect effect = null) {
			return LaborOutsideColumns(item.PathToReferencedRow, effect);
		}
		static ColumnBuilder LaborOutsideColumns(dsMB.PathClass.PathToLaborOutsideRow item, FieldSelect effect = null) {
			return ColumnBuilder.New(item
				, CodeDescColumnBuilder.New(item, effect)
				, TblColumnNode.New(item.F.Cost, effect.ShowIfOneOf(ShowAll, ShowXIDAndCost))
				, TblColumnNode.New(item.F.PurchaseOrderText, effect.ShowIfOneOf(ShowAll))
				, VendorColumns(item.F.VendorID)
				, TradeColumns(item.F.TradeID)
			);
		}
		#endregion
		#region OtherWork Columns
		static ColumnBuilder OtherWorkInsideColumns(dsMB.PathClass.PathToOtherWorkInsideLink item, FieldSelect effect = null) {
			return OtherWorkInsideColumns(item.PathToReferencedRow, effect);
		}
		static ColumnBuilder OtherWorkInsideColumns(dsMB.PathClass.PathToOtherWorkInsideRow item, FieldSelect effect = null) {
			return ColumnBuilder.New(item
				, CodeDescColumnBuilder.New(item, effect)
				, TblColumnNode.New(item.F.Cost, effect.ShowIfOneOf(ShowAll, ShowXIDAndCost))
				, CostCenterColumns(item.F.CostCenterID)
				, EmployeeColumns(item.F.EmployeeID)
				, TradeColumns(item.F.TradeID)
			);
		}
		static ColumnBuilder OtherWorkOutsideColumns(dsMB.PathClass.PathToOtherWorkOutsideLink item, FieldSelect effect = null) {
			return OtherWorkOutsideColumns(item.PathToReferencedRow, effect);
		}
		static ColumnBuilder OtherWorkOutsideColumns(dsMB.PathClass.PathToOtherWorkOutsideRow item, FieldSelect effect = null) {
			return ColumnBuilder.New(item
				, CodeDescColumnBuilder.New(item, effect)
				, TblColumnNode.New(item.F.Cost, effect.ShowIfOneOf(ShowAll, ShowXIDAndCost))
				, TblColumnNode.New(item.F.PurchaseOrderText, effect.ShowIfOneOf(ShowAll))
				, VendorColumns(item.F.VendorID)
				, TradeColumns(item.F.TradeID)
			);
		}
		#endregion
		#region Miscellaneous Cost Columns
		static ColumnBuilder MiscellaneousWorkOrderCostColumns(dsMB.PathClass.PathToMiscellaneousWorkOrderCostRow item, FieldSelect effect = null) {
			return ColumnBuilder.New(item
				, CodeDescColumnBuilder.New(item, effect)
				, TblColumnNode.New(item.F.Cost, effect.ShowIfOneOf(ShowAll, ShowXIDAndCost), FooterAggregateCol.Sum())
				, CostCenterColumns(item.F.CostCenterID)
			);
		}
		#endregion

		#region Schedule Columns
		static readonly char[] DayLetter = new char[] { KB.K("Monday").Translate()[0], KB.K("Tuesday").Translate()[0], KB.K("Wednesday").Translate()[0], KB.K("Thursday").Translate()[0], KB.K("Friday").Translate()[0], KB.K("Saturday").Translate()[0], KB.K("Sunday").Translate()[0], };
		/// <summary>
		///  A single collection of the allowed days enabled on a scheduled (for compactness)
		/// </summary>
		/// <param name="Schedule"></param>
		/// <returns></returns>
		static ColumnBuilder AllowDays(dsMB.PathClass.PathToScheduleRow Schedule) {
			var AllowMonday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableMonday));
			var AllowTuesday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableTuesday));
			var AllowWednesday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableWednesday));
			var AllowThursday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableThursday));
			var AllowFriday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableFriday));
			var AllowSaturday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableSaturday));
			var AllowSunday = new TblQueryExpression(new SqlExpression(Schedule.F.EnableSunday));
			var AllowDays = TblQueryCalculation.New(values => new string(values.Zip(DayLetter, (a, b) => (bool)a ? b : '_').ToArray()),
				new Libraries.TypeInfo.StringTypeInfo(7, 7, 0, true, true, false),
				AllowMonday, AllowTuesday, AllowWednesday, AllowThursday, AllowFriday, AllowSaturday, AllowSunday);

			return ColumnBuilder.New(Schedule, TblQueryValueNode.New(SameKeyContextAs.K(KB.K("Allow Days"), Schedule), AllowDays, Fmt.SetMonospace(true), DefaultShowInDetailsCol.Show()));
		}
		static ColumnBuilder ScheduleColumns(dsMB.PathClass.PathToScheduleLink Schedule, FieldSelect effect = null) {
			return ScheduleColumns(Schedule.PathToReferencedRow, effect);
		}
		static ColumnBuilder ScheduleColumns(dsMB.PathClass.PathToScheduleRow Schedule, FieldSelect effect = null) {
			var defaultShow = effect.ShowIfOneOf(ShowMaintenanceTiming);
			return ColumnBuilder.New(Schedule)
				.ColumnSection(StandardReport.LeftColumnFmtId
					, CodeDescColumnBuilder.New(Schedule, effect == null ? null : effect == ShowMaintenanceTiming ? ShowAll : ShowXID)
					, TblQueryValueNode.New(Schedule.F.SeasonStart.Key(), Statics.DateInYearFormatter(Schedule.F.SeasonStart), defaultShow)
					, TblQueryValueNode.New(Schedule.F.SeasonEnd.Key(), Statics.DateInYearFormatter(Schedule.F.SeasonEnd), defaultShow)
				).ColumnSection(StandardReport.RightColumnFmtId
					, effect == ShowMaintenanceTiming ?
						AllowDays(Schedule)
					:
						ColumnBuilder.New(Schedule
							, TblColumnNode.New(Schedule.F.EnableMonday, FieldSelect.ShowNever)
							, TblColumnNode.New(Schedule.F.EnableTuesday, FieldSelect.ShowNever)
							, TblColumnNode.New(Schedule.F.EnableWednesday, FieldSelect.ShowNever)
							, TblColumnNode.New(Schedule.F.EnableThursday, FieldSelect.ShowNever)
							, TblColumnNode.New(Schedule.F.EnableFriday, FieldSelect.ShowNever)
							, TblColumnNode.New(Schedule.F.EnableSaturday, FieldSelect.ShowNever)
							, TblColumnNode.New(Schedule.F.EnableSunday, FieldSelect.ShowNever)
						)
					, TblColumnNode.New(Schedule.F.InhibitIfOverdue, Fmt.SetEnumText(TISchedule.DeferInhibitEnumText), defaultShow)
					, TblColumnNode.New(Schedule.F.InhibitSeason, Fmt.SetEnumText(TISchedule.DeferInhibitEnumText), defaultShow)
					, TblColumnNode.New(Schedule.F.InhibitWeek, Fmt.SetEnumText(TISchedule.DeferInhibitEnumText), defaultShow)
				);
		}
		#endregion
		#endregion

		#region common Tbl generation
		static Tbl GenericChartBase(DBI_Table rootTable, FeatureGroup featureGroup, Tbl.TblIdentification title, RTbl.BuildReportObjectDelegate reportBuilder,
			TblLayoutNodeArray resourceFilters, params RTblBase.ICtorArg[] additionalRTblAttrs) {
			var rTblAttr = new List<RTblBase.ICtorArg>(additionalRTblAttrs) {
				// No Fields tab (they are hardwired into the report) and no Suppress Costs (because right now none of these reports show costs)
				RTblBase.SetPreferWidePage(true),
				RTblBase.SetNoUserFieldSelectionAllowed(),
				RTblBase.SetNoUserSortingAllowed()
			};
			return new Tbl(rootTable, title,
				new Tbl.IAttr[] {
					featureGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new RTbl(reportBuilder, typeof(ReportViewerControl), rTblAttr.ToArray())
				},
				resourceFilters
			);
		}
		static Tbl SelectableFieldChartBase(DBI_Table rootTable, FeatureGroup featureGroup, Tbl.TblIdentification title, RTbl.BuildReportObjectDelegate reportBuilder,
			TblLayoutNodeArray resourceFilters, RTblBase.LeafNodeFilter filter, params RTblBase.ICtorArg[] additionalRTblAttrs) {
			var rTblAttr = new List<RTblBase.ICtorArg>(additionalRTblAttrs) {
				// Fields tab content controlled by the filter and no Suppress Costs (because right now none of these reports show costs)
				RTblBase.SetPreferWidePage(true),
				RTblBase.SetUserFieldSelection(RTblBase.FieldSelectTabOptions.SuppressCostsControl | RTblBase.FieldSelectTabOptions.ShowFieldsControl, filter),
				RTblBase.SetNoUserSortingAllowed()
			};
			return new Tbl(rootTable, title,
				new Tbl.IAttr[] {
					featureGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new RTbl(reportBuilder, typeof(ReportViewerControl), rTblAttr.ToArray())
				},
				resourceFilters
			);
		}
		#endregion

		#region Requests
		#region - Requests
		private static RTblBase.ICtorArg RequestStateHistoryChildDefinition(dsMB.PathClass.PathToRequestStateHistoryRow RSH, IEnumerable<TblLeafNode> customGrouping = null) {
			return RTblBase.SetParentChildInformation(StateHistoryKey,
						RTblBase.ColumnsRowType.JoinWithAssuredChildren,
						null,
						// TODO: All columns of the StateHistory records will appear in COLUMNS (only), including the Comments fields as we have no means to have them be separated onto individual Label/Value full width rows in the report at this time
						RequestStateHistoryWithDurationColumns(RSH, ShowStateHistory).LayoutArray(),
						customGrouping,
						new DBI_Path[] { RSH.F.EffectiveDate });
		}
		#region -   Request Form & History [by Assignee]
		static Tbl RequestHistoryOrFormTbl(Tbl.TblIdentification title, bool formLayout, dsMB.PathClass.PathToRequestReportRow RFR, dsMB.PathClass.PathToRequestAssigneeRow assignee = null, bool singleRecord = false, SqlExpression filter = null) {
			dsMB.PathClass.PathToRequestLink Rl = RFR.F.RequestID;
			dsMB.PathClass.PathToRequestRow R = Rl.PathToReferencedRow;
			dsMB.PathClass.PathToRequestExtrasRow RR = Rl.L.RequestExtras.RequestID;

			var rtblArgs = new List<RTblBase.ICtorArg> {
				RTblBase.SetReportCaptionText(title.Compose(FormReportCaption)),
				RTblBase.SetReportTitle(title.Compose(FormReportTitle)),
				RTblBase.SetRecordHeader(null,
					TblQueryValueNode.New(null, ValueAsBarcode(R.F.Number), Fmt.SetUsage(DBI_Value.UsageType.Image), FieldSelect.ShowNever),
					TblQueryValueNode.New(null, new TblQueryPath(R.F.Number))),
				RTbl.IncludeBarCodeSymbologyControl(),
				RTblBase.SetAllowSummaryFormat(false),
				RTblBase.SetPrimaryRecordHeaderNode(R.F.Number),
				RequestStateHistoryChildDefinition(RFR.F.RequestStateHistoryID.PathToReferencedRow,
					assignee != null ?
						new TblLeafNode[] {
							TblColumnNode.New(assignee.F.ContactID),
							TblColumnNode.New(R.F.Number)
						}
						:
						new TblLeafNode[] {
							TblColumnNode.New(R.F.Number)
						}
				)
			};
			if (filter != null)
				rtblArgs.Add(RTblBase.SetFilter(filter));
			if (singleRecord) {
				rtblArgs.Add(RTblBase.SetNoUserFilteringAllowed());
				rtblArgs.Add(RTblBase.SetNoUserSortingAllowed());
			}
			if (formLayout) {
				rtblArgs.Add(RTblBase.SetNoUserGroupingAllowed());
			}
			else {
				rtblArgs.Add(RTblBase.SetPageBreakDefault(true));
			}
			if (assignee != null) {
				rtblArgs.Add(RTblBase.SetNoSearchPrefixing());
				rtblArgs.Add(RTblBase.SetRequiredInvariantForFooterAggregates(assignee));
			}
			List<Tbl.IAttr> iAttr = new List<Tbl.IAttr> {
				RequestsGroup,
				new PrimaryRecordArg(R),
				CommonTblAttrs.ViewCostsDefinedBySchema
			};
			if (formLayout)
				iAttr.Add(new RTbl((r, logic) => new Reports.MainBossFormReport(r, logic), typeof(ReportViewerControl), rtblArgs.ToArray()));
			else
				iAttr.Add(new RTbl((r, logic) => new TblDrivenDynamicColumnsReport(r, logic), typeof(ReportViewerControl), rtblArgs.ToArray()));

			var layout = ColumnBuilder.New(R);
			if (assignee != null) {
				layout.ColumnSection(StandardReport.RightColumnFmtId
					, ContactColumns(assignee.F.ContactID, ShowXIDBold)
					, UserColumnsForContact(assignee.F.ContactID.L.User.ContactID)
				);
			}
			layout.Concat(
				RequestColumns(R, RR, ShowForRequest)
			);
			List<TblActionNode> extraNodes = new List<TblActionNode>();
			if (formLayout)
				extraNodes.Add(ClearSelectForPrintCommand(R.F.SelectPrintFlag, Rl));
			return new Tbl(R.Table, title, iAttr.ToArray(), layout.LayoutArray(), extraNodes.ToArray());
		}
		public static Tbl RequestFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport);
		public static Tbl RequestNewFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport, filter: new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsNew).IsTrue());
		public static Tbl RequestInProgressFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport, filter: new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress).IsTrue());
		public static Tbl RequestInProgressAndAssignedFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport, filter:
							new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID)
									.In(new SelectSpecification(
										null,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID) },
										new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))
												.And(new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress).IsTrue()),
											null).SetDistinct(true)));
		public static Tbl RequestClosedFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport, filter: new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsClosed).IsTrue());
		public static Tbl RequestInProgressWithWOFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport, filter: new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders).Gt(SqlExpression.Constant(0))
											.And(new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress).IsTrue()));
		public static Tbl RequestInProgressWithoutWOFormReport = RequestHistoryOrFormTbl(TId.Request, true, dsMB.Path.T.RequestReport, filter: new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders).Eq(SqlExpression.Constant(0))
											.And(new SqlExpression(dsMB.Path.T.RequestReport.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress).IsTrue()));

		public static Tbl SingleRequestFormReport = RequestHistoryOrFormTbl(TId.Request.ReportSingle, true, dsMB.Path.T.RequestReport, singleRecord: true);
		public static Tbl RequestByAssigneeFormReport = RequestHistoryOrFormTbl(TId.Request.ReportByAssignee, true, dsMB.Path.T.RequestAssignmentReport.F.RequestReportID.PathToReferencedRow,
																					dsMB.Path.T.RequestAssignmentReport.F.RequestAssignmentID.F.RequestAssigneeID.PathToReferencedRow);
		public static Tbl RequestHistory = RequestHistoryOrFormTbl(TId.Request.ReportHistory, false, dsMB.Path.T.RequestReport);
		public static Tbl RequestHistoryByAssignee = RequestHistoryOrFormTbl(TId.Request.ReportHistory.ReportByAssignee, false, dsMB.Path.T.RequestAssignmentReport.F.RequestReportID.PathToReferencedRow,
			dsMB.Path.T.RequestAssignmentReport.F.RequestAssignmentID.F.RequestAssigneeID.PathToReferencedRow);
		#endregion
		#endregion

		#region RequestSummary
		public static Tbl RequestSummary = new Tbl(dsMB.Schema.T.Request,
			TId.Request.ReportSummary,
			new Tbl.IAttr[] {
				RequestsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true), RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.Request,
				RequestColumns(dsMB.Path.T.Request, dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID, ShowForRequestSummary)
			).LayoutArray()
		);
		#endregion
		#region RequestSummaryByAssignee
		public static Tbl RequestByAssigneeSummary = new Tbl(dsMB.Schema.T.RequestsWithAssignments,
			TId.Request.ReportSummary.ReportByAssignee,
			new Tbl.IAttr[] {
				RequestsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetRequiredInvariantForFooterAggregates(TblColumnNode.New(dsMB.Path.T.RequestsWithAssignments.F.RequestAssignmentID.F.RequestAssigneeID.F.ContactID.F.Code)),
					RTblBase.SetGrouping(dsMB.Path.T.RequestsWithAssignments.F.RequestAssignmentID.F.RequestAssigneeID)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.RequestsWithAssignments,
					ContactColumns(dsMB.Path.T.RequestsWithAssignments.F.RequestAssignmentID.F.RequestAssigneeID.F.ContactID, ShowXID),
					UserColumnsForContact(dsMB.Path.T.RequestsWithAssignments.F.RequestAssignmentID.F.RequestAssigneeID.F.ContactID.L.User.ContactID),
					RequestColumns(dsMB.Path.T.RequestsWithAssignments.F.RequestID, dsMB.Path.T.RequestsWithAssignments.F.RequestID.L.RequestExtras.RequestID, ShowForRequestSummary)
			).LayoutArray()
		);
		#endregion
		#region Requestor
		public static Tbl RequestorReport = new Tbl(dsMB.Schema.T.Requestor,
			TId.Requestor,
			new Tbl.IAttr[] {
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.Requestor,
				  ContactColumns(dsMB.Path.T.Requestor.F.ContactID, ShowXID)
				, TblColumnNode.New(dsMB.Path.T.Requestor.F.ReceiveAcknowledgement, FieldSelect.ShowAlways, Fmt.SetEnumText(TIRequest.YesOrNoForBoolColumnReportText))
				, TblColumnNode.New(dsMB.Path.T.Requestor.F.Comment)
			).LayoutArray()
		);
		#endregion
		#region RequestAssignee
		public static Tbl RequestAssigneeReport = new Tbl(dsMB.Schema.T.RequestAssignee,
			TId.RequestAssignee,
			new Tbl.IAttr[] {
				RequestsGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.RequestAssignee,
				  ContactColumns(dsMB.Path.T.RequestAssignee.F.ContactID, ShowXID)
				, UserColumnsForContact(dsMB.Path.T.RequestAssignee.F.ContactID.L.User.ContactID)
				, TblColumnNode.New(dsMB.Path.T.RequestAssignee.F.Id.L.RequestAssigneeStatistics.RequestAssigneeID.F.NumNew, FieldSelect.ShowAlways)
				, TblColumnNode.New(dsMB.Path.T.RequestAssignee.F.Id.L.RequestAssigneeStatistics.RequestAssigneeID.F.NumInProgress, FieldSelect.ShowAlways)
			).LayoutArray()
		);
		#endregion
		#region - Request Charts
		#region -     RequestChartBase
		static Tbl RequestChartBase(Tbl.TblIdentification title, RTbl.BuildReportObjectDelegate reportBuilder, RTblBase.ICtorArg intervalAttribute = null, RTblBase.ICtorArg filterAttribute = null, bool defaultGroupByRequestor = false) {
			return GenericChartBase(dsMB.Schema.T.Request, RequestsGroup, title, reportBuilder,
				ColumnBuilder.New(dsMB.Path.T.Request,
					RequestColumns(dsMB.Path.T.Request, dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID)
				).LayoutArray(),
				intervalAttribute,
				filterAttribute,
				defaultGroupByRequestor ? RTblBase.SetGrouping(dsMB.Path.T.Request.F.RequestorID) : null);
		}
		#endregion
		#region -     Request Count charts
		static Tbl RequestChartCountBase(Tbl.TblIdentification title, DBI_Path groupingPath, ReportViewLogic.IntervalSettings? groupingInterval) {
			return RequestChartBase(title, (Report r, ReportViewLogic logic) => new Reports.RequestChartCountBase(r, logic, groupingPath), RTblBase.SetChartIntervalGrouping(groupingInterval), defaultGroupByRequestor: groupingPath == null);
		}
		public static Tbl RequestChartCountByCreatedDate = RequestChartCountBase(TId.RequestChartCountByCreatedDate, dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CreatedDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl RequestChartCountByInProgressDate = RequestChartCountBase(TId.RequestChartCountByInProgressDate, dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.InProgressDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl RequestChartCountByEndedDate = RequestChartCountBase(TId.RequestChartCountByEndedDate, dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.EndedDateIfEnded, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl RequestChartCount = RequestChartCountBase(TId.RequestChartCount, (DBI_Path)null, null);
		#endregion
		#region -     Request time-span charts
		static Tbl RequestChartDurationBase(Tbl.TblIdentification title, TblLeafNode valueNode, Expression.Function aggregateFunction, ReportViewLogic.IntervalSettings intervalValueScaling, SqlExpression filter) {
			return RequestChartBase(title, (Report r, ReportViewLogic logic) => new Reports.RequestChartDurationBase(r, logic, valueNode, aggregateFunction), RTblBase.SetChartIntervalValueScaling(intervalValueScaling), filterAttribute: RTblBase.SetFilter(filter), defaultGroupByRequestor: true);
		}
		public static Tbl RequestChartAverageDuration = RequestChartDurationBase(TId.RequestChartAverageDuration,
			TblColumnNode.New(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.InProgressDuration), Expression.Function.Avg, ReportViewLogic.IntervalSettings.Days, new SqlExpression(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.InProgressDuration).IsNotNull());
		#endregion
		public static Tbl RequestChartLifetime = RequestChartBase(TId.RequestChartLifetime, (Report r, ReportViewLogic logic) => new Reports.RequestChartLifetime(r, logic));
		#endregion
		#region - RequestStateHistory
		#region -   RequestStateHistory
		public static Tbl RequestStateHistory = new Tbl(dsMB.Schema.T.RequestStateHistory,
			TId.RequestStateHistory,
			new Tbl.IAttr[] {
				RequestsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new PrimaryRecordArg(dsMB.Path.T.RequestStateHistory.F.RequestID.PathToReferencedRow),
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.RequestStateHistory.F.RequestID.F.Number),
					// Set No Search Prefixing because the primary path (and the root of all paths in this report) include a labelled reference to the WO from the WOSH record.
					RTblBase.SetNoSearchPrefixing(),
					RequestStateHistoryChildDefinition(dsMB.Path.T.RequestStateHistory)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.RequestStateHistory
				, RequestColumns(dsMB.Path.T.RequestStateHistory.F.RequestID, dsMB.Path.T.RequestStateHistory.F.RequestID.L.RequestExtras.RequestID, ShowForRequestSummary)
			).LayoutArray()
		);
		#endregion
		#region -   RequestStateHistorySummary
		public static Tbl RequestStateHistorySummary = new Tbl(dsMB.Schema.T.RequestStateHistory,
			TId.RequestStateHistory.ReportSummary,
			new Tbl.IAttr[] {
				RequestsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.RequestStateHistory
				, RequestColumns(dsMB.Path.T.RequestStateHistory.F.RequestID, dsMB.Path.T.RequestStateHistory.F.RequestID.L.RequestExtras.RequestID, ShowXID)
				, RequestStateHistoryWithDurationColumns(dsMB.Path.T.RequestStateHistory, ShowStateHistory)
			).LayoutArray()
		);
		#endregion
		#region -   RequestChartStatus
		// Because this chart shows averages per request, we need several special provisions:
		// 1 - We can't allow user filtering by State History record because this might hide some requestss (which should report as an average of zero)
		// 2 - We must only look at draft/open time; otherwise old work orders will show up as spending an average of huge amounts of time in whatever their final Status is.
		// All work orders will have at least one State History record (from when they were created) so the data is available to count the work orders in each user-defined
		// grouping.
		// Although grouping by properties of the Work Order would be meaningful, the method whereby charts are generated for the groups does not allow the count of
		// work orders in the group to be determined (the scope for the CountDistinct has to be different for each group) so for now we don't allow user grouping.
		public static Tbl RequestChartStatus = new Tbl(dsMB.Schema.T.RequestStateHistory,
			TId.RequestChartStatus,
			new Tbl.IAttr[] {
				RequestsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl( typeof(Thinkage.MainBoss.Controls.Reports.RequestChartStatusReport), typeof(ReportViewerControl),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.RequestStateHistory.F.RequestStateID.F.FilterAsNew)
									.Or(new SqlExpression(dsMB.Path.T.RequestStateHistory.F.RequestStateID.F.FilterAsInProgress))),
					RTblBase.SetChartIntervalValueScaling(ReportViewLogic.IntervalSettings.Days),
					RTblBase.SetNoUserFieldSelectionAllowed(),
					RTblBase.SetNoUserGroupingAllowed()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.RequestStateHistory,
				RequestColumns(dsMB.Path.T.RequestStateHistory.F.RequestID, dsMB.Path.T.RequestStateHistory.F.RequestID.L.RequestExtras.RequestID)
			).LayoutArray()
		);
		#endregion
		#endregion
		#endregion

		#region Unit
		#region UnitReport

		private static DelayedCreateTbl UnitReportOrSummary(bool summary) {
			return new DelayedCreateTbl(
				delegate () {
					return new Tbl(dsMB.Schema.T.Unit,
						summary ? TId.Unit.ReportSummary : TId.Unit,
						new Tbl.IAttr[] {
							UnitsDependentGroup,
							CommonTblAttrs.ViewCostsDefinedBySchema,
							TblDrivenReportRtbl(
								RTbl.IncludeBarCodeSymbologyControl(),
								RTblBase.SetAllowSummaryFormat(summary),
								RTblBase.SetDualLayoutDefault(summary),
								RTblBase.SetPreferWidePage(summary),
								RTblBase.SetGrouping(dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID.F.Code)
							)
						},
						ColumnBuilder.New(dsMB.Path.T.Unit
							, UnitColumns(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, summary ? ShowForUnitSummary : ShowForUnit)
#if TRANSITION_REPORTS
							, TblColumnNode.New(KB.K("Specifications"), dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, Fmt.SetShowReferences(SpecificationReport, dsMB.Path.T.Specification.F.UnitLocationID))
							, TblColumnNode.New(KB.K("Attachments"), dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, Fmt.SetShowReferences(AttachmentReport, dsMB.Path.T.Attachment.F.UnitLocationID))
							, TblColumnNode.New(KB.K("Relationships"), dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, Fmt.SetShowReferences(UnitRelatedRecordsReport, dsMB.Path.T.UnitRelatedRecords.F.ThisUnitLocationID))
							, TblColumnNode.New(KB.K("Service Contracts"), dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID, Fmt.SetShowReferences(UnitServiceContractReport, dsMB.Path.T.UnitServiceContract.F.UnitLocationID))
#else
							, TblColumnNode.New(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.UnitSpecification)
							, TblColumnNode.New(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.UnitAttachment)
#endif
).LayoutArray()
					);
				}
			);
		}
		public static DelayedCreateTbl UnitReport = UnitReportOrSummary(summary: false);
		#endregion
		#region UnitSummary
		public static DelayedCreateTbl UnitSummary = UnitReportOrSummary(summary: true);
		#endregion
		#region UnitParts
		// TODO: This report is poorly named. It is also the same as the UnitParts report other than the default grouping.
		public static Tbl UnitParts = new Tbl(dsMB.Schema.T.SparePart,
			TId.Part,
			new Tbl.IAttr[] {
				InventoryGroup | PartsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTbl.IncludeBarCodeSymbologyControl()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.SparePart,
				ItemColumns(dsMB.Path.T.SparePart.F.ItemID, ShowXIDAndUOM),
				TblColumnNode.New(dsMB.Path.T.SparePart.F.Quantity, DefaultShowInDetailsCol.Show()),
				UnitColumns(dsMB.Path.T.SparePart.F.UnitLocationID, ShowXID)
			).LayoutArray()
		);
		#endregion
		#region UnitMeters
		public static Tbl UnitMeters = new Tbl(dsMB.Schema.T.Meter,
		TId.Meter,
		new Tbl.IAttr[] {
				MetersDependentGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTbl.IncludeBarCodeSymbologyControl(),
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			MeterColumns(dsMB.Path.T.Meter, ShowAll)
			//				.Concat(
			//					GroupingSpecifier(0, dsMB.Path.T.Meter.F.UnitLocationID) // This will currently do two level grouping, and show GUIDS for the the Unit identification; not really desired. We need a means to put the grouping ON the TblColumnNode that is in the column list, not specifiy a separate grouping path ..
			//			)
			.LayoutArray()
		);
		#endregion
		#region MeterReadingHistory
		public static Tbl MeterReadingHistory = new Tbl(dsMB.Schema.T.MeterReading,
			TId.MeterReading.ReportHistory,
			new Tbl.IAttr[] {
				MetersDependentGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTbl.IncludeBarCodeSymbologyControl(),
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetGrouping(dsMB.Path.T.MeterReading.F.MeterID)
				)
			},
			MeterReadingColumns(dsMB.Path.T.MeterReading, ShowAll).Concat(MeterColumns(dsMB.Path.T.MeterReading.F.MeterID, ShowXID)).LayoutArray()
		);
		#endregion
		#region UnitMaintenanceHistory
		public static Tbl UnitMaintenanceHistory = WOSummaryBase(TId.MaintenanceHistory, dsMB.Path.T.WorkOrder, ShowForMaintenanceHistory, null);
		#endregion
		#region MeterClass
		public static Tbl MeterClassReport = new Tbl(dsMB.Schema.T.MeterClass,
			TId.MeterClass,
			new Tbl.IAttr[] {
				MetersDependentGroup,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			MeterClassColumns(dsMB.Path.T.MeterClass, ShowAll).LayoutArray()
		);
		#endregion
		#region ServiceContract
		static ColumnBuilder ServiceContractColumns(dsMB.PathClass.PathToServiceContractRow ServiceContract, FieldSelect effect = null) {
			string CoverParts = KB.TOc(TId.Part).Translate();
			string CoverLabor = KB.TOi(TId.Labor).Translate();
			StringTypeInfo resultType = new StringTypeInfo(0, CoverParts.Length + CoverLabor.Length + 1, 0, true, true, true);
			var coverage = TblQueryValueNode.New(SameKeyContextAs.K(KB.K("Coverage"), ServiceContract), TblQueryCalculation.New(
								(values => {
									System.Text.StringBuilder result = new System.Text.StringBuilder();
									if ((bool)Libraries.TypeInfo.BoolTypeInfo.AsNativeType(values[0], typeof(bool)))
										result.Append(CoverParts);
									if ((bool)Libraries.TypeInfo.BoolTypeInfo.AsNativeType(values[1], typeof(bool))) {
										if (result.Length > 0)
											result.Append(" ");
										result.Append(CoverLabor);
									}
									return result.Length > 0 ? result.ToString() : null;
								}
								),
								resultType,
								new TblQueryPath(ServiceContract.F.Parts),
								new TblQueryPath(ServiceContract.F.Labor)
					), effect.ShowIfOneOf(ShowForServiceContract));

			return ColumnBuilder.New(ServiceContract)
				.ColumnSection(StandardReport.LeftColumnFmtId
					, TblColumnNode.New(ServiceContract.F.Code, effect.ShowIfOneOf(ShowForServiceContract, ShowForServiceContractSummary))
					, TblColumnNode.New(ServiceContract.F.Desc, effect.ShowIfOneOf(ShowForServiceContract)) // CodeDesc here, Comments below
					, TblColumnNode.New(ServiceContract.F.ContractNumber, effect.ShowIfOneOf(ShowForServiceContract))
					, TblColumnNode.New(ServiceContract.F.StartDate, effect.ShowIfOneOf(ShowForServiceContract, ShowForServiceContractSummary))
					, TblColumnNode.New(ServiceContract.F.EndDate, effect.ShowIfOneOf(ShowForServiceContract, ShowForServiceContractSummary))
					, coverage
				)
				.ColumnSection(StandardReport.RightColumnFmtId
					, VendorColumns(ServiceContract.F.VendorID, effect == ShowForServiceContract ? ShowServiceContactBusPhoneEmail : null)
					, TblColumnNode.New(ServiceContract.F.Cost, FooterAggregateCol.Sum(), effect.ShowIfOneOf(ShowForServiceContract, ShowForServiceContractSummary))
				)
				.ColumnSection(StandardReport.MultiLineRowsFmtId
					, TblColumnNode.New(ServiceContract.F.Comment, effect.ShowIfOneOf(ShowForServiceContract))
				);
		}
		public static Tbl ServiceContractReport = new Tbl(dsMB.Schema.T.UnitServiceContract,
			TId.ServiceContract,
			new Tbl.IAttr[] {
				UnitValueAndServiceGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new PrimaryRecordArg(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.PathToReferencedRow),
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Code),
					// All the paths traverse dsMB.Path.T.UnitServiceContract.F.ServiceContractID which supplies root record searching.
					RTblBase.SetNoSearchPrefixing(),
					RTblBase.SetAllowSummaryFormat(false),
					RTbl.IncludeBarCodeSymbologyControl(),
					RTblBase.SetParentChildInformation(KB.K("Show Units"),
						// TODO: Because this report is improperly driven by the UnitServiceContract table, which is essentially the child row, it will appear as a join with assured children.
						// However, service contracts with no associated units will not appear in the report.
						RTblBase.ColumnsRowType.JoinWithAssuredChildren,
						null,
						TIReports.UnitColumns(dsMB.Path.T.UnitServiceContract.F.UnitLocationID, TIReports.ShowXID).LayoutArray(),
						null,
						new DBI_Path[] {
						dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.RelativeLocationID.F.Code,
						dsMB.Path.T.UnitServiceContract.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.RelativeLocationID.F.LocationID.F.Code
						}
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.PathToReferencedRow
				, ServiceContractColumns(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.PathToReferencedRow, ShowForServiceContract)
			).LayoutArray()
		);
		public static Tbl ServiceContractSummaryReport = new Tbl(dsMB.Schema.T.ServiceContract,
			TId.ServiceContract.ReportSummary,
			new Tbl.IAttr[] {
				UnitValueAndServiceGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTbl.IncludeBarCodeSymbologyControl(),
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ServiceContract
				, ServiceContractColumns(dsMB.Path.T.ServiceContract, ShowForServiceContractSummary)
			).LayoutArray()
		);
		#endregion
		#region SpecificationForm
		public static Tbl SpecificationFormReport = new Tbl(dsMB.Schema.T.SpecificationFormField,
			// TODO: This report always groups on SpecificationForm and by doing so tries to be a report on SpecificationForms, showing the fields as "child" records
			// However, the user can filter the child records, or there could be a SpecificationForm with no fields, in which case the SpecificationForm will just not
			// show up in this report (rather than showing up with no entries).
			// This should either be changed to have a driver view that returns records for all SpecificationForm, even ones with no fields, and with no filtering on Field properties,
			// or changed to a report that default-groups on SpecificationForm but allows the user to change this, in which case it is a "Specification Form Field" report.
			TId.SpecificationForm,
			new Tbl.IAttr[] {
				UnitsDependentGroup,
				new PrimaryRecordArg(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID.PathToReferencedRow),
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetNoUserGroupingAllowed(),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID.F.Code),
					RTblBase.SetParentChildInformation(KB.K("Show Form Fields"),
						// TODO: Because this report is improperly driven by the SpecificationFormField table, which is essentially the child row, it will appear as a join with assured children.
						// However, specification forms with no fields will not appear in the report.
						RTblBase.ColumnsRowType.JoinWithAssuredChildren,
						null,
						new TblLayoutNodeArray(
							TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.FieldName),
							TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.EditLabel),
							TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.FieldOrder),
							TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.FieldSize)
						),
						null,
						new DBI_Path[] {dsMB.Path.T.SpecificationFormField.F.FieldOrder}
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.SpecificationFormField)
				.ColumnSection(StandardReport.LVPRowsFmtId
					, CodeDescColumnBuilder.New(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID, ShowAll)
				)
				.ColumnSection(StandardReport.MultiLineRowsFmtId
					, TblServerExprNode.New(SameKeyContextAs.K(KB.K("Report Layout"), dsMB.Path.T.SpecificationFormField.F.SpecificationFormID),
							SqlExpression.Coalesce(
								new SqlExpression(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID.F.CustomizedReportLayout),
								new SqlExpression(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID.F.DefaultReportLayout)),
								Fmt.SetMonospace(true)
				)
			).LayoutArray()
		);
#if TRANSITION_REPORTS
		// TODO: This includes minimal information from the Specification Form, and no information about the Unit, as it is intended to be used as a subreport
		// in Unit reports. We need a method similar to that used in browsers to eliminate columns that are fixed by the subreport linkage filters.
		public static DelayedCreateTbl SpecificationReport = new DelayedCreateTbl(new Tbl(dsMB.Schema.T.Specification,
			TId.Specification,
			new Tbl.IAttr[] {
				UnitsDependentGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true))
			},
			new TblLayoutNodeArray(
				  TblColumnNode.New(dsMB.Path.T.Specification.F.Code)
				, TblColumnNode.New(dsMB.Path.T.Specification.F.Desc)
				, TblColumnNode.New(dsMB.Path.T.Specification.F.SpecificationFormID.F.Code)
				, TblColumnNode.New(dsMB.Path.T.Specification.F.ReportText)
				, TblColumnNode.New(dsMB.Path.T.Specification.F.Comment)
			)
		));
		public static DelayedCreateTbl AttachmentReport = new DelayedCreateTbl(new Tbl(dsMB.Schema.T.Attachment,
			TId.Attachment,
			new Tbl.IAttr[] {
				UnitsDependentGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(true))
			},
			new TblLayoutNodeArray(
				  TblColumnNode.New(dsMB.Path.T.Attachment.F.Code)
				, TblColumnNode.New(dsMB.Path.T.Attachment.F.Desc)
				, TblColumnNode.New(dsMB.Path.T.Attachment.F.Path)
				, TblColumnNode.New(dsMB.Path.T.Attachment.F.Comment)
			)
		));
		public static DelayedCreateTbl UnitRelatedRecordsReport = new DelayedCreateTbl(new Tbl(dsMB.Schema.T.UnitRelatedRecords,
			TId.UnitRelation,
			new Tbl.IAttr[] {
				UnitsDependentGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(true))
			},
			new TblLayoutNodeArray(
				  TblColumnNode.New(dsMB.Path.T.UnitRelatedRecords.F.RelationshipID.F.Code)
				, TblColumnNode.New(dsMB.Path.T.UnitRelatedRecords.F.RelationshipID.F.AAsRelatedToBPhrase)		// TODO AtoB or BtoA depending on Reverse.
				, TblColumnNode.New(dsMB.Path.T.UnitRelatedRecords.F.UnitRelatedContactID.F.ContactID.F.Code)
				, TblColumnNode.New(dsMB.Path.T.UnitRelatedRecords.F.UnitRelatedUnitID.F.AUnitLocationID.F.Code)	// TODO: A or B depending on Reverse.
			)
		));
		public static DelayedCreateTbl UnitServiceContractReport = new DelayedCreateTbl(new Tbl(dsMB.Schema.T.UnitServiceContract,
			TId.UnitServiceContract,
			new Tbl.IAttr[] {
				UnitsDependentGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(true))
			},
			new TblLayoutNodeArray(
				TblGroupNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID, new TblLayoutNode.ICtorArg[0],
					  TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Code)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Desc)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.ContractNumber)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.StartDate)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.EndDate)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Cost)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Labor)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Parts)
					, VendorColumns(GroupCol.GroupSortAbility.GROUPFlag, dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.VendorID, ShowXID)
					, TblColumnNode.New(dsMB.Path.T.UnitServiceContract.F.ServiceContractID.F.Comment)
				)
			)
		));
#endif
		#endregion
		#endregion

		#region Purchasing
		#region - Purchase Orders
		private static RTblBase.ICtorArg PurchaseOrderStateHistoryChildDefinition(dsMB.PathClass.PathToPurchaseOrderStateHistoryRow POSH, IEnumerable<TblLeafNode> customGrouping = null) {
			return RTblBase.SetParentChildInformation(StateHistoryKey,
						RTblBase.ColumnsRowType.JoinWithAssuredChildren,
						null,
						// TODO: All columns of the StateHistory records will appear in COLUMNS (only), including the Comments fields as we have no means to have them be separated onto individual Label/Value full width rows in the report at this time
						PurchaseOrderStateHistoryAndExtraColumns(POSH, ShowStateHistory).LayoutArray(),
						customGrouping,
						new DBI_Path[] { POSH.F.EffectiveDate });
		}

		#region -   PO Form & History [by Assignee]
		public static PermissionToView[] ViewCostPermissionsFromPath(DBI_Path path) {
			return path.Table.CostRights.Select(pn => new PermissionToView((Right)Root.Rights.ViewCost[pn])).ToArray();
		}
		static Tbl POHistoryOrFormTbl(Tbl.TblIdentification title, bool formLayout, dsMB.PathClass.PathToPurchaseOrderFormReportRow POFR, dsMB.PathClass.PathToPurchaseOrderAssigneeRow assignee = null, bool singleRecord = false, SqlExpression filter = null) {
			dsMB.PathClass.PathToPurchaseOrderLink POl = POFR.F.PurchaseOrderID;
			dsMB.PathClass.PathToPurchaseOrderRow PO = POl.PathToReferencedRow;
			dsMB.PathClass.PathToPurchaseOrderExtrasRow POR = POl.L.PurchaseOrderExtras.PurchaseOrderID;
			#region Child-row column expressions
			// The (total) cost, ordered or actual.
			var costCalculation = new TblQueryExpression(SqlExpression.Coalesce(
				new SqlExpression(POFR.F.AccountingTransactionID.F.Cost),
				new SqlExpression(POFR.F.POLineID.F.Cost)
			));
			// The quantity, ordered or received, for records that count in units
			var longQuantityExpression = SqlExpression.Coalesce(
				new SqlExpression(POFR.F.AccountingTransactionID.F.ReceiveItemPOID.F.Quantity),
				new SqlExpression(POFR.F.AccountingTransactionID.F.ReceiveMiscellaneousPOID.F.Quantity),
				new SqlExpression(POFR.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.Quantity),
				new SqlExpression(POFR.F.POLineID.F.POLineItemID.F.Quantity),
				new SqlExpression(POFR.F.POLineID.F.POLineMiscellaneousID.F.Quantity),
				new SqlExpression(POFR.F.POLineID.F.POLineOtherWorkID.F.Quantity)
			);
			// The quantity, ordered or received, for records that count in time intervals
			var timeQuantityExpression = SqlExpression.Coalesce(
				new SqlExpression(POFR.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.Quantity),
				new SqlExpression(POFR.F.POLineID.F.POLineLaborID.F.Quantity)
			);

			var descriptionColumn = TblQueryValueNode.New(POFR.F.POLineID.F.PurchaseOrderText.Key(), TblQueryCalculation.New(
				delegate (object[] values) {
					if (values[1] != null)
						// it is a receiving line, show the effective date, but we format it here so the expression has a consistent type.
						// We are assuming that the Format operation will never generate text too wide for the type of POLine.PurchaseOrderText.
						return RDLReport.DateHHMMFormatter.Format(values[1]);
					return values[0];
				},
				POFR.F.POLineID.F.PurchaseOrderText.ReferencedColumn.EffectiveType,
				new TblQueryExpression(new SqlExpression(POFR.F.POLineID.F.PurchaseOrderText)),
				new TblQueryExpression(new SqlExpression(POFR.F.AccountingTransactionID.F.EffectiveDate))
			));
			// Several of the expressions used here are implemented using DisplayValueExpression which turns out to be quite clumsy to code.
			// They were changed from being RDL expressions on the thought that these were slow to evaluate, but then the plain field references that result are also RDL expressions...
			// Most of the complex query columns could be done using TblReportQueryExpression if SqlExpression had ?:-type operations, in which case they could be done server-side as part of the query.
			// TODO: The driving query uses a union rather than outer join to attach the child records, so there is always at least one record with no child information and this must be filtered out
			// of the child-record grouping.
			// TODO: Because of the variant nature of some of the columns (Line number and perhaps PO Line Text) it is difficult to decide what the show/hide option and the column caption should be called.
			// Perhaps these should not be optional columns and they could skip the headers (blank headers)
			// TODO: The two Cost columns should perhaps be controlled by a single Total Cost show/hide option.
			// TODO: The context part of the column captions must be stripped off.
			// TODO: We really want to be able to call AddStandardReportChildColumnsRow twice, once for the POLine row, and another time for the Receive row, giving the actual display expression
			// and perhaps a distinct label each time. The second call would be required to use the same column structure as the first generated and the labels would only affect the "Show" parameters.
			// Unfortunately, the first call might omit columns if the user elected to hide them and this only works if the number of columns generated by the first call is fixed. The report-column
			// removal would have to be based on the overall hiding of all the column contents (taking spanning etc into effect).
			// So for now we use expressions that coalesce the ordered and received values and generic headers control the hiding of both together.
			#endregion
			var hideOnReceiveLines = RFmt.SetHideIf(new SqlExpression(POFR.F.AccountingTransactionID).IsNotNull());
			var rtblArgs = new List<RTblBase.ICtorArg> {
				RTblBase.SetReportCaptionText(title.Compose(FormReportCaption)),
				RTblBase.SetReportTitle(title.Compose(FormReportTitle), dsMB.Schema.V.POFormTitle),
				RTblBase.SetRecordHeader(null,
					TblQueryValueNode.New(null, ValueAsBarcode(PO.F.Number), Fmt.SetUsage(DBI_Value.UsageType.Image), FieldSelect.ShowNever),
					TblQueryValueNode.New(null, new TblQueryPath(PO.F.Number))),
				RTbl.IncludeBarCodeSymbologyControl(),
				RTblBase.SetAllowSummaryFormat(false),
				RTblBase.SetPrimaryRecordHeaderNode(PO.F.Number),
				RTblBase.SetParentChildInformation(KB.K("Purchase Lines"),
					RTblBase.ColumnsRowType.UnionWithChildren,
					POFR.F.POLineID,
					new TblLayoutNodeArray(
						TblRowNode.New(null, Array.Empty<TblLayoutNode.ICtorArg>(),
							ColumnBuilder.New( POFR, 
							// TODO: The following "formats" the LineNumber on the server side by casting it to a string type.
							// This really should happen client-side where we can use a TypeFormatter or RDL-side.
							// TODO: We have a guess here at the type info of the result of formatting.
							TblQueryValueNode.New(KB.K("Line"),
									new TblQueryExpression(SqlExpression.Select(
										new SqlExpression(POFR.F.AccountingTransactionID).IsNotNull(),
													SqlExpression.Constant(KB.K("Received").Translate()),
										new SqlExpression(POFR.F.POLineID.F.LineNumber).IsNotNull(),
													SqlExpression.Constant("[").Plus(new SqlExpression(POFR.F.POLineID.F.LineNumber).Cast(new Libraries.TypeInfo.StringTypeInfo(1, 100, 0, false, true, true))).Plus(SqlExpression.Constant("]"))
									)),
									Fmt.SetFontSize(Fmt.NamedFontSize.smaller),
									new LabelFormattingArg(Fmt.SetFontSize(Fmt.NamedFontSize.medium))
								)
							, TblServerExprNode.New(dsMB.Path.T.Demand.F.WorkOrderID.Key(),
									SqlExpression.Coalesce(
										new SqlExpression(POFR.F.POLineID.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderID.F.Number),
										new SqlExpression(POFR.F.POLineID.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderID.F.Number)), hideOnReceiveLines)
							// Labor is measured in time, all else is in UoM's or has no units at all.
							// We coalesce the Quantity from the Actual/Receive record, if any, to that of the PO Line, and format time-quantities.
							, TIReports.QuantityNodeForMixedQuantities(KB.K("Quantity"), longQuantityExpression, timeQuantityExpression)
							, TIReports.UoMNodeForMixedQuantities(longQuantityExpression, timeQuantityExpression, POFR.F.POLineID.F.POLineItemID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code)
							, descriptionColumn
							, TIReports.UnitCostNodeForMixedValues(KB.K("Unit Price"), longQuantityExpression, timeQuantityExpression, costCalculation, ViewCostPermissionsFromPath(POFR.F.Id))
							, TblQueryValueNode.New(KB.K("Order Cost"), new TblQueryExpression(SqlExpression.Select(new SqlExpression(POFR.F.AccountingTransactionID).IsNull(), new SqlExpression(POFR.F.POLineID.F.Cost))), FooterAggregateCol.Sum())
							, TblColumnNode.New(KB.K("Actual Cost"), POFR.F.AccountingTransactionID.F.Cost, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum())
							).Concat(CostCenterColumns(POFR.F.CostCenterId)).NodeArray()
						),
						TblServerExprNode.New(dsMB.Path.T.WorkOrder.F.UnitLocationID.Key(),
									SqlExpression.Coalesce(
										new SqlExpression(POFR.F.POLineID.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderID.F.UnitLocationID.F.Code),
										new SqlExpression(POFR.F.POLineID.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderID.F.UnitLocationID.F.Code)), hideOnReceiveLines),
						TblColumnNode.New(dsMB.Path.T.ItemLocation.F.ItemID.F.Code.Key(), POFR.F.POLineID.F.POLineItemID.F.ItemLocationID.F.ItemID.F.Code, hideOnReceiveLines),
						TblColumnNode.New(dsMB.Path.T.ItemLocation.F.ItemID.F.Desc.Key(), POFR.F.POLineID.F.POLineItemID.F.ItemLocationID.F.ItemID.F.Desc, hideOnReceiveLines, DefaultShowInDetailsCol.Hide()),
						TblServerExprNode.New(dsMB.Path.T.ItemLocation.F.LocationID.F.Code.Key(),
									SqlExpression.Select(
										// On the POLineItem we show the location of the ItemLocation
										new SqlExpression(POFR.F.AccountingTransactionID).IsNull(),
											new SqlExpression(POFR.F.POLineID.F.POLineItemID.F.ItemLocationID.F.LocationID.F.Code),
										// On the ReceiveItemPO lines we show the location of the received-to ItemLocation, if it differs from the one on the POLineItem.
										new SqlExpression(POFR.F.AccountingTransactionID.F.ReceiveItemPOID.F.ItemLocationID).NEq(new SqlExpression(POFR.F.POLineID.F.POLineItemID.F.ItemLocationID)),
											new SqlExpression(POFR.F.AccountingTransactionID.F.ReceiveItemPOID.F.ItemLocationID.F.LocationID.F.Code)))
					),
					mainRecordGroupings:
						assignee != null ?
							new TblLeafNode[] {
								TblColumnNode.New(assignee.F.ContactID),
								TblColumnNode.New(PO.F.Number)
							}
							:
							new TblLeafNode[] {
								TblColumnNode.New(PO.F.Number)
							},
					sortingPaths: new DBI_Path[] {
						POFR.F.POLineID.F.LineNumber,
						POFR.F.POLineID.F.PurchaseOrderText,
						POFR.F.POLineID,
						POFR.F.AccountingTransactionID.F.EffectiveDate,
						POFR.F.AccountingTransactionID.F.EntryDate
					}),
				RTblBase.SetChildClassFilter(new SqlExpression(POFR.F.AccountingTransactionID).IsNull(), KB.K("Receipts"), true)
			};
			if (filter != null)
				rtblArgs.Add(RTblBase.SetFilter(filter));
			if (singleRecord) {
				rtblArgs.Add(RTblBase.SetNoUserFilteringAllowed());
				rtblArgs.Add(RTblBase.SetNoUserSortingAllowed());
			}
			if (formLayout) {
				rtblArgs.Add(RTbl.ReportParameter(FormReport.AdditionalBlankLinesLabel, dsMB.Schema.V.POFormAdditionalBlankLines, FormReport.AdditionalBlankLinesParameterID));
				rtblArgs.Add(RTbl.ReportParameter(FormReport.AdditionalInformationLabel, dsMB.Schema.V.POFormAdditionalInformation, FormReport.AdditionalInformationParameterID));
				rtblArgs.Add(RTblBase.SetNoUserGroupingAllowed());
			}
			else {
				rtblArgs.Add(RTblBase.SetPageBreakDefault(true));
			}

			if (assignee != null) {
				rtblArgs.Add(RTblBase.SetNoSearchPrefixing());
				rtblArgs.Add(RTblBase.SetRequiredInvariantForFooterAggregates(assignee));
			}
			List<Tbl.IAttr> iAttr = new List<Tbl.IAttr> {
				PurchasingGroup,
				new PrimaryRecordArg(PO),
				CommonTblAttrs.ViewCostsDefinedBySchema
			};
			if (formLayout)
				iAttr.Add(new RTbl((r, logic) => new Reports.MainBossFormReport(r, logic), typeof(ReportViewerControl), rtblArgs.ToArray()));
			else
				iAttr.Add(new RTbl((r, logic) => new TblDrivenDynamicColumnsReport(r, logic), typeof(ReportViewerControl), rtblArgs.ToArray()));

			var layout = ColumnBuilder.New(PO);
			if (assignee != null) {
				layout.ColumnSection(StandardReport.RightColumnFmtId
					, ContactColumns(assignee.F.ContactID, ShowXIDBold)
					, UserColumnsForContact(assignee.F.ContactID.L.User.ContactID)
				);
			}
			layout.Concat(
				PurchaseOrderColumns(PO, POR, POFR, ShowForPurchaseOrder)
			);
			List<TblActionNode> extraNodes = new List<TblActionNode>();
			if (formLayout)
				extraNodes.Add(ClearSelectForPrintCommand(PO.F.SelectPrintFlag, POl));
			return new Tbl(PO.Table, title, iAttr.ToArray(), layout.LayoutArray(), extraNodes.ToArray());
		}
		public static Tbl PurchaseOrderFormReport = POHistoryOrFormTbl(TId.PurchaseOrder, true, dsMB.Path.T.PurchaseOrderFormReport);
		public static Tbl PurchaseOrderDraftFormReport = POHistoryOrFormTbl(TId.PurchaseOrder, true, dsMB.Path.T.PurchaseOrderFormReport, filter: new SqlExpression(dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsDraft).IsTrue());
		public static Tbl PurchaseOrderIssuedFormReport = POHistoryOrFormTbl(TId.PurchaseOrder, true, dsMB.Path.T.PurchaseOrderFormReport, filter: new SqlExpression(dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsIssued).IsTrue());
		public static Tbl PurchaseOrderIssuedAndAssignedFormReport = POHistoryOrFormTbl(TId.PurchaseOrder, true, dsMB.Path.T.PurchaseOrderFormReport, filter:
				new SqlExpression(dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID)
									.In(new SelectSpecification(
										null,
										new SqlExpression[] { new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID) },
										new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))
												.And(new SqlExpression(dsMB.Path.T.PurchaseOrderAssignment.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsIssued).IsTrue()),
												null).SetDistinct(true)));
		public static Tbl PurchaseOrderClosedFormReport = POHistoryOrFormTbl(TId.PurchaseOrder, true, dsMB.Path.T.PurchaseOrderFormReport, filter: new SqlExpression(dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsClosed).IsTrue());
		public static Tbl PurchaseOrderVoidFormReport = POHistoryOrFormTbl(TId.PurchaseOrder, true, dsMB.Path.T.PurchaseOrderFormReport, filter: new SqlExpression(dsMB.Path.T.PurchaseOrderFormReport.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.FilterAsVoid).IsTrue());

		public static Tbl SinglePurchaseOrderFormReport = POHistoryOrFormTbl(TId.PurchaseOrder.ReportSingle, true, dsMB.Path.T.PurchaseOrderFormReport, singleRecord: true);
		public static Tbl PurchaseOrderByAssigneeFormReport = POHistoryOrFormTbl(TId.PurchaseOrder.ReportByAssignee, true, dsMB.Path.T.PurchaseOrderAssignmentReport.F.PurchaseOrderFormReportID.PathToReferencedRow,
																					dsMB.Path.T.PurchaseOrderAssignmentReport.F.PurchaseOrderAssigneeID.PathToReferencedRow);
		public static Tbl POHistory = POHistoryOrFormTbl(TId.PurchaseOrder.ReportHistory, false, dsMB.Path.T.PurchaseOrderFormReport);
		public static Tbl POHistoryByAssignee = POHistoryOrFormTbl(TId.PurchaseOrder.ReportHistory.ReportByAssignee, false, dsMB.Path.T.PurchaseOrderAssignmentReport.F.PurchaseOrderFormReportID.PathToReferencedRow,
			dsMB.Path.T.PurchaseOrderAssignmentReport.F.PurchaseOrderAssigneeID.PathToReferencedRow);
		#endregion
		#region -   POSummary
		static Tbl POSummaryBase(Tbl.TblIdentification title, dsMB.PathClass.PathToPurchaseOrderRow PO, dsMB.PathClass.PathToPurchaseOrderExtrasRow POR, dsMB.PathClass.PathToPurchaseOrderAssigneeRow assignee = null) {
			var rtblArgs = new List<RTblBase.ICtorArg> {
				RTblBase.SetAllowSummaryFormat(true),
				RTblBase.SetDualLayoutDefault(defaultInColumns: true),
				RTblBase.SetPreferWidePage(true),
				assignee != null ? RTblBase.SetGrouping(assignee.F.ContactID) : null
			};
			if (assignee != null) {
				//rtblArgs.Add(RTblBase.SetNoSearchPrefixing());	// This was in the PO/Req summary by assignee but not the PO one...
				rtblArgs.Add(RTblBase.SetRequiredInvariantForFooterAggregates(assignee));
			}
			List<Tbl.IAttr> iAttr = new List<Tbl.IAttr> {
				PurchasingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(rtblArgs.ToArray())
			};
			var layout = ColumnBuilder.New(PO, PurchaseOrderColumns(PO, POR, null, ShowForPurchaseOrderSummary));

			if (assignee != null) {
				layout.Concat(ContactColumns(assignee.F.ContactID, ShowXIDBold));
				layout.Concat(UserColumnsForContact(assignee.F.ContactID.L.User.ContactID));
			}
			return new Tbl(PO.Table, title, iAttr.ToArray(), layout.LayoutArray());
		}
		public static Tbl POSummary = POSummaryBase(TId.PurchaseOrder.ReportSummary, dsMB.Path.T.PurchaseOrder, dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID);
		public static Tbl POSummaryByAssignee = POSummaryBase(TId.PurchaseOrder.ReportSummary.ReportByAssignee, dsMB.Path.T.PurchaseOrdersWithAssignments.F.PurchaseOrderID.PathToReferencedRow, dsMB.Path.T.PurchaseOrdersWithAssignments.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID,
			dsMB.Path.T.PurchaseOrdersWithAssignments.F.PurchaseOrderAssignmentID.F.PurchaseOrderAssigneeID.PathToReferencedRow);
		#endregion
		#region -   PurchaseOrder Charts
		#region -     POChartBase
		static Tbl POChartBase(Tbl.TblIdentification title, RTbl.BuildReportObjectDelegate reportBuilder, RTblBase.ICtorArg intervalAttribute = null, RTblBase.ICtorArg filterAttribute = null, bool defaultGroupByVendor = false) {
			return GenericChartBase(dsMB.Schema.T.PurchaseOrder, PurchasingGroup, title, reportBuilder,
				ColumnBuilder.New(dsMB.Path.T.PurchaseOrder,
					PurchaseOrderColumns(dsMB.Path.T.PurchaseOrder, dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID)
				).LayoutArray(),
				filterAttribute,
				intervalAttribute,
				defaultGroupByVendor ? RTblBase.SetGrouping(dsMB.Path.T.PurchaseOrder.F.VendorID) : null);
		}
		#endregion
		#region -     PurchaseOrder Count charts
		static Tbl PurchaseOrderChartCountBase(Tbl.TblIdentification title, DBI_Path groupingPath, ReportViewLogic.IntervalSettings? groupingInterval) {
			return POChartBase(title, (Report r, ReportViewLogic logic) => new Reports.POChartCountBase(r, logic, groupingPath), RTblBase.SetChartIntervalGrouping(groupingInterval), defaultGroupByVendor: groupingPath == null);
		}
		public static Tbl POChartCountByCreatedDate = PurchaseOrderChartCountBase(TId.POChartCountByCreatedDate, dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.CreatedDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl POChartCountByIssuedDate = PurchaseOrderChartCountBase(TId.POChartCountByIssuedDate, dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.IssuedDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl POChartCountByEndedDate = PurchaseOrderChartCountBase(TId.POChartCountByEndedDate, dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.EndedDateIfEnded, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl POChartCount = PurchaseOrderChartCountBase(TId.POChartCount, (DBI_Path)null, null);
		#endregion
		#region -     PurchaseOrder time-span charts
		static Tbl POChartDurationBase(Tbl.TblIdentification title, TblLeafNode valueNode, Expression.Function aggregateFunction, ReportViewLogic.IntervalSettings intervalValueScaling, SqlExpression filter) {
			return POChartBase(title, (Report r, ReportViewLogic logic) => new Reports.POChartDurationBase(r, logic, valueNode, aggregateFunction), RTblBase.SetChartIntervalValueScaling(intervalValueScaling), filterAttribute: RTblBase.SetFilter(filter), defaultGroupByVendor: true);
		}
		public static Tbl POChartAverageDuration = POChartDurationBase(TId.POChartAverageDuration,
			TblColumnNode.New(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.InIssuedDuration), Expression.Function.Avg, ReportViewLogic.IntervalSettings.Days, new SqlExpression(dsMB.Path.T.PurchaseOrder.F.Id.L.PurchaseOrderExtras.PurchaseOrderID.F.InIssuedDuration).IsNotNull());
		#endregion
		public static Tbl POChartLifetime = POChartBase(TId.POChartLifetime, (Report r, ReportViewLogic logic) => new Reports.POChartLifetime(r, logic));
		#endregion
		#endregion
		#region Miscellaneous
		public static Tbl MiscellaneousReport = new Tbl(dsMB.Schema.T.Miscellaneous,
			TId.MiscellaneousItem,
			new Tbl.IAttr[] {
				PurchasingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.Miscellaneous,
					MiscellaneousColumns(dsMB.Path.T.Miscellaneous, ShowAll)
			).LayoutArray()
		);
		#endregion
		#region PurchaseOrderTemplate
		public static Tbl PurchaseOrderTemplateReport = new Tbl(dsMB.Schema.T.PurchaseOrderTemplateReport,
			TId.PurchaseOrderTemplate,
			new Tbl.IAttr[] {
				PurchasingGroup,
				new PrimaryRecordArg(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.PathToReferencedRow),
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetPageBreakDefault(true),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.Code),
					RTblBase.SetParentChildInformation(KB.K("Order Lines"),
						RTblBase.ColumnsRowType.JoinWithChildren,
						dsMB.Path.T.PurchaseOrderTemplateReport.F.POLineTemplateID,
						ColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport
							, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.Code)
							, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderText)
							, TIReports.QuantityNodeForMixedQuantities(dsMB.Path.T.PurchaseOrderTemplateReport.F.Quantity.Key(), dsMB.Path.T.PurchaseOrderTemplateReport.F.Quantity, dsMB.Path.T.PurchaseOrderTemplateReport.F.Labor)
							, TIReports.UoMNodeForMixedQuantities(
								new SqlExpression(dsMB.Path.T.PurchaseOrderTemplateReport.F.Quantity),
								new SqlExpression(dsMB.Path.T.PurchaseOrderTemplateReport.F.Labor),
								dsMB.Path.T.PurchaseOrderTemplateReport.F.UnitOfMeasureID.F.Code)
							, ItemColumns(dsMB.Path.T.PurchaseOrderTemplateReport.F.Item)
							, MiscellaneousColumns(dsMB.Path.T.PurchaseOrderTemplateReport.F.MiscellaneousID)
							, OtherWorkOutsideColumns(dsMB.Path.T.PurchaseOrderTemplateReport.F.OtherWorkOutsideID)
							, LaborOutsideColumns(dsMB.Path.T.PurchaseOrderTemplateReport.F.LaborOutsideID)
						).LayoutArray(),
						null,
						new DBI_Path[] {
							dsMB.Path.T.PurchaseOrderTemplateReport.F.POLineTemplateID.F.LineNumberRank,
							dsMB.Path.T.PurchaseOrderTemplateReport.F.Code
						}
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport)
					.ColumnSection(StandardReport.LeftColumnFmtId
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.Code, FieldSelect.ShowAlways)
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.Desc, FieldSelect.ShowAlways)
						, VendorColumns(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.VendorID, ShowXID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.PurchaseOrderCategoryID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.PaymentTermID, ShowXID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.ShippingModeID, ShowXID)
					)
					.ColumnSection(StandardReport.RightColumnFmtId
						, CodeDescColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.PurchaseOrderStateID, ShowXID)
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.RequiredByInterval, FieldSelect.ShowAlways)
						, TblQueryValueNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.ShipToLocationID.Key(), LocationDetailExpression(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.ShipToLocationID.PathToReferencedRow, includeCode: true), Fmt.SetFontStyle(System.Drawing.FontStyle.Bold), FieldSelect.ShowAlways)
						, CodeDescColumnBuilder.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.ProjectID)
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.SelectPrintFlag, FieldSelect.ShowAlways)
					)
					.ColumnSection(StandardReport.LVPRowsFmtId
					)
					.ColumnSection(StandardReport.MultiLineRowsFmtId
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.Subject, Fmt.SetFontStyle(System.Drawing.FontStyle.Bold), FieldSelect.ShowAlways)
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.Comment, Fmt.SetMonospace(true), FieldSelect.ShowAlways)
						, TblColumnNode.New(dsMB.Path.T.PurchaseOrderTemplateReport.F.PurchaseOrderTemplateID.F.CommentToVendor, Fmt.SetMonospace(true), FieldSelect.ShowAlways)
			).LayoutArray()
		);
		#endregion
		#region InventoryOnOrder
		// TODO: Calculations of unit cost remaining on order, unit cost of original order, unit cost of what's received should be done using query expressions.
		// TODO: Because of the filter on the PO state, all the relevant PO's are active and so there should be no checkbox for filtering out Inactive records.
		// TODO: Because of the filter on the PO state, the filter control in the UI offers states to filter on that could never produce results. Somehow we want the filter
		//		expressed here to be passed to the filter control so it can apply it when picking any records along the path (PO, PO State).
		public static Tbl InventoryOnOrder = new Tbl(dsMB.Schema.T.POLineItem,
			TId.ItemOnOrder,
			new Tbl.IAttr[] {
				PurchasingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.F.CurrentPurchaseOrderStateHistoryID.F.PurchaseOrderStateID.F.OrderCountsActive)),
					RTblBase.SetGrouping(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.POLineItem
					, ItemColumns(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ItemID, ShowXIDAndUOM)
					, ActualItemLocationColumns(dsMB.Path.T.POLineItem.F.ItemLocationID.F.ActualItemLocationID, ShowXID)
					, PurchaseOrderColumns(dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID, dsMB.Path.T.POLineItem.F.POLineID.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID, null, ShowXID)
					, TblColumnNode.New(dsMB.Path.T.POLineItem.F.Quantity, FieldSelect.ShowAlways)  // TODO: Sum if group is single item
					, TblColumnNode.New(dsMB.Path.T.POLineItem.F.POLineID.F.Cost, FooterAggregateCol.Sum(), FieldSelect.ShowAlways)
					, TblColumnNode.New(dsMB.Path.T.POLineItem.F.ReceiveQuantity, DefaultShowInDetailsCol.Show())   // TODO: Sum if group is single item
					, TblColumnNode.New(dsMB.Path.T.POLineItem.F.POLineID.F.ReceiveCost, FooterAggregateCol.Sum(), FieldSelect.ShowAlways)
					, TblServerExprNode.New(SameKeyContextAs.K(KB.K("Quantity Pending"), dsMB.Path.T.POLineItem.F.POLineID),
						SqlExpression.Select(
							new SqlExpression(dsMB.Path.T.POLineItem.F.Quantity).GEq(new SqlExpression(dsMB.Path.T.POLineItem.F.ReceiveQuantity)),
								new SqlExpression(dsMB.Path.T.POLineItem.F.Quantity).Minus(new SqlExpression(dsMB.Path.T.POLineItem.F.ReceiveQuantity)),
								SqlExpression.Constant(0)
						), DefaultShowInDetailsCol.Show())  // TODO: Sum if group is single item
					, TblServerExprNode.New(SameKeyContextAs.K(KB.K("Cost Pending"), dsMB.Path.T.POLineItem.F.POLineID),
						SqlExpression.Select(
							new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.Cost).GEq(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.ReceiveCost)),
								new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.Cost).Minus(new SqlExpression(dsMB.Path.T.POLineItem.F.POLineID.F.ReceiveCost)),
								SqlExpression.Constant(0m, dsMB.Path.T.POLineItem.F.POLineID.F.ReceiveCost.ReferencedColumn.EffectiveType)
						), DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum())
			).LayoutArray()
		);
		#endregion
		#region Receiving
		private static Tbl MakeReceivingTbl(FeatureGroup controlGroup) {
			return new Tbl(dsMB.Schema.T.ReceivingReport,
			TId.ReceiptActivity,
			new Tbl.IAttr[] {
				controlGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					// TODO: The layout of these filters is not ideal. It would be nice to get unlabelled group boxes around each group or something like that.
					// Filters for PO/Non-PO
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ReceivingReport.F.POLineID).IsNotNull(), KB.K("Non-PO Receiving"), false, PurchasingGroup, false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ReceivingReport.F.POLineID).IsNull(), KB.K("PO Receiving"), false, PurchasingGroup, true),
					// Filters on resource (item, Labor, Other Work, Miscellaneous)
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ReceivingReport.F.ItemLocationID).IsNull(), KB.K("Item Receiving"), false, ItemResourcesGroup | StoreroomGroup, true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ReceivingReport.F.LaborOutsideID).IsNull(), KB.K("Labor Receiving"), false, LaborResourcesGroup, true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ReceivingReport.F.OtherWorkOutsideID).IsNull(), KB.K("Other Work Receiving"), false, LaborResourcesGroup, true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ReceivingReport.F.MiscellaneousID).IsNull(), KB.K("Miscellaneous Receiving"), false, PurchasingGroup, true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ReceivingReport,
				  PurchaseOrderColumns(dsMB.Path.T.ReceivingReport.F.ReceiptID.F.PurchaseOrderID, dsMB.Path.T.ReceivingReport.F.ReceiptID.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID, null, ShowXID)
				, TblColumnNode.New(dsMB.Path.T.ReceivingReport.F.ReceiptID.F.Waybill)
				, AccountingTransactionColumns(dsMB.Path.T.ReceivingReport.F.AccountingTransactionID, ShowXID)
				// An expression for the unified quantity (TBD)
				, TblColumnNode.New(dsMB.Path.T.ReceivingReport.F.QuantityCount, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.ReceivingReport.F.QuantityDuration, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.ReceivingReport.F.UnitCost, DefaultShowInDetailsCol.Show())
				// The unified resource identification
				, TblColumnNode.New(dsMB.Path.T.ReceivingReport.F.Code, DefaultShowInDetailsCol.Show())
				// The purchased resource information, for Items
				, ItemColumns(dsMB.Path.T.ReceivingReport.F.ItemLocationID.F.ItemID)
				, TblColumnNode.New(dsMB.Path.T.ReceivingReport.F.ItemLocationID.F.LocationID.F.Code)
				// The purchased resource information, for Labor or OtherWork
				, WorkOrderColumns(dsMB.Path.T.ReceivingReport.F.WorkOrderID)
				, TradeColumns(dsMB.Path.T.ReceivingReport.F.TradeID)
				// The purchased resource information, for Labor
				, LaborOutsideColumns(dsMB.Path.T.ReceivingReport.F.LaborOutsideID)
				// The purchased resource information, for OtherWork
				, OtherWorkOutsideColumns(dsMB.Path.T.ReceivingReport.F.OtherWorkOutsideID)
				// The purchased resource information, for Miscellaneous
				, MiscellaneousColumns(dsMB.Path.T.ReceivingReport.F.MiscellaneousID)
			).LayoutArray()
		);
		}
		// TODO: This report is schizophrenic: It is the "Print" button for the Receipt Tbl but it listed as "Information on item receipts" it its own control panel node TId.ItemReceiving
		// The original custom report also seemed confused, as it handled both labor and items in terms of showing quantity but never gave any details about anything but items, and also
		// would include both PO and non-PO receiving.
		// I expect there should be two reports (or more) here: One for the Receipt Tbl which includes all record types but only PO receiving, another for Item receiving which shows only Items
		// but includes both PO and non-PO. Or perhaps there should be one report for only PO receiving (allowing useful grouping by Receipt and/or PO) and another for all receiving (with
		// filter options for PO and non-PO) and both reports having filter options for Item/Labor/Other Work/Miscellaneous.
		// Interim: Split the identical report with identical Tbl identification, with 3 different control groups (which also set the default filters to the corresponding controlGroup).
		// This allows the three reports to appear in WorkOrders, Items and Purchasing as part of the same controlGroup so the reports do NOT appear if a license for WorkOrders, Inventory and/or 
		// Purchasing is not present.
		public static Tbl PurchaseReceiving = MakeReceivingTbl(PurchasingGroup);
		public static Tbl ItemReceiving = MakeReceivingTbl(StoreroomGroup);
		public static Tbl ResourceReceiving = MakeReceivingTbl(ItemResourcesGroup);
		#endregion
		#region Receipt
		private static Tbl ReceiptReportTbl() {
			var ReceivingReport = dsMB.Path.T.ReceiptReport.F.AccountingTransactionID.L.ReceivingReport.AccountingTransactionID;
			var OQIHNode = TIReports.QuantityNodeForMixedQuantities(KB.K("Original Ordered Quantity"), ReceivingReport.F.OriginalOrderedQuantity, ReceivingReport.F.OriginalOrderedLabor, DefaultShowInDetailsCol.Hide());
			var QIHNode = TIReports.QuantityNodeForMixedQuantities(KB.K("Quantity"), ReceivingReport.F.QuantityCount, ReceivingReport.F.QuantityDuration, DefaultShowInDetailsCol.Show());
			var UOMNode = TIReports.UoMNodeForMixedQuantities(
				SqlExpression.Coalesce(new SqlExpression(ReceivingReport.F.QuantityCount), new SqlExpression(ReceivingReport.F.OriginalOrderedQuantity)),
				SqlExpression.Coalesce(new SqlExpression(ReceivingReport.F.QuantityDuration), new SqlExpression(ReceivingReport.F.OriginalOrderedLabor)),
				ReceivingReport.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code, DefaultShowInDetailsCol.Show());

			return new Tbl(dsMB.Schema.T.ReceiptReport,
				TId.Receipt,
				new Tbl.IAttr[] {
					PurchasingGroup,
					new PrimaryRecordArg(dsMB.Path.T.ReceiptReport.F.ReceiptID.PathToReferencedRow),
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
						RTblBase.SetPageBreakDefault(true),
						RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.Waybill),
						RTblBase.SetAllowSummaryFormat(false),
						RTblBase.SetPreferWidePage(false),
						RTblBase.SetParentChildInformation(KB.TOi(TId.ReceiptActivity),
							RTblBase.ColumnsRowType.UnionWithChildren,
							dsMB.Path.T.ReceiptReport.F.AccountingTransactionID,
							ColumnBuilder.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.PathToReferencedRow
								, TblColumnNode.New(ReceivingReport.F.Code, DefaultShowInDetailsCol.Show())
								, ItemColumns(ReceivingReport.F.ItemLocationID.F.ItemID)
								, TblColumnNode.New(ReceivingReport.F.ItemLocationID.F.LocationID.F.Code, DefaultShowInDetailsCol.Hide())
								, LaborOutsideColumns(ReceivingReport.F.LaborOutsideID)
								, OtherWorkOutsideColumns(ReceivingReport.F.OtherWorkOutsideID)
								, MiscellaneousColumns(ReceivingReport.F.MiscellaneousID)
								, WorkOrderColumns(ReceivingReport.F.WorkOrderID)
								, OQIHNode
								, QIHNode
								, UOMNode
								, TblColumnNode.New(ReceivingReport.F.POLineID.F.Cost, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum())
								, TblColumnNode.New(ReceivingReport.F.UnitCost, DefaultShowInDetailsCol.Hide())
								, AccountingTransactionColumns(ReceivingReport.F.AccountingTransactionID)
							).LayoutArray(),
							mainRecordGroupings: new TblLeafNode[] {
								// TODO (W20150190): This grouping correctly puts multiple receipts with the same waybill separately but there is only one document map entry
								// for the entire set of them. For this to work the way we want, we need the grouping generated by the first node (Waybill) to have
								// no document map entry. This will mean the waybill number will appear as part of the document map entry for the second grouping instead,
								// which is a true per-record grouping but which entirely suppresses display of its own grouping information.
								TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.Waybill),	// Ideally we want DocumentMapEntry: false on this one
								TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID, new FormatCol((e) => Expression.Null), RFmt.SetNoValueText(null))
							},
							sortingPaths: new DBI_Path[] {
								ReceivingReport.F.ItemLocationID.F.ItemID.F.Code,
								ReceivingReport.F.AccountingTransactionID.F.EffectiveDate
							}
						)
					)
				},
				ColumnBuilder.New(dsMB.Path.T.ReceiptReport)
						.ColumnSection(StandardReport.LeftColumnFmtId
							, DateHHMMColumnNode(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.EntryDate, FieldSelect.ShowAlways)
							, TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.Waybill, FieldSelect.ShowAlways)
							, TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.Desc, FieldSelect.ShowAlways)
							, TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.Reference, FieldSelect.ShowAlways)
							, TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.TotalReceive, FieldSelect.ShowNever)
						)
						.ColumnSection(StandardReport.RightColumnFmtId
							, PurchaseOrderColumns(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.PurchaseOrderID, dsMB.Path.T.ReceiptReport.F.ReceiptID.F.PurchaseOrderID.L.PurchaseOrderExtras.Id, null, ShowXID)
						)
						.ColumnSection(StandardReport.MultiLineRowsFmtId
							, TblColumnNode.New(dsMB.Path.T.ReceiptReport.F.ReceiptID.F.Comment, FieldSelect.ShowAlways)
				).LayoutArray()
			);
		}
		public static Tbl ReceiptReport = ReceiptReportTbl();
		#endregion
		#region PurchaseOrderAssignee
		public static Tbl PurchaseOrderAssigneeReport = new Tbl(dsMB.Schema.T.PurchaseOrderAssignee,
			TId.PurchaseOrderAssignee,
			new Tbl.IAttr[] {
				PurchasingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.PurchaseOrderAssignee
				, ContactColumns(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID, ShowXIDBold)
				, UserColumnsForContact(dsMB.Path.T.PurchaseOrderAssignee.F.ContactID.L.User.ContactID)
				, TblColumnNode.New(dsMB.Path.T.PurchaseOrderAssignee.F.Id.L.PurchaseOrderAssigneeStatistics.PurchaseOrderAssigneeID.F.NumNew, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.PurchaseOrderAssignee.F.Id.L.PurchaseOrderAssigneeStatistics.PurchaseOrderAssigneeID.F.NumInProgress, DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region - Purchase Order State History
		#region -   POStateHistory
		public static Tbl POStateHistory = new Tbl(dsMB.Schema.T.PurchaseOrderStateHistory,
			TId.PurchaseOrderStateHistory,
			new Tbl.IAttr[] {
				PurchasingGroup,
				new PrimaryRecordArg(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.PathToReferencedRow),
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.F.Number),
					// Set No Search Prefixing because the primary path (and the root of all paths in this report) include a labelled reference to the WO from the WOSH record.
					RTblBase.SetNoSearchPrefixing(),
					PurchaseOrderStateHistoryChildDefinition(dsMB.Path.T.PurchaseOrderStateHistory)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.PurchaseOrderStateHistory
				, PurchaseOrderColumns(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID, null, ShowXID)
			).LayoutArray()
		);

		#endregion
		#region -   POStateHistorySummary
		public static Tbl POStateHistorySummary = new Tbl(dsMB.Schema.T.PurchaseOrderStateHistory,
			TId.PurchaseOrderStateHistory.ReportSummary,
			new Tbl.IAttr[] {
				PurchasingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.PurchaseOrderStateHistory
				, PurchaseOrderColumns(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID, null, ShowXID)
				, PurchaseOrderStateHistoryAndExtraColumns(dsMB.Path.T.PurchaseOrderStateHistory)
			).LayoutArray()
		);
		#endregion
		#region -   POChartStatus
		// Because this chart shows averages per request, we need several special provisions:
		// 1 - We can't allow user filtering by State History record because this might hide some requestss (which should report as an average of zero)
		// 2 - We must only look at draft/open time; otherwise old work orders will show up as spending an average of huge amounts of time in whatever their final Status is.
		// All work orders will have at least one State History record (from when they were created) so the data is available to count the work orders in each user-defined
		// grouping.
		// Although grouping by properties of the Work Order would be meaningful, the method whereby charts are generated for the groups does not allow the count of
		// work orders in the group to be determined (the scope for the CountDistinct has to be different for each group) so for now we don't allow user grouping.
		public static Tbl POChartStatus = new Tbl(dsMB.Schema.T.PurchaseOrderStateHistory,
			TId.POChartStatus,
			new Tbl.IAttr[] {
				PurchasingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl( typeof(Thinkage.MainBoss.Controls.Reports.PurchaseOrderChartStatusReport), typeof(ReportViewerControl),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.F.FilterAsDraft)
									.Or(new SqlExpression(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderStateID.F.FilterAsIssued))),
					RTblBase.SetChartIntervalValueScaling(ReportViewLogic.IntervalSettings.Days),
					RTblBase.SetNoUserFieldSelectionAllowed(),
					RTblBase.SetNoUserGroupingAllowed()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.PurchaseOrderStateHistory,
				PurchaseOrderColumns(dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID, dsMB.Path.T.PurchaseOrderStateHistory.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID)
			).LayoutArray()
		);
		#endregion
		#endregion
		#endregion

		#region Inventory
		#region ItemReport
		public static Tbl ItemReport = new Tbl(dsMB.Schema.T.Item,
			TId.Item,
			new Tbl.IAttr[] {
				ItemsDependentGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.Item
				, ItemColumns(dsMB.Path.T.Item, ShowForItem)
			).LayoutArray()
		);
		#endregion
		#region ItemPricing
		public static Tbl ItemPricing = new Tbl(dsMB.Schema.T.ItemPrice,
			TId.ItemPricing,
			new Tbl.IAttr[] {
				ItemsDependentGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ItemPrice
				, ItemPriceColumns(dsMB.Path.T.ItemPrice, ShowForItemPrice)
			).LayoutArray()
		);
		#endregion
		#region StorageLocationStatus
		public static Tbl StorageLocationStatus = new Tbl(dsMB.Schema.T.ActualItemLocation,
			TId.ItemLocation,
			new Tbl.IAttr[] {
				ItemsDependentGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					// need to provide choices for InventoryGroup for both permanent and temporary storage. If no Inventory, then always just apply temporary
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ActualItemLocation.F.PermanentItemLocationID).IsNull(), dsMB.Schema.T.PermanentItemLocation.LabelKey, false, InventoryGroup, true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ActualItemLocation.F.TemporaryItemLocationID).IsNull(), dsMB.Schema.T.TemporaryItemLocation.LabelKey, false, InventoryGroup, false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ActualItemLocation.F.OnHand).NEq(SqlExpression.Constant(0)), KB.K("Empty Storage Assignments"), false),
					RTblBase.SetGrouping(dsMB.Path.T.ActualItemLocation.F.ItemLocationID.F.LocationID)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualItemLocation,
				ItemColumns(dsMB.Path.T.ActualItemLocation.F.ItemLocationID.F.ItemID, ShowXIDAndUOM),
				ActualItemLocationColumns(dsMB.Path.T.ActualItemLocation, ShowXIDAndOnHand),
				TblColumnNode.New(dsMB.Path.T.ActualItemLocation.F.Id.L.ItemLocationReport.ActualItemLocationID.F.SuggestedRestockingQTY, DefaultShowInDetailsCol.Show()),
				TblServerExprNode.New(KB.K("Physical Count"), SqlExpression.Constant("______"))   // Instead we should have a null value, and use Fmt attributes to force minimum width and underlining or a box (giving more vertical space)
			).LayoutArray()
		);
		#endregion

		#region Inventory Activity
		private static object UnitCostClientCalculation(object[] values) {
			if (values[0] == null || values[1] == null)
				return null;
			long quantity = (long)IntegralTypeInfo.AsNativeType(values[0], typeof(long));
			if (Compute.IsZero<long>(quantity))
				return null;
			decimal total = (decimal)CurrencyTypeInfo.AsNativeType(values[1], typeof(decimal));
			return checked(Compute.Divide(total, quantity));
		}
		public static Tbl InventoryActivity = new Tbl(dsMB.Schema.T.ItemActivityReport,
			TId.ItemActivity,
			new Tbl.IAttr[] {
				InventoryGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true),RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.ItemActivityReport,
					ItemColumns(dsMB.Path.T.ItemActivityReport.F.ItemLocationID.F.ItemID, ShowXIDAndUOM)
				, ActualItemLocationColumns(dsMB.Path.T.ItemActivityReport.F.ItemLocationID.F.ActualItemLocationID, ShowXID)
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.ActivityType, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.IsCorrection)
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.ReasonReference, DefaultShowInDetailsCol.Show())
				, TblServerExprNode.New(KB.K("Contra"),
					SqlExpression.Select(new SqlExpression(dsMB.Path.T.ItemActivityReport.F.ActivityType).Eq(SqlExpression.Constant((int)ViewRecordTypes.ItemActivityReport.ItemTransferTo)),
						new SqlExpression(dsMB.Path.T.ItemActivityReport.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.FromReference),
						new SqlExpression(dsMB.Path.T.ItemActivityReport.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.ToReference)), DefaultShowInDetailsCol.Show())
				, DateHHMMColumnNode(dsMB.Path.T.ItemActivityReport.F.AccountingTransactionID.F.EffectiveDate, DefaultShowInDetailsCol.Show())
				, DateHHMMColumnNode(dsMB.Path.T.ItemActivityReport.F.AccountingTransactionID.F.EntryDate)
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.QuantityChange, DefaultShowInDetailsCol.Show())    // TODO: Total if homogeneous Item
				, TblQueryValueNode.New(KB.K("Unit Cost of Change"),
					TblQueryCalculation.New(UnitCostClientCalculation, ItemUnitCostTypeOnServer,
						new TblQueryPath(dsMB.Path.T.ItemActivityReport.F.QuantityChange),
						new TblQueryPath(dsMB.Path.T.ItemActivityReport.F.CostChange)
					))
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.CostChange, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum())
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.ResultingQuantity, DefaultShowInDetailsCol.Show())
				, TblQueryValueNode.New(KB.K("Resulting Unit Cost"),
					TblQueryCalculation.New(UnitCostClientCalculation, ItemUnitCostTypeOnServer,
						new TblQueryPath(dsMB.Path.T.ItemActivityReport.F.ResultingQuantity),
						new TblQueryPath(dsMB.Path.T.ItemActivityReport.F.ResultingCost)
					))
				, TblColumnNode.New(dsMB.Path.T.ItemActivityReport.F.ResultingCost, DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region Inventory Usage
		public static Tbl InventoryUsageAsParts = new Tbl(dsMB.Schema.T.SparePart,
			TId.ItemUsageAsParts,
			new Tbl.IAttr[] {
				InventoryGroup | PartsGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true),
					RTbl.IncludeBarCodeSymbologyControl(),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.SparePart,
					ItemColumns(dsMB.Path.T.SparePart.F.ItemID, ShowXIDAndUOM),
					TblColumnNode.New(dsMB.Path.T.SparePart.F.Quantity, DefaultShowInDetailsCol.Show()),
					UnitColumns(dsMB.Path.T.SparePart.F.UnitLocationID, ShowXID)
			).LayoutArray()
		);
		#endregion
		#region InventoryRestocking
		private static readonly RecordTypeClassifierByLinkages RestockingSourceTypes
			= new RecordTypeClassifierByLinkages(true,
				new Tuple<DBI_Path, Key>(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID, KB.TOi(TId.StoreroomAssignment)),
				new Tuple<DBI_Path, Key>(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID, KB.TOi(TId.TemporaryStorageAssignment)),
				new Tuple<DBI_Path, Key>(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID, KB.K("Price Quote from Vendor")),
				new Tuple<DBI_Path, Key>(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID, KB.K("Previous Purchase from Vendor (no PO)")),
				new Tuple<DBI_Path, Key>(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID, KB.K("Previous Purchase from Vendor (with PO)")));
		public class UnitCostCallee : SqlExpression.INamedCallable, SqlExpression.ICallableStaticResultType {
			private UnitCostCallee() {
			}
			public string Name {
				get {
					return StaticName;
				}
			}
			public static readonly string StaticName = KB.I("dbo.mbfn_CalculateUnitCost");

			public TypeInfo GetResultType() {
				// TODO: Perhaps there should be a better way to get this information!
				return dsMB.Schema.T.ActualItemLocation.F.UnitCost.EffectiveType;
			}
			public static readonly UnitCostCallee Instance = new UnitCostCallee();
		}
		public static Tbl InventoryRestocking = new Tbl(dsMB.Schema.T.ItemRestockingReport,
			TId.ItemRestocking,
			new Tbl.IAttr[] {
				InventoryGroup,	// TODO: THis could also be argued to belong in the ResourceGroup which allows creation of temp stroage assignments. This browser would help fulfill ordering of such items.
								// This would add complexity to the main-record filter definitions, to handle all 3 cases in which this report is visible (and in fact the defaults may not be possible to express because they depend on
								// which feature groups are present. Only if both InventoryGroup and ResourcesGroup are enabled do we need to show any checkboxes. Otherwise they are both hidden and the one whose feature group is enabled
								// must default to not apply the filter.
				new PrimaryRecordArg(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.PathToReferencedRow),
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ItemID.F.Hidden).IsNull()),	// TODO: This is a wart that should go away once we reconcile records referring to hidden records.
					// Parent-row filter for Permanent IL's. Hide if no resource group, default no filter
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID).IsNull(), KB.TOc(TId.StoreroomAssignment), false, ItemResourcesGroup, false), 
					// Parent-row filter for temp IL's. Hide if no resource group, default filter
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID).IsNull(), KB.TOc(TId.TemporaryStorageAssignment), false, ItemResourcesGroup, true), 
					// Child-row filter for Permanent IL's. Hide if no inventory group, default false
					RTblBase.SetChildClassFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID).IsNull(), KB.TOc(TId.StoreroomAssignment), false, InventoryGroup, true), 
					// Child-row filter for temp IL's. Hide if no resource group, default false
					RTblBase.SetChildClassFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID).IsNull(), KB.TOc(TId.TemporaryStorageAssignment), false, ItemResourcesGroup, true), 
					// Child-row filter for price quotes
					RTblBase.SetChildClassFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID).IsNull(), KB.TOi(TId.ItemPricing), false, ItemsDependentGroup, true), 
					// Child-row filter for non-po purchases
					RTblBase.SetChildClassFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID).IsNull(), KB.K("Previous Purchase (no PO)"), false, InventoryGroup | ItemResourcesGroup, true), 
					// Child-roww filter for po purchases.
					RTblBase.SetChildClassFilter(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID).IsNull(), KB.K("Previous Purchase (with PO)"), false, PurchasingAndInventoryGroup, true), 
					// TODO: Hidden/Inactive filter on child records
					RTblBase.SetGrouping(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ItemID),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.LocationID.F.Code),
					RTblBase.SetParentChildInformation(KB.K("Potential Restocking Sources"),
						RTblBase.ColumnsRowType.UnionWithChildren,
						new TblQueryExpression(SqlExpression.Coalesce(
							new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID),
							new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID),
							new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID)
						)),
						new TblLayoutNodeArray(
							// TODO: Unit Cost, Total Cost
							TblServerExprNode.New(KB.K("Source Type"), RestockingSourceTypes.RecordTypeExpression, Fmt.SetEnumText(RestockingSourceTypes.EnumValueProvider), DefaultShowInDetailsCol.Show()),
							TblServerExprNode.New(KB.K("Source"), SqlExpression.Coalesce(
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.LocationID.F.Code),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID.F.VendorID.F.Code),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.VendorID.F.Code),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID.F.POLineItemID.F.POLineID.F.PurchaseOrderID.F.VendorID.F.Code)
								), DefaultShowInDetailsCol.Show()
							),
							TblServerExprNode.New(KB.K("Quantity"), SqlExpression.Coalesce(
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.Available),// TODO: Use lesser of on-hand and available
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID.F.Quantity),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedQuantity),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectedQuantity)
								), DefaultShowInDetailsCol.Show()
							),
							TblServerExprNode.New(KB.K("Unit Cost"), SqlExpression.Coalesce(
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.UnitCost),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID.F.UnitCost),
									SqlExpression.Call(UnitCostCallee.Instance, new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedCost), new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedQuantity), SqlExpression.Constant(1)),
									SqlExpression.Call(UnitCostCallee.Instance, new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectedCost), new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectedQuantity), SqlExpression.Constant(1))
								), DefaultShowInDetailsCol.Show()
							)
#if MAYBE_NOT	
							// The total cost of the supply is sort of deceptive and should probbly not be shown
							// What we really want is the Unit Cost times the restocking amount (which should be passed to UnitCostCallee as a third argument instead of 1, and the result rounded to whole cents)
							// but the restocking amount depends on whether the user wants to restock to min or to max. Also, for AIL sources, the source's quantity must be used if less than the restocking quantity.
							// In such cases it is also only a budgetary cost, and not an actual cost to the organization as a purchase of more items would be, so it is also entirely unclear how the costs should be totalled over
							// the entire report.
							,
							TblServerExprNode.New(KB.K("Total Cost"), SqlExpression.Coalesce(
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.SupplyingItemLocationID.F.ActualItemLocationID.F.TotalCost),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ItemPriceID.F.Cost),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemNonPOID.F.CorrectedCost),
									new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.AccountingTransactionID.F.ReceiveItemPOID.F.CorrectedCost)
								), DefaultShowInDetailsCol.Show()
							)
#endif
						),
						mainRecordGroupings: new TblLeafNode[] {
							TblColumnNode.New(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ItemID.F.Code),
							TblColumnNode.New(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ItemID.F.Hidden),
							TblColumnNode.New(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.LocationID.F.Code)
						},
						sortingValues: new TblQueryValue[] {
							new TblQueryPath(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID),
							new TblQueryExpression(RestockingSourceTypes.RecordTypeExpression),
							// Source IL's should be sorted by compound code in Location order (in any particular parent record they will all have the same Item)
							// Price quotes should be sorted by date, then vendor, then unit cost?
							// Previous purchasing should be sorted by date (latest first), then vendor, then unit cost?
						}
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualItemLocation,
				ItemColumns(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ItemID, ShowXIDAndUOM),
				ActualItemLocationColumns(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID, ShowXID),
				TblServerExprNode.New(KB.K("Shortage Quantity"), new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID.F.EffectiveMinimum).Minus(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID.F.Available)), DefaultShowInDetailsCol.Show()),
				TblServerExprNode.New(KB.K("Suggested Restocking Quantity"), new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID.F.EffectiveMaximum).Minus(new SqlExpression(dsMB.Path.T.ItemRestockingReport.F.ShortStockedItemLocationID.F.ActualItemLocationID.F.Available)), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region TemporaryInventoryLocation
		public static Tbl TemporaryInventoryLocation = new Tbl(dsMB.Schema.T.TemporaryItemLocation,
			TId.ItemsInTemporaryStorage,
			new Tbl.IAttr[] {
				InventoryGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true),RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.TemporaryItemLocation,
				ItemColumns(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, ShowXIDAndUOM),
				ActualItemLocationColumns(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.PathToReferencedRow, ShowXIDAndOnHand, TILocations.AllTemporaryStoragePickerTblCreator),
				TblColumnNode.New(dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemPriceID.F.UnitCost),
				WorkOrderColumns(dsMB.Path.T.TemporaryItemLocation.F.WorkOrderID, ShowXID)
			).LayoutArray()
		);
		#endregion
		#region PermanentInventoryLocation
		public static Tbl PermanentInventoryLocation = new Tbl(dsMB.Schema.T.PermanentItemLocation,
			TId.ItemLocation,
			new Tbl.IAttr[] {
				InventoryGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTbl.IncludeBarCodeSymbologyControl(),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.OnHand).NEq(SqlExpression.Constant(0)), KB.K("Empty Storage Assignments"), false),
					RTblBase.SetGrouping(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.PermanentItemLocation,
				ItemColumns(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, ShowXIDAndUOM),
				TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ExternalTag),
				TblQueryValueNode.New(KB.K("External Tag Bar Code"), ValueAsBarcode(dsMB.Path.T.PermanentItemLocation.F.ExternalTag), Fmt.SetUsage(DBI_Value.UsageType.Image)),
				TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.Minimum, DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.Maximum, DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID.L.LocationReport.LocationID.F.OrderByRank, DefaultShowInDetailsCol.Hide()),
				ActualItemLocationColumns(dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.PathToReferencedRow, ShowXIDAndOnHand, TILocations.PermanentStorageBrowseTblCreator),
				TblServerExprNode.New(KB.K("Physical Count"), SqlExpression.Constant("______"))   // Instead we should have a null value, and use Fmt attributes to force minimum width and underlining or a box (giving more vertical space)
			).LayoutArray()
		);
		#endregion
		#region TemplateInventoryLocation
		// TODO: This report does not appear to be reachable in any manner. It is referenced in TemporaryTaskItemLocationBrowseTblCreator which is only
		// used as a browsette (never as a picker), and also in the registered (non-hierarchical) browse tbl for TemplateItemLocation but nothing makes a browser just on this.
		public static Tbl TemplateInventoryLocation = new Tbl(dsMB.Schema.T.TemplateItemLocation,
			TId.TaskTemporaryStorageAssignment,
			new Tbl.IAttr[] {
				SchedulingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetGrouping(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID.F.TemplateTemporaryStorageID.F.ContainingLocationID)
				)

			},
			ColumnBuilder.New(dsMB.Path.T.TemplateItemLocation,
				ItemColumns(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID, ShowAvailable),
				TaskColumns(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID.F.TemplateTemporaryStorageID.F.WorkOrderTemplateID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID.F.TemplateTemporaryStorageID.F.ContainingLocationID.F.Code, new MapSortCol(Statics.LocationSort), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemPriceID.F.UnitCost)
			).LayoutArray()
		);
		#endregion
		#region InventoryAdjustment
		public static Tbl InventoryAdjustment = new Tbl(dsMB.Schema.T.ItemAdjustment,
			TId.ItemAdjustment,
			new Tbl.IAttr[] {
				InventoryGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.ItemAdjustment
				, ItemColumns(dsMB.Path.T.ItemAdjustment.F.ItemLocationID.F.ItemID, ShowXIDAndUOM)
				, ActualItemLocationColumns(dsMB.Path.T.ItemAdjustment.F.ItemLocationID.F.ActualItemLocationID, ShowXID)
				, ItemAdjustmentCodeColumns(dsMB.Path.T.ItemAdjustment.F.ItemAdjustmentCodeID, ShowXID)
				, AccountingTransactionColumns(dsMB.Path.T.ItemAdjustment.F.AccountingTransactionID, ShowXID, TblColumnNode.New(dsMB.Path.T.ItemAdjustment.F.Quantity, DefaultShowInDetailsCol.Show())) // TODO: Figure out how to total this if the group contains a single item
				, UserColumns(dsMB.Path.T.ItemAdjustment.F.AccountingTransactionID.F.UserID)
			).LayoutArray()
		);
		#endregion
		#region ItemAdjustmentCode
		public static Tbl ItemAdjustmentCodeReport = new Tbl(dsMB.Schema.T.ItemAdjustmentCode,
			TId.ItemAdjustmentCode,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ItemAdjustmentCodeColumns(dsMB.Path.T.ItemAdjustmentCode, ShowAll).LayoutArray()
			);
		#endregion
		#region InventoryIssue
		public static Tbl InventoryIssue = new Tbl(dsMB.Schema.T.ItemIssue,
			TId.ItemIssue,
			new Tbl.IAttr[] {
				InventoryGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.ItemIssue,
					ItemColumns(dsMB.Path.T.ItemIssue.F.ItemLocationID.F.ItemID, ShowXIDAndUOM)
				, ActualItemLocationColumns(dsMB.Path.T.ItemIssue.F.ItemLocationID.F.ActualItemLocationID, ShowXID)
				, ItemIssueCodeColumns(dsMB.Path.T.ItemIssue.F.ItemIssueCodeID, ShowXID)
				, ContactColumns(dsMB.Path.T.ItemIssue.F.EmployeeID.F.ContactID, ShowXID)
				, UserColumnsForContact(dsMB.Path.T.ItemIssue.F.EmployeeID.F.ContactID.L.User.ContactID)
				, AccountingTransactionColumns(dsMB.Path.T.ItemIssue.F.AccountingTransactionID, ShowXID, TblColumnNode.New(dsMB.Path.T.ItemIssue.F.Quantity, DefaultShowInDetailsCol.Show()))
				, UserColumns(dsMB.Path.T.ItemIssue.F.AccountingTransactionID.F.UserID)
			).LayoutArray()
		);
		#endregion
		#region ItemIssueCode
		public static Tbl ItemIssueCodeReport = new Tbl(dsMB.Schema.T.ItemIssueCode,
			TId.ItemIssueCode,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			ItemIssueCodeColumns(dsMB.Path.T.ItemIssueCode, ShowAll).LayoutArray()
			);
		#endregion
		#endregion

		#region Preventive Maintenance and Forecasts
		#region Maintenance Timings
		public static Tbl MaintenanceTimings = new Tbl(dsMB.Schema.T.MaintenanceTimingReport,
				TId.MaintenanceTiming,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new PrimaryRecordArg(dsMB.Path.T.MaintenanceTimingReport.F.ScheduleID.PathToReferencedRow),
					new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
						RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.MaintenanceTimingReport.F.ScheduleID.F.Code),
						RTblBase.SetParentChildInformation(KB.TOc(TId.Period),
							RTblBase.ColumnsRowType.UnionWithChildren,
							dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID,
							ColumnBuilder.New(dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID.PathToReferencedRow
								, TblColumnNode.New(dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID.F.Interval, DefaultShowInDetailsCol.Show())
								, TblColumnNode.New(dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID.F.CalendarUnit, DefaultShowInDetailsCol.Show())
								, MeterClassColumns(dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID.F.MeterClassID.PathToReferencedRow, ShowXID)
							).LayoutArray(),
							null,
							new DBI_Path[] {
								dsMB.Path.T.MaintenanceTimingReport.F.ScheduleID.F.Code,
								dsMB.Path.T.MaintenanceTimingReport.F.ScheduleID.F.Hidden,
								dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID.F.CalendarUnit,
								dsMB.Path.T.MaintenanceTimingReport.F.PeriodicityID.F.MeterClassID.F.Code
							}
						)
					)
			},
			ColumnBuilder.New(dsMB.Path.T.MaintenanceTimingReport.F.ScheduleID.PathToReferencedRow,
				ScheduleColumns(dsMB.Path.T.MaintenanceTimingReport.F.ScheduleID, ShowMaintenanceTiming)
			).LayoutArray()
		);
		#endregion
		#region Scheduled Work Order Report
		static ColumnBuilder ScheduledWorkOrderColumns(dsMB.PathClass.PathToScheduledWorkOrderRow ScheduledWorkOrder, FieldSelect effect = null) {
			return ColumnBuilder.New(ScheduledWorkOrder)
				.ColumnSection(StandardReport.LeftColumnFmtId
					// The first node is the only field in the ScheduledWorkOrder record itself that is displayed directly,
					// and only if the user has chosen show deleted records. The best we can do is add
					// a line with the date the record was Hidden.
					, TblColumnNode.New(ScheduledWorkOrder.F.Hidden, effect.ShowIfOneOf(ShowForScheduledWorkOrderSummary))
					, TblColumnNode.New(ScheduledWorkOrder.F.Inhibit, Fmt.SetEnumText(TISchedule.UnitMaintenancePlanInhibitEnumText), effect.ShowIfOneOf(ShowForScheduledWorkOrderSummary))
					, TblColumnNode.New(ScheduledWorkOrder.F.SlackDays, effect.ShowIfOneOf(ShowForScheduledWorkOrderSummary))
					, TblColumnNode.New(ScheduledWorkOrder.F.RescheduleBasisAlgorithm, effect.ShowIfOneOf(ShowForScheduledWorkOrderSummary))
					, TaskColumns(ScheduledWorkOrder.F.WorkOrderTemplateID, effect.XIDIfOneOf(ShowForScheduledWorkOrderSummary))
				)
				.ColumnSection(StandardReport.RightColumnFmtId
					, UnitColumns(ScheduledWorkOrder.F.UnitLocationID, effect == ShowForScheduledWorkOrderSummary ? ShowUnitAndContainingUnitAndUnitAddress : ShowXID)
					, ScheduleColumns(ScheduledWorkOrder.F.ScheduleID, effect.XIDIfOneOf(ShowForScheduledWorkOrderSummary))
			);
		}
		static ColumnBuilder ResolvedWorkOrderTemplateColumns(dsMB.PathClass.PathToResolvedWorkOrderTemplateRow WO, FieldSelect effect = null) {
			// This is stolen from ForecastWorkOrderColumns.
			return ColumnBuilder.New(WO
				, CodeDescColumnBuilder.New(WO.F.WorkOrderPriorityID)
				, CodeDescColumnBuilder.New(WO.F.AccessCodeID)
				, CodeDescColumnBuilder.New(WO.F.WorkCategoryID)
				, CodeDescColumnBuilder.New(WO.F.ProjectID)
				, CodeDescColumnBuilder.New(WO.F.CloseCodeID)
				, TblColumnNode.New(WO.F.ClosingComment)
				, WorkOrderExpenseModelColumns(WO.F.WorkOrderExpenseModelID, showEntry: false)
				, TblColumnNode.New(WO.F.Subject)
				, TblColumnNode.New(WO.F.Description)
				, TblColumnNode.New(WO.F.Downtime)
				// TODO: The following are missing from ForecaseWorkOrderColumns and should perhaps be there
				, TblColumnNode.New(WO.F.Duration)
				, TblColumnNode.New(WO.F.SelectPrintFlag)
				, TblColumnNode.New(WO.F.GenerateLeadTime)
				);
		}
		private static Tbl MakeScheduledWorkOrderReportTbl() {
			var resourceCodeHiddenExpression = new TblQueryExpression(
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID.F.ItemID.F.Hidden),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.Hidden),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Hidden),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID.F.Hidden),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Hidden),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandMiscellaneousWorkOrderCostTemplateID.F.MiscellaneousWorkOrderCostID.F.Hidden)
				));
			var resourceCodeQueryValue = new TblQueryExpression(
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID.F.ItemID.F.Code),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.Code),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Code),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID.F.Code),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Code),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandMiscellaneousWorkOrderCostTemplateID.F.MiscellaneousWorkOrderCostID.F.Code)
				));
			var demandTime = SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborInsideTemplateID.F.Quantity),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborOutsideTemplateID.F.Quantity));
			var demandNumber = SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID.F.Quantity),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID.F.Quantity),
					new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID.F.Quantity));

			return new Tbl(dsMB.Schema.T.ScheduledWorkOrderReport,
				TId.UnitMaintenancePlan,
				new Tbl.IAttr[] {
					SchedulingGroup,
					new PrimaryRecordArg(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.PathToReferencedRow),
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new RTbl((r, logic) => new TblDrivenDynamicColumnsReport(r, logic), typeof(ReportViewerControl),
						RTblBase.SetPageBreakDefault(true),
						RTblBase.SetAllowSummaryFormat(false),
						// TODO: We want several identifying values in the header since several maintenance plans can name the same task.
						// There is redundancy between these and the mainRecordGroupings.
						RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code),
						RTblBase.SetParentChildInformation(KB.K("Resource Demands"),
							RTblBase.ColumnsRowType.UnionWithChildren,
							dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID,
							new TblLayoutNodeArray(
								TblRowNode.New(Array.Empty<TblLayoutNode.ICtorArg>(),
									TblQueryValueNode.New(KB.K("Resource"), resourceCodeQueryValue, resourceCodeHiddenExpression),
									TIReports.UoMNodeForMixedQuantities(
											new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID),
											SqlExpression.Coalesce(new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborInsideTemplateID), new SqlExpression(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandLaborOutsideTemplateID)),
											dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code),
									TIReports.QuantityNodeForMixedQuantities(KB.K("Demanded Quantity"), demandNumber, demandTime, new AllowShowInDetailsChangeCol(false), DefaultShowInDetailsCol.Show()),
									TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.EstimateCost),
									TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.WorkOrderExpenseCategoryID.F.Code),
									TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandActualCalculationInitValue),
									TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.WorkOrderTemplateID.F.Code)
								)
							),
							mainRecordGroupings: new [] {
								TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.F.Code),
								TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.UnitLocationID.F.RelativeLocationID.F.ContainingLocationID.F.Code),
								TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.UnitLocationID.F.RelativeLocationID.F.Code),
								TblColumnNode.New(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.ScheduleID.F.Code)
							},
							sortingValues: new TblQueryValue[] {
								new TblQueryExpression(TIReports.WOTemplateReportResourceDemandCategoryClassifier(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F).RecordTypeExpression),
								resourceCodeQueryValue,
								new TblQueryPath(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID.F.LocationID.F.Code),
								new TblQueryPath(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID.F.DemandItemTemplateID.F.ItemLocationID.F.LocationID),
								resourceCodeHiddenExpression,
								new TblQueryPath(dsMB.Path.T.ScheduledWorkOrderReport.F.DemandTemplateID)
							})
					)
				},
				ColumnBuilder.New(dsMB.Path.T.ScheduledWorkOrderReport,
					// Show the fields from the SWO (Maintenance plan)
					ScheduledWorkOrderColumns(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.PathToReferencedRow, ShowForWorkOrder),
					// Show the resolved task fields, which is the prototype work order
					ResolvedWorkOrderTemplateColumns(dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.WorkOrderTemplateID.L.ResolvedWorkOrderTemplate.WorkOrderTemplateID.F.ResolvedWorkOrderTemplateID.PathToReferencedRow)
				//GroupingSpecifier(0, dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.UnitLocationID),
				//GroupingSpecifier(1, dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.WorkOrderTemplateID),
				//GroupingSpecifier(2, dsMB.Path.T.ScheduledWorkOrderReport.F.ScheduledWorkOrderID.F.ScheduleID)
				).LayoutArray()
			);
		}
		public static Tbl ScheduledWorkOrderReport = MakeScheduledWorkOrderReportTbl();
		#endregion
		#region Scheduled Work Order Summary
		public static Tbl ScheduledWorkOrderSummary = new Tbl(dsMB.Schema.T.ScheduledWorkOrder,
			TId.UnitMaintenancePlan.ReportSummary,
			new Tbl.IAttr[] {
				SchedulingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true),RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.ScheduledWorkOrder,
				ScheduledWorkOrderColumns(dsMB.Path.T.ScheduledWorkOrder, ShowForScheduledWorkOrderSummary)
			).LayoutArray()
		);
		#endregion
		#region Material Forecast
		public static Tbl MaterialForecast = new Tbl(dsMB.Schema.T.ExistingAndForecastResources,
			TId.MaterialForecast,
			new Tbl.IAttr[] {
				SchedulingAndItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRTbl(typeof(ResourceForecastReportViewerControl),
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetFilter(
							new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandItemID).IsNotNull()
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandItemTemplateID).IsNotNull())
					),
					RTblBase.SetFilter(
						new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID).IsNull(),
							KB.K("Resources for existing Work Orders"), false
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ExistingAndForecastResources,
				//ScheduledWorkOrderFilters(GroupCol.GroupSortAbility.GROUPFlag, null, defaultShowXid: false),
				UnitColumns(dsMB.Path.T.ExistingAndForecastResources.F.UnitLocationID, ShowXID),
				ForecastWorkOrderColumns(dsMB.Path.T.ExistingAndForecastResources, ShowXID),
				ItemColumns(dsMB.Path.T.ExistingAndForecastResources.F.ItemLocationID.F.ItemID, ShowXID),
				ItemLocationColumns(dsMB.Path.T.ExistingAndForecastResources.F.ItemLocationID, ShowXID, TILocations.PermanentItemLocationPickerTblCreator),
				TblColumnNode.New(dsMB.Path.T.ExistingAndForecastResources.F.QuantityCount, FooterAggregateCol.Sum(), FieldSelect.ShowAlways)
			//, TblColumnNode.New(dsMB.Path.T.ExistingAndForecastResources.F.Cost)
			).LayoutArray()
		);
		#endregion
		#region Labor Forecast
		public static Tbl LaborForecast = new Tbl(dsMB.Schema.T.ExistingAndForecastResources,
			TId.LaborForecast,
			new Tbl.IAttr[] {
				SchedulingAndLaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRTbl(typeof(ResourceForecastReportViewerControl),
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetFilter(
							new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandLaborInsideID).IsNotNull()
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandOtherWorkInsideID).IsNotNull())
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandLaborOutsideID).IsNotNull())
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandOtherWorkOutsideID).IsNotNull())
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandLaborInsideTemplateID).IsNotNull())
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID).IsNotNull())
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandLaborOutsideTemplateID).IsNotNull())
						.Or(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID).IsNotNull())
					),
					RTblBase.SetFilter(
						new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID).IsNull(),
							KB.K("Resources for existing Work Orders"), false
					),
					RTblBase.SetFilter(
						new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandLaborInsideID).IsNull()
							.And(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandLaborInsideTemplateID).IsNull()),
							KB.TOi(TId.HourlyInside), false
					),
					RTblBase.SetFilter(
						new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandOtherWorkInsideID).IsNull()
							.And(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandOtherWorkInsideTemplateID).IsNull()),
							KB.TOi(TId.PerJobInside), false
					),
					RTblBase.SetFilter(
						new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandLaborOutsideID).IsNull()
							.And(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandLaborOutsideTemplateID).IsNull()),
							KB.TOi(TId.HourlyOutside), false
					),
					RTblBase.SetFilter(
						new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandID.F.DemandOtherWorkOutsideID).IsNull()
							.And(new SqlExpression(dsMB.Path.T.ExistingAndForecastResources.F.DemandTemplateID.F.DemandOtherWorkOutsideTemplateID).IsNull()),
							KB.TOi(TId.PerJobOutside), false
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ExistingAndForecastResources,
				//ScheduledWorkOrderFilters(GroupCol.GroupSortAbility.GROUPFlag, null, defaultShowXid: false),
				UnitColumns(dsMB.Path.T.ExistingAndForecastResources.F.UnitLocationID, ShowXID),
				ForecastWorkOrderColumns(dsMB.Path.T.ExistingAndForecastResources, ShowXID),
				TradeColumns(dsMB.Path.T.ExistingAndForecastResources.F.TradeID, ShowXID),
				EmployeeColumns(dsMB.Path.T.ExistingAndForecastResources.F.EmployeeID, ShowXID),
				VendorColumns(dsMB.Path.T.ExistingAndForecastResources.F.VendorID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.ExistingAndForecastResources.F.QuantityTime, FooterAggregateCol.Sum(), FieldSelect.ShowAlways),
				TblColumnNode.New(dsMB.Path.T.ExistingAndForecastResources.F.QuantityCount, FooterAggregateCol.Sum(), FieldSelect.ShowAlways)
			//, TblColumnNode.New(dsMB.Path.T.ExistingAndForecastResources.F.Cost)
			).LayoutArray()
		);
		#endregion
		#region Maintenance Forecast
		private static readonly SqlExpression ForecastRecordTypes = SqlExpression.Constant(new Set<object>(new[] {
			(object)(short)DatabaseEnums.PMType.MakeWorkOrder,
			(object)(short)DatabaseEnums.PMType.MakeSharedWorkOrder,
			(object)(short)DatabaseEnums.PMType.PredictedWorkOrder
		}));
		public static Tbl MaintenanceForecast = new Tbl(dsMB.Schema.T.MaintenanceForecastReport,
			TId.MaintenanceForecast,
			new Tbl.IAttr[] {
				SchedulingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRTbl(typeof(MaintenanceForecastReportViewerControl),
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.PMGenerationDetailType).In(ForecastRecordTypes)
									.Or(new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.PMGenerationDetailType).IsNull()))
				)
			},
			ColumnBuilder.New(dsMB.Path.T.MaintenanceForecastReport
				, TblServerExprNode.New(KB.K("Work Start Date"), SqlExpression.Coalesce(new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.ForecastWorkStartDate), new SqlExpression(dsMB.Path.T.MaintenanceForecastReport.F.WorkOrderID.F.StartDateEstimate)))
				, TblColumnNode.New(dsMB.Path.T.MaintenanceForecastReport.F.Subject, DefaultShowInDetailsCol.Show())
				, TaskColumns(dsMB.Path.T.MaintenanceForecastReport.F.WorkOrderTemplateID, ShowXID)
				, TblColumnNode.New(dsMB.Path.T.MaintenanceForecastReport.F.WorkOrderID.F.Number)
				, CodeDescColumnBuilder.New(dsMB.Path.T.MaintenanceForecastReport.F.WorkOrderPriorityID)
				, CodeDescColumnBuilder.New(dsMB.Path.T.MaintenanceForecastReport.F.AccessCodeID)
				, WorkOrderExpenseModelColumns(dsMB.Path.T.MaintenanceForecastReport.F.WorkOrderExpenseModelID, showEntry: false)
				, CodeDescColumnBuilder.New(dsMB.Path.T.MaintenanceForecastReport.F.WorkCategoryID)
				, CodeDescColumnBuilder.New(dsMB.Path.T.MaintenanceForecastReport.F.ProjectID)
				, WorkOrderStateHistoryColumns(dsMB.Path.T.MaintenanceForecastReport.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID)
				, ScheduleColumns(dsMB.Path.T.MaintenanceForecastReport.F.ScheduleID)
				, UnitColumns(dsMB.Path.T.MaintenanceForecastReport.F.UnitLocationID, ShowXID)
			).LayoutArray()
		);
		#endregion
		#region Unit Replacement Schedule
		public class PowerCallee : SqlExpression.INamedCallable, SqlExpression.ICallableStaticResultType {
			public PowerCallee() {
			}
			public string Name {
				get {
					return StaticName;
				}
			}
			public static readonly string StaticName = KB.I("power");

			public TypeInfo GetResultType() {
				// TODO: The actual return type of POWER is float, but we declare it this way so the reporting field gets cast back to a percentage,
				// with a maximum value of 1,000,000%.
				// Once POWER is declared properly here (when we have a RealTypeInfo), the expressions below that call POWER to return a result
				// will have to add explicit casts to format the value as a percentage.
				return new PercentTypeInfo(0.0001m, 0m, 10000.0m, allow_null: true);
			}
			public static readonly PowerCallee Instance = new PowerCallee();
		}
		// We should not need the following, SqlExpresion allows division of one Interval by another but the result type is Integral,
		// whereas we want a real number. The unparser does, however, call _IRatio and return a FLOAT.
		public class IntervalRatioCallee : SqlExpression.INamedCallable, SqlExpression.ICallableStaticResultType {
			public IntervalRatioCallee() {
			}
			public string Name {
				get {
					return StaticName;
				}
			}
			public static readonly string StaticName = KB.I("dbo._IRatio");

			public TypeInfo GetResultType() {
				// TODO: This return type is BS but does not appear in any generated SQL nor in any reporting field.
				// The range allows for 1000 years of depreciation but no one range-checks this.
				return new PercentTypeInfo(0.0001m, 0m, 1000m, allow_null: true);
			}
			public static readonly IntervalRatioCallee Instance = new IntervalRatioCallee();
		}
		public static DelayedCreateTbl UnitReplacementSchedule =
			new DelayedCreateTbl(delegate () {
				RTblBase.ReportParameterArg inflationParameter = RTblBase.ReportParameter(KB.K("Annual inflation rate"), new PercentTypeInfo(0.0001m, 0m, 0.50m, allow_null: false), 0.02m);
				var unitInflationRate = SqlExpression.Constant(1.0m, new PercentTypeInfo(1.0m, 1.0m, false));
				var annualInflationValue = unitInflationRate.Plus(SqlExpression.CustomLeafNode(inflationParameter));
				var inflationBasisToNow = SqlExpression.Call(PowerCallee.Instance, annualInflationValue, SqlExpression.Call(IntervalRatioCallee.Instance, new SqlExpression(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.TimeSinceCostBasis), SqlExpression.Constant(new TimeSpan(365, 6, 0, 0))));
				var inflationBasisToEndOfLife = SqlExpression.Call(PowerCallee.Instance, annualInflationValue, SqlExpression.Call(IntervalRatioCallee.Instance, new SqlExpression(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.LifetimeAfterCostBasis), SqlExpression.Constant(new TimeSpan(365, 6, 0, 0))));
				var estimatedReplacementCost = inflationBasisToEndOfLife.Times(new SqlExpression(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.CostBasis)).Cast(dsMB.Schema.T.UnitReport.F.CostBasis.EffectiveType);
				var currentReplacementCost = inflationBasisToNow.Times(new SqlExpression(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.CostBasis)).Cast(dsMB.Schema.T.UnitReport.F.CostBasis.EffectiveType);
				SameKeyContextAs ourContext = new SameKeyContextAs(dsMB.Path.T.Unit.F.RelativeLocationID.F.ExternalTag.Key());
				return new Tbl(dsMB.Schema.T.Unit,
					TId.UnitReplacementForecast,
					new Tbl.IAttr[] {
						UnitValueAndServiceGroup,
						CommonTblAttrs.ViewCostsDefinedBySchema,
						TblDrivenReportRtbl(
							inflationParameter,
							RTblBase.SetAllowSummaryFormat(true),
							RTbl.IncludeBarCodeSymbologyControl(),
							RTblBase.SetDualLayoutDefault(defaultInColumns: true),
							RTblBase.SetFilter(
								new SqlExpression(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.EndOfLife).IsNotNull()
										.Or((new SqlExpression(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.CostBasis).IsNotNull()).And(new SqlExpression(dsMB.Path.T.Unit.F.ReplacementCostLastDate).IsNotNull()))
							)
						)
						},
					ColumnBuilder.New(dsMB.Path.T.Unit,
							// We don't use UnitFilter or UnitValueFilter because we have our own special show/hide and extra fields to place in order.
							TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.Code, FieldSelect.ShowAlways)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID.F.Code, new MapSortCol(Statics.LocationSort), FieldSelect.ShowNever)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.Desc)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.Comment)
						, TblClientTypeFormatterNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.GISLocation)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Make)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Model)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Serial)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Drawing)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.RelativeLocationID.F.ExternalTag)
						, TblQueryValueNode.New(ourContext.K(KB.K("External Tag Bar Code")), ValueAsBarcode(dsMB.Path.T.Unit.F.RelativeLocationID.F.ExternalTag), Fmt.SetUsage(DBI_Value.UsageType.Image), FieldSelect.ShowNever)
						, WorkOrderExpenseModelColumns(dsMB.Path.T.Unit.F.WorkOrderExpenseModelID, showEntry: false)
						, CodeDescColumnBuilder.New(dsMB.Path.T.Unit.F.SystemCodeID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.Unit.F.UnitCategoryID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.Unit.F.UnitUsageID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.Unit.F.AccessCodeID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.Unit.F.OwnershipID)
						, CodeDescColumnBuilder.New(dsMB.Path.T.Unit.F.AssetCodeID)
						, VendorColumns(dsMB.Path.T.Unit.F.PurchaseVendorID)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.PurchaseDate)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.OriginalCost, FooterAggregateCol.Sum())
						, TblColumnNode.New(dsMB.Path.T.Unit.F.ReplacementCostLastDate)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.ReplacementCost, FooterAggregateCol.Sum())
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.CostDate)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.CostBasis, FooterAggregateCol.Sum())
						, TblColumnNode.New(dsMB.Path.T.Unit.F.ScrapDate)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.ScrapValue, FooterAggregateCol.Sum())
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.EndOfLife, DefaultShowInDetailsCol.Show())
						, TblColumnNode.New(dsMB.Path.T.Unit.F.TypicalLife)
						, TblColumnNode.New(dsMB.Path.T.Unit.F.Id.L.UnitReport.UnitID.F.LifetimeAfterCostBasis) // TODO: Show whole years only
						, TblServerExprNode.New(KB.K("Inflation to date"), inflationBasisToNow.Minus(unitInflationRate))
						, TblServerExprNode.New(KB.K("Inflation to end of life"), inflationBasisToEndOfLife.Minus(unitInflationRate))
						, TblServerExprNode.New(KB.K("Current replacement cost"), currentReplacementCost, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum())
						, TblServerExprNode.New(KB.K("End of life replacement cost"), estimatedReplacementCost, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum())
					).LayoutArray()
				);
			}
		);
		#endregion
		#endregion

		#region WorkOrder-related

		// In the following, the derived DemandXxxID is sufficient for classifying the record, but does not really give a good label
		// (the labels are all of the form "Demand Xxxx Yyyy"), so we extend the path to the actual resource being demanded.
		public static RecordTypeClassifierByLinkages WOReportResourceDemandCategoryClassifier(dsMB.PathClass.PathToDemandRow.FAccessor D) {
			return new RecordTypeClassifierByLinkages(true,
					D.DemandItemID.F.ItemLocationID.F.ItemID,
					// We order by Inside/Outside before Labor/OtherWork because that is the way the records are divided in the WO resource browsettes.
					D.DemandLaborInsideID.F.LaborInsideID,
					D.DemandOtherWorkInsideID.F.OtherWorkInsideID,
					D.DemandLaborOutsideID.F.LaborOutsideID,
					D.DemandOtherWorkOutsideID.F.OtherWorkOutsideID,
					D.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID
				);
		}
		// In the following, the derived DemandXxxID is sufficient for classifying the record, but does not really give a good label
		// (the labels are all of the form "Demand Xxxx Yyyy"), so we extend the path to the actual resource being demanded.
		public static RecordTypeClassifierByLinkages WOTemplateReportResourceDemandCategoryClassifier(dsMB.PathClass.PathToDemandTemplateRow.FAccessor D) {
			return new RecordTypeClassifierByLinkages(true,
					D.DemandItemTemplateID.F.ItemLocationID.F.ItemID,
					// We order by Inside/Outside before Labor/OtherWork because that is the way the records are divided in the WO resource browsettes.
					D.DemandLaborInsideTemplateID.F.LaborInsideID,
					D.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID,
					D.DemandLaborOutsideTemplateID.F.LaborOutsideID,
					D.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID,
					D.DemandMiscellaneousWorkOrderCostTemplateID.F.MiscellaneousWorkOrderCostID
				);
		}
		// In the following, the derived ActualXxxID is sufficient for classifying the record, but does not really give a good label
		// (the labels are all of the form "Actual Xxxx Yyyy"), so we extend the path to the actual resource being demanded.
		public static RecordTypeClassifierByLinkages WOReportResourceActualCategoryClassifier(dsMB.PathClass.PathToAccountingTransactionRow.FAccessor TX) {
			return new RecordTypeClassifierByLinkages(true,
					new Tuple<DBI_Path, Key>(TX.ActualItemID.F.DemandItemID.F.ItemLocationID.F.ItemID, null),
					new Tuple<DBI_Path, Key>(TX.ActualLaborInsideID.F.DemandLaborInsideID.F.LaborInsideID, null),
					new Tuple<DBI_Path, Key>(TX.ActualOtherWorkInsideID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID, null),
					new Tuple<DBI_Path, Key>(TX.ActualLaborOutsidePOID.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID, KB.K("Hourly Outside (with PO)")),
					new Tuple<DBI_Path, Key>(TX.ActualLaborOutsideNonPOID.F.DemandLaborOutsideID.F.LaborOutsideID, KB.K("Hourly Outside (no PO)")),
					new Tuple<DBI_Path, Key>(TX.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID, KB.K("Per Job Outside (with PO)")),
					new Tuple<DBI_Path, Key>(TX.ActualOtherWorkOutsideNonPOID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID, KB.K("Per Job Outside (no PO)")),
					new Tuple<DBI_Path, Key>(TX.ActualMiscellaneousWorkOrderCostID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID, null)
				);
			// We order by Inside/Outside before Labor/OtherWork because that is the way the records are divided in the WO resource browsettes.
		}
		public static EnumValueTextRepresentations WOTemplateRecordTypeNames =
			new EnumValueTextRepresentations(
				new Key[] {
					KB.TOi(TId.Item),
					KB.TOi(TId.HourlyInside),
					KB.TOi(TId.HourlyOutside),
					KB.TOi(TId.PerJobInside),
					KB.TOi(TId.PerJobOutside),
					KB.TOi(TId.MiscellaneousCost),
					KB.TOi(TId.PurchaseOrderTemplate)
				},
				null,
				new object[] { 1, 2, 3, 4, 5, 6, 7 }
			);
		#region - Work Orders
		private static RTblBase.ICtorArg WorkOrderStateHistoryChildDefinition(dsMB.PathClass.PathToWorkOrderStateHistoryRow WOSH, IEnumerable<TblLeafNode> customGrouping = null) {
			return RTblBase.SetParentChildInformation(StateHistoryKey,
						RTblBase.ColumnsRowType.JoinWithAssuredChildren,
						null,
						// TODO: All columns of the StateHistory records will appear in COLUMNS (only), including the Comments fields as we have no means to have them be separated onto individual Label/Value full width rows in the report at this time
						WorkOrderStateHistoryAndExtraColumns(WOSH).LayoutArray(),
						customGrouping,
						new DBI_Path[] { WOSH.F.EffectiveDate });
		}
		#region -   WO Form & History [by Assignee]
		#region StateHistoryAsText: add a table row for server-side-formatted State History
		static TblLayoutNode StateHistoryAsText(RTbl.ReportParameterArg showOption, DBI_Path stateHistoryAsString, params SimpleKey[] stateNamesToTranslate) {
			// Calculate the space required for the State column.
			int maxStateLength = KB.K("State").Translate().Length;
			foreach (SimpleKey k in stateNamesToTranslate) {
				int translatedLength = k.Translate().Length;
				if (translatedLength > maxStateLength)
					maxStateLength = translatedLength;
			}
			// The SQL functions mbfn_PurchaseOrder_History_As_String and mbfn_WorkOrder_History_As_String have formatted the state history into columns and so we have to display it using a fixed-pitch font,
			// and we also have to also use this font for the headers so they line up.
			// We would like to assume that the pitch of the regular and bold fonts match, and in most cases they do but not for Lucida Console (but they do for Lucida Console Typewriter).
			// The widths in the format insertions correspond to the widths mbfn_WorkOrder_History_As_String uses for the data. The exception is the State column which is 44 wide and which
			// we replace with maxStateLength characters.
			var p = Strings.IFormat(Application.InstanceFormatCultureInfo, "{0,-14} {1,-8} {2} {3,-25} {4}", StateHistoryKey, KB.K("Date"), KB.K("State").Translate().PadRight(maxStateLength), KB.K("User"), KB.K("Status"));
			//			Textbox t = report.ValueTextbox(p);	// NOTE (W20130015): Should call LabelTextbox instead to get bold but we can't assume bold and non-bold pitches match (thanks, MS)
			//			t.Style.FontFamily = report.Logic.FixedPitchFaceName;
			//			t.Style.FontSize = new Size(report.Logic.FixedTextSizePoints, Size.Units.Points);
			// if we can determine if the pitch of the Bold and Regular fonts match, we could turn on Bold style
			// however, for now lets just decorate the header with underline
			//			t.Style.TextDecoration = TextDecoration.Underline;

			//			new TableRow(where, new TablixCell(t, -1));
			// The state codes placed in the text by mbfn_WorkOrder_History_As_String are actually Keys in the stored SQL form with an extra SimpleKey.ContextDelimiter in front for no particular reason.
			//			t = report.ValueTextbox(report.Field(s));
			//			t.Style.FontFamily = report.Logic.FixedPitchFaceName;
			//			t.Style.FontSize = new Size(report.Logic.FixedTextSizePoints, Size.Units.Points);
			//			new TableRow(where, new TablixCell(t, -1));
			return TblRowNode.New(null, Array.Empty<TblLayoutNode.ICtorArg>(),
				TblQueryValueNode.New(KB.T(p), TblQueryCalculation.New(
						delegate (object[] values) {
							var rawValue = (string)values[0];
							if (rawValue == null)
								return null;
							foreach (SimpleKey k in stateNamesToTranslate)
								rawValue = rawValue.Replace((SimpleKey.ContextDelimiter + SimpleKey.UnParse(k)).PadRight(44), k.Translate().PadRight(maxStateLength + 1));
							return rawValue;
						}
						, stateHistoryAsString.ReferencedColumn.EffectiveType
						, new TblQueryExpression(new SqlExpression(stateHistoryAsString))
					),
					new AllowShowInDetailsChangeCol(false),
					Fmt.SetMonospace(true),
					RFmt.SetHideIf(SqlExpression.CustomLeafNode(showOption).Not()),
					RFmt.SetAllowUserSorting(false),
					RFmt.SetAllowUserGrouping(false),
					RFmt.SetAllowUserFilter(false),
					new LabelFormattingArg(
						Fmt.SetMonospace(true),
						// TODO: If we can be sure that the pitch of the bold and non-bold Monospace fonts match, we should use Fmt.SetFontStyle(System.Drawing.FontStyle.Bold)
						// rather than Fmt.SetFontStyle(System.Drawing.FontStyle.Underline). Unfortunately, one of the fixed-pitch fonts (Lucida Console Typewriter) is wider in bold
						// than it is in normal weight. We have to way of checking this at Tbl creation time.
						Fmt.SetFontStyle(System.Drawing.FontStyle.Underline)
					)
				)
			);
		}
		#endregion

		static Tbl WOHistoryOrFormTbl(Tbl.TblIdentification title, bool formLayout, dsMB.PathClass.PathToWorkOrderFormReportRow WOFR, dsMB.PathClass.PathToWorkOrderAssigneeRow assignee = null, bool singleRecord = false, SqlExpression filter = null) {
			dsMB.PathClass.PathToWorkOrderLink WOl = WOFR.F.WorkOrderID;
			dsMB.PathClass.PathToWorkOrderRow WO = WOl.PathToReferencedRow;
			dsMB.PathClass.PathToWorkOrderExtrasRow WOR = WOl.L.WorkOrderExtras.WorkOrderID;

			var showResourceDetails = RTbl.ReportShowChildRowsParameter(KB.K("Resource Details"), false);
			var demandNumberLabel = KB.K("Demanded Number");
			var showDemandNumber = RTbl.ReportShowChildFieldParameter(demandNumberLabel);
			var demandTimeLabel = KB.K("Demanded Time");
			var showDemandTime = RTbl.ReportShowChildFieldParameter(demandTimeLabel);
			var actualQuantityLabel = KB.K("Actual Quantity");
			var showActualQuantity = RTbl.ReportShowChildFieldParameter(actualQuantityLabel);
			// TODO: The following two parameter definitions only work if the root table of the path has *exactly* one ViewCosts permission defined.
			// Changing this would require extending the semantics of ReportShow[Child]FieldParameter to accept multiple or no IHidingAttr objects
			// and implementing the changes down the line to properly implement this.
			var demandCostLabel = KB.K("Demanded Cost");
			var showDemandCost = RTbl.ReportShowChildFieldParameter(demandCostLabel, ViewCostPermissionsFromPath(WOFR.F.Id)[0]);
			var actualCostLabel = KB.K("Actual Cost");
			var showActualCost = RTbl.ReportShowChildFieldParameter(actualCostLabel, ViewCostPermissionsFromPath(WOFR.F.Id)[0]);

			#region Child-row column expressions
			#region Values valid for either demands or actuals
			var isActual = new SqlExpression(WOFR.F.AccountingTransactionID).IsNotNull();
			var isDemand = new SqlExpression(WOFR.F.AccountingTransactionID).IsNull();
			var quantityIsTime = new SqlExpression(WOFR.F.DemandID.F.DemandItemID).IsNull().And(new SqlExpression(WOFR.F.DemandID.F.DemandMiscellaneousWorkOrderCostID).IsNull());
			var resourceCodeHiddenExpression = new TblQueryExpression(
				SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ItemID.F.Hidden),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.Hidden),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Hidden),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Hidden),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Hidden),
					new SqlExpression(WOFR.F.DemandID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Hidden)
				));
			var resourceCodeQueryValue = new TblQueryExpression(
				SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ItemID.F.Code),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.Code),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Code),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code),
					new SqlExpression(WOFR.F.DemandID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Code)
				));
			var resourceDescQueryValue = new TblQueryExpression(
				SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ItemID.F.Desc),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.Desc),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Desc),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Desc),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Desc),
					new SqlExpression(WOFR.F.DemandID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Desc)
				));
			var UoMNode = TIReports.UoMNodeForMixedQuantities(
					new SqlExpression(WOFR.F.DemandID.F.DemandItemID),
					SqlExpression.Coalesce(new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID), new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID)),
					WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code);
			#endregion
			#region Values for the "non-detail" rows
			var demandTime = SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.Quantity),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.Quantity));
			var demandTimeExpression = SqlExpression.Select(isDemand, demandTime);
			var demandNumber = SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.Quantity),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.Quantity),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.Quantity));
			var actualTime = SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.ActualQuantity),
					new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.ActualQuantity));
			var actualNumber = SqlExpression.Coalesce(
					new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ActualQuantity),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.ActualQuantity),
					new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.ActualQuantity));
			// Although the non-details row hides if the row is an actual, we still need the expression to be correct for such rows so the totalling works.
			var estimatedCostExpression = SqlExpression.Select(isDemand, new SqlExpression(WOFR.F.DemandID.F.CostEstimate));
			var actualCostExpression = SqlExpression.Select(isDemand, new SqlExpression(WOFR.F.DemandID.F.ActualCost));
			#endregion
			#region Values for the "detail" rows which are individual Demand or Actual (AccountingTransaction) records
			// The detail rows (individual Demand and Actual records including corrections) have their own tabular layout with no column headers.

			// Make the "Estimate"/"Correction" column, which shows "Estimate" for Demands, "Correction" for actuals that are corrections, and null otherwise.
			var detailRecordType = new TblQueryExpression(SqlExpression.Constant(KB.I("    ")).Plus(
				SqlExpression.Select(
					isDemand, SqlExpression.Constant(KB.K("Estimate").Translate()),
					new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborInsideID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborInsideID))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID)))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborOutsidePOID)))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkInsideID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkInsideID)))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID)))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID)))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualItemID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualItemID)))
						.Or(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualMiscellaneousWorkOrderCostID.F.CorrectionID).NEq(new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualMiscellaneousWorkOrderCostID))),
						SqlExpression.Constant(KB.K("Correction").Translate()),
						SqlExpression.Constant(KB.K("Actual").Translate()))));
			var detailActualTime = SqlExpression.Coalesce(
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborInsideID.F.Quantity),
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.Quantity),
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.Quantity));
			var detailActualNumber = SqlExpression.Coalesce(
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualItemID.F.Quantity),
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkInsideID.F.Quantity),
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkOutsideNonPOID.F.Quantity),
						new SqlExpression(WOFR.F.AccountingTransactionID.F.ActualOtherWorkOutsidePOID.F.Quantity));

			// We don't show the location of the item if it is a temp storage for this WO located at the unit, which is typical of temporary storage locations.
			// Note that the first two terms of the OR are indeterminate if the location is not a temporary storage location.
			var locationIfItem = SqlExpression.Select(
				new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.LocationID.F.TemporaryStorageID.F.ContainingLocationID).NEq(new SqlExpression(WO.F.UnitLocationID))
					.Or(new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.LocationID.F.TemporaryStorageID.F.WorkOrderID).NEq(new SqlExpression(WOFR.F.WorkOrderID)))
					.Or(new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID).IsNotNull()),
									new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.LocationID.F.Code));
			var tradeIfLabor = SqlExpression.Coalesce(
				new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.TradeID.F.Code),
				new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.TradeID.F.Code),
				new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.TradeID.F.Code),
				new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.TradeID.F.Code));

			var extraInfoLabel = SqlExpression.Select(
									locationIfItem.IsNotNull(), SqlExpression.Constant(KB.K("From").Translate()),
									tradeIfLabor.IsNotNull(), SqlExpression.Constant(KB.TOi(TId.Trade).Translate())
								);
			var extraInfoCode = SqlExpression.Coalesce(locationIfItem, tradeIfLabor);

			var extraInfoHidden = SqlExpression.Coalesce(
				// ItemLocation.LocationID can be a storeroom (which is a RelativeLocation) or a TemporaryStorage (which is not hideable)
				new SqlExpression(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.LocationID.F.RelativeLocationID.F.Hidden),
				new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.TradeID.F.Hidden),
				new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.TradeID.F.Hidden),
				new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.TradeID.F.Hidden),
				new SqlExpression(WOFR.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.TradeID.F.Hidden));
			var detailCostExpression = new SqlExpression(WOFR.F.AccountingTransactionID.F.Cost);
			#endregion
			#endregion

			// For some of the TODO's in the detail section we need the ability to refer to some of the show-child-field options within Sql expressions.
			// Some of these options are already implied by layout nodes in the non-detail row (Demanded Time, Demanded Quantity, Demanded Cost, Actual Cost)
			// which would have to be identified with each other using an object-identical key.
			// Others are brand-new for the details (Actual Time, Actual Quantity).
			var rtblArgs = new List<RTblBase.ICtorArg> {
				RTblBase.SetReportCaptionText(title.Compose(FormReportCaption)),
				RTblBase.SetReportTitle(title.Compose(FormReportTitle)), // TODO: This should move to SetPageHeader as the Left portion
				RTblBase.SetRecordHeader(null,
					TblQueryValueNode.New(null, ValueAsBarcode(WO.F.Number), Fmt.SetUsage(DBI_Value.UsageType.Image), FieldSelect.ShowNever),
					TblQueryValueNode.New(null, new TblQueryPath(WO.F.Number))),
				RTbl.IncludeBarCodeSymbologyControl(),
				RTblBase.SetAllowSummaryFormat(false),
				RTblBase.SetPrimaryRecordHeaderNode(WO.F.Number),
				showResourceDetails, showDemandNumber, showDemandTime, showActualQuantity, showDemandCost, showActualCost,
				RTblBase.SetParentChildInformation(KB.K("Resources"),
					RTblBase.ColumnsRowType.JoinWithChildren,
					WOFR.F.DemandID,
					new TblLayoutNodeArray(
						TblRowNode.New(null, new TblLayoutNode.ICtorArg[] { RFmt.SetHideIf(isActual) }
							, TblQueryValueNode.New(KB.K("Resource"), resourceCodeQueryValue, resourceCodeHiddenExpression)
							, TIReports.UoMNodeForMixedQuantities(
												new SqlExpression(WOFR.F.DemandID.F.DemandItemID),
												SqlExpression.Coalesce(new SqlExpression(WOFR.F.DemandID.F.DemandLaborInsideID), new SqlExpression(WOFR.F.DemandID.F.DemandLaborOutsideID)),
												WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID.F.Code)
							, TblServerExprNode.New(demandTimeLabel, demandTimeExpression, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum())
							, TblServerExprNode.New(demandNumberLabel, demandNumber, DefaultShowInDetailsCol.Show())
							, TIReports.QuantityNodeForMixedQuantities(actualQuantityLabel, actualNumber, actualTime, Fmt.SetFontStyle(System.Drawing.FontStyle.Bold))	// Can't sum this because it has varying type
							, TblServerExprNode.New(demandCostLabel, estimatedCostExpression, FooterAggregateCol.Sum())
							, TblServerExprNode.New(actualCostLabel, actualCostExpression, FooterAggregateCol.Sum(), Fmt.SetFontStyle(System.Drawing.FontStyle.Bold))
						),
						TblRowNode.New(null, new TblLayoutNode.ICtorArg[] { RFmt.SetHideIf(isActual)}
							, TblQueryValueNode.New(KB.K("Resource Description"), resourceDescQueryValue, resourceCodeHiddenExpression, DefaultShowInDetailsCol.Hide())
						),
						// We don't show any column headers for this grouping level, so the layout nodes' Label is immaterial.
						// TODO: The format for the detail rows is the same for Demands and Actuals, except that we want quantities and costs on actuals to be boldface.
						// We have no way in the tbl layout to make formatting data-dependent. We can't use a distinct TblRowNode for demands and actuals because the columns might not line up.
						TblRowNode.New(null, new TblLayoutNode.ICtorArg[] { RFmt.SetHideIf(SqlExpression.CustomLeafNode(showResourceDetails).Not()) },
							ColumnBuilder.New(WOFR
							, TblQueryValueNode.New(null, detailRecordType)
//							, DateHHMMColumnNode(WOFR.F.AccountingTransactionID.F.EffectiveDate)
							, AccountingTransactionColumns(WOFR.F.AccountingTransactionID, ShowXID)
							, TblServerExprNode.New(null, SqlExpression.Select(isDemand, SqlExpression.Select(SqlExpression.CustomLeafNode(showDemandTime), demandTime), SqlExpression.Select(SqlExpression.CustomLeafNode(showActualQuantity), detailActualTime)))
							, TblServerExprNode.New(null, SqlExpression.Select(isDemand, SqlExpression.Select(SqlExpression.CustomLeafNode(showDemandNumber), demandNumber), SqlExpression.Select(SqlExpression.CustomLeafNode(showActualQuantity), detailActualNumber)))
							// Note that the costs here are the plain paths for Demand.Cost and AccountingTransaction.Cost, not the special ones estimatedCostExpression and actualCostExpression required for proper totalling
							, TblServerExprNode.New(null, SqlExpression.Select(isDemand, SqlExpression.Select(SqlExpression.CustomLeafNode(showDemandCost), new SqlExpression(WOFR.F.DemandID.F.CostEstimate)), SqlExpression.Select(SqlExpression.CustomLeafNode(showActualCost), new SqlExpression(WOFR.F.DemandID.F.ActualCost))))
							// Right-align next so it is next to the actual resource description.
							, TblServerExprNode.New(null, extraInfoLabel, Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far))
							, TblServerExprNode.New(null, extraInfoCode, extraInfoHidden)
							).NodeArray()
						)
					),
					mainRecordGroupings:
						assignee != null ?
							new TblLeafNode[] {
								TblColumnNode.New(assignee.F.ContactID),
								TblColumnNode.New(WO.F.Number)
							}
							:
							new TblLeafNode[] {
								TblColumnNode.New(WO.F.Number)
							},
					sortingValues: new TblQueryValue[] {
			// Sort the child records amongst themselves.
			// Because these are only for sorting, no label is necessary.
						new TblQueryExpression(TIReports.WOReportResourceDemandCategoryClassifier(WOFR.F.DemandID.F).RecordTypeExpression),
						resourceCodeQueryValue,
						new TblQueryPath(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.LocationID.F.Code),
						new TblQueryPath(WOFR.F.DemandID.F.DemandItemID.F.ItemLocationID.F.LocationID),
						resourceCodeHiddenExpression,
						new TblQueryPath(WOFR.F.DemandID.F.EntryDate),
						new TblQueryPath(WOFR.F.DemandID),
						// Because the data includes Demands and Actuals as a Union organization (rather than a outer join), sort by (true) EffectiveDate,
						// which will sort the Demand first (where EffectiveDate is null) followed by all the actuals in effective-date order.
						new TblQueryPath(WOFR.F.AccountingTransactionID.F.EffectiveDate),
						new TblQueryPath(WOFR.F.AccountingTransactionID.F.EntryDate)
					})
			};
			if (filter != null)
				rtblArgs.Add(RTblBase.SetFilter(filter));
			if (singleRecord) {
				rtblArgs.Add(RTblBase.SetNoUserFilteringAllowed());
				rtblArgs.Add(RTblBase.SetNoUserSortingAllowed());
			}
			var showHistory = RTbl.ReportShowChildRowsParameter(StateHistoryKey, !formLayout);
			rtblArgs.Add(showHistory);
			var stateHistoryNode = StateHistoryAsText(showHistory, WOR.F.StateHistoryAsText, StateContext.DraftCode, StateContext.OpenCode, StateContext.ClosedCode, StateContext.VoidedCode);
			if (formLayout) {
				rtblArgs.Add(RTbl.ReportParameter(FormReport.AdditionalBlankLinesLabel, dsMB.Schema.V.WOFormAdditionalBlankLines, FormReport.AdditionalBlankLinesParameterID));
				rtblArgs.Add(RTbl.ReportParameter(FormReport.AdditionalInformationLabel, dsMB.Schema.V.WOFormAdditionalInformation, FormReport.AdditionalInformationParameterID));
				rtblArgs.Add(RTblBase.SetNoUserGroupingAllowed());
			}
			else {
				rtblArgs.Add(RTblBase.SetPageBreakDefault(true));
			}

			if (assignee != null) {
				rtblArgs.Add(RTblBase.SetNoSearchPrefixing());
				rtblArgs.Add(RTblBase.SetRequiredInvariantForFooterAggregates(assignee));
			}
			List<Tbl.IAttr> iAttr = new List<Tbl.IAttr> {
				WorkOrdersGroup,
				new PrimaryRecordArg(WO),
				CommonTblAttrs.ViewCostsDefinedBySchema
			};
			if (formLayout)
				iAttr.Add(new RTbl((r, logic) => new Reports.MainBossFormReport(r, logic), typeof(ReportViewerControl), rtblArgs.ToArray()));
			else
				iAttr.Add(new RTbl((r, logic) => new TblDrivenDynamicColumnsReport(r, logic), typeof(ReportViewerControl), rtblArgs.ToArray()));

			var layout = ColumnBuilder.New(WO);
			if (assignee != null) {
				layout.ColumnSection(StandardReport.RightColumnFmtId
					, ContactColumns(assignee.F.ContactID, ShowXIDBold)
					, UserColumnsForContact(assignee.F.ContactID.L.User.ContactID)
				);
			}
			layout.Concat(
				WorkOrderColumns(WO, ShowForWorkOrder)
				, ColumnBuilder.New(WO).ColumnSection(StandardReport.MultiLineRowsFmtId, stateHistoryNode)
			);
			List<TblActionNode> extraNodes = new List<TblActionNode>();
			if (formLayout)
				extraNodes.Add(ClearSelectForPrintCommand(WO.F.SelectPrintFlag, WOl));
			return new Tbl(WO.Table, title, iAttr.ToArray(), layout.LayoutArray(), extraNodes.ToArray());
		}
		public static Tbl WorkOrderFormReport = WOHistoryOrFormTbl(TId.WorkOrder, true, dsMB.Path.T.WorkOrderFormReport);
		public static Tbl WorkOrderDraftFormReport = WOHistoryOrFormTbl(TId.WorkOrder, true, dsMB.Path.T.WorkOrderFormReport, filter: new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsDraft).IsTrue());
		public static Tbl WorkOrderOpenFormReport = WOHistoryOrFormTbl(TId.WorkOrder, true, dsMB.Path.T.WorkOrderFormReport, filter: new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen).IsTrue());
		public static Tbl WorkOrderOpenAndAssignedFormReport = WOHistoryOrFormTbl(TId.WorkOrder, true, dsMB.Path.T.WorkOrderFormReport, filter: new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID).In(new SelectSpecification( // open w/o assigned to the current user
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID) },
							new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))
									.And(new SqlExpression(dsMB.Path.T.WorkOrderAssignmentAll.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsOpen).IsTrue()),
							null)));
		public static Tbl WorkOrderClosedFormReport = WOHistoryOrFormTbl(TId.WorkOrder, true, dsMB.Path.T.WorkOrderFormReport, filter: new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsClosed).IsTrue());
		public static Tbl WorkOrderVoidFormReport = WOHistoryOrFormTbl(TId.WorkOrder, true, dsMB.Path.T.WorkOrderFormReport, filter: new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.NoLabelWorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsVoid).IsTrue());


		public static Tbl SingleWorkOrderFormReport = WOHistoryOrFormTbl(TId.WorkOrder.ReportSingle, true, dsMB.Path.T.WorkOrderFormReport, singleRecord: true);
		public static Tbl WorkOrderByAssigneeFormReport = WOHistoryOrFormTbl(TId.WorkOrder.ReportByAssignee, true, dsMB.Path.T.WorkOrderAssignmentReport.F.WorkOrderFormReportID.PathToReferencedRow,
																					dsMB.Path.T.WorkOrderAssignmentReport.F.WorkOrderAssigneeID.PathToReferencedRow);
		public static Tbl WOHistory = WOHistoryOrFormTbl(TId.WorkOrder.ReportHistory, false, dsMB.Path.T.WorkOrderFormReport);
		public static Tbl WOHistoryByAssignee = WOHistoryOrFormTbl(TId.WorkOrder.ReportHistory.ReportByAssignee, false, dsMB.Path.T.WorkOrderAssignmentReport.F.WorkOrderFormReportID.PathToReferencedRow,
			dsMB.Path.T.WorkOrderAssignmentReport.F.WorkOrderAssigneeID.PathToReferencedRow);
		#endregion
		#region -   WO Summary (no child records), Assigned WO Summary
		static Tbl WOSummaryBase(Tbl.TblIdentification title, dsMB.PathClass.PathToWorkOrderRow WO, FieldSelect fieldSelect, dsMB.PathClass.PathToWorkOrderAssigneeRow assignee = null) {

			var rtblArgs = new List<RTblBase.ICtorArg> {
				RTblBase.SetAllowSummaryFormat(true),
				RTblBase.SetDualLayoutDefault(true),
				RTblBase.SetPreferWidePage(true),
				fieldSelect == ShowForMaintenanceHistory ? RTblBase.SetGrouping(WO.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Id) : null,
				fieldSelect == ShowForMaintenanceHistory ? RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.WorkOrder.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.FilterAsClosed)) : null,
				fieldSelect == ShowForOverdueWorkOrder ? RTblBase.SetSortingGrouping(new SortingGrouping(SqlExpression.CustomLeafNode(OverdueLabelKey, ObjectTypeInfo.Universe), SortingGrouping.Orderings.Descending)) : null,
				fieldSelect == ShowForOverdueWorkOrder ? RTblBase.SetFilter(WOStatisticCalculation.IsIncompleteOverdueWorkOrder(dsMB.Path.T.WorkOrder), KB.K("Non Overdue WorkOrders"), true) : null,
				assignee != null ? RTblBase.SetGrouping(assignee.F.ContactID) : null
			};
			if (assignee != null) {
				rtblArgs.Add(RTblBase.SetNoSearchPrefixing());
				rtblArgs.Add(RTblBase.SetRequiredInvariantForFooterAggregates(assignee));
			}
			List<Tbl.IAttr> iAttr = new List<Tbl.IAttr> {
				WorkOrdersGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(rtblArgs.ToArray())
			};
			// TODO: Fields here should also exist in WorkOrderFilters but without any footer aggregates and DefaultShowInDetailsCol.Show()
			var layout = ColumnBuilder.New(WO, WorkOrderColumns(WO, fieldSelect));

			if (assignee != null) {
				layout.Concat(ContactColumns(assignee.F.ContactID, ShowXIDBold));
				layout.Concat(UserColumnsForContact(assignee.F.ContactID.L.User.ContactID));
			}
			return new Tbl(WO.Table, title, iAttr.ToArray(), layout.LayoutArray());
		}

		public static Tbl WOSummary = WOSummaryBase(TId.WorkOrder.ReportSummary, dsMB.Path.T.WorkOrder, ShowForWorkOrderSummary, null);
		public static Tbl WOOverdue = WOSummaryBase(TId.OverdueWorkOrder.ReportSummary, dsMB.Path.T.WorkOrder, ShowForOverdueWorkOrder, null);
		public static Tbl WOSummaryByAssignee = WOSummaryBase(TId.WorkOrderAssignmentByAssignee.ReportSummary,
			dsMB.Path.T.WorkOrderAssignmentAndUnassignedWorkOrder.F.WorkOrderID.PathToReferencedRow, ShowForWorkOrderSummary,
			dsMB.Path.T.WorkOrderAssignmentAndUnassignedWorkOrder.F.WorkOrderAssigneeID.PathToReferencedRow);
		#endregion
		#region -   Work Order Charts
		static Tbl WOChartBase(Tbl.TblIdentification title, RTbl.BuildReportObjectDelegate reportBuilder, RTblBase.ICtorArg intervalAttribute = null, RTblBase.ICtorArg filterAttribute = null,
			bool defaultGroupByUnit = false) {
			return GenericChartBase(dsMB.Schema.T.WorkOrder, WorkOrdersGroup, title, reportBuilder,
				ColumnBuilder.New(dsMB.Path.T.WorkOrder
					, WorkOrderColumns(dsMB.Path.T.WorkOrder)
				).LayoutArray(),
				filterAttribute,
				intervalAttribute,
				defaultGroupByUnit ? RTblBase.SetGrouping(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Id) : null);
		}
		static Tbl WOSelectableFieldChartBase(Tbl.TblIdentification title, RTbl.BuildReportObjectDelegate reportBuilder, RTblBase.LeafNodeFilter filter, RTblBase.ICtorArg intervalAttribute = null, RTblBase.ICtorArg filterAttribute = null,
			bool defaultGroupByUnit = false) {
			return SelectableFieldChartBase(dsMB.Schema.T.WorkOrder, WorkOrdersGroup, title, reportBuilder,
				ColumnBuilder.New(dsMB.Path.T.WorkOrder
					, WorkOrderColumns(dsMB.Path.T.WorkOrder)
				).LayoutArray(),
				filter,
				filterAttribute,
				intervalAttribute,
				defaultGroupByUnit ? RTblBase.SetGrouping(dsMB.Path.T.WorkOrder.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Id) : null);
		}
		#region -     Work Order Count charts
		static Tbl WOChartCountBase(Tbl.TblIdentification title, DBI_Path groupingPath, ReportViewLogic.IntervalSettings? groupingInterval) {
			return WOChartBase(title, (Report r, ReportViewLogic logic) => new Reports.WOChartCountBase(r, logic, groupingPath), RTblBase.SetChartIntervalGrouping(groupingInterval), defaultGroupByUnit: groupingPath == null);
		}
		static Tbl WOChartCountBase(Tbl.TblIdentification title, TblLeafNode groupingNode, ReportViewLogic.IntervalSettings? groupingInterval) {
			return WOChartBase(title, (Report r, ReportViewLogic logic) => new Reports.WOChartCountBase(r, logic, groupingNode), RTblBase.SetChartIntervalGrouping(groupingInterval), defaultGroupByUnit: groupingNode == null);
		}
		public static Tbl WOChartCountByCreatedDate = WOChartCountBase(TId.WOChartCountByCreatedDate, dsMB.Path.T.WorkOrder.F.FirstWorkOrderStateHistoryID.F.EffectiveDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl WOChartCountByOpenedDate = WOChartCountBase(TId.WOChartCountByOpenedDate, dsMB.Path.T.WorkOrder.F.FirstOpenWorkOrderStateHistoryID.F.EffectiveDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl WOChartCountByEndedDate = WOChartCountBase(TId.WOChartCountByEndedDate, dsMB.Path.T.WorkOrder.F.CompletionWorkOrderStateHistoryID.F.EffectiveDate, ReportViewLogic.IntervalSettings.Weeks);
		public static Tbl WOChartCount = WOChartCountBase(TId.WOChartCount, (DBI_Path)null, null);
		#endregion
		#region -     Work Order time-span charts
		static Tbl WOChartDurationBase(Tbl.TblIdentification title, TblLeafNode valueNode, Expression.Function aggregateFunction, ReportViewLogic.IntervalSettings intervalValueScaling, SqlExpression filter) {
			return WOChartBase(title, (Report r, ReportViewLogic logic) => new Reports.WOChartDurationBase(r, logic, valueNode, aggregateFunction), RTblBase.SetChartIntervalValueScaling(intervalValueScaling), filterAttribute: RTblBase.SetFilter(filter), defaultGroupByUnit: true);
		}
		static readonly SqlExpression actualDurationExpression = WOStatisticCalculation.ActualDuration(dsMB.Path.T.WorkOrder);
		static readonly TblQueryValueNode actualDuration = TblQueryValueNode.New(new SameKeyContextAs(dsMB.Path.T.WorkOrder.F.EndDateEstimate.Key()).K(KB.K("Actual Duration")),
			new TblQueryExpression(actualDurationExpression));
		public static Tbl WOChartAverageDuration = WOChartDurationBase(TId.WOChartAverageDuration,
			actualDuration, Expression.Function.Avg, ReportViewLogic.IntervalSettings.Days, actualDurationExpression.IsNotNull());
		public static Tbl WOChartTotalDuration = WOChartDurationBase(TId.WOChartTotalDuration,
			actualDuration, Expression.Function.Sum, ReportViewLogic.IntervalSettings.Days, actualDurationExpression.IsNotNull());
		public static Tbl WOChartAverageDowntime = WOChartDurationBase(TId.WOChartAverageDowntime,
			TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Downtime), Expression.Function.Avg, ReportViewLogic.IntervalSettings.Hours, new SqlExpression(dsMB.Path.T.WorkOrder.F.Downtime).IsNotNull());
		public static Tbl WOChartTotalDowntime = WOChartDurationBase(TId.WOChartTotalDowntime,
			TblColumnNode.New(dsMB.Path.T.WorkOrder.F.Downtime), Expression.Function.Sum, ReportViewLogic.IntervalSettings.Hours, new SqlExpression(dsMB.Path.T.WorkOrder.F.Downtime).IsNotNull());
		public static Tbl WOChartAverageSelectedDuration = WOSelectableFieldChartBase(TId.WOChartAverageSelectedDuration,
			(Report r, ReportViewLogic logic) => new Reports.WOChartSelectedDurationBase(r, logic, Expression.Function.Avg),
			// the condition on being a simple path or not a column node is to exclude Task fields, which are not of an analytical nature.
			(leafNode => (!(leafNode is TblColumnNode colNode) || colNode.Path.IsSimple) && leafNode.ReferencedType is IntervalTypeInfo iti && iti.Epsilon is TimeSpan eps && (eps.Ticks % TimeSpan.TicksPerDay) == 0),
			RTblBase.SetChartIntervalValueScaling(ReportViewLogic.IntervalSettings.Days), defaultGroupByUnit: true);
		#endregion
		public static Tbl WOChartLifetime = WOChartBase(TId.WOChartLifetime, (Report r, ReportViewLogic logic) => new Reports.WOChartLifetime(r, logic));
		// TODO: Work Order Demand and Actual costs totalled (do we need avg?) per grouping, requires view-costs permissions.
		#endregion
		#endregion
		#region - WO Resources
		#region -   WO Resource Demands
		#region -     WO Generic resources
		public static DelayedCreateTbl WOResourceDemand = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.Quantity)
													);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.Demand.F.DemandItemID.F.Quantity),
														new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.Quantity)
													);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			SqlExpression actualTime = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.ActualQuantity),
														new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.ActualQuantity)
													);
			TypeFormatter actualTimeFormatter = actualTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression actualCount = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.Demand.F.DemandItemID.F.ActualQuantity),
														new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.ActualQuantity),
														new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.ActualQuantity)
													);
			TypeFormatter actualCountFormatter = actualCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedActualQuantityType = new StringTypeInfo(Math.Min((int)actualTimeFormatter.SizingInformation.MinWidth, (int)actualCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)actualTimeFormatter.SizingInformation.MaxWidth, (int)actualCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = WOReportResourceDemandCategoryClassifier(dsMB.Path.T.Demand.F);
			return new Tbl(dsMB.Schema.T.Demand,
				TId.WorkOrderResourceDemand,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.Demand,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandItemID.F.ItemLocationID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.LaborInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandItemID.F.ItemLocationID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.LaborInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.LaborOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					CodeDescColumnBuilder.New(dsMB.Path.T.Demand.F.DemandItemID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID),
					WorkOrderColumns(dsMB.Path.T.Demand.F.WorkOrderID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified Demand quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide()),   // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.Demand.F.CostEstimate, FooterAggregateCol.Sum()),
					TblQueryValueNode.New(KB.K("Actual"), TblQueryCalculation.New(
																							(values => values[0] != null ? actualTimeFormatter.Format(values[0]) : values[1] != null ? actualCountFormatter.Format(values[1]) : null),
																							formattedActualQuantityType,
																							new TblQueryExpression(actualTime),
																							new TblQueryExpression(actualCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Actual Time"), actualTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Actual Count"), actualCount, DefaultShowInDetailsCol.Hide()),     // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.Demand.F.ActualCost, FooterAggregateCol.Sum())
				).LayoutArray()
			);
		});
		#endregion
		#region -     WO Items
		public static Tbl WODemandItem = new Tbl(dsMB.Schema.T.DemandItem,
			TId.WorkOrderItem,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandItem,
				ItemColumns(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ItemID, ShowXIDAndUOM),
				ActualItemLocationColumns(dsMB.Path.T.DemandItem.F.ItemLocationID.F.ActualItemLocationID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandItem.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.DemandItem.F.Quantity, DefaultShowInDetailsCol.Show()),               // DemandItems are not consistently the same item, so Sum makes no sense
				TblColumnNode.New(dsMB.Path.T.DemandItem.F.ActualQuantity, DefaultShowInDetailsCol.Show()),         // DemandItems are not consistently the same item, so Sum makes no sense
				TblColumnNode.New(dsMB.Path.T.DemandItem.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.DemandItem.F.DemandID.F.ActualCost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region -     WO Generic Inside resources
		public static DelayedCreateTbl WOResourceInsideDemand = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.Quantity);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.Quantity);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			SqlExpression actualTime = new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.ActualQuantity);
			TypeFormatter actualTimeFormatter = actualTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression actualCount = new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.ActualQuantity);
			TypeFormatter actualCountFormatter = actualCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedActualQuantityType = new StringTypeInfo(Math.Min((int)actualTimeFormatter.SizingInformation.MinWidth, (int)actualCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)actualTimeFormatter.SizingInformation.MaxWidth, (int)actualCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = new RecordTypeClassifierByLinkages(true,
					dsMB.Path.T.Demand.F.DemandLaborInsideID.F.LaborInsideID,
					dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.OtherWorkInsideID
				);
			return new Tbl(dsMB.Schema.T.Demand,
				TId.WorkOrderResourceDemand,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID).IsNotNull()
							.Or(new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID).IsNotNull())),
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.Demand,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.LaborInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.LaborInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					// Can't do employee because of alternative paths to the employee record.
					//EmployeeColumns(dsMB.Path.T.Demand.F.DemandLaborInsideID.F.LaborInsideID.F.EmployeeID),
					WorkOrderColumns(dsMB.Path.T.Demand.F.WorkOrderID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified Demand quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide()),   // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.Demand.F.CostEstimate, FooterAggregateCol.Sum()),
					TblQueryValueNode.New(KB.K("Actual"), TblQueryCalculation.New(
																							(values => values[0] != null ? actualTimeFormatter.Format(values[0]) : values[1] != null ? actualCountFormatter.Format(values[1]) : null),
																							formattedActualQuantityType,
																							new TblQueryExpression(actualTime),
																							new TblQueryExpression(actualCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Actual Time"), actualTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Actual Count"), actualCount, DefaultShowInDetailsCol.Hide()),     // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.Demand.F.ActualCost, FooterAggregateCol.Sum())
				).LayoutArray()
			);
		});
		#endregion
		#region -     WO Generic Outside resources
		public static DelayedCreateTbl WOResourceOutsideDemand = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.Quantity);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.Quantity);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			SqlExpression actualTime = new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.ActualQuantity);
			TypeFormatter actualTimeFormatter = actualTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression actualCount = new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.ActualQuantity);
			TypeFormatter actualCountFormatter = actualCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedActualQuantityType = new StringTypeInfo(Math.Min((int)actualTimeFormatter.SizingInformation.MinWidth, (int)actualCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)actualTimeFormatter.SizingInformation.MaxWidth, (int)actualCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = new RecordTypeClassifierByLinkages(true,
					dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.LaborOutsideID,
					dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID
				);
			return new Tbl(dsMB.Schema.T.Demand,
				TId.WorkOrderResourceDemand,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID).IsNotNull()
							.Or(new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID).IsNotNull())),
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.Demand,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.LaborOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.Demand.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					// Can't do vendor because of alternative paths to the vendor record.
					//VendorColumns(dsMB.Path.T.Demand.F.DemandLaborOutsideID.F.LaborOutsideID.F.VendorID),
					WorkOrderColumns(dsMB.Path.T.Demand.F.WorkOrderID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.Demand.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified Demand quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide()),   // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.Demand.F.CostEstimate, FooterAggregateCol.Sum()),
					TblQueryValueNode.New(KB.K("Actual"), TblQueryCalculation.New(
																							(values => values[0] != null ? actualTimeFormatter.Format(values[0]) : values[1] != null ? actualCountFormatter.Format(values[1]) : null),
																							formattedActualQuantityType,
																							new TblQueryExpression(actualTime),
																							new TblQueryExpression(actualCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Actual Time"), actualTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Actual Count"), actualCount, DefaultShowInDetailsCol.Hide()),     // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.Demand.F.ActualCost, FooterAggregateCol.Sum())
				).LayoutArray()
			);
		});
		#endregion
		#region -     WO Hourly Inside
		public static Tbl WODemandHourlyInside = new Tbl(dsMB.Schema.T.DemandLaborInside,
			TId.WorkOrderHourlyInside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandLaborInside,
				LaborInsideColumns(dsMB.Path.T.DemandLaborInside.F.LaborInsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.DemandLaborInside.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandLaborInside.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.DemandLaborInside.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),
				TblColumnNode.New(dsMB.Path.T.DemandLaborInside.F.ActualQuantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),
				TblColumnNode.New(dsMB.Path.T.DemandLaborInside.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.DemandLaborInside.F.DemandID.F.ActualCost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region -     WO Hourly Outside
		public static Tbl WODemandHourlyOutside = new Tbl(dsMB.Schema.T.DemandLaborOutside,
			TId.WorkOrderHourlyOutside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandLaborOutside,
				LaborOutsideColumns(dsMB.Path.T.DemandLaborOutside.F.LaborOutsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.DemandLaborOutside.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandLaborOutside.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.DemandLaborOutside.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),
				TblColumnNode.New(dsMB.Path.T.DemandLaborOutside.F.ActualQuantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),
				TblColumnNode.New(dsMB.Path.T.DemandLaborOutside.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.DemandLaborOutside.F.DemandID.F.ActualCost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region -     WO Per Job Inside
		public static Tbl WODemandPerJobInside = new Tbl(dsMB.Schema.T.DemandOtherWorkInside,
			TId.WorkOrderPerJobInside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandOtherWorkInside,
				OtherWorkInsideColumns(dsMB.Path.T.DemandOtherWorkInside.F.OtherWorkInsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInside.F.Quantity, DefaultShowInDetailsCol.Show()),            // DemandOtherWorkInside are not consistently the same item, so Sum makes no sense
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInside.F.ActualQuantity, DefaultShowInDetailsCol.Show()),      // DemandOtherWorkInside are not consistently the same item, so Sum makes no sense
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInside.F.DemandID.F.ActualCost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region -     WO Per Job Outside
		public static Tbl WODemandPerJobOutside = new Tbl(dsMB.Schema.T.DemandOtherWorkOutside,
			TId.WorkOrderPerJobOutside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandOtherWorkOutside,
				OtherWorkOutsideColumns(dsMB.Path.T.DemandOtherWorkOutside.F.OtherWorkOutsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutside.F.Quantity, DefaultShowInDetailsCol.Show()),           // DemandOtherWorkOutside are not consistently the same item, so Sum makes no sense
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutside.F.ActualQuantity, DefaultShowInDetailsCol.Show()),     // DemandOtherWorkOutside are not consistently the same item, so Sum makes no sense
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutside.F.DemandID.F.ActualCost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region -     WO Miscellaneous
		public static Tbl WODemandMiscellaneousWorkOrderCost = new Tbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCost,
			TId.WorkOrderMiscellaneousExpense,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCost,
				MiscellaneousWorkOrderCostColumns(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.MiscellaneousWorkOrderCostID.PathToReferencedRow, ShowXID),
				WorkOrderColumns(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				TblColumnNode.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.ActualCost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#endregion
		#region -   WO Resource Actuals
		#region -     WO Generic resources
		public static DelayedCreateTbl WOResourceActual = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborInsideID.F.DemandLaborInsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsidePOID.F.POLineLaborID.F.DemandLaborOutsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsideNonPOID.F.DemandLaborOutsideID.F.Quantity)
													);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualItemID.F.DemandItemID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkInsideID.F.DemandOtherWorkInsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsideNonPOID.F.DemandOtherWorkOutsideID.F.Quantity)
													);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			SqlExpression actualTime = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborInsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsidePOID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsideNonPOID.F.Quantity)
													);
			TypeFormatter actualTimeFormatter = actualTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression actualCount = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualItemID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkInsideID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsidePOID.F.Quantity),
														new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsideNonPOID.F.Quantity)
													);
			TypeFormatter actualCountFormatter = actualCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedActualQuantityType = new StringTypeInfo(Math.Min((int)actualTimeFormatter.SizingInformation.MinWidth, (int)actualCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)actualTimeFormatter.SizingInformation.MaxWidth, (int)actualCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = WOReportResourceActualCategoryClassifier(dsMB.Path.T.AccountingTransaction.F);
			return new Tbl(dsMB.Schema.T.AccountingTransaction,
				TId.WorkOrderResourceActual,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					new MinimumDBVersionTbl(new Version(1, 1, 5, 5)),
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.Demand,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualItemID.F.DemandItemID.F.ItemLocationID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborInsideID.F.DemandLaborInsideID.F.LaborInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsidePOID.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsideNonPOID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkInsideID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsideNonPOID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualMiscellaneousWorkOrderCostID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualItemID.F.DemandItemID.F.ItemLocationID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborInsideID.F.DemandLaborInsideID.F.LaborInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsidePOID.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualLaborOutsideNonPOID.F.DemandLaborOutsideID.F.LaborOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkInsideID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsidePOID.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualOtherWorkOutsideNonPOID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.AccountingTransaction.F.ActualMiscellaneousWorkOrderCostID.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					CodeDescColumnBuilder.New(dsMB.Path.T.AccountingTransaction.F.ActualItemID.F.DemandItemID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID),
					WorkOrderColumns(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.DemandID.F.WorkOrderID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.DemandID.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.DemandID.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.DemandID.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified Demand quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide()),   // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.DemandID.F.CostEstimate, FooterAggregateCol.Sum()),
					TblQueryValueNode.New(KB.K("Actual"), TblQueryCalculation.New(
																							(values => values[0] != null ? actualTimeFormatter.Format(values[0]) : values[1] != null ? actualCountFormatter.Format(values[1]) : null),
																							formattedActualQuantityType,
																							new TblQueryExpression(actualTime),
																							new TblQueryExpression(actualCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Actual Time"), actualTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Actual Count"), actualCount, DefaultShowInDetailsCol.Hide()),     // Sum makes no sense for non homogenous items
					TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Cost, FooterAggregateCol.Sum())
				).LayoutArray()
			);
		});
		#endregion
		#region -     WO Items
		public static Tbl WOActualItem = new Tbl(dsMB.Schema.T.ActualItem,
			TId.WorkOrderItemActual,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualItem,
				ItemColumns(dsMB.Path.T.ActualItem.F.DemandItemID.F.ItemLocationID.F.ItemID, ShowXIDAndUOM),
				ActualItemLocationColumns(dsMB.Path.T.ActualItem.F.DemandItemID.F.ItemLocationID.F.ActualItemLocationID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualItem.F.DemandItemID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualItem.F.DemandItemID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualItem.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualItem.F.Quantity, DefaultShowInDetailsCol.Show()))               // ActualItems are not consistently the same item, so Sum makes no sense
			).LayoutArray()
		);
		#endregion
		#region -     WO Hourly Inside
		public static Tbl WOActualHourlyInside = new Tbl(dsMB.Schema.T.ActualLaborInside,
			TId.WorkOrderHourlyInsideActual,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualLaborInside,
				LaborInsideColumns(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.LaborInsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualLaborInside.F.DemandLaborInsideID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualLaborInside.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualLaborInside.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()))
			).LayoutArray()
		);
		#endregion
		#region -     WO Hourly Outside PO
		public static Tbl WOActualHourlyOutsidePO = new Tbl(dsMB.Schema.T.ActualLaborOutsidePO,
			TId.WorkOrderHourlyOutsidePOActual,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualLaborOutsidePO,
				LaborOutsideColumns(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.LaborOutsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				PurchaseOrderColumns(dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.POLineID.F.PurchaseOrderID, dsMB.Path.T.ActualLaborOutsidePO.F.POLineLaborID.F.POLineID.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID, null, ShowXID),
				ReceiptColumns(dsMB.Path.T.ActualLaborOutsidePO.F.ReceiptID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualLaborOutsidePO.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualLaborOutsidePO.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()))
			).LayoutArray()
		);
		#endregion
		#region -     WO Hourly Outside NonPO
		public static Tbl WOActualHourlyOutsideNonPO = new Tbl(dsMB.Schema.T.ActualLaborOutsideNonPO,
			TId.WorkOrderHourlyOutsideNonPOActual,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualLaborOutsideNonPO,
				LaborOutsideColumns(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.LaborOutsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualLaborOutsideNonPO.F.DemandLaborOutsideID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				VendorColumns(dsMB.Path.T.ActualLaborOutsideNonPO.F.VendorID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualLaborOutsideNonPO.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualLaborOutsideNonPO.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()))
			).LayoutArray()
		);
		#endregion
		#region -     WO Per Job Inside
		public static Tbl WOActualPerJobInside = new Tbl(dsMB.Schema.T.ActualOtherWorkInside,
			TId.WorkOrderPerJobInsideActual,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualOtherWorkInside,
				OtherWorkInsideColumns(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.OtherWorkInsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualOtherWorkInside.F.DemandOtherWorkInsideID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualOtherWorkInside.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualOtherWorkInside.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()))
			).LayoutArray()
		);
		#endregion
		#region -     WO Per Job Outside PO
		public static Tbl WOActualPerJobOutsidePO = new Tbl(dsMB.Schema.T.ActualOtherWorkOutsidePO,
			TId.WorkOrderPerJobOutsidePOActual,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualOtherWorkOutsidePO,
				OtherWorkOutsideColumns(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				PurchaseOrderColumns(dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.POLineID.F.PurchaseOrderID, dsMB.Path.T.ActualOtherWorkOutsidePO.F.POLineOtherWorkID.F.POLineID.F.PurchaseOrderID.L.PurchaseOrderExtras.PurchaseOrderID, null, ShowXID),
				ReceiptColumns(dsMB.Path.T.ActualOtherWorkOutsidePO.F.ReceiptID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualOtherWorkOutsidePO.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualOtherWorkOutsidePO.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()))
			).LayoutArray()
		);
		#endregion
		#region -     WO Per Job Outside NonPO
		public static Tbl WOActualPerJobOutsideNonPO = new Tbl(dsMB.Schema.T.ActualOtherWorkOutsideNonPO,
			TId.WorkOrderPerJobOutsideNonPOActual,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualOtherWorkOutsideNonPO,
				OtherWorkOutsideColumns(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.DemandOtherWorkOutsideID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				VendorColumns(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.VendorID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.AccountingTransactionID, ShowAll,
					TblColumnNode.New(dsMB.Path.T.ActualOtherWorkOutsideNonPO.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()))
			).LayoutArray()
		);
		#endregion
		#region -     WO Miscellaneous
		public static Tbl WOActualMiscellaneousWorkOrderCost = new Tbl(dsMB.Schema.T.ActualMiscellaneousWorkOrderCost,
			TId.WorkOrderMiscellaneousExpenseActual,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetNoSearchPrefixing()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ActualMiscellaneousWorkOrderCost,
				MiscellaneousWorkOrderCostColumns(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.MiscellaneousWorkOrderCostID.PathToReferencedRow, ShowXID),
				WorkOrderColumns(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.DemandID.F.WorkOrderID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.DemandMiscellaneousWorkOrderCostID.F.DemandID.F.WorkOrderExpenseCategoryID, ShowXID),
				AccountingTransactionColumns(dsMB.Path.T.ActualMiscellaneousWorkOrderCost.F.AccountingTransactionID, ShowAll)
			).LayoutArray()
		);
		#endregion
		#endregion
		#region -   WO Resource Actuals Charts
		static Tbl WOResourceChartBase(Tbl.TblIdentification title, FeatureGroup governingFeatureGroup, RTbl.BuildReportObjectDelegate reportBuilder, bool preventResourceCategoryGrouping, SqlExpression recordTypeFilterExpr, ReportViewLogic.IntervalSettings? intervalScaling = null,
			IEnumerable<Right> rights = null, TblLayoutNode resourceFilter = null) {
			var WOFR = dsMB.Path.T.WorkOrderFormReport.F;
			var WO = WOFR.WorkOrderID;
			var WOR = WOFR.WorkOrderID.L.WorkOrderExtras.WorkOrderID;
			RecordTypeClassifierByLinkages classifier = WOReportResourceDemandCategoryClassifier(WOFR.DemandID.F);
			List<Tbl.IAttr> iAttr = new List<Tbl.IAttr> {
				governingFeatureGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl(reportBuilder, typeof(ReportViewerControl),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetFilter(recordTypeFilterExpr),
					RTblBase.SetNoUserFieldSelectionAllowed(),
					RTblBase.SetChartIntervalValueScaling(intervalScaling)
				)
			};
			if (rights != null)
				foreach (var right in rights)
					iAttr.Add(new ViewCostPermissionImpliesTablePermissionsTbl(right, new TableOperationRightsGroup.TableOperation[] { TableOperationRightsGroup.TableOperation.Report }));

			var layout = new List<TblLayoutNode> {
				WorkOrderColumns(WO).AsTblGroup()
			};

			var filters = new List<TblLayoutNode>();
			if (resourceFilter != null)
				filters.Add(resourceFilter);
			filters.Add(DateHHMMColumnNode(WOFR.AccountingTransactionID.F.EntryDate));
			filters.Add(DateHHMMColumnNode(WOFR.AccountingTransactionID.F.EffectiveDate));
			filters.Add(TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), preventResourceCategoryGrouping ? RFmt.SetAllowUserGrouping(false) : null));
			filters.Add(TblColumnNode.New(WOFR.AccountingTransactionID.F.FromCostCenterID.F.Code, AccountingFeatureArg));
			filters.Add(TblColumnNode.New(WOFR.AccountingTransactionID.F.ToCostCenterID.F.Code, AccountingFeatureArg));
			layout.Add(TblGroupNode.New(KB.K("Resource"), NoAttr, filters.ToArray()));
			return new Tbl(dsMB.Schema.T.WorkOrderFormReport, title, iAttr.ToArray(), new TblLayoutNodeArray(layout.ToArray()));
		}

		static Tbl WOResourceCostChartBase(Tbl.TblIdentification title, FeatureGroup governingFeatureGroup, bool preventResourceCategoryGrouping, SqlExpression recordTypeFilterExpr, TblLeafNode grouping, IEnumerable<Right> rights = null, TblLayoutNode resourceFilter = null) {
			return WOResourceChartBase(title, governingFeatureGroup, (r, logic) => new Reports.WOResourceChartBase(r, logic, KB.K("Work Order Resource Costs"),
																	new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.CostEstimate),
																	new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.AccountingTransactionID.F.Cost),
																	grouping),
											preventResourceCategoryGrouping, recordTypeFilterExpr,
											rights: rights,
											resourceFilter: resourceFilter);
		}
		static Tbl WOResourceTimeChartBase(Tbl.TblIdentification title, FeatureGroup governingFeatureGroup, bool preventResourceCategoryGrouping, SqlExpression recordTypeFilterExpr, TblLeafNode grouping, IEnumerable<Right> rights = null, TblLayoutNode resourceFilter = null) {
			return WOResourceChartBase(title, governingFeatureGroup, (r, logic) => new Reports.WOResourceChartBase(r, logic, KB.K("Work Order Labor Time"),
																	SqlExpression.Coalesce(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID.F.Quantity),
																		new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID.F.Quantity)),
																	SqlExpression.Coalesce(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.AccountingTransactionID.F.ActualLaborInsideID.F.Quantity),
																		new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.AccountingTransactionID.F.ActualLaborOutsideNonPOID.F.Quantity),
																		new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.AccountingTransactionID.F.ActualLaborOutsidePOID.F.Quantity)),
																	grouping),
											preventResourceCategoryGrouping, recordTypeFilterExpr,
											intervalScaling: ReportViewLogic.IntervalSettings.Hours,
											rights: rights,
											resourceFilter: resourceFilter);
		}

		public static Tbl WOChartCosts = WOResourceCostChartBase(TId.WOChartCosts, TIGeneralMB3.AnyResourcesGroup, false, new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID).IsNotNull(),
			null,
			rights: new Right[] { Root.Rights.ViewCost.WorkOrderInside, Root.Rights.ViewCost.WorkOrderOutside, Root.Rights.ViewCost.WorkOrderItem, Root.Rights.ViewCost.WorkOrderMiscellaneous });
		private static readonly RecordTypeClassifierByLinkages ChartWOResourceClassifier = WOReportResourceDemandCategoryClassifier(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F);
		public static Tbl WOChartCostsByResourceType = WOResourceCostChartBase(TId.WOChartCostsByResourceType, TIGeneralMB3.AnyResourcesGroup, true, new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID).IsNotNull(),
			TblServerExprNode.New(KB.K("Resource Type"), ChartWOResourceClassifier.RecordTypeExpression, Fmt.SetEnumText(ChartWOResourceClassifier.EnumValueProvider)),
			rights: new Right[] { Root.Rights.ViewCost.WorkOrderInside, Root.Rights.ViewCost.WorkOrderOutside, Root.Rights.ViewCost.WorkOrderItem, Root.Rights.ViewCost.WorkOrderMiscellaneous });
		public static Tbl WOChartCostsByTrade = WOResourceCostChartBase(TId.WOChartCostsByTrade, TIGeneralMB3.LaborResourcesGroup, false,
			new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID).IsNotNull()
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID).IsNotNull())
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkInsideID).IsNotNull())
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkOutsideID).IsNotNull()),
			TblServerExprNode.New(KB.TOi(TId.Trade),
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.TradeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.TradeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.TradeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.TradeID)
				),
				Fmt.SetDisplayPath(dsMB.Path.T.Trade.F.Code)
			),
			rights: new Right[] { Root.Rights.ViewCost.WorkOrderInside, Root.Rights.ViewCost.WorkOrderOutside });
		public static Tbl WOChartCostsByEmployee = WOResourceCostChartBase(TId.WOChartCostsByEmployee, TIGeneralMB3.LaborResourcesGroup, false,
			new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID).IsNotNull()
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkInsideID).IsNotNull()),
			TblServerExprNode.New(KB.TOi(TId.Employee),
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.EmployeeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.EmployeeID)
				),
				Fmt.SetDisplayPath(dsMB.Path.T.Employee.F.ContactID.F.Code)
			),
			rights: new Right[] { Root.Rights.ViewCost.WorkOrderInside },
			resourceFilter: LaborInsideColumns(dsMB.Path.T.WorkOrderFormReport.F.LaborInsideID).AsTblGroup());
		public static Tbl WOChartCostsByVendor = WOResourceCostChartBase(TId.WOChartCostsByVendor, TIGeneralMB3.LaborResourcesGroup, false,
			new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID).IsNotNull()
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkOutsideID).IsNotNull()),
			TblServerExprNode.New(KB.TOi(TId.Vendor),
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.VendorID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.VendorID)
				),
				Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)
			),
			rights: new Right[] { Root.Rights.ViewCost.WorkOrderOutside },
			resourceFilter: LaborOutsideColumns(dsMB.Path.T.WorkOrderFormReport.F.LaborOutsideID).AsTblGroup());

		public static Tbl WOChartHours = WOResourceTimeChartBase(TId.WOChartLaborTime, TIGeneralMB3.LaborResourcesGroup, false,
			new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID).IsNotNull()
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID).IsNotNull()),
			null, null,
			resourceFilter: TradeColumns(dsMB.Path.T.WorkOrderFormReport.F.TradeID).AsTblGroup());
		public static Tbl WOChartHoursByTrade = WOResourceTimeChartBase(TId.WOChartLaborTimeByTrade, TIGeneralMB3.LaborResourcesGroup, false,
			new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID).IsNotNull()
				.Or(new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID).IsNotNull()),
			TblServerExprNode.New(KB.TOi(TId.Trade),
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.TradeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.TradeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.TradeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.TradeID)
				),
				Fmt.SetDisplayPath(dsMB.Path.T.Trade.F.Code)
			));
		public static Tbl WOChartHoursByEmployee = WOResourceTimeChartBase(TId.WOChartLaborTimeByEmployee, TIGeneralMB3.LaborResourcesGroup, false, new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID).IsNotNull(),
			TblServerExprNode.New(KB.TOi(TId.Employee),
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborInsideID.F.LaborInsideID.F.EmployeeID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkInsideID.F.OtherWorkInsideID.F.EmployeeID)
				),
				Fmt.SetDisplayPath(dsMB.Path.T.Employee.F.ContactID.F.Code)
			),
			resourceFilter: LaborInsideColumns(dsMB.Path.T.WorkOrderFormReport.F.LaborInsideID).AsTblGroup());
		public static Tbl WOChartHoursByVendor = WOResourceTimeChartBase(TId.WOChartLaborTimeByVendor, TIGeneralMB3.LaborResourcesGroup, false, new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID).IsNotNull(),
			TblServerExprNode.New(KB.TOi(TId.Vendor),
				SqlExpression.Coalesce(
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandLaborOutsideID.F.LaborOutsideID.F.VendorID),
					new SqlExpression(dsMB.Path.T.WorkOrderFormReport.F.DemandID.F.DemandOtherWorkOutsideID.F.OtherWorkOutsideID.F.VendorID)
				),
				Fmt.SetDisplayPath(dsMB.Path.T.Vendor.F.Code)
			),
			resourceFilter: LaborOutsideColumns(dsMB.Path.T.WorkOrderFormReport.F.LaborOutsideID).AsTblGroup());
		#endregion
		#endregion
		#region - WO State History reports
		#region -   WOStateHistory
		public static Tbl WOStateHistory = new Tbl(dsMB.Schema.T.WorkOrderStateHistory,
			TId.WorkOrderStateHistory,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				new PrimaryRecordArg(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.PathToReferencedRow),
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID.F.Number),
					// Set No Search Prefixing because the primary path (and the root of all paths in this report) include a labelled reference to the WO from the WOSH record.
					RTblBase.SetNoSearchPrefixing(),
					WorkOrderStateHistoryChildDefinition(dsMB.Path.T.WorkOrderStateHistory)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderStateHistory
					, WorkOrderColumns(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, ShowXID)
			).LayoutArray()
		);
		#endregion
		#region -   WOStateHistorySummary
		public static Tbl WOStateHistorySummary = new Tbl(dsMB.Schema.T.WorkOrderStateHistory,
			TId.WorkOrderStateHistory.ReportSummary,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderStateHistory
				, WorkOrderColumns(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID, ShowXID)
				, WorkOrderStateHistoryAndExtraColumns(dsMB.Path.T.WorkOrderStateHistory)
			).LayoutArray()
		);
		#endregion
		#region -   WOChartStatus
		// Because this chart shows averages per work order, we need several special provisions:
		// 1 - We can't allow user filtering by State History record because this might hide some work orders (which should report as an average of zero)
		// 2 - We must only look at draft/open time; otherwise old work orders will show up as spending an average of huge amounts of time in whatever their final Status is.
		// All work orders will have at least one State History record (from when they were created) so the data is available to count the work orders in each user-defined
		// grouping.
		// Although grouping by properties of the Work Order would be meaningful, the method whereby charts are generated for the groups does not allow the count of
		// work orders in the group to be determined (the scope for the CountDistinct has to be different for each group) so for now we don't allow user grouping.
		public static Tbl WOChartStatus = new Tbl(dsMB.Schema.T.WorkOrderStateHistory,
			TId.WOChartStatus,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl( typeof(Thinkage.MainBoss.Controls.Reports.WOChartStatusReport), typeof(ReportViewerControl),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.FilterAsDraft)
									.Or(new SqlExpression(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderStateID.F.FilterAsOpen))),
					RTblBase.SetChartIntervalValueScaling(ReportViewLogic.IntervalSettings.Days),
					RTblBase.SetNoUserFieldSelectionAllowed(),
					RTblBase.SetNoUserGroupingAllowed()
				)
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderStateHistory
				, WorkOrderColumns(dsMB.Path.T.WorkOrderStateHistory.F.WorkOrderID)
			).LayoutArray()
		);
		#endregion
		#endregion
		#region - Employee
		public static Tbl EmployeeReport = new Tbl(dsMB.Schema.T.Employee,
			TId.Employee,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			EmployeeColumns(dsMB.Path.T.Employee, ShowAll).LayoutArray()
		);
		#endregion
		#region - Resource Definitions (except ItemLocation)
		#region -   LaborInside
		public static Tbl LaborInsideReport = new Tbl(dsMB.Schema.T.LaborInside,
			TId.HourlyInside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetDualLayoutDefault(defaultInColumns: true), RTblBase.SetPreferWidePage(true), RTblBase.SetAllowSummaryFormat(true))
			},
			ColumnBuilder.New(dsMB.Path.T.LaborInside,
				  LaborInsideColumns(dsMB.Path.T.LaborInside, ShowAll)
			).LayoutArray()
		);
		#endregion
		#region -   LaborOutside
		public static Tbl LaborOutsideReport = new Tbl(dsMB.Schema.T.LaborOutside,
			TId.HourlyOutside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetDualLayoutDefault(defaultInColumns: true),RTblBase.SetPreferWidePage(true), RTblBase.SetAllowSummaryFormat(true))
			},
			ColumnBuilder.New(dsMB.Path.T.LaborOutside,
				LaborOutsideColumns(dsMB.Path.T.LaborOutside, ShowAll)
			).LayoutArray()
		);
		#endregion
		#region -   OtherWorkInside
		public static Tbl OtherWorkInsideReport = new Tbl(dsMB.Schema.T.OtherWorkInside,
			TId.PerJobInside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetDualLayoutDefault(defaultInColumns: true),RTblBase.SetPreferWidePage(true), RTblBase.SetAllowSummaryFormat(true))
			},
			OtherWorkInsideColumns(dsMB.Path.T.OtherWorkInside, ShowAll).LayoutArray()
		);
		#endregion
		#region -   OtherWorkOutside
		public static Tbl OtherWorkOutsideReport = new Tbl(dsMB.Schema.T.OtherWorkOutside,
			TId.PerJobOutside,
			new Tbl.IAttr[] {
				LaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetDualLayoutDefault(defaultInColumns: true),RTblBase.SetPreferWidePage(true), RTblBase.SetAllowSummaryFormat(true))
			},
			OtherWorkOutsideColumns(dsMB.Path.T.OtherWorkOutside, ShowAll).LayoutArray()
		);
		#endregion
		#region -   MiscellaneousWorkOrderCost
		public static Tbl MiscellaneousWorkOrderCostReport = new Tbl(dsMB.Schema.T.MiscellaneousWorkOrderCost,
			TId.MiscellaneousCost,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.MiscellaneousWorkOrderCost,
				MiscellaneousWorkOrderCostColumns(dsMB.Path.T.MiscellaneousWorkOrderCost, ShowAll)
			).LayoutArray()
		);
		#endregion
		#endregion
		#region - WorkOrderAssignee
		public static Tbl WorkOrderAssigneeReport = new Tbl(dsMB.Schema.T.WorkOrderAssignee,
			TId.WorkOrderAssignee,
			new Tbl.IAttr[] {
				WorkOrdersAssignmentsGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderAssignee
				, ContactColumns(dsMB.Path.T.WorkOrderAssignee.F.ContactID, ShowXID)
				, UserColumnsForContact(dsMB.Path.T.WorkOrderAssignee.F.ContactID.L.User.ContactID)
				, TblColumnNode.New(dsMB.Path.T.WorkOrderAssignee.F.Id.L.WorkOrderAssigneeStatistics.WorkOrderAssigneeID.F.NumNew, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.WorkOrderAssignee.F.Id.L.WorkOrderAssigneeStatistics.WorkOrderAssigneeID.F.NumInProgress, DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#endregion

		#region Tasks (WorkOrderTemplate)

		#region WorkOrderTemplate
		public static Tbl WorkOrderTemplateReport = new Tbl(dsMB.Schema.T.WorkOrderTemplateReport,
			TId.Task,
			new Tbl.IAttr[] {
				SchedulingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new PrimaryRecordArg(dsMB.Path.T.WorkOrderTemplateReport.F.WorkOrderTemplateID.PathToReferencedRow),
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetPageBreakDefault(true),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.WorkOrderTemplateReport.F.WorkOrderTemplateID.F.Code),
					RTblBase.SetParentChildInformation(KB.TOc(TId.TaskResource),
									RTblBase.ColumnsRowType.JoinWithChildren,
									dsMB.Path.T.WorkOrderTemplateReport.F.Resource,
									ColumnBuilder.New(dsMB.Path.T.WorkOrderTemplateReport
										, TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateReport.F.Resource, Fmt.SetEnumText(TIReports.WOTemplateRecordTypeNames))
										, TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateReport.F.Code)
										, TIReports.QuantityNodeForMixedQuantities(KB.K("Quantity"), dsMB.Path.T.WorkOrderTemplateReport.F.Quantity, dsMB.Path.T.WorkOrderTemplateReport.F.Labor)
										, TIReports.UoMNodeForMixedQuantities(new SqlExpression(dsMB.Path.T.WorkOrderTemplateReport.F.Quantity), new SqlExpression(dsMB.Path.T.WorkOrderTemplateReport.F.Labor), dsMB.Path.T.WorkOrderTemplateReport.F.UOM)
									).LayoutArray(),
									null,
									sortingPaths: new DBI_Path[] {
										dsMB.Path.T.WorkOrderTemplateReport.F.Resource,
										dsMB.Path.T.WorkOrderTemplateReport.F.Code
									}
						)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderTemplateReport
				, TaskColumns(dsMB.Path.T.WorkOrderTemplateReport.F.WorkOrderTemplateID, ShowForTask)
			).LayoutArray()
		);
		#endregion
		#region WorkOrderTemplateSummary
		public static Tbl WorkOrderTemplateSummary = new Tbl(dsMB.Schema.T.WorkOrderTemplate,
			TId.Task.ReportSummary,
			new Tbl.IAttr[] {
				SchedulingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderTemplate
				, TaskColumns(dsMB.Path.T.WorkOrderTemplate, ShowForTaskSummary)
			).LayoutArray()
		);
		#endregion
		#region - WOTemplateResource
		#region -   WO Template Generic resources
		public static DelayedCreateTbl WOTemplateResource = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.Quantity),
														new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.Quantity)
													);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = SqlExpression.Coalesce(
														new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandItemTemplateID.F.Quantity),
														new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.Quantity),
														new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.Quantity)
													);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = WOTemplateReportResourceDemandCategoryClassifier(dsMB.Path.T.DemandTemplate.F);
			return new Tbl(dsMB.Schema.T.DemandTemplate,
				TId.TaskResource,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.DemandTemplate,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandItemTemplateID.F.ItemLocationID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandMiscellaneousWorkOrderCostTemplateID.F.MiscellaneousWorkOrderCostID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandItemTemplateID.F.ItemLocationID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandMiscellaneousWorkOrderCostTemplateID.F.MiscellaneousWorkOrderCostID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					CodeDescColumnBuilder.New(dsMB.Path.T.DemandTemplate.F.DemandItemTemplateID.F.ItemLocationID.F.ItemID.F.UnitOfMeasureID),
					TaskColumns(dsMB.Path.T.DemandTemplate.F.WorkOrderTemplateID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified DemandTemplate quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide())   // Sum makes no sense for non homogenous items
				).LayoutArray()
			);
		});
		#endregion
		#region -   WOTemplateItem
		public static Tbl WOTemplateItem = new Tbl(dsMB.Schema.T.DemandItemTemplate,
			TId.TaskDemandItem,
			new Tbl.IAttr[] {
				SchedulingAndItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandItemTemplate,
					ItemColumns(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.ItemID, ShowXIDAndUOM),
					ActualItemLocationColumns(dsMB.Path.T.DemandItemTemplate.F.ItemLocationID.F.ActualItemLocationID, ShowXID),
					TaskColumns(dsMB.Path.T.DemandItemTemplate.F.DemandTemplateID.F.WorkOrderTemplateID, ShowXID),
					WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandItemTemplate.F.DemandTemplateID.F.WorkOrderExpenseCategoryID),
					TblColumnNode.New(dsMB.Path.T.DemandItemTemplate.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),         // TODO: Only Sum if the Item is consistent
					TblColumnNode.New(dsMB.Path.T.DemandItemTemplate.F.DemandTemplateID.F.EstimateCost)
			).LayoutArray()
		);
		#endregion
		#region -   WO Template Inside work resources
		public static DelayedCreateTbl WOTemplateInsideResource = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.Quantity);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.Quantity);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = new RecordTypeClassifierByLinkages(true,
					dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.LaborInsideID,
					dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID
				);
			return new Tbl(dsMB.Schema.T.DemandTemplate,
				TId.TaskResource,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID).IsNotNull()
							.Or(new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID).IsNotNull())),
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.DemandTemplate,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkInsideTemplateID.F.OtherWorkInsideID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					// Can't do employee because of alternative paths to the employee record.
					//EmployeeColumns(dsMB.Path.T.DemandTemplate.F.DemandLaborInsideTemplateID.F.LaborInsideID.F.EmployeeID),
					TaskColumns(dsMB.Path.T.DemandTemplate.F.WorkOrderTemplateID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified DemandTemplate quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide())   // Sum makes no sense for non homogenous items
				).LayoutArray()
			);
		});
		#endregion
		#region -   WO Template Outside work resources
		public static DelayedCreateTbl WOTemplateOutsideResource = new DelayedCreateTbl(delegate () {
			SqlExpression estimatedTime = new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.Quantity);
			TypeFormatter estimatedTimeFormatter = estimatedTime.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			SqlExpression estimatedCount = new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.Quantity);
			TypeFormatter estimatedCountFormatter = estimatedCount.ResultType.GetTypeFormatter(Application.InstanceFormatCultureInfo);
			StringTypeInfo formattedEstimatedQuantityType = new StringTypeInfo(Math.Min((int)estimatedTimeFormatter.SizingInformation.MinWidth, (int)estimatedCountFormatter.SizingInformation.MinWidth),
																	Math.Max((int)estimatedTimeFormatter.SizingInformation.MaxWidth, (int)estimatedCountFormatter.SizingInformation.MaxWidth), 0, true, true, true);
			RecordTypeClassifierByLinkages classifier = new RecordTypeClassifierByLinkages(true,
					dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID,
					dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID
				);
			return new Tbl(dsMB.Schema.T.DemandTemplate,
				TId.TaskResource,
				new Tbl.IAttr[] {
					ItemResourcesGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(
						RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID).IsNotNull()
							.Or(new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID).IsNotNull())),
						RTblBase.SetAllowSummaryFormat(true),
						RTblBase.SetDualLayoutDefault(true),
						RTblBase.SetPreferWidePage(true),
						RTblBase.SetNoSearchPrefixing()
					)
				},
				ColumnBuilder.New(dsMB.Path.T.DemandTemplate,
					// TODO: The enum text provider should make the column non-sortable.
					// TODO: The enum text provider must be used for group labels (in group headers, footers, and doc map)
					TblServerExprNode.New(KB.K("Resource Type"), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), DefaultShowInDetailsCol.Show()),
					TblServerExprNode.New(KB.K("Resource Code"), SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Code),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Code)
																			),
																		 SqlExpression.Coalesce(
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.Hidden),
																				new SqlExpression(dsMB.Path.T.DemandTemplate.F.DemandOtherWorkOutsideTemplateID.F.OtherWorkOutsideID.F.Hidden)
																			), DefaultShowInDetailsCol.Show()),
					// Can't do vendor because of alternative paths to the vendor record.
					//VendorColumns(dsMB.Path.T.DemandTemplate.F.DemandLaborOutsideTemplateID.F.LaborOutsideID.F.VendorID),
					TaskColumns(dsMB.Path.T.DemandTemplate.F.WorkOrderTemplateID, ShowXID),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Code, DefaultShowInDetailsCol.Show(), AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Desc, AccountingFeatureArg),
					TblColumnNode.New(dsMB.Path.T.DemandTemplate.F.WorkOrderExpenseCategoryID.F.Comment, AccountingFeatureArg),
					// show the unified DemandTemplate quantity
					TblQueryValueNode.New(KB.K("Demanded"), TblQueryCalculation.New(
																							(values => values[0] != null ? estimatedTimeFormatter.Format(values[0]) : values[1] != null ? estimatedCountFormatter.Format(values[1]) : null),
																							formattedEstimatedQuantityType,
																							new TblQueryExpression(estimatedTime),
																							new TblQueryExpression(estimatedCount)
																						),
																						DefaultShowInDetailsCol.Show(),
																						Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far)
													),
					TblServerExprNode.New(KB.K("Estimated Time"), estimatedTime, DefaultShowInDetailsCol.Hide(), FooterAggregateCol.Sum()),
					TblServerExprNode.New(KB.K("Estimated Count"), estimatedCount, DefaultShowInDetailsCol.Hide())   // Sum makes no sense for non homogenous items
				).LayoutArray()
			);
		});
		#endregion
		#region -   WOTemplateHourlyInside
		public static Tbl WOTemplateHourlyInside = new Tbl(dsMB.Schema.T.DemandLaborInsideTemplate,
			TId.TaskDemandHourlyInside,
			new Tbl.IAttr[] {
				SchedulingAndLaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandLaborInsideTemplate,
				LaborInsideColumns(dsMB.Path.T.DemandLaborInsideTemplate.F.LaborInsideID, ShowXIDAndCost),
				TaskColumns(dsMB.Path.T.DemandLaborInsideTemplate.F.DemandTemplateID.F.WorkOrderTemplateID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandLaborInsideTemplate.F.DemandTemplateID.F.WorkOrderExpenseCategoryID),
				TblColumnNode.New(dsMB.Path.T.DemandLaborInsideTemplate.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),          // TODO: Only Sum if the LaborInside is consistent
				TblColumnNode.New(dsMB.Path.T.DemandLaborInsideTemplate.F.DemandTemplateID.F.EstimateCost)
			).LayoutArray()
		);
		#endregion
		#region -   WOTemplateHourlyOutside
		public static Tbl WOTemplateHourlyOutside = new Tbl(dsMB.Schema.T.DemandLaborOutsideTemplate,
			TId.TaskDemandHourlyOutside,
			new Tbl.IAttr[] {
				SchedulingAndLaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandLaborOutsideTemplate,
				LaborOutsideColumns(dsMB.Path.T.DemandLaborOutsideTemplate.F.LaborOutsideID, ShowXIDAndCost),
				TaskColumns(dsMB.Path.T.DemandLaborOutsideTemplate.F.DemandTemplateID.F.WorkOrderTemplateID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandLaborOutsideTemplate.F.DemandTemplateID.F.WorkOrderExpenseCategoryID),
				TblColumnNode.New(dsMB.Path.T.DemandLaborOutsideTemplate.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),         // TODO: Only Sum if the LaborOutside is consistent
				TblColumnNode.New(dsMB.Path.T.DemandLaborOutsideTemplate.F.DemandTemplateID.F.EstimateCost)
			).LayoutArray()
		);
		#endregion
		#region -   WOTemplatePerJobInside
		public static Tbl WOTemplatePerJobInside = new Tbl(dsMB.Schema.T.DemandOtherWorkInsideTemplate,
			TId.TaskDemandPerJobInside,
			new Tbl.IAttr[] {
				SchedulingAndLaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandOtherWorkInsideTemplate,
					OtherWorkInsideColumns(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.OtherWorkInsideID, ShowXIDAndCost),
					TaskColumns(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.DemandTemplateID.F.WorkOrderTemplateID, ShowXID),
					WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.DemandTemplateID.F.WorkOrderExpenseCategoryID),
					TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),          // TODO: Only Sum if the OtherWorkInside is consistent
					TblColumnNode.New(dsMB.Path.T.DemandOtherWorkInsideTemplate.F.DemandTemplateID.F.EstimateCost)
			).LayoutArray()
		);
		#endregion
		#region -   WOTemplatePerJobOutside
		public static Tbl WOTemplatePerJobOutside = new Tbl(dsMB.Schema.T.DemandOtherWorkOutsideTemplate,
			TId.TaskDemandPerJobOutside,
			new Tbl.IAttr[] {
				SchedulingAndLaborResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandOtherWorkOutsideTemplate,
				OtherWorkOutsideColumns(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.OtherWorkOutsideID, ShowXIDAndCost),
				TaskColumns(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.DemandTemplateID.F.WorkOrderTemplateID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.DemandTemplateID.F.WorkOrderExpenseCategoryID),
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.Quantity, DefaultShowInDetailsCol.Show(), FooterAggregateCol.Sum()),         // TODO: Only Sum if the OtherWorkOutside is consistent
				TblColumnNode.New(dsMB.Path.T.DemandOtherWorkOutsideTemplate.F.DemandTemplateID.F.EstimateCost)
			).LayoutArray()
		);
		#endregion
		#region -   WOTemplateMisc
		public static Tbl WOTemplateMisc = new Tbl(dsMB.Schema.T.DemandMiscellaneousWorkOrderCostTemplate,
			TId.TaskMiscellaneousExpense,
			new Tbl.IAttr[] {
				SchedulingAndItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate,
				MiscellaneousWorkOrderCostColumns(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.MiscellaneousWorkOrderCostID.PathToReferencedRow, ShowXIDAndCost),
				TaskColumns(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.DemandTemplateID.F.WorkOrderTemplateID, ShowXID),
				WorkOrderExpenseCategoryColumns(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.DemandTemplateID.F.WorkOrderExpenseCategoryID),
				TblColumnNode.New(dsMB.Path.T.DemandMiscellaneousWorkOrderCostTemplate.F.DemandTemplateID.F.EstimateCost)
			).LayoutArray()
		);
		#endregion
		#endregion
		#endregion

		#region Chargeback
		#region ChargebackForm

		static ColumnBuilder ChargebackColumns(dsMB.PathClass.PathToChargebackLink CB, FieldSelect effect = null) {
			return ChargebackColumns(CB.PathToReferencedRow, effect);
		}
		static ColumnBuilder ChargebackColumns(dsMB.PathClass.PathToChargebackRow CB, FieldSelect effect = null) {
			FieldSelect contactEffect = null;
			FieldSelect unitEffect = null;

			if (effect == ShowForChargeback) {
				contactEffect = ShowContactBusPhoneEmail;
				unitEffect = ShowUnitAndContainingUnitAndUnitAddress;
			}
			else if (effect == ShowForChargebackSummary) {
				contactEffect = ShowXID;
				unitEffect = ShowXID;
			}

			return ColumnBuilder.New(CB)
				.ColumnSection(StandardReport.LeftColumnFmtId
					, WorkOrderColumns(CB.F.WorkOrderID, effect.XIDIfOneOf(ShowForChargeback, ShowForChargebackSummary, ShowXID), excludeUnits: true)
				)
				.ColumnSection(StandardReport.RightColumnFmtId
					, TblColumnNode.New(CB.F.Code, effect.ShowIfOneOf(ShowForChargeback, ShowForChargebackSummary, ShowXID))
					, ContactColumns(CB.F.BillableRequestorID.F.ContactID, contactEffect)
					, UnitColumns(CB.F.WorkOrderID.F.UnitLocationID, unitEffect)
					, TblColumnNode.New(CB.F.TotalCost, FooterAggregateCol.Sum(), FooterAggregateCol.Average(), effect.ShowIfOneOf(ShowForChargebackSummary))
				)
				.ColumnSection(StandardReport.MultiLineRowsFmtId
					, TblColumnNode.New(CB.F.Comment, effect.ShowIfOneOf(ShowForChargeback))
				);
		}
		public static ColumnBuilder ChargebackDetailsColumns = ColumnBuilder.New(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.PathToReferencedRow,
					  TblColumnNode.New(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.ChargebackLineCategoryID.F.Code, DefaultShowInDetailsCol.Hide())
					, TblColumnNode.New(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.ChargebackLineCategoryID.F.Desc)
					, TblColumnNode.New(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.Comment, DefaultShowInDetailsCol.Hide())
					, TblServerExprNode.New(null, new SqlExpression(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.CorrectionID).NEq(new SqlExpression(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID)), Fmt.SetEnumText(TIReports.IsCorrectionNames), Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Far))
					, AccountingTransactionColumns(dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.AccountingTransactionID, ShowXID)
					);

		// The only difference between the two reports is the Form versus non Form headers
		private static Tbl MakeChargebackFormReport(bool singleRecord) {
			var rtblArgs = new List<RTblBase.ICtorArg>() {
				RTblBase.SetReportCaptionText(TId.Chargeback.Compose(FormReportCaption)),
				RTblBase.SetReportTitle(TId.Chargeback.Compose(FormReportTitle)),
				RTblBase.SetAllowSummaryFormat(false),
				RTblBase.SetNoUserGroupingAllowed(),
				RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.ChargebackFormReport.F.ChargebackID.F.Code),
				RTblBase.SetParentChildInformation(KB.TOi(TId.ChargebackActivity),
					RTblBase.ColumnsRowType.JoinWithChildren,
					dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID,
					ChargebackDetailsColumns.LayoutArray(),
					new TblLeafNode[] {
						TblColumnNode.New(dsMB.Path.T.ChargebackFormReport.F.ChargebackID.F.Code)
					},
					new DBI_Path[] {
						dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.ChargebackLineCategoryID.F.Code,
						dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.AccountingTransactionID.F.EffectiveDate
					}
				)
			};
			if (singleRecord) {
				rtblArgs.Add(RTblBase.SetNoUserFilteringAllowed());
				rtblArgs.Add(RTblBase.SetNoUserSortingAllowed());
			}
			return new Tbl(dsMB.Schema.T.ChargebackFormReport,
				singleRecord ? TId.Chargeback.ReportSingle : TId.Chargeback,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new PrimaryRecordArg(dsMB.Path.T.ChargebackFormReport.F.ChargebackID.PathToReferencedRow),
					CommonTblAttrs.ViewCostsDefinedBySchema,
					new RTbl(typeof(Thinkage.MainBoss.Controls.Reports.MainBossFormReport), typeof(ReportViewerControl), rtblArgs.ToArray())
				},
				ColumnBuilder.New(dsMB.Path.T.ChargebackFormReport
					, ChargebackColumns(dsMB.Path.T.ChargebackFormReport.F.ChargebackID, ShowForChargeback)
				).LayoutArray()
			);
		}
		public static Tbl ChargebackFormReport = MakeChargebackFormReport(singleRecord: false);
		public static Tbl SingleChargebackFormReport = MakeChargebackFormReport(singleRecord: true);
		#endregion
		#region ChargebackHistory
		public static Tbl ChargebackHistoryReport = new Tbl(dsMB.Schema.T.ChargebackFormReport,
			TId.Chargeback.ReportHistory,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				new PrimaryRecordArg(dsMB.Path.T.ChargebackFormReport.F.ChargebackID.PathToReferencedRow),
				CommonTblAttrs.ViewCostsDefinedBySchema,
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetPageBreakDefault(true),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(dsMB.Path.T.ChargebackFormReport.F.ChargebackID.F.Code),
					RTblBase.SetParentChildInformation(KB.TOi(TId.ChargebackActivity),
						RTblBase.ColumnsRowType.JoinWithChildren,
						dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID,
						ChargebackDetailsColumns.LayoutArray(),
						sortingPaths:new DBI_Path[] {
							dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.ChargebackLineCategoryID.F.Code,
							dsMB.Path.T.ChargebackFormReport.F.ChargebackLineID.F.Comment
						}
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ChargebackFormReport
				, ChargebackColumns(dsMB.Path.T.ChargebackFormReport.F.ChargebackID, ShowForChargeback)
			).LayoutArray()
		);
		#endregion
		#region ChargebackSummary
		public static Tbl ChargebackSummaryReport = new Tbl(dsMB.Schema.T.Chargeback,
			TId.Chargeback.ReportSummary,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ChargebackColumns(dsMB.Path.T.Chargeback, ShowForChargebackSummary).LayoutArray()
		);
		#endregion
		#region ChargebackLines
		public static Tbl ChargebackLineReport = new Tbl(dsMB.Schema.T.ChargebackLine,
			TId.ChargebackActivity,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ChargebackLine
				, ChargebackColumns(dsMB.Path.T.ChargebackLine.F.ChargebackID, ShowForChargebackSummary)
				, CodeDescColumnBuilder.New(dsMB.Path.T.ChargebackLine.F.ChargebackLineCategoryID, ShowXID)
				, AccountingTransactionColumns(dsMB.Path.T.ChargebackLine.F.AccountingTransactionID, ShowXID)
				, TblColumnNode.New(dsMB.Path.T.ChargebackLine.F.Comment, FieldSelect.ShowNever)
			).LayoutArray()
		);
		#endregion
		#region ChargebackLineCategory
		public static Tbl ChargebackLineCategoryReport = new Tbl(dsMB.Schema.T.ChargebackLineCategory,
			TId.ChargebackCategory,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.ChargebackLineCategory,
				  CodeDescColumnBuilder.New(dsMB.Path.T.ChargebackLineCategory, ShowAll)
				, CostCenterColumns(dsMB.Path.T.ChargebackLineCategory.F.CostCenterID)
			).LayoutArray()
		);
		#endregion
		#region BillableRequestor
		public static Tbl BillableRequestorReport = new Tbl(dsMB.Schema.T.BillableRequestor,
			TId.BillableRequestor,
			new Tbl.IAttr[] {
				WorkOrdersGroup,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.BillableRequestor,
				  ContactColumns(dsMB.Path.T.BillableRequestor.F.ContactID, ShowAll)
				, CostCenterColumns(dsMB.Path.T.BillableRequestor.F.AccountsReceivableCostCenterID)
				, TblColumnNode.New(dsMB.Path.T.BillableRequestor.F.Comment)
			).LayoutArray()
		);
		#endregion
		#endregion

		#region Locations
		#region Location
		public static Tbl LocationReport = new Tbl(dsMB.Schema.T.Location,
			TId.Location,
			new Tbl.IAttr[] {
				LocationGroup,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(true),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					// TODO: The following filters are sort of pointless because the standard filter can filter by LocationType, but they are here for now to emulate the original Location report.
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Location.F.PostalAddressID).IsNull(), KB.TOc(TId.PostalAddress), applyFilterByDefault: false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Location.F.TemplateTemporaryStorageID).IsNull(), KB.TOc(TId.TemplateTemporaryStorage), applyFilterByDefault: false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Location.F.TemporaryStorageID).IsNull(), KB.TOc(TId.TemporaryStorage), applyFilterByDefault: false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Location.F.RelativeLocationID.F.UnitID).IsNull(), KB.TOc(TId.Unit), applyFilterByDefault: false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Location.F.RelativeLocationID.F.PermanentStorageID).IsNull(), KB.TOc(TId.Storeroom), applyFilterByDefault: false),
					RTblBase.SetFilter(new SqlExpression(dsMB.Path.T.Location.F.RelativeLocationID.F.PlainRelativeLocationID).IsNull(), KB.TOc(TId.SubLocation), applyFilterByDefault: false)
				)
			},
			new TblLayoutNodeArray(
				TblColumnNode.New(dsMB.Path.T.Location.F.Id.L.LocationReport.LocationID.F.LocationCode, DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.Location.F.Desc),
				TblColumnNode.New(dsMB.Path.T.Location.F.Id.L.LocationReport.LocationID.F.LocationDetail, DefaultShowInDetailsCol.Show()),  // Desc for most types, WO Subject for temporary, address for Postal, WOT Description for Template.
				TblClientTypeFormatterNode.New(dsMB.Path.T.Location.F.GISLocation),
				TblColumnNode.New(dsMB.Path.T.Location.F.Id.L.LocationReport.LocationID.F.LocationType),
				TblColumnNode.New(dsMB.Path.T.Location.F.Code, new MapSortCol(Statics.LocationSort), DefaultShowInDetailsCol.Show()),
				TblColumnNode.New(dsMB.Path.T.Location.F.Comment)
			)
		);
		#endregion
		#region PermanentStorage
		public static Tbl PermanentStorageReport = new Tbl(dsMB.Schema.T.PermanentStorage,
			TId.Storeroom,
			new Tbl.IAttr[] {
				StoreroomGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true),
				RTblBase.SetGrouping((dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID)))
			},
			new TblLayoutNodeArray(
				  TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID.F.Code, new MapSortCol(Statics.LocationSort), FieldSelect.ShowAlways)
				, TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code, FieldSelect.ShowAlways)
				, TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ExternalTag, FieldSelect.ShowNever)
				, TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.Rank, FieldSelect.ShowAlways)
				, TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.Desc, FieldSelect.ShowAlways)
				, TblClientTypeFormatterNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.GISLocation, FieldSelect.ShowNever)
				, TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.L.LocationReport.LocationID.F.OrderByRank, FieldSelect.ShowNever)
				, TblColumnNode.New(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.Comment, FieldSelect.ShowNever)
			)
		);
		#endregion
		#region TemporaryStorage
		public static Tbl TemporaryStorageReport = new Tbl(dsMB.Schema.T.TemporaryStorage,
			TId.TemporaryStorage,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true), RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.TemporaryStorage
				, TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.ContainingLocationID.F.Code, new MapSortCol(Statics.LocationSort), FieldSelect.ShowAlways)
				, WorkOrderColumns(dsMB.Path.T.TemporaryStorage.F.WorkOrderID, ShowXIDAndCurrentStateHistoryState)
				, TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.LocationID.F.Desc, FieldSelect.ShowAlways)
				, TblClientTypeFormatterNode.New(dsMB.Path.T.TemporaryStorage.F.LocationID.F.GISLocation, FieldSelect.ShowNever)
				, TblColumnNode.New(dsMB.Path.T.TemporaryStorage.F.LocationID.F.Comment, FieldSelect.ShowNever)
			).LayoutArray()
		);
		#endregion
		#endregion

		#region Other
		#region Contact
		public static Tbl ContactReport = new Tbl(dsMB.Schema.T.Contact,
			TId.Contact,
			new Tbl.IAttr[] {
				ContactsDependentGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			ColumnBuilder.New(dsMB.Path.T.Contact,
				  ContactColumns(dsMB.Path.T.Contact, ShowForContact)
				, UserColumnsForContact(dsMB.Path.T.Contact.F.Id.L.User.ContactID)
			).LayoutArray()
			// TODO: Show role(s) (Requestor, Employee, Billable Requestor, three Vendor contact types, any UnitRelatedContact relationship but do not enumerate the units)
			// Would be nice to be able to filter on this so you can see only Employees, but there is always the Employee report for that. Or is there (permissions?)
			// Because it is combinatorial grouping on it would not be useful.
			);
		#endregion
		#region Vendor
		public static Tbl VendorReport = new Tbl(dsMB.Schema.T.Vendor,
			TId.Vendor,
			new Tbl.IAttr[] {
				UnitValueAndServiceGroup,
				TblDrivenReportRtbl(RTblBase.SetAllowSummaryFormat(true), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			VendorColumns(dsMB.Path.T.Vendor, ShowForVendor).LayoutArray()
		);
		#endregion
		#region EmailRequest
		private static Tbl BuildEmailRequestReport(TId identification, bool singleRecord = false) {
			var rtblArgs = new List<RTblBase.ICtorArg>() {
				RTblBase.SetAllowSummaryFormat(false),
				RTblBase.SetDualLayoutDefault(defaultInColumns: true)
			};
			if (singleRecord) {
				rtblArgs.Add(RTblBase.SetNoUserFilteringAllowed());
				rtblArgs.Add(RTblBase.SetNoUserSortingAllowed());
				rtblArgs.Add(RTblBase.SetNoUserGroupingAllowed());
			}
			else
				rtblArgs.Add(RTblBase.SetGrouping(dsMB.Path.T.EmailRequest.F.RequestorEmailAddress));
			return new Tbl(dsMB.Schema.T.EmailRequest,
				identification,
				new Tbl.IAttr[] {
					MainBossServiceGroup,
					CommonTblAttrs.ViewCostsDefinedBySchema,
					TblDrivenReportRtbl(rtblArgs.ToArray())
				},
				ColumnBuilder.New(dsMB.Path.T.EmailRequest,
					  TblColumnNode.New(dsMB.Path.T.EmailRequest.F.RequestorEmailAddress, DefaultShowInDetailsCol.Show())
					, DateHHMMColumnNode(dsMB.Path.T.EmailRequest.F.ReceiveDate, DefaultShowInDetailsCol.Show())
					, RequestColumns(dsMB.Path.T.EmailRequest.F.RequestID, dsMB.Path.T.EmailRequest.F.RequestID.L.RequestExtras.RequestID, ShowXID)
					, TblColumnNode.New(dsMB.Path.T.EmailRequest.F.Comment)
					, TblColumnNode.New(dsMB.Path.T.EmailRequest.F.MailHeader)
					, TblColumnNode.New(dsMB.Path.T.EmailRequest.F.MailMessage)
				).LayoutArray()
			);
		}
		public static Tbl EmailRequestReport = BuildEmailRequestReport(TId.EmailRequest);
		public static Tbl SingleEmailRequestReport = BuildEmailRequestReport(TId.EmailRequest.ReportSingle, singleRecord: true);
		#endregion
		#region License
		public static Tbl LicenseReport = new Tbl(dsMB.Schema.T.License,
			TId.License,
			new Tbl.IAttr[] {
				LicenseGroup,
				TblDrivenReportRtbl(
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true)
				)
			},
			new TblLayoutNodeArray(
				  TblColumnNode.New(KB.K("License Module"), dsMB.Path.T.License.F.ApplicationID, Fmt.SetEnumText(Thinkage.MainBoss.Database.Licensing.LicenseModuleIdProvider), DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(KB.K("License Module ID"), dsMB.Path.T.License.F.ApplicationID, DefaultShowInDetailsCol.Hide())
				, TblColumnNode.New(dsMB.Path.T.License.F.ExpiryModel, Fmt.SetEnumText(Thinkage.Libraries.Licensing.License.ExpiryModelNameProvider), DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.License.F.Expiry, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.License.F.LicenseID, DefaultShowInDetailsCol.Hide())
				, TblColumnNode.New(dsMB.Path.T.License.F.LicenseCount, DefaultShowInDetailsCol.Show(), Fmt.SetFontStyle(System.Drawing.FontStyle.Bold))
				, TblColumnNode.New(dsMB.Path.T.License.F.LicenseModel, Fmt.SetEnumText(Thinkage.Libraries.Licensing.License.LicenseModelNameProvider), DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.License.F.License, Fmt.SetMonospace(true), DefaultShowInDetailsCol.Show())
			)
		);
		#endregion
		#region User
		// TODO: Should be expanded to include security roles associated with the User
		public static Tbl UserReport = new Tbl(dsMB.Schema.T.User,
			TId.User,
			new Tbl.IAttr[] {
				SecurityGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			UserColumn(dsMB.Path.T.User, ShowAll).LayoutArray()
		);
		#endregion
		#region Relationships
		public static Tbl RelationshipReport = new Tbl(dsMB.Schema.T.Relationship,
			TId.Relationship,
			new Tbl.IAttr[] {
				UnitsDependentGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			new TblLayoutNodeArray(
				  TblColumnNode.New(dsMB.Path.T.Relationship.F.Code, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.Relationship.F.Desc, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.Relationship.F.AType)
				, TblColumnNode.New(dsMB.Path.T.Relationship.F.BType)
				, TblColumnNode.New(dsMB.Path.T.Relationship.F.BAsRelatedToAPhrase, new FormatCol(e => Statics.FormatTextExpression("A {0} B", e)), DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.Relationship.F.AAsRelatedToBPhrase, new FormatCol(e => Statics.FormatTextExpression("B {0} A", e)), DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.Relationship.F.Comment)
			)
		);
		#endregion
		#region SecurityRole
		static ColumnBuilder SecurityRoleAndUserRoleReportColumns(dsMB.PathClass.PathToSecurityRoleAndUserRoleReportRow SecurityRoleAndUserRoleReport, FieldSelect effect = null) {
			return ColumnBuilder.New(SecurityRoleAndUserRoleReport)
				.ColumnSection(StandardReport.LVPRowsFmtId
					, CustomOrBuiltinRoleValue(SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.CustomRoleID.F.Code, dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID.F.RoleName, effect.ShowIfOneOf(ShowAll, ShowXID))
					, SecurityRoleTypeClassifier(SecurityRoleAndUserRoleReport, effect)
					, CustomOrBuiltinRoleValue(SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.CustomRoleID.F.Desc, dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID.F.RoleDesc, effect.ShowIfOneOf(ShowAll))
				)
				.ColumnSection(StandardReport.MultiLineRowsFmtId
					, CustomOrBuiltinRoleValue(SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.CustomRoleID.F.Comment, dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID.F.RoleComment, effect.ShowIfOneOf(ShowAll))
					, TblColumnNode.New(SecurityRoleAndUserRoleReport.F.RolePrincipalID.L.PrincipalExtraInformation.PrincipalID.F.PermissionsAsText, FieldSelect.ShowNever)
				);
		}
		public static TblQueryValueNode CustomOrBuiltinRoleValue(DBI_Path custom, DBI_Path builtin, params TblLayoutNode.ICtorArg[] args) {
			return TblQueryValueNode.New(builtin.Key(), TblQueryCalculation.New(
							(values =>
								values[0] != null
									? (string)Libraries.TypeInfo.StringTypeInfo.AsNativeType(values[0], typeof(string))
									: values[1] != null ? ((SimpleKey)Libraries.TypeInfo.TranslationKeyTypeInfo.AsNativeType(values[1], typeof(SimpleKey))).Translate()
										: null
							),
							custom.ReferencedColumn.EffectiveType,
							new TblQueryPath(custom),
							new TblQueryPath(builtin)
				)
				, args
			);
		}
		public static TblServerExprNode SecurityRoleTypeClassifier(dsMB.PathClass.PathToSecurityRoleAndUserRoleReportRow SecurityRoleAndUserRoleReport, FieldSelect effect = null) {
			var classifier = new Thinkage.MainBoss.Controls.TIReports.RecordTypeClassifierByLinkages(true,
				new Tuple<DBI_Path, Key>(SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID, KB.K("Built-in")),
				new Tuple<DBI_Path, Key>(SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.CustomRoleID, KB.K("Custom")));
			return TblServerExprNode.New(SameKeyContextAs.K(KB.K("Type"), SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID), classifier.RecordTypeExpression, Fmt.SetEnumText(classifier.EnumValueProvider), effect.ShowIfOneOf(ShowAll));
		}
		// Currently limited to just the Role Code/Desc/Comments for all records where UserID field is null
		public static Tbl RoleReport = new Tbl(dsMB.Schema.T.SecurityRoleAndUserRoleReport,
			// TODO: This report always groups on Role and by doing so tries to be a report on Roles, showing the member users as "child" records
			// However, the user can filter the child records, or there could be a Role with no member users, in which case the Role will just not
			// show up in this report (rather than showing up with no entries).
			// This should either be changed to have a driver view that returns records for all Roles, even ones with no members, and with no filtering on User,
			// or changed to a report that default-groups on Role but allows the user to change this, in which case it is a "Security Role Member" report.
			TId.SecurityRole,
			new Tbl.IAttr[] {
				SecurityGroup,
				new MinimumDBVersionTbl(new Version(1, 0, 10, 42)),
				new PrimaryRecordArg(dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.PathToReferencedRow),
				new RTbl(typeof(TblDrivenDynamicColumnsReport), typeof(ReportViewerControl),
					RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetPrimaryRecordHeaderNode(TIReports.CustomOrBuiltinRoleValue(dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.CustomRoleID.F.Code, dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID.F.RoleName)),
					RTblBase.SetParentChildInformation(KB.K("Show assigned Users"), RTblBase.ColumnsRowType.JoinWithChildren,
						dsMB.Path.T.SecurityRoleAndUserRoleReport.F.UserID,
						new TblLayoutNodeArray(
							TblColumnNode.New(dsMB.Path.T.SecurityRoleAndUserRoleReport.F.UserID.F.ContactID.F.Code, Fmt.SetFontStyle(System.Drawing.FontStyle.Bold)),
							TblColumnNode.New(dsMB.Path.T.SecurityRoleAndUserRoleReport.F.UserID.F.AuthenticationCredential)
						),
						mainRecordGroupings: new TblLeafNode[] {
							SecurityRoleTypeClassifier(dsMB.Path.T.SecurityRoleAndUserRoleReport),
							TIReports.CustomOrBuiltinRoleValue(dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.CustomRoleID.F.Code, dsMB.Path.T.SecurityRoleAndUserRoleReport.F.RolePrincipalID.F.RoleID.F.RoleName)
						},
						sortingPaths: new DBI_Path[] {
							dsMB.Path.T.SecurityRoleAndUserRoleReport.F.UserID.F.ContactID.F.Code
						}
					)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.SecurityRoleAndUserRoleReport,
					SecurityRoleAndUserRoleReportColumns(dsMB.Path.T.SecurityRoleAndUserRoleReport, ShowAll)
			).LayoutArray()
		);
		#endregion
		#region DatabaseHistory
		public static Tbl DatabaseHistoryReport = new Tbl(dsMB.Schema.T.DatabaseHistory,
			TId.DatabaseManagement,
			new Tbl.IAttr[] {
				AdminGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: false))
			},
			new TblLayoutNodeArray(
				  DateHHMMColumnNode(dsMB.Path.T.DatabaseHistory.F.EntryDate, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.DatabaseHistory.F.Subject, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.DatabaseHistory.F.Description, DefaultShowInDetailsCol.Show())
			)
		);
		#endregion
		#region BackupFileName
		public static Tbl BackupFileReport = new Tbl(dsMB.Schema.T.BackupFileName,
			TId.Backup,
			new Tbl.IAttr[] {
				AdminGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true))
			},
			new TblLayoutNodeArray(
				  DateHHMMColumnNode(dsMB.Path.T.BackupFileName.F.LastBackupDate, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.BackupFileName.F.FileName, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.BackupFileName.F.Comment)
				, TblColumnNode.New(dsMB.Path.T.BackupFileName.F.DatabaseVersion)
				, TblColumnNode.New(dsMB.Path.T.BackupFileName.F.Message)
			)
		);
		#endregion
		#region ExpenseCategory
		//TODO: The column width calculated for this expression will be based on SqlExpression.Constant(true) (SqlExpression.Select) resulting in only 5 characters (for the word "False") allowed for the output.
		static readonly SqlExpression ExpenseCategoryUsages =
			SqlExpression.Select(new SqlExpression(dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsItem), SqlExpression.Constant(KB.TOi(TId.Item).Translate() + Environment.NewLine), SqlExpression.Constant(""))
			.Plus(SqlExpression.Select(new SqlExpression(dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsLabor),
				SqlExpression.Constant(
					KB.TOi(TId.HourlyInside).Translate() + Environment.NewLine +
					KB.TOi(TId.PerJobInside).Translate() + Environment.NewLine +
					KB.TOi(TId.HourlyOutside).Translate() + Environment.NewLine +
					KB.TOi(TId.PerJobOutside).Translate() + Environment.NewLine), SqlExpression.Constant("")))
			.Plus(SqlExpression.Select(new SqlExpression(dsMB.Path.T.WorkOrderExpenseCategory.F.FilterAsMiscellaneous), SqlExpression.Constant(KB.TOi(TId.MiscellaneousCost).Translate() + Environment.NewLine), SqlExpression.Constant("")));

		public static Tbl ExpenseCategoryReport = new Tbl(dsMB.Schema.T.WorkOrderExpenseCategory,
			TId.ExpenseCategory,
			new Tbl.IAttr[] {
				ItemResourcesGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true), RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderExpenseCategory,
				CodeDescColumnBuilder.New(dsMB.Path.T.WorkOrderExpenseCategory, ShowXID)
				, TblServerExprNode.New(KB.K("Usable for"), ExpenseCategoryUsages, FieldSelect.ShowAlways)
			).LayoutArray()
		);
		#endregion
		#region ExpenseModels
		public static Tbl ExpenseModelReport = new Tbl(dsMB.Schema.T.WorkOrderExpenseModelEntry,
			// Changed to a report that default-groups on Model but allows the user to change this, in which case it is a "Work Order Expense Model Entry" report.
			TId.ExpenseModel,
			new Tbl.IAttr[] {
				AccountingGroup,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetGrouping(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.WorkOrderExpenseModelEntry
				, WorkOrderExpenseModelEntryColumns(dsMB.Path.T.WorkOrderExpenseModelEntry, ShowAll, showModel: true)
			).LayoutArray()
		);
		#endregion
		#region AccountingLedger
		private static SqlExpression NullIfNotPositive(SqlExpression value) {
			return SqlExpression.Select(value.Gt(SqlExpression.Constant(0)), value);
		}
		private static SqlExpression NormalCost() {
			return SqlExpression.Select(
				new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.IsReversed), new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.Cost).Negate(),
																								new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.Cost));
		}
		private static SqlExpression NegatedCost() {
			return SqlExpression.Select(
				new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.IsReversed), new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.Cost),
																								new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.Cost).Negate());
		}
		private static SqlExpression LedgerCCInformation(DBI_Path infoPath) {
			return SqlExpression.Select(
				new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.IsReversed), new SqlExpression(new DBI_Path(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.ToCostCenterID.PathToReferencedRow, infoPath)),
																								new SqlExpression(new DBI_Path(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.FromCostCenterID.PathToReferencedRow, infoPath)));
		}
		private static SqlExpression ContraCCInformation(DBI_Path infoPath) {
			return SqlExpression.Select(
				new SqlExpression(dsMB.Path.T.AccountingTransactionsAndReversals.F.IsReversed), new SqlExpression(new DBI_Path(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.FromCostCenterID.PathToReferencedRow, infoPath)),
																								new SqlExpression(new DBI_Path(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.ToCostCenterID.PathToReferencedRow, infoPath)));
		}
		private static readonly object DefaultSortNode = KB.I("DefaultSortNode");
		public static Tbl AccountingLedgerReport = new Tbl(dsMB.Schema.T.AccountingTransactionsAndReversals,
			// This is more or less in "Ledger" form, with one account (the "ledger" account) being somewhat implied, and the contra account described in detail.
			TId.AccountingLedger,
			new Tbl.IAttr[] {
				AccountingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false),
					RTblBase.SetDualLayoutDefault(defaultInColumns: true),
					RTblBase.SetPreferWidePage(true),
					RTblBase.SetGrouping(DefaultSortNode)
				)
			},
			ColumnBuilder.New(dsMB.Path.T.AccountingTransactionsAndReversals
				, DateHHMMColumnNode(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.EffectiveDate, DefaultShowInDetailsCol.Show())
				, DateHHMMColumnNode(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.EntryDate)
				, UserColumns(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.F.UserID)
				, TblColumnNode.New(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.TransactionType, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.IsCorrection)
				, TblColumnNode.New(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.FromReference, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.ReasonReference, DefaultShowInDetailsCol.Show())
				, TblColumnNode.New(dsMB.Path.T.AccountingTransactionsAndReversals.F.AccountingTransactionID.L.AccountingTransactionVariants.AccountingTransactionID.F.ToReference, DefaultShowInDetailsCol.Show())
				, TblServerExprNode.New(XKey.New(KB.K("Code"), KB.K("Ledger Cost Center")), LedgerCCInformation(dsMB.Path.T.CostCenter.F.Code), Fmt.SetId(DefaultSortNode), DefaultShowInDetailsCol.Show())
				, TblServerExprNode.New(XKey.New(KB.K("Code"), KB.TOi(TId.CostCenter)), ContraCCInformation(dsMB.Path.T.CostCenter.F.Code), DefaultShowInDetailsCol.Show())
				, TblServerExprNode.New(XKey.New(KB.K("General Ledger Account"), KB.TOi(TId.CostCenter)), ContraCCInformation(dsMB.Path.T.CostCenter.F.GeneralLedgerAccount))
				, TblServerExprNode.New(KB.K("Credit"), NullIfNotPositive(NormalCost()), FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
				, TblServerExprNode.New(KB.K("Debit"), NullIfNotPositive(NegatedCost()), FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#region AccountingTransaction
		// This is sort of in "Journal" form. Each transaction lists only once and gives a signed value transfer from one CC to another.
		// Technically a better "Journal" form would use 3 lines per record, with the second line be the "Credit" (from) line with C/C info and Credit amount, and the 3rd line being the "Debit" (To) line with C/C info and Debit amount,
		// with From and To chosen so the amounts would show positive.
		// For now, though, we can't represent multi-line columnar reports.
		public static Tbl AccountingTransactionReport = new Tbl(dsMB.Schema.T.AccountingTransaction,
			TId.AccountingTransaction,
			new Tbl.IAttr[] {
				AccountingGroup,
				CommonTblAttrs.ViewCostsDefinedBySchema,
				TblDrivenReportRtbl( RTblBase.SetAllowSummaryFormat(false), RTblBase.SetDualLayoutDefault(defaultInColumns: true), RTblBase.SetPreferWidePage(true))
			},
			ColumnBuilder.New(dsMB.Path.T.AccountingTransaction,
					  DateHHMMColumnNode(dsMB.Path.T.AccountingTransaction.F.EffectiveDate, DefaultShowInDetailsCol.Show())
					, DateHHMMColumnNode(dsMB.Path.T.AccountingTransaction.F.EntryDate)
					, UserColumns(dsMB.Path.T.AccountingTransaction.F.UserID)
					, TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.TransactionType, DefaultShowInDetailsCol.Show())
					, TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.IsCorrection)
					, TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.FromReference, DefaultShowInDetailsCol.Show())
					, TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.ReasonReference, DefaultShowInDetailsCol.Show())
					, TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Id.L.AccountingTransactionVariants.AccountingTransactionID.F.ToReference, DefaultShowInDetailsCol.Show())
					, CostCenterColumns(dsMB.Path.T.AccountingTransaction.F.FromCostCenterID, ShowXID)
					, CostCenterColumns(dsMB.Path.T.AccountingTransaction.F.ToCostCenterID, ShowXID)
					, TblColumnNode.New(dsMB.Path.T.AccountingTransaction.F.Cost, FooterAggregateCol.Sum(), DefaultShowInDetailsCol.Show())
			).LayoutArray()
		);
		#endregion
		#endregion
	}
}
