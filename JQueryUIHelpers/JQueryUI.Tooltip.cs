namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Returns a tooltip.
        /// </summary>
        /// <returns>The datepicker.</returns>
        public Tooltip Tooltip()
        {
            return new Tooltip(m_HtmlHelper, null);
        }

        /// <summary>
        /// Returns a tooltip.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The datepicker.</returns>
        public Tooltip Tooltip(object htmlAttributes)
        {
            return new Tooltip(m_HtmlHelper, htmlAttributes);
        }

        /// <summary>
        /// Returns a tooltip with the specified selector.
        /// </summary>
        /// <param name="selector">A jQuery selector which specifies the scope of the tooltip.</param>
        /// <returns>The datepicker.</returns>
        public Tooltip Tooltip(string selector)
        {
            return new Tooltip(m_HtmlHelper, null, selector);
        }

        /// <summary>
        /// Returns a tooltip with the specified selector.
        /// </summary>
        /// <param name="selector">A jQuery selector which specifies the scope of the tooltip.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The datepicker.</returns>
        public Tooltip Tooltip(string selector, object htmlAttributes)
        {
            return new Tooltip(m_HtmlHelper, htmlAttributes, selector);
        }
    }
}
