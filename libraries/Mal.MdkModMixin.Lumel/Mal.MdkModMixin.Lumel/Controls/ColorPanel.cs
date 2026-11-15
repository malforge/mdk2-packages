using System;
using System.Globalization;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A color, chosen on a square of saturation and value beside a strip of hues, by typing its hue, saturation and
    ///     value, its red, green and blue, or its code, or from the palettes under them.
    /// </summary>
    /// <remarks>
    ///     The code is written <c>#RRGGBB</c> in capitals, or <c>#RRGGBBAA</c> where the color is not opaque, and read with or
    ///     without the <c>#</c>; an empty code is no color. Only a code changes the alpha.
    /// </remarks>
    public partial class ColorPanel : VStack, IColorPanel
    {
        float _hue;
        float _saturation;
        float _value = 1f;
        byte _alpha = 255;
        ICommand _submit;
        bool _settingEntry;
        bool _chosen = true;

        /// <summary>
        ///     Creates a panel resting on white with no palettes, using the look from the host's theme for a color panel
        ///     until it is given a style.
        /// </summary>
        public ColorPanel()
        {
            CreateView();

            _spectrum.ColorChanged += () => Take(_hue, _spectrum.Saturation, _spectrum.Value);
            _strip.ValueChanged += () => Take(_strip.Hue, _saturation, _value);
            _hueBox.Edited += () => Typed(_hueBox, 360f, part => Take(part, _saturation, _value));
            _saturationBox.Edited += () => Typed(_saturationBox, 100f, part => Take(_hue, part, _value));
            _valueBox.Edited += () => Typed(_valueBox, 100f, part => Take(_hue, _saturation, part));
            _redBox.Edited += () => Typed(_redBox, 255f, part => TakeRgb(new Color(Channel(part), Rgb.G, Rgb.B)));
            _greenBox.Edited += () => Typed(_greenBox, 255f, part => TakeRgb(new Color(Rgb.R, Channel(part), Rgb.B)));
            _blueBox.Edited += () => Typed(_blueBox, 255f, part => TakeRgb(new Color(Rgb.R, Rgb.G, Channel(part))));
            _code.Edited += Reread;
            _entries.SelectionChanged += _ =>
            {
                if (!_settingEntry)
                    EntryChanged?.Invoke();
            };

            Show();
        }

        /// <summary>
        ///     Raised when the player changes the color, by any of the panel's parts, but not when <see cref="Color" /> is
        ///     set.
        /// </summary>
        public event Action ColorChanged;

        /// <summary>Gets or sets the style of this panel, or null to use the default from the host's theme for a color panel.</summary>
        public new IStyle<ColorPanel> Style
        {
            get { return (IStyle<ColorPanel>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets or sets the color, its alpha included, or null for none.</summary>
        /// <remarks>With no color, the panel rests on white and the color made shows <see cref="EmptyFill" /> inside its edge.</remarks>
        public Color? Color
        {
            get
            {
                if (!_chosen)
                    return null;

                var rgb = Rgb;
                rgb.A = _alpha;
                return rgb;
            }
            set
            {
                _chosen = value.HasValue;
                var color = value ?? VRageMath.Color.White;
                var hsv = color.ColorToHSV();
                _hue = hsv.X;
                _saturation = hsv.Y;
                _value = hsv.Z;
                _alpha = color.A;
                Show();
            }
        }

        /// <summary>Gets or sets the tip on the square of saturation and value, or null for none.</summary>
        public Tip SpectrumTip
        {
            get { return _spectrum.Tip; }
            set { _spectrum.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the strip of hues and the box the hue is typed in, or null for none.</summary>
        public Tip HueTip
        {
            get { return _hueBox.Tip; }
            set
            {
                _hueBox.Tip = value;
                _strip.Tip = value;
            }
        }

        /// <summary>Gets or sets the tip on the box the saturation is typed in, or null for none.</summary>
        public Tip SaturationTip
        {
            get { return _saturationBox.Tip; }
            set { _saturationBox.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the box the value is typed in, or null for none.</summary>
        public Tip ValueTip
        {
            get { return _valueBox.Tip; }
            set { _valueBox.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the box the red is typed in, or null for none.</summary>
        public Tip RedTip
        {
            get { return _redBox.Tip; }
            set { _redBox.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the box the green is typed in, or null for none.</summary>
        public Tip GreenTip
        {
            get { return _greenBox.Tip; }
            set { _greenBox.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the box the blue is typed in, or null for none.</summary>
        public Tip BlueTip
        {
            get { return _blueBox.Tip; }
            set { _blueBox.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the box the code is typed in, or null for none.</summary>
        public Tip CodeTip
        {
            get { return _code.Tip; }
            set { _code.Tip = value; }
        }

        /// <summary>Raised when the player shows another tab for typing the color, but not when <see cref="Entry" /> is set.</summary>
        public event Action EntryChanged;

        /// <summary>Gets or sets the way the color is typed: the tab shown beside the color made.</summary>
        public ColorEntry Entry
        {
            get { return (ColorEntry)_entries.SelectedIndex; }
            set
            {
                _settingEntry = true;
                try
                {
                    _entries.SelectedIndex = (int)value;
                }
                finally
                {
                    _settingEntry = false;
                }
            }
        }

        /// <summary>Gets or sets what Enter does in any of the panel's boxes, or null for nothing.</summary>
        public ICommand Submit
        {
            get { return _submit; }
            set
            {
                _submit = value;
                _hueBox.Submit = value;
                _saturationBox.Submit = value;
                _valueBox.Submit = value;
                _redBox.Submit = value;
                _greenBox.Submit = value;
                _blueBox.Submit = value;
                _code.Submit = value;
            }
        }

        /// <summary>
        ///     Puts the box the code is typed in into <paramref name="state" /> where <paramref name="on" /> is true, and takes it
        ///     out otherwise, as an application marks a code it refuses.
        /// </summary>
        public void SetCodeState(State state, bool on)
        {
            _code.Set(state, on);
        }

        /// <summary>Gets the palettes under the panel, top to bottom, whose colors are chosen by pressing them.</summary>
        /// <remarks>A <see cref="ColorPalette" /> added here gives the panel the color pressed in it.</remarks>
        public Collection Palettes => _palettes.Children;

        /// <summary>Gets the color without its alpha, as red, green and blue.</summary>
        Color Rgb => new Vector3(_hue, _saturation, _value).HSVtoColor();

        /// <summary>Gets the color as its code is written.</summary>
        string Code
        {
            get
            {
                var rgb = Rgb;
                var code = "#" + Hex(rgb.R) + Hex(rgb.G) + Hex(rgb.B);
                return _alpha == 255 ? code : code + Hex(_alpha);
            }
        }

        /// <inheritdoc />
        /// <remarks>Takes <paramref name="color" />'s red, green and blue, and keeps the alpha.</remarks>
        void IColorPanel.Picked(Color color)
        {
            TakeRgb(color);
        }

        /// <summary>Gets this panel's own look, applied in every state and layered over the look from its style.</summary>
        public new ColorPanelLook Base => (ColorPanelLook)base.Base;

        /// <summary>Gets this panel's own look applied on hover, layered over the look from its style.</summary>
        public new ColorPanelLook PointerOver => (ColorPanelLook)base.PointerOver;

        /// <summary>Gets this panel's own look applied when disabled, layered over the look from its style.</summary>
        public new ColorPanelLook Disabled => (ColorPanelLook)base.Disabled;

        /// <summary>
        ///     Gets this panel's own look for <paramref name="state" />, or for every state it joins, layered over the look from
        ///     its style.
        /// </summary>
        public new ColorPanelLook this[State state] => (ColorPanelLook)base[state];

        /// <summary>Gets the fill of the color made while there is no color; transparent shows what is behind it.</summary>
        /// <remarks>Set by this panel's looks.</remarks>
        public Color EmptyFill { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ColorPanel>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ColorPanelLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            EmptyFill = ((ColorPanelLook)look).EmptyFill ?? VRageMath.Color.Transparent;
            if (!_chosen)
                _sample.Color = EmptyFill;
        }

        /// <summary>Gives the channel typed in <paramref name="box" />, out of <paramref name="most" />, to <paramref name="take" /> as a part from 0 to 1, where it holds a number.</summary>
        static void Typed(NumberBox box, float most, Action<float> take)
        {
            var typed = box.Value;
            if (double.IsNaN(typed))
                return;

            take((float)Math.Max(0d, Math.Min(most, typed)) / most);
        }

        /// <summary>Returns <paramref name="part" />, from 0 to 1, as a channel of red, green or blue.</summary>
        static byte Channel(float part)
        {
            return (byte)Math.Round(part * 255f);
        }

        /// <summary>Takes <paramref name="color" />'s red, green and blue as the color, keeping the hue where it is a gray.</summary>
        void TakeRgb(Color color)
        {
            var hsv = color.ColorToHSV();
            Take(hsv.Y <= 0f ? _hue : hsv.X, hsv.Y, hsv.Z);
        }

        /// <summary>Takes the color from its hue, saturation and value, shows it in every part and tells whoever listens.</summary>
        void Take(float hue, float saturation, float value)
        {
            _hue = MathHelper.Clamp(hue, 0f, 1f);
            _saturation = MathHelper.Clamp(saturation, 0f, 1f);
            _value = MathHelper.Clamp(value, 0f, 1f);
            _chosen = true;
            Show();
            ColorChanged?.Invoke();
        }

        /// <summary>Takes the code typed as the color where it reads as one, and as no color where it is empty.</summary>
        void Reread()
        {
            if (_code.Text.Trim().Length == 0)
            {
                if (!_chosen)
                    return;

                _chosen = false;
                Show();
                ColorChanged?.Invoke();
                return;
            }

            Color color;
            byte alpha;
            if (!Read(_code.Text, out color, out alpha))
                return;

            _alpha = alpha;
            TakeRgb(color);
        }

        /// <summary>Shows the color in every part, leaving alone a box that is being typed in.</summary>
        void Show()
        {
            _spectrum.Hue = _hue;
            _spectrum.Saturation = _saturation;
            _spectrum.Value = _value;
            _strip.Hue = _hue;

            var rgb = Rgb;
            Say(_hueBox, _hue * 360f);
            Say(_saturationBox, _saturation * 100f);
            Say(_valueBox, _value * 100f);
            Say(_redBox, rgb.R);
            Say(_greenBox, rgb.G);
            Say(_blueBox, rgb.B);
            if (!_code.IsFocused)
                _code.Text = _chosen ? Code : "";

            rgb.A = _alpha;
            _sample.Color = _chosen ? rgb : EmptyFill;
        }

        /// <summary>Writes <paramref name="number" />, rounded, in <paramref name="box" />, unless it is being typed in.</summary>
        static void Say(NumberBox box, float number)
        {
            if (!box.IsFocused)
                box.Value = Math.Round(number);
        }

        /// <summary>
        ///     Reads <paramref name="code" />, six or eight hex digits after an optional <c>#</c>, as a color and its alpha,
        ///     and returns true if it reads as one.
        /// </summary>
        static bool Read(string code, out Color color, out byte alpha)
        {
            color = default(Color);
            alpha = 255;
            var text = code.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal))
                text = text.Substring(1);

            uint number;
            if ((text.Length != 6 && text.Length != 8) || !uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out number))
                return false;

            if (text.Length == 8)
            {
                alpha = (byte)(number & 0xFF);
                number >>= 8;
            }

            color = new Color((byte)(number >> 16), (byte)(number >> 8), (byte)number);
            return true;
        }

        static string Hex(byte channel)
        {
            return channel.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
