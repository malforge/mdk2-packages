using System;
using Lumel.Input;
using Sandbox.ModAPI;
using VRage.Input;

namespace Lumel.Screens
{
    /// <summary>An <see cref="IGamepad" /> that reads the game's first gamepad as it stands when asked.</summary>
    /// <remarks>
    ///     Read as the game's default controller scheme binds its screens: the D-pad and left stick move, A presses, the
    ///     triggers switch pages and the right stick scrolls.
    /// </remarks>
    public sealed class GameGamepad : IGamepad
    {
        /// <inheritdoc />
        public bool IsLastUsed => MyAPIGateway.Input.IsJoystickLastUsed;

        /// <inheritdoc />
        public bool IsNewPressed(PadControl control)
        {
            MyJoystickButtonsEnum button;
            MyJoystickAxesEnum axis;
            Bound(control, out button, out axis);

            var input = MyAPIGateway.Input;
            return (button != MyJoystickButtonsEnum.None && input.IsJoystickButtonNewPressed(button))
                   || (axis != MyJoystickAxesEnum.None && input.IsJoystickAxisNewPressed(axis));
        }

        /// <inheritdoc />
        public bool IsHeld(PadControl control)
        {
            MyJoystickButtonsEnum button;
            MyJoystickAxesEnum axis;
            Bound(control, out button, out axis);

            var input = MyAPIGateway.Input;
            return (button != MyJoystickButtonsEnum.None && input.IsJoystickButtonPressed(button))
                   || (axis != MyJoystickAxesEnum.None && input.IsJoystickAxisPressed(axis));
        }

        /// <summary>
        ///     Gives the button and the stick direction that <paramref name="control" /> is bound to, each None where it
        ///     has none.
        /// </summary>
        static void Bound(PadControl control, out MyJoystickButtonsEnum button, out MyJoystickAxesEnum axis)
        {
            button = MyJoystickButtonsEnum.None;
            axis = MyJoystickAxesEnum.None;
            switch (control)
            {
                case PadControl.Up:
                    button = MyJoystickButtonsEnum.JDUp;
                    axis = MyJoystickAxesEnum.Yneg;
                    break;
                case PadControl.Down:
                    button = MyJoystickButtonsEnum.JDDown;
                    axis = MyJoystickAxesEnum.Ypos;
                    break;
                case PadControl.Left:
                    button = MyJoystickButtonsEnum.JDLeft;
                    axis = MyJoystickAxesEnum.Xneg;
                    break;
                case PadControl.Right:
                    button = MyJoystickButtonsEnum.JDRight;
                    axis = MyJoystickAxesEnum.Xpos;
                    break;
                case PadControl.Accept:
                    button = MyJoystickButtonsEnum.J01;
                    break;
                case PadControl.PreviousPage:
                    axis = MyJoystickAxesEnum.Zpos;
                    break;
                case PadControl.NextPage:
                    axis = MyJoystickAxesEnum.Zneg;
                    break;
                case PadControl.ScrollUp:
                    axis = MyJoystickAxesEnum.RotationYneg;
                    break;
                case PadControl.ScrollDown:
                    axis = MyJoystickAxesEnum.RotationYpos;
                    break;
                default:
                    throw new ArgumentException("A gamepad has no control " + control + ".", nameof(control));
            }
        }
    }
}
