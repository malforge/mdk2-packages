using System.Text;
using Lumel.Drawing;
using VRage.Game;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Images
{
    /// <summary>One character of a font, drawn as a symbol and centered in the space it is given.</summary>
    public class Glyph : Element, IPadded
    {
        readonly StringBuilder _measuring = new StringBuilder(2);
        readonly StringBuilder _haloMeasuring = new StringBuilder(2);

        string _font = MyFontEnum.White;
        float _fontSize = 24f;
        Thickness _padding;
        Vector2 _size;

        float _sizedAt = -1f;

        string _sizedFor;

        string _symbol;

        string _halo;
        float _haloSize;
        Vector2 _haloSized;
        string _haloSizedFor;
        float _haloSizedAt = -1f;

        /// <summary>
        ///     Creates a glyph with no symbol, using the look its host's theme holds for a glyph until it is given a
        ///     style.
        /// </summary>
        public Glyph()
        {
            Tint = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this glyph, or null to use the default its host's theme holds for a glyph.
        /// </summary>
        public new IStyle<Glyph> Style
        {
            get { return (IStyle<Glyph>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>The look this glyph sets itself for every state, applied over its style's.</summary>
        public new GlyphLook Base => (GlyphLook)base.Base;

        /// <summary>
        ///     The look this glyph sets itself for when the pointer is over it, applied over its style's.
        /// </summary>
        public new GlyphLook PointerOver => (GlyphLook)base.PointerOver;

        /// <summary>The look this glyph sets itself for when it is disabled, applied over its style's.</summary>
        public new GlyphLook Disabled => (GlyphLook)base.Disabled;

        /// <summary>
        ///     The look this glyph sets itself for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style's.
        /// </summary>
        public new GlyphLook this[State state] => (GlyphLook)base[state];

        /// <summary>Gets or sets the character to draw.</summary>
        public string Symbol
        {
            get { return _symbol; }
            set
            {
                if (_symbol == value)
                    return;

                _symbol = value;
                _measuring.Length = 0;
                InvalidateMeasure();

                if (value != null)
                    _measuring.Append(value);
            }
        }

        /// <summary>The font the symbol is drawn from.</summary>
        public string Font
        {
            get { return _font; }
            private set
            {
                if (_font == value)
                    return;

                _font = value;
                _sizedAt = -1f;
                InvalidateMeasure();
            }
        }

        /// <summary>The height of the font's line, in UI units.</summary>
        public float FontSize
        {
            get { return _fontSize; }
            private set
            {
                if (_fontSize == value)
                    return;

                _fontSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The space kept clear between the edge and the symbol, which is centered within it.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>The color the symbol is drawn in; transparent draws nothing but keeps the space.</summary>
        /// <remarks>Set by this glyph's looks.</remarks>
        public Color Tint { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Glyph>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new GlyphLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var glyph = (GlyphLook)look;
            Font = glyph.Font ?? MyFontEnum.White;
            FontSize = glyph.FontSize ?? TextLook.NeutralFontSize;
            Tint = glyph.Tint ?? Color.White;
            HaloSize = glyph.HaloSize ?? 0f;
            HaloTint = glyph.HaloTint ?? Color.Transparent;
        }

        /// <summary>
        ///     Gets or sets a character of the same font drawn around the symbol, centered on it and taking no space of
        ///     its own, or null for none.
        /// </summary>
        /// <remarks>Changing it measures again, so it is given once and shown or hidden by <see cref="HaloTint" />, which its looks set.</remarks>
        public string Halo
        {
            get { return _halo; }
            set
            {
                if (_halo == value)
                    return;

                _halo = value;
                _haloMeasuring.Length = 0;
                if (value != null)
                    _haloMeasuring.Append(value);

                InvalidateMeasure();
            }
        }

        /// <summary>The height of the halo's line, in UI units.</summary>
        /// <remarks>Set by this glyph's looks.</remarks>
        public float HaloSize
        {
            get { return _haloSize; }
            private set
            {
                if (_haloSize == value)
                    return;

                _haloSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The color the halo is drawn in; transparent draws no halo.</summary>
        /// <remarks>Set by this glyph's looks.</remarks>
        public Color HaloTint { get; private set; }

        /// <summary>Returns how much space the symbol wants, measured once per symbol, font and size.</summary>
        /// <remarks>Held, because measuring text is the dearest call the layout makes.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_haloSizedFor != _halo || _haloSizedAt != _haloSize)
            {
                _haloSizedFor = _halo;
                _haloSizedAt = _haloSize;
                _haloSized = _haloMeasuring.Length == 0 ? Vector2.Zero : g.MeasureText(_font, _haloMeasuring, _haloSize);
            }

            if (_sizedFor == _symbol && _sizedAt == _fontSize)
                return _size;

            _sizedFor = _symbol;
            _sizedAt = _fontSize;

            _size = _measuring.Length == 0
                ? Vector2.Zero
                : g.MeasureText(_font, _measuring, _fontSize);

            return _size;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            if (HaloTint.A != 0 && !string.IsNullOrEmpty(_halo))
            {
                g.DrawText(_font, _halo, new Vector2(
                    room.X + (room.Width - _haloSized.X) * 0.5f,
                    room.Y + (room.Height - _haloSized.Y) * 0.5f), _haloSize, HaloTint);
            }

            if (Tint.A == 0 || string.IsNullOrEmpty(_symbol))
                return;

            g.DrawText(_font, _symbol, new Vector2(
                room.X + (room.Width - _size.X) * 0.5f,
                room.Y + (room.Height - _size.Y) * 0.5f), _fontSize, Tint);
        }
    }
}
