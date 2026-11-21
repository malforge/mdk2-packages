using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The style of a token box: its words, caret, selection, hint and box, when idle, hovered, focused and disabled.
    /// </summary>
    public class TokenBoxStyle : Style<TokenBox, TokenBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the token box at its neutral values.</summary>
        public TokenBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public TokenBoxStyle(IStyle<TokenBox> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look that applies when the token box has keyboard focus.</summary>
        public TokenBoxLook Focused => this[State.Focused];
    }
}
