namespace Lumel.Viewing
{
    /// <summary>The directions in which a <see cref="Viewbox" /> scales its content to fill its space.</summary>
    public enum StretchDirection
    {
        /// <summary>
        ///     Content smaller than the space is scaled up to fill it; larger content is drawn at its own size.
        /// </summary>
        UpOnly,

        /// <summary>
        ///     Content larger than the space is scaled down to fit it; smaller content is drawn at its own size.
        /// </summary>
        DownOnly,

        /// <summary>Content is scaled up or down to fill the space.</summary>
        Both,

        /// <summary>Content is drawn at its own size.</summary>
        None
    }
}
