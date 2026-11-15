using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>The game's own screen, and the only place the mod calls the game's screen-space API.</summary>
    public sealed class GameScreenSpace : IScreenSpace
    {
        readonly List<MyScreenSpace.MyScissorScope> _scissors =
            new List<MyScreenSpace.MyScissorScope>();

        readonly Dictionary<string, int> _lineUnits = new Dictionary<string, int>();

        /// <inheritdoc />
        public RectangleF SafeArea => MyScreenSpace.GetSafeGuiArea();

        /// <summary>The size of the camera's viewport, or of the safe area before the session has a camera.</summary>
        public Vector2 ViewportSize
        {
            get
            {
                // The camera's viewport is the window rather than the HUD. There is no camera until the
                // session has finished loading.
                if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
                    return MyAPIGateway.Session.Camera.ViewportSize;

                var area = MyScreenSpace.GetSafeGuiArea();
                return new Vector2(area.Width, area.Height);
            }
        }

        /// <inheritdoc />
        public void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer)
        {
            // Unverified in the game: whether a scope is recorded into a buffered layer's queue or applied when that
            // queue replays.
            // Kept typed: holding the struct scope as IDisposable would box one per push. Disposing a copy is safe, as
            // it reads its two fields and pops.
            _scissors.Add(MyScreenSpace.UsingScissorRectanglePixels(pixels, layer));
        }

        /// <inheritdoc />
        public void PopScissor(MyScreenSpaceLayer layer)
        {
            var last = _scissors.Count - 1;
            if (last < 0)
                return;

            _scissors[last].Dispose();
            _scissors.RemoveAt(last);
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, Rectangle destination, Rectangle? source, Color color, float rotation,
            MyScreenSpaceLayer layer)
        {
            MyScreenSpaceSprite.DrawPixels(material, destination, source, color, rotation, layer);
        }

        /// <inheritdoc />
        public Vector2 TextureSize(MyStringId material)
        {
            return MyScreenSpaceSprite.GetTextureSize(material);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            MyScreenSpaceText.DrawPixels(font, text, position, scale, color, align, maxWidth, layer);
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            return MyScreenSpaceText.MeasurePixels(font, text, scale);
        }

        /// <inheritdoc />
        public bool HasFont(string font)
        {
            return GameFonts.Exists(font);
        }

        /// <inheritdoc />
        /// <remarks>Read from the font's file the first time each font is asked for, and kept.</remarks>
        public int LineUnits(string font)
        {
            var units = 0;
            if (font == null || _lineUnits.TryGetValue(font, out units))
                return units;

            units = Read(font);
            _lineUnits[font] = units;
            return units;
        }

        /// <inheritdoc />
        public bool TryCursorPixels(out Vector2 pixels)
        {
            return MyScreenSpace.TryGetCursorPositionPixels(out pixels);
        }

        /// <inheritdoc />
        public Vector2 NormalizedToPixels(Vector2 position)
        {
            return MyScreenSpace.NormalizedToPixels(position);
        }

        /// <inheritdoc />
        public Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return MyScreenSpace.NormalizedSizeToPixels(size);
        }

        /// <summary>
        ///     Returns the height from the font element in the font file of <paramref name="font" />, or 0 if the file
        ///     gives none.
        /// </summary>
        static int Read(string font)
        {
            var definitions = MyDefinitionManager.Static;
            if (definitions == null)
                return 0;

            var definition = definitions.GetDefinition(
                new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), font)) as MyFontDefinition;
            if (definition == null)
                return 0;

            var path = definition.CompatibilityPath;
            foreach (var resource in definition.Resources)
            {
                path = resource.Path;
                break;
            }

            if (string.IsNullOrEmpty(path))
                return 0;

            string text;
            try
            {
                var context = definition.Context;
                using (var reader = context == null || context.IsBaseGame
                           ? MyAPIGateway.Utilities.ReadFileInGameContent(path)
                           : MyAPIGateway.Utilities.ReadFileInModLocation(path, context.ModItem))
                    text = reader.ReadToEnd();
            }
            catch (Exception)
            {
                return 0;
            }

            return Height(text);
        }

        /// <summary>
        ///     Returns the whole number in the first height attribute after <c>&lt;font</c> in
        ///     <paramref name="text" />, or 0 if there is none.
        /// </summary>
        static int Height(string text)
        {
            var element = text.IndexOf("<font", StringComparison.Ordinal);
            if (element < 0)
                return 0;

            const string attribute = " height=\"";
            var at = text.IndexOf(attribute, element, StringComparison.Ordinal);
            if (at < 0)
                return 0;

            at += attribute.Length;
            var end = text.IndexOf('"', at);
            int height;
            return end > at && int.TryParse(text.Substring(at, end - at), out height) ? height : 0;
        }
    }
}
