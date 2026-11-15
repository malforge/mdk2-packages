using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Lumel.Tests;

/// <summary>
///     A stand-in for the game's text panel that draws the fonts it is given and measures every letter one font size
///     wide and tall.
/// </summary>
sealed class PanelSurface : Sandbox.ModAPI.IMyTextSurface
{
    readonly List<string> _fonts;

    public PanelSurface(params string[] fonts)
    {
        _fonts = new List<string>(fonts);
    }

    public Vector2 TextureSize { get; set; } = new Vector2(512f, 512f);

    public Vector2 SurfaceSize { get; set; } = new Vector2(512f, 512f);

    public void GetFonts(List<string> fonts) => fonts.AddRange(_fonts);

    /// <summary>Gets the number of times text has been measured on the surface.</summary>
    public int Measured { get; private set; }

    public Vector2 MeasureStringInPixels(StringBuilder text, string font, float scale)
    {
        Measured++;
        return new Vector2(text.Length * 28.8f * scale, 28.8f * scale);
    }

    public Vector2 MeasureStringInPixels(string text, string font, float scale) => MeasureStringInPixels(new StringBuilder(text), font, scale);

    public float FontSize { get; set; }
    public Color FontColor { get; set; }
    public Color BackgroundColor { get; set; }
    public byte BackgroundAlpha { get; set; }
    public float ChangeInterval { get; set; }
    public string Font { get; set; } = "";
    public TextAlignment Alignment { get; set; }
    public string Script { get; set; } = "";
    public ContentType ContentType { get; set; }
    public bool PreserveAspectRatio { get; set; }
    public float TextPadding { get; set; }
    public Color ScriptBackgroundColor { get; set; }
    public Color ScriptForegroundColor { get; set; }
    public string CurrentlyShownImage => "";
    public string Name => "Panel";
    public string DisplayName => "Panel";

    public bool WriteText(string value, bool append = false) => throw new NotSupportedException();
    public string GetText() => throw new NotSupportedException();
    public bool WriteText(StringBuilder value, bool append = false) => throw new NotSupportedException();
    public void ReadText(StringBuilder buffer, bool append = false) => throw new NotSupportedException();
    public void AddImageToSelection(string id, bool checkExistence = false) => throw new NotSupportedException();
    public void AddImagesToSelection(List<string> ids, bool checkExistence = false) => throw new NotSupportedException();
    public void RemoveImageFromSelection(string id, bool removeDuplicates = false) => throw new NotSupportedException();
    public void RemoveImagesFromSelection(List<string> ids, bool removeDuplicates = false) => throw new NotSupportedException();
    public void ClearImagesFromSelection() => throw new NotSupportedException();
    public void GetSelectedImages(List<string> output) => throw new NotSupportedException();
    public void GetSprites(List<string> sprites) => throw new NotSupportedException();
    public void GetScripts(List<string> scripts) => throw new NotSupportedException();
    /// <summary>Gets the number of frames handed to the surface.</summary>
    public int Frames { get; private set; }

    public MySpriteDrawFrame DrawFrame() => new MySpriteDrawFrame(_ => Frames++);
}
