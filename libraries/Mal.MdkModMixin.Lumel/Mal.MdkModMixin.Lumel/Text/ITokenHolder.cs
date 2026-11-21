namespace Lumel.Text
{
    /// <summary>What holds tokens, told by each of them when it is pressed.</summary>
    /// <remarks>Implemented explicitly, so that only a token reaches it.</remarks>
    public interface ITokenHolder
    {
        /// <summary>Called when <paramref name="token" />, which this holds, is pressed.</summary>
        void Press(Token token);
    }
}
