using SharpSprite.Core.Document;
using SharpSprite.Infrastructure.Ase;

namespace SharpSprite.Infrastructure
{
    /// <summary>
    /// High-level document I/O service.
    ///
    /// The format is detected automatically from the file extension.
    /// Only the native .ase / .aseprite format is currently supported.
    /// </summary>
    public static class DocumentIO
    {
        // ── Load ──────────────────────────────────────────────────────────

        /// <summary>
        /// Load a document from <paramref name="path"/>.
        /// Detects format from the file extension.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown when the format is not recognised.
        /// </exception>
        /// <exception cref="System.IO.InvalidDataException">
        /// Thrown when the file data is corrupt.
        /// </exception>
        public static Document Load(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".ase" or ".aseprite" => AseDecoder.DecodeFile(path),
                _ => throw new NotSupportedException($"Unsupported file format: '{ext}'")
            };
        }

        /// <summary>
        /// Load a document from a <see cref="Stream"/> using the given
        /// format hint (file extension including the dot, e.g. ".aseprite").
        /// </summary>
        public static Document Load(Stream stream, string formatHint = ".aseprite")
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            string ext = (formatHint ?? ".aseprite").ToLowerInvariant();
            return ext switch
            {
                ".ase" or ".aseprite" => AseDecoder.DecodeStream(stream),
                _ => throw new NotSupportedException($"Unsupported format hint: '{ext}'")
            };
        }

        // ── Save ──────────────────────────────────────────────────────────

        /// <summary>
        /// Save <paramref name="doc"/> to <paramref name="path"/>.
        /// Detects format from the file extension.
        /// If the path ends in .ase or .aseprite the native format is used.
        /// The document's <see cref="Document.FilePath"/> and
        /// <see cref="Document.IsModified"/> are updated on success.
        /// </summary>
        public static void Save(Document doc, string path)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (path == null) throw new ArgumentNullException(nameof(path));

            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".ase":
                case ".aseprite":
                    AseEncoder.EncodeFile(doc, path);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported file format: '{ext}'");
            }

            doc.FilePath = path;
            doc.IsModified = false;
        }

        /// <summary>
        /// Save <paramref name="doc"/> to an arbitrary <see cref="Stream"/>
        /// in the native .aseprite format.
        /// </summary>
        public static void Save(Document doc, Stream stream)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            AseEncoder.EncodeStream(doc, stream);
        }

        // ── Convenience ───────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="path"/> has a file extension
        /// recognised by this IO layer.
        /// </summary>
        public static bool IsSupported(string path)
        {
            string ext = Path.GetExtension(path ?? "").ToLowerInvariant();
            return ext is ".ase" or ".aseprite";
        }
    }
}