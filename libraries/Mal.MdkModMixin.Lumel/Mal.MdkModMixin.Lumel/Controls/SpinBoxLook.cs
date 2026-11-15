using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a spin box in one state, in addition to what any stack has: the styles of its number box and of
    ///     the buttons either side of it.
    /// </summary>
    public class SpinBoxLook : StackLook
    {
        IStyle<NumberBox> _boxStyle;
        IStyle<Button> _stepStyle;

        /// <summary>
        ///     Gets or sets the style of the number box, or null to use the underlying looks, which leave it to the host
        ///     theme's number box style.
        /// </summary>
        public IStyle<NumberBox> BoxStyle
        {
            get { return _boxStyle; }
            set
            {
                _boxStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the style of the minus and plus buttons, or null to use the underlying looks, which leave them
        ///     to the host theme's button style.
        /// </summary>
        public IStyle<Button> StepStyle
        {
            get { return _stepStyle; }
            set
            {
                _stepStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as SpinBoxLook;
            if (look == null)
                return;

            _boxStyle = look._boxStyle ?? _boxStyle;
            _stepStyle = look._stepStyle ?? _stepStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _boxStyle = null;
            _stepStyle = null;
        }
    }
}
