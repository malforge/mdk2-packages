using System.Collections.Generic;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>A stack of clip rectangles, each already intersected with the one below it.</summary>
    public sealed class ClipStack
    {
        readonly List<RectangleF> _stack = new List<RectangleF>();

        /// <summary>Whether anything is clipping right now.</summary>
        public bool HasClip => _stack.Count > 0;

        /// <summary>The rectangle currently in force, already intersected with every rectangle below it.</summary>
        /// <remarks>Only meaningful while <see cref="HasClip" /> is true.</remarks>
        public RectangleF Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : new RectangleF(0f, 0f, 0f, 0f);

        /// <summary>Pushes <paramref name="bounds" />, narrowed by the clip already in force.</summary>
        public void Push(RectangleF bounds)
        {
            _stack.Add(_stack.Count > 0 ? Intersect(bounds, _stack[_stack.Count - 1]) : bounds);
        }

        /// <summary>Removes the innermost rectangle.</summary>
        public void Pop()
        {
            if (_stack.Count > 0)
                _stack.RemoveAt(_stack.Count - 1);
        }

        /// <summary>Removes every rectangle, at the start of a frame.</summary>
        public void Clear()
        {
            _stack.Clear();
        }

        /// <summary>Returns the overlap of two rectangles, or an empty rectangle if they do not meet.</summary>
        static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            var left = a.X > b.X ? a.X : b.X;
            var top = a.Y > b.Y ? a.Y : b.Y;

            var aRight = a.X + a.Width;
            var bRight = b.X + b.Width;
            var aBottom = a.Y + a.Height;
            var bBottom = b.Y + b.Height;

            var right = aRight < bRight ? aRight : bRight;
            var bottom = aBottom < bBottom ? aBottom : bBottom;

            var width = right - left;
            var height = bottom - top;

            return new RectangleF(left, top, width > 0f ? width : 0f, height > 0f ? height : 0f);
        }
    }
}