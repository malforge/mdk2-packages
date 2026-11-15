using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a color panel: the space between its rows, the box it paints across its bounds, and how the color made shows while there is none.</summary>
    public class ColorPanelStyle : Style<ColorPanel, ColorPanelLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the panel at its neutral values.</summary>
        public ColorPanelStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ColorPanelStyle(IStyle<ColorPanel> basedOn) : base(basedOn)
        {
        }
    }
}
