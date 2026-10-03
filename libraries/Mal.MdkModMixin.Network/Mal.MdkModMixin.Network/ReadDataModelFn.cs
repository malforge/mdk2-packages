
namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Delegate for deserializing a struct data model from a byte array.
    /// </summary>
    /// <typeparam name="TModel">The struct data model type to deserialize.</typeparam>
    /// <param name="data">The source byte array containing serialized data.</param>
    /// <param name="offset">The position in the byte array; updated to point after the deserialized data.</param>
    /// <param name="msg">The deserialized model instance.</param>
    /// <returns>The number of bytes consumed from the array.</returns>
    public delegate int ReadDataModelFn<TModel>(byte[] data, ref int offset, out TModel msg) where TModel : struct;
}
