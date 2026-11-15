using System.Collections.Generic;

namespace Lumel.Styling
{
    /// <summary>How an element looks, read-only: a look for its base and for each state it names.</summary>
    public interface IStyle
    {
        /// <summary>
        ///     Lays everything this style sets for an element in <paramref name="states" /> over
        ///     <paramref name="look" />.
        /// </summary>
        void Lay(ElementLook look, State states);

        /// <summary>
        ///     Adds to <paramref name="keys" /> each state held in <paramref name="states" /> that this style has a
        ///     look for, if it is not already there.
        /// </summary>
        void FindLooks(State states, List<State> keys);

        /// <summary>
        ///     Lays this style's own look for exactly <paramref name="key" />, on top of the one from the style it is
        ///     based on, over <paramref name="look" />.
        /// </summary>
        void LayOne(ElementLook look, State key);

        /// <summary>
        ///     Returns true if this style is for <paramref name="element" />, that is, if <paramref name="element" />
        ///     is the kind of control this style is for or derives from it.
        /// </summary>
        bool IsFor(Element element);

        /// <summary>
        ///     Returns true if <paramref name="other" /> is for this style's kind of control or for one derived from
        ///     it.
        /// </summary>
        bool Includes(IStyle other);
    }
}
