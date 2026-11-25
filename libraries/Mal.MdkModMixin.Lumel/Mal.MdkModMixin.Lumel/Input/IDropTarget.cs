namespace Lumel.Input
{
    /// <summary>What dropping a dragged thing on an element does, implemented by the application.</summary>
    public interface IDropTarget
    {
        /// <summary>Returns true if <paramref name="data" /> can be dropped here.</summary>
        /// <remarks>Asked once for each drag, when it begins.</remarks>
        bool CanDrop(object data);

        /// <summary>Takes <paramref name="data" />, dropped here.</summary>
        void Drop(object data);
    }
}
