using System.Text;

namespace SharpSprite.Infrastructure.Ase
{
    /// <summary>
    /// Wraps a <see cref="BinaryReader"/> and adds convenience methods for
    /// the Aseprite type system (WORD, DWORD, SHORT, STRING, FIXED, …).
    /// All multi-byte integers are little-endian (Intel byte order).
    /// </summary>
    internal sealed class AseReader : IDisposable
    {
        private readonly BinaryReader _r;

        public AseReader(Stream stream, bool leaveOpen = false)
        {
            _r = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
        }

        // ── Primitives ────────────────────────────────────────────────────

        public byte ReadByte() => _r.ReadByte();
        public byte[] ReadBytes(int count) => _r.ReadBytes(count);

        /// <summary>WORD – 16-bit unsigned integer.</summary>
        public ushort ReadWORD() => _r.ReadUInt16();

        /// <summary>SHORT – 16-bit signed integer.</summary>
        public short ReadSHORT() => _r.ReadInt16();

        /// <summary>DWORD – 32-bit unsigned integer.</summary>
        public uint ReadDWORD() => _r.ReadUInt32();

        /// <summary>LONG – 32-bit signed integer.</summary>
        public int ReadLONG() => _r.ReadInt32();

        /// <summary>QWORD – 64-bit unsigned integer.</summary>
        public ulong ReadQWORD() => _r.ReadUInt64();

        /// <summary>LONG64 – 64-bit signed integer.</summary>
        public long ReadLONG64() => _r.ReadInt64();

        /// <summary>FLOAT – 32-bit IEEE 754.</summary>
        public float ReadFLOAT() => _r.ReadSingle();

        /// <summary>DOUBLE – 64-bit IEEE 754.</summary>
        public double ReadDOUBLE() => _r.ReadDouble();

        /// <summary>FIXED – 16.16 fixed point.</summary>
        public float ReadFIXED()
        {
            int raw = _r.ReadInt32();
            return raw / 65536f;
        }

        /// <summary>
        /// STRING: WORD length + UTF-8 bytes (no null terminator).
        /// </summary>
        public string ReadSTRING()
        {
            ushort len = ReadWORD();
            byte[] data = _r.ReadBytes(len);
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>UUID – 16 raw bytes.</summary>
        public byte[] ReadUUID() => _r.ReadBytes(16);

        /// <summary>Current stream position.</summary>
        public long Position => _r.BaseStream.Position;

        /// <summary>Seek to a position.</summary>
        public void Seek(long pos, SeekOrigin origin = SeekOrigin.Begin)
            => _r.BaseStream.Seek(pos, origin);

        /// <summary>Skip <paramref name="count"/> bytes.</summary>
        public void Skip(int count) => _r.BaseStream.Seek(count, SeekOrigin.Current);

        public void Dispose() => _r.Dispose();
    }
}