namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new tabs to the response.
        /// </summary>
        /// <returns>The tabs builder.</returns>
        public TabsBuilder BeginTabs()
        {
            return Begin(new Tabs());
        }

        /// <summary>
        /// Writes the start tag of the specified tabs to the response.
        /// </summary>
        /// <param name="tabs">The tabs.</param>
        /// <returns>The tabs builder.</returns>
        public TabsBuilder Begin(Tabs tabs)
        {
            Guard.ArgumentNotNull(() => tabs);
            return new TabsBuilder(m_HtmlHelper, tabs);
        }
    }
}
