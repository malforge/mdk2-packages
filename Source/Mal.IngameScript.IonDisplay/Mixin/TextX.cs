using System;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    public static class TextX
    {
        static readonly StringBuilder Buf = new StringBuilder();

        public static Text Text(this IIon ion, string value, Color color)
        {
            var view = ion.View<Text>();
            view.Set("Value", value);
            view.Set("Color", color);
            return view;
        }

        public static Text Text(this IIon ion, Func<StringBuilder, StringBuilder> valueFn, Color color)
        {
            var view = ion.View<Text>();
            view.Set("Value", valueFn(Buf.Clear()).ToString());
            view.Set("Color", color);
            return view;
        }
        
        public static T FontSize<T>(this T view, float fontSize) where T : Text
        {
            view.Set("FontSize", fontSize);
            return view;
        }
        
        public static T AlignRight<T>(this T view) where T : Text
        {
            view.Set("Alignment", TextAlignment.RIGHT);
            return view;
        }

        public static T AlignCenter<T>(this T view) where T : Text
        {
            view.Set("Alignment", TextAlignment.CENTER);
            return view;
        }
    }
}