using System;
using Lumel.Drawing;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>
    ///     A window that hangs from an element on the side given by <see cref="Side" />, or opens at a point, and closes
    ///     when a press lands anywhere else.
    /// </summary>
    /// <remarks>A press that closes it goes no further.</remarks>
    public class Popup : Window
    {
        Vector2? _at;

        /// <summary>Creates a popup laid out across the whole screen, so it can hang from anything on it.</summary>
        public Popup()
        {
            Area = ScreenArea.Window;
        }

        /// <summary>
        ///     Gets or sets the style of this popup, or null to use the host theme's default for this kind of popup.
        /// </summary>
        public new IStyle<Popup> Style
        {
            get { return (IStyle<Popup>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this popup's own look, applied in every state on top of the style's look.</summary>
        public new PopupLook Base => (PopupLook)base.Base;

        /// <summary>Gets this popup's own look when hovered, applied on top of the style's look.</summary>
        public new PopupLook PointerOver => (PopupLook)base.PointerOver;

        /// <summary>Gets this popup's own look when disabled, applied on top of the style's look.</summary>
        public new PopupLook Disabled => (PopupLook)base.Disabled;

        /// <summary>
        ///     Gets this popup's own look for <paramref name="state" />, or for every state that includes it, applied
        ///     on top of the style's look.
        /// </summary>
        public new PopupLook this[State state] => (PopupLook)base[state];

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Popup>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new PopupLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            Gap = ((PopupLook)look).Gap ?? 0f;
        }

        /// <summary>
        ///     Gets or sets the element the popup hangs from, or null to open it at the point <see cref="OpenAt" /> gave,
        ///     or else centered like any window.
        /// </summary>
        public Element Anchor { get; set; }

        /// <summary>
        ///     Gets or sets what finds the element <see cref="Hang" /> hangs the popup from, for an anchor that is rebuilt
        ///     while the popup is open, or null to hang from <see cref="Anchor" /> as it is set.
        /// </summary>
        public Func<Element> FnAnchor { get; set; }

        /// <summary>Gets the distance between the popup and its anchor, in UI units.</summary>
        /// <remarks>Set by this popup's looks.</remarks>
        public float Gap { get; private set; }

        /// <summary>Gets or sets the side of its anchor the popup opens on; below it by default.</summary>
        public PopupSide Side { get; set; }

        /// <summary>
        ///     Whether the popup, when hanging below its anchor, is at least as wide as the anchor, as a drop-down's
        ///     list is at least as wide as its line.
        /// </summary>
        public bool MatchAnchorWidth { get; set; }

        /// <summary>Opens the popup over the screen or window its anchor is on.</summary>
        /// <remarks>Throws where it has no anchor, or its anchor is on no screen or window.</remarks>
        public void Open()
        {
            if (Anchor == null)
                throw new InvalidOperationException("A popup with no anchor has nowhere to open; open it in a window layer instead.");

            var layer = Anchor.Windows;
            if (layer == null)
                throw new InvalidOperationException("A popup's anchor is on no screen or window, so the popup has nowhere to open.");

            layer.Open(this);
        }

        /// <summary>
        ///     Opens the popup in <paramref name="layer" /> with its top left corner at <paramref name="point" />, in the
        ///     layer's UI units.
        /// </summary>
        /// <remarks>
        ///     Where it would run off the area, it opens to the left of or above the point instead. Throws where it has an
        ///     anchor, which decides where it opens.
        /// </remarks>
        public void OpenAt(WindowLayer layer, Vector2 point)
        {
            if (Anchor != null)
                throw new InvalidOperationException("A popup with an anchor opens beside its anchor; clear the anchor to open it at a point.");

            _at = point;
            layer.Open(this);
        }

        /// <summary>
        ///     Hangs the popup from the element <see cref="FnAnchor" /> finds: opens it there once that element is on a
        ///     screen or window, and moves it to the element found while it is open.
        /// </summary>
        /// <remarks>
        ///     Cheap enough to call on every tick, as it costs one call of <see cref="FnAnchor" />. It opens a closed popup
        ///     too, so a caller stops calling it once the popup has closed for good. Throws where <see cref="FnAnchor" />
        ///     is null.
        /// </remarks>
        public void Hang()
        {
            if (FnAnchor == null)
                throw new InvalidOperationException("A popup hangs from what FnAnchor finds; give it FnAnchor first.");

            var found = FnAnchor();
            if (found == null || found.Windows == null)
                return;

            Anchor = found;
            if (!IsOpen)
                Open();
        }

        /// <summary>Closes the popup if it is open, and opens it otherwise.</summary>
        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        ///     Returns the space to measure the popup in: beside its anchor, the part of <paramref name="area" /> on
        ///     the roomier side; above or below it, the whole height of <paramref name="area" />, so the popup is
        ///     measured at its natural height and <see cref="Place" /> decides where it fits.
        /// </summary>
        public override Vector2 Room(RectangleF area)
        {
            if (Anchor == null)
                return base.Room(area);

            if (Side != PopupSide.Right)
                return new Vector2(area.Width, area.Height);

            var from = Anchor.Bounds;
            var right = area.X + area.Width - (from.X + from.Width + Gap);
            var left = from.X - Gap - area.X;
            var wide = right > left ? right : left;

            return new Vector2(wide > 0f ? wide : 0f, area.Height);
        }

        /// <summary>
        ///     Returns the bounds of the popup: below its anchor if all of it fits there, otherwise above if all of it
        ///     fits, otherwise on the roomier side and no taller than that space, never over the anchor; beside its
        ///     anchor, on the right if it fits and on the left otherwise, kept inside <paramref name="area" />; below a
        ///     line, as wide as the line if <see cref="MatchAnchorWidth" /> is set.
        /// </summary>
        /// <remarks>Where it is cut shorter than it asks, what it holds scrolls only if that is a scroll view.</remarks>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            if (Anchor == null)
                return _at.HasValue ? From(area, _at.Value, size) : base.Place(area, size);

            var from = Anchor.Bounds;
            if (Side == PopupSide.Right)
                return Beside(area, from, size);

            if (MatchAnchorWidth && size.X < from.Width)
                size.X = from.Width;

            var below = area.Y + area.Height - (from.Y + from.Height + Gap);
            var above = from.Y - Gap - area.Y;
            bool over;
            if (size.Y <= below)
                over = false;
            else if (size.Y <= above)
                over = true;
            else
            {
                over = above > below;
                var room = over ? above : below;
                size.Y = room > 0f ? room : 0f;
            }

            var y = over ? from.Y - Gap - size.Y : from.Y + from.Height + Gap;
            return new RectangleF(Across(area, from.X, size.X), y, size.X, size.Y);
        }

        /// <summary>
        ///     Returns the bounds of the popup at <paramref name="size" /> beside <paramref name="from" />, kept inside
        ///     <paramref name="area" />.
        /// </summary>
        RectangleF Beside(RectangleF area, RectangleF from, Vector2 size)
        {
            var x = from.X + from.Width + Gap;
            if (x + size.X > area.X + area.Width)
                x = from.X - Gap - size.X;

            var y = from.Y + from.Height * 0.5f - Lead();
            if (y + size.Y > area.Y + area.Height)
                y = area.Y + area.Height - size.Y;

            if (y < area.Y)
                y = area.Y;

            return new RectangleF(Across(area, x, size.X), y, size.X, size.Y);
        }

        /// <summary>
        ///     Returns the bounds of the popup at <paramref name="size" /> with its top left corner at
        ///     <paramref name="point" />, or another corner where that keeps it inside <paramref name="area" />.
        /// </summary>
        static RectangleF From(RectangleF area, Vector2 point, Vector2 size)
        {
            var x = point.X + size.X > area.X + area.Width ? point.X - size.X : point.X;
            var y = point.Y + size.Y > area.Y + area.Height ? point.Y - size.Y : point.Y;
            return new RectangleF(Across(area, x, size.X), Down(area, y, size.Y), size.X, size.Y);
        }

        /// <summary>
        ///     Returns <paramref name="y" />, moved in if something <paramref name="tall" /> tall standing there would
        ///     run off <paramref name="area" />.
        /// </summary>
        static float Down(RectangleF area, float y, float tall)
        {
            if (y + tall > area.Y + area.Height)
                y = area.Y + area.Height - tall;

            return y < area.Y ? area.Y : y;
        }

        /// <summary>
        ///     Returns <paramref name="x" />, moved in if something <paramref name="wide" /> wide standing there would
        ///     run off <paramref name="area" />.
        /// </summary>
        static float Across(RectangleF area, float x, float wide)
        {
            if (x + wide > area.X + area.Width)
                x = area.X + area.Width - wide;

            return x < area.X ? area.X : x;
        }

        /// <summary>
        ///     Returns the distance from the popup's top to the middle of its first entry, which is aligned with the
        ///     middle of the anchor.
        /// </summary>
        /// <remarks>The first thing in what it holds, where that holds several, and otherwise what it holds as a whole.</remarks>
        protected virtual float Lead()
        {
            var content = Content;
            if (content == null)
                return 0f;

            var first = content.VisualChildCount > 0 ? content.GetVisualChild(0) : content;
            return Padding.Top + first.DesiredSize.Y * 0.5f;
        }
    }
}
