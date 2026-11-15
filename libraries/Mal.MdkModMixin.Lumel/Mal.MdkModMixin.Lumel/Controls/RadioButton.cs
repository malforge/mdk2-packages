using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A ring that is one of a <see cref="RadioGroup" />, with text beside it describing what it stands for; pressing
    ///     either one checks it, which shows a dot in the ring and unchecks the button of the group checked before.
    /// </summary>
    /// <remarks>
    ///     The player never unchecks a button, only checks another. A button with no group is checked by a press, and
    ///     stays checked.
    /// </remarks>
    public class RadioButton : Element, IPadded, ITextual
    {
        /// <summary>The state of a radio button while it is checked.</summary>
        public static readonly State Checked = State.Declare("RadioChecked", 60);

        // The button's words take no look but the one the button gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        readonly Label _word = new Label { Style = Bare, TextWrapping = TextWrapping.Trim };
        readonly RelayCommand _check;

        RadioGroup _group;
        Thickness _padding;
        float? _markSize;

        /// <summary>
        ///     Creates an unchecked radio button with no text and no group, using the look from the host's theme for a radio
        ///     button until it is given a style.
        /// </summary>
        public RadioButton()
        {
            var children = CreateChildren();
            children.Add(_word);
            _word.IsVisible = false;

            _check = new RelayCommand(Check);
            Command = _check;

            Font = VRage.Game.MyFontEnum.White;
            FontSize = TextLook.NeutralFontSize;
            Color = Color.White;
            Ring = Color.White;
            Dot = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this radio button, or null to use the default from the host's theme for a radio
        ///     button.
        /// </summary>
        public new IStyle<RadioButton> Style
        {
            get { return (IStyle<RadioButton>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this radio button's own look, applied in every state and layered over the look from its style.</summary>
        public new RadioButtonLook Base => (RadioButtonLook)base.Base;

        /// <summary>Gets this radio button's own look applied on hover, layered over the look from its style.</summary>
        public new RadioButtonLook PointerOver => (RadioButtonLook)base.PointerOver;

        /// <summary>Gets this radio button's own look applied when disabled, layered over the look from its style.</summary>
        public new RadioButtonLook Disabled => (RadioButtonLook)base.Disabled;

        /// <summary>
        ///     Gets this radio button's own look for <paramref name="state" />, or for every state it joins, layered over
        ///     the look from its style.
        /// </summary>
        public new RadioButtonLook this[State state] => (RadioButtonLook)base[state];

        /// <summary>Gets or sets the group this button is one of, or null for none.</summary>
        /// <remarks>A checked button joining a group unchecks the one checked there before; leaving it, it stays checked.</remarks>
        public RadioGroup Group
        {
            get { return _group; }
            set
            {
                if (value == _group)
                    return;

                var was = _group;
                _group = value;
                if (was != null && was.Chosen == this)
                {
                    was.Chosen = null;
                    Set(Checked, true);
                }

                if (value != null && IsChecked)
                    value.Chosen = this;
            }
        }

        /// <summary>Whether the button is checked.</summary>
        /// <remarks>Checking a button of a group unchecks the one checked before, and tells nobody.</remarks>
        public bool IsChecked
        {
            get { return Is(Checked); }
            set
            {
                if (_group == null)
                    Set(Checked, value);
                else if (value)
                    _group.Chosen = this;
                else if (_group.Chosen == this)
                    _group.Chosen = null;
            }
        }

        /// <summary>Gets or sets the text beside the ring, or empty to show the ring alone.</summary>
        public string Text
        {
            get { return _word.Text; }
            set
            {
                _word.Text = value;
                _word.IsVisible = !string.IsNullOrEmpty(value);
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the font the text is drawn in.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public string Font { get; private set; }

        /// <summary>Gets the height of a line of text, in UI units.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public float FontSize { get; private set; }

        /// <summary>Gets the color of the text.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the space around the text.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public Thickness WordPadding { get; private set; }

        /// <summary>Gets the width and height of the ring in UI units, or null to make it as tall as a line of text.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public float? MarkSize
        {
            get { return _markSize; }
            private set
            {
                if (_markSize == value)
                    return;

                _markSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the color of the ring.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public Color Ring { get; private set; }

        /// <summary>Gets the fill color of the inside of the ring, or transparent to show what is behind it.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public Color RingFill { get; private set; }

        /// <summary>Gets the color of the dot that shows the button as checked.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public Color Dot { get; private set; }

        /// <summary>Gets the space between the edge of the radio button and its ring and text.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        float Side => _markSize ?? FontSize;

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Throws where the button's command was replaced: pressing it checks it, and does nothing else.</remarks>
        protected override void OnPressed()
        {
            if (Command != _check)
                throw new InvalidOperationException("A radio button has no other command; pressing it checks it.");
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<RadioButton>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new RadioButtonLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var radio = (RadioButtonLook)look;
            Font = radio.Font ?? VRage.Game.MyFontEnum.White;
            FontSize = radio.FontSize ?? TextLook.NeutralFontSize;
            Color = radio.Color ?? Color.White;
            WordPadding = radio.WordPadding ?? default(Thickness);
            MarkSize = radio.MarkSize;
            Ring = radio.Ring ?? Color.White;
            RingFill = radio.RingFill ?? Color.Transparent;
            Dot = radio.Dot ?? Color.White;

            var words = _word.Base;
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
            var side = Side;
            if (!_word.IsVisible)
                return new Vector2(side, side);

            _word.Measure(g, new Vector2(available.X - side, available.Y));
            var words = _word.DesiredSize;
            return new Vector2(side + words.X, words.Y > side ? words.Y : side);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var side = Side;
            if (_word.IsVisible)
                _word.Arrange(new RectangleF(bounds.X + side, bounds.Y, bounds.Width - side, bounds.Height));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var side = Side;
            var ring = new RectangleF(room.X, room.Y + (room.Height - side) * 0.5f, side, side);

            if (RingFill.A > 0)
                g.DrawSprite(LumelMaterials.Disc, ring, RingFill, 0f);

            if (Ring.A > 0)
                g.DrawSprite(LumelMaterials.Ring, ring, Ring, 0f);

            // The dot stands a quarter of the ring in from each edge, as a check box's mark does.
            if (IsChecked)
            {
                var inset = side * 0.25f;
                g.DrawSprite(LumelMaterials.Disc, new RectangleF(ring.X + inset, ring.Y + inset, side - inset * 2f, side - inset * 2f), Dot, 0f);
            }

            if (_word.IsVisible)
                _word.Draw(g);
        }

        /// <summary>Checks the button, as the player pressed it, telling its group where it has one.</summary>
        void Check()
        {
            if (_group == null)
                Set(Checked, true);
            else
                ((IRadioGroup)_group).Picked(this);
        }
    }
}
