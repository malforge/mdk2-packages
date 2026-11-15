using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Text
{
    partial class TextArea
    {
        /// <summary>The line numbers shown to the left of the text: their width and the numbers displayed.</summary>
        sealed class Gutter
        {
            readonly List<string> _numbers = new List<string>();
            readonly StringBuilder _digitText = new StringBuilder(1);

            float _digit;
            float _digitAt = -1f;
            string _digitIn;
            float _right;
            int _from = int.MaxValue;
            int _to = int.MinValue;

            /// <summary>
            ///     Gets the width of the gutter, including the numbers and the padding on either side, or 0 when lines
            ///     are not numbered.
            /// </summary>
            public float Width { get; private set; }

            /// <summary>
            ///     Computes the gutter width for <paramref name="lines" /> lines in <paramref name="font" /> at
            ///     <paramref name="size" />, using <paramref name="padding" /> on either side (the width of a digit if
            ///     null), or 0 if <paramref name="numbered" /> is false.
            /// </summary>
            public void Measure(IGraphics g, bool numbered, int lines, string font, float size, Thickness? padding)
            {
                if (!numbered)
                {
                    Width = 0f;
                    _right = 0f;
                    return;
                }

                var digit = DigitOf(g, font, size);
                var around = padding ?? new Thickness(digit, 0f, digit, 0f);
                Width = around.Left + Digits(lines) * digit + around.Right;
                _right = around.Right;
            }

            /// <summary>
            ///     Draws the line numbers from <paramref name="first" /> to <paramref name="last" />, with the first at
            ///     <paramref name="top" />, right-aligned against the text.
            /// </summary>
            public void Draw(IGraphics g, RectangleF view, int first, int last, float top, string font, float size, Color color)
            {
                if (Width <= 0f || color.A == 0)
                    return;

                Number(first, last);

                var edge = view.X + Width - _right;
                var y = top;

                g.PushClip(new RectangleF(view.X, view.Y, Width, view.Height));
                foreach (var number in _numbers)
                {
                    g.DrawText(font, number, new Vector2(edge, y), size, color, TextAlign.Right);
                    y += size;
                }

                g.PopClip();
            }

            /// <summary>Returns the number of decimal digits in <paramref name="count" />.</summary>
            static int Digits(int count)
            {
                var digits = 1;
                while (count >= 10)
                {
                    count /= 10;
                    digits++;
                }

                return digits;
            }

            /// <summary>
            ///     Generates the numbers for the lines from <paramref name="first" /> to <paramref name="last" />, only
            ///     when that range has changed.
            /// </summary>
            void Number(int first, int last)
            {
                if (_from == first && _to == last)
                    return;

                _from = first;
                _to = last;
                _numbers.Clear();

                for (var i = first; i <= last; i++)
                    _numbers.Add((i + 1).ToString(CultureInfo.InvariantCulture));
            }

            /// <summary>
            ///     Returns the width of the widest of the ten digits, measured only when the font or size has changed.
            /// </summary>
            float DigitOf(IGraphics g, string font, float size)
            {
                if (_digitAt == size && _digitIn == font)
                    return _digit;

                _digitAt = size;
                _digitIn = font;
                _digit = 0f;

                for (var d = 0; d < 10; d++)
                {
                    _digitText.Length = 0;
                    _digitText.Append((char)('0' + d));
                    _digit = Math.Max(_digit, g.MeasureText(font, _digitText, size).X);
                }

                return _digit;
            }
        }
    }
}
