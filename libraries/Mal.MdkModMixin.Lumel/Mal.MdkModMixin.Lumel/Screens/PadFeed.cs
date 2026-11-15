using Lumel.Input;
using VRage.Input;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>Passes a workspace what a gamepad does, one frame at a time, as the game's own screens take it.</summary>
    /// <remarks>B reaches the workspace as Escape does, through the game, so it is not passed here.</remarks>
    public sealed class PadFeed
    {
        const float ScrollStep = 60f;

        static readonly PadControl[] Moves = { PadControl.Up, PadControl.Down, PadControl.Left, PadControl.Right };
        static readonly MyKeys[] Arrows = { MyKeys.Up, MyKeys.Down, MyKeys.Left, MyKeys.Right };

        // The game's screens repeat a held direction after half a second and then every tenth of a second
        // (MyJoystickConstants), at 60 ticks a second.
        readonly KeyRepeater _repeat = new KeyRepeater(30, 6);

        /// <summary>
        ///     Passes <paramref name="view" /> this frame's gamepad from <paramref name="pad" /> at tick
        ///     <paramref name="now" />, while the gamepad is the last thing used, and returns true if it was.
        /// </summary>
        /// <remarks>
        ///     Something always has the keyboard while the gamepad is in use: the first element that can take it, where
        ///     nothing has. The directions move the focus as the arrows do and repeat while held, A presses as Enter does, the
        ///     triggers switch pages as Ctrl+Tab and Ctrl+Shift+Tab do, and the right stick scrolls what has the focus.
        /// </remarks>
        public bool Dispatch(IGamepad pad, Workspace view, int now)
        {
            if (!pad.IsLastUsed)
            {
                _repeat.Clear();
                return false;
            }

            if (view.Input.Focused == null)
                view.FocusFirst();

            for (var i = 0; i < Moves.Length; i++)
            {
                if (Fires(pad, Moves[i], now))
                    view.Key(Arrows[i], false, false);
            }

            if (pad.IsNewPressed(PadControl.Accept))
                view.Key(MyKeys.Enter, false, false);

            if (pad.IsNewPressed(PadControl.PreviousPage))
                view.Key(MyKeys.Tab, true, true);

            if (pad.IsNewPressed(PadControl.NextPage))
                view.Key(MyKeys.Tab, false, true);

            var focused = view.Input.Focused;
            if (focused == null)
                return true;

            var middle = new Vector2(focused.Bounds.Center.X, focused.Bounds.Center.Y);
            if (Fires(pad, PadControl.ScrollUp, now))
                view.Wheel(focused.ToScreen(middle), -ScrollStep);

            if (Fires(pad, PadControl.ScrollDown, now))
                view.Wheel(focused.ToScreen(middle), ScrollStep);

            return true;
        }

        /// <summary>Returns true if <paramref name="control" /> counts as pressed at <paramref name="now" />, by a press or a repeat.</summary>
        bool Fires(IGamepad pad, PadControl control, int now)
        {
            return _repeat.Fires((int)control, pad.IsNewPressed(control), pad.IsHeld(control), now);
        }
    }
}
