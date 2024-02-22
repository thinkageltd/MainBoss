using System;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {
	public class HtmlNotificationEmail : INotificationEmail {
		delegate void ContentAppend();
		private static readonly string SiteCss = InitializeSiteCss();
		private readonly StringBuilder body;
		private readonly bool ended = false;

		private string Encode(string value) {
			return (!String.IsNullOrEmpty(value)) ? System.Net.WebUtility.HtmlEncode(value) : String.Empty;
		}
		public System.Globalization.CultureInfo PreferredLanguage {
			get {
				return pPreferredLanguage;
			}
		}
		private readonly System.Globalization.CultureInfo pPreferredLanguage;

		public HtmlNotificationEmail(System.Globalization.CultureInfo preferredLanguage) {
			pPreferredLanguage = preferredLanguage;
			body = new StringBuilder();
			body.Append(KB.I("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">"));
			body.Append(KB.I("<HTML xmlns=\"http://www.w3.org/1999/xhtml\">"));
			body.Append(KB.I("<HEAD>")); // <META http-equiv=Content-Type content=\"text/html; charset=iso-8859-1\"></HEAD>");
			body.Append(SiteCss);
			body.Append(KB.I("</head>"));
			body.Append(KB.I("<BODY>"));
			NewLine();
		}
		public string BodyAsString {
			get {
				if (!ended)
					End();
				return body.ToString();
			}
		}
		static string InitializeSiteCss() {
			StringBuilder cssBody = new StringBuilder();
			System.IO.Stream cssStream = null;
			try {
				cssStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Thinkage.MainBoss.Database.Service.Site.css");
				cssBody.Append(KB.I("<STYLE type=\"text/css\">"));
				using (System.IO.StreamReader cssStringReader = new System.IO.StreamReader(cssStream, Encoding.UTF8)) {
					cssStream = null; //cssStringReader will clean up the stream
					cssBody.Append(cssStringReader.ReadToEnd());
				}
			}
			finally {
				if (cssStream != null)
					cssStream.Dispose();
			}
			cssBody.Append(KB.I("</STYLE>"));
			// Compress the CSS to save bandwidth
			System.Text.RegularExpressions.Regex commentRemoval = new System.Text.RegularExpressions.Regex(KB.I("/[*][^*]*[*]+([^/][^*]*[*]+)*/"));
			// remove tabs, spaces, newlines, etc.
			string compressed = commentRemoval.Replace(cssBody.ToString(), "");
			compressed = compressed.Replace(KB.I("\r\n"), "");
			compressed = compressed.Replace("\r", "");
			compressed = compressed.Replace("\n", "");
			compressed = compressed.Replace("\t", "");
			compressed = compressed.Replace("  ", "");
			compressed = compressed.Replace("    ", "");
			return compressed;
		}
		[Invariant]
		public void AppendMultiLine([Invariant] string css, string content) {
			StartDiv(css, "MultiLine");
			body.Append(Encode(content).Replace("\n", KB.I("<br/>"))); // try to preserve the newlines in the fields by putting HTML <br> directives in.
			EndDiv();
		}
		public void Append([Invariant] string content) {
			body.Append(Encode(content));
		}
		public void AppendLine([Invariant] string content) {
			Append(content);
			NewLine();
		}
		[Invariant]
		public void AppendParagraph([Invariant] string content) {
			if (content == null)
				return;
			body.Append(KB.I("<P>"));
			body.Append(Encode(content).Replace("\n", KB.I("<br/>"))); // try to preserve the newlines in the fields by putting HTML <br> directives in.
			body.Append(KB.I("</P>"));
			NewLine();
		}
		public void NewLine() {
			body.Append(SMTPClient.CrLf);
		}
		[Invariant]
		public void AppendBlank() {
			body.Append(KB.I("&nbsp;"));
		}
		[Invariant]
		public void AppendWebAccessLink([Invariant] string preamble, [Invariant] string urlText, [Invariant] string urlLink) {
			body.Append(KB.I("<P>"));
			Append(preamble);
			AppendBlank();
			body.Append(KB.I("<A href=\""));
			body.Append(urlLink); // not sure why using UrlEncode/UrlPathEncode gives an <A href="" that is not interpretable by Outlook (for example); So we won't use it.
			body.Append(KB.I("\">"));
			Append(urlText);
			body.Append(KB.I("</A>"));
			body.Append(KB.I("</P>"));
		}
		[Invariant]
		public void StartDiv([Invariant]params string[] classnames) {
			bool first = true;
			body.Append(KB.I("<div"));
			foreach (var s in classnames) {
				if (String.IsNullOrEmpty(s))
					continue;
				if (first) {
					body.Append(KB.I(" class=\""));
					first = false;
				}
				else
					body.Append(" ");
				body.Append(s);
			}
			if (!first)
				body.Append(KB.I("\""));
			body.Append(KB.I(">"));
		}
		[Invariant]
		public void EndDiv() {
			body.Append(KB.I("</div>"));
			NewLine();
		}
		public void LabelValueLine(Key label, [Invariant] string value) {
			LabelValueLine(label, null, delegate () {
				body.Append(value);
			});
		}
		public void AppendInitialInformation(InitialNotificationInformation value) {
			if (value.UnitInformation != null) {
				LabelValueLine(KB.K("Unit"), value.UnitInformation.IsDeleted ? "Deleted" : null, delegate () {
					body.Append(value.UnitInformation.Unit);
				});
			}
			if (value.RequestorInformation != null) {
				LabelValueLine(KB.K("Requestor"), null, delegate () {
					string name = Strings.IFormat("<td class=\"{1}\">{0}</td>", value.RequestorInformation.Name, value.RequestorInformation.IsDeleted ? KB.I("Deleted Name") : "Name");
					string phone = Strings.IFormat("<td{1}>{0}</td>", value.RequestorInformation.Phone, value.RequestorInformation.IsDeleted ? KB.I(" class=\"Deleted\"") : "");
					string email = Strings.IFormat("<td{1}>{0}</td>", String.IsNullOrEmpty(value.RequestorInformation.MailTo) ? "" : value.RequestorInformation.MailTo, value.RequestorInformation.IsDeleted ? KB.I(" class=\"Deleted\"") : "");

					body.Append(Strings.IFormat("<table id=\"contactPanel\"><tr>{0}{1}{2}</tr></table>", name, phone, email));
				});
			}
			if (!string.IsNullOrEmpty(value.WorkDescription))
				LabelValueMultiLine(KB.K("Description"), null, value.WorkDescription);

		}
		private void LabelValueLine(Key label, [Invariant] string valueclassname, ContentAppend BodyAppend) {
			body.Append(KB.I("<tr><td class=\"Label\">"));
			Append(label.Translate(PreferredLanguage));
			body.Append(KB.I("</td><td"));
			if (valueclassname != null) {
				body.Append(KB.I(" class=\""));
				body.Append(valueclassname);
				body.Append(KB.I("\""));
			}
			body.Append(KB.I(">"));
			BodyAppend();
			body.Append(KB.I("</td></tr>"));
			NewLine();
		}
		public void LabelValueMultiLine(Key label, [Invariant] string css, string value) {
			LabelValueLine(label, null, delegate () {
				AppendMultiLine(css, value);
			});
		}
		private void End() {
			body.Append(KB.I("</BODY>"));
			NewLine();
			body.Append(KB.I("</HTML>"));
			NewLine();
		}
		public void StartViewPanel() {
			StartDiv(KB.I("viewPanel"));
			body.Append(KB.I("<table>"));
			NewLine();
			body.Append(KB.I("<tbody>"));
			NewLine();
		}
		public void EndViewPanel() {
			NewLine();
			body.Append(KB.I("</tbody>"));
			NewLine();
			body.Append(KB.I("</table>"));
			NewLine();
			EndDiv();
			NewLine();
		}
		public void StartViewHistoryItem() {
			StartDiv(KB.I("viewHistoryItem"));
		}
		public void EndViewHistoryItem() {
			EndDiv();
			NewLine();
		}
		public void StartHistoryItemTitle() {
			StartDiv(KB.I("title"));
		}
		public void EndHistoryItemTitle() {
			EndDiv();
			NewLine();
		}
		public void StartHistoryItemBody() {
			StartDiv(KB.I("body"));
		}
		public void EndHistoryItemBody() {
			EndDiv();
			NewLine();
		}
		public void AppendAlert([Invariant] string content) {
			StartDiv(KB.I("Alert"));
			Append(content);
			EndDiv();
		}
	}
}
