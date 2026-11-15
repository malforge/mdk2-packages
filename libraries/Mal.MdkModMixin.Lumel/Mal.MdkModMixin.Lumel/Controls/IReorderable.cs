namespace Lumel.Controls
{
    /// <summary>
    ///     Implemented by the rows of a <see cref="ReorderList" />: a row is dragged by its handle, can draw itself as
    ///     dragged, and brings along the rows under it that belong to it.
    /// </summary>
    /// <remarks>A row also implements <see cref="IDragged" />, through which the list tells it whether it is being dragged.</remarks>
    public interface IReorderable
    {
        /// <summary>Gets the handle the row is grabbed and dragged by.</summary>
        DragHandle DragHandle { get; }

        /// <summary>Whether the row is being dragged, which the row may draw however it likes.</summary>
        bool IsDragging { get; }

        /// <summary>
        ///     Gets how many of the rows under this one are dragged along with it, or 0 for a row that is dragged
        ///     alone.
        /// </summary>
        int DraggedWithCount { get; }
    }
}
