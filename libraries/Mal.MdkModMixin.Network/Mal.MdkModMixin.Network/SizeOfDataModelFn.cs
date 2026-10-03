
namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Delegate for calculating the serialized size of a struct data model in bytes.
    /// </summary>
    /// <typeparam name="TModel">The struct data model type to measure.</typeparam>
    /// <param name="msg">The model instance to measure (passed by reference to avoid copying).</param>
    /// <returns>The number of bytes required to serialize this model instance.</returns>
    public delegate int SizeOfDataModelFn<TModel>(ref TModel msg) where TModel : struct;
}
