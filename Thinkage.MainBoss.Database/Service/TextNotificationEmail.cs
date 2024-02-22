using System.Text;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {
	[Invariant]
	public class TextNotificationEmail : INotificationEmail {
		private readonly StringBuilder body;

		public System.Globalization.CultureInfo PreferredLanguage {
			get {
				return pPreferredLanguage;
			}
		}
		private readonly System.Globalization.CultureInfo pPreferredLanguage;
		public TextNotificationEmail(System.Globalization.CultureInfo preferredLanguage) {
			pPreferredLanguage = preferredLanguage;
			body = new StringBuilder();
		}
		public string BodyAsString {
			get {
				return body.ToString();
			}
		}
		public void AppendParagraph(string content) {
			if (content == null)
				return;
			body.Append(content);
			NewLine();
		}
		public void AppendMultiLine(string css, string content) {
			StartDiv(css);
			Append(content);
			EndDiv();
		}
		public void Append(string content) {
			body.Append(content);
		}
		public void AppendLine(string content) {
			body.Append(content);
			NewLine();
		}
		public void NewLine() {
			body.Append(SMTPClient.CrLf);
		}
		public void AppendBlank() {
			body.Append(" ");
		}
		public void AppendWebAccessLink(string preamble, string urlText, string urlLink) {
			Append(preamble);
			AppendBlank();
			Append(urlText);
			AppendBlank();
			Append("<");
			Append(urlLink);
			Append(">");
			NewLine();
		}
		public void StartDiv(params string[] classnames) {
			NewLine();
		}
		public void EndDiv() {
			NewLine();
		}
		public void LabelValueLine(Key label, string value) {
			Append(label.Translate(PreferredLanguage));
			NewLine();
			AppendLine(value);
		}
		public void AppendInitialInformation(InitialNotificationInformation value) {
			if (value.UnitInformation != null) {
				LabelValueLine(KB.K("Unit"), value.UnitInformation.Unit);
			}
			if (value.RequestorInformation != null) {
				Append(KB.K("Requestor").Translate(PreferredLanguage));
				NewLine();
				Append(value.RequestorInformation.Name);
				AppendBlank();
				Append(value.RequestorInformation.Phone);
				if (!string.IsNullOrEmpty(value.RequestorInformation.MailTo)) {
					AppendBlank();
					Append(value.RequestorInformation.MailTo);
					AppendBlank();
					Append("<mailto:");
					Append(value.RequestorInformation.MailTo);
					Append(">");
				}
				NewLine();
			}
			if (!string.IsNullOrEmpty(value.WorkDescription))
				LabelValueLine(KB.K("Description"), value.WorkDescription);
		}
		public void LabelValueMultiLine(Key label, string css, string value) {
			body.Append(label.Translate(PreferredLanguage));
			NewLine();
			AppendMultiLine(css, value);
		}
		public void StartViewPanel() {
			NewLine();
		}
		public void EndViewPanel() {
			NewLine();
			NewLine();
		}
		public void StartViewHistoryItem() {
			NewLine();
		}
		public void EndViewHistoryItem() {
			NewLine();
		}
		public void StartHistoryItemTitle() {
			NewLine();
		}
		public void EndHistoryItemTitle() {
			NewLine();
		}
		public void StartHistoryItemBody() {
			NewLine();
		}
		public void EndHistoryItemBody() {
			NewLine();
		}
		public void AppendAlert(string content) {
			body.Append("***");
			body.Append(content);
			body.Append("***");
		}
	}
}