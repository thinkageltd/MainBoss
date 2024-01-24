using System;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a tooltip widget.
    /// </summary>
    public class Tooltip : Widget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tooltip"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public Tooltip(HtmlHelper htmlHelper, object htmlAttributes)
            : base(htmlHelper, htmlAttributes)
        {
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Tooltip);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tooltip"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        /// <param name="selector">The target element selector.</param>
        public Tooltip(HtmlHelper htmlHelper, object htmlAttributes, string selector)
            : base(htmlHelper, htmlAttributes)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);

            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Tooltip);
            m_HtmlAttributes[TooltipAttributeName.Selector] = selector;
        }

        /// <summary>
        /// Sets the content of the tooltip. This can be a JavaScript function name or the actual content of the tooltip.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Content(string content)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => content);
            m_HtmlAttributes[TooltipAttributeName.Content] = content;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the tooltip is disabled.
        /// </summary>
        /// <param name="disabled">If true, the tooltip is disabled.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Disabled(bool disabled)
        {
            m_HtmlAttributes[TooltipAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the duration of the hide animation using the default effect and easing.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Hide(uint duration)
        {
            m_HtmlAttributes[TooltipAttributeName.Hide] = duration;
            return this;
        }

        /// <summary>
        /// Sets the effect of the hide animation using the default duration and easing.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Hide(Effect effect)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TooltipAttributeName.Hide] = false;
            }
            else
            {
                m_HtmlAttributes[TooltipAttributeName.Hide] = effect.ToString().StartLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the effect, duration and easing of the hide animation.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Hide(Effect effect, uint duration, Easing easing)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TooltipAttributeName.Hide] = false;
            }
            else
            {
                m_HtmlAttributes[TooltipAttributeName.Hide] = String.Format("{{\"effect\":\"{0}\",\"duration\":{1},\"easing\":\"{2}\"}}",
                    effect.ToString().StartLowerInvariant(), duration, easing.ToString().StartLowerInvariant());
            }

            return this;
        }

        /// <summary>
        /// Sets the jQuery selector which selects the items that should show the tooltip.
        /// </summary>
        /// <param name="itemsSelector">The jQuery selector.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Items(string itemsSelector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => itemsSelector);
            m_HtmlAttributes[TooltipAttributeName.Items] = itemsSelector;
            return this;
        }

        /// <summary>
        /// Sets the position of the tooltip relative to the target element.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Position(Position position)
        {
            Guard.ArgumentNotNull(() => position);
            m_HtmlAttributes[TooltipAttributeName.Position] = position.ToString();
            return this;
        }

        /// <summary>
        /// Sets the duration of the show animation using the default effect and easing.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Show(uint duration)
        {
            m_HtmlAttributes[TooltipAttributeName.Show] = duration;
            return this;
        }

        /// <summary>
        /// Sets the effect of the show animation using the default duration and easing.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Show(Effect effect)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TooltipAttributeName.Show] = false;
            }
            else
            {
                m_HtmlAttributes[TooltipAttributeName.Show] = effect.ToString().StartLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the effect, duration and easing of the show animation.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Show(Effect effect, uint duration, Easing easing)
        {
            if (effect == Effect.None)
            {
                m_HtmlAttributes[TooltipAttributeName.Show] = false;
            }
            else
            {
                m_HtmlAttributes[TooltipAttributeName.Show] = String.Format("{{\"effect\":\"{0}\",\"duration\":{1},\"easing\":\"{2}\"}}",
                    effect.ToString().StartLowerInvariant(), duration, easing.ToString().StartLowerInvariant());
            }

            return this;
        }

        /// <summary>
        /// Specifies a CSS class name which will be added to the tooltip element.
        /// </summary>
        /// <param name="cssClassName">The class name.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip TooltipClass(string cssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => cssClassName);
            m_HtmlAttributes[TooltipAttributeName.TooltipClass] = cssClassName;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the tooltip should track (follow) the mouse.
        /// </summary>
        /// <param name="track">If true, the tooltip follows the mouse.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip Track(bool track)
        {
            m_HtmlAttributes[TooltipAttributeName.Track] = track;
            return this;
        }
        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the tooltip is closed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip OnClose(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TooltipAttributeName.OnClose] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the tooltip is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TooltipAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the tooltip is opened.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This tooltip.</returns>
        public Tooltip OnOpen(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[TooltipAttributeName.OnOpen] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Returns the HTML-encoded representation of the tooltip.
        /// </summary>
        /// <returns>The HTML-encoded representation of the tooltip.</returns>
        public override string ToHtmlString()
        {
            TagBuilder tagBuilder = new TagBuilder("input");
            tagBuilder.Attributes.Add("type", "hidden");
            tagBuilder.MergeAttributes(m_HtmlAttributes);
            return tagBuilder.ToString(TagRenderMode.SelfClosing);
        }
    }
}
