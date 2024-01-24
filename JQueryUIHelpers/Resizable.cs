using System;
using System.Globalization;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a resizable widget.
    /// </summary>
    public class Resizable : HtmlElement
    {        
        /// <summary>
        /// Initializes a new instance of the <see cref="Resizable"/> class.
        /// </summary>
        public Resizable() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resizable"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Resizable(object htmlAttributes)
            : base("div", htmlAttributes)
        {            
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Resizable);
        }

        /// <summary>
        /// Sets the jQuery selector that is used to select the element(s) to resize when this resizable is resized.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This resizable.</returns>
        public Resizable AlsoResize(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[ResizableAttributeName.AlsoResize] = selector;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to animate to the final size after resizing.
        /// </summary>
        /// <param name="animate">If true, the resizable is animated to the final size.</param>
        /// <returns>This resizable.</returns>
        public Resizable Animate(bool animate)
        {
            m_HtmlAttributes[ResizableAttributeName.Animate] = animate;
            return this;
        }

        /// <summary>
        /// Enables animation with the specified duration and easing.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This resizable.</returns>
        public Resizable Animate(uint duration, Easing easing)
        {
            m_HtmlAttributes[ResizableAttributeName.Animate] = true;
            m_HtmlAttributes[ResizableAttributeName.AnimateDuration] = duration;
            m_HtmlAttributes[ResizableAttributeName.AnimateEasing] = easing.ToString().StartLowerInvariant();
            return this;
        }

        /// <summary>
        /// Enables animation with the specified duration and easing.
        /// </summary>
        /// <param name="duration">The duration.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This resizable.</returns>
        public Resizable Animate(Duration duration, Easing easing)
        {
            m_HtmlAttributes[ResizableAttributeName.Animate] = true;
            m_HtmlAttributes[ResizableAttributeName.AnimateDuration] = duration.ToString().ToLowerInvariant();
            m_HtmlAttributes[ResizableAttributeName.AnimateEasing] = easing.ToString().StartLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to keep the original aspect ratio when resizing.
        /// </summary>
        /// <param name="keepOriginal">If true, the original aspect ratio is preserved.</param>
        /// <returns>This resizable.</returns>
        public Resizable AspectRatio(bool keepOriginal)
        {
            m_HtmlAttributes[ResizableAttributeName.AspectRatio] = keepOriginal;
            return this;
        }

        /// <summary>
        /// Sets the aspect ratio to keep when resizing.
        /// </summary>
        /// <param name="ratio">The aspect ratio.</param>
        /// <returns>This resizable.</returns>
        public Resizable AspectRatio(double ratio)
        {
            Guard.ArgumentInRange(() => ratio, 0, Double.MaxValue);
            m_HtmlAttributes[ResizableAttributeName.AspectRatio] = ratio.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to hide the resize handles.
        /// </summary>
        /// <param name="autoHide">If true, the handles are hidden except when the mouse hovers over the element.</param>
        /// <returns>This resizable.</returns>
        public Resizable AutoHide(bool autoHide)
        {
            m_HtmlAttributes[ResizableAttributeName.AutoHide] = autoHide;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the elements which prevent resizing if the user starts on them.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This resizable.</returns>
        public Resizable Cancel(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[ResizableAttributeName.Cancel] = selector;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector that selects the element that constrains the size of the resizable.
        /// </summary>
        /// <param name="selector">The jQuery selector.</param>
        /// <returns>This resizable.</returns>
        public Resizable Containment(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[ResizableAttributeName.Containment] = selector;
            return this;
        }

        /// <summary>
        /// Sets the element that constrains the size of the resizable.
        /// </summary>
        /// <param name="containment">The element.</param>
        /// <returns>This resizable.</returns>
        public Resizable Containment(Containment containment)
        {
            m_HtmlAttributes[ResizableAttributeName.Containment] = containment.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when resizing should start.
        /// Resizing will not start until after the mouse is moved beyond the specified duration.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This resizable.</returns>
        public Resizable Delay(uint duration)
        {
            m_HtmlAttributes[ResizableAttributeName.Delay] = duration;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the resizable is disabled.
        /// </summary>
        /// <param name="disabled">If true, the resizable is disabled.</param>
        /// <returns>This resizable.</returns>
        public Resizable Disabled(bool disabled)
        {
            m_HtmlAttributes[ResizableAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the tolerance for when resizing should start.
        /// Resizing will not start until after the mouse is moved beyond the specified distance.
        /// </summary>
        /// <param name="distance">The distance in pixels.</param>
        /// <returns>This resizable.</returns>
        public Resizable Distance(uint distance)
        {
            m_HtmlAttributes[ResizableAttributeName.Distance] = distance;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether to show a helper element for resizing.
        /// </summary>
        /// <param name="ghost">If true, a semi-transparent helper element is shown for resizing.</param>
        /// <returns>This resizable.</returns>
        public Resizable Ghost(bool ghost)
        {
            m_HtmlAttributes[ResizableAttributeName.Ghost] = ghost;
            return this;
        }

        /// <summary>
        /// Snaps the resizing element to a grid specified by the x and y values.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>This resizable.</returns>
        public Resizable Grid(uint x, uint y)
        {
            m_HtmlAttributes[ResizableAttributeName.Grid] = String.Format("{0},{1}", x, y);
            return this;
        }

        /// <summary>
        /// Sets the resize handles.
        /// To define multiple handles use the | operator, e.g.: ResizableHandles.E | ResizableHandles.S
        /// </summary>
        /// <param name="handles">The handles.</param>
        /// <returns>This resizable.</returns>
        public Resizable Handles(ResizableHandles handles)
        {
            m_HtmlAttributes[ResizableAttributeName.Handles] = handles.ToString().ToLowerInvariant().Replace(" ", String.Empty);
            return this;
        }

        /// <summary>
        /// Sets the CSS class that will be added to a proxy element to outline the resize during the drag of the resize handle.
        /// </summary>
        /// <param name="cssClassName">The CSS class.</param>
        /// <returns>This resizable.</returns>
        public Resizable Helper(string cssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => cssClassName);
            m_HtmlAttributes[ResizableAttributeName.Helper] = cssClassName;
            return this;
        }

        /// <summary>
        /// Sets the maximum height of the resizable.
        /// </summary>
        /// <param name="maxHeight">The maximum height.</param>
        /// <returns>This resizable.</returns>
        public Resizable MaxHeight(uint maxHeight)
        {
            m_HtmlAttributes[ResizableAttributeName.MaxHeight] = maxHeight;
            return this;
        }

        /// <summary>
        /// Sets the maximum width of the resizable.
        /// </summary>
        /// <param name="maxWidth">The maximum width.</param>
        /// <returns>This resizable.</returns>
        public Resizable MaxWidth(uint maxWidth)
        {
            m_HtmlAttributes[ResizableAttributeName.MaxWidth] = maxWidth;
            return this;
        }

        /// <summary>
        /// Sets the minimum height of the resizable.
        /// </summary>
        /// <param name="minHeight">The minimum height.</param>
        /// <returns>This resizable.</returns>
        public Resizable MinHeight(uint minHeight)
        {
            m_HtmlAttributes[ResizableAttributeName.MinHeight] = minHeight;
            return this;
        }

        /// <summary>
        /// Sets the minimum width of the resizable.
        /// </summary>
        /// <param name="minWidth">The minimum width.</param>
        /// <returns>This resizable.</returns>
        public Resizable MinWidth(uint minWidth)
        {
            m_HtmlAttributes[ResizableAttributeName.MinWidth] = minWidth;
            return this;
        }

        /// <summary>
        /// Sets the HTML tag that is used as the resizable.
        /// </summary>
        /// <param name="tagName">The HTML tag.</param>
        /// <returns>This resizable.</returns>
        public Resizable HtmlTag(string tagName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => tagName);
            m_Tag = tagName;
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the resizable is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This resizable.</returns>
        public Resizable OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ResizableAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered during the resize.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This resizable.</returns>
        public Resizable OnResize(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ResizableAttributeName.OnResize] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the start of a resize operation.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This resizable.</returns>
        public Resizable OnStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ResizableAttributeName.OnStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the end of a resize operation.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This resizable.</returns>
        public Resizable OnStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[ResizableAttributeName.OnStop] = functionName;
            return this;
        }

        #endregion        
    }
}
