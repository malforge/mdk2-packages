using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Styling;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Input
{
    /// <summary>
    ///     The drag under way in one window layer: what is carried, the drop targets that take it, the one under the
    ///     pointer, and the ghost drawn where the pointer has taken it.
    /// </summary>
    public class DragDrop
    {
        /// <summary>The state of a drop target that takes what is being dragged.</summary>
        public static readonly State Accepting = State.Declare("Accepting", 110);

        /// <summary>The state of a drop target that refuses what is being dragged.</summary>
        public static readonly State Refusing = State.Declare("Refusing", 110);

        /// <summary>The state of the accepting drop target under the pointer, where a release drops what is dragged.</summary>
        public static readonly State DraggedOver = State.Declare("DraggedOver", 150);

        readonly WindowLayer _layer;
        readonly List<Element> _targets = new List<Element>();
        Vector2 _from;
        Vector2 _at;

        /// <summary>Creates the drag of <paramref name="layer" />, with nothing dragged.</summary>
        public DragDrop(WindowLayer layer)
        {
            _layer = layer;
        }

        /// <summary>Raised when a drag ends, with the drop target it was dropped on, or null if it was canceled.</summary>
        public event Action<Element> Ended;

        /// <summary>Whether something is being dragged.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>Gets what is being dragged, or null.</summary>
        public object Data { get; private set; }

        /// <summary>Gets the element the drag began from, or null.</summary>
        public Element Source { get; private set; }

        /// <summary>Gets the element drawn where the pointer has taken it while dragging, or null for none.</summary>
        public Element Ghost { get; private set; }

        /// <summary>Gets the accepting drop target under the pointer, or null.</summary>
        public Element Target { get; private set; }

        /// <summary>Whether any drop target shown when the drag began takes what is dragged.</summary>
        public bool FitsAnywhere { get; private set; }

        /// <summary>
        ///     Begins dragging <paramref name="data" /> from <paramref name="source" />, grabbed at
        ///     <paramref name="point" /> on the screen, with the source as its ghost.
        /// </summary>
        public void Begin(Element source, object data, Vector2 point)
        {
            Begin(source, data, point, source);
        }

        /// <summary>
        ///     Begins dragging <paramref name="data" /> from <paramref name="source" />, grabbed at
        ///     <paramref name="point" /> on the screen, drawing <paramref name="ghost" />, or nothing where it is null,
        ///     where the pointer takes it.
        /// </summary>
        /// <remarks>
        ///     Every drop target shown in the layer's windows is asked once whether it takes the data, and put in
        ///     <see cref="Accepting" /> or <see cref="Refusing" />. A drag already under way is canceled first.
        /// </remarks>
        public void Begin(Element source, object data, Vector2 point, Element ghost)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (IsDragging)
                Cancel();

            IsDragging = true;
            Source = source;
            Data = data;
            Ghost = ghost;
            _from = point;
            _at = point;
            FitsAnywhere = false;
            for (var i = 0; i < _layer.Count; i++)
                Gather(_layer[i]);

            Move(point);
        }

        /// <summary>Moves the drag to <paramref name="point" /> on the screen, finding the drop target under it.</summary>
        /// <remarks>
        ///     Called by the source as the pointer moves, and by the workspace each frame, as targets can move under a
        ///     still pointer.
        /// </remarks>
        public void Move(Vector2 point)
        {
            if (!IsDragging)
                return;

            _at = point;
            var target = _layer.DropTargetAt(point);
            if (target == Target)
                return;

            Target?.Set(DraggedOver, false);
            Target = target;
            Target?.Set(DraggedOver, true);
        }

        /// <summary>
        ///     Ends the drag at <paramref name="point" /> on the screen, dropping what is dragged on the accepting
        ///     target there; returns true if one took it.
        /// </summary>
        public bool Drop(Vector2 point)
        {
            if (!IsDragging)
                return false;

            Move(point);
            var target = Target;
            var data = Data;
            End();
            target?.DropTarget.Drop(data);
            Ended?.Invoke(target);
            return target != null;
        }

        /// <summary>Ends the drag with nothing dropped.</summary>
        public void Cancel()
        {
            if (!IsDragging)
                return;

            End();
            Ended?.Invoke(null);
        }

        /// <summary>Draws the ghost, moved by as far as the pointer has gone since the grab.</summary>
        public void DrawGhost(IGraphics g)
        {
            if (!IsDragging || Ghost == null)
                return;

            // Drawn through every transform above the ghost, then moved, so it is seen where the pointer took it.
            var parent = Ghost.Parent;
            var origin = parent?.ToScreen(Vector2.Zero) ?? Vector2.Zero;
            var scale = parent == null ? 1f : parent.ToScreen(Vector2.UnitX).X - origin.X;
            g.PushTransform(new RenderTransform(origin + _at - _from, scale), Vector2.Zero);
            Ghost.Draw(g);
            g.PopTransform();
        }

        void Gather(Element element)
        {
            if (!element.IsVisible)
                return;

            if (element.DropTarget != null)
            {
                var accepts = element.DropTarget.CanDrop(Data);
                element.Set(accepts ? Accepting : Refusing, true);
                FitsAnywhere |= accepts;
                _targets.Add(element);
            }

            for (var i = 0; i < element.VisualChildCount; i++)
                Gather(element.GetVisualChild(i));
        }

        void End()
        {
            foreach (var target in _targets)
            {
                target.Set(Accepting, false);
                target.Set(Refusing, false);
                target.Set(DraggedOver, false);
            }

            _targets.Clear();
            IsDragging = false;
            Data = null;
            Source = null;
            Ghost = null;
            Target = null;
            FitsAnywhere = false;
        }
    }
}
