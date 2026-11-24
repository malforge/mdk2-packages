using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using VRage.Game;
using VRage.Input;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     A single line of editable text with a caret and a selection, edited with the keyboard and pointer.
    /// </summary>
    /// <remarks>
    ///     Every boundary between characters is measured once each time the text or its font changes, so moving the
    ///     caret or selecting measures nothing.
    /// </remarks>
    public class TextBox : Element, IPadded, ITextual, IEditableText
    {
        readonly EditableTextCore _core;

        Thickness _padding;

        readonly List<float> _edges = new List<float>();

        readonly StringBuilder _run = new StringBuilder(64);

        bool _dragging;
        bool _entering;

        bool _edgesStale = true;
        string _font = MyFontEnum.White;
        float _fontSize = 24f;
        float _caretWidth = 2f;
        string _hint;
        float _hintWidth;
        bool _hintStale = true;

        string _before = "";
        string _after = "";
        float _beforeWidth;
        float _afterWidth;

        float _scroll;

        float _lead;

        /// <summary>
        ///     Creates an empty text box that takes keyboard focus when pressed, using the host theme's text box style
        ///     until it is given a style.
        /// </summary>
        public TextBox()
        {
            _core = new EditableTextCore(IsTextAllowed);
            _core.Replaced += delegate
            {
                Remeasure();
                OnTextChanged();
            };
            _core.Placed += InvalidateArrange;
            _core.Edited += OnEdited;

            Focusable = true;
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            CaretColor = Color.White;
            HintColor = Color.Gray;
        }

        /// <summary>
        ///     Gets or sets the style of this text box, or null to use the host theme's default text box style.
        /// </summary>
        public new IStyle<TextBox> Style
        {
            get { return (IStyle<TextBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this text box's own look, applied in every state on top of the style's look.</summary>
        public new TextBoxLook Base => (TextBoxLook)base.Base;

        /// <summary>Gets this text box's own look when hovered, applied on top of the style's look.</summary>
        public new TextBoxLook PointerOver => (TextBoxLook)base.PointerOver;

        /// <summary>Gets this text box's own look when disabled, applied on top of the style's look.</summary>
        public new TextBoxLook Disabled => (TextBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this text box's own look for <paramref name="state" />, or for every state that includes it,
        ///     applied on top of the style's look.
        /// </summary>
        public new TextBoxLook this[State state] => (TextBoxLook)base[state];

        /// <inheritdoc />
        public event Action Edited;

        /// <summary>Gets the font the text is drawn in.</summary>
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

        /// <summary>Gets the height of one line, in UI units.</summary>
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

        /// <summary>Gets the color of the text.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the color drawn behind the selection.</summary>
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

        /// <summary>Gets how the line is aligned horizontally when it is shorter than the box.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public TextAlignment TextAlignment { get; private set; }

        /// <summary>Gets or sets the text shown when the box is empty and does not have keyboard focus.</summary>
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

        /// <inheritdoc />
        /// <remarks>Held to as it is edited; text set whole is taken as it is.</remarks>
        public int MaxLength
        {
            get { return _core.MaxLength; }
            set { _core.MaxLength = value; }
        }

        /// <inheritdoc />
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

        /// <summary>Gets the color of <see cref="Hint" />.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color HintColor { get; private set; }

        /// <summary>Gets the font of <see cref="Hint" />, or null to use the box's own font.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public string HintFont { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<TextBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TextBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (TextBoxLook)look;
            Font = box.Font ?? MyFontEnum.White;
            FontSize = box.FontSize ?? TextLook.NeutralFontSize;
            Color = box.Color ?? Color.White;
            SelectionColor = box.SelectionColor ?? new Color(40, 90, 150);
            CaretColor = box.CaretColor ?? Color.White;
            CaretWidth = box.CaretWidth ?? 2f;
            HintColor = box.HintColor ?? Color.Gray;

            var hintFont = box.HintFont;
            if (hintFont != HintFont)
            {
                HintFont = hintFont;
                _hintStale = true;
                InvalidateMeasure();
            }

            var alignment = box.TextAlignment ?? TextAlignment.Left;
            if (alignment != TextAlignment)
            {
                TextAlignment = alignment;
                InvalidateArrange();
            }
        }

        /// <summary>
        ///     Whether the box selects all its text when it gains keyboard focus, so that typing replaces it.
        /// </summary>
        public bool SelectsAll { get; set; }

        /// <summary>
        ///     Gets or sets the command that runs when Enter is pressed while the box has focus, or null to leave Enter
        ///     to the element that holds the box.
        /// </summary>
        public ICommand Submit { get; set; }

        /// <summary>
        ///     Gets or sets a function that returns true if the box may hold the text an edit would produce, or null to
        ///     accept anything the kind of box allows.
        /// </summary>
        /// <remarks>Asked before every edit made by typing, cutting, pasting or deleting; an edit it refuses is not made.</remarks>
        public Func<string, bool> FnIsTextAllowed { get; set; }

        /// <inheritdoc />
        public ICommand Cancel { get; set; }

        /// <inheritdoc />
        public string Text
        {
            get { return _core.Text; }
            set
            {
                _core.Reset(value);
                _core.Place(0, 0);
                Remeasure();
                OnTextChanged();
            }
        }

        /// <summary>Gets the number of characters in the box.</summary>
        public int Length => _core.Length;

        /// <inheritdoc />
        public int CaretIndex => _core.CaretIndex;

        /// <inheritdoc />
        public int SelectionStart => _core.SelectionStart;

        /// <inheritdoc />
        public int SelectionLength => _core.SelectionLength;

        /// <inheritdoc />
        public string SelectedText => _core.SelectedText;

        /// <summary>Whether any text is selected.</summary>
        public bool HasSelection => _core.HasSelection;

        /// <summary>Whether there is an edit that can be undone.</summary>
        public bool CanUndo => _core.CanUndo;

        /// <summary>Whether there is an undone edit that can be redone.</summary>
        public bool CanRedo => _core.CanRedo;

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        public void Select(int start, int length)
        {
            _core.Select(start, length);
        }

        /// <inheritdoc />
        public void SelectAll()
        {
            _core.SelectAll();
        }

        /// <inheritdoc />
        public void Undo()
        {
            _core.Undo();
        }

        /// <inheritdoc />
        public void Redo()
        {
            _core.Redo();
        }

        /// <summary>
        ///     Moves the caret to <paramref name="index" />, extending the selection if <paramref name="select" /> is
        ///     true.
        /// </summary>
        public void MoveCaret(int index, bool select)
        {
            _core.MoveCaret(index, select);
        }

        /// <summary>Inserts <paramref name="words" /> at the caret, replacing any selection.</summary>
        public void Insert(string words)
        {
            _core.Insert(words, false);
        }

        /// <summary>Deletes the selection, or the character before the caret.</summary>
        public void Backspace()
        {
            _core.Backspace();
        }

        /// <summary>Deletes the selection, or the character after the caret.</summary>
        public void DeleteForward()
        {
            _core.DeleteForward();
        }

        /// <summary>Raised when the box has taken keyboard focus.</summary>
        public event Action GotFocus;

        /// <summary>Raised when the box has lost keyboard focus.</summary>
        public event Action LostFocus;

        /// <inheritdoc />
        protected override void OnGotFocus(bool byPress)
        {
            _core.RestartBlink();
            if (SelectsAll)
            {
                SelectAll();
                _entering = byPress;
            }

            GotFocus?.Invoke();
        }

        /// <inheritdoc />
        protected override void OnLostFocus()
        {
            LostFocus?.Invoke();
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            // The press that gave it the keyboard keeps what taking it selected, whichever button made it.
            if (_entering)
            {
                _entering = false;
                return;
            }

            if (button != PointerButton.Left)
                return;

            var index = IndexAt(point.X);

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
            MoveCaret(index, false);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            if (_dragging)
                MoveCaret(IndexAt(point.X), true);
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            _dragging = false;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            _dragging = false;
        }

        /// <inheritdoc />
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            _entering = false;
            _core.RestartBlink();
            if (ctrl)
                return _core.Shortcut(input.Clipboard, key, shift);

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
                    if (Submit == null || !Submit.CanExecute)
                        return false;

                    Submit.Execute();
                    return true;
                case MyKeys.Escape:
                    if (Cancel == null || !Cancel.CanExecute)
                        return false;

                    Cancel.Execute();
                    return true;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        protected override void OnTextInput(char c)
        {
            _entering = false;
            _core.RestartBlink();
            if (c < ' ' || c == (char)127)
                return;

            _core.Insert(c.ToString(), true);
        }

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>Gets the space kept clear between the edge and the text.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>Measures the character boundaries if the text or font changed, and the width of the hint.</summary>
        /// <remarks>
        ///     One measure per character, once per change; nothing else about the box measures. The box is never
        ///     narrower than it is tall, so an empty one is still somewhere to type.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_edgesStale)
            {
                MeasureEdges(g);
                _beforeWidth = WidthOf(g, _before);
                _afterWidth = WidthOf(g, _after);
                _edgesStale = false;
            }

            if (_hintStale)
            {
                MeasureHint(g);
                _hintStale = false;
            }

            var width = SurroundWidth + _edges[_edges.Count - 1] + _caretWidth;
            return new Vector2(width > _fontSize ? width : _fontSize, _fontSize);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            KeepCaretInView(bounds.Width);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var at = new Vector2(room.X - _scroll + _lead + BeforeShown, room.Y);

            if (HasSelection && IsFocused)
            {
                var from = _edges[SelectionStart];
                g.Fill(new RectangleF(at.X + from, at.Y, _edges[_core.SelectionEnd] - from, _fontSize), SelectionColor);
            }

            if (_core.Length > 0)
            {
                g.DrawText(_font, Text, at, _fontSize, Color);
                DrawSurround(g, at);
            }
            else if (!IsFocused && !string.IsNullOrEmpty(Hint))
                g.DrawText(HintFont ?? _font, Hint, new Vector2(room.X + Lead(_hintWidth, room.Width), room.Y), _fontSize, HintColor);

            // After the text, so the caret shows inside a selection.
            if (IsFocused && _core.IsCaretShown(g))
                g.Fill(new RectangleF(at.X + _edges[_core.CaretIndex], at.Y, _caretWidth, _fontSize), CaretColor);
        }

        /// <summary>Gets the room the words around the text take, in UI units, as of the last measure.</summary>
        protected float SurroundWidth => _beforeWidth + _afterWidth;

        /// <summary>
        ///     Shows <paramref name="before" /> and <paramref name="after" /> around the text while there is any, which are
        ///     no part of <see cref="Text" /> and the caret never enters.
        /// </summary>
        protected void Surround(string before, string after)
        {
            _before = before;
            _after = after;
            Remeasure();
        }

        /// <summary>Called when the contents of the box change, by an edit or by setting <see cref="Text" />.</summary>
        protected virtual void OnTextChanged()
        {
        }

        /// <summary>
        ///     Returns true if the box may hold <paramref name="text" />, as decided by <see cref="FnIsTextAllowed" />.
        /// </summary>
        /// <remarks>Asked before every edit made by typing, cutting, pasting or deleting; an edit it refuses is not made.</remarks>
        protected virtual bool IsTextAllowed(string text)
        {
            return FnIsTextAllowed == null || FnIsTextAllowed(text);
        }

        void MeasureEdges(IGraphics g)
        {
            _edges.Clear();
            _edges.Add(0f);
            _run.Clear();

            for (var i = 0; i < _core.Length; i++)
            {
                _run.Append(_core[i]);
                _edges.Add(g.MeasureText(_font, _run, _fontSize).X);
            }
        }

        /// <summary>Gets the room the words before the text take while they show.</summary>
        float BeforeShown => _core.Length > 0 ? _beforeWidth : 0f;

        /// <summary>Draws the words around the text that starts at <paramref name="at" />.</summary>
        void DrawSurround(IGraphics g, Vector2 at)
        {
            if (_before.Length > 0)
                g.DrawText(_font, _before, new Vector2(at.X - _beforeWidth, at.Y), _fontSize, Color);

            if (_after.Length > 0)
                g.DrawText(_font, _after, new Vector2(at.X + _edges[_edges.Count - 1], at.Y), _fontSize, Color);
        }

        /// <summary>Returns the width of <paramref name="words" /> in the box's font, or 0 for none.</summary>
        float WidthOf(IGraphics g, string words)
        {
            if (words.Length == 0)
                return 0f;

            _run.Clear();
            _run.Append(words);
            return g.MeasureText(_font, _run, _fontSize).X;
        }

        void MeasureHint(IGraphics g)
        {
            _hintWidth = 0f;
            if (string.IsNullOrEmpty(_hint))
                return;

            _run.Clear();
            _run.Append(_hint);
            _hintWidth = g.MeasureText(HintFont ?? _font, _run, _fontSize).X;
        }

        /// <summary>
        ///     Returns the offset from the left at which a run of <paramref name="width" /> sits in
        ///     <paramref name="room" />, according to <see cref="TextAlignment" />.
        /// </summary>
        float Lead(float width, float room)
        {
            if (width >= room)
                return 0f;

            switch (TextAlignment)
            {
                case TextAlignment.Center:
                    return (room - width) * 0.5f;
                case TextAlignment.Right:
                    return room - width;
                default:
                    return 0f;
            }
        }

        /// <summary>Returns the character boundary nearest <paramref name="x" />.</summary>
        int IndexAt(float x)
        {
            var local = x - ContentBounds.X + _scroll - _lead - BeforeShown;
            var best = 0;

            for (var i = 1; i < _edges.Count; i++)
            {
                if (Math.Abs(_edges[i] - local) < Math.Abs(_edges[best] - local))
                    best = i;
            }

            return best;
        }

        void KeepCaretInView(float width)
        {
            if (_edgesStale || _edges.Count == 0)
                return;

            if (_core.Length > 0)
                width -= SurroundWidth;

            var caretAt = _edges[_core.CaretIndex];
            var end = _edges[_edges.Count - 1] + _caretWidth;

            if (caretAt - _scroll > width - _caretWidth)
                _scroll = caretAt - width + _caretWidth;

            if (caretAt < _scroll)
                _scroll = caretAt;

            if (end - _scroll < width)
                _scroll = Math.Max(0f, end - width);

            _lead = Lead(end, width);
        }

        void Remeasure()
        {
            _edgesStale = true;
            InvalidateMeasure();
        }

        void OnEdited()
        {
            Edited?.Invoke();
        }
    }
}
