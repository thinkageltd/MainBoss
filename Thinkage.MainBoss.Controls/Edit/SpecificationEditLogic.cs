using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	public class SpecificationEditLogic : EditLogic {
		public static readonly Thinkage.Libraries.Translation.Key CustomFieldPlaceholderLabel = KB.T("Custom Fields Placeholder");
		private class RowMapperClass {
			public RowMapperClass(DBClient db, Tbl tbl, EdtMode mode, object[][] rowIDs, List<TblActionNode>[] initLists) {
				if (rowIDs.Length != 1)
					throw new GeneralException(KB.T("Only one Specification can be edited at a time"));

				// Create a mapping between recordSet indices and SpecificationFormFieldID IDs.
				SpecificationFormFieldIDsByRecordSet = new List<object>();
				for (int i = tbl.RecordSetCount; --i >= 0;)
					SpecificationFormFieldIDsByRecordSet.Add(null);

				// Create a new set of layout nodes for use in our substitute Tbl. We will be replacing a placeholder with the nodes resulting from the SpecificationFormField records.
				System.Diagnostics.Debug.Assert(((TblLayoutNode)tbl.Columns[0]).Label.IdentifyingName == "Details");
				var newDetailsLayout = new List<TblLayoutNode>(((TblLayoutNodeArray)((TblContainerNode)tbl.Columns[0]).Columns.Clone()).ColumnArray);

				// We have to make sure the Form is properly identified. There must be an Init directive to set the field if the user starts or switches to New mode,
				// and we also have to know the form ID right here in order to generate the custom Tbl.

				// Note that we only look for the init in the initList argument, not in the Inits in the basis Tbl.
				ModifiedInitLists = new List<TblActionNode>[] { initLists?[0] == null ? new List<TblActionNode>() : new List<TblActionNode>(initLists[0]) };
				if (mode != EdtMode.EditDefault && mode != EdtMode.ViewDefault) {
					// Find and extract the Init targeting the SpecificationFormID field. There should be only one.
					Init formIdInit = null;
					for (int i = ModifiedInitLists[0].Count; --i >= 0;) {
						if (!(ModifiedInitLists[0][i] is Init init))
							continue;
						PathTarget pTarget = init.InitTarget as PathTarget;
						PathOrFilterTarget pfTarget = init.InitTarget as PathOrFilterTarget;
						if (pTarget == null && pfTarget == null)
							continue;
						if (pTarget == null) {
							if (pfTarget.RecordSet != 0 || !pfTarget.PathInEditBuffer.Equals(dsMB.Path.T.Specification.F.SpecificationFormID))
								continue;
						}
						else {
							if (pTarget.RecordSet != 0 || !pTarget.PathInEditBuffer.Equals(dsMB.Path.T.Specification.F.SpecificationFormID))
								continue;
						}
						// This Init targets the field in question.
						System.Diagnostics.Debug.Assert(formIdInit == null, "Specification edit control: Found extra Init for form ID");
						formIdInit = init;
						ModifiedInitLists[0].RemoveAt(i);
#if !DEBUG
						break;
#endif
					}
					object formID = null;
					if (formIdInit != null) {
						// TODO: Verify the init is on New Load only.

						// Verify the init has a constant source whose value is not null.
						if (!(formIdInit.InitValue is ConstantValue source))
							formIdInit = null;  // TODO: this quietly tosses the "defective" init we found.
						else
							formID = source.Value;
					}

					using (dsMB ds = new dsMB(db)) {
						if (mode != EdtMode.New) {
							// In all modes relying on an existing specification record fetch the identified Specification record
							// and extract the form ID from it. We ignore any init that might have been there
							// TODO (debugging?) if there was an init it should match what is in the record. Not sure about this one.
							DBIDataRow r = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.Specification, SqlExpression.Constant(rowIDs[0][0]).Eq(new SqlExpression(dsMB.Path.T.Specification.F.Id)));
							if (r != null)
								formID = r[dsMB.Schema.T.Specification.F.SpecificationFormID];
							else
								throw new GeneralException(KB.K("Cannot find specification record"));
						}
						if (formID == null)
							throw new GeneralException(KB.K("Cannot edit or create a specification without selecting a Form"));

						// Force the value for the Form on new records. Because we use PathOrFilterTarget and the field is captive this forces the picker always readonly.
						ModifiedInitLists[0].Add(Init.OnLoadNew(new PathOrFilterTarget(dsMB.Path.T.Specification.F.SpecificationFormID), new ConstantValue(formID)));

						//
						// Find the placeholder in the layout columns before which we should place the custom fields. It is identified by its NodeId.
						// Note that we do no remove the placeholder node. If we find no placeholder the nodes go at the end.
						int customPlacement;
						for (customPlacement = 0; customPlacement < newDetailsLayout.Count; ++customPlacement)
							if (newDetailsLayout[customPlacement].Label == CustomFieldPlaceholderLabel)
								break;
						// Remove the placeholder now that we have found it.
						newDetailsLayout.RemoveAt(customPlacement);

						var splitterContents = new List<TblLayoutNode>();
						var splitterColumnContents = new List<TblLayoutNode>();

						// Query the SpeficiationForm and SpecificationFormField records.
						// I would like to do this as one View call, but if I ask for the Fields and to follow a path back to the specification form, and there
						// are no fields, I will also not get the form. Currently we don't need the form anyway. We would only need it if we had it version-numbered
						// and we wanted to verify that the form had not changed since the user started editing the specificaiton. But this needs additional
						// concurrency checking ability.
						//ds.DB.View(ds, dsMB.Schema.T.SpecificationForm, SqlExpression.Constant(FormID).Eq(new SqlExpression(dsMB.Path.T.SpecificationForm.F.Id)))
						ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.SpecificationFormField, SqlExpression.Constant(formID).Eq(new SqlExpression(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID)));
						// Build the extra tbl layout nodes and add to the mapping table. We use a DataView to sort the fields in desired order.
						foreach (var row in ds.T.SpecificationFormField.Rows.Select(null, new SortExpression(dsMB.Path.T.SpecificationFormField.F.FieldOrder))) {
							int rs = SpecificationFormFieldIDsByRecordSet.Count;
							object formFieldID = row.F.Id;
							SpecificationFormFieldIDsByRecordSet.Add(formFieldID);

							// TODO: allow types other than 1-line text.
							// TODO: The data field should be of type binary (blob) [and then the 80 that corresponds to the field limit in SpecificationFormData.FieldValue can go away]
							// To enforce the column length properly, we need a custom derivation of TblColumnNode which overrides the accessor that obtains
							// the type of the bound value, and which also wraps the field with an encoder similar to the one used for Variables.
							splitterColumnContents.Add(
								TblCustomTypedColumnNode.New(
									KB.T(row.F.EditLabel),
									new StringTypeInfo(0, Math.Min(row.F.FieldSize, 80), 0, false, false, false),
										delegate (object o) {
											// nothing special needed for now
											return o;
										},
										delegate (object o) {
											// nothing special needed for now
											return o;
										},
									dsMB.Path.T.SpecificationData.F.FieldValue, rs, ECol.Normal));

							// Add the Init directive which in New and Clone modes links the Data record to the Specification
							ModifiedInitLists[0].Add(Init.LinkRecordSets(dsMB.Path.T.SpecificationData.F.SpecificationID, rs, dsMB.Path.T.Specification.F.Id, 0));

							// Add the Init directive which in New mode links the Data record to the SpecificationFormField (clone doesn't need this, as the cloned value
							// will be correct)
							ModifiedInitLists[0].Add(Init.OnLoadNew(new PathTarget(dsMB.Path.T.SpecificationData.F.SpecificationFormFieldID, rs), new ConstantValue(formFieldID)));

							if (splitterColumnContents.Count >= 15) {
								splitterContents.Add(TblSectionNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, splitterColumnContents.ToArray()));
								splitterColumnContents.Clear();
							}
						}
						if (splitterColumnContents.Count > 0)
							splitterContents.Add(TblSectionNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, splitterColumnContents.ToArray()));
						newDetailsLayout.Insert(customPlacement, TblContainerNode.New(new TblLayoutNode.ICtorArg[] { ECol.Normal }, splitterContents.ToArray()));
					}
				}
				// we replace the Details container and other tab nodes with JUST the newDetailsLayout
				CustomizedTbl = new Tbl(tbl.Schema, tbl.Identification, tbl.Attributes, new TblLayoutNodeArray(newDetailsLayout), tbl.InitList);
			}
			public readonly List<TblActionNode>[] ModifiedInitLists;
			public readonly List<object> SpecificationFormFieldIDsByRecordSet;
			public readonly Tbl CustomizedTbl;
		}
		public SpecificationEditLogic(IEditUI control, DBClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: this(control, db, initialEditMode, initRowIDs, subsequentModeRestrictions, new RowMapperClass(db, tbl, initialEditMode, initRowIDs, initLists), settingsContainer) {
		}
		private SpecificationEditLogic(IEditUI control, DBClient db, EdtMode initialEditMode, object[][] initRowIDs, bool[] subsequentModeRestrictions, RowMapperClass rowMapper, Settings.Container settingsContainer)
			: base(control, db, rowMapper.CustomizedTbl, settingsContainer, initialEditMode, initRowIDs, subsequentModeRestrictions, rowMapper.ModifiedInitLists) {
			RowMapper = rowMapper;
		}
		private readonly RowMapperClass RowMapper;
		public override object[] GetEditRowIDs() {
			// Override EditLogic's holdover code that eliminates all but the first ID
			return GetFullRowIDs();
		}
		public override object[] GetFullRowIDs() {
			object[] result = base.GetFullRowIDs();
			using (dsMB ds = new dsMB(DB)) {
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.SpecificationData, new SqlExpression(dsMB.Path.T.SpecificationData.F.SpecificationID).Eq(SqlExpression.Constant(result[0])));
				foreach (DBIDataRow r in ds.T.SpecificationData.Rows) {
					object formFieldID = r[dsMB.Schema.T.SpecificationData.F.SpecificationFormFieldID];
					object dataID = r[dsMB.Schema.T.SpecificationData.F.Id];
					for (int i = RowMapper.SpecificationFormFieldIDsByRecordSet.Count; --i >= 0;)
						if (dsMB.Schema.T.SpecificationData.F.SpecificationFormFieldID.EffectiveType.GenericEquals(formFieldID, RowMapper.SpecificationFormFieldIDsByRecordSet[i])) {
							result[i] = dataID;
							break;
						}
				}
			}
			return result;
		}
	}
}
