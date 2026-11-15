using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Images;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Lumel.Drawing
{
    /// <summary>Lumel's drawing, recorded as the sprites of a text panel to be drawn as one frame.</summary>
    /// <remarks>
    ///     A UI unit is one of the panel's pixels, counted from the corner of the part of its texture the screen shows.
    ///     Text is measured the panel's own way, so lines break where the panel would draw them.
    /// </remarks>
    public class SpriteGraphics : IGraphics
    {
        const string Square = "SquareSimple";
        const string Circle = "Circle";
        const string Ring = "CircleHollow";

        readonly IMyTextSurface _surface;
        readonly List<MySprite> _sprites;
        readonly Dictionary<string, float> _lines = new Dictionary<string, float>();
        readonly Dictionary<string, string> _faces = new Dictionary<string, string>();
        readonly List<string> _drawable = new List<string>();
        readonly StringBuilder _letter = new StringBuilder("A");
        readonly ClipStack _clips = new ClipStack();
        readonly List<RenderTransform> _transforms = new List<RenderTransform>();
        readonly List<Vector2> _origins = new List<Vector2>();

        Vector2 _corner;
        readonly Func<int> _clock;

        bool _drawableRead;

        /// <summary>
        ///     Creates graphics for <paramref name="surface" /> that write into <paramref name="sprites" />, find
        ///     pictures in <paramref name="images" />, and are timed by <paramref name="clock" />.
        /// </summary>
        public SpriteGraphics(IMyTextSurface surface, List<MySprite> sprites, IImageCatalog images, Func<int> clock)
        {
            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            _clock = clock;
            _surface = surface;
            _sprites = sprites;
            Images = images;
        }

        /// <inheritdoc />
        public Vector2 Size { get; private set; }

        /// <inheritdoc />
        public RectangleF ScreenBounds => new RectangleF(Vector2.Zero, Size);

        /// <inheritdoc />
        public IImageCatalog Images { get; }

        /// <inheritdoc />
        public bool SupportsSourceRectangles => false;

        /// <inheritdoc />
        public int Now => _clock();

        /// <summary>Whether something drawn since <see cref="Begin" /> has asked for the next frame.</summary>
        public bool IsFrameRequested { get; private set; }

        /// <inheritdoc />
        public void RequestFrame()
        {
            IsFrameRequested = true;
        }

        /// <summary>Starts a frame: discards the sprites recorded so far and reads the screen's size again.</summary>
        public void Begin()
        {
            IsFrameRequested = false;
            _sprites.Clear();
            _clips.Clear();
            _transforms.Clear();
            _origins.Clear();
            Size = _surface.SurfaceSize;
            _corner = (_surface.TextureSize - _surface.SurfaceSize) * 0.5f;
        }

        /// <summary>
        ///     Returns the face <paramref name="font" /> is drawn in: the font itself if the panel can draw it,
        ///     otherwise the game's own face.
        /// </summary>
        /// <remarks>The panel is asked for the fonts it can draw once, and each font is looked up among them once.</remarks>
        string Face(string font)
        {
            if (font == null)
                return null;

            string face;
            if (_faces.TryGetValue(font, out face))
                return face;

            if (!_drawableRead)
            {
                _surface.GetFonts(_drawable);
                _drawableRead = true;
            }

            face = _drawable.Contains(font) ? font : GameFonts.Default;
            _faces.Add(font, face);
            return face;
        }

        /// <summary>
        ///     Returns the height of one line of <paramref name="font" /> at a panel scale of 1, in panel pixels.
        /// </summary>
        public float LineHeight(string font)
        {
            float height;
            if (_lines.TryGetValue(font, out height))
                return height;

            height = _surface.MeasureStringInPixels(_letter, font, 1f).Y;
            if (height <= 0f)
                height = 1f;

            _lines.Add(font, height);
            return height;
        }

        /// <inheritdoc />
        public void PushClip(RectangleF bounds)
        {
            _clips.Push(Placed(bounds));
            Clip(_clips.Current);
        }

        /// <inheritdoc />
        public void PopClip()
        {
            if (!_clips.HasClip)
                return;

            _clips.Pop();
            if (_clips.HasClip)
                Clip(_clips.Current);
            else
                _sprites.Add(MySprite.CreateClearClipRect());
        }

        /// <inheritdoc />
        public void PushTransform(RenderTransform transform, Vector2 origin)
        {
            _transforms.Add(transform);
            _origins.Add(origin);
        }

        /// <inheritdoc />
        public void PopTransform()
        {
            if (_transforms.Count == 0)
                return;

            _transforms.RemoveAt(_transforms.Count - 1);
            _origins.RemoveAt(_origins.Count - 1);
        }

        /// <inheritdoc />
        /// <remarks>Lumel's own fill, disc and ring are drawn as the panel's own square, circle and hollow circle.</remarks>
        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            var placed = Placed(destination);
            _sprites.Add(new MySprite(SpriteType.TEXTURE, Sprite(material), placed.Center, placed.Size, color, null, TextAlignment.CENTER, rotation));
        }

        /// <summary>
        ///     Returns the name of the panel's sprite for <paramref name="material" />: the panel's own shape for each of
        ///     Lumel's materials that has one, and otherwise the material's own name.
        /// </summary>
        static string Sprite(MyStringId material)
        {
            if (material == LumelMaterials.Fill)
                return Square;

            if (material == LumelMaterials.Disc)
                return Circle;

            return material == LumelMaterials.Ring ? Ring : material.String;
        }

        /// <inheritdoc />
        /// <remarks>A panel draws a sprite whole, so the part is drawn as the whole picture stretched over the destination.</remarks>
        public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color)
        {
            DrawSprite(material, destination, color, 0f);
        }

        /// <inheritdoc />
        public void Fill(RectangleF destination, Color color, float rotation = 0f)
        {
            var placed = Placed(destination);
            _sprites.Add(new MySprite(SpriteType.TEXTURE, Square, placed.Center, placed.Size, color, null, TextAlignment.CENTER, rotation));
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color)
        {
            DrawText(font, text, position, size, color, TextAlign.Left);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align)
        {
            var face = Face(font);
            if (face == null)
                return;

            var at = Placed(position);
            var scale = Scaled(size) / LineHeight(face);
            _sprites.Add(new MySprite(SpriteType.TEXT, text, at, null, color, face,
                align == TextAlign.Right ? TextAlignment.RIGHT : TextAlignment.LEFT, scale));
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float size)
        {
            var face = Face(font);
            if (face == null)
                return Vector2.Zero;

            var scale = size / LineHeight(face);
            return _surface.MeasureStringInPixels(text, face, scale);
        }

        /// <inheritdoc />
        public Vector2 ToDevicePixels(Vector2 point)
        {
            return Placed(point);
        }

        /// <inheritdoc />
        public Vector2 FromDevicePixels(Vector2 pixels)
        {
            var point = pixels - _corner;
            for (var i = 0; i < _transforms.Count; i++)
                point = _transforms[i].Inverse(point, _origins[i]);

            return point;
        }

        void Clip(RectangleF clip)
        {
            _sprites.Add(MySprite.CreateClipRect(new Rectangle((int)clip.X, (int)clip.Y, (int)(clip.Width + 0.5f), (int)(clip.Height + 0.5f))));
        }

        /// <summary>Returns <paramref name="point" /> in the panel's texture, after every transform in force.</summary>
        Vector2 Placed(Vector2 point)
        {
            for (var i = _transforms.Count - 1; i >= 0; i--)
                point = _transforms[i].Apply(point, _origins[i]);

            return point + _corner;
        }

        RectangleF Placed(RectangleF area)
        {
            var corner = Placed(area.Position);
            return new RectangleF(corner, area.Size * Grown());
        }

        float Scaled(float size)
        {
            return size * Grown();
        }

        float Grown()
        {
            var scale = 1f;
            foreach (var transform in _transforms)
                scale *= transform.Scale;

            return scale;
        }
    }
}
