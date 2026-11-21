using System;

namespace Lumel.Text
{
    /// <summary>One part of what a token box holds: a run of words, or a token.</summary>
    public struct TokenBoxPart
    {
        /// <summary>Creates a part holding the words <paramref name="text" />.</summary>
        public TokenBoxPart(string text)
        {
            Text = text ?? "";
            Token = null;
        }

        /// <summary>Creates a part holding <paramref name="token" />.</summary>
        public TokenBoxPart(Token token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            Text = null;
            Token = token;
        }

        /// <summary>The words of this part, or null where it is a token.</summary>
        public readonly string Text;

        /// <summary>The token of this part, or null where it is words.</summary>
        public readonly Token Token;

        /// <summary>Whether this part is a token.</summary>
        public bool IsToken => Token != null;
    }
}
