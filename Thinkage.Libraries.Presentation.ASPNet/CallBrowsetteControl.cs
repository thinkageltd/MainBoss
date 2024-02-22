using System.Web;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.Presentation;
using System.Collections.Generic;
using System.Text;

namespace Thinkage.Libraries.Presentation.ASPNet {
	/// <summary>
	/// For the browsette control the DataFlow value is the master value for tbe browsette filter.
	/// </summary>
	public class CallBrowsetteControl : HtmlAnchor {
		// TODO: We need the ability (if asked) to create a BrowseLogic derivation for the purpose of extracting
		// exported commands.
		// We would have:
		// - an argument saying the caller might want exported commands.
		// - if so, find the tbl, check the composite views to see if any exported commands exist.
		// - if so, create a dummy IBrowseUI object and the appropriate BrowseLogic derivation
		// - have a method to extract the commands from this object
		// - arrange that all our filter and value binding touches the "live" browseLogic object if any as well as modifying the URL.
		private class BrowsetteFilter : DataFlow.NotifyingValue {
			public BrowsetteFilter(CallBrowsetteControl owner, TypeFormatter tf, DBI_Path slaveLinkage) {
				SlaveLinkage = slaveLinkage;
				Owner = owner;
				pFormatter = tf;
				pValueStatus = TypeInfo.CheckMembership(pValue);
			}
			public void SetValue(object val) {
				pValue = TypeInfo.ClosestValueTo(val);
				pValueStatus = TypeInfo.CheckMembership(pValue);

				if (Notify != null)
					Notify();
				Owner.UpdateHRef();
			}
			public object GetValue() {
				if (pValueStatus != null)
					throw pValueStatus;
				return pValue;
			}
			public TypeInfo.TypeInfo TypeInfo { get { return pFormatter.GetTypeInfo(); } }
			private readonly TypeFormatter pFormatter;
			public event DataFlow.Notification Notify;
			private object pValue;
			private System.Exception pValueStatus;
			private readonly DBI_Path SlaveLinkage;
			private readonly CallBrowsetteControl Owner;
			public void AddURLFilter(StringBuilder sb) {
				sb.Append("&PF=");
				sb.Append(HttpUtility.UrlEncode(SlaveLinkage.ToString()));
				sb.Append("=");
				sb.Append(HttpUtility.UrlEncode(pFormatter.Format(pValue)));
			}
		}
		public CallBrowsetteControl(Key label, [Translation.Translated] string qualifierFormat, Tbl browseTbl) {
			QualifierFormat = qualifierFormat;
			BrowseTbl = browseTbl;
			Literal content = new Literal();
			content.Text = HttpUtility.HtmlEncode(label.Translate());
			Controls.Add(content);
		}
		public DataFlow.NotifyingValue AddFilter(TypeFormatter tf, DBI_Path slaveLinkage) {
			BrowsetteFilter f = new BrowsetteFilter(this, tf, slaveLinkage);
			Filters.Add(f);
			return f;
		}
		// Both of the following need the ability to register Id's
		public DataFlow.NotifyingValue AddValueBindTarget(object tagId) {
			return null;
		}
		public DataFlow.NotifyingValue AddFilterBindTarget(object tagId) {
			return null;
		}
		private readonly string QualifierFormat;
		private readonly Tbl BrowseTbl;
		private string Xid;
		private List<BrowsetteFilter> Filters = new List<BrowsetteFilter>();

		public void SetXID(string xid) {
			Xid = xid;
			UpdateHRef();
		}
		private void UpdateHRef() {
			// TODO: More encoding here in case the format insertions contain &?=%/ etc they must be encoded as %hh syntax.
			// TODO: The Filter string could in theory contain Unicode text. Whatever streams the Filter to text must generate
			// it in some 8-bit encoding like UTF-8. Alternatively we could use a miniform here which uses POST so the data is
			// encoded in the post data. Not sure what encoding you get then... I guess whatever the HTTP request header says.
			StringBuilder sb = new StringBuilder();
			sb.Append("BrowseTable.aspx?Tbl=");
			sb.Append(TblApplication.Instance.GetTblId(BrowseTbl));
			sb.Append("&Q=");
			sb.Append(HttpUtility.UrlEncode(Strings.IFormat(QualifierFormat, Xid)));
			foreach (BrowsetteFilter f in Filters)
				f.AddURLFilter(sb);
			HRef = HttpUtility.HtmlEncode(sb.ToString());
		}
	}
}
