using System;

namespace Lumel.Controls
{
    public partial class TabView
    {
        /// <summary>The tabs of one view, which only that view can create.</summary>
        sealed class OwnedTabs : TabList
        {
            public OwnedTabs(Action<Tab> admit, Action<Tab, int> inserted, Action<Tab, int> removed) : base(admit, inserted, removed)
            {
            }
        }
    }
}
