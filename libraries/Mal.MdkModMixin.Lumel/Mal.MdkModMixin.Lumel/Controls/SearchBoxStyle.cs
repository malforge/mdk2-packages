using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a search box: the space between its parts, the style of its text box, and its cross.</summary>
    public class SearchBoxStyle : Style<SearchBox, SearchBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the search box at its neutral values.</summary>
        public SearchBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public SearchBoxStyle(IStyle<SearchBox> basedOn) : base(basedOn)
        {
        }
    }
}
