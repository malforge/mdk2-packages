using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Abstraction over the Space Engineers multiplayer networking API.
    ///     Allows for dependency injection and testing by providing an interface for network operations.
    /// </summary>
    public interface INetworkProxy
    {
        /// <summary>
        ///     True when running as a dedicated/headless server instance.
        /// </summary>
        bool IsHeadlessServer { get; }

        /// <summary>
        ///     True when this process is acting as the multiplayer server.
        /// </summary>
        bool IsServer { get; }

        /// <summary>
        ///     True when this process is a multiplayer client (not a dedicated server).
        ///     A hosted multiplayer server is both a server and a client.
        /// </summary>
        bool IsClient { get; }

        /// <summary>
        ///     Registers a secure message handler for the specified channel ID.
        /// </summary>
        /// <param name="id">The channel identifier.</param>
        /// <param name="messageHandler">The callback to invoke when messages are received on this channel.</param>
        void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler);

        /// <summary>
        ///     Unregisters a previously registered secure message handler for the specified channel ID.
        /// </summary>
        /// <param name="id">The channel identifier.</param>
        /// <param name="messageHandler">The callback to unregister.</param>
        void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler);

        /// <summary>
        ///     Sends a message to the specified recipient.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="data">The message data to send.</param>
        /// <param name="recipientId">The Steam ID of the recipient player.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
        void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable);

        /// <summary>
        ///     Broadcasts a message to all other players (excludes the sender).
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="data">The message data to send.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
        void SendMessageToOthers(ushort channelId, byte[] data, bool reliable);

        /// <summary>
        ///     Sends a message to the server.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="data">The message data to send.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
        void SendMessageToServer(ushort channelId, byte[] data, bool reliable);

        /// <summary>
        ///     Adds every player in the world to <paramref name="players" />, for <see cref="NetworkChannel.SendToNearby{T}" />.
        /// </summary>
        /// <param name="players">The list to add the players to.</param>
        void GetPlayers(List<IMyPlayer> players);
    }
}