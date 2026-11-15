using System;

namespace Lumel.Input
{
    /// <summary>
    ///     An <see cref="ICommand" /> that runs a given action and is enabled for as long as it is told it is.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        readonly Action _execute;
        bool _canExecute = true;

        /// <summary>Creates a command that runs <paramref name="execute" />.</summary>
        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        /// <inheritdoc cref="ICommand.CanExecute" />
        public bool CanExecute
        {
            get { return _canExecute; }
            set
            {
                if (_canExecute == value)
                    return;

                _canExecute = value;

                var changed = CanExecuteChanged;
                if (changed != null)
                    changed();
            }
        }

        /// <inheritdoc />
        public event Action CanExecuteChanged;

        /// <inheritdoc />
        public void Execute()
        {
            if (_canExecute)
                _execute();
        }
    }
}
