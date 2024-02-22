using System;
using System.Collections.Generic;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.Libraries.XAF.Database.Layout;

// TODO: Use <caption> (first element within <table>) to label nested groups and the main field group
// TODO: Use <thead> instead of <tr> for the header row of a multi-column group
// TODO: Use explicit <tbody> around <tr> collection
namespace Thinkage.Libraries.Presentation.ASPNet {
	public class EditorControl : HtmlSequence, IEditUI {
		// TODO: A method to add a command button with a new URL  and options for a new Tbl, same Tbl, and passing the current ID or not.

		protected string ModeName {
			get {
				switch (Mode) {
				case EdtMode.Clone:
					return KB.T("record cloning").Translate();
				case EdtMode.Edit:
					return KB.T("record editing").Translate();
				case EdtMode.View:
					return KB.T("record viewing").Translate();
				case EdtMode.ViewDeleted:
					return KB.T("deleted record viewing").Translate();
				case EdtMode.ViewDefault:
					return KB.T("default values viewing").Translate();
				case EdtMode.EditDefault:
					// TODO: Need a 'default record viewing' mode??
					return KB.T("default values editing").Translate();
				case EdtMode.New:
					return KB.T("new record creation").Translate();
				case EdtMode.UnDelete:
					return KB.T("record recovery").Translate();
				}
				return KB.I("");
			}
		}
		private class EditControlInfo : EditLogic.TblLayoutNodeInfo {
			public EditControlInfo(TblLayoutNode definingLayoutNode)
				: base(definingLayoutNode) {
			}
			public bool InLine = false;
			public Key Label;
			public System.Web.UI.Control BareControl;
			public uint OriginalValueStoreIndex;
			public override bool HasUI {
				get { return BareControl != null || (RowContents != null && RowContents.HasUI); }
			}
		}
		#region Page processing sequence
		#region Construction
		#region - Constructor
		public EditorControl(XAFClient db, Tbl tbl, EdtMode initialEditMode, object[] initRowIDs, bool[] subsequentModeRestrictions, List<TblActionNode> initList) {
			TInfo = tbl;
			EditorLogic = EditLogic.Create(this, db, tbl, null, initialEditMode, new object[][] { initRowIDs }, subsequentModeRestrictions, new List<TblActionNode>[] { initList });
			//TODO: RecordID should probably be just EditorLogic.RowRootID
			RecordId = initRowIDs != null && initRowIDs.Length > 0 ? initRowIDs[0] : null;
			EnableViewState = false;

			CurrentlyOpenPicker = new HiddenField();
			Controls.Add(CurrentlyOpenPicker);

			{
				// <body> fields
				HtmlGenericControl subtitle = new HtmlGenericControl("H2");
				Controls.Add(subtitle);
				subtitle.InnerText = "Fields";
				// TODO: EditorLogic.SetupControls needs a way for IEditUI.CreateControl to say "Don't make a ControlInfo for this"
				// or some other way it can be called several times filtering out various controls. Perhaps another callback in the interface,
				// or a delegate passed to SetupControls?
				Controls.Add(CreateGroupContent(EditorLogic.SetupControls()));
			}

			if (Mode == EdtMode.View || Mode == EdtMode.ViewDeleted) {
				// <body> browsette linkages, only on existing records. Perhaps these should even be limited to View mode, or have
				// a way to call up a browsette that can't make any changes to the data, for Edit mode, to avoid the problem with browsettes
				// that require saving the master record first. Or the TblBrowsetteNode should contain a "Master must be saved" attribute.
				HtmlGenericControl subtitle = null;
				HtmlGenericControl browsetteLinkageList = null;
				// Find all the browsettes defined by the Tbl; use the containing tab or group label, or maybe the label of the target tbl.
				// TODO: Handle browsettes whose master expression is not the ID of the master record.
				// Typically the TblBrowsetteNode does not carry a label, so we have to track the label of the closest-enclosing container.
				List<Key> labelStack = new List<Key>();
				TInfo.Columns.TraverseLayoutNodes(
					delegate(TblLayoutNode node) {
						ECol ec = TblLayoutNode.GetECol(node);
						if (ec == null)
							return false;
						// Because we don't do Checks and control Inits we can just skip controls that are not visible.
						if (node.Visibility != Tbl.Visibilities.Visible)
							return false;
						if (ec.Access[(int)Mode] == TblLeafNode.Access.Omit)
							return false;
						if (node is TblContainerNode && node.Label != null)
							labelStack.Add(node.Label);
						// We create browsettes separately (here) so any leaf nodes that are not browsettes are filtered out.
						if (node is TblLeafNode && Fmt.GetShowReferences(node) == null && Fmt.GetShowReferences(ec) == null)
							return false;
						return true;
					},
					delegate(TblLayoutNode node) {
						if (node is TblContainerNode) {
							if (node.Label != null)
								labelStack.RemoveAt(labelStack.Count - 1);
						}
						else {
							//	use the top of the labels list to create a browsette linkage
							if (subtitle == null) {
								subtitle = new HtmlGenericControl("H2");
								Controls.Add(subtitle);
								subtitle.InnerText = "Child records";

								browsetteLinkageList = new HtmlGenericControl("ul");
								Controls.Add(browsetteLinkageList);
							}
							{
								var cNode = (TblColumnNode)node;
								Fmt.ShowReferencesArg bn = Fmt.GetShowReferences(TblLayoutNode.GetECol(node));
								if (bn == null)
									bn = Fmt.GetShowReferences(node);
								if (bn.ReferencingLink == null)
									// THis condition is temporary until we can honour BrowsetteFilterBind. Without it you would get an unfiltered browsette.
									// TODO: Honour BrowsetteFilterBind (even if multiple); make CallBrowsetteControl just
									// be CallBrowserControl with the ability to create filters and qualify the title.
									return;
								Tbl referencedTbl = bn.BasisTbl;
								if (referencedTbl.Visibility != Tbl.Visibilities.Visible
									|| !referencedTbl.GetPermissionBasedDisabler(TableOperationRightsGroup.TableOperation.View).Enabled)
									return;
								// TODO: any exported commands should appear as our own command buttons, even in View mode.
								// to accomplish this, CallBrowsetteControl should act as a dummy IBrowseUI and create an appropriate BrowseLogic object in order
								// to extract its exported commands if any. The added Filters and value binding must not only change the URL for calling up the
								// browser but must also change the corresponding information on the BrowseLogic object.
								//ctrl.ExportButtons(delegate(IContainable button) {
								//	Buttons.Add(button);
								//});
								DataFlow.NotifyingSource s = null;
								s = RecordManager.GetPathNotifyingSource(cNode.Path, cNode.RecordSet);
								HtmlGenericControl browserLinkageListItem = new HtmlGenericControl("li");
								CallBrowsetteControl browserLinkage
									= new CallBrowsetteControl(labelStack[labelStack.Count - 1], Strings.Format(KB.K(" for {0} {{0}}"), TInfo.Identification), referencedTbl);
								browserLinkage.Target = "_blank";	// Ask for a new form.
								browserLinkageListItem.Controls.Add(browserLinkage);
								browsetteLinkageList.Controls.Add(browserLinkageListItem);
								Browsettes.Add(browserLinkage);
								if (bn.ReferencingLink != null) {
									DataFlow.NotifyingValue bf = browserLinkage.AddFilter(cNode.ReferencedType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo), bn.ReferencingLink);
									EditLogic.DataAction pushChannel = new EditLogic.BindingOrInitDataAction(EditorLogic, s, bf, null, null, -1);
									EditorLogic.AddDynamicInit(pushChannel);
								}
								foreach (BrowsetteValueBind vp in TblLayoutNode.GetBrowsetteValueBinds(cNode)) {
									DataFlow.NotifyingValue bv = browserLinkage.AddValueBindTarget(vp.LayoutNodeId);
									if (bv != null) {
										DataFlow.NotifyingSource boundSource = (DataFlow.NotifyingSource)vp.EditSource.GetNotifyingSource(EditorLogic);
										EditLogic.DataAction pushChannel = new EditLogic.BindingOrInitDataAction(EditorLogic, boundSource, bv, null, null, -1);
										EditorLogic.AddDynamicInit(pushChannel);
									}
								}
								foreach (BrowsetteFilterBind bfb in TblLayoutNode.GetBrowsetteFilterBinds(cNode)) {
									DataFlow.NotifyingValue bv = browserLinkage.AddFilterBindTarget(bfb.FilterId);
									if (bv != null) {
										DataFlow.NotifyingSource boundSource = (DataFlow.NotifyingSource)bfb.EditSource.GetNotifyingSource(EditorLogic);
										EditLogic.DataAction pushChannel = new EditLogic.BindingOrInitDataAction(EditorLogic, boundSource, bv, null, null, -1);
										EditorLogic.AddDynamicInit(pushChannel);
									}
								}
							}
						}
					}
				);
			}
			// Create the command buttons
			Key editButtonText = null;
			EdtMode correspondingEditMode = EdtMode.Max;
			Key saveButtonText = null;
			switch (Mode) {
			case EdtMode.Edit:
			case EdtMode.New:
			case EdtMode.Clone:
				saveButtonText = KB.K("Save");
				break;
			case EdtMode.EditDefault:
				saveButtonText = KB.K("Save Defaults");
				break;
			case EdtMode.Delete:
				saveButtonText = KB.K("Delete");
				break;
			case EdtMode.UnDelete:
				saveButtonText = KB.K("Save restored record");
				break;
			case EdtMode.View:
				editButtonText = KB.K("Edit");
				correspondingEditMode = EdtMode.Edit;
				break;
			case EdtMode.ViewDefault:
				editButtonText = KB.K("Edit Defaults");
				correspondingEditMode = EdtMode.EditDefault;
				break;
			case EdtMode.ViewDeleted:
				editButtonText = KB.K("Restore");
				correspondingEditMode = EdtMode.UnDelete;
				break;
			}
			if (editButtonText != null) {
				// Create the Edit button that transitions to Edit mode.
				CommandButton editButton = new CommandButton(editButtonText, new CallDelegateCommand(
					delegate() {
						Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(null, TInfo, correspondingEditMode, new object[][] { new object[] {RecordId}}, null, null, ((IEditUI)this).Form, true, null);
					}
				));
				Controls.Add(editButton);
			}
			if (saveButtonText != null) {
				CommandButton saveButton = new CommandButton(saveButtonText, new CallDelegateCommand(
					delegate() {
						// TODO: Validate all the controls. Note that the above GetValue call will get an exception so we should execute the above
						// loop with a try/catch block and use any execptions to tag the invalid controls. This requires each control cell in the layout
						// table to have another cell to contain its status message, normally empty.
						// Save the changes
						RecordManager.SaveRecordSet(Libraries.DBILibrary.Server.UpdateOptions.Normal);
						// Redirect to View mode on the record just saved.
						Thinkage.Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().PerformMultiEdit(null, TInfo, EdtMode.View, new object[][] { new object[] { RecordId } }, null, null, ((IEditUI)this).Form, true, null);
					}
				));
				Controls.Add(saveButton);
			}

