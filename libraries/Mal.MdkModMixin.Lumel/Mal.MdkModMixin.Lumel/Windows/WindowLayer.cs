using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>
    ///     The windows open over a screen, in the order they were opened: drawn after it with the last on top, and hit
    ///     tested before it from the top down.
    /// </summary>
    public sealed partial class WindowLayer
    {
        readonly List<Window> _open = new List<Window>();
        readonly List<Countdown> _countdowns = new List<Countdown>();
        readonly List<Countdown> _due = new List<Countdown>();
        int _now;
        ITheme _theme;

        /// <summary>
        ///     Gets or sets the resources the windows open here, and the screen this layer covers, find above everything in
        ///     them: their workspace's.
        /// </summary>
        public ResourceDictionary Resources { get; set; }

        /// <summary>Gets the number of open windows.</summary>
        public int Count => _open.Count;

        /// <summary>
        ///     Gets the open window at <paramref name="index" /> counted from the bottom, with the first opened at 0.
        /// </summary>
        public Window this[int index] => _open[index];

        /// <summary>
        ///     Whether every open window is a floating one, which leaves the screen beneath free to use around them.
        /// </summary>
        public bool Floats
        {
            get
            {
                foreach (var window in _open)
                {
                    if (!(window is FloatingWindow))
                        return false;
                }

                return true;
            }
        }

        /// <summary>Gets the window opened last, which is drawn over the rest, or null if none is open.</summary>
        public Window Top => _open.Count > 0 ? _open[_open.Count - 1] : null;

        /// <summary>Gets the topmost modal window, which receives all input, or null if none is open.</summary>
        public Window Modal
        {
            get
            {
                for (var i = _open.Count - 1; i >= 0; i--)
                {
                    if (_open[i].IsModal)
                        return _open[i];
                }

                return null;
            }
        }

        /// <summary>
        ///     Makes this the layer over <paramref name="screen" />, so that everything on it opens its windows here.
        /// </summary>
        public void Covers(Element screen)
        {
            ((ILayered)screen).Layer = this;
        }

        /// <summary>Raised when a window is put on top of the layer, with that window.</summary>
        public event Action<Window> Opened;

        /// <summary>Raised when a window has been removed from the layer, however it closed, with that window.</summary>
        public event Action<Window> Closed;

        /// <summary>Puts <paramref name="window" /> on top; a window that is already open stays where it is.</summary>
        public void Open(Window window)
        {
            if (_open.Contains(window))
                return;

            _open.Add(window);
            ((ILayered)window).Layer = this;
            ((IThemed)window).UseTheme(_theme);
            ((IShown)window).IsShown = true;
            ((IWindowLifetime)window).PutUp();
            Opened?.Invoke(window);
        }

        /// <summary>
        ///     Gets or sets the theme that supplies the style for each control in a window here that has none, or null
        ///     to use Lumel's game look.
        /// </summary>
        public ITheme Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                foreach (var window in _open)
                    ((IThemed)window).UseTheme(value);
            }
        }

        /// <summary>Removes <paramref name="window" />, if it is open.</summary>
        public void Close(Window window)
        {
            Close(window, CloseReason.Closed);
        }

        /// <summary>Removes <paramref name="window" />, if it is open, telling it it closed for <paramref name="reason" />.</summary>
        public void Close(Window window, CloseReason reason)
        {
            if (_open.Remove(window))
                TakenAway(window, reason);
        }

        /// <summary>
        ///     Tells <paramref name="window" />, just removed, that it is closed for <paramref name="reason" /> and no longer
        ///     shown, then raises <see cref="Closed" />.
        /// </summary>
        void TakenAway(Window window, CloseReason reason)
        {
            ((ILayered)window).Layer = null;
            ((IShown)window).IsShown = false;
            ((IWindowLifetime)window).TakenAway(reason);
            Closed?.Invoke(window);
        }

        /// <summary>Returns true if <paramref name="window" /> is open.</summary>
        public bool IsOpen(Window window)
        {
            return _open.Contains(window);
        }

        /// <summary>
        ///     Lays out and draws every open window over <paramref name="area" />, each modal one over a backdrop
        ///     covering everything beneath it.
        /// </summary>
        /// <remarks>A window is as wide and as tall as its placement says where it says, and otherwise as it asks, up to the area.</remarks>
        public void Draw(IGraphics g, RectangleF area)
        {
            Draw(g, area, area);
        }

        /// <summary>
        ///     Lays out and draws every open window in the area it chose, the whole of <paramref name="screen" /> or the
        ///     terminal's <paramref name="terminal" /> within it, each modal one over a backdrop covering the screen.
        /// </summary>
        /// <remarks>
        ///     A window is as wide and as tall as its placement says where it says, and otherwise as it asks, up to its area.
        ///     A popup whose anchor is no longer shown here closes first.
        /// </remarks>
        public void Draw(IGraphics g, RectangleF screen, RectangleF terminal)
        {
            CloseStranded();
            foreach (var window in _open)
            {
                var area = window.Area == ScreenArea.Terminal ? terminal : screen;
                var sized = window.Placement as ISized;
                var wide = sized != null && !float.IsNaN(sized.Width);
                var tall = sized != null && !float.IsNaN(sized.Height);

                var floating = window as FloatingWindow;
                if (floating != null)
                    ((IFloatingWindow)floating).Screen = g.ScreenBounds;

                var room = window.Room(area);
                window.Measure(g, new Vector2(wide ? sized.Width : room.X, tall ? sized.Height : room.Y));

                // Painted once the window is measured, as a window works out its look, backdrop and all, at its first measure.
                if (window.IsModal && window.Backdrop.A > 0)
                    g.Fill(screen, window.Backdrop);

                var size = window.DesiredSize;
                if (tall)
                    size.Y = sized.Height;

                if (wide)
                    size.X = sized.Width;

                window.Arrange(window.Place(area, size));
                window.DrawBehind(g);
                window.Draw(g);
            }
        }

        /// <summary>
        ///     Closes every popup whose anchor is no longer shown here, as when it is taken out of the tree or something
        ///     holding it is hidden; a popup with <see cref="Popup.FnAnchor" /> moves to the element that finds instead,
        ///     where that is shown.
        /// </summary>
        void CloseStranded()
        {
            // Hiding an element raises nothing its descendants hear, so the open popups are checked once a frame: a walk up
            // from each anchor, of which there are rarely more than one or two.
            for (var i = 0; i < _open.Count;)
            {
                var popup = _open[i] as Popup;
                if (popup == null || !Stranded(popup))
                {
                    i++;
                    continue;
                }

                _open.RemoveAt(i);
                TakenAway(popup, CloseReason.Closed);
            }
        }

        /// <summary>
        ///     Returns true if <paramref name="popup" /> hangs from an element no longer shown here, and nothing its
        ///     <see cref="Popup.FnAnchor" /> finds is shown here either; moves it to what that finds where it is.
        /// </summary>
        bool Stranded(Popup popup)
        {
            if (popup.Anchor == null || Shows(popup.Anchor))
                return false;

            var found = popup.FnAnchor?.Invoke();
            if (found == null || !Shows(found))
                return true;

            popup.Anchor = found;
            return false;
        }

        /// <summary>
        ///     Returns true if <paramref name="element" /> and everything holding it are visible, on the screen this layer
        ///     covers or in a window open here.
        /// </summary>
        bool Shows(Element element)
        {
            for (var at = element; at != null; at = at.Parent)
            {
                if (!at.IsVisible)
                    return false;
            }

            return element.Windows == this;
        }

        /// <summary>
        ///     Runs <paramref name="action" /> once, on the first <see cref="Tick" /> at least <paramref name="ticks" />
        ///     after the last one, unless it is canceled first.
        /// </summary>
        /// <remarks>
        ///     Canceled as <paramref name="owner" /> stops being shown, as when the window it is in closes. Throws where
        ///     <paramref name="owner" /> is not shown.
        /// </remarks>
        public ICountdown After(Element owner, int ticks, Action action)
        {
            if (!owner.IsLoaded)
                throw new InvalidOperationException("Only an element that is shown can wait on a countdown.");

            var countdown = new Countdown(this, owner, _now + ticks, action);
            _countdowns.Add(countdown);
            return countdown;
        }

        /// <summary>Runs every countdown due at <paramref name="now" />, in the drawing's ticks, in the order they were made.</summary>
        /// <remarks>Called by the host once a frame, before the windows are laid out; it returns at once while nothing waits.</remarks>
        public void Tick(int now)
        {
            _now = now;
            if (_countdowns.Count == 0)
                return;

            for (var i = 0; i < _countdowns.Count; i++)
            {
                if (_countdowns[i].Due <= now)
                    _due.Add(_countdowns[i]);
            }

            foreach (var countdown in _due)
                _countdowns.Remove(countdown);

            // An action may cancel a countdown due with it, which then does not run.
            foreach (var countdown in _due)
                countdown.Run();

            _due.Clear();
        }

        /// <summary>
        ///     Closes every popup that a press at <paramref name="point" /> misses, from the top down to the first
        ///     window the press lands in, and returns true if any closed and the press goes no further.
        /// </summary>
        /// <remarks>
        ///     For whoever takes presses to ask before anything else, since a press that closes a popup goes no further,
        ///     unless it lands in the popup the last one closed hung from, as a press on a menu goes on to it once the
        ///     submenu it opened closes.
        /// </remarks>
        public bool Dismiss(Vector2 point)
        {
            Popup closed = null;
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var window = _open[i];
                if (On(window, point) || !(window is Popup))
                    break;

                _open.RemoveAt(i);
                TakenAway(window, CloseReason.Closed);
                closed = (Popup)window;
            }

            if (closed == null)
                return false;

            var under = _open.Count > 0 ? _open[_open.Count - 1] : null;
            return !(under is Popup && On(under, point) && WindowOf(closed.Anchor) == under);
        }

        /// <summary>Returns the window <paramref name="element" /> is in, or null if it is in none.</summary>
        static Window WindowOf(Element element)
        {
            var top = element;
            while (top?.Parent != null)
                top = top.Parent;

            return top as Window;
        }

        /// <summary>
        ///     Returns the element in the windows that a press at <paramref name="point" /> reaches, or null;
        ///     <paramref name="stopped" /> is true if a window there, or a modal one, keeps the press from the screen
        ///     beneath.
        /// </summary>
        /// <remarks>A window keeps every press on it to itself, where something in it takes the press or not.</remarks>
        public Element HitTest(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var hit = _open[i].HitTest(point);
                if (hit != null || On(_open[i], point))
                {
                    stopped = true;
                    return hit;
                }

                if (_open[i].IsModal)
                {
                    stopped = true;
                    return null;
                }
            }

            stopped = false;
            return null;
        }

        /// <summary>
        ///     Returns the element in the windows that a wheel turned at <paramref name="point" /> is offered to first,
        ///     or null; <paramref name="stopped" /> is true if a window there, or a modal one, keeps it from the screen
        ///     beneath.
        /// </summary>
        public Element WheelAt(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var window = _open[i];
                if (On(window, point) || window.IsModal)
                {
                    stopped = true;
                    return window.WheelAt(point);
                }
            }

            stopped = false;
            return null;
        }

        /// <summary>
        ///     Returns the element in the windows under the pointer at <paramref name="point" /> that has a tip to
        ///     show, or null; <paramref name="stopped" /> is set as for <see cref="HitTest" />.
        /// </summary>
        public Element TipOwnerAt(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var found = _open[i].TipOwnerAt(point);
                if (found != null || On(_open[i], point) || _open[i].IsModal)
                {
                    stopped = true;
                    return found;
                }
            }

            stopped = false;
            return null;
        }

        /// <summary>
        ///     Returns true if <paramref name="point" /> is on <paramref name="window" />, anywhere in it whether or
        ///     not what is there takes input.
        /// </summary>
        static bool On(Window window, Vector2 point)
        {
            return window.IsVisible && window.Bounds.Contains(point);
        }
    }
}
