using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel
{
    /// <summary>
    ///     A tip shown flush against the element it explains, which the pointer can move onto; it shows pages of rich
    ///     text that links navigate between, with back and forward steps, or, where its codex says so, opens each link's
    ///     page in a separate tip beside it.
    /// </summary>
    /// <remarks>
    ///     What its links lead to and how it is pinned come from its <see cref="Lumel.Codex" />: the one it is made with, or
    ///     else the one it finds among the resources above its element as it opens.
    /// </remarks>
    public partial class CodexTip : Tip
    {
        /// <summary>The state of a tip and of its page's text while <see cref="IsPinned" /> is true.</summary>
        /// <remarks>Follows <see cref="IsPinned" /> at each draw, so a look answering it changes with the tip open.</remarks>
        public static readonly State Pinned = State.Declare("CodexTipPinned", 300);

        readonly List<Tip> _pages = new List<Tip>();
        readonly Codex _given;
        Codex _codex;

        int _at = -1;
        TipSide _side;
        bool _sided;
        int _left = -1;
        CodexTip _next;
        CodexTip _above;
        Element _opener;
        Tip _restingPage;
        int _restingSince;
        float _maxHeight = float.PositiveInfinity;

        /// <summary>
        ///     Creates a tip from <paramref name="markdown" />, written in markdown-lite, in the codex it finds as it opens.
        /// </summary>
        public CodexTip(string markdown) : base(markdown)
        {
        }

        /// <summary>
        ///     Creates a tip from <paramref name="template" />, markdown-lite with {slots} filled from
        ///     <paramref name="arguments" />, in the codex it finds as it opens.
        /// </summary>
        public CodexTip(string template, Arguments arguments) : base(template, arguments)
        {
        }

        /// <summary>Creates a tip from a <paramref name="document" /> built in code, in the codex it finds as it opens.</summary>
        public CodexTip(Document document) : base(document)
        {
        }

        /// <summary>Creates a tip in <paramref name="codex" /> showing its page <paramref name="id" />.</summary>
        /// <remarks>Throws where the codex has no page <paramref name="id" />.</remarks>
        public CodexTip(Codex codex, string id) : this(codex, PageOf(codex, id))
        {
        }

        /// <summary>Creates a tip in <paramref name="codex" /> showing <paramref name="page" /> first.</summary>
        public CodexTip(Codex codex, Tip page) : base(Required(page).Text, page.Document, page.Arguments)
        {
            if (codex == null)
                throw new ArgumentNullException(nameof(codex));

            _given = codex;
            _codex = codex;
        }

        /// <summary>
        ///     Gets or sets the style of this tip, or null to use the default from the host's theme for a codex tip.
        /// </summary>
        public new IStyle<CodexTip> Style
        {
            get { return (IStyle<CodexTip>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this tip's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new CodexTipLook Base => (CodexTipLook)base.Base;

        /// <summary>Gets this tip's own look applied on hover, layered over the look from its style.</summary>
        public new CodexTipLook PointerOver => (CodexTipLook)base.PointerOver;

        /// <summary>Gets this tip's own look applied when disabled, layered over the look from its style.</summary>
        public new CodexTipLook Disabled => (CodexTipLook)base.Disabled;

        /// <summary>
        ///     Gets this tip's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new CodexTipLook this[State state] => (CodexTipLook)base[state];

        /// <summary>
        ///     Gets the codex the tip belongs to: the one it was made with, or else the one it found as it last opened, or null
        ///     until then.
        /// </summary>
        public Codex Codex => _codex;

        /// <summary>
        ///     Gets or sets the key the tip finds its codex under among the resources above its element, or null for the
        ///     default, the full name of <see cref="Lumel.Codex" />.
        /// </summary>
        /// <remarks>Not used by a tip made with a codex.</remarks>
        public string CodexKey { get; set; }

        /// <summary>Gets the tip opened beside this one by a link in it, or null if none is open.</summary>
        public CodexTip NestedTip => _next != null && _next.IsOpen ? _next : null;

        /// <summary>
        ///     Whether the tip is open and holds the pointer, because it is either always pinned or currently pinned by
        ///     the host's pin key.
        /// </summary>
        public bool IsPinned => IsOpen && (!_codex.IsPinnedByKey || IsPinKeyHeld);

        /// <summary>Gets the maximum height of the tip in UI units; a taller page is cut off at this height.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public float MaxHeight => _maxHeight;

        /// <summary>Gets the space between the row of steps, the page's text and the pin hint.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public float Spacing { get; private set; }

        /// <summary>
        ///     Gets the style of the back and forward steps, or null to use the default from the host's theme.
        /// </summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public IStyle<Glyph> StepStyle { get; private set; }

        /// <summary>Gets the symbol of the step that goes back to the previous page.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public string BackSymbol { get; private set; }

        /// <summary>Gets the symbol of the step that goes forward to the page that was stepped back from.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public string ForwardSymbol { get; private set; }

        /// <summary>
        ///     Gets how far a tip beside another stands along the edge they share from that tip's corner: down on its left or
        ///     right, and to the right above or below it.
        /// </summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public float Stagger { get; private set; }

        /// <summary>Gets how long the tip stays open after the pointer leaves both it and its element, in frames, while no pin key holds it.</summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public int CloseDelay { get; private set; }

        /// <summary>
        ///     Gets how long the pointer rests on a link before its page replaces the one open beside this tip, in frames.
        /// </summary>
        /// <remarks>Set by this tip's looks.</remarks>
        public int ReplaceDelay { get; private set; }

        /// <summary>Gets the page being shown, or null while the tip is closed.</summary>
        public Tip Page => _at >= 0 ? _pages[_at] : null;

        /// <summary>
        ///     Gets the side of its element the tip is shown on, chosen when the tip is first drawn after opening and
        ///     kept while its pages change.
        /// </summary>
        public TipSide Side => _side;

        /// <summary>Whether there is a page before the current one to go back to.</summary>
        public bool CanGoBack => _at > 0;

        /// <summary>Whether there is a page that was stepped back from to go forward to.</summary>
        public bool CanGoForward => _at >= 0 && _at < _pages.Count - 1;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <inheritdoc />
        /// <remarks>Opens on its own page, with no pages to go back or forward to.</remarks>
        protected override void Open(Element owner, Vector2 pointer)
        {
            OpenOn(owner, this, pointer);
        }

        /// <inheritdoc />
        /// <remarks>Closes any tip opened beside it too.</remarks>
        protected override void Close()
        {
            if (!IsOpen)
                return;

            _next?.Close();
            _pages.Clear();
            _at = -1;
            base.Close();
        }

        /// <summary>
        ///     Shows <paramref name="page" /> and discards the pages that were stepped back from; does nothing while
        ///     the tip is closed.
        /// </summary>
        public void Navigate(Tip page)
        {
            if (!IsOpen || page == null)
                return;

            _pages.RemoveRange(_at + 1, _pages.Count - _at - 1);
            _pages.Add(page);
            _at++;
            Show();
        }

        /// <summary>
        ///     Shows the page that the link target <paramref name="id" /> leads to, and returns true if the codex found
        ///     one.
        /// </summary>
        public bool Follow(string id)
        {
            if (!IsOpen)
                return false;

            var page = _codex.Page(id);
            if (page == null)
                return false;

            Navigate(page);
            return true;
        }

        /// <summary>Shows the previous page, if there is one.</summary>
        public void GoBack()
        {
            if (!CanGoBack)
                return;

            _at--;
            Show();
        }

        /// <summary>Shows the page that was stepped back from, if there is one.</summary>
        public void GoForward()
        {
            if (!CanGoForward)
                return;

            _at++;
            Show();
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Stays wherever the pointer goes while the pin key is held. Otherwise stays while the pointer is on it or on its
        ///     element, and for <see cref="CloseDelay" /> after the pointer has left both, where it holds the pointer; gives
        ///     way to another tip its element offers. A tip beside it stays as long as it does, and closes with the pin key's
        ///     release.
        /// </remarks>
        protected override bool StaysOpen(Element over, Tip offered, Vector2 pointer, int now)
        {
            if (!Owner.IsLoaded)
                return false;

            if (Holding(pointer, now) || _codex.IsPinnedByKey && IsPinKeyHeld)
                return true;

            if (Owner.Reaches(pointer))
            {
                _left = -1;
                if (!IsPinned)
                    _next?.Close();

                return over != Owner || offered == null || offered == this;
            }

            return Lingers(now);
        }

        /// <inheritdoc />
        /// <remarks>Covers itself and the tip opened beside it, while it holds the pointer.</remarks>
        public override bool Covers(Vector2 point)
        {
            return Own(point) || NestedTip != null && _next.Covers(point);
        }

        /// <inheritdoc />
        /// <remarks>Reaches into the tip opened beside this one as well.</remarks>
        public override Element HitTest(Vector2 point)
        {
            return NestedTip != null && _next.Covers(point) ? _next.HitTest(point) : base.HitTest(point);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     As wide as its placement says where it says, and otherwise as it asks, up to <see cref="Tip.MaxWidth" />; flush
        ///     against its element on the side chosen as it opened, and the tip beside it after it.
        /// </remarks>
        protected override void Present(IGraphics g, RectangleF area)
        {
            if (!IsOpen)
                return;

            var pinned = IsPinned;
            Set(Pinned, pinned);
            Body.Set(Pinned, pinned);

            var sized = Placement as ISized;
            var fixedWidth = sized != null && !float.IsNaN(sized.Width);

            Measure(g, new Vector2(fixedWidth ? sized.Width : area.Width, area.Height));
            var wide = fixedWidth ? sized.Width : Math.Min(DesiredSize.X, area.Width);
            var size = new Vector2(wide, Math.Min(DesiredSize.Y, area.Height));

            Arrange(Place(area, size));
            DrawOn(g);

            _next?.Present(g, area);
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<CodexTip>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new CodexTipLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var tip = (CodexTipLook)look;
            var maxHeight = tip.MaxHeight ?? float.PositiveInfinity;
            if (maxHeight != _maxHeight)
            {
                _maxHeight = maxHeight;
                InvalidateMeasure();
            }

            Spacing = tip.Spacing ?? 0f;
            StepStyle = tip.StepStyle;
            BackSymbol = tip.BackSymbol;
            ForwardSymbol = tip.ForwardSymbol;
            CloseDelay = tip.CloseDelay ?? 0;
            ReplaceDelay = tip.ReplaceDelay ?? ShowDelay;
            Stagger = tip.Stagger ?? 0f;

            if (_column.Base.Spacing != Spacing)
                _column.Base.Spacing = Spacing;

            _pinHint.Style = BodyStyle;
            _back.Style = StepStyle;
            _forward.Style = StepStyle;
            _back.Symbol = BackSymbol;
            _forward.Symbol = ForwardSymbol;

            if (_next != null)
                PassLookTo(_next);
        }

        /// <inheritdoc />
        /// <remarks>Never taller than <see cref="MaxHeight" />, padding included.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var most = Math.Max(0f, _maxHeight - Padding.Vertical);
            var size = base.MeasureContent(g, new Vector2(available.X, Math.Min(available.Y, most)));
            return new Vector2(size.X, Math.Min(size.Y, most));
        }

        /// <summary>
        ///     Returns the codex tip for <paramref name="markdown" />: <paramref name="current" /> if it is a codex tip that
        ///     already has that content, a new tip otherwise, or null if there is nothing to show.
        /// </summary>
        /// <remarks>For a view that sets its tip on every refresh: the words are parsed only when they change.</remarks>
        public new static CodexTip For(Tip current, string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return null;

            var codex = current as CodexTip;
            return codex != null && codex.Arguments == null && codex.Text == markdown ? codex : new CodexTip(markdown);
        }

        /// <summary>
        ///     Returns the codex tip for <paramref name="template" /> with its slots filled from <paramref name="arguments" />:
        ///     <paramref name="current" /> if it is a codex tip that already has that content, a new tip otherwise, or null if
        ///     there is nothing to show.
        /// </summary>
        public new static CodexTip For(Tip current, string template, Arguments arguments)
        {
            if (string.IsNullOrEmpty(template))
                return null;

            var codex = current as CodexTip;
            return codex != null && codex.Arguments == arguments && codex.Text == template ? codex : new CodexTip(template, arguments);
        }

        /// <summary>Returns <paramref name="page" />, or throws where there is none.</summary>
        static Tip Required(Tip page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            return page;
        }

        /// <summary>Returns the page <paramref name="id" /> of <paramref name="codex" />, or throws where it has none.</summary>
        static Tip PageOf(Codex codex, string id)
        {
            if (codex == null)
                throw new ArgumentNullException(nameof(codex));

            var page = codex.Page(id);
            if (page == null)
                throw new ArgumentException("The codex has no page " + id + ".", nameof(id));

            return page;
        }

        /// <summary>
        ///     Opens the tip beside <paramref name="anchor" /> showing <paramref name="page" />, with no pages to go back or
        ///     forward to.
        /// </summary>
        /// <remarks>Finds its codex among the resources above <paramref name="anchor" />, unless it was made with one; throws where there is none.</remarks>
        void OpenOn(Element anchor, Tip page, Vector2 pointer)
        {
            base.Open(anchor, pointer);
            _codex = _given ?? anchor.FindResource<Codex>(CodexKey ?? ResourceDictionary.KeyOf<Codex>());
            _sided = false;
            _left = -1;
            _pages.Clear();
            _pages.Add(page);
            _at = 0;
            Show();
        }

        /// <summary>Gets whether the host's pin key is held.</summary>
        bool IsPinKeyHeld => _codex.FnIsPinKeyHeld != null && _codex.FnIsPinKeyHeld();

        /// <summary>Returns true if <paramref name="point" /> is on this tip itself while it holds the pointer.</summary>
        bool Own(Vector2 point)
        {
            return IsPinned && Holds(point);
        }

        /// <summary>
        ///     Returns true if the pointer is on this tip or on a tip opened beside it, which keeps this tip open; where the
        ///     codex opens nested tips, it also tells the nested tip which link's page the pointer rests on.
        /// </summary>
        bool Holding(Vector2 pointer, int now)
        {
            if (NestedTip != null && _next.Covers(pointer))
            {
                _left = -1;
                _next.Track(null, pointer, now);
                return true;
            }

            if (!Own(pointer))
            {
                if (_next != null)
                    _next._restingPage = null;

                return false;
            }

            _left = -1;
            if (_codex.OpensNestedTips)
                Next().Track(PageAt(pointer), pointer, now);

            return true;
        }

        /// <summary>
        ///     Returns true if the tip stays open after the pointer has left it and its element: only where it holds the
        ///     pointer, and until <see cref="CloseDelay" /> has passed.
        /// </summary>
        bool Lingers(int now)
        {
            if (!IsPinned)
                return false;

            if (_left < 0)
                _left = now;

            return now - _left < CloseDelay;
        }

        /// <summary>
        ///     Shows <paramref name="page" /> in this nested tip once the pointer has rested on its link in the previous tip,
        ///     for <see cref="Tip.ShowDelay" /> where the tip is closed and <see cref="ReplaceDelay" /> where it is open,
        ///     closing the tips beside it; <paramref name="page" /> is null where the pointer rests on no link there.
        /// </summary>
        void Track(Tip page, Vector2 pointer, int now)
        {
            if (page != _restingPage)
            {
                _restingPage = page;
                _restingSince = now;
            }

            if (IsOpen && Holding(pointer, now))
                return;

            if (page == null || page == Page || now - _restingSince < (IsOpen ? ReplaceDelay : ShowDelay))
                return;

            _next?.Close();
            OpenOn(_opener, page, pointer);
        }

        void FollowLink(Link link)
        {
            if (!_codex.OpensNestedTips)
                Follow(link.Target);
        }

        /// <summary>
        ///     Returns the page of the link at <paramref name="pointer" />, in screen coordinates, or null if there is
        ///     none.
        /// </summary>
        Tip PageAt(Vector2 pointer)
        {
            var link = Body.LinkAt(Body.FromScreen(pointer));
            return link == null ? null : _codex.Page(link.Target);
        }

        /// <summary>
        ///     Returns the tip that this tip's links open beside it, creating it on first use and styling it like this
        ///     one.
        /// </summary>
        CodexTip Next()
        {
            if (_next != null)
                return _next;

            _next = new CodexTip(_codex, Page) { Placement = Placement };
            _next._opener = Body;
            _next._above = this;
            PassLookTo(_next);

            // Its delay is a look, read before it is ever measured.
            _next.Restyle();
            return _next;
        }

        /// <summary>
        ///     Copies this tip's style, own looks and host theme to <paramref name="next" />, the tip beside this one.
        /// </summary>
        void PassLookTo(CodexTip next)
        {
            next.Style = Style;
            CopyOwnLooksTo(next);
            ((IThemed)next).UseTheme(HostTheme);
        }

        /// <summary>
        ///     Returns true if <paramref name="link" /> resolves to a page shown by this tip or a tip it stands beside, where
        ///     the codex opens nested tips.
        /// </summary>
        bool LeadsToAnOpenPage(Link link)
        {
            if (!_codex.OpensNestedTips)
                return false;

            var page = _codex.Page(link.Target);
            if (page == null)
                return false;

            for (var tip = this; tip != null; tip = tip._above)
            {
                if (IsSamePage(tip.Page, page))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if <paramref name="first" /> and <paramref name="second" /> show the same thing: they are one tip,
        ///     hold one document, or were made from the same text and arguments.
        /// </summary>
        static bool IsSamePage(Tip first, Tip second)
        {
            if (first == second || first.Document != null && first.Document == second.Document)
                return true;

            return !string.IsNullOrEmpty(first.Text) && first.Text == second.Text && first.Arguments == second.Arguments;
        }

        /// <summary>Returns true if <paramref name="page" /> has a link that leads to no page already open.</summary>
        bool HasLinkToShow(Tip page)
        {
            if (page.Document == null)
                return false;

            var links = page.Document.Links;
            for (var i = 0; i < links.Count; i++)
            {
                if (!LeadsToAnOpenPage(links[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns the bounds of the tip for <paramref name="size" />, flush against its element on the chosen side
        ///     and inside <paramref name="area" />.
        /// </summary>
        RectangleF Place(RectangleF area, Vector2 size)
        {
            var beside = _above ?? Owner;
            var bounds = beside.Bounds;
            var from = beside.ToScreen(new Vector2(bounds.X, bounds.Y));
            var to = beside.ToScreen(new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height));
            var step = _above != null ? Stagger : 0f;

            if (!_sided)
            {
                _side = to.X + size.X <= area.X + area.Width ? TipSide.Right
                    : from.X - size.X >= area.X ? TipSide.Left
                    : from.Y - size.Y >= area.Y ? TipSide.Above
                    : TipSide.Below;
                _sided = true;
            }

            float x;
            float y;
            switch (_side)
            {
                case TipSide.Right:
                    x = to.X;
                    y = from.Y + step;
                    break;
                case TipSide.Left:
                    x = from.X - size.X;
                    y = from.Y + step;
                    break;
                case TipSide.Above:
                    x = from.X + step;
                    y = from.Y - size.Y;
                    break;
                default:
                    x = from.X + step;
                    y = to.Y;
                    break;
            }

            x = Math.Max(area.X, Math.Min(x, area.X + area.Width - size.X));
            y = Math.Max(area.Y, Math.Min(y, area.Y + area.Height - size.Y));
            return new RectangleF(x, y, size.X, size.Y);
        }

        /// <summary>
        ///     Puts the current page into the body, shows the row of steps only when there is a page to go to, and the pin
        ///     hint while the codex pins by key and a link on the page can show something new.
        /// </summary>
        void Show()
        {
            var page = _pages[_at];
            Body.Document = page.Document;
            Body.Arguments = page.Arguments;
            _back.IsVisible = CanGoBack;
            _forward.IsVisible = CanGoForward;
            _steps.IsVisible = CanGoBack || CanGoForward;
            _pinHint.Document = _codex.PinHint;
            Body.FnIsLinkPlain = LeadsToAnOpenPage;
            _pinHint.IsVisible = _codex.IsPinnedByKey && _codex.PinHint != null && HasLinkToShow(page);
        }
    }
}
