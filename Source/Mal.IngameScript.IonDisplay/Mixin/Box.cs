using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    public class Box : View
    {
        float _rotationRad;

        public Box()
        {
            Set("Color", Color.White, true);
            Set("PatternId", "SquareSimple", true);
        }

        protected override void OnPropertyChanged(IProperty property)
        {
            base.OnPropertyChanged(property);
            switch (property.Name)
            {
                case "Rotation":
                    _rotationRad = MathHelper.ToRadians(((Property<float>)property).Get());
                    break;
            }
        }

        protected override void OnBeforeFrame() { }

        protected override void OnDraw(DC dc)
        {
            var size = dc.Bounds.Size;
            var mirror = Get<bool>("Mirror");
            var flip = Get<bool>("Flip");
            var color = Get<Color>("Color");
            var patternId = Get<string>("PatternId");
            if (mirror) size.X = -size.X;
            if (flip) size.Y = -size.Y;
            dc.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = patternId,
                Position = dc.Bounds.Center,
                Size = size,
                Color = color,
                RotationOrScale = _rotationRad,
                Alignment = TextAlignment.CENTER
            });
        }
    }
}