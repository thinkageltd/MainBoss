namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new sortable to the response.
        /// </summary>
        /// <returns>The sortable builder.</returns>
        public SortableBuilder BeginSortable()
        {
            return Begin(new Sortable());
        }

        /// <summary>
        /// Writes the start tag of the specified sortable to the response.
        /// </summary>
        /// <param name="sortable">The sortable.</param>
        /// <returns>The sortable builder.</returns>
        public SortableBuilder Begin(Sortable sortable)
        {
            Guard.ArgumentNotNull(() => sortable);
            return new SortableBuilder(m_HtmlHelper, sortable);
        }
    }
}
