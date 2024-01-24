using System;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a datetimepicker widget (derived from Datepicker)
    /// </summary>
    public class Datetimepicker : Datepicker
    {
		private string m_FormatString;
		/// <summary>
		/// The format string as it applies to the Time
		/// </summary>
		protected override string FormattingString {
			get {
				if (String.IsNullOrEmpty(m_FormatString))
					return base.FormattingString;
				else if (String.IsNullOrEmpty(base.FormattingString))
					return m_FormatString;
				else
					return base.FormattingString + " " + m_FormatString;
			}
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="Datetimepicker"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="name">The name of the datepicker.</param>
        /// <param name="value">The value of the datepicker.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
		/// <param name="dateFormatString">The date format string.</param>
		/// <param name="timeFormatString">The time format string.</param>
		public Datetimepicker(HtmlHelper htmlHelper, string name, DateTime? value, object htmlAttributes, string dateFormatString, string timeFormatString)
			: base(htmlHelper, name, value, htmlAttributes, dateFormatString)
        {
			m_HtmlAttributes.Remove(CommonHtmlAttributeName.JQueryUIType); // change Datepicker to Datetimepicker
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Datetimepicker);

			if (String.IsNullOrWhiteSpace(timeFormatString))
            {
                m_FormatString = DateTimeFormatInfo.CurrentInfo.ShortTimePattern;
            }
            else
            {
				m_FormatString = timeFormatString;
            }
            m_HtmlAttributes[DatetimepickerAttributeName.TimeFormat] = TimeFormatUtility.DotNetFormatToJQueryUIFormat(m_FormatString);
        }
		/// <summary>
		/// Sets the format for parsed and displayed time.
		/// </summary>
		/// <param name="format">The time format string. This should be a valid .NET time format string</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker TimeFormat(string format) {
			Guard.ArgumentNotNullOrWhiteSpace(() => format);
			m_FormatString = format;
			m_HtmlAttributes[DatetimepickerAttributeName.TimeFormat] = TimeFormatUtility.DotNetFormatToJQueryUIFormat(format);
			return this;
		}
		/// <summary>
		/// Sets the AM names
		/// </summary>
		/// <param name="names">The name list containing strings to determine AM</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker AmNames(string[] names) {
			Guard.ArgumentNotNull(() => names);
			m_HtmlAttributes[DatetimepickerAttributeName.AmNames] = String.Join(",", names);
			return this;
		}
		/// <summary>
		/// Sets the PM names
		/// </summary>
		/// <param name="names">The name list containing strings to determine AM</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker PmNames(string[] names) {
			Guard.ArgumentNotNull(() => names);
			m_HtmlAttributes[DatetimepickerAttributeName.PmNames] = String.Join(",", names);
			return this;
		}
		/// <summary>
		/// Sets the time hour
		/// </summary>
		/// <param name="hour">The initial hour.</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker Hour(int hour) {
			Guard.ArgumentInRange( hour, 0, 23, "hour");
			m_HtmlAttributes[DatetimepickerAttributeName.Hour] = hour;
			return this;
		}
		/// <summary>
		/// Sets the time minute
		/// </summary>
		/// <param name="minute">The initial minute.</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker Minute(int minute) {
			Guard.ArgumentInRange(minute, 0, 59, "minute");
			m_HtmlAttributes[DatetimepickerAttributeName.Minute] = minute;
			return this;
		}
		/// <summary>
		/// Sets the time second
		/// </summary>
		/// <param name="second">The initial second.</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker Second(int second) {
			Guard.ArgumentInRange(second, 0, 59, "second"); //TODO: min/Max should be secondMin/Max if set
			m_HtmlAttributes[DatetimepickerAttributeName.Second] = second;
			return this;
		}
		/// <summary>
		/// Sets the time millisec
		/// </summary>
		/// <param name="millisec">The initial millisec.</param>
		/// <returns>This datepicker.</returns>
		public Datetimepicker Millisec(int millisec) {
			Guard.ArgumentInRange(millisec, 0, 999, "millisec");
			m_HtmlAttributes[DatetimepickerAttributeName.Millisec] = millisec;
			return this;
		}
	}
}
