namespace Lumel.Text
{
    /// <summary>A run of link text, or a slot inside one.</summary>
    public sealed class LinkRun : Run
    {
        /// <summary>
        ///     Creates a run of <paramref name="words" /> that belongs to <paramref name="link" /> and is formatted as
        ///     <paramref name="kind" />.
        /// </summary>
        public LinkRun(Link link, string words, RunKind kind)
            : base(words, kind)
        {
            Link = link;
        }

        LinkRun(Link link, RunKind kind, string slot)
            : base(kind, slot)
        {
            Link = link;
        }

        /// <inheritdoc />
        public override Link Link { get; }

        /// <summary>
        ///     Creates a slot of <paramref name="link" /> for the argument named <paramref name="name" />, formatted as
        ///     <paramref name="kind" />.
        /// </summary>
        public static LinkRun Slotted(Link link, string name, RunKind kind)
        {
            return new LinkRun(link, kind, name);
        }
    }
}
