using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a hidden element specifying the datepicker culture.
    /// </summary>
    public class DatepickerCulture : Widget
    {
        private readonly CultureInfo m_Culture;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatepickerCulture"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="culture">The culture.</param>
        public DatepickerCulture(HtmlHelper htmlHelper, CultureInfo culture)
            : base(htmlHelper, null)
        {
            Guard.ArgumentNotNull(() => culture);

            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.DatepickerCulture);
            m_Culture = culture;
        }

        /// <summary>
        /// Returns the HTML-encoded representation of the datepicker culture.
        /// </summary>
        /// <returns>The HTML-encoded representation of the datepicker culture.</returns>
        public override string ToHtmlString()
        {
            return m_HtmlHelper.Hidden(JQueryUIType.DatepickerCulture, GetCultureName(), m_HtmlAttributes).ToHtmlString();
        }

        /// <summary>
        /// Returns the name of the culture.
        /// </summary>
        /// <returns>The culture name.</returns>
        private string GetCultureName()
        {
            string name = m_Culture.Name;
            if (DatepickerCultureList.SupportedCultures.Contains(name))
            {
                return name;
            }
            string neutralName = name.Split('-').First();
            if (DatepickerCultureList.SupportedCultures.Contains(neutralName))
            {
                return neutralName;
            }
            return String.Empty;
        }
    }
}
