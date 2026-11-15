using System;
using System.Collections.Generic;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A row of colors to choose from, a swatch each, running on to further rows where they do not fit; pressing a
    ///     swatch chooses its color.
    /// </summary>
    /// <remarks>
    ///     In a <see cref="ColorPanel" />'s <see cref="ColorPanel.Palettes" />, a choice is given to the panel. Its colors
    ///     are the application's, as its players' paint colors or the colors they chose last are.
    /// </remarks>
    public class ColorPalette : Flow
    {
        readonly List<Color> _colors = new List<Color>();
        float _swatchSize;
        int _places;

        /// <summary>Raised when the player presses a swatch, with its color.</summary>
        public event Action<Color> Chosen;

        /// <summary>Gets or sets the style of this palette, or null to use the default from the host's theme for a palette.</summary>
        public new IStyle<ColorPalette> Style
        {
            get { return (IStyle<ColorPalette>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this palette's own look, applied in every state and layered over the look from its style.</summary>
        public new ColorPaletteLook Base => (ColorPaletteLook)base.Base;

        /// <summary>Gets this palette's own look applied on hover, layered over the look from its style.</summary>
        public new ColorPaletteLook PointerOver => (ColorPaletteLook)base.PointerOver;

        /// <summary>Gets this palette's own look applied when disabled, layered over the look from its style.</summary>
        public new ColorPaletteLook Disabled => (ColorPaletteLook)base.Disabled;

        /// <summary>
        ///     Gets this palette's own look for <paramref name="state" />, or for every state it joins, layered over the look
        ///     from its style.
        /// </summary>
        public new ColorPaletteLook this[State state] => (ColorPaletteLook)base[state];

        /// <summary>Gets the colors it offers, in order.</summary>
        public IReadOnlyList<Color> Colors => _colors;

        /// <summary>Gets the width and height of each swatch.</summary>
        /// <remarks>Set by this palette's looks.</remarks>
        public float SwatchSize => _swatchSize;

        /// <summary>Gets the style of each swatch, or null to use the host theme's swatch style.</summary>
        /// <remarks>Set by this palette's looks.</remarks>
        public IStyle<Swatch> SwatchStyle { get; private set; }

        /// <summary>Gets or sets how many places the palette has, or 0 for one per color.</summary>
        /// <remarks>
        ///     A place no color fills shows its swatch's edge alone and does nothing when pressed, so a row of colors chosen
        ///     last keeps its length while it fills. Throws if set below the number of colors offered.
        /// </remarks>
        public int Places
        {
            get { return _places; }
            set
            {
                if (value < 0 || (value > 0 && value < _colors.Count))
                    throw new ArgumentException("A palette has room for every color it offers, and " + value + " places hold fewer than its " + _colors.Count + ".", nameof(value));

                _places = value;
                Fill();
            }
        }

        /// <summary>
        ///     Offers <paramref name="colors" /> in place of the colors it offered, keeping the swatches it has and adding or
        ///     removing the difference.
        /// </summary>
        /// <remarks>Throws if the palette has fewer <see cref="Places" /> than <paramref name="colors" />.</remarks>
        public void SetColors(IReadOnlyList<Color> colors)
        {
            if (_places > 0 && colors.Count > _places)
                throw new ArgumentException("A palette of " + _places + " places cannot offer " + colors.Count + " colors.", nameof(colors));

            _colors.Clear();
            _colors.AddRange(colors);
            Fill();
        }

        /// <summary>Makes a swatch for each place, each filled with its color where it has one and inert where it has none.</summary>
        void Fill()
        {
            var count = _places > 0 ? _places : _colors.Count;
            while (Children.Count > count)
                Children.Remove(Children[Children.Count - 1]);

            while (Children.Count < count)
            {
                var at = Children.Count;
                Children.Add(new Swatch { Style = SwatchStyle, Placement = FixedSize.Size(_swatchSize, _swatchSize), Command = new RelayCommand(() => Choose(at)) });
            }

            for (var i = 0; i < count; i++)
            {
                var swatch = (Swatch)Children[i];
                var filled = i < _colors.Count;
                swatch.Color = filled ? _colors[i] : Color.Transparent;
                swatch.IsEnabled = filled;
            }
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ColorPalette>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ColorPaletteLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var palette = (ColorPaletteLook)look;
            var size = palette.SwatchSize ?? 0f;
            var style = palette.SwatchStyle;
            if (size == _swatchSize && style == SwatchStyle)
                return;

            _swatchSize = size;
            SwatchStyle = style;
            foreach (var element in Children)
            {
                ((Swatch)element).Style = style;
                element.Placement = FixedSize.Size(size, size);
            }
        }

        /// <summary>
        ///     Tells whoever listens, and the panel the palette is in where there is one, that the color at
        ///     <paramref name="at" /> was chosen.
        /// </summary>
        void Choose(int at)
        {
            var color = _colors[at];
            Chosen?.Invoke(color);
            ((IColorPanel)Panel())?.Picked(color);
        }

        /// <summary>Returns the color panel the palette is in, or null if it is in none.</summary>
        ColorPanel Panel()
        {
            for (var element = Parent; element != null; element = element.Parent)
            {
                var panel = element as ColorPanel;
                if (panel != null)
                    return panel;
            }

            return null;
        }
    }
}
