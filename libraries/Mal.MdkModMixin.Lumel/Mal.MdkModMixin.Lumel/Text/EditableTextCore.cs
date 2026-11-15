using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using VRage.Input;

namespace Lumel.Text
{
    /// <summary>
    ///     The editing logic behind a text box: its text, caret and selection, undo history, clipboard keys and caret
    ///     blink.
    /// </summary>
    /// <remarks>A position is a count of characters from the start, a line break counting as one.</remarks>
    public sealed class EditableTextCore
    {
        /// <summary>The number of game ticks the caret stays visible, and then hidden, as it blinks.</summary>
        public const int CaretBlinkTicks = 30;

        readonly StringBuilder _text = new StringBuilder(64);
        readonly List<Change> _undo = new List<Change>();
        readonly List<Change> _redo = new List<Change>();
        readonly Func<string, bool> _isAllowed;

        string _written;
        int _caret;
        int _anchor;
        int _undoLimit = 200;

        // The blink starts again, solid, at the next frame drawn: set as the box takes the keyboard and at every keystroke.
        bool _restartBlink = true;
        int _blinkFrom;

        /// <summary>
        ///     Creates an empty text whose edits made by the user are applied only if <paramref name="isAllowed" />
        ///     accepts the resulting text, or any edit if it is null.
        /// </summary>
        public EditableTextCore(Func<string, bool> isAllowed)
        {
            _isAllowed = isAllowed;
        }

        /// <summary>
        ///     Raised when an edit, undo or redo replaces a run of text, with the start of the run, its former length,
        ///     and the text that replaced it.
        /// </summary>
        /// <remarks>Raised after the text has changed and before the caret moves.</remarks>
        public event Action<int, int, string> Replaced;

        /// <summary>Raised when the caret or the other end of the selection moves.</summary>
        public event Action Placed;

        /// <summary>Raised when the text is changed by typing, cutting, pasting, deleting, undoing or redoing.</summary>
        public event Action Edited;

        /// <summary>The whole text.</summary>
        public string Text => _written ?? (_written = _text.ToString());

        /// <summary>Gets the number of characters in the text.</summary>
        public int Length => _text.Length;

        /// <summary>Gets the caret position.</summary>
        public int CaretIndex => _caret;

        /// <summary>
        ///     Gets the index of the selection end opposite the caret, which equals <see cref="CaretIndex" /> when
        ///     nothing is selected.
        /// </summary>
        public int AnchorIndex => _anchor;

        /// <summary>Gets the index where the selection starts.</summary>
        public int SelectionStart => Math.Min(_caret, _anchor);

        /// <summary>Gets the index where the selection ends.</summary>
        public int SelectionEnd => Math.Max(_caret, _anchor);

        /// <summary>Gets the number of selected characters.</summary>
        public int SelectionLength => SelectionEnd - SelectionStart;

        /// <summary>Whether any text is selected.</summary>
        public bool HasSelection => _caret != _anchor;

        /// <summary>Gets the selected text, or an empty string when nothing is selected.</summary>
        public string SelectedText => HasSelection ? _text.ToString(SelectionStart, SelectionLength) : string.Empty;

        /// <summary>
        ///     Gets or sets the maximum number of characters that typing and pasting can produce, or 0 for no limit.
        /// </summary>
        /// <remarks>Held to as it is edited; text set whole is taken as it is.</remarks>
        public int MaxLength { get; set; }

        /// <summary>Whether the text is read-only: it can be selected and copied but not edited.</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>Gets or sets the maximum number of edits that can be undone.</summary>
        public int UndoLimit
        {
            get { return _undoLimit; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("An undo limit is nought or more.", nameof(value));

                _undoLimit = value;
                while (_undo.Count > _undoLimit)
                    _undo.RemoveAt(0);
            }
        }

        /// <summary>Whether there is an edit to undo.</summary>
        public bool CanUndo => _undo.Count > 0;

        /// <summary>Whether there is an undone edit to redo.</summary>
        public bool CanRedo => _redo.Count > 0;

        /// <summary>Gets the character at <paramref name="index" />.</summary>
        public char this[int index] => _text[index];

        /// <summary>
        ///     Replaces the whole text with <paramref name="text" />, clears the undo history, and keeps the caret and
        ///     selection within the new text.
        /// </summary>
        /// <remarks>Raises only <see cref="Placed" />: the box laying out the text lays it out again itself.</remarks>
        public void Reset(string text)
        {
            _text.Clear();
            if (!string.IsNullOrEmpty(text))
                _text.Append(text);

            _written = null;
            _undo.Clear();
            _redo.Clear();
            Place(Math.Min(_caret, _text.Length), Math.Min(_anchor, _text.Length));
        }

