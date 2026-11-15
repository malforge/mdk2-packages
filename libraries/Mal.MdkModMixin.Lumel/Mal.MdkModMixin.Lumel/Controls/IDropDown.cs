namespace Lumel.Controls
{
    /// <summary>Implemented by a <see cref="DropDown" /> so its items can tell it one of them was chosen.</summary>
    /// <remarks>Implemented explicitly by <see cref="DropDown" />, so only its items call it.</remarks>
    public interface IDropDown
    {
        /// <summary>Tells the drop-down the player chose <paramref name="item" /> from its list.</summary>
        void Picked(DropDownItem item);
    }
}
