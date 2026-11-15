namespace Lumel.Text
{
    /// <summary>How a label handles a run of text that is too wide for its space.</summary>
    public enum TextWrapping
    {
        /// <summary>The text stays on one line and extends past the available space.</summary>
        NoWrap,

        /// <summary>The text continues on the next line, breaking between words.</summary>
        Wrap,

        /// <summary>The text stays on one line and is cut short with an ellipsis.</summary>
        Trim
    }
}
