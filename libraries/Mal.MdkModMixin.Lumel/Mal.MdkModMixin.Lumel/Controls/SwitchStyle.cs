using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a <see cref="Switch" />: its words, track and thumb, when off, on, hovered and disabled.
    /// </summary>
    public class SwitchStyle : Style<Switch, SwitchLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the switch at its neutral values.</summary>
        public SwitchStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with this style's own settings
        ///     applied over it.
        /// </summary>
        public SwitchStyle(IStyle<Switch> basedOn) : base(basedOn)
        {
        }

        /// <summary>The look applied while the switch is on.</summary>
        public SwitchLook On => this[Switch.On];
    }
}
