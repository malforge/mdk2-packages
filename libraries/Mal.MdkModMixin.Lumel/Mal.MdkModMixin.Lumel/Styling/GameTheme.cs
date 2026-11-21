using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Menus;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Styling
{
    /// <summary>The game's own look for each control, which a host shows when it is given no theme.</summary>
    /// <remarks>
    ///     An application changes a kind's look by giving its host a theme based on <see cref="Theme" />, or one control's by
    ///     giving it a style, often based on one of these, as <c>new ButtonStyle(GameTheme.Button) { ... }</c>.
    /// </remarks>
    public static class GameTheme
    {
        static readonly NineSlice Shade = new NineSlice(LumelMaterials.Shadow, 64, 24);

        /// <summary>The game's sound as the pointer comes onto one of its controls.</summary>
        public static readonly Sound HoverSound = new Sound("HudMouseOver");

        /// <summary>The game's sound as one of its controls is pressed.</summary>
        public static readonly Sound ClickSound = new Sound("HudMouseClick");

        /// <summary>
        ///     The shadow of a window that floats over the screen: wide and soft, falling down and to the right.
        /// </summary>
        public static readonly Shadow WindowShadow = new Shadow(Shade, 24f, new Vector2(6f, 10f), GameColors.Shadow);

        /// <summary>The shadow of a dialog, popup or tip: short and close beneath it.</summary>
        public static readonly Shadow ShortShadow = new Shadow(Shade, 8f, new Vector2(2f, 4f), GameColors.Shadow);

        /// <summary>The style for a label: a line of the game's ink.</summary>
        public static readonly IStyle<Label> Label = new LabelStyle
        {
            Base = { Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink }
        };

        /// <summary>
        ///     The style for rich text, in the same ink as a label: bold and italic in the game's faces from the font
        ///     styles mod, spaced paragraphs, code on a faint white wash, and link underlines just under the text.
        /// </summary>
        /// <remarks>Strong and emphasized words fall back to the game's own face where the font styles mod is not loaded.</remarks>
        public static readonly IStyle<RichText> RichText = new RichTextStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, StrongFont = "SpaceEngineersBold", EmphasisFont = "SpaceEngineersItalic",
                StrongEmphasisFont = "SpaceEngineersBoldItalic", BlockSpacing = 12f, CodeBackground = new Color(255, 255, 255, 10),
                CodePadding = new Thickness(4f, -1f), UnderlineOffset = 0.85f
            }
        };

        /// <summary>
        ///     The style for a glyph: a symbol in the same ink as text, in the application's font since Lumel has no
        ///     symbols of its own.
        /// </summary>
        public static readonly IStyle<Glyph> Glyph = new GlyphStyle
        {
            Base = { FontSize = 20f, Tint = GameColors.Ink, HoverSound = HoverSound, ClickSound = ClickSound }
        };

        /// <summary>
        ///     The style for the game's button: a panel with an edge, lifted on hover and while its focus shows,
        ///     highlighted while pressed, with dimmed text when disabled.
        /// </summary>
        public static readonly IStyle<Button> Button = new ButtonStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 16f, Color = GameColors.Ink, Background = GameColors.Panel, Border = GameColors.ControlEdge, Padding = new Thickness(16f, 8f),
                HoverSound = HoverSound, ClickSound = ClickSound
            },
            PointerOver = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot },
            [State.FocusVisible] = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot },
            Pressed = { Background = GameColors.Line },
            Disabled = { Color = GameColors.Dim }
        };

        /// <summary>
        ///     The style for the default button, the one Enter presses: accent colored, highlighted on hover and while
        ///     its focus shows, plain when disabled.
        /// </summary>
        /// <remarks>A second look for a button, which a view assigns; the theme's button is <see cref="Button" />.</remarks>
        public static readonly IStyle<Button> PrimaryButton = new ButtonStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 16f, Color = GameColors.Chrome, Background = GameColors.Accent, Padding = new Thickness(16f, 8f), HoverSound = HoverSound,
                ClickSound = ClickSound
            },
            PointerOver = { Background = GameColors.Ink },
            [State.FocusVisible] = { Background = GameColors.Ink },
            Disabled = { Color = GameColors.Dim, Background = GameColors.Panel }
        };

        /// <summary>
        ///     The style for a check box shaped like the game's, with an edge and mark bright enough to read on a
        ///     panel, highlighted on hover and while its focus shows, and dimmed when disabled.
        /// </summary>
        public static readonly IStyle<CheckBox> CheckBox = new CheckBoxStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, Box = GameColors.Muted, BoxThickness = 2f, BoxFill = GameColors.Panel, Mark = GameColors.Ink,
                BoxSize = 16f, WordPadding = new Thickness(12f, 0f), HoverSound = HoverSound, ClickSound = ClickSound
            },
            PointerOver = { Box = GameColors.Ink, BoxFill = GameColors.PanelHot, Mark = Color.White },
            [State.FocusVisible] = { Box = GameColors.Ink, BoxFill = GameColors.PanelHot, Mark = Color.White },
            Disabled = { Color = GameColors.Dim, Box = GameColors.Dim, Mark = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a radio button, a round check box: the same edge, fill and mark, highlighted on hover and while
        ///     its focus shows, and dimmed when disabled.
        /// </summary>
        public static readonly IStyle<RadioButton> RadioButton = new RadioButtonStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, Ring = GameColors.Muted, RingFill = GameColors.Panel, Dot = GameColors.Ink, MarkSize = 16f,
                WordPadding = new Thickness(12f, 0f), HoverSound = HoverSound, ClickSound = ClickSound
            },
            PointerOver = { Ring = GameColors.Ink, RingFill = GameColors.PanelHot, Dot = Color.White },
            [State.FocusVisible] = { Ring = GameColors.Ink, RingFill = GameColors.PanelHot, Dot = Color.White },
            Disabled = { Color = GameColors.Dim, Ring = GameColors.Dim, Dot = GameColors.Dim }
        };

        /// <summary>
        ///     The style for an on/off switch: a square thumb and the word, sunk into a dark track while off, and edged
        ///     on hover and while its focus shows.
        /// </summary>
        public static readonly IStyle<Switch> Switch = new SwitchStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 16f, Color = GameColors.Ink, Background = GameColors.Line, Track = GameColors.Well, Thumb = GameColors.Ink,
                WordPadding = new Thickness(8f, 4f), Padding = new Thickness(4f), HoverSound = HoverSound, ClickSound = ClickSound
            },
            On = { Track = Color.Transparent },
            PointerOver = { Border = GameColors.ControlEdgeHot },
            [State.FocusVisible] = { Border = GameColors.ControlEdgeHot },
            Disabled = { Color = GameColors.Dim, Thumb = GameColors.Dim, Background = GameColors.Well, Track = GameColors.Well }
        };

        /// <summary>
        ///     The style for the header of one page among several, as the game draws its terminal's tabs, lifted on
        ///     hover and while its focus shows, with the showing page on the lightest plate.
        /// </summary>
        public static readonly IStyle<Tab> Tab = new TabStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Muted, Background = GameColors.Panel, Border = GameColors.Line, Padding = new Thickness(8f, 8f),
                HoverSound = HoverSound, ClickSound = ClickSound
            },
            PointerOver = { Background = GameColors.PanelHot, Color = GameColors.Ink },
            [State.FocusVisible] = { Background = GameColors.PanelHot, Color = GameColors.Ink },
            Selected = { Background = GameColors.Line, Color = GameColors.Ink }
        };

        /// <summary>
        ///     The style for the minus and plus buttons of a spin box: a button's panel, as tall as a number box beside it.
        /// </summary>
        /// <remarks>A second look for a button, which a spin box's style assigns; the theme's button is <see cref="Button" />.</remarks>
        public static readonly IStyle<Button> StepButton = new ButtonStyle(Button)
        {
            Base = { FontSize = 20f, Padding = new Thickness(12f, 8f) }
        };

        /// <summary>The style for a spin box: a number box between its minus and plus buttons, close beside it.</summary>
        public static readonly IStyle<SpinBox> SpinBox = new SpinBoxStyle
        {
            Base = { Spacing = 4f, StepStyle = StepButton }
        };

        /// <summary>
        ///     The style for a box a line is typed into: a page-dark interior with an edge that highlights while it has
        ///     the keyboard.
        /// </summary>
        public static readonly IStyle<TextBox> TextBox = new TextBoxStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, CaretColor = GameColors.Ink, SelectionColor = GameColors.Line, HintColor = GameColors.Faint,
                Background = GameColors.Page, Border = GameColors.Line, Padding = new Thickness(12f, 8f)
            },
            Focused = { Border = GameColors.ControlEdgeHot },
            Disabled = { Color = GameColors.Dim }
        };

        /// <summary>
        ///     The style for words with tokens among them: a text box's look, with room on each side of a token.
        /// </summary>
        public static readonly IStyle<TokenBox> TokenBox = new TokenBoxStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, CaretColor = GameColors.Ink, SelectionColor = GameColors.Line, HintColor = GameColors.Faint,
                Background = GameColors.Page, Border = GameColors.Line, Padding = new Thickness(12f, 8f), TokenSpacing = 4f
            },
            Focused = { Border = GameColors.ControlEdgeHot },
            Disabled = { Color = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a token in a token box: a small button, lit while selected and edged in ink while marked.
        /// </summary>
        public static readonly IStyle<Token> Token = new TokenStyle(Button)
        {
            Base = { Padding = new Thickness(8f, 4f) },
            Selected = { Background = GameColors.Line },
            Marked = { Border = GameColors.Ink },
            Disabled = { Color = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a box a number is typed into: a text box's look with the number set against its right-hand
        ///     end.
        /// </summary>
        public static readonly IStyle<NumberBox> NumberBox = new NumberBoxStyle(TextBox)
        {
            Base = { TextAlignment = TextAlignment.Right }
        };

        /// <summary>
        ///     The style for a box many lines are written in: the game's ink on whatever is behind it, dim line
        ///     numbers, and no scrollbars until a style adds some.
        /// </summary>
        public static readonly IStyle<TextArea> TextArea = new TextAreaStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, CaretColor = GameColors.Ink, SelectionColor = GameColors.Line, HintColor = GameColors.Faint,
                LineNumberColor = GameColors.Dim, ScrollBarGap = 8f, Thumb = GameColors.Dim, MinThumbLength = 32f
            },
            Dragging = { Thumb = GameColors.Faint },
            Disabled = { Color = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a scroll view, with a bar as wide as the one the game uses for its own, 8 units clear of the
        ///     content.
        /// </summary>
        public static readonly IStyle<ScrollView> ScrollView = new ScrollViewStyle
        {
            Base = { ScrollBarWidth = 12f, ScrollBarGap = 8f, Thumb = GameColors.Dim, MinThumbLength = 32f },
            Dragging = { Thumb = GameColors.Faint }
        };

        /// <summary>
        ///     The style for a tip: a box of text in the chrome color with a hairline border and a short shadow, wrapping
        ///     at 400 units, that opens once the pointer has rested half a second.
        /// </summary>
        public static readonly IStyle<Tip> Tip = new TipStyle
        {
            Base =
            {
                Background = GameColors.Chrome, Border = GameColors.Line, Padding = new Thickness(8f, 4f), MaxWidth = 400f, Shadow = ShortShadow,
                ShowDelay = 30
            }
        };

        /// <summary>
        ///     The style for a codex tip: a tip as wide as its placement says, with more padding inside the edge and
        ///     slightly spaced parts.
        /// </summary>
        public static readonly IStyle<CodexTip> CodexTip = new CodexTipStyle(Tip)
        {
            Base = { Padding = new Thickness(12f, 8f), MaxWidth = float.PositiveInfinity, Spacing = 8f, Stagger = 8f, ReplaceDelay = 24 }
        };

        /// <summary>The style for a window: a panel with a hairline border, casting a window shadow.</summary>
        public static readonly IStyle<Window> Window = new WindowStyle
        {
            Base = { Background = GameColors.Panel, Border = GameColors.Line, Padding = new Thickness(12f), Shadow = WindowShadow }
        };

        /// <summary>
        ///     The style for a popup hanging from what opened it: a window's panel, tighter around its content, with a
        ///     short shadow.
        /// </summary>
        public static readonly IStyle<Popup> Popup = new PopupStyle(Window)
        {
            Base = { Padding = new Thickness(8f), Shadow = ShortShadow }
        };

        /// <summary>
        ///     The style for a coach mark: a popup's panel edged in the accent color, its text wrapping at 320 units, with a
        ///     tail 12 units long.
        /// </summary>
        public static readonly IStyle<CoachMark> CoachMark = new CoachMarkStyle(Popup)
        {
            Base = { Border = GameColors.Accent, Padding = new Thickness(12f, 8f), Gap = 12f, MaxWidth = 320f }
        };

        /// <summary>
        ///     The style for a menu: a popup's panel, close around its items, opening a submenu once the pointer has rested
        ///     on its item for a moment.
        /// </summary>
        public static readonly IStyle<Menu> Menu = new MenuStyle(Popup)
        {
            Base = { Padding = new Thickness(4f), SubmenuDelay = 24, HeaderSpacing = 4f }
        };

        /// <summary>
        ///     The style for a menu's item: words in the game's ink on no plate, lit where its menu has lit it,
        ///     an accent square before the chosen one, its shortcut smaller and muted at its right, a muted arrow at the far
    ///     right of one that opens a submenu, and dimmed when disabled.
        /// </summary>
        public static readonly IStyle<MenuItem> MenuItem = new MenuItemStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, Padding = new Thickness(12f, 8f), ShortcutColor = GameColors.Muted, ShortcutFontSize = 16f,
                ShortcutSpacing = 24f, Mark = GameColors.Accent, MarkSize = 8f, MarkSpacing = 12f, Arrow = GameColors.Muted, ArrowSize = 8f, ArrowSpacing = 16f,
                HoverSound = HoverSound, ClickSound = ClickSound
            },
            Highlighted = { Background = GameColors.Line },
            Disabled = { Color = GameColors.Dim, ShortcutColor = GameColors.Dim, Mark = GameColors.Dim, Arrow = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a dialog: a panel in the middle of the screen with a short shadow, leaving the world
        ///     visible around it.
        /// </summary>
        public static readonly IStyle<Dialog> Dialog = new DialogStyle
        {
            Base = { Background = GameColors.Chrome, Border = GameColors.Line, Padding = new Thickness(24f), Shadow = ShortShadow }
        };

        /// <summary>
        ///     The style for a color spectrum: Lumel's own white and black laid over the hue, edged as a swatch is, its
        ///     marker white outlined in black.
        /// </summary>
        public static readonly IStyle<ColorSpectrum> ColorSpectrum = new ColorSpectrumStyle
        {
            Base = { Pale = LumelMaterials.Pale, Dark = LumelMaterials.Dark, Border = GameColors.Muted }
        };

        /// <summary>The style for a hue slider: Lumel's own strip of hues, edged as a swatch is, its marker white outlined in black.</summary>
        public static readonly IStyle<HueSlider> HueSlider = new HueSliderStyle
        {
            Base = { Hues = LumelMaterials.Hues, Border = GameColors.Muted }
        };

        /// <summary>The style for a color palette: square swatches a little apart, in rows as far apart.</summary>
        public static readonly IStyle<ColorPalette> ColorPalette = new ColorPaletteStyle
        {
            Base = { Spacing = 4f, LineSpacing = 4f, SwatchSize = 36f }
        };

        /// <summary>The style for a color panel: its rows set apart as a form's are.</summary>
        public static readonly IStyle<ColorPanel> ColorPanel = new ColorPanelStyle
        {
            Base = { Spacing = 8f }
        };

        /// <summary>
        ///     The style for a color picker: a drop-down's box with the color in place of its words, edged so that a dark
        ///     color still shows, lifted on hover, while its panel is open and while its focus shows, and dimmed when disabled;
        ///     its Done button is the primary button.
        /// </summary>
        public static readonly IStyle<ColorPicker> ColorPicker = new ColorPickerStyle
        {
            Base =
            {
                SampleSize = new Vector2(96f, 20f), SampleEdge = GameColors.Muted, Arrow = GameColors.Muted, ArrowSize = 12f, ArrowSpacing = 16f,
                DoneStyle = PrimaryButton, Background = GameColors.Panel, Border = GameColors.ControlEdge, Padding = new Thickness(12f, 8f),
                HoverSound = HoverSound, ClickSound = ClickSound
            },
            PointerOver = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot, Arrow = GameColors.Ink },
            Expanded = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot, Arrow = GameColors.Ink },
            [State.FocusVisible] = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot, Arrow = GameColors.Ink },
            Disabled = { SampleEdge = GameColors.Dim, Arrow = GameColors.Dim }
        };

        /// <summary>The style for a patch of color, edged so that a dark color still shows on a panel.</summary>
        public static readonly IStyle<Swatch> Swatch = new SwatchStyle
        {
            Base = { Border = GameColors.Muted, HoverSound = HoverSound, ClickSound = ClickSound }
        };

        /// <summary>
        ///     The style for a message box: a dialog whose Enter answer is accent colored, as the primary button is.
        /// </summary>
        public static readonly IStyle<MessageBox> MessageBox = new MessageBoxStyle(Dialog)
        {
            Base = { MaxWidth = 640f, MessageSpacing = 24f, ButtonSpacing = 8f, DefaultButtonStyle = PrimaryButton }
        };

        /// <summary>The style for a hairline across a block, as the game rules off its panels.</summary>
        public static readonly IStyle<Separator> Separator = new SeparatorStyle
        {
            Base = { Thickness = 1f, Color = GameColors.Line }
        };

        /// <summary>The style for a line between a menu's groups: a hairline with a little room above and below.</summary>
        public static readonly IStyle<MenuSeparator> MenuSeparator = new MenuSeparatorStyle(Separator)
        {
            Base = { Padding = new Thickness(0f, 4f) }
        };

        /// <summary>
        ///     The style for the line down a tree's gutter, in the hairline's color, turning in toward what hangs from
        ///     it.
        /// </summary>
        public static readonly IStyle<TreeLine> TreeLine = new TreeLineStyle
        {
            Base = { Thickness = 2f, Turn = 12f, Color = GameColors.Line }
        };

        /// <summary>The style for a progress bar: an accent fill over a well-colored track.</summary>
        public static readonly IStyle<ProgressBar> ProgressBar = new ProgressBarStyle
        {
            Base = { Background = GameColors.Well, Fill = GameColors.Accent }
        };

        /// <summary>The style for a drag handle: faint dots that brighten to ink on hover and while dragged.</summary>
        public static readonly IStyle<DragHandle> DragHandle = new DragHandleStyle
        {
            Base = { Color = GameColors.Faint },
            PointerOver = { Color = GameColors.Ink },
            Dragging = { Color = GameColors.Ink }
        };

        /// <summary>
        ///     The style for a slider: a sunken track and a block of a thumb in ink, both lit on hover, while dragged and
        ///     while its focus shows, as the game's own slider is, and dim when disabled.
        /// </summary>
        public static readonly IStyle<Slider> Slider = new SliderStyle
        {
            Base = { TrackTint = GameColors.Well, TrackHeight = 8f, Thumb = GameColors.Ink, ThumbWidth = 12f, ThumbHeight = 20f },
            PointerOver = { TrackTint = GameColors.Line, Thumb = Color.White },
            [Controls.Slider.Dragging] = { TrackTint = GameColors.Line, Thumb = Color.White },
            [State.FocusVisible] = { TrackTint = GameColors.Line, Thumb = Color.White },
            Disabled = { TrackTint = GameColors.ControlEdge, Thumb = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a drop-down: a button's panel with the words at its left and a muted arrow at its right, lifted
        ///     on hover, while its list is open and while its focus shows, and dimmed when disabled.
        /// </summary>
        public static readonly IStyle<DropDown> DropDown = new DropDownStyle
        {
            Base =
            {
                Font = GameFonts.Default, FontSize = 20f, Color = GameColors.Ink, HintColor = GameColors.Faint, Arrow = GameColors.Muted, ArrowSize = 12f,
                ArrowSpacing = 16f, Background = GameColors.Panel, Border = GameColors.ControlEdge, Padding = new Thickness(12f, 8f), HoverSound = HoverSound,
                ClickSound = ClickSound
            },
            PointerOver = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot, Arrow = GameColors.Ink },
            Expanded = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot, Arrow = GameColors.Ink },
            [State.FocusVisible] = { Background = GameColors.PanelHot, Border = GameColors.ControlEdgeHot, Arrow = GameColors.Ink },
            Disabled = { Color = GameColors.Dim, Arrow = GameColors.Dim }
        };

        /// <summary>
        ///     The style for a dial: faint marks, quarter marks a step brighter, and a needle in ink that turns white
        ///     on hover and while turned.
        /// </summary>
        public static readonly IStyle<Dial> Dial = new DialStyle
        {
            Base = { MarkColor = GameColors.Faint, QuarterColor = GameColors.Muted, NeedleColor = GameColors.Ink },
            PointerOver = { NeedleColor = Color.White },
            Dragging = { NeedleColor = Color.White }
        };

        /// <summary>The style for a search box: a text box, and a cross after it the height of its line.</summary>
        public static readonly IStyle<SearchBox> SearchBox = new SearchBoxStyle
        {
            Base = { Spacing = 4f, ClearSymbol = "x", ClearSize = 28f }
        };

        /// <summary>The theme containing every style above, each the default for its kind of control.</summary>
        public static readonly ITheme Theme = new Theme
        {
            Label,
            RichText,
            Glyph,
            Button,
            CheckBox,
            RadioButton,
            Switch,
            Tab,
            TextBox,
            TokenBox,
            Token,
            NumberBox,
            SpinBox,
            TextArea,
            ScrollView,
            Tip,
            CodexTip,

            Window,
            Popup,
            CoachMark,
            Menu,
            MenuItem,
            Dialog,
            MessageBox,
            Swatch,
            ColorSpectrum,
            HueSlider,
            ColorPalette,
            ColorPanel,
            ColorPicker,
            Separator,
            MenuSeparator,
            TreeLine,
            ProgressBar,
            DragHandle,
            Slider,
            Dial,
            DropDown,
            SearchBox
        };
    }
}
