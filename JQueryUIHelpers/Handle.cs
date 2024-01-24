namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a handle on a slider widget.
    /// </summary>
    public class Handle
    {
        /// <summary>
        /// Gets the name of the form field bound to the handle.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int Value { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Handle"/> class
        /// with the specified name and value.
        /// </summary>
        /// <param name="name">The name of the form field bound to the handle.</param>
        /// <param name="value">The value.</param>
        public Handle(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}
