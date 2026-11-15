using VRageMath;
using Lumel.Input;

namespace Lumel.Windows
{
    /// <summary>
    ///     The bar along the top of a floating window that moves it: pressing anywhere on it except on something that
    ///     handles presses itself makes the window follow the pointer.
    /// </summary>
    public class TitleBar : ContentControl
    {
        Vector2 _held;
        bool _holding;

        /// <summary>Gets or sets the window it moves, or null for none.</summary>
        public FloatingWindow Window { get; set; }

        /// <inheritdoc />
        protected override bool TakesPointer => Window != null;

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            if (Window == null)
                return;

            var bounds = Window.Bounds;
            point = Placed(point);
            _held = new Vector2(point.X - bounds.X, point.Y - bounds.Y);
            _holding = true;
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            if (_holding && Window != null)
                Window.Position = Placed(point) - _held;
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left)
                return;

            if (!_holding || Window == null)
                return;

            _holding = false;
            Window.Position = Placed(point) - _held;
            ((IFloatingWindow)Window).EndMove();
        }

        /// <inheritdoc />
        /// <remarks>The window stays where the last move put it.</remarks>
        protected override void OnPointerCaptureLost()
        {
            if (!_holding)
                return;

            _holding = false;
            if (Window != null)
                ((IFloatingWindow)Window).EndMove();
        }

        /// <summary>
        ///     Converts <paramref name="point" /> from this bar's layout to the layout the window is placed in, which
        ///     stays fixed while the window moves.
        /// </summary>
        Vector2 Placed(Vector2 point)
        {
            var screen = ToScreen(point);
            return Window.Parent?.FromScreen(screen) ?? screen;
        }
    }
}
