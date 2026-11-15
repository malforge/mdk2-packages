using System;
using Lumel.Drawing;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>
    ///     An element that floats over the screen in a window layer, holding one element and placed by the layer rather
    ///     than by a container.
    /// </summary>
    public class Window : ContentControl, IWindowLifetime
    {
        /// <summary>
        ///     Gets or sets the style of this window, or null to use the host theme's default for this kind of window.
        /// </summary>
        public new IStyle<Window> Style
        {
            get { return (IStyle<Window>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this window's own look, applied in every state on top of the style's look.</summary>
        public new WindowLook Base => (WindowLook)base.Base;

        /// <summary>Gets this window's own look when hovered, applied on top of the style's look.</summary>
        public new WindowLook PointerOver => (WindowLook)base.PointerOver;

        /// <summary>Gets this window's own look when disabled, applied on top of the style's look.</summary>
        public new WindowLook Disabled => (WindowLook)base.Disabled;

        /// <summary>
        ///     Gets this window's own look for <paramref name="state" />, or for every state that includes it, applied
        ///     on top of the style's look.
        /// </summary>
        public new WindowLook this[State state] => (WindowLook)base[state];

        /// <summary>Raised when the window has opened in a layer.</summary>
        public event Action Opened;

        /// <summary>
        ///     Raised when the layer it was open in removes it, however it was closed: by itself, by being accepted, by a
        ///     press elsewhere, by Escape, or by its workspace closing as the world unloads.
        /// </summary>
        /// <remarks>Raised with why it closed, so a window can tell the world unloading from any other close.</remarks>
        public event Action<CloseReason> Closed;

        /// <summary>
        ///     Gets or sets the part of the game's screen the window is laid out in: the size of the game's terminal in the
        ///     middle, as the game's own screens are, or the whole screen.
        /// </summary>
        /// <remarks>The terminal's box for a window, and the whole screen for a popup, unless set otherwise.</remarks>
        public ScreenArea Area { get; set; } = ScreenArea.Terminal;

        /// <summary>Whether nothing beneath the window receives input while it is open.</summary>
        public bool IsModal { get; set; }

        /// <summary>Whether it is open in a layer.</summary>
        public bool IsOpen => Layer != null;

        /// <summary>Gets the color a modal window paints over everything beneath it.</summary>
        /// <remarks>Set by this window's looks.</remarks>
        public Color Backdrop { get; private set; }

        /// <summary>Gets the shadow the window casts on what lies beneath it, or null for none.</summary>
        /// <remarks>Set by this window's looks.</remarks>
        public Shadow Shadow { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Window>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new WindowLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var window = (WindowLook)look;
            Backdrop = window.Backdrop ?? Color.Transparent;
            Shadow = window.Shadow;
        }

        /// <summary>Removes the window from the layer it is open in, if it is open.</summary>
        public void Close()
        {
            var layer = Layer;
            if (layer != null)
                layer.Close(this);
        }

        /// <summary>
        ///     Removes the window from the layer it is open in, if it is open, as the player finishing with it, so
        ///     <see cref="Closed" /> is raised with <see cref="CloseReason.Accepted" />.
        /// </summary>
        public void Accept()
        {
            var layer = Layer;
            if (layer != null)
                layer.Close(this, CloseReason.Accepted);
        }

        /// <summary>Called when the window has opened, before <see cref="Opened" /> is raised.</summary>
        protected virtual void OnOpened()
        {
        }

        /// <summary>Called when the window has closed for <paramref name="reason" />, before <see cref="Closed" /> is raised.</summary>
        /// <remarks>For a window to clean up after itself, however it was closed.</remarks>
        protected virtual void OnClosed(CloseReason reason)
        {
        }

        /// <inheritdoc />
        void IWindowLifetime.PutUp()
        {
            OnOpened();
            Opened?.Invoke();
        }

        /// <inheritdoc />
        void IWindowLifetime.TakenAway(CloseReason reason)
        {
            OnClosed(reason);
            Closed?.Invoke(reason);
        }

        /// <summary>
        ///     Returns the space within <paramref name="area" /> in which to measure the window: all of it, unless a
        ///     derived window says otherwise.
        /// </summary>
        public virtual Vector2 Room(RectangleF area)
        {
            return new Vector2(area.Width, area.Height);
        }

        /// <summary>
        ///     Draws whatever lies under and outside the window before the window itself: its <see cref="Shadow" />,
        ///     unless a derived window says otherwise.
        /// </summary>
        public virtual void DrawBehind(IGraphics g)
        {
            var shadow = Shadow;
            if (shadow != null)
                shadow.Draw(g, Bounds);
        }

        /// <summary>
        ///     Returns the bounds of the window of the given <paramref name="size" /> in <paramref name="area" />:
        ///     centered, unless a derived window says otherwise.
        /// </summary>
        public virtual RectangleF Place(RectangleF area, Vector2 size)
        {
            return new RectangleF(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f, size.X,
                size.Y);
        }
    }
}
