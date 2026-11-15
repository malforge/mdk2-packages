using VRageMath;

namespace Lumel.Styling
{
    /// <summary>The colors the game draws its own screens in, which Lumel's default look uses.</summary>
    /// <remarks>Sampled from the game's GUI textures.</remarks>
    public static class GameColors
    {
        /// <summary>The darkest background: a page, and the inside of a box text is typed into.</summary>
        public static readonly Color Page = new Color(20, 27, 32);

        /// <summary>The background of a dialog, a step lighter than a page.</summary>
        public static readonly Color Chrome = new Color(27, 36, 42);

        /// <summary>The background that controls stand on: a panel, a menu, a button.</summary>
        public static readonly Color Panel = new Color(41, 54, 62);

        /// <summary>A panel on hover.</summary>
        public static readonly Color PanelHot = new Color(60, 76, 82);

        /// <summary>A hairline, and what is highlighted or chosen.</summary>
        public static readonly Color Line = new Color(91, 115, 123);

        /// <summary>The edge of a control at rest.</summary>
        public static readonly Color ControlEdge = new Color(63, 71, 79);

        /// <summary>The edge of a control on hover.</summary>
        public static readonly Color ControlEdgeHot = new Color(112, 117, 122);

        /// <summary>A recess, such as a switch's track while off.</summary>
        public static readonly Color Well = new Color(32, 39, 45);

        /// <summary>The color the game writes in.</summary>
        public static readonly Color Ink = new Color(220, 245, 253);

        /// <summary>A step dimmer than <see cref="Ink" />, for secondary text.</summary>
        public static readonly Color Muted = new Color(162, 195, 205);

        /// <summary>Faint text: a hint in an empty box, a held scrollbar.</summary>
        public static readonly Color Faint = new Color(133, 161, 169);

        /// <summary>What is disabled, and a scrollbar's thumb.</summary>
        public static readonly Color Dim = new Color(95, 114, 120);

        /// <summary>The accent color: the default choice of a dialog.</summary>
        public static readonly Color Accent = new Color(142, 188, 207);

        /// <summary>The shadow a window casts on what lies beneath it.</summary>
        public static readonly Color Shadow = new Color(0, 0, 0, 170);

        /// <summary>The overlay that veils the screen behind a dialog, for an application that wants one.</summary>
        public static readonly Color Veil = new Color(20, 27, 32, 153);
    }
}
