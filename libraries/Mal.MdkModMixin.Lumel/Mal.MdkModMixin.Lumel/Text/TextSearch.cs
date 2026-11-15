using System;

namespace Lumel.Text
{
    /// <summary>The matching of a search box: each word typed is looked for anywhere in the text, in the order typed, in any case.</summary>
    /// <remarks>"airl 1" finds "Airlock 10" and "Airlock Door 1", and leaves out "1 Airlock".</remarks>
    public static class TextSearch
    {
        /// <summary>
        ///     Returns true if every word of <paramref name="search" /> is found in <paramref name="text" />, each after the
        ///     one before it; a search with no words finds everything.
        /// </summary>
        /// <remarks>Asked of every row on every keystroke, so the words are walked in place and nothing is allocated.</remarks>
        public static bool Matches(string text, string search)
        {
            if (string.IsNullOrEmpty(search))
                return true;

            text = text ?? "";
            var at = 0;
            var after = 0;
            while (true)
            {
                while (at < search.Length && char.IsWhiteSpace(search[at]))
                    at++;

                if (at == search.Length)
                    return true;

                var start = at;
                while (at < search.Length && !char.IsWhiteSpace(search[at]))
                    at++;

                var found = IndexOf(text, after, search, start, at - start);
                if (found < 0)
                    return false;

                after = found + at - start;
            }
        }

        /// <summary>
        ///     Returns where the word of <paramref name="length" /> at <paramref name="from" /> in <paramref name="search" />
        ///     first stands in <paramref name="text" />, at or after <paramref name="after" />, or -1 if nowhere.
        /// </summary>
        static int IndexOf(string text, int after, string search, int from, int length)
        {
            for (var start = after; start <= text.Length - length; start++)
            {
                if (string.Compare(text, start, search, from, length, StringComparison.OrdinalIgnoreCase) == 0)
                    return start;
            }

            return -1;
        }
    }
}
