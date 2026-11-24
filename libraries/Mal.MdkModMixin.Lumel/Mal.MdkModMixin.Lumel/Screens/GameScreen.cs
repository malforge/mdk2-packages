using System;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Sandbox.ModAPI;
using VRageMath;
using IMyInput = VRage.ModAPI.IMyInput;

namespace Lumel.Screens
{
    /// <summary>
    ///     The game's screen a <see cref="Workspace" /> shows its windows on, which passes it the pointer, the wheel, the
    ///     keys and the gamepad.
    /// </summary>
    /// <remarks>Opened by its workspace as its first window opens, and closed once its last window has closed.</remarks>
    public class GameScreen : MyModScreen
    {
        const int DoubleClickFrames = 20;
        const float DoubleClickSlack = 12f;

        const float ScrollStep = 60f;

        readonly Workspace _view;
        readonly IScreenSpace _screen;
        readonly ScreenSpaceGraphics _graphics;
        readonly KeyFeed _keys = new KeyFeed();
        readonly GameKeyboard _keyboard = new GameKeyboard();
        readonly PadFeed _pad = new PadFeed();
        readonly GameGamepad _gamepad = new GameGamepad();

        Vector2 _pointer;
        PointerButton? _held;
        int? _lastDownFrame;
        Vector2 _lastDownAt;
        PointerButton _lastDownButton;
        int _clickCount;

        /// <summary>
        ///     Creates a screen named <paramref name="name" /> that shows the windows of <paramref name="view" /> on
        ///     <paramref name="screen" />, using <paramref name="images" /> for pictures and <paramref name="clock" />
        ///     for timing.
        /// </summary>
        public GameScreen(string name, Workspace view, IScreenSpace screen, IImageCatalog images, Func<int> clock)
            // No size: the game's full-screen screen, which draws no panel of its own, and each window lays itself out in
            // its own area of it.
            : base(name)
        {
            _view = view;
            _screen = screen;

            // BelowHud is where a mod screen's own content goes. The scissor is pushed onto this layer, so the two must agree
            // or clipping silently does nothing.
            _graphics = new ScreenSpaceGraphics(screen, MyScreenSpaceLayer.BelowHud, images, clock);

            // Off, and the view draws its own cross instead: the game lays its controls down before it calls the mod, so the
            // first opaque thing this screen paints buries the button while leaving it live.
            CloseButtonEnabled = false;
            CloseOnEscape = true;

            // The game draws the cursor, so the view does not.
            DrawMouseCursor = true;

            // Nothing drawn here is multiplied by the transition's alpha, so a fade would draw the screen fully opaque part way
            // through one.
            SkipTransition = true;
        }


        /// <inheritdoc />
        protected override void OnOpened()
        {
            _held = null;
            EnabledBackgroundFade = _view.DimsWorld;
            ((IScreenLifetime)_view).Opened();
        }

        /// <inheritdoc />
        protected override void OnClosed(bool isUnloading)
        {
            base.OnClosed(isUnloading);
            ((IScreenLifetime)_view).Closed(isUnloading);
        }

        /// <summary>
        ///     Handles Escape and the close request from the game, both of which arrive as Escape; the screen closes if
        ///     nothing in the view handled it.
        /// </summary>
        protected override bool OnCancelling()
        {
            return _keys.Escape(_keyboard, _view);
        }

        /// <inheritdoc />
        protected override void OnHandleInput()
        {
            IMyInput input = MyAPIGateway.Input;
            if (input == null)
                return;

            if (!GameInput.IsCleared)
                Dispatch(input);

            // The game reads the fade on every frame it draws, so it follows whichever window is on top.
            EnabledBackgroundFade = _view.DimsWorld;

            // Closed once the last window has, and no press is under way, so a press that closed it ends here.
            if (!_held.HasValue && _view.Windows.Count == 0)
                Close();
        }

