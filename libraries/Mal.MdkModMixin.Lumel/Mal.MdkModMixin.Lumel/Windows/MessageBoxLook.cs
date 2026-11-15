using Lumel.Controls;
using Lumel.Styling;
using Lumel.Text;

namespace Lumel.Windows
{
    /// <summary>
    ///     The look of a message box in one state, adding the looks of its message and buttons and the space between
    ///     them to what any window has.
    /// </summary>
    public class MessageBoxLook : WindowLook
    {
        float? _buttonSpacing;
        float? _maxWidth;
        IStyle<Button> _buttonStyle;
        IStyle<Button> _defaultButtonStyle;
        float? _messageSpacing;
        IStyle<RichText> _messageStyle;

        /// <summary>
        ///     The maximum width of the box in UI units, beyond which the message wraps, or null to inherit it from the
        ///     looks beneath.
        /// </summary>
        public float? MaxWidth
        {
            get { return _maxWidth; }
            set
            {
                _maxWidth = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The space between the message and the buttons, or null to inherit it from the looks beneath.
        /// </summary>
        public float? MessageSpacing
        {
            get { return _messageSpacing; }
            set
            {
                _messageSpacing = value;
                OnChanged();
            }
        }

        /// <summary>The space between one button and the next, or null to inherit it from the looks beneath.</summary>
        public float? ButtonSpacing
        {
            get { return _buttonSpacing; }
            set
            {
                _buttonSpacing = value;
                OnChanged();
            }
        }

        /// <summary>The style of the message, or null to inherit it from the looks beneath.</summary>
        public IStyle<RichText> MessageStyle
        {
            get { return _messageStyle; }
            set
            {
                _messageStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The style of the answer buttons other than the default one, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public IStyle<Button> ButtonStyle
        {
            get { return _buttonStyle; }
            set
            {
                _buttonStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The style of the default answer button, the one Enter gives, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public IStyle<Button> DefaultButtonStyle
        {
            get { return _defaultButtonStyle; }
            set
            {
                _defaultButtonStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as MessageBoxLook;
            if (look == null)
                return;

            _messageSpacing = look._messageSpacing ?? _messageSpacing;
            _buttonSpacing = look._buttonSpacing ?? _buttonSpacing;
            _maxWidth = look._maxWidth ?? _maxWidth;
            _messageStyle = look._messageStyle ?? _messageStyle;
            _buttonStyle = look._buttonStyle ?? _buttonStyle;
            _defaultButtonStyle = look._defaultButtonStyle ?? _defaultButtonStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _messageSpacing = null;
            _buttonSpacing = null;
            _maxWidth = null;
            _messageStyle = null;
            _buttonStyle = null;
            _defaultButtonStyle = null;
        }
    }
}
