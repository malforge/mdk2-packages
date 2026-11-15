using Lumel.Styling;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>
    ///     The look of an image in one state: the tint its picture is multiplied by, on top of what any element look
    ///     sets.
    /// </summary>
    public class ImageLook : ElementLook
    {
        Color? _tint;

        /// <summary>
        ///     Gets or sets the color the image is multiplied by, where white draws it unchanged, or null to inherit it
        ///     from the looks beneath.
        /// </summary>
        public Color? Tint
        {
            get { return _tint; }
            set
            {
                _tint = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ImageLook;
            if (look == null)
                return;

            _tint = look._tint ?? _tint;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _tint = null;
        }
    }
}
