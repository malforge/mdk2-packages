using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A handle for dragging something, such as a row or a window, drawn as six small dots in two columns and three
    ///     rows.
    /// </summary>
    public class DragHandle : Element
    {
        /// <summary>The state of a drag handle while it is being dragged.</summary>
        public static readonly State Dragging = State.Declare("Dragging", 400);

        /// <summary>The side length of each dot in UI units when no look sets it.</summary>
        public const float NeutralDotSize = 3f;

        /// <summary>The space between adjacent dots in UI units when no look sets it.</summary>
        public const float NeutralDotSpacing = 3f;

        const int Across = 2;
        const int Down = 3;

        float _dotSize = NeutralDotSize;
        float _dotSpacing = NeutralDotSpacing;

        /// <summary>
        ///     Creates a handle that cannot be dragged yet, using the look from the host's theme for a drag handle
        ///     until it is given a style.
        /// </summary>
        public DragHandle()
        {
            Color = Color.White;
        }

        /// <summary>Raised with the pointer position on the screen when the handle is grabbed.</summary>
        public event Action<Vector2> DragStarted;

        /// <summary>Raised with the pointer position on the screen as the handle is dragged.</summary>
        public event Action<Vector2> DragDelta;

        /// <summary>Raised with the pointer position on the screen when the handle is released.</summary>
        public event Action<Vector2> DragCompleted;

        /// <summary>
        ///     Raised when the drag ends without the handle being released, such as when the screen closes mid-drag.
        /// </summary>
        public event Action DragCanceled;

        /// <summary>
        ///     Gets or sets the style of this handle, or null to use the default from the host's theme for a drag
        ///     handle.
        /// </summary>
        public new IStyle<DragHandle> Style
        {
            get { return (IStyle<DragHandle>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this handle's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new DragHandleLook Base => (DragHandleLook)base.Base;

        /// <summary>Gets this handle's own look applied on hover, layered over the look from its style.</summary>
        public new DragHandleLook PointerOver => (DragHandleLook)base.PointerOver;

        /// <summary>Gets this handle's own look applied when disabled, layered over the look from its style.</summary>
        public new DragHandleLook Disabled => (DragHandleLook)base.Disabled;

        /// <summary>
        ///     Gets this handle's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new DragHandleLook this[State state] => (DragHandleLook)base[state];

        /// <summary>Gets the color of the dots.</summary>
        /// <remarks>Set by this handle's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the side length of each dot, in UI units.</summary>
        /// <remarks>Set by this handle's looks.</remarks>
        public float DotSize => _dotSize;

        /// <summary>Gets the space between adjacent dots, in UI units.</summary>
        /// <remarks>Set by this handle's looks.</remarks>
        public float DotSpacing => _dotSpacing;

        /// <summary>
        ///     Gets or sets what grabbing the handle drags to a drop target through its layer's
        ///     <see cref="WindowLayer.DragDrop" />, or null where it drags nothing there.
        /// </summary>
        public object DragData { get; set; }

        /// <summary>
        ///     Gets or sets the element drawn where the pointer takes it while <see cref="DragData" /> is dragged, or null
        ///     for the handle itself.
        /// </summary>
        public Element DragGhost { get; set; }

        /// <summary>Whether the handle can currently be grabbed.</summary>
        public bool CanDrag { get; set; }

        /// <summary>Whether the handle is currently being dragged.</summary>
        /// <remarks>While it is, it shows its looks for <see cref="Dragging" />.</remarks>
        public bool IsDragging
        {
            get { return Is(Dragging); }
            private set { Set(Dragging, value); }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => CanDrag;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<DragHandle>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new DragHandleLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var handle = (DragHandleLook)look;
            Color = handle.Color ?? Color.White;

            var dotSize = handle.DotSize ?? NeutralDotSize;
            var dotSpacing = handle.DotSpacing ?? NeutralDotSpacing;
            if (dotSize == _dotSize && dotSpacing == _dotSpacing)
                return;

            _dotSize = dotSize;
            _dotSpacing = dotSpacing;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            IsDragging = true;
            var at = ToScreen(point);
            if (DragData != null)
                Windows?.DragDrop.Begin(this, DragData, at, DragGhost ?? this);
            DragStarted?.Invoke(at);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            var at = ToScreen(point);
            Carried()?.Move(at);
            DragDelta?.Invoke(at);
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            IsDragging = false;
            var at = ToScreen(point);
            Carried()?.Drop(at);
            DragCompleted?.Invoke(at);
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            if (!IsDragging)
                return;

            IsDragging = false;
            Carried()?.Cancel();
            DragCanceled?.Invoke();
        }

        /// <summary>Returns the layer's drag if this handle began it, or null.</summary>
        DragDrop Carried()
        {
            var drag = Windows?.DragDrop;
            return drag != null && drag.IsDragging && drag.Source == this ? drag : null;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Across * _dotSize + (Across - 1) * _dotSpacing, Down * _dotSize + (Down - 1) * _dotSpacing);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var width = Across * _dotSize + (Across - 1) * _dotSpacing;
            var height = Down * _dotSize + (Down - 1) * _dotSpacing;

            var left = ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
            var top = ContentBounds.Y + (ContentBounds.Height - height) * 0.5f;

            for (var x = 0; x < Across; x++)
            for (var y = 0; y < Down; y++)
                g.Fill(new RectangleF(left + x * (_dotSize + _dotSpacing), top + y * (_dotSize + _dotSpacing), _dotSize, _dotSize), Color);
        }
    }
}
