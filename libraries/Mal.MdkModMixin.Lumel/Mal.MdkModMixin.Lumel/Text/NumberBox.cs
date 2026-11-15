using System;
using System.Globalization;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRage.Input;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>
    ///     A text box for a number: as wide as the widest number it expects, accepting only text that can become a
    ///     number, and stepped with the up and down keys.
    /// </summary>
    /// <remarks>
    ///     What <see cref="FormatString" /> writes around the number, such as a unit, shows around the text while there is
    ///     any, and is no part of <see cref="TextBox.Text" /> or <see cref="Value" />.
    /// </remarks>
    public class NumberBox : TextBox
    {
        // Two numbers sharing no digit, longer than any format keeps, so all the two share in writing is the format's own words.
        const double FirstSample = 1.1111111111111;
        const double SecondSample = 2.2222222222222;

        readonly StringBuilder _measuring = new StringBuilder(16);

        string _format;
        string _before = "";
        string _after = "";
        string _widest = "00000";
        string _measuredWidest;
        string _measuredFont;
        float _measuredSize;
        float _widestWidth;
        double _value = double.NaN;

        /// <summary>
        ///     Creates an empty box that accepts any number from 0 up, is five digits wide, and steps by 1.
        /// </summary>
        public NumberBox()
        {
            SmallChange = 1;
            Minimum = 0;
            FormatString = "0.##########";
        }

        /// <summary>
        ///     Gets or sets the style of this number box, or null to use the default number box style of its host
        ///     theme.
        /// </summary>
        public new IStyle<NumberBox> Style
        {
            get { return (IStyle<NumberBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Raised when <see cref="Value" /> changes, whether by an edit, by setting it, or by setting the text.
        /// </summary>
        public event Action ValueChanged;

        /// <summary>
        ///     Gets or sets the number in the box, or NaN while the typed text is not yet a number; setting it writes
        ///     the number as <see cref="FormatString" /> specifies.
        /// </summary>
        public double Value
        {
            get { return _value; }
            set { Text = double.IsNaN(value) ? "" : Written(value); }
        }

        /// <summary>Gets or sets the widest number the box expects to hold, which determines its width.</summary>
        public string Widest
        {
            get { return _widest; }
            set
            {
                value = value ?? "";
                if (_widest == value)
                    return;

                _widest = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether the box accepts only whole numbers.</summary>
        public bool IsInteger { get; set; }

        /// <summary>Gets or sets how far one press of up or down changes the number.</summary>
        public double SmallChange { get; set; }

        /// <summary>
        ///     Gets or sets the lowest value that up and down move to, which also determines whether a minus sign can
        ///     be typed, or null for no minimum.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>Gets or sets the highest value that up and down move to, or null for no maximum.</summary>
        public double? Maximum { get; set; }

        /// <summary>
        ///     Gets or sets the format used to write a number changed by up or down or set as <see cref="Value" />, as
        ///     when formatting a double, where anything it writes around the number, such as the unit of
        ///     <c>0.0# 'kg'</c>, shows around the box's text.
        /// </summary>
        /// <remarks>
        ///     A change shows at once, and leaves the text as it is. Throws for a format that changes the number's size, as
        ///     a bare percent sign does, or writes no number; a percent sign in quotes is a word.
        /// </remarks>
        public string FormatString
        {
            get { return _format; }
            set
            {
                var first = FirstSample.ToString(value, CultureInfo.InvariantCulture);
                var second = SecondSample.ToString(value, CultureInfo.InvariantCulture);
                var lead = SharedLead(first, second);
                var trail = SharedTrail(first, second);
                double number;
                if (lead + trail >= first.Length
                    || !double.TryParse(first.Substring(lead, first.Length - lead - trail), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                    || (int)number != 1)
                    throw new ArgumentException("A number box's format writes the number at its size, and " + value + " does not.", nameof(value));

                _format = value;
                _before = first.Substring(0, lead);
                _after = first.Substring(first.Length - trail);
                Surround(_before, _after);
            }
        }

        /// <summary>Whether a minus sign may be typed: when there is no minimum or the minimum is below 0.</summary>
        bool AllowsNegative => !Minimum.HasValue || Minimum.Value < 0;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<NumberBox>;
        }

        /// <inheritdoc />
        /// <remarks>Up and down move the number a step.</remarks>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (!ctrl && (key == MyKeys.Up || key == MyKeys.Down))
            {
                StepBy(key == MyKeys.Up ? 1 : -1);
                return true;
            }

            return base.OnKeyDown(input, key, shift, ctrl);
        }

        /// <summary>
        ///     Moves the number by <paramref name="by" /> steps from the step nearest the typed value, within its
        ///     bounds, as an edit.
        /// </summary>
        public void StepBy(int by)
        {
            double number;
            if (!double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                number = 0;

            var step = SmallChange > 0 ? SmallChange : 1;
            number = Math.Round(number / step) * step + by * step;

            if (Minimum.HasValue && number < Minimum.Value)
                number = Minimum.Value;

            if (Maximum.HasValue && number > Maximum.Value)
                number = Maximum.Value;

            SelectAll();
            Insert(Written(number));
        }

        /// <inheritdoc />
        /// <remarks>Whatever can still become a number: digits, a point unless it <see cref="IsInteger" />, and a leading minus where <see cref="Minimum" /> allows one.</remarks>
        protected override bool IsTextAllowed(string text)
        {
            var point = false;
            var negative = AllowsNegative;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '0' && c <= '9')
                    continue;

                if (c == '-' && i == 0 && negative)
                    continue;

                if (c == '.' && !point && !IsInteger)
                {
                    point = true;
                    continue;
                }

                return false;
            }

            return base.IsTextAllowed(text);
        }

        /// <inheritdoc />
        /// <remarks>The widest expected number is measured once each time it, the font or the size changes.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var size = base.MeasureContent(g, available);

            if (_measuredWidest != _widest || _measuredFont != Font || _measuredSize != FontSize)
            {
                _measuring.Clear();
                _measuring.Append(_widest);
                _widestWidth = g.MeasureText(Font, _measuring, FontSize).X;
                _measuredWidest = _widest;
                _measuredFont = Font;
                _measuredSize = FontSize;
            }

            var width = SurroundWidth + _widestWidth + CaretWidth;
            return new Vector2(width > size.Y ? width : size.Y, size.Y);
        }

        /// <summary>Returns <paramref name="number" /> as <see cref="FormatString" /> writes it, without the words around it.</summary>
        string Written(double number)
        {
            var written = number.ToString(_format, CultureInfo.InvariantCulture);
            if (_before.Length > 0)
                written = written.Replace(_before, "");

            return _after.Length > 0 ? written.Replace(_after, "") : written;
        }

        /// <summary>Returns how many characters <paramref name="first" /> and <paramref name="second" /> share from their start.</summary>
        static int SharedLead(string first, string second)
        {
            var count = 0;
            while (count < first.Length && count < second.Length && first[count] == second[count])
                count++;

            return count;
        }

        /// <summary>Returns how many characters <paramref name="first" /> and <paramref name="second" /> share from their end.</summary>
        static int SharedTrail(string first, string second)
        {
            var count = 0;
            while (count < first.Length && count < second.Length && first[first.Length - 1 - count] == second[second.Length - 1 - count])
                count++;

            return count;
        }

        /// <inheritdoc />
        /// <remarks>Reads the number from what is typed, and tells whoever listens where it has changed.</remarks>
        protected override void OnTextChanged()
        {
            base.OnTextChanged();

            double number;
            if (!double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                number = double.NaN;

            if (number.Equals(_value))
                return;

            _value = number;
            ValueChanged?.Invoke();
        }
    }
}
