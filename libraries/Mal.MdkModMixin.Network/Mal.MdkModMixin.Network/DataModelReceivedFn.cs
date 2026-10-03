
namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Delegate invoked when a network message of a specific type is received.
    /// </summary>
    /// <typeparam name="TModel">The struct data model type that was received.</typeparam>
    /// <param name="channelId">The channel ID the message was received on.</param>
    /// <param name="msg">The deserialized message data.</param>
    /// <param name="senderId">The Steam ID of the player who sent the message.</param>
    /// <param name="fromServer">True if the message came from the server; false if from another client.</param>
    public delegate void DataModelReceivedFn<in TModel>(ushort channelId, TModel msg, ulong senderId, bool fromServer) where TModel : struct;
}
