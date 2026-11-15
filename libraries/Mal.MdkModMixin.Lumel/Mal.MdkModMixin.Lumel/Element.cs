using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Windows;
using VRage.Input;
using VRageMath;

namespace Lumel
{
    /// <summary>Anything that occupies a rectangle and draws in it.</summary>
    public abstract partial class Element : IInputTarget, ILayered, IShown
    {
        /// <summary>Space along an axis that stands for no limit, as a scroll gives its content.</summary>
        public const float Unbounded = 1000000f;

        Action<Element, IPlacement> _admit;
        Align _align;
        bool _arrangeDirty = true;
        RectangleF _arrangedTo;
        ICommand _command;
        Action _commandChanged;
        Collection _contents;
        bool _disabled;
        bool _focused;
        bool _focusShown;
        bool _hidden;
        bool _pointerOver;
        Action<Element, int> _added;
        bool _measureDirty = true;
        Vector2 _measuredFor;
        Element _parent;
        WindowLayer _layer;
        bool _shown;
        bool _loaded;
        ResourceDictionary _resources;
        IPlacement _placement;
        Action _placementChanged;
        Action<Element, int> _removed;
        Action<Element> _placementOfChildChanged;
        bool _creatingChildren;
        RenderTransform _transform;

        /// <summary>
        ///     The parent element, set when this is added to another element's children; null while it has none.
        /// </summary>
        public Element Parent => _parent;

        /// <summary>
        ///     The layer where this opens windows: the layer of the window it is in, or the layer over the screen it is
        ///     on.
        /// </summary>
        /// <remarks>Null while this is loose, or in a window that is closed.</remarks>
        public WindowLayer Windows
        {
            get
            {
                for (var at = this; at != null; at = at._parent)
                {
                    if (at._layer != null)
                        return at._layer;
                }

                return null;
            }
        }

        /// <summary>Whether this is part of something shown: an open window, a workspace's layer or a text panel.</summary>
        public bool IsLoaded => _loaded;

        /// <summary>Gets or sets the resources this puts where it and the elements beneath it can find them.</summary>
        /// <remarks>Made empty the first time it is read.</remarks>
        public ResourceDictionary Resources
        {
            get { return _resources ?? (_resources = new ResourceDictionary()); }
            set { _resources = value; }
        }

        /// <summary>Returns the nearest resource of type <typeparamref name="T" />, kept under its type's full name.</summary>
        /// <remarks>Throws where there is none; see <see cref="FindResource{T}(string)" /> for where it looks.</remarks>
        public T FindResource<T>() where T : class
        {
            return FindResource<T>(ResourceDictionary.KeyOf<T>());
        }

        /// <summary>Returns the nearest resource under <paramref name="key" />, which must be a <typeparamref name="T" />.</summary>
        /// <remarks>
        ///     Looks in this element's resources, then its parents', a popup's anchor's, an open tip's element's, and last
        ///     those of the workspace or text panel showing it. Throws where there is none, or where the one found is not a <typeparamref name="T" />.
        /// </remarks>
        public T FindResource<T>(string key) where T : class
        {
            var found = TryFindResource<T>(key);
            if (found == null)
                throw new InvalidOperationException("No resource " + key + " is set above " + GetType().Name + ".");

            return found;
        }

        /// <summary>Returns the nearest resource of type <typeparamref name="T" />, kept under its type's full name, or null if there is none.</summary>
        public T TryFindResource<T>() where T : class
        {
            return TryFindResource<T>(ResourceDictionary.KeyOf<T>());
        }

        /// <summary>
        ///     Returns the nearest resource under <paramref name="key" />, looking where <see cref="FindResource{T}(string)" />
        ///     does, or null if there is none.
        /// </summary>
        /// <remarks>Throws where the one found is not a <typeparamref name="T" />.</remarks>
        public T TryFindResource<T>(string key) where T : class
        {
            for (var at = this; at != null;)
            {
                object value;
                if (at._resources != null && at._resources.TryGet(key, out value))
                    return Resource<T>(key, value);

                if (at._parent != null)
                {
                    at = at._parent;
                    continue;
                }

                var anchor = (at as Popup)?.Anchor;
                if (anchor != null)
                {
                    at = anchor;
                    continue;
                }

                var owner = (at as Tip)?.Owner;
                if (owner != null)
                {
                    at = owner;
                    continue;
                }

                var hosted = at._layer?.Resources;
                return hosted != null && hosted.TryGet(key, out value) ? Resource<T>(key, value) : null;
            }

            return null;
        }

