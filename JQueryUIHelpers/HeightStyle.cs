namespace JQueryUIHelpers
{
    /// <summary>
    /// Contains a list of height styles.
    /// </summary>
    public enum HeightStyle
    {
        /// <summary>
        /// All panels are set to the height of the tallest panel.
        /// </summary>
        Auto,
        
        /// <summary>
        /// Expand to the available height based on the parent's height.
        /// </summary>
        Fill,
        
        /// <summary>
        /// Each panel will be as tall as its content.
        /// </summary>
        Content
    }
}
