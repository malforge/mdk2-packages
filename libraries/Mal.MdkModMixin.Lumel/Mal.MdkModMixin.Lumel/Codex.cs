using System;
using Lumel.Text;

namespace Lumel
{
    /// <summary>
    ///     What a mod's <see cref="CodexTip" />s share: where their links lead, and how they are pinned so the pointer can
    ///     move onto them.
    /// </summary>
    /// <remarks>Made once by the mod and given to every codex tip it makes.</remarks>
    public class Codex
    {
        /// <summary>
        ///     Gets or sets the callback that returns the page a link leads to for the link's target, or null if there is
        ///     none.
        /// </summary>
        public Func<string, Tip> FnResolvePage { get; set; }

        /// <summary>
        ///     Whether a codex tip holds the pointer only while the host's pin key is held; without the key it behaves as an
        ///     ordinary tip that the pointer passes through, and with the key it stays open wherever the pointer goes.
        /// </summary>
        public bool IsPinnedByKey { get; set; }

        /// <summary>
        ///     Gets or sets the callback that returns true if the host's pin key is held, or null if the host has no pin key,
        ///     in which case a tip is never pinned by key.
        /// </summary>
        /// <remarks>Asked only while a tip is open and <see cref="IsPinnedByKey" /> is set.</remarks>
        public Func<bool> FnIsPinKeyHeld { get; set; }

        /// <summary>
        ///     Gets or sets the line explaining how to pin a tip, at the bottom of a page with a link to a page not open, shown while
        ///     <see cref="IsPinnedByKey" /> is set, or null for none.
        /// </summary>
        public Document PinHint { get; set; }

        /// <summary>
        ///     Whether a link in a codex tip opens its page in a separate tip beside it once the pointer has rested on the
        ///     link, instead of in the same tip when pressed.
        /// </summary>
        public bool OpensNestedTips { get; set; }

        /// <summary>Returns the page <paramref name="id" /> leads to, or null if there is none.</summary>
        public Tip Page(string id)
        {
            return FnResolvePage?.Invoke(id);
        }
    }
}
