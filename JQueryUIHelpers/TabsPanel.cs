using System;
using System.IO;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a tabs panel.
    /// </summary>
    public class TabsPanel : IDisposable
    {
        private string m_Tag;
        private bool m_Disposed = false;

        /// <summary>
        /// Gets or sets the TextWriter.
        /// </summary>
        protected TextWriter Writer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabsPanel"/> class.
        /// </summary>
        /// <param name="writer">The TextWriter.</param>
        /// <param name="tag">The HTML tag of the panel.</param>
        /// <param name="id">The id of the panel.</param>
        public TabsPanel(TextWriter writer, string tag, string id)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => tag);
            Guard.ArgumentNotNullOrWhiteSpace(() => id);
            Writer = writer;
            m_Tag = tag;
            TagBuilder tagBuilder = new TagBuilder(m_Tag);
            tagBuilder.Attributes.Add("id", id);
            Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TabsPanel"/> class. 
        /// </summary>
        ~TabsPanel()
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
                    Writer.Write("</{0}>", m_Tag);
                }

                m_Disposed = true;
            }
        }
    }
}
