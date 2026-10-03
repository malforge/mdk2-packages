using System;
using System.Collections.Generic;
using FakeItEasy;
using NUnit.Framework;
using VRage.Game.ModAPI;
using VRageMath;

namespace Mal.MdkModMixin.Network.Tests
{
    [TestFixture]
    public class ChannelTests
    {
        const ushort ChannelId = 4242;

        FakeProxy _proxy;
        INetworkProxy _previous;
        NetworkChannel _channel;

        [SetUp]
        public void SetUp()
        {
            _previous = NetworkChannel.Network;
            _proxy = new FakeProxy(true);
            NetworkChannel.Network = _proxy;
            _channel = NetworkChannel.Create(ChannelId)
                .UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write)
                .UsingModel<Faulty>(Faulty.SizeOf, Faulty.Read, Faulty.Write)
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            _channel.Dispose();
            NetworkChannel.Network = _previous;
        }

        static IMyPlayer Player(ulong steamId, Vector3D position, bool bot = false, bool hasBody = true)
        {
            var player = A.Fake<IMyPlayer>();
            A.CallTo(() => player.SteamUserId).Returns(steamId);
            A.CallTo(() => player.GetPosition()).Returns(position);
            A.CallTo(() => player.IsBot).Returns(bot);
            if (!hasBody)
                A.CallTo(() => player.Character).Returns(null);
            return player;
        }

        [Test]
        public void AListenerGetsTheMessage()
        {
            var got = new List<Ping>();
            _channel.ListenFor<Ping>((id, msg, sender, fromServer) => got.Add(msg));

            _channel.SendToServer(new Ping { Number = 7, Text = "up" }, true);

            Assert.That(got, Has.Count.EqualTo(1));
            Assert.That(got[0].Number, Is.EqualTo(7));
            Assert.That(got[0].Text, Is.EqualTo("up"));
        }

        [Test]
        public void AListenerMayStopListeningDuringItsOwnCall()
        {
            var calls = 0;
            DataModelReceivedFn<Ping> once = null;
            once = (id, msg, sender, fromServer) =>
            {
                calls++;
                _channel.StopListeningFor(once);
            };
            var others = 0;
            _channel.ListenFor(once);
            _channel.ListenFor<Ping>((id, msg, sender, fromServer) => others++);

            Assert.DoesNotThrow(() => _channel.SendToServer(new Ping(), true));
            Assert.DoesNotThrow(() => _channel.SendToServer(new Ping(), true));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(others, Is.EqualTo(2));
        }

        [Test]
        public void AListenerAddedDuringACallHearsFromTheNextMessage()
        {
            var late = 0;
            var added = false;
            _channel.ListenFor<Ping>((id, msg, sender, fromServer) =>
            {
                if (added)
                    return;
                added = true;
                _channel.ListenFor<Ping>((i, m, s, f) => late++);
            });

            _channel.SendToServer(new Ping(), true);
            Assert.That(late, Is.EqualTo(0));
            _channel.SendToServer(new Ping(), true);
            Assert.That(late, Is.EqualTo(1));
        }

        [Test]
        public void ChangesDuringANestedDeliveryWaitForTheOutermostToFinish()
        {
            var inner = 0;
            var depth = 0;
            DataModelReceivedFn<Ping> nested = null;
            nested = (id, msg, sender, fromServer) =>
            {
                depth++;
                if (depth == 1)
                {
                    // Sends to itself on the server, which delivers at once: a nested delivery.
                    _channel.SendToServer(new Ping { Number = 2 }, true);
                    _channel.StopListeningFor(nested);
                }
            };
            _channel.ListenFor(nested);
            _channel.ListenFor<Ping>((id, msg, sender, fromServer) => inner++);

            Assert.DoesNotThrow(() => _channel.SendToServer(new Ping { Number = 1 }, true));
            Assert.That(depth, Is.EqualTo(2));
            Assert.That(inner, Is.EqualTo(2));

            _channel.SendToServer(new Ping(), true);
            Assert.That(depth, Is.EqualTo(2));
        }

        [Test]
        public void AListenerThatThrowsDoesNotStopTheOthersOrReachTheGame()
        {
            var others = 0;
            _channel.ListenFor<Ping>((id, msg, sender, fromServer) => { throw new InvalidOperationException("boom"); });
            _channel.ListenFor<Ping>((id, msg, sender, fromServer) => others++);

            Assert.DoesNotThrow(() => _channel.SendToServer(new Ping(), true));
            Assert.That(others, Is.EqualTo(1));
        }

        [Test]
        public void AReadThatThrowsIsDroppedWithoutReachingTheGame()
        {
            var heard = 0;
            _channel.ListenFor<Faulty>((id, msg, sender, fromServer) => heard++);

            Assert.DoesNotThrow(() => _channel.SendToServer(new Faulty(), true));
            Assert.That(heard, Is.EqualTo(0));
        }

