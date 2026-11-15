using System;

namespace Lumel.Controls
{
    /// <summary>
    ///     Implemented by the tabs of a <see cref="TabView" /> so the view can tell a tab whether its page is shown and
    ///     whom to notify when its page changes.
    /// </summary>
    /// <remarks>Implemented explicitly by <see cref="Tab" />, so only the view it is in sets these.</remarks>
    public interface ITabbed
    {
        /// <summary>Sets whether the tab's page is the one shown.</summary>
        bool IsSelected { set; }

        /// <summary>
        ///     Sets the callback to invoke when the tab's page changes, or null while the tab is not in a view.
        /// </summary>
        Action ContentChanged { set; }
    }
}
