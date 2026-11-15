using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a color panel in one state, in addition to what any stack has: how the color made shows while there
    ///     is none.
    /// </summary>
    public class ColorPanelLook : StackLook
    {
        Color? _emptyFill;

        /// <summary>
        ///     Gets or sets the fill of the color made while there is no color, or null to use the underlying looks; transparent
        ///     shows what is behind it.
        /// </summary>
        public Color? EmptyFill
        {
            get { return _emptyFill; }
            set
            {
                _emptyFill = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ColorPanelLook;
            if (look == null)
                return;

            _emptyFill = look._emptyFill ?? _emptyFill;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _emptyFill = null;
        }
    }
}
