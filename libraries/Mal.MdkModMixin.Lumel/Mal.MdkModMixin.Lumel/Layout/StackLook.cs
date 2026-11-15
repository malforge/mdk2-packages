using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>
    ///     The look of a stack in one state: the space between its children, on top of what any element look sets.
    /// </summary>
    public class StackLook : ElementLook
    {
        float? _spacing;

        /// <summary>
        ///     Gets or sets the blank space between one child and the next, or null to inherit it from the looks
        ///     beneath.
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
            var look = over as StackLook;
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
