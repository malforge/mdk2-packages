using System;
using Lumel.Drawing;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;

namespace Lumel
{
    /// <summary>
    ///     A box of text shown by the pointer once it has rested on an element for <see cref="ShowDelay" />, which the
    ///     pointer passes through and which closes as soon as the pointer leaves the element.
    /// </summary>
    /// <remarks>
    ///     An element carries one in <see cref="Element.Tip" />, and its workspace opens, keeps and draws it. The text is
    ///     parsed when the tip is made, so a tip is made once and kept.
    /// </remarks>
    public class Tip : Element, IPadded, ITipLifetime
    {
        // Where the tip sits from the pointer it opened at, clear of the game's cursor.
        const float FromPointer = 16f;

        readonly RichText _body = new RichText();
        readonly Element _content;
        Thickness _padding;
        float _maxWidth = float.PositiveInfinity;
        Element _owner;
        Vector2 _openedAt;

        /// <summary>Creates a tip from <paramref name="markdown" />, written in markdown-lite.</summary>
        public Tip(string markdown) : this(markdown, Markdown.Parse(markdown ?? ""), null)
        {
        }

        /// <summary>
        ///     Creates a tip from <paramref name="template" />, markdown-lite with {slots}, whose slots are filled from
        ///     <paramref name="arguments" />.
        /// </summary>
        public Tip(string template, Arguments arguments) : this(template, Template.Parse(template ?? ""), arguments)
        {
        }

        /// <summary>Creates a tip from a <paramref name="document" /> built in code.</summary>
        public Tip(Document document) : this(null, document, null)
        {
        }

        /// <summary>
        ///     Creates a tip showing <paramref name="document" />, written as <paramref name="text" /> where it came from
        ///     markdown-lite, with <paramref name="arguments" /> for its slots where it has any.
        /// </summary>
        protected Tip(string text, Document document, Arguments arguments)
        {
            Text = text;
            Document = document;
            Arguments = arguments;
            _body.Document = document;
            _body.Arguments = arguments;

            // Called while the tip is made, by design: a derived tip only builds its parts here, from nothing its own
            // constructor sets.
            // ReSharper disable once VirtualMemberCallInConstructor
            _content = CreateContent(_body);
            CreateChildren().Add(_content);
            IsVisible = false;
        }

