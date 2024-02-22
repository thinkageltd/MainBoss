using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI.HtmlControls;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.Collections;

// TODO: This generates __VIEWSTATE and some other state junk that I want to avoid having. Investigate why it is being created.
// The length of the __VIEWSTATE seems roughly proportional to the number of records in the browse data; it would appear that adding all those
// <tr> elements for the list data is causing them to appear.

namespace Thinkage.Libraries.Presentation.ASPNet {
	// TODO: Have this class automatically generate the std browser commands based on the Mode, instead of BrowsePage doing it.
	public class BrowserControl : HtmlSequence, IBrowseUI, IBasicDataControl {
		private class ColSourceWrapper : IListSortColumn {
			public ColSourceWrapper(DataFlow.Source s) {
				Source = s;
			}
			public readonly DataFlow.Source Source;
		}
		#region Construction and setup (OnLoad)
		public BrowserControl(XAFClient db, bool takeDBCustody, Tbl tbl, BrowseLogic.BrowseOptions structure, BrowserModes mode, TypeInfo.TypeInfo valueType, [Invariant] string idPrefix) {
			pTypeInfo = valueType;
			// The following limits but does not entirely eliminate the __VIEWSTATE data in the form. It at least keeps it to more or less constant length.
			EnableViewState = false;
			Prefix = idPrefix;
			pMode = mode;

			BrowserLogic = BrowseLogic.Create(this, db, takeDBCustody, tbl, null, structure);

			BrowserLogic.CreateTblCodedFilters();
			// Create the SelectionFilter of the form tinfo.Schema.InternalId = (param)
			SelectionFilter = new BrowseLogic.PathValueFilter(BrowserLogic.TblSchema.InternalId, false);
			SelectionFilter.DontClone = true;
			SelectionFilterEnabler = new BrowseLogic.DisableableFilter(SelectionFilter, false);
			// Now that the SelectionFilterEnabler exists we can set Mode properly.
			Mode = pMode;
			BrowserLogic.AddGlobalFilter(SelectionFilterEnabler);

			BrowserLogic.OnlyIncludeActiveRecords = true;	// for now, active records only.

			// Traverse the Tbl setting up the list columns.
			BrowserLogic.CreateListAndColumns(false);

			List = new HtmlGenericControl("table");
			Controls.Add(List);
			Body = new HtmlGenericControl("tbody");
			List.Controls.Add(Body);

			SelectionButtonGroup = new HtmlRadioButtonSet(TypeInfo);
			SelectionButtonGroup.Name = Prefix + SelectionButtonGroupName;
			Controls.Add(SelectionButtonGroup);
			SelectionButtonGroup.Notify += new DataFlow.Notification(SelectionControl_ValueChanged);
			// Until render time we just put the command holder in-line so the command buttons contained in it can generate
			// events. In OnPreRender, we move this to its proper location.
			Controls.Add(CommandHolder);

			// If type info is nullable give user a way to clear the value
			if (IsPicker && TypeInfo.AllowNull) {
				ClearCommand = new CallDelegateCommand(KB.K("Clear the selection"), KB.K("The value is already null"),
					delegate() {
						Value = null;
					},
					SelectionButtonGroup.ValueStatus != null || SelectionButtonGroup.GetValue() != null
				);
				ICommand cmd = CommandEnabledOnlyWhenClosed(ClearCommand);
				BrowserLogic.Commands.AddCommand(KB.K("Clear"), null, cmd, cmd);
			}
			switch (pMode) {
			case BrowserModes.Browse:
				BrowserLogic.CreateLocalNewCommands(true, null);
				BrowserLogic.CreateCustomCommands();
				BrowserLogic.CreateViewCommand(BrowserLogic.Commands);
				break;
			case BrowserModes.ClosedPicker:
			case BrowserModes.OpenPicker:
				// These two modes must define the same commands, although we can apply WhenOpenEnabler or WhenClosedEnabler to control which ones
				// appear in the finished form. We would do this by passing a DisablerAdder to CreateXxxxCommands.
				// TODO: Some way of making these call up a new browser (needs client-side script to set and later restore the <form> element target
				// attribute in the button's onclick handler, or all submit buttons must script to set the form target as desired)
				BrowserLogic.CreateLocalNewCommands(true, null);
				BrowserLogic.CreateCustomCommands();
				BrowserLogic.CreateViewCommand(BrowserLogic.Commands);
				break;
			case BrowserModes.Display:
				BrowserLogic.CreateViewCommand(BrowserLogic.Commands);
				break;
			}
		}
		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			AddCommandButtons();
			BrowserLogic.ControlCreationCompleted();
			// Move the data to the selection so that any commands that might execute have the correct context.
			// TODO: Note that:
			// (a) this is not necessary if the command is context-free
			// (b) if we know there is a context command we could enable the selection filter so only the selected record is fetched
			// (c) we only have to do a full fetch if there is no command or the command fails.
			// One problem is that we have no way of knowing if a command is pending, and if so whether it is contextual. If it is the context
			// must be set before executing the command so its disabler can act appropriately.
			// If we knew we had a commands *and* it needed context, we could enable the selection filter and do a fetch here.
			// Perhaps it might be possible to wrap the commands as we build the command buttons, with a wrapper that echoes the Enabled status and tips of the "real"
			// command but whose Execute call fetches and positions the data before Executing the original command. This would bypass the bug mentioned below because
			// by the time the command is called (by a Click event) the SelectionButtonGroup will have notified, thus properly setting up the SelectionFilter's Value.
			// This still leaves the problem of *recognizing* contextual commands as we build the buttons. I suppose for now we could wrap them all.
			// The PreRender could optionally (based on Mode) disable the selection filter. Thus we only do a double-fetch (once for the selected
			// record and later for all records) if the command does not cause a transfer to another URL.
			// TODO (bug): At this point if this is a callback and the user has changed the selection, the Get[Nullable]Value call will return the new value,
			// but the control has not notified yet (happens between OnLoad and OnPreRender), and as a result SelectionId and more importantly SelectionFilter's Value
			// do not reflect the new selection. Thus if the SelectionFilter is enabled the record we try to move to will not be in the data.
			// Somehow the duplicity of SelectionId, SelectionFilter's Value, SelectionButtonGroup's Value, and BrowserLogic's CurrentPosition must be reconciled.
			SyncData();
		}
		#endregion
		#region Properties/members
		private string Prefix;
		private static readonly string SelectionButtonGroupName = "RowSelectButtons";
		#region - browser Mode and its dependent properties
		public enum BrowserModes { Browse, OpenPicker, ClosedPicker, Display }
		public BrowserModes Mode {
			set {
				// TODO: Allow initial setting of Mode by the ctor, but subsequent mode changes should not be allowed except between
				// OpenPicker and ClosedPicker.
				pMode = value;
				switch (pMode) {
				case BrowserModes.ClosedPicker:
				case BrowserModes.Display:
					HeaderOption = HeaderOptions.None;
					ForceDummyRow = true;
					SelectionFilterEnabler.SetValue(true);
					break;
				case BrowserModes.OpenPicker:
				case BrowserModes.Browse:
					HeaderOption = HeaderOptions.Always;
					ForceDummyRow = false;
					SelectionFilterEnabler.SetValue(false);
					break;
				}
			}
		}
		private BrowserModes pMode;
		private bool IsPicker {
			get {
				return pMode == BrowserModes.OpenPicker || pMode == BrowserModes.ClosedPicker;
			}
		}
		private bool IsSelectable {
			get {
				return pMode == BrowserModes.OpenPicker || pMode == BrowserModes.Browse;
			}
		}
		protected enum HeaderOptions { None, Always, OnlyWithMultipleRows, OnlyWithRows }
		// The following (HeaderOption and ForceDummyRow) are set by the Mode accessor, but they could instead be crackers like
		// IsPicker and IsSelectable.
		protected HeaderOptions HeaderOption = HeaderOptions.None;
		protected bool ForceDummyRow = true;
		#endregion
		#region - UI and command elements
		private HtmlRadioButtonSet SelectionButtonGroup;
		private HtmlSequence CommandHolder = new HtmlSequence();
		private HtmlGenericControl List;
		private HtmlGenericControl Body;
		private CallDelegateCommand ClearCommand = null;
		#endregion
		#region - Disablers
		// The following enablers are both enabled under we start PreRender. This allows commands under their control
		// to work on postback even though the control has not been correctly set open/closed by its owner.
		private SettableDisablerProperties WhenOpenEnabler = new SettableDisablerProperties(null, KB.K("This picker control is closed"), true);
		private SettableDisablerProperties WhenClosedEnabler = new SettableDisablerProperties(null, KB.K("This picker control is open"), true);
		#endregion
		#region - Data transfer/list column elements
		private List<DataFlow.Source> ColumnSources = new List<DataFlow.Source>();
		private List<DataFlow.Source> ColumnHiddenSources = new List<DataFlow.Source>();
		private List<Key> ColumnHeaders = new List<Key>();
		#endregion
		#region - Browser Data
		public readonly BrowseLogic BrowserLogic;
		private BrowseLogic.ParameterizedFilter SelectionFilter;
		private BrowseLogic.DisableableFilter SelectionFilterEnabler;
		private object UncheckedValue;
		private bool KeepDataSynched = false;
		#endregion
		#endregion
		#region Browser Command handling
		public ICommand CommandEnabledOnlyWhenOpen(ICommand underlying) {
			return new MultiCommandIfAllEnabled(underlying, WhenOpenEnabler);
		}
		public ICommand CommandEnabledOnlyWhenClosed(ICommand underlying) {
			return new MultiCommandIfAllEnabled(underlying, WhenClosedEnabler);
		}
		private class CommandButtonInfo {
			public CommandButtonInfo(CommandButton b, ICommand cmd) {
				Button = b;
				Command = cmd;
			}
			public readonly CommandButton Button;
			public readonly ICommand Command;
			public bool SometimesEnabled = false;
			// NOTE: SometimesDisabled is not currently used, but in the future we could use it to elide never-disabled
			// commands from the enabler mask thus allowing more command buttons.
			public bool SometimesDisabled = false;
		}
		private List<CommandButtonInfo> CommandButtonInfoList = new List<CommandButtonInfo>();
		/// <summary>
		/// The owner must call AddCommandButtons *after* it has added all its custom commands to Commands
		/// but *before* the Load event ends (?) so that the buttons will click properly on postback.
		/// </summary>
		private void AddCommandButtons() {
			foreach (CommonLogic.CommandNode.Entry e in BrowserLogic.Commands.Nodes) {
				if (e.Subnodes != null) {
					// For now render just as for single buttons.
					// TODO: Make a group frame with a caption.
					foreach (CommonLogic.CommandNode.Entry sube in e.Subnodes.Nodes)
						AddCommandButton(sube);
				}
				else
					AddCommandButton(e);
			}
		}
		private void AddCommandButton(CommonLogic.CommandNode.Entry e) {
			// Because we move the button around at render time we must give it a specific ID so the ID is the same
			// at event time as it is when rendering.
			CommandButton b = new CommandButton(e.Caption, e.ContextualCommand);
			b.ID = Prefix + "C_" + CommandHolder.Controls.Count.ToString();
			CommandHolder.Controls.Add(b);
			CommandButtonInfoList.Add(new CommandButtonInfo(b, e.ContextualCommand));
		}
		#endregion
		#region PreRender handling
		#region - OnPreRender
		private void SetBrowseContextRecordId(object selectionId) {
			var contextIds = new Set<object>(BrowserLogic.BrowserDataPositioner.IdType);
			if (selectionId != null)
				contextIds.Add(selectionId);
			BrowserLogic.SetBrowseContextRecordIds(contextIds);
		}
		protected override void OnPreRender(EventArgs e) {
			WhenOpenEnabler.Enabled = pMode == BrowserModes.OpenPicker;
			WhenClosedEnabler.Enabled = pMode == BrowserModes.ClosedPicker;
			base.OnPreRender(e);
			BrowserLogic.KeepUpToDate(true);
			if (UncheckedValue != null && !BrowserLogic.IsValidRecordId(UncheckedValue))
				Value = null;
			BrowserLogic.IterateOverAllRecords(
				delegate(object rowId) {
					SetBrowseContextRecordId(rowId);
					if (pMode == BrowserModes.Browse && UncheckedValue == null)
						Value = rowId;

					// Populate the table row by creating one <tr> element.
					// The MS classes do not support <tbody> etc but HTML makes them implicit so we don't really need them and thus we will skip the
					// headstands they require.
					HtmlTableRow row = new HtmlTableRow();
					Body.Controls.Add(row);

					AddLeadingDataColumns(row, rowId);
					for (int i = 0; i < ColumnSources.Count; i++) {
						HtmlTableCell c = new HtmlTableCell();
						c.InnerText = ColumnSources[i].TypeInfo.GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo).Format(ColumnSources[i].GetValue());
						row.Controls.Add(c);
						if (ColumnHiddenSources[i] != null && ColumnHiddenSources[i].GetValue() != null)
							// There is a Hidden column and it is non-null, indicating this value originates in a record
							// that is hidden and is not part of the actual browser record.
							// Set a special style in the cell to get struck-out text
							c.Attributes["class"] = "hidden";
					}
					AddTrailingDataColumns(row, rowId);
					return true;
				}
			);
			SetBrowseContextRecordId(null);
			if (HeaderOption == HeaderOptions.Always
				|| (HeaderOption == HeaderOptions.OnlyWithRows && Body.Controls.Count > 0)
				|| (HeaderOption == HeaderOptions.OnlyWithMultipleRows && Body.Controls.Count > 1)) {
				HtmlGenericControl row = new HtmlGenericControl("thead");
				List.Controls.AddAt(0, row);

				AddLeadingHeaderColumns(row);
				for (int i = 0; i < ColumnHeaders.Count; i++) {
					HtmlTableCell c = new HtmlTableCell("th");
					c.InnerText = ColumnHeaders[i].Translate();
					row.Controls.Add(c);
				}
				AddTrailingHeaderColumns(row);
			}
			if (Body.Controls.Count == 0 && ForceDummyRow) {
				HtmlTableRow row = new HtmlTableRow();
				Body.Controls.Add(row);

				AddLeadingDummyDataColumns(row);
				for (int i = 0; i < ColumnSources.Count; i++) {
					HtmlTableCell c = new HtmlTableCell();
					row.Controls.Add(c);
				}
				AddTrailingDummyDataColumns(row);
			}
			if (IsSelectable) {
				// Make sure the scripts we need are present.
				((TblPage)Page).AddScript(SelectionChangeScript);
				// base.OnPreRender leaves the BrowseLogic positioned at no record thus we can get the proper enabler mask for no-selection state
				((TblPage)Page).AddGlobalEventCode("onload", Strings.IFormat("showSelected(0x{0:x}, \"{1}\");", GetEnablerFlags(), Prefix));
			}
			else
				GetEnablerFlags();

