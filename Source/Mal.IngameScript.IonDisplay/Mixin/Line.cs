using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    public class Line : View
    {
        public Line()
        {
            Set("Color", Color.White);
            Set("PatternId", "SquareSimple");;
        }

        protected override void OnPropertyChanged(IProperty property)
        {
            base.OnPropertyChanged(property);
            switch (property.Name)
            {
                case "Start":
                case "End":
                {
                    var start = Get<Vector2>("Start");
                    var end = Get<Vector2>("End");
                    var min = Vector2.Min(start, end);
                    var max = Vector2.Max(start, end);
                    var size = max - min;
                    Set("Bounds", new RectangleF(min, size));
                    break;
                }
            }
        }

        protected override void OnBeforeFrame() { }

        protected override void OnDraw(DC dc)
        {
            var start = dc.Bounds.Position + Get<Vector2>("Start");
            var end = dc.Bounds.Position + Get<Vector2>("End");

            var size = new Vector2((end - start).Length(), Get<float>("Thickness"));
            var center = (start + end) / 2;
            var rotation = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            dc.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = Get<string>("PatternId"),
                Position = center,
                Size = size,
                Color = Get<Color>("Color"),
                RotationOrScale = rotation,
                Alignment = TextAlignment.CENTER
            });
        }
    }
}