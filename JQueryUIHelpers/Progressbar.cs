using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a progressbar widget.
    /// </summary>
    public class Progressbar : Widget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Progressbar"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="value">The value of the progressbar.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public Progressbar(HtmlHelper htmlHelper, uint? value, object htmlAttributes)
            : base(htmlHelper, htmlAttributes)
        {
            if (value != null)
            {
                m_HtmlAttributes[ProgressbarAttributeName.Value] = value.Value;
            }
            else {
                m_HtmlAttributes[ProgressbarAttributeName.Value] = false;
            }
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Progressbar);
        }

        /// <summary>
        /// Sets a value indicating whether the progressbar is disabled.
        /// </summary>
        /// <param name="disabled">If true, the progressbar is disabled.</param>
        /// <returns>This progressbar.</returns>
        public Progressbar Disabled(bool disabled)
        {
            m_HtmlAttributes[ProgressbarAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the maximum value of the progressbar.
        /// </summary>
        /// <param name="max">The maximum value.</param>
        /// <returns>This progressbar.</returns>
        public Progressbar Max(uint max)
        {
            m_HtmlAttributes[ProgressbarAttributeName.Max] = max;
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the value of the progressbar is changed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This progressbar.</returns>
        public Progressbar OnChange(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ProgressbarAttributeName.OnChange] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the progressbar reaches the maximum value.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This progressbar.</returns>
        public Progressbar OnComplete(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ProgressbarAttributeName.OnComplete] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the progressbar is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This progressbar.</returns>
        public Progressbar OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ProgressbarAttributeName.OnCreate] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Returns the HTML-encoded representation of the progressbar.
        /// </summary>
        /// <returns>This datepicker.</returns>
        public override string ToHtmlString()
        {
            TagBuilder divBuilder = new TagBuilder("div");
            divBuilder.MergeAttributes(m_HtmlAttributes);
            return divBuilder.ToString();
        }
    }
}
