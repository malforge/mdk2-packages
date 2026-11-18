using System;
using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;
using Lumel.Input;
using Lumel.Styling;

namespace Lumel.Viewing
{
    /// <summary>
    ///     ShowsBars as much as fits of content larger than its space, scrolled vertically with a thumb on a bar at its side,
    ///     horizontally with one on a bar along its foot, or both.
    /// </summary>
    public class ScrollView : ContentControl
    {
        /// <summary>The state in which a thumb is being dragged with the pointer.</summary>
        public static readonly State Dragging = State.Declare("Dragging", 400);

        readonly ScrollAxis _down = new ScrollAxis { MinThumbLength = ScrollViewLook.NeutralMinThumbLength };
        readonly ScrollAxis _across = new ScrollAxis { MinThumbLength = ScrollViewLook.NeutralMinThumbLength };

        float _scrollBarWidth = ScrollViewLook.NeutralScrollBarWidth;
        float _scrollBarGap;
        bool _scrollsDown = true;
        bool _scrollsAcross;
        float _downBarRoom;
        float _acrossBarRoom;
        bool _fits;

        bool _draggingAcross;
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

        /// <summary>Gets the thickness of each scroll bar, across it.</summary>
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

        /// <summary>Gets the space between the content and each scroll bar, kept wherever the bar's room is kept.</summary>
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

