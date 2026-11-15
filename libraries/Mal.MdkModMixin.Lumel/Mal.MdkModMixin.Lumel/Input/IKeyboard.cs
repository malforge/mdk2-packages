using System.Collections.Generic;
using VRage.Input;

namespace Lumel.Input
{
    /// <summary>The state of the keyboard in one frame, as a host reads it.</summary>
    public interface IKeyboard
    {
        /// <summary>Whether either Shift key is held.</summary>
        bool IsShiftHeld { get; }

        /// <summary>Whether either Ctrl key is held.</summary>
        bool IsCtrlHeld { get; }

        /// <summary>Gets the number of characters typed in this frame.</summary>
        int TypedCount { get; }

        /// <summary>Whether <paramref name="key" /> went down in this frame.</summary>
        bool IsNewKeyPressed(MyKeys key);

        /// <summary>Whether <paramref name="key" /> is held down.</summary>
        bool IsKeyPressed(MyKeys key);

        /// <summary>Fills <paramref name="keys" /> with every key held down, replacing what it held.</summary>
        void ListHeldKeys(List<MyKeys> keys);

        /// <summary>Returns the character typed at position <paramref name="index" /> in this frame.</summary>
        char Typed(int index);
    }
}
