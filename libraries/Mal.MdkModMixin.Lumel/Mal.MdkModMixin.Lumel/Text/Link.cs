namespace Lumel.Text
{
    /// <summary>A link in a document: a target string that only the host interprets, and a class that picks its look.</summary>
    /// <remarks>One link's words may become several runs; they share this one instance, which is how they are one link.</remarks>
    public sealed class Link
    {
        /// <summary>Creates a link to <paramref name="target" />, of the class <paramref name="className" /> if it is given.</summary>
        public Link(string target, string className = null)
        {
            Target = target;
            Class = className;
        }

        /// <summary>Gets what the link leads to, as written between its parentheses.</summary>
        public string Target { get; }

        /// <summary>Gets the class named by the link's <c>class=</c>, or null if it names none.</summary>
        public string Class { get; }
    }
}
