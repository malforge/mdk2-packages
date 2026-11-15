using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Layout
{
    /// <summary>
    ///     The rows of a <see cref="Form" />, in order, each a label and a field or one element across the whole width.
    /// </summary>
    /// <remarks>
    /// Made by the form that holds them, and written as a collection initializer:
    /// <c>Rows = { { label, field }, { label, field, FormLabelPlacement.Above }, { swatches } }</c>.
    /// </remarks>
    public abstract class FormRows : IEnumerable
    {
        readonly List<FormRow> _rows = new List<FormRow>();
        readonly Action<Element> _joined;
        readonly Action<Element> _released;

        /// <summary>
        ///     Creates a rows collection that calls <paramref name="joined" /> as each element joins it and
        ///     <paramref name="released" /> as each leaves.
        /// </summary>
        protected FormRows(Action<Element> joined, Action<Element> released)
        {
            _joined = joined;
            _released = released;
        }

        /// <summary>Gets the number of rows.</summary>
        public int Count => _rows.Count;

        /// <summary>Gets the row at <paramref name="index" />.</summary>
        public FormRow this[int index] => _rows[index];

        /// <summary>
        ///     Adds a row of <paramref name="label" /> and <paramref name="field" />, side by side where they fit.
        /// </summary>
        public void Add(Element label, Element field)
        {
            Add(label, field, FormLabelPlacement.Auto);
        }

        /// <summary>
        ///     Adds a row of <paramref name="label" /> and <paramref name="field" />, with the label placed as
        ///     <paramref name="labelPlacement" /> says.
        /// </summary>
        public void Add(Element label, Element field, FormLabelPlacement labelPlacement)
        {
            if (label == null || field == null)
                throw new ArgumentNullException(label == null ? nameof(label) : nameof(field));

            _rows.Add(new FormRow(label, field, labelPlacement));
            _joined(label);
            _joined(field);
        }

        /// <summary>Adds a row of <paramref name="whole" /> alone, across the whole width.</summary>
        public void Add(Element whole)
        {
            if (whole == null)
                throw new ArgumentNullException(nameof(whole));

            _rows.Add(new FormRow(null, whole, FormLabelPlacement.Above));
            _joined(whole);
        }

        /// <summary>Removes every row.</summary>
        public void Clear()
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                var row = _rows[i];
                _rows.RemoveAt(i);
                _released(row.Field);
                if (row.Label != null)
                    _released(row.Label);
            }
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator()
        {
            return _rows.GetEnumerator();
        }
    }
}
