using System;
using System.IO;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents the base class of the builders.
    /// </summary>
    /// <typeparam name="T">The type of the element this builder builds.</typeparam>
    public abstract class BuilderBase<T> : IDisposable
        where T : HtmlElement
    {        
        /// <summary>
        /// The element built by this builder.
        /// </summary>
        protected readonly T m_Element;

        /// <summary>
        /// The output writer.
        /// </summary>
        protected readonly TextWriter m_Writer;

        private bool m_Disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuilderBase{T}"/> class with the specified HtmlHelper and HTML element.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="element">The element this builder builds.</param>
        public BuilderBase(HtmlHelper htmlHelper, T element)
        {
            Guard.ArgumentNotNull(() => element);
            m_Element = element;
            m_Writer = htmlHelper.ViewContext.Writer;
            m_Writer.Write(m_Element.StartTag);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BuilderBase{T}"/> class. 
        /// </summary>
        ~BuilderBase()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Dispose of this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Writes the end tag of the element to the response.
        /// </summary>
        /// <param name="isDisposing">Indicates whether Dispose is called by Dispose or the Destructor.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!m_Disposed)
            {
                if (isDisposing)
                {
                    m_Writer.Write(m_Element.EndTag);
                }
                m_Disposed = true;
            }
        }
    }
}
