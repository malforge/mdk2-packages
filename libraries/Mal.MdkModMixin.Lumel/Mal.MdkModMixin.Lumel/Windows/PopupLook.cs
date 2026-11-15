using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>The look of a popup in one state, adding the distance from its anchor to what any window has.</summary>
    public class PopupLook : WindowLook
    {
        float? _gap;

        /// <summary>
        ///     The distance between the popup and its anchor in UI units, or null to inherit it from the looks beneath.
        /// </summary>
        public float? Gap
        {
            get { return _gap; }
            set
            {
                _gap = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as PopupLook;
            if (look == null)
                return;

            _gap = look._gap ?? _gap;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _gap = null;
        }
    }
}