        /// <summary>Returns <paramref name="value" />, found under <paramref name="key" />, as a <typeparamref name="T" />, or throws where it is not one.</summary>
        static T Resource<T>(string key, object value) where T : class
        {
            var resource = value as T;
            if (resource == null)
                throw new InvalidOperationException("The resource " + key + " is a " + value.GetType().Name + ", not a " + typeof(T).Name + ".");

            return resource;
        }

        /// <summary>Raised when this becomes part of something shown, after everything holding it has.</summary>
        public event Action Loaded;

        /// <summary>Raised when this stops being part of something shown, after everything it holds has.</summary>
        /// <remarks>Raised for every element loaded when its workspace or text panel is disposed, so it can clean up.</remarks>
        public event Action Unloaded;

        /// <summary>Called when this becomes part of something shown, before <see cref="Loaded" /> is raised.</summary>
        protected virtual void OnLoaded()
        {
        }

        /// <summary>Called when this stops being part of something shown, before <see cref="Unloaded" /> is raised.</summary>
        protected virtual void OnUnloaded()
        {
        }

        /// <inheritdoc />
        bool IShown.IsShown
        {
            set
            {
                _shown = value;
                Reload();
            }
        }

        /// <summary>
        ///     Brings whether this is loaded up to date with whatever holds it, or with whether it is shown where nothing
        ///     does, raising the change through everything inside it.
        /// </summary>
        void Reload()
        {
            var loaded = _parent != null ? _parent._loaded : _shown;
            if (loaded == _loaded)
                return;

            _loaded = loaded;
            if (loaded)
            {
                OnLoaded();
                Loaded?.Invoke();
            }

            for (var i = 0; i < VisualChildCount; i++)
                GetVisualChild(i).Reload();

            if (loaded)
                return;

            OnUnloaded();
            Unloaded?.Invoke();
        }

        /// <summary>
        ///     The layer this is the top of, as a screen it covers or a window open in it; null for anything else.
        /// </summary>
        public WindowLayer Layer => _layer;

        WindowLayer ILayered.Layer
        {
            set { _layer = value; }
        }

        /// <summary>
        ///     Gets or sets where this sits in the space its container gives it; <c>Default</c> leaves that to the
        ///     container.
        /// </summary>
        public Align Align
        {
            get { return _align; }
            set
            {
                if (_align == value)
                    return;

                _align = value;
                InvalidateArrange();
            }
        }

        /// <summary>
        ///     Gets or sets how the container is asked to place this, or null for the container's own default.
        /// </summary>
        /// <remarks>Throws, leaving the placement as it was, when the holder cannot place this that way.</remarks>
        public IPlacement Placement
        {
            get { return _placement; }
            set
            {
                if (_placement == value)
                    return;

                if (_parent != null && _parent._admit != null)
                    _parent._admit(this, value);

                if (_placementChanged == null)
                    _placementChanged = PlacementChanged;

                if (_placement != null)
                    _placement.Changed -= _placementChanged;

                _placement = value;

                if (_placement != null)
                    _placement.Changed += _placementChanged;

                PlacementChanged();
            }
        }

        /// <summary>Whether the pointer is over this right now.</summary>
        public bool IsPointerOver => _pointerOver;

        /// <inheritdoc />
        bool IInputTarget.IsPointerOver
        {
            set
            {
                if (_pointerOver == value)
                    return;

                _pointerOver = value;
                Reapply();
                OnPointerOverChanged();
            }
        }

        /// <summary>
        ///     Gets or sets whether this can be used now: it is not switched off and its command, if it has one, can
        ///     run.
        /// </summary>
        public bool IsEnabled
        {
            get { return !_disabled && (_command == null || _command.CanExecute); }
            set
            {
                if (_disabled == !value)
                    return;

                _disabled = !value;
                Reapply();
            }
        }

        /// <summary>The number of presses in quick succession that the last press on this was part of.</summary>
        public int ClickCount { get; private set; }

