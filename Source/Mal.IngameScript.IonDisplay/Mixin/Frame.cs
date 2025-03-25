using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    public class Frame : View, IContainer
    {
        readonly List<IView> _children = new List<IView>();
        IReadOnlyList<IView> IContainer.Children => _children;
        protected List<IView> Children => _children;

        void IContainer.Add(IView view)
        {
            _children.Add(view);
            view.Parent = this;
        }

        void IContainer.AddRange(IEnumerable<IView> views)
        {
            foreach (var view in views)
            {
                _children.Add(view);
                view.Parent = this;
            }
        }

        protected override void OnBeforeFrame() => _children.Clear();

        protected override void OnDraw(DC dc)
        {
            if (_children == null)
                return;

            var childDc = OpenChildDc(dc);
            foreach (var child in _children)
                Draw(child, childDc);
            CloseChildDc(dc);
        }

        protected virtual void CloseChildDc(DC dc)
        {
            if (!Get<bool>("ClipToBounds")) return;
            var clip = Context.PopClip();
            if (clip.HasValue)
                dc.Add(new MySprite(SpriteType.CLIP_RECT, position: new Vector2(clip.Value.X, clip.Value.Y), size: new Vector2(clip.Value.Width, clip.Value.Height)));
            else
                dc.Add(new MySprite(SpriteType.CLIP_RECT));
        }

        protected virtual DC OpenChildDc(DC dc)
        {
            if (!Get<bool>("ClipToBounds")) return dc;
            var clip = Context.PushClip(dc.Bounds);
            dc.Add(new MySprite(SpriteType.CLIP_RECT,
                position: new Vector2(clip.X, clip.Y),
                size: new Vector2(clip.Width, clip.Height)
            ));
            return dc;
        }

        public override Vector2 Measure()
        {
            if (_children == null || _children.Count == 0)
                return Vector2.Zero;
            var extents = _children[0].Get<RectangleF>("Bounds");
            for (var i = 1; i < _children.Count; i++)
            {
                var child = _children[i];
                var childBounds = child.Get<RectangleF>("Bounds");
                extents = new RectangleF(
                    Math.Min(extents.X, childBounds.X),
                    Math.Min(extents.Y, childBounds.Y),
                    Math.Max(extents.Right, childBounds.Right),
                    Math.Max(extents.Bottom, childBounds.Bottom));
            }

            return extents.Size;
        }
    }
}