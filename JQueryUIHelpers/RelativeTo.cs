namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies a date which is used as a base date when calculating a date.
    /// </summary>
    public enum RelativeTo
    {
        /// <summary>
        /// Today's year should be used as the base date.
        /// </summary>
        TodaysYear,

        /// <summary>
        /// The year selected in the datepicker should be used as the base date.
        /// </summary>
        SelectedYear
    }
}
