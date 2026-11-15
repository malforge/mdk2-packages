using System;
using Lumel.Input;

namespace Lumel.Text
{
    /// <summary>
    ///     A text box edited from the keyboard and the pointer, whatever its shape, exposing its text, caret, selection
    ///     and undo history.
    /// </summary>
    /// <remarks>A position is a count of characters from the start, a line break counting as one.</remarks>
    public interface IEditableText
    {
        /// <summary>
        ///     Gets or sets the text. Setting it, even to the current value, replaces the text, moves the caret to the
        ///     start, clears the selection and clears the undo history.
        /// </summary>
        string Text { get; set; }

        /// <summary>Gets the caret position, between 0 and the length of <see cref="Text" />.</summary>
        int CaretIndex { get; }

        /// <summary>Gets the index where the selection starts.</summary>
        int SelectionStart { get; }

        /// <summary>Gets the number of selected characters.</summary>
        int SelectionLength { get; }

        /// <summary>Gets the selected text, or an empty string when nothing is selected.</summary>
        string SelectedText { get; }

        /// <summary>
        ///     Gets or sets the maximum number of characters that typing and pasting can produce, or 0 for no limit.
        /// </summary>
        int MaxLength { get; set; }

        /// <summary>Whether the text is read-only: it can be selected and copied but not typed into.</summary>
        bool IsReadOnly { get; set; }

        /// <summary>
        ///     Gets or sets the command run when Escape is pressed while the box has keyboard focus, or null to release
        ///     the keyboard.
        /// </summary>
        ICommand Cancel { get; set; }

        /// <summary>
        ///     Raised when the text is changed by typing, cutting, pasting, deleting, undoing or redoing, but not when
        ///     <see cref="Text" /> is set.
        /// </summary>
        event Action Edited;

        /// <summary>
        ///     Selects <paramref name="length" /> characters starting at <paramref name="start" />, clamped to the
        ///     text, with the caret at the end.
        /// </summary>
        void Select(int start, int length);

        /// <summary>Selects all the text, with the caret at the end.</summary>
        void SelectAll();

        /// <summary>Undoes the last edit and restores the caret and selection.</summary>
        void Undo();

        /// <summary>Redoes the last undone edit.</summary>
        void Redo();
    }
}
