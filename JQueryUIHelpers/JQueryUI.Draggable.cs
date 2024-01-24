namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new draggable to the response.
        /// </summary>
        /// <returns>The draggable builder.</returns>
        public DraggableBuilder BeginDraggable()
        {
            return Begin(new Draggable());
        }

        /// <summary>
        /// Writes the start tag of the specified draggable to the response.
        /// </summary>
        /// <param name="draggable">The draggable.</param>
        /// <returns>The draggable builder.</returns>
        public DraggableBuilder Begin(Draggable draggable)
        {
            Guard.ArgumentNotNull(() => draggable);
            return new DraggableBuilder(m_HtmlHelper, draggable);
        }
    }
}
