using System;
using System.Text;
using NUnit.Framework;
using VRageMath;

namespace Mal.MdkModMixin.Network.Tests
{
    [TestFixture]
    public class ByteDataTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void Int32RoundTrips(int value)
        {
            var data = new byte[4];
            var pos = 0;
            ByteData.Write(value, data, ref pos);
            pos = 0;
            int read;
            Assert.That(ByteData.TryReadInt32(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));
        }

        [TestCase(0L)]
        [TestCase(long.MaxValue)]
        [TestCase(long.MinValue)]
        public void Int64RoundTrips(long value)
        {
            var data = new byte[8];
            var pos = 0;
            ByteData.Write(value, data, ref pos);
            pos = 0;
            long read;
            Assert.That(ByteData.TryReadInt64(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));
        }

        [TestCase(0f)]
        [TestCase(-1.5f)]
        [TestCase(float.MaxValue)]
        [TestCase(float.Epsilon)]
        public void FloatRoundTripsAsTheRuntimesBytes(float value)
        {
            var data = new byte[4];
            var pos = 0;
            ByteData.Write(value, data, ref pos);
            Assert.That(data, Is.EqualTo(BitConverter.GetBytes(value)));
            pos = 0;
            float read;
            Assert.That(ByteData.TryReadFloat(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));
        }

        [TestCase(0.0)]
        [TestCase(-123.456)]
        [TestCase(double.MaxValue)]
        public void DoubleRoundTripsAsTheRuntimesBytes(double value)
        {
            var data = new byte[8];
            var pos = 0;
            ByteData.Write(value, data, ref pos);
            Assert.That(data, Is.EqualTo(BitConverter.GetBytes(value)));
            pos = 0;
            double read;
            Assert.That(ByteData.TryReadDouble(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));
        }

        [Test]
        public void VectorRoundTrips()
        {
            var value = new Vector3D(1.25, -2.5, 1e9);
            var data = new byte[24];
            var pos = 0;
            ByteData.Write(value, data, ref pos);
            pos = 0;
            Vector3D read;
            Assert.That(ByteData.TryReadVector3D(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));
        }

        [TestCase("")]
        [TestCase("Floor 3: Hangar")]
        [TestCase("Ünïcödé ✓ 電梯")]
        public void StringRoundTrips(string value)
        {
            var data = new byte[ByteData.SizeOfString(value)];
            var pos = 0;
            ByteData.WriteString(value, data, ref pos);
            pos = 0;
            string read;
            Assert.That(ByteData.TryReadString(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));
        }

        [Test]
        public void AStringClaimingTheLargestLengthIsRefusedNotThrown()
        {
            var data = new byte[12];
            var pos = 4;
            ByteData.Write(int.MaxValue, data, ref pos);
            pos = 4;
            string read;

            Assert.That(ByteData.TryReadString(data, ref pos, out read), Is.False);
        }

        [Test]
        public void AStringClaimingTheLargestLengthIsRefusedNotThrownIntoAStringBuilder()
        {
            var data = new byte[12];
            var pos = 4;
            ByteData.Write(int.MaxValue, data, ref pos);
            pos = 4;

            Assert.That(ByteData.TryReadString(data, ref pos, new StringBuilder()), Is.False);
        }

        [TestCase(0UL)]
        [TestCase(ulong.MaxValue)]
        [TestCase(76561198000000000UL)]
        public void UInt64RoundTripsAsEightBytes(ulong value)
        {
            var data = new byte[8];
            var pos = 0;
            ByteData.Write(value, data, ref pos);
            Assert.That(pos, Is.EqualTo(8));
            pos = 0;
            ulong read;
            Assert.That(ByteData.TryReadUInt64(data, ref pos, out read), Is.True);
            Assert.That(read, Is.EqualTo(value));

            // The same bytes as the signed 64-bit writer, so either reader can take them.
            var asLong = new byte[8];
            pos = 0;
            ByteData.Write(unchecked((long)value), asLong, ref pos);
            Assert.That(data, Is.EqualTo(asLong));
        }

        // Longer than the reader's 1024-byte chunks, in characters of one, two, three and four bytes. The prefixed
        // cases make the first chunk end 1, 2 and 3 bytes into a 4-byte character, so every back-off is taken.
        [TestCase(0, "a", 2000)]
        [TestCase(0, "\u00e9", 1500)]
        [TestCase(0, "\u20ac", 800)]
        [TestCase(0, "a\u00e9\u20ac\U0001D11E", 400)]
        [TestCase(1021, "\U0001D11E", 300)]
        [TestCase(1022, "\U0001D11E", 300)]
        [TestCase(1023, "\U0001D11E", 300)]
        public void ALongStringRoundTripsIntoAStringBuilder(int prefix, string unit, int repeats)
        {
            var text = new StringBuilder();
            text.Append('a', prefix);
            for (var i = 0; i < repeats; i++)
                text.Append(unit);
            var data = new byte[4 + Encoding.UTF8.GetByteCount(text.ToString())];
            var pos = 0;
            ByteData.WriteString(text.ToString(), data, ref pos);
            pos = 0;

            var read = new StringBuilder();
            Assert.That(ByteData.TryReadString(data, ref pos, read), Is.True);
            Assert.That(read.ToString(), Is.EqualTo(text.ToString()));
            Assert.That(pos, Is.EqualTo(data.Length));
        }

