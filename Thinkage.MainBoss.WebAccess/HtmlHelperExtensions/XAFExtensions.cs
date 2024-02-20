using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;
//using System.Web.Mvc.Resources;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using System.Collections.Generic;

namespace Thinkage.Web.Mvc.Html {
	public delegate TagBuilder GetHtmlIfObjectDefined<T>(T o);
	public delegate MvcHtmlString GetHtmlStringIfObjectDefined<T>(T o);

	// Note the CssClass used here are defined in Sites.css for the viewPanel css table class.
	public static class XAFExtensions {
		/// <summary>
		/// Return the raw non break space encoding.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <returns></returns>
		public static System.Web.IHtmlString NBSP(this HtmlHelper htmlHelper) {
			return htmlHelper.Raw("&nbsp;");
		}
		public static MvcHtmlString BackOperation(this HtmlHelper htmlHelper, string linkText, string uri) {
			TagBuilder row = new TagBuilder("div");
			row.AddCssClass("Action");
			TagBuilder a = new TagBuilder("a");
			a.MergeAttribute("href", uri);
			a.SetInnerText(linkText);
			row.InnerHtml = a.ToString();
			return MvcHtmlString.Create(row.ToString());
		}
		public static MvcHtmlString BackOperation(this HtmlHelper htmlHelper, string linkText, TempDataDictionary tData) {
			if (tData.ContainsKey("CancelURL"))
				return htmlHelper.BackOperation(linkText, tData.Peek("CancelURL").ToString());
			else if (tData.ContainsKey("HomeURL"))
				return htmlHelper.BackOperation(linkText, tData.Peek("HomeURL").ToString());
			else
				return MvcHtmlString.Empty;

		}
		public static TagBuilder ValueIfDefined<T>(this HtmlHelper htmlHelper, T o, GetHtmlIfObjectDefined<T> getter) {
			if (!EqualityComparer<T>.Default.Equals(o, default(T)))
				return getter(o);
			else
				return new TagBuilder("p"); // Do nothing value
		}
		public static MvcHtmlString ValueIfDefined<T>(this HtmlHelper htmlHelper, T o, GetHtmlStringIfObjectDefined<T> getter) {
			if (!EqualityComparer<T>.Default.Equals(o, default(T)))
				return getter(o);
			else
				return MvcHtmlString.Empty; // Do nothing value
		}

