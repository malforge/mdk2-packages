using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>
    ///     The one tip a workspace shows at a time: which tip is open, and how long the pointer has rested on what offers
    ///     one.
    /// </summary>
    /// <remarks>
    ///     A tip opens once its element has offered it for the tip's <see cref="Tip.ShowDelay" />, and stays as long as it
    ///     says. A tip closed by a press or a window opening opens again only once the pointer has rested on something else.
    /// </remarks>
    public class TipHost
    {
        ITheme _theme;
        Tip _tip;
        Element _resting;
        Tip _restingTip;
        int _restingSince;
        Element _spent;

        /// <summary>Gets the tip that is open, or null if none is.</summary>
        public Tip Shown => _tip;

        /// <summary>Sets the theme that gives the tips a style when they have none, or null for <see cref="GameTheme" />.</summary>
        public ITheme Theme
        {
            set
            {
                _theme = value;
                if (_tip != null)
                    ((IThemed)_tip).UseTheme(value);
            }
        }

        /// <summary>
        ///     Opens, keeps or closes the tip, where <paramref name="owner" /> is the element with a tip that the pointer or
        ///     keyboard rests on at <paramref name="at" />, <paramref name="offered" /> the tip it offers there, and
        ///     <paramref name="now" /> the frame.
        /// </summary>
        /// <remarks>Called on every frame the workspace's windows are drawn.</remarks>
        public void Update(Element owner, Tip offered, Vector2 at, int now)
        {
            if (owner != _resting || offered != _restingTip)
            {
                _resting = owner;
                _restingTip = offered;
                _restingSince = now;
                if (offered != null)
                {
                    ((IThemed)offered).UseTheme(_theme);
                    offered.Restyle();
                }
            }

            if (_tip != null)
            {
                if (((ITipLifetime)_tip).StaysOpen(owner, offered, at, now))
                    return;

                var shown = _tip.Owner;
                Close(false);

                // Another tip of the same element takes the place of the one that gave way, as a link's does over its text's.
                if (owner != null && owner == shown && offered != null)
                {
                    Open(owner, offered, at);
                    return;
                }
            }

            if (owner != _spent)
                _spent = null;

            if (owner == null || offered == null || owner == _spent || now - _restingSince < offered.ShowDelay)
                return;

            Open(owner, offered, at);
        }

        /// <summary>Closes the open tip, if any, for good: its element offers it again only once the pointer has rested on something else.</summary>
        /// <remarks>For a press elsewhere, or a window opening.</remarks>
        public void Dismiss()
        {
            Close(true);
        }

        /// <summary>Returns true if the open tip holds the pointer at <paramref name="point" />.</summary>
        public bool Covers(Vector2 point)
        {
            return _tip != null && _tip.Covers(point);
        }

        /// <summary>Lays the open tip out within <paramref name="area" /> and draws it with <paramref name="g" />.</summary>
        public void Present(IGraphics g, RectangleF area)
        {
            if (_tip != null)
                ((ITipLifetime)_tip).Present(g, area);
        }

        /// <summary>Opens <paramref name="tip" /> on <paramref name="owner" />, with the pointer or keyboard at <paramref name="at" />.</summary>
        void Open(Element owner, Tip tip, Vector2 at)
        {
            _tip = tip;
            ((ITipLifetime)tip).Open(owner, at);
        }

        /// <summary>
        ///     Closes the open tip, if any; where <paramref name="spent" /> is set, its element offers it again only once the
        ///     pointer has rested on something else.
        /// </summary>
        void Close(bool spent)
        {
            if (_tip == null)
                return;

            if (spent)
                _spent = _tip.Owner;

            ((ITipLifetime)_tip).Close();
            _tip = null;
        }
    }
}
