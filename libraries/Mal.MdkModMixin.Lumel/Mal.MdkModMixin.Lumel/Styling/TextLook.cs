using VRageMath;

namespace Lumel.Styling
{
    /// <summary>
    ///     What an element that draws text looks like in one state, in addition to what any element does.
    /// </summary>
    public class TextLook : ElementLook
    {
        /// <summary>The line height used when no look sets one.</summary>
        public const float NeutralFontSize = 24f;

        Color? _color;
        string _font;
        float? _fontSize;

        /// <summary>Gets or sets the face the text is drawn in, or null to leave it to the looks beneath.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                _font = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the height of a line, in UI units, or null to leave it to the looks beneath.</summary>
        public float? FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the text, or null to leave it to the looks beneath.</summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var text = over as TextLook;
            if (text == null)
                return;

            _font = text._font ?? _font;
            _fontSize = text._fontSize ?? _fontSize;
            _color = text._color ?? _color;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _font = null;
            _fontSize = null;
            _color = null;
        }
    }
}