        /// <summary>
        ///     Passes this frame's pointer, wheel, buttons, keys and gamepad from <paramref name="input" /> to the view.
        /// </summary>
        void Dispatch(IMyInput input)
        {
            // While the gamepad is in use the game hides the cursor, so nothing is under it.
            var padding = _gamepad.IsLastUsed;
            Vector2 pixels;
            if (_screen.TryCursorPixels(out pixels))
                _pointer = _graphics.FromPixels(pixels);

            _view.Pointer = padding ? new Vector2(float.NegativeInfinity) : _pointer;
            _view.TipsFollowFocus = padding;

            // A notch up is a positive delta and scrolls the content up, which is a negative amount. The game reports only the
            // vertical wheel, so a turn with Shift held scrolls sideways, as on the desktop.
            var wheel = input.DeltaMouseScrollWheelValue();
            if (wheel != 0)
            {
                var turn = -wheel / 120f * ScrollStep;
                _view.Wheel(_pointer, input.IsAnyShiftKeyPressed() ? new Vector2(turn, 0f) : new Vector2(0f, turn));
            }

            // One press at a time: the button that began it is the one whose release ends it, and a button that goes down
            // meanwhile is ignored.
            if (_held.HasValue)
            {
                if (IsHeld(input, _held.Value))
                {
                    _view.PointerMove(_pointer);
                }
                else
                {
                    var released = _held.Value;
                    _held = null;
                    _view.PointerUp(_pointer, released);
                }
            }
            else
            {
                var pressed = NewlyPressed(input);
                if (pressed.HasValue)
                {
                    _held = pressed;
                    _view.PointerDown(_pointer, pressed.Value, CountClicks(pressed.Value), input.IsAnyShiftKeyPressed(), input.IsAnyCtrlKeyPressed());
                }
            }

            _keys.Dispatch(_keyboard, _view, _graphics.Now);
            _pad.Dispatch(_gamepad, _view, _graphics.Now);
        }

        /// <inheritdoc />
        protected override void OnDraw(float transitionAlpha)
        {
            _graphics.Begin();

            // The game raises nothing when its options change, so the setting is read on every frame drawn.
            var config = MyAPIGateway.Session?.Config;
            _view.Windows.UserBackgroundOpacity = config != null ? config.UIBkOpacity : 1f;

            _view.Update();
            _view.DrawScreen(_graphics, _graphics.BoundsOf(ScreenArea.Window), _graphics.BoundsOf(ScreenArea.Terminal));
        }


        /// <summary>
        ///     Returns how many presses of <paramref name="button" /> in quick succession the one going down now is,
        ///     counted when the button goes down.
        /// </summary>
        /// <remarks>Counted here because the game reports double clicks only for its own controls.</remarks>
        int CountClicks(PointerButton button)
        {
            var now = _graphics.Now;

            var again = button == _lastDownButton
                        && _lastDownFrame.HasValue
                        && now - _lastDownFrame.Value <= DoubleClickFrames
                        && Vector2.Distance(_lastDownAt, _pointer) <= DoubleClickSlack;

            _clickCount = again ? _clickCount + 1 : 1;
            _lastDownFrame = now;
            _lastDownAt = _pointer;
            _lastDownButton = button;
            return _clickCount;
        }

        /// <summary>
        ///     Returns the button that has just gone down, preferring the left if more than one has, or null if none
        ///     has.
        /// </summary>
        static PointerButton? NewlyPressed(IMyInput input)
        {
            if (input.IsNewLeftMousePressed())
                return PointerButton.Left;

            if (input.IsNewRightMousePressed())
                return PointerButton.Right;

            if (input.IsNewMiddleMousePressed())
                return PointerButton.Middle;

            return null;
        }

        /// <summary>Whether <paramref name="button" /> is held down.</summary>
        static bool IsHeld(IMyInput input, PointerButton button)
        {
            switch (button)
            {
                case PointerButton.Left:
                    return input.IsLeftMousePressed();
                case PointerButton.Right:
                    return input.IsRightMousePressed();
                default:
                    return input.IsMiddleMousePressed();
            }
        }
    }
}