			ClientValueStore = PersistentData.CreateClientSide(this);

			// TODO: New Equates
			// TODO: EditorLogic.SetupInits();
			EditorLogic.EndSetup();
			// At this point we have to delay the rest of the work until OnInit since the RecordManagerState doesn't get its value until then.
		}
		#endregion
		#region - Control creation support
		#region -   LabelledControlPanel - a customized HTML table to show control labels and controls
		private class LabelledControlPanel : HtmlGenericControl {
			public LabelledControlPanel(Key[] ColumnHeaders)
				: base("table") {
				if (ColumnHeaders == null) {
					ColumnCount = 1;
					Body = this;
				}
				else {
					ColumnCount = (uint)ColumnHeaders.Length - 1;
					var head = new HtmlGenericControl("thead");
					Controls.Add(head);
					var row = new HtmlGenericControl("tr");
					head.Controls.Add(row);
					for (int i = 0; i <= ColumnCount; i++) {
						var cell = new HtmlGenericControl("td");
						if (ColumnHeaders[i] != null)
							cell.InnerText = ColumnHeaders[i].Translate();
						row.Controls.Add(cell);
					}
					Body = new HtmlGenericControl("tbody");
					Controls.Add(Body);
				}
			}
			private readonly uint ColumnCount;
			private readonly HtmlGenericControl Body;
			private HtmlGenericControl CurrentRow = null;
			public void AddControl(Key label, System.Web.UI.Control control) {
				if (CurrentRow == null) {
					CurrentRow = new HtmlGenericControl("tr");
					Body.Controls.Add(CurrentRow);

					var labelCell = new HtmlGenericControl("td");
					if (label != null)
						labelCell.InnerText = label.Translate();
					CurrentRow.Controls.Add(labelCell);
				}
				var cell = new HtmlGenericControl("td");
				if (control != null)
					cell.Controls.Add(control);
				CurrentRow.Controls.Add(cell);
				if (CurrentRow.Controls.Count > ColumnCount)
					CurrentRow = null;
			}
		}
		#endregion
		#region -   CreateGroupContent - method to create the appropriate HTML content for a tbl container node's contents
		private System.Web.UI.Control CreateGroupContent(EditLogic.ContainerNodeContent contents) {
			HtmlSequence result = null;
			LabelledControlPanel lcp = null;
			foreach (EditControlInfo eci in contents) {
				if (eci == null || !eci.InLine) {
					if (lcp == null)
						lcp = new LabelledControlPanel(contents.ColumnHeaders);

					if (eci == null)
						lcp.AddControl(null, null);
					else
						lcp.AddControl(eci.Label, eci.BareControl);
				}
				else {
					if (result == null)
						result = new HtmlSequence();
					if (lcp != null) {
						result.Controls.Add(lcp);
						lcp = null;
					}
					result.Controls.Add(eci.BareControl);
				}
			}
			if (lcp != null) {
				if (result == null)
					return lcp;
				result.Controls.Add(lcp);
			}
			return result;
		}
		#endregion
		#endregion
		#region - Tbl Traversal building browsette linkages
		protected delegate System.Web.UI.Control LeafNodeControlCreator(TblLeafNode node, ref Key label);
		protected class CreatedPanelContent {
			// This represents the content of a panel, i.e. a series of LCP rows intermixed with full-width entries.
			private readonly List<HtmlTableRow> Contents = new List<HtmlTableRow>();
			private readonly TblMultiColumnNode Formatting = null;
			private int CurrentColumnNumber = 0;
			#region Construction
			public CreatedPanelContent(TblContainerNode containerNode) {
				if (containerNode != null)
					Formatting = containerNode as TblMultiColumnNode;
			}
			public void AddNewInlineEntry(System.Web.UI.Control control) {
				CompleteRow();
				if (control != null) {
					HtmlTableCell c = new HtmlTableCell();
					c.Controls.Add(control);
					c.ColSpan = ColumnCount + 1;
					HtmlTableRow r = new HtmlTableRow();
					r.Controls.Add(c);
					Contents.Add(r);
				}
			}
			public void AddNewLCPEntry(Translation.Key label, System.Web.UI.Control control) {
				HtmlTableRow r;
				if (CurrentColumnNumber == 0) {
					// Create the new row and Add the label
					r = new HtmlTableRow();
					{
						HtmlTableCell c = new HtmlTableCell();
						if (label != null)
							c.InnerText = label.Translate();
#if SHOWCELLS
						else
							c.InnerText = "*E*";	// DEBUG ONLY
#endif
						r.Controls.Add(c);
					}
					Contents.Add(r);
				}
				else
					r = Contents[Contents.Count - 1];

				{
					// Add the cell contents
					HtmlTableCell c = new HtmlTableCell();
					if (control != null)
						c.Controls.Add(control);
#if SHOWCELLS
					else
						c.InnerText = "*E*";	// DEBUG ONLY
#endif
					r.Controls.Add(c);
				}
				if (++CurrentColumnNumber == ColumnCount) {
					// If this completes a row and all the controls in the row are null, drop the entire row.
					CurrentColumnNumber = 0;
					for (int i = 1; i <= ColumnCount; ++i)
						if (r.Controls[i].Controls.Count > 0)
							return;
					Contents.Remove(r);
				}
			}
			private void CompleteRow() {
				while (CurrentColumnNumber != 0)
					AddNewLCPEntry(null, null);
			}
			public void Merge(CreatedPanelContent other) {
				CompleteRow();
				Contents.AddRange(other.Contents);
			}
			#endregion
			#region Properties
			public bool CanMerge(CreatedPanelContent other) {
				return other.Formatting == null && Formatting == null;
			}
			private int ColumnCount {
				get {
					return Formatting == null ? 1 : Formatting.ColumnCount;
				}
			}
			public bool HasContent {
				get {
					CompleteRow();
					return Contents.Count > 0;
				}
			}
			#endregion
			#region CreatePanelControl - actually create a panel containing the specified contents.
			public HtmlTable CreatePanelControl() {
				CompleteRow();
				HtmlTable result = new HtmlTable();
				if (Formatting != null) {
					// Build the header row and put it in the Table.
					// Note that there is no special HTML for the header row, but the cells are <th> instead of <td>
					// TODO: Specially style the header row.
					// TODO: Use <thead> and <tbody>
					HtmlTableRow headerRow = new HtmlTableRow();
					headerRow.Controls.Add(new HtmlTableCell("th"));	// The labels column has no header
					for (int i = 0; i < Formatting.ColumnCount; i++) {
						HtmlTableCell c = new HtmlTableCell("th");
						c.InnerText = Formatting.ColumnHeaders[i].Translate();
						headerRow.Controls.Add(c);
					}
					result.Controls.Add(headerRow);
				}
				for (int i = 0; i < Contents.Count; i++)
					result.Controls.Add(Contents[i]);
				return result;
			}
			#endregion
		}
		protected static void TraverseLayoutNodesCreatingControls(CreatedPanelContent outputList, TblLayoutNodeArray nodes, TblLayoutNodeArray.PreviewNode preNode, LeafNodeControlCreator CreateLeafNodeControl) {
			for (int i = 0; i < nodes.Count; i++) {
				TblLayoutNode node = nodes.ColumnArray[i];
				if (!preNode(node))
					continue;
				TblLeafNode lnode = node as TblLeafNode;
				if (lnode != null) {
					Translation.Key label = node.Label;
					System.Web.UI.Control ctrl = CreateLeafNodeControl(lnode, ref label);
					outputList.AddNewLCPEntry(label, ctrl);
				}
				else {
					TblContainerNode contNode = (TblContainerNode)node;
					TblTabNode tnode = node as TblTabNode;
					TblSectionNode snode;
					if (tnode != null) {
						List<KeyValuePair<TblTabNode, CreatedPanelContent>> tabs = new List<KeyValuePair<TblTabNode, CreatedPanelContent>>();
						do {
							if (!preNode(tnode) || tnode.Visibility != Tbl.Visibilities.Visible)
								continue;
							CreatedPanelContent tabContent = new CreatedPanelContent(tnode);
							TraverseLayoutNodesCreatingControls(tabContent, tnode.Columns, preNode, CreateLeafNodeControl);
							if (tabContent.HasContent)
								tabs.Add(new KeyValuePair<TblTabNode, CreatedPanelContent>(tnode, tabContent));
						} while (++i < nodes.Count && (tnode = nodes.ColumnArray[i] as TblTabNode) != null);
						--i;
						if (tabs.Count == 0)
							continue;
						else if (tabs.Count == 1 && tabs[0].Key.MergeIfOnlySingleTab)
							// merge the contents of the tab into the current output list if possible, otherwise convert to a GroupBox labeled container
							MergeOrInlineGroup(outputList, tabs[0].Value, tabs[0].Key.Label);
						else
							// Treat each tab as a group box
							foreach (KeyValuePair<TblTabNode, CreatedPanelContent> tab in tabs)
								InlineGroup(outputList, tab.Value, tab.Key.Label);
					}
					else if ((snode = node as TblSectionNode) != null) {
						List<CreatedPanelContent> sections = new List<CreatedPanelContent>();
						do {
							if (!preNode(snode) || snode.Visibility != Tbl.Visibilities.Visible)
								continue;
							CreatedPanelContent sectionContent = new CreatedPanelContent(snode);
							TraverseLayoutNodesCreatingControls(sectionContent, snode.Columns, preNode, CreateLeafNodeControl);
							if (sectionContent.HasContent)
								sections.Add(sectionContent);
						} while (++i < nodes.Count && (snode = nodes.ColumnArray[i] as TblSectionNode) != null);
						--i;
						if (sections.Count == 0)
							continue;
						foreach (CreatedPanelContent sectionContents in sections)
							MergeOrInlineGroup(outputList, sectionContents, null);
					}
					else if (contNode.HasColumns) {
						CreatedPanelContent groupContent = new CreatedPanelContent(contNode);
						TraverseLayoutNodesCreatingControls(groupContent, contNode.Columns, preNode, CreateLeafNodeControl);
						if (!groupContent.HasContent)
							continue;
						// Switch on the type of container node and manually create the appropriate container ourselves for now.
						TblGroupNode gnode = contNode as TblGroupNode;
						if (gnode != null)
							InlineGroup(outputList, groupContent, gnode.Label);
						else
							// This includes groups built merely to set multicolumn layout.
							MergeOrInlineGroup(outputList, groupContent, null);
					}
				}
			}
		}
		private static void MergeOrInlineGroup(CreatedPanelContent outputList, CreatedPanelContent groupContents, Key groupLabel) {
			if (outputList.CanMerge(groupContents))
				outputList.Merge(groupContents);
			else
				InlineGroup(outputList, groupContents, groupLabel);
		}
		private static void InlineGroup(CreatedPanelContent outputList, CreatedPanelContent groupContents, Key groupLabel) {
			HtmlTable groupTable = groupContents.CreatePanelControl();
			if (groupLabel == null)
				outputList.AddNewInlineEntry(groupTable);
			else
				outputList.AddNewLCPEntry(groupLabel, groupTable);
		}
		#endregion
		#endregion
		#region OnInit
		protected override void OnInit(EventArgs e) {
			// Load the record.
			// Note: If this is a callback from an editing mode we must instead synthesize the record buffer from information in
			// the POSTDATA. This requires cooperation from the RecordManager since we must synthesize the records in the correct
			// order and/or suppress lookup fetches while synthesizing. For existing records we must preserve the Original values,
			// for all records we must preserve the current values. Since all records will have current values what we actually do
			// is preserve the current values and, on a column-by-column basis, the original values if they differ. For new-mode of
			// course, there are not Original values.
			// All of this means that ultimately we don't have to preserve the "original" control values of any bound controls;
			// after synthesizing the record, we copy the control values back to the record per the binding.
			// Set the state of all the Actions so they ignore any notifications they get.
			// This is equivalent to the SetupDataset code in EditControl.
			if (!Page.IsPostBack) {
				// This is an initial call to the editor (including coming from the Edit button of the View mode)
				EditorLogic.SuspendBeforeRecordOp();
				switch (Mode) {
				case EdtMode.Edit:
				case EdtMode.View:
				case EdtMode.Delete:
				case EdtMode.UnDelete:
				case EdtMode.ViewDeleted:
					RecordManager.EditRecordSet(new object[] { RecordId });
					break;
				case EdtMode.EditDefault:
				case EdtMode.ViewDefault:
					RecordManager.EditDefaultRecord();
					break;
				case EdtMode.Clone:
					RecordManager.CloneRecordSet(new object[] { RecordId });
					// TODO: Having obtained the record copies, we should now switch to New mode.
					break;
				case EdtMode.New:
					RecordManager.NewRecordSet();
					break;
				}
				EditorLogic.ResumeAfterRecordOp();
			}
			else {
				var store = new ValueStore(ClientValueStore.Data);
				uint[] controlValueIndices = (uint[])store[store.Count - 1];
				// TODO: Need to completely suspend all inits (not just suspend-pending)
				// Right now because we do *nothing at all* to them, they are just in Normal mode (enabled); our buffer load generates
				// no notifications so the controls start off null, then when the values come back in the form it looks like the user has
				// updated *all* the non-null controls, which copies the values back to the record so stuff appears to work.
				// This is a callback from a form we already posted containing saved state.
				// Recreate the RecordManager state from RecordManagerState.Value
				RecordManager.SetBufferFromValueStore(store, (uint)store[store.Count-2]);
				// Reload all the control values from client-saved state
				// In particular, load the mapping from TblLeafNodeInfo index to ValueStore index and place the latter values into the TblLeafNodeInfo entries.
				// Then fetch the individual values and place them in the controls.
				for (int i = controlValueIndices.Length; --i >= 0; )
					((CommonLogic.TblLayoutNodeInfo)EditorLogic.TblLeafNodeInfoCollection[i]).UncheckedControl.Value = store[controlValueIndices[i]];
				// TODO: Make all the inits enter their normal states.
			}

			// If the mode is Clone or New the record in the buffer has a new ID which we must fish out.
			// This will allow our Save command to call up View mode on the newly-saved record.
			switch (Mode) {
			case EdtMode.Clone:
			case EdtMode.New:
				RecordId = RecordManager.GetCurrentRecordIDs()[0];
				break;
			}
		}
		#endregion
		// No Page_Load handling, at that point we are in a limbo state where the controls have their new values but have not notified anyone.
		// After Page_Load and before PreRender, control change events occur (and our binding handles changes) and command button if any are
		// executed... some of these redirect to a new page. If not redirected we fall into our PreRender and Render.
		#region PreRender
		protected override void OnPreRender(EventArgs e) {
			base.OnPreRender(e);
			// Open up whichever picker control should be open (if any)
			int index = CurrentlyOpenPickerIndex;
			if (index >= 0) {
				BrowserControl ctrl = ((EditControlInfo)EditorLogic.TblLeafNodeInfoCollection[index]).BareControl as BrowserControl;
				if (ctrl == null)
					// Somehow we got confused (or a user had a stale URL and the Tbl interpretation has changed since then).
					// Just clear the currently open picker.
					CurrentlyOpenPicker.Value = null;
				else
					ctrl.Mode = BrowserControl.BrowserModes.OpenPicker;
			}
			((TblPage)Page).SetTitles(EditorLogic.TitleText);
			if (Browsettes.Count > 0) {
				string masterXID = EditorLogic.TitleText;	// TODO: This is not quite what is required here.
				foreach (CallBrowsetteControl cbc in Browsettes)
					cbc.SetXID(masterXID);
			}

			// Save away the state information.
			// We save the RecordManager buffer contents, and the control original values (so we can get proper Notifications)
			var store = new ValueStore();
			uint rmRootIndex = RecordManager.GetBufferIntoValueStore(store);

			// Loop over all the TblLeafNodeInfo entries; for each fetch the control value, place it in the value store
			// and put the index into the entry. Then build a directory of leaf-node index to value store index and place that in the store as well.
			uint[] indices = new uint[EditorLogic.TblLeafNodeInfoCollection.Count];
			for (int i = EditorLogic.TblLeafNodeInfoCollection.Count; --i >= 0; ) {
				EditControlInfo eci = (EditControlInfo)EditorLogic.TblLeafNodeInfoCollection[i];
				object value = eci.UncheckedControl.ValueStatus == null ? eci.UncheckedControl.Value : null;
				indices[i] = eci.OriginalValueStoreIndex = store.Add(value);	// TODO: Better handling of non-null ValueStatus???
			}
			// Put indices into the store and stream the store to client-side persistent storage.
			// Note that these indices is always the last items in the store so on callback we can find it by indexing back from Count.
			store.AddUnique(rmRootIndex);
			store.AddUnique(indices);
			ClientValueStore.Data = store.StreamedData;
		}
		#endregion
		#region Destruction
		public override void Dispose() {
			if (EditorLogic != null) {
				EditorLogic.Dispose();
				EditorLogic = null;
			}
		}
		#endregion
		#endregion
		#region Members
		protected EditLogic EditorLogic;
		protected readonly Tbl TInfo;
		protected EdtMode Mode { get { return EditorLogic.EditMode; } }
		protected object RecordId;
		private XAFDataSet dataSet { get { return EditorLogic.DataSet; } }
		protected RecordManager RecordManager { get { return EditorLogic.RecordManager; } }
		private PersistentData ClientValueStore = null;
		private readonly List<CallBrowsetteControl> Browsettes = new List<CallBrowsetteControl>();
		private HiddenField CurrentlyOpenPicker;
		private int CurrentlyOpenPickerIndex {
			get {
				if (string.IsNullOrEmpty(CurrentlyOpenPicker.Value))
					return -1;
				return Int32.Parse(CurrentlyOpenPicker.Value);
			}
		}
		#endregion
		#region client-side value storage for control value persistence
		private class ValueStore : Presentation.RecordManager.IValueStore {
			public ValueStore() {
				// Give null the index zero.
				Values.Add(null);
			}
			public ValueStore(byte[] streamedData) {
				// This leaves the dictionary empty, essentially treating all values as from 'AddUnique' calls.
				Values.Add(null);
				// TODO: Parse the streamedData, adding each value in turn to Values.
				uint offset = 0;
				while (offset < streamedData.Length)
					Values.Add(GetFromStream(streamedData, ref offset));
			}
			private static object GetFromStream(Byte[] streamedData, ref uint offset) {
				TypeCode code = (TypeCode)(streamedData[offset] % MaxTypeCode);
				uint length = (uint)streamedData[offset++] / (uint)MaxTypeCode;
				switch (code) {
				case TypeCode.SByte:
					return (SByte)GetSignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.Int16:
					return (Int16)GetSignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.Int32:
					return (Int32)GetSignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.Int64:
					return (Int64)GetSignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.Byte:
					return (Byte)GetUnsignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.UInt16:
					return (UInt16)GetUnsignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.UInt32:
					return (UInt32)GetUnsignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.UInt64:
					return (UInt64)GetUnsignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.Char:
					return (Char)GetUnsignedIntegerFromStream(streamedData, ref offset, length);
				case TypeCode.Boolean:
					return length != 0;
				case TypeCode.String:
					var ulen = GetUnsignedIntegerFromStream(streamedData, ref offset, length);
					Char[] buffer = new Char[ulen];
					if (ulen > 0) {
						ulen *= 2;
						unsafe {
							fixed (char* p = buffer) {
								Byte* bp = (Byte*)p;
								do {
									*bp++ = streamedData[offset++];
								} while (--ulen > 0);
							}
						}
					}
					return new string(buffer);
				case TypeCode.Object:
					// This includes array of int, which we want to be able to encode. We look at the Reflection information to see
					// if it is indeed an array type, and if so, what the element type is. Actually for now we just recursively encode the
					// elements, which is somewhat wasteful of space (since we are repeating the type for each element), but avoids having to build
					// a different encoding to use when the TypeCode is known in advance.
					// TODO: This has the problem that if the array contains no elements we have no way of knowing what the element type is and thus will
					// be unable to re-create the array on callback.
					ulong ualen = GetUnsignedIntegerFromStream(streamedData, ref offset, length);
					Type elementType;
					// TODO: Is there a method to do the following?
					switch ((TypeCode)streamedData[offset++]) {
					case TypeCode.Boolean:
						elementType = typeof(Boolean);
						break;
					case TypeCode.SByte:
						elementType = typeof(SByte);
						break;
					case TypeCode.Int16:
						elementType = typeof(Int16);
						break;
					case TypeCode.Int32:
						elementType = typeof(Int32);
						break;
					case TypeCode.Int64:
						elementType = typeof(Int64);
						break;
					case TypeCode.Byte:
						elementType = typeof(Byte);
						break;
					case TypeCode.UInt16:
						elementType = typeof(UInt16);
						break;
					case TypeCode.UInt32:
						elementType = typeof(UInt32);
						break;
					case TypeCode.UInt64:
						elementType = typeof(UInt64);
						break;
					case TypeCode.Char:
						elementType = typeof(Char);
						break;
					case TypeCode.String:
						elementType = typeof(String);
						break;
					default:
						throw new NotImplementedException();
					}
					Array result = Array.CreateInstance(elementType, (Int64)ualen);
					for (uint ix = 0; ix < ualen; ix++)
						result.SetValue(GetFromStream(streamedData, ref offset), ix);
					return result;
				default:
					throw new NotImplementedException();
				}
			}
			private static Int64 GetSignedIntegerFromStream(Byte[] streamedData, ref uint offset, uint length) {
				Int64 result = 0;
				if (length > 0 && (streamedData[offset] & 0x80) != 0)
					result = -1L;
				while (length-- > 0)
					result = (result << 8) | streamedData[offset++];
				return result;
			}
			private static UInt64 GetUnsignedIntegerFromStream(Byte[] streamedData, ref uint offset, uint length) {
				UInt64 result = 0;
				while (length-- > 0)
					result = (result << 8) | streamedData[offset++];
				return result;
			}
			public Byte[] StreamedData {
				get {
					var result = new List<Byte>();
					// TODO: Iterate through Values starting at element 1, appending each value's byte stream to result.
					// We don't use ISerializable because its results are waaaay too bulky for our purposes.
					// We also can't use XAF type support because we are treating the values as native CLR types without reference to any
					// TypeInfo-defined interpretation they might have.
					for (int i = 1; i < Values.Count; i++)
						AddToStream(result, Values[i]);
					return result.ToArray();
				}
			}
			private void AddToStream(List<Byte> stream, object o) {
				if (o.GetType().IsEnum)
					// TODO: How do enum types come across???
					throw new NotImplementedException();
				switch (Type.GetTypeCode(o.GetType())) {
				case TypeCode.SByte:
					AddSignedIntegerToStream(stream, TypeCode.SByte, (SByte)o);
					break;
				case TypeCode.Int16:
					AddSignedIntegerToStream(stream, TypeCode.Int16, (Int16)o);
					break;
				case TypeCode.Int32:
					AddSignedIntegerToStream(stream, TypeCode.Int32, (Int32)o);
					break;
				case TypeCode.Int64:
					AddSignedIntegerToStream(stream, TypeCode.Int64, (Int64)o);
					break;
				case TypeCode.Byte:
					AddUnsignedIntegerToStream(stream, TypeCode.Byte, (Byte)o);
					break;
				case TypeCode.UInt16:
					AddUnsignedIntegerToStream(stream, TypeCode.UInt16, (UInt16)o);
					break;
				case TypeCode.UInt32:
					AddUnsignedIntegerToStream(stream, TypeCode.UInt32, (UInt32)o);
					break;
				case TypeCode.UInt64:
					AddUnsignedIntegerToStream(stream, TypeCode.UInt64, (UInt64)o);
					break;
				case TypeCode.Char:
					AddUnsignedIntegerToStream(stream, TypeCode.Char, (Char)o);
					break;
				case TypeCode.Boolean:
					stream.Add((Byte)(TypeCode.Boolean + MaxTypeCode * ((bool)o ? 0 : 1)));
					break;
				case TypeCode.String:
					var oString = (string)o;
					var ulen = (UInt32)oString.Length;
					// We treat the len as unsigned as far as encoding goes. The value cannot actually be negative.
					// Something we could do here to save space would be to encode the original string as UTF-8 so we get a string containing
					// *only* 8-bit characters and just stream out one Byte per character. The length we put out would be the length of the UTF-8
					// encoding. We would then not need the block of unsafe code, as well. Actually we don't need it anyway, since we can just use
					// shift operations to pick apart the char values into two bytes. This is however not advantageous for non-ansi text because UTF-8
					// can be quite a bit larger. I suppose we could do both, pick the shorter, and steal a bit from the length to encode which way we did it.
					AddUnsignedIntegerToStream(stream, TypeCode.String, ulen);
					ulen *= sizeof(char);
					unsafe {
						fixed (char* p = oString.ToCharArray()) {
							Byte* bp = (Byte*)p;
							for (int i = 0; i < ulen; i++)
								stream.Add(bp[i]);
						}
					}
					break;
				case TypeCode.Object:
					// This includes array of int, which we want to be able to encode. We look at the Reflection information to see
					// if it is indeed an array type, and if so, what the element type is. Actually for now we just recursively encode the
					// elements, which is somewhat wasteful of space (since we are repeating the type for each element), but avoids having to build
					// a different encoding to use when the TypeCode is known in advance.
					// TODO: This has the problem that if the array contains no elements we have no way of knowing what the element type is and thus will
					// be unable to re-create the array on callback.
					if (o.GetType().IsArray) {
						if (o.GetType().GetArrayRank() == 1) {
							if (((Array)o).GetLowerBound(0) == 0) {
								TypeCode elementCode = Type.GetTypeCode(o.GetType().GetElementType());
								switch (elementCode) {
								case TypeCode.Boolean:
								case TypeCode.SByte:
								case TypeCode.Int16:
								case TypeCode.Int32:
								case TypeCode.Int64:
								case TypeCode.Byte:
								case TypeCode.UInt16:
								case TypeCode.UInt32:
								case TypeCode.UInt64:
								case TypeCode.Char:
								case TypeCode.String:
									uint ualen = (uint)((Array)o).GetUpperBound(0) + 1;
									AddUnsignedIntegerToStream(stream, TypeCode.Object, ualen);
									stream.Add((Byte)elementCode);	// so we know the declared type of the array
									for (uint ix = 0; ix < ualen; ix++)
										AddToStream(stream, ((Array)o).GetValue(ix));
									return;
								}
								break;
							}
						}
					}
					throw new NotImplementedException();
				default:
					throw new NotImplementedException();
				}
			}
			private void AddSignedIntegerToStream(List<Byte> stream, TypeCode code, Int64 value) {
				int shift = 64;
				while ((shift -= 8) >= 0)
					if ((value >> shift) != 0L && (shift == 0 || (value >> (shift-1)) != -1L))
						break;
				shift += 8;
				stream.Add((Byte)(code + MaxTypeCode * shift / 8));
				while ((shift -= 8) >= 0)
					stream.Add((Byte)(value >> shift));
			}
			private void AddUnsignedIntegerToStream(List<Byte> stream, TypeCode code, UInt64 value) {
				int shift = 64;
				while ((shift -= 8) >= 0)
					if ((value >> shift) != 0)
						break;
				shift += 8;
				stream.Add((Byte)(code + MaxTypeCode * shift/8));
				while ((shift -= 8) >= 0)
					stream.Add((Byte)(value >> shift));
			}
			private unsafe void AddBytesToStream(List<Byte> stream, TypeCode code, Byte* bytes, int count) {
				stream.Add((Byte)(code + MaxTypeCode * count));
				while (--count >= 0)
					stream.Add(bytes[count]);
			}
			static ValueStore() {
				MaxTypeCode = 0;
				foreach (int v in Enum.GetValues(typeof(TypeCode)))
					if (v > MaxTypeCode)
						MaxTypeCode = v + 1;
			}
			private static readonly int MaxTypeCode;
			private Dictionary<object, uint> Directory = new Dictionary<object, uint>();
			private List<object> Values = new List<object>();
			public uint Add(object o) {
				if (o == null)
					return 0;
				uint result;
				if (!Directory.TryGetValue(o, out result)) {
					result = (uint)Values.Count;
					Directory.Add(o, result);
					Values.Add(o);
				}
				return result;
			}
			public uint AddUnique(object o) {
				uint result = (uint)Values.Count;
				Values.Add(o);
				return result;
			}
			public object this[uint index] {
				get {
					return Values[(int)index];
				}
			}
			public uint Count {
				get {
					return (uint)Values.Count;
				}
			}
		}
		#endregion
		#region IEditUI Members
		public void SetInitialFocus() {
		}

