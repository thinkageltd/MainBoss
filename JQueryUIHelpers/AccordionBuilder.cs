using System;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds an accordion widget.
    /// </summary>
    public class AccordionBuilder : BuilderBase<Accordion>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccordionBuilder"/> class
        /// using the specified accordion and writes the start tag of the accordion to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="accordion">The accordion.</param>
        public AccordionBuilder(HtmlHelper htmlHelper, Accordion accordion)
            : base(htmlHelper, accordion)
        {
        }

        /// <summary>
        /// Writes the start tag of a new accordion panel to the response.
        /// </summary>
        /// <param name="title">The title of the panel.</param>
        /// <returns>The accordion panel.</returns>
        public AccordionPanel BeginPanel(string title)
        {
            return new AccordionPanel(m_Writer, m_Element.HeaderTag, m_Element.HeaderCssClass, title, String.Empty);
        }

        /// <summary>
        /// Writes the start tag of a new accordion panel to the response.
        /// </summary>
        /// <param name="title">The title of the panel.</param>
        /// <param name="anchor">The anchor of the panel.</param>
        /// <returns>The accordion panel.</returns>
        public AccordionPanel BeginPanel(string title, string anchor)
        {
            return new AccordionPanel(m_Writer, m_Element.HeaderTag, m_Element.HeaderCssClass, title, anchor);
        }        
    }
}
