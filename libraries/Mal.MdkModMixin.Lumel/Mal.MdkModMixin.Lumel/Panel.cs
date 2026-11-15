
namespace Lumel
{
    /// <summary>A container that holds any number of children and lays them out according to its kind.</summary>
    public abstract class Panel : Container
    {
        /// <summary>Creates an empty panel.</summary>
        protected Panel()
        {
            Children = CreateChildren();
        }

        /// <summary>Gets the children the panel holds, in order.</summary>
        public Collection Children { get; }
    }
}
