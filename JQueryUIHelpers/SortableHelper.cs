using System;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies a sortable helper.
    /// </summary>
    [Obsolete("Use DragHelper Enum.")]
    public enum SortableHelper
    {
        /// <summary>
        /// Set the Helper to the Original object.
        /// </summary>
        Original,

        /// <summary>
        /// Set the Helper to a Clone of the original object.
        /// </summary>
        Clone
    }
}