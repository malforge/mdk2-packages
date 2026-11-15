using System;
using System.Collections.Generic;
using System.Text;

namespace Lumel.Text
{
    /// <summary>Converts markdown-lite to a document, and makes any string safe to put into markdown-lite.</summary>
    /// <remarks>
    ///     Markdown-lite is CommonMark's headings (<c>#</c> to <c>######</c>), rules (<c>---</c>, <c>***</c>,
    ///     <c>___</c>, spaced or not), one level of lists (<c>-</c>, <c>*</c> or <c>+</c>, and <c>1.</c> or
    ///     <c>1)</c>), quotes (<c>&gt;</c>, one level), callouts (<c>:::</c>), GitHub's tables, strong and emphasis with <c>*</c> and <c>_</c>, code spans, images
    ///     (<c>![description](locator)</c>), links (<c>[words](target)</c>) and backslash escapes. A single newline
    ///     is a line break and a blank line ends a block. Anything else reads as the text it is.
    ///     <para>
    ///         A table's delimiter row may give a column a star for each share it takes of the width the table
    ///         has to spare; a table with no star is as wide as its cells. A heading row of empty cells is no
    ///         heading at all, which is how a table without one is written.
    ///     </para>
    ///     <para>
    ///         An image is as tall as its line unless <c>=WxH</c>, <c>=W</c> or <c>=xH</c> after its locator says
    ///         otherwise, a side not given following the picture's shape, and <c>90deg</c> there turns it clockwise.
    ///         An image's locator and a link's target are strings only the host reads, and hold no spaces.
    ///         <c>class=name</c> after a link's target gives it a class, whose look the host's style says, and any other
    ///         word there or after an image's locator leaves what was written as words. An image inside a link's words is part of the
    ///         link, and is drawn in its color.
    ///     </para>
    ///     <para>
    ///         A span is words in brackets followed by braces: <c>[words]{color=red}</c> draws them in a color, named
    ///         (<c>black</c>, <c>white</c>, <c>gray</c>, <c>silver</c>, <c>red</c>, <c>maroon</c>, <c>orange</c>,
    ///         <c>yellow</c>, <c>olive</c>, <c>lime</c>, <c>green</c>, <c>aqua</c>, <c>teal</c>, <c>blue</c>,
    ///         <c>navy</c>, <c>fuchsia</c>, <c>purple</c>) or as <c>#rrggbb</c> or <c>#aarrggbb</c>. Where the parse reads span classes,
    ///         <c>{.name}</c> gives the span a class whose look the host's style says, and may stand with a color, the
    ///         color winning. Anything else in the braces leaves what was written as words.
    ///     </para>
    ///     <para>
    ///         A line of three or more <c>:</c> and then classes opens a callout, and a line of only <c>:</c> closes the
    ///         innermost one; what is between is the callout's blocks. The classes are letters, digits, <c>-</c> and
    ///         <c>_</c>. <c>left</c>, <c>center</c> and <c>right</c> set where the callout's lines sit, and every other
    ///         class looks as the host's style says; a callout inside a callout adds its own to those around it. A
    ///         callout never closed runs to the end, and a closing line with no callout open is words. Callouts nest
    ///         <see cref="MaxNestingDepth" /> deep at most; a fence that would open one deeper is words.
    ///     </para>
    /// </remarks>
    public static partial class Markdown
    {
        /// <summary>The maximum depth of nested callouts in a document.</summary>
        /// <remarks>
        ///     Each callout is read and laid out one call deeper, and running the stack out takes the game down.
        /// </remarks>
        public const int MaxNestingDepth = 16;

        /// <summary>Parses <paramref name="source" /> into a document, reading a span's class as words.</summary>
        public static Document Parse(string source)
        {
            return Parse(source, false);
        }

        /// <summary>
        ///     Parses <paramref name="source" /> into a document, reading <c>{.name}</c> after a span's words as its class
        ///     if <paramref name="spanClasses" /> is true, and as words otherwise.
        /// </summary>
        public static Document Parse(string source, bool spanClasses)
        {
            var blocks = new List<Block>();
            if (string.IsNullOrEmpty(source))
                return new Document(blocks);

            var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            Read(lines, 0, lines.Length, blocks, false, 0, Pairs(lines), spanClasses);            return new Document(blocks);
        }

        /// <summary>
        ///     Returns <paramref name="text" /> with every ASCII punctuation character escaped, so that it reads
        ///     literally.
        /// </summary>
        /// <remarks>For putting a name somebody typed into markdown-lite.</remarks>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var escaped = new StringBuilder(text.Length + 8);
            for (var i = 0; i < text.Length; i++)
            {
                if (InlineParser.IsAsciiPunctuation(text[i]))
                    escaped.Append('\\');

                escaped.Append(text[i]);
            }

            return escaped.ToString();
        }

