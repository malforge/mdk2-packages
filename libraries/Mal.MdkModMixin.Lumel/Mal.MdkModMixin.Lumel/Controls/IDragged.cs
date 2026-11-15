namespace Lumel.Controls
{
    /// <summary>
    ///     Implemented by the rows of a <see cref="ReorderList" /> so the list can tell a row whether it is being
    ///     dragged.
    /// </summary>
    /// <remarks>Implemented explicitly by a row, so only the list it is in sets it.</remarks>
    public interface IDragged
    {
        /// <summary>Sets whether the row is being dragged.</summary>
        bool IsDragging { set; }
    }
}
