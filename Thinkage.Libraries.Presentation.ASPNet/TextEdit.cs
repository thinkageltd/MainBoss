using System.Web.UI.WebControls;
using Thinkage.Libraries.XAF.UI;

namespace Thinkage.Libraries.Presentation.ASPNet {
	public class TextEdit : TextBox, IBasicDataControl {
		public TextEdit(TypeEditTextHandler tf) {
			Thinkage.Libraries.TypeInfo.StringTypeInfo tinfo = tf.GetTypeInfo() as Thinkage.Libraries.TypeInfo.StringTypeInfo;
			// TODO: The multiline check may be different now.
			if (tinfo != null && tinfo.ElementType.CheckMembership('\n') == null)
				TextMode = TextBoxMode.MultiLine;
			// TODO: The default sizes for controls in HTML are really stupid, but I'm not sure how to set them without creating an overwidth page
			// for the user's browser.
			pFormatter = tf;
			TextChanged += delegate(object sender, System.EventArgs ea) {
				HandleChangedText();
			};
			ParseText();
		}
		private readonly TypeEditTextHandler pFormatter;

		public object Value {
			get {
				if (pValueStatus != null)
					throw pValueStatus;
				return pValue;
			}
			set {
				Text = value == null ? null : pFormatter.FormatForEdit(TypeInfo.ClosestValueTo(value));
				HandleChangedText();
			}
		}
		private object pValue;

		public TypeInfo.TypeInfo TypeInfo {
			get { return pFormatter.GetTypeInfo(); }
		}

		public System.Exception ValueWarning { get { if (ValueStatus != null) throw ValueStatus; return null; } }
		public System.Exception ValueStatus {
			get { return pValueStatus; }
		}
		private System.Exception pValueStatus;

		public bool SuppressNotification {
			get {
				return pSuppressNotification;
			}
			set {
				pSuppressNotification = value;
			}
		}
		private bool pSuppressNotification = false;
		public event ControlNotification Notify;

		private void HandleChangedText() {
			ParseText();
			if (Notify != null && !pSuppressNotification)
				Notify();
		}
		private void ParseText() {
			string text = Text;
			if (text.Length != 0 || !TypeInfo.AllowNull) {
				// The text is non-empty or the type does not allow the null value. Parse the text into a value.
				// We do this in case the empty text can parse into a valid non-null value.
				try {
					pValue = TypeInfo.ClosestValueTo(pFormatter.ParseEditText(text));
					pValueStatus = TypeInfo.CheckMembership(pValue);
				}
				catch (System.Exception ex) {
					pValueStatus = ex;
				}
				if (text.Length == 0 && pValueStatus != null) {
					// The text was empty but did not parse into a valid value, so treat the empty text as the null value instead,
					// thus yielding a better error message.
					pValue = null;
					pValueStatus = TypeInfo.CheckMembership(pValue);
				}
			}
			else {
				// The text is empty and the type allows null. Treat empty text as a null value.
				pValue = null;
				pValueStatus = null;
			}
		}
	}
}
