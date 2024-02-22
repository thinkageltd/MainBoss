using System;
using System.Collections.Generic;
using System.Web;
using Thinkage.Libraries.Presentation;
using System.Web.UI.HtmlControls;
using Thinkage.Libraries.Collections;
using System.Text;

namespace Thinkage.Libraries.Presentation.ASPNet {
	public abstract class TblPage : System.Web.UI.Page {
		protected override void OnInit(EventArgs e) {
			base.OnInit(e);

			// Identify the Tbl from the query params T=
			string tblId = Request.QueryString["Tbl"];
			if (string.IsNullOrEmpty(tblId))
				throw new GeneralException(KB.K("Query parameter 'Tbl' is missing or empty"));
			TInfo = TblApplication.Instance.GetTblFromTblId(tblId);
			if (TInfo == null)
				throw new GeneralException(KB.K("Cannot find form layout information for Tbl '{0}'"), tblId);
			BuildHead();
			BuildBody();
		}
		private void BuildHead() {
			// <head>
			PageHead = new HtmlHead();
			Controls.Add(PageHead);
#if SHOWCELLS
					// <head><style>
					// The following is DEBUG CODE to make cells visible.
					HtmlGenericControl style = new HtmlGenericControl("style");
					style.InnerText = @"TABLE { border-width : medium; border : solid }
										TD { border-width : thin; border : solid }
										TH { border-width : thin; border : solid }";
					style.Attributes.Add("type", "text/css");
					PageHead.Controls.Add(style);
#endif
			{
				// <head><title>
				PageTitle = new HtmlTitle();
				PageHead.Controls.Add(PageTitle);
			}
			{
				// <head><link> for style sheet
				HtmlLink styleLink = new HtmlLink();
				PageHead.Controls.Add(styleLink);
				styleLink.Href = "Style.css";
				styleLink.Attributes["rel"] = "stylesheet";
				styleLink.Attributes["type"] = "text/css";
			}
			if (DefaultStyleLanguage != null) {
				// <head><meta> for style language
				HtmlMeta meta = new HtmlMeta();
				PageHead.Controls.Add(meta);
				meta.HttpEquiv = "Content-Style-Type";
				meta.Content = DefaultStyleLanguage;
			}
			if (DefaultScriptingLanguage != null) {
				// <head><meta> for scripting language
				HtmlMeta meta = new HtmlMeta();
				PageHead.Controls.Add(meta);
				meta.HttpEquiv = "Content-Script-Type";
				meta.Content = DefaultScriptingLanguage;
			}
		}
		private void BuildBody() {
			// <body>
			Body = new HtmlGenericControl("body");
			Controls.Add(Body);
			{
				// <body> main title
				BodyTitle = new HtmlGenericControl("H1");
				Body.Controls.Add(BodyTitle);
			}
		}

		public void SetTitles(string title) {
			PageTitle.Text = HttpUtility.HtmlEncode(title);
			BodyTitle.InnerText = title;
		}

		public void AddScript(string script) {
			if (Scripts.Contains(script))
				return;
			Scripts.Add(script);
			HtmlGenericControl scriptCtrl = new HtmlGenericControl("script");
			// TODO: These doodads assume javascript
			scriptCtrl.InnerHtml = "<!-- " + script + " -->";
			PageHead.Controls.Add(scriptCtrl);
		}
		public void AddGlobalEventCode(string eventName, string code) {
			Set<string> already;
			if (!GlobalEvents.TryGetValue(eventName, out already)) {
				already = new Set<string>();
				GlobalEvents[eventName] = already;
			}
			already.Add(code);
			StringBuilder sb = new StringBuilder();
			foreach (string s in already)
				sb.Append(s);
			Body.Attributes[eventName] = sb.ToString();
		}
		protected Tbl TInfo;
		private HtmlHead PageHead;
		private HtmlTitle PageTitle;
		private HtmlGenericControl BodyTitle;
		protected HtmlGenericControl Body;
		protected string DefaultScriptingLanguage = "text/javascript";
		protected string DefaultStyleLanguage = "text/css";
		private Set<string> Scripts = new Set<string>();
		private Dictionary<string, Set<string>> GlobalEvents = new Dictionary<string, Set<string>>();

		public bool CallEditorsModally { get { return true; } }
	}
}
