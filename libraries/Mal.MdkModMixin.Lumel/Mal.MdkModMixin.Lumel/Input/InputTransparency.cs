using System;

namespace Lumel.Input
{
    /// <summary>
    ///     Which parts of an element let presses, hover and keyboard focus pass through to what is beneath it.
    /// </summary>
    /// <remarks>Tips are said whatever this is. <see cref="Children" /> does nothing on an element with nothing inside it.</remarks>
    [Flags]
    public enum InputTransparency
    {
        /// <summary>The element and everything inside it receive input as usual.</summary>
        None = 0,

        /// <summary>The element itself passes input through; the elements inside it still receive it.</summary>
        Self = 1,

        /// <summary>Everything inside the element passes input through; the element itself still receives it.</summary>
        Children = 2,

        /// <summary>The element and everything inside it pass input through.</summary>
        All = Self | Children
    }
}