        [Test]
        public void ANearbyMessageReachesOnlyRealPlayersWithABodyInRange()
        {
            _proxy.Players.Add(Player(10, new Vector3D(5, 0, 0)));
            _proxy.Players.Add(Player(11, new Vector3D(500, 0, 0)));
            _proxy.Players.Add(Player(12, Vector3D.Zero, bot: true));
            _proxy.Players.Add(Player(13, Vector3D.Zero, hasBody: false));

            _channel.SendToNearby(new Ping(), Vector3D.Zero, 50, true);

            Assert.That(_proxy.ToPlayers, Has.Count.EqualTo(1));
            Assert.That(_proxy.ToPlayers[0].Key, Is.EqualTo(10UL));
        }

        [Test]
        public void ANearbyMessageWithANegativeOrNaNRadiusOrANaNOriginReachesNoOne()
        {
            _proxy.Players.Add(Player(10, Vector3D.Zero));
            _channel.SendToNearby(new Ping(), Vector3D.Zero, -50, true);

            _channel.SendToNearby(new Ping(), Vector3D.Zero, double.NaN, true);
            _channel.SendToNearby(new Ping(), new Vector3D(double.NaN, 0, 0), 50, true);

            Assert.That(_proxy.ToPlayers, Is.Empty);
        }

        [Test]
        public void EachNearbyMessageSeesOnlyThePlayersInTheWorldAtTheTime()
        {
            _proxy.Players.Add(Player(10, Vector3D.Zero));
            _channel.SendToNearby(new Ping(), Vector3D.Zero, 50, true);

            _proxy.Players.Clear();
            _proxy.Players.Add(Player(11, Vector3D.Zero));
            _channel.SendToNearby(new Ping(), Vector3D.Zero, 50, true);

            Assert.That(_proxy.ToPlayers, Has.Count.EqualTo(2));
            Assert.That(_proxy.ToPlayers[0].Key, Is.EqualTo(10UL));
            Assert.That(_proxy.ToPlayers[1].Key, Is.EqualTo(11UL));
        }

