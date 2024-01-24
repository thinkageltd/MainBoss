using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a selectable widget.
    /// </summary>
    public class SelectableBuilder : BuilderBase<Selectable>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableBuilder"/> class
        /// using the specified selectable and writes the start tag of the selectable to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="selectable">The selectable.</param>
        public SelectableBuilder(HtmlHelper htmlHelper, Selectable selectable)
            : base(htmlHelper, selectable)
        {            
        }        
    }
}
