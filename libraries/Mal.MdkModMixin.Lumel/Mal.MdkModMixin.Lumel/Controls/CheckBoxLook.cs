using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The look of a check box in one state: its text, its box and the mark in the box.</summary>
    /// <remarks><see cref="TextLook.Color" /> is the words.</remarks>
    public class CheckBoxLook : TextLook
    {
        Color? _box;
        Color? _boxFill;
        float? _boxSize;
        float? _boxThickness;
        Color? _mark;
        Thickness? _wordPadding;

        /// <summary>Gets or sets the color of the box's border, or null to use the underlying looks.</summary>
        public Color? Box
        {
            get { return _box; }
            set
            {
                _box = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the thickness of the box's border, or null to use the underlying looks.</summary>
        public float? BoxThickness
        {
            get { return _boxThickness; }
            set
            {
                _boxThickness = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the fill color of the inside of the box, or null to use the underlying looks.
        /// </summary>
        public Color? BoxFill
        {
            get { return _boxFill; }
            set
            {
                _boxFill = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the mark that shows the box as checked or partly checked, or null to use the
        ///     underlying looks.
        /// </summary>
        public Color? Mark
        {
            get { return _mark; }
            set
            {
                _mark = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the width and height of the box, or null to use the underlying looks, which make it as tall
        ///     as a line of text.
        /// </summary>
        public float? BoxSize
        {
            get { return _boxSize; }
            set
            {
                _boxSize = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the space around the text, or null to use the underlying looks.</summary>
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
            var look = over as CheckBoxLook;
            if (look == null)
                return;

            _box = look._box ?? _box;
            _boxThickness = look._boxThickness ?? _boxThickness;
            _boxFill = look._boxFill ?? _boxFill;
            _mark = look._mark ?? _mark;
            _boxSize = look._boxSize ?? _boxSize;
            _wordPadding = look._wordPadding ?? _wordPadding;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _box = null;
            _boxThickness = null;
            _boxFill = null;
            _mark = null;
            _boxSize = null;
            _wordPadding = null;
        }
    }
}
