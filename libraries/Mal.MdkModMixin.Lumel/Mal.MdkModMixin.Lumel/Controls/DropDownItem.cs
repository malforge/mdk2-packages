using System;
using Lumel.Input;
using Lumel.Menus;

namespace Lumel.Controls
{
    /// <summary>
    ///     One choice in a <see cref="DropDown" />'s list: a menu item whose press makes it the drop-down's choice.
    /// </summary>
    /// <remarks>
    ///     Disabled, it shows in the list and cannot be chosen there; hidden, it is left out of the list. Either way the
    ///     application can still make it the choice through <see cref="DropDown.Chosen" />.
    /// </remarks>
    public class DropDownItem : MenuItem
    {
        readonly RelayCommand _choose;

        /// <summary>Creates a choice with no text, using the look its host's theme holds for a menu item until it is given a style.</summary>
        public DropDownItem()
        {
            _choose = new RelayCommand(Choose);
            Command = _choose;
        }

        /// <summary>Gets the drop-down whose list the item is in, or null if it is in none.</summary>
        public DropDown DropDown => Menu?.Anchor as DropDown;

        /// <inheritdoc />
        /// <remarks>Throws where the item's command was replaced: pressing a choice chooses it, and does nothing else.</remarks>
        protected override void OnPressed()
        {
            if (Command != _choose)
                throw new InvalidOperationException("A drop-down's choice has no other command; pressing it chooses it.");

            base.OnPressed();
        }

        /// <summary>Makes the item its drop-down's choice, as the player picked it.</summary>
        /// <remarks>Throws where the item is in no drop-down's list.</remarks>
        void Choose()
        {
            var dropDown = DropDown;
            if (dropDown == null)
                throw new InvalidOperationException("A choice is made in the drop-down whose list it is in, and this one is in none.");

            ((IDropDown)dropDown).Picked(this);
        }
    }
}
