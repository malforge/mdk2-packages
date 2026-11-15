using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a separator in one state, in addition to what any element has: the thickness and color of its
    ///     line.
    /// </summary>
    public class SeparatorLook : ElementLook
    {
        Color? _color;
        float? _thickness;

        /// <summary>Gets or sets the thickness of the line in UI units, or null to use the underlying looks.</summary>
        public float? Thickness
        {
            get { return _thickness; }
            set
            {
                _thickness = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the line, or null to use the underlying looks.</summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as SeparatorLook;
            if (look == null)
                return;

            _thickness = look._thickness ?? _thickness;
            _color = look._color ?? _color;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _thickness = null;
            _color = null;
        }
    }
}
