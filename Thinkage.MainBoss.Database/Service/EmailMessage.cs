using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dart.Mail;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	public class EmailMessage : IDisposable {
		MailMessage Message;
		readonly PopMessage PopMessage;
		readonly ImapMessage ImapMessage;

		public EmailMessage(Dart.Mail.MailMessage message) {
			Message = message;
		}
		public EmailMessage(string message) {
			Message = new MailMessage(message);
		}
		public EmailMessage(PopMessage popM) {
			PopMessage = popM;
		}
		public EmailMessage(ImapMessage imapM) {
			ImapMessage = imapM;
		}
		void messageGet() {
			if (pError != null || Message != null)
				return;
			if (PopMessage != null) {
				try {
					PopMessage.Get();
					Message = PopMessage.Message;
					return;
				}
				catch (System.Exception e) {
					var se = e as System.Net.Sockets.SocketException;
					if (se != null && (uint)se.HResult == 0x80004005)
						throw new GeneralException(e, KB.K("Connection to {0} has been closed. Retry will occur because of earlier errors."), KB.I("POP"));
					pError = new GeneralException(e, KB.K("Error retrieving {0} message {1}. The problem may be transient and will be retried again later, but manual intervention may be necessary"), KB.I("POP"), PopMessage.Uid);
					return;
				}
			}
			if (ImapMessage != null) {
				try {
					ImapMessage.Get();
					Message = ImapMessage.Message;
					return;
				}
				catch (System.Exception e) {
					var se = e as System.Net.Sockets.SocketException;
					if (se != null && (uint)se.HResult == 0x80004005)
						throw new GeneralException(e, KB.K("Connection to {0} has been closed. Retry will occur because of earlier errors."), KB.I("IMAP"));
					pError = new GeneralException(e, KB.K("Error retrieving {0} message {1}. The problem may be transient and will be retried again later, but manual intervention may be necessary"), KB.I("IMAP"), ImapMessage.Uid);
					return;
				}
			}
			pError = new GeneralException(KB.K("No email message available to be examined."));
		}
		private HeaderField Header([Invariant] string key) {
			messageGet();
			HeaderField property;
			if (Message.Headers.TryGetValue(key, out property))
				return property;
			return null;
		}
		//
		// taken from http://www.codeproject.com/Articles/11902/Convert-HTML-to-Plain-Text
		//
		public static string StripHTML(string source) {
			string result = source;
			try {
				// Remove HTML Development formatting
				// Replace line breaks with space
				// because browsers inserts space
				result = result.Replace("\r", " ");
				// Replace line breaks with space
				// because browsers inserts space
				result = result.Replace("\n", " ");
				// Remove step-formatting
				result = result.Replace("\t", " ");
				// Remove repeating spaces because browsers ignore them
				result = Regex.Replace(result, @"( )+", " ");

				// Remove the header (prepare first by clearing attributes)
				result = Regex.Replace(result, @"<( )*head([^>])*>", "<head>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"(<( )*(/)( )*head( )*>)", "</head>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, "(<head>).*(</head>)", string.Empty, RegexOptions.IgnoreCase);

				// remove all scripts (prepare first by clearing attributes)
				result = Regex.Replace(result, @"<( )*script([^>])*>", "<script>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"(<( )*(/)( )*script( )*>)", "</script>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"(<script>).*(</script>)", string.Empty, RegexOptions.IgnoreCase);

				// remove all styles (prepare first by clearing attributes)
				result = Regex.Replace(result, @"<( )*style([^>])*>", "<style>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"(<( )*(/)( )*style( )*>)", "</style>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, "(<style>).*(</style>)", string.Empty, RegexOptions.IgnoreCase);

				// insert tabs in spaces of <td> tags
				result = Regex.Replace(result, @"<( )*td([^>])*>", "\t", RegexOptions.IgnoreCase);

				// insert line breaks in places of <BR> and <LI> tags
				result = Regex.Replace(result, @"<( )*br( )*>", "\r", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"<( )*li( )*>", "\r", RegexOptions.IgnoreCase);

				// insert line paragraphs (double line breaks) in place
				// if <P>, <DIV> and <TR> tags
				result = Regex.Replace(result, @"<( )*div([^>])*>", "\r\r", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"<( )*tr([^>])*>", "\r\r", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"<( )*p([^>])*>", "\r\r", RegexOptions.IgnoreCase);

				// Remove remaining tags like <a>, links, images,
				// comments etc - anything that's enclosed inside < >
				result = Regex.Replace(result, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);


				result = System.Net.WebUtility.HtmlDecode(result);
#if OLD
				;				// replace special characters:
				result = Regex.Replace(result, @" ", " ", RegexOptions.IgnoreCase);

				result = Regex.Replace(result, @"&bull;", " * ", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&lsaquo;", "<<", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&rsaquo;", ">>", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&trade;", "(tm)", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&frasl;", "/", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&lt;", "<", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&gt;", ">", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&copy;", "(c)", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&reg;", "(r)", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&quot;", "\"", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&apos;", ";", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"&amp;", "&", RegexOptions.IgnoreCase);
				
				// most of the others are for writing non ascii characters in a ascii character sequence.
				//result = Regex.Replace(result, @"&(.{2,6});", string.Empty, RegexOptions.IgnoreCase);
				// we are also ignoring the &#nnn; and &#xhhh; sequences 
#endif

				// for testing
				//Regex.Replace(result,
				//       this.txtRegex.Text,string.Empty,
				//       RegexOptions.IgnoreCase);

				// Remove extra line breaks and tabs:
				// replace over 2 breaks with 2 and over 4 tabs with 4.
				// Prepare first to remove any whitespaces in between
				// the escaped characters and remove redundant tabs in between line breaks
				result = Regex.Replace(result, "(\r)( )+(\r)", "\r\r", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, "(\t)( )+(\t)", "\t\t", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, "(\t)( )+(\r)", "\t\r", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, "(\r)( )+(\t)", "\r\t", RegexOptions.IgnoreCase);
				// Remove redundant tabs
				result = Regex.Replace(result, "(\r)(\t)+(\r)", "\r\r", RegexOptions.IgnoreCase);
				// Remove multiple tabs following a line break with just one tab
				result = Regex.Replace(result, "(\r)(\t)+", "\r\t", RegexOptions.IgnoreCase);
				// Initial replacement target string for line breaks
				string breaks = "\r\r\r";
				// Initial replacement target string for tabs
				string tabs = "\t\t\t\t\t";
				for (int index = 0; index < result.Length; index++) {
					result = result.Replace(breaks, "\r\r");
					result = result.Replace(tabs, "\t\t\t\t");
					breaks = breaks + "\r";
					tabs = tabs + "\t";
				}
				result = result.Replace("\r", "\n\r");
				// That's it.
				return result;
			}
			catch {
				return result;
			}
		}
		string pMessageId = null;
		public string MessageId {
			get {
				if (pMessageId != null)
					return pMessageId;
				var property = Header(HeaderKey.MessageID);
				if (property == null)
					return null;
				pMessageId = property.Value;
				return pMessageId;
			}
		}
		bool? isAlternative = null;
		public bool IsAlternative {
			get {
				if (isAlternative.HasValue)
					return isAlternative.Value;
				var property = Header(HeaderKey.ContentType);
				if (property == null)
					return false;
				isAlternative = property.Value.IndexOf(KB.I("multipart/alternative"), StringComparison.OrdinalIgnoreCase) >= 0;
				return isAlternative.Value;
			}
		}
		string pSentAsString = null;
		public String SentAsString {
			get {
				if (pSentAsString != null)
					return pSentAsString;
				var property = Header(HeaderKey.Date);
				pSentAsString = property?.Value;
				return pSentAsString;
			}
		}

		String pToNames;
		public string ToNames {
			get {
				if (!foundFrom)
					emailAddressParts();
				return pToNames;
			}
		}

		string pSubject;
		public string Subject {
			get {
				messageGet();
				if (pSubject == null)
					pSubject = Message.Subject;
				return pSubject;
			}
		}
		string pFromAdddress;
		bool foundFrom;
		public string FromAddress {
			get {
				if (!foundFrom)
					emailAddressParts();
				return pFromAdddress;
			}
		}
		String pFromName;
		public string FromName {
			get {
				if (!foundFrom)
					emailAddressParts();
				return pFromName;
			}
		}
		void emailAddressParts() {
			foundFrom = true;
			pFromAdddress = null;
			pFromName = null;
			messageGet();
			pToNames = Message.To;
			if (!string.IsNullOrWhiteSpace(Message.From)) {
				try {
					var from = Thinkage.Libraries.Mail.MailAddress(Message.From);
					pFromAdddress = from.Address;
					pFromName = from.DisplayName;
				}
				catch (FormatException) {
					pFromName = Message.From;
				}
			}
		}

		public DateTime Sent {
			get {
				messageGet();
				return Message.Date;
			}
		}
		public string Body {
			get {
				messageGet();
				if (!string.IsNullOrWhiteSpace(Message.Text))
					return Message.Text;
				else if (!string.IsNullOrWhiteSpace(Message.Html))
					return StripHTML(Message.Html);
				return "";
			}
		}
		public string HeaderText {
			get {
				messageGet();
				return string.Join(Environment.NewLine, Message.Headers.Select(h => h.Value));
			}
		}
		public bool Delete {
			get {
				return (PopMessage != null && PopMessage.Deleted)
					|| ImapMessage != null && ImapMessage.Deleted;
			}
			set {
				if (PopMessage != null)
					PopMessage.Deleted = value;
				else if (ImapMessage != null)
					ImapMessage.Update(value, ImapFlags.Deleted);
			}
		}
		int? pPreferredLanguage = null;
		bool hadLanguage = false;
		public int? PreferredLanguage {
			get {
				if (hadLanguage)
					return pPreferredLanguage;
				hadLanguage = true;
				var language = Header(HeaderKey.AcceptLanguage);
				if (language != null && !String.IsNullOrWhiteSpace(language.Value)) {
					string[] acceptableLanguages = language.Value.Split(',');
					foreach (string l in acceptableLanguages) {
						try {
							int lcid = System.Globalization.CultureInfo.GetCultureInfo(l).LCID;
							pPreferredLanguage = lcid;
						}
						catch (System.Globalization.CultureNotFoundException) {
						}
					}
				}
				return pPreferredLanguage;
			}
		}
		/// <summary>
		/// See if this message was AutoSubmitted such as out of office reply, or some other auto means. Note we only look for auto-replied to be ignored.
		/// </summary>
		public bool AutoSubmitted {
			get {
				var autosubmit = Header(HeaderKey.AutoSubmitted);
				return autosubmit != null && autosubmit.Value == KB.I("auto-replied");
			}
		}
		GeneralException pError = null;
		public GeneralException Error {
			get {
				if (Message != null || pError != null)
					return pError;
				messageGet();
				return pError;
			}
		}
		public MultipartContent Parts {
			get {
				messageGet();
				return Message.Parts;
			}
		}
		static public Encoding ContentEncoding(string charSet) {
			try {
				if (!string.IsNullOrWhiteSpace(charSet))
					return System.Text.Encoding.GetEncoding(charSet);
			}
			catch (System.Exception) {
				return System.Text.Encoding.ASCII;
			}
			return System.Text.Encoding.ASCII;
		}
		#region IDisposable Members
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (Message != null)
					Message.Dispose();
				Message = null;
			}
		}
		#endregion
		static public string EmailRequestToRFC822(XAFClient DB, bool encode, Guid EmailRequestId) {
			using (dsMB ds = new dsMB(DB)) {
				ds.EnsureDataTableExists(dsMB.Schema.T.EmailRequest, dsMB.Schema.T.EmailPart);
				var emailRequest = (dsMB.EmailRequestRow)DB.ViewAdditionalRow(ds, dsMB.Schema.T.EmailRequest, new SqlExpression(dsMB.Path.T.EmailRequest.F.Id).Eq(EmailRequestId));
				ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.EmailPart, new SqlExpression(dsMB.Path.T.EmailPart.F.EmailRequestID).Eq(EmailRequestId));
				using (var mailMessage = new MailMessage()) {
					var headers = emailRequest.F.MailHeader.Replace("\r", "").Split('\n');
					foreach (var h in headers)
						mailMessage.Headers.Add(h);
					List<dsMB.EmailPartRow> parts = new List<dsMB.EmailPartRow>();
					foreach (dsMB.EmailPartRow r in ds.T.EmailPart.Rows)
						parts.Add(r);
					if (parts.Count() == 0) {
						mailMessage.Text = emailRequest.F.MailMessage;
						return mailMessage.ToString(encode, true, null);
					}
					var fromGuid = new Dictionary<Guid, dsMB.EmailPartRow>();
					var tree = new Dictionary<Guid, List<dsMB.EmailPartRow>>();
					var top = new List<dsMB.EmailPartRow>();
					foreach (var p in parts.OrderBy(e => e.F.Order)) {
						fromGuid[p.F.Id] = p;
						if (p.F.ParentID == null)
							top.Add(p);
						else if (!tree.ContainsKey(p.F.ParentID.Value))
							tree[p.F.ParentID.Value] = new List<dsMB.EmailPartRow>() { p };
						else
							tree[p.F.ParentID.Value].Add(p);
					}
					Dictionary<Guid?, Multipart> groups = new Dictionary<Guid?, Multipart>();
					foreach (var n in tree) {
						var p = fromGuid[n.Key];
						if (p != null)
							groups[n.Key] = new Multipart(p.F.ContentType);
					}
					foreach (var n in tree) {
						var m = groups[n.Key];
						foreach (var l in n.Value)
							if (groups.ContainsKey(l.F.Id))
								m.Parts.Add(groups[l.F.Id]);
							else
								m.Parts.Add(makePart(l));
					}
					var pm = new MultipartContent();
					if (headers.Any(e => e.StartsWith(KB.I("Content-Type:"), true, System.Globalization.CultureInfo.InvariantCulture) && e.IndexOf(KB.I("multipart/alternative"), StringComparison.OrdinalIgnoreCase) >= 0))
						mailMessage.ContentType.MediaType = KB.I("multipart/alternative");
					mailMessage.Parts = pm;
					foreach (var l in top)
						if (groups.ContainsKey(l.F.Id))
							pm.Add(groups[l.F.Id]);
						else
							pm.Add(makePart(l));
					return mailMessage.ToString(encode, true, null);
				}
			}
		}
		static Part makePart(dsMB.EmailPartRow partRow) {
			Part p = null;
			var Headers = partRow.F.Header.Replace("\r", "").Split('\n');
			if (partRow.F.FileName != null) {
				p = new Attachment(partRow.F.Content, partRow.F.FileName);
				foreach (var h in Headers)
					p.Headers.Add(h);
			}
			else if (string.Compare(partRow.F.ContentType, KB.I("text/plain"), true) == 0)
				p = new Textpart(EmailMessage.ContentEncoding(partRow.F.ContentEncoding).GetString(partRow.F.Content));
			else if (string.Compare(partRow.F.ContentType, KB.I("text/html"), true) == 0)
				p = new Htmlpart(EmailMessage.ContentEncoding(partRow.F.ContentEncoding).GetString(partRow.F.Content));
			else {
				p = new Attachment(partRow.F.Content, partRow.F.Name);
				foreach (var h in Headers)
					p.Headers.Add(h);
			}
			p.ContentType.MediaType = partRow.F.ContentType;
			p.ContentType.Name = partRow.F.Name;
			return p;
		}
		public string MessageAsText(bool full) {
			messageGet();
			if (full)
				return Message.ToString(true, true, null);
			var m = new StringBuilder();
			var from = FromAddress == FromName ? FromAddress : new System.Net.Mail.MailAddress(FromAddress, FromName).ToString();
			m.AppendLine();
			m.AppendLine(Strings.Format(KB.K("To: {0}"), ToNames));
			m.AppendLine(Strings.Format(KB.K("From: {0}"), from));
			m.AppendLine(Strings.Format(KB.K("Subject: {0}"), Subject));
			m.AppendLine(Strings.Format(KB.K("Sent: {0}"), Sent));
			m.AppendLine();
			m.AppendLine(Body);
			return m.ToString();
		}

	}
}