        /// <summary>Gets or sets what using this does, or null for nothing.</summary>
        /// <remarks>Carried out when a press begun on this is released on it, while it is enabled.</remarks>
        public ICommand Command
        {
            get { return _command; }
            set
            {
                if (_command == value)
                    return;

                if (_commandChanged == null)
                    _commandChanged = Reapply;

                if (_command != null)
                    _command.CanExecuteChanged -= _commandChanged;

                _command = value;

                if (_command != null)
                    _command.CanExecuteChanged += _commandChanged;

                Reapply();
            }
        }

        /// <summary>
        ///     Gets or sets whether this can hold keyboard focus without a command, showing it even when a press gave it, as
        ///     a box that takes typing does; an element with a command can always hold it.
        /// </summary>
        public bool Focusable { get; set; }

        /// <summary>
        ///     Gets or sets the element given keyboard focus when this is pressed, or null where this takes it itself.
        /// </summary>
        /// <remarks>
        ///     An element that hands its focus to another is passed over by Tab, and a press on it leaves nothing focused while
        ///     the target is disabled.
        /// </remarks>
        public Element FocusTarget { get; set; }

        /// <summary>Whether this holds keyboard focus right now.</summary>
        public bool IsFocused => _focused;

        /// <summary>Whether this holds keyboard focus and shows it, as <see cref="Styling.State.FocusVisible" /> says.</summary>
        public bool IsFocusVisible => _focused && _focusShown;

        /// <summary>Gets or sets the tip shown while the pointer rests on this, or null for none.</summary>
        public Tip Tip { get; set; }

        /// <summary>
        ///     Gets or sets which of presses, hover and keyboard input pass through this to whatever is beneath it.
        /// </summary>
        public InputTransparency InputTransparency { get; set; }

        /// <summary>
        ///     Gets or sets the name a test or script finds this by in preference to any text it shows, or null to be
        ///     found by that text.
        /// </summary>
        public string AutomationId { get; set; }

        /// <summary>
        ///     Gets or sets how this is drawn and hit-tested away from where layout put it, which layout never sees.
        /// </summary>
        public RenderTransform RenderTransform
        {
            get { return _transform; }
            set { _transform = value; }
        }