        /// <summary>
        ///     Moves the caret to <paramref name="caret" /> and the selection anchor to <paramref name="anchor" />,
        ///     each clamped to the text.
        /// </summary>
        public void Place(int caret, int anchor)
        {
            caret = Within(caret);
            anchor = Within(anchor);
            if (_caret == caret && _anchor == anchor)
                return;

            _caret = caret;
            _anchor = anchor;
            Placed?.Invoke();
        }

        /// <summary>
        ///     Moves the caret to <paramref name="index" />, extending the selection if <paramref name="select" /> is
        ///     true.
        /// </summary>
        public void MoveCaret(int index, bool select)
        {
            index = Within(index);
            Place(index, select ? _anchor : index);
        }

        /// <summary>
        ///     Selects <paramref name="length" /> characters starting at <paramref name="start" />, clamped to the
        ///     text, with the caret at the end.
        /// </summary>
        public void Select(int start, int length)
        {
            start = Within(start);
            Place(Within(start + Math.Max(0, length)), start);
        }

        /// <summary>Selects all the text, with the caret at the end.</summary>
        public void SelectAll()
        {
            Place(_text.Length, 0);
        }

        /// <summary>
        ///     Selects the run of letters and digits at <paramref name="index" />, or the single character there if it
        ///     is neither.
        /// </summary>
        public void SelectWord(int index)
        {
            index = Within(index);
            var start = index;
            var end = index;

            while (start > 0 && char.IsLetterOrDigit(_text[start - 1]))
                start--;

            while (end < _text.Length && char.IsLetterOrDigit(_text[end]))
                end++;

            if (start == end && end < _text.Length && _text[end] != '\n')
                end++;

            Place(end, start);
        }

        /// <summary>
        ///     Inserts <paramref name="words" /> at the caret, replacing any selection; if <paramref name="typed" /> is
        ///     true, the insertion joins the characters typed just before it, so that one undo reverses them together.
        /// </summary>
        public void Insert(string words, bool typed)
        {
            if (string.IsNullOrEmpty(words))
                return;

            Replace(SelectionStart, SelectionEnd, words, typed);
        }

        /// <summary>Deletes the selection, or the character before the caret if nothing is selected.</summary>
        public void Backspace()
        {
            if (HasSelection)
                Replace(SelectionStart, SelectionEnd, string.Empty, false);
            else if (_caret > 0)
                Replace(_caret - 1, _caret, string.Empty, false);
        }

        /// <summary>Deletes the selection, or the character after the caret if nothing is selected.</summary>
        public void DeleteForward()
        {
            if (HasSelection)
                Replace(SelectionStart, SelectionEnd, string.Empty, false);
            else if (_caret < _text.Length)
                Replace(_caret, _caret + 1, string.Empty, false);
        }

        /// <summary>
        ///     Replaces the text between <paramref name="from" /> and <paramref name="to" /> with
        ///     <paramref name="words" /> as an undoable edit and places the caret after it; <paramref name="typed" />
        ///     has the same meaning as in <see cref="Insert" />.
        /// </summary>
        /// <remarks>
        ///     Nothing is made while it is read only, every line break in the words becomes one character, the words are cut to
        ///     the room <see cref="MaxLength" /> leaves, and an edit whose text would be refused is not made. Characters typed one
        ///     after another are remembered as one edit.
        /// </remarks>
        public void Replace(int from, int to, string words, bool typed)
        {
            if (IsReadOnly)
                return;

            from = Within(from);
            to = Math.Max(from, Within(to));
            words = LineBreaksAsOne(words ?? string.Empty);

            var removed = to - from;
            if (MaxLength > 0)
            {
                var room = Math.Max(0, MaxLength - (_text.Length - removed));
                if (words.Length > room)
                    words = words.Substring(0, room);
            }

            if (removed == 0 && words.Length == 0)
                return;

            if (_isAllowed != null && !_isAllowed(_text.ToString(0, from) + words + _text.ToString(to, _text.Length - to)))
                return;

            var removedText = _text.ToString(from, removed);
            var last = _undo.Count > 0 ? _undo[_undo.Count - 1] : null;
            if (typed && removed == 0 && last != null && last.Typed && last.Removed.Length == 0 && last.At + last.Inserted.Length == from)
                last.Inserted += words;
            else
                Remember(new Change(from, removedText, words, typed, _caret, _anchor));

            _redo.Clear();
            Apply(from, removed, words);
            Place(from + words.Length, from + words.Length);
            Edited?.Invoke();
        }

