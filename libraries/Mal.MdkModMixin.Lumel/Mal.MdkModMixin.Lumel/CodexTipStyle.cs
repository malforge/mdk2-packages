using Lumel.Styling;

namespace Lumel
{
    /// <summary>
    ///     The style of a codex tip: its box and shadow, maximum height and the looks of its text and steps.
    /// </summary>
    public class CodexTipStyle : Style<CodexTip, CodexTipLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the tip at its neutral values.</summary>
        public CodexTipStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public CodexTipStyle(IStyle<Tip> basedOn) : base(basedOn)
        {
        }
    }
}
