namespace Lumel.Controls
{
    /// <summary>Implemented by a <see cref="RadioGroup" /> so its buttons can tell it one of them was pressed.</summary>
    /// <remarks>Implemented explicitly by <see cref="RadioGroup" />, so only its buttons call it.</remarks>
    public interface IRadioGroup
    {
        /// <summary>Tells the group the player pressed <paramref name="button" />, one of its own.</summary>
        void Picked(RadioButton button);
    }
}
