using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a draggable widget.
    /// </summary>
    public class DraggableBuilder : BuilderBase<Draggable>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DraggableBuilder"/> class
        /// using the specified draggable and writes the start tag of the draggable to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="draggable">The draggable.</param>
        public DraggableBuilder(HtmlHelper htmlHelper, Draggable draggable)
            : base(htmlHelper, draggable)
        {
        }
    }
}
