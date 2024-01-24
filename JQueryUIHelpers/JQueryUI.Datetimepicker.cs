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
        /// Returns a datetimepicker with the specified form field name.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <returns>The datetimepicker.</returns>
        public Datetimepicker Datetimepicker(string name)
        {
            return Datetimepicker(name, null, null);
        }

        /// <summary>
        /// Returns a datetimepicker with the specified form field name and value.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The datetimepicker.</returns>
        public Datetimepicker Datetimepicker(string name, DateTime? value)
        {
            return Datetimepicker(name, value, null);
        }

        /// <summary>
        /// Returns a datetimepicker with the specified form field name, value and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The datetimepicker.</returns>
        public Datetimepicker Datetimepicker(string name, DateTime? value, object htmlAttributes)
        {
            return CreateDatetimepicker(name, value, htmlAttributes, null, null);
        }

        /// <summary>
        /// Returns a datetimepicker initialized using the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <returns>The datetimepicker.</returns>
        public Datetimepicker DatetimepickerFor<TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            return DatetimepickerFor(expression, null);
        }

        /// <summary>
        /// Returns a datetimepicker initialized using the specified HTML attributes and the property represented by the specified expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">The expression that identifies the property.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The datetimepicker.</returns>
        public Datetimepicker DatetimepickerFor<TProperty>(Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
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
			// TODO: the EditFormatString is not taking the time format into account here
            return CreateDatetimepicker(name, modelMetadata.Model as DateTime?, htmlAttributes, formatString, null);
        }

        /// <summary>
        /// Returns a datetimepicker with the specified form field name, value, HTML attributes and format string.
        /// </summary>
        /// <param name="name">The name of the form field.</param>
        /// <param name="value">The value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
		/// <param name="dateFormatString">The date format string.</param>
		/// <param name="timeFormatString">The time format string.</param>
		/// <returns>The datetimepicker.</returns>
        protected Datetimepicker CreateDatetimepicker(string name, DateTime? value, object htmlAttributes, string dateFormatString, string timeFormatString)
        {
			return new Datetimepicker(m_HtmlHelper, name, value, htmlAttributes, dateFormatString, timeFormatString);
        }
    }
}
