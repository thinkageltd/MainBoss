using System;
using System.Text;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a button on a dialog.
    /// </summary>
    public class DialogButton
    {

        private string m_Text;
        private string m_FunctionName;
        private string m_PrimaryIconCssClassName;
        private string m_SecondaryIconCssClassName;
        private bool m_ShowText;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogButton"/> class.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="functionName">The name of the JavaScript function that is invoked by the button.</param>
        public DialogButton(string text, string functionName)
            : this(text, functionName, String.Empty, String.Empty, true)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogButton"/> class.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="functionName">The name of the JavaScript function that is invoked by the button.</param>
        /// <param name="primaryIconCssClassName">The CSS class name of the primary icon.</param>
        /// <param name="secondaryIconCssClassName">The CSS class name of the secondary icon.</param>
        /// <param name="showText">If false, only the icons are visible on the button.</param>
        public DialogButton(string text, string functionName, string primaryIconCssClassName, string secondaryIconCssClassName, bool showText)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => text);
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_Text = text;
            m_FunctionName = functionName;
            m_PrimaryIconCssClassName = primaryIconCssClassName;
            m_SecondaryIconCssClassName = secondaryIconCssClassName;
            m_ShowText = showText;
        }

        /// <summary>
        /// Returns a string that represents the current dialog button.
        /// </summary>
        /// <returns>The string representation of the dialog button.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{{\"text\":\"{0}\",\"click\":\"{1}\"", m_Text, m_FunctionName);
            bool hasPrimary = !String.IsNullOrWhiteSpace(m_PrimaryIconCssClassName);
            bool hasSecondary = !String.IsNullOrWhiteSpace(m_SecondaryIconCssClassName);
            if (hasPrimary || hasSecondary)
            {
                builder.Append(",\"icons\":{");
                if (hasPrimary)
                {
                    builder.AppendFormat("\"primary\":\"{0}\"", m_PrimaryIconCssClassName);
                }
                if (hasSecondary)
                {
                    builder.AppendFormat("{0}\"secondary\":\"{1}\"", hasPrimary ? "," : String.Empty, m_SecondaryIconCssClassName);
                }
                builder.Append("}");
                builder.AppendFormat(",\"showText\":{0}", m_ShowText.ToString().ToLowerInvariant());
            }
            builder.Append("}");
            return builder.ToString();
        }
    }
}
