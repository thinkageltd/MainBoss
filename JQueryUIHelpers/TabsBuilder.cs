using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a builder that builds a tabs widget.
    /// </summary>
    public class TabsBuilder : BuilderBase<Tabs>
    {
        private Queue<string> m_TabIds;
        private bool m_HeaderClosed;
        private bool m_WritingContent;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabsBuilder"/> class.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="tabs">The tabs.</param>
        public TabsBuilder(HtmlHelper htmlHelper, Tabs tabs)
            : base(htmlHelper, tabs)
        {
            m_TabIds = new Queue<string>();
            m_HeaderClosed = false;
            m_WritingContent = false;
            m_Writer.Write("<ul>");
        }

        /// <summary>
        /// Creates a new tab with the specified label for the specified content.
        /// </summary>
        /// <param name="label">The label of the tab.</param>
        /// <param name="id">The id of the content panel.</param>
        public void Tab(string label, string id)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => label);
            Guard.ArgumentNotNullOrWhiteSpace(() => id);
            CheckBuilderState();
            string tabId = HtmlHelper.GenerateIdFromName(id);
            m_TabIds.Enqueue(tabId);
            WriteTab(label, "#" + tabId);
        }

        /// <summary>
        /// Creates a new tab with the specified label for the specified remote content.
        /// </summary>
        /// <param name="label">The label of the tab.</param>
        /// <param name="url">The url of the content.</param>
        public void AjaxTab(string label, string url)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => label);
            Guard.ArgumentNotNullOrWhiteSpace(() => url);
            CheckBuilderState();
            WriteTab(label, url);
        }

        /// <summary>
        /// Writes the start tag of a new tabs content panel to the response.
        /// </summary>
        /// <returns>The new panel.</returns>
        public TabsPanel BeginPanel()
        {
            m_WritingContent = true;
            CloseHeader();
            if (m_TabIds.Count == 0)
            {
                throw new InvalidOperationException(StringResource.UndefinedTab);
            }

            string id = m_TabIds.Dequeue();
            return new TabsPanel(m_Writer, m_Element.InternalPanelTag, id);
        }

        /// <summary>
        /// Writes the end tag of the tabs to the response.
        /// </summary>
        /// <param name="isDisposing">Indicates whether Dispose is called by Dispose or the Destructor.</param>
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                CloseHeader();
            }

            base.Dispose(isDisposing);
        }

        private void CloseHeader()
        {
            if (!m_HeaderClosed)
            {
                m_Writer.Write("</ul>");
                m_HeaderClosed = true;
            }
        }

        private void WriteTab(string label, string href)
        {
            m_Writer.Write(m_Element.InternalTabTemplate.Replace("#{label}", label).Replace("#{href}", href));
        }

        private void CheckBuilderState()
        {
            if (m_WritingContent)
            {
                throw new InvalidOperationException(StringResource.TabMixedWithContent);
            }
        }
    }
}