        // A client broadcasting to itself while there is no session (loading or unloading) has no self to reach.
        [Test]
        public void AClientBroadcastIncludingItselfWithNoSessionSkipsItself()
        {
            var client = new FakeProxy(false);
            NetworkChannel.Network = client;
            var channel = NetworkChannel.Create("Mal.MdkModMixin.Network.Tests.Client")
                .UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write)
                .Build();
            try
            {
                Assert.DoesNotThrow(() => channel.Broadcast(new Ping { Number = 1 }, true, true));
                Assert.That(client.ToPlayers, Is.Empty);
            }
            finally
            {
                channel.Dispose();
                NetworkChannel.Network = _proxy;
            }
        }

        // The relay's send to the host is delivered on the spot, inside the relay. If the host's listener sends
        // nearby in turn, that inner relay must not cut the outer one short for the players after the host.
        [Test]
        public void ANearbySendFromAListenerDuringARelayStillReachesEveryone()
        {
            _proxy.Players.Add(Player(10, Vector3D.Zero));
            _proxy.Players.Add(Player(11, Vector3D.Zero));
            _proxy.Players.Add(Player(12, Vector3D.Zero));
            _proxy.DeliversOnTheSpotTo = 10;
            var answered = false;
            _channel.ListenFor<Ping>((channelId, msg, senderId, fromServer) =>
            {
                if (msg.Number != 1 || answered)
                    return;
                answered = true;
                _channel.SendToNearby(new Ping { Number = 2 }, Vector3D.Zero, 50, true);
            });

            _channel.SendToNearby(new Ping { Number = 1 }, Vector3D.Zero, 50, true);

            Assert.That(answered, Is.True);
            var first = new List<ulong>();
            var second = new List<ulong>();
            foreach (var sent in _proxy.ToPlayers)
                (NumberIn(sent.Value) == 1 ? first : second).Add(sent.Key);
            Assert.That(first, Is.EquivalentTo(new ulong[] { 10, 11, 12 }));
            Assert.That(second, Is.EquivalentTo(new ulong[] { 10, 11, 12 }));
        }

        static int NumberIn(byte[] data)
        {
            var pos = 4;
            Ping msg;
            Ping.Read(data, ref pos, out msg);
            return msg.Number;
        }

        // 0 is how the game says "no player"; to the channel's internals it means everyone.
        [Test]
        public void SendingToPlayerZeroSendsNothing()
        {
            _channel.Send(new Ping { Number = 1 }, 0UL, true);

            Assert.That(_proxy.ToOthers, Is.EqualTo(0));
            Assert.That(_proxy.ToPlayers, Is.Empty);
        }

        [Test]
        public void ANearbySendWithANegativeRadiusDoesNotReachTheSenderEither()
        {
            _proxy.Players.Add(Player(10, Vector3D.Zero));
            var heard = 0;
            _channel.ListenFor<Ping>((channelId, msg, senderId, fromServer) => heard++);

            _channel.SendToNearby(new Ping { Number = 1 }, Vector3D.Zero, -1, true, true);

            Assert.That(heard, Is.EqualTo(0));
            Assert.That(_proxy.ToPlayers, Is.Empty);
        }

        [Test]
        public void SendingToTheServersSpecialIdSendsNothing()
        {
            var heard = 0;
            _channel.ListenFor<Ping>((channelId, msg, senderId, fromServer) => heard++);

            _channel.Send(new Ping { Number = 1 }, ulong.MaxValue, true);

            Assert.That(heard, Is.EqualTo(0));
            Assert.That(_proxy.ToOthers, Is.EqualTo(0));
            Assert.That(_proxy.ToPlayers, Is.Empty);
        }

        // A builder used again after Build adds to itself, not to the channel it built.
        [Test]
        public void ABuilderUsedAgainDoesNotChangeTheChannelItBuilt()
        {
            var builder = NetworkChannel.Create("Mal.MdkModMixin.Network.Tests.Reused")
                .UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write);
            var channel = builder.Build();
            try
            {
                builder.UsingModel<Faulty>(Faulty.SizeOf, Faulty.Read, Faulty.Write);
                Assert.Throws<InvalidOperationException>(() => channel.ListenFor<Faulty>((channelId, msg, senderId, fromServer) => { }));
                Assert.DoesNotThrow(() => channel.ListenFor<Ping>((channelId, msg, senderId, fromServer) => { }));
            }
            finally
            {
                channel.Dispose();
            }
        }

        // A client writes its message straight into the envelope; handed to a server, the envelope relays exactly
        // that message to the players in range, minus the client that sent it.
        [Test]
        public void AClientsNearbyMessageReachesThePlayersInRangeThroughTheServer()
        {
            var client = new FakeProxy(false);
            NetworkChannel.Network = client;
            var clientChannel = NetworkChannel.Create("Mal.MdkModMixin.Network.Tests.Relay")
                .UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write)
                .Build();
            byte[] envelope;
            try
            {
                clientChannel.SendToNearby(new Ping { Number = 42, Text = "near" }, new Vector3D(100, 0, 0), 10, true);
                Assert.That(client.ToServer, Has.Count.EqualTo(1));
                envelope = client.ToServer[0];
            }
            finally
            {
                clientChannel.Dispose();
                NetworkChannel.Network = _proxy;
            }

            _proxy.Players.Add(Player(20, new Vector3D(105, 0, 0)));
            _proxy.Players.Add(Player(21, new Vector3D(200, 0, 0)));
            _proxy.Players.Add(Player(22, new Vector3D(100, 0, 0)));
            _proxy.Arrive(ChannelId, envelope, 22, false);

            Assert.That(_proxy.ToPlayers, Has.Count.EqualTo(1));
            Assert.That(_proxy.ToPlayers[0].Key, Is.EqualTo(20UL));
            var pos = 4;
            Ping relayed;
            Ping.Read(_proxy.ToPlayers[0].Value, ref pos, out relayed);
            Assert.That(relayed.Number, Is.EqualTo(42));
            Assert.That(relayed.Text, Is.EqualTo("near"));
        }

        // The nesting the game really produces: a client's envelope relayed by the server reaches the host's own
        // player on the spot, and the host's listener sends nearby in turn while the relay is still going.
        [Test]
        public void ANearbySendFromTheHostDuringARelayOfAClientsMessageStillReachesEveryone()
        {
            var client = new FakeProxy(false);
            NetworkChannel.Network = client;
            var clientChannel = NetworkChannel.Create("Mal.MdkModMixin.Network.Tests.Nested")
                .UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write)
                .Build();
            byte[] envelope;
            try
            {
                clientChannel.SendToNearby(new Ping { Number = 1 }, Vector3D.Zero, 50, true);
                envelope = client.ToServer[0];
            }
            finally
            {
                clientChannel.Dispose();
                NetworkChannel.Network = _proxy;
            }

            _proxy.Players.Add(Player(10, Vector3D.Zero));
            _proxy.Players.Add(Player(11, Vector3D.Zero));
            _proxy.Players.Add(Player(12, Vector3D.Zero));
            _proxy.DeliversOnTheSpotTo = 10;
            var answered = false;
            _channel.ListenFor<Ping>((channelId, msg, senderId, fromServer) =>
            {
                if (msg.Number != 1 || answered)
                    return;
                answered = true;
                _channel.SendToNearby(new Ping { Number = 2 }, Vector3D.Zero, 50, true);
            });

            _proxy.Arrive(ChannelId, envelope, 99, false);

            Assert.That(answered, Is.True);
            var first = new List<ulong>();
            var second = new List<ulong>();
            foreach (var sent in _proxy.ToPlayers)
                (NumberIn(sent.Value) == 1 ? first : second).Add(sent.Key);
            Assert.That(first, Is.EquivalentTo(new ulong[] { 10, 11, 12 }));
            Assert.That(second, Is.EquivalentTo(new ulong[] { 10, 11, 12 }));
        }

        [Test]
        public void ANearbySendFromANaNOrInfiniteOriginDoesNotReachTheSenderEither()
        {
            var heard = 0;
            _channel.ListenFor<Ping>((channelId, msg, senderId, fromServer) => heard++);

            _channel.SendToNearby(new Ping(), new Vector3D(double.NaN, 0, 0), 50, true, true);
            _channel.SendToNearby(new Ping(), new Vector3D(0, double.PositiveInfinity, 0), 50, true, true);

            Assert.That(heard, Is.EqualTo(0));
        }
    }
}
