using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Implements the jQuery UI Helper extensions to the HtmlHelper class.
    /// </summary>
    public static class HtmlHelperExtensions
    {
        /// <summary>
        /// Returns a JQueryUI object which gives access to the jQuery UI HTML helpers.
        /// </summary>
        /// <typeparam name="TModel">The Model Type.</typeparam>
        /// <param name="htmlHelper">The HtmlHelper object.</param>
        /// <returns>A JQueryUI object.</returns>
        public static JQueryUI<TModel> JQueryUI<TModel>(this HtmlHelper<TModel> htmlHelper)
        {
            return new JQueryUI<TModel>(htmlHelper);
        }        
    }
}