        /// <summary>
        ///     Gets or sets whether this is shown, which also decides whether it takes part in layout at all.
        /// </summary>
        public bool IsVisible
        {
            get { return !_hidden; }
            set
            {
                if (_hidden == !value)
                    return;

                _hidden = !value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     The space kept clear inside <see cref="Bounds" /> within which content is measured and placed: the
        ///     padding of an element that has one.
        /// </summary>
        protected virtual Thickness Inset => default(Thickness);

        /// <summary>Whether what this draws inside itself is clipped to its bounds.</summary>
        protected virtual bool Clips => false;

        /// <summary>The size the last measure asked for, padding included.</summary>
        public Vector2 DesiredSize { get; private set; }

        /// <summary>The bounds the last arrange gave this.</summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>What is left of <see cref="Bounds" /> after the padding is removed.</summary>
        public RectangleF ContentBounds { get; private set; }

        /// <summary>Whether anything inside this can be reached at all.</summary>
        protected virtual bool Passes => true;

        /// <summary>Whether this handles the pointer itself, and so receives it even with no command or tip.</summary>
        protected virtual bool TakesPointer => false;

        /// <summary>
        ///     Whether the pointer and keyboard reach this through an <see cref="InputRoot" />: it has a command, can
        ///     hold keyboard focus, or handles the pointer itself.
        /// </summary>
        public bool TakesInput => _command != null || Focusable || TakesPointer;

        /// <summary>
        ///     The number of children this has, which <see cref="GetVisualChild" /> returns in drawing order.
        /// </summary>
        public virtual int VisualChildCount => _contents?.Count ?? 0;

        /// <summary>Marks this, and every element above it, as needing to be measured again.</summary>
        /// <remarks>Called by a change to how big this wants to be; a change to where it sits calls <see cref="InvalidateArrange" />.</remarks>
        public void InvalidateMeasure()
        {
            _arrangeDirty = true;

            if (_measureDirty)
                return;

            _measureDirty = true;

            Parent?.InvalidateMeasure();
        }

        /// <summary>Whether this has been measured since anything changed how big it wants to be.</summary>
        public bool IsMeasureValid => !_measureDirty;

        /// <summary>Marks this as needing to be arranged again, without its size having changed.</summary>
        public void InvalidateArrange()
        {
            if (_arrangeDirty)
                return;

            _arrangeDirty = true;

            Parent?.InvalidateArrange();
        }

        /// <summary>Measures how much space this wants, given the <paramref name="available" /> space.</summary>
        public void Measure(IGraphics g, Vector2 available)
        {
            // An element first works out its look here, while it is still dirty, so what the look changes asks nothing
            // more of the elements above it.
            if (!_applied)
                ApplyLook();

            // Safe to stop without asking the children: a child that changed has already dirtied every
            // parent between it and the root.
            if (!_measureDirty && _measuredFor.X == available.X && _measuredFor.Y == available.Y)
                return;

            _measureDirty = false;
            _measuredFor = available;

            if (_hidden)
            {
                DesiredSize = Vector2.Zero;
                return;
            }

            var inset = Inset;
            var inner = new Vector2(
                available.X - inset.Horizontal,
                available.Y - inset.Vertical);

            if (inner.X < 0f)
                inner.X = 0f;
            if (inner.Y < 0f)
                inner.Y = 0f;

            var content = MeasureContent(g, inner);
            DesiredSize = new Vector2(content.X + inset.Horizontal, content.Y + inset.Vertical);
        }

        /// <summary>
        ///     Tells this the <paramref name="bounds" /> it was given, which may be smaller than it asked for.
        /// </summary>
        public void Arrange(RectangleF bounds)
        {
            if (!_arrangeDirty
                && _arrangedTo.X == bounds.X && _arrangedTo.Y == bounds.Y
                && _arrangedTo.Width == bounds.Width && _arrangedTo.Height == bounds.Height)
                return;

            _arrangeDirty = false;
            _arrangedTo = bounds;
            Bounds = bounds;

            var inset = Inset;
            var width = bounds.Width - inset.Horizontal;
            var height = bounds.Height - inset.Vertical;

            ContentBounds = new RectangleF(
                bounds.X + inset.Left,
                bounds.Y + inset.Top,
                width > 0f ? width : 0f,
                height > 0f ? height : 0f);

            ArrangeContent(ContentBounds);
        }

        /// <summary>
        ///     Draws the background, then the content, with the clip applied if one was requested, all under the render
        ///     transform.
        /// </summary>
        public void Draw(IGraphics g)
        {
            if (_hidden)
                return;

            if (_transform.IsIdentity)
            {
                DrawBox(g);
                return;
            }

            g.PushTransform(_transform, new Vector2(Bounds.X, Bounds.Y));
            DrawBox(g);
            g.PopTransform();
        }

        /// <summary>Converts <paramref name="point" /> as drawn to the layout this was placed in.</summary>
        public Vector2 Local(Vector2 point)
        {
            return _transform.IsIdentity ? point : _transform.Inverse(point, new Vector2(Bounds.X, Bounds.Y));
        }

        /// <summary>
        ///     Converts <paramref name="point" /> as drawn on the screen to this element's own layout, through the
        ///     render transform of every element above it and its own.
        /// </summary>
        public Vector2 FromScreen(Vector2 point)
        {
            return Local(_parent?.FromScreen(point) ?? point);
        }

        /// <summary>
        ///     Converts <paramref name="point" /> in this element's own layout to where it is drawn on the screen: the
        ///     reverse of <see cref="FromScreen" />.
        /// </summary>
        public Vector2 ToScreen(Vector2 point)
        {
            var drawn = _transform.IsIdentity ? point : _transform.Apply(point, new Vector2(Bounds.X, Bounds.Y));
            return _parent?.ToScreen(drawn) ?? drawn;
        }

        /// <summary>
        ///     Returns where the grid the <see cref="Pattern" /> is laid on starts: the parent's top-left corner, or
        ///     this element's own if it has no parent.
        /// </summary>
        Vector2 PatternOrigin()
        {
            var from = _parent != null ? _parent.Bounds : Bounds;
            return new Vector2(from.X, from.Y);
        }

        void DrawBox(IGraphics g)
        {
            if (Background.A > 0)
                g.Fill(Bounds, Background);

            if (Pattern != null)
                Pattern.Draw(g, Bounds, PatternOrigin());

            if (Border.A > 0 && BorderThickness > 0f)
                Outline(g, Bounds, Border, BorderThickness);

            if (!Clips)
            {
                DrawContent(g);
                return;
            }

            g.PushClip(Bounds);
            DrawContent(g);
            g.PopClip();
        }

        /// <summary>
        ///     Returns the element that a press or hover at <paramref name="point" /> reaches, or null if none.
        /// </summary>
        public virtual Element HitTest(Vector2 point)
        {
            if (_hidden)
                return null;

            point = Local(point);

            if (Clips && !Holds(point))
                return null;

            if ((InputTransparency & InputTransparency.Children) == 0)
            {
                var inner = HitTestContent(point);
                if (inner != null)
                    return inner;
            }

            return (InputTransparency & InputTransparency.Self) == 0 && TakesInput && Hits(point) ? this : null;
        }

        /// <summary>Returns the deepest element at <paramref name="point" /> that has a tip to show, or null.</summary>
        /// <remarks>Finds tips beneath input transparency too.</remarks>
        public Element TipOwnerAt(Vector2 point)
        {
            if (_hidden)
                return null;

            point = Local(point);
            if (!Holds(point))
                return null;

            for (var i = VisualChildCount - 1; i >= 0; i--)
            {
                var found = GetVisualChild(i).TipOwnerAt(point);
                if (found != null)
                    return found;
            }

            return TipAt(point) != null ? this : null;
        }

        /// <summary>
        ///     Returns the element a wheel turn at <paramref name="point" /> is offered to first: the innermost element
        ///     that holds the point, or null.
        /// </summary>
        public Element WheelAt(Vector2 point)
        {
            if (_hidden)
                return null;

            point = Local(point);
            if (!Holds(point))
                return null;

            for (var i = VisualChildCount - 1; i >= 0; i--)
            {
                var found = GetVisualChild(i).WheelAt(point);
                if (found != null)
                    return found;
            }

            return this;
        }

        /// <summary>
        ///     Returns the tip shown while the pointer rests on this at <paramref name="point" />: its
        ///     <see cref="Tip" />, unless it shows different tips in different places.
        /// </summary>
        /// <remarks>Asked again and again while the pointer rests on this, so an override hands back a tip made once and kept.</remarks>
        public virtual Tip TipAt(Vector2 point)
        {
            return Tip;
        }

        /// <summary>Returns true if <paramref name="point" /> is inside <see cref="Bounds" />.</summary>
        public bool Holds(Vector2 point)
        {
            return point.X >= Bounds.X && point.X <= Bounds.X + Bounds.Width
                                       && point.Y >= Bounds.Y && point.Y <= Bounds.Y + Bounds.Height;
        }

        /// <summary>
        ///     Returns true if a press or hover at <paramref name="point" />, that no child took, reaches this:
        ///     anywhere it holds, unless overridden.
        /// </summary>
        protected virtual bool Hits(Vector2 point)
        {
            return Holds(point);
        }

        /// <summary>
        ///     Returns true if this is visible at <paramref name="point" />: it holds the point, and nothing above it
        ///     has collapsed it, clipped it, or not finished bringing it into view.
        /// </summary>
        /// <remarks>The caller settles first whether a sibling drawn over this, or a popup outside the layout, covers the point.</remarks>
        public bool Reaches(Vector2 point)
        {
            if (_hidden)
                return false;

            // The point as each element above this sees it, from the outermost in, since each one's render
            // transform moves everything inside it.
            var depth = 0;
            for (var above = Parent; above != null; above = above.Parent)
                depth++;

            for (var level = depth; level > 0; level--)
            {
                var above = Parent;
                for (var i = 1; i < level; i++)
                    above = above.Parent;

                if (!above.IsVisible || !above.Passes)
                    return false;

                point = above.Local(point);
                if (above.Clips && !above.Holds(point))
                    return false;
            }

            return Holds(Local(point));
        }

        /// <summary>Draws a hairline just inside <paramref name="bounds" />, as four thin rectangles.</summary>
        protected static void Outline(IGraphics g, RectangleF bounds, Color color, float thickness)
        {
            g.Fill(new RectangleF(bounds.X, bounds.Y, bounds.Width, thickness), color);
            g.Fill(new RectangleF(
                bounds.X, bounds.Y + bounds.Height - thickness, bounds.Width, thickness), color);
            g.Fill(new RectangleF(bounds.X, bounds.Y, thickness, bounds.Height), color);
            g.Fill(new RectangleF(
                bounds.X + bounds.Width - thickness, bounds.Y, thickness, bounds.Height), color);
        }

        /// <summary>Returns how much space this wants.</summary>
        /// <remarks>Must not change anything a later pass reads.</remarks>
        protected abstract Vector2 MeasureContent(IGraphics g, Vector2 available);

        /// <summary>Places whatever is inside this.</summary>
        protected virtual void ArrangeContent(RectangleF bounds)
        {
        }

        /// <summary>Draws whatever is inside this, after the background has been painted and any clip pushed.</summary>
        protected abstract void DrawContent(IGraphics g);

        /// <summary>
        ///     Returns the element inside this that handles a press at <paramref name="point" />, or null.
        /// </summary>
        /// <remarks>
        ///     An override asks in the reverse of the order <see cref="DrawContent" /> draws in, so the thing
        ///     on top is the thing that answers.
        /// </remarks>
        protected virtual Element HitTestContent(Vector2 point)
        {
            return null;
        }

        /// <summary>
        ///     Returns the last of <paramref name="children" /> that handles <paramref name="point" />, which is the
        ///     one drawn over the rest.
        /// </summary>
        /// <remarks>Right only for a container that draws its children in the order they were added.</remarks>
        protected static Element OnTop(Collection children, Vector2 point)
        {
            for (var i = children.Count - 1; i >= 0; i--)
            {
                var found = children[i].HitTest(point);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        ///     Called when a press of <paramref name="button" /> has begun on this at <paramref name="point" />, as the
        ///     <paramref name="clickCount" />th in quick succession; <paramref name="shift" /> and
        ///     <paramref name="ctrl" /> say whether those keys were held.
        /// </summary>
        /// <remarks>
        ///     The pointer's moves and its release come to this until it is released, wherever they are. Every point a
        ///     pointer handler is given is in this element's own layout, as <see cref="FromScreen" /> finds it.
        /// </remarks>
        protected virtual void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
        }

        /// <summary>
        ///     Called when the pointer has moved to <paramref name="point" /> during a press of
        ///     <paramref name="button" /> that began on this.
        /// </summary>
        protected virtual void OnPointerMove(Vector2 point, PointerButton button)
        {
        }

        /// <summary>
        ///     Called when a press of <paramref name="button" /> that began on this is released at
        ///     <paramref name="point" />.
        /// </summary>
        protected virtual void OnPointerUp(Vector2 point, PointerButton button)
        {
        }

        /// <summary>
        ///     Called when a press that began on this ends without its release reaching it, as when another press
        ///     begins or the screen opens or closes.
        /// </summary>
        /// <remarks>An override ends what the press was doing without carrying out what a release would.</remarks>
        protected virtual void OnPointerCaptureLost()
        {
        }

        /// <summary>Called when the pointer comes onto this or leaves it, after <see cref="IsPointerOver" /> has changed.</summary>
        protected virtual void OnPointerOverChanged()
        {
        }

        /// <summary>Called as a press, by pointer or key, is about to run this element's command.</summary>
        protected virtual void OnPressed()
        {
        }

        /// <summary>
        ///     Offers <paramref name="key" /> to this while it has keyboard focus, and returns true if it handled it.
        /// </summary>
        protected virtual bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            return false;
        }

        /// <summary>
        ///     Offers a wheel turn of <paramref name="amount" /> UI units (positive is down), and returns true if this
        ///     handled it.
        /// </summary>
        /// <remarks>Offered to the innermost element under the pointer, then outwards.</remarks>
        protected virtual bool OnWheel(float amount)
        {
            return false;
        }

        /// <summary>
        ///     Offers <paramref name="intent" />, what a key that nobody handled means, and returns true if this
        ///     handled it.
        /// </summary>
        /// <remarks>Offered as keys are: to what holds the keyboard, or where nothing does to what the host starts from, then outwards.</remarks>
        protected virtual bool OnIntent(InputRoot input, Intent intent)
        {
            return false;
        }

        /// <summary>Called with a typed character <paramref name="c" /> while this has keyboard focus.</summary>
        protected virtual void OnTextInput(char c)
        {
        }

        /// <inheritdoc />
        void IInputTarget.PointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            ClickCount = clickCount;
            OnPointerDown(point, button, clickCount, shift, ctrl);
        }

        /// <inheritdoc />
        void IInputTarget.PointerMove(Vector2 point, PointerButton button)
        {
            OnPointerMove(point, button);
        }

        /// <inheritdoc />
        void IInputTarget.PointerUp(Vector2 point, PointerButton button)
        {
            OnPointerUp(point, button);
        }

        /// <inheritdoc />
        void IInputTarget.PointerCaptureLost()
        {
            OnPointerCaptureLost();
        }

        /// <inheritdoc />
        void IInputTarget.Pressed()
        {
            OnPressed();
        }

        /// <inheritdoc />
        bool IInputTarget.Key(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            return OnKeyDown(input, key, shift, ctrl);
        }

        /// <inheritdoc />
        bool IInputTarget.Wheel(float amount)
        {
            return OnWheel(amount);
        }

        /// <inheritdoc />
        bool IInputTarget.Intent(InputRoot input, Intent intent)
        {
            return OnIntent(input, intent);
        }

        /// <inheritdoc />
        void IInputTarget.Text(char c)
        {
            OnTextInput(c);
        }

        /// <inheritdoc />
        void IInputTarget.Focus(bool byPress)
        {
            if (_focused)
                return;

            _focused = true;
            Reapply();
            OnGotFocus(byPress);
        }

        /// <summary>
        ///     Called when this has just taken keyboard focus, where <paramref name="byPress" /> says whether a pointer
        ///     press gave it.
        /// </summary>
        /// <remarks>A press that gives focus reaches the element after this call.</remarks>
        protected virtual void OnGotFocus(bool byPress)
        {
        }

        /// <inheritdoc />
        void IInputTarget.Blur()
        {
            if (!_focused)
                return;

            _focused = false;
            _focusShown = false;
            Reapply();
            OnLostFocus();
        }

        /// <inheritdoc />
        bool IInputTarget.ShowsFocus
        {
            set
            {
                if (_focusShown == value)
                    return;

                _focusShown = value;
                Reapply();
            }
        }

        /// <summary>Called when this has just lost keyboard focus.</summary>
        protected virtual void OnLostFocus()
        {
        }

        /// <summary>Returns the child at <paramref name="index" />, counted in drawing order.</summary>
        public virtual Element GetVisualChild(int index)
        {
            return _contents[index];
        }

        /// <summary>Creates the collection this holds its children in, which for a control is its own parts.</summary>
        /// <remarks>Throws when this already holds one: an element makes its children once.</remarks>
        protected Collection CreateChildren()
        {
            return CreateChildren(null, null, null, null);
        }

        /// <summary>
        ///     Creates the collection this holds its children in, calling <paramref name="admit" /> to check whether
        ///     this can place each child, and <paramref name="added" />, <paramref name="removed" /> and
        ///     <paramref name="placementChanged" /> on every change; any of them may be null.
        /// </summary>
        /// <remarks>Throws when this already holds one: an element makes its children once.</remarks>
        protected Collection CreateChildren(
            Action<Element, IPlacement> admit,
            Action<Element, int> added,
            Action<Element, int> removed,
            Action<Element> placementChanged)
        {
            _creatingChildren = true;
            Collection children;
            try
            {
                children = new OwnedCollection(this);
            }
            finally
            {
                _creatingChildren = false;
            }

            _admit = admit;
            _added = added;
            _removed = removed;
            _placementOfChildChanged = placementChanged;
            return children;
        }

        void PlacementChanged()
        {
            InvalidateMeasure();

            if (_parent == null)
                return;

            if (_parent._placementOfChildChanged != null)
                _parent._placementOfChildChanged(this);

            _parent.InvalidateMeasure();
        }
    }
}