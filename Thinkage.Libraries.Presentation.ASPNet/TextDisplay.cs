using System.Web;
using System.Web.UI.WebControls;
using Thinkage.Libraries.XAF.UI;

// TODO: Use <caption> (first element within <table>) to label nested groups and the main field group
// TODO: Use <thead> instead of <tr> for the header row of a multi-column group
// TODO: Use explicit <tbody> around <tr> collection
namespace Thinkage.Libraries.Presentation.ASPNet {
	public class TextDisplay : Label, IBasicDataControl {
		public TextDisplay(TypeFormatter tf) {
			pFormatter = tf;
			pValueStatus = TypeInfo.CheckMembership(pValue);
		}
		public TypeInfo.TypeInfo TypeInfo { get { return pFormatter.GetTypeInfo(); } }
		private TypeFormatter pFormatter;
		private object pValue;
		private System.Exception pValueStatus;

		#region IBasicDataControl Members
		public object Value {
			get {
				if (pValueStatus != null)
					throw pValueStatus;
				return pValue;
			}
			set {
				pValue = TypeInfo.ClosestValueTo(value);
				pValueStatus = TypeInfo.CheckMembership(pValue);

				Text = HttpUtility.HtmlEncode(pFormatter.Format(pValue));
				if (Notify != null && !pSuppressNotification)
					Notify();
			}
		}
		public System.Exception ValueWarning { get { if (ValueStatus != null) throw ValueStatus; return null; } }
		public System.Exception ValueStatus {
			get { throw new System.NotImplementedException(); }
		}
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
		#endregion
	}
}
