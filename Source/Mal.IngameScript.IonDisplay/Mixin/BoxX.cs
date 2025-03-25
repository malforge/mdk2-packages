using VRageMath;

namespace IngameScript
{
    public static class BoxX
    {
        public static Box Box(this IIon ion, Color color, string patternId = null)
        {
            var box = ion.View<Box>();
            box.Set("Color", color);
            box.Set("PatternId", patternId ?? "SquareSimple");
            return box;
        }

        public static T RotatedImg<T>(this T view, float rotation) where T : Box
        {
            view.Set("Rotation", rotation);
            return view;
        }

        public static T MirroredImg<T>(this T view) where T : Box
        {
            view.Set("Mirror", true);
            return view;
        }

        public static T FlippedImg<T>(this T view) where T : Box
        {
            view.Set("Flip", true);
            return view;
        }
    }
}