using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a <see cref="Switch" /> in one state: its words, thumb and the fill of its inner area.
    /// </summary>
    /// <remarks><see cref="TextLook.Color" /> is the words.</remarks>
    public class SwitchLook : TextLook
    {
        Color? _thumb;
        Color? _track;
        Thickness? _wordPadding;

        /// <summary>The fill of the area inside the switch, or null to use the looks beneath.</summary>
        public Color? Track
        {
            get { return _track; }
            set
            {
                _track = value;
                OnChanged();
            }
        }

        /// <summary>The color of the thumb, or null to use the looks beneath.</summary>
        public Color? Thumb
        {
            get { return _thumb; }
            set
            {
                _thumb = value;
                OnChanged();
            }
        }

        /// <summary>The space kept clear around the word, or null to use the looks beneath.</summary>
        public Thickness? WordPadding
        {
            get { return _wordPadding; }
            set
            {
                _wordPadding = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as SwitchLook;
            if (look == null)
                return;

            _track = look._track ?? _track;
            _thumb = look._thumb ?? _thumb;
            _wordPadding = look._wordPadding ?? _wordPadding;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _track = null;
            _thumb = null;
            _wordPadding = null;
        }
    }
}
