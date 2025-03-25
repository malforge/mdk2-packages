using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript
{
    public class Text : View
    {
        float _fontScale;
        string _measuredFontId;
        float _measuredFontSize;
        Vector2 _measuredSize;
        string _measuredValue;

        public Text()
        {
            Set("FontId", "White");
            Set("FontSize", 24f);
            Set("Color", Color.White);
        }

        public override Vector2 Measure()
        {
            var value = Get<string>("Value");
            if (string.IsNullOrEmpty(value))
                return Vector2.Zero;
            var fontId = Get<string>("FontId");
            var fontSize = Get<float>("FontSize");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_measuredFontSize != fontSize || _measuredFontId != fontId || _measuredValue != value)
            {
                _measuredFontSize = fontSize;
                _measuredFontId = fontId;
                _measuredValue = value;
                _measuredSize = Context.MeasureString(new StringSegment(value), fontId, fontSize, out _fontScale);
            }

            return _measuredSize;
        }

        protected override void OnBeforeFrame() { }

        protected override void OnDraw(DC dc)
        {
            Measure();
            Vector2 position;
            var alignment = Get<TextAlignment>("Alignment");
            switch (alignment)
            {
                case TextAlignment.RIGHT:
                    position = new Vector2(dc.Bounds.Right, dc.Bounds.Y);
                    break;
                case TextAlignment.CENTER:
                    position = new Vector2(dc.Bounds.Center.X, dc.Bounds.Y);
                    break;
                default:
                    position = dc.Bounds.Position;
                    break;
            }

            dc.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = Get<string>("Value"),
                Position = position,
                RotationOrScale = _fontScale,
                Color = Get<Color>("Color"),
                Alignment = Get<TextAlignment>("Alignment"),
                FontId = Get<string>("FontId"),
                Size = dc.Bounds.Size
            });
        }
    }
}