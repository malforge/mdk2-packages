using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a <see cref="ProgressBar" /> in one state, adding the color of its filled part to what any
    ///     element sets; the track is the element's background.
    /// </summary>
    public class ProgressBarLook : ElementLook
    {
        Color? _fill;

        /// <summary>The color of the filled part, or null to use the looks beneath.</summary>
        public Color? Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ProgressBarLook;
            if (look == null)
                return;

            _fill = look._fill ?? _fill;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _fill = null;
        }
    }
}
