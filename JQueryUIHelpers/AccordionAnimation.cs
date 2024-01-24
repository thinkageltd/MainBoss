using System;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Specifies the animation of an accordion.
    /// </summary>
    /// <remarks>Depreciated: http://bugs.jqueryui.com/ticket/8600 </remarks>
    [Obsolete("Depreciated: Use Animate(Easing easing, uint duration, Easing downEasing, uint downDuration)")]
    public enum AccordionAnimation
    {
        /// <summary>
        /// The accordion is not animated.
        /// </summary>
        None,

        /// <summary>
        /// Request Slide Animation.
        /// </summary>
        Slide,

        /// <summary>
        /// Request BounceSlide Animation.
        /// </summary>
        BounceSlide
    }
}
