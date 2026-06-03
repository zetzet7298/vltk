// -----------------------------------------------------------------------------
// VLTK Mobile — ST-00.2 Binary Reader Tests
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Core;

namespace VLTK.Tests.Sandbox
{
    public class PcBinaryReaderTests
    {
        [Test]
        public void ReadByte_TracksPosition()
        {
            var data = new byte[] { 0x10, 0x20, 0x30 };
            var reader = new PcBinaryReader(data);

            Assert.AreEqual(0x10, reader.ReadByte());
            Assert.AreEqual(0x20, reader.ReadByte());
            Assert.AreEqual(0x30, reader.ReadByte());
            Assert.AreEqual(3, reader.Position);
            Assert.IsFalse(reader.HasMore);
        }

        [Test]
        public void ReadInt32_LittleEndian()
        {
            // 0x04030201 = 67305985
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var reader = new PcBinaryReader(data);

            Assert.AreEqual(0x04030201, reader.ReadInt32());
            Assert.AreEqual(4, reader.Position);
        }

        [Test]
        public void ReadUInt16_LittleEndian()
        {
            var data = new byte[] { 0x34, 0x12 };
            var reader = new PcBinaryReader(data);

            Assert.AreEqual(0x1234, reader.ReadUInt16());
        }

        [Test]
        public void ReadSingle_LittleEndian()
        {
            // 1.0f = 0x3F800000 → bytes: 00 00 80 3F
            var data = new byte[] { 0x00, 0x00, 0x80, 0x3F };
            var reader = new PcBinaryReader(data);

            Assert.AreEqual(1.0f, reader.ReadSingle(), 0.0001f);
        }

        [Test]
        public void ReadStringFixed_RespectsMaxBytes()
        {
            var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x00, 0x00 };
            var reader = new PcBinaryReader(data);
            var buf = new char[32];

            int count = reader.ReadStringFixed(8, buf);
            Assert.AreEqual(5, count); // "Hello" = 5 chars
            Assert.AreEqual(8, reader.Position); // Advanced by fixed size
        }

        [Test]
        public void ReadStringNullTerminated_StopsAtNull()
        {
            var data = new byte[] { 0x41, 0x42, 0x43, 0x00, 0x44 };
            var reader = new PcBinaryReader(data);
            var buf = new char[32];

            int count = reader.ReadStringNullTerminated(buf, 32);
            Assert.AreEqual(3, count); // "ABC"
            Assert.AreEqual(4, reader.Position); // Past the null
        }

        [Test]
        public void Skip_AdvancesPosition()
        {
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var reader = new PcBinaryReader(data);

            reader.Skip(3);
            Assert.AreEqual(3, reader.Position);
            Assert.AreEqual(0x04, reader.ReadByte());
        }

        [Test]
        public void ReadSpan_NoCopy()
        {
            var data = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D };
            var reader = new PcBinaryReader(data);

            var span = reader.ReadSpan(2);
            Assert.AreEqual(2, span.Length);
            Assert.AreEqual(0x0A, span[0]);
            Assert.AreEqual(0x0B, span[1]);
            Assert.AreEqual(2, reader.Position);
        }

        [Test]
        public void ReadBytes_IntoSharedBuffer()
        {
            var data = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            var reader = new PcBinaryReader(data);
            var shared = new byte[2];

            int read = reader.ReadBytes(2, shared);
            Assert.AreEqual(2, read);
            Assert.AreEqual(0x10, shared[0]);
            Assert.AreEqual(0x20, shared[1]);
        }

        // ── PcBinaryReaderAlloc tests ────────────────────────────────────

        [Test]
        public void Alloc_ReadStringFixed_ReturnsString()
        {
            var data = new byte[] { 0x48, 0x69, 0x00, 0x00 };
            var reader = new PcBinaryReaderAlloc(data);

            string s = reader.ReadStringFixed(4);
            Assert.AreEqual("Hi", s);
            Assert.AreEqual(4, reader.Position);
        }

        [Test]
        public void Alloc_ReadStringNullTerminated_ReturnsString()
        {
            var data = new byte[] { 0x41, 0x42, 0x00, 0x43 };
            var reader = new PcBinaryReaderAlloc(data);

            string s = reader.ReadStringNullTerminated();
            Assert.AreEqual("AB", s);
            Assert.AreEqual(3, reader.Position);
        }

        [Test]
        public void Alloc_Reset_RestartsPosition()
        {
            var data = new byte[] { 0x01, 0x02 };
            var reader = new PcBinaryReaderAlloc(data);

            reader.ReadByte();
            Assert.AreEqual(1, reader.Position);
            reader.Reset();
            Assert.AreEqual(0, reader.Position);
            Assert.AreEqual(0x01, reader.ReadByte());
        }
    }
}
