using System;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>
    ///     One element laid out across a room and drawn there, taking no input: what a workspace's HUD layers and a text
    ///     panel show.
    /// </summary>
    /// <remarks>Its content is loaded from the start, and unloaded by <see cref="Dispose" />.</remarks>
    public class Stage : IDisposable
    {
        readonly ZStack _root = new ZStack();
        Element _content;
        ITheme _theme;

        /// <summary>Creates a stage that shows nothing until it is given content.</summary>
        public Stage()
        {
            ((IShown)_root).IsShown = true;
        }

        /// <summary>
        ///     Gets or sets the element shown, placed in the room by its own <see cref="Element.Align" />, or null for
        ///     nothing.
        /// </summary>
        public Element Content
        {
            get { return _content; }
            set
            {
                if (ReferenceEquals(_content, value))
                    return;

                if (_content != null)
                    _root.Children.Remove(_content);

                _content = value;
                if (value != null)
                    _root.Children.Add(value);
            }
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
                ((IThemed)_root).UseTheme(value);
            }
        }

        /// <summary>Gets or sets the resources the content finds above everything in it.</summary>
        public ResourceDictionary Resources
        {
            get { return _root.Resources; }
            set { _root.Resources = value; }
        }

        /// <summary>Lays the content out across <paramref name="bounds" /> and draws it with <paramref name="g" />.</summary>
        public void Draw(IGraphics g, RectangleF bounds)
        {
            _root.Measure(g, new Vector2(bounds.Width, bounds.Height));
            _root.Arrange(bounds);
            _root.Draw(g);
        }

        /// <summary>Unloads the content, raising <see cref="Element.Unloaded" /> through it, so it can clean up after itself.</summary>
        public void Dispose()
        {
            ((IShown)_root).IsShown = false;
        }
    }
}
