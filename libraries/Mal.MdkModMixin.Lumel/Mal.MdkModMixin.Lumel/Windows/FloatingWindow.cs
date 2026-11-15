using System;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>A window that stays open beside what it floats over, at the position the player moved it to.</summary>
    public class FloatingWindow : Window, IFloatingWindow
    {
        /// <summary>
        ///     Raised when a move by its <see cref="TitleBar" /> ends, with <see cref="Position" /> set to where it was
        ///     left.
        /// </summary>
        public event Action Moved;

        /// <summary>
        ///     Gets or sets the position of the window's top-left corner after a move, or null if it stands where
        ///     <see cref="Home" /> puts it.
        /// </summary>
        public Vector2? Position { get; set; }

        /// <summary>
        ///     Gets the whole screen that the window is kept on, or empty to keep it within the area it is placed in.
        /// </summary>
        /// <remarks>Set by the layer it is open in, through <see cref="IFloatingWindow" />.</remarks>
        public RectangleF Screen { get; private set; }

        /// <inheritdoc />
        RectangleF IFloatingWindow.Screen
        {
            set { Screen = value; }
        }

        /// <summary>
        ///     Gets or sets how much of the window always stays on the screen, horizontally and vertically, in UI
        ///     units; 96 by default.
        /// </summary>
        public float MinimumVisible { get; set; } = 96f;

        /// <inheritdoc />
        /// <remarks>
        /// Brought back far enough that <see cref="MinimumVisible" /> of it is on the screen, and its top never above the screen's.
        /// </remarks>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            var at = Position ?? Home(area, size);
            var screen = Screen.Width > 0f && Screen.Height > 0f ? Screen : area;
            var across = Math.Min(MinimumVisible, size.X);
            var down = Math.Min(MinimumVisible, size.Y);

            if (at.X > screen.X + screen.Width - across)
                at.X = screen.X + screen.Width - across;

            if (at.Y > screen.Y + screen.Height - down)
                at.Y = screen.Y + screen.Height - down;

            if (at.X < screen.X - size.X + across)
                at.X = screen.X - size.X + across;

            if (at.Y < screen.Y)
                at.Y = screen.Y;

            return new RectangleF(at.X, at.Y, size.X, size.Y);
        }

        /// <inheritdoc />
        /// <remarks>Brings <see cref="Position" /> back far enough onto the screen first, then raises <see cref="Moved" />.</remarks>
        void IFloatingWindow.EndMove()
        {
            if (Position.HasValue && Screen.Width > 0f && Screen.Height > 0f)
            {
                var kept = Place(Screen, new Vector2(Bounds.Width, Bounds.Height));
                Position = new Vector2(kept.X, kept.Y);
            }

            Moved?.Invoke();
        }

        /// <summary>
        ///     Returns the position of the window's top-left corner in <paramref name="area" /> before it has been
        ///     moved: centered, unless a derived floating window says otherwise.
        /// </summary>
        protected virtual Vector2 Home(RectangleF area, Vector2 size)
        {
            return new Vector2(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f);
        }
    }
}
