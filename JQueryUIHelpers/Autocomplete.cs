using System;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents an autocomplete widget.
    /// </summary>
    public class Autocomplete : Widget
    {
        private readonly string m_Name;
        private readonly object m_Value;

        private readonly string m_AutocompleteId;
        private readonly string m_AutocompleteText;

        /// <summary>
        /// Initializes a new instance of the <see cref="Autocomplete"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="name">The name of the autocomplete.</param>
        /// <param name="source">The data source.</param>
        /// <param name="value">The value of the autocomplete.</param>
        /// <param name="autocompleteId">The id of the autocomplete when a hidden element is used to store the value.</param>
        /// <param name="autocompleteText">The text of the autocomplete when a hidden element is used to store the value.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public Autocomplete(HtmlHelper htmlHelper, string name, string source, object value, 
            string autocompleteId, string autocompleteText, object htmlAttributes)
            : base(htmlHelper, htmlAttributes)
        {            
            Guard.ArgumentNotNullOrWhiteSpace(() => source);
            m_Name = name;
            m_Value = value;
            m_AutocompleteId = autocompleteId;
            m_AutocompleteText = autocompleteText;

            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Autocomplete);
            m_HtmlAttributes[AutocompleteAttributeName.Source] = source;
        }

        /// <summary>
        /// Sets a selector that will be used to select the element the menu should be appended to.
        /// </summary>
        /// <param name="selector">A jQuery selector.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete AppendTo(string selector)
        {
            Guard.ArgumentNotNull(() => selector);
            m_HtmlAttributes[AutocompleteAttributeName.AppendTo] = selector;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to autofocus the first element in the list.
        /// </summary>
        /// <param name="autoFocus">If true, the first element in the list will be automatically focused.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete AutoFocus(bool autoFocus)
        {
            m_HtmlAttributes[AutocompleteAttributeName.AutoFocus] = autoFocus;
            return this;
        }

        /// <summary>
        /// Sets the delay the Autocomplete waits after a keystroke to activate itself.
        /// </summary>
        /// <param name="delay">The delay in milliseconds.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete Delay(uint delay)
        {
            m_HtmlAttributes[AutocompleteAttributeName.Delay] = delay;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the autocomplete is disabled.
        /// </summary>
        /// <param name="disabled">If true, the autocomplete is disabled.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete Disabled(bool disabled)
        {
            m_HtmlAttributes[AutocompleteAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets minimum number of characters a user has to type before the Autocomplete activates.
        /// </summary>
        /// <param name="minLength">The minimum number of characters.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete MinLength(int minLength)
        {
            Guard.ArgumentInRange(() => minLength, 0, Int32.MaxValue);
            m_HtmlAttributes[AutocompleteAttributeName.MinLength] = minLength;
            return this;
        }

        /// <summary>
        /// Sets the position of the suggestion list relative to the input field.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete Position(Position position)
        {
            Guard.ArgumentNotNull(() => position);
            m_HtmlAttributes[AutocompleteAttributeName.Position] = position.ToString();
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the field is blurred and the value has changed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnChange(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnChange] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the list is closed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnClose(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnClose] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the autocomplete is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered before focus is moved to an item in the list. 
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnFocus(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnFocus] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the suggestion list is opened.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnOpen(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnOpen] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered after a search completes, before the menu is shown.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnResponse(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnResponse] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered before a search request is started, after MinLength and Delay are met.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnSearch(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnSearch] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an item is selected from the list.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This autocomplete.</returns>
        public Autocomplete OnSelect(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AutocompleteAttributeName.OnSelect] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Returns the HTML-encoded representation of the autocomplete.
        /// </summary>
        /// <returns>The HTML-encoded representation of the autocomplete.</returns>
        public override string ToHtmlString()
        {
            string fullName = m_HtmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(m_Name);
            Guard.ArgumentNotNullOrWhiteSpace(fullName, "name");
            if (String.IsNullOrWhiteSpace(m_AutocompleteId))
            {
                return m_HtmlHelper.TextBox(m_Name, m_Value, m_HtmlAttributes).ToHtmlString();
            }
            else
            {                
                m_HtmlAttributes[AutocompleteAttributeName.HiddenValue] = HtmlHelper.GenerateIdFromName(fullName);
                string autocomplete = m_HtmlHelper.TextBox(m_AutocompleteId, m_AutocompleteText, m_HtmlAttributes).ToHtmlString();
                string hidden = m_HtmlHelper.Hidden(m_Name, m_Value).ToHtmlString();
                return autocomplete + hidden;
            }
        }
    }
}
