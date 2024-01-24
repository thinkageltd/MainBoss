namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a selectable widget.
    /// </summary>
    public class Selectable : HtmlElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Selectable"/> class.
        /// </summary>
        public Selectable()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Selectable"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Selectable(object htmlAttributes)
            : base("div", htmlAttributes)
        {            
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Selectable);
        }

        /// <summary>
        /// Sets a value indicating whether to refresh the position and size of each selectee at the beginning of each select operation.
        /// </summary>
        /// <param name="autoRefresh">If true, the position and size of each selectee is recalculated at the beginning of each select operation.</param>
        /// <returns>This selectable.</returns>
        public Selectable AutoRefresh(bool autoRefresh)
        {
            m_HtmlAttributes[SelectableAttributeName.AutoRefresh] = autoRefresh;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the elements which prevent selecting if the user starts on them.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This selectable.</returns>
        public Selectable Cancel(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SelectableAttributeName.Cancel] = selector;
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when selecting should start.
        /// Selecting will not start until after the mouse is moved beyond the specified duration.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This selectable.</returns>
        public Selectable Delay(uint duration)
        {
            m_HtmlAttributes[SelectableAttributeName.Delay] = duration;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the selectable is disabled.
        /// </summary>
        /// <param name="disabled">If true, the selectable is disabled.</param>
        /// <returns>This selectable.</returns>
        public Selectable Disabled(bool disabled)
        {
            m_HtmlAttributes[SelectableAttributeName.Disabled] = disabled;
            return this;
        }
        
        /// <summary>
        /// Sets the tolerance for when selecting should start.
        /// Selecting will not start until after the mouse is moved beyond the specified distance.
        /// </summary>
        /// <param name="distance">The distance in pixels.</param>
        /// <returns>This selectable.</returns>
        public Selectable Distance(uint distance)
        {
            m_HtmlAttributes[SelectableAttributeName.Distance] = distance;
            return this;
        }        

        /// <summary>
        /// Sets the jQuery selector that selects the elements that will be made selectees (able to be selected).
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This selectable.</returns>
        public Selectable Filter(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SelectableAttributeName.Filter] = selector;
            return this;
        }

        /// <summary>
        /// Sets the mode used for testing whether a selectee should be selected.
        /// </summary>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>This selectable.</returns>
        public Selectable Tolerance(SelectableTolerance tolerance)
        {
            m_HtmlAttributes[SelectableAttributeName.Tolerance] = tolerance.ToString().ToLowerInvariant();
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the selectable is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the end of the select operation, on each 
        /// element added to the selection.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnSelected(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnSelected] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered during the select operation, on each element 
        /// added to the selection.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnSelecting(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnSelecting] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the beginning of the select operation.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the end of the select operation.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnStop] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the end of the select operation, on each 
        /// element removed from the selection.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnUnselected(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnUnselected] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered during the select operation, on each element 
        /// removed from the selection.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This selectable.</returns>
        public Selectable OnUnselecting(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SelectableAttributeName.OnUnselecting] = functionName;
            return this;
        }

        #endregion
    }
}
