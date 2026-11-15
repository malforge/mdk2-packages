using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>The style of rich text: its text kinds, headings, links, code, rules, quotes and callouts.</summary>
    public class RichTextStyle : Style<RichText, RichTextLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the text at its default values.</summary>
        public RichTextStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and applies its own settings
        ///     over it.
        /// </summary>
        public RichTextStyle(IStyle<RichText> basedOn) : base(basedOn)
        {
        }
    }
}
