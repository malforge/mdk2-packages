using System;
using System.Collections.Generic;
using Lumel.Input;
using VRage.Input;

namespace Lumel.Screens
{
    /// <summary>Passes a workspace the keys a keyboard reports, one frame at a time.</summary>
    public sealed class KeyFeed
    {
        readonly MyKeys[] _repeating =
        {
            MyKeys.Left, MyKeys.Right, MyKeys.Up, MyKeys.Down, MyKeys.Back, MyKeys.Delete, MyKeys.Home, MyKeys.End
        };

        readonly List<MyKeys> _held = new List<MyKeys>();
        readonly KeyRepeater _repeat = new KeyRepeater();

        /// <summary>
        ///     Passes <paramref name="view" /> this frame's keys from <paramref name="keys" /> at tick
        ///     <paramref name="now" />: every key that went down, the arrows and editing keys again while held, and while an
        ///     element has keyboard focus the characters typed without Ctrl. Returns true if anything was typed or edited
        ///     while an element had keyboard focus.
        /// </summary>
        /// <remarks>Escape is handed over by <see cref="Escape" />, as the game reports it, and the left, right and middle mouse buttons as the pointer's.</remarks>
        public bool Dispatch(IKeyboard keys, Workspace view, int now)
        {
            var shift = keys.IsShiftHeld;
            var ctrl = keys.IsCtrlHeld;
            var focused = view.Input.Focused != null;
            var typed = focused && !ctrl && Type(keys, view);

            keys.ListHeldKeys(_held);
            foreach (var key in _held)
            {
                if (IsPassedElsewhere(key) || IsRepeating(key) || !keys.IsNewKeyPressed(key))
                    continue;

                view.Key(key, shift, ctrl);
            }

            foreach (var key in _repeating)
            {
                if (!_repeat.Fires((int)key, keys.IsNewKeyPressed(key), keys.IsKeyPressed(key), now))
                    continue;

                view.Key(key, shift, ctrl);
                typed |= focused;
            }

            return typed;
        }

        /// <summary>
        ///     Passes Escape to <paramref name="view" /> and returns true if the screen may close, which is when
        ///     nothing in the view handled it.
        /// </summary>
        public bool Escape(IKeyboard keys, Workspace view)
        {
            return !view.Key(MyKeys.Escape, keys.IsShiftHeld, keys.IsCtrlHeld);
        }

        /// <summary>Passes <paramref name="view" /> the characters typed this frame, and returns true if there were any.</summary>
        static bool Type(IKeyboard keys, Workspace view)
        {
            for (var i = 0; i < keys.TypedCount; i++)
                view.Text(keys.Typed(i));

            return keys.TypedCount > 0;
        }

        /// <summary>Returns true if <paramref name="key" /> repeats while held.</summary>
        bool IsRepeating(MyKeys key)
        {
            return Array.IndexOf(_repeating, key) >= 0;
        }

        /// <summary>
        ///     Returns true if <paramref name="key" /> reaches the workspace by another way than this feed's keys: Escape, a
        ///     pointer button, or a modifier, which comes with every key as the shift and ctrl it passes.
        /// </summary>
        static bool IsPassedElsewhere(MyKeys key)
        {
            switch (key)
            {
                case MyKeys.LeftButton:
                case MyKeys.RightButton:
                case MyKeys.MiddleButton:
                case MyKeys.Escape:
                case MyKeys.Shift:
                case MyKeys.LeftShift:
                case MyKeys.RightShift:
                case MyKeys.Control:
                case MyKeys.LeftControl:
                case MyKeys.RightControl:
                case MyKeys.Alt:
                case MyKeys.LeftAlt:
                case MyKeys.RightAlt:
                    return true;
                default:
                    return false;
            }
        }
    }
}
