using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A minus or a plus drawn as bars, half a line across, in the color of the button whose content it is, as on a
    ///     spin box's buttons.
    /// </summary>
    /// <remarks>Throws as it is measured where it is not the content of a <see cref="Button" />.</remarks>
    public class StepSign : Element
    {
        readonly bool _plus;

        /// <summary>Creates a plus where <paramref name="plus" /> is true, and a minus otherwise.</summary>
        public StepSign(bool plus)
        {
            _plus = plus;
        }

        /// <summary>Whether the sign is a plus rather than a minus.</summary>
        public bool IsPlus => _plus;

        /// <summary>Gets the button whose content the sign is.</summary>
        Button Holder
        {
            get
            {
                var button = Parent as Button;
                if (button == null)
                    throw new InvalidOperationException("A step sign is drawn as the content of a button, and this one is in none.");

                return button;
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var line = Holder.FontSize;
            return new Vector2(line * 0.5f, line);
        }

        /// <inheritdoc />
        /// <remarks>Its bars are an eighth of a line thick to the nearest unit, and at least one.</remarks>
        protected override void DrawContent(IGraphics g)
        {
            var holder = Holder;
            var room = ContentBounds;
            var side = room.Width;
            var stroke = MathHelper.Max(1f, (float)Math.Round(holder.FontSize * 0.125f));
            var middle = new Vector2(room.X + room.Width * 0.5f, room.Y + room.Height * 0.5f);

            g.Fill(new RectangleF(middle.X - side * 0.5f, middle.Y - stroke * 0.5f, side, stroke), holder.Color);
            if (_plus)
                g.Fill(new RectangleF(middle.X - stroke * 0.5f, middle.Y - side * 0.5f, stroke, side), holder.Color);
        }
    }
}
