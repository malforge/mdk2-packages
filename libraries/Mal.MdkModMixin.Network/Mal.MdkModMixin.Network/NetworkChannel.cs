using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

// ReSharper disable PossibleNullReferenceException

namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Represents a network message channel identified by a 16-bit channel identifier.
    ///     Manages registration of struct-based data models with custom serialization delegates.
    ///     Use static <see cref="Create(ushort)" /> methods to obtain a <see cref="Builder" />, then register
    ///     model types and call <see cref="Builder.Build" /> to construct the channel.
    ///     <para>
    ///         <strong>Important:</strong> Use only a single shared channel per mod to avoid consuming multiple channel IDs.
    ///         A single channel can handle multiple message types through the type registration system.
    ///     </para>
    /// </summary>
    /// <example>
    ///     <code>
    /// // 1. Define your message struct
    /// public struct PlayerActionMsg
    /// {
    ///     public int ActionType;
    ///     public Vector3D Position;
    ///     
    ///     public static int SizeOf(ref PlayerActionMsg msg)
    ///     {
    ///         return sizeof(int) + sizeof(double) * 3;
    ///     }
    ///     
    ///     public static int Read(byte[] data, ref int offset, out PlayerActionMsg msg)
    ///     {
    ///         var start = offset;
    ///         msg = new PlayerActionMsg();
    ///         ByteData.TryReadInt32(data, ref offset, out msg.ActionType);
    ///         ByteData.TryReadVector3D(data, ref offset, out msg.Position);
    ///         return offset - start;
    ///     }
    ///     
    ///     public static int Write(ref PlayerActionMsg msg, byte[] data, ref int offset)
    ///     {
    ///         var start = offset;
    ///         ByteData.Write(msg.ActionType, data, ref offset);
    ///         ByteData.Write(msg.Position, data, ref offset);
    ///         return offset - start;
    ///     }
    /// }
    /// 
    /// // 2. Create and configure the channel (typically in BeforeStart)
    /// // Use your session component type to generate a channel ID with low collision probability
    /// NetworkChannel _channel;
    /// 
    /// public override void BeforeStart()
    /// {
    ///     _channel = NetworkChannel.Create(GetType())
    ///         .UsingModel&lt;PlayerActionMsg&gt;(PlayerActionMsg.SizeOf, PlayerActionMsg.Read, PlayerActionMsg.Write)
    ///         .Build();
    ///     
    ///     // Server: Listen for messages from clients
    ///     if (NetworkChannel.Network.IsServer)
    ///     {
    ///         _channel.ListenFor&lt;PlayerActionMsg&gt;(OnPlayerActionReceived);
    ///     }
    /// }
    /// 
    /// // 3. Handle received messages
    /// void OnPlayerActionReceived(ushort channelId, PlayerActionMsg msg, ulong senderId, bool fromServer)
    /// {
    ///     // Process the action on the server
    ///     MyLog.Default.Info($"Player {senderId} performed action {msg.ActionType} at {msg.Position}");
    /// }
    /// 
    /// // 4. Send messages
    /// public void PerformAction(int actionType)
    /// {
    ///     var msg = new PlayerActionMsg 
    ///     { 
    ///         ActionType = actionType, 
    ///         Position = MyAPIGateway.Session.LocalHumanPlayer.Character.GetPosition() 
    ///     };
    ///     
    ///     // Client to server
    ///     _channel.SendToServer(msg, reliable: true);
    ///     
    ///     // Or broadcast to all players
    ///     // _channel.Broadcast(msg, reliable: true, includingSelf: false);
    ///     
    ///     // Sending to one specific player works from the server only: the game drops a client's message to
    ///     // another player. On the server: _channel.Send(msg, recipientSteamId, reliable: true);
    /// }
    /// 
    /// // 5. Clean up (typically in UnloadData)
    /// protected override void UnloadData()
    /// {
    ///     _channel?.Dispose();
    ///     _channel = null;
    /// }
    /// </code>
    /// </example>
    public class NetworkChannel : IDisposable
    {
        // Reserved internal type id marking a SendToNearby envelope (origin + radius + an inner,
        // normal message). Handled in OnMessageReceived before normal dispatch. It only has to be
        // distinct from real message type ids, which are IdOf(FullName) values - collision
        // chance is ~1 in 4 billion.
        const int ProximityEnvelopeTypeId = 0x00C0FFEE;

        readonly ushort _channelId;

        // The registered message types: by type id for what arrives, and by type for what is sent, so a send
        // finds its type without hashing the type's name.
        readonly Dictionary<int, IDataModelInfo> _dataModels;
        readonly Dictionary<Type, IDataModelInfo> _dataModelsByType = new Dictionary<Type, IDataModelInfo>();

        // Envelope header: type id, origin, radius, reliable flag.
        const int EnvelopeHeaderSize = 4 + 24 + 8 + 4;

        // Buffers for nearby sends and relays, kept so they allocate nothing once each is big enough. One set
        // per nesting level: a relay to the host is delivered on the spot, and if the host's listener sends
        // nearby in turn, that inner send must not overwrite the list or bytes the outer relay is still
        // working through. Locked, as the send buffers are; the lock lets the same thread back in.
        readonly object _nearbyLock = new object();
        readonly List<NearbyBuffers> _nearbyBuffers = new List<NearbyBuffers>();
        int _nearbyDepth;

        NetworkChannel(ushort channelId, Dictionary<int, IDataModelInfo> dataModels)
        {
            // Copied, so a builder used again after Build cannot add types to a channel already built. The
            // message models themselves (listeners, send buffers) stay shared with anything else built from it.
            _dataModels = new Dictionary<int, IDataModelInfo>(dataModels);
            foreach (var dataModel in _dataModels.Values)
                _dataModelsByType[dataModel.Type] = dataModel;
            _channelId = channelId;
            Network.RegisterSecureMessageHandler(_channelId, OnMessageReceived);
        }

        /// <summary>
        ///     Gets or sets the network proxy used for multiplayer communication.
        ///     Defaults to <see cref="DefaultNetwork" />. Can be replaced for testing or custom network implementations.
        /// </summary>
        /// <remarks>
        ///     Static because the game hands a mod its network through the static
        ///     <c>MyAPIGateway.Multiplayer</c>, so whatever stands in for it in a test has to be reachable the
        ///     same way.
        /// </remarks>
        public static INetworkProxy Network { get; set; } = new DefaultNetwork();

        /// <summary>
        ///     Disposes the network channel and unregisters the message handler from the multiplayer system.
        /// </summary>
        public void Dispose()
        {
            Network.UnregisterSecureMessageHandler(_channelId, OnMessageReceived);
        }

        /// <summary>
        ///     Creates a new channel builder with a channel ID derived from the specified type's full name.
        ///     The channel ID should avoid collisions with other loaded mods.
        ///     Using a type from your mod works well if you have a proper namespace and unique type name.
        ///     <strong>Recommended:</strong> Pass your session component type (e.g., <c>GetType()</c>) to reduce collision
        ///     likelihood.
        /// </summary>
        /// <param name="type">The type whose full name will be used to generate the channel ID.</param>
        /// <returns>A new <see cref="Builder" /> instance.</returns>
        public static Builder Create(Type type)
        {
            var channelId = (ushort)(IdOf(type.FullName) & 0xFFFF);
            return new Builder(channelId);
        }

        /// <summary>
        ///     Creates a new channel builder with a channel ID derived from the specified channel name.
        ///     The channel ID should avoid collisions with other loaded mods.
        ///     Only use this if the string is designed to be unique (e.g., includes your mod name or Steam Workshop ID).
        /// </summary>
        /// <param name="channelName">The channel name to use for generating the channel ID.</param>
        /// <returns>A new <see cref="Builder" /> instance.</returns>
        public static Builder Create(string channelName)
        {
            var channelId = (ushort)(IdOf(channelName) & 0xFFFF);
            return new Builder(channelId);
        }

        /// <summary>
        ///     Returns the id <paramref name="text" /> stands for on the wire: the 32-bit FNV-1a hash of its UTF-16
        ///     code units, the same on every machine.
        /// </summary>
        /// <remarks>
        ///     <see cref="string.GetHashCode" /> cannot serve: on .NET Core and later it differs in every process, so a
        ///     client and a server would never agree on an id. Changing this changes every id a mod sends.
        /// </remarks>
        /// <param name="text">The name to hash, such as a type's full name.</param>
        public static int IdOf(string text)
        {
            // FNV-1a, 32-bit: offset basis 2166136261, prime 16777619 (Fowler, Noll and Vo).
            var hash = 2166136261u;
            foreach (var c in text)
            {
                hash ^= c;
                hash = unchecked(hash * 16777619u);
            }

            return unchecked((int)hash);
        }

        /// <summary>
        ///     Creates a new channel builder with the specified channel ID.
        ///     The channel ID should avoid collisions with other loaded mods.
        ///     This variant is commonly used with the first bits of a mod's Steam Workshop ID to reduce collision likelihood.
        /// </summary>
        /// <param name="channelId">The 16-bit channel identifier to use.</param>
        /// <returns>A new <see cref="Builder" /> instance.</returns>
        public static Builder Create(ushort channelId)
        {
            return new Builder(channelId);
        }

        void OnMessageReceived(ushort channelId, byte[] data, ulong senderId, bool fromServer)
        {
            var pos = 0;
            int typeId;
            if (!ByteData.TryReadInt32(data, ref pos, out typeId))
            {
                MyLog.Default.Warning($"NetworkChannel received malformed message on channel {_channelId}.");
                return;
            }

            if (typeId == ProximityEnvelopeTypeId)
            {
                OnProximityEnvelope(channelId, data, ref pos, senderId);
                return;
            }

            IDataModelInfo dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
            {
                MyLog.Default.Warning($"NetworkChannel received message for unregistered type ID {typeId} on channel {_channelId}.");
                return;
            }

            dataModel.DidReceive(this, channelId, data, ref pos, senderId, fromServer);
        }

        // Server-side unwrap of a SendToNearby envelope: read the origin/radius the sender attached,
        // then forward the inner (normal) message to every player within range, minus the sender.
        // Recipients get the plain inner message, so their ListenFor handlers fire as usual - no
        // envelope leaks past the server. Clients never receive envelopes, so this no-ops off-server.
        void OnProximityEnvelope(ushort channelId, byte[] data, ref int pos, ulong senderId)
        {
            Vector3D origin;
            double radius;
            int reliableFlag;
            if (!ByteData.TryReadVector3D(data, ref pos, out origin)) return;
            if (!ByteData.TryReadDouble(data, ref pos, out radius)) return;
            if (!ByteData.TryReadInt32(data, ref pos, out reliableFlag)) return;
            if (!Network.IsServer) return;
            if (!ReachesAnyone(origin, radius)) return;

            int innerLen = data.Length - pos;
            if (innerLen <= 0) return;
            lock (_nearbyLock)
            {
                var buffers = EnterNearby();
                try
                {
                    buffers.Inner = AtLeast(buffers.Inner, innerLen);
                    Array.Copy(data, pos, buffers.Inner, 0, innerLen);
                    RelayToNearby(buffers, channelId, buffers.Inner, origin, radius, reliableFlag != 0, senderId);
                }
                finally
                {
                    _nearbyDepth--;
                }
            }
        }

        // Whether a nearby send reaches anyone at all. A negative or NaN radius, or an origin that is not a real
        // position (NaN or infinite), reaches no one, the sender included. The range test alone would not do: it
        // squares the radius, which makes a negative one positive.
        static bool ReachesAnyone(Vector3D origin, double radius)
        {
            return radius >= 0 && IsFinite(origin.X) && IsFinite(origin.Y) && IsFinite(origin.Z);
        }

        static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        // The buffers for the nesting level being entered. The caller holds _nearbyLock and leaves the level
        // with _nearbyDepth-- when done.
        NearbyBuffers EnterNearby()
        {
            if (_nearbyDepth == _nearbyBuffers.Count)
                _nearbyBuffers.Add(new NearbyBuffers());
            return _nearbyBuffers[_nearbyDepth++];
        }

        // A buffer at least size bytes long: the one given if it is, a new one otherwise. Like the send
        // buffers, a kept buffer can be longer than the message in it; receivers read only what they need.
        static byte[] AtLeast(byte[] buffer, int size)
        {
            return buffer.Length >= size ? buffer : new byte[size];
        }

        sealed class NearbyBuffers
        {
            public readonly List<IMyPlayer> Players = new List<IMyPlayer>();
            public byte[] Envelope = new byte[0];
            public byte[] Inner = new byte[0];
        }

        // Forwards already-serialized message bytes to every player within radius of origin, skipping
        // excludeSteamId. Server-side only - it needs every player's position. Off-server (no
        // MyAPIGateway.Players) it no-ops.
        // Bots are skipped: they carry the server's own Steam id, so each would be another send to the
        // server. Players with no body are skipped: the game gives them the world origin as their
        // position. Callers have already turned away a radius or origin that reaches no one (see ReachesAnyone).
        void RelayToNearby(NearbyBuffers buffers, ushort channelId, byte[] inner, Vector3D origin, double radius, bool reliable, ulong excludeSteamId)
        {
            var players = buffers.Players;
            players.Clear();
            Network.GetPlayers(players);
            double r2 = radius * radius;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null) continue;
                // Distance first, so a player out of range costs one check rather than four.
                if (!(Vector3D.DistanceSquared(p.GetPosition(), origin) <= r2)) continue;
                var steamId = p.SteamUserId;
                if (steamId == excludeSteamId || p.IsBot || p.Character == null) continue;
                Network.SendMessageTo(channelId, inner, steamId, reliable);
            }

            // Don't keep players alive between relays.
            players.Clear();
        }

        /// <summary>
        ///     Broadcasts a message of type <typeparamref name="T" /> to every player within
        ///     <paramref name="radius" /> metres of <paramref name="origin" /> (the sender excluded).
        ///     Like the other senders this is "just another way to send data"; the server is the relay
        ///     hub (clients can't address each other), so from a client the message round-trips through
        ///     the server, which forwards it to the in-range recipients. On a host the forward is
        ///     direct. Recipients receive the plain message and their <see cref="ListenFor{T}" />
        ///     handlers fire as normal.
        /// </summary>
        /// <typeparam name="T">The struct data model type to send.</typeparam>
        /// <param name="model">The message data to send.</param>
        /// <param name="origin">World position the radius is measured from (e.g. the broadcasting block).</param>
        /// <param name="radius">Maximum distance in metres a recipient may be from <paramref name="origin" />; a negative one reaches no one.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss.</param>
        /// <param name="includingSelf">If true, also delivers to the sender; if false (default), excludes it.</param>
        /// <exception cref="InvalidOperationException">Thrown if the type is not registered on this channel.</exception>
        public void SendToNearby<T>(T model, Vector3D origin, double radius, bool reliable, bool includingSelf = false) where T : struct
        {
            var typed = ModelFor<T>();
            if (!ReachesAnyone(origin, radius))
                return;

            lock (_nearbyLock)
            {
                var buffers = EnterNearby();
                try
                {
                    // Inner = the normal [typeId][payload] bytes a recipient dispatches as type T.
                    var innerLen = typed.InnerSize(ref model);
                    byte[] inner = null;
                    if (Network.IsServer)
                    {
                        // Already the relay hub: forward straight to in-range players (minus self).
                        inner = buffers.Inner = AtLeast(buffers.Inner, innerLen);
                        var ip = 0;
                        typed.WriteInner(ref model, inner, ref ip);
                        RelayToNearby(buffers, _channelId, inner, origin, radius, reliable, LocalSteamId());
                    }
                    else
                    {
                        // Write inner straight into a proximity envelope and ship it to the server to relay.
                        var env = buffers.Envelope = AtLeast(buffers.Envelope, EnvelopeHeaderSize + innerLen);
                        var ep = 0;
                        ByteData.Write(ProximityEnvelopeTypeId, env, ref ep);
                        ByteData.Write(origin, env, ref ep);
                        ByteData.Write(radius, env, ref ep);
                        ByteData.Write(reliable ? 1 : 0, env, ref ep);
                        typed.WriteInner(ref model, env, ref ep);
                        Network.SendMessageToServer(_channelId, env, reliable);

                        // Sending to self needs inner on its own, at the start of an array.
                        if (includingSelf)
                        {
                            inner = buffers.Inner = AtLeast(buffers.Inner, innerLen);
                            Array.Copy(env, EnvelopeHeaderSize, inner, 0, innerLen);
                        }
                    }

                    if (includingSelf)
                        SendToSelf(_channelId, inner, reliable);
                }
                finally
                {
                    _nearbyDepth--;
                }
            }
        }

        // The registered data model for T.
        DataModelInfo<T> ModelFor<T>() where T : struct
        {
            IDataModelInfo dataModel;
            if (!_dataModelsByType.TryGetValue(typeof(T), out dataModel))
                throw new InvalidOperationException($"Type {typeof(T).FullName} is not registered on this channel.");
            return (DataModelInfo<T>)dataModel;
        }

        // Delivers data to this machine's own channel. A server sends to itself, which the game hands over
        // locally; a client sends to its own player, by way of the server. With no local player (or no session
        // yet, while loading or unloading) there is no self to send to.
        static void SendToSelf(ushort channelId, byte[] data, bool reliable)
        {
            if (Network.IsServer)
            {
                Network.SendMessageToServer(channelId, data, reliable);
                return;
            }

            var selfId = LocalSteamId();
            if (selfId != 0UL)
                Network.SendMessageTo(channelId, data, selfId, reliable);
        }

        static ulong LocalSteamId()
        {
            var p = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
            return p != null ? p.SteamUserId : 0UL;
        }

        /// <summary>
        ///     Registers a handler to be invoked when a message of type <typeparamref name="T" /> is received on this channel.
        ///     The type must have been previously registered with <see cref="Builder.UsingModel{TModel}" />.
        /// </summary>
        /// <typeparam name="T">The struct data model type to listen for.</typeparam>
        /// <param name="handler">The callback to invoke when a message is received.</param>
        /// <exception cref="InvalidOperationException">Thrown if the type is not registered on this channel.</exception>
        public void ListenFor<T>(DataModelReceivedFn<T> handler) where T : struct
        {
            var typedDataModel = ModelFor<T>();
            typedDataModel.AddListener(handler);
        }

        /// <summary>
        ///     Unregisters a handler that was previously registered with <see cref="ListenFor{T}" />.
        ///     The handler will no longer be invoked when messages of type <typeparamref name="T" /> are received.
        /// </summary>
        /// <typeparam name="T">The struct data model type to stop listening for.</typeparam>
        /// <param name="handler">The callback to unregister.</param>
        /// <exception cref="InvalidOperationException">Thrown if the type is not registered on this channel.</exception>
        public void StopListeningFor<T>(DataModelReceivedFn<T> handler) where T : struct
        {
            var typedDataModel = ModelFor<T>();
            typedDataModel.RemoveListener(handler);
        }

        /// <summary>
        ///     Sends a message of type <typeparamref name="T" /> to the specified recipient.
        ///     The type must have been previously registered with <see cref="Builder.UsingModel{TModel}" />.
        /// </summary>
        /// <typeparam name="T">The struct data model type to send.</typeparam>
        /// <param name="model">The message data to send.</param>
        /// <param name="recipientId">The Steam ID of the recipient player.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
        /// <exception cref="InvalidOperationException">Thrown if the type is not registered on this channel.</exception>
        /// <remarks>
        ///     From the server this reaches any player. From a client it reaches only that client's own machine: the game
        ///     routes it through the server and back, so it arrives a round trip later, marked as coming from the server.
        ///     A message a client sends to another player is dropped by the game; to reach another player, a client asks
        ///     the server with <see cref="SendToServer{T}" />.
        ///     A recipient id of 0, which is how the game says "no player" (an unowned or NPC block's owner, say),
        ///     sends nothing; so does <see cref="ulong.MaxValue" />, which no player has.
        /// </remarks>
        public void Send<T>(T model, ulong recipientId, bool reliable) where T : struct
        {
            var typedDataModel = ModelFor<T>();
            // To DataModelInfo.Send, 0 means everyone and ulong.MaxValue the server; neither is a player, so here
            // nothing goes out.
            if (recipientId == 0UL || recipientId == ulong.MaxValue)
                return;
            typedDataModel.Send(_channelId, ref model, recipientId, reliable);
        }

        /// <summary>
        ///     Broadcasts a message of type <typeparamref name="T" /> to all other players (excludes the sender).
        ///     The type must have been previously registered with <see cref="Builder.UsingModel{TModel}" />.
        /// </summary>
        /// <typeparam name="T">The struct data model type to broadcast.</typeparam>
        /// <param name="model">The message data to send.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
        /// <param name="includingSelf">If true, also sends the message to the sender; if false, excludes the sender.</param>
        /// <exception cref="InvalidOperationException">Thrown if the type is not registered on this channel.</exception>
        /// <remarks>
        ///     On a client, the game passes the broadcast on to the other players by way of the server, and they receive it
        ///     marked as coming from the server. The client's own copy, when <paramref name="includingSelf" /> is true, also
        ///     comes back through the server: a round trip later, marked as coming from the server, as the others see it.
        /// </remarks>
        public void Broadcast<T>(T model, bool reliable, bool includingSelf = false) where T : struct
        {
            var typedDataModel = ModelFor<T>();
            typedDataModel.Send(_channelId, ref model, 0UL, reliable, includingSelf);
        }

        /// <summary>
        ///     Sends a message of type <typeparamref name="T" /> to the server.
        ///     The type must have been previously registered with <see cref="Builder.UsingModel{TModel}" />.
        /// </summary>
        /// <typeparam name="T">The struct data model type to send.</typeparam>
        /// <param name="model">The message data to send.</param>
        /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
        /// <exception cref="InvalidOperationException">Thrown if the type is not registered on this channel.</exception>
        public void SendToServer<T>(T model, bool reliable) where T : struct
        {
            var typedDataModel = ModelFor<T>();
            typedDataModel.Send(_channelId, ref model, ulong.MaxValue, reliable);
        }

        /// <summary>
        ///     Default implementation of <see cref="INetworkProxy" /> that uses the Space Engineers Multiplayer API.
        ///     Provides access to server/client status and message handler registration.
        /// </summary>
        public class DefaultNetwork : INetworkProxy
        {
            /// <summary>
            ///     True when running as a dedicated/headless server instance.
            /// </summary>
            public bool IsHeadlessServer => MyAPIGateway.Utilities.IsDedicated;

            /// <summary>
            ///     True when this process is acting as the multiplayer server.
            /// </summary>
            public bool IsServer => MyAPIGateway.Multiplayer.IsServer;

            /// <summary>
            ///     True when this process is a multiplayer client (not a dedicated server).
            ///     A hosted multiplayer server is both a server and a client.
            /// </summary>
            public bool IsClient => !MyAPIGateway.Utilities.IsDedicated;

            /// <summary>
            ///     Registers a secure message handler for the specified channel ID.
            /// </summary>
            /// <param name="id">The channel identifier.</param>
            /// <param name="messageHandler">The callback to invoke when messages are received on this channel.</param>
            public void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(id, messageHandler);
            }

            /// <summary>
            ///     Unregisters a previously registered secure message handler for the specified channel ID.
            /// </summary>
            /// <param name="id">The channel identifier.</param>
            /// <param name="messageHandler">The callback to unregister.</param>
            public void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(id, messageHandler);
            }

            /// <summary>
            ///     Sends a message to the specified recipient.
            /// </summary>
            /// <param name="channelId">The channel identifier.</param>
            /// <param name="data">The message data to send.</param>
            /// <param name="recipientId">The Steam ID of the recipient player.</param>
            /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
            public void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(channelId, data, recipientId, reliable);
            }

            /// <summary>
            ///     Broadcasts a message to all other players (excludes the sender).
            /// </summary>
            /// <param name="channelId">The channel identifier.</param>
            /// <param name="data">The message data to send.</param>
            /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
            public void SendMessageToOthers(ushort channelId, byte[] data, bool reliable)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(channelId, data, reliable);
            }

            /// <summary>
            ///     Sends a message to the server.
            /// </summary>
            /// <param name="channelId">The channel identifier.</param>
            /// <param name="data">The message data to send.</param>
            /// <param name="reliable">If true, ensures delivery; if false, allows packet loss for better performance.</param>
            public void SendMessageToServer(ushort channelId, byte[] data, bool reliable)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(channelId, data, reliable);
            }

            /// <summary>
            ///     Adds every player in the world to <paramref name="players" />; nothing where the game has no players.
            /// </summary>
            /// <param name="players">The list to add the players to.</param>
            public void GetPlayers(List<IMyPlayer> players)
            {
                if (MyAPIGateway.Players == null)
                    return;
                MyAPIGateway.Players.GetPlayers(players);
            }
        }

        interface IDataModelInfo
        {
            int TypeId { get; }
            Type Type { get; }

            void DidReceive(NetworkChannel network, ushort channelId, byte[] data, ref int offset, ulong senderId, bool fromServer);
        }

        class DataModelInfo<TModel> : IDataModelInfo where TModel : struct
        {
            readonly object _bufferLock = new object();
            readonly List<DataModelReceivedFn<TModel>> _handlers;
            // Listeners added (true) or removed (false) while messages were being delivered, applied once the
            // outermost delivery is done. Reused, so it only ever grows to the most changes made in one delivery.
            readonly List<KeyValuePair<DataModelReceivedFn<TModel>, bool>> _pending = new List<KeyValuePair<DataModelReceivedFn<TModel>, bool>>();
            int _delivering;
            readonly ReadDataModelFn<TModel> _readFn;
            readonly SizeOfDataModelFn<TModel> _sizeOfFn;
            readonly WriteDataModelFn<TModel> _writeFn;
            byte[] _buffer;

            public DataModelInfo(Type type, SizeOfDataModelFn<TModel> sizeOfFn, ReadDataModelFn<TModel> readFn, WriteDataModelFn<TModel> writeFn)
            {
                Type = type;
                TypeId = IdOf(type.FullName);
                _sizeOfFn = sizeOfFn;
                _readFn = readFn;
                _writeFn = writeFn;
                _handlers = new List<DataModelReceivedFn<TModel>>();
            }

            public int TypeId { get; }
            public Type Type { get; }

            // Reading network input handles its own exceptions: a read or a listener that throws is logged and
            // dropped, and the other listeners still get the message. Listeners added or removed during delivery
            // take effect from the next message, so the list is never changed while it is being walked; a
            // listener may cause a nested delivery, so changes wait for the outermost one.
            public void DidReceive(NetworkChannel network, ushort channelId, byte[] data, ref int offset, ulong senderId, bool fromServer)
            {
                TModel msg;
                try
                {
                    _readFn(data, ref offset, out msg);
                }
                catch (Exception ex)
                {
                    Warn($"NetworkChannel dropped a {Type.Name} from {senderId} on channel {channelId}: reading it failed: {ex.Message}");
                    return;
                }

                _delivering++;
                try
                {
                    foreach (var handler in _handlers)
                    {
                        try
                        {
                            handler(channelId, msg, senderId, fromServer);
                        }
                        catch (Exception ex)
                        {
                            Warn($"NetworkChannel: a listener for {Type.Name} from {senderId} on channel {channelId} failed: {ex.Message}");
                        }
                    }
                }
                finally
                {
                    _delivering--;
                    if (_delivering == 0 && _pending.Count > 0)
                        ApplyPending();
                }
            }

            public void AddListener(DataModelReceivedFn<TModel> handler)
            {
                if (_delivering > 0)
                    _pending.Add(new KeyValuePair<DataModelReceivedFn<TModel>, bool>(handler, true));
                else
                    _handlers.Add(handler);
            }

            public void RemoveListener(DataModelReceivedFn<TModel> handler)
            {
                if (_delivering > 0)
                    _pending.Add(new KeyValuePair<DataModelReceivedFn<TModel>, bool>(handler, false));
                else
                    _handlers.Remove(handler);
            }

            void ApplyPending()
            {
                for (var i = 0; i < _pending.Count; i++)
                {
                    if (_pending[i].Value)
                        _handlers.Add(_pending[i].Key);
                    else
                        _handlers.Remove(_pending[i].Key);
                }
                _pending.Clear();
            }

            // The game's log exists only inside the game, which a test is not.
            static void Warn(string message)
            {
                if (MyLog.Default != null)
                    MyLog.Default.Warning(message);
            }

            // Serialized size of the [typeId][payload] "inner" message - what a recipient dispatches.
            public int InnerSize(ref TModel model)
            {
                return _sizeOfFn(ref model) + 4;
            }

            // Writes the [typeId][payload] "inner" message into data at offset: the one place that layout is
            // written, for normal and nearby sends alike.
            public void WriteInner(ref TModel model, byte[] data, ref int offset)
            {
                ByteData.Write(TypeId, data, ref offset);
                _writeFn(ref model, data, ref offset);
            }

            public void Send(ushort channelId, ref TModel model, ulong recipientId, bool reliable, bool includingSelf = false)
            {
                lock (_bufferLock)
                {
                    var size = InnerSize(ref model);
                    if (_buffer == null || _buffer.Length < size)
                        _buffer = new byte[size];
                    var offset = 0;
                    WriteInner(ref model, _buffer, ref offset);

                    switch (recipientId)
                    {
                        case 0UL:
                            Network.SendMessageToOthers(channelId, _buffer, reliable);
                            if (includingSelf)
                                SendToSelf(channelId, _buffer, reliable);

                            break;
                        case ulong.MaxValue:
                            Network.SendMessageToServer(channelId, _buffer, reliable);
                            break;
                        default:
                            Network.SendMessageTo(channelId, _buffer, recipientId, reliable);
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     Fluent builder for registering struct-based data models and creating a <see cref="NetworkChannel" />.
        ///     Models are registered with custom serialization delegates for size calculation, reading, and writing.
        /// </summary>
        public struct Builder
        {
            readonly ushort _channelId;
            readonly Dictionary<int, IDataModelInfo> _registeredTypes;

            /// <summary>
            ///     Initializes a new <see cref="Builder" /> for the specified channel identifier.
            /// </summary>
            /// <param name="channelId">The 16-bit channel identifier to use when the channel is built.</param>
            public Builder(ushort channelId)
            {
                _channelId = channelId;
                _registeredTypes = new Dictionary<int, IDataModelInfo>();
            }

            Builder(ushort channelId, Dictionary<int, IDataModelInfo> registeredTypes)
            {
                _channelId = channelId;
                _registeredTypes = registeredTypes;
            }

            /// <summary>
            ///     Registers a struct-based model type with custom serialization delegates.
            /// </summary>
            /// <typeparam name="TModel">The struct data model type to register.</typeparam>
            /// <param name="sizeOfFn">Delegate that calculates the serialized size in bytes for a given model instance.</param>
            /// <param name="readFn">Delegate that deserializes bytes into a model instance.</param>
            /// <param name="writeFn">Delegate that serializes a model instance into bytes.</param>
            /// <returns>A new <see cref="Builder" /> instance with the model registered (fluent API).</returns>
            /// <exception cref="InvalidOperationException">
            ///     Thrown if the type conflicts with another registered type or is already registered on this channel.
            /// </exception>
            public Builder UsingModel<TModel>(SizeOfDataModelFn<TModel> sizeOfFn, ReadDataModelFn<TModel> readFn, WriteDataModelFn<TModel> writeFn) where TModel : struct
            {
                var type = typeof(TModel);
                var typeId = IdOf(type.FullName);
                IDataModelInfo existingType;
                if (_registeredTypes.TryGetValue(typeId, out existingType))
                {
                    if (existingType.Type != type)
                        throw new InvalidOperationException($"Type ID conflict: {type.FullName} and {existingType.Type.FullName} have the same type ID {typeId}.");
                    throw new InvalidOperationException($"Type {type.FullName} is already registered on this channel.");
                }

                var dataModelInfo = new DataModelInfo<TModel>(type, sizeOfFn, readFn, writeFn);
                _registeredTypes.Add(typeId, dataModelInfo);
                return new Builder(_channelId, _registeredTypes);
            }

            /// <summary>
            ///     Validates the builder state and returns a configured <see cref="NetworkChannel" /> instance.
            /// </summary>
            /// <returns>A constructed <see cref="NetworkChannel" />.</returns>
            /// <exception cref="InvalidOperationException">
            ///     Thrown when the builder was not properly initialized, the channel ID is
            ///     zero, or no types were registered.
            /// </exception>
            public NetworkChannel Build()
            {
                if (_channelId == 0)
                    throw new InvalidOperationException("Channel ID cannot be zero.");
                if (_registeredTypes == null)
                    throw new InvalidOperationException("This builder was not properly initialized.");
                if (_registeredTypes.Count == 0)
                    throw new InvalidOperationException("No types registered for this channel.");
                return new NetworkChannel(_channelId, _registeredTypes);
            }
        }
    }
}
