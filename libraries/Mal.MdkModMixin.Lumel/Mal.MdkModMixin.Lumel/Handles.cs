
namespace Lumel
{
    /// <summary>Keeps a reference to an element while it is built inside an initializer.</summary>
    public static class Handles
    {
        /// <summary>
        ///     Stores <paramref name="element" /> in <paramref name="handle" /> and returns it to take its place in the
        ///     tree.
        /// </summary>
        public static T As<T>(this T element, out T handle) where T : Element
        {
            handle = element;
            return element;
        }
    }
}
