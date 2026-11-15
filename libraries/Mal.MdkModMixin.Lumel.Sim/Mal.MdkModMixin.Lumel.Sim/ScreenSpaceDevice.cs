using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lumel.Drawing;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Lumel.Sim
{
    /// <summary>A screen that records what is drawn on it.</summary>
    /// <remarks>
    /// The mod's own drawing runs above this line - the arithmetic, the clipping, the letterboxing -
    /// and every call it makes to the game's screen lands here as an ordered command instead. That
    /// list is what a reference frame is.
    /// </remarks>
    public sealed class ScreenSpaceDevice : IScreenSpace
    {
        readonly List<DrawCommand> _commands = new List<DrawCommand>();
        readonly List<Rectangle> _scissors = new List<Rectangle>();
        readonly Dictionary<string, Vector2> _textureSizes = new Dictionary<string, Vector2>();

        /// <summary>
        ///     Creates a 1920 by 1080 window whose 4:3 safe area is at the left, with pictures from
        ///     <paramref name="content" />.
        /// </summary>
        public ScreenSpaceDevice(SimContent content)
        {
            Content = content;
            SafeArea = new RectangleF(0f, 0f, 1440f, 1080f);
            ViewportSize = new Vector2(1920f, 1080f);
        }

        /// <summary>
        ///     Gets or sets the 4:3 box at full screen height that the game lays its own UI out in; set once per frame
        ///     by whoever owns the window.
        /// </summary>
        public RectangleF SafeArea { get; set; }

        /// <summary>
        ///     Gets or sets the whole window, which is wider than the safe area on anything but a 4:3 monitor.
        /// </summary>
        public Vector2 ViewportSize { get; set; }

        /// <summary>Gets the content the screen's pictures come from.</summary>
        public SimContent Content { get; }

        /// <summary>Gets or sets how wide and tall text is in each face, or null to measure nothing.</summary>
        public IFontMetrics? Fonts { get; set; }

        /// <summary>Gets this frame's calls, in the order they were made.</summary>
        public IList<DrawCommand> Commands => _commands;

        /// <summary>Clears the last frame's calls and clips to start a new frame.</summary>
        public void BeginFrame()
        {
            _commands.Clear();
            _scissors.Clear();
        }

        /// <summary>Gets how many times anything has asked for the width of a string.</summary>
        /// <remarks>
        /// <b>For measuring, and it is not idle curiosity.</b> Measuring text is the dearest thing the
        /// layout does and the one whose cost no screenshot can show: a layout that re-measures every
        /// label on every frame draws exactly the right picture. The only honest proof is a count, so
        /// the harness keeps one.
        /// </remarks>
        public int Measured { get; private set; }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder? text, float scale)
        {
            if (Fonts == null || text == null)
                return Vector2.Zero;

            Measured++;

            // The real one measures the builder without unpacking it, which is the reason a builder is held
            // at all. Here the string is fine: nothing in this process is running inside a game's frame.
            return Fonts.MeasurePixels(font, text.ToString(), scale);
        }

        /// <summary>
        ///     Gets the fonts this stand-in for the game has not loaded, such as when the mod that provides one is
        ///     missing.
        /// </summary>
        // Filled by whoever drives the stand-in, such as Lumel.Tests through Easel, which a mod's solution does not hold.
        // ReSharper disable once CollectionNeverUpdated.Global
        public ISet<string> MissingFonts { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool HasFont(string font)
        {
            return !MissingFonts.Contains(font);
        }

        /// <inheritdoc />
        public int LineUnits(string font)
        {
            return Fonts?.LineUnits(font) ?? 0;
        }

        /// <summary>Returns the height of a line in pixels, the unit the screen reports in.</summary>
        public float LineHeight(string font, float scale)
        {
            return Fonts?.LineHeightPixels(font, scale) ?? 0f;
        }

        /// <summary>Always returns false.</summary>
        /// <remarks>
        /// The harness drives the editor through its own window and its own recorded frames, and the
        /// only thing that reads this is the game's screen host, which the harness does not run.
        /// </remarks>
        public bool TryCursorPixels(out Vector2 pixels)
        {
            pixels = Vector2.Zero;
            return false;
        }

        /// <inheritdoc />
        public Vector2 NormalizedToPixels(Vector2 position)
        {
            return new Vector2(position.X * ViewportSize.X, position.Y * ViewportSize.Y);
        }

        /// <inheritdoc />
        public Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return new Vector2(size.X * ViewportSize.X, size.Y * ViewportSize.Y);
        }

        /// <summary>
        ///     Returns the size of the picture of <paramref name="material" />, read from the picture the canvas would
        ///     draw.
        /// </summary>
        /// <remarks>Each is read once; a machine with neither the game nor the cache knows none.</remarks>
        public Vector2 TextureSize(MyStringId material)
        {
            string name = material.String;
            Vector2 size;
            if (_textureSizes.TryGetValue(name, out size))
                return size;

            string png = Content.PictureFor(name, out _);
            size = png == null || !png.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? Vector2.Zero : PictureSize(png);
            _textureSizes.Add(name, size);
            return size;
        }

        /// <summary>Returns the width and height given by a PNG's header.</summary>
        static Vector2 PictureSize(string png)
        {
            using (var file = File.OpenRead(png))
            {
                var header = new byte[24];
                if (file.Read(header, 0, header.Length) < header.Length)
                    return Vector2.Zero;

                return new Vector2(BigEndian(header, 16), BigEndian(header, 20));
            }
        }

        static int BigEndian(byte[] bytes, int at)
        {
            return bytes[at] << 24 | bytes[at + 1] << 16 | bytes[at + 2] << 8 | bytes[at + 3];
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, Rectangle destination, Rectangle? source, Color color, float rotation,
            MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand
            {
                Kind = DrawKind.Sprite,
                Layer = layer,
                Material = material,
                Destination = destination,
                Source = source,
                Color = color,
                Rotation = rotation
            });
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand
            {
                Kind = DrawKind.Text,
                Layer = layer,
                Font = font,
                Text = text,
                Position = Placed(font, text, scale, position, align),
                Scale = scale,
                Color = color,
                Align = align,
                MaxWidth = maxWidth
            });
        }

        /// <summary>Returns where a run actually starts once its alignment has been applied.</summary>
        /// <remarks>
        /// <b>Resolved here, where the game resolves it.</b> The game is handed an alignment and a point
        /// and works out the rest inside its own text drawing, so this stands in the same place - and
        /// then <see cref="DrawCommand.Position" /> goes on meaning what every reader of a frame already
        /// takes it to mean, which is where the letters begin. A renderer, a dump and a test would
        /// otherwise each have to undo the alignment, and each be a place to get it wrong.
        /// <para>
        /// The width is this device's, so it is as good as <see cref="Fonts" /> is: exact against the
        /// game's own metrics, and a few percent out against a stand-in face. A right-aligned run is
        /// therefore placed no better than a question about whether text fits, which is the rule the
        /// rest of this harness already lives by.
        /// </para>
        /// </remarks>
        Vector2 Placed(string font, string text, float scale, Vector2 position, MyGuiDrawAlignEnum align)
        {
            if (Fonts == null || string.IsNullOrEmpty(text) || !Rightward(align))
                return position;

            return new Vector2(position.X - Fonts.MeasurePixels(font, text, scale).X, position.Y);
        }

        /// <summary>
        ///     Returns true if <paramref name="align" /> puts the point at the right-hand end of the run.
        /// </summary>
        static bool Rightward(MyGuiDrawAlignEnum align)
        {
            return align == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
                || align == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
                || align == MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
        }

        /// <summary>Starts a scissor that clips to <paramref name="pixels" /> on <paramref name="layer" />.</summary>
        /// <remarks>
        /// The stack lives here because that is where the game keeps its own, and because a clip still
        /// has to reach the frame as an ordered command: the renderer replays a list and <c>--dump</c>
        /// reads one, and neither should have to know a stack exists.
        /// </remarks>
        public void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer)
        {
            _scissors.Add(pixels);
            SetClip(pixels, layer);
        }

        /// <summary>
        ///     Ends the innermost scissor on <paramref name="layer" />, restoring its parent's rather than clearing the
        ///     clip outright.
        /// </summary>
        public void PopScissor(MyScreenSpaceLayer layer)
        {
            if (_scissors.Count == 0)
                return;

            _scissors.RemoveAt(_scissors.Count - 1);

            if (_scissors.Count > 0)
                SetClip(_scissors[_scissors.Count - 1], layer);
            else
                ClearClip(layer);
        }

        void SetClip(Rectangle rect, MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand { Kind = DrawKind.SetClip, Layer = layer, Clip = rect });
        }

        void ClearClip(MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand { Kind = DrawKind.ClearClip, Layer = layer });
        }
    }
}
