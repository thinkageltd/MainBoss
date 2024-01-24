namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a droppable widget.
    /// </summary>
    public class Droppable : HtmlElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Droppable"/> class.
        /// </summary>
        public Droppable()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Droppable"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Droppable(object htmlAttributes)
            : base("div", htmlAttributes)
        {
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Droppable);
        }

        /// <summary>
        /// Sets a jQuery selector or a JavaScript function name that defines the acceptable draggables.
        /// </summary>
        /// <param name="accept">The jQuery selector or JavaScript function name.</param>
        /// <returns>This droppable.</returns>
        public Droppable Accept(string accept)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => accept);
            m_HtmlAttributes[DroppableAttributeName.Accept] = accept;
            return this;
        }

        /// <summary>
        /// Sets the CSS class that will be added to the droppable while an acceptable draggable is being dragged.
        /// </summary>
        /// <param name="cssClassName">The CSS class.</param>
        /// <returns>This droppable.</returns>
        public Droppable ActiveClass(string cssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => cssClassName);
            m_HtmlAttributes[DroppableAttributeName.ActiveClass] = cssClassName;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to add the ui-droppable class to the droppable element.
        /// </summary>
        /// <param name="addClasses">If true, the ui-droppable class is added to the element.</param>
        /// <returns>This droppable.</returns>
        public Droppable AddClasses(bool addClasses)
        {
            m_HtmlAttributes[DroppableAttributeName.AddClasses] = addClasses;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the droppable is disabled.
        /// </summary>
        /// <param name="disabled">If true, the droppable is disabled.</param>
        /// <returns>This droppable.</returns>
        public Droppable Disabled(bool disabled)
        {
            m_HtmlAttributes[DroppableAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to prevent event propagation on nested droppables.
        /// </summary>
        /// <param name="greedy">If true, prevents event propagation on nested droppables.</param>
        /// <returns>This droppable.</returns>
        public Droppable Greedy(bool greedy)
        {
            m_HtmlAttributes[DroppableAttributeName.Greedy] = greedy;
            return this;
        }

        /// <summary>
        /// Sets the CSS class that will be added to the droppable while an acceptable draggable is being hovered.
        /// </summary>
        /// <param name="cssClassName">The CSS class.</param>
        /// <returns>This droppable.</returns>
        public Droppable HoverClass(string cssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => cssClassName);
            m_HtmlAttributes[DroppableAttributeName.HoverClass] = cssClassName;
            return this;
        }

        /// <summary>
        /// Sets the scope of the droppable.
        /// A draggable with the same scope value as a droppable will be accepted.
        /// </summary>
        /// <param name="scope">The scope name.</param>
        /// <returns>This droppable.</returns>
        public Droppable Scope(string scope)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => scope);
            m_HtmlAttributes[DroppableAttributeName.Scope] = scope;
            return this;
        }

        /// <summary>
        /// Sets the mode used for testing whether a draggable is over a droppable.
        /// </summary>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>This droppable.</returns>
        public Droppable Tolerance(DroppableTolerance tolerance)
        {
            m_HtmlAttributes[DroppableAttributeName.Tolerance] = tolerance.ToString().ToLowerInvariant();
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an accepted draggable starts dragging.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This droppable.</returns>
        public Droppable OnActivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DroppableAttributeName.OnActivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the droppable is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This droppable.</returns>
        public Droppable OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DroppableAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an accepted draggable stops dragging.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This droppable.</returns>
        public Droppable OnDeactivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DroppableAttributeName.OnDeactivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an accepted draggable is dropped over this droppable.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This droppable.</returns>
        public Droppable OnDrop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DroppableAttributeName.OnDrop] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an accepted draggable is dragged out this droppable.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This droppable.</returns>
        public Droppable OnOut(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DroppableAttributeName.OnOut] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when an accepted draggable is dragged over this droppable.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This droppable.</returns>
        public Droppable OnOver(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DroppableAttributeName.OnOver] = functionName;
            return this;
        }

        #endregion
    }
}
