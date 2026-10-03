using System;
using System.Text;
using VRageMath;

namespace Mal.MdkModMixin.Network
{
    /// <summary>
    ///     Utility class for reading and writing primitive types and vectors to/from byte arrays.
    ///     Uses little-endian byte order and thread-safe buffer access for floating-point conversions.
    /// </summary>
    public static class ByteData
    {
        static readonly double[] DoubleBuffer = new double[1];
        static readonly float[] FloatBuffer = new float[1];
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, false);
        static readonly char[] CharBuffer = new char[1024]; // Reusable buffer for StringBuilder char extraction

        /// <summary>
        ///     Writes a 32-bit unsigned integer to the byte array using little-endian byte order.
        /// </summary>
        /// <param name="value">The <see cref="uint" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 upon return.</param>
        public static void Write(uint value, byte[] data, ref int pos)
        {
            // Get individual bytes (not through BitConverter to avoid endianness issues)
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>
        ///     Attempts to read a 32-bit unsigned integer from the byte array using little-endian byte order.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 upon success.</param>
        /// <param name="value">The read value, or 0 if the read fails.</param>
        /// <returns>True if at least 4 bytes were available; false otherwise.</returns>
        public static bool TryReadUInt32(byte[] data, ref int pos, out uint value)
        {
            if (pos + 4 > data.Length)
            {
                value = 0;
                return false;
            }

            value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
            pos += 4;
            return true;
        }

        /// <summary>
        ///     Writes a 32-bit signed integer to the byte array using little-endian byte order.
        /// </summary>
        /// <param name="value">The <see cref="int" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 upon return.</param>
        public static void Write(int value, byte[] data, ref int pos)
        {
            // Get individual bytes (not through BitConverter to avoid endianness issues)
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>
        ///     Writes a 64-bit signed integer to the byte array using little-endian byte order.
        /// </summary>
        /// <param name="value">The <see cref="long" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 8 upon return.</param>
        public static void Write(long value, byte[] data, ref int pos)
        {
            // Get individual bytes (not through BitConverter to avoid endianness issues)
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
            data[pos++] = (byte)((value >> 32) & 0xFF);
            data[pos++] = (byte)((value >> 40) & 0xFF);
            data[pos++] = (byte)((value >> 48) & 0xFF);
            data[pos++] = (byte)((value >> 56) & 0xFF);
        }

        /// <summary>
        ///     Writes a 64-bit unsigned integer, such as a Steam id, to the byte array using little-endian byte order.
        /// </summary>
        /// <remarks>
        ///     Without this overload, C# would quietly convert a <see cref="ulong" /> to <see cref="float" /> and write
        ///     4 bytes of a rounded value.
        /// </remarks>
        /// <param name="value">The <see cref="ulong" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 8 upon return.</param>
        public static void Write(ulong value, byte[] data, ref int pos)
        {
            // Get individual bytes (not through BitConverter to avoid endianness issues)
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
            data[pos++] = (byte)((value >> 32) & 0xFF);
            data[pos++] = (byte)((value >> 40) & 0xFF);
            data[pos++] = (byte)((value >> 48) & 0xFF);
            data[pos++] = (byte)((value >> 56) & 0xFF);
        }

        /// <summary>
        ///     Attempts to read a 32-bit signed integer from the byte array using little-endian byte order.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 upon success.</param>
        /// <param name="value">The read value, or 0 if the read fails.</param>
        /// <returns>True if at least 4 bytes were available; false otherwise.</returns>
        public static bool TryReadInt32(byte[] data, ref int pos, out int value)
        {
            if (pos + 4 > data.Length)
            {
                value = 0;
                return false;
            }

            value = data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24);
            pos += 4;
            return true;
        }

        /// <summary>
        ///     Attempts to read a 64-bit signed integer from the byte array using little-endian byte order.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 8 upon success.</param>
        /// <param name="value">The read value, or 0 if the read fails.</param>
        /// <returns>True if at least 8 bytes were available; false otherwise.</returns>
        public static bool TryReadInt64(byte[] data, ref int pos, out long value)
        {
            if (pos + 8 > data.Length)
            {
                value = 0;
                return false;
            }

            value = data[pos]
                    | ((long)data[pos + 1] << 8)
                    | ((long)data[pos + 2] << 16)
                    | ((long)data[pos + 3] << 24)
                    | ((long)data[pos + 4] << 32)
                    | ((long)data[pos + 5] << 40)
                    | ((long)data[pos + 6] << 48)
                    | ((long)data[pos + 7] << 56);
            pos += 8;
            return true;
        }

        /// <summary>
        ///     Attempts to read a 64-bit unsigned integer, such as a Steam id, from the byte array using little-endian
        ///     byte order.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 8 upon success.</param>
        /// <param name="value">The read value, or 0 if the read fails.</param>
        /// <returns>True if at least 8 bytes were available; false otherwise.</returns>
        public static bool TryReadUInt64(byte[] data, ref int pos, out ulong value)
        {
            if (pos + 8 > data.Length)
            {
                value = 0;
                return false;
            }

            value = data[pos]
                    | ((ulong)data[pos + 1] << 8)
                    | ((ulong)data[pos + 2] << 16)
                    | ((ulong)data[pos + 3] << 24)
                    | ((ulong)data[pos + 4] << 32)
                    | ((ulong)data[pos + 5] << 40)
                    | ((ulong)data[pos + 6] << 48)
                    | ((ulong)data[pos + 7] << 56);
            pos += 8;
            return true;
        }

        /// <summary>
        ///     Writes a 32-bit floating-point value to the byte array using little-endian byte order.
        ///     Uses thread-safe buffer conversion.
        /// </summary>
        /// <param name="value">The <see cref="float" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 upon return.</param>
        public static void Write(float value, byte[] data, ref int pos)
        {
            lock (FloatBuffer)
            {
                FloatBuffer[0] = value;
                Buffer.BlockCopy(FloatBuffer, 0, data, pos, 4);
            }

            pos += 4;
        }

        /// <summary>
        ///     Attempts to read a 32-bit floating-point value from the byte array using little-endian byte order.
        ///     Uses thread-safe buffer conversion.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 upon success.</param>
        /// <param name="value">The read value, or 0.0f if the read fails.</param>
        /// <returns>True if at least 4 bytes were available; false otherwise.</returns>
        public static bool TryReadFloat(byte[] data, ref int pos, out float value)
        {
            if (pos + 4 > data.Length)
            {
                value = 0f;
                return false;
            }

            lock (FloatBuffer)
            {
                Buffer.BlockCopy(data, pos, FloatBuffer, 0, 4);
                value = FloatBuffer[0];
            }

            pos += 4;
            return true;
        }

        /// <summary>
        ///     Writes a 64-bit floating-point value to the byte array using little-endian byte order.
        ///     Uses thread-safe buffer conversion.
        /// </summary>
        /// <param name="value">The <see cref="double" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 8 upon return.</param>
        public static void Write(double value, byte[] data, ref int pos)
        {
            lock (DoubleBuffer)
            {
                DoubleBuffer[0] = value;
                Buffer.BlockCopy(DoubleBuffer, 0, data, pos, 8);
            }

            pos += 8;
        }

        /// <summary>
        ///     Attempts to read a 64-bit floating-point value from the byte array using little-endian byte order.
        ///     Uses thread-safe buffer conversion.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 8 upon success.</param>
        /// <param name="value">The read value, or 0.0 if the read fails.</param>
        /// <returns>True if at least 8 bytes were available; false otherwise.</returns>
        public static bool TryReadDouble(byte[] data, ref int pos, out double value)
        {
            if (pos + 8 > data.Length)
            {
                value = 0.0;
                return false;
            }

            lock (DoubleBuffer)
            {
                Buffer.BlockCopy(data, pos, DoubleBuffer, 0, 8);
                value = DoubleBuffer[0];
            }

            pos += 8;
            return true;
        }

        /// <summary>
        ///     Writes a 3D vector with 32-bit floating-point components to the byte array.
        ///     Each component (X, Y, Z) is written sequentially using <see cref="Write(float, byte[], ref int)" />.
        /// </summary>
        /// <param name="value">The <see cref="Vector3" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 12 upon return.</param>
        public static void Write(Vector3 value, byte[] data, ref int pos)
        {
            Write(value.X, data, ref pos);
            Write(value.Y, data, ref pos);
            Write(value.Z, data, ref pos);
        }

        /// <summary>
        ///     Attempts to read a 3D vector with 32-bit floating-point components from the byte array.
        ///     Each component (X, Y, Z) is read sequentially using <see cref="TryReadFloat(byte[], ref int, out float)" />.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 12 upon success.</param>
        /// <param name="value">The read vector, or <see cref="Vector3.Zero" /> if the read fails.</param>
        /// <returns>True if at least 12 bytes were available; false otherwise.</returns>
        public static bool TryReadVector3(byte[] data, ref int pos, out Vector3 value)
        {
            value = Vector3.Zero;
            float x, y, z;
            if (!TryReadFloat(data, ref pos, out x))
                return false;
            if (!TryReadFloat(data, ref pos, out y))
                return false;
            if (!TryReadFloat(data, ref pos, out z))
                return false;
            value = new Vector3(x, y, z);
            return true;
        }

        /// <summary>
        ///     Writes a 3D vector with 64-bit floating-point components to the byte array.
        ///     Each component (X, Y, Z) is written sequentially using <see cref="Write(double, byte[], ref int)" />.
        /// </summary>
        /// <param name="value">The <see cref="Vector3D" /> value to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 24 upon return.</param>
        public static void Write(Vector3D value, byte[] data, ref int pos)
        {
            Write(value.X, data, ref pos);
            Write(value.Y, data, ref pos);
            Write(value.Z, data, ref pos);
        }

        /// <summary>
        ///     Attempts to read a 3D vector with 64-bit floating-point components from the byte array.
        ///     Each component (X, Y, Z) is read sequentially using <see cref="TryReadDouble(byte[], ref int, out double)" />.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 24 upon success.</param>
        /// <param name="value">The read vector, or <see cref="Vector3D.Zero" /> if the read fails.</param>
        /// <returns>True if at least 24 bytes were available; false otherwise.</returns>
        public static bool TryReadVector3D(byte[] data, ref int pos, out Vector3D value)
        {
            value = Vector3D.Zero;
            double x, y, z;
            if (!TryReadDouble(data, ref pos, out x))
                return false;
            if (!TryReadDouble(data, ref pos, out y))
                return false;
            if (!TryReadDouble(data, ref pos, out z))
                return false;
            value = new Vector3D(x, y, z);
            return true;
        }

        // How many chars of value, from offset, the StringBuilder writer encodes in one go: as many as CharBuffer
        // holds, but never ending between the two halves of a surrogate pair (a character outside the Basic
        // Multilingual Plane, such as an emoji), which would encode each half as a replacement character.
        static int ChunkSize(StringBuilder value, int offset, int charCount)
        {
            var size = Math.Min(CharBuffer.Length, charCount - offset);
            if (offset + size < charCount && char.IsHighSurrogate(value[offset + size - 1]))
                size--;
            return size;
        }

        /// <summary>
        ///     Writes a string from a <see cref="StringBuilder" /> to the byte array using UTF-8 encoding.
        ///     The string is prefixed with a 32-bit length field (byte count). Avoids string allocation
        ///     by extracting characters directly from the StringBuilder. A null StringBuilder is written as length -1,
        ///     as <see cref="WriteString(string, byte[], ref int)" /> writes a null string.
        /// </summary>
        /// <param name="value">The <see cref="StringBuilder" /> containing the string to write.</param>
        /// <param name="data">The destination byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 + byte length upon return.</param>
        public static void WriteString(StringBuilder value, byte[] data, ref int pos)
        {
            // Null and empty are written as the string overload writes them, so either reader can take either.
            if (value == null)
            {
                Write(-1, data, ref pos);
                return;
            }

            if (value.Length == 0)
            {
                Write(0, data, ref pos);
                return;
            }

            var charCount = value.Length;
            lock (CharBuffer)
            {
                // The length goes first, but is only known once the text is encoded: leave room for it, encode
                // the text once straight into data in chunks, then write the length into the room left.
                var lengthPos = pos;
                pos += 4;
                var offset = 0;
                while (offset < charCount)
                {
                    var chunkSize = ChunkSize(value, offset, charCount);
                    value.CopyTo(offset, CharBuffer, 0, chunkSize);
                    var bytesWritten = Utf8.GetBytes(CharBuffer, 0, chunkSize, data, pos);
                    pos += bytesWritten;
                    offset += chunkSize;
                }

                Write(pos - lengthPos - 4, data, ref lengthPos);
            }
        }

        /// <summary>
        ///     Attempts to read a string from the byte array into a <see cref="StringBuilder" /> using UTF-8 encoding.
        ///     The string must be prefixed with a 32-bit length field (byte count). Avoids string allocation by
        ///     decoding UTF-8 bytes into a char buffer and appending directly to the StringBuilder.
        /// </summary>
        /// <param name="data">The source byte array.</param>
        /// <param name="pos">The starting position in <paramref name="data" />; incremented by 4 + byte length upon success.</param>
        /// <param name="value">The <see cref="StringBuilder" /> to append the decoded string to.</param>
        /// <returns>True if the string was read, or was null (nothing is appended then); false otherwise.</returns>
        public static bool TryReadString(byte[] data, ref int pos, StringBuilder value)
        {
            if (value == null)
                return false;

            // Read length prefix
            int byteCount;
            if (!TryReadInt32(data, ref pos, out byteCount))
                return false;

            // Empty, or null as either writer writes it (-1): a StringBuilder cannot hold null, so nothing is
            // appended. Any other negative length is not something a writer writes.
            if (byteCount == 0 || byteCount == -1)
                return true;
            if (byteCount < 0)
                return false;

            // Check bounds
            if (byteCount > data.Length - pos)
                return false;

            // Room for exactly the decoded string, counted first without allocating: growing chunk by chunk would
            // copy everything read so far each time.
            value.EnsureCapacity(value.Length + Utf8.GetCharCount(data, pos, byteCount));

            lock (CharBuffer)
            {
                var bytesProcessed = 0;
                while (bytesProcessed < byteCount)
                {
                    // A chunk of at most CharBuffer.Length bytes can never decode to more chars than that. A chunk
                    // that stops short of the end is backed off to a UTF-8 character boundary (not in front of a
                    // 10xxxxxx continuation byte), so no character is split between chunks.
                    var bytesRemaining = byteCount - bytesProcessed;
                    var bytesToProcess = Math.Min(bytesRemaining, CharBuffer.Length);
                    if (bytesToProcess < bytesRemaining)
                    {
                        var boundary = bytesToProcess;
                        while (boundary > 0 && (data[pos + bytesProcessed + boundary] & 0xC0) == 0x80)
                            boundary--;
                        if (boundary > 0)
                            bytesToProcess = boundary;
                    }

                    var charsDecoded = Utf8.GetChars(data, pos + bytesProcessed, bytesToProcess, CharBuffer, 0);
                    value.Append(CharBuffer, 0, charsDecoded);
                    bytesProcessed += bytesToProcess;
                }
            }

            pos += byteCount;
            return true;
        }

        /// <summary>
        ///     Writes a string to the byte array using UTF-8 encoding.
        ///     The string is prefixed with a 32-bit length field (byte count).
        ///     A null string is written as length -1.
        /// </summary>
        public static void WriteString(string value, byte[] data, ref int pos)
        {
            if (value == null)
            {
                Write(-1, data, ref pos);
                return;
            }

            if (value.Length == 0)
            {
                Write(0, data, ref pos);
                return;
            }

            // Encoded once, straight into data: room is left for the length, which is written once known.
            var lengthPos = pos;
            pos += 4;
            pos += Utf8.GetBytes(value, 0, value.Length, data, pos);
            Write(pos - lengthPos - 4, data, ref lengthPos);
        }

        /// <summary>
        ///     Attempts to read a string from the byte array using UTF-8 encoding.
        ///     The string must be prefixed with a 32-bit length field (byte count).
        ///     A length of -1 indicates a null string.
        /// </summary>
        public static bool TryReadString(byte[] data, ref int pos, out string value)
        {
            value = null;
            int byteCount;
            if (!TryReadInt32(data, ref pos, out byteCount))
                return false;

            // Null is written as -1; any other negative length is not something a writer writes.
            if (byteCount == -1)
                return true;
            if (byteCount < 0)
                return false;

            if (byteCount == 0)
            {
                value = string.Empty;
                return true;
            }

            if (byteCount > data.Length - pos)
                return false;

            value = Utf8.GetString(data, pos, byteCount);
            pos += byteCount;
            return true;
        }

        /// <summary>
        ///     Returns the number of bytes <see cref="WriteString(StringBuilder, byte[], ref int)" /> writes for a
        ///     <see cref="StringBuilder" /> (4 byte length prefix + UTF-8 bytes), counted without allocating a string.
        /// </summary>
        public static int SizeOfString(StringBuilder value)
        {
            if (value == null || value.Length == 0)
                return 4;
            lock (CharBuffer)
                return 4 + Utf8ByteCount(value);
        }

        // The UTF-8 byte count of a non-empty StringBuilder, counted in the chunks the writer encodes it in. The
        // caller holds the CharBuffer lock.
        static int Utf8ByteCount(StringBuilder value)
        {
            var charCount = value.Length;
            var byteCount = 0;
            var offset = 0;
            while (offset < charCount)
            {
                var chunkSize = ChunkSize(value, offset, charCount);
                value.CopyTo(offset, CharBuffer, 0, chunkSize);
                byteCount += Utf8.GetByteCount(CharBuffer, 0, chunkSize);
                offset += chunkSize;
            }
            return byteCount;
        }

        /// <summary>
        ///     Returns the number of bytes needed to serialize a string (4 byte length prefix + UTF-8 bytes).
        /// </summary>
        public static int SizeOfString(string value)
        {
            if (value == null || value.Length == 0)
                return 4;
            return 4 + Utf8.GetByteCount(value);
        }
    }
}