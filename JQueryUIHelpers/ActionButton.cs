using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a button widget generated from an anchor tag.
    /// </summary>
    public class ActionButton : ButtonBase
    {
        private readonly ActionDescription m_ActionDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionButton"/> class which invokes the specified action when clicked.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionDescription">The action invoked by the button.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public ActionButton(HtmlHelper htmlHelper, string text, ActionDescription actionDescription, object htmlAttributes)
            : base(htmlHelper, text, htmlAttributes)
        {
            Guard.ArgumentNotNull(() => actionDescription);
            m_ActionDescription = actionDescription;

            m_HtmlAttributes[CommonHtmlAttributeName.JQueryUIType] = JQueryUIType.Button;
        }

        /// <summary>
        /// Returns the HTML-encoded representation of the button.
        /// </summary>
        /// <returns>the HTML-encoded representation of the button.</returns>
        public override string ToHtmlString()
        {
            return m_HtmlHelper.ActionLink(m_Text, m_ActionDescription.ActionName, m_ActionDescription.ControllerName, m_ActionDescription.Protocol, m_ActionDescription.HostName, m_ActionDescription.Fragment, m_ActionDescription.RouteValueDictionary, m_HtmlAttributes)
                .ToHtmlString();
        }
    }
}
