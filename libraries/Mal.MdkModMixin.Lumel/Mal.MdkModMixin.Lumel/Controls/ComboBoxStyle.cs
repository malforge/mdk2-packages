using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a combo box: its box, field and arrow when idle, hovered, focused, open and disabled.</summary>
    public class ComboBoxStyle : Style<ComboBox, ComboBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the combo box at its neutral values.</summary>
        public ComboBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ComboBoxStyle(IStyle<ComboBox> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the combo box's field has keyboard focus.</summary>
        public ComboBoxLook Focused => this[State.Focused];

        /// <summary>Gets the look changes applied while the combo box's list is open.</summary>
        public ComboBoxLook Expanded => this[ComboBox.Expanded];
    }
}