			// Remove any buttons which are never enabled
			for (int i = CommandButtonInfoList.Count; --i >= 0; ) {
				CommandButtonInfo info = CommandButtonInfoList[i];
				if (!info.SometimesEnabled)
					info.Button.Parent.Controls.Remove(info.Button);
			}
		}
		#endregion
		#region - Header specialization
		protected virtual void AddLeadingHeaderColumns(HtmlGenericControl headerRow) {
			if (IsSelectable) {
				HtmlTableCell c = new HtmlTableCell();
				if (IsPicker && TypeInfo.AllowNull) {
					// TODO: Clicking on this row does not click the button
					// TODO: Clicking on this row does not clear the 'selected' style from the former selection (it should leave no row selected)
					// TODO: It is not obvious that this button means no selection
					HtmlInputRadioButton button = SelectionButtonGroup.AddButton(null);
					c.Controls.Add(button);
				}
				headerRow.Controls.Add(c);
			}
		}
		protected virtual void AddTrailingHeaderColumns(HtmlGenericControl headerRow) {
		}
		#endregion
		#region - Data-row specialization
		protected virtual void AddLeadingDataColumns(HtmlTableRow row, object rowId) {
			if (IsSelectable) {
				// It appears that (at least in Firefox) a click on the button, whether by the user or a programmatic call to button.click(),
				// also raises the click events on all the containing elements including the table row. As a result our scripting only has to look
				// for clicks on the row, and it then selects the contained button by setting 'checked' rather than calling 'click()' to avoid a second
				// recursive click event back to itself.
				row.Attributes["onclick"] = Strings.IFormat("rowClickSelects(this, 0x{0:x}, \"{1}\");", GetEnablerFlags(), Prefix);

				HtmlTableCell c = new HtmlTableCell();
				HtmlInputRadioButton button = SelectionButtonGroup.AddButton(rowId);
				c.Controls.Add(button);
				row.Controls.Add(c);
				if (pMode == BrowserModes.Browse && (SelectionButtonGroup.ValueStatus != null || SelectionButtonGroup.GetValue() == null))
					Value = rowId;
			}
			else
				GetEnablerFlags();
		}
		protected virtual void AddTrailingDataColumns(System.Web.UI.HtmlControls.HtmlTableRow row, object id) {
			if (!IsSelectable)
				row.Controls.Add(ClosedCommandTableCellHolder);
		}
		#endregion
		#region - Dummy-row specialization
		protected virtual void AddLeadingDummyDataColumns(HtmlTableRow row) {
		}
		protected virtual void AddTrailingDummyDataColumns(System.Web.UI.HtmlControls.HtmlTableRow row) {
			// We only force the dummy record when !IsSelectable to give us a place to park the commands.
			row.Controls.Add(ClosedCommandTableCellHolder);
		}
		#endregion
		// Get the mask of which commands are currently enabled, and also record whether they are enabled or not.
		private Int32 GetEnablerFlags() {
			Int32 result = 0;
			for (int i = CommandButtonInfoList.Count; --i >= 0; ) {
				CommandButtonInfo info = CommandButtonInfoList[i];
				if (info.Command.Enabled) {
					result |= checked(1 << i);
					info.SometimesEnabled = true;
				}
				else
					info.SometimesDisabled = true;
			}
			return result;
		}
		// Return the CommandHolder wrapped in a table cell
		private HtmlTableCell ClosedCommandTableCellHolder {
			get {
				HtmlTableCell result = new HtmlTableCell();
				result.Controls.Add(CommandHolder);
				return result;
			}
		}
		// Note: showSelected really wants to click the *parent* of the button input control since the button itself is already
		// selected. However, although all elements have an onclick *event* only (some) input controls have a click() *method*,
		// so we can't click the row directly, we must click the radio button.
		// It is truly annoying that there is no succinct way of finding *which* button in a radio button set is checked, instead it
		// is necessary to loop through all the radio buttons with the desired Name property.
		//
		// Note: rowClickSelects could remember i (the row index) of the oldSelectedRow, and thus restore an 'evenUnselected' or 'oddUnselected'
		// style if we want to use even/odd row styling to create row contrast.
		private static readonly string SelectionChangeScript = @"
			function showSelected(noSelectionEnablerMask, prefix) {
				var buttons = document.getElementsByName(prefix+""" + SelectionButtonGroupName + @""");
				for (var i = buttons.length; --i >= 0; ) {
					var ctrl = buttons[i];
					if (ctrl.checked) {
						ctrl.click();
						return;
					}
				}
				enableCommands(noSelectionEnablerMask, prefix);
			}
			function enableCommands(enablerMask, prefix) {
				for (var i = 30; --i >= 0; ) {
					var cmdButton = document.getElementById(prefix+""C_""+i);
					if (cmdButton != null)
						cmdButton.disabled = (enablerMask & (1 << i)) == 0;
				}
			}
			function rowClickSelects(rowObj, enablerMask, prefix) {
				var buttons = rowObj.getElementsByTagName(""input"");
				if (buttons.length == 1) {
					buttons[0].checked = true;
					var containingTableRows = rowObj.parentNode.parentNode.tBodies[0].rows;
					var oldSelectedRow = null;
					for (var i = containingTableRows.length; --i >= 0; ) {
						var row = containingTableRows[i];
						if (row.className == ""selected"") {
							oldSelectedRow = row;
							break;
						}
					}
					if (oldSelectedRow !== rowObj) {
						if (oldSelectedRow != null)
							oldSelectedRow.className = """";
						rowObj.className = ""selected"";
					}
					enableCommands(enablerMask, prefix);
				}
			}
		";
		#endregion
		#region CreateListAndColumns - List and its columns
		public IListSortColumn AddListColumn(Translation.Key label, Fmt fmt, DataFlow.Source colSource, DataFlow.Source colHiddenSource, BTbl.ListColumnArg.Contexts contexts) {
			// TODO: Use different contexts depending on our Mode:
			// Browse -> List
			// OpenPicker -> OpenCombo
			// CLosedPicker -> ClosedCombo
			// Display -> ClosedCombo
			// The problem is that this code is called at ctor time, well before PreRender when the Mode has been finalized. So I guess we instead
			// must build all Contexts.List columns, and then let PreRender remove unwanted columns.
			if ((contexts & BTbl.ListColumnArg.Contexts.List) == 0)
				return null;

			TypeFormatter tf = Fmt.GetEditTextHandler(fmt);
			if( tf == null )
				tf = colSource.TypeInfo.GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo);
			if (tf == null) {
				System.Diagnostics.Debug.Assert(false, Strings.IFormat("GCol for {0} returned null TypeFormatter", label.IdentifyingName));
				return null;
			}
			ColumnSources.Add(colSource);
			ColumnHiddenSources.Add(colHiddenSource);
			ColumnHeaders.Add(label);
			return new ColSourceWrapper(colSource);
		}
		#endregion
		#region IBrowseUI Members
		public System.Drawing.Bitmap RefreshBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap DeleteBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap PrintBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap SearchBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap SearchNextBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap SearchPreviousBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap EditBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap RestoreBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap ViewBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap ImportBitmap {
			get {
				return null;
			}
		}
		public System.Drawing.Bitmap ExportBitmap {
			get {
				return null;
			}
		}
		public UIForm Form {
			get { return null;/*this.Page;*/ }
		}
		public void SetInitialSortKey(params SortKey<IListSortColumn>[] keys) {
			BrowserLogic.BrowserSortingPositioner.KeyProvider = new DataFlow.KeyProviderFromSources(keys.Select(key => new SortKey<DataFlow.Source>(((ColSourceWrapper)key.Value).Source, key.Descending)).ToArray());
		}
		public bool HandleConcurrencyError(XAFClient db, System.Data.DBConcurrencyException ex) {
			return false;	// no retry on concurrency error, just issue the error.
		}
		public DataFlow.NotifyingSource GetControlNotifyingSource(object Id, out bool needsContext) {
			throw new NotImplementedException();
		}
		public DataFlow.NotifyingSource GetControlUncheckedNotifyingSource(object Id, out bool needsContext) {
			throw new NotImplementedException();
		}
		public DataFlow.Sink GetSubBrowserSink(object Id, BrowserInitTarget valInSubBrowser) {
			throw new NotImplementedException();
		}
		public DataFlow.Sink GetControlSink(object id) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateGroupBox(BrowseLogic.ContainerNodeContent contents, Key labelContext) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateTabControl(List<CommonLogic.ContainerNodeContent> tabList, Key labelContext) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateSplitterGroup(List<CommonLogic.ContainerNodeContent> pageList) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateGroup(CommonLogic.ContainerNodeContent contents) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateRow(CommonLogic.ContainerNodeContent contents, Key labelContext) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateControl(TblLeafNode leafNode, TypeInfo.TypeInfo controlType, Fmt fmt, Settings.Container settingsContainer, MultiDisablerIfAllEnabled writeableDisabler, MultiDisablerIfAllEnabled enabledDisabler, Key labelContext, Key labelSuffix = null) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateInfoForHiddenControl(TblLeafNode leafNode, IBasicDataControl dummyControl) {
			throw new NotImplementedException();
		}
		public CommonLogic.TblLayoutNodeInfoBase CreateInfoForEmptyControl(TblLeafNode leafNode) {
			throw new NotImplementedException();
		}
		public Key GetLabelForColumn(Thinkage.Libraries.DBILibrary.DBI_Column column, object rowId) {
			throw new NotImplementedException();
		}
		#endregion
		#region Value synchronization
		private void SyncData() {
			KeepDataSynched = true;
			SetBrowseContextRecordId(UncheckedValue);
		}
		void SelectionControl_ValueChanged() {
			UncheckedValue = ValueStatus == null ? SelectionButtonGroup.GetValue() : null;
			SelectionFilter.SetValue(UncheckedValue);
			if (KeepDataSynched)
				SetBrowseContextRecordId(UncheckedValue);
			if (ClearCommand != null)
				ClearCommand.Enabled = SelectionButtonGroup.ValueStatus != null || SelectionButtonGroup.GetValue() != null;
			if (Notify != null && !pSuppressNotification)
				Notify();
		}
		#endregion
		#region IDisposable Members
		public override void Dispose() {
			base.Dispose();

			if (BrowserLogic != null)
				BrowserLogic.Dispose();
		}
		#endregion
		#region IBasicDataControl Members
		public TypeInfo.TypeInfo TypeInfo {
			get { return pTypeInfo; }
		}
		private readonly TypeInfo.TypeInfo pTypeInfo;
		public object Value {
			get {
				return SelectionButtonGroup.GetValue();
			}
			set {
				SelectionButtonGroup.SetValue(value);
			}
		}
		public System.Exception ValueWarning { get { if (ValueStatus != null) throw ValueStatus; return null; } }
		public System.Exception ValueStatus {
			get { return SelectionButtonGroup.ValueStatus; }
		}
		public bool SuppressNotification {
			get {
				return pSuppressNotification;
			}
			set {
				pSuppressNotification = value; ;
			}
		}
		private bool pSuppressNotification = false;
		public event ControlNotification Notify;
		#endregion

		#region ICommonUI Members


		public CommonLogic LogicObject {
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}