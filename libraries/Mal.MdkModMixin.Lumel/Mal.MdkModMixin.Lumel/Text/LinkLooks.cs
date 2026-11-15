using System;
using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>The look of a link of each class, found by class name without regard to case.</summary>
    /// <remarks>Written in an initializer, as <c>Links = { ["term"] = { Color = ..., Underline = UnderlineStyle.Dotted } }</c>.</remarks>
    public sealed class LinkLooks
    {
        readonly Dictionary<string, LinkLook> _byClass = new Dictionary<string, LinkLook>(StringComparer.OrdinalIgnoreCase);
        readonly Action _changed;

        /// <summary>
        ///     Creates an empty set of looks that calls <paramref name="changed" /> whenever anything they set changes.
        /// </summary>
        public LinkLooks(Action changed)
        {
            _changed = changed;
        }

        /// <summary>Gets the look of a link of <paramref name="className" />, creating it on first access.</summary>
        public LinkLook this[string className]
        {
            get
            {
                if (className == null)
                    throw new ArgumentNullException(nameof(className));

                return Of(className);
            }
        }

        /// <summary>Returns the look of a link of <paramref name="className" />, or null if none is set.</summary>
        public LinkLook Find(string className)
        {
            LinkLook look;
            return className != null && _byClass.TryGetValue(className, out look) ? look : null;
        }

        /// <summary>
        ///     Applies each look in <paramref name="over" /> on top of this set's look for the same class.
        /// </summary>
        public void Merge(LinkLooks over)
        {
            foreach (var pair in over._byClass)
                Of(pair.Key).Merge(pair.Value);
        }

        /// <summary>Removes every look.</summary>
        public void Clear()
        {
            _byClass.Clear();
        }

        /// <summary>
        ///     Returns true if this set sets exactly what <paramref name="other" /> sets, class by class.
        /// </summary>
        public bool SetsSameAs(LinkLooks other)
        {
            if (_byClass.Count != other._byClass.Count)
                return false;

            foreach (var pair in _byClass)
            {
                LinkLook look;
                if (!other._byClass.TryGetValue(pair.Key, out look) || !pair.Value.SetsSameAs(look))
                    return false;
            }

            return true;
        }

        /// <summary>Returns the look of <paramref name="className" />, creating it if none exists.</summary>
        LinkLook Of(string className)
        {
            LinkLook look;
            if (_byClass.TryGetValue(className, out look))
                return look;

            look = new LinkLook(_changed);
            _byClass.Add(className, look);
            return look;
        }
    }
}
