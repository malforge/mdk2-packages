using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>
    ///     Structured text: an ordered list of blocks, each holding runs of text, independent of the format it was
    ///     written in.
    /// </summary>
    /// <remarks>Never changes once made, so whatever shows one lays it out again only when it is given another.</remarks>
    public sealed class Document
    {
        /// <summary>Creates a document from <paramref name="blocks" />, in order.</summary>
        public Document(IEnumerable<Block> blocks)
        {
            Blocks = blocks == null ? new Block[0] : new List<Block>(blocks).ToArray();
            var links = new List<Link>();
            Collect(Blocks, links);
            Links = links;
        }

        /// <summary>Gets the blocks of the document, in order.</summary>
        public IReadOnlyList<Block> Blocks { get; }

        /// <summary>Gets the document's links, each once, in tables, quotes and callouts too, in order.</summary>
        public IReadOnlyList<Link> Links { get; }

        /// <summary>
        ///     Creates a document of one paragraph that holds <paramref name="text" /> as is, with each newline as a
        ///     line break.
        /// </summary>
        public static Document Plain(string text)
        {
            var runs = new List<Run>();

            if (!string.IsNullOrEmpty(text))
            {
                var start = 0;
                for (var i = 0; i <= text.Length; i++)
                {
                    if (i < text.Length && text[i] != '\n')
                        continue;

                    if (i > start)
                        runs.Add(new Run(text.Substring(start, i - start), RunKind.Plain));

                    if (i < text.Length)
                        runs.Add(Run.Break);

                    start = i + 1;
                }
            }

            return new Document(new[] { Block.Paragraph(runs) });
        }

        /// <summary>Adds the links of <paramref name="blocks" />, and of anything they hold, to <paramref name="links" />.</summary>
        static void Collect(IReadOnlyList<Block> blocks, List<Link> links)
        {
            foreach (var block in blocks)
            {
                foreach (var run in block.Runs)
                {
                    if (run.Link != null && !links.Contains(run.Link))
                        links.Add(run.Link);
                }

                Collect(block.Blocks, links);
            }
        }
    }
}
