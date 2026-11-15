namespace Lumel.Menus
{
    /// <summary>Implemented by a <see cref="Menu" /> so its items can tell it one of them is being pressed.</summary>
    /// <remarks>Implemented explicitly by <see cref="Menu" />, so only its items call it.</remarks>
    public interface IMenu
    {
        /// <summary>Tells the menu <paramref name="item" /> is being pressed, before its command runs.</summary>
        void Pressing(MenuItem item);

        /// <summary>Tells the menu <paramref name="item" /> was pressed to open its submenu.</summary>
        void Opening(MenuItem item);

        /// <summary>Tells the menu the pointer has come onto <paramref name="item" />, or left it where <paramref name="on" /> is false.</summary>
        void PointerOn(MenuItem item, bool on);
    }
}
