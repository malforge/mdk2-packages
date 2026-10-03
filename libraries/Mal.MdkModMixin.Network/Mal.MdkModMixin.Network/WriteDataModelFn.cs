
namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Delegate for serializing a struct data model to a byte array.
    /// </summary>
    /// <typeparam name="TModel">The struct data model type to serialize.</typeparam>
    /// <param name="msg">The model instance to serialize (passed by reference to avoid copying).</param>
    /// <param name="data">The destination byte array.</param>
    /// <param name="offset">The position in the byte array; updated to point after the serialized data.</param>
    /// <returns>The number of bytes written to the array.</returns>
    public delegate int WriteDataModelFn<TModel>(ref TModel msg, byte[] data, ref int offset) where TModel : struct;
}
