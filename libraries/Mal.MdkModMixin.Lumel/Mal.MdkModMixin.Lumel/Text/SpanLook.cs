using System;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>The look of a span of one class: each part it sets, leaving the rest to the text's own look.</summary>
    public sealed class SpanLook
    {
        readonly Action _changed;
        Color? _color;

        /// <summary>
        ///     Creates a look that sets nothing and calls <paramref name="changed" /> whenever anything it sets
        ///     changes.
        /// </summary>
        public SpanLook(Action changed)
        {
            _changed = changed;
        }

        /// <summary>Gets or sets the color of the span's words, or null to leave it to the looks beneath.</summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _changed?.Invoke();
            }
        }

        /// <summary>Applies everything <paramref name="over" /> sets on top of what this look sets.</summary>
        public void Merge(SpanLook over)
        {
            _color = over._color ?? _color;
        }

        /// <summary>Returns true if this look sets exactly what <paramref name="other" /> sets.</summary>
        public bool SetsSameAs(SpanLook other)
        {
            return _color == other._color;
        }
    }
}
