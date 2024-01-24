using System;
using System.Text;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Implements the basic functionality common to button widgets.
    /// </summary>
    public abstract class ButtonBase : Widget
    {
        /// <summary>
        /// The text (value) of the button.
        /// </summary>
        protected readonly string m_Text;        

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonBase"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="text">The text of the button.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public ButtonBase(HtmlHelper htmlHelper, string text, object htmlAttributes)
            : base(htmlHelper, htmlAttributes)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => text);
            m_Text = text;
        }

        /// <summary>
        /// Sets the primary icon of the button.
        /// </summary>
        /// <param name="primaryIconCssClassName">The CSS class name of the primary icon.</param>
        /// <returns>This button.</returns>
        public ButtonBase Icons(string primaryIconCssClassName)
        {
            return Icons(primaryIconCssClassName, null, false);
        }

        /// <summary>
        /// Sets the primary icon of the button.
        /// </summary>
        /// <param name="primaryIconCssClassName">The CSS class name of the primary icon.</param>
        /// <param name="hideText">If true, only the icon is visible on the button.</param>
        /// <returns>This button.</returns>
        public ButtonBase Icons(string primaryIconCssClassName, bool hideText)
        {
            return Icons(primaryIconCssClassName, null, hideText);
        }

        /// <summary>
        /// Sets the primary and secondary icons of the button.
        /// </summary>
        /// <param name="primaryIconCssClassName">The CSS class name of the primary icon.</param>
        /// <param name="secondaryIconCssClassName">The CSS class name of the secondary icon.</param>
        /// <param name="hideText">If true, only the icons are visible on the button.</param>
        /// <returns>This button.</returns>
        public ButtonBase Icons(string primaryIconCssClassName, string secondaryIconCssClassName, bool hideText)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => primaryIconCssClassName);
            StringBuilder builder = new StringBuilder("{");
            builder.AppendFormat("\"primary\":\"{0}\"", primaryIconCssClassName);
            if (!String.IsNullOrWhiteSpace(secondaryIconCssClassName))
            {
                builder.AppendFormat(",\"secondary\":\"{0}\"", secondaryIconCssClassName);
            }

            builder.Append("}");
            m_HtmlAttributes[ButtonAttributeName.Icons] = builder.ToString();
            if (hideText)
            {
                m_HtmlAttributes[ButtonAttributeName.Text] = false;
            }

            return this;
        }

        /// <summary>
        /// Sets the label of the button.
        /// </summary>
        /// <param name="label">The label text.</param>
        /// <returns>This button.</returns>
        public ButtonBase Label(string label)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => label);
            m_HtmlAttributes[ButtonAttributeName.Label] = label;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the button is disabled.
        /// </summary>
        /// <param name="disabled">If true, the button is disabled.</param>
        /// <returns>This button.</returns>
        public ButtonBase Disabled(bool disabled)
        {
            m_HtmlAttributes[ButtonAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the button is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This button.</returns>
        public ButtonBase OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ButtonAttributeName.OnCreate] = functionName;
            return this;
        }
    }
}
