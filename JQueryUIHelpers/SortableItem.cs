using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a sortable item.
    /// </summary>
    public class SortableItem : IDisposable
    {
        private bool m_Disposed = false;

        /// <summary>
        /// Gets or sets the TextWriter.
        /// </summary>
        protected TextWriter Writer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableItem"/> class.
        /// </summary>
        /// <param name="writer">The TextWriter.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The value of the item.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public SortableItem(TextWriter writer, string name, object value, int index, object htmlAttributes)
        {            
            Writer = writer;
            IDictionary<string, object> htmlAttributesDictionary = 
                HtmlAttributesUtility.ObjectToHtmlAttributesDictionary(htmlAttributes);            
            TagBuilder tagBuilder = new TagBuilder("li");
            tagBuilder.MergeAttributes(htmlAttributesDictionary);
            Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));

            if (!String.IsNullOrWhiteSpace(name) && value != null)
            {
                TagBuilder hiddenBuilder = new TagBuilder("input");
                hiddenBuilder.Attributes.Add("type", "hidden");
                hiddenBuilder.Attributes.Add("id", HtmlHelper.GenerateIdFromName(name + "_" + index));
                hiddenBuilder.Attributes.Add("name", name);
                hiddenBuilder.Attributes.Add("value", value.ToString());
                Writer.Write(hiddenBuilder.ToString(TagRenderMode.SelfClosing));
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SortableItem"/> class. 
        /// </summary>
        ~SortableItem()
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
                    Writer.Write("</li>");
                }
                m_Disposed = true;
            }
        }
    }
}
