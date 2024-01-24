using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a slider widget.
    /// </summary>
    public class Slider : Widget
    {
        private readonly Handle[] m_Handles;
        private readonly bool m_HiddenValues;

        private string m_Label;
        private string m_ContainerCssClass;
        private SliderLabelStyle m_SliderLabelStyle;

        // jQuery UI defaults
        private int m_Min = 0;
        private int m_Max = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="Slider"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="handles">The slider handles.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        /// <param name="hiddenValues">If true, the handler values are stored in hidden HTML elements.</param>
        /// <param name="label">The label of the slider.</param>
        public Slider(HtmlHelper htmlHelper, Handle[] handles, object htmlAttributes, bool hiddenValues, string label)
            : base(htmlHelper, htmlAttributes)
        {
            Guard.ArgumentNotNull(() => handles);
            m_Handles = handles;
            m_HiddenValues = hiddenValues;
            m_Label = label;
            m_SliderLabelStyle = SliderLabelStyle.LabelAndValue;

            if (m_Handles.Length == 0)
            {
                throw new ArgumentException(StringResource.SliderWithoutHandle, "handles");
            }

            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Slider);
        }

        /// <summary>
        /// Enables the animation of the handle and sets the duration.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        /// <returns>This slider.</returns>
        public Slider Animate(Duration duration)
        {
            m_HtmlAttributes[SliderAttributeName.Animate] = duration.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Enables the animation of the handle and sets the duration.
        /// </summary>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <returns>This slider.</returns>
        public Slider Animate(int duration)
        {
            Guard.ArgumentInRange(() => duration, 1, Int32.MaxValue);
            m_HtmlAttributes[SliderAttributeName.Animate] = duration;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the slider is disabled.
        /// </summary>
        /// <param name="disabled">If true, the slider is disabled.</param>
        /// <returns>This slider.</returns>
        public Slider Disabled(bool disabled)
        {
            m_HtmlAttributes[SliderAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets the maximum value of the slider.
        /// </summary>
        /// <param name="max">The maximum value of the slider.</param>
        /// <returns>This slider.</returns>
        public Slider Max(int max)
        {
            m_Max = max;
            m_HtmlAttributes[SliderAttributeName.Max] = max;
            return this;
        }

        /// <summary>
        /// Sets the minimum value of the slider.
        /// </summary>
        /// <param name="min">The minimum value of the slider.</param>
        /// <returns>This slider.</returns>
        public Slider Min(int min)
        {
            m_Min = min;
            m_HtmlAttributes[SliderAttributeName.Min] = min;
            return this;
        }

        /// <summary>
        /// Sets the orientation of the slider.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        /// <returns>This slider.</returns>
        public Slider Orientation(Orientation orientation)
        {
            m_HtmlAttributes[SliderAttributeName.Orientation] = orientation.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets how the slider displays and treats ranges.
        /// User Min or Max with a single handle and Between with two handles.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <returns>This slider.</returns>
        public Slider Range(SliderRange range)
        {
            if (range == SliderRange.Between)
            {
                m_HtmlAttributes[SliderAttributeName.Range] = true;
            }
            else
            {
                m_HtmlAttributes[SliderAttributeName.Range] = range.ToString().ToLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the amount of each step the slider takes between min and max.
        /// </summary>
        /// <param name="step">The value of a single step.</param>
        /// <returns>This slider.</returns>
        public Slider Step(int step)
        {
            Guard.ArgumentInRange(() => step, 1, Int32.MaxValue);
            m_HtmlAttributes[SliderAttributeName.Step] = step;
            return this;
        }

        /// <summary>
        /// Sets the text that is displayed before the slider.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <returns>This slider.</returns>
        public Slider Label(string label)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => label);
            m_Label = label;
            return this;
        }

        /// <summary>
        /// Sets the CSS class of the div element that contains the slider and its label.
        /// </summary>
        /// <param name="containerCssClass">The name of the CSS class.</param>
        /// <returns>This slider.</returns>
        public Slider ContainerCssClass(string containerCssClass)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => containerCssClass);
            m_ContainerCssClass = containerCssClass;
            return this;
        }

        /// <summary>
        /// Sets the label style of the slider.
        /// </summary>
        /// <param name="labelStyle">The style of the label.</param>
        /// <returns>This slider.</returns>
        public Slider LabelStyle(SliderLabelStyle labelStyle)
        {
            m_SliderLabelStyle = labelStyle;
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered on slide stop or if the value is changed programmatically.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This slider.</returns>
        public Slider OnChange(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SliderAttributeName.OnChange] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the slider is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This slider.</returns>
        public Slider OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SliderAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered on every mouse move during slide.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This slider.</returns>
        public Slider OnSlide(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SliderAttributeName.OnSlide] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the user starts sliding.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This slider.</returns>
        public Slider OnStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SliderAttributeName.OnStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the user stops sliding.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This slider.</returns>
        public Slider OnStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SliderAttributeName.OnStop] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Returns the HTML-encoded representation of the slider.
        /// </summary>
        /// <returns>the HTML-encoded representation of the slider.</returns>
        public override string ToHtmlString()
        {
            NormalizeValues();
            if (m_HiddenValues)
            {
                string names = String.Join(",",
                    m_Handles
                    .Select(h => m_HtmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(h.Name)));
                m_HtmlAttributes[SliderAttributeName.Names] = names;
            }

            if (m_Handles.Length == 1)
            {
                m_HtmlAttributes[SliderAttributeName.Value] = m_Handles[0].Value;
            }
            else
            {
                string values = String.Join(",", m_Handles.Select(h => h.Value));
                m_HtmlAttributes[SliderAttributeName.Values] = values;
            }

            TagBuilder sliderBuilder = new TagBuilder("div");
            sliderBuilder.MergeAttributes(m_HtmlAttributes);
            string slider = sliderBuilder.ToString();

            if (!m_HiddenValues)
            {
                return slider;
            }

            if (m_Handles.Select(h => m_HtmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(h.Name)).Any(String.IsNullOrWhiteSpace))
            {
                throw new ArgumentNullException("Handle.Name");
            }

            TagBuilder containerBuilder = new TagBuilder("div");
            if (!String.IsNullOrWhiteSpace(m_ContainerCssClass))
            {
                containerBuilder.AddCssClass(m_ContainerCssClass);
            }

            if (m_SliderLabelStyle == SliderLabelStyle.LabelAndValue)
            {
                TagBuilder labelBuilder = new TagBuilder("span");
                if (String.IsNullOrWhiteSpace(m_Label))
                {
                    string fullName = m_HtmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(m_Handles[0].Name);
                    labelBuilder.InnerHtml = HttpUtility.HtmlEncode(fullName);
                }
                else
                {
                    labelBuilder.InnerHtml = HttpUtility.HtmlEncode(m_Label);
                }

                labelBuilder.InnerHtml += "&nbsp;";

                containerBuilder.InnerHtml = labelBuilder.ToString();
            }
            if (m_SliderLabelStyle != SliderLabelStyle.None)
            {
                TagBuilder valueBuilder = new TagBuilder("span");
                valueBuilder.InnerHtml = String.Join(m_Handles.Length > 2 ? ", " : "-", m_Handles.Select(h => h.Value));
                containerBuilder.InnerHtml += valueBuilder.ToString();
            }

            StringBuilder stringBuilder = new StringBuilder(slider);
            foreach (Handle handle in m_Handles)
            {
                stringBuilder.Append(m_HtmlHelper.Hidden(handle.Name, handle.Value).ToHtmlString());
            }

            containerBuilder.InnerHtml += stringBuilder.ToString();
            return containerBuilder.ToString();
        }

        /// <summary>
        /// Ensures that the handle values are between the min and max values.
        /// </summary>
        private void NormalizeValues()
        {
            foreach (Handle handle in m_Handles)
            {
                if (handle.Value < m_Min)
                {
                    handle.Value = m_Min;
                }
                else if (handle.Value > m_Max)
                {
                    handle.Value = m_Max;
                }
            }
        }
    }
}
