using System;
using VRageMath;

namespace IngameScript
{
    public class VStack : Stack
    {
        protected override void Advance(IView child, ref Vector2 position, Vector2 size) => position.Y += size.Y;

        protected override void MutateSizeOnMeasure(IView child, ref Vector2 size)
        {
            var bounds = child.Get<RectangleF>("Bounds");
            var margin = child.Get<Thickness>("Margin");
            var childSize = bounds.Size + margin.Size;
            size.X = Math.Max(size.X, childSize.X);
            size.Y += childSize.Y;
        }
    }
}