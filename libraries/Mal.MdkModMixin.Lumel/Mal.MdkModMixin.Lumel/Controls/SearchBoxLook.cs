using Lumel.Images;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a search box in one state, in addition to what any stack has: the style of its text box, and the
    ///     style, symbol and size of its cross.
    /// </summary>
    public class SearchBoxLook : StackLook
    {
        IStyle<TextBox> _boxStyle;
        IStyle<Glyph> _clearStyle;
        string _clearSymbol;
        float? _clearSize;

        /// <summary>Gets or sets the style of the text box, or null to use the underlying looks.</summary>
        public IStyle<TextBox> BoxStyle
        {
            get { return _boxStyle; }
            set
            {
                _boxStyle = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the style of the cross, or null to use the underlying looks.</summary>
        public IStyle<Glyph> ClearStyle
        {
            get { return _clearStyle; }
            set
            {
                _clearStyle = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the symbol of the cross, or null to use the underlying looks.</summary>
        public string ClearSymbol
        {
            get { return _clearSymbol; }
            set
            {
                _clearSymbol = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the width and height of the cross, in UI units, or null to use the underlying looks.</summary>
        public float? ClearSize
        {
            get { return _clearSize; }
            set
            {
                _clearSize = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as SearchBoxLook;
            if (look == null)
                return;

            _boxStyle = look._boxStyle ?? _boxStyle;
            _clearStyle = look._clearStyle ?? _clearStyle;
            _clearSymbol = look._clearSymbol ?? _clearSymbol;
            _clearSize = look._clearSize ?? _clearSize;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _boxStyle = null;
            _clearStyle = null;
            _clearSymbol = null;
            _clearSize = null;
        }
    }
}
