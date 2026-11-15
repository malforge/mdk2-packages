using Lumel.Input;
using Sandbox.Game;

namespace Lumel.Screens
{
    /// <summary>Plays the game's own feedback sounds for its controls, to the local player only.</summary>
    /// <remarks>Played as the game's scripts play a sound to the local player, so silent while the player has no character.</remarks>
    public sealed class GameControlSounds : IControlSounds
    {
        /// <inheritdoc />
        public void Play(Sound sound)
        {
            MyVisualScriptLogicProvider.PlaySoundAmbientLocal(sound.Name);
        }
    }
}
