namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Returns a menu with the specified menu items.
        /// </summary>
        /// <param name="items">The menu items.</param>
        /// <returns>The menu.</returns>
        public Menu Menu(params MenuItem[] items)
        {
            return new Menu(m_HtmlHelper, null, items);
        }

        /// <summary>
        /// Returns a menu with the specified menu items and HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <param name="items">An array of <see cref="MenuItem"/> objects.</param>
        /// <returns>The menu.</returns>
        public Menu Menu(object htmlAttributes, params MenuItem[] items)
        {
            return new Menu(m_HtmlHelper, htmlAttributes, items);
        }
    }
}
