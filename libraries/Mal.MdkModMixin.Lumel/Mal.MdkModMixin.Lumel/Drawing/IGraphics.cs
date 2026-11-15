using System.Text;
using Lumel.Images;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>Everything the UI is allowed to draw with.</summary>
    /// <remarks>
    ///     Coordinates are UI units throughout, with the origin at the top-left of the drawable area. The
    ///     layout never rounds; rounding to pixels is the backend's.
    /// </remarks>
    public interface IGraphics
    {
        /// <summary>The drawable area, in UI units.</summary>
        Vector2 Size { get; }

        /// <summary>
        ///     The whole screen in UI units, which extends past the drawable area on either side when the screen is
        ///     wider than it.
        /// </summary>
        RectangleF ScreenBounds { get; }

        /// <summary>The host's resolver for the pictures that can be drawn, or null if it has none.</summary>
        IImageCatalog Images { get; }

        /// <summary>The current game tick, sixty to a second: the clock that anything animated is timed by.</summary>
        /// <remarks>Read from the host's clock when asked.</remarks>
        int Now { get; }

        /// <summary>
        ///     Asks the host to draw the next frame as well, for something in this frame that is still animating.
        /// </summary>
        /// <remarks>A screen is drawn on every frame anyway; a text surface is drawn on every frame only while one is asked for.</remarks>
        void RequestFrame();

        /// <summary>
        ///     Restricts drawing to <paramref name="bounds" />, intersected with the clip already in effect.
        /// </summary>
        /// <remarks>Every call is matched by a <see cref="PopClip" />.</remarks>
        void PushClip(RectangleF bounds);

        /// <summary>Removes the innermost clip and restores the one around it.</summary>
        void PopClip();

        /// <summary>
        ///     Moves and scales everything drawn and clipped until the matching <see cref="PopTransform" /> by
        ///     <paramref name="transform" /> about <paramref name="origin" />, inside any transform already in effect.
        /// </summary>
        /// <remarks>Text is not measured through it.</remarks>
        void PushTransform(RenderTransform transform, Vector2 origin);

        /// <summary>Removes the innermost transform and restores the one around it.</summary>
        void PopTransform();

        /// <summary>
        ///     Draws <paramref name="material" /> to fill <paramref name="destination" />, rotated about its center.
        /// </summary>
        void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation);

        /// <summary>
        ///     Draws the part of the picture of <paramref name="material" /> given by <paramref name="source" />, in
        ///     the picture's own pixels, to fill <paramref name="destination" />.
        /// </summary>
        /// <remarks>Where <see cref="SupportsSourceRectangles" /> is false the whole picture is drawn instead, stretched over the destination.</remarks>
        void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color);

        /// <summary>
        ///     Whether a sprite drawn with a source rectangle draws only that part of its picture, as the screen does
        ///     and a text panel does not.
        /// </summary>
        bool SupportsSourceRectangles { get; }

        /// <summary>
        ///     Fills <paramref name="destination" /> with a flat <paramref name="color" />, rotated about its center.
        /// </summary>
        void Fill(RectangleF destination, Color color, float rotation = 0f);

        /// <summary>Draws one run of text with its top-left corner at <paramref name="position" />.</summary>
        /// <remarks><paramref name="size" /> is the height of one line, in UI units.</remarks>
        void DrawText(string font, string text, Vector2 position, float size, Color color);

        /// <summary>
        ///     Draws one run of text at <paramref name="position" />, where <paramref name="align" /> says which end of
        ///     the run the position refers to.
        /// </summary>
        void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align);

        /// <summary>
        ///     Returns the size of <paramref name="text" /> if it were drawn at <paramref name="size" />, in UI units.
        /// </summary>
        /// <remarks>Must not allocate.</remarks>
        Vector2 MeasureText(string font, StringBuilder text, float size);

        /// <summary>
        ///     Returns where <paramref name="point" />, in UI units under every transform in force, lands on the device, in
        ///     the device's pixels: a text panel's texture, or the game's window.
        /// </summary>
        /// <remarks>Read while drawing, since it depends on the transforms pushed so far.</remarks>
        Vector2 ToDevicePixels(Vector2 point);

        /// <summary>
        ///     Returns the point, in UI units under every transform in force, that lands at <paramref name="pixels" /> on
        ///     the device; the reverse of <see cref="ToDevicePixels" />.
        /// </summary>
        /// <remarks>Read while drawing, since it depends on the transforms pushed so far.</remarks>
        Vector2 FromDevicePixels(Vector2 pixels);
    }
}