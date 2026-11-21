using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>The look of a token box in one state, adding the room around its tokens to what any editable text has.</summary>
    public class TokenBoxLook : EditableTextLook
    {
        float? _tokenSpacing;

        /// <summary>The room on each side of a token, between it and the words beside it, or null to inherit it.</summary>
        public float? TokenSpacing
        {
            get { return _tokenSpacing; }
            set
            {
                _tokenSpacing = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as TokenBoxLook;
            if (look == null)
                return;

            _tokenSpacing = look._tokenSpacing ?? _tokenSpacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _tokenSpacing = null;
        }
    }
}
