using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VRageMath;

namespace Lumel.Text
{
    public static partial class Markdown
    {
        /// <summary>Parses the words inside one block of markdown-lite into runs: strong, emphasis, code spans, images, links, escapes and line breaks.</summary>
        /// <remarks>
        ///     Strong and emphasis follow CommonMark's delimiter runs, and <c>_</c> inside a word is only a character.
        /// </remarks>
        static class InlineParser
        {
            // The furthest an entity reference's semicolon can stand from its ampersand: a seven-digit number after "&#".
            const int Longest = 10;

            /// <summary>Adds the runs that <paramref name="text" /> contains to <paramref name="runs" />.</summary>
            public static void Parse(string text, IList<Run> runs, bool spanClasses)
            {
                var pieces = Split(text, spanClasses);
                Match(pieces);
                Emit(pieces, runs);
            }

            /// <summary>Whether <paramref name="c" /> is one of the ASCII punctuation marks a backslash escapes.</summary>
            public static bool IsAsciiPunctuation(char c)
            {
                return c >= '!' && c <= '/' || c >= ':' && c <= '@' || c >= '[' && c <= '`' || c >= '{' && c <= '~';
            }

            /// <summary>
            ///     Splits <paramref name="text" /> into words, code spans, line breaks and runs of delimiters.
            /// </summary>
            static List<Piece> Split(string text, bool spanClasses)
            {
                var pieces = new List<Piece>();
                var words = new StringBuilder();
                var i = 0;

                while (i < text.Length)
                {
                    var c = text[i];

                    if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]))
                    {
                        words.Append(text[i + 1]);
                        i += 2;
                        continue;
                    }

                    if (c == '\n')
                    {
                        Flush(pieces, words);
                        pieces.Add(new Piece { Kind = PieceKind.Break });
                        i++;
                        continue;
                    }

                    if (c == '`')
                    {
                        var ticks = Count(text, i, '`');
                        var close = Closing(text, i + ticks, ticks);
                        if (close < 0)
                        {
                            words.Append('`', ticks);
                            i += ticks;
                            continue;
                        }

                        Flush(pieces, words);
                        pieces.Add(new Piece { Kind = PieceKind.Code, Text = Code(text.Substring(i + ticks, close - i - ticks)) });
                        i = close + ticks;
                        continue;
                    }

                    if (c == '!' && i + 1 < text.Length && text[i + 1] == '[')
                    {
                        int end;
                        var image = Image(text, i, out end);
                        if (image != null)
                        {
                            Flush(pieces, words);
                            pieces.Add(image);
                            i = end;
                            continue;
                        }
                    }

                    if (c == '[')
                    {
                        int end;
                        var link = Link(text, i, out end);
                        if (link != null)
                        {
                            Flush(pieces, words);
                            pieces.Add(link);
                            i = end;
                            continue;
                        }

                        var span = Span(text, i, spanClasses, out end);
                        if (span != null)
                        {
                            Flush(pieces, words);
                            pieces.Add(span);
                            i = end;
                            continue;
                        }
                    }

                    if (c == '*' || c == '_')
                    {
                        var count = Count(text, i, c);
                        var before = i > 0 ? text[i - 1] : ' ';
                        var after = i + count < text.Length ? text[i + count] : ' ';
                        var left = !IsSpace(after) && (!IsPunctuation(after) || IsSpace(before) || IsPunctuation(before));
                        var right = !IsSpace(before) && (!IsPunctuation(before) || IsSpace(after) || IsPunctuation(after));

                        Flush(pieces, words);
                        pieces.Add(new Piece
                        {
                            Kind = PieceKind.Delimiter,
                            Mark = c,
                            Left = count,
                            Length = count,
                            Opens = c == '*' ? left : left && (!right || IsPunctuation(before)),
                            Closes = c == '*' ? right : right && (!left || IsPunctuation(after))
                        });

                        i += count;
                        continue;
                    }

                    if (c == '&')
                    {
                        int end;
                        var meant = Entity(text, i, out end);
                        if (meant != null)
                        {
                            words.Append(meant);
                            i = end;
                            continue;
                        }
                    }

                    words.Append(c);
                    i++;
                }

