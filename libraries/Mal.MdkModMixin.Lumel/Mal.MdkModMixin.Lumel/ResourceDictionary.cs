using System;
using System.Collections.Generic;

namespace Lumel
{
    /// <summary>
    ///     Objects an application puts where the elements beneath can find them, such as a <see cref="Codex" />, each kept
    ///     under a key: the full name of its type unless it is given one.
    /// </summary>
    /// <remarks>
    ///     An element finds a resource with <see cref="Element.FindResource{T}()" />, in its own resources, then its
    ///     parents', and so on up to its workspace or text panel, so a resource set lower down takes the place of one set
    ///     higher up.
    /// </remarks>
    public class ResourceDictionary
    {
        readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        /// <summary>Gets the number of resources.</summary>
        public int Count => _items.Count;

        /// <summary>Puts <paramref name="value" /> under the full name of <typeparamref name="T" />, replacing what was there.</summary>
        public void Add<T>(T value) where T : class
        {
            Add(KeyOf<T>(), value);
        }

        /// <summary>Puts <paramref name="value" /> under <paramref name="key" />, replacing what was there.</summary>
        public void Add(string key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _items[key] = value;
        }

        /// <summary>Removes what is under <paramref name="key" />, and returns true if there was anything.</summary>
        public bool Remove(string key)
        {
            return _items.Remove(key);
        }

        /// <summary>Returns true if there is a resource under <paramref name="key" />.</summary>
        public bool Contains(string key)
        {
            return _items.ContainsKey(key);
        }

        /// <summary>
        ///     Gives the resource under <paramref name="key" />, or null, and returns true if there was one.
        /// </summary>
        public bool TryGet(string key, out object value)
        {
            return _items.TryGetValue(key, out value);
        }

        /// <summary>Returns the key a resource of type <typeparamref name="T" /> is kept under where it is given none: its type's full name.</summary>
        public static string KeyOf<T>()
        {
            return typeof(T).FullName;
        }
    }
}
