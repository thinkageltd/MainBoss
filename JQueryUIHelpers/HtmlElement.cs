using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents an HTML element.
    /// </summary>
    public abstract class HtmlElement
    {
        /// <summary>
        /// The HTML tag.
        /// </summary>
        protected string m_Tag;

        /// <summary>
        /// The HTML attributes.
        /// </summary>
        protected readonly IDictionary<string, object> m_HtmlAttributes;        

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlElement"/> class.
        /// </summary>
        /// <param name="tag">The tag name of the element.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public HtmlElement(string tag, object htmlAttributes)
        {
            m_Tag = tag;
            m_HtmlAttributes = HtmlAttributesUtility.ObjectToHtmlAttributesDictionary(htmlAttributes);
        }

        /// <summary>
        /// Gets the start tag of the element.
        /// </summary>        
        public virtual string StartTag
        {
            get
            {
                TagBuilder divBuilder = new TagBuilder(m_Tag);
                divBuilder.MergeAttributes(m_HtmlAttributes);
                return divBuilder.ToString(TagRenderMode.StartTag);
            }
        }

        /// <summary>
        /// Gets the end tag of the element.
        /// </summary>
        public string EndTag
        {
            get
            {
                return String.Format("</{0}>", m_Tag);
            }
        }
    }
}
