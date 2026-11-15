using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A vertical list of rows, any of which can be dragged by its handle to another place in the list, bringing
    ///     along the rows that belong to it.
    /// </summary>
    /// <remarks>
    /// Every row is an <see cref="IReorderable" /> and an <see cref="IDragged" />; the rows not dragged slide aside to make room
    /// where the dragged ones would land, and every row slides home after a drop.
    /// </remarks>
    // The rows are the host's own elements, so Lumel inspected alone holds no type that is both an element and a row.
    // ReSharper disable SuspiciousTypeConversion.Global
    public class ReorderList : VStack
    {
        // How much of the way left to where it should be a row not dragged moves each tick.
        const float SlidePerTick = 0.35f;

        readonly List<float> _wanted = new List<float>();
        readonly Dictionary<IReorderable, Action<Vector2>> _takes = new Dictionary<IReorderable, Action<Vector2>>();
        readonly Dictionary<Element, float> _landed = new Dictionary<Element, float>();

        int _dragging = -1;
        int _span;
        IList<int> _allowed;
        float _grabbedAt;
        float _grabbedAcross;
        float _moved;
        float _across;
        bool _startedIndented;
        bool _canIndent;
        bool _goesIn;
        float _indentWidth;
        int _slidAt;
        bool _sliding;

        // Every row is home and none is dragged, so a frame passes over the rows only once something moves.
        bool _settled = true;

        /// <summary>
        ///     Gets or sets the style of this list, or null to use the default from the host's theme for this kind of
        ///     list.
        /// </summary>
        public new IStyle<ReorderList> Style
        {
            get { return (IStyle<ReorderList>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this list's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new ReorderListLook Base => (ReorderListLook)base.Base;

        /// <summary>Gets this list's own look applied on hover, layered over the look from its style.</summary>
        public new ReorderListLook PointerOver => (ReorderListLook)base.PointerOver;

        /// <summary>Gets this list's own look applied when disabled, layered over the look from its style.</summary>
        public new ReorderListLook Disabled => (ReorderListLook)base.Disabled;

        /// <summary>
        ///     Gets this list's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new ReorderListLook this[State state] => (ReorderListLook)base[state];

        /// <summary>
        ///     Gets or sets the callback called when a row is grabbed that returns the positions it may be dropped at,
        ///     each given as the number of non-dragged rows above it, or null to allow any position.
        /// </summary>
        public Func<int, IList<int>> FnAllowedDropIndexes { get; set; }

        /// <summary>Whether a row is being dragged.</summary>
        public bool IsDragging => _dragging >= 0;

        /// <summary>Gets the index in the list of the row being dragged, or -1 if none is.</summary>
        protected int DraggedIndex => _dragging;

        /// <summary>Raised when a row is dropped in a new place.</summary>
        public event Action<ReorderedEventArgs> Reordered;

        /// <summary>
        ///     Gets or sets the callback called while a row is dragged, with the index it came from and the number of
        ///     other rows that would be above it, that returns true if the row would land at the end of a group, where
        ///     dragging it right puts it in the group and dragging it left leaves it out; null to never allow it.
        /// </summary>
        public Func<int, int, bool> FnCanIndent { get; set; }

        /// <summary>
        ///     Gets or sets the callback called when a row is grabbed that returns true if the row is in a group, and
        ///     so is indented by <see cref="IndentWidth" />; null to treat no row as indented.
        /// </summary>
        public Func<int, bool> FnIsIndented { get; set; }

        /// <summary>
        ///     Gets how far a row in a group is indented, which is also how far to the right a dragged row must go to
        ///     join a group.
        /// </summary>
        /// <remarks>Set by this list's looks.</remarks>
        public float IndentWidth => _indentWidth;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ReorderList>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ReorderListLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            _indentWidth = ((ReorderListLook)look).IndentWidth ?? 0f;
        }

        /// <summary>
        ///     Lays out the rows within <paramref name="bounds" />, and keeps each row drawn where a drop left it so it
        ///     slides into place from there.
        /// </summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);

            if (_landed.Count == 0)
                return;

            foreach (var child in Children)
            {
                float seen;
                if (_landed.TryGetValue(child, out seen))
                    child.RenderTransform = RenderTransform.Moved(new Vector2(0f, seen - child.Bounds.Y));
            }

            _landed.Clear();
            _settled = false;
        }

        /// <summary>
        ///     Draws the rows in the list, then the dragged rows over them, along with whatever
        ///     <see cref="DrawBeside" /> adds to each, after moving every row closer to its target position.
        /// </summary>
        protected override void DrawContent(IGraphics g)
        {
            Slide(g);

            for (var i = 0; i < Children.Count; i++)
            {
                if (!Dragged(i))
                    Children[i].Draw(g);
            }

            DrawBeside(g, false);
            if (_dragging < 0)
                return;

            for (var i = _dragging; i <= _dragging + _span; i++)
                Children[i].Draw(g);

            DrawBeside(g, true);
        }

        /// <summary>
        ///     Draws what accompanies the rows in the list, or the rows being dragged when <paramref name="dragged" />
        ///     is true; draws nothing by default.
        /// </summary>
        protected virtual void DrawBeside(IGraphics g, bool dragged)
        {
        }

        /// <summary>
        ///     Returns true if the row at <paramref name="index" /> is being dragged, either as the grabbed row or as
        ///     one brought along with it.
        /// </summary>
        protected bool Dragged(int index)
        {
            return _dragging >= 0 && index >= _dragging && index <= _dragging + _span;
        }

        /// <summary>
        ///     Rejects a child that is not a row, meaning one that is both <see cref="IReorderable" /> and
        ///     <see cref="IDragged" />.
        /// </summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            base.Admit(child, placement);
            if (!(child is IReorderable) || !(child is IDragged))
                throw new ArgumentException("A " + GetType().Name + " holds only rows that are IReorderable and IDragged; " + child.GetType().Name + " is not.", nameof(child));
        }

        /// <inheritdoc />
        protected override void OnChildAdded(Element child, int index)
        {
            base.OnChildAdded(child, index);

            _wanted.Insert(index, 0f);

            var row = (IReorderable)child;
            Action<Vector2> take = point => Take(row, point);
            _takes[row] = take;
            row.DragHandle.DragStarted += take;
            row.DragHandle.DragDelta += Drag;
            row.DragHandle.DragCompleted += Drop;
            row.DragHandle.DragCanceled += CancelDrag;
        }

        /// <inheritdoc />
        protected override void OnChildRemoved(Element child, int index)
        {
            base.OnChildRemoved(child, index);
            _wanted.RemoveAt(index);

            var row = (IReorderable)child;
            Action<Vector2> take;
            if (_takes.TryGetValue(row, out take))
                row.DragHandle.DragStarted -= take;

            _takes.Remove(row);
            row.DragHandle.DragDelta -= Drag;
            row.DragHandle.DragCompleted -= Drop;
            row.DragHandle.DragCanceled -= CancelDrag;
        }

        /// <summary>
        ///     Moves every row slightly closer to its target position for the ticks elapsed since the last frame, and
        ///     requests another frame while any row is still moving; the dragged row follows the pointer exactly.
        /// </summary>
        void Slide(IGraphics g)
        {
            if (_settled)
                return;

            var now = g.Now;
            var ticks = _sliding ? now - _slidAt : 1;
            _slidAt = now;
            _sliding = false;

            var share = ticks <= 0 ? 0f : 1f - (float)Math.Pow(1f - SlidePerTick, ticks);
            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i];
                var shift = row.RenderTransform.Translate.Y;
                var dragged = Dragged(i);
                var target = dragged ? _moved : _wanted[i];

                if (dragged)
                    shift = target;
                else
                    shift += (target - shift) * share;

                if (shift < 0.5f && shift > -0.5f && target == 0f)
                    shift = 0f;

                if (shift != target)
                    _sliding = true;

                row.RenderTransform = RenderTransform.Moved(new Vector2(dragged ? Across() : 0f, shift));
            }

            if (_sliding || _dragging >= 0)
                g.RequestFrame();

            _settled = !_sliding && _dragging < 0;
        }

        void Take(IReorderable row, Vector2 point)
        {
            _landed.Clear();
            _settled = false;
            _dragging = Children.IndexOf((Element)row);
            _span = Math.Max(0, Math.Min(row.DraggedWithCount, Children.Count - 1 - _dragging));
            _allowed = FnAllowedDropIndexes?.Invoke(_dragging);
            var grabbed = FromScreen(point);
            _grabbedAt = grabbed.Y;
            _grabbedAcross = grabbed.X;
            _moved = 0f;
            _across = 0f;
            _startedIndented = FnIsIndented != null && FnIsIndented(_dragging);
            _canIndent = false;
            _goesIn = _startedIndented;
            ((IDragged)row).IsDragging = true;
            for (var i = _dragging; i <= _dragging + _span; i++)
                _wanted[i] = 0f;
        }

        void Drag(Vector2 point)
        {
            if (_dragging < 0)
                return;

            var at = FromScreen(point);
            _settled = false;
            _moved = at.Y - _grabbedAt;
            _across = at.X - _grabbedAcross;

            var to = LandingFor(_moved);
            Indenting(to);
            MakeRoom(to);
        }

        /// <summary>
        ///     Determines whether the dragged row, landing under <paramref name="to" /> of the other rows, joins the
        ///     group it would end.
        /// </summary>
        void Indenting(int to)
        {
            _canIndent = FnCanIndent != null && FnCanIndent(_dragging, to);
            if (!_canIndent)
                return;

            var depth = (_startedIndented ? _indentWidth : 0f) + _across;
            _goesIn = depth > _indentWidth * 0.5f;
        }

        /// <summary>
        ///     Returns the horizontal offset of the dragged rows from their normal position: in or out by a group's
        ///     indent where they may choose.
        /// </summary>
        float Across()
        {
            if (!_canIndent || _goesIn == _startedIndented)
                return 0f;

            return _goesIn ? _indentWidth : -_indentWidth;
        }

        void Drop(Vector2 point)
        {
            if (_dragging < 0)
                return;

            var from = _dragging;
            _settled = false;
            var to = LandingFor(_moved);
            Indenting(to);
            var dragged = Children[from];
            var indentChanged = _canIndent && _goesIn != _startedIndented;
            var indented = _canIndent && _goesIn;

            EndDrag();

            if (to == from && !indentChanged)
                return;

            var landing = Landing(from, to);
            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i];
                var inSpan = i >= from && i <= from + _span;
                _landed[row] = inSpan
                    ? landing + row.Bounds.Y - dragged.Bounds.Y
                    : row.Bounds.Y + row.RenderTransform.Translate.Y;

                if (inSpan)
                    row.RenderTransform = RenderTransform.Moved(new Vector2(0f, _landed[row] - row.Bounds.Y));
            }

            Reordered?.Invoke(new ReorderedEventArgs(from, to, indented));
        }

        /// <summary>
        ///     Ends a drag without dropping the row: the rows slide back to their positions and nothing is reordered.
        /// </summary>
        void CancelDrag()
        {
            if (_dragging < 0)
                return;

            _settled = false;
            EndDrag();
        }

        /// <summary>Releases the row being dragged, leaving every row drawn where it is.</summary>
        void EndDrag()
        {
            var dragged = Children[_dragging];
            _dragging = -1;
            _moved = 0f;
            _across = 0f;
            _canIndent = false;
            ((IDragged)dragged).IsDragging = false;

            for (var i = 0; i < _wanted.Count; i++)
                _wanted[i] = 0f;
        }

        void MakeRoom(int to)
        {
            var height = 0f;
            for (var i = _dragging; i <= _dragging + _span; i++)
                height += Children[i].DesiredSize.Y + Spacing;

            for (var i = 0; i < Children.Count; i++)
            {
                if (Dragged(i))
                    continue;

                // Counted among the rows not dragged, as drop indexes are.
                var among = i < _dragging ? i : i - _span - 1;
                var shift = 0f;
                if (_dragging < to && among >= _dragging && among < to)
                    shift = -height;
                else if (_dragging > to && among >= to && among < _dragging)
                    shift = height;

                _wanted[i] = shift;
            }
        }

        int LandingFor(float moved)
        {
            var dragged = Children[_dragging].Bounds;
            var middle = dragged.Y + dragged.Height * 0.5f + moved;

            var index = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                if (Dragged(i))
                    continue;

                var bounds = Children[i].Bounds;
                if (middle > bounds.Y + bounds.Height * 0.5f)
                    index++;
            }

            return Allowed(index);
        }

        /// <summary>
        ///     Returns the allowed drop position nearest to <paramref name="index" />, or <paramref name="index" /> if
        ///     the dragged row may be dropped anywhere.
        /// </summary>
        int Allowed(int index)
        {
            if (_allowed == null || _allowed.Count == 0)
                return index;

            var best = _allowed[0];
            for (var i = 1; i < _allowed.Count; i++)
            {
                if (Math.Abs(_allowed[i] - index) < Math.Abs(best - index))
                    best = _allowed[i];
            }

            return best;
        }

        float Landing(int from, int to)
        {
            var top = Children[0].Bounds.Y;
            var placed = 0;

            for (var i = 0; i < Children.Count && placed < to; i++)
            {
                if (i >= from && i <= from + _span)
                    continue;

                top += Children[i].DesiredSize.Y + Spacing;
                placed++;
            }

            return top;
        }
    }
}
