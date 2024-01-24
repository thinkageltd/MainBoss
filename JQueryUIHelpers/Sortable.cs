using System;
using System.Globalization;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a sortable widget.
    /// </summary>
    public class Sortable : HtmlElement
    {
        internal string InternalName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sortable"/> class.
        /// </summary>
        public Sortable() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sortable"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Sortable(object htmlAttributes)
            : base("ul", htmlAttributes)
        {            
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Sortable);
        }

        /// <summary>
        /// Sets the jQuery selector that selects the element where the helper 
        /// that moves with the mouse is being appended to during the drag.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This sortable.</returns>
        public Sortable AppendTo(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SortableAttributeName.AppendTo] = selector;
            return this;
        }

        /// <summary>
        /// Sets the axis where the items can be dragged.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <returns>This sortable.</returns>
        public Sortable Axis(Axis axis)
        {
            m_HtmlAttributes[SortableAttributeName.Axis] = axis.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the elements which prevent sorting if the user starts on them.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This sortable.</returns>
        public Sortable Cancel(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SortableAttributeName.Cancel] = selector;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects other sortables which can receive items from this sortable.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This sortable.</returns>
        public Sortable ConnectWith(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SortableAttributeName.ConnectWith] = selector;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the element that constrains dragging.
        /// The specified element must have a calculated width and height.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This sortable.</returns>
        public Sortable Containment(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SortableAttributeName.Containment] = selector;
            return this;
        }

        /// <summary>
        /// Sets the element that constrains dragging.
        /// The specified element must have a calculated width and height.
        /// </summary>
        /// <param name="containment">The element.</param>
        /// <returns>This sortable.</returns>
        public Sortable Containment(Containment containment)
        {
            m_HtmlAttributes[SortableAttributeName.Containment] = containment.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the cursor that is being shown while sorting.
        /// </summary>
        /// <param name="cursor">The cursor.</param>
        /// <returns>This sortable.</returns>
        public Sortable Cursor(Cursor cursor)
        {
            m_HtmlAttributes[SortableAttributeName.Cursor] = CursorUtility.GetCursorText(cursor);
            return this;
        }

        /// <summary>
        /// Sets the cursor position relative to the element being dragged.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>This sortable.</returns>
        public Sortable CursorAt(CursorPosition position)
        {
            Guard.ArgumentNotNull(() => position);
            m_HtmlAttributes[SortableAttributeName.CursorAt] = position.ToJsonString();
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when sorting should start.
        /// Sorting will not start until after the mouse is moved beyond the specified duration.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This sortable.</returns>
        public Sortable Delay(uint duration)
        {
            m_HtmlAttributes[SortableAttributeName.Delay] = duration;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the sortable is disabled.
        /// </summary>
        /// <param name="disabled">If true, the sortable is disabled.</param>
        /// <returns>This sortable.</returns>
        public Sortable Disabled(bool disabled)
        {
            m_HtmlAttributes[SortableAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when sorting should start.
        /// Sorting will not start until after the mouse is moved beyond the specified distance.
        /// </summary>
        /// <param name="distance">The distance in pixels.</param>
        /// <returns>This sortable.</returns>
        public Sortable Distance(uint distance)
        {
            m_HtmlAttributes[SortableAttributeName.Distance] = distance;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether items form this sortable can be dropped to an empty sortable.
        /// </summary>
        /// <param name="dropOnEmpty">If true, items can be dropped to an empty sortable.</param>
        /// <returns>This sortable.</returns>
        public Sortable DropOnEmpty(bool dropOnEmpty)
        {
            m_HtmlAttributes[SortableAttributeName.DropOnEmpty] = dropOnEmpty;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the helper is forced to have a size.
        /// </summary>
        /// <param name="forceHelperSize">If true, the helper is forced to have a size.</param>
        /// <returns>This sortable.</returns>
        public Sortable ForceHelperSize(bool forceHelperSize)
        {
            m_HtmlAttributes[SortableAttributeName.ForceHelperSize] = forceHelperSize;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the placeholder is forced to have a size.
        /// </summary>
        /// <param name="forcePlaceholderSize">If true, the placeholder is forced to have a size.</param>
        /// <returns>This sortable.</returns>
        public Sortable ForcePlaceholderSize(bool forcePlaceholderSize)
        {
            m_HtmlAttributes[SortableAttributeName.ForcePlaceholderSize] = forcePlaceholderSize;
            return this;
        }

        /// <summary>
        /// Snaps the helper to a grid specified by the x and y values.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>This sortable.</returns>
        public Sortable Grid(uint x, uint y)
        {
            m_HtmlAttributes[SortableAttributeName.Grid] = String.Format("{0},{1}", x, y);
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the elements which can start sorting.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This sortable.</returns>
        public Sortable Handle(string selector)
        {
          Guard.ArgumentNotNullOrWhiteSpace(() => selector);
          m_HtmlAttributes[SortableAttributeName.Handle] = selector;
          return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that returns the helper element.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable Helper(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.Helper] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the helper element.
        /// </summary>
        /// <param name="helper">The helper element.</param>
        /// <returns>This sortable.</returns>
        public Sortable Helper(DragHelper helper)
        {
            m_HtmlAttributes[SortableAttributeName.Helper] = helper.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the helper element.
        /// </summary>
        /// <param name="helper">The helper element.</param>
        /// <returns>This sortable.</returns>
        [Obsolete("Use the Sortable.Helper(DragHelper helper) method.")]
        public Sortable Helper(SortableHelper helper)
        {
            m_HtmlAttributes[SortableAttributeName.Helper] = helper.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the sortable elements inside this sortable.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This sortable.</returns>
        public Sortable Items(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[SortableAttributeName.Items] = selector;
            return this;
        }

        /// <summary>
        /// Sets the opacity of the helper element.
        /// </summary>
        /// <param name="opacity">The opacity from 0.01 to 1.</param>
        /// <returns>This sortable.</returns>
        public Sortable Opacity(double opacity)
        {
            Guard.ArgumentInRange(() => opacity, 0.01, 1.0);
            m_HtmlAttributes[SortableAttributeName.Opacity] = opacity.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        /// <summary>
        /// Sets the CSS class that is added to the placeholder element.
        /// </summary>
        /// <param name="cssClassName">The CSS class.</param>
        /// <returns>This sortable.</returns>
        public Sortable Placeholder(string cssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => cssClassName);
            m_HtmlAttributes[SortableAttributeName.Placeholder] = cssClassName;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the item is moved to its new position with an animation.
        /// </summary>
        /// <param name="revert">If true, the item is moved to its new position with an animation.</param>
        /// <returns>This sortable.</returns>
        public Sortable Revert(bool revert)
        {
            m_HtmlAttributes[SortableAttributeName.Revert] = revert;
            return this;
        }

        /// <summary>
        /// Enables animation with the specified duration.
        /// The item will be moved to its new position with an animation.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This sortable.</returns>
        public Sortable Revert(uint duration)
        {
            m_HtmlAttributes[SortableAttributeName.Revert] = duration;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the page is scrolled when an item is moved to the edge.
        /// </summary>
        /// <param name="scroll">If true, the page is scrolled when an item is moved to the edge.</param>
        /// <returns>This sortable.</returns>
        public Sortable Scroll(bool scroll)
        {
            m_HtmlAttributes[SortableAttributeName.Scroll] = scroll;
            return this;
        }

        /// <summary>
        /// Defines how near the mouse must be to an edge to start scrolling.
        /// </summary>
        /// <param name="sensitivity">The sensitivity in pixels.</param>
        /// <returns>This sortable.</returns>
        public Sortable ScrollSensitivity(uint sensitivity)
        {
            m_HtmlAttributes[SortableAttributeName.ScrollSensitivity] = sensitivity;
            return this;
        }

        /// <summary>
        /// Sets the scroll speed.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <returns>This sortable.</returns>
        public Sortable ScrollSpeed(uint speed)
        {
            m_HtmlAttributes[SortableAttributeName.ScrollSpeed] = speed;
            return this;
        }

        /// <summary>
        /// Sets the way the reordering behaves during drag.
        /// </summary>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>This sortable.</returns>
        public Sortable Tolerance(SortableTolerance tolerance)
        {
            m_HtmlAttributes[SortableAttributeName.Tolerance] = tolerance.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the z-index of the helper element.
        /// </summary>
        /// <param name="zIndex">The z-index.</param>
        /// <returns>This sortable.</returns>
        public Sortable ZIndex(int zIndex)
        {
            m_HtmlAttributes[SortableAttributeName.ZIndex] = zIndex;
            return this;
        }

        /// <summary>
        /// Sets the form field name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>This sortable.</returns>
        public Sortable Name(string name)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => name);
            InternalName = name;
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered on drag start when using connected lists.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnActivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnActivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the sorting stops.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnBeforeStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnBeforeStop] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered during sorting when the DOM position has changed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnChange(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnChange] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the sortable is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when sorting was stopped.
        /// The event is propagated to all possible connected lists.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnDeactivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnDeactivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when a sortable item is moved away from 
        /// a connected list.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnOut(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnOut] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when a sortable item is moved into a connected list.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnOver(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnOver] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when a connected sortable list has received an 
        /// item from another list.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnReceive(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnReceive] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when a sortable item has been 
        /// dragged out from the list and into another.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnRemove(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnRemove] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered during sorting.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnSort(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnSort] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when sorting starts.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when sorting has stopped.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnStop] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the user stopped sorting and the 
        /// DOM position has changed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This sortable.</returns>
        public Sortable OnUpdate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SortableAttributeName.OnUpdate] = functionName;
            return this;
        }
        #endregion       
    }
}
