using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a dialog widget.
    /// </summary>
    public class DialogBuilder : BuilderBase<Dialog>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogBuilder"/> class
        /// using the specified dialog and writes the start tag of the dialog to the response.
        /// </summary>        
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="dialog">The dialog.</param>
        public DialogBuilder(HtmlHelper htmlHelper, Dialog dialog)
            : base(htmlHelper, dialog)
        {            
        }
    }
}
