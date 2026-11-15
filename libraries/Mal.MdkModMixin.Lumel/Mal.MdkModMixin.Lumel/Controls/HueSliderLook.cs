using Lumel.Styling;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a hue slider in one state, in addition to what any element has: the picture it is drawn with and
    ///     its marker.
    /// </summary>
    public class HueSliderLook : ElementLook
    {
        MyStringId? _hues;
        Color? _markerColor;
        Color? _markerOutlineColor;
        float? _markerOutlineThickness;
        float? _markerThickness;

        /// <summary>
        ///     Gets or sets the picture of every hue in order, from red at the top around to red at the bottom, or null
        ///     to use the underlying looks.
        /// </summary>
        public MyStringId? Hues
        {
            get { return _hues; }
            set
            {
                _hues = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the thickness of the marker's bar in UI units, or null to use the underlying looks.
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
        ///     Gets or sets the thickness of the outline around the marker in UI units, or null to use the underlying
        ///     looks.
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
            var look = over as HueSliderLook;
            if (look == null)
                return;

            _hues = look._hues ?? _hues;
            _markerThickness = look._markerThickness ?? _markerThickness;
            _markerOutlineThickness = look._markerOutlineThickness ?? _markerOutlineThickness;
            _markerColor = look._markerColor ?? _markerColor;
            _markerOutlineColor = look._markerOutlineColor ?? _markerOutlineColor;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _hues = null;
            _markerThickness = null;
            _markerOutlineThickness = null;
            _markerColor = null;
            _markerOutlineColor = null;
        }
    }
}
