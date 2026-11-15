using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Styling;
using VRage.Game.GUI.TextPanel;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Lumel.Screens
{
    /// <summary>An element drawn on a text surface, such as an LCD panel's screen, one frame at a time.</summary>
    /// <remarks>One unit is one of the surface's pixels. A panel takes no input.</remarks>
    public class TextPanel : IDisposable
    {
        readonly List<MySprite> _sprites = new List<MySprite>();
        readonly Stage _stage = new Stage();
        readonly IMyTextSurface _surface;
        readonly SpriteGraphics _graphics;

        /// <summary>Creates a panel drawn on <paramref name="surface" />, reading each picture locator as a material name.</summary>
        public TextPanel(IMyTextSurface surface)
            : this(surface, new MaterialImages(new GameScreenSpace().TextureSize))
        {
        }

        /// <summary>
        ///     Creates a panel drawn on <paramref name="surface" />, using <paramref name="images" /> for pictures and
        ///     <paramref name="clock" /> for timing, or the game's frame count when <paramref name="clock" /> is null.
        /// </summary>
        public TextPanel(IMyTextSurface surface, IImageCatalog images, Func<int> clock = null)
        {
            _surface = surface;
            _graphics = new SpriteGraphics(surface, _sprites, images, clock ?? (() => GameClock.Now));
        }

        /// <summary>
        ///     Gets or sets the element shown, laid out across the surface and placed by its own
        ///     <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        public Element Content
        {
            get { return _stage.Content; }
            set { _stage.Content = value; }
        }

        /// <summary>
        ///     Gets or sets the theme that gives each control shown here a style when it has none, or null for
        ///     <see cref="GameTheme" />.
        /// </summary>
        public ITheme Theme
        {
            get { return _stage.Theme; }
            set { _stage.Theme = value; }
        }

        /// <summary>Gets the resources what the panel shows finds above everything in it, such as a <see cref="Codex" />.</summary>
        public ResourceDictionary Resources => _stage.Resources;

        /// <summary>Gets the drawing the panel writes its sprites with, which measures text the way the surface does.</summary>
        public SpriteGraphics Graphics => _graphics;

        /// <summary>Whether something drawn in the last frame is animating and asked to be drawn again on the next.</summary>
        public bool IsFrameRequested => _graphics.IsFrameRequested;

        /// <summary>Gets the sprites of the last frame drawn.</summary>
        public IReadOnlyList<MySprite> LastFrame => _sprites;

        /// <summary>Lays the content out across the surface and hands the result to the surface as one frame.</summary>
        /// <remarks>
        ///     Lays out only what changed since the last frame, so a frame in which things only move, by their render
        ///     transforms, measures nothing.
        /// </remarks>
        public void Draw()
        {
            Paint();
            Resend();
        }

        /// <summary>
        ///     Lays the content out across the surface and adds the frame's sprites to <paramref name="output" />, for a
        ///     host that hands the surface its frames itself.
        /// </summary>
        /// <remarks>Lays out only what changed since the last frame, as <see cref="Draw()" /> does.</remarks>
        public void Draw(List<MySprite> output)
        {
            Paint();
            output.AddRange(_sprites);
        }

        /// <summary>Hands the surface the last frame drawn again, without drawing, as when the game makes the panel's texture afresh.</summary>
        public void Resend()
        {
            using (var frame = _surface.DrawFrame())
            {
                foreach (var sprite in _sprites)
                    frame.Add(sprite);
            }
        }

        /// <summary>Unloads the content, raising <see cref="Element.Unloaded" /> through it, so it can clean up after itself.</summary>
        public void Dispose()
        {
            _stage.Dispose();
        }

        /// <summary>Lays the content out across the surface and draws it into the frame's sprites.</summary>
        void Paint()
        {
            _graphics.Begin();
            _stage.Draw(_graphics, _graphics.ScreenBounds);
        }
    }
}
