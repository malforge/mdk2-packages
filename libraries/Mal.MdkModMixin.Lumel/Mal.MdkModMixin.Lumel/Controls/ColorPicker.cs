using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A box showing a color, with an arrow at its right, which opens a <see cref="ColorPanel" /> under itself when
    ///     pressed, for the player to make another.
    /// </summary>
    /// <remarks>
    ///     The panel gives the picker its color when Done is pressed under it, or Enter; put away any other way, it gives
    ///     nothing, and the picker goes on showing the color it held.
    /// </remarks>
    public partial class ColorPicker : Element, IPadded
    {
        /// <summary>The state of a color picker while its panel is open.</summary>
        public static readonly State Expanded = State.Declare("ColorPickerExpanded", 300);

        readonly ColorPanel _panel = new ColorPanel();
        readonly Button _done;
        readonly PanelPopup _popup;
        readonly RelayCommand _toggle;
        readonly ColorPalette _fixed = new ColorPalette { IsVisible = false };
        readonly ColorPalette _recent = new ColorPalette { IsVisible = false };
        readonly List<Color> _palette = new List<Color>();

        ColorHistory _history;

        Thickness _padding;
        Vector2 _sampleSize;
        float _arrowSize;
        float _arrowSpacing;

        /// <summary>
        ///     Creates a picker holding no color, with no palettes, using the look its host's theme holds for a color picker
        ///     until it is given a style.
        /// </summary>
        public ColorPicker()
        {
            var done = new RelayCommand(Done);
            _panel.Submit = done;
            _popup = new PanelPopup(new VStack
            {
                Base = { Spacing = 8f },
                Children =
                {
                    // The row of its own keeps the popup as wide as the panel.
                    new HStack { Children = { _panel } },
                    new Button { Text = "Done", Align = Align.Right, Command = done }.As(out _done)
                }
            }, done) { Anchor = this };
            _popup.Opened += () => Set(Expanded, true);
            _popup.Closed += _ => Set(Expanded, false);
            _panel.EntryChanged += () => EntryChanged?.Invoke();
            _panel.Palettes.Add(_fixed);
            _panel.Palettes.Add(_recent);

            _toggle = new RelayCommand(Toggle);
            Command = _toggle;
            Arrow = VRageMath.Color.White;
        }

        /// <summary>Raised when the player gives the picker another color with Done or Enter, but not when <see cref="Color" /> is set.</summary>
        public event Action ColorChanged;

        /// <summary>Raised when the player shows another tab for typing the color in the panel, but not when <see cref="Entry" /> is set.</summary>
        public event Action EntryChanged;

        /// <summary>Gets or sets the style of this picker, or null to use the default its host's theme holds for a color picker.</summary>
        public new IStyle<ColorPicker> Style
        {
            get { return (IStyle<ColorPicker>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this picker's own look, applied in every state on top of the style's look.</summary>
        public new ColorPickerLook Base => (ColorPickerLook)base.Base;

        /// <summary>Gets this picker's own look when hovered, applied on top of the style's look.</summary>
        public new ColorPickerLook PointerOver => (ColorPickerLook)base.PointerOver;

        /// <summary>Gets this picker's own look when disabled, applied on top of the style's look.</summary>
        public new ColorPickerLook Disabled => (ColorPickerLook)base.Disabled;

        /// <summary>
        ///     Gets this picker's own look for <paramref name="state" />, or for every state that includes it, applied on top
        ///     of the style's look.
        /// </summary>
        public new ColorPickerLook this[State state] => (ColorPickerLook)base[state];

        /// <summary>Gets or sets the color, its alpha included, or null for none, which shows the edge of the color alone.</summary>
        public Color? Color { get; set; }

        /// <summary>Gets or sets the way the color is typed in the panel: the tab shown there.</summary>
        public ColorEntry Entry
        {
            get { return _panel.Entry; }
            set { _panel.Entry = value; }
        }

        /// <summary>Gets or sets the words on the button under the panel that gives the picker its color.</summary>
        public string DoneText
        {
            get { return _done.Text; }
            set { _done.Text = value; }
        }

        /// <summary>Gets the colors offered first in the panel, as a player's paint colors are, or none.</summary>
        public IReadOnlyList<Color> Palette => _palette;

        /// <summary>
        ///     Gets or sets the colors chosen last, offered under <see cref="Palette" /> in a row as long as the history holds,
        ///     or null for none.
        /// </summary>
        /// <remarks>A color given with Done or Enter is added to it. It is read each time the panel opens, so pickers may share one.</remarks>
        public ColorHistory Recent
        {
            get { return _history; }
            set
            {
                _history = value;
                ShowRecent();
            }
        }

        /// <summary>Gets the panel the picker opens.</summary>
        /// <remarks>
        ///     Its <see cref="ColorPanel.Submit" /> and palettes are the picker's own. Setting its color while it is open
        ///     changes the color made, which Done then gives.
        /// </remarks>
        public ColorPanel Panel => _panel;

        /// <summary>Whether the panel is open.</summary>
        public bool IsOpen => _popup.IsOpen;

        /// <summary>Gets the width and height of the color shown, in UI units.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public Vector2 SampleSize => _sampleSize;

        /// <summary>Gets the color of the hairline around the color shown; transparent draws none.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public Color SampleEdge { get; private set; }

        /// <summary>Gets the color of the arrow.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public Color Arrow { get; private set; }

        /// <summary>Gets the width and height of the arrow, in UI units.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public float ArrowSize => _arrowSize;

        /// <summary>Gets the least space between the color shown and the arrow, in UI units.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public float ArrowSpacing => _arrowSpacing;

        /// <summary>Gets the style of the popup the panel opens in, or null to use the host theme's popup style.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public IStyle<Popup> PopupStyle { get; private set; }

        /// <summary>Gets the style of the Done button under the panel, or null to use the host theme's button style.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public IStyle<Button> DoneStyle { get; private set; }

        /// <summary>Gets the space between the edge of the box and the color and arrow.</summary>
        /// <remarks>Set by this picker's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>
        ///     Offers <paramref name="colors" /> first in the panel, in place of those it offered, or nothing where it is
        ///     empty.
        /// </summary>
        public void SetPalette(IReadOnlyList<Color> colors)
        {
            _palette.Clear();
            _palette.AddRange(colors);
            _fixed.SetColors(colors);
            _fixed.IsVisible = colors.Count > 0;
        }

        /// <summary>
        ///     Opens the panel under the box, showing the picker's color and the colors chosen last as they stand, unless it
        ///     is open already.
        /// </summary>
        /// <remarks>Throws where the box is on no screen or window.</remarks>
        public void Open()
        {
            if (_popup.IsOpen)
                return;

            _panel.Color = Color;
            ShowRecent();
            _popup.Open();
        }

        /// <summary>Closes the panel if it is open, giving the picker nothing.</summary>
        public void Close()
        {
            _popup.Close();
        }

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Throws where the picker's command was replaced: pressing it opens its panel, and does nothing else.</remarks>
        protected override void OnPressed()
        {
            if (Command != _toggle)
                throw new InvalidOperationException("A color picker has no other command; pressing it opens its panel.");
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ColorPicker>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ColorPickerLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var picker = (ColorPickerLook)look;
            SampleEdge = picker.SampleEdge ?? VRageMath.Color.Transparent;
            Arrow = picker.Arrow ?? VRageMath.Color.White;
            PopupStyle = picker.PopupStyle;
            DoneStyle = picker.DoneStyle;
            _popup.Style = PopupStyle;
            _done.Style = DoneStyle;

            var size = picker.SampleSize ?? Vector2.Zero;
            var arrowSize = picker.ArrowSize ?? 0f;
            var arrowSpacing = picker.ArrowSpacing ?? 0f;
            if (size != _sampleSize || arrowSize != _arrowSize || arrowSpacing != _arrowSpacing)
            {
                _sampleSize = size;
                _arrowSize = arrowSize;
                _arrowSpacing = arrowSpacing;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        /// <remarks>As wide as the color shown and the arrow after it, and as tall as the taller of them.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(_sampleSize.X + Aside(), MathHelper.Max(_sampleSize.Y, _arrowSize));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var sample = new RectangleF(room.X, room.Y + (room.Height - _sampleSize.Y) * 0.5f, room.Width - Aside(), _sampleSize.Y);
            DrawSample(g, sample);

            if (_arrowSize > 0f && Arrow.A > 0)
                Chevrons.Down(g, new Vector2(room.X + room.Width - _arrowSize * 0.5f, room.Y + room.Height * 0.5f), _arrowSize, Arrow);
        }

        /// <summary>Fills <paramref name="sample" /> with the color, inside its hairline where it has one.</summary>
        void DrawSample(IGraphics g, RectangleF sample)
        {
            if (SampleEdge.A > 0)
            {
                g.Fill(new RectangleF(sample.X, sample.Y, sample.Width, 1f), SampleEdge);
                g.Fill(new RectangleF(sample.X, sample.Y + sample.Height - 1f, sample.Width, 1f), SampleEdge);
                g.Fill(new RectangleF(sample.X, sample.Y + 1f, 1f, sample.Height - 2f), SampleEdge);
                g.Fill(new RectangleF(sample.X + sample.Width - 1f, sample.Y + 1f, 1f, sample.Height - 2f), SampleEdge);
                sample = new RectangleF(sample.X + 1f, sample.Y + 1f, sample.Width - 2f, sample.Height - 2f);
            }

            if (Color.HasValue && Color.Value.A > 0)
                g.Fill(sample, Color.Value);
        }

        /// <summary>Returns the width the arrow takes at the right, its spacing included, or 0 without one.</summary>
        float Aside()
        {
            return _arrowSize > 0f ? _arrowSpacing + _arrowSize : 0f;
        }

        /// <summary>Opens the panel if it is closed, and closes it otherwise.</summary>
        void Toggle()
        {
            if (_popup.IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        ///     Takes the panel's color as the player gave it and puts the panel away; where it is another, adds it to the
        ///     colors chosen last and raises <see cref="ColorChanged" />.
        /// </summary>
        void Done()
        {
            var given = _panel.Color;
            _popup.Close();
            if (given == Color)
                return;

            if (given.HasValue)
                _history?.Add(given.Value);

            Color = given;
            ColorChanged?.Invoke();
        }

        /// <summary>Shows the colors chosen last, a place for each the history can hold, or no row where there is none.</summary>
        void ShowRecent()
        {
            _recent.IsVisible = _history != null;
            if (_history == null)
                return;

            _recent.SetColors(new Color[0]);
            _recent.Places = _history.Capacity;
            _recent.SetColors(_history.Colors);
        }
    }
}
