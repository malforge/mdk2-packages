using VRageMath;
using Lumel.Styling;

namespace Lumel.Images
{
    /// <summary>
    ///     The look of a <see cref="Glyph" /> in one state, adding its font, size, tint and halo to what any element
    ///     sets.
    /// </summary>
    public class GlyphLook : ElementLook
    {
        string _font;
        float? _fontSize;
        float? _haloSize;
        Color? _haloTint;
        Color? _tint;

        /// <summary>
        ///     Gets or sets the font the symbol is drawn from, or null to inherit it from the looks beneath.
        /// </summary>
        public string Font
        {
            get { return _font; }
            set
            {
                _font = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the height of the font's line in UI units, or null to inherit it from the looks beneath.
        /// </summary>
        public float? FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color the symbol is drawn in, or null to inherit it from the looks beneath.
        /// </summary>
        public Color? Tint
        {
            get { return _tint; }
            set
            {
                _tint = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the height of the halo's line in UI units, or null to inherit it from the looks beneath.
        /// </summary>
        public float? HaloSize
        {
            get { return _haloSize; }
            set
            {
                _haloSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the halo, where transparent draws no halo, or null to inherit it from the
        ///     looks beneath.
        /// </summary>
        public Color? HaloTint
        {
            get { return _haloTint; }
            set
            {
                _haloTint = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as GlyphLook;
            if (look == null)
                return;

            _font = look._font ?? _font;
            _fontSize = look._fontSize ?? _fontSize;
            _tint = look._tint ?? _tint;
            _haloSize = look._haloSize ?? _haloSize;
            _haloTint = look._haloTint ?? _haloTint;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _font = null;
            _fontSize = null;
            _tint = null;
            _haloSize = null;
            _haloTint = null;
        }
    }
}
