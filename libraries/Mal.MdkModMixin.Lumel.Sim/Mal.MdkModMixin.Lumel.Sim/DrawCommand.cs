using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Lumel.Sim
{
    /// <summary>One recorded draw call, kept as data instead of being executed immediately.</summary>
    /// <remarks>
    /// Recorded rather than drawn for two reasons. The near one is mechanical: a canvas only hands out
    /// something to draw with inside its own render pass, while the editor lays out and draws in one go and
    /// should not have to care. The far one is the useful one - a frame that exists as a list can be
    /// compared to another frame, which is how this stops being something to look at and starts being
    /// something to assert on.
    /// </remarks>
    public struct DrawCommand
    {
        /// <summary>The kind of call recorded.</summary>
        public DrawKind Kind;

        /// <summary>The layer the call drew on.</summary>
        public MyScreenSpaceLayer Layer;

        /// <summary>The material a sprite was drawn with.</summary>
        public MyStringId Material;

        /// <summary>The box a sprite was drawn into, in pixels.</summary>
        public Rectangle Destination;

        /// <summary>The part of the material's picture a sprite was drawn from, in its own pixels, or null for all of it.</summary>
        public Rectangle? Source;

        /// <summary>How far a sprite was turned about its middle, in radians.</summary>
        public float Rotation;

        /// <summary>The font a run of text was drawn in.</summary>
        public string? Font;

        /// <summary>The run of text drawn.</summary>
        public string? Text;

        /// <summary>Where a run of text starts, in pixels, with its alignment already applied.</summary>
        public Vector2 Position;

        /// <summary>The scale a run of text was drawn at.</summary>
        public float Scale;

        /// <summary>How a run of text was aligned on the point it was handed.</summary>
        public MyGuiDrawAlignEnum Align;

        /// <summary>The maximum width a run of text was allowed.</summary>
        public float MaxWidth;

        /// <summary>The color a sprite or a run of text was tinted.</summary>
        public Color Color;

        /// <summary>The box drawing was clipped to, in pixels.</summary>
        public Rectangle Clip;
    }
}
