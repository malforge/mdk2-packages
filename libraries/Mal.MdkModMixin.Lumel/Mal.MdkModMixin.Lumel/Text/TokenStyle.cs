using Lumel.Controls;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>The style of a token: its words and pill at rest, on hover, while pressed, selected, marked and disabled.</summary>
    public class TokenStyle : Style<Token, TextLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the token at its neutral values.</summary>
        public TokenStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public TokenStyle(IStyle<Button> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the pointer is pressed on the token.</summary>
        public TextLook Pressed => this[Button.Pressed];

        /// <summary>Gets the look changes applied while the token is inside its box's selection.</summary>
        public TextLook Selected => this[Token.Selected];

        /// <summary>Gets the look changes applied while the token is marked as wrong.</summary>
        public TextLook Marked => this[Token.Marked];
    }
}
