using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JQueryUIHelpers
{
    internal static class DateFormatUtility
    {
        /// <summary>
        /// Creates a jQuery UI date format string from the specified .NET format string.
        /// </summary>
        /// <remarks>See: http://docs.jquery.com/UI/Datepicker/formatDate </remarks>
        /// <param name="formatString">The .NET format string.</param>
        /// <returns>The jQuery UI format string.</returns>
        public static string DotNetFormatToJQueryUIFormat(string formatString)
        {
            StringBuilder builder = new StringBuilder();
            bool inText = false;
            char? textStarter = null;
            int formatCharCount = 0;
            for (int i = 0; i < formatString.Length; i++)
            {
                char c = formatString[i];
                if ((c == '"' || c == '\'') && !inText)
                {
                    textStarter = c;
                    inText = true;
                    builder.Append(c);
                    continue;
                }

                if (inText || "yMd".IndexOf(c) < 0)
                {
                    builder.Append(c);
                }
                else if (i == formatString.Length - 1 || c != formatString[i + 1])
                {
                    formatCharCount++;
                    builder.Append(ConvertFormatPart(c, formatCharCount));
                    formatCharCount = 0;
                }
                else
                {
                    formatCharCount++;
                }

                if ((c == '"' || c == '\'') && inText && c == textStarter)
                {
                    textStarter = null;
                    inText = false;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Removes the {0} references from the specified format string.
        /// The returned string is usable for the DateTime.ToString and the DotNetFormatToJQueryUIFormat methods.
        /// </summary>
        /// <param name="formatString">The format string.</param>
        /// <returns>The normalized format string.</returns>
        public static string NormalizeDateFormatString(string formatString)
        {
            Regex pattern = new Regex(@"\{0:(?<val>.*?)\}");
            string result = "'" + pattern.Replace(formatString,
                me => 
                {
                    string r = "'";
                    if (me.Groups["val"].Value == "d")
                    {
                        r += DateTimeFormatInfo.CurrentInfo.ShortDatePattern;
                    }
                    else if (me.Groups["val"].Value == " d" || me.Groups["val"].Value == "%d" || me.Groups["val"].Value == "d ")
                    {
                        r += "d";
                    }
                    else
                    {
                        r += me.Groups["val"].Value;
                    }

                    return r + "'";
                }) + "'";
                    
            if (result.StartsWith("''"))
            {
                result = result.Remove(0, 2);
            }

            if (result.EndsWith("''"))
            {
                result = result.Remove(result.Length - 2, 2);
            }

            return result;
        }

        private static string ConvertFormatPart(char c, int count)
        {
            if (c == 'd' || c == 'M')
            {
                if (count <= 2)
                {
                    return new String(Char.ToLowerInvariant(c), count);
                }
                else
                {
                    return new String(Char.ToUpperInvariant(c), count - 2);
                }
            }

            if (c == 'y')
            {
                return new String(c, count / 2);
            }

            return String.Empty;
        }
    }
}
