using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A square showing every saturation and value of one hue, with a marker placed on it: saturation increases to
    ///     the right and value increases upward.
    /// </summary>
    /// <remarks>Drawn in three: a fill of the hue, <see cref="Pale" /> over it, and <see cref="Dark" /> over both.</remarks>
    public class ColorSpectrum : Element
    {
        /// <summary>The width of the marker's square in UI units when no look sets it.</summary>
        public const float NeutralMarkerSize = 10f;

        /// <summary>The thickness in UI units of the marker's square and its outline when no look sets them.</summary>
        public const float NeutralMarkerThickness = 2f;

        float _hue;
        float _saturation;
        float _value;

        /// <summary>
        ///     Creates a spectrum of red with the marker at the bottom left, using the look from the host's theme for a
        ///     spectrum until it is given a style.
        /// </summary>
        public ColorSpectrum()
        {
            MarkerSize = NeutralMarkerSize;
            MarkerThickness = NeutralMarkerThickness;
            MarkerOutlineThickness = NeutralMarkerThickness;
            MarkerColor = Color.White;
            MarkerOutlineColor = Color.Black;
        }

        /// <summary>
        ///     Raised when the pointer moves the marker, but not when the marker is moved by setting the color parts.
        /// </summary>
        public event Action ColorChanged;

        /// <summary>
        ///     Gets or sets the style of this spectrum, or null to use the default from the host's theme for a
        ///     spectrum.
        /// </summary>
        public new IStyle<ColorSpectrum> Style
        {
            get { return (IStyle<ColorSpectrum>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this spectrum's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new ColorSpectrumLook Base => (ColorSpectrumLook)base.Base;

        /// <summary>Gets this spectrum's own look applied on hover, layered over the look from its style.</summary>
        public new ColorSpectrumLook PointerOver => (ColorSpectrumLook)base.PointerOver;

        /// <summary>
        ///     Gets this spectrum's own look applied when disabled, layered over the look from its style.
        /// </summary>
        public new ColorSpectrumLook Disabled => (ColorSpectrumLook)base.Disabled;

        /// <summary>
        ///     Gets this spectrum's own look for <paramref name="state" />, or for every state it joins, layered over
        ///     the look from its style.
        /// </summary>
        public new ColorSpectrumLook this[State state] => (ColorSpectrumLook)base[state];

        /// <summary>
        ///     Gets the picture of a white gradient that is opaque at the left edge and fades to transparent at the
        ///     right, representing saturation across.
        /// </summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public MyStringId Pale { get; private set; }

        /// <summary>
        ///     Gets the picture of a black gradient that is transparent at the top edge and opaque at the bottom,
        ///     representing value upward.
        /// </summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public MyStringId Dark { get; private set; }

        /// <summary>Gets the width of the marker's square, in UI units.</summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public float MarkerSize { get; private set; }

        /// <summary>Gets the line thickness of the marker's square, in UI units.</summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public float MarkerThickness { get; private set; }

        /// <summary>Gets the line thickness of the outline around the marker, in UI units.</summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public float MarkerOutlineThickness { get; private set; }

        /// <summary>Gets the color of the marker.</summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public Color MarkerColor { get; private set; }

        /// <summary>
        ///     Gets the color of the outline around the marker, which keeps the marker visible on both light and dark
        ///     colors.
        /// </summary>
        /// <remarks>Set by this spectrum's looks.</remarks>
        public Color MarkerOutlineColor { get; private set; }

        /// <summary>Gets or sets the hue shown by the square, from 0 to 1 around the color wheel.</summary>
        public float Hue
        {
            get { return _hue; }
            set { _hue = Held(value); }
        }

        /// <summary>
        ///     Gets or sets the horizontal position of the marker, from none of the hue at the left to all of it at the
        ///     right.
        /// </summary>
        public float Saturation
        {
            get { return _saturation; }
            set { _saturation = Held(value); }
        }

        /// <summary>
        ///     Gets or sets the vertical position of the marker, from black at the bottom to full brightness at the
        ///     top.
        /// </summary>
        public float Value
        {
            get { return _value; }
            set { _value = Held(value); }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ColorSpectrum>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ColorSpectrumLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var spectrum = (ColorSpectrumLook)look;
            Pale = spectrum.Pale ?? MyStringId.NullOrEmpty;
            Dark = spectrum.Dark ?? MyStringId.NullOrEmpty;
            MarkerSize = spectrum.MarkerSize ?? NeutralMarkerSize;
            MarkerThickness = spectrum.MarkerThickness ?? NeutralMarkerThickness;
            MarkerOutlineThickness = spectrum.MarkerOutlineThickness ?? NeutralMarkerThickness;
            MarkerColor = spectrum.MarkerColor ?? Color.White;
            MarkerOutlineColor = spectrum.MarkerOutlineColor ?? Color.Black;
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

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
        /// <remarks>Asks for no room of its own: its placement gives it its size.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            g.Fill(room, new Vector3(_hue, 1f, 1f).HSVtoColor());
            g.DrawSprite(Pale, room, Color.White, 0f);
            g.DrawSprite(Dark, room, Color.White, 0f);

            var at = new Vector2(room.X + room.Width * _saturation, room.Y + room.Height * (1f - _value));
            Ring(g, at, MarkerSize + MarkerOutlineThickness * 2f, MarkerOutlineThickness, MarkerOutlineColor);
            Ring(g, at, MarkerSize, MarkerThickness, MarkerColor);
        }

        /// <summary>
        ///     Draws a square outline of width <paramref name="size" /> and line thickness
        ///     <paramref name="thickness" /> in <paramref name="color" />, centered on <paramref name="at" />, using
        ///     <paramref name="g" />.
        /// </summary>
        static void Ring(IGraphics g, Vector2 at, float size, float thickness, Color color)
        {
            var corner = at - new Vector2(size / 2f);
            g.Fill(new RectangleF(corner.X, corner.Y, size, thickness), color);
            g.Fill(new RectangleF(corner.X, corner.Y + size - thickness, size, thickness), color);
            g.Fill(new RectangleF(corner.X, corner.Y, thickness, size), color);
            g.Fill(new RectangleF(corner.X + size - thickness, corner.Y, thickness, size), color);
        }

        /// <summary>Moves the marker to <paramref name="point" />.</summary>
        void Follow(Vector2 point)
        {
            var room = ContentBounds;
            Saturation = room.Width <= 0f ? 0f : (point.X - room.X) / room.Width;
            Value = room.Height <= 0f ? 0f : 1f - (point.Y - room.Y) / room.Height;

            ColorChanged?.Invoke();
        }

        static float Held(float part)
        {
            return part < 0f ? 0f : part > 1f ? 1f : part;
        }
    }
}
