using System.Text;

namespace SharpSprite.Infrastructure.Ase
{
    /// <summary>
    /// Wraps a <see cref="BinaryWriter"/> and adds convenience methods for
    /// the Aseprite type system (WORD, DWORD, SHORT, STRING, FIXED, …).
    /// All multi-byte integers are written little-endian.
    /// </summary>
    internal sealed class AseWriter : IDisposable
    {
        private readonly BinaryWriter _w;

        public AseWriter(Stream stream, bool leaveOpen = false)
        {
            _w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);
        }

        // ── Primitives ────────────────────────────────────────────────────

        public void WriteByte(byte v) => _w.Write(v);
        public void WriteBytes(byte[] data) => _w.Write(data);
        public void WriteBytes(byte[] data, int offset, int count) => _w.Write(data, offset, count);

        /// <summary>WORD – 16-bit unsigned integer.</summary>
        public void WriteWORD(ushort v) => _w.Write(v);

        /// <summary>SHORT – 16-bit signed integer.</summary>
        public void WriteSHORT(short v) => _w.Write(v);

        /// <summary>DWORD – 32-bit unsigned integer.</summary>
        public void WriteDWORD(uint v) => _w.Write(v);

        /// <summary>LONG – 32-bit signed integer.</summary>
        public void WriteLONG(int v) => _w.Write(v);

        /// <summary>FLOAT – 32-bit IEEE 754.</summary>
        public void WriteFLOAT(float v) => _w.Write(v);

        /// <summary>DOUBLE – 64-bit IEEE 754.</summary>
        public void WriteDOUBLE(double v) => _w.Write(v);

        /// <summary>FIXED – 16.16 fixed point.</summary>
        public void WriteFIXED(float v)
        {
            int raw = (int)(v * 65536f);
            _w.Write(raw);
        }

        /// <summary>
        /// STRING: WORD length + UTF-8 bytes (no null terminator).
        /// </summary>
        public void WriteSTRING(string s)
        {
            byte[] data = Encoding.UTF8.GetBytes(s);
            WriteWORD((ushort)data.Length);
            _w.Write(data);
        }

        /// <summary>Write <paramref name="count"/> zero bytes.</summary>
        public void WritePad(int count)
        {
            for (int i = 0; i < count; i++) _w.Write((byte)0);
        }

        /// <summary>Current stream position.</summary>
        public long Position => _w.BaseStream.Position;

        /// <summary>Seek to a position.</summary>
        public void Seek(long pos, SeekOrigin origin = SeekOrigin.Begin)
            => _w.BaseStream.Seek(pos, origin);

        public void Flush() => _w.Flush();

        public void Dispose() => _w.Dispose();
    }
}