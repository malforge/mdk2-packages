using System;

namespace Lumel.Layout
{
    /// <summary>A placement for a stack child that takes a share of the room the other children leave.</summary>
    public sealed class Flex : IStacked
    {
        /// <summary>A flex of weight 1, which takes all the remaining room when no other child grows.</summary>
        public static readonly Flex Grow = new Flex(1f, FlexFit.Tight);

        /// <summary>
        ///     A flex that asks for what its content needs when the stack is measured, none for an empty one, and takes
        ///     whatever the stack has left when it is arranged, so a spacer pushes the children after it to the far end.
        /// </summary>
        public static readonly Flex Spacer = new Flex(1f, FlexFit.Loose);

        readonly float _weight;
        readonly FlexFit _fit;

        Flex(float weight, FlexFit fit)
        {
            _weight = weight;
            _fit = fit;
        }

        float ISized.Width => float.NaN;

        float ISized.Height => float.NaN;

        float IStacked.Grow => _weight;

        FlexFit IStacked.Fit => _fit;

        event Action IPlacement.Changed
        {
            add { }
            remove { }
        }

        /// <summary>
        ///     Creates a flex with the given <paramref name="weight" />, relative to the weights of the other growing
        ///     children.
        /// </summary>
        public static Flex By(float weight)
        {
            return new Flex(weight, FlexFit.Tight);
        }
    }
}
