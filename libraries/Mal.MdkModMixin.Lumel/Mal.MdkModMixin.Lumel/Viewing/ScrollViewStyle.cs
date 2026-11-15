using Lumel.Styling;

namespace Lumel.Viewing
{
    /// <summary>The style of a scroll view: its bar and thumb, when idle and while the thumb is dragged.</summary>
    public class ScrollViewStyle : Style<ScrollView, ScrollViewLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the scroll view at its neutral values.</summary>
        public ScrollViewStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public ScrollViewStyle(IStyle<ScrollView> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look that applies while the thumb is being dragged.</summary>
        public ScrollViewLook Dragging => this[ScrollView.Dragging];
    }
}
