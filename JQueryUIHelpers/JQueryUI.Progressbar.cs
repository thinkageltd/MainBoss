namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Returns a progressbar with the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The progressbar.</returns>
        public Progressbar Progressbar(uint? value)
        {
            return Progressbar(value, null);
        }

        /// <summary>
        /// Returns a progressbar with the specified value and HTML attributes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The progressbar.</returns>
        public Progressbar Progressbar(uint? value, object htmlAttributes)
        {
            return new Progressbar(m_HtmlHelper, value, htmlAttributes);
        }
    }
}
