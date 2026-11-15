using VRage.Input;
using VRageMath;

namespace Lumel.Input
{
    /// <summary>
    ///     Receives input for an element: presses, keys, typed characters, keyboard focus changes and pointer hover.
    /// </summary>
    /// <remarks>Implemented explicitly by <see cref="Element" />, so input reaches an element only from an input root, its host, or a deliberate cast.</remarks>
    public interface IInputTarget
    {
        /// <summary>
        ///     Called when a press of <paramref name="button" /> begins at <paramref name="point" />, where
        ///     <paramref name="clickCount" /> counts the presses in quick succession.
        /// </summary>
        void PointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl);

        /// <summary>
        ///     Called when the pointer moves to <paramref name="point" /> during a press of <paramref name="button" />
        ///     that began on the element.
        /// </summary>
        void PointerMove(Vector2 point, PointerButton button);

        /// <summary>
        ///     Called when a press of <paramref name="button" /> that began on the element is released at
        ///     <paramref name="point" />.
        /// </summary>
        void PointerUp(Vector2 point, PointerButton button);

        /// <summary>Called when a press that began on the element ends without its release reaching it.</summary>
        void PointerCaptureLost();

        /// <summary>Called as a press, by pointer or key, is about to run the element's command.</summary>
        void Pressed();

        /// <summary>Offers <paramref name="key" /> to the element and returns true if it handled it.</summary>
        bool Key(InputRoot input, MyKeys key, bool shift, bool ctrl);

        /// <summary>
        ///     Offers a wheel turn of <paramref name="amount" /> to the element and returns true if it handled it.
        /// </summary>
        bool Wheel(float amount);

        /// <summary>
        ///     Offers <paramref name="intent" />, the meaning of a key no element handled, to the element and returns
        ///     true if it handled it.
        /// </summary>
        bool Intent(InputRoot input, Intent intent);

        /// <summary>Gives the element the typed character <paramref name="c" />.</summary>
        void Text(char c);

        /// <summary>
        ///     Called when the element gains keyboard focus, where <paramref name="byPress" /> says whether a pointer press
        ///     gave it.
        /// </summary>
        void Focus(bool byPress);

        /// <summary>Called when the element loses keyboard focus.</summary>
        void Blur();

        /// <summary>Sets whether the element, while it holds keyboard focus, shows it.</summary>
        bool ShowsFocus { set; }

        /// <summary>Sets whether the pointer is over the element.</summary>
        bool IsPointerOver { set; }
    }
}
