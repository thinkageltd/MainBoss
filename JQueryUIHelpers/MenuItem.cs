using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a menu item.
    /// </summary>
    public class MenuItem
    {
        private string m_Text;
        private string m_Url;
        private MenuItem[] m_Items;
        private bool m_Enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuItem"/> class
        /// with the specified text, url and sub menu items.
        /// </summary>
        /// <param name="text">The text of the menu item.</param>
        /// <param name="url">The url.</param>
        /// <param name="items">The sub menu items.</param>
        public MenuItem(string text, string url, params MenuItem[] items)
            : this(text, url, true, items)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuItem"/> class
        /// with the specified text, url and sub menu items.
        /// </summary>
        /// <param name="text">The text of the menu item.</param>
        /// <param name="url">The url.</param>
        /// <param name="enabled">A value indicating whether the menu item is enabled.</param>
        /// <param name="items">The sub menu items.</param>
        public MenuItem(string text, string url, bool enabled, params MenuItem[] items)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => text);
            Guard.ArgumentNotNullOrWhiteSpace(() => url);
            Guard.ArgumentNotNull(() => items);

            m_Text = text;
            m_Url = url;
            m_Items = items;
            m_Enabled = enabled;
        }

        /// <summary>
        /// Returns the HTML-encoded representation of the menu item.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <returns>The HTML-encoded representation of the menu item.</returns>
        public string ToHtmlString(HtmlHelper htmlHelper)
        {
            TagBuilder aBuilder = new TagBuilder("a");
            aBuilder.Attributes.Add("href", m_Url);
            aBuilder.InnerHtml = htmlHelper.Encode(m_Text);
            TagBuilder liBuilder = new TagBuilder("li");
            liBuilder.InnerHtml = aBuilder.ToString();
            if (!m_Enabled)
            {
                liBuilder.AddCssClass("ui-state-disabled");
            }

            if (m_Items.Length > 0)
            {
                TagBuilder ulBuilder = new TagBuilder("ul");
                foreach (MenuItem item in m_Items)
                {
                    ulBuilder.InnerHtml += item.ToHtmlString(htmlHelper);
                }

                liBuilder.InnerHtml += ulBuilder.ToString();
            }

            return liBuilder.ToString();
        }
    }
}
