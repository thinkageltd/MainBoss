using System;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a tabs widget.
    /// </summary>
    public class Tabs : HtmlElement
    {
        internal string InternalTabTemplate { get; private set; }

        internal string InternalPanelTag { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tabs"/> class
        /// </summary>
        public Tabs()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tabs"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Tabs(object htmlAttributes)
            : base("div", htmlAttributes)
        {
            InternalTabTemplate = "<li><a href=\"#{href}\"><span>#{label}</span></a></li>";
            InternalPanelTag = "div";

            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Tabs);
        }

        /// <summary>
        /// Sets the index of the panel which is active when the tabs is created.
        /// </summary>
        /// <param name="panelIndex">The panel index.</param>
        /// <returns>This tabs.</returns>
        public Tabs Active(int panelIndex)
        {
            m_HtmlAttributes[TabsAttributeName.Active] = panelIndex;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether all the tabs can be closed at once.
        /// </summary>
        /// <param name="collapsible">If true, all the tabs can be closed at once.</param>
        /// <returns>This tabs.</returns>
        public Tabs Collapsible(bool collapsible)
        {
            m_HtmlAttributes[TabsAttributeName.Collapsible] = collapsible;
            return this;
        }

        /// <summary>
        /// Sets whether all the panels can be closed at once and whether they start closed.
        /// </summary>
        /// <param name="collapsible">If true, all the tabs can be closed at once.</param>
        /// <param name="startCollapsed">If true, all tabs start closed.</param>
        /// <returns>This tabs.</returns>
        public Tabs Collapsible(bool collapsible, bool startCollapsed)
        {
            m_HtmlAttributes[TabsAttributeName.Collapsible] = collapsible;
            if (collapsible && startCollapsed)
            {
                m_HtmlAttributes[TabsAttributeName.Active] = false;
            }

            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the tabs is disabled.
        /// </summary>
        /// <param name="disabled">If true, the tabs is disabled.</param>
        /// <returns>This tabs.</returns>
        public Tabs Disabled(bool disabled)
        {
            m_HtmlAttributes[TabsAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Disables the specified tabs.
        /// </summary>
        /// <param name="disabledIndexes">An array containing the position of the tabs (zero-based index) that should be disabled on initialization.</param>
        /// <returns>This tabs.</returns>
        public Tabs Disabled(int[] disabledIndexes)
        {
            Guard.ArgumentNotNull(() => disabledIndexes);
            m_HtmlAttributes[TabsAttributeName.Disabled] = String.Join(",", disabledIndexes);
            return this;
        }

        /// <summary>
        /// Sets the name of the event to select a tab.
        /// </summary>
        /// <param name="eventType">The event.</param>
        /// <returns>This tabs.</returns>
        public Tabs Event(Event eventType)
        {
            m_HtmlAttributes[TabsAttributeName.Event] = eventType.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the height style of the tabs and its panels.
        /// </summary>
        /// <param name="heightStyle">The height style.</param>
        /// <returns>This tabs.</returns>
        public Tabs HeightStyle(HeightStyle heightStyle)
        {
            m_HtmlAttributes[TabsAttributeName.HeightStyle] = heightStyle.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the duration of the hide animation using the default effect and easing.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This tabs.</returns>
        public Tabs Hide(uint duration)
        {
            m_HtmlAttributes[TabsAttributeName.Hide] = duration;
            return this;
        }

        /// <summary>
        /// Sets the effect of the hide animation using the default duration and easing.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <returns>This tabs.</returns>
        public Tabs Hide(Effect effect)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TabsAttributeName.Hide] = false;
            }
            else
            {
                m_HtmlAttributes[TabsAttributeName.Hide] = effect.ToString().StartLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the effect, duration and easing of the hide animation.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This tabs.</returns>
        public Tabs Hide(Effect effect, uint duration, Easing easing)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TabsAttributeName.Hide] = false;
            }
            else
            {
                m_HtmlAttributes[TabsAttributeName.Hide] = String.Format("{{\"effect\":\"{0}\",\"duration\":{1},\"easing\":\"{2}\"}}",
                    effect.ToString().StartLowerInvariant(), duration, easing.ToString().StartLowerInvariant());
            }

            return this;
        }

        /// <summary>
        /// Sets the duration of the show animation using the default effect and easing.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This tabs.</returns>
        public Tabs Show(uint duration)
        {
            m_HtmlAttributes[TabsAttributeName.Show] = duration;
            return this;
        }

        /// <summary>
        /// Sets the effect of the show animation using the default duration and easing.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <returns>This tabs.</returns>
        public Tabs Show(Effect effect)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TabsAttributeName.Show] = false;
            }
            else
            {
                m_HtmlAttributes[TabsAttributeName.Show] = effect.ToString().StartLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the effect, duration and easing of the show animation.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This tabs.</returns>
        public Tabs Show(Effect effect, uint duration, Easing easing)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TabsAttributeName.Show] = false;
            }
            else
            {
                m_HtmlAttributes[TabsAttributeName.Show] = String.Format("{{\"effect\":\"{0}\",\"duration\":{1},\"easing\":\"{2}\"}}",
                    effect.ToString().StartLowerInvariant(), duration, easing.ToString().StartLowerInvariant());
            }

            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the tabs is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tabs.</returns>
        public Tabs OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TabsAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered after the content of a remote tab has been loaded.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tabs.</returns>
        public Tabs OnLoad(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TabsAttributeName.OnLoad] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered after a tab has been activated.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tabs.</returns>
        public Tabs OnActivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TabsAttributeName.OnActivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered directly after a tab is activated.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tabs.</returns>
        public Tabs OnBeforeActivate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TabsAttributeName.OnBeforeActivate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered before a remote tab is loaded.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tabs.</returns>
        public Tabs OnBeforeLoad(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TabsAttributeName.OnBeforeLoad] = functionName;
            return this;
        }
        #endregion
    }
}
