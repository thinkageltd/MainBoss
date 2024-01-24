namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Writes the start tag of a new dialog to the response.
        /// </summary>
        /// <returns>The dialog builder.</returns>
        public DialogBuilder BeginDialog()
        {
            return Begin(new Dialog());
        }

        /// <summary>
        /// Writes the start tag of a new dialog to the response.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <returns>The dialog builder.</returns>
        public DialogBuilder BeginDialog(string title)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => title);
            return Begin(new Dialog().Title(title));
        }

        /// <summary>
        /// Writes the start tag of the specified dialog to the response.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <returns>The dialog builder.</returns>
        public DialogBuilder Begin(Dialog dialog)
        {
            Guard.ArgumentNotNull(() => dialog);
            return new DialogBuilder(m_HtmlHelper, dialog);
        }
    }
}
