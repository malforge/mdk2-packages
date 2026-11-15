using System;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Styling;
using Sandbox.ModAPI;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>
    ///     The three layers of HUD a workspace draws over the world, taking no input: under the game's HUD, over it, and
    ///     over everything.
    /// </summary>
    public class Hud : IDisposable
    {
        readonly Stage _below = new Stage();
        readonly Stage _above = new Stage();
        readonly Stage _aboveEverything = new Stage();
        readonly IScreenSpace _screen;
        readonly IImageCatalog _images;
        readonly Func<int> _clock;
        ScreenSpaceGraphics[] _graphics;

        /// <summary>
        ///     Creates layers drawn on <paramref name="screen" />, using <paramref name="images" /> for pictures and
        ///     <paramref name="clock" /> for timing, or drawn only by <see cref="Draw(IGraphics, RectangleF, RectangleF)" /> where
        ///     <paramref name="screen" /> is null.
        /// </summary>
        public Hud(IScreenSpace screen, IImageCatalog images, Func<int> clock)
        {
            _screen = screen;
            _images = images;
            _clock = clock;
        }

        /// <summary>
        ///     Gets or sets the element drawn over the world and under the game's HUD, placed by its own
        ///     <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        public Element BelowHud
        {
            get { return _below.Content; }
            set { _below.Content = value; }
        }

        /// <summary>
        ///     Gets or sets the element drawn over the game's HUD and under open screens such as the terminal, placed by its
        ///     own <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        /// <remarks>Shown while the game's HUD is hidden too.</remarks>
        public Element AboveHud
        {
            get { return _above.Content; }
            set { _above.Content = value; }
        }

        /// <summary>
        ///     Gets or sets the element drawn over everything, open screens and the game's tooltips included, placed by its
        ///     own <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        public Element AboveEverything
        {
            get { return _aboveEverything.Content; }
            set { _aboveEverything.Content = value; }
        }

        /// <summary>
        ///     Gets or sets the part of the game's screen <see cref="BelowHud" /> is laid out in: the whole screen, the
        ///     default, as the game's own HUD is, or the size of the game's terminal in the middle.
        /// </summary>
        public ScreenArea BelowHudArea { get; set; } = ScreenArea.Window;

        /// <summary>
        ///     Gets or sets the part of the game's screen <see cref="AboveHud" /> is laid out in: the whole screen, the
        ///     default, as the game's own HUD is, or the size of the game's terminal in the middle.
        /// </summary>
        public ScreenArea AboveHudArea { get; set; } = ScreenArea.Window;

        /// <summary>
        ///     Gets or sets the part of the game's screen <see cref="AboveEverything" /> is laid out in: the whole
        ///     screen, the default, as the game's own HUD is, or the size of the game's terminal in the middle.
        /// </summary>
        public ScreenArea AboveEverythingArea { get; set; } = ScreenArea.Window;

        /// <summary>Sets the resources what the layers show finds above everything in it.</summary>
        public ResourceDictionary Resources
        {
            set
            {
                _below.Resources = value;
                _above.Resources = value;
                _aboveEverything.Resources = value;
            }
        }

        /// <summary>Sets the theme that gives each control on the layers a style when it has none, or null for <see cref="GameTheme" />.</summary>
        public ITheme Theme
        {
            set
            {
                _below.Theme = value;
                _above.Theme = value;
                _aboveEverything.Theme = value;
            }
        }

        /// <summary>Draws each layer that shows anything on its layer of the game's screen, laid out in its area of it.</summary>
        /// <remarks>Throws for layers made with no screen.</remarks>
        public void Draw()
        {
            if (_screen == null)
                throw new InvalidOperationException("Layers made with no screen have nowhere to draw; draw them on graphics of their own.");

            if (_graphics == null)
            {
                _graphics = new[]
                {
                    new ScreenSpaceGraphics(_screen, MyScreenSpaceLayer.BelowHud, _images, _clock),
                    new ScreenSpaceGraphics(_screen, MyScreenSpaceLayer.AboveHud, _images, _clock),
                    new ScreenSpaceGraphics(_screen, MyScreenSpaceLayer.AboveEverything, _images, _clock)
                };
            }

            Draw(_below, _graphics[0], BelowHudArea);
            Draw(_above, _graphics[1], AboveHudArea);
            Draw(_aboveEverything, _graphics[2], AboveEverythingArea);
        }

        /// <summary>
        ///     Draws the three layers on <paramref name="g" />, bottom first, each in its area: the whole of
        ///     <paramref name="screen" />, or the terminal's <paramref name="terminal" /> within it.
        /// </summary>
        public void Draw(IGraphics g, RectangleF screen, RectangleF terminal)
        {
            _below.Draw(g, BelowHudArea == ScreenArea.Terminal ? terminal : screen);
            _above.Draw(g, AboveHudArea == ScreenArea.Terminal ? terminal : screen);
            _aboveEverything.Draw(g, AboveEverythingArea == ScreenArea.Terminal ? terminal : screen);
        }

        /// <summary>Unloads the layers, raising <see cref="Element.Unloaded" /> through everything they show.</summary>
        public void Dispose()
        {
            _below.Dispose();
            _above.Dispose();
            _aboveEverything.Dispose();
        }

        /// <summary>Lays out and draws <paramref name="stage" /> in <paramref name="area" /> of <paramref name="g" />, where it shows anything.</summary>
        static void Draw(Stage stage, ScreenSpaceGraphics g, ScreenArea area)
        {
            if (stage.Content == null)
                return;

            g.Begin();
            stage.Draw(g, g.BoundsOf(area));
        }
    }
}