		UIForm ICommonUI.Form {
			get { return null /*this.Page*/; }
		}

		public bool HandleConcurrencyError(XAFClient db, System.Data.DBConcurrencyException e) {
			return false;
		}

		public void MarkControlWarnings(System.Exception ex) {
		}

		public void Print() {
			throw new NotImplementedException();
		}

		public void PrintNow()
		{
			throw new NotImplementedException();
		}

		public bool CloseEditor() {
			throw new NotImplementedException();
		}

		public DataFlow.NotifyingSource GetControlNotifyingSource(object Id, out bool needContext) {
			throw new NotImplementedException();
		}
		public DataFlow.NotifyingSource GetControlUncheckedNotifyingSource(object Id, out bool needContext) {
			throw new NotImplementedException();
		}
		public DataFlow.Sink GetControlReadonlySink(object id, Translation.Key readonlyMessage) {
			throw new NotImplementedException();
		}
		public DataFlow.Sink GetSubBrowserSink(object Id, BrowserInitTarget valInSubBrowser) {
			throw new NotImplementedException();
		}
		public DataFlow.Sink GetControlSink(object id) {
			throw new NotImplementedException();
		}

		public EditLogic.TblLayoutNodeInfoBase CreateGroupBox(EditLogic.ContainerNodeContent contents, Key labelContext) {
			HtmlGenericControl groupBox = new HtmlGenericControl("div");
			if (contents.Node.Label != null) {
				HtmlGenericControl title = new HtmlGenericControl("h3");
				title.InnerText = contents.Node.Label.Translate();
				groupBox.Controls.Add(title);
			}
			groupBox.Controls.Add(CreateGroupContent(contents));
			return CreateContainerResult(contents.Node, groupBox);
		}

