using System;
using VRageMath;

namespace IngameScript
{
    public static class ViewExtensions
    {
        public static T CenteredAt<T>(this T view, float x, float y) where T : View
            => CenteredAt(view, new Vector2(x, y));

        public static T CenteredAt<T>(this T view, Vector2 position) where T : View
        {
            var bounds = view.Get<RectangleF>("Bounds");
            ((IView)view).Set("Bounds", new RectangleF(position - bounds.Size / 2, bounds.Size));
            return view;
        }

        public static T At<T>(this T view, float x, float y) where T : View
            => At(view, new Vector2(x, y));

        public static T At<T>(this T view, Vector2 position) where T : View
        {
            var bounds = view.Get<RectangleF>("Bounds");
            ((IView)view).Set("Bounds", new RectangleF(position, bounds.Size));
            return view;
        }

        public static T CenterVAt<T>(this T view, float x, float y) where T : View
        {
            var bounds = view.Get<RectangleF>("Bounds");
            view.Set("Bounds", new RectangleF(new Vector2(x, y - bounds.Height / 2), bounds.Size));
            return view;
        }

        public static T CenterHAt<T>(this T view, float x, float y) where T : View
        {
            var bounds = view.Get<RectangleF>("Bounds");
            view.Set("Bounds", new RectangleF(new Vector2(x - bounds.Width / 2, y), bounds.Size));
            return view;
        }

        public static T Size<T>(this T view, float width, float height) where T : View
        {
            var bounds = view.Get<RectangleF>("Bounds");
            view.Set("Bounds", new RectangleF(bounds.Position, new Vector2(width, height)));
            return view;
        }

        public static T AutoSize<T>(this T view, float fixedWidth = -1f, float fixedHeight = -1f) where T : View
        {
            var size = view.Measure();
            if (fixedWidth >= 0) size.X = fixedWidth;
            if (fixedHeight >= 0) size.Y = fixedHeight;
            var bounds = view.Get<RectangleF>("Bounds");
            view.Set("Bounds", new RectangleF(bounds.Position, size));
            return view;
        }

        public static T RotateAround<T>(this T view, float rotation, float x, float y, bool affectPattern = true) where T : View
        {
            var line = view as Line;
            if (line != null)
                return line.RotateAround(rotation, x, y) as T;

            var bounds = view.Get<RectangleF>("Bounds");
            var position = bounds.Position;
            var center = new Vector2(x, y);
            position -= center;
            var angle = MathHelper.ToRadians(rotation);
            x = (float)(position.X * Math.Cos(angle) - position.Y * Math.Sin(angle));
            y = (float)(position.X * Math.Sin(angle) + position.Y * Math.Cos(angle));
            position = new Vector2(x, y);
            position += center;
            view.Set("Bounds", new RectangleF(position, bounds.Size));
            if (affectPattern)
            {
                var boxRotation = view.Get<float>("Rotation");
                var box = view as Box;
                box?.Set("Rotation", (boxRotation + rotation) % 360);
            }

            return view;
        }

        public static T Hidden<T>(this T view) where T : View
        {
            view.Set("IsVisible", false);
            return view;
        }

        public static T Visibility<T>(this T view, bool state) where T : View
        {
            view.Set("IsVisible", state);
            return view;
        }

        public static T Margin<T>(this T view, float left, float top, float right, float bottom) where T : View
        {
            view.Set("Margin", new Thickness(left, top, right, bottom));
            return view;
        }

        public static T Margin<T>(this T view, float uniformSize) where T : View
        {
            view.Set("Margin", new Thickness(uniformSize));
            return view;
        }

        public static T Margin<T>(this T view, float horizontal, float vertical) where T : View
        {
            view.Set("Margin", new Thickness(horizontal, vertical));
            return view;
        }

        public static T Margin<T>(this T view, Thickness margin) where T : View
        {
            view.Set("Margin", margin);
            return view;
        }

        public static T Flex<T>(this T view) where T : View
        {
            view.Set("Flex", Flexing.Width | Flexing.Height);
            return view;
        }

        public static T FlexWidth<T>(this T view) where T : View
        {
            view.Set("Flex", Flexing.Width);
            return view;
        }

        public static T FlexHeight<T>(this T view) where T : View
        {
            view.Set("Flex", Flexing.Height);
            return view;
        }
    }
}