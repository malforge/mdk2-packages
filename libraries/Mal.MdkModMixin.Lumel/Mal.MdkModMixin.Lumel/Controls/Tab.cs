using System;
using Lumel.Drawing;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     One page of a <see cref="TabView" />: the header that shows it, with text and a badge after it, and the page
    ///     itself.
    /// </summary>
    /// <remarks>
    /// The tab is its header; the page is drawn by the view, below every header, while the tab is the selected one. Its
    /// <see cref="Element.Command" /> is its view's, which selects it.
    /// </remarks>
    public class Tab : Element, IPadded, ITextual, ITabbed
    {
        /// <summary>The state of a tab whose page is the one shown.</summary>
        public static readonly State Selected = State.Declare("Selected", 400);

        // The tab's words take no look but the one the tab gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        readonly Label _label = new Label { Style = Bare };
        readonly Collection _parts;
        Thickness _padding;
        Element _badge;
        Element _content;
        Action _contentChanged;

        /// <summary>
        ///     Creates a tab with no text and no page, using the look its host's theme holds for a tab until it is
        ///     given a style.
        /// </summary>
        public Tab()
        {
            _parts = CreateChildren();
            _parts.Add(_label);
            Font = VRage.Game.MyFontEnum.White;
            FontSize = TextLook.NeutralFontSize;
            Color = Color.White;
            ContentAlign = Align.Center;
        }

        /// <summary>
        ///     Gets or sets the style of this tab, or null to use the default its host's theme holds for a tab.
        /// </summary>
        public new IStyle<Tab> Style
        {
            get { return (IStyle<Tab>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>The look this tab sets itself for every state, applied over its style's.</summary>
        public new TabLook Base => (TabLook)base.Base;

        /// <summary>The look this tab sets itself for when the pointer is over it, applied over its style's.</summary>
        public new TabLook PointerOver => (TabLook)base.PointerOver;

        /// <summary>The look this tab sets itself for when it is disabled, applied over its style's.</summary>
        public new TabLook Disabled => (TabLook)base.Disabled;

        /// <summary>
        ///     The look this tab sets itself for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style's.
        /// </summary>
        public new TabLook this[State state] => (TabLook)base[state];

        /// <summary>Gets or sets the text on the header.</summary>
        public string Header
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        /// <summary>
        ///     Gets or sets the element shown after the header text, such as a mark that something on the page needs
        ///     attention, or null for none.
        /// </summary>
        /// <remarks>Its own padding holds it apart from the words.</remarks>
        public Element Badge
        {
            get { return _badge; }
            set
            {
                if (ReferenceEquals(_badge, value))
                    return;

                if (_badge != null)
                    _parts.Remove(_badge);

                _badge = value;
                if (value != null)
                    _parts.Add(value);

                InvalidateMeasure();
            }
        }

        /// <summary>Gets or sets the page the tab shows.</summary>
        public Element Content
        {
            get { return _content; }
            set
            {
                if (ReferenceEquals(_content, value))
                    return;

                _content = value;
                _contentChanged?.Invoke();
            }
        }

        /// <summary>Whether the tab's page is the one shown.</summary>
        /// <remarks>Set by the view the tab is in, through <see cref="ITabbed" />. While it is, the tab shows its looks for <see cref="Selected" />.</remarks>
        public bool IsSelected => Is(Selected);

        /// <inheritdoc />
        bool ITabbed.IsSelected
        {
            set { Set(Selected, value); }
        }

        /// <inheritdoc />
        Action ITabbed.ContentChanged
        {
            set { _contentChanged = value; }
        }

        /// <summary>The font the header text is drawn in.</summary>
        /// <remarks>Set by this tab's looks.</remarks>
        public string Font { get; private set; }

        /// <summary>The height of a line of the header text, in UI units.</summary>
        /// <remarks>Set by this tab's looks.</remarks>
        public float FontSize { get; private set; }

        /// <summary>The color the header text is drawn in.</summary>
        /// <remarks>Set by this tab's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>How the text and badge are aligned across the header: left, right or centered.</summary>
        /// <remarks>Set by this tab's looks.</remarks>
        public Align ContentAlign { get; private set; }

        /// <summary>The space kept clear between the edge and the text.</summary>
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
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Tab>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TabLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var tab = (TabLook)look;
            Font = tab.Font ?? VRage.Game.MyFontEnum.White;
            FontSize = tab.FontSize ?? TextLook.NeutralFontSize;
            Color = tab.Color ?? Color.White;

            var align = tab.ContentAlign ?? Align.Center;
            if (align != ContentAlign)
            {
                ContentAlign = align;
                InvalidateArrange();
            }

            var words = _label.Base;
            if (words.Font != Font)
                words.Font = Font;

            if (words.FontSize != FontSize)
                words.FontSize = FontSize;

            if (words.Color != Color)
                words.Color = Color;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _label.Measure(g, available);
            var size = _label.DesiredSize;
            if (_badge == null)
                return size;

            // Measured hidden too, so the change when it is shown reaches the header, as a change only rises from what was measured.
            _badge.Measure(g, available);
            return new Vector2(size.X + _badge.DesiredSize.X, MathHelper.Max(size.Y, _badge.DesiredSize.Y));
        }

        /// <summary>
        ///     Arranges the text and the badge after it across the header's space according to
        ///     <see cref="ContentAlign" />, centered vertically.
        /// </summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var marked = _badge != null && _badge.IsVisible;
            var badgeWidth = marked ? _badge.DesiredSize.X : 0f;
            var width = MathHelper.Min(_label.DesiredSize.X + badgeWidth, bounds.Width);
            var edges = ContentAlign & Align.FillWidth;
            var x = edges == Align.Left ? bounds.X
                : edges == Align.Right ? bounds.X + bounds.Width - width
                : bounds.X + (bounds.Width - width) * 0.5f;
            var words = MathHelper.Max(0f, width - badgeWidth);
            _label.Arrange(new RectangleF(x, bounds.Y, words, bounds.Height));
            if (marked)
                _badge.Arrange(new RectangleF(x + words, bounds.Y + (bounds.Height - _badge.DesiredSize.Y) * 0.5f, badgeWidth, _badge.DesiredSize.Y));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _label.Draw(g);
            if (_badge != null)
                _badge.Draw(g);
        }
    }
}
