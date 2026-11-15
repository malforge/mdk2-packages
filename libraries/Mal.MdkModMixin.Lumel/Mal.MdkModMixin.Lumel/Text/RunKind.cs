using System;

namespace Lumel.Text
{
    /// <summary>
    ///     How a run is formatted relative to the text around it; strong, emphasis and code can be combined.
    /// </summary>
    [Flags]
    public enum RunKind
    {
        /// <summary>Ordinary text.</summary>
        Plain = 0,

        /// <summary>Text with strong importance.</summary>
        Strong = 1,

        /// <summary>Text with emphasis, a lighter stress than strong.</summary>
        Emphasis = 2,

        /// <summary>Code text.</summary>
        Code = 4,

        /// <summary>A line break, carrying no words.</summary>
        Break = 8
    }
}
