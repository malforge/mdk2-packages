using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a <see cref="Swatch" /> in one state, adding how strongly it shows its color to what any element
    ///     sets.
    /// </summary>
    public class SwatchLook : ElementLook
    {
        float? _opacity;

        /// <summary>
        ///     How strongly the color is shown, from 0 (not at all) to 1 (fully), or null to use the looks beneath.
        /// </summary>
        public float? Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as SwatchLook;
            if (look == null)
                return;

            _opacity = look._opacity ?? _opacity;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _opacity = null;
        }
    }
}
