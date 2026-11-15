using System.Collections.Generic;
using Lumel.Input;
using Sandbox.ModAPI;
using VRage.Input;

namespace Lumel.Screens
{
    /// <summary>An <see cref="IKeyboard" /> that reads the game's keyboard as it stands when asked.</summary>
    public sealed class GameKeyboard : IKeyboard
    {
        /// <inheritdoc />
        public bool IsShiftHeld => MyAPIGateway.Input.IsAnyShiftKeyPressed();

        /// <inheritdoc />
        public bool IsCtrlHeld => MyAPIGateway.Input.IsAnyCtrlKeyPressed();

        /// <inheritdoc />
        public int TypedCount => MyAPIGateway.Input.TextInput.Count;

        /// <inheritdoc />
        public bool IsNewKeyPressed(MyKeys key)
        {
            return MyAPIGateway.Input.IsNewKeyPressed(key);
        }

        /// <inheritdoc />
        public bool IsKeyPressed(MyKeys key)
        {
            return MyAPIGateway.Input.IsKeyPress(key);
        }

        /// <inheritdoc />
        public void ListHeldKeys(List<MyKeys> keys)
        {
            keys.Clear();
            MyAPIGateway.Input.GetPressedKeys(keys);
        }

        /// <inheritdoc />
        public char Typed(int index)
        {
            return MyAPIGateway.Input.TextInput[index];
        }
    }
}