        /// <summary>
        ///     Reads the lines from <paramref name="from" /> up to <paramref name="to" /> into
        ///     <paramref name="blocks" />, at callout nesting depth <paramref name="nested" /> and with fences paired
        ///     as <paramref name="closes" /> says; inside a quote, a line starting with &gt; is plain text.
        /// </summary>
        static void Read(string[] lines, int from, int to, List<Block> blocks, bool quoted, int nested, int[] closes, bool spanClasses)
        {
            var open = new OpenBlock(spanClasses);
            OpenTable table = null;

            for (var i = from; i < to; i++)
            {
                var line = lines[i].Trim();

                if (line.Length == 0)
                {
                    open.Close(blocks);
                    Close(blocks, ref table);
                    continue;
                }

                if (table != null)
                {
                    if (line.IndexOf('|') >= 0)
                    {
                        table.Rows.Add(Row(table.Columns.Count, line, false, spanClasses));
                        continue;
                    }

                    Close(blocks, ref table);
                }

                List<string> classes;
                if (nested < MaxNestingDepth && IsFence(line, out classes) && classes.Count > 0)
                {
                    open.Close(blocks);
                    i = Callout(lines, i, to, blocks, quoted, classes, nested, closes, spanClasses) - 1;
                    continue;
                }

                if (!quoted && line[0] == '>')
                {
                    open.Close(blocks);
                    i = Quote(lines, i, to, blocks, nested, spanClasses) - 1;
                    continue;
                }

                int number;
                string rest;

                if (IsRule(line))
                {
                    open.Close(blocks);
                    blocks.Add(Block.Rule());
                    continue;
                }

                if (IsHeading(line, out number, out rest))
                {
                    open.Close(blocks);
                    open.Start(BlockKind.Heading, number, rest);
                    open.Close(blocks);
                    continue;
                }

                if (IsBullet(line, out rest))
                {
                    open.Close(blocks);
                    open.Start(BlockKind.BulletListItem, 0, rest);
                    continue;
                }

                if (IsOrdered(line, out number, out rest))
                {
                    open.Close(blocks);
                    open.Start(BlockKind.OrderedListItem, number, rest);
                    continue;
                }

                List<Column> columns;
                if (open.Kind == BlockKind.Paragraph && IsDelimiterRow(line, out columns) && Cells(open.LastLine()).Count == columns.Count)
                {
                    table = Table(blocks, open, columns, spanClasses);
                    continue;
                }

                if (open.Kind == null)
                    open.Start(BlockKind.Paragraph, 0, line);
                else
                    open.Words.Append('\n').Append(line);
            }

            open.Close(blocks);
            Close(blocks, ref table);
        }

        /// <summary>
        ///     Reads the quote that starts at line <paramref name="from" /> into <paramref name="blocks" />, up to the
        ///     first line before <paramref name="to" /> that does not start with &gt;, and returns the index of that
        ///     line.
        /// </summary>
        /// <remarks>Each line loses its <c>&gt;</c> and one space after it.</remarks>
        static int Quote(string[] lines, int from, int to, List<Block> blocks, int nested, bool spanClasses)
        {
            var end = from;
            while (end < to)
            {
                var line = lines[end].Trim();
                if (line.Length == 0 || line[0] != '>')
                    break;

                end++;
            }

            var inner = new string[end - from];
            for (var i = 0; i < inner.Length; i++)
            {
                var line = lines[from + i].Trim();
                inner[i] = line.Length > 1 && line[1] == ' ' ? line.Substring(2) : line.Substring(1);
            }

            var quoted = new List<Block>();
            Read(inner, 0, inner.Length, quoted, true, nested, Pairs(inner), spanClasses);
            blocks.Add(Block.Quote(quoted));
            return end;
        }

        /// <summary>
        ///     Reads the callout opened at line <paramref name="from" /> with <paramref name="classes" /> into
        ///     <paramref name="blocks" />, up to the fence that closes it or <paramref name="to" />, and returns the
        ///     index where reading continues.
        /// </summary>
        /// <remarks>The fence closing it is the one <paramref name="closes" /> pairs it with, so a callout inside it closes first.</remarks>
        static int Callout(string[] lines, int from, int to, List<Block> blocks, bool quoted, List<string> classes, int nested, int[] closes, bool spanClasses)
        {
            var close = closes[from];
            var end = close >= 0 && close < to ? close : to;

            var held = new List<Block>();
            Read(lines, from + 1, end, held, quoted, nested + 1, closes, spanClasses);
            blocks.Add(Block.Callout(classes, held));
            return end < to ? end + 1 : to;
        }

