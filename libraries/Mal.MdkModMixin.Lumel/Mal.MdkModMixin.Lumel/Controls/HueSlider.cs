using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A vertical strip showing every hue around the color wheel, with a marker at the chosen hue: red at the top
    ///     and again at the bottom.
    /// </summary>
    public class HueSlider : Element
    {
        /// <summary>The thickness of the marker's bar in UI units when no look sets it.</summary>
        public const float NeutralMarkerThickness = 2f;

        /// <summary>The thickness of the outline around the marker in UI units when no look sets it.</summary>
        public const float NeutralMarkerOutlineThickness = 2f;

        float _hue;

        /// <summary>
        ///     Creates a slider set to red, using the look from the host's theme for a hue slider until it is given a
        ///     style.
        /// </summary>
        public HueSlider()
        {
            MarkerThickness = NeutralMarkerThickness;
            MarkerOutlineThickness = NeutralMarkerOutlineThickness;
            MarkerColor = Color.White;
            MarkerOutlineColor = Color.Black;
        }

        /// <summary>
        ///     Raised when the pointer moves the marker, but not when the marker is moved by setting
        ///     <see cref="Hue" />.
        /// </summary>
        public event Action ValueChanged;

        /// <summary>
        ///     Gets or sets the style of this slider, or null to use the default from the host's theme for a hue
        ///     slider.
        /// </summary>
        public new IStyle<HueSlider> Style
        {
            get { return (IStyle<HueSlider>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this slider's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new HueSliderLook Base => (HueSliderLook)base.Base;

        /// <summary>Gets this slider's own look applied on hover, layered over the look from its style.</summary>
        public new HueSliderLook PointerOver => (HueSliderLook)base.PointerOver;

        /// <summary>Gets this slider's own look applied when disabled, layered over the look from its style.</summary>
        public new HueSliderLook Disabled => (HueSliderLook)base.Disabled;

        /// <summary>
        ///     Gets this slider's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new HueSliderLook this[State state] => (HueSliderLook)base[state];

        /// <summary>Gets the picture of every hue in order, from red at the top around to red at the bottom.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public MyStringId Hues { get; private set; }

        /// <summary>Gets the thickness of the marker's bar, in UI units.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public float MarkerThickness { get; private set; }

        /// <summary>
        ///     Gets the thickness of the outline around the marker, which is also how far the outline extends past the
        ///     sides of the strip, in UI units.
        /// </summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public float MarkerOutlineThickness { get; private set; }

        /// <summary>Gets the color of the marker.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public Color MarkerColor { get; private set; }

        /// <summary>
        ///     Gets the color of the outline around the marker, which keeps the marker visible on any hue.
        /// </summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public Color MarkerOutlineColor { get; private set; }

        /// <summary>Gets or sets the position of the marker, from 0 at the top to 1 at the bottom.</summary>
        public float Hue
        {
            get { return _hue; }
            set { _hue = value < 0f ? 0f : value > 1f ? 1f : value; }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<HueSlider>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new HueSliderLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var slider = (HueSliderLook)look;
            Hues = slider.Hues ?? MyStringId.NullOrEmpty;
            MarkerThickness = slider.MarkerThickness ?? NeutralMarkerThickness;
            MarkerOutlineThickness = slider.MarkerOutlineThickness ?? NeutralMarkerOutlineThickness;
            MarkerColor = slider.MarkerColor ?? Color.White;
            MarkerOutlineColor = slider.MarkerOutlineColor ?? Color.Black;
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
            g.DrawSprite(Hues, room, Color.White, 0f);

            var y = room.Y + room.Height * _hue;
            var outline = MarkerOutlineThickness;
            var outer = MarkerThickness + outline * 2f;
            g.Fill(new RectangleF(room.X - outline, y - outer * 0.5f, room.Width + outline * 2f, outer), MarkerOutlineColor);
            g.Fill(new RectangleF(room.X, y - MarkerThickness * 0.5f, room.Width, MarkerThickness), MarkerColor);
        }

        /// <summary>Moves the marker to <paramref name="point" />.</summary>
        void Follow(Vector2 point)
        {
            var room = ContentBounds;
            Hue = room.Height <= 0f ? 0f : (point.Y - room.Y) / room.Height;

            ValueChanged?.Invoke();
        }
    }
}
