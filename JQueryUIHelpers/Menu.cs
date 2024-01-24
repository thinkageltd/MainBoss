using System;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a menu widget.
    /// </summary>
    public class Menu : Widget
    {
        private MenuItem[] m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        /// <param name="items">The menu items.</param>
        public Menu(HtmlHelper htmlHelper, object htmlAttributes, MenuItem[] items) :
            base(htmlHelper, htmlAttributes)
        {
            Guard.ArgumentNotNull(() => items);
            if (items.Length == 0)
            {
                throw new ArgumentException(StringResource.MenuWithoutItem, "items");
            }

            m_Items = items;

            m_HtmlAttributes[CommonHtmlAttributeName.JQueryUIType] = JQueryUIType.Menu;
        }

        /// <summary>
        /// Sets a value indicating whether the menu is disabled.
        /// </summary>
        /// <param name="disabled">If true, the menu is disabled.</param>
        /// <returns>This menu.</returns>
        public Menu Disabled(bool disabled)
        {
            m_HtmlAttributes[MenuAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the icon to use for submenus.
        /// </summary>
        /// <param name="submenuCssClassName">The CSS class name of the icon.</param>
        /// <returns>This menu.</returns>
        public Menu Icons(string submenuCssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => submenuCssClassName);
            m_HtmlAttributes[MenuAttributeName.Icons] = String.Format("{{\"submenu\":\"{0}\"}}", submenuCssClassName);
            return this;
        }

        /// <summary>
        /// Sets the position of submenus in relation to the associated parent menu item.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>This menu.</returns>
        public Menu Position(Position position)
        {
            Guard.ArgumentNotNull(() => position);
            m_HtmlAttributes[MenuAttributeName.Position] = position.ToString();
            return this;
        }

        /// <summary>
        /// Sets the ARIA roles used for the menu and menu items. 
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>This menu.</returns>
        public Menu Role(MenuRole role)
        {
            if (role == MenuRole.None)
            {
                m_HtmlAttributes[MenuAttributeName.Role] = "(null)";
            }
            else
            {
                m_HtmlAttributes[MenuAttributeName.Role] = role.ToString().StartLowerInvariant();
            }

            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the menu loses focus.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This datepicker.</returns>
        public Menu OnBlur(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[MenuAttributeName.OnBlur] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the menu is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This datepicker.</returns>
        public Menu OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[MenuAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the menu gains focus or when any menu item is activated.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This datepicker.</returns>
        public Menu OnFocus(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[MenuAttributeName.OnFocus] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when a menu item is selected.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This datepicker.</returns>
        public Menu OnSelect(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[MenuAttributeName.OnSelect] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Returns the HTML-encoded representation of the menu.
        /// </summary>
        /// <returns>The HTML-encoded representation of the menu.</returns>
        public override string ToHtmlString()
        {
            TagBuilder tagBuilder = new TagBuilder("ul");
            tagBuilder.MergeAttributes(m_HtmlAttributes);
            foreach (MenuItem item in m_Items)
            {
                tagBuilder.InnerHtml += item.ToHtmlString(m_HtmlHelper);
            }

            return tagBuilder.ToString(TagRenderMode.Normal);
        }
    }
}
