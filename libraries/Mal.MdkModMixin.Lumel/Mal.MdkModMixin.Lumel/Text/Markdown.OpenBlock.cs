using System;
using System.Collections.Generic;
using System.Text;

namespace Lumel.Text
{
    public static partial class Markdown
    {
        /// <summary>The block whose words are being read, before it is created.</summary>
        sealed class OpenBlock
        {
            public readonly StringBuilder Words = new StringBuilder();
            public BlockKind? Kind;
            int _number;
            readonly bool _spanClasses;

            /// <summary>Creates an open block whose words read a span's class if <paramref name="spanClasses" /> is true.</summary>
            public OpenBlock(bool spanClasses)
            {
                _spanClasses = spanClasses;
            }

            public void Start(BlockKind kind, int number, string words)
            {
                Kind = kind;
                _number = number;
                Words.Append(words);
            }

            /// <summary>Returns the last line of the words being read.</summary>
            public string LastLine()
            {
                var text = Words.ToString();
                return text.Substring(text.LastIndexOf('\n') + 1);
            }

            /// <summary>
            ///     Creates the block from its words and adds it to <paramref name="blocks" />, if one is open.
            /// </summary>
            public void Close(List<Block> blocks)
            {
                if (Kind == null)
                    return;

                var runs = new List<Run>();
                InlineParser.Parse(Words.ToString(), runs, _spanClasses);
                blocks.Add(Made(Kind.Value, _number, runs));
                Drop();
            }

            /// <summary>Discards the block without creating it.</summary>
            public void Drop()
            {
                Kind = null;
                Words.Clear();
            }

            static Block Made(BlockKind kind, int number, List<Run> runs)
            {
                switch (kind)
                {
                    case BlockKind.Paragraph:
                        return Block.Paragraph(runs);
                    case BlockKind.Heading:
                        return Block.Heading(number, runs);
                    case BlockKind.BulletListItem:
                        return Block.BulletListItem(runs);
                    case BlockKind.OrderedListItem:
                        return Block.OrderedListItem(number, runs);
                    default:
                        throw new ArgumentException(kind + " is not read as words: only a paragraph, a heading or a list item is.", nameof(kind));
                }
            }
        }
    }
}
