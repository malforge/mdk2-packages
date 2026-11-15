using Lumel.Drawing;
using VRage.Game.GUI.TextPanel;
using Xunit;
using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Lumel.Tests;

/// <summary>A surface whose screen moves is drawn every other tick until its motion ends, and then left alone.</summary>
public class SurfaceLoopTests
{
    sealed class Moving : ILiveSurface
    {
        public int Frames;
        public int Last = -1;
        public int Stops = int.MaxValue;

        public IMyCubeBlock Block => null!;

        public IMyTextSurface Surface => null!;

        public bool Frame(List<MySprite> output, int tick)
        {
            Frames++;
            Last = tick;
            return tick < Stops;
        }
    }

    /// <summary>A moving surface is drawn on every frame's tick, thirty times a second.</summary>
    [Fact]
    public void AMovingSurfaceIsDrawnEveryOtherTick()
    {
        var loop = new SurfaceLoop();
        var moving = new Moving();
        loop.Add(moving);

        for (var tick = 1; tick <= 60; tick++)
            loop.Run(tick);

        Assert.Equal(60 / SurfaceLoop.Interval, moving.Frames);
        Assert.True(loop.IsBusy);
    }

    /// <summary>A surface whose motion has ended is drawn that last time and then no more, and the loop has nothing to do.</summary>
    [Fact]
    public void AStillSurfaceLeavesTheLoop()
    {
        var loop = new SurfaceLoop();
        var moving = new Moving { Stops = 10 };
        loop.Add(moving);

        for (var tick = 1; tick <= 60; tick++)
            loop.Run(tick);

        Assert.Equal(10, moving.Last);
        Assert.False(loop.IsBusy);
    }

    /// <summary>A surface taken out is drawn no more.</summary>
    [Fact]
    public void ARemovedSurfaceIsNotDrawn()
    {
        var loop = new SurfaceLoop();
        var moving = new Moving();
        loop.Add(moving);
        loop.Remove(moving);

        loop.Run(2);

        Assert.Equal(0, moving.Frames);
        Assert.False(loop.IsBusy);
    }
}
