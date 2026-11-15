using System.Text;
using Sandbox.ModAPI;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Text;
using VRageMath;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>
/// One element, measured, arranged and drawn with nothing else on the screen.
/// </summary>
/// <remarks>
/// The device and graphics a host's harness draws with, with nothing above them, so a control can be asked what it does
/// on its own rather than through whichever screen happens to use it. What comes back is the frame as a list of calls, which is what makes cost assertable.
/// </remarks>
sealed class Easel
{
    readonly ScreenSpaceDevice _device;
    readonly ScreenSpaceGraphics _graphics;
    readonly Element _element;

    readonly bool _exact;

    public Easel(Element element, float width, float height) : this(element, width, height, false)
    {
    }

    /// <summary>
    ///     Creates an easel for <paramref name="element" /> at <paramref name="width" /> by <paramref name="height" />,
    ///     measuring with the game's own font instead of a stand-in if <paramref name="exact" /> is true.
    /// </summary>
    /// <remarks>
    /// The stand-in's widths are not the game's and it says so: its digits are all one width, where the
    /// game's are not remotely - a <c>1</c> is less than half a <c>0</c>. Anything about how text of
    /// different widths lines up therefore cannot be seen with it, and has to ask for this instead.
    /// </remarks>
    public Easel(Element element, float width, float height, bool exact) : this(element, width, height, exact, null)
    {
    }

    /// <summary>
    ///     Creates an easel that draws <paramref name="element" /> using <paramref name="images" /> to resolve
    ///     pictures, as a host passes its own resolver to the graphics, instead of the screen's materials.
    /// </summary>
    public Easel(Element element, float width, float height, IImageCatalog images) : this(element, width, height, false, images)
    {
    }

    Easel(Element element, float width, float height, bool exact, IImageCatalog? images)
    {
        _element = element;

        _device = new ScreenSpaceDevice(LumelContent.Instance);

        SeFontMetrics? game = exact ? SeFontMetrics.Load(_device) : null;
        _exact = game != null;
        _device.Fonts = game ?? (IFontMetrics)new FallbackFonts(_device, "Segoe UI");

        _device.SafeArea = new RectangleF(0f, 0f, width, height);
        _device.ViewportSize = new Vector2(width, height);

        _graphics = new ScreenSpaceGraphics(_device, MyScreenSpaceLayer.AboveHud, images ?? new MaterialImages(_device.TextureSize), () => Now);

        Draw(width, height);
    }

    /// <summary>Gets every draw command the last frame issued.</summary>
    public IList<DrawCommand> Commands => _device.Commands;

    /// <summary>Gets the text runs the last frame drew.</summary>
    public IEnumerable<DrawCommand> Written => Commands.Where(c => c.Kind == DrawKind.Text);

    /// <summary>Gets the sprites the last frame drew, which for a bare control are its thumbs.</summary>
    public IEnumerable<DrawCommand> Sprites => Commands.Where(c => c.Kind == DrawKind.Sprite);

    /// <summary>Gets the number of times anything has measured the width of a string.</summary>
    public int Measured => _device.Measured;

    /// <summary>Gets the fonts the stand-in game has not loaded.</summary>
    public ISet<string> MissingFonts => _device.MissingFonts;

    /// <summary>
    ///     Gets the height of a line of text at the starting size of a text element, in the units the screen lays out
    ///     in.
    /// </summary>
    public float LineHeight => Starting;

    /// <summary>Gets the number of window pixels per UI unit.</summary>
    /// <remarks>
    /// A recorded call carries pixels and a layout carries UI units, and the two are only the same on
    /// a screen the height the layout was written against. Anything comparing a drawn position with a
    /// number the layout was given goes through here.
    /// </remarks>
    public float Pixels => _device.ViewportSize.Y / _graphics.Size.Y;

    /// <summary>Whether the widths are the game's own rather than a stand-in's.</summary>
    public bool IsExact => _exact;

    /// <summary>
    ///     Returns the width of the text run <paramref name="said" />, in the units a drawn position is recorded in.
    /// </summary>
    public float WidthOf(string said) => WidthOf(said, Starting);

    /// <summary>
    ///     Returns the width of the text run <paramref name="said" /> at <paramref name="size" />, in the units a drawn
    ///     position is recorded in.
    /// </summary>
    public float WidthOf(string said, float size)
    {
        _measuring.Length = 0;
        _measuring.Append(said);

        return _graphics.MeasureText(VRage.Game.MyFontEnum.White, _measuring, size).X * Pixels;
    }

    readonly StringBuilder _measuring = new StringBuilder(32);

    // The size a label is drawn at out of the box: the game theme's, which a label takes once it works out its look.
    static readonly float Starting = Styled(new Label()).FontSize;

    static Label Styled(Label label)
    {
        label.Restyle();
        return label;
    }

    /// <summary>Gets the game tick at which the last frame was drawn.</summary>
    public int Now { get; private set; }

    /// <summary>Whether anything drawn in the last frame is still animating.</summary>
    public bool IsFrameRequested => _graphics.IsFrameRequested;

    /// <summary>
    ///     Lays out and draws repeatedly, one second apart, until nothing is animating, so the last frame is at rest,
    ///     up to <see cref="MostFrames" /> frames.
    /// </summary>
    /// <remarks>
    /// A box holding the keyboard asks for every frame while its caret blinks, so it is never at rest; anything that moves
    /// comes to rest in a handful of seconds.
    /// </remarks>
    public void Draw(float width, float height)
    {
        var frames = 0;
        do
            Draw(width, height, Now + 60);
        while (IsFrameRequested && ++frames < MostFrames);
    }

    /// <summary>
    ///     The maximum number of frames <see cref="Draw(float, float)" /> draws while waiting for everything to come to rest.
    /// </summary>
    public const int MostFrames = 100;

    /// <summary>Lays out and draws one frame at the game tick <paramref name="now" />.</summary>
    public void Draw(float width, float height, int now)
    {
        Now = now;
        _device.BeginFrame();
        _graphics.Begin();

        _element.Measure(_graphics, new Vector2(width, height));
        _element.Arrange(new RectangleF(0f, 0f, width, height));
        _element.Draw(_graphics);
    }
}
