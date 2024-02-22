using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
//using System.Web.Mvc.Resources;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;

namespace Thinkage.Web.Mvc.Html
{
	public delegate TagBuilder GetHtmlIfObjectDefined<T>(T o);
	public delegate MvcHtmlString GetHtmlStringIfObjectDefined<T>(T o);

	// Note the CssClass used here are defined in Sites.css for the viewPanel css table class.
	public static class XAFExtensions
	{
		/// <summary>
		/// Return the raw non break space encoding.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <returns></returns>
		public static System.Web.IHtmlString NBSP(this HtmlHelper htmlHelper)
		{
			return htmlHelper.Raw("&nbsp;");
		}
		public static  MvcHtmlString BackOperation(this HtmlHelper htmlHelper, string linkText, string uri)
		{
			TagBuilder a = new TagBuilder("a");
			a.MergeAttribute("href", uri);
			a.SetInnerText(linkText);
			a.AddCssClass("backOperation");
			return MvcHtmlString.Create(a.ToString());
		}
		public static MvcHtmlString BackOperation(this HtmlHelper htmlHelper, string linkText, TempDataDictionary tData) {
			if (tData.ContainsKey("CancelURL"))
				return htmlHelper.BackOperation(linkText, tData.Peek("CancelURL").ToString());
			else if (tData.ContainsKey("HomeURL"))
				return htmlHelper.BackOperation(linkText, tData.Peek("HomeURL").ToString());
			else
				return MvcHtmlString.Empty;

		}
		public static TagBuilder ValueIfDefined<T>(this HtmlHelper htmlHelper, T o, GetHtmlIfObjectDefined<T> getter)
		{
			if (o != null)
				return getter(o);
			else
				return new TagBuilder("p"); // Do nothing value
		}
		public static  MvcHtmlString ValueIfDefined<T>(this HtmlHelper htmlHelper, T o, GetHtmlStringIfObjectDefined<T> getter)
		{
			if (o != null)
				return getter(o);
			else
				return MvcHtmlString.Empty; // Do nothing value
		}

