using System;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a year definition.
    /// </summary>
    public class YearDefinition
    {
        private uint? m_AbsoluteValue;
        private int? m_Difference;
        private RelativeTo? m_RelativeTo;

        /// <summary>
        /// Initializes a new instance of the <see cref="YearDefinition"/> class
        /// with the specified year.
        /// </summary>
        /// <param name="absolute">The year.</param>
        public YearDefinition(uint absolute)
        {
            m_AbsoluteValue = absolute;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YearDefinition"/> class
        /// either relative to the current year or to the currently selected year.
        /// </summary>
        /// <param name="difference">The difference in years.</param>
        /// <param name="relativeTo">Indicates whether the year is relative to today's year or to the currently selected year.</param>
        public YearDefinition(int difference, RelativeTo relativeTo)
        {
            m_Difference = difference;
            m_RelativeTo = relativeTo;
        }

        /// <summary>
        /// Returns a string that represents the current YearDefinition.
        /// </summary>
        /// <returns>The string representation of the YearDefinition.</returns>
        public override string ToString()
        {
            if (m_AbsoluteValue != null)
            {
                return m_AbsoluteValue.ToString();
            }
            else if (m_RelativeTo.Value == RelativeTo.TodaysYear)
            {
                return m_Difference.Value.ToString("+0;-#");
            }
            else
            {
                return String.Format("c{0:+0;-#}", m_Difference);
            }
        }
    }
}
