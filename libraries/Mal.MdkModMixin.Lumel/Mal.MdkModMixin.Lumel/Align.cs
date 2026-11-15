using System;

namespace Lumel
{
    /// <summary>How an element is positioned within the space its container gives it, on each axis.</summary>
    /// <remarks>
    /// Each axis is read alone: one named edge pins the element to it, both stretch it, and none centers it where
    /// <see cref="Center" /> is set. A holder ignores the axis it stacks along.
    /// </remarks>
    [Flags]
    public enum Align
    {
        /// <summary>Places the element the way its container prefers.</summary>
        Default = 0,

        /// <summary>Centers the element on every axis where no edge is specified.</summary>
        Center = 1,

        /// <summary>
        ///     Aligns the element to the left edge; combined with <see cref="Right" />, it stretches horizontally.
        /// </summary>
        Left = 2,

        /// <summary>
        ///     Aligns the element to the right edge; combined with <see cref="Left" />, it stretches horizontally.
        /// </summary>
        Right = 4,

        /// <summary>
        ///     Aligns the element to the top edge; combined with <see cref="Bottom" />, it stretches vertically.
        /// </summary>
        Top = 8,

        /// <summary>
        ///     Aligns the element to the bottom edge; combined with <see cref="Top" />, it stretches vertically.
        /// </summary>
        Bottom = 16,

        /// <summary>Stretches the element horizontally from the left edge to the right edge.</summary>
        FillWidth = Left | Right,

        /// <summary>Stretches the element vertically from the top edge to the bottom edge.</summary>
        FillHeight = Top | Bottom,

        /// <summary>Stretches the element in both directions to fill the available space.</summary>
        Fill = FillWidth | FillHeight
    }
}
