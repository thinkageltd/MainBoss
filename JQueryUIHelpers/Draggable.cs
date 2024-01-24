using System;
using System.Globalization;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a draggable widget.
    /// </summary>
    public class Draggable : HtmlElement
    {       
        /// <summary>
        /// Initializes a new instance of the <see cref="Draggable"/> class.
        /// </summary>
        public Draggable()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Draggable"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Draggable(object htmlAttributes)
            : base("div", htmlAttributes)
        {
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Draggable);
        }

        /// <summary>
        /// Sets a value indicating whether to add the ui-draggable class to the draggable element.
        /// </summary>
        /// <param name="addClasses">If true, the ui-draggable class is added to the element.</param>
        /// <returns>This draggable.</returns>
        public Draggable AddClasses(bool addClasses)
        {
            m_HtmlAttributes[DraggableAttributeName.AddClasses] = addClasses;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the element where the helper 
        /// container will be appended to during the drag.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable AppendTo(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.AppendTo] = selector;
            return this;
        }

        /// <summary>
        /// Sets the axis where the element can be dragged.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <returns>This draggable.</returns>
        public Draggable Axis(Axis axis)
        {
            m_HtmlAttributes[DraggableAttributeName.Axis] = axis.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the elements which prevent dragging if the user starts on them.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable Cancel(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.Cancel] = selector;
            return this;
        }

        /// <summary>
        /// Allows the draggable to be dropped onto the sortable specified by the jQuery selector, and sets the helper 
        /// element to 'clone'.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable ConnectToSortable(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.ConnectToSortable] = selector;
            m_HtmlAttributes[DraggableAttributeName.Helper] = DragHelper.Clone.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the element that constrains dragging.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable Containment(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.Containment] = selector;
            return this;
        }

        /// <summary>
        /// Sets the element that constrains dragging.
        /// </summary>
        /// <param name="containment">The element.</param>
        /// <returns>This draggable.</returns>
        public Draggable Containment(Containment containment)
        {
            m_HtmlAttributes[DraggableAttributeName.Containment] = containment.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the element that constrains dragging.
        /// </summary>
        /// <param name="x1">The x-coordinate of the top left corner.</param>
        /// <param name="y1">The y-coordinate of the top left corner.</param>
        /// <param name="x2">The x-coordinate of the bottom right corner.</param>
        /// <param name="y2">The y-coordinate of the bottom right corner.</param>
        /// <returns>This draggable.</returns>
        public Draggable Containment(uint x1, uint y1, uint x2, uint y2)
        {
            m_HtmlAttributes[DraggableAttributeName.Containment] = String.Format("{0},{1},{2},{3}", x1, y1, x2, y2);
            return this;
        }

        /// <summary>
        /// Sets the cursor that is being shown while dragging.
        /// </summary>
        /// <param name="cursor">The cursor.</param>
        /// <returns>This draggable.</returns>
        public Draggable Cursor(Cursor cursor)
        {
            m_HtmlAttributes[DraggableAttributeName.Cursor] = CursorUtility.GetCursorText(cursor);
            return this;
        }

        /// <summary>
        /// Sets the cursor position relative to the element being dragged.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>This draggable.</returns>
        public Draggable CursorAt(CursorPosition position)
        {
            Guard.ArgumentNotNull(() => position);
            m_HtmlAttributes[DraggableAttributeName.CursorAt] = position.ToJsonString();
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when dragging should start.
        /// Dragging will not start until after the mouse is moved beyond the specified duration.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This draggable.</returns>
        public Draggable Delay(uint duration)
        {
            m_HtmlAttributes[DraggableAttributeName.Delay] = duration;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the draggable is disabled.
        /// </summary>
        /// <param name="disabled">If true, the draggable is disabled.</param>
        /// <returns>This draggable.</returns>
        public Draggable Disabled(bool disabled)
        {
            m_HtmlAttributes[DraggableAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when dragging should start.
        /// Dragging will not start until after the mouse is moved beyond the specified distance.
        /// </summary>
        /// <param name="distance">The distance in pixels.</param>
        /// <returns>This draggable.</returns>
        public Draggable Distance(uint distance)
        {
            m_HtmlAttributes[DraggableAttributeName.Distance] = distance;
            return this;
        }

        /// <summary>
        /// Snaps the helper to a grid specified by the x and y values.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>This draggable.</returns>
        public Draggable Grid(uint x, uint y)
        {
            m_HtmlAttributes[DraggableAttributeName.Grid] = String.Format("{0},{1}", x, y);
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the elements which can start dragging.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable Handle(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.Handle] = selector;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that returns the helper element.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This draggable.</returns>
        public Draggable Helper(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DraggableAttributeName.Helper] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the helper element.
        /// </summary>
        /// <param name="helper">The helper element.</param>
        /// <returns>This draggable.</returns>
        public Draggable Helper(DragHelper helper)
        {
            m_HtmlAttributes[DraggableAttributeName.Helper] = helper.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether iframes should ignore the mousemove events during a drag.
        /// </summary>
        /// <param name="iframeFix">If true, prevents iframes from capturing the mousemove events during a drag.</param>
        /// <returns>This draggable.</returns>
        public Draggable IframeFix(bool iframeFix)
        {
            m_HtmlAttributes[DraggableAttributeName.IframeFix] = iframeFix;
            return this;
        }

        /// <summary>
        /// Prevent iframes specified by the jQuery selector from capturing the mousemove events during a drag.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable IframeFix(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.IframeFix] = selector;
            return this;
        }

        /// <summary>
        /// Sets the opacity of the helper element.
        /// </summary>
        /// <param name="opacity">The opacity from 0.01 to 1.</param>
        /// <returns>This draggable.</returns>
        public Draggable Opacity(double opacity)
        {
            Guard.ArgumentInRange(() => opacity, 0.01, 1.0);
            m_HtmlAttributes[DraggableAttributeName.Opacity] = opacity.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether all droppable positions should be calculated on every mousemove.
        /// </summary>
        /// <param name="refreshPositions">If true, all droppable positions are calculated on every mousemove.</param>
        /// <returns>This draggable.</returns>
        public Draggable RefreshPositions(bool refreshPositions)
        {
            m_HtmlAttributes[DraggableAttributeName.RefreshPositions] = refreshPositions;
            return this;
        }

        /// <summary>
        /// Sets when should the element return to its starting position when dragging stops.
        /// </summary>
        /// <param name="revertMode">The revert mode.</param>
        /// <returns>This draggable.</returns>
        public Draggable Revert(DraggableRevert revertMode)
        {
            if (revertMode == DraggableRevert.Always)
            {
                m_HtmlAttributes[DraggableAttributeName.Revert] = true;
            }
            else
            {
                m_HtmlAttributes[DraggableAttributeName.Revert] = revertMode.ToString().ToLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the duration of the revert animation.
        /// </summary>
        /// <param name="duration">The duration is milliseconds.</param>
        /// <returns>This draggable.</returns>
        public Draggable RevertDuration(uint duration)
        {
            m_HtmlAttributes[DraggableAttributeName.RevertDuration] = duration;
            return this;
        }

        /// <summary>
        /// Sets the scope of the draggable.
        /// </summary>
        /// <param name="scope">The scope name.</param>
        /// <returns>This draggable.</returns>
        public Draggable Scope(string scope)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => scope);
            m_HtmlAttributes[DraggableAttributeName.Scope] = scope;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the container auto-scrolls while dragging.
        /// </summary>
        /// <param name="scroll">If true, the container auto-scrolls when an item is moved to the edge.</param>
        /// <returns>This draggable.</returns>
        public Draggable Scroll(bool scroll)
        {
            m_HtmlAttributes[DraggableAttributeName.Scroll] = scroll;
            return this;
        }

        /// <summary>
        /// Defines how near the mouse must be to an edge to start scrolling.
        /// </summary>
        /// <param name="sensitivity">The sensitivity in pixels.</param>
        /// <returns>This draggable.</returns>
        public Draggable ScrollSensitivity(uint sensitivity)
        {
            m_HtmlAttributes[DraggableAttributeName.ScrollSensitivity] = sensitivity;
            return this;
        }

        /// <summary>
        /// Sets the scroll speed.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <returns>This draggable.</returns>
        public Draggable ScrollSpeed(uint speed)
        {
            m_HtmlAttributes[DraggableAttributeName.ScrollSpeed] = speed;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the draggable should snap to the edges of other draggable elements.
        /// </summary>
        /// <param name="snap">If true, the draggable will snap to other draggables.</param>
        /// <returns>This draggable.</returns>
        public Draggable Snap(bool snap)
        {
            m_HtmlAttributes[DraggableAttributeName.Snap] = snap;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector used to select the elements to which this draggable will snap.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable Snap(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.Snap] = selector;
            return this;
        }

        /// <summary>
        /// Sets which edges of snap elements the draggable will snap to
        /// </summary>
        /// <param name="mode">The snap mode.</param>
        /// <returns>This draggable.</returns>
        public Draggable SnapMode(SnapMode mode)
        {
            m_HtmlAttributes[DraggableAttributeName.SnapMode] = mode.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the distance from the snap element edges at which snapping should occur.
        /// </summary>
        /// <param name="distance">The distance is pixels.</param>
        /// <returns>This draggable.</returns>
        public Draggable SnapTolerance(uint distance)
        {
            m_HtmlAttributes[DraggableAttributeName.SnapTolerance] = distance;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector used to select the elements that define a stack. The dragged element
        /// is always at the top of its stack.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This draggable.</returns>
        public Draggable Stack(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DraggableAttributeName.Stack] = selector;
            return this;
        }

        /// <summary>
        /// Sets the z-index of the helper element.
        /// </summary>
        /// <param name="zIndex">The z-index.</param>
        /// <returns>This draggable.</returns>
        public Draggable ZIndex(int zIndex)
        {
            m_HtmlAttributes[DraggableAttributeName.ZIndex] = zIndex;
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the draggable is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This draggable.</returns>
        public Draggable OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DraggableAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the mouse is moved during dragging.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This draggable.</returns>
        public Draggable OnDrag(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DraggableAttributeName.OnDrag] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when dragging starts.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This draggable.</returns>
        public Draggable OnStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DraggableAttributeName.OnStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when dragging stops.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This draggable.</returns>
        public Draggable OnStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DraggableAttributeName.OnStop] = functionName;
            return this;
        }

        #endregion
    }
}
