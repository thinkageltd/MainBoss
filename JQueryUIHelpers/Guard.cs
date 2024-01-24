/* *****************************************
 * Based on many guard class implementations found on the internet.
 ***************************************** */

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Static methods for parameter checking.
    /// </summary>
    public static class Guard
    {
        private static object GetValue(MemberExpression memberExpression)
        {
            ConstantExpression constantExpression = (ConstantExpression)memberExpression.Expression;
            FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
            return fieldInfo.GetValue(constantExpression.Value);
        }

        /// <summary>
        /// Throws an ArgumentNullException if the specified argument is null.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="name">The name of the argument.</param>
        public static void ArgumentNotNull(object argument, string name)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throws an ArgumentNullException if the value of the specified expression is null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="expression">The expression.</param>
        public static void ArgumentNotNull<T>(Expression<Func<T>> expression)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;            
            object value = GetValue(memberExpression);
            if (value == null)
            {
                throw new ArgumentNullException(memberExpression.Member.Name);
            }
        }

        /// <summary>
        /// Throws an ArgumentNullException if the specified string argument is null or empty.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="name">The name of the argument.</param>
        public static void ArgumentNotNullOrEmpty(string argument, string name)
        {
            if (String.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throws an ArgumentNullException if the string value of the specified expression is null or empty.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public static void ArgumentNotNullOrEmpty(Expression<Func<string>> expression)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            string value = (string)GetValue(memberExpression);
            if (String.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(memberExpression.Member.Name);
            }
        }

        /// <summary>
        /// Throws an ArgumentNullException if the specified string argument is null, empty or contains only whitespaces.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="name">The name of the argument.</param>
        public static void ArgumentNotNullOrWhiteSpace(string argument, string name)
        {
            if (String.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throws an ArgumentNullException if the string value of the specified expression is null, empty or contains only whitespaces.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public static void ArgumentNotNullOrWhiteSpace(Expression<Func<string>> expression)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            string value = (string)GetValue(memberExpression);
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(memberExpression.Member.Name);
            }
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the specified int argument is not in the specfied range.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        /// <param name="name">The name of the argument.</param>
        public static void ArgumentInRange(int argument, int min, int max, string name)
        {
            if (argument < min || argument > max)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the specified uint argument is not in the specfied range.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        /// <param name="name">The name of the argument.</param>
        public static void ArgumentInRange(uint argument, uint min, uint max, string name)
        {
            if (argument < min || argument > max)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the int value of the specified expression is not in the specified range.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        public static void ArgumentInRange(Expression<Func<int>> expression, int min, int max)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            int value = (int)GetValue(memberExpression);
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(memberExpression.Member.Name);
            }
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the uint value of the specified expression is not in the specified range.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        public static void ArgumentInRange(Expression<Func<uint>> expression, uint min, uint max)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            uint value = (uint)GetValue(memberExpression);
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(memberExpression.Member.Name);
            }
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the double value of the specified expression is not in the specified range.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        public static void ArgumentInRange(Expression<Func<double>> expression, double min, double max)
        {
            MemberExpression memberExpression = (MemberExpression)expression.Body;
            double value = (double)GetValue(memberExpression);
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(memberExpression.Member.Name);
            }
        }
    }
}
