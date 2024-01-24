using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a cursor position.
    /// </summary>
    public class CursorPosition
    {
        private Dictionary<string, object> m_Properties = new Dictionary<string, object>();

        /// <summary>
        /// Sets the top offset value in pixels.
        /// </summary>
        public int Top
        {
            set
            {
                m_Properties["top"] = value;
            }
        }

        /// <summary>
        /// Sets the left offset value in pixels.
        /// </summary>
        public int Left
        {
            set
            {
                m_Properties["left"] = value;
            }
        }

        /// <summary>
        /// Sets the right offset value in pixels.
        /// </summary>
        public int Right
        {
            set
            {
                m_Properties["right"] = value;
            }
        }

        /// <summary>
        /// Sets the bottom offset value in pixels.
        /// </summary>
        public int Bottom
        {
            set
            {
                m_Properties["bottom"] = value;
            }
        }

        /// <summary>
        /// Returns the JSON string representation of this cursor position.
        /// </summary>
        /// <returns>The JSON string representation of this cursor position.</returns>
        public string ToJsonString()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(m_Properties);
        }
    }
}
