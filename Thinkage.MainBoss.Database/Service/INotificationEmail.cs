using System;
namespace Thinkage.MainBoss.Database.Service {
	public interface INotificationEmail {
		void Append(string content);
		void AppendLine(string content);
		void NewLine();
		void AppendBlank();
		void AppendWebAccessLink(string preamble, string urlText, string urlLink);
		void AppendAlert(string content);
		void AppendMultiLine([Thinkage.Libraries.Translation.Invariant]string css, string content);
		void EndDiv();
		void EndHistoryItemBody();
		void EndHistoryItemTitle();
		void EndViewHistoryItem();
		void EndViewPanel();
		void LabelValueMultiLine(Thinkage.Libraries.Translation.Key label, [Thinkage.Libraries.Translation.Invariant]string css, string value);
		void LabelValueLine(Thinkage.Libraries.Translation.Key label, string value);
		void AppendInitialInformation(InitialNotificationInformation info);
		void StartDiv([Thinkage.Libraries.Translation.Invariant]params string[] classnames);
		void StartHistoryItemBody();
		void StartHistoryItemTitle();
		void StartViewHistoryItem();
		void StartViewPanel();
		void AppendParagraph(string paragraph);
		// Output function
		string BodyAsString {
			get;
		}
		System.Globalization.CultureInfo PreferredLanguage {
			get;
		}
	}
}