		/// <summary>
		/// Generate the table tr/td combination for a viewPanel table class with Label and Value for values that cannot be Hidden (Deleted)
		/// Supplied cssClasses apply to both
		/// </summary>
		public static MvcHtmlString LabelValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object value, params string[] cssClasses)
		{
			return LabelValue(htmlHelper, label, typeInfo, value, false, cssClasses);
		}
		public static MvcHtmlString LabelValue(this HtmlHelper htmlHelper, DBI_Path path, object value, params string[] cssClasses) {
			return LabelValue(htmlHelper, path.Key().Translate(), path.ReferencedColumn.EffectiveType, value, false, cssClasses);
		}
		public static MvcHtmlString ActiveLabelValue(this HtmlHelper htmlHelper, DBI_Path path, object value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(path.Key().Translate(), path.ReferencedColumn.EffectiveType, value, true, false, cssClasses, cssClasses));
		}
		public static MvcHtmlString ActiveLabelValue(this HtmlHelper htmlHelper, MvcHtmlString label, TypeInfo typeInfo, object value, params string[] cssClasses)
		{
			return MvcHtmlString.Create(labelValueRow(label.ToString(), typeInfo, value, true, false, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelActiveValue(this HtmlHelper htmlHelper, DBI_Path path, MvcHtmlString value, params string[] cssClasses) {
			return MvcHtmlString.Create(labelValueRow(path.Key().Translate(), path.ReferencedColumn.EffectiveType, value.ToString(), false, true, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelActiveValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, MvcHtmlString value, params string[] cssClasses)
		{
			return MvcHtmlString.Create(labelValueRow(label, typeInfo, value.ToString(), false, true, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelActiveValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object value, params string[] cssClasses)
		{
			return MvcHtmlString.Create(labelValueRow(label, typeInfo, value, false, true, cssClasses, cssClasses));
		}
		/// <summary>
		/// Generate the table tr/td combination for a viewPanel table class with Label and single Value; Account for Hidden (Deleted) values requiring a different
		/// css class.
		/// </summary>
		public static MvcHtmlString LabelValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object value, bool deleted, params string[] cssClasses)
		{
			List<string> x = new List<string>(cssClasses);
			if (deleted)
				x.Add("Deleted");
			return MvcHtmlString.Create(labelValueRow(label, typeInfo, value, false, false, cssClasses, x.ToArray()));
		}
		/// <summary>
		/// Generate the table tr/td combination for a viewPanel table class with Label and Code / Desc Value; Account for Hidden (Deleted) values requiring a different
		/// css class.
		/// </summary>
		public static MvcHtmlString LabelValue(this HtmlHelper htmlHelper, [Translated] string label, TypeInfo typeInfo, object codeValue, object descValue, bool deleted, params string[] cssClasses)
		{
			List<string> x = new List<string>(cssClasses);
			if (deleted)
				x.Add("Deleted");
			return MvcHtmlString.Create(labelCodeDescRow(label, typeInfo, codeValue, descValue, false, false, cssClasses, x.ToArray()));
		}
		public static MvcHtmlString LabelValue(this HtmlHelper htmlHelper, DBI_Path path, TagBuilder tb, params string[] cssClasses) {
			return LabelValue(htmlHelper, path.Key().Translate(), tb, cssClasses);
		}
		public static MvcHtmlString LabelValue(this HtmlHelper htmlHelper, [Translated] string label, TagBuilder tb, params string[] cssClasses)
		{
			return MvcHtmlString.Create(labelValueRow(label, StringTypeInfo.Universe, tb.ToString(), false, true, cssClasses, cssClasses));
		}
		public static MvcHtmlString LabelValueMultiLine(this HtmlHelper htmlHelper, DBI_Path path, [Invariant] string value, bool suppressIfEmpty) {
			return LabelValueMultiLine(htmlHelper, path.Key().Translate(), value, suppressIfEmpty);
		}
		public static MvcHtmlString LabelValueMultiLine(this HtmlHelper htmlHelper, [Translated] string label, [Invariant] string value, bool suppressIfEmpty)
		{
			if (suppressIfEmpty && String.IsNullOrEmpty(value))
				return MvcHtmlString.Empty;
			string[] css = new string[] { "MultiLine" };
			return  MvcHtmlString.Create(labelValueRow(label, StringTypeInfo.Universe, String.IsNullOrEmpty(value) ? String.Empty : value, false, false, new string[] {}, css));
		}
		public static MvcHtmlString LabelOnlyAsInnerHtml(this HtmlHelper htmlHelper, string label)
		{
			return MvcHtmlString.Create(labelTag(label, true));
		}
		public static MvcHtmlString LabelOnly(this HtmlHelper htmlHelper, string label)
		{
			return MvcHtmlString.Create(labelTag(label, false));
		}
		public static MvcHtmlString ValueOnly(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value)
		{
			return ValueOnly(htmlHelper, typeInfo, value, false);
		}
		public static MvcHtmlString ValueOnlyAsUrlLink(this HtmlHelper htmlHelper, string linktext, string url)
		{
			TagBuilder aTag = new TagBuilder("a");
			aTag.MergeAttribute("href", System.Web.HttpUtility.UrlPathEncode(url));
			aTag.SetInnerText(linktext);
			return MvcHtmlString.Create(valueTag(StringTypeInfo.Universe, aTag.ToString(), true).ToString());
		}
		public static MvcHtmlString ValueOnly(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value, bool deleted)
		{
			TagBuilder tag = valueTag(typeInfo, value, false);
			if (deleted)
				tag.AddCssClass("Deleted");
			return MvcHtmlString.Create(tag.ToString());
		}
		public static MvcHtmlString ValueOnly(this HtmlHelper htmlHelper, TypeInfo typeInfo, object value1, object value2, bool deleted)
		{
			TagBuilder tag = valueTag(typeInfo, value1, false);
			if (deleted)
				tag.AddCssClass("Deleted");
			TagBuilder tag1 = valueTag(typeInfo, value1, false);
			tag1.AddCssClass("V1");
			if (deleted)
				tag1.AddCssClass("Deleted");
			TagBuilder tag2 = valueTag(typeInfo, value2, false);
			tag2.AddCssClass("V2");
			if (deleted)
				tag2.AddCssClass("Deleted");
			return MvcHtmlString.Create(tag1.ToString() + tag2.ToString());
		}
		private static string labelCodeDescRow([Translated]string label, TypeInfo typeInfo, object value1, object value2, bool labelIsHtml, bool valueIsHtml, string[] labelCssClasses, string[] valueCssClasses)
		{
			TagBuilder row = new TagBuilder("tr");
			TagBuilder tag1 = valueTag(typeInfo, value1, valueIsHtml, valueCssClasses);
			tag1.AddCssClass("V1");
			TagBuilder tag2 = valueTag(typeInfo, value2, valueIsHtml, valueCssClasses);
			tag2.AddCssClass("V2");
			row.InnerHtml = labelTag(label, labelIsHtml, labelCssClasses) + tag1.ToString() + tag2.ToString();
			return row.ToString();
		}
		private static string labelValueRow([Translated]string label, TypeInfo typeInfo, object value, bool labelIsHtml, bool valueIsHtml, string[] labelCssClasses, string[] valueCssClasses)
		{
			TagBuilder row = new TagBuilder("tr");
			TagBuilder vTag = valueTag(typeInfo, value, valueIsHtml, valueCssClasses);
			vTag.MergeAttribute("colspan", "2");
			row.InnerHtml = labelTag(label, labelIsHtml, labelCssClasses) + vTag.ToString();
			return row.ToString();
		}
		private static string labelTag([Translated]string label, bool labelIsHtml, params string[] cssClasses)
		{
			TagBuilder labelTag = new TagBuilder("td");
			labelTag.AddCssClass("Label");
			foreach (var s in cssClasses)
				labelTag.AddCssClass(s);
			if (labelIsHtml)
				labelTag.InnerHtml = label;
			else
				labelTag.SetInnerText(label);
			return labelTag.ToString();
		}
		private static TagBuilder valueTag(TypeInfo typeInfo, object value, bool valueIsHtml, params string[] cssClasses)
		{
			TagBuilder valueTag = new TagBuilder("td");
			foreach( var s in cssClasses)
				valueTag.AddCssClass(s);
			string valueAsString = typeInfo.GetTypeFormatter(Libraries.Application.InstanceFormatCultureInfo).Format(value);
			if (valueIsHtml)
				valueTag.InnerHtml = valueAsString;
			else
				valueTag.SetInnerText(valueAsString);
			return valueTag;
		}
	}
}