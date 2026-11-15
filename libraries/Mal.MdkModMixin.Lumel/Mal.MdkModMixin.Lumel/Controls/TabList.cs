using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Controls
{
    /// <summary>The tabs of one <see cref="TabView" />, in the order their headers appear.</summary>
    /// <remarks>Made by the view that holds them, and filled by the view's initializer.</remarks>
    public abstract class TabList : IEnumerable<Tab>
    {
        readonly Action<Tab> _admit;
        readonly Action<Tab, int> _inserted;
        readonly Action<Tab, int> _removed;
        readonly List<Tab> _tabs = new List<Tab>();

        /// <summary>
        ///     Creates a list that calls <paramref name="admit" /> to check each tab before it is added, and calls
        ///     <paramref name="inserted" /> and <paramref name="removed" /> with the tab and its index when a tab is
        ///     inserted or removed.
        /// </summary>
        protected TabList(Action<Tab> admit, Action<Tab, int> inserted, Action<Tab, int> removed)
        {
            _admit = admit;
            _inserted = inserted;
            _removed = removed;
        }

        /// <summary>The number of tabs.</summary>
        public int Count => _tabs.Count;

        /// <summary>Gets the tab at <paramref name="index" />, counted from 0.</summary>
        public Tab this[int index] => _tabs[index];

        /// <summary>Adds <paramref name="tab" /> after the others.</summary>
        public void Add(Tab tab)
        {
            Insert(_tabs.Count, tab);
        }

        /// <summary>Inserts <paramref name="tab" /> at <paramref name="index" />.</summary>
        /// <remarks>Throws, leaving the tabs as they were, for a null tab, a tab already here, a tab the view refuses, and an index outside nought to <see cref="Count" />.</remarks>
        public void Insert(int index, Tab tab)
        {
            if (tab == null)
                throw new ArgumentNullException(nameof(tab));

            if (_tabs.Contains(tab))
                throw new ArgumentException("The tab is already in this view.", nameof(tab));

            if (index < 0 || index > _tabs.Count)
                throw new ArgumentException("There is no tab at " + index + ".", nameof(index));

            _admit(tab);
            _tabs.Insert(index, tab);
            _inserted(tab, index);
        }

        /// <summary>Returns the index of <paramref name="tab" />, or -1 if it is not in the list.</summary>
        public int IndexOf(Tab tab)
        {
            return _tabs.IndexOf(tab);
        }

        /// <summary>Removes <paramref name="tab" /> and returns true if it was in the list.</summary>
        public bool Remove(Tab tab)
        {
            var index = _tabs.IndexOf(tab);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>Removes the tab at <paramref name="index" />.</summary>
        public void RemoveAt(int index)
        {
            var tab = _tabs[index];
            _tabs.RemoveAt(index);
            _removed(tab, index);
        }

        /// <summary>Removes every tab.</summary>
        public void Clear()
        {
            for (var i = _tabs.Count - 1; i >= 0; i--)
                RemoveAt(i);
        }

        /// <inheritdoc />
        public IEnumerator<Tab> GetEnumerator()
        {
            return _tabs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
