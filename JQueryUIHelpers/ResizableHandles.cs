using System;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies a resizable handle.
    /// </summary>
    [Flags]
    public enum ResizableHandles
    {
        /// <summary>
        /// Top handle.
        /// </summary>
        N = 1,

        /// <summary>
        /// Right handle.
        /// </summary>
        E = 2,

        /// <summary>
        /// Bottom handle.
        /// </summary>
        S = 4,
        
        /// <summary>
        /// Left handle.
        /// </summary>
        W = 8,
        
        /// <summary>
        /// Top-right handle.
        /// </summary>
        NE = 16,
        
        /// <summary>
        /// Bottom-right handle.
        /// </summary>
        SE = 32,
        
        /// <summary>
        /// Bottom-left handle.
        /// </summary>
        SW = 64,

        /// <summary>
        /// Top-left handle.
        /// </summary>
        NW = 128
    }
}