                Flush(pieces, words);
                return pieces;
            }

            /// <summary>
            ///     Returns the character that the entity reference at index <paramref name="at" /> stands for, with
            ///     <paramref name="end" /> set just past it; returns null if there is none, in which case the &amp; is
            ///     a plain character.
            /// </summary>
            /// <remarks>
            ///     A number in decimal or hexadecimal, as CommonMark reads one, and a short list of names. A number that names no
            ///     character stands for the replacement character, as CommonMark has it.
            /// </remarks>
            static string Entity(string text, int at, out int end)
            {
                end = at;
                var close = text.IndexOf(';', at + 1);
                if (close < 0 || close - at > Longest)
                    return null;

                var name = text.Substring(at + 1, close - at - 1);
                var meant = name.Length > 1 && name[0] == '#' ? Numbered(name.Substring(1)) : Named(name);
                if (meant != null)
                    end = close + 1;

                return meant;
            }

            /// <summary>
            ///     Returns the character that a numeric reference's <paramref name="digits" /> stand for, or null if
            ///     they are not a valid number.
            /// </summary>
            static string Numbered(string digits)
            {
                var hex = digits.Length > 1 && (digits[0] == 'x' || digits[0] == 'X');
                var number = hex ? digits.Substring(1) : digits;
                if (number.Length == 0 || number.Length > (hex ? 6 : 7))
                    return null;

                for (var i = 0; i < number.Length; i++)
                {
                    var c = number[i];
                    var digit = c >= '0' && c <= '9' || hex && (c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');
                    if (!digit)
                        return null;
                }

                var value = int.Parse(number, hex ? NumberStyles.HexNumber : NumberStyles.None, CultureInfo.InvariantCulture);
                if (value == 0 || value > 0x10FFFF || value >= 0xD800 && value <= 0xDFFF)
                    return "�";

                return char.ConvertFromUtf32(value);
            }

            /// <summary>
            ///     Returns the character that the named reference <paramref name="name" /> stands for, or null if the
            ///     name is not recognized.
            /// </summary>
            static string Named(string name)
            {
                switch (name)
                {
                    case "nbsp": return " ";
                    case "amp": return "&";
                    case "lt": return "<";
                    case "gt": return ">";
                    case "quot": return "\"";
                    case "apos": return "'";
                    default: return null;
                }
            }

            /// <summary>
            ///     Pairs closing delimiters with openers and marks where each pair's span starts and ends.
            /// </summary>
            /// <remarks>
            ///     CommonMark's own procedure, linear in the number of delimiters. A span is marked at its two ends, and
            ///     <see cref="Emit" /> counts the depth as it passes.
            /// </remarks>
            static void Match(List<Piece> pieces)
            {
                var stack = new List<int>();
                var floors = new int[12];
                for (var i = 0; i < floors.Length; i++)
                    floors[i] = -1;

                for (var closer = 0; closer < pieces.Count; closer++)
                {
                    var c = pieces[closer];
                    if (c.Kind != PieceKind.Delimiter)
                        continue;

                    if (!c.Closes)
                    {
                        if (c.Opens)
                            stack.Add(closer);

                        continue;
                    }

                    var kind = (c.Mark == '*' ? 0 : 6) + c.Length % 3 * 2 + (c.Opens ? 1 : 0);

                    while (c.Left > 0)
                    {
                        var at = Opener(pieces, stack, c, floors[kind]);
                        if (at < 0)
                        {
                            floors[kind] = closer;
                            break;
                        }

                        var opener = stack[at];
                        var o = pieces[opener];
                        var strong = o.Left >= 2 && c.Left >= 2;

                        if (strong)
                        {
                            pieces[opener + 1].StrongDelta++;
                            c.StrongDelta--;
                        }
                        else
                        {
                            pieces[opener + 1].EmphasisDelta++;
                            c.EmphasisDelta--;
                        }

                        o.Left -= strong ? 2 : 1;
                        c.Left -= strong ? 2 : 1;

                        stack.RemoveRange(at + 1, stack.Count - at - 1);
                        if (o.Left == 0)
                            stack.RemoveAt(at);
                    }

                    if (c.Left > 0 && c.Opens)
                        stack.Add(closer);
                }
            }

            /// <summary>
            ///     Returns the index in <paramref name="stack" /> of the nearest opener that <paramref name="closer" />
            ///     can close, searching no lower than <paramref name="floor" />, or -1 if there is none.
            /// </summary>
            /// <remarks>
            ///     CommonMark's rule of three: where either side can both open and close, a pair whose lengths add
            ///     to a multiple of three does not match unless both lengths are.
            /// </remarks>
            static int Opener(List<Piece> pieces, List<int> stack, Piece closer, int floor)
            {
                for (var i = stack.Count - 1; i >= 0 && stack[i] > floor; i--)
                {
                    var o = pieces[stack[i]];
                    if (o.Mark != closer.Mark || o.Left == 0)
                        continue;

                    if ((o.Closes || closer.Opens) && (o.Length + closer.Length) % 3 == 0
                                                   && (o.Length % 3 != 0 || closer.Length % 3 != 0))
                        continue;

                    return i;
                }

                return -1;
            }

            /// <summary>
            ///     Converts <paramref name="pieces" /> to runs in <paramref name="runs" />, merging neighbors that are
            ///     formatted alike.
            /// </summary>
            static void Emit(List<Piece> pieces, IList<Run> runs)
            {
                var words = new StringBuilder();
                var kind = RunKind.Plain;
                var strong = 0;
                var emphasis = 0;

                foreach (var piece in pieces)
                {
                    strong += piece.StrongDelta;
                    emphasis += piece.EmphasisDelta;

                    if (piece.Kind == PieceKind.Break)
                    {
                        Add(runs, words, kind);
                        runs.Add(Run.Break);
                        continue;
                    }

                    if (piece.Kind == PieceKind.Image)
                    {
                        Add(runs, words, kind);
                        var stressed = (strong > 0 ? RunKind.Strong : RunKind.Plain) | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain);
                        runs.Add(new ImageRun(((ImagePiece)piece).Picture, piece.Text, stressed));
                        continue;
                    }

                    if (piece.Kind == PieceKind.Link)
                    {
                        Add(runs, words, kind);
                        var stressed = (strong > 0 ? RunKind.Strong : RunKind.Plain) | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain);
                        var link = (LinkPiece)piece;
                        foreach (var part in link.Parts)
                        {
                            var image = part as ImagePiece;
                            if (image != null)
                                runs.Add(new ImageRun(image.Picture, image.Text, stressed, link.To));
                            else
                                runs.Add(new LinkRun(link.To, part.Text, stressed));
                        }

                        continue;
                    }

                    if (piece.Kind == PieceKind.Span)
                    {
                        Add(runs, words, kind);
                        var stressed = (strong > 0 ? RunKind.Strong : RunKind.Plain) | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain);
                        var span = (SpanPiece)piece;
                        foreach (var part in span.Parts)
                        {
                            var image = part as ImagePiece;
                            if (image != null)
                                runs.Add(new ImageRun(image.Picture, image.Text, stressed));
                            else
                                runs.Add(new Run(part.Text, stressed, span.Class, span.Color));
                        }

                        continue;
                    }

                    var text = piece.Kind == PieceKind.Delimiter ? new string(piece.Mark, piece.Left) : piece.Text;
                    if (text.Length == 0)
                        continue;

                    var its = (strong > 0 ? RunKind.Strong : RunKind.Plain)
                              | (emphasis > 0 ? RunKind.Emphasis : RunKind.Plain)
                              | (piece.Kind == PieceKind.Code ? RunKind.Code : RunKind.Plain);

                    if (its != kind)
                    {
                        Add(runs, words, kind);
                        kind = its;
                    }

                    words.Append(text);
                }

                Add(runs, words, kind);
            }

            static void Add(IList<Run> runs, StringBuilder words, RunKind kind)
            {
                if (words.Length == 0)
                    return;

                runs.Add(new Run(words.ToString(), kind));
                words.Clear();
            }

            static void Flush(List<Piece> pieces, StringBuilder words)
            {
                if (words.Length == 0)
                    return;

                pieces.Add(new Piece { Kind = PieceKind.Words, Text = words.ToString() });
                words.Clear();
            }

            /// <summary>
            ///     Parses the image written at index <paramref name="at" /> as ![words](locator), optionally followed
            ///     after the locator by a size =size and a turn such as 90deg, and sets <paramref name="end" /> to
            ///     where it ends; returns null if no image is written there.
            /// </summary>
            /// <remarks>The words in the brackets are only words, read for nothing, as an image's description. The size and the turn may come in either order, each once.</remarks>
            static ImagePiece Image(string text, int at, out int end)
            {
                end = at;

                var close = Bracket(text, at + 2, ']');
                if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
                    return null;

                var shut = Bracket(text, close + 2, ')');
                if (shut < 0)
                    return null;

                var inside = text.Substring(close + 2, shut - close - 2).Trim();
                var space = inside.IndexOf(' ');
                var locator = space < 0 ? inside : inside.Substring(0, space);
                if (locator.Length == 0)
                    return null;

                var width = float.NaN;
                var height = float.NaN;
                var turn = 0f;
                var sized = false;
                var turned = false;
                var i = space;
                string word;
                while ((word = NextWord(inside, ref i)) != null)
                {
                    if (!sized && IsSize(word, out width, out height))
                        sized = true;
                    else if (!turned && IsTurn(word, out turn))
                        turned = true;
                    else
                        return null;
                }

                end = shut + 1;
                return new ImagePiece
                {
                    Kind = PieceKind.Image,
                    Text = Unescaped(text, at + 2, close),
                    Picture = new Picture(locator, width, height, turn)
                };
            }

            /// <summary>
            ///     Parses the link written at index <paramref name="at" /> as [words](target) or
            ///     [words](target class=name) and sets
            ///     <paramref name="end" /> to where it ends; returns null if no link is written there.
            /// </summary>
            /// <remarks>A target holds no spaces, as a link destination in CommonMark does not; the words are only words.</remarks>
            static LinkPiece Link(string text, int at, out int end)
            {
                end = at;

                var parts = new List<Piece>();
                var close = LinkWords(text, at + 1, parts);
                if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
                    return null;

                var shut = Bracket(text, close + 2, ')');
                if (shut < 0)
                    return null;

                var inside = text.Substring(close + 2, shut - close - 2).Trim();
                if (inside.Length == 0)
                    return null;

                var space = 0;
                while (space < inside.Length && !IsSpace(inside[space]))
                    space++;

                var target = inside.Substring(0, space);
                string className = null;
                var i = space;
                string word;
                while ((word = NextWord(inside, ref i)) != null)
                {
                    if (className != null || !IsClass(word, out className))
                        return null;
                }

                end = shut + 1;
                return new LinkPiece
                {
                    Kind = PieceKind.Link,
                    Parts = parts,
                    To = new Link(target, className)
                };
            }

            /// <summary>
            ///     Parses the span written at index <paramref name="at" /> as [words]{color=name}, [words]{.class} or both,
            ///     and sets <paramref name="end" /> to where it ends; returns null if no span is written there.
            /// </summary>
            /// <remarks>A class is read only where <paramref name="classes" /> is true; elsewhere a span naming one is words.</remarks>
            static SpanPiece Span(string text, int at, bool classes, out int end)
            {
                end = at;

                var parts = new List<Piece>();
                var close = LinkWords(text, at + 1, parts);
                if (close < 0 || close + 1 >= text.Length || text[close + 1] != '{')
                    return null;

                var shut = Bracket(text, close + 2, '}');
                if (shut < 0)
                    return null;

                var inside = text.Substring(close + 2, shut - close - 2);
                string className = null;
                Color? color = null;
                var i = 0;
                string word;
                var any = false;
                while ((word = NextWord(inside, ref i)) != null)
                {
                    any = true;
                    Color named;
                    if (classes && className == null && word.Length > 1 && word[0] == '.' && IsName(word, 1))
                        className = word.Substring(1);
                    else if (color == null && word.StartsWith("color=", StringComparison.Ordinal) && SpanColors.TryRead(word.Substring(6), out named))
                        color = named;
                    else
                        return null;
                }

                if (!any)
                    return null;

                end = shut + 1;
                return new SpanPiece
                {
                    Kind = PieceKind.Span,
                    Parts = parts,
                    Class = className,
                    Color = color
                };
            }

            /// <summary>
            ///     Returns true if <paramref name="word" /> from <paramref name="from" /> on is letters, digits, - and _.
            /// </summary>
            static bool IsName(string word, int from)
            {
                for (var i = from; i < word.Length; i++)
                {
                    var c = word[i];
                    if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                        return false;
                }

                return true;
            }

            /// <summary>
            ///     Reads the words of a link from <paramref name="from" /> up to its closing bracket into
            ///     <paramref name="parts" />, each image in them a part of its own, and returns where the bracket is, or -1 if
            ///     none closes them.
            /// </summary>
            static int LinkWords(string text, int from, List<Piece> parts)
            {
                var words = new StringBuilder();
                for (var i = from; i < text.Length; i++)
                {
                    var c = text[i];
                    if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]))
                    {
                        words.Append(text[++i]);
                        continue;
                    }

                    if (c == ']')
                    {
                        if (words.Length > 0 || parts.Count == 0)
                            parts.Add(new Piece { Text = words.ToString() });

                        return i;
                    }

                    if (c == '!' && i + 1 < text.Length && text[i + 1] == '[')
                    {
                        int over;
                        var image = Image(text, i, out over);
                        if (image != null)
                        {
                            if (words.Length > 0)
                                parts.Add(new Piece { Text = words.ToString() });

                            words.Clear();
                            parts.Add(image);
                            i = over - 1;
                            continue;
                        }
                    }

                    if (c == '&')
                    {
                        int over;
                        var meant = Entity(text, i, out over);
                        if (meant != null)
                        {
                            words.Append(meant);
                            i = over - 1;
                            continue;
                        }
                    }

                    if (c == '[' || c == '(' || c == ')' || c == '\n')
                        return -1;

                    words.Append(c);
                }

                return -1;
            }

            /// <summary>
            ///     Returns the next word of <paramref name="inside" /> at or after <paramref name="at" /> and moves
            ///     <paramref name="at" /> past it, or returns null if there are no more or <paramref name="at" /> is negative.
            /// </summary>
            static string NextWord(string inside, ref int at)
            {
                if (at < 0)
                    return null;

                while (at < inside.Length && IsSpace(inside[at]))
                    at++;

                if (at >= inside.Length)
                    return null;

                var from = at;
                while (at < inside.Length && !IsSpace(inside[at]))
                    at++;

                return inside.Substring(from, at - from);
            }

            /// <summary>
            ///     Returns true if <paramref name="word" /> is <c>class=</c> and a name of letters, digits, - and _, which
            ///     <paramref name="name" /> is set to.
            /// </summary>
            static bool IsClass(string word, out string name)
            {
                const string prefix = "class=";
                name = null;
                if (word.Length <= prefix.Length || !word.StartsWith(prefix, StringComparison.Ordinal) || !IsName(word, prefix.Length))
                    return false;

                name = word.Substring(prefix.Length);
                return true;
            }

            /// <summary>
            ///     Returns the text from <paramref name="from" /> up to <paramref name="to" /> with each escaped
            ///     punctuation mark reduced to the mark itself.
            /// </summary>
            static string Unescaped(string text, int from, int to)
            {
                if (text.IndexOf('\\', from, to - from) < 0)
                    return text.Substring(from, to - from);

                var words = new StringBuilder(to - from);
                for (var i = from; i < to; i++)
                {
                    if (text[i] == '\\' && i + 1 < to && IsAsciiPunctuation(text[i + 1]))
                        i++;

                    words.Append(text[i]);
                }

                return words.ToString();
            }

            /// <summary>
            ///     Returns true if <paramref name="text" /> is an image size: =WxH, or =W, =Wx or =xH for one side,
            ///     each a number above 0.
            /// </summary>
            static bool IsSize(string text, out float width, out float height)
            {
                width = float.NaN;
                height = float.NaN;

                if (text.Length < 2 || text[0] != '=')
                    return false;

                var by = text.IndexOf('x');
                var across = by < 0 ? text.Substring(1) : text.Substring(1, by - 1);
                var down = by < 0 ? "" : text.Substring(by + 1);

                return (across.Length > 0 || down.Length > 0)
                       && (across.Length == 0 || IsNumber(across, out width))
                       && (down.Length == 0 || IsNumber(down, out height));
            }

            /// <summary>
            ///     Returns true if <paramref name="text" /> is an image rotation: degrees clockwise followed by deg,
            ///     such as 90deg or -12.5deg.
            /// </summary>
            static bool IsTurn(string text, out float degrees)
            {
                degrees = 0f;
                if (text.Length < 4 || !text.EndsWith("deg", StringComparison.Ordinal))
                    return false;

                float read;
                if (!float.TryParse(text.Substring(0, text.Length - 3), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out read) || float.IsInfinity(read))
                    return false;

                degrees = read;
                return true;
            }

            static bool IsNumber(string text, out float number)
            {
                if (float.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number) && number > 0f)
                    return true;

                number = float.NaN;
                return false;
            }

            /// <summary>
            ///     Returns the index of the next <paramref name="closer" /> at or after <paramref name="from" />, or -1
            ///     if another bracket or the end of the line comes first; escaped characters are skipped.
            /// </summary>
            /// <remarks>
            ///     Stopping at the next bracket keeps reading linear: an unclosed image or link is given up there rather than
            ///     searched for to the end of the text.
            /// </remarks>
            static int Bracket(string text, int from, char closer)
            {
                for (var i = from; i < text.Length; i++)
                {
                    var c = text[i];
                    if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]))
                    {
                        i++;
                        continue;
                    }

                    if (c == closer)
                        return i;

                    if (c == '[' || c == ']' || c == '(' || c == ')' || c == '\n')
                        return -1;
                }

                return -1;
            }

            static int Count(string text, int from, char c)
            {
                var count = 0;
                while (from + count < text.Length && text[from + count] == c)
                    count++;

                return count;
            }

            /// <summary>
            ///     Returns the start index, at or after <paramref name="from" />, of a run of exactly
            ///     <paramref name="ticks" /> backticks, or -1 if there is none.
            /// </summary>
            static int Closing(string text, int from, int ticks)
            {
                var i = from;
                while (i < text.Length)
                {
                    if (text[i] != '`')
                    {
                        i++;
                        continue;
                    }

                    var run = Count(text, i, '`');
                    if (run == ticks)
                        return i;

                    i += run;
                }

                return -1;
            }

            /// <summary>
            ///     Returns the content of a code span: line breaks become spaces, and one space is stripped from each
            ///     end if both ends have one.
            /// </summary>
            static string Code(string content)
            {
                content = content.Replace('\n', ' ');

                if (content.Length >= 2 && content[0] == ' ' && content[content.Length - 1] == ' ' && content.Trim().Length > 0)
                    content = content.Substring(1, content.Length - 2);

                return content;
            }

            static bool IsSpace(char c)
            {
                return char.IsWhiteSpace(c);
            }

            static bool IsPunctuation(char c)
            {
                return char.IsPunctuation(c) || char.IsSymbol(c);
            }

            enum PieceKind
            {
                Words,
                Code,
                Break,
                Delimiter,
                Image,
                Link,
                Span
            }

            /// <summary>One cut of the text, along with where the spans it belongs to begin and end.</summary>
            class Piece
            {
                public PieceKind Kind;
                public string Text = "";
                public char Mark;

                /// <summary>The number of delimiters still unmatched.</summary>
                public int Left;

                /// <summary>The original length of the delimiter run, which the rule of three uses.</summary>
                public int Length;

                public bool Opens;
                public bool Closes;

                /// <summary>How many strong spans deeper this piece is than the one before it.</summary>
                public int StrongDelta;

                /// <summary>How many emphasis spans deeper this piece is than the one before it.</summary>
                public int EmphasisDelta;
            }

            /// <summary>An image, whose words are its description.</summary>
            sealed class ImagePiece : Piece
            {
                /// <summary>The picture the image shows.</summary>
                public Picture Picture;
            }

            /// <summary>A link, made of its words and the images among them.</summary>
            sealed class LinkPiece : Piece
            {
                /// <summary>Where the link leads.</summary>
                public Link To;

                /// <summary>The link's words and images, in order.</summary>
                public List<Piece> Parts;
            }

            /// <summary>A span, made of its words and the images among them, with the class and color it gives them.</summary>
            sealed class SpanPiece : Piece
            {
                /// <summary>The span's class, or null.</summary>
                public string Class;

                /// <summary>The span's color, or null.</summary>
                public Color? Color;

                /// <summary>The span's words and images, in order.</summary>
                public List<Piece> Parts;
            }
        }
    }
}
