using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a <see cref="TreeLine" />: its width, turn and color.</summary>
    public class TreeLineStyle : Style<TreeLine, TreeLineLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the line at its neutral values.</summary>
        public TreeLineStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with this style's own settings
        ///     applied over it.
        /// </summary>
        public TreeLineStyle(IStyle<TreeLine> basedOn) : base(basedOn)
        {
        }
    }
}
