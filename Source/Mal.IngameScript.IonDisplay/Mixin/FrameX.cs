using System.Collections.Generic;

namespace IngameScript
{
    public static class FrameX
    {
        public static Frame Frame(this IIon ion) => ion.View<Frame>();

        public static T Add<T>(this T view, params View[] children) where T : Frame
        {
            ((IContainer)view).AddRange(children);
            return view;
        }

        public static T Add<T>(this T view, IEnumerable<View> children) where T : Frame
        {
            ((IContainer)view).AddRange(children);
            return view;
        }

        public static T Clipped<T>(this T view) where T : Frame
        {
            view.Set("ClipToBounds", true);
            return view;
        }
    }
}