        /// <summary>Gets or sets the style of this tip, or null to use the default its host's theme holds for a tip.</summary>
        public new IStyle<Tip> Style
        {
            get { return (IStyle<Tip>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this tip's own look, applied in every state on top of the style's look.</summary>
        public new TipLook Base => (TipLook)base.Base;

        /// <summary>Gets this tip's own look when hovered, applied on top of the style's look.</summary>
        public new TipLook PointerOver => (TipLook)base.PointerOver;

        /// <summary>Gets this tip's own look when disabled, applied on top of the style's look.</summary>
        public new TipLook Disabled => (TipLook)base.Disabled;

        /// <summary>
        ///     Gets this tip's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of the style's look.
        /// </summary>
        public new TipLook this[State state] => (TipLook)base[state];

        /// <summary>Gets the markdown-lite the tip was created from, or null for a tip built in code.</summary>
        public string Text { get; }

        /// <summary>Gets the content of the tip.</summary>
        public Document Document { get; }

        /// <summary>Gets the arguments for the slots in <see cref="Document" />, or null if it has none.</summary>
        public Arguments Arguments { get; }

        /// <summary>Gets the shadow the tip casts on what lies beneath it, or null for none.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public Shadow Shadow { get; private set; }

        /// <summary>Gets the widest the tip grows in UI units before its text wraps.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public float MaxWidth => _maxWidth;

        /// <summary>Gets how long the pointer must rest on an element before its tip opens, in frames.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public int ShowDelay { get; private set; }

        /// <summary>Gets the style of the tip's text, or null to use the default from the host's theme.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public IStyle<RichText> BodyStyle { get; private set; }

        /// <summary>Gets the space between the tip's edge and its text.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <summary>Gets the element the tip is open on, or null while it is closed.</summary>
        public Element Owner => _owner;

        /// <summary>Whether the tip is open on an element.</summary>
        public bool IsOpen => _owner != null;

        /// <summary>Gets the text the tip shows, for a derived tip that shows more than one page in it.</summary>
        protected RichText Body => _body;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <summary>
        ///     Returns the tip for <paramref name="markdown" />: <paramref name="current" /> if it already has that
        ///     content, a new tip otherwise, or null if there is nothing to show.
        /// </summary>
        /// <remarks>For a view that sets its tip on every refresh: the words are parsed only when they change.</remarks>
        public static Tip For(Tip current, string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return null;

            return current != null && current.Arguments == null && current.Text == markdown ? current : new Tip(markdown);
        }

        /// <summary>
        ///     Returns the tip for <paramref name="template" /> with its slots filled from
        ///     <paramref name="arguments" />: <paramref name="current" /> if it already has that content, a new tip
        ///     otherwise, or null if there is nothing to show.
        /// </summary>
        public static Tip For(Tip current, string template, Arguments arguments)
        {
            if (string.IsNullOrEmpty(template))
                return null;

            return current != null && current.Arguments == arguments && current.Text == template
                ? current
                : new Tip(template, arguments);
        }

        /// <inheritdoc />
        void ITipLifetime.Open(Element owner, Vector2 pointer)
        {
            Open(owner, pointer);
        }

        /// <inheritdoc />
        void ITipLifetime.Close()
        {
            Close();
        }

        /// <inheritdoc />
        bool ITipLifetime.StaysOpen(Element over, Tip offered, Vector2 pointer, int now)
        {
            return StaysOpen(over, offered, pointer, now);
        }

        /// <inheritdoc />
        void ITipLifetime.Present(IGraphics g, RectangleF area)
        {
            Present(g, area);
        }

        /// <summary>Opens the tip on <paramref name="owner" />, which the pointer rests on at <paramref name="pointer" />.</summary>
        /// <remarks>Called by the workspace once the pointer has rested on the element for <see cref="ShowDelay" />.</remarks>
        protected virtual void Open(Element owner, Vector2 pointer)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            _owner = owner;
            _openedAt = pointer;
            IsVisible = true;
            ((IShown)this).IsShown = true;
        }

        /// <summary>Closes the tip, if it is open.</summary>
        protected virtual void Close()
        {
            if (_owner == null)
                return;

            _owner = null;
            IsVisible = false;
            ((IShown)this).IsShown = false;
        }

        /// <summary>
        ///     Returns true if the tip stays open, where <paramref name="over" /> is the element with a tip that the pointer
        ///     rests on at <paramref name="pointer" />, <paramref name="offered" /> is the tip it offers there, and
        ///     <paramref name="now" /> is the frame.
        /// </summary>
        /// <remarks>Asked by the workspace on every frame the tip is open; it stays while the pointer rests on its element and offers it.</remarks>
        protected virtual bool StaysOpen(Element over, Tip offered, Vector2 pointer, int now)
        {
            return over == _owner && offered == this;
        }

        /// <summary>Returns true if the tip holds the pointer at <paramref name="point" />, so the pointer reaches it and nothing beneath.</summary>
        /// <remarks>The pointer passes through a tip.</remarks>
        public virtual bool Covers(Vector2 point)
        {
            return false;
        }

        /// <summary>Lays the tip out within <paramref name="area" /> and draws it with <paramref name="g" />.</summary>
        /// <remarks>Below and to the right of where the pointer was as it opened, or turned away from an edge it would cross.</remarks>
        protected virtual void Present(IGraphics g, RectangleF area)
        {
            Measure(g, new Vector2(area.Width, area.Height));
            var size = new Vector2(Math.Min(DesiredSize.X, area.Width), Math.Min(DesiredSize.Y, area.Height));

            var x = _openedAt.X + FromPointer;
            if (x + size.X > area.X + area.Width)
                x = _openedAt.X - FromPointer - size.X;

            var y = _openedAt.Y + FromPointer;
            if (y + size.Y > area.Y + area.Height)
                y = _openedAt.Y - FromPointer - size.Y;

            x = Math.Max(area.X, Math.Min(x, area.X + area.Width - size.X));
            y = Math.Max(area.Y, Math.Min(y, area.Y + area.Height - size.Y));
            Arrange(new RectangleF(x, y, size.X, size.Y));
            DrawOn(g);
        }

        /// <summary>Draws the tip where it was last arranged, over its shadow.</summary>
        protected void DrawOn(IGraphics g)
        {
            if (Shadow != null)
                Shadow.Draw(g, Bounds);

            Draw(g);
        }

        /// <summary>Returns what the tip holds, built around <paramref name="body" />, its text.</summary>
        /// <remarks>Called once, as the tip is made; a derived tip that shows more than its text builds its parts here.</remarks>
        protected virtual Element CreateContent(RichText body)
        {
            return body;
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Tip>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TipLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var tip = (TipLook)look;
            Shadow = tip.Shadow;
            ShowDelay = tip.ShowDelay ?? 0;
            BodyStyle = tip.BodyStyle;
            _body.Style = BodyStyle;

            var maxWidth = tip.MaxWidth ?? float.PositiveInfinity;
            if (maxWidth == _maxWidth)
                return;

            _maxWidth = maxWidth;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Never wider than <see cref="MaxWidth" />, padding included.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var widest = Math.Max(0f, _maxWidth - _padding.Horizontal);
            _content.Measure(g, new Vector2(Math.Min(available.X, widest), available.Y));
            return _content.DesiredSize;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _content.Arrange(bounds);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _content.Draw(g);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return _content.HitTest(point);
        }
    }
}
