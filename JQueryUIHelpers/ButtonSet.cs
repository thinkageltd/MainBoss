using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.IO;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a button set.
    /// </summary>
    public class ButtonSet : IDisposable
    {
        private readonly IDictionary<string, object> m_HtmlAttributes;

        private bool disposed = false;

        /// <summary>
        /// Gets or sets the TextWriter.
        /// </summary>
        protected TextWriter Writer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonSet"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public ButtonSet(HtmlHelper htmlHelper, object htmlAttributes)
        {
            Writer = htmlHelper.ViewContext.Writer;
            m_HtmlAttributes = HtmlAttributesUtility.ObjectToHtmlAttributesDictionary(htmlAttributes);
            
            m_HtmlAttributes[CommonHtmlAttributeName.JQueryUIType] = JQueryUIType.ButtonSet;

            TagBuilder divBuilder = new TagBuilder("div");
            divBuilder.MergeAttributes(m_HtmlAttributes);
            Writer.Write(divBuilder.ToString(TagRenderMode.StartTag));
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ButtonSet"/> class. 
        /// </summary>
        ~ButtonSet()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes of this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Writes the end tag of the panel to the response.
        /// </summary>
        /// <param name="isDisposing">Indicates whether Dispose is called by Dispose or the Destructor.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!disposed)
            {
                if (isDisposing)
                {
                    Writer.Write("</div>");
                }
                disposed = true;
            }
        }
    }
}
