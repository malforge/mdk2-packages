using VRageMath;

namespace Lumel.Text
{
    /// <summary>
    ///     A stretch of text within a block that is formatted uniformly, a slot whose text is supplied later, or a line
    ///     break.
    /// </summary>
    /// <remarks>An image is an <see cref="ImageRun" />, and a link's words are a <see cref="LinkRun" />.</remarks>
    public class Run
    {
        /// <summary>The space character at which a line is never broken, U+00A0.</summary>
        public const char NoBreakSpace = ' ';

        /// <summary>A line break run.</summary>
        public static readonly Run Break = new Run("", RunKind.Break);

        /// <summary>Creates a run of <paramref name="text" /> formatted as <paramref name="kind" />.</summary>
        public Run(string text, RunKind kind)
            : this(text, kind, null, null)
        {
        }

        /// <summary>
        ///     Creates a run of <paramref name="text" /> formatted as <paramref name="kind" />, in a span of the class
        ///     <paramref name="className" /> and the color <paramref name="color" />, either of which may be null.
        /// </summary>
        public Run(string text, RunKind kind, string className, Color? color)
        {
            Text = text ?? "";
            Kind = kind;
            Class = className;
            Color = color;
            HasNoBreakSpace = Text.IndexOf(NoBreakSpace) >= 0;
        }

        /// <summary>
        ///     Creates a slot for the argument named <paramref name="slot" />, formatted as <paramref name="kind" />.
        /// </summary>
        protected Run(RunKind kind, string slot)
            : this(kind, slot, null, null)
        {
        }

        /// <summary>
        ///     Creates a slot for the argument named <paramref name="slot" />, formatted as <paramref name="kind" />, in a
        ///     span of the class <paramref name="className" /> and the color <paramref name="color" />, either of which may
        ///     be null.
        /// </summary>
        protected Run(RunKind kind, string slot, string className, Color? color)
            : this("", kind, className, color)
        {
            Slot = slot;
        }

        /// <summary>Gets the text, which is empty for a line break or a slot.</summary>
        public string Text { get; }

        /// <summary>Gets how the text is formatted relative to the text around it.</summary>
        public RunKind Kind { get; }

        /// <summary>
        ///     Gets the name of the argument whose text goes here, or null for a run that carries its own text.
        /// </summary>
        public string Slot { get; }

        /// <summary>
        ///     Whether the text contains a no-break space, computed once so that layout need not search for one.
        /// </summary>
        public bool HasNoBreakSpace { get; }

        /// <summary>Gets the class of the span this run is in, whose look the host's style says, or null if it has none.</summary>
        public string Class { get; }

        /// <summary>Gets the color the span this run is in gives its words, or null if it gives none.</summary>
        public Color? Color { get; }

        /// <summary>Gets the link this run belongs to, or null if it belongs to none.</summary>
        public virtual Link Link => null;

        /// <summary>Whether this run ends a line rather than carrying text.</summary>
        public bool IsBreak => (Kind & RunKind.Break) != 0;

        /// <summary>
        ///     Creates a slot for the argument named <paramref name="name" />, formatted as <paramref name="kind" />.
        /// </summary>
        public static Run Slotted(string name, RunKind kind)
        {
            return new Run(kind, name);
        }

        /// <summary>
        ///     Creates a slot for the argument named <paramref name="name" />, formatted as <paramref name="kind" />, in a
        ///     span of the class <paramref name="className" /> and the color <paramref name="color" />, either of which may
        ///     be null.
        /// </summary>
        public static Run Slotted(string name, RunKind kind, string className, Color? color)
        {
            return new Run(kind, name, className, color);
        }
    }
}
