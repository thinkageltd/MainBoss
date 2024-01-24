using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a resizable widget.
    /// </summary>
    public class ResizableBuilder : BuilderBase<Resizable>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResizableBuilder"/> class
        /// using the specified resizable and writes the start tag of the resizable to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="resizable">The resizable.</param>
        public ResizableBuilder(HtmlHelper htmlHelper, Resizable resizable)
            : base(htmlHelper, resizable)
        {            
        }       
    }
}
