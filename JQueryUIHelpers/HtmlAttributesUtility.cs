using System.Collections.Generic;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Contains methods to manage HTML attributes.
    /// </summary>
    public static class HtmlAttributesUtility
    {
        /// <summary>
        /// Converts the specified object into a IDictionary&lt;string, object&gt;. 
        /// If the object is a dictionary, returns the object.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The dictionary created from the object.</returns>
        public static IDictionary<string, object> ObjectToHtmlAttributesDictionary(object htmlAttributes)
        {
            IDictionary<string, object> htmlAttributesDictionary = null;
            if (htmlAttributes == null)
            {
                htmlAttributesDictionary = new Dictionary<string, object>();
            }
            else
            {
                htmlAttributesDictionary = htmlAttributes as IDictionary<string, object>;
                if (htmlAttributesDictionary == null)
                {
                    htmlAttributesDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
                }
            }

            return htmlAttributesDictionary;
        }
    }
}
