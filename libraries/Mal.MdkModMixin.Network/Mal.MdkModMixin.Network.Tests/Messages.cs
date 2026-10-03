using System;

namespace Mal.MdkModMixin.Network.Tests
{
    /// <summary>A test message: a number and some text.</summary>
    public struct Ping
    {
        public int Number;
        public string Text;

        public static int SizeOf(ref Ping msg)
        {
            return sizeof(int) + ByteData.SizeOfString(msg.Text);
        }

        public static int Write(ref Ping msg, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(msg.Number, data, ref offset);
            ByteData.WriteString(msg.Text, data, ref offset);
            return offset - start;
        }

        public static int Read(byte[] data, ref int offset, out Ping msg)
        {
            var start = offset;
            msg = new Ping();
            ByteData.TryReadInt32(data, ref offset, out msg.Number);
            ByteData.TryReadString(data, ref offset, out msg.Text);
            return offset - start;
        }
    }

    /// <summary>A test message whose reading always fails by throwing.</summary>
    public struct Faulty
    {
        public int Number;

        public static int SizeOf(ref Faulty msg)
        {
            return sizeof(int);
        }

        public static int Write(ref Faulty msg, byte[] data, ref int offset)
        {
            ByteData.Write(msg.Number, data, ref offset);
            return sizeof(int);
        }

        public static int Read(byte[] data, ref int offset, out Faulty msg)
        {
            throw new InvalidOperationException("reading failed on purpose");
        }
    }
}
