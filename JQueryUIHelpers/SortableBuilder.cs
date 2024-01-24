using System;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a sortable widget.
    /// </summary>
    public class SortableBuilder : BuilderBase<Sortable>
    {
        private int m_Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableBuilder"/> class
        /// using the specified sortable and writes the start tag of the sortable to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="sortable">The sortable.</param>
        public SortableBuilder(HtmlHelper htmlHelper, Sortable sortable)
            : base(htmlHelper, sortable)
        {
        }

        /// <summary>
        /// Writes the start tag of a new sortable item to the response.
        /// </summary>
        /// <returns>The sortable item.</returns>
        public SortableItem BeginItem()
        {                                    
            return new SortableItem(m_Writer, null, null, 0, null);
        }        

        /// <summary>
        /// Writes the start tag of a new sortable item with the specified HTML attributes to the response.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The sortable item.</returns>
        public SortableItem BeginItem(object htmlAttributes)
        {
            return new SortableItem(m_Writer, null, null, 0, htmlAttributes);
        }

        /// <summary>
        /// Writes the start tag of a new sortable item with the specified value and HTML attributes to the response.
        /// </summary>
        /// <param name="value">The value of the item.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The sortable item.</returns>
        public SortableItem BeginItem(object value, object htmlAttributes)
        {
            if (String.IsNullOrWhiteSpace(m_Element.InternalName))
            {
                throw new InvalidOperationException(StringResource.SortableNameRequired);
            }

            return new SortableItem(m_Writer, m_Element.InternalName, value, m_Index++, htmlAttributes);
        } 
    }
}
