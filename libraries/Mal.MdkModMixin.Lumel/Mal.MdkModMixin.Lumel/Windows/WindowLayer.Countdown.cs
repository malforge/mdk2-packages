using System;

namespace Lumel.Windows
{
    public partial class WindowLayer
    {
        /// <summary>An action waiting in this layer for its time, for as long as the element it was made for is shown.</summary>
        sealed class Countdown : ICountdown
        {
            readonly WindowLayer _layer;
            readonly Element _owner;
            readonly Action _action;

            /// <summary>
            ///     Creates the countdown, due at <paramref name="due" />, and cancels it as <paramref name="owner" />
            ///     stops being shown.
            /// </summary>
            public Countdown(WindowLayer layer, Element owner, int due, Action action)
            {
                _layer = layer;
                _owner = owner;
                _action = action;
                Due = due;
                IsPending = true;
                owner.Unloaded += Cancel;
            }

            /// <summary>Gets the time, in the drawing's ticks, from which the action runs.</summary>
            public int Due { get; }

            /// <inheritdoc />
            public bool IsPending { get; private set; }

            /// <inheritdoc />
            public void Cancel()
            {
                if (!IsPending)
                    return;

                End();
                _layer._countdowns.Remove(this);
            }

            /// <summary>Runs the action, if it is still pending.</summary>
            public void Run()
            {
                if (!IsPending)
                    return;

                End();
                _action();
            }

            /// <summary>Marks the countdown done and stops listening to its element.</summary>
            void End()
            {
                IsPending = false;
                _owner.Unloaded -= Cancel;
            }
        }
    }
}
