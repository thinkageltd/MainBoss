using System;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a spinner widget.
    /// </summary>
    public class Spinner : Widget
    {
        private string m_Name;
        private object m_Value;
        private string m_NumberFormat;
        private CultureInfo m_Culture;

        /// <summary>
        /// Initializes a new instance of the <see cref="Spinner"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="name">The name of the spinner.</param>
        /// <param name="value">The value of the spinner.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        public Spinner(HtmlHelper htmlHelper, string name, object value, object htmlAttributes)
            : base(htmlHelper, htmlAttributes)
        {
            m_Name = name;
            m_Value = value;
            
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Spinner);
        }

        /// <summary>
        /// Sets the culture used for number formatting. Requires Globalize.js on the client side.
        /// </summary>
        /// <param name="culture">The culture.</param>
        /// <returns>This spinner.</returns>
        public Spinner Culture(CultureInfo culture)
        {
            m_Culture = culture;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the spinner is disabled.
        /// </summary>
        /// <param name="disabled">If true, the spinner is disabled.</param>
        /// <returns>This spinner.</returns>
        public Spinner Disabled(bool disabled)
        {
            m_HtmlAttributes[SpinnerAttributeName.Disabled] = disabled;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the stepping delta should be increased when spun incessantly.
        /// </summary>
        /// <param name="incremental">If true, the stepping delta is increased when spun incessantly, otherwise it is constant.</param>
        /// <returns>This spinner.</returns>
        public Spinner Incremental(bool incremental)
        {
            m_HtmlAttributes[SpinnerAttributeName.Incremental] = incremental;
            return this;
        }

        /// <summary>
        /// Sets the up and down icons.
        /// </summary>
        /// <param name="upIconCssClassName">The CSS class name of the up icon.</param>
        /// <param name="downIconCssClassName">The CSS class name of the  down icon.</param>
        /// <returns>This spinner.</returns>
        public Spinner Icons(string upIconCssClassName, string downIconCssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => upIconCssClassName);
            Guard.ArgumentNotNullOrWhiteSpace(() => downIconCssClassName);
            m_HtmlAttributes[SpinnerAttributeName.Icons] = String.Format("{{\"up\":\"{0}\",\"down\":\"{1}\"}}", upIconCssClassName, downIconCssClassName);
            return this;
        }

        /// <summary>
        /// Sets the number of decimal digits.
        /// </summary>
        /// <param name="count">The number of decimal digits.</param>
        /// <returns>This spinner.</returns>
        public Spinner DecimalDigits(uint count)
        {
            m_NumberFormat = String.Format("n{0}", count);
            return this;
        }

        /// <summary>
        /// Sets the maximum value.
        /// </summary>
        /// <param name="max">The maximum value.</param>
        /// <returns>This spinner.</returns>
        public Spinner Max(double max)
        {
            m_HtmlAttributes[SpinnerAttributeName.Max] = max.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        /// <summary>
        /// Sets the minimum value.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <returns>This spinner.</returns>
        public Spinner Min(double min)
        {
            m_HtmlAttributes[SpinnerAttributeName.Min] = min.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        /// <summary>
        /// Sets the number of steps to take when paging.
        /// </summary>
        /// <param name="numberOfSteps">The number of steps per page.</param>
        /// <returns>This spinner.</returns>
        public Spinner Page(uint numberOfSteps)
        {
            m_HtmlAttributes[SpinnerAttributeName.Page] = numberOfSteps;
            return this;
        }

        /// <summary>
        /// Sets the value of a single step.
        /// </summary>
        /// <param name="step">The value of a step.</param>
        /// <returns>This spinner.</returns>
        public Spinner Step(double step)
        {
            Guard.ArgumentInRange(() => step, 0, Double.MaxValue);
            m_HtmlAttributes[SpinnerAttributeName.Step] = step.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the value has changed and the input is no longer focused.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This spinner.</returns>
        public Spinner OnChange(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SpinnerAttributeName.OnChange] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the spinner is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This spinner.</returns>
        public Spinner OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SpinnerAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered during increment and decrement.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This spinner.</returns>
        public Spinner OnSpin(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SpinnerAttributeName.OnSpin] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered before a spin.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This spinner.</returns>
        public Spinner OnStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SpinnerAttributeName.OnStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered after a spin.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This spinner.</returns>
        public Spinner OnStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[SpinnerAttributeName.OnStop] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Returns the HTML-encoded representation of the spinner.
        /// </summary>
        /// <returns>The HTML-encoded representation of the spinner.</returns>
        public override string ToHtmlString()
        {
            string fullName = m_HtmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(m_Name);
            Guard.ArgumentNotNullOrWhiteSpace(fullName, "name");

            m_HtmlAttributes[SpinnerAttributeName.Culture] = m_Culture != null ? m_Culture.Name : CultureInfo.CurrentCulture.Name;

            if (!String.IsNullOrWhiteSpace(m_NumberFormat))
            {
                m_HtmlAttributes[SpinnerAttributeName.NumberFormat] = m_NumberFormat;
                string formatString = "{0:" + m_NumberFormat + "}";
                return m_HtmlHelper.TextBox(m_Name, String.Format(m_Culture, formatString, m_Value), m_HtmlAttributes).ToHtmlString();
            }
            else
            {
                return m_HtmlHelper.TextBox(m_Name, m_Value, m_HtmlAttributes).ToHtmlString();
            }
        }
    }
}
