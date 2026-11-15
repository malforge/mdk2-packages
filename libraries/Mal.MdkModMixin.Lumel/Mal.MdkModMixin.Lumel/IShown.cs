namespace Lumel
{
    /// <summary>
    ///     Implemented by elements so whatever shows a tree, such as a workspace, a window layer or a text panel, can tell
    ///     the tree's top it is shown.
    /// </summary>
    /// <remarks>Implemented explicitly by <see cref="Element" />, so only what shows a tree sets it.</remarks>
    public interface IShown
    {
        /// <summary>Sets whether the element, the top of its tree, is shown.</summary>
        bool IsShown { set; }
    }
}
