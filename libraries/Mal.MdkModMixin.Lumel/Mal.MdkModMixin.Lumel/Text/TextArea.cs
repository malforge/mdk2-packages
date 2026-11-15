using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Viewing;
using VRage.Game;
using VRage.Input;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>A multi-line text area that scrolls in both directions and can be edited or read-only.</summary>
    /// <remarks>
    /// Read only, a press on the text drags it about; edited, it places the caret and a drag selects. Drawing a frame
    /// measures only the lines coming into view.
    /// </remarks>
    public partial class TextArea : Element, IPadded, ITextual, IEditableText
    {
        /// <summary>The state while a scroll bar thumb is being dragged by the pointer.</summary>
        public static readonly State Dragging = State.Declare("Dragging", 400);

        static readonly string[] Endings = { "\r\n", "\n", "\r" };

        readonly EditableTextCore _core = new EditableTextCore(null);
        readonly ScrollAxis _across = new ScrollAxis();
        readonly ScrollAxis _down = new ScrollAxis();
        readonly List<string> _lines = new List<string>();
        readonly List<float> _widths = new List<float>();
        readonly Gutter _gutter = new Gutter();
        readonly StringBuilder _scratch = new StringBuilder(128);
        readonly Dictionary<int, List<float>> _edges = new Dictionary<int, List<float>>();
        readonly List<List<float>> _spareEdges = new List<List<float>>();
        readonly List<int> _dropped = new List<int>();

        Thickness _padding;
        Color _restingThumb;
        TextAreaLook _resting;
        float _scrollBarWidth;
        float _scrollBarGap;
        float _caretWidth = 2f;
        string _font;
        float _fontSize;
        bool _showLineNumbers;
        Thickness? _lineNumbersPadding;
        float _tall;
        float _widest;
        int _shownFrom = int.MaxValue;
        int _shownTo = int.MinValue;
        Held _held;
        Vector2 _from;
        Spot _caret;
        Spot _anchor;
        float _goal = -1f;
        bool _follow;
        bool _settling;
        Asked _asked;
        Vector2 _askedAt;
        int _askedLine;
        bool _askedSelects;

        /// <summary>
        ///     Creates an empty white text area with lines 24 units tall that takes keyboard focus when pressed.
        /// </summary>
        public TextArea()
        {
            _font = MyFontEnum.White;
            _fontSize = 24f;
            _lines.Add(string.Empty);
            _widths.Add(0f);
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            CaretColor = Color.White;
            HintColor = Color.Gray;
            Focusable = true;

            _core.Replaced += OnReplaced;
            _core.Placed += OnPlaced;
            _core.Edited += OnEdited;
        }

        /// <summary>
        ///     Gets or sets the style of this text area, or null to use the default text area style of its host theme.
        /// </summary>
        public new IStyle<TextArea> Style
        {
            get { return (IStyle<TextArea>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this text area's own look for every state, applied over its style.</summary>
        public new TextAreaLook Base => (TextAreaLook)base.Base;

        /// <summary>Gets this text area's own look on hover, applied over its style.</summary>
        public new TextAreaLook PointerOver => (TextAreaLook)base.PointerOver;

        /// <summary>Gets this text area's own look when disabled, applied over its style.</summary>
        public new TextAreaLook Disabled => (TextAreaLook)base.Disabled;

        /// <summary>
        ///     Gets this text area's own look for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style.
        /// </summary>
        public new TextAreaLook this[State state] => (TextAreaLook)base[state];

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<TextArea>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TextAreaLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (TextAreaLook)look;
            Font = box.Font ?? MyFontEnum.White;
            FontSize = box.FontSize ?? TextLook.NeutralFontSize;
            Color = box.Color ?? Color.White;
            SelectionColor = box.SelectionColor ?? new Color(40, 90, 150);
            CaretColor = box.CaretColor ?? Color.White;
            CaretWidth = box.CaretWidth ?? 2f;
            HintColor = box.HintColor ?? Color.Gray;
            HintFont = box.HintFont;
            LineNumberColor = box.LineNumberColor ?? Color.Transparent;
            LineNumbersPadding = box.LineNumbersPadding;
            LineBreakSelectionWidth = box.LineBreakSelectionWidth;
            ScrollBarWidth = box.ScrollBarWidth ?? 0f;
            ScrollBarGap = box.ScrollBarGap ?? 0f;
            Thumb = box.Thumb ?? Color.Transparent;
            MinThumbLength = box.MinThumbLength ?? 0f;
            _restingThumb = IsDragging ? RestingThumb() : Thumb;
        }

        /// <summary>
        ///     Returns the thumb color from this text area's looks when no thumb is held, used for a scroll bar that is
        ///     not being dragged.
        /// </summary>
        Color RestingThumb()
        {
            var resting = _resting ?? (_resting = (TextAreaLook)NewLook());
            Lay(resting, States.Except(Dragging));
            return resting.Thumb ?? Color.Transparent;
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _core.Text; }
            set
            {
                var incoming = value ?? string.Empty;
                _lines.Clear();
                _widths.Clear();
                _lines.AddRange(incoming.Split(Endings, StringSplitOptions.None));
                for (var i = 0; i < _lines.Count; i++)
                    _widths.Add(-1f);

                _core.Reset(string.Join("\n", _lines));
                _core.Place(0, 0);
                _caret = new Spot(0, 0);
                _anchor = _caret;
                _goal = -1f;
                Forget();
                InvalidateMeasure();
            }
        }

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
                Forget();
                InvalidateMeasure();
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
                Forget();
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the color of the text.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the color painted behind the selection.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color SelectionColor { get; private set; }

        /// <summary>The color of the caret.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color CaretColor { get; private set; }

        /// <summary>How wide the caret is drawn.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float CaretWidth
        {
            get { return _caretWidth; }
            private set
            {
                if (_caretWidth == value)
                    return;

                _caretWidth = value;
                InvalidateArrange();
            }
        }

        /// <summary>Gets or sets the text shown while the box is empty and does not have keyboard focus.</summary>
        public string Hint { get; set; }

        /// <summary>Gets the color <see cref="Hint" /> is drawn in.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color HintColor { get; private set; }

        /// <summary>Gets the font <see cref="Hint" /> is drawn in, or null to use the box's own.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public string HintFont { get; private set; }

        /// <inheritdoc />
        public ICommand Cancel { get; set; }

        /// <inheritdoc />
        /// <remarks>A press on the text then drags it about.</remarks>
        public bool IsReadOnly
        {
            get { return _core.IsReadOnly; }
            set
            {
                if (_core.IsReadOnly == value)
                    return;

                _core.IsReadOnly = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether each line is numbered in a gutter on the left.</summary>
        public bool ShowLineNumbers
        {
            get { return _showLineNumbers; }
            set
            {
                if (_showLineNumbers == value)
                    return;

                _showLineNumbers = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the color of the line numbers.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color LineNumberColor { get; private set; }

        /// <summary>
        ///     Gets the space on either side of the line numbers, the right side lying between them and the text, or
        ///     null for the width of a digit on each side.
        /// </summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Thickness? LineNumbersPadding
        {
            get { return _lineNumbersPadding; }
            private set
            {
                if (_lineNumbersPadding == value)
                    return;

                _lineNumbersPadding = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Gets how far the selection extends past the end of a line that continues on the next, or null for a
        ///     quarter of a line's height.
        /// </summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float? LineBreakSelectionWidth { get; private set; }

        /// <summary>Gets the thickness of the scroll bars, or 0 for none.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float ScrollBarWidth
        {
            get { return _scrollBarWidth; }
            private set
            {
                if (_scrollBarWidth == value)
                    return;

                _scrollBarWidth = value;
                InvalidateArrange();
            }
        }

        /// <summary>Gets the space between the text and the scroll bars.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float ScrollBarGap
        {
            get { return _scrollBarGap; }
            private set
            {
                if (_scrollBarGap == value)
                    return;

                _scrollBarGap = value;
                InvalidateArrange();
            }
        }

        /// <summary>Gets the color of the scroll bar thumbs.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Thumb { get; private set; }

        /// <summary>Gets the minimum length of a thumb.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float MinThumbLength
        {
            get { return _down.MinThumbLength; }
            private set
            {
                _down.MinThumbLength = value;
                _across.MinThumbLength = value;
            }
        }

        /// <summary>Whether a thumb is being dragged by the pointer.</summary>
        public bool IsDragging
        {
            get { return Is(Dragging); }
            private set { Set(Dragging, value); }
        }

        /// <inheritdoc />
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

        /// <summary>Gets the number of lines.</summary>
        public int LineCount => _lines.Count;

        /// <summary>Gets the number of characters, counting a line break as one.</summary>
        public int Length => _core.Length;

        /// <inheritdoc />
        /// <remarks>Held to as it is edited; text set whole is taken as it is.</remarks>
        public int MaxLength
        {
            get { return _core.MaxLength; }
            set { _core.MaxLength = value; }
        }

        /// <summary>Gets or sets the maximum number of edits that can be undone.</summary>
        public int UndoLimit
        {
            get { return _core.UndoLimit; }
            set { _core.UndoLimit = value; }
        }

        /// <summary>Gets the height of the whole content, once it has been measured.</summary>
        public float ExtentHeight => _down.Extent;

        /// <summary>Gets the width of the widest line shown so far.</summary>
        public float ExtentWidth => _across.Extent;

        /// <summary>Whether the text itself is being dragged.</summary>
        public bool IsPanning => _held == Held.Text;

        /// <inheritdoc />
        public int CaretIndex => _core.CaretIndex;

        /// <summary>Gets the zero-based index of the line the caret is on.</summary>
        public int CaretLine => _caret.Line;

        /// <summary>Gets the caret's position within its line, in characters.</summary>
        public int CaretColumn => _caret.Column;

        /// <inheritdoc />
        public int SelectionStart => _core.SelectionStart;

        /// <inheritdoc />
        public int SelectionLength => _core.SelectionLength;

        /// <inheritdoc />
        public string SelectedText => _core.SelectedText;

        /// <summary>Whether any text is selected.</summary>
        public bool HasSelection => _core.HasSelection;

        /// <summary>Whether there is an edit to undo.</summary>
        public bool CanUndo => _core.CanUndo;

        /// <summary>Whether there is an undone edit to redo.</summary>
        public bool CanRedo => _core.CanRedo;

        /// <summary>Gets how far down the text is scrolled, in UI units.</summary>
        public float VerticalOffset => _down.Offset;

        /// <summary>Gets how far across the text is scrolled, in UI units.</summary>
        public float HorizontalOffset => _across.Offset;

        /// <summary>Whether the content extends beyond the visible area above or below.</summary>
        public bool CanScroll => _down.CanMove;

        /// <summary>Whether the content extends beyond the visible area to the left or right.</summary>
        public bool CanScrollHorizontally => _across.CanMove;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>The area available for the text, after the scroll bars have taken their share.</summary>
        RectangleF View
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X, room.Y, Room(room.Width, BarRoom), Room(room.Height, BarRoom));
            }
        }

        /// <summary>The room each bar takes from the text: the bar and the gap beside it, or nothing for no bars.</summary>
        float BarRoom => _scrollBarWidth > 0f ? _scrollBarWidth + _scrollBarGap : 0f;

        /// <summary>
        ///     The horizontal position where the text starts, after the line numbers have taken their share.
        /// </summary>
        float WordsX => View.X + _gutter.Width;

        Spot SelectionFirst => _caret.Before(_anchor) ? _caret : _anchor;

        Spot SelectionLast => _caret.Before(_anchor) ? _anchor : _caret;

        /// <summary>Whether the text is empty, that is, a single line with no characters.</summary>
        bool IsEmpty => _lines.Count == 1 && _lines[0].Length == 0;

        /// <summary>The position at the end of the text.</summary>
        Spot End => new Spot(_lines.Count - 1, _lines[_lines.Count - 1].Length);

        /// <summary>The number of lines visible at once, at least one.</summary>
        int Page => Math.Max(1, _fontSize > 0f ? (int)(_down.Viewport / _fontSize) : 1);

        /// <summary>
        ///     The horizontal position of the caret within its line, or 0 if the line has not been measured yet.
        /// </summary>
        float CaretX => XOf(_caret);

        /// <summary>Scrolls the text vertically to <paramref name="offset" />.</summary>
        /// <remarks>Kept inside what it holds at once, or, while it waits to be measured again, at the next layout, so an offset set with new content in the same frame is measured against that content.</remarks>
        public void ScrollToVerticalOffset(float offset)
        {
            var held = IsMeasureValid ? _down.Clamp(offset) : Math.Max(0f, offset);
            if (_down.Offset == held)
                return;

            _down.Offset = held;
            InvalidateArrange();
        }

        /// <summary>Scrolls the text horizontally to <paramref name="offset" />.</summary>
        /// <remarks>Kept inside what it holds at once, or, while it waits to be measured again, at the next layout, so an offset set with new content in the same frame is measured against that content.</remarks>
        public void ScrollToHorizontalOffset(float offset)
        {
            var held = IsMeasureValid ? _across.Clamp(offset) : Math.Max(0f, offset);
            if (_across.Offset == held)
                return;

            _across.Offset = held;
            InvalidateArrange();
        }

        /// <summary>
        ///     Scrolls vertically by <paramref name="amount" />, down when positive, clamped to the content.
        /// </summary>
        public void ScrollBy(float amount)
        {
            ScrollToVerticalOffset(_down.Offset + amount);
        }

        /// <summary>
        ///     Scrolls horizontally by <paramref name="amount" />, right when positive, clamped to the content.
        /// </summary>
        public void ScrollHorizontallyBy(float amount)
        {
            ScrollToHorizontalOffset(_across.Offset + amount);
        }

        /// <inheritdoc />
        /// <remarks>Takes the wheel while any of what it holds is out of sight.</remarks>
        protected override bool OnWheel(float amount)
        {
            if (!CanScroll)
                return false;

            ScrollBy(amount);
            return true;
        }

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

        /// <summary>
        ///     Selects the whole of <paramref name="line" /> (zero-based, clamped to the text) and scrolls it into
        ///     view.
        /// </summary>
        /// <remarks>The caret goes to the start of the line, so a long line is shown from its beginning.</remarks>
        public void SelectLine(int line)
        {
            var start = Within(new Spot(line, 0));
            Place(start, new Spot(start.Line, _lines[start.Line].Length));
            Follow();
        }

        /// <summary>
        ///     Moves the caret to <paramref name="line" /> and <paramref name="column" />, clamped to the text, and
        ///     extends the selection if <paramref name="select" /> is true.
        /// </summary>
        public void MoveCaret(int line, int column, bool select)
        {
            var at = Within(new Spot(line, column));
            Place(at, select ? _anchor : at);
        }

        /// <summary>
        ///     Inserts <paramref name="words" /> at the caret, replacing any selection; line breaks in it start new
        ///     lines.
        /// </summary>
        public void Insert(string words)
        {
            if (string.IsNullOrEmpty(words))
                return;

            _core.Insert(words, false);
        }

        /// <summary>
        ///     Deletes the selection, or the character before the caret, joining lines when the caret is at the start
        ///     of one.
        /// </summary>
        public void Backspace()
        {
            _core.Backspace();
        }

        /// <summary>
        ///     Deletes the selection, or the character after the caret, joining lines when the caret is at the end of
        ///     one.
        /// </summary>
        public void DeleteForward()
        {
            _core.DeleteForward();
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

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            _from = point;

            if (Grab(Held.Down, point))
            {
                _held = Held.Down;
                IsDragging = true;
                return;
            }

            if (Grab(Held.Across, point))
            {
                _held = Held.Across;
                IsDragging = true;
                return;
            }

            if (IsReadOnly)
            {
                _held = Inside(View, point) && (_down.CanMove || _across.CanMove) ? Held.Text : Held.Nothing;
                return;
            }

            _held = Held.Selecting;
            Ask(clickCount >= 3 ? Asked.Line : clickCount == 2 ? Asked.Word : Asked.Point, point, shift);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            if (_held == Held.Down)
                ScrollToVerticalOffset(_down.Dragged(point.Y - _from.Y));
            else if (_held == Held.Across)
                ScrollToHorizontalOffset(_across.Dragged(point.X - _from.X));
            else if (_held == Held.Text)
            {
                ScrollBy(_from.Y - point.Y);
                ScrollHorizontallyBy(_from.X - point.X);
            }
            else if (_held == Held.Selecting)
                Ask(Asked.Point, point, true);

            _from = point;
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            _held = Held.Nothing;
            IsDragging = false;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            _held = Held.Nothing;
            IsDragging = false;
        }

        /// <inheritdoc />
        protected override void OnGotFocus(bool byPress)
        {
            _core.RestartBlink();

            // Its caret's line is measured from now on, for the keys that move the caret.
            if (IsReadOnly)
                InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Read only, it takes the keys that move the caret, select and copy, and Escape.</remarks>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            _core.RestartBlink();

            if (ctrl && _core.Shortcut(input.Clipboard, key, shift))
                return true;

            if (key == MyKeys.Escape)
            {
                if (Cancel == null || !Cancel.CanExecute)
                    return false;

                Cancel.Execute();
                return true;
            }

            if (IsReadOnly && (key == MyKeys.Back || key == MyKeys.Delete || key == MyKeys.Enter))
                return false;

            switch (key)
            {
                case MyKeys.Left:
                    Across(ctrl ? Word(_caret, -1) : !shift && HasSelection ? SelectionFirst : Step(_caret, -1), shift);
                    return true;
                case MyKeys.Right:
                    Across(ctrl ? Word(_caret, 1) : !shift && HasSelection ? SelectionLast : Step(_caret, 1), shift);
                    return true;
                case MyKeys.Up:
                    Vertical(_caret.Line - 1, shift);
                    return true;
                case MyKeys.Down:
                    Vertical(_caret.Line + 1, shift);
                    return true;
                case MyKeys.PageUp:
                    Vertical(_caret.Line - Page, shift);
                    return true;
                case MyKeys.PageDown:
                    Vertical(_caret.Line + Page, shift);
                    return true;
                case MyKeys.Home:
                    Across(ctrl ? new Spot(0, 0) : new Spot(_caret.Line, 0), shift);
                    return true;
                case MyKeys.End:
                    Across(ctrl ? End : new Spot(_caret.Line, _lines[_caret.Line].Length), shift);
                    return true;
                case MyKeys.Back:
                    Backspace();
                    return true;
                case MyKeys.Delete:
                    DeleteForward();
                    return true;
                case MyKeys.Enter:
                    Insert("\n");
                    return true;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        protected override void OnTextInput(char c)
        {
            _core.RestartBlink();
            if (c < ' ' || c == (char)127)
                return;

            _core.Insert(c.ToString(), true);
        }

        /// <summary>Measures the text area, which takes all the available space.</summary>
        /// <remarks>
        /// Works out the text's height, the numbers' width and where a press lands here, because arranging has no font to
        /// ask.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _tall = _lines.Count * _fontSize;

            _gutter.Measure(g, _showLineNumbers, _lines.Count, _font, _fontSize, _lineNumbersPadding);

            // Read only and without the keyboard, nothing moves the caret, and its lines are measured only for a selection.
            Answer(g);
            if (!IsReadOnly || IsFocused || HasSelection)
                KeepEdges(g);

            return available;
        }

        /// <summary>
        ///     Sizes both scroll bars to the bounds and clamps each offset to its bar, then scrolls the caret into view
        ///     if it moved.
        /// </summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            _down.Extent = _tall;
            _across.Extent = Math.Max(_widest, CaretX + _caretWidth);
            _down.Viewport = Room(bounds.Height, BarRoom);
            _across.Viewport = Room(Room(bounds.Width, BarRoom), _gutter.Width);

            if (_follow)
            {
                _follow = false;
                BringCaretIntoView();
            }

            _down.Offset = _down.Clamp(_down.Offset);
            _across.Offset = _across.Clamp(_across.Offset);
        }

        /// <summary>
        ///     Draws only the visible lines, with the selection behind them and the caret over them, then the line
        ///     numbers and the scroll bar thumbs.
        /// </summary>
        protected override void DrawContent(IGraphics g)
        {
            var view = View;
            if (_fontSize > 0f && view.Height > 0f)
            {
                var first = Math.Max(0, (int)(_down.Offset / _fontSize));
                var last = Math.Min(_lines.Count - 1, (int)((_down.Offset + view.Height) / _fontSize));
                var top = view.Y - _down.Offset + first * _fontSize;

                _gutter.Draw(g, view, first, last, top, _font, _fontSize, LineNumberColor);

                var words = new RectangleF(WordsX, view.Y, Room(view.Width, _gutter.Width), view.Height);
                var x = words.X - _across.Offset;
                var y = top;
                var selecting = HasSelection && IsFocused;
                var start = SelectionFirst;
                var end = SelectionLast;

                g.PushClip(words);

                if (IsEmpty && !IsFocused && !string.IsNullOrEmpty(Hint))
                    g.DrawText(HintFont ?? _font, Hint, new Vector2(x, y), _fontSize, HintColor);

                for (var i = first; i <= last; i++)
                {
                    var said = _lines[i];
                    if (_widths[i] < 0f || i < _shownFrom || i > _shownTo)
                        Widen(g, i);

                    if (selecting && i >= start.Line && i <= end.Line)
                        Select(g, i, start, end, x, y);

                    if (said.Length > 0)
                        g.DrawText(_font, said, new Vector2(x, y), _fontSize, Color);

                    y += _fontSize;
                }

                // After the text, so the caret shows inside a selection.
                if (IsFocused && _core.IsCaretShown(g) && _caret.Line >= first && _caret.Line <= last)
                {
                    g.Fill(new RectangleF(x + CaretX, top + (_caret.Line - first) * _fontSize, _caretWidth, _fontSize),
                        CaretColor);
                }

                g.PopClip();
                _shownFrom = first;
                _shownTo = last;
            }

            Bar(g, Held.Down);
            Bar(g, Held.Across);
        }

        /// <summary>
        ///     Returns true if <paramref name="point" /> is inside <paramref name="bounds" />, a rectangle that is not
        ///     an element's bounds.
        /// </summary>
        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }

        /// <summary>
        ///     Returns what remains of <paramref name="whole" /> after subtracting <paramref name="taken" />, or 0 if
        ///     nothing remains.
        /// </summary>
        static float Room(float whole, float taken)
        {
            var room = whole - taken;
            return room > 0f ? room : 0f;
        }

        /// <summary>
        ///     Moves the caret to <paramref name="to" /> and forgets the column that Up and Down were keeping; extends
        ///     the selection if <paramref name="select" /> is true.
        /// </summary>
        void Across(Spot to, bool select)
        {
            _goal = -1f;
            Place(to, select ? _anchor : to);
            Follow();
        }

        /// <summary>
        ///     Requests the caret on <paramref name="line" />, at the column it had when the current run of Up and Down
        ///     presses began.
        /// </summary>
        void Vertical(int line, bool select)
        {
            if (_goal < 0f)
                _goal = CaretX;

            if (line < 0)
            {
                Place(new Spot(0, 0), select ? _anchor : new Spot(0, 0));
                Follow();
                return;
            }

            if (line >= _lines.Count)
            {
                Place(End, select ? _anchor : End);
                Follow();
                return;
            }

            _askedLine = line;
            _askedAt = new Vector2(_goal, 0f);
            _askedSelects = select;
            _asked = Asked.Across;
            Follow();
            InvalidateMeasure();
        }

        /// <summary>
        ///     Requests the caret at <paramref name="point" />, to be resolved in the measure pass once the line's
        ///     characters are known.
        /// </summary>
        void Ask(Asked what, Vector2 point, bool select)
        {
            _asked = what;
            _askedAt = point;
            _askedSelects = select;
            _goal = -1f;
            Follow();
            InvalidateMeasure();
        }

        /// <summary>Resolves the pending caret request, measuring the line it lands on.</summary>
        void Answer(IGraphics g)
        {
            if (_asked == Asked.Nothing)
                return;

            var what = _asked;
            _asked = Asked.Nothing;

            int line;
            float x;
            if (what == Asked.Across)
            {
                line = _askedLine;
                x = _askedAt.X;
            }
            else
            {
                var view = View;
                line = _fontSize > 0f ? (int)Math.Floor((_askedAt.Y - view.Y + _down.Offset) / _fontSize) : 0;
                line = Math.Max(0, Math.Min(_lines.Count - 1, line));
                x = _askedAt.X - WordsX + _across.Offset;
            }

            var at = new Spot(line, ColumnAt(g, line, x));

            _settling = true;
            if (what == Asked.Word)
                _core.SelectWord(IndexOf(at));
            else if (what == Asked.Line)
                Place(line + 1 < _lines.Count ? new Spot(line + 1, 0) : new Spot(line, _lines[line].Length), new Spot(line, 0));
            else
                Place(at, _askedSelects ? _anchor : at);

            _settling = false;
        }

        /// <summary>Returns the character boundary on <paramref name="line" /> nearest <paramref name="x" />.</summary>
        int ColumnAt(IGraphics g, int line, float x)
        {
            var edges = EdgesOf(g, line);
            var best = 0;
            for (var i = 1; i < edges.Count; i++)
            {
                if (Math.Abs(edges[i] - x) < Math.Abs(edges[best] - x))
                    best = i;
            }

            return best;
        }

        /// <summary>
        ///     Keeps the character boundaries of the lines holding the caret and the selection anchor, and discards the
        ///     rest.
        /// </summary>
        void KeepEdges(IGraphics g)
        {
            EdgesOf(g, _caret.Line);
            EdgesOf(g, _anchor.Line);

            _dropped.Clear();
            foreach (var line in _edges.Keys)
            {
                if (line != _caret.Line && line != _anchor.Line)
                    _dropped.Add(line);
            }

            foreach (var line in _dropped)
            {
                _spareEdges.Add(_edges[line]);
                _edges.Remove(line);
            }
        }

        /// <summary>
        ///     Returns the position of every character boundary on <paramref name="line" />, measuring the line on
        ///     first use.
        /// </summary>
        List<float> EdgesOf(IGraphics g, int line)
        {
            List<float> edges;
            if (_edges.TryGetValue(line, out edges))
                return edges;

            if (_spareEdges.Count > 0)
            {
                edges = _spareEdges[_spareEdges.Count - 1];
                _spareEdges.RemoveAt(_spareEdges.Count - 1);
                edges.Clear();
            }
            else
            {
                edges = new List<float>();
            }

            var said = _lines[line];
            edges.Add(0f);
            _scratch.Length = 0;
            foreach (var character in said)
            {
                _scratch.Append(character);
                edges.Add(g.MeasureText(_font, _scratch, _fontSize).X);
            }

            _edges[line] = edges;
            return edges;
        }

        /// <summary>
        ///     Returns the horizontal position of <paramref name="at" />, or 0 if its line has not been measured.
        /// </summary>
        float XOf(Spot at)
        {
            List<float> edges;
            return _edges.TryGetValue(at.Line, out edges) && at.Column < edges.Count ? edges[at.Column] : 0f;
        }

        /// <summary>
        ///     Fills the background of the selected part of one line, extending past the line's end if the selection
        ///     continues onto the next line.
        /// </summary>
        void Select(IGraphics g, int line, Spot start, Spot end, float x, float y)
        {
            var from = line == start.Line ? XOf(start) : 0f;
            var to = line == end.Line ? XOf(end) : Math.Max(0f, _widths[line]) + (LineBreakSelectionWidth ?? _fontSize * 0.25f);
            if (to > from)
                g.Fill(new RectangleF(x + from, y, to - from, _fontSize), SelectionColor);
        }

        /// <summary>Scrolls just far enough to show the caret.</summary>
        void BringCaretIntoView()
        {
            var top = _caret.Line * _fontSize;
            if (top < _down.Offset)
                _down.Offset = top;
            else if (top + _fontSize > _down.Offset + _down.Viewport)
                _down.Offset = top + _fontSize - _down.Viewport;

            var across = CaretX;
            if (across < _across.Offset)
                _across.Offset = across;
            else if (across + _caretWidth > _across.Offset + _across.Viewport)
                _across.Offset = across + _caretWidth - _across.Viewport;
        }

        /// <summary>
        ///     Applies a text replacement to the lines: <paramref name="removed" /> characters from
        ///     <paramref name="at" /> are replaced by <paramref name="words" />.
        /// </summary>
        /// <remarks>Raised by the text as it changes, while the lines still hold what it held.</remarks>
        void OnReplaced(int at, int removed, string words)
        {
            var from = SpotOf(at);
            var to = SpotOf(at + removed);
            var head = _lines[from.Line].Substring(0, from.Column);
            var tail = _lines[to.Line].Substring(to.Column);
            var pieces = words.Split('\n');

            _lines.RemoveRange(from.Line, to.Line - from.Line + 1);
            _widths.RemoveRange(from.Line, to.Line - from.Line + 1);

            for (var i = 0; i < pieces.Length; i++)
            {
                var said = pieces[i];
                if (i == 0)
                    said = head + said;
                if (i == pieces.Length - 1)
                    said += tail;

                _lines.Insert(from.Line + i, said);
                _widths.Insert(from.Line + i, -1f);
            }

            _edges.Clear();
            _shownFrom = int.MaxValue;
            _shownTo = int.MinValue;
            _goal = -1f;
            OnPlaced();
            Follow();
        }

        /// <summary>
        ///     Computes the line and column of the caret and the selection anchor, and measures again unless the
        ///     measure pass already placed them.
        /// </summary>
        void OnPlaced()
        {
            _caret = SpotOf(_core.CaretIndex);
            _anchor = SpotOf(_core.AnchorIndex);

            if (!_settling)
                InvalidateMeasure();
        }

        void OnEdited()
        {
            Edited?.Invoke();
        }

        /// <summary>
        ///     Returns the line and column at <paramref name="index" /> characters into the text, clamped to the text.
        /// </summary>
        Spot SpotOf(int index)
        {
            for (var line = 0; line < _lines.Count; line++)
            {
                var length = _lines[line].Length;
                if (index <= length)
                    return new Spot(line, Math.Max(0, index));

                index -= length + 1;
            }

            return End;
        }

        /// <summary>Returns the number of characters from the start of the text to <paramref name="at" />.</summary>
        int IndexOf(Spot at)
        {
            var index = at.Column;
            for (var line = 0; line < at.Line; line++)
                index += _lines[line].Length + 1;

            return index;
        }

        /// <summary>
        ///     Returns the position one character from <paramref name="at" /> in the direction <paramref name="way" />,
        ///     crossing to the next or previous line at a line end.
        /// </summary>
        Spot Step(Spot at, int way)
        {
            if (way < 0)
            {
                if (at.Column > 0)
                    return new Spot(at.Line, at.Column - 1);

                return at.Line > 0 ? new Spot(at.Line - 1, _lines[at.Line - 1].Length) : at;
            }

            if (at.Column < _lines[at.Line].Length)
                return new Spot(at.Line, at.Column + 1);

            return at.Line + 1 < _lines.Count ? new Spot(at.Line + 1, 0) : at;
        }

        /// <summary>
        ///     Returns the start of the next word from <paramref name="at" /> in the direction <paramref name="way" />,
        ///     crossing lines when a line runs out.
        /// </summary>
        Spot Word(Spot at, int way)
        {
            var moved = Step(at, way);
            if (moved.Line != at.Line)
                return moved;

            var said = _lines[at.Line];
            var column = moved.Column;
            if (way < 0)
            {
                while (column > 0 && !char.IsLetterOrDigit(said[column]))
                    column--;
                while (column > 0 && char.IsLetterOrDigit(said[column - 1]))
                    column--;
            }
            else
            {
                while (column < said.Length && char.IsLetterOrDigit(said[column - 1]))
                    column++;
                while (column < said.Length && !char.IsLetterOrDigit(said[column]))
                    column++;
            }

            return new Spot(at.Line, column);
        }

        /// <summary>Returns <paramref name="at" /> clamped to the text.</summary>
        Spot Within(Spot at)
        {
            var line = Math.Max(0, Math.Min(_lines.Count - 1, at.Line));
            return new Spot(line, Math.Max(0, Math.Min(_lines[line].Length, at.Column)));
        }

        /// <summary>
        ///     Sets the caret to <paramref name="caret" /> and the selection anchor to <paramref name="anchor" />.
        /// </summary>
        void Place(Spot caret, Spot anchor)
        {
            _core.Place(IndexOf(caret), IndexOf(anchor));
        }

        /// <summary>Requests that the view follow the caret the next time it is arranged.</summary>
        void Follow()
        {
            _follow = true;
            InvalidateArrange();
        }

        /// <summary>
        ///     Grabs the bar named by <paramref name="way" /> if <paramref name="point" /> is on it, and returns true
        ///     if it did; a press off the thumb centers the thumb on the press.
        /// </summary>
        bool Grab(Held way, Vector2 point)
        {
            if (!HasBar(way) || !Inside(Track(way), point))
                return false;

            if (!Inside(ThumbBounds(way), point))
            {
                var track = Track(way);
                if (way == Held.Across)
                    ScrollToHorizontalOffset(_across.ThumbedTo(point.X, track.X));
                else
                    ScrollToVerticalOffset(_down.ThumbedTo(point.Y, track.Y));
            }

            return true;
        }

        void Bar(IGraphics g, Held way)
        {
            if (HasBar(way))
                g.Fill(ThumbBounds(way), _held == way ? Thumb : _restingThumb);
        }

        bool HasBar(Held way)
        {
            return _scrollBarWidth > 0f && Thumb.A > 0 && Of(way).CanMove;
        }

        /// <summary>
        ///     Returns the bounds of the bar named by <paramref name="way" />, thumb and track together, in the space
        ///     the text does not use.
        /// </summary>
        RectangleF Track(Held way)
        {
            var room = ContentBounds;
            return way == Held.Across
                ? new RectangleF(room.X, room.Y + room.Height - _scrollBarWidth, Room(room.Width, BarRoom), _scrollBarWidth)
                : new RectangleF(room.X + room.Width - _scrollBarWidth, room.Y, _scrollBarWidth, Room(room.Height, BarRoom));
        }

        RectangleF ThumbBounds(Held way)
        {
            var track = Track(way);
            var rail = Of(way);
            return way == Held.Across
                ? new RectangleF(track.X + rail.ThumbStart, track.Y, rail.ThumbLength, track.Height)
                : new RectangleF(track.X, track.Y + rail.ThumbStart, track.Width, rail.ThumbLength);
        }

        ScrollAxis Of(Held way)
        {
            return way == Held.Across ? _across : _down;
        }

        /// <summary>
        ///     Measures <paramref name="line" /> if it came into view or changed, and extends the scrollable width if
        ///     it is the longest so far.
        /// </summary>
        /// <remarks>
        /// Measures while drawing, only lines not on show last frame or edited since: measuring the whole text up front was
        /// timed at 11ms for five hundred lines and 221ms for ten thousand, against a frame of under 17. The reach goes onto
        /// the rail without invalidating, because only the thumb is drawn from it, after this.
        /// </remarks>
        void Widen(IGraphics g, int line)
        {
            var said = _lines[line];
            var wide = 0f;
            if (said.Length > 0)
            {
                _scratch.Length = 0;
                _scratch.Append(said);
                wide = g.MeasureText(_font, _scratch, _fontSize).X;
            }

            _widths[line] = wide;
            if (wide <= _widest)
                return;

            _widest = wide;
            _across.Extent = _widest;
        }

        /// <summary>Discards cached measurements that are stale because the text, font or size changed.</summary>
        void Forget()
        {
            _widest = 0f;
            _across.Extent = 0f;
            _shownFrom = int.MaxValue;
            _shownTo = int.MinValue;
            _edges.Clear();
            for (var i = 0; i < _widths.Count; i++)
                _widths[i] = -1f;
        }

        /// <summary>The part of the text area the pointer is holding, if any.</summary>
        enum Held
        {
            Nothing,
            Down,
            Across,
            Text,
            Selecting
        }

        /// <summary>A pending caret request that waits for a line's characters to be measured.</summary>
        enum Asked
        {
            Nothing,
            Point,
            Word,
            Line,
            Across
        }
    }
}
