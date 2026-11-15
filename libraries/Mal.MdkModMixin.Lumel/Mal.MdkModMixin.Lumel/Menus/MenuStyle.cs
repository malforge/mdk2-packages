using Lumel.Styling;
using Lumel.Windows;

namespace Lumel.Menus
{
    /// <summary>The style of a <see cref="Menu" />: a popup's look and the style of the scroll that holds its items.</summary>
    public class MenuStyle : Style<Menu, MenuLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the menu at its neutral values.</summary>
        public MenuStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public MenuStyle(IStyle<Popup> basedOn) : base(basedOn)
        {
        }
    }
}
