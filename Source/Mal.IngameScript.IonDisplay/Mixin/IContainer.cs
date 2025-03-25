using System.Collections.Generic;

namespace IngameScript
{
    public interface IContainer: IView
    {
        IReadOnlyList<IView> Children { get; }
        void Add(IView view);
        void AddRange(IEnumerable<IView> views);
    }
}