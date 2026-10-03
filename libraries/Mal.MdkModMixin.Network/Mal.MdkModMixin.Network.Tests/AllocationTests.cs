using System;
using System.Text;
using NUnit.Framework;
using VRageMath;

namespace Mal.MdkModMixin.Network.Tests
{
    /// <summary>Sending and writing allocate nothing once their kept buffers are big enough.</summary>
    [TestFixture]
    public class AllocationTests
    {
        const int Rounds = 100;

        INetworkProxy _previous;

        [SetUp]
        public void SetUp()
        {
            _previous = NetworkChannel.Network;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkChannel.Network = _previous;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SendingToTheServerAndNearbyFromAClientAllocatesNothing(bool nearby)
        {
            NetworkChannel.Network = new FakeProxy(false) { Records = false };
            var channel = NetworkChannel.Create("Mal.MdkModMixin.Network.Tests.Allocation")
                .UsingModel<Ping>(Ping.SizeOf, Ping.Read, Ping.Write)
                .Build();
            try
            {
                var msg = new Ping { Number = 7, Text = "a message with some text in it" };
                Send(channel, msg, nearby);

                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < Rounds; i++)
                    Send(channel, msg, nearby);
                Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L));
            }
            finally
            {
                channel.Dispose();
            }
        }

        static void Send(NetworkChannel channel, Ping msg, bool nearby)
        {
            if (nearby)
                channel.SendToNearby(msg, Vector3D.Zero, 50, true);
            else
                channel.SendToServer(msg, true);
        }

        [Test]
        public void WritingStringsAllocatesNothing()
        {
            const string text = "Text with é, € and \U0001D11E in it.";
            var builder = new StringBuilder(text);
            var data = new byte[1024];
            var pos = 0;
            ByteData.WriteString(text, data, ref pos);
            ByteData.WriteString(builder, data, ref pos);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < Rounds; i++)
            {
                pos = 0;
                ByteData.WriteString(text, data, ref pos);
                ByteData.WriteString(builder, data, ref pos);
                ByteData.SizeOfString(text);
                ByteData.SizeOfString(builder);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L));
        }

        [Test]
        public void ReadingAStringIntoAStringBuilderAllocatesNothingOnceItHasRoom()
        {
            const string text = "Text with é, € and \U0001D11E in it.";
            var data = new byte[ByteData.SizeOfString(text)];
            var pos = 0;
            ByteData.WriteString(text, data, ref pos);
            var builder = new StringBuilder();
            pos = 0;
            ByteData.TryReadString(data, ref pos, builder);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < Rounds; i++)
            {
                builder.Clear();
                pos = 0;
                ByteData.TryReadString(data, ref pos, builder);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L));
        }
    }
}
