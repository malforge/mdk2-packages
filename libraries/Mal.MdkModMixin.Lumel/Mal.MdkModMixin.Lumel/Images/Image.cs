using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>An element that draws an image across its bounds, inside its padding.</summary>
    /// <remarks>
    ///     Given a size both ways by its placement, it is that size; given one side, it takes the other from the
    ///     picture's shape. Given neither, it is as big as fits the room it is offered, or as big as the picture was
    ///     made where the room is unbounded both ways.
    /// </remarks>
    public class Image : Element, IPadded
    {
        Thickness _padding;
        IImageSource _source;

        /// <summary>
        ///     Creates an image that draws nothing yet and uses its own colors unless its looks say otherwise.
        /// </summary>
        public Image()
        {
            Tint = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this image, or null to use the theme's default style for images.
        /// </summary>
        public new IStyle<Image> Style
        {
            get { return (IStyle<Image>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this image's own look, applied in every state on top of its style's.</summary>
        /// <remarks>A tint that comes from data, rather than from how the image looks, is set here: <c>image.Base.Tint = color</c>.</remarks>
        public new ImageLook Base => (ImageLook)base.Base;

        /// <summary>Gets this image's own look for when it is hovered, applied on top of its style's.</summary>
        public new ImageLook PointerOver => (ImageLook)base.PointerOver;

        /// <summary>Gets this image's own look for when it is disabled, applied on top of its style's.</summary>
        public new ImageLook Disabled => (ImageLook)base.Disabled;

        /// <summary>
        ///     Gets this image's own look for <paramref name="state" />, or for every state that includes it, applied
        ///     on top of its style's.
        /// </summary>
        public new ImageLook this[State state] => (ImageLook)base[state];

        /// <summary>Gets or sets the image to draw, or null to draw nothing.</summary>
        public IImageSource Source
        {
            get { return _source; }
            set
            {
                if (_source == value)
                    return;

                _source = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets or sets how far the image is turned clockwise about its center, in radians.</summary>
        /// <remarks>The image keeps the room it was laid out in, and its corners may be drawn outside it.</remarks>
        public float Rotation { get; set; }

        /// <summary>Gets the color the image is multiplied by; white draws it unchanged.</summary>
        /// <remarks>Set by this image's looks.</remarks>
        public Color Tint { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Image>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ImageLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            Tint = ((ImageLook)look).Tint ?? Color.White;
        }

        /// <summary>Gets the space kept clear between the edge and the image, which is drawn inside it.</summary>
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
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_source == null)
                return Vector2.Zero;

            var size = _source.Size;
            var sized = Placement as ISized;
            var wide = sized != null && !float.IsNaN(sized.Width);
            var high = sized != null && !float.IsNaN(sized.Height);

            if (wide && high)
                return available;

            if (wide)
                return new Vector2(available.X, AspectRatio.HeightFor(size, available.X));

            if (high)
                return new Vector2(AspectRatio.WidthFor(size, available.Y), available.Y);

            var acrossBounded = available.X < Unbounded;
            var downBounded = available.Y < Unbounded;

            if (!acrossBounded && !downBounded)
                return size;

            var across = acrossBounded ? available.X : AspectRatio.WidthFor(size, available.Y);
            var down = AspectRatio.HeightFor(size, across);
            if (downBounded && down > available.Y)
            {
                down = available.Y;
                across = AspectRatio.WidthFor(size, down);
            }

            return new Vector2(across, down);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _source?.Draw(g, ContentBounds, Tint, Rotation);
        }
    }
}
