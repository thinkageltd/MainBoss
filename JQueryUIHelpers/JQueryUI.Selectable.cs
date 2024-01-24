namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new selectable to the response.
        /// </summary>
        /// <returns>The selectable builder.</returns>
        public SelectableBuilder BeginSelectable()
        {
            return Begin(new Selectable());
        }

        /// <summary>
        /// Writes the start tag of the specified selectable to the response.
        /// </summary>
        /// <param name="selectable">The selectable.</param>
        /// <returns>The selectable builder.</returns>
        public SelectableBuilder Begin(Selectable selectable)
        {
            Guard.ArgumentNotNull(() => selectable);
            return new SelectableBuilder(m_HtmlHelper, selectable);
        }
    }
}
