using Thinkage.Libraries;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	public class TIRelationship : TIGeneralMB3
	{
		#region Record-type providers
		#region - RoleTypeProviders
		private static object[] FromRelationshipRoleTypeValues = new object[] {
			(int)DatabaseEnums.RelationshipRoleType.Unit
		};
		private static Key[] FromRelationshipRoleTypeLabels = new Key[] {
			KB.TOi(TId.Unit)
		};
		public static EnumValueTextRepresentations FromRoleTypeProvider = new EnumValueTextRepresentations(FromRelationshipRoleTypeLabels, null, FromRelationshipRoleTypeValues);
		private static object[] ToRelationshipRoleTypeValues = new object[] {
			(int)DatabaseEnums.RelationshipRoleType.Unit,
			(int)DatabaseEnums.RelationshipRoleType.Contact
		};
		private static Key[] ToRelationshipRoleTypeLabels = new Key[] {
			KB.TOi(TId.Unit),
			KB.K("Contact")
		};
		public static EnumValueTextRepresentations ToRoleTypeProvider = new EnumValueTextRepresentations(ToRelationshipRoleTypeLabels, null, ToRelationshipRoleTypeValues);
		#endregion
		#endregion
		#region NodeIds
		#endregion

		#region Named Tbls
		public static DelayedCreateTbl UnitRelatedRecordsBrowseTbl;
		public static DelayedCreateTbl ContactRelatedRecordsBrowseTbl;
		private static DelayedCreateTbl ReverseUnitRelatedUnitPanelTbl;
		private static DelayedCreateTbl ReverseUnitRelatedContactPanelTbl;
		private static DelayedCreateTbl UnitUnitRelationshipEditTbl;
		private static DelayedCreateTbl UnitContactRelationshipEditTbl;
		private static DelayedCreateTbl RelationshipWithContactBrowseTblCreator;
		#endregion
		#region Constructors and Property Initializers
		private TIRelationship() {
		}
		static TIRelationship() {
		}
		#endregion

		internal static void DefineTblEntries()
		{
			#region Relationship
			DefineBrowseTbl(dsMB.Schema.T.Relationship, delegate()
			{
				return new CompositeTbl(dsMB.Schema.T.Relationship, TId.Relationship,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Relationship.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Relationship.F.Desc),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.RelationshipReport))
						)
					},
					null,
					new CompositeView(UnitUnitRelationshipEditTbl, dsMB.Path.T.Relationship.F.Id,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.Relationship.F.AType).Eq((int)DatabaseEnums.RelationshipRoleType.Unit)
																.And(new SqlExpression(dsMB.Path.T.Relationship.F.BType).Eq((int)DatabaseEnums.RelationshipRoleType.Unit)))
					),
					new CompositeView(UnitContactRelationshipEditTbl, dsMB.Path.T.Relationship.F.Id,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.Relationship.F.AType).Eq((int)DatabaseEnums.RelationshipRoleType.Unit)
																.And(new SqlExpression(dsMB.Path.T.Relationship.F.BType).Eq((int)DatabaseEnums.RelationshipRoleType.Contact)))
					)
				);
			});
			// The following is a browser only on relationships that involve Contacts.
			RelationshipWithContactBrowseTblCreator = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.Relationship, TId.Relationship,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Relationship.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Relationship.F.Desc),
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.Relationship.F.AType).Eq((int)DatabaseEnums.RelationshipRoleType.Unit)
																.And(new SqlExpression(dsMB.Path.T.Relationship.F.BType).Eq((int)DatabaseEnums.RelationshipRoleType.Contact))),
							BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.RelationshipReport))
						)
					},
					null,
					CompositeView.ChangeEditTbl(UnitContactRelationshipEditTbl)
				);
			});

			UnitUnitRelationshipEditTbl = new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.Relationship, TId.UnitUnitRelationship,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.Relationship.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.BAsRelatedToAPhrase, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.AAsRelatedToBPhrase, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.Comment, DCol.Normal, ECol.Normal)
						),
						BrowsetteTabNode.New(TId.UnitRelatedUnit, TId.UnitUnitRelationship, TblColumnNode.NewBrowsette(dsMB.Path.T.UnitRelatedUnit.F.RelationshipID, DCol.Normal, ECol.Normal))
					),
					Init.OnLoadNewClone(new PathTarget(dsMB.Path.T.Relationship.F.AType), new ConstantValue((int)DatabaseEnums.RelationshipRoleType.Unit)),
					Init.OnLoadNewClone(new PathTarget(dsMB.Path.T.Relationship.F.BType), new ConstantValue((int)DatabaseEnums.RelationshipRoleType.Unit))
				);
			});
			UnitContactRelationshipEditTbl = new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.Relationship, TId.UnitContactRelationship,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.Relationship.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.BAsRelatedToAPhrase, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.AAsRelatedToBPhrase, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.Relationship.F.Comment, DCol.Normal, ECol.Normal)
						),
						BrowsetteTabNode.New(TId.UnitRelatedContact, TId.UnitContactRelationship, TblColumnNode.NewBrowsette(dsMB.Path.T.UnitRelatedContact.F.RelationshipID, DCol.Normal, ECol.Normal))
					),
					Init.OnLoadNewClone(new PathTarget(dsMB.Path.T.Relationship.F.AType), new ConstantValue((int)DatabaseEnums.RelationshipRoleType.Unit)),
					Init.OnLoadNewClone(new PathTarget(dsMB.Path.T.Relationship.F.BType), new ConstantValue((int)DatabaseEnums.RelationshipRoleType.Contact))
				);
			});
			RegisterForImportExport(TId.Relationship, new DelayedCreateTbl(delegate()
				{
					return new Tbl(dsMB.Schema.T.Relationship, TId.Relationship,
						new Tbl.IAttr[] {
						UnitsDependentGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
						new TblLayoutNodeArray(
							DetailsTabNode.New(
								TblColumnNode.New(dsMB.Path.T.Relationship.F.Code, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Relationship.F.Desc, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Relationship.F.AType, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Relationship.F.BType, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Relationship.F.BAsRelatedToAPhrase, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Relationship.F.AAsRelatedToBPhrase, ECol.Normal),
								TblColumnNode.New(dsMB.Path.T.Relationship.F.Comment, ECol.Normal)
							)
						)
					);
				}
			));
			#endregion
			#region UnitRelatedRecords
			// TODO: In all 4 panels for related records, the 'this' object is shown in a short form just before the appropriate relationship phrase. For some reason the filter-based
			// elision is not removing this field which is good because we actually want to keep it even though it is a constant for any given browsette container context. If it vanishes
			// someday because we figure out why the elision was not working, we may need to add an attribute to prevent the elision.

			// The "forward" and "reverse" ones differ only insofar as the Unit order is reversed and the opposite phrase chosen, and details only included for the second ("other") record.
			// Also the reverse has no ETbl/ECol since it is only used for a panel.
			
			// The single edit tbl can be used to make related records with the initialized unit from the calling browsette as the A or B unit. We use an Init to copy the value to both
			// controls, and since the field are not captive, the user can change either Unit picker to some other unit. This does, however, let them change both units to create a record
			// that will not show up in the browsette they came from.
			DefineTbl(dsMB.Schema.T.UnitRelatedUnit, delegate()
			{
				return new Tbl(dsMB.Schema.T.UnitRelatedUnit, TId.UnitRelatedUnit,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.Code)
						),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault)),
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.RelationshipID,
								new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Relationship.F.Code)),
								new ECol(ECol.ReadonlyInUpdateAccess, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.Relationship.F.AType, (int)DatabaseEnums.RelationshipRoleType.Unit)), Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.Relationship.F.BType, (int)DatabaseEnums.RelationshipRoleType.Unit)))
							),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID, ECol.ReadonlyInUpdate),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.Code, DCol.Normal),
							TblColumnNode.New(null, dsMB.Path.T.UnitRelatedUnit.F.RelationshipID.F.AAsRelatedToBPhrase, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID, ECol.ReadonlyInUpdate),
							TblGroupNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.ContainingLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.ExternalTag, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.LocationID.F.GISLocation, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.Make, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.Model, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.Serial, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.UnitUsageID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitUsage.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.UnitCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitCategory.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.SystemCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.SystemCode.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.Drawing, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.RelativeLocationID.F.UnitID.F.RelativeLocationID.F.LocationID.F.Comment, DCol.Normal)
							)
						)
					),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID), new EditorPathValue(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID))
				);
			});
			RegisterExistingForImportExport(TId.UnitRelatedUnit, dsMB.Schema.T.UnitRelatedUnit);
			ReverseUnitRelatedUnitPanelTbl = new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.UnitRelatedUnit, TId.UnitRelatedUnit,
					new Tbl.IAttr[] {
						UnitsDependentGroup, new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.Delete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.RelationshipID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Relationship.F.Code))),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code))),
							TblColumnNode.New(null, dsMB.Path.T.UnitRelatedUnit.F.RelationshipID.F.BAsRelatedToAPhrase, DCol.Normal),
							TblGroupNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.ContainingLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.ExternalTag, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.LocationID.F.GISLocation, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.Make, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.Model, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.Serial, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.UnitUsageID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitUsage.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.UnitCategoryID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UnitCategory.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.SystemCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.SystemCode.F.Code))),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.Drawing, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.RelativeLocationID.F.UnitID.F.RelativeLocationID.F.LocationID.F.Comment, DCol.Normal)
							)
						)
					)
				);
			});

			UnitRelatedRecordsBrowseTbl = new DelayedCreateTbl(
				delegate() {
					object relationshipColumnId = KB.I("RelationshipId");
					return new CompositeTbl(dsMB.Schema.T.UnitRelatedRecords, TId.UnitRelation,
						new Tbl.IAttr[] {
							UnitsDependentGroup,
							new BTbl(
								BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Relationship"), relationshipColumnId),
								BTbl.SetTreeStructure(dsMB.Path.T.UnitRelatedRecords.F.ParentID, 2, 2, dsMB.Schema.T.UnitRelatedRecordsContainment)
							)
						},
						null,
						// When identifying the various Relationship record types, we do monimal checks on AType and BType, relying on the query not sending us
						// any bogus relationships (so for instance we will not get any reverse Unit/Contact relationships)
						// Unit/Unit Relationship. Note that these records will always be secondary because this tbl is always UnitLocationID-filtered (but BrowseLogic doesn't realize it)
						new CompositeView(UnitUnitRelationshipEditTbl, dsMB.Path.T.UnitRelatedRecords.F.RelationshipID,
							CompositeView.RecognizeByValidEditLinkage(),
							PanelOnly,
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.Relationship.F.AAsRelatedToBPhrase),
							// Because these are not Reversed we don't have to check that AType ('this' type) is Unit, but we do have to check that BType ('other' type) is Unit
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitRelatedRecords.F.RelationshipID.F.BType).Eq((int)DatabaseEnums.RelationshipRoleType.Unit).And(new SqlExpression(dsMB.Path.T.UnitRelatedRecords.F.Reverse).Not()))
						),
						// Reversed Unit/Unit relationship. Note that these records will always be secondary because this tbl is always UnitLocationID-filtered (but BrowseLogic doesn't realize it)
						new CompositeView(UnitUnitRelationshipEditTbl, dsMB.Path.T.UnitRelatedRecords.F.RelationshipID,
							CompositeView.RecognizeByValidEditLinkage(),
							PanelOnly,
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.Relationship.F.BAsRelatedToAPhrase),
							// Because these are Reversed we don't have to check that BType ('this' type) is Unit, and the only AType ('other' type) we support is Unit so we don't check that either.
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitRelatedRecords.F.Reverse))
						),
						// Unit/Contact Relationship. Note that these records will always be secondary because this tbl is always UnitLocationID-filtered (but BrowseLogic doesn't realize it)
						new CompositeView(UnitContactRelationshipEditTbl, dsMB.Path.T.UnitRelatedRecords.F.RelationshipID,
							CompositeView.RecognizeByValidEditLinkage(),
							PanelOnly,
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.Relationship.F.AAsRelatedToBPhrase),
							// Checking the BType for COntact ensures that the record is not Reverse and also that AType is Unit (the only AType allowed when BType is Contact)
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitRelatedRecords.F.RelationshipID.F.BType).Eq((int)DatabaseEnums.RelationshipRoleType.Contact))
						),
						// Unit to Unit related records
						new CompositeView(dsMB.Path.T.UnitRelatedRecords.F.UnitRelatedUnitID,
							CompositeView.RecognizeByValidEditLinkage(),
							NoViewEdit,				// Because the reverse related records cannot call editor, make this consistent.
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.UnitRelatedRecords.F.ThisUnitLocationID, dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitRelatedRecords.F.Reverse).Not())
						),
						// Unit to Unit reverse related records
						new CompositeView(ReverseUnitRelatedUnitPanelTbl, dsMB.Path.T.UnitRelatedRecords.F.UnitRelatedUnitID,
							CompositeView.RecognizeByValidEditLinkage(),
							PanelOnlyWithDelete,	// We can't allow calling an editor because the edit tbl has no ECols. These can be created using the previous view. TODO: Want to specify a different edit Tbl for editing and for panel layout
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.UnitRelatedUnit.F.AUnitLocationID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.UnitRelatedRecords.F.ThisUnitLocationID, dsMB.Path.T.UnitRelatedUnit.F.BUnitLocationID),
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitRelatedRecords.F.Reverse))
						),
						// Unit to Contact related records
						new CompositeView(dsMB.Path.T.UnitRelatedRecords.F.UnitRelatedContactID,
							CompositeView.RecognizeByValidEditLinkage(),
							NoViewEdit,				// Because the reverse related records cannot call editor, make this consistent.
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.UnitRelatedContact.F.ContactID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.UnitRelatedRecords.F.ThisUnitLocationID, dsMB.Path.T.UnitRelatedContact.F.UnitLocationID)
							// If these are UnitRelatedContact records they can't be Reverse.
						)
					);
				}
			);
			#endregion

			#region ContactRelatedRecords
			// The "forward" and "reverse" ones differ only insofar as the Unit and Contact order is reversed and the opposite phrase chosen, and details only included for the second ("other") record.
			// Also the reverse has no ETbl/ECol since it is only used for a panel.
			DefineTbl(dsMB.Schema.T.UnitRelatedContact, delegate() {
				return new Tbl(dsMB.Schema.T.UnitRelatedContact, TId.UnitRelatedContact,
					new Tbl.IAttr[] {
						UnitsDependentGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.Code)
						),
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault)),
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.RelationshipID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Relationship.F.Code)),
								new ECol(ECol.NormalAccess, Fmt.SetPickFrom(RelationshipWithContactBrowseTblCreator))
							),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
							TblColumnNode.New(null, dsMB.Path.T.UnitRelatedContact.F.RelationshipID.F.AAsRelatedToBPhrase, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID, ECol.Normal),
							TblGroupNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.BusinessPhone, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.Email, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.HomePhone, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.PagerPhone, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.MobilePhone, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.FaxPhone, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.WebURL, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.LDAPGuid, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.LocationID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.LocationID.F.Desc, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.Comment, DCol.Normal)
							)
						)
					)
				);
			});
			RegisterExistingForImportExport(TId.UnitRelatedContact, dsMB.Schema.T.UnitRelatedContact);

			ReverseUnitRelatedContactPanelTbl = new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.UnitRelatedContact, TId.UnitRelatedContact,
					new Tbl.IAttr[] {
						UnitsDependentGroup, new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.Delete))
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.RelationshipID.F.Code, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.ContactID.F.Code, DCol.Normal),
							TblColumnNode.New(null, dsMB.Path.T.UnitRelatedContact.F.RelationshipID.F.BAsRelatedToAPhrase, DCol.Normal),
							TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID),
							TblGroupNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID, new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.ContainingLocationID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.ExternalTag, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.LocationID.F.GISLocation, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Make, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Model, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Serial, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.UnitUsageID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.UnitCategoryID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.SystemCodeID.F.Code, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Drawing, DCol.Normal),
								TblColumnNode.New(dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.RelativeLocationID.F.LocationID.F.Comment, DCol.Normal)
							)
						)
					)
				);
			});

			ContactRelatedRecordsBrowseTbl = new DelayedCreateTbl(
				delegate() {
					object relationshipColumnId = KB.I("RelationshipId");
					return new CompositeTbl(dsMB.Schema.T.ContactRelatedRecords, TId.ContactRelation,
						new Tbl.IAttr[] {
							UnitsDependentGroup,
							new BTbl(
								BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Relationship"), relationshipColumnId),
								BTbl.SetTreeStructure(dsMB.Path.T.ContactRelatedRecords.F.ParentID, 2, 2, dsMB.Schema.T.ContactRelatedRecordsContainment)
							)
						},
						null,
						// The BAsRelatedToA relationship role. Note that these records will always be secondary because this tbl is always ContactID-filtered (but BrowseLogic doesn't realize it)
						new CompositeView(UnitContactRelationshipEditTbl, dsMB.Path.T.ContactRelatedRecords.F.RelationshipID,
							CompositeView.RecognizeByValidEditLinkage(),
							PanelOnly,
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.Relationship.F.BAsRelatedToAPhrase)
						),
						// The Unit to Contact BAsRelatedToA relationship
						new CompositeView(ReverseUnitRelatedContactPanelTbl, dsMB.Path.T.ContactRelatedRecords.F.UnitRelatedContactID,
							CompositeView.RecognizeByValidEditLinkage(),
							PanelOnlyWithDelete,	// We can't allow calling an editor because the edit tbl has no ECols. TODO: Want to specify a different edit Tbl for editing and for panel layout
							BTbl.PerViewColumnValue(relationshipColumnId, dsMB.Path.T.UnitRelatedContact.F.UnitLocationID.F.Code),
							CompositeView.PathAlias(dsMB.Path.T.ContactRelatedRecords.F.ThisContactID, dsMB.Path.T.UnitRelatedContact.F.ContactID)
						),
						// Because ReverseUnitRelatedContactTbl is not editable, we have to find the normal UnitRelatedContact editor and use it to create new related records.
						CompositeView.ExtraNewVerb(TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.UnitRelatedContact),
							// This path alias ensures that a browsette filter on ThisRecordID turns into an Init in the called editor.
							CompositeView.PathAlias(dsMB.Path.T.ContactRelatedRecords.F.ThisContactID, dsMB.Path.T.UnitRelatedContact.F.ContactID)
						)
					);
				}
			);
			#endregion

		}
	}
}