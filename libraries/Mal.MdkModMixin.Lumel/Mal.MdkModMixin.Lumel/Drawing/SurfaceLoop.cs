using System;
using System.Collections.Generic;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Lumel.Drawing
{
    /// <summary>
    ///     Draws text surfaces while something on them is animating, every other tick, by handing each frame straight
    ///     to its block's screen; a surface's own app is drawn at most six times a second.
    /// </summary>
    /// <remarks>
    /// On a client only, where screens are drawn. A surface leaves the loop once its frame says nothing moves; the host runs
    /// the loop on every tick only while <see cref="IsBusy" />.
    /// </remarks>
    public sealed class SurfaceLoop
    {
        /// <summary>The number of ticks between frames, which gives thirty frames a second.</summary>
        public const int Interval = 2;

        readonly List<ILiveSurface> _live = new List<ILiveSurface>();
        readonly List<MySprite> _frame = new List<MySprite>();

        /// <summary>Whether any surface is animating.</summary>
        public bool IsBusy => _live.Count > 0;

        /// <summary>
        ///     Draws <paramref name="surface" /> on every frame until its frame reports that nothing is animating.
        /// </summary>
        public void Add(ILiveSurface surface)
        {
            if (!_live.Contains(surface))
                _live.Add(surface);
        }

        /// <summary>Stops drawing <paramref name="surface" />.</summary>
        public void Remove(ILiveSurface surface)
        {
            _live.Remove(surface);
        }

        /// <summary>
        ///     Draws the frame of every animating surface for <paramref name="tick" />, if it is a frame tick.
        /// </summary>
        public void Run(int tick)
        {
            if (tick % Interval != 0)
                return;

            for (var i = _live.Count - 1; i >= 0; i--)
            {
                var surface = _live[i];
                _frame.Clear();
                var moving = surface.Frame(_frame, tick);
                Show(surface.Block, surface.Surface, _frame);
                if (!moving)
                    _live.RemoveAt(i);
            }
        }

        /// <summary>
        ///     Hands <paramref name="sprites" /> straight to the screen of <paramref name="block" /> that
        ///     <paramref name="surface" /> is.
        /// </summary>
        static void Show(IMyCubeBlock block, IMyTextSurface surface, List<MySprite> sprites)
        {
            // A projection or a grid still streaming in has no screen to draw on yet, and throws where it is drawn on.
            var cube = block as MyCubeBlock;
            var screen = cube?.Render as MyRenderComponentScreenAreas;
            var area = Area(block, surface);
            if (screen == null || cube.CubeGrid == null || cube.CubeGrid.Physics == null || area < 0)
                return;

            var size = surface.SurfaceSize;
            var aspect = size.X > size.Y ? new Vector2(1f, size.Y / size.X) : new Vector2(size.X / size.Y, 1f);
            try
            {
                screen.RenderSpritesToTexture(area, sprites, (Vector2I)surface.TextureSize, aspect, surface.ScriptBackgroundColor, 0);
            }
            catch (Exception)
            {
                // A screen whose areas the game fills in a few ticks after the block arrives throws until it has; the next
                // frame draws again.
            }
        }

        /// <summary>
        ///     Returns which of the screens of <paramref name="block" /> is <paramref name="surface" />, or -1.
        /// </summary>
        static int Area(IMyCubeBlock block, IMyTextSurface surface)
        {
            var provider = block as IMyTextSurfaceProvider;
            if (provider == null)
                return -1;

            for (var i = 0; i < provider.SurfaceCount; i++)
            {
                if (provider.GetSurface(i) == surface)
                    return i;
            }

            return -1;
        }
    }
}
