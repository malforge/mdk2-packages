using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Mal.MdkModMixin.Network.Tests
{
    /// <summary>A stand-in for the game's multiplayer: records what is sent, and hands messages to the channel on request.</summary>
    /// <remarks>
    ///     On a server, a message the server sends to itself is handed to the channel at once, as the game does it
    ///     (the game invokes such a message locally).
    /// </remarks>
    public class FakeProxy : INetworkProxy
    {
        readonly List<Action<ushort, byte[], ulong, bool>> _handlers = new List<Action<ushort, byte[], ulong, bool>>();

        /// <summary>A machine: a server or a client.</summary>
        public FakeProxy(bool isServer)
        {
            IsServer = isServer;
        }

        public bool IsServer { get; }

        public bool IsHeadlessServer
        {
            get { return false; }
        }

        public bool IsClient
        {
            get { return true; }
        }

        /// <summary>The players in the world.</summary>
        public List<IMyPlayer> Players { get; } = new List<IMyPlayer>();

        /// <summary>Every message sent to one player, with whom.</summary>
        public List<KeyValuePair<ulong, byte[]>> ToPlayers { get; } = new List<KeyValuePair<ulong, byte[]>>();

        /// <summary>The channel id of every handler registered, in order.</summary>
        public List<ushort> Registered { get; } = new List<ushort>();

        public void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
            Registered.Add(id);
            _handlers.Add(messageHandler);
        }

        public void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
            _handlers.Remove(messageHandler);
        }

        /// <summary>
        ///     On a server, a player whose messages it hands to its own channel at once, as a server does with its own
        ///     host's player; 0 for none. A client never does this: its messages to itself go by way of the server.
        /// </summary>
        public ulong DeliversOnTheSpotTo { get; set; }

        public void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable)
        {
            if (Records)
                ToPlayers.Add(new KeyValuePair<ulong, byte[]>(recipientId, (byte[])data.Clone()));
            if (IsServer && recipientId != 0 && recipientId == DeliversOnTheSpotTo)
                Arrive(channelId, data, 0, true);
        }

        /// <summary>How many messages were sent to everyone else.</summary>
        public int ToOthers { get; private set; }

        public void SendMessageToOthers(ushort channelId, byte[] data, bool reliable)
        {
            ToOthers++;
        }

        /// <summary>Whether sent messages are kept (copied) in the lists below; off for allocation tests.</summary>
        public bool Records { get; set; } = true;

        /// <summary>Every message a client sent to the server.</summary>
        public List<byte[]> ToServer { get; } = new List<byte[]>();

        public void SendMessageToServer(ushort channelId, byte[] data, bool reliable)
        {
            if (IsServer)
                Arrive(channelId, data, 0, true);
            else if (Records)
                ToServer.Add((byte[])data.Clone());
        }

        public void GetPlayers(List<IMyPlayer> players)
        {
            players.AddRange(Players);
        }

        /// <summary>Hands a message to the channel as if it arrived from the game's network.</summary>
        public void Arrive(ushort channelId, byte[] data, ulong senderId, bool fromServer)
        {
            foreach (var handler in _handlers.ToArray())
                handler(channelId, data, senderId, fromServer);
        }
    }
}
