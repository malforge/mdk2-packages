using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Utils;
using VRageMath;

namespace Lumel.Tests;

/// <summary>
///     An <see cref="IGraphics" /> that measures every character as half its size wide and counts what it is asked to
///     measure.
/// </summary>
/// <remarks>Exact and the same on every machine, which is what a budget test needs from it.</remarks>
sealed class Counting : IGraphics
{
    /// <summary>Gets the number of times anything was measured.</summary>
    public int Calls { get; private set; }

    /// <summary>Gets the total number of characters passed to be measured.</summary>
    public long Characters { get; private set; }

    public Vector2 Size => new Vector2(1440f, 1080f);

    public RectangleF ScreenBounds => new RectangleF(0f, 0f, 1440f, 1080f);

    public IImageCatalog? Images => null;

    public bool SupportsSourceRectangles => true;

    public int Now => 0;

    public void RequestFrame()
    {
    }

    /// <summary>Resets both counts to zero.</summary>
    public void Reset()
    {
        Calls = 0;
        Characters = 0;
    }

    public Vector2 MeasureText(string font, StringBuilder text, float size)
    {
        Calls++;
        Characters += text.Length;
        return new Vector2(text.Length * size * 0.5f, size);
    }

    public void PushClip(RectangleF bounds)
    {
    }

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

    public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
    {
    }

    public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color)
    {
    }

    public void Fill(RectangleF destination, Color color, float rotation = 0f)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align)
    {
    }
}
