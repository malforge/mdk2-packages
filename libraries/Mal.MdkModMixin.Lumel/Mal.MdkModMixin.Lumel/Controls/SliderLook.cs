using Lumel.Styling;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a slider in one state, in addition to what any element has: its track, the fill along it and its
    ///     thumb.
    /// </summary>
    public class SliderLook : ElementLook
    {
        Color? _fill;
        Color? _thumb;
        float? _thumbHeight;
        float? _thumbWidth;
        MyStringId? _track;
        float? _trackHeight;
        Color? _trackTint;

        /// <summary>
        ///     Gets or sets the picture the track is drawn with, the empty id for a plain fill, or null to use the
        ///     underlying looks.
        /// </summary>
        public MyStringId? Track
        {
            get { return _track; }
            set
            {
                _track = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the tint applied to the track, white for a picture that already has its own colors, or null
        ///     to use the underlying looks.
        /// </summary>
        public Color? TrackTint
        {
            get { return _trackTint; }
            set
            {
                _trackTint = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the height of the track, centered vertically in the slider, 0 for the slider's full height,
        ///     or null to use the underlying looks.
        /// </summary>
        public float? TrackHeight
        {
            get { return _trackHeight; }
            set
            {
                _trackHeight = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color laid over the track from its left end to the middle of the thumb, transparent for
        ///     none, or null to use the underlying looks.
        /// </summary>
        public Color? Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the width of the thumb, or null to use the underlying looks.</summary>
        public float? ThumbWidth
        {
            get { return _thumbWidth; }
            set
            {
                _thumbWidth = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the height of the thumb, centered vertically in the slider, 0 for the slider's full height, or
        ///     null to use the underlying looks.
        /// </summary>
        public float? ThumbHeight
        {
            get { return _thumbHeight; }
            set
            {
                _thumbHeight = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the fill color of the thumb, or null to use the underlying looks.</summary>
        public Color? Thumb
        {
            get { return _thumb; }
            set
            {
                _thumb = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as SliderLook;
            if (look == null)
                return;

            _track = look._track ?? _track;
            _trackTint = look._trackTint ?? _trackTint;
            _trackHeight = look._trackHeight ?? _trackHeight;
            _fill = look._fill ?? _fill;
            _thumbWidth = look._thumbWidth ?? _thumbWidth;
            _thumbHeight = look._thumbHeight ?? _thumbHeight;
            _thumb = look._thumb ?? _thumb;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _track = null;
            _trackTint = null;
            _trackHeight = null;
            _fill = null;
            _thumbWidth = null;
            _thumbHeight = null;
            _thumb = null;
        }
    }
}
