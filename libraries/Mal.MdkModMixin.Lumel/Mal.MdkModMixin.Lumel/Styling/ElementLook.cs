using System;
using Lumel.Drawing;
using Lumel.Input;
using VRageMath;

namespace Lumel.Styling
{
    /// <summary>
    ///     What any element looks like in one state: only what that state sets, leaving the rest to the looks beneath.
    /// </summary>
    /// <remarks>
    ///     A control with more to look like derives its own look from this one, and gives every property a field, a
    ///     setter that calls <see cref="OnChanged" />, and a line in <see cref="Merge" /> and <see cref="Clear" />.
    /// </remarks>
    public class ElementLook
    {
        Color? _background;
        Color? _border;
        float? _borderThickness;
        Thickness? _padding;
        Pattern _pattern;
        Sound _hoverSound;
        Sound _clickSound;

        /// <summary>Raised when anything this look sets changes.</summary>
        public event Action Changed;

        /// <summary>
        ///     Gets or sets the color painted behind the element, or null to leave it to the looks beneath.
        /// </summary>
        public Color? Background
        {
            get { return _background; }
            set
            {
                _background = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the pattern laid over the element's background, or null to leave it to the looks beneath.
        /// </summary>
        public Pattern Pattern
        {
            get { return _pattern; }
            set
            {
                _pattern = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the line drawn around the element, inside it, or null to leave it to the looks
        ///     beneath.
        /// </summary>
        public Color? Border
        {
            get { return _border; }
            set
            {
                _border = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how thick the border is drawn, in UI units, or null to leave it to the looks beneath.
        /// </summary>
        public float? BorderThickness
        {
            get { return _borderThickness; }
            set
            {
                _borderThickness = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space kept clear inside the element, or null to leave it to the looks beneath.
        /// </summary>
        public Thickness? Padding
        {
            get { return _padding; }
            set
            {
                _padding = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the sound played as the pointer comes onto the element while a press would run its command,
        ///     or null to leave it to the look beneath.
        /// </summary>
        public Sound HoverSound
        {
            get { return _hoverSound; }
            set
            {
                _hoverSound = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the sound played as a press runs the element's command, or null to leave it to the look beneath.</summary>
        public Sound ClickSound
        {
            get { return _clickSound; }
            set
            {
                _clickSound = value;
                OnChanged();
            }
        }

        /// <summary>Applies every property that <paramref name="over" /> sets on top of what this look sets.</summary>
        /// <remarks>An override takes its own properties where <paramref name="over" /> is a look of its kind, and none where it is not.</remarks>
        public virtual void Merge(ElementLook over)
        {
            _background = over._background ?? _background;
            _pattern = over._pattern ?? _pattern;
            _border = over._border ?? _border;
            _borderThickness = over._borderThickness ?? _borderThickness;
            _padding = over._padding ?? _padding;
            _hoverSound = over._hoverSound ?? _hoverSound;
            _clickSound = over._clickSound ?? _clickSound;
        }

        /// <summary>Clears every property, leaving them all to the looks beneath.</summary>
        public virtual void Clear()
        {
            _background = null;
            _pattern = null;
            _border = null;
            _borderThickness = null;
            _padding = null;
            _hoverSound = null;
            _clickSound = null;
        }

        /// <summary>Notifies listeners that something this look sets has changed.</summary>
        protected void OnChanged()
        {
            Changed?.Invoke();
        }
    }
}
