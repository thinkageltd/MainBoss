namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies a droppable tolerance.
    /// </summary>
    public enum DroppableTolerance
    {
        /// <summary>
        /// The draggable overlaps the droppable at least 50%.
        /// </summary>
        Intersect,

        /// <summary>
        /// The draggable overlaps the droppable entirely.
        /// </summary>
        Fit,

        /// <summary>
        /// The mouse pointer overlaps the droppable.
        /// </summary>
        Pointer,

        /// <summary>
        /// The draggable overlaps the droppable any amount.
        /// </summary>
        Touch
    }
}
