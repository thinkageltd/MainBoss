namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies a revert mode.
    /// </summary>
    public enum DraggableRevert
    {
        /// <summary>
        /// Revert Mode is Always.
        /// </summary>
        Always,

        /// <summary>
        /// The draggable has not been dropped on a droppable.
        /// </summary>
        Invalid,

        /// <summary>
        /// The draggable has been dropped on a droppable.
        /// </summary>
        Valid
    }
}
