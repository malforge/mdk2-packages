using Lumel.Controls;
using Lumel.Styling;

namespace Lumel.Menus
{
    /// <summary>A line between two groups of a <see cref="Menu" />'s items.</summary>
    /// <remarks>The menu shows it only where there are visible items on both sides of it.</remarks>
    public class MenuSeparator : Separator
    {
        /// <summary>
        ///     Gets or sets the style of this separator, or null to use the default its host's theme holds for a menu
        ///     separator.
        /// </summary>
        public new IStyle<MenuSeparator> Style
        {
            get { return (IStyle<MenuSeparator>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<MenuSeparator>;
        }
    }
}
