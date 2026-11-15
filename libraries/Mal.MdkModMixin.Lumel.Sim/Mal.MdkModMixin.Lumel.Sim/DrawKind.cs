
namespace Lumel.Sim
{
    /// <summary>The kind of a recorded screen call.</summary>
    public enum DrawKind
    {
        /// <summary>Drew a material into a box.</summary>
        Sprite,

        /// <summary>Drew a run of text.</summary>
        Text,

        /// <summary>Clipped drawing to a box.</summary>
        SetClip,

        /// <summary>Removed the clip so that drawing reaches the whole screen again.</summary>
        ClearClip
    }
}
