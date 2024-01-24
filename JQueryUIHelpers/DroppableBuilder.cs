using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a droppable widget.
    /// </summary>
    public class DroppableBuilder : BuilderBase<Droppable>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DroppableBuilder"/> class
        /// using the specified droppable and writes the start tag of the droppable to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="droppable">The droppable.</param>
        public DroppableBuilder(HtmlHelper htmlHelper, Droppable droppable)
            : base(htmlHelper, droppable)
        {            
        }        
    }
}
