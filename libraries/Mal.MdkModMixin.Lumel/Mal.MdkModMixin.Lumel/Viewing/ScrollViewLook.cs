using VRageMath;
using Lumel.Styling;

namespace Lumel.Viewing
{
    /// <summary>The look of a scroll view in one state, adding to what any element has.</summary>
    public class ScrollViewLook : ElementLook
    {
        /// <summary>The scroll bar width used when a style leaves it to the neutral values.</summary>
        public const float NeutralScrollBarWidth = 12f;

        /// <summary>The minimum thumb length used when a style leaves it to the neutral values.</summary>
        public const float NeutralMinThumbLength = 32f;

        /// <summary>
        ///     The thumb color used when a style leaves it to the neutral values: a gray-blue that is visible on both
        ///     dark and light backgrounds.
        /// </summary>
        public static readonly Color NeutralThumb = new Color(95, 114, 120);

        float? _scrollBarWidth;
        float? _scrollBarGap;
        float? _minThumbLength;
        Color? _thumb;

        /// <summary>The width of the scroll bar, or null to inherit it from the looks beneath.</summary>
        public float? ScrollBarWidth
        {
            get { return _scrollBarWidth; }
            set
            {
                _scrollBarWidth = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The space between the content and the scroll bar, or null to inherit it from the looks beneath.
        /// </summary>
        public float? ScrollBarGap
        {
            get { return _scrollBarGap; }
            set
            {
                _scrollBarGap = value;
                OnChanged();
            }
        }

        /// <summary>The color of the thumb, or null to inherit it from the looks beneath.</summary>
        public Color? Thumb
        {
            get { return _thumb; }
            set
            {
                _thumb = value;
                OnChanged();
            }
        }

        /// <summary>The minimum length of the thumb, or null to inherit it from the looks beneath.</summary>
        public float? MinThumbLength
        {
            get { return _minThumbLength; }
            set
            {
                _minThumbLength = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ScrollViewLook;
            if (look == null)
                return;

            _scrollBarWidth = look._scrollBarWidth ?? _scrollBarWidth;
            _scrollBarGap = look._scrollBarGap ?? _scrollBarGap;
            _thumb = look._thumb ?? _thumb;
            _minThumbLength = look._minThumbLength ?? _minThumbLength;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _scrollBarWidth = null;
            _scrollBarGap = null;
            _thumb = null;
            _minThumbLength = null;
        }
    }
}
