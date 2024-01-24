using System;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Returns an autocomplete with the specified form field name and data source.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete Autocomplete(string name, string source)
        {
            return Autocomplete(name, source, null, null, null, null);
        }

        /// <summary>
        /// Returns an autocomplete with the specified form field name, data source and value.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="value">The value.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete Autocomplete(string name, string source, object value)
        {
            return Autocomplete(name, source, value, null, null, null);
        }

        /// <summary>
        /// Returns an autocomplete with the specified form field name, data source, value and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete Autocomplete(string name, string source, object value, object htmlAttributes)
        {
            return Autocomplete(name, source, value, null, null, htmlAttributes);
        }

        /// <summary>
        /// Returns an autocomplete with the specified form field name, data source, value and HTML attributes.
        /// The returned autocomplete will create two input elements: a hidden element for the name and value, 
        /// and a text element for the visible autocomplete.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="value">The value.</param>
        /// <param name="autocompleteId">The id of the autocomplete input element.</param>
        /// <param name="autocompleteText">The initial text of the autocomplete input element.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete Autocomplete(string name, string source, object value, string autocompleteId, 
            string autocompleteText, object htmlAttributes)
        {
            return CreateAutocomplete(name, source, value, autocompleteId, autocompleteText, htmlAttributes);
        }

        /// <summary>
        /// Returns an autocomplete initialized using the specified data source and the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete AutocompleteFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string source)
        {
            return AutocompleteFor(expression, source, null, null, null);
        }

        /// <summary>
        /// Returns an autocomplete initialized using the specified data source and the property represented by the specified expression.
        /// The returned autocomplete will create two input elements: a hidden element for the property, 
        /// and a text element for the visible autocomplete.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="autocompleteId">The id of the autocomplete input element.</param>
        /// <param name="autocompleteText">The initial text of the autocomplete input element.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete AutocompleteFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string source,
            string autocompleteId, string autocompleteText)
        {
            return AutocompleteFor(expression, source, autocompleteId, autocompleteText, null);
        }

        /// <summary>
        /// Returns an autocomplete initialized using the specified data source, HTML attributes and the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete AutocompleteFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string source, object htmlAttributes)
        {
            return AutocompleteFor(expression, source, null, null, htmlAttributes);
        }

        /// <summary>
        /// Returns an autocomplete initialized using the specified data source, HTML attributes and the property represented by the specified expression.
        /// The returned autocomplete will create two input elements: a hidden element for the property, 
        /// and a text element for the visible autocomplete.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="autocompleteId">The id of the autocomplete input element.</param>
        /// <param name="autocompleteText">The initial text of the autocomplete input element.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The autocomplete.</returns>
        public Autocomplete AutocompleteFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string source,
            string autocompleteId, string autocompleteText, object htmlAttributes)
        {
            ModelMetadata modelMetadata = ModelMetadata.FromLambdaExpression(expression, m_HtmlHelper.ViewData);
            string name = ExpressionHelper.GetExpressionText(expression);
            return CreateAutocomplete(name, source, modelMetadata.Model, autocompleteId, autocompleteText, htmlAttributes);
        }

        /// <summary>
        /// Returns an autocomplete initialized using the specified name, data source, value, autocompleteId,
        /// autocompleteText and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="source">The data source. Can be a URL (must contain '/'), the name of a JavaScript function or the name of an array variable.</param>
        /// <param name="value">The value.</param>
        /// <param name="autocompleteId">The id of the autocomplete input element.</param>
        /// <param name="autocompleteText">The initial text of the autocomplete input element.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The autocomplete.</returns>
        protected Autocomplete CreateAutocomplete(string name, string source, object value, string autocompleteId, string autocompleteText, object htmlAttributes)
        {
            return new Autocomplete(m_HtmlHelper, name, source, value, autocompleteId, autocompleteText, htmlAttributes);
        }
    }
}
