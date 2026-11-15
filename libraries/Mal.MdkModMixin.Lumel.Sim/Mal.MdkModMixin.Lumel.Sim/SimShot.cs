using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lumel.Drawing;
using Lumel.Screens;
using Color = VRageMath.Color;
using Point = System.Windows.Point;

namespace Lumel.Sim
{
    /// <summary>Renders a frame to a PNG with no window and nobody watching, using the game's own fonts.</summary>
    /// <remarks>Rendering needs a single-threaded apartment: a console's <c>Main</c> is marked <c>[STAThread]</c>.</remarks>
    public static class SimShot
    {
        /// <summary>
        ///     Writes <paramref name="workspace" />, its layers and then its windows laid out in its area of a frame of
        ///     <paramref name="width" /> by <paramref name="height" /> pixels, to <paramref name="png" /> in front of
        ///     <paramref name="behind" />, or in front of <see cref="Backdrop" /> if <paramref name="behind" /> is null.
        /// </summary>
        /// <remarks>The workspace lights what its <see cref="Workspace.Pointer" /> rests on, where the caller sets one.</remarks>
        public static void Save(string png, int width, int height, SimContent content, Workspace workspace, Action<DrawingContext, Size>? behind = null)
        {
            Save(png, width, height, content, graphics =>
            {
                var screen = graphics.BoundsOf(ScreenArea.Window);
                var terminal = graphics.BoundsOf(ScreenArea.Terminal);
                workspace.DrawHud(graphics, screen, terminal);
                workspace.DrawScreen(graphics, screen, terminal);
            }, behind);
        }

        /// <summary>
        ///     Writes whatever <paramref name="frame" /> draws, in a frame of <paramref name="width" /> by
        ///     <paramref name="height" /> pixels, to <paramref name="png" /> in front of <paramref name="behind" />.
        /// </summary>
        public static void Save(string png, int width, int height, SimContent content, Action<ScreenSpaceGraphics> frame,
            Action<DrawingContext, Size>? behind = null)
        {
            var surface = new Surface(content);
            surface.Target = new PixelSize(width, height);
            surface.Frame += frame;

            var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            Take(bitmap, behind, surface, new Size(width, height));
            Write(bitmap, png);
        }

        /// <summary>Returns a flat color to draw behind the frame.</summary>
        public static Action<DrawingContext, Size> Plain(Color color)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            return (context, size) => context.DrawRectangle(brush, null, new Rect(0d, 0d, size.Width, size.Height));
        }

        /// <summary>
        ///     Returns a background that stretches the picture in <paramref name="file" />, such as a screenshot of the
        ///     game, across the frame.
        /// </summary>
        public static Action<DrawingContext, Size> Picture(string file)
        {
            var picture = new BitmapImage(new Uri(Path.GetFullPath(file), UriKind.Absolute));
            return (context, size) => context.DrawImage(picture, new Rect(new Point(0d, 0d), size));
        }

        /// <summary>
        ///     Draws the screen as it is now into <paramref name="bitmap" />, in front of <paramref name="behind" />,
        ///     or in front of <see cref="Backdrop" /> if <paramref name="behind" /> is null.
        /// </summary>
        /// <remarks>
        ///     Painted into a visual of its own rather than by invalidating a control and rendering it. This framework repaints
        ///     a control when its dispatcher next runs, and a shot has no dispatcher to run - so that route quietly writes out
        ///     the frame before the one that was asked for.
        /// </remarks>
        public static void Take(RenderTargetBitmap bitmap, Action<DrawingContext, Size>? behind, Surface surface, Size size)
        {
            var frame = new DrawingVisual();
            using (DrawingContext context = frame.RenderOpen())
            {
                // Something behind it, or the bitmap is transparent, which reads as white and makes every light-on-dark
                // choice look wrong.
                (behind ?? new Backdrop().Paint)(context, size);
                surface.Paint(context, size);
            }

            // Cleared as well, because the bitmap composites onto what is already in it.
            bitmap.Clear();
            bitmap.Render(frame);
        }

        /// <summary>Writes <paramref name="bitmap" /> to the file <paramref name="png" />.</summary>
        public static void Write(RenderTargetBitmap bitmap, string png)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream file = File.Create(png))
                encoder.Save(file);
        }
    }
}
