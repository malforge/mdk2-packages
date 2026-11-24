namespace Lumel.Controls
{
    /// <summary>Implemented by a <see cref="ComboBox" /> so its items can tell it one of them was picked.</summary>
    /// <remarks>Implemented explicitly by <see cref="ComboBox" />, so only its items call it.</remarks>
    public interface IComboBox
    {
        /// <summary>Tells the combo box the player picked <paramref name="item" /> from its list.</summary>
        void Picked(ComboBoxItem item);
    }
}
