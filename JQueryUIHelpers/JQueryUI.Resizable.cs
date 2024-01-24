namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new resizable to the response.
        /// </summary>
        /// <returns>The resizable builder.</returns>
        public ResizableBuilder BeginResizable()
        {
            return Begin(new Resizable());
        }
        
        /// <summary>
        /// Writes the start tag of the specified resizable to the response.
        /// </summary>
        /// <param name="resizable">The resizable.</param>
        /// <returns>The resizable builder.</returns>
        public ResizableBuilder Begin(Resizable resizable)
        {
            Guard.ArgumentNotNull(() => resizable);
            return new ResizableBuilder(m_HtmlHelper, resizable);
        }
    }
}
