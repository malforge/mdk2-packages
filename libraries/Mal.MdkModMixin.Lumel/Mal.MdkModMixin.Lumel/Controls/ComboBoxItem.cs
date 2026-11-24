using System;
using Lumel.Input;
using Lumel.Menus;

namespace Lumel.Controls
{
    /// <summary>
    ///     One choice in a <see cref="ComboBox" />'s list: a menu item whose press puts its words in the combo box.
    /// </summary>
    /// <remarks>Disabled, it shows in the list and cannot be picked there; hidden, it is left out of the list.</remarks>
    public class ComboBoxItem : MenuItem
    {
        readonly RelayCommand _pick;

        /// <summary>Creates a choice with no text, using the look its host's theme holds for a menu item until it is given a style.</summary>
        public ComboBoxItem()
        {
            _pick = new RelayCommand(Pick);
            Command = _pick;
        }

        /// <summary>Gets the combo box whose list the item is in, or null if it is in none.</summary>
        public ComboBox ComboBox => Menu?.Anchor as ComboBox;

        /// <inheritdoc />
        /// <remarks>Throws where the item's command was replaced: pressing a choice picks it, and does nothing else.</remarks>
        protected override void OnPressed()
        {
            if (Command != _pick)
                throw new InvalidOperationException("A combo box's choice has no other command; pressing it picks it.");

            base.OnPressed();
        }

        /// <summary>Puts the item's words in its combo box, as the player picked it.</summary>
        /// <remarks>Throws where the item is in no combo box's list.</remarks>
        void Pick()
        {
            var comboBox = ComboBox;
            if (comboBox == null)
                throw new InvalidOperationException("A choice is picked in the combo box whose list it is in, and this one is in none.");

            ((IComboBox)comboBox).Picked(this);
        }
    }
}
