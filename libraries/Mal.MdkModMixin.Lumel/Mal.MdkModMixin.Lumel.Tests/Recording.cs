using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Utils;
using VRageMath;

namespace Lumel.Tests;

/// <summary>
///     An <see cref="IGraphics" /> that measures every character as half its size wide and records where it was asked
///     to fill.
/// </summary>
sealed class Recording(RectangleF area) : IGraphics
{
    public List<RectangleF> Fills { get; } = new List<RectangleF>();

    /// <summary>Gets every picture drawn from part of a texture, in order, with where it was drawn.</summary>
    public List<(MyStringId Material, RectangleF Destination)> Pieces { get; } = new List<(MyStringId, RectangleF)>();

    public Vector2 Size => new Vector2(area.Width, area.Height);

    public RectangleF ScreenBounds => area;

    public IImageCatalog? Images => null;

    /// <summary>
    ///     Whether it draws part of a picture, as the screen does, or only the whole picture, as a text panel does.
    /// </summary>
    public bool SupportsSourceRectangles { get; set; } = true;

    /// <summary>Gets or sets the tick the frame is drawn at, which the test advances.</summary>
    public int Now { get; set; }

    /// <summary>Gets the number of times something drawn requested the next frame.</summary>
    public int FramesRequested { get; private set; }

    public void RequestFrame() => FramesRequested++;

    public Vector2 MeasureText(string font, StringBuilder text, float size) => new Vector2(text.Length * size * 0.5f, size);

    public void Fill(RectangleF destination, Color color, float rotation = 0f) => Fills.Add(destination);

    /// <summary>Gets the number of clips pushed.</summary>
    public int Clips { get; private set; }

    public void PushClip(RectangleF bounds) => Clips++;

    public void PopClip()
    {
    }

    public Vector2 ToDevicePixels(Vector2 point) => point;

    public Vector2 FromDevicePixels(Vector2 pixels) => pixels;

    public void PushTransform(RenderTransform transform, Vector2 origin)
    {
    }

    public void PopTransform()
    {
    }

    public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation) =>
        Pieces.Add((material, destination));

    public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color) =>
        Pieces.Add((material, destination));

    public void DrawText(string font, string text, Vector2 position, float size, Color color)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align)
    {
    }
}
