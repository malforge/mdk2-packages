using VRageMath;
using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>The look of a window in one state, adding to what any element has.</summary>
    public class WindowLook : ElementLook
    {
        Color? _backdrop;
        Shadow _shadow;

        /// <summary>
        ///     The color a modal window paints over everything beneath it, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public Color? Backdrop
        {
            get { return _backdrop; }
            set
            {
                _backdrop = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The shadow the window casts on what lies beneath it, or null to inherit it from the looks beneath.
        /// </summary>
        public Shadow Shadow
        {
            get { return _shadow; }
            set
            {
                _shadow = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as WindowLook;
            if (look == null)
                return;

            _backdrop = look._backdrop ?? _backdrop;
            _shadow = look._shadow ?? _shadow;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _backdrop = null;
            _shadow = null;
        }
    }
}
