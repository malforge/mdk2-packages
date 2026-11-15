using System;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>The look of a link of one class: each part it sets, leaving the rest to the text's own link look.</summary>
    public sealed class LinkLook
    {
        readonly Action _changed;
        Color? _background;
        Color? _color;
        bool? _presses;
        UnderlineStyle? _underline;
        float? _underlineThickness;

        /// <summary>
        ///     Creates a look that sets nothing and calls <paramref name="changed" /> whenever anything it sets
        ///     changes.
        /// </summary>
        public LinkLook(Action changed)
        {
            _changed = changed;
        }

        /// <summary>Gets or sets the color of the link's text and underline, or null to leave it to the looks beneath.</summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color painted behind the link's text, or null to leave it to the looks beneath.</summary>
        public Color? Background
        {
            get { return _background; }
            set
            {
                _background = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the thickness of the link's underline, or null to leave it to the looks beneath.</summary>
        public float? UnderlineThickness
        {
            get { return _underlineThickness; }
            set
            {
                _underlineThickness = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets how the link's underline is drawn, or null to leave it to the looks beneath.</summary>
        public UnderlineStyle? Underline
        {
            get { return _underline; }
            set
            {
                _underline = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets whether a press on the link raises <see cref="RichText.LinkClicked" />, or null to leave it to the
        ///     looks beneath; false leaves the link's tip.
        /// </summary>
        public bool? Presses
        {
            get { return _presses; }
            set
            {
                _presses = value;
                OnChanged();
            }
        }

        /// <summary>Applies everything <paramref name="over" /> sets on top of what this look sets.</summary>
        public void Merge(LinkLook over)
        {
            _color = over._color ?? _color;
            _background = over._background ?? _background;
            _underlineThickness = over._underlineThickness ?? _underlineThickness;
            _underline = over._underline ?? _underline;
            _presses = over._presses ?? _presses;
        }

        /// <summary>Returns true if this look sets exactly what <paramref name="other" /> sets.</summary>
        public bool SetsSameAs(LinkLook other)
        {
            return _color == other._color && _background == other._background && _underlineThickness == other._underlineThickness &&
                   _underline == other._underline && _presses == other._presses;
        }

        void OnChanged()
        {
            _changed?.Invoke();
        }
    }
}
