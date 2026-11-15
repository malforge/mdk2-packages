using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a drag handle in one state, in addition to what any element has: the color, size and spacing of
    ///     its dots.
    /// </summary>
    public class DragHandleLook : ElementLook
    {
        Color? _color;
        float? _dotSize;
        float? _dotSpacing;

        /// <summary>Gets or sets the color of the dots, or null to use the underlying looks.</summary>
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
        ///     Gets or sets the side length of each dot in UI units, or null to use the underlying looks.
        /// </summary>
        public float? DotSize
        {
            get { return _dotSize; }
            set
            {
                _dotSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space between adjacent dots in UI units, or null to use the underlying looks.
        /// </summary>
        public float? DotSpacing
        {
            get { return _dotSpacing; }
            set
            {
                _dotSpacing = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as DragHandleLook;
            if (look == null)
                return;

            _color = look._color ?? _color;
            _dotSize = look._dotSize ?? _dotSize;
            _dotSpacing = look._dotSpacing ?? _dotSpacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _color = null;
            _dotSize = null;
            _dotSpacing = null;
        }
    }
}
