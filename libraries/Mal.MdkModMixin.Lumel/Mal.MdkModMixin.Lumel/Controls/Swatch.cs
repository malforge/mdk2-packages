using Lumel.Drawing;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>A patch of a single color that shows what a color looks like.</summary>
    /// <remarks>
    ///     It has no size of its own: its placement gives it one, such as <c>FixedSize.Size</c>. Its border is the element's
    ///     own <see cref="Element.Border" />, set by its looks. The color it shows is its content, which no look touches.
    /// </remarks>
    public class Swatch : Element
    {
        /// <summary>
        ///     Gets or sets the style of this swatch, or null to use the default from the host's theme for a swatch.
        /// </summary>
        public new IStyle<Swatch> Style
        {
            get { return (IStyle<Swatch>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this swatch's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new SwatchLook Base => (SwatchLook)base.Base;

        /// <summary>Gets this swatch's own look applied on hover, layered over the look from its style.</summary>
        public new SwatchLook PointerOver => (SwatchLook)base.PointerOver;

        /// <summary>Gets this swatch's own look applied when disabled, layered over the look from its style.</summary>
        public new SwatchLook Disabled => (SwatchLook)base.Disabled;

        /// <summary>
        ///     Gets this swatch's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new SwatchLook this[State state] => (SwatchLook)base[state];

        /// <summary>The color it shows.</summary>
        public Color Color { get; set; }

        /// <summary>How strongly the color is shown, from 0 (not at all) to 1 (fully).</summary>
        /// <remarks>Set by this swatch's looks.</remarks>
        public float Opacity { get; private set; } = 1f;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Swatch>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new SwatchLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            Opacity = ((SwatchLook)look).Opacity ?? 1f;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        /// <remarks>Fills inside the border, which the element has drawn already.</remarks>
        protected override void DrawContent(IGraphics g)
        {
            var shown = new Color(Color.R, Color.G, Color.B, (byte)(Color.A * MathHelper.Clamp(Opacity, 0f, 1f)));
            if (shown.A == 0)
                return;

            var room = ContentBounds;
            var edge = Border.A > 0 ? BorderThickness : 0f;
            g.Fill(new RectangleF(room.X + edge, room.Y + edge, room.Width - edge * 2f, room.Height - edge * 2f), shown);
        }
    }
}
