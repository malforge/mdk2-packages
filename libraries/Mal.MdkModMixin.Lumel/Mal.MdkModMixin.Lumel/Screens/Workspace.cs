using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Styling;
using Lumel.Windows;
using VRage.Input;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>
    ///     Everything a mod shows on the game's screen: three layers of HUD that take no input, and the windows it opens,
    ///     which take the mouse, the keyboard and the gamepad.
    /// </summary>
    /// <remarks>
    ///     The game's screen comes up with the first window opened and goes once the last one has closed, so a mod never
    ///     opens or closes it itself. A press first closes the popups and the tip it misses; a press that closes a popup goes
    ///     no further. Every point the workspace is given is in UI units, the units it is laid out and drawn in.
    /// </remarks>
    public class Workspace : IScreenLifetime, IDisposable
    {
        readonly string _name;
        readonly IScreenSpace _screen;
        readonly IImageCatalog _images;
        readonly Func<int> _clock;
        readonly Hud _hud;
        readonly TipHost _tips = new TipHost();
        readonly ResourceDictionary _resources = new ResourceDictionary();
        readonly WindowLayer _windows = new WindowLayer();
        readonly Dictionary<Window, bool> _dims = new Dictionary<Window, bool>();
        readonly InputRoot _input = new InputRoot(null);

        GameScreen _gameScreen;
        bool _screenUp;
        Element _pointerOver;
        ITheme _theme;

        /// <summary>
        ///     Creates a workspace that stands apart from the game, whose windows open without a game screen and whose
        ///     layers are drawn by <see cref="DrawHud(IGraphics, RectangleF, RectangleF)" /> and
        ///     <see cref="DrawScreen(IGraphics, RectangleF, RectangleF)" />, for tests and simulations.
        /// </summary>
        public Workspace()
        {
            _hud = new Hud(null, null, null) { Resources = _resources };
            _windows.Resources = _resources;
            _windows.Opened += Opened;
            _windows.Closed += Closed;
            _input.Pressed += Pressed;
            _input.Reached += Reached;
        }

        /// <summary>
        ///     Creates a workspace on the game's screen, whose game screen the game knows by <paramref name="name" />,
        ///     reading each picture locator as a material name and playing the game's own sounds.
        /// </summary>
        public Workspace(string name)
            : this(name, new GameScreenSpace(), null, null)
        {
        }

        /// <summary>
        ///     Creates a workspace drawn on <paramref name="screen" />, whose game screen the game knows by
        ///     <paramref name="name" />, using <paramref name="images" /> for pictures, or material names where it is null,
        ///     and <paramref name="clock" /> for timing, or the game's frame count where it is null.
        /// </summary>
        public Workspace(string name, IScreenSpace screen, IImageCatalog images, Func<int> clock) : this()
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            _name = name;
            _screen = screen;
            _images = images ?? new MaterialImages(screen.TextureSize);
            _clock = clock ?? (() => GameClock.Now);
            _hud = new Hud(screen, _images, _clock) { Resources = _resources };
            Sounds = new GameControlSounds();
        }

        /// <summary>
        ///     Gets or sets the element drawn over the world and under the game's HUD, placed by its own
        ///     <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        public Element BelowHud
        {
            get { return _hud.BelowHud; }
            set { _hud.BelowHud = value; }
        }

        /// <summary>
        ///     Gets or sets the element drawn over the game's HUD and under open screens such as the terminal, placed by its
        ///     own <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        /// <remarks>Shown while the game's HUD is hidden too.</remarks>
        public Element AboveHud
        {
            get { return _hud.AboveHud; }
            set { _hud.AboveHud = value; }
        }

        /// <summary>
        ///     Gets or sets the element drawn over everything, open screens and the game's tooltips included, placed by its
        ///     own <see cref="Element.Align" />, or null for nothing.
        /// </summary>
        public Element AboveEverything
        {
            get { return _hud.AboveEverything; }
            set { _hud.AboveEverything = value; }
        }

        /// <summary>
        ///     Gets or sets the part of the game's screen <see cref="BelowHud" /> is laid out in: the whole screen, the
        ///     default, as the game's own HUD is, or the size of the game's terminal in the middle.
        /// </summary>
        public ScreenArea BelowHudArea
        {
            get { return _hud.BelowHudArea; }
            set { _hud.BelowHudArea = value; }
        }

        /// <summary>
        ///     Gets or sets the part of the game's screen <see cref="AboveHud" /> is laid out in: the whole screen, the
        ///     default, as the game's own HUD is, or the size of the game's terminal in the middle.
        /// </summary>
        public ScreenArea AboveHudArea
        {
            get { return _hud.AboveHudArea; }
            set { _hud.AboveHudArea = value; }
        }

        /// <summary>
        ///     Gets or sets the part of the game's screen <see cref="AboveEverything" /> is laid out in: the whole
        ///     screen, the default, as the game's own HUD is, or the size of the game's terminal in the middle.
        /// </summary>
        public ScreenArea AboveEverythingArea
        {
            get { return _hud.AboveEverythingArea; }
            set { _hud.AboveEverythingArea = value; }
        }

        /// <summary>
        ///     Gets or sets the theme that gives each control shown here a style when it has none, or null for
        ///     <see cref="GameTheme" />.
        /// </summary>
        public ITheme Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                _hud.Theme = value;
                _windows.Theme = value;
                _tips.Theme = value;
            }
        }

        /// <summary>Gets or sets what plays the controls' sounds, or null for silence.</summary>
        /// <remarks>
        ///     The game's own sounds for a workspace on the game's screen. Each control's sound is its own, from its looks or
        ///     its <see cref="Element.FnSound" />, played as the pointer comes onto something a press would run a command on
        ///     or keys move to something, and as a press, by pointer or key, is about to run one.
        /// </remarks>
        public IControlSounds Sounds { get; set; }

        /// <summary>Gets or sets whether the controls play their sounds.</summary>
        public bool PlaysSounds { get; set; } = true;

        /// <summary>
        ///     Gets the resources everything the workspace shows finds above everything in it, its windows and its layers
        ///     alike, such as a <see cref="Codex" />.
        /// </summary>
        public ResourceDictionary Resources => _resources;

        /// <summary>Gets the windows open in the workspace, the last opened on top.</summary>
        public WindowLayer Windows => _windows;

        /// <summary>Gets the root that receives keys and typed characters while an element in a window has the keyboard.</summary>
        public InputRoot Input => _input;

        /// <summary>
        ///     Whether the game's screen should fade the world behind it: what the window on top asked for as it opened,
        ///     looking past the popups over it, which hang from what is beneath them.
        /// </summary>
        public bool DimsWorld
        {
            get
            {
                for (var i = _windows.Count - 1; i >= 0; i--)
                {
                    var window = _windows[i];
                    if (window is Popup)
                        continue;

                    bool dims;
                    return _dims.TryGetValue(window, out dims) && dims;
                }

                return false;
            }
        }

        /// <summary>
        ///     Returns the window on top that keys and Escape go to, looking past the coach marks over it, or null if none
        ///     is open.
        /// </summary>
        Window KeyedTop()
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                if (!(_windows[i] is CoachMark))
                    return _windows[i];
            }

            return null;
        }

        /// <summary>
        ///     Gets or sets the pointer position in UI units; a position outside the workspace means there is no
        ///     pointer.
        /// </summary>
        /// <remarks>Outside every room until the host first says where it is.</remarks>
        public Vector2 Pointer { get; set; } = new Vector2(float.NegativeInfinity);

        /// <summary>Gets the tip open on the element the pointer rests on, or null if none is open.</summary>
        /// <remarks>The tip takes the workspace's theme, and closes whenever a window opens.</remarks>
        public Tip ShownTip => _tips.Shown;

        /// <summary>
        ///     Gets or sets whether the tip shown is the one for the element that has the keyboard, rather than for what
        ///     the pointer rests on, as the game's screens show tips while the gamepad is in use.
        /// </summary>
        public bool TipsFollowFocus { get; set; }

        /// <summary>
        ///     Gets the element highlighted under the pointer, which is the one a press at the pointer would reach, or
        ///     null if there is none.
        /// </summary>
        public Element PointerOver => _pointerOver;

        /// <summary>Raised when <see cref="PointerOver" /> changes, once the windows are laid out.</summary>
        public event Action PointerOverChanged;

        /// <summary>
        ///     Opens <paramref name="window" /> on top, bringing up the game's screen if nothing was open, with the world
        ///     behind it faded while it is on top where <paramref name="dimsWorld" /> is set.
        /// </summary>
        /// <remarks>A window already open stays where it is.</remarks>
        public void Open(Window window, bool dimsWorld = false)
        {
            if (_windows.IsOpen(window))
                return;

            _dims[window] = dimsWorld;
            _windows.Open(window);
        }

        /// <summary>Closes every open window, from the top down, which takes the game's screen down with the last.</summary>
        public void CloseAll()
        {
            CloseAll(CloseReason.Closed);
        }

        /// <summary>
        ///     Draws the layers on the game's screen, for this frame; called on every frame the game draws, from a session
        ///     component's <c>Draw</c>, since what is drawn lasts one frame.
        /// </summary>
        /// <remarks>Throws for a workspace that stands apart from the game.</remarks>
        public void Draw()
        {
            if (_screen == null)
                throw new InvalidOperationException("A workspace apart from the game has no screen to draw on; draw it with DrawHud.");

            _hud.Draw();
        }

        /// <summary>Draws the three layers in <paramref name="bounds" /> of <paramref name="g" />, bottom first, whatever their areas.</summary>
        /// <remarks>For drawing the layers somewhere other than the game's screen, as tests and simulations do.</remarks>
        public void DrawHud(IGraphics g, RectangleF bounds)
        {
            _hud.Draw(g, bounds, bounds);
        }

        /// <summary>
        ///     Draws the three layers on <paramref name="g" />, bottom first, each in its area: the whole of
        ///     <paramref name="screen" />, or the terminal's <paramref name="terminal" /> within it.
        /// </summary>
        /// <remarks>For drawing the layers somewhere other than the game's screen, as tests and simulations do.</remarks>
        public void DrawHud(IGraphics g, RectangleF screen, RectangleF terminal)
        {
            _hud.Draw(g, screen, terminal);
        }

        /// <summary>
        ///     Lays out and draws the windows and the tip in <paramref name="bounds" /> of <paramref name="g" />, every window
        ///     there whatever its area, after lighting what the pointer rests on.
        /// </summary>
        public void DrawScreen(IGraphics g, RectangleF bounds)
        {
            DrawScreen(g, bounds, bounds);
        }

        /// <summary>
        ///     Lays out and draws the windows, each in its area of <paramref name="screen" /> or the terminal's
        ///     <paramref name="terminal" /> within it, and the tip, after lighting what the pointer rests on.
        /// </summary>
        /// <remarks>
        ///     Called by the game's screen on every frame it draws. Runs the countdowns due first, then takes the keyboard
        ///     from an element no longer shown, giving it to the element that opened the popup it was in where there is one,
        ///     and opens, keeps or closes the tip by what the pointer rests on, or by what has the keyboard while
        ///     <see cref="TipsFollowFocus" /> is set.
        /// </remarks>
        public void DrawScreen(IGraphics g, RectangleF screen, RectangleF terminal)
        {
            _windows.Tick(g.Now);
            KeepFocusShown();
            var at = TipsFollowFocus ? FocusPoint() : Pointer;
            var owner = TipOwnerAt(at);
            _tips.Update(owner, owner == null ? null : TipFor(owner, owner.TipAt(owner.FromScreen(at))), at, g.Now);
            LightPointerOver();

            _windows.Draw(g, screen, terminal);
            _tips.Present(g, screen);
        }

        /// <summary>Brings the workspace up to date before its windows are drawn, once a frame.</summary>
        public virtual void Update()
        {
        }

        /// <summary>
        ///     Handles a press of <paramref name="button" /> at <paramref name="point" />;
        ///     <paramref name="clickCount" /> is its place in a quick succession of clicks, and
        ///     <paramref name="shift" /> and <paramref name="ctrl" /> tell whether those keys were held.
        /// </summary>
        public void PointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            _input.ReleasePointerCapture();

            // A press on the tip is the tip's alone: it reaches no window beneath it, so none is dismissed by it.
            if (!_tips.Covers(point))
            {
                _tips.Dismiss();

                if (_windows.Dismiss(point))
                    return;
            }

            var target = HitTest(point);
            if (target == null && _windows.Modal != null)
                return;

            _input.PointerDown(target, point, button, clickCount, shift, ctrl);
        }

        /// <summary>Handles the pointer moving to <paramref name="point" /> while a button is held down.</summary>
        public void PointerMove(Vector2 point)
        {
            _input.PointerMove(point);
        }

        /// <summary>Handles the release of <paramref name="button" /> at <paramref name="point" />.</summary>
        public void PointerUp(Vector2 point, PointerButton button)
        {
            _input.PointerUp(point, button);
        }

        /// <summary>
        ///     Handles the wheel turning with the pointer at <paramref name="point" />, by <paramref name="amount" />
        ///     UI units, positive down along Y and right along X.
        /// </summary>
        /// <remarks>
        ///     Offered to the innermost element under the pointer in the window there, and then outwards. An open tip has
        ///     nothing to scroll, so the wheel reaches what lies under it.
        /// </remarks>
        public void Wheel(Vector2 point, Vector2 amount)
        {
            bool stopped;
            _input.Wheel(_windows.WheelAt(point, out stopped), amount);
        }

        /// <summary>
        ///     Handles a key press, with <paramref name="shift" /> and <paramref name="ctrl" /> telling whether those
        ///     keys are held, and returns true if anything handled it.
        /// </summary>
        /// <remarks>
        ///     Offered to what holds the keyboard, or where nothing does to the window on top, then outwards, first as the key
        ///     and then as what it means. A cancel nobody took closes the window on top; with none open it answers that nothing
        ///     took the key.
        /// </remarks>
        public virtual bool Key(MyKeys key, bool shift, bool ctrl)
        {
            KeepFocusShown();
            if (_input.Key(KeysFrom(), key, shift, ctrl))
                return true;

            Intent intent;
            return _input.Intents.TryGetValue(key, out intent) && intent == Intent.Cancel && CloseTop();
        }

        /// <summary>Handles a typed character by sending it to the element that has the keyboard.</summary>
        public void Text(char c)
        {
            _input.Text(c);
        }

        /// <summary>
        ///     Gives the keyboard to the first element that can take it in the window on top, and returns true if there
        ///     was one.
        /// </summary>
        public bool FocusFirst()
        {
            return _input.FocusFirst(KeysFrom());
        }

        /// <summary>
        ///     Returns the element that a press or hover at <paramref name="point" /> reaches, or null if there is
        ///     none.
        /// </summary>
        public Element HitTest(Vector2 point)
        {
            if (_tips.Covers(point))
                return _tips.Shown.HitTest(point);

            bool stopped;
            var found = _windows.HitTest(point, out stopped);
            return _input.Allows(found) ? found : null;
        }

        /// <summary>
        ///     Closes every open window, raising <see cref="Window.Closed" /> on each for
        ///     <see cref="CloseReason.WorldUnloading" />, takes the game's screen down, and unloads the layers, raising
        ///     <see cref="Element.Unloaded" /> through everything they show.
        /// </summary>
        /// <remarks>Called from a session component's <c>UnloadData</c>, so whatever was shown can clean up after itself.</remarks>
        public void Dispose()
        {
            CloseAll(CloseReason.WorldUnloading);
            _gameScreen?.CloseNow();
            _hud.Dispose();
        }

        /// <summary>Closes the window on top, past any coach mark over it, and returns true if one was open.</summary>
        protected bool CloseTop()
        {
            var top = KeyedTop();
            if (top == null)
                return false;

            _windows.Close(top);
            return true;
        }

        /// <summary>
        ///     Returns the tip to open for <paramref name="tip" />, the tip <paramref name="owner" /> carries where the
        ///     pointer rests, or null to open none.
        /// </summary>
        /// <remarks>
        ///     Called on every frame the pointer rests on <paramref name="owner" />. A tip open on it is replaced when this
        ///     returns a different instance, so return the same instance for the same tip.
        /// </remarks>
        protected virtual Tip TipFor(Element owner, Tip tip)
        {
            return tip;
        }

        /// <inheritdoc />
        void IScreenLifetime.Opened()
        {
            _input.ReleasePointerCapture();
        }

        /// <inheritdoc />
        /// <remarks>Closes whatever windows the game's screen took with it, as when the world unloads.</remarks>
        void IScreenLifetime.Closed(bool isUnloading)
        {
            _screenUp = false;
            _input.ReleasePointerCapture();
            CloseAll(isUnloading ? CloseReason.WorldUnloading : CloseReason.Closed);
        }

        /// <summary>
        ///     Brings up the game's screen for <paramref name="window" /> if nothing was open, and gives the window the
        ///     keyboard where it can take it, or takes it from the windows beneath a modal one, so that keys go to it.
        /// </summary>
        void Opened(Window window)
        {
            _tips.Dismiss();

            if (_screen != null)
            {
                if (_gameScreen == null)
                    _gameScreen = new GameScreen(_name, this, _screen, _images, _clock);

                if (!_screenUp)
                {
                    _screenUp = true;
                    _gameScreen.Open();
                }
            }

            if (window.Focusable)
                _input.Focus(window.FocusTarget ?? window);
            else if (window.IsModal)
                _input.Focus(null);
        }

        /// <summary>Closes every open window, from the top down, telling each it closed for <paramref name="reason" />.</summary>
        void CloseAll(CloseReason reason)
        {
            while (_windows.Top != null)
                _windows.Close(_windows.Top, reason);
        }

        /// <summary>Forgets what <paramref name="window" /> asked of the world behind it.</summary>
        void Closed(Window window)
        {
            _dims.Remove(window);
        }

        /// <summary>Plays the hover sound of <paramref name="element" />, which keys have moved to.</summary>
        void Reached(Element element)
        {
            Play(ControlSound.Hover, element);
        }

        /// <summary>Plays the click for a press about to run <paramref name="element" />'s command.</summary>
        void Pressed(Element element)
        {
            Play(ControlSound.Click, element);
        }

        /// <summary>Plays the sound <paramref name="element" /> has for <paramref name="sound" />, if it has one.</summary>
        void Play(ControlSound sound, Element element)
        {
            if (!PlaysSounds || Sounds == null)
                return;

            var chosen = element.SoundFor(sound);
            if (chosen != null)
                Sounds.Play(chosen);
        }

        /// <summary>Lights what a press at the pointer would reach, and plays its hover sound where it can be pressed.</summary>
        void LightPointerOver()
        {
            var wanted = HitTest(Pointer);
            if (wanted == _pointerOver)
                return;

            if (_pointerOver != null)
                ((IInputTarget)_pointerOver).IsPointerOver = false;

            _pointerOver = wanted;

            if (_pointerOver != null)
                ((IInputTarget)_pointerOver).IsPointerOver = true;

            if (_pointerOver != null && _pointerOver.Command != null && _pointerOver.IsEnabled)
                Play(ControlSound.Hover, _pointerOver);

            PointerOverChanged?.Invoke();
        }

        /// <summary>Returns the element a key is offered to first: the one that has the keyboard, or else the window on top.</summary>
        Element KeysFrom()
        {
            return _input.Focused ?? KeyedTop();
        }

        /// <summary>
        ///     Takes the keyboard from an element no longer shown, giving it to the element that opened the popup it was in
        ///     where there is one.
        /// </summary>
        void KeepFocusShown()
        {
            var focused = _input.Focused;
            if (focused != null && !IsShown(focused))
                _input.Focus(OpenerOf(focused));
        }

        /// <summary>Returns the middle of the element that has the keyboard, or a point outside every room if none has.</summary>
        Vector2 FocusPoint()
        {
            var focused = _input.Focused;
            if (focused == null)
                return new Vector2(float.NegativeInfinity);

            var bounds = focused.Bounds;
            return focused.ToScreen(new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f));
        }

        /// <summary>
        ///     Returns true if <paramref name="element" /> and everything holding it are visible, in the tip or a window
        ///     that is open.
        /// </summary>
        bool IsShown(Element element)
        {
            var top = element;
            for (var at = element; at != null; at = at.Parent)
            {
                if (!at.IsVisible)
                    return false;

                top = at;
            }

            return top == _tips.Shown || (top is Window && _windows.IsOpen((Window)top));
        }

        /// <summary>
        ///     Returns the element that opened the popup <paramref name="element" /> is in, where that is still shown and can
        ///     hold the keyboard, or the window it is in where that window holds the keyboard for what is in it, as a menu
        ///     does for its items, or null.
        /// </summary>
        Element OpenerOf(Element element)
        {
            var anchor = (TopOf(element) as Popup)?.Anchor;
            if (anchor == null || !anchor.IsEnabled || !IsShown(anchor))
                return null;

            var window = TopOf(anchor);
            if (window.Focusable)
                return window;

            return anchor.Focusable || anchor.Command != null ? anchor : null;
        }

        /// <summary>Returns the element at the top of the tree <paramref name="element" /> is in.</summary>
        static Element TopOf(Element element)
        {
            var top = element;
            while (top.Parent != null)
                top = top.Parent;

            return top;
        }

        /// <summary>
        ///     Returns the element under <paramref name="point" /> that has a tip to show, or null if there is none.
        /// </summary>
        Element TipOwnerAt(Vector2 point)
        {
            if (_tips.Covers(point))
                return null;

            bool stopped;
            var found = _windows.TipOwnerAt(point, out stopped);
            return _input.Allows(found) ? found : null;
        }
    }
}
