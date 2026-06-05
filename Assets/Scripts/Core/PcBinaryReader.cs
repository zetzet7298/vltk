// -----------------------------------------------------------------------------
// VLTK Mobile — ST-00.2 Binary Reader Không GC Cho Runtime Config
// Zero-GC struct-based binary reader for PC .dat/.pak binary formats.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace VLTK.Core
{
    /// <summary>
    /// Zero-GC struct binary reader. Wraps byte[] buffer + position.
    /// Dùng cho hot-path đọc binary data (Region_S.dat, SPR headers, v.v.)
    /// không tạo GC allocation.
    /// </summary>
    public struct PcBinaryReader
    {
        public readonly byte[] Buffer;
        public int Position;

        public PcBinaryReader(byte[] buffer)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            Position = 0;
        }

        public int Length => Buffer?.Length ?? 0;
        public int Remaining => Length - Position;
        public bool HasMore => Position < Length;

        // ── Read primitives (little-endian, no alloc) ──────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (Position >= Length) throw new EndOfStreamException();
            return Buffer[Position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            int p = Position;
            if (p + 2 > Length) throw new EndOfStreamException();
            ushort v = (ushort)(Buffer[p] | (Buffer[p + 1] << 8));
            Position = p + 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            int p = Position;
            if (p + 4 > Length) throw new EndOfStreamException();
            uint v = (uint)(Buffer[p] | (Buffer[p + 1] << 8) |
                           (Buffer[p + 2] << 16) | (Buffer[p + 3] << 24));
            Position = p + 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            int p = Position;
            if (p + 4 > Length) throw new EndOfStreamException();
            float v = BitConverter.Int32BitsToSingle(
                Buffer[p] | (Buffer[p + 1] << 8) |
                (Buffer[p + 2] << 16) | (Buffer[p + 3] << 24));
            Position = p + 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            int p = Position;
            if (p + 8 > Length) throw new EndOfStreamException();
            uint lo = (uint)(Buffer[p] | (Buffer[p + 1] << 8) |
                            (Buffer[p + 2] << 16) | (Buffer[p + 3] << 24));
            uint hi = (uint)(Buffer[p + 4] | (Buffer[p + 5] << 8) |
                            (Buffer[p + 6] << 16) | (Buffer[p + 7] << 24));
            Position = p + 8;
            return ((ulong)hi << 32) | lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        // ── String reads (zero-alloc) ──────────────────────────────────────

        /// <summary>
        /// Đọc fixed-length string. Ghi vào shared buffer, trả về char count.
        /// Caller cung cấp char[] buffer (pre-allocated).
        /// </summary>
        public int ReadStringFixed(int maxBytes, char[] sharedCharBuffer)
        {
            int p = Position;
            int len = Math.Min(maxBytes, Remaining);
            int charCount = 0;
            for (int i = 0; i < len; i++)
            {
                byte b = Buffer[p + i];
                if (b == 0) break;
                if (charCount < sharedCharBuffer.Length)
                    sharedCharBuffer[charCount++] = (char)b;
            }
            Position = p + maxBytes; // Advance by fixed size regardless
            return charCount;
        }

        /// <summary>
        /// Đọc null-terminated string. Ghi vào shared buffer.
        /// Trả về số chars đã đọc.
        /// </summary>
        public int ReadStringNullTerminated(char[] sharedCharBuffer, int maxChars)
        {
            int charCount = 0;
            while (Position < Length && charCount < maxChars)
            {
                byte b = Buffer[Position++];
                if (b == 0) break;
                sharedCharBuffer[charCount++] = (char)b;
            }
            return charCount;
        }

        /// <summary>
        /// Đọc bytes vào pre-allocated shared buffer.
        /// </summary>
        public int ReadBytes(int count, byte[] sharedBuffer)
        {
            int len = Math.Min(count, Remaining);
            int len2 = Math.Min(len, sharedBuffer.Length);
            System.Buffer.BlockCopy(Buffer, Position, sharedBuffer, 0, len2);
            Position += len;
            return len2;
        }

        /// <summary>
        /// Skip bytes without reading.
        /// </summary>
        public void Skip(int count)
        {
            Position += count;
        }

        /// <summary>
        /// Đọc raw byte span (no copy). Trả về ReadOnlySpan trỏ vào buffer.
        /// </summary>
        public ReadOnlySpan<byte> ReadSpan(int count)
        {
            int len = Math.Min(count, Remaining);
            var span = new ReadOnlySpan<byte>(Buffer, Position, len);
            Position += len;
            return span;
        }
    }

    // ─── Allocating reader (for one-time startup loading) ────────────────────

    /// <summary>
    /// Binary reader cho phép allocation. Dùng khi loading một lần (config, SPR headers).
    /// Trả về real strings.
    /// </summary>
    public class PcBinaryReaderAlloc
    {
        private readonly byte[] _buffer;
        private int _position;

        public PcBinaryReaderAlloc(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _position = 0;
        }

        public int Length => _buffer.Length;
        public int Position => _position;
        public int Remaining => Length - _position;

        public byte ReadByte()
        {
            if (_position >= Length) throw new EndOfStreamException();
            return _buffer[_position++];
        }

        public sbyte ReadSByte() => (sbyte)ReadByte();

        public ushort ReadUInt16()
        {
            int p = _position;
            if (p + 2 > Length) throw new EndOfStreamException();
            ushort v = (ushort)(_buffer[p] | (_buffer[p + 1] << 8));
            _position = p + 2;
            return v;
        }

        public short ReadInt16() => (short)ReadUInt16();

        public uint ReadUInt32()
        {
            int p = _position;
            if (p + 4 > Length) throw new EndOfStreamException();
            uint v = (uint)(_buffer[p] | (_buffer[p + 1] << 8) |
                           (_buffer[p + 2] << 16) | (_buffer[p + 3] << 24));
            _position = p + 4;
            return v;
        }

        public int ReadInt32() => (int)ReadUInt32();

        public float ReadSingle()
        {
            int p = _position;
            if (p + 4 > Length) throw new EndOfStreamException();
            float v = BitConverter.Int32BitsToSingle(
                _buffer[p] | (_buffer[p + 1] << 8) |
                (_buffer[p + 2] << 16) | (_buffer[p + 3] << 24));
            _position = p + 4;
            return v;
        }

        public ulong ReadUInt64()
        {
            int p = _position;
            if (p + 8 > Length) throw new EndOfStreamException();
            uint lo = (uint)(_buffer[p] | (_buffer[p + 1] << 8) |
                            (_buffer[p + 2] << 16) | (_buffer[p + 3] << 24));
            uint hi = (uint)(_buffer[p + 4] | (_buffer[p + 5] << 8) |
                            (_buffer[p + 6] << 16) | (_buffer[p + 7] << 24));
            _position = p + 8;
            return ((ulong)hi << 32) | lo;
        }

        public long ReadInt64() => (long)ReadUInt64();

        /// <summary>
        /// Đọc fixed-length string (GB2312/ASCII). Trả về string (có alloc).
        /// </summary>
        public string ReadStringFixed(int maxBytes)
        {
            int p = _position;
            int len = Math.Min(maxBytes, Remaining);
            // Find null terminator
            int actualLen = 0;
            for (int i = 0; i < len; i++)
            {
                if (_buffer[p + i] == 0) { actualLen = i; break; }
                actualLen = i + 1;
            }
            string s = Encoding.ASCII.GetString(_buffer, p, actualLen);
            _position = p + maxBytes;
            return s;
        }

        /// <summary>
        /// Đọc null-terminated string. Trả về string (có alloc).
        /// </summary>
        public string ReadStringNullTerminated()
        {
            int p = _position;
            int len = 0;
            while (p + len < Length && _buffer[p + len] != 0)
                len++;
            string s = Encoding.ASCII.GetString(_buffer, p, len);
            _position = p + len + 1; // Skip past null terminator
            return s;
        }

        /// <summary>
        /// Đọc raw bytes vào new array (alloc).
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            int len = Math.Min(count, Remaining);
            var result = new byte[len];
            System.Buffer.BlockCopy(_buffer, _position, result, 0, len);
            _position += len;
            return result;
        }

        public void Skip(int count) { _position += count; }

        /// <summary>Reset reader position.</summary>
        public void Reset() { _position = 0; }
    }
}
