using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>A dial that shows and sets an angle with a needle from the center and marks around the edge.</summary>
    public class Dial : Element
    {
        /// <summary>The state of a dial while the pointer is turning the needle.</summary>
        public static readonly State Dragging = State.Declare("Dragging", 400);

        const float Across = 96f;
        const float Mark = 8f;
        const float Quarter = 12f;
        const float Thin = 2f;
        const float Needle = 4f;
        const float Hub = 8f;
        const float RightAngle = 90f;

        float? _angle;
        float _maximumAngle = 360f;
        bool _half;

        /// <summary>
        ///     Creates a dial with no angle that can turn a full circle in either direction, using the look from the
        ///     host's theme for a dial until it is given a style.
        /// </summary>
        public Dial()
        {
            MarkColor = Color.Gray;
            QuarterColor = Color.White;
            NeedleColor = Color.White;
        }

        /// <summary>
        ///     Raised when the pointer turns the needle, but not when the angle is changed by setting
        ///     <see cref="Angle" />.
        /// </summary>
        public event Action AngleChanged;

        /// <summary>
        ///     Gets or sets the style of this dial, or null to use the default from the host's theme for a dial.
        /// </summary>
        public new IStyle<Dial> Style
        {
            get { return (IStyle<Dial>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this dial's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new DialLook Base => (DialLook)base.Base;

        /// <summary>Gets this dial's own look applied on hover, layered over the look from its style.</summary>
        public new DialLook PointerOver => (DialLook)base.PointerOver;

        /// <summary>Gets this dial's own look applied when disabled, layered over the look from its style.</summary>
        public new DialLook Disabled => (DialLook)base.Disabled;

        /// <summary>
        ///     Gets this dial's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new DialLook this[State state] => (DialLook)base[state];

        /// <summary>
        ///     Gets or sets the angle the needle points to, in degrees clockwise from straight up and limited to
        ///     <see cref="MaximumAngle" /> in either direction; null for no angle, which draws only the marks and the
        ///     hub.
        /// </summary>
        public float? Angle
        {
            get { return _angle; }
            set { _angle = value.HasValue ? Held(value.Value) : (float?)null; }
        }

        /// <summary>
        ///     Gets or sets how far the needle may turn in either direction, in degrees; a quarter turn or less is
        ///     drawn as the top half of a dial, half as tall.
        /// </summary>
        public float MaximumAngle
        {
            get { return _maximumAngle; }
            set
            {
                _maximumAngle = Math.Max(0f, value);
                if (_angle.HasValue)
                    _angle = Held(_angle.Value);

                var half = _maximumAngle <= RightAngle;
                if (half == _half)
                    return;

                _half = half;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the color of the marks between the quarters.</summary>
        /// <remarks>Set by this dial's looks.</remarks>
        public Color MarkColor { get; private set; }

        /// <summary>Gets the color of the marks on the quarters.</summary>
        /// <remarks>Set by this dial's looks.</remarks>
        public Color QuarterColor { get; private set; }

        /// <summary>Gets the color of the needle and the hub.</summary>
        /// <remarks>Set by this dial's looks.</remarks>
        public Color NeedleColor { get; private set; }

        /// <summary>Whether the pointer is currently turning the needle.</summary>
        /// <remarks>While it is, the dial shows its looks for <see cref="Dragging" />.</remarks>
        public bool IsDragging
        {
            get { return Is(Dragging); }
            private set { Set(Dragging, value); }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <summary>
        ///     Gets the point the needle turns about: the middle of a full dial, or the bottom of a half dial.
        /// </summary>
        Vector2 Pivot
        {
            get
            {
                var room = ContentBounds;
                return _half ? new Vector2(room.Center.X, room.Y + room.Height - Hub) : room.Center;
            }
        }

        /// <summary>Gets how far from the pivot the marks reach.</summary>
        float Reach
        {
            get
            {
                var room = ContentBounds;
                return _half ? Math.Min(room.Width / 2f, room.Height - Hub) : Math.Min(room.Width, room.Height) / 2f;
            }
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Dial>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new DialLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var dial = (DialLook)look;
            MarkColor = dial.MarkColor ?? Color.Gray;
            QuarterColor = dial.QuarterColor ?? Color.White;
            NeedleColor = dial.NeedleColor ?? Color.White;
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            IsDragging = true;
            Follow(point);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            Follow(point);
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
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return _half ? new Vector2(Across, Across / 2f + Hub) : new Vector2(Across, Across);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var center = Pivot;
            var reach = Reach;

            // A mark every fifteen degrees, longer on each quarter; a half dial has only the ones it reaches.
            var from = _half ? -6 : 0;
            var to = _half ? 6 : 23;
            for (var i = from; i <= to; i++)
            {
                var quarter = i % 6 == 0;
                Line(g, center, i * 15f, reach - (quarter ? Quarter : Mark), reach, quarter ? QuarterColor : MarkColor, Thin);
            }

            if (_angle.HasValue)
                Line(g, center, _angle.Value, 0f, reach - Quarter - Thin, NeedleColor, Needle);

            g.Fill(new RectangleF(center.X - Hub / 2f, center.Y - Hub / 2f, Hub, Hub), NeedleColor);
        }

        /// <summary>
        ///     Turns the needle the short way from its current angle toward <paramref name="point" />, in whole
        ///     degrees.
        /// </summary>
        void Follow(Vector2 point)
        {
            var center = Pivot;
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            if (dx * dx + dy * dy < Hub * Hub)
                return;

            var standing = _angle ?? 0f;
            var pointing = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
            var moved = Wrapped(pointing - Wrapped(standing) + 180f) - 180f;
            Angle = (float)Math.Round(standing + moved);

            AngleChanged?.Invoke();
        }

        /// <summary>
        ///     Returns <paramref name="degrees" /> limited to <see cref="MaximumAngle" /> in either direction.
        /// </summary>
        float Held(float degrees)
        {
            return Math.Max(-_maximumAngle, Math.Min(_maximumAngle, degrees));
        }

        /// <summary>Returns <paramref name="degrees" /> wrapped into a single turn, from 0 to 360.</summary>
        static float Wrapped(float degrees)
        {
            var wrapped = degrees % 360f;
            return wrapped < 0 ? wrapped + 360f : wrapped;
        }

        /// <summary>
        ///     Draws a line in <paramref name="color" /> with thickness <paramref name="thick" /> at angle
        ///     <paramref name="degrees" /> from <paramref name="center" />, between the distances
        ///     <paramref name="from" /> and <paramref name="to" />, using <paramref name="g" />.
        /// </summary>
        static void Line(IGraphics g, Vector2 center, float degrees, float from, float to, Color color, float thick)
        {
            var turned = MathHelper.ToRadians(degrees);
            var along = new Vector2((float)Math.Sin(turned), -(float)Math.Cos(turned));
            var middle = center + along * ((from + to) / 2f);
            var length = to - from;

            g.Fill(new RectangleF(middle.X - thick / 2f, middle.Y - length / 2f, thick, length), color, turned);
        }
    }
}
