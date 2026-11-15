using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel
{
    public abstract partial class Element
    {
        /// <summary>The children of one element.</summary>
        /// <remarks>Made by the element that holds it, through <see cref="CreateChildren()" />.</remarks>
        public abstract class Collection : IEnumerable<Element>
        {
            readonly List<Element> _items = new List<Element>();
            readonly Element _owner;

            /// <summary>
            ///     Creates an empty collection whose members become children of <paramref name="owner" />.
            /// </summary>
            /// <remarks>
            ///     Made only through the owner's <see cref="CreateChildren()" />, and throws otherwise, or when
            ///     <paramref name="owner" /> already holds one: an element makes its children once.
            /// </remarks>
            protected Collection(Element owner)
            {
                if (!owner._creatingChildren)
                    throw new InvalidOperationException("An element's children are made by the element, through CreateChildren.");

                if (owner._contents != null)
                    throw new InvalidOperationException("An element's children are made once, by the element.");

                _owner = owner;
                owner._contents = this;
            }

            /// <summary>The number of children.</summary>
            public int Count => _items.Count;

            /// <summary>Gets the child at <paramref name="index" />.</summary>
            public Element this[int index] => _items[index];

            IEnumerator<Element> IEnumerable<Element>.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            /// <summary>
            ///     Adds <paramref name="child" /> at the end and makes this its parent, moving it from its previous
            ///     parent.
            /// </summary>
            public void Add(Element child)
            {
                Insert(_items.Count, child);
            }

            /// <summary>
            ///     Inserts <paramref name="child" /> at <paramref name="index" /> and makes this its parent, moving it
            ///     from its previous parent.
            /// </summary>
            /// <remarks>Throws, leaving everything as it was, for a null child, for the owner itself, and when the owner cannot place the child as its placement asks.</remarks>
            public void Insert(int index, Element child)
            {
                if (child == null)
                    throw new ArgumentNullException(nameof(child));

                if (child == _owner)
                    throw new ArgumentException("An element cannot be its own child.", nameof(child));

                if (_owner._admit != null)
                    _owner._admit(child, child.Placement);

                if (child._parent != null)
                {
                    if (child._parent == _owner && _items.IndexOf(child) < index)
                        index--;

                    child._parent._contents.Remove(child);
                }

                if (index < 0)
                    index = 0;
                if (index > _items.Count)
                    index = _items.Count;

                _items.Insert(index, child);
                child._parent = _owner;
                child.Rehost();
                child.Reload();

                if (_owner._added != null)
                    _owner._added(child, index);

                // A child never measured is already dirty, so its own invalidation stops at itself and never
                // reaches the owner.
                child.InvalidateMeasure();
                _owner.InvalidateMeasure();
            }

            /// <summary>Returns the index of <paramref name="child" />, or -1 if it is not in the collection.</summary>
            public int IndexOf(Element child)
            {
                return _items.IndexOf(child);
            }

            /// <summary>
            ///     Removes <paramref name="child" />, leaving it without a parent, and returns true if it was in the
            ///     collection.
            /// </summary>
            public bool Remove(Element child)
            {
                if (child == null)
                    return false;

                var index = _items.IndexOf(child);
                if (index < 0)
                    return false;

                _items.RemoveAt(index);
                Orphan(child, index);
                _owner.InvalidateMeasure();
                return true;
            }

            /// <summary>Removes every child, leaving them without a parent.</summary>
            public void Clear()
            {
                for (var i = _items.Count - 1; i >= 0; i--)
                {
                    var child = _items[i];
                    _items.RemoveAt(i);
                    Orphan(child, i);
                }

                _owner.InvalidateMeasure();
            }

            /// <summary>
            ///     Returns the children in order, through the list's own struct enumerator, which a foreach walks
            ///     without allocating.
            /// </summary>
            public List<Element>.Enumerator GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            void Orphan(Element child, int index)
            {
                child._parent = null;
                child.Reload();

                if (_owner._removed != null)
                    _owner._removed(child, index);
            }
        }
    }
}
