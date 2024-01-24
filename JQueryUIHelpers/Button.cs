using System.Web.Mvc;
using System.Web;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a button widget.
    /// </summary>
    public class Button : ButtonBase
    {
        private readonly ButtonElement m_ButtonElement;
        private readonly ButtonType m_ButtonType;

        /// <summary>
        /// Initializes a new instance of the <see cref="Button"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="text">The text of the button.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        /// <param name="element">The button HTML element.</param>
        /// <param name="type">The type of the button.</param>
        public Button(HtmlHelper htmlHelper, string text, object htmlAttributes, ButtonElement element, ButtonType type)
            : base(htmlHelper, text, htmlAttributes)
        {
            m_ButtonElement = element;
            m_ButtonType = type;

            m_HtmlAttributes[CommonHtmlAttributeName.JQueryUIType] = JQueryUIType.Button;
        }

        /// <summary>
        /// Returns the HTML-encoded representation of the button.
        /// </summary>
        /// <returns>the HTML-encoded representation of the button.</returns>
        public override string ToHtmlString()
        {
            TagRenderMode renderMode = TagRenderMode.SelfClosing;
            m_HtmlAttributes["type"] = m_ButtonType.ToString().ToLowerInvariant();
            m_HtmlAttributes["value"] = m_Text;
            TagBuilder builder = new TagBuilder(m_ButtonElement.ToString().ToLowerInvariant());
            builder.MergeAttributes(m_HtmlAttributes);
            if (m_ButtonElement == ButtonElement.Button)
            {
                builder.InnerHtml = HttpUtility.HtmlEncode(m_Text);
                renderMode = TagRenderMode.Normal;
            }

            return builder.ToString(renderMode);
        }
    }
}
