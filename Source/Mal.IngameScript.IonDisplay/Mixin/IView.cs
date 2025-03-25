using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    public interface IView
    {
        IView Parent { get; set; }
        IIon Context { get; }
        T Get<T>(string name);
        void Set<T>(string name, T value, bool valueIsDefault = false);
        void BeginFrame(IIon ion);
        void Draw(DC dc);
    }

    public struct DC
    {
        public readonly Action<MySprite> Add;
        public readonly RectangleF Bounds;

        public DC(Action<MySprite> add, RectangleF bounds)
        {
            Add = add;
            Bounds = bounds;
        }

        public DC WithBounds(RectangleF bounds) => new DC(Add, bounds);
        public DC WithAdd(Action<MySprite> add) => new DC(add, Bounds);
    }
}