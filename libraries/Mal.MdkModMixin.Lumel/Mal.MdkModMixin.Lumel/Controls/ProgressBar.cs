using System;
using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A flat bar filled from the left by <see cref="Value" />, a share from 0 to 1, over its background as the
    ///     track.
    /// </summary>
    /// <remarks>It asks for no room of its own, so its size comes from its placement.</remarks>
    public class ProgressBar : Element
    {
        float _value;

        /// <summary>
        ///     Creates an empty bar, using the look its host's theme holds for a progress bar until it is given a style.
        /// </summary>
        public ProgressBar()
        {
            Fill = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this bar, or null to use the default its host's theme holds for a progress bar.
        /// </summary>
        public new IStyle<ProgressBar> Style
        {
            get { return (IStyle<ProgressBar>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this bar's own look, applied in every state on top of the style's look.</summary>
        public new ProgressBarLook Base => (ProgressBarLook)base.Base;

        /// <summary>Gets this bar's own look when hovered, applied on top of the style's look.</summary>
        public new ProgressBarLook PointerOver => (ProgressBarLook)base.PointerOver;

        /// <summary>Gets this bar's own look when disabled, applied on top of the style's look.</summary>
        public new ProgressBarLook Disabled => (ProgressBarLook)base.Disabled;

        /// <summary>
        ///     Gets this bar's own look for <paramref name="state" />, or for every state that includes it, applied on top
        ///     of the style's look.
        /// </summary>
        public new ProgressBarLook this[State state] => (ProgressBarLook)base[state];

        /// <summary>Gets or sets how much of the bar is filled, from 0 for none to 1 for all; anything outside is held to it.</summary>
        public float Value
        {
            get { return _value; }
            set { _value = float.IsNaN(value) ? 0f : Math.Max(0f, Math.Min(1f, value)); }
        }

        /// <summary>The color of the filled part.</summary>
        /// <remarks>Set by this bar's looks.</remarks>
        public Color Fill { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ProgressBar>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ProgressBarLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            Fill = ((ProgressBarLook)look).Fill ?? Color.White;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var area = ContentBounds;
            var width = area.Width * _value;
            if (width > 0f && Fill.A > 0)
                g.Fill(new RectangleF(area.X, area.Y, width, area.Height), Fill);
        }
    }
}
