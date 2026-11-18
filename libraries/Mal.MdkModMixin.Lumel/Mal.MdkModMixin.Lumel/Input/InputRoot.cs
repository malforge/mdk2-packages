using System;
using System.Collections.Generic;
using VRage.Input;
using Lumel.Viewing;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Input
{
    /// <summary>
    ///     The entry point for pointer and keyboard input into one tree of elements, tracking which element has
    ///     keyboard focus and which holds the pointer.
    /// </summary>
    /// <remarks>The host turns the game's input into these calls.</remarks>
    public sealed class InputRoot
    {
        readonly Element _root;


        readonly List<Element> _order = new List<Element>();

        Element _captured;
        PointerButton _capturedWith;
        Element _focused;
        bool _keysLast;

        /// <summary>
        ///     Creates input handling for the tree under <paramref name="root" />, or for trees a host finds itself where
        ///     <paramref name="root" /> is null.
        /// </summary>
        public InputRoot(Element root)
        {
            _root = root;
            Clipboard = new Clipboard();
        }

        /// <summary>Gets the element that has keyboard focus, or null if none does.</summary>
        public Element Focused => _focused;

        /// <summary>
        ///     Gets or sets where text copied or cut from elements under this root is stored and pasted from; in memory
        ///     unless the host sets otherwise.
        /// </summary>
        public IClipboard Clipboard { get; set; }

        /// <summary>Raised after keyboard focus has moved to another element, or to none.</summary>
        public event Action FocusChanged;

        /// <summary>
        ///     Raised when keys move to an element without pressing it, as when they move the focus or a menu's
        ///     highlight, with that element.
        /// </summary>
        public event Action<Element> Reached;

        /// <summary>Raised as a press, by pointer or key, runs an element's command, with that element, before the command runs.</summary>
        public event Action<Element> Pressed;

        /// <summary>
        ///     Raised when a press begins, before it reaches anything, with the element it will reach, or null if none.
        /// </summary>
        public event Action<Element> PreviewPointerDown;

        /// <summary>
        ///     Raised when the wheel turns, before it is offered to any element; a handler that marks it handled stops
        ///     it there.
        /// </summary>
        public event Action<WheelEventArgs> PreviewWheel;

        /// <summary>
        ///     Gets or sets the element that presses, the wheel and keyboard focus moves are confined to, or null if
        ///     input is unconfined.
        /// </summary>
        /// <remarks>A press outside it reaches nothing and leaves the keyboard where it is.</remarks>
        public Element Confined { get; set; }

        /// <summary>
        ///     Returns true if input may reach <paramref name="element" />, that is, it is inside
        ///     <see cref="Confined" /> or nothing is confined.
        /// </summary>
        public bool Allows(Element element)
        {
            if (Confined == null)
                return true;

            for (var at = element; at != null; at = Above(at))
            {
                if (at == Confined)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Offers a wheel turn of <paramref name="amount" /> UI units, positive down along Y and right along X, to
        ///     <paramref name="from" />, then to each element above it, and returns true if one of them handled it.
        /// </summary>
        /// <remarks>
        ///     <paramref name="from" /> is what the host found under the pointer, such as by <see cref="Element.WheelAt" />.
        ///     <see cref="PreviewWheel" /> is raised first.
        /// </remarks>
        public bool Wheel(Element from, Vector2 amount)
        {
            if (!Allows(from))
                return false;

            var preview = PreviewWheel;
            if (preview != null)
            {
                var turned = new WheelEventArgs(from, amount);
                preview(turned);
                if (turned.Handled)
                    return true;
            }

            for (var at = from; at != null; at = Above(at))
            {
                if (((IInputTarget)at).Wheel(amount))
                    return true;
            }

            return false;
        }

        /// <summary>Gives keyboard focus to <paramref name="element" />, or clears focus if it is null.</summary>
        /// <remarks>
        ///     Every scroll view the element is in scrolls it into view. The element shows its focus where the keys were used
        ///     more recently than the pointer, or where it is <see cref="Element.Focusable" />.
        /// </remarks>
        public void Focus(Element element)
        {
            Focus(element, false);
        }

        /// <summary>
        ///     Gives keyboard focus to <paramref name="element" />, or clears focus if it is null, where
        ///     <paramref name="byPress" /> says whether a pointer press is what moved it.
        /// </summary>
        void Focus(Element element, bool byPress)
        {
            if (_focused == element)
                return;

            if (_focused != null)
                ((IInputTarget)_focused).Blur();

            _focused = element;

            if (_focused != null)
            {
                ((IInputTarget)_focused).Focus(byPress);
                ((IInputTarget)_focused).ShowsFocus = _keysLast || _focused.Focusable;
                for (var at = Above(_focused); at != null; at = Above(at))
                    (at as ScrollView)?.BringIntoView(_focused);
            }

            var changed = FocusChanged;
            if (changed != null)
                changed();
        }

        /// <summary>
        ///     Begins a press of <paramref name="button" /> at <paramref name="point" />, where
        ///     <paramref name="clickCount" /> counts the presses in quick succession and <paramref name="shift" /> and
        ///     <paramref name="ctrl" /> say whether those keys are held, and returns true if an element handled it.
        /// </summary>
        /// <remarks>
        ///     Focus goes to the nearest element at or above what was pressed that is focusable or has a command, or to the
        ///     element it hands its focus to, and leaves when there is none. One that only has a command holds the keyboard
        ///     without showing it, until a key is used.
        /// </remarks>
        public bool PointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (_root == null)
                throw new InvalidOperationException("Input with no root finds nothing at a point; give the press its target.");

            return PointerDown(_root.HitTest(point), point, button, clickCount, shift, ctrl);
        }

        /// <summary>
        ///     Begins a press of <paramref name="button" /> at <paramref name="point" /> on <paramref name="target" />,
        ///     which the host has already found as one that layers other trees over this one does, where
        ///     <paramref name="clickCount" /> counts the presses in quick succession and <paramref name="shift" /> and
        ///     <paramref name="ctrl" /> say whether those keys are held.
        /// </summary>
        public bool PointerDown(Element target, Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            // A press still held whose release never came ends here, as the new one begins.
            ReleasePointerCapture();

            if (Confined != null && (target == null || !Allows(target)))
                return false;

            _keysLast = false;
            PreviewPointerDown?.Invoke(target);
            Focus(FocusableFrom(target), true);

            _captured = target;
            _capturedWith = button;
            if (target == null)
                return false;

            ((IInputTarget)target).PointerDown(target.FromScreen(point), button, clickCount, shift, ctrl);
            return true;
        }

        /// <summary>
        ///     Moves the pointer to <paramref name="point" />, which the element a press began on receives.
        /// </summary>
        public void PointerMove(Vector2 point)
        {
            if (_captured != null)
                ((IInputTarget)_captured).PointerMove(_captured.FromScreen(point), _capturedWith);
        }

        /// <summary>
        ///     Ends the press under way, if any, without a release, telling the element it began on that it has lost
        ///     the pointer.
        /// </summary>
        /// <remarks>For a host whose press can end unseen, as when its screen opens or closes.</remarks>
        public void ReleasePointerCapture()
        {
            var lost = _captured;
            _captured = null;
            if (lost != null)
                ((IInputTarget)lost).PointerCaptureLost();
        }

        /// <summary>
        ///     Ends the press of <paramref name="button" /> at <paramref name="point" />, running the pressed element's
        ///     command if it is the left button and the press ends on that element.
        /// </summary>
        /// <remarks>The release of a button other than the one the press began with leaves the press under way.</remarks>
        public void PointerUp(Vector2 point, PointerButton button)
        {
            var released = _captured;
            if (released == null || button != _capturedWith)
                return;

            _captured = null;

            var local = released.FromScreen(point);
            ((IInputTarget)released).PointerUp(local, button);

            if (button == PointerButton.Left && released.Holds(local))
                Press(released);
        }

        /// <summary>Tells whoever listens to <see cref="Reached" /> that keys have moved to <paramref name="element" />.</summary>
        public void Reach(Element element)
        {
            Reached?.Invoke(element);
        }

        /// <summary>
        ///     Presses <paramref name="element" />, running its command, and returns true if it has one it can run.
        /// </summary>
        /// <remarks>
        ///     For a press from any device: the pointer's release presses what it was pressed on, and a key presses what it
        ///     stands for. <see cref="Pressed" /> is raised before the command runs.
        /// </remarks>
        public bool Press(Element element)
        {
            if (element.Command == null || !element.IsEnabled)
                return false;

            Pressed?.Invoke(element);
            ((IInputTarget)element).Pressed();
            element.Command.Execute();
            return true;
        }

        /// <summary>
        ///     Gets what a key that no element handled means, by key; by default Enter accepts, Space presses, Escape
        ///     cancels and the arrows move focus, until the host changes it.
        /// </summary>
        public Dictionary<MyKeys, Intent> Intents { get; } = new Dictionary<MyKeys, Intent>
        {
            { MyKeys.Enter, Intent.Accept },
            { MyKeys.Space, Intent.Press },
            { MyKeys.Escape, Intent.Cancel },
            { MyKeys.Up, Intent.Up },
            { MyKeys.Down, Intent.Down },
            { MyKeys.Left, Intent.Left },
            { MyKeys.Right, Intent.Right }
        };

        /// <summary>Offers a key press and returns true if an element handled it.</summary>
        /// <remarks>
        /// What holds the keyboard is offered it first, and then whatever holds that, out to the root. Tab then moves the
        /// focus on, or back with shift; any other key is offered as what it means, by <see cref="Intents" />, the same way,
        /// except that an accept presses what holds the keyboard where that has a command it can run, and a press only ever
        /// presses what holds the keyboard. With nothing holding the keyboard, Tab and the arrows give it to the first
        /// element that can take it, or the last for Shift+Tab, Up and Left.
        /// A cancel nobody took lets the keyboard go, and still answers that nothing took the key.
        /// </remarks>
        public bool Key(MyKeys key, bool shift, bool ctrl)
        {
            return Key(_focused, key, shift, ctrl);
        }

        /// <summary>
        ///     Offers a key press to <paramref name="from" />, which the host has already found as one that layers
        ///     other trees over this one does, then to each element above it, and returns true if one of them handled
        ///     it.
        /// </summary>
        /// <remarks>Offered as <see cref="Key(MyKeys, bool, bool)" /> offers it, starting from <paramref name="from" /> rather than what holds the keyboard.</remarks>
        public bool Key(Element from, MyKeys key, bool shift, bool ctrl)
        {
            if (!_keysLast)
            {
                _keysLast = true;
                if (_focused != null)
                    ((IInputTarget)_focused).ShowsFocus = true;
            }

            for (var at = from; at != null; at = Above(at))
            {
                if (((IInputTarget)at).Key(this, key, shift, ctrl))
                    return true;
            }

            if (key == MyKeys.Tab)
                return FocusNext(from, shift);

            Intent intent;
            if (!Intents.TryGetValue(key, out intent))
                return false;

            if ((intent == Intent.Accept || intent == Intent.Press) && from != null && from == _focused && Press(from))
                return true;

            if (intent == Intent.Press)
                return false;

            for (var at = from; at != null; at = Above(at))
            {
                if (((IInputTarget)at).Intent(this, intent))
                    return true;
            }

            if (intent == Intent.Cancel)
                Focus(null);

            if (intent == Intent.Accept || intent == Intent.Cancel)
                return false;

            return _focused == null ? FocusNext(from, intent == Intent.Up || intent == Intent.Left) : FocusToward(intent);
        }

        /// <summary>Sends a typed character to the element that has keyboard focus.</summary>
        public void Text(char c)
        {
            if (_focused != null)
                ((IInputTarget)_focused).Text(c);
        }

        /// <summary>
        ///     Moves focus from the focused element to the nearest focusable element in the direction
        ///     <paramref name="toward" /> names, and returns true if there was one.
        /// </summary>
        /// <remarks>Looks where <see cref="FocusNext(bool)" /> looks, measuring between the middles of the elements as they are drawn.</remarks>
        public bool FocusToward(Intent toward)
        {
            if (_focused == null)
                return false;

            Gather(_focused);
            var from = Middle(_focused);
            Element nearest = null;
            var best = float.MaxValue;

            foreach (var element in _order)
            {
                if (element == _focused)
                    continue;

                var apart = Middle(element) - from;
                float ahead;
                float aside;
                switch (toward)
                {
                    case Intent.Up:
                        ahead = -apart.Y;
                        aside = apart.X;
                        break;
                    case Intent.Down:
                        ahead = apart.Y;
                        aside = apart.X;
                        break;
                    case Intent.Left:
                        ahead = -apart.X;
                        aside = apart.Y;
                        break;
                    case Intent.Right:
                        ahead = apart.X;
                        aside = apart.Y;
                        break;
                    default:
                        throw new ArgumentException(toward + " is not a direction.", nameof(toward));
                }

                if (ahead <= 0f)
                    continue;

                // Straying to the side counts double, so the element in line wins over a nearer one off to the side.
                var cost = ahead + 2f * Math.Abs(aside);
                if (cost >= best)
                    continue;

                best = cost;
                nearest = element;
            }

            if (nearest == null)
                return false;

            Focus(nearest);
            Reach(nearest);
            return true;
        }

        /// <summary>
        ///     Moves focus to the next focusable element in tree order, or the previous one if <paramref name="back" />
        ///     is true, and returns true if there was one.
        /// </summary>
        /// <remarks>Looks inside what input is confined to, or else the tree what holds the keyboard is in, or else the whole root.</remarks>
        public bool FocusNext(bool back)
        {
            return FocusNext(_focused, back);
        }

        /// <summary>
        ///     Gives keyboard focus to the first element that can take it in the tree containing <paramref name="within" />,
        ///     where input is not confined elsewhere, showing it as the keys do, and returns true if there was one.
        /// </summary>
        public bool FocusFirst(Element within)
        {
            _keysLast = true;
            Gather(within);
            if (_order.Count == 0)
                return false;

            Focus(_order[0]);
            return true;
        }

        /// <summary>
        ///     Moves focus from the focused element to the next focusable element, or the previous one if
        ///     <paramref name="back" /> is true, among the focusable elements in the tree containing
        ///     <paramref name="within" />.
        /// </summary>
        bool FocusNext(Element within, bool back)
        {
            Gather(within);
            if (_order.Count == 0)
                return false;

            var at = _focused == null ? -1 : _order.IndexOf(_focused);
            int next;

            if (at < 0)
                next = back ? _order.Count - 1 : 0;
            else
                next = (at + (back ? _order.Count - 1 : 1)) % _order.Count;

            Focus(_order[next]);
            Reach(_order[next]);
            return true;
        }

        /// <summary>
        ///     Collects the focusable elements inside what input is confined to, or else in the tree or window
        ///     containing <paramref name="within" />, or the root.
        /// </summary>
        void Gather(Element within)
        {
            _order.Clear();
            if (Confined != null)
            {
                Collect(Confined);
                return;
            }

            var top = within;
            while (top != null && top.Parent != null)
                top = top.Parent;

            var tree = top ?? _root;
            if (tree != null)
                Collect(tree);
        }

        /// <summary>Returns the center of <paramref name="element" /> as it is drawn on the screen.</summary>
        static Vector2 Middle(Element element)
        {
            var bounds = element.Bounds;
            return element.ToScreen(new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f));
        }

        void Collect(Element element)
        {
            if (!element.IsVisible)
                return;

            if ((element.Focusable || element.Command != null) && element.IsEnabled && (element.InputTransparency & InputTransparency.Self) == 0 && element.FocusTarget == null)
                _order.Add(element);

            if ((element.InputTransparency & InputTransparency.Children) != 0)
                return;

            for (var i = 0; i < element.VisualChildCount; i++)
                Collect(element.GetVisualChild(i));
        }

        /// <summary>
        ///     Returns the element that holds <paramref name="element" />: its parent, or for a popup, the element it
        ///     hangs from.
        /// </summary>
        static Element Above(Element element)
        {
            if (element.Parent != null)
                return element.Parent;

            var popup = element as Popup;
            return popup?.Anchor;
        }

        static Element FocusableFrom(Element element)
        {
            for (var at = element; at != null; at = Above(at))
            {
                if ((at.Focusable || at.Command != null) && at.IsEnabled && (at.InputTransparency & InputTransparency.Self) == 0)
                    return at.FocusTarget == null ? at : at.FocusTarget.IsEnabled ? at.FocusTarget : null;
            }

            return null;
        }
    }
}
