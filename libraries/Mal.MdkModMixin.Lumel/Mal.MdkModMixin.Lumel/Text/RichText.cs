using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Game;
using VRageMath;
using Lumel.Input;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     A document wrapped to the width it is given, showing headings, paragraphs, list items, rules, tables,
    ///     quotes, callouts and images, with each run formatted by its kind.
    /// </summary>
    /// <remarks>
    ///     Laid out only when the document, its sizes, its <see cref="Arguments" /> or the room change; drawing measures
    ///     nothing, and changing a color lays out nothing. An image whose locator the graphics'
    ///     <see cref="IGraphics.Images" /> cannot find says its description instead.
    /// </remarks>
    public class RichText : Element, IPadded, ITextual
    {
        Thickness _padding;
        readonly List<Piece> _pieces = new List<Piece>();
        readonly List<Mark> _marks = new List<Mark>();

        readonly StringBuilder _piece = new StringBuilder(64);

        readonly StringBuilder _probe = new StringBuilder(64);

        readonly StringBuilder _tail = new StringBuilder(64);

        readonly List<float> _wides = new List<float>();
        readonly List<Spot> _spots = new List<Spot>();

        readonly CalloutLooks _callouts = new CalloutLooks(null);
        readonly LinkLooks _links = new LinkLooks(null);
        readonly SpanLooks _spans = new SpanLooks(null);
        bool _hasDottedLinks;
        readonly StringBuilder _spaces = new StringBuilder();
        Func<Link, bool> _fnIsLinkPlain;

        Document _document;
        string _markdown;
        Arguments _arguments;
        string _font = MyFontEnum.White;
        string _codeFont = MyFontEnum.White;
        string _strongFont;
        string _emphasisFont;
        string _strongEmphasisFont;
        string _headingFont;
        float _fontSize = 24f;
        float _heading1 = 24f;
        float _heading2 = 24f;
        float _heading3 = 24f;
        float _spacing;
        float? _headingSpacing;
        float? _headingRuleSpacing;
        int _underlinedHeadingLevel;
        float _ruleThickness = 1f;
        float? _bulletSize;
        float? _quoteBarWidth;
        float _indent = 24f;
        float? _quoteIndent;
        float? _tableColumnSpacing;
        Thickness? _calloutPadding;
        Thickness _codePadding;
        TextAlignment _textAlignment;
        Action<Link> _linkClicked;
        Link _pressed;

        bool _stale = true;
        float _laidOutFor = -1f;
        Vector2 _laidOut;

        float _pieceX;
        string _pieceFont;
        float _pieceSize;
        Tone _pieceTone;
        Link _pieceLink;
        Color? _pieceSpanInk;
        bool _pieceCode;

        bool _emphasized;

        /// <summary>
        ///     Creates an empty document that uses its host theme's default rich text style until it is given a style.
        /// </summary>
        public RichText()
        {
            Color = Color.White;
            StrongColor = Color.White;
            EmphasisColor = Color.White;
            HeadingColor = Color.White;
            RuleColor = Color.White;
            BulletColor = Color.White;
            QuoteBarColor = Color.White;
            LinkColor = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this rich text, or null to use the default rich text style of its host theme.
        /// </summary>
        public new IStyle<RichText> Style
        {
            get { return (IStyle<RichText>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this rich text's own look for every state, applied over its style.</summary>
        public new RichTextLook Base => (RichTextLook)base.Base;

        /// <summary>Gets this rich text's own look on hover, applied over its style.</summary>
        public new RichTextLook PointerOver => (RichTextLook)base.PointerOver;

        /// <summary>Gets this rich text's own look when disabled, applied over its style.</summary>
        public new RichTextLook Disabled => (RichTextLook)base.Disabled;

        /// <summary>
        ///     Gets this rich text's own look for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style.
        /// </summary>
        public new RichTextLook this[State state] => (RichTextLook)base[state];

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<RichText>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new RichTextLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var text = (RichTextLook)look;
            Font = text.Font ?? MyFontEnum.White;
            FontSize = text.FontSize ?? TextLook.NeutralFontSize;
            Color = text.Color ?? Color.White;
            StrongColor = text.StrongColor ?? Color.White;
            EmphasisColor = text.EmphasisColor ?? Color.White;
            HeadingColor = text.HeadingColor ?? Color.White;
            RuleColor = text.RuleColor ?? Color.White;
            RuleThickness = text.RuleThickness ?? 1f;
            BulletColor = text.BulletColor ?? RuleColor;
            BulletSize = text.BulletSize;
            QuoteBarColor = text.QuoteBarColor ?? RuleColor;
            QuoteBarWidth = text.QuoteBarWidth;
            CodeFont = text.CodeFont ?? MyFontEnum.White;
            StrongFont = text.StrongFont;
            EmphasisFont = text.EmphasisFont;
            StrongEmphasisFont = text.StrongEmphasisFont;
            HeadingFont = text.HeadingFont;
            Heading1Size = text.Heading1Size ?? TextLook.NeutralFontSize;
            Heading2Size = text.Heading2Size ?? TextLook.NeutralFontSize;
            Heading3Size = text.Heading3Size ?? TextLook.NeutralFontSize;
            BlockSpacing = text.BlockSpacing ?? 0f;
            HeadingSpacing = text.HeadingSpacing;
            HeadingRuleSpacing = text.HeadingRuleSpacing;
            UnderlinedHeadingLevel = text.UnderlinedHeadingLevel ?? 0;
            Indent = text.Indent ?? TextLook.NeutralFontSize;
            QuoteIndent = text.QuoteIndent;
            TableColumnSpacing = text.TableColumnSpacing;
            CalloutPadding = text.CalloutPadding;
            TextAlignment = text.TextAlignment ?? TextAlignment.Left;
            LinkColor = text.LinkColor ?? Color.White;
            LinkBackground = text.LinkBackground;
            CodeBackground = text.CodeBackground;
            CodePadding = text.CodePadding ?? default(Thickness);
            LinkUnderlineThickness = text.LinkUnderlineThickness ?? 0f;
            UnderlineOffset = text.UnderlineOffset ?? 0.85f;

            if (_callouts.SetsSameAs(text.Callouts) && _links.SetsSameAs(text.Links) && _spans.SetsSameAs(text.Spans))
                return;

            _callouts.Clear();
            _callouts.Merge(text.Callouts);
            _links.Clear();
            _links.Merge(text.Links);
            _spans.Clear();
            _spans.Merge(text.Spans);
            Relayout();
        }

        /// <summary>Gets or sets the document shown, or null to show nothing.</summary>
        public Document Document
        {
            get { return _document; }
            set
            {
                if (_document == value)
                    return;

                _document = value;
                _markdown = null;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets or sets the markdown-lite the document was parsed from, or null if the document was set directly;
        ///     it is parsed again only when it changes.
        /// </summary>
        public string Markdown
        {
            get { return _markdown; }
            set
            {
                if (value == _markdown)
                    return;

                Document = value == null ? null : Text.Markdown.Parse(value);
                _markdown = value;
            }
        }

        /// <summary>
        ///     Gets or sets the text that fills the document's slots, or null for none; slots without text show
        ///     nothing.
        /// </summary>
        public Arguments Arguments
        {
            get { return _arguments; }
            set
            {
                if (_arguments == value)
                    return;

                if (_arguments != null)
                    _arguments.Changed -= Relayout;

                _arguments = value;

                if (_arguments != null)
                    _arguments.Changed += Relayout;

                Relayout();
            }
        }

        /// <summary>Gets the font for all text except code and strong or emphasized text.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public string Font
        {
            get { return _font; }
            private set
            {
                if (_font == value)
                    return;

                _font = value;
                Relayout();
            }
        }

        /// <summary>Gets the font for code.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public string CodeFont
        {
            get { return _codeFont; }
            private set
            {
                if (_codeFont == value)
                    return;

                _codeFont = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets the font for strong text, or null to use <see cref="Font" />; strong code stays in
        ///     <see cref="CodeFont" />.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public string StrongFont
        {
            get { return _strongFont; }
            private set
            {
                if (_strongFont == value)
                    return;

                _strongFont = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets the font for emphasized text, or null to use <see cref="Font" />; emphasized code stays in
        ///     <see cref="CodeFont" />.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public string EmphasisFont
        {
            get { return _emphasisFont; }
            private set
            {
                if (_emphasisFont == value)
                    return;

                _emphasisFont = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets the font for text that is both strong and emphasized, or null to use <see cref="StrongFont" />,
        ///     then <see cref="EmphasisFont" />.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public string StrongEmphasisFont
        {
            get { return _strongEmphasisFont; }
            private set
            {
                if (_strongEmphasisFont == value)
                    return;

                _strongEmphasisFont = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets the font for headings and a table's heading row, or null to use the strong font, which makes every
        ///     heading bold.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public string HeadingFont
        {
            get { return _headingFont; }
            private set
            {
                if (_headingFont == value)
                    return;

                _headingFont = value;
                Relayout();
            }
        }

        /// <summary>Gets the line height of all text except headings, in UI units.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float FontSize
        {
            get { return _fontSize; }
            private set { Resize(ref _fontSize, value); }
        }

        /// <summary>Gets the line height of first-level headings.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float Heading1Size
        {
            get { return _heading1; }
            private set { Resize(ref _heading1, value); }
        }

        /// <summary>Gets the line height of second-level headings.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float Heading2Size
        {
            get { return _heading2; }
            private set { Resize(ref _heading2, value); }
        }

        /// <summary>Gets the line height of headings at the third level or deeper.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float Heading3Size
        {
            get { return _heading3; }
            private set { Resize(ref _heading3, value); }
        }

        /// <summary>Gets the space between one block and the next.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float BlockSpacing
        {
            get { return _spacing; }
            private set { Resize(ref _spacing, value); }
        }

        /// <summary>
        ///     Gets the space above a heading that is not the first block, or null for twice
        ///     <see cref="BlockSpacing" />.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float? HeadingSpacing
        {
            get { return _headingSpacing; }
            private set { Resize(ref _headingSpacing, value); }
        }

        /// <summary>
        ///     Gets the space between a heading and the rule under it, or null for half of <see cref="BlockSpacing" />.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float? HeadingRuleSpacing
        {
            get { return _headingRuleSpacing; }
            private set { Resize(ref _headingRuleSpacing, value); }
        }

        /// <summary>
        ///     Gets the deepest heading level drawn with a rule under it, counting from 1, or 0 for none.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public int UnderlinedHeadingLevel
        {
            get { return _underlinedHeadingLevel; }
            private set
            {
                if (_underlinedHeadingLevel == value)
                    return;

                _underlinedHeadingLevel = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets the thickness of rules and of the lines under headings and a table's heading row, in UI units.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float RuleThickness
        {
            get { return _ruleThickness; }
            private set { Resize(ref _ruleThickness, value); }
        }

        /// <summary>Gets the width of a bullet's square mark, or null for a fifth of its line height.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float? BulletSize
        {
            get { return _bulletSize; }
            private set { Resize(ref _bulletSize, value); }
        }

        /// <summary>
        ///     Gets the width of the bar along the edge of a quote or callout, or null for an eighth of a line with a
        ///     minimum of 1.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float? QuoteBarWidth
        {
            get { return _quoteBarWidth; }
            private set { Resize(ref _quoteBarWidth, value); }
        }

        /// <summary>Gets how far a list item's text is indented from its mark.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float Indent
        {
            get { return _indent; }
            private set { Resize(ref _indent, value); }
        }

        /// <summary>
        ///     Gets how far a quote's blocks are indented from its edge, or null to use <see cref="Indent" />.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float? QuoteIndent
        {
            get { return _quoteIndent; }
            private set { Resize(ref _quoteIndent, value); }
        }

        /// <summary>Gets the space between a table's columns, or null to use <see cref="Indent" />.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float? TableColumnSpacing
        {
            get { return _tableColumnSpacing; }
            private set { Resize(ref _tableColumnSpacing, value); }
        }

        /// <summary>
        ///     Gets the space between a callout's edge and its blocks, or null for <see cref="Indent" /> on the left of
        ///     a callout with a bar or a background, a quarter of a line above and below, and half of
        ///     <see cref="Indent" /> on the right of a callout with a background.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Thickness? CalloutPadding
        {
            get { return _calloutPadding; }
            private set
            {
                if (_calloutPadding == value)
                    return;

                _calloutPadding = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets the alignment of every line within the available width, and of a table narrower than that width;
        ///     table cells align as their columns specify.
        /// </summary>
        /// <remarks>Lines set anywhere but the left take the whole width they are given, so that they have room to be set in. Set by this text's looks.</remarks>
        public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            private set
            {
                if (_textAlignment == value)
                    return;

                _textAlignment = value;
                Relayout();
            }
        }

        /// <summary>Gets the color of ordinary text and of an ordered list item's number.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the color of strong text.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color StrongColor { get; private set; }

        /// <summary>Gets the color of emphasized text that is not also strong.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color EmphasisColor { get; private set; }

        /// <summary>Gets the color of headings.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color HeadingColor { get; private set; }

        /// <summary>Gets the color of rules and of the line under a table's heading row.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color RuleColor { get; private set; }

        /// <summary>Gets the color of a bullet's mark.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color BulletColor { get; private set; }

        /// <summary>Gets the color of the bar along a quote's edge.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color QuoteBarColor { get; private set; }

        /// <summary>Gets the color of link text.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color LinkColor { get; private set; }

        /// <summary>Gets the color painted behind link text, or null for none.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color? LinkBackground { get; private set; }

        /// <summary>Gets the color painted behind code, or null for none.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Color? CodeBackground { get; private set; }

        /// <summary>
        ///     Gets how far the box behind code extends past its text on each side, in UI units; neighboring text stays
        ///     clear of it.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public Thickness CodePadding
        {
            get { return _codePadding; }
            private set
            {
                if (_codePadding == value)
                    return;

                _codePadding = value;
                Relayout();
            }
        }

        /// <summary>Gets the thickness of the underline under link text, in UI units, or 0 for none.</summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float LinkUnderlineThickness { get; private set; }

        /// <summary>
        ///     Gets the vertical position of a link's underline, as a fraction of the line height measured from the top
        ///     of the line.
        /// </summary>
        /// <remarks>Set by this text's looks.</remarks>
        public float UnderlineOffset { get; private set; }

        /// <summary>
        ///     Raised with the link when a link is pressed; while nothing handles it, links are plain text that ignores
        ///     presses.
        /// </summary>
        /// <remarks>While it is handled, the text takes the pointer only where a link is, and a press anywhere else reaches what is beneath it.</remarks>
        public event Action<Link> LinkClicked
        {
            add
            {
                _linkClicked += value;
                _pressed = null;
            }
            remove
            {
                _linkClicked -= value;
                _pressed = null;
            }
        }

        /// <summary>
        ///     Gets or sets the callback that returns true for a link to show as the words around it, with no look of a
        ///     link, no tip and no press, or null to show every link.
        /// </summary>
        /// <remarks>
        ///     Asked as the text is laid out, which setting this causes, so a host whose answer has changed sets it again.
        /// </remarks>
        public Func<Link, bool> FnIsLinkPlain
        {
            get { return _fnIsLinkPlain; }
            set
            {
                _fnIsLinkPlain = value;
                Relayout();
            }
        }

        /// <summary>
        ///     Gets or sets the callback that returns the tip shown for a given link when the pointer rests on it, or
        ///     null to use the text's own tip.
        /// </summary>
        /// <remarks>Asked while the pointer rests on a link, so a tip it hands back is one made once and kept.</remarks>
        public Func<Link, Tip> FnResolveLinkTip { get; set; }

        /// <summary>Gets the space between the edge and the text.</summary>
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

        /// <inheritdoc />
        protected override bool TakesPointer => _linkClicked != null && _spots.Count > 0;

        /// <summary>
        ///     Returns the link whose text is at <paramref name="point" />, in the coordinates <see cref="Element.Holds" />
        ///     takes, or null if there is none.
        /// </summary>
        /// <remarks>Looked for among the links' own places, worked out when the text was laid out, so asking costs no layout.</remarks>
        public Link LinkAt(Vector2 point)
        {
            var at = SpotAt(point);
            return at < 0 ? null : _spots[at].Link;
        }

        /// <summary>Returns the index of the spot at <paramref name="point" />, or -1 if there is none.</summary>
        int SpotAt(Vector2 point)
        {
            var room = ContentBounds;
            var x = point.X - room.X;
            var y = point.Y - room.Y;

            for (var i = 0; i < _spots.Count; i++)
            {
                var where = _spots[i].Where;
                if (x >= where.X && x <= where.X + where.Width && y >= where.Y && y <= where.Y + where.Height)
                    return i;
            }

            return -1;
        }

        /// <inheritdoc />
        public override Tip TipAt(Vector2 point)
        {
            var link = FnResolveLinkTip == null ? null : LinkAt(point);
            return link == null ? Tip : FnResolveLinkTip(link) ?? Tip;
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            var at = SpotAt(point);
            _pressed = at >= 0 && _spots[at].Presses ? _spots[at].Link : null;
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            var pressed = _pressed;
            _pressed = null;

            if (pressed != null && LinkAt(point) == pressed)
                _linkClicked?.Invoke(pressed);
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            _pressed = null;
        }

        /// <inheritdoc />
        protected override bool Hits(Vector2 point)
        {
            return _linkClicked == null ? base.Hits(point) : LinkAt(point) != null;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_stale || _laidOutFor != available.X)
                Lay(g, available.X);

            return _laidOut;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;

            for (var i = 0; i < _marks.Count; i++)
            {
                var where = _marks[i].Where;
                if (_marks[i].Across)
                    where.Width = room.Width - where.X;

                g.Fill(new RectangleF(room.X + where.X, room.Y + where.Y, where.Width, where.Height), ColorOf(_marks[i]));
            }

            for (var i = 0; i < _spots.Count; i++)
            {
                var back = _spots[i].Background ?? LinkBackground;
                var where = _spots[i].Where;
                if (back.HasValue)
                    g.Fill(new RectangleF(room.X + where.X, room.Y + where.Y, where.Width, where.Height), back.Value);
            }

            DrawUnderlines(g, room, false);

            // The dotted lines are drawn together, since the game starts a new batch at every change of texture.
            if (_hasDottedLinks)
                DrawUnderlines(g, room, true);

            var padding = _codePadding;
            foreach (var piece in _pieces)
            {
                if (piece.Code && CodeBackground.HasValue)
                {
                    g.Fill(new RectangleF(room.X + piece.At.X - padding.Left, room.Y + piece.At.Y - padding.Top, piece.Box.X + padding.Horizontal,
                        piece.Box.Y + padding.Vertical), CodeBackground.Value);
                }

                if (piece.Image != null)
                    piece.Image.Draw(g, new RectangleF(room.X + piece.At.X + (piece.Box.X - piece.Drawn.X) * 0.5f,
                        room.Y + piece.At.Y + (piece.Box.Y - piece.Drawn.Y) * 0.5f, piece.Drawn.X, piece.Drawn.Y), piece.Tinted ? InkOf(piece) : Color.White, piece.Turn);
                else
                    g.DrawText(piece.Font, piece.Text, new Vector2(room.X + piece.At.X, room.Y + piece.At.Y), piece.Size, InkOf(piece));
            }
        }

        /// <summary>Returns the color <paramref name="piece" /> is drawn in.</summary>
        Color InkOf(Piece piece)
        {
            return piece.LinkInk ?? piece.SpanInk ?? (piece.Tone == Tone.Plain && piece.Ink.HasValue ? piece.Ink.Value : ColorOf(piece.Tone));
        }

        /// <summary>Draws the solid underlines of the links, or the dotted ones where <paramref name="dotted" /> is true.</summary>
        void DrawUnderlines(IGraphics g, RectangleF room, bool dotted)
        {
            for (var i = 0; i < _spots.Count; i++)
            {
                var spot = _spots[i];
                var thick = spot.Thickness ?? LinkUnderlineThickness;
                if (spot.Dotted != dotted || thick <= 0f || spot.IsPicture)
                    continue;

                var where = spot.Where;
                var line = new RectangleF(room.X + where.X + spot.Lead, room.Y + where.Y + where.Height * UnderlineOffset, where.Width - spot.Lead, thick);
                if (dotted)
                    DottedLine.Draw(g, line, spot.Ink ?? LinkColor);
                else
                    g.Fill(line, spot.Ink ?? LinkColor);
            }
        }

        /// <summary>
        ///     Returns the color <paramref name="mark" /> is drawn in: its own color if it has one, otherwise the color
        ///     of its kind.
        /// </summary>
        Color ColorOf(Mark mark)
        {
            if (mark.Color.HasValue)
                return mark.Color.Value;

            switch (mark.Kind)
            {
                case MarkKind.Bullet:
                    return BulletColor;
                case MarkKind.QuoteBar:
                    return QuoteBarColor;
                default:
                    return RuleColor;
            }
        }

        Color ColorOf(Tone tone)
        {
            switch (tone)
            {
                case Tone.Strong:
                    return StrongColor;
                case Tone.Emphasis:
                    return EmphasisColor;
                case Tone.Heading:
                    return HeadingColor;
                case Tone.Link:
                    return LinkColor;
                default:
                    return Color;
            }
        }

        void Resize(ref float field, float value)
        {
            if (field == value)
                return;

            field = value;
            Relayout();
        }

        void Resize(ref float? field, float? value)
        {
            if (field == value)
                return;

            field = value;
            Relayout();
        }

        void Relayout()
        {
            _stale = true;
            InvalidateMeasure();
        }

        /// <summary>
        ///     Breaks the document into lines no wider than <paramref name="width" />, and the lines into pieces that
        ///     are each drawn in one call.
        /// </summary>
        void Lay(IGraphics g, float width)
        {
            _stale = false;
            _laidOutFor = width;

            _pieces.Clear();
            _marks.Clear();
            _spots.Clear();

            var y = 0f;
            var widest = 0f;

            if (_document == null)
            {
                _laidOut = Vector2.Zero;
                return;
            }

            Stack(g, _document.Blocks, 0f, width, _textAlignment, ref y, ref widest);

            if (_textAlignment != TextAlignment.Left && width < Unbounded && widest < width)
                widest = width;

            _laidOut = new Vector2(widest, y);

            _hasDottedLinks = false;
            foreach (var piece in _pieces)
            {
                if (piece.Link == null)
                    continue;

                var look = _links.Find(piece.Link.Class);
                var dotted = look?.Underline == UnderlineStyle.Dotted;
                _hasDottedLinks |= dotted;
                _spots.Add(new Spot
                {
                    Where = new RectangleF(piece.At.X, piece.At.Y, piece.Box.X, piece.Box.Y),
                    Link = piece.Link,
                    Ink = look?.Color,
                    Background = look?.Background,
                    Thickness = look?.UnderlineThickness,
                    Dotted = dotted,
                    Presses = look?.Presses ?? true,
                    IsPicture = piece.Image != null,
                    Lead = piece.Lead
                });
            }
        }

        /// <summary>
        ///     Lays out <paramref name="blocks" /> one under another starting at line <paramref name="y" />, between
        ///     <paramref name="left" /> and <paramref name="width" />, with lines aligned per
        ///     <paramref name="align" />, and moves <paramref name="y" /> below the last block.
        /// </summary>
        void Stack(IGraphics g, IReadOnlyList<Block> blocks, float left, float width, TextAlignment align, ref float y, ref float widest)
        {
            var body = _fontSize;
            var indent = _indent;

            for (var b = 0; b < blocks.Count; b++)
            {
                var block = blocks[b];

                if (b > 0)
                    y += block.Kind == BlockKind.Heading ? _headingSpacing ?? _spacing * 2f : _spacing;

                switch (block.Kind)
                {
                    case BlockKind.Rule:
                        _marks.Add(new Mark { Where = new RectangleF(left, y + body * 0.5f, 0f, _ruleThickness), Across = true, Kind = MarkKind.Rule });
                        y += body;
                        continue;
                    case BlockKind.Table:
                        Table(g, block, left, width, align, ref y, ref widest);
                        continue;
                    case BlockKind.Quote:
                        Quote(g, block, left, width, align, ref y, ref widest);
                        continue;
                    case BlockKind.Callout:
                        Callout(g, block, left, width, align, ref y, ref widest);
                        continue;
                    case BlockKind.Paragraph:
                    case BlockKind.Heading:
                    case BlockKind.BulletListItem:
                    case BlockKind.OrderedListItem:
                        break;
                    default:
                        throw new InvalidOperationException("A " + block.Kind + " stands only inside a table.");
                }

                var size = block.Kind == BlockKind.Heading ? HeadingSize(block.Level) : body;
                var start = left;

                if (block.Kind == BlockKind.BulletListItem)
                {
                    start = left + indent;
                    var side = _bulletSize ?? size / 5f;
                    _marks.Add(new Mark { Where = new RectangleF(left + (indent - side) * 0.5f, y + (size - side) * 0.5f, side, side), Kind = MarkKind.Bullet });
                }
                else if (block.Kind == BlockKind.OrderedListItem)
                {
                    start = left + indent;
                    _pieces.Add(new Piece { Text = block.Number + ".", At = new Vector2(left, y), Font = _font, Size = size, Tone = Tone.Plain });
                }

                Flow(g, block, start, size, width, align, ref y, ref widest);

                if (block.Kind == BlockKind.Heading && block.Level <= _underlinedHeadingLevel)
                {
                    var under = _headingRuleSpacing ?? _spacing * 0.5f;
                    _marks.Add(new Mark { Where = new RectangleF(left, y + under, 0f, _ruleThickness), Across = true, Kind = MarkKind.Rule });
                    y += under + _ruleThickness;
                }
            }
        }

        /// <summary>Gets the width of the bar along the edge of a quote or callout.</summary>
        float QuoteBar => _quoteBarWidth ?? Math.Max(1f, _fontSize / 8f);

        /// <summary>
        ///     Lays out <paramref name="quote" /> starting at line <paramref name="y" />, with its blocks indented from
        ///     a bar along its edge at <paramref name="left" />, and moves <paramref name="y" /> below it.
        /// </summary>
        void Quote(IGraphics g, Block quote, float left, float width, TextAlignment align, ref float y, ref float widest)
        {
            var top = y;
            Stack(g, quote.Blocks, left + (_quoteIndent ?? _indent), width, align, ref y, ref widest);
            _marks.Add(new Mark { Where = new RectangleF(left, top, QuoteBar, y - top), Kind = MarkKind.QuoteBar });
        }

        /// <summary>
        ///     Lays out <paramref name="callout" /> starting at line <paramref name="y" /> as the looks of its classes
        ///     specify, and moves <paramref name="y" /> below it.
        /// </summary>
        /// <remarks>
        ///     A later class's look wins over an earlier one's, and the callout's color, face and alignment reach the callouts
        ///     inside it.
        /// </remarks>
        void Callout(IGraphics g, Block callout, float left, float width, TextAlignment align, ref float y, ref float widest)
        {
            Color? bar = null;
            Color? background = null;
            Color? color = null;
            bool? emphasized = null;
            var inner = align;
            for (var i = 0; i < callout.Classes.Count; i++)
            {
                var name = callout.Classes[i];
                if (string.Equals(name, "left", StringComparison.OrdinalIgnoreCase))
                    inner = TextAlignment.Left;
                else if (string.Equals(name, "center", StringComparison.OrdinalIgnoreCase))
                    inner = TextAlignment.Center;
                else if (string.Equals(name, "right", StringComparison.OrdinalIgnoreCase))
                    inner = TextAlignment.Right;

                var look = _callouts.Find(name);
                if (look == null)
                    continue;

                bar = look.BarColor ?? bar;
                background = look.Background ?? background;
                color = look.Color ?? color;
                emphasized = look.IsEmphasized ?? emphasized;
            }

            var top = y;
            var behind = _marks.Count;
            var first = _pieces.Count;
            var padding = _calloutPadding ?? new Thickness(bar.HasValue || background.HasValue ? _indent : 0f, background.HasValue ? _fontSize * 0.25f : 0f,
                background.HasValue ? _indent * 0.5f : 0f, background.HasValue ? _fontSize * 0.25f : 0f);

            // Its face reaches the callouts inside it, as its color does, until one of them says otherwise.
            var emphasizedWas = _emphasized;
            _emphasized = emphasized ?? _emphasized;

            y += padding.Top;
            Stack(g, callout.Blocks, left + padding.Left, width < Unbounded ? width - padding.Right : width, inner, ref y, ref widest);
            y += padding.Bottom;

            _emphasized = emphasizedWas;

            if (background.HasValue)
                _marks.Insert(behind, new Mark { Where = new RectangleF(left, top, 0f, y - top), Across = true, Color = background });

            if (bar.HasValue)
                _marks.Add(new Mark { Where = new RectangleF(left, top, QuoteBar, y - top), Color = bar });

            if (inner != TextAlignment.Left && width < Unbounded && width > widest)
                widest = width;

            if (!color.HasValue)
                return;

            for (var i = first; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece.Ink.HasValue)
                    continue;

                piece.Ink = color;
                _pieces[i] = piece;
            }
        }

        /// <summary>
        ///     Lays out <paramref name="table" /> starting at line <paramref name="y" /> one row at a time, and moves
        ///     <paramref name="y" /> below it.
        /// </summary>
        /// <remarks>
        ///     Starred columns share the room the others leave by their stars. A table too wide for its room takes the width
        ///     back from its widest columns, whose cells then wrap.
        /// </remarks>
        void Table(IGraphics g, Block table, float from, float width, TextAlignment align, ref float y, ref float widest)
        {
            var count = table.Columns.Count;
            var gap = _tableColumnSpacing ?? _indent;
            var room = width >= Unbounded ? Unbounded : width - from;
            Widths(g, table, room, gap);

            var across = gap * (count - 1);
            for (var c = 0; c < count; c++)
                across += _wides[c];

            var left = from;
            if (align != TextAlignment.Left && room < Unbounded && across < room)
                left += align == TextAlignment.Center ? (room - across) * 0.5f : room - across;

            for (var r = 0; r < table.Blocks.Count; r++)
            {
                var row = table.Blocks[r];
                var bottom = y + _fontSize;
                var x = left;

                for (var c = 0; c < count && c < row.Blocks.Count; c++)
                {
                    var line = y;
                    var right = x + _wides[c];
                    var ignored = 0f;

                    Flow(g, row.Blocks[c], x, _fontSize, right, table.Columns[c].TextAlignment, ref line, ref ignored);
                    if (line > bottom)
                        bottom = line;

                    x = right + gap;
                }

                y = bottom;

                if (row.IsHeader)
                {
                    _marks.Add(new Mark { Where = new RectangleF(left, y + gap * 0.25f, across, _ruleThickness), Kind = MarkKind.Rule });
                    y += gap * 0.5f;
                }
            }

            if (left + across > widest)
                widest = left + across;
        }

        /// <summary>
        ///     Computes the width of each column of <paramref name="table" /> within <paramref name="width" />, with
        ///     <paramref name="gap" /> between columns.
        /// </summary>
        void Widths(IGraphics g, Block table, float width, float gap)
        {
            var count = table.Columns.Count;
            _wides.Clear();
            for (var c = 0; c < count; c++)
                _wides.Add(0f);

            for (var r = 0; r < table.Blocks.Count; r++)
            {
                var row = table.Blocks[r];
                for (var c = 0; c < count && c < row.Blocks.Count; c++)
                {
                    var pieces = _pieces.Count;
                    var line = 0f;
                    var natural = 0f;

                    Flow(g, row.Blocks[c], 0f, _fontSize, Unbounded, TextAlignment.Left, ref line, ref natural);
                    _pieces.RemoveRange(pieces, _pieces.Count - pieces);

                    if (natural > _wides[c])
                        _wides[c] = natural;
                }
            }

            if (width >= Unbounded)
                return;

            var least = _fontSize * 3f;
            var fixedAcross = gap * (count - 1);
            var weights = 0;
            for (var c = 0; c < count; c++)
            {
                if (table.Columns[c].Weight > 0)
                    weights += table.Columns[c].Weight;
                else
                    fixedAcross += _wides[c];
            }

            if (weights > 0)
            {
                var room = Math.Max(0f, width - fixedAcross);
                for (var c = 0; c < count; c++)
                {
                    if (table.Columns[c].Weight > 0)
                        _wides[c] = Math.Max(least, room * table.Columns[c].Weight / weights);
                }
            }

            var across = gap * (count - 1);
            for (var c = 0; c < count; c++)
                across += _wides[c];

            var over = across - width;
            while (over > 0.5f)
            {
                var broadest = 0;
                for (var c = 1; c < count; c++)
                {
                    if (_wides[c] > _wides[broadest])
                        broadest = c;
                }

                var given = Math.Min(over, _wides[broadest] - least);
                if (given <= 0f)
                    break;

                _wides[broadest] -= given;
                over -= given;
            }
        }

        /// <summary>
        ///     Lays out the runs of <paramref name="block" /> starting at line <paramref name="y" /> and
        ///     <paramref name="start" />, wrapping at <paramref name="width" /> with each line aligned per
        ///     <paramref name="align" />, and moves <paramref name="y" /> below the last line.
        /// </summary>
        /// <remarks>A line is as tall as <paramref name="size" />, or as its tallest image, and everything on it stands on its foot.</remarks>
        void Flow(IGraphics g, Block block, float start, float size, float width, TextAlignment align, ref float y, ref float widest)
        {
            var heading = block.Kind == BlockKind.Heading || block.Kind == BlockKind.Cell && block.IsHeader;
            var x = start;
            var empty = true;
            var line = _pieces.Count;
            var tall = size;
            var pictured = false;

            for (var r = 0; r < block.Runs.Count; r++)
            {
                var run = block.Runs[r];

                if (run.IsBreak)
                {
                    End(g, y, true, ref x, ref widest);
                    Settle(line, x, width, align, y, tall, pictured);
                    line = _pieces.Count;
                    y += tall;
                    tall = size;
                    pictured = false;
                    x = start;
                    empty = true;
                    continue;
                }

                var shown = run as ImageRun;
                var image = shown != null && g.Images != null ? g.Images.Find(shown.Picture.Locator) : null;
                if (image != null)
                {
                    var drawn = SizeOf(shown.Picture, image, size);
                    var turn = MathHelper.ToRadians(shown.Picture.Rotation);
                    var box = Turned(drawn, turn);
                    End(g, y, false, ref x, ref widest);

                    if (!empty && width < Unbounded && x + box.X + Tail(g, block, r + 1, size, heading) > width)
                    {
                        Trim(g, line, ref x);
                        Settle(line, x, width, align, y, tall, pictured);
                        line = _pieces.Count;
                        y += tall;
                        tall = size;
                        x = start;
                    }

                    var imageLink = LiveLink(run);
                    _pieces.Add(new Piece
                    {
                        Image = image,
                        At = new Vector2(x, y),
                        Box = box,
                        Drawn = drawn,
                        Turn = turn,
                        Tinted = run.Link != null,
                        Link = imageLink,
                        Tone = ToneOf(run, imageLink, heading),
                        LinkInk = _links.Find(imageLink?.Class)?.Color
                    });
                    pictured = true;
                    x += box.X;
                    if (x > widest)
                        widest = x;
                    if (box.Y > tall)
                        tall = box.Y;

                    empty = false;
                    continue;
                }

                var link = LiveLink(run);
                var tone = ToneOf(run, link, heading);
                var code = (run.Kind & RunKind.Code) != 0;
                var spanInk = run.Color ?? _spans.Find(run.Class)?.Color;
                var font = FaceFor(run, heading);
                var text = run.Slot == null ? run.Text : _arguments == null ? "" : _arguments.Get(run.Slot);
                var joins = run.Slot == null ? run.HasNoBreakSpace : text.IndexOf(Run.NoBreakSpace) >= 0;
                var i = 0;

                while (i < text.Length)
                {
                    if (text[i] == ' ')
                    {
                        if (!empty)
                        {
                            // A link's piece ends at its last word, so that its fill and the room that presses it
                            // stop there too; so does code's, so that its box does.
                            if (_piece.Length > 0 && (_pieceLink != link || _pieceCode != code))
                                End(g, y, false, ref x, ref widest);

                            if (_piece.Length == 0)
                                Begin(x, font, size, tone, link, code, spanInk);

                            _piece.Append(' ');
                        }

                        i++;
                        continue;
                    }

                    var end = text.IndexOf(' ', i);
                    if (end < 0)
                        end = text.Length;

                    if (_piece.Length > 0 && (_pieceTone != tone || _pieceFont != font || _pieceLink != link || _pieceCode != code || _pieceSpanInk != spanInk))
                        End(g, y, false, ref x, ref widest);

                    if (!empty && width < Unbounded && !Fits(g, text, i, end, joins, font, size, code, x, end == text.Length ? width - Tail(g, block, r + 1, size, heading) : width))
                    {
                        End(g, y, true, ref x, ref widest);
                        Settle(line, x, width, align, y, tall, pictured);
                        line = _pieces.Count;
                        y += tall;
                        tall = size;
                        pictured = false;
                        x = start;
                    }

                    if (_piece.Length == 0)
                        Begin(x, font, size, tone, link, code, spanInk);

                    Shown(_piece, text, i, end, joins);
                    empty = false;
                    i = end;
                }
            }

            End(g, y, true, ref x, ref widest);
            Settle(line, x, width, align, y, tall, pictured);
            y += tall;
        }

        /// <summary>Returns the link <paramref name="run" /> belongs to, or null if it has none or the link shows as plain words.</summary>
        Link LiveLink(Run run)
        {
            var link = run.Link;
            return link != null && _fnIsLinkPlain != null && _fnIsLinkPlain(link) ? null : link;
        }

        /// <summary>Returns the tone of <paramref name="run" />, showing <paramref name="link" /> if it has one.</summary>
        static Tone ToneOf(Run run, Link link, bool heading)
        {
            return link != null ? Tone.Link
                : heading ? Tone.Heading
                : (run.Kind & RunKind.Strong) != 0 ? Tone.Strong
                : (run.Kind & RunKind.Emphasis) != 0 ? Tone.Emphasis
                : Tone.Plain;
        }

        /// <summary>
        ///     Returns the space a picture of <paramref name="size" /> takes once rotated by <paramref name="turn" />
        ///     radians: the bounding box of its rotated outline.
        /// </summary>
        static Vector2 Turned(Vector2 size, float turn)
        {
            if (turn == 0f)
                return size;

            var cos = Math.Abs((float)Math.Cos(turn));
            var sin = Math.Abs((float)Math.Sin(turn));
            return new Vector2(size.X * cos + size.Y * sin, size.X * sin + size.Y * cos);
        }

        /// <summary>
        ///     Returns the size an image of <paramref name="picture" /> is drawn at: as written, with a side that is
        ///     not written taken from the aspect ratio of <paramref name="image" />, and as tall as a line of
        ///     <paramref name="size" /> if neither side is written.
        /// </summary>
        static Vector2 SizeOf(Picture picture, IImageSource image, float size)
        {
            var width = picture.Width;
            var height = picture.Height;

            if (float.IsNaN(width) && float.IsNaN(height))
                height = size;

            if (float.IsNaN(width))
                width = AspectRatio.WidthFor(image.Size, height);
            else if (float.IsNaN(height))
                height = AspectRatio.HeightFor(image.Size, width);

            return new Vector2(width, height);
        }

        /// <summary>
        ///     Returns the font for text of <paramref name="kind" /> within a heading or a table's heading row.
        /// </summary>
        string HeadingFaceOf(RunKind kind)
        {
            if (_headingFont == null)
                return FaceOf(kind | RunKind.Strong);

            return (kind & (RunKind.Strong | RunKind.Emphasis)) == 0 ? _headingFont : FaceOf(kind);
        }

        /// <summary>
        ///     Returns the font for text of <paramref name="kind" /> other than code, falling back to the nearest font
        ///     that is set.
        /// </summary>
        string FaceOf(RunKind kind)
        {
            var strong = (kind & RunKind.Strong) != 0;
            var emphasized = (kind & RunKind.Emphasis) != 0;

            if (strong && emphasized)
                return _strongEmphasisFont ?? _strongFont ?? _emphasisFont ?? _font;

            if (strong)
                return _strongFont ?? _font;

            return emphasized ? _emphasisFont ?? _font : _font;
        }

        /// <summary>
        ///     Starts a piece at <paramref name="x" />; for <paramref name="code" />, the text starts past the room its
        ///     box needs.
        /// </summary>
        void Begin(float x, string font, float size, Tone tone, Link link, bool code, Color? spanInk)
        {
            _pieceX = code ? x + _codePadding.Left : x;
            _pieceCode = code;
            _pieceFont = font;
            _pieceSize = size;
            _pieceTone = tone;
            _pieceLink = link;
            _pieceSpanInk = spanInk;
        }

        /// <summary>
        ///     Drops the trailing spaces of the last piece of the line that starts at piece <paramref name="first" />,
        ///     and moves <paramref name="x" /> back to where its text ends.
        /// </summary>
        /// <remarks>For a line that breaks before an image, whose words were ended before it was known the image would not fit.</remarks>
        void Trim(IGraphics g, int first, ref float x)
        {
            var last = _pieces.Count - 1;
            if (last < first || _pieces[last].Image != null)
                return;

            var piece = _pieces[last];
            var words = piece.Text.TrimEnd(' ');
            if (words.Length == piece.Text.Length)
                return;

            _probe.Clear();
            _probe.Append(words);

            piece.Text = words;
            piece.Box.X = g.MeasureText(piece.Font, _probe, piece.Size).X;
            _pieces[last] = piece;

            x = piece.At.X + piece.Box.X + (piece.Code ? _codePadding.Right : 0f);
        }

        /// <summary>
        ///     Positions the pieces of the line from piece <paramref name="first" /> to <paramref name="end" /> on the
        ///     bottom of a line <paramref name="tall" /> high from <paramref name="top" />, aligned per
        ///     <paramref name="align" /> within <paramref name="width" />; <paramref name="pictured" /> is whether an
        ///     image is on the line.
        /// </summary>
        void Settle(int first, float end, float width, TextAlignment align, float top, float tall, bool pictured)
        {
            var shift = 0f;
            if (align != TextAlignment.Left && width < Unbounded && end < width)
                shift = align == TextAlignment.Center ? (width - end) * 0.5f : width - end;

            // A line of words alone, set from the left, is where it was laid already: the common case, so it
            // costs nothing.
            if (shift == 0f && !pictured)
                return;

            for (var i = first; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                piece.At.X += shift;
                piece.At.Y = top + tall - (piece.Image != null ? piece.Box.Y : piece.Size);
                _pieces[i] = piece;
            }
        }

        /// <summary>
        ///     Returns true if the word from <paramref name="start" /> to <paramref name="end" /> fits on the line
        ///     after the piece being built.
        /// </summary>
        /// <remarks>Measures the piece with the word on it, because the game spaces a string as a whole and a sum of its words' widths is short of it.</remarks>
        bool Fits(IGraphics g, string text, int start, int end, bool joins, string font, float size, bool code, float x, float width)
        {
            _probe.Clear();
            _probe.Append(_piece);
            Shown(_probe, text, start, end, joins);

            var from = _piece.Length > 0 ? _pieceX : x + (code ? _codePadding.Left : 0f);
            return from + g.MeasureText(font, _probe, size).X + (code ? _codePadding.Right : 0f) <= width;
        }

        /// <summary>
        ///     Returns the width of what follows <paramref name="from" /> up to the first place a line can break: the
        ///     words and pictures joined to the one being placed with no space between, which a line keeps together
        ///     with it.
        /// </summary>
        /// <remarks>
        ///     Asked only of a word or picture that ends its run, and nothing is measured where the next run starts with a space,
        ///     which is the common case.
        /// </remarks>
        float Tail(IGraphics g, Block block, int from, float size, bool heading)
        {
            var tail = 0f;
            for (var r = from; r < block.Runs.Count; r++)
            {
                var run = block.Runs[r];
                if (run.IsBreak)
                    return tail;

                var shown = run as ImageRun;
                var image = shown != null && g.Images != null ? g.Images.Find(shown.Picture.Locator) : null;
                if (image != null)
                {
                    tail += Turned(SizeOf(shown.Picture, image, size), MathHelper.ToRadians(shown.Picture.Rotation)).X;
                    continue;
                }

                var text = run.Slot == null ? run.Text : _arguments == null ? "" : _arguments.Get(run.Slot);
                var joins = run.Slot == null ? run.HasNoBreakSpace : text.IndexOf(Run.NoBreakSpace) >= 0;
                var space = text.IndexOf(' ');
                var end = space < 0 ? text.Length : space;
                if (end > 0)
                {
                    _tail.Clear();
                    Shown(_tail, text, 0, end, joins);
                    tail += g.MeasureText(FaceFor(run, heading), _tail, size).X;
                }

                if (space >= 0)
                    return tail;
            }

            return tail;
        }

        /// <summary>
        ///     Appends the text from <paramref name="start" /> to <paramref name="end" /> to <paramref name="into" />
        ///     as it is drawn, replacing a no-break space with a plain space, which every font has.
        /// </summary>
        /// <remarks><paramref name="joins" /> says whether the text holds one at all, known once for its run, so a word without one is appended whole.</remarks>
        static void Shown(StringBuilder into, string text, int start, int end, bool joins)
        {
            if (!joins)
            {
                into.Append(text, start, end - start);
                return;
            }

            for (var i = start; i < end; i++)
                into.Append(text[i] == Run.NoBreakSpace ? ' ' : text[i]);
        }

        /// <summary>
        ///     Returns the font <paramref name="run" /> is drawn in, using the heading font where
        ///     <paramref name="heading" /> is true.
        /// </summary>
        string FaceFor(Run run, bool heading)
        {
            if ((run.Kind & RunKind.Code) != 0)
                return _codeFont;

            var kind = _emphasized ? run.Kind | RunKind.Emphasis : run.Kind;
            return heading ? HeadingFaceOf(kind) : FaceOf(kind);
        }

        /// <summary>
        ///     Ends the piece being built at line <paramref name="y" /> and moves <paramref name="x" /> past it; a
        ///     piece that ends a line drops its trailing spaces.
        /// </summary>
        void End(IGraphics g, float y, bool endsLine, ref float x, ref float widest)
        {
            if (endsLine)
            {
                while (_piece.Length > 0 && _piece[_piece.Length - 1] == ' ')
                    _piece.Length--;
            }

            if (_piece.Length == 0)
                return;

            var advance = g.MeasureText(_pieceFont, _piece, _pieceSize).X;
            _pieces.Add(new Piece { Text = _piece.ToString(), At = new Vector2(_pieceX, y), Font = _pieceFont, Size = _pieceSize, Tone = _pieceTone, Link = _pieceLink, LinkInk = _links.Find(_pieceLink?.Class)?.Color, SpanInk = _pieceSpanInk, Code = _pieceCode, Box = new Vector2(advance, _pieceSize), Lead = _pieceLink == null ? 0f : LeadOf(g) });
            _piece.Clear();

            x = _pieceX + advance + (_pieceCode ? _codePadding.Right : 0f);
            if (x > widest)
                widest = x;
        }

        /// <summary>Returns how wide the spaces at the start of the piece being built are.</summary>
        float LeadOf(IGraphics g)
        {
            var spaces = 0;
            while (spaces < _piece.Length && _piece[spaces] == ' ')
                spaces++;

            if (spaces == 0)
                return 0f;

            _spaces.Clear().Append(' ', spaces);
            return g.MeasureText(_pieceFont, _spaces, _pieceSize).X;
        }

        float HeadingSize(int level)
        {
            return (level <= 1 ? _heading1 : level == 2 ? _heading2 : _heading3);
        }

        enum Tone
        {
            Plain,
            Strong,
            Emphasis,
            Heading,
            Link
        }

        /// <summary>Words or an image, drawn in one call.</summary>
        struct Piece
        {
            public string Text;
            public Vector2 At;
            public string Font;
            public float Size;
            public Tone Tone;

            /// <summary>How wide the spaces at the start of a link's words are.</summary>
            public float Lead;

            /// <summary>Whether a picture takes the color of the link it is in, rather than its own.</summary>
            public bool Tinted;

            /// <summary>The picture an image draws, or null for words.</summary>
            public IImageSource Image;

            /// <summary>
            ///     The size an image's picture is drawn at before it is rotated, centered in <see cref="Box" />.
            /// </summary>
            public Vector2 Drawn;

            /// <summary>The clockwise rotation of an image's picture, in radians.</summary>
            public float Turn;

            /// <summary>The link the text belongs to, or null.</summary>
            public Link Link;

            /// <summary>Whether the words are code, drawn on a box of their own.</summary>
            public bool Code;

            /// <summary>
            ///     The size the piece is drawn at: an image's size, or the text's width and its line's height.
            /// </summary>
            public Vector2 Box;

            /// <summary>
            ///     The color plain text takes instead of the text's own, as a callout's look specifies, or null.
            /// </summary>
            public Color? Ink;

            /// <summary>The color a link's class gives its words, or null to leave them to their tone.</summary>
            public Color? LinkInk;

            /// <summary>The color the span the words are in gives them, or null to leave them to their tone.</summary>
            public Color? SpanInk;
        }

        /// <summary>Where one piece of a link's text is, used to find the link under the pointer.</summary>
        struct Spot
        {
            public RectangleF Where;
            public Link Link;
            public Color? Ink;
            public Color? Background;
            public float? Thickness;
            public bool Dotted;
            public bool Presses;

            /// <summary>Whether this is a picture in a link, which takes presses and no underline.</summary>
            public bool IsPicture;

            /// <summary>How far in from its start the words begin, past the spaces that join them to a picture.</summary>
            public float Lead;
        }

        /// <summary>
        ///     A rule, a bullet's mark, the rule under a table's heading row, or the bar or background of a quote or
        ///     callout; one that spans the width fills the available room from where it starts.
        /// </summary>
        struct Mark
        {
            public RectangleF Where;
            public bool Across;
            public MarkKind Kind;

            /// <summary>
            ///     The mark's own color, as a callout's look specifies, or null to use the color of its kind.
            /// </summary>
            public Color? Color;
        }

        /// <summary>The kind of a mark, which determines its color.</summary>
        enum MarkKind
        {
            Rule,
            Bullet,
            QuoteBar
        }
    }
}