		public EditLogic.TblLayoutNodeInfoBase CreateTabControl(List<CommonLogic.ContainerNodeContent> tabList, Key labelContext) {
			HtmlGenericControl tabBox = new HtmlGenericControl("div");
			foreach (CommonLogic.ContainerNodeContent contents in tabList) {
				HtmlGenericControl title = new HtmlGenericControl("h3");
				title.InnerText = contents.Node.Label.Translate();
				HtmlGenericControl tabPageBox = new HtmlGenericControl("div");
				tabPageBox.Controls.Add(title);
				tabPageBox.Controls.Add(CreateGroupContent(contents));
				tabBox.Controls.Add(tabPageBox);
			}
			return CreateContainerResult(null, tabBox);
		}

		public EditLogic.TblLayoutNodeInfoBase CreateSplitterGroup(List<CommonLogic.ContainerNodeContent> pageList) {
			HtmlGenericControl splitterBox = new HtmlGenericControl("div");
			foreach (EditLogic.ContainerNodeContent contents in pageList) {
				HtmlGenericControl splitterPageBox = new HtmlGenericControl("div");
				splitterPageBox.Controls.Add(CreateGroupContent(contents));
				splitterBox.Controls.Add(splitterPageBox);
			}
			return CreateContainerResult(null, splitterBox);
		}

