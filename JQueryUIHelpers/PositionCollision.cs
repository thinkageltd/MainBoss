namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies the collision type for a position.
    /// </summary>
    public enum PositionCollision
    {
        /// <summary>
        /// Apply No Collision Detection.
        /// </summary>
        None,

        /// <summary>
        /// The element is flipped to the opposite side.
        /// </summary>
        Flip,

        /// <summary>
        /// The element is re-positioned.
        /// </summary>
        Fit,

        /// <summary>
        /// First flip then fit.
        /// </summary>
        Flipfit
    }
}
