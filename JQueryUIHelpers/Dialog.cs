using System;
using System.Collections.Generic;
using System.Linq;

namespace JQueryUIHelpers
{
    /// <summary>
    /// Represents a dialog widget.
    /// </summary>
    public class Dialog : HtmlElement
    {
        private readonly List<DialogButton> m_Buttons;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dialog"/> class.
        /// </summary>
        public Dialog()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dialog"/> class
        /// with the specified HTML attributes.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes.</param>
        public Dialog(object htmlAttributes)
            : base("div", htmlAttributes)
        {
            m_Buttons = new List<DialogButton>();
            m_HtmlAttributes.Add(CommonHtmlAttributeName.JQueryUIType, JQueryUIType.Dialog);
        }

        /// <summary>
        /// Sets a jQuery selector that is used to determine which element the dialog should be appended to.
        /// </summary>
        /// <param name="selector">A jQuery selector.</param>
        /// <returns>This dialog.</returns>
        public Dialog AppendTo(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DialogAttributeName.AppendTo] = selector;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the dialog opens automatically when created.
        /// </summary>
        /// <param name="autoOpen">If true, the dialog opens automatically.</param>
        /// <returns>This dialog.</returns>
        public Dialog AutoOpen(bool autoOpen)
        {
            m_HtmlAttributes[DialogAttributeName.AutoOpen] = autoOpen;
            return this;
        }

        /// <summary>
        /// Adds the specified button to the dialog.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="functionName">The name of the JavaScript function that is invoked by the button.</param>
        /// <returns>This dialog.</returns>
        public Dialog Button(string text, string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => text);
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_Buttons.Add(new DialogButton(text, functionName));
            return this;
        }

