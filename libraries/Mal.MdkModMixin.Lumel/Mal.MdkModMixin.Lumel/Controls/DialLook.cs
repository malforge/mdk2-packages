using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a dial in one state, in addition to what any element has: the colors of its marks and its
    ///     needle.
    /// </summary>
    public class DialLook : ElementLook
    {
        Color? _markColor;
        Color? _needleColor;
        Color? _quarterColor;

        /// <summary>
        ///     Gets or sets the color of the marks between the quarters, or null to use the underlying looks.
        /// </summary>
        public Color? MarkColor
        {
            get { return _markColor; }
            set
            {
                _markColor = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the marks on the quarters, or null to use the underlying looks.</summary>
        public Color? QuarterColor
        {
            get { return _quarterColor; }
            set
            {
                _quarterColor = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the needle and the hub, or null to use the underlying looks.</summary>
        public Color? NeedleColor
        {
            get { return _needleColor; }
            set
            {
                _needleColor = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as DialLook;
            if (look == null)
                return;

            _markColor = look._markColor ?? _markColor;
            _quarterColor = look._quarterColor ?? _quarterColor;
            _needleColor = look._needleColor ?? _needleColor;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _markColor = null;
            _quarterColor = null;
            _needleColor = null;
        }
    }
}
