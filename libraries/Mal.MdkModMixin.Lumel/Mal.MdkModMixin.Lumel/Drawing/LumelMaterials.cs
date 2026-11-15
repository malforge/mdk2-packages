using VRage.Utils;

namespace Lumel.Drawing
{
    /// <summary>
    ///     The transparent materials Lumel draws with, declared in Content/Data/Lumel_TransparentMaterials.sbc and
    ///     packed into every mod built on it.
    /// </summary>
    public static class LumelMaterials
    {
        /// <summary>
        ///     Flat white over the game's Textures\GUI\Blank.dds, tinted to the wanted color, used for every flat fill.
        /// </summary>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Lumel_Fill");

        /// <summary>
        ///     A soft shadow over Lumel's own Textures\Sprites\Lumel_Shadow.dds, solid in the middle and fading out
        ///     toward its edges, used for every window's shadow.
        /// </summary>
        public static readonly MyStringId Shadow = MyStringId.GetOrCompute("Lumel_Shadow");

        /// <summary>
        ///     A white disc with a smooth edge over Lumel's own Textures\Sprites\Lumel_Disc.dds, tinted to the wanted color;
        ///     a text panel draws its own circle for it.
        /// </summary>
        public static readonly MyStringId Disc = MyStringId.GetOrCompute("Lumel_Disc");

        /// <summary>
        ///     A white ring an eighth of its width thick over Lumel's own Textures\Sprites\Lumel_Ring.dds, tinted to the
        ///     wanted color; a text panel draws its own hollow circle for it.
        /// </summary>
        public static readonly MyStringId Ring = MyStringId.GetOrCompute("Lumel_Ring");

        /// <summary>
        ///     Every hue from red at the top round to red at the bottom over Lumel's own Textures\Sprites\Lumel_Hues.dds,
        ///     drawn untinted: a hue slider's strip.
        /// </summary>
        public static readonly MyStringId Hues = MyStringId.GetOrCompute("Lumel_Hues");

        /// <summary>
        ///     White fading from solid at the left to nothing at the right over Lumel's own Textures\Sprites\Lumel_Pale.dds:
        ///     the saturation a color spectrum lays over its hue.
        /// </summary>
        public static readonly MyStringId Pale = MyStringId.GetOrCompute("Lumel_Pale");

        /// <summary>
        ///     Black rising from nothing at the top to solid at the bottom over Lumel's own Textures\Sprites\Lumel_Dark.dds:
        ///     the value a color spectrum lays over the rest.
        /// </summary>
        public static readonly MyStringId Dark = MyStringId.GetOrCompute("Lumel_Dark");

        /// <summary>
        ///     A row of white dots a dot apart over Lumel's own TexturesSpritesumel_dots.dds, tinted to the wanted color, of
        ///     which a dotted line draws a slice.
        /// </summary>
        public static readonly MyStringId Dots = MyStringId.GetOrCompute("Lumel_Dots");
    }
}