        /// <summary>Undoes the last edit and restores the caret and selection.</summary>
        public void Undo()
        {
            if (IsReadOnly || _undo.Count == 0)
                return;

            var change = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            Apply(change.At, change.Inserted.Length, change.Removed);
            _redo.Add(change);
            Place(change.CaretBefore, change.AnchorBefore);
            Edited?.Invoke();
        }

        /// <summary>Redoes the last undone edit, with the caret after it.</summary>
        public void Redo()
        {
            if (IsReadOnly || _redo.Count == 0)
                return;

            var change = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            Apply(change.At, change.Removed.Length, change.Inserted);
            Remember(change);
            var end = change.At + change.Inserted.Length;
            Place(end, end);
            Edited?.Invoke();
        }

        /// <summary>
        ///     Runs the Ctrl shortcut for <paramref name="key" /> and returns true if <paramref name="key" /> has one.
        /// </summary>
        /// <remarks>A selects all, C copies, X cuts, V pastes, Z undoes and with Shift redoes, and Y redoes.</remarks>
        public bool Shortcut(IClipboard clipboard, MyKeys key, bool shift)
        {
            switch (key)
            {
                case MyKeys.A:
                    SelectAll();
                    return true;
                case MyKeys.C:
                    if (HasSelection)
                        clipboard.Set(SelectedText);
                    return true;
                case MyKeys.X:
                    if (HasSelection && !IsReadOnly)
                    {
                        clipboard.Set(SelectedText);
                        Backspace();
                    }

                    return true;
                case MyKeys.V:
                    Insert(clipboard.Get(), false);
                    return true;
                case MyKeys.Z:
                    if (shift)
                        Redo();
                    else
                        Undo();
                    return true;
                case MyKeys.Y:
                    Redo();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Makes the caret solid from the next drawn frame, as when the box gets keyboard focus or a key is
        ///     pressed.
        /// </summary>
        public void RestartBlink()
        {
            _restartBlink = true;
        }

        /// <summary>
        ///     Returns true if the caret of a focused box is visible in the frame drawn by <paramref name="g" />: never
        ///     when read-only, otherwise solid after the last keystroke and then alternating between hidden and shown
        ///     every <see cref="CaretBlinkTicks" />.
        /// </summary>
        /// <remarks>Asks <paramref name="g" /> for the next frame whenever the caret blinks.</remarks>
        public bool IsCaretShown(IGraphics g)
        {
            if (IsReadOnly)
                return false;

            // The caret blinks by the clock, so a host that draws only when asked has to draw every frame while it does.
            g.RequestFrame();

            var now = g.Now;
            if (_restartBlink || now < _blinkFrom)
            {
                _blinkFrom = now;
                _restartBlink = false;
            }

            return (now - _blinkFrom) / CaretBlinkTicks % 2 == 0;
        }

        /// <summary>
        ///     Returns <paramref name="words" /> with every line break normalized to a single character, as the text is
        ///     stored.
        /// </summary>
        static string LineBreaksAsOne(string words)
        {
            return words.IndexOf('\r') < 0 ? words : words.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        int Within(int index)
        {
            return Math.Max(0, Math.Min(index, _text.Length));
        }

        void Remember(Change change)
        {
            _undo.Add(change);
            while (_undo.Count > _undoLimit)
                _undo.RemoveAt(0);
        }

        /// <summary>
        ///     Replaces <paramref name="removed" /> characters starting at <paramref name="at" /> with
        ///     <paramref name="words" /> and notifies the box.
        /// </summary>
        void Apply(int at, int removed, string words)
        {
            _text.Remove(at, removed);
            _text.Insert(at, words);
            _written = null;
            Replaced?.Invoke(at, removed, words);
        }

        /// <summary>One edit, stored so that it can be undone and redone.</summary>
        sealed class Change
        {
            public readonly int At;
            public readonly string Removed;
            public readonly bool Typed;
            public readonly int CaretBefore;
            public readonly int AnchorBefore;
            public string Inserted;

            public Change(int at, string removed, string inserted, bool typed, int caretBefore, int anchorBefore)
            {
                At = at;
                Removed = removed;
                Inserted = inserted;
                Typed = typed;
                CaretBefore = caretBefore;
                AnchorBefore = anchorBefore;
            }
        }
    }
}
