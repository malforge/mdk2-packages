using System;
using System.Collections.Generic;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The colors chosen last, the newest first, each once and no more than a fixed number, which any number of
    ///     <see cref="ColorPicker" />s share and add to.
    /// </summary>
    /// <remarks>Keeping it between sessions is the application's: it loads it with <see cref="SetColors" />, and saves it on <see cref="Changed" />.</remarks>
    public class ColorHistory
    {
        readonly List<Color> _colors = new List<Color>();

        /// <summary>Creates an empty history of no more than <paramref name="capacity" /> colors.</summary>
        /// <remarks>Throws if <paramref name="capacity" /> is less than 1.</remarks>
        public ColorHistory(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("A history holds at least one color.", nameof(capacity));

            Capacity = capacity;
        }

        /// <summary>Raised when a color is added, but not when <see cref="SetColors" /> is called.</summary>
        public event Action Changed;

        /// <summary>Gets the most colors it holds.</summary>
        public int Capacity { get; }

        /// <summary>Gets the colors, the newest first.</summary>
        public IReadOnlyList<Color> Colors => _colors;

        /// <summary>Puts <paramref name="color" /> first, taking it out where it was, and drops the oldest past <see cref="Capacity" />.</summary>
        /// <remarks>Raises <see cref="Changed" /> unless the color was first already.</remarks>
        public void Add(Color color)
        {
            var at = _colors.IndexOf(color);
            if (at == 0)
                return;

            if (at > 0)
                _colors.RemoveAt(at);

            _colors.Insert(0, color);
            if (_colors.Count > Capacity)
                _colors.RemoveRange(Capacity, _colors.Count - Capacity);

            Changed?.Invoke();
        }

        /// <summary>Takes <paramref name="colors" />, the newest first, in place of the colors it held.</summary>
        /// <remarks>Throws if there are more than <see cref="Capacity" />, or one is there twice.</remarks>
        public void SetColors(IList<Color> colors)
        {
            if (colors.Count > Capacity)
                throw new ArgumentException("A history of " + Capacity + " colors cannot hold " + colors.Count + ".", nameof(colors));

            for (var i = 0; i < colors.Count; i++)
            {
                if (colors.IndexOf(colors[i]) != i)
                    throw new ArgumentException("A history holds each color once, and " + colors[i] + " is there twice.", nameof(colors));
            }

            _colors.Clear();
            _colors.AddRange(colors);
        }
    }
}
