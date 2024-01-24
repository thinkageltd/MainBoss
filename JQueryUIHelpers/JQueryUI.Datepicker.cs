using System;
using System.Globalization;
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
        /// Returns a DatepickerCulture initialized to the current UI culture.
        /// </summary>
        /// <returns>The DatepickerCulture.</returns>
        public DatepickerCulture InitializeDatepickerCulture()
        {
            return InitializeDatepickerCulture(CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Returns a DatepickerCulture initialized to the specified culture.
        /// </summary>
        /// <param name="culture">The culture.</param>
        /// <returns>The DatepickerCulture.</returns>
        public DatepickerCulture InitializeDatepickerCulture(CultureInfo culture)
        {
            return new DatepickerCulture(m_HtmlHelper, culture);
        }

        /// <summary>
        /// Returns a datepicker with the specified form field name.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <returns>The datepicker.</returns>
        public Datepicker Datepicker(string name)
        {
            return Datepicker(name, null, null);
        }

        /// <summary>
        /// Returns a datepicker with the specified form field name and value.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The datepicker.</returns>
        public Datepicker Datepicker(string name, DateTime? value)
        {
            return Datepicker(name, value, null);
        }

        /// <summary>
        /// Returns a datepicker with the specified form field name, value and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The datepicker.</returns>
        public Datepicker Datepicker(string name, DateTime? value, object htmlAttributes)
        {
            return CreateDatepicker(name, value, htmlAttributes, null);
        }

        /// <summary>
        /// Returns a datepicker initialized using the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <returns>The datepicker.</returns>
        public Datepicker DatepickerFor<TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            return DatepickerFor(expression, null);
        }

        /// <summary>
        /// Returns a datepicker initialized using the specified HTML attributes and the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The datepicker.</returns>
        public Datepicker DatepickerFor<TProperty>(Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            ModelMetadata modelMetadata = ModelMetadata.FromLambdaExpression(expression, m_HtmlHelper.ViewData);
            if (modelMetadata.ModelType != typeof(DateTime?) && modelMetadata.ModelType != typeof(DateTime))
            {
                throw new ArgumentException(StringResource.DatepickerPropertyNotDateTime, "expression");
            }

            string name = ExpressionHelper.GetExpressionText(expression);
            string formatString = null;
            if (!String.IsNullOrWhiteSpace(modelMetadata.EditFormatString))
            {
                formatString = DateFormatUtility.NormalizeDateFormatString(modelMetadata.EditFormatString);
            }

            return CreateDatepicker(name, modelMetadata.Model as DateTime?, htmlAttributes, formatString);
        }

        /// <summary>
        /// Returns a datepicker with the specified form field name, value, HTML attributes and format string.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <param name="formatString">The date format string.</param>
        /// <returns>The datepicker.</returns>
        protected Datepicker CreateDatepicker(string name, DateTime? value, object htmlAttributes, string formatString)
        {
            return new Datepicker(m_HtmlHelper, name, value, htmlAttributes, formatString);
        }
    }
}
