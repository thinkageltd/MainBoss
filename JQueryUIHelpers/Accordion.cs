using System;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents an accordion widget.
    /// </summary>
    public class Accordion : HtmlElement
    {
        internal string HeaderTag { get; private set; }

        internal string HeaderCssClass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Accordion"/> class.
        /// </summary>
        public Accordion()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Accordion"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Accordion(object htmlAttributes)
            : base("div", htmlAttributes)
        {
            HeaderTag = "h3";
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Accordion);
        }

        /// <summary>
        /// Sets the index of the panel which is active when the accordion is created.
        /// </summary>
        /// <param name="panelIndex">The panel index.</param>
        /// <returns>This accordion.</returns>
        public Accordion Active(int panelIndex)
        {
            m_HtmlAttributes[AccordionAttributeName.Active] = panelIndex;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the accordion should animate panel changes.
        /// </summary>
        /// <param name="animate">If false, the animation is disabled, otherwise the animation is enabled.</param>
        /// <returns>This accordion.</returns>
        public Accordion Animate(bool animate)
        {
            m_HtmlAttributes[AccordionAttributeName.Animate] = animate;
            return this;
        }

        /// <summary>
        /// Sets the duration of the animation with default easing.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This accordion.</returns>
        public Accordion Animate(uint duration)
        {
            m_HtmlAttributes[AccordionAttributeName.Animate] = duration;
            return this;
        }

        /// <summary>
        /// Sets the easing of the animation with default duration.
        /// </summary>
        /// <param name="easing">The easing.</param>
        /// <returns>This accordion.</returns>
        public Accordion Animate(Easing easing)
        {
            m_HtmlAttributes[AccordionAttributeName.Animate] = easing.ToString().StartLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the easing and duration of the animation.
        /// </summary>
        /// <param name="easing">The easing.</param>
        /// <param name="duration">The duration.</param>
        /// <returns>This accordion.</returns>
        public Accordion Animate(Easing easing, uint duration)
        {
            m_HtmlAttributes[AccordionAttributeName.Animate] = String.Format("{{\"easing\":\"{0}\", \"duration\":{1}}}", easing.ToString().StartLowerInvariant(), duration);
            return this;
        }

        /// <summary>
        /// Sets the easing and duration of the animation.
        /// Down easing and duration are used when the panel being activated has a lower index than the currently active panel.
        /// </summary>
        /// <param name="easing">The easing.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="downEasing">The down easing.</param>
        /// <param name="downDuration">The down duration.</param>
        /// <returns>This accordion.</returns>
        public Accordion Animate(Easing easing, uint duration, Easing downEasing, uint downDuration)
        {
            m_HtmlAttributes[AccordionAttributeName.Animate] = String.Format("{{\"easing\":\"{0}\", \"duration\":{1}, \"down\":{{\"easing\":\"{2}\", \"duration\":{3}}}}}",
                easing.ToString().StartLowerInvariant(), duration, downEasing.ToString().StartLowerInvariant(), downDuration);
            return this;
        } 

        /// <summary>
        /// Sets a value indicating whether all the sections can be closed at once.
        /// </summary>
        /// <param name="collapsible">If true, all the sections can be closed at once.</param>
        /// <returns>This accordion.</returns>
        public Accordion Collapsible(bool collapsible)
        {
            m_HtmlAttributes[AccordionAttributeName.Collapsible] = collapsible;
            return this;
        }

        /// <summary>
        /// Sets whether all the sections can be closed at once and whether they start closed.
        /// </summary>
        /// <param name="collapsible">If true, all the sections can be closed at once.</param>
        /// <param name="startCollapsed">If true, all sections start closed.</param>
        /// <returns>This accordion.</returns>
        public Accordion Collapsible(bool collapsible, bool startCollapsed)
        {
            m_HtmlAttributes[AccordionAttributeName.Collapsible] = collapsible;
            if (collapsible && startCollapsed)
            {
                m_HtmlAttributes[AccordionAttributeName.Active] = false;
            }

            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the accordion is disabled.
        /// </summary>
        /// <param name="disabled">If true, the accordion is disabled.</param>
        /// <returns>This accordion.</returns>
        public Accordion Disabled(bool disabled)
        {
            m_HtmlAttributes[AccordionAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the name of the event to trigger the accordion.
        /// </summary>
        /// <param name="eventType">The event.</param>
        /// <returns>This accordion.</returns>
        public Accordion Event(Event eventType)
        {
            m_HtmlAttributes[AccordionAttributeName.Event] = eventType.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector for the header element and defines the header tag.
        /// Supports: tag or tag.class
        /// </summary>
        /// <param name="selector">The header selector.</param>
        /// <returns>This accordion.</returns>
        public Accordion Header(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            string[] parts = selector.Split('.');
            HeaderTag = parts[0];
            if (parts.Length > 1)
            {
                HeaderCssClass = parts[1];
            }

            m_HtmlAttributes[AccordionAttributeName.Header] = selector;
            return this;
        }

        /// <summary>
        /// Sets the height style of the accordion and its panels.
        /// </summary>
        /// <param name="heightStyle">The height style.</param>
        /// <returns>This accordion.</returns>
        public Accordion HeightStyle(HeightStyle heightStyle)
        {
            m_HtmlAttributes[AccordionAttributeName.HeightStyle] = heightStyle.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the icons used for headers.
        /// </summary>
        /// <param name="headerIconCssClassName">The CSS class name of the header icon.</param>
        /// <param name="activeHeaderIconCssClassName">The CSS class name of the active header icon.</param>
        /// <returns>This accordion.</returns>
        public Accordion Icons(string headerIconCssClassName, string activeHeaderIconCssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => headerIconCssClassName);
            Guard.ArgumentNotNullOrWhiteSpace(() => activeHeaderIconCssClassName);
            m_HtmlAttributes[AccordionAttributeName.Icons] =
                String.Format("{{\"header\":\"{0}\",\"activeHeader\":\"{1}\"}}", headerIconCssClassName, activeHeaderIconCssClassName);
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the accordion is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This accordion.</returns>
        public Accordion OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AccordionAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when a panel is activated.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This accordion.</returns>
        public Accordion OnActivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AccordionAttributeName.OnActivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered before a panel is activated.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This accordion.</returns>
        public Accordion OnBeforeActivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[AccordionAttributeName.OnBeforeActivate] = functionName;
            return this;
        }
        #endregion
    }
}
