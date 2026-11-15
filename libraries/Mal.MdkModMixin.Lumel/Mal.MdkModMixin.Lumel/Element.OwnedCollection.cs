namespace Lumel
{
    public abstract partial class Element
    {
        /// <summary>The children of one element, which only that element can create.</summary>
        sealed class OwnedCollection : Collection
        {
            public OwnedCollection(Element owner) : base(owner)
            {
            }
        }
    }
}
