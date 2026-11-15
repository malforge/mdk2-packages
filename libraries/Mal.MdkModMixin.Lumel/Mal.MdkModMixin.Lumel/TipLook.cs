using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;

namespace Lumel
{
    /// <summary>The look of a tip in one state: its shadow, widest width, text and how long it waits before it opens.</summary>
    public class TipLook : ElementLook
    {
        IStyle<RichText> _bodyStyle;
        float? _maxWidth;
        Shadow _shadow;
        int? _showDelay;

        /// <summary>Gets or sets the shadow the tip casts on what lies beneath it, or null to use the underlying looks.</summary>
        public Shadow Shadow
        {
            get { return _shadow; }
            set
            {
                _shadow = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the widest the tip grows in UI units before its text wraps, or null to use the underlying
        ///     looks.
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

        /// <summary>Gets or sets the style of the tip's text, or null to use the underlying looks.</summary>
        public IStyle<RichText> BodyStyle
        {
            get { return _bodyStyle; }
            set
            {
                _bodyStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how long the pointer must rest on an element before its tip opens, in frames, or null to use
        ///     the underlying looks.
        /// </summary>
        public int? ShowDelay
        {
            get { return _showDelay; }
            set
            {
                _showDelay = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as TipLook;
            if (look == null)
                return;

            _shadow = look._shadow ?? _shadow;
            _maxWidth = look._maxWidth ?? _maxWidth;
            _bodyStyle = look._bodyStyle ?? _bodyStyle;
            _showDelay = look._showDelay ?? _showDelay;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _shadow = null;
            _maxWidth = null;
            _bodyStyle = null;
            _showDelay = null;
        }
    }
}