        /// <summary>Gets or sets whether the content scrolls vertically, which it does by default.</summary>
        /// <remarks>Content that does not scroll vertically is given the view's height, and placed in it by its alignment.</remarks>
        public bool ScrollsVertically
        {
            get { return _scrollsDown; }
            set
            {
                if (_scrollsDown == value)
                    return;

                _scrollsDown = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets or sets whether the content scrolls horizontally, which it does not by default.</summary>
        /// <remarks>Content that does not scroll horizontally is given the view's width, and placed in it by its alignment.</remarks>
        public bool ScrollsHorizontally
        {
            get { return _scrollsAcross; }
            set
            {
                if (_scrollsAcross == value)
                    return;

                _scrollsAcross = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Whether it asks only for the space its content needs, up to the space given, rather than all of it.
        /// </summary>
        /// <remarks>While it does, it keeps room for a scroll bar only once its content runs past the space given that way.</remarks>
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

        /// <summary>Gets the color of the thumbs.</summary>
        /// <remarks>Set by this scroll's looks.</remarks>
        public Color Thumb { get; private set; } = ScrollViewLook.NeutralThumb;

        /// <summary>Gets the minimum length of a thumb.</summary>
        /// <remarks>Set by this scroll's looks.</remarks>
        public float MinThumbLength
        {
            get { return _down.MinThumbLength; }
            private set
            {
                _down.MinThumbLength = value;
                _across.MinThumbLength = value;
            }
        }

        /// <summary>Whether a thumb is being dragged with the pointer.</summary>
        public bool IsDragging
        {
            get { return Is(Dragging); }
            private set { Set(Dragging, value); }
        }

        /// <summary>Gets how far down the content is scrolled, in UI units.</summary>
        public float VerticalOffset => _down.Offset;

        /// <summary>Gets how far across the content is scrolled, in UI units.</summary>
        public float HorizontalOffset => _across.Offset;

        /// <summary>Whether it scrolls vertically and any of the content is out of view above or below.</summary>
        public bool CanScroll => _scrollsDown && _down.CanMove;

        /// <summary>Whether it scrolls horizontally and any of the content is out of view to the left or right.</summary>
        public bool CanScrollHorizontally => _scrollsAcross && _across.CanMove;

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

        bool ShowsBars => _scrollBarWidth > 0f && Thumb.A > 0;

        bool HasDownBar => ShowsBars && CanScroll;

        bool HasAcrossBar => ShowsBars && CanScrollHorizontally;

        /// <summary>The room a bar takes from the content: the bar and the gap beside it, or nothing for no bar.</summary>
        float BarRoom => _scrollBarWidth > 0f ? _scrollBarWidth + _scrollBarGap : 0f;

        /// <summary>The part of the view the content shows in, short of the room the bars take.</summary>
        RectangleF Viewport
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X, room.Y, _across.Viewport, _down.Viewport);
            }
        }

        /// <summary>The bounds of the vertical bar, thumb and track together, at the right, above the horizontal bar's room.</summary>
        RectangleF DownTrack
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X + room.Width - _scrollBarWidth, room.Y, _scrollBarWidth, _down.Viewport);
            }
        }

        /// <summary>The bounds of the horizontal bar, thumb and track together, along the foot, left of the vertical bar's room.</summary>
        RectangleF AcrossTrack
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X, room.Y + room.Height - _scrollBarWidth, _across.Viewport, _scrollBarWidth);
            }
        }

        /// <summary>The bounds of the part of the vertical track that represents the visible content.</summary>
        RectangleF DownThumb
        {
            get
            {
                var track = DownTrack;
                return new RectangleF(track.X, track.Y + _down.ThumbStart, track.Width, _down.ThumbLength);
            }
        }

        /// <summary>The bounds of the part of the horizontal track that represents the visible content.</summary>
        RectangleF AcrossThumb
        {
            get
            {
                var track = AcrossTrack;
                return new RectangleF(track.X + _across.ThumbStart, track.Y, _across.ThumbLength, track.Height);
            }
        }

        /// <summary>Scrolls vertically by <paramref name="amount" />, down when positive, clamped to the content.</summary>
        public void ScrollBy(float amount)
        {
            ScrollToVerticalOffset(_down.Offset + amount);
        }

        /// <summary>Scrolls horizontally by <paramref name="amount" />, right when positive, clamped to the content.</summary>
        public void ScrollHorizontallyBy(float amount)
        {
            ScrollToHorizontalOffset(_across.Offset + amount);
        }

        /// <summary>Scrolls the content vertically to <paramref name="offset" />.</summary>
        /// <remarks>Kept inside what it holds at once, or, while it waits to be measured again, at the next layout, so an offset set with new content in the same frame is measured against that content.</remarks>
        public void ScrollToVerticalOffset(float offset)
        {
            if (Scrolled(_down, offset))
                InvalidateArrange();
        }

        /// <summary>Scrolls the content horizontally to <paramref name="offset" />.</summary>
        /// <remarks>Kept inside what it holds at once, or, while it waits to be measured again, at the next layout, so an offset set with new content in the same frame is measured against that content.</remarks>
        public void ScrollToHorizontalOffset(float offset)
        {
            if (Scrolled(_across, offset))
                InvalidateArrange();
        }

        /// <summary>Moves <paramref name="axis" /> to <paramref name="offset" />, and returns true if that moved it.</summary>
        bool Scrolled(ScrollAxis axis, float offset)
        {
            var held = IsMeasureValid ? axis.Clamp(offset) : Math.Max(0f, offset);
            if (axis.Offset == held)
                return false;

            axis.Offset = held;
            return true;
        }

        /// <inheritdoc />
        /// <remarks>Takes each way of a turn along which it scrolls and any of what it holds is out of sight.</remarks>
        protected override bool OnWheel(Vector2 amount)
        {
            var took = false;
            if (amount.Y != 0f && CanScroll)
            {
                ScrollBy(amount.Y);
                took = true;
            }

            if (amount.X != 0f && CanScrollHorizontally)
            {
                ScrollHorizontallyBy(amount.X);
                took = true;
            }

            return took;
        }

        /// <summary>
        ///     Scrolls by the minimum needed to show all of <paramref name="element" /> within the visible area, along
        ///     each way it scrolls, the next time the view is placed.
        /// </summary>
        public void BringIntoView(Element element)
        {
            _bringing = element;
            InvalidateArrange();
        }

        /// <inheritdoc />
        protected override bool Hits(Vector2 point)
        {
            return (HasDownBar && Inside(DownTrack, point)) || (HasAcrossBar && Inside(AcrossTrack, point));
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            if (HasAcrossBar && Inside(AcrossTrack, point))
            {
                if (!Inside(AcrossThumb, point))
                    ScrollToHorizontalOffset(_across.ThumbedTo(point.X, AcrossTrack.X));

                _draggingAcross = true;
                _heldAt = point.X;
            }
            else if (HasDownBar && Inside(DownTrack, point))
            {
                if (!Inside(DownThumb, point))
                    ScrollToVerticalOffset(_down.ThumbedTo(point.Y, DownTrack.Y));

                _draggingAcross = false;
                _heldAt = point.Y;
            }
            else
            {
                return;
            }

            IsDragging = true;
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            if (!IsDragging)
                return;

            if (_draggingAcross)
            {
                ScrollToHorizontalOffset(_across.Dragged(point.X - _heldAt));
                _heldAt = point.X;
            }
            else
            {
                ScrollToVerticalOffset(_down.Dragged(point.Y - _heldAt));
                _heldAt = point.Y;
            }
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

        /// <summary>
        ///     Scrolls the content from the top left along each way it scrolls, and stretches it along each way it does
        ///     not.
        /// </summary>
        protected override Align Aligns => (_scrollsAcross ? Align.Left : Align.FillWidth) | (_scrollsDown ? Align.Top : Align.FillHeight);

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Content == null)
                return _fits ? Vector2.Zero : available;

            _downBarRoom = _scrollsDown && !_fits ? BarRoom : 0f;
            _acrossBarRoom = _scrollsAcross && !_fits ? BarRoom : 0f;
            MeasureIn(g, available);
            if (!_fits)
                return available;

            // A bar taken for one way leaves less room the other way, which can call for the other bar too; bars are only
            // ever added here, so two rounds settle it.
            for (var round = 0; round < 2; round++)
            {
                var down = _downBarRoom > 0f || (_scrollsDown && HeldSize.Y > available.Y - _acrossBarRoom) ? BarRoom : 0f;
                var across = _acrossBarRoom > 0f || (_scrollsAcross && HeldSize.X > available.X - _downBarRoom) ? BarRoom : 0f;
                if (down == _downBarRoom && across == _acrossBarRoom)
                    break;

                _downBarRoom = down;
                _acrossBarRoom = across;
                MeasureIn(g, available);
            }

            var wanted = HeldSize;
            return new Vector2(Math.Min(wanted.X + _downBarRoom, available.X), Math.Min(wanted.Y + _acrossBarRoom, available.Y));
        }

        /// <summary>
        ///     Measures the content in <paramref name="available" /> less the room the bars take, without bound along each
        ///     way it scrolls.
        /// </summary>
        void MeasureIn(IGraphics g, Vector2 available)
        {
            var wide = _scrollsAcross ? Unbounded : Room(available.X, _downBarRoom);
            var tall = _scrollsDown ? Unbounded : Room(available.Y, _acrossBarRoom);
            MeasureHeld(g, new Vector2(wide, tall));
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _down.Viewport = Room(bounds.Height, _acrossBarRoom);
            _across.Viewport = Room(bounds.Width, _downBarRoom);

            if (Content == null)
            {
                _down.Extent = 0f;
                _down.Offset = 0f;
                _across.Extent = 0f;
                _across.Offset = 0f;
                return;
            }

            _down.Extent = _scrollsDown ? HeldSize.Y : 0f;
            _down.Offset = _down.Clamp(_down.Offset);
            _across.Extent = _scrollsAcross ? HeldSize.X : 0f;
            _across.Offset = _across.Clamp(_across.Offset);
            Place(bounds);

            var bringing = _bringing;
            _bringing = null;
            if (bringing == null)
                return;

            // Where it stands in the content, known only once the content has been placed.
            var down = Brought(_down, bringing.Bounds.Y - (bounds.Y - _down.Offset), bringing.Bounds.Height);
            var across = Brought(_across, bringing.Bounds.X - (bounds.X - _across.Offset), bringing.Bounds.Width);
            if (down == _down.Offset && across == _across.Offset)
                return;

            _down.Offset = down;
            _across.Offset = across;
            Place(bounds);
        }

        /// <summary>
        ///     Returns the offset along <paramref name="axis" /> nearest its own that shows all of something
        ///     <paramref name="length" /> long at <paramref name="at" /> in the content.
        /// </summary>
        static float Brought(ScrollAxis axis, float at, float length)
        {
            var offset = axis.Offset;
            if (at < offset)
                offset = at;
            else if (at + length > offset + axis.Viewport)
                offset = at + length - axis.Viewport;

            return axis.Clamp(offset);
        }

        /// <summary>
        ///     Places the content at its scrolled position inside <paramref name="bounds" />, minus the bars.
        /// </summary>
        void Place(RectangleF bounds)
        {
            var sized = Content.Placement as ISized;

            float x;
            float wide;
            if (_scrollsAcross)
            {
                x = bounds.X - _across.Offset;
                wide = HeldSize.X;
            }
            else
            {
                Span(Content, true, bounds.X, _across.Viewport, Given(sized, true, float.NaN), HeldSize.X, out x, out wide);
            }

            float y;
            float tall;
            if (_scrollsDown)
            {
                y = bounds.Y - _down.Offset;
                tall = HeldSize.Y;
            }
            else
            {
                Span(Content, false, bounds.Y, _down.Viewport, Given(sized, false, float.NaN), HeldSize.Y, out y, out tall);
            }

            Content.Arrange(new RectangleF(x, y, wide, tall));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            // Content scrolled across reaches into the room the bars take, and is cut where that room begins.
            if (Content != null && _scrollsAcross)
            {
                g.PushClip(Viewport);
                Content.Draw(g);
                g.PopClip();
            }
            else if (Content != null)
            {
                Content.Draw(g);
            }

            // Thumbs on no groove, as the game draws its own.
            if (HasDownBar)
                g.Fill(DownThumb, Thumb);

            if (HasAcrossBar)
                g.Fill(AcrossThumb, Thumb);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return !_scrollsAcross || Inside(Viewport, point) ? Content?.HitTest(point) : null;
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
