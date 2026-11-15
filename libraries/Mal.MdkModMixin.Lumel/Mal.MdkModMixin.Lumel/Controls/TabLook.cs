using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a <see cref="Tab" /> in one state, adding where its text sits across the header to what any text
    ///     look sets.
    /// </summary>
    public class TabLook : TextLook
    {
        Align? _contentAlign;

        /// <summary>
        ///     How the text and badge are aligned across the header: left, right or centered, or null to use the looks
        ///     beneath.
        /// </summary>
        public Align? ContentAlign
        {
            get { return _contentAlign; }
            set
            {
                _contentAlign = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as TabLook;
            if (look == null)
                return;

            _contentAlign = look._contentAlign ?? _contentAlign;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _contentAlign = null;
        }
    }
}
