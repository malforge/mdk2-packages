using System;

namespace Lumel.Input
{
    /// <summary>An action an element runs when it is used, supplied by the application.</summary>
    public interface ICommand
    {
        /// <summary>
        ///     Whether the command can run now; an element carrying a command that cannot run is disabled.
        /// </summary>
        bool CanExecute { get; }

        /// <summary>Raised when <see cref="CanExecute" /> may have changed.</summary>
        event Action CanExecuteChanged;

        /// <summary>Runs the command.</summary>
        void Execute();
    }
}
