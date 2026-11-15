using Lumel.Layout;
using Lumel.Text;

namespace Lumel.Controls
{
    public partial class ColorPanel
    {
        // As wide as eight swatches of a palette in the game theme, four apart, so the rows of all three parts share an edge.
        const float Wide = 316f;
        const float Gap = 8f;
        const float Close = 4f;

        // Palettes stand further apart than the swatches in any one of them, so each reads as a row of its own.
        const float PaletteGap = 16f;
        const float SpectrumHigh = 176f;
        const float StripWide = 24f;
        const float ChannelWide = 56f;
        const float CodeWide = 124f;
        const float SampleWide = ChannelWide;
        const float SampleHigh = 80f;
        const float SpectrumWide = Wide - StripWide - Gap;

        ColorSpectrum _spectrum;
        HueSlider _strip;
        Swatch _sample;
        TabView _entries;
        NumberBox _hueBox;
        NumberBox _saturationBox;
        NumberBox _valueBox;
        NumberBox _redBox;
        NumberBox _greenBox;
        NumberBox _blueBox;
        TextBox _code;
        VStack _palettes;

        /// <summary>
        ///     Creates the square of saturation and value beside the strip of hues; under them the color made, beside a tab
        ///     each for typing its hue, saturation and value, its red, green and blue, or its code; and under those the
        ///     palettes.
        /// </summary>
        void CreateView()
        {
            Children.Add(new HStack
            {
                Base = { Spacing = Gap },
                Children =
                {
                    new ColorSpectrum { Placement = FixedSize.Size(SpectrumWide, SpectrumHigh) }.As(out _spectrum),
                    new HueSlider { Placement = FixedSize.Size(StripWide, SpectrumHigh) }.As(out _strip)
                }
            });
            Children.Add(new HStack
            {
                Base = { Spacing = Gap },
                Placement = FixedSize.Width(Wide),
                Children =
                {
                    new Swatch { Placement = FixedSize.Size(SampleWide, SampleHigh) }.As(out _sample),
                    new TabView
                    {
                        Base = { Spacing = Gap },
                        Placement = Flex.Grow,
                        Tabs =
                        {
                            new Tab { Header = "HSV", Content = Channels("H", 360, out _hueBox, "S", 100, out _saturationBox, "V", 100, out _valueBox) },
                            new Tab { Header = "RGB", Content = Channels("R", 255, out _redBox, "G", 255, out _greenBox, "B", 255, out _blueBox) },
                            new Tab
                            {
                                Header = "Hex",
                                Content = new TextBox { SelectsAll = true, Placement = FixedSize.Width(CodeWide), Align = Align.Left }.As(out _code)
                            }
                        }
                    }.As(out _entries)
                }
            });
            Children.Add(new VStack { Base = { Spacing = PaletteGap }, Placement = FixedSize.Width(Wide) }.As(out _palettes));
        }

        /// <summary>Three channels in a line, each a letter before the box it is typed in.</summary>
        static HStack Channels(string first, int firstMost, out NumberBox firstBox, string second, int secondMost, out NumberBox secondBox,
            string third, int thirdMost, out NumberBox thirdBox)
        {
            return new HStack
            {
                Base = { Spacing = Gap },
                Children =
                {
                    Lettered(first, Channel(firstMost).As(out firstBox)),
                    Lettered(second, Channel(secondMost).As(out secondBox)),
                    Lettered(third, Channel(thirdMost).As(out thirdBox))
                }
            };
        }

        /// <summary>A channel's letter, close before the box it is typed in.</summary>
        static HStack Lettered(string letter, NumberBox box)
        {
            return new HStack
            {
                Base = { Spacing = Close },
                Children = { new Label { Text = letter, Align = Align.Center }, box }
            };
        }

        /// <summary>The box one channel is typed in, as a whole number from 0 to <paramref name="most" />.</summary>
        static NumberBox Channel(int most)
        {
            var widest = most.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new NumberBox
            {
                SelectsAll = true, IsInteger = true, Minimum = 0, Maximum = most, Widest = widest, Placement = FixedSize.Width(ChannelWide)
            };
        }
    }
}
