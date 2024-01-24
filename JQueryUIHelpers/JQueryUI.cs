using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Implements the factory methods which create the widgets.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public partial class JQueryUI<TModel>
    {        
        /// <summary>
        /// The HtmlHelper.
        /// </summary>
        protected readonly HtmlHelper<TModel> m_HtmlHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="JQueryUI{TModel}"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        public JQueryUI(HtmlHelper<TModel> htmlHelper)
        {
            m_HtmlHelper = htmlHelper;
        }
    }
}
