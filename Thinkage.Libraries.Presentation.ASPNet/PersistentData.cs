using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System;
namespace Thinkage.Libraries.Presentation.ASPNet {
	public class PersistentData : HiddenField {
		public static PersistentData CreateClientSide(HtmlControl controlInForm) {
			PersistentData result = new PersistentData();
			controlInForm.Controls.Add(result);
			return result;
		}
		// We don't use ControlState because we want the saved information available as soon as Inited.
		// We don't use ViewState because this can be turned off (and in fact *we* turn it off because it contains so much stuff
		// we don't want).
		// So we instead have a HiddenField but on Init we dig around in the form data and find the value right away.
		private PersistentData() {
		}
		protected override void OnInit(EventArgs e) {
			base.OnInit(e);
			Value = Page.Request.Form[this.ClientID];
		}
		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
		}
		public byte[] Data {
			get {
				return Convert.FromBase64String(Value);
			}
			set {
				Value = Convert.ToBase64String(value);
			}
		}
		public bool DataPresent {
			get {
				return !string.IsNullOrEmpty(Value);
			}
		}
	}
}