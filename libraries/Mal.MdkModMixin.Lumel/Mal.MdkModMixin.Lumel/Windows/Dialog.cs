using Lumel.Input;
using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>A modal window in the middle of the screen that asks a question and waits for the answer.</summary>
    /// <remarks>Accepting carries out <see cref="AcceptCommand" /> and canceling <see cref="CancelCommand" />: Enter and Escape, where nothing inside takes the key itself.</remarks>
    public class Dialog : Window
    {
        ICommand _accept;
        ICommand _cancel;

        /// <summary>Creates an empty dialog.</summary>
        public Dialog()
        {
            IsModal = true;
        }

        /// <summary>
        ///     Gets or sets the style of this dialog, or null to use the host theme's default for this kind of dialog.
        /// </summary>
        public new IStyle<Dialog> Style
        {
            get { return (IStyle<Dialog>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Dialog>;
        }

        /// <summary>
        ///     Gets or sets the command that gives the answer when Enter is pressed, or null if Enter gives none.
        /// </summary>
        public ICommand AcceptCommand
        {
            get { return _accept; }
            set
            {
                _accept = value;
                CommandsChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the command that runs when Escape is pressed, closing the dialog without an answer, or null
        ///     if the dialog cannot be closed that way.
        /// </summary>
        public ICommand CancelCommand
        {
            get { return _cancel; }
            set
            {
                _cancel = value;
                CommandsChanged();
            }
        }

        /// <summary>Gives the answer that Enter gives and returns true if there was one.</summary>
        public bool PerformAccept()
        {
            return Carry(_accept);
        }

        /// <summary>Closes the dialog the way Escape does and returns true if it could be closed.</summary>
        public bool PerformCancel()
        {
            return Carry(_cancel);
        }

        /// <summary>
        ///     Called when <see cref="AcceptCommand" /> or <see cref="CancelCommand" /> changes, for dialogs that show
        ///     them.
        /// </summary>
        protected virtual void CommandsChanged()
        {
        }

        /// <inheritdoc />
        protected override bool OnIntent(InputRoot input, Intent intent)
        {
            if (intent == Intent.Accept)
                return PerformAccept();

            return intent == Intent.Cancel && PerformCancel();
        }

        static bool Carry(ICommand command)
        {
            if (command == null || !command.CanExecute)
                return false;

            command.Execute();
            return true;
        }
    }
}
