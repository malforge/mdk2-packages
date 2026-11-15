using System;

namespace Lumel.Controls
{
    /// <summary>The radio buttons of which one at most is checked: checking one unchecks the one checked before.</summary>
    /// <remarks>A button joins by being given the group as its <see cref="RadioButton.Group" />.</remarks>
    public class RadioGroup : IRadioGroup
    {
        RadioButton _chosen;

        /// <summary>
        ///     Raised when the player checks another button of the group, but not when <see cref="Chosen" /> or a button's
        ///     <see cref="RadioButton.IsChecked" /> is set.
        /// </summary>
        public event Action ChoiceChanged;

        /// <summary>Gets or sets the button that is checked, or null for none.</summary>
        /// <remarks>Throws if set to a button of another group.</remarks>
        public RadioButton Chosen
        {
            get { return _chosen; }
            set
            {
                if (value != null && value.Group != this)
                    throw new ArgumentException("A group can only check one of its own buttons.", nameof(value));

                if (value == _chosen)
                    return;

                var was = _chosen;
                _chosen = value;
                was?.Set(RadioButton.Checked, false);
                value?.Set(RadioButton.Checked, true);
            }
        }

        /// <inheritdoc />
        /// <remarks>Checks <paramref name="button" /> and raises <see cref="ChoiceChanged" /> where it is another.</remarks>
        void IRadioGroup.Picked(RadioButton button)
        {
            if (button == _chosen)
                return;

            Chosen = button;
            ChoiceChanged?.Invoke();
        }
    }
}
