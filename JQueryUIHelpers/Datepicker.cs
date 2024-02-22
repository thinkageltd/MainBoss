using System;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JQueryUIHelpers {
	/// <summary>
	/// Represents a datepicker widget.
	/// </summary>
	public class Datepicker : Widget {
		private readonly string m_Name;
		private readonly DateTime? m_Value;

		/// <summary>
		/// virtual so datetimepicker can supplement with the TimeFormat as required.
		/// </summary>
		protected virtual string FormattingString {
			get {
				return m_FormatString;
			}
		}
		private string m_FormatString;
		private DateTime? m_DefaultDate;
		private DateTime? m_MaxDate;
		private DateTime? m_MinDate;
		private bool m_NavigationAsDateFormat;
		private bool m_Inline;

		/// <summary>
		/// Initializes a new instance of the <see cref="Datepicker"/> class.
		/// </summary>
		/// <param name="htmlHelper">The HtmlHelper.</param>
		/// <param name="name">The name of the datepicker.</param>
		/// <param name="value">The value of the datepicker.</param>
		/// <param name="htmlAttributes">The HTML attributes.</param>
		/// <param name="formatString">The date format string.</param>
		public Datepicker(HtmlHelper htmlHelper, string name, DateTime? value, object htmlAttributes, string formatString)
			: base(htmlHelper, htmlAttributes) {
			m_Name = name;
			m_Value = value;

			m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Datepicker);

			if (String.IsNullOrWhiteSpace(formatString)) {
				m_FormatString = DateTimeFormatInfo.CurrentInfo.ShortDatePattern;
			}
			else {
				m_FormatString = formatString;
			}

			m_HtmlAttributes[DatepickerAttributeName.DateFormat] = DateFormatUtility.DotNetFormatToJQueryUIFormat(m_FormatString);
		}

		/// <summary>
		/// Sets the selector and format of another field that will be updated with the selected date from the datepicker.
		/// </summary>
		/// <param name="selector">The jQuery selector for the other field.</param>
		/// <param name="format">The date format string.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker AlternateField(string selector, string format) {
			Guard.ArgumentNotNull(() => selector);
			Guard.ArgumentNotNull(() => format);
			m_HtmlAttributes[DatepickerAttributeName.AlternateField] = selector;
			m_HtmlAttributes[DatepickerAttributeName.AlternateFormat] = DateFormatUtility.DotNetFormatToJQueryUIFormat(format);
			return this;
		}

		/// <summary>
		/// Sets the text to display after the datepicker.
		/// </summary>
		/// <param name="text">The text to display.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker AppendText(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.AppendText] = text;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether the datepicker should automatically resize the input field to accommodate the date.
		/// </summary>
		/// <param name="autoSize">If true, the datepicker automatically resizes the input field to accommodate the date.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker AutoSize(bool autoSize) {
			m_HtmlAttributes[DatepickerAttributeName.AutoSize] = autoSize;
			return this;
		}

		/// <summary>
		/// Sets the name of the JavaScript function that will be used to calculate the week numbers.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker CalculateWeek(string functionName) {
			Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
			m_HtmlAttributes[DatepickerAttributeName.CalculateWeek] = functionName;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to show the month drop-down list.
		/// </summary>
		/// <param name="changeMonth">If true, the month drop-down list is visible.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ChangeMonth(bool changeMonth) {
			m_HtmlAttributes[DatepickerAttributeName.ChangeMonth] = changeMonth;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to show the year drop-down list.
		/// </summary>
		/// <param name="changeYear">If true, the year drop-down list is visible.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ChangeYear(bool changeYear) {
			m_HtmlAttributes[DatepickerAttributeName.ChangeYear] = changeYear;
			return this;
		}

		/// <summary>
		/// Shows the year-drop down and defines its range.
		/// </summary>        
		/// <param name="from">The lowest year in the drop-down.</param>
		/// <param name="to">The highest year in the drop-down.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ChangeYear(YearDefinition from, YearDefinition to) {
			Guard.ArgumentNotNull(() => from);
			Guard.ArgumentNotNull(() => to);
			m_HtmlAttributes[DatepickerAttributeName.ChangeYear] = true;
			m_HtmlAttributes[DatepickerAttributeName.YearRange] = String.Format("{0}:{1}", from, to);
			return this;
		}

		/// <summary>
		/// Sets the text of the close button.
		/// </summary>
		/// <param name="text">The text of the close button.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker CloseText(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.CloseText] = text;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to constrain entry in the input field to the current date format.
		/// </summary>
		/// <param name="constraintInput">If true, entry in the input field is constrained to those characters allowed by the current date format.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ConstrainInput(bool constraintInput) {
			m_HtmlAttributes[DatepickerAttributeName.ConstrainInput] = constraintInput;
			return this;
		}

		/// <summary>
		/// Sets the text of current day button.
		/// </summary>
		/// <param name="text">The text of the current day button.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker CurrentText(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.CurrentText] = text;
			return this;
		}

		/// <summary>
		/// Sets the format for parsed and displayed dates.
		/// </summary>
		/// <param name="format">The date format string. This should be a valid .NET date format string</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DateFormat(string format) {
			Guard.ArgumentNotNullOrWhiteSpace(() => format);
			m_FormatString = format;
			m_HtmlAttributes[DatepickerAttributeName.DateFormat] = DateFormatUtility.DotNetFormatToJQueryUIFormat(format);
			return this;
		}

		/// <summary>
		/// Sets the full names of the days (e.g. Sunday, Monday).
		/// </summary>
		/// <param name="names">The name list containing exactly 7 names, starting from Sunday.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DayNames(string[] names) {
			Guard.ArgumentNotNull(() => names);
			if (names.Length != 7) {
				throw new ArgumentException(StringResource.DayNamesNotEqualSeven, "names");
			}

			m_HtmlAttributes[DatepickerAttributeName.DayNames] = String.Join(",", names);
			return this;
		}

		/// <summary>
		/// Sets the minimum names of the days (e. g. Su, Mo).
		/// </summary>
		/// <param name="names">The name list containing exactly 7 names, starting from Sunday.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DayNamesMin(string[] names) {
			Guard.ArgumentNotNull(() => names);
			if (names.Length != 7) {
				throw new ArgumentException(StringResource.DayNamesNotEqualSeven, "names");
			}

			m_HtmlAttributes[DatepickerAttributeName.DayNamesMin] = String.Join(",", names);
			return this;
		}

		/// <summary>
		/// Sets the short names of the days (e. g. Sun, Mon).
		/// </summary>
		/// <param name="names">The name list containing exactly 7 names, starting from Sunday.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DayNamesShort(string[] names) {
			Guard.ArgumentNotNull(() => names);
			if (names.Length != 7) {
				throw new ArgumentException(StringResource.DayNamesNotEqualSeven, "names");
			}

			m_HtmlAttributes[DatepickerAttributeName.DayNamesShort] = String.Join(",", names);
			return this;
		}

		/// <summary>
		/// Sets the date to highlight on the first opening if the field is blank.
		/// </summary>
		/// <param name="daysFromToday">The date definition as a number of days from today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DefaultDate(int daysFromToday) {
			m_DefaultDate = null;
			m_HtmlAttributes[DatepickerAttributeName.DefaultDate] = daysFromToday;
			return this;
		}

		/// <summary>
		/// Sets the date to highlight on the first opening if the field is blank.
		/// </summary>
		/// <param name="date">The date.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DefaultDate(DateTime date) {
			m_DefaultDate = date;
			return this;
		}

		/// <summary>
		/// Sets the date to highlight on the first opening if the field is blank.
		/// </summary>
		/// <param name="difference">The date definition as a difference from today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker DefaultDate(DateDifference difference) {
			Guard.ArgumentNotNull(() => difference);
			m_DefaultDate = null;
			m_HtmlAttributes[DatepickerAttributeName.DefaultDate] = difference.ToString();
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether the datepicker is disabled.
		/// </summary>
		/// <param name="disabled">If true, the datepicker is disabled.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker Disabled(bool disabled) {
			m_HtmlAttributes[DatepickerAttributeName.Disabled] = disabled;
			return this;
		}

		/// <summary>
		/// Sets the first day of the week.
		/// </summary>
		/// <param name="day">The number of the first day. Sunday is 0, Monday is 1, ...</param>
		/// <returns>This datepicker.</returns>
		public Datepicker FirstDay(uint day) {
			Guard.ArgumentInRange(() => day, 0, 6);
			m_HtmlAttributes[DatepickerAttributeName.FirstDay] = day;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether the current day button should move to the currently selected date.
		/// </summary>
		/// <param name="gotoCurrent">If true, the current day button moves to the currently selected date instead of today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker GotoCurrent(bool gotoCurrent) {
			m_HtmlAttributes[DatepickerAttributeName.GotoCurrent] = gotoCurrent;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to show the previous and next links when they are not applicable.
		/// </summary>
		/// <param name="hideIfNoPrevNext">If true, the previous and next links are hidden when they are not applicable, otherwise they are just disabled.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker HideIfNoPrevNext(bool hideIfNoPrevNext) {
			m_HtmlAttributes[DatepickerAttributeName.HideIfNoPrevNext] = hideIfNoPrevNext;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether the current language is drawn from right to left.
		/// </summary>
		/// <param name="isRtl">If true, the current language is drawn from right to left.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker IsRtl(bool isRtl) {
			m_HtmlAttributes[DatepickerAttributeName.IsRtl] = isRtl;
			return this;
		}

		/// <summary>
		/// Sets the maximum selectable date.
		/// </summary>
		/// <param name="daysFromToday">The date definition as a number of days from today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MaxDate(int daysFromToday) {
			m_MaxDate = null;
			m_HtmlAttributes[DatepickerAttributeName.MaxDate] = daysFromToday;
			return this;
		}

		/// <summary>
		/// Sets the maximum selectable date.
		/// </summary>
		/// <param name="date">The date.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MaxDate(DateTime date) {
			m_MaxDate = date;
			return this;
		}

		/// <summary>
		/// Sets the maximum selectable date.
		/// </summary>
		/// <param name="difference">The date definition as a difference from today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MaxDate(DateDifference difference) {
			Guard.ArgumentNotNull(() => difference);
			m_MaxDate = null;
			m_HtmlAttributes[DatepickerAttributeName.MaxDate] = difference.ToString();
			return this;
		}

		/// <summary>
		/// Sets the minimum selectable date.
		/// </summary>
		/// <param name="daysFromToday">The date definition as a number of days from today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MinDate(int daysFromToday) {
			m_MinDate = null;
			m_HtmlAttributes[DatepickerAttributeName.MinDate] = daysFromToday;
			return this;
		}

		/// <summary>
		/// Sets the minimum selectable date.
		/// </summary>
		/// <param name="date">The date.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MinDate(DateTime date) {
			m_MinDate = date;
			return this;
		}

		/// <summary>
		/// Sets the minimum selectable date.
		/// </summary>
		/// <param name="difference">The date definition as a difference from today.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MinDate(DateDifference difference) {
			Guard.ArgumentNotNull(() => difference);
			m_MinDate = null;
			m_HtmlAttributes[DatepickerAttributeName.MinDate] = difference.ToString();
			return this;
		}

		/// <summary>
		/// Sets the full names of the months (e.g. January, February).
		/// </summary>
		/// <param name="names">The name list containing exactly 12 names, starting from January.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MonthNames(string[] names) {
			Guard.ArgumentNotNull(() => names);
			if (names.Length != 12) {
				throw new ArgumentException(StringResource.MonthNamesNotEqualTwelve, "names");
			}

			m_HtmlAttributes[DatepickerAttributeName.MonthNames] = String.Join(",", names);
			return this;
		}

		/// <summary>
		/// Sets the short names of the months (e.g. Jan, Feb).
		/// </summary>
		/// <param name="names">The name list containing exactly 12 names, starting from January.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker MonthNamesShort(string[] names) {
			Guard.ArgumentNotNull(() => names);
			if (names.Length != 12) {
				throw new ArgumentException(StringResource.MonthNamesNotEqualTwelve, "names");
			}

			m_HtmlAttributes[DatepickerAttributeName.MonthNamesShort] = String.Join(",", names);
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to treat the CurrentText, PrevText and NextText as date format strings.
		/// </summary>
		/// <param name="navigationAsDateFormat">If true, the CurrentText, PrevText and NextText are treated as date format strings.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker NavigationAsDateFormat(bool navigationAsDateFormat) {
			m_NavigationAsDateFormat = navigationAsDateFormat;
			m_HtmlAttributes[DatepickerAttributeName.NavigationAsDateFormat] = navigationAsDateFormat;
			return this;
		}

		/// <summary>
		/// Sets the text of the next month link.
		/// </summary>
		/// <param name="text">The text of the next month link.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker NextText(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.NextText] = text;
			return this;
		}

		/// <summary>
		/// Sets the number of months displayed.
		/// </summary>
		/// <param name="numberOfMonths">The number of months.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker NumberOfMonths(int numberOfMonths) {
			Guard.ArgumentInRange(() => numberOfMonths, 1, 12);
			m_HtmlAttributes[DatepickerAttributeName.NumberOfMonths] = numberOfMonths;
			return this;
		}

		/// <summary>
		/// Sets the number of months displayed and the position of the current month.
		/// </summary>
		/// <param name="numberOfMonths">The number of months.</param>
		/// <param name="showCurrentAtPosition">The position of the current month, starting from 0.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker NumberOfMonths(int numberOfMonths, int showCurrentAtPosition) {
			Guard.ArgumentInRange(() => numberOfMonths, 1, 12);
			m_HtmlAttributes[DatepickerAttributeName.NumberOfMonths] = numberOfMonths;
			m_HtmlAttributes[DatepickerAttributeName.ShowCurrentAtPos] = showCurrentAtPosition;
			return this;
		}

		/// <summary>
		/// Sets the number of month rows and columns to display and the position of the current month.
		/// </summary>
		/// <param name="rows">The number of rows.</param>
		/// <param name="columns">The number of columns.</param>
		/// <param name="showCurrentAtPosition">The position of the current month, starting from 0.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker NumberOfMonths(int rows, int columns, int showCurrentAtPosition) {
			Guard.ArgumentInRange(() => rows, 1, 6);
			Guard.ArgumentInRange(() => columns, 1, 6);
			m_HtmlAttributes[DatepickerAttributeName.NumberOfMonths] = String.Format("{0},{1}", rows, columns);
			m_HtmlAttributes[DatepickerAttributeName.ShowCurrentAtPos] = showCurrentAtPosition;
			return this;
		}

		/// <summary>
		/// Sets the text of the previous month link.
		/// </summary>
		/// <param name="text">The text of the previous month link.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker PrevText(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.PrevText] = text;
			return this;
		}

		/// <summary>
		/// Sets the cutoff year for determining the century for a date when a 2-digit year format is used. Any dates entered with a year value less than or equal to it are considered to be in the current century, while those greater than it are deemed to be in the previous century.
		/// </summary>
		/// <param name="year">The cutoff year.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShortYearCutoff(uint year) {
			Guard.ArgumentInRange(() => year, 0, 99);
			m_HtmlAttributes[DatepickerAttributeName.ShortYearCutoff] = year;
			return this;
		}

		/// <summary>
		/// Sets the animation used to show or hide the datepicker.
		/// </summary>
		/// <param name="animation">The animation.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowAnimation(DatepickerAnimation animation) {
			AddAnimationAttribute(animation);
			return this;
		}

		/// <summary>
		/// Sets the animation and its duration used to show or hide the datepicker.
		/// </summary>
		/// <param name="animation">The animation.</param>
		/// <param name="duration">The duration in milliseconds.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowAnimation(DatepickerAnimation animation, uint duration) {
			AddAnimationAttribute(animation);
			m_HtmlAttributes[DatepickerAttributeName.Duration] = duration;
			return this;
		}

		/// <summary>
		/// Sets the animation and its duration used to show or hide the datepicker.
		/// </summary>
		/// <param name="animation">The animation.</param>
		/// <param name="duration">The duration.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowAnimation(DatepickerAnimation animation, Duration duration) {
			AddAnimationAttribute(animation);
			m_HtmlAttributes[DatepickerAttributeName.Duration] = duration.ToString().StartLowerInvariant();
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to show the button panel.
		/// </summary>
		/// <param name="showButtonPanel">If true, the button panel is displayed.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowButtonPanel(bool showButtonPanel) {
			m_HtmlAttributes[DatepickerAttributeName.ShowButtonPanel] = showButtonPanel;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to show the month after the year in the display panel.
		/// </summary>
		/// <param name="showMonthAfterYear">If true, the month is shown after the year.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowMonthAfterYear(bool showMonthAfterYear) {
			m_HtmlAttributes[DatepickerAttributeName.ShowMonthAfterYear] = showMonthAfterYear;
			return this;
		}

		/// <summary>
		/// Sets the event (focus, trigger button, both) which displays the datepicker.
		/// </summary>
		/// <param name="showOn">The event.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowOn(DatepickerShowOn showOn) {
			m_HtmlAttributes[DatepickerAttributeName.ShowOn] = showOn.ToString().StartLowerInvariant();
			return this;
		}

		/// <summary>
		/// Sets the event (focus, trigger button, both) which displays the datepicker and the parameters of the button.
		/// The button parameters have no effect if the first parameter is set to DatepickerShowOn.Focus.
		/// </summary>
		/// <param name="showOn">The event.</param>
		/// <param name="imageUrl">The URL of the trigger button image.</param>
		/// <param name="text">The text of the trigger button.</param>
		/// <param name="imageOnly">If true the image is used as a trigger button.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowOn(DatepickerShowOn showOn, string imageUrl, string text, bool imageOnly) {
			m_HtmlAttributes[DatepickerAttributeName.ShowOn] = showOn.ToString().StartLowerInvariant();
			if (!String.IsNullOrWhiteSpace(imageUrl)) {
				m_HtmlAttributes[DatepickerAttributeName.ButtonImage] = imageUrl;
			}

			if (!String.IsNullOrWhiteSpace(text)) {
				m_HtmlAttributes[DatepickerAttributeName.ButtonText] = text;
			}

			m_HtmlAttributes[DatepickerAttributeName.ButtonImageOnly] = imageOnly;
			return this;
		}

		/// <summary>
		/// Sets how dates in other months are displayed.
		/// </summary>
		/// <param name="otherMonths">The state of the dates in other months.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowOtherMonths(OtherMonths otherMonths) {
			m_HtmlAttributes[DatepickerAttributeName.ShowOtherMonths] = otherMonths != OtherMonths.Hidden;
			m_HtmlAttributes[DatepickerAttributeName.SelectOtherMonths] = otherMonths == OtherMonths.VisibleAndSelectable;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether to show the numbers of the weeks.
		/// </summary>
		/// <param name="showWeek">If true, the week numbers are displayed.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker ShowWeek(bool showWeek) {
			m_HtmlAttributes[DatepickerAttributeName.ShowWeek] = showWeek;
			return this;
		}

		/// <summary>
		/// Sets how many months to move when clicking the previous or next links.
		/// </summary>
		/// <param name="months">The number of months to step.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker StepMonths(int months) {
			Guard.ArgumentInRange(() => months, 1, 1000);
			m_HtmlAttributes[DatepickerAttributeName.StepMonths] = months;
			return this;
		}

		/// <summary>
		/// Sets the text of the week header.
		/// </summary>
		/// <param name="text">The text of the week header.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker WeekHeader(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.WeekHeader] = text;
			return this;
		}

		/// <summary>
		/// Sets the text to display after the year in the datepicker header.
		/// </summary>
		/// <param name="text">The year suffix.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker YearSuffix(string text) {
			Guard.ArgumentNotNull(() => text);
			m_HtmlAttributes[DatepickerAttributeName.YearSuffix] = text;
			return this;
		}

		/// <summary>
		/// Sets a value indicating whether the datepicker should be displayed inline.
		/// </summary>
		/// <param name="inline">If true, the datepicker is displayed inline.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker Inline(bool inline) {
			m_Inline = inline;
			return this;
		}

		#region Events

		/// <summary>
		/// Sets the name of the JavaScript function that is triggered before the datepicker is displayed.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker OnBeforeShow(string functionName) {
			Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
			m_HtmlAttributes[DatepickerAttributeName.OnBeforeShow] = functionName;
			return this;
		}

		/// <summary>
		/// Sets the name of the JavaScript function that is triggered before a day is displayed. Called for each day in the datepicker.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker OnBeforeShowDay(string functionName) {
			Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
			m_HtmlAttributes[DatepickerAttributeName.OnBeforeShowDay] = functionName;
			return this;
		}

		/// <summary>
		/// Sets the name of the JavaScript function that is triggered when the datepicker move to a new month and/or year.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker OnChangeMonthYear(string functionName) {
			Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
			m_HtmlAttributes[DatepickerAttributeName.OnChangeMonthYear] = functionName;
			return this;
		}

		/// <summary>
		/// Sets the name of the JavaScript function that is triggered when the datepicker is closed.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker OnClose(string functionName) {
			Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
			m_HtmlAttributes[DatepickerAttributeName.OnClose] = functionName;
			return this;
		}

		/// <summary>
		/// Sets the name of the JavaScript function that is triggered when a date is selected.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>This datepicker.</returns>
		public Datepicker OnSelect(string functionName) {
			Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
			m_HtmlAttributes[DatepickerAttributeName.OnSelect] = functionName;
			return this;
		}

		#endregion

		/// <summary>
		/// Returns the HTML-encoded representation of the datepicker.
		/// </summary>
		/// <returns>The HTML-encoded representation of the datepicker.</returns>
		public override string ToHtmlString() {
			string fullName = m_HtmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(m_Name);
			Guard.ArgumentNotNullOrWhiteSpace(fullName, "name");
			if (m_DefaultDate != null) {
				m_HtmlAttributes[DatepickerAttributeName.DefaultDate] = m_DefaultDate.Value.ToString(m_FormatString);
			}

			if (m_MaxDate != null) {
				m_HtmlAttributes[DatepickerAttributeName.MaxDate] = m_MaxDate.Value.ToString(m_FormatString);
			}

			if (m_MinDate != null) {
				m_HtmlAttributes[DatepickerAttributeName.MinDate] = m_MinDate.Value.ToString(m_FormatString);
			}

			if (m_NavigationAsDateFormat) {
				ChangeAttributeToJQueryFormat(DatepickerAttributeName.CurrentText);
				ChangeAttributeToJQueryFormat(DatepickerAttributeName.PrevText);
				ChangeAttributeToJQueryFormat(DatepickerAttributeName.NextText);
			}

			if (!m_Inline) {
				return m_HtmlHelper.TextBox(m_Name,
					m_Value?.ToString(FormattingString),
					m_HtmlAttributes).ToHtmlString();
			}
			else {
				m_HtmlAttributes[DatepickerAttributeName.HiddenValue] = HtmlHelper.GenerateIdFromName(fullName);
				string hidden = m_HtmlHelper.Hidden(m_Name, m_Value?.ToString(FormattingString)).ToHtmlString();
				TagBuilder divBuilder = new TagBuilder("div");
				divBuilder.MergeAttributes(m_HtmlAttributes);
				string datepicker = divBuilder.ToString();
				return datepicker + hidden;
			}
		}

		/// <summary>
		/// Adds ShowAnim to the HTML attributes and sets its value based on the specified animation.
		/// </summary>
		/// <param name="animation">The animation.</param>
		private void AddAnimationAttribute(DatepickerAnimation animation) {
			if (animation == DatepickerAnimation.None) {
				m_HtmlAttributes[DatepickerAttributeName.ShowAnim] = String.Empty;
			}
			else {
				m_HtmlAttributes[DatepickerAttributeName.ShowAnim] = animation.ToString().StartLowerInvariant();
			}
		}

		/// <summary>
		/// Changes the value of specified HTML attribute to the corresponding jQuery date format string.
		/// </summary>
		/// <param name="attributeName">The name of the HTML attribute.</param>
		private void ChangeAttributeToJQueryFormat(string attributeName) {
			if (m_HtmlAttributes.ContainsKey(attributeName)) {
				m_HtmlAttributes[attributeName] = DateFormatUtility.DotNetFormatToJQueryUIFormat(m_HtmlAttributes[attributeName].ToString());
			}
		}
	}
}
