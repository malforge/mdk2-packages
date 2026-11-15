using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>The style of a label: the font, size and color of its text, and its box.</summary>
    public class LabelStyle : Style<Label, LabelLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the label at its default values.</summary>
        public LabelStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and applies its own settings
        ///     over it.
        /// </summary>
        public LabelStyle(IStyle<Label> basedOn) : base(basedOn)
        {
        }
    }
}
