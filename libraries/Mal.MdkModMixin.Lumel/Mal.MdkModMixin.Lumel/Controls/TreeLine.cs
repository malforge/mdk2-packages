using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A line down a gutter that connects the elements indented beside it to the element they hang from, turning
    ///     inward at <see cref="Ends" />.
    /// </summary>
    /// <remarks>
    /// The turn is drawn past the line's own width. Something that draws its own share of a longer line, such as one row of
    /// many, draws it with <see cref="Down" /> and <see cref="Turning" />.
    /// </remarks>
    public class TreeLine : Element
    {
        /// <summary>The width of the line when no look sets it, in UI units.</summary>
        public const float NeutralThickness = 1f;

        float _thickness = NeutralThickness;

        /// <summary>
        ///     Creates a line using the look its host's theme holds for a tree line until it is given a style.
        /// </summary>
        public TreeLine()
        {
            Color = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this line, or null to use the default its host's theme holds for a tree line.
        /// </summary>
        public new IStyle<TreeLine> Style
        {
            get { return (IStyle<TreeLine>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>The look this line sets itself for every state, applied over its style's.</summary>
        public new TreeLineLook Base => (TreeLineLook)base.Base;

        /// <summary>The look this line sets itself for when the pointer is over it, applied over its style's.</summary>
        public new TreeLineLook PointerOver => (TreeLineLook)base.PointerOver;

        /// <summary>The look this line sets itself for when it is disabled, applied over its style's.</summary>
        public new TreeLineLook Disabled => (TreeLineLook)base.Disabled;

        /// <summary>
        ///     The look this line sets itself for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style's.
        /// </summary>
        public new TreeLineLook this[State state] => (TreeLineLook)base[state];

        /// <summary>The width of the line, in UI units.</summary>
        /// <remarks>Set by this line's looks.</remarks>
        public float Thickness => _thickness;

        /// <summary>How far the line turns inward at its end, in UI units.</summary>
        /// <remarks>Set by this line's looks.</remarks>
        public float Turn { get; private set; }

        /// <summary>The color of the line.</summary>
        /// <remarks>Set by this line's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>
        ///     Gets or sets the element the line turns inward at, or null for a line spanning the full height of the
        ///     element.
        /// </summary>
        public Element Ends { get; set; }

        /// <summary>
        ///     Gets or sets whether the line turns inward along the bottom edge of <see cref="Ends" />, rather than at its
        ///     middle.
        /// </summary>
        public bool TurnsAtBottom { get; set; }

        /// <summary>
        ///     Draws a vertical line segment of width <paramref name="thickness" /> at <paramref name="x" />, from
        ///     <paramref name="top" /> to <paramref name="bottom" />.
        /// </summary>
        public static void Down(IGraphics g, float x, float top, float bottom, float thickness, Color color)
        {
            g.Fill(new RectangleF(x, top, thickness, bottom - top), color);
        }

        /// <summary>
        ///     Draws the last segment of a line at <paramref name="x" />, from <paramref name="top" /> down to
        ///     <paramref name="at" />, where it turns inward by <paramref name="turn" />.
        /// </summary>
        public static void Turning(IGraphics g, float x, float top, float at, float thickness, float turn, Color color)
        {
            Down(g, x, top, at + thickness, thickness, color);
            g.Fill(new RectangleF(x, at, turn, thickness), color);
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<TreeLine>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TreeLineLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var line = (TreeLineLook)look;
            Turn = line.Turn ?? 0f;
            Color = line.Color ?? Color.White;

            var thickness = line.Thickness ?? NeutralThickness;
            if (thickness == _thickness)
                return;

            _thickness = thickness;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(_thickness, 0f);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Ends == null || !Ends.IsVisible)
                Down(g, Bounds.X, Bounds.Y, Bounds.Y + Bounds.Height, _thickness, Color);
            else
                Turning(g, Bounds.X, Bounds.Y, TurnPoint(), _thickness, Turn, Color);
        }

        /// <summary>Returns the top of the turn, which at the bottom of <see cref="Ends" /> stands inside its bounds.</summary>
        float TurnPoint()
        {
            return TurnsAtBottom ? Ends.Bounds.Y + Ends.Bounds.Height - _thickness : Ends.Bounds.Y + Ends.Bounds.Height * 0.5f;
        }
    }
}
