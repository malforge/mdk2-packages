using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a <see cref="TabView" /> in one state, adding the space between its headers and its page to what
    ///     any element sets.
    /// </summary>
    public class TabViewLook : ElementLook
    {
        float? _spacing;

        /// <summary>
        ///     The blank space between the strip of headers and the page, or null to use the looks beneath.
        /// </summary>
        public float? Spacing
        {
            get { return _spacing; }
            set
            {
                _spacing = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as TabViewLook;
            if (look == null)
                return;

            _spacing = look._spacing ?? _spacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _spacing = null;
        }
    }
}
