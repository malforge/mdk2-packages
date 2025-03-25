using System;
using VRageMath;

namespace IngameScript
{
    public class HStack : Stack
    {
        protected override void Advance(IView child, ref Vector2 position, Vector2 size) => position.X += size.X;

        protected override void MutateSizeOnMeasure(IView child, ref Vector2 size)
        {
            var bounds = child.Get<RectangleF>("Bounds");
            var margin = child.Get<Thickness>("Margin");
            var childSize = bounds.Size + margin.Size;
            size.X += childSize.X;
            size.Y = Math.Max(size.Y, childSize.Y);
        }
    }
}