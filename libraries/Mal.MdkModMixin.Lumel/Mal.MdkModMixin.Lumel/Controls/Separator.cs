using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>A line across a block, with space around it.</summary>
    public class Separator : Element, IPadded
    {
        /// <summary>The thickness of the line in UI units when no look sets it.</summary>
        public const float NeutralThickness = 1f;

        Thickness _padding;
        float _thickness = NeutralThickness;

        /// <summary>
        ///     Creates a line using the look from the host's theme for a separator until it is given a style.
        /// </summary>
        public Separator()
        {
            Color = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this separator, or null to use the default from the host's theme for a
        ///     separator.
        /// </summary>
        public new IStyle<Separator> Style
        {
            get { return (IStyle<Separator>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this separator's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new SeparatorLook Base => (SeparatorLook)base.Base;

        /// <summary>Gets this separator's own look applied on hover, layered over the look from its style.</summary>
        public new SeparatorLook PointerOver => (SeparatorLook)base.PointerOver;

        /// <summary>
        ///     Gets this separator's own look applied when disabled, layered over the look from its style.
        /// </summary>
        public new SeparatorLook Disabled => (SeparatorLook)base.Disabled;

        /// <summary>
        ///     Gets this separator's own look for <paramref name="state" />, or for every state it joins, layered over
        ///     the look from its style.
        /// </summary>
        public new SeparatorLook this[State state] => (SeparatorLook)base[state];

        /// <summary>Gets the thickness of the line, in UI units.</summary>
        /// <remarks>Set by this separator's looks.</remarks>
        public float Thickness => _thickness;

        /// <summary>Gets the color of the line.</summary>
        /// <remarks>Set by this separator's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the space between the edge of the separator and the line.</summary>
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
            return style is IStyle<Separator>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new SeparatorLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var line = (SeparatorLook)look;
            Color = line.Color ?? Color.White;

            var thickness = line.Thickness ?? NeutralThickness;
            if (thickness == _thickness)
                return;

            _thickness = thickness;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Asks for no width, so it is as wide as what holds it makes it and never widens it.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(0f, _thickness);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            g.Fill(new RectangleF(ContentBounds.X, ContentBounds.Y, ContentBounds.Width, _thickness), Color);
        }
    }
}
