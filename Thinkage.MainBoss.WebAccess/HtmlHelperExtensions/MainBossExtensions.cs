namespace Thinkage.Web.Mvc.Html {
	using System.Web.Mvc;
	using System.Web.Mvc.Html;
	using System;
	using Thinkage.Libraries.Translation;
	using System.Reflection;
	using Thinkage.Libraries.TypeInfo;
	using System.Collections.Generic;
	using System.Web.Routing;
	using Thinkage.Libraries.XAF.Database.Layout;
	/// <summary>
	/// Extensions for building Html SPECIFIC to MainBoss
	/// </summary>
	public static class MainBossExtensions {
		public static MvcHtmlString ResultMessage(this HtmlHelper htmlHelper, object message) {
			if (message == null)
				return null;
			string s = (string)message;
			if (String.IsNullOrEmpty(s))
				return null;
			TagBuilder divTag = new TagBuilder("div");
			divTag.AddCssClass("ResultMessage");
			divTag.SetInnerText(s); // TagBuilder encodes in SetInnerText
			return MvcHtmlString.Create(divTag.ToString());
		}
		public static TagBuilder ContactValue(this HtmlHelper htmlHelper, [Invariant] string name, [Invariant] string phone, [Invariant]string email, bool deleted) {
			TagBuilder nameTag = new TagBuilder("div");
			nameTag.AddCssClass("Name");
			nameTag.SetInnerText(name);
			TagBuilder phoneTag = new TagBuilder("div");
			phoneTag.AddCssClass("Phone");
			phoneTag.SetInnerText(phone);
			TagBuilder emailTag = new TagBuilder("div");
			emailTag.AddCssClass("Email");
			emailTag.InnerHtml = htmlHelper.Mailto(htmlHelper.Encode(email ?? "").Replace("@", "&#8203;@"), email ?? "")?.ToString(); // allow to fold at @ sign
			if (deleted) {
				nameTag.AddCssClass("Deleted");
				phoneTag.AddCssClass("Deleted");
				emailTag.AddCssClass("Deleted");
			}
			TagBuilder requestorTag = new TagBuilder("div");
			requestorTag.AddCssClass("contactPanel");
			requestorTag.InnerHtml = nameTag.ToString() + phoneTag.ToString() + emailTag.ToString();
			return requestorTag;
		}
		public static MvcHtmlString CodeValueForBrowsePanel(this HtmlHelper htmlHelper, object instance) {
			GetCodeHiddenMembers(instance, out string code, out string desc, out object hidden);
			return htmlHelper.ValueOnlyWithValue2Popup(StringTypeInfo.Universe, code, desc, hidden != null, KB.I("td"));
		}
		public static MvcHtmlString ValueOnlyForBrowsePanel(this HtmlHelper htmlHelper, Libraries.TypeInfo.TypeInfo typeInfo, object instance) {
			return htmlHelper.ValueOnly(typeInfo, instance, false, KB.I("td"));
		}
		public static MvcHtmlString CodeDescValue(this HtmlHelper htmlHelper, object instance) {
			if (instance == null)
				return null;
			GetCodeHiddenMembers(instance, out string code, out string desc, out object hidden);
			return htmlHelper.ValueOnly(StringTypeInfo.Universe, code, desc, hidden != null);
		}
		public static MvcHtmlString CodeLabelValue(this HtmlHelper htmlHelper, [Translated]string label, object instance) {
			if (instance == null)
				return null;

			GetCodeHiddenMembers(instance, out string code, out _, out object hidden);
			return htmlHelper.LabelValueRow(label, StringTypeInfo.Universe, code, hidden != null);
		}
		public static MvcHtmlString LabelValueRowCodeDesc(this HtmlHelper htmlHelper, DBI_Path path, object instance) {
			if (instance == null)
				return null;
			GetCodeHiddenMembers(instance, out string code, out string desc, out object hidden);
			return htmlHelper.LabelValueRowCodeDesc(path.Key().Translate(), StringTypeInfo.Universe, code, desc, hidden != null);
		}
		public static MvcHtmlString LabelValueRowCodeDesc(this HtmlHelper htmlHelper, [Translated]string label, object instance) {
			if (instance == null)
				return null;
			GetCodeHiddenMembers(instance, out string code, out string desc, out object hidden);
			return htmlHelper.LabelValueRowCodeDesc(label, StringTypeInfo.Universe, code, desc, hidden != null);
		}
		/// <summary>
		/// Pass the Deleted flag explicitly when it is necessary (e.g. The Hidden value is in some base record, not the derived record passed in as instance)
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="label"></param>
		/// <param name="instance"></param>
		/// <param name="explicitDeleted"></param>
		/// <returns></returns>
		public static MvcHtmlString CodeLabelValue(this HtmlHelper htmlHelper, [Translated]string label, object instance, bool explicitDeleted) {
			GetCodeHiddenMembers(instance, out string code, out string desc, out object hidden);
			return htmlHelper.LabelValueRowCodeDesc(label, StringTypeInfo.Universe, code, desc, hidden != null || explicitDeleted);
		}
		private static void GetCodeHiddenMembers(object instance, out string code, out string desc, out object hidden) {
			if (instance != null) {
				var modelType = instance.GetType();
				PropertyInfo CodeMember = modelType.GetProperty("Code");
				PropertyInfo DescMember = modelType.GetProperty("Desc");
				PropertyInfo HiddenMember = modelType.GetProperty("Hidden");
				object v = CodeMember.GetValue(instance, null);
				if (v is SimpleKey)
					code = ((SimpleKey)v).Translate();
				else
					code = (string)v;
				v = DescMember.GetValue(instance, null);
				if (v is SimpleKey)
					desc = ((SimpleKey)v).Translate();
				else
					desc = (string)v;
				if (HiddenMember == null)
					hidden = null;
				else
					hidden = HiddenMember.GetValue(instance, null);
			}
			else {
				code = "";
				desc = "";
				hidden = null;
			}
		}
		/// <summary>
		/// Generate a LabelValueRow pair where the Value part 'may' be an active link to another View. Only non-hidden values are eligible to be active
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="label"></param>
		/// <param name="instance">Entity containing the value linked to; if null, then there is nothing to link to</param>
		/// <param name="linkText">delegate to get text to display for the link (usually the Code of the field)</param>
		/// <param name="linkGudd">delegate to get the Guid of the record to be viewed if the link is followed</param>
		/// <param name="explicitDeleted">delegate to get the deleted condition of the value</param>
		/// <returns></returns>
		public delegate Guid GetLinkGuid<T>(T instance);
		public delegate bool IsDeleted<T>(T instance);
		public delegate string GetLinkText<T>(T instance);
		public static MvcHtmlString LabelActionValue<T>(this HtmlHelper htmlHelper, DBI_Path path, T instance, GetLinkText<T> linkText, GetLinkGuid<T> linkGuid, IsDeleted<T> explicitDeleted) {
			return LabelActionValue<T>(htmlHelper, path.Key().Translate(), instance, linkText, linkGuid, explicitDeleted);
		}
		public static MvcHtmlString LabelActionValue<T>(this HtmlHelper htmlHelper, [Translated]string label, T instance, GetLinkText<T> linkText, GetLinkGuid<T> linkGuid, IsDeleted<T> explicitDeleted) {
			if (!EqualityComparer<T>.Default.Equals(instance, default)) {
				if (explicitDeleted(instance))
					return CodeLabelValue(htmlHelper, label, instance, explicitDeleted(instance));
				else
					return htmlHelper.LabelValueRowActiveValue(label, StringTypeInfo.Universe, htmlHelper.ActionLink(linkText(instance), "View", "Unit", new {
						id = linkGuid(instance).ToString()
					}, new {
						@class = "ValueLink"
					}));
			}
			else
				return htmlHelper.LabelValueRow(label, StringTypeInfo.Universe, "");
		}
	}
	// Copied from Microsoft.Web.Mvc1.0
	public static class MailToExtensions {
		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress) {
			return Mailto(helper, linkText, emailAddress, null, null, null, null, (IDictionary<string, object>)null);
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, object htmlAttributes) {
			return Mailto(helper, linkText, emailAddress, null, null, null, null, htmlAttributes);
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, IDictionary<string, object> htmlAttributes) {
			return Mailto(helper, linkText, emailAddress, null, null, null, null, htmlAttributes);
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, string subject) {
			return Mailto(helper, linkText, emailAddress, subject, null, null, null, (IDictionary<string, object>)null);
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, string subject, object htmlAttributes) {
			return Mailto(helper, linkText, emailAddress, subject, null, null, null, htmlAttributes);
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, string subject, IDictionary<string, object> htmlAttributes) {
			return Mailto(helper, linkText, emailAddress, subject, null, null, null, htmlAttributes);
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, string subject, string body, string cc, string bcc, object htmlAttributes) {
			return Mailto(helper, linkText, emailAddress, subject, body, cc, bcc, new RouteValueDictionary(htmlAttributes));
		}

		public static MvcHtmlString Mailto(this HtmlHelper helper, string linkText, string emailAddress, string subject,
			string body, string cc, string bcc, IDictionary<string, object> htmlAttributes) {
			if (emailAddress == null) {
				throw new ArgumentNullException(nameof(emailAddress)); // TODO: Resource message
			}

			string mailToUrl = "mailto:" + emailAddress;

			List<string> mailQuery = new List<string>();
			if (!String.IsNullOrEmpty(subject)) {
				mailQuery.Add("subject=" + helper.Encode(subject));
			}

			if (!String.IsNullOrEmpty(cc)) {
				mailQuery.Add("cc=" + helper.Encode(cc));
			}

			if (!String.IsNullOrEmpty(bcc)) {
				mailQuery.Add("bcc=" + helper.Encode(bcc));
			}

			if (!String.IsNullOrEmpty(body)) {
				string encodedBody = helper.Encode(body);
				encodedBody = encodedBody.Replace(Environment.NewLine, "%0A");
				mailQuery.Add("body=" + encodedBody);
			}

			string query = string.Empty;
			for (int i = 0; i < mailQuery.Count; i++) {
				query += mailQuery[i];
				if (i < mailQuery.Count - 1) {
					query += "&";
				}
			}
			if (query.Length > 0) {
				mailToUrl += "?" + query;
			}

			TagBuilder mailtoAnchor = new TagBuilder("a");
			mailtoAnchor.MergeAttribute("href", mailToUrl);
			mailtoAnchor.MergeAttributes(htmlAttributes, true);
			mailtoAnchor.InnerHtml = linkText ?? throw new ArgumentNullException(nameof(linkText));
			return MvcHtmlString.Create(mailtoAnchor.ToString());
		}
	}
}