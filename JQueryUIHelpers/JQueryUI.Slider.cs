using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Returns a slider initialized using values and HTML attributes.
        /// </summary>
        /// <param name="values">The initial values of the slider. At least one value is required.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The slider.</returns>
        public Slider Slider(int[] values, object htmlAttributes)
        {
            Guard.ArgumentNotNull(() => values);
            Handle[] handles = values.Select(v => new Handle(String.Empty, v)).ToArray();
            return CreateSlider(handles, htmlAttributes, false, null);
        }

        /// <summary>
        /// Returns a slider initialized using the specified name and value.
        /// </summary>
        /// <param name="name">The name of the hidden input field.</param>
        /// <param name="value">The initial value.</param>
        /// <returns>The slider.</returns>
        public Slider Slider(string name, int value)
        {
            return Slider(name, value, null);
        }

        /// <summary>
        /// Returns a slider initialized using the specified name, value and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the hidden input field.</param>
        /// <param name="value">The initial value.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The slider.</returns>
        public Slider Slider(string name, int value, object htmlAttributes)
        {
            return Slider(new Handle[] { new Handle(name, value) }, htmlAttributes);
        }

        /// <summary>
        /// Returns a slider initialized using the specified handles and HTML attributes.
        /// </summary>
        /// <param name="handles">An array of the handles. At least one handle is required.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <returns>The slider.</returns>
        public Slider Slider(Handle[] handles, object htmlAttributes)
        {
            Guard.ArgumentNotNull(() => handles);
            return CreateSlider(handles, htmlAttributes, true, null);
        }

        /// <summary>
        /// Returns a slider initialized using the specified the properties represented by the specified expressions.
        /// </summary>
        /// <param name="expressions">The expressions that identify the properties. At least one expression is required.</param>
        /// <returns>The slider.</returns>
        public Slider SliderFor(params Expression<Func<TModel, int>>[] expressions)
        {
            return SliderFor(null, expressions);
        }

        /// <summary>
        /// Returns a slider initialized using the specified HTML attributes and the properties represented by the specified expressions.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <param name="expressions">The expressions that identify the properties. At least one expression is required.</param>
        /// <returns>The slider.</returns>
        public Slider SliderFor(object htmlAttributes, params Expression<Func<TModel, int>>[] expressions)
        {
            Guard.ArgumentNotNull(() => expressions);
            List<Handle> handles = new List<Handle>();
            string label = String.Empty;
            for (int i = 0; i < expressions.Length; i++)
            {
                Expression<Func<TModel, int>> expression = expressions[i];
                ModelMetadata modelMetadata = ModelMetadata.FromLambdaExpression(expression, m_HtmlHelper.ViewData);
                string name = ExpressionHelper.GetExpressionText(expression);
                int value = modelMetadata.Model == null ? Int32.MinValue : (int)modelMetadata.Model;
                Handle handle = new Handle(name, value);
                handles.Add(handle);
                if (i == 0)
                {
                    label = modelMetadata.DisplayName;
                }
            }

            return CreateSlider(handles.ToArray(), htmlAttributes, true, label);
        }

        /// <summary>
        /// Returns a slider initialized using the specified handles, HTML attributes, hidden values and label.
        /// </summary>
        /// <param name="handles">An array of the handles.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        /// <param name="hiddenValues">If true, hidden input elements will be generated to store the values.</param>
        /// <param name="label">The text to display before the slider.</param>
        /// <returns>The slider.</returns>
        protected Slider CreateSlider(Handle[] handles, object htmlAttributes, bool hiddenValues, string label)
        {
            return new Slider(m_HtmlHelper, handles, htmlAttributes, hiddenValues, label);
        }
    }
}
