using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A control that sets a single value between a minimum and a maximum by moving a thumb along a track.
    /// </summary>
    public class Slider : Element
    {
        const float Thick = 24f;
        const float NeutralThumbWidth = 8f;

        /// <summary>The state of a slider whose thumb the pointer is moving.</summary>
        public static readonly State Dragging = State.Declare("Dragging", 400);

        float _value;
        float _minimum;
        float _maximum = 1f;
        float _smallChange = 0.1f;

        /// <summary>
        ///     Creates a slider ranging from 0 to 1 and set to 0, using the look from the host's theme for a slider
        ///     until it is given a style.
        /// </summary>
        public Slider()
        {
            TrackTint = Color.White;
            Thumb = Color.White;
            ThumbWidth = NeutralThumbWidth;
            Focusable = true;
        }

        /// <summary>
        ///     Raised when the pointer or the keys move the thumb, but not when the thumb is moved by setting
        ///     <see cref="Value" />.
        /// </summary>
        public event Action ValueChanged;

        /// <summary>
        ///     Gets or sets the style of this slider, or null to use the default from the host's theme for a slider.
        /// </summary>
        public new IStyle<Slider> Style
        {
            get { return (IStyle<Slider>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this slider's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new SliderLook Base => (SliderLook)base.Base;

        /// <summary>Gets this slider's own look applied on hover, layered over the look from its style.</summary>
        public new SliderLook PointerOver => (SliderLook)base.PointerOver;

        /// <summary>Gets this slider's own look applied when disabled, layered over the look from its style.</summary>
        public new SliderLook Disabled => (SliderLook)base.Disabled;

        /// <summary>
        ///     Gets this slider's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new SliderLook this[State state] => (SliderLook)base[state];

        /// <summary>
        ///     Gets or sets the value, which places the thumb from <see cref="Minimum" /> at the left to
        ///     <see cref="Maximum" /> at the right.
        /// </summary>
        public float Value
        {
            get { return _value; }
            set { _value = Held(value); }
        }

        /// <summary>Gets or sets the value at the left end of the track.</summary>
        /// <remarks>A minimum above <see cref="Maximum" /> raises the maximum to it, as WPF's <c>RangeBase</c> does.</remarks>
        public float Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                if (_maximum < value)
                    _maximum = value;

                _value = Held(_value);
            }
        }

        /// <summary>
        ///     Gets or sets the value at the right end of the track, which is never below <see cref="Minimum" />.
        /// </summary>
        /// <remarks>A maximum below <see cref="Minimum" /> is taken as the minimum, as WPF's <c>RangeBase</c> does.</remarks>
        public float Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value < _minimum ? _minimum : value;
                _value = Held(_value);
            }
        }

        /// <summary>Gets or sets how far Left and Right move the value while the slider has the keyboard; 0.1 by default, as WPF's.</summary>
        public float SmallChange
        {
            get { return _smallChange; }
            set { _smallChange = value; }
        }

        /// <summary>Gets the picture the track is drawn with, or the empty id for a plain fill.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public MyStringId Track { get; private set; }

        /// <summary>
        ///     Gets the tint applied to the track, which is white for a picture that already has its own colors.
        /// </summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public Color TrackTint { get; private set; }

        /// <summary>
        ///     Gets the height of the track, centered vertically in the slider, or 0 for the slider's full height.
        /// </summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public float TrackHeight { get; private set; }

        /// <summary>Gets the color laid over the track from its left end to the middle of the thumb.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public Color Fill { get; private set; }

        /// <summary>Gets the fill color of the thumb.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public Color Thumb { get; private set; }

        /// <summary>Gets the width of the thumb.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public float ThumbWidth { get; private set; }

        /// <summary>Gets the height of the thumb, centered vertically in the slider, or 0 for the slider's full height.</summary>
        /// <remarks>Set by this slider's looks.</remarks>
        public float ThumbHeight { get; private set; }

        /// <summary>Whether the pointer is moving the thumb.</summary>
        /// <remarks>While it is, the slider shows its looks for <see cref="Dragging" />.</remarks>
        public bool IsDragging
        {
            get { return Is(Dragging); }
            private set { Set(Dragging, value); }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Slider>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new SliderLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var slider = (SliderLook)look;
            Track = slider.Track ?? MyStringId.NullOrEmpty;
            TrackTint = slider.TrackTint ?? Color.White;
            TrackHeight = slider.TrackHeight ?? 0f;
            Fill = slider.Fill ?? Color.Transparent;
            Thumb = slider.Thumb ?? Color.White;
            ThumbWidth = slider.ThumbWidth ?? NeutralThumbWidth;
            ThumbHeight = slider.ThumbHeight ?? 0f;
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
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button == PointerButton.Left)
                IsDragging = false;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            IsDragging = false;
        }

        /// <inheritdoc />
        /// <remarks>Left and Right move the value by <see cref="SmallChange" />.</remarks>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (key != MyKeys.Left && key != MyKeys.Right)
                return false;

            var moved = Held(_value + (key == MyKeys.Left ? -_smallChange : _smallChange));
            if (moved != _value)
            {
                _value = moved;
                ValueChanged?.Invoke();
            }

            return true;
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            Follow(point);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Thick, Thick);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var track = TrackHeight > 0f && TrackHeight < room.Height
                ? new RectangleF(room.X, room.Y + (room.Height - TrackHeight) * 0.5f, room.Width, TrackHeight)
                : room;
            if (Track == MyStringId.NullOrEmpty)
                g.Fill(track, TrackTint);
            else
                g.DrawSprite(Track, track, TrackTint, 0f);

            var left = room.X + (room.Width - ThumbWidth) * Fraction;
            if (Fill.A > 0)
                g.Fill(new RectangleF(track.X, track.Y, left + ThumbWidth * 0.5f - track.X, track.Height), Fill);

            var high = ThumbHeight > 0f && ThumbHeight < room.Height ? ThumbHeight : room.Height;
            g.Fill(new RectangleF(left, room.Y + (room.Height - high) * 0.5f, ThumbWidth, high), Thumb);
        }

        /// <summary>
        ///     Gets how far along the track the value is, from 0 at <see cref="Minimum" /> to 1 at
        ///     <see cref="Maximum" />.
        /// </summary>
        float Fraction => _maximum > _minimum ? (_value - _minimum) / (_maximum - _minimum) : 0f;

        /// <summary>
        ///     Moves the thumb to <paramref name="point" />, measured over the thumb's travel so both ends can be
        ///     reached.
        /// </summary>
        void Follow(Vector2 point)
        {
            var room = ContentBounds;
            var travel = room.Width - ThumbWidth;
            var fraction = travel <= 0f ? 0f : (point.X - (room.X + ThumbWidth / 2f)) / travel;
            fraction = fraction < 0f ? 0f : fraction > 1f ? 1f : fraction;
            Value = _minimum + fraction * (_maximum - _minimum);

            ValueChanged?.Invoke();
        }

        /// <summary>
        ///     Returns <paramref name="value" /> limited to the range from <see cref="Minimum" /> to
        ///     <see cref="Maximum" />.
        /// </summary>
        float Held(float value)
        {
            return value < _minimum ? _minimum : value > _maximum ? _maximum : value;
        }
    }
}
