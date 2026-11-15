using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a check box: its text, box and mark when idle, checked, partly checked, on hover and when
    ///     disabled.
    /// </summary>
    public class CheckBoxStyle : Style<CheckBox, CheckBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the check box at its neutral values.</summary>
        public CheckBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public CheckBoxStyle(IStyle<CheckBox> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the box is checked.</summary>
        public CheckBoxLook Checked => this[CheckBox.Checked];

        /// <summary>Gets the look changes applied while the box is partly checked.</summary>
        public CheckBoxLook Indeterminate => this[CheckBox.Indeterminate];
    }
}
