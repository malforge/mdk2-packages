using Lumel.Drawing;
using VRageMath;

namespace Lumel
{
    /// <summary>A tip that a workspace opens, keeps, draws and closes.</summary>
    /// <remarks>Implemented explicitly by <see cref="Tip" />, so only the workspace showing it calls it.</remarks>
    public interface ITipLifetime
    {
        /// <summary>Opens the tip on <paramref name="owner" />, which the pointer rests on at <paramref name="pointer" />.</summary>
        void Open(Element owner, Vector2 pointer);

        /// <summary>Closes the tip, if it is open.</summary>
        void Close();

        /// <summary>
        ///     Returns true if the tip stays open, where <paramref name="over" /> is the element with a tip that the pointer
        ///     rests on at <paramref name="pointer" />, <paramref name="offered" /> is the tip it offers there, and
        ///     <paramref name="now" /> is the frame.
        /// </summary>
        bool StaysOpen(Element over, Tip offered, Vector2 pointer, int now);

        /// <summary>Lays the tip out within <paramref name="area" /> and draws it with <paramref name="g" />.</summary>
        void Present(IGraphics g, RectangleF area);
    }
}
