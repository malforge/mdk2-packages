using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Lumel.Drawing
{
    /// <summary>
    ///     A text surface that a <see cref="SurfaceLoop" /> draws while something on it is animating, faster than its
    ///     app runs.
    /// </summary>
    public interface ILiveSurface
    {
        /// <summary>The block the surface is on.</summary>
        IMyCubeBlock Block { get; }

        /// <summary>The surface that is drawn.</summary>
        IMyTextSurface Surface { get; }

        /// <summary>
        ///     Fills <paramref name="output" /> with the frame for <paramref name="tick" /> and returns false once
        ///     nothing is animating any more.
        /// </summary>
        bool Frame(List<MySprite> output, int tick);
    }
}
