using Lumel.Styling;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a color spectrum in one state, in addition to what any element has: the pictures it is drawn
    ///     with and its marker.
    /// </summary>
    public class ColorSpectrumLook : ElementLook
    {
        MyStringId? _dark;
        Color? _markerColor;
        Color? _markerOutlineColor;
        float? _markerOutlineThickness;
        float? _markerSize;
        float? _markerThickness;
        MyStringId? _pale;

        /// <summary>
        ///     Gets or sets the picture of a white gradient that is opaque at the left edge and fades to transparent at
        ///     the right, or null to use the underlying looks.
        /// </summary>
        public MyStringId? Pale
        {
            get { return _pale; }
            set
            {
                _pale = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the picture of a black gradient that is transparent at the top edge and opaque at the
        ///     bottom, or null to use the underlying looks.
        /// </summary>
        public MyStringId? Dark
        {
            get { return _dark; }
            set
            {
                _dark = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the width of the marker's square in UI units, or null to use the underlying looks.
        /// </summary>
        public float? MarkerSize
        {
            get { return _markerSize; }
            set
            {
                _markerSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the line thickness of the marker's square in UI units, or null to use the underlying looks.
        /// </summary>
        public float? MarkerThickness
        {
            get { return _markerThickness; }
            set
            {
                _markerThickness = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the line thickness of the outline around the marker in UI units, or null to use the
        ///     underlying looks.
        /// </summary>
        public float? MarkerOutlineThickness
        {
            get { return _markerOutlineThickness; }
            set
            {
                _markerOutlineThickness = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the marker, or null to use the underlying looks.</summary>
        public Color? MarkerColor
        {
            get { return _markerColor; }
            set
            {
                _markerColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the outline around the marker, or null to use the underlying looks.
        /// </summary>
        public Color? MarkerOutlineColor
        {
            get { return _markerOutlineColor; }
            set
            {
                _markerOutlineColor = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ColorSpectrumLook;
            if (look == null)
                return;

            _pale = look._pale ?? _pale;
            _dark = look._dark ?? _dark;
            _markerSize = look._markerSize ?? _markerSize;
            _markerThickness = look._markerThickness ?? _markerThickness;
            _markerOutlineThickness = look._markerOutlineThickness ?? _markerOutlineThickness;
            _markerColor = look._markerColor ?? _markerColor;
            _markerOutlineColor = look._markerOutlineColor ?? _markerOutlineColor;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _pale = null;
            _dark = null;
            _markerSize = null;
            _markerThickness = null;
            _markerOutlineThickness = null;
            _markerColor = null;
            _markerOutlineColor = null;
        }
    }
}
