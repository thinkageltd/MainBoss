using System.Web.Routing;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Describes an ASP.NET MVC action.
    /// </summary>
    public class ActionDescription
    {
        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the controller.
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Gets or sets the protocol for the URL.
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Gets or sets the host name for the URL.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the URL fragment name (the anchor name).
        /// </summary>
        public string Fragment { get; set; }

        /// <summary>
        /// Gets or sets the object that contains the parameters for the route.
        /// </summary>
        public object RouteValues { get; set; }

        /// <summary>
        /// Gets the dictionary that contains the parameters for the route.
        /// </summary>
        public RouteValueDictionary RouteValueDictionary
        {
            get
            {
                RouteValueDictionary dictionary = null;
                if (RouteValues != null)                
                {
                    dictionary = RouteValues as RouteValueDictionary;
                    if (dictionary == null)
                    {
                        dictionary = new RouteValueDictionary(RouteValues);
                    }
                }

                return dictionary;
            }
        }
    }
}
