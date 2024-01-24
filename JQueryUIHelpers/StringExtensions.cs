using System;

namespace JQueryUIHelpers
{
    internal static class StringExtensions
    {
        public static string StartLowerInvariant(this string s)
        {
            if (!String.IsNullOrWhiteSpace(s))
            {
                return Char.ToLowerInvariant(s[0]).ToString() + s.Substring(1);
            }

            return s;
        }
    }
}
