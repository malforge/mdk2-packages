using Lumel.Layout;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a reorder list in one state, in addition to what any stack has: how far a row in a group is
    ///     indented.
    /// </summary>
    public class ReorderListLook : StackLook
    {
        float? _indentWidth;

        /// <summary>Gets or sets how far a row in a group is indented, or null to use the underlying looks.</summary>
        public float? IndentWidth
        {
            get { return _indentWidth; }
            set
            {
                _indentWidth = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ReorderListLook;
            if (look == null)
                return;

            _indentWidth = look._indentWidth ?? _indentWidth;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _indentWidth = null;
        }
    }
}
