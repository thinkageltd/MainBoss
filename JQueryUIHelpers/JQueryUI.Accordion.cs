namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new accordion to the response.
        /// </summary>
        /// <returns>The accordion builder.</returns>
        public AccordionBuilder BeginAccordion()
        {
            return Begin(new Accordion());
        }

        /// <summary>
        /// Writes the start tag of the specified accordion to the response.
        /// </summary>
        /// <param name="accordion">The accordion.</param>
        /// <returns>The accordion builder.</returns>
        public AccordionBuilder Begin(Accordion accordion)
        {
            Guard.ArgumentNotNull(() => accordion);
            return new AccordionBuilder(m_HtmlHelper, accordion);
        }
    }
}
