using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Defines the base class for widgets.
    /// </summary>
    public abstract class Widget : IHtmlString
    {
        /// <summary>
        /// The HtmlHelper.
        /// </summary>
        protected readonly HtmlHelper m_HtmlHelper;

        /// <summary>
        /// A dictionary that contains the HTML attributes.
        /// </summary>
        protected readonly IDictionary<string, object> m_HtmlAttributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Widget"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public Widget(HtmlHelper htmlHelper, object htmlAttributes)
        {            
            m_HtmlHelper = htmlHelper;
            m_HtmlAttributes = HtmlAttributesUtility.ObjectToHtmlAttributesDictionary(htmlAttributes);            
        }

        /// <summary>
        /// Returns the HTML-encoded representation of the widget.
        /// </summary>
        /// <returns>the HTML-encoded representation of the widget.</returns>
        public abstract string ToHtmlString();        
    }
}
