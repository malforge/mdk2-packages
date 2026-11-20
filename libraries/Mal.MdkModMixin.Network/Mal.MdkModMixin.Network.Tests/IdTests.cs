using NUnit.Framework;

namespace Mal.MdkModMixin.Network.Tests
{
    [TestFixture]
    public class IdTests
    {
        INetworkProxy _previous;
        FakeProxy _proxy;

        [SetUp]
        public void SetUp()
        {
            _previous = NetworkChannel.Network;
            _proxy = new FakeProxy(false);
            NetworkChannel.Network = _proxy;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkChannel.Network = _previous;
        }

        // Reference values for 32-bit FNV-1a, from the test vectors Fowler, Noll and Vo publish. For ASCII text one
        // UTF-16 code unit is one byte's step, so they hold for these strings. Fixed numbers, because a hash that
        // differs between processes would still agree with itself inside one test run.
        [TestCase("", unchecked((int)0x811C9DC5u))]
        [TestCase("a", unchecked((int)0xE40C292Cu))]
        [TestCase("foobar", unchecked((int)0xBF9CF968u))]
        public void AnIdIsTheFnv1aHashOfTheText(string text, int expected)
        {
            Assert.That(NetworkChannel.IdOf(text), Is.EqualTo(expected));
        }

        [Test]
        public void AChannelMadeFromATypeUsesTheIdOfItsFullName()
        {
            using (NetworkChannel.Create(typeof(Ping)).UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write).Build())
                Assert.That(_proxy.Registered, Is.EqualTo(new[] { (ushort)(NetworkChannel.IdOf(typeof(Ping).FullName) & 0xFFFF) }));
        }

        [Test]
        public void AChannelMadeFromANameUsesTheIdOfTheName()
        {
            using (NetworkChannel.Create("foobar").UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write).Build())
                Assert.That(_proxy.Registered, Is.EqualTo(new[] { (ushort)0xF968 }));
        }

        [Test]
        public void AMessageStartsWithTheIdOfItsTypesFullName()
        {
            using (var channel = NetworkChannel.Create((ushort)4242).UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write).Build())
                channel.SendToServer(new Ping { Number = 1, Text = "" }, true);

            var sent = _proxy.ToServer[0];
            var offset = 0;
            int typeId;
            ByteData.TryReadInt32(sent, ref offset, out typeId);
            Assert.That(typeId, Is.EqualTo(NetworkChannel.IdOf(typeof(Ping).FullName)));
        }
    }
}
