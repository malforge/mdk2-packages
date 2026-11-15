using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>
    ///     A container of rows, each a label and the field it names, placed side by side where both fit and with the
    ///     field under its label where they do not.
    /// </summary>
    /// <remarks>
    /// Side by side, a field keeps to the right-hand edge; under its label it starts at the left. A child's own alignment wins
    /// over either, and a fixed width from its placement is kept as long as there is room for it. A form asks for the width
    /// its rows need, and a field that stretches fills whatever width its holder gives the form.
    /// </remarks>
    public partial class Form : Container
    {
        /// <summary>
        ///     The default space between a label and its field when side by side, and between one row and the next,
        ///     used when no look sets it.
        /// </summary>
        public const float NeutralSpacing = 8f;

        /// <summary>The default space between a label and the field under it, used when no look sets it.</summary>
        public const float NeutralLabelSpacing = 4f;

        readonly Collection _children;
        readonly List<Vector2> _labels = new List<Vector2>();
        readonly List<Vector2> _fields = new List<Vector2>();
        readonly List<float> _heights = new List<float>();
        readonly List<bool> _stacked = new List<bool>();

        float _columnSpacing = NeutralSpacing;
        float _rowSpacing = NeutralSpacing;
        float _labelSpacing = NeutralLabelSpacing;

        /// <summary>Creates a form with no rows yet.</summary>
        public Form()
        {
            _children = CreateChildren();
            Rows = new OwnedRows(Join, Release);
        }

        /// <summary>Gets or sets the style of this form, or null to use the theme's default style for forms.</summary>
        public new IStyle<Form> Style
        {
            get { return (IStyle<Form>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this form's own look, applied in every state on top of its style's.</summary>
        public new FormLook Base => (FormLook)base.Base;

        /// <summary>Gets this form's own look for when it is hovered, applied on top of its style's.</summary>
        public new FormLook PointerOver => (FormLook)base.PointerOver;

        /// <summary>Gets this form's own look for when it is disabled, applied on top of its style's.</summary>
        public new FormLook Disabled => (FormLook)base.Disabled;

        /// <summary>
        ///     Gets this form's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of its style's.
        /// </summary>
        public new FormLook this[State state] => (FormLook)base[state];

        /// <summary>Gets the rows, top to bottom.</summary>
        public FormRows Rows { get; }

        /// <summary>Gets the blank space between a label and its field when they sit side by side.</summary>
        /// <remarks>Set by this form's looks.</remarks>
        public float ColumnSpacing => _columnSpacing;

        /// <summary>Gets the blank space between one row and the next.</summary>
        /// <remarks>Set by this form's looks.</remarks>
        public float RowSpacing => _rowSpacing;

        /// <summary>Gets the blank space between a label and the field under it.</summary>
        /// <remarks>Set by this form's looks.</remarks>
        public float LabelSpacing => _labelSpacing;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Form>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new FormLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var form = (FormLook)look;
            var column = form.ColumnSpacing ?? NeutralSpacing;
            var row = form.RowSpacing ?? NeutralSpacing;
            var label = form.LabelSpacing ?? NeutralLabelSpacing;
            if (column == _columnSpacing && row == _rowSpacing && label == _labelSpacing)
                return;

            _columnSpacing = column;
            _rowSpacing = row;
            _labelSpacing = label;
            InvalidateMeasure();
        }

        /// <summary>In a side-by-side row, the field keeps to the right-hand edge.</summary>
        protected override Align Aligns => Align.Right;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var room = available.X;
            var bounded = room < Unbounded;
            var wide = 0f;
            var high = 0f;
            var placed = false;

            _labels.Clear();
            _fields.Clear();
            _heights.Clear();
            _stacked.Clear();
            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                var label = Vector2.Zero;
                var field = Vector2.Zero;
                var tall = 0f;

                if (!row.Field.IsVisible)
                {
                    Keep(label, field, tall, false);
                    continue;
                }

                if (row.Label != null && row.Label.IsVisible)
                {
                    row.Label.Measure(g, new Vector2(room, Unbounded));
                    label = row.Label.DesiredSize;
                }

                var fixedWidth = FixedWidth(row.Field);
                row.Field.Measure(g, new Vector2(float.IsNaN(fixedWidth) ? Unbounded : fixedWidth, Unbounded));
                var natural = Width(row.Field, row.Field.DesiredSize.X);

                var alone = row.Label == null;
                var beside = !alone && row.LabelPlacement == FormLabelPlacement.Auto
                             && (!bounded || label.X + ColumnSpacing + natural <= room);
                var stacked = !alone && !beside;

                if (beside)
                {
                    // Measured again in the width it will have where it stretches, so it is laid out for that width.
                    if (Stretches(row.Field, Align.Right) && bounded)
                        row.Field.Measure(g, new Vector2(room - label.X - ColumnSpacing, Unbounded));

                    field = new Vector2(natural, row.Field.DesiredSize.Y);
                    tall = Math.Max(label.Y, field.Y);
                    wide = Math.Max(wide, label.X + ColumnSpacing + natural);
                }
                else
                {
                    row.Field.Measure(g, new Vector2(bounded ? room : Unbounded, Unbounded));
                    field = new Vector2(Math.Min(Width(row.Field, row.Field.DesiredSize.X), room), row.Field.DesiredSize.Y);
                    tall = alone ? field.Y : label.Y + LabelSpacing + field.Y;
                    wide = Math.Max(wide, Math.Max(label.X, field.X));
                }

                Keep(label, field, tall, stacked);
                if (placed)
                    high += RowSpacing;

                placed = true;
                high += tall;
            }

            return new Vector2(wide, high);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var y = bounds.Y;
            var placed = false;
            for (var i = 0; i < Rows.Count && i < _heights.Count; i++)
            {
                var row = Rows[i];
                var field = _fields[i];
                var height = _heights[i];
                if (!row.Field.IsVisible)
                    continue;

                if (placed)
                    y += RowSpacing;

                placed = true;

                if (row.Label == null)
                {
                    Put(row.Field, Align.FillWidth, bounds.X, bounds.Width, y, field);
                }
                else if (!_stacked[i])
                {
                    var label = _labels[i];
                    row.Label.Arrange(new RectangleF(bounds.X, y + (height - label.Y) * 0.5f, label.X, label.Y));

                    var from = bounds.X + label.X + ColumnSpacing;
                    Put(row.Field, Align.Right, from, bounds.X + bounds.Width - from, y + (height - field.Y) * 0.5f, field);
                }
                else
                {
                    var label = _labels[i];
                    row.Label.Arrange(new RectangleF(bounds.X, y, Math.Min(label.X, bounds.Width), label.Y));
                    Put(row.Field, row.LabelPlacement == FormLabelPlacement.Above ? Align.FillWidth : Align.Left, bounds.X, bounds.Width,
                        y + label.Y + LabelSpacing, field);
                }

                y += height;
            }
        }

        /// <summary>
        ///     Draws each row whose field is shown, with its label; a row whose field is hidden is hidden entirely.
        /// </summary>
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                if (!row.Field.IsVisible)
                    continue;

                row.Label?.Draw(g);
                row.Field.Draw(g);
            }
        }

        /// <summary>
        ///     Returns the element under <paramref name="point" /> among the rows whose field is shown, checking the
        ///     last row first.
        /// </summary>
        protected override Element HitTestContent(Vector2 point)
        {
            for (var i = Rows.Count - 1; i >= 0; i--)
            {
                var row = Rows[i];
                if (!row.Field.IsVisible)
                    continue;

                var found = row.Field.HitTest(point) ?? row.Label?.HitTest(point);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>Rejects a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <summary>
        ///     Places <paramref name="field" /> across <paramref name="room" /> starting at <paramref name="from" /> as
        ///     its own alignment says, otherwise as <paramref name="fallback" /> says.
        /// </summary>
        static void Put(Element field, Align fallback, float from, float room, float y, Vector2 size)
        {
            var edges = field.Align & Align.FillWidth;
            if (edges == Align.Default)
                edges = (field.Align & Align.Center) != 0 ? Align.Center : fallback;

            var stretched = edges == Align.FillWidth && float.IsNaN(FixedWidth(field));
            var width = stretched ? room : Math.Min(size.X, room);
            var x = edges == Align.Right ? from + room - width
                : edges == Align.Center ? from + (room - width) * 0.5f
                : from;

            field.Arrange(new RectangleF(x, y, width, size.Y));
        }

        /// <summary>
        ///     Returns true if <paramref name="field" /> fills all the room it is given, where its own alignment defers
        ///     to <paramref name="fallback" />.
        /// </summary>
        static bool Stretches(Element field, Align fallback)
        {
            var edges = field.Align & Align.FillWidth;
            return (edges == Align.Default ? fallback : edges) == Align.FillWidth && float.IsNaN(FixedWidth(field));
        }

        /// <summary>Returns the width a child's placement fixes, or NaN if it fixes none.</summary>
        static float FixedWidth(Element child)
        {
            var sized = child.Placement as ISized;
            return sized?.Width ?? float.NaN;
        }

        /// <summary>
        ///     Returns a child's width: the one its placement fixes, otherwise <paramref name="desired" />.
        /// </summary>
        static float Width(Element child, float desired)
        {
            var fixedWidth = FixedWidth(child);
            return float.IsNaN(fixedWidth) ? desired : fixedWidth;
        }

        /// <summary>
        ///     Stores what one row measured (the sizes of its <paramref name="label" /> and <paramref name="field" />,
        ///     its total <paramref name="height" /> and whether the field went under the label,
        ///     <paramref name="stacked" />) for use when the row is placed.
        /// </summary>
        void Keep(Vector2 label, Vector2 field, float height, bool stacked)
        {
            _labels.Add(label);
            _fields.Add(field);
            _heights.Add(height);
            _stacked.Add(stacked);
        }

        void Join(Element child)
        {
            _children.Add(child);
        }

        void Release(Element child)
        {
            _children.Remove(child);
        }
    }
}
