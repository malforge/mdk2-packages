using System;
using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>The values that fill a document's slots, keyed by slot name.</summary>
    /// <remarks>
    ///     Said as they are: a value is words, never markdown, so a name with a star in it says the star.
    ///     Setting a value to what it already is changes nothing and tells nobody.
    /// </remarks>
    public sealed class Arguments
    {
        readonly List<string> _names = new List<string>(2);
        readonly List<string> _values = new List<string>(2);

        /// <summary>
        ///     Raised when a value changes, so that whatever shows the values can lay its text out again.
        /// </summary>
        public event Action Changed;

        /// <summary>Sets <paramref name="value" /> as the text for the slots named <paramref name="name" />.</summary>
        public void Set(string name, string value)
        {
            value = value ?? "";

            var at = _names.IndexOf(name);
            if (at < 0)
            {
                _names.Add(name);
                _values.Add(value);
            }
            else
            {
                if (_values[at] == value)
                    return;

                _values[at] = value;
            }

            var changed = Changed;
            if (changed != null)
                changed();
        }

        /// <summary>
        ///     Returns the text for the slots named <paramref name="name" />, or an empty string if none is set.
        /// </summary>
        public string Get(string name)
        {
            var at = _names.IndexOf(name);
            return at < 0 ? "" : _values[at];
        }
    }
}
