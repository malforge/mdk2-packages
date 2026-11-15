using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using VRage.Input;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     Holds pages with a strip of their headers above them, and shows the page of the selected header.
    /// </summary>
    /// <remarks>
    /// The pages are held in one <see cref="Deck" />, so the view is as large as its largest page and choosing another tab
    /// never changes its size. Only the page shown is drawn, pressed or reached by the keyboard; a page not shown keeps
    /// everything it holds, and comes back as it was. Each tab's command is the view's, which selects
    /// it; a tab given to a view with a command of its own is refused. While the keyboard is inside the view, Ctrl+Tab presses
    /// the next tab and Ctrl+Shift+Tab the previous one, around the ends, as the game's tab control does.
    /// </remarks>
    public partial class TabView : Container
    {
        /// <summary>The space between the strip of headers and the page when no look sets it, in UI units.</summary>
        public const float NeutralSpacing = 12f;

        readonly Collection _children;
        readonly TabList _tabs;
        readonly List<RelayCommand> _presses = new List<RelayCommand>();

        // Equal plates butted edge to edge, as the game draws the tabs across the top of its terminal.
        Grid _strip;
        readonly Deck _pages = new Deck();
        int _selected;
        float _spacing = NeutralSpacing;

        /// <inheritdoc />
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (key != MyKeys.Tab || !ctrl)
                return false;

            var count = _tabs.Count;
            for (var step = 1; step < count; step++)
            {
                var at = ((_selected + (shift ? -step : step)) % count + count) % count;
                if (input.Press(_tabs[at]))
                    return true;
            }

            return true;
        }

        /// <summary>Creates a tab view with no tabs.</summary>
        public TabView()
        {
            _children = CreateChildren();
            _children.Add(_pages);
            _tabs = new OwnedTabs(AdmitTab, Inserted, Removed);
            Restrip();
        }

        /// <summary>
        ///     Gets or sets the style of this view, or null to use the default its host's theme holds for a tab view.
        /// </summary>
        public new IStyle<TabView> Style
        {
            get { return (IStyle<TabView>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>The look this view sets itself for every state, applied over its style's.</summary>
        public new TabViewLook Base => (TabViewLook)base.Base;

        /// <summary>The look this view sets itself for when the pointer is over it, applied over its style's.</summary>
        public new TabViewLook PointerOver => (TabViewLook)base.PointerOver;

        /// <summary>The look this view sets itself for when it is disabled, applied over its style's.</summary>
        public new TabViewLook Disabled => (TabViewLook)base.Disabled;

        /// <summary>
        ///     The look this view sets itself for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style's.
        /// </summary>
        public new TabViewLook this[State state] => (TabViewLook)base[state];

        /// <summary>The tabs, in the order their headers appear.</summary>
        public TabList Tabs => _tabs;

        /// <summary>The blank space between the strip of headers and the page.</summary>
        /// <remarks>Set by this view's looks.</remarks>
        public float Spacing => _spacing;

        /// <summary>Gets or sets the index of the tab whose page is shown, counted from 0.</summary>
        /// <remarks>Throws for an index with no tab.</remarks>
        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                if (value < 0 || value >= _tabs.Count)
                    throw new ArgumentException("There is no tab " + value + " of " + _tabs.Count + ".", nameof(value));

                if (value == _selected && _pages.Top != null)
                    return;

                _selected = value;
                Show();
                SelectionChanged?.Invoke(value);
            }
        }

        /// <summary>Raised when a different page is shown, with the index of the tab that is now selected.</summary>
        public event Action<int> SelectionChanged;

        /// <summary>The alignment that makes the strip and the page stretch across.</summary>
        protected override Align Aligns => Align.FillWidth;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<TabView>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TabViewLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var spacing = ((TabViewLook)look).Spacing ?? NeutralSpacing;
            if (spacing == _spacing)
                return;

            _spacing = spacing;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _strip.Measure(g, available);
            var strip = _strip.DesiredSize;
            if (!_pages.IsVisible)
                return strip;

            var left = available.Y - strip.Y - _spacing;
            _pages.Measure(g, new Vector2(available.X, left > 0f ? left : 0f));
            var page = _pages.DesiredSize;
            return new Vector2(Math.Max(strip.X, page.X), strip.Y + _spacing + page.Y);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            Place(_strip, bounds, bounds.Y);
            if (_pages.IsVisible)
                Place(_pages, bounds, bounds.Y + _strip.DesiredSize.Y + _spacing);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _strip.Draw(g);
            if (_pages.IsVisible)
                _pages.Draw(g);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return (_pages.IsVisible ? _pages.HitTest(point) : null) ?? _strip.HitTest(point);
        }

        /// <summary>Rejects a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            AdmitSized(placement);
        }

        /// <summary>
        ///     Places <paramref name="child" /> at <paramref name="y" />, as tall as it asks for or its placement says,
        ///     and across the width according to its alignment.
        /// </summary>
        void Place(Element child, RectangleF bounds, float y)
        {
            var sized = child.Placement as ISized;
            var height = Given(sized, false, child.DesiredSize.Y);
            float x;
            float width;
            Span(child, true, bounds.X, bounds.Width, sized?.Width ?? float.NaN, child.DesiredSize.X, out x, out width);
            child.Arrange(new RectangleF(x, y, width, height));
        }

        /// <summary>Rejects a tab that has its own command, because a tab's command belongs to its view.</summary>
        static void AdmitTab(Tab tab)
        {
            if (tab.Command != null)
                throw new ArgumentException("A tab's command is its view's; this tab was given one of its own.", nameof(tab));
        }

        /// <summary>
        ///     Takes <paramref name="tab" /> in at <paramref name="index" />, giving it the view's command and making
        ///     it report when its page changes.
        /// </summary>
        void Inserted(Tab tab, int index)
        {
            ((ITabbed)tab).ContentChanged = Show;
            if (_tabs.Count > 1 && index <= _selected)
                _selected++;

            Restrip();
            Show();
        }

        /// <summary>
        ///     Releases <paramref name="tab" /> from <paramref name="index" />, and shows another page if its page was
        ///     the one shown.
        /// </summary>
        void Removed(Tab tab, int index)
        {
            var wasSelected = index == _selected;
            ((ITabbed)tab).ContentChanged = null;
            ((ITabbed)tab).IsSelected = false;
            tab.Command = null;

            if (index < _selected)
                _selected--;
            else if (wasSelected && _selected >= _tabs.Count && _selected > 0)
                _selected--;

            Restrip();
            Show();
            if (wasSelected && _tabs.Count > 0)
                SelectionChanged?.Invoke(_selected);
        }

        /// <summary>
        ///     Lays the headers out again in their own strip, one equal column each, with a command on each that
        ///     selects it.
        /// </summary>
        void Restrip()
        {
            if (_strip != null)
            {
                // Emptied first, so a tab gone from the view is not left inside the strip it leaves.
                for (var i = 0; i < _strip.Columns.Count; i++)
                    _strip.Cells[0, i] = null;

                _children.Remove(_strip);
            }

            _strip = new Grid();
            _presses.Clear();
            for (var i = 0; i < _tabs.Count; i++)
            {
                var index = i;
                _presses.Add(new RelayCommand(() => SelectedIndex = index));
                _strip.Columns.Add(GridLength.Flex(1));
                _strip.Cells[0, i] = _tabs[i];
            }

            _children.Insert(0, _strip);
        }

        /// <summary>Keeps the deck holding the page of every tab, and nothing else.</summary>
        void Repage()
        {
            var cards = _pages.Children;
            for (var i = cards.Count - 1; i >= 0; i--)
            {
                if (!IsPage(cards[i]))
                    cards.Remove(cards[i]);
            }

            for (var i = 0; i < _tabs.Count; i++)
            {
                var page = _tabs[i].Content;
                if (page != null && page.Parent != _pages)
                    cards.Add(page);
            }
        }

        /// <summary>Returns true if <paramref name="element" /> is the page of one of the tabs.</summary>
        bool IsPage(Element element)
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                if (ReferenceEquals(_tabs[i].Content, element))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Marks the selected tab, whose header does nothing when pressed, and replaces the page in the view with
        ///     its page.
        /// </summary>
        void Show()
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                ((ITabbed)_tabs[i]).IsSelected = i == _selected;
                _tabs[i].Command = i == _selected ? null : _presses[i];
            }

            Repage();
            var page = _selected < _tabs.Count ? _tabs[_selected].Content : null;
            _pages.Top = page;
            _pages.IsVisible = page != null;
        }
    }
}
