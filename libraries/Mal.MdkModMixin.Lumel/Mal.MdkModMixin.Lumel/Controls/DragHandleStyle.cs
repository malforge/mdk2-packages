using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a drag handle: its dots when idle, on hover, while dragged and when disabled.</summary>
    public class DragHandleStyle : Style<DragHandle, DragHandleLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the handle at its neutral values.</summary>
        public DragHandleStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public DragHandleStyle(IStyle<DragHandle> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the handle is being dragged.</summary>
        public DragHandleLook Dragging => this[DragHandle.Dragging];
    }
}
