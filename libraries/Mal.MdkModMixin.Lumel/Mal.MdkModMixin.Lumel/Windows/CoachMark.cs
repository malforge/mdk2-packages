using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>
    ///     A bubble of text that hangs from an element to point something out, with a tail aimed at it, which the player
    ///     dismisses with a press on it, or where <see cref="DismissesOnPressOutside" /> is set, anywhere else.
    /// </summary>
    /// <remarks>
    ///     It closes without being dismissed when its anchor is no longer shown. Keys and Escape go to the window
    ///     beneath it, as if it were not there.
    /// </remarks>
    public class CoachMark : Popup
    {
        // The tail is a square turned to stand on a corner, half of it out past the bubble's edge: sqrt(2) of the
        // gap on a side reaches the gap out.
        const float SquareToTail = 1.41421356f;

        readonly RichText _body = new RichText();
        float _maxWidth = float.PositiveInfinity;

        /// <summary>Creates a coach mark saying <paramref name="markdown" />, written in markdown-lite.</summary>
        public CoachMark(string markdown) : this(Markdown.Parse(markdown ?? ""))
        {
        }

        /// <summary>Creates a coach mark showing a <paramref name="document" /> built in code.</summary>
        public CoachMark(Document document)
        {
            _body.Document = document;
            Content = _body;
        }

        /// <summary>
        ///     Gets or sets the style of this coach mark, or null to use the host theme's default for this kind of coach
        ///     mark.
        /// </summary>
        public new IStyle<CoachMark> Style
        {
            get { return (IStyle<CoachMark>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this coach mark's own look, applied in every state on top of the style's look.</summary>
        public new CoachMarkLook Base => (CoachMarkLook)base.Base;

        /// <summary>Gets this coach mark's own look when hovered, applied on top of the style's look.</summary>
        public new CoachMarkLook PointerOver => (CoachMarkLook)base.PointerOver;

        /// <summary>Gets this coach mark's own look when disabled, applied on top of the style's look.</summary>
        public new CoachMarkLook Disabled => (CoachMarkLook)base.Disabled;

        /// <summary>
        ///     Gets this coach mark's own look for <paramref name="state" />, or for every state that includes it,
        ///     applied on top of the style's look.
        /// </summary>
        public new CoachMarkLook this[State state] => (CoachMarkLook)base[state];

        /// <summary>Gets the text the coach mark was made with, for handling its links.</summary>
        public RichText Body => _body;

        /// <summary>Gets the widest the coach mark grows in UI units before its text wraps.</summary>
        /// <remarks>Set by this coach mark's looks.</remarks>
        public float MaxWidth => _maxWidth;

        /// <summary>Gets the style of the coach mark's text, or null to use the default from the host's theme.</summary>
        /// <remarks>Set by this coach mark's looks.</remarks>
        public IStyle<RichText> BodyStyle { get; private set; }

        /// <summary>Whether a press anywhere off the coach mark dismisses it; false by default.</summary>
        public bool DismissesOnPressOutside { get; set; }

        /// <summary>
        ///     Whether a press off the coach mark that dismisses it goes on to what it lands on, as if the coach mark had
        ///     not been there; true by default.
        /// </summary>
        public bool PassesPressOutside { get; set; } = true;

        /// <summary>Raised when the player has dismissed the coach mark, after it closed.</summary>
        /// <remarks>Raised by <see cref="Dismiss" /> only, never when it closes any other way.</remarks>
        public event Action Dismissed;

        /// <summary>
        ///     Closes the coach mark, if it is open, as dismissed by the player, then raises <see cref="Dismissed" />.
        /// </summary>
        public void Dismiss()
        {
            if (!IsOpen)
                return;

            Close();
            Dismissed?.Invoke();
        }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        /// <remarks>A press of the left button released on the coach mark, away from anything in it that takes the press, dismisses it.</remarks>
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button == PointerButton.Left && Bounds.Contains(point))
                Dismiss();
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<CoachMark>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new CoachMarkLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var mark = (CoachMarkLook)look;
            BodyStyle = mark.BodyStyle;
            _body.Style = BodyStyle;

            var maxWidth = mark.MaxWidth ?? float.PositiveInfinity;
            if (maxWidth == _maxWidth)
                return;

            _maxWidth = maxWidth;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Never wider than <see cref="MaxWidth" />, padding included.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var widest = Math.Max(0f, _maxWidth - Padding.Horizontal);
            return base.MeasureContent(g, new Vector2(Math.Min(available.X, widest), available.Y));
        }

        /// <summary>
        ///     Returns the bounds of the coach mark: where a popup on the same <see cref="Popup.Side" /> would be, and
        ///     above or below its anchor centered across it, kept inside <paramref name="area" />.
        /// </summary>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            var placed = base.Place(area, size);
            if (Anchor == null || Side == PopupSide.Right)
                return placed;

            var from = Anchor.Bounds;
            var x = from.X + (from.Width - placed.Width) * 0.5f;
            x = Math.Min(x, area.X + area.Width - placed.Width);
            placed.X = Math.Max(x, area.X);
            return placed;
        }

        /// <inheritdoc />
        /// <remarks>Half the coach mark's height, so it stands centered beside its anchor.</remarks>
        protected override float Lead()
        {
            return DesiredSize.Y * 0.5f;
        }

        /// <inheritdoc />
        /// <remarks>Its shadow, then the tail's edge in the border's color, or the whole tail where it has no border.</remarks>
        public override void DrawBehind(IGraphics g)
        {
            base.DrawBehind(g);

            RectangleF tail;
            if (!Tail(out tail))
                return;

            var edged = Border.A > 0 && BorderThickness > 0f;
            var color = edged ? Border : Background;
            if (color.A > 0)
                g.Fill(tail, color, MathHelper.PiOver4);
        }

        /// <inheritdoc />
        /// <remarks>The inside of the tail goes over the bubble's border where the two meet, then what it holds.</remarks>
        protected override void DrawContent(IGraphics g)
        {
            RectangleF tail;
            if (Background.A > 0 && Border.A > 0 && BorderThickness > 0f && Tail(out tail))
            {
                var inset = BorderThickness;
                var inner = new RectangleF(tail.X + inset, tail.Y + inset, tail.Width - 2f * inset, tail.Height - 2f * inset);
                if (inner.Width > 0f)
                    g.Fill(inner, Background, MathHelper.PiOver4);
            }

            base.DrawContent(g);
        }

        /// <summary>
        ///     Returns true if the coach mark has a tail, with <paramref name="square" /> the square, before it is turned
        ///     on its corner, that draws it: centered on the edge facing the anchor, level with the anchor's middle as near
        ///     as the bubble allows.
        /// </summary>
        bool Tail(out RectangleF square)
        {
            square = default(RectangleF);
            if (Anchor == null || Gap <= 0f)
                return false;

            var side = Gap * SquareToTail;
            var half = side * 0.5f;
            var reach = Gap;
            var bubble = Bounds;
            var from = Anchor.Bounds;
            Vector2 at;
            if (bubble.Y >= from.Y + from.Height)
                at = new Vector2(Along(from.X + from.Width * 0.5f, bubble.X, bubble.Width, reach), bubble.Y);
            else if (bubble.Y + bubble.Height <= from.Y)
                at = new Vector2(Along(from.X + from.Width * 0.5f, bubble.X, bubble.Width, reach), bubble.Y + bubble.Height);
            else if (bubble.X >= from.X + from.Width)
                at = new Vector2(bubble.X, Along(from.Y + from.Height * 0.5f, bubble.Y, bubble.Height, reach));
            else if (bubble.X + bubble.Width <= from.X)
                at = new Vector2(bubble.X + bubble.Width, Along(from.Y + from.Height * 0.5f, bubble.Y, bubble.Height, reach));
            else
                return false;

            square = new RectangleF(at.X - half, at.Y - half, side, side);
            return true;
        }

        /// <summary>
        ///     Returns <paramref name="target" />, kept <paramref name="margin" /> inside an edge that starts at
        ///     <paramref name="start" /> and is <paramref name="length" /> long, or the edge's middle where it is too short.
        /// </summary>
        static float Along(float target, float start, float length, float margin)
        {
            if (length <= 2f * margin)
                return start + length * 0.5f;

            return Math.Max(start + margin, Math.Min(target, start + length - margin));
        }
    }
}
