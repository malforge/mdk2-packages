using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a drop-down: its words, box and arrow when idle, hovered, open and disabled.</summary>
    public class DropDownStyle : Style<DropDown, DropDownLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the drop-down at its neutral values.</summary>
        public DropDownStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public DropDownStyle(IStyle<DropDown> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the drop-down's list is open.</summary>
        public DropDownLook Expanded => this[DropDown.Expanded];
    }
}
