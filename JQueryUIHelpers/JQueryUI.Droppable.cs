namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new droppable to the response.
        /// </summary>
        /// <returns>The droppable builder.</returns>
        public DroppableBuilder BeginDroppable()
        {
            return Begin(new Droppable());
        }

        /// <summary>
        /// Writes the start tag of the specified droppable to the response.
        /// </summary>
        /// <param name="droppable">The droppable.</param>
        /// <returns>The droppable builder.</returns>
        public DroppableBuilder Begin(Droppable droppable)
        {
            Guard.ArgumentNotNull(() => droppable);
            return new DroppableBuilder(m_HtmlHelper, droppable);
        }
    }
}