		/// <summary>
		/// Generate the table tr/td combination for a viewPanel table class with Label and Value for values that cannot be Hidden (Deleted)
		/// Supplied cssClasses apply to both
		/// </summary>
		public static MvcHtmlString LabelValueRow(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object value, bool valueIsHtml = false, params string[] cssClasses) {
			return LabelValueRow(htmlHelper, label, typeInfo, value, valueIsHtml, false, cssClasses);
		}
		public static MvcHtmlString LabelValueRow(this HtmlHelper htmlHelper, DBI_Path path, object value, bool valueIsHtml = false, params string[] cssClasses) {
			return LabelValueRow(htmlHelper, path.Key().Translate(), path.ReferencedColumn.EffectiveType, value, valueIsHtml, false, cssClasses);
		}
		public static MvcHtmlString LabelValueRowActiveLabel(this HtmlHelper htmlHelper, DBI_Path path, object value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(path.Key().Translate(), path.ReferencedColumn.EffectiveType, value, true, false, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelValueRowActiveLabel(this HtmlHelper htmlHelper, MvcHtmlString label, TypeInfo typeInfo, object value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(label.ToString(), typeInfo, value, true, false, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelValueRowActiveValue(this HtmlHelper htmlHelper, DBI_Path path, MvcHtmlString value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(path.Key().Translate(), path.ReferencedColumn.EffectiveType, value.ToString(), false, true, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelValueRowActiveValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, MvcHtmlString value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(label, typeInfo, value.ToString(), false, true, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelValueRowActiveValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(label, typeInfo, value, false, true, cssClasses, cssClasses));
		}
		/// <summary>
		/// Generate the table tr/td combination for a viewPanel table class with Label and single Value; Account for Hidden (Deleted) values requiring a different
		/// css class.
		/// </summary>
		public static MvcHtmlString LabelValueRow(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object value, bool valueIsHtml, bool deleted, params string[] cssClasses) {
			List<string> x = new List<string>(cssClasses);
			if (deleted)
				x.Add("Deleted");
			return MvcHtmlString.Create(labelValueRow(label, typeInfo, value, false, valueIsHtml, cssClasses, x.ToArray()));
		}
		/// <summary>
		/// Generate the table tr/td combination for a viewPanel table class with Label and Code / Desc Value; Account for Hidden (Deleted) values requiring a different
		/// css class.
		/// </summary>
		public static MvcHtmlString LabelValueRowCodeDesc(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object codeValue, object descValue, bool deleted, params string[] cssClasses) {
			List<string> x = new List<string>(cssClasses);
			if (deleted)
				x.Add("Deleted");
			return MvcHtmlString.Create(labelCodeDescRow(label, typeInfo, codeValue, descValue, false, false, cssClasses, x.ToArray()));
		}
		public static MvcHtmlString LabelValueRow(this HtmlHelper htmlHelper, DBI_Path path, TagBuilder tb, params string[] cssClasses) {
			return LabelValueRow(htmlHelper, path.Key().Translate(), tb, cssClasses);
		}
		public static MvcHtmlString LabelValueRow(this HtmlHelper htmlHelper, [Translated] string label, TagBuilder tb, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(label, StringTypeInfo.Universe, tb.ToString(), false, true, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelValueRowMultiLine(this HtmlHelper htmlHelper, DBI_Path path, [Invariant] string value, bool suppressIfEmpty) {
			return LabelValueRowMultiLine(htmlHelper, path.Key().Translate(), value, suppressIfEmpty);
		}
		public static MvcHtmlString LabelValueRowMultiLine(this HtmlHelper htmlHelper, [Translated] string label, [Invariant] string value, bool suppressIfEmpty) {
			if (suppressIfEmpty && String.IsNullOrEmpty(value))
				return MvcHtmlString.Empty;
			TagBuilder row = new TagBuilder("div");
			row.AddCssClass("LabelValueRowMultiLine");
			TagBuilder vTag = valueTag("div", StringTypeInfo.Universe, string.IsNullOrEmpty(value) ? String.Empty : value, false);
			vTag.AddCssClass("LabelValue");
			row.InnerHtml = labelTag("div", label, false, null) + vTag.ToString();
			return MvcHtmlString.Create(row.ToString());
		}
		public static MvcHtmlString LabelOnlyAsInnerHtml(this HtmlHelper htmlHelper, string label, string tag = "div", IEnumerable<string> cssClasses = null) {
			return MvcHtmlString.Create(labelTag(tag, label, true, cssClasses));
		}
		public static MvcHtmlString LabelOnly(this HtmlHelper htmlHelper, string label, string tag = "div", IEnumerable<string> cssClasses = null) {
			return MvcHtmlString.Create(labelTag(tag, label, false, cssClasses));
		}
		public static MvcHtmlString LabelOnly(this HtmlHelper htmlHelper, DBI_Path path, string tag = "div", IEnumerable<string> cssClasses = null) {
			return LabelOnly(htmlHelper, path.Key().Translate(), tag, cssClasses);
		}
		public static MvcHtmlString ValueOnlyAsUrlLink(this HtmlHelper htmlHelper, string tag, string linktext, string url) {
			TagBuilder aTag = new TagBuilder("a");
			aTag.MergeAttribute("href", System.Web.HttpUtility.UrlPathEncode(url));
			aTag.SetInnerText(linktext);
			return MvcHtmlString.Create(valueTag(tag, StringTypeInfo.Universe, aTag.ToString(), true).ToString());
		}
		public static MvcHtmlString ValueOnly(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value, bool deleted = false, string tag = "div", IEnumerable<string> cssClasses = null) {
			TagBuilder stag = valueTag(tag, typeInfo, value, false, cssClasses);
			if (deleted)
				stag.AddCssClass("Deleted");
			return MvcHtmlString.Create(stag.ToString());
		}
		public static MvcHtmlString WOResourceIdentifier(this HtmlHelper htmlHelper, MainBoss.WebAccess.Models.ResourceDescription resource) {
			TagBuilder row = new TagBuilder("div");
			row.AddCssClass("LabelValueRow");
			row.AddCssClass("ResourceLabel");
			object resourceCode = resource.Code;
			if (resource.ViewId.HasValue)
				resourceCode = htmlHelper.ActionLink(resource.Code, "View", resource.ViewController, new {
					id = resource.ViewId,
					resultMessage = ""
				}, htmlAttributes: null);
			TagBuilder codeDescTag = ValueOnlyWithValue2PopupTag(htmlHelper, StringTypeInfo.Universe, resourceCode, resource.Description, resource.IsHidden);
			codeDescTag.AddCssClass("Code");
			TagBuilder locationTag = ValueOnlyWithValue2PopupTag(htmlHelper, StringTypeInfo.Universe, resource.LocationShort, resource.LocationLong, resource.IsHiddenLocation);
			locationTag.AddCssClass("Desc");
			row.InnerHtml = codeDescTag.ToString() + locationTag.ToString();
			return MvcHtmlString.Create(row.ToString());
		}
		private static string labelCodeDescRow([Translated]string label, TypeInfo typeInfo, object value1, object value2, bool labelIsHtml, bool valueIsHtml, IEnumerable<string> labelCssClasses, IEnumerable<string> valueCssClasses) {
			TagBuilder row = new TagBuilder("div");
			row.AddCssClass("LabelValueRow");
			TagBuilder tag1 = valueTag("div", typeInfo, value1, valueIsHtml, addClass(valueCssClasses, "Code"));
			TagBuilder tag2 = valueTag("div", typeInfo, value2, valueIsHtml, addClass(valueCssClasses, "Description"));
			row.InnerHtml = labelTag("div", label, labelIsHtml, labelCssClasses) + valueTag("div", typeInfo, (tag1.ToString() + tag2.ToString()), true, addClass(valueCssClasses, "LabelCodeDescValue"));
			return row.ToString();
		}
		public static MvcHtmlString ValueOnly(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value1, object value2, bool deleted, string tag1 = "div", IEnumerable<string> value1CssClasses = null, string tag2 = "div", IEnumerable<string> value2CssClasses = null) {
			TagBuilder tagValue1 = valueTag(tag1, typeInfo, value1, false, value1CssClasses);
			if (deleted)
				tagValue1.AddCssClass("Deleted");
			TagBuilder tagValue2 = valueTag(tag2, typeInfo, value2, false, value2CssClasses);
			if (deleted)
				tagValue2.AddCssClass("Deleted");
			return MvcHtmlString.Create(tagValue1.ToString() + tagValue2.ToString());
		}
		public static TagBuilder ValueOnlyWithValue2PopupTag(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value1, object value2, bool deleted, string tag1 = "div") {
			TagBuilder tagValue1;
			if (value1 is MvcHtmlString htmlString)
				tagValue1 = valueTag(tag1, typeInfo, htmlString.ToString(), true);
			else
				tagValue1 = valueTag(tag1, typeInfo, value1, false);
			tagValue1.AddCssClass("WithPopup");
			if (deleted)
				tagValue1.AddCssClass("Deleted");
			if (!String.IsNullOrEmpty(value2 as string)) {
				TagBuilder tagValue2 = valueTag("span", typeInfo, value2, false);
				tagValue2.AddCssClass("ThePopup");
				if (deleted)
					tagValue2.AddCssClass("Deleted");
				tagValue1.InnerHtml = tagValue1.InnerHtml + tagValue2.ToString();
			}
			return tagValue1;
		}
		public static MvcHtmlString ValueOnlyWithValue2Popup(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value1, object value2, bool deleted, string tag1 = "div") {
			return MvcHtmlString.Create(ValueOnlyWithValue2PopupTag(htmlHelper, typeInfo, value1, value2, deleted, tag1).ToString());
		}
		private static string labelValueRow([Translated]string label, TypeInfo typeInfo, object value, bool labelIsHtml, bool valueIsHtml, IEnumerable<string> labelCssClasses, IEnumerable<string> valueCssClasses) {
			TagBuilder row = new TagBuilder("div");
			row.AddCssClass("LabelValueRow");
			TagBuilder vTag = valueTag("div", typeInfo, value, valueIsHtml, addClass(valueCssClasses, "LabelValue"));
			row.InnerHtml = labelTag("div", label, labelIsHtml, labelCssClasses) + vTag.ToString();
			return row.ToString();
		}
		private static string labelTag(string tag, [Translated]string label, bool labelIsHtml, IEnumerable<string> cssClasses = null) {
			TagBuilder labelTag = new TagBuilder(tag);
			labelTag.AddCssClass("Label");
			if (cssClasses != null)
				foreach (var s in cssClasses)
					labelTag.AddCssClass(s);
			if (labelIsHtml)
				labelTag.InnerHtml = label;
			else
				labelTag.SetInnerText(label);
			return labelTag.ToString();
		}
		private static TagBuilder valueTag(string tag, TypeInfo typeInfo, object value, bool valueIsHtml, IEnumerable<string> cssClasses = null) {
			TagBuilder valueTag = new TagBuilder(tag);
			if (cssClasses != null)
				foreach (var s in cssClasses)
					valueTag.AddCssClass(s);
			if (valueIsHtml)
				valueTag.InnerHtml = value.ToString();
			else {
				string valueAsString = typeInfo.GetTypeFormatter(Libraries.Application.InstanceFormatCultureInfo).Format(value);
				valueTag.AddCssClass(typeInfo.GetType().Name);
				valueTag.SetInnerText(valueAsString);
			}
			return valueTag;
		}
		private static IEnumerable<string> addClass(IEnumerable<string> c1, params string[] c2) {
			if (c1 == null)
				return c2;
			if (c2 == null)
				return c1;
			return c1.Concat(c2);
		}
	}
}