using System.Windows;
using System.Windows.Media;

namespace Lumel.Sim
{
    /// <summary>
    ///     A scene for the screen to sit in front of, space over a hull, so that a picture of the screen shows where
    ///     its edges are.
    /// </summary>
    /// <remarks>It stands for no particular place in the game; it gives a translucent screen something with edges to be judged against.</remarks>
    public sealed class Backdrop : FrameworkElement
    {
        /// <inheritdoc />
        protected override void OnRender(DrawingContext context)
        {
            Paint(context, RenderSize);
        }

        /// <summary>Draws the backdrop into <paramref name="context" /> at the size <paramref name="into" />.</summary>
        public void Paint(DrawingContext context, Size into)
        {
            double width = into.Width;
            double height = into.Height;
            if (width <= 0d || height <= 0d)
                return;

            var space = new LinearGradientBrush
            {
                StartPoint = new Point(0d, 0d),
                EndPoint = new Point(0d, 1d),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(9, 13, 20), 0d),
                    new GradientStop(Color.FromRgb(26, 36, 48), 0.62d),
                    new GradientStop(Color.FromRgb(64, 74, 82), 0.66d),
                    new GradientStop(Color.FromRgb(36, 40, 44), 1d)
                }
            };

            context.DrawRectangle(space, null, new Rect(0d, 0d, width, height));

            // Hull plating below the horizon, lit from the left, so the veil has edges to soften and
            // not only a wash to darken.
            var plate = new SolidColorBrush(Color.FromArgb(38, 190, 205, 220));
            var seam = new Pen(new SolidColorBrush(Color.FromArgb(60, 10, 14, 18)), 1d);

            double top = height * 0.66d;
            double step = width / 9d;

            for (int i = 0; i < 9; i++)
            {
                var cell = new Rect(i * step, top, step, height - top);

                if ((i & 1) == 0)
                    context.DrawRectangle(plate, null, cell);

                context.DrawLine(seam, cell.TopLeft, cell.BottomLeft);
            }

            context.DrawLine(
                new Pen(new SolidColorBrush(Color.FromArgb(110, 210, 225, 240)), 1d),
                new Point(0d, top), new Point(width, top));
        }
    }
}
