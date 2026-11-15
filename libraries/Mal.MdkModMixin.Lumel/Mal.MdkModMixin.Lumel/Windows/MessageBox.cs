using System;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>
    ///     A dialog that shows a message and offers up to three standard answers, Yes, No and Cancel, in that order
    ///     along its bottom, as the game's own message box does.
    /// </summary>
    /// <remarks>
    ///     A Cancel button is shown while <see cref="Dialog.CancelCommand" /> has a command and <see cref="ShowsCancel" /> is set.
    ///     The message and the buttons look as the box's looks say, and where those say nothing, as the host's theme holds for
    ///     their kinds.
    /// </remarks>
    public class MessageBox : Dialog
    {
        /// <summary>The space between the message and the buttons when no look sets it, in UI units.</summary>
        public const float NeutralMessageSpacing = 24f;

        /// <summary>The space between one button and the next when no look sets it, in UI units.</summary>
        public const float NeutralButtonSpacing = 8f;

        readonly VStack _column;
        readonly HStack _buttons;
        readonly RichText _message;
        readonly Button _yes;
        readonly Button _no;
        readonly Button _cancel;
        readonly RelayCommand _accepts;

        ICommand _yesCommand;
        ICommand _noCommand;
        MessageBoxAnswer? _defaultButton;
        bool _showsCancel = true;

        /// <summary>
        ///     Creates a message box with no message and buttons labeled Yes, No and Cancel, using the host theme's
        ///     message box style until it is given a style.
        /// </summary>
        public MessageBox()
        {
            _accepts = new RelayCommand(Accepted);

            Content = new VStack
            {
                Children =
                {
                    new RichText().As(out _message),
                    new HStack
                    {
                        Align = Align.Center,
                        Children =
                        {
                            new Button { Text = "Yes", Command = new RelayCommand(() => Carry(_yesCommand)), FnSound = s => AnswerSound(MessageBoxAnswer.Yes, s) }.As(out _yes),
                            new Button { Text = "No", Command = new RelayCommand(() => Carry(_noCommand)), FnSound = s => AnswerSound(MessageBoxAnswer.No, s) }.As(out _no),
                            new Button { Text = "Cancel", Command = new RelayCommand(() => PerformCancel()), FnSound = s => AnswerSound(MessageBoxAnswer.Cancel, s) }.As(out _cancel)
                        }
                    }.As(out _buttons)
                }
            }.As(out _column);

            MaxWidth = float.PositiveInfinity;
            MessageSpacing = NeutralMessageSpacing;
            ButtonSpacing = NeutralButtonSpacing;
            AcceptCommand = _accepts;
        }

        /// <summary>
        ///     Gets or sets the style of this message box, or null to use the host theme's default message box style.
        /// </summary>
        public new IStyle<MessageBox> Style
        {
            get { return (IStyle<MessageBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this message box's own look, applied in every state on top of the style's look.</summary>
        public new MessageBoxLook Base => (MessageBoxLook)base.Base;

        /// <summary>Gets this message box's own look when hovered, applied on top of the style's look.</summary>
        public new MessageBoxLook PointerOver => (MessageBoxLook)base.PointerOver;

        /// <summary>Gets this message box's own look when disabled, applied on top of the style's look.</summary>
        public new MessageBoxLook Disabled => (MessageBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this message box's own look for <paramref name="state" />, or for every state that includes it,
        ///     applied on top of the style's look.
        /// </summary>
        public new MessageBoxLook this[State state] => (MessageBoxLook)base[state];

        /// <summary>Gets or sets the message the box shows.</summary>
        public Document Message
        {
            get { return _message.Document; }
            set { _message.Document = value; }
        }

        /// <summary>Gets or sets the arguments for the message's named slots, or null if it has none.</summary>
        public Arguments MessageArguments
        {
            get { return _message.Arguments; }
            set { _message.Arguments = value; }
        }

        /// <summary>Gets the maximum width of the box, in UI units, beyond which the message wraps.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float MaxWidth { get; private set; }

        /// <summary>Gets the space between the message and the buttons.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float MessageSpacing { get; private set; }

        /// <summary>Gets the space between one button and the next.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float ButtonSpacing { get; private set; }

        /// <summary>Gets the style of the message, or null to use the host theme's rich text style.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public IStyle<RichText> MessageStyle { get; private set; }

        /// <summary>
        ///     Gets the style of the answer buttons other than the default one, or null to use the host theme's button
        ///     style.
        /// </summary>
        /// <remarks>Set by this box's looks.</remarks>
        public IStyle<Button> ButtonStyle { get; private set; }

        /// <summary>
        ///     Gets the style of the default answer button, the one Enter gives, or null to use the host theme's button
        ///     style.
        /// </summary>
        /// <remarks>Set by this box's looks.</remarks>
        public IStyle<Button> DefaultButtonStyle { get; private set; }

        /// <summary>Gets or sets the command that runs for Yes, or null to hide the Yes button.</summary>
        public ICommand YesCommand
        {
            get { return _yesCommand; }
            set
            {
                _yesCommand = value;
                Dress();
            }
        }

        /// <summary>Gets or sets the command that runs for No, or null to hide the No button.</summary>
        public ICommand NoCommand
        {
            get { return _noCommand; }
            set
            {
                _noCommand = value;
                Dress();
            }
        }

        /// <summary>Gets or sets the label of the Yes button.</summary>
        public string YesText
        {
            get { return _yes.Text; }
            set { _yes.Text = value; }
        }

        /// <summary>Gets or sets the label of the No button.</summary>
        public string NoText
        {
            get { return _no.Text; }
            set { _no.Text = value; }
        }

        /// <summary>Gets or sets the label of the Cancel button.</summary>
        public string CancelText
        {
            get { return _cancel.Text; }
            set { _cancel.Text = value; }
        }

        /// <summary>Whether canceling has its own button, rather than only the Escape key.</summary>
        public bool ShowsCancel
        {
            get { return _showsCancel; }
            set
            {
                _showsCancel = value;
                Dress();
            }
        }

        /// <summary>
        ///     Gets or sets the answer that Enter gives, which is highlighted among the buttons, or null if Enter gives
        ///     none.
        /// </summary>
        public MessageBoxAnswer? DefaultButton
        {
            get { return _defaultButton; }
            set
            {
                _defaultButton = value;
                Dress();
            }
        }

        /// <summary>
        ///     Gets or sets the logic that picks the sound each answer's button plays, returning null for the one its
        ///     style sets or <see cref="Sound.None" /> for silence.
        /// </summary>
        /// <remarks>Asked as it happens: for a click, before the answer's command runs.</remarks>
        public Func<MessageBoxAnswer, ControlSound, Sound> FnAnswerSound { get; set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<MessageBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new MessageBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (MessageBoxLook)look;
            var maxWidth = box.MaxWidth ?? float.PositiveInfinity;
            if (maxWidth != MaxWidth)
            {
                MaxWidth = maxWidth;
                InvalidateMeasure();
            }

            MessageSpacing = box.MessageSpacing ?? NeutralMessageSpacing;
            ButtonSpacing = box.ButtonSpacing ?? NeutralButtonSpacing;
            MessageStyle = box.MessageStyle;
            ButtonStyle = box.ButtonStyle;
            DefaultButtonStyle = box.DefaultButtonStyle;

            if (_column.Base.Spacing != MessageSpacing)
                _column.Base.Spacing = MessageSpacing;

            if (_buttons.Base.Spacing != ButtonSpacing)
                _buttons.Base.Spacing = ButtonSpacing;

            _message.Style = MessageStyle;
            Dress();
        }

        /// <inheritdoc />
        /// <remarks>Holds what it holds to <see cref="MaxWidth" />, less its padding.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var widest = MaxWidth - Padding.Horizontal;
            return base.MeasureContent(g, new Vector2(Math.Min(available.X, Math.Max(0f, widest)), available.Y));
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Enter presses the default answer's button and Escape the Cancel button, shown or not, so each sounds as that
        ///     button does.
        /// </remarks>
        protected override bool OnIntent(InputRoot input, Intent intent)
        {
            MessageBoxAnswer answer;
            if (intent == Intent.Accept && _defaultButton.HasValue)
                answer = _defaultButton.Value;
            else if (intent == Intent.Cancel)
                answer = MessageBoxAnswer.Cancel;
            else
                return false;

            var command = CommandFor(answer);
            return command != null && command.CanExecute && input.Press(ButtonFor(answer));
        }

        /// <inheritdoc />
        protected override void CommandsChanged()
        {
            Dress();
        }

        /// <summary>Shows the buttons that have a command and marks the default one.</summary>
        void Dress()
        {
            _accepts.CanExecute = _defaultButton.HasValue;

            _yes.IsVisible = _yesCommand != null;
            _no.IsVisible = _noCommand != null;
            _cancel.IsVisible = CancelCommand != null && _showsCancel;

            _yes.Style = _defaultButton == MessageBoxAnswer.Yes ? DefaultButtonStyle : ButtonStyle;
            _no.Style = _defaultButton == MessageBoxAnswer.No ? DefaultButtonStyle : ButtonStyle;
            _cancel.Style = _defaultButton == MessageBoxAnswer.Cancel ? DefaultButtonStyle : ButtonStyle;
        }

        void Accepted()
        {
            if (_defaultButton.HasValue)
                Carry(CommandFor(_defaultButton.Value));
        }

        /// <summary>Returns the command that gives <paramref name="answer" />, or null if it has none.</summary>
        ICommand CommandFor(MessageBoxAnswer answer)
        {
            switch (answer)
            {
                case MessageBoxAnswer.Yes:
                    return _yesCommand;
                case MessageBoxAnswer.No:
                    return _noCommand;
                case MessageBoxAnswer.Cancel:
                    return CancelCommand;
                default:
                    throw new ArgumentException("A message box has no answer " + answer + ".", nameof(answer));
            }
        }

        /// <summary>Returns the button that gives <paramref name="answer" />.</summary>
        Button ButtonFor(MessageBoxAnswer answer)
        {
            switch (answer)
            {
                case MessageBoxAnswer.Yes:
                    return _yes;
                case MessageBoxAnswer.No:
                    return _no;
                case MessageBoxAnswer.Cancel:
                    return _cancel;
                default:
                    throw new ArgumentException("A message box has no answer " + answer + ".", nameof(answer));
            }
        }

        /// <summary>Returns the sound <see cref="FnAnswerSound" /> picks for <paramref name="answer" />, or null for its button's own.</summary>
        Sound AnswerSound(MessageBoxAnswer answer, ControlSound sound)
        {
            return FnAnswerSound?.Invoke(answer, sound);
        }

        static void Carry(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }
    }
}
