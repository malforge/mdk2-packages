using Lumel.Drawing;
using Lumel.Motion;
using Lumel.Text;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>An on/off switch: a thumb at one end, with a word beside it for the current state.</summary>
    /// <remarks>
    ///     The body is the element's own <see cref="Element.Background" />, and the room round the thumb and the word
    ///     its <see cref="Padding" />.
    /// </remarks>
    public class Switch : Element, IPadded, ITextual
    {
        /// <summary>The state of a switch that is on.</summary>
        public static readonly State On = State.Declare("On", 50);

        /// <summary>How many game ticks the thumb takes to slide from one end to the other.</summary>
        public const int SlideTicks = 8;

        // The switch's words take no look but the one the switch gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        Thickness _padding;
        readonly Label _off = Word("OFF");
        readonly Label _on = Word("ON");

        // Where the thumb is, from 0 at the left to 1 at the right: sliding from _from to _to as _slide runs.
        readonly Transition _slide = new Transition(SlideTicks, Easing.EaseOut);
        float _from;
        float _to;
        bool _drawn;

        // The track of the state the switch last flipped from, which stays on the side the thumb is leaving.
        Color _trackBefore;
        bool _appliedOn;

        /// <summary>
        ///     Creates a switch that is off, using the look its host's theme holds for a switch until it is given a
        ///     style.
        /// </summary>
        public Switch()
        {
            var children = CreateChildren();
            children.Add(_on);
            children.Add(_off);

            Font = VRage.Game.MyFontEnum.White;
            FontSize = TextLook.NeutralFontSize;
            Color = Color.White;
            Thumb = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this switch, or null to use the default its host's theme holds for a switch.
        /// </summary>
        public new IStyle<Switch> Style
        {
            get { return (IStyle<Switch>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>The look this switch sets itself for every state, applied over its style's.</summary>
        public new SwitchLook Base => (SwitchLook)base.Base;

        /// <summary>
        ///     The look this switch sets itself for when the pointer is over it, applied over its style's.
        /// </summary>
        public new SwitchLook PointerOver => (SwitchLook)base.PointerOver;

        /// <summary>The look this switch sets itself for when it is disabled, applied over its style's.</summary>
        public new SwitchLook Disabled => (SwitchLook)base.Disabled;

        /// <summary>
        ///     The look this switch sets itself for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style's.
        /// </summary>
        public new SwitchLook this[State state] => (SwitchLook)base[state];

        /// <summary>Whether the switch is on.</summary>
        /// <remarks>While it is on, it shows its looks for <see cref="On" />.</remarks>
        public bool IsOn
        {
            get { return Is(On); }
            set { Set(On, value); }
        }

        /// <summary>The font the words are drawn in.</summary>
        /// <remarks>Set by this switch's looks.</remarks>
        public string Font { get; private set; }

        /// <summary>The height of a line of the words, in UI units.</summary>
        /// <remarks>Set by this switch's looks.</remarks>
        public float FontSize { get; private set; }

        /// <summary>The space kept clear around the word.</summary>
        /// <remarks>Set by this switch's looks.</remarks>
        public Thickness WordPadding { get; private set; }

        /// <summary>The color of the words.</summary>
        /// <remarks>Set by this switch's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>The fill of the area inside the switch, or transparent to show the body.</summary>
        /// <remarks>Set by this switch's looks.</remarks>
        public Color Track { get; private set; }

        /// <summary>The color of the thumb.</summary>
        /// <remarks>Set by this switch's looks.</remarks>
        public Color Thumb { get; private set; }

        /// <summary>Gets or sets the word shown when the switch is on.</summary>
        public string OnText
        {
            get { return _on.Text; }
            set { _on.Text = value; }
        }

        /// <summary>Gets or sets the word shown when the switch is off.</summary>
        public string OffText
        {
            get { return _off.Text; }
            set { _off.Text = value; }
        }

        /// <summary>The space kept clear between the edge and the thumb and word.</summary>
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
            return style is IStyle<Switch>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new SwitchLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var before = Track;
            var body = (SwitchLook)look;
            Font = body.Font ?? VRage.Game.MyFontEnum.White;
            FontSize = body.FontSize ?? TextLook.NeutralFontSize;
            Color = body.Color ?? Color.White;
            WordPadding = body.WordPadding ?? default(Thickness);
            Track = body.Track ?? Color.Transparent;
            Thumb = body.Thumb ?? Color.White;

            if (IsOn != _appliedOn)
            {
                _appliedOn = IsOn;
                _trackBefore = before;
            }

            Dress(_on.Base);
            Dress(_off.Base);
        }

        /// <summary>
        ///     Gives <paramref name="words" /> the switch's font, size, color and word padding, changing only what
        ///     differs.
        /// </summary>
        void Dress(TextLook words)
        {
            if (words.Font != Font)
                words.Font = Font;

            if (words.FontSize != FontSize)
                words.FontSize = FontSize;

            if (words.Color != Color)
                words.Color = Color;

            if (words.Padding != WordPadding)
                words.Padding = WordPadding;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _on.Measure(g, available);
            _off.Measure(g, available);

            var tall = _on.DesiredSize.Y > _off.DesiredSize.Y ? _on.DesiredSize.Y : _off.DesiredSize.Y;
            var wide = _on.DesiredSize.X > _off.DesiredSize.X ? _on.DesiredSize.X : _off.DesiredSize.X;

            // The thumb is square, so it takes as much room across as the words take down.
            return new Vector2(wide + tall, tall);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            // Centered here from the measured width: a label centering itself measures on its first draw, which for a
            // switch's other word is the frame it is flipped.
            var thumb = bounds.Height;
            var room = bounds.Width - thumb;
            Center(_on, bounds.X, room, bounds);
            Center(_off, bounds.X + thumb, room, bounds);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var inside = ContentBounds;
            var thumb = inside.Height;
            var room = inside.Width - thumb;
            var left = inside.X + room * Position(g);

            if (!_slide.IsRunning)
            {
                if (Track.A > 0)
                    g.Fill(inside, Track);

                g.Fill(new RectangleF(left, inside.Y, thumb, thumb), Thumb);
                (IsOn ? _on : _off).Draw(g);
                return;
            }

            // Thumb, words and tracks slide as one strip: each word rides beside the thumb, each track behind its word,
            // and the strip is cut off at the inside's edges.
            g.PushClip(inside);

            var middle = left + thumb * 0.5f;
            var onTrack = IsOn ? Track : _trackBefore;
            var offTrack = IsOn ? _trackBefore : Track;
            if (onTrack.A > 0)
                g.Fill(new RectangleF(inside.X, inside.Y, middle - inside.X, thumb), onTrack);

            if (offTrack.A > 0)
                g.Fill(new RectangleF(middle, inside.Y, inside.X + inside.Width - middle, thumb), offTrack);

            g.Fill(new RectangleF(left, inside.Y, thumb, thumb), Thumb);
            DrawMoved(g, _on, left - (inside.X + room));
            DrawMoved(g, _off, left - inside.X);

            g.PopClip();
        }

        /// <summary>
        ///     Returns the thumb's position in this frame, from 0 at the left to 1 at the right, and starts a slide if
        ///     the switch has flipped since the last frame.
        /// </summary>
        /// <remarks>The first frame drawn shows the thumb at rest where the switch's state puts it.</remarks>
        float Position(IGraphics g)
        {
            var to = IsOn ? 1f : 0f;
            if (to != _to)
            {
                _from = _drawn ? MathHelper.Lerp(_from, _to, _slide.Progress(g)) : to;
                _to = to;
                if (_drawn)
                    _slide.Start();
            }

            _drawn = true;
            return MathHelper.Lerp(_from, _to, _slide.Progress(g));
        }

        /// <summary>
        ///     Draws <paramref name="word" /> shifted <paramref name="across" /> units to the right of its laid-out
        ///     position.
        /// </summary>
        static void DrawMoved(IGraphics g, Label word, float across)
        {
            g.PushTransform(RenderTransform.Moved(new Vector2(across, 0f)), Vector2.Zero);
            word.Draw(g);
            g.PopTransform();
        }

        /// <summary>
        ///     Arranges <paramref name="word" /> in the middle of the space of width <paramref name="room" /> that
        ///     starts at <paramref name="left" />.
        /// </summary>
        static void Center(Label word, float left, float room, RectangleF bounds)
        {
            var wide = word.DesiredSize.X;
            word.Arrange(new RectangleF(left + (room - wide) * 0.5f, bounds.Y, wide, bounds.Height));
        }

        static Label Word(string text)
        {
            var word = new Label { Style = Bare };
            word.Text = text;
            return word;
        }
    }
}
