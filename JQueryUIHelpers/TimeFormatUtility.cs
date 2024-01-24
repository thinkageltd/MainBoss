using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JQueryUIHelpers {
	internal static class TimeFormatUtility {
		/// <summary>
		/// Creates a jQuery UI time format string from the specified .NET format string.
		/// </summary>
		/// <param name="formatString">The .NET time format string.</param>
		/// <returns>The jQuery UI format string.</returns>
		/// <remarks>See: http://docs.jquery.com/UI/Datetimepicker/formatTime </remarks>
		/// JQueryUI Format is based on following
		//    H       Hour with no leading 0 (24 hour)
		//    HH      Hour with leading 0 (24 hour)
		//    h       Hour with no leading 0 (12 hour)
		//    hh      Hour with leading 0 (12 hour)
		//    m       Minute with no leading 0
		//    mm      Minute with leading 0
		//    s       Second with no leading 0
		//    ss      Second with leading 0
		//    l       Milliseconds always with leading 0
		//    c       Microseconds always with leading 0
		//    t       a or p for AM/PM
		//    T       A or P for AM/PM
		//    tt      am or pm for AM/PM
		//    TT      AM or PM for AM/PM
		//    z       Timezone as defined by timezoneList
		//    Z       Timezone in Iso 8601 format (+04:45)
		//    '...'   Literal text (Uses single quotes)
		public static string DotNetFormatToJQueryUIFormat(string formatString) {
			return formatString; // The JQueryUI format is basically what .NET does already with a few changes that we ignore for now.
		}
	}
}
