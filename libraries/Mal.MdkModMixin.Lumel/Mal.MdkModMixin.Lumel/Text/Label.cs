using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using VRage.Game;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>A text element that shows its text on one line or wraps it onto as many lines as needed.</summary>
    public class Label : Element, IPadded, ITextual
    {
        const string Ellipsis = "…";

        Thickness _padding;

        readonly List<string> _lines = new List<string>();
        readonly List<float> _lineWidths = new List<float>();

        readonly List<int> _breaks = new List<int>();

        readonly StringBuilder _measured = new StringBuilder(64);
        readonly StringBuilder _scratch = new StringBuilder(64);

        bool _dirty = true;

        float _flat;
        float _flatAt = -1f;
        bool _flatDirty = true;
        string _flatIn;
        string _font;
        float _laidOutAt = -1f;

        float _laidOutFor = -1f;
        string _laidOutIn;

        float _fontSize;

        string _text = string.Empty;
        TextWrapping _textWrapping;
        Vector2 _wrapped;

        /// <summary>
        ///     Creates an empty label that uses its host theme's default label style until it is given a style.
        /// </summary>
        public Label()
        {
            _font = MyFontEnum.White;
            _fontSize = TextLook.NeutralFontSize;
            Color = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this label, or null to use the default label style of its host theme.
        /// </summary>
        public new IStyle<Label> Style
        {
            get { return (IStyle<Label>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this label's own look for every state, applied over its style.</summary>
        public new LabelLook Base => (LabelLook)base.Base;

        /// <summary>Gets this label's own look on hover, applied over its style.</summary>
        public new LabelLook PointerOver => (LabelLook)base.PointerOver;

        /// <summary>Gets this label's own look when disabled, applied over its style.</summary>
        public new LabelLook Disabled => (LabelLook)base.Disabled;

        /// <summary>
        ///     Gets this label's own look for <paramref name="state" />, or for every state that includes it, applied
        ///     over its style.
        /// </summary>
        public new LabelLook this[State state] => (LabelLook)base[state];

        /// <summary>Gets or sets the text shown.</summary>
        public string Text
        {
            get { return _text; }
            set
            {
                var incoming = value ?? string.Empty;
                if (_text == incoming)
                    return;

                _text = incoming;
                _measured.Clear();
                _measured.Append(incoming);
                _dirty = true;
                _flatDirty = true;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the font the text is drawn in.</summary>
        /// <remarks>Set by this label's looks.</remarks>
        public string Font
        {
            get { return _font; }
            private set
            {
                if (_font == value)
                    return;

                _font = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the height of one line, in UI units.</summary>
        /// <remarks>Set by this label's looks.</remarks>
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

        /// <summary>Gets the color of the text.</summary>
        /// <remarks>Set by this label's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets how the lines are aligned within the width given to the label.</summary>
        /// <remarks>Set by this label's looks.</remarks>
        public TextAlignment TextAlignment { get; private set; }

        /// <summary>
        ///     Gets or sets what happens to text that is too wide for the label: it overflows, wraps onto the next
        ///     line, or is trimmed with an ellipsis.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return _textWrapping; }
            set
            {
                if (_textWrapping == value)
                    return;

                _textWrapping = value;
                _dirty = true;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the lines the text was last laid out as when wrapped or trimmed.</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Gets the space between the label's edge and its text.</summary>
        /// <remarks>Set by this label's looks.</remarks>
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

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Label>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new LabelLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var text = (LabelLook)look;
            Font = text.Font ?? MyFontEnum.White;
            FontSize = text.FontSize ?? TextLook.NeutralFontSize;
            Color = text.Color ?? Color.White;
            TextAlignment = text.TextAlignment ?? TextAlignment.Left;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_text.Length == 0)
                return new Vector2(0f, FontSize);

            // The line height, not the measured height: DrawText places a line box at ContentBounds.Y,
            // and the game's glyph cell is 45 against a line of 37, so the measured height is not what
            // is drawn.
            switch (_textWrapping)
            {
                case TextWrapping.NoWrap:
                    return new Vector2(Flat(g), FontSize);
                case TextWrapping.Trim:
                    Retrim(g, available.X);
                    return new Vector2(_wrapped.X, FontSize);
                case TextWrapping.Wrap:
                    Rewrap(g, available.X);
                    return _wrapped;
                default:
                    throw new System.InvalidOperationException("A label cannot lay its words out as " + _textWrapping + ".");
            }
        }

        /// <summary>Returns the width of the text as one unwrapped line, cached between measures.</summary>
        /// <remarks>Kept, because measuring text is the dearest call the layout makes.</remarks>
        float Flat(IGraphics g)
        {
            if (!_flatDirty && _flatAt == FontSize && _flatIn == Font)
                return _flat;

            _flatDirty = false;
            _flatAt = FontSize;
            _flatIn = Font;
            _flat = g.MeasureText(Font, _measured, FontSize).X;

            return _flat;
        }

        /// <inheritdoc />
        /// <remarks>Measures nothing: every line's width was kept when it was laid out.</remarks>
        protected override void DrawContent(IGraphics g)
        {
            if (_text.Length == 0)
                return;

            if (_textWrapping == TextWrapping.NoWrap)
            {
                g.DrawText(Font, _text, new Vector2(Left(_flat), ContentBounds.Y), FontSize, Color);
                return;
            }

            var y = ContentBounds.Y;
            for (var i = 0; i < _lines.Count; i++)
            {
                g.DrawText(Font, _lines[i], new Vector2(Left(_lineWidths[i]), y), FontSize, Color);
                y += FontSize;
            }
        }

        /// <summary>
        ///     Returns the x position where a line of <paramref name="width" /> starts, according to
        ///     <see cref="TextAlignment" />; a line wider than the available space starts at the left edge.
        /// </summary>
        float Left(float width)
        {
            var room = ContentBounds;
            if (TextAlignment == TextAlignment.Left || width >= room.Width)
                return room.X;

            return TextAlignment == TextAlignment.Center ? room.X + (room.Width - width) * 0.5f : room.X + room.Width - width;
        }

        /// <summary>
        ///     Breaks the text into lines that fit <paramref name="width" />, if anything has changed since the last
        ///     wrap.
        /// </summary>
        void Rewrap(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == FontSize && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = FontSize;
            _laidOutIn = Font;

            _lines.Clear();
            _lineWidths.Clear();

            var widest = 0f;
            var i = 0;

            while (i < _text.Length)
            {
                var end = _text.IndexOf('\n', i);
                if (end < 0)
                    end = _text.Length;

                Paragraph(g, i, end, width, ref widest);
                i = end + 1;
            }

            _wrapped = new Vector2(widest, _lines.Count * FontSize);
        }

        /// <summary>
        ///     Breaks the text from <paramref name="start" /> to <paramref name="end" />, which contains no newline,
        ///     into lines no wider than <paramref name="width" />.
        /// </summary>
        /// <remarks>
        ///     Each line is the furthest word end that fits, found by halving, so a line costs a few measures rather than
        ///     one a word. A word too wide for any line has one to itself.
        /// </remarks>
        void Paragraph(IGraphics g, int start, int end, float width, ref float widest)
        {
            _breaks.Clear();
            for (var b = start; b < end;)
            {
                b = NextBreak(b);
                _breaks.Add(b < end ? b : end);
            }

            if (_breaks.Count == 0)
            {
                _lines.Add("");
                _lineWidths.Add(0f);
                return;
            }

            var from = start;
            var first = 0;

            while (first < _breaks.Count)
            {
                var best = first;
                var fits = Width(g, from, _breaks[first]);
                var last = _breaks.Count - 1;

                if (last > first)
                {
                    var whole = Width(g, from, _breaks[last]);
                    if (whole <= width)
                    {
                        best = last;
                        fits = whole;
                    }
                    else if (fits <= width)
                    {
                        var low = first + 1;
                        var high = last - 1;

                        while (low <= high)
                        {
                            var middle = (low + high) / 2;
                            var measured = Width(g, from, _breaks[middle]);

                            if (measured <= width)
                            {
                                best = middle;
                                fits = measured;
                                low = middle + 1;
                            }
                            else
                                high = middle - 1;
                        }
                    }
                }

                _lines.Add(Line(from, _breaks[best]));
                _lineWidths.Add(fits);
                if (fits > widest)
                    widest = fits;

                from = _breaks[best];
                first = best + 1;
            }
        }

        /// <summary>
        ///     Returns the drawn width of the text from <paramref name="from" /> to <paramref name="to" />, excluding
        ///     the spaces it starts with.
        /// </summary>
        float Width(IGraphics g, int from, int to)
        {
            while (from < to && _text[from] == ' ')
                from++;

            _scratch.Clear();
            _scratch.Append(_text, from, to - from);
            return g.MeasureText(Font, _scratch, FontSize).X;
        }

        /// <summary>
        ///     Returns the text from <paramref name="from" /> to <paramref name="to" />, excluding the spaces it starts
        ///     with.
        /// </summary>
        string Line(int from, int to)
        {
            while (from < to && _text[from] == ' ')
                from++;

            return _text.Substring(from, to - from);
        }

        /// <summary>Trims the text with an ellipsis to the longest part that fits <paramref name="width" />.</summary>
        void Retrim(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == FontSize && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = FontSize;
            _laidOutIn = Font;

            _lines.Clear();
            _lineWidths.Clear();

            var full = g.MeasureText(Font, _measured, FontSize).X;
            if (full <= width)
            {
                _lines.Add(_text);
                _lineWidths.Add(full);
                _wrapped = new Vector2(full, 0f);
                return;
            }

            var keep = 0;
            var keptWidth = 0f;
            var low = 1;
            var high = _text.Length - 1;

            while (low <= high)
            {
                var middle = (low + high) / 2;

                _scratch.Clear();
                _scratch.Append(_text, 0, middle);
                _scratch.Append(Ellipsis);

                var measured = g.MeasureText(Font, _scratch, FontSize).X;
                if (measured <= width)
                {
                    keep = middle;
                    keptWidth = measured;
                    low = middle + 1;
                }
                else
                    high = middle - 1;
            }

            if (keep == 0)
            {
                _scratch.Clear();
                _scratch.Append(Ellipsis);
                keptWidth = g.MeasureText(Font, _scratch, FontSize).X;
            }

            _lines.Add(_text.Substring(0, keep) + Ellipsis);
            _lineWidths.Add(keptWidth);
            _wrapped = new Vector2(width, 0f);
        }

        /// <summary>
        ///     Returns the index where the next word ends, counting the spaces that follow the word as part of it.
        /// </summary>
        int NextBreak(int from)
        {
            var i = from;

            while (i < _text.Length && _text[i] == ' ')
                i++;

            while (i < _text.Length && _text[i] != ' ' && _text[i] != '\n')
                i++;

            return i > from ? i : from + 1;
        }
    }
}
