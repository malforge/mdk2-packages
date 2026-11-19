using Lumel.Styling;
using Lumel.Text;

namespace Lumel.Windows
{
    /// <summary>
    ///     The look of a coach mark in one state, adding the widest it grows and the style of its text to what any popup
    ///     has; the popup's gap is the length of its tail.
    /// </summary>
    public class CoachMarkLook : PopupLook
    {
        IStyle<RichText> _bodyStyle;
        float? _maxWidth;

        /// <summary>
        ///     Gets or sets the widest the coach mark grows in UI units before its text wraps, or null to use the
        ///     underlying looks.
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

        /// <summary>Gets or sets the style of the coach mark's text, or null to use the underlying looks.</summary>
        public IStyle<RichText> BodyStyle
        {
            get { return _bodyStyle; }
            set
            {
                _bodyStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as CoachMarkLook;
            if (look == null)
                return;

            _maxWidth = look._maxWidth ?? _maxWidth;
            _bodyStyle = look._bodyStyle ?? _bodyStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _maxWidth = null;
            _bodyStyle = null;
        }
    }
}
