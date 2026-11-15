using Lumel.Layout;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a color palette in one state, in addition to what any flow has: how large each swatch is, and its
    ///     style.
    /// </summary>
    public class ColorPaletteLook : FlowLook
    {
        float? _swatchSize;
        IStyle<Swatch> _swatchStyle;

        /// <summary>Gets or sets the width and height of each swatch, or null to use the underlying looks.</summary>
        public float? SwatchSize
        {
            get { return _swatchSize; }
            set
            {
                _swatchSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the style of each swatch, or null to use the underlying looks, which leave it to the host theme's
        ///     swatch style.
        /// </summary>
        public IStyle<Swatch> SwatchStyle
        {
            get { return _swatchStyle; }
            set
            {
                _swatchStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ColorPaletteLook;
            if (look == null)
                return;

            _swatchSize = look._swatchSize ?? _swatchSize;
            _swatchStyle = look._swatchStyle ?? _swatchStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _swatchSize = null;
            _swatchStyle = null;
        }
    }
}
