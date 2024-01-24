namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Returns a button with the specified text.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <returns>The button.</returns>
        public Button Button(string text)
        {
            return Button(text, null);
        }

        /// <summary>
        /// Returns a button with the specified text and HTML attributes.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The button.</returns>
        public Button Button(string text, object htmlAttributes)
        {
            return Button(text, ButtonElement.Input, ButtonType.Submit, htmlAttributes);
        }

        /// <summary>
        /// Returns a button with the specified text, element, type and HTML attributes.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="element">The button element (&lt;input&gt; or &lt;button&gt;).</param>
        /// <param name="type">The type of the button.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The button.</returns>
        public Button Button(string text, ButtonElement element, ButtonType type, object htmlAttributes)
        {
            return CreateButton(text, element, type, htmlAttributes);
        }

        /// <summary>
        /// Writes the opening tag of the buttonset to the response.
        /// </summary>
        /// <returns>The buttonset.</returns>
        public ButtonSet BeginButtonSet()
        {
            return BeginButtonSet(null);
        }

        /// <summary>
        /// Writes the opening tag of the buttonset to the response using the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The buttonset.</returns>
        public ButtonSet BeginButtonSet(object htmlAttributes)
        {
            return new ButtonSet(m_HtmlHelper, htmlAttributes);
        }

        /// <summary>
        /// Returns an action button with the specified text and and the virtual path of the specified action.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>The action button.</returns>
        public ActionButton ActionButton(string text, string actionName)
        {
            return ActionButton(text, actionName, null);
        }

        /// <summary>
        /// Returns an action button with the specified text and and the virtual path of the specified action.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">An object that contains the parameters for the route.</param>
        /// <returns>The action button.</returns>
        public ActionButton ActionButton(string text, string actionName, object routeValues)
        {
            return ActionButton(text, actionName, routeValues, null);
        }

        /// <summary>
        /// Returns an action button with the specified text and and the virtual path of the specified action.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">An object that contains the parameters for the route.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The action button.</returns>
        public ActionButton ActionButton(string text, string actionName, object routeValues, object htmlAttributes)
        {
            return ActionButton(text, actionName, null, null, null, null, routeValues, htmlAttributes);
        }

        /// <summary>
        /// Returns an action button with the specified text and and the virtual path of the specified action.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>The action button.</returns>
        public ActionButton ActionButton(string text, string actionName, string controllerName)
        {
            return ActionButton(text, actionName, controllerName, null, null);
        }

        /// <summary>
        /// Returns an action button with the specified text and and the virtual path of the specified action.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">An object that contains the parameters for the route.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The action button.</returns>
        public ActionButton ActionButton(string text, string actionName, string controllerName, object routeValues, object htmlAttributes)
        {
            return ActionButton(text, actionName, controllerName, null, null, null, routeValues, htmlAttributes);
        }

        /// <summary>
        /// Returns an action button with the specified text and and the virtual path of the specified action.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="protocol">The protocol for the URL.</param>
        /// <param name="hostName">The host name for the URL.</param>
        /// <param name="fragment">The URL fragment name (the anchor name).</param>
        /// <param name="routeValues">An object that contains the parameters for the route.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The action button.</returns>
        public ActionButton ActionButton(string text, string actionName, string controllerName, string protocol, 
            string hostName, string fragment, object routeValues, object htmlAttributes)
        {
            ActionDescription actionDescription = new ActionDescription()
            {
                ActionName = actionName,
                ControllerName = controllerName,
                Protocol = protocol,
                HostName = hostName,
                Fragment = fragment,
                RouteValues = routeValues
            };
            return CreateActionButton(text, actionDescription, htmlAttributes);
        }

        /// <summary>
        /// Returns an action button initialized using the specified text, action description and HTML attributes.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="actionDescription">The action description.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The action button.</returns>
        protected ActionButton CreateActionButton(string text, ActionDescription actionDescription, object htmlAttributes)
        {
            return new ActionButton(m_HtmlHelper, text, actionDescription, htmlAttributes);
        }

        /// <summary>
        /// Returns a button initialized using the specified text, HTML attributes, element and type.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="element">The button element (&lt;input&gt; or &lt;button&gt;).</param>
        /// <param name="type">The type of the button.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The button.</returns>
        protected Button CreateButton(string text, ButtonElement element, ButtonType type, object htmlAttributes)
        {
            return new Button(m_HtmlHelper, text, htmlAttributes, element, type);
        }
    }
}
