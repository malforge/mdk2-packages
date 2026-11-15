using System;
using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;
using Lumel.Input;
using Lumel.Styling;

namespace Lumel.Viewing
{
    /// <summary>
    ///     Shows as much as fits of content taller than its space, scrolled vertically with a thumb on a bar at its
    ///     side.
    /// </summary>
    public class ScrollView : ContentControl
    {
        /// <summary>The state in which the thumb is being dragged with the pointer.</summary>
        public static readonly State Dragging = State.Declare("Dragging", 400);

        readonly ScrollAxis _rail = new ScrollAxis { MinThumbLength = ScrollViewLook.NeutralMinThumbLength };

        float _scrollBarWidth = ScrollViewLook.NeutralScrollBarWidth;
        float _scrollBarGap;
        float _barRoom;
        bool _fits;

        float _heldAt;
        Element _bringing;

        /// <summary>
        ///     Gets or sets the style of this scroll view, or null to use the host theme's default scroll view style.
        /// </summary>
        public new IStyle<ScrollView> Style
        {
            get { return (IStyle<ScrollView>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this scroll view's own look, applied in every state on top of the style's look.</summary>
        public new ScrollViewLook Base => (ScrollViewLook)base.Base;

        /// <summary>Gets this scroll view's own look when hovered, applied on top of the style's look.</summary>
        public new ScrollViewLook PointerOver => (ScrollViewLook)base.PointerOver;

        /// <summary>Gets this scroll view's own look when disabled, applied on top of the style's look.</summary>
        public new ScrollViewLook Disabled => (ScrollViewLook)base.Disabled;

        /// <summary>
        ///     Gets this scroll view's own look for <paramref name="state" />, or for every state that includes it,
        ///     applied on top of the style's look.
        /// </summary>
        public new ScrollViewLook this[State state] => (ScrollViewLook)base[state];

        /// <summary>Gets the width of the scroll bar.</summary>
        /// <remarks>Set by this scroll's looks.</remarks>
        public float ScrollBarWidth
        {
            get { return _scrollBarWidth; }
            private set
            {
                if (_scrollBarWidth == value)
                    return;

                _scrollBarWidth = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the space between the content and the scroll bar, kept wherever the bar's room is kept.</summary>
        /// <remarks>Set by this scroll's looks.</remarks>
        public float ScrollBarGap
        {
            get { return _scrollBarGap; }
            private set
            {
                if (_scrollBarGap == value)
                    return;

                _scrollBarGap = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Whether it asks only for the space its content needs, up to the space given, rather than all of it.
        /// </summary>
        /// <remarks>While it does, it keeps room for the scroll bar only once its content runs past the space given.</remarks>
        public bool FitsContent
        {
            get { return _fits; }
            set
            {
                if (_fits == value)
                    return;

                _fits = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the color of the thumb.</summary>
        /// <remarks>Set by this scroll's looks.</remarks>
        public Color Thumb { get; private set; } = ScrollViewLook.NeutralThumb;

        /// <summary>Gets the minimum length of the thumb.</summary>
        /// <remarks>Set by this scroll's looks.</remarks>
        public float MinThumbLength
        {
            get { return _rail.MinThumbLength; }
            private set { _rail.MinThumbLength = value; }
        }

        /// <summary>Whether the thumb is being dragged with the pointer.</summary>
        public bool IsDragging
        {
            get { return Is(Dragging); }
            private set { Set(Dragging, value); }
        }

        /// <summary>Gets how far down the content is scrolled, in UI units.</summary>
        public float VerticalOffset => _rail.Offset;

        /// <summary>Whether any of the content is out of view.</summary>
        public bool CanScroll => _rail.CanMove;

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ScrollView>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ScrollViewLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var scroll = (ScrollViewLook)look;
            ScrollBarWidth = scroll.ScrollBarWidth ?? ScrollViewLook.NeutralScrollBarWidth;
            ScrollBarGap = scroll.ScrollBarGap ?? 0f;
            Thumb = scroll.Thumb ?? ScrollViewLook.NeutralThumb;
            MinThumbLength = scroll.MinThumbLength ?? ScrollViewLook.NeutralMinThumbLength;
        }

        bool HasBar => _scrollBarWidth > 0f && Thumb.A > 0 && _rail.CanMove;

        /// <summary>The width the bar takes from the content: the bar and the gap beside it, or nothing for no bar.</summary>
        float BarRoom => _scrollBarWidth > 0f ? _scrollBarWidth + _scrollBarGap : 0f;

        /// <summary>The bounds of the whole bar, thumb and track together.</summary>
        RectangleF Track
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X + room.Width - _scrollBarWidth, room.Y, _scrollBarWidth, room.Height);
            }
        }

        /// <summary>The bounds of the part of the track that represents the visible content.</summary>
        RectangleF ThumbBounds
        {
            get
            {
                var track = Track;
                return new RectangleF(track.X, track.Y + _rail.ThumbStart, track.Width, _rail.ThumbLength);
            }
        }

        /// <summary>Scrolls by <paramref name="amount" />, down when positive, clamped to the content.</summary>
        public void ScrollBy(float amount)
        {
            ScrollToVerticalOffset(_rail.Moved(amount));
        }

        /// <summary>Scrolls the content vertically to <paramref name="offset" />.</summary>
        /// <remarks>Kept inside what it holds at once, or, while it waits to be measured again, at the next layout, so an offset set with new content in the same frame is measured against that content.</remarks>
        public void ScrollToVerticalOffset(float offset)
        {
            var held = IsMeasureValid ? _rail.Clamp(offset) : Math.Max(0f, offset);
            if (_rail.Offset == held)
                return;

            _rail.Offset = held;
            InvalidateArrange();
        }

        /// <inheritdoc />
        /// <remarks>Takes the wheel while any of what it holds is out of sight.</remarks>
        protected override bool OnWheel(float amount)
        {
            if (!CanScroll)
                return false;

            ScrollBy(amount);
            return true;
        }

        /// <summary>
        ///     Scrolls by the minimum needed to show all of <paramref name="element" /> within the visible area, the
        ///     next time the view is placed.
        /// </summary>
        public void BringIntoView(Element element)
        {
            _bringing = element;
            InvalidateArrange();
        }

        /// <inheritdoc />
        protected override bool Hits(Vector2 point)
        {
            return HasBar && Inside(Track, point);
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            if (!Hits(point))
                return;

            if (!Inside(ThumbBounds, point))
                ScrollToVerticalOffset(_rail.ThumbedTo(point.Y, Track.Y));

            _heldAt = point.Y;
            IsDragging = true;
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            if (!IsDragging)
                return;

            ScrollToVerticalOffset(_rail.Dragged(point.Y - _heldAt));
            _heldAt = point.Y;
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            IsDragging = false;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            IsDragging = false;
        }

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>Stretches the content horizontally and scrolls it vertically from the top.</summary>
        protected override Align Aligns => Align.FillWidth | Align.Top;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Content == null)
                return _fits ? Vector2.Zero : available;

            _barRoom = _fits ? 0f : BarRoom;
            MeasureHeld(g, new Vector2(Room(available.X, _barRoom), Unbounded));
            if (!_fits)
                return available;

            if (HeldSize.Y > available.Y)
            {
                _barRoom = BarRoom;
                MeasureHeld(g, new Vector2(Room(available.X, _barRoom), Unbounded));
            }

            var wanted = HeldSize;
            return new Vector2(Math.Min(wanted.X + _barRoom, available.X), Math.Min(wanted.Y, available.Y));
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _rail.Viewport = bounds.Height;

            if (Content == null)
            {
                _rail.Extent = 0f;
                _rail.Offset = 0f;
                return;
            }

            _rail.Extent = HeldSize.Y;
            _rail.Offset = _rail.Clamp(_rail.Offset);
            Place(bounds);

            var bringing = _bringing;
            _bringing = null;
            if (bringing == null)
                return;

            // Where it stands in the content, known only once the content has been placed.
            var top = bringing.Bounds.Y - (bounds.Y - _rail.Offset);
            var offset = _rail.Offset;
            if (top < offset)
                offset = top;
            else if (top + bringing.Bounds.Height > offset + bounds.Height)
                offset = top + bringing.Bounds.Height - bounds.Height;

            offset = _rail.Clamp(offset);
            if (offset == _rail.Offset)
                return;

            _rail.Offset = offset;
            Place(bounds);
        }

        /// <summary>
        ///     Places the content at its scrolled position inside <paramref name="bounds" />, minus the bar.
        /// </summary>
        void Place(RectangleF bounds)
        {
            float x;
            float wide;
            Span(Content, true, bounds.X, Room(bounds.Width, _barRoom), Given(Content.Placement as ISized, true, float.NaN), HeldSize.X, out x, out wide);
            Content.Arrange(new RectangleF(x, bounds.Y - _rail.Offset, wide, _rail.Extent));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Content != null)
                Content.Draw(g);

            // A thumb on no groove, as the game draws its own.
            if (HasBar)
                g.Fill(ThumbBounds, Thumb);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return Content?.HitTest(point);
        }

        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }

        /// <summary>
        ///     Returns what remains of <paramref name="whole" /> after a bar <paramref name="bar" /> thick has taken
        ///     its share.
        /// </summary>
        static float Room(float whole, float bar)
        {
            var room = whole - bar;
            return room > 0f ? room : 0f;
        }
    }
}
