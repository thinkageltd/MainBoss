using System;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Location Browsers and pickers
	/// </summary>
	public class TILocations : TIGeneralMB3
	{
		#region Record-type providers
		#region - AllLocationProvider
		private static object[] AllLocationValues = new object[] {
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PostalAddress,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryStorage,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.Unit,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentStorage,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PlainRelativeLocation,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocation,
			(int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocationTemplate
		};
		private static Key[] AllLocationLabels = new Key[] {
			KB.TOi(TId.PostalAddress),
			KB.TOi(TId.TemporaryStorage),
			KB.TOi(TId.Unit),
			KB.TOi(TId.Storeroom),
			KB.TOi(TId.SubLocation),
			KB.TOi(TId.StoreroomAssignment),
			KB.TOi(TId.TemporaryStorageAssignment),
			KB.K("Task Temporary Storage Assignment")
		};
		public static EnumValueTextRepresentations AllLocationsProvider = new EnumValueTextRepresentations(AllLocationLabels, null, AllLocationValues);
		#endregion
		#endregion
		#region Helpers for creating Location and ItemLocation composite Tbl's
		/// <summary>
		/// Control over type of view to create for LACComposite Tbl construction
		/// </summary>
		internal enum ViewUse {
			/// <summary>
			/// Make the Composite View fully editable, deletable, creatable, and pickable
			/// </summary>
			Full,
			/// <summary>
			/// Make the Composite View fully editable, deletable, pickable, but not createable
			/// </summary>
			FullOnlyExisting,
			/// <summary>
			/// Make the composite view fully editable, creatable, and pickable, but only allow "active" records (whatever that means in the context)
			/// </summary>
			ActiveOnly,
			/// <summary>
			/// Make the Composite View secondary only for records which should only appear as supporting tree structure. The record type will be filtered out of the Primary set,
			/// and any such records left will be non-editable, non-pickable, and new ones cannot be created.
			/// </summary>
			Secondary,
			/// <summary>
			/// Do not include this record type in the Composite Views. This should only be used if the record type is not a (possibly indirect) container type for any of
			/// the other views marked ViewUse.Full.
			/// </summary>
			None
		};
		/// <summary>
		/// Generate the composite view for a specific Location derivation for use in a LocationAndContainers composite tbl. Note that the distinction between Full and
		/// ActiveOnly is done by our caller's choice of editTblCreator.
		/// </summary>
		/// <param name="derivedTable"></param>
		/// <param name="use"></param>
		/// <param name="attrsForFullUse"></param>
		/// <returns></returns>
		private static CompositeView LACLocationView(DBI_Table derivedTable, DelayedCreateTbl editTblCreator, CompositeView.ICtorArg codeColumnDefinition, ViewUse use, params CompositeView.ICtorArg[] attrsForFullUse) {
			switch (use) {
				case ViewUse.None:
					return null;

				case ViewUse.Secondary:
					attrsForFullUse = new CompositeView.ICtorArg[] {
						ReadonlyView,
						CompositeView.ForceNotPrimary()
					};
					break;
				case ViewUse.FullOnlyExisting:
					attrsForFullUse = new CompositeView.ICtorArg[] {
						CompositeView.EditorAccess(false, EdtMode.New, EdtMode.Clone, EdtMode.EditDefault, EdtMode.ViewDefault)
					};
					break;
			}

			List<CompositeView.ICtorArg> overallAttrs = new List<CompositeView.ICtorArg>(attrsForFullUse);
			overallAttrs.Add(codeColumnDefinition);
			return new CompositeView(
				// TODO: DBI_Path has a reorient from related table that changes the *start* of a path but no method to modify the *end* of a path to point to a related table.
				// This would likely be called RetargetToRelatedTable. Even at that DBI_Path's Reorient etc doesn't like using paths that might be null (including derived-table linkages)
				editTblCreator == null ? TIGeneralMB3.FindDelayedEditTbl(derivedTable) : editTblCreator,
				new DBI_PathToRow(dsMB.Path.T.LocationDerivations.F.LocationID.PathToReferencedRow, dsMB.Schema.T.Location.PathToVariantIndirectDerived(derivedTable)).PathToReferencedRowId,
				overallAttrs.ToArray());
		}
		#region Individual CompositeViews for the ItemLocationAndContainers tbls
		// TODO: It may be possible to collapse these into a single method ILACItemLocationView similar to LACLocationView fold the remaining differences into the (only) call point.
		#region Table 7 (TemplateItemLocation)
		private static CompositeView ILACTemporaryTaskItemLocation(params CompositeView.ICtorArg[] otherParams)
		{
			List<CompositeView.ICtorArg> viewArgs = new List<CompositeView.ICtorArg>();
			viewArgs.AddRange(new CompositeView.ICtorArg[] {
				CompositeView.PathAlias(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID),
				CompositeView.PathAlias(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID),
				// If the current selection happens to be a temporary storage location, helpfully pick it in the new ItemLocation editor.
				CompositeView.ContextualInit(new int[] {
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.Unit,
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentStorage,
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PostalAddress,
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PlainRelativeLocation
					},
					dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID),
				// If the current selection happens to be an ItemLocation in temporary storage, helpfully pick the same Item in the new ItemLocation editor.
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocationTemplate, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.TemplateItemLocationID.F.ItemLocationID.F.ItemID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocationTemplate, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.TemplateItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID)
			});
			if (otherParams != null)
				viewArgs.AddRange(otherParams);
			return new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.TemplateItemLocationID,
						viewArgs.ToArray());
		}
		#endregion
		#endregion
		#region Overall CompositeTbls
		internal static CompositeTbl LACComposite(Tbl.TblIdentification tableName, DBI_Table permission, IPTbl pTbl,
			ViewUse postal, ViewUse temporaryStorage, ViewUse unit, ViewUse permanentStorage, ViewUse plainRelativeLocation, ViewUse templateTemporaryStorage, params BTbl.ICtorArg[] btblArgs) {
			object localCodeColumnId = KB.I("Local Code Id");
			List<BTbl.ICtorArg> btblAttrs = new List<BTbl.ICtorArg>(btblArgs);
			btblAttrs.Add(BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Code"), localCodeColumnId, NonPerViewColumn));
			btblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.LocationDerivations.F.LocationID.F.Code, BTbl.ListColumnArg.Contexts.ClosedCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter));
			btblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.LocationDerivations.F.LocationID.F.Desc, NonPerViewColumn));

			List<Tbl.IAttr> attrs = new List<Tbl.IAttr>(new Tbl.IAttr[]
			{
				new BTbl(btblAttrs.ToArray()),
				new FilteredTreeStructuredTbl(dsMB.Path.T.LocationDerivations.F.ContainingLocationID, dsMB.Schema.T.LocationAndContainers, 4, uint.MaxValue)
			});
			if( permission != null )
				attrs.Add(new UseNamedTableSchemaPermissionTbl(permission));
			if (pTbl != null)
				attrs.Add(pTbl);

			var viewOnMapKey = KB.K("Show on map");
			CompositeView.CreateVerbCommandDelegate viewOnMapDelegate =
				delegate(BrowseLogic browserLogic, int viewIndex) {
					return new ShowOnMapCommand(browserLogic, viewIndex);
				};
			List<CompositeView> viewList = new List<CompositeView>();
			viewList.AddRange( new CompositeView[] {
				// Table #0 (PostalAddress)
				LACLocationView(dsMB.Schema.T.PostalAddress, null, BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PostalAddress.F.Code), postal, new CompositeView.ICtorArg[] {
						CompositeView.AdditionalVerb(viewOnMapKey, viewOnMapDelegate)
					}),
				// Table #1 (TemporaryStorage)
				LACLocationView(dsMB.Schema.T.TemporaryStorage, temporaryStorage == ViewUse.ActiveOnly ? TIItem.ActiveTemporaryStorageTblCreator : TIItem.AllTemporaryStorageTblCreator, BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.TemporaryStorage.F.WorkOrderID.F.Number), temporaryStorage, new CompositeView.ICtorArg[] {
						CompositeView.ContextFreeInit(dsMB.Path.T.LocationDerivations.F.LocationID, dsMB.Path.T.TemporaryStorage.F.ContainingLocationID)
					}),
				// Table #2 (Unit)
				LACLocationView(dsMB.Schema.T.Unit, null, BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.Unit.F.RelativeLocationID.F.Code), unit, new CompositeView.ICtorArg[] {
						CompositeView.AdditionalVerb(viewOnMapKey, viewOnMapDelegate),
						CompositeView.ContextualInit(
							new int[] {
								(int)ViewRecordTypes.LocationDerivations.Unit,
								(int)ViewRecordTypes.LocationDerivations.PostalAddress,
								(int)ViewRecordTypes.LocationDerivations.PermanentStorage,
								(int)ViewRecordTypes.LocationDerivations.PlainRelativeLocation
							},
							new CompositeView.Init(new PathTarget(dsMB.Path.T.Unit.F.RelativeLocationID.F.ContainingLocationID), dsMB.Path.T.LocationDerivations.F.LocationID)
						)
					}),
				// Table #3 (PermanentStorage)
				LACLocationView(dsMB.Schema.T.PermanentStorage, null, BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code), permanentStorage, new CompositeView.ICtorArg[] {
						CompositeView.AdditionalVerb(viewOnMapKey, viewOnMapDelegate),
						CompositeView.ContextualInit(
							new int[] {
								(int)ViewRecordTypes.LocationDerivations.PostalAddress,
								(int)ViewRecordTypes.LocationDerivations.PermanentStorage,
								(int)ViewRecordTypes.LocationDerivations.PlainRelativeLocation
							},
							new CompositeView.Init(new PathTarget(dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.ContainingLocationID), dsMB.Path.T.LocationDerivations.F.LocationID)
						)
					}),
				// Table #4 (PlainRelativeLocation)
				LACLocationView(dsMB.Schema.T.PlainRelativeLocation, null, BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code), plainRelativeLocation, new CompositeView.ICtorArg[] {
						CompositeView.AdditionalVerb(viewOnMapKey, viewOnMapDelegate),
						CompositeView.ContextualInit(
							new int[] {
								(int)ViewRecordTypes.LocationDerivations.PostalAddress,
								(int)ViewRecordTypes.LocationDerivations.PlainRelativeLocation
							},
							new CompositeView.Init(new PathTarget(dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.ContainingLocationID), dsMB.Path.T.LocationDerivations.F.LocationID)
						)
					}),
				// Table #5 (TemplateStorageLocation)
				LACLocationView(dsMB.Schema.T.TemplateTemporaryStorage, null, BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.TemplateTemporaryStorage.F.WorkOrderTemplateID.F.Code), templateTemporaryStorage, new CompositeView.ICtorArg[] {
						CompositeView.ContextualInit(
							new int[] {
								(int)ViewRecordTypes.LocationDerivations.PostalAddress,
								(int)ViewRecordTypes.LocationDerivations.PlainRelativeLocation,
								(int)ViewRecordTypes.LocationDerivations.PermanentStorage,
								(int)ViewRecordTypes.LocationDerivations.Unit
							},
							new CompositeView.Init(new PathTarget(dsMB.Path.T.TemplateTemporaryStorage.F.ContainingLocationID), dsMB.Path.T.LocationDerivations.F.LocationID)
						)
					})
				}
			);
			// Now add EditDefault options depending on the type of Location
			if (tableName == TId.Unit)
				viewList.Add(CompositeView.AdditionalEditDefault(TIUnit.AttachmentTblCreator));

			return new CompositeTbl(dsMB.Schema.T.LocationDerivations,
				tableName,
				attrs.ToArray(),
				dsMB.Path.T.LocationDerivations.F.TableEnum,
				viewList.ToArray()
			);
		}
		private delegate List<CompositeView.ICtorArg> InitILACViewArgsT(bool isPrimary, DBI_Table editTableSchema, CompositeView.ICtorArg pathAlias, params CompositeView.ICtorArg[] contextualInits);
		private static CompositeTbl ILACComposite(Tbl.TblIdentification tableName, DBI_Table permission, IPTbl pTbl, bool groupNewCommands, bool onlyActiveTemporaryItemLocations, bool permanentIL, bool temporaryIL, bool templateIL) {
			Key newItemLocationButtonGroupKey = KB.K("New Location Assignment");
			object localCodeColumnId = KB.I("Local Code Id");
			object descriptionColumnId = KB.I("DescriptionId");
			List<Tbl.IAttr> attrs = new List<Tbl.IAttr>(new Tbl.IAttr[]	{
					new BTbl(
						BTbl.PerViewListColumn(dsMB.LabelKeyBuilder.K("Code"), localCodeColumnId, NonPerViewColumn),
						// The following Closed form will have no value to display for (non-Item) Location record types but none of these are pickable anyway.
						BTbl.ListColumn(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.Code, BTbl.ListColumnArg.Contexts.ClosedCombo|BTbl.ListColumnArg.Contexts.SearchAndFilter),
						BTbl.ListColumn(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.Shortage, BTbl.ListColumnArg.Contexts.SearchAndFilter),
						BTbl.PerViewListColumn(CommonDescColumnKey, descriptionColumnId, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.Available, NonPerViewColumn, new BTbl.ListColumnArg.FeatureGroupArg(StoreroomGroup))
					),
					new FilteredTreeStructuredTbl(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ContainingLocationID, dsMB.Schema.T.ItemLocationAndContainers, 5, uint.MaxValue)
				});
			if (permission != null)
				attrs.Add(new UseNamedTableSchemaPermissionTbl(permission));
			if (pTbl != null)
				attrs.Add(pTbl);

			var views = new List<CompositeView>();
			List<CompositeView.ICtorArg> viewArgs;

			// Table #0 (PostalAddress)
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID.F.PostalAddressID,
				ReadonlyView,
				BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PostalAddress.F.Code),
				BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.PostalAddress.F.LocationID.F.Desc),
				CompositeView.ForceNotPrimary()));
			// Table #1 (TemporaryStorage)
			views.Add(new CompositeView(TIItem.AllTemporaryStorageTblCreator, dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID.F.TemporaryStorageID,
				BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.TemporaryStorage.F.WorkOrderID.F.Number),
				BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.TemporaryStorage.F.LocationID.F.Desc),
				ReadonlyView,
				CompositeView.ForceNotPrimary()));
			// Table #2 (Unit)
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID.F.RelativeLocationID.F.UnitID,
				BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.Unit.F.RelativeLocationID.F.Code),
				BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.Unit.F.RelativeLocationID.F.LocationID.F.Desc),
				ReadonlyView,
				CompositeView.ForceNotPrimary()));
			// Table #3 (PermanentStorage)
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID.F.RelativeLocationID.F.PermanentStorageID,
				BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.Code),
				BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.PermanentStorage.F.RelativeLocationID.F.LocationID.F.Desc),
				ReadonlyView,
				CompositeView.ForceNotPrimary()));
			// Table #4 (PlainRelativeLocation)
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID.F.RelativeLocationID.F.PlainRelativeLocationID,
				BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.Code),
				BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.PlainRelativeLocation.F.RelativeLocationID.F.LocationID.F.Desc),
				ReadonlyView,
				CompositeView.ForceNotPrimary()));
			// Table #5 (TemplateTemporaryStorage)
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID.F.TemplateTemporaryStorageID,
				BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.TemplateTemporaryStorage.F.WorkOrderTemplateID.F.Code),
				BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.TemplateTemporaryStorage.F.LocationID.F.Desc),
				ReadonlyView,
				CompositeView.ForceNotPrimary()));

			// This is as close to a nested function as I can get. The only thing is that the delegate type must be declared with a name and outside the function.
			InitILACViewArgsT InitILACViewArgs = delegate(bool isPrimary, DBI_Table editTableSchema, CompositeView.ICtorArg pathAlias, CompositeView.ICtorArg[] contextualInits) {
				var result = new List<CompositeView.ICtorArg>();
				result.Add(BTbl.PerViewColumnValue(localCodeColumnId, dsMB.Path.T.ItemLocation.F.ItemID.F.Code.ReOrientFromRelatedTable(editTableSchema)));
				result.Add(BTbl.PerViewColumnValue(descriptionColumnId, dsMB.Path.T.ItemLocation.F.ItemID.F.Desc.ReOrientFromRelatedTable(editTableSchema)));
				result.Add(CompositeView.PathAlias(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ItemID, dsMB.Path.T.ItemLocation.F.ItemID.ReOrientFromRelatedTable(editTableSchema)));
				result.Add(CompositeView.PathAlias(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.LocationID, dsMB.Path.T.ItemLocation.F.LocationID.ReOrientFromRelatedTable(editTableSchema)));
				if (groupNewCommands)
					result.Add(CompositeView.NewCommandGroup(newItemLocationButtonGroupKey));
				if (pathAlias != null)
					result.Add(pathAlias);
				if (isPrimary)
					result.AddRange(contextualInits);
				else {
					result.Add(ReadonlyView);
					result.Add(CompositeView.ForceNotPrimary());
				}
				return result;
			};
			// Table #6 (PermanentItemLocation)
			viewArgs = InitILACViewArgs(permanentIL, dsMB.Schema.T.PermanentItemLocation,
				CompositeView.PathAlias(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.CostCenterID, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.CostCenterID),
				// If the current selection happens to be a permanent storage location, helpfully pick it in the new ItemLocation editor.
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentStorage, dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID),
				// If the current selection happens to be an ItemLocation in permanent storage, helpfully pick the same Item/Storeroom in the new ItemLocation editor.
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.PermanentItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID)
			);
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID, viewArgs.ToArray()));

			// Table #7 (TemporaryItemLocation)
			viewArgs = InitILACViewArgs(temporaryIL, dsMB.Schema.T.TemporaryItemLocation,
				CompositeView.PathAlias(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.CostCenterID, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.CostCenterID),
				// If the current selection happens to be a temporary storage location, helpfully pick it in the new ItemLocation editor.
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryStorage, dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID),
				// If the current selection happens to be an ItemLocation in temporary storage, helpfully pick the same Item in the new ItemLocation editor.
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.ItemID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.TemporaryItemLocation.F.ActualItemLocationID.F.ItemLocationID.F.LocationID)
			);
			views.Add(new CompositeView(onlyActiveTemporaryItemLocations ? TIItem.ActiveTemporaryItemLocationTblCreator : TIItem.AllTemporaryItemLocationTblCreator,
				dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.TemporaryItemLocationID, viewArgs.ToArray()));

			// Table #8 (TemplateItemLocation)
			viewArgs = InitILACViewArgs(templateIL, dsMB.Schema.T.TemplateItemLocation,
				null,
				// If the current selection happens to be a temporary storage location, helpfully pick it in the new ItemLocation editor.
				CompositeView.ContextualInit(new int[] {
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.Unit,
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentStorage,
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PostalAddress,
					(int)ViewRecordTypes.LocationDerivationsAndItemLocations.PlainRelativeLocation
					},
					dsMB.Path.T.LocationDerivationsAndItemLocations.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID),
				// If the current selection happens to be an ItemLocation in temporary storage, helpfully pick the same Item in the new ItemLocation editor.
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocationTemplate, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.TemplateItemLocationID.F.ItemLocationID.F.ItemID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.TemporaryItemLocationTemplate, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.TemplateItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.ItemID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.ItemID),
				CompositeView.ContextualInit((int)ViewRecordTypes.LocationDerivationsAndItemLocations.PermanentItemLocation, dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.ActualItemLocationID.F.PermanentItemLocationID.F.ActualItemLocationID.F.ItemLocationID.F.LocationID, dsMB.Path.T.TemplateItemLocation.F.ItemLocationID.F.LocationID)
			);
			views.Add(new CompositeView(dsMB.Path.T.LocationDerivationsAndItemLocations.F.ItemLocationID.F.TemplateItemLocationID, viewArgs.ToArray()));

			return new CompositeTbl(dsMB.Schema.T.LocationDerivationsAndItemLocations,
				tableName,
				attrs.ToArray(),
				dsMB.Path.T.LocationDerivationsAndItemLocations.F.TableEnum,
				views.ToArray()
			);
		}
		#endregion
		#endregion
		#region Declarations for picker Tbl Creators used by XAFDB "pickfrom" extensions
		// NOTE: Various XAFDB files use the "pickfrom" extension to refer to these Tbl creators.
		// Therefore, if you change one of these (e.g. change its name, move it, or remove it), then
		// first check for "pickfrom" extensions and change these as appropriate as well.

		#region Location Picker Tbl Creators
		public static readonly DelayedCreateTbl TemplateTemporaryStoragePickerTblCreator = null;
		public static readonly DelayedCreateTbl PermanentLocationPickerTblCreator = null;
		public static readonly DelayedCreateTbl AllShipToLocationPickerTblCreator = null;
		public static readonly DelayedCreateTbl ShipToLocationForPurchaseOrderTemplatePickerTblCreator = null;
		public static readonly DelayedCreateTbl CompanyLocationPickerTblCreator = null;
		public static readonly DelayedCreateTbl AllTemporaryStoragePickerTblCreator = null;
		#endregion
		#region ItemLocation Picker Tbl Creators
		public static readonly DelayedCreateTbl AllActualItemLocationPickerTblCreator = null;
		public static readonly DelayedCreateTbl ActiveActualItemLocationBrowseTblCreator = null;
		public static readonly DelayedCreateTbl PermanentItemLocationPickerTblCreator = null;
		public static readonly DelayedCreateTbl ActiveTemporaryItemLocationBrowseTblCreator = null;
		public static readonly DelayedCreateTbl PermanentOrTemporaryTaskItemLocationPickerTblCreator = null;
		#endregion
		#endregion
		#region Declarations for Browse/Browsette Tbl Creators
		#region Location Browse Tbl Creators
		public static readonly DelayedCreateTbl LocationBrowseTblCreator = null;
		public static readonly DelayedCreateTbl LocationOrganizerBrowseTblCreator = null;
		#region MoveLocationsCommand - part of Reorganize locations
		private class MoveLocationsCommand : BrowseLogic.MultiBrowserCommand {
			public MoveLocationsCommand(BrowseLogic browser, object targetId)
				: base(browser) {
				IDisablerProperties NeedPrimaryRecordDisabler = new BrowseLogic.NeedPrimaryRecordDisablerClass(Browser, ref Notification);
				Disablers = new IDisablerProperties[Browser.NQueryCompositeViews];
				SourceForIDOfRelativeLocationRowToModify = Browser.GetTblPathDisplaySource(dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID, -1);
				SourceForIDOfPlainRelativeLocationRowToModify = Browser.GetTblPathDisplaySource(dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID.F.PlainRelativeLocationID, -1);
				NoRelativeLocationsInPermanentStorageDisabler = new SettableDisablerProperties(null, TId.SubLocation.Compose("{0} cannot be contained in a Storeroom"), false);
				NoRelativeLocationsInUnitsDisabler = new SettableDisablerProperties(null, TId.SubLocation.Compose("{0} cannot be contained in a Unit"), false);
				for (int viewIndex = Browser.NQueryCompositeViews; --viewIndex >= 0; ) {
					CompositeView cv = Browser.CompositeViews[viewIndex];
					if (!Browser.ViewMightShowPrimaryRecords[viewIndex] || cv.EditTbl.Schema == null) {
						Disablers[viewIndex] = new SettableDisablerProperties(null, cv.Identification.Compose("{0} cannot be moved"), false);
						continue;
					}
					var moveThisViewDisabler = new MultiDisablerIfAllEnabled(NeedPrimaryRecordDisabler);
					if (viewIndex == (int)ViewRecordTypes.LocationDerivations.PlainRelativeLocation) {
						moveThisViewDisabler.Add(NoRelativeLocationsInUnitsDisabler);
						moveThisViewDisabler.Add(NoRelativeLocationsInPermanentStorageDisabler);
					}

					if (cv.EditTblVisibility == Tbl.Visibilities.Disabled)
						moveThisViewDisabler.Add(new SettableDisablerProperties(null, cv.EditTblTipWhenNotVisible, false));

					moveThisViewDisabler.Add(cv.EditTbl.GetPermissionBasedDisabler(browser.DB.Session, TableOperationRightsGroup.TableOperation.Edit));
					Disablers[viewIndex] = moveThisViewDisabler;
				}

				UpdateEnabling(Libraries.DataFlow.DataChangedEvent.Reset, null);

				Browser.ControlCreationCompletedNotificationRecipients += delegate() {
					bool NeedsContext;
					TargetLocationIdSource = new ControlValue(targetId).GetSourceForInit(Browser, -1, out NeedsContext);
					TargetUnitIdSource = new InSubBrowserValue( targetId, new BrowserPathValue(dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID.F.UnitID)).GetSourceForInit(Browser, -1, out NeedsContext);
					TargetPermanentStorageIdSource = new InSubBrowserValue(targetId, new BrowserPathValue(dsMB.Path.T.LocationDerivations.F.LocationID.F.RelativeLocationID.F.PermanentStorageID)).GetSourceForInit(Browser, -1, out NeedsContext);
				};
			}
			public void TargetPickerChanged() {
				NoRelativeLocationsInUnitsDisabler.Enabled = TargetUnitIdSource.GetValue() == null;
				NoRelativeLocationsInPermanentStorageDisabler.Enabled = TargetPermanentStorageIdSource.GetValue() == null;
				UpdateEnabling(Libraries.DataFlow.DataChangedEvent.Reset, null);
			}
			private readonly SettableDisablerProperties NoRelativeLocationsInUnitsDisabler;
			private readonly SettableDisablerProperties NoRelativeLocationsInPermanentStorageDisabler;
			private Libraries.DataFlow.Source TargetLocationIdSource;
			private Libraries.DataFlow.Source TargetUnitIdSource;
			private Libraries.DataFlow.Source TargetPermanentStorageIdSource;
			private BrowseLogic.RowContentChangedHandler Notification;
			// The following arrays are all indexed by view index.
			// The enablers that control whether deletion is allowed.
			private readonly IDisablerProperties[] Disablers;
			// The source in the browser for the ID of the row within TableToModify that we would have to modify to perform the deletion
			private readonly Libraries.DataFlow.Source SourceForIDOfRelativeLocationRowToModify;
			private readonly Libraries.DataFlow.Source SourceForIDOfPlainRelativeLocationRowToModify;
			protected override Key AnalyzeSingleRecord(int enabledCount) {
				// TODO: We still must subscribe to external disablers such as the PermissionBasedDisabler; if it notifies we must re-evaluate from scratch.
				Notification();
				int viewIndex = Browser.CompositeRecordType ?? int.MaxValue;
				if (viewIndex >= Disablers.Length)
					return KB.K("The record is not a recognized type");
				if (!Disablers[viewIndex].Enabled)
					return Disablers[viewIndex].Tip ?? KB.K("No reason specified");
				return null;
			}
			public override void Execute() {
				int count = 0;
				int viewIndex = int.MaxValue;
				// Prohibit move if an editor is currently editing the record. Note that this check is done
				// here but it should really be done using the Enabled property of the command. This would however remove the ability to activate the offending editor.
				// This could be done via new ApplicationTblDefaultsUsingWindows.AlreadyBeingEditedDisabler(TblSchema) but then you must set the Id property of the disabler,
				// and this is a single Id. The disabler would have to be expanded to take a set of ID's, and we would once again be faced with the choice of whether
				// the overall Enabled would be false if *any* or *all* of the named id's were being edited. Either that, or we would have to make one such notifier for each record
				// in the selection, and figure out how to keep it somewhere.
				if (!Browser.IterateOverContextRecords(
					delegate() {
						viewIndex = Browser.CompositeRecordType ?? int.MaxValue;
						++count;
						IUIModifyingRecord modifier = Application.Instance.GetInterface<ITblDrivenApplication>().FindRecordModifier(new object[] { Browser.CompositeRecordEditIDSource.GetValue() }, new DBI_Table[] { Browser.CompositeViews[viewIndex].EditTbl.Schema });
						if (modifier != null) {
							if (Ask.Question(KB.K("Cannot move because a record is currently being modified in another form. Do you want to switch to that form?").Translate()) == Ask.Result.Yes)
								modifier.Activate();
							return false;
						}
						return true;
					}))
					return;

				// TODO: (proper concurrency checking): we really should check all the columns of the record and its derivations and bases against anything
				// being shown in the browser. Rather than fetching the records here we should copy them (somehow) from the browser's data set. This would require
				// a bit of XAFCLient cooperation so it knows we are editing based on already-fetched record copies. We would also have to ensure that we only check
				// stuff that the browser shows, but the explicit concurrency checking as it stands does not support this.
				using (XAFDataSet dsMove = XAFDataSet.New(Browser.TInfo.Schema.Database, Browser.DB)) {
					dsMove.EnforceConstraints = false;
					dsMove.DataSetName = KB.I("BrowseBaseControl.MoveRecord.dsMove");
					Browser.IterateOverContextRecords(
						delegate() {
							var rowToModify = (dsMB.RelativeLocationRow) dsMove.DB.ViewAdditionalRow(dsMove, dsMB.Schema.T.RelativeLocation, new SqlExpression(dsMB.Path.T.RelativeLocation.F.Id).Eq(SqlExpression.Constant(SourceForIDOfRelativeLocationRowToModify.GetValue())));
							if (rowToModify == null) // already deleted (if we can't find now, perhaps someone else moved or deleted it while we were browsing)
								return true; // nothing further we can do

							rowToModify.F.ContainingLocationID = (System.Guid) TargetLocationIdSource.GetValue();
							return true;
						}
					);

					Thinkage.Libraries.DBILibrary.Server.UpdateOptions updateOptions = Thinkage.Libraries.DBILibrary.Server.UpdateOptions.Normal;
					for (; ; ) {
						try {
							dsMove.DB.Update(dsMove, updateOptions);
						}
						catch (DBConcurrencyException e) {
							if (updateOptions == Libraries.DBILibrary.Server.UpdateOptions.Normal && Browser.BrowseUI.HandleConcurrencyError(Browser.DB, e)) {
								// User wants to retry the delete even though changed
								updateOptions = Libraries.DBILibrary.Server.UpdateOptions.NoConcurrencyCheck;
								continue;
							}
						}
						return;
					}
				}
			}

			public override bool RunElevated {
				get {
					return false;
				}
			}
			protected override Key TipWhenEnabled {
				get {
					return KB.K("Change the parent location of the selected records to be the destination location");
				}
			}
		}
		#endregion
		public static readonly DelayedCreateTbl UnitBrowseTblCreator = null;
		public static readonly DelayedCreateTbl PermanentStorageBrowseTblCreator = null;
		public static readonly DelayedCreateTbl AllStorageBrowseTblCreator = null;
		#endregion
		#region ItemLocation Browse Tbl Creators
		public static readonly DelayedCreateTbl TemporaryTaskItemLocationBrowseTblCreator = null;
		public static readonly DelayedCreateTbl PermanentItemLocationBrowseTblCreator = null;
		public static readonly DelayedCreateTbl TemporaryItemLocationBrowseTblCreator = null;
		#endregion
		#endregion

		static TILocations() {
			#region Location Tbl Creators
			#region PermanentLocationPickerTblCreator
			// For picking a non-temporary location, e.g. for temporary storage (task), contact
			PermanentLocationPickerTblCreator = new DelayedCreateTbl(delegate() {
				return LACComposite(TId.NonTemporaryLocation, null, null,
					ViewUse.Full,	// Table 0 (PostalAddress)
					ViewUse.None,	// Table 1 (TemporaryStorage)
					ViewUse.Full,	// Table 2 (Unit)
					ViewUse.Full,	// Table 3 (PermanentStorage)
					ViewUse.Full,	// Table 4 (PlainRelativeLocation)
					ViewUse.None	// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region AllShipToLocationPickerTblCreator
			// For picking a location where items can be shipped to (for a PurchaseOrder)
			AllShipToLocationPickerTblCreator = new DelayedCreateTbl(delegate() {
				return LACComposite(TId.AllowedShipToLocation, null, null,
					ViewUse.Full, 		// Table 0 (PostalAddress)
					ViewUse.Full,		// Table 1 (TemporaryStorage)
					ViewUse.Full,	 	// Table 2 (Unit)
					ViewUse.Full, 		// Table 3 (PermanentStorage)
					ViewUse.Full,	// Table 4 (PlainRelativeLocation)
					ViewUse.None	// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region ShipToLocationForPurchaseOrderTemplatePickerTblCreator
			// For picking a location where items can be shipped to (for a PurchaseOrderTemplate)
			ShipToLocationForPurchaseOrderTemplatePickerTblCreator = new DelayedCreateTbl(delegate() {
				return LACComposite(TId.AllowedShipToLocation, null, null,
					ViewUse.Full, 	// Table 0 (PostalAddress)
					ViewUse.None, 	// Table 1 (TemporaryStorage)
					ViewUse.Full, 	// Table 2 (Unit)
					ViewUse.Full, 	// Table 3 (PermanentStorage)
					ViewUse.Full,	// Table 4 (PlainRelativeLocation)
					ViewUse.None	// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region CompanyLocationPickerTblCreator
			// For picking a location to use for the Purchaser's address on Purchase Orders
			CompanyLocationPickerTblCreator = new DelayedCreateTbl(delegate() {
				return LACComposite(TId.PostalAddress, null, null,
					ViewUse.Full, 	// Table 0 (PostalAddress)
					ViewUse.None, 	// Table 1 (TemporaryStorage)
					ViewUse.None, 	// Table 2 (Unit)
					ViewUse.None, 	// Table 3 (PermanentStorage)
					ViewUse.None,	// Table 4 (PlainRelativeLocation)
					ViewUse.None	// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region AllTemporaryStoragePickerTblCreator
			// For picking a temporary storage location
			AllTemporaryStoragePickerTblCreator = new DelayedCreateTbl(delegate() {
				return LACComposite(TId.TemporaryStorage, null, TIReports.NewRemotePTbl(TIReports.TemporaryStorageReport),
					ViewUse.Secondary, 	// Table 0 (PostalAddress)
					ViewUse.Full,		// Table 1 (TemporaryStorage)
					ViewUse.Secondary, 	// Table 2 (Unit)
					ViewUse.Secondary, 	// Table 3 (PermanentStorage)
					ViewUse.Secondary,	// Table 4 (PlainRelativeLocation)
					ViewUse.None		// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region TemplateTemporaryStoragePickerTblCreator
			// For picking the containing location of a permanent storage location
			TemplateTemporaryStoragePickerTblCreator = new DelayedCreateTbl(delegate() {
				return LACComposite(TId.TemplateTemporaryStorage, null, null,
					ViewUse.Secondary, 	// Table 0 (PostalAddress)
					ViewUse.None, 		// Table 1 (TemporaryStorage)
					ViewUse.Secondary, 	// Table 2 (Unit)
					ViewUse.Secondary, 	// Table 3 (PermanentStorage)
					ViewUse.Secondary,	// Table 4 (PlainRelativeLocation)
					ViewUse.Full		// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion

			#region LocationBrowseTblCreator
			// For browsing/picking normal locations (postal/relative)
			LocationBrowseTblCreator = new DelayedCreateTbl(delegate()
			{
				return LACComposite(TId.Location, dsMB.Schema.T.Location,
					TIReports.NewRemotePTbl(
						new DelayedCreateTbl(
							delegate()
							{
								return TIReports.LocationReport;
							}
						)
					),
					ViewUse.Full, 		// Table 0 (PostalAddress)
					ViewUse.None, 		// Table 1 (TemporaryStorage)
					ViewUse.None, 		// Table 2 (Unit)
					ViewUse.None, 		// Table 3 (PermanentStorage)
					ViewUse.Full,		// Table 4 (PlainRelativeLocation)
					ViewUse.None		// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region LocationOrganizerBrowseTblCreator
			// Requires Unit table permissions since it is Units that are what are commonly 'organized'
			LocationOrganizerBrowseTblCreator = new DelayedCreateTbl(delegate() {
				object targetLocationId = KB.I("Target LocationID");
				return LACComposite(TId.Location, dsMB.Schema.T.Unit, null,
					ViewUse.Secondary, 			// Table 0 (PostalAddress)
					ViewUse.None, 				// Table 1 (TemporaryStorage)
					ViewUse.FullOnlyExisting, 	// Table 2 (Unit)
					ViewUse.FullOnlyExisting, 	// Table 3 (PermanentStorage)
					ViewUse.FullOnlyExisting,	// Table 4 (PlainRelativeLocation)
					ViewUse.None,				// Table 5 (TemplateTemporaryStorage)
						BTbl.AdditionalVerb(null,
						delegate(BrowseLogic browserLogic) {
							var cmd = new MoveLocationsCommand(browserLogic, targetLocationId);
						browserLogic.Commands.AddCommand(KB.K("Move to Destination"), null, cmd, null, null,
								TblUnboundControlNode.New(KB.K("Destination"), dsMB.Schema.T.RelativeLocation.F.ContainingLocationID.EffectiveType,
									new DCol(Fmt.SetId(targetLocationId)),
									Fmt.SetPickFrom(TILocations.PermanentLocationPickerTblCreator, new SettableDisablerProperties(KB.K("Choose the location where the selected records will be moved to"))),
									Fmt.SetCreated(
										delegate(IBasicDataControl c) {
											c.Notify += cmd.TargetPickerChanged;
										})));
							return null;
						}
					)
				);
			});

			#endregion

			#region UnitBrowseTblCreator
			// For browsing units
			UnitBrowseTblCreator = new DelayedCreateTbl(delegate()
			{
				return LACComposite(TId.Unit, dsMB.Schema.T.Unit, TIReports.NewRemotePTbl(TIReports.UnitReport),

					ViewUse.Secondary, 	// Table 0 (PostalAddress)
					ViewUse.None, 		// Table 1 (TemporaryStorage)
					ViewUse.Full, 		// Table 2 (Unit)
					ViewUse.Secondary, 	// Table 3 (PermanentStorage)
					ViewUse.Secondary,	// Table 4 (PlainRelativeLocation)
					ViewUse.None		// Table 5 (TemplateTemporaryStorage)
					);
			});
			#endregion
			#region PermanentStorageBrowseTblCreator
			// For browsing/picking a permanent storage template location
			PermanentStorageBrowseTblCreator = new DelayedCreateTbl(delegate()
			{
				return LACComposite(TId.Storeroom, dsMB.Schema.T.PermanentStorage, TIReports.NewRemotePTbl(new DelayedCreateTbl(delegate() {
						return TIReports.PermanentStorageReport;
					})),
					ViewUse.Secondary, 	// Table 0 (PostalAddress)
					ViewUse.None, 		// Table 1 (TemporaryStorage)
					ViewUse.Secondary, 	// Table 2 (Unit)
					ViewUse.Full, 		// Table 3 (PermanentStorage)
					ViewUse.Secondary,	// Table 4 (PlainRelativeLocation)
					ViewUse.None		// Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#region AllStorageBrowseTblCreator
			// For browsing/picking a All storage template location
			AllStorageBrowseTblCreator = new DelayedCreateTbl(delegate () {
				return LACComposite(TId.Storeroom, dsMB.Schema.T.RelativeLocation, TIReports.NewRemotePTbl(new DelayedCreateTbl(delegate () {
						return TIReports.StorageLocationStatus;
					})),
					ViewUse.Secondary,  // Table 0 (PostalAddress)
					ViewUse.Full,       // Table 1 (TemporaryStorage)
					ViewUse.Secondary,  // Table 2 (Unit)
					ViewUse.Full,       // Table 3 (PermanentStorage)
					ViewUse.Secondary,  // Table 4 (PlainRelativeLocation)
					ViewUse.None        // Table 5 (TemplateTemporaryStorage)
				);
			});
			#endregion
			#endregion
			#region ItemLocation Tbl Creators
			#region AllActualItemLocationPickerTblCreator
			// For picking an ItemLocation for an item located in actual (permanent or temporary) storage
			AllActualItemLocationPickerTblCreator = new DelayedCreateTbl(delegate() {
				return ILACComposite(TId.StoreroomOrTemporaryStorageAssignment, null, null, false, false,
					true,	// Table #5 (PermanentItemLocation)
					true,	// Table #6 (TemporaryItemLocation)
					false	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region ActiveActualItemLocationBrowseTblCreator
			// For picking an ItemLocation for an item located in permanent storage or temporary storage for a particular work order.
			ActiveActualItemLocationBrowseTblCreator = new DelayedCreateTbl(delegate() {
				return ILACComposite(TId.StoreroomOrTemporaryStorageAssignment, null, null, false, true,
					true,	// Table #5 (PermanentItemLocation)
					true,	// Table #6 (TemporaryItemLocation)
					false	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region PermanentItemLocationPickerTblCreator
			// For picking an ItemLocation for an item located in permanent storage
			PermanentItemLocationPickerTblCreator = new DelayedCreateTbl(delegate() {
				return ILACComposite(TId.StoreroomAssignment, dsMB.Schema.T.PermanentItemLocation,
					null,
					false, false,
					true,	// Table #5 (PermanentItemLocation)
					false,	// Table #6 (TemporaryItemLocation)
					false	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region PermanentItemLocationBrowseTblCreator
			// For browsing an ItemLocation for an item located in permanent storage
			PermanentItemLocationBrowseTblCreator = new DelayedCreateTbl(delegate()
			{
				return ILACComposite(TId.StoreroomAssignment, dsMB.Schema.T.PermanentItemLocation,
					TIReports.NewRemotePTbl(new DelayedCreateTbl(delegate() {
						return TIReports.PermanentInventoryLocation;
					})),
					false, false,
					true,	// Table #5 (PermanentItemLocation)
					false,	// Table #6 (TemporaryItemLocation)
					false	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region PermanentOrTemporaryTaskItemLocationPickerTblCreator
			// For picking an ItemLocation for an item located in permanent storage or template temporary storage
			PermanentOrTemporaryTaskItemLocationPickerTblCreator = new DelayedCreateTbl(delegate()
			{
				return ILACComposite(TId.TaskStoreroomorTemporaryStorageAssignment, dsMB.Schema.T.TemporaryItemLocation, null, false, false,
					true,	// Table #5 (PermanentItemLocation)
					false,	// Table #6 (TemporaryItemLocation)
					true	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region ActiveTemporaryItemLocationBrowseTblCreator
			ActiveTemporaryItemLocationBrowseTblCreator = new DelayedCreateTbl(delegate() {
				return ILACComposite(TId.TemporaryStorageAssignment, dsMB.Schema.T.TemporaryItemLocation,
					null,
					false, true,
					false,	// Table #5 (PermanentItemLocation)
					true,	// Table #6 (TemporaryItemLocation)
					false	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region TemporaryItemLocationBrowseTblCreator
			// For browsing an ItemLocation for an item located in temporary storage
			TemporaryItemLocationBrowseTblCreator = new DelayedCreateTbl(delegate()
			{
				return ILACComposite(TId.TemporaryStorageAssignment, dsMB.Schema.T.TemporaryItemLocation,
					null,
					false, false,
					false,	// Table #5 (PermanentItemLocation)
					true,	// Table #6 (TemporaryItemLocation)
					false	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#region TemporaryTaskItemLocationBrowseTblCreator
			// For browsing/picking an ItemLocation for an item located in template temporary storage
			TemporaryTaskItemLocationBrowseTblCreator = new DelayedCreateTbl(delegate() {
				return ILACComposite(TId.TaskTemporaryStorageAssignment, dsMB.Schema.T.TemplateItemLocation,
					TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.TemplateInventoryLocation; })),
					false, false,
					false,	// Table #5 (PermanentItemLocation)
					false,	// Table #6 (TemporaryItemLocation)
					true	// Table #7 (TemplateItemLocation)
				);
			});
			#endregion
			#endregion
		}
		internal static void DefineTblEntries()
		{
		}
		private TILocations()
		{
		}
	}
}
