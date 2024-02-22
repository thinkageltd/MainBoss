using System.Collections.Generic;
using System.Web.UI.HtmlControls;
using Thinkage.Libraries.Translation;
using System;

namespace Thinkage.Libraries.Presentation.ASPNet {
	public class HtmlRadioButtonSet : HtmlSequence, System.Web.UI.IPostBackDataHandler, DataFlow.NotifyingValue {
		public HtmlRadioButtonSet(TypeInfo.TypeInfo tinfo) {
			pTypeInfo = tinfo;
			pEditTextHandler = pTypeInfo.GetTypeEditTextHandler(Thinkage.Libraries.Application.InstanceCultureInfo);
			if (pEditTextHandler == null)
				// We could check the type info for being IdTypeInfo or LinkedTypeInfo but all we'll do is die anyway eventually...
				pEditTextHandler = new IdEditTextHandler(pTypeInfo);
			pValueStatus = pTypeInfo.CheckMembership(pValue);
		}
		public string Name {
			get {
				return pName;
			}
			set {
				pName = value;
				ID = value;	// so that our IPostBackDataHandler gets used.
				foreach (HtmlInputRadioButton irb in ButtonIndex.Values)
					irb.Name = value;
			}
		}
		private string pName;

		private static readonly string ClientButtonValueForNull = "n";
		private static readonly string ClientButtonNonNullValuePrefix = "v";

		// The Html Forms definition states that one should not rely on the ability to have NO buttons selected.
		//
		// However, if the caller chooses to set the control's initial value to null even though the type info does not allow null,
		// the behaviour will be the same as any other value-not-in-selection-set scenario.
		// The browser may or may not select a radio button on the user's behalf, but if this happens we should
		// properly get the change notification on form submission.
		public HtmlInputRadioButton AddButton(object value) {
			HtmlInputRadioButton result = new HtmlInputRadioButton();
			result.Value = FormatValue(value);
			result.Name = pName;
			ButtonIndex.Add(result.Value, result);
			if (result.Value == FormatValue(pValue))
				result.Checked = true;	// In theory no other button should be checked, barring duplicate valueText.
			return result;
		}
		private readonly Dictionary<string, HtmlInputRadioButton> ButtonIndex = new Dictionary<string, HtmlInputRadioButton>();

		public object GetValue() {
			if (pValueStatus != null)
				throw pValueStatus;
			return pValue;
		}
		public System.Exception ValueStatus {
			get {
				return pValueStatus;
			}
		}
		public Thinkage.Libraries.TypeInfo.TypeInfo TypeInfo {
			get { return pTypeInfo; }
		}
		private readonly TypeInfo.TypeInfo pTypeInfo;
		private readonly TypeEditTextHandler pEditTextHandler;
		private object pValue = null;
		private System.Exception pValueStatus = null;
		public event Thinkage.Libraries.DataFlow.Notification Notify;

		public void SetValue(object val) {
			pValue = val;
			pValueStatus = pTypeInfo.CheckMembership(pValue);
			HtmlInputRadioButton newSelectedButton;
			foreach (HtmlInputRadioButton irb in ButtonIndex.Values)
				if (irb.Checked)
					irb.Checked = false;
			if (ButtonIndex.TryGetValue(FormatValue(val), out newSelectedButton))
				newSelectedButton.Checked = true;

			if (Notify != null)
				Notify();
		}

		public bool LoadPostData(string postDataKey, System.Collections.Specialized.NameValueCollection postCollection) {
			// We want to reproduce the semantics of other controls insofar as on postback they obtain their new changed values
			// between Init and PageLoad stages, but do not notify until after PageLoad is done.
			string buttonValue = postCollection[pName];
			object newValue;
			if (buttonValue == null || buttonValue == ClientButtonValueForNull)
				// A null can happen if no buttons were selected or in particular there were no buttons at all.
				newValue = null;
			else if (buttonValue.StartsWith(ClientButtonNonNullValuePrefix)) {
				newValue = pEditTextHandler.ParseEditText(buttonValue.Substring(ClientButtonNonNullValuePrefix.Length));
			}
			else
				throw new GeneralException(KB.K("Unexpected form data value '{0}' for control '{1}'"), buttonValue, pName);

			if (!TypeInfo.GenericEquals(newValue, pValue)) {
				pValue = newValue;
				pValueStatus = pTypeInfo.CheckMembership(pValue);
				return true;
			}
			return false;
		}

		public void RaisePostDataChangedEvent() {
			if (Notify != null)
				Notify();
		}
		private string FormatValue(object val) {
			if (val == null)
				return ClientButtonValueForNull;
			else
				return ClientButtonNonNullValuePrefix+pEditTextHandler.Format(val);
		}

		private class IdEditTextHandler : TypeEditTextHandler {
			public IdEditTextHandler(TypeInfo.TypeInfo tinfo) {
				pTypeInfo = tinfo;
			}
			public string FormatForEdit(object val) {
				return ((Guid)val).ToString();
			}
			public object ParseEditText(string str) {
				return new Guid(str);
			}
			public Thinkage.Libraries.TypeInfo.TypeInfo GetTypeInfo() {
				return pTypeInfo;
			}
			private readonly TypeInfo.TypeInfo pTypeInfo;
			public string Format(object val) {
				return val == null ? null : FormatForEdit(val);
			}
			public System.Drawing.StringAlignment PreferredAlignment {
				get {
					return System.Drawing.StringAlignment.Near;
				}
			}
			public SizingInformation SizingInformation {
				get {
					return pSizingInformation;
				}
			}
			private static SizingInformation pSizingInformation = new SizingInformation(
				 KB.I("1E7A848F-AFDE-441E-AE7E-F4059CC80EE4"),
				 36,
				 0
			);
		}
	}
}