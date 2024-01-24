using System.Text;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a date difference.
    /// </summary>
    public class DateDifference
    {
        /// <summary>
        /// Gets or sets the number of the years.
        /// </summary>
        public int? Years { get; set; }

        /// <summary>
        /// Gets or sets the number of the months.
        /// </summary>
        public int? Months { get; set; }

        /// <summary>
        /// Gets or sets the number of the weeks.
        /// </summary>
        public int? Weeks { get; set; }

        /// <summary>
        /// Gets or sets the number of the days.
        /// </summary>
        public int? Days { get; set; }

        /// <summary>
        /// Returns a string that represents the current DateDifference.
        /// </summary>
        /// <returns>The string representation of the DateDifference.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            if (Years != null)
            {
                builder.AppendFormat("{0:+0;-#}y", Years);
            }

            if (Months != null)
            {
                builder.AppendFormat("{0:+0;-#}m", Months);
            }

            if (Weeks != null)
            {
                builder.AppendFormat("{0:+0;-#}w", Weeks);
            }

            if (Days != null)
            {
                builder.AppendFormat("{0:+0;-#}d", Days);
            }

            return builder.ToString();
        }
    }
}
