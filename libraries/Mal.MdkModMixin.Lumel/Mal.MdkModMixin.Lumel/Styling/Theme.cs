using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Styling
{
    /// <summary>
    ///     The style each kind of control takes when it is given none: one per kind, each filed under the kind its
    ///     style is for.
    /// </summary>
    /// <remarks>
    ///     Written once, in its initializer, and handed out as an <see cref="ITheme" />, which has nothing to write. A kind
    ///     with no style here takes the style for the nearest kind it derives from.
    /// </remarks>
    public sealed class Theme : ITheme, IEnumerable<IStyle>
    {
        readonly ITheme _basedOn;
        readonly List<IStyle> _styles = new List<IStyle>();
        Dictionary<Type, IStyle> _found = new Dictionary<Type, IStyle>();

        /// <summary>Creates a theme with no styles, leaving every control at its neutral values.</summary>
        public Theme()
        {
        }

        /// <summary>
        ///     Creates a theme that includes everything <paramref name="basedOn" /> has, with this theme's own styles
        ///     replacing those of <paramref name="basedOn" /> for the same kind.
        /// </summary>
        /// <remarks>Throws for a null base, which is what a static theme declared above the one it is based on receives.</remarks>
        public Theme(ITheme basedOn)
        {
            if (basedOn == null)
                throw new ArgumentNullException("basedOn", "A theme's base must be declared before it.");

            _basedOn = basedOn;
        }

        /// <summary>Adds <paramref name="style" /> as the default for the kind of control it is for.</summary>
        public void Add(IStyle style)
        {
            if (style == null)
                throw new ArgumentNullException("style");

            _styles.Add(style);
        }

        /// <inheritdoc />
        /// <remarks>Safe to call from several threads at once.</remarks>
        public IStyle StyleFor(Element element)
        {
            var kind = element.GetType();

            // Copied on write: a dictionary once published is never changed, so a thread reading it while another adds
            // reads a whole one. Two threads adding at once may each find a style, and an entry lost to the other's copy
            // is found again when next asked for.
            var known = _found;
            IStyle found;
            if (known.TryGetValue(kind, out found))
                return found;

            found = Nearest(element);

            var inherited = _basedOn?.StyleFor(element);
            if (inherited != null && (found == null || !inherited.Includes(found)))
                found = inherited;

            _found = new Dictionary<Type, IStyle>(known) { [kind] = found };
            return found;
        }

        /// <inheritdoc />
        public IEnumerator<IStyle> GetEnumerator()
        {
            return _styles.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Returns, of the styles this holds that are for <paramref name="element" />, the one for the kind nearest
        ///     it, or null if there is none.
        /// </summary>
        /// <remarks>Of two for the same kind, the one added last.</remarks>
        IStyle Nearest(Element element)
        {
            IStyle nearest = null;
            foreach (var style in _styles)
            {
                if (style.IsFor(element) && (nearest == null || nearest.Includes(style)))
                    nearest = style;
            }

            return nearest;
        }
    }
}
