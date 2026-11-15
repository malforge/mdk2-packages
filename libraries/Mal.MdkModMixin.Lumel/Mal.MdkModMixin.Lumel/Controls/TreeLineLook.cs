using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a <see cref="TreeLine" /> in one state, adding its width, turn and color to what any element
    ///     sets.
    /// </summary>
    public class TreeLineLook : ElementLook
    {
        Color? _color;
        float? _thickness;
        float? _turn;

        /// <summary>The width of the line, in UI units, or null to use the looks beneath.</summary>
        public float? Thickness
        {
            get { return _thickness; }
            set
            {
                _thickness = value;
                OnChanged();
            }
        }

        /// <summary>How far the line turns inward at its end, in UI units, or null to use the looks beneath.</summary>
        public float? Turn
        {
            get { return _turn; }
            set
            {
                _turn = value;
                OnChanged();
            }
        }

        /// <summary>The color of the line, or null to use the looks beneath.</summary>
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
            var look = over as TreeLineLook;
            if (look == null)
                return;

            _thickness = look._thickness ?? _thickness;
            _turn = look._turn ?? _turn;
            _color = look._color ?? _color;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _thickness = null;
            _turn = null;
            _color = null;
        }
    }
}
