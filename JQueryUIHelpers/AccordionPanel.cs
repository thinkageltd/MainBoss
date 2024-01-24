using System;
using System.IO;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents an accordion panel.
    /// </summary>
    public class AccordionPanel : IDisposable
    {
        private bool m_Disposed = false;

        /// <summary>
        /// Gets or sets the TextWriter.
        /// </summary>
        protected TextWriter Writer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccordionPanel"/> class.
        /// </summary>
        /// <param name="writer">The TextWriter.</param>
        /// <param name="headerTag">The header tag name.</param>
        /// <param name="headerCssClass">The header CSS class name.</param>
        /// <param name="title">The title.</param>
        /// <param name="anchor">The anchor of the panel.</param>
        public AccordionPanel(TextWriter writer, string headerTag, string headerCssClass, string title, string anchor)
        {
            Guard.ArgumentNotNullOrEmpty(() => headerTag);
            Guard.ArgumentNotNullOrEmpty(() => title);
            Writer = writer;
            TagBuilder headerBuilder = new TagBuilder(headerTag);
            if (headerTag == "a")
            {
                headerBuilder.Attributes.Add("href", "#" + anchor);
                headerBuilder.InnerHtml = title;
            }
            else if (!String.IsNullOrWhiteSpace(anchor))
            {
                TagBuilder aBuilder = new TagBuilder("a");
                aBuilder.Attributes.Add("href", "#" + anchor);
                aBuilder.InnerHtml = title;
                headerBuilder.InnerHtml = aBuilder.ToString();
            }
            else
            {
                headerBuilder.InnerHtml = title;
            }

            if (!String.IsNullOrWhiteSpace(headerCssClass))
            {
                headerBuilder.AddCssClass(headerCssClass);
            }

            Writer.Write(headerBuilder.ToString());
            Writer.Write("<div>");
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AccordionPanel"/> class. 
        /// </summary>
        ~AccordionPanel()
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
            if (!m_Disposed)
            {
                if (isDisposing)
                {
                    Writer.Write("</div>");
                }
                m_Disposed = true;
            }
        }
    }
}
