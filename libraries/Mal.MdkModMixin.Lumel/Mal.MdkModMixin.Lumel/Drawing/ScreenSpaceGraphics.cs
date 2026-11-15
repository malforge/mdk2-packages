using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Images;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>Draws on the game's screen at a fixed UI size, scaled to the screen.</summary>
    /// <remarks>
    ///     The game's safe GUI area is 4:3 at the full screen height, so at the game's reference height
    ///     this draws into roughly 1440x1080 whatever the resolution.
    /// </remarks>
    public sealed class ScreenSpaceGraphics : IGraphics
    {
        /// <summary>The height the game's own UI is designed at.</summary>
        /// <remarks>Transcribed from <c>MyGuiManager</c>, whose HUD scale is the screen height over this.</remarks>
        public const float GameReferenceHeight = 1080f;

        /// <summary>Pass as the reference height to work in real window pixels, with no scaling.</summary>
        public const float NativePixels = 0f;

        /// <summary>How far below the requested position the game draws every glyph, in the font's own units.</summary>
        /// <remarks>
        ///     Transcribed from the game's <c>MyRenderGuiConstants.FONT_TOP_SIDE_BEARING</c>, which the renderer
        ///     adds to every glyph whatever the font.
        /// </remarks>
        public const float TopSideBearing = 3f * 23f / 18f;

        /// <summary>The width of the game's own terminal screen, as a fraction of the safe area.</summary>
        /// <remarks>
        ///     Transcribed from the game's terminal screen, 1.0157 by 0.9172 of the safe area; the width drops the overhang
        ///     that the game's soft-edged background needs.
        /// </remarks>
        public const float TerminalWidth = 1f;

        /// <summary>The height of the game's own terminal screen, as a fraction of the safe area.</summary>
        /// <remarks>Transcribed from the game's terminal screen, 1.0157 by 0.9172 of the safe area.</remarks>
        public const float TerminalHeight = 0.9172f;

        // Body, bold, italic, bold italic and code: the faces a paragraph switches between.
        const int RecentFonts = 5;

        readonly ClipStack _clip = new ClipStack();

        readonly Dictionary<string, float> _lines = new Dictionary<string, float>();
        readonly Dictionary<string, float> _lifts = new Dictionary<string, float>();
        readonly Dictionary<string, string> _faces = new Dictionary<string, string>();

        // The fonts last drawn in, by reference, with the face drawn and its line and lift: a paragraph switches font at
        // every bold or italic word, and each switch would otherwise hash the font's name three times, every frame.
        readonly string[] _recentFonts = new string[RecentFonts];
        readonly string[] _recentFaces = new string[RecentFonts];
        readonly float[] _recentLines = new float[RecentFonts];
        readonly float[] _recentLifts = new float[RecentFonts];
        int _recentNext;

        readonly StringBuilder _nothing = new StringBuilder();
        readonly MyScreenSpaceLayer _layer;
        readonly float _referenceHeight;

        readonly IScreenSpace _screenSpace;
        readonly Func<int> _clock;

        readonly List<Vector3> _transforms = new List<Vector3>();

        int _clips;

        string _lastFont;

        string _lastFace;

        float _lastLine;

        float _lastLift;

        float _fontScale = 1f;

        Vector2 _origin;

        float _scale = 1f;

        float _transformScale = 1f;
        Vector2 _transformMove;
        bool _transformed;

        /// <summary>
        ///     Creates graphics that draw onto <paramref name="layer" />, using <see cref="LumelMaterials.Fill" /> for
        ///     flat fills and <paramref name="images" /> for pictures, timed by <paramref name="clock" />, and scaled
        ///     to the height <paramref name="referenceHeight" /> that a layout is written against.
        /// </summary>
        public ScreenSpaceGraphics(IScreenSpace screenSpace, MyScreenSpaceLayer layer, IImageCatalog images, Func<int> clock,
            float referenceHeight = GameReferenceHeight)
        {
            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            _screenSpace = screenSpace;
            _clock = clock;
            _layer = layer;
            _referenceHeight = referenceHeight;
            Images = images;
            Begin();
        }

        /// <inheritdoc />
        public IImageCatalog Images { get; }

        /// <inheritdoc />
        public bool SupportsSourceRectangles => true;

        /// <inheritdoc />
        public int Now => _clock();

        /// <summary>Whether something drawn since <see cref="Begin" /> has asked for the next frame.</summary>
        public bool IsFrameRequested { get; private set; }

        /// <inheritdoc />
        public void RequestFrame()
        {
            IsFrameRequested = true;
        }

        /// <summary>The whole window in UI units, which extends past the size on a screen wider than 4:3.</summary>
        /// <remarks>The game's own cursor reaches the edges of this, not of <see cref="Size" />.</remarks>
        public RectangleF ScreenBounds { get; private set; }

        /// <inheritdoc />
        public Vector2 Size { get; private set; }

        /// <summary>
        ///     Restricts drawing to <paramref name="bounds" />, intersected with the clip already in force.
        /// </summary>
        /// <remarks>A scissor applies only to the layer it is pushed on.</remarks>
        public void PushClip(RectangleF bounds)
        {
            _clip.Push(Transformed(bounds));
            _screenSpace.PushScissor(ToPixels(_clip.Current), _layer);
            _clips++;
        }

        /// <summary>Removes the innermost clip and restores the one around it.</summary>
        public void PopClip()
        {
            if (_clips > 0)
            {
                _screenSpace.PopScissor(_layer);
                _clips--;
            }

            _clip.Pop();
        }

        /// <inheritdoc />
        public void PushTransform(RenderTransform transform, Vector2 origin)
        {
            // Held as a scale and a move, p landing at p * scale + move, with the ones around it kept flat in a reused
            // list, because this runs on every frame's drawing path.
            _transforms.Add(new Vector3(_transformScale, _transformMove.X, _transformMove.Y));

            // The element's own transform, about its origin, as a scale and a move, and then the one around it.
            var scale = transform.Scale;
            var move = origin - origin * scale + transform.Translate;

            _transformMove = move * _transformScale + _transformMove;
            _transformScale *= scale;
            _transformed = _transformScale != 1f || _transformMove.X != 0f || _transformMove.Y != 0f;
        }

        /// <inheritdoc />
        public void PopTransform()
        {
            var last = _transforms.Count - 1;
            if (last < 0)
                return;

            var held = _transforms[last];
            _transforms.RemoveAt(last);

            _transformScale = held.X;
            _transformMove = new Vector2(held.Y, held.Z);
            _transformed = _transformScale != 1f || _transformMove.X != 0f || _transformMove.Y != 0f;
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            if (destination.Width <= 0f || destination.Height <= 0f)
                return;

            _screenSpace.DrawSprite(material, AtLeastOnePixel(ToPixels(Transformed(destination))), null, color, rotation, _layer);
        }

        /// <inheritdoc />
        /// <remarks>Placed by its edges, so the parts of one picture drawn side by side meet without a gap or an overlap.</remarks>
        public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color)
        {
            if (destination.Width <= 0f || destination.Height <= 0f || source.Width <= 0 || source.Height <= 0)
                return;

            _screenSpace.DrawSprite(material, AtLeastOnePixel(EdgesToPixels(Transformed(destination))), source, color, 0f, _layer);
        }

        /// <inheritdoc />
        public void Fill(RectangleF destination, Color color, float rotation = 0f)
        {
            DrawSprite(LumelMaterials.Fill, destination, color, rotation);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color)
        {
            DrawText(font, text, position, size, color, TextAlign.Left);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color,
            TextAlign align)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Always the top, because a position in the layout is the top of the run.
            var placed = align == TextAlign.Right
                ? MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
                : MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            if (_transformed)
            {
                position = position * _transformScale + _transformMove;
                size *= _transformScale;
            }

            var scale = ScaleOf(font, size);
            position.Y -= size * _lastLift;

            _screenSpace.DrawText(_lastFace, text, position * _scale + _origin, scale * _fontScale, color,
                placed, float.PositiveInfinity, _layer);
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float size)
        {
            var scale = ScaleOf(font, size);
            return _screenSpace.MeasureText(_lastFace, text, scale * _fontScale) / _scale;
        }

        /// <summary>Samples the safe area and clears any clip left over.</summary>
        /// <remarks>Called at the top of every draw.</remarks>
        public void Begin()
        {
            IsFrameRequested = false;
            var area = _screenSpace.SafeArea;

            // What the game is already applying to every font scale it is handed.
            var gameScale = area.Height > 0f ? area.Height / GameReferenceHeight : 1f;

            _origin = new Vector2(area.X, area.Y);

            var scale = _referenceHeight > 0f && area.Height > 0f ? area.Height / _referenceHeight : 1f;
            var fontScale = gameScale > 0f ? scale / gameScale : 1f;

            if (scale != _scale || fontScale != _fontScale)
            {
                _lines.Clear();
                _lastFont = null;
                Array.Clear(_recentFonts, 0, RecentFonts);
            }

            _scale = scale;
            _fontScale = fontScale;
            Size = new Vector2(area.Width / _scale, area.Height / _scale);

            // Starts at a negative X on anything wider than 4:3.
            var viewport = _screenSpace.ViewportSize;

            ScreenBounds = new RectangleF(
                -_origin.X / _scale,
                -_origin.Y / _scale,
                viewport.X / _scale,
                viewport.Y / _scale);

            CloseScissors();
            _clip.Clear();

            _transforms.Clear();
            _transformScale = 1f;
            _transformMove = Vector2.Zero;
            _transformed = false;
        }

        /// <summary>
        ///     Returns the scale that draws <paramref name="font" /> with lines <paramref name="size" /> tall, and
        ///     records its face as the last face drawn.
        /// </summary>
        /// <remarks>Measured once per font.</remarks>
        float ScaleOf(string font, float size)
        {
            if (!ReferenceEquals(font, _lastFont))
            {
                _lastFont = font;
                if (!Recall(font))
                {
                    var face = Face(font);
                    float line;
                    if (!_lines.TryGetValue(font, out line))
                    {
                        line = _screenSpace.MeasureText(face, _nothing, _fontScale).Y / _scale;
                        _lines[font] = line;
                    }

                    _lastFace = face;
                    _lastLine = line;
                    _lastLift = Lift(face);

                    _recentFonts[_recentNext] = font;
                    _recentFaces[_recentNext] = face;
                    _recentLines[_recentNext] = _lastLine;
                    _recentLifts[_recentNext] = _lastLift;
                    _recentNext = (_recentNext + 1) % RecentFonts;
                }
            }

            return _lastLine > 0f ? size / _lastLine : 0f;
        }

        /// <summary>
        ///     Takes the face, line and lift of <paramref name="font" /> from the fonts last drawn in, and returns true
        ///     if it was one of them.
        /// </summary>
        bool Recall(string font)
        {
            for (var i = 0; i < RecentFonts; i++)
            {
                if (!ReferenceEquals(_recentFonts[i], font))
                    continue;

                _lastFace = _recentFaces[i];
                _lastLine = _recentLines[i];
                _lastLift = _recentLifts[i];
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns the face <paramref name="font" /> is drawn in: the font itself if the game has it, otherwise the
        ///     game's own face.
        /// </summary>
        /// <remarks>Asked of the game once per font.</remarks>
        string Face(string font)
        {
            if (font == null)
                return null;

            string face;
            if (_faces.TryGetValue(font, out face))
                return face;

            face = _screenSpace.HasFont(font) ? font : GameFonts.Default;
            _faces[font] = face;
            return face;
        }

        /// <summary>
        ///     Returns the game's top side bearing for <paramref name="font" />, as a fraction of one of its lines.
        /// </summary>
        /// <remarks>
        ///     The game scales the bearing and the line alike, so a run <c>size</c> tall is drawn <c>size</c> times this
        ///     too low. Kept per font, since reading one opens its file.
        /// </remarks>
        float Lift(string font)
        {
            if (font == null)
                return 0f;

            float lift;
            if (_lifts.TryGetValue(font, out lift))
                return lift;

            var units = _screenSpace.LineUnits(font);
            lift = units > 0 ? TopSideBearing / units : 0f;
            _lifts[font] = lift;
            return lift;
        }

        /// <summary>Ends every open scissor, innermost first.</summary>
        /// <remarks>A clip left open by an abandoned draw would otherwise narrow the next frame.</remarks>
        void CloseScissors()
        {
            while (_clips > 0)
            {
                _screenSpace.PopScissor(_layer);
                _clips--;
            }
        }

        /// <summary>Returns the bounds of <paramref name="area" /> this frame, in UI units.</summary>
        /// <remarks>Read after <see cref="Begin" />, since a change of resolution or UI scale moves it.</remarks>
        public RectangleF BoundsOf(ScreenArea area)
        {
            if (area == ScreenArea.Window)
                return ScreenBounds;

            var width = Size.X * TerminalWidth;
            var height = Size.Y * TerminalHeight;
            return new RectangleF((Size.X - width) * 0.5f, (Size.Y - height) * 0.5f, width, height);
        }

        /// <summary>Converts a point from window pixels to UI units.</summary>
        public Vector2 FromPixels(Vector2 pixels)
        {
            return _scale > 0f ? (pixels - _origin) / _scale : pixels;
        }

        /// <summary>Converts a size from window pixels to UI units.</summary>
        public Vector2 FromPixelSize(Vector2 pixels)
        {
            return _scale > 0f ? pixels / _scale : pixels;
        }

        /// <inheritdoc />
        public Vector2 ToDevicePixels(Vector2 point)
        {
            return (point * _transformScale + _transformMove) * _scale + _origin;
        }

        /// <inheritdoc />
        public Vector2 FromDevicePixels(Vector2 pixels)
        {
            // Scaled to nothing, every point lands on the same pixel, and the origin is one of them.
            if (_transformScale == 0f)
                return Vector2.Zero;

            return (FromPixels(pixels) - _transformMove) / _transformScale;
        }

        /// <summary>Returns where <paramref name="bounds" /> is drawn under the transform in force.</summary>
        RectangleF Transformed(RectangleF bounds)
        {
            if (!_transformed)
                return bounds;

            return new RectangleF(
                bounds.X * _transformScale + _transformMove.X,
                bounds.Y * _transformScale + _transformMove.Y,
                bounds.Width * _transformScale,
                bounds.Height * _transformScale);
        }

        /// <summary>
        ///     Converts a rectangle from UI units to window pixels by rounding each edge, so rectangles that meet in UI
        ///     units meet in pixels.
        /// </summary>
        Rectangle EdgesToPixels(RectangleF bounds)
        {
            var left = Pixel(bounds.X * _scale + _origin.X);
            var top = Pixel(bounds.Y * _scale + _origin.Y);
            var right = Pixel((bounds.X + bounds.Width) * _scale + _origin.X);
            var bottom = Pixel((bounds.Y + bounds.Height) * _scale + _origin.Y);

            return new Rectangle(left, top, right - left, bottom - top);
        }

        /// <summary>Returns <paramref name="target" />, at least one pixel wide and tall, so nothing thin vanishes.</summary>
        static Rectangle AtLeastOnePixel(Rectangle target)
        {
            return new Rectangle(target.X, target.Y, Math.Max(1, target.Width), Math.Max(1, target.Height));
        }

        /// <summary>
        ///     Returns <paramref name="at" /> rounded to the nearest whole pixel, with a half rounding up.
        /// </summary>
        static int Pixel(float at)
        {
            // Not Math.Round, which rounds a half to the nearest even number.
            return (int)Math.Floor(at + 0.5f);
        }

        /// <summary>Converts a rectangle from UI units to window pixels.</summary>
        /// <remarks>Position and size are rounded apart, so the same size is the same number of pixels wherever it sits.</remarks>
        Rectangle ToPixels(RectangleF bounds)
        {
            // Not Math.Round, which rounds a half to the nearest even number.
            var left = (int)Math.Floor(bounds.X * _scale + _origin.X + 0.5f);
            var top = (int)Math.Floor(bounds.Y * _scale + _origin.Y + 0.5f);
            var width = (int)Math.Floor(bounds.Width * _scale + 0.5f);
            var height = (int)Math.Floor(bounds.Height * _scale + 0.5f);

            return new Rectangle(left, top, width, height);
        }
    }
}