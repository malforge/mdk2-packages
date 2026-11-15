namespace Lumel.Styling
{
    /// <summary>The top of what a host shows, which takes its defaults from the host's theme.</summary>
    public interface IThemed
    {
        /// <summary>
        ///     Applies <paramref name="theme" /> to everything this is the top of, or Lumel's game look if
        ///     <paramref name="theme" /> is null.
        /// </summary>
        void UseTheme(ITheme theme);
    }
}
