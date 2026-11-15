using Lumel.Controls;
using Lumel.Styling;

namespace Lumel.Menus
{
    /// <summary>
    ///     The style of a <see cref="MenuItem" />: its words, shortcut and plate when idle, hovered, highlighted, chosen,
    ///     pressed and disabled.
    /// </summary>
    public class MenuItemStyle : Style<MenuItem, MenuItemLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the item at its neutral values.</summary>
        public MenuItemStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public MenuItemStyle(IStyle<Button> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look applied while the item is the one its menu has lit.</summary>
        public MenuItemLook Highlighted => this[MenuItem.Highlighted];

        /// <summary>Gets the look applied while the item is marked as the chosen one.</summary>
        public MenuItemLook Chosen => this[MenuItem.Chosen];

        /// <summary>Gets the look applied while the pointer is pressed on the item.</summary>
        public MenuItemLook Pressed => this[Button.Pressed];
    }
}
