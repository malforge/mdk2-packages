using System.Collections.Generic;
using System.Text;

namespace Lumel.Text
{
    /// <summary>Text with named slots, {name}: parsed once into a document, or filled in as plain text.</summary>
    /// <remarks>
    ///     A name is a letter or an underscore, then letters, digits and underscores; <c>{{</c> and <c>}}</c> are a brace.
    ///     Anything else in braces is only text.
    /// </remarks>
    public static class Template
    {
        const char Mark = '﷐';
        const char Unmark = '﷑';

        /// <summary>
        ///     Parses <paramref name="template" /> as markdown-lite into a document, with each slot becoming a run
        ///     whose text is the argument of that name.
        /// </summary>
        /// <remarks>
        ///     Parsed once, however often its arguments change: <see cref="RichText" /> puts them in as it lays the words out.
        /// </remarks>
        public static Document Parse(string template)
        {
            var names = new List<string>();
            var document = Markdown.Parse(Marked(template ?? "", names));

            if (names.Count == 0)
                return document;

            var blocks = new List<Block>(document.Blocks.Count);
            foreach (var block in document.Blocks)
                blocks.Add(SplitSlots(block, names));

            return new Document(blocks);
        }

        /// <summary>
        ///     Returns <paramref name="template" /> with every slot named <paramref name="name" /> replaced by
        ///     <paramref name="value" /> and each doubled brace reduced to a single brace; other slots are left as they
        ///     are, as plain text.
        /// </summary>
        public static string Fill(string template, string name, string value)
        {
            if (string.IsNullOrEmpty(template))
                return "";

            var filled = new StringBuilder(template.Length + (value?.Length ?? 0));
            var i = 0;

            while (i < template.Length)
            {
                var c = template[i];

                if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
                {
                    filled.Append(c);
                    i += 2;
                    continue;
                }

                var end = c == '{' ? NameEnd(template, i + 1) : -1;
                if (end < 0 || end >= template.Length || template[end] != '}')
                {
                    filled.Append(c);
                    i++;
                    continue;
                }

                var length = end - i - 1;
                if (name != null && length == name.Length && string.CompareOrdinal(template, i + 1, name, 0, length) == 0)
                    filled.Append(value);
                else
                    filled.Append(template, i, end - i + 1);

                i = end + 1;
            }

            return filled.ToString();
        }

        /// <summary>
        ///     Returns <paramref name="template" /> with each slot replaced by a marker holding its index among
        ///     <paramref name="names" />, and each doubled brace reduced to a single brace.
        /// </summary>
        static string Marked(string template, List<string> names)
        {
            var marked = new StringBuilder(template.Length);
            var i = 0;

            while (i < template.Length)
            {
                var c = template[i];

                if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
                {
                    marked.Append(c);
                    i += 2;
                    continue;
                }

                var end = c == '{' ? NameEnd(template, i + 1) : -1;
                if (end < 0 || end >= template.Length || template[end] != '}')
                {
                    marked.Append(c);
                    i++;
                    continue;
                }

                var name = template.Substring(i + 1, end - i - 1);
                var place = names.IndexOf(name);
                if (place < 0)
                {
                    place = names.Count;
                    names.Add(name);
                }

                marked.Append(Mark).Append(place).Append(Unmark);
                i = end + 1;
            }

            return marked.ToString();
        }

        /// <summary>
        ///     Returns the index where the name starting at <paramref name="start" /> ends, or -1 if no name starts
        ///     there.
        /// </summary>
        static int NameEnd(string text, int start)
        {
            var i = start;
            while (i < text.Length)
            {
                var c = text[i];
                var letter = c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';
                if (!letter && (i == start || c < '0' || c > '9'))
                    break;

                i++;
            }

            return i > start ? i : -1;
        }

        /// <summary>
        ///     Returns <paramref name="block" /> with the slots marked in its runs, and in the blocks it contains,
        ///     split out into runs of their own.
        /// </summary>
        static Block SplitSlots(Block block, List<string> names)
        {
            if (block.Blocks.Count > 0)
            {
                var blocks = new List<Block>(block.Blocks.Count);
                foreach (var inner in block.Blocks)
                    blocks.Add(SplitSlots(inner, names));

                block = block.WithBlocks(blocks);
            }

            if (block.Runs.Count == 0)
                return block;

            var runs = new List<Run>(block.Runs.Count);
            foreach (var run in block.Runs)
                Unmarked(run, names, runs);

            return block.WithRuns(runs);
        }

        /// <summary>
        ///     Adds <paramref name="run" /> to <paramref name="runs" />, split into its text and the slots marked in
        ///     it.
        /// </summary>
        static void Unmarked(Run run, List<string> names, List<Run> runs)
        {
            var text = run.Text;
            if (run.IsBreak || run is ImageRun || text.IndexOf(Mark) < 0)
            {
                runs.Add(run);
                return;
            }

            var linked = run as LinkRun;
            var link = linked?.Link;

            var i = 0;
            while (i < text.Length)
            {
                var start = text.IndexOf(Mark, i);
                var end = start < 0 ? -1 : text.IndexOf(Unmark, start);
                if (end < 0)
                {
                    runs.Add(Words(text.Substring(i), run, link));
                    return;
                }

                if (start > i)
                    runs.Add(Words(text.Substring(i, start - i), run, link));

                var name = names[int.Parse(text.Substring(start + 1, end - start - 1))];
                runs.Add(link == null ? Run.Slotted(name, run.Kind, run.Class, run.Color) : LinkRun.Slotted(link, name, run.Kind));
                i = end + 1;
            }
        }

        /// <summary>
        ///     Creates a run of <paramref name="text" /> formatted and in the span as <paramref name="from" /> is,
        ///     belonging to <paramref name="link" /> if one is given.
        /// </summary>
        static Run Words(string text, Run from, Link link)
        {
            return link == null ? new Run(text, from.Kind, from.Class, from.Color) : new LinkRun(link, text, from.Kind);
        }
    }
}
