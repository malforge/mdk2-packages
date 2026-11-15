using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Lumel.Drawing
{
    /// <summary>
    ///     The fonts the game has loaded, its own and those of the mods in the world, by the names of their
    ///     definitions.
    /// </summary>
    public static class GameFonts
    {
        /// <summary>
        ///     The game's own font for its screen text, which is used to draw text in a font the game has not loaded.
        /// </summary>
        public const string Default = "White";

        /// <summary>Returns true if the game has loaded a font named <paramref name="font" />.</summary>
        public static bool Exists(string font)
        {
            var definitions = MyDefinitionManager.Static;
            return definitions != null && definitions.GetDefinition(
                new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), font)) is MyFontDefinition;
        }
    }
}
