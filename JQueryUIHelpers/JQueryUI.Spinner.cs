using System;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <content>
    /// Implements the factory methods which create the widgets.
    /// </content>
    public partial class JQueryUI<TModel>
    {
        /// <summary>
        /// Returns a spinner with the specified form field name.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <returns>The spinner.</returns>
        public Spinner Spinner(string name)
        {
            return Spinner(name, null, null);
        }

        /// <summary>
        /// Returns a spinner with the specified form field name and value.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The spinner.</returns>
        public Spinner Spinner(string name, int? value)
        {
            return Spinner(name, value, null);
        }

        /// <summary>
        /// Returns a spinner with the specified form field name, value and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The spinner.</returns>
        public Spinner Spinner(string name, int? value, object htmlAttributes)
        {
            return CreateSpinner(name, value, htmlAttributes);
        }

        /// <summary>
        /// Returns a spinner with the specified form field name and value.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The spinner.</returns>
        public Spinner Spinner(string name, double? value)
        {
            return Spinner(name, value, null);
        }

        /// <summary>
        /// Returns a spinner with the specified form field name, value and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The spinner.</returns>
        public Spinner Spinner(string name, double? value, object htmlAttributes)
        {
            return CreateSpinner(name, value, htmlAttributes);
        }

        /// <summary>
        /// Returns a spinner initialized using the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <returns>The spinner.</returns>
        public Spinner SpinnerFor<TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            return SpinnerFor(expression, null);
        }

        /// <summary>
        /// Returns a spinner initialized using the specified HTML attributes and the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The spinner.</returns>
        public Spinner SpinnerFor<TProperty>(Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            ModelMetadata modelMetadata = ModelMetadata.FromLambdaExpression(expression, m_HtmlHelper.ViewData);
            string name = ExpressionHelper.GetExpressionText(expression);
            return CreateSpinner(name, modelMetadata.Model, htmlAttributes);
        }

        /// <summary>
        /// Returns a spinner with the specified form field name, value, and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The spinner.</returns>
        protected Spinner CreateSpinner(string name, object value, object htmlAttributes)
        {
            return new Spinner(m_HtmlHelper, name, value, htmlAttributes);
        }   
    }
}
