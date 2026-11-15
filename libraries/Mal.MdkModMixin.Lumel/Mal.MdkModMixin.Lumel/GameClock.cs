using Sandbox.ModAPI;

namespace Lumel
{
    /// <summary>The game's clock, read by the hosts that draw on the game's screens.</summary>
    /// <remarks>Static because the clock is the game session's own, one for the whole game.</remarks>
    public static class GameClock
    {
        /// <summary>The game's frame count, sixty to a second, or 0 outside a session.</summary>
        public static int Now => MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
    }
}
