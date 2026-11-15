using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>The screen the game gives a mod to draw on, in the game's own terms.</summary>
    /// <remarks>The game's scissor is a struct scope, so each implementation keeps its own stack of them.</remarks>
    public interface IScreenSpace
    {
        /// <summary>The area the game guarantees is on screen, in pixels.</summary>
        RectangleF SafeArea { get; }

        /// <summary>
        ///     The size of the whole window in pixels, which is wider than the safe area on anything but 4:3.
        /// </summary>
        /// <remarks>The game's own cursor reaches the edges of this, not of the safe area.</remarks>
        Vector2 ViewportSize { get; }

        /// <summary>
        ///     Clips what is drawn on <paramref name="layer" /> to <paramref name="pixels" /> until the matching pop.
        /// </summary>
        void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer);

        /// <summary>Removes the innermost scissor and restores the one around it.</summary>
        void PopScissor(MyScreenSpaceLayer layer);

        /// <summary>
        ///     Draws <paramref name="source" /> of the picture of <paramref name="material" />, in its own pixels (or
        ///     all of it if null), stretched over <paramref name="destination" />, tinted and rotated.
        /// </summary>
        void DrawSprite(MyStringId material, Rectangle destination, Rectangle? source, Color color, float rotation,
            MyScreenSpaceLayer layer);

        /// <summary>
        ///     Returns the size of the picture of <paramref name="material" /> in its own pixels, or zero if the
        ///     material cannot be drawn.
        /// </summary>
        Vector2 TextureSize(MyStringId material);

        /// <summary>
        ///     Draws one run of text at <paramref name="position" />, aligned about it as <paramref name="align" />
        ///     says.
        /// </summary>
        void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer);

        /// <summary>Returns the size of the run of text in pixels.</summary>
        /// <remarks>An empty run measures as one line, as tall as the game draws a line.</remarks>
        Vector2 MeasureText(string font, StringBuilder text, float scale);

        /// <summary>Returns true if the game has loaded a font named <paramref name="font" />.</summary>
        bool HasFont(string font);

        /// <summary>
        ///     Returns the height of one line of <paramref name="font" /> in the font's own units, as its file says, or
        ///     0 if that cannot be read.
        /// </summary>
        int LineUnits(string font);

        /// <summary>
        ///     Returns true and the pointer position in <paramref name="pixels" />, or false if the game is not showing
        ///     a pointer.
        /// </summary>
        bool TryCursorPixels(out Vector2 pixels);

        /// <summary>Converts <paramref name="position" /> from the game's normalized units to pixels.</summary>
        Vector2 NormalizedToPixels(Vector2 position);

        /// <summary>Converts <paramref name="size" /> from the game's normalized units to pixels.</summary>
        Vector2 NormalizedSizeToPixels(Vector2 size);
    }
}