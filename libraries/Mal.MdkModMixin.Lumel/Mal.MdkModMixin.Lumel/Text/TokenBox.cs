using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRage.Game;
using VRage.Input;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>
    ///     Editable words with tokens among them, each a pill standing for a thing the application names, such as a
    ///     reference in a formula or a tag; the line wraps where it is longer than the box is wide.
    /// </summary>
    /// <remarks>
    ///     A token is one unit: the caret steps over it, a selection takes it whole, and Backspace or Delete removes it
    ///     whole. Tokens are put in by code, never typed; a press on one raises <see cref="TokenPressed" />. Every character
    ///     is measured once each time the words or the font change, so moving the caret measures nothing.
    /// </remarks>
    public class TokenBox : Element, IPadded, ITextual, ITokenHolder
    {
        // A token stands in the text as one character of the Private Use Area, which no font draws and no player types,
        // so the editing core keeps tokens in order through every edit, undo and redo.
        const char FirstMark = '';
        const char LastMark = '';

        readonly EditableTextCore _core;
        readonly Collection _tokens;
        readonly Dictionary<char, Token> _byMark = new Dictionary<char, Token>();
        readonly Dictionary<Token, char> _markOf = new Dictionary<Token, char>();
        readonly List<float> _advances = new List<float>();
        readonly List<float> _x = new List<float>();
        readonly List<int> _rowOf = new List<int>();
        readonly List<int> _rowStarts = new List<int>();
        readonly List<Stretch> _words = new List<Stretch>();
        readonly StringBuilder _run = new StringBuilder(64);

        Thickness _padding;
        string _font = MyFontEnum.White;
        float _fontSize = 24f;
        float _caretWidth = 2f;
        float _tokenSpacing;
        string _hint;
        float _hintWidth;
        bool _hintStale = true;
        bool _advancesStale = true;
        bool _dragging;
        char _nextMark = FirstMark;

        float _wrappedWidth = float.NaN;
        float _rowHeight;
        float _widest;

        /// <summary>
        ///     Creates an empty token box that takes keyboard focus when pressed, using the host theme's token box style
        ///     until it is given a style.
        /// </summary>
        public TokenBox()
        {
            _tokens = CreateChildren();
            _core = new EditableTextCore(IsTextAllowed);
            _core.Replaced += delegate
            {
                Gather();
                Remeasure();
            };
            _core.Placed += Placed;
            _core.Edited += () => Edited?.Invoke();

            Focusable = true;
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            CaretColor = Color.White;
            HintColor = Color.Gray;
        }

        /// <summary>
        ///     Gets or sets the style of this token box, or null to use the host theme's default token box style.
        /// </summary>
        public new IStyle<TokenBox> Style
        {
            get { return (IStyle<TokenBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this token box's own look, applied in every state on top of the style's look.</summary>
        public new TokenBoxLook Base => (TokenBoxLook)base.Base;

        /// <summary>Gets this token box's own look when hovered, applied on top of the style's look.</summary>
        public new TokenBoxLook PointerOver => (TokenBoxLook)base.PointerOver;

        /// <summary>Gets this token box's own look when disabled, applied on top of the style's look.</summary>
        public new TokenBoxLook Disabled => (TokenBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this token box's own look for <paramref name="state" />, or for every state that includes it, applied
        ///     on top of the style's look.
        /// </summary>
        public new TokenBoxLook this[State state] => (TokenBoxLook)base[state];

        /// <summary>Raised when what the box holds is changed by typing, cutting, pasting, deleting, undoing or redoing.</summary>
        public event Action Edited;

        /// <summary>Raised when a token in the box is pressed, with that token.</summary>
        public event Action<Token> TokenPressed;

        /// <summary>Gets the font the words are drawn in.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public string Font
        {
            get { return _font; }
            private set
            {
                if (_font == value)
                    return;

                _font = value;
                _hintStale = true;
                Remeasure();
            }
        }

        /// <summary>Gets the height of one line of words, in UI units.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float FontSize
        {
            get { return _fontSize; }
            private set
            {
                if (_fontSize == value)
                    return;

                _fontSize = value;
                _hintStale = true;
                Remeasure();
            }
        }

        /// <summary>Gets the color of the words.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the color drawn behind the selected words.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color SelectionColor { get; private set; }

        /// <summary>Gets the color of the caret.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color CaretColor { get; private set; }

        /// <summary>Gets the width of the caret.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float CaretWidth
        {
            get { return _caretWidth; }
            private set
            {
                if (_caretWidth == value)
                    return;

                _caretWidth = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the room on each side of a token, between it and the words beside it.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float TokenSpacing
        {
            get { return _tokenSpacing; }
            private set
            {
                if (_tokenSpacing == value)
                    return;

                _tokenSpacing = value;
                Remeasure();
            }
        }

        /// <summary>Gets the color of <see cref="Hint" />.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color HintColor { get; private set; }

        /// <summary>Gets the font of <see cref="Hint" />, or null to use the box's own font.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public string HintFont { get; private set; }

        /// <summary>Gets or sets the words shown when the box is empty and does not have keyboard focus.</summary>
        public string Hint
        {
            get { return _hint; }
            set
            {
                if (_hint == value)
                    return;

                _hint = value;
                _hintStale = true;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether what the box holds can be selected and copied but not edited.</summary>
        public bool IsReadOnly
        {
            get { return _core.IsReadOnly; }
            set { _core.IsReadOnly = value; }
        }

        /// <summary>Gets or sets the maximum number of edits that can be undone.</summary>
        public int UndoLimit
        {
            get { return _core.UndoLimit; }
            set { _core.UndoLimit = value; }
        }

        /// <summary>
        ///     Gets or sets the command that runs when Enter is pressed while the box has focus, or null to leave Enter
        ///     to the element that holds the box.
        /// </summary>
        public ICommand Submit { get; set; }

        /// <summary>
        ///     Gets or sets the command that runs when Escape is pressed while the box has focus, or null to leave Escape
        ///     to the element that holds the box.
        /// </summary>
        public ICommand Cancel { get; set; }

        /// <summary>Gets the number of positions in the box, a token counting as one.</summary>
        public int Length => _core.Length;

        /// <summary>Gets the caret position, a token counting as one.</summary>
        public int CaretIndex => _core.CaretIndex;

        /// <summary>Gets the position where the selection starts.</summary>
        public int SelectionStart => _core.SelectionStart;

        /// <summary>Gets the number of positions selected, a token counting as one.</summary>
        public int SelectionLength => _core.SelectionLength;

        /// <summary>Whether anything is selected.</summary>
        public bool HasSelection => _core.HasSelection;

        /// <summary>Whether there is an edit that can be undone.</summary>
        public bool CanUndo => _core.CanUndo;

        /// <summary>Whether there is an undone edit that can be redone.</summary>
        public bool CanRedo => _core.CanRedo;

        /// <summary>Gets the space kept clear between the edge and the words.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <summary>
        ///     Gets what the box holds, in order: each run of words between tokens, and each token, in a new list.
        /// </summary>
        public List<TokenBoxPart> Parts
        {
            get
            {
                var parts = new List<TokenBoxPart>();
                var text = _core.Text;
                var start = 0;
                for (var i = 0; i < text.Length; i++)
                {
                    if (!IsMark(text[i]))
                        continue;

                    if (i > start)
                        parts.Add(new TokenBoxPart(text.Substring(start, i - start)));

                    parts.Add(new TokenBoxPart(_byMark[text[i]]));
                    start = i + 1;
                }

                if (start < text.Length)
                    parts.Add(new TokenBoxPart(text.Substring(start)));

                return parts;
            }
        }

        /// <summary>
        ///     Gets what the box holds as words alone, each token written as its <see cref="Controls.Button.Text" />.
        /// </summary>
        public string Words => Written(0, _core.Length);

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>
        ///     Replaces what the box holds with <paramref name="parts" />, clears the undo history and puts the caret at
        ///     the start.
        /// </summary>
        /// <remarks>
        ///     Throws for a token that is in another box, or given twice. Line breaks in the words become spaces, as the
        ///     box holds one line.
        /// </remarks>
        public void SetParts(IEnumerable<TokenBoxPart> parts)
        {
            var given = new List<TokenBoxPart>(parts);
            var seen = new HashSet<Token>();
            foreach (var part in given)
            {
                if (!part.IsToken)
                    continue;

                if (!seen.Add(part.Token))
                    throw new ArgumentException("A token is in a token box once.", nameof(parts));

                if (part.Token.Parent != null && part.Token.Parent != this)
                    throw new ArgumentException("A token is in one token box at a time.", nameof(parts));
            }

            _byMark.Clear();
            _markOf.Clear();
            _nextMark = FirstMark;
            var text = new StringBuilder();
            foreach (var part in given)
            {
                if (part.IsToken)
                    text.Append(Register(part.Token));
                else
                    text.Append(Typeable(part.Text));
            }

            _core.Reset(text.ToString());
            _core.Place(0, 0);
            Gather();
            Remeasure();
        }

        /// <summary>Puts <paramref name="token" /> at the caret, in place of any selection, with the caret after it.</summary>
        /// <remarks>
        ///     Throws for a token already in a token box. Undoable, as an edit is; nothing is put in while it is read only.
        ///     Putting back a token the box held before clears the undo history, which could otherwise bring it back twice.
        /// </remarks>
        public void Insert(Token token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            if (token.Parent != null)
                throw new ArgumentException("A token is in one token box at a time, and once.", nameof(token));

            char mark;
            if (_markOf.TryGetValue(token, out mark))
                ClearHistory();
            else
                mark = Register(token);

            _core.Insert(mark.ToString(), false);
        }

        /// <summary>Puts <paramref name="words" /> at the caret, in place of any selection, with the caret after them.</summary>
        public void Insert(string words)
        {
            _core.Insert(Typeable(words), false);
        }

        /// <summary>Takes <paramref name="token" /> out of the box, as an edit that can be undone.</summary>
        /// <remarks>Throws for a token the box does not hold.</remarks>
        public void Remove(Token token)
        {
            var at = IndexOf(token);
            if (at < 0)
                throw new ArgumentException("The token box does not hold this token.", nameof(token));

            _core.Replace(at, at + 1, "", false);
        }

        /// <summary>Puts <paramref name="with" /> where <paramref name="token" /> is, in its place.</summary>
        /// <remarks>
        ///     Throws for a token the box does not hold, or one to put in that is already in a token box. An undo that brings
        ///     <paramref name="token" /> back brings <paramref name="with" /> instead.
        /// </remarks>
        public void Replace(Token token, Token with)
        {
            if (with == null)
                throw new ArgumentNullException(nameof(with));

            if (IndexOf(token) < 0)
                throw new ArgumentException("The token box does not hold this token.", nameof(token));

            if (with.Parent != null || _markOf.ContainsKey(with))
                throw new ArgumentException("A token is in one token box at a time, and once.", nameof(with));

            var mark = _markOf[token];
            _markOf.Remove(token);
            _byMark[mark] = with;
            _markOf[with] = mark;
            _tokens.Remove(token);
            Gather();
            Remeasure();
        }

        /// <summary>Returns the position of <paramref name="token" /> in the box, or -1 if the box does not hold it.</summary>
        public int IndexOf(Token token)
        {
            char mark;
            if (token == null || token.Parent != this || !_markOf.TryGetValue(token, out mark))
                return -1;

            return _core.Text.IndexOf(mark);
        }

        /// <summary>
        ///     Selects <paramref name="length" /> positions starting at <paramref name="start" />, clamped to what the box
        ///     holds, with the caret at the end.
        /// </summary>
        public void Select(int start, int length)
        {
            _core.Select(start, length);
        }

        /// <summary>Selects everything in the box, with the caret at the end.</summary>
        public void SelectAll()
        {
            _core.SelectAll();
        }

        /// <summary>
        ///     Moves the caret to <paramref name="index" />, extending the selection if <paramref name="select" /> is
        ///     true.
        /// </summary>
        public void MoveCaret(int index, bool select)
        {
            _core.MoveCaret(index, select);
        }

        /// <summary>Undoes the last edit and restores the caret and selection.</summary>
        public void Undo()
        {
            _core.Undo();
        }

        /// <summary>Redoes the last undone edit, with the caret after it.</summary>
        public void Redo()
        {
            _core.Redo();
        }

        /// <summary>Deletes the selection, or the character or token before the caret.</summary>
        public void Backspace()
        {
            _core.Backspace();
        }

        /// <summary>Deletes the selection, or the character or token after the caret.</summary>
        public void DeleteForward()
        {
            _core.DeleteForward();
        }

        /// <inheritdoc />
        void ITokenHolder.Press(Token token)
        {
            TokenPressed?.Invoke(token);
        }

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<TokenBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TokenBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (TokenBoxLook)look;
            Font = box.Font ?? MyFontEnum.White;
            FontSize = box.FontSize ?? TextLook.NeutralFontSize;
            Color = box.Color ?? Color.White;
            SelectionColor = box.SelectionColor ?? new Color(40, 90, 150);
            CaretColor = box.CaretColor ?? Color.White;
            CaretWidth = box.CaretWidth ?? 2f;
            HintColor = box.HintColor ?? Color.Gray;
            TokenSpacing = box.TokenSpacing ?? 0f;

            var hintFont = box.HintFont;
            if (hintFont == HintFont)
                return;

            HintFont = hintFont;
            _hintStale = true;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override void OnGotFocus(bool byPress)
        {
            _core.RestartBlink();
            Placed();
        }

        /// <inheritdoc />
        protected override void OnLostFocus()
        {
            Placed();
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            var index = IndexAt(point);
            if (clickCount >= 3)
            {
                SelectAll();
                return;
            }

            if (clickCount == 2)
            {
                _core.SelectWord(index);
                return;
            }

            _dragging = true;
            MoveCaret(index, shift);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button == PointerButton.Left && _dragging)
                MoveCaret(IndexAt(point), true);
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button == PointerButton.Left)
                _dragging = false;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            _dragging = false;
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Ctrl+C and Ctrl+X copy the selection as words, each token as its text; Ctrl+V pastes words alone. The other
        ///     Ctrl keys are those of a text box.
        /// </remarks>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            _core.RestartBlink();
            if (ctrl)
                return Shortcut(input.Clipboard, key, shift);

            if (IsReadOnly && (key == MyKeys.Back || key == MyKeys.Delete))
                return false;

            var caret = _core.CaretIndex;
            switch (key)
            {
                case MyKeys.Left:
                    MoveCaret(!shift && HasSelection ? SelectionStart : caret - 1, shift);
                    return true;
                case MyKeys.Right:
                    MoveCaret(!shift && HasSelection ? _core.SelectionEnd : caret + 1, shift);
                    return true;
                case MyKeys.Home:
                    MoveCaret(0, shift);
                    return true;
                case MyKeys.End:
                    MoveCaret(_core.Length, shift);
                    return true;
                case MyKeys.Back:
                    Backspace();
                    return true;
                case MyKeys.Delete:
                    DeleteForward();
                    return true;
                case MyKeys.Enter:
                    return Run(Submit);
                case MyKeys.Escape:
                    return Run(Cancel);
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        protected override void OnTextInput(char c)
        {
            _core.RestartBlink();
            if (c < ' ' || c == (char)127 || IsMark(c))
                return;

            _core.Insert(c.ToString(), true);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Measures the words once per change, then lays the line out across the width given, breaking it after a
        ///     space or beside a token. The box is never narrower than it is tall.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_advancesStale)
            {
                MeasureAdvances(g);
                _advancesStale = false;
            }

            if (_hintStale)
            {
                _hintWidth = string.IsNullOrEmpty(_hint) ? 0f : WidthOf(g, HintFont ?? _font, _hint);
                _hintStale = false;
            }

            _rowHeight = _fontSize;
            foreach (var token in _tokens)
            {
                token.Measure(g, new Vector2(float.PositiveInfinity, float.PositiveInfinity));
                _rowHeight = Math.Max(_rowHeight, token.DesiredSize.Y);
            }

            Wrap(available.X - _caretWidth);
            var width = Math.Max(_widest, _core.Length == 0 ? _hintWidth : 0f) + _caretWidth;
            return new Vector2(Math.Max(width, _rowHeight), _rowStarts.Count * _rowHeight);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (bounds.Width - _caretWidth != _wrappedWidth)
                Wrap(bounds.Width - _caretWidth);

            var text = _core.Text;
            for (var i = 0; i < text.Length; i++)
            {
                if (!IsMark(text[i]))
                    continue;

                var token = _byMark[text[i]];
                var size = token.DesiredSize;
                var top = bounds.Y + _rowOf[i] * _rowHeight + (_rowHeight - size.Y) * 0.5f;
                token.Arrange(new RectangleF(bounds.X + _x[i] + _tokenSpacing, top, size.X, size.Y));
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var lift = (_rowHeight - _fontSize) * 0.5f;
            if (HasSelection && IsFocused)
                DrawSelection(g, room, lift);

            foreach (var words in _words)
                g.DrawText(_font, words.Text, new Vector2(room.X + words.X, room.Y + words.Row * _rowHeight + lift), _fontSize, Color);

            foreach (var token in _tokens)
                token.Draw(g);

            if (_core.Length == 0 && !IsFocused && !string.IsNullOrEmpty(_hint))
                g.DrawText(HintFont ?? _font, _hint, new Vector2(room.X, room.Y + lift), _fontSize, HintColor);

            // After the words and tokens, so the caret shows over a selection.
            if (!IsFocused || !_core.IsCaretShown(g) || _x.Count == 0)
                return;

            var caret = _core.CaretIndex;
            var at = new Vector2(room.X + _x[caret], room.Y + _rowOf[caret] * _rowHeight + lift);
            g.Fill(new RectangleF(at.X, at.Y, _caretWidth, _fontSize), CaretColor);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return OnTop(_tokens, point);
        }

        /// <summary>
        ///     Returns true if the box may hold <paramref name="text" />: one line, with every token in it once and every
        ///     token's character one the box gave out.
        /// </summary>
        bool IsTextAllowed(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\n')
                    return false;

                if (IsMark(c) && (!_byMark.ContainsKey(c) || text.IndexOf(c, i + 1) >= 0))
                    return false;
            }

            return true;
        }

        /// <summary>Runs <paramref name="command" /> and returns true if there is one that can run.</summary>
        static bool Run(ICommand command)
        {
            if (command == null || !command.CanExecute)
                return false;

            command.Execute();
            return true;
        }

        /// <summary>Returns true if <paramref name="c" /> is a character that stands for a token.</summary>
        static bool IsMark(char c)
        {
            return c >= FirstMark && c <= LastMark;
        }

        /// <summary>Returns <paramref name="words" /> on one line, each line break a space.</summary>
        static string OneLine(string words)
        {
            return words.IndexOf('\r') < 0 && words.IndexOf('\n') < 0 ? words : words.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        }

        /// <summary>Returns <paramref name="words" /> on one line, with every character that stands for a token taken out.</summary>
        static string Typeable(string words)
        {
            words = OneLine(words ?? "");
            for (var i = 0; i < words.Length; i++)
            {
                if (!IsMark(words[i]))
                    continue;

                var kept = new StringBuilder(words.Length);
                foreach (var c in words)
                {
                    if (!IsMark(c))
                        kept.Append(c);
                }

                return kept.ToString();
            }

            return words;
        }

        /// <summary>
        ///     Runs the Ctrl shortcut for <paramref name="key" /> and returns true if <paramref name="key" /> has one.
        /// </summary>
        bool Shortcut(IClipboard clipboard, MyKeys key, bool shift)
        {
            switch (key)
            {
                case MyKeys.C:
                    if (HasSelection)
                        clipboard.Set(Written(SelectionStart, _core.SelectionEnd));
                    return true;
                case MyKeys.X:
                    if (HasSelection && !IsReadOnly)
                    {
                        clipboard.Set(Written(SelectionStart, _core.SelectionEnd));
                        Backspace();
                    }

                    return true;
                case MyKeys.V:
                    Insert(clipboard.Get());
                    return true;
                default:
                    return _core.Shortcut(clipboard, key, shift);
            }
        }

        /// <summary>Returns the positions from <paramref name="from" /> to <paramref name="to" /> as words, each token as its text.</summary>
        string Written(int from, int to)
        {
            var text = _core.Text;
            var words = new StringBuilder(to - from);
            for (var i = from; i < to; i++)
            {
                var c = text[i];
                words.Append(IsMark(c) ? _byMark[c].Text : c.ToString());
            }

            return words.ToString();
        }

        /// <summary>Gives <paramref name="token" /> a character to stand for it and returns it.</summary>
        /// <remarks>Throws when every character is given out to a token the box holds.</remarks>
        char Register(Token token)
        {
            var mark = FreeMark();
            _byMark[mark] = token;
            _markOf[token] = mark;
            return mark;
        }

        /// <summary>
        ///     Returns a character no token has, after forgetting the tokens the box no longer holds, and the undo history
        ///     that could bring them back, where none is left.
        /// </summary>
        char FreeMark()
        {
            for (var pass = 0; pass < 2; pass++)
            {
                for (var tried = 0; tried <= LastMark - FirstMark; tried++)
                {
                    var mark = _nextMark;
                    _nextMark = mark == LastMark ? FirstMark : (char)(mark + 1);
                    if (!_byMark.ContainsKey(mark))
                        return mark;
                }

                Forget();
            }

            throw new InvalidOperationException("A token box holds at most " + (LastMark - FirstMark + 1) + " tokens.");
        }

        /// <summary>Forgets every token the box no longer holds, with the undo history that could bring one back.</summary>
        void Forget()
        {
            var text = _core.Text;
            var gone = new List<char>();
            foreach (var pair in _byMark)
            {
                if (text.IndexOf(pair.Key) < 0)
                    gone.Add(pair.Key);
            }

            foreach (var mark in gone)
            {
                _markOf.Remove(_byMark[mark]);
                _byMark.Remove(mark);
            }

            ClearHistory();
        }

        /// <summary>Clears the undo history, keeping what the box holds, the caret and the selection.</summary>
        void ClearHistory()
        {
            var caret = _core.CaretIndex;
            var anchor = _core.AnchorIndex;
            _core.Reset(_core.Text);
            _core.Place(caret, anchor);
        }

        /// <summary>Makes the tokens the box holds its children, and only those, and marks the ones selected.</summary>
        void Gather()
        {
            var text = _core.Text;
            for (var i = _tokens.Count - 1; i >= 0; i--)
            {
                var token = (Token)_tokens[i];
                char mark;
                if (!_markOf.TryGetValue(token, out mark) || text.IndexOf(mark) < 0)
                {
                    token.Set(Token.Selected, false);
                    _tokens.Remove(token);
                }
            }

            foreach (var c in text)
            {
                if (!IsMark(c))
                    continue;

                var token = _byMark[c];
                if (token.Parent != this)
                    _tokens.Add(token);
            }

            Placed();
        }

        /// <summary>Marks each token as selected or not as the selection now stands, and lays the caret out again.</summary>
        void Placed()
        {
            var text = _core.Text;
            var from = _core.SelectionStart;
            var to = _core.SelectionEnd;
            for (var i = 0; i < text.Length; i++)
            {
                if (IsMark(text[i]))
                    _byMark[text[i]].Set(Token.Selected, IsFocused && i >= from && i < to);
            }

            InvalidateArrange();
        }

        /// <summary>Measures how far each character of words moves the line on, within the run of words it is in.</summary>
        void MeasureAdvances(IGraphics g)
        {
            _advances.Clear();
            _run.Clear();
            var before = 0f;
            var text = _core.Text;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (IsMark(c))
                {
                    _advances.Add(0f);
                    _run.Clear();
                    before = 0f;
                    continue;
                }

                _run.Append(c);
                var width = g.MeasureText(_font, _run, _fontSize).X;
                _advances.Add(width - before);
                before = width;
            }
        }

        /// <summary>Returns how far the character at <paramref name="index" /> moves the line on, a token with the room around it.</summary>
        float Advance(int index)
        {
            var c = _core[index];
            return IsMark(c) ? _byMark[c].DesiredSize.X + 2f * _tokenSpacing : _advances[index];
        }

        /// <summary>
        ///     Breaks the line into rows no wider than <paramref name="width" />, after a space or beside a token where it
        ///     can and anywhere where a word is too long, and places every position and run of words on them.
        /// </summary>
        void Wrap(float width)
        {
            _wrappedWidth = width;
            _rowStarts.Clear();
            _rowStarts.Add(0);
            var length = _core.Length;
            var x = 0f;
            var start = 0;
            var lastBreak = -1;
            for (var i = 0; i < length;)
            {
                var advance = Advance(i);
                if (x + advance > width && i > start)
                {
                    start = lastBreak > start ? lastBreak : i;
                    _rowStarts.Add(start);
                    x = 0f;
                    lastBreak = -1;
                    i = start;
                    continue;
                }

                x += advance;
                var c = _core[i];
                if (c == ' ' || IsMark(c) || (i + 1 < length && IsMark(_core[i + 1])))
                    lastBreak = i + 1;

                i++;
            }

            Place();
        }

        /// <summary>Works out where every position and every run of words sits on the rows, and the widest row.</summary>
        void Place()
        {
            _x.Clear();
            _rowOf.Clear();
            _words.Clear();
            _widest = 0f;
            var text = _core.Text;
            var row = 0;
            var x = 0f;
            var wordsFrom = -1;
            for (var i = 0; i <= text.Length; i++)
            {
                var rowStarts = row + 1 < _rowStarts.Count && _rowStarts[row + 1] == i;
                var ends = i == text.Length || rowStarts || IsMark(text[i]);
                if (ends && wordsFrom >= 0)
                {
                    _words.Add(new Stretch(text.Substring(wordsFrom, i - wordsFrom), _x[wordsFrom], row));
                    wordsFrom = -1;
                }

                if (rowStarts)
                {
                    row++;
                    x = 0f;
                }

                _x.Add(x);
                _rowOf.Add(row);
                if (i == text.Length)
                    break;

                if (!IsMark(text[i]) && wordsFrom < 0)
                    wordsFrom = i;

                x += Advance(i);
                _widest = Math.Max(_widest, x);
            }
        }

        /// <summary>
        ///     Fills behind the selected words, one stretch for each run of them on a row; a selected token shows it by its
        ///     own look.
        /// </summary>
        void DrawSelection(IGraphics g, RectangleF room, float lift)
        {
            var text = _core.Text;
            var to = _core.SelectionEnd;
            for (var from = _core.SelectionStart; from < to;)
            {
                if (IsMark(text[from]))
                {
                    from++;
                    continue;
                }

                var row = _rowOf[from];
                var end = from;
                while (end < to && !IsMark(text[end]) && _rowOf[end] == row)
                    end++;

                var left = _x[from];
                var right = _x[end - 1] + Advance(end - 1);
                g.Fill(new RectangleF(room.X + left, room.Y + row * _rowHeight + lift, right - left, _fontSize), SelectionColor);
                from = end;
            }
        }

        /// <summary>Returns the position nearest <paramref name="point" />, on the row it is level with.</summary>
        int IndexAt(Vector2 point)
        {
            if (_x.Count == 0)
                return 0;

            var room = ContentBounds;
            var row = _rowHeight > 0f ? (int)Math.Floor((point.Y - room.Y) / _rowHeight) : 0;
            row = Math.Max(0, Math.Min(row, _rowStarts.Count - 1));
            var local = point.X - room.X;
            var best = -1;
            for (var i = 0; i < _x.Count; i++)
            {
                if (_rowOf[i] != row)
                    continue;

                if (best < 0 || Math.Abs(_x[i] - local) < Math.Abs(_x[best] - local))
                    best = i;
            }

            return best < 0 ? 0 : best;
        }

        /// <summary>Returns the width of <paramref name="words" /> in <paramref name="font" />.</summary>
        float WidthOf(IGraphics g, string font, string words)
        {
            _run.Clear();
            _run.Append(words);
            return g.MeasureText(font, _run, _fontSize).X;
        }

        void Remeasure()
        {
            _advancesStale = true;
            _wrappedWidth = float.NaN;
            InvalidateMeasure();
        }

        /// <summary>A run of words on one row, between tokens or the row's ends.</summary>
        struct Stretch
        {
            public readonly string Text;
            public readonly float X;
            public readonly int Row;

            public Stretch(string text, float x, int row)
            {
                Text = text;
                X = x;
                Row = row;
            }
        }
    }
}
