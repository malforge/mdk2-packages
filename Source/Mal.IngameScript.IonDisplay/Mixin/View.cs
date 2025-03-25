using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    public abstract class View : IView
    {
        readonly Dictionary<string, IProperty> _properties = new Dictionary<string, IProperty>(StringComparer.Ordinal);

        protected View()
        {
            Set("IsVisible", true, true);
            Set("Bounds", new RectangleF(0, 0, -1, -1), true);
        }

        IView IView.Parent { get; set; }
        public IIon Context { get; private set; }

        public T Get<T>(string name)
        {
            IProperty property;
            if (!_properties.TryGetValue(name, out property)) return default(T);
            return (T)property.Get();
        }

        public void Set<T>(string name, T value, bool valueIsDefault = false)
        {
            IProperty property;
            if (!_properties.TryGetValue(name, out property))
            {
                property = new Property<T>(this, name, value, valueIsDefault ? value : default(T));
                _properties.Add(name, property);
            }
            else
                ((Property<T>)property).Set(value);
        }

        void IView.BeginFrame(IIon ion)
        {
            Context = ion;
            foreach (var property in _properties.Values) property.Reset();
            OnBeforeFrame();
        }

        void IView.Draw(DC dc)
        {
            var bounds = Get<RectangleF>("Bounds");
            bounds = new RectangleF(
                dc.Bounds.X + bounds.X,
                dc.Bounds.Y + bounds.Y,
                bounds.Width < 0 ? dc.Bounds.Width : bounds.Width,
                bounds.Height < 0 ? dc.Bounds.Height : bounds.Height);
            OnDraw(dc.WithBounds(bounds));
        }

        protected abstract void OnBeforeFrame();

        public Vector2 Measure(bool withMargins)
        {
            var margin = Get<Thickness>("Margin");
            var size = Measure();
            if (withMargins)
                size += margin.Size;
            return size;
        }

        public virtual Vector2 Measure() => Vector2.Zero;

        protected static void Draw(IView view, DC dc)
        {
            if (view.Get<bool>("IsVisible"))
                view.Draw(dc);
        }

        protected abstract void OnDraw(DC dc);

        protected virtual void OnPropertyChanged(IProperty property) { }

        public interface IProperty
        {
            string Name { get; }
            View Parent { get; }
            object Get();
            void Set(object value);
            void Reset();
        }

        public class Property<T> : IProperty
        {
            T _value;

            public Property(View parent, string name, T value, T defaultValue = default(T))
            {
                Name = name;
                Parent = parent;
                Default = defaultValue;
                _value = value;
            }

            public T Default { get; }
            public string Name { get; }
            public View Parent { get; }

            object IProperty.Get() => Get();
            void IProperty.Set(object value) => Set((T)value);
            public void Reset() => _value = Default;
            public T Get() => _value;

            public void Set(T value)
            {
                _value = value;
                Parent.OnPropertyChanged(this);
            }
        }
    }
}