        /// <summary>
        /// Adds the specified buttons to the dialog.
        /// </summary>
        /// <param name="buttons">The buttons.</param>
        /// <returns>This dialog.</returns>
        public Dialog Buttons(params DialogButton[] buttons)
        {
            Guard.ArgumentNotNull(() => buttons);
            m_Buttons.AddRange(buttons);
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the dialog should close when has focus and the user presses the ESC key.
        /// </summary>
        /// <param name="close">If true, the dialog closes when the user presses the ESC key.</param>
        /// <returns>This dialog.</returns>
        public Dialog CloseOnEscape(bool close)
        {
            m_HtmlAttributes[DialogAttributeName.CloseOnEscape] = close;
            return this;
        }

        /// <summary>
        /// Sets the text for the close button.
        /// Note that the close text is hidden when using a standard theme.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>This dialog.</returns>
        public Dialog CloseText(string text)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => text);
            m_HtmlAttributes[DialogAttributeName.CloseText] = text;
            return this;
        }

        /// <summary>
        /// Sets the CSS class name(s) to add to the dialog.
        /// </summary>
        /// <param name="cssClassName">The class name(s).</param>
        /// <returns>This dialog.</returns>
        public Dialog DialogClass(string cssClassName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => cssClassName);
            m_HtmlAttributes[DialogAttributeName.DialogClass] = cssClassName;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the dialog is draggable.
        /// </summary>
        /// <param name="draggable">If true, the dialog is draggable.</param>
        /// <returns>This dialog.</returns>
        public Dialog Draggable(bool draggable)
        {
            m_HtmlAttributes[DialogAttributeName.Draggable] = draggable;
            return this;
        }

        /// <summary>
        /// Sets the height of the dialog.
        /// </summary>
        /// <param name="height">The height in pixels.</param>
        /// <returns>This dialog.</returns>
        public Dialog Height(uint height)
        {
            m_HtmlAttributes[DialogAttributeName.Height] = height;
            return this;
        }

        /// <summary>
        /// Sets the effect to be used when the dialog is closed.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <returns>This dialog.</returns>
        public Dialog Hide(Effect effect)
        {
            if (effect != Effect.None)
            {
                m_HtmlAttributes[DialogAttributeName.Hide] = effect.ToString().StartLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the duration of the default hide animation.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This dialog.</returns>
        public Dialog Hide(uint duration)
        {
            m_HtmlAttributes[DialogAttributeName.Hide] = duration;
            return this;
        }

        /// <summary>
        /// Sets the effect, duration and easing of the hide animation.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This dialog.</returns>
        public Dialog Hide(Effect effect, uint duration, Easing easing)
        {
            if (effect != Effect.None)
            {
                m_HtmlAttributes[DialogAttributeName.Hide] = String.Format("{{\"effect\":\"{0}\",\"duration\":{1},\"easing\":\"{2}\"}}",
                    effect.ToString().StartLowerInvariant(), duration, easing.ToString().StartLowerInvariant());
            }

            return this;
        }

        /// <summary>
        /// Sets the maximum height of the dialog.
        /// </summary>
        /// <param name="maxHeight">The maximum height in pixels.</param>
        /// <returns>This dialog.</returns>
        public Dialog MaxHeight(uint maxHeight)
        {
            m_HtmlAttributes[DialogAttributeName.MaxHeight] = maxHeight;
            return this;
        }

        /// <summary>
        /// Sets the maximum width of the dialog.
        /// </summary>
        /// <param name="maxWidth">The maximum width in pixels.</param>
        /// <returns>This dialog.</returns>
        public Dialog MaxWidth(uint maxWidth)
        {
            m_HtmlAttributes[DialogAttributeName.MaxWidth] = maxWidth;
            return this;
        }

        /// <summary>
        /// Sets the minimum height of the dialog.
        /// </summary>
        /// <param name="minHeight">The minimum height in pixels.</param>
        /// <returns>This dialog.</returns>
        public Dialog MinHeight(uint minHeight)
        {
            m_HtmlAttributes[DialogAttributeName.MinHeight] = minHeight;
            return this;
        }

        /// <summary>
        /// Sets the minimum width of the dialog.
        /// </summary>
        /// <param name="minWidth">The minimum width in pixels.</param>
        /// <returns>This dialog.</returns>
        public Dialog MinWidth(uint minWidth)
        {
            m_HtmlAttributes[DialogAttributeName.MinWidth] = minWidth;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the dialog is modal.
        /// </summary>
        /// <param name="modal">If true, the dialog is modal.</param>
        /// <returns>This dialog.</returns>
        public Dialog Modal(bool modal)
        {
            m_HtmlAttributes[DialogAttributeName.Modal] = modal;
            return this;
        }

        /// <summary>
        /// Sets the position of the dialog.
        /// </summary>
        /// <param name="x">The x coordinate of the left, top corner.</param>
        /// <param name="y">The y coordinate of the left, top corner.</param>
        /// <returns>This dialog.</returns>
        public Dialog Position(uint x, uint y)
        {
            Position position = new Position()
            {
                MyHorizontal = HorizontalPosition.Left,
                MyVertical = VerticalPosition.Top,
                AtHorizontal = HorizontalPosition.Left,
                AtVertical = VerticalPosition.Top,
                OffsetLeft = (int?)x,
                OffsetTop = (int?)y
            };
            return Position(position);
        }

        /// <summary>
        /// Sets the position of the dialog.
        /// </summary>
        /// <param name="horizontalPosition">The horizontal position of the dialog.</param>
        /// <param name="verticalPosition">The vertical position of the dialog.</param>        
        /// <returns>This dialog.</returns>
        public Dialog Position(HorizontalPosition horizontalPosition, VerticalPosition verticalPosition)
        {
            Position position = new Position()
            {
                MyHorizontal = horizontalPosition,
                MyVertical = verticalPosition,
                AtHorizontal = horizontalPosition,
                AtVertical = verticalPosition
            };
            return Position(position);
        }

        /// <summary>
        /// Sets the position of the dialog.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>This dialog.</returns>
        public Dialog Position(Position position)
        {
            Guard.ArgumentNotNull(() => position);
            m_HtmlAttributes[DialogAttributeName.Position] = position.ToString();
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the dialog is resizable.
        /// </summary>
        /// <param name="resizable">If true, the dialog is resizable.</param>
        /// <returns>This dialog.</returns>
        public Dialog Resizable(bool resizable)
        {
            m_HtmlAttributes[DialogAttributeName.Resizable] = resizable;
            return this;
        }

        /// <summary>
        /// Sets the effect to be used when the dialog is opened.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <returns>This dialog.</returns>
        public Dialog Show(Effect effect)
        {
            if (effect != Effect.None)
            {
                m_HtmlAttributes[DialogAttributeName.Show] = effect.ToString().StartLowerInvariant();
            }

            return this;
        }

        /// <summary>
        /// Sets the duration of the default show animation.
        /// </summary>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <returns>This dialog.</returns>
        public Dialog Show(uint duration)
        {
            m_HtmlAttributes[DialogAttributeName.Show] = duration;
            return this;
        }

        /// <summary>
        /// Sets the effect, duration and easing of the show animation.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="duration">The duration in milliseconds.</param>
        /// <param name="easing">The easing.</param>
        /// <returns>This dialog.</returns>
        public Dialog Show(Effect effect, uint duration, Easing easing)
        {
            if (effect != Effect.None)
            {
                m_HtmlAttributes[DialogAttributeName.Show] = String.Format("{{\"effect\":\"{0}\",\"duration\":{1},\"easing\":\"{2}\"}}",
                    effect.ToString().StartLowerInvariant(), duration, easing.ToString().StartLowerInvariant());
            }

            return this;
        }

        /// <summary>
        /// Sets the title of the dialog.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <returns>This dialog.</returns>
        public Dialog Title(string title)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => title);
            m_HtmlAttributes[DialogAttributeName.Title] = title;
            return this;
        }

        /// <summary>
        /// Sets the width of the dialog.
        /// </summary>
        /// <param name="width">The width in pixels.</param>
        /// <returns>This dialog.</returns>
        public Dialog Width(uint width)
        {
            m_HtmlAttributes[DialogAttributeName.Width] = width;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector of the element that opens the dialog on click.
        /// </summary>
        /// <param name="selector">The jQuery selector that selects the trigger element(s).</param>
        /// <returns>This dialog.</returns>
        public Dialog TriggerClick(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DialogAttributeName.TriggerClick] = selector;
            return this;
        }

        /// <summary>
        /// Sets the jQuery selector of the element that opens the dialog on mouse enter.
        /// </summary>
        /// <param name="selector">The jQuery selector that selects the trigger element(s).</param>
        /// <returns>This dialog.</returns>
        public Dialog TriggerHover(string selector)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            m_HtmlAttributes[DialogAttributeName.TriggerHover] = selector;
            return this;
        }

        /// <summary>
        /// Sets up the dialog to ask for confirmation when the user clicks the specified link(s).
        /// </summary>
        /// <param name="selector">The jQuery selector that selects the link(s).</param>
        /// <param name="acceptText">The text of the accept button.</param>
        /// <param name="cancelText">The text of the cancel button.</param>
        /// <returns>This dialog.</returns>
        public Dialog Confirm(string selector, string acceptText, string cancelText)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            Guard.ArgumentNotNullOrWhiteSpace(() => acceptText);
            Guard.ArgumentNotNullOrWhiteSpace(() => cancelText);
            m_HtmlAttributes[DialogAttributeName.Confirm] = selector;
            m_HtmlAttributes[DialogAttributeName.ConfirmAccept] = acceptText;
            m_HtmlAttributes[DialogAttributeName.ConfirmCancel] = cancelText;
            return this;
        }

        /// <summary>
        /// Sets up the dialog to ask for confirmation when the user clicks the specified link(s).
        /// If the user confirms the action it will be executed using Ajax and the specified Ajax settings.
        /// </summary>
        /// <param name="selector">The jQuery selector that selects the link(s).</param>
        /// <param name="acceptText">The text of the accept button.</param>
        /// <param name="cancelText">The text of the cancel button.</param>
        /// <param name="ajaxSettings">The Ajax settings.</param>
        /// <returns>This dialog.</returns>
        public Dialog ConfirmAjax(string selector, string acceptText, string cancelText, AjaxSettings ajaxSettings)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => selector);
            Guard.ArgumentNotNullOrWhiteSpace(() => acceptText);
            Guard.ArgumentNotNullOrWhiteSpace(() => cancelText);
            Guard.ArgumentNotNullOrWhiteSpace(() => acceptText);
            Guard.ArgumentNotNull(() => ajaxSettings);
            m_HtmlAttributes[DialogAttributeName.ConfirmAjax] = selector;
            m_HtmlAttributes[DialogAttributeName.ConfirmAccept] = acceptText;
            m_HtmlAttributes[DialogAttributeName.ConfirmCancel] = cancelText;
            m_HtmlAttributes[DialogAttributeName.ConfirmAjaxSettings] = ajaxSettings.ToJsonString();
            return this;
        }

        #region Events

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog attempts to close.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnBeforeClose(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnBeforeClose] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog is closed.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnClose(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnClose] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog is created.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnCreate(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnCreate] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog is dragged.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnDrag(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnDrag] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the beginning of the dialog being dragged.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnDragStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnDragStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered after the dialog has been dragged.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnDragStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnDragStop] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog gains focus.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnFocus(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnFocus] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog is opened.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnOpen(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnOpen] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered when the dialog is resized.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnResize(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnResize] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered at the beginning of the dialog being resized.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnResizeStart(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnResizeStart] = functionName;
            return this;
        }

        /// <summary>
        /// Sets the name of the JavaScript function that is triggered after the dialog has been resized.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <returns>This dialog.</returns>
        public Dialog OnResizeStop(string functionName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(() => functionName);
            m_HtmlAttributes[DialogAttributeName.OnResizeStop] = functionName;
            return this;
        }

        #endregion

        /// <summary>
        /// Gets the start tag of the dialog.
        /// </summary>        
        public override string StartTag
        {
            get
            {
                AddButtons();
                return base.StartTag;
            }
        }

        /// <summary>
        /// Adds the defined buttons to the HTML attributes.
        /// </summary>
        private void AddButtons()
        {
            if (m_Buttons.Count > 0)
            {
                m_HtmlAttributes[DialogAttributeName.Buttons] = 
                    String.Join("|", 
                    m_Buttons.Select(b => b.ToString()));                
            }
        }
    }
}
