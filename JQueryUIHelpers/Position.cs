using System;
using System.Text;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a position.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Gets or sets which horizontal position will be aligned with the target element.
        /// </summary>
        public HorizontalPosition MyHorizontal { get; set; }

        /// <summary>
        /// Gets or sets which vertical position will be aligned with the target element.
        /// </summary>
        public VerticalPosition MyVertical { get; set; }

        /// <summary>
        /// Gets or sets which horizontal position on the target element to align the positioned element against.
        /// </summary>
        public HorizontalPosition AtHorizontal { get; set; }

        /// <summary>
        /// Gets or sets which vertical position on the target element to align the positioned element against.
        /// </summary>
        public VerticalPosition AtVertical { get; set; }

        /// <summary>
        /// Gets or sets the value to add to the top position of the element.
        /// </summary>
        public int? OffsetTop { get; set; }

        /// <summary>
        /// Gets or sets the value to add to the left position of the element.
        /// </summary>
        public int? OffsetLeft { get; set; }

        /// <summary>
        /// Gets or sets the jQuery selector of the element to position against.
        /// </summary>
        public string OfSelector { get; set; }

        /// <summary>
        /// Gets or sets the horizontal collision.
        /// </summary>
        public PositionCollision HorizontalCollision { get; set; }
        
        /// <summary>
        /// Gets or sets the vertical collision.
        /// </summary>
        public PositionCollision VerticalCollision { get; set; }

        /// <summary>
        /// Gets or sets the Within attribute
        /// </summary>
        public string Within { get; set; }

        /// <summary>
        /// Returns a string that represents the current Position.
        /// </summary>
        /// <returns>The string representation of the Position.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("{\"my\":\"");
            builder.Append(MyHorizontal.ToString().ToLowerInvariant());
            if (OffsetLeft != null && OffsetLeft.Value != 0)
            {
                builder.AppendFormat("{0:+0;-#}", OffsetLeft);
            }

            builder.Append(" ");
            builder.Append(MyVertical.ToString().ToLowerInvariant());
            if (OffsetTop != null && OffsetTop.Value != 0)
            {
                builder.AppendFormat("{0:+0;-#}", OffsetTop);
            }

            builder.AppendFormat("\",\"at\":\"{0} {1}\"",  AtHorizontal.ToString().ToLowerInvariant(), AtVertical.ToString().ToLowerInvariant());
            builder.AppendFormat(",\"collision\":\"{0} {1}\"", HorizontalCollision.ToString().ToLowerInvariant(),
                VerticalCollision.ToString().ToLowerInvariant());
            if (!String.IsNullOrWhiteSpace(OfSelector))
            {
                builder.AppendFormat(",\"of\":\"{0}\"", OfSelector);
            }

            if (!String.IsNullOrWhiteSpace(Within))
            {
                builder.AppendFormat(",\"within\":\"{0}\"", Within);
            }

            builder.Append("}");
            return builder.ToString();
        }
    }
}