		public void AddSplitterSection(EditLogic.TblLayoutNodeInfoBase tabControlNodeInfo, EditLogic.ContainerNodeContent contents) {
			HtmlGenericControl groupBox = new HtmlGenericControl("div");
			groupBox.Controls.Add(CreateGroupContent(contents));
			((EditControlInfo)tabControlNodeInfo).BareControl.Controls.Add(groupBox);
		}

		public EditLogic.TblLayoutNodeInfoBase CreateGroup(EditLogic.ContainerNodeContent contents) {
			HtmlGenericControl groupBox = new HtmlGenericControl("div");
			groupBox.Controls.Add(CreateGroupContent(contents));
			return CreateContainerResult(contents.Node, groupBox);
		}
		public EditLogic.TblLayoutNodeInfoBase CreateRow(EditLogic.ContainerNodeContent contents, Key labelContext) {
			// TODO: Return some special TblLayoutNodeInfo which CreateContainerResult recognizes and turns into a single row in the table.
			return CreateGroup(contents);
		}
		private EditLogic.TblLayoutNodeInfoBase CreateContainerResult(TblContainerNode lnode, System.Web.UI.Control geometricControl) {
			EditControlInfo ci = new EditControlInfo(lnode);
			ci.BareControl = geometricControl;
			ci.InLine = true;
			return ci;
		}
		private int NextControlIndex() {
			return EditorLogic.TblLeafNodeInfoCollection.Count;
		}
		private string NextControlId() {
			return Strings.IFormat("a{0}_", NextControlIndex());
		}
		public EditLogic.TblLayoutNodeInfoBase CreateControl(TblLeafNode leafNode, TypeInfo.TypeInfo referencedType, Fmt fmt, Settings.Container settingsContainer, MultiDisablerIfAllEnabled writeableDisabler, MultiDisablerIfAllEnabled enabledDisabler, Key labelContext, Key labelSuffix = null) {
			Key label = leafNode.Label;
			IBasicDataControl valueCtrl = null;
			System.Web.UI.Control geometricCtrl = null;
			TypeInfo.LinkedTypeInfo linkedType = referencedType as TypeInfo.LinkedTypeInfo;
			if (writeableDisabler != null) {
				TypeEditTextHandler teth = referencedType.GetTypeEditTextHandler(Thinkage.Libraries.Application.InstanceCultureInfo);
				if (teth != null) {
					var ctrl = new TextEdit(teth);
					valueCtrl = ctrl;
					geometricCtrl = ctrl;
				}
				else if (linkedType != null) {
					Tbl linkedTbl = Fmt.GetPickFrom(fmt);
					if (linkedTbl.Visibility == Tbl.Visibilities.Visible) {
						// For now we create all the pickers closed. In PreRender we will see if one of them should be open.
						XAFClient db = TblApplication.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
						CustomSessionTbl customSessionAttrib = CustomSessionTbl.Find(fmt);
						bool takeDBCustody = false;
						if (customSessionAttrib != null)
							db = customSessionAttrib.CreateCustomSession(db, linkedTbl.Schema.Database, out takeDBCustody);
						BrowserControl ctrl = new BrowserControl(db, takeDBCustody, linkedTbl, BrowseLogic.BrowseOptions.NoSecondaries, BrowserControl.BrowserModes.ClosedPicker, referencedType, NextControlId());
						int controlsIndex = NextControlIndex();
						ICommand cmd = ctrl.CommandEnabledOnlyWhenClosed(new CallDelegateCommand(
							delegate() {
								// Change the state variable that contains the currently-open picker
								CurrentlyOpenPicker.Value = controlsIndex.ToString();
							}
						));
						ctrl.BrowserLogic.Commands.AddCommand(KB.K("Change"), null, cmd, cmd);
						cmd = ctrl.CommandEnabledOnlyWhenOpen(new CallDelegateCommand(
							delegate() {
								CurrentlyOpenPicker.Value = null;
							}
						));
						ctrl.BrowserLogic.Commands.AddCommand(KB.K("Select"), null, cmd, cmd);
						valueCtrl = ctrl;
						geometricCtrl = ctrl;
					}
				}
			}
			else if (Fmt.GetShowReferences(fmt) != null)
				return null;
			else {
				if (linkedType != null) {
					Tbl linkedTbl = Fmt.GetPickFrom(fmt);
					if (linkedTbl.Visibility == Tbl.Visibilities.Visible) {
						XAFClient db = TblApplication.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session;
						CustomSessionTbl customSessionAttrib = CustomSessionTbl.Find(fmt);
						bool takeDBCustody = false;
						if (customSessionAttrib != null)
							db = customSessionAttrib.CreateCustomSession(db, linkedTbl.Schema.Database, out takeDBCustody);
						BrowserControl disp = new BrowserControl(db, takeDBCustody, linkedTbl, BrowseLogic.BrowseOptions.NoSecondaries, BrowserControl.BrowserModes.Display, referencedType, NextControlId());
						valueCtrl = disp;
						geometricCtrl = disp;
					}
				}
				if (valueCtrl == null) {
					TextDisplay disp = new TextDisplay(referencedType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo));
					valueCtrl = disp;
					geometricCtrl = disp;
				}
			}
			if (valueCtrl == null) {
				System.Diagnostics.Debug.Assert(geometricCtrl == null, "EditorControl.CreateControl returned a UI object with no associated value object");
				return null;
			}
			// If the custom create wants no UI it should return no value either, and let EditLogic create a HiddenControl. There is no room in this town
			// for a custom hidden control class.
			System.Diagnostics.Debug.Assert(geometricCtrl != null, "EditorControl.CreateControl returned a value object with no associated UI object");
			EditControlInfo result = new EditControlInfo(leafNode);
			result.UncheckedControl = valueCtrl;
			// TODO: What if is is the equivalent to a BrowsetteWrapper? (a CallBrowsetteCommand or something like that)
			if (valueCtrl is BrowserControl)
				result.BrowserLogic = ((BrowserControl)valueCtrl).BrowserLogic;

			result.Label = label;
			result.InLine = false;
			result.BareControl = geometricCtrl;
			return result;
		}

		public EditLogic.TblLayoutNodeInfoBase CreateInfoForHiddenControl(TblLeafNode leafNode, Thinkage.Libraries.XAF.UI.IBasicDataControl dummyControl) {
			EditControlInfo result = new EditControlInfo(leafNode);
			result.UncheckedControl = dummyControl;

			result.Label = leafNode.Label;
			result.InLine = false;
			result.BareControl = null;
			return result;
		}
		public EditLogic.TblLayoutNodeInfoBase CreateInfoForEmptyControl(TblLeafNode leafNode) {
			EditControlInfo result = new EditControlInfo(leafNode);
			result.Label = leafNode.Label;
			result.InLine = false;
			result.BareControl = null;
			return result;
		}
		#endregion

		#region ICommonUI Members


		public CommonLogic LogicObject {
			get { throw new NotImplementedException(); }
		}

		#endregion

		public void ProcessDelayedControlNotifications() {
		}
	}
}
