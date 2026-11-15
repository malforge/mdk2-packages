using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Layout
{
    /// <summary>The columns or rows of a grid, in order.</summary>
    /// <remarks>Made by the grid that holds them. A track past the last one defined is <see cref="GridLength.Auto" />.</remarks>
    public abstract class GridTracks : IEnumerable<GridLength>
    {
        readonly Action _changed;
        readonly List<GridLength> _lengths = new List<GridLength>();

        /// <summary>
        ///     Creates an empty list of tracks that calls <paramref name="changed" /> whenever the tracks change.
        /// </summary>
        protected GridTracks(Action changed)
        {
            _changed = changed;
        }

        /// <summary>Gets the number of tracks defined.</summary>
        public int Count => _lengths.Count;

        /// <summary>Gets or sets the track at <paramref name="index" />, or Auto past the last one defined.</summary>
        public GridLength this[int index]
        {
            get { return index < _lengths.Count ? _lengths[index] : GridLength.Auto; }
            set
            {
                while (_lengths.Count <= index)
                    _lengths.Add(GridLength.Auto);

                _lengths[index] = value;
                _changed();
            }
        }

        IEnumerator<GridLength> IEnumerable<GridLength>.GetEnumerator()
        {
            return _lengths.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _lengths.GetEnumerator();
        }

        /// <summary>Adds a track after the last.</summary>
        public void Add(GridLength length)
        {
            _lengths.Add(length);
            _changed();
        }
    }
}
