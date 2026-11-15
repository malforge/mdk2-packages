using System;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>
    ///     The look of a callout of one class: each part it sets, leaving the rest to the looks beneath it.
    /// </summary>
    /// <remarks>Where a callout's lines sit is markdown-lite's own, by the classes <c>left</c>, <c>center</c> and <c>right</c>.</remarks>
    public sealed class CalloutLook
    {
        readonly Action _changed;
        Color? _background;
        Color? _barColor;
        Color? _color;
        bool? _isEmphasized;

        /// <summary>
        ///     Creates a look that sets nothing and calls <paramref name="changed" /> whenever anything it sets
        ///     changes.
        /// </summary>
        public CalloutLook(Action changed)
        {
            _changed = changed;
        }

        /// <summary>
        ///     Gets or sets the color of a bar down the callout's edge, drawn as a quote's bar is, or null to leave it
        ///     to the looks beneath.
        /// </summary>
        public Color? BarColor
        {
            get { return _barColor; }
            set
            {
                _barColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets what is painted behind the callout, or null to leave it to the looks beneath.
        /// </summary>
        public Color? Background
        {
            get { return _background; }
            set
            {
                _background = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the callout's plain text, or null to leave it to the looks beneath. Strong,
        ///     emphasized and linked text keep their own colors.
        /// </summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets whether the callout's text is drawn in the emphasized face, or null to leave it to the
        ///     looks beneath. Code keeps its own face.
        /// </summary>
        public bool? IsEmphasized
        {
            get { return _isEmphasized; }
            set
            {
                _isEmphasized = value;
                OnChanged();
            }
        }

        /// <summary>Applies everything <paramref name="over" /> sets on top of what this look sets.</summary>
        public void Merge(CalloutLook over)
        {
            _barColor = over._barColor ?? _barColor;
            _background = over._background ?? _background;
            _color = over._color ?? _color;
            _isEmphasized = over._isEmphasized ?? _isEmphasized;
        }

        /// <summary>Returns true if this look sets exactly what <paramref name="other" /> sets.</summary>
        public bool SetsSameAs(CalloutLook other)
        {
            return _barColor == other._barColor && _background == other._background && _color == other._color && _isEmphasized == other._isEmphasized;
        }

        void OnChanged()
        {
            _changed?.Invoke();
        }
    }
}
