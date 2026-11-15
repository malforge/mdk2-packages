using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;

namespace Lumel.Controls
{
    /// <summary>
    ///     A number box between a minus button and a plus button, which step its number down and up by its
    ///     <see cref="NumberBox.SmallChange" />, within its <see cref="NumberBox.Minimum" /> and
    ///     <see cref="NumberBox.Maximum" />.
    /// </summary>
    /// <remarks>
    ///     <see cref="Box" /> is the number box, read and set as any other: its value, its step and its bounds. A button
    ///     is disabled while the number stands at the bound it steps toward, as its value last changed.
    /// </remarks>
    public class SpinBox : HStack
    {
        readonly NumberBox _box;
        readonly Button _less;
        readonly Button _more;
        readonly RelayCommand _down;
        readonly RelayCommand _up;

        /// <summary>
        ///     Creates a spin box around an empty number box, using the look from the host's theme for a spin box until it
        ///     is given a style.
        /// </summary>
        public SpinBox()
        {
            _box = new NumberBox();
            _down = new RelayCommand(() => _box.StepBy(-1));
            _up = new RelayCommand(() => _box.StepBy(1));
            Children.Add(new Button { Content = new StepSign(false), Align = Align.FillHeight, Command = _down, FocusTarget = _box }.As(out _less));
            Children.Add(_box);
            Children.Add(new Button { Content = new StepSign(true), Align = Align.FillHeight, Command = _up, FocusTarget = _box }.As(out _more));

            _box.ValueChanged += Bound;
            Bound();
        }

        /// <summary>Gets the number box between the buttons.</summary>
        public NumberBox Box => _box;

        /// <summary>Gets or sets the tip on the minus button, or null for none.</summary>
        public Tip LessTip
        {
            get { return _less.Tip; }
            set { _less.Tip = value; }
        }

        /// <summary>Gets or sets the tip on the plus button, or null for none.</summary>
        public Tip MoreTip
        {
            get { return _more.Tip; }
            set { _more.Tip = value; }
        }

        /// <summary>
        ///     Gets or sets the style of this spin box, or null to use the default from the host's theme for a spin box.
        /// </summary>
        public new IStyle<SpinBox> Style
        {
            get { return (IStyle<SpinBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this spin box's own look, applied in every state and layered over the look from its style.</summary>
        public new SpinBoxLook Base => (SpinBoxLook)base.Base;

        /// <summary>Gets this spin box's own look applied on hover, layered over the look from its style.</summary>
        public new SpinBoxLook PointerOver => (SpinBoxLook)base.PointerOver;

        /// <summary>Gets this spin box's own look applied when disabled, layered over the look from its style.</summary>
        public new SpinBoxLook Disabled => (SpinBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this spin box's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new SpinBoxLook this[State state] => (SpinBoxLook)base[state];

        /// <summary>Gets the style of the number box, or null to use the host theme's number box style.</summary>
        /// <remarks>Set by this spin box's looks.</remarks>
        public IStyle<NumberBox> BoxStyle { get; private set; }

        /// <summary>Gets the style of the minus and plus buttons, or null to use the host theme's button style.</summary>
        /// <remarks>Set by this spin box's looks.</remarks>
        public IStyle<Button> StepStyle { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<SpinBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new SpinBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var spin = (SpinBoxLook)look;
            BoxStyle = spin.BoxStyle;
            StepStyle = spin.StepStyle;
            Box.Style = BoxStyle;
            _less.Style = StepStyle;
            _more.Style = StepStyle;
        }

        /// <summary>Disables the button that steps toward a bound the number stands at, and enables the other.</summary>
        void Bound()
        {
            var value = Box.Value;
            var known = !double.IsNaN(value);
            _down.CanExecute = !known || !Box.Minimum.HasValue || value > Box.Minimum.Value;
            _up.CanExecute = !known || !Box.Maximum.HasValue || value < Box.Maximum.Value;
        }
    }
}