        // The StringBuilder writer encodes 1024 chars at a time; these put a surrogate pair (a 4-byte character)
        // across, just before and just after that boundary, and write the same bytes as the string writer.
        [TestCase(1022)]
        [TestCase(1023)]
        [TestCase(1024)]
        [TestCase(2047)]
        public void AStringBuilderWritesASurrogatePairAtAChunkBoundaryWhole(int prefix)
        {
            var text = new StringBuilder();
            text.Append('a', prefix);
            for (var i = 0; i < 600; i++)
                text.Append("\U0001D11E");
            var fromString = new byte[4 + Encoding.UTF8.GetByteCount(text.ToString())];
            var fromBuilder = new byte[fromString.Length];
            var pos = 0;
            ByteData.WriteString(text.ToString(), fromString, ref pos);
            pos = 0;
            ByteData.WriteString(text, fromBuilder, ref pos);
            Assert.That(pos, Is.EqualTo(fromBuilder.Length));
            Assert.That(fromBuilder, Is.EqualTo(fromString));

            pos = 0;
            var read = new StringBuilder();
            Assert.That(ByteData.TryReadString(fromBuilder, ref pos, read), Is.True);
            Assert.That(read.ToString(), Is.EqualTo(text.ToString()));
        }

        // Both writers write null and empty alike, and both readers read them alike.
        [TestCase(true)]
        [TestCase(false)]
        public void NullAndEmptyAreTheSameWhicheverWriterWroteThem(bool empty)
        {
            var fromString = new byte[4];
            var fromBuilder = new byte[4];
            var pos = 0;
            ByteData.WriteString(empty ? "" : null, fromString, ref pos);
            pos = 0;
            ByteData.WriteString(empty ? new StringBuilder() : null, fromBuilder, ref pos);
            Assert.That(fromBuilder, Is.EqualTo(fromString));

            pos = 0;
            string asString;
            Assert.That(ByteData.TryReadString(fromBuilder, ref pos, out asString), Is.True);
            Assert.That(asString, Is.EqualTo(empty ? "" : null));
            Assert.That(pos, Is.EqualTo(4));

            pos = 0;
            var asBuilder = new StringBuilder("kept");
            Assert.That(ByteData.TryReadString(fromString, ref pos, asBuilder), Is.True);
            Assert.That(asBuilder.ToString(), Is.EqualTo("kept"));
            Assert.That(pos, Is.EqualTo(4));
        }

        // Nothing but continuation bytes: no character boundary to back a chunk off to.
        [Test]
        public void ALongRunOfBrokenUtf8IsReadAsReplacementsWithoutHangingOrThrowing()
        {
            var data = new byte[4 + 3000];
            var pos = 0;
            ByteData.Write(3000, data, ref pos);
            for (var i = 4; i < data.Length; i++)
                data[i] = 0x80;
            pos = 0;

            var read = new StringBuilder();
            Assert.That(ByteData.TryReadString(data, ref pos, read), Is.True);
            Assert.That(read.Length, Is.EqualTo(3000));
            Assert.That(read.ToString().Trim('\uFFFD'), Is.Empty);
            Assert.That(pos, Is.EqualTo(data.Length));
        }

        [Test]
        public void AUInt64IsRefusedFromTooFewBytesWithoutMoving()
        {
            var data = new byte[7];
            var pos = 0;
            ulong read;
            Assert.That(ByteData.TryReadUInt64(data, ref pos, out read), Is.False);
            Assert.That(pos, Is.EqualTo(0));
        }

        // Only -1 means null; any other negative length is corrupt and refused, by both readers.
        [TestCase(-2)]
        [TestCase(-7)]
        [TestCase(int.MinValue)]
        public void ANegativeLengthOtherThanMinusOneIsRefused(int length)
        {
            var data = new byte[16];
            var pos = 0;
            ByteData.Write(length, data, ref pos);

            pos = 0;
            string asString;
            Assert.That(ByteData.TryReadString(data, ref pos, out asString), Is.False);
            pos = 0;
            Assert.That(ByteData.TryReadString(data, ref pos, new StringBuilder()), Is.False);
        }

        [Test]
        public void ALongStringIsAppendedToWhatTheStringBuilderAlreadyHolds()
        {
            var text = new string('x', 3000) + "\u20ac\U0001D11E";
            var data = new byte[ByteData.SizeOfString(text)];
            var pos = 0;
            ByteData.WriteString(text, data, ref pos);
            pos = 0;

            var read = new StringBuilder("before:");
            Assert.That(ByteData.TryReadString(data, ref pos, read), Is.True);
            Assert.That(read.ToString(), Is.EqualTo("before:" + text));
        }

        // The size the StringBuilder writer is given must be exactly what it writes, chunk boundaries and all.
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(1023)]
        [TestCase(5000)]
        public void TheSizeOfAStringBuilderIsWhatItsWriterWrites(int prefix)
        {
            var text = new StringBuilder();
            text.Append('a', prefix);
            text.Append("\u00e9\u20ac\U0001D11E");
            var data = new byte[ByteData.SizeOfString(text)];
            var pos = 0;
            ByteData.WriteString(text, data, ref pos);
            Assert.That(pos, Is.EqualTo(data.Length));
            Assert.That(data.Length, Is.EqualTo(ByteData.SizeOfString(text.ToString())));
        }

        [Test]
        public void TheSizeOfANullOrEmptyStringBuilderIsItsLengthAlone()
        {
            Assert.That(ByteData.SizeOfString((StringBuilder)null), Is.EqualTo(4));
            Assert.That(ByteData.SizeOfString(new StringBuilder()), Is.EqualTo(4));
        }
    }
}