        /// <summary>
        ///     Returns, for each line of <paramref name="lines" /> that opens a callout, the index of the line with its
        ///     closing fence, or -1 if there is none; returns null if no line opens a callout.
        /// </summary>
        /// <remarks>
        ///     One pass, so finding where a callout ends costs nothing however deep callouts nest. A closing fence with no
        ///     callout open pairs with nothing, and is words.
        /// </remarks>
        static int[] Pairs(string[] lines)
        {
            int[] closes = null;
            List<int> open = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var fence = FenceOf(lines[i]);
                if (fence == Fence.Opens)
                {
                    if (closes == null)
                    {
                        closes = new int[lines.Length];
                        for (var j = 0; j < closes.Length; j++)
                            closes[j] = -1;

                        open = new List<int>();
                    }

                    open.Add(i);
                }
                else if (fence == Fence.Closes && open != null && open.Count > 0)
                {
                    closes[open[open.Count - 1]] = i;
                    open.RemoveAt(open.Count - 1);
                }
            }

            return closes;
        }

        /// <summary>
        ///     Classifies <paramref name="line" /> as a fence the way <see cref="IsFence" /> does, without building its
        ///     classes.
        /// </summary>
        static Fence FenceOf(string line)
        {
            var start = 0;
            var end = line.Length;
            while (start < end && char.IsWhiteSpace(line[start]))
                start++;

            while (end > start && char.IsWhiteSpace(line[end - 1]))
                end--;

            if (end - start < 3 || line[start] != ':' || line[start + 1] != ':' || line[start + 2] != ':')
                return Fence.None;

            var at = start + 3;
            while (at < end && line[at] == ':')
                at++;

            var named = false;
            for (; at < end; at++)
            {
                var c = line[at];
                if (c == ' ' || c == '\t')
                    continue;

                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return Fence.None;

                named = true;
            }

            return named ? Fence.Opens : Fence.Closes;
        }

        /// <summary>
        ///     Returns true if <paramref name="line" /> is a callout fence: three or more colons followed by class
        ///     names made of letters, digits, - and _, or no classes for a fence that closes a callout.
        /// </summary>
        static bool IsFence(string line, out List<string> classes)
        {
            classes = null;
            if (line.Length < 3 || line[0] != ':' || line[1] != ':' || line[2] != ':')
                return false;

            var at = 3;
            while (at < line.Length && line[at] == ':')
                at++;

            var words = line.Substring(at).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                for (var i = 0; i < word.Length; i++)
                {
                    if (!char.IsLetterOrDigit(word[i]) && word[i] != '-' && word[i] != '_')
                        return false;
                }
            }

            classes = new List<string>(words);
            return true;
        }

        /// <summary>Adds the table being read, if there is one, to <paramref name="blocks" />.</summary>
        static void Close(List<Block> blocks, ref OpenTable table)
        {
            if (table == null)
                return;

            blocks.Add(Block.Table(table.Columns, table.Rows));
            table = null;
        }

        /// <summary>
        ///     Creates a table with <paramref name="columns" />, taking its heading row from the last line of the
        ///     paragraph being read and leaving the earlier lines as a paragraph.
        /// </summary>
        /// <remarks>As GitHub's own reader does, so a table can start straight after the words that introduce it.</remarks>
        static OpenTable Table(List<Block> blocks, OpenBlock open, List<Column> columns, bool spanClasses)
        {
            var heading = open.LastLine();

            if (heading.Length < open.Words.Length)
            {
                open.Words.Length -= heading.Length + 1;
                open.Close(blocks);
            }
            else
                open.Drop();

            var table = new OpenTable(columns);

            var cells = Cells(heading);
            foreach (var cell in cells)
            {
                if (cell.Trim().Length > 0)
                {
                    table.Rows.Add(Row(columns.Count, heading, true, spanClasses));
                    break;
                }
            }

            return table;
        }

        /// <summary>
        ///     Creates the row written as <paramref name="line" /> with <paramref name="count" /> cells, padding
        ///     missing cells with empty ones and dropping extra ones.
        /// </summary>
        static Block Row(int count, string line, bool isHeader, bool spanClasses)
        {
            var cells = Cells(line);
            var row = new List<Block>(count);

            for (var i = 0; i < count; i++)
            {
                var runs = new List<Run>();
                InlineParser.Parse(i < cells.Count ? cells[i].Trim() : "", runs, spanClasses);
                row.Add(Block.Cell(isHeader, runs));
            }

            return Block.Row(isHeader, row);
        }

        /// <summary>
        ///     Returns the cells of a table row: the text between its pipes, ignoring a pipe at either end and keeping
        ///     an escaped pipe in its cell.
        /// </summary>
        static List<string> Cells(string line)
        {
            var cells = new List<string>();
            var from = line.Length > 0 && line[0] == '|' ? 1 : 0;
            var cell = new StringBuilder();

            for (var i = from; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '\\' && i + 1 < line.Length && line[i + 1] == '|')
                {
                    cell.Append("\\|");
                    i++;
                    continue;
                }

                if (c == '|')
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }

                cell.Append(c);
            }

            if (cell.Length > 0 || line.Length == 0 || line[line.Length - 1] != '|')
                cells.Add(cell.ToString());

            return cells;
        }

        /// <summary>
        ///     Returns true if <paramref name="line" /> is a table delimiter row: each cell holds dashes, with a colon
        ///     at either end for how the column's cells align and a star for each share of spare width the column
        ///     takes.
        /// </summary>
        static bool IsDelimiterRow(string line, out List<Column> columns)
        {
            columns = null;

            // Asked of every line under a paragraph, so the first character turns nearly all of them away
            // before the line is searched.
            var first = line[0];
            if (first != '|' && first != '-' && first != ':' && first != '*' || line.IndexOf('|') < 0 || line.IndexOf('-') < 0)
                return false;

            var cells = Cells(line);
            var found = new List<Column>(cells.Count);

            foreach (var raw in cells)
            {
                var cell = raw.Trim();
                var weight = 0;
                var marks = new StringBuilder(cell.Length);
                for (var j = 0; j < cell.Length; j++)
                {
                    if (cell[j] == '*')
                        weight++;
                    else
                        marks.Append(cell[j]);
                }

                var bare = marks.ToString();
                var left = bare.Length > 0 && bare[0] == ':';
                var right = bare.Length > 1 && bare[bare.Length - 1] == ':';
                var dashes = bare.Substring(left ? 1 : 0, bare.Length - (left ? 1 : 0) - (right ? 1 : 0));
                if (dashes.Length == 0 || dashes.Trim('-').Length != 0)
                    return false;

                found.Add(new Column(left && right ? TextAlignment.Center : right ? TextAlignment.Right : TextAlignment.Left, weight));
            }

            columns = found;
            return true;
        }

        /// <summary>
        ///     Returns true if <paramref name="line" /> is a rule: three or more of one of -, * or _, and nothing else
        ///     but spaces.
        /// </summary>
        static bool IsRule(string line)
        {
            var mark = line[0];
            if (mark != '-' && mark != '*' && mark != '_')
                return false;

            var count = 0;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == mark)
                    count++;
                else if (line[i] != ' ' && line[i] != '\t')
                    return false;
            }

            return count >= 3;
        }

        /// <summary>
        ///     Returns true if <paramref name="line" /> is a heading: one to six # followed by a space or nothing, with
        ///     a closing run of # after a space dropped; also returns the heading level and the remaining text.
        /// </summary>
        static bool IsHeading(string line, out int level, out string rest)
        {
            level = 0;
            rest = null;

            while (level < line.Length && line[level] == '#')
                level++;

            if (level == 0 || level > 6 || level < line.Length && line[level] != ' ' && line[level] != '\t')
                return false;

            rest = line.Substring(level).Trim();

            var end = rest.Length;
            while (end > 0 && rest[end - 1] == '#')
                end--;

            if (end == 0)
                rest = "";
            else if (end < rest.Length && (rest[end - 1] == ' ' || rest[end - 1] == '\t'))
                rest = rest.Substring(0, end).TrimEnd();

            return true;
        }

        /// <summary>
        ///     Returns true if <paramref name="line" /> starts a bullet item: -, * or +, followed by a space.
        /// </summary>
        static bool IsBullet(string line, out string rest)
        {
            rest = null;

            if (line.Length < 2 || line[0] != '-' && line[0] != '*' && line[0] != '+' || line[1] != ' ' && line[1] != '\t')
                return false;

            rest = line.Substring(2).Trim();
            return true;
        }

        /// <summary>
        ///     Returns true if <paramref name="line" /> starts an ordered list item: one to nine digits, then . or ),
        ///     then a space.
        /// </summary>
        static bool IsOrdered(string line, out int number, out string rest)
        {
            number = 0;
            rest = null;

            var digits = 0;
            while (digits < line.Length && digits < 10 && line[digits] >= '0' && line[digits] <= '9')
                digits++;

            if (digits == 0 || digits > 9 || digits + 1 >= line.Length)
                return false;

            var mark = line[digits];
            var after = line[digits + 1];
            if (mark != '.' && mark != ')' || after != ' ' && after != '\t')
                return false;

            number = int.Parse(line.Substring(0, digits));
            rest = line.Substring(digits + 2).Trim();
            return true;
        }

        /// <summary>The table whose rows are being read, before it is created.</summary>
        sealed class OpenTable
        {
            public readonly List<Column> Columns;
            public readonly List<Block> Rows = new List<Block>();

            public OpenTable(List<Column> columns)
            {
                Columns = columns;
            }
        }

        /// <summary>The kind of fence a line is.</summary>
        enum Fence
        {
            None,
            Opens,
            Closes
        }
    }
}
